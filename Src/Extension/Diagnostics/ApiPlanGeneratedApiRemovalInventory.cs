#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using GenexusOpenApiBuilder.Extension.Domain;
using Newtonsoft.Json.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Fase 7 — inventário dinâmico de SDTs próprios para remoção a partir da metadata.
/// </summary>
internal static class ApiPlanGeneratedApiRemovalInventory
{
    private static readonly string[] SharedSdtNames =
    {
        "sdt_API_ErrorMessage",
        "sdt_API_ErrorResponse",
        "sdt_API_Pagination",
    };

    private static readonly string[] DefaultServices = { "List", "Get", "Create", "Update" };

    public static IReadOnlyList<string> ResolveOwnSdtNames(JObject metadata)
    {
        if (metadata is null)
        {
            throw new ArgumentNullException(nameof(metadata));
        }

        var ownFromMetadata = ReadStringArray(metadata.SelectToken("objects.sdts.own"));
        if (ownFromMetadata.Count > 0)
        {
            return ownFromMetadata;
        }

        if (ApiPlanMetadataLevelsCodec.HasHierarchicalLevels(metadata))
        {
            ApiPlan? stub;
            try
            {
                stub = TryCreateStubApiPlanFromMetadata(metadata);
            }
            catch (InvalidOperationException ex)
            {
                // levels anunciados mas ilegíveis: não cair no flat (deixaria SDTs de subnível órfãos).
                throw new InvalidOperationException(
                    "Metadata hierárquica com levels ilegível; a remoção não usa fallback flat. Corrija a metadata ou regenere a API.",
                    ex);
            }

            if (stub is not null)
            {
                return BuildOwnSdtNamesForRemoval(stub);
            }
        }

        return BuildFlatOwnSdtNames(metadata);
    }

    internal static IReadOnlyList<string> BuildOwnSdtNamesForRemoval(ApiPlan apiPlan)
    {
        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        if (ApiPlanSdtHierarchicalNaming.HasSelectedSublevels(apiPlan))
        {
            // OwnSdts sai em pós-ordem (filhos antes do pai). Remover precisa do inverso.
            return ApiPlanSdtGenerationPlanBuilder.Create(apiPlan).OwnSdts
                .Select(definition => definition.Name)
                .Reverse()
                .ToArray();
        }

        return new[]
        {
            apiPlan.ListResponseSdtName,
            apiPlan.CreateRequestSdtName,
            apiPlan.UpdateRequestSdtName,
            apiPlan.ListFiltersSdtName,
            apiPlan.ResponseSdtName,
        };
    }

    private static IReadOnlyList<string> BuildFlatOwnSdtNames(JObject metadata)
    {
        return new[]
            {
                metadata.SelectToken("objects.sdts.listResponse")?.Value<string>(),
                metadata.SelectToken("objects.sdts.createRequest")?.Value<string>(),
                metadata.SelectToken("objects.sdts.updateRequest")?.Value<string>(),
                metadata.SelectToken("objects.sdts.listFilters")?.Value<string>(),
                metadata.SelectToken("objects.sdts.response")?.Value<string>(),
            }
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static ApiPlan? TryCreateStubApiPlanFromMetadata(JObject metadata)
    {
        var transactionName = metadata.SelectToken("ownership.transactionName")?.Value<string>();
        if (string.IsNullOrWhiteSpace(transactionName))
        {
            return null;
        }

        var root = ApiPlanMetadataLevelsCodec.TryReadRoot(metadata);
        if (root is null || root.ChildLevels.Count == 0)
        {
            return null;
        }

        var sdts = metadata.SelectToken("objects.sdts") as JObject;
        if (sdts is null)
        {
            return null;
        }

        var createRequest = sdts["createRequest"]?.Value<string>();
        var updateRequest = sdts["updateRequest"]?.Value<string>();
        var response = sdts["response"]?.Value<string>();
        var listFilters = sdts["listFilters"]?.Value<string>();
        var listResponse = sdts["listResponse"]?.Value<string>();
        if (string.IsNullOrWhiteSpace(createRequest)
            || string.IsNullOrWhiteSpace(updateRequest)
            || string.IsNullOrWhiteSpace(response)
            || string.IsNullOrWhiteSpace(listFilters)
            || string.IsNullOrWhiteSpace(listResponse))
        {
            return null;
        }

        var transaction = transactionName!;
        var createRequestName = createRequest!;
        var updateRequestName = updateRequest!;
        var responseName = response!;
        var listFiltersName = listFilters!;
        var listResponseName = listResponse!;

        var apiName = metadata.SelectToken("ownership.apiName")?.Value<string>()
            ?? metadata.SelectToken("api.name")?.Value<string>()
            ?? "api" + transaction;
        var restPath = metadata.SelectToken("api.restPath")?.Value<string>()
            ?? "/" + transaction.ToLowerInvariant();
        var folderName = metadata.SelectToken("objects.transactionFolder.name")?.Value<string>()
            ?? transaction + "OpenApi";
        var metadataFileName = metadata.SelectToken("ownership.metadataFileName")?.Value<string>()
            ?? "api" + transaction + "_Metadata";
        var procedures = ReadStringArray(metadata.SelectToken("objects.procedures"));
        if (procedures.Count == 0)
        {
            procedures = ApiPlanNames.Create(transaction, DefaultServices).ProcedureNames;
        }

        var services = DefaultServices
            .Select(serviceName => new ApiPlanService(
                serviceName,
                string.Equals(serviceName, "Create", StringComparison.OrdinalIgnoreCase) ? "POST"
                    : string.Equals(serviceName, "Update", StringComparison.OrdinalIgnoreCase) ? "PUT"
                    : "GET",
                restPath,
                apiName + "." + serviceName))
            .ToArray();
        var descriptions = services
            .Select(service => new ApiPlanServiceDescription(service.Name, service.Name + " " + transactionName))
            .ToArray();
        var classification = ApiPlanFieldClassificationConfiguration.Create(
            PrototypeWizardFieldClassificationConfiguration.CreateDefaultInMemory(
                Array.Empty<string>(),
                Array.Empty<string>()));

        return new ApiPlan(
            transaction,
            "Root Module",
            ApiPlan.GeneratorTargetDotNet,
            apiName,
            metadata.SelectToken("api.servicesBasePath")?.Value<string>() ?? apiName,
            restPath,
            procedures,
            createRequestName,
            updateRequestName,
            responseName,
            listFiltersName,
            listResponseName,
            SharedSdtNames,
            folderName,
            metadata.SelectToken("objects.transactionFolder.wasCreated")?.Value<bool>() == true,
            metadata.SelectToken("security.level")?.Value<string>() ?? "Authentication",
            ApiPlanSecurity.CreateResolved(metadata.SelectToken("security.level")?.Value<string>() ?? "Authentication"),
            classification,
            metadata.SelectToken("pagination.defaultPageSize")?.Value<int>() ?? 50,
            metadata.SelectToken("pagination.maximumPageSize")?.Value<int>() ?? 200,
            Array.Empty<ApiPlanStaticOrder>(),
            descriptions,
            ApiPlan.ServiceDescriptionLanguageEnglish,
            ApiPlan.ServiceDescriptionLanguageSourcePendingKbLanguageApi,
            true,
            ApiPlan.ServiceDescriptionFallbackReasonPendingKbLanguageApi,
            services.Length,
            metadataFileName,
            ApiPlan.ConflictModeBlockOnCollision,
            ApiPlan.ReexecutionModeSafe,
            ApiPlan.RestArtifactTargetApiObject,
            true,
            Array.Empty<string>(),
            Array.Empty<ApiPlanField>(),
            Array.Empty<ApiPlanField>(),
            Array.Empty<ApiPlanField>(),
            Array.Empty<ApiPlanField>(),
            Array.Empty<ApiPlanFilter>(),
            Array.Empty<ApiPlanRequiredField>(),
            services,
            new PrototypeWizardBusinessComponentSelection(transaction, true, false, "RemovalInventoryStub"),
            includeBusinessComponentErrorMessages: true,
            new[] { root });
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
