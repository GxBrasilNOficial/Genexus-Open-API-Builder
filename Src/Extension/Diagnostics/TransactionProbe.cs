using System;
using System.Collections.Generic;
using System.Linq;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Lista Transactions da KB recebida pelo evento público de abertura da IDE.
/// Não cria, salva, fecha ou altera objetos GeneXus.
/// </summary>
internal static class TransactionProbe
{
    public static IReadOnlyList<string> ReadNames(KnowledgeBase knowledgeBase)
    {
        if (knowledgeBase is null)
        {
            throw new ArgumentNullException(nameof(knowledgeBase));
        }

        return Transaction.GetAll(knowledgeBase.DesignModel)
            .Select(transaction => transaction.Name)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}