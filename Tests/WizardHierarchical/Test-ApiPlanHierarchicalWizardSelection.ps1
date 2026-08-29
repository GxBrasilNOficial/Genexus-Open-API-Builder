#requires -Version 7.4

[CmdletBinding()]
param(
    [string]$DllPath = '',
    [string]$GeneXusDirectory = 'C:\Program Files (x86)\GeneXus\GeneXus18'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$selectionSourcePath = Join-Path $repositoryRoot 'Src\Domain\ApiPlanHierarchicalWizardSelection.cs'
$dialogSourcePath = Join-Path $repositoryRoot 'Src\Extension\PrototypeWizardDialog.cs'
$builderSourcePath = Join-Path $repositoryRoot 'Src\Domain\ApiPlan.cs'
$readerSourcePath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\TransactionStructureReader.cs'

if ([string]::IsNullOrWhiteSpace($DllPath)) {
    $DllPath = Join-Path $repositoryRoot 'Src\Extension\bin\Release\net471\GenexusOpenApiBuilder.Extension.dll'
}

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

function Assert-Contains {
    param([string]$Text, [string]$Needle, [string]$Message)
    if ($Text.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "ASSERT_CONTAINS_FAILED: $Message"
    }
}

function Assert-NotContains {
    param([string]$Text, [string]$Needle, [string]$Message)
    if ($Text.IndexOf($Needle, [StringComparison]::Ordinal) -ge 0) {
        throw "ASSERT_NOT_CONTAINS_FAILED: $Message"
    }
}

function Get-Prop {
    param($Object, [string]$Name)
    $property = $Object.GetType().GetProperty($Name, [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    Assert-True ($null -ne $property) "Propriedade ausente: $Name"
    return $property.GetValue($Object)
}

function Get-AssemblyDirectoryCandidates {
    param([string]$GeneXusRoot)

    $candidates = [System.Collections.Generic.List[string]]::new()
    foreach ($relative in @('Packages', 'GeneXusBlazorControls', '')) {
        $path = if ([string]::IsNullOrWhiteSpace($relative)) { $GeneXusRoot } else { Join-Path $GeneXusRoot $relative }
        if (Test-Path -LiteralPath $path -PathType Container) {
            $candidates.Add($path)
        }
    }

    $dllDirectory = Split-Path -Parent $DllPath
    if (Test-Path -LiteralPath $dllDirectory -PathType Container) {
        $candidates.Add($dllDirectory)
    }

    return @($candidates | Select-Object -Unique)
}

function Initialize-GeneXusAssemblyResolver {
    param([string[]]$SearchDirectories)

    $script:AssemblySearchDirectories = @($SearchDirectories)
    $script:AssemblyResolveBusy = $false
    $script:AssemblyResolveHandler = [System.ResolveEventHandler]{
        param($sender, $args)
        if ($script:AssemblyResolveBusy) {
            return $null
        }

        $script:AssemblyResolveBusy = $true
        try {
            $requestedName = New-Object System.Reflection.AssemblyName($args.Name)
            $simpleName = $requestedName.Name
            if ($simpleName.EndsWith('.resources', [System.StringComparison]::OrdinalIgnoreCase)) {
                return $null
            }

            foreach ($directory in $script:AssemblySearchDirectories) {
                $candidate = Join-Path $directory ($simpleName + '.dll')
                if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                    return [System.Reflection.Assembly]::LoadFrom($candidate)
                }
            }

            return $null
        }
        finally {
            $script:AssemblyResolveBusy = $false
        }
    }

    [System.AppDomain]::CurrentDomain.add_AssemblyResolve($script:AssemblyResolveHandler)
}

Assert-True (Test-Path -LiteralPath $selectionSourcePath -PathType Leaf) "Fonte ausente: $selectionSourcePath"
Assert-True (Test-Path -LiteralPath $dialogSourcePath -PathType Leaf) "Fonte ausente: $dialogSourcePath"
Assert-True (Test-Path -LiteralPath $builderSourcePath -PathType Leaf) "Fonte ausente: $builderSourcePath"
Assert-True (Test-Path -LiteralPath $readerSourcePath -PathType Leaf) "Fonte ausente: $readerSourcePath"

$selectionSource = [IO.File]::ReadAllText($selectionSourcePath)
$dialogSource = [IO.File]::ReadAllText($dialogSourcePath)
$builderSource = [IO.File]::ReadAllText($builderSourcePath)
$readerSource = [IO.File]::ReadAllText($readerSourcePath)

Assert-Contains $selectionSource 'class ApiPlanHierarchicalWizardSelection' 'Tipo de seleção hierárquica B099a deve existir.'
Assert-Contains $selectionSource 'SetLevelIncluded' 'Dependência pai/filho deve existir.'
Assert-Contains $selectionSource 'IncludeListCount' 'Controle de contador deve existir.'
Assert-Contains $selectionSource 'WarnUnvalidatedDepth' 'Aviso de profundidade deve existir.'
Assert-Contains $selectionSource 'ApplyPersistedPrune' 'B099b deve restaurar poda persistida no Sync.'
Assert-Contains $selectionSource 'IncludeAddedFieldsByGuid' 'Sync hierárquico deve mesclar ADDED por GUID no nível certo.'
Assert-Contains $selectionSource 'ResolvePersistedNamesToCurrent' 'ApplyPersistedPrune deve remapear Selected* por AttributeGuid.'
Assert-NotContains $selectionSource 'LifecycleV1WarningText' 'Aviso V1 de ciclo de vida deve ter sido removido em B099b.'
Assert-NotContains $dialogSource 'LifecycleV1WarningText' 'Wizard não deve mais exibir aviso V1 de Remover/Sync.'
Assert-Contains $dialogSource 'HierarchicalSelection' 'O fluxo do Wizard deve persistir a seleção hierárquica.'
Assert-Contains $dialogSource '_levelCreateFieldsList' 'Listas de subnível devem ser distintas das do cabeçalho.'
Assert-Contains $builderSource 'ResolveHierarchicalLevels' 'ApiPlanBuilder deve podar Levels a partir da seleção.'
Assert-Contains $readerSource 'CreateFourDeepFixture' 'Fixture de profundidade 4 deve existir fora de CreateFixtures.'
Assert-Contains $readerSource 'CreateFiveDeepFixture' 'Fixture de profundidade 5 deve existir fora de CreateFixtures.'

$orchestratorSourcePath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\ApiPlanTransactionSyncOrchestrator.cs'
Assert-True (Test-Path -LiteralPath $orchestratorSourcePath -PathType Leaf) "Fonte ausente: $orchestratorSourcePath"
$orchestratorSource = [IO.File]::ReadAllText($orchestratorSourcePath)
Assert-Contains $orchestratorSource 'ApplyHierarchicalIncludeAdded' 'BuildSelection do Sync deve mesclar IncludeAdded na árvore.'
Assert-Contains $orchestratorSource 'IncludeAddedFieldsByGuid' 'Orchestrator deve chamar IncludeAddedFieldsByGuid após o prune.'

$createFixturesBlock = [regex]::Match($readerSource, 'public static IReadOnlyList<TransactionStructureFixture> CreateFixtures\(\)[\s\S]*?return new\[\]')
Assert-True $createFixturesBlock.Success 'CreateFixtures deve existir.'
Assert-NotContains $createFixturesBlock.Value 'CreateFourDeepFixture' 'FourDeep não deve entrar no ouro B095.'
Assert-NotContains $createFixturesBlock.Value 'CreateFiveDeepFixture' 'FiveDeep não deve entrar no ouro B095.'

function Find-NodeByLevelName {
    param($Selection, [string]$Name)
    foreach ($node in @(Get-Prop $Selection 'Options')) {
        if ([string](Get-Prop (Get-Prop $node 'Level') 'LevelName') -eq $Name) {
            return $node
        }
    }

    return $null
}

function Get-InstanceMethod {
    param($Type, [string]$Name)
    $method = $Type.GetMethod($Name, [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    Assert-True ($null -ne $method) "Método de instância ausente: $Name"
    return $method
}

function Get-StaticMethod {
    param($Type, [string]$Name)
    $method = $Type.GetMethod($Name, [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    return $method
}

if (-not (Test-Path -LiteralPath $DllPath -PathType Leaf)) {
    Write-Output "ENVIRONMENT_BLOCKED: DLL Release ausente em $DllPath"
    exit 2
}

if (-not (Test-Path -LiteralPath $GeneXusDirectory -PathType Container)) {
    Write-Output "ENVIRONMENT_BLOCKED: Instalacao GeneXus nao encontrada em modo leitura: $GeneXusDirectory"
    exit 2
}

$script:AssemblyResolveHandler = $null
try {
    Initialize-GeneXusAssemblyResolver -SearchDirectories (Get-AssemblyDirectoryCandidates -GeneXusRoot $GeneXusDirectory)
    $assembly = [System.Reflection.Assembly]::LoadFrom($DllPath)
    $readerType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.TransactionStructureReader', $true, $false)
    $selectionType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Domain.ApiPlanHierarchicalWizardSelection', $true, $false)
    $sdtPlanType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Domain.ApiPlanSdtGenerationPlanBuilder', $true, $false)
    Assert-True ($null -ne $sdtPlanType) 'ApiPlanSdtGenerationPlanBuilder não encontrado.'

    $createFixtures = $readerType.GetMethod('CreateFixtures', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $createFourDeep = $readerType.GetMethod('CreateFourDeepFixture', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $createFiveDeep = $readerType.GetMethod('CreateFiveDeepFixture', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $createDefault = $selectionType.GetMethod('CreateDefault', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $hasSelectedMethod = Get-InstanceMethod $selectionType 'HasSelectedSublevels'
    $countSelectedMethod = Get-InstanceMethod $selectionType 'CountSelectedSublevels'
    $getIncludeCountMethod = Get-InstanceMethod $selectionType 'GetIncludeListCount'
    $setIncludeCountMethod = Get-InstanceMethod $selectionType 'SetIncludeListCount'
    $setFieldMethod = Get-InstanceMethod $selectionType 'SetFieldSelected'
    $setIncludedMethod = Get-InstanceMethod $selectionType 'SetLevelIncluded'
    $isIncludedMethod = Get-InstanceMethod $selectionType 'IsLevelIncluded'
    $pruneMethod = Get-InstanceMethod $selectionType 'Prune'
    $applyPersistedPruneMethod = Get-InstanceMethod $selectionType 'ApplyPersistedPrune'
    $includeAddedMethod = Get-InstanceMethod $selectionType 'IncludeAddedFieldsByGuid'
    $getSelectedFieldsMethod = Get-InstanceMethod $selectionType 'GetSelectedFields'
    $levelType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Domain.ApiPlanLevel', $true, $false)
    $fieldType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Domain.ApiPlanLevelField', $true, $false)
    Assert-True ($null -ne $levelType) 'ApiPlanLevel não encontrado.'
    Assert-True ($null -ne $fieldType) 'ApiPlanLevelField não encontrado.'

    Assert-True ($null -ne $createFixtures) 'CreateFixtures não encontrado.'
    Assert-True ($null -ne $createFourDeep) 'CreateFourDeepFixture não encontrado.'
    Assert-True ($null -ne $createFiveDeep) 'CreateFiveDeepFixture não encontrado.'
    Assert-True ($null -ne $createDefault) 'CreateDefault não encontrado.'

    function New-LevelFieldFromTemplate {
        param(
            $Template,
            [string]$Name = '',
            [string]$AttributeGuid = ''
        )

        $ctor = $fieldType.GetConstructors()[0]
        $resolvedName = if ([string]::IsNullOrWhiteSpace($Name)) { [string](Get-Prop $Template 'Name') } else { $Name }
        $resolvedGuid = if ([string]::IsNullOrWhiteSpace($AttributeGuid)) { [string](Get-Prop $Template 'AttributeGuid') } else { $AttributeGuid }
        return $ctor.Invoke(@(
                [int](Get-Prop $Template 'Order'),
                $resolvedGuid,
                $resolvedName,
                [string](Get-Prop $Template 'DataType'),
                [int](Get-Prop $Template 'Length'),
                [int](Get-Prop $Template 'Decimals'),
                [bool](Get-Prop $Template 'IsPrimaryKey'),
                [bool](Get-Prop $Template 'IsNullable'),
                [bool](Get-Prop $Template 'IsInferred'),
                [bool](Get-Prop $Template 'IsRedundant'),
                [bool](Get-Prop $Template 'IsForeignKey'),
                [bool](Get-Prop $Template 'IsFormula'),
                [bool](Get-Prop $Template 'IsNoAccept'),
                [bool](Get-Prop $Template 'IsAutonumber')
            ))
    }

    function New-LevelFromTemplate {
        param(
            $Template,
            $Fields = $null,
            $ChildLevels = $null,
            $SelectedCreate = $null,
            $SelectedUpdate = $null,
            $SelectedResponse = $null,
            [bool]$KeepSelectedNull = $false
        )

        $ctor = $levelType.GetConstructors() | Sort-Object { $_.GetParameters().Count } -Descending | Select-Object -First 1
        if ($PSBoundParameters.ContainsKey('Fields')) {
            if ($null -eq $Fields) {
                $resolvedFields = @()
            }
            else {
                $resolvedFields = @($Fields)
            }
        }
        else {
            $resolvedFields = @(Get-Prop $Template 'Fields')
        }

        if ($PSBoundParameters.ContainsKey('ChildLevels')) {
            if ($null -eq $ChildLevels) {
                $resolvedChildren = @()
            }
            else {
                $resolvedChildren = @($ChildLevels)
            }
        }
        else {
            $resolvedChildren = @(Get-Prop $Template 'ChildLevels')
        }

        $fieldArray = [System.Array]::CreateInstance($fieldType, @($resolvedFields).Length)
        for ($i = 0; $i -lt @($resolvedFields).Length; $i++) {
            $fieldArray.SetValue($resolvedFields[$i], $i)
        }

        $childArray = [System.Array]::CreateInstance($levelType, @($resolvedChildren).Length)
        for ($i = 0; $i -lt @($resolvedChildren).Length; $i++) {
            $childArray.SetValue($resolvedChildren[$i], $i)
        }

        $pkSource = @(Get-Prop $Template 'PrimaryKey')
        $pkArray = [System.Array]::CreateInstance($fieldType, @($pkSource).Length)
        for ($i = 0; $i -lt @($pkSource).Length; $i++) {
            $pkArray.SetValue((New-LevelFieldFromTemplate -Template $pkSource[$i]), $i)
        }

        $createNames = $null
        $updateNames = $null
        $responseNames = $null
        if (-not $KeepSelectedNull) {
            if ($null -ne $SelectedCreate) {
                $createNames = [string[]]@($SelectedCreate)
            }
            elseif ($null -ne (Get-Prop $Template 'SelectedCreateFieldNames')) {
                $createNames = [string[]]@(Get-Prop $Template 'SelectedCreateFieldNames')
            }

            if ($null -ne $SelectedUpdate) {
                $updateNames = [string[]]@($SelectedUpdate)
            }
            elseif ($null -ne (Get-Prop $Template 'SelectedUpdateFieldNames')) {
                $updateNames = [string[]]@(Get-Prop $Template 'SelectedUpdateFieldNames')
            }

            if ($null -ne $SelectedResponse) {
                $responseNames = [string[]]@($SelectedResponse)
            }
            elseif ($null -ne (Get-Prop $Template 'SelectedResponseFieldNames')) {
                $responseNames = [string[]]@(Get-Prop $Template 'SelectedResponseFieldNames')
            }
        }

        return $ctor.Invoke(@(
                [string](Get-Prop $Template 'LevelName'),
                [int](Get-Prop $Template 'Depth'),
                [string](Get-Prop $Template 'ParentLevelName'),
                [int](Get-Prop $Template 'LevelOrder'),
                $pkArray,
                $fieldArray,
                $childArray,
                [bool](Get-Prop $Template 'IncludeListCount'),
                $createNames,
                $updateNames,
                $responseNames
            ))
    }

    $fixtures = @($createFixtures.Invoke($null, @()))
    $byName = @{}
    foreach ($fixture in $fixtures) {
        $byName[[string](Get-Prop $fixture 'Name')] = $fixture
    }

    $oneRoot = Get-Prop (Get-Prop $byName['OneSublevel'] 'Snapshot') 'RootLevel'
    $oneSelection = $createDefault.Invoke($null, @($oneRoot))
    Assert-True ([bool](Get-Prop $oneSelection 'HasSublevels')) 'OneSublevel deve ter subníveis.'
    Assert-True ([bool]$hasSelectedMethod.Invoke($oneSelection, @())) 'Default deve incluir o subnível Lines.'
    Assert-True (([int]$countSelectedMethod.Invoke($oneSelection, @())) -eq 1) 'OneSublevel default: 1 subnível.'
    Assert-False ([bool](Get-Prop $oneSelection 'WarnUnvalidatedDepth')) 'Profundidade 2 não avisa.'

    $linesNode = Find-NodeByLevelName $oneSelection 'Lines'
    Assert-True ($null -ne $linesNode) 'Nó Lines deve existir.'
    $linesKey = [string](Get-Prop $linesNode 'PathKey')
    Assert-True ([bool]$getIncludeCountMethod.Invoke($oneSelection, @($linesKey))) 'Contador de List default ligado no filho direto.'

    [void]$setIncludeCountMethod.Invoke($oneSelection, @($linesKey, $false))
    $prunedDisabled = $pruneMethod.Invoke($oneSelection, @())
    $prunedChildren = @(Get-Prop $prunedDisabled 'ChildLevels')
    Assert-True ($prunedChildren.Count -eq 1) 'Poda com contador desligado ainda gera o subnível.'
    Assert-False ([bool](Get-Prop $prunedChildren[0] 'IncludeListCount')) 'IncludeListCount=false deve sobreviver à poda.'

    [void]$setIncludeCountMethod.Invoke($oneSelection, @($linesKey, $true))
    foreach ($fieldName in @('LineId', 'LineQty', 'LineTotal', 'LineStamp')) {
        [void]$setFieldMethod.Invoke($oneSelection, @($linesKey, 'CreateRequest', $fieldName, $false))
        [void]$setFieldMethod.Invoke($oneSelection, @($linesKey, 'UpdateRequest', $fieldName, $false))
        [void]$setFieldMethod.Invoke($oneSelection, @($linesKey, 'Response', $fieldName, $false))
    }

    Assert-False ([bool]$hasSelectedMethod.Invoke($oneSelection, @())) 'Subnível sem campos marcados não é gerado.'

    [void]$setFieldMethod.Invoke($oneSelection, @($linesKey, 'Response', 'LineQty', $true))
    Assert-True ([bool]$hasSelectedMethod.Invoke($oneSelection, @())) 'Um campo marcado reintroduz o subnível.'
    $prunedRole = $pruneMethod.Invoke($oneSelection, @())
    $prunedRoleChild = @(Get-Prop $prunedRole 'ChildLevels')[0]
    $selectForRole = Get-StaticMethod $sdtPlanType 'SelectLevelFieldsForRole'
    Assert-True ($null -ne $selectForRole) 'SelectLevelFieldsForRole deve existir no plano de SDT.'
    $createEligible = @($selectForRole.Invoke($null, @($prunedRoleChild, 'CreateRequest')))
    $updateEligible = @($selectForRole.Invoke($null, @($prunedRoleChild, 'UpdateRequest')))
    $responseEligible = @($selectForRole.Invoke($null, @($prunedRoleChild, 'Response')))
    Assert-True ($createEligible.Count -eq 0) 'CreateRequest não deve herdar campo marcado só em Response.'
    Assert-True ($updateEligible.Count -eq 0) 'UpdateRequest não deve herdar campo marcado só em Response.'
    Assert-True ($responseEligible.Count -eq 1) 'Response deve preservar LineQty elegível.'
    Assert-True ([string](Get-Prop $responseEligible[0] 'Name') -eq 'LineQty') 'O campo Response deve ser LineQty.'

    $lineFieldNames = @('LineId', 'LineQty', 'LineTotal', 'LineStamp')
    function Clear-LineRoleFields {
        param($Selection, [string]$Key)
        foreach ($fieldName in $lineFieldNames) {
            foreach ($role in @('CreateRequest', 'UpdateRequest', 'Response')) {
                [void]$setFieldMethod.Invoke($Selection, @($Key, $role, $fieldName, $false))
            }
        }
    }

    function Assert-RoleEligibleNames {
        param(
            $PrunedChild,
            [string]$Role,
            [string[]]$ExpectedNames,
            [string]$Message
        )

        $eligible = @($selectForRole.Invoke($null, @($PrunedChild, $Role)))
        $actual = @($eligible | ForEach-Object { [string](Get-Prop $_ 'Name') })
        Assert-True ($actual.Count -eq $ExpectedNames.Count) ("{0} (count): esperado {1}, obtido {2} [{3}]." -f $Message, $ExpectedNames.Count, $actual.Count, ($actual -join ','))
        foreach ($expectedName in $ExpectedNames) {
            Assert-True ($actual -contains $expectedName) ("{0}: falta {1}." -f $Message, $expectedName)
        }
    }

    Clear-LineRoleFields -Selection $oneSelection -Key $linesKey
    [void]$setFieldMethod.Invoke($oneSelection, @($linesKey, 'CreateRequest', 'LineQty', $true))
    $prunedCreateOnly = @(Get-Prop ($pruneMethod.Invoke($oneSelection, @())) 'ChildLevels')[0]
    Assert-RoleEligibleNames $prunedCreateOnly 'CreateRequest' @('LineQty') 'Create-only'
    Assert-RoleEligibleNames $prunedCreateOnly 'UpdateRequest' @() 'Create-only não vaza para Update'
    Assert-RoleEligibleNames $prunedCreateOnly 'Response' @() 'Create-only não vaza para Response'
    Assert-True (@(Get-Prop $prunedCreateOnly 'SelectedCreateFieldNames') -contains 'LineQty') 'SelectedCreateFieldNames guarda LineQty.'
    Assert-True (@(Get-Prop $prunedCreateOnly 'SelectedUpdateFieldNames').Count -eq 0) 'SelectedUpdateFieldNames vazio no Create-only.'
    Assert-True (@(Get-Prop $prunedCreateOnly 'SelectedResponseFieldNames').Count -eq 0) 'SelectedResponseFieldNames vazio no Create-only.'

    Clear-LineRoleFields -Selection $oneSelection -Key $linesKey
    [void]$setFieldMethod.Invoke($oneSelection, @($linesKey, 'UpdateRequest', 'LineQty', $true))
    $prunedUpdateOnly = @(Get-Prop ($pruneMethod.Invoke($oneSelection, @())) 'ChildLevels')[0]
    Assert-RoleEligibleNames $prunedUpdateOnly 'CreateRequest' @() 'Update-only não vaza para Create'
    Assert-RoleEligibleNames $prunedUpdateOnly 'UpdateRequest' @('LineQty') 'Update-only'
    Assert-RoleEligibleNames $prunedUpdateOnly 'Response' @() 'Update-only não vaza para Response'

    Clear-LineRoleFields -Selection $oneSelection -Key $linesKey
    [void]$setFieldMethod.Invoke($oneSelection, @($linesKey, 'CreateRequest', 'LineQty', $true))
    [void]$setFieldMethod.Invoke($oneSelection, @($linesKey, 'UpdateRequest', 'LineQty', $true))
    $prunedCreateUpdate = @(Get-Prop ($pruneMethod.Invoke($oneSelection, @())) 'ChildLevels')[0]
    Assert-RoleEligibleNames $prunedCreateUpdate 'CreateRequest' @('LineQty') 'Create+Update sem Response (Create)'
    Assert-RoleEligibleNames $prunedCreateUpdate 'UpdateRequest' @('LineQty') 'Create+Update sem Response (Update)'
    Assert-RoleEligibleNames $prunedCreateUpdate 'Response' @() 'Create+Update não inventa Response'

    Clear-LineRoleFields -Selection $oneSelection -Key $linesKey
    [void]$setFieldMethod.Invoke($oneSelection, @($linesKey, 'CreateRequest', 'LineQty', $true))
    [void]$setFieldMethod.Invoke($oneSelection, @($linesKey, 'UpdateRequest', 'LineId', $true))
    [void]$setFieldMethod.Invoke($oneSelection, @($linesKey, 'Response', 'LineStamp', $true))
    $prunedDistinct = @(Get-Prop ($pruneMethod.Invoke($oneSelection, @())) 'ChildLevels')[0]
    Assert-RoleEligibleNames $prunedDistinct 'CreateRequest' @('LineQty') 'Papéis distintos (Create=LineQty)'
    Assert-RoleEligibleNames $prunedDistinct 'UpdateRequest' @('LineId') 'Papéis distintos (Update=LineId)'
    Assert-RoleEligibleNames $prunedDistinct 'Response' @('LineStamp') 'Papéis distintos (Response=LineStamp)'
    $catalogFields = @((Get-Prop $prunedDistinct 'Fields') | ForEach-Object { [string](Get-Prop $_ 'Name') })
    Assert-True ($catalogFields -contains 'LineQty') 'Fields mantém LineQty (marcado em Create).'
    Assert-True ($catalogFields -contains 'LineId') 'Fields mantém LineId (marcado em Update).'
    Assert-True ($catalogFields -contains 'LineStamp') 'Fields mantém LineStamp (marcado em Response).'
    Assert-True ($catalogFields -contains 'LineTotal') 'Fields mantém LineTotal omitido — catálogo completo (anti falso Added no Sync).'
    Assert-False (@(Get-Prop $prunedDistinct 'SelectedCreateFieldNames') -contains 'LineTotal') 'LineTotal omitido não entra em SelectedCreate.'
    Assert-False (@(Get-Prop $prunedDistinct 'SelectedUpdateFieldNames') -contains 'LineTotal') 'LineTotal omitido não entra em SelectedUpdate.'
    Assert-False (@(Get-Prop $prunedDistinct 'SelectedResponseFieldNames') -contains 'LineTotal') 'LineTotal omitido não entra em SelectedResponse.'

    $parallelRoot = Get-Prop (Get-Prop $byName['ParallelSublevels'] 'Snapshot') 'RootLevel'
    $parallelSelection = $createDefault.Invoke($null, @($parallelRoot))
    Assert-True (([int]$countSelectedMethod.Invoke($parallelSelection, @())) -eq 2) 'Parallel default: Notes e Tags.'
    $notesNode = Find-NodeByLevelName $parallelSelection 'Notes'
    $tagsNode = Find-NodeByLevelName $parallelSelection 'Tags'
    $notesKey = [string](Get-Prop $notesNode 'PathKey')
    $tagsKey = [string](Get-Prop $tagsNode 'PathKey')
    [void]$setIncludedMethod.Invoke($parallelSelection, @($notesKey, $false))
    $prunedParallel = $pruneMethod.Invoke($parallelSelection, @())
    $parallelChildren = @(Get-Prop $prunedParallel 'ChildLevels')
    Assert-True ($parallelChildren.Count -eq 1) 'Desmarcar Notes deixa só Tags.'
    Assert-True ([string](Get-Prop $parallelChildren[0] 'LevelName') -eq 'Tags') 'O filho sobrevivente deve ser Tags.'
    Assert-True ([bool]$isIncludedMethod.Invoke($parallelSelection, @($tagsKey))) 'Tags permanece incluído.'

    $threeRoot = Get-Prop (Get-Prop $byName['ThreeDeep'] 'Snapshot') 'RootLevel'
    $threeSelection = $createDefault.Invoke($null, @($threeRoot))
    Assert-False ([bool](Get-Prop $threeSelection 'WarnUnvalidatedDepth')) 'Profundidade 3 não avisa.'
    $shiftNode = Find-NodeByLevelName $threeSelection 'Shift'
    $workerNode = Find-NodeByLevelName $threeSelection 'Worker'
    $shiftKey = [string](Get-Prop $shiftNode 'PathKey')
    $workerKey = [string](Get-Prop $workerNode 'PathKey')
    Assert-False ([bool](Get-Prop $workerNode 'CanIncludeListCount')) 'Neto não exibe contador de List.'
    Assert-True ([bool](Get-Prop $shiftNode 'CanIncludeListCount')) 'Filho direto exibe contador de List.'

    [void]$setIncludedMethod.Invoke($threeSelection, @($shiftKey, $false))
    Assert-False ([bool]$isIncludedMethod.Invoke($threeSelection, @($workerKey))) 'Desmarcar o pai desmarca o neto.'
    Assert-False ([bool]$hasSelectedMethod.Invoke($threeSelection, @())) 'Sem pai, a árvore podada não tem filhos.'

    [void]$setIncludedMethod.Invoke($threeSelection, @($workerKey, $true))
    Assert-True ([bool]$isIncludedMethod.Invoke($threeSelection, @($shiftKey))) 'Marcar o neto inclui o pai.'
    Assert-True ([bool]$hasSelectedMethod.Invoke($threeSelection, @())) 'Pai e neto voltam à poda.'
    $prunedThree = $pruneMethod.Invoke($threeSelection, @())
    $shiftPruned = @(Get-Prop $prunedThree 'ChildLevels')[0]
    $workerPruned = @(Get-Prop $shiftPruned 'ChildLevels')
    Assert-True ($workerPruned.Count -eq 1) 'Neto sobrevive quando o pai foi reativado pela dependência.'

    $fourFixture = $createFourDeep.Invoke($null, @())
    $fourRoot = Get-Prop (Get-Prop $fourFixture 'Snapshot') 'RootLevel'
    $fourSelection = $createDefault.Invoke($null, @($fourRoot))
    Assert-True (([int](Get-Prop $fourSelection 'MaxDepth')) -eq 4) 'FourDeep MaxDepth=4.'
    Assert-False ([bool](Get-Prop $fourSelection 'WarnUnvalidatedDepth')) 'Profundidade 4 não avisa após o smoke U15.'
    $fiveFixture = $createFiveDeep.Invoke($null, @())
    $fiveRoot = Get-Prop (Get-Prop $fiveFixture 'Snapshot') 'RootLevel'
    $fiveSelection = $createDefault.Invoke($null, @($fiveRoot))
    Assert-True (([int](Get-Prop $fiveSelection 'MaxDepth')) -eq 5) 'FiveDeep MaxDepth=5.'
    Assert-True ([bool](Get-Prop $fiveSelection 'WarnUnvalidatedDepth')) 'Profundidade 5 deve avisar.'
    $warningField = $selectionType.GetField('DepthWarningText', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    Assert-True ($null -ne $warningField) 'DepthWarningText deve existir.'
    Assert-Contains ([string]$warningField.GetValue($null)) 'Profundidade não validada' 'Texto canônico do aviso de profundidade.'
    Assert-Contains ([string]$warningField.GetValue($null)) 'até 4 níveis' 'O aviso deve citar a profundidade validada atual.'

    # --- Rename por GUID: Selected* da metadata antiga vira nome corrente ---
    $renameRootTemplate = Get-Prop (Get-Prop $byName['OneSublevel'] 'Snapshot') 'RootLevel'
    $renameLinesTemplate = @(Get-Prop $renameRootTemplate 'ChildLevels')[0]
    $lineQtyTemplate = @(Get-Prop $renameLinesTemplate 'Fields') | Where-Object { [string](Get-Prop $_ 'Name') -eq 'LineQty' } | Select-Object -First 1
    Assert-True ($null -ne $lineQtyTemplate) 'Fixture OneSublevel deve ter LineQty.'

    $persistedLineFields = @()
    foreach ($field in @(Get-Prop $renameLinesTemplate 'Fields')) {
        if ([string](Get-Prop $field 'Name') -eq 'LineQty') {
            $persistedLineFields += ,(New-LevelFieldFromTemplate -Template $field -Name 'OldLineQty')
        }
        else {
            $persistedLineFields += ,(New-LevelFieldFromTemplate -Template $field)
        }
    }

    $currentLineFields = @()
    foreach ($field in @(Get-Prop $renameLinesTemplate 'Fields')) {
        if ([string](Get-Prop $field 'Name') -eq 'LineQty') {
            $currentLineFields += ,(New-LevelFieldFromTemplate -Template $field -Name 'NewLineQty')
        }
        else {
            $currentLineFields += ,(New-LevelFieldFromTemplate -Template $field)
        }
    }

    $persistedSelected = @('OldLineQty')
    foreach ($field in @(Get-Prop $renameLinesTemplate 'Fields')) {
        $name = [string](Get-Prop $field 'Name')
        if ($name -ne 'LineQty') {
            $persistedSelected += $name
        }
    }

    $persistedLines = New-LevelFromTemplate -Template $renameLinesTemplate -Fields $persistedLineFields `
        -ChildLevels @() -SelectedCreate $persistedSelected -SelectedUpdate $persistedSelected -SelectedResponse $persistedSelected
    $persistedRoot = New-LevelFromTemplate -Template $renameRootTemplate -ChildLevels @($persistedLines) -KeepSelectedNull:$true
    $currentLines = New-LevelFromTemplate -Template $renameLinesTemplate -Fields $currentLineFields `
        -ChildLevels @() -KeepSelectedNull:$true
    $currentRoot = New-LevelFromTemplate -Template $renameRootTemplate -ChildLevels @($currentLines) -KeepSelectedNull:$true

    $renameSelection = $createDefault.Invoke($null, @($currentRoot))
    [void]$applyPersistedPruneMethod.Invoke($renameSelection, @($persistedRoot))
    $renameLinesNode = Find-NodeByLevelName $renameSelection 'Lines'
    Assert-True ($null -ne $renameLinesNode) 'Nó Lines deve existir após remap.'
    $renameLinesKey = [string](Get-Prop $renameLinesNode 'PathKey')
    $renameCreate = @([string[]]$getSelectedFieldsMethod.Invoke($renameSelection, @($renameLinesKey, 'CreateRequest')))
    Assert-True ($renameCreate -contains 'NewLineQty') 'Rename: SelectedCreate deve usar o nome corrente NewLineQty.'
    Assert-False ($renameCreate -contains 'OldLineQty') 'Rename: SelectedCreate não deve conservar o nome antigo OldLineQty.'
    Assert-False ($renameCreate -contains 'LineQty') 'Rename: nome intermediário LineQty não deve aparecer.'

    # --- ADDED em subnível: prune remove o default; IncludeAddedByGuid restaura ---
    $addedGuid = [guid]::NewGuid().ToString()
    $addedOrder = ([int](Get-Prop $lineQtyTemplate 'Order')) + 100
    $addedCtor = $fieldType.GetConstructors()[0]
    $addedField = $addedCtor.Invoke(@(
            $addedOrder,
            $addedGuid,
            'NewAttr',
            [string](Get-Prop $lineQtyTemplate 'DataType'),
            [int](Get-Prop $lineQtyTemplate 'Length'),
            [int](Get-Prop $lineQtyTemplate 'Decimals'),
            $false,
            [bool](Get-Prop $lineQtyTemplate 'IsNullable'),
            $false,
            $false,
            $false,
            $false,
            $false,
            $false
        ))

    $baseLineFields = @(@(Get-Prop $renameLinesTemplate 'Fields') | ForEach-Object { New-LevelFieldFromTemplate -Template $_ })
    $currentWithAddedFields = $baseLineFields + @($addedField)
    $persistedWithoutAddedNames = @($baseLineFields | ForEach-Object { [string](Get-Prop $_ 'Name') })

    $persistedLinesNoAdded = New-LevelFromTemplate -Template $renameLinesTemplate -Fields $baseLineFields `
        -ChildLevels @() -SelectedCreate $persistedWithoutAddedNames -SelectedUpdate $persistedWithoutAddedNames `
        -SelectedResponse $persistedWithoutAddedNames
    $persistedRootNoAdded = New-LevelFromTemplate -Template $renameRootTemplate -ChildLevels @($persistedLinesNoAdded) -KeepSelectedNull:$true
    $currentLinesWithAdded = New-LevelFromTemplate -Template $renameLinesTemplate -Fields $currentWithAddedFields `
        -ChildLevels @() -KeepSelectedNull:$true
    $currentRootWithAdded = New-LevelFromTemplate -Template $renameRootTemplate -ChildLevels @($currentLinesWithAdded) -KeepSelectedNull:$true

    $addedSelection = $createDefault.Invoke($null, @($currentRootWithAdded))
    $addedLinesNode = Find-NodeByLevelName $addedSelection 'Lines'
    $addedLinesKey = [string](Get-Prop $addedLinesNode 'PathKey')
    Assert-True ([bool]$isIncludedMethod.Invoke($addedSelection, @($addedLinesKey))) 'Lines permanece incluído após CreateDefault.'
    # CreateDefault já marca NewAttr; o prune sem ele deve remover.
    [void]$applyPersistedPruneMethod.Invoke($addedSelection, @($persistedRootNoAdded))
    $afterPruneCreate = @([string[]]$getSelectedFieldsMethod.Invoke($addedSelection, @($addedLinesKey, 'CreateRequest')))
    Assert-False ($afterPruneCreate -contains 'NewAttr') 'ADDED: prune sem o campo novo deve remover NewAttr do SelectedCreate.'

    $guidList = [string[]]@($addedGuid)
    [void]$includeAddedMethod.Invoke($addedSelection, @('CreateRequest', $guidList))
    [void]$includeAddedMethod.Invoke($addedSelection, @('UpdateRequest', $guidList))
    [void]$includeAddedMethod.Invoke($addedSelection, @('Response', $guidList))
    $afterIncludeCreate = @([string[]]$getSelectedFieldsMethod.Invoke($addedSelection, @($addedLinesKey, 'CreateRequest')))
    Assert-True ($afterIncludeCreate -contains 'NewAttr') 'ADDED: IncludeAddedFieldsByGuid deve marcar NewAttr em CreateRequest.'
    $prunedAdded = @(Get-Prop ($pruneMethod.Invoke($addedSelection, @())) 'ChildLevels')[0]
    Assert-True (@(Get-Prop $prunedAdded 'SelectedCreateFieldNames') -contains 'NewAttr') 'ADDED: Prune preserva NewAttr em SelectedCreate.'
    Assert-True (@(Get-Prop $prunedAdded 'SelectedUpdateFieldNames') -contains 'NewAttr') 'ADDED: Prune preserva NewAttr em SelectedUpdate.'
    Assert-True (@(Get-Prop $prunedAdded 'SelectedResponseFieldNames') -contains 'NewAttr') 'ADDED: Prune preserva NewAttr em SelectedResponse.'
    $unionAdded = @((Get-Prop $prunedAdded 'Fields') | ForEach-Object { [string](Get-Prop $_ 'Name') })
    Assert-True ($unionAdded -contains 'NewAttr') 'ADDED: Fields da poda inclui NewAttr.'

    Write-Output 'PASS: ApiPlanHierarchicalWizardSelection'
}
finally {
    if ($null -ne $script:AssemblyResolveHandler) {
        [System.AppDomain]::CurrentDomain.remove_AssemblyResolve($script:AssemblyResolveHandler)
    }
}
