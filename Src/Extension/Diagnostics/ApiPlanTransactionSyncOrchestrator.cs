#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;
using Artech.Genexus.Common.Parts.SDT;
using Artech.Genexus.Common.Wiki;
using GenexusOpenApiBuilder.Extension.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

internal static class ApiPlanTransactionSyncOrchestrator
{
    public static ApiPlanTransactionSyncPreview Preview(KBModel designModel, Transaction transaction)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        var metadataFile = FindOwnedMetadataFile(designModel, $"api{transaction.Name}_Metadata", transaction.Name);
        var metadata = ParseMetadata(metadataFile);
        RequireOwnership(metadata, transaction);
        var metadataStructure = ReadMetadataStructure(metadata);
        var snapshot = PrototypeWizardContractReader.Read(transaction);
        ApiPlanLevel? currentHierarchicalRoot = null;
        if (ApiPlanMetadataLevelsCodec.HasHierarchicalLevels(metadata))
        {
            currentHierarchicalRoot = TransactionStructureReader.Read(transaction).RootLevel;
        }

        var currentStructure = BuildCurrentStructure(currentHierarchicalRoot, metadata, snapshot);
        var diff = ApiPlanTransactionSyncComparer.Compare(metadataStructure, currentStructure);
        var sdtConflicts = DetectSdtConflicts(designModel, metadata);
        return new ApiPlanTransactionSyncPreview(
            transaction.Name,
            metadata,
            metadataFile,
            snapshot,
            diff,
            sdtConflicts,
            currentHierarchicalRoot);
    }

    private static IReadOnlyList<ApiPlanTransactionSyncAttributeSnapshot> ReadMetadataStructure(JObject metadata)
    {
        if (ApiPlanMetadataLevelsCodec.HasHierarchicalLevels(metadata))
        {
            var root = ApiPlanMetadataLevelsCodec.TryReadRoot(metadata)
                ?? throw new InvalidOperationException("Metadata hierárquica sem levels legível. Regenere a API pelo Wizard antes de sincronizar.");
            return ApiPlanMetadataLevelsCodec.FlattenToSyncSnapshots(root);
        }

        return ApiPlanTransactionSyncComparer.ReadStructure(metadata);
    }

    private static IReadOnlyList<ApiPlanTransactionSyncAttributeSnapshot> BuildCurrentStructure(
        ApiPlanLevel? currentHierarchicalRoot,
        JObject metadata,
        PrototypeWizardContractSnapshot snapshot)
    {
        if (currentHierarchicalRoot is not null && ApiPlanMetadataLevelsCodec.HasHierarchicalLevels(metadata))
        {
            return ApiPlanMetadataLevelsCodec.FlattenToSyncSnapshots(currentHierarchicalRoot);
        }

        return snapshot.Attributes.Select(ToSyncSnapshot).ToArray();
    }

    public static PrototypeWizardFlowSelection BuildSelection(
        ApiPlanTransactionSyncPreview preview,
        ApiPlanTransactionSyncChoices choices)
    {
        if (preview is null)
        {
            throw new ArgumentNullException(nameof(preview));
        }

        if (choices is null)
        {
            throw new ArgumentNullException(nameof(choices));
        }

        if (choices.Cancel)
        {
            throw new InvalidOperationException("Selecao de sincronizacao cancelada.");
        }

        var metadata = preview.Metadata;
        var attributesByGuid = preview.Snapshot.Attributes.ToDictionary(item => item.AttributeGuid, StringComparer.OrdinalIgnoreCase);
        var createFields = ResolveSelectedFieldNames(metadata, "fields.createRequest", attributesByGuid, choices, "CreateRequest", preview);
        var updateFields = ResolveSelectedFieldNames(metadata, "fields.updateRequest", attributesByGuid, choices, "UpdateRequest", preview);
        var responseFields = ResolveSelectedFieldNames(metadata, "fields.response", attributesByGuid, choices, "Response", preview);
        var listFilters = ResolveSelectedFieldNames(metadata, "fields.listFilters", attributesByGuid, choices, "ListFilters", preview, listFiltersMode: true);

        var services = ((JArray?)metadata.SelectToken("services") ?? new JArray())
            .Select(token => token["name"]?.Value<string>() ?? string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();
        if (services.Length == 0)
        {
            throw new InvalidOperationException("Metadata sem servicos para reconstruir o ApiPlan na sincronizacao.");
        }

        var apiName = RequireString(metadata.SelectToken("api.name"), "api.name");
        var servicesBasePath = RequireString(metadata.SelectToken("api.servicesBasePath"), "api.servicesBasePath");
        var restPath = RequireString(metadata.SelectToken("api.restPath"), "api.restPath");
        var securityLevel = RequireString(metadata.SelectToken("security.level"), "security.level");
        var defaultPageSize = metadata.SelectToken("pagination.defaultPageSize")?.Value<int>() ?? 50;
        var maximumPageSize = metadata.SelectToken("pagination.maximumPageSize")?.Value<int>() ?? 200;
        var includeBusinessComponentErrorMessages = metadata.SelectToken("errorDetail.includeBusinessComponentMessages")?.Value<bool>() ?? true;
        var staticOrder = ((JArray?)metadata["order"] ?? new JArray())
            .Select(token =>
            {
                var guid = token["attributeGuid"]?.Value<string>();
                var name = token["attributeName"]?.Value<string>() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(guid) && attributesByGuid.TryGetValue(guid!, out var attribute))
                {
                    name = attribute.Name;
                }

                return new PrototypeWizardStaticOrderPart(
                    token["order"]?.Value<int>() ?? 0,
                    name,
                    token["direction"]?.Value<string>() ?? "ASC");
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.AttributeName))
            .ToArray();

        var requiredFields = ((JArray?)metadata.SelectToken("fields.required") ?? new JArray())
            .Select(token =>
            {
                var requestName = token["requestName"]?.Value<string>() ?? string.Empty;
                var guid = token["attributeGuid"]?.Value<string>();
                var fieldName = token["fieldName"]?.Value<string>() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(guid))
                {
                    if (!attributesByGuid.TryGetValue(guid!, out var attribute))
                    {
                        return null;
                    }

                    fieldName = attribute.Name;
                }

                var selected = string.Equals(requestName, "UpdateRequest", StringComparison.OrdinalIgnoreCase)
                    ? updateFields
                    : createFields;
                if (!selected.Any(name => string.Equals(name, fieldName, StringComparison.OrdinalIgnoreCase)))
                {
                    return null;
                }

                return new PrototypeWizardRequiredFieldDecision(
                    requestName,
                    fieldName,
                    token["isRequired"]?.Value<bool>() ?? false,
                    token["reason"]?.Value<string>() ?? string.Empty);
            })
            .Where(item => item is not null)
            .Cast<PrototypeWizardRequiredFieldDecision>()
            .ToArray();

        var bcStatus = metadata.SelectToken("businessComponent.status")?.Value<string>() ?? "SyncedFromMetadata";
        var isBc = metadata.SelectToken("businessComponent.isBusinessComponent")?.Value<bool>()
            ?? preview.Snapshot.Attributes.Count > 0;

        ApiPlanHierarchicalWizardSelection? hierarchicalSelection = null;
        if (ApiPlanMetadataLevelsCodec.HasHierarchicalLevels(metadata))
        {
            if (preview.CurrentHierarchicalRoot is null)
            {
                throw new InvalidOperationException("Preview hierárquico sem árvore corrente da Transaction.");
            }

            var persistedRoot = ApiPlanMetadataLevelsCodec.TryReadRoot(metadata)
                ?? throw new InvalidOperationException("Metadata hierárquica sem levels para restaurar a seleção do Sync.");
            hierarchicalSelection = ApiPlanHierarchicalWizardSelection.CreateDefault(preview.CurrentHierarchicalRoot);
            hierarchicalSelection.ApplyPersistedPrune(persistedRoot);
            ApplyHierarchicalIncludeAdded(hierarchicalSelection, choices);
        }

        return new PrototypeWizardFlowSelection(
            new PrototypeWizardContractSelection(
                preview.TransactionName,
                services,
                createFields,
                updateFields,
                responseFields,
                listFilters),
            new PrototypeWizardReviewSelection(
                preview.TransactionName,
                apiName,
                servicesBasePath,
                restPath,
                securityLevel,
                defaultPageSize,
                maximumPageSize,
                staticOrder,
                includeBusinessComponentErrorMessages),
            requiredFields,
            new PrototypeWizardBusinessComponentSelection(preview.TransactionName, isBc, false, bcStatus),
            generateSdts: true,
            generateProcedures: true,
            generateApiObject: true,
            generateMetadata: true,
            applyList: services.Any(name => string.Equals(name, "List", StringComparison.OrdinalIgnoreCase)),
            applyBusinessComponent: services.Any(name =>
                string.Equals(name, "Get", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Create", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Update", StringComparison.OrdinalIgnoreCase)),
            hierarchicalSelection);
    }

    private static void ApplyHierarchicalIncludeAdded(
        ApiPlanHierarchicalWizardSelection selection,
        ApiPlanTransactionSyncChoices choices)
    {
        foreach (var role in new[] { "CreateRequest", "UpdateRequest", "Response" })
        {
            if (!choices.IncludeAddedByRole.TryGetValue(role, out var guids) || guids is null || guids.Count == 0)
            {
                continue;
            }

            selection.IncludeAddedFieldsByGuid(role, guids);
        }
    }

    public static IReadOnlyCollection<string> ResolvePreservedSdtNames(
        ApiPlanTransactionSyncPreview preview,
        ApiPlanTransactionSyncChoices choices)
    {
        if (choices.Cancel)
        {
            return Array.Empty<string>();
        }

        var preserved = new List<string>();
        foreach (var conflict in preview.SdtConflicts)
        {
            if (!choices.SdtResolutions.TryGetValue(conflict.SdtName, out var resolution))
            {
                throw new InvalidOperationException($"Conflito de SDT sem resolucao: {conflict.SdtName}.");
            }

            if (string.Equals(resolution, ApiPlanTransactionSyncSdtResolution.Cancel, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Sincronizacao cancelada pelo conflito do SDT '{conflict.SdtName}'.");
            }

            if (string.Equals(resolution, ApiPlanTransactionSyncSdtResolution.Keep, StringComparison.Ordinal))
            {
                preserved.Add(conflict.SdtName);
            }
        }

        return preserved;
    }

    internal static ApiPlanTransactionSyncAttributeSnapshot ToSyncSnapshot(PrototypeWizardAttributeDecision attribute)
    {
        return new ApiPlanTransactionSyncAttributeSnapshot(
            attribute.Order,
            attribute.AttributeGuid,
            attribute.Name,
            attribute.DataType,
            attribute.Length,
            attribute.Decimals,
            attribute.IsPrimaryKey,
            attribute.IsNullable,
            attribute.IsFormula,
            attribute.IsInferred,
            attribute.IsRedundant,
            attribute.IsPayloadEligible,
            attribute.IsUpdatePayloadEligible,
            attribute.IsFilterEligible,
            attribute.IsSensitive,
            attribute.IsAudit,
            attribute.DefaultCreateSelected,
            attribute.DefaultUpdateSelected,
            attribute.DefaultResponseSelected,
            attribute.DefaultFilterSelected,
            attribute.PayloadDisabledReason,
            attribute.UpdatePayloadDisabledReason);
    }

    private static IReadOnlyList<string> ResolveSelectedFieldNames(
        JObject metadata,
        string path,
        IReadOnlyDictionary<string, PrototypeWizardAttributeDecision> attributesByGuid,
        ApiPlanTransactionSyncChoices choices,
        string role,
        ApiPlanTransactionSyncPreview preview,
        bool listFiltersMode = false)
    {
        var names = attributesByGuid
            .Select(pair => new ApiPlanTransactionSyncFieldName(pair.Key, pair.Value.Name))
            .ToArray();
        var removedGuids = preview.Diff.Removed.Select(item => item.AttributeGuid).ToArray();
        choices.IncludeAddedByRole.TryGetValue(role, out var includeAdded);
        var includeAddedArray = includeAdded?.ToArray() ?? Array.Empty<string>();
        var addedCandidates = preview.Diff.Added
            .Where(item => item.Current is not null)
            .Select(item => new ApiPlanTransactionSyncAddedFieldCandidate(
                item.AttributeGuid,
                item.Current!.IsWritableByCreate,
                item.Current.IsWritableByUpdate,
                item.Current.IsFilterEligible))
            .ToArray();

        var resolved = ApiPlanTransactionSyncFieldSelection.ResolveOrderedFieldNames(
            metadata,
            path,
            listFiltersMode,
            names,
            removedGuids,
            role,
            includeAddedArray,
            addedCandidates);

        if (string.Equals(role, "CreateRequest", StringComparison.Ordinal))
        {
            return resolved
                .Where(name => attributesByGuid.Values.Any(attribute =>
                    string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase) && attribute.IsPayloadEligible))
                .ToArray();
        }

        if (string.Equals(role, "UpdateRequest", StringComparison.Ordinal))
        {
            return resolved
                .Where(name => attributesByGuid.Values.Any(attribute =>
                    string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase) && attribute.IsUpdatePayloadEligible))
                .ToArray();
        }

        return resolved;
    }

    private static IReadOnlyList<ApiPlanTransactionSyncSdtConflict> DetectSdtConflicts(KBModel designModel, JObject metadata)
    {
        // B099b/Fase 7: metadata hierárquica grava campos flat no cabeçalho; comparar
        // membros do SDT raiz contra esse snapshot produz falso positivo nos três contratos.
        if (ApiPlanMetadataLevelsCodec.HasHierarchicalLevels(metadata))
        {
            return Array.Empty<ApiPlanTransactionSyncSdtConflict>();
        }

        var conflicts = new List<ApiPlanTransactionSyncSdtConflict>();
        AddConflictIfDiverged(designModel, conflicts, metadata.SelectToken("objects.sdts.createRequest")?.Value<string>(), ReadFieldNames(metadata, "fields.createRequest"));
        AddConflictIfDiverged(designModel, conflicts, metadata.SelectToken("objects.sdts.updateRequest")?.Value<string>(), ReadFieldNames(metadata, "fields.updateRequest"));
        AddConflictIfDiverged(designModel, conflicts, metadata.SelectToken("objects.sdts.response")?.Value<string>(), ReadFieldNames(metadata, "fields.response"));
        AddConflictIfDiverged(designModel, conflicts, metadata.SelectToken("objects.sdts.listFilters")?.Value<string>(), ReadExpectedListFilterMemberNames(metadata));
        return conflicts;
    }

    private static void AddConflictIfDiverged(
        KBModel designModel,
        List<ApiPlanTransactionSyncSdtConflict> conflicts,
        string? sdtName,
        IReadOnlyCollection<string> expectedNames)
    {
        if (string.IsNullOrWhiteSpace(sdtName))
        {
            return;
        }

        var ownedSdtName = sdtName!;
        var matches = SDT.GetAll(designModel)
            .Where(item => string.Equals(item.Name, ownedSdtName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (matches.Length != 1)
        {
            return;
        }

        var sdt = matches[0];
        if (!ApiPlanOwnedObjectDescription.IsOwnedSdt(sdt.Description, ownedSdtName))
        {
            return;
        }

        var actualNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (SDTItem item in sdt.SDTStructure.Root.Items)
        {
            if (!string.IsNullOrWhiteSpace(item.Name))
            {
                actualNames.Add(item.Name);
            }
        }
        var expected = new HashSet<string>(expectedNames, StringComparer.OrdinalIgnoreCase);
        if (actualNames.SetEquals(expected))
        {
            return;
        }

        conflicts.Add(new ApiPlanTransactionSyncSdtConflict(
            sdtName!,
            "Estrutura do SDT diverge do snapshot da metadata (edicao manual provavel)."));
    }

    private static IReadOnlyCollection<string> ReadFieldNames(JObject metadata, string path)
    {
        return ((JArray?)metadata.SelectToken(path) ?? new JArray())
            .Select(token => token["name"]?.Value<string>() ?? string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();
    }

    private static IReadOnlyCollection<string> ReadFilterFieldNames(JObject metadata)
    {
        return ((JArray?)metadata.SelectToken("fields.listFilters") ?? new JArray())
            .Select(token => token.SelectToken("field.name")?.Value<string>() ?? string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();
    }

    private static IReadOnlyCollection<string> ReadExpectedListFilterMemberNames(JObject metadata)
    {
        var expected = new List<string>();
        foreach (var token in (JArray?)metadata.SelectToken("fields.listFilters") ?? new JArray())
        {
            var fieldName = token.SelectToken("field.name")?.Value<string>() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                continue;
            }

            var usesPeriod = token["usesPeriod"]?.Value<bool>() ?? false;
            var usesRange = token["usesRange"]?.Value<bool>() ?? false;
            if (usesPeriod)
            {
                expected.Add(fieldName + "From");
                expected.Add(fieldName + "To");
            }
            else if (usesRange)
            {
                expected.Add(fieldName + "Min");
                expected.Add(fieldName + "Max");
            }
            else
            {
                expected.Add(fieldName);
            }
        }

        return expected;
    }

    private static void RequireOwnership(JObject metadata, Transaction transaction)
    {
        var transactionName = RequireString(metadata.SelectToken("ownership.transactionName"), "ownership.transactionName");
        var transactionGuid = RequireString(metadata.SelectToken("ownership.transactionGuid"), "ownership.transactionGuid");
        if (!string.Equals(transactionName, transaction.Name, StringComparison.Ordinal)
            || !string.Equals(transactionGuid, transaction.Guid.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Metadata nao pertence a Transaction selecionada. Nenhuma alteracao foi feita.");
        }
    }

    private static WikiFileKBObject FindOwnedMetadataFile(KBModel designModel, string metadataFileName, string transactionName)
    {
        var matches = WikiFileKBObject.GetAll(designModel)
            .Where(file => string.Equals(file.Name, metadataFileName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matches.Length == 0)
        {
            throw new InvalidOperationException($"Sincronizacao bloqueada: File de metadata '{metadataFileName}' nao foi encontrado. Nenhuma alteracao foi feita.");
        }

        if (matches.Length > 1)
        {
            throw new InvalidOperationException($"Sincronizacao bloqueada: foram encontrados {matches.Length} Files chamados '{metadataFileName}'. Nenhuma alteracao foi feita.");
        }

        var file = matches[0];
        if (ApiPlanOwnedObjectDescription.IsOwnedMetadataFile(file.Description, metadataFileName, transactionName))
        {
            return file;
        }

        throw new InvalidOperationException($"Sincronizacao bloqueada: File '{metadataFileName}' nao e metadata propria da extensao. Nenhuma alteracao foi feita.");
    }

    private static JObject ParseMetadata(WikiFileKBObject file)
    {
        var bytes = file.BlobPart?.Data?.GetBytes();
        if (bytes is null || bytes.Length == 0)
        {
            throw new InvalidOperationException($"Sincronizacao bloqueada: File '{file.Name}' nao possui JSON persistido. Nenhuma alteracao foi feita.");
        }

        try
        {
            return ApiPlanMetadataIntegrity.ParseMetadataBytes(bytes);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Sincronizacao bloqueada: File '{file.Name}' possui JSON invalido. Nenhuma alteracao foi feita.", ex);
        }
    }

    private static string RequireString(JToken? token, string path)
    {
        var value = token?.Value<string>();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Metadata incompleta: '{path}' ausente.");
        }

        return value!;
    }
}

internal sealed class ApiPlanTransactionSyncPreview
{
    public ApiPlanTransactionSyncPreview(
        string transactionName,
        JObject metadata,
        WikiFileKBObject metadataFile,
        PrototypeWizardContractSnapshot snapshot,
        ApiPlanTransactionSyncDiff diff,
        IReadOnlyList<ApiPlanTransactionSyncSdtConflict> sdtConflicts,
        ApiPlanLevel? currentHierarchicalRoot = null)
    {
        TransactionName = transactionName ?? throw new ArgumentNullException(nameof(transactionName));
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        MetadataFile = metadataFile ?? throw new ArgumentNullException(nameof(metadataFile));
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        Diff = diff ?? throw new ArgumentNullException(nameof(diff));
        SdtConflicts = sdtConflicts ?? throw new ArgumentNullException(nameof(sdtConflicts));
        CurrentHierarchicalRoot = currentHierarchicalRoot;
    }

    public string TransactionName { get; }

    public JObject Metadata { get; }

    public WikiFileKBObject MetadataFile { get; }

    public PrototypeWizardContractSnapshot Snapshot { get; }

    public ApiPlanTransactionSyncDiff Diff { get; }

    public IReadOnlyList<ApiPlanTransactionSyncSdtConflict> SdtConflicts { get; }

    /// <summary>Árvore corrente da Transaction quando a metadata é hierárquica V2; null no sync plano.</summary>
    public ApiPlanLevel? CurrentHierarchicalRoot { get; }
}

internal sealed class ApiPlanTransactionSyncSdtConflict
{
    public ApiPlanTransactionSyncSdtConflict(string sdtName, string reason)
    {
        SdtName = sdtName ?? throw new ArgumentNullException(nameof(sdtName));
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
    }

    public string SdtName { get; }

    public string Reason { get; }
}

internal static class ApiPlanTransactionSyncSdtResolution
{
    public const string Keep = "Keep";
    public const string Replace = "Replace";
    public const string Cancel = "Cancel";
}

internal sealed class ApiPlanTransactionSyncChoices
{
    public ApiPlanTransactionSyncChoices(
        bool cancel,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> includeAddedByRole,
        IReadOnlyDictionary<string, string> sdtResolutions)
    {
        Cancel = cancel;
        IncludeAddedByRole = includeAddedByRole ?? throw new ArgumentNullException(nameof(includeAddedByRole));
        SdtResolutions = sdtResolutions ?? throw new ArgumentNullException(nameof(sdtResolutions));
    }

    public bool Cancel { get; }

    /// <summary>
    /// Chave = role (CreateRequest, UpdateRequest, Response, ListFilters); valor = attributeGuids a incluir.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> IncludeAddedByRole { get; }

    public IReadOnlyDictionary<string, string> SdtResolutions { get; }
}
