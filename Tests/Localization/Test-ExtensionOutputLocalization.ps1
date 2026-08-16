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

Write-Output 'PASS: ExtensionOutputLocalization'
