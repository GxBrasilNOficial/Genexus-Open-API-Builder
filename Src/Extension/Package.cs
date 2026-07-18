using Artech.Architecture.Common.Packages;
using Artech.Architecture.UI.Framework.Packages;

[assembly: Package(typeof(GenexusOpenApiBuilder.Extension.Package))]

namespace GenexusOpenApiBuilder.Extension;

/// <summary>
/// Ponto de entrada mínimo sob teste no B000. Não registra comandos, abre UI ou
/// acessa Knowledge Bases; o carregamento será comprovado manualmente na IDE.
/// </summary>
public sealed class Package : AbstractPackageUI
{
    public override string Name => "Genexus Open API Builder";
}
