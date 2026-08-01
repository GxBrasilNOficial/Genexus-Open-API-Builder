namespace GenexusOpenApiBuilder.Extension.Diagnostics;

public static class PrototypeWizardBusinessComponentNavigationPolicy
{
    public static bool ShouldRequestEnableOnNext(bool isBusinessComponentReady, bool enableBusinessComponentRequested)
    {
        return !isBusinessComponentReady && enableBusinessComponentRequested;
    }
}
