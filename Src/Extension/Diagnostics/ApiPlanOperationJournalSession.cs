#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// S-B111 / F3 — condução de uma operação sob o diário durável.
///
/// A sessão amarra a máquina de checkpoints ao armazenamento: ela decide se a operação pode
/// começar, grava e confirma cada fronteira, e trava assim que a durabilidade deixa de ser
/// confirmável. Duas regras a governam:
///
/// - **nada de negócio é gravado antes de `Prepared` e `Active` confirmados.** Se a criação
///   do envelope não for confirmada, a operação aborta sem tocar a KB;
/// - **nunca existe um segundo diário.** Duplicidade, colisão externa, conteúdo inválido ou
///   envelope anterior não terminal bloqueiam a operação e pedem decisão humana, em vez de
///   criar um File novo que esconderia a incerteza.
///
/// Uma falha de checkpoint depois do primeiro `Save()` de negócio não desfaz nada — não há
/// atomicidade — mas impede que a operação continue às cegas e deixa o último snapshot
/// durável no lugar.
/// </summary>
internal sealed class ApiPlanOperationJournalSession
{
    private readonly ApiPlanOperationJournalStore _store;
    private readonly List<string> _diagnostics = new List<string>();
    private ApiPlanPersistenceLog? _persistenceLog;
    private Func<IEnumerable<string>>? _createdNames;

    private ApiPlanOperationJournalSession(ApiPlanOperationJournalStore store, ApiPlanOperationJournal envelope)
    {
        _store = store;
        Envelope = envelope;
    }

    internal ApiPlanOperationJournal Envelope { get; }

    internal int FileId => _store.FileId;

    internal JournalDurability Durability => _store.Durability;

    internal int PhysicalCheckpoints => _store.PhysicalCheckpoints;

    /// <summary>Custo somado das gravações do diário, para comparar com o orçamento da F3.</summary>
    internal long CheckpointMs => _store.CheckpointMs;

    internal string SnapshotHash => _store.LastConfirmedSnapshotHash;

    /// <summary>
    /// Verdadeiro quando um checkpoint deixou de ser confirmado. A partir daí a sessão não
    /// grava mais nada: o último snapshot durável é preservado para a reconciliação.
    /// </summary>
    internal bool IsBlocked { get; private set; }

    internal string BlockDetail { get; private set; } = string.Empty;

    internal IReadOnlyList<string> Diagnostics => _diagnostics;

    /// <summary>
    /// Devolve as linhas ainda não publicadas e as descarta, para que a Output não repita o
    /// mesmo diagnóstico a cada fronteira.
    /// </summary>
    internal IReadOnlyList<string> DrainDiagnostics()
    {
        var lines = _diagnostics.ToArray();
        _diagnostics.Clear();
        return lines;
    }

    /// <summary>
    /// Abre a operação: localiza o diário, avalia se o envelope anterior permite substituição
    /// e grava os checkpoints <c>Prepared</c> e <c>Active</c>, confirmando cada um. Só
    /// devolve sessão quando os dois ficaram duráveis.
    /// </summary>
    internal static ApiPlanOperationJournalStart Start(
        KBModel designModel,
        ApiPlanKbObjectNameIndex? kbIndex,
        Guid knowledgeBaseGuid,
        Transaction transaction,
        JournalOperationKind operationKind,
        ApiPlanOperationJournalPlan plan,
        Guid applicationId,
        JournalIntentKind intentKind,
        string? metadataSchemaVersion)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        var lookup = ApiPlanOperationJournalStore.Locate(designModel, kbIndex);
        switch (lookup.Kind)
        {
            case ApiPlanOperationJournalLookupKind.Ambiguous:
            case ApiPlanOperationJournalLookupKind.ExternalCollision:
            case ApiPlanOperationJournalLookupKind.Unreadable:
                return ApiPlanOperationJournalStart.Unavailable(lookup.Detail);
        }

        if (lookup.Kind == ApiPlanOperationJournalLookupKind.Found)
        {
            // A durabilidade do que já está gravado é conhecida: o snapshot foi lido e
            // validado agora. O que decide é o estado da operação anterior.
            var reuse = ApiPlanOperationJournalCheckpoints.EvaluateReuse(lookup.Journal, JournalDurability.Confirmed);
            if (!reuse.CanStart)
            {
                return ApiPlanOperationJournalStart.Blocked(reuse.Reason, lookup.Journal);
            }
        }

        var operationId = Guid.NewGuid();
        var envelope = ApiPlanOperationJournalCheckpoints.CreatePrepared(
            operationKind,
            plan,
            knowledgeBaseGuid,
            transaction.Guid,
            transaction.Name,
            operationId,
            applicationId,
            ResolveGeneratorVersion(),
            intentKind,
            metadataSchemaVersion,
            DateTime.UtcNow);

        var store = new ApiPlanOperationJournalStore(designModel, lookup.File);
        ApiPlanOperationJournalCheckpointResult prepared;
        ApiPlanOperationJournalCheckpointResult active;
        try
        {
            prepared = store.WriteCheckpoint(envelope);
            if (!prepared.IsConfirmed)
            {
                // Antes de qualquer objeto de negócio: abortar é seguro e é a única saída
                // honesta, porque não se sabe qual snapshot ficou no File.
                return ApiPlanOperationJournalStart.Unavailable(
                    "O envelope Prepared do diário não pôde ser confirmado: " + prepared.Detail);
            }

            ApiPlanOperationJournalCheckpoints.PromoteToActive(envelope, DateTime.UtcNow);
            active = store.WriteCheckpoint(envelope);
            if (!active.IsConfirmed)
            {
                return ApiPlanOperationJournalStart.Unavailable(
                    "A promoção do diário a Active não pôde ser confirmada: " + active.Detail);
            }
        }
        catch (Exception exception)
        {
            return ApiPlanOperationJournalStart.Unavailable(
                "A abertura do diário falhou: " + Clean(exception.Message));
        }

        var session = new ApiPlanOperationJournalSession(store, envelope);
        session.Note(string.Format(
            CultureInfo.InvariantCulture,
            "Diário aberto: OperationKind='{0}', OperationId='{1}', ApplicationId='{2}', FileId={3}, Created={4}, Bytes={5}.",
            operationKind,
            operationId,
            applicationId,
            store.FileId,
            prepared.Created,
            active.Bytes));
        return ApiPlanOperationJournalStart.Started(session);
    }

    /// <summary>
    /// Registra a identidade da API assim que ela existe. Numa criação nova o GUID nasce do
    /// `API.Create`, dentro do pipeline: até lá o plano não tem o que registrar. O valor
    /// nunca é atribuído pela extensão — é lido do objeto — e não pode ser trocado depois
    /// de registrado.
    /// </summary>
    internal void SetPlannedApiGuid(Guid plannedApiGuid)
    {
        if (plannedApiGuid == Guid.Empty)
        {
            return;
        }

        var current = Envelope.Plan.PlannedApiGuid;
        if (current.HasValue && current.Value != plannedApiGuid)
        {
            Block(string.Format(
                CultureInfo.InvariantCulture,
                "A identidade da API mudou durante a operação: o diário registra '{0}' e o pipeline apresentou '{1}'.",
                current.Value,
                plannedApiGuid));
            return;
        }

        Envelope.Plan.PlannedApiGuid = plannedApiGuid;
    }

    /// <summary>
    /// Liga o diário ao log de persistência da F2. A partir daqui, cada checkpoint leva para
    /// o envelope os recibos e o inventário observados — sem isso, o diário saberia em que
    /// ponto a operação parou, mas não o que já tinha sido confirmado por objeto.
    /// </summary>
    internal void AttachPersistence(ApiPlanPersistenceLog? persistenceLog, Func<IEnumerable<string>>? createdNames)
    {
        _persistenceLog = persistenceLog;
        _createdNames = createdNames;
    }

    /// <summary>CP3 de Apply e Sync.</summary>
    internal bool NoteApiPhysicallySaved()
    {
        return Advance(
            () => ApiPlanOperationJournalCheckpoints.NoteApiPhysicallySaved(Envelope, DateTime.UtcNow),
            "API Object confirmado");
    }

    /// <summary>Estágio de indeterminação do `Save()` do API; não é estado terminal.</summary>
    internal bool NoteApiSaveOutcomeUnknown()
    {
        return Advance(
            () => ApiPlanOperationJournalCheckpoints.NoteApiSaveOutcomeUnknown(Envelope, DateTime.UtcNow),
            "resultado indeterminado do API Object");
    }

    /// <summary>CP3 do Recovery autônomo de B115.</summary>
    internal bool NoteMetadataRecovered()
    {
        return Advance(
            () => ApiPlanOperationJournalCheckpoints.NoteMetadataRecovered(Envelope, DateTime.UtcNow),
            "metadata recuperada");
    }

    /// <summary>CP3 do Remove: um por passada da fila.</summary>
    internal bool NoteRemovalPassCompleted()
    {
        return Advance(
            () => ApiPlanOperationJournalCheckpoints.NoteRemovalPassCompleted(Envelope, DateTime.UtcNow),
            "passada de remoção");
    }

    /// <summary>CP4 de sucesso.</summary>
    internal bool Complete()
    {
        var completed = Advance(
            () => ApiPlanOperationJournalCheckpoints.Complete(Envelope, DateTime.UtcNow),
            "conclusão");
        NoteCost();
        return completed;
    }

    /// <summary>CP4 de interrupção, com o motivo persistido do enum fechado.</summary>
    internal bool Interrupt(JournalOperationState state, JournalBlockReason blockReason)
    {
        var interrupted = Advance(
            () => ApiPlanOperationJournalCheckpoints.Interrupt(Envelope, state, blockReason, DateTime.UtcNow),
            "interrupção (" + blockReason + ")");
        NoteCost();
        return interrupted;
    }

    /// <summary>
    /// Publica o custo do diário no fechamento. Sem esta linha, o acréscimo do diário ficaria
    /// embutido no tempo total da operação e não daria para compará-lo com o orçamento.
    /// </summary>
    private void NoteCost()
    {
        Note(string.Format(
            CultureInfo.InvariantCulture,
            "Custo do diário: Checkpoints={0}, TotalMs={1}, FileId={2}, Durability={3}, Estado={4}/{5}.",
            PhysicalCheckpoints,
            CheckpointMs,
            FileId,
            Durability,
            Envelope.OperationState,
            Envelope.LogicalStage));
    }

    private bool Advance(Action transition, string description)
    {
        if (IsBlocked)
        {
            Note("Checkpoint '" + description + "' não foi gravado: o diário já está bloqueado. " + BlockDetail);
            return false;
        }

        try
        {
            transition();
            SyncPersistenceSnapshot();
        }
        catch (InvalidOperationException exception)
        {
            Block("A transição '" + description + "' não é válida no estado atual: " + exception.Message);
            return false;
        }

        ApiPlanOperationJournalCheckpointResult result;
        try
        {
            result = _store.WriteCheckpoint(Envelope);
        }
        catch (Exception exception)
        {
            // O diário é instrumento de diagnóstico: ele pode bloquear a si mesmo, nunca
            // derrubar a operação de negócio que o cerca. Um envelope recusado pelo schema
            // vira bloqueio visível, com os objetos já gravados intactos e o relatório final
            // preservado.
            Block("O checkpoint '" + description + "' falhou ao ser gravado: " + Clean(exception.Message)
                + " A operação seguiu; o diário ficou bloqueado e o último snapshot durável foi preservado.");
            return false;
        }

        if (!result.IsConfirmed)
        {
            Block("O checkpoint '" + description + "' não pôde ser confirmado: " + result.Detail
                + " O último snapshot durável foi preservado e a operação não continua às cegas.");
            return false;
        }

        Note(string.Format(
            CultureInfo.InvariantCulture,
            "Checkpoint '{0}': {1}/{2}, FileId={3}, Bytes={4}, Recibos={5}, Inventário={6}.",
            description,
            Envelope.OperationState,
            Envelope.LogicalStage,
            result.FileId,
            result.Bytes,
            Envelope.Receipts.Count,
            Envelope.Inventory.Count));
        return true;
    }

    /// <summary>
    /// Copia recibos e inventário do log da F2 para o envelope. É reconstrução completa, não
    /// acréscimo: o snapshot do checkpoint descreve o estado observado naquele instante.
    /// </summary>
    private void SyncPersistenceSnapshot()
    {
        if (_persistenceLog is null)
        {
            return;
        }

        var receipts = ApiPlanOperationJournalReceiptMapper.MapReceipts(_persistenceLog.Receipts);
        var inventory = ApiPlanOperationJournalReceiptMapper.BuildInventory(
            _persistenceLog.Receipts,
            _createdNames?.Invoke(),
            Envelope.OperationKind);

        Envelope.Receipts.Clear();
        foreach (var receipt in receipts)
        {
            Envelope.Receipts.Add(receipt);
        }

        Envelope.Inventory.Clear();
        foreach (var item in inventory)
        {
            Envelope.Inventory.Add(item);
        }
    }

    private void Block(string detail)
    {
        IsBlocked = true;
        BlockDetail = detail;
        Note(detail);
    }

    private void Note(string line) => _diagnostics.Add(line);

    private static string Clean(string value) => (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ");

    private static string ResolveGeneratorVersion()
    {
        var assembly = typeof(ApiPlanOperationJournalSession).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            return informational!;
        }

        return assembly.GetName().Version?.ToString() ?? "0.0.0";
    }
}

internal enum ApiPlanOperationJournalStartKind
{
    Started = 0,

    /// <summary>O diário não pôde ser lido, validado ou confirmado — `JournalUnavailable`.</summary>
    Unavailable = 1,

    /// <summary>O diário está legível e durável, mas o estado anterior impede a operação.</summary>
    Blocked = 2,
}

internal sealed class ApiPlanOperationJournalStart
{
    private ApiPlanOperationJournalStart(
        ApiPlanOperationJournalStartKind kind,
        ApiPlanOperationJournalSession? session,
        ApiPlanOperationJournal? currentEnvelope,
        string detail)
    {
        Kind = kind;
        Session = session;
        CurrentEnvelope = currentEnvelope;
        Detail = detail;
    }

    internal ApiPlanOperationJournalStartKind Kind { get; }

    internal ApiPlanOperationJournalSession? Session { get; }

    /// <summary>Envelope anterior, quando o bloqueio vem do estado da operação previamente registrada.</summary>
    internal ApiPlanOperationJournal? CurrentEnvelope { get; }

    internal string Detail { get; }

    internal bool IsStarted => Kind == ApiPlanOperationJournalStartKind.Started && Session is not null;

    internal static ApiPlanOperationJournalStart Started(ApiPlanOperationJournalSession session) =>
        new ApiPlanOperationJournalStart(ApiPlanOperationJournalStartKind.Started, session, null, string.Empty);

    internal static ApiPlanOperationJournalStart Unavailable(string detail) =>
        new ApiPlanOperationJournalStart(ApiPlanOperationJournalStartKind.Unavailable, null, null, detail);

    internal static ApiPlanOperationJournalStart Blocked(string detail, ApiPlanOperationJournal? currentEnvelope) =>
        new ApiPlanOperationJournalStart(ApiPlanOperationJournalStartKind.Blocked, null, currentEnvelope, detail);
}
