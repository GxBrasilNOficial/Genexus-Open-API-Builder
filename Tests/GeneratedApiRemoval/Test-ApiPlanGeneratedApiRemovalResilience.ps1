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
$package = Read-Source 'Src\Extension\Package.cs'

# --- 1. O laço de SDTs não pode ser um foreach direto sobre a lista ------------------------
Assert-True ($remover -match 'DeleteOwnSdtsResilientToOrder') 'A remoção de SDTs próprios deve passar pelo laço resiliente à ordem.'

# `DeleteSingleOwnSdt` só pode ser invocada de dentro do laço resiliente. Qualquer outro
# chamador voltaria a apagar em sequência e a interromper no meio numa ordem imperfeita —
# foi assim que `DeleteOwnSdts`, sem chamador, ficou no código como armadilha.
$deleteCalls = ([regex]::Matches($remover, 'DeleteSingleOwnSdt\(')).Count
Assert-True ($deleteCalls -eq 2) "DeleteSingleOwnSdt deve ter exatamente uma invocação, no laço resiliente, mais a declaração; encontradas $deleteCalls ocorrências."

$loopStart = $remover.IndexOf('private static int DeleteOwnSdtsResilientToOrder')
Assert-True ($loopStart -ge 0) 'O laço resiliente deve existir.'
$loopBody = $remover.Substring($loopStart)
$loopBody = $loopBody.Substring(0, $loopBody.IndexOf('private static string BuildStalledRemovalMessage'))

# --- 2. Progresso é a condição de continuar ------------------------------------------------
Assert-True ($loopBody -match 'while\s*\(pending\.Count\s*>\s*0\)') 'O laço deve repetir enquanto houver pendentes.'
Assert-True ($loopBody -match 'deletedInPass\+\+') 'O laço deve contar quantos foram apagados na passada.'
Assert-True ($loopBody -match 'if\s*\(deletedInPass\s*==\s*0\)') 'Uma passada sem progresso deve encerrar o laço; sem isso ele nunca termina.'
Assert-True ($loopBody -match 'throw new InvalidOperationException\(BuildStalledRemovalMessage') 'A parada sem progresso deve reportar os pendentes e o motivo de cada um.'

# --- 3. O abort do usuário nunca é adiado --------------------------------------------------
# Sem esta guarda, Abortar viraria "adiar" e o laço tentaria de novo, ignorando o usuário.
$abortIndex = $loopBody.IndexOf('catch (ApiPlanBusyAbortedException)')
$genericIndex = $loopBody.IndexOf('catch (Exception exception)')
Assert-True ($abortIndex -ge 0) 'O laço deve relançar ApiPlanBusyAbortedException.'
Assert-True ($genericIndex -ge 0) 'O laço deve adiar as demais falhas.'
Assert-True ($abortIndex -lt $genericIndex) 'O catch do abort deve vir antes do catch genérico, senão o abort é engolido como adiável.'

# --- 4. A recusa não pode ser interpretada por mensagem ------------------------------------
# Depender do texto da exceção da IDE é frágil e muda com a versão/idioma.
Assert-False ($loopBody -match 'is referenced') 'O laço não pode decidir o que é adiável lendo o texto da exceção.'
Assert-False ($loopBody -match 'Message\.Contains') 'O laço não pode decidir o que é adiável lendo o texto da exceção.'

# --- 5. O relatório precisa saber o que já saiu da KB --------------------------------------
Assert-True ($remover -match 'List<string>\?\s*deletedSink') 'Remove deve aceitar um coletor externo do que foi apagado.'
Assert-True ($remover -match 'deleted\s*=\s*deletedSink\s*\?\?') 'O coletor externo deve ser a lista usada durante a remoção.'
Assert-True ($package -match 'deletedBeforeFailure') 'O comando Remover deve manter a lista do que já saiu.'

$deletedDecl = $package.IndexOf('var deletedBeforeFailure = new List<string>();')
$removeCall = $package.IndexOf('ApiPlanGeneratedApiRemover.Remove(knowledgeBase.DesignModel, transaction, busy.Session, deletedBeforeFailure)')
Assert-True ($deletedDecl -ge 0) 'A lista deve ser declarada no comando Remover.'
Assert-True ($removeCall -ge 0) 'A lista deve ser passada para Remove.'
Assert-True ($deletedDecl -lt $removeCall) 'A lista deve ser declarada antes da chamada.'

# Declarada fora do try: dentro dele, o catch externo não a enxergaria.
$tryIndex = $package.IndexOf('try', $deletedDecl)
Assert-True ($deletedDecl -lt $tryIndex) 'A lista deve ser declarada fora do try, para o catch conseguir lê-la.'

$addDeletedCount = ([regex]::Matches($package, 'AddDeletedItems\(deletedBeforeFailure\.ToArray\(\)\)')).Count
Assert-True ($addDeletedCount -eq 2) "O relatório deve listar os removidos nos dois caminhos de interrupção — abort e falha; encontrados $addDeletedCount."
Assert-True ($package -match 'Remocao parcial:') 'Uma remoção interrompida deve avisar que a API ficou incompleta.'

Write-Output 'PASS: ApiPlanGeneratedApiRemovalResilience'
