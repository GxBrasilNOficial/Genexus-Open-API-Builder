#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>B110: composição pura, compartilhada pelo retorno inicial e pelo preflight.</summary>
public static class ApiPlanWriteBlockMessage
{
    public static string Build(string prefix, IEnumerable<string> stages,
        IReadOnlyList<ApiPlanCollisionConflict> collisions, string suffix,
        IEnumerable<string>? details = null)
    {
        var message = prefix + string.Join(", ", stages);
        if (details is not null)
        {
            message += ". " + string.Join(" | ", details.Where(detail => !string.IsNullOrWhiteSpace(detail)));
        }
        if (collisions.Count > 0)
        {
            message += ". " + ApiPlanCollisionConflict.FormatList(collisions);
        }
        return message + suffix;
    }
}
