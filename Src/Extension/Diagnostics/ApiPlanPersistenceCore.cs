#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Identidade fechada usada para confirmar o alvo físico de uma persistência.
/// O nome planejado é apenas apresentação; a confirmação deve usar uma destas
/// identidades.
/// </summary>
public abstract class PersistenceIdentity
{
    public static readonly PersistenceIdentity None = new NonePersistenceIdentity();

    public abstract string Kind { get; }

    public abstract string StableKey { get; }

    public abstract string Display { get; }

    private sealed class NonePersistenceIdentity : PersistenceIdentity
    {
        public override string Kind => "None";

        public override string StableKey => "none";

        public override string Display => "None";
    }
}

public sealed class GuidIdentity : PersistenceIdentity
{
    public GuidIdentity(Guid guid)
    {
        if (guid == Guid.Empty)
        {
            throw new ArgumentException("O GUID da identidade não pode ser vazio.", nameof(guid));
        }

        Guid = guid;
    }

    public Guid Guid { get; }

    public override string Kind => "Guid";

    public override string StableKey => Kind + ":" + Guid.ToString("D");

    public override string Display => Guid.ToString("D");
}

public sealed class FileIdentity : PersistenceIdentity
{
    public FileIdentity(Guid fileId, string canonicalName, string expectedSha256)
    {
        if (fileId == Guid.Empty)
        {
            throw new ArgumentException("O FileId da identidade não pode ser vazio.", nameof(fileId));
        }

        FileId = fileId;
        CanonicalName = Require(canonicalName, nameof(canonicalName));
        ExpectedSha256 = Require(expectedSha256, nameof(expectedSha256));
    }

    public Guid FileId { get; }

    public string CanonicalName { get; }

    public string ExpectedSha256 { get; }

    public override string Kind => "File";

    public override string StableKey => string.Format(
        CultureInfo.InvariantCulture,
        "{0}:{1}:{2}:{3}",
        Kind,
        FileId.ToString("D"),
        CanonicalName,
        ExpectedSha256);

    public override string Display => CanonicalName + "#" + FileId.ToString("D");

    private static string Require(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("O valor da identidade é obrigatório.", parameterName);
        }

        return value;
    }
}

public sealed class CompositeIdentity : PersistenceIdentity
{
    public CompositeIdentity(
        string exactName,
        string objectType,
        string role,
        string canonicalDescription,
        Guid transactionGuid,
        Guid apiGuid)
    {
        ExactName = Require(exactName, nameof(exactName));
        ObjectType = Require(objectType, nameof(objectType));
        Role = Require(role, nameof(role));
        CanonicalDescription = canonicalDescription ?? string.Empty;
        TransactionGuid = transactionGuid;
        ApiGuid = apiGuid;
    }

    public string ExactName { get; }

    public string ObjectType { get; }

    public string Role { get; }

    public string CanonicalDescription { get; }

    public Guid TransactionGuid { get; }

    public Guid ApiGuid { get; }

    public override string Kind => "Composite";

    public override string StableKey => string.Format(
        CultureInfo.InvariantCulture,
        "{0}:{1}:{2}:{3}:{4}:{5}:{6}",
        Kind,
        ObjectType,
        Role,
        ExactName,
        TransactionGuid.ToString("D"),
        ApiGuid.ToString("D"),
        CanonicalDescription);

    public override string Display => ObjectType + ":" + ExactName;

    private static string Require(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("O valor da identidade é obrigatório.", parameterName);
        }

        return value;
    }
}

public sealed class FolderIdentity : PersistenceIdentity
{
    public FolderIdentity(string exactName, bool owned, bool emptyConfirmed)
    {
        if (string.IsNullOrWhiteSpace(exactName))
        {
            throw new ArgumentException("O nome exato do Folder é obrigatório.", nameof(exactName));
        }

        ExactName = exactName;
        Owned = owned;
        EmptyConfirmed = emptyConfirmed;
    }

    public string ExactName { get; }

    public bool Owned { get; }

    public bool EmptyConfirmed { get; }

    public override string Kind => "Folder";

    public override string StableKey => string.Format(
        CultureInfo.InvariantCulture,
        "{0}:{1}:{2}:{3}",
        Kind,
        ExactName,
        Owned,
        EmptyConfirmed);

    public override string Display => ExactName;
}

public enum PersistenceAttemptState
{
    Started = 0,
    Finished = 1,
    Interrupted = 2,
}

public enum PersistenceOutcome
{
    Confirmed = 0,
    Failed = 1,
    OutcomeUnknown = 2,
}

public enum PersistenceConfirmationStatus
{
    NotAttempted = 0,
    Confirmed = 1,
    Absent = 2,
    Divergent = 3,
    Unreadable = 4,
}

public enum PersistencePhysicalState
{
    Present = 0,
    Absent = 1,
    Unknown = 2,
}

public enum PersistenceRetryableReason
{
    StillPresentAfterDelete = 0,
}

public sealed class PersistenceConfirmation
{
    private PersistenceConfirmation(
        PersistenceConfirmationStatus status,
        PersistencePhysicalState physicalState,
        string observedIdentity,
        string detail)
    {
        Status = status;
        PhysicalState = physicalState;
        ObservedIdentity = observedIdentity ?? string.Empty;
        Detail = detail ?? string.Empty;
    }

    public PersistenceConfirmationStatus Status { get; }

    public PersistencePhysicalState PhysicalState { get; }

    public string ObservedIdentity { get; }

    public string Detail { get; }

    public static PersistenceConfirmation Confirmed(string observedIdentity = "", string detail = "") =>
        new PersistenceConfirmation(
            PersistenceConfirmationStatus.Confirmed,
            PersistencePhysicalState.Present,
            observedIdentity,
            detail);

    public static PersistenceConfirmation Absent(string detail = "") =>
        new PersistenceConfirmation(
            PersistenceConfirmationStatus.Absent,
            PersistencePhysicalState.Absent,
            string.Empty,
            detail);

    public static PersistenceConfirmation Divergent(string observedIdentity = "", string detail = "") =>
        new PersistenceConfirmation(
            PersistenceConfirmationStatus.Divergent,
            PersistencePhysicalState.Unknown,
            observedIdentity,
            detail);

    public static PersistenceConfirmation Unreadable(string detail) =>
        new PersistenceConfirmation(
            PersistenceConfirmationStatus.Unreadable,
            PersistencePhysicalState.Unknown,
            string.Empty,
            detail);

    public static PersistenceConfirmation NotAttempted(PersistencePhysicalState physicalState, string detail = "")
    {
        if (physicalState != PersistencePhysicalState.Absent && physicalState != PersistencePhysicalState.Unknown)
        {
            throw new ArgumentException("NotAttempted exige estado físico Absent ou Unknown.", nameof(physicalState));
        }

        return new PersistenceConfirmation(
            PersistenceConfirmationStatus.NotAttempted,
            physicalState,
            string.Empty,
            detail);
    }
}

public enum PersistenceFaultPoint
{
    None = 0,
    FolderSave = 1,
    SdtSave = 2,
    ProcedureSave = 3,
    ApiSave = 4,
    MetadataSave = 5,
    B115MetadataSave = 6,
    BusinessComponentEnablementSave = 7,
    ApiDelete = 8,
    ProcedureDelete = 9,
    SdtDelete = 10,
    MetadataDelete = 11,
    FolderDelete = 12,
}

public enum PersistenceFaultAction
{
    None = 0,
    Throw = 1,
    Cancel = 2,
    // Válido somente em Before: simula um delegate físico que retorna sem mutar o alvo.
    ReturnWithoutMutation = 3,
    DivergentConfirmation = 4,
    UnreadableConfirmation = 5,
}

public interface IApiPlanPersistenceFaultInjector
{
    PersistenceFaultAction Before(PersistenceFaultPoint point, int attempt);

    PersistenceFaultAction After(PersistenceFaultPoint point, int attempt);
}

/// <summary>
/// Núcleo SDK-free do seam de persistência. Ele registra observações, mas não
/// decide retry, recuperação ou bloqueio de negócio.
/// </summary>
public static class ApiPlanPersistenceCore
{
    [ThreadStatic]
    private static ApiPlanPersistenceLog? _current;

    [ThreadStatic]
    private static IApiPlanPersistenceFaultInjector? _faultInjector;

    public static IDisposable Begin(ApiPlanPersistenceLog log, Action<ApiPlanPersistenceLog>? onDispose = null)
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        var previous = _current;
        _current = log;
        return new Scope(() => _current = previous, () => SafePublish(log, onDispose));
    }

    internal static IDisposable BeginFaultInjection(IApiPlanPersistenceFaultInjector injector)
    {
        if (injector is null)
        {
            throw new ArgumentNullException(nameof(injector));
        }

        var previous = _faultInjector;
        _faultInjector = injector;
        return new Scope(() => _faultInjector = previous, onDispose: null);
    }

    public static IDisposable Suspend()
    {
        var previous = _current;
        _current = null;
        return new Scope(() => _current = previous, onDispose: null);
    }

    public static PersistenceReceipt? Persist(
        string operationKind,
        string objectType,
        string stage,
        string plannedName,
        PersistenceIdentity identity,
        Action persist,
        Func<PersistenceConfirmation> confirm)
    {
        return Persist(
            PersistenceFaultPoint.None,
            operationKind,
            objectType,
            stage,
            plannedName,
            identity,
            persist,
            confirm);
    }

    public static PersistenceReceipt? Persist(
        PersistenceFaultPoint faultPoint,
        string operationKind,
        string objectType,
        string stage,
        string plannedName,
        PersistenceIdentity identity,
        Action persist,
        Func<PersistenceConfirmation> confirm)
    {
        ValidateInputs(operationKind, objectType, stage, plannedName, identity, persist, confirm);

        var log = _current;
        if (log is null)
        {
            persist();
            return null;
        }

        var attempt = log.GetNextAttempt(operationKind, objectType, identity);
        PersistenceFaultAction beforeAction;
        try
        {
            beforeAction = ApplyBeforeFault(faultPoint, attempt);
        }
        catch (Exception exception)
        {
            log.NoteStageFailed(stage, "persistence.before_failed", exception.Message);
            throw;
        }

        var receipt = log.StartReceipt(operationKind, objectType, stage, plannedName, identity, attempt);
        var physicalDelegateReturned = false;
        var confirmationRead = false;
        try
        {
            if (beforeAction != PersistenceFaultAction.ReturnWithoutMutation)
            {
                persist();
            }

            physicalDelegateReturned = true;
            var afterAction = GetFaultAction(faultPoint, attempt, after: true);
            ApplyAfterFault(afterAction);

            var observation = Confirm(confirm, ref confirmationRead);
            observation = OverrideConfirmation(observation, afterAction);
            var outcome = ResolveOutcome(operationKind, observation);
            log.CompleteReceipt(
                receipt,
                PersistenceAttemptState.Finished,
                outcome,
                observation,
                exception: null,
                confirmationRead);
            return receipt;
        }
        catch (Exception exception)
        {
            PersistenceConfirmation? observation = null;
            if (string.Equals(operationKind, "Delete", StringComparison.OrdinalIgnoreCase))
            {
                observation = TryConfirm(confirm, ref confirmationRead);
            }

            if (observation is null)
            {
                observation = PersistenceConfirmation.Unreadable("A confirmação não foi obtida após a falha da operação física.");
            }

            var outcome = string.Equals(operationKind, "Delete", StringComparison.OrdinalIgnoreCase)
                ? ResolveOutcome(operationKind, observation)
                : PersistenceOutcome.OutcomeUnknown;
            log.CompleteReceipt(
                receipt,
                physicalDelegateReturned ? PersistenceAttemptState.Finished : PersistenceAttemptState.Interrupted,
                outcome,
                observation,
                exception,
                confirmationRead);
            throw;
        }
    }

    public static PersistenceReceipt? RecordNotAttempted(
        string operationKind,
        string objectType,
        string stage,
        string plannedName,
        PersistenceIdentity identity,
        PersistenceConfirmation observation)
    {
        ValidateInputs(operationKind, objectType, stage, plannedName, identity, () => { }, () => observation);
        if (!string.Equals(operationKind, "Delete", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("RecordNotAttempted só pode registrar Delete.", nameof(operationKind));
        }

        if (observation.Status != PersistenceConfirmationStatus.NotAttempted)
        {
            throw new ArgumentException("RecordNotAttempted exige confirmação NotAttempted.", nameof(observation));
        }

        var log = _current;
        return log?.RecordNotAttempted(operationKind, objectType, stage, plannedName, identity, observation);
    }

    public static void NoteStageFailed(string stage, string reasonCode, string detail)
    {
        _current?.NoteStageFailed(stage, reasonCode, detail);
    }

    private static PersistenceConfirmation Confirm(Func<PersistenceConfirmation> confirm, ref bool confirmationRead)
    {
        try
        {
            var result = confirm();
            confirmationRead = true;
            return result ?? throw new InvalidOperationException("A confirmação retornou nulo.");
        }
        catch
        {
            confirmationRead = true;
            throw;
        }
    }

    private static PersistenceConfirmation TryConfirm(Func<PersistenceConfirmation> confirm, ref bool confirmationRead)
    {
        try
        {
            return Confirm(confirm, ref confirmationRead);
        }
        catch (Exception exception)
        {
            return PersistenceConfirmation.Unreadable(exception.GetType().FullName + ": " + Clean(exception.Message));
        }
    }

    private static PersistenceConfirmation OverrideConfirmation(
        PersistenceConfirmation observation,
        PersistenceFaultAction afterAction)
    {
        return afterAction switch
        {
            PersistenceFaultAction.DivergentConfirmation => PersistenceConfirmation.Divergent(
                observation.ObservedIdentity,
                "Confirmação divergente injetada pelo teste."),
            PersistenceFaultAction.UnreadableConfirmation => PersistenceConfirmation.Unreadable(
                "Confirmação ilegível injetada pelo teste."),
            _ => observation,
        };
    }

    private static PersistenceOutcome ResolveOutcome(string operationKind, PersistenceConfirmation observation)
    {
        if (!string.Equals(operationKind, "Delete", StringComparison.OrdinalIgnoreCase))
        {
            return observation.Status == PersistenceConfirmationStatus.Confirmed
                ? PersistenceOutcome.Confirmed
                : PersistenceOutcome.OutcomeUnknown;
        }

        if (observation.Status == PersistenceConfirmationStatus.Absent
            || (observation.Status == PersistenceConfirmationStatus.NotAttempted
                && observation.PhysicalState == PersistencePhysicalState.Absent))
        {
            return observation.Status == PersistenceConfirmationStatus.Absent
                ? PersistenceOutcome.Confirmed
                : PersistenceOutcome.OutcomeUnknown;
        }

        if (observation.Status == PersistenceConfirmationStatus.Confirmed
            && observation.PhysicalState == PersistencePhysicalState.Present)
        {
            return PersistenceOutcome.Failed;
        }

        return PersistenceOutcome.OutcomeUnknown;
    }

    private static void ValidateInputs(
        string operationKind,
        string objectType,
        string stage,
        string plannedName,
        PersistenceIdentity identity,
        Action persist,
        Func<PersistenceConfirmation> confirm)
    {
        Require(operationKind, nameof(operationKind));
        Require(objectType, nameof(objectType));
        Require(stage, nameof(stage));
        Require(plannedName, nameof(plannedName));
        if (!string.Equals(operationKind, "Save", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(operationKind, "Delete", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("A operação deve ser Save ou Delete.", nameof(operationKind));
        }

        if (identity is null)
        {
            throw new ArgumentNullException(nameof(identity));
        }

        if (persist is null)
        {
            throw new ArgumentNullException(nameof(persist));
        }

        if (confirm is null)
        {
            throw new ArgumentNullException(nameof(confirm));
        }
    }

    private static PersistenceFaultAction ApplyBeforeFault(PersistenceFaultPoint point, int attempt)
    {
        var action = GetFaultAction(point, attempt, after: false);
        if (action == PersistenceFaultAction.Throw)
        {
            throw new InvalidOperationException("Falha de preparação injetada em " + point + ".");
        }

        if (action == PersistenceFaultAction.Cancel)
        {
            throw new OperationCanceledException("Cancelamento de preparação injetado em " + point + ".");
        }

        if (action == PersistenceFaultAction.None || action == PersistenceFaultAction.ReturnWithoutMutation)
        {
            return action;
        }

        throw new InvalidOperationException(
            action + " só pode ser injetado depois do delegate físico.");
    }

    private static void ApplyAfterFault(PersistenceFaultAction action)
    {
        if (action == PersistenceFaultAction.Throw)
        {
            throw new InvalidOperationException("Falha após a persistência injetada.");
        }

        if (action == PersistenceFaultAction.Cancel)
        {
            throw new OperationCanceledException("Cancelamento após a persistência injetado.");
        }

        if (action == PersistenceFaultAction.ReturnWithoutMutation)
        {
            throw new InvalidOperationException(
                "ReturnWithoutMutation deve ser injetado antes do delegate físico.");
        }

        if (action != PersistenceFaultAction.None
            && action != PersistenceFaultAction.DivergentConfirmation
            && action != PersistenceFaultAction.UnreadableConfirmation)
        {
            throw new InvalidOperationException(
                "Ação de falha posterior não reconhecida: " + action + ".");
        }
    }

    private static PersistenceFaultAction GetFaultAction(PersistenceFaultPoint point, int attempt, bool after)
    {
        if (point == PersistenceFaultPoint.None || _faultInjector is null)
        {
            return PersistenceFaultAction.None;
        }

        return after
            ? _faultInjector.After(point, attempt)
            : _faultInjector.Before(point, attempt);
    }

    private static void SafePublish(ApiPlanPersistenceLog log, Action<ApiPlanPersistenceLog>? onDispose)
    {
        try
        {
            onDispose?.Invoke(log);
        }
        catch
        {
        }
    }

    private static string Require(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("O valor é obrigatório.", parameterName);
        }

        return value;
    }

    private static string Clean(string value) => (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ");

    private sealed class Scope : IDisposable
    {
        private readonly Action _restore;
        private readonly Action? _onDispose;
        private bool _disposed;

        public Scope(Action restore, Action? onDispose)
        {
            _restore = restore;
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _restore();
            _onDispose?.Invoke();
        }
    }
}
