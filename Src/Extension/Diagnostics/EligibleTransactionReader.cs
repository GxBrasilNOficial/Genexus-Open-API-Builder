using System;
using System.Collections.Generic;
using System.Linq;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Lê as Transactions candidatas à seleção no protótipo navegável.
/// A elegibilidade nesta etapa corresponde à enumeração pública de Transactions;
/// validações de módulo, Business Component e chave pertencem às frentes posteriores.
/// </summary>
internal static class EligibleTransactionReader
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
