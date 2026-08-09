#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Residual B083 — conflito de colisão apresentado ao usuário (nome, tipo, módulo e Folder).
/// </summary>
public sealed class ApiPlanCollisionConflict
{
    public const string NotApplicable = "(n/a)";

    public ApiPlanCollisionConflict(string name, string objectType, string moduleName, string folderName)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ObjectType = objectType ?? throw new ArgumentNullException(nameof(objectType));
        ModuleName = string.IsNullOrWhiteSpace(moduleName) ? NotApplicable : moduleName;
        FolderName = string.IsNullOrWhiteSpace(folderName) ? NotApplicable : folderName;
    }

    public string Name { get; }

    public string ObjectType { get; }

    public string ModuleName { get; }

    public string FolderName { get; }

    public string FormatLine()
    {
        return $"Nome='{Name}' | Tipo='{ObjectType}' | Modulo='{ModuleName}' | Folder='{FolderName}'";
    }

    public static string FormatList(IReadOnlyList<ApiPlanCollisionConflict> conflicts)
    {
        if (conflicts is null)
        {
            throw new ArgumentNullException(nameof(conflicts));
        }

        if (conflicts.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.Append("Conflitos (").Append(conflicts.Count).Append("):");
        foreach (var conflict in conflicts)
        {
            builder.AppendLine();
            builder.Append("  - ").Append(conflict.FormatLine());
        }

        return builder.ToString();
    }

    public static IReadOnlyList<ApiPlanCollisionConflict> Merge(params IEnumerable<ApiPlanCollisionConflict>[] groups)
    {
        if (groups is null || groups.Length == 0)
        {
            return Array.Empty<ApiPlanCollisionConflict>();
        }

        return groups
            .SelectMany(group => group ?? Array.Empty<ApiPlanCollisionConflict>())
            .ToArray();
    }
}
