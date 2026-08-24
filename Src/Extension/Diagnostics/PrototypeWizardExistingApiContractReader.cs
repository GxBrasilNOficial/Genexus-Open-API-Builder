#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;
using Artech.Genexus.Common.Parts.SDT;
using Artech.Genexus.Common.Wiki;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// B034/B070: reconstrói, em modo somente leitura, as decisões já persistidas
/// pelo API Object e pela metadata própria antes de abrir o Wizard.
/// O usuário pode alterar qualquer decisão; a leitura apenas evita que uma
/// reexecução comece com defaults e apague escolhas existentes.
/// </summary>
internal static class PrototypeWizardExistingApiContractReader
{
    // O lookbehind exclui o sufixo das chamadas geradas (procX_API_List(...), Modulo.List(...)),
    // que antes era lido como uma segunda declaração do mesmo serviço.
    private static readonly Regex ServiceBlockPattern = new(
        @"(?<annotations>(?:\s*\[[^\r\n]*\]\s*)*)(?<![\w.])(?<service>List|Get|Create|Update)\s*\((?<parameters>[^)]*)\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex InputParameterPattern = new(
        @"(?<direction>in|out)\s*:\s*&(?<name>[A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex AnnotationPattern = new(
        @"\[(?<name>Description|RestPath|SecurityLevel|RestMethod)\s*\(\s*(?:""(?<value>[^""]*)""|(?<bare>[^)]*))\s*\)\]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex GeneratedProcedureCallPattern = new(
        @"\bproc(?<transaction>[A-Za-z_][A-Za-z0-9_]*)_API_(?:List|Get|Create|Update)\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static PrototypeWizardExistingApiContract Read(KBModel designModel, Transaction transaction)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        var attributeNames = new HashSet<string>(
            transaction.Structure.Root.Attributes.Select(item => item.Name),
            StringComparer.OrdinalIgnoreCase);
        var metadata = ReadMetadata(designModel, transaction);
        var api = ResolveApiObject(designModel, transaction, metadata);
        var source = api is null
            ? ExistingApiSource.Empty
            : ReadApiSource(api.ServiceGroupSource.Source ?? string.Empty, api.Name, attributeNames, metadata.Filters);

        var serviceDefinitions = metadata.Services.IsAvailable
            ? metadata.Services.Values.Values
            : source.Services;
        var servicesAvailable = metadata.Services.IsAvailable || source.ServicesAvailable;

        var createFields = metadata.CreateFields.IsAvailable
            ? metadata.CreateFields
            : ReadOwnedSdtFields(designModel, $"sdt{transaction.Name}_API_CreateRequest");
        var updateFields = metadata.UpdateFields.IsAvailable
            ? metadata.UpdateFields
            : ReadOwnedSdtFields(designModel, $"sdt{transaction.Name}_API_UpdateRequest");
        var responseFields = metadata.ResponseFields.IsAvailable
            ? metadata.ResponseFields
            : ReadOwnedSdtFields(designModel, $"sdt{transaction.Name}_API_Response");

        var filters = source.FiltersAvailable
            ? source.Filters
            : metadata.Filters.Values;
        var filtersAvailable = source.FiltersAvailable || metadata.FiltersAvailable;

        var apiName = ReadString(metadata.Document, "api.name") ?? api?.Name;
        var servicesBasePath = ReadString(metadata.Document, "api.servicesBasePath");
        var restPath = ReadString(metadata.Document, "api.restPath") ?? source.ListRestPath;
        var securityLevel = ReadString(metadata.Document, "security.level") ?? source.SecurityLevel;
        var defaultPageSize = ReadInt(metadata.Document, "pagination.defaultPageSize");
        var maximumPageSize = ReadInt(metadata.Document, "pagination.maximumPageSize");
        var includeBusinessComponentErrorMessages = ReadOptionalBool(metadata.Document, "errorDetail.includeBusinessComponentMessages") ?? true;
        var staticOrder = ReadStaticOrder(metadata.Document);
        var requiredFields = ReadRequiredFields(metadata.Document);
        IReadOnlyDictionary<string, string> serviceDescriptions = metadata.ServiceDescriptions.IsAvailable
            ? metadata.ServiceDescriptions.Values
            : source.Services
                .Where(item => !string.IsNullOrWhiteSpace(item.Description))
                .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().Description!, StringComparer.OrdinalIgnoreCase);

        var hasExistingApi = api is not null || metadata.IsAvailable;
        return new PrototypeWizardExistingApiContract(
            hasExistingApi,
            serviceDefinitions,
            servicesAvailable,
            createFields,
            updateFields,
            responseFields,
            filters,
            filtersAvailable,
            requiredFields,
            apiName,
            servicesBasePath,
            restPath,
            securityLevel,
            defaultPageSize,
            maximumPageSize,
            staticOrder,
            serviceDescriptions,
            source.DuplicateServiceNames,
            includeBusinessComponentErrorMessages);
    }

    private static API? ResolveApiObject(
        KBModel designModel,
        Transaction transaction,
        ExistingApiMetadata metadata)
    {
        var allApis = API.GetAll(designModel).ToArray();
        var metadataApiName = ReadString(metadata.Document, "api.name")
            ?? ReadString(metadata.Document, "ownership.apiName");
        if (!string.IsNullOrWhiteSpace(metadataApiName))
        {
            var metadataMatches = allApis
                .Where(item => string.Equals(item.Name, metadataApiName, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            return metadataMatches.Length == 1 ? metadataMatches[0] : null;
        }

        var conventionalName = "api" + transaction.Name;
        var conventionalMatches = allApis
            .Where(item => string.Equals(item.Name, conventionalName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (conventionalMatches.Length > 1)
        {
            return null;
        }

        // Sem metadata (por exemplo, geração interrompida antes de B060), um nome
        // customizado só é aceito quando há duas provas: Description própria da
        // extensão e chamada a Procedure gerada para esta Transaction. Ambiguidade
        // mantém o reencontro bloqueado, sem escolher um API Object arbitrariamente.
        var ownedMatches = allApis
            .Where(item => IsOwnedApiCandidateForTransaction(item, transaction.Name))
            .ToArray();
        var candidates = conventionalMatches
            .Concat(ownedMatches)
            .GroupBy(item => item.Guid)
            .Select(group => group.First())
            .ToArray();
        return candidates.Length == 1 ? candidates[0] : null;
    }

    private static bool IsOwnedApiCandidateForTransaction(API api, string transactionName)
    {
        var descriptionOwned =
            ApiPlanOwnedObjectDescription.IsCanonical(api.Description, api.Name)
            || string.Equals(
                api.Description,
                ApiPlanOwnedObjectDescription.CreateLegacyApiObjectDescription(transactionName),
                StringComparison.Ordinal)
            || string.Equals(
                api.Description,
                ApiPlanOwnedObjectDescription.CreateLegacyApiObjectDescription(transactionName, withTrailingPeriod: true),
                StringComparison.Ordinal);
        if (!descriptionOwned)
        {
            return false;
        }

        var source = api.ServiceGroupSource?.Source ?? string.Empty;
        return GeneratedProcedureCallPattern.Matches(source)
            .Cast<Match>()
            .Any(match => string.Equals(
                match.Groups["transaction"].Value,
                transactionName,
                StringComparison.OrdinalIgnoreCase));
    }

    private static ExistingApiSource ReadApiSource(
        string source,
        string apiName,
        ISet<string> attributeNames,
        IReadOnlyDictionary<string, PrototypeWizardExistingFilter> metadataFilters)
    {
        var services = new List<PrototypeWizardExistingService>();
        var filters = new List<PrototypeWizardExistingFilter>();
        var declaredServiceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicateServiceNames = new List<string>();
        var filterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? listRestPath = null;
        string? securityLevel = null;

        foreach (Match match in ServiceBlockPattern.Matches(source))
        {
            var serviceName = match.Groups["service"].Value;
            if (!declaredServiceNames.Add(serviceName))
            {
                if (!duplicateServiceNames.Contains(serviceName, StringComparer.OrdinalIgnoreCase))
                {
                    duplicateServiceNames.Add(serviceName);
                }

                continue;
            }

            var annotations = ReadAnnotations(match.Groups["annotations"].Value);
            var restPath = annotations.TryGetValue("RestPath", out var resolvedRestPath) ? resolvedRestPath : null;
            var description = annotations.TryGetValue("Description", out var resolvedDescription) ? resolvedDescription : null;
            var resolvedSecurity = annotations.TryGetValue("SecurityLevel", out var resolvedSecurityLevel) ? resolvedSecurityLevel : null;
            var httpMethod = annotations.TryGetValue("RestMethod", out var resolvedMethod)
                ? resolvedMethod.ToUpperInvariant()
                : string.Equals(serviceName, "Create", StringComparison.OrdinalIgnoreCase)
                    ? "POST"
                    : string.Equals(serviceName, "Update", StringComparison.OrdinalIgnoreCase) ? "PUT" : "GET";

            services.Add(new PrototypeWizardExistingService(
                serviceName,
                httpMethod,
                restPath,
                apiName + "." + serviceName,
                description));
            if (string.Equals(serviceName, "List", StringComparison.OrdinalIgnoreCase))
            {
                listRestPath = restPath;
                securityLevel ??= resolvedSecurity;
                foreach (var filter in ReadApiFilters(match.Groups["parameters"].Value, attributeNames, metadataFilters))
                {
                    if (filterNames.Add(filter.Name))
                    {
                        filters.Add(filter);
                    }
                }
            }

            securityLevel ??= resolvedSecurity;
        }

        return new ExistingApiSource(
            services,
            filters,
            filters.Count > 0 || declaredServiceNames.Contains("List"),
            listRestPath,
            securityLevel,
            duplicateServiceNames);
    }

    private static Dictionary<string, string> ReadAnnotations(string annotations)
    {
        return AnnotationPattern.Matches(annotations)
            .Cast<Match>()
            .GroupBy(match => match.Groups["name"].Value, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Last().Groups["value"].Success
                    ? group.Last().Groups["value"].Value
                    : group.Last().Groups["bare"].Value.Trim(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<PrototypeWizardExistingFilter> ReadApiFilters(
        string parameters,
        ISet<string> attributeNames,
        IReadOnlyDictionary<string, PrototypeWizardExistingFilter> metadataFilters)
    {
        var inputNames = new HashSet<string>(
            InputParameterPattern.Matches(parameters)
                .Cast<Match>()
                .Where(match => string.Equals(match.Groups["direction"].Value, "in", StringComparison.OrdinalIgnoreCase))
                .Select(match => match.Groups["name"].Value),
            StringComparer.OrdinalIgnoreCase);

        var filters = new List<PrototypeWizardExistingFilter>();
        foreach (var attributeName in attributeNames)
        {
            var usesPeriod = inputNames.Contains(attributeName + "From") && inputNames.Contains(attributeName + "To");
            var usesRange = inputNames.Contains(attributeName + "Min") && inputNames.Contains(attributeName + "Max");
            var selected = inputNames.Contains(attributeName) || usesPeriod || usesRange;
            if (!selected)
            {
                continue;
            }

            if (metadataFilters.TryGetValue(attributeName, out var metadataFilter))
            {
                filters.Add(metadataFilter);
                continue;
            }

            filters.Add(new PrototypeWizardExistingFilter(attributeName, null, usesPeriod, usesRange));
        }

        return filters;
    }

    private static PrototypeWizardExistingFieldSelection ReadOwnedSdtFields(KBModel designModel, string sdtName)
    {
        var matches = SDT.GetAll(designModel)
            .Where(item => string.Equals(item.Name, sdtName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (matches.Length != 1 || !ApiPlanOwnedObjectDescription.IsOwnedSdt(matches[0].Description, sdtName))
        {
            return PrototypeWizardExistingFieldSelection.Unavailable;
        }

        // StructureItemCollection não implementa IEnumerable<SDTItem>; o cast genérico falha em runtime.
        // O padrão validado em ApiPlanTransactionSyncOrchestrator é foreach (SDTItem item in Items).
        var names = new List<string>();
        foreach (SDTItem item in matches[0].SDTStructure.Root.Items)
        {
            if (!string.IsNullOrWhiteSpace(item.Name))
            {
                names.Add(item.Name);
            }
        }

        return new PrototypeWizardExistingFieldSelection(true, names);
    }

    private static ExistingApiMetadata ReadMetadata(KBModel designModel, Transaction transaction)
    {
        var metadataName = "api" + transaction.Name + "_Metadata";
        var files = WikiFileKBObject.GetAll(designModel)
            .Where(item => string.Equals(item.Name, metadataName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var file = files.Length == 1 && ApiPlanOwnedObjectDescription.IsOwnedMetadataFile(files[0].Description, metadataName, transaction.Name)
            ? files[0]
            : null;
        if (file is null)
        {
            return ExistingApiMetadata.Empty;
        }

        try
        {
            var bytes = file.BlobPart?.Data?.GetBytes();
            if (bytes is null || bytes.Length == 0)
            {
                return ExistingApiMetadata.Empty;
            }

            var document = ApiPlanMetadataIntegrity.ParseMetadataBytes(bytes);
            return new ExistingApiMetadata(
                document,
                ReadServiceNames(document),
                ReadFieldSelection(document, "fields.createRequest"),
                ReadFieldSelection(document, "fields.updateRequest"),
                ReadFieldSelection(document, "fields.response"),
                ReadMetadataFilters(document),
                ReadRequiredFields(document),
                ReadStaticOrder(document),
                ReadServiceDescriptions(document));
        }
        catch (JsonException)
        {
            return ExistingApiMetadata.Empty;
        }
        catch (InvalidDataException)
        {
            return ExistingApiMetadata.Empty;
        }
    }

    private static PrototypeWizardExistingFieldSelection ReadFieldSelection(JObject document, string path)
    {
        var fields = document.SelectToken(path) as JArray;
        if (fields is null)
        {
            return PrototypeWizardExistingFieldSelection.Unavailable;
        }

        return new PrototypeWizardExistingFieldSelection(
            true,
            fields.Select(item => item["name"]?.Value<string>())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Cast<string>()
                .ToArray());
    }

    private static PrototypeWizardExistingFieldSelection ReadMetadataFilters(JObject document)
    {
        var filters = document.SelectToken("fields.listFilters") as JArray;
        if (filters is null)
        {
            return PrototypeWizardExistingFieldSelection.Unavailable;
        }

        var values = filters
            .Select(item =>
            {
                var name = item.SelectToken("field.name")?.Value<string>();
                return string.IsNullOrWhiteSpace(name)
                    ? null
                    : new PrototypeWizardExistingFilter(
                        name!,
                        item["operator"]?.Value<string>(),
                        item["usesPeriod"]?.Value<bool>() ?? false,
                        item["usesRange"]?.Value<bool>() ?? false);
            })
            .Where(item => item is not null)
            .Cast<PrototypeWizardExistingFilter>()
            .ToArray();
        return new PrototypeWizardExistingFieldSelection(true, values.Select(item => item.Name));
    }

    private static IReadOnlyDictionary<string, PrototypeWizardExistingFilter> ReadMetadataFilterDictionary(JObject document)
    {
        var filters = document.SelectToken("fields.listFilters") as JArray;
        if (filters is null)
        {
            return new Dictionary<string, PrototypeWizardExistingFilter>(StringComparer.OrdinalIgnoreCase);
        }

        return filters
            .Select(item =>
            {
                var name = item.SelectToken("field.name")?.Value<string>();
                return string.IsNullOrWhiteSpace(name)
                    ? null
                    : new PrototypeWizardExistingFilter(
                        name!,
                        item["operator"]?.Value<string>(),
                        item["usesPeriod"]?.Value<bool>() ?? false,
                        item["usesRange"]?.Value<bool>() ?? false);
            })
            .Where(item => item is not null)
            .Cast<PrototypeWizardExistingFilter>()
            .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, bool> ReadRequiredFields(JObject? document)
    {
        var fields = document?.SelectToken("fields.required") as JArray;
        if (fields is null)
        {
            return new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        }

        return fields
            .Where(item => string.Equals(item["requestName"]?.Value<string>(), "CreateRequest", StringComparison.OrdinalIgnoreCase))
            .Select(item => new
            {
                Name = item["fieldName"]?.Value<string>(),
                IsRequired = item["isRequired"]?.Value<bool>() ?? false,
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .GroupBy(item => item.Name!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().IsRequired, StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<PrototypeWizardExistingStaticOrder> ReadStaticOrder(JObject? document)
    {
        var order = document?.SelectToken("order") as JArray;
        if (order is null)
        {
            return Array.Empty<PrototypeWizardExistingStaticOrder>();
        }

        return order
            .Select(item =>
            {
                var name = item["attributeName"]?.Value<string>();
                return string.IsNullOrWhiteSpace(name)
                    ? null
                    : new PrototypeWizardExistingStaticOrder(
                        item["order"]?.Value<int>() ?? 0,
                        name!,
                        item["direction"]?.Value<string>() ?? "ASC");
            })
            .Where(item => item is not null)
            .Cast<PrototypeWizardExistingStaticOrder>()
            .OrderBy(item => item.Order)
            .ToArray();
    }

    private static ExistingApiMetadataServiceDescriptions ReadServiceDescriptions(JObject document)
    {
        var services = document.SelectToken("descriptions.services") as JArray;
        if (services is null)
        {
            return ExistingApiMetadataServiceDescriptions.Unavailable;
        }

        var values = services
            .Select(item => new
            {
                Name = item["serviceName"]?.Value<string>(),
                Description = item["description"]?.Value<string>(),
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Name) && item.Description is not null)
            .GroupBy(item => item.Name!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Description!, StringComparer.OrdinalIgnoreCase);
        return new ExistingApiMetadataServiceDescriptions(true, values);
    }

    private static ExistingApiMetadataServiceSelection ReadServiceNames(JObject document)
    {
        var services = document["services"] as JArray;
        if (services is null)
        {
            return ExistingApiMetadataServiceSelection.Unavailable;
        }

        var values = services
            .Select(item => new
            {
                Name = item["name"]?.Value<string>(),
                Description = item["description"]?.Value<string>(),
                HttpMethod = item["httpMethod"]?.Value<string>(),
                RestPath = item["restPath"]?.Value<string>(),
                OperationId = item["operationId"]?.Value<string>(),
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .GroupBy(item => item.Name!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => new PrototypeWizardExistingService(
                    group.Key,
                    group.First().HttpMethod ?? "GET",
                    group.First().RestPath,
                    group.First().OperationId,
                    group.First().Description),
                StringComparer.OrdinalIgnoreCase);
        return new ExistingApiMetadataServiceSelection(true, values.Keys, values);
    }

    private static string? ReadString(JObject? document, string path)
    {
        var value = document?.SelectToken(path)?.Value<string>();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static int? ReadInt(JObject? document, string path)
    {
        var value = document?.SelectToken(path);
        return value?.Type == JTokenType.Integer ? value.Value<int>() : null;
    }

    private static bool? ReadOptionalBool(JObject? document, string path)
    {
        var value = document?.SelectToken(path);
        return value?.Type == JTokenType.Boolean ? value.Value<bool>() : null;
    }

    private sealed class ExistingApiSource
    {
        public static ExistingApiSource Empty { get; } = new(
            Array.Empty<PrototypeWizardExistingService>(),
            Array.Empty<PrototypeWizardExistingFilter>(),
            false,
            null,
            null,
            Array.Empty<string>());

        public ExistingApiSource(
            IReadOnlyList<PrototypeWizardExistingService> services,
            IReadOnlyList<PrototypeWizardExistingFilter> filters,
            bool filtersAvailable,
            string? listRestPath,
            string? securityLevel,
            IReadOnlyList<string> duplicateServiceNames)
        {
            Services = services;
            Filters = filters;
            FiltersAvailable = filtersAvailable;
            ListRestPath = listRestPath;
            SecurityLevel = securityLevel;
            DuplicateServiceNames = duplicateServiceNames;
        }

        public IReadOnlyList<string> DuplicateServiceNames { get; }

        public IReadOnlyList<PrototypeWizardExistingService> Services { get; }
        public bool ServicesAvailable => Services.Count > 0;
        public IReadOnlyList<PrototypeWizardExistingFilter> Filters { get; }
        public bool FiltersAvailable { get; }
        public string? ListRestPath { get; }
        public string? SecurityLevel { get; }
    }

    private sealed class ExistingApiMetadata
    {
        public static ExistingApiMetadata Empty { get; } = new(
            null,
            ExistingApiMetadataServiceSelection.Unavailable,
            PrototypeWizardExistingFieldSelection.Unavailable,
            PrototypeWizardExistingFieldSelection.Unavailable,
            PrototypeWizardExistingFieldSelection.Unavailable,
            PrototypeWizardExistingFieldSelection.Unavailable,
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase),
            Array.Empty<PrototypeWizardExistingStaticOrder>(),
            ExistingApiMetadataServiceDescriptions.Unavailable);

        public ExistingApiMetadata(
            JObject? document,
            ExistingApiMetadataServiceSelection services,
            PrototypeWizardExistingFieldSelection createFields,
            PrototypeWizardExistingFieldSelection updateFields,
            PrototypeWizardExistingFieldSelection responseFields,
            PrototypeWizardExistingFieldSelection filterFields,
            IReadOnlyDictionary<string, bool> requiredFields,
            IReadOnlyList<PrototypeWizardExistingStaticOrder> staticOrder,
            ExistingApiMetadataServiceDescriptions serviceDescriptions)
        {
            Document = document;
            Services = services;
            CreateFields = createFields;
            UpdateFields = updateFields;
            ResponseFields = responseFields;
            Filters = document is null
                ? new Dictionary<string, PrototypeWizardExistingFilter>(StringComparer.OrdinalIgnoreCase)
                : ReadMetadataFilterDictionary(document);
            FiltersAvailable = filterFields.IsAvailable;
            RequiredFields = requiredFields;
            StaticOrder = staticOrder;
            ServiceDescriptions = serviceDescriptions;
        }

        public JObject? Document { get; }
        public bool IsAvailable => Document is not null;
        public ExistingApiMetadataServiceSelection Services { get; }
        public PrototypeWizardExistingFieldSelection CreateFields { get; }
        public PrototypeWizardExistingFieldSelection UpdateFields { get; }
        public PrototypeWizardExistingFieldSelection ResponseFields { get; }
        public IReadOnlyDictionary<string, PrototypeWizardExistingFilter> Filters { get; }
        public bool FiltersAvailable { get; }
        public IReadOnlyDictionary<string, bool> RequiredFields { get; }
        public IReadOnlyList<PrototypeWizardExistingStaticOrder> StaticOrder { get; }
        public ExistingApiMetadataServiceDescriptions ServiceDescriptions { get; }
    }
}

internal sealed class PrototypeWizardExistingApiContract
{
    private readonly IReadOnlyDictionary<string, PrototypeWizardExistingFilter> _filters;
    private readonly IReadOnlyDictionary<string, bool> _services;
    private readonly IReadOnlyDictionary<string, bool> _createRequiredFields;
    private readonly IReadOnlyDictionary<string, PrototypeWizardExistingFieldSelection> _fieldSelections;

    public PrototypeWizardExistingApiContract(bool hasExistingApi, IEnumerable<PrototypeWizardExistingFilter> filters)
        : this(
            hasExistingApi,
            Array.Empty<PrototypeWizardExistingService>(),
            false,
            PrototypeWizardExistingFieldSelection.Unavailable,
            PrototypeWizardExistingFieldSelection.Unavailable,
            PrototypeWizardExistingFieldSelection.Unavailable,
            filters,
            false,
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase),
            null,
            null,
            null,
            null,
            null,
            null,
            Array.Empty<PrototypeWizardExistingStaticOrder>(),
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            Array.Empty<string>())
    {
    }

    public PrototypeWizardExistingApiContract(
        bool hasExistingApi,
        IEnumerable<PrototypeWizardExistingService> services,
        bool servicesAvailable,
        PrototypeWizardExistingFieldSelection createFields,
        PrototypeWizardExistingFieldSelection updateFields,
        PrototypeWizardExistingFieldSelection responseFields,
        IEnumerable<PrototypeWizardExistingFilter> filters,
        bool filtersAvailable,
        IReadOnlyDictionary<string, bool> createRequiredFields,
        string? apiName,
        string? servicesBasePath,
        string? restPath,
        string? securityLevel,
        int? defaultPageSize,
        int? maximumPageSize,
        IReadOnlyList<PrototypeWizardExistingStaticOrder> staticOrder,
        IReadOnlyDictionary<string, string> serviceDescriptions,
        IReadOnlyList<string> duplicateServiceNames,
        bool includeBusinessComponentErrorMessages = true)
    {
        HasExistingApi = hasExistingApi;
        // A primeira declaração de cada nome vence: contrato de origem malformado não pode
        // derrubar a abertura do wizard com ArgumentException de chave duplicada.
        var distinctServices = (services ?? Array.Empty<PrototypeWizardExistingService>())
            .Where(item => item is not null && !string.IsNullOrWhiteSpace(item.Name))
            .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
        _services = distinctServices.ToDictionary(item => item.Name, item => true, StringComparer.OrdinalIgnoreCase);
        Services = distinctServices;
        ServicesAvailable = servicesAvailable;
        DuplicateServiceNames = duplicateServiceNames ?? Array.Empty<string>();
        _fieldSelections = new Dictionary<string, PrototypeWizardExistingFieldSelection>(StringComparer.OrdinalIgnoreCase)
        {
            ["CreateRequest"] = createFields,
            ["UpdateRequest"] = updateFields,
            ["Response"] = responseFields,
        };
        _filters = (filters ?? Array.Empty<PrototypeWizardExistingFilter>())
            .Where(item => item is not null && !string.IsNullOrWhiteSpace(item.Name))
            .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        FiltersAvailable = filtersAvailable;
        _createRequiredFields = createRequiredFields;
        ApiName = apiName;
        ServicesBasePath = servicesBasePath;
        RestPath = restPath;
        SecurityLevel = securityLevel;
        DefaultPageSize = defaultPageSize;
        MaximumPageSize = maximumPageSize;
        StaticOrder = staticOrder;
        ServiceDescriptions = serviceDescriptions;
        IncludeBusinessComponentErrorMessages = includeBusinessComponentErrorMessages;
    }

    public bool HasExistingApi { get; }
    public IReadOnlyList<PrototypeWizardExistingService> Services { get; }
    public bool ServicesAvailable { get; }
    public bool FiltersAvailable { get; }
    public string? ApiName { get; }
    public string? ServicesBasePath { get; }
    public string? RestPath { get; }
    public string? SecurityLevel { get; }
    public int? DefaultPageSize { get; }
    public int? MaximumPageSize { get; }
    public IReadOnlyList<PrototypeWizardExistingStaticOrder> StaticOrder { get; }
    public IReadOnlyDictionary<string, string> ServiceDescriptions { get; }
    public IReadOnlyList<string> DuplicateServiceNames { get; }
    public bool IncludeBusinessComponentErrorMessages { get; }

    public bool TryGetServiceSelection(string name, out bool selected)
    {
        return _services.TryGetValue(name, out selected);
    }

    public bool TryGetService(string name, out PrototypeWizardExistingService service)
    {
        service = Services.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))!;
        return service is not null;
    }

    public bool TryGetFieldSelection(string requestName, string name, out bool selected)
    {
        selected = false;
        return _fieldSelections.TryGetValue(requestName, out var selection) && selection.TryGet(name, out selected);
    }

    public bool IsFieldSelectionAvailable(string requestName)
    {
        return _fieldSelections.TryGetValue(requestName, out var selection) && selection.IsAvailable;
    }

    public bool TryGetFilter(string name, out PrototypeWizardExistingFilter filter)
    {
        return _filters.TryGetValue(name, out filter!);
    }

    public bool TryGetCreateRequired(string name, out bool isRequired)
    {
        return _createRequiredFields.TryGetValue(name, out isRequired);
    }
}

internal sealed class PrototypeWizardExistingFieldSelection
{
    private readonly HashSet<string> _names;

    public static PrototypeWizardExistingFieldSelection Unavailable { get; } = new(false, Array.Empty<string>());

    public PrototypeWizardExistingFieldSelection(bool isAvailable, IEnumerable<string> names)
    {
        IsAvailable = isAvailable;
        _names = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
    }

    public bool IsAvailable { get; }

    public bool TryGet(string name, out bool selected)
    {
        selected = _names.Contains(name);
        return IsAvailable;
    }
}

internal sealed class ExistingApiMetadataServiceSelection
{
    public static ExistingApiMetadataServiceSelection Unavailable { get; } = new(false, Array.Empty<string>(), new Dictionary<string, PrototypeWizardExistingService>(StringComparer.OrdinalIgnoreCase));

    public ExistingApiMetadataServiceSelection(bool isAvailable, IEnumerable<string> names)
        : this(isAvailable, names, new Dictionary<string, PrototypeWizardExistingService>(StringComparer.OrdinalIgnoreCase))
    {
    }

    public ExistingApiMetadataServiceSelection(
        bool isAvailable,
        IEnumerable<string> names,
        IReadOnlyDictionary<string, PrototypeWizardExistingService> services)
    {
        IsAvailable = isAvailable;
        Names = names.ToArray();
        Values = services;
    }

    public bool IsAvailable { get; }
    public IReadOnlyList<string> Names { get; }
    public IReadOnlyDictionary<string, PrototypeWizardExistingService> Values { get; }
}

internal sealed class ExistingApiMetadataServiceDescriptions
{
    public static ExistingApiMetadataServiceDescriptions Unavailable { get; } = new(false, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

    public ExistingApiMetadataServiceDescriptions(bool isAvailable, IReadOnlyDictionary<string, string> values)
    {
        IsAvailable = isAvailable;
        Values = values;
    }

    public bool IsAvailable { get; }
    public IReadOnlyDictionary<string, string> Values { get; }
}

internal sealed class PrototypeWizardExistingService
{
    public PrototypeWizardExistingService(string name, string httpMethod, string? restPath, string? operationId, string? description)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        HttpMethod = httpMethod ?? throw new ArgumentNullException(nameof(httpMethod));
        RestPath = restPath;
        OperationId = operationId;
        Description = description;
    }

    public string Name { get; }
    public string HttpMethod { get; }
    public string? RestPath { get; }
    public string? OperationId { get; }
    public string? Description { get; }
}

internal sealed class PrototypeWizardExistingStaticOrder
{
    public PrototypeWizardExistingStaticOrder(int order, string attributeName, string direction)
    {
        Order = order;
        AttributeName = attributeName ?? throw new ArgumentNullException(nameof(attributeName));
        Direction = direction ?? throw new ArgumentNullException(nameof(direction));
    }

    public int Order { get; }
    public string AttributeName { get; }
    public string Direction { get; }
}

internal sealed class PrototypeWizardExistingFilter
{
    public PrototypeWizardExistingFilter(string name, string? filterOperator, bool usesPeriod, bool usesRange)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        FilterOperator = filterOperator;
        UsesPeriod = usesPeriod;
        UsesRange = usesRange;
    }

    public string Name { get; }
    public string? FilterOperator { get; }
    public bool UsesPeriod { get; }
    public bool UsesRange { get; }
}
