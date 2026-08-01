Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$writerPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanBusinessComponentWriter.cs'
$source = Get-Content -Path $writerPath -Raw

function Assert-Contains {
    param([string]$Text, [string]$Expected, [string]$Message)
    if (-not $Text.Contains($Expected)) {
        throw "ASSERT_CONTAINS_FAILED: $Message Expected='$Expected'"
    }
}

function Assert-NotContains {
    param([string]$Text, [string]$Unexpected, [string]$Message)
    if ($Text.Contains($Unexpected)) {
        throw "ASSERT_NOT_CONTAINS_FAILED: $Message Unexpected='$Unexpected'"
    }
}

Assert-Contains $source 'new VariableSpec("ErrorResponse", "sdt_API_ErrorResponse")' 'Create/Update devem declarar ErrorResponse como corpo publico de erro.'
Assert-Contains $source '&ErrorResponse.Code = !\"validation_error\"' 'Procedure deve popular codigo de erro top-level para falha de regra de negocio.'
Assert-Contains $source '&Messages = {bc}.GetMessages()' 'Procedure deve preservar mensagens do Business Component para diagnostico no Output.'
Assert-Contains $source 'PreviousB079BusinessRuleFailureMessages' 'Preflight deve reconhecer a variante intermediaria com ErrorItem apenas para migracao.'

$currentFailureStart = $source.IndexOf('private static IEnumerable<string> BusinessRuleFailureMessages', [StringComparison]::Ordinal)
$previousFailureStart = $source.IndexOf('private static IEnumerable<string> PreviousB079BusinessRuleFailureMessages', [StringComparison]::Ordinal)
if ($currentFailureStart -lt 0 -or $previousFailureStart -lt 0 -or $previousFailureStart -le $currentFailureStart) {
    throw 'ASSERT_SECTION_FAILED: nao foi possivel isolar BusinessRuleFailureMessages atual.'
}

$currentFailureSource = $source.Substring($currentFailureStart, $previousFailureStart - $currentFailureStart)
Assert-NotContains $currentFailureSource '&ErrorResponse.Errors.Add(&ErrorItem)' 'Procedure gerada atualmente nao deve chamar Errors.Add com item nested enquanto GeneXus rejeita a validacao do objeto.'

Write-Output 'PASS: ApiPlanBusinessComponentWriterVariableContract'
