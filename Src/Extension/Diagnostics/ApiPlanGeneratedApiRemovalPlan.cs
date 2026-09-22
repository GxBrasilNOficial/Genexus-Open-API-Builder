#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// B086: plano de remoção a partir do File de metadata, sem tocar SDTs compartilhados.
/// </summary>
public sealed class ApiPlanGeneratedApiRemovalPlan
{
    private ApiPlanGeneratedApiRemovalPlan(
        string transactionName,
        string apiName,
        string apiGuid,
        string metadataFileName,
        string? folderName,
        bool folderWasCreated,
        bool folderOwnedByThisApi,
        Guid? folderGuid,
        bool folderShouldBeRemoved,
        IReadOnlyList<string> procedureNames,
        IReadOnlyList<string> ownSdtNames,
        IReadOnlyList<string> sharedSdtNamesPreserved)
    {
        TransactionName = transactionName;
        ApiName = apiName;
        ApiGuid = apiGuid;
        MetadataFileName = metadataFileName;
        FolderName = folderName;
        FolderWasCreated = folderWasCreated;
        FolderOwnedByThisApi = folderOwnedByThisApi;
        FolderGuid = folderGuid;
        FolderShouldBeRemoved = folderShouldBeRemoved;
        ProcedureNames = procedureNames;
        OwnSdtNames = ownSdtNames;
        SharedSdtNamesPreserved = sharedSdtNamesPreserved;
    }

    public string TransactionName { get; }
    public string ApiName { get; }
    public string ApiGuid { get; }
    public string MetadataFileName { get; }
    public string? FolderName { get; }
    public bool FolderWasCreated { get; }
    public bool FolderOwnedByThisApi { get; }
    public Guid? FolderGuid { get; }
    /// <summary>
    /// Inicialmente posse+nome; após <see cref="AttachPreviewCapture"/> pode
    /// cair a <c>false</c> quando o GUID persistido diverge do Folder na KB.
    /// </summary>
    public bool FolderShouldBeRemoved { get; private set; }
    public IReadOnlyList<string> ProcedureNames { get; }
    public IReadOnlyList<string> OwnSdtNames { get; }
    public IReadOnlyList<string> SharedSdtNamesPreserved { get; }

    /// <summary>
    /// B082 Etapa 2: identidades capturadas no Preview. Nulo até
    /// <see cref="AttachPreviewCapture"/>; o ResolveIntent exige captura presente.
    /// </summary>
    internal ApiPlanGeneratedApiRemovalPreviewCapture? PreviewCapture { get; private set; }

    internal void AttachPreviewCapture(ApiPlanGeneratedApiRemovalPreviewCapture capture)
    {
        PreviewCapture = capture ?? throw new ArgumentNullException(nameof(capture));

        // Homônimo com GUID divergente: a fila já preservava em BuildTargets;
        // alinhar anúncio/contagem/confirmação ao mesmo critério.
        if (FolderShouldBeRemoved
            && !ApiPlanTransactionFolderOwnership.MatchesPersistedGuid(FolderGuid, capture.FolderGuid))
        {
            FolderShouldBeRemoved = false;
        }
    }

    public static ApiPlanGeneratedApiRemovalPlan FromMetadata(
        JObject metadata,
        string expectedTransactionName,
        string expectedTransactionGuid)
    {
        if (metadata is null)
        {
            throw new ArgumentNullException(nameof(metadata));
        }

        if (string.IsNullOrWhiteSpace(expectedTransactionName))
        {
            throw new ArgumentException("Transaction name is required.", nameof(expectedTransactionName));
        }

        if (string.IsNullOrWhiteSpace(expectedTransactionGuid))
        {
            throw new ArgumentException("Transaction GUID is required.", nameof(expectedTransactionGuid));
        }

        RequireSupportedSchemaVersion(metadata["schemaVersion"]);
        RequireString(metadata.SelectToken("ownership.transactionName"), expectedTransactionName, "ownership.transactionName");
        RequireString(metadata.SelectToken("ownership.transactionGuid"), expectedTransactionGuid, "ownership.transactionGuid");

        var apiName = RequirePresent(metadata.SelectToken("ownership.apiName"), "ownership.apiName");
        var apiGuid = RequirePresent(metadata.SelectToken("ownership.apiGuid"), "ownership.apiGuid");
        var metadataFileName = RequirePresent(metadata.SelectToken("ownership.metadataFileName"), "ownership.metadataFileName");
        var folderName = metadata.SelectToken("objects.transactionFolder.name")?.Value<string>();
        var folderWasCreated = metadata.SelectToken("objects.transactionFolder.wasCreated")?.Value<bool>() == true;
        var folderOwnedByThisApi = ApiPlanTransactionFolderOwnership.ReadOwnedByThisApi(metadata);
        var folderGuid = ApiPlanTransactionFolderOwnership.TryReadFolderGuid(metadata);
        var resolvedFolderName = string.IsNullOrWhiteSpace(folderName) ? null : folderName;
        var folderShouldBeRemoved = folderOwnedByThisApi && !string.IsNullOrWhiteSpace(resolvedFolderName);

        var procedures = ReadStringArray(metadata.SelectToken("objects.procedures"));
        var shared = ReadStringArray(metadata.SelectToken("objects.sdts.shared"));
        // Ordem de exclusao: ListResponse tipa Items com Response; apagar Response antes falha na IDE.
        // V2 pode trazer objects.sdts.own com inventário completo (inclui SDTs hierárquicos).
        var ownSdts = ApiPlanGeneratedApiRemovalInventory.ResolveOwnSdtNames(metadata);

        foreach (var sharedName in shared)
        {
            if (ownSdts.Contains(sharedName, StringComparer.Ordinal))
            {
                throw new InvalidOperationException($"Plano de remoção bloqueado: SDT '{sharedName}' aparece como próprio e compartilhado.");
            }
        }

        return new ApiPlanGeneratedApiRemovalPlan(
            expectedTransactionName,
            apiName,
            apiGuid,
            metadataFileName,
            string.IsNullOrWhiteSpace(folderName) ? null : folderName,
            folderWasCreated,
            folderOwnedByThisApi,
            folderGuid,
            folderShouldBeRemoved,
            procedures,
            ownSdts,
            shared);
    }

    public string BuildConfirmationLists()
    {
        var presentProcedures = FilterToPreviewPresence(JournalObjectType.Procedure, ProcedureNames);
        var presentOwnSdts = FilterToPreviewPresence(JournalObjectType.Sdt, OwnSdtNames);
        var alreadyAbsent = BuildAlreadyAbsentItems();
        var builder = new System.Text.StringBuilder();
        builder.Append("Procedures presentes na KB (").Append(presentProcedures.Count).AppendLine("):");
        AppendIndentedItems(builder, presentProcedures);
        builder.AppendLine();
        builder.Append("SDTs próprios presentes na KB (").Append(presentOwnSdts.Count).AppendLine("):");
        AppendIndentedItems(builder, presentOwnSdts);
        builder.AppendLine();
        builder.Append("SDTs compartilhados preservados (").Append(SharedSdtNamesPreserved.Count).AppendLine("):");
        AppendIndentedItems(builder, SharedSdtNamesPreserved);

        if (alreadyAbsent.Count > 0)
        {
            builder.AppendLine();
            builder.Append("Já ausentes na KB (não serão apagados nesta execução) (")
                .Append(alreadyAbsent.Count)
                .AppendLine("):");
            AppendIndentedItems(builder, alreadyAbsent);
        }

        return builder.ToString().TrimEnd();
    }

    public string BuildConfirmationSummary()
    {
        var builder = new System.Text.StringBuilder();
        builder.Append("Transaction: ").AppendLine(TransactionName);
        builder.Append("API Object: ").AppendLine(ApiName);
        builder.Append("Metadata File: ").AppendLine(MetadataFileName);
        builder.AppendLine();
        builder.AppendLine(BuildConfirmationLists());

        if (!string.IsNullOrWhiteSpace(FolderName))
        {
            builder.AppendLine();
            if (FolderShouldBeRemoved)
            {
                if (FolderWasCreated)
                {
                    builder.Append("Folder: ").Append(FolderName).AppendLine(" (criado pela extensão; apagar só se ficar vazio)");
                }
                else
                {
                    builder.Append("Folder: ").Append(FolderName).AppendLine(" (próprio da API; a remoção apaga se ficar vazio)");
                }
            }
            else
            {
                builder.Append("Folder: ").Append(FolderName).AppendLine(" (reutilizado; nunca apagar)");
            }
        }

        builder.AppendLine();
        builder.Append("Business Component da Transaction: não será revertido.");
        return builder.ToString();
    }

    private static void AppendIndentedItems(System.Text.StringBuilder builder, IReadOnlyList<string> items)
    {
        if (items.Count == 0)
        {
            builder.AppendLine("  (nenhum)");
            return;
        }

        foreach (var item in items)
        {
            builder.Append("  - ").AppendLine(item);
        }
    }

    /// <summary>
    /// B125: o inventário da metadata continua inteiro para o diário e a recuperação, mas o
    /// Preview não pode anunciar como exclusão um alvo que já está ausente na KB.
    /// </summary>
    private IReadOnlyList<string> FilterToPreviewPresence(JournalObjectType objectType, IReadOnlyList<string> names)
    {
        if (PreviewCapture is null)
        {
            return names;
        }

        return names
            .Where(name => PreviewCapture.PresentObjectGuids.ContainsKey(
                ApiPlanGeneratedApiRemovalPreviewCapture.Key(objectType, name)))
            .ToArray();
    }

    private IReadOnlyList<string> BuildAlreadyAbsentItems()
    {
        if (PreviewCapture is null)
        {
            return Array.Empty<string>();
        }

        var absent = new List<string>();
        AddIfAbsent(absent, JournalObjectType.ApiObject, "API Object", ApiName);
        AddAbsentItems(absent, JournalObjectType.Procedure, "Procedure", ProcedureNames);
        AddAbsentItems(absent, JournalObjectType.Sdt, "SDT", OwnSdtNames);
        return absent;
    }

    private void AddIfAbsent(List<string> absent, JournalObjectType objectType, string label, string name)
    {
        if (!PreviewCapture!.PresentObjectGuids.ContainsKey(
                ApiPlanGeneratedApiRemovalPreviewCapture.Key(objectType, name)))
        {
            absent.Add(label + ": " + name);
        }
    }

    private void AddAbsentItems(
        List<string> absent,
        JournalObjectType objectType,
        string label,
        IReadOnlyList<string> names)
    {
        foreach (var name in names)
        {
            AddIfAbsent(absent, objectType, label, name);
        }
    }

    /// <summary>
    /// B111/F3 P8: a recusa por metadata insuficiente bloqueava sem dizer o que fazer, enquanto a
    /// recusa irmã da metadata hierárquica ilegível já apontava a saída. O nome do campo continua
    /// no começo — quem edita metadata legada precisa dele —, mas a frase que a pessoa lê termina
    /// com um caminho, como as mensagens da seção 10 da P8.
    /// </summary>
    private const string InvalidMetadataExit =
        " A remoção não apaga nada sem o inventário completo: corrija o File de metadata ou, para regerar a API sobre o que restou na KB, apague o File de metadata e o API Object e execute o Wizard de novo.";

    private static string RequirePresent(JToken? token, string path)
    {
        if (token is null || token.Type != JTokenType.String || string.IsNullOrWhiteSpace(token.Value<string>()))
        {
            throw new InvalidOperationException($"Metadata de remoção inválida: campo '{path}' ausente." + InvalidMetadataExit);
        }

        return token.Value<string>()!;
    }

    private static void RequireSupportedSchemaVersion(JToken? token)
    {
        var actual = token is not null && token.Type == JTokenType.String ? token.Value<string>() : null;
        var supported = ApiPlanMetadataSchema.IsSupported(actual);
        if (!supported)
        {
            throw new InvalidOperationException(
                $"Metadata de remoção incompatível em 'schemaVersion': esperado {ApiPlanMetadataSchema.FormatSupportedVersionList()}, encontrado '{actual ?? "<ausente>"}'." + InvalidMetadataExit);
        }
    }

    private static void RequireString(JToken? token, string expected, string path)
    {
        var actual = RequirePresent(token, path);
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Metadata de remoção incompatível em '{path}': esperado '{expected}', encontrado '{actual}'." + InvalidMetadataExit);
        }
    }

    private static IReadOnlyList<string> ReadStringArray(JToken? token)
    {
        if (token is not JArray array)
        {
            return Array.Empty<string>();
        }

        return array
            .Where(item => item.Type == JTokenType.String && !string.IsNullOrWhiteSpace(item.Value<string>()))
            .Select(item => item.Value<string>()!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
