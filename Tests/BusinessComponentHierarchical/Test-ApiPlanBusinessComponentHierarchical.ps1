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
$writerSourcePath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\ApiPlanBusinessComponentWriter.cs'
$hierarchicalSourcePath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\ApiPlanBusinessComponentHierarchicalSource.cs'
$mapSourcePath = Join-Path $repositoryRoot 'Src\Domain\ApiPlanHierarchicalContractMap.cs'

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

Assert-True (Test-Path -LiteralPath $writerSourcePath -PathType Leaf) "Fonte ausente: $writerSourcePath"
Assert-True (Test-Path -LiteralPath $hierarchicalSourcePath -PathType Leaf) "Fonte ausente: $hierarchicalSourcePath"
Assert-True (Test-Path -LiteralPath $mapSourcePath -PathType Leaf) "Fonte ausente: $mapSourcePath"

$writerSource = [IO.File]::ReadAllText($writerSourcePath)
$hierarchicalSource = [IO.File]::ReadAllText($hierarchicalSourcePath)
$mapSource = [IO.File]::ReadAllText($mapSourcePath)

Assert-Contains $writerSource 'HasSelectedSublevels' 'Writer BC deve ramificar no caminho hierarquico.'
Assert-Contains $writerSource 'EmitCreateCollectionAssignments' 'Create deve emitir colecoes hierarquicas.'
Assert-Contains $writerSource 'EmitUpdateCollectionAssignments' 'Update deve emitir Replace hierarquico.'
Assert-Contains $writerSource 'EmitGetCollectionAssignments' 'Get deve emitir colecoes hierarquicas.'
Assert-Contains $hierarchicalSource 'ReplaceMemberName' 'Emissor deve respeitar o marcador Replace.'
Assert-Contains $hierarchicalSource 'HasAutonumberPrimaryKey' 'Update autonumerado usa Clear+Add.'
Assert-Contains $mapSource 'ApiPlanHierarchicalContractMapBuilder' 'Mapa compartilhado B096/B097 deve existir.'

if (-not (Test-Path -LiteralPath $DllPath -PathType Leaf)) {
    Write-Output "ENVIRONMENT_BLOCKED: DLL Release ausente em $DllPath"
    exit 2
}

if (-not (Test-Path -LiteralPath $GeneXusDirectory -PathType Container)) {
    Write-Output "ENVIRONMENT_BLOCKED: Instalacao GeneXus nao encontrada em modo leitura: $GeneXusDirectory"
    exit 2
}

$script:AssemblyResolveHandler = $null
try {
    Initialize-GeneXusAssemblyResolver -SearchDirectories (Get-AssemblyDirectoryCandidates -GeneXusRoot $GeneXusDirectory)
    $assembly = [System.Reflection.Assembly]::LoadFrom($DllPath)
    $baselineType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanBusinessComponentHierarchicalBaseline', $true, $false)
    $fixtureType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanSdtHierarchicalPlanFixture', $true, $false)
    $snapshotType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanBusinessComponentHierarchicalSnapshot', $true, $false)

    $createFixtures = $baselineType.GetMethod('CreateFixtures', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $capture = $baselineType.GetMethod('Capture', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $normalize = $baselineType.GetMethod('NormalizeForComparison', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $assertMap = $baselineType.GetMethod('AssertMapMatchesSdtPlan', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $toFileMap = $snapshotType.GetMethod('ToFileMap', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $nameProperty = $fixtureType.GetProperty('Name', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $planProperty = $fixtureType.GetProperty('Plan', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')

    Assert-True ($null -ne $createFixtures) 'CreateFixtures nao encontrado.'
    Assert-True ($null -ne $capture) 'Capture nao encontrado.'
    Assert-True ($null -ne $normalize) 'NormalizeForComparison nao encontrado.'
    Assert-True ($null -ne $assertMap) 'AssertMapMatchesSdtPlan nao encontrado.'
    Assert-True ($null -ne $toFileMap) 'ToFileMap nao encontrado.'

    $fixtures = @($createFixtures.Invoke($null, @()))
    Assert-True ($fixtures.Count -ge 5) "Esperava pelo menos 5 fixtures; encontrado $($fixtures.Count)."

    $expectedNames = @('OneSublevel', 'ParallelSublevels', 'ThreeDeep', 'InheritedPrimaryKey', 'MemberCollision', 'HeaderOnly')
    $actualNames = @($fixtures | ForEach-Object { $nameProperty.GetValue($_) })
    foreach ($expectedName in $expectedNames) {
        Assert-True ($actualNames -contains $expectedName) "Fixture ausente: $expectedName"
    }

    $divergences = [System.Collections.Generic.List[string]]::new()
    foreach ($fixture in $fixtures) {
        $fixtureName = [string]$nameProperty.GetValue($fixture)
        $plan = $planProperty.GetValue($fixture)
        $assertMap.Invoke($null, @($plan))
        $snapshot = $capture.Invoke($null, @($plan))
        $fileMap = $toFileMap.Invoke($snapshot, @())
        $fixtureDirectory = Join-Path $baselinesRoot $fixtureName

        $getSource = [string]$normalize.Invoke($null, @([string]($snapshot.GetType().GetProperty('GetSource').GetValue($snapshot))))
        $createSource = [string]$normalize.Invoke($null, @([string]($snapshot.GetType().GetProperty('CreateSource').GetValue($snapshot))))
        $updateSource = [string]$normalize.Invoke($null, @([string]($snapshot.GetType().GetProperty('UpdateSource').GetValue($snapshot))))

        if ($fixtureName -eq 'HeaderOnly') {
            Assert-True ($getSource.IndexOf('For &Bc_', [StringComparison]::Ordinal) -lt 0) 'HeaderOnly nao deve emitir For hierarquico no Get.'
            Assert-True ($updateSource.IndexOf('Replace', [StringComparison]::Ordinal) -lt 0) 'HeaderOnly nao deve emitir Replace.'
        }
        else {
            Assert-True ($getSource.IndexOf('For &Bc_', [StringComparison]::Ordinal) -ge 0) "$fixtureName Get deve iterar colecoes BC."
            Assert-True ($createSource.IndexOf('For &Create_', [StringComparison]::Ordinal) -ge 0) "$fixtureName Create deve iterar request."
            Assert-True ($updateSource.IndexOf('Replace', [StringComparison]::Ordinal) -ge 0) "$fixtureName Update deve condicionar Replace."
        }

        if ($fixtureName -eq 'ThreeDeep') {
            Assert-True ($updateSource.IndexOf('ShiftReplace', [StringComparison]::Ordinal) -ge 0) 'ThreeDeep deve emitir ShiftReplace.'
            Assert-True ($updateSource.IndexOf('WorkerReplace', [StringComparison]::Ordinal) -ge 0) 'ThreeDeep deve emitir WorkerReplace aninhado.'
            Assert-True ($updateSource.IndexOf('.Clear()', [StringComparison]::Ordinal) -ge 0) 'Worker autonumerado deve usar Clear no Replace.'
        }

        if ($fixtureName -eq 'MemberCollision') {
            Assert-True ($createSource.IndexOf('Notes1', [StringComparison]::Ordinal) -ge 0) 'MemberCollision Create deve usar membro SDT desambiguado Notes1.'
            Assert-True ($createSource.IndexOf('.Notes.Add', [StringComparison]::Ordinal) -ge 0) 'MemberCollision Create deve Add no BC estrutural Notes.'
        }

        if ($UpdateBaselines) {
            foreach ($entry in $fileMap.GetEnumerator()) {
                $target = Join-Path $fixtureDirectory $entry.Key
                $content = [string]$normalize.Invoke($null, @([string]$entry.Value))
                Write-Utf8LfFile -Path $target -Content $content
            }

            continue
        }

        if (-not (Test-Path -LiteralPath $fixtureDirectory -PathType Container)) {
            $divergences.Add("Diretorio de referencia ausente: $fixtureDirectory")
            continue
        }

        foreach ($entry in $fileMap.GetEnumerator()) {
            $referencePath = Join-Path $fixtureDirectory $entry.Key
            if (-not (Test-Path -LiteralPath $referencePath -PathType Leaf)) {
                $divergences.Add("Arquivo de referencia ausente: $referencePath")
                continue
            }

            $actual = [string]$normalize.Invoke($null, @([string]$entry.Value))
            if (-not $actual.EndsWith("`n")) {
                $actual += "`n"
            }

            $expected = [System.IO.File]::ReadAllText($referencePath).Replace("`r`n", "`n").Replace("`r", "`n")
            if (-not $expected.EndsWith("`n")) {
                $expected += "`n"
            }

            if ($actual -cne $expected) {
                $divergences.Add("Divergencia em $fixtureName/$($entry.Key)")
            }
        }
    }

    if ($UpdateBaselines) {
        Write-Output "UPDATED: linha de base B097 gravada em $baselinesRoot"
        exit 0
    }

    if ($divergences.Count -gt 0) {
        throw ("ASSERT_BASELINE_FAILED:`n" + ($divergences -join "`n"))
    }

    Write-Output 'PASS: ApiPlanBusinessComponentHierarchicalBaseline'
}
finally {
    if ($null -ne $script:AssemblyResolveHandler) {
        [System.AppDomain]::CurrentDomain.remove_AssemblyResolve($script:AssemblyResolveHandler)
    }
}
