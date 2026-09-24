Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Test-OpenApiClientContractValidity.ps1
# Trava offline, no repositorio, os identificadores _API_ e a lista de servicos
# que o plano da API declara para consumo por geradores de cliente OpenAPI.
# Nao le YAML publicado pelo Build da KB: esse artefato e do ambiente GeneXus,
# nao deste repositorio (B107).

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$apiPlanPath = Join-Path $repositoryRoot 'Src\Domain\ApiPlan.cs'
$namingPath = Join-Path $repositoryRoot 'Src\Domain\ApiPlanSdtHierarchicalNaming.cs'
$apiPlanSource = Get-Content -Path $apiPlanPath -Raw
$namingSource = Get-Content -Path $namingPath -Raw

function Assert-Contains {
    param([string]$Text, [string]$Expected, [string]$Message)
    if (-not $Text.Contains($Expected)) {
        throw "ASSERT_CONTAINS_FAILED: $Message Expected='$Expected'"
    }
}

# 1. Nomes _API_ e SDTs compartilhados no plano (ApiPlan.cs)
# B120: _API_ListResponse saiu do contrato HTTP público; o nome residual
# ListResponseSdtName / sdt*_API_ListResponse permanece só para limpeza A2.
$expectedSdtPatterns = @(
    '_API_CreateRequest',
    '_API_UpdateRequest',
    '_API_Response',
    '_API_ListFilters',
    'sdt_API_ErrorMessage',
    'sdt_API_ErrorResponse',
    'sdt_API_Pagination'
)

foreach ($pattern in $expectedSdtPatterns) {
    Assert-Contains $apiPlanSource $pattern "O padrao de nome de SDT '$pattern' deve permanecer declarado em ApiPlan.cs."
}

Assert-Contains $apiPlanSource 'ListResponseSdtName' "ListResponseSdtName deve permanecer em ApiPlan.cs para limpeza A2 do orfao."
Assert-Contains $apiPlanSource '_API_ListResponse' "O sufixo de nome residual _API_ListResponse deve permanecer em ApiPlan.cs para A2."

$expectedNestedPatterns = @(
    '_API_CreateRequest_',
    '_API_UpdateRequest_',
    '_API_Response_',
    '_API_ListResponse_Item'
)
foreach ($pattern in $expectedNestedPatterns) {
    Assert-Contains $namingSource $pattern "O padrao derivado de SDT '$pattern' deve permanecer declarado em ApiPlanSdtHierarchicalNaming.cs."
}

# 2. Servicos suportados no plano (o operationId apiNome.Servico e emitido pelo GeneXus)
$expectedServices = @('List', 'Get', 'Create', 'Update', 'Delete')
foreach ($service in $expectedServices) {
    Assert-Contains $apiPlanSource $service "O servico '$service' deve permanecer suportado em ApiPlan.cs."
}

Write-Output 'PASS: OpenApiClientContractValidity'
