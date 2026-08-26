#requires -Version 7.4

[CmdletBinding()]
param(
    [switch]$UpdateBaselines,
    [string]$DllPath = '',
    [string]$GeneXusDirectory = 'C:\Program Files (x86)\GeneXus\GeneXus18'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$baselinesRoot = Join-Path $PSScriptRoot 'Baselines'
$readerSourcePath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\TransactionStructureReader.cs'
$helperSourcePath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\TransactionAttributeKeyTraits.cs'
$domainSourcePath = Join-Path $repositoryRoot 'Src\Domain\ApiPlan.cs'
$contractSourcePath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\PrototypeWizardContract.cs'

if ([string]::IsNullOrWhiteSpace($DllPath)) {
    $DllPath = Join-Path $repositoryRoot 'Src\Extension\bin\Release\net471\GenexusOpenApiBuilder.Extension.dll'
}

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw "ASSERT_TRUE_FAILED: $Message"
    }
}

function Assert-Contains {
    param([string]$Text, [string]$Needle, [string]$Message)
    if ($Text.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "ASSERT_CONTAINS_FAILED: $Message"
    }
}

function Assert-NotContains {
    param([string]$Text, [string]$Needle, [string]$Message)
    if ($Text.IndexOf($Needle, [StringComparison]::Ordinal) -ge 0) {
        throw "ASSERT_NOT_CONTAINS_FAILED: $Message"
    }
}

function Write-Utf8LfFile {
    param(
        [Parameter(Mandatory)] [string]$Path,
        [Parameter(Mandatory)] [string]$Content
    )

    $directory = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $directory -PathType Container)) {
        [void][System.IO.Directory]::CreateDirectory($directory)
    }

    $normalized = $Content.Replace("`r`n", "`n").Replace("`r", "`n")
    if (-not $normalized.EndsWith("`n")) {
        $normalized += "`n"
    }

    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $normalized, $encoding)
}

Assert-True (Test-Path -LiteralPath $readerSourcePath -PathType Leaf) "Fonte ausente: $readerSourcePath"
Assert-True (Test-Path -LiteralPath $helperSourcePath -PathType Leaf) "Fonte ausente: $helperSourcePath"
Assert-True (Test-Path -LiteralPath $domainSourcePath -PathType Leaf) "Fonte ausente: $domainSourcePath"
Assert-True (Test-Path -LiteralPath $contractSourcePath -PathType Leaf) "Fonte ausente: $contractSourcePath"

$readerSource = [IO.File]::ReadAllText($readerSourcePath)
$helperSource = [IO.File]::ReadAllText($helperSourcePath)
$domainSource = [IO.File]::ReadAllText($domainSourcePath)
$contractSource = [IO.File]::ReadAllText($contractSourcePath)

Assert-Contains $readerSource 'MapLevel(root)' 'Adaptador SDK deve mapear Structure.Root.'
Assert-Contains $readerSource 'level.Levels.Select(MapLevel)' 'Adaptador SDK deve mapear filhos Levels.'
Assert-Contains $readerSource 'ReadLevel(rootLevel' 'Núcleo recursivo ReadLevel deve existir.'
Assert-Contains $readerSource 'TransactionStructureLevelSource' 'Fonte neutra da árvore deve existir.'
Assert-Contains $readerSource 'Build(' 'CreateFixtures deve passar pelo núcleo Build.'
Assert-Contains $readerSource 'PrimaryKeyNames' 'Ordem da PK deve vir de PrimaryKeyNames.'
Assert-Contains $readerSource '"<unnamed>"' 'Fallback de nível sem nome deve existir.'
Assert-Contains $readerSource 'TransactionAttributeKeyTraits.IsAutonumber' 'CreateField deve usar o helper compartilhado.'
Assert-Contains $helperSource 'GetPropertyValueString("Autonumber")' 'Helper deve ler Autonumber.'
Assert-Contains $helperSource 'GetPropertyValueString("idAUTONUMBER")' 'Helper deve ter fallback idAUTONUMBER.'
Assert-Contains $domainSource 'class ApiPlanLevel' 'Modelo ApiPlanLevel deve existir.'
Assert-Contains $domainSource 'IReadOnlyList<ApiPlanLevel> Levels' 'ApiPlan deve expor Levels.'
Assert-Contains $contractSource 'TransactionAttributeKeyTraits.IsAutonumber' 'Wizard flat deve usar o helper.'
Assert-NotContains $contractSource 'TransactionStructureReader' 'Wizard flat não deve acoplar o leitor hierárquico B095.'
Assert-NotContains $readerSource 'FromRootLevel' 'Fixtures não devem contornar o núcleo via FromRootLevel.'

if (-not (Test-Path -LiteralPath $DllPath -PathType Leaf)) {
    Write-Output "ENVIRONMENT_BLOCKED: DLL Release ausente em $DllPath"
    exit 2
}

function Get-AssemblyDirectoryCandidates {
    param([string]$GeneXusRoot)

    $candidates = [System.Collections.Generic.List[string]]::new()
    foreach ($relative in @('Packages', 'GeneXusBlazorControls', '')) {
        $path = if ([string]::IsNullOrWhiteSpace($relative)) { $GeneXusRoot } else { Join-Path $GeneXusRoot $relative }
        if (Test-Path -LiteralPath $path -PathType Container) {
            $candidates.Add($path)
        }
    }

    $dllDirectory = Split-Path -Parent $DllPath
    if (Test-Path -LiteralPath $dllDirectory -PathType Container) {
        $candidates.Add($dllDirectory)
    }

    return @($candidates | Select-Object -Unique)
}

function Initialize-GeneXusAssemblyResolver {
    param([string[]]$SearchDirectories)

    $script:AssemblySearchDirectories = @($SearchDirectories)
    $script:AssemblyResolveBusy = $false
    $script:AssemblyResolveHandler = [System.ResolveEventHandler]{
        param($sender, $args)
        if ($script:AssemblyResolveBusy) {
            return $null
        }

        $script:AssemblyResolveBusy = $true
        try {
            $requestedName = New-Object System.Reflection.AssemblyName($args.Name)
            $simpleName = $requestedName.Name
            if ($simpleName.EndsWith('.resources', [System.StringComparison]::OrdinalIgnoreCase)) {
                return $null
            }

            foreach ($directory in $script:AssemblySearchDirectories) {
                $candidate = Join-Path $directory ($simpleName + '.dll')
                if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                    return [System.Reflection.Assembly]::LoadFrom($candidate)
                }
            }

            return $null
        }
        finally {
            $script:AssemblyResolveBusy = $false
        }
    }

    [System.AppDomain]::CurrentDomain.add_AssemblyResolve($script:AssemblyResolveHandler)
}

function Get-Prop {
    param($Object, [string]$Name)
    $property = $Object.GetType().GetProperty($Name, [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    Assert-True ($null -ne $property) "Propriedade ausente: $Name"
    return $property.GetValue($Object)
}

function Find-LevelByName {
    param($Flattened, [string]$Name)
    foreach ($level in $Flattened) {
        if ([string](Get-Prop $level 'LevelName') -eq $Name) {
            return $level
        }
    }

    return $null
}

function Find-FieldByName {
    param($Fields, [string]$Name)
    foreach ($field in $Fields) {
        if ([string](Get-Prop $field 'Name') -eq $Name) {
            return $field
        }
    }

    return $null
}

$script:AssemblyResolveHandler = $null
try {
    Initialize-GeneXusAssemblyResolver -SearchDirectories (Get-AssemblyDirectoryCandidates -GeneXusRoot $GeneXusDirectory)
    $assembly = [System.Reflection.Assembly]::LoadFrom($DllPath)
    $readerType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.TransactionStructureReader', $true, $false)
    $helperType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.TransactionAttributeKeyTraits', $true, $false)
    $snapshotType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.TransactionStructureSnapshot', $true, $false)

    $createFixtures = $readerType.GetMethod('CreateFixtures', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $serialize = $readerType.GetMethod('SerializeSnapshot', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $normalize = $readerType.GetMethod('NormalizeForComparison', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $isAutonumberPure = $helperType.GetMethod('IsAutonumberCore', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $flattenMethod = $snapshotType.GetMethod('FlattenLevels', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')

    Assert-True ($null -ne $createFixtures) 'CreateFixtures não encontrado.'
    Assert-True ($null -ne $serialize) 'SerializeSnapshot não encontrado.'
    Assert-True ($null -ne $normalize) 'NormalizeForComparison não encontrado.'
    Assert-True ($null -ne $isAutonumberPure) 'IsAutonumberCore não encontrado.'
    Assert-True ($null -ne $flattenMethod) 'FlattenLevels não encontrado.'

    # Núcleo puro do helper: defeito aqui falha o assert (não é constante de fixture).
    Assert-True (-not [bool]$isAutonumberPure.Invoke($null, @(3, $true, 'True'))) 'PK composta nunca é autonumerada.'
    Assert-True (-not [bool]$isAutonumberPure.Invoke($null, @(3, $false, $null))) 'PK composta sem metadata = false (contagem sobre fail-open).'
    Assert-True ([bool]$isAutonumberPure.Invoke($null, @(1, $true, 'True'))) 'PK simples Autonumber=True.'
    Assert-True (-not [bool]$isAutonumberPure.Invoke($null, @(1, $true, 'False'))) 'PK simples Autonumber=False.'
    Assert-True ([bool]$isAutonumberPure.Invoke($null, @(1, $false, 'False'))) 'PK simples sem metadata, fail-open = true.'

    $fixtures = @($createFixtures.Invoke($null, @()))
    Assert-True ($fixtures.Count -eq 4) "Esperava 4 fixtures; encontrado $($fixtures.Count)."

    $expectedNames = @('OneSublevel', 'ParallelSublevels', 'ThreeDeep', 'InheritedPrimaryKey')
    $actualNames = @($fixtures | ForEach-Object { [string](Get-Prop $_ 'Name') })
    foreach ($expectedName in $expectedNames) {
        Assert-True ($actualNames -contains $expectedName) "Fixture ausente: $expectedName"
    }

    $byName = @{}
    foreach ($fixture in $fixtures) {
        $byName[[string](Get-Prop $fixture 'Name')] = $fixture
    }

    $one = Get-Prop $byName['OneSublevel'] 'Snapshot'
    $oneFlat = @($flattenMethod.Invoke($one, @()))
    Assert-True (([int](Get-Prop $one 'MaxDepth')) -eq 2) 'OneSublevel MaxDepth=2.'
    Assert-True ($oneFlat.Count -eq 2) 'OneSublevel deve ter 2 níveis.'
    $linesLevel = Find-LevelByName $oneFlat 'Lines'
    $lineId = Find-FieldByName (Get-Prop $linesLevel 'Fields') 'LineId'
    $lineTotal = Find-FieldByName (Get-Prop $linesLevel 'Fields') 'LineTotal'
    $lineStamp = Find-FieldByName (Get-Prop $linesLevel 'Fields') 'LineStamp'
    Assert-True (-not [bool](Get-Prop $lineId 'IsAutonumber')) 'LineId com Autonumber=False deve sair não autonumerado do Build.'
    Assert-True ([bool](Get-Prop $lineTotal 'IsFormula')) 'LineTotal fórmula deve sair do Build.'
    Assert-True ([bool](Get-Prop $lineStamp 'IsNoAccept')) 'LineStamp NoAccept deve sair do Build.'

    $parallel = Get-Prop $byName['ParallelSublevels'] 'Snapshot'
    $parallelFlat = @($flattenMethod.Invoke($parallel, @()))
    Assert-True ($parallelFlat.Count -eq 3) 'ParallelSublevels: raiz + 2 irmãos.'
    $notes = Find-LevelByName $parallelFlat 'Notes'
    $tags = Find-LevelByName $parallelFlat 'Tags'
    Assert-True ([int](Get-Prop $notes 'LevelOrder') -eq 1) 'Notes LevelOrder=1 (índice do filho no núcleo).'
    Assert-True ([int](Get-Prop $tags 'LevelOrder') -eq 2) 'Tags LevelOrder=2.'

    $three = Get-Prop $byName['ThreeDeep'] 'Snapshot'
    $threeFlat = @($flattenMethod.Invoke($three, @()))
    Assert-True (([int](Get-Prop $three 'MaxDepth')) -eq 3) 'ThreeDeep MaxDepth=3.'
    $worker = Find-LevelByName $threeFlat 'Worker'
    $workerId = Find-FieldByName (Get-Prop $worker 'Fields') 'WorkerId'
    $workerScore = Find-FieldByName (Get-Prop $worker 'Fields') 'WorkerScore'
    Assert-True ([bool](Get-Prop $workerId 'IsAutonumber')) 'WorkerId Autonumber=True deve sair autonumerado do Build/helper.'
    Assert-True ([bool](Get-Prop $workerScore 'IsFormula')) 'WorkerScore fórmula.'

    $inherited = Get-Prop $byName['InheritedPrimaryKey'] 'Snapshot'
    $inheritedFlat = @($flattenMethod.Invoke($inherited, @()))
    $unnamed = Find-LevelByName $inheritedFlat '<unnamed>'
    Assert-True ($null -ne $unnamed) 'Nível sem nome deve virar <unnamed>.'
    $pk = @(Get-Prop $unnamed 'PrimaryKey')
    Assert-True ($pk.Count -eq 2) 'PK herdada deve ter 2 partes.'
    Assert-True ([string](Get-Prop $pk[0] 'Name') -eq 'HeaderId') 'Ordem da PK: HeaderId primeiro.'
    Assert-True ([string](Get-Prop $pk[1] 'Name') -eq 'LineId') 'Ordem da PK: LineId segundo.'
    Assert-True (-not [bool](Get-Prop $pk[0] 'IsAutonumber')) 'PK composta: HeaderId não autonumerado.'
    Assert-True (-not [bool](Get-Prop $pk[1] 'IsAutonumber')) 'PK composta: LineId não autonumerado.'

    $divergences = [System.Collections.Generic.List[string]]::new()
    foreach ($fixture in $fixtures) {
        $fixtureName = [string](Get-Prop $fixture 'Name')
        $snapshot = Get-Prop $fixture 'Snapshot'
        $actual = [string]$normalize.Invoke($null, @([string]$serialize.Invoke($null, @($snapshot))))
        if (-not $actual.EndsWith("`n")) {
            $actual += "`n"
        }

        $referencePath = Join-Path $baselinesRoot ($fixtureName + '.json')
        if ($UpdateBaselines) {
            Write-Utf8LfFile -Path $referencePath -Content $actual
            continue
        }

        if (-not (Test-Path -LiteralPath $referencePath -PathType Leaf)) {
            $divergences.Add("Arquivo de referência ausente: $referencePath")
            continue
        }

        $expected = [System.IO.File]::ReadAllText($referencePath).Replace("`r`n", "`n").Replace("`r", "`n")
        if (-not $expected.EndsWith("`n")) {
            $expected += "`n"
        }

        if ($actual -cne $expected) {
            $divergences.Add("Divergência em $fixtureName.json")
        }
    }

    if ($UpdateBaselines) {
        Write-Output "UPDATED: baselines B095 em $baselinesRoot"
        exit 0
    }

    if ($divergences.Count -gt 0) {
        throw ("ASSERT_BASELINE_FAILED:`n" + ($divergences -join "`n"))
    }

    Write-Output 'PASS: TransactionStructureReader'
}
finally {
    if ($null -ne $script:AssemblyResolveHandler) {
        [System.AppDomain]::CurrentDomain.remove_AssemblyResolve($script:AssemblyResolveHandler)
    }
}
