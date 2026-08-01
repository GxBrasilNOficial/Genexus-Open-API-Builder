namespace GenexusOpenApiBuilder.Extension.Diagnostics;

public static class PrototypeWizardBusinessComponentNavigationPolicy
{
    public static bool ShouldRequestEnableBeforeLeavingWizard(bool isBusinessComponentReady, bool enableBusinessComponentRequested)
    {
        return !isBusinessComponentReady && enableBusinessComponentRequested;
    }

    public static bool ShouldAllowApplyBusinessComponent(
        bool isBusinessComponentReady,
        bool enableBusinessComponentRequested,
        bool sdtsAvailable,
        bool proceduresAvailable,
        bool apiObjectAvailable)
    {
        return (isBusinessComponentReady || enableBusinessComponentRequested) &&
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
        bool currentApplySelection,
        bool pendingApplySelection)
    {
        return ShouldApplyBusinessComponentWhenAllowed(
            ShouldAllowApplyBusinessComponent(
                isBusinessComponentReady,
                enableBusinessComponentRequested,
                sdtsAvailable,
                proceduresAvailable,
                apiObjectAvailable),
            currentApplySelection,
            pendingApplySelection);
    }
}
