#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$helperPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanSdtMemberSequenceMatcher.cs'
$matcherType = Add-Type -Path $helperPath -PassThru

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw "ASSERT_TRUE_FAILED: $Message" }
}

function Assert-False {
    param([bool]$Condition, [string]$Message)
    if ($Condition) { throw "ASSERT_FALSE_FAILED: $Message" }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Message)
    if ($Expected -ne $Actual) {
        throw "ASSERT_EQUAL_FAILED: $Message (expected='$Expected' actual='$Actual')"
    }
}

function Invoke-Match {
    param(
        [string[]]$Actual,
        [string[]]$Planned,
        [string[]]$Allowed = $null
    )

    [System.Collections.Generic.IReadOnlyList[int]]$indexes = $null
    [string]$mismatch = $null
    $method = $matcherType.GetMethod('TryMatch')
    $arguments = [object[]]@($Actual, $Planned, $Allowed, $indexes, $mismatch)
    $matched = $method.Invoke($null, $arguments)
    $indexes = $arguments[3]
    $mismatch = $arguments[4]
    return [pscustomobject]@{
        Matched = $matched
        Indexes = @($indexes)
        Mismatch = $mismatch
    }
}

$strict = Invoke-Match -Actual @('A', 'B') -Planned @('A', 'B')
Assert-True $strict.Matched 'A sequência estrita igual deve passar.'
Assert-Equal 2 $strict.Indexes.Count 'A sequência estrita deve mapear os dois membros.'
Assert-Equal 0 $strict.Indexes[0] 'O primeiro membro estrito deve manter o índice.'
Assert-Equal 1 $strict.Indexes[1] 'O segundo membro estrito deve manter o índice.'

$strictMissing = Invoke-Match -Actual @('A') -Planned @('A', 'B')
Assert-False $strictMissing.Matched 'A ausência não selecionada deve continuar bloqueada.'
Assert-True ($strictMissing.Mismatch -like 'qtd=1*esperada 2*') 'O bloqueio estrito deve reportar a diferença de quantidade.'

$selectedAppend = Invoke-Match -Actual @('A') -Planned @('A', 'B') -Allowed @('B')
Assert-True $selectedAppend.Matched 'Uma inclusão selecionada no final deve passar.'
Assert-Equal 2 $selectedAppend.Indexes.Count 'A inclusão selecionada deve preservar o plano completo.'
Assert-Equal 0 $selectedAppend.Indexes[0] 'O membro persistido deve continuar mapeado.'
Assert-Equal -1 $selectedAppend.Indexes[1] 'O membro novo deve ser marcado como ausente para o writer.'

$selectedMiddle = Invoke-Match -Actual @('A', 'C') -Planned @('A', 'B', 'C') -Allowed @('B')
Assert-True $selectedMiddle.Matched 'Uma inclusão selecionada no meio deve passar sem reordenar membros existentes.'
Assert-Equal 1 $selectedMiddle.Indexes[2] 'O membro posterior deve conservar seu índice persistido.'

$unselectedMissing = Invoke-Match -Actual @('A', 'B') -Planned @('A', 'B', 'C') -Allowed @('B')
Assert-False $unselectedMissing.Matched 'Uma ausência não selecionada deve continuar bloqueada mesmo com outra inclusão autorizada.'
Assert-True ($unselectedMissing.Mismatch -like "membros ausentes 'C'") 'O bloqueio deve identificar a inclusão não autorizada.'

$reordered = Invoke-Match -Actual @('B') -Planned @('A', 'B') -Allowed @('B')
Assert-False $reordered.Matched 'A alteração de ordem de membro existente não deve ser aceita.'
Assert-True ($reordered.Mismatch -like "ordem 'B' != 'A'*") 'O bloqueio deve identificar a ordem divergente.'

Write-Output 'PASS: ApiPlanTransactionSyncSdtMemberSequenceMatcher'
