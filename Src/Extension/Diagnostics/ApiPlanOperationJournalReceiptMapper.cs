#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// S-B111 / F3 — leva os recibos da F2 e o inventário para dentro do envelope do diário.
///
/// A F2 produz <see cref="PersistenceReceipt"/> em memória; eles morrem com o processo. A
/// matriz de checkpoints da seção 4.4 do plano manda gravá-los nas fronteiras, porque é isso
/// que permite dizer, depois de uma interrupção, **o que** foi confirmado — e não apenas em
/// que ponto a operação parou.
///
/// O mapeamento é fechado de propósito: um tipo de objeto que esta classe não conheça vira
/// exceção, que a sessão converte em bloqueio visível do diário. Gravar um recibo com tipo
/// inventado seria pior que não gravar.
/// </summary>
internal static class ApiPlanOperationJournalReceiptMapper
{
    internal static IReadOnlyList<ApiPlanOperationJournalReceipt> MapReceipts(
        IEnumerable<PersistenceReceipt> receipts)
    {
        if (receipts is null)
        {
            throw new ArgumentNullException(nameof(receipts));
        }

        var mapped = new List<ApiPlanOperationJournalReceipt>();
        foreach (var receipt in receipts.OrderBy(item => item.Sequence))
        {
            mapped.Add(new ApiPlanOperationJournalReceipt
            {
                Sequence = ToSequence(receipt.Sequence),
                Operation = MapOperation(receipt.OperationKind),
                Stage = string.IsNullOrWhiteSpace(receipt.Stage) ? receipt.ObjectType : receipt.Stage,
                ObjectType = MapObjectType(receipt.ObjectType),
                Attempt = receipt.Attempt <= 0 ? 1 : receipt.Attempt,
                RetryOfSequence = receipt.RetryOfSequence.HasValue
                    ? ToSequence(receipt.RetryOfSequence.Value)
                    : (int?)null,
                AttemptState = MapAttemptState(receipt.AttemptState),
                Result = MapResult(receipt.Outcome),
                Confirmation = MapConfirmation(receipt.Confirmation),
                PhysicalState = MapPhysicalState(receipt.PhysicalState),
                RetryEligible = receipt.RetryEligible,
                RetryableReason = receipt.RetryEligible ? JournalRetryableReason.StillPresentAfterDelete : (JournalRetryableReason?)null,
            });
        }

        return mapped;
    }

    /// <summary>
    /// Inventário derivado dos recibos: um item por alvo, com a última observação conhecida.
    ///
    /// Em Apply e Sync, o inventário registra o que a operação efetivamente tocou — a
    /// identidade só existe depois que o objeto existe. A distinção entre `Create` e
    /// `Update` vem da lista de criados do relatório final, porque o recibo prova a gravação
    /// mas não diz se o alvo nasceu agora.
    /// </summary>
    internal static IReadOnlyList<ApiPlanOperationJournalInventoryItem> BuildInventory(
        IEnumerable<PersistenceReceipt> receipts,
        IEnumerable<string>? createdNames,
        JournalOperationKind operationKind)
    {
        if (receipts is null)
        {
            throw new ArgumentNullException(nameof(receipts));
        }

        var created = new HashSet<string>(
            (createdNames ?? Array.Empty<string>()).Where(name => !string.IsNullOrWhiteSpace(name)),
            StringComparer.OrdinalIgnoreCase);

        var byTarget = new Dictionary<string, ApiPlanOperationJournalInventoryItem>(StringComparer.Ordinal);
        var sequencesByTarget = new Dictionary<string, List<int>>(StringComparer.Ordinal);

        foreach (var receipt in receipts.OrderBy(item => item.Sequence))
        {
            var sequence = ToSequence(receipt.Sequence);
            var item = new ApiPlanOperationJournalInventoryItem
            {
                ObjectType = MapObjectType(receipt.ObjectType),
                Name = receipt.PlannedName,
                OwnershipValidated = true,
                Action = ResolveAction(operationKind, receipt, created),
                PhysicalState = MapPhysicalState(receipt.PhysicalState),
                Confirmation = MapConfirmation(receipt.Confirmation),
            };

            ApplyIdentity(item, receipt.Identity);

            // A chave é a do validador, não a da F2: identidades diferentes do mesmo alvo
            // (um Save antes do objeto existir e outro depois) precisam colapsar no mesmo
            // item, senão o inventário sai com alvo repetido.
            var key = ApiPlanOperationJournalValidator.BuildIdentityKey(item);
            if (!sequencesByTarget.TryGetValue(key, out var sequences))
            {
                sequences = new List<int>();
                sequencesByTarget[key] = sequences;
            }

            sequences.Add(sequence);
            byTarget[key] = item;
        }

        foreach (var pair in byTarget)
        {
            foreach (var sequence in sequencesByTarget[pair.Key])
            {
                pair.Value.ReceiptSequences.Add(sequence);
            }
        }

        return byTarget.Values.ToArray();
    }

    /// <summary>
    /// Associa cada recibo ao item de inventário que já existe — o caminho do Remove, em que o
    /// inventário vem da intenção registrada antes do primeiro <c>Delete()</c> e não pode ser
    /// reconstruído a partir dos recibos.
    ///
    /// A ligação é pela mesma chave de identidade que o validador usa. Um recibo cujo alvo não
    /// está no inventário é ignorado de propósito: ele pertence a outra etapa da operação, e
    /// inventar um item para ele acrescentaria ao envelope um alvo que a intenção não declarou.
    /// </summary>
    internal static IReadOnlyList<ApiPlanOperationJournalInventoryItem> AttachReceiptSequences(
        IEnumerable<ApiPlanOperationJournalInventoryItem> inventory,
        IEnumerable<PersistenceReceipt> receipts)
    {
        if (inventory is null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        if (receipts is null)
        {
            throw new ArgumentNullException(nameof(receipts));
        }

        var items = inventory.ToArray();
        var byKey = new Dictionary<string, ApiPlanOperationJournalInventoryItem>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            byKey[ApiPlanOperationJournalValidator.BuildIdentityKey(item)] = item;
        }

        foreach (var receipt in receipts.OrderBy(item => item.Sequence))
        {
            // A chave sai da mesma derivação de identidade que monta um item: é ela que faz o
            // recibo de um Delete encontrar o alvo que a intenção declarou.
            var probe = new ApiPlanOperationJournalInventoryItem
            {
                ObjectType = MapObjectType(receipt.ObjectType),
                Name = receipt.PlannedName,
            };
            ApplyIdentity(probe, receipt.Identity);

            if (!byKey.TryGetValue(ApiPlanOperationJournalValidator.BuildIdentityKey(probe), out var item))
            {
                continue;
            }

            var sequence = ToSequence(receipt.Sequence);
            if (!item.ReceiptSequences.Contains(sequence))
            {
                item.ReceiptSequences.Add(sequence);
            }
        }

        return items;
    }

    private static void ApplyIdentity(ApiPlanOperationJournalInventoryItem item, PersistenceIdentity identity)
    {
        switch (identity)
        {
            case GuidIdentity guidIdentity:
                item.IdentityKind = JournalIdentityKind.Guid;
                item.Guid = guidIdentity.Guid;
                break;

            // A F2 identifica File pelo GUID do objeto, não pelo Id numérico; o hash esperado
            // vem junto e é preservado como material de conferência.
            case FileIdentity fileIdentity:
                item.IdentityKind = JournalIdentityKind.Guid;
                item.Guid = fileIdentity.FileId;
                item.ExpectedHash = fileIdentity.ExpectedSha256;
                break;

            case CompositeIdentity composite:
                item.IdentityKind = JournalIdentityKind.Composite;
                item.Composite = new ApiPlanOperationJournalCompositeIdentity
                {
                    ExactName = composite.ExactName,
                    ObjectTypeName = composite.ObjectType,
                    Role = composite.Role,
                    CanonicalDescription = composite.CanonicalDescription,
                    TransactionGuid = composite.TransactionGuid,
                    ApiGuid = composite.ApiGuid,
                };
                break;

            case FolderIdentity folderIdentity:
                item.IdentityKind = JournalIdentityKind.Folder;
                item.OwnershipValidated = folderIdentity.Owned;
                item.EmptyConfirmed = folderIdentity.EmptyConfirmed ? true : (bool?)null;
                break;

            default:
                item.IdentityKind = JournalIdentityKind.None;
                item.Action = JournalInventoryAction.Preserve;
                break;
        }
    }

    private static JournalInventoryAction ResolveAction(
        JournalOperationKind operationKind,
        PersistenceReceipt receipt,
        ICollection<string> createdNames)
    {
        if (string.Equals(receipt.OperationKind, "Delete", StringComparison.OrdinalIgnoreCase))
        {
            return JournalInventoryAction.Delete;
        }

        if (operationKind == JournalOperationKind.Remove)
        {
            return JournalInventoryAction.Preserve;
        }

        return createdNames.Contains(receipt.PlannedName)
            ? JournalInventoryAction.Create
            : JournalInventoryAction.Update;
    }

    internal static JournalObjectType MapObjectType(string objectType) => objectType switch
    {
        "Transaction" => JournalObjectType.Transaction,
        "Folder" => JournalObjectType.Folder,
        "API" => JournalObjectType.ApiObject,
        "Procedure" => JournalObjectType.Procedure,
        "SDT" => JournalObjectType.Sdt,
        "File" => JournalObjectType.MetadataFile,
        _ => throw new InvalidOperationException(
            "Tipo de objeto sem correspondência no schema do diário: '" + objectType + "'."),
    };

    private static JournalReceiptOperation MapOperation(string operationKind) =>
        string.Equals(operationKind, "Delete", StringComparison.OrdinalIgnoreCase)
            ? JournalReceiptOperation.Delete
            : JournalReceiptOperation.Save;

    private static JournalAttemptState MapAttemptState(PersistenceAttemptState state) => state switch
    {
        PersistenceAttemptState.Started => JournalAttemptState.Started,
        PersistenceAttemptState.Interrupted => JournalAttemptState.Interrupted,
        _ => JournalAttemptState.Finished,
    };

    private static JournalResult MapResult(PersistenceOutcome outcome) => outcome switch
    {
        PersistenceOutcome.Confirmed => JournalResult.Confirmed,
        PersistenceOutcome.Failed => JournalResult.Failed,
        _ => JournalResult.OutcomeUnknown,
    };

    private static JournalConfirmation MapConfirmation(PersistenceConfirmationStatus status) => status switch
    {
        PersistenceConfirmationStatus.Confirmed => JournalConfirmation.Confirmed,
        PersistenceConfirmationStatus.Absent => JournalConfirmation.Absent,
        PersistenceConfirmationStatus.Divergent => JournalConfirmation.Divergent,
        PersistenceConfirmationStatus.Unreadable => JournalConfirmation.Unreadable,
        _ => JournalConfirmation.NotAttempted,
    };

    private static JournalPhysicalState MapPhysicalState(PersistencePhysicalState state) => state switch
    {
        PersistencePhysicalState.Present => JournalPhysicalState.Present,
        PersistencePhysicalState.Absent => JournalPhysicalState.Absent,
        _ => JournalPhysicalState.Unknown,
    };

    private static int ToSequence(long sequence)
    {
        if (sequence <= 0 || sequence > int.MaxValue)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.InvariantCulture,
                "Sequência de recibo fora do domínio do schema do diário: {0}.",
                sequence));
        }

        return (int)sequence;
    }
}
