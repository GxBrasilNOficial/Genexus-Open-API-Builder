#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$helperPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanTransactionSyncFieldSelection.cs'
$newtonsoftPath = Get-ChildItem -Path (Join-Path $env:USERPROFILE '.nuget\packages\newtonsoft.json') -Filter Newtonsoft.Json.dll -Recurse |
    Where-Object { $_.FullName -match '\\lib\\netstandard2\.0\\Newtonsoft\.Json\.dll$' } |
    Sort-Object FullName -Descending |
    Select-Object -First 1 -ExpandProperty FullName

if ([string]::IsNullOrWhiteSpace($newtonsoftPath)) {
    throw 'Newtonsoft.Json.dll não encontrado no cache NuGet local.'
}

$runtimeAssemblies = @([System.AppContext]::GetData('TRUSTED_PLATFORM_ASSEMBLIES') -split [System.IO.Path]::PathSeparator |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
if ($runtimeAssemblies.Count -eq 0) {
    throw 'Assemblies do runtime PowerShell atual não foram encontrados.'
}

Add-Type -Path $helperPath -ReferencedAssemblies @(($runtimeAssemblies + $newtonsoftPath) | Sort-Object -Unique)

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw "ASSERT_TRUE_FAILED: $Message" }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Message)
    if ($Expected -ne $Actual) {
        throw "ASSERT_EQUAL_FAILED: $Message (expected='$Expected' actual='$Actual')"
    }
}

function Assert-SequenceEqual {
    param([string[]]$Expected, [string[]]$Actual, [string]$Message)
    Assert-Equal $Expected.Count $Actual.Count "$Message (count)"
    for ($i = 0; $i -lt $Expected.Count; $i++) {
        Assert-Equal $Expected[$i] $Actual[$i] "$Message (index $i)"
    }
}

$guidA = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
$guidB = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
$guidC = 'cccccccc-cccc-cccc-cccc-cccccccccccc'
$guidD = 'dddddddd-dddd-dddd-dddd-dddddddddddd'

$names = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncFieldName[]]@(
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncFieldName]::new($guidA, 'CampoA'),
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncFieldName]::new($guidB, 'CampoB'),
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncFieldName]::new($guidC, 'CampoC'),
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncFieldName]::new($guidD, 'CampoD')
)

$metadata = [Newtonsoft.Json.Linq.JObject]::Parse(@"
{
  `"fields`": {
    `"response`": [
      { `"attributeGuid`": `"$guidB`", `"name`": `"CampoB`" },
      { `"attributeGuid`": `"$guidA`", `"name`": `"CampoA`" }
    ],
    `"listFilters`": [
      { `"field`": { `"attributeGuid`": `"$guidA`", `"name`": `"CampoA`" }, `"usesRange`": false },
      { `"field`": { `"attributeGuid`": `"$guidB`", `"name`": `"CampoB`" }, `"usesRange`": true }
    ]
  }
}
"@)

$emptyRemoved = [string[]]@()
$emptyInclude = [string[]]@()
$emptyAdded = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncAddedFieldCandidate[]]@()

$baseResponse = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncFieldSelection]::ResolveOrderedFieldNames(
    $metadata, 'fields.response', $false, $names, $emptyRemoved, 'Response', $emptyInclude, $emptyAdded)
Assert-SequenceEqual @('CampoB', 'CampoA') $baseResponse 'ordem da metadata Response'

$added = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncAddedFieldCandidate[]]@(
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncAddedFieldCandidate]::new($guidC, $true, $true, $true),
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncAddedFieldCandidate]::new($guidD, $true, $true, $true)
)
$withAddRemove = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncFieldSelection]::ResolveOrderedFieldNames(
    $metadata, 'fields.response', $false, $names, [string[]]@($guidB), 'Response', [string[]]@($guidC), $added)
Assert-SequenceEqual @('CampoA', 'CampoC') $withAddRemove 'remove B e anexa C no fim'

$onlyMarked = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncFieldSelection]::ResolveOrderedFieldNames(
    $metadata, 'fields.response', $false, $names, $emptyRemoved, 'Response', [string[]]@($guidC), $added)
Assert-SequenceEqual @('CampoB', 'CampoA', 'CampoC') $onlyMarked 'só GUID marcado em IncludeAdded'

$listFilters = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncFieldSelection]::ResolveOrderedFieldNames(
    $metadata, 'fields.listFilters', $true, $names, $emptyRemoved, 'ListFilters', [string[]]@($guidC), $added)
Assert-SequenceEqual @('CampoA', 'CampoB', 'CampoC') $listFilters 'ListFilters ordem + add'

$createBlocked = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncAddedFieldCandidate[]]@(
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncAddedFieldCandidate]::new($guidC, $false, $true, $true)
)
$createResult = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncFieldSelection]::ResolveOrderedFieldNames(
    $metadata, 'fields.response', $false, $names, $emptyRemoved, 'CreateRequest', [string[]]@($guidC), $createBlocked)
Assert-SequenceEqual @('CampoB', 'CampoA') $createResult 'CreateRequest bloqueia não gravável'

$dupAdd = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncFieldSelection]::ResolveOrderedFieldNames(
    $metadata, 'fields.response', $false, $names, $emptyRemoved, 'Response', [string[]]@($guidA),
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncAddedFieldCandidate[]]@(
        [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncAddedFieldCandidate]::new($guidA, $true, $true, $true)
    ))
Assert-SequenceEqual @('CampoB', 'CampoA') $dupAdd 'GUID já presente não é reanexado'

Write-Output 'PASS: ApiPlanTransactionSyncFieldSelection preserva ordem, remoções, adds e elegibilidade.'
