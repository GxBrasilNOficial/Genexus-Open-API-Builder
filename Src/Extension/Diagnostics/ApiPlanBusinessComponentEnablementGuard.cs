#nullable enable

using System;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Mantém o bloqueio B055 até que o Wizard possa habilitar o Business Component
/// antes de persistir qualquer consumidor dependente.
/// </summary>
internal static class ApiPlanBusinessComponentEnablementGuard
{
    internal static void ThrowIfBlocked(
        bool applyBusinessComponent,
        bool isBusinessComponent,
        bool businessComponentEnablementPending,
        string transactionName)
    {
        if (!applyBusinessComponent || isBusinessComponent || businessComponentEnablementPending)
        {
            return;
        }

        throw new InvalidOperationException(
            $"B055 bloqueado: Transaction='{transactionName}' esta com Business Component desabilitado. Nenhuma alteracao foi feita.");
    }
}
