#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# B082 Etapa 2 — contratos textuais (offline): guarda, abort, Preview→Remove, Folder D10.

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

function Read-Source {
    param([string]$RelativePath)
    $path = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "SOURCE_MISSING: $RelativePath"
    }
    return [IO.File]::ReadAllText($path)
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

function Assert-Order {
    param([string]$Text, [string]$First, [string]$Second, [string]$Message)
    $firstIndex = $Text.IndexOf($First, [StringComparison]::Ordinal)
    $secondIndex = $Text.IndexOf($Second, [StringComparison]::Ordinal)
    if ($firstIndex -lt 0 -or $secondIndex -lt 0 -or $firstIndex -ge $secondIndex) {
        throw "ASSERT_ORDER_FAILED: $Message"
    }
}

$package = Read-Source 'Src\Extension\Package.cs'
$remover = Read-Source 'Src\Extension\Diagnostics\ApiPlanGeneratedApiRemover.cs'
$dialog = Read-Source 'Src\Extension\ExtensionBusyProgressDialog.cs'
$guard = Read-Source 'Src\Extension\ExtensionOperationGuard.cs'
$folder = Read-Source 'Src\Extension\Diagnostics\ApiPlanTransactionFolder.cs'
$plan = Read-Source 'Src\Extension\Diagnostics\ApiPlanGeneratedApiRemovalPlan.cs'
$saveExecutor = Read-Source 'Src\Extension\Diagnostics\ApiPlanSaveStepExecutor.cs'
$localization = Read-Source 'Src\Domain\ExtensionOutputLocalization.cs'

# --- 1. Guarda de operação única nos cinco handlers longos ----------------------------------
Assert-Contains $guard 'internal static bool TryEnter(string operationName)' 'A guarda deve expor TryEnter.'
Assert-Contains $guard 'internal static void Exit()' 'A guarda deve expor Exit.'
Assert-Contains $package 'TryEnter("Configurar Preferências do Wizard")' 'Preferências deve disputar a guarda.'
Assert-Contains $package 'TryEnter("Sincronizar com a Transaction")' 'Sync deve disputar a guarda.'
Assert-Contains $package 'TryEnter("Remover API gerada")' 'Remover deve disputar a guarda.'
Assert-Contains $package 'TryEnter("Wizard")' 'Wizard deve disputar a guarda.'
Assert-Contains $package 'TryEnter("Recuperar operação interrompida")' 'Recuperar (menu) deve disputar a guarda.'
if ([regex]::Matches($package, 'ExtensionOperationGuard\.TryEnter\(').Count -ne 5) {
    throw 'ASSERT_FAILED: Os cinco handlers longos devem ser os únicos TryEnter da guarda.'
}

Assert-Contains $package 'ExecuteConfigureWizardPreferencesCore(' 'Preferências deve liberar a guarda via Core+finally.'
Assert-Contains $package 'ExecuteSynchronizeWithTransactionCore(' 'Sync deve liberar a guarda via Core+finally.'
Assert-Contains $package 'ExecuteRemoveGeneratedApiCore(' 'Remover deve liberar a guarda via Core+finally.'
Assert-Contains $package 'ExecuteOpenWizardStepOneCore(' 'Wizard deve liberar a guarda via Core+finally.'
Assert-Contains $package 'ExecuteRecoverInterruptedOperationCore(' 'Recuperar deve liberar a guarda via Core+finally.'
Assert-Contains $package 'OfferRecoveryAfterJournalBlock(' 'A oferta proativa após bloqueio do diário deve continuar existindo.'
Assert-Contains $package 'RunRecovery(knowledgeBase, texts, owner);' 'A oferta proativa chama RunRecovery direto (sem TryEnter próprio).'
Assert-Contains $localization "Operação recusada: já há '" 'A mensagem da guarda deve estar no catálogo de Output.'

$offerStart = $package.IndexOf('private static void OfferRecoveryAfterJournalBlock(', [StringComparison]::Ordinal)
$offerEnd = $package.IndexOf('private static bool QueryRecoverInterruptedOperationPortuguese(', [StringComparison]::Ordinal)
if ($offerStart -lt 0 -or $offerEnd -le $offerStart) {
    throw 'ASSERT_FAILED: Não localizei OfferRecoveryAfterJournalBlock para auditar MessageBox.'
}
$offerBody = $package.Substring($offerStart, $offerEnd - $offerStart)
Assert-Contains $offerBody 'ExtensionRecoveryDialog.Ask(' 'A oferta proativa deve usar ExtensionRecoveryDialog.'
Assert-NotContains $offerBody 'MessageBox.Show' 'A oferta proativa não deve mais usar MessageBox nativo.'

$runStart = $package.IndexOf('private static void RunRecovery(', [StringComparison]::Ordinal)
$runEnd = $package.IndexOf('private static string DescribeRemovalGuidance(', [StringComparison]::Ordinal)
if ($runStart -lt 0 -or $runEnd -le $runStart) {
    throw 'ASSERT_FAILED: Não localizei RunRecovery para auditar MessageBox.'
}
$runBody = $package.Substring($runStart, $runEnd - $runStart)
Assert-NotContains $runBody 'MessageBox.Show' 'RunRecovery não deve mais usar MessageBox nativo.'
Assert-Contains $runBody 'ExtensionRecoveryDialog.Inform(' 'RunRecovery deve informar via ExtensionRecoveryDialog.'

# --- 2. Abort: Report → ThrowIfAbort → mutar; sem DoEvents no clique -------------------------
$executeStart = $remover.IndexOf('internal static ApiPlanGeneratedApiRemovalResult Execute(', [StringComparison]::Ordinal)
Assert-Contains ($remover.Substring($executeStart)) 'progress?.Report("Removendo ' 'A fila deve reportar antes de mutar.'
Assert-Order ($remover.Substring($executeStart, 2500)) `
    'progress?.Report("Removendo ' `
    'progress?.ThrowIfAbortRequested();' `
    'No Remove, Report deve preceder ThrowIfAbort (D3).'
Assert-Order ($remover.Substring($executeStart, 2500)) `
    'progress?.ThrowIfAbortRequested();' `
    'var result = Attempt(' `
    'ThrowIfAbort deve preceder Attempt/Delete.'

$abortClickStart = $dialog.IndexOf('private void OnAbortClicked(', [StringComparison]::Ordinal)
Assert-Contains ($dialog.Substring([Math]::Max(0, $abortClickStart))) 'private void OnAbortClicked(' 'OnAbortClicked deve existir.'
$abortClickBody = $dialog.Substring($abortClickStart, [Math]::Min(600, $dialog.Length - $abortClickStart))
Assert-NotContains $abortClickBody 'Application.DoEvents()' 'OnAbortClicked não deve chamar DoEvents (D5).'

Assert-Order $saveExecutor `
    'progress?.Report(step.Stage, saveIndex, materialized.Length, step.Label);' `
    'progress?.ThrowIfAbortRequested();' `
    'No SaveStepExecutor, Report deve preceder ThrowIfAbort.'
Assert-Order $saveExecutor `
    'progress?.ThrowIfAbortRequested();' `
    'step.Prepare();' `
    'ThrowIfAbort deve preceder a mutação Save.'

# --- 3. Preview→Remove por referência (decisão 7) --------------------------------------------
Assert-Contains $plan 'AttachPreviewCapture(' 'O plano deve aceitar a captura do Preview.'
Assert-Contains $remover 'plan.AttachPreviewCapture(CapturePreviewIdentities(' 'O Preview deve anexar a captura na mesma instância.'
Assert-Contains $remover 'folderDescriptionMatchesOwned' 'Preview deve capturar casamento de Description do Folder (B126).'
Assert-Contains $remover 'folderInExpectedContainer' 'Preview deve capturar contêiner esperado do Folder (B126).'
Assert-Contains $plan 'FolderPreservedAtPreviewForSafety' 'Plano deve expor preservação B126 no Preview.'
Assert-Contains $plan 'BuildFolderConfirmationLine(' 'Confirmação do Folder deve passar por linha compartilhada.'

Assert-Contains $remover 'ApiPlanGeneratedApiRemovalPlan confirmedPlan' 'ResolveIntent deve receber o plano confirmado.'
Assert-Contains $remover 'AssertPreviewStillMatches(' 'ResolveIntent deve conferir identidades do Preview.'
Assert-Contains $remover 'BuildPreviewDivergence(' 'Divergência deve bloquear com mensagem própria.'
Assert-Contains $remover 'Nenhuma exclusão foi feita.' 'Divergência declara zero exclusões.'
Assert-Contains $package 'ResolveIntent(
                        knowledgeBase.DesignModel,
                        transaction,
                        plan,' 'O comando deve passar a mesma instância do Preview.'
Assert-NotContains $remover 'FromMetadata(metadata, transaction.Name, transaction.Guid.ToString());
        telemetry.MarkPhase("ResolucaoMetadata"' 'ResolveIntent não deve reconstruir o plano a partir do File corrente.'

# --- 4. Folder D10: contêiner + GUID; sem IsReusable do Apply --------------------------------
Assert-Contains $folder 'internal static bool IsInExpectedContainer(' 'IsInExpectedContainer deve ser compartilhável.'
Assert-Contains $remover 'ApiPlanTransactionFolder.IsInExpectedContainer(folder, transaction)' 'DeleteOwnFolder deve exigir o contêiner esperado.'
Assert-Contains $remover 'target.Guid.HasValue && target.Guid.Value != Guid.Empty && folder.Guid != target.Guid.Value' 'DeleteOwnFolder deve conferir o GUID do Preview.'
Assert-NotContains ($remover.Substring($remover.IndexOf('private static ApiPlanRemovalAttemptResult DeleteOwnFolder('))) `
    'IsReusableTransactionFolderDescription' `
    'DeleteOwnFolder não pode reutilizar a permissividade de Description vazia do Apply.'

# --- 5. IsFolderEmpty cobre qualquer filho (caso 6 / WebPanel), não só tipos da API ----------
Assert-Contains $remover '!folder.HasObjects && !folder.SubFolders.Any()' 'IsFolderEmpty deve usar HasObjects/SubFolders.'
Assert-NotContains $remover 'API.GetAll(designModel).Any(item => item.Parent is not null && item.Parent.Guid == folder.Guid)' `
    'IsFolderEmpty nao deve mais varrer API tipada para decidir vazio.'
Assert-NotContains $remover 'Procedure.GetAll(designModel).Any(item => item.Parent is not null && item.Parent.Guid == folder.Guid)' `
    'IsFolderEmpty nao deve mais varrer Procedure tipada para decidir vazio.'

Write-Output 'PASS: ApiPlanB082Etapa2Safety'
