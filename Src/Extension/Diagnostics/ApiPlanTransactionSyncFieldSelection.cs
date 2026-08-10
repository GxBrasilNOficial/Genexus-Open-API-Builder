#nullable enable

using System;
using Newtonsoft.Json.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Seleção ordenada de campos no Sync B085: preserva a ordem da metadata,
/// remove por GUID e anexa campos novos no fim. Deduplica por GUID.
/// Evita List/Dictionary/HashSet concretos para permitir Add-Type offline no teste PowerShell.
/// </summary>
public static class ApiPlanTransactionSyncFieldSelection
{
    public static string[] ResolveOrderedFieldNames(
        JObject metadata,
        string path,
        bool listFiltersMode,
        ApiPlanTransactionSyncFieldName[] attributeNames,
        string[] removedAttributeGuids,
        string role,
        string[] includeAddedGuidsForRole,
        ApiPlanTransactionSyncAddedFieldCandidate[] addedCandidates)
    {
        if (metadata is null)
        {
            throw new ArgumentNullException(nameof(metadata));
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        if (attributeNames is null)
        {
            throw new ArgumentNullException(nameof(attributeNames));
        }

        if (removedAttributeGuids is null)
        {
            throw new ArgumentNullException(nameof(removedAttributeGuids));
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Role is required.", nameof(role));
        }

        if (includeAddedGuidsForRole is null)
        {
            throw new ArgumentNullException(nameof(includeAddedGuidsForRole));
        }

        if (addedCandidates is null)
        {
            throw new ArgumentNullException(nameof(addedCandidates));
        }

        var tokens = metadata.SelectToken(path) as JArray;
        var tokenCount = tokens is null ? 0 : tokens.Count;
        var capacity = tokenCount + addedCandidates.Length;
        var selectedGuids = new string[capacity];
        var selectedCount = 0;

        if (tokens is not null)
        {
            for (var index = 0; index < tokens.Count; index++)
            {
                var token = tokens[index];
                var guid = listFiltersMode
                    ? token.SelectToken("field.attributeGuid")?.Value<string>()
                    : token["attributeGuid"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(guid) || ContainsGuid(selectedGuids, selectedCount, guid!))
                {
                    continue;
                }

                selectedGuids[selectedCount++] = guid!;
            }
        }

        for (var index = 0; index < removedAttributeGuids.Length; index++)
        {
            var removedGuid = removedAttributeGuids[index];
            if (string.IsNullOrWhiteSpace(removedGuid))
            {
                continue;
            }

            selectedCount = RemoveGuid(selectedGuids, selectedCount, removedGuid);
        }

        for (var index = 0; index < addedCandidates.Length; index++)
        {
            var candidate = addedCandidates[index];
            if (candidate is null || string.IsNullOrWhiteSpace(candidate.AttributeGuid))
            {
                continue;
            }

            if (!ContainsGuid(includeAddedGuidsForRole, includeAddedGuidsForRole.Length, candidate.AttributeGuid))
            {
                continue;
            }

            if (string.Equals(role, "CreateRequest", StringComparison.Ordinal) && !candidate.IsWritableByCreate)
            {
                continue;
            }

            if (string.Equals(role, "UpdateRequest", StringComparison.Ordinal) && !candidate.IsWritableByUpdate)
            {
                continue;
            }

            if (string.Equals(role, "ListFilters", StringComparison.Ordinal) && !candidate.IsFilterEligible)
            {
                continue;
            }

            if (ContainsGuid(selectedGuids, selectedCount, candidate.AttributeGuid))
            {
                continue;
            }

            selectedGuids[selectedCount++] = candidate.AttributeGuid;
        }

        var names = new string[selectedCount];
        var nameCount = 0;
        for (var index = 0; index < selectedCount; index++)
        {
            var name = FindName(attributeNames, selectedGuids[index]);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            names[nameCount++] = name!;
        }

        return Take(names, nameCount);
    }

    private static bool ContainsGuid(string[] guids, int count, string attributeGuid)
    {
        for (var index = 0; index < count; index++)
        {
            if (string.Equals(guids[index], attributeGuid, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static int RemoveGuid(string[] guids, int count, string attributeGuid)
    {
        var write = 0;
        for (var read = 0; read < count; read++)
        {
            if (string.Equals(guids[read], attributeGuid, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            guids[write++] = guids[read];
        }

        for (var index = write; index < count; index++)
        {
            guids[index] = null!;
        }

        return write;
    }

    private static string? FindName(ApiPlanTransactionSyncFieldName[] attributeNames, string attributeGuid)
    {
        for (var index = 0; index < attributeNames.Length; index++)
        {
            var item = attributeNames[index];
            if (item is not null &&
                string.Equals(item.AttributeGuid, attributeGuid, StringComparison.OrdinalIgnoreCase))
            {
                return item.Name;
            }
        }

        return null;
    }

    private static string[] Take(string[] buffer, int count)
    {
        var result = new string[count];
        Array.Copy(buffer, result, count);
        return result;
    }
}

public sealed class ApiPlanTransactionSyncFieldName
{
    public ApiPlanTransactionSyncFieldName(string attributeGuid, string name)
    {
        if (string.IsNullOrWhiteSpace(attributeGuid))
        {
            throw new ArgumentException("Attribute GUID is required.", nameof(attributeGuid));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        AttributeGuid = attributeGuid;
        Name = name;
    }

    public string AttributeGuid { get; }

    public string Name { get; }
}

public sealed class ApiPlanTransactionSyncAddedFieldCandidate
{
    public ApiPlanTransactionSyncAddedFieldCandidate(
        string attributeGuid,
        bool isWritableByCreate,
        bool isWritableByUpdate,
        bool isFilterEligible)
    {
        if (string.IsNullOrWhiteSpace(attributeGuid))
        {
            throw new ArgumentException("Attribute GUID is required.", nameof(attributeGuid));
        }

        AttributeGuid = attributeGuid;
        IsWritableByCreate = isWritableByCreate;
        IsWritableByUpdate = isWritableByUpdate;
        IsFilterEligible = isFilterEligible;
    }

    public string AttributeGuid { get; }

    public bool IsWritableByCreate { get; }

    public bool IsWritableByUpdate { get; }

    public bool IsFilterEligible { get; }
}
