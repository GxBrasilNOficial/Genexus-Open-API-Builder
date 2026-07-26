using System;
using System.Collections.Generic;
using System.Linq;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common;
using Artech.Genexus.Common.Objects;
using Artech.Genexus.Common.Parts.SDT;
using Artech.Genexus.Common.Types;
using GenexusOpenApiBuilder.Extension.Domain;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

internal static class ApiPlanSdtWriter
{
    private const string OwnedDescriptionPrefix = "Genexus Open API Builder B040-B046 SDT";
    private const string SharedFolderName = "GxOpenAPI";

    public static ApiPlanSdtWriteResult CreateOrReencounter(KBModel designModel, Transaction transaction, ApiPlan apiPlan)
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

        if (!string.Equals(transaction.Name, apiPlan.TransactionName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Criacao de SDTs bloqueada: o ApiPlan em memoria nao pertence a Transaction selecionada atual. Nenhuma alteracao foi feita.");
        }

        var generationPlan = ApiPlanSdtGenerationPlanBuilder.Create(apiPlan);
        var preflight = Preflight(designModel, generationPlan);
        ApiPlanTransactionFolder.Preflight(designModel, apiPlan);
        var sharedFolder = preflight.SharedFolder ?? CreateSharedFolder(designModel);
        var transactionFolder = ApiPlanTransactionFolder.CreateOrReencounter(designModel, transaction, apiPlan);
        var results = new List<ApiPlanSdtWriteItemResult>();

        foreach (var sdt in generationPlan.SharedSdts)
        {
            results.Add(CreateOrReencounterSdt(designModel, transaction, sharedFolder, sdt, preflight));
        }

        foreach (var sdt in generationPlan.OwnSdts)
        {
            results.Add(CreateOrReencounterSdt(designModel, transaction, transactionFolder, sdt, preflight));
        }

        return new ApiPlanSdtWriteResult(
            generationPlan.OwnSdts.Count,
            generationPlan.SharedSdts.Count,
            results.Count(item => item.Status == ApiPlanSdtWriteStatus.Created),
            results.Count(item => item.Status == ApiPlanSdtWriteStatus.Reencountered),
            transactionFolder.Name,
            transactionFolder.Guid,
            results);
    }

    internal static string CreateOwnedDescriptionFor(string backlogId, string kind)
    {
        return $"{OwnedDescriptionPrefix} - {backlogId} - {kind}";
    }

    private static ApiPlanSdtPreflightResult Preflight(KBModel designModel, ApiPlanSdtGenerationPlan generationPlan)
    {
        var allDefinitions = generationPlan.SharedSdts.Concat(generationPlan.OwnSdts).ToArray();
        var plannedNames = new HashSet<string>(allDefinitions.Select(item => item.Name), StringComparer.OrdinalIgnoreCase);
        var existingByName = new Dictionary<string, SDT>(StringComparer.OrdinalIgnoreCase);

        var folders = Folder.GetAll(designModel)
            .Where(folder => string.Equals(folder.Name, SharedFolderName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (folders.Length > 1)
        {
            throw new InvalidOperationException($"Criacao de SDTs bloqueada: foram encontrados {folders.Length} Folders chamados '{SharedFolderName}'. Nenhuma alteracao foi feita.");
        }

        foreach (var definition in allDefinitions)
        {
            ValidateSdtDefinitionTypes(designModel, definition, plannedNames);
            var existing = SDT.GetAll(designModel)
                .Where(sdt => string.Equals(sdt.Name, definition.Name, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (existing.Length > 1)
            {
                throw new InvalidOperationException($"Criacao de SDT bloqueada: foram encontrados {existing.Length} SDTs chamados '{definition.Name}'. Nenhuma alteracao foi feita.");
            }

            var description = CreateOwnedDescription(definition);
            if (existing.Length == 1)
            {
                var existingSdt = existing[0];
                if (!string.Equals(existingSdt.Description, description, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Criacao de SDT bloqueada: ja existe SDT externo ou incompativel chamado '{definition.Name}'. Nenhuma alteracao foi feita.");
                }

                existingByName.Add(definition.Name, existingSdt);
            }
        }

        return new ApiPlanSdtPreflightResult(folders.SingleOrDefault(), existingByName);
    }

    private static void ValidateSdtDefinitionTypes(KBModel designModel, ApiPlanSdtDefinition definition, HashSet<string> plannedNames)
    {
        if (string.Equals(definition.Kind, "SharedErrorResponse", StringComparison.Ordinal))
        {
            foreach (var member in definition.Members.Where(item => item.Name.IndexOf(".", StringComparison.Ordinal) >= 0 || !string.Equals(item.Name, "Errors", StringComparison.Ordinal)))
            {
                ResolveDbType(member.DataType);
            }

            return;
        }

        foreach (var member in definition.Members.Where(item => item.Name.IndexOf(".", StringComparison.Ordinal) < 0))
        {
            if (member.IsCollection || IsSdtReference(member.DataType))
            {
                if (!plannedNames.Contains(member.DataType) && !OwnedSdtExists(designModel, member.DataType))
                {
                    throw new InvalidOperationException($"Criacao de SDT bloqueada: tipo SDT requerido nao foi validado antes da escrita para membro '{member.Name}': '{member.DataType}'. Nenhuma alteracao foi feita.");
                }

                continue;
            }

            ResolveDbType(member.DataType);
        }
    }

    private static bool OwnedSdtExists(KBModel designModel, string name)
    {
        return SDT.GetAll(designModel)
            .Any(sdt => string.Equals(sdt.Name, name, StringComparison.OrdinalIgnoreCase) &&
                        (sdt.Description ?? string.Empty).StartsWith(OwnedDescriptionPrefix, StringComparison.Ordinal));
    }

    private static Folder CreateSharedFolder(KBModel designModel)
    {
        var folder = new Folder(designModel, SharedFolderName)
        {
            Description = "Genexus Open API Builder shared SDTs folder",
        };
        folder.Save();
        return folder;
    }

    private static ApiPlanSdtWriteItemResult CreateOrReencounterSdt(KBModel designModel, Transaction transaction, Folder? targetFolder, ApiPlanSdtDefinition definition, ApiPlanSdtPreflightResult preflight)
    {
        if (preflight.ExistingSdtsByName.TryGetValue(definition.Name, out var existingSdt))
        {
            if (targetFolder is not null)
            {
                existingSdt.Parent = targetFolder;
                existingSdt.Save();
            }

            return new ApiPlanSdtWriteItemResult(definition.BacklogId, definition.Kind, definition.Name, definition.Scope, ApiPlanSdtWriteStatus.Reencountered, existingSdt.Guid);
        }

        var sdt = new SDT(designModel)
        {
            Name = definition.Name,
            Description = CreateOwnedDescription(definition),
        };

        if (targetFolder is not null)
        {
            sdt.Parent = targetFolder;
        }
        else if (transaction.Module is not null)
        {
            sdt.Module = transaction.Module;
        }

        ConfigureSdt(designModel, sdt, definition);
        sdt.Save();

        var persisted = SDT.Get(designModel, sdt.Guid);
        return new ApiPlanSdtWriteItemResult(definition.BacklogId, definition.Kind, definition.Name, definition.Scope, ApiPlanSdtWriteStatus.Created, persisted.Guid);
    }

    private static string CreateOwnedDescription(ApiPlanSdtDefinition definition)
    {
        return CreateOwnedDescriptionFor(definition.BacklogId, definition.Kind);
    }

    private static void ConfigureSdt(KBModel designModel, SDT sdt, ApiPlanSdtDefinition definition)
    {
        var root = sdt.SDTStructure.Root;
        root.Name = definition.Name;

        if (string.Equals(definition.Kind, "SharedErrorResponse", StringComparison.Ordinal))
        {
            AddBuiltInMember(root, "Code", "VarChar", 64, 0);
            AddBuiltInMember(root, "Message", "VarChar", 256, 0);
            var errors = root.AddLevel("Errors", true);
            errors.CollectionItemName = "Error";
            AddBuiltInMember(errors, "Code", "VarChar", 64, 0);
            AddBuiltInMember(errors, "Message", "VarChar", 256, 0);
            AddBuiltInMember(errors, "Field", "VarChar", 128, 0);
            return;
        }

        foreach (var member in definition.Members.Where(item => item.Name.IndexOf(".", StringComparison.Ordinal) < 0))
        {
            AddMember(designModel, root, member);
        }
    }

    private static void AddMember(KBModel designModel, SDTLevel root, ApiPlanSdtMember member)
    {
        if (member.IsCollection || IsSdtReference(member.DataType))
        {
            var item = root.AddItem(member.Name, eDBType.GX_SDT);
            if (!DataType.ParseInto(designModel, member.DataType, item))
            {
                throw new InvalidOperationException($"Tipo SDT nao resolvido para membro '{member.Name}': '{member.DataType}'. Nenhuma alteracao foi feita.");
            }

            item.IsCollection = member.IsCollection;
            if (member.IsCollection && !string.IsNullOrWhiteSpace(member.CollectionItemType))
            {
                item.CollectionItemName = member.CollectionItemType;
            }

            return;
        }

        AddBuiltInMember(root, member.Name, member.DataType, member.Length, member.Decimals);
    }

    private static void AddBuiltInMember(SDTLevel root, string name, string dataType, int length, int decimals)
    {
        root.AddItem(name, ResolveDbType(dataType), Math.Max(length, 0), Math.Max(decimals, 0));
    }

    private static bool IsSdtReference(string dataType)
    {
        return dataType.StartsWith("sdt", StringComparison.OrdinalIgnoreCase);
    }

    private static eDBType ResolveDbType(string dataType)
    {
        if (string.Equals(dataType, "Numeric", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "NUMERIC", StringComparison.OrdinalIgnoreCase))
        {
            return eDBType.NUMERIC;
        }

        if (string.Equals(dataType, "VarChar", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "VARCHAR", StringComparison.OrdinalIgnoreCase))
        {
            return eDBType.VARCHAR;
        }

        if (string.Equals(dataType, "Character", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "CHARACTER", StringComparison.OrdinalIgnoreCase))
        {
            return eDBType.CHARACTER;
        }

        if (string.Equals(dataType, "Date", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "DATE", StringComparison.OrdinalIgnoreCase))
        {
            return eDBType.DATE;
        }

        if (string.Equals(dataType, "DateTime", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "DATETIME", StringComparison.OrdinalIgnoreCase))
        {
            return eDBType.DATETIME;
        }

        if (string.Equals(dataType, "Boolean", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "BOOLEAN", StringComparison.OrdinalIgnoreCase))
        {
            return eDBType.Boolean;
        }

        if (string.Equals(dataType, "Guid", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "GUID", StringComparison.OrdinalIgnoreCase))
        {
            return eDBType.GUID;
        }

        if (string.Equals(dataType, "LongVarChar", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "LONGVARCHAR", StringComparison.OrdinalIgnoreCase))
        {
            return eDBType.LONGVARCHAR;
        }

        throw new InvalidOperationException($"Tipo de dado ainda nao suportado para criacao de SDT: '{dataType}'. Nenhuma alteracao foi feita.");
    }
}

internal static class ApiPlanSdtWriteStatus
{
    public const string Created = "Created";
    public const string Reencountered = "Reencountered";
}

internal sealed class ApiPlanSdtPreflightResult
{
    public ApiPlanSdtPreflightResult(Folder? sharedFolder, IReadOnlyDictionary<string, SDT> existingSdtsByName)
    {
        SharedFolder = sharedFolder;
        ExistingSdtsByName = existingSdtsByName ?? throw new ArgumentNullException(nameof(existingSdtsByName));
    }

    public Folder? SharedFolder { get; }

    public IReadOnlyDictionary<string, SDT> ExistingSdtsByName { get; }
}

internal sealed class ApiPlanSdtWriteResult
{
    public ApiPlanSdtWriteResult(int plannedOwnSdts, int plannedSharedSdts, int createdSdts, int reencounteredSdts, string transactionFolderName, Guid transactionFolderGuid, IReadOnlyList<ApiPlanSdtWriteItemResult> items)
    {
        PlannedOwnSdts = plannedOwnSdts;
        PlannedSharedSdts = plannedSharedSdts;
        CreatedSdts = createdSdts;
        ReencounteredSdts = reencounteredSdts;
        TransactionFolderName = transactionFolderName ?? throw new ArgumentNullException(nameof(transactionFolderName));
        TransactionFolderGuid = transactionFolderGuid;
        Items = items ?? throw new ArgumentNullException(nameof(items));
    }

    public int PlannedOwnSdts { get; }
    public int PlannedSharedSdts { get; }
    public int CreatedSdts { get; }
    public int ReencounteredSdts { get; }
    public string TransactionFolderName { get; }
    public Guid TransactionFolderGuid { get; }
    public IReadOnlyList<ApiPlanSdtWriteItemResult> Items { get; }
}

internal sealed class ApiPlanSdtWriteItemResult
{
    public ApiPlanSdtWriteItemResult(string backlogId, string kind, string name, string scope, string status, Guid guid)
    {
        BacklogId = backlogId ?? throw new ArgumentNullException(nameof(backlogId));
        Kind = kind ?? throw new ArgumentNullException(nameof(kind));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        Status = status ?? throw new ArgumentNullException(nameof(status));
        Guid = guid;
    }

    public string BacklogId { get; }
    public string Kind { get; }
    public string Name { get; }
    public string Scope { get; }
    public string Status { get; }
    public Guid Guid { get; }
}
