Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$helperPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanOwnedObjectDescription.cs'
Add-Type -Path $helperPath

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

$description = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanOwnedObjectDescription]

$procedureName = 'procTeste_API_List'
$procedureLegacy = $description::CreateLegacyProcedureDescription($procedureName)
Assert-True ($description::IsOwnedProcedure($procedureName + $description::Suffix, $procedureName)) 'Procedure canônica deve ser própria.'
Assert-True ($description::IsOwnedProcedure($procedureLegacy, $procedureName)) 'Procedure legada compatível deve ser própria.'
Assert-False ($description::IsOwnedProcedure('Genexus Open API Builder B051 - List', $procedureName)) 'Procedure com backlog divergente deve bloquear.'
Assert-False ($description::IsOwnedProcedure('Genexus Open API Builder B050 - Get', $procedureName)) 'Procedure com serviço divergente deve bloquear.'
Assert-False ($description::IsOwnedProcedure('Genexus Open API Builder B050-B053 Procedure - B050 - List', 'ProcedureHumana')) 'Objeto sem nome gerado não deve herdar posse legada.'

$sdtName = 'sdtTeste_API_CreateRequest'
$sdtLegacy = $description::CreateLegacySdtDescription($sdtName)
Assert-True ($description::IsOwnedSdt($sdtName + $description::Suffix, $sdtName)) 'SDT canônico deve ser próprio.'
Assert-True ($description::IsOwnedSdt($sdtLegacy, $sdtName)) 'SDT legado compatível deve ser próprio.'
Assert-False ($description::IsOwnedSdt('Genexus Open API Builder B041 - CreateRequest', $sdtName)) 'SDT com backlog divergente deve bloquear.'
Assert-False ($description::IsOwnedSdt('Genexus Open API Builder B040-B046 SDT - B040 - UpdateRequest', $sdtName)) 'SDT com kind divergente deve bloquear.'
Assert-True ($description::IsOwnedSdt('Genexus Open API Builder B040-B046 SDT - B045/B046 - SharedErrorResponse', 'sdt_API_ErrorResponse')) 'SDT compartilhado legado compatível deve ser próprio.'
Assert-True ($description::IsOwnedSdt('Genexus Open API Builder B040-B046 SDT - B102 - SharedErrorMessage', 'sdt_API_ErrorMessage')) 'SDT compartilhado de item de erro do B102 deve ser próprio.'

$metadataName = 'apiTeste_Metadata'
$metadataLegacy = $description::CreateLegacyMetadataDescription('Teste', 'apiTeste')
Assert-True ($description::IsOwnedMetadataFile($metadataName + $description::Suffix, $metadataName, 'Teste')) 'Metadata canônica deve ser própria.'
Assert-True ($description::IsOwnedMetadataFile($metadataLegacy, $metadataName, 'Teste')) 'Metadata legada compatível deve ser própria.'
Assert-False ($description::IsOwnedMetadataFile($metadataLegacy, $metadataName, 'Outra')) 'Metadata de outra Transaction deve bloquear.'
Assert-False ($description::IsOwnedMetadataFile('Genexus Open API Builder B060 Metadata File - Transaction=Teste - Api=apiOutra', $metadataName, 'Teste')) 'Metadata de outra API deve bloquear.'
Assert-False ($description::IsOwnedMetadataFile('Genexus Open API Builder B060 Metadata File - Transaction=Teste - Api=apiTeste', 'ArquivoHumano', 'Teste')) 'Nome de metadata incompatível deve bloquear.'

Write-Output 'PASS: ApiPlanOwnedObjectDescription'
