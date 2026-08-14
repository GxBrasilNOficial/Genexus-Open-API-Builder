#requires -Version 7.4

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$DllPath,

    [Parameter(Mandatory)]
    [string]$ExpectedAssetName,

    [Parameter(Mandatory)]
    [string]$ExpectedInformationalVersion,

    [string]$ExpectedLine,

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

function Read-SerializedString {
    param(
        [Parameter(Mandatory)] [byte[]]$Bytes,
        [Parameter(Mandatory)] [ref]$Offset
    )

    if ($Offset.Value -ge $Bytes.Length) {
        throw 'Blob de atributo customizado terminou antes da string serializada.'
    }

    if ($Bytes[$Offset.Value] -eq 0xFF) {
        $Offset.Value++
        return $null
    }

    $length = [int](Get-CompressedUInt32 -Bytes $Bytes -Offset $Offset)
    if ($Offset.Value + $length -gt $Bytes.Length) {
        throw 'Blob de atributo customizado terminou dentro da string serializada.'
    }

    $value = [Text.Encoding]::UTF8.GetString($Bytes, $Offset.Value, $length)
    $Offset.Value += $length
    return $value
}

function Get-AssemblyCustomAttributeBlob {
    param(
        [Parameter(Mandatory)] [object]$Metadata,
        [Parameter(Mandatory)] [object]$AssemblyDefinition,
        [Parameter(Mandatory)] [string]$AttributeTypeName
    )

    foreach ($attributeHandle in $AssemblyDefinition.GetCustomAttributes()) {
        $attribute = $Metadata.GetCustomAttribute($attributeHandle)
        if ($attribute.Constructor.Kind -ne [System.Reflection.Metadata.HandleKind]::MemberReference) {
            continue
        }

        $member = $Metadata.GetMemberReference([System.Reflection.Metadata.MemberReferenceHandle]$attribute.Constructor)
        $attributeType = Get-MetadataTypeName -Metadata $Metadata -Handle $member.Parent
        if ($attributeType -ceq $AttributeTypeName) {
            return [byte[]]$Metadata.GetBlobBytes($attribute.Value)
        }
    }

    return $null
}

function Get-AssemblyInformationalVersion {
    param(
        [Parameter(Mandatory)] [object]$Metadata,
        [Parameter(Mandatory)] [object]$AssemblyDefinition
    )

    $blob = Get-AssemblyCustomAttributeBlob `
        -Metadata $Metadata `
        -AssemblyDefinition $AssemblyDefinition `
        -AttributeTypeName 'System.Reflection.AssemblyInformationalVersionAttribute'
    if ($null -eq $blob -or $blob.Length -lt 2) {
        return $null
    }

    if ($blob[0] -ne 1 -or $blob[1] -ne 0) {
        throw 'AssemblyInformationalVersionAttribute possui blob sem prólogo válido.'
    }

    $offset = 2
    return Read-SerializedString -Bytes $blob -Offset ([ref]$offset)
}

function Get-AssemblyMetadataValue {
    param(
        [Parameter(Mandatory)] [object]$Metadata,
        [Parameter(Mandatory)] [object]$AssemblyDefinition,
        [Parameter(Mandatory)] [string]$Key
    )

    $targetTypeName = 'System.Reflection.AssemblyMetadataAttribute'
    foreach ($attributeHandle in $AssemblyDefinition.GetCustomAttributes()) {
        $attribute = $Metadata.GetCustomAttribute($attributeHandle)
        if ($attribute.Constructor.Kind -ne [System.Reflection.Metadata.HandleKind]::MemberReference) {
            continue
        }

        $member = $Metadata.GetMemberReference([System.Reflection.Metadata.MemberReferenceHandle]$attribute.Constructor)
        $attributeType = Get-MetadataTypeName -Metadata $Metadata -Handle $member.Parent
        if ($attributeType -cne $targetTypeName) {
            continue
        }

        $blob = [byte[]]$Metadata.GetBlobBytes($attribute.Value)
        if ($blob.Length -lt 2 -or $blob[0] -ne 1 -or $blob[1] -ne 0) {
            throw 'AssemblyMetadataAttribute possui blob sem prólogo válido.'
        }

        $offset = 2
        $attributeKey = Read-SerializedString -Bytes $blob -Offset ([ref]$offset)
        $attributeValue = Read-SerializedString -Bytes $blob -Offset ([ref]$offset)
        if ($attributeKey -ceq $Key) {
            return $attributeValue
        }
    }

    return $null
}

Require-File -Path $DllPath -Label 'DLL de Release'
$DllPath = [System.IO.Path]::GetFullPath($DllPath)

$actualAssetName = [System.IO.Path]::GetFileName($DllPath)
if ($actualAssetName -cne $ExpectedAssetName) {
    throw "Nome do asset divergente. Esperado='$ExpectedAssetName'; encontrado='$actualAssetName'."
}

$inventoryScript = Join-Path $PSScriptRoot 'Test-ExtensionAssemblyInventory.ps1'
Require-File -Path $inventoryScript -Label 'Checker de inventário da assembly'
$inventoryJson = & pwsh -NoProfile -File $inventoryScript -DllPath $DllPath -ExpectedPackageCompatibility $ExpectedPackageCompatibility -AsJson
if ($LASTEXITCODE -ne 0) {
    throw "O inventário offline da assembly falhou com código $LASTEXITCODE."
}
$inventory = ($inventoryJson -join [Environment]::NewLine) | ConvertFrom-Json

$stream = [System.IO.File]::OpenRead($DllPath)
try {
    $peReader = [System.Reflection.PortableExecutable.PEReader]::new($stream)
    try {
        $metadata = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($peReader)
        $assemblyDefinition = $metadata.GetAssemblyDefinition()
        $informationalVersion = Get-AssemblyInformationalVersion -Metadata $metadata -AssemblyDefinition $assemblyDefinition
        $gxLine = Get-AssemblyMetadataValue -Metadata $metadata -AssemblyDefinition $assemblyDefinition -Key 'GxLine'
        $assemblyVersion = $assemblyDefinition.GetAssemblyName().Version.ToString()
    }
    finally {
        $peReader.Dispose()
    }
}
finally {
    $stream.Dispose()
}

if ($informationalVersion -cne $ExpectedInformationalVersion) {
    throw "InformationalVersion divergente. Esperado='$ExpectedInformationalVersion'; encontrado='$informationalVersion'."
}

if ($PSBoundParameters.ContainsKey('ExpectedLine') -and $gxLine -cne $ExpectedLine) {
    throw "GxLine divergente. Esperado='$ExpectedLine'; encontrado='$gxLine'."
}

$result = [pscustomobject]@{
    Status                  = 'OK'
    AssetName               = $actualAssetName
    DllPath                 = $DllPath
    Sha256                  = (Get-FileHash -LiteralPath $DllPath -Algorithm SHA256).Hash
    AssemblyVersion         = $assemblyVersion
    InformationalVersion    = $informationalVersion
    GxLine                  = if ($null -eq $gxLine) { '<none>' } else { $gxLine }
    PackageCompatibility    = $inventory.PackageCompatibility
    ManifestResource        = $inventory.ManifestResource
    ManifestId              = $inventory.ManifestId
    EntryType               = $inventory.EntryType
    ValidationMode          = 'Inventário PEReader + metadados de Release; sem IDE, KB, instalação ou rede'
}

if ($AsJson) {
    $result | ConvertTo-Json -Depth 6
}
else {
    $result
}
