#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$removerPath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\ApiPlanGeneratedApiRemover.cs'
if (-not (Test-Path -LiteralPath $removerPath -PathType Leaf)) {
    throw "SOURCE_MISSING: $removerPath"
}

function Assert-Contains {
    param([string]$Text, [string]$Needle, [string]$Message)
    if ($Text.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "ASSERT_CONTAINS_FAILED: $Message"
    }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Message)
    if ($Expected -ne $Actual) {
        throw "ASSERT_EQUAL_FAILED: $Message (expected='$Expected' actual='$Actual')"
    }
}

$source = [IO.File]::ReadAllText($removerPath)
$removeStart = $source.IndexOf('public static ApiPlanGeneratedApiRemovalResult Remove(', [StringComparison]::Ordinal)
$previewStart = $source.IndexOf('public static ApiPlanGeneratedApiRemovalPlan Preview(', [StringComparison]::Ordinal)
if ($removeStart -lt 0 -or $previewStart -lt 0 -or $previewStart -le $removeStart) {
    throw 'ASSERT_FAILED: Remove/Preview não encontrados na ordem esperada.'
}

$removeBlock = $source.Substring($removeStart, $previewStart - $removeStart)
Assert-Contains $removeBlock 'ValidateRemovalTargets(designModel, plan' 'Remove deve executar o preflight antes de qualquer Delete.'
$validationIndex = $removeBlock.IndexOf('ValidateRemovalTargets(designModel, plan', [StringComparison]::Ordinal)
$deleteIndex = $removeBlock.IndexOf('DeleteApiObject(designModel, plan, deleted)', [StringComparison]::Ordinal)
if ($validationIndex -lt 0 -or $deleteIndex -lt 0 -or $validationIndex -ge $deleteIndex) {
    throw 'ASSERT_ORDER_FAILED: preflight deve ocorrer antes da primeira exclusão.'
}

Assert-Contains $source 'ValidateRemovalTargets(designModel, plan, progress' 'Preview e Remove devem compartilhar o preflight (com progresso/índice opcionais).'
Assert-Equal 3 ([regex]::Matches($source, 'ValidateRemovalTargets\(designModel, plan').Count) 'Wrapper de 2 args, Remove e Preview devem ser os únicos pontos de entrada do preflight.'
Assert-Contains $source 'ValidateApiObjectTarget' 'Preflight deve validar o API Object.'
Assert-Contains $source 'ValidateProcedureTarget' 'Preflight deve validar Procedures.'
Assert-Contains $source 'ValidateOwnSdtTarget' 'Preflight deve validar SDTs próprios.'
Assert-Contains $source 'if (matches.Length > 1)' 'Preflight deve bloquear alvos ambíguos.'
Assert-Contains $source 'Nenhuma alteracao foi feita.' 'Bloqueio antes do Delete deve declarar ausência de alterações.'

Write-Output 'PASS: ApiPlanGeneratedApiRemovalPreflight'
