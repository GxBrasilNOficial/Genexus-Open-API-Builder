#requires -Version 7.4

<#
    S-B111 / F3 — gate offline da precedência de diagnóstico do diário (etapa P3).

    A seção 4.2 do plano da F3 e a decisão 24 fecham quatro códigos de diagnóstico numa
    ordem que não é negociável: `JournalUnavailable` sobre `DurabilityUnknown`, que fica
    sobre `GateBlocked`, e `PreconditionFailed` fora de `GateBlocked` — nunca como subcausa
    dele. Ordem errada muda o que a recuperação vai oferecer ao usuário: reconciliar, apagar
    ou continuar são saídas diferentes.

    Este gate exercita a matriz sem KB. O que depende da IDE é localizar o File; o resultado
    dessa busca entra aqui já na forma neutra, que é justamente o desenho que torna a
    precedência testável antes de chegar na IDE.
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
    $gateType = $assembly.GetType($ns + 'ApiPlanOperationJournalGate', $true, $false)
    $inputType = $assembly.GetType($ns + 'ApiPlanOperationJournalGateInput', $true, $false)
    $authType = $assembly.GetType($ns + 'ApiPlanOperationJournalContinuationAuthorization', $true, $false)
    $reasonCodesType = $assembly.GetType($ns + 'JournalGateReasonCodes', $true, $false)
    $lookupStateType = $assembly.GetType($ns + 'JournalGateLookupState', $true, $false)
    $checkpointsType = $assembly.GetType($ns + 'ApiPlanOperationJournalCheckpoints', $true, $false)
    $plansType = $assembly.GetType($ns + 'ApiPlanOperationJournalPlans', $true, $false)
    $kindType = $assembly.GetType($ns + 'JournalOperationKind', $true, $false)
    $stateType = $assembly.GetType($ns + 'JournalOperationState', $true, $false)
    $intentType = $assembly.GetType($ns + 'JournalIntentKind', $true, $false)
    $durabilityType = $assembly.GetType($ns + 'JournalDurability', $true, $false)
    $blockReasonType = $assembly.GetType($ns + 'JournalBlockReason', $true, $false)

    $static = [System.Reflection.BindingFlags]'Static, Public'
    $instance = [System.Reflection.BindingFlags]'Instance, Public'
    $evaluate = $gateType.GetMethod('Evaluate', $static)
    $saveUnconfirmed = $gateType.GetMethod('SaveUnconfirmed', $static)
    $continuationUnavailable = $gateType.GetMethod('ContinuationServiceUnavailable', $static)
    $ownerOf = $reasonCodesType.GetMethod('OwnerOf', $static)
    $createPrepared = $checkpointsType.GetMethod('CreatePrepared', $static)
    $promote = $checkpointsType.GetMethod('PromoteToActive', $static)
    $complete = $checkpointsType.GetMethod('Complete', $static)
    $interrupt = $checkpointsType.GetMethod('Interrupt', $static)
    $abandon = $checkpointsType.GetMethod('Abandon', $static)
    $forGeneration = $plansType.GetMethod('ForGeneration', $static)

    function Get-Prop { param($Object, [string]$Name) return $Object.GetType().GetProperty($Name, $instance).GetValue($Object) }
    function Get-Enum { param($Type, [string]$Name) return [Enum]::Parse($Type, $Name) }
    function Get-Const { param($Type, [string]$Name) return [string]$Type.GetField($Name, [System.Reflection.BindingFlags]'Static, Public').GetValue($null) }

    $now = [DateTime]::UtcNow
    $kbGuid = [Guid]'11111111-1111-1111-1111-111111111111'
    $otherKbGuid = [Guid]'aaaaaaaa-1111-1111-1111-111111111111'
    $txGuid = [Guid]'22222222-2222-2222-2222-222222222222'
    $opGuid = [Guid]'33333333-3333-3333-3333-333333333333'
    $appGuid = [Guid]'44444444-4444-4444-4444-444444444444'
    $apiGuid = [Guid]'55555555-5555-5555-5555-555555555555'

    function New-GenerationPlan {
        $args = New-Object object[] 7
        $args[0] = [object]$apiGuid
        $args[1] = 'abc123'
        $args[2] = $true
        $args[3] = $true
        $args[4] = $true
        $args[5] = $true
        $args[6] = [string[]]@('List', 'Get')
        return $forGeneration.Invoke($null, $args)
    }

    function New-Prepared {
        param([Guid]$KnowledgeBase = $kbGuid, [Guid]$Operation = $opGuid)
        $args = New-Object object[] 12
        $args[0] = Get-Enum $kindType 'Apply'
        $args[1] = New-GenerationPlan
        $args[2] = $KnowledgeBase
        $args[3] = $txGuid
        $args[4] = 'Teste'
        $args[5] = $Operation
        $args[6] = $appGuid
        $args[7] = '0.1.0-alpha.7'
        $args[8] = Get-Enum $intentType 'Current'
        $args[9] = 'GOAB_API_METADATA_B060_V3'
        $args[10] = $now
        $args[11] = $null
        return $createPrepared.Invoke($null, $args)
    }

    function New-Envelope {
        # Constrói o envelope no estado pedido, passando pelas transições reais: um estado
        # montado à mão poderia ser um que a máquina de checkpoints nunca produz.
        param([ValidateSet('Prepared', 'Active', 'Completed', 'Partial', 'OutcomeUnknown', 'Abandoned')][string]$State,
              [Guid]$KnowledgeBase = $kbGuid,
              [Guid]$Operation = $opGuid)

        $journal = New-Prepared -KnowledgeBase $KnowledgeBase -Operation $Operation
        switch ($State) {
            'Prepared' { return $journal }
            'Abandoned' {
                [void]$abandon.Invoke($null, @($journal, 'Abandono autorizado', 'ANTONIOJOSE', $now))
                return $journal
            }
        }

        [void]$promote.Invoke($null, @($journal, $now))
        switch ($State) {
            'Active' { return $journal }
            'Completed' { [void]$complete.Invoke($null, @($journal, $now)) }
            'Partial' { [void]$interrupt.Invoke($null, @($journal, (Get-Enum $stateType 'Partial'), (Get-Enum $blockReasonType 'UserAborted'), $now)) }
            'OutcomeUnknown' { [void]$interrupt.Invoke($null, @($journal, (Get-Enum $stateType 'OutcomeUnknown'), (Get-Enum $blockReasonType 'OutcomeUnknown'), $now)) }
        }

        return $journal
    }

    function Invoke-Gate {
        param(
            [string]$LookupState,
            $Envelope = $null,
            [string]$Durability = 'Confirmed',
            [Guid]$KnowledgeBase = $kbGuid,
            [int]$FileId = 0,
            $Authorization = $null,
            [string]$LookupDetail = 'detalhe da busca')

        $gateInput = [Activator]::CreateInstance($inputType)
        $gateInput.LookupState = Get-Enum $lookupStateType $LookupState
        $gateInput.LookupDetail = $LookupDetail
        $gateInput.CurrentEnvelope = $Envelope
        $gateInput.ObservedDurability = Get-Enum $durabilityType $Durability
        $gateInput.KnowledgeBaseGuid = $KnowledgeBase
        $gateInput.JournalFileId = $FileId
        $gateInput.PreparedContinuationAuthorization = $Authorization
        return $evaluate.Invoke($null, @($gateInput))
    }

    function Assert-Diagnostic {
        param($Decision, [string]$Code, [string]$ReasonCode, [string]$Precondition, [string]$Message)
        Assert-Equal 'Blocked' ([string](Get-Prop $Decision 'Outcome')) "$Message — o gate bloqueia"
        Assert-True (-not [bool](Get-Prop $Decision 'CanStart')) "$Message — CanStart é falso"
        $diagnostic = Get-Prop $Decision 'Diagnostic'
        Assert-True ($null -ne $diagnostic) "$Message — o diagnóstico existe"
        Assert-Equal $Code ([string](Get-Prop $diagnostic 'Code')) "$Message — código de alto nível"
        Assert-Equal $ReasonCode ([string](Get-Prop $diagnostic 'ReasonCode')) "$Message — reasonCode estável"
        Assert-Equal $Precondition ([string](Get-Prop $diagnostic 'FailedPrecondition')) "$Message — pré-condição que falhou"
        # O mapeamento fechado precisa concordar com o que foi emitido: é a trava que impede
        # um reasonCode migrar de código de alto nível sem ninguém perceber.
        Assert-Equal $Code ([string]$ownerOf.Invoke($null, @($ReasonCode))) "$Message — OwnerOf concorda com o emitido"
    }

    function Get-Diagnostic {
        param($Decision)
        return (Get-Prop $Decision 'Diagnostic')
    }

    # --- 1. Ausência do diário não é falta -----------------------------------------------------
    # Numa KB que nunca gerou nada o File ainda não existe; esta operação vai criá-lo.
    $absent = Invoke-Gate -LookupState 'Absent'
    Assert-True ([bool](Get-Prop $absent 'CanStart')) 'Diário ausente permite começar'
    Assert-Equal 'Allowed' ([string](Get-Prop $absent 'Outcome')) 'Ausência devolve Allowed'
    Assert-True ($null -eq (Get-Prop $absent 'Diagnostic')) 'Allowed não carrega diagnóstico'

    # --- 2. Disponibilidade e integridade (pré-condição 2) -------------------------------------
    Assert-Diagnostic (Invoke-Gate -LookupState 'Ambiguous') `
        'JournalUnavailable' (Get-Const $reasonCodesType 'JournalDuplicate') 'JournalAvailable' `
        'Dois Files com o nome do diário'

    Assert-Diagnostic (Invoke-Gate -LookupState 'Unreadable') `
        'JournalUnavailable' (Get-Const $reasonCodesType 'JournalInvalid') 'JournalAvailable' `
        'Conteúdo do diário ilegível'

    # Description alheia é divergência de identidade, não conteúdo inválido: o File
    # encontrado não é o diário.
    Assert-Diagnostic (Invoke-Gate -LookupState 'ExternalCollision') `
        'JournalUnavailable' (Get-Const $reasonCodesType 'JournalIdentityDivergent') 'JournalIdentityConfirmed' `
        'Colisão externa no nome do diário'

    # --- 3. Identidade do envelope (pré-condição 3) --------------------------------------------
    # O envelope está Completed e durável: sem a checagem de KB, isto liberaria a operação.
    # Um diário copiado entre KBs passava até a P3.
    $foreign = New-Envelope -State 'Completed' -KnowledgeBase $otherKbGuid
    $foreignDecision = Invoke-Gate -LookupState 'Found' -Envelope $foreign -FileId 84
    Assert-Diagnostic $foreignDecision `
        'JournalUnavailable' (Get-Const $reasonCodesType 'JournalIdentityDivergent') 'JournalIdentityConfirmed' `
        'Diário de outra KB'
    $foreignDiagnostic = Get-Diagnostic $foreignDecision
    Assert-Contains ([string](Get-Prop $foreignDiagnostic 'Message')) ([string]$otherKbGuid) 'A mensagem nomeia a KB do envelope'

    # --- 4. Durabilidade prevalece sobre o estado global ----------------------------------------
    # O envelope é Partial — estado que sozinho daria GateBlocked. Com a durabilidade em
    # dúvida, o diagnóstico correto é o outro: não se sabe se o Partial é o estado real.
    $partial = New-Envelope -State 'Partial'
    Assert-Diagnostic (Invoke-Gate -LookupState 'Found' -Envelope $partial -Durability 'Unknown') `
        'DurabilityUnknown' (Get-Const $reasonCodesType 'JournalReloadDivergent') 'JournalIdentityConfirmed' `
        'Durabilidade não confirmada com envelope Partial'

    # --- 5. Estados terminais liberam -----------------------------------------------------------
    foreach ($terminal in @('Completed', 'Abandoned')) {
        $decision = Invoke-Gate -LookupState 'Found' -Envelope (New-Envelope -State $terminal)
        Assert-True ([bool](Get-Prop $decision 'CanStart')) "Envelope $terminal libera a próxima operação"
    }

    # --- 6. Estado global bloqueante (GateBlocked) ----------------------------------------------
    # Partial: intenção anterior que ficou pela metade (pré-condição 1).
    $partialDecision = Invoke-Gate -LookupState 'Found' -Envelope $partial -FileId 137
    Assert-Diagnostic $partialDecision `
        'GateBlocked' (Get-Const $reasonCodesType 'JournalNonTerminal') 'PriorIntentReconciled' `
        'Envelope Partial durável'
    $partialDiagnostic = Get-Diagnostic $partialDecision
    Assert-Contains ([string](Get-Prop $partialDiagnostic 'Message')) 'não é terminal' 'A mensagem humana explica o estado'

    # Active/Running: intenção ativa (pré-condição 4). Mesma reason, pré-condição diferente.
    Assert-Diagnostic (Invoke-Gate -LookupState 'Found' -Envelope (New-Envelope -State 'Active')) `
        'GateBlocked' (Get-Const $reasonCodesType 'JournalNonTerminal') 'NoActiveIntent' `
        'Envelope ativo'

    # OutcomeUnknown tem razão própria: a saída é consultar a KB por identidade, não continuar.
    Assert-Diagnostic (Invoke-Gate -LookupState 'Found' -Envelope (New-Envelope -State 'OutcomeUnknown')) `
        'GateBlocked' (Get-Const $reasonCodesType 'UnreconciledOutcome') 'NoUnreconciledOutcome' `
        'Resultado indeterminado não reconciliado'

    # --- 7. `Prepared` é o único não terminal que admite continuação ----------------------------
    $prepared = New-Envelope -State 'Prepared'
    Assert-Diagnostic (Invoke-Gate -LookupState 'Found' -Envelope $prepared) `
        'GateBlocked' (Get-Const $reasonCodesType 'PreparedContinuationNotAuthorized') 'NoActiveIntent' `
        'Envelope preparado sem autorização'

    # Autorização de outra operação: entre o consentimento e agora, o envelope mudou.
    $staleAuth = [Activator]::CreateInstance($authType, @([Guid]'99999999-9999-9999-9999-999999999999', 'ANTONIOJOSE'))
    Assert-Diagnostic (Invoke-Gate -LookupState 'Found' -Envelope $prepared -Authorization $staleAuth) `
        'GateBlocked' (Get-Const $reasonCodesType 'RecoveryAuthorizationStale') 'NoActiveIntent' `
        'Autorização de continuação obsoleta'

    # Autorização coerente: o gate reconhece, mas isto não é «pode começar uma operação nova».
    # Continuar preservando operationId e applicationId é serviço da P5/P6.
    $goodAuth = [Activator]::CreateInstance($authType, @($opGuid, 'ANTONIOJOSE'))
    $continuation = Invoke-Gate -LookupState 'Found' -Envelope $prepared -Authorization $goodAuth
    Assert-Equal 'ContinuationAuthorized' ([string](Get-Prop $continuation 'Outcome')) 'Autorização coerente é reconhecida'
    Assert-True (-not [bool](Get-Prop $continuation 'CanStart')) 'Continuação autorizada não é operação nova'
    Assert-True ($null -ne (Get-Prop $continuation 'CurrentEnvelope')) 'A continuação devolve o envelope a continuar'

    # --- 8. Diagnósticos construídos fora da avaliação -------------------------------------------
    $unconfirmed = $saveUnconfirmed.Invoke($null, @(86, 'O Save() do diário lançou.'))
    Assert-Equal 'DurabilityUnknown' ([string](Get-Prop $unconfirmed 'Code')) 'Gravação não confirmada é DurabilityUnknown'
    Assert-Equal (Get-Const $reasonCodesType 'JournalSaveUnconfirmed') ([string](Get-Prop $unconfirmed 'ReasonCode')) 'A razão da gravação não confirmada é própria'

    $serviceMissing = $continuationUnavailable.Invoke($null, @($prepared))
    Assert-Equal 'GateBlocked' ([string](Get-Prop $serviceMissing 'Code')) 'Serviço de continuação ausente é GateBlocked'
    Assert-Equal (Get-Const $reasonCodesType 'RecoveryAuthorizationLockUnavailable') ([string](Get-Prop $serviceMissing 'ReasonCode')) 'A razão nomeia o mecanismo ausente'

    # --- 9. Contexto estruturado, não texto livre -------------------------------------------------
    $context = Get-Prop $partialDiagnostic 'Context'
    $map = @{}
    foreach ($pair in $context) { $map[$pair.Key] = $pair.Value }
    foreach ($key in @('lookupState', 'journalFileId', 'operationId', 'operationKind', 'envelopePhase', 'operationState', 'logicalStage', 'blockReason', 'journalDurability')) {
        Assert-True ($map.ContainsKey($key)) "O contexto carrega '$key'"
    }
    Assert-Equal '137' $map['journalFileId'] 'O contexto vincula o FileId do diário'
    Assert-Equal 'UserAborted' $map['blockReason'] 'O contexto reexpõe o blockReason já persistido'
    Assert-Equal ([string]$opGuid) $map['operationId'] 'O contexto identifica a operação bloqueante'

    $describe = [string]$partialDiagnostic.GetType().GetMethod('Describe', $instance).Invoke($partialDiagnostic, @())
    Assert-Contains $describe 'GateBlocked/JournalNonTerminal' 'A linha da Output abre com código e razão'
    Assert-Contains $describe 'journalFileId=137' 'A linha da Output leva o contexto'

    # --- 10. `PreconditionFailed` não é subcausa de `GateBlocked` ---------------------------------
    # A decisão 24 é explícita nisso. A trava é dupla: cada razão de pré-condição pertence ao
    # código próprio, e nenhuma delas é emitida por um caminho que produza GateBlocked.
    foreach ($reason in @('IdentityAmbiguous', 'IdentityDivergent', 'InventoryInsufficient', 'AuthorizationMismatch')) {
        $code = [string]$ownerOf.Invoke($null, @((Get-Const $reasonCodesType $reason)))
        Assert-Equal 'PreconditionFailed' $code "A razão '$reason' pertence a PreconditionFailed"
    }

    foreach ($reason in @('JournalNonTerminal', 'PreparedContinuationNotAuthorized', 'UnreconciledOutcome', 'RecoveryAuthorizationStale', 'RecoveryAuthorizationLockUnavailable')) {
        $code = [string]$ownerOf.Invoke($null, @((Get-Const $reasonCodesType $reason)))
        Assert-Equal 'GateBlocked' $code "A razão '$reason' pertence a GateBlocked"
    }

    foreach ($reason in @('JournalMissing', 'JournalInvalid', 'JournalDuplicate', 'JournalIdentityDivergent')) {
        $code = [string]$ownerOf.Invoke($null, @((Get-Const $reasonCodesType $reason)))
        Assert-Equal 'JournalUnavailable' $code "A razão '$reason' pertence a JournalUnavailable"
    }

    foreach ($reason in @('JournalSaveUnconfirmed', 'JournalReloadDivergent')) {
        $code = [string]$ownerOf.Invoke($null, @((Get-Const $reasonCodesType $reason)))
        Assert-Equal 'DurabilityUnknown' $code "A razão '$reason' pertence a DurabilityUnknown"
    }

    # Razão fora do mapeamento fechado não é aceita em silêncio.
    $rejected = $false
    try {
        [void]$ownerOf.Invoke($null, @('MotivoInventado'))
    } catch {
        $rejected = $true
        Assert-Contains ([string]$_.Exception.InnerException.Message) 'fora do mapeamento fechado' 'A recusa explica o contrato'
    }
    Assert-True $rejected 'Um reasonCode inventado é recusado'

} finally {
    if ($null -ne $script:AssemblyResolveHandler) {
        [System.AppDomain]::CurrentDomain.remove_AssemblyResolve($script:AssemblyResolveHandler)
    }
}

Write-Output 'PASS: ApiPlanOperationJournalGate'
