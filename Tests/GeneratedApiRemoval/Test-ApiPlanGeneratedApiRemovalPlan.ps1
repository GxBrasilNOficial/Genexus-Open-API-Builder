Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$helperPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanGeneratedApiRemovalPlan.cs'
$newtonsoftPath = Get-ChildItem -Path (Join-Path $env:USERPROFILE '.nuget\packages\newtonsoft.json') -Filter Newtonsoft.Json.dll -Recurse |
    Where-Object { $_.FullName -match '\\lib\\netstandard2\.0\\Newtonsoft\.Json\.dll$' } |
    Sort-Object FullName -Descending |
    Select-Object -First 1 -ExpandProperty FullName

if ([string]::IsNullOrWhiteSpace($newtonsoftPath)) {
    throw 'Newtonsoft.Json.dll não encontrado no cache NuGet local.'
}

$runtimeAssemblies = @([System.AppContext]::GetData('TRUSTED_PLATFORM_ASSEMBLIES') -split [System.IO.Path]::PathSeparator |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
if ($runtimeAssemblies.Count -eq 0) {
    throw 'Assemblies do runtime PowerShell atual não foram encontrados.'
}

Add-Type -Path $helperPath -ReferencedAssemblies @(($runtimeAssemblies + $newtonsoftPath) | Sort-Object -Unique)

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw "ASSERT_TRUE_FAILED: $Message" }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Message)
    if ($Expected -ne $Actual) {
        throw "ASSERT_EQUAL_FAILED: $Message (expected='$Expected' actual='$Actual')"
    }
}

$txGuid = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
$apiGuid = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
$metadata = [Newtonsoft.Json.Linq.JObject]::Parse(@"
{
  `"schemaVersion`": `"GOAB_API_METADATA_B060_V1`",
  `"ownership`": {
    `"transactionName`": `"Teste`",
    `"transactionGuid`": `"$txGuid`",
    `"apiName`": `"apiTeste`",
    `"apiGuid`": `"$apiGuid`",
    `"metadataFileName`": `"apiTeste_Metadata`"
  },
  `"objects`": {
    `"transactionFolder`": { `"name`": `"TesteOpenApi`", `"wasCreated`": true },
    `"apiObject`": { `"name`": `"apiTeste`", `"guid`": `"$apiGuid`" },
    `"procedures`": [ `"procTeste_API_List`", `"procTeste_API_Get`", `"procTeste_API_Create`", `"procTeste_API_Update`" ],
    `"sdts`": {
      `"createRequest`": `"sdtTeste_API_CreateRequest`",
      `"updateRequest`": `"sdtTeste_API_UpdateRequest`",
      `"response`": `"sdtTeste_API_Response`",
      `"listFilters`": `"sdtTeste_API_ListFilters`",
      `"listResponse`": `"sdtTeste_API_ListResponse`",
      `"shared`": [ `"sdt_API_ErrorMessage`", `"sdt_API_ErrorResponse`", `"sdt_API_Pagination`" ]
    }
  }
}
"@)

$plan = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanGeneratedApiRemovalPlan]::FromMetadata($metadata, 'Teste', $txGuid)
Assert-Equal 'apiTeste' $plan.ApiName 'ApiName do plano'
Assert-Equal 4 $plan.ProcedureNames.Count 'Procedures no plano'
Assert-Equal 5 $plan.OwnSdtNames.Count 'SDTs próprios no plano'
Assert-Equal 'sdtTeste_API_ListResponse' $plan.OwnSdtNames[0] 'ListResponse deve ser o primeiro SDT a apagar'
Assert-Equal 'sdtTeste_API_Response' $plan.OwnSdtNames[$plan.OwnSdtNames.Count - 1] 'Response deve ser o último SDT a apagar'
Assert-Equal 3 $plan.SharedSdtNamesPreserved.Count 'SDTs compartilhados preservados'
Assert-True $plan.FolderWasCreated 'Folder criado pela extensão'
$summary = $plan.BuildConfirmationSummary() -replace "`r`n", "`n"
Assert-True ($summary -match 'Business Component') 'Resumo menciona BC'
Assert-True ($summary -match '(?m)^  - procTeste_API_List$') 'Procedure List em linha própria'
Assert-True ($summary -match '(?m)^  - procTeste_API_Get$') 'Procedure Get em linha própria'
Assert-True ($summary -match '(?m)^Procedures \(4\):$') 'Cabeçalho de Procedures em linha própria'

try {
    [void][GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanGeneratedApiRemovalPlan]::FromMetadata($metadata, 'Outra', $txGuid)
    throw 'ASSERT_FAILED: deveria rejeitar Transaction divergente.'
}
catch {
    if ($_.Exception.Message -notmatch 'ownership.transactionName') {
        throw
    }
}

Write-Output 'PASS: ApiPlanGeneratedApiRemovalPlan'
