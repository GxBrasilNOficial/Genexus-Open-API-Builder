#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;
using Artech.Genexus.Common.Wiki;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// B113 — compara a visibilidade dos Files no KBModel usado pela IDE com a
/// referência que o wizard acabou de ler.
///
/// A sonda é somente leitura. Ela não cria, salva, atualiza ou exclui objetos.
/// Sai junto com as demais sondas temporárias depois que a divergência entre o
/// índice do MCP e o <see cref="WikiFileKBObject.GetAll(KBModel)"/> da IDE for
/// explicada.
/// </summary>
internal static class B113MetadataVisibilityProbe
{
    public static IReadOnlyList<string> Run(KBModel designModel, Transaction transaction)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        var metadataName = "api" + transaction.Name + "_Metadata";
        var settingsName = "GxOpenApiBuilder_Settings";
        var lines = new List<string>
        {
            $"[B113] Transaction='{transaction.Name}', Metadata='{metadataName}', Settings='{settingsName}'. Somente leitura.",
        };

        try
        {
            var firstWatch = Stopwatch.StartNew();
            var first = WikiFileKBObject.GetAll(designModel).ToArray();
            firstWatch.Stop();

            var secondWatch = Stopwatch.StartNew();
            var second = WikiFileKBObject.GetAll(designModel).ToArray();
            secondWatch.Stop();

            lines.Add(
                $"[B113] GetAll Files: primeira={first.Length} em {firstWatch.ElapsedMilliseconds} ms; "
                + $"segunda={second.Length} em {secondWatch.ElapsedMilliseconds} ms.");
            AppendMatches(lines, "Metadata", metadataName, first);
            AppendMatches(lines, "Settings", settingsName, first);

            var metadata = first
                .Where(file => string.Equals(file.Name, metadataName, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            foreach (var file in metadata)
            {
                try
                {
                    var direct = WikiFileKBObject.Get(designModel, file.Id);
                    lines.Add(direct is null
                        ? $"[B113] Metadata direct-Get: Id={file.Id} NÃO reencontrado."
                        : $"[B113] Metadata direct-Get: Id={file.Id} reencontrado Name='{direct.Name}', Guid='{direct.Guid}', Bytes={GetByteCount(direct)}.");
                }
                catch (Exception ex)
                {
                    lines.Add($"[B113] Metadata direct-Get: Id={file.Id} falhou: {Describe(ex)}.");
                }
            }
        }
        catch (Exception ex)
        {
            lines.Add($"[B113] GetAll Files falhou: {Describe(ex)}.");
        }

        return lines;
    }

    private static void AppendMatches(
        List<string> lines,
        string label,
        string expectedName,
        IEnumerable<WikiFileKBObject> files)
    {
        var matches = files
            .Where(file => string.Equals(file.Name, expectedName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        lines.Add($"[B113] {label}: nome esperado='{expectedName}', correspondências={matches.Length}.");
        foreach (var file in matches)
        {
            lines.Add(
                $"[B113] {label} File: Name='{file.Name}', Id={file.Id}, Guid='{file.Guid}', "
                + $"Parent='{file.Parent?.Name ?? "<null>"}', Description='{Clean(file.Description)}', Bytes={GetByteCount(file)}.");
        }
    }

    private static int GetByteCount(WikiFileKBObject file)
    {
        try
        {
            return file.BlobPart?.Data?.GetBytes()?.Length ?? 0;
        }
        catch
        {
            return -1;
        }
    }

    private static string Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "<vazia>";
        }

        var singleLine = value!.Replace('\r', ' ').Replace('\n', ' ');
        return singleLine.Length <= 240 ? singleLine : singleLine.Substring(0, 240) + "...";
    }

    private static string Describe(Exception exception)
    {
        var inner = exception.InnerException;
        return inner is null
            ? exception.GetType().Name + ": " + Clean(exception.Message)
            : exception.GetType().Name + ": " + Clean(exception.Message)
              + " | Inner=" + inner.GetType().Name + ": " + Clean(inner.Message);
    }
}
