#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$folderPath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\ApiPlanTransactionFolder.cs'
$statePath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\ApiPlanGenerationStateReader.cs'
$packagePath = Join-Path $repositoryRoot 'Src\Extension\Package.cs'
$metadataPath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\ApiPlanMetadataFileWriter.cs'

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

foreach ($path in @($folderPath, $statePath, $packagePath, $metadataPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "SOURCE_MISSING: $path"
    }
}

$folderSource = [IO.File]::ReadAllText($folderPath)
$stateSource = [IO.File]::ReadAllText($statePath)
$packageSource = [IO.File]::ReadAllText($packagePath)
$metadataSource = [IO.File]::ReadAllText($metadataPath)

$reuseStart = $folderSource.IndexOf('if (existingFolder is not null)', [StringComparison]::Ordinal)
$reuseEnd = $folderSource.IndexOf('return existingFolder;', $reuseStart, [StringComparison]::Ordinal)
if ($reuseStart -lt 0 -or $reuseEnd -lt 0) {
    throw 'ASSERT_FAILED: ramo de reuso do Folder nao encontrado.'
}

$reuseBranch = $folderSource.Substring($reuseStart, $reuseEnd - $reuseStart)
Assert-NotContains $reuseBranch '.Save()' 'Folder reutilizado nao pode ser salvo nem realinhado.'
Assert-Contains $folderSource 'var existingFolder = Preflight(designModel, transaction, apiPlan);' 'CreateOrReencounter deve validar o contenedor com a Transaction atual.'

$preflightStart = $folderSource.IndexOf('public static Folder? Preflight(', [StringComparison]::Ordinal)
$policyStart = $folderSource.IndexOf('internal static bool IsReusable(', [StringComparison]::Ordinal)
if ($preflightStart -lt 0 -or $policyStart -lt 0) {
    throw 'ASSERT_FAILED: Preflight/IsReusable nao encontrados.'
}

$preflightBlock = $folderSource.Substring($preflightStart, $policyStart - $preflightStart)
Assert-Contains $preflightBlock 'if (!IsReusable(folder, transaction, apiPlan))' 'Preflight deve bloquear Folder externo, incompatível ou fora do contenedor.'
Assert-Contains $folderSource 'description.StartsWith(OwnedDescriptionPrefix, StringComparison.Ordinal)' 'Description com sentinela deve ser classificada como posse da extensao.'
Assert-Contains $folderSource '!string.Equals(description, CreateOwnedDescription(apiPlan), StringComparison.Ordinal)' 'Sentinela de outra Transaction deve continuar bloqueando.'
Assert-Contains $folderSource 'transaction.Parent' 'A decisao deve considerar o Parent efetivo da Transaction.'
Assert-Contains $folderSource 'folder.Parent' 'A decisao deve comparar o Parent efetivo do Folder.'
Assert-Contains $folderSource 'transaction.Module' 'A decisao deve considerar o Module da Transaction.'
Assert-Contains $folderSource 'folder.Module' 'A decisao deve comparar o Module do Folder.'
Assert-Contains $folderSource 'CreateReuseWarning' 'O reuso deve produzir aviso explicito.'

Assert-Contains $stateSource 'InspectFolder(KbObjectNameIndex index, Transaction transaction, ApiPlan apiPlan)' 'O leitor de estado deve receber a Transaction para validar o contenedor.'
Assert-Contains $stateSource 'ApiPlanTransactionFolder.IsReusable(matches[0], transaction, apiPlan)' 'O leitor de estado deve compartilhar a politica de reuso.'
Assert-Contains $stateSource 'ApiPlanTransactionFolder.CreateReuseWarning(apiPlan)' 'O detalhe do Wizard deve informar o reuso.'
Assert-Contains $stateSource 'TransactionFolderWarning' 'O estado deve transportar o aviso para o relatorio final.'

Assert-Contains $packageSource 'AppendTransactionFolderWarning(report, generationState);' 'Wizard/Sync devem propagar o aviso ao relatorio B081.'
Assert-Contains $packageSource 'ReadForSync(knowledgeBase.DesignModel, transaction, apiPlan)' 'Sync deve validar o Folder no contenedor da Transaction.'
Assert-Contains $metadataSource '"wasCreated"] = apiPlan.TransactionFolderWasCreated' 'Metadata deve continuar persistindo wasCreated.'

Write-Output 'PASS: ApiPlanTransactionFolderReusePolicy'
