#requires -Version 7.4

<#
    S-B111 / F3 — gate offline do transporte de recibos e inventário para o diário.

    A F2 produz recibos em memória; a matriz 4.4 manda gravá-los nas fronteiras. Este gate
    cobre a conversão: tipos de objeto, identidades, distinção entre Create e Update, e a
    recusa de um tipo desconhecido — que precisa falhar alto, porque um recibo com tipo
    inventado é pior que recibo nenhum.

    O envelope resultante é validado pelo mesmo validador do schema: se a conversão produzir
    algo fora do contrato, o teste quebra aqui e não na IDE.
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
    if ($Expected -ne $Actual) { throw "ASSERT_EQUAL_FAILED: $Message (expected='$Expected' actual='$Actual')" }
}

function Assert-Contains {
    param([string]$Text, [string]$Needle, [string]$Message)
    if ($Text.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "ASSERT_CONTAINS_FAILED: $Message (needle='$Needle' text='$Text')"
    }
}

# O PowerShell às vezes entrega a exceção original e às vezes a TargetInvocationException
# que a invoca por reflexão; ler só InnerException falha no primeiro caso.
function Get-ErrorMessage {
    param($ErrorRecord)
    $exception = $ErrorRecord.Exception
    while ($null -ne $exception.InnerException) { $exception = $exception.InnerException }
    return [string]$exception.Message
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
    $mapperType = $assembly.GetType($ns + 'ApiPlanOperationJournalReceiptMapper', $true, $false)
    $coreType = $assembly.GetType($ns + 'ApiPlanPersistenceCore', $true, $false)
    $logType = $assembly.GetType($ns + 'ApiPlanPersistenceLog', $true, $false)
    $guidIdentityType = $assembly.GetType($ns + 'GuidIdentity', $true, $false)
    $fileIdentityType = $assembly.GetType($ns + 'FileIdentity', $true, $false)
    $folderIdentityType = $assembly.GetType($ns + 'FolderIdentity', $true, $false)
    $confirmationType = $assembly.GetType($ns + 'PersistenceConfirmation', $true, $false)
    $kindType = $assembly.GetType($ns + 'JournalOperationKind', $true, $false)

    $static = [System.Reflection.BindingFlags]'Static, Public'
    $staticAny = [System.Reflection.BindingFlags]'Static, NonPublic, Public'
    $instance = [System.Reflection.BindingFlags]'Instance, Public'
    $mapReceipts = $mapperType.GetMethod('MapReceipts', $staticAny)
    $buildInventory = $mapperType.GetMethod('BuildInventory', $staticAny)
    $mapObjectType = $mapperType.GetMethod('MapObjectType', $staticAny)

    function Get-Prop { param($Object, [string]$Name) return $Object.GetType().GetProperty($Name, $instance).GetValue($Object) }
    function Get-Enum { param($Type, [string]$Name) return [Enum]::Parse($Type, $Name) }

    # --- 1. Mapa fechado de tipos ------------------------------------------------------------
    $expectedTypes = @{
        'Transaction' = 'Transaction'
        'Folder'      = 'Folder'
        'API'         = 'ApiObject'
        'Procedure'   = 'Procedure'
        'SDT'         = 'Sdt'
        'File'        = 'MetadataFile'
    }
    foreach ($pair in $expectedTypes.GetEnumerator()) {
        $mapped = [string]$mapObjectType.Invoke($null, @([string]$pair.Key))
        Assert-Equal $pair.Value $mapped "Tipo '$($pair.Key)' deve mapear para o schema do diário."
    }

    # Um tipo novo precisa falhar alto: gravar recibo com tipo inventado é pior que não gravar.
    $rejected = $false
    try {
        [void]$mapObjectType.Invoke($null, @('WebPanel'))
    } catch {
        $rejected = $true
        Assert-Contains (Get-ErrorMessage $_) 'sem correspondência no schema do diário' 'A recusa deve nomear o problema.'
    }
    Assert-True $rejected 'Tipo de objeto desconhecido deve ser recusado.'

    # --- 2. Recibos reais, produzidos pelo log da F2 ------------------------------------------
    # Usa o próprio ApiPlanPersistenceLog para criar e completar os recibos: são os mesmos
    # objetos que o seam produz em runtime, sem precisar montar delegates em PowerShell.
    $log = [Activator]::CreateInstance($logType)
    $instanceAny = [System.Reflection.BindingFlags]'Instance, NonPublic, Public'
    $startReceipt = $logType.GetMethod('StartReceipt', $instanceAny)
    $completeReceipt = $logType.GetMethod('CompleteReceipt', $instanceAny)
    $confirmedFactory = $confirmationType.GetMethod('Confirmed', $static)
    $attemptStateType = $assembly.GetType($ns + 'PersistenceAttemptState', $true, $false)
    $outcomeType = $assembly.GetType($ns + 'PersistenceOutcome', $true, $false)

    $sdtGuid = [Guid]'aaaaaaaa-0000-0000-0000-000000000001'
    $apiGuid = [Guid]'aaaaaaaa-0000-0000-0000-000000000002'
    $fileGuid = [Guid]'aaaaaaaa-0000-0000-0000-000000000003'

    $targets = @(
        @{ Type = 'SDT'; Name = 'sdtTeste_API_Response'; Stage = 'B040-B046'; Identity = [Activator]::CreateInstance($guidIdentityType, @([object]$sdtGuid)) },
        @{ Type = 'API'; Name = 'apiTeste'; Stage = 'B054'; Identity = [Activator]::CreateInstance($guidIdentityType, @([object]$apiGuid)) },
        @{ Type = 'File'; Name = 'apiTeste_Metadata'; Stage = 'B060'; Identity = [Activator]::CreateInstance($fileIdentityType, @([object]$fileGuid, 'apiTeste_Metadata', 'deadbeef')) },
        @{ Type = 'Folder'; Name = 'TesteOpenApi'; Stage = 'Folder'; Identity = [Activator]::CreateInstance($folderIdentityType, @([object]'TesteOpenApi', [object]$true, [object]$false)) }
    )

    foreach ($target in $targets) {
        $receipt = $startReceipt.Invoke($log, @('Save', [string]$target.Type, [string]$target.Stage, [string]$target.Name, $target.Identity, 1))
        $observation = $confirmedFactory.Invoke($null, @('observado', 'teste'))
        [void]$completeReceipt.Invoke($log, @(
            $receipt,
            (Get-Enum $attemptStateType 'Finished'),
            (Get-Enum $outcomeType 'Confirmed'),
            $observation,
            $null,
            $true))
    }

    # Acesso direto: o return de uma função PowerShell desembrulha a coleção e o tipo
    # genérico exigido pela assinatura se perde.
    $receipts = $log.Receipts
    Assert-Equal 4 ([int]@($receipts).Count) 'O log deve ter quatro recibos.'

    # @() expandiria a coleção em vários argumentos; o array precisa ser montado explicitamente.
    $mapArgs = New-Object object[] 1
    $mapArgs[0] = $receipts
    $mapped = $mapReceipts.Invoke($null, $mapArgs)
    Assert-Equal 4 ([int]@($mapped).Count) 'Todos os recibos devem ser transportados.'
    $first = $mapped[0]
    Assert-Equal 1 ([int](Get-Prop $first 'Sequence')) 'A sequência é preservada.'
    Assert-Equal 'Save' ([string](Get-Prop $first 'Operation')) 'A operação é preservada.'
    Assert-Equal 'Sdt' ([string](Get-Prop $first 'ObjectType')) 'O tipo é convertido para o schema.'
    Assert-Equal 'Confirmed' ([string](Get-Prop $first 'Result')) 'O resultado observado é preservado.'
    Assert-Equal $false ([bool](Get-Prop $first 'RetryEligible')) 'Save nunca é retryable.'

    # Decisão 29: o transporte F2→diário leva os três campos de tempo. Sem isso, o schema
    # passa verde com recibo mudo e a auditabilidade da duração some no File.
    foreach ($item in $mapped) {
        $started = [DateTime](Get-Prop $item 'StartedUtc')
        Assert-True ($started -ne [DateTime]::MinValue) 'startedUtc precisa sair do mapper com valor medido.'
        $ended = Get-Prop $item 'EndedUtc'
        Assert-True ($null -ne $ended) 'endedUtc precisa sair do mapper quando o recibo terminou.'
        Assert-True (([DateTime]$ended) -ge $started) 'endedUtc não pode anteceder startedUtc.'
        Assert-True (([long](Get-Prop $item 'DurationMs')) -ge 0) 'durationMs nunca é negativo.'
    }

    # --- 3. Inventário: Create x Update pela lista de criados ---------------------------------
    $created = [string[]]@('sdtTeste_API_Response', 'apiTeste')
    $inventoryArgs = New-Object object[] 3
    $inventoryArgs[0] = $receipts
    $inventoryArgs[1] = $created
    $inventoryArgs[2] = Get-Enum $kindType 'Apply'
    $inventory = $buildInventory.Invoke($null, $inventoryArgs)
    Assert-Equal 4 ([int]@($inventory).Count) 'Cada alvo tocado entra uma vez no inventário.'

    $byName = @{}
    foreach ($item in $inventory) { $byName[[string](Get-Prop $item 'Name')] = $item }

    Assert-Equal 'Create' ([string](Get-Prop $byName['sdtTeste_API_Response'] 'Action')) 'Alvo na lista de criados entra como Create.'
    Assert-Equal 'Create' ([string](Get-Prop $byName['apiTeste'] 'Action')) 'O API criado entra como Create.'
    Assert-Equal 'Update' ([string](Get-Prop $byName['apiTeste_Metadata'] 'Action')) 'Alvo fora da lista de criados entra como Update.'

    Assert-Equal 'Guid' ([string](Get-Prop $byName['apiTeste'] 'IdentityKind')) 'Identidade por GUID é preservada.'
    Assert-Equal 'Guid' ([string](Get-Prop $byName['apiTeste_Metadata'] 'IdentityKind')) 'File é identificado pelo GUID que a F2 usa.'
    Assert-Equal 'deadbeef' ([string](Get-Prop $byName['apiTeste_Metadata'] 'ExpectedHash')) 'O hash esperado do File acompanha o item.'
    Assert-Equal 'Folder' ([string](Get-Prop $byName['TesteOpenApi'] 'IdentityKind')) 'Folder mantém a identidade própria.'

    # --- 3.1 Reencontro: identidade composta sem GUIDs e dois recibos do mesmo alvo ----------
    # Cenário medido na IDE em 2026-09-14, no segundo Apply da `Escola`: o writer de Business
    # Component identifica a Procedure por CompositeIdentity, e no reencontro os GUIDs podem
    # chegar vazios. Exigi-los num Save era regra indevida — a identidade histórica completa
    # só é exigência de quem autoriza exclusão.
    $compositeType = $assembly.GetType($ns + 'CompositeIdentity', $true, $false)
    $log2 = [Activator]::CreateInstance($logType)
    $emptyGuid = [Guid]::Empty
    $transactionGuid = [Guid]'22222222-2222-2222-2222-222222222222'

    # O mesmo alvo aparece duas vezes, com identidades diferentes: antes e depois de o objeto
    # existir. O inventário precisa colapsar os dois num item só. O papel canônico é Get
    # (decisão 30 / ApiPlanJournalRoles), não um literal livre.
    foreach ($identity in @(
        [Activator]::CreateInstance($compositeType, @([object]'procEscola_API_Get', [object]'Procedure', [object]'Get', [object]'', [object]$transactionGuid, [object]$emptyGuid)),
        [Activator]::CreateInstance($compositeType, @([object]'procEscola_API_Get', [object]'Procedure', [object]'Get', [object]'apiEscola', [object]$transactionGuid, [object]$emptyGuid))
    )) {
        $receipt = $startReceipt.Invoke($log2, @('Save', 'Procedure', 'B071-B073/B079', 'procEscola_API_Get', $identity, 1))
        $observation = $confirmedFactory.Invoke($null, @('observado', 'teste'))
        [void]$completeReceipt.Invoke($log2, @($receipt, (Get-Enum $attemptStateType 'Finished'), (Get-Enum $outcomeType 'Confirmed'), $observation, $null, $true))
    }

    $reencounterArgs = New-Object object[] 3
    $reencounterArgs[0] = $log2.Receipts
    $reencounterArgs[1] = [string[]]@()
    $reencounterArgs[2] = Get-Enum $kindType 'Apply'
    $reencounterInventory = $buildInventory.Invoke($null, $reencounterArgs)
    Assert-Equal 1 ([int]@($reencounterInventory).Count) 'Dois recibos do mesmo alvo produzem um item de inventário.'
    $reencounterItem = @($reencounterInventory)[0]
    Assert-Equal 'Update' ([string](Get-Prop $reencounterItem 'Action')) 'No reencontro o alvo entra como Update.'
    Assert-Equal 2 ([int]@(Get-Prop $reencounterItem 'ReceiptSequences').Count) 'O item acumula as duas sequências.'
    $reencounterComposite = Get-Prop $reencounterItem 'Composite'
    Assert-True ($null -ne $reencounterComposite) 'Identidade composta precisa sobreviver no inventário.'
    Assert-Equal 'Get' ([string](Get-Prop $reencounterComposite 'Role')) 'composite.role canônico precisa atravessar o mapper.'

    # --- 4. O envelope resultante tem de ser válido -------------------------------------------
    $checkpointsType = $assembly.GetType($ns + 'ApiPlanOperationJournalCheckpoints', $true, $false)
    $plansType = $assembly.GetType($ns + 'ApiPlanOperationJournalPlans', $true, $false)
    $intentType = $assembly.GetType($ns + 'JournalIntentKind', $true, $false)
    $validatorType = $assembly.GetType($ns + 'ApiPlanOperationJournalValidator', $true, $false)

    $planArgs = New-Object object[] 7
    $planArgs[0] = [object]$apiGuid
    $planArgs[1] = 'abc123'
    $planArgs[2] = $true; $planArgs[3] = $true; $planArgs[4] = $true; $planArgs[5] = $true
    $planArgs[6] = [string[]]@('List')
    $plan = $plansType.GetMethod('ForGeneration', $static).Invoke($null, $planArgs)

    $createArgs = New-Object object[] 12
    $createArgs[0] = Get-Enum $kindType 'Apply'
    $createArgs[1] = $plan
    $createArgs[2] = [Guid]'11111111-1111-1111-1111-111111111111'
    $createArgs[3] = [Guid]'22222222-2222-2222-2222-222222222222'
    $createArgs[4] = 'Teste'
    $createArgs[5] = [Guid]'33333333-3333-3333-3333-333333333333'
    $createArgs[6] = [Guid]'44444444-4444-4444-4444-444444444444'
    $createArgs[7] = '0.1.0-alpha.7'
    $createArgs[8] = Get-Enum $intentType 'Current'
    $createArgs[9] = 'GOAB_API_METADATA_B060_V3'
    $createArgs[10] = [DateTime]::UtcNow
    $createArgs[11] = $null
    $journal = $checkpointsType.GetMethod('CreatePrepared', $static).Invoke($null, $createArgs)

    foreach ($item in $mapped) { [void]$journal.Receipts.Add($item) }
    foreach ($item in $inventory) { [void]$journal.Inventory.Add($item) }

    $validateArgs = New-Object object[] 1
    $validateArgs[0] = $journal
    $validation = $validatorType.GetMethod('Validate', $static).Invoke($null, $validateArgs)
    Assert-True ([bool](Get-Prop $validation 'IsValid')) "O envelope com recibos e inventário reais deve ser válido. Erros: $([string](Get-Prop $validation 'Errors') -join '; ')"

    # O envelope do reencontro, com identidade composta sem GUIDs, também precisa ser válido.
    $journal2 = $checkpointsType.GetMethod('CreatePrepared', $static).Invoke($null, $createArgs)
    $mapArgs2 = New-Object object[] 1
    $mapArgs2[0] = $log2.Receipts
    foreach ($item in $mapReceipts.Invoke($null, $mapArgs2)) { [void]$journal2.Receipts.Add($item) }
    foreach ($item in $reencounterInventory) { [void]$journal2.Inventory.Add($item) }
    $validateArgs2 = New-Object object[] 1
    $validateArgs2[0] = $journal2
    $validation2 = $validatorType.GetMethod('Validate', $static).Invoke($null, $validateArgs2)
    Assert-True ([bool](Get-Prop $validation2 'IsValid')) "Identidade composta sem GUIDs é válida num Save. Erros: $([string](Get-Prop $validation2 'Errors') -join '; ')"

    # Mas continua exigida onde autoriza exclusão.
    $deleteAction = [Enum]::Parse($assembly.GetType($ns + 'JournalInventoryAction', $true, $false), 'Delete')
    $reencounterItem.GetType().GetProperty('Action', $instance).SetValue($reencounterItem, $deleteAction)
    $journal3 = $checkpointsType.GetMethod('CreatePrepared', $static).Invoke($null, $createArgs)
    foreach ($item in $mapReceipts.Invoke($null, $mapArgs2)) { [void]$journal3.Receipts.Add($item) }
    [void]$journal3.Inventory.Add($reencounterItem)
    $validateArgs3 = New-Object object[] 1
    $validateArgs3[0] = $journal3
    $validation3 = $validatorType.GetMethod('Validate', $static).Invoke($null, $validateArgs3)
    Assert-True (-not [bool](Get-Prop $validation3 'IsValid')) 'Identidade composta sem GUIDs não pode autorizar exclusão.'
    Assert-Contains ([string](Get-Prop $validation3 'Errors') -join '; ') 'composite.apiGuid é obrigatório' 'A exigência permanece na fila destrutiva.'

} finally {
    if ($null -ne $script:AssemblyResolveHandler) {
        [System.AppDomain]::CurrentDomain.remove_AssemblyResolve($script:AssemblyResolveHandler)
    }
}

Write-Output 'PASS: ApiPlanOperationJournalReceipts'
