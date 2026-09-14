#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Artech.Architecture.Common.Objects;
using Artech.Common;
using Artech.Genexus.Common.Wiki;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// S-B111 / F3 — armazenamento do diário durável na KB.
///
/// Há exatamente **um** File por KB, com o nome lógico fixo
/// <c>GxOpenApiBuilder_OperationJournal</c>. O nome apenas localiza o candidato; posse e
/// validade vêm da Description própria e do conteúdo.
///
/// O custo mandou o desenho (sondas de 2026-09-04): localizar pelo nome no índice já
/// montado custa 0 ms, reler por <c>Id</c> custa 0 ms e **remontar o índice custa ~3,1 s**
/// na KB grande. Por isso o <c>Id</c> é guardado na criação e toda releitura passa por ele;
/// o índice nunca é remontado para reencontrar o diário.
///
/// As gravações do diário **não** passam pelo <c>Persist(...)</c> da F2: elas usam esta
/// rotina própria de durabilidade, com confirmação por <c>FileId</c>, bytes e hash, e
/// atualizam <see cref="Durability"/> separadamente dos recibos dos objetos de negócio.
/// </summary>
internal sealed class ApiPlanOperationJournalStore
{
    private readonly KBModel _designModel;
    private WikiFileKBObject? _file;

    internal ApiPlanOperationJournalStore(KBModel designModel, WikiFileKBObject? existingFile)
    {
        _designModel = designModel ?? throw new ArgumentNullException(nameof(designModel));
        _file = existingFile;
        FileId = existingFile?.Id ?? 0;
    }

    /// <summary>
    /// Vinculação externa entre o envelope e o <c>WikiFileKBObject.Id</c>. Não é campo JSON
    /// do schema: é o endereço por onde cada releitura confirma o mesmo File.
    /// </summary>
    internal int FileId { get; private set; }

    /// <summary>
    /// Durabilidade observada da última gravação. <c>Unknown</c> quando o <c>Save()</c> ou a
    /// releitura não permitiram confirmar o snapshot novo — nesse caso a extensão **não**
    /// tenta uma segunda gravação para "corrigir" o estado.
    /// </summary>
    internal JournalDurability Durability { get; private set; } = JournalDurability.Confirmed;

    /// <summary>Gravações físicas efetivamente tentadas, para conferir a política de checkpoints.</summary>
    internal int PhysicalCheckpoints { get; private set; }

    /// <summary>
    /// Tempo somado das gravações do diário, gravação e confirmação incluídas. É o número que
    /// se compara com o orçamento da seção 4.4 do plano da F3; sem ele, o custo do diário
    /// ficaria indistinguível do custo do resto do Apply.
    /// </summary>
    internal long CheckpointMs { get; private set; }

    /// <summary>Hash canônico do último snapshot confirmado; vazio enquanto não houver um.</summary>
    internal string LastConfirmedSnapshotHash { get; private set; } = string.Empty;

    internal static string OwnedDescription =>
        ApiPlanOwnedObjectDescription.Create(ApiPlanOperationJournal.JournalObjectName);

    /// <summary>
    /// Localiza o diário pelo nome fixo, preferindo o índice já montado. Ausência,
    /// duplicidade, colisão externa e conteúdo inválido são resultados distintos: todos
    /// bloqueiam, mas por motivos diferentes, e a recuperação precisa saber qual.
    /// </summary>
    internal static ApiPlanOperationJournalLookup Locate(KBModel designModel, ApiPlanKbObjectNameIndex? kbIndex)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        IReadOnlyList<WikiFileKBObject> matches = kbIndex is not null
            ? kbIndex.FindFiles(ApiPlanOperationJournal.JournalObjectName)
            : ApiPlanScanProbe.Scan(
                "File",
                "journal-locate",
                () => WikiFileKBObject.GetAll(designModel)
                    .Where(item => string.Equals(item.Name, ApiPlanOperationJournal.JournalObjectName, StringComparison.OrdinalIgnoreCase))
                    .ToArray());

        if (matches.Count == 0)
        {
            return ApiPlanOperationJournalLookup.Absent();
        }

        if (matches.Count > 1)
        {
            return ApiPlanOperationJournalLookup.Ambiguous(matches.Count);
        }

        var file = matches[0];
        if (!ApiPlanOwnedObjectDescription.IsCanonical(file.Description, ApiPlanOperationJournal.JournalObjectName))
        {
            return ApiPlanOperationJournalLookup.ExternalCollision(file);
        }

        var bytes = file.BlobPart?.Data?.GetBytes();
        if (bytes is null || bytes.Length == 0)
        {
            return ApiPlanOperationJournalLookup.Unreadable(file, "O File do diário não tem conteúdo persistido.");
        }

        var read = ApiPlanOperationJournalSerializer.ReadBytes(bytes);
        if (!read.IsValid)
        {
            return ApiPlanOperationJournalLookup.Unreadable(file, read.Describe());
        }

        return ApiPlanOperationJournalLookup.Found(file, read.Journal!, read.SnapshotHash);
    }

    /// <summary>
    /// Grava um checkpoint e o confirma relendo pelo <c>FileId</c>. Nunca lança por falha de
    /// confirmação: devolve o resultado com <c>Unknown</c>, porque a decisão de parar é de
    /// quem conduz a operação, e uma segunda gravação incerta só apagaria a evidência.
    /// </summary>
    internal ApiPlanOperationJournalCheckpointResult WriteCheckpoint(ApiPlanOperationJournal journal)
    {
        if (journal is null)
        {
            throw new ArgumentNullException(nameof(journal));
        }

        // Serializar primeiro: um envelope inválido não pode chegar ao File.Save().
        var canonicalJson = ApiPlanOperationJournalSerializer.Serialize(journal);
        var bytes = new UTF8Encoding(false).GetBytes(canonicalJson);
        var expectedHash = ApiPlanOperationJournalSerializer.ComputeSnapshotHash(canonicalJson);

        var file = _file;
        var creating = file is null;
        if (file is null)
        {
            file = new WikiFileKBObject(_designModel) { Name = ApiPlanOperationJournal.JournalObjectName };
        }

        file.Description = OwnedDescription;
        SetExtractionFlags(file);
        file.BlobPart.SetPropertyValue("FileName", ApiPlanOperationJournal.JournalExternalFileName);
        file.BlobPart.Data = BinaryStream.FromBytes(bytes);

        PhysicalCheckpoints++;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            file.Save();
        }
        catch (Exception exception)
        {
            // O Save lançou: não se sabe se o snapshot novo foi persistido. Isso é
            // indeterminação, não "não gravou".
            Durability = JournalDurability.Unknown;
            CheckpointMs += watch.ElapsedMilliseconds;
            return ApiPlanOperationJournalCheckpointResult.Unconfirmed(
                FileId,
                expectedHash,
                "O Save() do diário lançou: " + Clean(exception.Message));
        }

        _file = file;
        FileId = file.Id;
        if (FileId <= 0)
        {
            Durability = JournalDurability.Unknown;
            CheckpointMs += watch.ElapsedMilliseconds;
            return ApiPlanOperationJournalCheckpointResult.Unconfirmed(
                FileId,
                expectedHash,
                "O File do diário não devolveu um Id utilizável após o Save().");
        }

        var confirmation = Confirm(expectedHash, bytes);
        if (!confirmation.Confirmed)
        {
            Durability = JournalDurability.Unknown;
            CheckpointMs += watch.ElapsedMilliseconds;
            return ApiPlanOperationJournalCheckpointResult.Unconfirmed(FileId, expectedHash, confirmation.Detail);
        }

        Durability = JournalDurability.Confirmed;
        LastConfirmedSnapshotHash = expectedHash;
        CheckpointMs += watch.ElapsedMilliseconds;
        return ApiPlanOperationJournalCheckpointResult.Confirmed(FileId, expectedHash, bytes.Length, creating);
    }

    private (bool Confirmed, string Detail) Confirm(string expectedHash, byte[] expectedBytes)
    {
        WikiFileKBObject? reloaded;
        try
        {
            // Releitura por Id: 0 ms medido nas duas KBs. Remontar o índice aqui custaria
            // ~3,1 s na KB grande e não diria nada a mais.
            reloaded = WikiFileKBObject.Get(_designModel, FileId);
        }
        catch (Exception exception)
        {
            return (false, "A releitura do diário por FileId falhou: " + Clean(exception.Message));
        }

        if (reloaded is null)
        {
            return (false, "A releitura do diário por FileId não encontrou o File.");
        }

        if (!string.Equals(reloaded.Name, ApiPlanOperationJournal.JournalObjectName, StringComparison.OrdinalIgnoreCase))
        {
            return (false, string.Format(
                CultureInfo.InvariantCulture,
                "O FileId {0} resolveu para '{1}', e não para o diário.",
                FileId,
                reloaded.Name));
        }

        var persistedBytes = reloaded.BlobPart?.Data?.GetBytes();
        if (persistedBytes is null || persistedBytes.Length == 0)
        {
            return (false, "O diário relido não tem conteúdo persistido.");
        }

        if (!persistedBytes.SequenceEqual(expectedBytes))
        {
            return (false, "Os bytes relidos do diário divergem do snapshot gravado.");
        }

        var persistedHash = ComputeSha256(persistedBytes);
        var expectedContentHash = ComputeSha256(expectedBytes);
        if (!string.Equals(persistedHash, expectedContentHash, StringComparison.Ordinal))
        {
            return (false, "O digest dos bytes relidos diverge do snapshot gravado.");
        }

        var read = ApiPlanOperationJournalSerializer.ReadBytes(persistedBytes);
        if (!read.IsValid || !string.Equals(read.SnapshotHash, expectedHash, StringComparison.Ordinal))
        {
            return (false, "O snapshot relido não reproduz o hash canônico esperado.");
        }

        return (true, string.Empty);
    }

    private static void SetExtractionFlags(WikiFileKBObject file)
    {
        file.SetPropertyValue("JavaExtract", false);
        file.SetPropertyValue("NetExtract", false);
        file.SetPropertyValue("NetCoreExtract", false);
        file.SetPropertyValue("IOSExtract", false);
        file.SetPropertyValue("AndroidExtract", false);
        file.SetPropertyValue("ExtractZip", false);
    }

    private static string ComputeSha256(byte[] bytes)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(bytes);
        var builder = new StringBuilder(hash.Length * 2);
        foreach (var value in hash)
        {
            builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static string Clean(string value) => (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
}

internal enum ApiPlanOperationJournalLookupKind
{
    Absent = 0,
    Found = 1,
    Ambiguous = 2,
    ExternalCollision = 3,
    Unreadable = 4,
}

internal sealed class ApiPlanOperationJournalLookup
{
    private ApiPlanOperationJournalLookup(
        ApiPlanOperationJournalLookupKind kind,
        WikiFileKBObject? file,
        ApiPlanOperationJournal? journal,
        string snapshotHash,
        string detail)
    {
        Kind = kind;
        File = file;
        Journal = journal;
        SnapshotHash = snapshotHash;
        Detail = detail;
    }

    internal ApiPlanOperationJournalLookupKind Kind { get; }

    internal WikiFileKBObject? File { get; }

    internal ApiPlanOperationJournal? Journal { get; }

    internal string SnapshotHash { get; }

    internal string Detail { get; }

    internal static ApiPlanOperationJournalLookup Absent() =>
        new ApiPlanOperationJournalLookup(ApiPlanOperationJournalLookupKind.Absent, null, null, string.Empty, string.Empty);

    internal static ApiPlanOperationJournalLookup Found(WikiFileKBObject file, ApiPlanOperationJournal journal, string snapshotHash) =>
        new ApiPlanOperationJournalLookup(ApiPlanOperationJournalLookupKind.Found, file, journal, snapshotHash, string.Empty);

    internal static ApiPlanOperationJournalLookup Ambiguous(int count) =>
        new ApiPlanOperationJournalLookup(
            ApiPlanOperationJournalLookupKind.Ambiguous,
            null,
            null,
            string.Empty,
            string.Format(
                CultureInfo.InvariantCulture,
                "Foram encontrados {0} Files chamados '{1}'. Há exatamente um diário por KB; a duplicidade precisa ser resolvida à mão.",
                count,
                ApiPlanOperationJournal.JournalObjectName));

    internal static ApiPlanOperationJournalLookup ExternalCollision(WikiFileKBObject file) =>
        new ApiPlanOperationJournalLookup(
            ApiPlanOperationJournalLookupKind.ExternalCollision,
            file,
            null,
            string.Empty,
            string.Format(
                CultureInfo.InvariantCulture,
                "Já existe um File '{0}' que não é do gerador: a Description não é a própria. Nenhuma alteração foi feita.",
                ApiPlanOperationJournal.JournalObjectName));

    internal static ApiPlanOperationJournalLookup Unreadable(WikiFileKBObject file, string detail) =>
        new ApiPlanOperationJournalLookup(ApiPlanOperationJournalLookupKind.Unreadable, file, null, string.Empty, detail);
}

internal sealed class ApiPlanOperationJournalCheckpointResult
{
    private ApiPlanOperationJournalCheckpointResult(
        bool confirmed,
        int fileId,
        string snapshotHash,
        int bytes,
        bool created,
        string detail)
    {
        IsConfirmed = confirmed;
        FileId = fileId;
        SnapshotHash = snapshotHash;
        Bytes = bytes;
        Created = created;
        Detail = detail;
    }

    internal bool IsConfirmed { get; }

    internal int FileId { get; }

    internal string SnapshotHash { get; }

    internal int Bytes { get; }

    internal bool Created { get; }

    internal string Detail { get; }

    internal static ApiPlanOperationJournalCheckpointResult Confirmed(int fileId, string snapshotHash, int bytes, bool created) =>
        new ApiPlanOperationJournalCheckpointResult(true, fileId, snapshotHash, bytes, created, string.Empty);

    internal static ApiPlanOperationJournalCheckpointResult Unconfirmed(int fileId, string snapshotHash, string detail) =>
        new ApiPlanOperationJournalCheckpointResult(false, fileId, snapshotHash, 0, false, detail);
}
