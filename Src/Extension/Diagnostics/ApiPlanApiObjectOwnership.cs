#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// B087: posse do API Object pela metadata de integridade, com fallback na Description
/// somente quando o File de metadata ainda não existe.
/// </summary>
public static class ApiPlanApiObjectOwnership
{
    public enum OwnershipKind
    {
        NotOwned = 0,
        OwnedByMetadata = 1,
        OwnedByDescriptionFallback = 2,
    }

    public static bool MatchesMetadataOwnership(
        JObject metadata,
        string expectedSchemaVersion,
        string expectedApiName,
        string apiGuid)
    {
        if (metadata is null)
        {
            throw new ArgumentNullException(nameof(metadata));
        }

        return HasString(metadata["schemaVersion"], expectedSchemaVersion)
            && HasString(metadata.SelectToken("ownership.apiName"), expectedApiName)
            && HasString(metadata.SelectToken("ownership.apiGuid"), apiGuid);
    }

    public static OwnershipKind Resolve(
        bool ownedMetadataFilePresent,
        JObject? metadata,
        string expectedSchemaVersion,
        string expectedApiName,
        string apiGuid,
        bool integrityCompatible,
        bool serviceSourceManaged,
        string? actualApiDescription,
        IEnumerable<string> expectedDescriptionFallbacks)
    {
        if (string.IsNullOrWhiteSpace(expectedSchemaVersion))
        {
            throw new ArgumentException("Schema version is required.", nameof(expectedSchemaVersion));
        }

        if (string.IsNullOrWhiteSpace(expectedApiName))
        {
            throw new ArgumentException("API name is required.", nameof(expectedApiName));
        }

        if (string.IsNullOrWhiteSpace(apiGuid))
        {
            throw new ArgumentException("API GUID is required.", nameof(apiGuid));
        }

        if (expectedDescriptionFallbacks is null)
        {
            throw new ArgumentNullException(nameof(expectedDescriptionFallbacks));
        }

        var fallbacks = expectedDescriptionFallbacks
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (fallbacks.Length == 0)
        {
            throw new ArgumentException("At least one fallback description is required.", nameof(expectedDescriptionFallbacks));
        }

        if (ownedMetadataFilePresent)
        {
            if (metadata is null ||
                !MatchesMetadataOwnership(metadata, expectedSchemaVersion, expectedApiName, apiGuid) ||
                !integrityCompatible ||
                !serviceSourceManaged)
            {
                return OwnershipKind.NotOwned;
            }

            return OwnershipKind.OwnedByMetadata;
        }

        if (serviceSourceManaged &&
            fallbacks.Any(expected => string.Equals(actualApiDescription, expected, StringComparison.Ordinal)))
        {
            return OwnershipKind.OwnedByDescriptionFallback;
        }

        return OwnershipKind.NotOwned;
    }

    public static bool IsOwned(OwnershipKind kind) =>
        kind == OwnershipKind.OwnedByMetadata || kind == OwnershipKind.OwnedByDescriptionFallback;

    private static bool HasString(JToken? token, string expectedValue)
    {
        return token is not null &&
            token.Type == JTokenType.String &&
            string.Equals(token.Value<string>(), expectedValue, StringComparison.Ordinal);
    }
}
