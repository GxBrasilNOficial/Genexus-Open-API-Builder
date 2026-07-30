#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

public static class ApiPlanListProcedureReencounterPolicy
{
    private const string PageSizeVariableName = "ApiPageSize";

    public static bool IsSourceAllowed(string? currentSource, string expectedSource, IEnumerable<string> knownOwnSources)
    {
        var current = NormalizeForComparison(currentSource);
        if (string.IsNullOrWhiteSpace(current))
        {
            return true;
        }

        foreach (var knownSource in new[] { expectedSource }.Concat(knownOwnSources ?? Array.Empty<string>()))
        {
            if (string.Equals(current, NormalizeForComparison(knownSource), StringComparison.Ordinal) ||
                MatchesKnownListSourceIgnoringPagination(current, knownSource))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsRulesAllowed(string? currentRules, string expectedRules, string legacyRules)
    {
        var current = NormalizeForComparison(currentRules);
        return string.IsNullOrWhiteSpace(current) ||
            string.Equals(current, NormalizeForComparison(expectedRules), StringComparison.Ordinal) ||
            string.Equals(current, NormalizeForComparison(legacyRules), StringComparison.Ordinal);
    }

    public static bool AreVariablesAllowed(bool hasCurrentCustomVariables, params bool[] knownVariableContractsMatch)
    {
        return !hasCurrentCustomVariables || knownVariableContractsMatch.Any(matches => matches);
    }

    public static bool MatchesKnownListSourceIgnoringPagination(string currentSource, string knownSource)
    {
        return string.Equals(NormalizePaginationLiterals(currentSource), NormalizePaginationLiterals(knownSource), StringComparison.Ordinal);
    }

    private static string NormalizePaginationLiterals(string source)
    {
        var lines = NormalizeForComparison(source).Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var trimmed = lines[index].TrimStart();
            var indent = lines[index].Substring(0, lines[index].Length - trimmed.Length);
            if (IsIntegerAssignment(trimmed, $"&{PageSizeVariableName} = "))
            {
                lines[index] = indent + $"&{PageSizeVariableName} = <DefaultPageSize>";
            }
            else if (IsIntegerAssignment(trimmed, $"If &{PageSizeVariableName} > "))
            {
                lines[index] = indent + $"If &{PageSizeVariableName} > <MaximumPageSize>";
            }
        }

        return string.Join("\n", lines);
    }

    private static bool IsIntegerAssignment(string line, string prefix)
    {
        if (!line.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var value = line.Substring(prefix.Length).Trim();
        return value.Length > 0 && value.All(char.IsDigit);
    }

    private static string NormalizeForComparison(string? value)
    {
        return (value ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n").Trim();
    }
}
