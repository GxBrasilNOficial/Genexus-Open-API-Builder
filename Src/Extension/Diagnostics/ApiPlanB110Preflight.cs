#nullable enable
using System;
using System.Collections.Generic;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>Bloqueio B110 identificado separadamente das falhas do preflight agregado.</summary>
public sealed class ApiPlanB110PreflightException : InvalidOperationException
{
    public const string ReportCode = "B110";

    public ApiPlanB110PreflightException(string message) : base(message) { }
}

public static class ApiPlanB110Preflight
{
    public static void RequireExistingName(bool hasExistingApi, Guid? resolvedGuid,
        string? existingName, string plannedName)
    {
        if (ApiPlanExistingNamePolicy.IsRenameBlocked(hasExistingApi, resolvedGuid, existingName, plannedName))
        {
            throw new ApiPlanB110PreflightException(ApiPlanExistingNamePolicy.Describe(existingName, plannedName));
        }
    }

    public static void RequireReconstructedContract(bool imported, bool hasLevelsKey,
        bool hasSublevels, IEnumerable<string> recalculatedSections,
        ReconstructedContractAcknowledgement? acknowledgement)
    {
        if (ReconstructedContractAcknowledgement.IsAccepted(imported, hasLevelsKey, hasSublevels,
            recalculatedSections, acknowledgement)) return;

        throw new ApiPlanB110PreflightException(
            ReconstructedContractAcknowledgement.IsHierarchyBlocked(imported, hasLevelsKey, hasSublevels)
                ? ReconstructedContractAcknowledgement.HierarchyBlocked
                : ReconstructedContractAcknowledgement.ConfirmationRequired);
    }
}
