#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Contrato da recuperação de metadata órfã (B115). Verificação textual: a classe depende de
# tipos do SDK do GeneXus e não compila isolada, mas as invariantes que quebraram em campo
# são todas verificáveis no fonte.
#
# Desenho e pontos em aberto: seção 13 de
# Docs/Implementation/2026-09-04-B111-F3-PLANO-DURABILIDADE-E-REMOCAO.md

function Read-Source {
    param([string]$RelativePath)
    $path = Join-Path $PSScriptRoot (Join-Path '..\..' $RelativePath)
    if (-not (Test-Path -LiteralPath $path)) {
        throw "FONTE_NAO_ENCONTRADA: $RelativePath"
    }
    return Get-Content -Raw -LiteralPath $path
}

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw "ASSERT_TRUE_FAILED: $Message"
    }
}

function Assert-False {
    param([bool]$Condition, [string]$Message)
    if ($Condition) {
        throw "ASSERT_FALSE_FAILED: $Message"
    }
}

$recovery = Read-Source 'Src\Extension\Diagnostics\ApiPlanOrphanMetadataRecovery.cs'
$package = Read-Source 'Src\Extension\Package.cs'
$sync = Read-Source 'Src\Extension\Diagnostics\ApiPlanTransactionSyncOrchestrator.cs'
$remover = Read-Source 'Src\Extension\Diagnostics\ApiPlanGeneratedApiRemovalPlan.cs'

# --- 1. A oferta precisa vir ANTES do diálogo do Wizard -----------------------------------
# Em 2026-09-06 o recurso era inalcançável: era oferecido depois de o Wizard concluir com
# sucesso, e sem a metadata o Wizard abre bloqueado, com Cancelar como única saída.
$offerIndex = $package.IndexOf('OfferOrphanMetadataRecoveryIfEnabled(')
Assert-True ($offerIndex -ge 0) 'O Wizard deve chamar a oferta de recuperação.'
$dialogIndex = $package.IndexOf('using (dialog!)')
Assert-True ($dialogIndex -ge 0) 'O bloco do diálogo do Wizard deve existir.'
Assert-True ($offerIndex -lt $dialogIndex) 'A oferta de recuperação deve ser avaliada ANTES de abrir o diálogo do Wizard; depois dele o estado bloqueado impede a conclusão e a oferta nunca é alcançada.'

# --- 2. O JSON gravado não pode inventar contrato -----------------------------------------
$jsonStart = $recovery.IndexOf('CreateRecoveryJson')
Assert-True ($jsonStart -ge 0) 'CreateRecoveryJson deve existir.'
$jsonBody = $recovery.Substring($jsonStart)
$jsonBody = $jsonBody.Substring(0, $jsonBody.IndexOf('private static bool CallsGeneratedProcedures'))

foreach ($forbidden in @('fields', 'pagination', 'order', 'services', 'levels', 'transactionStructure')) {
    Assert-False ($jsonBody -match ('\["' + $forbidden + '"\]\s*=')) "A metadata recuperada não pode gravar o bloco '$forbidden': esse dado só existe na metadata perdida, e inventá-lo faria o Sincronizar comparar a API real contra uma descrição falsa."
}

# --- 3. Os blocos que a remoção consome precisam estar lá ---------------------------------
foreach ($required in @('schemaVersion', 'ownership', 'objects', 'recovery')) {
    Assert-True ($jsonBody -match ('\["' + $required + '"\]\s*=')) "A metadata recuperada deve gravar o bloco '$required'."
}
foreach ($ownershipKey in @('transactionName', 'transactionGuid', 'apiName', 'apiGuid', 'metadataFileName')) {
    Assert-True ($jsonBody -match ('\["' + $ownershipKey + '"\]\s*=')) "ApiPlanGeneratedApiRemovalPlan.FromMetadata exige ownership.$ownershipKey."
}
foreach ($objectKey in @('transactionFolder', 'procedures', 'own', 'shared')) {
    Assert-True ($jsonBody -match ('\["' + $objectKey + '"\]\s*=')) "ApiPlanGeneratedApiRemovalPlan.FromMetadata exige objects.$objectKey."
}

# --- 4. O schemaVersion tem de ser o mesmo do writer --------------------------------------
# Com outro valor, RequireSupportedSchemaVersion recusaria a metadata e o Remover seguiria
# bloqueado — o oposto do objetivo da recuperação.
Assert-True ($jsonBody -match 'ApiPlanMetadataFileWriter\.SchemaVersion') 'A metadata recuperada deve reusar ApiPlanMetadataFileWriter.SchemaVersion, e não um literal próprio.'

# --- 5. O Folder nunca entra como criado pela extensão ------------------------------------
Assert-True ($jsonBody -match '\["wasCreated"\]\s*=\s*false') 'objects.transactionFolder.wasCreated deve ser false: não há como saber se o Folder foi criado pela extensão, e a remoção não pode apagar Folder de terceiro.'

# --- 6. A marca de intenção importada -----------------------------------------------------
Assert-True ($jsonBody -match '\["imported"\]\s*=\s*true') 'A metadata recuperada deve ser marcada como intenção importada.'
Assert-True ($jsonBody -match '\["notRecovered"\]') 'A marca deve listar o que não foi recuperado.'
Assert-True ($recovery -match 'IsImportedRecovery') 'Deve existir o reconhecedor da marca.'
Assert-True ($recovery -match 'recovery\.imported') 'O reconhecedor deve ler recovery.imported.'

# --- 7. O Sincronizar recusa sob a marca --------------------------------------------------
Assert-True ($sync -match 'ApiPlanOrphanMetadataRecovery\.IsImportedRecovery') 'O Sincronizar deve consultar a marca de intenção importada.'
$syncGuardIndex = $sync.IndexOf('IsImportedRecovery')
$structureIndex = $sync.IndexOf('ReadMetadataStructure(metadata)')
Assert-True ($syncGuardIndex -lt $structureIndex) 'O bloqueio do Sincronizar deve vir antes de ler a estrutura da metadata.'
Assert-True ($sync -match 'Sincronizacao bloqueada') 'O bloqueio deve explicar o motivo ao usuário.'

# --- 8. Posse verificada por dois sinais independentes ------------------------------------
Assert-True ($recovery -match 'ApiPlanOwnedObjectDescription\.IsCanonical') 'A elegibilidade deve exigir a Description própria do API Object.'
Assert-True ($recovery -match 'CallsGeneratedProcedures') 'A elegibilidade deve exigir que o Service Source chame as Procedures geradas.'
Assert-True ($recovery -match 'IsOwnedProcedure') 'O inventário de Procedures deve confirmar posse por Description.'
Assert-True ($recovery -match 'IsOwnedSdt') 'O inventário de SDTs deve confirmar posse por Description.'

# --- 9. Nada além do File é gravado --------------------------------------------------------
Assert-False ($recovery -match '(?m)^\s*(?!//).*\b(apiObject|procedure|sdt)\.Save\(\)') 'A recuperação não pode salvar API Object, Procedure ou SDT.'
$saveCount = ([regex]::Matches($recovery, '\.Save\(\)')).Count
Assert-True ($saveCount -eq 1) "A recuperação deve conter exatamente um Save(), o do File de metadata; encontrados $saveCount."

# --- 9b. Metadata existente: só a importada com API Object trocado é regravável ------------
# Em 2026-09-06 a metadata recuperada guardou o apiGuid do momento; o API Object foi removido
# e outro criado com o mesmo nome. A metadata passou a apontar para um GUID morto, o Wizard
# travou em OwnershipSchemaApiNameOrGuidMismatch e a recuperação não se oferecia — o File
# existia. Só se sai disso apagando o File à mão.
Assert-True ($recovery -match 'TryReadStaleRecoveredMetadata') 'Deve existir a avaliação da metadata existente.'
Assert-True ($recovery -match 'if \(!IsImportedRecovery\(metadata\)\)') 'Só metadata marcada como importada pode ser regravada: numa metadata completa o fingerprint B067 cobre o conteúdo inteiro, e corrigir só o apiGuid trocaria um bloqueio por outro.'
Assert-True ($recovery -match 'ownership\.apiGuid') 'A avaliação deve comparar o apiGuid gravado com o do API Object real.'
Assert-True ($recovery -match 'metadataMatches\.Length > 1') 'Metadata ambígua deve recusar a recuperação.'

# Regravar o File existente, nunca criar um segundo com o mesmo nome — dois Files homônimos
# bloqueiam Remover e Sincronizar por ambiguidade.
Assert-True ($recovery -match 'plan\.StaleFile \?\? new WikiFileKBObject') 'A recuperação deve reusar o File existente quando estiver corrigindo uma metadata importada.'

# --- 10. O que a remoção consome não pode divergir sem ninguém perceber --------------------
foreach ($token in @('ownership.transactionName', 'ownership.apiGuid', 'objects.procedures', 'objects.sdts.shared')) {
    Assert-True ($remover -match [regex]::Escape($token)) "O teste está desatualizado: ApiPlanGeneratedApiRemovalPlan não lê mais '$token'. Reveja o que a metadata recuperada precisa gravar."
}

Write-Output 'PASS: ApiPlanOrphanMetadataRecovery'
