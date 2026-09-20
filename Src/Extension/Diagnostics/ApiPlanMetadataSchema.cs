#nullable enable

using System;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Versões da metadata de negócio (<c>schemaVersion</c> do File da API).
/// Lista única para writer, remoção e diário — sem cópias privadas.
/// </summary>
internal static class ApiPlanMetadataSchema
{
    internal const string V1 = "GOAB_API_METADATA_B060_V1";
    internal const string V2 = "GOAB_API_METADATA_B060_V2";
    internal const string V3 = "GOAB_API_METADATA_B060_V3";

    /// <summary>
    /// Versão emitida por Apply, Sync e B115. A promoção V3→V4 é aditiva:
    /// acrescenta <c>objects.transactionFolder.guid</c> e
    /// <c>objects.transactionFolder.ownedByThisApi</c>.
    /// </summary>
    internal const string V4 = "GOAB_API_METADATA_B060_V4";

    internal const string Current = V4;

    internal static readonly string[] Supported =
    {
        V1,
        V2,
        V3,
        V4,
    };

    internal static bool IsSupported(string? schemaVersion)
    {
        if (string.IsNullOrWhiteSpace(schemaVersion))
        {
            return false;
        }

        for (var index = 0; index < Supported.Length; index++)
        {
            if (string.Equals(schemaVersion, Supported[index], StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Lista em português para mensagens de recusa, por exemplo
    /// <c>V1, V2, V3 ou V4</c>. A localização traduz o fragmento inteiro.
    /// </summary>
    internal static string FormatSupportedVersionList()
    {
        var suffixes = new string[Supported.Length];
        for (var index = 0; index < Supported.Length; index++)
        {
            var value = Supported[index];
            var separator = value.LastIndexOf('_');
            suffixes[index] = separator >= 0 && separator < value.Length - 1
                ? value.Substring(separator + 1)
                : value;
        }

        if (suffixes.Length == 1)
        {
            return suffixes[0];
        }

        return string.Join(", ", suffixes, 0, suffixes.Length - 1) + " ou " + suffixes[suffixes.Length - 1];
    }
}
