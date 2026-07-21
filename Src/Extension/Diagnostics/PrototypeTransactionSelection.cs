using System;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Mantém exclusivamente em memória a identidade da Transaction escolhida
/// no protótipo navegável. Nenhuma escolha é persistida na Knowledge Base.
/// </summary>
internal sealed class PrototypeTransactionSelection
{
    public PrototypeTransactionSelection(Guid knowledgeBaseGuid, Guid transactionGuid, string transactionName)
    {
        KnowledgeBaseGuid = knowledgeBaseGuid;
        TransactionGuid = transactionGuid;
        TransactionName = transactionName ?? throw new ArgumentNullException(nameof(transactionName));
    }

    public Guid KnowledgeBaseGuid { get; }

    public Guid TransactionGuid { get; }

    public string TransactionName { get; }
}

internal static class PrototypeTransactionSelectionState
{
    public static PrototypeTransactionSelection? Current { get; private set; }

    public static void ClearIfKnowledgeBaseChanged(KnowledgeBase knowledgeBase)
    {
        if (knowledgeBase is null)
        {
            throw new ArgumentNullException(nameof(knowledgeBase));
        }

        if (Current is not null && Current.KnowledgeBaseGuid != knowledgeBase.Guid)
        {
            Current = null;
        }
    }

    public static void Clear()
    {
        Current = null;
    }

    public static void Store(KnowledgeBase knowledgeBase, Transaction transaction)
    {
        if (knowledgeBase is null)
        {
            throw new ArgumentNullException(nameof(knowledgeBase));
        }

        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        Current = new PrototypeTransactionSelection(knowledgeBase.Guid, transaction.Guid, transaction.Name);
    }
}
