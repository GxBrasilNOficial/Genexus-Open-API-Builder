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

Assert-False ($policy::ShouldRequestEnableBeforeLeavingWizard($false, $false)) 'Wizard não deve executar habilitação de BC quando BC está desligado e habilitação explícita não foi marcada.'
Assert-True ($policy::ShouldRequestEnableBeforeLeavingWizard($false, $true)) 'Wizard deve pedir confirmação antes de resumo/conclusão quando BC está desligado e habilitação explícita foi marcada.'
Assert-False ($policy::ShouldRequestEnableBeforeLeavingWizard($true, $false)) 'Transaction já apta via BC não deve pedir habilitação.'
Assert-False ($policy::ShouldRequestEnableBeforeLeavingWizard($true, $true)) 'Transaction já apta via BC não deve pedir nova habilitação mesmo com checkbox marcado.'
Assert-False ($policy::ShouldAllowApplyBusinessComponent($false, $false, $true, $true, $true)) 'Aplicação via BC não deve ser permitida quando a Transaction não está apta e a habilitação explícita não foi marcada.'
Assert-False ($policy::ShouldAllowApplyBusinessComponent($false, $true, $false, $true, $true)) 'Aplicação via BC não deve ser permitida sem SDTs disponíveis ou confirmados.'
Assert-False ($policy::ShouldAllowApplyBusinessComponent($false, $true, $true, $false, $true)) 'Aplicação via BC não deve ser permitida sem Procedures disponíveis ou confirmadas.'
Assert-False ($policy::ShouldAllowApplyBusinessComponent($false, $true, $true, $true, $false)) 'Aplicação via BC não deve ser permitida sem API Object disponível ou confirmado.'
Assert-True ($policy::ShouldAllowApplyBusinessComponent($false, $true, $true, $true, $true)) 'Marcar habilitação explícita deve permitir selecionar aplicação via BC como intenção pendente quando dependências estão disponíveis.'
Assert-True ($policy::ShouldAllowApplyBusinessComponent($true, $false, $true, $true, $true)) 'Transaction já apta via BC deve permitir aplicação via BC quando dependências estão disponíveis.'
Assert-False ($policy::ShouldApplyBusinessComponentWhenAllowed($false, $false, $true)) 'Aplicação via BC não deve ficar marcada quando ainda não é permitida.'
Assert-True ($policy::ShouldApplyBusinessComponentWhenAllowed($true, $false, $true)) 'Preferência/intenção pendente de aplicação via BC deve voltar quando a aplicação passa a ser permitida.'
Assert-True ($policy::ShouldApplyBusinessComponentWhenAllowed($true, $true, $false)) 'Seleção atual de aplicação via BC deve ser preservada quando a aplicação é permitida.'
Assert-False ($policy::ShouldApplyBusinessComponentWhenAllowed($true, $false, $false)) 'Aplicação via BC não deve ser marcada sem seleção atual nem intenção pendente.'

$pendingApplySelection = $true
$selectionAfterBusinessComponentLocalRefresh = $policy::ResolveApplyBusinessComponentAfterGenerationRefresh(
    $true,
    $false,
    $false,
    $false,
    $false,
    $false,
    $pendingApplySelection)
Assert-False $selectionAfterBusinessComponentLocalRefresh 'Refresh local da aba BC sem dependências não deve manter o checkbox visual marcado.'

$selectionAfterFullGenerationRefresh = $policy::ResolveApplyBusinessComponentAfterGenerationRefresh(
    $true,
    $false,
    $true,
    $true,
    $true,
    $selectionAfterBusinessComponentLocalRefresh,
    $pendingApplySelection)
Assert-True $selectionAfterFullGenerationRefresh 'Após habilitar BC e recalcular dependências reais, a intenção pendente de B055 deve ser restaurada antes da seleção final.'

$finalApplyBusinessComponent = $selectionAfterFullGenerationRefresh -and $true
Assert-True $finalApplyBusinessComponent 'TryCreateSelection deve produzir ApplyBusinessComponent=True quando o refresh completo restaurou a intenção pendente.'

Write-Output 'PASS: PrototypeWizardBusinessComponentNavigationPolicy'
