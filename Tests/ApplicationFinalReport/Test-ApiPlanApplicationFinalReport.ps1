Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$helperPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanApplicationFinalReport.cs'
$dialogPath = Join-Path $PSScriptRoot '..\..\Src\Extension\ApiPlanApplicationFinalReportDialog.cs'
$runtimeAssemblies = @([System.AppContext]::GetData('TRUSTED_PLATFORM_ASSEMBLIES') -split [System.IO.Path]::PathSeparator |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
if ($runtimeAssemblies.Count -eq 0) {
    throw 'Assemblies do runtime PowerShell atual não foram encontrados.'
}

Add-Type -Path $helperPath -ReferencedAssemblies @($runtimeAssemblies | Sort-Object -Unique)

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw "ASSERT_TRUE_FAILED: $Message"
    }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Message)
    if ($Expected -ne $Actual) {
        throw "ASSERT_EQUAL_FAILED: $Message (expected='$Expected' actual='$Actual')"
    }
}

$collector = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanApplicationFinalReportCollector]::new('Wizard', 'Contrato', 'apiContrato')
$collector.AddFromWriteStatus('SDT', 'sdtContrato_API_Response', 'Created', 'OwnResponse')
$collector.AddFromWriteStatus('SDT', 'sdt_API_ErrorResponse', 'Reencountered', 'SharedErrorResponse')
$collector.AddFromWriteStatus('Procedure', 'procContrato_API_List', 'Created', 'List')
$collector.AddFromWriteStatus('API Object', 'apiContrato', 'Created')
$collector.SetMainObject('apiContrato', [guid]'11111111-1111-1111-1111-111111111111')
$collector.AddWarning('Descricoes de servico usaram fallback em ingles.')

$report = $collector.Build([timespan]::FromMilliseconds(1250))
Assert-Equal ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanApplicationFinalOutcome]::SuccessWithWarnings) $report.Outcome 'Com avisos o outcome deve ser SuccessWithWarnings.'
Assert-Equal 'API gerada com avisos.' $report.Headline 'Headline parcial do Wizard.'
# Created: Response SDT, Procedure List, API Object = 3; Updated: ErrorResponse = 1
Assert-Equal 3 $report.CreatedCount 'Criados: Response, List Procedure, API Object.'
Assert-Equal 1 $report.UpdatedCount 'Atualizados: ErrorResponse reencontrado.'
Assert-Equal 0 $report.BlockedCount 'Sem bloqueios.'
Assert-Equal 1 $report.WarningCount 'Um aviso.'
Assert-True ($report.BuildOutputSummary() -match "\[B081\] Relatorio final") 'Output summary deve citar B081.'
Assert-True ($report.BuildReadableBody() -match 'Criados \(3\)') 'Corpo legivel lista criados.'
Assert-True ($report.BuildReadableBody() -match 'Atualizados \(1\)') 'Corpo legivel lista atualizados.'

$long = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanApplicationFinalReportCollector]::new('Wizard', 'Teste', 'apiTeste')
$long.AddWarning('Descricoes de servico usaram fallback em ingles (Idioma principal da KB ainda nao validado por API publica; fallback tecnico em ingles registrado no ApiPlan.).')
$longBody = $long.Build([timespan]::FromMilliseconds(10)).BuildReadableBody()
Assert-True ($longBody -match '(?m)^  - Descricoes') 'Aviso longo inicia na primeira linha.'
Assert-True (($longBody -split "`n").Count -ge 2) 'Aviso longo deve quebrar em mais de uma linha no corpo legivel.'

$blocked = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanApplicationFinalReportCollector]::new('Wizard', 'Contrato', 'apiContrato')
$blocked.AddBlocked('Preflight', 'B063', 'colisao externa')
$blockedReport = $blocked.Build([timespan]::FromMilliseconds(40))
Assert-Equal ([GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanApplicationFinalOutcome]::Interrupted) $blockedReport.Outcome 'Bloqueio interrompe.'
Assert-Equal 'Geracao interrompida.' $blockedReport.Headline 'Headline de interrupcao do Wizard.'

$sync = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanApplicationFinalReportCollector]::new('Sincronizar', 'Teste', 'apiTeste')
$sync.AddFromWriteStatus('SDT', 'sdtTeste_API_Response', 'Reencountered')
$syncReport = $sync.Build([timespan]::FromSeconds(2))
Assert-Equal 'API sincronizada com sucesso.' $syncReport.Headline 'Headline de Sync sem avisos.'

$remove = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanApplicationFinalReportCollector]::new('Remover', 'Teste', 'apiTeste')
$remove.AddDeletedItems(@('API:apiTeste', 'Procedure:procTeste_API_List', 'SDT:sdtTeste_API_Response', 'File:apiTeste_Metadata', 'Folder:TesteOpenApi', 'Folder:OtherOpenApi:PreservedNonEmpty'))
$removeReport = $remove.Build([timespan]::Zero)
Assert-Equal 'API removida com avisos.' $removeReport.Headline 'Folder preservado vira aviso.'
Assert-Equal 5 $removeReport.DeletedCount 'Cinco removidos reais.'
Assert-Equal 1 $removeReport.WarningCount 'Aviso do Folder preservado.'
Assert-Equal 'API Object' $removeReport.Deleted[0].ObjectKind 'API: mapeia para API Object.'
Assert-True ($removeReport.BuildReadableBody($false) -notmatch '(?m)^API removida') 'Corpo sem headline nao repete o titulo.'

$dialogSource = Get-Content -Raw -LiteralPath $dialogPath
Assert-True ($dialogSource -match 'EnsureBodyScrollBars') 'Dialogo B081 deve recalcular a rolagem apos o layout.'
Assert-True ($dialogSource -match 'GetPositionFromCharIndex') 'Dialogo B081 deve medir a ultima linha visual do corpo.'
Assert-True ($dialogSource -match 'ScrollBars\.Vertical') 'Dialogo B081 deve habilitar rolagem vertical quando o corpo exceder a area.'

Write-Host 'Test-ApiPlanApplicationFinalReport.ps1 OK'
