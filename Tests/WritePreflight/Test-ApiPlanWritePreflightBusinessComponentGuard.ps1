Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$preflightPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanWritePreflight.cs'
$source = Get-Content -LiteralPath $preflightPath -Raw

function Assert-Contains {
    param([string]$Text, [string]$Expected, [string]$Message)
    if (-not $Text.Contains($Expected)) {
        throw "ASSERT_CONTAINS_FAILED: $Message Expected='$Expected'"
    }
}

$methodStart = $source.IndexOf('internal static void ValidateForF1(', [StringComparison]::Ordinal)
$nextMethodStart = $source.IndexOf('private static void ValidateForIntentionalChange(', [StringComparison]::Ordinal)
if ($methodStart -lt 0 -or $nextMethodStart -le $methodStart) {
    throw 'ASSERT_SECTION_FAILED: nao foi possivel isolar ValidateForF1.'
}

$methodSource = $source.Substring($methodStart, $nextMethodStart - $methodStart)
Assert-Contains $methodSource 'if (applyBusinessComponent && !transaction.IsBusinessComponent)' 'F1 deve bloquear BC quando a Transaction estiver com Business Component desabilitado.'
Assert-Contains $methodSource 'B055 bloqueado: Transaction=' 'O bloqueio de BC deve preservar o diagnostico B055.'
Assert-Contains $methodSource 'Nenhuma alteracao foi feita.' 'O bloqueio de BC deve declarar que nenhuma alteracao foi feita.'

$guardIndex = $methodSource.IndexOf('if (applyBusinessComponent && !transaction.IsBusinessComponent)', [StringComparison]::Ordinal)
$consumerValidationIndex = $methodSource.IndexOf('if (generateSdts && requiresConsumersOrApi)', [StringComparison]::Ordinal)
if ($guardIndex -lt 0 -or $consumerValidationIndex -lt 0 -or $guardIndex -ge $consumerValidationIndex) {
    throw 'ASSERT_ORDER_FAILED: o guard de BC deve ocorrer antes das validacoes de writers consumidores.'
}

Write-Output 'PASS: ApiPlanWritePreflightBusinessComponentGuard'
