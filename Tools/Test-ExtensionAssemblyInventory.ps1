#requires -Version 7.4

[CmdletBinding()]
param(
    [string]$DllPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'Src\Extension\bin\Release\net471\GenexusOpenApiBuilder.Extension.dll'),
    [string]$ExpectedManifestResource = 'GenexusOpenApiBuilder.Extension.GenexusOpenApiBuilder.package',
    [string]$ExpectedEntryType = 'GenexusOpenApiBuilder.Extension.Package',
    [string]$ExpectedEntryBaseType = 'Artech.Architecture.UI.Framework.Packages.AbstractPackageUI',
    [Nullable[int]]$ExpectedPackageCompatibility = 143920,
    [switch]$AsJson
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

function Get-MetadataTypeName {
    param(
        [Parameter(Mandatory)] [object]$Metadata,
        [Parameter(Mandatory)] [System.Reflection.Metadata.EntityHandle]$Handle
    )

    if ($Handle.IsNil) {
        return $null
    }

    $kind = $Handle.Kind.ToString()
    switch ($kind) {
        'TypeDefinition' {
            $definition = $Metadata.GetTypeDefinition([System.Reflection.Metadata.TypeDefinitionHandle]$Handle)
            $namespace = $Metadata.GetString($definition.Namespace)
            $name = $Metadata.GetString($definition.Name)
        }
        'TypeReference' {
            $reference = $Metadata.GetTypeReference([System.Reflection.Metadata.TypeReferenceHandle]$Handle)
            $namespace = $Metadata.GetString($reference.Namespace)
            $name = $Metadata.GetString($reference.Name)
        }
        default {
            return "<$kind>"
        }
    }

    if ([string]::IsNullOrWhiteSpace($namespace)) {
        return $name
    }

    return "$namespace.$name"
}

function Get-CompressedUInt32 {
    param(
        [Parameter(Mandatory)] [byte[]]$Bytes,
        [Parameter(Mandatory)] [ref]$Offset
    )

    $index = $Offset.Value
    if ($index -ge $Bytes.Length) {
        throw 'Blob de atributo customizado terminou antes do comprimento da string.'
    }

    $first = $Bytes[$index]
    if (($first -band 0x80) -eq 0) {
        $Offset.Value = $index + 1
        return [uint32]$first
    }

    if (($first -band 0xC0) -eq 0x80) {
        if ($index + 1 -ge $Bytes.Length) {
            throw 'Blob de atributo customizado terminou no comprimento de dois bytes.'
        }

        $value = (($first -band 0x3F) -shl 8) -bor $Bytes[$index + 1]
        $Offset.Value = $index + 2
        return [uint32]$value
    }

    if (($first -band 0xE0) -eq 0xC0) {
        if ($index + 3 -ge $Bytes.Length) {
            throw 'Blob de atributo customizado terminou no comprimento de quatro bytes.'
        }

        $value = (($first -band 0x1F) -shl 24) -bor
            ($Bytes[$index + 1] -shl 16) -bor
            ($Bytes[$index + 2] -shl 8) -bor
            $Bytes[$index + 3]
        $Offset.Value = $index + 4
        return [uint32]$value
    }

    throw 'Comprimento comprimido inválido no blob de atributo customizado.'
}

function Get-PackageCompatibilityValue {
    param(
        [Parameter(Mandatory)] [object]$Metadata,
        [Parameter(Mandatory)] [object]$AssemblyDefinition
    )

    $targetAttribute = 'Artech.Architecture.Common.Packages.PackageCompatibilityAttribute'
    foreach ($attributeHandle in $AssemblyDefinition.GetCustomAttributes()) {
        $attribute = $Metadata.GetCustomAttribute($attributeHandle)
        if ($attribute.Constructor.Kind -ne [System.Reflection.Metadata.HandleKind]::MemberReference) {
            continue
        }

        $member = $Metadata.GetMemberReference([System.Reflection.Metadata.MemberReferenceHandle]$attribute.Constructor)
        $attributeType = Get-MetadataTypeName -Metadata $Metadata -Handle $member.Parent
        if ($attributeType -cne $targetAttribute) {
            continue
        }

        $blob = [byte[]]$Metadata.GetBlobBytes($attribute.Value)
        if ($blob.Length -lt 4 -or $blob[0] -ne 1 -or $blob[1] -ne 0) {
            throw 'PackageCompatibilityAttribute possui blob sem prólogo válido.'
        }

        $namedArgumentCount = [BitConverter]::ToUInt16($blob, 2)
        $offset = 4
        for ($index = 0; $index -lt $namedArgumentCount; $index++) {
            if ($offset + 2 -gt $blob.Length) {
                throw 'PackageCompatibilityAttribute terminou antes do argumento nomeado.'
            }

            $elementKind = $blob[$offset + 1]
            $offset += 2
            $nameLength = [uint32](Get-CompressedUInt32 -Bytes $blob -Offset ([ref]$offset))
            if ($offset + $nameLength -gt $blob.Length) {
                throw 'PackageCompatibilityAttribute terminou no nome do argumento.'
            }

            $name = [Text.Encoding]::UTF8.GetString($blob, $offset, [int]$nameLength)
            $offset += [int]$nameLength
            if ($elementKind -eq 0x08) {
                if ($offset + 4 -gt $blob.Length) {
                    throw 'PackageCompatibilityAttribute terminou no valor Version.'
                }

                $value = [BitConverter]::ToInt32($blob, $offset)
                $offset += 4
                if ($name -ceq 'Version') {
                    return $value
                }

                continue
            }

            throw "Tipo de argumento nomeado não suportado em PackageCompatibilityAttribute: 0x{0:X2}" -f $elementKind
        }
    }

    return $null
}

function Get-AssemblyReferences {
    param(
        [Parameter(Mandatory)] [object]$Metadata
    )

    return @(
        foreach ($handle in $Metadata.AssemblyReferences) {
            $reference = $Metadata.GetAssemblyReference($handle)
            [pscustomobject]@{
                Name    = $Metadata.GetString($reference.Name)
                Version = $reference.Version.ToString()
            }
        }
    ) | Sort-Object Name, Version
}

Require-File -Path $DllPath -Label 'DLL'
$DllPath = [System.IO.Path]::GetFullPath($DllPath)
$sha256 = (Get-FileHash -LiteralPath $DllPath -Algorithm SHA256).Hash

$stream = [System.IO.File]::OpenRead($DllPath)
try {
    $peReader = [System.Reflection.PortableExecutable.PEReader]::new($stream)
    try {
        $metadata = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($peReader)
        $assemblyDefinition = $metadata.GetAssemblyDefinition()
        $assemblyName = $assemblyDefinition.GetAssemblyName()
        $packageCompatibility = Get-PackageCompatibilityValue -Metadata $metadata -AssemblyDefinition $assemblyDefinition

        $entryType = $null
        $entryBaseType = $null
        foreach ($typeHandle in $metadata.TypeDefinitions) {
            $definition = $metadata.GetTypeDefinition($typeHandle)
            $typeName = Get-MetadataTypeName -Metadata $metadata -Handle $typeHandle
            if ($typeName -ceq $ExpectedEntryType) {
                $entryType = $typeName
                $entryBaseType = Get-MetadataTypeName -Metadata $metadata -Handle $definition.BaseType
                break
            }
        }

        $metadataResources = @(
            foreach ($resourceHandle in $metadata.ManifestResources) {
                $resource = $metadata.GetManifestResource($resourceHandle)
                $metadata.GetString($resource.Name)
            }
        ) | Sort-Object
        $assemblyReferences = @(Get-AssemblyReferences -Metadata $metadata)
    }
    finally {
        $peReader.Dispose()
    }
}
finally {
    $stream.Dispose()
}

$assembly = [System.Reflection.Assembly]::LoadFile($DllPath)
$resourceNames = @($assembly.GetManifestResourceNames() | Sort-Object)
$packageResources = @($resourceNames | Where-Object { $_ -like '*.package' })
if ($packageResources.Count -ne 1) {
    throw "Era esperado exatamente um recurso .package; foram encontrados $($packageResources.Count)."
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
$metadataResourcesMatchAssembly = (@($metadataResources) -join '|') -eq (@($resourceNames) -join '|')

if ($ExpectedManifestResource -and $manifestResource -cne $ExpectedManifestResource) {
    throw "Recurso .package divergente. Esperado='$ExpectedManifestResource'; encontrado='$manifestResource'."
}
if ($entryType -cne $ExpectedEntryType) {
    throw "Tipo de entrada divergente. Esperado='$ExpectedEntryType'; encontrado='$entryType'."
}
if ($entryBaseType -cne $ExpectedEntryBaseType) {
    throw "Classe-base divergente. Esperado='$ExpectedEntryBaseType'; encontrado='$entryBaseType'."
}
if ($null -eq $packageCompatibility) {
    throw 'PackageCompatibilityAttribute não foi encontrado ou não pôde ser decodificado.'
}
if ($null -ne $ExpectedPackageCompatibility -and $packageCompatibility -ne $ExpectedPackageCompatibility) {
    throw "PackageCompatibility divergente. Esperado='$ExpectedPackageCompatibility'; encontrado='$packageCompatibility'."
}
if (-not $metadataResourcesMatchAssembly) {
    throw 'A lista de recursos do MetadataReader diverge da lista retornada pelo Assembly.'
}
if ([string]::IsNullOrWhiteSpace($manifestId) -or [string]::IsNullOrWhiteSpace($manifestName)) {
    throw 'O manifesto incorporado precisa conter os atributos id e name.'
}

$result = [pscustomobject]@{
    Status                    = 'OK'
    DllPath                   = $DllPath
    Sha256                    = $sha256
    AssemblyName              = $assemblyName.Name
    AssemblyVersion           = $assemblyName.Version.ToString()
    PackageCompatibility      = $packageCompatibility
    EntryType                 = $entryType
    EntryBaseType             = $entryBaseType
    ManifestResource          = $manifestResource
    ManifestId                = $manifestId
    ManifestName              = $manifestName
    MetadataResources         = @($metadataResources)
    AssemblyResources         = @($resourceNames)
    AssemblyReferences        = @($assemblyReferences)
    AssemblyReferenceNames    = @($assemblyReferences | ForEach-Object Name)
    ValidationMode            = 'PEReader metadata + resource stream; sem IDE, KB, instalação ou rede'
}

if ($AsJson) {
    $result | ConvertTo-Json -Depth 8
}
else {
    $result | Format-List
}
