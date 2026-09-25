#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

public sealed class PersistenceReceipt
{
    internal PersistenceReceipt(
        Guid operationId,
        long sequence,
        string operationKind,
        string objectType,
        string stage,
        string plannedName,
        PersistenceIdentity identity,
        int attempt,
        long? retryOfSequence,
        DateTimeOffset startedAt)
    {
        OperationId = operationId;
        Sequence = sequence;
        OperationKind = operationKind;
        ObjectType = objectType;
        Stage = stage;
        PlannedName = plannedName;
        Identity = identity;
        Attempt = attempt;
        RetryOfSequence = retryOfSequence;
        StartedAt = startedAt;
        AttemptState = PersistenceAttemptState.Started;
        Outcome = PersistenceOutcome.OutcomeUnknown;
        Confirmation = PersistenceConfirmationStatus.NotAttempted;
        PhysicalState = PersistencePhysicalState.Unknown;
    }

    public Guid OperationId { get; }

    public long Sequence { get; }

    public string OperationKind { get; }

    public string ObjectType { get; }

    public string Stage { get; }

    public string PlannedName { get; }

    public PersistenceIdentity Identity { get; }

    public string? ObservedIdentity { get; private set; }

    public DateTimeOffset StartedAt { get; }

    public DateTimeOffset? FinishedAt { get; private set; }

    public long DurationMs { get; private set; }

    public int Attempt { get; }

    public long? RetryOfSequence { get; }

    public PersistenceAttemptState AttemptState { get; private set; }

    public PersistenceOutcome Outcome { get; private set; }

    public PersistenceConfirmationStatus Confirmation { get; private set; }

    public PersistencePhysicalState PhysicalState { get; private set; }

    public bool RetryEligible { get; private set; }

    public PersistenceRetryableReason? RetryableReason { get; private set; }

    public string? ExceptionType { get; private set; }

    public string? ExceptionMessage { get; private set; }

    public bool ConfirmationRead { get; private set; }

    public string? ConfirmationDetail { get; private set; }

    /// <summary>B109 ramo C — ver <see cref="PersistenceConfirmation.Cause"/>. Não vai para o diário.</summary>
    public Exception? ConfirmationCause { get; private set; }

    internal void Complete(
        PersistenceAttemptState attemptState,
        PersistenceOutcome outcome,
        PersistenceConfirmation observation,
        Exception? exception,
        bool confirmationRead)
    {
        var finished = DateTimeOffset.UtcNow;
        AttemptState = attemptState;
        Outcome = outcome;
        Confirmation = observation.Status;
        PhysicalState = observation.PhysicalState;
        ObservedIdentity = observation.ObservedIdentity;
        ConfirmationDetail = observation.Detail;
        ConfirmationCause = observation.Cause;
        FinishedAt = finished;
        DurationMs = Math.Max(0L, (long)(finished - StartedAt).TotalMilliseconds);
        ConfirmationRead = confirmationRead;
        ExceptionType = exception?.GetType().FullName;
        ExceptionMessage = exception?.Message;
        RetryEligible = string.Equals(OperationKind, "Delete", StringComparison.OrdinalIgnoreCase)
            && outcome == PersistenceOutcome.Failed
            && observation.Status == PersistenceConfirmationStatus.Confirmed
            && observation.PhysicalState == PersistencePhysicalState.Present;
        RetryableReason = RetryEligible ? PersistenceRetryableReason.StillPresentAfterDelete : (PersistenceRetryableReason?)null;
    }

    internal void CompleteNotAttempted(PersistenceConfirmation observation)
    {
        Complete(
            PersistenceAttemptState.Finished,
            PersistenceOutcome.OutcomeUnknown,
            observation,
            exception: null,
            confirmationRead: true);
    }
}

public sealed class PersistenceStageFailure
{
    internal PersistenceStageFailure(Guid operationId, string stage, string reasonCode, string detail)
    {
        OperationId = operationId;
        Stage = stage;
        ReasonCode = reasonCode;
        Detail = detail;
        OccurredAt = DateTimeOffset.UtcNow;
    }

    public Guid OperationId { get; }

    public string Stage { get; }

    public string ReasonCode { get; }

    public string Detail { get; }

    public DateTimeOffset OccurredAt { get; }
}

/// <summary>Log ordenado dos receipts de uma única operação.</summary>
public sealed class ApiPlanPersistenceLog
{
    private readonly List<PersistenceReceipt> _receipts = new List<PersistenceReceipt>();
    private readonly List<PersistenceStageFailure> _stageFailures = new List<PersistenceStageFailure>();
    private readonly Dictionary<string, PersistenceReceipt> _lastByTarget = new Dictionary<string, PersistenceReceipt>(StringComparer.Ordinal);
    private long _nextSequence;

    public ApiPlanPersistenceLog()
        : this(Guid.NewGuid())
    {
    }

    public ApiPlanPersistenceLog(Guid operationId)
    {
        OperationId = operationId == Guid.Empty ? Guid.NewGuid() : operationId;
    }

    public Guid OperationId { get; }

    public IReadOnlyList<PersistenceReceipt> Receipts => _receipts;

    public IReadOnlyList<PersistenceStageFailure> StageFailures => _stageFailures;

    public PersistenceReceipt? GetLastReceipt()
    {
        return _receipts.Count == 0 ? null : _receipts[_receipts.Count - 1];
    }

    public bool TryGetReceipt(long sequence, out PersistenceReceipt? receipt)
    {
        receipt = _receipts.FirstOrDefault(item => item.Sequence == sequence);
        return receipt is not null;
    }

    public string[] BuildOutputLines()
    {
        var lines = new List<string>
        {
            string.Format(
                CultureInfo.InvariantCulture,
                "== B111/F2 — recibos de persistência == OperationId='{0}' Receipts={1} StageFailures={2}",
                OperationId,
                _receipts.Count,
                _stageFailures.Count),
        };

        foreach (var receipt in _receipts)
        {
            lines.Add(string.Format(
                CultureInfo.InvariantCulture,
                "Receipt Sequence={0} Attempt={1} RetryOf={2} Operation='{3}' Stage='{4}' ObjectType='{5}' Name='{6}' Outcome='{7}' AttemptState='{8}' Confirmation='{9}' PhysicalState='{10}' RetryEligible={11} DurationMs={12} Identity='{13}' Observed='{14}' Detail='{15}' Exception='{16}'",
                receipt.Sequence,
                receipt.Attempt,
                receipt.RetryOfSequence?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                receipt.OperationKind,
                receipt.Stage,
                receipt.ObjectType,
                Clean(receipt.PlannedName),
                receipt.Outcome,
                receipt.AttemptState,
                receipt.Confirmation,
                receipt.PhysicalState,
                receipt.RetryEligible,
                receipt.DurationMs,
                Clean(receipt.Identity.Display),
                Clean(receipt.ObservedIdentity),
                Clean(receipt.ConfirmationDetail),
                Clean(receipt.ExceptionMessage)));
        }

        foreach (var failure in _stageFailures)
        {
            lines.Add(string.Format(
                CultureInfo.InvariantCulture,
                "StageFailed Stage='{0}' ReasonCode='{1}' Detail='{2}'",
                Clean(failure.Stage),
                Clean(failure.ReasonCode),
                Clean(failure.Detail)));
        }

        return lines.ToArray();
    }

    internal int GetNextAttempt(string operationKind, string objectType, PersistenceIdentity identity)
    {
        var key = BuildTargetKey(operationKind, objectType, identity);
        return _lastByTarget.TryGetValue(key, out var previous) ? previous.Attempt + 1 : 1;
    }

    internal PersistenceReceipt StartReceipt(
        string operationKind,
        string objectType,
        string stage,
        string plannedName,
        PersistenceIdentity identity,
        int attempt)
    {
        var key = BuildTargetKey(operationKind, objectType, identity);
        var retryOf = _lastByTarget.TryGetValue(key, out var previous) ? previous.Sequence : (long?)null;
        var receipt = new PersistenceReceipt(
            OperationId,
            ++_nextSequence,
            operationKind,
            objectType,
            stage,
            plannedName,
            identity,
            attempt,
            retryOf,
            DateTimeOffset.UtcNow);
        _receipts.Add(receipt);
        _lastByTarget[key] = receipt;
        return receipt;
    }

    internal void CompleteReceipt(
        PersistenceReceipt receipt,
        PersistenceAttemptState attemptState,
        PersistenceOutcome outcome,
        PersistenceConfirmation observation,
        Exception? exception,
        bool confirmationRead)
    {
        receipt.Complete(attemptState, outcome, observation, exception, confirmationRead);
    }

    internal PersistenceReceipt RecordNotAttempted(
        string operationKind,
        string objectType,
        string stage,
        string plannedName,
        PersistenceIdentity identity,
        PersistenceConfirmation observation)
    {
        var receipt = new PersistenceReceipt(
            OperationId,
            ++_nextSequence,
            operationKind,
            objectType,
            stage,
            plannedName,
            identity,
            attempt: 1,
            retryOfSequence: null,
            startedAt: DateTimeOffset.UtcNow);
        _receipts.Add(receipt);
        receipt.CompleteNotAttempted(observation);
        return receipt;
    }

    internal void NoteStageFailed(string stage, string reasonCode, string detail)
    {
        _stageFailures.Add(new PersistenceStageFailure(
            OperationId,
            stage ?? string.Empty,
            reasonCode ?? string.Empty,
            detail ?? string.Empty));
    }

    private static string BuildTargetKey(string operationKind, string objectType, PersistenceIdentity identity) =>
        operationKind + "|" + objectType + "|" + identity.StableKey;

    private static string Clean(string? value) => string.IsNullOrEmpty(value) ? string.Empty : value!.Replace("\r", " ").Replace("\n", " ");
}
