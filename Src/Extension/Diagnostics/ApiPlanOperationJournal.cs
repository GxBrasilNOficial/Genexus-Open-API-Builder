#nullable enable

using System;
using System.Collections.Generic;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// S-B111 / F3 — modelo do diário durável de operação (Modo A).
///
/// Há exatamente um diário por KB, no File <c>GxOpenApiBuilder_OperationJournal</c>, e ele
/// guarda somente a operação corrente: não existe histórico, lista de operações anteriores
/// nem recibos acumulados de execuções passadas.
///
/// As quatro dimensões de estado são registradas separadamente e nunca colapsadas em um enum
/// só: <see cref="OperationState"/> (resultado global), <see cref="LogicalStage"/> (ponto do
/// pipeline), <see cref="JournalDurability"/> (confirmação da última gravação do próprio
/// diário) e <see cref="IntentKind"/> (origem da intenção). O estado físico do objeto não é
/// uma quinta dimensão do envelope: é observação por item do inventário.
///
/// Este arquivo é SDK-free de propósito, para que schema, serialização canônica e validação
/// sejam exercitados offline, sem a IDE.
/// </summary>
public sealed class ApiPlanOperationJournal
{
    public const int SchemaVersion = 1;
    public const string JournalKind = "GOAB_OPERATION_JOURNAL";

    /// <summary>Nome lógico fixo do File na KB. Localiza o candidato; não prova posse.</summary>
    public const string JournalObjectName = "GxOpenApiBuilder_OperationJournal";

    /// <summary>Nome do arquivo externo associado ao File.</summary>
    public const string JournalExternalFileName = "GxOpenApiBuilder_OperationJournal.json";

    public Guid KnowledgeBaseGuid { get; set; }

    public Guid TransactionGuid { get; set; }

    public string TransactionName { get; set; } = string.Empty;

    public Guid OperationId { get; set; }

    public Guid ApplicationId { get; set; }

    public JournalOperationKind OperationKind { get; set; }

    public string GeneratorVersion { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public JournalEnvelopePhase EnvelopePhase { get; set; }

    public JournalOperationState OperationState { get; set; }

    public JournalLogicalStage LogicalStage { get; set; }

    public JournalDurability JournalDurability { get; set; }

    public JournalIntentKind IntentKind { get; set; }

    /// <summary>
    /// Versão da metadata de negócio associada, quando houver. Fechado em
    /// <c>GOAB_API_METADATA_B060_V1</c>, <c>..._V2</c> ou <c>..._V3</c>.
    /// </summary>
    public string? MetadataSchemaVersion { get; set; }

    public ApiPlanOperationJournalPlan Plan { get; set; } = new ApiPlanOperationJournalPlan();

    public IList<ApiPlanOperationJournalInventoryItem> Inventory { get; } =
        new List<ApiPlanOperationJournalInventoryItem>();

    public IList<ApiPlanOperationJournalReceipt> Receipts { get; } =
        new List<ApiPlanOperationJournalReceipt>();

    public ApiPlanOperationJournalAbandonment? Abandonment { get; set; }

    /// <summary>
    /// Motivo persistido do bloqueio. Namespace distinto do <c>reasonCode</c> efêmero do
    /// diagnóstico: coincidência textual entre os dois nunca autoriza copiar um para o outro.
    /// </summary>
    public JournalBlockReason? BlockReason { get; set; }
}

public sealed class ApiPlanOperationJournalPlan
{
    public JournalPlanKind PlanKind { get; set; }

    public Guid? PlannedApiGuid { get; set; }

    public string? ContractHash { get; set; }

    public bool? GenerateApiObject { get; set; }

    public bool? GenerateSdts { get; set; }

    public bool? GenerateProcedures { get; set; }

    public bool? GenerateMetadata { get; set; }

    public IList<string> Services { get; } = new List<string>();
}

public sealed class ApiPlanOperationJournalInventoryItem
{
    public JournalObjectType ObjectType { get; set; }

    public JournalIdentityKind IdentityKind { get; set; }

    public Guid? Guid { get; set; }

    /// <summary>
    /// <c>WikiFileKBObject.Id</c> do alvo, quando a identidade for <see cref="JournalIdentityKind.FileId"/>.
    /// Inteiro positivo; não confundir com o <c>journalFileId</c>, que é vinculação externa do
    /// próprio diário e não é campo JSON.
    /// </summary>
    public int? FileId { get; set; }

    /// <summary>Identidade histórica completa, exigida por <see cref="JournalIdentityKind.Composite"/>.</summary>
    public ApiPlanOperationJournalCompositeIdentity? Composite { get; set; }

    /// <summary>Exigido <c>true</c> por <see cref="JournalIdentityKind.Folder"/>.</summary>
    public bool? EmptyConfirmed { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool OwnershipValidated { get; set; }

    public JournalInventoryAction Action { get; set; }

    public JournalPhysicalState PhysicalState { get; set; }

    public JournalConfirmation Confirmation { get; set; }

    public string? ExpectedHash { get; set; }

    public IList<int> ReceiptSequences { get; } = new List<int>();
}

public sealed class ApiPlanOperationJournalCompositeIdentity
{
    public string ExactName { get; set; } = string.Empty;

    public string ObjectTypeName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string CanonicalDescription { get; set; } = string.Empty;

    public Guid TransactionGuid { get; set; }

    public Guid ApiGuid { get; set; }
}

public sealed class ApiPlanOperationJournalReceipt
{
    public int Sequence { get; set; }

    public JournalReceiptOperation Operation { get; set; }

    public string Stage { get; set; } = string.Empty;

    public JournalObjectType ObjectType { get; set; }

    public int Attempt { get; set; }

    public int? RetryOfSequence { get; set; }

    public JournalAttemptState AttemptState { get; set; }

    public JournalResult Result { get; set; }

    public JournalConfirmation Confirmation { get; set; }

    public JournalPhysicalState PhysicalState { get; set; }

    public bool RetryEligible { get; set; }

    public JournalRetryableReason? RetryableReason { get; set; }
}

public sealed class ApiPlanOperationJournalAbandonment
{
    public string Reason { get; set; } = string.Empty;

    public DateTime AuthorizedUtc { get; set; }

    public string AuthorizedBy { get; set; } = string.Empty;
}

public enum JournalOperationKind
{
    Apply = 0,
    Sync = 1,
    Remove = 2,

    /// <summary>
    /// Reservado à recuperação que não continua um envelope de negócio existente. Uma
    /// continuação de Apply, Sync ou Remove preserva o <c>operationKind</c> original.
    /// </summary>
    Recovery = 3,
}

public enum JournalPlanKind
{
    Generation = 0,
    Removal = 1,
    MetadataRecovery = 2,
}

public enum JournalEnvelopePhase
{
    Prepared = 0,
    Active = 1,
}

public enum JournalOperationState
{
    Pending = 0,
    Running = 1,
    Partial = 2,
    OutcomeUnknown = 3,
    Completed = 4,
    Removed = 5,
}

public enum JournalLogicalStage
{
    NotStarted = 0,
    IntentionRecorded = 1,
    TransactionPending = 2,
    FolderPending = 3,
    SdtsPending = 4,
    ProceduresPending = 5,
    ApiPending = 6,
    ApiSaveOutcomeUnknown = 7,
    ApiPhysicallySaved = 8,
    MetadataPending = 9,
    MetadataRecovered = 10,
    RemovalInProgress = 11,
    RemovalPartial = 12,
    RecoveryInProgress = 13,
    Abandoned = 14,
    Completed = 15,
    Removed = 16,

    /// <summary>
    /// Registro encerrado por decisão humana informada, depois de a operação ter gravado algo e
    /// parado no meio. Não é <see cref="Abandoned"/>: aquele pertence a um envelope que nunca
    /// tocou a KB, e por isso pode ser descartado sem consequência. Aqui houve gravação, o
    /// inventário fica preservado e o que se encerra é a **intenção ativa**, não os objetos.
    ///
    /// Existe porque um envelope interrompido bloqueia as operações seguintes, e sem esta saída
    /// a única alternativa era apagar o File do diário à mão — que apaga também a prova.
    /// </summary>
    Discarded = 17,
}

public enum JournalDurability
{
    Confirmed = 0,
    Unknown = 1,
}

public enum JournalIntentKind
{
    Current = 0,
    Imported = 1,
}

public enum JournalObjectType
{
    Transaction = 0,
    Folder = 1,
    ApiObject = 2,
    Procedure = 3,
    Sdt = 4,
    MetadataFile = 5,
}

public enum JournalIdentityKind
{
    Guid = 0,
    FileId = 1,
    Composite = 2,
    Folder = 3,
    None = 4,
}

public enum JournalInventoryAction
{
    Create = 0,
    Update = 1,
    Delete = 2,
    Preserve = 3,
}

public enum JournalPhysicalState
{
    Present = 0,
    Absent = 1,
    Unknown = 2,
}

public enum JournalConfirmation
{
    NotAttempted = 0,
    Confirmed = 1,
    Absent = 2,
    Divergent = 3,
    Unreadable = 4,
}

public enum JournalReceiptOperation
{
    Save = 0,
    Delete = 1,
}

public enum JournalAttemptState
{
    Started = 0,
    Finished = 1,
    Interrupted = 2,
}

public enum JournalResult
{
    Confirmed = 0,
    Failed = 1,
    OutcomeUnknown = 2,
}

public enum JournalRetryableReason
{
    StillPresentAfterDelete = 0,
}

/// <summary>
/// Motivos persistíveis no envelope. Não confundir com os códigos efêmeros de
/// <c>GateDiagnostic</c>, que ficam só no relatório.
/// </summary>
public enum JournalBlockReason
{
    OutcomeUnknown = 0,
    InventoryInsufficient = 1,
    IdentityAmbiguous = 2,
    IdentityDivergent = 3,
    UnreconciledNotAttempted = 4,
    TargetAbsentBeforeDelete = 5,
    StageFailed = 6,
    RetryBudgetExhausted = 7,

    /// <summary>
    /// Interrupção deliberada pelo usuário. Existe porque <c>StageFailed</c> é, pela decisão
    /// 24, «falha conhecida e não retryable comunicada por `NoteStageFailed`» — e um aborto
    /// não é falha nenhuma. A distinção não é cosmética: a reconciliação da P5 oferece coisas
    /// diferentes para algo que quebrou e para algo que alguém decidiu parar.
    /// </summary>
    UserAborted = 8,
}
