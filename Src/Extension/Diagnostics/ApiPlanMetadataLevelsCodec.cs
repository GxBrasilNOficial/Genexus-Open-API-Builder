#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using GenexusOpenApiBuilder.Extension.Domain;
using Newtonsoft.Json.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// B099b — serialização da árvore <see cref="ApiPlanLevel"/> na metadata V2 e no contrato B067.
/// </summary>
internal static class ApiPlanMetadataLevelsCodec
{
    public static JToken? CreateLevelsToken(ApiPlan apiPlan)
    {
        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        if (!ApiPlanSdtHierarchicalNaming.TryGetRoot(apiPlan, out var root) || root.ChildLevels.Count == 0)
        {
            return null;
        }

        return SerializeLevel(root);
    }

    public static bool HasHierarchicalLevels(JObject metadata)
    {
        if (metadata is null)
        {
            throw new ArgumentNullException(nameof(metadata));
        }

        var levels = metadata["levels"] as JObject;
        if (levels is null)
        {
            return false;
        }

        var children = levels["childLevels"] as JArray;
        return children is not null && children.Count > 0;
    }

    public static ApiPlanLevel? TryReadRoot(JObject metadata)
    {
        if (metadata is null)
        {
            throw new ArgumentNullException(nameof(metadata));
        }

        if (metadata["levels"] is not JObject levels)
        {
            return null;
        }

        return ReadLevel(levels);
    }

    public static JObject SerializeLevel(ApiPlanLevel level)
    {
        if (level is null)
        {
            throw new ArgumentNullException(nameof(level));
        }

        var json = new JObject
        {
            ["levelName"] = level.LevelName,
            ["depth"] = level.Depth,
            ["parentLevelName"] = level.ParentLevelName,
            ["levelOrder"] = level.LevelOrder,
            ["includeListCount"] = level.IncludeListCount,
            ["primaryKey"] = new JArray(level.PrimaryKey.Select(SerializeField)),
            ["fields"] = new JArray(level.Fields.Select(SerializeField)),
            ["childLevels"] = new JArray(level.ChildLevels.Select(SerializeLevel)),
        };

        if (level.SelectedCreateFieldNames is not null)
        {
            json["selectedCreateFieldNames"] = ToNameArray(level.SelectedCreateFieldNames);
        }

        if (level.SelectedUpdateFieldNames is not null)
        {
            json["selectedUpdateFieldNames"] = ToNameArray(level.SelectedUpdateFieldNames);
        }

        if (level.SelectedResponseFieldNames is not null)
        {
            json["selectedResponseFieldNames"] = ToNameArray(level.SelectedResponseFieldNames);
        }

        return json;
    }

    public static ApiPlanLevel ReadLevel(JObject json)
    {
        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        var levelName = json["levelName"]?.Value<string>();
        if (string.IsNullOrWhiteSpace(levelName))
        {
            throw new InvalidOperationException("levels.levelName é obrigatório.");
        }

        var depth = json["depth"]?.Value<int>() ?? 0;
        var parentLevelName = json["parentLevelName"]?.Value<string>() ?? string.Empty;
        var levelOrder = json["levelOrder"]?.Value<int>() ?? 0;
        var includeListCount = json["includeListCount"]?.Value<bool>() ?? true;
        var primaryKey = ReadFieldArray(json["primaryKey"] as JArray);
        var fields = ReadFieldArray(json["fields"] as JArray);
        var children = ((JArray?)json["childLevels"] ?? new JArray())
            .OfType<JObject>()
            .Select(ReadLevel)
            .ToArray();

        return new ApiPlanLevel(
            levelName!,
            depth,
            parentLevelName,
            levelOrder,
            primaryKey,
            fields,
            children,
            includeListCount,
            ReadOptionalNames(json["selectedCreateFieldNames"] as JArray),
            ReadOptionalNames(json["selectedUpdateFieldNames"] as JArray),
            ReadOptionalNames(json["selectedResponseFieldNames"] as JArray));
    }

    public static IReadOnlyList<ApiPlanTransactionSyncAttributeSnapshot> FlattenToSyncSnapshots(ApiPlanLevel root)
    {
        if (root is null)
        {
            throw new ArgumentNullException(nameof(root));
        }

        var buffer = new List<ApiPlanTransactionSyncAttributeSnapshot>();
        AppendSnapshots(root, buffer);
        return buffer;
    }

    private static void AppendSnapshots(ApiPlanLevel level, List<ApiPlanTransactionSyncAttributeSnapshot> buffer)
    {
        foreach (var field in level.Fields.OrderBy(item => item.Order))
        {
            if (buffer.Any(item => string.Equals(item.AttributeGuid, field.AttributeGuid, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            buffer.Add(ToSyncSnapshot(field));
        }

        foreach (var child in level.ChildLevels)
        {
            AppendSnapshots(child, buffer);
        }
    }

    private static ApiPlanTransactionSyncAttributeSnapshot ToSyncSnapshot(ApiPlanLevelField field)
    {
        var writableCreate = IsWritableCreate(field);
        var writableUpdate = IsWritableUpdate(field);
        return new ApiPlanTransactionSyncAttributeSnapshot(
            field.Order,
            field.AttributeGuid,
            field.Name,
            field.DataType,
            field.Length,
            field.Decimals,
            field.IsPrimaryKey,
            field.IsNullable,
            field.IsFormula,
            field.IsInferred,
            field.IsRedundant,
            writableCreate,
            writableUpdate,
            isFilterEligible: false,
            isSensitive: false,
            isAuditField: false,
            defaultCreateSelected: writableCreate,
            defaultUpdateSelected: writableUpdate,
            defaultResponseSelected: true,
            defaultFilterSelected: false,
            payloadDisabledReason: string.Empty,
            updatePayloadDisabledReason: string.Empty);
    }

    private static bool IsWritableCreate(ApiPlanLevelField field)
    {
        if (field.IsFormula || field.IsNoAccept || field.IsInferred || field.IsRedundant)
        {
            return false;
        }

        if (field.IsPrimaryKey && field.IsAutonumber)
        {
            return false;
        }

        if (field.IsPrimaryKey && field.IsForeignKey)
        {
            return false;
        }

        return true;
    }

    private static bool IsWritableUpdate(ApiPlanLevelField field)
    {
        return !(field.IsFormula || field.IsNoAccept || field.IsInferred || field.IsRedundant);
    }

    private static JObject SerializeField(ApiPlanLevelField field)
    {
        return new JObject
        {
            ["order"] = field.Order,
            ["attributeGuid"] = field.AttributeGuid,
            ["name"] = field.Name,
            ["dataType"] = field.DataType,
            ["length"] = field.Length,
            ["decimals"] = field.Decimals,
            ["isPrimaryKey"] = field.IsPrimaryKey,
            ["isNullable"] = field.IsNullable,
            ["isInferred"] = field.IsInferred,
            ["isRedundant"] = field.IsRedundant,
            ["isForeignKey"] = field.IsForeignKey,
            ["isFormula"] = field.IsFormula,
            ["isNoAccept"] = field.IsNoAccept,
            ["isAutonumber"] = field.IsAutonumber,
        };
    }

    private static IReadOnlyList<ApiPlanLevelField> ReadFieldArray(JArray? array)
    {
        if (array is null || array.Count == 0)
        {
            return Array.Empty<ApiPlanLevelField>();
        }

        return array.OfType<JObject>().Select(ReadField).ToArray();
    }

    private static ApiPlanLevelField ReadField(JObject json)
    {
        var guid = json["attributeGuid"]?.Value<string>();
        if (string.IsNullOrWhiteSpace(guid))
        {
            throw new InvalidOperationException("levels.fields exige attributeGuid.");
        }

        return new ApiPlanLevelField(
            json["order"]?.Value<int>() ?? 0,
            guid!,
            json["name"]?.Value<string>() ?? string.Empty,
            json["dataType"]?.Value<string>() ?? string.Empty,
            json["length"]?.Value<int>() ?? 0,
            json["decimals"]?.Value<int>() ?? 0,
            json["isPrimaryKey"]?.Value<bool>() ?? false,
            json["isNullable"]?.Value<bool>() ?? false,
            json["isInferred"]?.Value<bool>() ?? false,
            json["isRedundant"]?.Value<bool>() ?? false,
            json["isForeignKey"]?.Value<bool>() ?? false,
            json["isFormula"]?.Value<bool>() ?? false,
            json["isNoAccept"]?.Value<bool>() ?? false,
            json["isAutonumber"]?.Value<bool>() ?? false);
    }

    private static JArray ToNameArray(IReadOnlyCollection<string> names)
    {
        return new JArray(names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(name => (JToken)name));
    }

    private static IReadOnlyCollection<string>? ReadOptionalNames(JArray? array)
    {
        if (array is null)
        {
            return null;
        }

        return array
            .Where(item => item.Type == JTokenType.String && !string.IsNullOrWhiteSpace(item.Value<string>()))
            .Select(item => item.Value<string>()!)
            .ToArray();
    }
}
