#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Description canônica de objetos gerados: "{Nome} - by Genexus Open API Builder".
/// Aceita sentinelas legadas (com IDs de backlog ou prosa antiga do API Object) só para reconhecimento.
/// </summary>
public static class ApiPlanOwnedObjectDescription
{
    public const string GeneratorName = "Genexus Open API Builder";
    public const string Suffix = " - by Genexus Open API Builder";

    private const string LegacyProcedurePrefix = "Genexus Open API Builder B050-B053 Procedure";
    private const string LegacySdtPrefix = "Genexus Open API Builder B040-B046 SDT";
    private const string LegacyMetadataPrefix = "Genexus Open API Builder B060 Metadata File";
    private const string LegacyTransactionFolderPrefix = "Genexus Open API Builder Transaction API folder";
    private const string LegacySharedFolderDescription = "Genexus Open API Builder shared SDTs folder";
    private const string LegacyPreferencesDescription = "Genexus Open API Builder Wizard Preferences";

    public static string Create(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            throw new ArgumentException("Object name is required.", nameof(objectName));
        }

        return objectName + Suffix;
    }

    public static bool IsCanonical(string? description, string objectName) =>
        string.Equals(description, Create(objectName), StringComparison.Ordinal);

    public static bool IsOwnedProcedure(string? description, string procedureName)
    {
        if (IsCanonical(description, procedureName))
        {
            return true;
        }

        if (TryCreateLegacyProcedureDescription(procedureName, out var expected)
            && string.Equals(description, expected, StringComparison.Ordinal))
        {
            return true;
        }

        return TryCreateAlternateLegacyDeleteProcedureDescription(procedureName, out var alternateDelete)
            && string.Equals(description, alternateDelete, StringComparison.Ordinal);
    }

    public static bool IsOwnedSdt(string? description, string sdtName)
    {
        if (IsCanonical(description, sdtName))
        {
            return true;
        }

        return TryCreateLegacySdtDescription(sdtName, out var expected)
            && string.Equals(description, expected, StringComparison.Ordinal);
    }

    public static bool IsOwnedMetadataFile(string? description, string metadataFileName) =>
        IsCanonical(description, metadataFileName);

    public static bool IsOwnedMetadataFile(string? description, string metadataFileName, string transactionName) =>
        IsOwnedMetadataFile(description, metadataFileName)
        || TryCreateLegacyMetadataDescription(transactionName, metadataFileName, out var expected)
            && string.Equals(description, expected, StringComparison.Ordinal);

    public static string CreateLegacyProcedureDescription(string procedureName)
    {
        var serviceName = GetApiServiceName(procedureName, "proc");
        return $"{LegacyProcedurePrefix} - {ResolveProcedureBacklogId(serviceName)} - {serviceName}";
    }

    public static string CreateLegacySdtDescription(string sdtName)
    {
        if (string.Equals(sdtName, "sdt_API_ErrorMessage", StringComparison.Ordinal))
        {
            return $"{LegacySdtPrefix} - B102 - SharedErrorMessage";
        }

        if (string.Equals(sdtName, "sdt_API_ErrorResponse", StringComparison.Ordinal))
        {
            return $"{LegacySdtPrefix} - B045/B046 - SharedErrorResponse";
        }

        if (string.Equals(sdtName, "sdt_API_Pagination", StringComparison.Ordinal))
        {
            return $"{LegacySdtPrefix} - B045/B046 - SharedPagination";
        }

        var kind = GetApiServiceName(sdtName, "sdt");
        return $"{LegacySdtPrefix} - {ResolveSdtBacklogId(kind)} - {kind}";
    }

    public static string CreateLegacyMetadataDescription(string transactionName, string apiName)
    {
        if (string.IsNullOrWhiteSpace(transactionName))
        {
            throw new ArgumentException("Transaction name is required.", nameof(transactionName));
        }

        if (string.IsNullOrWhiteSpace(apiName))
        {
            throw new ArgumentException("API name is required.", nameof(apiName));
        }

        return $"{LegacyMetadataPrefix} - Transaction={transactionName} - Api={apiName}";
    }

    private static bool TryCreateLegacyProcedureDescription(string procedureName, out string expected)
    {
        try
        {
            expected = CreateLegacyProcedureDescription(procedureName);
            return true;
        }
        catch (ArgumentException)
        {
            expected = string.Empty;
            return false;
        }
    }

    private static bool TryCreateAlternateLegacyDeleteProcedureDescription(string procedureName, out string expected)
    {
        try
        {
            var serviceName = GetApiServiceName(procedureName, "proc");
            if (!string.Equals(serviceName, "Delete", StringComparison.OrdinalIgnoreCase))
            {
                expected = string.Empty;
                return false;
            }

            expected = $"{LegacyProcedurePrefix} - B050-B053 - Delete";
            return true;
        }
        catch (ArgumentException)
        {
            expected = string.Empty;
            return false;
        }
    }

    private static bool TryCreateLegacySdtDescription(string sdtName, out string expected)
    {
        try
        {
            expected = CreateLegacySdtDescription(sdtName);
            return true;
        }
        catch (ArgumentException)
        {
            expected = string.Empty;
            return false;
        }
    }

    private static bool TryCreateLegacyMetadataDescription(
        string transactionName,
        string metadataFileName,
        out string expected)
    {
        try
        {
            expected = CreateLegacyMetadataDescription(transactionName, GetApiNameFromMetadataFileName(metadataFileName));
            return true;
        }
        catch (ArgumentException)
        {
            expected = string.Empty;
            return false;
        }
    }

    public static bool IsOwnedSharedFolder(string? description) =>
        IsCanonical(description, "GxOpenAPI")
        || string.Equals(description, LegacySharedFolderDescription, StringComparison.Ordinal);

    public static bool IsOwnedPreferencesFile(string? description) =>
        IsCanonical(description, "GxOpenApiBuilder_Settings")
        || string.Equals(description, LegacyPreferencesDescription, StringComparison.Ordinal);

    public static string CreateTransactionFolderDescription(string folderName) => Create(folderName);

    public static string CreateLegacyTransactionFolderDescription(string transactionName) =>
        $"{LegacyTransactionFolderPrefix} - Transaction={transactionName}";

    /// <summary>
    /// Folder reutilizável: Description humana/vazia, canônica deste Folder, ou sentinela legada desta Transaction.
    /// Sentinela da extensão de outra Transaction (ou nome divergente) bloqueia.
    /// </summary>
    public static bool IsReusableTransactionFolderDescription(
        string? description,
        string folderName,
        string transactionName)
    {
        if (IsCanonical(description, folderName)
            || string.Equals(description, CreateLegacyTransactionFolderDescription(transactionName), StringComparison.Ordinal))
        {
            return true;
        }

        if (StartsWithLegacy(description, LegacyTransactionFolderPrefix))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(description)
            && description!.EndsWith(Suffix, StringComparison.Ordinal)
            && !IsCanonical(description, folderName))
        {
            return false;
        }

        return true;
    }

    public static string CreateApiObjectDescription(string apiName) => Create(apiName);

    public static string CreateLegacyApiObjectDescription(string transactionName, bool withTrailingPeriod = false)
    {
        var value = $"REST API for {transactionName}, generated by {GeneratorName}";
        return withTrailingPeriod ? value + "." : value;
    }

    public static IReadOnlyList<string> CreateApiObjectDescriptionFallbacks(string apiName, string transactionName)
    {
        if (string.IsNullOrWhiteSpace(apiName))
        {
            throw new ArgumentException("API name is required.", nameof(apiName));
        }

        if (string.IsNullOrWhiteSpace(transactionName))
        {
            throw new ArgumentException("Transaction name is required.", nameof(transactionName));
        }

        return new[]
        {
            CreateApiObjectDescription(apiName),
            CreateLegacyApiObjectDescription(transactionName, withTrailingPeriod: false),
            CreateLegacyApiObjectDescription(transactionName, withTrailingPeriod: true),
        }
        .Distinct(StringComparer.Ordinal)
        .ToArray();
    }

    private static string GetApiServiceName(string objectName, string expectedPrefix)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            throw new ArgumentException("Object name is required.", nameof(objectName));
        }

        if (!objectName.StartsWith(expectedPrefix, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Object name must start with '{expectedPrefix}'.", nameof(objectName));
        }

        const string marker = "_API_";
        var markerIndex = objectName.LastIndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < expectedPrefix.Length || markerIndex + marker.Length >= objectName.Length)
        {
            throw new ArgumentException($"Object name must contain a service marker '{marker}'.", nameof(objectName));
        }

        return objectName.Substring(markerIndex + marker.Length);
    }

    private static string GetApiNameFromMetadataFileName(string metadataFileName)
    {
        const string suffix = "_Metadata";
        if (string.IsNullOrWhiteSpace(metadataFileName)
            || !metadataFileName.EndsWith(suffix, StringComparison.Ordinal)
            || metadataFileName.Length == suffix.Length)
        {
            throw new ArgumentException("Metadata file name must end with '_Metadata'.", nameof(metadataFileName));
        }

        return metadataFileName.Substring(0, metadataFileName.Length - suffix.Length);
    }

    private static string ResolveProcedureBacklogId(string serviceName)
    {
        if (string.Equals(serviceName, "List", StringComparison.OrdinalIgnoreCase)) return "B050";
        if (string.Equals(serviceName, "Get", StringComparison.OrdinalIgnoreCase)) return "B051";
        if (string.Equals(serviceName, "Create", StringComparison.OrdinalIgnoreCase)) return "B052";
        if (string.Equals(serviceName, "Update", StringComparison.OrdinalIgnoreCase)) return "B053";
        return string.Equals(serviceName, "Delete", StringComparison.OrdinalIgnoreCase) ? "B100" : "B050-B053";
    }

    private static string ResolveSdtBacklogId(string kind)
    {
        if (string.Equals(kind, "CreateRequest", StringComparison.OrdinalIgnoreCase)) return "B040";
        if (string.Equals(kind, "UpdateRequest", StringComparison.OrdinalIgnoreCase)) return "B041";
        if (string.Equals(kind, "Response", StringComparison.OrdinalIgnoreCase)) return "B042";
        if (string.Equals(kind, "ListFilters", StringComparison.OrdinalIgnoreCase)) return "B043";
        return string.Equals(kind, "ListResponse", StringComparison.OrdinalIgnoreCase) ? "B044" : "B040-B046";
    }

    private static bool StartsWithLegacy(string? description, string prefix) =>
        !string.IsNullOrEmpty(description)
        && description!.StartsWith(prefix, StringComparison.Ordinal);
}
