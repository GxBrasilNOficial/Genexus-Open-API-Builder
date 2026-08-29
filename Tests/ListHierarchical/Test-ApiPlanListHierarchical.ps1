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
$listWriterPath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\ApiPlanListProcedureWriter.cs'
$contractPath = Join-Path $repositoryRoot 'Src\Domain\ApiPlanListHierarchicalContract.cs'
$planPath = Join-Path $repositoryRoot 'Src\Domain\ApiPlanSdtGenerationPlan.cs'
$namingPath = Join-Path $repositoryRoot 'Src\Domain\ApiPlanSdtHierarchicalNaming.cs'

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

Assert-True (Test-Path -LiteralPath $listWriterPath -PathType Leaf) "Fonte ausente: $listWriterPath"
Assert-True (Test-Path -LiteralPath $contractPath -PathType Leaf) "Fonte ausente: $contractPath"
Assert-True (Test-Path -LiteralPath $planPath -PathType Leaf) "Fonte ausente: $planPath"
Assert-True (Test-Path -LiteralPath $namingPath -PathType Leaf) "Fonte ausente: $namingPath"

$listWriterSource = [IO.File]::ReadAllText($listWriterPath)
$contractSource = [IO.File]::ReadAllText($contractPath)
$planSource = [IO.File]::ReadAllText($planPath)
$namingSource = [IO.File]::ReadAllText($namingPath)

Assert-Contains $listWriterSource 'ResolveListCountAssignments' 'List deve emitir contadores hierarquicos.'
Assert-Contains $listWriterSource 'count(' 'List deve usar formula agregada count().'
Assert-Contains $listWriterSource 'ResolveListItemSdtName' 'List deve tipar &Item conforme hierarquia.'
Assert-Contains $contractSource 'AllocateCountMemberName' 'Contrato List deve alocar membros Count.'
Assert-Contains $planSource 'ListResponse_Item' 'Plano de SDT deve emitir ListResponse_Item.'
Assert-Contains $namingSource 'ListResponseItemNamePattern' 'Naming deve declarar padrao ListResponse_Item.'

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
    $baselineType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanListHierarchicalBaseline', $true, $false)
    $fixtureType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanSdtHierarchicalPlanFixture', $true, $false)

    $createFixtures = $baselineType.GetMethod('CreateFixtures', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $capture = $baselineType.GetMethod('Capture', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $serialize = $baselineType.GetMethod('Serialize', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $normalize = $baselineType.GetMethod('NormalizeForComparison', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $assertContract = $baselineType.GetMethod('AssertContractMatchesSdtPlan', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $nameProperty = $fixtureType.GetProperty('Name', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $planProperty = $fixtureType.GetProperty('Plan', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')

    Assert-True ($null -ne $createFixtures) 'CreateFixtures nao encontrado.'
    Assert-True ($null -ne $capture) 'Capture nao encontrado.'
    Assert-True ($null -ne $serialize) 'Serialize nao encontrado.'
    Assert-True ($null -ne $normalize) 'NormalizeForComparison nao encontrado.'
    Assert-True ($null -ne $assertContract) 'AssertContractMatchesSdtPlan nao encontrado.'

    $fixtures = @($createFixtures.Invoke($null, @()))
    Assert-True ($fixtures.Count -ge 6) "Esperava pelo menos 6 fixtures; encontrado $($fixtures.Count)."

    $expectedNames = @('OneSublevel', 'ParallelSublevels', 'ThreeDeep', 'InheritedPrimaryKey', 'MemberCollision', 'LongQualifier', 'VariableTokenCollision', 'HeaderOnly', 'CountsDisabled', 'ExclusiveCreateEmpty')
    $actualNames = @($fixtures | ForEach-Object { $nameProperty.GetValue($_) })
    foreach ($expectedName in $expectedNames) {
        Assert-True ($actualNames -contains $expectedName) "Fixture ausente: $expectedName"
    }

    $divergences = [System.Collections.Generic.List[string]]::new()
    foreach ($fixture in $fixtures) {
        $fixtureName = [string]$nameProperty.GetValue($fixture)
        $plan = $planProperty.GetValue($fixture)
        $assertContract.Invoke($null, @($plan))
        $snapshot = $capture.Invoke($null, @($plan))
        $payload = [string]$normalize.Invoke($null, @([string]$serialize.Invoke($null, @($snapshot))))
        $listSource = [string]$normalize.Invoke($null, @([string]($snapshot.GetType().GetProperty('ListSource').GetValue($snapshot))))
        $itemSdt = [string]($snapshot.GetType().GetProperty('ItemVariableSdtName').GetValue($snapshot))

        if ($fixtureName -eq 'HeaderOnly') {
            Assert-True ($listSource.IndexOf('count(', [StringComparison]::Ordinal) -lt 0) 'HeaderOnly nao deve emitir count().'
            Assert-True ($itemSdt.IndexOf('ListResponse_Item', [StringComparison]::Ordinal) -lt 0) 'HeaderOnly deve tipar &Item como Response.'
        }
        else {
            Assert-True ($itemSdt.IndexOf('ListResponse_Item', [StringComparison]::Ordinal) -ge 0) "$fixtureName deve tipar &Item como ListResponse_Item."
        }

        if ($fixtureName -eq 'OneSublevel') {
            Assert-Contains $listSource 'count(LineId)' 'OneSublevel deve agregar LineId.'
            Assert-Contains $listSource '&Item.LinesCount' 'OneSublevel deve preencher LinesCount.'
        }

        if ($fixtureName -eq 'InheritedPrimaryKey') {
            Assert-Contains $listSource 'count(LineId)' 'InheritedPrimaryKey deve agregar a PK propria (LineId), nao a FK do pai.'
            Assert-True ($listSource.IndexOf('count(HeaderId)', [StringComparison]::Ordinal) -lt 0) 'InheritedPrimaryKey nao deve emitir count(HeaderId).'
        }

        if ($fixtureName -eq 'ExclusiveCreateEmpty') {
            Assert-Contains $listSource 'ExclusiveCount' 'ExclusiveCreateEmpty deve contar o subnivel mesmo sem Create.'
            Assert-Contains $listSource 'count(FirmId)' 'ExclusiveCreateEmpty agrega a unica PK do filho (FirmId herdado).'
        }

        if ($fixtureName -eq 'ThreeDeep') {
            Assert-Contains $listSource 'ShiftCount' 'ThreeDeep deve contar apenas o subnivel direto.'
            Assert-True ($listSource.IndexOf('WorkerCount', [StringComparison]::Ordinal) -lt 0) 'ThreeDeep nao deve emitir contador de neto.'
        }

        if ($fixtureName -eq 'CountsDisabled') {
            Assert-True ($listSource.IndexOf('count(', [StringComparison]::Ordinal) -lt 0) 'CountsDisabled nao deve emitir count().'
            Assert-True ($itemSdt.IndexOf('ListResponse_Item', [StringComparison]::Ordinal) -ge 0) 'CountsDisabled ainda usa ListResponse_Item.'
        }

        if ($fixtureName -eq 'MemberCollision') {
            Assert-Contains $listSource 'Notes1Count' 'MemberCollision deve desambiguar o contador.'
        }

        $referencePath = Join-Path $baselinesRoot ($fixtureName + '.txt')
        if ($UpdateBaselines) {
            Write-Utf8LfFile -Path $referencePath -Content $payload
            continue
        }

        if (-not (Test-Path -LiteralPath $referencePath -PathType Leaf)) {
            $divergences.Add("Arquivo de referencia ausente: $referencePath")
            continue
        }

        $actual = $payload
        if (-not $actual.EndsWith("`n")) {
            $actual += "`n"
        }

        $expected = [System.IO.File]::ReadAllText($referencePath).Replace("`r`n", "`n").Replace("`r", "`n")
        if (-not $expected.EndsWith("`n")) {
            $expected += "`n"
        }

        if ($actual -cne $expected) {
            $divergences.Add("Divergencia em $fixtureName")
        }
    }

    if ($UpdateBaselines) {
        Write-Output "UPDATED: linha de base B098 gravada em $baselinesRoot"
        exit 0
    }

    if ($divergences.Count -gt 0) {
        throw ("ASSERT_BASELINE_FAILED:`n" + ($divergences -join "`n"))
    }

    Write-Output 'PASS: ApiPlanListHierarchicalBaseline'
}
finally {
    if ($null -ne $script:AssemblyResolveHandler) {
        [System.AppDomain]::CurrentDomain.remove_AssemblyResolve($script:AssemblyResolveHandler)
    }
}
