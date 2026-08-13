#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Read-Source {
    param([string]$RelativePath)

    $path = Join-Path $PSScriptRoot "..\..\$RelativePath"
    if (-not (Test-Path -LiteralPath $path)) {
        throw "SOURCE_MISSING: $path"
    }

    return [IO.File]::ReadAllText($path)
}

function Assert-Contains {
    param([string]$Text, [string]$Expected, [string]$Message)

    if ($Text.IndexOf($Expected, [StringComparison]::Ordinal) -lt 0) {
        throw "ASSERT_CONTAINS_FAILED: $Message Expected='$Expected'"
    }
}

$contract = Read-Source 'Src\Extension\Diagnostics\PrototypeWizardContract.cs'
$apiPlan = Read-Source 'Src\Domain\ApiPlan.cs'
$sync = Read-Source 'Src\Extension\Diagnostics\ApiPlanTransactionSyncOrchestrator.cs'
$dialog = Read-Source 'Src\Extension\PrototypeWizardDialog.cs'
$contractDialog = Read-Source 'Src\Extension\PrototypeWizardContractDialog.cs'
$writer = Read-Source 'Src\Extension\Diagnostics\ApiPlanBusinessComponentWriter.cs'

Assert-Contains $contract 'PrototypeWizardNoAcceptRuleReader.ReadAttributeNames' 'Transaction Rules devem alimentar a classificação NoAccept.'
Assert-Contains $contract 'var isNoAccept = noAcceptAttributeNames.Contains(name)' 'A decisão de atributo deve preservar NoAccept.'
Assert-Contains $contract 'NoAccept torna o atributo somente leitura via BC' 'A justificativa de request deve explicar o bloqueio por NoAccept.'
Assert-Contains $contract 'public bool IsNoAccept { get; }' 'A decisão pública deve expor a marca NoAccept.'

Assert-Contains $dialog 'attribute.IsNoAccept' 'O wizard principal deve exibir a marca NoAccept.'
Assert-Contains $contractDialog 'attribute.IsNoAccept' 'O diálogo de contrato deve exibir a marca NoAccept.'

Assert-Contains $apiPlan 'CreateSelectedFields(contract.CreateFields, attributesByName, "CreateRequest")' 'CreateRequest deve aplicar a elegibilidade de escrita.'
Assert-Contains $apiPlan 'CreateSelectedFields(contract.UpdateFields, attributesByName, "UpdateRequest")' 'UpdateRequest deve aplicar a elegibilidade de escrita.'
Assert-Contains $apiPlan '"CreateRequest" => field.IsWritableByCreate' 'CreateRequest deve excluir campos não graváveis.'
Assert-Contains $apiPlan '"UpdateRequest" => field.IsWritableByUpdate' 'UpdateRequest deve excluir campos não graváveis.'
Assert-Contains $apiPlan 'IsSelectedRequestField' 'Required não pode sobreviver para campo removido do request.'

Assert-Contains $sync 'attribute.IsPayloadEligible' 'A sincronização deve revalidar campos existentes do CreateRequest.'
Assert-Contains $sync 'attribute.IsUpdatePayloadEligible' 'A sincronização deve revalidar campos existentes do UpdateRequest.'

Assert-Contains $writer 'plan.CreateRequestFields.Select' 'Create deve gerar assignments somente a partir dos campos filtrados.'
Assert-Contains $writer 'plan.UpdateRequestFields.Select' 'Update deve gerar assignments somente a partir dos campos filtrados.'

Write-Output 'PASS: NoAcceptRequestEligibilityContract'
