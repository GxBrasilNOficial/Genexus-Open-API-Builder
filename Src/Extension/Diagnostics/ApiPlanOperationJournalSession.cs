#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;
using Artech.Genexus.Common.Wiki;

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
    private Func<IEnumerable<ApiPlanOperationJournalInventoryItem>>? _inventoryProvider;

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
        string? metadataSchemaVersion,
        IEnumerable<ApiPlanOperationJournalInventoryItem>? inventory = null)
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

        // B111/F3 P3: quem decide se a operação pode começar é o gate estendido, e ele
        // devolve o motivo em forma classificada — código, razão estável, pré-condição e
        // contexto —, não uma frase.
        var decision = ApiPlanOperationJournalGate.Evaluate(new ApiPlanOperationJournalGateInput
        {
            LookupState = MapLookup(lookup.Kind),
            LookupDetail = lookup.Detail,
            CurrentEnvelope = lookup.Journal,
            // A durabilidade do que já está gravado é conhecida: o snapshot foi lido e
            // validado agora.
            ObservedDurability = JournalDurability.Confirmed,
            KnowledgeBaseGuid = knowledgeBaseGuid,
            JournalFileId = lookup.File?.Id ?? 0,
            // A continuação de um envelope Prepared é serviço da P5/P6. Enquanto ela não
            // existe, nenhum chamador autoriza — e o gate diz isso com razão própria.
            PreparedContinuationAuthorization = null,
        });

        if (decision.Outcome != JournalGateOutcome.Allowed)
        {
            return ApiPlanOperationJournalStart.Rejected(decision);
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
            DateTime.UtcNow,
            // B111/F3 P4: o Remove registra a intenção já com o inventário completo dos alvos
            // validados. É esse registro, gravado antes do primeiro Delete(), que permite dizer
            // depois o que foi previsto e o que chegou a sair da KB.
            inventory);

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
                return ApiPlanOperationJournalStart.Rejected(ApiPlanOperationJournalGate.SaveUnconfirmed(
                    store.FileId,
                    "O envelope Prepared do diário não pôde ser confirmado: " + prepared.Detail));
            }

            ApiPlanOperationJournalCheckpoints.PromoteToActive(envelope, DateTime.UtcNow);
            active = store.WriteCheckpoint(envelope);
            if (!active.IsConfirmed)
            {
                return ApiPlanOperationJournalStart.Rejected(ApiPlanOperationJournalGate.SaveUnconfirmed(
                    store.FileId,
                    "A promoção do diário a Active não pôde ser confirmada: " + active.Detail));
            }
        }
        catch (Exception exception)
        {
            return ApiPlanOperationJournalStart.Rejected(ApiPlanOperationJournalGate.SaveUnconfirmed(
                store.FileId,
                "A abertura do diário falhou: " + Clean(exception.Message)));
        }

        var session = new ApiPlanOperationJournalSession(store, envelope);
        session.Note(string.Format(
            CultureInfo.InvariantCulture,
            "Diário aberto: OperationKind='{0}', OperationId='{1}', ApplicationId='{2}', FileId={3}, Module='{4}', Created={5}, Bytes={6}.",
            operationKind,
            operationId,
            applicationId,
            store.FileId,
            store.ModuleName,
            prepared.Created,
            active.Bytes));
        return ApiPlanOperationJournalStart.Started(session);
    }

    /// <summary>
    /// Reabre a sessão sobre um envelope que já existe na KB — a retomada da P5/P6. Não há CP1
    /// nem CP2: o envelope não nasce agora, e criar um novo aqui apagaria a intenção que a
    /// continuação existe para honrar.
    /// </summary>
    internal static ApiPlanOperationJournalSession Resume(
        KBModel designModel,
        WikiFileKBObject journalFile,
        ApiPlanOperationJournal envelope)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (journalFile is null)
        {
            throw new ArgumentNullException(nameof(journalFile));
        }

        if (envelope is null)
        {
            throw new ArgumentNullException(nameof(envelope));
        }

        var store = new ApiPlanOperationJournalStore(designModel, journalFile);
        return new ApiPlanOperationJournalSession(store, envelope);
    }

    /// <summary>
    /// Checkpoint da retomada: o envelope volta a Running com os mesmos identificadores, antes
    /// de a fila tentar a primeira exclusão da continuação.
    /// </summary>
    internal bool ResumeRemoval()
    {
        return Advance(
            () => ApiPlanOperationJournalCheckpoints.ResumeRemoval(Envelope, DateTime.UtcNow),
            "retomada de remoção");
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

    /// <summary>
    /// Liga o diário ao inventário vivo de uma remoção. Sem ele, o snapshot de cada checkpoint
    /// seria reconstruído só a partir dos recibos — e a intenção registrada antes do primeiro
    /// <c>Delete()</c> desapareceria justamente quando ela passa a valer: numa interrupção, o
    /// que foi previsto e não saiu não teria recibo nenhum para reaparecer.
    /// </summary>
    internal void AttachRemovalInventory(Func<IEnumerable<ApiPlanOperationJournalInventoryItem>>? inventoryProvider)
    {
        _inventoryProvider = inventoryProvider;
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
        if (_persistenceLog is null && _inventoryProvider is null)
        {
            return;
        }

        var receipts = _persistenceLog is null
            ? Array.Empty<ApiPlanOperationJournalReceipt>()
            : ApiPlanOperationJournalReceiptMapper.MapReceipts(_persistenceLog.Receipts);

        // Numa remoção, o inventário é a intenção — atualizada pelas observações de cada
        // tentativa —, e não uma derivação dos recibos.
        var inventory = _inventoryProvider is not null
            ? ApiPlanOperationJournalReceiptMapper.AttachReceiptSequences(
                _inventoryProvider.Invoke(),
                _persistenceLog?.Receipts ?? Array.Empty<PersistenceReceipt>())
            : ApiPlanOperationJournalReceiptMapper.BuildInventory(
                _persistenceLog!.Receipts,
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

    /// <summary>
    /// Traduz o resultado da busca, que depende do SDK, para a forma neutra que o gate avalia.
    /// </summary>
    private static JournalGateLookupState MapLookup(ApiPlanOperationJournalLookupKind kind) => kind switch
    {
        ApiPlanOperationJournalLookupKind.Absent => JournalGateLookupState.Absent,
        ApiPlanOperationJournalLookupKind.Found => JournalGateLookupState.Found,
        ApiPlanOperationJournalLookupKind.Ambiguous => JournalGateLookupState.Ambiguous,
        ApiPlanOperationJournalLookupKind.ExternalCollision => JournalGateLookupState.ExternalCollision,
        ApiPlanOperationJournalLookupKind.Unreadable => JournalGateLookupState.Unreadable,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Resultado de busca do diário fora do contrato."),
    };

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

/// <summary>
/// Resultado da abertura. Quando a operação não começa, o motivo vem classificado pelo gate:
/// o relatório e a Output publicam código, razão estável e contexto, não apenas a frase.
/// </summary>
internal sealed class ApiPlanOperationJournalStart
{
    private ApiPlanOperationJournalStart(
        ApiPlanOperationJournalSession? session,
        ApiPlanOperationJournalGateDiagnostic? diagnostic,
        ApiPlanOperationJournal? currentEnvelope)
    {
        Session = session;
        Diagnostic = diagnostic;
        CurrentEnvelope = currentEnvelope;
    }

    internal ApiPlanOperationJournalSession? Session { get; }

    /// <summary>Preenchido sempre que a operação não pôde começar.</summary>
    internal ApiPlanOperationJournalGateDiagnostic? Diagnostic { get; }

    /// <summary>Envelope anterior, quando o bloqueio vem do estado da operação previamente registrada.</summary>
    internal ApiPlanOperationJournal? CurrentEnvelope { get; }

    internal bool IsStarted => Session is not null;

    /// <summary>Linha única para a Output e para o relatório final.</summary>
    internal string Detail => Diagnostic?.Describe() ?? string.Empty;

    internal static ApiPlanOperationJournalStart Started(ApiPlanOperationJournalSession session) =>
        new ApiPlanOperationJournalStart(session, null, null);

    internal static ApiPlanOperationJournalStart Rejected(ApiPlanOperationJournalGateDecision decision)
    {
        if (decision.Diagnostic is not null)
        {
            return new ApiPlanOperationJournalStart(null, decision.Diagnostic, decision.CurrentEnvelope);
        }

        // Continuação autorizada: o gate a reconhece, mas continuar um envelope preservando
        // os identificadores é serviço da P5/P6. Enquanto ele não existe, a operação não
        // começa — e dizer isso é mais honesto que abrir um envelope novo por cima.
        return new ApiPlanOperationJournalStart(
            null,
            ApiPlanOperationJournalGate.ContinuationServiceUnavailable(decision.CurrentEnvelope!),
            decision.CurrentEnvelope);
    }

    internal static ApiPlanOperationJournalStart Rejected(ApiPlanOperationJournalGateDiagnostic diagnostic) =>
        new ApiPlanOperationJournalStart(null, diagnostic, null);
}
