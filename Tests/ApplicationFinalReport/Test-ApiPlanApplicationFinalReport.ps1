Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$helperPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanApplicationFinalReport.cs'
$identityPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanTransactionIdentity.cs'
$resolutionPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanMainObjectResolution.cs'
$persistenceCorePath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanPersistenceCore.cs'
$persistenceLogPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanPersistenceLog.cs'
$dialogPath = Join-Path $PSScriptRoot '..\..\Src\Extension\ApiPlanApplicationFinalReportDialog.cs'
function Read-DiagnosticsSourceBody {
    param([string]$Path)
    return [System.IO.File]::ReadAllText($Path) -replace '(?m)^#nullable enable\r?\n', '' -replace '(?m)^using [^\r\n]+\r?\n', '' -replace '(?m)^namespace GenexusOpenApiBuilder\.Extension\.Diagnostics;\r?\n', ''
}

$source = @(
    '#nullable enable'
    'using System;'
    'using System.Collections.Generic;'
    'using System.Globalization;'
    'using System.Linq;'
    'using System.Text;'
    'namespace GenexusOpenApiBuilder.Extension.Diagnostics {'
    (Read-DiagnosticsSourceBody $identityPath)
    (Read-DiagnosticsSourceBody $resolutionPath)
    (Read-DiagnosticsSourceBody $persistenceCorePath)
    (Read-DiagnosticsSourceBody $persistenceLogPath)
    (Read-DiagnosticsSourceBody $helperPath)
    '}'
) -join [Environment]::NewLine
Add-Type -TypeDefinition $source

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

function Assert-Equal {
    param($Expected, $Actual, [string]$Message)
    if ($Expected -ne $Actual) {
        throw "ASSERT_EQUAL_FAILED: $Message (expected='$Expected' actual='$Actual')"
    }
}

$collector = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanApplicationFinalReportCollector]::new('Wizard', 'Contrato', 'apiContrato')
$collector.AddFromWriteStatus('SDT', 'sdtContrato_API_Response', 'Created', 'OwnResponse')
$collector.AddFromWriteStatus('SDT', 'sdt_API_ErrorResponse', 'Reencountered', 'SharedErrorResponse')
$collector.AddFromWriteStatus('Procedure', 'procContrato_API_List', 'Created', 'List')
$collector.AddFromWriteStatus('API Object', 'apiContrato', 'Created')
$collector.AddCreated('Folder', 'ContratoOpenApi', 'criado pela extensão; apagar só se ficar vazio')
$collector.SetPersistedMainObject('apiContrato', [guid]'11111111-1111-1111-1111-111111111111')
$collector.AddWarning('Descricoes de servico usaram fallback em ingles.')

$report = $collector.Build([timespan]::FromMilliseconds(1250))
Assert-Equal ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanApplicationFinalOutcome]::SuccessWithWarnings) $report.Outcome 'Com avisos o outcome deve ser SuccessWithWarnings.'
Assert-Equal 'API gerada com avisos.' $report.Headline 'Headline parcial do Wizard.'
# Created: Response SDT, Procedure List, API Object e Folder = 4; Updated: ErrorResponse = 1
Assert-Equal 4 $report.CreatedCount 'Criados: Response, List Procedure, API Object e Folder.'
Assert-Equal 1 $report.UpdatedCount 'Atualizados: ErrorResponse reencontrado.'
Assert-Equal 0 $report.BlockedCount 'Sem bloqueios.'
Assert-Equal 1 $report.WarningCount 'Um aviso.'
Assert-True ($report.BuildOutputSummary() -match "\[B081\] Relatório final") 'Output summary deve citar B081.'
Assert-True ($report.BuildOutputSummary() -match "PersistedMainObjectGuid='11111111-1111-1111-1111-111111111111'") 'Output summary deve expor o GUID persistido.'

# B111/F3 P3: identificar o objeto principal não é persistir. Medido na IDE em 2026-09-14:
# operações bloqueadas antes da primeira gravação, e um Apply que não escreveu o API Object,
# reportavam PersistedMainObject preenchido — o GUID vinha só da identificação na KB.
$identifiedOnly = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanApplicationFinalReportCollector]::new('Wizard', 'Contrato', 'apiContrato')
$identifiedOnly.SetMainObject('apiContrato', [guid]'22222222-2222-2222-2222-222222222222')
Assert-Equal ([guid]'22222222-2222-2222-2222-222222222222') $identifiedOnly.MainObjectGuid 'Identificar deve registrar o objeto principal.'
Assert-Equal $null $identifiedOnly.PersistedMainObjectGuid 'Identificar não pode declarar persistência.'
$identifiedOnly.SetPersistedMainObject('apiContrato', [guid]'22222222-2222-2222-2222-222222222222')
Assert-Equal ([guid]'22222222-2222-2222-2222-222222222222') $identifiedOnly.PersistedMainObjectGuid 'Save confirmado declara persistência.'

# O defeito real vivia um nível abaixo: o collector deixava vazio, e o relatório repunha o
# valor por fallback para o objeto identificado. Medido na IDE em 2026-09-14 — um Apply sem
# gravação de API Object reportava PersistedMainObject preenchido. A trava é sobre Build().
$identifiedReport = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanApplicationFinalReportCollector]::new('Wizard', 'Contrato', 'apiContrato')
$identifiedReport.SetMainObject('apiContrato', [guid]'33333333-3333-3333-3333-333333333333')
$identifiedBuilt = $identifiedReport.Build([timespan]::FromMilliseconds(10))
Assert-Equal ([guid]'33333333-3333-3333-3333-333333333333') $identifiedBuilt.MainObjectGuid 'O relatório preserva o objeto principal identificado.'
Assert-Equal $null $identifiedBuilt.PersistedMainObjectGuid 'O relatório não pode repor o persistido por fallback do identificado.'
Assert-Equal $null $identifiedBuilt.PersistedMainObjectName 'O nome persistido também não pode vir por fallback.'
Assert-True ($identifiedBuilt.BuildOutputSummary() -match "PersistedMainObjectGuid=''") 'Sem gravação, o summary expõe o GUID persistido vazio.'
Assert-True ($report.BuildReadableBody() -match 'Resultado: Criados=4; Atualizados=1; Removidos=0.') 'Corpo legivel deve resumir os efeitos bem-sucedidos por contagem.'
Assert-True ($report.BuildReadableBody() -notmatch 'Guid persistido do objeto principal') 'Corpo legivel normal não deve exibir GUID técnico.'
Assert-True ($report.BuildReadableBody() -notmatch '\[Folder\] ContratoOpenApi') 'Corpo legivel normal não deve listar cada objeto criado.'

# Uma recuperação concluída precisa deixar no relatório seu resumo final sem virar aviso.
$recovery = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanApplicationFinalReportCollector]::new('Recuperar', 'Teste', 'apiTeste')
$recovery.AddDeletedItems(@('SDT:sdtTeste_API_UpdateRequest'))
$recovery.AddInformation('A remoção foi retomada e concluída: 15 objeto(s) saíram da KB nesta continuação.')
$recoveryReport = $recovery.Build([timespan]::FromMilliseconds(10))
$recoveryBody = $recoveryReport.BuildReadableBody()
Assert-Equal ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanApplicationFinalOutcome]::Success) $recoveryReport.Outcome 'Informação não deve transformar recuperação concluída em aviso.'
Assert-Equal 0 $recoveryReport.WarningCount 'Informação não incrementa Avisos.'
Assert-Equal 1 $recoveryReport.Information.Count 'Recuperação deve guardar um resumo final informativo.'
Assert-True ($recoveryBody -match 'Informações \(1\):') 'Relatório deve abrir a seção informativa quando houver resumo de recuperação.'
Assert-True ($recoveryBody -match 'A remoção foi retomada e concluída: 15 objeto\(s\) saíram da KB nesta continuação\.') 'Relatório deve mostrar o resumo da continuação.'

$persistenceSuccess = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanPersistenceLog]::new()
$successScope = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanPersistenceCore]::Begin($persistenceSuccess)
try {
    [void][GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanPersistenceCore]::Persist(
        'Save', 'Procedure', 'Procedures', 'procContrato_API_List',
        [GenexusOpenApiBuilder.Extension.Diagnostics.GuidIdentity]::new([guid]'55555555-5555-5555-5555-555555555555'),
        [Action] { },
        [Func[GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceConfirmation]] { [GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceConfirmation]::Confirmed('procContrato_API_List') })
}
finally {
    $successScope.Dispose()
}
$compactPersistence = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanApplicationFinalReportCollector]::new('Wizard', 'Contrato', 'apiContrato')
$compactPersistence.SetPersistenceLog($persistenceSuccess)
$compactBody = $compactPersistence.Build([timespan]::FromMilliseconds(10)).BuildReadableBody()
Assert-True ($compactBody -match 'Persistência: Confirmados=1; Pendências=0.') 'Sucesso deve resumir recibos por contagem.'
Assert-True ($compactBody -notmatch 'Receipt Sequence=') 'Sucesso não deve listar cada recibo técnico.'

$persistenceFailure = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanPersistenceLog]::new()
$failureScope = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanPersistenceCore]::Begin($persistenceFailure)
try {
    [void][GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanPersistenceCore]::Persist(
        'Save', 'Procedure', 'Procedures', 'procContrato_API_Create',
        [GenexusOpenApiBuilder.Extension.Diagnostics.GuidIdentity]::new([guid]'66666666-6666-6666-6666-666666666666'),
        [Action] { },
        [Func[GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceConfirmation]] { [GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceConfirmation]::Divergent('outro-guid', 'identidade divergente') })
}
finally {
    $failureScope.Dispose()
}
$detailedPersistence = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanApplicationFinalReportCollector]::new('Wizard', 'Contrato', 'apiContrato')
$detailedPersistence.SetPersistenceLog($persistenceFailure)
$detailedBody = $detailedPersistence.Build([timespan]::FromMilliseconds(10)).BuildReadableBody()
Assert-True ($detailedBody -match 'Diagnóstico de persistência:') 'Falha de confirmação deve abrir o diagnóstico técnico.'
Assert-True ($detailedBody -match 'procContrato_API_Create') 'Falha de confirmação deve identificar somente o recibo problemático.'

# S-B111 F1: exercita as decisões de identidade e resolução sem depender de uma KB/IDE.
$transactionGuid = [guid]'22222222-2222-2222-2222-222222222222'
$otherGuid = [guid]'33333333-3333-3333-3333-333333333333'
Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionIdentity]::Matches(
        $transactionGuid,
        'Contrato',
        $transactionGuid,
        'Contrato')) 'Identidade aceita somente GUID e nome correspondentes.'
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionIdentity]::Matches(
        $otherGuid,
        'Contrato',
        $transactionGuid,
        'Contrato')) 'Mesmo nome com GUID diferente deve ser bloqueado.'
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionIdentity]::Matches(
        $transactionGuid,
        'OutraTransaction',
        $transactionGuid,
        'Contrato')) 'Mesmo GUID com nome diferente deve ser bloqueado.'
$identityDiagnostic = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionIdentity]::BuildMismatchMessage(
    'B111/F1',
    $otherGuid,
    'Contrato',
    $transactionGuid,
    'Contrato')
Assert-True ($identityDiagnostic -match 'TransactionGuid') 'Diagnostico de identidade deve expor GUID esperado e atual.'

$confirmedCandidate = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMainObjectCandidate]::new($transactionGuid, 'apiContrato')
$confirmedResolution = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMainObjectResolver]::Resolve(
    $transactionGuid,
    'apiContrato',
    $confirmedCandidate,
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMainObjectCandidate[]]@())
Assert-True $confirmedResolution.IsConfirmed 'API Object lido pelo GUID planejado deve ser confirmado.'
Assert-Equal ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMainObjectResolutionKind]::ConfirmedByGuid) $confirmedResolution.Kind 'Resolucao confirmada deve registrar a origem por GUID.'
Assert-Equal $transactionGuid $confirmedResolution.Candidate.Guid 'Resolucao confirmada deve conservar o GUID planejado.'

$nominalOnly = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMainObjectCandidate[]]@(
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMainObjectCandidate]::new($otherGuid, 'apiContrato'))
$unconfirmedResolution = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMainObjectResolver]::Resolve(
    $transactionGuid,
    'apiContrato',
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMainObjectCandidate]$null,
    $nominalOnly)
Assert-False $unconfirmedResolution.IsConfirmed 'Candidato encontrado apenas por nome nao pode criar o link.'
Assert-Equal ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMainObjectResolutionKind]::GuidNotFound) $unconfirmedResolution.Kind 'Ausencia da releitura por GUID deve ser explicitada.'

$ambiguousCandidates = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMainObjectCandidate[]]@(
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMainObjectCandidate]::new($otherGuid, 'apiContrato'),
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMainObjectCandidate]::new([guid]'44444444-4444-4444-4444-444444444444', 'apiContrato'))
$ambiguousResolution = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMainObjectResolver]::Resolve(
    $transactionGuid,
    'apiContrato',
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMainObjectCandidate]$null,
    $ambiguousCandidates)
Assert-False $ambiguousResolution.IsConfirmed 'Ambiguidade nominal nao pode ser resolvida automaticamente.'
Assert-Equal ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMainObjectResolutionKind]::NameAmbiguousWithoutGuid) $ambiguousResolution.Kind 'Ambiguidade deve aparecer como estado de resolucao.'

$missingGuidAmbiguousResolution = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMainObjectResolver]::Resolve(
    [guid]::Empty,
    'apiContrato',
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMainObjectCandidate]$null,
    $ambiguousCandidates)
Assert-False $missingGuidAmbiguousResolution.IsConfirmed 'GUID ausente com candidatos multiplos tambem nao pode ser associado.'
Assert-Equal ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMainObjectResolutionKind]::NameAmbiguousWithoutGuid) $missingGuidAmbiguousResolution.Kind 'GUID ausente deve preservar o diagnostico de ambiguidade nominal.'

$mismatchedResolution = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMainObjectResolver]::Resolve(
    $transactionGuid,
    'apiContrato',
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMainObjectCandidate]::new($otherGuid, 'apiContrato'),
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMainObjectCandidate[]]@())
Assert-False $mismatchedResolution.IsConfirmed 'Objeto devolvido com GUID diferente nao pode ser associado.'
Assert-Equal ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMainObjectResolutionKind]::GuidNameMismatch) $mismatchedResolution.Kind 'Divergencia entre GUID planejado e objeto lido deve ser bloqueada.'

$long = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanApplicationFinalReportCollector]::new('Wizard', 'Teste', 'apiTeste')
$long.AddWarning('Descricoes de servico usaram fallback em ingles (Idioma principal da KB ainda nao validado por API publica; fallback tecnico em ingles registrado no ApiPlan.).')
$longBody = $long.Build([timespan]::FromMilliseconds(10)).BuildReadableBody()
Assert-True ($longBody -match '(?m)^  - Descricoes') 'Aviso longo inicia na primeira linha.'
Assert-True (($longBody -split "`n").Count -ge 2) 'Aviso longo deve quebrar em mais de uma linha no corpo legivel.'

$blocked = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanApplicationFinalReportCollector]::new('Wizard', 'Contrato', 'apiContrato')
$blocked.AddBlocked('Preflight', 'B063', 'colisao externa')
$blockedReport = $blocked.Build([timespan]::FromMilliseconds(40))
Assert-Equal ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanApplicationFinalOutcome]::Interrupted) $blockedReport.Outcome 'Bloqueio interrompe.'
Assert-Equal 'Geracao interrompida.' $blockedReport.Headline 'Headline de interrupcao do Wizard.'

$unconfirmed = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanApplicationFinalReportCollector]::new('Wizard', 'Contrato', 'apiContrato')
$unconfirmed.MarkApiSavePathEntered()
$unconfirmed.MarkApiSaveAttempted()
$unconfirmed.AddBlocked('API Object', 'apiContrato', 'releitura pós-Save não confirmou a identidade')
$unconfirmedReport = $unconfirmed.Build([timespan]::FromMilliseconds(40))
Assert-True $unconfirmedReport.ApiSaveAttempted 'O relatório deve preservar que houve tentativa de Save.'
Assert-True $unconfirmed.ApiSavePathEntered 'O coletor deve distinguir a entrada no caminho de Save da chamada física.'
Assert-Equal $null $unconfirmedReport.PersistedMainObjectGuid 'Tentativa não confirmada não pode produzir GUID persistido.'
Assert-True ($unconfirmedReport.BuildReadableBody() -match 'Tentativa de salvamento do API Object: sim') 'O corpo deve distinguir tentativa de persistência confirmada.'
try {
    $unconfirmed.SetPersistedMainObject('apiContrato', [guid]::Empty)
    throw 'GUID vazio aceito indevidamente.'
}
catch [System.ArgumentException] {
}

$sync = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanApplicationFinalReportCollector]::new('Sincronizar', 'Teste', 'apiTeste')
$sync.AddFromWriteStatus('SDT', 'sdtTeste_API_Response', 'Reencountered')
$syncReport = $sync.Build([timespan]::FromSeconds(2))
Assert-Equal 'API sincronizada com sucesso.' $syncReport.Headline 'Headline de Sync sem avisos.'

$unchanged = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanApplicationFinalReportCollector]::new('Wizard', 'DocumentoFiscal', 'apiDocumentoFiscal')
$unchanged.AddFromWriteStatus('SDT', 'sdtDocumentoFiscal_API_Response', 'Unchanged')
$unchanged.AddFromWriteStatus('Procedure', 'procDocumentoFiscal_API_List', 'Reencountered')
$unchangedReport = $unchanged.Build([timespan]::FromMilliseconds(100))
Assert-Equal 0 $unchangedReport.CreatedCount 'Unchanged nao conta como criado.'
Assert-Equal 1 $unchangedReport.UpdatedCount 'Unchanged de SDT nao entra em Atualizados; Procedure reencontrada entra.'
Assert-True ($unchangedReport.BuildReadableBody() -notmatch 'sdtDocumentoFiscal_API_Response') 'B081 nao lista SDT Unchanged.'

$remove = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanApplicationFinalReportCollector]::new('Remover', 'Teste', 'apiTeste')
$remove.AddDeletedItems(@('API:apiTeste', 'Procedure:procTeste_API_List', 'SDT:sdtTeste_API_Response', 'File:apiTeste_Metadata', 'Folder:TesteOpenApi'))
$remove.AddPreservedNonEmptyFolder('OtherOpenApi')
$removeReport = $remove.Build([timespan]::Zero)
Assert-Equal 'API removida com avisos.' $removeReport.Headline 'Folder preservado vira aviso.'
Assert-Equal 5 $removeReport.DeletedCount 'Cinco removidos reais.'
Assert-Equal 1 $removeReport.WarningCount 'Aviso do Folder preservado.'
Assert-Equal 'API Object' $removeReport.Deleted[0].ObjectKind 'API: mapeia para API Object.'
Assert-True ($removeReport.Warnings[0] -match "Folder 'OtherOpenApi' nao foi apagado") 'Aviso tipado nomeia o Folder preservado.'
Assert-True ($removeReport.BuildReadableBody($false) -notmatch '(?m)^API removida') 'Corpo sem headline nao repete o titulo.'
Assert-True ($removeReport.BuildReadableBody($false) -notmatch 'PreservedNonEmpty') 'String magica PreservedNonEmpty nao aparece no corpo.'
Assert-True (($removeReport.Deleted | ForEach-Object Name) -notcontains 'OtherOpenApi:PreservedNonEmpty') 'Folder preservado nao entra como removido.'
Assert-True (($removeReport.Deleted | ForEach-Object Name) -notcontains 'OtherOpenApi') 'Folder preservado tipado nao conta como Deleted.'

Assert-True ($source -match 'AddPreservedNonEmptyFolder') 'Relatorio B081 expoe Folder preservado tipado.'
Assert-True ($source -notmatch 'TryParsePreservedFolder') 'Parsing da string magica PreservedNonEmpty foi removido.'
$removerSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanGeneratedApiRemover.cs')
Assert-True ($removerSource -match 'preservedNonEmptyFolders\.Add\(target\.Name\)') 'Produtor tipado registra o nome do Folder.'
Assert-True ($removerSource -notmatch 'Folder:\{target\.Name\}:PreservedNonEmpty') 'Produtor nao emite mais string magica na lista de removidos.'
Assert-True ($removerSource -match 'PreservedNonEmptyFolders') 'Resultado de remocao expoe Folders preservados tipados.'
$packageFatiaB = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\..\Src\Extension\Package.cs')
Assert-True (([regex]::Matches($packageFatiaB, 'AddPreservedNonEmptyFolders')).Count -ge 4) 'Package entrega Folders preservados tipados nos caminhos Remover/Recuperar.'

$dialogSource = Get-Content -Raw -LiteralPath $dialogPath
Assert-True ($dialogSource -match 'EnsureBodyScrollBars') 'Dialogo B081 deve recalcular a rolagem apos o layout.'
Assert-True ($dialogSource -match 'GetPositionFromCharIndex') 'Dialogo B081 deve medir a ultima linha visual do corpo.'
Assert-True ($dialogSource -match 'ScrollBars\.Vertical') 'Dialogo B081 deve habilitar rolagem vertical quando o corpo exceder a area.'
Assert-True ($dialogSource -match 'WidthScale = 1\.5') 'Dialogo B081 deve ampliar a largura em pelo menos 50%.'
Assert-True ($dialogSource -match 'Size = new Size\(preferredWidth, maxHeight\)') 'Dialogo B081 deve abrir com a altura disponivel da tela.'
Assert-True ($dialogSource -match 'StartPosition = FormStartPosition\.Manual') 'Dialogo B081 deve posicionar-se manualmente para respeitar o monitor atual.'
Assert-True ($dialogSource -match 'IWin32Window\? owner') 'Dialogo B081 deve receber a janela owner da IDE.'
Assert-True ($dialogSource -match '_owner\.Handle') 'Dialogo B081 deve priorizar o monitor da janela owner.'
Assert-True ($dialogSource -match 'Process\.GetCurrentProcess\(\)\.MainWindowHandle') 'Dialogo B081 deve usar a janela principal do processo como fallback.'
Assert-True ($dialogSource -notmatch 'Screen\.FromPoint\(Cursor\.Position\)\.WorkingArea') 'Dialogo B081 não deve escolher o monitor pela posição do cursor.'
Assert-True ($dialogSource -match 'MaximumSize = new Size\(maxWidth, maxHeight\)') 'Dialogo B081 deve limitar o tamanho maximo a area util da tela.'
Assert-True ($dialogSource -match 'working\.Height - 64') 'Dialogo B081 deve reservar margem vertical na area util.'
Assert-True ($dialogSource -match 'working\.Width - 32') 'Dialogo B081 deve reservar margem horizontal na area util.'
Assert-True ($dialogSource -match 'FitToCurrentWorkingArea\(\)') 'Dialogo B081 deve recalcular os limites depois de criar o handle.'
Assert-True ($dialogSource -match 'CenterInWorkingArea\(working\)') 'Dialogo B081 deve manter os botoes dentro da area util do monitor.'

$packageSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\..\Src\Extension\Package.cs')
$apiPlanSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\..\Src\Domain\ApiPlan.cs')
$identitySource = Get-Content -Raw -LiteralPath $identityPath
$resolutionSource = Get-Content -Raw -LiteralPath $resolutionPath
$sdtWriterSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanSdtWriter.cs')
$businessComponentWriterSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanBusinessComponentWriter.cs')
$listWriterSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanListProcedureWriter.cs')
$apiObjectWriterSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanApiObjectWriter.cs')
$metadataWriterSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanMetadataFileWriter.cs')
$preflightSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanWritePreflight.cs')
$existingApiReaderSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\PrototypeWizardExistingApiContractReader.cs')
$transientContextSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanTransientApiContext.cs')
Assert-True ($packageSource -match 'AppendPlanSideEffects\(collector, apiPlan\)') 'B081 deve anexar efeitos de Folder e Transaction ao relatorio final.'
Assert-True ($packageSource -match 'conflict\.DiagnosticDetails') 'B081 deve preservar o diagnóstico detalhado de colisões no Output.'
Assert-True ($packageSource -match 'onSdtWrite: item => AppendSdtWriteItemToReport') 'B081 deve receber SDTs escritos internamente por B055/B070.'
Assert-True ($packageSource -match 'ResolveFinalReportOwner') 'B081 deve resolver o owner do relatório em um único ponto.'
Assert-True ($packageSource -match 'ExtensionIdeScreenPlacement.ResolveOwner') 'Owner da IDE vem do handle principal do GeneXus, nao de ActiveForm.'
Assert-True ($packageSource -notmatch 'OpenForms') 'B081 não deve escolher owner pela primeira janela visível de OpenForms.'
Assert-True ($packageSource -match 'dialog\.ShowDialog\(owner\)') 'B081 deve abrir o relatório como diálogo da janela owner.'
Assert-True ($packageSource -match 'CenterOnIdeScreen\(dialog, wizardOwner\)') 'Wizard deve centralizar no monitor da IDE antes do ShowDialog.'
Assert-True ($apiPlanSource -match 'SharedSdtFolderWasCreated') 'ApiPlan deve transportar a criacao do Folder compartilhado.'
Assert-True ($sdtWriterSource -match 'SharedSdtFolderWasCreated = true') 'Writer de SDT deve marcar o Folder compartilhado criado.'
Assert-True ($businessComponentWriterSource -match 'onSdtWrite') 'Writer de Business Component deve propagar SDTs escritos.'
Assert-True ($listWriterSource -match 'onSdtWrite') 'Writer de List deve propagar SDTs escritos.'

# S-B111 F1: contexto transitório, ordem de gravação e guard de API/List.
Assert-True ($transientContextSource -match 'PlannedApiGuid') 'F1 deve transportar o GUID planejado no contexto transitório.'
Assert-True ($apiObjectWriterSource -match 'PrepareOrReencounter') 'F1 deve preparar o API Object antes da gravação final.'
Assert-True ($apiObjectWriterSource -match 'SavePreparedApiObject') 'F1 deve separar preparação e Save do API Object.'
Assert-True ($apiObjectWriterSource -match 'apiPlan\.PlannedApiGuid = api\.Guid') 'F1 deve fixar o GUID retornado pela criação, sem atribuí-lo manualmente.'
Assert-True ($apiObjectWriterSource -match 'api\.Guid == Guid\.Empty') 'B111 deve bloquear API novo ou reencontrado sem identidade GUID estável.'
Assert-True ($apiObjectWriterSource -match 'RequirePersistedApiObject') 'B111 deve confirmar o API Object por releitura e contrato de identidade após o Save.'
Assert-True ($businessComponentWriterSource -match 'RequirePersistedApiObject') 'BC deve confirmar o API Object por releitura antes de concluir o callback.'
Assert-True ($listWriterSource -match 'RequirePersistedApiObject') 'List deve confirmar o API Object por releitura antes de concluir o callback.'
Assert-True ($apiObjectWriterSource -match 'onApiPhysicalSave\?\.Invoke\(api\.Guid\)') 'B111 deve registrar o Save físico por callback próprio.'
Assert-True ($businessComponentWriterSource -match 'onApiPhysicalSave\?\.Invoke\(api\.Guid\)') 'BC deve registrar o Save físico por callback próprio.'
Assert-True ($listWriterSource -match 'onApiPhysicalSave\?\.Invoke\(api\.Guid\)') 'List deve registrar o Save físico por callback próprio.'
Assert-Equal 3 ([regex]::Matches($packageSource, 'report\?\.MarkApiSavePathEntered\(\)').Count) 'Os tres fluxos gerenciados devem registrar a entrada no caminho de Save antes do writer.'
Assert-Equal 3 ([regex]::Matches($packageSource, 'onApiSaveAttempted: \(\) => report\?\.MarkApiSaveAttempted\(\)').Count) 'Os tres writers devem marcar a tentativa imediatamente antes da chamada fisica.'
Assert-Equal 3 ([regex]::Matches($apiObjectWriterSource + $businessComponentWriterSource + $listWriterSource, 'onApiSaveAttempted\?\.Invoke\(\)').Count) 'Cada writer deve notificar a tentativa no ponto da chamada fisica.'
Assert-Equal 2 ([regex]::Matches($packageSource, 'onSaveCompleted: null').Count) 'Em sucesso normal, os callbacks B111 não devem despejar um trace por Save físico no Output.'
Assert-True ($packageSource -match 'if \(forceShow\)\s*\{\s*output\.Show\(outputId\);\s*\}') 'A exibição do Output deve ser condicional ao modo solicitado.'
Assert-True ($packageSource -notmatch 'WriteOutputWithoutShow') 'Sem trace B111 por Save, a variante de Output sem exibição deixa de ser necessária.'
Assert-True ($packageSource -match 'if \(!_log\.HasAnomaly\)') 'O dump B109 deve ser publicado somente quando a sonda detectar uma anomalia.'
Assert-True ($packageSource -match 'if \(System\.Diagnostics\.Debugger\.IsAttached\)') 'A telemetria detalhada B082 deve ficar fora do Output normal.'
Assert-True ($packageSource -notmatch '\[B081\] Criado:') 'O Output normal não deve repetir a lista de itens criados já consolidada no relatório final.'
Assert-True ($packageSource -notmatch '\[B081\] Atualizado:') 'O Output normal não deve repetir a lista de itens atualizados já consolidada no relatório final.'
Assert-True ($packageSource -notmatch '\[B081\] Removido:') 'O Output normal não deve repetir a lista de itens removidos já consolidada no relatório final.'

$apiSaveIndex = $apiObjectWriterSource.IndexOf('api.Save();', [System.StringComparison]::Ordinal)
$apiAttemptIndex = $apiObjectWriterSource.IndexOf('onApiSaveAttempted?.Invoke()', [System.StringComparison]::Ordinal)
$apiPhysicalIndex = $apiObjectWriterSource.IndexOf('onApiPhysicalSave?.Invoke(api.Guid)', [System.StringComparison]::Ordinal)
$apiReadIndex = $apiObjectWriterSource.IndexOf('var persisted = RequirePersistedApiObject', [System.StringComparison]::Ordinal)
$apiCompletedIndex = $apiObjectWriterSource.IndexOf('onApiSaveCompleted?.Invoke(persisted.Guid)', [System.StringComparison]::Ordinal)
Assert-True ($apiSaveIndex -ge 0 -and $apiSaveIndex -lt $apiPhysicalIndex) 'B111 deve registrar o Save físico depois da chamada a API.Save().'
Assert-True ($apiAttemptIndex -ge 0 -and $apiAttemptIndex -lt $apiSaveIndex) 'B111 deve marcar a tentativa imediatamente antes da chamada a API.Save().'
Assert-True ($apiPhysicalIndex -lt $apiReadIndex -and $apiReadIndex -lt $apiCompletedIndex) 'B111 só deve confirmar o callback após a releitura do API Object.'

$bcSaveIndex = $businessComponentWriterSource.IndexOf('api.Save();', [System.StringComparison]::Ordinal)
$bcAttemptIndex = $businessComponentWriterSource.IndexOf('onApiSaveAttempted?.Invoke()', [System.StringComparison]::Ordinal)
$bcPhysicalIndex = $businessComponentWriterSource.IndexOf('onApiPhysicalSave?.Invoke(api.Guid)', [System.StringComparison]::Ordinal)
$bcReadIndex = $businessComponentWriterSource.IndexOf('var persisted = ApiPlanApiObjectWriter.RequirePersistedApiObject', [System.StringComparison]::Ordinal)
$bcCompletedIndex = $businessComponentWriterSource.IndexOf('onApiSaveCompleted?.Invoke(persisted.Guid)', [System.StringComparison]::Ordinal)
Assert-True ($bcSaveIndex -ge 0 -and $bcSaveIndex -lt $bcPhysicalIndex) 'BC deve registrar o Save físico depois da chamada a API.Save().'
Assert-True ($bcAttemptIndex -ge 0 -and $bcAttemptIndex -lt $bcSaveIndex) 'BC deve marcar a tentativa imediatamente antes da chamada a API.Save().'
Assert-True ($bcPhysicalIndex -lt $bcReadIndex -and $bcReadIndex -lt $bcCompletedIndex) 'BC só deve confirmar o callback após a releitura e validação do API Object.'

$listSaveIndex = $listWriterSource.IndexOf('api.Save();', [System.StringComparison]::Ordinal)
$listAttemptIndex = $listWriterSource.IndexOf('onApiSaveAttempted?.Invoke()', [System.StringComparison]::Ordinal)
$listPhysicalIndex = $listWriterSource.IndexOf('onApiPhysicalSave?.Invoke(api.Guid)', [System.StringComparison]::Ordinal)
$listReadIndex = $listWriterSource.IndexOf('var persisted = ApiPlanApiObjectWriter.RequirePersistedApiObject', [System.StringComparison]::Ordinal)
$listCompletedIndex = $listWriterSource.IndexOf('onApiSaveCompleted?.Invoke(persisted.Guid)', [System.StringComparison]::Ordinal)
Assert-True ($listSaveIndex -ge 0 -and $listSaveIndex -lt $listPhysicalIndex) 'List deve registrar o Save físico depois da chamada a API.Save().'
Assert-True ($listAttemptIndex -ge 0 -and $listAttemptIndex -lt $listSaveIndex) 'List deve marcar a tentativa imediatamente antes da chamada a API.Save().'
Assert-True ($listPhysicalIndex -lt $listReadIndex -and $listReadIndex -lt $listCompletedIndex) 'List só deve confirmar o callback após a releitura e validação do API Object.'
Assert-True ($metadataWriterSource -match 'apiPlan\.PlannedApiGuid = apiObject\.Guid') 'Metadata-only deve transportar o GUID lido do API Object reencontrado.'
Assert-True ($apiObjectWriterSource -match 'PreflightExistingApiObjectStrict') 'F1 deve ter um preflight dedicado para API existente, sem criar objeto transitório quando a persistencia esta desabilitada.'
Assert-True ($apiObjectWriterSource -match 'PreflightB054ApiObjectStrict') 'F1 deve validar o contrato B054 do API Object antes das fases consumidoras.'
Assert-True ($apiObjectWriterSource -match 'ApiPlanMainObjectResolver\.Resolve') 'A preparacao do API Object deve validar a identidade com o resolvedor GUID-first.'
Assert-True ($metadataWriterSource -match 'ApiPlanApiObjectWriter\.PreflightExistingApiObjectStrict') 'Metadata-only deve reencontrar o API Object pelo GUID planejado, sem associacao autoritativa por nome.'
Assert-True ($preflightSource -match '!generateApiObject && \(generateMetadata \|\| applyList \|\| applyBusinessComponent\)') 'Consumidores sem persistencia de API devem exigir o API Object existente antes do primeiro Save.'
Assert-True ($preflightSource -match 'generateApiObject && !applyList && !applyBusinessComponent') 'API-only deve validar a compatibilidade B054 antes de qualquer escrita de SDT ou Procedure.'
Assert-True ($preflightSource -match 'ApiPlanApiObjectWriter\.PreflightB054ApiObjectStrict') 'O preflight agregado deve executar a guarda B054 antes das fases de escrita.'
Assert-True ($preflightSource -match 'PreflightExistingApiObjectStrict') 'O gate reduzido deve bloquear API ausente ou incompativel antes das fases consumidoras.'
Assert-True ($apiPlanSource -match 'apiPlan\.PlannedApiGuid = existingApiContract\?\.ApiGuid') 'ApiPlan deve carregar o GUID do contrato existente antes do Apply/Sync.'
Assert-True ($existingApiReaderSource -match 'ReadGuid\(metadata\.Document, "ownership\.apiGuid"\)') 'O reencontro deve priorizar o GUID registrado na metadata.'
Assert-True ($existingApiReaderSource -match 'metadata\.Document\?\.SelectToken\("ownership\.apiGuid"\)') 'GUID registrado e invalido ou ausente nao deve cair silenciosamente no reencontro por nome.'
Assert-True ($existingApiReaderSource -match 'public Guid\? ApiGuid') 'O contrato existente deve transportar a identidade do API Object para o ApiPlan.'
Assert-True ($sdtWriterSource -match 'StrictReencounter') 'F1 deve possuir reencontro estrito de SDTs.'
Assert-True ($sdtWriterSource -match 'PreflightStrict') 'F1 deve validar SDTs estritamente antes das gravações consumidoras.'
Assert-True ($sdtWriterSource -match 'PreflightExistingStructures') 'F1 deve validar a estrutura dos SDTs existentes antes da geração normal.'
Assert-True ($preflightSource -match 'generateSdts && requiresConsumersOrApi') 'F1 deve validar SDTs existentes antes de permitir a geração normal com consumidores.'
Assert-True ($packageSource -match 'ApiPlanWritePreflight\.ValidateForF1') 'F1 deve executar o gate reduzido antes das fases de escrita.'
Assert-True ($packageSource -match 'ApiPlanTransientApiSelection') 'F1 deve transportar as flags da seleção no contexto transitório.'
Assert-True ($transientContextSource -match 'TransactionGuid') 'F1 deve transportar a identidade da Transaction.'
Assert-True ($transientContextSource -match 'ApplicationId') 'F1 deve transportar a identidade da aplicação.'
Assert-True ($transientContextSource -match 'ExistingApiServiceGroupSource') 'F1 deve capturar o Source persistido antes da mutação.'
Assert-True ($packageSource -match 'TryPrepareApiObject') 'Sync/Wizard devem usar preparação transitória do API Object.'
Assert-True ($packageSource -match 'TryResolveMainObjectFromKb\(collector, designModel, apiPlan\)') 'B081 deve resolver o objeto principal com o ApiPlan gerenciado disponível.'
Assert-True ($packageSource -match 'collector\.ApiSavePathEntered && !collector\.PersistedMainObjectGuid\.HasValue') 'B081 não deve associar por releitura nominal depois de entrar no caminho de Save sem confirmação.'
Assert-True ($packageSource -match 'API\.Get\(designModel, apiPlan\.PlannedApiGuid\.Value\)') 'B081 deve confirmar o API Object pelo GUID planejado.'
Assert-True ($packageSource -match 'ApiPlanMainObjectResolver\.Resolve') 'B081 deve aplicar a decisão central de resolução segura.'
Assert-True ($packageSource -match 'if \(apiPlan is null\)') 'A compatibilidade nominal deve ficar restrita aos fluxos legados sem ApiPlan.'
Assert-True ($identitySource -match 'actualTransactionGuid == expectedTransactionGuid') 'A guarda de identidade deve comparar GUID real e planejado.'
Assert-True ($identitySource -match 'StringComparison\.Ordinal') 'A guarda de identidade deve comparar o nome com semantica ordinal.'
Assert-True ($resolutionSource -match 'NameAmbiguousWithoutGuid') 'A resolução deve possuir estado explícito para ambiguidade nominal.'
Assert-True ($resolutionSource -match 'associacao por nome nao foi aplicada') 'A resolução deve impedir fallback nominal autoritativo.'
Assert-Equal 2 ([regex]::Matches($packageSource, 'var b111ManagedApply = selection\.GenerateApiObject \|\| selection\.GenerateMetadata \|\| selection\.ApplyBusinessComponent \|\| selection\.ApplyList;').Count) 'Sync e Wizard devem usar o mesmo predicado de aplicação gerenciada da F1.'
Assert-True ($packageSource -match 'context: out syncApiContext') 'Sync deve propagar o contexto transitório aos consumidores.'
Assert-True ($packageSource -match 'context: out wizardApiContext') 'Wizard deve propagar o contexto transitório aos consumidores.'
Assert-True ($packageSource -notmatch 'selection\.GenerateApiObject && selection\.ApplyBusinessComponent') 'F1 não deve manter a decisão antiga por existência nominal antes do consumidor.'
Assert-True ($businessComponentWriterSource -match 'ApiPlanSdtWriter\.WriteMode\.StrictReencounter') 'BC deve reencontrar SDTs sem criar/corrigir durante o consumo.'
Assert-True ($listWriterSource -match 'ApiPlanSdtWriter\.WriteMode\.StrictReencounter') 'List deve reencontrar SDTs sem criar/corrigir durante o consumo.'
Assert-True ($businessComponentWriterSource -match 'apiContext\.PersistApiObject') 'BC deve decidir o Save do API pelo contexto transitório.'
Assert-True ($listWriterSource -match 'apiContext\.PersistApiObject') 'List deve decidir o Save do API pelo contexto transitório.'
Assert-True ($businessComponentWriterSource -match 'if \(apiContext is null \|\| apiContext\.PersistApiObject\)') 'BC sem persistência de API não deve validar variáveis de uma mutação que não ocorrerá.'
Assert-True ($listWriterSource -match 'if \(apiContext is null \|\| apiContext\.PersistApiObject\)') 'List sem persistência de API não deve validar variáveis de uma mutação que não ocorrerá.'
Assert-True ($listWriterSource -match 'apiContext\.BusinessComponentParticipated') 'List deve usar o fato da participação do BC no guard de parâmetros.'
Assert-True ($listWriterSource -match 'apiContext\.ApiWasCreated') 'List deve distinguir API novo de API existente antes da mutação.'
Assert-True ($businessComponentWriterSource -match 'onSaveCompleted') 'BC deve manter o callback de conclusão para diagnóstico programático quando solicitado.'
Assert-True ($listWriterSource -match 'onSaveCompleted') 'List deve manter o callback de conclusão para diagnóstico programático quando solicitado.'

$createApiStart = $packageSource.IndexOf('private static bool TryCreateApiObject', [System.StringComparison]::Ordinal)
$prepareApiStart = $packageSource.IndexOf('private static bool TryPrepareApiObject', [System.StringComparison]::Ordinal)
$metadataStart = $packageSource.IndexOf('private static bool TryWriteMetadataFile', [System.StringComparison]::Ordinal)
$createApiBlock = $packageSource.Substring($createApiStart, $prepareApiStart - $createApiStart)
$prepareApiBlock = $packageSource.Substring($prepareApiStart, $metadataStart - $prepareApiStart)
Assert-True ($createApiBlock -match 'catch \(ApiPlanBusyAbortedException\)') 'Criação de API deve propagar cancelamento do usuário ao fluxo externo.'
Assert-True ($prepareApiBlock -match 'catch \(ApiPlanBusyAbortedException\)') 'Preparação de API deve propagar cancelamento do usuário ao fluxo externo.'

$bcSaveBlock = [regex]::Match($businessComponentWriterSource, '(?s)var saveSteps = new List<ApiPlanSaveStep>.*?ApiPlanSaveStepExecutor.Execute').Value
$listSaveBlock = [regex]::Match($listWriterSource, '(?s)var saveSteps = new List<ApiPlanSaveStep>.*?ApiPlanSaveStepExecutor.Execute').Value
$bcProcedureIndex = $bcSaveBlock.IndexOf('CreateProcedureSaveStep(model, kbIndex, plan, get,', [System.StringComparison]::Ordinal)
$bcApiIndex = $bcSaveBlock.IndexOf('saveSteps.Add(CreateApiSaveStep', [System.StringComparison]::Ordinal)
$listProcedureIndex = $listSaveBlock.IndexOf('CreateProcedureSaveStep(model, kbIndex, plan, procedure,', [System.StringComparison]::Ordinal)
$listApiIndex = $listSaveBlock.IndexOf('saveSteps.Add(CreateApiSaveStep', [System.StringComparison]::Ordinal)
Assert-True ($bcProcedureIndex -ge 0) 'BC deve salvar Procedures no bloco de etapas.'
Assert-True ($bcApiIndex -gt $bcProcedureIndex) 'BC deve colocar o API Save depois das Procedures.'
Assert-True ($listProcedureIndex -ge 0) 'List deve salvar a Procedure no bloco de etapas.'
Assert-True ($listApiIndex -gt $listProcedureIndex) 'List deve colocar o API Save depois da Procedure.'

$reportSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanApplicationFinalReport.cs')
Assert-True ($reportSource -match 'PlannedApiName') 'B081 deve registrar o nome planejado do API Object.'
Assert-True ($reportSource -match 'PersistedMainObjectGuid') 'B081 deve registrar o GUID persistido do objeto principal.'
Assert-True ($reportSource -match 'ApiSaveCount') 'B081 deve registrar a contagem física de Save do API Object.'
Assert-True ($reportSource -match 'ApiSaveAttempted') 'B081 deve distinguir tentativa de Save físico de persistência confirmada.'
Assert-True ($transientContextSource -match 'api\.Guid == Guid\.Empty') 'O contexto transitório deve rejeitar API Object sem GUID estável.'

$placementSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\..\Src\Extension\ExtensionIdeScreenPlacement.cs')
$wizardSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\..\Src\Extension\PrototypeWizardDialog.cs')
$busySource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\..\Src\Extension\ExtensionBusyProgressDialog.cs')
Assert-True ($placementSource -match 'Process\.GetCurrentProcess\(\)\.MainWindowHandle') 'Placement deve preferir a janela principal do GeneXus.'
Assert-True ($placementSource -match 'Screen\.FromHandle\(owner\.Handle\)') 'Placement deve usar o monitor do handle da IDE.'
Assert-True ($placementSource -notmatch 'Screen\.FromPoint\(Cursor\.Position\)') 'Placement nao deve seguir o cursor.'
Assert-True ($wizardSource -match 'StartPosition = FormStartPosition\.Manual') 'Wizard deve posicionar-se manualmente no monitor da IDE.'
Assert-True ($wizardSource -notmatch 'FormStartPosition\.CenterParent') 'CenterParent nao posiciona HWND da IDE.'
Assert-True ($busySource -match 'StartPosition = FormStartPosition\.Manual') 'Quadro de progresso deve posicionar-se no monitor da IDE.'
Assert-True ($busySource -notmatch 'FormStartPosition\.CenterScreen') 'CenterScreen abre no monitor primário.'

Write-Host 'Test-ApiPlanApplicationFinalReport.ps1 OK'
