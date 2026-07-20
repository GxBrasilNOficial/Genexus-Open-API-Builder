using System;
using System.Collections.Generic;
using System.Linq;
using Artech.Genexus.Common.Objects;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Le, em modo somente leitura, a chave primaria completa da Transaction
/// selecionada no prototipo navegavel.
/// </summary>
internal static class PrototypePrimaryKeyReader
{
    public static PrototypePrimaryKeySnapshot Read(Transaction transaction)
    {
        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        var parts = transaction.Structure.Root.PrimaryKey
            .Select((part, index) => new PrototypePrimaryKeyPart(
                index + 1,
                part.Name,
                part.Attribute.Type.ToString(),
                part.Attribute.Length,
                part.Attribute.Decimals))
            .ToArray();

        return new PrototypePrimaryKeySnapshot(transaction.Name, parts);
    }
}

internal sealed class PrototypePrimaryKeySnapshot
{
    public PrototypePrimaryKeySnapshot(string transactionName, IReadOnlyList<PrototypePrimaryKeyPart> parts)
    {
        TransactionName = transactionName ?? throw new ArgumentNullException(nameof(transactionName));
        Parts = parts ?? throw new ArgumentNullException(nameof(parts));
    }

    public string TransactionName { get; }

    public IReadOnlyList<PrototypePrimaryKeyPart> Parts { get; }

    public int Count => Parts.Count;

    public bool HasCompositeKey => Count > 1;
}

internal sealed class PrototypePrimaryKeyPart
{
    public PrototypePrimaryKeyPart(int order, string name, string type, int length, int decimals)
    {
        Order = order;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Length = length;
        Decimals = decimals;
    }

    public int Order { get; }

    public string Name { get; }

    public string Type { get; }

    public int Length { get; }

    public int Decimals { get; }
}
