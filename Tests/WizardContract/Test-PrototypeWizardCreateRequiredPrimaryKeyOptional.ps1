Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$dialogPath = Join-Path $PSScriptRoot '..\..\Src\Extension\PrototypeWizardDialog.cs'
if (-not (Test-Path -LiteralPath $dialogPath)) {
    throw "SOURCE_MISSING: $dialogPath"
}

$source = [IO.File]::ReadAllText($dialogPath)

function Assert-Contains {
    param([string]$Text, [string]$Needle, [string]$Message)
    if ($Text.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "ASSERT_CONTAINS_FAILED: $Message"
    }
}

Assert-Contains $source 'if (attribute.IsPrimaryKey)' 'DefaultCreateRequired deve tratar chave primaria.'
Assert-Contains $source 'DefaultCreateRequired' 'Heuristica DefaultCreateRequired deve existir.'
Assert-Contains $source '_createRequiredList' 'Aba Obrigatorios Create deve ser editavel via lista de checkboxes.'
Assert-Contains $source 'Chave primária não autonumerada inicia opcional' 'Motivo de PK opcional no Create deve existir.'
Assert-Contains $source 'CreateRequest - Obrigatório no payload (editável)' 'Rotulo da aba deve indicar Create required editavel.'

# Garante que o default de PK e false (return false apos IsPrimaryKey no DefaultCreateRequired).
if ($source -notmatch '(?s)private bool DefaultCreateRequired\(string fieldName\).*?if \(attribute\.IsPrimaryKey\)\s*\{\s*return false;') {
    throw 'ASSERT_FAILED: DefaultCreateRequired deve retornar false para IsPrimaryKey.'
}

Write-Output 'PASS: PrototypeWizardCreateRequiredPrimaryKeyOptional'
