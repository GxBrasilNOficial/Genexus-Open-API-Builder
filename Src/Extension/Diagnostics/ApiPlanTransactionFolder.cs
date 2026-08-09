using System;
using System.Linq;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;
using GenexusOpenApiBuilder.Extension.Domain;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

internal static class ApiPlanTransactionFolder
{
    private const string OwnedDescriptionPrefix = "Genexus Open API Builder Transaction API folder";

    public static Folder CreateOrReencounter(KBModel designModel, Transaction transaction, ApiPlan apiPlan)
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
            return existingFolder;
        }

        var folder = new Folder(designModel, apiPlan.TransactionFolderName)
        {
            Description = CreateOwnedDescription(apiPlan),
        };

        AlignWithTransactionContainer(folder, transaction);

        folder.Save();
        apiPlan.TransactionFolderWasCreated = true;
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

        return $"{OwnedDescriptionPrefix} - Transaction={apiPlan.TransactionName}";
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
        if (description.StartsWith(OwnedDescriptionPrefix, StringComparison.Ordinal)
            && !string.Equals(description, CreateOwnedDescription(apiPlan), StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    internal static string CreateReuseWarning(ApiPlan apiPlan)
    {
        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        return $"Folder preexistente '{apiPlan.TransactionFolderName}' no contenedor correto sera reutilizado; a Description existente sera preservada e o Folder nunca sera removido pela remocao desta API.";
    }

    private static bool IsInExpectedContainer(Folder folder, Transaction transaction)
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
