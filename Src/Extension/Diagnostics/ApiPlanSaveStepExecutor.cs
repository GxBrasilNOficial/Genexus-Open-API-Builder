#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Um passo de persistência gerenciado. A preparação fica fora do delegate físico;
/// o executor envolve apenas a chamada de Save e sua confirmação obrigatória.
/// </summary>
internal sealed class ApiPlanSaveStep
{
    public ApiPlanSaveStep(
        string label,
        string objectType,
        string stage,
        PersistenceIdentity identity,
        PersistenceFaultPoint faultPoint,
        Action prepare,
        Action persist,
        Func<PersistenceConfirmation> confirm,
        Func<string> snapshot)
    {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        ObjectType = objectType ?? throw new ArgumentNullException(nameof(objectType));
        Stage = stage ?? throw new ArgumentNullException(nameof(stage));
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        Prepare = prepare ?? throw new ArgumentNullException(nameof(prepare));
        Persist = persist ?? throw new ArgumentNullException(nameof(persist));
        Confirm = confirm ?? throw new ArgumentNullException(nameof(confirm));
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        FaultPoint = faultPoint;
    }

    public string Label { get; }

    public string ObjectType { get; }

    public string Stage { get; }

    public PersistenceIdentity Identity { get; }

    public PersistenceFaultPoint FaultPoint { get; }

    public Action Prepare { get; }

    public Action Persist { get; }

    public Func<PersistenceConfirmation> Confirm { get; }

    public Func<string> Snapshot { get; }
}

/// <summary>Executor único dos antigos laços de Save do BC e do List.</summary>
internal static class ApiPlanSaveStepExecutor
{
    public static void Execute(
        IEnumerable<ApiPlanSaveStep> steps,
        ApiPlanBusyProgressSession? progress,
        Action<string, string, long>? onSaveCompleted)
    {
        if (steps is null)
        {
            throw new ArgumentNullException(nameof(steps));
        }

        var materialized = steps.ToArray();
        var saveIndex = 0;
        foreach (var step in materialized)
        {
            progress?.ThrowIfAbortRequested();
            saveIndex++;
            var beforePumpSnapshot = step.Snapshot();
            progress?.Report(step.Stage, saveIndex, materialized.Length, step.Label);
            progress?.Pump();
            var afterPumpSnapshot = step.Snapshot();
            ApiPlanSaveBoundaryProbe.PumpBoundary(step.Stage, step.Label, beforePumpSnapshot, afterPumpSnapshot);
            ApiPlanSaveBoundaryProbe.BeforeSave(step.Stage, step.Label, afterPumpSnapshot);
            var persistenceStarted = false;
            var sw = Stopwatch.StartNew();
            try
            {
                step.Prepare();
                persistenceStarted = true;
                var receipt = ApiPlanSaveBoundaryProbe.Persist(
                    step.FaultPoint,
                    "Save",
                    step.ObjectType,
                    step.Stage,
                    step.Label,
                    step.Identity,
                    step.Persist,
                    step.Confirm);

                if (receipt is not null)
                {
                    if (receipt.Outcome != PersistenceOutcome.Confirmed)
                    {
                        throw new InvalidOperationException(
                            $"Persistência de '{step.Label}' não foi confirmada: Outcome='{receipt.Outcome}', Confirmation='{receipt.Confirmation}', Detail='{receipt.ConfirmationDetail}'.");
                    }
                }
                else
                {
                    var confirmation = step.Confirm();
                    if (confirmation.Status != PersistenceConfirmationStatus.Confirmed
                        || confirmation.PhysicalState != PersistencePhysicalState.Present)
                    {
                        throw new InvalidOperationException(
                            $"Persistência de '{step.Label}' não foi confirmada: Confirmation='{confirmation.Status}', PhysicalState='{confirmation.PhysicalState}', Detail='{confirmation.Detail}'.");
                    }
                }

                sw.Stop();
                ApiPlanSaveBoundaryProbe.Saved(step.Stage, step.Label, step.Snapshot());
                onSaveCompleted?.Invoke(step.Stage, step.Label, sw.ElapsedMilliseconds);
            }
            catch (Exception exception)
            {
                if (!persistenceStarted)
                {
                    ApiPlanSaveBoundaryProbe.NoteStageFailed(
                        step.Stage,
                        "stage.preparation_failed",
                        exception.Message);
                }

                ApiPlanSaveBoundaryProbe.Failed(step.Stage, step.Label, exception, step.Snapshot());
                throw;
            }

            progress?.Report(step.Stage, saveIndex, materialized.Length, step.Label, sw.ElapsedMilliseconds);
        }
    }
}
