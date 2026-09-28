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
Assert-True ($source -match 'Tests/BusinessComponentHierarchical/Test-ApiPlanBusinessComponentHierarchical\.ps1') 'O checker deve executar o teste unitário do Source BC hierárquico B097.'
Assert-True ($source -match 'Tests/ListHierarchical/Test-ApiPlanListHierarchical\.ps1') 'O checker deve executar o teste unitário do List hierárquico B098.'
Assert-True ($source -match 'Tests/WizardHierarchical/Test-ApiPlanHierarchicalWizardSelection\.ps1') 'O checker deve executar o teste unitário da seleção hierárquica do Wizard B099a.'
Assert-True ($source -match 'Tests/WizardLifecycle/Test-ApiPlanWizardHierarchicalLifecycle\.ps1') 'O checker deve executar o teste unitário do ciclo de vida hierárquico do Wizard (Fase 7).'
Assert-True ($source -match 'Tests/MetadataHierarchical/Test-ApiPlanMetadataLevels\.ps1') 'O checker deve executar o teste unitário da metadata hierárquica B099b.'
Assert-True ($source -match 'Tests/ServiceSourceContract/Test-ApiPlanServiceSourceContract\.ps1') 'O checker deve executar o teste unitário do parser Service Source.'
Assert-True ($source -match 'Tests/PersistenceProbe/Test-ApiPlanPersistenceCore\.ps1') 'O checker deve executar o teste unitário do seam de persistência B111/F2.'
Assert-True ($source -match 'Tests/PersistenceProbe/Test-ApiPlanSaveStepExecutor\.ps1') 'O checker deve executar o teste unitário do executor de persistência B111/F2.'
Assert-True ($source -match 'Tests/PersistenceProbe/Test-ApiPlanPersistenceSeamCoverage\.ps1') 'O checker deve executar a sentinela de cobertura do seam B111/F2.'
Assert-True ($source -match 'Tests/OperationJournal/Test-ApiPlanOperationJournalSchema\.ps1') 'O checker deve executar o teste do schema V1 do diário B111/F3.'
Assert-True ($source -match 'Tests/OperationJournal/Test-ApiPlanOperationJournalCheckpoints\.ps1') 'O checker deve executar o teste da matriz de checkpoints do diário B111/F3.'
Assert-True ($source -match 'Tests/OperationJournal/Test-ApiPlanOperationJournalReceipts\.ps1') 'O checker deve executar o teste do transporte de recibos do diário B111/F3.'
Assert-True ($source -match 'Tests/OperationJournal/Test-ApiPlanOperationJournalGate\.ps1') 'O checker deve executar o teste da precedência de diagnóstico do gate estendido do diário B111/F3.'
Assert-True ($source -match 'Tests/OperationJournal/Test-ApiPlanJournalFrontierSentinel\.ps1') 'O checker deve executar a sentinela da fronteira do API Object e da identidade composta B111/F3.'
Assert-True ($source -match 'Tests/MetadataIntegrity/Test-ApiPlanMetadataIntegrity\.ps1') 'O checker deve executar o teste unitário da integridade B067.'
Assert-True ($source -match 'Tests/ApiObjectOwnership/Test-ApiPlanApiObjectOwnership\.ps1') 'O checker deve executar o teste unitário da posse B087 do API Object.'
Assert-True ($source -match 'Tests/OwnershipDescriptions/Test-ApiPlanOwnedObjectDescription\.ps1') 'O checker deve executar o teste unitário das descrições canônicas e legadas de ownership.'
Assert-True ($source -match 'Tests/GeneratedApiRemoval/Test-ApiPlanGeneratedApiRemovalPlan\.ps1') 'O checker deve executar o teste unitário do plano de remoção B086.'
Assert-True ($source -match 'Tests/GeneratedApiRemoval/Test-ApiPlanGeneratedApiRemovalPreflight\.ps1') 'O checker deve executar o teste unitário do preflight B086 antes do primeiro Delete.'
Assert-True ($source -match 'Tests/GeneratedApiRemoval/Test-ApiPlanB082Etapa2Safety\.ps1') 'O checker deve executar o teste textual da B082 Etapa 2.'
Assert-True ($source -match 'Tests/TransactionSync/Test-ApiPlanTransactionSyncComparer\.ps1') 'O checker deve executar o teste unitário do diff B085 de sincronização.'
Assert-True ($source -match 'Tests/TransactionSync/Test-ApiPlanTransactionSyncFieldSelection\.ps1') 'O checker deve executar o teste unitário da seleção ordenada de campos B085.'
Assert-True ($source -match 'Tests/TransactionSync/Test-ApiPlanTransactionSyncSdtMemberSequenceMatcher\.ps1') 'O checker deve executar o teste unitário do reencontro de SDT com inclusões selecionadas.'
Assert-True ($source -match 'Tests/ApplicationFinalReport/Test-ApiPlanApplicationFinalReport\.ps1') 'O checker deve executar o teste unitário do relatório final B081.'
Assert-True ($source -match 'Tests/CollisionUx/Test-ApiPlanCollisionConflict\.ps1') 'O checker deve executar o teste unitário da UX residual B083 de conflitos.'
Assert-True ($source -match 'Tests/WizardPreferences/Test-PrototypeWizardPreferences\.ps1') 'O checker deve executar o teste unitário das preferências do wizard.'
Assert-True ($source -match 'Tests/WizardNavigation/Test-PrototypeWizardBusinessComponentNavigationPolicy\.ps1') 'O checker deve executar o teste unitário da navegação do wizard.'
Assert-True ($source -match 'Tests/WritePreflight/Test-ApiPlanWritePreflightScope\.ps1') 'O checker deve executar o teste unitário do escopo de preflight.'
Assert-True ($source -match 'Tests/WritePreflight/Test-ApiPlanWritePreflightHierarchicalStructure\.ps1') 'O checker deve executar o teste unitário do preflight hierárquico de subnível sem nome.'
Assert-True ($source -match 'Tests/WritePreflight/Test-ApiPlanWritePreflightBusinessComponentGuard\.ps1') 'O checker deve executar o teste unitário da guarda B055 de Business Component.'
Assert-True ($source -match 'Tests/BusinessComponentWriter/Test-ApiPlanBusinessComponentWriterVariableContract\.ps1') 'O checker deve executar o teste unitário do contrato de variáveis do writer Business Component.'
Assert-True ($source -match 'Tests/ListProcedure/Test-ApiPlanListProcedureReencounterPolicy\.ps1') 'O checker deve executar o teste unitário do reencontro B070.'
Assert-True ($source -match 'Tests/RequiredSemantics/Test-RequiredMemberSemanticsConsistency\.ps1') 'O checker deve executar o teste unitário da coerência semântica de Required.'
Assert-True ($source -match 'Tests/WizardContract/Test-PrototypeWizardAutonumberCompositeKey\.ps1') 'O checker deve executar o teste unitário de autonumeração e chave composta.'
Assert-True ($source -match 'Tests/WizardContract/Test-ApiPlanGenerationStateReaderGetAllIndex\.ps1') 'O checker deve executar o teste unitário do leitor de estado de geração.'
Assert-True ($source -match 'Tests/KbIndexReuse/Test-ApiPlanKbIndexReuse\.ps1') 'O checker deve executar o teste unitário da reutilização do índice da KB.'
Assert-True ($source -match 'Tests/WizardContract/Test-PrototypeWizardCreateRequiredPrimaryKeyOptional\.ps1') 'O checker deve executar o teste unitário do contrato de wizard CreateRequired.'
Assert-True ($source -match 'Tests/WizardContract/Test-PrototypeWizardNoAcceptRuleReader\.ps1') 'O checker deve executar o teste unitário da leitura de regras NoAccept.'
Assert-True ($source -match 'Tests/WizardContract/Test-NoAcceptRequestEligibilityContract\.ps1') 'O checker deve executar o teste unitário da elegibilidade de requests NoAccept.'
Assert-True ($source -match 'Tests/WizardContract/Test-PrototypeWizardExistingApiFilters\.ps1') 'O checker deve executar o teste unitário dos filtros e restauração de API existente do wizard.'
Assert-True ($source -match 'Tests/ExtensionAssemblyInventory/Test-ExtensionAssemblyInventory\.ps1') 'O checker deve executar o teste unitário do inventário offline da assembly.'
Assert-True ($source -match 'Tests/Installation/Test-InstallExtensionBatPathHandling\.ps1') 'O checker deve executar o teste unitário do tratamento de caminhos dos BATs.'
Assert-True ($source -match 'Tests/Localization/Test-ExtensionLanguage\.ps1') 'O checker deve executar o teste unitário da resolução do idioma da extensão.'
Assert-True ($source -match 'Tests/Localization/Test-ExtensionOutputLocalization\.ps1') 'O checker deve executar o teste unitário da localização do Output da extensão.'
Assert-True ($source -match 'Tests/Localization/Test-ExtensionOutputLocalizationSelfConsistency\.ps1') 'O checker deve exercer o catálogo de saída contra si mesmo.'
Assert-True ($source -match 'Tests/IssueForms/Test-GitHubIssueFormsYaml\.ps1') 'O checker deve executar o teste unitário dos YAML / Issue Forms.'
Assert-True ($source -match 'Tests/TextPatch/Test-ApplyTextPatch\.ps1') 'O checker deve executar o teste unitário da edição textual ancorada B122.'
Assert-True ($source -match 'Tests/PrePushChecker/Test-B128ReferenceTokenizer\.ps1') 'O checker deve executar o teste do tokenizer compartilhado B128.'
Assert-True ($source.Contains('\b(B00[0-6])\b')) 'currentFront deve reconhecer somente spikes B000-B006.'
Assert-True ($source -match 'lista vazia com próxima ação B007\+') 'O JSON notCovered deve declarar que manualRequired vazio fora de B000-B006 não substitui a revisão semântica.'
Assert-True ($source -match 'evidence-doc-required:checkpoint') 'O checker deve emitir o aviso evidence-doc-required:checkpoint no canal warnings.'
Assert-True ($source -match 'evidence-doc-required:validated') 'O checker deve emitir o aviso evidence-doc-required:validated no canal warnings.'
Assert-True ($source -match 'evidence-doc-required:fixed') 'O checker deve emitir o aviso evidence-doc-required:fixed no canal warnings.'

# B129 — asserções estáticas do checker e do módulo de funções puras.
$b129Module = Join-Path $repositoryRoot 'scripts\B129-SatelliteChecks.ps1'
Assert-True (Test-Path -LiteralPath $b129Module -PathType Leaf) "Módulo B129 não encontrado: $b129Module"
$b129ModuleSource = Get-Content -LiteralPath $b129Module -Raw
Assert-True ($source -match 'GenexusOpenApiBuilder\.Gx18u13\.sln') 'O checker deve compilar a solution satélite U13.'
Assert-True ($source -match "Join-Path \`$PSScriptRoot 'B129-SatelliteChecks\.ps1'") 'O checker deve carregar o módulo B129 por dot-source.'
foreach ($b129Source in @(@{ Name = 'checker'; Text = $source }, @{ Name = 'módulo B129'; Text = $b129ModuleSource })) {
    Assert-True ($b129Source.Text -notmatch '(?i)LoadFile|LoadFrom|Add-Type\s+-Path|\[(System\.)?Reflection\.Assembly\]') "O $($b129Source.Name) não pode carregar assembly."
    Assert-True ($b129Source.Text -notmatch '(?im)^\s*(?:&|\.)\s+.*\bTools[\\/]') "O $($b129Source.Name) não pode invocar scripts de Tools."
    Assert-True ($b129Source.Text -notmatch '(?i)Invoke-Expression|ScriptBlock::Create|&\s*\(') "O $($b129Source.Name) não pode usar invocação dinâmica."
    Assert-True ($b129Source.Text -notmatch '(?i)(?:C:|%ProgramFiles%)\\[^\r\n]*Program Files|C:\\GxModels') "O $($b129Source.Name) não pode acessar Program Files nem uma KB local."
    Assert-True ($b129Source.Text -notmatch '(?i)Start-Process\s+.*(?:genexus|dll)') "O $($b129Source.Name) não pode iniciar IDE ou operações de DLL."
}
. $b129Module

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
    # --- B129: casos de função, sem execução do checker ---
    $utf8 = [System.Text.UTF8Encoding]::new($false)

    # Paridade (seção 4.1).
    Assert-True ((Compare-B129CompileSets -CanonicalPaths @('Extension/A.cs', 'Domain/D.cs') -SatellitePaths @('domain/d.cs', 'extension/a.cs')).Passed) 'Paridade: conjuntos iguais (sem distinção de caixa) devem passar.'
    $b129Diff = Compare-B129CompileSets -CanonicalPaths @('Extension/A.cs', 'Extension/OnlyC.cs') -SatellitePaths @('Extension/A.cs', 'Extension/OnlyS.cs')
    Assert-True (-not $b129Diff.Passed -and @($b129Diff.OnlyCanonical) -contains 'Extension/OnlyC.cs' -and @($b129Diff.OnlySatellite) -contains 'Extension/OnlyS.cs') 'Paridade: onlyCanonical/onlySatellite.'
    $b129Rule1 = Compare-B129CompileSets -CanonicalPaths @('Extension/A.cs', 'Extension/Line.Gx18u13/X.cs') -SatellitePaths @('Extension/A.cs', 'Extension/Line.Gx18u13/X.cs')
    Assert-True (-not $b129Rule1.Passed -and @($b129Rule1.ForbiddenInCanonical).Count -eq 1 -and @($b129Rule1.OnlySatellite).Count -eq 0) 'Paridade: regra 1 (Line.Gx18u13 no canônico).'
    $b129Rule2 = Compare-B129CompileSets -CanonicalPaths @('Extension/A.cs') -SatellitePaths @('Extension/A.cs', 'Extension/Line.Gx18u14plus/Y.cs')
    Assert-True (-not $b129Rule2.Passed -and @($b129Rule2.ForbiddenInSatellite).Count -eq 1) 'Paridade: regra 2 (Line.Gx18u14plus no satélite).'
    $b129Rule4 = Compare-B129CompileSets -CanonicalPaths @('Extension/A.cs') -SatellitePaths @('Extension/A.cs', 'Extension/a.cs')
    Assert-True (-not $b129Rule4.Passed -and @($b129Rule4.Duplicates).Count -eq 1 -and @($b129Rule4.Duplicates)[0].count -eq 2 -and @($b129Rule4.Duplicates)[0].project -eq 'satellite') 'Paridade: regra 4 (duplicata).'
    Assert-True (-not (Get-B129CompileItems -Text 'MSBUILD : error MSB1009: Project file does not exist.' -SrcRoot $tempRoot).Readable) 'Paridade: saída não JSON é ilegível.'
    Assert-True (-not (Get-B129CompileItems -Text '{"Items":{"Compile":[{"Identity":"A.cs"}]}}' -SrcRoot $tempRoot).Readable) 'Paridade: item sem FullPath é ilegível.'
    foreach ($b129EmptyJson in @('{"Items":{"Compile":[]}}', '{"Items":{}}', '{}')) {
        $b129Empty = Get-B129CompileItems -Text $b129EmptyJson -SrcRoot $tempRoot
        Assert-True ($b129Empty.Readable -and @($b129Empty.Paths).Count -eq 0) "Paridade: Items.Compile vazio ou ausente vira lista vazia ($b129EmptyJson)."
    }
    $b129ItemJson = '{"Items":{"Compile":[{"FullPath":' + ((Join-Path $tempRoot 'Src\Extension\Sub\B.cs') | ConvertTo-Json) + '}]}}'
    Assert-True (@((Get-B129CompileItems -Text $b129ItemJson -SrcRoot (Join-Path $tempRoot 'Src')).Paths)[0] -eq 'Extension/Sub/B.cs') 'Paridade: FullPath normalizado relativo a Src/ com separador /.'
    Assert-True ((Get-B129TargetFramework '{"Properties":{"TargetFramework":"","TargetFrameworks":"net471;net48"}}').Target -eq 'net471') 'Paridade: alvo = primeiro de TargetFrameworks.'
    Assert-True ((Get-B129TargetFramework '{"Properties":{"TargetFramework":"net471","TargetFrameworks":""}}').Target -eq 'net471') 'Paridade: alvo = TargetFramework.'
    Assert-True (-not (Get-B129TargetFramework 'texto').Readable) 'Paridade: -getProperty ilegível.'

    # Contrato do .props e estado das refs (seção 4.2).
    $b129CaseProject = Join-Path $tempRoot 'b129-contract\Src\Extension'
    $b129CaseLib = Join-Path $tempRoot 'b129-contract\Src\Lib\Gx18u13'
    $b129CaseProps = Join-Path $b129CaseProject 'Lib.Gx18u13.References.props'
    [void][System.IO.Directory]::CreateDirectory($b129CaseProject)
    function Test-B129ContractCase {
        param([string]$Body, [switch]$NoNamespace, [switch]$Missing)
        if (Test-Path -LiteralPath $b129CaseProps) { Remove-Item -LiteralPath $b129CaseProps -Force }
        if (-not $Missing) {
            $namespace = if ($NoNamespace) { '' } else { ' xmlns="http://schemas.microsoft.com/developer/msbuild/2003"' }
            [System.IO.File]::WriteAllText($b129CaseProps, "<Project$namespace><ItemGroup>$Body</ItemGroup></Project>", $utf8)
        }
        return Test-B129ReferencesContract -PropsPath $b129CaseProps -ProjectDirectory $b129CaseProject -LibDirectory $b129CaseLib
    }
    $b129GoodRef = '<Reference Include="A"><HintPath>..\Lib\Gx18u13\A.dll</HintPath><Private>false</Private></Reference>'
    $b129Framework = '<Reference Include="System.Drawing" /><Reference Include="System.Windows.Forms" />'
    Assert-True (Test-B129ContractCase ($b129GoodRef + $b129Framework)).Valid 'Contrato: namespace MSBuild e referências de framework sem HintPath.'
    Assert-True (Test-B129ContractCase $b129GoodRef -NoNamespace).Valid 'Contrato: sem namespace.'
    $b129MissingProps = Test-B129ContractCase -Missing
    Assert-True (-not $b129MissingProps.Valid -and @($b129MissingProps.Violations)[0] -match 'não existe') 'Contrato: .props ausente.'
    Assert-True (-not (Test-B129ContractCase $b129Framework).Valid) 'Contrato: nenhum HintPath.'
    Assert-True (-not (Test-B129ContractCase ($b129GoodRef + '<Reference Include="Artech.Nova" />')).Valid) 'Contrato: Reference sem HintPath fora da lista fechada.'
    Assert-True (-not (Test-B129ContractCase '<Reference Include="A"><HintPath>..\Lib\Gx18u13\A.dll</HintPath></Reference>').Valid) 'Contrato: HintPath sem Private=false.'
    Assert-True (Test-B129ContractCase '<Reference Include="A" Private="False"><HintPath>..\Lib\Gx18u13\A.dll</HintPath></Reference>').Valid 'Contrato: Private=false como atributo, outra caixa.'
    Assert-True (Test-B129ContractCase '<Reference Include="A"><HintPath>..\Lib\Gx18u13\A.dll</HintPath><Private>FALSE</Private></Reference>').Valid 'Contrato: Private=false como elemento, outra caixa.'
    Assert-True (-not (Test-B129ContractCase ('<Reference Include="A"><HintPath>' + (Join-Path $b129CaseLib 'A.dll') + '</HintPath><Private>false</Private></Reference>')).Valid) 'Contrato: HintPath absoluto.'
    Assert-True (-not (Test-B129ContractCase '<Reference Include="A"><HintPath>..\Lib\Gx18u13\*.dll</HintPath><Private>false</Private></Reference>').Valid) 'Contrato: HintPath com curinga.'
    Assert-True (-not (Test-B129ContractCase '<Reference Include="A"><HintPath>..\Lib\Outra\A.dll</HintPath><Private>false</Private></Reference>').Valid) 'Contrato: HintPath fora de Src/Lib/Gx18u13.'
    Assert-True (-not (Test-B129ContractCase '<Reference Include="A"><HintPath>..\Lib\Gx18u13\..\..\A.dll</HintPath><Private>false</Private></Reference>').Valid) 'Contrato: HintPath que escapa por ..'
    Assert-True (Test-B129ContractCase '<Reference Include="A"><HintPath>..\lib\GX18U13\A.dll</HintPath><Private>false</Private></Reference>').Valid 'Contrato: diferença de caixa no HintPath não o tira de Src/Lib/Gx18u13.'
    [System.IO.File]::WriteAllText($b129CaseProps, '<Project><ItemGroup><Reference', $utf8)
    $b129Malformed = Test-B129ReferencesContract -PropsPath $b129CaseProps -ProjectDirectory $b129CaseProject -LibDirectory $b129CaseLib
    Assert-True (-not $b129Malformed.Valid) 'Contrato: XML malformado reprova sem exceção.'
    Assert-True ((Get-B129SatelliteRefsState -Contract $b129Malformed -LibDirectory $b129CaseLib).State -eq 'absent') 'Refs: pasta ausente com contrato violado = absent.'
    $b129Valid = Test-B129ContractCase $b129GoodRef
    Assert-True ((Get-B129SatelliteRefsState -Contract $b129Valid -LibDirectory $b129CaseLib).State -eq 'absent') 'Refs: pasta ausente = absent.'
    [void][System.IO.Directory]::CreateDirectory($b129CaseLib)
    Assert-True ((Get-B129SatelliteRefsState -Contract $b129Malformed -LibDirectory $b129CaseLib).State -eq 'unverifiable') 'Refs: pasta presente com XML malformado = unverifiable.'
    $b129Incomplete = Get-B129SatelliteRefsState -Contract $b129Valid -LibDirectory $b129CaseLib
    Assert-True ($b129Incomplete.State -eq 'incomplete' -and @($b129Incomplete.Missing).Count -eq 1) 'Refs: arquivo pinado faltando = incomplete.'
    [System.IO.File]::WriteAllBytes((Join-Path $b129CaseLib 'A.dll'), [byte[]]@(0))
    Assert-True ((Get-B129SatelliteRefsState -Contract $b129Valid -LibDirectory $b129CaseLib).State -eq 'complete') 'Refs: todos presentes = complete.'

    # Avisos do satélite (seção 4.2).
    $b129Proj = 'C:\f\Sat.csproj(9,5)'
    $b129Orphan = "${b129Proj}: warning MSB3277: Linha MSB3277 orfa sem cabecalho [x]"
    $b129MsHeader = "${b129Proj}: warning MSB3277: Found conflicts between different versions of `"mscorlib`" that could not be resolved. [x]"
    $b129MsCont = "${b129Proj}: warning MSB3277: continuacao do mscorlib [x]"
    $b129PtHeader = "${b129Proj}: warning MSB3277: foram encontrados conflitos entre diferentes versões do `"FixtureAssembly`" que não puderam ser resolvidas. [x]"
    $b129PtCont = "${b129Proj}: warning MSB3277: continuacao do outro assembly [x]"
    $b129Block = @($b129Orphan, $b129MsHeader, $b129MsCont, $b129MsCont, $b129PtHeader, $b129PtCont)
    $b129Present = Get-B129SatelliteWarnings -Output ((@('Restore complete') + $b129Block + @('Sat -> C:\f\Sat.dll', 'Build succeeded.') + $b129Block + @('    6 Warning(s)', '    0 Error(s)')) -join "`n")
    Assert-True ($b129Present.KnownWarnings.groups -eq 2 -and $b129Present.KnownWarnings.continuationLines -eq 4) 'Avisos: dois grupos mscorlib conhecidos, repetidos.'
    Assert-True (@($b129Present.Warnings).Count -eq 2 -and @($b129Present.Occurrences | Where-Object { $_.occurrences -eq 2 }).Count -eq 2) 'Avisos: grupo desconhecido e órfão deduplicados, 2 ocorrências cada.'
    Assert-True ($b129Present.HeaderLanguage -eq 'mixed') 'Avisos: cabeçalho em inglês e português = mixed.'
    $b129Real = Get-B129SatelliteWarnings -Output ((@($b129MsHeader, $b129MsCont, 'csc -> ok', 'Build succeeded.', $b129MsHeader, $b129MsCont, '    1 Warning(s)')) -join "`n")
    Assert-True ($b129Real.KnownWarnings.groups -eq 2 -and @($b129Real.Warnings).Count -eq 0 -and $b129Real.HeaderLanguage -eq 'en') 'Avisos: saída real (dois blocos mscorlib) sem nada em warnings.'
    $b129OnlyMs = Get-B129SatelliteWarnings -Output $b129MsHeader
    Assert-True ($b129OnlyMs.KnownWarnings.groups -eq 1 -and $b129OnlyMs.KnownWarnings.continuationLines -eq 0 -and @($b129OnlyMs.Warnings).Count -eq 0) 'Avisos: cabeçalho sem continuação é grupo conhecido com 0 continuações.'
    Assert-True (@((Get-B129SatelliteWarnings -Output $b129Orphan).Warnings).Count -eq 1) 'Avisos: só órfão vai para warnings.'
    $b129Interleaved = Get-B129SatelliteWarnings -Output (@($b129MsHeader, $b129PtHeader, $b129MsCont) -join "`n")
    Assert-True ($b129Interleaved.KnownWarnings.groups -eq 1 -and $b129Interleaved.KnownWarnings.continuationLines -eq 0 -and @($b129Interleaved.Warnings).Count -eq 1 -and @($b129Interleaved.Warnings)[0] -match '2 linha') 'Avisos: cabeçalho intercalado leva a continuação para o grupo seguinte.'
    $b129Separated = Get-B129SatelliteWarnings -Output (@($b129MsHeader, 'outra linha', $b129MsCont) -join "`n")
    Assert-True ($b129Separated.KnownWarnings.continuationLines -eq 0 -and @($b129Separated.Warnings).Count -eq 1) 'Avisos: continuação separada do cabeçalho vira órfã.'
    $b129Other = Get-B129SatelliteWarnings -Output (@('C:\f\Sat.csproj : warning : aviso sem codigo', 'C:\f\A.cs(1,1): warning CS0168: variavel', '    2 Aviso(s)', '    0 Erro(s)') -join "`n")
    Assert-True (@($b129Other.Warnings).Count -eq 2 -and @($b129Other.Warnings | Where-Object { $_ -notmatch '^satellite-build: ' }).Count -eq 0 -and $b129Other.HeaderLanguage -eq 'none') 'Avisos: aviso com e sem código vão para warnings com prefixo; contagens excluídas.'

    # Classificação de falhas (seção 4.4).
    $b129Root = Join-Path $tempRoot 'b129-root'
    $b129Outside = 'C:\B129SdkAusente\Sdk\Sdk.props'
    $b129Inside = Join-Path $b129Root 'Src\Extension\Ausente.props'
    $b129Failures = @(
        @{ Out = 'C:\p.csproj : error MSB4236: The SDK specified could not be found.'; Kind = 'sdkUnavailable'; Status = 'environmentBlocked' },
        @{ Out = "C:\p.csproj(1,1): error MSB4019: The imported project `"$b129Outside`" was not found."; Kind = 'sdkUnavailable'; Status = 'environmentBlocked' },
        @{ Out = "C:\p.csproj(1,1): error MSB4019: O projeto importado `"$b129Inside`" não foi encontrado."; Kind = 'repositoryImportMissing'; Status = 'failed' },
        @{ Out = 'C:\p.csproj(1,1): error MSB4019: The imported project was not found.'; Kind = 'unclassified'; Status = 'failed' },
        @{ Out = "C:\t.targets(1,1): error MSB3021: Unable to copy file `"a`" to `"b`". Access to the path 'b' is denied."; Kind = 'accessDenied'; Status = 'environmentBlocked' },
        @{ Out = "C:\t.targets(1,1): error MSB3021: Não é possível copiar o arquivo. Acesso ao caminho 'b' foi negado."; Kind = 'accessDenied'; Status = 'environmentBlocked' },
        @{ Out = "C:\t.targets(1,1): warning MSB3026: Could not copy. Beginning retry 1. Access to the path 'b' is denied.`nC:\t.targets(1,1): error MSB3027: Could not copy. Exceeded retry count of 10. Failed."; Kind = 'accessDenied'; Status = 'environmentBlocked' },
        @{ Out = 'C:\t.targets(1,1): error MSB3027: Could not copy. Exceeded retry count of 10. Failed.'; Kind = 'fileLocked'; Status = 'environmentBlocked' },
        @{ Out = 'C:\t.targets(1,1): error MSB3021: Unable to copy. The process cannot access the file because it is being used by another process.'; Kind = 'fileLocked'; Status = 'environmentBlocked' },
        @{ Out = 'C:\t.targets(1,1): error MSB3021: Não é possível copiar: o arquivo está sendo usado por outro processo.'; Kind = 'fileLocked'; Status = 'environmentBlocked' },
        @{ Out = "C:\t.targets(1,1): error MSB3021: Unable to copy file `"a`" to `"b`". Could not find file 'a'."; Kind = 'compilationOrBuildFailure'; Status = 'failed' },
        @{ Out = 'C:\p.csproj : error NETSDK1045: The current .NET SDK does not support targeting .NET 99.'; Kind = 'sdkUnavailable'; Status = 'environmentBlocked' },
        @{ Out = 'C:\p.csproj : error NETSDK1013: The TargetFramework value was not recognized.'; Kind = 'compilationOrBuildFailure'; Status = 'failed' },
        @{ Out = 'C:\global.json : error : A compatible installed .NET SDK for global.json version was not found.'; Kind = 'sdkUnavailable'; Status = 'environmentBlocked' },
        @{ Out = 'C:\p.csproj : error NU1301: Unable to load the service index for source https://api.nuget.org/v3/index.json.'; Kind = 'networkOrFeedUnavailable'; Status = 'environmentBlocked' },
        @{ Out = 'C:\p.csproj : error NU1101: Unable to find package Inexistente.'; Kind = 'compilationOrBuildFailure'; Status = 'failed' },
        @{ Out = 'C:\p.csproj : error : No such host is known.'; Kind = 'networkOrFeedUnavailable'; Status = 'environmentBlocked' },
        @{ Out = 'C:\A.cs(3,9): error CS0103: The name ProxyClient does not exist; the Proxy call timed out.'; Kind = 'compilationOrBuildFailure'; Status = 'failed' },
        @{ Out = "C:\t.targets(1,1): error MSB3030: Could not copy the file `"ProxyHelper.dll`" because it was not found."; Kind = 'compilationOrBuildFailure'; Status = 'failed' },
        @{ Out = "C:\f\Sat.csproj(9,5): warning MSB3277: Found conflicts between different versions of `"ProxyLib`" that timed out.`nC:\A.cs(1,1): error CS1002: ; expected"; Kind = 'compilationOrBuildFailure'; Status = 'failed' },
        @{ Out = "Build FAILED.`nC:\f\Sat.csproj : warning : nada de erro"; Kind = 'unclassified'; Status = 'failed' }
    )
    foreach ($b129Failure in $b129Failures) {
        $b129Classified = Get-B129FailureClassification -StdOut $b129Failure.Out -StdErr '' -RepositoryRoot $b129Root
        Assert-True ($b129Classified.Kind -eq $b129Failure.Kind -and $b129Classified.Status -eq $b129Failure.Status) "Classificação B129 divergente: esperado $($b129Failure.Kind), obtido $($b129Classified.Kind) para '$($b129Failure.Out)'."
    }

    # Guarda da D15 (seção 4.3).
    $b129IfLines = @(Get-B129IfDirectiveLines -Lines @('using X;', '#if DEBUG', '  # if B', '#IF C', '#ifdef D', '#endif'))
    Assert-True ($b129IfLines.Count -eq 2 -and $b129IfLines[0].line -eq 2 -and $b129IfLines[1].line -eq 3) 'Guarda D15: #if casa com -cmatch, inclusive com espaço; #IF e #ifdef não.'

    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'remote.git'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\scripts'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Src'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\ServiceSourceContract'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\MetadataIntegrity'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\ApiObjectOwnership'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\OwnershipDescriptions'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\GeneratedApiRemoval'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\ScanProbe'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\PersistenceProbe'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\OrphanMetadataRecovery'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\OperationJournal'))
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
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\BusinessComponentHierarchical'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\ListHierarchical'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\WizardHierarchical'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\WizardLifecycle'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\MetadataHierarchical'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\WizardContract'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\KbIndexReuse'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\ExtensionAssemblyInventory'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\Installation'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\Localization'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\IssueForms'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\TextPatch'))
    [void][System.IO.Directory]::CreateDirectory((Join-Path $tempRoot 'repo\Tests\PrePushChecker'))
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

        # B129 — pares canônico/satélite nos caminhos relativos de produção (plano B129, seção 4.7).
        $productionBuildProps = [System.IO.File]::ReadAllText((Join-Path $repositoryRoot 'Directory.Build.props'))
        $d47Block = [regex]::Match($productionBuildProps, "(?s)[ \t]*<PropertyGroup Condition=`"'\`$\(MSBuildProjectName\)' == 'GenexusOpenApiBuilder\.Extension\.Gx18u13'`">.*?</PropertyGroup>")
        Assert-True $d47Block.Success 'O bloco condicional D47 não foi encontrado no Directory.Build.props de produção.'
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Directory.Build.props'), "<Project>`n$($d47Block.Value)`n</Project>`n", $utf8)
        [void][System.IO.Directory]::CreateDirectory((Join-Path $PWD 'Src\Extension'))
        [void][System.IO.Directory]::CreateDirectory((Join-Path $PWD 'Src\Domain'))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Src\Domain\DomainItem.cs'), "namespace B129Fixture.Domain;`n`npublic static class DomainItem { }`n", $utf8)
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Src\Extension\Package.cs'), "namespace B129Fixture;`n`npublic static class Package { }`n", $utf8)
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Src\Extension\GenexusOpenApiBuilder.Extension.csproj'), @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net10.0</TargetFrameworks>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="..\Domain\**\*.cs" LinkBase="Domain" />
    <Compile Remove="Temp\**\*.cs" />
    <Compile Remove="Only.Sat\**" />
  </ItemGroup>
</Project>
'@, $utf8)
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Src\Extension\Compile.Shared.props'), @'
<Project>
  <ItemGroup>
    <Compile Include="**\*.cs" Exclude="bin\**;obj\**;Temp\**;Line.*\**;Only.Sat\**" LinkBase="Extension" />
    <Compile Include="..\Domain\**\*.cs" LinkBase="Domain" />
  </ItemGroup>
</Project>
'@, $utf8)
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Src\Extension\GenexusOpenApiBuilder.Extension.Gx18u13.csproj'), @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <Import Project="Compile.Shared.props" />
  <Import Project="Lib.Gx18u13.References.props" />
  <ItemGroup>
    <Compile Include="Only.Sat\**\*.cs" />
    <Compile Include="Line.Gx18u13\**\*.cs" LinkBase="Line.Gx18u13" />
  </ItemGroup>
  <Target Name="B129FixtureWarnings" BeforeTargets="CoreCompile" Condition="Exists('B129.warnings.flag')">
    <Warning Code="MSB3277" Text="Linha MSB3277 orfa sem cabecalho" />
    <Warning Code="MSB3277" Text="Found conflicts between different versions of &quot;mscorlib&quot; that could not be resolved." />
    <Warning Code="MSB3277" Text="continuacao do mscorlib 1" />
    <Warning Code="MSB3277" Text="continuacao do mscorlib 2" />
    <Warning Code="MSB3277" Text="foram encontrados conflitos entre diferentes versões do &quot;FixtureAssembly&quot; que não puderam ser resolvidas." />
    <Warning Code="MSB3277" Text="continuacao do outro assembly" />
  </Target>
</Project>
'@, $utf8)
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Src\Extension\Lib.Gx18u13.References.props'), @'
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <ItemGroup>
    <Reference Include="FakeRef">
      <HintPath>..\Lib\Gx18u13\FakeRef.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
'@, $utf8)
        & dotnet new sln --name GenexusOpenApiBuilder.Gx18u13 --output Src --format sln | Out-Null
        Assert-True ($LASTEXITCODE -eq 0) 'Não foi possível criar a solution satélite da fixture.'
        & dotnet sln Src\GenexusOpenApiBuilder.Gx18u13.sln add Src\Extension\GenexusOpenApiBuilder.Extension.Gx18u13.csproj | Out-Null
        Assert-True ($LASTEXITCODE -eq 0) 'Não foi possível adicionar o satélite à solution satélite da fixture.'
        [System.IO.File]::AppendAllText((Join-Path $PWD '.gitignore'), "artifacts/gx18u13/`nSrc/Lib/Gx18u13/`n", $utf8)
        [System.IO.File]::Copy($b129Module, (Join-Path $PWD 'scripts\B129-SatelliteChecks.ps1'))

        [System.IO.File]::Copy($checker, (Join-Path $PWD 'scripts\Invoke-PrePushMechanicalChecks.ps1'))
        [System.IO.File]::Copy((Join-Path $repositoryRoot 'scripts\B128-ReferenceTokenizer.ps1'), (Join-Path $PWD 'scripts\B128-ReferenceTokenizer.ps1'))
        [System.IO.File]::Copy((Join-Path $PSScriptRoot 'Test-B128ReferenceTokenizer.ps1'), (Join-Path $PWD 'Tests\PrePushChecker\Test-B128ReferenceTokenizer.ps1'))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\ServiceSourceContract\Test-ApiPlanServiceSourceContract.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Service Source contract'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\MetadataIntegrity\Test-ApiPlanMetadataIntegrity.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Metadata Integrity'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\ApiObjectOwnership\Test-ApiPlanApiObjectOwnership.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Api Object Ownership'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\OwnershipDescriptions\Test-ApiPlanOwnedObjectDescription.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Owned Object Description'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\GeneratedApiRemoval\Test-ApiPlanGeneratedApiRemovalPlan.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Generated Api Removal Plan'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\GeneratedApiRemoval\Test-ApiPlanGeneratedApiRemovalPreflight.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Generated Api Removal Preflight'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\GeneratedApiRemoval\Test-ApiPlanB082Etapa2Safety.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture B082 Etapa 2 Safety'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\GeneratedApiRemoval\Test-ApiPlanB082Etapa1BIndex.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture B082 Etapa 1B Index'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\ScanProbe\Test-ApiPlanScanProbe.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Scan Probe'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\PersistenceProbe\Test-ApiPlanPersistenceCore.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Persistence Core'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\PersistenceProbe\Test-ApiPlanSaveStepExecutor.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Persistence Executor'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\PersistenceProbe\Test-ApiPlanPersistenceSeamCoverage.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Persistence Seam Coverage'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\OrphanMetadataRecovery\Test-ApiPlanOrphanMetadataRecovery.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Orphan Metadata Recovery'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\OperationJournal\Test-ApiPlanOperationJournalSchema.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Operation Journal Schema'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\OperationJournal\Test-ApiPlanOperationJournalCheckpoints.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Operation Journal Checkpoints'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\OperationJournal\Test-ApiPlanOperationJournalReceipts.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Operation Journal Receipts'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\OperationJournal\Test-ApiPlanOperationJournalGate.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Operation Journal Gate'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\OperationJournal\Test-ApiPlanJournalFrontierSentinel.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Journal Frontier Sentinel'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\OperationJournal\Test-ApiPlanRecovery.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Recovery'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\GeneratedApiRemoval\Test-ApiPlanRemovalQueue.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Removal Queue'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\GeneratedApiRemoval\Test-ApiPlanGeneratedApiRemovalResilience.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Generated Api Removal Resilience'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\TransactionSync\Test-ApiPlanTransactionSyncComparer.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Transaction Sync Comparer'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\TransactionSync\Test-ApiPlanTransactionSyncFieldSelection.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Transaction Sync Field Selection'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\TransactionSync\Test-ApiPlanTransactionSyncSdtMemberSequenceMatcher.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Transaction Sync SDT Member Sequence Matcher'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\ApplicationFinalReport\Test-ApiPlanApplicationFinalReport.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Application Final Report'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\CollisionUx\Test-ApiPlanCollisionConflict.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Collision Ux'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\TransactionFolder\Test-ApiPlanTransactionFolderReusePolicy.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Transaction Folder Reuse'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WizardPreferences\Test-PrototypeWizardPreferences.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Wizard Preferences'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WizardNavigation\Test-PrototypeWizardBusinessComponentNavigationPolicy.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Wizard Navigation'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WritePreflight\Test-ApiPlanWritePreflightScope.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Write Preflight Scope'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WritePreflight\Test-ApiPlanWritePreflightHierarchicalStructure.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Write Preflight Hierarchical Structure'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WritePreflight\Test-ApiPlanWritePreflightBusinessComponentGuard.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Write Preflight Business Component Guard'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\BusinessComponentWriter\Test-ApiPlanBusinessComponentWriterVariableContract.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Business Component Writer Variable Contract'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\ListProcedure\Test-ApiPlanListProcedureReencounterPolicy.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture List Procedure Reencounter Policy'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\RequiredSemantics\Test-RequiredMemberSemanticsConsistency.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Required Member Semantics Consistency'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\OpenApiContract\Test-ApiPlanOpenApiContractMarks.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture OpenApi Contract Marks'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\OpenApiContract\Test-OpenApiClientContractValidity.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture OpenApi Client Contract Validity'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\GenerationBaseline\Test-ApiPlanGenerationBaseline.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Generation Baseline'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\TransactionStructure\Test-TransactionStructureReader.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Transaction Structure Reader'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\SdtHierarchicalPlan\Test-ApiPlanSdtHierarchicalPlan.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Sdt Hierarchical Plan'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\BusinessComponentHierarchical\Test-ApiPlanBusinessComponentHierarchical.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Business Component Hierarchical'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\ListHierarchical\Test-ApiPlanListHierarchical.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture List Hierarchical'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WizardHierarchical\Test-ApiPlanHierarchicalWizardSelection.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Wizard Hierarchical Selection'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WizardLifecycle\Test-ApiPlanWizardHierarchicalLifecycle.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Wizard Hierarchical Lifecycle'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\MetadataHierarchical\Test-ApiPlanMetadataLevels.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Metadata Hierarchical Levels'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WizardContract\Test-PrototypeWizardAutonumberCompositeKey.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Autonumber Composite Key'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WizardContract\Test-ApiPlanGenerationStateReaderGetAllIndex.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Generation State Reader GetAll Index'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\KbIndexReuse\Test-ApiPlanKbIndexReuse.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Kb Index Reuse'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WizardContract\Test-PrototypeWizardCreateRequiredPrimaryKeyOptional.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Create Required Primary Key Optional'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WizardContract\Test-PrototypeWizardNoAcceptRuleReader.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture NoAccept Rule Reader'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WizardContract\Test-NoAcceptRequestEligibilityContract.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture NoAccept Request Eligibility'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WizardContract\Test-PrototypeWizardExistingApiFilters.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Prototype Wizard Existing Api Filters'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\WizardContract\Test-PrototypeWizardServiceSourceParsing.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Prototype Wizard Service Source Parsing'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\ExtensionAssemblyInventory\Test-ExtensionAssemblyInventory.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Extension Assembly Inventory'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\Installation\Test-InstallExtensionBatPathHandling.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Installation BAT Path Handling'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\Localization\Test-ExtensionLanguage.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Extension Language'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\Localization\Test-ExtensionOutputLocalization.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Extension Output Localization'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\Localization\Test-ExtensionOutputLocalizationSelfConsistency.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Output Localization Self Consistency'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\IssueForms\Test-GitHubIssueFormsYaml.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Issue Forms Yaml'`n", [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Tests\TextPatch\Test-ApplyTextPatch.ps1'), "#requires -Version 7.4`nWrite-Output 'PASS: fixture Text Patch'`n", [System.Text.UTF8Encoding]::new($false))
        & git add .gitignore README.md Directory.Build.props Src scripts Tests
        foreach ($b110Test in @('Test-ApiPlanWriteBlockMessage.ps1', 'Test-ApiPlanExistingNamePolicy.ps1', 'Test-ApiPlanContractProvenance.ps1', 'Test-ReconstructedContractAcknowledgement.ps1', 'Test-ApiPlanB110Preflight.ps1')) {
            [IO.File]::WriteAllText((Join-Path $PWD "Tests\WritePreflight\$b110Test"), "#requires -Version 7.4`nWrite-Output 'PASS: fixture B110'`n", [Text.UTF8Encoding]::new($false))
        }
        & git add Tests/WritePreflight
        & git commit -m 'Fixture do checker' | Out-Null
        & git remote add origin (Join-Path $tempRoot 'remote.git')
        & git push -u origin main | Out-Null

        $json = & pwsh -NoProfile -File scripts/Invoke-PrePushMechanicalChecks.ps1 -AsJson
        $checkerExit = $LASTEXITCODE
        $result = $json | ConvertFrom-Json
        Assert-True ($checkerExit -eq 0) 'A fixture limpa deve concluir todos os checks mecânicos.'
        foreach ($b110Check in @('tests.writeBlockMessage', 'tests.existingNamePolicy', 'tests.contractProvenance', 'tests.reconstructedAcknowledgement', 'tests.b110Preflight')) {
            Assert-True (($result.checks | Where-Object name -eq $b110Check).status -eq 'passed') "Check B110 não executado: $b110Check"
        }
        Assert-True ($result.gitContext.branch -eq 'main') 'O checker não reconheceu a branch main na fixture.'
        Assert-True ($result.remoteReadiness -eq 'unverified') 'Sem -Fetch, a referência remota deve permanecer unverified.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'git.branch' }).status -eq 'passed') 'A checagem de branch deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.serviceSourceContract' }).status -eq 'passed') 'O teste unitário do parser Service Source deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.metadataIntegrity' }).status -eq 'passed') 'O teste unitário da integridade B067 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.apiObjectOwnership' }).status -eq 'passed') 'O teste unitário da posse B087 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.ownedObjectDescription' }).status -eq 'passed') 'O teste unitário das descrições de ownership deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.generatedApiRemovalPlan' }).status -eq 'passed') 'O teste unitário do plano de remoção B086 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.generatedApiRemovalPreflight' }).status -eq 'passed') 'O teste unitário do preflight B086 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.b082Etapa2Safety' }).status -eq 'passed') 'O teste textual da B082 Etapa 2 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.b082Etapa1BIndex' }).status -eq 'passed') 'O teste textual da B082 Etapa 1B deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.scanProbe' }).status -eq 'passed') 'O teste unitário do probe de medição B082 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.persistenceCore' }).status -eq 'passed') 'O teste unitário do seam de persistência B111/F2 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.persistenceExecutor' }).status -eq 'passed') 'O teste unitário do executor de persistência B111/F2 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.operationJournalSchema' }).status -eq 'passed') 'O teste do schema V1 do diário B111/F3 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.operationJournalCheckpoints' }).status -eq 'passed') 'O teste da matriz de checkpoints do diário B111/F3 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.operationJournalReceipts' }).status -eq 'passed') 'O teste do transporte de recibos do diário B111/F3 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.operationJournalGate' }).status -eq 'passed') 'O teste da precedência de diagnóstico do gate estendido do diário B111/F3 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.journalFrontierSentinel' }).status -eq 'passed') 'A sentinela da fronteira do API Object e da identidade composta B111/F3 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.removalQueue' }).status -eq 'passed') 'O teste da fila de remoção por passadas B111/F3 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.operationJournalRecovery' }).status -eq 'passed') 'O teste da recuperação B111/F3 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.transactionSyncComparer' }).status -eq 'passed') 'O teste unitário do diff B085 de sincronização deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.transactionSyncFieldSelection' }).status -eq 'passed') 'O teste unitário da seleção ordenada de campos B085 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.transactionSyncSdtMemberSequenceMatcher' }).status -eq 'passed') 'O teste unitário do reencontro de SDT com inclusões selecionadas deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.applicationFinalReport' }).status -eq 'passed') 'O teste unitário do relatório final B081 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.collisionUx' }).status -eq 'passed') 'O teste unitário da UX residual B083 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.transactionFolderReuse' }).status -eq 'passed') 'O teste unitário do reuso de Folder deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.wizardPreferences' }).status -eq 'passed') 'O teste unitário das preferências do wizard deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.wizardNavigation' }).status -eq 'passed') 'O teste unitário da navegação do wizard deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.writePreflightScope' }).status -eq 'passed') 'O teste unitário do escopo de preflight deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.writePreflightHierarchicalStructure' }).status -eq 'passed') 'O teste unitário do preflight hierárquico de subnível sem nome deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.writePreflightBusinessComponentGuard' }).status -eq 'passed') 'O teste unitário da guarda B055 de Business Component deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.businessComponentWriterVariableContract' }).status -eq 'passed') 'O teste unitário do contrato de variáveis do writer Business Component deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.listProcedureReencounterPolicy' }).status -eq 'passed') 'O teste unitário do reencontro B070 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.requiredMemberSemantics' }).status -eq 'passed') 'O teste unitário da coerência semântica de Required deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.openApiContractMarks' }).status -eq 'passed') 'O teste unitário das marcações do contrato OpenAPI deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.openApiClientContractValidity' }).status -eq 'passed') 'O teste unitário da validade do contrato de cliente OpenAPI deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.generationBaseline' }).status -eq 'passed') 'O teste unitário da linha de base de geração (Fase 0) deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.transactionStructure' }).status -eq 'passed') 'O teste unitário da leitura hierárquica B095 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.sdtHierarchicalPlan' }).status -eq 'passed') 'O teste unitário do plano de SDT hierárquico B096 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.businessComponentHierarchical' }).status -eq 'passed') 'O teste unitário do Source BC hierárquico B097 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.listHierarchical' }).status -eq 'passed') 'O teste unitário do List hierárquico B098 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.wizardHierarchical' }).status -eq 'passed') 'O teste unitário da seleção hierárquica do Wizard B099a deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.wizardLifecycle' }).status -eq 'passed') 'O teste unitário do ciclo de vida hierárquico do Wizard (Fase 7) deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.metadataHierarchical' }).status -eq 'passed') 'O teste unitário da metadata hierárquica B099b deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.wizardContractAutonumberCompositeKey' }).status -eq 'passed') 'O teste unitário de autonumeração e chave composta deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.wizardContractGenerationStateReader' }).status -eq 'passed') 'O teste unitário do leitor de estado de geração deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.kbIndexReuse' }).status -eq 'passed') 'O teste unitário da reutilização do índice da KB deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.wizardContractCreateRequired' }).status -eq 'passed') 'O teste unitário do contrato de wizard CreateRequired deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.wizardContractNoAcceptRuleReader' }).status -eq 'passed') 'O teste unitário da leitura de regras NoAccept deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.wizardContractNoAcceptRequestEligibility' }).status -eq 'passed') 'O teste unitário da elegibilidade de requests NoAccept deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.wizardContractExistingApiFilters' }).status -eq 'passed') 'O teste unitário dos filtros e restauração de API existente do wizard deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.wizardContractServiceSourceParsing' }).status -eq 'passed') 'O teste unitário do parser de Service Source do wizard deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.extensionAssemblyInventory' }).status -eq 'passed') 'O teste unitário do inventário offline da assembly deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.installationBatPathHandling' }).status -eq 'passed') 'O teste unitário do tratamento de caminhos dos BATs deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.extensionLanguage' }).status -eq 'passed') 'O teste unitário da resolução do idioma da extensão deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.extensionOutputLocalization' }).status -eq 'passed') 'O teste unitário da localização do Output da extensão deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.outputLocalizationSelfConsistency' }).status -eq 'passed') 'A auto-consistência do catálogo de saída deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.issueForms' }).status -eq 'passed') 'O teste unitário dos YAML / Issue Forms deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.textPatch' }).status -eq 'passed') 'O teste unitário da edição textual ancorada B122 deveria passar na fixture.'
        Assert-True (($result.checks | Where-Object { $_.name -eq 'tests.b128ReferenceTokenizer' }).status -eq 'passed') 'O teste do tokenizer compartilhado B128 deveria passar na fixture.'
        $b128Baseline = $result.checks | Where-Object { $_.name -eq 'docs.csharpLineReferences' }
        Assert-True ($b128Baseline.status -eq 'passed') 'Sem novas citações, o gate B128 deveria passar.'
        Assert-True ($b128Baseline.evidence.baseCandidates -eq 0 -and $b128Baseline.evidence.headCandidates -eq 0) 'O JSON deve expor a evidência de contagem B128 da fixture.'
        Assert-True (@($result.notCovered | Where-Object { $_ -match 'Referências legadas deslocadas' }).Count -eq 1 -and @($result.notCovered | Where-Object { $_ -match 'não prova que o texto citado' }).Count -eq 1) 'notCovered deve declarar legado e limite semântico B128.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/PrePushChecker/Test-B128ReferenceTokenizer.ps1' }).Count -eq 1) 'O comando do teste B128 deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/ServiceSourceContract/Test-ApiPlanServiceSourceContract.ps1' }).Count -eq 1) 'O comando do teste Service Source deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/MetadataIntegrity/Test-ApiPlanMetadataIntegrity.ps1' }).Count -eq 1) 'O comando do teste Metadata Integrity deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/ApiObjectOwnership/Test-ApiPlanApiObjectOwnership.ps1' }).Count -eq 1) 'O comando do teste Api Object Ownership deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/OwnershipDescriptions/Test-ApiPlanOwnedObjectDescription.ps1' }).Count -eq 1) 'O comando do teste Owned Object Description deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/GeneratedApiRemoval/Test-ApiPlanGeneratedApiRemovalPlan.ps1' }).Count -eq 1) 'O comando do teste Generated Api Removal Plan deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/GeneratedApiRemoval/Test-ApiPlanGeneratedApiRemovalPreflight.ps1' }).Count -eq 1) 'O comando do teste Generated Api Removal Preflight deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/ScanProbe/Test-ApiPlanScanProbe.ps1' }).Count -eq 1) 'O comando do teste do probe de medição B082 deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/TransactionSync/Test-ApiPlanTransactionSyncComparer.ps1' }).Count -eq 1) 'O comando do teste Transaction Sync Comparer deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/TransactionSync/Test-ApiPlanTransactionSyncFieldSelection.ps1' }).Count -eq 1) 'O comando do teste Transaction Sync Field Selection deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/TransactionSync/Test-ApiPlanTransactionSyncSdtMemberSequenceMatcher.ps1' }).Count -eq 1) 'O comando do teste Transaction Sync SDT Member Sequence Matcher deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/ApplicationFinalReport/Test-ApiPlanApplicationFinalReport.ps1' }).Count -eq 1) 'O comando do teste Application Final Report deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/CollisionUx/Test-ApiPlanCollisionConflict.ps1' }).Count -eq 1) 'O comando do teste Collision Ux deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/TransactionFolder/Test-ApiPlanTransactionFolderReusePolicy.ps1' }).Count -eq 1) 'O comando do teste Transaction Folder Reuse deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WizardPreferences/Test-PrototypeWizardPreferences.ps1' }).Count -eq 1) 'O comando do teste Wizard Preferences deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WizardNavigation/Test-PrototypeWizardBusinessComponentNavigationPolicy.ps1' }).Count -eq 1) 'O comando do teste Wizard Navigation deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WritePreflight/Test-ApiPlanWritePreflightScope.ps1' }).Count -eq 1) 'O comando do teste Write Preflight Scope deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WritePreflight/Test-ApiPlanWritePreflightHierarchicalStructure.ps1' }).Count -eq 1) 'O comando do teste Write Preflight Hierarchical Structure deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WritePreflight/Test-ApiPlanWritePreflightBusinessComponentGuard.ps1' }).Count -eq 1) 'O comando do teste Write Preflight Business Component Guard deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/BusinessComponentWriter/Test-ApiPlanBusinessComponentWriterVariableContract.ps1' }).Count -eq 1) 'O comando do teste Business Component Writer Variable Contract deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/OpenApiContract/Test-ApiPlanOpenApiContractMarks.ps1' }).Count -eq 1) 'O comando do teste OpenApi Contract Marks deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/OpenApiContract/Test-OpenApiClientContractValidity.ps1' }).Count -eq 1) 'O comando do teste OpenApi Client Contract Validity deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/GenerationBaseline/Test-ApiPlanGenerationBaseline.ps1' }).Count -eq 1) 'O comando do teste Generation Baseline deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/TransactionStructure/Test-TransactionStructureReader.ps1' }).Count -eq 1) 'O comando do teste Transaction Structure deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/SdtHierarchicalPlan/Test-ApiPlanSdtHierarchicalPlan.ps1' }).Count -eq 1) 'O comando do teste Sdt Hierarchical Plan deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/BusinessComponentHierarchical/Test-ApiPlanBusinessComponentHierarchical.ps1' }).Count -eq 1) 'O comando do teste Business Component Hierarchical deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/ListHierarchical/Test-ApiPlanListHierarchical.ps1' }).Count -eq 1) 'O comando do teste List Hierarchical deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WizardHierarchical/Test-ApiPlanHierarchicalWizardSelection.ps1' }).Count -eq 1) 'O comando do teste Wizard Hierarchical Selection deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WizardLifecycle/Test-ApiPlanWizardHierarchicalLifecycle.ps1' }).Count -eq 1) 'O comando do teste Wizard Hierarchical Lifecycle deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/MetadataHierarchical/Test-ApiPlanMetadataLevels.ps1' }).Count -eq 1) 'O comando do teste Metadata Hierarchical Levels deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WizardContract/Test-PrototypeWizardAutonumberCompositeKey.ps1' }).Count -eq 1) 'O comando do teste Autonumber Composite Key deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WizardContract/Test-ApiPlanGenerationStateReaderGetAllIndex.ps1' }).Count -eq 1) 'O comando do teste Generation State Reader GetAll Index deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/KbIndexReuse/Test-ApiPlanKbIndexReuse.ps1' }).Count -eq 1) 'O comando do teste Kb Index Reuse deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WizardContract/Test-PrototypeWizardCreateRequiredPrimaryKeyOptional.ps1' }).Count -eq 1) 'O comando do teste Create Required Primary Key Optional deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WizardContract/Test-PrototypeWizardNoAcceptRuleReader.ps1' }).Count -eq 1) 'O comando do teste NoAccept Rule Reader deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WizardContract/Test-NoAcceptRequestEligibilityContract.ps1' }).Count -eq 1) 'O comando do teste NoAccept Request Eligibility deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WizardContract/Test-PrototypeWizardExistingApiFilters.ps1' }).Count -eq 1) 'O comando do teste Prototype Wizard Existing Api Filters deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/WizardContract/Test-PrototypeWizardServiceSourceParsing.ps1' }).Count -eq 1) 'O comando do teste Prototype Wizard Service Source Parsing deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/ExtensionAssemblyInventory/Test-ExtensionAssemblyInventory.ps1' }).Count -eq 1) 'O comando do teste Extension Assembly Inventory deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/Installation/Test-InstallExtensionBatPathHandling.ps1' }).Count -eq 1) 'O comando do teste Installation BAT Path Handling deve aparecer no JSON.'
        Assert-True (@($result.commands | Where-Object { $_.command -eq 'pwsh -NoProfile -File Tests/TextPatch/Test-ApplyTextPatch.ps1' }).Count -eq 1) 'O comando do teste Text Patch B122 deve aparecer no JSON.'
        Assert-True (@($result.warnings).Count -eq 0) 'O checker não deve registrar "0 Aviso(s)" como warning.'
        # B129 — execução base: pares iguais, Package.cs limpo, sem Src/Lib/Gx18u13.
        Assert-True (($result.checks | Where-Object name -eq 'git.statusPre').status -eq 'passed') 'B129 base: a fixture deve partir de working tree limpa.'
        $b129BaseParity = $result.checks | Where-Object name -eq 'msbuild.compileSetParity'
        Assert-True ($b129BaseParity.status -eq 'passed') 'B129 base: a paridade deveria passar.'
        Assert-True ($b129BaseParity.evidence.targetFrameworks.canonical -eq 'net10.0' -and $b129BaseParity.evidence.targetFrameworks.satellite -eq 'net10.0') 'B129 base: a descoberta do alvo deveria rodar nos dois projetos.'
        Assert-True ($b129BaseParity.evidence.itemCounts.canonical -eq 2 -and $b129BaseParity.evidence.itemCounts.satellite -eq 2) 'B129 base: cada projeto deveria avaliar Package.cs e o .cs de Domain.'
        Assert-True (($result.checks | Where-Object name -eq 'source.packageNoIfDirective').status -eq 'passed') 'B129 base: a guarda de #if deveria passar.'
        $b129BaseSatellite = $result.checks | Where-Object name -eq 'dotnet.buildSatellite'
        Assert-True ($b129BaseSatellite.status -eq 'skipped' -and $b129BaseSatellite.evidence.kind -eq 'satelliteRefsAbsent') 'B129 base: sem refs, o satélite deveria sair skipped/satelliteRefsAbsent.'
        Assert-True ($result.satelliteRefs -eq 'absent') 'B129 base: satelliteRefs deveria ser absent.'
        Assert-True (@($result.notCovered | Where-Object { $_ -match 'Src/Lib/Gx18u13' }).Count -ge 1) 'B129 base: notCovered deveria declarar a cobertura condicional do satélite.'

        $baseCommit = (& git rev-parse HEAD).Trim()
        [System.IO.Directory]::CreateDirectory((Join-Path $PWD 'Docs')) | Out-Null
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Docs\b128-reference.md'), "$baseCommit`:Src/Fixture/Class1.cs#L1`n", [System.Text.UTF8Encoding]::new($false))
        & git add Docs\b128-reference.md
        & git commit -m 'Fixture com endereco fixo valido' | Out-Null
        $fixedJson = & pwsh -NoProfile -File scripts/Invoke-PrePushMechanicalChecks.ps1 -AsJson
        $fixedExit = $LASTEXITCODE
        $fixedResult = $fixedJson | ConvertFrom-Json
        Assert-True ($fixedExit -eq 0) 'Endereço fixo novo que aponta para blob ancestral deve passar.'
        Assert-True (($fixedResult.checks | Where-Object { $_.name -eq 'docs.csharpLineReferences' }).status -eq 'passed') 'Endereço fixo válido deve passar no gate B128.'
        Assert-True (($fixedResult.checks | Where-Object { $_.name -eq 'docs.csharpLineReferences' }).evidence.excessCandidates -eq 1) 'O JSON deve registrar o endereço fixo excedente avaliado.'
        & git push origin main | Out-Null

        [System.IO.Directory]::CreateDirectory((Join-Path $PWD 'Artifacts\B128')) | Out-Null
        [System.IO.File]::WriteAllBytes((Join-Path $PWD 'Artifacts\B128\InvalidUtf8.cs'), [byte[]]@(0xFF, 0x0A))
        & git add Artifacts\B128\InvalidUtf8.cs
        & git commit -m 'Fixture com fonte CSharp UTF-8 invalida' | Out-Null
        $invalidUtf8Commit = (& git rev-parse HEAD).Trim()
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Docs\b128-environment.md'), "$invalidUtf8Commit`:Artifacts/B128/InvalidUtf8.cs#L1`n", [System.Text.UTF8Encoding]::new($false))
        & git add Docs\b128-environment.md
        & git commit -m 'Fixture de bloqueio de ambiente B128' | Out-Null
        $environmentJson = & pwsh -NoProfile -File scripts/Invoke-PrePushMechanicalChecks.ps1 -AsJson
        $environmentExit = $LASTEXITCODE
        $environmentResult = $environmentJson | ConvertFrom-Json
        $environmentCheck = $environmentResult.checks | Where-Object { $_.name -eq 'docs.csharpLineReferences' }
        Assert-True ($environmentExit -eq 2 -and $environmentCheck.status -eq 'environmentBlocked') 'Blob CSharp que não decodifica deve bloquear como falha de ambiente no JSON.'
        Assert-True (@($environmentCheck.evidence.environmentFindings | Where-Object { $_.reason -match 'UTF-8' }).Count -eq 1) 'O JSON deve localizar a causa de ambiente B128.'
        & git push origin main | Out-Null

        [System.IO.File]::AppendAllText((Join-Path $PWD 'Docs\b128-reference.md'), "Src/Fixture/Class1.cs:1,2`n", [System.Text.UTF8Encoding]::new($false))
        & git add Docs\b128-reference.md
        & git commit -m 'Fixture com citacao movel invalida' | Out-Null
        $mobileJson = & pwsh -NoProfile -File scripts/Invoke-PrePushMechanicalChecks.ps1 -AsJson
        $mobileExit = $LASTEXITCODE
        $mobileResult = $mobileJson | ConvertFrom-Json
        $mobileCheck = $mobileResult.checks | Where-Object { $_.name -eq 'docs.csharpLineReferences' }
        Assert-True ($mobileExit -eq 1 -and $mobileCheck.status -eq 'failed') 'Citação móvel malformada deve bloquear o checker.'
        Assert-True (@($mobileCheck.evidence.findings | Where-Object { $_.reason -match 'localização inválida' }).Count -eq 1) 'O JSON deve localizar a citação móvel inválida.'
        & git push origin main | Out-Null

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

        # --- Evidência B124: aviso evidence-doc-required:* no canal warnings (não bloqueante) ---
        & git checkout -- README.md
        Assert-True ($LASTEXITCODE -eq 0) 'Não foi possível limpar a working tree antes da bateria B124.'

        [System.IO.File]::WriteAllText((Join-Path $PWD 'CHANGELOG.md'), (@'
## [Unreleased]

### Added

- item novo

### Validated

- validação nova

### Fixed

- correção nova

# [0.1.0-alpha.1] - 2026-01-01

### Validated

- validação publicada
'@), [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Docs\STATUS_ATUAL_E_PROXIMO_PASSO.md'), "## Próxima ação única`n`nExecutar B125.`n`n## Histórico`n`nB000 concluído.`n", [System.Text.UTF8Encoding]::new($false))
        & git add CHANGELOG.md Docs\STATUS_ATUAL_E_PROXIMO_PASSO.md
        & git commit -m 'Fixture baseline B124' | Out-Null
        & git push origin main | Out-Null

        function Invoke-FixtureChecker {
            $j = & pwsh -NoProfile -File scripts/Invoke-PrePushMechanicalChecks.ps1 -AsJson
            return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Result = ($j | ConvertFrom-Json) }
        }

        function Append-Commit {
            param([string]$File, [string]$Anchor, [string]$Extra)
            $text = [IO.File]::ReadAllText((Join-Path $PWD $File))
            Assert-True ($text.Contains($Anchor)) "Âncora ausente em $File."
            [IO.File]::WriteAllText((Join-Path $PWD $File), $text.Replace($Anchor, $Anchor + $Extra), [System.Text.UTF8Encoding]::new($false))
            & git add $File
            & git commit -m 'Fixture B124 delta' | Out-Null
        }

        # Negativo — baseline limpa: nenhum aviso de evidência.
        $baseRun = Invoke-FixtureChecker
        Assert-True ($baseRun.ExitCode -eq 0) 'Baseline B124 deve passar sem bloqueio.'
        Assert-True (@($baseRun.Result.warnings | Where-Object { $_ -match '^evidence-doc-required' }).Count -eq 0) 'Baseline B124 não deve emitir aviso de evidência.'

        [System.IO.File]::WriteAllText((Join-Path $PWD 'Docs\b128-changelog.md'), "CHANGELOG.md linha 89`nCHANGELOG.md line 90`nCHANGELOG.md línea 91`n", [System.Text.UTF8Encoding]::new($false))
        & git add Docs\b128-changelog.md
        & git commit -m 'Fixture referencia numerica CHANGELOG' | Out-Null
        $lineWarningRun = Invoke-FixtureChecker
        Assert-True ($lineWarningRun.ExitCode -eq 0) 'Aviso heurístico de linha CHANGELOG não bloqueia o checker.'
        Assert-True (@($lineWarningRun.Result.warnings | Where-Object { $_ -match '^b128-changelog-line-reference' }).Count -eq 3) 'Linhas numéricas próximas a CHANGELOG em português, inglês e espanhol devem emitir avisos B128.'
        & git push origin main | Out-Null

        [System.IO.File]::AppendAllText((Join-Path $PWD 'Docs\b128-changelog.md'), "CHANGELOG linha 90`n", [System.Text.UTF8Encoding]::new($false))
        $uncommittedLineRun = Invoke-FixtureChecker
        Assert-True (@($uncommittedLineRun.Result.warnings | Where-Object { $_ -match '^b128-changelog-line-reference' }).Count -eq 0) 'Aviso B128 CHANGELOG não incorpora alterações da working tree.'
        & git checkout -- Docs\b128-changelog.md
        Assert-True ($LASTEXITCODE -eq 0) 'Não foi possível limpar a alteração da fixture B128 CHANGELOG.'

        # Positivo — só Validated.
        Append-Commit -File 'CHANGELOG.md' -Anchor '- validação nova' -Extra "`n- validação nova 2"
        $vRun = Invoke-FixtureChecker
        Assert-True ($vRun.ExitCode -eq 0) 'Aviso de evidência não deve bloquear (Validated).'
        Assert-True (@($vRun.Result.warnings | Where-Object { $_ -match '^evidence-doc-required:validated' }).Count -eq 1) 'Tocar Validated de [Unreleased] deve emitir evidence-doc-required:validated.'
        Assert-True (@($vRun.Result.warnings | Where-Object { $_ -match '^evidence-doc-required:fixed' }).Count -eq 0) 'Só Validated não deve emitir evidence-doc-required:fixed.'
        Assert-True (@($vRun.Result.warnings | Where-Object { $_ -match '^evidence-doc-required:checkpoint' }).Count -eq 0) 'Só Validated não deve emitir evidence-doc-required:checkpoint.'
        Assert-True (@($vRun.Result.incompleteReasons).Count -eq 0) 'Aviso de evidência não alimenta incompleteReasons.'
        Assert-True ($vRun.Result.pushReadiness -eq 'readyLocal') 'Aviso de evidência não bloqueia pushReadiness.'
        & git push origin main | Out-Null

        # Positivo — só Fixed.
        Append-Commit -File 'CHANGELOG.md' -Anchor '- correção nova' -Extra "`n- correção nova 2"
        $fRun = Invoke-FixtureChecker
        Assert-True ($fRun.ExitCode -eq 0) 'Aviso de evidência não deve bloquear (Fixed).'
        Assert-True (@($fRun.Result.warnings | Where-Object { $_ -match '^evidence-doc-required:fixed' }).Count -eq 1) 'Tocar Fixed de [Unreleased] deve emitir evidence-doc-required:fixed.'
        & git push origin main | Out-Null

        # Positivo — checkpoint (Próxima ação única).
        Append-Commit -File 'Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md' -Anchor 'Executar B125.' -Extra ' (revisado)'
        $cRun = Invoke-FixtureChecker
        Assert-True ($cRun.ExitCode -eq 0) 'Aviso de evidência não deve bloquear (checkpoint).'
        Assert-True (@($cRun.Result.warnings | Where-Object { $_ -match '^evidence-doc-required:checkpoint' }).Count -eq 1) 'Tocar Próxima ação única deve emitir evidence-doc-required:checkpoint.'
        & git push origin main | Out-Null

        # Negativo — checkpoint fora das seções (Histórico).
        Append-Commit -File 'Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md' -Anchor 'B000 concluído.' -Extra "`nB999 histórico."
        $hRun = Invoke-FixtureChecker
        Assert-True (@($hRun.Result.warnings | Where-Object { $_ -match '^evidence-doc-required:checkpoint' }).Count -eq 0) 'Tocar fora da Próxima ação única não deve emitir aviso de checkpoint.'
        & git push origin main | Out-Null

        # Negativo — Validated de versão publicada.
        Append-Commit -File 'CHANGELOG.md' -Anchor '- validação publicada' -Extra "`n- publicada 2"
        $pRun = Invoke-FixtureChecker
        Assert-True (@($pRun.Result.warnings | Where-Object { $_ -match '^evidence-doc-required:validated' }).Count -eq 0) 'Tocar Validated de versão publicada não deve emitir aviso.'
        & git push origin main | Out-Null

        # Negativo — subseção não monitorada (Added) dentro de [Unreleased].
        Append-Commit -File 'CHANGELOG.md' -Anchor '- item novo' -Extra "`n- item novo 2"
        $aRun = Invoke-FixtureChecker
        Assert-True (@($aRun.Result.warnings | Where-Object { $_ -match '^evidence-doc-required:(validated|fixed)' }).Count -eq 0) 'Tocar Added não deve emitir aviso de Validated/Fixed.'

        # --- B129: cenários do satélite, depois de toda a bateria existente ---
        # Cada cenário com mudança rastreada commita, roda o checker e desfaz com git revert + push.
        & git push origin main | Out-Null
        Assert-True ($LASTEXITCODE -eq 0) 'B129: push antes dos cenários falhou.'

        function Invoke-B129Commit {
            param([string]$Message)
            & git add -A Src | Out-Null
            Assert-True ($LASTEXITCODE -eq 0) "B129: git add falhou ($Message)."
            & git commit -m $Message | Out-Null
            Assert-True ($LASTEXITCODE -eq 0) "B129: git commit falhou ($Message)."
        }
        function Undo-B129Commit {
            & git revert --no-edit HEAD | Out-Null
            Assert-True ($LASTEXITCODE -eq 0) 'B129: git revert falhou.'
            & git push origin main | Out-Null
            Assert-True ($LASTEXITCODE -eq 0) 'B129: push depois do revert falhou.'
        }
        function Get-B129Check {
            param([object]$Run, [string]$Name)
            return $Run.Result.checks | Where-Object name -eq $Name
        }

        # Defeitos de fonte: arquivo só do satélite, Line.Gx18u13 no canônico e #if no Package.cs.
        [void][System.IO.Directory]::CreateDirectory((Join-Path $PWD 'Src\Extension\Only.Sat'))
        [void][System.IO.Directory]::CreateDirectory((Join-Path $PWD 'Src\Extension\Line.Gx18u13'))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Src\Extension\Only.Sat\B129OnlySat.cs'), "namespace B129Fixture;`n`npublic static class B129OnlySat { }`n", $utf8)
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Src\Extension\Line.Gx18u13\B129Line.cs'), "namespace B129Fixture;`n`npublic static class B129Line { }`n", $utf8)
        [System.IO.File]::AppendAllText((Join-Path $PWD 'Src\Extension\Package.cs'), "#if DEBUG`n#endif`n", $utf8)
        Invoke-B129Commit 'B129 defeitos de fonte'
        $b129SourceRun = Invoke-FixtureChecker
        $b129SourceParity = Get-B129Check $b129SourceRun 'msbuild.compileSetParity'
        Assert-True ($b129SourceRun.ExitCode -eq 1) "B129 defeitos de fonte: exit esperado 1, obtido $($b129SourceRun.ExitCode)."
        Assert-True ($b129SourceParity.status -eq 'failed' -and $b129SourceParity.evidence.kind -eq 'parityMismatch') 'B129 defeitos de fonte: a paridade deveria falhar.'
        Assert-True (@($b129SourceParity.evidence.onlySatellite) -contains 'Extension/Only.Sat/B129OnlySat.cs') 'B129 defeitos de fonte: o arquivo de Only.Sat deveria estar em onlySatellite.'
        Assert-True (@($b129SourceParity.evidence.forbiddenInCanonical) -contains 'Extension/Line.Gx18u13/B129Line.cs') 'B129 defeitos de fonte: o arquivo de Line.Gx18u13 deveria estar em forbiddenInCanonical.'
        Assert-True (@($b129SourceParity.evidence.duplicates).Count -eq 0) 'B129 defeitos de fonte: a exclusão com curinga deveria evitar duplicata no satélite.'
        Assert-True ($b129SourceParity.summary -match 'D46') 'B129 defeitos de fonte: a mensagem deveria citar a D46.'
        $b129IfCheck = Get-B129Check $b129SourceRun 'source.packageNoIfDirective'
        Assert-True ($b129IfCheck.status -eq 'failed' -and $b129IfCheck.evidence.kind -eq 'ifDirectiveFound') 'B129 defeitos de fonte: a guarda de #if deveria falhar.'
        Undo-B129Commit

        # Satélite presente: refs completas e grupos de aviso emitidos pela fixture.
        $fakeRefRoot = Join-Path $tempRoot 'fakeref'
        & dotnet new classlib --name FakeRef --output $fakeRefRoot --framework net10.0 | Out-Null
        Assert-True ($LASTEXITCODE -eq 0) 'B129: não foi possível criar o FakeRef.'
        & dotnet build (Join-Path $fakeRefRoot 'FakeRef.csproj') --configuration Release -nodeReuse:false | Out-Null
        Assert-True ($LASTEXITCODE -eq 0) 'B129: não foi possível compilar o FakeRef.'
        $fakeRefLib = Join-Path $PWD 'Src\Lib\Gx18u13'
        [void][System.IO.Directory]::CreateDirectory($fakeRefLib)
        [System.IO.File]::Copy((Join-Path $fakeRefRoot 'bin\Release\net10.0\FakeRef.dll'), (Join-Path $fakeRefLib 'FakeRef.dll'))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Src\Extension\B129.warnings.flag'), "b129`n", $utf8)
        Invoke-B129Commit 'B129 satelite presente'
        $b129PresentRun = Invoke-FixtureChecker
        $b129PresentSatellite = Get-B129Check $b129PresentRun 'dotnet.buildSatellite'
        Assert-True ($b129PresentRun.ExitCode -eq 0) "B129 satélite presente: exit esperado 0, obtido $($b129PresentRun.ExitCode)."
        Assert-True ($b129PresentSatellite.status -eq 'passed') 'B129 satélite presente: o build satélite deveria passar.'
        Assert-True ($b129PresentRun.Result.satelliteRefs -eq 'complete') 'B129 satélite presente: satelliteRefs deveria ser complete.'
        Assert-True ($b129PresentSatellite.evidence.knownWarnings.groups -eq 2) "B129 satélite presente: esperados 2 grupos mscorlib conhecidos, obtidos $($b129PresentSatellite.evidence.knownWarnings.groups)."
        $b129PresentWarnings = @($b129PresentRun.Result.warnings | Where-Object { $_ -match '^satellite-build: ' })
        Assert-True ($b129PresentWarnings.Count -eq 2) "B129 satélite presente: esperadas 2 entradas satellite-build, obtidas $($b129PresentWarnings.Count): $($b129PresentWarnings -join ' | ')"
        Assert-True (@($b129PresentWarnings | Where-Object { $_ -match 'FixtureAssembly' }).Count -eq 1 -and @($b129PresentWarnings | Where-Object { $_ -match 'orfa sem cabecalho' }).Count -eq 1) 'B129 satélite presente: grupo do outro assembly e linha órfã, uma vez cada.'
        Assert-True (@($b129PresentSatellite.evidence.warningOccurrences | Where-Object { $_.occurrences -eq 2 }).Count -eq 2) 'B129 satélite presente: cada entrada deduplicada deveria registrar 2 ocorrências.'
        Assert-True ($b129PresentSatellite.evidence.headerLanguage -eq 'mixed') 'B129 satélite presente: headerLanguage deveria ser mixed.'
        Undo-B129Commit

        # Satélite quebrado: refs presentes e .cs inválido só do satélite.
        [void][System.IO.Directory]::CreateDirectory((Join-Path $PWD 'Src\Extension\Only.Sat'))
        [System.IO.File]::WriteAllText((Join-Path $PWD 'Src\Extension\Only.Sat\B129Broken.cs'), "namespace B129Fixture;`n`npublic static class B129Broken { public static int M() { return ; } }`n", $utf8)
        Invoke-B129Commit 'B129 satelite quebrado'
        $b129BrokenRun = Invoke-FixtureChecker
        $b129BrokenSatellite = Get-B129Check $b129BrokenRun 'dotnet.buildSatellite'
        Assert-True ($b129BrokenRun.ExitCode -eq 1) "B129 satélite quebrado: exit esperado 1, obtido $($b129BrokenRun.ExitCode)."
        Assert-True ($b129BrokenSatellite.status -eq 'failed' -and $b129BrokenSatellite.evidence.kind -eq 'compilationOrBuildFailure') "B129 satélite quebrado: o build satélite deveria falhar (kind $($b129BrokenSatellite.evidence.kind))."
        Assert-True ((Get-B129Check $b129BrokenRun 'msbuild.compileSetParity').status -eq 'failed') 'B129 satélite quebrado: a paridade também deveria falhar.'
        Undo-B129Commit

        # Refs incompletas: pasta mantida, referência pinada apagada. Sem mudança rastreada: sem commit nem revert.
        Remove-Item -LiteralPath (Join-Path $fakeRefLib 'FakeRef.dll') -Force
        $b129IncompleteRun = Invoke-FixtureChecker
        $b129IncompleteSatellite = Get-B129Check $b129IncompleteRun 'dotnet.buildSatellite'
        Assert-True ($b129IncompleteRun.ExitCode -eq 2) "B129 refs incompletas: exit esperado 2, obtido $($b129IncompleteRun.ExitCode)."
        Assert-True ($b129IncompleteSatellite.status -eq 'environmentBlocked' -and $b129IncompleteSatellite.evidence.kind -eq 'satelliteRefsIncomplete') 'B129 refs incompletas: o satélite deveria sair environmentBlocked/satelliteRefsIncomplete.'
        Assert-True ($b129IncompleteRun.Result.satelliteRefs -eq 'incomplete') 'B129 refs incompletas: satelliteRefs deveria ser incomplete.'
        Remove-Item -LiteralPath $fakeRefLib -Recurse -Force
    }
    finally {
        Pop-Location
    }
}
finally {
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
}

'OK: fixtures de classificação, tokenizer B128, validação de citações fixas/móveis, aviso numérico de CHANGELOG, fronteira operacional, Git limpo/sujo, fetch local, avisos B124 e checks B129 do satélite validados.'
