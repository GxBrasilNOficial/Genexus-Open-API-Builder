#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

public enum ApiPlanMainObjectResolutionKind
{
    ConfirmedByGuid = 0,
    MissingPlannedGuid = 1,
    GuidNotFound = 2,
    GuidNameMismatch = 3,
    NameAmbiguousWithoutGuid = 4,
}

public sealed class ApiPlanMainObjectCandidate
{
    public ApiPlanMainObjectCandidate(Guid guid, string name)
    {
        if (guid == Guid.Empty)
        {
            throw new ArgumentException("O GUID do candidato e obrigatorio.", nameof(guid));
        }

        Guid = guid;
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public Guid Guid { get; }

    public string Name { get; }
}

public sealed class ApiPlanMainObjectResolution
{
    internal ApiPlanMainObjectResolution(
        ApiPlanMainObjectResolutionKind kind,
        ApiPlanMainObjectCandidate? candidate,
        string diagnostic)
    {
        Kind = kind;
        Candidate = candidate;
        Diagnostic = diagnostic ?? throw new ArgumentNullException(nameof(diagnostic));
    }

    public ApiPlanMainObjectResolutionKind Kind { get; }

    public ApiPlanMainObjectCandidate? Candidate { get; }

    public string Diagnostic { get; }

    public bool IsConfirmed => Kind == ApiPlanMainObjectResolutionKind.ConfirmedByGuid;
}

public static class ApiPlanMainObjectResolver
{
    public static ApiPlanMainObjectResolution Resolve(
        Guid? plannedGuid,
        string expectedName,
        ApiPlanMainObjectCandidate? candidateByGuid,
        IReadOnlyCollection<ApiPlanMainObjectCandidate>? nameMatches)
    {
        if (string.IsNullOrWhiteSpace(expectedName))
        {
            throw new ArgumentException("O nome esperado do API Object e obrigatorio.", nameof(expectedName));
        }

        var matches = nameMatches ?? Array.Empty<ApiPlanMainObjectCandidate>();
        if (!plannedGuid.HasValue || plannedGuid.Value == Guid.Empty)
        {
            if (matches.Count > 1)
            {
                return new ApiPlanMainObjectResolution(
                    ApiPlanMainObjectResolutionKind.NameAmbiguousWithoutGuid,
                    null,
                    $"nenhum GUID planejado foi produzido e existem {matches.Count} candidatos nominais para '{expectedName}'; nenhum foi associado.");
            }

            return new ApiPlanMainObjectResolution(
                ApiPlanMainObjectResolutionKind.MissingPlannedGuid,
                null,
                $"nenhum GUID planejado foi produzido para o API Object '{expectedName}'; associacao por nome nao foi aplicada.");
        }

        if (candidateByGuid is null)
        {
            if (matches.Count > 1)
            {
                return new ApiPlanMainObjectResolution(
                    ApiPlanMainObjectResolutionKind.NameAmbiguousWithoutGuid,
                    null,
                    $"o GUID planejado '{plannedGuid.Value}' nao foi confirmado e existem {matches.Count} candidatos nominais para '{expectedName}'; nenhum foi associado.");
            }

            var nominalDetail = matches.Count == 1
                ? $"um candidato nominal com GUID '{matches.First().Guid}' foi encontrado, mas nao foi associado"
                : "nenhum candidato nominal foi encontrado";
            return new ApiPlanMainObjectResolution(
                ApiPlanMainObjectResolutionKind.GuidNotFound,
                null,
                $"o GUID planejado '{plannedGuid.Value}' nao foi confirmado; {nominalDetail} para '{expectedName}'.");
        }

        if (candidateByGuid.Guid != plannedGuid.Value
            || !string.Equals(candidateByGuid.Name, expectedName, StringComparison.OrdinalIgnoreCase))
        {
            return new ApiPlanMainObjectResolution(
                ApiPlanMainObjectResolutionKind.GuidNameMismatch,
                null,
                $"o objeto lido pelo GUID planejado '{plannedGuid.Value}' nao corresponde ao contrato esperado " +
                $"(Guid='{candidateByGuid.Guid}', Name='{candidateByGuid.Name}', esperado Name='{expectedName}'); nenhum foi associado.");
        }

        return new ApiPlanMainObjectResolution(
            ApiPlanMainObjectResolutionKind.ConfirmedByGuid,
            candidateByGuid,
            $"API Object '{candidateByGuid.Name}' confirmado pelo GUID planejado '{candidateByGuid.Guid}'.");
    }
}
