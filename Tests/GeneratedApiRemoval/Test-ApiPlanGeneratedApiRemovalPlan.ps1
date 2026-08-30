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
    if ($null -eq $property) { throw "PROPERTY_MISSING: $Name" }
    return $property.GetValue($Object)
}

function Get-Count {
    param($Object)
    if ($null -eq $Object) { return 0 }
    $countProperty = $Object.GetType().GetProperty('Count', [System.Reflection.BindingFlags]'Instance, Public')
    if ($null -ne $countProperty) {
        return [int]$countProperty.GetValue($Object)
    }
    $count = 0
    foreach ($item in @($Object)) { $count++ }
    return $count
}

function Get-ItemAt {
    param($Object, [int]$Index)
    $list = @($Object)
    return $list[$Index]
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
    $planType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanGeneratedApiRemovalPlan', $true, $false)
    $inventoryType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanGeneratedApiRemovalInventory', $true, $false)
    Assert-True ($null -ne $planType) 'ApiPlanGeneratedApiRemovalPlan não encontrado.'
    Assert-True ($null -ne $inventoryType) 'ApiPlanGeneratedApiRemovalInventory não encontrado.'

    $fromMetadata = $planType.GetMethod('FromMetadata', [System.Reflection.BindingFlags]'Static, Public')
    $resolveOwn = $inventoryType.GetMethod('ResolveOwnSdtNames', [System.Reflection.BindingFlags]'Static, NonPublic, Public')

    $txGuid = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
    $apiGuid = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
    $metadata = [Newtonsoft.Json.Linq.JObject]::Parse(@"
{
  `"schemaVersion`": `"GOAB_API_METADATA_B060_V1`",
  `"ownership`": {
    `"transactionName`": `"Teste`",
    `"transactionGuid`": `"$txGuid`",
    `"apiName`": `"apiTeste`",
    `"apiGuid`": `"$apiGuid`",
    `"metadataFileName`": `"apiTeste_Metadata`"
  },
  `"objects`": {
    `"transactionFolder`": { `"name`": `"TesteOpenApi`", `"wasCreated`": true },
    `"apiObject`": { `"name`": `"apiTeste`", `"guid`": `"$apiGuid`" },
    `"procedures`": [ `"procTeste_API_List`", `"procTeste_API_Get`", `"procTeste_API_Create`", `"procTeste_API_Update`" ],
    `"sdts`": {
      `"createRequest`": `"sdtTeste_API_CreateRequest`",
      `"updateRequest`": `"sdtTeste_API_UpdateRequest`",
      `"response`": `"sdtTeste_API_Response`",
      `"listFilters`": `"sdtTeste_API_ListFilters`",
      `"listResponse`": `"sdtTeste_API_ListResponse`",
      `"shared`": [ `"sdt_API_ErrorMessage`", `"sdt_API_ErrorResponse`", `"sdt_API_Pagination`" ]
    }
  }
}
"@)

    $plan = $fromMetadata.Invoke($null, @($metadata, 'Teste', $txGuid))
    Assert-Equal 'apiTeste' (Get-Prop $plan 'ApiName') 'ApiName do plano'
    Assert-Equal 4 (Get-Count (Get-Prop $plan 'ProcedureNames')) 'Procedures no plano'
    $ownSdts = Get-Prop $plan 'OwnSdtNames'
    Assert-Equal 5 (Get-Count $ownSdts) 'SDTs próprios flat'
    Assert-Equal 'sdtTeste_API_ListResponse' (Get-ItemAt $ownSdts 0) 'ListResponse primeiro'
    $listsMethod = $planType.GetMethod('BuildConfirmationLists', [System.Reflection.BindingFlags]'Instance, Public')
    $lists = [string]$listsMethod.Invoke($plan, @())
    Assert-Contains $lists "  - sdtTeste_API_ListResponse" 'Lista de confirmacao deve citar cada SDT em linha propria.'
    Assert-Contains $lists "SDTs próprios (5):" 'Lista de confirmacao deve contar SDTs proprios.'

    $metadataV2 = [Newtonsoft.Json.Linq.JObject]::Parse($metadata.ToString([Newtonsoft.Json.Formatting]::None))
    $metadataV2['schemaVersion'] = [Newtonsoft.Json.Linq.JValue]::new('GOAB_API_METADATA_B060_V2')
    $own = [Newtonsoft.Json.Linq.JArray]::new()
    [void]$own.Add([Newtonsoft.Json.Linq.JValue]::new('sdtTeste_API_ListResponse'))
    [void]$own.Add([Newtonsoft.Json.Linq.JValue]::new('sdtTeste_API_ListResponse_Item'))
    [void]$own.Add([Newtonsoft.Json.Linq.JValue]::new('sdtTeste_API_CreateRequest'))
    [void]$own.Add([Newtonsoft.Json.Linq.JValue]::new('sdtTeste_API_CreateRequest_Item'))
    [void]$own.Add([Newtonsoft.Json.Linq.JValue]::new('sdtTeste_API_Response'))
    $metadataV2['objects']['sdts']['own'] = $own
    $planV2 = $fromMetadata.Invoke($null, @($metadataV2, 'Teste', $txGuid))
    $ownV2 = Get-Prop $planV2 'OwnSdtNames'
    Assert-Equal 5 (Get-Count $ownV2) 'V2 com own usa inventário gravado'
    Assert-Equal 'sdtTeste_API_CreateRequest_Item' (Get-ItemAt $ownV2 3) 'own inclui SDT hierárquico'

    $dynamic = [Newtonsoft.Json.Linq.JObject]::Parse($metadataV2.ToString([Newtonsoft.Json.Formatting]::None))
    $sdtsToken = $dynamic.SelectToken('objects.sdts')
    if ($sdtsToken -is [Newtonsoft.Json.Linq.JObject]) {
        [void]$sdtsToken.Remove('own')
    }
    $dynamic['levels'] = [Newtonsoft.Json.Linq.JObject]::Parse(@'
{
  "levelName": "Teste",
  "depth": 1,
  "parentLevelName": "",
  "levelOrder": 1,
  "includeListCount": true,
  "primaryKey": [],
  "fields": [],
  "childLevels": [
    {
      "levelName": "TesteItem",
      "depth": 2,
      "parentLevelName": "Teste",
      "levelOrder": 1,
      "includeListCount": false,
      "primaryKey": [],
      "fields": [],
      "childLevels": [],
      "selectedCreateFieldNames": ["TesteItemId"],
      "selectedUpdateFieldNames": ["TesteItemId"],
      "selectedResponseFieldNames": ["TesteItemId"]
    }
  ]
}
'@)
    $dynamicOwn = $resolveOwn.Invoke($null, @(, $dynamic))
    Assert-True ((Get-Count $dynamicOwn) -gt 5) 'Inventário dinâmico hierárquico sem own'
    Assert-Equal 'sdtTeste_API_ListResponse' (Get-ItemAt $dynamicOwn 0) 'Ordem dinâmica preserva ListResponse primeiro'

    $corrupt = [Newtonsoft.Json.Linq.JObject]::Parse($dynamic.ToString([Newtonsoft.Json.Formatting]::None))
    $corruptLevels = $corrupt['levels']
    Assert-True ($corruptLevels -is [Newtonsoft.Json.Linq.JObject]) 'levels do caso dinâmico deve ser JObject.'
    [void]$corruptLevels.Remove('levelName')
    $threwCorrupt = $false
    try {
        [void]$resolveOwn.Invoke($null, @(, $corrupt))
    }
    catch {
        $threwCorrupt = $true
        $msg = [string]$_.Exception.ToString()
        Assert-Contains $msg 'levels ilegível' 'Remoção deve recusar levels ilegível sem fallback flat.'
    }

    Assert-True $threwCorrupt 'levels sem levelName deve falhar (fail-closed), não cair no flat.'

    try {
        [void]$fromMetadata.Invoke($null, @($metadata, 'Outra', $txGuid))
        throw 'ASSERT_FAILED: deveria rejeitar Transaction divergente.'
    }
    catch {
        if ($_.Exception.Message -notmatch 'ownership.transactionName') {
            throw
        }
    }
}
finally {
    if ($null -ne $script:AssemblyResolveHandler) {
        [System.AppDomain]::CurrentDomain.remove_AssemblyResolve($script:AssemblyResolveHandler)
    }
}

$confirmDialogPath = Join-Path $repositoryRoot 'Src\Extension\ExtensionConfirmDialog.cs'
$confirmDialogSource = Get-Content -Raw -LiteralPath $confirmDialogPath
Assert-True ($confirmDialogSource -match 'WordWrap = false') 'O preview B086 nao deve quebrar nome de objeto no meio da palavra.'
Assert-True ($confirmDialogSource -match 'ScrollBars = ScrollBars\.Both') 'O preview B086 deve rolar vertical e horizontal quando a lista exceder a area.'
Assert-True ($confirmDialogSource -match 'BuildConfirmationLists\(\)') 'O preview B086 deve reusar a mesma lista da Output.'
Assert-True ($confirmDialogSource -match 'RowStyle\(SizeType\.Percent, 100f\)') 'O preview B086 deve reservar a linha do meio para a lista rolavel.'
Assert-True ($confirmDialogSource -match 'FlowDirection = FlowDirection\.RightToLeft') 'O preview B086 deve manter Sim/Nao fora da area de rolagem.'
Assert-True ($confirmDialogSource -match 'working\.Height - 32') 'O preview B086 deve limitar a altura a area util do monitor.'
Assert-True ($confirmDialogSource -match 'Size = new Size\(preferredWidth, maxHeight\)') 'O preview B086 deve abrir com a altura disponivel da tela.'
Assert-True ($confirmDialogSource -match 'WidthScale = 1\.5') 'O preview B086 deve ampliar a largura em pelo menos 50%.'
Assert-True ($confirmDialogSource -match 'MaximumSize = new Size\(maxWidth, maxHeight\)') 'O preview B086 nao deve estourar a tela.'
Assert-True ($confirmDialogSource -notmatch 'Screen\.FromPoint\(Cursor\.Position\)\.WorkingArea') 'O preview B086 nao deve escolher o monitor pela posicao do cursor.'
Assert-True ($confirmDialogSource -match 'IWin32Window\? owner') 'O preview B086 deve receber a janela owner da IDE.'
Assert-True ($confirmDialogSource -match '_owner\.Handle') 'O preview B086 deve priorizar o monitor da janela owner.'
Assert-True ($confirmDialogSource -match 'Process\.GetCurrentProcess\(\)\.MainWindowHandle') 'O preview B086 deve usar a janela principal do processo como fallback.'
Assert-True ($confirmDialogSource -match 'AcceptButton = _noButton') 'O preview B086 deve manter Nao como default seguro.'
Assert-True ($confirmDialogSource -notmatch '_leftColumnLabel') 'O preview B086 nao deve mais partir a lista em duas colunas com wrap.'

$packageSource = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot 'Src\Extension\Package.cs')
Assert-True ($packageSource -match 'new ExtensionConfirmDialog\(') 'Package deve abrir o preview B086 pelo ExtensionConfirmDialog.'
Assert-True ($packageSource -match 'confirmationDialog\.ShowDialog\(owner\)') 'Package deve ancorar o preview B086 no owner da IDE.'

Write-Output 'PASS: ApiPlanGeneratedApiRemovalPlan'
