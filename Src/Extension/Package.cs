using Artech.Architecture.Common.Events;
using Artech.Architecture.Common.Packages;
using Artech.Architecture.Common.Services;
using Artech.Architecture.UI.Framework.Packages;
using GenexusOpenApiBuilder.Extension.Diagnostics;

[assembly: Package(typeof(GenexusOpenApiBuilder.Extension.Package))]

namespace GenexusOpenApiBuilder.Extension;

/// <summary>
/// Ponto de entrada mínimo validado no B000. Não registra comandos, abre UI ou
/// acessa Knowledge Bases; o carregamento foi comprovado manualmente no U15.
/// </summary>
public sealed class Package : AbstractPackageUI
{
    public override string Name => "Genexus Open API Builder";

    public override void OnAfterOpenKB(object sender, KBEventArgs e)
    {
        base.OnAfterOpenKB(sender, e);

        var activeKnowledgeBase = ActiveKnowledgeBaseProbe.TryRead(e.KB);
        if (activeKnowledgeBase is null)
        {
            WriteOutput("[Genexus Open API Builder][B001] Nenhuma Knowledge Base ativa estava disponível para leitura.");
            return;
        }

        WriteOutput(
            $"[Genexus Open API Builder][B001] Knowledge Base ativa detectada: " +
            $"Name='{activeKnowledgeBase.Name}', Guid='{activeKnowledgeBase.Guid}', Location='{activeKnowledgeBase.Location}'.");

        var transactions = TransactionProbe.ReadNames(e.KB);
        WriteOutput($"[Genexus Open API Builder][B002] Transactions encontradas: {transactions.Count}.");
        foreach (var transactionName in transactions)
        {
            WriteOutput($"[Genexus Open API Builder][B002] Transaction: {transactionName}");
        }
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