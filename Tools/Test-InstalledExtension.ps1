[CmdletBinding()]
param(
    [string]$BuildDll = (Join-Path (Split-Path -Parent $PSScriptRoot) 'Src\Extension\bin\Release\net471\GenexusOpenApiBuilder.Extension.dll'),
    [string]$InstalledDll = 'C:\Program Files (x86)\GeneXus\GeneXus18\Packages\GenexusOpenApiBuilder.Extension.dll'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Require-File {
    param(
        [Parameter(Mandatory)] [string]$Path,
        [Parameter(Mandatory)] [string]$Label
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Label não encontrado: $Path"
    }
}

Require-File -Path $BuildDll -Label 'DLL compilada'
Require-File -Path $InstalledDll -Label 'DLL instalada'

$buildHash = (Get-FileHash -LiteralPath $BuildDll -Algorithm SHA256).Hash
$installedHash = (Get-FileHash -LiteralPath $InstalledDll -Algorithm SHA256).Hash
$assembly = [System.Reflection.Assembly]::LoadFile($BuildDll)
$resourceNames = @($assembly.GetManifestResourceNames())
$packageResources = @($resourceNames | Where-Object { $_ -like '*.package' })

$peStream = [System.IO.File]::OpenRead($BuildDll)
try {
    $peReader = [System.Reflection.PortableExecutable.PEReader]::new($peStream)
    $metadataReader = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($peReader)
    $typeNames = foreach ($typeHandle in $metadataReader.TypeDefinitions) {
        $typeDefinition = $metadataReader.GetTypeDefinition($typeHandle)
        "$($metadataReader.GetString($typeDefinition.Namespace)).$($metadataReader.GetString($typeDefinition.Name))"
    }

    $entryTypeHandle = $metadataReader.TypeDefinitions |
        Where-Object {
            $typeDefinition = $metadataReader.GetTypeDefinition($_)
            $metadataReader.GetString($typeDefinition.Namespace) -eq 'GenexusOpenApiBuilder.Extension' -and
            $metadataReader.GetString($typeDefinition.Name) -eq 'Package'
        } |
        Select-Object -First 1

    $entryTypeDefinition = $metadataReader.GetTypeDefinition($entryTypeHandle)
    $entryBaseType = $metadataReader.GetTypeReference([System.Reflection.Metadata.TypeReferenceHandle]$entryTypeDefinition.BaseType)
    $entryBaseTypeName = "$($metadataReader.GetString($entryBaseType.Namespace)).$($metadataReader.GetString($entryBaseType.Name))"
}
finally {
    $peStream.Dispose()
}

if ('GenexusOpenApiBuilder.Extension.Package' -notin $typeNames) {
    throw 'O tipo de entrada GenexusOpenApiBuilder.Extension.Package não foi encontrado na DLL compilada.'
}

if ($entryBaseTypeName -ne 'Artech.Architecture.UI.Framework.Packages.AbstractPackageUI') {
    throw "O tipo de entrada deve herdar de AbstractPackageUI; classe-base encontrada: $entryBaseTypeName"
}

if ($packageResources.Count -ne 1) {
    throw "Era esperado exatamente um manifesto .package incorporado; foram encontrados $($packageResources.Count)."
}

$manifestResource = $packageResources[0]
$resourceStream = $assembly.GetManifestResourceStream($manifestResource)
if ($null -eq $resourceStream) {
    throw "Não foi possível abrir o recurso incorporado '$manifestResource'."
}

try {
    $reader = [System.IO.StreamReader]::new($resourceStream)
    try {
        [xml]$manifest = $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }
}
finally {
    $resourceStream.Dispose()
}

$manifestRoot = $manifest.DocumentElement
if ($null -eq $manifestRoot -or $manifestRoot.LocalName -ne 'Package') {
    throw 'O manifesto incorporado não possui o elemento raiz Package.'
}

$manifestId = $manifestRoot.GetAttribute('id')
$manifestName = $manifestRoot.GetAttribute('name')
if ([string]::IsNullOrWhiteSpace($manifestId) -or [string]::IsNullOrWhiteSpace($manifestName)) {
    throw 'O manifesto incorporado precisa conter os atributos id e name.'
}

$result = [pscustomobject]@{
    BuildDll              = $BuildDll
    InstalledDll          = $InstalledDll
    InstalledMatchesBuild = ($buildHash -eq $installedHash)
    Sha256                = $buildHash
    EntryTypeFound        = $true
    EntryBaseType         = $entryBaseTypeName
    ManifestResource      = $manifestResource
    ManifestId            = $manifestId
    ManifestName          = $manifestName
    ActivationVerified    = $false
    ActivationNote        = 'A marcação no Extensions Manager é estado da IDE e deve ser confirmada manualmente.'
}

$result

if (-not $result.InstalledMatchesBuild) {
    Write-Error 'A DLL instalada é diferente da DLL compilada. Reinstale a DLL pela IDE antes de testar a carga.'
    exit 1
}
