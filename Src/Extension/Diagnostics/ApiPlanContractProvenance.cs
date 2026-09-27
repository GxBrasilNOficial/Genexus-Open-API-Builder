#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

public enum ApiPlanContractOrigin { Metadata, Fallback, Default }

/// <summary>B110: presença de chave governa a proveniência; notRecovered é apenas informativo.</summary>
public sealed class ApiPlanContractProvenance
{
    private ApiPlanContractProvenance(bool imported, bool hasLevelsKey, IDictionary<string, ApiPlanContractOrigin> sections)
    {
        Imported = imported;
        HasLevelsKey = hasLevelsKey;
        Sections = new System.Collections.ObjectModel.ReadOnlyDictionary<string, ApiPlanContractOrigin>(sections);
    }
    public bool Imported { get; }
    public bool HasLevelsKey { get; }
    public IReadOnlyDictionary<string, ApiPlanContractOrigin> Sections { get; }
    public string[] ReconstructedSections => Sections.Where(pair => pair.Value != ApiPlanContractOrigin.Metadata).Select(pair => pair.Key).OrderBy(key => key, StringComparer.Ordinal).ToArray();

    public static ApiPlanContractProvenance Read(JObject? metadata, bool sourceServices,
        bool sourceDescriptions, bool sourceRestPath, bool sourceSecurity, bool sourceFilters,
        bool sdtCreate, bool sdtUpdate, bool sdtResponse)
    {
        var sections = new Dictionary<string, ApiPlanContractOrigin>(StringComparer.Ordinal);
        void Add(string key, bool fallback = false) => sections.Add(key,
            HasKey(metadata, key) ? ApiPlanContractOrigin.Metadata
                : fallback ? ApiPlanContractOrigin.Fallback : ApiPlanContractOrigin.Default);
        Add("services", sourceServices);
        Add("descriptions.services", sourceDescriptions);
        Add("security.level", sourceSecurity);
        Add("api.restPath", sourceRestPath);
        Add("fields.createRequest", sdtCreate);
        Add("fields.updateRequest", sdtUpdate);
        Add("fields.response", sdtResponse);
        Add("fields.listFilters", sourceFilters);
        Add("fields.required");
        Add("pagination.defaultPageSize");
        Add("pagination.maximumPageSize");
        Add("order");
        Add("api.servicesBasePath");
        Add("levels");
        Add("errorDetail.includeBusinessComponentMessages");
        return new ApiPlanContractProvenance(
            metadata?.SelectToken("recovery.imported")?.Type == JTokenType.Boolean
                && metadata.SelectToken("recovery.imported")!.Value<bool>(),
            HasKey(metadata, "levels"), sections);
    }

    public static bool HasKey(JObject? document, string path)
    {
        JToken? current = document;
        foreach (var key in path.Split('.'))
        {
            if (current is not JObject parent || parent.Property(key) is not JProperty property)
                return false;
            current = property.Value;
        }
        return true; // Inclui null explícito, distinto de chave ausente.
    }

    // Usado pelo reader real: List() B054/B055 não contém inputs; B070 os acrescenta.
    public static string[] ReadInputNames(string parameters) => Regex.Matches(parameters,
        @"(?<direction>in|out)\s*:\s*&(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
        .Cast<Match>().Where(match => string.Equals(match.Groups["direction"].Value, "in", StringComparison.OrdinalIgnoreCase))
        .Select(match => match.Groups["name"].Value).ToArray();

    public static ApiPlanSourceFilter[] ReadFilters(string parameters, IEnumerable<string> attributeNames)
    {
        var inputs = new HashSet<string>(ReadInputNames(parameters), StringComparer.OrdinalIgnoreCase);
        return attributeNames.Select(name => new ApiPlanSourceFilter(name,
                inputs.Contains(name + "From") && inputs.Contains(name + "To"),
                inputs.Contains(name + "Min") && inputs.Contains(name + "Max")))
            .Where(filter => inputs.Contains(filter.Name) || filter.UsesPeriod || filter.UsesRange).ToArray();
    }
}

public sealed class ApiPlanSourceFilter
{
    public ApiPlanSourceFilter(string name, bool usesPeriod, bool usesRange)
    { Name = name; UsesPeriod = usesPeriod; UsesRange = usesRange; }
    public string Name { get; }
    public bool UsesPeriod { get; }
    public bool UsesRange { get; }
}
