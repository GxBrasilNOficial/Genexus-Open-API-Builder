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
    /// Reabre uma remoção interrompida para a passada seguinte, preservando <c>operationId</c>,
    /// <c>applicationId</c> e o inventário. Não é uma operação nova: é a mesma, continuada.
    ///
    /// Só sai daqui o que parou por orçamento esgotado ou por falha de etapa. Ausência antes do
    /// Delete e resultado indeterminado não voltam para a fila sozinhos — são exatamente os
    /// casos em que ninguém sabe o que aconteceu, e repetir seria inventar uma certeza.
    /// </summary>
    public static void ResumeRemoval(ApiPlanOperationJournal journal, DateTime utcNow)
    {
        Require(journal);
        if (journal.OperationKind != JournalOperationKind.Remove)
        {
            throw new InvalidOperationException(
                "A retomada de passadas pertence ao Remove, não a " + journal.OperationKind + ".");
        }

        if (journal.EnvelopePhase != JournalEnvelopePhase.Active
            || journal.OperationState != JournalOperationState.Partial)
        {
            throw new InvalidOperationException(
                "Só uma remoção Active/Partial pode ser retomada. Estado atual: " + Describe(journal) + ".");
        }

        if (journal.BlockReason != JournalBlockReason.RetryBudgetExhausted
            && journal.BlockReason != JournalBlockReason.StageFailed
            && journal.BlockReason != JournalBlockReason.UserAborted)
        {
            throw new InvalidOperationException(
                "A retomada não cobre o motivo registrado: " + journal.BlockReason + ".");
        }

        journal.OperationState = JournalOperationState.Running;
        journal.LogicalStage = JournalLogicalStage.RemovalInProgress;
        journal.BlockReason = null;
        Touch(journal, utcNow, "retomada de remoção");
    }

    /// <summary>
    /// Reconciliação de uma remoção interrompida cujo inventário está **confirmadamente
    /// ausente** da KB. Não é um atalho para fechar o que ficou pela metade: só o executor de
    /// recuperação chama isto, e só depois de reler cada alvo previsto por identidade.
    ///
    /// A distinção importa: <see cref="Complete"/> encerra uma operação que chegou ao fim
    /// sozinha; esta fecha o registro de uma que chegou ao fim sem conseguir dizê-lo.
    /// </summary>
    public static void ReconcileRemoved(ApiPlanOperationJournal journal, DateTime utcNow)
    {
        Require(journal);
        if (journal.OperationKind != JournalOperationKind.Remove)
        {
            throw new InvalidOperationException(
                "A reconciliação para Removed pertence ao Remove, não a " + journal.OperationKind + ".");
        }

        if (journal.EnvelopePhase != JournalEnvelopePhase.Active)
        {
            throw new InvalidOperationException(
                "Só um envelope Active pode ser reconciliado como removido. Estado atual: " + Describe(journal) + ".");
        }

        if (journal.OperationState != JournalOperationState.Partial
            && journal.OperationState != JournalOperationState.Running)
        {
            throw new InvalidOperationException(
                "A reconciliação para Removed parte de Partial ou Running, não de " + journal.OperationState + ".");
        }

        journal.OperationState = JournalOperationState.Removed;
        journal.LogicalStage = JournalLogicalStage.Removed;
        journal.BlockReason = null;
        Touch(journal, utcNow, "reconciliação para Removed");
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
    /// Encerra o registro de uma operação que gravou alguma coisa e parou no meio, por decisão
    /// humana informada. O envelope vira terminal e libera a KB; o inventário, os recibos e a
    /// disposição de quem encerrou ficam preservados.
    ///
    /// O que isto **não** faz: apagar objeto nenhum, e afirmar que a operação concluiu. A KB
    /// continua exatamente como ficou, e é por isso que a confirmação precisa mostrar o
    /// inventário antes de perguntar.
    ///
    /// <c>OutcomeUnknown</c> não entra: ali não se sabe se a última gravação aconteceu, e
    /// encerrar transformaria dúvida em certeza. Esse caso continua exigindo consulta por
    /// identidade antes de qualquer decisão.
    /// </summary>
    public static void DiscardInterrupted(
        ApiPlanOperationJournal journal,
        string reason,
        string authorizedBy,
        DateTime utcNow)
    {
        Require(journal);
        if (journal.EnvelopePhase != JournalEnvelopePhase.Active)
        {
            throw new InvalidOperationException(
                "Só um envelope Active pode ter o registro encerrado; um Prepared é abandonado. Estado atual: "
                + Describe(journal) + ".");
        }

        if (journal.OperationState != JournalOperationState.Partial
            && journal.OperationState != JournalOperationState.Running)
        {
            throw new InvalidOperationException(
                "O encerramento do registro parte de Partial ou Running, não de " + journal.OperationState + ".");
        }

        if (journal.JournalDurability != JournalDurability.Confirmed)
        {
            throw new InvalidOperationException(
                "Encerrar um registro cuja durabilidade não foi confirmada esconderia justamente o que não se sabe.");
        }

        journal.Abandonment = new ApiPlanOperationJournalAbandonment
        {
            Reason = reason ?? string.Empty,
            AuthorizedUtc = utcNow,
            AuthorizedBy = authorizedBy ?? string.Empty,
        };
        journal.OperationState = JournalOperationState.Completed;
        journal.LogicalStage = JournalLogicalStage.Discarded;
        journal.BlockReason = null;
        Touch(journal, utcNow, "encerramento do registro");
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
        // A regra tem um dono só: o gate estendido da P3. Esta sobrecarga continua existindo
        // como leitura simplificada — «pode ou não pode» —, mas delega, para que as duas não
        // divirjam com o tempo.
        var decision = ApiPlanOperationJournalGate.Evaluate(new ApiPlanOperationJournalGateInput
        {
            LookupState = current is null ? JournalGateLookupState.Absent : JournalGateLookupState.Found,
            CurrentEnvelope = current,
            ObservedDurability = observedDurability,
        });

        if (decision.CanStart)
        {
            return ApiPlanOperationJournalReuse.Allowed();
        }

        return ApiPlanOperationJournalReuse.Blocked(
            decision.Diagnostic?.Message
            ?? "A continuação do envelope preparado ainda não tem serviço que a execute.");
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
