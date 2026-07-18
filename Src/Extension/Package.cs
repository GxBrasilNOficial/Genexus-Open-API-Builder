using Artech.Architecture.Common.Packages;
using Artech.Architecture.UI.Framework.Packages;

[assembly: Package(typeof(GenexusOpenApiBuilder.Extension.Package))]

namespace GenexusOpenApiBuilder.Extension;

/// <summary>
/// Ponto de entrada mínimo validado no B000. Não registra comandos, abre UI ou
/// acessa Knowledge Bases; o carregamento foi comprovado manualmente no U15.
/// </summary>
public sealed class Package : AbstractPackageUI
{
    public override string Name => "Genexus Open API Builder";
}
