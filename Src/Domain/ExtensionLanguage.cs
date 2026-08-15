#nullable enable

using System;
using System.Globalization;
using System.Text;

namespace GenexusOpenApiBuilder.Extension.Domain;

public enum ExtensionLanguage
{
    PortugueseBrazil,
    Spanish,
    English,
}

public static class ExtensionLanguageResolver
{
    public static ExtensionLanguage Resolve(
        string? languageName,
        string? ietfLanguageTag,
        string? rawValue)
    {
        var normalizedTag = Normalize(ietfLanguageTag);
        if (IsSpanish(normalizedTag))
        {
            return ExtensionLanguage.Spanish;
        }

        if (IsPortugueseBrazil(normalizedTag))
        {
            return ExtensionLanguage.PortugueseBrazil;
        }

        if (IsPortuguesePortugal(normalizedTag))
        {
            return ExtensionLanguage.English;
        }

        foreach (var value in new[] { languageName, rawValue })
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var normalizedValue = Normalize(value);
            if (IsSpanish(normalizedValue))
            {
                return ExtensionLanguage.Spanish;
            }

            if (IsPortugueseBrazil(normalizedValue))
            {
                return ExtensionLanguage.PortugueseBrazil;
            }
        }

        return ExtensionLanguage.English;
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = (value ?? string.Empty).Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        var normalized = builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('_', '-')
            .Replace('(', ' ')
            .Replace(')', ' ')
            .ToLowerInvariant();

        return string.Join(" ", normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
    }

    private static bool IsSpanish(string value)
    {
        return value == "es"
            || value.StartsWith("es-", StringComparison.Ordinal)
            || value.IndexOf("spanish", StringComparison.Ordinal) >= 0
            || value.IndexOf("espanol", StringComparison.Ordinal) >= 0;
    }

    private static bool IsPortugueseBrazil(string value)
    {
        return value == "pt"
            || value == "pt-br"
            || value.StartsWith("pt-br-", StringComparison.Ordinal)
            || value == "portuguese"
            || value == "portugues"
            || value.IndexOf("portuguese brazil", StringComparison.Ordinal) >= 0
            || value.IndexOf("brazil portuguese", StringComparison.Ordinal) >= 0
            || value.IndexOf("portugues brasil", StringComparison.Ordinal) >= 0
            || value.IndexOf("brasil portugues", StringComparison.Ordinal) >= 0;
    }

    private static bool IsPortuguesePortugal(string value)
    {
        return value == "pt-pt"
            || value.StartsWith("pt-pt-", StringComparison.Ordinal);
    }
}
