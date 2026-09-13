#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$corePath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanPersistenceCore.cs'
$logPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanPersistenceLog.cs'
$coreSource = [System.IO.File]::ReadAllText($corePath)
$logSource = [System.IO.File]::ReadAllText($logPath)
$injectorSource = @"
using GenexusOpenApiBuilder.Extension.Diagnostics;
public sealed class PersistenceTestFaultInjector : IApiPlanPersistenceFaultInjector
{
    public PersistenceFaultAction BeforeAction { get; set; }
    public PersistenceFaultAction AfterAction { get; set; }
    public PersistenceFaultAction Before(PersistenceFaultPoint point, int attempt) => BeforeAction;
    public PersistenceFaultAction After(PersistenceFaultPoint point, int attempt) => AfterAction;
}
public sealed class PersistenceTestCounter
{
    public int Value { get; private set; }
    public void Increment() => Value++;
    public System.Action AsAction() => Increment;
}
public static class PersistenceTestFaultScope
{
    public static System.IDisposable Begin(IApiPlanPersistenceFaultInjector injector) =>
        ApiPlanPersistenceCore.BeginFaultInjection(injector);
}
"@
$logBody = $logSource -replace '(?m)^#nullable enable\r?\n', '' -replace '(?m)^using [^\r\n]+\r?\n', '' -replace '(?m)^namespace [^\r\n]+\r?\n', ''
$injectorBody = $injectorSource -replace '(?m)^using [^\r\n]+\r?\n', ''
Add-Type -TypeDefinition ("using System.Linq;`r`n" + $coreSource + [Environment]::NewLine + $logBody + [Environment]::NewLine + $injectorBody)

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

function New-Identity {
    return [GenexusOpenApiBuilder.Extension.Diagnostics.GuidIdentity]::new([guid]'11111111-1111-1111-1111-111111111111')
}

$core = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanPersistenceCore]
$logType = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanPersistenceLog]
$confirmationType = [GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceConfirmation]
$outcomeType = [GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceOutcome]
$statusType = [GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceConfirmationStatus]
$physicalType = [GenexusOpenApiBuilder.Extension.Diagnostics.PersistencePhysicalState]
$faultPointType = [GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceFaultPoint]
$faultActionType = [GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceFaultAction]
$faultScopeType = [GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceTestFaultScope]

# 1) Sem escopo, o delegate roda e a confirmação não é chamada.
$log = $logType::new()
$persistCount = 0
$confirmCount = 0
$result = $core::Persist(
    'Save', 'API', 'Api', 'apiTeste', (New-Identity),
    [Action] { $script:persistCount++ },
    [Func[GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceConfirmation]] {
        $script:confirmCount++
        return $confirmationType::Confirmed('nao-deve-ser-lido')
    })
Assert-True ($null -eq $result) 'Persist sem escopo deve retornar nulo.'
Assert-Equal 1 $persistCount 'Persist sem escopo deve executar o delegate.'
Assert-Equal 0 $confirmCount 'Persist sem escopo não deve executar confirmação.'
Assert-Equal 0 $log.Receipts.Count 'Log inativo não deve receber recibos.'

# 2) Save confirmado: sequência, identidade e duração são publicadas.
$scope = $core::Begin($log)
try {
    $receipt = $core::Persist(
        'Save', 'API', 'Api', 'apiTeste', (New-Identity),
        [Action] { $script:persistCount++ },
        [Func[GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceConfirmation]] {
            return $confirmationType::Confirmed('api-guid')
        })
}
finally {
    $scope.Dispose()
}
Assert-Equal 1 $log.Receipts.Count 'Save confirmado deve gerar um recibo.'
Assert-Equal 1 $receipt.Sequence 'O primeiro recibo deve receber sequência 1.'
Assert-Equal $outcomeType::Confirmed $receipt.Outcome 'Save confirmado deve ser Confirmed.'
Assert-Equal $statusType::Confirmed $receipt.Confirmation 'A confirmação deve ser preservada.'
Assert-Equal $physicalType::Present $receipt.PhysicalState 'Save confirmado deve observar alvo presente.'
Assert-True ($receipt.ConfirmationRead) 'Save confirmado deve registrar a leitura de confirmação.'

# 3) Exceção de Save: a exceção é relançada e o resultado fica indeterminado.
$saveFailureLog = $logType::new()
$scope = $core::Begin($saveFailureLog)
try {
    try {
        [void]$core::Persist(
            'Save', 'Procedure', 'Procedure', 'procTeste', (New-Identity),
            [Action] { throw [InvalidOperationException]::new('falha save') },
            [Func[GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceConfirmation]] {
                return $confirmationType::Confirmed('nao-lido')
            })
        throw 'A exceção do Save deveria ter sido relançada.'
    }
    catch {
        Assert-True ($_.Exception.InnerException.Message -eq 'falha save') 'A exceção original deve ser relançada.'
    }
}
finally {
    $scope.Dispose()
}
$saveFailureReceipt = $saveFailureLog.Receipts[0]
Assert-Equal $outcomeType::OutcomeUnknown $saveFailureReceipt.Outcome 'Falha de Save deve ser OutcomeUnknown.'
Assert-Equal $saveFailureReceipt.AttemptState $([GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceAttemptState]::Interrupted) 'Falha de Save deve ser Interrupted.'

# 4) Delete com exceção e alvo ainda presente é falha retryable.
$deletePresentLog = $logType::new()
$scope = $core::Begin($deletePresentLog)
try {
    try {
        [void]$core::Persist(
            'Delete', 'SDT', 'Sdt', 'sdtTeste', (New-Identity),
            [Action] { throw [InvalidOperationException]::new('falha delete') },
            [Func[GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceConfirmation]] {
                return $confirmationType::Confirmed('sdt-guid')
            })
    }
    catch {
    }
}
finally {
    $scope.Dispose()
}
$deletePresentReceipt = $deletePresentLog.Receipts[0]
Assert-Equal $outcomeType::Failed $deletePresentReceipt.Outcome 'Delete presente deve ser Failed.'
Assert-True ($deletePresentReceipt.RetryEligible) 'Delete presente deve ser retryable.'
Assert-Equal 'StillPresentAfterDelete' $deletePresentReceipt.RetryableReason.ToString() 'Motivo retryable deve ser estável.'

# 5) Delete com exceção e ausência confirmada é sucesso.
$deleteAbsentLog = $logType::new()
$scope = $core::Begin($deleteAbsentLog)
try {
    try {
        [void]$core::Persist(
            'Delete', 'File', 'Metadata', 'apiTeste_Metadata', (New-Identity),
            [Action] { throw [InvalidOperationException]::new('falha após remoção') },
            [Func[GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceConfirmation]] {
                return $confirmationType::Absent('ausência confirmada')
            })
    }
    catch {
    }
}
finally {
    $scope.Dispose()
}
Assert-Equal $outcomeType::Confirmed $deleteAbsentLog.Receipts[0].Outcome 'Delete ausente deve ser Confirmed.'

# 6) Delete com confirmação ilegível continua indeterminado e não autoriza retry.
$deleteUnreadableLog = $logType::new()
$scope = $core::Begin($deleteUnreadableLog)
try {
    try {
        [void]$core::Persist(
            'Delete', 'Folder', 'Folder', 'TesteOpenApi', (New-Identity),
            [Action] { throw [InvalidOperationException]::new('falha sem leitura') },
            [Func[GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceConfirmation]] {
                throw [InvalidOperationException]::new('consulta ilegível')
            })
    }
    catch {
    }
}
finally {
    $scope.Dispose()
}
Assert-Equal $outcomeType::OutcomeUnknown $deleteUnreadableLog.Receipts[0].Outcome 'Confirmação ilegível deve ser OutcomeUnknown.'
Assert-True (-not $deleteUnreadableLog.Receipts[0].RetryEligible) 'Confirmação ilegível não pode autorizar retry.'

# 7) Falha antes do delegate é classe C: evento de etapa sem recibo.
$stageFailureLog = $logType::new()
$scope = $core::Begin($stageFailureLog)
try {
    $core::NoteStageFailed('Sdt', 'stage.preparation_failed', 'falha antes do Save')
}
finally {
    $scope.Dispose()
}
Assert-Equal 0 $stageFailureLog.Receipts.Count 'Falha de etapa não deve criar PersistenceReceipt.'
Assert-Equal 1 $stageFailureLog.StageFailures.Count 'Falha de etapa deve ser registrada.'
Assert-Equal 'stage.preparation_failed' $stageFailureLog.StageFailures[0].ReasonCode 'ReasonCode de etapa deve ser legível por máquina.'

# 8) Divergência, ausência e alvo não tentado usam o log comum.
$divergentLog = $logType::new()
$scope = $core::Begin($divergentLog)
try {
    [void]$core::Persist(
        'Save', 'Procedure', 'Procedure', 'procDivergente', (New-Identity),
        [Action] { },
        [Func[GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceConfirmation]] {
            return $confirmationType::Divergent('outro-guid', 'identidade divergente')
        })
    [void]$core::RecordNotAttempted(
        'Delete', 'SDT', 'Sdt', 'sdtAusente', (New-Identity),
        $confirmationType::NotAttempted($physicalType::Absent, 'alvo não localizado'))
}
finally {
    $scope.Dispose()
}
Assert-Equal $outcomeType::OutcomeUnknown $divergentLog.Receipts[0].Outcome 'Divergência deve ser OutcomeUnknown.'
Assert-Equal $statusType::NotAttempted $divergentLog.Receipts[1].Confirmation 'Alvo ausente antes de Delete deve ser NotAttempted.'
Assert-Equal 1 $divergentLog.Receipts[1].Attempt 'RecordNotAttempted deve usar attempt=1.'
Assert-Equal $outcomeType::OutcomeUnknown $divergentLog.Receipts[1].Outcome 'NotAttempted nunca equivale a remoção confirmada.'

# 9) Escopos aninhados, Suspend e publicação defeituosa restauram o estado.
$outerLog = $logType::new()
$innerLog = $logType::new()
$outerScope = $core::Begin($outerLog, [Action[GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanPersistenceLog]] { throw 'falha de publicação' })
try {
    [void]$core::Persist('Save', 'API', 'Outer', 'apiOuter', (New-Identity), [Action] { }, [Func[GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceConfirmation]] { return $confirmationType::Confirmed('outer') })
    $innerScope = $core::Begin($innerLog)
    try {
        [void]$core::Persist('Save', 'API', 'Inner', 'apiInner', [GenexusOpenApiBuilder.Extension.Diagnostics.GuidIdentity]::new([guid]'22222222-2222-2222-2222-222222222222'), [Action] { }, [Func[GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceConfirmation]] { return $confirmationType::Confirmed('inner') })
    }
    finally {
        $innerScope.Dispose()
    }
    [void]$core::Persist('Save', 'API', 'Outer', 'apiOuter2', (New-Identity), [Action] { }, [Func[GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceConfirmation]] { return $confirmationType::Confirmed('outer2') })
    $suspend = $core::Suspend()
    try {
        [void]$core::Persist('Save', 'API', 'Suspended', 'apiSuspended', [GenexusOpenApiBuilder.Extension.Diagnostics.GuidIdentity]::new([guid]'33333333-3333-3333-3333-333333333333'), [Action] { }, [Func[GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceConfirmation]] { throw 'não deve ser lida' })
    }
    finally {
        $suspend.Dispose()
    }
    [void]$core::Persist('Save', 'API', 'Outer', 'apiOuter3', (New-Identity), [Action] { }, [Func[GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceConfirmation]] { return $confirmationType::Confirmed('outer3') })
}
finally {
    $outerScope.Dispose()
}
Assert-Equal 3 $outerLog.Receipts.Count 'Escopo externo deve ser restaurado depois do interno e do Suspend.'
Assert-Equal 1 $innerLog.Receipts.Count 'Escopo interno deve receber apenas seu próprio recibo.'

# 10) Injetor exercita Before/After sem alterar a produção por configuração externa.
$injector = [GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceTestFaultInjector]::new()
$faultLog = $logType::new()
$scope = $core::Begin($faultLog)
    $faultScope = $faultScopeType::Begin($injector)
try {
    $injector.BeforeAction = $faultActionType::Throw
    try {
        [void]$core::Persist($faultPointType::ApiSave, 'Save', 'API', 'Api', 'apiBefore', (New-Identity), [Action] { throw 'não deve executar' }, [Func[GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceConfirmation]] { return $confirmationType::Confirmed('nao') })
    }
    catch {
    }
    Assert-Equal 0 $faultLog.Receipts.Count 'Falha Before deve acontecer sem recibo físico.'
    Assert-Equal 1 $faultLog.StageFailures.Count 'Falha Before deve registrar falha de etapa sem recibo físico.'
    Assert-Equal 'persistence.before_failed' $faultLog.StageFailures[0].ReasonCode 'Falha Before deve preservar ReasonCode de etapa.'

    $injector.BeforeAction = $faultActionType::None
    $injector.AfterAction = $faultActionType::DivergentConfirmation
    [void]$core::Persist($faultPointType::ApiSave, 'Save', 'API', 'Api', 'apiAfter', [GenexusOpenApiBuilder.Extension.Diagnostics.GuidIdentity]::new([guid]'44444444-4444-4444-4444-444444444444'), [Action] { }, [Func[GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceConfirmation]] { return $confirmationType::Confirmed('api') })
    $injector.AfterAction = $faultActionType::UnreadableConfirmation
    [void]$core::Persist($faultPointType::ApiSave, 'Save', 'API', 'Api', 'apiUnreadable', [GenexusOpenApiBuilder.Extension.Diagnostics.GuidIdentity]::new([guid]'55555555-5555-5555-5555-555555555555'), [Action] { }, [Func[GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceConfirmation]] { return $confirmationType::Confirmed('api') })
}
finally {
    $faultScope.Dispose()
    $scope.Dispose()
}
Assert-Equal $outcomeType::OutcomeUnknown $faultLog.Receipts[0].Outcome 'Divergência injetada deve ser OutcomeUnknown.'
Assert-Equal $outcomeType::OutcomeUnknown $faultLog.Receipts[1].Outcome 'Confirmação ilegível injetada deve ser OutcomeUnknown.'

# 11) A matriz percorre todos os pontos fechados e mantém o contrato de cada classe.
$expectedFaultPoints = @(
    'FolderSave', 'SdtSave', 'ProcedureSave', 'ApiSave',
    'MetadataSave', 'B115MetadataSave', 'BusinessComponentEnablementSave',
    'ApiDelete', 'ProcedureDelete', 'SdtDelete', 'MetadataDelete', 'FolderDelete')
$actualFaultPoints = [Enum]::GetNames($faultPointType) | Where-Object { $_ -ne 'None' }
Assert-Equal ($expectedFaultPoints -join ',') ($actualFaultPoints -join ',') 'A enumeração dos pontos de falha deve permanecer fechada e ordenada.'

function Invoke-SaveCase {
    param(
        [GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceFaultPoint]$Point,
        [GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceFaultAction]$BeforeAction,
        [GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceFaultAction]$AfterAction)

    $caseInjector = [GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceTestFaultInjector]::new()
    $caseInjector.BeforeAction = $BeforeAction
    $caseInjector.AfterAction = $AfterAction
    $caseLog = $logType::new()
    $physicalCounter = [GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceTestCounter]::new()
    $caseException = $null
    $scope = $core::Begin($caseLog)
    $faultScope = $faultScopeType::Begin($caseInjector)
    try {
        $persistAction = $physicalCounter.AsAction()
        try {
            [void]$core::Persist(
                $Point, 'Save', 'Fixture', 'FaultMatrix', $Point.ToString(), (New-Identity),
                $persistAction,
                [Func[GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceConfirmation]] {
                    if ($BeforeAction -eq $faultActionType::ReturnWithoutMutation) {
                        return $confirmationType::Absent('fixture sem mutação')
                    }

                    return $confirmationType::Confirmed($Point.ToString())
                })
        }
        catch {
            $caseException = $_.Exception
        }
    }
    finally {
        $faultScope.Dispose()
        $scope.Dispose()
    }

    return [pscustomobject]@{
        Log = $caseLog
        PhysicalCount = $physicalCounter.Value
        Exception = $caseException
    }
}

foreach ($pointName in $expectedFaultPoints) {
    $point = [Enum]::Parse($faultPointType, $pointName)

    $normal = Invoke-SaveCase $point $faultActionType::None $faultActionType::None
    Assert-Equal 1 $normal.PhysicalCount "Save normal deve executar o delegate em '$pointName'."
    Assert-Equal 1 $normal.Log.Receipts.Count "Save normal deve gerar recibo em '$pointName'."
    Assert-Equal $outcomeType::Confirmed $normal.Log.Receipts[0].Outcome "Save normal deve ser confirmado em '$pointName'."

    foreach ($beforeAction in @($faultActionType::Throw, $faultActionType::Cancel)) {
        $beforeFailure = Invoke-SaveCase $point $beforeAction $faultActionType::None
        Assert-Equal 0 $beforeFailure.PhysicalCount "Falha Before não pode executar o delegate em '$pointName'."
        Assert-Equal 0 $beforeFailure.Log.Receipts.Count "Falha Before não pode criar recibo em '$pointName'."
        Assert-Equal 1 $beforeFailure.Log.StageFailures.Count "Falha Before deve registrar etapa em '$pointName'."
    }

    foreach ($beforeAction in @($faultActionType::DivergentConfirmation, $faultActionType::UnreadableConfirmation)) {
        $invalidBefore = Invoke-SaveCase $point $beforeAction $faultActionType::None
        Assert-Equal 0 $invalidBefore.PhysicalCount "Ação posterior em Before não pode executar o delegate em '$pointName'."
        Assert-Equal 0 $invalidBefore.Log.Receipts.Count "Ação posterior em Before não pode criar recibo em '$pointName'."
        Assert-Equal 1 $invalidBefore.Log.StageFailures.Count "Ação posterior em Before deve registrar falha de etapa em '$pointName'."
    }

    $noMutation = Invoke-SaveCase $point $faultActionType::ReturnWithoutMutation $faultActionType::None
    Assert-Equal 0 $noMutation.PhysicalCount "ReturnWithoutMutation não pode mutar em '$pointName'."
    Assert-Equal 1 $noMutation.Log.Receipts.Count "ReturnWithoutMutation deve gerar observação em '$pointName'."
    Assert-Equal $outcomeType::OutcomeUnknown $noMutation.Log.Receipts[0].Outcome "Save sem mutação deve ser indeterminado em '$pointName'."

    foreach ($afterAction in @(
        $faultActionType::Throw,
        $faultActionType::Cancel,
        $faultActionType::DivergentConfirmation,
        $faultActionType::UnreadableConfirmation)) {
        $afterFailure = Invoke-SaveCase $point $faultActionType::None $afterAction
        Assert-Equal 1 $afterFailure.PhysicalCount "Falha After deve ocorrer depois do delegate em '$pointName'."
        Assert-Equal 1 $afterFailure.Log.Receipts.Count "Falha After deve preservar recibo em '$pointName'."
        Assert-Equal $outcomeType::OutcomeUnknown $afterFailure.Log.Receipts[0].Outcome "Falha After deve ser indeterminada em '$pointName'."
    }

    $invalidAfter = Invoke-SaveCase $point $faultActionType::None $faultActionType::ReturnWithoutMutation
    Assert-Equal 1 $invalidAfter.PhysicalCount "ReturnWithoutMutation em After ocorre depois do delegate em '$pointName'."
    Assert-Equal 1 $invalidAfter.Log.Receipts.Count "ReturnWithoutMutation em After deve preservar recibo em '$pointName'."
    Assert-Equal $outcomeType::OutcomeUnknown $invalidAfter.Log.Receipts[0].Outcome "ReturnWithoutMutation em After deve ser indeterminado em '$pointName'."
    Assert-True ($invalidAfter.Exception.Message -match 'antes do delegate físico') "ReturnWithoutMutation em After deve explicar a violação em '$pointName'."
}

function Invoke-DeleteCase {
    param(
        [GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceFaultPoint]$Point,
        [GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceFaultAction]$BeforeAction,
        [GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceFaultAction]$AfterAction,
        [bool]$DeleteMutates = $true)

    $caseInjector = [GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceTestFaultInjector]::new()
    $caseInjector.BeforeAction = $BeforeAction
    $caseInjector.AfterAction = $AfterAction
    $caseLog = $logType::new()
    $physicalCounter = [GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceTestCounter]::new()
    $physicalState = [pscustomobject]@{ Present = $true }
    $caseException = $null
    $scope = $core::Begin($caseLog)
    $faultScope = $faultScopeType::Begin($caseInjector)
    try {
        $deleteAction = if ($DeleteMutates) {
            [Action] { $physicalCounter.Increment(); $physicalState.Present = $false }.GetNewClosure()
        }
        else {
            [Action] { $physicalCounter.Increment() }.GetNewClosure()
        }
        try {
            [void]$core::Persist(
                $Point, 'Delete', 'Fixture', 'FaultMatrix', $Point.ToString(), (New-Identity),
                $deleteAction,
                [Func[GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceConfirmation]] {
                    if ($physicalState.Present) {
                        return $confirmationType::Confirmed('fixture-present')
                    }

                    return $confirmationType::Absent('fixture-absent')
                }.GetNewClosure())
        }
        catch {
            $caseException = $_.Exception
        }
    }
    finally {
        $faultScope.Dispose()
        $scope.Dispose()
    }

    return [pscustomobject]@{
        Log = $caseLog
        PhysicalCount = $physicalCounter.Value
        PhysicalPresent = $physicalState.Present
        Exception = $caseException
    }
}

$deleteFaultPoints = @('ApiDelete', 'ProcedureDelete', 'SdtDelete', 'MetadataDelete', 'FolderDelete')
foreach ($pointName in $deleteFaultPoints) {
    $point = [Enum]::Parse($faultPointType, $pointName)

    $normal = Invoke-DeleteCase $point $faultActionType::None $faultActionType::None
    Assert-Equal 1 $normal.PhysicalCount "Delete normal deve executar o delegate em '$pointName'."
    Assert-Equal $outcomeType::Confirmed $normal.Log.Receipts[0].Outcome "Delete normal deve ser confirmado em '$pointName'."

    foreach ($beforeAction in @($faultActionType::Throw, $faultActionType::Cancel)) {
        $beforeFailure = Invoke-DeleteCase $point $beforeAction $faultActionType::None
        Assert-Equal 0 $beforeFailure.PhysicalCount "Falha Before de Delete não pode executar o delegate em '$pointName'."
        Assert-Equal 0 $beforeFailure.Log.Receipts.Count "Falha Before de Delete não pode criar recibo em '$pointName'."
        Assert-Equal 1 $beforeFailure.Log.StageFailures.Count "Falha Before de Delete deve registrar etapa em '$pointName'."
    }

    $noMutation = Invoke-DeleteCase $point $faultActionType::ReturnWithoutMutation $faultActionType::None
    Assert-Equal 0 $noMutation.PhysicalCount "Delete ReturnWithoutMutation não pode mutar em '$pointName'."
    Assert-Equal $outcomeType::Failed $noMutation.Log.Receipts[0].Outcome "Delete sem mutação deve ser Failed em '$pointName'."
    Assert-True $noMutation.Log.Receipts[0].RetryEligible "Delete sem mutação deve ser retryable em '$pointName'."

    foreach ($afterAction in @(
        $faultActionType::Throw,
        $faultActionType::Cancel)) {
        $afterFailure = Invoke-DeleteCase $point $faultActionType::None $afterAction
        Assert-Equal 1 $afterFailure.PhysicalCount "Falha After de Delete deve ocorrer depois do delegate em '$pointName'."
        Assert-Equal $outcomeType::Confirmed $afterFailure.Log.Receipts[0].Outcome "Delete confirmado por ausência deve ser reconciliado em '$pointName'."
    }

    foreach ($afterAction in @(
        $faultActionType::DivergentConfirmation,
        $faultActionType::UnreadableConfirmation)) {
        $ambiguous = Invoke-DeleteCase $point $faultActionType::None $afterAction
        Assert-Equal 1 $ambiguous.PhysicalCount "Confirmação ambígua de Delete deve ocorrer depois do delegate em '$pointName'."
        Assert-Equal $outcomeType::OutcomeUnknown $ambiguous.Log.Receipts[0].Outcome "Confirmação ambígua de Delete deve bloquear em '$pointName'."
        Assert-True (-not $ambiguous.Log.Receipts[0].RetryEligible) "Delete ambíguo não pode autorizar retry em '$pointName'."
    }

    $stillPresent = Invoke-DeleteCase $point $faultActionType::None $faultActionType::Throw $false
    Assert-Equal $outcomeType::Failed $stillPresent.Log.Receipts[0].Outcome "Delete ainda presente deve ser Failed em '$pointName'."
    Assert-True $stillPresent.Log.Receipts[0].RetryEligible "Delete ainda presente deve autorizar retry em '$pointName'."
}

# 12) Confirmação ausente é rejeitada; identidades File preservam nome, id e bytes esperados.
$missingConfirmationLog = $logType::new()
$scope = $core::Begin($missingConfirmationLog)
try {
    try {
        [void]$core::Persist(
            $faultPointType::B115MetadataSave, 'Save', 'File', 'Metadata', 'apiTeste_Metadata', (New-Identity),
            [Action] { },
            [Func[GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceConfirmation]] { return $null })
    }
    catch {
    }
}
finally {
    $scope.Dispose()
}
Assert-Equal $outcomeType::OutcomeUnknown $missingConfirmationLog.Receipts[0].Outcome 'Confirmação ausente deve ser OutcomeUnknown.'
Assert-Equal $statusType::Unreadable $missingConfirmationLog.Receipts[0].Confirmation 'Confirmação ausente deve ser Unreadable.'
Assert-True $missingConfirmationLog.Receipts[0].ConfirmationRead 'Tentativa de confirmação ausente deve ser registrada.'

$newFile = $confirmationType::Confirmed('file-new')
$newFileIdentity = [GenexusOpenApiBuilder.Extension.Diagnostics.FileIdentity]::new(
    [guid]'66666666-6666-6666-6666-666666666666', 'apiTeste_Metadata', 'sha256-new')
$reusedFileIdentity = [GenexusOpenApiBuilder.Extension.Diagnostics.FileIdentity]::new(
    [guid]'77777777-7777-7777-7777-777777777777', 'apiTeste_Metadata', 'sha256-reused')
Assert-True ($newFileIdentity.StableKey -ne $reusedFileIdentity.StableKey) 'File novo e reutilizado devem ter identidades distintas.'
Assert-True ($newFileIdentity.Display -match 'apiTeste_Metadata') 'Identidade File nova deve preservar o nome canônico.'
Assert-True ($newFileIdentity.StableKey -match 'sha256-new') 'Identidade File nova deve preservar os bytes esperados.'
Assert-True ($reusedFileIdentity.StableKey -match 'sha256-reused') 'Identidade File reutilizada deve preservar os bytes esperados.'

Write-Output 'PASS: ApiPlanPersistenceCore'
