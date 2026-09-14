#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// S-B111 / F3 — gate estendido do diário durável (seção 4.2 do plano da F3).
///
/// O gate reduzido da F1 só sabia recusar uma dependência ausente. Este avalia o que depende
/// de **intenção durável** e, quando recusa, diz por quê de forma que uma máquina entenda: um
/// código de alto nível, uma <c>reasonCode</c> estável, a pré-condição que reprovou e o
/// contexto estruturado. A mensagem humana continua existindo, mas deixa de ser o único
/// veículo do motivo.
///
/// A precedência é fechada pela decisão 24 e implementada nesta ordem, que não é negociável:
///
/// 1. <see cref="JournalGateDiagnosticCode.JournalUnavailable"/> — o diário não pode ser lido,
///    validado ou identificado. Prevalece sobre tudo: sem diário legível não há estado global
///    sobre o qual decidir;
/// 2. <see cref="JournalGateDiagnosticCode.DurabilityUnknown"/> — o diário existe, mas o
///    snapshot corrente não pôde ser confirmado;
/// 3. <see cref="JournalGateDiagnosticCode.GateBlocked"/> — o diário é legível, válido e
///    durável, e é o **estado global** que impede a operação;
/// 4. <see cref="JournalGateDiagnosticCode.PreconditionFailed"/> — uma pré-condição específica
///    da operação falha antes da primeira mutação. Não é subcausa de <c>GateBlocked</c>.
///
/// Este tipo é SDK-free de propósito: o que depende da IDE é localizar o File, e isso fica no
/// store. Aqui entra o resultado já neutro dessa busca, para que a matriz de precedência seja
/// exercitada offline.
///
/// Nada que este gate produz é persistido. <c>reasonCode</c> e <c>blockReason</c> são
/// namespaces distintos — uma coincidência textual entre os dois nunca autoriza copiar um
/// diagnóstico efêmero para o envelope.
/// </summary>
public static class ApiPlanOperationJournalGate
{
    /// <summary>
    /// Avalia se uma operação nova pode começar. Nunca lança por estado da KB: um estado que
    /// impede a operação é resultado, não exceção.
    /// </summary>
    public static ApiPlanOperationJournalGateDecision Evaluate(ApiPlanOperationJournalGateInput input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        // --- 1. Disponibilidade e integridade do diário (pré-condição 2) --------------------
        switch (input.LookupState)
        {
            case JournalGateLookupState.Ambiguous:
                return Blocked(
                    JournalGateDiagnosticCode.JournalUnavailable,
                    JournalGateReasonCodes.JournalDuplicate,
                    JournalGatePrecondition.JournalAvailable,
                    input,
                    input.LookupDetail);

            case JournalGateLookupState.Unreadable:
                return Blocked(
                    JournalGateDiagnosticCode.JournalUnavailable,
                    JournalGateReasonCodes.JournalInvalid,
                    JournalGatePrecondition.JournalAvailable,
                    input,
                    input.LookupDetail);

            case JournalGateLookupState.ExternalCollision:
                // Um File com o nome do diário e Description alheia é divergência de
                // identidade, não conteúdo inválido: o objeto encontrado não é o diário.
                return Blocked(
                    JournalGateDiagnosticCode.JournalUnavailable,
                    JournalGateReasonCodes.JournalIdentityDivergent,
                    JournalGatePrecondition.JournalIdentityConfirmed,
                    input,
                    input.LookupDetail);

            case JournalGateLookupState.Absent:
                // Ausência não é falta: numa KB que nunca gerou nada, o diário ainda não
                // existe e será criado por esta operação. `JournalMissing` fica reservado a
                // quem exige um envelope preexistente — a continuação, que é P5/P6.
                return ApiPlanOperationJournalGateDecision.Allowed();
        }

        var current = input.CurrentEnvelope;
        if (current is null)
        {
            // Found sem envelope é contradição do chamador, não estado da KB.
            throw new ArgumentException(
                "LookupState=Found exige o envelope lido em CurrentEnvelope.",
                nameof(input));
        }

        // --- 2. Identidade do envelope encontrado (pré-condição 3) --------------------------
        // schemaVersion e journalKind já foram validados na leitura; o que ninguém conferia
        // é se o envelope pertence à KB aberta. Um diário copiado entre KBs passava.
        if (input.KnowledgeBaseGuid != Guid.Empty
            && current.KnowledgeBaseGuid != Guid.Empty
            && current.KnowledgeBaseGuid != input.KnowledgeBaseGuid)
        {
            return Blocked(
                JournalGateDiagnosticCode.JournalUnavailable,
                JournalGateReasonCodes.JournalIdentityDivergent,
                JournalGatePrecondition.JournalIdentityConfirmed,
                input,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "O diário encontrado pertence à KB '{0}', e a KB aberta é '{1}'. Um diário de outra KB não governa esta.",
                    current.KnowledgeBaseGuid,
                    input.KnowledgeBaseGuid));
        }

        // --- 3. Durabilidade do snapshot corrente (pré-condição 3) --------------------------
        if (input.ObservedDurability != JournalDurability.Confirmed)
        {
            return Blocked(
                JournalGateDiagnosticCode.DurabilityUnknown,
                JournalGateReasonCodes.JournalReloadDivergent,
                JournalGatePrecondition.JournalIdentityConfirmed,
                input,
                "A durabilidade do diário atual não pôde ser confirmada; a operação anterior precisa ser reconciliada antes de uma nova.");
        }

        // --- 4. Estado global do envelope corrente ------------------------------------------
        if (IsTerminal(current))
        {
            return ApiPlanOperationJournalGateDecision.Allowed();
        }

        // `Prepared`/`Pending` é o único não terminal que admite continuação explícita
        // (seção 4.1.1). Ele tem reason própria porque a saída é diferente: continuar com os
        // mesmos identificadores, em vez de reconciliar uma operação que já mutou a KB.
        if (current.EnvelopePhase == JournalEnvelopePhase.Prepared
            && current.OperationState == JournalOperationState.Pending)
        {
            var authorization = input.PreparedContinuationAuthorization;
            if (authorization is null)
            {
                return Blocked(
                    JournalGateDiagnosticCode.GateBlocked,
                    JournalGateReasonCodes.PreparedContinuationNotAuthorized,
                    JournalGatePrecondition.NoActiveIntent,
                    input,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "O diário da KB registra a operação {0} preparada e ainda não iniciada. Continuá-la ou abandoná-la exige autorização explícita.",
                        current.OperationKind));
            }

            if (authorization.OperationId != current.OperationId)
            {
                // Autorizou-se a continuação de outra operação: entre a autorização e agora,
                // o envelope corrente mudou. Prosseguir usaria um consentimento que não é
                // sobre este estado.
                return Blocked(
                    JournalGateDiagnosticCode.GateBlocked,
                    JournalGateReasonCodes.RecoveryAuthorizationStale,
                    JournalGatePrecondition.NoActiveIntent,
                    input,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "A autorização de continuação é da operação '{0}', e o diário registra '{1}'.",
                        authorization.OperationId,
                        current.OperationId));
            }

            return ApiPlanOperationJournalGateDecision.ContinuationAuthorized(current);
        }

        // Indeterminação tem reason própria: a saída não é «continuar», é consultar a KB por
        // identidade e reconciliar (pré-condição 5).
        if (current.OperationState == JournalOperationState.OutcomeUnknown)
        {
            return Blocked(
                JournalGateDiagnosticCode.GateBlocked,
                JournalGateReasonCodes.UnreconciledOutcome,
                JournalGatePrecondition.NoUnreconciledOutcome,
                input,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "O diário da KB registra a operação {0} em {1}/{2}: o resultado da última gravação não é conhecido e precisa ser reconciliado por identidade antes de outra operação.",
                    current.OperationKind,
                    current.OperationState,
                    current.LogicalStage));
        }

        // Partial (pré-condição 1) e Active/Running (pré-condição 4) caem aqui.
        var precondition = current.OperationState == JournalOperationState.Partial
            ? JournalGatePrecondition.PriorIntentReconciled
            : JournalGatePrecondition.NoActiveIntent;

        return Blocked(
            JournalGateDiagnosticCode.GateBlocked,
            JournalGateReasonCodes.JournalNonTerminal,
            precondition,
            input,
            string.Format(
                CultureInfo.InvariantCulture,
                "O diário da KB registra a operação {0} em estado {1}/{2}, que não é terminal. Reconcilie ou continue essa operação antes de iniciar outra.",
                current.OperationKind,
                current.OperationState,
                current.LogicalStage));
    }

    /// <summary>
    /// Diagnóstico de uma gravação do próprio diário que não pôde ser confirmada. Fica aqui,
    /// e não no store, para que a taxonomia inteira tenha um único dono.
    /// </summary>
    public static ApiPlanOperationJournalGateDiagnostic SaveUnconfirmed(
        int journalFileId,
        string detail)
    {
        var context = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("journalFileId", journalFileId.ToString(CultureInfo.InvariantCulture)),
            new KeyValuePair<string, string>("journalDurability", JournalDurability.Unknown.ToString()),
        };

        return new ApiPlanOperationJournalGateDiagnostic(
            JournalGateDiagnosticCode.DurabilityUnknown,
            JournalGateReasonCodes.JournalSaveUnconfirmed,
            JournalGatePrecondition.JournalIdentityConfirmed,
            detail ?? string.Empty,
            context);
    }

    /// <summary>
    /// Diagnóstico para quando a continuação foi autorizada mas o serviço que a executa ainda
    /// não existe. É subcausa de <c>GateBlocked</c>: o diário está legível e durável, e o que
    /// falta é o mecanismo, não o consentimento. Deixa de ser necessário quando a P5/P6
    /// entregar a continuação.
    /// </summary>
    public static ApiPlanOperationJournalGateDiagnostic ContinuationServiceUnavailable(ApiPlanOperationJournal current)
    {
        var context = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("operationId", current.OperationId.ToString("D", CultureInfo.InvariantCulture)),
            new KeyValuePair<string, string>("operationKind", current.OperationKind.ToString()),
            new KeyValuePair<string, string>("envelopePhase", current.EnvelopePhase.ToString()),
            new KeyValuePair<string, string>("operationState", current.OperationState.ToString()),
            new KeyValuePair<string, string>("logicalStage", current.LogicalStage.ToString()),
        };

        return new ApiPlanOperationJournalGateDiagnostic(
            JournalGateDiagnosticCode.GateBlocked,
            JournalGateReasonCodes.RecoveryAuthorizationLockUnavailable,
            JournalGatePrecondition.NoActiveIntent,
            "A continuação do envelope preparado foi autorizada, mas o serviço que a executa ainda não existe.",
            context);
    }

    private static bool IsTerminal(ApiPlanOperationJournal journal) =>
        journal.OperationState == JournalOperationState.Completed
        || journal.OperationState == JournalOperationState.Removed;

    private static ApiPlanOperationJournalGateDecision Blocked(
        JournalGateDiagnosticCode code,
        string reasonCode,
        JournalGatePrecondition precondition,
        ApiPlanOperationJournalGateInput input,
        string message)
    {
        return ApiPlanOperationJournalGateDecision.Blocked(
            new ApiPlanOperationJournalGateDiagnostic(
                code,
                reasonCode,
                precondition,
                message,
                BuildContext(input)),
            input.CurrentEnvelope);
    }

    /// <summary>
    /// Contexto estruturado do diagnóstico: o que a decisão 24 manda carregar ao lado da
    /// mensagem. Campos sem valor são omitidos em vez de virarem string vazia.
    /// </summary>
    private static IReadOnlyList<KeyValuePair<string, string>> BuildContext(ApiPlanOperationJournalGateInput input)
    {
        var context = new List<KeyValuePair<string, string>>();
        void Add(string key, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                context.Add(new KeyValuePair<string, string>(key, value!));
            }
        }

        Add("lookupState", input.LookupState.ToString());
        if (input.JournalFileId > 0)
        {
            Add("journalFileId", input.JournalFileId.ToString(CultureInfo.InvariantCulture));
        }

        var current = input.CurrentEnvelope;
        if (current is not null)
        {
            Add("operationId", current.OperationId.ToString("D", CultureInfo.InvariantCulture));
            Add("operationKind", current.OperationKind.ToString());
            Add("envelopePhase", current.EnvelopePhase.ToString());
            Add("operationState", current.OperationState.ToString());
            Add("logicalStage", current.LogicalStage.ToString());
            Add("blockReason", current.BlockReason?.ToString());
        }

        Add("journalDurability", input.ObservedDurability.ToString());
        return context;
    }
}

/// <summary>Resultado neutro da busca pelo File do diário, sem dependência do SDK.</summary>
public enum JournalGateLookupState
{
    Absent = 0,
    Found = 1,
    Ambiguous = 2,
    ExternalCollision = 3,
    Unreadable = 4,
}

/// <summary>
/// Códigos de alto nível do <c>GateDiagnostic</c>, na ordem de precedência da decisão 24.
/// Nenhum deles é <c>operationState</c>, <c>logicalStage</c> ou <c>blockReason</c>: eles
/// existem somente no relatório.
/// </summary>
public enum JournalGateDiagnosticCode
{
    JournalUnavailable = 0,
    DurabilityUnknown = 1,
    GateBlocked = 2,
    PreconditionFailed = 3,
}

/// <summary>As cinco validações da seção 4.2, na ordem em que o plano as enumera.</summary>
public enum JournalGatePrecondition
{
    None = 0,

    /// <summary>1 — ausência de intenção anterior parcial, indeterminada ou ambígua.</summary>
    PriorIntentReconciled = 1,

    /// <summary>2 — disponibilidade e integridade do diário único da KB.</summary>
    JournalAvailable = 2,

    /// <summary>3 — identidade, versão e durabilidade confirmadas, sem divergência física.</summary>
    JournalIdentityConfirmed = 3,

    /// <summary>4 — ausência de intenção ativa, ou transição autorizada de `Prepared`.</summary>
    NoActiveIntent = 4,

    /// <summary>5 — ausência de diagnóstico irremediado e de `OutcomeUnknown` não reconciliado.</summary>
    NoUnreconciledOutcome = 5,
}

/// <summary>
/// Códigos estáveis, destinados a máquinas. São contrato: mudar um texto aqui quebra quem
/// classifica diagnóstico por ele.
/// </summary>
public static class JournalGateReasonCodes
{
    public const string JournalMissing = "JournalMissing";
    public const string JournalInvalid = "JournalInvalid";
    public const string JournalDuplicate = "JournalDuplicate";
    public const string JournalIdentityDivergent = "JournalIdentityDivergent";
    public const string JournalSaveUnconfirmed = "JournalSaveUnconfirmed";
    public const string JournalReloadDivergent = "JournalReloadDivergent";
    public const string JournalNonTerminal = "JournalNonTerminal";
    public const string PreparedContinuationNotAuthorized = "PreparedContinuationNotAuthorized";
    public const string UnreconciledOutcome = "UnreconciledOutcome";
    public const string RecoveryAuthorizationStale = "RecoveryAuthorizationStale";
    public const string RecoveryAuthorizationLockUnavailable = "RecoveryAuthorizationLockUnavailable";
    public const string IdentityAmbiguous = "IdentityAmbiguous";
    public const string IdentityDivergent = "IdentityDivergent";
    public const string InventoryInsufficient = "InventoryInsufficient";
    public const string AuthorizationMismatch = "AuthorizationMismatch";

    /// <summary>
    /// A qual código de alto nível cada razão pertence. Serve ao gate mecânico: é aqui que se
    /// prova que <c>PreconditionFailed</c> não virou subcausa de <c>GateBlocked</c>.
    /// </summary>
    public static JournalGateDiagnosticCode OwnerOf(string reasonCode)
    {
        switch (reasonCode)
        {
            case JournalMissing:
            case JournalInvalid:
            case JournalDuplicate:
            case JournalIdentityDivergent:
                return JournalGateDiagnosticCode.JournalUnavailable;

            case JournalSaveUnconfirmed:
            case JournalReloadDivergent:
                return JournalGateDiagnosticCode.DurabilityUnknown;

            case JournalNonTerminal:
            case PreparedContinuationNotAuthorized:
            case UnreconciledOutcome:
            case RecoveryAuthorizationStale:
            case RecoveryAuthorizationLockUnavailable:
                return JournalGateDiagnosticCode.GateBlocked;

            case IdentityAmbiguous:
            case IdentityDivergent:
            case InventoryInsufficient:
            case AuthorizationMismatch:
                return JournalGateDiagnosticCode.PreconditionFailed;

            default:
                throw new ArgumentOutOfRangeException(nameof(reasonCode), reasonCode, "reasonCode fora do mapeamento fechado da decisão 24.");
        }
    }
}

/// <summary>
/// Autorização humana para continuar um envelope <c>Prepared</c>. O tipo existe desde a P3
/// para que a taxonomia esteja fechada; quem o preenche é a P6, e **nenhum chamador atual
/// passa um valor**.
/// </summary>
public sealed class ApiPlanOperationJournalContinuationAuthorization
{
    public ApiPlanOperationJournalContinuationAuthorization(Guid operationId, string authorizedBy)
    {
        OperationId = operationId;
        AuthorizedBy = authorizedBy ?? string.Empty;
    }

    /// <summary>Operação a que o consentimento se refere. Outra operação invalida a autorização.</summary>
    public Guid OperationId { get; }

    public string AuthorizedBy { get; }
}

public sealed class ApiPlanOperationJournalGateInput
{
    public JournalGateLookupState LookupState { get; set; }

    public string LookupDetail { get; set; } = string.Empty;

    /// <summary>Envelope lido do File, obrigatório quando <see cref="LookupState"/> é <c>Found</c>.</summary>
    public ApiPlanOperationJournal? CurrentEnvelope { get; set; }

    /// <summary>Durabilidade observada do snapshot corrente, não do que esta operação pretende gravar.</summary>
    public JournalDurability ObservedDurability { get; set; } = JournalDurability.Confirmed;

    /// <summary>KB aberta. <c>Guid.Empty</c> dispensa a comparação de identidade.</summary>
    public Guid KnowledgeBaseGuid { get; set; }

    public int JournalFileId { get; set; }

    public ApiPlanOperationJournalContinuationAuthorization? PreparedContinuationAuthorization { get; set; }
}

public enum JournalGateOutcome
{
    /// <summary>Uma operação nova pode começar.</summary>
    Allowed = 0,

    /// <summary>
    /// Há um envelope <c>Prepared</c> e a continuação foi autorizada para ele. Quem continua
    /// preserva <c>operationId</c> e <c>applicationId</c> — serviço da P5/P6.
    /// </summary>
    ContinuationAuthorized = 1,

    Blocked = 2,
}

public sealed class ApiPlanOperationJournalGateDecision
{
    private ApiPlanOperationJournalGateDecision(
        JournalGateOutcome outcome,
        ApiPlanOperationJournalGateDiagnostic? diagnostic,
        ApiPlanOperationJournal? currentEnvelope)
    {
        Outcome = outcome;
        Diagnostic = diagnostic;
        CurrentEnvelope = currentEnvelope;
    }

    public JournalGateOutcome Outcome { get; }

    /// <summary>Preenchido somente quando <see cref="Outcome"/> é <c>Blocked</c>.</summary>
    public ApiPlanOperationJournalGateDiagnostic? Diagnostic { get; }

    public ApiPlanOperationJournal? CurrentEnvelope { get; }

    public bool CanStart => Outcome == JournalGateOutcome.Allowed;

    internal static ApiPlanOperationJournalGateDecision Allowed() =>
        new ApiPlanOperationJournalGateDecision(JournalGateOutcome.Allowed, null, null);

    internal static ApiPlanOperationJournalGateDecision ContinuationAuthorized(ApiPlanOperationJournal current) =>
        new ApiPlanOperationJournalGateDecision(JournalGateOutcome.ContinuationAuthorized, null, current);

    internal static ApiPlanOperationJournalGateDecision Blocked(
        ApiPlanOperationJournalGateDiagnostic diagnostic,
        ApiPlanOperationJournal? currentEnvelope) =>
        new ApiPlanOperationJournalGateDecision(JournalGateOutcome.Blocked, diagnostic, currentEnvelope);
}

/// <summary>
/// O diagnóstico como a decisão 24 o define: código de alto nível, razão estável, pré-condição
/// que falhou, mensagem humana e contexto estruturado. Efêmero — nada disto é gravado no
/// envelope.
/// </summary>
public sealed class ApiPlanOperationJournalGateDiagnostic
{
    internal ApiPlanOperationJournalGateDiagnostic(
        JournalGateDiagnosticCode code,
        string reasonCode,
        JournalGatePrecondition failedPrecondition,
        string message,
        IReadOnlyList<KeyValuePair<string, string>> context)
    {
        Code = code;
        ReasonCode = reasonCode;
        FailedPrecondition = failedPrecondition;
        Message = message;
        Context = context;
    }

    public JournalGateDiagnosticCode Code { get; }

    public string ReasonCode { get; }

    public JournalGatePrecondition FailedPrecondition { get; }

    public string Message { get; }

    public IReadOnlyList<KeyValuePair<string, string>> Context { get; }

    /// <summary>Linha única para a Output, com o contexto em forma legível.</summary>
    public string Describe()
    {
        var context = Context.Count == 0
            ? string.Empty
            : " Contexto: " + string.Join(", ", Context.Select(pair => pair.Key + "=" + pair.Value)) + ".";

        return string.Format(
            CultureInfo.InvariantCulture,
            "[{0}/{1}] Pré-condição '{2}'. {3}{4}",
            Code,
            ReasonCode,
            FailedPrecondition,
            Message,
            context);
    }
}
