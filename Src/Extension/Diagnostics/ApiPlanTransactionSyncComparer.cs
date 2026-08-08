#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Compara a estrutura atual da Transaction com o snapshot persistido na metadata (B085).
/// Identidade de atributo por GUID; rename = mesmo GUID com nome diferente.
/// Evita List/Dictionary concretos para permitir Add-Type offline no teste PowerShell.
/// </summary>
public static class ApiPlanTransactionSyncComparer
{
    public static ApiPlanTransactionSyncDiff Compare(
        IReadOnlyList<ApiPlanTransactionSyncAttributeSnapshot> metadataStructure,
        IReadOnlyList<ApiPlanTransactionSyncAttributeSnapshot> currentStructure)
    {
        if (metadataStructure is null)
        {
            throw new ArgumentNullException(nameof(metadataStructure));
        }

        if (currentStructure is null)
        {
            throw new ArgumentNullException(nameof(currentStructure));
        }

        var addedBuffer = new ApiPlanTransactionSyncAttributeChange[currentStructure.Count];
        var removedBuffer = new ApiPlanTransactionSyncAttributeChange[metadataStructure.Count];
        var renamedBuffer = new ApiPlanTransactionSyncAttributeChange[currentStructure.Count];
        var modifiedBuffer = new ApiPlanTransactionSyncAttributeChange[currentStructure.Count];
        var unchangedBuffer = new ApiPlanTransactionSyncAttributeChange[currentStructure.Count];
        var addedCount = 0;
        var removedCount = 0;
        var renamedCount = 0;
        var modifiedCount = 0;
        var unchangedCount = 0;

        foreach (var current in currentStructure.OrderBy(item => item.Order))
        {
            var previous = FindByGuid(metadataStructure, current.AttributeGuid);
            if (previous is null)
            {
                addedBuffer[addedCount++] = ApiPlanTransactionSyncAttributeChange.Added(current);
                continue;
            }

            var rename = !string.Equals(previous.Name, current.Name, StringComparison.Ordinal);
            var details = DescribeModifications(previous, current);
            if (rename && details.Length == 0)
            {
                renamedBuffer[renamedCount++] = ApiPlanTransactionSyncAttributeChange.Renamed(previous, current);
            }
            else if (rename || details.Length > 0)
            {
                modifiedBuffer[modifiedCount++] = ApiPlanTransactionSyncAttributeChange.Modified(previous, current, rename, details);
            }
            else
            {
                unchangedBuffer[unchangedCount++] = ApiPlanTransactionSyncAttributeChange.Unchanged(current);
            }
        }

        foreach (var previous in metadataStructure.OrderBy(item => item.Order))
        {
            if (FindByGuid(currentStructure, previous.AttributeGuid) is null)
            {
                removedBuffer[removedCount++] = ApiPlanTransactionSyncAttributeChange.Removed(previous);
            }
        }

        return new ApiPlanTransactionSyncDiff(
            Take(addedBuffer, addedCount),
            Take(removedBuffer, removedCount),
            Take(renamedBuffer, renamedCount),
            Take(modifiedBuffer, modifiedCount),
            Take(unchangedBuffer, unchangedCount));
    }

    public static IReadOnlyList<ApiPlanTransactionSyncAttributeSnapshot> ReadStructure(JObject metadata)
    {
        if (metadata is null)
        {
            throw new ArgumentNullException(nameof(metadata));
        }

        var array = metadata["transactionStructure"] as JArray;
        if (array is null || array.Count == 0)
        {
            throw new InvalidOperationException("Metadata sem transactionStructure com attributeGuid. Regenere a API pelo Wizard antes de sincronizar.");
        }

        var result = new ApiPlanTransactionSyncAttributeSnapshot[array.Count];
        for (var index = 0; index < array.Count; index++)
        {
            result[index] = ReadSnapshot(array[index]);
        }

        return result;
    }

    private static ApiPlanTransactionSyncAttributeSnapshot? FindByGuid(
        IReadOnlyList<ApiPlanTransactionSyncAttributeSnapshot> items,
        string attributeGuid)
    {
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            if (string.Equals(item.AttributeGuid, attributeGuid, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        return null;
    }

    private static ApiPlanTransactionSyncAttributeChange[] Take(ApiPlanTransactionSyncAttributeChange[] buffer, int count)
    {
        var result = new ApiPlanTransactionSyncAttributeChange[count];
        Array.Copy(buffer, result, count);
        return result;
    }

    private static ApiPlanTransactionSyncAttributeSnapshot ReadSnapshot(JToken token)
    {
        var guid = token["attributeGuid"]?.Value<string>();
        if (string.IsNullOrWhiteSpace(guid))
        {
            throw new InvalidOperationException("transactionStructure exige attributeGuid em cada atributo.");
        }

        return new ApiPlanTransactionSyncAttributeSnapshot(
            token["order"]?.Value<int>() ?? 0,
            guid!,
            token["name"]?.Value<string>() ?? string.Empty,
            token["dataType"]?.Value<string>() ?? string.Empty,
            token["length"]?.Value<int>() ?? 0,
            token["decimals"]?.Value<int>() ?? 0,
            token["isPrimaryKey"]?.Value<bool>() ?? false,
            token["isNullable"]?.Value<bool>() ?? false,
            token["isFormula"]?.Value<bool>() ?? false,
            token["isInferred"]?.Value<bool>() ?? false,
            token["isRedundant"]?.Value<bool>() ?? false,
            token["isWritableByCreate"]?.Value<bool>() ?? false,
            token["isWritableByUpdate"]?.Value<bool>() ?? false,
            token["isFilterEligible"]?.Value<bool>() ?? false,
            token["isSensitive"]?.Value<bool>() ?? false,
            token["isAuditField"]?.Value<bool>() ?? false,
            defaultCreateSelected: false,
            defaultUpdateSelected: false,
            defaultResponseSelected: false,
            defaultFilterSelected: false,
            payloadDisabledReason: string.Empty,
            updatePayloadDisabledReason: string.Empty);
    }

    private static string[] DescribeModifications(
        ApiPlanTransactionSyncAttributeSnapshot previous,
        ApiPlanTransactionSyncAttributeSnapshot current)
    {
        var buffer = new string[8];
        var count = 0;
        if (!string.Equals(previous.DataType, current.DataType, StringComparison.OrdinalIgnoreCase))
        {
            buffer[count++] = $"tipo {previous.DataType} -> {current.DataType}";
        }

        if (previous.Length != current.Length || previous.Decimals != current.Decimals)
        {
            buffer[count++] = $"tamanho {previous.Length}.{previous.Decimals} -> {current.Length}.{current.Decimals}";
        }

        if (previous.IsPrimaryKey != current.IsPrimaryKey)
        {
            buffer[count++] = $"chavePrimaria {previous.IsPrimaryKey} -> {current.IsPrimaryKey}";
        }

        if (previous.IsNullable != current.IsNullable)
        {
            buffer[count++] = $"nullable {previous.IsNullable} -> {current.IsNullable}";
        }

        if (previous.IsWritableByCreate != current.IsWritableByCreate)
        {
            buffer[count++] = $"gravavelCreate {previous.IsWritableByCreate} -> {current.IsWritableByCreate}";
        }

        if (previous.IsWritableByUpdate != current.IsWritableByUpdate)
        {
            buffer[count++] = $"gravavelUpdate {previous.IsWritableByUpdate} -> {current.IsWritableByUpdate}";
        }

        if (previous.IsFormula != current.IsFormula || previous.IsInferred != current.IsInferred || previous.IsRedundant != current.IsRedundant)
        {
            buffer[count++] = "natureza estrutural (formula/inferido/redundante) alterada";
        }

        var result = new string[count];
        Array.Copy(buffer, result, count);
        return result;
    }
}

public sealed class ApiPlanTransactionSyncAttributeSnapshot
{
    public ApiPlanTransactionSyncAttributeSnapshot(
        int order,
        string attributeGuid,
        string name,
        string dataType,
        int length,
        int decimals,
        bool isPrimaryKey,
        bool isNullable,
        bool isFormula,
        bool isInferred,
        bool isRedundant,
        bool isWritableByCreate,
        bool isWritableByUpdate,
        bool isFilterEligible,
        bool isSensitive,
        bool isAuditField,
        bool defaultCreateSelected,
        bool defaultUpdateSelected,
        bool defaultResponseSelected,
        bool defaultFilterSelected,
        string payloadDisabledReason,
        string updatePayloadDisabledReason)
    {
        if (string.IsNullOrWhiteSpace(attributeGuid))
        {
            throw new ArgumentException("Attribute GUID is required.", nameof(attributeGuid));
        }

        Order = order;
        AttributeGuid = attributeGuid;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
        Length = length;
        Decimals = decimals;
        IsPrimaryKey = isPrimaryKey;
        IsNullable = isNullable;
        IsFormula = isFormula;
        IsInferred = isInferred;
        IsRedundant = isRedundant;
        IsWritableByCreate = isWritableByCreate;
        IsWritableByUpdate = isWritableByUpdate;
        IsFilterEligible = isFilterEligible;
        IsSensitive = isSensitive;
        IsAuditField = isAuditField;
        DefaultCreateSelected = defaultCreateSelected;
        DefaultUpdateSelected = defaultUpdateSelected;
        DefaultResponseSelected = defaultResponseSelected;
        DefaultFilterSelected = defaultFilterSelected;
        PayloadDisabledReason = payloadDisabledReason ?? string.Empty;
        UpdatePayloadDisabledReason = updatePayloadDisabledReason ?? string.Empty;
    }

    public int Order { get; }

    public string AttributeGuid { get; }

    public string Name { get; }

    public string DataType { get; }

    public int Length { get; }

    public int Decimals { get; }

    public bool IsPrimaryKey { get; }

    public bool IsNullable { get; }

    public bool IsFormula { get; }

    public bool IsInferred { get; }

    public bool IsRedundant { get; }

    public bool IsWritableByCreate { get; }

    public bool IsWritableByUpdate { get; }

    public bool IsFilterEligible { get; }

    public bool IsSensitive { get; }

    public bool IsAuditField { get; }

    public bool DefaultCreateSelected { get; }

    public bool DefaultUpdateSelected { get; }

    public bool DefaultResponseSelected { get; }

    public bool DefaultFilterSelected { get; }

    public string PayloadDisabledReason { get; }

    public string UpdatePayloadDisabledReason { get; }
}

public sealed class ApiPlanTransactionSyncAttributeChange
{
    private ApiPlanTransactionSyncAttributeChange(
        string kind,
        ApiPlanTransactionSyncAttributeSnapshot? previous,
        ApiPlanTransactionSyncAttributeSnapshot? current,
        bool renamed,
        IReadOnlyList<string> details)
    {
        Kind = kind;
        Previous = previous;
        Current = current;
        IsRename = renamed;
        Details = details ?? Array.Empty<string>();
    }

    public static ApiPlanTransactionSyncAttributeChange Added(ApiPlanTransactionSyncAttributeSnapshot current)
        => new("Added", null, current, false, Array.Empty<string>());

    public static ApiPlanTransactionSyncAttributeChange Removed(ApiPlanTransactionSyncAttributeSnapshot previous)
        => new("Removed", previous, null, false, Array.Empty<string>());

    public static ApiPlanTransactionSyncAttributeChange Renamed(
        ApiPlanTransactionSyncAttributeSnapshot previous,
        ApiPlanTransactionSyncAttributeSnapshot current)
        => new("Renamed", previous, current, true, Array.Empty<string>());

    public static ApiPlanTransactionSyncAttributeChange Modified(
        ApiPlanTransactionSyncAttributeSnapshot previous,
        ApiPlanTransactionSyncAttributeSnapshot current,
        bool renamed,
        IReadOnlyList<string> details)
        => new("Modified", previous, current, renamed, details);

    public static ApiPlanTransactionSyncAttributeChange Unchanged(ApiPlanTransactionSyncAttributeSnapshot current)
        => new("Unchanged", current, current, false, Array.Empty<string>());

    public string Kind { get; }

    public ApiPlanTransactionSyncAttributeSnapshot? Previous { get; }

    public ApiPlanTransactionSyncAttributeSnapshot? Current { get; }

    public bool IsRename { get; }

    public IReadOnlyList<string> Details { get; }

    public string AttributeGuid => Current?.AttributeGuid ?? Previous?.AttributeGuid ?? string.Empty;

    public string DisplayName => Current?.Name ?? Previous?.Name ?? string.Empty;

    public string FormatSummary()
    {
        if (string.Equals(Kind, "Added", StringComparison.Ordinal))
        {
            return $"+ {Current!.Name} ({Current.DataType})";
        }

        if (string.Equals(Kind, "Removed", StringComparison.Ordinal))
        {
            return $"- {Previous!.Name} ({Previous.DataType})";
        }

        if (string.Equals(Kind, "Renamed", StringComparison.Ordinal))
        {
            return $"~ {Previous!.Name} -> {Current!.Name}";
        }

        if (string.Equals(Kind, "Modified", StringComparison.Ordinal))
        {
            var renamePart = IsRename ? $"{Previous!.Name} -> {Current!.Name}; " : string.Empty;
            return $"* {Current!.Name}: {renamePart}{string.Join("; ", Details)}";
        }

        return $"= {Current!.Name}";
    }
}

public sealed class ApiPlanTransactionSyncDiff
{
    public ApiPlanTransactionSyncDiff(
        IReadOnlyList<ApiPlanTransactionSyncAttributeChange> added,
        IReadOnlyList<ApiPlanTransactionSyncAttributeChange> removed,
        IReadOnlyList<ApiPlanTransactionSyncAttributeChange> renamed,
        IReadOnlyList<ApiPlanTransactionSyncAttributeChange> modified,
        IReadOnlyList<ApiPlanTransactionSyncAttributeChange> unchanged)
    {
        Added = added ?? throw new ArgumentNullException(nameof(added));
        Removed = removed ?? throw new ArgumentNullException(nameof(removed));
        Renamed = renamed ?? throw new ArgumentNullException(nameof(renamed));
        Modified = modified ?? throw new ArgumentNullException(nameof(modified));
        Unchanged = unchanged ?? throw new ArgumentNullException(nameof(unchanged));
    }

    public IReadOnlyList<ApiPlanTransactionSyncAttributeChange> Added { get; }

    public IReadOnlyList<ApiPlanTransactionSyncAttributeChange> Removed { get; }

    public IReadOnlyList<ApiPlanTransactionSyncAttributeChange> Renamed { get; }

    public IReadOnlyList<ApiPlanTransactionSyncAttributeChange> Modified { get; }

    public IReadOnlyList<ApiPlanTransactionSyncAttributeChange> Unchanged { get; }

    public bool HasDifferences => Added.Count > 0 || Removed.Count > 0 || Renamed.Count > 0 || Modified.Count > 0;

    public string BuildSummary()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Adicionados: {Added.Count}; Removidos: {Removed.Count}; Renomeados: {Renamed.Count}; Modificados: {Modified.Count}; Inalterados: {Unchanged.Count}");
        foreach (var change in Added.Concat(Removed).Concat(Renamed).Concat(Modified))
        {
            builder.AppendLine(change.FormatSummary());
        }

        return builder.ToString().TrimEnd();
    }
}
