#requires -Version 7.4

<#
    S-B111 / F3 P4 — gate offline da fila de remoção por passadas.

    A seção 4.3 do plano da F3 fecha as regras da fila, e elas não são detalhe de
    implementação: quem volta para a fila, quem a interrompe, quando o orçamento acaba e o que
    autoriza terminar em `Removed`. Este gate exercita a máquina sem KB, para que a IDE não
    seja o primeiro lugar onde uma remoção parcial aparece.

    Origem do risco: 2026-09-06, KB `wsEducacaoSpTeste`, Transaction `Teste`. Uma remoção parou
    no meio com o API Object, cinco Procedures e cinco SDTs já apagados, e o relatório final
    informou «Removidos: nenhum».
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
    $queueType = $assembly.GetType($ns + 'ApiPlanRemovalQueue', $true, $false)
    $intentType = $assembly.GetType($ns + 'ApiPlanRemovalIntent', $true, $false)
    $targetType = $assembly.GetType($ns + 'ApiPlanRemovalTarget', $true, $false)
    $resultEnum = $assembly.GetType($ns + 'ApiPlanRemovalAttemptResult', $true, $false)
    $objectTypeEnum = $assembly.GetType($ns + 'JournalObjectType', $true, $false)
    $actionEnum = $assembly.GetType($ns + 'JournalInventoryAction', $true, $false)
    $identityEnum = $assembly.GetType($ns + 'JournalIdentityKind', $true, $false)
    $validatorType = $assembly.GetType($ns + 'ApiPlanOperationJournalValidator', $true, $false)

    $static = [System.Reflection.BindingFlags]'Static, Public'
    $staticAny = [System.Reflection.BindingFlags]'Static, NonPublic, Public'
    $instance = [System.Reflection.BindingFlags]'Instance, Public'
    $run = $queueType.GetMethod('Run', $static)
    $buildInventory = $intentType.GetMethod('BuildInventory', $static)
    $resolveMaxPasses = $intentType.GetMethod('ResolveMaxPasses', $static)
    $order = $intentType.GetMethod('Order', $static)

    function Get-Prop { param($Object, [string]$Name) return $Object.GetType().GetProperty($Name, $instance).GetValue($Object) }

    $listType = [System.Collections.Generic.List`1].MakeGenericType(@($targetType))
    $enumerableType = [System.Collections.Generic.IEnumerable`1].MakeGenericType(@($targetType))
    $readOnlyListType = [System.Collections.Generic.IReadOnlyList`1].MakeGenericType(@($targetType))
    $funcType = [System.Func`2].MakeGenericType(@($targetType, $resultEnum))
    $actionType = [System.Action`2].MakeGenericType(@([int], $readOnlyListType))

    function Get-Enum { param($Type, [string]$Name) return [Enum]::Parse($Type, $Name) }

    function New-Target {
        param(
            [string]$Name,
            [string]$ObjectType = 'Sdt',
            [string]$Action = 'Delete',
            [bool]$Queued = $true,
            [string]$IdentityKind = 'Guid'
        )
        $target = [Activator]::CreateInstance($targetType)
        $target.Name = $Name
        $target.ObjectType = Get-Enum $objectTypeEnum $ObjectType
        $target.Action = Get-Enum $actionEnum $Action
        $target.Queued = $Queued
        $target.IdentityKind = Get-Enum $identityEnum $IdentityKind
        if ($IdentityKind -eq 'Guid') { $target.Guid = [Guid]::NewGuid() }
        return $target
    }

    function New-TargetList {
        param($Targets)
        $list = [Activator]::CreateInstance($listType)
        foreach ($target in $Targets) { [void]$list.Add($target) }
        return ,$list
    }

    # O roteiro decide o resultado de cada tentativa por nome e por número da tentativa, para
    # que «recusou na primeira passada e saiu na segunda» seja exercitável sem KB.
    $script:Attempts = @{}
    $script:Script = @{}
    $script:PassLog = [System.Collections.Generic.List[string]]::new()

    $attemptBlock = {
        param($target)
        $name = $target.Name
        if (-not $script:Attempts.ContainsKey($name)) { $script:Attempts[$name] = 0 }
        $script:Attempts[$name] = $script:Attempts[$name] + 1
        $planned = $script:Script[$name]
        $index = [Math]::Min($script:Attempts[$name], $planned.Count) - 1
        return [Enum]::Parse($resultEnum, $planned[$index])
    }

    $passBlock = {
        param($pass, $pending)
        $script:PassLog.Add("$pass=$($pending.Count)")
    }

    function Invoke-Queue {
        param($Targets, $MaxPasses = $null)
        $script:Attempts = @{}
        $script:PassLog = [System.Collections.Generic.List[string]]::new()
        # Um object[] explícito: dentro de @(...), a List<T> seria enumerada e a chamada
        # chegaria à reflexão com o número errado de parâmetros.
        $args = New-Object object[] 4
        $args[0] = New-TargetList $Targets
        $args[1] = $attemptBlock -as $funcType
        $args[2] = $passBlock -as $actionType
        $args[3] = $MaxPasses
        return $run.Invoke($null, $args)
    }

    # --- 1. Caminho completo: uma passada, tudo confirmado ------------------------------------
    $api = New-Target -Name 'apiTeste' -ObjectType 'ApiObject'
    $proc = New-Target -Name 'procTeste_API_List' -ObjectType 'Procedure'
    $sdt = New-Target -Name 'sdtTeste_API_Response' -ObjectType 'Sdt'
    $file = New-Target -Name 'apiTeste_Metadata' -ObjectType 'MetadataFile'
    $script:Script = @{
        'apiTeste' = @('Confirmed')
        'procTeste_API_List' = @('Confirmed')
        'sdtTeste_API_Response' = @('Confirmed')
        'apiTeste_Metadata' = @('Confirmed')
    }
    $result = Invoke-Queue -Targets @($file, $sdt, $proc, $api)
    Assert-Equal 'Removed' ([string]$result.Outcome) 'Todos confirmados devem terminar em Removed'
    Assert-Equal $null $result.BlockReason 'Removed não grava motivo de bloqueio'
    Assert-Equal 1 $result.PassesExecuted 'Sem recusa, uma passada basta'
    Assert-Equal 4 $result.Deleted.Count 'Os quatro alvos saíram'
    Assert-Equal 1 $script:PassLog.Count 'O checkpoint de passada é emitido uma vez'
    Assert-Equal '1=0' $script:PassLog[0] 'A passada termina sem pendentes'

    # --- 2. Ordem canônica da fila -------------------------------------------------------------
    $orderArgs = New-Object object[] 1
    $orderArgs[0] = New-TargetList @($file, $sdt, $proc, $api)
    $orderedRaw = $order.Invoke($null, $orderArgs)
    $ordered = @($orderedRaw | ForEach-Object { [string]$_.ObjectType })
    Assert-Equal 'ApiObject' $ordered[0] 'O API Object sai primeiro: ele referencia as Procedures'
    Assert-Equal 'Procedure' $ordered[1] 'As Procedures vêm depois do API Object'
    Assert-Equal 'Sdt' $ordered[2] 'Os SDTs vêm depois das Procedures, que os tipam'
    Assert-Equal 'MetadataFile' $ordered[3] 'O File de metadata sai por último entre os objetos'

    # --- 3. Ordem de dependência imperfeita resolve-se na passada seguinte ---------------------
    # É o caso real: a lista de SDTs veio em ordem alfabética e a IDE recusou o primeiro.
    $filters = New-Target -Name 'sdtTeste_API_ListFilters'
    $listResponse = New-Target -Name 'sdtTeste_API_ListResponse'
    $script:Script = @{
        'sdtTeste_API_ListFilters' = @('StillPresent', 'Confirmed')
        'sdtTeste_API_ListResponse' = @('Confirmed')
    }
    $result = Invoke-Queue -Targets @($filters, $listResponse)
    Assert-Equal 'Removed' ([string]$result.Outcome) 'O que a IDE recusou volta para a fila e sai na passada seguinte'
    Assert-Equal 2 $result.PassesExecuted 'Foram necessárias duas passadas'
    Assert-Equal 2 $script:PassLog.Count 'Cada passada emite seu checkpoint'
    Assert-Equal '1=1' $script:PassLog[0] 'A primeira passada termina com um pendente'
    Assert-Equal '2=0' $script:PassLog[1] 'A segunda passada esvazia a fila'

    # --- 4. Orçamento de tentativas -------------------------------------------------------------
    $stubborn = New-Target -Name 'sdtTeste_API_Response'
    $other = New-Target -Name 'sdtTeste_API_GetResponse'
    $script:Script = @{
        'sdtTeste_API_Response' = @('StillPresent')
        'sdtTeste_API_GetResponse' = @('Confirmed')
    }
    $result = Invoke-Queue -Targets @($stubborn, $other)
    Assert-Equal 'Partial' ([string]$result.Outcome) 'Orçamento esgotado termina em Partial'
    Assert-Equal 'RetryBudgetExhausted' ([string]$result.BlockReason) 'O motivo persistido é o orçamento'
    Assert-Equal 2 $result.MaxPasses 'O orçamento é max(1, itens da fila)'
    Assert-Equal 2 $result.PassesExecuted 'A fila consome o orçamento e para'
    Assert-Equal 1 $result.Pending.Count 'O que sobrou fica declarado'
    Assert-Equal 'sdtTeste_API_Response' $result.Pending[0].Name 'O pendente é o alvo que a IDE nunca aceitou'

    # --- 5. Alvo ausente antes do Delete --------------------------------------------------------
    # Ausência não é sucesso implícito: quem apagou não foi esta operação.
    $first = New-Target -Name 'procTeste_API_Get' -ObjectType 'Procedure'
    $missing = New-Target -Name 'procTeste_API_Create' -ObjectType 'Procedure'
    $later = New-Target -Name 'sdtTeste_API_Response'
    $script:Script = @{
        'procTeste_API_Get' = @('Confirmed')
        'procTeste_API_Create' = @('AbsentBeforeDelete')
        'sdtTeste_API_Response' = @('Confirmed')
    }
    $result = Invoke-Queue -Targets @($first, $missing, $later)
    Assert-Equal 'Partial' ([string]$result.Outcome) 'Alvo ausente antes do Delete encerra em Partial'
    Assert-Equal 'TargetAbsentBeforeDelete' ([string]$result.BlockReason) 'O motivo nomeia a ausência'
    Assert-Equal 1 $result.Deleted.Count 'O que já tinha saído continua registrado'
    Assert-Equal 2 $result.Pending.Count 'O alvo ausente e o que nem foi tentado ficam pendentes'
    Assert-Equal 0 $script:PassLog.Count 'Uma passada interrompida não emite checkpoint de passada'
    Assert-True (-not $script:Attempts.ContainsKey('sdtTeste_API_Response')) 'O alvo seguinte não chega a ser tentado'

    # --- 6. Resultado indeterminado bloqueia sem retry -------------------------------------------
    $unknown = New-Target -Name 'apiTeste' -ObjectType 'ApiObject'
    $script:Script = @{ 'apiTeste' = @('OutcomeUnknown', 'Confirmed') }
    $result = Invoke-Queue -Targets @($unknown)
    Assert-Equal 'OutcomeUnknown' ([string]$result.Outcome) 'Releitura ambígua termina em OutcomeUnknown'
    Assert-Equal 'OutcomeUnknown' ([string]$result.BlockReason) 'O motivo persistido acompanha o estado'
    Assert-Equal 1 $script:Attempts['apiTeste'] 'Não há segunda tentativa sobre um resultado desconhecido'

    # --- 7. Falha de etapa não retryable ----------------------------------------------------------
    $failed = New-Target -Name 'sdtTeste_API_Response'
    $script:Script = @{ 'sdtTeste_API_Response' = @('StageFailed', 'Confirmed') }
    $result = Invoke-Queue -Targets @($failed)
    Assert-Equal 'Partial' ([string]$result.Outcome) 'Falha de etapa encerra em Partial'
    Assert-Equal 'StageFailed' ([string]$result.BlockReason) 'O motivo nomeia a etapa'
    Assert-Equal 1 $script:Attempts['sdtTeste_API_Response'] 'Uma falha não retryable não volta para a fila'

    # --- 8. Folder preservado não impede Removed ---------------------------------------------------
    $folder = New-Target -Name 'TesteOpenApi' -ObjectType 'Folder' -Action 'Preserve' -IdentityKind 'Folder'
    $folder.OwnershipValidated = $true
    $apiForFolder = New-Target -Name 'apiTeste' -ObjectType 'ApiObject'
    $script:Script = @{
        'apiTeste' = @('Confirmed')
        'TesteOpenApi' = @('Preserved')
    }
    $result = Invoke-Queue -Targets @($apiForFolder, $folder)
    Assert-Equal 'Removed' ([string]$result.Outcome) 'Preservar o Folder reutilizado não impede a conclusão'
    Assert-Equal 1 $result.Preserved.Count 'O preservado é declarado à parte do removido'
    Assert-Equal 'Preserve' ([string]$folder.Action) 'O Folder preservado permanece Preserve no inventário'

    # --- 9. Observação de um Folder apagado ---------------------------------------------------------
    $emptied = New-Target -Name 'TesteOpenApi' -ObjectType 'Folder' -Action 'Preserve' -IdentityKind 'Folder'
    $emptied.OwnershipValidated = $true
    $script:Script = @{ 'TesteOpenApi' = @('Confirmed') }
    $result = Invoke-Queue -Targets @($emptied)
    Assert-Equal 'Removed' ([string]$result.Outcome) 'O Folder vazio é apagado como qualquer alvo da fila'
    Assert-Equal 'Delete' ([string]$emptied.Action) 'Ao ser apagado, o Folder passa a declarar Delete'
    Assert-Equal $true $emptied.EmptyConfirmed 'A confirmação de vazio só é declarada depois de medida'

    # --- 10. Orçamento e inventário ------------------------------------------------------------------
    # O SDT compartilhado nunca entra na fila destrutiva: aparece só como Preserve.
    $shared = New-Target -Name 'sdt_API_ErrorResponse' -Action 'Preserve' -Queued $false -IdentityKind 'None'
    $shared.OwnershipValidated = $false
    $queuedArgs = New-Object object[] 1
    $queuedArgs[0] = New-TargetList @($api, $proc, $sdt, $file, $folder, $shared)
    Assert-Equal 5 ([int]$resolveMaxPasses.Invoke($null, $queuedArgs)) 'Itens fora da fila não entram no orçamento'

    $inventory = $buildInventory.Invoke($null, $queuedArgs)
    Assert-Equal 6 $inventory.Count 'O inventário declara tudo: o que sai e o que fica'
    $sharedItem = @($inventory | Where-Object { $_.Name -eq 'sdt_API_ErrorResponse' })[0]
    Assert-Equal 'Preserve' ([string]$sharedItem.Action) 'O SDT compartilhado é declarado Preserve'
    $folderItem = @($inventory | Where-Object { [string]$_.ObjectType -eq 'Folder' })[0]
    Assert-Equal 'Preserve' ([string]$folderItem.Action) 'O Folder ainda não medido é declarado Preserve'

    # Alvo repetido é recusado: um inventário com o mesmo alvo duas vezes não se reconcilia.
    $duplicatedArgs = New-Object object[] 1
    $duplicatedArgs[0] = New-TargetList @($api, $api)
    $rejected = $false
    try {
        [void]$buildInventory.Invoke($null, $duplicatedArgs)
    } catch {
        $rejected = $true
        $message = [string]$_.Exception.InnerException.Message
        Assert-True ($message.Contains('repete o mesmo alvo')) 'A recusa deve nomear a repetição'
    }
    Assert-True $rejected 'O inventário deve recusar alvo repetido'

    # --- 11. Vocabulário fechado de role e suficiência explícita no plano de Remove ----------
    # Decisões 21 e 30: sem estes literais as cinco Procedures voltam a ser indistinguíveis
    # no inventário composto, e a suficiência some do envelope.
    $rolesType = $assembly.GetType($ns + 'ApiPlanJournalRoles', $true, $false)
    $forProcedure = $rolesType.GetMethod('ForProcedureName', $staticAny)
    $forService = $rolesType.GetMethod('ForService', $staticAny)
    Assert-Equal 'MainApi' ([string]$rolesType.GetField('MainApi', $staticAny).GetValue($null)) 'MainApi canônico'
    Assert-Equal 'OwnSdt' ([string]$rolesType.GetField('OwnSdt', $staticAny).GetValue($null)) 'OwnSdt canônico'
    Assert-equal 'SharedSdt' ([string]$rolesType.GetField('SharedSdt', $staticAny).GetValue($null)) 'SharedSdt canônico'
    Assert-Equal 'Metadata' ([string]$rolesType.GetField('Metadata', $staticAny).GetValue($null)) 'Metadata canônico'
    Assert-equal 'List' ([string]$forProcedure.Invoke($null, @('procTeste_API_List'))) 'List pelo nome da Procedure'
    Assert-equal 'Get' ([string]$forProcedure.Invoke($null, @('procTeste_API_Get'))) 'Get pelo nome da Procedure'
    Assert-equal 'Create' ([string]$forProcedure.Invoke($null, @('procTeste_API_Create'))) 'Create pelo nome da Procedure'
    Assert-equal 'Update' ([string]$forProcedure.Invoke($null, @('procTeste_API_Update'))) 'Update pelo nome da Procedure'
    Assert-equal 'Delete' ([string]$forProcedure.Invoke($null, @('procTeste_API_Delete'))) 'Delete pelo nome da Procedure'
    Assert-equal 'Get' ([string]$forService.Invoke($null, @('get'))) 'ForService normaliza case'

    $plansType = $assembly.GetType($ns + 'ApiPlanOperationJournalPlans', $true, $false)
    $removalPlan = $plansType.GetMethod('ForRemoval', $static).Invoke($null, @(
        [object][Guid]'55555555-5555-5555-5555-555555555555',
        'abc123',
        [string[]]@('List', 'Get')))
    Assert-equal 'InventorySufficient' ([string](Get-Prop $removalPlan 'InventorySufficiency')) 'Plano de Remove declara suficiência explícita'
    $recoveryPlan = $plansType.GetMethod('ForMetadataRecovery', $static).Invoke($null, @(
        [object][Guid]'55555555-5555-5555-5555-555555555555'))
    Assert-Equal 'InventorySufficient' ([string](Get-Prop $recoveryPlan 'InventorySufficiency')) 'Plano de MetadataRecovery declara suficiência explícita'

    # Inventário de remoção com identidade composta: o role canônico viaja no item.
    $compositeIdentityType = $assembly.GetType($ns + 'ApiPlanOperationJournalCompositeIdentity', $true, $false)
    $listTarget = New-Target -Name 'procTeste_API_List' -ObjectType 'Procedure' -IdentityKind 'Composite'
    $listComposite = [Activator]::CreateInstance($compositeIdentityType)
    $listComposite.ExactName = 'procTeste_API_List'
    $listComposite.ObjectTypeName = 'Procedure'
    $listComposite.Role = [string]$forProcedure.Invoke($null, @('procTeste_API_List'))
    $listComposite.CanonicalDescription = 'procTeste_API_List'
    $listComposite.TransactionGuid = [Guid]'22222222-2222-2222-2222-222222222222'
    $listComposite.ApiGuid = [Guid]'55555555-5555-5555-5555-555555555555'
    $listTarget.Composite = $listComposite
    $createTarget = New-Target -Name 'procTeste_API_Create' -ObjectType 'Procedure' -IdentityKind 'Composite'
    $createComposite = [Activator]::CreateInstance($compositeIdentityType)
    $createComposite.ExactName = 'procTeste_API_Create'
    $createComposite.ObjectTypeName = 'Procedure'
    $createComposite.Role = [string]$forProcedure.Invoke($null, @('procTeste_API_Create'))
    $createComposite.CanonicalDescription = 'procTeste_API_Create'
    $createComposite.TransactionGuid = [Guid]'22222222-2222-2222-2222-222222222222'
    $createComposite.ApiGuid = [Guid]'55555555-5555-5555-5555-555555555555'
    $createTarget.Composite = $createComposite
    $roleArgs = New-Object object[] 1
    $roleArgs[0] = New-TargetList @($listTarget, $createTarget)
    $roleInventory = $buildInventory.Invoke($null, $roleArgs)
    $byRoleName = @{}
    foreach ($item in $roleInventory) { $byRoleName[[string]$item.Name] = $item }
    Assert-Equal 'List' ([string](Get-Prop (Get-Prop $byRoleName['procTeste_API_List'] 'Composite') 'Role')) 'List permanece distinto no inventário'
    Assert-Equal 'Create' ([string](Get-Prop (Get-Prop $byRoleName['procTeste_API_Create'] 'Composite') 'Role')) 'Create permanece distinto no inventário'

} finally {
    if ($null -ne $script:AssemblyResolveHandler) {
        [System.AppDomain]::CurrentDomain.remove_AssemblyResolve($script:AssemblyResolveHandler)
    }
}

Write-Output 'PASS: ApiPlanRemovalQueue'
