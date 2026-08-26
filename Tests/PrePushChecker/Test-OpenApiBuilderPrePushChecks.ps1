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
Assert-True ($source -match 'Tests/OpenApiContract/Test-ApiPlanOpenApiContractMarks\.ps1') 'O checker deve executar o teste unitário das marcações do contrato OpenAPI.'
Assert-True ($source -match 'Tests/OpenApiContract/Test-OpenApiClientContractValidity\.ps1') 'O checker deve executar o teste unitário da validade do contrato de cliente OpenAPI.'
Assert-True ($source -match 'Tests/GenerationBaseline/Test-ApiPlanGenerationBaseline\.ps1') 'O checker deve executar o teste unitário da linha de base de geração (Fase 0).'
Assert-True ($source -match 'Tests/TransactionStructure/Test-TransactionStructureReader\.ps1') 'O checker deve executar o teste unitário da leitura hierárquica B095.'
Assert-True ($source -match 'Tests/SdtHierarchicalPlan/Test-ApiPlanSdtHierarchicalPlan\.ps1') 'O checker deve executar o teste unitário do plano de SDT hierárquico B096.'
Assert-True ($source -match 'Tests/ServiceSourceContract/Test-ApiPlanServiceSourceContract\.ps1') 'O checker deve executar o teste unitário do parser Service Source.'
Assert-True ($source -match 'Tests/MetadataIntegrity/Test-ApiPlanMetadataIntegrity\.ps1') 'O checker deve executar o teste unitário da integridade B067.'
Assert-True ($source -match 'Tests/ApiObjectOwnership/Test-ApiPlanApiObjectOwnership\.ps1') 'O checker deve executar o teste unitário da posse B087 do API Object.'
Assert-True ($source -match 'Tests/OwnershipDescriptions/Test-ApiPlanOwnedObjectDescription\.ps1') 'O checker deve executar o teste unitário das descrições canônicas e legadas de ownership.'
Assert-True ($source -match 'Tests/GeneratedApiRemoval/Test-ApiPlanGeneratedApiRemovalPlan\.ps1') 'O checker deve executar o teste unitário do plano de remoção B086.'
Assert-True ($source -match 'Tests/GeneratedApiRemoval/Test-ApiPlanGeneratedApiRemovalPreflight\.ps1') 'O checker deve executar o teste unitário do preflight B086 antes do primeiro Delete.'
Assert-True ($source -match 'Tests/TransactionSync/Test-ApiPlanTransactionSyncComparer\.ps1') 'O checker deve executar o teste unitário do diff B085 de sincronização.'
Assert-True ($source -match 'Tests/TransactionSync/Test-ApiPlanTransactionSyncFieldSelection\.ps1') 'O checker deve executar o teste unitário da seleção ordenada de campos B085.'
Assert-True ($source -match 'Tests/ApplicationFinalReport/Test-ApiPlanApplicationFinalReport\.ps1') 'O checker deve executar o teste unitário do relatório final B081.'
Assert-True ($source -match 'Tests/CollisionUx/Test-ApiPlanCollisionConflict\.ps1') 'O checker deve executar o teste unitário da UX residual B083 de conflitos.'
Assert-True ($source -match 'Tests/WizardPreferences/Test-PrototypeWizardPreferences\.ps1') 'O checker deve executar o teste unitário das preferências do wizard.'
Assert-True ($source -match 'Tests/WizardNavigation/Test-PrototypeWizardBusinessComponentNavigationPolicy\.ps1') 'O checker deve executar o teste unitário da navegação do wizard.'
Assert-True ($source -match 'Tests/WritePreflight/Test-ApiPlanWritePreflightScope\.ps1') 'O checker deve executar o teste unitário do escopo de preflight.'
Assert-True ($source -match 'Tests/BusinessComponentWriter/Test-ApiPlanBusinessComponentWriterVariableContract\.ps1') 'O checker deve executar o teste unitário do contrato de variáveis do writer Business Component.'
Assert-True ($source -match 'Tests/ListProcedure/Test-ApiPlanListProcedureReencounterPolicy\.ps1') 'O checker deve executar o teste unitário do reencontro B070.'
Assert-True ($source -match 'Tests/RequiredSemantics/Test-RequiredMemberSemanticsConsistency\.ps1') 'O checker deve executar o teste unitário da coerência semântica de Required.'
Assert-True ($source -match 'Tests/WizardContract/Test-PrototypeWizardAutonumberCompositeKey\.ps1') 'O checker deve executar o teste unitário de autonumeração e chave composta.'
Assert-True ($source -match 'Tests/WizardContract/Test-ApiPlanGenerationStateReaderGetAllIndex\.ps1') 'O checker deve executar o teste unitário do leitor de estado de geração.'
Assert-True ($source -match 'Tests/WizardContract/Test-PrototypeWizardCreateRequiredPrimaryKeyOptional\.ps1') 'O checker deve executar o teste unitário do contrato de wizard CreateRequired.'
Assert-True ($source -match 'Tests/WizardContract/Test-PrototypeWizardNoAcceptRuleReader\.ps1') 'O checker deve executar o teste unitário da leitura de regras NoAccept.'
Assert-True ($source -match 'Tests/WizardContract/Test-NoAcceptRequestEligibilityContract\.ps1') 'O checker deve executar o teste unitário da elegibilidade de requests NoAccept.'
Assert-True ($source -match 'Tests/WizardContract/Test-PrototypeWizardExistingApiFilters\.ps1') 'O checker deve executar o teste unitário dos filtros e restauração de API existente do wizard.'
Assert-True ($source -match 'Tests/ExtensionAssemblyInventory/Test-ExtensionAssemblyInventory\.ps1') 'O checker deve executar o teste unitário do inventário offline da assembly.'
Assert-True ($source -match 'Tests/Installation/Test-InstallExtensionBatPathHandling\.ps1') 'O checker deve executar o teste unitário do tratamento de caminhos dos BATs.'
Assert-True ($source -match 'Tests/Localization/Test-ExtensionLanguage\.ps1') 'O checker deve executar o teste unitário da resolução do idioma da extensão.'
Assert-True ($source -match 'Tests/Localization/Test-ExtensionOutputLocalization\.ps1') 'O checker deve executar o teste unitário da localização do Output da extensão.'
Assert-True ($source -match 'Tests/IssueForms/Test-GitHubIssueFormsYaml\.ps1') 'O checker deve executar o teste unitário dos YAML / Issue Forms.'
Assert-True ($source.Contains('\b(B00[0-6])\b')) 'currentFront deve reconhecer somente spikes B000-B006.'
Assert-True ($source -match 'lista vazia com próxima ação B007\+') 'O JSON notCovered deve declarar que manualRequired vazio fora de B000-B006 não substitui a revisão semântica.'

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
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\ApiObjectOwnership'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\OwnershipDescriptions'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\GeneratedApiRemoval'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\TransactionSync'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\ApplicationFinalReport'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\CollisionUx'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\TransactionFolder'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\WizardPreferences'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\WizardNavigation'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\WritePreflight'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\BusinessComponentWriter'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\ListProcedure'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\RequiredSemantics'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\OpenApiContract'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\GenerationBaseline'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\TransactionStructure'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\SdtHierarchicalPlan'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\WizardContract'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\ExtensionAssemblyInventory'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\Installation'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\Localization'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\IssueForms'))
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
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\ApiObjectOwnership\Test-ApiPlanApiObjectOwnership.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Api Object Ownership'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\OwnershipDescriptions\Test-ApiPlanOwnedObjectDescription.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Owned Object Description'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\GeneratedApiRemoval\Test-ApiPlanGeneratedApiRemovalPlan.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Generated Api Removal Plan'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\GeneratedApiRemoval\Test-ApiPlanGeneratedApiRemovalPreflight.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Generated Api Removal Preflight'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\TransactionSync\Test-ApiPlanTransactionSyncComparer.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Transaction Sync Comparer'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\TransactionSync\Test-ApiPlanTransactionSyncFieldSelection.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Transaction Sync Field Selection'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\ApplicationFinalReport\Test-ApiPlanApplicationFinalReport.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Application Final Report'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\CollisionUx\Test-ApiPlanCollisionConflict.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Collision Ux'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\TransactionFolder\Test-ApiPlanTransactionFolderReusePolicy.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Transaction Folder Reuse'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WizardPreferences\Test-PrototypeWizardPreferences.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Wizard Preferences'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WizardNavigation\Test-PrototypeWizardBusinessComponentNavigationPolicy.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Wizard Navigation'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WritePreflight\Test-ApiPlanWritePreflightScope.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Write Preflight Scope'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\BusinessComponentWriter\Test-ApiPlanBusinessComponentWriterVariableContract.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Business Component Writer Variable Contract'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\ListProcedure\Test-ApiPlanListProcedureReencounterPolicy.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture List Procedure Reencounter Policy'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\RequiredSemantics\Test-RequiredMemberSemanticsConsistency.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Required Member Semantics Consistency'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\OpenApiContract\Test-ApiPlanOpenApiContractMarks.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture OpenApi Contract Marks'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\OpenApiContract\Test-OpenApiClientContractValidity.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture OpenApi Client Contract Validity'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\GenerationBaseline\Test-ApiPlanGenerationBaseline.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Generation Baseline'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\TransactionStructure\Test-TransactionStructureReader.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Transaction Structure Reader'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\SdtHierarchicalPlan\Test-ApiPlanSdtHierarchicalPlan.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Sdt Hierarchical Plan'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WizardContract\Test-PrototypeWizardAutonumberCompositeKey.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Autonumber Composite Key'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WizardContract\Test-ApiPlanGenerationStateReaderGetAllIndex.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Generation State Reader GetAll Index'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WizardContract\Test-PrototypeWizardCreateRequiredPrimaryKeyOptional.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Create Required Primary Key Optional'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WizardContract\Test-PrototypeWizardNoAcceptRuleReader.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture NoAccept Rule Reader'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WizardContract\Test-NoAcceptRequestEligibilityContract.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture NoAccept Request Eligibility'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WizardContract\Test-PrototypeWizardExistingApiFilters.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Prototype Wizard Existing Api Filters'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WizardContract\Test-PrototypeWizardServiceSourceParsing.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Prototype Wizard Service Source Parsing'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\ExtensionAssemblyInventory\Test-ExtensionAssemblyInventory.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Extension Assembly Inventory'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\Installation\Test-InstallExtensionBatPathHandling.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Installation BAT Path Handling'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\Localization\Test-ExtensionLanguage.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Extension Language'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\Localization\Test-ExtensionOutputLocalization.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Extension Output Localization'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\IssueForms\Test-GitHubIssueFormsYaml.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Issue Forms Yaml'`n", [System.Text.UTF8Encoding]::new($false))
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
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.apiObjectOwnership' }).status -eq 'passed') 'O teste unitário da posse B087 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.ownedObjectDescription' }).status -eq 'passed') 'O teste unitário das descrições de ownership deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.generatedApiRemovalPlan' }).status -eq 'passed') 'O teste unitário do plano de remoção B086 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.generatedApiRemovalPreflight' }).status -eq 'passed') 'O teste unitário do preflight B086 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.transactionSyncComparer' }).status -eq 'passed') 'O teste unitário do diff B085 de sincronização deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.transactionSyncFieldSelection' }).status -eq 'passed') 'O teste unitário da seleção ordenada de campos B085 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.applicationFinalReport' }).status -eq 'passed') 'O teste unitário do relatório final B081 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.collisionUx' }).status -eq 'passed') 'O teste unitário da UX residual B083 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.transactionFolderReuse' }).status -eq 'passed') 'O teste unitário do reuso de Folder deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.wizardPreferences' }).status -eq 'passed') 'O teste unitário das preferências do wizard deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.wizardNavigation' }).status -eq 'passed') 'O teste unitário da navegação do wizard deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.writePreflightScope' }).status -eq 'passed') 'O teste unitário do escopo de preflight deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.businessComponentWriterVariableContract' }).status -eq 'passed') 'O teste unitário do contrato de variáveis do writer Business Component deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.listProcedureReencounterPolicy' }).status -eq 'passed') 'O teste unitário do reencontro B070 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.requiredMemberSemantics' }).status -eq 'passed') 'O teste unitário da coerência semântica de Required deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.openApiContractMarks' }).status -eq 'passed') 'O teste unitário das marcações do contrato OpenAPI deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.openApiClientContractValidity' }).status -eq 'passed') 'O teste unitário da validade do contrato de cliente OpenAPI deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.generationBaseline' }).status -eq 'passed') 'O teste unitário da linha de base de geração (Fase 0) deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.transactionStructure' }).status -eq 'passed') 'O teste unitário da leitura hierárquica B095 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.sdtHierarchicalPlan' }).status -eq 'passed') 'O teste unitário do plano de SDT hierárquico B096 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.wizardContractAutonumberCompositeKey' }).status -eq 'passed') 'O teste unitário de autonumeração e chave composta deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.wizardContractGenerationStateReader' }).status -eq 'passed') 'O teste unitário do leitor de estado de geração deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.wizardContractCreateRequired' }).status -eq 'passed') 'O teste unitário do contrato de wizard CreateRequired deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.wizardContractNoAcceptRuleReader' }).status -eq 'passed') 'O teste unitário da leitura de regras NoAccept deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.wizardContractNoAcceptRequestEligibility' }).status -eq 'passed') 'O teste unitário da elegibilidade de requests NoAccept deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.wizardContractExistingApiFilters' }).status -eq 'passed') 'O teste unitário dos filtros e restauração de API existente do wizard deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.wizardContractServiceSourceParsing' }).status -eq 'passed') 'O teste unitário do parser de Service Source do wizard deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.extensionAssemblyInventory' }).status -eq 'passed') 'O teste unitário do inventário offline da assembly deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.installationBatPathHandling' }).status -eq 'passed') 'O teste unitário do tratamento de caminhos dos BATs deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.extensionLanguage' }).status -eq 'passed') 'O teste unitário da resolução do idioma da extensão deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.extensionOutputLocalization' }).status -eq 'passed') 'O teste unitário da localização do Output da extensão deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.issueForms' }).status -eq 'passed') 'O teste unitário dos YAML / Issue Forms deveria passar na fixture.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/ServiceSourceContract/Test-ApiPlanServiceSourceContract.ps1' }).Count -eq 1) 'O comando do teste Service Source deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/MetadataIntegrity/Test-ApiPlanMetadataIntegrity.ps1' }).Count -eq 1) 'O comando do teste Metadata Integrity deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/ApiObjectOwnership/Test-ApiPlanApiObjectOwnership.ps1' }).Count -eq 1) 'O comando do teste Api Object Ownership deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/OwnershipDescriptions/Test-ApiPlanOwnedObjectDescription.ps1' }).Count -eq 1) 'O comando do teste Owned Object Description deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/GeneratedApiRemoval/Test-ApiPlanGeneratedApiRemovalPlan.ps1' }).Count -eq 1) 'O comando do teste Generated Api Removal Plan deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/GeneratedApiRemoval/Test-ApiPlanGeneratedApiRemovalPreflight.ps1' }).Count -eq 1) 'O comando do teste Generated Api Removal Preflight deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/TransactionSync/Test-ApiPlanTransactionSyncComparer.ps1' }).Count -eq 1) 'O comando do teste Transaction Sync Comparer deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/TransactionSync/Test-ApiPlanTransactionSyncFieldSelection.ps1' }).Count -eq 1) 'O comando do teste Transaction Sync Field Selection deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/ApplicationFinalReport/Test-ApiPlanApplicationFinalReport.ps1' }).Count -eq 1) 'O comando do teste Application Final Report deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/CollisionUx/Test-ApiPlanCollisionConflict.ps1' }).Count -eq 1) 'O comando do teste Collision Ux deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/TransactionFolder/Test-ApiPlanTransactionFolderReusePolicy.ps1' }).Count -eq 1) 'O comando do teste Transaction Folder Reuse deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WizardPreferences/Test-PrototypeWizardPreferences.ps1' }).Count -eq 1) 'O comando do teste Wizard Preferences deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WizardNavigation/Test-PrototypeWizardBusinessComponentNavigationPolicy.ps1' }).Count -eq 1) 'O comando do teste Wizard Navigation deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WritePreflight/Test-ApiPlanWritePreflightScope.ps1' }).Count -eq 1) 'O comando do teste Write Preflight Scope deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/BusinessComponentWriter/Test-ApiPlanBusinessComponentWriterVariableContract.ps1' }).Count -eq 1) 'O comando do teste Business Component Writer Variable Contract deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/OpenApiContract/Test-ApiPlanOpenApiContractMarks.ps1' }).Count -eq 1) 'O comando do teste OpenApi Contract Marks deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/OpenApiContract/Test-OpenApiClientContractValidity.ps1' }).Count -eq 1) 'O comando do teste OpenApi Client Contract Validity deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/GenerationBaseline/Test-ApiPlanGenerationBaseline.ps1' }).Count -eq 1) 'O comando do teste Generation Baseline deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/TransactionStructure/Test-TransactionStructureReader.ps1' }).Count -eq 1) 'O comando do teste Transaction Structure deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/SdtHierarchicalPlan/Test-ApiPlanSdtHierarchicalPlan.ps1' }).Count -eq 1) 'O comando do teste Sdt Hierarchical Plan deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WizardContract/Test-PrototypeWizardAutonumberCompositeKey.ps1' }).Count -eq 1) 'O comando do teste Autonumber Composite Key deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WizardContract/Test-ApiPlanGenerationStateReaderGetAllIndex.ps1' }).Count -eq 1) 'O comando do teste Generation State Reader GetAll Index deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WizardContract/Test-PrototypeWizardCreateRequiredPrimaryKeyOptional.ps1' }).Count -eq 1) 'O comando do teste Create Required Primary Key Optional deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WizardContract/Test-PrototypeWizardNoAcceptRuleReader.ps1' }).Count -eq 1) 'O comando do teste NoAccept Rule Reader deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WizardContract/Test-NoAcceptRequestEligibilityContract.ps1' }).Count -eq 1) 'O comando do teste NoAccept Request Eligibility deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WizardContract/Test-PrototypeWizardExistingApiFilters.ps1' }).Count -eq 1) 'O comando do teste Prototype Wizard Existing Api Filters deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WizardContract/Test-PrototypeWizardServiceSourceParsing.ps1' }).Count -eq 1) 'O comando do teste Prototype Wizard Service Source Parsing deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/ExtensionAssemblyInventory/Test-ExtensionAssemblyInventory.ps1' }).Count -eq 1) 'O comando do teste Extension Assembly Inventory deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/Installation/Test-InstallExtensionBatPathHandling.ps1' }).Count -eq 1) 'O comando do teste Installation BAT Path Handling deve aparecer no JSON.'
        Assert-True (@($result.warnings).Count -eq 0) 'O checker não deve registrar "0 Aviso(s)" como warning.'

        $fetchJson = & pwsh -NoProfile -File scripts/Invoke-PrePushMechanicalChecks.ps1 -AsJson -Fetch
        $fetchExit = $LASTEXITCODE
        $fetchResult = $fetchJson | ConvertFrom-Json
        Assert-True ($fetchExit -eq 0) 'A fixture com fetch deve concluir todos os checks mecânicos.'
        Assert-True ($fetchResult.remoteFetchStatus -eq 'succeeded') 'O fetch local da fixture deveria concluir.'
        Assert-True ($fetchResult.remoteReadiness -eq 'confirmed') 'Com fetch bem-sucedido, a referência remota deve ser confirmed.'

        [System.IO.File]::AppendAllText((Join-Path $PWD 'README.md'), "referência histórica B000`n", [System.Text.UTF8Encoding]::new($false))
        & git add README.md
        & git commit -m 'Fixture com referência histórica' | Out-Null
        $historicalJson = & pwsh -NoProfile -File scripts/Invoke-PrePushMechanicalChecks.ps1 -AsJson
        $historicalExit = $LASTEXITCODE
        $historicalResult = $historicalJson | ConvertFrom-Json
        Assert-True ($historicalExit -eq 0) 'Uma referência histórica B000 não deve exigir revisão manual sem frente vigente.'
        Assert-True (@($historicalResult.manualRequired).Count -eq 0) 'Referências históricas não devem alimentar manualRequired.'
        Assert-True (@($historicalResult.notCovered | Where-Object { $_ -match 'B000-B006' }).Count -eq 1) 'notCovered deve explicar o recorte B000-B006 de currentFront/manualRequired.'

        [System.IO.Directory]::CreateDirectory((Join-Path $PWD 'Docs')) | Out-Null
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Docs\STATUS_ATUAL_E_PROXIMO_PASSO.md'), "## Próxima ação única`n`nExecutar B006.`n`n## Histórico`n`nB000 concluído.`n", [System.Text.UTF8Encoding]::new($false))
        & git add Docs\STATUS_ATUAL_E_PROXIMO_PASSO.md
        & git commit -m 'Fixture com frente vigente' | Out-Null
        $activeJson = & pwsh -NoProfile -File scripts/Invoke-PrePushMechanicalChecks.ps1 -AsJson
        $activeExit = $LASTEXITCODE
        $activeResult = $activeJson | ConvertFrom-Json
        Assert-True ($activeExit -eq 3) 'Uma frente B006 explicitamente vigente deve exigir revisão manual.'
        Assert-True ('manualRequired' -in @($activeResult.incompleteReasons)) 'A frente vigente deve bloquear por manualRequired.'
        Assert-True (@($activeResult.manualRequired | Where-Object { $_.path -eq 'Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md' }).Count -eq 1) 'O checkpoint da frente vigente deve aparecer em manualRequired.'

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
