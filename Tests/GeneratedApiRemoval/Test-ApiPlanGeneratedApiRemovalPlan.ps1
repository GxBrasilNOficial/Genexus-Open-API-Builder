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

    # V3 acrescenta ownership.applicationId; a remoção continua aceitando a metadata.
    $metadataV3 = [Newtonsoft.Json.Linq.JObject]::Parse($metadataV2.ToString([Newtonsoft.Json.Formatting]::None))
    $metadataV3['schemaVersion'] = [Newtonsoft.Json.Linq.JValue]::new('GOAB_API_METADATA_B060_V3')
    $metadataV3['ownership']['applicationId'] = [Newtonsoft.Json.Linq.JValue]::new('cccccccc-cccc-cccc-cccc-cccccccccccc')
    $planV3 = $fromMetadata.Invoke($null, @($metadataV3, 'Teste', $txGuid))
    Assert-Equal 5 (Get-Count (Get-Prop $planV3 'OwnSdtNames')) 'V3 aceita e preserva o inventário gravado'
    Assert-True ([bool](Get-Prop $planV3 'FolderShouldBeRemoved')) 'Legado V3 com wasCreated=true ainda enfileira o Folder'

    $metadataV3False = [Newtonsoft.Json.Linq.JObject]::Parse($metadataV3.ToString([Newtonsoft.Json.Formatting]::None))
    $metadataV3False['objects']['transactionFolder']['wasCreated'] = [Newtonsoft.Json.Linq.JValue]::new($false)
    $planV3False = $fromMetadata.Invoke($null, @($metadataV3False, 'Teste', $txGuid))
    Assert-True (-not [bool](Get-Prop $planV3False 'FolderShouldBeRemoved')) 'Legado V3 com wasCreated=false não enfileira o Folder'

    # V4: posse histórica. wasCreated da operação não autoriza sozinho.
    $folderGuid = 'dddddddd-dddd-dddd-dddd-dddddddddddd'
    $metadataV4 = [Newtonsoft.Json.Linq.JObject]::Parse($metadataV3.ToString([Newtonsoft.Json.Formatting]::None))
    $metadataV4['schemaVersion'] = [Newtonsoft.Json.Linq.JValue]::new('GOAB_API_METADATA_B060_V4')
    $metadataV4['objects']['transactionFolder']['wasCreated'] = [Newtonsoft.Json.Linq.JValue]::new($false)
    $metadataV4['objects']['transactionFolder']['ownedByThisApi'] = [Newtonsoft.Json.Linq.JValue]::new($true)
    $metadataV4['objects']['transactionFolder']['guid'] = [Newtonsoft.Json.Linq.JValue]::new($folderGuid)
    $planV4 = $fromMetadata.Invoke($null, @($metadataV4, 'Teste', $txGuid))
    Assert-True ([bool](Get-Prop $planV4 'FolderOwnedByThisApi')) 'V4 ownedByThisApi=true marca posse'
    Assert-True ([bool](Get-Prop $planV4 'FolderShouldBeRemoved')) 'V4 ownedByThisApi=true enfileira mesmo com wasCreated=false'
    Assert-Equal $folderGuid ([string](Get-Prop $planV4 'FolderGuid')) 'V4 persiste o GUID do Folder'
    $summaryV4 = [string]$planType.GetMethod('BuildConfirmationSummary', [System.Reflection.BindingFlags]'Instance, Public').Invoke($planV4, @())
    Assert-Contains $summaryV4 'próprio da API; a remoção apaga se ficar vazio' 'Confirmação V4 de reencontro próprio'

    $metadataV4Op = [Newtonsoft.Json.Linq.JObject]::Parse($metadataV4.ToString([Newtonsoft.Json.Formatting]::None))
    $metadataV4Op['objects']['transactionFolder']['wasCreated'] = [Newtonsoft.Json.Linq.JValue]::new($true)
    $metadataV4Op['objects']['transactionFolder']['ownedByThisApi'] = [Newtonsoft.Json.Linq.JValue]::new($false)
    $planV4Op = $fromMetadata.Invoke($null, @($metadataV4Op, 'Teste', $txGuid))
    Assert-True (-not [bool](Get-Prop $planV4Op 'FolderShouldBeRemoved')) 'V4 wasCreated=true sem posse não enfileira'

    $ownType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanTransactionFolderOwnership', $true, $false)
    Assert-True ($null -ne $ownType) 'ApiPlanTransactionFolderOwnership não encontrado.'
    $resolveOwnFolder = $ownType.GetMethod('ResolveOwnedByThisApi', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $matchGuid = $ownType.GetMethod('MatchesPersistedGuid', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    Assert-True ($null -ne $resolveOwnFolder) 'ResolveOwnedByThisApi não encontrado.'
    Assert-True ($null -ne $matchGuid) 'MatchesPersistedGuid não encontrado.'

    $previousOwned = [Newtonsoft.Json.Linq.JObject]::Parse('{"objects":{"transactionFolder":{"name":"TesteOpenApi","wasCreated":false,"ownedByThisApi":true}}}')
    Assert-True ([bool]$resolveOwnFolder.Invoke($null, @($false, $previousOwned))) 'Regravação preserva ownedByThisApi=true do JSON anterior'
    Assert-True ([bool]$resolveOwnFolder.Invoke($null, @($true, $null))) 'Criação nesta execução marca posse mesmo sem metadata anterior'
    Assert-True (-not [bool]$resolveOwnFolder.Invoke($null, @($false, $null))) 'Reuso sem metadata anterior não inventa posse'

    $sameGuid = [guid]$folderGuid
    $otherGuid = [guid]'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee'
    Assert-True ([bool]$matchGuid.Invoke($null, @($sameGuid, $sameGuid))) 'GUID persistido igual ao da KB autoriza'
    Assert-True (-not [bool]$matchGuid.Invoke($null, @($sameGuid, $otherGuid))) 'GUID persistido diferente do da KB recusa exclusão'
    Assert-True ([bool]$matchGuid.Invoke($null, @($null, $sameGuid))) 'Sem GUID persistido a identidade não restringe'

    # Preview: GUID divergente alinha anúncio/contagem (não só a fila em BuildTargets).
    $captureType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanGeneratedApiRemovalPreviewCapture', $true, $false)
    Assert-True ($null -ne $captureType) 'ApiPlanGeneratedApiRemovalPreviewCapture não encontrado.'
    $emptyGuids = New-Object 'System.Collections.Generic.Dictionary[string,System.Guid]'
    $emptyGuidsRo = [System.Collections.ObjectModel.ReadOnlyDictionary[string,System.Guid]]::new($emptyGuids)
    $captureCtor = $captureType.GetConstructors([System.Reflection.BindingFlags]'Instance, NonPublic, Public') |
        Where-Object { $_.GetParameters().Count -eq 9 } |
        Select-Object -First 1
    Assert-True ($null -ne $captureCtor) 'Construtor da PreviewCapture não encontrado.'
    $planGuidMismatch = $fromMetadata.Invoke($null, @($metadataV4, 'Teste', $txGuid))
    Assert-True ([bool](Get-Prop $planGuidMismatch 'FolderShouldBeRemoved')) 'Antes do Preview, V4 com posse ainda anuncia o Folder'
    $captureMismatch = $captureCtor.Invoke([object[]]@(
        [guid]$txGuid,
        [guid]'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        '0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef',
        'GOAB_API_METADATA_B060_V4',
        $null,
        $null,
        $false,
        $otherGuid,
        $emptyGuidsRo
    ))
    $attach = $planType.GetMethod('AttachPreviewCapture', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    Assert-True ($null -ne $attach) 'AttachPreviewCapture não encontrado.'
    [void]$attach.Invoke($planGuidMismatch, @($captureMismatch))
    Assert-True (-not [bool](Get-Prop $planGuidMismatch 'FolderShouldBeRemoved')) 'GUID divergente no Preview desanuncia o Folder'
    $countDeletes = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanGeneratedApiRemover', $true, $false).
        GetMethod('CountPlannedDeletes', [System.Reflection.BindingFlags]'Static, Public')
    $plannedAfterMismatch = [int]$countDeletes.Invoke($null, @($planGuidMismatch))
    $planGuidMatch = $fromMetadata.Invoke($null, @($metadataV4, 'Teste', $txGuid))
    $captureMatch = $captureCtor.Invoke([object[]]@(
        [guid]$txGuid,
        [guid]'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        '0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef',
        'GOAB_API_METADATA_B060_V4',
        $null,
        $null,
        $false,
        $sameGuid,
        $emptyGuidsRo
    ))
    [void]$attach.Invoke($planGuidMatch, @($captureMatch))
    Assert-True ([bool](Get-Prop $planGuidMatch 'FolderShouldBeRemoved')) 'GUID igual no Preview mantém o anúncio do Folder'
    $plannedAfterMatch = [int]$countDeletes.Invoke($null, @($planGuidMatch))
    Assert-True ($plannedAfterMatch -eq ($plannedAfterMismatch + 1)) 'Contagem do Preview deve incluir o Folder só quando o GUID casa'

    # Gate: FormatSupportedVersionList() deve existir como fragmento no catálogo de l10n.
    $schemaType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanMetadataSchema', $true, $false)
    $formatList = $schemaType.GetMethod('FormatSupportedVersionList', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $versionListPt = [string]$formatList.Invoke($null, @())
    $locSrc = Get-Content -LiteralPath (Join-Path $repositoryRoot 'Src/Domain/ExtensionOutputLocalization.cs') -Raw
    Assert-Contains $locSrc ('"' + $versionListPt + '"') "Catálogo deve traduzir o fragmento produzido por FormatSupportedVersionList ('$versionListPt')."

    $metadataV0 = [Newtonsoft.Json.Linq.JObject]::Parse($metadataV4.ToString([Newtonsoft.Json.Formatting]::None))
    $metadataV0['schemaVersion'] = [Newtonsoft.Json.Linq.JValue]::new('GOAB_API_METADATA_B060_V5')
    $rejected = $false
    try {
        [void]$fromMetadata.Invoke($null, @($metadataV0, 'Teste', $txGuid))
    } catch {
        $rejected = $true
        Assert-Contains ([string]$_.Exception.InnerException.Message) 'V1, V2, V3 ou V4' 'A mensagem deve citar as quatro versões aceitas.'
    }
    Assert-True $rejected 'Versão desconhecida de schema deve bloquear a remoção.'

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
