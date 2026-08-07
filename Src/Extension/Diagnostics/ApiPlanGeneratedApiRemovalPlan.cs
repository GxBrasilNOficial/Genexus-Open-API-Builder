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
    public IReadOnlyList<string> ProcedureNames { get; }
    public IReadOnlyList<string> OwnSdtNames { get; }
    public IReadOnlyList<string> SharedSdtNamesPreserved { get; }

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

        RequireString(metadata["schemaVersion"], "GOAB_API_METADATA_B060_V1", "schemaVersion");
        RequireString(metadata.SelectToken("ownership.transactionName"), expectedTransactionName, "ownership.transactionName");
        RequireString(metadata.SelectToken("ownership.transactionGuid"), expectedTransactionGuid, "ownership.transactionGuid");

        var apiName = RequirePresent(metadata.SelectToken("ownership.apiName"), "ownership.apiName");
        var apiGuid = RequirePresent(metadata.SelectToken("ownership.apiGuid"), "ownership.apiGuid");
        var metadataFileName = RequirePresent(metadata.SelectToken("ownership.metadataFileName"), "ownership.metadataFileName");
        var folderName = metadata.SelectToken("objects.transactionFolder.name")?.Value<string>();
        var folderWasCreated = metadata.SelectToken("objects.transactionFolder.wasCreated")?.Value<bool>() == true;

        var procedures = ReadStringArray(metadata.SelectToken("objects.procedures"));
        var shared = ReadStringArray(metadata.SelectToken("objects.sdts.shared"));
        var ownSdts = new[]
            {
                metadata.SelectToken("objects.sdts.createRequest")?.Value<string>(),
                metadata.SelectToken("objects.sdts.updateRequest")?.Value<string>(),
                metadata.SelectToken("objects.sdts.response")?.Value<string>(),
                metadata.SelectToken("objects.sdts.listFilters")?.Value<string>(),
                metadata.SelectToken("objects.sdts.listResponse")?.Value<string>(),
            }
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

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
            procedures,
            ownSdts,
            shared);
    }

    public string BuildConfirmationSummary()
    {
        var folderLine = string.IsNullOrWhiteSpace(FolderName)
            ? null
            : FolderWasCreated
                ? $"Folder: {FolderName} (criado pela extensão; apagar só se ficar vazio)"
                : $"Folder: {FolderName} (reutilizado; nunca apagar)";

        var lines = new[]
        {
            $"Transaction: {TransactionName}",
            $"API Object: {ApiName}",
            $"Metadata File: {MetadataFileName}",
            $"Procedures ({ProcedureNames.Count}): {string.Join(", ", ProcedureNames)}",
            $"SDTs próprios ({OwnSdtNames.Count}): {string.Join(", ", OwnSdtNames)}",
            $"SDTs compartilhados preservados ({SharedSdtNamesPreserved.Count}): {string.Join(", ", SharedSdtNamesPreserved)}",
            folderLine,
            "Business Component da Transaction: não será revertido.",
        };

        return string.Join(Environment.NewLine, lines.Where(line => !string.IsNullOrWhiteSpace(line)));
    }

    private static string RequirePresent(JToken? token, string path)
    {
        if (token is null || token.Type != JTokenType.String || string.IsNullOrWhiteSpace(token.Value<string>()))
        {
            throw new InvalidOperationException($"Metadata de remoção inválida: campo '{path}' ausente.");
        }

        return token.Value<string>()!;
    }

    private static void RequireString(JToken? token, string expected, string path)
    {
        var actual = RequirePresent(token, path);
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Metadata de remoção incompatível em '{path}': esperado '{expected}', encontrado '{actual}'.");
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
