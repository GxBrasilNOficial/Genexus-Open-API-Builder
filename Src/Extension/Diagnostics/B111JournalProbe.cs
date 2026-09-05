#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Artech.Architecture.Common.Objects;
using Artech.Common;
using Artech.Genexus.Common.Wiki;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// B111 — sonda de viabilidade e custo do diário durável (planejamento da frente).
///
/// O plano prevê um File próprio da KB como diário, com nome determinístico, e manda
/// "enumerar todos os diários B111" ao abrir um fluxo. O índice de nomes já carrega os
/// Files da KB numa varredura só, então a pergunta é quanto custa de fato:
///
/// 1. o que custa a varredura de File isolada, e o índice completo, nesta KB;
/// 2. o que custa criar, salvar e reler um File de diário pelo nome determinístico;
/// 3. um índice montado ANTES enxerga um File criado DEPOIS? (invalida a hipótese de
///    resolver o diário pelo índice pré-Apply sem refresh, como já ocorre com Folder/SDT);
/// 4. o que custa varrer por prefixo em memória, que é o caminho da recuperação.
///
/// A sonda cria UM File de teste e o exclui ao final. É diagnóstico temporário: deve sair
/// das três camadas de registro no fechamento da frente.
/// </summary>
internal static class B111JournalProbe
{
    private const string JournalPrefix = "B111_J_";
    private const string ProbeTransactionGuid = "00000000-0000-0000-0000-0000000000b1";
    private const string ProbePlannedApiName = "apiGoabB111JournalProbe";
    private const string ProbeDescription = "Gx Open API Builder B111 Journal Probe";

    public static IReadOnlyList<string> Run(KBModel designModel)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        var lines = new List<string>();
        var journalName = BuildJournalName(ProbeTransactionGuid, ProbePlannedApiName);
        lines.Add($"S3.0 nome determinístico do diário (fórmula da seção 4.6): '{journalName}' ({journalName.Length} chars)");

        var existing = WikiFileKBObject.GetAll(designModel)
            .Where(file => string.Equals(file.Name, journalName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (existing.Length > 0)
        {
            lines.Add($"BLOQUEIO: já existem {existing.Length} File(s) chamados '{journalName}'. Exclua-os antes de repetir a sonda. Nenhuma alteração foi feita.");
            return lines;
        }

        WikiFileKBObject? journal = null;
        try
        {
            var indexBefore = MeasureScans(designModel, lines);
            journal = MeasureWrite(designModel, journalName, lines);
            MeasureUpdateCost(designModel, journal, lines);
            MeasureLookup(designModel, indexBefore, journalName, lines);
        }
        catch (Exception ex)
        {
            lines.Add($"ERRO durante a sonda: {Describe(ex)}");
        }
        finally
        {
            CleanUp(designModel, journal, journalName, lines);
        }

        return lines;
    }

    // ---------- custo das varreduras ----------

    private static ApiPlanKbObjectNameIndex MeasureScans(KBModel designModel, List<string> lines)
    {
        lines.Add("== S3 — custo das varreduras nesta KB ==");

        var watch = Stopwatch.StartNew();
        var files = WikiFileKBObject.GetAll(designModel).ToArray();
        watch.Stop();
        lines.Add($"S3.1 WikiFileKBObject.GetAll materializado: {files.Length} Files em {watch.ElapsedMilliseconds} ms.");

        var journalCandidates = files.Count(file =>
            file.Name is not null && file.Name.StartsWith(JournalPrefix, StringComparison.OrdinalIgnoreCase));
        lines.Add($"S3.2 Files já existentes com prefixo '{JournalPrefix}': {journalCandidates}.");

        watch.Restart();
        var index = ApiPlanKbObjectNameIndex.Create(designModel);
        watch.Stop();
        lines.Add($"S3.3 índice completo (7 varreduras de catálogo) criado em {watch.ElapsedMilliseconds} ms.");

        return index;
    }

    // ---------- gravação e releitura ----------

    private static WikiFileKBObject MeasureWrite(KBModel designModel, string journalName, List<string> lines)
    {
        var payload = BuildSampleJournal(journalName);
        var bytes = Encoding.UTF8.GetBytes(payload);

        var watch = Stopwatch.StartNew();
        var journal = new WikiFileKBObject(designModel)
        {
            Name = journalName,
            Description = ProbeDescription,
        };
        journal.SetPropertyValue("JavaExtract", false);
        journal.SetPropertyValue("NetExtract", false);
        journal.SetPropertyValue("NetCoreExtract", false);
        journal.SetPropertyValue("IOSExtract", false);
        journal.SetPropertyValue("AndroidExtract", false);
        journal.SetPropertyValue("ExtractZip", false);
        journal.BlobPart.SetPropertyValue("FileName", journalName + ".json");
        journal.BlobPart.Data = BinaryStream.FromBytes(bytes);
        journal.Save();
        watch.Stop();
        lines.Add($"S3.4 criar + salvar o File de diário ({bytes.Length} bytes de JSON): {watch.ElapsedMilliseconds} ms.");

        watch.Restart();
        var persisted = WikiFileKBObject.GetAll(designModel)
            .SingleOrDefault(file => string.Equals(file.Name, journalName, StringComparison.OrdinalIgnoreCase));
        watch.Stop();

        if (persisted is null)
        {
            lines.Add("S3.5 releitura por nome via GetAll: NÃO reencontrou o diário recém-salvo.");
            return journal;
        }

        var persistedBytes = persisted.BlobPart?.Data?.GetBytes();
        var intact = persistedBytes is not null && persistedBytes.SequenceEqual(bytes);
        lines.Add($"S3.5 releitura por nome via GetAll: {watch.ElapsedMilliseconds} ms; bytes íntegros? {(intact ? "SIM" : "NÃO")}; Guid='{persisted.Guid}'.");

        return journal;
    }

    // ---------- custo de ATUALIZAR o diário ----------

    /// <summary>
    /// S4 — o plano atualiza o diário a cada etapa confirmada, e toda atualização depois da
    /// primeira é update de File existente, não criação. A criação já foi medida em S3.4;
    /// aqui se mede o que realmente domina o orçamento: o custo repetido do update, se ele
    /// estabiliza depois da primeira vez, quanto do custo vem do tamanho do payload e
    /// quanto é custo fixo de Save, e o que custa reler por GUID em vez de por varredura.
    /// </summary>
    private static void MeasureUpdateCost(KBModel designModel, WikiFileKBObject journal, List<string> lines)
    {
        lines.Add("== S4 — custo de ATUALIZAR o diário já existente ==");

        var journalGuid = journal.Guid;
        var watch = new Stopwatch();

        // Save sem alteração nenhuma: isola o custo fixo da operação.
        watch.Restart();
        journal.Save();
        watch.Stop();
        lines.Add($"S4.1 Save() sem nenhuma alteração (custo fixo): {watch.ElapsedMilliseconds} ms.");

        // Só metadado, sem tocar no conteúdo binário.
        watch.Restart();
        journal.Description = ProbeDescription + " u1";
        journal.Save();
        watch.Stop();
        lines.Add($"S4.2 update só da Description, sem tocar no Blob: {watch.ElapsedMilliseconds} ms.");

        // Updates sucessivos do conteúdo, mesmo tamanho: mostra se o custo estabiliza.
        for (var round = 1; round <= 4; round++)
        {
            var payload = BuildSizedPayload(247, round);
            watch.Restart();
            journal.BlobPart.Data = BinaryStream.FromBytes(payload);
            journal.Save();
            watch.Stop();
            lines.Add($"S4.3.{round} update do Blob com {payload.Length} bytes: {watch.ElapsedMilliseconds} ms.");
        }

        // Payload grande: separa custo por tamanho de custo fixo.
        var big = BuildSizedPayload(20 * 1024, 9);
        watch.Restart();
        journal.BlobPart.Data = BinaryStream.FromBytes(big);
        journal.Save();
        watch.Stop();
        lines.Add($"S4.4 update do Blob com {big.Length} bytes (~20 KB): {watch.ElapsedMilliseconds} ms.");

        var small = BuildSizedPayload(247, 10);
        watch.Restart();
        journal.BlobPart.Data = BinaryStream.FromBytes(small);
        journal.Save();
        watch.Stop();
        lines.Add($"S4.5 update do Blob de volta para {small.Length} bytes: {watch.ElapsedMilliseconds} ms.");

        // Releitura direta por identidade. Diferente de API, WikiFileKBObject.Get não aceita
        // Guid: a chave direta é o Id inteiro da KB. Por isso o plano não pode dizer
        // "reler o diário por GUID" — é o Id que precisa ser guardado.
        var journalId = journal.Id;
        lines.Add($"S4.6 identidade do diário: Id={journalId}, Guid='{journalGuid}'. WikiFileKBObject.Get aceita Id (int), não Guid.");

        watch.Restart();
        var byId = WikiFileKBObject.Get(designModel, journalId);
        watch.Stop();
        lines.Add(byId is null
            ? $"S4.7 releitura por Id: NÃO reencontrou o diário ({watch.ElapsedMilliseconds} ms)."
            : $"S4.7 releitura por Id: reencontrou Name='{byId.Name}' em {watch.ElapsedMilliseconds} ms.");

        watch.Restart();
        var byIdAgain = WikiFileKBObject.Get(designModel, journalId);
        watch.Stop();
        var bytesBack = byIdAgain?.BlobPart?.Data?.GetBytes();
        var intact = bytesBack is not null && bytesBack.SequenceEqual(small);
        lines.Add($"S4.8 segunda releitura por Id: {watch.ElapsedMilliseconds} ms; último conteúdo íntegro? {(intact ? "SIM" : "NÃO")}.");

        lines.Add(string.Empty);
    }

    /// <summary>Payload determinístico do tamanho pedido, distinto a cada rodada.</summary>
    private static byte[] BuildSizedPayload(int size, int round)
    {
        var builder = new StringBuilder(size);
        var block = 0;
        while (builder.Length < size)
        {
            var chunk = string.Format(
                CultureInfo.InvariantCulture,
                "r{0:D2}b{1:D6}-payload-",
                round,
                block);
            builder.Append(chunk, 0, Math.Min(chunk.Length, size - builder.Length));
            block++;
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    // ---------- resolução pelo índice ----------

    private static void MeasureLookup(
        KBModel designModel,
        ApiPlanKbObjectNameIndex indexBefore,
        string journalName,
        List<string> lines)
    {
        var watch = Stopwatch.StartNew();
        var staleHits = indexBefore.FindFiles(journalName);
        watch.Stop();
        lines.Add($"S3.6 índice montado ANTES da gravação enxerga o diário? {(staleHits.Count > 0 ? "SIM" : "NÃO")} "
            + $"({staleHits.Count} resultado(s) em {watch.ElapsedMilliseconds} ms). "
            + "NÃO significa que o diário precisa de refresh de índice, como já ocorre com Folder e SDT.");

        watch.Restart();
        var indexAfter = ApiPlanKbObjectNameIndex.Create(designModel);
        watch.Stop();
        var freshHits = indexAfter.FindFiles(journalName);
        lines.Add($"S3.7 índice remontado APÓS a gravação: {freshHits.Count} resultado(s); custo do remonte {watch.ElapsedMilliseconds} ms.");

        watch.Restart();
        var prefixHits = WikiFileKBObject.GetAll(designModel)
            .Count(file => file.Name is not null && file.Name.StartsWith(JournalPrefix, StringComparison.OrdinalIgnoreCase));
        watch.Stop();
        lines.Add($"S3.8 varredura por prefixo (caminho de recuperação): {prefixHits} candidato(s) em {watch.ElapsedMilliseconds} ms.");

        lines.Add(string.Empty);
    }

    // ---------- fórmula e payload ----------

    /// <summary>
    /// Fórmula da seção 4.6: B111_J_ + 24 primeiros hex de
    /// SHA-256(TransactionGuid minúsculo invariant + separador + PlannedApiName).
    /// </summary>
    internal static string BuildJournalName(string transactionGuid, string plannedApiName)
    {
        var seed = transactionGuid.ToLowerInvariant() + "|" + plannedApiName;
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(seed));

        var builder = new StringBuilder(JournalPrefix.Length + 24);
        builder.Append(JournalPrefix);
        for (var i = 0; builder.Length < JournalPrefix.Length + 24; i++)
        {
            builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.ToString(0, JournalPrefix.Length + 24);
    }

    private static string BuildSampleJournal(string journalName) =>
        "{"
        + "\"schemaVersion\":\"GOAB_B111_JOURNAL_PROBE\","
        + $"\"journalName\":\"{journalName}\","
        + $"\"transactionGuid\":\"{ProbeTransactionGuid}\","
        + $"\"plannedApiName\":\"{ProbePlannedApiName}\","
        + "\"applicationId\":\"probe\","
        + "\"stage\":\"Planned\","
        + "\"receipts\":[]"
        + "}";

    // ---------- limpeza ----------

    private static void CleanUp(KBModel designModel, WikiFileKBObject? journal, string journalName, List<string> lines)
    {
        if (journal is null)
        {
            lines.Add("Limpeza: nenhum File de sonda chegou a ser criado.");
            return;
        }

        try
        {
            var journalGuid = journal.Guid;
            journal.Delete();

            var stillExists = WikiFileKBObject.GetAll(designModel).Any(file => file.Guid == journalGuid);
            lines.Add(stillExists
                ? $"Limpeza: a exclusão do File '{journalName}' NÃO foi confirmada. Exclua-o manualmente (Guid='{journalGuid}')."
                : $"Limpeza: File '{journalName}' excluído e ausência confirmada (Guid='{journalGuid}').");
        }
        catch (Exception ex)
        {
            lines.Add($"Limpeza: falhou ao excluir o File '{journalName}' — {Describe(ex)}. Exclua-o manualmente.");
        }
    }

    private static string Describe(Exception ex) =>
        ex.InnerException is null ? ex.Message : $"{ex.Message} | Inner='{ex.InnerException.Message}'";
}
