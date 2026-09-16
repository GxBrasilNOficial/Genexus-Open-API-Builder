#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Contrato do preflight B086, sob a intenção durável da F3 (P4) e o vínculo Preview→Remove
# da B082 Etapa 2 (decisão 7).
#
# Desde a P4, a validação agregada vive na resolução da intenção: é lá que cada alvo é
# validado e identificado, antes de o diário registrar o inventário e de a fila tentar a
# primeira exclusão. A invariante protegida continua a mesma — nenhuma exclusão sem
# preflight aprovado —, mas o lugar onde ela se lê mudou. A B082 acrescenta: a mesma
# instância do Preview alimenta a confirmação e o ResolveIntent.

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

function Assert-Order {
    param([string]$Text, [string]$First, [string]$Second, [string]$Message)
    $firstIndex = $Text.IndexOf($First, [StringComparison]::Ordinal)
    $secondIndex = $Text.IndexOf($Second, [StringComparison]::Ordinal)
    if ($firstIndex -lt 0 -or $secondIndex -lt 0 -or $firstIndex -ge $secondIndex) {
        throw "ASSERT_ORDER_FAILED: $Message"
    }
}

$source = [IO.File]::ReadAllText($removerPath)
$package = [IO.File]::ReadAllText((Join-Path $repositoryRoot 'Src\Extension\Package.cs'))

# --- 1. A intenção é resolvida com preflight aprovado, antes de qualquer exclusão ------------
$intentStart = $source.IndexOf('internal static ApiPlanGeneratedApiRemovalIntent ResolveIntent(', [StringComparison]::Ordinal)
$removeStart = $source.IndexOf('internal static ApiPlanGeneratedApiRemovalResult Remove(', [StringComparison]::Ordinal)
if ($intentStart -lt 0 -or $removeStart -lt 0 -or $intentStart -ge $removeStart) {
    throw 'ASSERT_FAILED: ResolveIntent/Remove não encontrados na ordem esperada.'
}

$intentBlock = $source.Substring($intentStart, $removeStart - $intentStart)
Assert-Contains $intentBlock 'ValidateRemovalTargets(designModel, plan' 'A resolução da intenção deve executar o preflight.'
Assert-Order $intentBlock 'ValidateRemovalTargets(designModel, plan' 'BuildTargets(' 'O preflight deve aprovar antes de a intenção montar os alvos.'
Assert-Contains $intentBlock 'ApiPlanGeneratedApiRemovalPlan confirmedPlan' 'A intenção deve receber o plano confirmado do Preview (B082 decisão 7).'
Assert-Contains $intentBlock 'AssertPreviewStillMatches(' 'A intenção deve conferir identidades capturadas no Preview antes de montar a fila.'
Assert-Contains $intentBlock 'AssertTargetsMatchCapture(' 'A fila montada deve bater com a captura do Preview.'

# --- 2. A fila só é alcançada a partir de uma intenção resolvida ------------------------------
# Sem isto, um caminho de exclusão poderia nascer sem preflight — que é o que o B086 proíbe.
$removeBlock = $source.Substring($removeStart)
$removeBlock = $removeBlock.Substring(0, $removeBlock.IndexOf('public static int CountPlannedDeletes', [StringComparison]::Ordinal))
Assert-Contains $removeBlock 'ApiPlanGeneratedApiRemovalIntent intent' 'Remove só executa sobre uma intenção já resolvida.'
Assert-Contains $removeBlock 'ApiPlanRemovalQueue.Run(' 'Remove executa a fila da F3, não um laço próprio.'
Assert-Equal 0 ([regex]::Matches($removeBlock, 'ValidateRemovalTargets\(').Count) 'Remove não repete o preflight: ele o exige já aprovado na intenção.'

# --- 3. Pontos de entrada do preflight ---------------------------------------------------------
Assert-Contains $source 'ValidateRemovalTargets(designModel, plan, progress' 'Intenção e Preview devem compartilhar o preflight (com progresso/índice opcionais).'
Assert-Equal 3 ([regex]::Matches($source, 'ValidateRemovalTargets\(designModel, plan').Count) 'Wrapper de 2 args, ResolveIntent e Preview devem ser os únicos pontos de entrada do preflight.'
Assert-Contains $source 'ValidateApiObjectTarget' 'Preflight deve validar o API Object.'
Assert-Contains $source 'ValidateProcedureTarget' 'Preflight deve validar Procedures.'
Assert-Contains $source 'ValidateOwnSdtTarget' 'Preflight deve validar SDTs próprios.'
Assert-Contains $source 'if (matches.Length > 1)' 'Preflight deve bloquear alvos ambíguos.'
Assert-Contains $source 'Nenhuma alteracao foi feita.' 'Bloqueio antes do Delete deve declarar ausência de alterações.'

# --- 4. No comando, a ordem é Preview → intenção(plano) → diário → fila -----------------------
# A intenção precisa estar durável antes do primeiro Delete: é ela que permite dizer, depois de
# uma interrupção, o que foi previsto e o que chegou a sair da KB. B082 decisão 7: a mesma
# instância do Preview alimenta a confirmação e o ResolveIntent.
Assert-Order $package 'ApiPlanGeneratedApiRemover.Preview(' 'ApiPlanGeneratedApiRemover.ResolveIntent(' 'O Preview deve preceder a resolução da intenção.'
Assert-Order $package 'ApiPlanGeneratedApiRemover.ResolveIntent(' 'ApiPlanOperationJournalSession.Start(
                    knowledgeBase.DesignModel,
                    intent.KbIndex,' 'A intenção deve ser resolvida antes de o diário abrir.'
Assert-Order $package 'JournalOperationKind.Remove,' 'ApiPlanGeneratedApiRemover.Remove(' 'O diário da remoção deve abrir antes da fila executar.'
Assert-Contains $package 'intent.BuildInventory()' 'O envelope da remoção deve nascer com o inventário completo dos alvos.'
Assert-Contains $package 'ResolveIntent(
                        knowledgeBase.DesignModel,
                        transaction,
                        plan,' 'ResolveIntent deve receber a instância confirmada do Preview.'

Write-Output 'PASS: ApiPlanGeneratedApiRemovalPreflight'
