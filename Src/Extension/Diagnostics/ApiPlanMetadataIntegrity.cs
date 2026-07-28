#nullable enable

using System;
using System.Collections.Generic;
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
        if (metadata is null)
        {
            throw new ArgumentNullException(nameof(metadata));
        }

        var integrity = metadata["integrity"] as JObject;
        if (integrity is null)
        {
            return true;
        }

        return HasString(integrity["version"], Version) &&
            HasString(integrity.SelectToken("generatedDescriptions.hash"), generatedDescriptionsHash) &&
            HasString(integrity.SelectToken("plannedContract.hash"), plannedContractHash) &&
            string.Equals(actualDescriptionsHash, generatedDescriptionsHash, StringComparison.Ordinal) &&
            HasString(integrity.SelectToken("apiObject.descriptionSentinel"), descriptionSentinel) &&
            HasString(integrity.SelectToken("apiObject.serviceSourceExpectedHash"), ComputeNormalizedTextSha256(serviceSourceExpected)) &&
            serviceSourceContractMatches;
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

        var pattern = @"\[Description\(\s*""(?<description>(?:\\.|[^""\\])*)""\s*\)\]\s*" + Regex.Escape(serviceName) + @"\s*\(";
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
