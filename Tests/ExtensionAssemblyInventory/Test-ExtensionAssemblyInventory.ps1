#requires -Version 7.4

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$tool = Join-Path $repositoryRoot 'Tools\Test-ExtensionAssemblyInventory.ps1'
if (-not (Test-Path -LiteralPath $tool -PathType Leaf)) {
    throw "Ferramenta de inventário não encontrada: $tool"
}

function Assert-Equal {
    param(
        [Parameter(Mandatory)] [object]$Actual,
        [Parameter(Mandatory)] [object]$Expected,
        [Parameter(Mandatory)] [string]$Message
    )

    if ($Actual -ne $Expected) {
        throw "ASSERT_EQUAL_FAILED: $Message Expected='$Expected'; Actual='$Actual'"
    }
}

function Assert-SequenceEqual {
    param(
        [Parameter(Mandatory)] [object[]]$Actual,
        [Parameter(Mandatory)] [object[]]$Expected,
        [Parameter(Mandatory)] [string]$Message
    )

    $actualText = (@($Actual | Sort-Object) -join '|')
    $expectedText = (@($Expected | Sort-Object) -join '|')
    if ($actualText -cne $expectedText) {
        throw "ASSERT_SEQUENCE_FAILED: $Message Expected='$expectedText'; Actual='$actualText'"
    }
}

$json = & pwsh -NoProfile -File $tool -AsJson
if ($LASTEXITCODE -ne 0) {
    throw "A ferramenta de inventário falhou com ExitCode=$LASTEXITCODE."
}

$result = $json | ConvertFrom-Json
Assert-Equal -Actual $result.Status -Expected 'OK' -Message 'O inventário da DLL canônica deve passar.'
Assert-Equal -Actual $result.AssemblyName -Expected 'GenexusOpenApiBuilder.Extension' -Message 'AssemblyName canônico divergente.'
Assert-Equal -Actual $result.PackageCompatibility -Expected 143920 -Message 'PackageCompatibility canônico divergente.'
Assert-Equal -Actual $result.EntryType -Expected 'GenexusOpenApiBuilder.Extension.Package' -Message 'Tipo de entrada canônico divergente.'
Assert-Equal -Actual $result.EntryBaseType -Expected 'Artech.Architecture.UI.Framework.Packages.AbstractPackageUI' -Message 'Classe-base canônica divergente.'
Assert-Equal -Actual $result.ManifestResource -Expected 'GenexusOpenApiBuilder.Extension.GenexusOpenApiBuilder.package' -Message 'Recurso .package canônico divergente.'
Assert-Equal -Actual $result.ManifestId -Expected '7be72bf4-8884-40dc-955d-ed9d31b69b74' -Message 'ID do manifesto canônico divergente.'
Assert-Equal -Actual $result.ManifestName -Expected 'Genexus Open API Builder' -Message 'Nome do manifesto canônico divergente.'
Assert-SequenceEqual -Actual $result.MetadataResources -Expected @('GenexusOpenApiBuilder.Extension.GenexusOpenApiBuilder.package') -Message 'Recursos do MetadataReader divergentes.'
Assert-SequenceEqual -Actual $result.AssemblyResources -Expected @('GenexusOpenApiBuilder.Extension.GenexusOpenApiBuilder.package') -Message 'Recursos retornados pelo Assembly divergentes.'

$expectedAssemblyReferenceNames = @(
    'Artech.Architecture.Common',
    'Artech.Architecture.UI.Framework',
    'Artech.Common',
    'Artech.Common.Framework',
    'Artech.Common.Helpers',
    'Artech.Common.Properties',
    'Artech.Genexus.Common',
    'Artech.Udm.Framework',
    'mscorlib',
    'Newtonsoft.Json',
    'System',
    'System.Core',
    'System.Drawing',
    'System.Windows.Forms'
)
Assert-SequenceEqual -Actual $result.AssemblyReferenceNames -Expected $expectedAssemblyReferenceNames -Message 'AssemblyRef direto da DLL canônica mudou; revisar a lista pinada antes da Fase 2.'

Write-Output 'PASS: ExtensionAssemblyInventory'
