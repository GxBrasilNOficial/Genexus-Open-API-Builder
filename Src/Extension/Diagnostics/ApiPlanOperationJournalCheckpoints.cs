#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// S-B111 / F3 — máquina de checkpoints do diário, sem SDK.
///
/// Ela decide **para onde** o envelope vai a cada fronteira e recusa transições que o
/// contrato não prevê; quem grava é o store, e quem conta as gravações é a sessão. A
/// separação existe para que a matriz de checkpoints da seção 4.4 do plano da F3 seja
/// exercitada offline, sem KB.
///
/// A política é fixa por operação e não pode ser reduzida para economizar I/O:
///
/// | Operação | CP1 | CP2 | CP3 | CP4 | Total físico |
/// |---|---|---|---|---|---|
/// | Apply / Sync | `Prepared` + `Pending/IntentionRecorded` | `Active` + `Running/NotStarted` | `Running/ApiPhysicallySaved` | terminal | 4 |
/// | Remove | `Prepared` + `Pending/IntentionRecorded` | `Active` + `Running/RemovalInProgress` | uma por passada | terminal | 3 + P |
/// | Recovery autônomo | `Prepared` + `Pending/IntentionRecorded` | `Active` + `Running/MetadataPending` | `Running/MetadataRecovered` | terminal | 4 |
///
/// Toda transição valida o envelope inteiro. Um envelope inválido em memória é erro de
/// programação da extensão, não estado que se grave e se diagnostique depois.
/// </summary>
public static class ApiPlanOperationJournalCheckpoints
{
    /// <summary>
    /// CP1 — a intenção, antes de qualquer gravação de negócio. O envelope nasce
    /// <c>Prepared</c>/<c>Pending</c>: ele ainda não autoriza nada, apenas declara o que se
    /// pretende fazer.
    /// </summary>
    public static ApiPlanOperationJournal CreatePrepared(
        JournalOperationKind operationKind,
        ApiPlanOperationJournalPlan plan,
        Guid knowledgeBaseGuid,
        Guid transactionGuid,
        string transactionName,
        Guid operationId,
        Guid applicationId,
        string generatorVersion,
        JournalIntentKind intentKind,
        string? metadataSchemaVersion,
        DateTime utcNow,
        IEnumerable<ApiPlanOperationJournalInventoryItem>? inventory = null)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        var journal = new ApiPlanOperationJournal
        {
            KnowledgeBaseGuid = knowledgeBaseGuid,
            TransactionGuid = transactionGuid,
            TransactionName = transactionName ?? string.Empty,
            OperationId = operationId,
            ApplicationId = applicationId,
            OperationKind = operationKind,
            GeneratorVersion = generatorVersion ?? string.Empty,
            CreatedUtc = utcNow,
            UpdatedUtc = utcNow,
            EnvelopePhase = JournalEnvelopePhase.Prepared,
            OperationState = JournalOperationState.Pending,
            LogicalStage = JournalLogicalStage.IntentionRecorded,
            JournalDurability = JournalDurability.Confirmed,
            IntentKind = intentKind,
            MetadataSchemaVersion = metadataSchemaVersion,
            Plan = plan,
        };

        // O Remove registra a intenção já com o conjunto completo de alvos validados: é esse
        // inventário que permite dizer, depois de uma interrupção, o que foi previsto e o
        // que chegou a sair da KB.
        if (inventory is not null)
        {
            foreach (var item in inventory)
            {
                journal.Inventory.Add(item);
            }
        }

        Validate(journal, "CP1 Prepared");
        return journal;
    }

    /// <summary>
    /// CP2 — promoção a <c>Active</c>. Só depois dela o pipeline pode gravar o primeiro
    /// objeto de negócio.
    /// </summary>
    public static void PromoteToActive(ApiPlanOperationJournal journal, DateTime utcNow)
    {
        Require(journal);
        if (journal.EnvelopePhase != JournalEnvelopePhase.Prepared
            || journal.OperationState != JournalOperationState.Pending)
        {
            throw new InvalidOperationException(
                "Só um envelope Prepared/Pending pode ser promovido a Active. Estado atual: "
                + Describe(journal) + ".");
        }

        journal.EnvelopePhase = JournalEnvelopePhase.Active;
        journal.OperationState = JournalOperationState.Running;
        journal.LogicalStage = journal.OperationKind switch
        {
            JournalOperationKind.Remove => JournalLogicalStage.RemovalInProgress,
            JournalOperationKind.Recovery => JournalLogicalStage.MetadataPending,
            _ => JournalLogicalStage.NotStarted,
        };
        Touch(journal, utcNow, "CP2 Active");
    }

    /// <summary>
    /// CP3 de Apply e Sync — o API Object está fisicamente confirmado. É a fronteira que
    /// muda a capacidade de continuar: a partir daqui, nenhuma recuperação pode repetir a
    /// persistência do API.
    /// </summary>
    public static void NoteApiPhysicallySaved(ApiPlanOperationJournal journal, DateTime utcNow)
    {
        Require(journal);
        RequireActiveRunning(journal, "registrar o API Object confirmado");
        if (journal.OperationKind != JournalOperationKind.Apply && journal.OperationKind != JournalOperationKind.Sync)
        {
            throw new InvalidOperationException(
                "ApiPhysicallySaved pertence a Apply e Sync, não a " + journal.OperationKind + ".");
        }

        journal.LogicalStage = JournalLogicalStage.ApiPhysicallySaved;
        Touch(journal, utcNow, "CP3 ApiPhysicallySaved");
    }

    /// <summary>
    /// Resultado indeterminado do `Save()` do API: nunca «não gravou». O estágio registra a
    /// indeterminação e a operação termina bloqueada para consulta por identidade.
    /// </summary>
    public static void NoteApiSaveOutcomeUnknown(ApiPlanOperationJournal journal, DateTime utcNow)
    {
        Require(journal);
        RequireActiveRunning(journal, "registrar resultado indeterminado do API Object");
        journal.LogicalStage = JournalLogicalStage.ApiSaveOutcomeUnknown;
        Touch(journal, utcNow, "ApiSaveOutcomeUnknown");
    }

    /// <summary>CP3 do Recovery autônomo de B115 — metadata reconstruída e confirmada.</summary>
    public static void NoteMetadataRecovered(ApiPlanOperationJournal journal, DateTime utcNow)
    {
        Require(journal);
        RequireActiveRunning(journal, "registrar metadata recuperada");
        if (journal.OperationKind != JournalOperationKind.Recovery)
        {
            throw new InvalidOperationException(
                "MetadataRecovered pertence ao Recovery autônomo, não a " + journal.OperationKind + ".");
        }

        journal.LogicalStage = JournalLogicalStage.MetadataRecovered;
        Touch(journal, utcNow, "CP3 MetadataRecovered");
    }

    /// <summary>
    /// CP3 do Remove — fim de uma passada da fila. O inventário atualizado é o que permite
    /// à passada seguinte saber o que ainda está presente.
    /// </summary>
    public static void NoteRemovalPassCompleted(ApiPlanOperationJournal journal, DateTime utcNow)
    {
        Require(journal);
        RequireActiveRunning(journal, "registrar uma passada de remoção");
        if (journal.OperationKind != JournalOperationKind.Remove)
        {
            throw new InvalidOperationException(
                "Passadas de remoção pertencem ao Remove, não a " + journal.OperationKind + ".");
        }

        journal.LogicalStage = JournalLogicalStage.RemovalInProgress;
        Touch(journal, utcNow, "CP3 passada de remoção");
    }

    /// <summary>
    /// CP4 de sucesso. `Remove` termina em <c>Removed</c>; as demais operações, em
    /// <c>Completed</c>.
    /// </summary>
    public static void Complete(ApiPlanOperationJournal journal, DateTime utcNow)
    {
        Require(journal);
        RequireActiveRunning(journal, "concluir a operação");
        if (journal.OperationKind == JournalOperationKind.Remove)
        {
            journal.OperationState = JournalOperationState.Removed;
            journal.LogicalStage = JournalLogicalStage.Removed;
        }
        else
        {
            journal.OperationState = JournalOperationState.Completed;
            journal.LogicalStage = JournalLogicalStage.Completed;
        }

        journal.BlockReason = null;
        Touch(journal, utcNow, "CP4 terminal");
    }

    /// <summary>
    /// CP4 de interrupção. O motivo é obrigatório e vem do enum fechado do envelope, nunca
    /// do texto de um diagnóstico efêmero.
    /// </summary>
    public static void Interrupt(
        ApiPlanOperationJournal journal,
        JournalOperationState state,
        JournalBlockReason blockReason,
        DateTime utcNow)
    {
        Require(journal);
        if (state != JournalOperationState.Partial && state != JournalOperationState.OutcomeUnknown)
        {
            throw new InvalidOperationException(
                "Uma interrupção termina em Partial ou OutcomeUnknown, não em " + state + ".");
        }

        RequireActiveRunning(journal, "interromper a operação");
        journal.OperationState = state;
        if (journal.OperationKind == JournalOperationKind.Remove && state == JournalOperationState.Partial)
        {
            journal.LogicalStage = JournalLogicalStage.RemovalPartial;
        }

        journal.BlockReason = blockReason;
        Touch(journal, utcNow, "CP4 interrupção");
    }

    /// <summary>
    /// Abandono explícito de um envelope que nunca gravou nada. Não apaga o File nem cria um
    /// estado novo: registra a disposição e libera a KB para uma operação seguinte.
    /// </summary>
    public static void Abandon(
        ApiPlanOperationJournal journal,
        string reason,
        string authorizedBy,
        DateTime utcNow)
    {
        Require(journal);
        if (journal.EnvelopePhase != JournalEnvelopePhase.Prepared
            || journal.OperationState != JournalOperationState.Pending)
        {
            throw new InvalidOperationException(
                "Só um envelope Prepared/Pending pode ser abandonado. Estado atual: " + Describe(journal) + ".");
        }

        journal.Abandonment = new ApiPlanOperationJournalAbandonment
        {
            Reason = reason ?? string.Empty,
            AuthorizedUtc = utcNow,
            AuthorizedBy = authorizedBy ?? string.Empty,
        };
        journal.OperationState = JournalOperationState.Completed;
        journal.LogicalStage = JournalLogicalStage.Abandoned;
        journal.JournalDurability = JournalDurability.Confirmed;
        Touch(journal, utcNow, "abandono");
    }

    /// <summary>
    /// Decide se o envelope corrente pode ser substituído por uma operação nova. Só um
    /// estado terminal com durabilidade confirmada libera; qualquer outro bloqueia e exige
    /// decisão explícita, porque sobrescrever um envelope não terminal apagaria a única
    /// prova do que ficou pela metade.
    /// </summary>
    public static ApiPlanOperationJournalReuse EvaluateReuse(
        ApiPlanOperationJournal? current,
        JournalDurability observedDurability)
    {
        if (current is null)
        {
            return ApiPlanOperationJournalReuse.Allowed();
        }

        if (observedDurability != JournalDurability.Confirmed)
        {
            return ApiPlanOperationJournalReuse.Blocked(
                "A durabilidade do diário atual não pôde ser confirmada; a operação anterior precisa ser reconciliada antes de uma nova.");
        }

        if (current.OperationState != JournalOperationState.Completed
            && current.OperationState != JournalOperationState.Removed)
        {
            return ApiPlanOperationJournalReuse.Blocked(string.Format(
                CultureInfo.InvariantCulture,
                "O diário da KB registra a operação {0} em estado {1}/{2}, que não é terminal. Reconcilie ou continue essa operação antes de iniciar outra.",
                current.OperationKind,
                current.OperationState,
                current.LogicalStage));
        }

        return ApiPlanOperationJournalReuse.Allowed();
    }

    /// <summary>
    /// Quantidade de gravações físicas do diário prevista para a operação, usada para
    /// conferir a política contra o que foi realmente gravado.
    /// </summary>
    public static int ExpectedPhysicalCheckpoints(JournalOperationKind operationKind, int removalPasses)
    {
        if (operationKind != JournalOperationKind.Remove)
        {
            return 4;
        }

        if (removalPasses < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(removalPasses));
        }

        return 3 + removalPasses;
    }

    private static void RequireActiveRunning(ApiPlanOperationJournal journal, string action)
    {
        if (journal.EnvelopePhase != JournalEnvelopePhase.Active
            || journal.OperationState != JournalOperationState.Running)
        {
            throw new InvalidOperationException(
                "Para " + action + " o envelope precisa estar Active/Running. Estado atual: " + Describe(journal) + ".");
        }
    }

    private static void Touch(ApiPlanOperationJournal journal, DateTime utcNow, string transition)
    {
        journal.UpdatedUtc = utcNow < journal.UpdatedUtc ? journal.UpdatedUtc : utcNow;
        Validate(journal, transition);
    }

    private static void Validate(ApiPlanOperationJournal journal, string transition)
    {
        var validation = ApiPlanOperationJournalValidator.Validate(journal);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                "Transição '" + transition + "' produziu um envelope inválido. " + validation.Describe());
        }
    }

    private static string Describe(ApiPlanOperationJournal journal) =>
        journal.EnvelopePhase + "/" + journal.OperationState + "/" + journal.LogicalStage;

    private static void Require(ApiPlanOperationJournal journal)
    {
        if (journal is null)
        {
            throw new ArgumentNullException(nameof(journal));
        }
    }
}

/// <summary>
/// Monta o bloco `plan` do envelope a partir do que a operação realmente pretende fazer.
/// Vive separado do <see cref="ApiPlanOperationJournalPlan"/> para que a construção continue
/// SDK-free e testável: o que entra aqui são valores, não objetos da KB.
/// </summary>
public static class ApiPlanOperationJournalPlans
{
    /// <summary>
    /// Plano de Apply e Sync. <paramref name="plannedApiGuid"/> é nulo numa criação nova,
    /// em que a identidade só nasce do `API.Create`, dentro do pipeline.
    /// </summary>
    public static ApiPlanOperationJournalPlan ForGeneration(
        Guid? plannedApiGuid,
        string contractHash,
        bool generateApiObject,
        bool generateSdts,
        bool generateProcedures,
        bool generateMetadata,
        IEnumerable<string> services)
    {
        var plan = new ApiPlanOperationJournalPlan
        {
            PlanKind = JournalPlanKind.Generation,
            PlannedApiGuid = plannedApiGuid.HasValue && plannedApiGuid.Value != Guid.Empty ? plannedApiGuid : null,
            ContractHash = contractHash,
            GenerateApiObject = generateApiObject,
            GenerateSdts = generateSdts,
            GenerateProcedures = generateProcedures,
            GenerateMetadata = generateMetadata,
        };

        AddServices(plan, services);
        return plan;
    }

    /// <summary>
    /// Plano de Remove. O inventário completo vive no envelope; aqui ficam a identidade da
    /// API e o contrato, que pode ser nulo quando a intenção foi importada de metadata
    /// legada.
    /// </summary>
    public static ApiPlanOperationJournalPlan ForRemoval(
        Guid? plannedApiGuid,
        string? contractHash,
        IEnumerable<string> services)
    {
        var plan = new ApiPlanOperationJournalPlan
        {
            PlanKind = JournalPlanKind.Removal,
            PlannedApiGuid = plannedApiGuid.HasValue && plannedApiGuid.Value != Guid.Empty ? plannedApiGuid : null,
            ContractHash = string.IsNullOrWhiteSpace(contractHash) ? null : contractHash,
        };

        AddServices(plan, services);
        return plan;
    }

    /// <summary>
    /// Plano do Recovery autônomo de B115: inventário, nunca contrato. Os blocos não
    /// recuperáveis ficam ausentes, e inventá-los faria o Sync comparar a API real contra
    /// uma descrição falsa.
    /// </summary>
    public static ApiPlanOperationJournalPlan ForMetadataRecovery(Guid? plannedApiGuid)
    {
        return new ApiPlanOperationJournalPlan
        {
            PlanKind = JournalPlanKind.MetadataRecovery,
            PlannedApiGuid = plannedApiGuid.HasValue && plannedApiGuid.Value != Guid.Empty ? plannedApiGuid : null,
        };
    }

    private static void AddServices(ApiPlanOperationJournalPlan plan, IEnumerable<string>? services)
    {
        if (services is null)
        {
            return;
        }

        foreach (var service in services)
        {
            if (!string.IsNullOrWhiteSpace(service))
            {
                plan.Services.Add(service);
            }
        }
    }
}

public sealed class ApiPlanOperationJournalReuse
{
    private ApiPlanOperationJournalReuse(bool canStart, string reason)
    {
        CanStart = canStart;
        Reason = reason;
    }

    public bool CanStart { get; }

    public string Reason { get; }

    internal static ApiPlanOperationJournalReuse Allowed() =>
        new ApiPlanOperationJournalReuse(true, string.Empty);

    internal static ApiPlanOperationJournalReuse Blocked(string reason) =>
        new ApiPlanOperationJournalReuse(false, reason);
}
