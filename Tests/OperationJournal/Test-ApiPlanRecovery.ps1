#requires -Version 7.4

<#
    S-B111 / F3 P5 — gate offline da reidratação e da autorização de recuperação.

    A recuperação decide **uma** coisa por envelope: continuar a etapa que ainda falta,
    abandonar o que nunca gravou, fechar o registro de algo que já terminou, ou bloquear. A
    seção 5.4.1 do plano da F3 fecha essas transições, e é aqui que elas são exercitadas sem
    KB — inclusive as que não podem ocorrer.

    A autorização é a defesa de frescor: entre ler o diário e agir sobre ele, outra sessão pode
    tê-lo substituído, e um consentimento dado sobre um estado não vale para outro.
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
    $rehydratorType = $assembly.GetType($ns + 'ApiPlanRecoveryRehydrator', $true, $false)
    $reportType = $assembly.GetType($ns + 'ApiPlanRecoveryReport', $true, $false)
    $observationType = $assembly.GetType($ns + 'RecoveryTargetObservation', $true, $false)
    $authorizationType = $assembly.GetType($ns + 'RecoveryAuthorization', $true, $false)
    $stepEnum = $assembly.GetType($ns + 'RecoveryNextStep', $true, $false)
    $checkpointsType = $assembly.GetType($ns + 'ApiPlanOperationJournalCheckpoints', $true, $false)
    $plansType = $assembly.GetType($ns + 'ApiPlanOperationJournalPlans', $true, $false)
    $intentType = $assembly.GetType($ns + 'ApiPlanRemovalIntent', $true, $false)
    $itemType = $assembly.GetType($ns + 'ApiPlanOperationJournalInventoryItem', $true, $false)
    $receiptType = $assembly.GetType($ns + 'ApiPlanOperationJournalReceipt', $true, $false)
    $kindEnum = $assembly.GetType($ns + 'JournalOperationKind', $true, $false)
    $stateEnum = $assembly.GetType($ns + 'JournalOperationState', $true, $false)
    $intentKindEnum = $assembly.GetType($ns + 'JournalIntentKind', $true, $false)
    $blockReasonEnum = $assembly.GetType($ns + 'JournalBlockReason', $true, $false)
    $objectTypeEnum = $assembly.GetType($ns + 'JournalObjectType', $true, $false)
    $identityEnum = $assembly.GetType($ns + 'JournalIdentityKind', $true, $false)
    $actionEnum = $assembly.GetType($ns + 'JournalInventoryAction', $true, $false)
    $physicalEnum = $assembly.GetType($ns + 'JournalPhysicalState', $true, $false)
    $confirmationEnum = $assembly.GetType($ns + 'JournalConfirmation', $true, $false)
    $durabilityEnum = $assembly.GetType($ns + 'JournalDurability', $true, $false)
    $receiptOperationEnum = $assembly.GetType($ns + 'JournalReceiptOperation', $true, $false)
    $attemptStateEnum = $assembly.GetType($ns + 'JournalAttemptState', $true, $false)
    $resultEnum = $assembly.GetType($ns + 'JournalResult', $true, $false)

    $static = [System.Reflection.BindingFlags]'Static, Public'
    $rehydrate = $rehydratorType.GetMethod('Rehydrate', $static)
    $describe = $reportType.GetMethod('Describe', $static)
    $fromInventory = $intentType.GetMethod('FromInventory', $static)
    $createPrepared = $checkpointsType.GetMethod('CreatePrepared', $static)
    $promote = $checkpointsType.GetMethod('PromoteToActive', $static)
    $interrupt = $checkpointsType.GetMethod('Interrupt', $static)
    $complete = $checkpointsType.GetMethod('Complete', $static)
    $notePass = $checkpointsType.GetMethod('NoteRemovalPassCompleted', $static)
    $forRemoval = $plansType.GetMethod('ForRemoval', $static)
    $forGeneration = $plansType.GetMethod('ForGeneration', $static)

    $observationListType = [System.Collections.Generic.List`1].MakeGenericType(@($observationType))
    $itemListType = [System.Collections.Generic.List`1].MakeGenericType(@($itemType))

    function Get-Enum { param($Type, [string]$Name) return [Enum]::Parse($Type, $Name) }

    $now = [DateTime]::UtcNow
    $kbGuid = [Guid]'11111111-1111-1111-1111-111111111111'
    $txGuid = [Guid]'22222222-2222-2222-2222-222222222222'
    $appGuid = [Guid]'44444444-4444-4444-4444-444444444444'
    $apiGuid = [Guid]'55555555-5555-5555-5555-555555555555'
    $sdtGuid = [Guid]'66666666-6666-6666-6666-666666666666'

    function New-Item2 {
        param(
            [string]$Name,
            [string]$ObjectType = 'Sdt',
            [string]$Action = 'Delete',
            $Guid = $null,
            [string]$IdentityKind = 'Guid'
        )
        $item = [Activator]::CreateInstance($itemType)
        $item.Name = $Name
        $item.ObjectType = Get-Enum $objectTypeEnum $ObjectType
        $item.Action = Get-Enum $actionEnum $Action
        $item.IdentityKind = Get-Enum $identityEnum $IdentityKind
        if ($IdentityKind -eq 'Guid') { $item.Guid = if ($null -eq $Guid) { [Guid]::NewGuid() } else { $Guid } }
        $item.OwnershipValidated = $true
        $item.PhysicalState = Get-Enum $physicalEnum 'Present'
        $item.Confirmation = Get-Enum $confirmationEnum 'NotAttempted'
        return $item
    }

    function New-Envelope {
        param(
            [string]$OperationKind,
            $Plan,
            $Items = @(),
            [string]$IntentKind = 'Current'
        )
        $list = [Activator]::CreateInstance($itemListType)
        foreach ($item in $Items) { [void]$list.Add($item) }
        $args = New-Object object[] 12
        $args[0] = Get-Enum $kindEnum $OperationKind
        $args[1] = $Plan
        $args[2] = $kbGuid
        $args[3] = $txGuid
        $args[4] = 'Teste'
        $args[5] = [Guid]::NewGuid()
        $args[6] = $appGuid
        $args[7] = '0.1.0-alpha.7'
        $args[8] = Get-Enum $intentKindEnum $IntentKind
        $args[9] = $null
        $args[10] = $now
        $args[11] = $list
        return $createPrepared.Invoke($null, $args)
    }

    function New-RemovalEnvelope {
        param($Items)
        $plan = $forRemoval.Invoke($null, @([object]$apiGuid, 'abc123', [string[]]@()))
        return New-Envelope -OperationKind 'Remove' -Plan $plan -Items $Items
    }

    function Invoke-Rehydrate {
        param($Envelope, $Observations = @())
        $list = [Activator]::CreateInstance($observationListType)
        foreach ($observation in $Observations) { [void]$list.Add($observation) }
        $args = New-Object object[] 2
        $args[0] = $Envelope
        $args[1] = $list
        return $rehydrate.Invoke($null, $args)
    }

    function New-Observation {
        param($Item, [string]$PhysicalState, [string]$Confirmation = 'Confirmed')
        return [Activator]::CreateInstance($observationType, @(
            $Item,
            (Get-Enum $physicalEnum $PhysicalState),
            (Get-Enum $confirmationEnum $Confirmation),
            'teste'))
    }

    # --- 1. Envelope já terminal: nada a recuperar --------------------------------------------
    $api = New-Item2 -Name 'apiTeste' -ObjectType 'ApiObject' -Guid $apiGuid
    $done = New-RemovalEnvelope -Items @($api)
    $promote.Invoke($null, @($done, $now))
    $complete.Invoke($null, @($done, $now))
    $operation = Invoke-Rehydrate -Envelope $done
    Assert-True ([bool]$operation.AlreadyTerminal) 'Um envelope Removed não tem o que recuperar'
    Assert-True (-not [bool]$operation.CanExecute) 'Nada é executável sobre um envelope terminal'

    # --- 2. Preparado e nunca iniciado: abandono explícito --------------------------------------
    $prepared = New-Envelope -OperationKind 'Apply' -Plan ($forGeneration.Invoke($null, @([object]$apiGuid, 'abc123', $true, $true, $true, $true, [string[]]@('List'))))
    $operation = Invoke-Rehydrate -Envelope $prepared
    Assert-Equal 'Abandon' ([string]$operation.NextStep) 'Um envelope preparado que nunca gravou pode ser abandonado'
    Assert-True ([bool]$operation.CanExecute) 'O abandono é executável pela ferramenta'

    # Com recibo de gravação, o mesmo envelope deixa de ser abandonável: ele afirma que nada foi
    # gravado, e os recibos dizem o contrário.
    $withReceipt = New-Envelope -OperationKind 'Apply' -Plan ($forGeneration.Invoke($null, @([object]$apiGuid, 'abc123', $true, $true, $true, $true, [string[]]@('List'))))
    $receipt = [Activator]::CreateInstance($receiptType)
    $receipt.Sequence = 1
    $receipt.Operation = Get-Enum $receiptOperationEnum 'Save'
    $receipt.Stage = 'OwnSdts'
    $receipt.ObjectType = Get-Enum $objectTypeEnum 'Sdt'
    $receipt.Attempt = 1
    $receipt.AttemptState = Get-Enum $attemptStateEnum 'Finished'
    $receipt.Result = Get-Enum $resultEnum 'Confirmed'
    $receipt.Confirmation = Get-Enum $confirmationEnum 'Confirmed'
    $receipt.PhysicalState = Get-Enum $physicalEnum 'Present'
    $withReceipt.Receipts.Add($receipt)
    $operation = Invoke-Rehydrate -Envelope $withReceipt
    Assert-Equal 'Block' ([string]$operation.NextStep) 'Prepared com recibo de gravação não é abandonável'
    Assert-Equal 'InventoryInsufficient' ([string]$operation.Diagnostic.ReasonCode) 'A contradição do envelope tem razão própria'

    # --- 3. Durabilidade desconhecida bloqueia tudo ---------------------------------------------
    $unconfirmed = New-Envelope -OperationKind 'Apply' -Plan ($forGeneration.Invoke($null, @([object]$apiGuid, 'abc123', $true, $true, $true, $true, [string[]]@('List'))))
    $unconfirmed.JournalDurability = Get-Enum $durabilityEnum 'Unknown'
    $operation = Invoke-Rehydrate -Envelope $unconfirmed
    Assert-Equal 'Block' ([string]$operation.NextStep) 'Durabilidade desconhecida bloqueia a recuperação'
    Assert-Equal 'DurabilityUnknown' ([string]$operation.Diagnostic.Code) 'A precedência do diagnóstico é a da decisão 24'

    # --- 4. Apply interrompido: o envelope não carrega o contrato --------------------------------
    $applyPartial = New-Envelope -OperationKind 'Apply' -Plan ($forGeneration.Invoke($null, @([object]$apiGuid, 'abc123', $true, $true, $true, $true, [string[]]@('List'))))
    $promote.Invoke($null, @($applyPartial, $now))
    $interrupt.Invoke($null, @($applyPartial, (Get-Enum $stateEnum 'Partial'), (Get-Enum $blockReasonEnum 'StageFailed'), $now))
    $operation = Invoke-Rehydrate -Envelope $applyPartial
    Assert-Equal 'Block' ([string]$operation.NextStep) 'Retomar o pipeline de Apply exigiria inventar um plano'
    Assert-Equal 'JournalNonTerminal' ([string]$operation.Diagnostic.ReasonCode) 'O bloqueio do Apply é pelo estado não terminal'

    # --- 5. Resultado indeterminado não é continuado ----------------------------------------------
    $unknownOutcome = New-Envelope -OperationKind 'Apply' -Plan ($forGeneration.Invoke($null, @([object]$apiGuid, 'abc123', $true, $true, $true, $true, [string[]]@('List'))))
    $promote.Invoke($null, @($unknownOutcome, $now))
    $interrupt.Invoke($null, @($unknownOutcome, (Get-Enum $stateEnum 'OutcomeUnknown'), (Get-Enum $blockReasonEnum 'OutcomeUnknown'), $now))
    $operation = Invoke-Rehydrate -Envelope $unknownOutcome
    Assert-Equal 'Block' ([string]$operation.NextStep) 'Indeterminação exige consulta humana'
    Assert-Equal 'UnreconciledOutcome' ([string]$operation.Diagnostic.ReasonCode) 'A indeterminação tem razão própria'

    # --- 6. Remoção interrompida com alvos ainda na KB: retomar ------------------------------------
    $apiItem = New-Item2 -Name 'apiTeste' -ObjectType 'ApiObject' -Guid $apiGuid
    $sdtItem = New-Item2 -Name 'sdtTeste_API_Response' -Guid $sdtGuid
    $partialRemoval = New-RemovalEnvelope -Items @($apiItem, $sdtItem)
    $promote.Invoke($null, @($partialRemoval, $now))
    $interrupt.Invoke($null, @($partialRemoval, (Get-Enum $stateEnum 'Partial'), (Get-Enum $blockReasonEnum 'RetryBudgetExhausted'), $now))
    $operation = Invoke-Rehydrate -Envelope $partialRemoval -Observations @(
        (New-Observation -Item $apiItem -PhysicalState 'Absent' -Confirmation 'Absent'),
        (New-Observation -Item $sdtItem -PhysicalState 'Present'))
    Assert-Equal 'ContinueRemovePass' ([string]$operation.NextStep) 'Alvo previsto ainda na KB autoriza retomar a fila'
    Assert-True ([bool]$operation.CanExecute) 'A retomada é executável'
    Assert-True ($operation.Summary.Contains('sdtTeste_API_Response')) 'O resumo nomeia o que continua na KB'

    # --- 7. Remoção que já terminou: fechar o registro ----------------------------------------------
    $operation = Invoke-Rehydrate -Envelope $partialRemoval -Observations @(
        (New-Observation -Item $apiItem -PhysicalState 'Absent' -Confirmation 'Absent'),
        (New-Observation -Item $sdtItem -PhysicalState 'Absent' -Confirmation 'Absent'))
    Assert-Equal 'Complete' ([string]$operation.NextStep) 'Todos os alvos ausentes reconciliam a remoção'

    # --- 8. Alvo ilegível bloqueia a retomada ---------------------------------------------------------
    $operation = Invoke-Rehydrate -Envelope $partialRemoval -Observations @(
        (New-Observation -Item $apiItem -PhysicalState 'Absent' -Confirmation 'Absent'),
        (New-Observation -Item $sdtItem -PhysicalState 'Unknown' -Confirmation 'Divergent'))
    Assert-Equal 'Block' ([string]$operation.NextStep) 'Sem saber se o alvo está lá, a fila não é retomada'
    Assert-Equal 'IdentityDivergent' ([string]$operation.Diagnostic.ReasonCode) 'A leitura ambígua tem razão própria'

    # Um alvo sem observação nenhuma cai no mesmo lugar: ausência de leitura não é ausência.
    $operation = Invoke-Rehydrate -Envelope $partialRemoval
    Assert-Equal 'Block' ([string]$operation.NextStep) 'Sem leitura, nada é presumido ausente'

    # --- 9. Ausência antes do Delete é decisão humana --------------------------------------------------
    $absentBefore = New-RemovalEnvelope -Items @($apiItem, $sdtItem)
    $promote.Invoke($null, @($absentBefore, $now))
    $interrupt.Invoke($null, @($absentBefore, (Get-Enum $stateEnum 'Partial'), (Get-Enum $blockReasonEnum 'TargetAbsentBeforeDelete'), $now))
    $operation = Invoke-Rehydrate -Envelope $absentBefore -Observations @(
        (New-Observation -Item $apiItem -PhysicalState 'Present'),
        (New-Observation -Item $sdtItem -PhysicalState 'Present'))
    Assert-Equal 'Block' ([string]$operation.NextStep) 'Quem apagou um alvo previsto não foi esta operação: a decisão é humana'
    Assert-Equal 'InventoryInsufficient' ([string]$operation.Diagnostic.ReasonCode) 'A razão nomeia o inventário'

    # --- 10. O relatório descreve sem decidir -----------------------------------------------------------
    $lines = $describe.Invoke($null, @([object]$operation))
    Assert-True ($lines.Count -ge 4) 'O relatório descreve envelope, identidade, etapa e alvos'
    Assert-True (($lines -join ' ').Contains('TargetAbsentBeforeDelete')) 'O relatório publica o motivo registrado no envelope'

    # --- 11. A autorização vincula o consentimento ao snapshot lido ---------------------------------------
    $partialRemoval2 = New-RemovalEnvelope -Items @($apiItem, $sdtItem)
    $promote.Invoke($null, @($partialRemoval2, $now))
    $interrupt.Invoke($null, @($partialRemoval2, (Get-Enum $stateEnum 'Partial'), (Get-Enum $blockReasonEnum 'RetryBudgetExhausted'), $now))
    $resumable = Invoke-Rehydrate -Envelope $partialRemoval2 -Observations @(
        (New-Observation -Item $apiItem -PhysicalState 'Absent' -Confirmation 'Absent'),
        (New-Observation -Item $sdtItem -PhysicalState 'Present'))

    function New-Authorization {
        param(
            [bool]$Human = $true,
            $OperationId = $null,
            $ApplicationId = $null,
            [int]$FileId = 42,
            $UpdatedUtc = $null,
            [string]$Hash = 'abc',
            [string]$Step = 'ContinueRemovePass'
        )
        return [Activator]::CreateInstance($authorizationType, @(
            $Human,
            $(if ($null -eq $OperationId) { $partialRemoval2.OperationId } else { $OperationId }),
            $(if ($null -eq $ApplicationId) { $partialRemoval2.ApplicationId } else { $ApplicationId }),
            $FileId,
            $(if ($null -eq $UpdatedUtc) { $partialRemoval2.UpdatedUtc } else { $UpdatedUtc }),
            $Hash,
            (Get-Enum $stepEnum $Step),
            'ANTONIOJOSE'))
    }

    $validate = $authorizationType.GetMethod('Validate', [System.Reflection.BindingFlags]'Instance, Public')
    Assert-Equal $null ($validate.Invoke((New-Authorization), @($resumable, 42, 'abc'))) 'A autorização íntegra é aceita'

    $stale = $validate.Invoke((New-Authorization -Human $false), @($resumable, 42, 'abc'))
    Assert-True ($null -ne $stale) 'Sem confirmação humana não há execução'
    Assert-Equal 'RecoveryAuthorizationStale' ([string]$stale.ReasonCode) 'A recusa usa a razão de frescor'

    Assert-True ($null -ne $validate.Invoke((New-Authorization -Step 'Abandon'), @($resumable, 42, 'abc'))) 'Autorizar outra etapa não vale para esta'
    Assert-True ($null -ne $validate.Invoke((New-Authorization -Hash 'outro'), @($resumable, 42, 'abc'))) 'Hash divergente recusa antes da mutação'
    Assert-True ($null -ne $validate.Invoke((New-Authorization -FileId 43), @($resumable, 42, 'abc'))) 'Outro File não é o mesmo diário'
    Assert-True ($null -ne $validate.Invoke((New-Authorization -UpdatedUtc $now.AddSeconds(5)), @($resumable, 42, 'abc'))) 'Envelope atualizado depois da leitura recusa'
    Assert-True ($null -ne $validate.Invoke((New-Authorization -OperationId ([Guid]::NewGuid())), @($resumable, 42, 'abc'))) 'Outra operação não é esta'

    # --- 12. Retomada: só volta para a fila o que a intenção declarou e a KB ainda mostra -------------------
    $folderItem = [Activator]::CreateInstance($itemType)
    $folderItem.Name = 'TesteOpenApi'
    $folderItem.ObjectType = Get-Enum $objectTypeEnum 'Folder'
    $folderItem.Action = Get-Enum $actionEnum 'Preserve'
    $folderItem.IdentityKind = Get-Enum $identityEnum 'Folder'
    $folderItem.OwnershipValidated = $true
    $folderItem.EmptyConfirmed = $false
    $folderItem.PhysicalState = Get-Enum $physicalEnum 'Present'
    $folderItem.Confirmation = Get-Enum $confirmationEnum 'NotAttempted'

    $reusedFolder = [Activator]::CreateInstance($itemType)
    $reusedFolder.Name = 'Outro'
    $reusedFolder.ObjectType = Get-Enum $objectTypeEnum 'Folder'
    $reusedFolder.Action = Get-Enum $actionEnum 'Preserve'
    $reusedFolder.IdentityKind = Get-Enum $identityEnum 'Folder'
    $reusedFolder.OwnershipValidated = $true
    $reusedFolder.PhysicalState = Get-Enum $physicalEnum 'Present'
    $reusedFolder.Confirmation = Get-Enum $confirmationEnum 'NotAttempted'

    $absentSdt = New-Item2 -Name 'sdtTeste_API_ListResponse'
    $absentSdt.PhysicalState = Get-Enum $physicalEnum 'Absent'
    $absentSdt.Confirmation = Get-Enum $confirmationEnum 'Absent'

    $inventory = [Activator]::CreateInstance($itemListType)
    foreach ($item in @($apiItem, $sdtItem, $absentSdt, $folderItem, $reusedFolder)) { [void]$inventory.Add($item) }
    $resumeArgs = New-Object object[] 2
    $resumeArgs[0] = $inventory
    $resumeArgs[1] = $null
    $targets = $fromInventory.Invoke($null, $resumeArgs)

    $queued = @($targets | Where-Object { $_.Queued })
    $queuedNames = @($queued | ForEach-Object { $_.Name })
    Assert-Equal 3 $queued.Count 'Voltam para a fila os Delete presentes e o Folder próprio ainda não medido'
    Assert-True ($queuedNames -contains 'apiTeste') 'O API Object previsto e presente volta para a fila'
    Assert-True ($queuedNames -contains 'TesteOpenApi') 'O Folder próprio enfileirado volta para a fila'
    Assert-True ($queuedNames -notcontains 'sdtTeste_API_ListResponse') 'O que já saiu não é tentado de novo'
    Assert-True ($queuedNames -notcontains 'Outro') 'O Folder reutilizado nunca entra na fila destrutiva'

} finally {
    if ($null -ne $script:AssemblyResolveHandler) {
        [System.AppDomain]::CurrentDomain.remove_AssemblyResolve($script:AssemblyResolveHandler)
    }
}

Write-Output 'PASS: ApiPlanRecovery'
