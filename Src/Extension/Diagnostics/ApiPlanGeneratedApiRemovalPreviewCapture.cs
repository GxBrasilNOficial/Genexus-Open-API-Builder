#nullable enable

using System;
using System.Collections.Generic;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// B082 Etapa 2 / decisão 7 — identidades capturadas no Preview do Remover.
/// A mesma instância do plano carrega esta captura; o ResolveIntent confere uma
/// vez antes da primeira exclusão e bloqueia com zero deletes se divergir.
/// </summary>
internal sealed class ApiPlanGeneratedApiRemovalPreviewCapture
{
    internal ApiPlanGeneratedApiRemovalPreviewCapture(
        Guid transactionGuid,
        Guid metadataFileGuid,
        string metadataSha256,
        string? metadataSchemaVersion,
        Guid? applicationId,
        string? contractHash,
        Guid? folderGuid,
        IReadOnlyDictionary<string, Guid> presentObjectGuids)
    {
        TransactionGuid = transactionGuid;
        MetadataFileGuid = metadataFileGuid;
        MetadataSha256 = metadataSha256 ?? throw new ArgumentNullException(nameof(metadataSha256));
        MetadataSchemaVersion = metadataSchemaVersion;
        ApplicationId = applicationId;
        ContractHash = contractHash;
        FolderGuid = folderGuid;
        PresentObjectGuids = presentObjectGuids ?? throw new ArgumentNullException(nameof(presentObjectGuids));
    }

    internal Guid TransactionGuid { get; }

    internal Guid MetadataFileGuid { get; }

    internal string MetadataSha256 { get; }

    internal string? MetadataSchemaVersion { get; }

    internal Guid? ApplicationId { get; }

    internal string? ContractHash { get; }

    /// <summary>GUID do Folder no Preview, quando encontrado; nulo se ausente ou não aplicável.</summary>
    internal Guid? FolderGuid { get; }

    /// <summary>
    /// GUIDs dos alvos destrutivos presentes no Preview, chave
    /// <c>{ObjectType}:{Name}</c> (ex.: <c>ApiObject:apiTeste</c>).
    /// </summary>
    internal IReadOnlyDictionary<string, Guid> PresentObjectGuids { get; }

    internal static string Key(JournalObjectType objectType, string name) =>
        objectType + ":" + (name ?? string.Empty);
}
