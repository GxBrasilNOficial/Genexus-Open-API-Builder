#requires -Version 7.4

<#
    S-B111 / F3 — gate offline do schema V1 do diário durável.

    Cobre três contratos que a recuperação inteira assume:

    1. o material canônico é determinístico — round-trip byte a byte, ordem do schema,
       nulos presentes, arrays vazios, GUID em `D` minúsculo e UTC com milissegundos;
    2. o `snapshotHash` é SHA-256 do material canônico e muda quando o envelope muda;
    3. o serializer recusa, antes de qualquer `File.Save()`, todo envelope que viole o
       contrato da decisão 24 — porque um diário inválido é indistinguível de um ausente
       na recuperação, e os dois bloqueiam a operação.
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

# O envelope canônico de referência. Qualquer mudança de ordem, espaçamento ou formato de
# valor quebra este literal — que é exatamente o ponto: o material canônico é contrato.
$canonical = '{"schemaVersion":1,"journalKind":"GOAB_OPERATION_JOURNAL","knowledgeBaseGuid":"11111111-1111-1111-1111-111111111111","transactionGuid":"22222222-2222-2222-2222-222222222222","transactionName":"Teste","operationId":"33333333-3333-3333-3333-333333333333","applicationId":"44444444-4444-4444-4444-444444444444","operationKind":"Apply","generatorVersion":"0.1.0-alpha.7","createdUtc":"2026-09-14T10:00:00.000Z","updatedUtc":"2026-09-14T10:00:01.500Z","envelopePhase":"Active","operationState":"Running","logicalStage":"ApiPhysicallySaved","journalDurability":"Confirmed","intentKind":"Current","metadataSchemaVersion":"GOAB_API_METADATA_B060_V3","plan":{"planKind":"Generation","plannedApiGuid":"55555555-5555-5555-5555-555555555555","contractHash":"abc123","generateApiObject":true,"generateSdts":true,"generateProcedures":true,"generateMetadata":true,"services":["List","Get"]},"inventory":[{"objectType":"ApiObject","identityKind":"Guid","guid":"55555555-5555-5555-5555-555555555555","fileId":null,"composite":null,"emptyConfirmed":null,"name":"apiTeste","ownershipValidated":true,"action":"Create","physicalState":"Present","confirmation":"Confirmed","expectedHash":null,"receiptSequences":[1]},{"objectType":"MetadataFile","identityKind":"FileId","guid":null,"fileId":42,"composite":null,"emptyConfirmed":null,"name":"apiTeste_Metadata","ownershipValidated":true,"action":"Create","physicalState":"Unknown","confirmation":"NotAttempted","expectedHash":"deadbeef","receiptSequences":[]}],"receipts":[{"sequence":1,"operation":"Save","stage":"B055","objectType":"ApiObject","attempt":1,"retryOfSequence":null,"attemptState":"Finished","result":"Confirmed","confirmation":"Confirmed","physicalState":"Present","retryEligible":false,"retryableReason":null}],"abandonment":null,"blockReason":null}'

$script:AssemblyResolveHandler = $null
try {
    Initialize-GeneXusAssemblyResolver -SearchDirectories (Get-AssemblyDirectoryCandidates -GeneXusRoot $GeneXusDirectory)
    $assembly = [System.Reflection.Assembly]::LoadFrom($DllPath)
    $journalType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanOperationJournal', $true, $false)
    $serializerType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanOperationJournalSerializer', $true, $false)
    $validatorType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanOperationJournalValidator', $true, $false)
    Assert-True ($null -ne $journalType) 'ApiPlanOperationJournal não encontrado.'
    Assert-True ($null -ne $serializerType) 'ApiPlanOperationJournalSerializer não encontrado.'
    Assert-True ($null -ne $validatorType) 'ApiPlanOperationJournalValidator não encontrado.'

    $readMethod = $serializerType.GetMethod('Read', [System.Reflection.BindingFlags]'Static, Public')
    $serializeMethod = $serializerType.GetMethod('Serialize', [System.Reflection.BindingFlags]'Static, Public')
    $hashOfJournal = $serializerType.GetMethod('ComputeSnapshotHash', [System.Reflection.BindingFlags]'Static, Public', $null, @($journalType), $null)
    $hashOfText = $serializerType.GetMethod('ComputeSnapshotHash', [System.Reflection.BindingFlags]'Static, Public', $null, @([string]), $null)

    function Read-Journal {
        param([string]$Json)
        return $readMethod.Invoke($null, @($Json))
    }

    # --- 1. Identidade fixa do schema ------------------------------------------------------
    Assert-Equal 1 ($journalType.GetField('SchemaVersion', [System.Reflection.BindingFlags]'Static, Public').GetValue($null)) 'SchemaVersion do diário'
    Assert-Equal 'GOAB_OPERATION_JOURNAL' ($journalType.GetField('JournalKind', [System.Reflection.BindingFlags]'Static, Public').GetValue($null)) 'JournalKind'
    Assert-Equal 'GxOpenApiBuilder_OperationJournal' ($journalType.GetField('JournalObjectName', [System.Reflection.BindingFlags]'Static, Public').GetValue($null)) 'Nome lógico fixo do File'
    Assert-Equal 'GxOpenApiBuilder_OperationJournal.json' ($journalType.GetField('JournalExternalFileName', [System.Reflection.BindingFlags]'Static, Public').GetValue($null)) 'Nome do arquivo externo'

    # --- 2. Round-trip byte a byte ---------------------------------------------------------
    $result = Read-Journal $canonical
    Assert-True ([bool]$result.IsValid) "O envelope de referência deve ser válido. Erros: $($result.Describe())"
    $reserialized = [string]$serializeMethod.Invoke($null, @($result.Journal))
    Assert-Equal $canonical $reserialized 'A reserialização deve reproduzir o material canônico byte a byte.'
    Assert-Equal $canonical $result.CanonicalJson 'A leitura deve devolver o material canônico.'

    # Formato: sem espaços supérfluos, nulos presentes, arrays vazios preservados.
    Assert-True ($canonical.IndexOf(': ', [StringComparison]::Ordinal) -lt 0) 'O material canônico não tem espaço após os dois-pontos.'
    Assert-Contains $canonical '"fileId":null' 'Campo anulável permanece presente como null.'
    Assert-Contains $canonical '"receiptSequences":[]' 'Array vazio permanece como [].'
    Assert-Contains $canonical '"createdUtc":"2026-09-14T10:00:00.000Z"' 'Timestamp com milissegundos fixos e sufixo Z.'

    # --- 3. Hash do snapshot ---------------------------------------------------------------
    $hash = [string]$hashOfJournal.Invoke($null, @($result.Journal))
    Assert-Equal 64 $hash.Length 'O snapshotHash é SHA-256 em hexadecimal.'
    Assert-Equal $hash ($hash.ToLowerInvariant()) 'O snapshotHash é hexadecimal minúsculo.'
    Assert-Equal $hash ([string]$hashOfText.Invoke($null, @($canonical))) 'O hash do envelope e o do material canônico coincidem.'
    Assert-Equal $hash ([string]$result.SnapshotHash) 'O resultado da leitura expõe o mesmo hash.'

    $mutated = Read-Journal ($canonical.Replace('"contractHash":"abc123"', '"contractHash":"abc124"'))
    Assert-True ([bool]$mutated.IsValid) 'O envelope mutado ainda é válido.'
    Assert-True ($hash -ne [string]$mutated.SnapshotHash) 'Mudar o envelope muda o snapshotHash.'

    # --- 4. Versão e identidade do schema ---------------------------------------------------
    # Versão desconhecida bloqueia operação e recuperação; não há migração automática.
    $wrongVersion = Read-Journal ($canonical.Replace('"schemaVersion":1', '"schemaVersion":2'))
    Assert-True (-not [bool]$wrongVersion.IsValid) 'schemaVersion desconhecida deve bloquear.'
    Assert-Contains $wrongVersion.Describe() 'schemaVersion desconhecida' 'A mensagem deve nomear a versão desconhecida.'

    $wrongKind = Read-Journal ($canonical.Replace('GOAB_OPERATION_JOURNAL', 'GOAB_OUTRO'))
    Assert-True (-not [bool]$wrongKind.IsValid) 'journalKind divergente deve bloquear.'

    $brokenJson = Read-Journal '{'
    Assert-True (-not [bool]$brokenJson.IsValid) 'JSON inválido deve bloquear sem exceção de fluxo.'
    Assert-Contains $brokenJson.Describe() 'JSON inválido' 'A leitura deve reportar JSON inválido como diagnóstico.'

    # --- 5. Regras de envelope, estado e bloqueio -------------------------------------------
    $cases = @(
        @{
            Name    = 'Prepared com operação em curso'
            Json    = $canonical.Replace('"envelopePhase":"Active"', '"envelopePhase":"Prepared"')
            Needle  = 'envelopePhase=Prepared exige operationState=Pending'
        },
        @{
            Name    = 'Pending com estágio de pipeline'
            Json    = $canonical.Replace('"operationState":"Running"', '"operationState":"Pending"')
            Needle  = 'operationState=Pending admite apenas logicalStage'
        },
        @{
            Name    = 'Removed fora de Remove'
            Json    = $canonical.Replace('"operationState":"Running"', '"operationState":"Removed"').Replace('"logicalStage":"ApiPhysicallySaved"', '"logicalStage":"Removed"')
            Needle  = 'operationState=Removed pertence somente a operationKind=Remove'
        },
        @{
            Name    = 'OutcomeUnknown sem blockReason'
            Json    = $canonical.Replace('"operationState":"Running"', '"operationState":"OutcomeUnknown"')
            Needle  = 'exige blockReason'
        },
        @{
            Name    = 'blockReason em estado normal'
            Json    = $canonical.Replace('"blockReason":null', '"blockReason":"OutcomeUnknown"')
            Needle  = 'blockReason só é persistido'
        },
        @{
            Name    = 'blockReason desconhecido'
            Json    = $canonical.Replace('"blockReason":null', '"blockReason":"QualquerCoisa"')
            Needle  = 'blockReason deve ser um valor conhecido'
        },
        @{
            Name    = 'abandonment sem estágio de abandono'
            Json    = $canonical.Replace('"abandonment":null', '"abandonment":{"reason":"teste","authorizedUtc":"2026-09-14T10:00:00.000Z","authorizedBy":"ANTONIOJOSE"}')
            Needle  = 'abandonment só é válido com logicalStage=Abandoned'
        },
        @{
            Name    = 'operationId igual ao applicationId'
            Json    = $canonical.Replace('"applicationId":"44444444-4444-4444-4444-444444444444"', '"applicationId":"33333333-3333-3333-3333-333333333333"')
            Needle  = 'devem ser distintos'
        },
        @{
            Name    = 'updatedUtc anterior a createdUtc'
            Json    = $canonical.Replace('"updatedUtc":"2026-09-14T10:00:01.500Z"', '"updatedUtc":"2026-09-14T09:00:00.000Z"')
            Needle  = 'updatedUtc não pode ser anterior'
        },
        @{
            Name    = 'timestamp fora do formato canônico'
            Json    = $canonical.Replace('"createdUtc":"2026-09-14T10:00:00.000Z"', '"createdUtc":"2026-09-14T10:00:00Z"')
            Needle  = 'createdUtc deve seguir'
        },
        @{
            Name    = 'plano de geração sem identidade da API'
            Json    = $canonical.Replace('"plannedApiGuid":"55555555-5555-5555-5555-555555555555","contractHash"', '"plannedApiGuid":null,"contractHash"')
            Needle  = 'plan.plannedApiGuid é obrigatório em Apply e Sync'
        },
        @{
            Name    = 'plano de geração sem flag'
            Json    = $canonical.Replace('"generateSdts":true', '"generateSdts":null')
            Needle  = 'plan.generateSdts é obrigatório'
        },
        @{
            Name    = 'planKind incompatível com a operação'
            Json    = $canonical.Replace('"planKind":"Generation"', '"planKind":"Removal"')
            Needle  = 'plan.planKind incompatível com operationKind=Apply'
        },
        @{
            Name    = 'Recovery sem intenção importada'
            Json    = $canonical.Replace('"operationKind":"Apply"', '"operationKind":"Recovery"').Replace('"planKind":"Generation"', '"planKind":"MetadataRecovery"')
            Needle  = 'operationKind=Recovery exige intentKind=Imported'
        },
        @{
            Name    = 'metadata no inventário sem metadataSchemaVersion'
            Json    = $canonical.Replace('"metadataSchemaVersion":"GOAB_API_METADATA_B060_V3"', '"metadataSchemaVersion":null')
            Needle  = 'metadataSchemaVersion é obrigatório'
        },
        @{
            Name    = 'metadataSchemaVersion desconhecida'
            Json    = $canonical.Replace('GOAB_API_METADATA_B060_V3', 'GOAB_API_METADATA_B060_V9')
            Needle  = 'metadataSchemaVersion desconhecida'
        },
        @{
            Name    = 'FileId sem hash esperado'
            Json    = $canonical.Replace('"expectedHash":"deadbeef"', '"expectedHash":null')
            Needle  = 'identityKind=FileId exige expectedHash'
        },
        @{
            Name    = 'recibo referenciado que não existe'
            Json    = $canonical.Replace('"receiptSequences":[1]', '"receiptSequences":[7]')
            Needle  = 'referencia um recibo inexistente'
        },
        @{
            Name    = 'ação fora do domínio da operação'
            Json    = $canonical.Replace('"action":"Create","physicalState":"Present"', '"action":"Delete","physicalState":"Present"')
            Needle  = 'não pertence ao domínio de operationKind=Apply'
        },
        @{
            Name    = 'retryEligible sem evidência de presença'
            Json    = $canonical.Replace('"retryEligible":false,"retryableReason":null', '"retryEligible":true,"retryableReason":"StillPresentAfterDelete"')
            Needle  = 'retryEligible=true exige Delete, Failed, Present'
        },
        @{
            Name    = 'motivo de retry sem retryEligible'
            Json    = $canonical.Replace('"retryableReason":null', '"retryableReason":"StillPresentAfterDelete"')
            Needle  = 'retryableReason só existe com retryEligible=true'
        },
        @{
            Name    = 'attempt não positivo'
            Json    = $canonical.Replace('"attempt":1', '"attempt":0')
            Needle  = 'receipts[].attempt deve ser inteiro positivo'
        }
    )

    foreach ($case in $cases) {
        $caseResult = Read-Journal ([string]$case.Json)
        Assert-True (-not [bool]$caseResult.IsValid) "Deveria bloquear: $($case.Name)."
        Assert-Contains $caseResult.Describe() ([string]$case.Needle) "Mensagem esperada para: $($case.Name)."
    }

    # --- 6. Remove: fila destrutiva, Partial e orçamento ------------------------------------
    $removeJson = '{"schemaVersion":1,"journalKind":"GOAB_OPERATION_JOURNAL","knowledgeBaseGuid":"11111111-1111-1111-1111-111111111111","transactionGuid":"22222222-2222-2222-2222-222222222222","transactionName":"Teste","operationId":"33333333-3333-3333-3333-333333333333","applicationId":"44444444-4444-4444-4444-444444444444","operationKind":"Remove","generatorVersion":"0.1.0-alpha.7","createdUtc":"2026-09-14T10:00:00.000Z","updatedUtc":"2026-09-14T10:00:02.000Z","envelopePhase":"Active","operationState":"Partial","logicalStage":"RemovalPartial","journalDurability":"Confirmed","intentKind":"Current","metadataSchemaVersion":null,"plan":{"planKind":"Removal","plannedApiGuid":"55555555-5555-5555-5555-555555555555","contractHash":"abc123","generateApiObject":null,"generateSdts":null,"generateProcedures":null,"generateMetadata":null,"services":[]},"inventory":[{"objectType":"ApiObject","identityKind":"Guid","guid":"55555555-5555-5555-5555-555555555555","fileId":null,"composite":null,"emptyConfirmed":null,"name":"apiTeste","ownershipValidated":true,"action":"Delete","physicalState":"Absent","confirmation":"Absent","expectedHash":null,"receiptSequences":[1]},{"objectType":"Sdt","identityKind":"Composite","guid":null,"fileId":null,"composite":{"exactName":"sdtTeste_API_ListFilters","objectTypeName":"SDT","role":"ListFilters","canonicalDescription":"apiTeste_Metadata","transactionGuid":"22222222-2222-2222-2222-222222222222","apiGuid":"55555555-5555-5555-5555-555555555555"},"emptyConfirmed":null,"name":"sdtTeste_API_ListFilters","ownershipValidated":true,"action":"Delete","physicalState":"Present","confirmation":"Confirmed","expectedHash":null,"receiptSequences":[2]},{"objectType":"Transaction","identityKind":"None","guid":null,"fileId":null,"composite":null,"emptyConfirmed":null,"name":"Teste","ownershipValidated":false,"action":"Preserve","physicalState":"Present","confirmation":"Confirmed","expectedHash":null,"receiptSequences":[]}],"receipts":[{"sequence":1,"operation":"Delete","stage":"B086","objectType":"ApiObject","attempt":1,"retryOfSequence":null,"attemptState":"Finished","result":"Confirmed","confirmation":"Absent","physicalState":"Absent","retryEligible":false,"retryableReason":null},{"sequence":2,"operation":"Delete","stage":"B086","objectType":"Sdt","attempt":1,"retryOfSequence":null,"attemptState":"Finished","result":"Failed","confirmation":"Confirmed","physicalState":"Present","retryEligible":true,"retryableReason":"StillPresentAfterDelete"}],"abandonment":null,"blockReason":"RetryBudgetExhausted"}'
    $remove = Read-Journal $removeJson
    Assert-True ([bool]$remove.IsValid) "O envelope de Remove deve ser válido. Erros: $($remove.Describe())"
    Assert-Equal $removeJson ([string]$serializeMethod.Invoke($null, @($remove.Journal))) 'Remove também reserializa byte a byte.'

    $removeCases = @(
        @{
            Name   = 'Partial de Remove fora de RemovalPartial'
            Json   = $removeJson.Replace('"logicalStage":"RemovalPartial"', '"logicalStage":"RemovalInProgress"')
            Needle = 'operationState=Partial em Remove exige logicalStage=RemovalPartial'
        },
        @{
            Name   = 'Transaction na fila destrutiva'
            Json   = $removeJson.Replace('"name":"Teste","ownershipValidated":false,"action":"Preserve"', '"name":"Teste","ownershipValidated":false,"action":"Delete"')
            Needle = 'a Transaction nunca entra na fila destrutiva'
        },
        @{
            Name   = 'identidade None fora de Preserve'
            Json   = $removeJson.Replace('"objectType":"Transaction","identityKind":"None"', '"objectType":"Folder","identityKind":"None"').Replace('"name":"Teste","ownershipValidated":false,"action":"Preserve"', '"name":"TesteOpenApi","ownershipValidated":true,"action":"Delete"')
            Needle = 'identityKind=None só é permitido em item Preserve'
        },
        @{
            Name   = 'identidade composta incompleta'
            Json   = $removeJson.Replace('"role":"ListFilters"', '"role":" "')
            Needle = 'composite.role é obrigatório'
        },
        @{
            Name   = 'flags de geração no plano de Remove'
            Json   = $removeJson.Replace('"generateSdts":null', '"generateSdts":true')
            Needle = 'as flags de geração não pertencem ao plano de Remove'
        },
        @{
            # P3: o aborto ganhou motivo próprio, e ele é de interrupção deliberada. Quem
            # aborta sabe que abortou — `OutcomeUnknown` diz o contrário.
            Name   = 'aborto do usuário em estado indeterminado'
            Json   = $canonical.Replace('"operationState":"Running"', '"operationState":"OutcomeUnknown"').Replace('"blockReason":null', '"blockReason":"UserAborted"')
            Needle = 'UserAborted exige operationState=Partial'
        },
        @{
            Name   = 'orçamento esgotado fora de Remove'
            Json   = $canonical.Replace('"operationState":"Running"', '"operationState":"Partial"').Replace('"blockReason":null', '"blockReason":"RetryBudgetExhausted"')
            Needle = 'RetryBudgetExhausted pertence ao orçamento de passadas do Remove'
        }
    )

    foreach ($case in $removeCases) {
        $caseResult = Read-Journal ([string]$case.Json)
        Assert-True (-not [bool]$caseResult.IsValid) "Deveria bloquear: $($case.Name)."
        Assert-Contains $caseResult.Describe() ([string]$case.Needle) "Mensagem esperada para: $($case.Name)."
    }

    # O mesmo motivo em Partial é o caminho legítimo: é o que o Apply e o Sync gravam quando
    # o usuário interrompe.
    $userAbortedJson = $canonical.Replace('"operationState":"Running"', '"operationState":"Partial"').Replace('"blockReason":null', '"blockReason":"UserAborted"')
    $userAborted = Read-Journal $userAbortedJson
    Assert-True ([bool]$userAborted.IsValid) "Partial com UserAborted deve ser válido. Erros: $($userAborted.Describe())"

    # --- 7. Abandono explícito de um envelope Prepared --------------------------------------
    $abandonJson = '{"schemaVersion":1,"journalKind":"GOAB_OPERATION_JOURNAL","knowledgeBaseGuid":"11111111-1111-1111-1111-111111111111","transactionGuid":"22222222-2222-2222-2222-222222222222","transactionName":"Teste","operationId":"33333333-3333-3333-3333-333333333333","applicationId":"44444444-4444-4444-4444-444444444444","operationKind":"Apply","generatorVersion":"0.1.0-alpha.7","createdUtc":"2026-09-14T10:00:00.000Z","updatedUtc":"2026-09-14T10:05:00.000Z","envelopePhase":"Prepared","operationState":"Completed","logicalStage":"Abandoned","journalDurability":"Confirmed","intentKind":"Current","metadataSchemaVersion":null,"plan":{"planKind":"Generation","plannedApiGuid":"55555555-5555-5555-5555-555555555555","contractHash":"abc123","generateApiObject":true,"generateSdts":true,"generateProcedures":true,"generateMetadata":true,"services":["List"]},"inventory":[],"receipts":[],"abandonment":{"reason":"Abandono autorizado pelo usuário","authorizedUtc":"2026-09-14T10:05:00.000Z","authorizedBy":"ANTONIOJOSE"},"blockReason":null}'
    $abandon = Read-Journal $abandonJson
    Assert-True ([bool]$abandon.IsValid) "O abandono explícito deve ser válido. Erros: $($abandon.Describe())"
    Assert-Equal $abandonJson ([string]$serializeMethod.Invoke($null, @($abandon.Journal))) 'O abandono também reserializa byte a byte.'

    $abandonCases = @(
        @{
            Name   = 'abandono sem o objeto de disposição'
            Json   = $abandonJson.Replace('"abandonment":{"reason":"Abandono autorizado pelo usuário","authorizedUtc":"2026-09-14T10:05:00.000Z","authorizedBy":"ANTONIOJOSE"}', '"abandonment":null')
            Needle = 'logicalStage=Abandoned exige o objeto abandonment'
        },
        @{
            Name   = 'abandono de envelope já ativo'
            Json   = $abandonJson.Replace('"envelopePhase":"Prepared"', '"envelopePhase":"Active"')
            Needle = 'somente um envelope Prepared pode ser abandonado'
        },
        @{
            Name   = 'abandono com durabilidade desconhecida'
            Json   = $abandonJson.Replace('"journalDurability":"Confirmed"', '"journalDurability":"Unknown"')
            Needle = 'o abandono exige journalDurability=Confirmed'
        },
        @{
            Name   = 'abandono com gravação de negócio'
            Json   = $abandonJson.Replace('"receipts":[]', '"receipts":[{"sequence":1,"operation":"Save","stage":"B055","objectType":"Sdt","attempt":1,"retryOfSequence":null,"attemptState":"Finished","result":"Confirmed","confirmation":"Confirmed","physicalState":"Present","retryEligible":false,"retryableReason":null}]')
            Needle = 'o abandono não admite recibos'
        }
    )

    foreach ($case in $abandonCases) {
        $caseResult = Read-Journal ([string]$case.Json)
        Assert-True (-not [bool]$caseResult.IsValid) "Deveria bloquear: $($case.Name)."
        Assert-Contains $caseResult.Describe() ([string]$case.Needle) "Mensagem esperada para: $($case.Name)."
    }

    # --- 8. O serializer recusa antes do File.Save() ----------------------------------------
    # Um envelope inválido em memória não pode virar bytes: o diário inválido é
    # indistinguível do ausente na recuperação.
    $invalid = Read-Journal $canonical
    $stageProperty = $journalType.GetProperty('LogicalStage', [System.Reflection.BindingFlags]'Instance, Public')
    $stageProperty.SetValue($invalid.Journal, [Enum]::Parse($stageProperty.PropertyType, 'Abandoned'))
    $rejected = $false
    try {
        [void]$serializeMethod.Invoke($null, @($invalid.Journal))
    } catch {
        $rejected = $true
        Assert-Contains ([string]$_.Exception.InnerException.Message) 'viola o schema V1' 'A recusa deve citar o schema V1.'
    }
    Assert-True $rejected 'O serializer deve recusar um envelope inválido antes de gravar.'

} finally {
    if ($null -ne $script:AssemblyResolveHandler) {
        [System.AppDomain]::CurrentDomain.remove_AssemblyResolve($script:AssemblyResolveHandler)
    }
}

Write-Output 'PASS: ApiPlanOperationJournalSchema'
