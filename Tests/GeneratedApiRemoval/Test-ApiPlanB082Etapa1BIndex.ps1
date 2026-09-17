#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# B082 Etapa 1B — contratos textuais (offline): índice Nível B no Remover.
# Confirmação pós-Delete permanece por leitura corrente; localização/revalidação usam o índice.

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

$index = Read-Source 'Src\Extension\Diagnostics\ApiPlanKbObjectNameIndex.cs'
$remover = Read-Source 'Src\Extension\Diagnostics\ApiPlanGeneratedApiRemover.cs'
$package = Read-Source 'Src\Extension\Package.cs'
$plan = Read-Source 'Docs\Implementation\2026-09-02-B082-PLANO-HARDENING-E-DESEMPENHO.md'

# --- 1. Contrato Nível B escrito no plano ----------------------------------------------------
Assert-Contains $plan '### Contrato Nível B — índice sob mutação (Remover / Etapa 1B)' 'O plano deve carregar o contrato Nível B antes do código.'
Assert-Contains $plan 'Confirmação pós-`Delete` via índice' 'O contrato deve proibir confirmação pós-Delete via índice.'

# --- 2. Índice: remoção pontual por GUID -----------------------------------------------------
Assert-Contains $index 'internal void ForgetRemovedApi(Guid objectGuid)' 'Índice deve expor ForgetRemovedApi.'
Assert-Contains $index 'internal void ForgetRemovedProcedure(Guid objectGuid)' 'Índice deve expor ForgetRemovedProcedure.'
Assert-Contains $index 'internal void ForgetRemovedSdt(Guid objectGuid)' 'Índice deve expor ForgetRemovedSdt.'
Assert-Contains $index 'internal void ForgetRemovedFile(Guid objectGuid)' 'Índice deve expor ForgetRemovedFile.'
Assert-Contains $index 'internal void ForgetRemovedFolder(Guid objectGuid)' 'Índice deve expor ForgetRemovedFolder.'
Assert-Contains $index 'FilterOutByGuid' 'Forget deve filtrar por GUID sem GetAll.'
Assert-NotContains $index 'private readonly ILookup<string, Procedure> _procedures' 'Procedures do índice não podem permanecer readonly na 1B.'
Assert-NotContains $index 'private readonly ILookup<string, API> _apis' 'APIs do índice não podem permanecer readonly na 1B.'
Assert-NotContains $index 'private readonly ILookup<string, WikiFileKBObject> _files' 'Files do índice não podem permanecer readonly na 1B.'

# --- 3. Remover: fila recebe o índice; Forget só após ausência -------------------------------
Assert-Contains $remover 'intent.KbIndex' 'Remove deve entregar o índice da intenção à fila.'
Assert-Contains $remover 'ConfirmDeleteAndForget(' 'Confirmação pós-Delete deve atualizar o índice só após ausência.'
Assert-Order $remover 'ConfirmDelete(stillExists, observedIdentity)' 'forgetFromIndex?.Invoke()' 'Forget só depois de ConfirmDelete.'
Assert-Contains $remover 'kbIndex?.ForgetRemovedProcedure(guid)' 'Procedure confirmada ausente deve sair do índice.'
Assert-Contains $remover 'kbIndex?.ForgetRemovedApi(guid)' 'API confirmada ausente deve sair do índice.'
Assert-Contains $remover 'kbIndex?.ForgetRemovedSdt(guid)' 'SDT confirmado ausente deve sair do índice.'
Assert-Contains $remover 'kbIndex?.ForgetRemovedFile(guid)' 'File confirmado ausente deve sair do índice.'
Assert-Contains $remover 'kbIndex?.ForgetRemovedFolder(guid)' 'Folder confirmado ausente deve sair do índice.'

# --- 4. Localização/revalidação pelo índice; confirmação permanece GetAll --------------------
Assert-Contains $remover 'kbIndex.FindProcedures(name).ToArray()' 'Localização de Procedure deve usar o índice quando presente.'
Assert-Contains $remover 'kbIndex.FindApis(context.ApiName).ToArray()' 'Localização de API deve usar o índice quando presente.'
Assert-Contains $remover 'kbIndex.FindSdts(name).ToArray()' 'Localização de SDT deve usar o índice quando presente.'
Assert-Contains $remover 'kbIndex.FindFiles(name).ToArray()' 'Localização de File deve usar o índice quando presente.'
Assert-Contains $remover 'kbIndex.FindFolders(target.Name).ToArray()' 'Localização de Folder deve usar o índice quando presente.'

$confirmScans = [regex]::Matches($remover, 'confirmacao-pos-delete').Count
if ($confirmScans -lt 5) {
    throw "ASSERT_FAILED: Esperava ao menos 5 confirmações pós-Delete por leitura corrente; achei $confirmScans."
}

Assert-Contains $remover '"confirmacao-pos-delete", () => Procedure.GetAll(designModel).Any' 'Confirmação de Procedure deve continuar em GetAll.'
Assert-Contains $remover '"confirmacao-pos-delete", () => API.GetAll(designModel).Any' 'Confirmação de API deve continuar em GetAll.'
Assert-Contains $remover '"confirmacao-pos-delete", () => SDT.GetAll(designModel).Any' 'Confirmação de SDT deve continuar em GetAll.'
Assert-Contains $remover '"confirmacao-pos-delete", () => WikiFileKBObject.GetAll(designModel).Any' 'Confirmação de File deve continuar em GetAll.'
Assert-Contains $remover '"confirmacao-pos-delete", () => Folder.GetAll(designModel).Any' 'Confirmação de Folder deve continuar em GetAll.'

# Revalidação pré-Delete com índice (não força null).
Assert-NotContains $remover 'ValidateProcedureTarget(designModel, name, beforeAnyDelete: false, kbIndex: null' 'Revalidação de Procedure não pode forçar kbIndex nulo.'
Assert-NotContains $remover 'ValidateApiObjectTarget(designModel, context.ApiName, context.ApiGuid, beforeAnyDelete: false, kbIndex: null' 'Revalidação de API não pode forçar kbIndex nulo.'
Assert-NotContains $remover 'ValidateOwnSdtTarget(designModel, context.PreservedSharedSdtNames, name, beforeAnyDelete: false, kbIndex: null' 'Revalidação de SDT não pode forçar kbIndex nulo.'

# --- 5. Retomada da recuperação também monta índice ------------------------------------------
Assert-Contains $package 'ContinueInterruptedRemoval(' 'Retomada de remoção interrompida deve existir.'
Assert-Contains $package 'ApiPlanKbObjectNameIndex.Create(knowledgeBase.DesignModel, busy.Session)' 'ContinueInterruptedRemoval deve criar índice para a fila.'
Assert-Contains $package 'kbIndex: kbIndex' 'Execute da retomada deve receber o índice.'

Write-Output 'PASS: ApiPlanB082Etapa1BIndex'
