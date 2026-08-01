#requires -Version 7.4

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$checker = Join-Path $repositoryRoot 'scripts\Invoke-PrePushMechanicalChecks.ps1'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Get-FixtureKind {
    param([string]$Text, [ValidateSet('restore', 'build')] [string]$Phase)
    if ($Phase -eq 'restore' -and $Text -match '(?i)(lock file.*(inconsistent|out of date)|NU1004|--locked-mode)') { return 'lockFileInconsistent' }
    if ($Text -match '(?i)(NU1301|unable to load the service index|no such host|name or service not known|network.*(unavailable|error)|timed out|proxy|connection.*(refused|reset))') { return 'networkOrFeedUnavailable' }
    if ($Text -match '(?i)(NETSDK[0-9]+|SDK.*(not found|could not be found)|A compatible installed .NET SDK)') { return 'sdkUnavailable' }
    if ($Phase -eq 'build') { return 'compilationOrBuildFailure' }
    return 'restoreFailure'
}

Assert-True (Test-Path -LiteralPath $checker -PathType Leaf) "Checker não encontrado: $checker"
$source = Get-Content -LiteralPath $checker -Raw

# A fronteira é propositalmente simples: examina apenas o fonte de produção e
# não interpreta referências em mensagens ou fixtures como invocações operacionais.
Assert-True ($source -notmatch '(?im)^\s*(?:&|\.)\s+.*\bTools[\\/]') 'O checker não pode invocar scripts de Tools.'
Assert-True ($source -notmatch '(?i)Invoke-Expression|ScriptBlock::Create|&\s*\(') 'O checker não pode usar invocação dinâmica.'
Assert-True ($source -notmatch '(?i)(?:C:|%ProgramFiles%)\\[^\r\n]*Program Files|C:\\GxModels') 'O checker não pode acessar Program Files nem uma KB local.'
Assert-True ($source -notmatch '(?i)Start-Process\s+.*(?:genexus|dll)') 'O checker não pode iniciar IDE ou operações de DLL.'
Assert-True ($source -match 'Tests/ServiceSourceContract/Test-ApiPlanServiceSourceContract\.ps1') 'O checker deve executar o teste unitário do parser Service Source.'
Assert-True ($source -match 'Tests/MetadataIntegrity/Test-ApiPlanMetadataIntegrity\.ps1') 'O checker deve executar o teste unitário da integridade B067.'
Assert-True ($source -match 'Tests/WizardPreferences/Test-PrototypeWizardPreferences\.ps1') 'O checker deve executar o teste unitário das preferências do wizard.'
Assert-True ($source -match 'Tests/WizardNavigation/Test-PrototypeWizardBusinessComponentNavigationPolicy\.ps1') 'O checker deve executar o teste unitário da navegação do wizard.'
Assert-True ($source -match 'Tests/WritePreflight/Test-ApiPlanWritePreflightScope\.ps1') 'O checker deve executar o teste unitário do escopo de preflight.'
Assert-True ($source -match 'Tests/ListProcedure/Test-ApiPlanListProcedureReencounterPolicy\.ps1') 'O checker deve executar o teste unitário do reencontro B070.'

$fixtures = @(
    @{ Text = 'error NU1004: The package lock file is inconsistent.'; Phase = 'restore'; Expected = 'lockFileInconsistent' },
    @{ Text = 'NU1301: Unable to load the service index for source.'; Phase = 'restore'; Expected = 'networkOrFeedUnavailable' },
    @{ Text = 'NETSDK1045: The current .NET SDK does not support.'; Phase = 'build'; Expected = 'sdkUnavailable' },
    @{ Text = 'error CS1002: ; expected'; Phase = 'build'; Expected = 'compilationOrBuildFailure' }
)
foreach ($fixture in $fixtures) {
    Assert-True ((Get-FixtureKind -Text $fixture.Text -Phase $fixture.Phase) -eq $fixture.Expected) "Classificação divergente para '$($fixture.Text)'."
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("OpenApiBuilderPrePushChecker-" + [guid]::NewGuid().ToString('N'))
try {
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'remote.git'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\scripts'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Src'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\ServiceSourceContract'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\MetadataIntegrity'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\WizardPreferences'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\WizardNavigation'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\WritePreflight'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\ListProcedure'))
    & git init --bare (Join-Path $tempRoot 'remote.git') | Out-Null
    Push-Location (Join-Path $tempRoot 'repo')
    try {
        & git init | Out-Null
        & git checkout -b main | Out-Null
        & git config user.email 'checker@example.invalid'
        & git config user.name 'PrePush Checker Test'
        [System.IO.File]::WriteAllText((Join-Path $PWD 'README.md'), "fixture`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD '.gitignore'), "bin/`nobj/`n", [System.Text.UTF8Encoding]::new($false))
        & dotnet new sln --name GenexusOpenApiBuilder --output Src --format sln | Out-Null
        Assert-True ($LASTEXITCODE -eq 0) 'Não foi possível criar a solution mínima da fixture.'
        & dotnet new classlib --name Fixture --output Src\Fixture --framework net10.0 --no-restore | Out-Null
        Assert-True ($LASTEXITCODE -eq 0) 'Não foi possível criar o projeto mínimo da fixture.'
        & dotnet sln Src\GenexusOpenApiBuilder.sln add Src\Fixture\Fixture.csproj | Out-Null
        Assert-True ($LASTEXITCODE -eq 0) 'Não foi possível adicionar o projeto à solution da fixture.'
        [System.IO.File]::Copy($checker, (Join-Path $PWD 'scripts\Invoke-PrePushMechanicalChecks.ps1'))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\ServiceSourceContract\Test-ApiPlanServiceSourceContract.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Service Source contract'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\MetadataIntegrity\Test-ApiPlanMetadataIntegrity.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Metadata Integrity'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WizardPreferences\Test-PrototypeWizardPreferences.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Wizard Preferences'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WizardNavigation\Test-PrototypeWizardBusinessComponentNavigationPolicy.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Wizard Navigation'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WritePreflight\Test-ApiPlanWritePreflightScope.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Write Preflight Scope'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\ListProcedure\Test-ApiPlanListProcedureReencounterPolicy.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture List Procedure Reencounter Policy'`n", [System.Text.UTF8Encoding]::new($false))
        & git add .gitignore README.md Src scripts Tests
        & git commit -m 'Fixture do checker' | Out-Null
        & git remote add origin (Join-Path $tempRoot 'remote.git')
        & git push -u origin main | Out-Null

        $json = & pwsh -NoProfile -File scripts/Invoke-PrePushMechanicalChecks.ps1 -AsJson
        $checkerExit = $LASTEXITCODE
        $result = $json | ConvertFrom-Json
        Assert-True ($checkerExit -eq 0) 'A fixture limpa deve concluir todos os checks mecânicos.'
        Assert-True ($result.gitContext.branch -eq 'main') 'O checker não reconheceu a branch main na fixture.'
        Assert-True ($result.remoteReadiness -eq 'unverified') 'Sem -Fetch, a referência remota deve permanecer unverified.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'git.branch' }).status -eq 'passed') 'A checagem de branch deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.serviceSourceContract' }).status -eq 'passed') 'O teste unitário do parser Service Source deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.metadataIntegrity' }).status -eq 'passed') 'O teste unitário da integridade B067 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.wizardPreferences' }).status -eq 'passed') 'O teste unitário das preferências do wizard deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.wizardNavigation' }).status -eq 'passed') 'O teste unitário da navegação do wizard deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.writePreflightScope' }).status -eq 'passed') 'O teste unitário do escopo de preflight deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.listProcedureReencounterPolicy' }).status -eq 'passed') 'O teste unitário do reencontro B070 deveria passar na fixture.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/ServiceSourceContract/Test-ApiPlanServiceSourceContract.ps1' }).Count -eq 1) 'O comando do teste Service Source deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/MetadataIntegrity/Test-ApiPlanMetadataIntegrity.ps1' }).Count -eq 1) 'O comando do teste Metadata Integrity deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WizardPreferences/Test-PrototypeWizardPreferences.ps1' }).Count -eq 1) 'O comando do teste Wizard Preferences deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WizardNavigation/Test-PrototypeWizardBusinessComponentNavigationPolicy.ps1' }).Count -eq 1) 'O comando do teste Wizard Navigation deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WritePreflight/Test-ApiPlanWritePreflightScope.ps1' }).Count -eq 1) 'O comando do teste Write Preflight Scope deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/ListProcedure/Test-ApiPlanListProcedureReencounterPolicy.ps1' }).Count -eq 1) 'O comando do teste List Procedure Reencounter Policy deve aparecer no JSON.'
        Assert-True (@($result.warnings).Count -eq 0) 'O checker não deve registrar "0 Aviso(s)" como warning.'

        $fetchJson = & pwsh -NoProfile -File scripts/Invoke-PrePushMechanicalChecks.ps1 -AsJson -Fetch
        $fetchExit = $LASTEXITCODE
        $fetchResult = $fetchJson | ConvertFrom-Json
        Assert-True ($fetchExit -eq 0) 'A fixture com fetch deve concluir todos os checks mecânicos.'
        Assert-True ($fetchResult.remoteFetchStatus -eq 'succeeded') 'O fetch local da fixture deveria concluir.'
        Assert-True ($fetchResult.remoteReadiness -eq 'confirmed') 'Com fetch bem-sucedido, a referência remota deve ser confirmed.'

        [System.IO.File]::AppendAllText((Join-Path $PWD 'README.md'), 'alteração preexistente' + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
        $dirtyJson = & pwsh -NoProfile -File scripts/Invoke-PrePushMechanicalChecks.ps1 -AsJson
        $dirtyExit = $LASTEXITCODE
        $dirtyResult = $dirtyJson | ConvertFrom-Json
        Assert-True ($dirtyExit -eq 3) 'A fixture com working tree suja deve exigir revisão humana.'
        Assert-True ($dirtyResult.pushReadiness -eq 'blocked') 'A working tree suja deve bloquear push.'
        Assert-True ('workingTreeDirty' -in @($dirtyResult.incompleteReasons)) 'O JSON deve explicitar workingTreeDirty.'
        Assert-True (@($dirtyResult.warnings | Where-Object { $_ -match 'working tree' }).Count -eq 1) 'A working tree suja deve gerar aviso explícito.'
    }
    finally {
        Pop-Location
    }
}
finally {
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
}

'OK: fixtures de classificação, fronteira operacional, Git limpo/sujo e fetch local validados.'
