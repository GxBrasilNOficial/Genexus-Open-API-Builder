Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$helperPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanMetadataIntegrity.cs'
$metadataWriterPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanMetadataFileWriter.cs'
$metadataWriterSource = Get-Content -Raw -LiteralPath $metadataWriterPath
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
$descriptionSentinel = 'apiTransaction2 - by Genexus Open API Builder'
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

Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::HasCompatibleIntegrity($metadata, $descriptionsHash, $plannedContractHash, $actualDescriptionsHash, $descriptionSentinel, $expectedSource, $true)) 'B067 deve aceitar metadata de integridade compatível.'

$currentSourceHash = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::ComputeNormalizedTextSha256($currentSource)
Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::HasCompatibleGeneratedBaseline($metadata, $actualDescriptionsHash, $currentSourceHash, $descriptionSentinel, '11111111-1111-1111-1111-111111111111')) 'O baseline deve aceitar o estado que a extensao gravou.'
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::HasCompatibleGeneratedBaseline($metadata, $actualDescriptionsHash, ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::ComputeNormalizedTextSha256($currentSource + "`n// edicao manual")), $descriptionSentinel, '11111111-1111-1111-1111-111111111111')) 'O baseline deve rejeitar Source editado diretamente.'
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::HasCompatibleGeneratedBaseline($metadata, $actualDescriptionsHash, $currentSourceHash, 'Descricao manual', '11111111-1111-1111-1111-111111111111')) 'O baseline deve rejeitar Description editada diretamente.'
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::HasCompatibleGeneratedBaseline($metadata, $actualDescriptionsHash, $currentSourceHash, $descriptionSentinel, '22222222-2222-2222-2222-222222222222')) 'O baseline deve rejeitar GUID divergente.'

$compatibleBaseline = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::DiagnoseGeneratedBaseline($metadata, $actualDescriptionsHash, $currentSourceHash, $descriptionSentinel, '11111111-1111-1111-1111-111111111111')
Assert-True $compatibleBaseline.IsCompatible 'DiagnoseGeneratedBaseline deve aceitar o estado gravado.'
Assert-True ($compatibleBaseline.FailingClause -eq 'None') 'DiagnoseGeneratedBaseline compatível deve reportar FailingClause=None.'

$sourceMismatch = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::DiagnoseGeneratedBaseline($metadata, $actualDescriptionsHash, ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::ComputeNormalizedTextSha256($currentSource + "`n// edicao manual")), $descriptionSentinel, '11111111-1111-1111-1111-111111111111')
Assert-False $sourceMismatch.IsCompatible 'DiagnoseGeneratedBaseline deve rejeitar Source editado.'
Assert-True ($sourceMismatch.FailingClause -eq 'BaselineServiceSourceHashMismatch') 'DiagnoseGeneratedBaseline deve identificar hash do Service Source.'

$descriptionMismatch = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::DiagnoseGeneratedBaseline($metadata, $actualDescriptionsHash, $currentSourceHash, 'Descricao manual', '11111111-1111-1111-1111-111111111111')
Assert-False $descriptionMismatch.IsCompatible 'DiagnoseGeneratedBaseline deve rejeitar Description editada.'
Assert-True ($descriptionMismatch.FailingClause -eq 'BaselineDescriptionMismatch') 'DiagnoseGeneratedBaseline deve identificar Description.'

$guidMismatch = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::DiagnoseGeneratedBaseline($metadata, $actualDescriptionsHash, $currentSourceHash, $descriptionSentinel, '22222222-2222-2222-2222-222222222222')
Assert-False $guidMismatch.IsCompatible 'DiagnoseGeneratedBaseline deve rejeitar GUID divergente.'
Assert-True ($guidMismatch.FailingClause -eq 'BaselineGuidMismatch') 'DiagnoseGeneratedBaseline deve identificar GUID.'

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
Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::HasCompatibleIntegrity($metadata, $descriptionsHash, $plannedContractHash, $formattedDescriptionsHash, $descriptionSentinel, $expectedSource, $true)) 'B067 deve aceitar formatação inofensiva quando descrição e contrato semântico permanecem compatíveis.'

$sourceWithRestMethodBetweenDescriptionAndService = @'
apiTransaction2
{
    [Description("Get Transaction2")]
    Get()
        => procTransaction2_API_Get();

    [Description("List Transaction2")]
    List()
        => procTransaction2_API_List();

    [Description("Create Transaction2")]
    [RestMethod(POST)]
    Create(in: &CreateRequest, out: &CreateResponse)
        => procTransaction2_API_Create(&CreateRequest, &CreateResponse);
}
'@
$descriptionsWithCreate = [Newtonsoft.Json.Linq.JArray]::Parse(@'
[
  { "serviceName": "Create", "description": "Create Transaction2" },
  { "serviceName": "Get", "description": "Get Transaction2" },
  { "serviceName": "List", "description": "List Transaction2" }
]
'@)
[string[]]$serviceNamesWithCreate = @('Get', 'List', 'Create')
$descriptionsWithCreateHash = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::ComputeJsonSha256($descriptionsWithCreate)
$actualDescriptionsWithRestMethodHash = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::ComputeJsonSha256([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::CreateServiceDescriptionsContractFromSource($sourceWithRestMethodBetweenDescriptionAndService, $serviceNamesWithCreate))
$metadataWithCreate = [Newtonsoft.Json.Linq.JObject]::new()
$metadataWithCreate['integrity'] = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::Create(
    $descriptionsWithCreate,
    $plannedContract,
    $descriptionSentinel,
    '11111111-1111-1111-1111-111111111111',
    'B079',
    $sourceWithRestMethodBetweenDescriptionAndService,
    $sourceWithRestMethodBetweenDescriptionAndService)
Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::HasCompatibleIntegrity($metadataWithCreate, $descriptionsWithCreateHash, $plannedContractHash, $actualDescriptionsWithRestMethodHash, $descriptionSentinel, $sourceWithRestMethodBetweenDescriptionAndService, $true)) 'B067 deve aceitar Description seguida de RestMethod antes do serviço.'

$manualDescriptionChange = $currentSource.Replace('[Description("List Transaction2")]', '[Description("List Transaction 2")]')
$manualDescriptionHash = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::ComputeJsonSha256([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::CreateServiceDescriptionsContractFromSource($manualDescriptionChange, $serviceNames))
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::HasCompatibleIntegrity($metadata, $descriptionsHash, $plannedContractHash, $manualDescriptionHash, $descriptionSentinel, $expectedSource, $true)) 'B067 deve rejeitar alteração manual posterior em Description.'

$manualContractChange = [Newtonsoft.Json.Linq.JObject]::Parse($plannedContract.ToString([Newtonsoft.Json.Formatting]::None))
$manualContractChange['pagination']['maximumPageSize'] = 100
$changedContractHash = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::ComputeJsonSha256($manualContractChange)
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::HasCompatibleIntegrity($metadata, $descriptionsHash, $changedContractHash, $actualDescriptionsHash, $descriptionSentinel, $expectedSource, $true)) 'B067 deve rejeitar divergência de contrato planejado essencial.'

$legacyPlannedContract = [Newtonsoft.Json.Linq.JObject]::Parse($plannedContract.ToString([Newtonsoft.Json.Formatting]::None))
$legacyPlannedContract['api']['restPath'] = [Newtonsoft.Json.Linq.JValue]::new('/transaction2/{Transaction2Id}')
$legacyPlannedContract['services'] = [Newtonsoft.Json.Linq.JArray]::Parse(@'
[
  {
    "name": "Get",
    "httpMethod": "GET",
    "restPath": "/transaction2/{Transaction2Id}",
    "operationId": "apiTransaction2.Get"
  },
  {
    "name": "List",
    "httpMethod": "GET",
    "restPath": "/transaction2",
    "operationId": "apiTransaction2.List"
  }
]
'@)
$legacyPlannedHash = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::ComputeJsonSha256($legacyPlannedContract)
$legacyIntegrity = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::Create(
    $descriptions,
    $legacyPlannedContract,
    $descriptionSentinel,
    '11111111-1111-1111-1111-111111111111',
    'B079',
    $currentSource,
    $expectedSource)
$legacyContractMetadata = [Newtonsoft.Json.Linq.JObject]::new()
$legacyContractMetadata['integrity'] = $legacyIntegrity
[string[]]$compatiblePlannedHashes = @($plannedContractHash, $legacyPlannedHash)
[string[]]$compatibleExpectedSources = @($expectedSource)
Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::HasCompatibleIntegrity($legacyContractMetadata, $descriptionsHash, $compatiblePlannedHashes, $actualDescriptionsHash, $descriptionSentinel, $compatibleExpectedSources, $true)) 'B067 deve aceitar metadata gerada por contrato planejado anterior quando a reexecução reconhece a variante como própria.'

[string[]]$currentPlannedHashOnly = @($plannedContractHash)
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::HasCompatibleIntegrity($legacyContractMetadata, $descriptionsHash, $currentPlannedHashOnly, $actualDescriptionsHash, $descriptionSentinel, $compatibleExpectedSources, $true)) 'B067 deve rejeitar contrato planejado anterior quando ele não foi declarado como variante compatível.'

Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::HasCompatibleIntegrity($metadata, $descriptionsHash, $plannedContractHash, $actualDescriptionsHash, $descriptionSentinel, $expectedSource, $false)) 'B067 deve rejeitar Service Source com contrato semântico divergente.'

$legacyMetadata = [Newtonsoft.Json.Linq.JObject]::new()
Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::HasCompatibleIntegrity($legacyMetadata, $descriptionsHash, $plannedContractHash, $actualDescriptionsHash, $descriptionSentinel, $expectedSource, $false)) 'Metadata legada sem bloco integrity deve continuar aceita para primeiro upgrade conservador.'
$legacyBaseline = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::DiagnoseGeneratedBaseline($legacyMetadata, $actualDescriptionsHash, $currentSourceHash, $descriptionSentinel, '11111111-1111-1111-1111-111111111111')
Assert-True $legacyBaseline.IsCompatible 'DiagnoseGeneratedBaseline deve aceitar metadata sem integrity.'
Assert-True ($legacyBaseline.FailingClause -eq 'None') 'Metadata sem integrity deve reportar FailingClause=None.'

$fingerprintMetadata = [Newtonsoft.Json.Linq.JObject]::Parse('{"schemaVersion":"GOAB_API_METADATA_B060_V1","generatedAtUtc":"2026-08-15T00:00:00.0000000Z"}')
$snapshotHash = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::ComputeJsonSha256($fingerprintMetadata)
$compatibleFingerprint = [Newtonsoft.Json.Linq.JObject]::Parse($fingerprintMetadata.ToString([Newtonsoft.Json.Formatting]::None))
$fingerprintBlock = [Newtonsoft.Json.Linq.JObject]::new()
$fingerprintBlock.Add('algorithm', [Newtonsoft.Json.Linq.JValue]::new('SHA-256'))
$fingerprintBlock.Add('scope', [Newtonsoft.Json.Linq.JValue]::new('metadataWithoutFingerprint'))
$fingerprintBlock.Add('value', [Newtonsoft.Json.Linq.JValue]::new($snapshotHash))
$compatibleFingerprint.Add('fingerprint', $fingerprintBlock)
$fingerprintOk = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::DiagnoseMetadataFingerprint($compatibleFingerprint)
Assert-True $fingerprintOk.IsCompatible 'Fingerprint compatível deve ser aceito.'
Assert-True ($fingerprintOk.FailingClause -eq 'None') 'Fingerprint compatível deve reportar FailingClause=None.'
Assert-True ($fingerprintOk.StoredValue -eq $snapshotHash) 'Fingerprint compatível deve preservar o hash gravado.'
Assert-True ($fingerprintOk.ActualValue -eq $snapshotHash) 'Fingerprint compatível deve recalcular o mesmo hash.'

$brokenFingerprint = [Newtonsoft.Json.Linq.JObject]::Parse($compatibleFingerprint.ToString([Newtonsoft.Json.Formatting]::None))
$brokenFingerprint.Property('generatedAtUtc').Value = [Newtonsoft.Json.Linq.JValue]::new('2026-08-16T00:00:00.0000000Z')
$fingerprintMismatch = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::DiagnoseMetadataFingerprint($brokenFingerprint)
Assert-False $fingerprintMismatch.IsCompatible 'Fingerprint deve rejeitar snapshot alterado.'
Assert-True ($fingerprintMismatch.FailingClause -eq 'FingerprintHashMismatch') 'Fingerprint deve identificar hash divergente.'
Assert-True ($fingerprintMismatch.StoredValue -eq $snapshotHash) 'Fingerprint divergente deve preservar o hash gravado.'
Assert-True ($fingerprintMismatch.ActualValue -ne $snapshotHash) 'Fingerprint divergente deve expor o hash recalculado.'

$absentFingerprint = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::DiagnoseMetadataFingerprint($fingerprintMetadata)
Assert-True $absentFingerprint.IsCompatible 'Metadata sem fingerprint deve continuar aceita.'

$writeTimestamp = '2026-08-15T22:00:00.1230000Z'
$writeMetadata = [Newtonsoft.Json.Linq.JObject]::new()
$writeMetadata.Add('schemaVersion', [Newtonsoft.Json.Linq.JValue]::new('GOAB_API_METADATA_B060_V1'))
$writeMetadata.Add('generatedAtUtc', [Newtonsoft.Json.Linq.JValue]::new($writeTimestamp))
$writeHash = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::ComputeJsonSha256($writeMetadata)
$writeFingerprint = [Newtonsoft.Json.Linq.JObject]::new()
$writeFingerprint.Add('algorithm', [Newtonsoft.Json.Linq.JValue]::new('SHA-256'))
$writeFingerprint.Add('scope', [Newtonsoft.Json.Linq.JValue]::new('metadataWithoutFingerprint'))
$writeFingerprint.Add('value', [Newtonsoft.Json.Linq.JValue]::new($writeHash))
$writeMetadata.Add('fingerprint', $writeFingerprint)
$persistedJson = $writeMetadata.ToString([Newtonsoft.Json.Formatting]::Indented) + "`n"
$defaultParsed = [Newtonsoft.Json.Linq.JObject]::Parse($persistedJson)
$defaultFingerprint = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::DiagnoseMetadataFingerprint($defaultParsed)
Assert-False $defaultFingerprint.IsCompatible 'JObject.Parse padrao deve quebrar o fingerprint quando generatedAtUtc tem zeros a direita.'
$stableParsed = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::ParseMetadataJson($persistedJson)
Assert-True ($stableParsed['generatedAtUtc'].Type.ToString() -eq 'String') 'ParseMetadataJson deve preservar generatedAtUtc como string.'
$stableFingerprint = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataIntegrity]::DiagnoseMetadataFingerprint($stableParsed)
Assert-True $stableFingerprint.IsCompatible 'ParseMetadataJson deve preservar o fingerprint com generatedAtUtc ToString(O).'
Assert-True ($stableFingerprint.ActualValue -eq $writeHash) 'O hash relido com DateParseHandling.None deve coincidir com o gravado.'

if ($metadataWriterSource.IndexOf('CreatePlannedContract(apiPlan, transactionStructure: transactionStructure, includePagination: false)', [StringComparison]::Ordinal) -lt 0) {
    throw 'ASSERT_FAILED: O hash B067 deve excluir a paginação do contrato essencial.'
}
if ($metadataWriterSource.IndexOf('CreatePlannedContract(apiPlan, includePagination: false)', [StringComparison]::Ordinal) -lt 0) {
    throw 'ASSERT_FAILED: O hash planejado atual deve excluir a paginação.'
}
if ($metadataWriterSource.IndexOf('CreatePlannedContract(apiPlan, useLegacyPathParameterSyntax: true)', [StringComparison]::Ordinal) -lt 0) {
    throw 'ASSERT_FAILED: Metadata legada com paginação deve permanecer compatível.'
}
if ($metadataWriterSource.IndexOf('storedContractWithoutPagination.Remove("pagination")', [StringComparison]::Ordinal) -lt 0) {
    throw 'ASSERT_FAILED: O reencounter deve comparar metadata legada sem considerar a paginação.'
}
if ($metadataWriterSource.IndexOf('HasCompatibleGeneratedBaseline', [StringComparison]::Ordinal) -lt 0) {
    throw 'ASSERT_FAILED: O writer deve possuir uma validacao de baseline independente do plano desejado.'
}
if ($metadataWriterSource.IndexOf('ParseMetadataBytes', [StringComparison]::Ordinal) -lt 0) {
    throw 'ASSERT_FAILED: O writer deve reler a metadata sem converter generatedAtUtc em DateTime.'
}

Write-Output 'PASS: ApiPlanMetadataIntegrity'
