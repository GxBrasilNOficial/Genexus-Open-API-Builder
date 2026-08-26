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

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw "ASSERT_TRUE_FAILED: $Message"
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

# Ordem no overload SDK: short-circuit de PK composta ANTES de GetPropertyValueString.
# O corpo do overload termina onde começa o XML de IsAutonumberCore (não incluir esse XML).
$sdkStart = $helper.IndexOf('public static bool IsAutonumber(TransactionAttribute', [StringComparison]::Ordinal)
$coreStart = $helper.IndexOf('public static bool IsAutonumberCore(', [StringComparison]::Ordinal)
Assert-True ($sdkStart -ge 0 -and $coreStart -gt $sdkStart) 'Overload SDK e IsAutonumberCore devem existir nesta ordem.'
$coreDocStart = $helper.IndexOf('/// <summary>', $sdkStart, [StringComparison]::Ordinal)
Assert-True ($coreDocStart -gt $sdkStart -and $coreDocStart -lt $coreStart) 'XML de IsAutonumberCore deve ficar entre os dois métodos.'
$sdkBody = $helper.Substring($sdkStart, $coreDocStart - $sdkStart)
$idxCount = $sdkBody.IndexOf('if (primaryKeyPartCount > 1)', [StringComparison]::Ordinal)
$idxRead = $sdkBody.IndexOf('GetPropertyValueString("Autonumber")', [StringComparison]::Ordinal)
Assert-True ($idxCount -ge 0) 'Short-circuit de PK composta deve existir no overload SDK.'
Assert-True ($idxRead -ge 0) 'Leitura Autonumber deve existir no overload SDK.'
Assert-True ($idxCount -lt $idxRead) 'PK composta deve decidir antes de consultar a propriedade no overload SDK.'

Assert-True ($sdkBody.IndexOf('Evidência 2026-08-06', [StringComparison]::Ordinal) -lt 0) 'Evidência empírica não deve ficar no overload SDK.'
$coreDoc = $helper.Substring($coreDocStart, $coreStart - $coreDocStart)
Assert-True ($coreDoc.IndexOf('Evidência 2026-08-06', [StringComparison]::Ordinal) -ge 0) 'Evidência 2026-08-06 deve ficar no XML de IsAutonumberCore.'

Write-Output 'PASS: PrototypeWizardAutonumberCompositeKey'
