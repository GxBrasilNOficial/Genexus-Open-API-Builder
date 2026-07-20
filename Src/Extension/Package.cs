using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Artech.Architecture.Common.Descriptors;
using Artech.Architecture.Common.Packages;
using Artech.Architecture.Common.Services;
using Artech.Architecture.UI.Framework.Packages;
using Artech.Architecture.UI.Framework.Services;
using Artech.Common.Framework.Commands;
using Artech.Genexus.Common.Objects;
using GenexusOpenApiBuilder.Extension.Diagnostics;

[assembly: Package(typeof(GenexusOpenApiBuilder.Extension.Package))]

namespace GenexusOpenApiBuilder.Extension;

/// <summary>
/// Ponto de entrada da extensão. As sondas B001-B006 permanecem como
/// evidências históricas e não são invocadas em runtime nem na abertura de KBs.
/// O placeholder mantém o submenu do produto visível, e os comandos B020-B025
/// executam leituras manuais e somente leitura para o protótipo navegável.
/// </summary>
public sealed class Package : AbstractPackageUI
{
    public override string Name => "Genexus Open API Builder";

    public override void Initialize(IGxServiceProvider services)
    {
        base.Initialize(services);

        AddCommand(new CommandKey(Id, "Futura Primeira Opção"), ExecuteFutureFirstOption, QueryFutureFirstOption);
        AddCommand(new CommandKey(Id, "Detectar KB Ativa (B020)"), ExecuteDetectActiveKnowledgeBase, QueryDetectActiveKnowledgeBase);
        AddCommand(new CommandKey(Id, "Listar Transactions Elegíveis (B021)"), ExecuteListEligibleTransactions, QueryListEligibleTransactions);
        AddCommand(new CommandKey(Id, "Selecionar Transaction e Ler Módulo (B022)"), ExecuteSelectTransactionAndReadModule, QuerySelectTransactionAndReadModule);
        AddCommand(new CommandKey(Id, "Detectar Objetos Existentes (B023)"), ExecuteDetectExistingObjects, QueryDetectExistingObjects);
        AddCommand(new CommandKey(Id, "Verificar Business Component (B024)"), ExecuteCheckBusinessComponent, QueryCheckBusinessComponent);
        AddCommand(new CommandKey(Id, "Ler Chave Primária (B025)"), ExecuteReadPrimaryKey, QueryReadPrimaryKey);
    }

    private static bool QueryFutureFirstOption(CommandData data, ref CommandStatus status)
    {
        status.Visible(true);
        return true;
    }

    private static bool ExecuteFutureFirstOption(CommandData data)
    {
        return true;
    }

    private static bool QueryDetectActiveKnowledgeBase(CommandData data, ref CommandStatus status)
    {
        status.Visible(true);
        return true;
    }

    private static bool ExecuteDetectActiveKnowledgeBase(CommandData data)
    {
        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        var snapshot = ActiveKnowledgeBaseProbe.TryRead(knowledgeBase);

        WriteOutput(
            snapshot is null
                ? "[Genexus Open API Builder][B020] Nenhuma Knowledge Base ativa foi encontrada. Abra uma KB e execute o comando novamente."
                : $"[Genexus Open API Builder][B020] Knowledge Base ativa detectada: Name='{snapshot.Name}', Guid='{snapshot.Guid}', Location='{snapshot.Location}'.");

        return true;
    }

    private static bool QueryListEligibleTransactions(CommandData data, ref CommandStatus status)
    {
        status.Visible(true);
        return true;
    }

    private static bool ExecuteListEligibleTransactions(CommandData data)
    {
        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        if (knowledgeBase is null)
        {
            WriteOutput("[Genexus Open API Builder][B021] Nenhuma Knowledge Base ativa foi encontrada. Abra uma KB e execute o comando novamente.");
            return true;
        }

        var transactionNames = EligibleTransactionReader.ReadNames(knowledgeBase);
        if (transactionNames.Count == 0)
        {
            WriteOutput("[Genexus Open API Builder][B021] Nenhuma Transaction elegível foi encontrada na Knowledge Base ativa.");
            return true;
        }

        WriteOutput($"[Genexus Open API Builder][B021] Transactions elegíveis encontradas: Total={transactionNames.Count}.");
        foreach (var transactionName in transactionNames)
        {
            WriteOutput($"[Genexus Open API Builder][B021] Transaction elegível: Name='{transactionName}'.");
        }

        return true;
    }

    private static bool QuerySelectTransactionAndReadModule(CommandData data, ref CommandStatus status)
    {
        status.Visible(true);
        return true;
    }

    private static bool ExecuteSelectTransactionAndReadModule(CommandData data)
    {
        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        if (knowledgeBase is null)
        {
            WriteOutput("[Genexus Open API Builder][B022] Nenhuma Knowledge Base ativa foi encontrada. Abra uma KB e execute o comando novamente.");
            return true;
        }

        PrototypeTransactionSelectionState.ClearIfKnowledgeBaseChanged(knowledgeBase);

        if (!UIServices.IsSelectObjectDialogAvailable)
        {
            WriteOutput("[Genexus Open API Builder][B022] O diálogo público de seleção não está disponível nesta IDE.");
            return true;
        }

        var options = new SelectObjectOptions
        {
            MultipleSelection = false,
            DialogTitle = "Selecionar Transaction para ler módulo (B022)",
            SupportCreateAction = false
        };
        options.ObjectTypes.Add(KBObjectDescriptor.Get<Transaction>());

        var selectedObject = UIServices.SelectObjectDialog.SelectObject(options);
        if (selectedObject is null)
        {
            WriteOutput("[Genexus Open API Builder][B022] Nenhuma Transaction foi selecionada.");
            return true;
        }

        if (selectedObject is not Transaction transaction)
        {
            WriteOutput("[Genexus Open API Builder][B022] A seleção retornada não é uma Transaction. Nenhuma escolha foi mantida.");
            return true;
        }

        var module = transaction.Module;
        if (module is null)
        {
            WriteOutput($"[Genexus Open API Builder][B022] A Transaction selecionada não possui módulo disponível: Name='{transaction.Name}'.");
            return true;
        }

        PrototypeTransactionSelectionState.Store(knowledgeBase, transaction);
        WriteOutput($"[Genexus Open API Builder][B022] Transaction selecionada: Name='{transaction.Name}'.");
        WriteOutput($"[Genexus Open API Builder][B022] Módulo da Transaction: Name='{module.Name}'.");

        return true;
    }

    private static bool QueryDetectExistingObjects(CommandData data, ref CommandStatus status)
    {
        status.Visible(true);
        return true;
    }

    private static bool ExecuteDetectExistingObjects(CommandData data)
    {
        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        if (knowledgeBase is null)
        {
            WriteOutput("[Genexus Open API Builder][B023] Nenhuma Knowledge Base ativa foi encontrada. Abra uma KB e execute o comando novamente.");
            return true;
        }

        PrototypeTransactionSelectionState.ClearIfKnowledgeBaseChanged(knowledgeBase);
        var selectedTransaction = PrototypeTransactionSelectionState.Current;
        if (selectedTransaction is null)
        {
            WriteOutput("[Genexus Open API Builder][B023] Nenhuma Transaction selecionada em memória. Execute primeiro o comando B022.");
            return true;
        }

        var transaction = Transaction.GetAll(knowledgeBase.DesignModel)
            .SingleOrDefault(item => item.Guid == selectedTransaction.TransactionGuid);
        if (transaction is null)
        {
            WriteOutput($"[Genexus Open API Builder][B023] A Transaction selecionada em memória não foi reencontrada: Name='{selectedTransaction.TransactionName}', Guid='{selectedTransaction.TransactionGuid}'. Nenhuma escolha foi persistida.");
            return true;
        }

        var snapshot = PrototypeExistingObjectReader.Read(knowledgeBase.DesignModel, transaction);
        WriteOutput($"[Genexus Open API Builder][B023] Transaction selecionada: Name='{snapshot.TransactionName}', MetadataFile='{snapshot.MetadataFileName}'.");
        WriteOutput($"[Genexus Open API Builder][B023] Objetos planejados verificados: Total={snapshot.Results.Count}, Existentes={snapshot.ExistingCount}, Ausentes={snapshot.MissingCount}.");
        foreach (var result in snapshot.Results)
        {
            WriteOutput($"[Genexus Open API Builder][B023] {result.ObjectType}: Name='{result.Name}', Count={result.Count}, Status='{result.Status}'.");
        }

        return true;
    }

    private static bool QueryCheckBusinessComponent(CommandData data, ref CommandStatus status)
    {
        status.Visible(true);
        return true;
    }

    private static bool ExecuteCheckBusinessComponent(CommandData data)
    {
        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        if (knowledgeBase is null)
        {
            WriteOutput("[Genexus Open API Builder][B024] Nenhuma Knowledge Base ativa foi encontrada. Abra uma KB e execute o comando novamente.");
            return true;
        }

        PrototypeTransactionSelectionState.ClearIfKnowledgeBaseChanged(knowledgeBase);
        var selectedTransaction = PrototypeTransactionSelectionState.Current;
        if (selectedTransaction is null)
        {
            WriteOutput("[Genexus Open API Builder][B024] Nenhuma Transaction selecionada em memória. Execute primeiro o comando B022.");
            return true;
        }

        var transaction = Transaction.GetAll(knowledgeBase.DesignModel)
            .SingleOrDefault(item => item.Guid == selectedTransaction.TransactionGuid);
        if (transaction is null)
        {
            WriteOutput($"[Genexus Open API Builder][B024] A Transaction selecionada em memória não foi reencontrada: Name='{selectedTransaction.TransactionName}', Guid='{selectedTransaction.TransactionGuid}'. Nenhuma escolha foi persistida.");
            return true;
        }

        var snapshot = PrototypeBusinessComponentReader.Read(transaction);
        WriteOutput($"[Genexus Open API Builder][B024] Transaction selecionada: Name='{snapshot.TransactionName}', IsBusinessComponent={snapshot.IsBusinessComponent}.");
        WriteOutput($"[Genexus Open API Builder][B024] Resultado da verificação: Status='{snapshot.Status}'.");

        return true;
    }

    private static bool QueryReadPrimaryKey(CommandData data, ref CommandStatus status)
    {
        status.Visible(true);
        return true;
    }

    private static bool ExecuteReadPrimaryKey(CommandData data)
    {
        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        if (knowledgeBase is null)
        {
            WriteOutput("[Genexus Open API Builder][B025] Nenhuma Knowledge Base ativa foi encontrada. Abra uma KB e execute o comando novamente.");
            return true;
        }

        PrototypeTransactionSelectionState.ClearIfKnowledgeBaseChanged(knowledgeBase);

        var transaction = TryResolveTransactionFromCommandData(data);
        if (transaction is not null)
        {
            var transactionGuid = transaction.Guid;
            transaction = Transaction.GetAll(knowledgeBase.DesignModel)
                .SingleOrDefault(item => item.Guid == transactionGuid);
            if (transaction is null)
            {
                WriteOutput("[Genexus Open API Builder][B025] A Transaction do menu de contexto não foi reencontrada na Knowledge Base ativa. Nenhuma escolha foi persistida.");
                return true;
            }

            PrototypeTransactionSelectionState.Store(knowledgeBase, transaction);
        }
        else
        {
            var selectedTransaction = PrototypeTransactionSelectionState.Current;
            if (selectedTransaction is null)
            {
                WriteOutput("[Genexus Open API Builder][B025] Nenhuma Transaction selecionada. Use o menu de contexto de uma Transaction ou execute primeiro o comando B022.");
                return true;
            }

            transaction = Transaction.GetAll(knowledgeBase.DesignModel)
                .SingleOrDefault(item => item.Guid == selectedTransaction.TransactionGuid);
            if (transaction is null)
            {
                WriteOutput($"[Genexus Open API Builder][B025] A Transaction selecionada em memória não foi reencontrada: Name='{selectedTransaction.TransactionName}', Guid='{selectedTransaction.TransactionGuid}'. Nenhuma escolha foi persistida.");
                return true;
            }
        }

        var snapshot = PrototypePrimaryKeyReader.Read(transaction);
        WriteOutput($"[Genexus Open API Builder][B025] Transaction selecionada: Name='{snapshot.TransactionName}', PrimaryKeyParts={snapshot.Count}, HasCompositeKey={snapshot.HasCompositeKey}.");
        foreach (var part in snapshot.Parts)
        {
            WriteOutput($"[Genexus Open API Builder][B025] KeyPart: Order={part.Order}, Name='{part.Name}', Type='{part.Type}', Length={part.Length}, Decimals={part.Decimals}.");
        }

        return true;
    }

    private static Transaction? TryResolveTransactionFromCommandData(CommandData data)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        return TryFindTransaction(data.Context, visited, 0) ?? TryFindTransaction(data.Parameters, visited, 0);
    }

    private static Transaction? TryFindTransaction(object? value, ISet<object> visited, int depth)
    {
        if (value is null || depth > 4)
        {
            return null;
        }

        if (value is Transaction transaction)
        {
            return transaction;
        }

        var type = value.GetType();
        if (type.IsPrimitive || value is string || value is Guid || value is DateTime)
        {
            return null;
        }

        if (type.Name is "KnowledgeBase" or "KBModel" || !visited.Add(value))
        {
            return null;
        }

        if (value is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                var transactionFromItem = TryFindTransaction(item, visited, depth + 1);
                if (transactionFromItem is not null)
                {
                    return transactionFromItem;
                }
            }
        }

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length != 0)
            {
                continue;
            }

            try
            {
                var transactionFromProperty = TryFindTransaction(property.GetValue(value), visited, depth + 1);
                if (transactionFromProperty is not null)
                {
                    return transactionFromProperty;
                }
            }
            catch (Exception)
            {
            }
        }

        return null;
    }

    private static void WriteOutput(string message)
    {
        if (!CommonServices.IsOutputAvailable)
        {
            return;
        }

        var output = CommonServices.Output;
        if (output is not IOutputService2 outputWithDefault)
        {
            return;
        }

        var outputId = outputWithDefault.DefaultOutputId;
        output.AddLine(outputId, message);
        output.Show(outputId);
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        public new bool Equals(object? x, object? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
