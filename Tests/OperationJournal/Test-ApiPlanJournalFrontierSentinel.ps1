#requires -Version 7.4

<#
    S-B111 / F3 — sentinela das duas afirmações que o diário faz sobre o API Object.

    As duas foram medidas erradas na IDE em 2026-09-14 (seções 6.6 e 6.7 do registro da P3), e
    nenhuma delas é observável offline: uma vive no fluxo do Package, a outra depende de
    objetos reais da KB. Por isso a trava é textual sobre o fonte — feia, mas é a única que
    pega a regressão antes da IDE.

    1. `ApiPhysicallySaved` exige gravação confirmada. Um Apply que não escreve o API Object
       registrava a fronteira assim mesmo, porque bastava um GUID conhecido. É justamente a
       fronteira que impede a recuperação de repetir o Save do API Object: afirmá-la à toa
       fecha a porta que deveria ficar aberta.

    2. `composite.apiGuid` é o GUID do API Object, não o do próprio objeto. Os writers de
       Business Component e de List passavam `procedure.Guid`, de modo que cada Procedure
       apontava para si mesma. Identidade composta é o que autoriza exclusão na P4.
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw "ASSERT_TRUE_FAILED: $Message" }
}

function Get-Source {
    param([string]$RelativePath)
    $path = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "FONTE_AUSENTE: $RelativePath"
    }
    return [System.IO.File]::ReadAllText($path)
}

# --- 1. A fronteira do API exige gravação confirmada ----------------------------------------
$package = Get-Source 'Src\Extension\Package.cs'

$frontierCalls = @([regex]::Matches($package, 'NoteApiPhysicallySaved\(\)'))
Assert-True ($frontierCalls.Count -eq 1) `
    "A fronteira ApiPhysicallySaved deve ter um único ponto de registro no Package; encontrados $($frontierCalls.Count)."

Assert-True ($package -match 'var apiPhysicallySaved = report\.ApiSaveCount > 0;') `
    'O registro da fronteira deve derivar de gravação confirmada (report.ApiSaveCount > 0).'

Assert-True ($package -match 'if \(apiPhysicallySaved && persistedApiGuid\.HasValue && persistedApiGuid\.Value != Guid\.Empty\)') `
    'A fronteira só pode ser registrada com gravação confirmada E identidade conhecida — um GUID sozinho não prova Save.'

# --- 2. `composite.apiGuid` aponta para o API Object -----------------------------------------
# O sexto argumento de CompositeIdentity é o apiGuid. `procedure.Guid` ali faz cada Procedure
# apontar para si mesma, e foi assim que o inventário do diário saiu na medição de 2026-09-14.
foreach ($relative in @(
    'Src\Extension\Diagnostics\ApiPlanBusinessComponentWriter.cs',
    'Src\Extension\Diagnostics\ApiPlanListProcedureWriter.cs')) {

    $source = Get-Source $relative
    $selfReferences = @([regex]::Matches(
        $source,
        'new CompositeIdentity\((?:[^()]|\([^()]*\))*?procedure\.Description\s*,(?:[^()]|\([^()]*\))*?procedure\.Guid\s*\)',
        [System.Text.RegularExpressions.RegexOptions]::Singleline))

    Assert-True ($selfReferences.Count -eq 0) `
        "$relative não pode passar procedure.Guid como apiGuid de CompositeIdentity; encontradas $($selfReferences.Count) ocorrência(s)."

    Assert-True ($source -match 'plan\.PlannedApiGuid \?\? Guid\.Empty') `
        "$relative deve usar o GUID do API Object planejado, com Guid.Empty quando ele ainda não existe."
}

Write-Output 'PASS: ApiPlanJournalFrontierSentinel'
