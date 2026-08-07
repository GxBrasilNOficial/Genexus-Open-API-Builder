Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$sourcePath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\PrototypeWizardContract.cs'
if (-not (Test-Path -LiteralPath $sourcePath)) {
    throw "SOURCE_MISSING: $sourcePath"
}

$source = [IO.File]::ReadAllText($sourcePath)

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

Assert-Contains $source 'if (primaryKeyPartCount > 1)' 'Atalho de chave composta deve existir em IsAutonumber.'
Assert-Contains $source 'GetPropertyValueString("Autonumber")' 'Leitura da propriedade Autonumber deve permanecer para PK simples.'
Assert-Contains $source 'GetPropertyValueString("idAUTONUMBER")' 'Fallback idAUTONUMBER deve permanecer para PK simples.'
Assert-NotContains $source 'AutonumberProbe' 'Sonda TEMP AutonumberProbe deve ter sido removida.'
Assert-NotContains $source 'WriteAutonumberProbe' 'WriteAutonumberProbe deve ter sido removido.'
Assert-NotContains $source 'IOutputService2' 'Instrumentacao de Output da sonda TEMP deve ter sido removida deste arquivo.'

Write-Output 'PASS: PrototypeWizardAutonumberCompositeKey'
