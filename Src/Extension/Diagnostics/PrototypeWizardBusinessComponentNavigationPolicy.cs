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

    public const string DeleteRequiresBusinessComponentRefusal =
        "O serviço Delete exige Completar REST via Business Component no mesmo Apply. Não gera skeleton nem rota sem 200/404/422. Marque a etapa ou desmarque Delete. Nenhuma alteração foi feita.";

    public static bool HasDeleteService(IEnumerable<string> selectedServices)
    {
        if (selectedServices is null)
        {
            return false;
        }

        foreach (var name in selectedServices)
        {
            if (string.Equals(name, "Delete", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsDeleteAllowed(IEnumerable<string> selectedServices, bool applyBusinessComponent)
    {
        return !HasDeleteService(selectedServices) || applyBusinessComponent;
    }

    public static void ThrowIfDeleteWithoutBusinessComponent(IEnumerable<string> selectedServices, bool applyBusinessComponent)
    {
        if (!IsDeleteAllowed(selectedServices, applyBusinessComponent))
        {
            throw new InvalidOperationException(DeleteRequiresBusinessComponentRefusal);
        }
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
