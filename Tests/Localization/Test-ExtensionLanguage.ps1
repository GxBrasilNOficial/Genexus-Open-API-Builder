#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$languagePath = Join-Path $PSScriptRoot '..\..\Src\Domain\ExtensionLanguage.cs'
$uiTermsPath = Join-Path $PSScriptRoot '..\..\Src\Domain\ExtensionUiTerms.cs'
$localizationPath = Join-Path $PSScriptRoot '..\..\Src\Extension\ExtensionLocalization.cs'
$wizardPath = Join-Path $PSScriptRoot '..\..\Src\Extension\PrototypeWizardDialog.cs'
$finalReportPath = Join-Path $PSScriptRoot '..\..\Src\Extension\ApiPlanApplicationFinalReportDialog.cs'
$runtimeAssemblies = @([System.AppContext]::GetData('TRUSTED_PLATFORM_ASSEMBLIES') -split [System.IO.Path]::PathSeparator |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) })

Add-Type -Path @($languagePath, $uiTermsPath) -ReferencedAssemblies $runtimeAssemblies

function Assert-Equal {
    param($Expected, $Actual, [string]$Message)
    if ($Expected -ne $Actual) {
        throw "ASSERT_EQUAL_FAILED: $Message Expected='$Expected' Actual='$Actual'"
    }
}

function Assert-Contains {
    param([string]$Content, [string]$Expected, [string]$Message)
    if (-not $Content.Contains($Expected, [System.StringComparison]::Ordinal)) {
        throw "ASSERT_CONTAINS_FAILED: $Message Expected='$Expected'"
    }
}

$resolver = [GenexusOpenApiBuilder.Extension.Domain.ExtensionLanguageResolver]
$ptBr = [GenexusOpenApiBuilder.Extension.Domain.ExtensionLanguage]::PortugueseBrazil
$spanish = [GenexusOpenApiBuilder.Extension.Domain.ExtensionLanguage]::Spanish
$english = [GenexusOpenApiBuilder.Extension.Domain.ExtensionLanguage]::English

Assert-Equal $ptBr $resolver::Resolve('Portuguese (Brazil)', 'pt-BR', '1') 'Português brasileiro deve usar PT-BR.'
Assert-Equal $ptBr $resolver::Resolve('Portuguese', $null, 'Portuguese') 'Valor real da KB Portuguese deve usar PT-BR.'
Assert-Equal $ptBr $resolver::Resolve('Portuguese', $null, 'Portuguese (Brazil)') 'Nome português com indicação brasileira deve usar PT-BR.'
Assert-Equal $spanish $resolver::Resolve('Español', 'es-ES', '2') 'Espanhol deve usar espanhol.'
Assert-Equal $spanish $resolver::Resolve($null, $null, 'es') 'Tag bruta espanhola deve usar espanhol.'
Assert-Equal $english $resolver::Resolve('Portuguese (Portugal)', 'pt-PT', '3') 'Português de Portugal deve cair no inglês.'
Assert-Equal $english $resolver::Resolve('Français', 'fr-FR', '4') 'Outros idiomas devem cair no inglês.'
Assert-Equal $english $resolver::Resolve('Italiano', 'it-IT', '5') 'Italiano deve cair no inglês.'

$uiTerms = [GenexusOpenApiBuilder.Extension.Domain.ExtensionUiTerms]
Assert-Equal 'CreateRequest (criação)' ($uiTerms::RoleLabel($ptBr, 'CreateRequest')) 'PT-BR deve glossar CreateRequest.'
Assert-Equal 'UpdateRequest (atualização)' ($uiTerms::RoleLabel($ptBr, 'UpdateRequest')) 'PT-BR deve glossar UpdateRequest.'
Assert-Equal 'ListFilters (filtros)' ($uiTerms::RoleLabel($ptBr, 'ListFilters')) 'PT-BR deve glossar ListFilters.'
Assert-Equal 'Response (resposta)' ($uiTerms::RoleLabel($ptBr, 'Response')) 'PT-BR deve glossar Response.'
Assert-Equal 'CreateRequest (creación)' ($uiTerms::RoleLabel($spanish, 'CreateRequest')) 'Espanhol deve glossar CreateRequest.'
Assert-Equal 'UpdateRequest (actualización)' ($uiTerms::RoleLabel($spanish, 'UpdateRequest')) 'Espanhol deve glossar UpdateRequest.'
Assert-Equal 'ListFilters (filtros)' ($uiTerms::RoleLabel($spanish, 'ListFilters')) 'Espanhol deve glossar ListFilters.'
Assert-Equal 'Response (respuesta)' ($uiTerms::RoleLabel($spanish, 'Response')) 'Espanhol deve glossar Response.'
Assert-Equal 'CreateRequest' ($uiTerms::RoleLabel($english, 'CreateRequest')) 'Inglês não deve acrescentar gloss.'
Assert-Equal 'List' ($uiTerms::RoleLabel($ptBr, 'List')) 'Verbos REST não devem receber gloss.'
Assert-Equal 'Nível de segurança' ($uiTerms::PortugueseChrome('Security Level')) 'PT-BR deve traduzir o rótulo de segurança.'
Assert-Equal 'Tamanho padrão da página' ($uiTerms::PortugueseChrome('Default Page Size')) 'PT-BR deve traduzir o tamanho padrão da página.'
Assert-Equal 'Tamanho máximo da página' ($uiTerms::PortugueseChrome('Maximum Page Size')) 'PT-BR deve traduzir o tamanho máximo da página.'
Assert-Equal 'Segurança e paginação' ($uiTerms::PortugueseChrome('Seguranca e paginacao')) 'PT-BR deve acentuação no grupo de segurança.'

$localization = Get-Content -LiteralPath $localizationPath -Raw
$wizard = Get-Content -LiteralPath $wizardPath -Raw
$finalReport = Get-Content -LiteralPath $finalReportPath -Raw
Assert-Contains $localization "Habilitar Business Component altera a Transaction '{0}' na KB." 'O catálogo deve conter a confirmação localizada do Business Component.'
Assert-Contains $localization 'Enabling Business Component changes the Transaction' 'O catálogo deve conter a confirmação em inglês do Business Component.'
Assert-Contains $localization 'Habilitar Business Component cambia la Transaction' 'O catálogo deve conter a confirmação em espanhol do Business Component.'
Assert-Contains $localization '"<não definido>" => "<undefined>"' 'O fallback de path deve ter tradução em inglês.'
Assert-Contains $localization '"<não definido>" => "<no definido>"' 'O fallback de path deve ter tradução em espanhol.'
Assert-Contains $wizard '_texts.Translate("Habilitar Business Component altera a Transaction ''{0}'' na KB.' 'A confirmação B035 deve passar pelo catálogo.'
Assert-Contains $localization 'O serviço Delete exige Completar REST via Business Component no mesmo Apply.' 'O catálogo deve recusar Delete sem a etapa BC.'
Assert-Contains $localization 'The Delete service requires Completing REST via Business Component in the same Apply.' 'O catálogo deve recusar Delete sem a etapa BC em inglês.'
Assert-Contains $localization 'El servicio Delete exige Completar REST vía Business Component en el mismo Apply.' 'O catálogo deve recusar Delete sem a etapa BC em espanhol.'
Assert-Contains $finalReport '_texts.Translate("Objeto principal ''{0}'' não foi encontrado na KB.")' 'O erro de objeto ausente deve passar pelo catálogo.'
Assert-Contains $finalReport '_texts.Translate("Não foi possível abrir o objeto principal: {0}")' 'O erro de abertura deve passar pelo catálogo.'

Write-Output 'PASS: ExtensionLanguage'
