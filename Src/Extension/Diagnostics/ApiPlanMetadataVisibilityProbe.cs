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
/// B115 — mede a visibilidade dos Files pelo <see cref="WikiFileKBObject.GetAll(KBModel)"/>
/// do KBModel usado pela IDE e tenta a leitura direta por <c>Id</c> do File de metadata.
///
/// A sonda roda em toda abertura do Wizard, porque o incidente que ela investiga é raro e
/// não reproduzível — deixá-la opt-in perderia justamente a captura. Por isso o caminho
/// normal é enxuto: uma varredura e o <c>direct-Get</c> da metadata. A segunda passagem e o
/// detalhamento dos Files, que custam outra varredura completa e a leitura dos blobs, só
/// rodam quando a metadata está ausente — o cenário que motivou a sonda.
///
/// **O que ela não faz:** não compara contra o índice do MCP nem contra qualquer outra
/// referência externa. A redação anterior prometia essa comparação, que o código nunca
/// executou. O que ela permite concluir é se o próprio `GetAll` da IDE é estável entre
/// passagens e se o File existe para a leitura direta — o suficiente para separar “o File
/// não existe” de “o File existe e o `GetAll` não o mostra”.
///
/// A sonda é somente leitura. Não cria, salva, atualiza ou exclui objetos.
/// </summary>
internal static class ApiPlanMetadataVisibilityProbe
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
            $"[B115] Transaction='{transaction.Name}', Metadata='{metadataName}', Settings='{settingsName}'. Somente leitura.",
        };

        try
        {
            var firstWatch = Stopwatch.StartNew();
            var first = ApiPlanScanProbe.Scan(
                "File",
                "b115-visibility",
                () => WikiFileKBObject.GetAll(designModel).ToArray());
            firstWatch.Stop();

            var metadata = MatchByName(first, metadataName);

            // Caminho normal: a metadata está onde deveria. Confirma que o Id encontrado é
            // legível diretamente — leitura pontual e barata — e para por aí. A segunda
            // passagem e a leitura dos blobs custam uma varredura completa e só respondem à
            // pergunta do caso anômalo, que aqui não se coloca.
            if (metadata.Length > 0)
            {
                lines.Add(
                    $"[B115] GetAll Files: {first.Length} em {firstWatch.ElapsedMilliseconds} ms; "
                    + $"metadata '{metadataName}' presente ({metadata.Length}).");
                AppendDirectGet(lines, designModel, metadata);
                return lines;
            }

            // A metadata deveria estar e não apareceu. É o cenário que a sonda existe para
            // investigar: segunda passagem para separar "o File não existe" de "o File existe
            // e o GetAll não o mostra", e detalhamento completo dos dois Files de interesse.
            var secondWatch = Stopwatch.StartNew();
            var second = ApiPlanScanProbe.Scan(
                "File",
                "b115-visibility-recheck",
                () => WikiFileKBObject.GetAll(designModel).ToArray());
            secondWatch.Stop();

            lines.Add(
                $"[B115] GetAll Files: primeira={first.Length} em {firstWatch.ElapsedMilliseconds} ms; "
                + $"segunda={second.Length} em {secondWatch.ElapsedMilliseconds} ms. "
                + $"Metadata '{metadataName}' ausente na primeira passagem.");
            AppendMatches(lines, "Metadata", metadataName, second);
            AppendMatches(lines, "Settings", settingsName, second);
            AppendDirectGet(lines, designModel, MatchByName(second, metadataName));
        }
        catch (Exception ex)
        {
            lines.Add($"[B115] GetAll Files falhou: {Describe(ex)}.");
        }

        return lines;
    }

    private static WikiFileKBObject[] MatchByName(IEnumerable<WikiFileKBObject> files, string expectedName)
    {
        return files
            .Where(file => string.Equals(file.Name, expectedName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static void AppendDirectGet(
        List<string> lines,
        KBModel designModel,
        IReadOnlyList<WikiFileKBObject> metadata)
    {
        foreach (var file in metadata)
        {
            try
            {
                var direct = WikiFileKBObject.Get(designModel, file.Id);
                lines.Add(direct is null
                    ? $"[B115] Metadata direct-Get: Id={file.Id} NÃO reencontrado."
                    : $"[B115] Metadata direct-Get: Id={file.Id} reencontrado Name='{direct.Name}', Guid='{direct.Guid}', Bytes={GetByteCount(direct)}.");
            }
            catch (Exception ex)
            {
                lines.Add($"[B115] Metadata direct-Get: Id={file.Id} falhou: {Describe(ex)}.");
            }
        }
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
        lines.Add($"[B115] {label}: nome esperado='{expectedName}', correspondências={matches.Length}.");
        foreach (var file in matches)
        {
            lines.Add(
                $"[B115] {label} File: Name='{file.Name}', Id={file.Id}, Guid='{file.Guid}', "
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
