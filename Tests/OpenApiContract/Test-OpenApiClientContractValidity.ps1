Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Test-OpenApiClientContractValidity.ps1
# Valida a integridade dos identificadores _API_ e operationIds para consumo por geradores de cliente OpenAPI.
# Garante a conformidade de schemas (_API_) e operationIds (apiNome.Serviço) sem depender de rede/downloads externos.

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$apiPlanPath = Join-Path $repositoryRoot 'Src\Domain\ApiPlan.cs'
$apiObjectWriterPath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\ApiPlanApiObjectWriter.cs'
$serviceSourceContractPath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\ApiPlanServiceSourceContract.cs'

$apiPlanSource = Get-Content -Path $apiPlanPath -Raw
$apiObjectWriterSource = Get-Content -Path $apiObjectWriterPath -Raw
$serviceSourceContractSource = Get-Content -Path $serviceSourceContractPath -Raw

function Assert-Contains {
    param([string]$Text, [string]$Expected, [string]$Message)
    if (-not $Text.Contains($Expected)) {
        throw "ASSERT_CONTAINS_FAILED: $Message Expected='$Expected'"
    }
}

function Assert-Match {
    param([string]$Text, [string]$Pattern, [string]$Message)
    if (-not ($Text -match $Pattern)) {
        throw "ASSERT_MATCH_FAILED: $Message Pattern='$Pattern'"
    }
}

# 1. Validacao de nomes _API_ no plano de SDTs (ApiPlan.cs)
$expectedSdtPatterns = @(
    '_API_CreateRequest',
    '_API_UpdateRequest',
    '_API_Response',
    '_API_ListFilters',
    '_API_ListResponse',
    'sdt_API_ErrorResponse',
    'sdt_API_Pagination'
)

foreach ($pattern in $expectedSdtPatterns) {
    Assert-Contains $apiPlanSource $pattern "O padrao de nome de SDT '$pattern' deve permanecer declarado em ApiPlan.cs."
}

# 2. Validacao do padrao de operationId (apiNome.Serviço)
# O padrao apiNome.Serviço e gerado nativamente pelo GeneXus quando o API Object expoe os servicos.
$expectedServices = @('List', 'Get', 'Create', 'Update')
foreach ($service in $expectedServices) {
    Assert-Contains $apiPlanSource $service "O servico '$service' deve permanecer suportado em ApiPlan.cs."
}

# 3. Validacao em arquivos YAML reais de KB se disponiveis no ambiente
$kbYamlPaths = @(
    'C:\KBs\wsEducacaoSpTeste\NETFrameworkSQLServer004\web\apiNotaFiscal.yaml',
    'C:\KBs\wsEducacaoSpTeste\NETPostgreSQL155\web\apiNotaFiscal.yaml'
)

$validatedCount = 0
foreach ($yamlPath in $kbYamlPaths) {
    if (Test-Path $yamlPath) {
        $validatedCount++
        $content = Get-Content -Path $yamlPath -Raw

        # Validar presenca dos schemas _API_ no YAML
        Assert-Contains $content 'sdtNotaFiscal_API_CreateRequest:' "YAML em '$yamlPath' deve conter sdtNotaFiscal_API_CreateRequest."
        Assert-Contains $content 'sdtNotaFiscal_API_UpdateRequest:' "YAML em '$yamlPath' deve conter sdtNotaFiscal_API_UpdateRequest."
        Assert-Contains $content 'sdtNotaFiscal_API_Response:' "YAML em '$yamlPath' deve conter sdtNotaFiscal_API_Response."
        Assert-Contains $content 'sdtNotaFiscal_API_ListResponse:' "YAML em '$yamlPath' deve conter sdtNotaFiscal_API_ListResponse."
        Assert-Contains $content 'sdtNotaFiscal_API_ListFilters:' "YAML em '$yamlPath' deve conter sdtNotaFiscal_API_ListFilters."
        Assert-Contains $content 'sdt_API_ErrorResponse:' "YAML em '$yamlPath' deve conter sdt_API_ErrorResponse."
        Assert-Contains $content 'sdt_API_Pagination:' "YAML em '$yamlPath' deve conter sdt_API_Pagination."

        # Validar operationIds no padrao apiNome.Servico
        Assert-Contains $content 'operationId: "apiNotaFiscal.List"' "YAML em '$yamlPath' deve conter operationId apiNotaFiscal.List."
        Assert-Contains $content 'operationId: "apiNotaFiscal.Get"' "YAML em '$yamlPath' deve conter operationId apiNotaFiscal.Get."
        Assert-Contains $content 'operationId: "apiNotaFiscal.Create"' "YAML em '$yamlPath' deve conter operationId apiNotaFiscal.Create."
        Assert-Contains $content 'operationId: "apiNotaFiscal.Update"' "YAML em '$yamlPath' deve conter operationId apiNotaFiscal.Update."

        # Validar ausencia de endpoints DELETE
        if ($content -match 'delete:') {
            throw "ASSERT_NOT_CONTAINS_FAILED: YAML em '$yamlPath' nao deve conter endpoints delete."
        }

        # Validar bloco security por operacao
        Assert-Contains $content 'security:' "YAML em '$yamlPath' deve declarar bloco security."
        Assert-Contains $content 'oAuthGXGAM:' "YAML em '$yamlPath' deve declarar oAuthGXGAM no bloco security."
    }
}

if ($validatedCount -eq 0) {
    throw "ASSERT_YAML_FILE_FOUND: Nenhum arquivo YAML de teste foi encontrado nos caminhos configurados."
}

Write-Output 'PASS: OpenApiClientContractValidity'
