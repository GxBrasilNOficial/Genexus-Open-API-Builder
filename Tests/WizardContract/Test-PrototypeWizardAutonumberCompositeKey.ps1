Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$helperPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\TransactionAttributeKeyTraits.cs'
$contractPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\PrototypeWizardContract.cs'
if (-not (Test-Path -LiteralPath $helperPath)) {
    throw "SOURCE_MISSING: $helperPath"
}

if (-not (Test-Path -LiteralPath $contractPath)) {
    throw "SOURCE_MISSING: $contractPath"
}

$helper = [IO.File]::ReadAllText($helperPath)
$contract = [IO.File]::ReadAllText($contractPath)

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

Assert-Contains $helper 'if (primaryKeyPartCount > 1)' 'Atalho de chave composta deve existir no helper compartilhado.'
Assert-Contains $helper 'GetPropertyValueString("Autonumber")' 'Leitura da propriedade Autonumber deve permanecer no helper.'
Assert-Contains $helper 'GetPropertyValueString("idAUTONUMBER")' 'Fallback idAUTONUMBER deve permanecer no helper.'
Assert-Contains $helper 'Evidência 2026-08-06' 'Justificativa empírica da chave composta deve permanecer no helper.'
Assert-Contains $contract 'TransactionAttributeKeyTraits.IsAutonumber' 'O Wizard flat deve delegar autonumeração ao helper compartilhado.'
Assert-NotContains $contract 'private static bool IsAutonumber(' 'A cópia local de IsAutonumber deve ter saído do Wizard flat.'
Assert-NotContains $contract 'AutonumberProbe' 'Sonda TEMP AutonumberProbe deve ter sido removida.'
Assert-NotContains $contract 'WriteAutonumberProbe' 'WriteAutonumberProbe deve ter sido removido.'
Assert-NotContains $contract 'IOutputService2' 'Instrumentacao de Output da sonda TEMP deve ter sido removida deste arquivo.'

Write-Output 'PASS: PrototypeWizardAutonumberCompositeKey'
