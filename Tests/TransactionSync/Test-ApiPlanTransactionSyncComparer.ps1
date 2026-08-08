Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$helperPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanTransactionSyncComparer.cs'
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

function New-Snap {
    param(
        [int]$Order,
        [string]$Guid,
        [string]$Name,
        [string]$DataType = 'VarChar',
        [int]$Length = 40,
        [bool]$WritableCreate = $true,
        [bool]$WritableUpdate = $true
    )
    return [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncAttributeSnapshot]::new(
        $Order, $Guid, $Name, $DataType, $Length, 0, $false, $true, $false, $false, $false,
        $WritableCreate, $WritableUpdate, $true, $false, $false,
        $true, $true, $true, $true, '', '')
}

$guidA = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
$guidB = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
$guidC = 'cccccccc-cccc-cccc-cccc-cccccccccccc'

$metadata = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncAttributeSnapshot[]]@(
    (New-Snap -Order 1 -Guid $guidA -Name 'CampoA'),
    (New-Snap -Order 2 -Guid $guidB -Name 'CampoB')
)
$current = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncAttributeSnapshot[]]@(
    (New-Snap -Order 1 -Guid $guidA -Name 'CampoARenomeado'),
    (New-Snap -Order 2 -Guid $guidB -Name 'CampoB' -DataType 'Numeric' -Length 8),
    (New-Snap -Order 3 -Guid $guidC -Name 'CampoC')
)

$diff = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncComparer]::Compare($metadata, $current)
Assert-Equal 1 $diff.Added.Count '1 adicionado'
Assert-Equal 0 $diff.Removed.Count '0 removidos'
Assert-Equal 1 $diff.Renamed.Count '1 renomeado por GUID'
Assert-Equal 1 $diff.Modified.Count '1 modificado (tipo)'
Assert-True $diff.HasDifferences 'há diferenças'
Assert-Equal 'CampoA' $diff.Renamed[0].Previous.Name 'rename previous'
Assert-Equal 'CampoARenomeado' $diff.Renamed[0].Current.Name 'rename current'
Assert-True ($diff.Modified[0].Details -join ';' -match 'tipo') 'modified menciona tipo'

$removedCurrent = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncAttributeSnapshot[]]@(
    (New-Snap -Order 1 -Guid $guidA -Name 'CampoA')
)
$removedDiff = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncComparer]::Compare($metadata, $removedCurrent)
Assert-Equal 1 $removedDiff.Removed.Count 'CampoB removido'
Assert-Equal 'CampoB' $removedDiff.Removed[0].Previous.Name 'nome removido'

$json = [Newtonsoft.Json.Linq.JObject]::Parse(@"
{
  `"transactionStructure`": [
    {
      `"order`": 1,
      `"attributeGuid`": `"$guidA`",
      `"name`": `"CampoA`",
      `"dataType`": `"VarChar`",
      `"length`": 40,
      `"decimals`": 0,
      `"isPrimaryKey`": false,
      `"isNullable`": true,
      `"isFormula`": false,
      `"isInferred`": false,
      `"isRedundant`": false,
      `"isWritableByCreate`": true,
      `"isWritableByUpdate`": true,
      `"isFilterEligible`": true,
      `"isSensitive`": false,
      `"isAuditField`": false
    }
  ]
}
"@)
$structure = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionSyncComparer]::ReadStructure($json)
Assert-Equal 1 $structure.Count 'structure count'
Assert-Equal $guidA $structure[0].AttributeGuid 'structure guid'

Write-Output 'Test-ApiPlanTransactionSyncComparer.ps1: OK'
