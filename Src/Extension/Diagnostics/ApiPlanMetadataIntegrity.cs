#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

public static class ApiPlanMetadataIntegrity
{
    public const string Version = "GOAB_B067_INTEGRITY_V1";

    public static JObject Create(
        JArray descriptionsContract,
        JObject plannedContract,
        string descriptionSentinel,
        string apiObjectGuid,
        string serviceSourceMode,
        string serviceSourceCurrent,
        string serviceSourceExpected)
    {
        if (descriptionsContract is null)
        {
            throw new ArgumentNullException(nameof(descriptionsContract));
        }

        if (plannedContract is null)
        {
            throw new ArgumentNullException(nameof(plannedContract));
        }

        return new JObject
        {
            ["version"] = Version,
            ["scope"] = "Generated descriptions, ownership and essential planned API contract before conservative rewrite",
            ["generatedDescriptions"] = new JObject
            {
                ["hash"] = ComputeJsonSha256(descriptionsContract),
                ["services"] = descriptionsContract,
            },
            ["plannedContract"] = new JObject
            {
                ["hash"] = ComputeJsonSha256(plannedContract),
                ["contract"] = plannedContract,
            },
            ["apiObject"] = new JObject
            {
                ["descriptionSentinel"] = descriptionSentinel ?? throw new ArgumentNullException(nameof(descriptionSentinel)),
                ["guid"] = apiObjectGuid ?? throw new ArgumentNullException(nameof(apiObjectGuid)),
                ["serviceSourceMode"] = serviceSourceMode ?? throw new ArgumentNullException(nameof(serviceSourceMode)),
                ["serviceSourceCurrentHash"] = ComputeNormalizedTextSha256(serviceSourceCurrent),
                ["serviceSourceExpectedHash"] = ComputeNormalizedTextSha256(serviceSourceExpected),
            },
        };
    }

    public static bool HasCompatibleIntegrity(
        JObject metadata,
        string generatedDescriptionsHash,
        string plannedContractHash,
        string actualDescriptionsHash,
        string descriptionSentinel,
        string serviceSourceExpected,
        bool serviceSourceContractMatches)
    {
        return HasCompatibleIntegrity(
            metadata,
            generatedDescriptionsHash,
            new[] { plannedContractHash },
            actualDescriptionsHash,
            new[] { descriptionSentinel },
            new[] { serviceSourceExpected },
            serviceSourceContractMatches);
    }

    public static bool HasCompatibleIntegrity(
        JObject metadata,
        string generatedDescriptionsHash,
        string plannedContractHash,
        string actualDescriptionsHash,
        string descriptionSentinel,
        IEnumerable<string> compatibleServiceSourceExpectedValues,
        bool serviceSourceContractMatches)
    {
        return HasCompatibleIntegrity(
            metadata,
            generatedDescriptionsHash,
            new[] { plannedContractHash },
            actualDescriptionsHash,
            new[] { descriptionSentinel },
            compatibleServiceSourceExpectedValues,
            serviceSourceContractMatches);
    }

    public static bool HasCompatibleIntegrity(
        JObject metadata,
        string generatedDescriptionsHash,
        IEnumerable<string> compatiblePlannedContractHashes,
        string actualDescriptionsHash,
        string descriptionSentinel,
        IEnumerable<string> compatibleServiceSourceExpectedValues,
        bool serviceSourceContractMatches)
    {
        return HasCompatibleIntegrity(
            metadata,
            generatedDescriptionsHash,
            compatiblePlannedContractHashes,
            actualDescriptionsHash,
            new[] { descriptionSentinel },
            compatibleServiceSourceExpectedValues,
            serviceSourceContractMatches);
    }

    public static bool HasCompatibleIntegrity(
        JObject metadata,
        string generatedDescriptionsHash,
        IEnumerable<string> compatiblePlannedContractHashes,
        string actualDescriptionsHash,
        IEnumerable<string> compatibleDescriptionSentinels,
        IEnumerable<string> compatibleServiceSourceExpectedValues,
        bool serviceSourceContractMatches)
    {
        if (compatibleServiceSourceExpectedValues is null)
        {
            throw new ArgumentNullException(nameof(compatibleServiceSourceExpectedValues));
        }

        if (compatiblePlannedContractHashes is null)
        {
            throw new ArgumentNullException(nameof(compatiblePlannedContractHashes));
        }

        if (compatibleDescriptionSentinels is null)
        {
            throw new ArgumentNullException(nameof(compatibleDescriptionSentinels));
        }

        var integrity = metadata?["integrity"] as JObject;
        if (integrity is null)
        {
            return true;
        }

        var plannedHashes = compatiblePlannedContractHashes
            .Where(hash => !string.IsNullOrWhiteSpace(hash))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var expectedHashes = compatibleServiceSourceExpectedValues
            .Select(ComputeNormalizedTextSha256)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var sentinels = compatibleDescriptionSentinels
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var storedPlannedHash = integrity.SelectToken("plannedContract.hash")?.Value<string>() ?? string.Empty;
        var storedExpectedHash = integrity.SelectToken("apiObject.serviceSourceExpectedHash")?.Value<string>() ?? string.Empty;
        var storedSentinel = integrity.SelectToken("apiObject.descriptionSentinel")?.Value<string>() ?? string.Empty;

        return HasString(integrity["version"], Version) &&
            HasString(integrity.SelectToken("generatedDescriptions.hash"), generatedDescriptionsHash) &&
            plannedHashes.Any(hash => string.Equals(hash, storedPlannedHash, StringComparison.Ordinal)) &&
            string.Equals(actualDescriptionsHash, generatedDescriptionsHash, StringComparison.Ordinal) &&
            sentinels.Any(sentinel => string.Equals(sentinel, storedSentinel, StringComparison.Ordinal)) &&
            expectedHashes.Any(hash => string.Equals(hash, storedExpectedHash, StringComparison.Ordinal)) &&
            serviceSourceContractMatches;
    }

    /// <summary>
    /// Valida o estado que a extensao gravou na ultima execucao, sem comparar
    /// o contrato atual desejado. Essa e a porta de seguranca usada antes de
    /// uma alteracao deliberada pelo Wizard ou pelo Sincronizar.
    /// </summary>
    public static bool HasCompatibleGeneratedBaseline(
        JObject metadata,
        string actualServiceDescriptionsHash,
        string actualServiceSourceHash,
        string actualApiDescription,
        string actualApiObjectGuid)
    {
        if (metadata is null)
        {
            throw new ArgumentNullException(nameof(metadata));
        }

        var integrity = metadata["integrity"] as JObject;
        if (integrity is null)
        {
            // Metadata anterior ao B067 nao possui baseline suficiente para
            // distinguir uma edicao manual. O primeiro upgrade continua
            // permitido; a proxima gravacao passa a persistir o baseline.
            return true;
        }

        var storedDescriptionSentinel = integrity.SelectToken("apiObject.descriptionSentinel")?.Value<string>() ?? string.Empty;
        var storedApiGuid = integrity.SelectToken("apiObject.guid")?.Value<string>() ?? string.Empty;
        var storedSourceHash = integrity.SelectToken("apiObject.serviceSourceCurrentHash")?.Value<string>() ?? string.Empty;
        var storedDescriptionsHash = integrity.SelectToken("generatedDescriptions.hash")?.Value<string>() ?? string.Empty;

        return DiagnoseGeneratedBaseline(
            metadata,
            actualServiceDescriptionsHash,
            actualServiceSourceHash,
            actualApiDescription,
            actualApiObjectGuid).IsCompatible;
    }

    public static GeneratedBaselineDiagnosis DiagnoseGeneratedBaseline(
        JObject metadata,
        string actualServiceDescriptionsHash,
        string actualServiceSourceHash,
        string actualApiDescription,
        string actualApiObjectGuid)
    {
        if (metadata is null)
        {
            throw new ArgumentNullException(nameof(metadata));
        }

        var integrity = metadata["integrity"] as JObject;
        if (integrity is null)
        {
            return GeneratedBaselineDiagnosis.IntegrityAbsentAccepted(
                actualApiObjectGuid ?? string.Empty,
                actualApiDescription ?? string.Empty,
                actualServiceSourceHash ?? string.Empty,
                actualServiceDescriptionsHash ?? string.Empty);
        }

        var storedDescriptionSentinel = integrity.SelectToken("apiObject.descriptionSentinel")?.Value<string>() ?? string.Empty;
        var storedApiGuid = integrity.SelectToken("apiObject.guid")?.Value<string>() ?? string.Empty;
        var storedSourceHash = integrity.SelectToken("apiObject.serviceSourceCurrentHash")?.Value<string>() ?? string.Empty;
        var storedDescriptionsHash = integrity.SelectToken("generatedDescriptions.hash")?.Value<string>() ?? string.Empty;
        var versionOk = HasString(integrity["version"], Version);
        var guidOk = string.Equals(actualApiObjectGuid ?? string.Empty, storedApiGuid, StringComparison.Ordinal);
        var descriptionOk = string.Equals(actualApiDescription ?? string.Empty, storedDescriptionSentinel, StringComparison.Ordinal);
        var sourceHashOk = string.Equals(actualServiceSourceHash ?? string.Empty, storedSourceHash, StringComparison.Ordinal);
        var descriptionsHashOk = string.Equals(actualServiceDescriptionsHash ?? string.Empty, storedDescriptionsHash, StringComparison.Ordinal);
        var failingClause = !versionOk
            ? "BaselineVersionMismatch"
            : !guidOk
                ? "BaselineGuidMismatch"
                : !descriptionOk
                    ? "BaselineDescriptionMismatch"
                    : !sourceHashOk
                        ? "BaselineServiceSourceHashMismatch"
                        : !descriptionsHashOk
                            ? "BaselineServiceDescriptionsHashMismatch"
                            : "None";
        return new GeneratedBaselineDiagnosis(
            integrityPresent: true,
            versionOk,
            guidOk,
            descriptionOk,
            sourceHashOk,
            descriptionsHashOk,
            storedApiGuid,
            actualApiObjectGuid ?? string.Empty,
            storedDescriptionSentinel,
            actualApiDescription ?? string.Empty,
            storedSourceHash,
            actualServiceSourceHash ?? string.Empty,
            storedDescriptionsHash,
            actualServiceDescriptionsHash ?? string.Empty,
            failingClause);
    }

    public static JArray CreateServiceDescriptionsContractFromSource(string source, IEnumerable<string> serviceNames)
    {
        if (serviceNames is null)
        {
            throw new ArgumentNullException(nameof(serviceNames));
        }

        return new JArray(serviceNames
            .OrderBy(serviceName => serviceName, StringComparer.Ordinal)
            .Select(serviceName => new JObject
            {
                ["serviceName"] = serviceName,
                ["description"] = ReadDescription(source, serviceName),
            }));
    }

    public static MetadataFingerprintDiagnosis DiagnoseMetadataFingerprint(JObject metadata)
    {
        if (metadata is null)
        {
            throw new ArgumentNullException(nameof(metadata));
        }

        var fingerprint = metadata["fingerprint"] as JObject;
        if (fingerprint is null)
        {
            return MetadataFingerprintDiagnosis.AbsentAccepted();
        }

        var algorithm = fingerprint["algorithm"]?.Value<string>() ?? string.Empty;
        var scope = fingerprint["scope"]?.Value<string>() ?? string.Empty;
        var storedValue = fingerprint["value"]?.Value<string>() ?? string.Empty;
        var algorithmOk = string.Equals(algorithm, "SHA-256", StringComparison.Ordinal);
        var scopeOk = string.Equals(scope, "metadataWithoutFingerprint", StringComparison.Ordinal);
        var valuePresent = !string.IsNullOrWhiteSpace(storedValue);
        var snapshot = (JObject)metadata.DeepClone();
        snapshot.Remove("fingerprint");
        var snapshotJson = snapshot.ToString(Formatting.None);
        var actualValue = ComputeSha256(Encoding.UTF8.GetBytes(snapshotJson));
        var hashMatch = valuePresent &&
            string.Equals(actualValue, storedValue, StringComparison.OrdinalIgnoreCase);
        var failingClause = !algorithmOk
            ? "FingerprintAlgorithmMismatch"
            : !scopeOk
                ? "FingerprintScopeMismatch"
                : !valuePresent
                    ? "FingerprintValueMissing"
                    : !hashMatch
                        ? "FingerprintHashMismatch"
                        : "None";
        return new MetadataFingerprintDiagnosis(
            fingerprintPresent: true,
            algorithmOk,
            scopeOk,
            valuePresent,
            hashMatch,
            algorithm,
            scope,
            storedValue,
            actualValue,
            snapshotJson.Length,
            failingClause);
    }

    public static JObject ParseMetadataBytes(byte[] bytes)
    {
        if (bytes is null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        return ParseMetadataJson(Encoding.UTF8.GetString(bytes));
    }

    public static JObject ParseMetadataJson(string json)
    {
        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        if (json.Length > 0 && json[0] == '\uFEFF')
        {
            json = json.Substring(1);
        }

        using (var reader = new JsonTextReader(new StringReader(json)))
        {
            reader.DateParseHandling = DateParseHandling.None;
            return JObject.Load(reader);
        }
    }

    public static string ComputeJsonSha256(JToken token)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        return ComputeSha256(Encoding.UTF8.GetBytes(token.ToString(Formatting.None)));
    }

    public static string ComputeNormalizedTextSha256(string? value)
    {
        return ComputeSha256(Encoding.UTF8.GetBytes((value ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n").Trim()));
    }

    private static bool HasString(JToken? token, string expectedValue)
    {
        return token is not null && token.Type == JTokenType.String && string.Equals(token.Value<string>(), expectedValue, StringComparison.Ordinal);
    }

    private static string ReadDescription(string source, string serviceName)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(serviceName))
        {
            return string.Empty;
        }

        var pattern = @"\[Description\(\s*""(?<description>(?:\\.|[^""\\])*)""\s*\)\]\s*(?:\[(?!Description\s*\()[^\]]+\]\s*)*" + Regex.Escape(serviceName) + @"\s*\(";
        var match = Regex.Match(source, pattern, RegexOptions.CultureInvariant);
        return match.Success ? UnescapeDescription(match.Groups["description"].Value) : string.Empty;
    }

    private static string UnescapeDescription(string value)
    {
        var builder = new StringBuilder(value.Length);
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] == '\\' && index + 1 < value.Length && (value[index + 1] == '\\' || value[index + 1] == '"'))
            {
                builder.Append(value[index + 1]);
                index++;
                continue;
            }

            builder.Append(value[index]);
        }

        return builder.ToString();
    }

    private static string ComputeSha256(byte[] bytes)
    {
        using (var algorithm = SHA256.Create())
        {
            return BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", string.Empty);
        }
    }
}

public sealed class GeneratedBaselineDiagnosis
{
    public GeneratedBaselineDiagnosis(
        bool integrityPresent,
        bool versionOk,
        bool guidOk,
        bool descriptionOk,
        bool serviceSourceHashOk,
        bool serviceDescriptionsHashOk,
        string storedGuid,
        string actualGuid,
        string storedDescription,
        string actualDescription,
        string storedSourceHash,
        string actualSourceHash,
        string storedDescriptionsHash,
        string actualDescriptionsHash,
        string failingClause)
    {
        IntegrityPresent = integrityPresent;
        VersionOk = versionOk;
        GuidOk = guidOk;
        DescriptionOk = descriptionOk;
        ServiceSourceHashOk = serviceSourceHashOk;
        ServiceDescriptionsHashOk = serviceDescriptionsHashOk;
        StoredGuid = storedGuid ?? string.Empty;
        ActualGuid = actualGuid ?? string.Empty;
        StoredDescription = storedDescription ?? string.Empty;
        ActualDescription = actualDescription ?? string.Empty;
        StoredSourceHash = storedSourceHash ?? string.Empty;
        ActualSourceHash = actualSourceHash ?? string.Empty;
        StoredDescriptionsHash = storedDescriptionsHash ?? string.Empty;
        ActualDescriptionsHash = actualDescriptionsHash ?? string.Empty;
        FailingClause = failingClause ?? "None";
    }

    public bool IntegrityPresent { get; }

    public bool VersionOk { get; }

    public bool GuidOk { get; }

    public bool DescriptionOk { get; }

    public bool ServiceSourceHashOk { get; }

    public bool ServiceDescriptionsHashOk { get; }

    public string StoredGuid { get; }

    public string ActualGuid { get; }

    public string StoredDescription { get; }

    public string ActualDescription { get; }

    public string StoredSourceHash { get; }

    public string ActualSourceHash { get; }

    public string StoredDescriptionsHash { get; }

    public string ActualDescriptionsHash { get; }

    public string FailingClause { get; }

    public bool IsCompatible =>
        !IntegrityPresent
        || (VersionOk && GuidOk && DescriptionOk && ServiceSourceHashOk && ServiceDescriptionsHashOk);

    public static GeneratedBaselineDiagnosis IntegrityAbsentAccepted(
        string actualGuid,
        string actualDescription,
        string actualSourceHash,
        string actualDescriptionsHash)
    {
        return new GeneratedBaselineDiagnosis(
            integrityPresent: false,
            versionOk: true,
            guidOk: true,
            descriptionOk: true,
            serviceSourceHashOk: true,
            serviceDescriptionsHashOk: true,
            storedGuid: string.Empty,
            actualGuid: actualGuid,
            storedDescription: string.Empty,
            actualDescription: actualDescription,
            storedSourceHash: string.Empty,
            actualSourceHash: actualSourceHash,
            storedDescriptionsHash: string.Empty,
            actualDescriptionsHash: actualDescriptionsHash,
            failingClause: "None");
    }
}

public sealed class MetadataFingerprintDiagnosis
{
    public MetadataFingerprintDiagnosis(
        bool fingerprintPresent,
        bool algorithmOk,
        bool scopeOk,
        bool valuePresent,
        bool hashMatch,
        string algorithm,
        string scope,
        string storedValue,
        string actualValue,
        int snapshotLength,
        string failingClause)
    {
        FingerprintPresent = fingerprintPresent;
        AlgorithmOk = algorithmOk;
        ScopeOk = scopeOk;
        ValuePresent = valuePresent;
        HashMatch = hashMatch;
        Algorithm = algorithm ?? string.Empty;
        Scope = scope ?? string.Empty;
        StoredValue = storedValue ?? string.Empty;
        ActualValue = actualValue ?? string.Empty;
        SnapshotLength = snapshotLength;
        FailingClause = failingClause ?? "None";
    }

    public bool FingerprintPresent { get; }

    public bool AlgorithmOk { get; }

    public bool ScopeOk { get; }

    public bool ValuePresent { get; }

    public bool HashMatch { get; }

    public string Algorithm { get; }

    public string Scope { get; }

    public string StoredValue { get; }

    public string ActualValue { get; }

    public int SnapshotLength { get; }

    public string FailingClause { get; }

    public bool IsCompatible =>
        !FingerprintPresent
        || (AlgorithmOk && ScopeOk && ValuePresent && HashMatch);

    public static MetadataFingerprintDiagnosis AbsentAccepted()
    {
        return new MetadataFingerprintDiagnosis(
            fingerprintPresent: false,
            algorithmOk: true,
            scopeOk: true,
            valuePresent: true,
            hashMatch: true,
            algorithm: string.Empty,
            scope: string.Empty,
            storedValue: string.Empty,
            actualValue: string.Empty,
            snapshotLength: 0,
            failingClause: "None");
    }
}
