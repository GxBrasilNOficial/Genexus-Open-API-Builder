#nullable enable
using System;
namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>B110: renomear API existente não é suportado, inclusive no Apply parcial.</summary>
public static class ApiPlanExistingNamePolicy
{
    public static bool IsRenameBlocked(bool hasExistingApi, Guid? resolvedGuid, string? existingName, string plannedName)
        => hasExistingApi && resolvedGuid.HasValue
            && !string.Equals(existingName, plannedName, StringComparison.OrdinalIgnoreCase);

    public static string Describe(string? existingName, string plannedName)
        => $"Renomeação de API existente bloqueada: nome existente='{existingName}', nome planejado='{plannedName}'. Mantenha o nome existente. Nenhuma escrita foi solicitada.";
}
