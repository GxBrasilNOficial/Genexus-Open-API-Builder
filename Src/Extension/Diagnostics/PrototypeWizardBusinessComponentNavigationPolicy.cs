using System;
using System.Collections.Generic;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

public static class PrototypeWizardBusinessComponentNavigationPolicy
{
    public static bool ShouldRequestEnableBeforeLeavingWizard(bool isBusinessComponentReady, bool enableBusinessComponentRequested)
    {
        return !isBusinessComponentReady && enableBusinessComponentRequested;
    }

    public static bool HasGetCreateUpdateServices(IEnumerable<string> selectedServices)
    {
        if (selectedServices is null)
        {
            return false;
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in selectedServices)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                names.Add(name);
            }
        }

        return names.Contains("Get") && names.Contains("Create") && names.Contains("Update");
    }

    public static bool ShouldAllowApplyBusinessComponent(
        bool isBusinessComponentReady,
        bool enableBusinessComponentRequested,
        bool sdtsAvailable,
        bool proceduresAvailable,
        bool apiObjectAvailable,
        bool hasGetCreateUpdateServices)
    {
        return hasGetCreateUpdateServices &&
            (isBusinessComponentReady || enableBusinessComponentRequested) &&
            sdtsAvailable &&
            proceduresAvailable &&
            apiObjectAvailable;
    }

    public static bool ShouldApplyBusinessComponentWhenAllowed(bool canApplyBusinessComponent, bool currentApplySelection, bool pendingApplySelection)
    {
        return canApplyBusinessComponent && (currentApplySelection || pendingApplySelection);
    }

    public static bool ResolveApplyBusinessComponentAfterGenerationRefresh(
        bool isBusinessComponentReady,
        bool enableBusinessComponentRequested,
        bool sdtsAvailable,
        bool proceduresAvailable,
        bool apiObjectAvailable,
        bool hasGetCreateUpdateServices,
        bool currentApplySelection,
        bool pendingApplySelection)
    {
        return ShouldApplyBusinessComponentWhenAllowed(
            ShouldAllowApplyBusinessComponent(
                isBusinessComponentReady,
                enableBusinessComponentRequested,
                sdtsAvailable,
                proceduresAvailable,
                apiObjectAvailable,
                hasGetCreateUpdateServices),
            currentApplySelection,
            pendingApplySelection);
    }
}
