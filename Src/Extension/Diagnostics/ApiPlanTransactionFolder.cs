using System;
using System.Linq;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;
using GenexusOpenApiBuilder.Extension.Domain;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

internal static class ApiPlanTransactionFolder
{
    public static Folder CreateOrReencounter(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        ApiPlanPersistenceLog? persistenceLog = null)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        var existingFolder = Preflight(designModel, transaction, apiPlan);
        if (existingFolder is not null)
        {
            apiPlan.TransactionFolderGuid = existingFolder.Guid;
            return existingFolder;
        }

        var folder = new Folder(designModel, apiPlan.TransactionFolderName)
        {
            Description = ApiPlanOwnedObjectDescription.CreateTransactionFolderDescription(apiPlan.TransactionFolderName),
        };

        AlignWithTransactionContainer(folder, transaction);

        var receipt = ApiPlanSaveBoundaryProbe.Persist(
            PersistenceFaultPoint.FolderSave,
            "Save",
            "Folder",
            "TransactionFolder",
            folder.Name,
            new FolderIdentity(folder.Name, owned: true, emptyConfirmed: true),
            folder.Save,
            () => ConfirmFolder(designModel, transaction, folder.Name, apiPlan));
        EnsureConfirmed(receipt, () => ConfirmFolder(designModel, transaction, folder.Name, apiPlan), $"Folder '{folder.Name}'");
        apiPlan.TransactionFolderWasCreated = true;
        apiPlan.TransactionFolderOwnedByThisApi = true;
        apiPlan.TransactionFolderGuid = folder.Guid;
        return folder;
    }

    private static PersistenceConfirmation ConfirmFolder(KBModel designModel, Transaction transaction, string name, ApiPlan apiPlan)
    {
        try
        {
            var matches = Folder.GetAll(designModel)
                .Where(folder => string.Equals(folder.Name, name, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matches.Length == 0)
            {
                return PersistenceConfirmation.Absent("Folder não foi reencontrado pelo nome exato.");
            }

            if (matches.Length != 1 || !IsReusable(matches[0], transaction, apiPlan))
            {
                return PersistenceConfirmation.Divergent(
                    string.Join(",", matches.Select(folder => folder.Guid.ToString())),
                    "O Folder persistido não corresponde ao Folder de Transaction gerenciado.");
            }

            return PersistenceConfirmation.Confirmed(matches[0].Guid.ToString());
        }
        catch (Exception exception)
        {
            return PersistenceConfirmation.Unreadable(exception);
        }
    }

    private static void EnsureConfirmed(
        PersistenceReceipt? receipt,
        Func<PersistenceConfirmation> fallbackConfirmation,
        string description)
    {
        if (receipt is not null)
        {
            if (receipt.Outcome != PersistenceOutcome.Confirmed)
            {
                throw new InvalidOperationException(
                    $"Persistência de {description} não foi confirmada: Outcome='{receipt.Outcome}', Confirmation='{receipt.Confirmation}', Detail='{receipt.ConfirmationDetail}'.",
                    receipt.ConfirmationCause);
            }

            return;
        }

        var confirmation = fallbackConfirmation();
        if (confirmation.Status != PersistenceConfirmationStatus.Confirmed ||
            confirmation.PhysicalState != PersistencePhysicalState.Present)
        {
            throw new InvalidOperationException(
                $"Persistência de {description} não foi confirmada: Confirmation='{confirmation.Status}', PhysicalState='{confirmation.PhysicalState}', Detail='{confirmation.Detail}'.",
                confirmation.Cause);
        }
    }

    internal static Folder GetOrReencounterStrict(KBModel designModel, Transaction transaction, ApiPlan apiPlan)
    {
        if (designModel is null) throw new ArgumentNullException(nameof(designModel));
        if (transaction is null) throw new ArgumentNullException(nameof(transaction));
        if (apiPlan is null) throw new ArgumentNullException(nameof(apiPlan));

        var folder = Preflight(designModel, transaction, apiPlan);
        if (folder is null)
        {
            throw new InvalidOperationException($"Reencontro estrito bloqueado: Folder requerido '{apiPlan.TransactionFolderName}' nao existe. Gere os artefatos base antes. Nenhuma alteracao foi feita.");
        }

        apiPlan.TransactionFolderGuid = folder.Guid;
        return folder;
    }

    private static void AlignWithTransactionContainer(Folder folder, Transaction transaction)
    {
        if (transaction.Parent is not null)
        {
            folder.Parent = transaction.Parent;
            return;
        }

        if (transaction.Module is not null)
        {
            folder.Module = transaction.Module;
        }
    }

    public static string CreateOwnedDescription(ApiPlan apiPlan)
    {
        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        return ApiPlanOwnedObjectDescription.CreateTransactionFolderDescription(apiPlan.TransactionFolderName);
    }

    public static Folder? Preflight(KBModel designModel, Transaction transaction, ApiPlan apiPlan)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        ApiPlanWritePreflight.ValidateTransactionIdentity(transaction, apiPlan, "Folder");

        var folders = Folder.GetAll(designModel)
            .Where(folder => string.Equals(folder.Name, apiPlan.TransactionFolderName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (folders.Length > 1)
        {
            throw new InvalidOperationException($"Criacao de Folder bloqueada: foram encontrados {folders.Length} Folders chamados '{apiPlan.TransactionFolderName}'. Nenhuma alteracao foi feita.");
        }

        if (folders.Length == 0)
        {
            return null;
        }

        var folder = folders[0];
        if (!IsReusable(folder, transaction, apiPlan))
        {
            throw new InvalidOperationException($"Criacao de Folder bloqueada: ja existe Folder externo ou incompativel chamado '{apiPlan.TransactionFolderName}'. Nenhuma alteracao foi feita.");
        }

        return folder;
    }

    internal static bool IsReusable(Folder folder, Transaction transaction, ApiPlan apiPlan)
    {
        if (folder is null)
        {
            throw new ArgumentNullException(nameof(folder));
        }

        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        if (!IsInExpectedContainer(folder, transaction))
        {
            return false;
        }

        var description = folder.Description ?? string.Empty;
        return ApiPlanOwnedObjectDescription.IsReusableTransactionFolderDescription(
            description,
            apiPlan.TransactionFolderName,
            apiPlan.TransactionName);
    }

    internal static string CreateReuseWarning(ApiPlan apiPlan, bool ownedByThisApi)
    {
        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        if (ownedByThisApi)
        {
            return $"Folder preexistente '{apiPlan.TransactionFolderName}' no contenedor correto sera reutilizado; a Description existente sera preservada e a remocao desta API apagara o Folder se ele ficar vazio.";
        }

        return $"Folder preexistente '{apiPlan.TransactionFolderName}' no contenedor correto sera reutilizado; a Description existente sera preservada e o Folder nunca sera removido pela remocao desta API.";
    }

    internal static bool IsInExpectedContainer(Folder folder, Transaction transaction)
    {
        if (transaction.Parent is not null)
        {
            return folder.Parent is not null && folder.Parent.Guid == transaction.Parent.Guid;
        }

        if (transaction.Module is not null)
        {
            return folder.Parent is null
                && folder.Module is not null
                && folder.Module.Guid == transaction.Module.Guid;
        }

        return folder.Parent is null && folder.Module is null;
    }
}
