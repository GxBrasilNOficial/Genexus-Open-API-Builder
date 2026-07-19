#requires -Version 7.4

[CmdletBinding()]
param(
    [string]$PackageSource,
    [string]$Manifest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($PackageSource)) {
    $PackageSource = Join-Path $repositoryRoot 'Src\Extension\Package.cs'
}
if ([string]::IsNullOrWhiteSpace($Manifest)) {
    $Manifest = Join-Path $repositoryRoot 'Src\Extension\GenexusOpenApiBuilder.package'
}

$PackageSource = [System.IO.Path]::GetFullPath($PackageSource)
$Manifest = [System.IO.Path]::GetFullPath($Manifest)

if (-not (Test-Path -LiteralPath $PackageSource -PathType Leaf)) {
    throw "Package.cs não encontrado: $PackageSource"
}
if (-not (Test-Path -LiteralPath $Manifest -PathType Leaf)) {
    throw "Manifesto .package não encontrado: $Manifest"
}

function Get-Duplicates {
    param([string[]]$Values)

    return @(
        $Values |
            Group-Object |
            Where-Object Count -gt 1 |
            ForEach-Object Name |
            Sort-Object
    )
}

function New-OrdinalSet {
    param([string[]]$Values)

    $set = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::Ordinal
    )
    foreach ($value in $Values) {
        [void]$set.Add($value)
    }
    return ,$set
}

function Get-SetDifference {
    param(
        [System.Collections.Generic.HashSet[string]]$Left,
        [System.Collections.Generic.HashSet[string]]$Right
    )

    return @(
        $Left |
            Where-Object { -not $Right.Contains($_) } |
            Sort-Object
    )
}

$packageText = Get-Content -LiteralPath $PackageSource -Raw
$runtimeMatches = @(
    [regex]::Matches(
        $packageText,
        'new\s+CommandKey\s*\(\s*Id\s*,\s*"(?<id>[^"]+)"\s*\)'
    )
)
$runtimeIds = @(
    $runtimeMatches |
        ForEach-Object { $_.Groups['id'].Value }
)

[xml]$manifestXml = Get-Content -LiteralPath $Manifest -Raw
$namespace = 'http://schemas.genexus.com/addin/v1.0'
$namespaceManager = [System.Xml.XmlNamespaceManager]::new($manifestXml.NameTable)
$namespaceManager.AddNamespace('gx', $namespace)

$definitionNodes = @(
    $manifestXml.SelectNodes(
        '/*[local-name()="Package"]/*[local-name()="Commands"]/*[local-name()="CommandDefinition"]',
        $namespaceManager
    )
)
$referenceNodes = @(
    $manifestXml.SelectNodes(
        '/*[local-name()="Package"]/*[local-name()="Groups"]/*[local-name()="Group"]/*[local-name()="Command"]',
        $namespaceManager
    )
)

$definitionIds = @($definitionNodes | ForEach-Object { $_.GetAttribute('id') })
$referenceIds = @($referenceNodes | ForEach-Object { $_.GetAttribute('refid') })
$definitionContexts = @($definitionNodes | ForEach-Object { $_.GetAttribute('context') })

$errors = [System.Collections.Generic.List[string]]::new()

if ($runtimeIds.Count -eq 0) {
    $errors.Add('Package.cs não registra nenhum CommandKey por AddCommand.')
}

foreach ($entry in @(
    @{ Label = 'Package.cs'; Values = $runtimeIds },
    @{ Label = 'CommandDefinition'; Values = $definitionIds },
    @{ Label = 'Command refid'; Values = $referenceIds }
)) {
    $duplicates = @(Get-Duplicates -Values $entry.Values)
    if ($duplicates.Count -gt 0) {
        $errors.Add(
            "$($entry.Label) contém IDs duplicados: $($duplicates -join ', ')"
        )
    }
}

$runtimeSet = New-OrdinalSet -Values $runtimeIds
$definitionSet = New-OrdinalSet -Values $definitionIds
$referenceSet = New-OrdinalSet -Values $referenceIds

$missingDefinitions = @(Get-SetDifference -Left $runtimeSet -Right $definitionSet)
$missingRuntime = @(Get-SetDifference -Left $definitionSet -Right $runtimeSet)
$missingReferences = @(Get-SetDifference -Left $definitionSet -Right $referenceSet)
$undefinedReferences = @(Get-SetDifference -Left $referenceSet -Right $definitionSet)

if ($missingDefinitions.Count -gt 0) {
    $errors.Add(
        "AddCommand sem CommandDefinition: $($missingDefinitions -join ', ')"
    )
}
if ($missingRuntime.Count -gt 0) {
    $errors.Add(
        "CommandDefinition sem AddCommand: $($missingRuntime -join ', ')"
    )
}
if ($missingReferences.Count -gt 0) {
    $errors.Add(
        "CommandDefinition ausente do grupo de comandos em Groups: $($missingReferences -join ', ')"
    )
}
if ($undefinedReferences.Count -gt 0) {
    $errors.Add(
        "Command refid sem CommandDefinition: $($undefinedReferences -join ', ')"
    )
}

$invalidContexts = @(
    for ($index = 0; $index -lt $definitionIds.Count; $index++) {
        if ($definitionContexts[$index] -cne 'selection') {
            "$($definitionIds[$index])='$($definitionContexts[$index])'"
        }
    }
)
if ($invalidContexts.Count -gt 0) {
    $errors.Add(
        "CommandDefinition com context diferente de 'selection': $($invalidContexts -join ', ')"
    )
}

if ($errors.Count -gt 0) {
    $separator = [Environment]::NewLine + '- '
    throw (
        'Registro de comandos inconsistente:' +
        [Environment]::NewLine +
        '- ' +
        ($errors -join $separator)
    )
}

$orderedIds = @($runtimeSet | Sort-Object)

[pscustomobject]@{
    Status = 'OK'
    PackageSource = $PackageSource
    Manifest = $Manifest
    CommandCount = $orderedIds.Count
    CommandIds = $orderedIds
} | Format-List
