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

    public enum IntentionalWriteOwnership
    {
        Blocked = 0,
        DescriptionFallback = 1,
        MetadataOwnership = 2,
    }

    public enum DiagnosticReason
    {
        OwnedByMetadata = 0,
        OwnedByDescriptionFallback = 1,
        MetadataAmbiguous = 2,
        MetadataDescriptionMismatch = 3,
        MetadataUnreadable = 4,
        MetadataGuidMismatch = 5,
        MetadataOwnershipMismatch = 6,
        MetadataIntegrityMismatch = 7,
        ServiceContractMismatch = 8,
        DescriptionFallbackMismatch = 9,
    }

    public sealed class Diagnostic
    {
        internal Diagnostic(
            OwnershipKind ownershipKind,
            DiagnosticReason reason,
            bool metadataFilePresent,
            bool metadataFileDescriptionOwned,
            bool metadataParsed,
            bool metadataOwnershipMatches,
            bool integrityCompatible,
            bool serviceSourceManaged,
            bool descriptionFallbackMatches,
            string actualApiGuid,
            string? metadataApiGuid)
        {
            OwnershipKind = ownershipKind;
            Reason = reason;
            MetadataFilePresent = metadataFilePresent;
            MetadataFileDescriptionOwned = metadataFileDescriptionOwned;
            MetadataParsed = metadataParsed;
            MetadataOwnershipMatches = metadataOwnershipMatches;
            IntegrityCompatible = integrityCompatible;
            ServiceSourceManaged = serviceSourceManaged;
            DescriptionFallbackMatches = descriptionFallbackMatches;
            ActualApiGuid = actualApiGuid ?? string.Empty;
            MetadataApiGuid = metadataApiGuid;
        }

        public OwnershipKind OwnershipKind { get; }

        public DiagnosticReason Reason { get; }

        public bool IsOwned => ApiPlanApiObjectOwnership.IsOwned(OwnershipKind);

        public bool MetadataFilePresent { get; }

        public bool MetadataFileDescriptionOwned { get; }

        public bool MetadataParsed { get; }

        public bool MetadataOwnershipMatches { get; }

        public bool IntegrityCompatible { get; }

        public bool ServiceSourceManaged { get; }

        public bool DescriptionFallbackMatches { get; }

        public string ActualApiGuid { get; }

        public string? MetadataApiGuid { get; }

        public string ReasonText => Reason switch
        {
            DiagnosticReason.OwnedByMetadata => "Posse confirmada pela metadata.",
            DiagnosticReason.OwnedByDescriptionFallback => "Posse confirmada pela Description fallback.",
            DiagnosticReason.MetadataAmbiguous => "Há mais de um arquivo de metadata com o mesmo nome.",
            DiagnosticReason.MetadataDescriptionMismatch => "A Description do arquivo de metadata não é reconhecida como própria.",
            DiagnosticReason.MetadataUnreadable => "O arquivo de metadata próprio não pôde ser lido como JSON.",
            DiagnosticReason.MetadataGuidMismatch => "O GUID atual do API Object diverge do GUID registrado na metadata.",
            DiagnosticReason.MetadataOwnershipMismatch => "O ownership registrado na metadata não corresponde ao API Object atual.",
            DiagnosticReason.MetadataIntegrityMismatch => "A integridade B067 da metadata não corresponde ao estado atual.",
            DiagnosticReason.ServiceContractMismatch => "Service Source, variáveis ou Events não correspondem ao contrato gerenciado.",
            DiagnosticReason.DescriptionFallbackMismatch => "A Description do API Object não corresponde a nenhum fallback próprio.",
            _ => "A posse do API Object não pôde ser confirmada.",
        };

        public string FormatDetails()
        {
            var metadataStatus = !MetadataFilePresent
                ? "não encontrada"
                : MetadataFileDescriptionOwned
                    ? "encontrada e reconhecida"
                    : "encontrada, mas Description não reconhecida";
            var metadataParseStatus = !MetadataFilePresent || !MetadataFileDescriptionOwned
                ? "não aplicável"
                : MetadataParsed ? "sim" : "não";
            var metadataOwnershipStatus = !MetadataFilePresent || !MetadataFileDescriptionOwned || !MetadataParsed
                ? "não aplicável"
                : MetadataOwnershipMatches ? "compatível" : "incompatível";

            return string.Join(
                Environment.NewLine,
                $"Causa principal: {ReasonText}",
                $"API Object GUID atual: '{ActualApiGuid}'",
                $"GUID da metadata: '{MetadataApiGuid ?? "não encontrado"}'",
                $"Arquivo de metadata: {metadataStatus}",
                $"Metadata lida como JSON: {metadataParseStatus}",
                $"Ownership da metadata: {metadataOwnershipStatus}",
                $"Integridade B067: {(IntegrityCompatible ? "compatível" : "incompatível")}",
                $"Service Source, variáveis e Events: {(ServiceSourceManaged ? "gerenciados" : "divergentes")}",
                $"Description fallback: {(DescriptionFallbackMatches ? "compatível" : "incompatível")}");
        }
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

    public static Diagnostic Diagnose(
        bool metadataFilePresent,
        bool metadataFileDescriptionOwned,
        bool metadataAmbiguous,
        bool metadataParsed,
        bool metadataOwnershipMatches,
        bool integrityCompatible,
        bool serviceSourceManaged,
        bool descriptionFallbackMatches,
        string actualApiGuid,
        string? metadataApiGuid)
    {
        if (actualApiGuid is null)
        {
            throw new ArgumentNullException(nameof(actualApiGuid));
        }

        if (metadataAmbiguous)
        {
            return new Diagnostic(
                OwnershipKind.NotOwned,
                DiagnosticReason.MetadataAmbiguous,
                metadataFilePresent,
                metadataFileDescriptionOwned,
                metadataParsed,
                metadataOwnershipMatches,
                integrityCompatible,
                serviceSourceManaged,
                descriptionFallbackMatches,
                actualApiGuid,
                metadataApiGuid);
        }

        if (metadataFilePresent && metadataFileDescriptionOwned)
        {
            if (!metadataParsed)
            {
                return NotOwnedDiagnostic(
                    DiagnosticReason.MetadataUnreadable,
                    metadataFilePresent,
                    metadataFileDescriptionOwned,
                    metadataParsed,
                    metadataOwnershipMatches,
                    integrityCompatible,
                    serviceSourceManaged,
                    descriptionFallbackMatches,
                    actualApiGuid,
                    metadataApiGuid);
            }

            if (!metadataOwnershipMatches)
            {
                var reason = !string.IsNullOrWhiteSpace(metadataApiGuid) &&
                    !string.Equals(metadataApiGuid, actualApiGuid, StringComparison.Ordinal)
                    ? DiagnosticReason.MetadataGuidMismatch
                    : DiagnosticReason.MetadataOwnershipMismatch;
                return NotOwnedDiagnostic(
                    reason,
                    metadataFilePresent,
                    metadataFileDescriptionOwned,
                    metadataParsed,
                    metadataOwnershipMatches,
                    integrityCompatible,
                    serviceSourceManaged,
                    descriptionFallbackMatches,
                    actualApiGuid,
                    metadataApiGuid);
            }

            if (!serviceSourceManaged)
            {
                return NotOwnedDiagnostic(
                    DiagnosticReason.ServiceContractMismatch,
                    metadataFilePresent,
                    metadataFileDescriptionOwned,
                    metadataParsed,
                    metadataOwnershipMatches,
                    integrityCompatible,
                    serviceSourceManaged,
                    descriptionFallbackMatches,
                    actualApiGuid,
                    metadataApiGuid);
            }

            if (!integrityCompatible)
            {
                return NotOwnedDiagnostic(
                    DiagnosticReason.MetadataIntegrityMismatch,
                    metadataFilePresent,
                    metadataFileDescriptionOwned,
                    metadataParsed,
                    metadataOwnershipMatches,
                    integrityCompatible,
                    serviceSourceManaged,
                    descriptionFallbackMatches,
                    actualApiGuid,
                    metadataApiGuid);
            }

            return new Diagnostic(
                OwnershipKind.OwnedByMetadata,
                DiagnosticReason.OwnedByMetadata,
                metadataFilePresent,
                metadataFileDescriptionOwned,
                metadataParsed,
                metadataOwnershipMatches,
                integrityCompatible,
                serviceSourceManaged,
                descriptionFallbackMatches,
                actualApiGuid,
                metadataApiGuid);
        }

        if (serviceSourceManaged && descriptionFallbackMatches)
        {
            return new Diagnostic(
                OwnershipKind.OwnedByDescriptionFallback,
                DiagnosticReason.OwnedByDescriptionFallback,
                metadataFilePresent,
                metadataFileDescriptionOwned,
                metadataParsed,
                metadataOwnershipMatches,
                integrityCompatible,
                serviceSourceManaged,
                descriptionFallbackMatches,
                actualApiGuid,
                metadataApiGuid);
        }

        return NotOwnedDiagnostic(
            !serviceSourceManaged
                ? DiagnosticReason.ServiceContractMismatch
                : DiagnosticReason.DescriptionFallbackMismatch,
            metadataFilePresent,
            metadataFileDescriptionOwned,
            metadataParsed,
            metadataOwnershipMatches,
            integrityCompatible,
            serviceSourceManaged,
            descriptionFallbackMatches,
            actualApiGuid,
            metadataApiGuid);
    }

    private static Diagnostic NotOwnedDiagnostic(
        DiagnosticReason reason,
        bool metadataFilePresent,
        bool metadataFileDescriptionOwned,
        bool metadataParsed,
        bool metadataOwnershipMatches,
        bool integrityCompatible,
        bool serviceSourceManaged,
        bool descriptionFallbackMatches,
        string actualApiGuid,
        string? metadataApiGuid) =>
        new(
            OwnershipKind.NotOwned,
            reason,
            metadataFilePresent,
            metadataFileDescriptionOwned,
            metadataParsed,
            metadataOwnershipMatches,
            integrityCompatible,
            serviceSourceManaged,
            descriptionFallbackMatches,
            actualApiGuid,
            metadataApiGuid);

    /// <summary>
    /// B054 → B070 → B060: na primeira geração a metadata só é gravada depois do List,
    /// então a escrita intencional precisa aceitar a posse pela Description enquanto o File
    /// não existir. Com File presente, a posse volta a ser exclusivamente da metadata.
    /// </summary>
    public static IntentionalWriteOwnership ResolveIntentionalWriteOwnership(
        bool metadataAmbiguous,
        bool metadataFilePresent)
    {
        if (metadataAmbiguous)
        {
            return IntentionalWriteOwnership.Blocked;
        }

        return metadataFilePresent
            ? IntentionalWriteOwnership.MetadataOwnership
            : IntentionalWriteOwnership.DescriptionFallback;
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
