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
    List(in: &ApiPage, in: &ApiPageSize, in: &SimulationResultId, out: &ListResponse)
        => Entities.procSimulationResult_API_List(&ApiPage, &ApiPageSize, &SimulationResultId, &ListResponse);
    [RestPath("/simulationresult/{&SimulationResultId}")]
    Get(in: &SimulationResultId, out: &GetResponse, out: &ErrorResponse)
        => Entities.procSimulationResult_API_Get(&SimulationResultId, &GetResponse, &ErrorResponse, &RestStatusCode);
    [RestMethod(POST)]
    [RestPath("/simulationresult")]
    Create(in: &CreateRequest, out: &CreateResponse, out: &ErrorResponse)
        => Entities.procSimulationResult_API_Create(&CreateRequest, &CreateResponse, &ErrorResponse, &RestStatusCode);
    [RestMethod(PUT)]
    [RestPath("/simulationresult/{&SimulationResultId}")]
    Update(in: &SimulationResultId, in: &UpdateRequest, out: &UpdateResponse, out: &ErrorResponse)
        => Entities.procSimulationResult_API_Update(&SimulationResultId, &UpdateRequest, &UpdateResponse, &ErrorResponse, &RestStatusCode);
}
'@

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

Write-Output 'PASS: ApiPlanServiceSourceContract'
