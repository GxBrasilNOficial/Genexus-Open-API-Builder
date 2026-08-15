#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$sourcePath = Join-Path $PSScriptRoot '..\..\Src\Domain\ExtensionLanguage.cs'
$runtimeAssemblies = @([System.AppContext]::GetData('TRUSTED_PLATFORM_ASSEMBLIES') -split [System.IO.Path]::PathSeparator |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) })

Add-Type -Path $sourcePath -ReferencedAssemblies $runtimeAssemblies

function Assert-Equal {
    param($Expected, $Actual, [string]$Message)
    if ($Expected -ne $Actual) {
        throw "ASSERT_EQUAL_FAILED: $Message Expected='$Expected' Actual='$Actual'"
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

Write-Output 'PASS: ExtensionLanguage'
