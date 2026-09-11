#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

internal static class ApiPlanSdtMemberSequenceMatcher
{
    public static bool TryMatch(
        IReadOnlyList<string> actualNames,
        IReadOnlyList<string> plannedNames,
        IReadOnlyCollection<string>? allowedMissingNames,
        out IReadOnlyList<int> actualIndexByPlannedIndex,
        out string? mismatch)
    {
        if (actualNames is null) throw new ArgumentNullException(nameof(actualNames));
        if (plannedNames is null) throw new ArgumentNullException(nameof(plannedNames));

        mismatch = null;
        actualIndexByPlannedIndex = Array.Empty<int>();
        if (allowedMissingNames is null)
        {
            if (actualNames.Count != plannedNames.Count)
            {
                mismatch = "qtd=" + actualNames.Count + " (esperada " + plannedNames.Count +
                    "); nomes='" + string.Join(",", actualNames) + "'";
                return false;
            }

            var strictIndexes = new int[plannedNames.Count];
            for (var index = 0; index < plannedNames.Count; index++)
            {
                if (!string.Equals(actualNames[index], plannedNames[index], StringComparison.OrdinalIgnoreCase))
                {
                    mismatch = "ordem '" + actualNames[index] + "' != '" + plannedNames[index] + "' @ " + index;
                    return false;
                }

                strictIndexes[index] = index;
            }

            actualIndexByPlannedIndex = strictIndexes;
            return true;
        }

        var allowedMissing = new HashSet<string>(allowedMissingNames, StringComparer.OrdinalIgnoreCase);
        var indexes = Enumerable.Repeat(-1, plannedNames.Count).ToArray();
        var missingNames = new List<string>();
        var actualIndex = 0;
        for (var plannedIndex = 0; plannedIndex < plannedNames.Count; plannedIndex++)
        {
            var plannedName = plannedNames[plannedIndex];
            if (actualIndex >= actualNames.Count)
            {
                missingNames.Add(plannedName);
                continue;
            }

            var actualName = actualNames[actualIndex];
            if (!string.Equals(actualName, plannedName, StringComparison.OrdinalIgnoreCase))
            {
                if (allowedMissing.Contains(plannedName))
                {
                    missingNames.Add(plannedName);
                    continue;
                }

                mismatch = "ordem '" + actualName + "' != '" + plannedName + "' @ " + actualIndex;
                return false;
            }

            indexes[plannedIndex] = actualIndex;
            actualIndex++;
        }

        if (actualIndex < actualNames.Count)
        {
            mismatch = "membros extras '" + string.Join(",", actualNames.Skip(actualIndex)) + "'";
            return false;
        }

        var disallowedMissing = missingNames
            .Where(name => !allowedMissing.Contains(name))
            .ToArray();
        if (disallowedMissing.Length > 0)
        {
            mismatch = "membros ausentes '" + string.Join(",", disallowedMissing) + "'";
            return false;
        }

        actualIndexByPlannedIndex = indexes;
        return true;
    }
}
