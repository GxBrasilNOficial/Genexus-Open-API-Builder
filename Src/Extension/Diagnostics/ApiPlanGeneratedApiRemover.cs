#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;
using Artech.Genexus.Common.Wiki;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

internal static class ApiPlanGeneratedApiRemover
{
    private const string ProcedureDescriptionPrefix = "Genexus Open API Builder B050-B053 Procedure";
    private const string SdtDescriptionPrefix = "Genexus Open API Builder";

    public static ApiPlanGeneratedApiRemovalResult Remove(KBModel designModel, Transaction transaction)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        var metadataFileName = $"api{transaction.Name}_Metadata";
        var metadataFile = FindOwnedMetadataFile(designModel, metadataFileName, transaction.Name);
        var metadata = ParseMetadata(metadataFile);
        var plan = ApiPlanGeneratedApiRemovalPlan.FromMetadata(metadata, transaction.Name, transaction.Guid.ToString());

        // Ordem obrigatoria na IDE:
        // 1) API Object (referencia Procedures)
        // 2) Procedures (tipam SDTs)
        // 3) SDTs proprios na ordem do plano (ListResponse antes de Response)
        var deleted = new List<string>();
        DeleteApiObject(designModel, plan, deleted);
        DeleteProcedures(designModel, plan, deleted);
        DeleteOwnSdts(designModel, plan, deleted);
        DeleteMetadataFile(designModel, metadataFile, deleted);
        MaybeDeleteFolder(designModel, plan, deleted);

        return new ApiPlanGeneratedApiRemovalResult(plan, deleted);
    }

    public static ApiPlanGeneratedApiRemovalPlan Preview(KBModel designModel, Transaction transaction)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        var metadataFileName = $"api{transaction.Name}_Metadata";
        var metadataFile = FindOwnedMetadataFile(designModel, metadataFileName, transaction.Name);
        var metadata = ParseMetadata(metadataFile);
        return ApiPlanGeneratedApiRemovalPlan.FromMetadata(metadata, transaction.Name, transaction.Guid.ToString());
    }

    private static WikiFileKBObject FindOwnedMetadataFile(KBModel designModel, string metadataFileName, string transactionName)
    {
        var matches = WikiFileKBObject.GetAll(designModel)
            .Where(file => string.Equals(file.Name, metadataFileName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matches.Length == 0)
        {
            throw new InvalidOperationException($"Remocao bloqueada: File de metadata '{metadataFileName}' nao foi encontrado. Nenhuma alteracao foi feita.");
        }

        if (matches.Length > 1)
        {
            throw new InvalidOperationException($"Remocao bloqueada: foram encontrados {matches.Length} Files chamados '{metadataFileName}'. Nenhuma alteracao foi feita.");
        }

        var file = matches[0];
        if (file.Description is null ||
            !file.Description.StartsWith("Genexus Open API Builder B060 Metadata File", StringComparison.Ordinal) ||
            file.Description.IndexOf($"Transaction={transactionName}", StringComparison.Ordinal) < 0)
        {
            throw new InvalidOperationException($"Remocao bloqueada: File '{metadataFileName}' nao e metadata propria da extensao. Nenhuma alteracao foi feita.");
        }

        return file;
    }

    private static JObject ParseMetadata(WikiFileKBObject file)
    {
        var bytes = file.BlobPart?.Data?.GetBytes();
        if (bytes is null || bytes.Length == 0)
        {
            throw new InvalidOperationException($"Remocao bloqueada: File '{file.Name}' nao possui JSON persistido. Nenhuma alteracao foi feita.");
        }

        try
        {
            return JObject.Parse(Encoding.UTF8.GetString(bytes));
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Remocao bloqueada: File '{file.Name}' possui JSON invalido. Nenhuma alteracao foi feita.", ex);
        }
    }

    private static void DeleteProcedures(KBModel designModel, ApiPlanGeneratedApiRemovalPlan plan, List<string> deleted)
    {
        foreach (var name in plan.ProcedureNames)
        {
            var matches = Procedure.GetAll(designModel)
                .Where(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matches.Length == 0)
            {
                continue;
            }

            if (matches.Length > 1)
            {
                throw new InvalidOperationException($"Remocao bloqueada: Procedure ambigua '{name}'. Nenhuma alteracao adicional sera feita apos o ponto de falha.");
            }

            var procedure = matches[0];
            if (procedure.Description is null ||
                !procedure.Description.StartsWith(ProcedureDescriptionPrefix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Remocao bloqueada: Procedure '{name}' nao e propria da extensao. Nenhuma alteracao adicional sera feita apos o ponto de falha.");
            }

            var guid = procedure.Guid;
            procedure.Delete();
            if (Procedure.GetAll(designModel).Any(item => item.Guid == guid))
            {
                throw new InvalidOperationException($"Remocao falhou: Procedure '{name}' ainda existe apos Delete().");
            }

            deleted.Add($"Procedure:{name}");
        }
    }

    private static void DeleteApiObject(KBModel designModel, ApiPlanGeneratedApiRemovalPlan plan, List<string> deleted)
    {
        var matches = API.GetAll(designModel)
            .Where(item => string.Equals(item.Name, plan.ApiName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (matches.Length == 0)
        {
            return;
        }

        if (matches.Length > 1)
        {
            throw new InvalidOperationException($"Remocao bloqueada: API Object ambiguo '{plan.ApiName}'.");
        }

        var api = matches[0];
        if (!Guid.TryParse(plan.ApiGuid, out var ownershipGuid) || api.Guid != ownershipGuid)
        {
            throw new InvalidOperationException($"Remocao bloqueada: API Object '{plan.ApiName}' nao corresponde ao Guid da metadata.");
        }

        var guid = api.Guid;
        api.Delete();
        if (API.GetAll(designModel).Any(item => item.Guid == guid))
        {
            throw new InvalidOperationException($"Remocao falhou: API Object '{plan.ApiName}' ainda existe apos Delete().");
        }

        deleted.Add($"API:{plan.ApiName}");
    }

    private static void DeleteOwnSdts(KBModel designModel, ApiPlanGeneratedApiRemovalPlan plan, List<string> deleted)
    {
        foreach (var name in plan.OwnSdtNames)
        {
            if (plan.SharedSdtNamesPreserved.Contains(name, StringComparer.Ordinal))
            {
                throw new InvalidOperationException($"Remocao bloqueada: tentativa de apagar SDT compartilhado '{name}'.");
            }

            var matches = SDT.GetAll(designModel)
                .Where(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matches.Length == 0)
            {
                continue;
            }

            if (matches.Length > 1)
            {
                throw new InvalidOperationException($"Remocao bloqueada: SDT ambiguo '{name}'.");
            }

            var sdt = matches[0];
            if (sdt.Description is null ||
                !sdt.Description.StartsWith(SdtDescriptionPrefix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Remocao bloqueada: SDT '{name}' nao e proprio da extensao.");
            }

            var guid = sdt.Guid;
            sdt.Delete();
            if (SDT.GetAll(designModel).Any(item => item.Guid == guid))
            {
                throw new InvalidOperationException($"Remocao falhou: SDT '{name}' ainda existe apos Delete().");
            }

            deleted.Add($"SDT:{name}");
        }
    }

    private static void DeleteMetadataFile(KBModel designModel, WikiFileKBObject metadataFile, List<string> deleted)
    {
        var name = metadataFile.Name;
        var guid = metadataFile.Guid;
        metadataFile.Delete();
        if (WikiFileKBObject.GetAll(designModel).Any(item => item.Guid == guid))
        {
            throw new InvalidOperationException($"Remocao falhou: File '{name}' ainda existe apos Delete().");
        }

        deleted.Add($"File:{name}");
    }

    private static void MaybeDeleteFolder(KBModel designModel, ApiPlanGeneratedApiRemovalPlan plan, List<string> deleted)
    {
        if (!plan.FolderWasCreated || string.IsNullOrWhiteSpace(plan.FolderName))
        {
            return;
        }

        var matches = Folder.GetAll(designModel)
            .Where(item => string.Equals(item.Name, plan.FolderName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (matches.Length != 1)
        {
            return;
        }

        var folder = matches[0];
        var expectedDescription = $"Genexus Open API Builder Transaction API folder - Transaction={plan.TransactionName}";
        if (!string.Equals(folder.Description, expectedDescription, StringComparison.Ordinal))
        {
            return;
        }

        if (!IsFolderEmpty(designModel, folder))
        {
            deleted.Add($"Folder:{plan.FolderName}:PreservedNonEmpty");
            return;
        }

        var guid = folder.Guid;
        folder.Delete();
        if (Folder.GetAll(designModel).Any(item => item.Guid == guid))
        {
            throw new InvalidOperationException($"Remocao falhou: Folder '{plan.FolderName}' ainda existe apos Delete().");
        }

        deleted.Add($"Folder:{plan.FolderName}");
    }

    private static bool IsFolderEmpty(KBModel designModel, Folder folder)
    {
        return !API.GetAll(designModel).Any(item => item.Parent is not null && item.Parent.Guid == folder.Guid)
            && !Procedure.GetAll(designModel).Any(item => item.Parent is not null && item.Parent.Guid == folder.Guid)
            && !SDT.GetAll(designModel).Any(item => item.Parent is not null && item.Parent.Guid == folder.Guid)
            && !WikiFileKBObject.GetAll(designModel).Any(item => item.Parent is not null && item.Parent.Guid == folder.Guid)
            && !Folder.GetAll(designModel).Any(item => item.Guid != folder.Guid && item.Parent is not null && item.Parent.Guid == folder.Guid);
    }
}

internal sealed class ApiPlanGeneratedApiRemovalResult
{
    public ApiPlanGeneratedApiRemovalResult(ApiPlanGeneratedApiRemovalPlan plan, IReadOnlyList<string> deletedItems)
    {
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        DeletedItems = deletedItems ?? throw new ArgumentNullException(nameof(deletedItems));
    }

    public ApiPlanGeneratedApiRemovalPlan Plan { get; }
    public IReadOnlyList<string> DeletedItems { get; }
}
