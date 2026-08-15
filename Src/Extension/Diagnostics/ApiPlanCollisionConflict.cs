#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Residual B083 — conflito apresentado ao usuário com contexto e diagnóstico detalhado quando disponível.
/// </summary>
public sealed class ApiPlanCollisionConflict
{
    public const string NotApplicable = "(n/a)";

    public ApiPlanCollisionConflict(
        string name,
        string objectType,
        string moduleName,
        string folderName,
        string? diagnosticReason = null,
        string? apiObjectGuid = null,
        string? metadataApiGuid = null,
        string? diagnosticDetails = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ObjectType = objectType ?? throw new ArgumentNullException(nameof(objectType));
        ModuleName = string.IsNullOrWhiteSpace(moduleName) ? NotApplicable : moduleName;
        FolderName = string.IsNullOrWhiteSpace(folderName) ? NotApplicable : folderName;
        DiagnosticReason = diagnosticReason;
        ApiObjectGuid = apiObjectGuid;
        MetadataApiGuid = metadataApiGuid;
        DiagnosticDetails = diagnosticDetails;
    }

    public string Name { get; }

    public string ObjectType { get; }

    public string ModuleName { get; }

    public string FolderName { get; }

    public string? DiagnosticReason { get; }

    public string? ApiObjectGuid { get; }

    public string? MetadataApiGuid { get; }

    public string? DiagnosticDetails { get; }

    public string FormatLine()
    {
        var line = $"Nome='{Name}' | Tipo='{ObjectType}' | Modulo='{ModuleName}' | Folder='{FolderName}'";
        if (string.IsNullOrWhiteSpace(DiagnosticReason))
        {
            return line;
        }

        line += $" | Causa='{DiagnosticReason}'";
        if (!string.IsNullOrWhiteSpace(ApiObjectGuid))
        {
            line += $" | ApiObjectGuid='{ApiObjectGuid}'";
        }

        if (!string.IsNullOrWhiteSpace(MetadataApiGuid))
        {
            line += $" | MetadataApiGuid='{MetadataApiGuid}'";
        }

        return line;
    }

    public string FormatDiagnosticDetails()
    {
        return DiagnosticDetails ?? string.Empty;
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
            var details = conflict.FormatDiagnosticDetails();
            if (!string.IsNullOrWhiteSpace(details))
            {
                foreach (var detailLine in details.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
                {
                    builder.AppendLine();
                    builder.Append("      ").Append(detailLine);
                }
            }
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
