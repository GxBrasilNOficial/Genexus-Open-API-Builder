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
/// O placeholder mantém o submenu do produto visível, e os comandos B020/B021/B022
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
}
