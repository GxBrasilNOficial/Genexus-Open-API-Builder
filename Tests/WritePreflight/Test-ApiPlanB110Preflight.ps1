#requires -Version 7.4
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$sources = @(
    (Join-Path $PSScriptRoot '../../Src/Extension/Diagnostics/ApiPlanExistingNamePolicy.cs'),
    (Join-Path $PSScriptRoot '../../Src/Extension/Diagnostics/ReconstructedContractAcknowledgement.cs'),
    (Join-Path $PSScriptRoot '../../Src/Extension/Diagnostics/ApiPlanB110Preflight.cs')
)
Add-Type -Path $sources
$guard = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanB110Preflight]
$block = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanB110PreflightException]
$guid = [Guid]::NewGuid()
$sections = [string[]]@('fields.listFilters', 'services')
$ack = [GenexusOpenApiBuilder.Extension.Diagnostics.ReconstructedContractAcknowledgement]::new($sections)

function Assert-B110Block {
    param([scriptblock]$Operation, [string]$ExpectedText)
    try {
        & $Operation
    } catch {
        $failure = $_.Exception
        while ($failure -isnot $block -and $null -ne $failure.InnerException) {
            $failure = $failure.InnerException
        }
        if ($failure -isnot $block) { throw }
        if ($block::ReportCode -cne 'B110') { throw 'Bloqueio B110 com código incorreto.' }
        if (-not $failure.Message.Contains($ExpectedText, [StringComparison]::Ordinal)) {
            throw "Texto de bloqueio inesperado: $($failure.Message)"
        }
        return
    }
    throw "Bloqueio esperado não ocorreu: $ExpectedText"
}

$guard::RequireExistingName($true, $guid, 'apiEmpresa', 'APIEMPRESA')
$guard::RequireExistingName($false, $null, $null, 'apiNova')
Assert-B110Block { $guard::RequireExistingName($true, $guid, 'apiEmpresa', 'apiOutra') } 'Renomeação de API existente bloqueada'

$guard::RequireReconstructedContract($false, $false, $false, $sections, $null)
$guard::RequireReconstructedContract($true, $false, $false, $sections, $ack)
Assert-B110Block { $guard::RequireReconstructedContract($true, $false, $false, $sections, $null) } 'confirme as seções recuperadas'
Assert-B110Block { $guard::RequireReconstructedContract($true, $false, $true, $sections, $ack) } 'não contém levels'

$package = [IO.File]::ReadAllText((Join-Path $PSScriptRoot '../../Src/Extension/Package.cs'))
foreach ($required in @(
    'ApiPlanB110Preflight.RequireExistingName(',
    'ApiPlanB110Preflight.RequireReconstructedContract(',
    'ex is ApiPlanB110PreflightException',
    'report.AddBlocked("Preflight", reportCode, ex.Message)'
)) {
    if (-not $package.Contains($required, [StringComparison]::Ordinal)) {
        throw "Apply não usa o código B110 no relatório: $required"
    }
}

Write-Output 'PASS: ApiPlanB110Preflight'
