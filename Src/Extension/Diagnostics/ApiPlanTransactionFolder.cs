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

        var existingFolder = Preflight(designModel, apiPlan);
        if (existingFolder is not null)
        {
            AlignWithTransactionContainer(existingFolder, transaction);
            existingFolder.Save();
            return existingFolder;
        }

        var folder = new Folder(designModel, apiPlan.TransactionFolderName)
        {
            Description = CreateOwnedDescription(apiPlan),
        };

        AlignWithTransactionContainer(folder, transaction);

        folder.Save();
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

    public static Folder? Preflight(KBModel designModel, ApiPlan apiPlan)
    {
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
        var expectedDescription = CreateOwnedDescription(apiPlan);
        if (!string.Equals(folder.Description, expectedDescription, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Criacao de Folder bloqueada: ja existe Folder externo ou incompativel chamado '{apiPlan.TransactionFolderName}'. Nenhuma alteracao foi feita.");
        }

        return folder;
    }
}
