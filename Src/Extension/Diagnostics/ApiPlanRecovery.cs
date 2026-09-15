#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// S-B111 / F3 P5 — reidratação de um envelope do diário (seção 5.4.1 do plano da F3).
///
/// A recuperação **não reabre o Wizard** e não resolve um alvo por nome. Ela lê o envelope
/// durável, cruza o inventário registrado com o que a KB mostra agora e escolhe **uma** próxima
/// etapa autorizada — ou bloqueia. Quando a reconciliação não é determinística, bloquear é o
/// resultado certo: continuar às cegas é o que a frente inteira existe para impedir.
///
/// O que esta etapa deliberadamente **não** faz: retomar o pipeline de Apply ou Sync. O
/// envelope guarda a identidade da API, o hash do contrato e as flags de geração — não o
/// contrato em si. Reconstruir um `ApiPlan` a partir disso seria inventá-lo, e um plano
/// inventado grava objetos que ninguém pediu. Para esses envelopes a decisão é
/// <see cref="RecoveryNextStep.Block"/>, com o motivo explícito.
///
/// SDK-free de propósito: quem lê o File é o reader, quem grava é o executor.
/// </summary>
public static class ApiPlanRecoveryRehydrator
{
    /// <summary>
    /// Cruza o envelope validado com as observações físicas e devolve a única etapa autorizada.
    /// </summary>
    public static ApiPlanRehydratedOperation Rehydrate(
        ApiPlanOperationJournal journal,
        IReadOnlyList<RecoveryTargetObservation> observations)
    {
        if (journal is null)
        {
            throw new ArgumentNullException(nameof(journal));
        }

        if (observations is null)
        {
            throw new ArgumentNullException(nameof(observations));
        }

        var classified = Classify(journal, observations);

        // --- Estados terminais: não há o que recuperar ---------------------------------------
        if (journal.OperationState == JournalOperationState.Completed
            || journal.OperationState == JournalOperationState.Removed)
        {
            return ApiPlanRehydratedOperation.Terminal(journal, classified);
        }

        // --- Durabilidade desconhecida: o snapshot corrente não é confiável -------------------
        if (journal.JournalDurability != JournalDurability.Confirmed)
        {
            return ApiPlanRehydratedOperation.Blocked(
                journal,
                classified,
                JournalGateDiagnosticCode.DurabilityUnknown,
                JournalGateReasonCodes.JournalReloadDivergent,
                "A durabilidade do último snapshot do diário não foi confirmada. Reconcilie a operação "
                + "antes de qualquer continuação: uma gravação incerta não vira certeza por repetição.");
        }

        // --- Envelope preparado e nunca iniciado ---------------------------------------------
        if (journal.EnvelopePhase == JournalEnvelopePhase.Prepared
            && journal.OperationState == JournalOperationState.Pending)
        {
            if (journal.Receipts.Count > 0)
            {
                // Prepared com recibo de negócio é contradição do próprio envelope: ele diz que
                // nada foi gravado e os recibos dizem o contrário.
                return ApiPlanRehydratedOperation.Blocked(
                    journal,
                    classified,
                    JournalGateDiagnosticCode.PreconditionFailed,
                    JournalGateReasonCodes.InventoryInsufficient,
                    "O envelope está Prepared, mas registra recibos de gravação. Abandoná-lo apagaria a "
                    + "única prova do que foi gravado; a reconciliação é humana.");
            }

            return ApiPlanRehydratedOperation.Authorized(
                journal,
                classified,
                RecoveryNextStep.Abandon,
                "A operação foi preparada e nunca gravou nada na KB. Abandoná-la explicitamente libera a "
                + "KB para a próxima operação, preservando o registro da disposição.");
        }

        // --- Resultado indeterminado ----------------------------------------------------------
        if (journal.OperationState == JournalOperationState.OutcomeUnknown)
        {
            return ApiPlanRehydratedOperation.Blocked(
                journal,
                classified,
                JournalGateDiagnosticCode.GateBlocked,
                JournalGateReasonCodes.UnreconciledOutcome,
                "A última gravação terminou com resultado desconhecido. A consulta por identidade precisa "
                + "ser feita e conferida por uma pessoa antes de qualquer nova gravação.");
        }

        // --- Remoção interrompida --------------------------------------------------------------
        if (journal.OperationKind == JournalOperationKind.Remove)
        {
            return RehydrateRemoval(journal, classified);
        }

        // --- Apply, Sync e Recovery: o envelope não carrega o contrato ---------------------------
        //
        // Retomar o pipeline não é possível, mas deixar a KB travada também não é resposta: o
        // envelope interrompido bloqueia todas as operações seguintes. O que se oferece é
        // encerrar o registro — decisão humana, informada pelo inventário, que não apaga nada.
        return ApiPlanRehydratedOperation.Authorized(
            journal,
            classified,
            RecoveryNextStep.Discard,
            string.Format(
                CultureInfo.InvariantCulture,
                "A operação {0} parou em {1}/{2}. O diário registra identidade, contrato por hash e o que já "
                + "foi confirmado — não o contrato em si —, então retomar o pipeline a partir dele seria "
                + "inventar um plano. O que a ferramenta pode fazer é encerrar este registro: a Knowledge "
                + "Base é liberada, nada é apagado, e o que ficou pela metade continua como está. Depois "
                + "disso, as duas saídas são reaplicar pelo Wizard sobre o estado atual — o reencontro "
                + "conservador cuida do que já existe — ou remover a API gerada.",
                journal.OperationKind,
                journal.OperationState,
                journal.LogicalStage));
    }

    private static ApiPlanRehydratedOperation RehydrateRemoval(
        ApiPlanOperationJournal journal,
        IReadOnlyList<RecoveryTargetState> classified)
    {
        var pendingDeletes = classified
            .Where(item => item.Item.Action == JournalInventoryAction.Delete)
            .Where(item => item.PhysicalState != JournalPhysicalState.Absent)
            .ToArray();

        var unreadable = pendingDeletes
            .Where(item => item.PhysicalState == JournalPhysicalState.Unknown)
            .ToArray();
        if (unreadable.Length > 0)
        {
            return ApiPlanRehydratedOperation.Blocked(
                journal,
                classified,
                JournalGateDiagnosticCode.PreconditionFailed,
                JournalGateReasonCodes.IdentityDivergent,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} alvo(s) previsto(s) não puderam ser lidos por identidade: {1}. Sem saber se ainda "
                    + "estão na KB, a fila não pode ser retomada.",
                    unreadable.Length,
                    string.Join(", ", unreadable.Select(item => item.Item.Name))));
        }

        if (pendingDeletes.Length == 0)
        {
            // Todos os alvos previstos estão confirmadamente ausentes: a remoção terminou, e o
            // que ficou pendente foi o registro dela. Essa reconciliação é determinística.
            return ApiPlanRehydratedOperation.Authorized(
                journal,
                classified,
                RecoveryNextStep.Complete,
                "Todos os alvos previstos estão ausentes da KB: a remoção chegou ao fim e só o registro "
                + "ficou aberto. Fechar o envelope como concluído reconcilia o diário com a KB.");
        }

        if (journal.BlockReason == JournalBlockReason.TargetAbsentBeforeDelete)
        {
            // Retomar a fila às cegas continua proibido; o que se oferece é encerrar o registro,
            // com o inventário à vista. A KB não é tocada por essa decisão.
            return ApiPlanRehydratedOperation.Authorized(
                journal,
                classified,
                RecoveryNextStep.Discard,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "A remoção parou porque um alvo previsto já não estava na KB antes da exclusão, e quem "
                    + "o apagou não foi esta operação: retomar a fila às cegas não é possível. {0} alvo(s) "
                    + "previstos ainda estão na KB, listados abaixo. Encerrar este registro libera a "
                    + "Knowledge Base e não apaga nada; o que estiver pela metade continua como está, para "
                    + "você decidir depois.",
                    pendingDeletes.Length));
        }

        return ApiPlanRehydratedOperation.Authorized(
            journal,
            classified,
            RecoveryNextStep.ContinueRemovePass,
            string.Format(
                CultureInfo.InvariantCulture,
                "{0} alvo(s) previsto(s) continuam na KB, listados abaixo. A fila pode ser retomada com "
                + "o mesmo inventário e o mesmo envelope.",
                pendingDeletes.Length));
    }

    /// <summary>
    /// Cruza cada item do inventário com a observação física correspondente. Um item sem
    /// observação vira <c>Unknown</c>: ausência de leitura nunca é ausência do objeto.
    /// </summary>
    private static IReadOnlyList<RecoveryTargetState> Classify(
        ApiPlanOperationJournal journal,
        IReadOnlyList<RecoveryTargetObservation> observations)
    {
        var byKey = new Dictionary<string, RecoveryTargetObservation>(StringComparer.Ordinal);
        foreach (var observation in observations)
        {
            byKey[observation.IdentityKey] = observation;
        }

        var states = new List<RecoveryTargetState>(journal.Inventory.Count);
        foreach (var item in journal.Inventory)
        {
            var key = ApiPlanOperationJournalValidator.BuildIdentityKey(item);
            if (byKey.TryGetValue(key, out var observation))
            {
                states.Add(new RecoveryTargetState(item, observation.PhysicalState, observation.Confirmation, observation.Detail));
                continue;
            }

            states.Add(new RecoveryTargetState(
                item,
                JournalPhysicalState.Unknown,
                JournalConfirmation.NotAttempted,
                "Sem leitura por identidade nesta reidratação."));
        }

        return states;
    }
}

/// <summary>
/// O que a KB mostra **agora** sobre um alvo do inventário. O nome serve a diagnóstico; a
/// ligação com o inventário é pela identidade.
/// </summary>
public sealed class RecoveryTargetObservation
{
    public RecoveryTargetObservation(
        ApiPlanOperationJournalInventoryItem item,
        JournalPhysicalState physicalState,
        JournalConfirmation confirmation,
        string detail = "")
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        IdentityKey = ApiPlanOperationJournalValidator.BuildIdentityKey(item);
        Name = item.Name;
        ObjectType = item.ObjectType;
        PhysicalState = physicalState;
        Confirmation = confirmation;
        Detail = detail ?? string.Empty;
    }

    public string IdentityKey { get; }

    public string Name { get; }

    public JournalObjectType ObjectType { get; }

    public JournalPhysicalState PhysicalState { get; }

    public JournalConfirmation Confirmation { get; }

    public string Detail { get; }
}

/// <summary>Um item do inventário com a observação física cruzada.</summary>
public sealed class RecoveryTargetState
{
    internal RecoveryTargetState(
        ApiPlanOperationJournalInventoryItem item,
        JournalPhysicalState physicalState,
        JournalConfirmation confirmation,
        string detail)
    {
        Item = item;
        PhysicalState = physicalState;
        Confirmation = confirmation;
        Detail = detail;
    }

    public ApiPlanOperationJournalInventoryItem Item { get; }

    public JournalPhysicalState PhysicalState { get; }

    public JournalConfirmation Confirmation { get; }

    public string Detail { get; }

    public string Describe() => string.Format(
        CultureInfo.InvariantCulture,
        "{0} {1} — previsto: {2}; na KB: {3}",
        Item.ObjectType,
        Item.Name,
        Item.Action,
        PhysicalState);
}

/// <summary>
/// As etapas que uma recuperação pode indicar. O conjunto é fechado pela seção 5.4.1: nenhuma
/// delas é inventada em tempo de execução, e só uma é autorizada por vez.
/// </summary>
public enum RecoveryNextStep
{
    ContinueTransaction = 0,
    ContinueFolder = 1,
    ContinueSdts = 2,
    ContinueProcedures = 3,
    ContinueApi = 4,
    ContinueMetadata = 5,
    ContinueRemovePass = 6,
    Complete = 7,
    Abandon = 8,
    Block = 9,

    /// <summary>
    /// Encerrar o registro de uma operação que gravou alguma coisa e parou no meio.
    ///
    /// **Valor acrescentado em 2026-09-14**, fora da lista original da seção 5.4.1. Ele nasceu
    /// de uma consequência que o conjunto fechado não cobria: um envelope interrompido bloqueia
    /// as operações seguintes, e quando a continuação não é possível — um `Apply` cujo contrato
    /// não está no envelope — só restava apagar o File do diário à mão, que apaga também a
    /// prova. A base contratual é a seção 4.1.1: voltar a uma situação sem intenção ativa é
    /// admitido mediante confirmação humana. É efêmero como o resto deste enum: o que fica
    /// gravado é <c>logicalStage=Discarded</c>.
    /// </summary>
    Discard = 10,
}

/// <summary>
/// O envelope reidratado: identidade preservada, inventário cruzado com a KB e a única etapa
/// autorizada — ou o bloqueio, com o motivo classificado.
/// </summary>
public sealed class ApiPlanRehydratedOperation
{
    private ApiPlanRehydratedOperation(
        ApiPlanOperationJournal journal,
        IReadOnlyList<RecoveryTargetState> targets,
        RecoveryNextStep nextStep,
        bool alreadyTerminal,
        ApiPlanOperationJournalGateDiagnostic? diagnostic,
        string summary)
    {
        Journal = journal;
        Targets = targets;
        NextStep = nextStep;
        AlreadyTerminal = alreadyTerminal;
        Diagnostic = diagnostic;
        Summary = summary;
    }

    public ApiPlanOperationJournal Journal { get; }

    public IReadOnlyList<RecoveryTargetState> Targets { get; }

    public RecoveryNextStep NextStep { get; }

    /// <summary>A operação já estava encerrada: não há o que recuperar.</summary>
    public bool AlreadyTerminal { get; }

    /// <summary>Preenchido quando <see cref="NextStep"/> é <see cref="RecoveryNextStep.Block"/>.</summary>
    public ApiPlanOperationJournalGateDiagnostic? Diagnostic { get; }

    /// <summary>Explicação para leitura humana do que foi decidido e por quê.</summary>
    public string Summary { get; }

    public Guid OperationId => Journal.OperationId;

    public Guid ApplicationId => Journal.ApplicationId;

    public JournalOperationKind OperationKind => Journal.OperationKind;

    /// <summary>Há uma ação que a ferramenta pode executar com autorização humana.</summary>
    public bool CanExecute =>
        !AlreadyTerminal
        && (NextStep == RecoveryNextStep.Abandon
            || NextStep == RecoveryNextStep.Complete
            || NextStep == RecoveryNextStep.ContinueRemovePass
            || NextStep == RecoveryNextStep.Discard);

    /// <summary>
    /// A ação encerra o registro sem concluir a operação: a KB fica como está, e quem confirma
    /// precisa ver o inventário antes.
    /// </summary>
    public bool RequiresStateAwareness => NextStep == RecoveryNextStep.Discard;

    internal static ApiPlanRehydratedOperation Terminal(
        ApiPlanOperationJournal journal,
        IReadOnlyList<RecoveryTargetState> targets) =>
        new ApiPlanRehydratedOperation(
            journal,
            targets,
            RecoveryNextStep.Complete,
            alreadyTerminal: true,
            diagnostic: null,
            summary: string.Format(
                CultureInfo.InvariantCulture,
                "A operação {0} já está encerrada em {1}. Não há nada a recuperar; a KB está liberada para "
                + "a próxima operação.",
                journal.OperationKind,
                journal.OperationState));

    internal static ApiPlanRehydratedOperation Authorized(
        ApiPlanOperationJournal journal,
        IReadOnlyList<RecoveryTargetState> targets,
        RecoveryNextStep nextStep,
        string summary) =>
        new ApiPlanRehydratedOperation(journal, targets, nextStep, alreadyTerminal: false, diagnostic: null, summary);

    internal static ApiPlanRehydratedOperation Blocked(
        ApiPlanOperationJournal journal,
        IReadOnlyList<RecoveryTargetState> targets,
        JournalGateDiagnosticCode code,
        string reasonCode,
        string message)
    {
        var context = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("operationId", journal.OperationId.ToString("D", CultureInfo.InvariantCulture)),
            new KeyValuePair<string, string>("operationKind", journal.OperationKind.ToString()),
            new KeyValuePair<string, string>("operationState", journal.OperationState.ToString()),
            new KeyValuePair<string, string>("logicalStage", journal.LogicalStage.ToString()),
        };
        if (journal.BlockReason.HasValue)
        {
            context.Add(new KeyValuePair<string, string>("blockReason", journal.BlockReason.Value.ToString()));
        }

        return new ApiPlanRehydratedOperation(
            journal,
            targets,
            RecoveryNextStep.Block,
            alreadyTerminal: false,
            new ApiPlanOperationJournalGateDiagnostic(
                code,
                reasonCode,
                JournalGatePrecondition.PriorIntentReconciled,
                message,
                context),
            message);
    }
}

/// <summary>
/// Autorização humana para executar a etapa indicada. A vinculação ao snapshot é defesa de
/// frescor: entre ler o diário e agir sobre ele, outra sessão pode tê-lo substituído, e um
/// consentimento dado sobre um estado não vale para outro.
/// </summary>
public sealed class RecoveryAuthorization
{
    public RecoveryAuthorization(
        bool humanConfirmed,
        Guid operationId,
        Guid applicationId,
        int journalFileId,
        DateTime updatedUtc,
        string snapshotHash,
        RecoveryNextStep authorizedStep,
        string authorizedBy)
    {
        HumanConfirmed = humanConfirmed;
        OperationId = operationId;
        ApplicationId = applicationId;
        JournalFileId = journalFileId;
        UpdatedUtc = updatedUtc;
        SnapshotHash = snapshotHash ?? string.Empty;
        AuthorizedStep = authorizedStep;
        AuthorizedBy = authorizedBy ?? string.Empty;
    }

    public bool HumanConfirmed { get; }

    public Guid OperationId { get; }

    public Guid ApplicationId { get; }

    public int JournalFileId { get; }

    public DateTime UpdatedUtc { get; }

    public string SnapshotHash { get; }

    public RecoveryNextStep AuthorizedStep { get; }

    public string AuthorizedBy { get; }

    /// <summary>
    /// Confere a autorização contra o envelope revalidado. Qualquer divergência recusa a
    /// execução antes da mutação — é a verificação otimista da seção 5.4.1, não uma promessa de
    /// atomicidade entre processos.
    /// </summary>
    public ApiPlanOperationJournalGateDiagnostic? Validate(
        ApiPlanRehydratedOperation operation,
        int journalFileId,
        string snapshotHash)
    {
        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        if (!HumanConfirmed)
        {
            return Stale("A execução exige confirmação humana explícita.");
        }

        if (AuthorizedStep != operation.NextStep)
        {
            return Stale(string.Format(
                CultureInfo.InvariantCulture,
                "A autorização é para '{0}', e a etapa apurada agora é '{1}'.",
                AuthorizedStep,
                operation.NextStep));
        }

        if (OperationId != operation.Journal.OperationId || ApplicationId != operation.Journal.ApplicationId)
        {
            return Stale("Os identificadores da autorização não são os do envelope revalidado.");
        }

        if (JournalFileId != journalFileId)
        {
            return Stale("O diário revalidado está em outro File.");
        }

        if (UpdatedUtc != operation.Journal.UpdatedUtc)
        {
            return Stale("O envelope foi atualizado depois da leitura que produziu a autorização.");
        }

        if (!string.Equals(SnapshotHash, snapshotHash, StringComparison.Ordinal))
        {
            return Stale("O hash canônico do snapshot mudou entre a leitura e a ação.");
        }

        return null;
    }

    private static ApiPlanOperationJournalGateDiagnostic Stale(string message) =>
        new ApiPlanOperationJournalGateDiagnostic(
            JournalGateDiagnosticCode.GateBlocked,
            JournalGateReasonCodes.RecoveryAuthorizationStale,
            JournalGatePrecondition.NoActiveIntent,
            message,
            Array.Empty<KeyValuePair<string, string>>());
}

/// <summary>
/// Relatório da recuperação. Só apresenta: não grava, não decide e não cria recibos.
/// </summary>
public static class ApiPlanRecoveryReport
{
    public static IReadOnlyList<string> Describe(ApiPlanRehydratedOperation operation)
    {
        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        var journal = operation.Journal;
        var lines = new List<string>
        {
            string.Format(
                CultureInfo.InvariantCulture,
                "Operação {0} sobre '{1}': estado {2}/{3}, envelope {4}, durabilidade {5}.",
                journal.OperationKind,
                journal.TransactionName,
                journal.OperationState,
                journal.LogicalStage,
                journal.EnvelopePhase,
                journal.JournalDurability),
            string.Format(
                CultureInfo.InvariantCulture,
                "OperationId={0}, ApplicationId={1}, atualizado em {2:yyyy-MM-dd HH:mm:ss}Z.",
                journal.OperationId,
                journal.ApplicationId,
                journal.UpdatedUtc),
        };

        if (journal.BlockReason.HasValue)
        {
            lines.Add("Motivo registrado no envelope: " + journal.BlockReason.Value + ".");
        }

        lines.Add("Próxima etapa apurada: " + operation.NextStep + ".");
        lines.Add(operation.Summary);

        if (operation.Diagnostic is not null)
        {
            lines.Add(operation.Diagnostic.Describe());
        }

        foreach (var target in operation.Targets)
        {
            lines.Add("  - " + target.Describe());
        }

        return lines;
    }
}
