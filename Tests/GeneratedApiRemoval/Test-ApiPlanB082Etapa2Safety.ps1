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

# --- 1. Guarda de operação única nos quatro handlers ----------------------------------------
Assert-Contains $guard 'internal static bool TryEnter(string operationName)' 'A guarda deve expor TryEnter.'
Assert-Contains $guard 'internal static void Exit()' 'A guarda deve expor Exit.'
foreach ($handler in @(
        'ExecuteConfigureWizardPreferences',
        'ExecuteSynchronizeWithTransaction',
        'ExecuteRemoveGeneratedApi',
        'ExecuteOpenWizardStepOne')) {
    Assert-Contains $package "ExtensionOperationGuard.TryEnter(" "O handler $handler deve adquirir a guarda."
}

Assert-Contains $package 'ExecuteConfigureWizardPreferencesCore(' 'Preferências deve liberar a guarda via Core+finally.'
Assert-Contains $package 'ExecuteSynchronizeWithTransactionCore(' 'Sync deve liberar a guarda via Core+finally.'
Assert-Contains $package 'ExecuteRemoveGeneratedApiCore(' 'Remover deve liberar a guarda via Core+finally.'
Assert-Contains $package 'ExecuteOpenWizardStepOneCore(' 'Wizard deve liberar a guarda via Core+finally.'
Assert-Contains $localization "Operação recusada: já há '" 'A mensagem da guarda deve estar no catálogo de Output.'

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

Write-Output 'PASS: ApiPlanB082Etapa2Safety'
