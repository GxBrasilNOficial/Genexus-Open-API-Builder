using Artech.Architecture.Common.Packages;
using Artech.Architecture.UI.Framework.Packages;

[assembly: Package(typeof(GenexusOpenApiBuilder.Extension.Package))]

namespace GenexusOpenApiBuilder.Extension;

/// <summary>
/// Ponto de entrada mínimo validado no B000. A configuração atual não acessa
/// Knowledge Bases nem executa sondas automaticamente. As sondas B001–B003
/// permanecem somente como evidência histórica e não são invocadas em runtime.
/// </summary>
public sealed class Package : AbstractPackageUI
{
    public override string Name => "Genexus Open API Builder";
}
