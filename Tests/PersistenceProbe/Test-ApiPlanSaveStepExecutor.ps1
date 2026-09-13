#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$corePath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanPersistenceCore.cs'
$logPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanPersistenceLog.cs'
$executorPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanSaveStepExecutor.cs'
$boundaryPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanSaveBoundaryProbe.cs'
$coreSource = [System.IO.File]::ReadAllText($corePath)
$logSource = [System.IO.File]::ReadAllText($logPath)
$executorSource = [System.IO.File]::ReadAllText($executorPath)
$boundarySource = [System.IO.File]::ReadAllText($boundaryPath)
$logBody = $logSource -replace '(?m)^#nullable enable\r?\n', '' -replace '(?m)^using [^\r\n]+\r?\n', '' -replace '(?m)^namespace [^\r\n]+\r?\n', ''
$executorBody = $executorSource -replace '(?m)^#nullable enable\r?\n', '' -replace '(?m)^using [^\r\n]+\r?\n', '' -replace '(?m)^namespace [^\r\n]+\r?\n', ''

$harnessSource = @"
internal sealed class ApiPlanBusyProgressSession
{
    public int ThrowIfAbortRequestedCount { get; private set; }
    public int ReportCount { get; private set; }
    public int PumpCount { get; private set; }

    public void ThrowIfAbortRequested() => ThrowIfAbortRequestedCount++;
    public void Report(string stage, int index, int total, string label) => ReportCount++;
    public void Report(string stage, int index, int total, string label, long elapsedMilliseconds) => ReportCount++;
    public void Pump() => PumpCount++;
}

internal static class ApiPlanSaveBoundaryProbe
{
    public static int PumpBoundaryCount { get; private set; }
    public static int BeforeSaveCount { get; private set; }
    public static int SavedCount { get; private set; }
    public static int FailedCount { get; private set; }

    public static void Reset()
    {
        PumpBoundaryCount = 0;
        BeforeSaveCount = 0;
        SavedCount = 0;
        FailedCount = 0;
    }

    public static void PumpBoundary(string stage, string label, string before, string after) => PumpBoundaryCount++;
    public static void BeforeSave(string stage, string label, string snapshot) => BeforeSaveCount++;
    public static PersistenceReceipt? Persist(
        PersistenceFaultPoint faultPoint,
        string operationKind,
        string objectType,
        string stage,
        string plannedName,
        PersistenceIdentity identity,
        Action persist,
        Func<PersistenceConfirmation> confirm) =>
        ApiPlanPersistenceCore.Persist(faultPoint, operationKind, objectType, stage, plannedName, identity, persist, confirm);
    public static void Saved(string stage, string label, string snapshot) => SavedCount++;
    public static void Failed(string stage, string label, Exception exception, string snapshot) => FailedCount++;
    public static void NoteStageFailed(string stage, string reasonCode, string detail) =>
        ApiPlanPersistenceCore.NoteStageFailed(stage, reasonCode, detail);
}

public sealed class ExecutorTestFaultInjector : IApiPlanPersistenceFaultInjector
{
    public PersistenceFaultPoint Point { get; set; }
    public PersistenceFaultAction BeforeAction { get; set; }
    public PersistenceFaultAction AfterAction { get; set; }

    public PersistenceFaultAction Before(PersistenceFaultPoint point, int attempt) =>
        point == Point && attempt == 1 ? BeforeAction : PersistenceFaultAction.None;

    public PersistenceFaultAction After(PersistenceFaultPoint point, int attempt) =>
        point == Point && attempt == 1 ? AfterAction : PersistenceFaultAction.None;
}

public sealed class ExecutorHarnessResult
{
    public int FirstPrepareCount { get; set; }
    public int FirstPersistCount { get; set; }
    public int SecondPersistCount { get; set; }
    public int CallbackCount { get; set; }
    public int ReceiptCount { get; set; }
    public int StageFailureCount { get; set; }
    public PersistenceOutcome FirstOutcome { get; set; }
    public string ExceptionMessage { get; set; } = string.Empty;
    public int ProgressThrowCount { get; set; }
    public int ProgressReportCount { get; set; }
    public int ProgressPumpCount { get; set; }
    public int PumpBoundaryCount { get; set; }
    public int BeforeSaveCount { get; set; }
    public int SavedCount { get; set; }
    public int FailedCount { get; set; }
}

public static class ApiPlanSaveStepExecutorHarness
{
    public static ExecutorHarnessResult Execute(PersistenceFaultAction beforeAction, PersistenceFaultAction afterAction)
    {
        var firstPrepareCount = 0;
        var firstPersistCount = 0;
        var secondPersistCount = 0;
        var callbackCount = 0;
        var log = new ApiPlanPersistenceLog(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var injector = new ExecutorTestFaultInjector
        {
            Point = PersistenceFaultPoint.ProcedureSave,
            BeforeAction = beforeAction,
            AfterAction = afterAction,
        };
        var progress = new ApiPlanBusyProgressSession();
        ApiPlanSaveBoundaryProbe.Reset();
        var scope = ApiPlanPersistenceCore.Begin(log);
        var faultScope = ApiPlanPersistenceCore.BeginFaultInjection(injector);
        Exception? exception = null;
        try
        {
            var steps = new List<ApiPlanSaveStep>
            {
                new ApiPlanSaveStep(
                    "procFixture_API_List",
                    "Procedure",
                    "Procedures",
                    new GuidIdentity(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")),
                    PersistenceFaultPoint.ProcedureSave,
                    () => firstPrepareCount++,
                    () => firstPersistCount++,
                    () => beforeAction == PersistenceFaultAction.ReturnWithoutMutation
                        ? PersistenceConfirmation.Absent("fixture sem mutação")
                        : PersistenceConfirmation.Confirmed("procFixture_API_List"),
                    () => "first"),
                new ApiPlanSaveStep(
                    "apiFixture",
                    "API",
                    "List",
                    new GuidIdentity(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")),
                    PersistenceFaultPoint.ApiSave,
                    () => { },
                    () => secondPersistCount++,
                    () => PersistenceConfirmation.Confirmed("apiFixture"),
                    () => "second"),
            };

            ApiPlanSaveStepExecutor.Execute(steps, progress, (stage, label, elapsed) => callbackCount++);
        }
        catch (Exception caught)
        {
            exception = caught;
        }
        finally
        {
            faultScope.Dispose();
            scope.Dispose();
        }

        return new ExecutorHarnessResult
        {
            FirstPrepareCount = firstPrepareCount,
            FirstPersistCount = firstPersistCount,
            SecondPersistCount = secondPersistCount,
            CallbackCount = callbackCount,
            ReceiptCount = log.Receipts.Count,
            StageFailureCount = log.StageFailures.Count,
            FirstOutcome = log.Receipts.Count == 0 ? PersistenceOutcome.OutcomeUnknown : log.Receipts[0].Outcome,
            ExceptionMessage = exception?.Message ?? string.Empty,
            ProgressThrowCount = progress.ThrowIfAbortRequestedCount,
            ProgressReportCount = progress.ReportCount,
            ProgressPumpCount = progress.PumpCount,
            PumpBoundaryCount = ApiPlanSaveBoundaryProbe.PumpBoundaryCount,
            BeforeSaveCount = ApiPlanSaveBoundaryProbe.BeforeSaveCount,
            SavedCount = ApiPlanSaveBoundaryProbe.SavedCount,
            FailedCount = ApiPlanSaveBoundaryProbe.FailedCount,
        };
    }
}
"@

Add-Type -TypeDefinition ("using System.Linq;`r`n" + $coreSource + [Environment]::NewLine + $logBody + [Environment]::NewLine + $harnessSource + [Environment]::NewLine + $executorBody)

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

$actionType = [GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceFaultAction]
$outcomeType = [GenexusOpenApiBuilder.Extension.Diagnostics.PersistenceOutcome]
$harness = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanSaveStepExecutorHarness]

Assert-True ($boundarySource -match 'internal static IDisposable BeginFaultInjection') 'O adaptador SDK deve expor somente hook interno para injeção de falha.'

$normal = $harness::Execute($actionType::None, $actionType::None)
Assert-Equal 1 $normal.FirstPrepareCount 'Executor deve preparar o primeiro passo.'
Assert-Equal 1 $normal.FirstPersistCount 'Executor deve persistir o primeiro passo normal.'
Assert-Equal 1 $normal.SecondPersistCount 'Executor deve continuar para o segundo passo normal.'
Assert-Equal 2 $normal.CallbackCount 'Executor deve notificar cada Save confirmado.'
Assert-Equal 2 $normal.ReceiptCount 'Executor deve registrar os dois recibos confirmados.'
Assert-Equal 0 $normal.StageFailureCount 'Fluxo normal não pode registrar falha de etapa.'
Assert-Equal 2 $normal.SavedCount 'Probe deve receber Saved para os dois passos confirmados.'
Assert-Equal 0 $normal.FailedCount 'Probe não pode receber Failed no fluxo normal.'
Assert-Equal 2 $normal.ProgressThrowCount 'Executor deve checar aborto antes de cada passo.'
Assert-Equal 4 $normal.ProgressReportCount 'Executor deve reportar antes e depois de cada Save.'
Assert-Equal 2 $normal.ProgressPumpCount 'Executor deve bombear uma vez por passo.'

$withoutMutation = $harness::Execute($actionType::ReturnWithoutMutation, $actionType::None)
Assert-Equal 1 $withoutMutation.FirstPrepareCount 'Executor deve preparar antes de ReturnWithoutMutation.'
Assert-Equal 0 $withoutMutation.FirstPersistCount 'ReturnWithoutMutation não pode chamar o delegate físico.'
Assert-Equal 0 $withoutMutation.SecondPersistCount 'Executor deve interromper a cadeia após recibo não confirmado.'
Assert-Equal 0 $withoutMutation.CallbackCount 'Executor não pode notificar callback após recibo não confirmado.'
Assert-Equal 1 $withoutMutation.ReceiptCount 'ReturnWithoutMutation deve preservar o recibo do primeiro passo.'
Assert-Equal $outcomeType::OutcomeUnknown $withoutMutation.FirstOutcome 'ReturnWithoutMutation deve ser OutcomeUnknown.'
Assert-True ($withoutMutation.ExceptionMessage -match 'não foi confirmada') 'Executor deve propagar a não confirmação.'
Assert-Equal 1 $withoutMutation.FailedCount 'Probe deve registrar a falha do executor.'

$invalidAfter = $harness::Execute($actionType::None, $actionType::ReturnWithoutMutation)
Assert-Equal 1 $invalidAfter.FirstPersistCount 'ReturnWithoutMutation em After ocorre após o delegate físico.'
Assert-Equal 0 $invalidAfter.SecondPersistCount 'Executor deve parar após ação inválida em After.'
Assert-Equal 1 $invalidAfter.ReceiptCount 'Ação inválida em After deve preservar o recibo.'
Assert-Equal $outcomeType::OutcomeUnknown $invalidAfter.FirstOutcome 'Ação inválida em After deve ser OutcomeUnknown.'
Assert-True ($invalidAfter.ExceptionMessage -match 'antes do delegate físico') 'Executor deve propagar a restrição de After.'
Assert-Equal 1 $invalidAfter.FailedCount 'Probe deve registrar a falha posterior inválida.'

$invalidBefore = $harness::Execute($actionType::DivergentConfirmation, $actionType::None)
Assert-Equal 0 $invalidBefore.FirstPersistCount 'Ação posterior em Before não pode chamar o delegate físico.'
Assert-Equal 0 $invalidBefore.SecondPersistCount 'Executor deve parar antes do segundo passo em Before inválido.'
Assert-Equal 0 $invalidBefore.ReceiptCount 'Ação posterior em Before não pode criar recibo.'
Assert-Equal 1 $invalidBefore.StageFailureCount 'Ação posterior em Before deve registrar falha de etapa.'
Assert-True ($invalidBefore.ExceptionMessage -match 'só pode ser injetado depois') 'Executor deve propagar a restrição de Before.'
Assert-Equal 1 $invalidBefore.FailedCount 'Probe deve registrar a falha de Before inválido.'

Write-Output 'PASS: ApiPlanSaveStepExecutor'
