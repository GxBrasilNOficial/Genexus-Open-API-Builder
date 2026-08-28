#requires -Version 7.4

[CmdletBinding()]
param(
    [string]$DllPath = '',
    [string]$GeneXusDirectory = 'C:\Program Files (x86)\GeneXus\GeneXus18'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($DllPath)) {
    $DllPath = Join-Path $repositoryRoot 'Src\Extension\bin\Release\net471\GenexusOpenApiBuilder.Extension.dll'
}

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw "ASSERT_TRUE_FAILED: $Message" }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Message)
    if ($Expected -ne $Actual) {
        throw "ASSERT_EQUAL_FAILED: $Message (expected='$Expected' actual='$Actual')"
    }
}

function Assert-Contains {
    param([string]$Text, [string]$Needle, [string]$Message)
    if ($Text.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "ASSERT_CONTAINS_FAILED: $Message"
    }
}

function Get-Prop {
    param($Object, [string]$Name)
    $property = $Object.GetType().GetProperty($Name, [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    Assert-True ($null -ne $property) "Propriedade ausente: $Name"
    return $property.GetValue($Object)
}

function Get-Count {
    param($Object)
    $count = 0
    foreach ($item in @($Object)) {
        $count++
    }
    return $count
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
        if ($script:AssemblyResolveBusy) { return $null }
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

$writerPath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\ApiPlanMetadataFileWriter.cs'
$codecPath = Join-Path $repositoryRoot 'Src\Extension\Diagnostics\ApiPlanMetadataLevelsCodec.cs'
Assert-Contains ([IO.File]::ReadAllText($writerPath)) 'GOAB_API_METADATA_B060_V2' 'SchemaVersion de gravação deve ser V2.'
Assert-Contains ([IO.File]::ReadAllText($writerPath)) 'SchemaVersionV1' 'Constante V1 deve existir para leitura tolerante.'
Assert-Contains ([IO.File]::ReadAllText($writerPath)) 'CreateLevelsToken' 'Metadata deve serializar levels.'
Assert-Contains ([IO.File]::ReadAllText($writerPath)) 'ApiPlanGeneratedApiRemovalInventory.BuildOwnSdtNamesForRemoval' 'Metadata deve inventariar SDTs próprios para remoção.'
Assert-Contains ([IO.File]::ReadAllText($codecPath)) 'HasHierarchicalLevels' 'Codec deve detectar árvore hierárquica.'
Assert-Contains ([IO.File]::ReadAllText($codecPath)) 'FlattenToSyncSnapshots' 'Codec deve achatar árvore para diff de Sync.'

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
    $writerType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataFileWriter', $true, $false)
    $codecType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataLevelsCodec', $true, $false)
    $readerType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.TransactionStructureReader', $true, $false)
    $selectionType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Domain.ApiPlanHierarchicalWizardSelection', $true, $false)
    Assert-True ($null -ne $writerType) 'ApiPlanMetadataFileWriter não encontrado.'
    Assert-True ($null -ne $codecType) 'ApiPlanMetadataLevelsCodec não encontrado.'
    Assert-True ($null -ne $readerType) 'TransactionStructureReader não encontrado.'
    Assert-True ($null -ne $selectionType) 'ApiPlanHierarchicalWizardSelection não encontrado.'

    $schema = $writerType.GetField('SchemaVersion', [System.Reflection.BindingFlags]'Static, NonPublic, Public').GetValue($null)
    $schemaV1 = $writerType.GetField('SchemaVersionV1', [System.Reflection.BindingFlags]'Static, NonPublic, Public').GetValue($null)
    Assert-Equal 'GOAB_API_METADATA_B060_V2' $schema 'SchemaVersion gravado'
    Assert-Equal 'GOAB_API_METADATA_B060_V1' $schemaV1 'SchemaVersionV1'

    $isSupported = $writerType.GetMethod('IsSupportedSchemaVersion', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $supportedArgs = New-Object object[] 1
    $supportedArgs[0] = [string]$schema
    Assert-True ([bool]$isSupported.Invoke($null, $supportedArgs)) 'V2 suportado'
    $supportedArgs[0] = [string]$schemaV1
    Assert-True ([bool]$isSupported.Invoke($null, $supportedArgs)) 'V1 suportado'
    $supportedArgs[0] = 'GOAB_API_METADATA_B060_V0'
    Assert-True (-not [bool]$isSupported.Invoke($null, $supportedArgs)) 'V0 rejeitado'

    $fixtures = @($readerType.GetMethod('CreateFixtures', [System.Reflection.BindingFlags]'Static, NonPublic, Public').Invoke($null, $null))
    $one = $fixtures | Where-Object { [string](Get-Prop $_ 'Name') -eq 'OneSublevel' } | Select-Object -First 1
    Assert-True ($null -ne $one) 'Fixture OneSublevel'
    $root = Get-Prop (Get-Prop $one 'Snapshot') 'RootLevel'

    $serialize = $codecType.GetMethod('SerializeLevel', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $readLevel = $codecType.GetMethod('ReadLevel', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $hasHierarchical = $codecType.GetMethod('HasHierarchicalLevels', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $serArgs = New-Object object[] 1
    $serArgs[0] = $root
    $json = $serialize.Invoke($null, $serArgs)
    $readArgs = New-Object object[] 1
    $readArgs[0] = $json
    $roundTrip = $readLevel.Invoke($null, $readArgs)
    Assert-Equal 'Order' ([string](Get-Prop $roundTrip 'LevelName')) 'Round-trip levelName'
    Assert-Equal 1 (Get-Count (Get-Prop $roundTrip 'ChildLevels')) 'Round-trip childLevels'

    $metadata = [Newtonsoft.Json.Linq.JObject]::new()
    $metadata['levels'] = $json
    $hierArgs = New-Object object[] 1
    $hierArgs[0] = $metadata
    Assert-True ([bool]$hasHierarchical.Invoke($null, $hierArgs)) 'HasHierarchicalLevels com filho'

    $flat = [Newtonsoft.Json.Linq.JObject]::new()
    $flat['schemaVersion'] = [Newtonsoft.Json.Linq.JValue]::new('GOAB_API_METADATA_B060_V2')
    $flatArgs = New-Object object[] 1
    $flatArgs[0] = $flat
    Assert-True (-not [bool]$hasHierarchical.Invoke($null, $flatArgs)) 'Sem levels = plano'

    $createDefault = $selectionType.GetMethod('CreateDefault', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $applyPrune = $selectionType.GetMethod('ApplyPersistedPrune', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $prune = $selectionType.GetMethod('Prune', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $getInclude = $selectionType.GetMethod('GetIncludeListCount', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $setInclude = $selectionType.GetMethod('SetIncludeListCount', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $defArgs = New-Object object[] 1
    $defArgs[0] = $root
    $selection = $createDefault.Invoke($null, $defArgs)
    $linesNode = $null
    foreach ($node in @(Get-Prop $selection 'Options')) {
        if ([string](Get-Prop (Get-Prop $node 'Level') 'LevelName') -eq 'Lines') {
            $linesNode = $node
            break
        }
    }
    Assert-True ($null -ne $linesNode) 'Nó Lines'
    $linesKey = [string](Get-Prop $linesNode 'PathKey')
    Assert-True ([bool]$getInclude.Invoke($selection, @($linesKey))) 'Contador default ligado'
    [void]$setInclude.Invoke($selection, @($linesKey, $false))
    $prunedDisabled = $prune.Invoke($selection, @())
    $prunedChildren = @(Get-Prop $prunedDisabled 'ChildLevels')
    Assert-Equal 1 $prunedChildren.Count 'Poda com contador desligado ainda gera o subnível'
    Assert-True (-not [bool](Get-Prop $prunedChildren[0] 'IncludeListCount')) 'IncludeListCount=false sobrevive à poda'

    $persistedLevel = $readLevel.Invoke($null, $readArgs)
    $selection2 = $createDefault.Invoke($null, $defArgs)
    [void]$setInclude.Invoke($selection2, @($linesKey, $false))
    Assert-True (-not [bool]$getInclude.Invoke($selection2, @($linesKey))) 'Pré-condição: contador desligado'
    $applyArgs = New-Object object[] 1
    $applyArgs[0] = $persistedLevel
    [void]$applyPrune.Invoke($selection2, $applyArgs)
    Assert-True ([bool]$getInclude.Invoke($selection2, @($linesKey))) 'ApplyPersistedPrune restaura IncludeListCount'
    Assert-Equal 1 (@(Get-Prop ($prune.Invoke($selection2, @())) 'ChildLevels')).Count 'ApplyPersistedPrune mantém Lines'
}
finally {
    if ($null -ne $script:AssemblyResolveHandler) {
        [System.AppDomain]::CurrentDomain.remove_AssemblyResolve($script:AssemblyResolveHandler)
    }
}

Write-Output 'PASS: ApiPlanMetadataLevelsB099b'
