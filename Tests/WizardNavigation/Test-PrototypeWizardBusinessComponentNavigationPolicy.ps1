Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$policyPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\PrototypeWizardBusinessComponentNavigationPolicy.cs'
Add-Type -Path $policyPath

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

$policy = [GenexusOpenApiBuilder.Extension.Diagnostics.PrototypeWizardBusinessComponentNavigationPolicy]

Assert-False ($policy::ShouldRequestEnableOnNext($false, $false)) 'Aba Business Component não deve bloquear navegação quando BC está desligado e habilitação explícita não foi marcada.'
Assert-True ($policy::ShouldRequestEnableOnNext($false, $true)) 'Aba Business Component deve pedir confirmação quando BC está desligado e habilitação explícita foi marcada.'
Assert-False ($policy::ShouldRequestEnableOnNext($true, $false)) 'Transaction já apta via BC não deve pedir habilitação.'
Assert-False ($policy::ShouldRequestEnableOnNext($true, $true)) 'Transaction já apta via BC não deve pedir nova habilitação mesmo com checkbox marcado.'

Write-Output 'PASS: PrototypeWizardBusinessComponentNavigationPolicy'
