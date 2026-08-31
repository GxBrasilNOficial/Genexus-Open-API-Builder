Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$contractPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanServiceSourceContract.cs'
Add-Type -Path $contractPath

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

[string[]]$services = @('List', 'Get', 'Create', 'Update')
[string[]]$primaryKey = @('SimulationResultId')

$b054 = @'
apiSimulationResult
{
    List()
        => Entities.procSimulationResult_API_List();
    Get()
        => Entities.procSimulationResult_API_Get();
    Create()
        => Entities.procSimulationResult_API_Create();
    Update()
        => Entities.procSimulationResult_API_Update();
}
'@

Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesB054($b054, 'apiSimulationResult', 'SimulationResult', 'Entities', $services)) 'B054 deve aceitar vinculo qualificado pelo modulo esperado.'
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesB054($b054.Replace('Entities.procSimulationResult_API_Get', 'Other.procSimulationResult_API_Get'), 'apiSimulationResult', 'SimulationResult', 'Entities', $services)) 'B054 deve rejeitar Procedure em modulo divergente.'

$b055 = @'
apiSimulationResult
{
    List()
        => Entities.procSimulationResult_API_List();
    Get()
        => Entities.procSimulationResult_API_Get();
    Create(in: &CreateRequest, out: &CreateResponse)
        => Entities.procSimulationResult_API_Create(&CreateRequest, &CreateResponse);
    Update(in: &SimulationResultId, in: &UpdateRequest, out: &UpdateResponse)
        => Entities.procSimulationResult_API_Update(&SimulationResultId, &UpdateRequest, &UpdateResponse);
}
'@

Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesB055($b055, 'apiSimulationResult', 'SimulationResult', 'Entities', $services, $primaryKey)) 'B055 deve aceitar vinculo, argumentos e modulo esperados.'
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesB055($b055.Replace('&UpdateRequest, &UpdateResponse', '&UpdateResponse, &UpdateRequest'), 'apiSimulationResult', 'SimulationResult', 'Entities', $services, $primaryKey)) 'B055 deve rejeitar argumentos divergentes.'
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesB055($b055.Replace('Entities.procSimulationResult_API_Create', 'Entities.procSimulationResult_API_Update'), 'apiSimulationResult', 'SimulationResult', 'Entities', $services, $primaryKey)) 'B055 deve rejeitar vinculo servico-Procedure divergente.'

[string[]]$listFilters = @('SimulationResultId')

$b070 = @'
apiSimulationResult
{
    List(in: &ApiPage, in: &ApiPageSize, in: &SimulationResultId, out: &ListResponse)
        => Entities.procSimulationResult_API_List(&ApiPage, &ApiPageSize, &SimulationResultId, &ListResponse);
    Get()
        => Entities.procSimulationResult_API_Get();
    Create(in: &CreateRequest, out: &CreateResponse)
        => Entities.procSimulationResult_API_Create(&CreateRequest, &CreateResponse);
    Update(in: &SimulationResultId, in: &UpdateRequest, out: &UpdateResponse)
        => Entities.procSimulationResult_API_Update(&SimulationResultId, &UpdateRequest, &UpdateResponse);
}
'@

Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesB070($b070, 'apiSimulationResult', 'SimulationResult', 'Entities', $services, $primaryKey, $listFilters, $true)) 'B070 deve aceitar List parametrizado e Create/Update B055.'
Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesB070(($b070 -replace 'in: &ApiPage', 'in:&ApiPage' -replace 'out: &ListResponse', 'out:&ListResponse'), 'apiSimulationResult', 'SimulationResult', 'Entities', $services, $primaryKey, $listFilters, $true)) 'B070 deve aceitar normalizacao inofensiva de espacos.'
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesB070($b070.Replace('&SimulationResultId, &ListResponse', '&ListResponse, &SimulationResultId'), 'apiSimulationResult', 'SimulationResult', 'Entities', $services, $primaryKey, $listFilters, $true)) 'B070 deve rejeitar argumentos de List divergentes.'

$b079 = @'
apiSimulationResult
{
    [RestPath("/simulationresult")]
    [SecurityLevel(Authentication)]
    List(in: &ApiPage, in: &ApiPageSize, in: &SimulationResultId, out: &ListResponse)
        => Entities.procSimulationResult_API_List(&ApiPage, &ApiPageSize, &SimulationResultId, &ListResponse);
    [RestPath("/simulationresult/{&SimulationResultId}")]
    [SecurityLevel(Authentication)]
    Get(in: &SimulationResultId, out: &GetResponse, out: &ErrorResponse)
        => Entities.procSimulationResult_API_Get(&SimulationResultId, &GetResponse, &ErrorResponse, &RestStatusCode);
    [RestMethod(POST)]
    [RestPath("/simulationresult")]
    [SecurityLevel(Authentication)]
    Create(in: &CreateRequest, out: &CreateResponse, out: &ErrorResponse)
        => Entities.procSimulationResult_API_Create(&CreateRequest, &CreateResponse, &ErrorResponse, &RestStatusCode);
    [RestMethod(PUT)]
    [RestPath("/simulationresult/{&SimulationResultId}")]
    [SecurityLevel(Authentication)]
    Update(in: &SimulationResultId, in: &UpdateRequest, out: &UpdateResponse, out: &ErrorResponse)
        => Entities.procSimulationResult_API_Update(&SimulationResultId, &UpdateRequest, &UpdateResponse, &ErrorResponse, &RestStatusCode);
}
'@

$currentB070 = $b079.Replace('out: &ListResponse)', 'out: &ListResponse, out: &ErrorResponse)').Replace('&SimulationResultId, &ListResponse);', '&SimulationResultId, &ListResponse, &ErrorResponse, &RestStatusCode);')
Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesCurrentB070($currentB070, 'apiSimulationResult', 'SimulationResult', 'Entities', $services, $primaryKey, $listFilters, $true)) 'B070 atual deve aceitar ErrorResponse publico e status interno na List.'
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesCurrentB070($b079, 'apiSimulationResult', 'SimulationResult', 'Entities', $services, $primaryKey, $listFilters, $true)) 'B070 atual deve distinguir a List historica sem ErrorResponse publico.'

Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesB079($b079, 'apiSimulationResult', 'SimulationResult', 'Entities', $services, $primaryKey, $listFilters, $true)) 'B079 deve aceitar Get/Create/Update com status e erro internos preservando List B070.'
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesB079($b079.Replace('&ErrorResponse, &RestStatusCode', '&RestStatusCode, &ErrorResponse'), 'apiSimulationResult', 'SimulationResult', 'Entities', $services, $primaryKey, $listFilters, $true)) 'B079 deve rejeitar argumentos internos de status/erro divergentes.'
$b079WithoutPost = $b079 -replace '(?m)^\s*\[RestMethod\(POST\)\]\r?\n', ''
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesB079($b079WithoutPost, 'apiSimulationResult', 'SimulationResult', 'Entities', $services, $primaryKey, $listFilters, $true)) 'B079 deve exigir Create exposto como POST no API Object.'
$b079WithoutPathVariable = $b079.Replace('{&SimulationResultId}', '{SimulationResultId}')
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesB079($b079WithoutPathVariable, 'apiSimulationResult', 'SimulationResult', 'Entities', $services, $primaryKey, $listFilters, $true)) 'B079 deve exigir variavel GeneXus no RestPath parametrizado para Get/Update.'
Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesPreviousB079RestMethodContract($b079WithoutPathVariable, 'apiSimulationResult', 'SimulationResult', 'Entities', $services, $primaryKey, $listFilters, $true)) 'B079 anterior com RestPath parametrizado sem & deve ser reconhecido apenas como migravel.'
$b079InternalErrorOnly = $b079.Replace(', out: &ErrorResponse)', ')')
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesB079($b079InternalErrorOnly, 'apiSimulationResult', 'SimulationResult', 'Entities', $services, $primaryKey, $listFilters, $true)) 'B079 atual deve rejeitar ErrorResponse apenas interno no API Object.'
Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesB079InternalErrorOnly($b079InternalErrorOnly, 'apiSimulationResult', 'SimulationResult', 'Entities', $services, $primaryKey, $listFilters, $true)) 'B079 legado deve reconhecer ErrorResponse apenas interno para migracao conservadora.'

$b079WithoutSecurityLevel = $b079 -replace '(?m)^\s*\[SecurityLevel\([^)]+\)\]\r?\n', ''
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesB079($b079WithoutSecurityLevel, 'apiSimulationResult', 'SimulationResult', 'Entities', $services, $primaryKey, $listFilters, $true)) 'B079 atual deve exigir SecurityLevel explicito.'
Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesPreviousB079SecurityLevelContract($b079WithoutSecurityLevel, 'apiSimulationResult', 'SimulationResult', 'Entities', $services, $primaryKey, $listFilters, $true)) 'MatchesPreviousB079SecurityLevelContract deve aceitar fonte B079 valido sem SecurityLevel.'
$b079WithoutSecurityLevelOrPost = $b079WithoutSecurityLevel -replace '(?m)^\s*\[RestMethod\(POST\)\]\r?\n', ''
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesPreviousB079SecurityLevelContract($b079WithoutSecurityLevelOrPost, 'apiSimulationResult', 'SimulationResult', 'Entities', $services, $primaryKey, $listFilters, $true)) 'MatchesPreviousB079SecurityLevelContract deve rejeitar fontes sem RestMethod(POST).'

[string[]]$servicesWithDelete = @('List', 'Get', 'Create', 'Update', 'Delete')
$b079WithDelete = $b079.Replace(
    '        => Entities.procSimulationResult_API_Update(&SimulationResultId, &UpdateRequest, &UpdateResponse, &ErrorResponse, &RestStatusCode);',
    @'
        => Entities.procSimulationResult_API_Update(&SimulationResultId, &UpdateRequest, &UpdateResponse, &ErrorResponse, &RestStatusCode);
    [RestMethod(DELETE)]
    [RestPath("/simulationresult/{&SimulationResultId}")]
    [SecurityLevel(Authentication)]
    Delete(in: &SimulationResultId, out: &ErrorResponse)
        => Entities.procSimulationResult_API_Delete(&SimulationResultId, &ErrorResponse, &RestStatusCode);
'@)
Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesB079($b079WithDelete, 'apiSimulationResult', 'SimulationResult', 'Entities', $servicesWithDelete, $primaryKey, $listFilters, $true)) 'B079 deve reconhecer Delete com PK, ErrorResponse e RestStatusCode.'
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesB079($b079, 'apiSimulationResult', 'SimulationResult', 'Entities', $servicesWithDelete, $primaryKey, $listFilters, $true)) 'B079 deve recusar plano com Delete quando o Source ainda nao declara o servico.'
$b079WithDeleteWithoutMethod = $b079WithDelete -replace '(?m)^\s*\[RestMethod\(DELETE\)\]\r?\n', ''
Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesB079($b079WithDeleteWithoutMethod, 'apiSimulationResult', 'SimulationResult', 'Entities', $servicesWithDelete, $primaryKey, $listFilters, $true)) 'B079 deve exigir Delete exposto como DELETE no API Object.'
Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesPreviousB079RestMethodContract($b079WithDeleteWithoutMethod, 'apiSimulationResult', 'SimulationResult', 'Entities', $servicesWithDelete, $primaryKey, $listFilters, $true)) 'B079 anterior sem RestMethod(DELETE) deve permanecer migravel.'
$b079WithDeleteInternalErrorOnly = $b079WithDelete.Replace(', out: &ErrorResponse)', ')')
Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::MatchesB079InternalErrorOnly($b079WithDeleteInternalErrorOnly, 'apiSimulationResult', 'SimulationResult', 'Entities', $servicesWithDelete, $primaryKey, $listFilters, $true)) 'B079 legado deve reconhecer Delete com ErrorResponse apenas interno.'

Assert-False ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::LooksLikeRestCompleteServiceGroupSource($b054)) 'Skeleton B054 não é contrato REST completo.'
Assert-True ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::LooksLikeRestCompleteServiceGroupSource($b079)) 'Source B079 com ErrorResponse/RestStatusCode é contrato REST completo.'
try {
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::ThrowIfB054WouldDowngradeRestContract($b079)
    throw 'ASSERT_TRUE_FAILED: B054 deve recusar rebaixar Source REST completo.'
}
catch {
    if ($_.Exception.Message -eq 'ASSERT_TRUE_FAILED: B054 deve recusar rebaixar Source REST completo.') {
        throw
    }

    $refusal = $_.Exception.Message
    if ($_.Exception.InnerException -is [System.InvalidOperationException]) {
        $refusal = $_.Exception.InnerException.Message
    }

    Assert-True ($refusal -eq [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::B054RestDowngradeRefusal) 'A recusa B054 deve usar a mensagem canônica.'
}

[GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanServiceSourceContract]::ThrowIfB054WouldDowngradeRestContract($b054)

Write-Output 'PASS: ApiPlanServiceSourceContract'
