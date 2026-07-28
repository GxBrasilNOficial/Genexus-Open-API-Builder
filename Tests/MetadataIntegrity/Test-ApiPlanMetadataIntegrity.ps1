Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$helperPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanMetadataIntegrity.cs'
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
    if (-not $Condition) {
        throw "ASSERT_TRUE_FAILED: $Message"
    }
}

function Assert-False {
    param([bool]$Condition, [string]$Message)
    if ($Condition) {
        throw "ASSERT_FALSE_FAILED: $Message"
    }
}

$descriptions = [Newtonsoft.Json.Linq.JArray]::Parse(@'
[
  { "serviceName": "Get", "description": "Get Transaction2" },
  { "serviceName": "List", "description": "List Transaction2" }
]
'@)

$plannedContract = [Newtonsoft.Json.Linq.JObject]::Parse(@'
{
  "api": {
    "name": "apiTransaction2",
    "servicesBasePath": "apiTransaction2",
    "restPath": "/transaction2",
    "securityLevel": "Authentication",
    "gamCondition": "GAM_AUTHENTICATION_REQUIRED"
  },
  "pagination": {
    "defaultPageSize": 50,
    "maximumPageSize": 200
  }
}
'@)

$currentSource = @'
apiTransaction2
{
    [Description("Get Transaction2")]
    Get()
        => procTransaction2_API_Get();

    [Description("List Transaction2")]
    List()
        => procTransaction2_API_List();
}
'@

$expectedSource = $currentSource
$descriptionSentinel = 'Genexus Open API Builder B054 API Object - Transaction=Transaction2'
$integrity = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::Create(
    $descriptions,
    $plannedContract,
    $descriptionSentinel,
    '11111111-1111-1111-1111-111111111111',
    'B054',
    $currentSource,
    $expectedSource)

$metadata = [Newtonsoft.Json.Linq.JObject]::new()
$metadata['integrity'] = $integrity
$descriptionsHash = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::ComputeJsonSha256($descriptions)
$plannedContractHash = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::ComputeJsonSha256($plannedContract)
[string[]]$serviceNames = @('Get', 'List')
$actualDescriptionsHash = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::ComputeJsonSha256([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::CreateServiceDescriptionsContractFromSource($currentSource, $serviceNames))

Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::HasCompatibleIntegrity($metadata, $descriptionsHash, $plannedContractHash, $actualDescriptionsHash, $descriptionSentinel, $currentSource, $expectedSource, $true)) 'B067 deve aceitar metadata de integridade compatível.'

$formattedSource = @'
apiTransaction2
{
    [Description("Get Transaction2")]
    Get()
        => procTransaction2_API_Get();

    [Description( "List Transaction2" )]
    List( )
        =>   procTransaction2_API_List ( ) ;
}
'@
$formattedDescriptionsHash = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::ComputeJsonSha256([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::CreateServiceDescriptionsContractFromSource($formattedSource, $serviceNames))
Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::HasCompatibleIntegrity($metadata, $descriptionsHash, $plannedContractHash, $formattedDescriptionsHash, $descriptionSentinel, $formattedSource, $expectedSource, $true)) 'B067 deve aceitar formatação inofensiva quando descrição e contrato semântico permanecem compatíveis.'

$manualDescriptionChange = $currentSource.Replace('[Description("List Transaction2")]', '[Description("List Transaction 2")]')
$manualDescriptionHash = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::ComputeJsonSha256([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::CreateServiceDescriptionsContractFromSource($manualDescriptionChange, $serviceNames))
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::HasCompatibleIntegrity($metadata, $descriptionsHash, $plannedContractHash, $manualDescriptionHash, $descriptionSentinel, $manualDescriptionChange, $expectedSource, $true)) 'B067 deve rejeitar alteração manual posterior em Description.'

$manualContractChange = [Newtonsoft.Json.Linq.JObject]::Parse($plannedContract.ToString([Newtonsoft.Json.Formatting]::None))
$manualContractChange['pagination']['maximumPageSize'] = 100
$changedContractHash = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::ComputeJsonSha256($manualContractChange)
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::HasCompatibleIntegrity($metadata, $descriptionsHash, $changedContractHash, $actualDescriptionsHash, $descriptionSentinel, $currentSource, $expectedSource, $true)) 'B067 deve rejeitar divergência de contrato planejado essencial.'

Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::HasCompatibleIntegrity($metadata, $descriptionsHash, $plannedContractHash, $actualDescriptionsHash, $descriptionSentinel, $currentSource, $expectedSource, $false)) 'B067 deve rejeitar Service Source com contrato semântico divergente.'

$legacyMetadata = [Newtonsoft.Json.Linq.JObject]::new()
Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::HasCompatibleIntegrity($legacyMetadata, $descriptionsHash, $plannedContractHash, $actualDescriptionsHash, $descriptionSentinel, $currentSource, $expectedSource, $false)) 'Metadata legada sem bloco integrity deve continuar aceita para primeiro upgrade conservador.'

Write-Output 'PASS: ApiPlanMetadataIntegrity'
