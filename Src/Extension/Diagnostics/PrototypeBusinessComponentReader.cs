using System;
using Artech.Genexus.Common.Objects;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Verifica, em modo somente leitura, se a Transaction selecionada pode ser
/// usada como Business Component no protótipo navegável.
/// </summary>
internal static class PrototypeBusinessComponentReader
{
    public static PrototypeBusinessComponentSnapshot Read(Transaction transaction)
    {
        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        return new PrototypeBusinessComponentSnapshot(
            transaction.Name,
            transaction.IsBusinessComponent,
            DescribeStatus(transaction.IsBusinessComponent));
    }

    private static string DescribeStatus(bool isBusinessComponent)
    {
        return isBusinessComponent
            ? "Apta via Business Component"
            : "Bloqueada: Business Component desabilitado";
    }
}

internal sealed class PrototypeBusinessComponentSnapshot
{
    public PrototypeBusinessComponentSnapshot(string transactionName, bool isBusinessComponent, string status)
    {
        TransactionName = transactionName ?? throw new ArgumentNullException(nameof(transactionName));
        IsBusinessComponent = isBusinessComponent;
        Status = status ?? throw new ArgumentNullException(nameof(status));
    }

    public string TransactionName { get; }

    public bool IsBusinessComponent { get; }

    public string Status { get; }
}
