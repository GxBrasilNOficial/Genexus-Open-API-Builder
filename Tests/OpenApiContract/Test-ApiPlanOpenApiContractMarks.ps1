Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Trava as marcacoes que fazem o contrato OpenAPI gerado bater com o comportamento real.
# Frente registrada em Docs/Implementation/2026-08-03-CONTRATO-OPENAPI-GAPS.md.

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$businessComponentWriterPath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\ApiPlanBusinessComponentWriter.cs'
$listProcedureWriterPath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\ApiPlanListProcedureWriter.cs'
$sdtWriterPath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\ApiPlanSdtWriter.cs'
$sdtPlanPath = Join-Path $repositoryRoot 'Src\Domain\ApiPlanSdtGenerationPlan.cs'
$apiObjectWriterPath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\ApiPlanApiObjectWriter.cs'

$businessComponentWriterSource = Get-Content -Path $businessComponentWriterPath -Raw
$listProcedureWriterSource = Get-Content -Path $listProcedureWriterPath -Raw
$sdtWriterSource = Get-Content -Path $sdtWriterPath -Raw
$sdtPlanSource = Get-Content -Path $sdtPlanPath -Raw
$apiObjectWriterSource = Get-Content -Path $apiObjectWriterPath -Raw

function Assert-Contains {
    param([string]$Text, [string]$Expected, [string]$Message)
    if (-not $Text.Contains($Expected)) {
        throw "ASSERT_CONTAINS_FAILED: $Message Expected='$Expected'"
    }
}

function Assert-NotContains {
    param([string]$Text, [string]$Unexpected, [string]$Message)
    if ($Text.Contains($Unexpected)) {
        throw "ASSERT_NOT_CONTAINS_FAILED: $Message Unexpected='$Unexpected'"
    }
}

function Get-SectionSource {
    param([string]$Text, [string]$StartMarker, [string]$EndMarker, [string]$Message)
    $start = $Text.IndexOf($StartMarker, [StringComparison]::Ordinal)
    $end = $Text.IndexOf($EndMarker, [StringComparison]::Ordinal)
    if ($start -lt 0 -or $end -lt 0 -or $end -le $start) {
        throw "ASSERT_SECTION_FAILED: $Message"
    }

    return $Text.Substring($start, $end - $start)
}

# 1. requestBody obrigatorio.
#
# A propriedade Required so produz efeito no YAML quando aplicada nas variaveis de request
# do API Object. Ela precisa ser marcada e aplicada por TODO writer que recria essas
# variaveis: o writer de List roda depois do de Business Component e ja apagou a marcacao
# uma vez, deixando o contrato com requestBody: required: false.

Assert-Contains $businessComponentWriterSource 'internal const string ServiceRequiredPropertyId = "idVarServiceRequired";' 'O id da propriedade Required de parametro de servico deve permanecer declarado em um unico ponto.'

$apiVariableSpecsSource = Get-SectionSource $businessComponentWriterSource 'private static IReadOnlyList<VariableSpec> ApiVariableSpecs(ApiPlan plan)' 'private static IReadOnlyList<VariableSpec> LegacyApiVariableSpecs(ApiPlan plan)' 'nao foi possivel isolar ApiVariableSpecs do writer Business Component.'
Assert-Contains $apiVariableSpecsSource 'new VariableSpec("CreateRequest", plan.CreateRequestSdtName, isServiceRequired: true)' 'Variavel de request Create do API Object deve nascer marcada como obrigatoria no servico.'
Assert-Contains $apiVariableSpecsSource 'new VariableSpec("UpdateRequest", plan.UpdateRequestSdtName, isServiceRequired: true)' 'Variavel de request Update do API Object deve nascer marcada como obrigatoria no servico.'

Assert-Contains $listProcedureWriterSource 'variables.Add(new VariableSpec("CreateRequest", plan.CreateRequestSdtName, isServiceRequired: true));' 'Writer de List deve preservar a marcacao de obrigatoriedade ao recriar a variavel de request Create do API Object.'
Assert-Contains $listProcedureWriterSource 'variables.Add(new VariableSpec("UpdateRequest", plan.UpdateRequestSdtName, isServiceRequired: true));' 'Writer de List deve preservar a marcacao de obrigatoriedade ao recriar a variavel de request Update do API Object.'

foreach ($writer in @(
    @{ Name = 'Business Component'; Source = $businessComponentWriterSource },
    @{ Name = 'List'; Source = $listProcedureWriterSource })) {
    $replaceApiVariablesSource = Get-SectionSource $writer.Source 'private static void ReplaceVariables(KBModel model, API api, IReadOnlyList<VariableSpec> variables)' 'private static bool HasExpectedVariables' "nao foi possivel isolar ReplaceVariables de API no writer $($writer.Name)."
    Assert-Contains $replaceApiVariablesSource 'ConfigureServiceRequired(item, variable);' "Writer $($writer.Name) deve aplicar a propriedade Required ao recriar variaveis do API Object."
}

# 2. Corpo de erro sem array que nunca e preenchido.

Assert-NotContains $sdtWriterSource 'root.AddLevel("Errors"' 'SDT compartilhado de erro nao deve recriar a subestrutura Errors, que a geracao nunca preenche.'
Assert-NotContains $sdtPlanSource 'new ApiPlanSdtMember("Errors"' 'Plano de SDTs nao deve declarar o membro Errors no contrato de erro compartilhado.'
Assert-Contains $sdtPlanSource 'new ApiPlanSdtMember("Code", "VarChar", 64, 0, false, false, string.Empty, "SharedErrorResponse")' 'Contrato de erro compartilhado deve manter Code top-level.'
Assert-Contains $sdtPlanSource 'new ApiPlanSdtMember("Message", "VarChar", 256, 0, false, false, string.Empty, "SharedErrorResponse")' 'Contrato de erro compartilhado deve manter Message top-level.'

Assert-Contains $businessComponentWriterSource 'PreviousB079BusinessRuleFailureMessages' 'Reconhecimento da Procedure legada com ErrorItem deve permanecer, para permitir regravar objeto antigo em vez de bloquea-lo como externo.'

# 3. Descricao publica do API Object sem identificador interno.

Assert-Contains $apiObjectWriterSource 'ApiPlanOwnedObjectDescription.CreateApiObjectDescription(apiPlan.ApiName)' 'Descricao do API Object, publicada em info.description, deve seguir o padrao nome + by Genexus Open API Builder.'
Assert-NotContains $apiObjectWriterSource 'B054 API Object' 'Descricao publicada nao pode voltar a expor identificador interno de backlog.'
Assert-NotContains $apiObjectWriterSource 'Procedures=B050-B053' 'Descricao publicada nao pode voltar a expor identificador interno de backlog.'
Assert-NotContains $apiObjectWriterSource 'REST API for {apiPlan.TransactionName}' 'Descricao canônica do API Object nao deve voltar a prosa REST API for.'

# 4. Security Level explicitamente aplicado nos servicos do API Object.

Assert-Contains $businessComponentWriterSource 'annotations.Add($"    [SecurityLevel({plan.Security.SecurityLevel})]");' 'Writer de Business Component deve aplicar SecurityLevel em cada servico do API Object.'
Assert-Contains $listProcedureWriterSource 'annotations.Add($"    [SecurityLevel({plan.Security.SecurityLevel})]");' 'Writer de List deve aplicar SecurityLevel em cada servico do API Object.'

Write-Output 'PASS: ApiPlanOpenApiContractMarks'
