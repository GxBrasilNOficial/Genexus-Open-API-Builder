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

    public static ApiPlanSdtWriteResult CreateOrReencounter(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        IReadOnlyCollection<string>? preserveSdtNames,
        ApiPlanKbObjectNameIndex kbIndex,
        System.Action<ApiPlanSdtWriteItemResult>? onSdtWrite = null,
        ApiPlanBusyProgressSession? progress = null)
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

        if (kbIndex is null)
        {
            throw new ArgumentNullException(nameof(kbIndex));
        }

        if (!string.Equals(transaction.Name, apiPlan.TransactionName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Criacao de SDTs bloqueada: o ApiPlan em memoria nao pertence a Transaction selecionada atual. Nenhuma alteracao foi feita.");
        }

        var preserve = new HashSet<string>(preserveSdtNames ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        var generationPlan = ApiPlanSdtGenerationPlanBuilder.Create(apiPlan);
        var planned = generationPlan.SharedSdts.Count + generationPlan.OwnSdts.Count;
        progress?.Report("SDTs", 0, planned, "Preflight");
        progress?.PumpAndThrowIfAbortRequested();
        var preflight = CreatePreflightResult(designModel, generationPlan, kbIndex, progress);
        progress?.PumpAndThrowIfAbortRequested();
        ApiPlanTransactionFolder.Preflight(designModel, transaction, apiPlan);
        progress?.PumpAndThrowIfAbortRequested();
        var sharedFolderWasCreated = preflight.SharedFolder is null;
        var sharedFolder = preflight.SharedFolder ?? CreateSharedFolder(designModel);
        if (sharedFolderWasCreated)
        {
            apiPlan.SharedSdtFolderWasCreated = true;
            kbIndex.RefreshFolders(designModel);
        }

        progress?.PumpAndThrowIfAbortRequested();
        var transactionFolder = ApiPlanTransactionFolder.CreateOrReencounter(designModel, transaction, apiPlan);
        var results = new List<ApiPlanSdtWriteItemResult>();
        var current = 0;

        foreach (var sdt in generationPlan.SharedSdts)
        {
            progress?.ThrowIfAbortRequested();
            current++;
            progress?.Report("SDTs", current, planned, sdt.Name);
            progress?.Pump();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = CreateOrReencounterSdt(designModel, transaction, sharedFolder, sdt, preflight, preserve, progress, kbIndex);
            sw.Stop();
            progress?.Report("SDTs", current, planned, sdt.Name, sw.ElapsedMilliseconds);
            onSdtWrite?.Invoke(result);
            results.Add(result);
        }

        foreach (var sdt in generationPlan.OwnSdts)
        {
            progress?.ThrowIfAbortRequested();
            current++;
            progress?.Report("SDTs", current, planned, sdt.Name);
            progress?.Pump();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = CreateOrReencounterSdt(designModel, transaction, transactionFolder, sdt, preflight, preserve, progress, kbIndex);
            sw.Stop();
            progress?.Report("SDTs", current, planned, sdt.Name, sw.ElapsedMilliseconds);
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

    internal static IReadOnlyList<string> PlannedSdtNames(ApiPlan apiPlan)
    {
        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        var generationPlan = ApiPlanSdtGenerationPlanBuilder.Create(apiPlan);
        return generationPlan.SharedSdts
            .Concat(generationPlan.OwnSdts)
            .Select(item => item.Name)
            .ToArray();
    }

    internal static void Preflight(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        ApiPlanKbObjectNameIndex kbIndex)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        if (kbIndex is null)
        {
            throw new ArgumentNullException(nameof(kbIndex));
        }

        CreatePreflightResult(
            designModel,
            ApiPlanSdtGenerationPlanBuilder.Create(apiPlan),
            kbIndex);
        ApiPlanTransactionFolder.Preflight(designModel, transaction, apiPlan);
    }

    private static ApiPlanSdtPreflightResult CreatePreflightResult(
        KBModel designModel,
        ApiPlanSdtGenerationPlan generationPlan,
        ApiPlanKbObjectNameIndex kbIndex,
        ApiPlanBusyProgressSession? progress = null)
    {
        var allDefinitions = generationPlan.SharedSdts.Concat(generationPlan.OwnSdts).ToArray();
        var plannedNames = new HashSet<string>(allDefinitions.Select(item => item.Name), StringComparer.OrdinalIgnoreCase);
        var existingByName = new Dictionary<string, SDT>(StringComparer.OrdinalIgnoreCase);

        progress?.PumpAndThrowIfAbortRequested();
        var folders = kbIndex.FindFolders(SharedFolderName);
        if (folders.Count > 1)
        {
            throw new InvalidOperationException($"Criacao de SDTs bloqueada: foram encontrados {folders.Count} Folders chamados '{SharedFolderName}'. Nenhuma alteracao foi feita.");
        }

        var preflightIndex = 0;
        foreach (var definition in allDefinitions)
        {
            preflightIndex++;
            progress?.Report("SDTs", preflightIndex, allDefinitions.Length, $"Preflight {definition.Name}");
            progress?.PumpAndThrowIfAbortRequested();
            if (definition.Members.Count == 0 &&
                !string.Equals(definition.Kind, "ListFilters", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Criacao de SDT bloqueada: o SDT '{definition.Name}' nao tem membros. Nenhuma alteracao foi feita.");
            }

            ValidateSdtDefinitionTypes(definition, plannedNames, kbIndex);
            var existingCount = kbIndex.GetSdtCount(definition.Name);

            if (existingCount > 1)
            {
                throw new InvalidOperationException($"Criacao de SDT bloqueada: foram encontrados {existingCount} SDTs chamados '{definition.Name}'. Nenhuma alteracao foi feita.");
            }

            if (existingCount == 1 && kbIndex.TryGetSingleSdt(definition.Name, out var existingSdt))
            {
                if (!ApiPlanOwnedObjectDescription.IsOwnedSdt(existingSdt.Description, definition.Name))
                {
                    throw new InvalidOperationException($"Criacao de SDT bloqueada: ja existe SDT externo ou incompativel chamado '{definition.Name}'. Nenhuma alteracao foi feita.");
                }

                existingByName.Add(definition.Name, existingSdt);
            }
        }

        return new ApiPlanSdtPreflightResult(folders.Count == 1 ? folders[0] : null, existingByName);
    }

    private static void ValidateSdtDefinitionTypes(
        ApiPlanSdtDefinition definition,
        HashSet<string> plannedNames,
        ApiPlanKbObjectNameIndex kbIndex)
    {
        foreach (var member in definition.Members.Where(item => item.Name.IndexOf(".", StringComparison.Ordinal) < 0))
        {
            if (member.IsCollection || IsSdtReference(member.DataType))
            {
                if (!plannedNames.Contains(member.DataType) && !kbIndex.OwnedSdtExists(member.DataType))
                {
                    throw new InvalidOperationException($"Criacao de SDT bloqueada: tipo SDT requerido nao foi validado antes da escrita para membro '{member.Name}': '{member.DataType}'. Nenhuma alteracao foi feita.");
                }

                continue;
            }

            if (IsAttributeReference(member.DataType))
            {
                EnsureAttributeExists(kbIndex, member.Name, member.DataType);
                continue;
            }

            ResolveDbType(member.DataType);
        }
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
        ISet<string> preserveSdtNames,
        ApiPlanBusyProgressSession? progress,
        ApiPlanKbObjectNameIndex kbIndex)
    {
        if (preflight.ExistingSdtsByName.TryGetValue(definition.Name, out var existingSdt))
        {
            var preserveThis = preserveSdtNames.Contains(definition.Name);
            var needsParentMove = NeedsParentMove(existingSdt, targetFolder);
            if (needsParentMove && targetFolder is not null)
            {
                existingSdt.Parent = targetFolder;
            }

            var canSkipRewrite = preserveThis || MatchesPlannedSdtStructure(existingSdt, definition, kbIndex);
            if (!canSkipRewrite)
            {
                ConfigureSdt(designModel, existingSdt, definition, kbIndex);
            }

            progress?.ThrowIfAbortRequested();
            if (!canSkipRewrite || needsParentMove)
            {
                existingSdt.Save();
            }

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

        ConfigureSdt(designModel, sdt, definition, kbIndex);
        progress?.ThrowIfAbortRequested();
        sdt.Save();

        var persisted = SDT.Get(designModel, sdt.Guid);
        return new ApiPlanSdtWriteItemResult(definition.BacklogId, definition.Kind, definition.Name, definition.Scope, ApiPlanSdtWriteStatus.Created, persisted.Guid);
    }

    private static void ConfigureSdt(KBModel designModel, SDT sdt, ApiPlanSdtDefinition definition, ApiPlanKbObjectNameIndex kbIndex)
    {
        var root = sdt.SDTStructure.Root;
        root.Items.Clear();
        root.Name = definition.Name;

        foreach (var member in definition.Members.Where(item => item.Name.IndexOf(".", StringComparison.Ordinal) < 0))
        {
            AddMember(designModel, root, member, kbIndex);
        }
    }

    private static bool NeedsParentMove(SDT sdt, Folder? targetFolder)
    {
        if (targetFolder is null)
        {
            return false;
        }

        return sdt.Parent is null || sdt.Parent.Guid != targetFolder.Guid;
    }

    /// <summary>
    /// Compara o SDT persistido com o plano. Não exige Length/Decimals/Type
    /// em membro AttributeBasedOn: o especificador da IDE troca o seed CHARACTER
    /// pelo tipo do atributo sem isso ser divergência de contrato.
    /// </summary>
    private static bool MatchesPlannedSdtStructure(
        SDT sdt,
        ApiPlanSdtDefinition definition,
        ApiPlanKbObjectNameIndex kbIndex)
    {
        if (!string.Equals(sdt.SDTStructure.Root.Name, definition.Name, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var planned = definition.Members
            .Where(item => item.Name.IndexOf(".", StringComparison.Ordinal) < 0)
            .ToArray();
        var actualByName = new Dictionary<string, SDTItem>(StringComparer.OrdinalIgnoreCase);
        foreach (SDTItem item in sdt.SDTStructure.Root.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Name) || actualByName.ContainsKey(item.Name))
            {
                continue;
            }

            actualByName.Add(item.Name, item);
        }

        if (actualByName.Count != planned.Length)
        {
            return false;
        }

        foreach (var member in planned)
        {
            if (!actualByName.TryGetValue(member.Name, out var item))
            {
                return false;
            }

            if (!MemberMatchesItem(item, member, kbIndex))
            {
                return false;
            }
        }

        return true;
    }

    private static bool MemberMatchesItem(SDTItem item, ApiPlanSdtMember member, ApiPlanKbObjectNameIndex kbIndex)
    {
        if (item.IsCollection != member.IsCollection)
        {
            return false;
        }

        if (ShouldSerializeAsJsonNull(member))
        {
            if (!string.Equals(ReadPropertyString(item, "idJsonInclude"), "idJsonJsonNull", StringComparison.Ordinal))
            {
                return false;
            }
        }

        if (member.IsCollection || IsSdtReference(member.DataType))
        {
            if (item.Type != eDBType.GX_SDT)
            {
                return false;
            }

            if (!SameSdtTypeName(ReadSdtItemTypeName(item), member.DataType))
            {
                return false;
            }

            if (member.IsCollection && !string.IsNullOrWhiteSpace(member.CollectionItemType) &&
                !string.Equals(item.CollectionItemName, member.CollectionItemType, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        if (IsAttributeReference(member.DataType))
        {
            var expectedName = ExtractAttributeName(member.DataType);
            return item.AttributeBasedOn is not null &&
                string.Equals(item.AttributeBasedOn.Name, expectedName, StringComparison.OrdinalIgnoreCase) &&
                kbIndex.TryGetSingleAttribute(expectedName, out _);
        }

        return item.AttributeBasedOn is null &&
            item.Type == ResolveDbType(member.DataType) &&
            item.Length == Math.Max(member.Length, 0) &&
            item.Decimals == Math.Max(member.Decimals, 0);
    }

    private static string ReadSdtItemTypeName(SDTItem item)
    {
        var customType = ReadPropertyString(item, "ATTCUSTOMTYPE");
        if (!string.IsNullOrWhiteSpace(customType))
        {
            return customType.Trim();
        }

        return item.BasedOn?.Name?.ToString() ?? string.Empty;
    }

    private static string ReadPropertyString(SDTItem item, string propertyName)
    {
        var value = item.GetPropertyValue(propertyName);
        return value?.ToString() ?? string.Empty;
    }

    private static bool SameSdtTypeName(string actual, string planned)
    {
        if (string.IsNullOrWhiteSpace(actual) || string.IsNullOrWhiteSpace(planned))
        {
            return false;
        }

        if (string.Equals(actual, planned, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return actual.EndsWith("." + planned, StringComparison.OrdinalIgnoreCase);
    }

    private static void AddMember(KBModel designModel, SDTLevel root, ApiPlanSdtMember member, ApiPlanKbObjectNameIndex kbIndex)
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
            item.AttributeBasedOn = EnsureAttributeExists(kbIndex, member.Name, member.DataType);
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

    private static Artech.Genexus.Common.Objects.Attribute EnsureAttributeExists(
        ApiPlanKbObjectNameIndex kbIndex,
        string memberName,
        string dataType)
    {
        var attributeName = ExtractAttributeName(dataType);
        if (!kbIndex.TryGetSingleAttribute(attributeName, out var attribute))
        {
            throw new InvalidOperationException($"Criacao de SDT bloqueada: atributo base requerido para membro '{memberName}' nao foi reencontrado com seguranca: '{attributeName}'. Nenhuma alteracao foi feita.");
        }

        return attribute;
    }

    private static string ExtractAttributeName(string dataType)
    {
        const string prefix = "Attribute:";
        return dataType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? dataType.Substring(prefix.Length).Trim()
            : dataType.Trim();
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
