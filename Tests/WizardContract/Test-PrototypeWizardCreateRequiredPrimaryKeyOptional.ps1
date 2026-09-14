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
Assert-Contains $source 'RoleLabel("CreateRequest")' 'Rotulo CreateRequest da aba Obrigatorios deve usar RoleLabel.'
Assert-Contains $source 'Obrigatório no payload (editável)' 'Rotulo da aba deve indicar Create required editavel.'
Assert-Contains $source 'Width = 1800;' 'Wizard principal deve iniciar com largura de 1800 pixels.'
# A largura subiu de 1500 para 1800 em 2026-09-14: o resumo e os avisos de escala quebravam
# linha cedo demais. A asserção continua fixando o valor de propósito — a janela é maior que
# muitos monitores, e quem mudá-la precisa conferir o encolhimento de CenterOnIdeScreen.
$placementPath = Join-Path $PSScriptRoot '..\..\Src\Extension\ExtensionIdeScreenPlacement.cs'
if (-not (Test-Path -LiteralPath $placementPath)) {
    throw "SOURCE_MISSING: $placementPath"
}
Assert-Contains ([IO.File]::ReadAllText($placementPath)) 'Math.Min(form.Width, working.Width)' 'Diálogos devem encolher para caber no monitor.'
Assert-Contains $source 'Height = 1004;' 'Wizard principal deve iniciar com altura de 1004 pixels.'

# Garante que o default de PK e false (return false apos IsPrimaryKey no DefaultCreateRequired).
if ($source -notmatch '(?s)private bool DefaultCreateRequired\(string fieldName\).*?if \(attribute\.IsPrimaryKey\)\s*\{\s*return false;') {
    throw 'ASSERT_FAILED: DefaultCreateRequired deve retornar false para IsPrimaryKey.'
}

Write-Output 'PASS: PrototypeWizardCreateRequiredPrimaryKeyOptional'
