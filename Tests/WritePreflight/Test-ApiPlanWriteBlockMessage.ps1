#requires -Version 7.4
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot '../../Src/Extension/Diagnostics'
Add-Type -Path @((Join-Path $root 'ApiPlanWriteBlockMessage.cs'), (Join-Path $root 'ApiPlanCollisionConflict.cs'), (Join-Path $root 'ApiPlanWritePreflightScope.cs'))
$scope = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanWritePreflightScope]::FromSelection($true,$false,$false,$false,$false,$false)
$kind = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanWritePreflightStageKind]
$stages = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanWritePreflightStageBlock[]]@([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanWritePreflightStageBlock]::new($kind::Procedures,'Procedures',$true), [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanWritePreflightStageBlock]::new($kind::ApiObject,'API Object',$true))
$collisions = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanCollisionConflict[]]@([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanCollisionConflict]::new('procEmpresa_API_List','Procedure','Modulo','Folder'))
$message = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanWriteBlockMessage]::Build('Apply bloqueado: ', $scope.SelectBlockedStageNames($stages), $collisions, '. Nenhum Save.', [string[]]@('Procedure externa; API bloqueada por dependência'))
foreach ($term in @('Procedures', 'API Object', 'procEmpresa_API_List', 'Procedure externa', 'Nenhum Save.')) {
    if (-not $message.Contains($term)) { throw "Causa omitida: $term" }
}
Write-Output 'PASS: ApiPlanWriteBlockMessage'
