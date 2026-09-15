#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Contrato da remoção resiliente à ordem e do relatório de remoção parcial.
#
# Origem: 2026-09-06, KB `wsEducacaoSpTeste`, Transaction `Teste`. Uma metadata cuja lista de
# SDTs próprios não vinha na ordem de dependência parou a remoção no meio —
# `'sdtTeste_API_ListFilters' is referenced at least by 'sdtTeste_API_ListResponse'` — com o
# API Object, 5 Procedures e 5 SDTs já apagados. O relatório final informou
# «Removidos: (nenhum)».
#
# Desde a P4 da F3, a resiliência deixou de ser um laço só de SDTs e passou a ser a fila
# unificada de `ApiPlanRemovalQueue`, com orçamento fechado e checkpoint por passada. O
# comportamento offline dessa fila é exercitado em `Test-ApiPlanRemovalQueue.ps1`; aqui ficam
# as invariantes de integração — quem chama quem, e o que o relatório continua sabendo.

function Read-Source {
    param([string]$RelativePath)
    $path = Join-Path $PSScriptRoot (Join-Path '..\..' $RelativePath)
    if (-not (Test-Path -LiteralPath $path)) {
        throw "FONTE_NAO_ENCONTRADA: $RelativePath"
    }
    return Get-Content -Raw -LiteralPath $path
}

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw "ASSERT_TRUE_FAILED: $Message"
    }
}

function Assert-False {
    param([bool]$Condition, [string]$Message)
    if ($Condition) {
        throw "ASSERT_FALSE_FAILED: $Message"
    }
}

$remover = Read-Source 'Src\Extension\Diagnostics\ApiPlanGeneratedApiRemover.cs'
$queue = Read-Source 'Src\Extension\Diagnostics\ApiPlanRemovalQueue.cs'
$package = Read-Source 'Src\Extension\Package.cs'

# --- 1. A fila é unificada e abrange todos os tipos removíveis ------------------------------
# O laço antigo tratava só SDTs; API Object, Procedures, metadata e Folder saíam em sequência
# e uma recusa no meio interrompia tudo.
Assert-True ($remover -match 'ApiPlanRemovalQueue\.Run\(') 'A remoção deve passar pela fila por passadas.'
Assert-False ($remover -match 'DeleteOwnSdtsResilientToOrder') 'O laço exclusivo de SDTs não pode voltar: a fila é de todos os tipos.'
foreach ($objectType in @('ApiObject', 'Procedure', 'Sdt', 'MetadataFile', 'Folder')) {
    Assert-True ($remover -match "case JournalObjectType\.$objectType") "A fila deve tratar $objectType."
}

# `DeleteSingleOwnSdt` só pode ser invocada de dentro da fila. Qualquer outro chamador voltaria
# a apagar em sequência — foi assim que `DeleteOwnSdts`, sem chamador, ficou no código como
# armadilha.
$deleteCalls = ([regex]::Matches($remover, 'DeleteSingleOwnSdt\(')).Count
Assert-True ($deleteCalls -eq 2) "DeleteSingleOwnSdt deve ter exatamente uma invocação, na fila, mais a declaração; encontradas $deleteCalls ocorrências."

# --- 2. Só a evidência recoloca um alvo na fila -----------------------------------------------
Assert-True ($queue -match 'case ApiPlanRemovalAttemptResult\.StillPresent:\s*\r?\n\s*next\.Add\(target\);') 'Só um alvo comprovadamente presente depois do Delete volta para a fila.'
Assert-True ($queue -match 'JournalBlockReason\.RetryBudgetExhausted') 'O orçamento esgotado deve terminar com motivo próprio.'
Assert-True ($queue -match 'JournalBlockReason\.TargetAbsentBeforeDelete') 'Ausência antes do Delete não é sucesso implícito.'
Assert-True ($queue -match 'JournalOperationState\.OutcomeUnknown') 'Resultado indeterminado bloqueia a operação.'
Assert-True ($remover -match 'ApiPlanRemovalIntent\.ResolveMaxPasses|intent\.MaxPasses') 'O orçamento de passadas deve vir do inventário.'

# --- 3. O abort do usuário nunca é adiado -----------------------------------------------------
# Sem esta guarda, Abortar viraria "adiar" e a fila tentaria de novo, ignorando o usuário.
$attemptStart = $remover.IndexOf('private static ApiPlanRemovalAttemptResult Attempt(')
Assert-True ($attemptStart -ge 0) 'A tentativa unificada deve existir.'
$attemptBody = $remover.Substring($attemptStart)
$attemptBody = $attemptBody.Substring(0, $attemptBody.IndexOf('internal static void ValidateRemovalTargets'))
$abortIndex = $attemptBody.IndexOf('catch (ApiPlanBusyAbortedException)')
$genericIndex = $attemptBody.IndexOf('catch (Exception exception)')
Assert-True ($abortIndex -ge 0) 'A tentativa deve relançar ApiPlanBusyAbortedException.'
Assert-True ($genericIndex -ge 0) 'A tentativa deve classificar as demais falhas.'
Assert-True ($abortIndex -lt $genericIndex) 'O catch do abort deve vir antes do genérico, senão o abort é engolido como falha de etapa.'

# --- 4. A recusa não pode ser interpretada por mensagem ---------------------------------------
# Depender do texto da exceção da IDE é frágil e muda com a versão/idioma.
Assert-False ($remover -match 'is referenced') 'A classificação não pode ler o texto da exceção da IDE.'
Assert-False ($remover -match 'Message\.Contains') 'A classificação não pode ler o texto da exceção da IDE.'
Assert-True ($remover -match 'private static ApiPlanRemovalAttemptResult Classify\(') 'A classificação deve vir da releitura do alvo.'

# --- 5. O relatório precisa saber o que já saiu da KB -----------------------------------------
Assert-True ($remover -match 'List<string>\?\s*deletedSink') 'Remove deve aceitar um coletor externo do que foi apagado.'
Assert-True ($remover -match 'deleted\s*=\s*deletedSink\s*\?\?') 'O coletor externo deve ser a lista usada durante a remoção.'
Assert-True ($package -match 'deletedBeforeFailure') 'O comando Remover deve manter a lista do que já saiu.'

$deletedDecl = $package.IndexOf('var deletedBeforeFailure = new List<string>();')
$removeCall = $package.IndexOf('ApiPlanGeneratedApiRemover.Remove(')
Assert-True ($deletedDecl -ge 0) 'A lista deve ser declarada no comando Remover.'
Assert-True ($removeCall -ge 0) 'A lista deve ser passada para Remove.'
Assert-True ($deletedDecl -lt $removeCall) 'A lista deve ser declarada antes da chamada.'

# Declarada fora do try: dentro dele, o catch externo não a enxergaria.
$tryIndex = $package.IndexOf('try', $deletedDecl)
Assert-True ($deletedDecl -lt $tryIndex) 'A lista deve ser declarada fora do try, para o catch conseguir lê-la.'

$addDeletedCount = ([regex]::Matches($package, 'AddDeletedItems\(deletedBeforeFailure\.ToArray\(\)\)')).Count
Assert-True ($addDeletedCount -eq 2) "O relatório deve listar os removidos nos dois caminhos de interrupção — abort e falha; encontrados $addDeletedCount."
Assert-True ($package -match 'Remoção parcial: ') 'Uma remoção interrompida deve avisar que a API ficou incompleta.'
# O aviso precisa apontar a saída que existe: repetir a remoção do zero bloqueia em
# TargetAbsentBeforeDelete, e quem retoma a fila no mesmo registro é o comando de recuperação.
$partialWarnings = ([regex]::Matches($package, "Recuperar operação interrompida' para retomar a fila no mesmo registro")).Count
Assert-True ($partialWarnings -eq 2) "Os dois avisos de remoção parcial devem indicar a recuperação; encontrados $partialWarnings."

# --- 6. O diário fecha com o estado real da fila ----------------------------------------------
# Terminar em `Removed` quando a fila parou no meio seria pior que não ter diário nenhum.
Assert-True ($package -match 'private static void CloseRemovalJournal\(') 'O comando deve fechar o diário conforme o desfecho da fila.'
Assert-True ($package -match 'journal\.Interrupt\(queue\.Outcome, queue\.BlockReason') 'Uma fila interrompida deve gravar o estado e o motivo que ela apurou.'
Assert-True ($package -match 'JournalBlockReason\.UserAborted') 'O aborto do usuário tem motivo próprio, distinto de falha de etapa.'

# A sessão do diário também não pode voltar a derivar o inventário de uma remoção dos recibos:
# a intenção registrada antes do primeiro Delete é que descreve o previsto.
$session = Read-Source 'Src\Extension\Diagnostics\ApiPlanOperationJournalSession.cs'
Assert-True ($session -match 'AttachRemovalInventory') 'A sessão deve aceitar o inventário vivo da remoção.'
Assert-True ($package -match 'removeJournal\.AttachRemovalInventory\(intent\.BuildInventory\)') 'O comando deve ligar o inventário da intenção ao diário.'

Write-Output 'PASS: ApiPlanGeneratedApiRemovalResilience'
