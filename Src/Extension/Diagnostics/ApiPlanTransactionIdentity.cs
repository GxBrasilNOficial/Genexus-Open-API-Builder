#nullable enable

using System;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Regra única de identidade para impedir que um ApiPlan seja aplicado em outra
/// Transaction que apenas compartilhe o mesmo nome.
/// </summary>
public static class ApiPlanTransactionIdentity
{
    public static bool Matches(
        Guid actualTransactionGuid,
        string actualTransactionName,
        Guid expectedTransactionGuid,
        string expectedTransactionName)
    {
        if (actualTransactionName is null)
        {
            throw new ArgumentNullException(nameof(actualTransactionName));
        }

        if (expectedTransactionName is null)
        {
            throw new ArgumentNullException(nameof(expectedTransactionName));
        }

        return actualTransactionGuid != Guid.Empty
            && expectedTransactionGuid != Guid.Empty
            && actualTransactionGuid == expectedTransactionGuid
            && string.Equals(actualTransactionName, expectedTransactionName, StringComparison.Ordinal);
    }

    public static string BuildMismatchMessage(
        string operationCode,
        Guid actualTransactionGuid,
        string actualTransactionName,
        Guid expectedTransactionGuid,
        string expectedTransactionName)
    {
        if (operationCode is null)
        {
            throw new ArgumentNullException(nameof(operationCode));
        }

        if (actualTransactionName is null)
        {
            throw new ArgumentNullException(nameof(actualTransactionName));
        }

        if (expectedTransactionName is null)
        {
            throw new ArgumentNullException(nameof(expectedTransactionName));
        }

        return $"{operationCode} bloqueado: o ApiPlan em memoria nao pertence a Transaction selecionada atual. " +
            $"TransactionGuid esperado='{expectedTransactionGuid}', atual='{actualTransactionGuid}'; " +
            $"TransactionName esperado='{expectedTransactionName}', atual='{actualTransactionName}'. " +
            "Nenhuma alteracao foi feita.";
    }
}
