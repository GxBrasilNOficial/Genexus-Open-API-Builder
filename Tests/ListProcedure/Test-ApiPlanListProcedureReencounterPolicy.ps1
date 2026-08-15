Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$policyPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanListProcedureReencounterPolicy.cs'
$serviceSourceContractPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanServiceSourceContract.cs'
Add-Type -Path @($policyPath, $serviceSourceContractPath)

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

$expectedSource = @'
&RestStatusCode = 200
&ListResponse = new()
&AppliedFilters = new()
&ListResponse.AppliedFilters = &AppliedFilters
&ApiPage = &pApiPage
&ApiPageSize = &pApiPageSize
If &ApiPage.IsEmpty()
    &ApiPage = 1
EndIf
If &ApiPageSize.IsEmpty()
    &ApiPageSize = 40
EndIf
If &ApiPage < 1
    &RestStatusCode = 400
    &ErrorResponse = new()
    &ErrorResponse.Code = !"invalid_request"
    &ErrorResponse.Message = !"page must be greater than or equal to 1"
    return
EndIf
If &ApiPageSize < 1
    &RestStatusCode = 400
    &ErrorResponse = new()
    &ErrorResponse.Code = !"invalid_request"
    &ErrorResponse.Message = !"pageSize must be greater than or equal to 1"
    return
EndIf
If &ApiPageSize > 100
    &RestStatusCode = 400
    &ErrorResponse = new()
    &ErrorResponse.Code = !"invalid_request"
    &ErrorResponse.Message = !"pageSize exceeds the configured maximum"
    return
EndIf
&FirstRecord = ((&ApiPage - 1) * &ApiPageSize) + 1
'@

$sameOwnSourceWithNewPagination = $expectedSource.Replace('&ApiPageSize = 40', '&ApiPageSize = 50').Replace('If &ApiPageSize > 100', 'If &ApiPageSize > 200')
Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanListProcedureReencounterPolicy]::IsSourceAllowed($sameOwnSourceWithNewPagination, $expectedSource, [string[]]@())) 'B070 deve aceitar Source próprio conhecido quando só literais de paginação mudam.'

$externalSource = $sameOwnSourceWithNewPagination.Replace('&ErrorResponse.Message = !"pageSize exceeds the configured maximum"', '&ErrorResponse.Message = !"custom message"')
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanListProcedureReencounterPolicy]::IsSourceAllowed($externalSource, $expectedSource, [string[]]@())) 'B070 deve recusar Source externo com divergência fora dos literais de paginação.'

$knownPreviousSource = $expectedSource.Replace('&ListResponse.AppliedFilters = &AppliedFilters', '//&ListResponse.AppliedFilters = &AppliedFilters')
$currentPreviousWithNewPagination = $knownPreviousSource.Replace('&ApiPageSize = 40', '&ApiPageSize = 25').Replace('If &ApiPageSize > 100', 'If &ApiPageSize > 80')
Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanListProcedureReencounterPolicy]::IsSourceAllowed($currentPreviousWithNewPagination, $expectedSource, [string[]]@($knownPreviousSource))) 'B070 deve aceitar fonte própria anterior conhecida quando apenas a paginação mudou.'

$expectedRules = @'
parm(in:&pApiPage, in:&pApiPageSize, out:&ListResponse, out:&ErrorResponse, out:&RestStatusCode);
'@
$legacyRules = @'
parm(out:&ListResponse);
'@
$previousB070Rules = @'
parm(in:&pApiPage, in:&pApiPageSize, out:&ListResponse);
'@
Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanListProcedureReencounterPolicy]::IsRulesAllowed($expectedRules, $expectedRules, $legacyRules)) 'Rules esperadas devem ser aceitas.'
Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanListProcedureReencounterPolicy]::IsRulesAllowed($previousB070Rules, $expectedRules, $legacyRules, [string[]]@($previousB070Rules))) 'Rules B070 anteriores sem ErrorResponse/RestStatusCode devem ser migraveis.'
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanListProcedureReencounterPolicy]::IsRulesAllowed('parm(in:&Unexpected, out:&ListResponse);', $expectedRules, $legacyRules)) 'Rules divergentes devem continuar bloqueadas.'

Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanListProcedureReencounterPolicy]::AreVariablesAllowed($true, $false, $true)) 'Variáveis devem ser aceitas quando algum contrato próprio conhecido casa.'
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanListProcedureReencounterPolicy]::AreVariablesAllowed($true, $false, $false)) 'Variáveis divergentes devem continuar bloqueadas.'

[string[]]$services = @('List', 'Get', 'Create', 'Update')
[string[]]$primaryKey = @('ContratoNumero')
[string[]]$listFilters = @('ContratoNumero')
$apiObjectSource = @'
apiContrato
{
    List(in: &ApiPage, in: &ApiPageSize, in: &ContratoNumero, out: &ListResponse)
        => procContrato_API_List(&ApiPage, &ApiPageSize, &ContratoNumero, &ListResponse);
    Get()
        => procContrato_API_Get();
    Create(in: &CreateRequest, out: &CreateResponse)
        => procContrato_API_Create(&CreateRequest, &CreateResponse);
    Update(in: &ContratoNumero, in: &UpdateRequest, out: &UpdateResponse)
        => procContrato_API_Update(&ContratoNumero, &UpdateRequest, &UpdateResponse);
}
'@
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesB070($apiObjectSource.Replace('procContrato_API_List', 'procContrato_API_CustomList'), 'apiContrato', 'Contrato', 'Root Module', $services, $primaryKey, $listFilters, $true)) 'API Object com chamada externa divergente deve continuar recusado pelo contrato B070.'

Write-Output 'PASS: ApiPlanListProcedureReencounterPolicy'
