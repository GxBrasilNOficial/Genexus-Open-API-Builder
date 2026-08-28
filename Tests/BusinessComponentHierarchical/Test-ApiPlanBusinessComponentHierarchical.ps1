#requires -Version 7.4

[CmdletBinding()]
param(
    [switch]$UpdateBaselines,
    [string]$DllPath = '',
    [string]$GeneXusDirectory = 'C:\Program Files (x86)\GeneXus\GeneXus18'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$baselinesRoot = Join-Path $PSScriptRoot 'Baselines'
$writerSourcePath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\ApiPlanBusinessComponentWriter.cs'
$hierarchicalSourcePath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\ApiPlanBusinessComponentHierarchicalSource.cs'
$mapSourcePath = Join-Path $repositoryRoot 'Src\Domain\ApiPlanHierarchicalContractMap.cs'

if ([string]::IsNullOrWhiteSpace($DllPath)) {
    $DllPath = Join-Path $repositoryRoot 'Src\Extension\bin\Release\net471\GenexusOpenApiBuilder.Extension.dll'
}

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw "ASSERT_TRUE_FAILED: $Message"
    }
}

function Assert-Contains {
    param([string]$Text, [string]$Needle, [string]$Message)
    if ($Text.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "ASSERT_CONTAINS_FAILED: $Message"
    }
}

function Get-BcVariableTypes {
    param($Method, $Plan)

    $map = @{}
    foreach ($item in @($Method.Invoke($null, @($Plan)))) {
        $nameProperty = $item.GetType().GetProperty('Name')
        $typeProperty = $item.GetType().GetProperty('DataType')
        $name = [string]$nameProperty.GetValue($item)
        $map[$name] = [string]$typeProperty.GetValue($item)
    }

    return ,$map
}

function Write-Utf8LfFile {
    param(
        [Parameter(Mandatory)] [string]$Path,
        [Parameter(Mandatory)] [string]$Content
    )

    $directory = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $directory -PathType Container)) {
        [void][System.IO.Directory]::CreateDirectory($directory)
    }

    $normalized = $Content.Replace("`r`n", "`n").Replace("`r", "`n")
    if (-not $normalized.EndsWith("`n")) {
        $normalized += "`n"
    }

    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $normalized, $encoding)
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

Assert-True (Test-Path -LiteralPath $writerSourcePath -PathType Leaf) "Fonte ausente: $writerSourcePath"
Assert-True (Test-Path -LiteralPath $hierarchicalSourcePath -PathType Leaf) "Fonte ausente: $hierarchicalSourcePath"
Assert-True (Test-Path -LiteralPath $mapSourcePath -PathType Leaf) "Fonte ausente: $mapSourcePath"

$writerSource = [IO.File]::ReadAllText($writerSourcePath)
$hierarchicalSource = [IO.File]::ReadAllText($hierarchicalSourcePath)
$mapSource = [IO.File]::ReadAllText($mapSourcePath)

Assert-Contains $writerSource 'HasSelectedSublevels' 'Writer BC deve ramificar no caminho hierarquico.'
Assert-Contains $writerSource 'EmitCreateCollectionAssignments' 'Create deve emitir colecoes hierarquicas.'
Assert-Contains $writerSource 'EmitUpdateCollectionAssignments' 'Update deve emitir Replace hierarquico.'
Assert-Contains $writerSource 'EmitGetCollectionAssignments' 'Get deve emitir colecoes hierarquicas.'
Assert-Contains $hierarchicalSource 'ReplaceMemberName' 'Emissor deve respeitar o marcador Replace.'
Assert-Contains $hierarchicalSource 'HasAutonumberPrimaryKey' 'Update autonumerado usa Clear+Add.'
Assert-Contains $hierarchicalSource 'ShouldAssignFieldToBc' 'Update deve omitir PK autonumerada na atribuicao ao BC.'
Assert-Contains $hierarchicalSource 'node.BcLevelType' 'Variavel BC deve usar o tipo com caminho completo do nivel.'
Assert-Contains $mapSource 'ApiPlanHierarchicalContractMapBuilder' 'Mapa compartilhado B096/B097 deve existir.'
Assert-Contains $mapSource 'BuildBcLevelType' 'Mapa deve montar o tipo BC aninhado.'
Assert-Contains $mapSource 'AllocateVariableToken' 'Mapa deve reservar VariableToken.'
Assert-Contains $mapSource 'subnivel sem nome estrutural' 'Mapa BC deve recusar subnivel sem nome na Transaction.'
Assert-Contains $mapSource '_V' 'Colisao de VariableToken deve desambiguar com sufixo _V.'
Assert-True ($hierarchicalSource.IndexOf('plan.TransactionName + "." + node.BcCollectionName', [StringComparison]::Ordinal) -lt 0) 'Tipo BC nao pode mais ser Transaction.Folha sem ancestrais.'

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
    $baselineType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanBusinessComponentHierarchicalBaseline', $true, $false)
    $fixtureType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanSdtHierarchicalPlanFixture', $true, $false)
    $snapshotType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanBusinessComponentHierarchicalSnapshot', $true, $false)

    $createFixtures = $baselineType.GetMethod('CreateFixtures', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $capture = $baselineType.GetMethod('Capture', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $normalize = $baselineType.GetMethod('NormalizeForComparison', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $assertMap = $baselineType.GetMethod('AssertMapMatchesSdtPlan', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $toFileMap = $snapshotType.GetMethod('ToFileMap', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $nameProperty = $fixtureType.GetProperty('Name', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $planProperty = $fixtureType.GetProperty('Plan', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $sourceType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanBusinessComponentHierarchicalSource', $true, $false)
    $collectGet = $sourceType.GetMethod('CollectGetVariables', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $mapBuilderType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Domain.ApiPlanHierarchicalContractMapBuilder', $true, $false)
    $allocateToken = $mapBuilderType.GetMethod('AllocateVariableToken', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $buildToken = $mapBuilderType.GetMethod('BuildVariableToken', [System.Reflection.BindingFlags]'Static, NonPublic, Public')

    Assert-True ($null -ne $createFixtures) 'CreateFixtures nao encontrado.'
    Assert-True ($null -ne $capture) 'Capture nao encontrado.'
    Assert-True ($null -ne $normalize) 'NormalizeForComparison nao encontrado.'
    Assert-True ($null -ne $assertMap) 'AssertMapMatchesSdtPlan nao encontrado.'
    Assert-True ($null -ne $toFileMap) 'ToFileMap nao encontrado.'
    Assert-True ($null -ne $collectGet) 'CollectGetVariables nao encontrado.'
    Assert-True ($null -ne $allocateToken) 'AllocateVariableToken nao encontrado.'
    Assert-True ($null -ne $buildToken) 'BuildVariableToken nao encontrado.'

    $reservedTokens = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $firstToken = [string]$allocateToken.Invoke($null, @([string[]]@(), 'Notes', 1, $reservedTokens))
    Assert-True ($firstToken -eq 'Notes') "Primeiro VariableToken curto deve ser Notes; obtido '$firstToken'."
    $secondToken = [string]$allocateToken.Invoke($null, @([string[]]@(), 'Notes', 1, $reservedTokens))
    Assert-True ($secondToken -eq 'Notes_V2') "Colisao de VariableToken deve virar Notes_V2; obtido '$secondToken'."
    $thirdToken = [string]$allocateToken.Invoke($null, @([string[]]@(), 'Notes', 1, $reservedTokens))
    Assert-True ($thirdToken -eq 'Notes_V3') "Segunda colisao de VariableToken deve virar Notes_V3; obtido '$thirdToken'."

    $longAncestors = [string[]]@(
        'AAAAAAAAAAAAAAA',
        'BBBBBBBBBBBBBBB',
        'CCCCCCCCCCCCCCC'
    )
    $longJoined = ($longAncestors + 'LeafLevel') -join '_'
    Assert-True ($longJoined.Length -gt 48) "Fixture de path longo deve exceder 48; length=$($longJoined.Length)."
    $longToken = [string]$buildToken.Invoke($null, @($longAncestors, 'LeafLevel', 7))
    Assert-True ($longToken -eq 'L7_LeafLevel') "Path longo deve encurtar para L{order}_folha; obtido '$longToken'."
    $reservedLong = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    [void]$reservedLong.Add('L7_LeafLevel')
    $longDisambiguated = [string]$allocateToken.Invoke($null, @($longAncestors, 'LeafLevel', 7, $reservedLong))
    Assert-True ($longDisambiguated -eq 'L7_LeafLevel_V2') "Colisao no token encurtado deve virar L7_LeafLevel_V2; obtido '$longDisambiguated'."

    $fixtures = @($createFixtures.Invoke($null, @()))
    Assert-True ($fixtures.Count -ge 5) "Esperava pelo menos 5 fixtures; encontrado $($fixtures.Count)."

    $expectedNames = @('OneSublevel', 'ParallelSublevels', 'ThreeDeep', 'InheritedPrimaryKey', 'MemberCollision', 'VariableTokenCollision', 'HeaderOnly')
    $actualNames = @($fixtures | ForEach-Object { $nameProperty.GetValue($_) })
    foreach ($expectedName in $expectedNames) {
        Assert-True ($actualNames -contains $expectedName) "Fixture ausente: $expectedName"
    }

    $divergences = [System.Collections.Generic.List[string]]::new()
    foreach ($fixture in $fixtures) {
        $fixtureName = [string]$nameProperty.GetValue($fixture)
        $plan = $planProperty.GetValue($fixture)
        $assertMap.Invoke($null, @($plan))
        $snapshot = $capture.Invoke($null, @($plan))
        $fileMap = $toFileMap.Invoke($snapshot, @())
        $fixtureDirectory = Join-Path $baselinesRoot $fixtureName

        $getSource = [string]$normalize.Invoke($null, @([string]($snapshot.GetType().GetProperty('GetSource').GetValue($snapshot))))
        $createSource = [string]$normalize.Invoke($null, @([string]($snapshot.GetType().GetProperty('CreateSource').GetValue($snapshot))))
        $updateSource = [string]$normalize.Invoke($null, @([string]($snapshot.GetType().GetProperty('UpdateSource').GetValue($snapshot))))

        if ($fixtureName -eq 'HeaderOnly') {
            Assert-True ($getSource.IndexOf('For &Bc_', [StringComparison]::Ordinal) -lt 0) 'HeaderOnly nao deve emitir For hierarquico no Get.'
            Assert-True ($updateSource.IndexOf('Replace', [StringComparison]::Ordinal) -lt 0) 'HeaderOnly nao deve emitir Replace.'
        }
        else {
            Assert-True ($getSource.IndexOf('For &Bc_', [StringComparison]::Ordinal) -ge 0) "$fixtureName Get deve iterar colecoes BC."
            Assert-True ($createSource.IndexOf('For &Create_', [StringComparison]::Ordinal) -ge 0) "$fixtureName Create deve iterar request."
            Assert-True ($updateSource.IndexOf('Replace', [StringComparison]::Ordinal) -ge 0) "$fixtureName Update deve condicionar Replace."
        }

        if ($fixtureName -eq 'ThreeDeep') {
            Assert-True ($updateSource.IndexOf('ShiftReplace', [StringComparison]::Ordinal) -ge 0) 'ThreeDeep deve emitir ShiftReplace.'
            Assert-True ($updateSource.IndexOf('WorkerReplace', [StringComparison]::Ordinal) -ge 0) 'ThreeDeep deve emitir WorkerReplace aninhado.'
            Assert-True ($updateSource.IndexOf('.Clear()', [StringComparison]::Ordinal) -ge 0) 'Worker autonumerado deve usar Clear no Replace.'
            Assert-True ($createSource.IndexOf('&Bc_Shift_Worker.WorkerId =', [StringComparison]::Ordinal) -lt 0) 'Create nao deve atribuir PK autonumerada WorkerId.'
            Assert-True ($updateSource.IndexOf('&Bc_Shift_Worker.WorkerId =', [StringComparison]::Ordinal) -lt 0) 'Update Clear+Add nao deve atribuir PK autonumerada WorkerId.'
            $bcTypes = Get-BcVariableTypes -Method $collectGet -Plan $plan
            Assert-True ($bcTypes.ContainsKey('Bc_Shift')) 'ThreeDeep deve declarar &Bc_Shift.'
            Assert-True ($bcTypes.ContainsKey('Bc_Shift_Worker')) 'ThreeDeep deve declarar &Bc_Shift_Worker.'
            Assert-True ($bcTypes['Bc_Shift'] -eq 'Day.Shift') 'Filho direto usa Transaction.Nivel.'
            Assert-True ($bcTypes['Bc_Shift_Worker'] -eq 'Day.Shift.Worker') 'Neto usa caminho completo Transaction.Pai.Neto.'
            Assert-True ($bcTypes['Bc_Shift_Worker'] -ne 'Day.Worker') 'Neto nao pode ser Transaction.Folha.'
        }

        if ($fixtureName -eq 'OneSublevel') {
            $bcTypes = Get-BcVariableTypes -Method $collectGet -Plan $plan
            Assert-True ($bcTypes.ContainsKey('Bc_Lines')) 'OneSublevel deve declarar &Bc_Lines.'
            Assert-True ($bcTypes['Bc_Lines'] -eq 'Order.Lines') 'Filho direto OneSublevel permanece Transaction.Nivel.'
        }

        if ($fixtureName -eq 'ParallelSublevels') {
            $bcTypes = Get-BcVariableTypes -Method $collectGet -Plan $plan
            Assert-True ($bcTypes.ContainsKey('Bc_Notes')) 'ParallelSublevels deve declarar &Bc_Notes.'
            Assert-True ($bcTypes.ContainsKey('Bc_Tags')) 'ParallelSublevels deve declarar &Bc_Tags.'
            Assert-True ($bcTypes['Bc_Notes'] -eq 'Document.Notes') 'Paralelo Notes permanece Transaction.Nivel.'
            Assert-True ($bcTypes['Bc_Tags'] -eq 'Document.Tags') 'Paralelo Tags permanece Transaction.Nivel.'
        }

        if ($fixtureName -eq 'InheritedPrimaryKey') {
            Assert-True ($getSource.IndexOf('.Line', [StringComparison]::Ordinal) -ge 0) 'InheritedPrimaryKey deve usar nome estrutural Line no BC.'
            Assert-True ($getSource.IndexOf('<unnamed>', [StringComparison]::Ordinal) -lt 0) 'InheritedPrimaryKey nao deve emitir colecao BC <unnamed>.'
        }

        if ($fixtureName -eq 'MemberCollision') {
            Assert-True ($createSource.IndexOf('Notes1', [StringComparison]::Ordinal) -ge 0) 'MemberCollision Create deve usar membro SDT desambiguado Notes1.'
            Assert-True ($createSource.IndexOf('.Notes.Add', [StringComparison]::Ordinal) -ge 0) 'MemberCollision Create deve Add no BC estrutural Notes.'
        }

        if ($fixtureName -eq 'VariableTokenCollision') {
            $mapCreate = $mapBuilderType.GetMethod('Create', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
            Assert-True ($null -ne $mapCreate) 'ApiPlanHierarchicalContractMapBuilder.Create ausente.'
            $map = $mapCreate.Invoke($null, @($plan))
            $createTree = $map.GetType().GetProperty('CreateRequest').GetValue($map)
            $branchNodes = @($createTree.GetType().GetProperty('Children').GetValue($createTree))
            Assert-True ($branchNodes.Count -eq 2) 'VariableTokenCollision deve ter dois ramos no mapa Create.'
            $leafTokens = foreach ($branch in $branchNodes) {
                $leaves = @($branch.GetType().GetProperty('Children').GetValue($branch))
                Assert-True ($leaves.Count -eq 1) 'Cada ramo deve ter uma folha SameLeaf.'
                [string]$leaves[0].GetType().GetProperty('VariableToken').GetValue($leaves[0])
            }
            Assert-True ($leafTokens -contains 'L1_SameLeaf') 'Primeira rota truncada deve reservar L1_SameLeaf.'
            Assert-True ($leafTokens -contains 'L1_SameLeaf_V2') 'Segunda rota colidente deve desambiguar para L1_SameLeaf_V2.'
            Assert-True ($createSource.IndexOf('&Bc_L1_SameLeaf', [StringComparison]::Ordinal) -ge 0) 'Create deve emitir &Bc_L1_SameLeaf.'
            Assert-True ($createSource.IndexOf('&Bc_L1_SameLeaf_V2', [StringComparison]::Ordinal) -ge 0) 'Create deve emitir &Bc_L1_SameLeaf_V2.'
            Assert-True ($createSource.IndexOf('&Create_L1_SameLeaf', [StringComparison]::Ordinal) -ge 0) 'Create deve emitir &Create_L1_SameLeaf.'
            Assert-True ($createSource.IndexOf('&Create_L1_SameLeaf_V2', [StringComparison]::Ordinal) -ge 0) 'Create deve emitir &Create_L1_SameLeaf_V2.'
            $bcTypes = Get-BcVariableTypes -Method $collectGet -Plan $plan
            Assert-True ($bcTypes.ContainsKey('Bc_L1_SameLeaf')) 'Get deve declarar &Bc_L1_SameLeaf.'
            Assert-True ($bcTypes.ContainsKey('Bc_L1_SameLeaf_V2')) 'Get deve declarar &Bc_L1_SameLeaf_V2.'
            Assert-True ($bcTypes['Bc_L1_SameLeaf'] -ne $bcTypes['Bc_L1_SameLeaf_V2']) 'Tokens desambiguados devem mapear tipos BC distintos.'
        }

        if ($UpdateBaselines) {
            foreach ($entry in $fileMap.GetEnumerator()) {
                $target = Join-Path $fixtureDirectory $entry.Key
                $content = [string]$normalize.Invoke($null, @([string]$entry.Value))
                Write-Utf8LfFile -Path $target -Content $content
            }

            continue
        }

        if (-not (Test-Path -LiteralPath $fixtureDirectory -PathType Container)) {
            $divergences.Add("Diretorio de referencia ausente: $fixtureDirectory")
            continue
        }

        foreach ($entry in $fileMap.GetEnumerator()) {
            $referencePath = Join-Path $fixtureDirectory $entry.Key
            if (-not (Test-Path -LiteralPath $referencePath -PathType Leaf)) {
                $divergences.Add("Arquivo de referencia ausente: $referencePath")
                continue
            }

            $actual = [string]$normalize.Invoke($null, @([string]$entry.Value))
            if (-not $actual.EndsWith("`n")) {
                $actual += "`n"
            }

            $expected = [System.IO.File]::ReadAllText($referencePath).Replace("`r`n", "`n").Replace("`r", "`n")
            if (-not $expected.EndsWith("`n")) {
                $expected += "`n"
            }

            if ($actual -cne $expected) {
                $divergences.Add("Divergencia em $fixtureName/$($entry.Key)")
            }
        }
    }

    if ($UpdateBaselines) {
        Write-Output "UPDATED: linha de base B097 gravada em $baselinesRoot"
        exit 0
    }

    if ($divergences.Count -gt 0) {
        throw ("ASSERT_BASELINE_FAILED:`n" + ($divergences -join "`n"))
    }

    Write-Output 'PASS: ApiPlanBusinessComponentHierarchicalBaseline'
}
finally {
    if ($null -ne $script:AssemblyResolveHandler) {
        [System.AppDomain]::CurrentDomain.remove_AssemblyResolve($script:AssemblyResolveHandler)
    }
}
