Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scopePath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanWritePreflightScope.cs'
Add-Type -Path $scopePath

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

function Assert-SequenceEqual {
    param([string[]]$Expected, [string[]]$Actual, [string]$Message)
    if ($Expected.Count -ne $Actual.Count) {
        throw "ASSERT_SEQUENCE_FAILED: $Message ExpectedCount=$($Expected.Count) ActualCount=$($Actual.Count)"
    }

    for ($index = 0; $index -lt $Expected.Count; $index++) {
        if ($Expected[$index] -ne $Actual[$index]) {
            throw "ASSERT_SEQUENCE_FAILED: $Message Index=$index Expected='$($Expected[$index])' Actual='$($Actual[$index])'"
        }
    }
}

$stageKind = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanWritePreflightStageKind]
$blocks = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanWritePreflightStageBlock[]]@(
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanWritePreflightStageBlock]::new($stageKind::Sdts, 'SDTs', $true),
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanWritePreflightStageBlock]::new($stageKind::Procedures, 'Procedures', $true),
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanWritePreflightStageBlock]::new($stageKind::ApiObject, 'API Object', $true),
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanWritePreflightStageBlock]::new($stageKind::MetadataFile, 'Metadata File', $true)
)

$listOnlyScope = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanWritePreflightScope]::FromSelection($false, $false, $false, $false, $true, $false)
Assert-True $listOnlyScope.RequireSdts 'List exige SDTs porque escreve/reencontra SDTs de lista.'
Assert-True $listOnlyScope.RequireProcedures 'List exige Procedures.'
Assert-True $listOnlyScope.RequireApiObject 'List exige API Object.'
Assert-False $listOnlyScope.RequireMetadataFile 'GenerateMetadata=False não deve exigir Metadata File.'
Assert-SequenceEqual @('SDTs', 'Procedures', 'API Object') $listOnlyScope.SelectBlockedStageNames($blocks) 'List sem metadata não deve bloquear por Metadata File.'

$metadataScope = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanWritePreflightScope]::FromSelection($false, $false, $false, $true, $false, $false)
Assert-True $metadataScope.RequireSdts 'Metadata exige SDTs.'
Assert-True $metadataScope.RequireProcedures 'Metadata exige Procedures.'
Assert-True $metadataScope.RequireApiObject 'Metadata exige API Object.'
Assert-True $metadataScope.RequireMetadataFile 'GenerateMetadata=True deve exigir Metadata File.'
Assert-SequenceEqual @('SDTs', 'Procedures', 'API Object', 'Metadata File') $metadataScope.SelectBlockedStageNames($blocks) 'Metadata deve preservar bloqueios dependentes e Metadata File.'

$bcScope = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanWritePreflightScope]::FromSelection($false, $false, $false, $false, $false, $true)
Assert-True $bcScope.RequireSdts 'Business Component exige SDTs.'
Assert-True $bcScope.RequireProcedures 'Business Component exige Procedures.'
Assert-True $bcScope.RequireApiObject 'Business Component exige API Object.'
Assert-False $bcScope.RequireMetadataFile 'Business Component sem GenerateMetadata não deve exigir Metadata File.'

$noneScope = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanWritePreflightScope]::FromSelection($false, $false, $false, $false, $false, $false)
Assert-False $noneScope.RequireSdts 'Sem etapas, SDTs não devem ser exigidos.'
Assert-False $noneScope.RequireProcedures 'Sem etapas, Procedures não devem ser exigidas.'
Assert-False $noneScope.RequireApiObject 'Sem etapas, API Object não deve ser exigido.'
Assert-False $noneScope.RequireMetadataFile 'Sem etapas, Metadata File não deve ser exigido.'
Assert-SequenceEqual @() $noneScope.SelectBlockedStageNames($blocks) 'Sem etapas, nenhum bloqueio deve ser selecionado.'

Write-Output 'PASS: ApiPlanWritePreflightScope'
