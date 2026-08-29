#requires -Version 7.4

[CmdletBinding()]
param(
    [string]$DllPath = '',
    [string]$GeneXusDirectory = 'C:\Program Files (x86)\GeneXus\GeneXus18'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$preflightSourcePath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\ApiPlanWritePreflight.cs'
$mapSourcePath = Join-Path $repositoryRoot 'Src\Domain\ApiPlanHierarchicalContractMap.cs'
$sdtWriterSourcePath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\ApiPlanSdtWriter.cs'

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

function Get-AssemblyDirectoryCandidates {
    param([string]$GeneXusRoot)

    $candidates = [System.Collections.Generic.List[string]]::new()
    foreach ($relative in @('Packages', 'GeneXusBlazorControls', '')) {
        $path = if ([string]::IsNullOrWhiteSpace($relative)) { $GeneXusRoot } else { Join-Path $GeneXusRoot $relative }
        if (Test-Path -LiteralPath $path -PathType Container) {
            [void]$candidates.Add($path)
        }
    }

    $dllDirectory = Split-Path -Parent $DllPath
    if (Test-Path -LiteralPath $dllDirectory -PathType Container) {
        [void]$candidates.Add($dllDirectory)
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

function Invoke-ExpectInvalidOperation {
    param(
        [scriptblock]$Action,
        [string]$Needle,
        [string]$Message
    )

    $threw = $false
    try {
        & $Action
    }
    catch [System.Reflection.TargetInvocationException] {
        $threw = $true
        $inner = $_.Exception.InnerException
        Assert-True ($null -ne $inner) "$Message (TargetInvocationException sem inner)."
        Assert-True ($inner -is [System.InvalidOperationException]) "$Message (inner nao e InvalidOperationException)."
        Assert-Contains $inner.Message $Needle $Message
    }
    catch [System.InvalidOperationException] {
        $threw = $true
        Assert-Contains $_.Exception.Message $Needle $Message
    }

    Assert-True $threw "$Message (nenhuma excecao lancada)."
}

Assert-True (Test-Path -LiteralPath $preflightSourcePath -PathType Leaf) "Fonte ausente: $preflightSourcePath"
Assert-True (Test-Path -LiteralPath $mapSourcePath -PathType Leaf) "Fonte ausente: $mapSourcePath"
Assert-True (Test-Path -LiteralPath $sdtWriterSourcePath -PathType Leaf) "Fonte ausente: $sdtWriterSourcePath"

$preflightSource = [IO.File]::ReadAllText($preflightSourcePath)
$mapSource = [IO.File]::ReadAllText($mapSourcePath)
$sdtWriterSource = [IO.File]::ReadAllText($sdtWriterSourcePath)

Assert-Contains $preflightSource 'ValidateStructuralSublevelNames(apiPlan)' 'Preflight agregado deve validar subnivel sem nome antes do primeiro Save().'
Assert-Contains $mapSource 'ValidateStructuralSublevelNames(ApiPlan apiPlan)' 'Validacao estrutural deve ser compartilhada com o mapa BC.'
Assert-Contains $mapSource 'eligible.Length == 0 && nested.Count == 0' 'Mapa BC deve pular filho sem membros no papel.'
Assert-Contains $sdtWriterSource 'nao tem membros' 'Preflight de SDT deve recusar definicao sem membros antes do Save().'

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

    $readerType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.TransactionStructureReader', $true, $false)
    $baselineType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanSdtHierarchicalPlanBaseline', $true, $false)
    $mapBuilderType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Domain.ApiPlanHierarchicalContractMapBuilder', $true, $false)
    $fixtureType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.TransactionStructureFixture', $true, $false)

    $createUnnamedReaderFixture = $readerType.GetMethod('CreateUnnamedSublevelReaderFixture', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $buildFromRoot = $baselineType.GetMethod('BuildFromRoot', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $validateStructural = $mapBuilderType.GetMethod('ValidateStructuralSublevelNames', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $createMap = $mapBuilderType.GetMethod('Create', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $snapshotProperty = $fixtureType.GetProperty('Snapshot', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $fixturePlanType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanSdtHierarchicalPlanFixture', $true, $false)
    $planProperty = $fixturePlanType.GetProperty('Plan', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')

    Assert-True ($null -ne $createUnnamedReaderFixture) 'CreateUnnamedSublevelReaderFixture nao encontrado.'
    Assert-True ($null -ne $buildFromRoot) 'BuildFromRoot nao encontrado.'
    Assert-True ($null -ne $validateStructural) 'ValidateStructuralSublevelNames nao encontrado.'
    Assert-True ($null -ne $createMap) 'Create mapa nao encontrado.'

    $unnamedReaderFixture = $createUnnamedReaderFixture.Invoke($null, @())
    $snapshot = $snapshotProperty.GetValue($unnamedReaderFixture)
    $transactionName = [string]$snapshot.GetType().GetProperty('TransactionName').GetValue($snapshot)
    $rootLevel = $snapshot.GetType().GetProperty('RootLevel').GetValue($snapshot)
    $unnamedPlanFixture = $buildFromRoot.Invoke($null, @('UnnamedSublevelApply', $transactionName, $rootLevel))
    $unnamedPlan = $planProperty.GetValue($unnamedPlanFixture)

    Invoke-ExpectInvalidOperation -Action { [void]$validateStructural.Invoke($null, @($unnamedPlan)) } -Needle 'subnivel sem nome estrutural' -Message 'ValidateStructuralSublevelNames deve bloquear subnivel <unnamed>.'
    Invoke-ExpectInvalidOperation -Action { [void]$createMap.Invoke($null, @($unnamedPlan)) } -Needle 'subnivel sem nome estrutural' -Message 'Mapa BC deve bloquear subnivel <unnamed>.'

    $readerFixtures = @($readerType.GetMethod('CreateFixtures', [System.Reflection.BindingFlags]'Static, NonPublic, Public').Invoke($null, @()))
    $inheritedFixture = @($readerFixtures | Where-Object { $_.GetType().GetProperty('Name').GetValue($_) -eq 'InheritedPrimaryKey' })[0]
    $inheritedSnapshot = $snapshotProperty.GetValue($inheritedFixture)
    $inheritedRoot = $inheritedSnapshot.GetType().GetProperty('RootLevel').GetValue($inheritedSnapshot)
    $inheritedPlanFixture = $buildFromRoot.Invoke($null, @('InheritedPrimaryKey', [string]$inheritedSnapshot.GetType().GetProperty('TransactionName').GetValue($inheritedSnapshot), $inheritedRoot))
    $inheritedPlan = $planProperty.GetValue($inheritedPlanFixture)

    [void]$validateStructural.Invoke($null, @($inheritedPlan))
    [void]$createMap.Invoke($null, @($inheritedPlan))
}
finally {
    if ($null -ne $script:AssemblyResolveHandler) {
        [System.AppDomain]::CurrentDomain.remove_AssemblyResolve($script:AssemblyResolveHandler)
        $script:AssemblyResolveHandler = $null
    }
}

Write-Output 'PASS: ApiPlanWritePreflightHierarchicalStructure'
