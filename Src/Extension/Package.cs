using Artech.Architecture.Common.Packages;
using Artech.Architecture.UI.Framework.Packages;

[assembly: Package(typeof(GenexusOpenApiBuilder.Extension.Package))]

namespace GenexusOpenApiBuilder.Extension;

/// <summary>
/// Ponto de entrada passivo da extensão. As sondas B001–B004 permanecem como
/// evidências históricas e não são invocadas em runtime, nem na abertura de KBs.
/// </summary>
public sealed class Package : AbstractPackageUI
{
    public override string Name => "Genexus Open API Builder";
}
