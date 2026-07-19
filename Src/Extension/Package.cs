using Artech.Architecture.Common.Packages;
using Artech.Architecture.Common.Services;
using Artech.Architecture.UI.Framework.Packages;
using Artech.Architecture.UI.Framework.Services;
using Artech.Common.Framework.Commands;
using GenexusOpenApiBuilder.Extension.Diagnostics;

[assembly: Package(typeof(GenexusOpenApiBuilder.Extension.Package))]

namespace GenexusOpenApiBuilder.Extension;

/// <summary>
/// Ponto de entrada da extensão. As sondas B001-B006 permanecem como
/// evidências históricas e não são invocadas em runtime nem na abertura de KBs.
/// O placeholder mantém o submenu do produto visível, e o comando B020 executa
/// leitura manual e somente leitura da KB ativa para o protótipo navegável.
/// </summary>
public sealed class Package : AbstractPackageUI
{
    public override string Name => "Genexus Open API Builder";

    public override void Initialize(IGxServiceProvider services)
    {
        base.Initialize(services);

        AddCommand(new CommandKey(Id, "Futura Primeira Opção"), ExecuteFutureFirstOption, QueryFutureFirstOption);
        AddCommand(new CommandKey(Id, "Detectar KB Ativa (B020)"), ExecuteDetectActiveKnowledgeBase, QueryDetectActiveKnowledgeBase);
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
