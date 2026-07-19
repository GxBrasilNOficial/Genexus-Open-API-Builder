using Artech.Architecture.Common.Packages;
using Artech.Architecture.Common.Services;
using Artech.Architecture.UI.Framework.Packages;
using Artech.Common.Framework.Commands;

[assembly: Package(typeof(GenexusOpenApiBuilder.Extension.Package))]

namespace GenexusOpenApiBuilder.Extension;

/// <summary>
/// Ponto de entrada passivo da extensão. As sondas B001-B005 permanecem como
/// evidências históricas e não são invocadas em runtime, nem na abertura de KBs.
/// O comando placeholder mantém o submenu do produto visível sem ler ou escrever na KB.
/// </summary>
public sealed class Package : AbstractPackageUI
{
    public override string Name => "Genexus Open API Builder";

    public override void Initialize(IGxServiceProvider services)
    {
        base.Initialize(services);

        AddCommand(new CommandKey(Id, "Futura Primeira Opção"), ExecuteFutureFirstOption, QueryFutureFirstOption);
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
}
