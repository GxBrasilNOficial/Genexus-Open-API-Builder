#requires -Version 7.4

<#
    S-B111 / F3 — gate offline da máquina de checkpoints do diário.

    A matriz da seção 4.4 do plano da F3 é política, não detalhe de implementação: ela diz
    quantas gravações cada operação faz e em que fronteiras. Este gate exercita as
    transições sem KB, para que a integração na IDE não seja o primeiro lugar onde um
    estágio errado aparece.

    Cobre também a regra de identidade da API: numa criação nova o GUID só nasce do
    `API.Create`, dentro do pipeline, então o envelope pode ser aberto sem ele — mas não
    pode alcançar a fronteira do API sem registrá-lo.
#>

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
        throw "ASSERT_CONTAINS_FAILED: $Message (needle='$Needle' text='$Text')"
    }
}

function Get-AssemblyDirectoryCandidates {
    param([string]$GeneXusRoot)
    $candidates = [System.Collections.Generic.List[string]]::new()
    foreach ($relative in @('Packages', 'GeneXusBlazorControls', '')) {
        $path = if ([string]::IsNullOrWhiteSpace($relative)) { $GeneXusRoot } else { Join-Path $GeneXusRoot $relative }
        if (Test-Path -LiteralPath $path -PathType Container) { $candidates.Add($path) }
    }
    $dllDirectory = Split-Path -Parent $DllPath
    if (Test-Path -LiteralPath $dllDirectory -PathType Container) { $candidates.Add($dllDirectory) }
    return @($candidates | Select-Object -Unique)
}

function Initialize-GeneXusAssemblyResolver {
    param([string[]]$SearchDirectories)
    $script:AssemblySearchDirectories = @($SearchDirectories)
    $script:AssemblyResolveBusy = $false
    $script:AssemblyResolveHandler = {
        param($sender, $eventArgs)
        if ($script:AssemblyResolveBusy) { return $null }
        $script:AssemblyResolveBusy = $true
        try {
            $simpleName = ($eventArgs.Name -split ',')[0]
            foreach ($directory in $script:AssemblySearchDirectories) {
                $candidate = Join-Path $directory ($simpleName + '.dll')
                if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                    return [System.Reflection.Assembly]::LoadFrom($candidate)
                }
            }
            return $null
        } finally {
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
    $ns = 'GenexusOpenApiBuilder.Extension.Diagnostics.'
    $checkpointsType = $assembly.GetType($ns + 'ApiPlanOperationJournalCheckpoints', $true, $false)
    $plansType = $assembly.GetType($ns + 'ApiPlanOperationJournalPlans', $true, $false)
    $journalType = $assembly.GetType($ns + 'ApiPlanOperationJournal', $true, $false)
    $kindType = $assembly.GetType($ns + 'JournalOperationKind', $true, $false)
    $stateType = $assembly.GetType($ns + 'JournalOperationState', $true, $false)
    $intentType = $assembly.GetType($ns + 'JournalIntentKind', $true, $false)
    $durabilityType = $assembly.GetType($ns + 'JournalDurability', $true, $false)
    $blockReasonType = $assembly.GetType($ns + 'JournalBlockReason', $true, $false)

    $static = [System.Reflection.BindingFlags]'Static, Public'
    $instance = [System.Reflection.BindingFlags]'Instance, Public'
    $createPrepared = $checkpointsType.GetMethod('CreatePrepared', $static)
    $promote = $checkpointsType.GetMethod('PromoteToActive', $static)
    $noteApi = $checkpointsType.GetMethod('NoteApiPhysicallySaved', $static)
    $noteApiUnknown = $checkpointsType.GetMethod('NoteApiSaveOutcomeUnknown', $static)
    $noteRecovered = $checkpointsType.GetMethod('NoteMetadataRecovered', $static)
    $notePass = $checkpointsType.GetMethod('NoteRemovalPassCompleted', $static)
    $complete = $checkpointsType.GetMethod('Complete', $static)
    $interrupt = $checkpointsType.GetMethod('Interrupt', $static)
    $abandon = $checkpointsType.GetMethod('Abandon', $static)
    $evaluateReuse = $checkpointsType.GetMethod('EvaluateReuse', $static)
    $expected = $checkpointsType.GetMethod('ExpectedPhysicalCheckpoints', $static)
    $forGeneration = $plansType.GetMethod('ForGeneration', $static)
    $forRemoval = $plansType.GetMethod('ForRemoval', $static)
    $forRecovery = $plansType.GetMethod('ForMetadataRecovery', $static)

    function Get-Prop { param($Object, [string]$Name) return $Object.GetType().GetProperty($Name, $instance).GetValue($Object) }
    function Get-Enum { param($Type, [string]$Name) return [Enum]::Parse($Type, $Name) }
    function Get-Stage { param($Journal) return [string](Get-Prop $Journal 'LogicalStage') }
    function Get-State { param($Journal) return [string](Get-Prop $Journal 'OperationState') }
    function Get-Phase { param($Journal) return [string](Get-Prop $Journal 'EnvelopePhase') }

    $now = [DateTime]::UtcNow
    $kbGuid = [Guid]'11111111-1111-1111-1111-111111111111'
    $txGuid = [Guid]'22222222-2222-2222-2222-222222222222'
    $opGuid = [Guid]'33333333-3333-3333-3333-333333333333'
    $appGuid = [Guid]'44444444-4444-4444-4444-444444444444'
    $apiGuid = [Guid]'55555555-5555-5555-5555-555555555555'

    function New-Prepared {
        param($OperationKind, $Plan, $IntentKind = 'Current', $MetadataSchema = $null, $Inventory = $null)
        $args = New-Object object[] 12
        $args[0] = Get-Enum $kindType $OperationKind
        $args[1] = $Plan
        $args[2] = $kbGuid
        $args[3] = $txGuid
        $args[4] = 'Teste'
        $args[5] = $opGuid
        $args[6] = $appGuid
        $args[7] = '0.1.0-alpha.7'
        $args[8] = Get-Enum $intentType $IntentKind
        $args[9] = $MetadataSchema
        $args[10] = $now
        $args[11] = $Inventory
        return $createPrepared.Invoke($null, $args)
    }

    function New-GenerationPlan {
        param($PlannedApi = $null)
        $args = New-Object object[] 7
        $args[0] = $PlannedApi
        $args[1] = 'abc123'
        $args[2] = $true
        $args[3] = $true
        $args[4] = $true
        $args[5] = $true
        $args[6] = [string[]]@('List', 'Get')
        return $forGeneration.Invoke($null, $args)
    }

    function Invoke-Transition {
        param($Method, $Journal, $ExtraArgs = @())
        $args = New-Object object[] (1 + $ExtraArgs.Count + 1)
        $args[0] = $Journal
        for ($i = 0; $i -lt $ExtraArgs.Count; $i++) { $args[$i + 1] = $ExtraArgs[$i] }
        $args[$args.Count - 1] = $now
        return $Method.Invoke($null, $args)
    }

    function Assert-Rejects {
        param($Method, $Journal, $ExtraArgs = @(), [string]$Needle, [string]$Message)
        $rejected = $false
        try {
            Invoke-Transition -Method $Method -Journal $Journal -ExtraArgs $ExtraArgs
        } catch {
            $rejected = $true
            Assert-Contains ([string]$_.Exception.InnerException.Message) $Needle "Mensagem esperada: $Message"
        }
        Assert-True $rejected "Deveria recusar: $Message"
    }

    # --- 1. Apply: CP1 → CP2 → CP3 → CP4 ----------------------------------------------------
    $apply = New-Prepared -OperationKind 'Apply' -Plan (New-GenerationPlan -PlannedApi $apiGuid) -MetadataSchema 'GOAB_API_METADATA_B060_V3'
    Assert-Equal 'Prepared' (Get-Phase $apply) 'CP1 abre o envelope em Prepared'
    Assert-Equal 'Pending' (Get-State $apply) 'CP1 mantém a operação Pending'
    Assert-Equal 'IntentionRecorded' (Get-Stage $apply) 'CP1 registra a intenção'

    Invoke-Transition -Method $promote -Journal $apply
    Assert-Equal 'Active' (Get-Phase $apply) 'CP2 promove a Active'
    Assert-Equal 'Running' (Get-State $apply) 'CP2 passa a Running'
    Assert-Equal 'NotStarted' (Get-Stage $apply) 'CP2 de Apply começa em NotStarted'

    Invoke-Transition -Method $noteApi -Journal $apply
    Assert-Equal 'ApiPhysicallySaved' (Get-Stage $apply) 'CP3 registra o API confirmado'

    Invoke-Transition -Method $complete -Journal $apply
    Assert-Equal 'Completed' (Get-State $apply) 'CP4 conclui Apply'
    Assert-Equal 'Completed' (Get-Stage $apply) 'CP4 usa o estágio terminal'
    Assert-Equal 4 ([int]$expected.Invoke($null, @((Get-Enum $kindType 'Apply'), 0))) 'Apply prevê 4 checkpoints físicos'
    Assert-Equal 4 ([int]$expected.Invoke($null, @((Get-Enum $kindType 'Sync'), 0))) 'Sync prevê 4 checkpoints físicos'

    # --- 2. Remove: passadas e terminal ------------------------------------------------------
    # O Remove registra a intenção já com o inventário validado; sem ele o CP1 é recusado,
    # porque uma remoção sem alvos declarados não pode ser reconciliada depois.
    $itemType = $assembly.GetType($ns + 'ApiPlanOperationJournalInventoryItem', $true, $false)
    $objectTypeEnum = $assembly.GetType($ns + 'JournalObjectType', $true, $false)
    $identityKindEnum = $assembly.GetType($ns + 'JournalIdentityKind', $true, $false)
    $actionEnum = $assembly.GetType($ns + 'JournalInventoryAction', $true, $false)
    $physicalEnum = $assembly.GetType($ns + 'JournalPhysicalState', $true, $false)
    $confirmationEnum = $assembly.GetType($ns + 'JournalConfirmation', $true, $false)

    function New-InventoryItem {
        $item = [Activator]::CreateInstance($itemType)
        $item.ObjectType = Get-Enum $objectTypeEnum 'ApiObject'
        $item.IdentityKind = Get-Enum $identityKindEnum 'Guid'
        $item.Guid = $apiGuid
        $item.Name = 'apiTeste'
        $item.OwnershipValidated = $true
        $item.Action = Get-Enum $actionEnum 'Delete'
        $item.PhysicalState = Get-Enum $physicalEnum 'Present'
        $item.Confirmation = Get-Enum $confirmationEnum 'NotAttempted'
        return $item
    }

    function New-RemovalInventory {
        # A lista precisa ser do tipo exato do item: IEnumerable<T> não aceita List<object>.
        $listType = [System.Collections.Generic.List`1].MakeGenericType(@($itemType))
        $list = [Activator]::CreateInstance($listType)
        [void]$list.Add((New-InventoryItem))
        # A vírgula impede o unroll do PowerShell: sem ela, a lista chega ao método como um
        # item só e a assinatura IEnumerable<> não casa.
        return ,$list
    }

    $removalPlan = $forRemoval.Invoke($null, @([object]$apiGuid, 'abc123', [string[]]@()))
    $remove = New-Prepared -OperationKind 'Remove' -Plan $removalPlan -Inventory (New-RemovalInventory)
    Invoke-Transition -Method $promote -Journal $remove
    Assert-Equal 'RemovalInProgress' (Get-Stage $remove) 'CP2 de Remove entra na remoção'
    Invoke-Transition -Method $notePass -Journal $remove
    Assert-Equal 'RemovalInProgress' (Get-Stage $remove) 'Uma passada mantém o estágio de remoção'
    Invoke-Transition -Method $complete -Journal $remove
    Assert-Equal 'Removed' (Get-State $remove) 'Remove termina em Removed'
    Assert-Equal 'Removed' (Get-Stage $remove) 'Remove usa o estágio Removed'
    Assert-Equal 5 ([int]$expected.Invoke($null, @((Get-Enum $kindType 'Remove'), 2))) 'Remove prevê 3 + P checkpoints'
    Assert-Equal 3 ([int]$expected.Invoke($null, @((Get-Enum $kindType 'Remove'), 0))) 'Remove sem passada prevê 3'

    # Interrupção de remoção: Partial exige o estágio que diz onde a fila parou.
    $removePartial = New-Prepared -OperationKind 'Remove' -Plan ($forRemoval.Invoke($null, @([object]$apiGuid, 'abc123', [string[]]@()))) -Inventory (New-RemovalInventory)
    Invoke-Transition -Method $promote -Journal $removePartial
    Invoke-Transition -Method $interrupt -Journal $removePartial -ExtraArgs @((Get-Enum $stateType 'Partial'), (Get-Enum $blockReasonType 'RetryBudgetExhausted'))
    Assert-Equal 'Partial' (Get-State $removePartial) 'Interrupção de Remove é Partial'
    Assert-Equal 'RemovalPartial' (Get-Stage $removePartial) 'Partial de Remove exige RemovalPartial'
    Assert-Equal 'RetryBudgetExhausted' ([string](Get-Prop $removePartial 'BlockReason')) 'O motivo persistido acompanha a interrupção'

    # --- 3. Recovery autônomo de B115 --------------------------------------------------------
    $recovery = New-Prepared -OperationKind 'Recovery' -Plan ($forRecovery.Invoke($null, @([object]$apiGuid))) -IntentKind 'Imported' -MetadataSchema 'GOAB_API_METADATA_B060_V3'
    Invoke-Transition -Method $promote -Journal $recovery
    Assert-Equal 'MetadataPending' (Get-Stage $recovery) 'CP2 do Recovery aguarda a metadata'
    Invoke-Transition -Method $noteRecovered -Journal $recovery
    Assert-Equal 'MetadataRecovered' (Get-Stage $recovery) 'CP3 do Recovery registra a metadata recuperada'
    Invoke-Transition -Method $complete -Journal $recovery
    Assert-Equal 'Completed' (Get-State $recovery) 'Recovery termina em Completed'

    # --- 4. Transições que o contrato recusa --------------------------------------------------
    $fresh = New-Prepared -OperationKind 'Apply' -Plan (New-GenerationPlan -PlannedApi $apiGuid)
    Assert-Rejects -Method $noteApi -Journal $fresh -Needle 'precisa estar Active/Running' -Message 'registrar o API antes de promover a Active'
    Assert-Rejects -Method $complete -Journal $fresh -Needle 'precisa estar Active/Running' -Message 'concluir um envelope ainda Prepared'

    $active = New-Prepared -OperationKind 'Apply' -Plan (New-GenerationPlan -PlannedApi $apiGuid)
    Invoke-Transition -Method $promote -Journal $active
    Assert-Rejects -Method $promote -Journal $active -Needle 'Só um envelope Prepared/Pending pode ser promovido' -Message 'promover duas vezes'
    Assert-Rejects -Method $abandon -Journal $active -ExtraArgs @('motivo', 'ANTONIOJOSE') -Needle 'Só um envelope Prepared/Pending pode ser abandonado' -Message 'abandonar um envelope ativo'
    Assert-Rejects -Method $interrupt -Journal $active -ExtraArgs @((Get-Enum $stateType 'Completed'), (Get-Enum $blockReasonType 'OutcomeUnknown')) -Needle 'termina em Partial ou OutcomeUnknown' -Message 'interromper para um estado terminal de sucesso'

    $removeApi = New-Prepared -OperationKind 'Remove' -Plan ($forRemoval.Invoke($null, @([object]$apiGuid, 'abc123', [string[]]@()))) -Inventory (New-RemovalInventory)
    Invoke-Transition -Method $promote -Journal $removeApi
    Assert-Rejects -Method $noteApi -Journal $removeApi -Needle 'ApiPhysicallySaved pertence a Apply e Sync' -Message 'registrar API confirmado numa remoção'
    Assert-Rejects -Method $noteRecovered -Journal $removeApi -Needle 'MetadataRecovered pertence ao Recovery' -Message 'registrar metadata recuperada fora do Recovery'

    $applyPass = New-Prepared -OperationKind 'Apply' -Plan (New-GenerationPlan -PlannedApi $apiGuid)
    Invoke-Transition -Method $promote -Journal $applyPass
    Assert-Rejects -Method $notePass -Journal $applyPass -Needle 'Passadas de remoção pertencem ao Remove' -Message 'registrar passada de remoção num Apply'

    # --- 5. Abandono explícito ----------------------------------------------------------------
    $abandoned = New-Prepared -OperationKind 'Apply' -Plan (New-GenerationPlan -PlannedApi $apiGuid)
    Invoke-Transition -Method $abandon -Journal $abandoned -ExtraArgs @('Abandono autorizado pelo usuário', 'ANTONIOJOSE')
    Assert-Equal 'Completed' (Get-State $abandoned) 'O abandono mantém operationState=Completed'
    Assert-Equal 'Abandoned' (Get-Stage $abandoned) 'O abandono usa o estágio próprio'
    Assert-Equal 'Prepared' (Get-Phase $abandoned) 'O abandono não promove o envelope'
    Assert-True ($null -ne (Get-Prop $abandoned 'Abandonment')) 'O abandono grava a disposição'

    # --- 6. Identidade da API por estágio ------------------------------------------------------
    # Criação nova: o GUID só nasce do API.Create, dentro do pipeline. O envelope abre sem ele...
    $newApi = New-Prepared -OperationKind 'Apply' -Plan (New-GenerationPlan)
    Invoke-Transition -Method $promote -Journal $newApi
    Assert-Equal 'NotStarted' (Get-Stage $newApi) 'Uma criação nova abre o envelope sem identidade de API'
    # ...mas não pode alcançar a fronteira do API sem registrá-lo.
    Assert-Rejects -Method $noteApi -Journal $newApi -Needle 'plan.plannedApiGuid é obrigatório' -Message 'confirmar o API sem identidade registrada'

    $plan = Get-Prop $newApi 'Plan'
    $plan.GetType().GetProperty('PlannedApiGuid', $instance).SetValue($plan, [object]$apiGuid)
    Invoke-Transition -Method $noteApi -Journal $newApi
    Assert-Equal 'ApiPhysicallySaved' (Get-Stage $newApi) 'Com a identidade registrada, a fronteira do API é aceita'

    # Resultado indeterminado também exige identidade: é por ela que a recuperação consulta.
    $unknownApi = New-Prepared -OperationKind 'Apply' -Plan (New-GenerationPlan)
    Invoke-Transition -Method $promote -Journal $unknownApi
    Assert-Rejects -Method $noteApiUnknown -Journal $unknownApi -Needle 'plan.plannedApiGuid é obrigatório' -Message 'registrar indeterminação sem identidade'

    # --- 7. Reutilização do diário entre operações --------------------------------------------
    $reuseNone = $evaluateReuse.Invoke($null, @($null, (Get-Enum $durabilityType 'Confirmed')))
    Assert-True ([bool](Get-Prop $reuseNone 'CanStart')) 'Sem diário anterior, a operação pode começar'

    $reuseCompleted = $evaluateReuse.Invoke($null, @($apply, (Get-Enum $durabilityType 'Confirmed')))
    Assert-True ([bool](Get-Prop $reuseCompleted 'CanStart')) 'Um envelope Completed e durável libera a próxima operação'

    $reuseRemoved = $evaluateReuse.Invoke($null, @($remove, (Get-Enum $durabilityType 'Confirmed')))
    Assert-True ([bool](Get-Prop $reuseRemoved 'CanStart')) 'Um envelope Removed e durável libera a próxima operação'

    $reuseUnknown = $evaluateReuse.Invoke($null, @($apply, (Get-Enum $durabilityType 'Unknown')))
    Assert-True (-not [bool](Get-Prop $reuseUnknown 'CanStart')) 'Durabilidade desconhecida bloqueia nova operação'
    Assert-Contains ([string](Get-Prop $reuseUnknown 'Reason')) 'não pôde ser confirmada' 'O bloqueio explica a durabilidade'

    $reusePartial = $evaluateReuse.Invoke($null, @($removePartial, (Get-Enum $durabilityType 'Confirmed')))
    Assert-True (-not [bool](Get-Prop $reusePartial 'CanStart')) 'Um envelope Partial bloqueia nova operação'
    Assert-Contains ([string](Get-Prop $reusePartial 'Reason')) 'não é terminal' 'O bloqueio explica o estado não terminal'

    $reuseAbandoned = $evaluateReuse.Invoke($null, @($abandoned, (Get-Enum $durabilityType 'Confirmed')))
    Assert-True ([bool](Get-Prop $reuseAbandoned 'CanStart')) 'Um envelope abandonado explicitamente libera a KB'

} finally {
    if ($null -ne $script:AssemblyResolveHandler) {
        [System.AppDomain]::CurrentDomain.remove_AssemblyResolve($script:AssemblyResolveHandler)
    }
}

Write-Output 'PASS: ApiPlanOperationJournalCheckpoints'
