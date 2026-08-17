#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$languagePath = Join-Path $PSScriptRoot '..\..\Src\Domain\ExtensionLanguage.cs'
$outputLocalizationPath = Join-Path $PSScriptRoot '..\..\Src\Domain\ExtensionOutputLocalization.cs'
$runtimeAssemblies = @([System.AppContext]::GetData('TRUSTED_PLATFORM_ASSEMBLIES') -split [System.IO.Path]::PathSeparator |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) })

Add-Type -Path @($languagePath, $outputLocalizationPath) -ReferencedAssemblies $runtimeAssemblies

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw "ASSERT_TRUE_FAILED: $Message"
    }
}

$languageType = [AppDomain]::CurrentDomain.GetAssemblies() |
    ForEach-Object { $_.GetType('GenexusOpenApiBuilder.Extension.Domain.ExtensionLanguage', $false) } |
    Where-Object { $null -ne $_ } |
    Select-Object -First 1
$localizerType = [AppDomain]::CurrentDomain.GetAssemblies() |
    ForEach-Object { $_.GetType('GenexusOpenApiBuilder.Extension.Domain.ExtensionOutputLocalization', $false) } |
    Where-Object { $null -ne $_ } |
    Select-Object -First 1

Assert-True ($null -ne $languageType) 'O enum de idioma deve ser carregado.'
Assert-True ($null -ne $localizerType) 'O localizador do Output deve ser carregado.'

$translate = $localizerType.GetMethod('Translate')
$ptBr = [Enum]::Parse($languageType, 'PortugueseBrazil')
$spanish = [Enum]::Parse($languageType, 'Spanish')
$english = [Enum]::Parse($languageType, 'English')

$source = "[B033] Obrigatoriedade em memoria: Required marca membro obrigatorio no payload."
$portuguese = $translate.Invoke($null, [object[]] @($source, $ptBr))
Assert-True ($portuguese -ceq $source) 'Português brasileiro deve preservar o texto de origem.'

$spanishText = $translate.Invoke($null, [object[]] @($source, $spanish))
Assert-True ($spanishText.Contains('Obligatoriedad en memoria')) 'Espanhol deve traduzir o cabeçalho da obrigatoriedade.'
Assert-True (-not $spanishText.Contains('Obrigatoriedade em memoria')) 'Espanhol não deve manter o cabeçalho em português.'

$report = "API gerada com avisos.`nAvisos: Nenhuma alteração foi feita na KB."
$englishText = $translate.Invoke($null, [object[]] @($report, $english))
Assert-True ($englishText.Contains('API generated with warnings.')) 'Inglês deve traduzir o cabeçalho do relatório.'
Assert-True ($englishText.Contains('Warnings:')) 'Inglês deve traduzir a seção de avisos.'
Assert-True (-not $englishText.Contains('API gerada com avisos.')) 'Inglês não deve manter o cabeçalho em português.'

$summary = "[B081] Relatório final: Operação='Wizard', Resultado='Success', Criados=1, Avisos=0, DuraçãoMs=10, Título='API gerada com avisos.'."
$summarySpanish = $translate.Invoke($null, [object[]] @($summary, $spanish))
$summaryEnglish = $translate.Invoke($null, [object[]] @($summary, $english))
Assert-True ($summarySpanish.Contains("Operación='Wizard'")) 'Espanhol deve traduzir os campos técnicos do resumo B081.'
Assert-True ($summaryEnglish.Contains("Operation='Wizard'")) 'Inglês deve traduzir os campos técnicos do resumo B081.'
Assert-True ($summaryEnglish.Contains('Created=1')) 'Inglês deve traduzir a contagem de objetos criados.'

$diagnostic = "Causa='O GUID atual do API Object diverge do GUID registrado na metadata.' | ApiObjectGuid='22222222-2222-2222-2222-222222222222' | MetadataApiGuid='11111111-1111-1111-1111-111111111111'`nCausa principal: O GUID atual do API Object diverge do GUID registrado na metadata.`nAPI Object GUID atual: '22222222-2222-2222-2222-222222222222'`nGUID da metadata: '11111111-1111-1111-1111-111111111111'`nIntegridade B067: incompatível"
$diagnosticSpanish = $translate.Invoke($null, [object[]] @($diagnostic, $spanish))
$diagnosticEnglish = $translate.Invoke($null, [object[]] @($diagnostic, $english))
Assert-True ($diagnosticSpanish.Contains('Causa principal: El GUID actual del API Object difiere')) 'Espanhol deve traduzir a causa do bloqueio.'
Assert-True ($diagnosticSpanish.Contains('GUID actual del API Object:')) 'Espanhol deve traduzir o rótulo do GUID atual.'
Assert-True ($diagnosticEnglish.Contains('Primary cause: The current API Object GUID differs')) 'Inglês deve traduzir a causa do bloqueio.'
Assert-True ($diagnosticEnglish.Contains('B067 integrity: incompatible')) 'Inglês deve traduzir o estado da integridade.'

$baselineMessage = 'Alteracoes deliberadas pelo Wizard/Sincronizar atualizam esse baseline; alteracoes diretas nos objetos continuam bloqueadas antes de qualquer Save().'
$baselineEnglish = $translate.Invoke($null, [object[]] @($baselineMessage, $english))
Assert-True ($baselineEnglish.Contains('Intentional Wizard/Synchronize changes update this baseline')) 'Inglês deve explicar a diferença entre mudança intencional e edição direta.'
Assert-True (-not $baselineEnglish.Contains('Alteracoes deliberadas')) 'Inglês não deve manter o aviso de baseline em português.'

$generationDetail = "Reencontrar e validar: gerenciados=8, ausentes=0, planejados=8. A confirmacao continua obrigatoria antes de qualquer escrita. Folder preexistente 'NotaFiscalOpenApi' no contenedor correto sera reutilizado; a Description existente sera preservada e o Folder nunca sera removido pela remocao desta API."
$generationSpanish = $translate.Invoke($null, [object[]] @($generationDetail, $spanish))
Assert-True ($generationSpanish.Contains('Reencontrar y validar')) 'Espanhol deve traduzir a ação de reencontro.'
Assert-True ($generationSpanish.Contains('planificados=8')) 'Espanhol deve traduzir planejados.'
Assert-True ($generationSpanish.Contains('La confirmación sigue siendo obligatoria')) 'Espanhol deve traduzir a confirmação obrigatória.'
Assert-True ($generationSpanish.Contains('Carpeta preexistente')) 'Espanhol deve traduzir o aviso de Folder reutilizado.'
Assert-True (-not $generationSpanish.Contains('planejados=')) 'Espanhol não deve manter planejados.'

$generationEnglish = $translate.Invoke($null, [object[]] @($generationDetail, $english))
Assert-True ($generationEnglish.Contains('missing=0')) 'Inglês deve traduzir ausentes=.'
Assert-True (-not $generationEnglish.Contains('ausentes=')) 'Inglês não deve manter ausentes=.'
Assert-True ($generationEnglish.Contains('planned=8')) 'Inglês deve traduzir planejados.'

$prefsLoaded = "Preferencias do wizard carregadas da KB ativa: File='GxOpenApiBuilder_Settings'."
$prefsSpanish = $translate.Invoke($null, [object[]] @($prefsLoaded, $spanish))
Assert-True ($prefsSpanish.Contains('Preferencias del wizard cargadas de la KB activa:')) 'Espanhol deve traduzir o carregamento das preferências.'

$syncHeadline = 'Nenhuma sincronizacao necessaria.'
$syncHeadlineSpanish = $translate.Invoke($null, [object[]] @($syncHeadline, $spanish))
Assert-True ($syncHeadlineSpanish -eq 'Ninguna sincronización necesaria.') 'Espanhol deve traduzir o título de sincronização sem diferença.'
Assert-True (-not $syncHeadlineSpanish.Contains('Ningúna')) 'Espanhol não deve corromper Nenhuma via replace de Nenhum.'
Assert-True (-not $syncHeadlineSpanish.Contains('sincronizacao')) 'Espanhol não deve manter sincronizacao em português.'

$apiObjectExists = "API Object ja existe para Transaction='NotaFiscal'. Como B071-B073/B079 tambem foi confirmado, a atualizacao do API Object sera absorvida pelo preflight de Business Component."
$apiObjectExistsSpanish = $translate.Invoke($null, [object[]] @($apiObjectExists, $spanish))
Assert-True ($apiObjectExistsSpanish.Contains('ya existe para')) 'Espanhol deve traduzir ja existe para.'
Assert-True ($apiObjectExistsSpanish.Contains('la actualización del API Object será absorbida')) 'Espanhol deve traduzir a atualização residual em minúsculas.'
Assert-True (-not $apiObjectExistsSpanish.Contains('atualizacao')) 'Espanhol não deve manter atualizacao em português.'

$apiObjectExistsEnglish = $translate.Invoke($null, [object[]] @($apiObjectExists, $english))
Assert-True ($apiObjectExistsEnglish.Contains('As B071-B073/B079 was also confirmed')) 'Inglês deve traduzir Como B071... tambem foi confirmado como frase completa.'
Assert-True (-not $apiObjectExistsEnglish.Contains('tambem')) 'Inglês não deve deixar tambem após foi confirmado.'
Assert-True (-not $apiObjectExistsEnglish.Contains('Como B071')) 'Inglês não deve manter Como em português.'

$b031 = "[B031] Contrato de API da Transacao='NotaFiscal' em memoria: Services='List'."
$b031English = $translate.Invoke($null, [object[]] @($b031, $english))
Assert-True ($b031English.Contains("Transaction API contract='NotaFiscal' in memory:")) 'Inglês deve traduzir o residual em memoria do B031.'
Assert-True (-not $b031English.Contains('em memoria')) 'Inglês não deve manter em memoria no B031.'

$b038 = "[B038] ApiPlan cobre: PrimaryKey=1."
$b038English = $translate.Invoke($null, [object[]] @($b038, $english))
Assert-True ($b038English.Contains('ApiPlan covers:')) 'Inglês deve traduzir ApiPlan cobre.'

$b056 = "[B056] Descricoes reaplicadas no API Object real durante B071-B073/B079: Transaction='NotaFiscal'."
$b056English = $translate.Invoke($null, [object[]] @($b056, $english))
Assert-True ($b056English.Contains('Descriptions reapplied to the real API Object during B071-B073/B079:')) 'Inglês deve traduzir durante B071.'
Assert-True (-not $b056English.Contains('durante B071')) 'Inglês não deve manter durante em português.'

$collisionSource = "Bloqueado: 1 colisao(oes) externa(s), incompativel(is) ou ambigua(s) detectada(s). Nenhuma escrita sera permitida.`nConflitos (1):`n  - Nome='apiNotaFiscal' | Tipo='API Object' | Modulo='Root Module' | Folder='NotaFiscalOpenApi'"
$collisionEnglish = $translate.Invoke($null, [object[]] @($collisionSource, $english))
Assert-True ($collisionEnglish.Contains('external, incompatible, or ambiguous collision(s)')) 'Inglês deve traduzir o motivo da colisão.'
Assert-True ($collisionEnglish.Contains('No writing will be allowed.')) 'Inglês deve traduzir Nenhuma escrita sera permitida como frase completa.'
Assert-True (-not $collisionEnglish.Contains('No escrita')) 'Inglês não deve corromper Nenhuma escrita via replace de Nenhuma.'
Assert-True ($collisionEnglish.Contains('Conflicts (1):')) 'Inglês deve traduzir Conflitos.'
Assert-True ($collisionEnglish.Contains("Name='apiNotaFiscal'")) 'Inglês deve traduzir Nome=.'
Assert-True ($collisionEnglish.Contains("Type='API Object'")) 'Inglês deve traduzir Tipo=.'
Assert-True ($collisionEnglish.Contains("Module='Root Module'")) 'Inglês deve traduzir Modulo=.'

$etapa = "Etapa 'API Object' bloqueada na KB: Bloqueado: o API Object precisa estar disponivel antes."
$etapaEnglish = $translate.Invoke($null, [object[]] @($etapa, $english))
Assert-True ($etapaEnglish.Contains("Stage 'API Object' blocked in the KB:")) 'Inglês deve traduzir Etapa bloqueada.'
Assert-True ($etapaEnglish.Contains('the API Object must be available first.')) 'Inglês deve traduzir precisa estar disponivel.'

$b087 = "[B087] Diagnostico de posse do API Object (baseline de alteracao intencional): Causa='BaselineDescriptionMismatch'. ClausulaQueFalhou='BaselineDescriptionMismatch' DescriptionAtual='manual' DescriptionSentinel='apiNotaFiscal - by Genexus Open API Builder'"
$b087English = $translate.Invoke($null, [object[]] @($b087, $english))
Assert-True ($b087English.Contains('API Object ownership diagnostic (intentional-change baseline):')) 'Inglês deve traduzir o cabeçalho B087.'
Assert-True ($b087English.Contains("FailingClause='BaselineDescriptionMismatch'")) 'Inglês deve traduzir ClausulaQueFalhou=.'
Assert-True ($b087English.Contains("CurrentDescription='manual'")) 'Inglês deve traduzir DescriptionAtual=.'
Assert-True ($b087English.Contains("DescriptionSentinel='apiNotaFiscal - by Genexus Open API Builder'")) 'Inglês deve manter DescriptionSentinel=.'

$b087Fingerprint = "[B087] FingerprintDetalhe='FingerprintHashMismatch' FingerprintGravado='AAA' FingerprintRecalculado='BBB' IntegrityPresente=True"
$b087FingerprintEnglish = $translate.Invoke($null, [object[]] @($b087Fingerprint, $english))
Assert-True ($b087FingerprintEnglish.Contains("FingerprintDetail='FingerprintHashMismatch'")) 'Inglês deve traduzir FingerprintDetalhe=.'
Assert-True ($b087FingerprintEnglish.Contains("FingerprintStored='AAA'")) 'Inglês deve traduzir FingerprintGravado=.'
Assert-True ($b087FingerprintEnglish.Contains("FingerprintActual='BBB'")) 'Inglês deve traduzir FingerprintRecalculado=.'
Assert-True ($b087FingerprintEnglish.Contains('IntegrityPresent=True')) 'Inglês deve traduzir IntegrityPresente=.'

$voltar = "[B034] Voltar acionado no início do wizard único. Transaction='NotaFiscal' e decisões em memoria foram descartadas; nenhum ApiPlan foi criado. Nenhuma alteracao foi feita na KB."
$voltarEnglish = $translate.Invoke($null, [object[]] @($voltar, $english))
$voltarSpanish = $translate.Invoke($null, [object[]] @($voltar, $spanish))
Assert-True ($voltarEnglish.Contains('Back was used at the start of the single wizard.')) 'Inglês deve traduzir Voltar acionado no início.'
Assert-True ($voltarEnglish.Contains('and in-memory decisions were discarded;')) 'Inglês deve traduzir decisões em memoria descartadas.'
Assert-True ($voltarEnglish.Contains('no ApiPlan was created.')) 'Inglês deve traduzir nenhum ApiPlan foi criado.'
Assert-True ($voltarEnglish.Contains('No changes were made to the KB.')) 'Inglês deve traduzir Nenhuma alteracao foi feita na KB.'
Assert-True (-not $voltarEnglish.Contains('Voltar acionado')) 'Inglês não deve manter Voltar acionado.'
Assert-True (-not $voltarEnglish.Contains('nenhum ApiPlan')) 'Inglês não deve manter nenhum ApiPlan.'
Assert-True ($voltarSpanish.Contains('Se activó Atrás al inicio del asistente único.')) 'Espanhol deve traduzir Voltar acionado no início.'
Assert-True ($voltarSpanish.Contains('no se creó ningún ApiPlan.')) 'Espanhol deve traduzir nenhum ApiPlan foi criado.'
Assert-True (-not $voltarSpanish.Contains('Voltar acionado')) 'Espanhol não deve manter Voltar acionado.'

$abortoSelecao = "[B034] A Transaction do menu de contexto não foi reencontrada na Knowledge Base ativa. Estado anterior do wizard descartado; nenhuma escolha foi persistida."
$abortoSelecaoEnglish = $translate.Invoke($null, [object[]] @($abortoSelecao, $english))
$abortoSelecaoSpanish = $translate.Invoke($null, [object[]] @($abortoSelecao, $spanish))
Assert-True ($abortoSelecaoEnglish.Contains('The context-menu Transaction was not found again in the active Knowledge Base.')) 'Inglês deve traduzir Transaction do menu de contexto.'
Assert-True ($abortoSelecaoEnglish.Contains('no selection was persisted.')) 'Inglês deve traduzir nenhuma escolha minúscula.'
Assert-True (-not $abortoSelecaoEnglish.Contains('não foi reencontrada')) 'Inglês não deve manter não foi reencontrada.'
Assert-True ($abortoSelecaoSpanish.Contains('La Transaction del menú contextual no fue reencontrada en la Knowledge Base activa.')) 'Espanhol deve traduzir Transaction do menu de contexto.'
Assert-True (-not $abortoSelecaoSpanish.Contains('não foi reencontrada')) 'Espanhol não deve manter não foi reencontrada.'

$cancelar = "[B034] Wizard único cancelado ou fechado para Transaction='NotaFiscal'. Transaction e decisões em memoria descartadas; nenhum ApiPlan foi criado. Business Component foi habilitado por confirmacao explicita antes da saida; essa alteracao foi gravada na KB e nao foi revertida automaticamente."
$cancelarEnglish = $translate.Invoke($null, [object[]] @($cancelar, $english))
Assert-True ($cancelarEnglish.Contains('Single wizard canceled or closed for')) 'Inglês deve traduzir Wizard único cancelado ou fechado.'
Assert-True ($cancelarEnglish.Contains('Business Component was enabled by explicit confirmation before exit;')) 'Inglês deve traduzir habilitação explícita na saída.'
Assert-True (-not $cancelarEnglish.Contains('Wizard único')) 'Inglês não deve manter Wizard único.'

$passo2 = "[B031] Voltar acionado no Passo 2. Transaction='NotaFiscal' permaneceu selecionada em memoria; nenhuma escolha de contrato foi persistida."
$passo2English = $translate.Invoke($null, [object[]] @($passo2, $english))
Assert-True ($passo2English.Contains('Back was used on Step 2.')) 'Inglês deve traduzir Voltar no Passo 2.'
Assert-True ($passo2English.Contains('remained selected in memory;')) 'Inglês deve traduzir permaneceu selecionada.'
Assert-True ($passo2English.Contains('no contract selection was persisted.')) 'Inglês deve traduzir escolha de contrato.'

$passo2Cancel = "[B031] Wizard cancelado no Passo 2 para Transaction='NotaFiscal'. Escolhas em memoria descartadas; nenhuma alteracao foi feita na KB."
$passo2CancelEnglish = $translate.Invoke($null, [object[]] @($passo2Cancel, $english))
$passo2CancelSpanish = $translate.Invoke($null, [object[]] @($passo2Cancel, $spanish))
Assert-True ($passo2CancelEnglish.Contains('Wizard canceled on Step 2 for')) 'Inglês deve traduzir Wizard cancelado no Passo 2.'
Assert-True ($passo2CancelEnglish.Contains('In-memory selections discarded;')) 'Inglês deve traduzir Escolhas em memoria descartadas.'
Assert-True ($passo2CancelEnglish.Contains('no changes were made to the KB.')) 'Inglês deve traduzir nenhuma alteracao minúscula.'
Assert-True (-not $passo2CancelEnglish.Contains('nenhuma alteracao')) 'Inglês não deve manter nenhuma alteracao.'
Assert-True ($passo2CancelSpanish.Contains('Wizard cancelado en el Paso 2 para')) 'Espanhol deve traduzir Wizard cancelado no Passo 2.'
Assert-True ($passo2CancelSpanish.Contains('no se realizaron cambios en la KB.')) 'Espanhol deve traduzir nenhuma alteracao minúscula.'

$b035 = "[B035] Transaction='NotaFiscal' bloqueada: Business Component desabilitado e habilitacao explicita nao confirmada. Nenhum ApiPlan foi criado e nenhuma alteracao foi feita na KB."
$b035English = $translate.Invoke($null, [object[]] @($b035, $english))
Assert-True ($b035English.Contains('blocked: Business Component disabled and explicit enablement not confirmed.')) 'Inglês deve traduzir B035 bloqueada.'
Assert-True ($b035English.Contains('No ApiPlan was created and no changes were made to the KB.')) 'Inglês deve traduzir Nenhum ApiPlan foi criado e nenhuma alteracao.'

$exceptionMsg = "Criacao de API Object bloqueada: o ApiPlan em memoria nao pertence a Transaction selecionada atual. Nenhuma alteracao foi feita."
$exceptionSpanish = $translate.Invoke($null, [object[]] @($exceptionMsg, $spanish))
$exceptionEnglish = $translate.Invoke($null, [object[]] @($exceptionMsg, $english))
Assert-True ($exceptionSpanish.Contains('No se realizaron cambios.')) 'Espanhol deve traduzir Nenhuma alteracao foi feita.'
Assert-True (-not $exceptionSpanish.Contains('Ningúna')) 'Espanhol não deve corromper Nenhuma alteracao foi feita.'
Assert-True ($exceptionEnglish.Contains('No changes were made.')) 'Inglês deve traduzir Nenhuma alteracao foi feita.'
Assert-True (-not $exceptionEnglish.Contains('Noa')) 'Inglês não deve corromper Nenhuma alteracao foi feita.'

$otherMsg = "B055 bloqueado: a Procedure 'procTeste' foi salva, mas o Source persistido nao corresponde ao conteudo Business Component planejado. Nenhuma outra alteracao sera feita."
$otherSpanish = $translate.Invoke($null, [object[]] @($otherMsg, $spanish))
$otherEnglish = $translate.Invoke($null, [object[]] @($otherMsg, $english))
Assert-True ($otherSpanish.Contains('No se realizarán otros cambios.')) 'Espanhol deve traduzir Nenhuma outra alteracao sera feita.'
Assert-True ($otherEnglish.Contains('No other changes will be made.')) 'Inglês deve traduzir Nenhuma outra alteracao sera feita.'

Write-Output 'PASS: ExtensionOutputLocalization'
