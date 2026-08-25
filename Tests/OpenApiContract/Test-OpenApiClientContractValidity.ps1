Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Test-OpenApiClientContractValidity.ps1
# Trava offline, no repositorio, os identificadores _API_ e a lista de servicos
# que o plano da API declara para consumo por geradores de cliente OpenAPI.
# Nao le YAML publicado pelo Build da KB: esse artefato e do ambiente GeneXus,
# nao deste repositorio (B107).

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$apiPlanPath = Join-Path $repositoryRoot 'Src\Domain\ApiPlan.cs'
$apiPlanSource = Get-Content -Path $apiPlanPath -Raw

function Assert-Contains {
    param([string]$Text, [string]$Expected, [string]$Message)
    if (-not $Text.Contains($Expected)) {
        throw "ASSERT_CONTAINS_FAILED: $Message Expected='$Expected'"
    }
}

# 1. Nomes _API_ e SDTs compartilhados no plano (ApiPlan.cs)
$expectedSdtPatterns = @(
    '_API_CreateRequest',
    '_API_UpdateRequest',
    '_API_Response',
    '_API_ListFilters',
    '_API_ListResponse',
    'sdt_API_ErrorMessage',
    'sdt_API_ErrorResponse',
    'sdt_API_Pagination'
)

foreach ($pattern in $expectedSdtPatterns) {
    Assert-Contains $apiPlanSource $pattern "O padrao de nome de SDT '$pattern' deve permanecer declarado em ApiPlan.cs."
}

# 2. Servicos suportados no plano (o operationId apiNome.Servico e emitido pelo GeneXus)
$expectedServices = @('List', 'Get', 'Create', 'Update')
foreach ($service in $expectedServices) {
    Assert-Contains $apiPlanSource $service "O servico '$service' deve permanecer suportado em ApiPlan.cs."
}

Write-Output 'PASS: OpenApiClientContractValidity'
