#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// B109 — captura o diagnóstico completo de uma exceção que hoje chega ao relatório
/// reduzida a uma linha de mensagem.
///
/// Os <c>catch</c> das etapas do Apply registram apenas <c>ex.Message</c> e a mensagem do
/// inner, e **descartam a stack trace**. Sem ela, a origem de
/// <c>Collection was modified; enumeration operation may not execute</c> — observada quatro
/// vezes, em Business Component, em List e num Save de API isolado — permanece hipótese.
///
/// Esta classe formata tipo, mensagem, origem e stack de cada nível da cadeia de exceções.
/// Não altera fluxo: os chamadores continuam tratando a exceção como antes.
///
/// Sonda temporária: sai com as demais no fechamento da sprint S-B111.
/// </summary>
internal static class B109ExceptionProbe
{
    private const int MaxDepth = 8;

    /// <summary>
    /// Linhas prontas para a Output, uma por linha lógica do diagnóstico.
    /// </summary>
    public static IReadOnlyList<string> Describe(Exception exception, string stage)
    {
        if (exception is null)
        {
            return new[] { $"[B109] {stage}: exceção nula." };
        }

        var lines = new List<string>
        {
            $"[B109] === diagnóstico completo da falha em '{stage}' ===",
        };

        var current = exception;
        var depth = 0;
        while (current is not null && depth < MaxDepth)
        {
            var prefix = depth == 0 ? "exceção" : "inner " + depth.ToString(CultureInfo.InvariantCulture);
            lines.Add($"[B109] {prefix}: {current.GetType().FullName}");
            lines.Add($"[B109]   Message : {Flatten(current.Message)}");
            lines.Add($"[B109]   Source  : {current.Source ?? "<null>"}");
            lines.Add($"[B109]   Site    : {DescribeTargetSite(current)}");

            if (string.IsNullOrWhiteSpace(current.StackTrace))
            {
                lines.Add("[B109]   Stack   : <vazia>");
            }
            else
            {
                lines.Add("[B109]   Stack   :");
                foreach (var frame in current.StackTrace.Split('\n'))
                {
                    var trimmed = frame.TrimEnd('\r').TrimEnd();
                    if (trimmed.Length > 0)
                    {
                        lines.Add("[B109]     " + trimmed.TrimStart());
                    }
                }
            }

            current = current.InnerException;
            depth++;
        }

        if (current is not null)
        {
            lines.Add($"[B109] (cadeia truncada em {MaxDepth} níveis)");
        }

        lines.Add("[B109] === fim do diagnóstico ===");
        return lines;
    }

    private static string DescribeTargetSite(Exception exception)
    {
        try
        {
            var site = exception.TargetSite;
            if (site is null)
            {
                return "<null>";
            }

            var declaring = site.DeclaringType is null ? "<?>" : site.DeclaringType.FullName;
            return declaring + "." + site.Name;
        }
        catch (Exception ex)
        {
            return "<erro ao ler TargetSite: " + ex.GetType().Name + ">";
        }
    }

    /// <summary>Mensagens do SDK podem trazer quebras de linha; a Output é linha a linha.</summary>
    private static string Flatten(string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return "<vazia>";
        }

        var builder = new StringBuilder(message!.Length);
        foreach (var c in message)
        {
            builder.Append(c == '\r' || c == '\n' ? ' ' : c);
        }

        return builder.ToString();
    }
}
