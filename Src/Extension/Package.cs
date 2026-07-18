using System;
using Artech.Architecture.Common.Helpers;
using Artech.Architecture.Common.Objects;
using Artech.Architecture.Common.Packages;
using Artech.Architecture.Common.Services;
using Artech.Architecture.UI.Framework.Packages;
using Artech.Common.Framework.Commands;
using GenexusOpenApiBuilder.Extension.Diagnostics;

[assembly: Package(typeof(GenexusOpenApiBuilder.Extension.Package))]

namespace GenexusOpenApiBuilder.Extension;

/// <summary>
/// Ponto de entrada da extensão. As operações B004 só são registradas como
/// comandos explícitos e nunca são acionadas pela abertura de uma Knowledge Base.
/// </summary>
public sealed class Package : AbstractPackageUI
{
    public override string Name => "Genexus Open API Builder";

    public override void Initialize(IGxServiceProvider services)
    {
        base.Initialize(services);

        AddCommand(new CommandKey(Id, "B004PreflightApiObject"), ExecutePreflight, QuerySingleKnowledgeBase);
        AddCommand(new CommandKey(Id, "B004CreateApiObject"), ExecuteCreate, QuerySingleKnowledgeBase);
        AddCommand(new CommandKey(Id, "B004UpdateApiObject"), ExecuteUpdate, QuerySingleKnowledgeBase);
        AddCommand(new CommandKey(Id, "B004ReadApiObject"), ExecuteRead, QuerySingleKnowledgeBase);
        AddCommand(new CommandKey(Id, "B004DeleteApiObject"), ExecuteDelete, QuerySingleKnowledgeBase);
    }

    private static bool QuerySingleKnowledgeBase(CommandData data, ref CommandStatus status)
    {
        status.Visible(TryGetSelectedModel(data) is not null);
        return true;
    }

    private static bool ExecutePreflight(CommandData data)
    {
        return Execute(data, ApiObjectLifecycleProbe.Preflight);
    }

    private static bool ExecuteCreate(CommandData data)
    {
        return Execute(data, ApiObjectLifecycleProbe.Create);
    }

    private static bool ExecuteUpdate(CommandData data)
    {
        return Execute(data, ApiObjectLifecycleProbe.Update);
    }

    private static bool ExecuteRead(CommandData data)
    {
        return Execute(data, ApiObjectLifecycleProbe.Read);
    }

    private static bool ExecuteDelete(CommandData data)
    {
        return Execute(data, ApiObjectLifecycleProbe.Delete);
    }

    private static bool Execute(CommandData data, Func<KBModel, string> action)
    {
        var designModel = TryGetSelectedModel(data);
        if (designModel is null)
        {
            WriteOutput("[Genexus Open API Builder][B004] Selecione um único objeto da Knowledge Base antes de executar o comando.");
            return true;
        }

        try
        {
            WriteOutput($"[Genexus Open API Builder][B004] {action(designModel)}");
        }
        catch (Exception exception)
        {
            WriteOutput($"[Genexus Open API Builder][B004] Falha: {exception.Message}");
        }

        return true;
    }

    private static KBModel? TryGetSelectedModel(CommandData data)
    {
        var selectedObject = KBObjectSelectionHelper.TryGetOnlyOneKBObjectFrom(data.Context);
        return selectedObject?.Model;
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
