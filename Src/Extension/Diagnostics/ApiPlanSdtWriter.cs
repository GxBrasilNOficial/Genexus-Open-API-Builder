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
    internal const string SharedFolderName = "GxOpenAPI";

    public static ApiPlanSdtWriteResult CreateOrReencounter(KBModel designModel, Transaction transaction, ApiPlan apiPlan)
    {
        return CreateOrReencounter(designModel, transaction, apiPlan, preserveSdtNames: null);
    }

    public static ApiPlanSdtWriteResult CreateOrReencounter(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        IReadOnlyCollection<string>? preserveSdtNames,
        System.Action<ApiPlanSdtWriteItemResult>? onSdtWrite = null)
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

        var preserve = new HashSet<string>(preserveSdtNames ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        var generationPlan = ApiPlanSdtGenerationPlanBuilder.Create(apiPlan);
        var preflight = CreatePreflightResult(designModel, generationPlan);
        ApiPlanTransactionFolder.Preflight(designModel, transaction, apiPlan);
        var sharedFolderWasCreated = preflight.SharedFolder is null;
        var sharedFolder = preflight.SharedFolder ?? CreateSharedFolder(designModel);
        if (sharedFolderWasCreated)
        {
            apiPlan.SharedSdtFolderWasCreated = true;
        }

        var transactionFolder = ApiPlanTransactionFolder.CreateOrReencounter(designModel, transaction, apiPlan);
        var results = new List<ApiPlanSdtWriteItemResult>();

        foreach (var sdt in generationPlan.SharedSdts)
        {
            var result = CreateOrReencounterSdt(designModel, transaction, sharedFolder, sdt, preflight, preserve);
            onSdtWrite?.Invoke(result);
            results.Add(result);
        }

        foreach (var sdt in generationPlan.OwnSdts)
        {
            var result = CreateOrReencounterSdt(designModel, transaction, transactionFolder, sdt, preflight, preserve);
            onSdtWrite?.Invoke(result);
            results.Add(result);
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

    internal static string CreateOwnedDescriptionFor(string objectName) =>
        ApiPlanOwnedObjectDescription.Create(objectName);

    internal static void Preflight(KBModel designModel, Transaction transaction, ApiPlan apiPlan)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        CreatePreflightResult(designModel, ApiPlanSdtGenerationPlanBuilder.Create(apiPlan));
        ApiPlanTransactionFolder.Preflight(designModel, transaction, apiPlan);
    }

    private static ApiPlanSdtPreflightResult CreatePreflightResult(KBModel designModel, ApiPlanSdtGenerationPlan generationPlan)
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

            if (existing.Length == 1)
            {
                var existingSdt = existing[0];
                if (!ApiPlanOwnedObjectDescription.IsOwnedSdt(existingSdt.Description, definition.Name))
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

            if (IsAttributeReference(member.DataType))
            {
                EnsureAttributeExists(designModel, member.Name, member.DataType);
                continue;
            }

            ResolveDbType(member.DataType);
        }
    }

    private static bool OwnedSdtExists(KBModel designModel, string name)
    {
        return SDT.GetAll(designModel)
            .Any(sdt => string.Equals(sdt.Name, name, StringComparison.OrdinalIgnoreCase) &&
                        ApiPlanOwnedObjectDescription.IsOwnedSdt(sdt.Description, name));
    }

    private static Folder CreateSharedFolder(KBModel designModel)
    {
        var folder = new Folder(designModel, SharedFolderName)
        {
            Description = ApiPlanOwnedObjectDescription.Create(SharedFolderName),
        };
        folder.Save();
        return folder;
    }

    private static ApiPlanSdtWriteItemResult CreateOrReencounterSdt(
        KBModel designModel,
        Transaction transaction,
        Folder? targetFolder,
        ApiPlanSdtDefinition definition,
        ApiPlanSdtPreflightResult preflight,
        ISet<string> preserveSdtNames)
    {
        if (preflight.ExistingSdtsByName.TryGetValue(definition.Name, out var existingSdt))
        {
            if (targetFolder is not null)
            {
                existingSdt.Parent = targetFolder;
            }

            if (!preserveSdtNames.Contains(definition.Name))
            {
                ConfigureSdt(designModel, existingSdt, definition);
            }

            existingSdt.Save();

            return new ApiPlanSdtWriteItemResult(definition.BacklogId, definition.Kind, definition.Name, definition.Scope, ApiPlanSdtWriteStatus.Reencountered, existingSdt.Guid);
        }

        var sdt = new SDT(designModel)
        {
            Name = definition.Name,
            Description = ApiPlanOwnedObjectDescription.Create(definition.Name),
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

    private static void ConfigureSdt(KBModel designModel, SDT sdt, ApiPlanSdtDefinition definition)
    {
        var root = sdt.SDTStructure.Root;
        root.Items.Clear();
        root.Name = definition.Name;

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
            ConfigureJsonNullSerialization(item, member);
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

        if (IsAttributeReference(member.DataType))
        {
            var item = root.AddItem(member.Name, eDBType.CHARACTER, Math.Max(member.Length, 0), Math.Max(member.Decimals, 0));
            ConfigureJsonNullSerialization(item, member);
            item.AttributeBasedOn = EnsureAttributeExists(designModel, member.Name, member.DataType);
            return;
        }

        AddBuiltInMember(root, member.Name, member.DataType, member.Length, member.Decimals, ShouldSerializeAsJsonNull(member));
    }

    private static void AddBuiltInMember(SDTLevel root, string name, string dataType, int length, int decimals, bool serializeAsJsonNull = false)
    {
        var item = root.AddItem(name, ResolveDbType(dataType), Math.Max(length, 0), Math.Max(decimals, 0));
        if (serializeAsJsonNull)
        {
            item.SetPropertyValue("idJsonInclude", "idJsonJsonNull");
        }
    }

    private static void ConfigureJsonNullSerialization(SDTItem item, ApiPlanSdtMember member)
    {
        if (ShouldSerializeAsJsonNull(member))
        {
            item.SetPropertyValue("idJsonInclude", "idJsonJsonNull");
        }
    }

    private static bool ShouldSerializeAsJsonNull(ApiPlanSdtMember member)
    {
        return member.IsNullable && string.Equals(member.Source, "ListFilters", StringComparison.Ordinal);
    }

    private static bool IsSdtReference(string dataType)
    {
        return dataType.StartsWith("sdt", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAttributeReference(string dataType)
    {
        return dataType.StartsWith("Attribute:", StringComparison.OrdinalIgnoreCase);
    }

    private static Artech.Genexus.Common.Objects.Attribute EnsureAttributeExists(KBModel model, string memberName, string dataType)
    {
        const string prefix = "Attribute:";
        var attributeName = dataType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? dataType.Substring(prefix.Length).Trim()
            : dataType.Trim();
        var matches = Artech.Genexus.Common.Objects.Attribute.GetAll(model)
            .Where(attribute => string.Equals(attribute.Name, attributeName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (matches.Length != 1)
        {
            throw new InvalidOperationException($"Criacao de SDT bloqueada: atributo base requerido para membro '{memberName}' nao foi reencontrado com seguranca: '{attributeName}'. Nenhuma alteracao foi feita.");
        }

        return matches[0];
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

        if (string.Equals(dataType, "Bitmap", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "BITMAP", StringComparison.OrdinalIgnoreCase))
        {
            return eDBType.BITMAP;
        }

        if (string.Equals(dataType, "Binary", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "BINARY", StringComparison.OrdinalIgnoreCase))
        {
            return eDBType.BINARY;
        }

        if (string.Equals(dataType, "BinaryFile", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "BINARYFILE", StringComparison.OrdinalIgnoreCase))
        {
            return eDBType.BINARYFILE;
        }

        if (string.Equals(dataType, "Geography", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "GEOGRAPHY", StringComparison.OrdinalIgnoreCase))
        {
            return eDBType.GEOGRAPHY;
        }

        if (string.Equals(dataType, "GeoPoint", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "GEOPOINT", StringComparison.OrdinalIgnoreCase))
        {
            return eDBType.GEOPOINT;
        }

        if (string.Equals(dataType, "GeoPolygon", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "GEOPOLYGON", StringComparison.OrdinalIgnoreCase))
        {
            return eDBType.GEOPOLYGON;
        }

        if (string.Equals(dataType, "GeoLine", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "GEOLINE", StringComparison.OrdinalIgnoreCase))
        {
            return eDBType.GEOLINE;
        }

        if (string.Equals(dataType, "Video", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "VIDEO", StringComparison.OrdinalIgnoreCase))
        {
            return eDBType.VIDEO;
        }

        if (string.Equals(dataType, "Audio", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "AUDIO", StringComparison.OrdinalIgnoreCase))
        {
            return eDBType.AUDIO;
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
