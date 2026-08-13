using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Identifica atributos cobertos por regras NoAccept sem depender de propriedades internas do SDK.
/// O leitor recebe somente o texto persistido das Rules da Transaction.
/// </summary>
internal static class PrototypeWizardNoAcceptRuleReader
{
    private static readonly Regex NoAcceptRulePattern = new(
        @"(?:^|[;{}\r\n])\s*noaccept\s*\(\s*(?<attribute>[A-Za-z_][A-Za-z0-9_]*)\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static IReadOnlyCollection<string> ReadAttributeNames(string source)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(source))
        {
            return names;
        }

        var sourceWithoutComments = RemoveComments(source);
        foreach (Match match in NoAcceptRulePattern.Matches(sourceWithoutComments))
        {
            var name = match.Groups["attribute"].Value;
            if (!string.IsNullOrWhiteSpace(name))
            {
                names.Add(name);
            }
        }

        return names;
    }

    private static string RemoveComments(string source)
    {
        var result = new StringBuilder(source.Length);
        var inLineComment = false;
        var inBlockComment = false;
        var inString = false;

        for (var index = 0; index < source.Length; index++)
        {
            var current = source[index];
            var next = index + 1 < source.Length ? source[index + 1] : '\0';

            if (inLineComment)
            {
                if (current == '\r' || current == '\n')
                {
                    inLineComment = false;
                    result.Append(current);
                }
                else
                {
                    result.Append(' ');
                }

                continue;
            }

            if (inBlockComment)
            {
                if (current == '*' && next == '/')
                {
                    inBlockComment = false;
                    result.Append("  ");
                    index++;
                }
                else if (current == '\r' || current == '\n')
                {
                    result.Append(current);
                }
                else
                {
                    result.Append(' ');
                }

                continue;
            }

            if (inString)
            {
                result.Append(current);
                if (current == '"' && (index == 0 || source[index - 1] != '\\'))
                {
                    inString = false;
                }

                continue;
            }

            if (current == '/' && next == '/')
            {
                inLineComment = true;
                result.Append("  ");
                index++;
                continue;
            }

            if (current == '/' && next == '*')
            {
                inBlockComment = true;
                result.Append("  ");
                index++;
                continue;
            }

            if (current == '"')
            {
                inString = true;
            }

            result.Append(current);
        }

        return result.ToString();
    }
}
