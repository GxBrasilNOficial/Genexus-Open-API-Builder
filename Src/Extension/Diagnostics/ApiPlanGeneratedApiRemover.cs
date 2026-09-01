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
    public static ApiPlanGeneratedApiRemovalResult Remove(KBModel designModel, Transaction transaction) =>
        Remove(designModel, transaction, progress: null);

    public static ApiPlanGeneratedApiRemovalResult Remove(
        KBModel designModel,
        Transaction transaction,
        ApiPlanBusyProgressSession? progress)
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
        var metadataFile = FindOwnedMetadataFile(designModel, metadataFileName, transaction.Name, kbIndex: null);
        var metadata = ParseMetadata(metadataFile);
        var plan = ApiPlanGeneratedApiRemovalPlan.FromMetadata(metadata, transaction.Name, transaction.Guid.ToString());
        ValidateRemovalTargets(designModel, plan, progress: null, kbIndex: null);

        var total = CountPlannedDeletes(plan);
        var current = 0;
        var deleted = new List<string>();

        // Ordem obrigatoria na IDE:
        // 1) API Object (referencia Procedures)
        // 2) Procedures (tipam SDTs)
        // 3) SDTs proprios na ordem do plano (ListResponse antes de Response)
        progress?.ThrowIfAbortRequested();
        current = ReportDelete(progress, current, total, "API Object", plan.ApiName, () =>
            DeleteApiObject(designModel, plan, deleted));

        foreach (var name in plan.ProcedureNames)
        {
            progress?.ThrowIfAbortRequested();
            current = ReportDelete(progress, current, total, "Procedure", name, () =>
                DeleteSingleProcedure(designModel, name, deleted));
        }

        foreach (var name in plan.OwnSdtNames)
        {
            progress?.ThrowIfAbortRequested();
            current = ReportDelete(progress, current, total, "SDT", name, () =>
                DeleteSingleOwnSdt(designModel, plan, name, deleted));
        }

        progress?.ThrowIfAbortRequested();
        current = ReportDelete(progress, current, total, "File", metadataFile.Name, () =>
            DeleteMetadataFile(designModel, metadataFile, deleted));

        if (plan.FolderWasCreated && !string.IsNullOrWhiteSpace(plan.FolderName))
        {
            progress?.ThrowIfAbortRequested();
            ReportDelete(progress, current, total, "Folder", plan.FolderName!, () =>
                MaybeDeleteFolder(designModel, plan, deleted));
        }

        return new ApiPlanGeneratedApiRemovalResult(plan, deleted);
    }

    public static int CountPlannedDeletes(ApiPlanGeneratedApiRemovalPlan plan)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        var total = 1 + plan.ProcedureNames.Count + plan.OwnSdtNames.Count + 1;
        if (plan.FolderWasCreated && !string.IsNullOrWhiteSpace(plan.FolderName))
        {
            total++;
        }

        return total;
    }

    private static int ReportDelete(
        ApiPlanBusyProgressSession? progress,
        int current,
        int total,
        string kind,
        string name,
        Action deleteAction)
    {
        var next = current + 1;
        progress?.Report($"Removendo {kind}", next, total, name);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        deleteAction();
        sw.Stop();
        progress?.Report($"Removendo {kind}", next, total, name, sw.ElapsedMilliseconds);
        return next;
    }

    public static ApiPlanGeneratedApiRemovalPlan Preview(KBModel designModel, Transaction transaction)
    {
        return Preview(designModel, transaction, progress: null, kbIndex: null);
    }

    public static ApiPlanGeneratedApiRemovalPlan Preview(
        KBModel designModel,
        Transaction transaction,
        ApiPlanBusyProgressSession? progress,
        ApiPlanKbObjectNameIndex? kbIndex)
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
        progress?.Report("Metadata", 0, 0, metadataFileName);
        progress?.PumpAndThrowIfAbortRequested();
        var metadataFile = FindOwnedMetadataFile(designModel, metadataFileName, transaction.Name, kbIndex);
        var metadata = ParseMetadata(metadataFile);
        var plan = ApiPlanGeneratedApiRemovalPlan.FromMetadata(metadata, transaction.Name, transaction.Guid.ToString());
        ValidateRemovalTargets(designModel, plan, progress, kbIndex);
        return plan;
    }

    /// <summary>
    /// Valida ambiguidade e posse de API Object, Procedures e SDTs proprios antes de qualquer Delete().
    /// Ausencia de um alvo listado e aceita (remocao idempotente); ambiguidade ou objeto nao proprio bloqueiam.
    /// </summary>
    internal static void ValidateRemovalTargets(KBModel designModel, ApiPlanGeneratedApiRemovalPlan plan)
    {
        ValidateRemovalTargets(designModel, plan, progress: null, kbIndex: null);
    }

    internal static void ValidateRemovalTargets(
        KBModel designModel,
        ApiPlanGeneratedApiRemovalPlan plan,
        ApiPlanBusyProgressSession? progress,
        ApiPlanKbObjectNameIndex? kbIndex)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        var total = 1 + plan.ProcedureNames.Count + plan.OwnSdtNames.Count;
        var current = 0;
        progress?.ThrowIfAbortRequested();
        current++;
        progress?.Report("Validando", current, total, plan.ApiName);
        progress?.Pump();
        ValidateApiObjectTarget(designModel, plan, beforeAnyDelete: true, kbIndex);
        foreach (var name in plan.ProcedureNames)
        {
            progress?.ThrowIfAbortRequested();
            current++;
            progress?.Report("Validando", current, total, name);
            progress?.Pump();
            ValidateProcedureTarget(designModel, name, beforeAnyDelete: true, kbIndex);
        }

        foreach (var name in plan.OwnSdtNames)
        {
            progress?.ThrowIfAbortRequested();
            current++;
            progress?.Report("Validando", current, total, name);
            progress?.Pump();
            ValidateOwnSdtTarget(designModel, plan, name, beforeAnyDelete: true, kbIndex);
        }
    }

    private static WikiFileKBObject FindOwnedMetadataFile(
        KBModel designModel,
        string metadataFileName,
        string transactionName,
        ApiPlanKbObjectNameIndex? kbIndex)
    {
        var matches = kbIndex is null
            ? WikiFileKBObject.GetAll(designModel)
                .Where(file => string.Equals(file.Name, metadataFileName, StringComparison.OrdinalIgnoreCase))
                .ToArray()
            : kbIndex.FindFiles(metadataFileName).ToArray();

        if (matches.Length == 0)
        {
            throw new InvalidOperationException($"Remocao bloqueada: File de metadata '{metadataFileName}' nao foi encontrado. Nenhuma alteracao foi feita.");
        }

        if (matches.Length > 1)
        {
            throw new InvalidOperationException($"Remocao bloqueada: foram encontrados {matches.Length} Files chamados '{metadataFileName}'. Nenhuma alteracao foi feita.");
        }

        var file = matches[0];
        if (ApiPlanOwnedObjectDescription.IsOwnedMetadataFile(file.Description, metadataFileName, transactionName))
        {
            return file;
        }

        throw new InvalidOperationException($"Remocao bloqueada: File '{metadataFileName}' nao e metadata propria da extensao. Nenhuma alteracao foi feita.");
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
            return ApiPlanMetadataIntegrity.ParseMetadataBytes(bytes);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Remocao bloqueada: File '{file.Name}' possui JSON invalido. Nenhuma alteracao foi feita.", ex);
        }
    }

    private static void ValidateApiObjectTarget(
        KBModel designModel,
        ApiPlanGeneratedApiRemovalPlan plan,
        bool beforeAnyDelete,
        ApiPlanKbObjectNameIndex? kbIndex = null)
    {
        var matches = kbIndex is null
            ? API.GetAll(designModel)
                .Where(item => string.Equals(item.Name, plan.ApiName, StringComparison.OrdinalIgnoreCase))
                .ToArray()
            : kbIndex.FindApis(plan.ApiName).ToArray();
        if (matches.Length == 0)
        {
            return;
        }

        if (matches.Length > 1)
        {
            throw new InvalidOperationException(BuildBlockedMessage(
                $"API Object ambiguo '{plan.ApiName}'",
                beforeAnyDelete));
        }

        var api = matches[0];
        if (!Guid.TryParse(plan.ApiGuid, out var ownershipGuid) || api.Guid != ownershipGuid)
        {
            throw new InvalidOperationException(BuildBlockedMessage(
                $"API Object '{plan.ApiName}' nao corresponde ao Guid da metadata",
                beforeAnyDelete));
        }
    }

    private static void ValidateProcedureTarget(
        KBModel designModel,
        string name,
        bool beforeAnyDelete,
        ApiPlanKbObjectNameIndex? kbIndex = null)
    {
        var matches = kbIndex is null
            ? Procedure.GetAll(designModel)
                .Where(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                .ToArray()
            : kbIndex.FindProcedures(name).ToArray();
        if (matches.Length == 0)
        {
            return;
        }

        if (matches.Length > 1)
        {
            throw new InvalidOperationException(BuildBlockedMessage(
                $"Procedure ambigua '{name}'",
                beforeAnyDelete));
        }

        var procedure = matches[0];
        if (!ApiPlanOwnedObjectDescription.IsOwnedProcedure(procedure.Description, name))
        {
            throw new InvalidOperationException(BuildBlockedMessage(
                $"Procedure '{name}' nao e propria da extensao",
                beforeAnyDelete));
        }
    }

    private static void ValidateOwnSdtTarget(
        KBModel designModel,
        ApiPlanGeneratedApiRemovalPlan plan,
        string name,
        bool beforeAnyDelete,
        ApiPlanKbObjectNameIndex? kbIndex = null)
    {
        if (plan.SharedSdtNamesPreserved.Contains(name, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(BuildBlockedMessage(
                $"tentativa de apagar SDT compartilhado '{name}'",
                beforeAnyDelete));
        }

        var matches = kbIndex is null
            ? SDT.GetAll(designModel)
                .Where(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                .ToArray()
            : kbIndex.FindSdts(name).ToArray();
        if (matches.Length == 0)
        {
            return;
        }

        if (matches.Length > 1)
        {
            throw new InvalidOperationException(BuildBlockedMessage(
                $"SDT ambiguo '{name}'",
                beforeAnyDelete));
        }

        var sdt = matches[0];
        if (!ApiPlanOwnedObjectDescription.IsOwnedSdt(sdt.Description, name))
        {
            throw new InvalidOperationException(BuildBlockedMessage(
                $"SDT '{name}' nao e proprio da extensao",
                beforeAnyDelete));
        }
    }

    private static string BuildBlockedMessage(string reason, bool beforeAnyDelete)
    {
        if (beforeAnyDelete)
        {
            return $"Remocao bloqueada: {reason}. Nenhuma alteracao foi feita.";
        }

        return $"Remocao bloqueada: {reason}. O estado da KB mudou apos o preflight; interrompendo para evitar mais exclusoes.";
    }

    private static void DeleteProcedures(KBModel designModel, ApiPlanGeneratedApiRemovalPlan plan, List<string> deleted)
    {
        foreach (var name in plan.ProcedureNames)
        {
            DeleteSingleProcedure(designModel, name, deleted);
        }
    }

    private static void DeleteSingleProcedure(KBModel designModel, string name, List<string> deleted)
    {
        ValidateProcedureTarget(designModel, name, beforeAnyDelete: false);

        var matches = Procedure.GetAll(designModel)
            .Where(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (matches.Length == 0)
        {
            return;
        }

        var procedure = matches[0];
        var guid = procedure.Guid;
        procedure.Delete();
        if (Procedure.GetAll(designModel).Any(item => item.Guid == guid))
        {
            throw new InvalidOperationException($"Remocao falhou: Procedure '{name}' ainda existe apos Delete().");
        }

        deleted.Add($"Procedure:{name}");
    }

    private static void DeleteApiObject(KBModel designModel, ApiPlanGeneratedApiRemovalPlan plan, List<string> deleted)
    {
        ValidateApiObjectTarget(designModel, plan, beforeAnyDelete: false);

        var matches = API.GetAll(designModel)
            .Where(item => string.Equals(item.Name, plan.ApiName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (matches.Length == 0)
        {
            return;
        }

        var api = matches[0];
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
            DeleteSingleOwnSdt(designModel, plan, name, deleted);
        }
    }

    private static void DeleteSingleOwnSdt(KBModel designModel, ApiPlanGeneratedApiRemovalPlan plan, string name, List<string> deleted)
    {
        ValidateOwnSdtTarget(designModel, plan, name, beforeAnyDelete: false);

        var matches = SDT.GetAll(designModel)
            .Where(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (matches.Length == 0)
        {
            return;
        }

        var sdt = matches[0];
        var guid = sdt.Guid;
        sdt.Delete();
        if (SDT.GetAll(designModel).Any(item => item.Guid == guid))
        {
            throw new InvalidOperationException($"Remocao falhou: SDT '{name}' ainda existe apos Delete().");
        }

        deleted.Add($"SDT:{name}");
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
        var expectedDescription = ApiPlanOwnedObjectDescription.CreateTransactionFolderDescription(plan.FolderName!);
        var legacyDescription = ApiPlanOwnedObjectDescription.CreateLegacyTransactionFolderDescription(plan.TransactionName);
        if (!string.Equals(folder.Description, expectedDescription, StringComparison.Ordinal)
            && !string.Equals(folder.Description, legacyDescription, StringComparison.Ordinal))
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
