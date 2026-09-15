#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;
using Artech.Genexus.Common.Wiki;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// S-B111 / F3 P5 — leitura e observação para a recuperação.
///
/// O reader resolve o diário único da KB pelo nome fixo no índice já montado, valida o
/// envelope e relê cada alvo do inventário **por identidade**. Nome isolado nunca resolve um
/// alvo: quando a identidade é composta, a Description canônica e a unicidade na KB fazem
/// parte da leitura.
///
/// Nada aqui grava. Ausência, duplicidade, colisão externa e conteúdo inválido no nome fixo
/// são resultados distintos, e todos bloqueiam por motivos diferentes — é isso que a
/// recuperação precisa saber para dizer o que fazer.
/// </summary>
internal static class ApiPlanRecoveryReader
{
    /// <summary>
    /// Localiza e valida o diário da KB. Devolve o envelope com a vinculação externa
    /// (<c>FileId</c>) e o hash canônico do snapshot lido, ou o diagnóstico de indisponibilidade.
    /// </summary>
    internal static ApiPlanValidatedJournal ReadAndValidate(
        KBModel designModel,
        ApiPlanKbObjectNameIndex? kbIndex,
        Guid knowledgeBaseGuid)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        var lookup = ApiPlanOperationJournalStore.Locate(designModel, kbIndex);
        switch (lookup.Kind)
        {
            case ApiPlanOperationJournalLookupKind.Absent:
                return ApiPlanValidatedJournal.Unavailable(
                    JournalGateReasonCodes.JournalMissing,
                    JournalGatePrecondition.JournalAvailable,
                    "A KB não tem diário de operação: nenhuma operação desta ferramenta ficou pendente aqui.");

            case ApiPlanOperationJournalLookupKind.Ambiguous:
                return ApiPlanValidatedJournal.Unavailable(
                    JournalGateReasonCodes.JournalDuplicate,
                    JournalGatePrecondition.JournalAvailable,
                    lookup.Detail);

            case ApiPlanOperationJournalLookupKind.ExternalCollision:
                return ApiPlanValidatedJournal.Unavailable(
                    JournalGateReasonCodes.JournalIdentityDivergent,
                    JournalGatePrecondition.JournalIdentityConfirmed,
                    lookup.Detail);

            case ApiPlanOperationJournalLookupKind.Unreadable:
                return ApiPlanValidatedJournal.Unavailable(
                    JournalGateReasonCodes.JournalInvalid,
                    JournalGatePrecondition.JournalAvailable,
                    lookup.Detail);
        }

        var journal = lookup.Journal!;
        if (knowledgeBaseGuid != Guid.Empty
            && journal.KnowledgeBaseGuid != Guid.Empty
            && journal.KnowledgeBaseGuid != knowledgeBaseGuid)
        {
            return ApiPlanValidatedJournal.Unavailable(
                JournalGateReasonCodes.JournalIdentityDivergent,
                JournalGatePrecondition.JournalIdentityConfirmed,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "O diário encontrado pertence à KB '{0}', e a KB aberta é '{1}'. Um diário de outra KB não governa esta.",
                    journal.KnowledgeBaseGuid,
                    knowledgeBaseGuid));
        }

        return ApiPlanValidatedJournal.Valid(lookup.File!, journal, lookup.SnapshotHash);
    }

    /// <summary>
    /// Relê cada alvo do inventário por identidade. Um alvo que não pode ser lido fica
    /// <c>Unknown</c> — ausência de leitura nunca vira ausência do objeto.
    /// </summary>
    internal static IReadOnlyList<RecoveryTargetObservation> ObserveTargets(
        KBModel designModel,
        ApiPlanOperationJournal journal,
        ApiPlanKbObjectNameIndex kbIndex)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (journal is null)
        {
            throw new ArgumentNullException(nameof(journal));
        }

        if (kbIndex is null)
        {
            throw new ArgumentNullException(nameof(kbIndex));
        }

        var observations = new List<RecoveryTargetObservation>(journal.Inventory.Count);
        foreach (var item in journal.Inventory)
        {
            observations.Add(Observe(designModel, kbIndex, item));
        }

        return observations;
    }

    private static RecoveryTargetObservation Observe(
        KBModel designModel,
        ApiPlanKbObjectNameIndex kbIndex,
        ApiPlanOperationJournalInventoryItem item)
    {
        try
        {
            switch (item.ObjectType)
            {
                case JournalObjectType.ApiObject:
                    return ByGuidOrName(
                        item,
                        kbIndex.FindApis(item.Name).Select(api => api.Guid).ToArray(),
                        kbIndex.FindApis(item.Name).Select(api => api.Description).ToArray());

                case JournalObjectType.Procedure:
                    return ByGuidOrName(
                        item,
                        kbIndex.FindProcedures(item.Name).Select(procedure => procedure.Guid).ToArray(),
                        kbIndex.FindProcedures(item.Name).Select(procedure => procedure.Description).ToArray());

                case JournalObjectType.Sdt:
                    return ByGuidOrName(
                        item,
                        kbIndex.FindSdts(item.Name).Select(sdt => sdt.Guid).ToArray(),
                        kbIndex.FindSdts(item.Name).Select(sdt => sdt.Description).ToArray());

                case JournalObjectType.MetadataFile:
                    return ByGuidOrName(
                        item,
                        kbIndex.FindFiles(item.Name).Select(file => file.Guid).ToArray(),
                        kbIndex.FindFiles(item.Name).Select(file => file.Description).ToArray());

                case JournalObjectType.Folder:
                    var folders = Folder.GetAll(designModel)
                        .Where(folder => string.Equals(folder.Name, item.Name, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    return Present(item, folders.Length, "Folder");

                case JournalObjectType.Transaction:
                    var transactions = Transaction.GetAll(designModel)
                        .Where(transaction => item.Guid.HasValue && transaction.Guid == item.Guid.Value)
                        .ToArray();
                    return Present(item, transactions.Length, "Transaction");

                default:
                    return new RecoveryTargetObservation(
                        item,
                        JournalPhysicalState.Unknown,
                        JournalConfirmation.NotAttempted,
                        "Tipo sem leitura por identidade.");
            }
        }
        catch (Exception exception)
        {
            return new RecoveryTargetObservation(
                item,
                JournalPhysicalState.Unknown,
                JournalConfirmation.Unreadable,
                exception.GetType().FullName + ": " + exception.Message.Replace("\r", " ").Replace("\n", " "));
        }
    }

    /// <summary>
    /// Alvo com GUID registrado é resolvido pelo GUID. Alvo de identidade composta — o legado,
    /// que nunca teve GUID no envelope — exige nome exato, unicidade **e** Description canônica:
    /// nome isolado não prova posse nem existência do alvo certo.
    /// </summary>
    private static RecoveryTargetObservation ByGuidOrName(
        ApiPlanOperationJournalInventoryItem item,
        IReadOnlyList<Guid> guids,
        IReadOnlyList<string?> descriptions)
    {
        if (item.Guid.HasValue && item.Guid.Value != Guid.Empty)
        {
            var found = guids.Any(guid => guid == item.Guid.Value);
            return new RecoveryTargetObservation(
                item,
                found ? JournalPhysicalState.Present : JournalPhysicalState.Absent,
                found ? JournalConfirmation.Confirmed : JournalConfirmation.Absent,
                found ? "Reencontrado pelo GUID registrado." : "Ausente: o GUID registrado não está na KB.");
        }

        if (guids.Count == 0)
        {
            return new RecoveryTargetObservation(
                item,
                JournalPhysicalState.Absent,
                JournalConfirmation.Absent,
                "Ausente: nenhum objeto com o nome exato do inventário.");
        }

        if (guids.Count > 1)
        {
            return new RecoveryTargetObservation(
                item,
                JournalPhysicalState.Unknown,
                JournalConfirmation.Divergent,
                "Ambíguo: mais de um objeto com o nome exato do inventário.");
        }

        var canonical = item.Composite?.CanonicalDescription;
        if (!string.IsNullOrWhiteSpace(canonical)
            && !string.Equals(descriptions.Count > 0 ? descriptions[0] : null, canonical, StringComparison.Ordinal))
        {
            return new RecoveryTargetObservation(
                item,
                JournalPhysicalState.Unknown,
                JournalConfirmation.Divergent,
                "Divergente: o objeto com esse nome não tem a Description canônica registrada.");
        }

        return new RecoveryTargetObservation(
            item,
            JournalPhysicalState.Present,
            JournalConfirmation.Confirmed,
            "Reencontrado pela identidade histórica composta.");
    }

    private static RecoveryTargetObservation Present(
        ApiPlanOperationJournalInventoryItem item,
        int matches,
        string kind)
    {
        if (matches == 1)
        {
            return new RecoveryTargetObservation(item, JournalPhysicalState.Present, JournalConfirmation.Confirmed, kind + " reencontrado.");
        }

        if (matches == 0)
        {
            return new RecoveryTargetObservation(item, JournalPhysicalState.Absent, JournalConfirmation.Absent, kind + " ausente.");
        }

        return new RecoveryTargetObservation(item, JournalPhysicalState.Unknown, JournalConfirmation.Divergent, kind + " ambíguo na KB.");
    }
}

/// <summary>Resultado da leitura: envelope validado ou indisponibilidade classificada.</summary>
internal sealed class ApiPlanValidatedJournal
{
    private ApiPlanValidatedJournal(
        WikiFileKBObject? file,
        ApiPlanOperationJournal? journal,
        string snapshotHash,
        ApiPlanOperationJournalGateDiagnostic? diagnostic)
    {
        File = file;
        Journal = journal;
        SnapshotHash = snapshotHash;
        Diagnostic = diagnostic;
    }

    internal WikiFileKBObject? File { get; }

    internal ApiPlanOperationJournal? Journal { get; }

    /// <summary>Hash canônico do snapshot lido; é ele que a autorização vincula.</summary>
    internal string SnapshotHash { get; }

    internal ApiPlanOperationJournalGateDiagnostic? Diagnostic { get; }

    internal bool IsValid => Journal is not null;

    internal int FileId => File?.Id ?? 0;

    internal static ApiPlanValidatedJournal Valid(WikiFileKBObject file, ApiPlanOperationJournal journal, string snapshotHash) =>
        new ApiPlanValidatedJournal(file, journal, snapshotHash, null);

    internal static ApiPlanValidatedJournal Unavailable(
        string reasonCode,
        JournalGatePrecondition precondition,
        string message) =>
        new ApiPlanValidatedJournal(
            null,
            null,
            string.Empty,
            new ApiPlanOperationJournalGateDiagnostic(
                JournalGateDiagnosticCode.JournalUnavailable,
                reasonCode,
                precondition,
                message,
                Array.Empty<KeyValuePair<string, string>>()));
}

/// <summary>
/// S-B111 / F3 P5 — execução da etapa autorizada.
///
/// O executor faz **uma** coisa por chamada, e só depois de revalidar o diário: relê o mesmo
/// <c>FileId</c>, confere identidade, <c>updatedUtc</c> e hash canônico contra a autorização, e
/// só então grava. Qualquer divergência entre a leitura que produziu o consentimento e o estado
/// de agora recusa a execução antes da mutação.
///
/// O lock é local ao processo, indexado pela KB: ele coordena duas continuações na mesma
/// instância da extensão, e não promete atomicidade entre duas IDEs. Onde ele não alcança, a
/// revalidação otimista é a defesa — e uma corrida posterior reaparece no checkpoint seguinte
/// como indeterminação, nunca como sucesso.
/// </summary>
internal static class ApiPlanRecoveryExecutor
{
    private static readonly Dictionary<Guid, object> Locks = new Dictionary<Guid, object>();
    private const int LockTimeoutMs = 5000;

    /// <summary>
    /// Executa a etapa autorizada sobre o envelope reidratado.
    /// </summary>
    /// <param name="removalContinuation">
    /// Retomada da fila de remoção, fornecida pelo chamador porque depende do plano e da KB.
    /// Ausente, <see cref="RecoveryNextStep.ContinueRemovePass"/> é recusado — sem executor de
    /// fila, dizer que a remoção continuou seria falso.
    /// </param>
    internal static ApiPlanRecoveryResult Continue(
        KBModel designModel,
        ApiPlanValidatedJournal validated,
        ApiPlanRehydratedOperation operation,
        RecoveryAuthorization authorization,
        Func<ApiPlanRehydratedOperation, ApiPlanRecoveryResult>? removalContinuation = null)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (validated is null || !validated.IsValid)
        {
            throw new ArgumentException("A execução exige um diário validado.", nameof(validated));
        }

        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        if (authorization is null)
        {
            throw new ArgumentNullException(nameof(authorization));
        }

        if (operation.AlreadyTerminal || !operation.CanExecute)
        {
            return ApiPlanRecoveryResult.Blocked(
                operation,
                operation.Diagnostic ?? new ApiPlanOperationJournalGateDiagnostic(
                    JournalGateDiagnosticCode.GateBlocked,
                    JournalGateReasonCodes.JournalNonTerminal,
                    JournalGatePrecondition.PriorIntentReconciled,
                    "Não há etapa executável para este envelope.",
                    Array.Empty<KeyValuePair<string, string>>()));
        }

        var knowledgeBaseGuid = operation.Journal.KnowledgeBaseGuid;
        var gate = AcquireLock(knowledgeBaseGuid);
        if (gate is null)
        {
            return ApiPlanRecoveryResult.Blocked(
                operation,
                new ApiPlanOperationJournalGateDiagnostic(
                    JournalGateDiagnosticCode.GateBlocked,
                    JournalGateReasonCodes.RecoveryAuthorizationLockUnavailable,
                    JournalGatePrecondition.NoActiveIntent,
                    "Outra continuação desta KB está em andamento nesta sessão. A execução não prossegue "
                    + "como se houvesse atomicidade.",
                    Array.Empty<KeyValuePair<string, string>>()));
        }

        try
        {
            // Revalidação imediatamente antes da mutação: entre a leitura e a ação, outra sessão
            // pode ter substituído o snapshot que fundamentou o consentimento.
            var fresh = ApiPlanRecoveryReader.ReadAndValidate(designModel, kbIndex: null, knowledgeBaseGuid);
            if (!fresh.IsValid)
            {
                return ApiPlanRecoveryResult.Blocked(operation, fresh.Diagnostic!);
            }

            var stale = authorization.Validate(operation, fresh.FileId, fresh.SnapshotHash);
            if (stale is not null)
            {
                return ApiPlanRecoveryResult.Blocked(operation, stale);
            }

            switch (operation.NextStep)
            {
                case RecoveryNextStep.Abandon:
                    return Write(
                        designModel,
                        fresh,
                        operation,
                        journal => ApiPlanOperationJournalCheckpoints.Abandon(
                            journal,
                            "Abandono autorizado na recuperação: o envelope estava preparado e não gravou nada na KB.",
                            authorization.AuthorizedBy,
                            DateTime.UtcNow),
                        "O envelope preparado foi abandonado. A KB está liberada para a próxima operação.");

                case RecoveryNextStep.Complete:
                    return Write(
                        designModel,
                        fresh,
                        operation,
                        journal => ApiPlanOperationJournalCheckpoints.ReconcileRemoved(journal, DateTime.UtcNow),
                        "A remoção foi reconciliada: todos os alvos previstos estão ausentes e o envelope "
                        + "fechou como concluído.");

                case RecoveryNextStep.Discard:
                    return Write(
                        designModel,
                        fresh,
                        operation,
                        journal => ApiPlanOperationJournalCheckpoints.DiscardInterrupted(
                            journal,
                            "Registro encerrado na recuperação: a operação parou no meio e a continuação não era possível.",
                            authorization.AuthorizedBy,
                            DateTime.UtcNow),
                        "O registro da operação interrompida foi encerrado. A Knowledge Base está liberada "
                        + "para a próxima operação; nenhum objeto foi apagado, e o inventário do que ficou "
                        + "pela metade continua gravado no diário.");

                case RecoveryNextStep.ContinueRemovePass:
                    if (removalContinuation is null)
                    {
                        return ApiPlanRecoveryResult.Blocked(
                            operation,
                            new ApiPlanOperationJournalGateDiagnostic(
                                JournalGateDiagnosticCode.GateBlocked,
                                JournalGateReasonCodes.RecoveryAuthorizationLockUnavailable,
                                JournalGatePrecondition.NoActiveIntent,
                                "A retomada da fila de remoção não foi fornecida por quem chamou a recuperação.",
                                Array.Empty<KeyValuePair<string, string>>()));
                    }

                    return removalContinuation(operation);

                default:
                    return ApiPlanRecoveryResult.Blocked(
                        operation,
                        new ApiPlanOperationJournalGateDiagnostic(
                            JournalGateDiagnosticCode.GateBlocked,
                            JournalGateReasonCodes.JournalNonTerminal,
                            JournalGatePrecondition.PriorIntentReconciled,
                            "Etapa sem execução disponível nesta versão: " + operation.NextStep + ".",
                            Array.Empty<KeyValuePair<string, string>>()));
            }
        }
        finally
        {
            Monitor.Exit(gate);
        }
    }

    private static ApiPlanRecoveryResult Write(
        KBModel designModel,
        ApiPlanValidatedJournal validated,
        ApiPlanRehydratedOperation operation,
        Action<ApiPlanOperationJournal> transition,
        string summary)
    {
        var envelope = validated.Journal!;
        try
        {
            transition(envelope);
        }
        catch (InvalidOperationException exception)
        {
            return ApiPlanRecoveryResult.Blocked(
                operation,
                new ApiPlanOperationJournalGateDiagnostic(
                    JournalGateDiagnosticCode.PreconditionFailed,
                    JournalGateReasonCodes.AuthorizationMismatch,
                    JournalGatePrecondition.PriorIntentReconciled,
                    "A transição autorizada não é válida no estado revalidado: " + exception.Message,
                    Array.Empty<KeyValuePair<string, string>>()));
        }

        var store = new ApiPlanOperationJournalStore(designModel, validated.File);
        var checkpoint = store.WriteCheckpoint(envelope);
        if (!checkpoint.IsConfirmed)
        {
            return ApiPlanRecoveryResult.Blocked(
                operation,
                ApiPlanOperationJournalGate.SaveUnconfirmed(store.FileId, checkpoint.Detail));
        }

        return ApiPlanRecoveryResult.Applied(operation, envelope, summary);
    }

    private static object? AcquireLock(Guid knowledgeBaseGuid)
    {
        object gate;
        lock (Locks)
        {
            if (!Locks.TryGetValue(knowledgeBaseGuid, out gate!))
            {
                gate = new object();
                Locks[knowledgeBaseGuid] = gate;
            }
        }

        return Monitor.TryEnter(gate, LockTimeoutMs) ? gate : null;
    }
}

/// <summary>Desfecho de uma execução de recuperação.</summary>
internal sealed class ApiPlanRecoveryResult
{
    private ApiPlanRecoveryResult(
        ApiPlanRehydratedOperation operation,
        ApiPlanOperationJournal? envelope,
        bool executed,
        string summary,
        ApiPlanOperationJournalGateDiagnostic? diagnostic)
    {
        Operation = operation;
        Envelope = envelope;
        Executed = executed;
        Summary = summary;
        Diagnostic = diagnostic;
    }

    internal ApiPlanRehydratedOperation Operation { get; }

    internal ApiPlanOperationJournal? Envelope { get; }

    internal bool Executed { get; }

    internal string Summary { get; }

    internal ApiPlanOperationJournalGateDiagnostic? Diagnostic { get; }

    internal static ApiPlanRecoveryResult Applied(ApiPlanRehydratedOperation operation, ApiPlanOperationJournal envelope, string summary) =>
        new ApiPlanRecoveryResult(operation, envelope, true, summary, null);

    internal static ApiPlanRecoveryResult Blocked(ApiPlanRehydratedOperation operation, ApiPlanOperationJournalGateDiagnostic diagnostic) =>
        new ApiPlanRecoveryResult(operation, null, false, diagnostic.Message, diagnostic);
}
