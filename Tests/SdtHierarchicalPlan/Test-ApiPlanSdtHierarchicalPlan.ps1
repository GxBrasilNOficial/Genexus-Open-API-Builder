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
$namingSourcePath = Join-Path $repositoryRoot 'Src\Domain\ApiPlanSdtHierarchicalNaming.cs'
$builderSourcePath = Join-Path $repositoryRoot 'Src\Domain\ApiPlanSdtGenerationPlan.cs'
$domainSourcePath = Join-Path $repositoryRoot 'Src\Domain\ApiPlan.cs'
$contractSourcePath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\PrototypeWizardContract.cs'

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

function Assert-NotContains {
    param([string]$Text, [string]$Needle, [string]$Message)
    if ($Text.IndexOf($Needle, [StringComparison]::Ordinal) -ge 0) {
        throw "ASSERT_NOT_CONTAINS_FAILED: $Message"
    }
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

function Get-Prop {
    param($Object, [string]$Name)
    $property = $Object.GetType().GetProperty($Name, [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    Assert-True ($null -ne $property) "Propriedade ausente: $Name"
    return $property.GetValue($Object)
}

function Find-OwnSdt {
    param($PlanJson, [string]$Name)
    foreach ($sdt in @($PlanJson.ownSdts)) {
        if ([string]$sdt.name -eq $Name) {
            return $sdt
        }
    }

    return $null
}

function Find-Member {
    param($Sdt, [string]$Name)
    foreach ($member in @($Sdt.members)) {
        if ([string]$member.name -eq $Name) {
            return $member
        }
    }

    return $null
}

function Get-OwnSdtIndex {
    param($PlanJson, [string]$Name)
    $index = 0
    foreach ($sdt in @($PlanJson.ownSdts)) {
        if ([string]$sdt.name -eq $Name) {
            return $index
        }

        $index += 1
    }

    return -1
}

Assert-True (Test-Path -LiteralPath $namingSourcePath -PathType Leaf) "Fonte ausente: $namingSourcePath"
Assert-True (Test-Path -LiteralPath $builderSourcePath -PathType Leaf) "Fonte ausente: $builderSourcePath"
Assert-True (Test-Path -LiteralPath $domainSourcePath -PathType Leaf) "Fonte ausente: $domainSourcePath"
Assert-True (Test-Path -LiteralPath $contractSourcePath -PathType Leaf) "Fonte ausente: $contractSourcePath"

$namingSource = [IO.File]::ReadAllText($namingSourcePath)
$builderSource = [IO.File]::ReadAllText($builderSourcePath)
$domainSource = [IO.File]::ReadAllText($domainSourcePath)
$contractSource = [IO.File]::ReadAllText($contractSourcePath)

Assert-Contains $namingSource 'GeneXusObjectNameMaxLength = 128' 'Limite de nome GeneXus 18 deve ser 128.'
Assert-Contains $namingSource 'membro não tem teto nesta fase' '128 não se aplica a nome de membro nesta fase.'
Assert-Contains $namingSource 'Gatilho: nome completo' 'O gatilho do encurtamento deve ser nome completo ou colisão, não folha > 32.'
Assert-Contains $namingSource '_API_CreateRequest_' 'Padrao de SDT Create derivado deve existir no helper.'
Assert-Contains $namingSource '_API_UpdateRequest_' 'Padrao de SDT Update derivado deve existir no helper.'
Assert-Contains $namingSource '_API_Response_' 'Padrao de SDT Response derivado deve existir no helper.'
Assert-Contains $builderSource 'HasSelectedSublevels' 'Builder deve ramificar no caminho hierarquico.'
Assert-Contains $builderSource 'ListResponse_Item' 'ListResponse_Item fica explicitamente fora de B096.'
Assert-Contains $domainSource 'O plano de SDT consome Levels' 'ApiPlan deve declarar consumo B096 do Levels.'
Assert-NotContains $contractSource 'ApiPlanSdtHierarchicalNaming' 'Wizard flat nao deve acoplar o naming B096.'

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
    $baselineType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanSdtHierarchicalPlanBaseline', $true, $false)
    $fixtureType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanSdtHierarchicalPlanFixture', $true, $false)

    $createFixtures = $baselineType.GetMethod('CreateFixtures', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $capture = $baselineType.GetMethod('Capture', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $normalize = $baselineType.GetMethod('NormalizeForComparison', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $assertUnresolvable = $baselineType.GetMethod('AssertUnresolvableMemberCollisionThrows', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $measureShortened = $baselineType.GetMethod('MeasureShortenedSdtNameLength', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $nameProperty = $fixtureType.GetProperty('Name', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $planProperty = $fixtureType.GetProperty('Plan', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')

    Assert-True ($null -ne $createFixtures) 'CreateFixtures nao encontrado.'
    Assert-True ($null -ne $capture) 'Capture nao encontrado.'
    Assert-True ($null -ne $normalize) 'NormalizeForComparison nao encontrado.'
    Assert-True ($null -ne $assertUnresolvable) 'AssertUnresolvableMemberCollisionThrows nao encontrado.'
    Assert-True ($null -ne $measureShortened) 'MeasureShortenedSdtNameLength nao encontrado.'

    $assertUnresolvable.Invoke($null, @())
    $shortenedLength = [int]$measureShortened.Invoke($null, @())
    Assert-True ($shortenedLength -le 128) "Nome encurtado deve caber em 128; obtido $shortenedLength."
    Assert-True ($shortenedLength -gt 0) 'Nome encurtado nao pode ser vazio.'

    $fixtures = @($createFixtures.Invoke($null, @()))
    $expectedNames = @(
        'OneSublevel',
        'ParallelSublevels',
        'ThreeDeep',
        'InheritedPrimaryKey',
        'MemberCollision',
        'LongQualifier',
        'HeaderOnly'
    )
    Assert-True ($fixtures.Count -eq $expectedNames.Count) "Esperava $($expectedNames.Count) fixtures; encontrado $($fixtures.Count)."
    $actualNames = @($fixtures | ForEach-Object { [string]$nameProperty.GetValue($_) })
    foreach ($expectedName in $expectedNames) {
        Assert-True ($actualNames -contains $expectedName) "Fixture ausente: $expectedName"
    }

    $captured = @{}
    foreach ($fixture in $fixtures) {
        $fixtureName = [string]$nameProperty.GetValue($fixture)
        $plan = $planProperty.GetValue($fixture)
        $jsonText = [string]$capture.Invoke($null, @($plan))
        $captured[$fixtureName] = $jsonText
    }

    $one = $captured['OneSublevel'] | ConvertFrom-Json
    $linesCreate = Find-OwnSdt $one 'sdtOrder_API_CreateRequest_Lines'
    $linesUpdate = Find-OwnSdt $one 'sdtOrder_API_UpdateRequest_Lines'
    $linesResponse = Find-OwnSdt $one 'sdtOrder_API_Response_Lines'
    $headerCreate = Find-OwnSdt $one 'sdtOrder_API_CreateRequest'
    $headerUpdate = Find-OwnSdt $one 'sdtOrder_API_UpdateRequest'
    $listResponse = Find-OwnSdt $one 'sdtOrder_API_ListResponse'
    Assert-True ($null -ne $linesCreate) 'OneSublevel deve emitir SDT Create das linhas.'
    Assert-True ($null -ne $linesUpdate) 'OneSublevel deve emitir SDT Update das linhas.'
    Assert-True ($null -ne $linesResponse) 'OneSublevel deve emitir SDT Response das linhas.'
    Assert-True ($null -ne (Find-Member $linesCreate 'LineId')) 'LineId informado entra no Create da linha.'
    Assert-True ($null -ne (Find-Member $linesCreate 'LineQty')) 'LineQty entra no Create da linha.'
    Assert-True ($null -eq (Find-Member $linesCreate 'LineTotal')) 'Formula nao entra no Create da linha.'
    Assert-True ($null -eq (Find-Member $linesCreate 'LineStamp')) 'NoAccept nao entra no Create da linha.'
    Assert-True ($null -ne (Find-Member $linesResponse 'LineTotal')) 'Formula entra no Response da linha.'
    Assert-True ($null -ne (Find-Member $linesResponse 'LineStamp')) 'NoAccept entra no Response da linha.'
    Assert-True ($null -ne (Find-Member $headerUpdate 'LinesReplace')) 'Update do cabecalho leva LinesReplace.'
    Assert-True ([bool](Find-Member $headerCreate 'Lines').isCollection) 'Create do cabecalho leva colecao Lines.'
    $items = Find-Member $listResponse 'Items'
    Assert-True ([string]$items.collectionItemType -eq 'sdtOrder_API_Response') 'ListResponse.Items permanece colecao de Response em B096.'
    Assert-True ((Get-OwnSdtIndex $one 'sdtOrder_API_CreateRequest_Lines') -lt (Get-OwnSdtIndex $one 'sdtOrder_API_CreateRequest')) 'Create das linhas em pos-ordem, antes do cabecalho.'

    $three = $captured['ThreeDeep'] | ConvertFrom-Json
    Assert-True ($null -ne (Find-OwnSdt $three 'sdtDay_API_CreateRequest_Shift_Worker')) 'Profundidade 3 acumula o caminho no qualificador.'
    $shiftUpdate = Find-OwnSdt $three 'sdtDay_API_UpdateRequest_Shift'
    Assert-True ($null -ne (Find-Member $shiftUpdate 'WorkerReplace')) 'Replace do neto fica dentro do item do pai.'
    $workerCreate = Find-OwnSdt $three 'sdtDay_API_CreateRequest_Worker'
    Assert-True ($null -eq $workerCreate) 'Qualificador do neto nao pode omitir o pai.'
    $workerCreateFull = Find-OwnSdt $three 'sdtDay_API_CreateRequest_Shift_Worker'
    Assert-True ($null -eq (Find-Member $workerCreateFull 'WorkerId')) 'PK autonumerada nao entra no Create da linha.'
    Assert-True ($null -ne (Find-Member $workerCreateFull 'WorkerName')) 'WorkerName entra no Create da linha.'
    Assert-True ($null -eq (Find-Member $workerCreateFull 'WorkerScore')) 'Formula nao entra no Create da linha.'

    $inherited = $captured['InheritedPrimaryKey'] | ConvertFrom-Json
    $unnamedCreate = Find-OwnSdt $inherited 'sdtHeader_API_CreateRequest_Level1'
    Assert-True ($null -ne $unnamedCreate) 'Nivel sem nome vira Level1 no SDT.'
    Assert-True ($null -eq (Find-Member $unnamedCreate 'HeaderId')) 'PK herdada (FK) nao entra no Create da linha.'
    Assert-True ($null -ne (Find-Member $unnamedCreate 'LineId')) 'Parte propria da PK informada entra no Create.'

    $collision = $captured['MemberCollision'] | ConvertFrom-Json
    $collisionCreate = Find-OwnSdt $collision 'sdtCollisionDoc_API_CreateRequest'
    Assert-True ($null -ne (Find-Member $collisionCreate 'Notes')) 'Campo Notes do cabecalho permanece.'
    Assert-True ($null -ne (Find-Member $collisionCreate 'Notes1')) 'Colecao colidente vira Notes1.'
    Assert-True ([bool](Find-Member $collisionCreate 'Notes1').isCollection) 'Notes1 e colecao.'

    $headerOnly = $captured['HeaderOnly'] | ConvertFrom-Json
    Assert-True (@($headerOnly.ownSdts).Count -eq 5) 'Cabecalho sem filhos permanece nos 5 SDTs planos.'
    foreach ($sdt in @($headerOnly.ownSdts)) {
        Assert-True ([string]$sdt.backlogId -ne 'B096') 'HeaderOnly nao deve emitir backlog B096.'
    }

    $long = $captured['LongQualifier'] | ConvertFrom-Json
    foreach ($sdt in @($long.ownSdts)) {
        $sdtName = [string]$sdt.name
        Assert-True ($sdtName.Length -le 128) "SDT '$sdtName' estoura 128."
        Assert-True ($sdtName.Length -gt 0) "SDT hierarquico sem nome."
        if ([string]$sdt.backlogId -eq 'B096') {
            Assert-True ($sdtName.IndexOf('LongTx', [StringComparison]::Ordinal) -ge 0) "Neste fixture o fragmento LongTx permanece no SDT encurtado: $sdtName"
        }
    }

    $divergences = [System.Collections.Generic.List[string]]::new()
    foreach ($fixture in $fixtures) {
        $fixtureName = [string]$nameProperty.GetValue($fixture)
        $actual = [string]$normalize.Invoke($null, @($captured[$fixtureName]))
        if (-not $actual.EndsWith("`n")) {
            $actual += "`n"
        }

        $referencePath = Join-Path $baselinesRoot ($fixtureName + '.json')
        if ($UpdateBaselines) {
            Write-Utf8LfFile -Path $referencePath -Content $actual
            continue
        }

        if (-not (Test-Path -LiteralPath $referencePath -PathType Leaf)) {
            $divergences.Add("Arquivo de referencia ausente: $referencePath")
            continue
        }

        $expected = [System.IO.File]::ReadAllText($referencePath).Replace("`r`n", "`n").Replace("`r", "`n")
        if (-not $expected.EndsWith("`n")) {
            $expected += "`n"
        }

        if ($actual -cne $expected) {
            $divergences.Add("Divergencia em $fixtureName")
        }
    }

    if ($UpdateBaselines) {
        Write-Output "UPDATED: ouro B096 gravado em $baselinesRoot"
        exit 0
    }

    if ($divergences.Count -gt 0) {
        throw ("ASSERT_BASELINE_FAILED:`n" + ($divergences -join "`n"))
    }

    Write-Output 'PASS: ApiPlanSdtHierarchicalPlan'
}
finally {
    if ($null -ne $script:AssemblyResolveHandler) {
        [System.AppDomain]::CurrentDomain.remove_AssemblyResolve($script:AssemblyResolveHandler)
    }
}
