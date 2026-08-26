#requires -Version 7.4

[CmdletBinding()]
param(
    [string]$DllPath = '',
    [string]$GeneXusDirectory = 'C:\Program Files (x86)\GeneXus\GeneXus18'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$readerSourcePath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\TransactionStructureReader.cs'
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

Assert-True (Test-Path -LiteralPath $readerSourcePath -PathType Leaf) "Fonte ausente: $readerSourcePath"
Assert-True (Test-Path -LiteralPath $domainSourcePath -PathType Leaf) "Fonte ausente: $domainSourcePath"
Assert-True (Test-Path -LiteralPath $contractSourcePath -PathType Leaf) "Fonte ausente: $contractSourcePath"

$readerSource = [IO.File]::ReadAllText($readerSourcePath)
$domainSource = [IO.File]::ReadAllText($domainSourcePath)
$contractSource = [IO.File]::ReadAllText($contractSourcePath)

Assert-Contains $readerSource 'transaction.Structure' 'Leitor B095 deve partir de transaction.Structure.'
Assert-Contains $readerSource '.Root' 'Leitor B095 deve ler Structure.Root.'
Assert-Contains $readerSource '.Levels' 'Leitor B095 deve navegar Levels recursivamente.'
Assert-Contains $readerSource 'parentLevelName: string.Empty' 'Cabeçalho deve ter ParentLevelName vazio.'
Assert-Contains $readerSource 'PrototypeWizardNoAcceptRuleReader' 'Leitor B095 deve reutilizar a leitura de NoAccept.'
Assert-Contains $domainSource 'class ApiPlanLevel' 'Modelo ApiPlanLevel deve existir.'
Assert-Contains $domainSource 'class ApiPlanLevelField' 'Modelo ApiPlanLevelField deve existir.'
Assert-Contains $domainSource 'IReadOnlyList<ApiPlanLevel> Levels' 'ApiPlan deve expor Levels.'
Assert-NotContains $contractSource 'TransactionStructureReader' 'Wizard flat não deve acoplar o leitor hierárquico B095.'

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

    $script:AssemblySearchDirectories = $SearchDirectories
    $script:AssemblyResolveHandler = [System.ResolveEventHandler]{
        param($sender, $args)
        $requestedName = New-Object System.Reflection.AssemblyName($args.Name)
        $simpleName = $requestedName.Name
        foreach ($directory in $script:AssemblySearchDirectories) {
            $candidate = Join-Path $directory ($simpleName + '.dll')
            if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                return [System.Reflection.Assembly]::LoadFrom($candidate)
            }
        }

        return $null
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
    $fixtureType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.TransactionStructureFixture', $true, $false)
    $snapshotType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.TransactionStructureSnapshot', $true, $false)

    $createFixtures = $readerType.GetMethod('CreateFixtures', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    Assert-True ($null -ne $createFixtures) 'CreateFixtures não encontrado.'

    $fixtures = @($createFixtures.Invoke($null, @()))
    Assert-True ($fixtures.Count -eq 3) "Esperava 3 fixtures; encontrado $($fixtures.Count)."

    $expectedNames = @('OneSublevel', 'ParallelSublevels', 'ThreeDeep')
    $actualNames = @($fixtures | ForEach-Object { [string](Get-Prop $_ 'Name') })
    foreach ($expectedName in $expectedNames) {
        Assert-True ($actualNames -contains $expectedName) "Fixture ausente: $expectedName"
    }

    $byName = @{}
    foreach ($fixture in $fixtures) {
        $byName[[string](Get-Prop $fixture 'Name')] = $fixture
    }

    $flattenMethod = $snapshotType.GetMethod('FlattenLevels', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    Assert-True ($null -ne $flattenMethod) 'FlattenLevels não encontrado.'

    # OneSublevel: Order -> Lines; PK informada; fórmula; NoAccept.
    $one = Get-Prop $byName['OneSublevel'] 'Snapshot'
    Assert-True (([int](Get-Prop $one 'MaxDepth')) -eq 2) 'OneSublevel MaxDepth deve ser 2.'
    $oneFlat = @($flattenMethod.Invoke($one, @()))
    Assert-True ($oneFlat.Count -eq 2) 'OneSublevel deve ter 2 níveis.'
    $orderLevel = Find-LevelByName $oneFlat 'Order'
    $linesLevel = Find-LevelByName $oneFlat 'Lines'
    Assert-True ($null -ne $orderLevel) 'Nível Order ausente.'
    Assert-True ($null -ne $linesLevel) 'Nível Lines ausente.'
    Assert-True ([string](Get-Prop $orderLevel 'ParentLevelName') -eq '') 'Cabeçalho Order deve ter pai vazio.'
    Assert-True ([int](Get-Prop $orderLevel 'Depth') -eq 1) 'Order Depth=1.'
    Assert-True ([string](Get-Prop $linesLevel 'ParentLevelName') -eq 'Order') 'Lines ParentLevelName=Order.'
    Assert-True ([int](Get-Prop $linesLevel 'Depth') -eq 2) 'Lines Depth=2.'
    $lineId = Find-FieldByName (Get-Prop $linesLevel 'Fields') 'LineId'
    $lineTotal = Find-FieldByName (Get-Prop $linesLevel 'Fields') 'LineTotal'
    $lineStamp = Find-FieldByName (Get-Prop $linesLevel 'Fields') 'LineStamp'
    Assert-True ($null -ne $lineId) 'LineId ausente.'
    Assert-True ([bool](Get-Prop $lineId 'IsPrimaryKey')) 'LineId deve ser PK.'
    Assert-True (-not [bool](Get-Prop $lineId 'IsAutonumber')) 'LineId deve ser chave informada (não autonumerada).'
    Assert-True ([bool](Get-Prop $lineTotal 'IsFormula')) 'LineTotal deve ser fórmula.'
    Assert-True ([bool](Get-Prop $lineStamp 'IsNoAccept')) 'LineStamp deve ser NoAccept.'

    # ParallelSublevels: Document -> Notes + Tags.
    $parallel = Get-Prop $byName['ParallelSublevels'] 'Snapshot'
    Assert-True (([int](Get-Prop $parallel 'MaxDepth')) -eq 2) 'ParallelSublevels MaxDepth deve ser 2.'
    $parallelFlat = @($flattenMethod.Invoke($parallel, @()))
    Assert-True ($parallelFlat.Count -eq 3) 'ParallelSublevels deve ter 3 níveis (raiz + 2 irmãos).'
    $notes = Find-LevelByName $parallelFlat 'Notes'
    $tags = Find-LevelByName $parallelFlat 'Tags'
    Assert-True ($null -ne $notes) 'Notes ausente.'
    Assert-True ($null -ne $tags) 'Tags ausente.'
    Assert-True ([int](Get-Prop $notes 'LevelOrder') -eq 1) 'Notes LevelOrder=1.'
    Assert-True ([int](Get-Prop $tags 'LevelOrder') -eq 2) 'Tags LevelOrder=2.'
    Assert-True ([string](Get-Prop $notes 'ParentLevelName') -eq 'Document') 'Notes pai=Document.'
    Assert-True ([string](Get-Prop $tags 'ParentLevelName') -eq 'Document') 'Tags pai=Document.'
    $tagCode = Find-FieldByName (Get-Prop $tags 'Fields') 'TagCode'
    Assert-True ([bool](Get-Prop $tagCode 'IsNoAccept')) 'TagCode deve ser NoAccept.'

    # ThreeDeep: Day -> Shift -> Worker.
    $three = Get-Prop $byName['ThreeDeep'] 'Snapshot'
    Assert-True (([int](Get-Prop $three 'MaxDepth')) -eq 3) 'ThreeDeep MaxDepth deve ser 3.'
    $threeFlat = @($flattenMethod.Invoke($three, @()))
    Assert-True ($threeFlat.Count -eq 3) 'ThreeDeep deve ter 3 níveis.'
    $day = Find-LevelByName $threeFlat 'Day'
    $shift = Find-LevelByName $threeFlat 'Shift'
    $worker = Find-LevelByName $threeFlat 'Worker'
    Assert-True ($null -ne $day) 'Day ausente.'
    Assert-True ($null -ne $shift) 'Shift ausente.'
    Assert-True ($null -ne $worker) 'Worker ausente.'
    Assert-True ([string](Get-Prop $day 'ParentLevelName') -eq '') 'Day pai vazio.'
    Assert-True ([string](Get-Prop $shift 'ParentLevelName') -eq 'Day') 'Shift pai=Day.'
    Assert-True ([string](Get-Prop $worker 'ParentLevelName') -eq 'Shift') 'Worker pai=Shift.'
    Assert-True ([int](Get-Prop $worker 'Depth') -eq 3) 'Worker Depth=3.'
    $workerId = Find-FieldByName (Get-Prop $worker 'Fields') 'WorkerId'
    $workerScore = Find-FieldByName (Get-Prop $worker 'Fields') 'WorkerScore'
    Assert-True ([bool](Get-Prop $workerId 'IsAutonumber')) 'WorkerId deve ser autonumerado.'
    Assert-True ([bool](Get-Prop $workerScore 'IsFormula')) 'WorkerScore deve ser fórmula.'

    Write-Output 'PASS: TransactionStructureReader'
}
finally {
    if ($null -ne $script:AssemblyResolveHandler) {
        [System.AppDomain]::CurrentDomain.remove_AssemblyResolve($script:AssemblyResolveHandler)
    }
}
