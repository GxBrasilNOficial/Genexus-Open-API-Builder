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

Write-Output 'PASS: ApiPlanServiceSourceContract'
