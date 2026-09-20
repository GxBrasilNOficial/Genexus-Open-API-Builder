#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// S-B111 / F3 — validação do schema V1 do diário.
///
/// O serializer chama esta validação antes de qualquer <c>File.Save()</c>: um envelope que
/// viole o contrato não pode chegar ao disco, porque um diário inválido é indistinguível de
/// um diário ausente na recuperação, e os dois bloqueiam a operação.
///
/// As regras abaixo são as da decisão 24 e das decisões 44 a 47. Onde a decisão fecha um
/// domínio, a validação fecha junto; onde ela é silenciosa, esta classe não inventa
/// restrição.
///
/// Num envelope reidratado pela leitura — e só nele — o marcador
/// <see cref="ApiPlanOperationJournal.AcceptsLegacyShapes"/> suaviza as regras de presença dos
/// campos novos do V1 (tempo de recibo e suficiência de inventário), para que a regravação de
/// um diário legado em recuperação não seja recusada. Envelopes construídos em memória
/// seguem o schema estrito.
/// </summary>
public static class ApiPlanOperationJournalValidator
{
    public static ApiPlanOperationJournalValidation Validate(ApiPlanOperationJournal journal)
    {
        if (journal is null)
        {
            throw new ArgumentNullException(nameof(journal));
        }

        var errors = new List<string>();
        ValidateIdentity(journal, errors);
        ValidateEnvelope(journal, errors);
        ValidatePlan(journal, errors);
        ValidateReceipts(journal, errors);
        ValidateInventory(journal, errors);
        ValidateMetadataSchemaVersion(journal, errors);
        return new ApiPlanOperationJournalValidation(errors);
    }

    private static void ValidateIdentity(ApiPlanOperationJournal journal, List<string> errors)
    {
        RequireGuid(journal.KnowledgeBaseGuid, "knowledgeBaseGuid", errors);
        RequireGuid(journal.TransactionGuid, "transactionGuid", errors);
        RequireGuid(journal.OperationId, "operationId", errors);
        RequireGuid(journal.ApplicationId, "applicationId", errors);
        RequireText(journal.TransactionName, "transactionName", errors);
        RequireText(journal.GeneratorVersion, "generatorVersion", errors);

        // operationId correlaciona a tentativa; applicationId é a identidade da aplicação
        // gravada também em ownership.applicationId. Colapsar os dois apagaria a distinção
        // entre "nova tentativa" e "mesma API".
        if (journal.OperationId != Guid.Empty && journal.OperationId == journal.ApplicationId)
        {
            errors.Add("operationId e applicationId devem ser distintos.");
        }

        if (journal.CreatedUtc == default)
        {
            errors.Add("createdUtc é obrigatório.");
        }

        if (journal.UpdatedUtc == default)
        {
            errors.Add("updatedUtc é obrigatório.");
        }

        if (journal.CreatedUtc != default && journal.UpdatedUtc != default && journal.UpdatedUtc < journal.CreatedUtc)
        {
            errors.Add("updatedUtc não pode ser anterior a createdUtc.");
        }
    }

    private static void ValidateEnvelope(ApiPlanOperationJournal journal, List<string> errors)
    {
        var abandoned = journal.LogicalStage == JournalLogicalStage.Abandoned;
        var discarded = journal.LogicalStage == JournalLogicalStage.Discarded;

        // Fases do envelope não substituem o estado global: Prepared acompanha Pending, e a
        // única saída de Prepared sem gravação de negócio é o abandono explícito.
        if (journal.EnvelopePhase == JournalEnvelopePhase.Prepared
            && journal.OperationState != JournalOperationState.Pending
            && !(abandoned && journal.OperationState == JournalOperationState.Completed))
        {
            errors.Add("envelopePhase=Prepared exige operationState=Pending, salvo o abandono explícito.");
        }

        if (journal.EnvelopePhase == JournalEnvelopePhase.Active
            && journal.OperationState == JournalOperationState.Pending)
        {
            errors.Add("envelopePhase=Active não admite operationState=Pending.");
        }

        if (journal.OperationState == JournalOperationState.Pending
            && journal.LogicalStage != JournalLogicalStage.NotStarted
            && journal.LogicalStage != JournalLogicalStage.IntentionRecorded)
        {
            errors.Add("operationState=Pending admite apenas logicalStage NotStarted ou IntentionRecorded.");
        }

        if (journal.OperationState == JournalOperationState.Completed
            && journal.LogicalStage != JournalLogicalStage.Completed
            && !abandoned
            && !discarded)
        {
            errors.Add("operationState=Completed exige logicalStage Completed, Abandoned ou Discarded.");
        }

        if (journal.OperationState == JournalOperationState.Removed)
        {
            if (journal.OperationKind != JournalOperationKind.Remove)
            {
                errors.Add("operationState=Removed pertence somente a operationKind=Remove.");
            }

            if (journal.LogicalStage != JournalLogicalStage.Removed)
            {
                errors.Add("operationState=Removed exige logicalStage=Removed.");
            }
        }

        // Partial é sempre estado global; durante remoção ele precisa vir acompanhado do
        // estágio que diz onde a fila parou.
        if (journal.OperationState == JournalOperationState.Partial
            && journal.OperationKind == JournalOperationKind.Remove
            && journal.LogicalStage != JournalLogicalStage.RemovalPartial)
        {
            errors.Add("operationState=Partial em Remove exige logicalStage=RemovalPartial.");
        }

        if (journal.OperationKind == JournalOperationKind.Recovery
            && (journal.OperationState == JournalOperationState.Removed
                || journal.OperationState == JournalOperationState.Partial))
        {
            errors.Add("operationKind=Recovery autônomo termina em Completed ou OutcomeUnknown.");
        }

        if (journal.OperationKind == JournalOperationKind.Recovery
            && journal.IntentKind != JournalIntentKind.Imported)
        {
            errors.Add("operationKind=Recovery exige intentKind=Imported.");
        }

        ValidateAbandonment(journal, errors, abandoned);
        ValidateDiscard(journal, errors, discarded);
        ValidateBlockReason(journal, errors);
    }

    private static void ValidateAbandonment(ApiPlanOperationJournal journal, List<string> errors, bool abandoned)
    {
        if (abandoned)
        {
            if (journal.Abandonment is null)
            {
                errors.Add("logicalStage=Abandoned exige o objeto abandonment.");
                return;
            }

            RequireText(journal.Abandonment.Reason, "abandonment.reason", errors);
            RequireText(journal.Abandonment.AuthorizedBy, "abandonment.authorizedBy", errors);
            if (journal.Abandonment.AuthorizedUtc == default)
            {
                errors.Add("abandonment.authorizedUtc é obrigatório.");
            }

            if (journal.OperationState != JournalOperationState.Completed)
            {
                errors.Add("o abandono mantém operationState=Completed.");
            }

            if (journal.EnvelopePhase != JournalEnvelopePhase.Prepared)
            {
                errors.Add("somente um envelope Prepared pode ser abandonado.");
            }

            if (journal.JournalDurability != JournalDurability.Confirmed)
            {
                errors.Add("o abandono exige journalDurability=Confirmed.");
            }

            // Disposição terminal sem gravação de negócio: Active, Partial e OutcomeUnknown
            // exigem reconciliação ou continuação, não abandono.
            if (journal.Receipts.Count > 0)
            {
                errors.Add("o abandono não admite recibos de gravação de negócio.");
            }

            return;
        }

        if (journal.Abandonment is not null && journal.LogicalStage != JournalLogicalStage.Discarded)
        {
            errors.Add("abandonment só é válido com logicalStage Abandoned ou Discarded.");
        }
    }

    /// <summary>
    /// O encerramento de um registro interrompido. Ao contrário do abandono, ele **admite**
    /// recibos de gravação: é exatamente o caso em que a operação tocou a KB e parou no meio.
    /// O que ele exige é a disposição registrada, o envelope ativo e a durabilidade confirmada —
    /// encerrar sobre um snapshot não confirmado esconderia o que não se sabe.
    /// </summary>
    private static void ValidateDiscard(ApiPlanOperationJournal journal, List<string> errors, bool discarded)
    {
        if (!discarded)
        {
            return;
        }

        if (journal.Abandonment is null)
        {
            errors.Add("logicalStage=Discarded exige o objeto abandonment com a disposição de quem encerrou.");
            return;
        }

        RequireText(journal.Abandonment.Reason, "abandonment.reason", errors);
        RequireText(journal.Abandonment.AuthorizedBy, "abandonment.authorizedBy", errors);
        if (journal.Abandonment.AuthorizedUtc == default)
        {
            errors.Add("abandonment.authorizedUtc é obrigatório.");
        }

        if (journal.OperationState != JournalOperationState.Completed)
        {
            errors.Add("o encerramento do registro mantém operationState=Completed.");
        }

        if (journal.EnvelopePhase != JournalEnvelopePhase.Active)
        {
            errors.Add("somente um envelope Active pode ter o registro encerrado.");
        }

        if (journal.JournalDurability != JournalDurability.Confirmed)
        {
            errors.Add("o encerramento do registro exige journalDurability=Confirmed.");
        }
    }

    private static void ValidateBlockReason(ApiPlanOperationJournal journal, List<string> errors)
    {
        var requiresBlockReason = journal.OperationState == JournalOperationState.Partial
            || journal.OperationState == JournalOperationState.OutcomeUnknown;
        if (requiresBlockReason && journal.BlockReason is null)
        {
            errors.Add("operationState Partial ou OutcomeUnknown exige blockReason.");
        }

        if (!requiresBlockReason && journal.BlockReason is not null)
        {
            errors.Add("blockReason só é persistido com operationState Partial ou OutcomeUnknown.");
        }

        if (journal.BlockReason == JournalBlockReason.RetryBudgetExhausted
            && journal.OperationKind != JournalOperationKind.Remove)
        {
            errors.Add("blockReason=RetryBudgetExhausted pertence ao orçamento de passadas do Remove.");
        }

        // Um aborto é decisão, não indeterminação: quem parou de propósito sabe que parou.
        // `OutcomeUnknown` significa que o resultado da última gravação não é conhecido, e
        // isso nunca decorre de o usuário ter cancelado.
        if (journal.BlockReason == JournalBlockReason.UserAborted
            && journal.OperationState != JournalOperationState.Partial)
        {
            errors.Add("blockReason=UserAborted exige operationState=Partial.");
        }
    }

    private static void ValidatePlan(ApiPlanOperationJournal journal, List<string> errors)
    {
        var plan = journal.Plan;
        if (plan is null)
        {
            errors.Add("plan é obrigatório.");
            return;
        }

        var expectedPlanKind = journal.OperationKind switch
        {
            JournalOperationKind.Apply => JournalPlanKind.Generation,
            JournalOperationKind.Sync => JournalPlanKind.Generation,
            JournalOperationKind.Remove => JournalPlanKind.Removal,
            _ => JournalPlanKind.MetadataRecovery,
        };

        if (plan.PlanKind != expectedPlanKind)
        {
            errors.Add(string.Format(
                CultureInfo.InvariantCulture,
                "plan.planKind incompatível com operationKind={0}: esperado {1}, encontrado {2}.",
                journal.OperationKind,
                expectedPlanKind,
                plan.PlanKind));
        }

        switch (plan.PlanKind)
        {
            case JournalPlanKind.Generation:
                // A identidade da API é **lida** do `API.Create` e nunca atribuída. Numa
                // criação nova ela ainda não existe quando a intenção é registrada, porque o
                // Create acontece dentro do pipeline, depois do envelope. Por isso o campo é
                // exigido a partir do estágio em que a identidade já tem de existir — o
                // mesmo momento em que a decisão 50 manda bloquear: antes do write do API.
                if (RequiresPlannedApiGuid(journal)
                    && (!plan.PlannedApiGuid.HasValue || plan.PlannedApiGuid.Value == Guid.Empty))
                {
                    errors.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "plan.plannedApiGuid é obrigatório em Apply e Sync a partir do estágio {0}.",
                        journal.LogicalStage));
                }

                if (plan.PlannedApiGuid.HasValue && plan.PlannedApiGuid.Value == Guid.Empty)
                {
                    errors.Add("plan.plannedApiGuid não pode ser o GUID vazio.");
                }

                if (string.IsNullOrWhiteSpace(plan.ContractHash))
                {
                    errors.Add("plan.contractHash é obrigatório em Apply e Sync.");
                }

                RequireFlag(plan.GenerateApiObject, "plan.generateApiObject", errors);
                RequireFlag(plan.GenerateSdts, "plan.generateSdts", errors);
                RequireFlag(plan.GenerateProcedures, "plan.generateProcedures", errors);
                RequireFlag(plan.GenerateMetadata, "plan.generateMetadata", errors);

                if (plan.InventorySufficiency.HasValue)
                {
                    errors.Add("plan.inventorySufficiency não existe em Generation: a suficiência é avaliação de remoção e recuperação.");
                }

                break;

            case JournalPlanKind.Removal:
                if (journal.Inventory.Count == 0)
                {
                    errors.Add("plan de Remove exige o inventário completo dos alvos.");
                }

                if (string.IsNullOrWhiteSpace(plan.ContractHash) && journal.IntentKind != JournalIntentKind.Imported)
                {
                    errors.Add("plan.contractHash só pode ser nulo em Remove sobre metadata legada importada.");
                }

                if (journal.Inventory.Any(item => item.ObjectType == JournalObjectType.ApiObject)
                    && (!plan.PlannedApiGuid.HasValue || plan.PlannedApiGuid.Value == Guid.Empty))
                {
                    errors.Add("plan.plannedApiGuid é obrigatório quando o inventário de Remove contém o API Object.");
                }

                RequireAbsentFlags(plan, "Remove", errors);

                if (!journal.AcceptsLegacyShapes && !plan.InventorySufficiency.HasValue)
                {
                    errors.Add("plan.inventorySufficiency é obrigatório em Remove.");
                }

                break;

            case JournalPlanKind.MetadataRecovery:
                if (!string.IsNullOrWhiteSpace(plan.ContractHash))
                {
                    // A recuperação de metadata órfã devolve inventário, nunca contrato: os
                    // blocos não recuperáveis ficam ausentes, e inventá-los faria o Sync
                    // comparar a API real contra uma descrição falsa.
                    errors.Add("plan.contractHash não existe em MetadataRecovery: a recuperação não reconstrói contrato.");
                }

                RequireAbsentFlags(plan, "MetadataRecovery", errors);

                if (!journal.AcceptsLegacyShapes && !plan.InventorySufficiency.HasValue)
                {
                    errors.Add("plan.inventorySufficiency é obrigatório em MetadataRecovery.");
                }

                break;
        }

        if (plan.Services.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add("plan.services não admite entradas vazias.");
        }
    }

    private static void ValidateReceipts(ApiPlanOperationJournal journal, List<string> errors)
    {
        var seen = new HashSet<int>();
        var previous = 0;
        foreach (var receipt in journal.Receipts)
        {
            if (receipt.Sequence <= 0)
            {
                errors.Add("receipts[].sequence deve ser inteiro positivo.");
            }
            else if (!seen.Add(receipt.Sequence))
            {
                errors.Add("receipts[].sequence deve ser único dentro da operação: " + receipt.Sequence + ".");
            }
            else if (receipt.Sequence <= previous)
            {
                errors.Add("receipts deve ser monotônico dentro da operação.");
            }

            if (receipt.Sequence > previous)
            {
                previous = receipt.Sequence;
            }

            if (receipt.Attempt <= 0)
            {
                errors.Add("receipts[].attempt deve ser inteiro positivo.");
            }

            RequireText(receipt.Stage, "receipts[].stage", errors);

            if (receipt.RetryOfSequence.HasValue && receipt.RetryOfSequence.Value >= receipt.Sequence)
            {
                errors.Add("receipts[].retryOfSequence deve apontar para um recibo anterior.");
            }

            // Retry é decidido por evidência, nunca por interpretação da mensagem: só um
            // Delete cujo alvo continua comprovadamente presente volta para a fila.
            if (receipt.RetryEligible)
            {
                if (receipt.Operation != JournalReceiptOperation.Delete
                    || receipt.Result != JournalResult.Failed
                    || receipt.PhysicalState != JournalPhysicalState.Present
                    || receipt.RetryableReason != JournalRetryableReason.StillPresentAfterDelete)
                {
                    errors.Add("receipts[].retryEligible=true exige Delete, Failed, Present e StillPresentAfterDelete.");
                }
            }
            else if (receipt.RetryableReason.HasValue)
            {
                errors.Add("receipts[].retryableReason só existe com retryEligible=true.");
            }

            if (!journal.AcceptsLegacyShapes && receipt.StartedUtc == default)
            {
                errors.Add("receipts[].startedUtc é obrigatório.");
            }

            if (receipt.EndedUtc.HasValue
                && receipt.StartedUtc != default
                && receipt.EndedUtc.Value < receipt.StartedUtc)
            {
                errors.Add("receipts[].endedUtc não pode ser anterior a startedUtc.");
            }

            if (!journal.AcceptsLegacyShapes
                && receipt.AttemptState != JournalAttemptState.Started
                && !receipt.EndedUtc.HasValue)
            {
                errors.Add("receipts[].endedUtc é obrigatório quando o recibo não ficou apenas iniciado.");
            }

            if (receipt.DurationMs < 0)
            {
                errors.Add("receipts[].durationMs não pode ser negativo.");
            }
        }

        var known = new HashSet<int>(journal.Receipts.Select(receipt => receipt.Sequence));
        foreach (var receipt in journal.Receipts)
        {
            if (receipt.RetryOfSequence.HasValue && !known.Contains(receipt.RetryOfSequence.Value))
            {
                errors.Add("receipts[].retryOfSequence referencia um recibo inexistente: " + receipt.RetryOfSequence.Value + ".");
            }
        }
    }

    private static void ValidateInventory(ApiPlanOperationJournal journal, List<string> errors)
    {
        var allowedActions = journal.OperationKind == JournalOperationKind.Remove
            ? new[] { JournalInventoryAction.Delete, JournalInventoryAction.Preserve }
            : new[] { JournalInventoryAction.Create, JournalInventoryAction.Update, JournalInventoryAction.Preserve };
        var known = new HashSet<int>(journal.Receipts.Select(receipt => receipt.Sequence));
        var identities = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in journal.Inventory)
        {
            RequireText(item.Name, "inventory[].name", errors);

            if (!allowedActions.Contains(item.Action))
            {
                errors.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "inventory[].action={0} não pertence ao domínio de operationKind={1}.",
                    item.Action,
                    journal.OperationKind));
            }

            // A Transaction, o próprio diário, as preferências, os SDTs compartilhados e o
            // Folder não próprio nunca entram na fila destrutiva: aparecem como Preserve.
            if (item.ObjectType == JournalObjectType.Transaction && item.Action == JournalInventoryAction.Delete)
            {
                errors.Add("a Transaction nunca entra na fila destrutiva.");
            }

            ValidateItemIdentity(item, errors);

            foreach (var sequence in item.ReceiptSequences)
            {
                if (!known.Contains(sequence))
                {
                    errors.Add("inventory[].receiptSequences referencia um recibo inexistente: " + sequence + ".");
                }
            }

            // Cada alvo e cada preservação aparecem uma vez.
            if (!identities.Add(BuildIdentityKey(item)))
            {
                errors.Add("inventory repete o mesmo alvo: " + item.Name + ".");
            }
        }
    }

    private static void ValidateItemIdentity(ApiPlanOperationJournalInventoryItem item, List<string> errors)
    {
        switch (item.IdentityKind)
        {
            case JournalIdentityKind.Guid:
                if (!item.Guid.HasValue || item.Guid.Value == Guid.Empty)
                {
                    errors.Add("identityKind=Guid exige guid.");
                }

                break;

            case JournalIdentityKind.FileId:
                if (!item.FileId.HasValue || item.FileId.Value <= 0)
                {
                    errors.Add("identityKind=FileId exige fileId inteiro positivo.");
                }

                if (string.IsNullOrWhiteSpace(item.ExpectedHash))
                {
                    errors.Add("identityKind=FileId exige expectedHash.");
                }

                break;

            case JournalIdentityKind.Composite:
                if (item.Composite is null)
                {
                    errors.Add("identityKind=Composite exige a identidade histórica completa.");
                    break;
                }

                RequireText(item.Composite.ExactName, "composite.exactName", errors);
                RequireText(item.Composite.ObjectTypeName, "composite.objectTypeName", errors);
                RequireText(item.Composite.Role, "composite.role", errors);

                // Nome isolado, Description isolada ou prefixo isolado nunca autorizam
                // exclusão: para apagar, a identidade histórica precisa estar inteira. Num
                // Save, a mesma identidade é só o endereço do alvo, e exigir dela os GUIDs
                // que só existem depois da gravação impediria registrar o recibo.
                if (item.Action == JournalInventoryAction.Delete)
                {
                    RequireText(item.Composite.CanonicalDescription, "composite.canonicalDescription", errors);
                    RequireGuid(item.Composite.TransactionGuid, "composite.transactionGuid", errors);
                    RequireGuid(item.Composite.ApiGuid, "composite.apiGuid", errors);
                }

                break;

            case JournalIdentityKind.Folder:
                // `emptyConfirmed` é exigência da fila destrutiva: só se apaga um Folder
                // próprio depois de confirmá-lo vazio. Num Apply, o mesmo Folder aparece
                // como alvo de criação ou reuso, e exigir vazio ali não teria sentido.
                if (item.Action == JournalInventoryAction.Delete && item.EmptyConfirmed != true)
                {
                    errors.Add("identityKind=Folder exige emptyConfirmed=true para ser removido.");
                }

                if (!item.OwnershipValidated)
                {
                    errors.Add("identityKind=Folder exige posse própria validada.");
                }

                break;

            case JournalIdentityKind.None:
                if (item.Action != JournalInventoryAction.Preserve)
                {
                    errors.Add("identityKind=None só é permitido em item Preserve.");
                }

                break;
        }
    }

    private static void ValidateMetadataSchemaVersion(ApiPlanOperationJournal journal, List<string> errors)
    {
        var version = journal.MetadataSchemaVersion;
        if (version is not null && !ApiPlanMetadataSchema.IsSupported(version))
        {
            errors.Add("metadataSchemaVersion desconhecida: " + version + ".");
        }

        var hasMetadata = journal.Inventory.Any(item => item.ObjectType == JournalObjectType.MetadataFile)
            || journal.Plan?.PlanKind == JournalPlanKind.MetadataRecovery;
        if (hasMetadata && version is null)
        {
            errors.Add("metadataSchemaVersion é obrigatório quando a operação envolve metadata.");
        }
    }

    /// <summary>
    /// Chave de unicidade de um alvo no inventário. É a mesma que o mapeador de recibos usa
    /// para agregar: se as duas divergissem, dois recibos do mesmo objeto virariam dois itens
    /// que o validador recusa como repetidos.
    /// </summary>
    internal static string BuildIdentityKey(ApiPlanOperationJournalInventoryItem item) =>
        item.ObjectType + "|" + item.IdentityKind + "|" + DescribeIdentity(item);

    private static string DescribeIdentity(ApiPlanOperationJournalInventoryItem item) => item.IdentityKind switch
    {
        JournalIdentityKind.Guid => item.Guid?.ToString("D", CultureInfo.InvariantCulture) ?? string.Empty,
        JournalIdentityKind.FileId => item.FileId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
        JournalIdentityKind.Composite => item.Composite is null
            ? string.Empty
            : item.Composite.ObjectTypeName + ":" + item.Composite.Role + ":" + item.Composite.ExactName,
        _ => item.Name,
    };

    /// <summary>
    /// Estágios em que a identidade da API já precisa estar registrada: a partir da
    /// persistência do API Object não há mais como reencontrá-lo sem ela.
    /// </summary>
    private static bool RequiresPlannedApiGuid(ApiPlanOperationJournal journal) =>
        journal.LogicalStage == JournalLogicalStage.ApiPhysicallySaved
        || journal.LogicalStage == JournalLogicalStage.ApiSaveOutcomeUnknown
        || journal.LogicalStage == JournalLogicalStage.Completed;

    private static void RequireFlag(bool? value, string name, List<string> errors)
    {
        if (!value.HasValue)
        {
            errors.Add(name + " é obrigatório em Apply e Sync.");
        }
    }

    private static void RequireAbsentFlags(ApiPlanOperationJournalPlan plan, string planDescription, List<string> errors)
    {
        if (plan.GenerateApiObject.HasValue
            || plan.GenerateSdts.HasValue
            || plan.GenerateProcedures.HasValue
            || plan.GenerateMetadata.HasValue)
        {
            errors.Add("as flags de geração não pertencem ao plano de " + planDescription + ".");
        }
    }

    private static void RequireGuid(Guid value, string name, List<string> errors)
    {
        if (value == Guid.Empty)
        {
            errors.Add(name + " é obrigatório.");
        }
    }

    private static void RequireText(string? value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(name + " é obrigatório.");
        }
    }
}

public sealed class ApiPlanOperationJournalValidation
{
    internal ApiPlanOperationJournalValidation(IReadOnlyList<string> errors)
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }

    public bool IsValid => Errors.Count == 0;

    public string Describe() => string.Join(" ", Errors);
}
