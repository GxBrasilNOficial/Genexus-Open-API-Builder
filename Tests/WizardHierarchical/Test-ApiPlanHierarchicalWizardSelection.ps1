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
Assert-Contains $dialogSource 'HierarchicalSelection' 'O fluxo do Wizard deve persistir a seleção hierárquica.'
Assert-Contains $dialogSource '_levelCreateFieldsList' 'Listas de subnível devem ser distintas das do cabeçalho.'
Assert-Contains $builderSource 'ResolveHierarchicalLevels' 'ApiPlanBuilder deve podar Levels a partir da seleção.'
Assert-Contains $readerSource 'CreateFourDeepFixture' 'Fixture de profundidade 4 deve existir fora de CreateFixtures.'

$createFixturesBlock = [regex]::Match($readerSource, 'public static IReadOnlyList<TransactionStructureFixture> CreateFixtures\(\)[\s\S]*?return new\[\]')
Assert-True $createFixturesBlock.Success 'CreateFixtures deve existir.'
Assert-NotContains $createFixturesBlock.Value 'CreateFourDeepFixture' 'FourDeep não deve entrar no ouro B095.'

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

    $createFixtures = $readerType.GetMethod('CreateFixtures', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $createFourDeep = $readerType.GetMethod('CreateFourDeepFixture', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $createDefault = $selectionType.GetMethod('CreateDefault', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $hasSelectedMethod = Get-InstanceMethod $selectionType 'HasSelectedSublevels'
    $countSelectedMethod = Get-InstanceMethod $selectionType 'CountSelectedSublevels'
    $getIncludeCountMethod = Get-InstanceMethod $selectionType 'GetIncludeListCount'
    $setIncludeCountMethod = Get-InstanceMethod $selectionType 'SetIncludeListCount'
    $setFieldMethod = Get-InstanceMethod $selectionType 'SetFieldSelected'
    $setIncludedMethod = Get-InstanceMethod $selectionType 'SetLevelIncluded'
    $isIncludedMethod = Get-InstanceMethod $selectionType 'IsLevelIncluded'
    $pruneMethod = Get-InstanceMethod $selectionType 'Prune'

    Assert-True ($null -ne $createFixtures) 'CreateFixtures não encontrado.'
    Assert-True ($null -ne $createFourDeep) 'CreateFourDeepFixture não encontrado.'
    Assert-True ($null -ne $createDefault) 'CreateDefault não encontrado.'

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
    Assert-True ([bool](Get-Prop $fourSelection 'WarnUnvalidatedDepth')) 'Profundidade 4 deve avisar.'
    $warningField = $selectionType.GetField('DepthWarningText', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    Assert-True ($null -ne $warningField) 'DepthWarningText deve existir.'
    Assert-Contains ([string]$warningField.GetValue($null)) 'Profundidade não validada' 'Texto canônico do aviso de profundidade.'

    Write-Output 'PASS: ApiPlanHierarchicalWizardSelection'
}
finally {
    if ($null -ne $script:AssemblyResolveHandler) {
        [System.AppDomain]::CurrentDomain.remove_AssemblyResolve($script:AssemblyResolveHandler)
    }
}
