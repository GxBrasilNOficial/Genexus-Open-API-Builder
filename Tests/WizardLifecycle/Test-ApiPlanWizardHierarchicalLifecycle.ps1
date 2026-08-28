#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

function Assert-Contains {
    param([string]$Text, [string]$Needle, [string]$Message)
    if ($Text.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "ASSERT_CONTAINS_FAILED: $Message"
    }
}

$readerPath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\PrototypeWizardExistingApiContractReader.cs'
$dialogPath = Join-Path $repositoryRoot 'Src\Extension\PrototypeWizardDialog.cs'
$syncPath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\ApiPlanTransactionSyncOrchestrator.cs'
$inventoryPath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\ApiPlanGeneratedApiRemovalInventory.cs'
$preferencesPath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\PrototypeWizardPreferencesCodec.cs'

foreach ($path in @($readerPath, $dialogPath, $syncPath, $inventoryPath, $preferencesPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "SOURCE_MISSING: $path"
    }
}

$reader = [IO.File]::ReadAllText($readerPath)
$dialog = [IO.File]::ReadAllText($dialogPath)
$sync = [IO.File]::ReadAllText($syncPath)
$inventory = [IO.File]::ReadAllText($inventoryPath)
$preferences = [IO.File]::ReadAllText($preferencesPath)

Assert-Contains $reader 'PersistedHierarchicalRoot' 'Contrato existente deve expor levels persistidos.'
Assert-Contains $reader 'ApiPlanMetadataLevelsCodec.HasHierarchicalLevels' 'Reader deve detectar metadata hierárquica.'
Assert-Contains $reader 'ApiPlanMetadataLevelsCodec.TryReadRoot' 'Reader deve reler a árvore da metadata.'
Assert-Contains $dialog 'PersistedHierarchicalRoot' 'Wizard deve consultar levels persistidos.'
Assert-Contains $dialog 'ApplyPersistedPrune' 'Wizard deve restaurar seleção hierárquica no reencontro.'
Assert-Contains $sync 'HasHierarchicalLevels(metadata)' 'Sync deve pular conflito flat quando há levels.'
Assert-Contains $inventory 'ResolveOwnSdtNames' 'Inventário dinâmico deve resolver SDTs próprios.'
Assert-Contains $inventory 'TryCreateStubApiPlanFromMetadata' 'Inventário dinâmico deve reconstruir plano a partir da metadata.'
Assert-Contains $preferences 'SupportedSchemaVersions' 'Preferências devem declarar schemas suportados.'
Assert-Contains $preferences 'IsSupportedSchemaVersion' 'Preferências devem tolerar schema legado/ausente.'

Write-Output 'PASS: ApiPlanWizardHierarchicalLifecycle'
