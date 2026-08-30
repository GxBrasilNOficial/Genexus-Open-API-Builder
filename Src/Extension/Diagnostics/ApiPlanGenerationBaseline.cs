using System;
using System.Collections.Generic;
using System.Linq;
using GenexusOpenApiBuilder.Extension.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Linha de base offline da Fase 0: monta ApiPlan sintético de transações planas e emite
/// Source / Service Source / plano de SDT para comparação byte a byte no pré-push.
/// Não cobre a forma física do SDT na IDE (camada XPZ manual).
/// </summary>
internal static class ApiPlanGenerationBaseline
{
    private static readonly string[] AllServices = { "List", "Get", "Create", "Update" };
    private static readonly string[] SharedSdtNames =
    {
        "sdt_API_ErrorMessage",
        "sdt_API_ErrorResponse",
        "sdt_API_Pagination",
    };

    private const string ClassificationSource = "DefaultInMemoryHardcodedB090B091Policy";
    private const string NotSensitiveReason = "Nenhuma regra explicita de sensibilidade aplicavel.";
    private const string NotAuditReason = "Nenhuma regra explicita de auditoria operacional aplicavel.";

    public static IReadOnlyList<ApiPlanGenerationBaselineFixture> CreateFixtures()
    {
        return new[]
        {
            CreateFlatSimpleKey(),
            CreateFlatCompositeKey(),
            CreateFlatNoAccept(),
        };
    }

    public static ApiPlanGenerationBaselineSnapshot Capture(ApiPlan plan)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        return new ApiPlanGenerationBaselineSnapshot(
            ApiPlanBusinessComponentWriter.CreateCurrentSource(plan),
            ApiPlanBusinessComponentWriter.CreateCurrentUpdateSource(plan),
            ApiPlanBusinessComponentWriter.CreateCurrentGetSource(plan),
            ApiPlanListProcedureWriter.CreateCurrentListSource(plan),
            ApiPlanListProcedureWriter.CreateB070ServiceGroupSource(plan, includeBusinessComponentParameters: true),
            SerializeSdtPlan(ApiPlanSdtGenerationPlanBuilder.Create(plan)));
    }

    public static string NormalizeForComparison(string value)
    {
        return (value ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n");
    }

    public static string SerializeSdtPlan(ApiPlanSdtGenerationPlan plan)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        var root = new JObject
        {
            ["phase"] = plan.Phase,
            ["writesKnowledgeBase"] = plan.WritesKnowledgeBase,
            ["status"] = plan.Status,
            ["ownSdts"] = SerializeSdtDefinitions(plan.OwnSdts),
            ["sharedSdts"] = SerializeSdtDefinitions(plan.SharedSdts),
        };

        return root.ToString(Formatting.Indented) + "\n";
    }

    private static JArray SerializeSdtDefinitions(IReadOnlyList<ApiPlanSdtDefinition> definitions)
    {
        var array = new JArray();
        foreach (var definition in definitions)
        {
            var members = new JArray();
            foreach (var member in definition.Members)
            {
                members.Add(new JObject
                {
                    ["name"] = member.Name,
                    ["dataType"] = member.DataType,
                    ["length"] = member.Length,
                    ["decimals"] = member.Decimals,
                    ["isNullable"] = member.IsNullable,
                    ["isCollection"] = member.IsCollection,
                    ["collectionItemType"] = member.CollectionItemType,
                    ["source"] = member.Source,
                });
            }

            array.Add(new JObject
            {
                ["name"] = definition.Name,
                ["backlogId"] = definition.BacklogId,
                ["kind"] = definition.Kind,
                ["scope"] = definition.Scope,
                ["members"] = members,
            });
        }

        return array;
    }

    private static ApiPlanGenerationBaselineFixture CreateFlatSimpleKey()
    {
        var id = Field(
            order: 1,
            guid: "aaaaaaaa-0001-4000-8000-000000000001",
            name: "FlatSimpleKeyId",
            dataType: "Numeric",
            length: 9,
            decimals: 0,
            isPrimaryKey: true,
            isNullable: false,
            writableCreate: false,
            writableUpdate: false,
            filterEligible: true);
        var name = Field(
            order: 2,
            guid: "aaaaaaaa-0001-4000-8000-000000000002",
            name: "FlatSimpleKeyName",
            dataType: "VarChar",
            length: 60,
            decimals: 0,
            isPrimaryKey: false,
            isNullable: false,
            writableCreate: true,
            writableUpdate: true,
            filterEligible: true);
        var active = Field(
            order: 3,
            guid: "aaaaaaaa-0001-4000-8000-000000000003",
            name: "FlatSimpleKeyActive",
            dataType: "Boolean",
            length: 1,
            decimals: 0,
            isPrimaryKey: false,
            isNullable: false,
            writableCreate: true,
            writableUpdate: true,
            filterEligible: true);

        return BuildFixture(
            "FlatSimpleKey",
            "/flatsimplekey",
            new[] { id },
            new[] { name, active },
            new[] { name, active },
            new[] { id, name, active },
            new[]
            {
                new ApiPlanFilter(name, "Contem", false, false),
            },
            new[]
            {
                new ApiPlanRequiredField("UpdateRequest", name.Name, true, "PUT completo"),
                new ApiPlanRequiredField("UpdateRequest", active.Name, true, "PUT completo"),
            },
            new[] { new ApiPlanStaticOrder(1, id.Name, "ASC") });
    }

    private static ApiPlanGenerationBaselineFixture CreateFlatCompositeKey()
    {
        var companyId = Field(
            order: 1,
            guid: "bbbbbbbb-0002-4000-8000-000000000001",
            name: "FlatCompositeKeyCompanyId",
            dataType: "Numeric",
            length: 9,
            decimals: 0,
            isPrimaryKey: true,
            isNullable: false,
            writableCreate: true,
            writableUpdate: false,
            filterEligible: true);
        var itemId = Field(
            order: 2,
            guid: "bbbbbbbb-0002-4000-8000-000000000002",
            name: "FlatCompositeKeyItemId",
            dataType: "Numeric",
            length: 9,
            decimals: 0,
            isPrimaryKey: true,
            isNullable: false,
            writableCreate: true,
            writableUpdate: false,
            filterEligible: true);
        var code = Field(
            order: 3,
            guid: "bbbbbbbb-0002-4000-8000-000000000003",
            name: "FlatCompositeKeyCode",
            dataType: "VarChar",
            length: 20,
            decimals: 0,
            isPrimaryKey: true,
            isNullable: false,
            writableCreate: true,
            writableUpdate: false,
            filterEligible: true);
        var description = Field(
            order: 4,
            guid: "bbbbbbbb-0002-4000-8000-000000000004",
            name: "FlatCompositeKeyDescription",
            dataType: "VarChar",
            length: 100,
            decimals: 0,
            isPrimaryKey: false,
            isNullable: true,
            writableCreate: true,
            writableUpdate: true,
            filterEligible: true);

        return BuildFixture(
            "FlatCompositeKey",
            "/flatcompositekey",
            new[] { companyId, itemId, code },
            new[] { companyId, itemId, code, description },
            new[] { description },
            new[] { companyId, itemId, code, description },
            new[]
            {
                new ApiPlanFilter(companyId, "Igual", false, false),
                new ApiPlanFilter(description, "Contem", false, false),
            },
            new[]
            {
                new ApiPlanRequiredField("UpdateRequest", description.Name, true, "PUT completo"),
            },
            new[]
            {
                new ApiPlanStaticOrder(1, companyId.Name, "ASC"),
                new ApiPlanStaticOrder(2, itemId.Name, "ASC"),
            });
    }

    private static ApiPlanGenerationBaselineFixture CreateFlatNoAccept()
    {
        var id = Field(
            order: 1,
            guid: "cccccccc-0003-4000-8000-000000000001",
            name: "FlatNoAcceptId",
            dataType: "Numeric",
            length: 9,
            decimals: 0,
            isPrimaryKey: true,
            isNullable: false,
            writableCreate: false,
            writableUpdate: false,
            filterEligible: true);
        var title = Field(
            order: 2,
            guid: "cccccccc-0003-4000-8000-000000000002",
            name: "FlatNoAcceptTitle",
            dataType: "VarChar",
            length: 80,
            decimals: 0,
            isPrimaryKey: false,
            isNullable: false,
            writableCreate: true,
            writableUpdate: true,
            filterEligible: true);
        // Simula atributo NoAccept: permanece no Response, fora de Create/Update Request.
        var addedDate = Field(
            order: 3,
            guid: "cccccccc-0003-4000-8000-000000000003",
            name: "FlatNoAcceptAddedDate",
            dataType: "Date",
            length: 8,
            decimals: 0,
            isPrimaryKey: false,
            isNullable: false,
            writableCreate: false,
            writableUpdate: false,
            filterEligible: false);

        return BuildFixture(
            "FlatNoAccept",
            "/flatnoaccept",
            new[] { id },
            new[] { title },
            new[] { title },
            new[] { id, title, addedDate },
            new[]
            {
                new ApiPlanFilter(title, "Contem", false, false),
            },
            new[]
            {
                new ApiPlanRequiredField("UpdateRequest", title.Name, true, "PUT completo"),
            },
            new[] { new ApiPlanStaticOrder(1, id.Name, "ASC") });
    }

    private static ApiPlanGenerationBaselineFixture BuildFixture(
        string transactionName,
        string restPath,
        IReadOnlyList<ApiPlanField> primaryKey,
        IReadOnlyList<ApiPlanField> createRequestFields,
        IReadOnlyList<ApiPlanField> updateRequestFields,
        IReadOnlyList<ApiPlanField> responseFields,
        IReadOnlyList<ApiPlanFilter> listFilters,
        IReadOnlyList<ApiPlanRequiredField> requiredFields,
        IReadOnlyList<ApiPlanStaticOrder> staticOrder)
    {
        var names = ApiPlanNames.Create(transactionName, AllServices);
        var services = AllServices
            .Select(service => CreateService(service, names.ApiName, restPath, primaryKey))
            .ToArray();
        var descriptions = services
            .Select(service => new ApiPlanServiceDescription(service.Name, service.Name + " " + transactionName))
            .ToArray();
        var classification = ApiPlanFieldClassificationConfiguration.Create(
            PrototypeWizardFieldClassificationConfiguration.CreateDefaultInMemory(
                Array.Empty<string>(),
                Array.Empty<string>()));
        var plan = new ApiPlan(
            transactionName,
            "Root Module",
            ApiPlan.GeneratorTargetDotNet,
            names.ApiName,
            names.ApiName,
            restPath,
            names.ProcedureNames,
            names.CreateRequestSdtName,
            names.UpdateRequestSdtName,
            names.ResponseSdtName,
            names.ListFiltersSdtName,
            names.ListResponseSdtName,
            SharedSdtNames,
            names.TransactionFolderName,
            false,
            "Authentication",
            ApiPlanSecurity.CreateResolved("Authentication"),
            classification,
            50,
            200,
            staticOrder,
            descriptions,
            ApiPlan.ServiceDescriptionLanguageEnglish,
            ApiPlan.ServiceDescriptionLanguageSourcePendingKbLanguageApi,
            true,
            ApiPlan.ServiceDescriptionFallbackReasonPendingKbLanguageApi,
            services.Length,
            names.MetadataFileName,
            ApiPlan.ConflictModeBlockOnCollision,
            ApiPlan.ReexecutionModeSafe,
            ApiPlan.RestArtifactTargetApiObject,
            true,
            Array.Empty<string>(),
            primaryKey,
            createRequestFields,
            updateRequestFields,
            responseFields,
            listFilters,
            requiredFields,
            services,
            new PrototypeWizardBusinessComponentSelection(transactionName, true, false, "AlreadyEnabled"),
            includeBusinessComponentErrorMessages: true);

        return new ApiPlanGenerationBaselineFixture(transactionName, plan);
    }

    private static ApiPlanService CreateService(
        string serviceName,
        string apiName,
        string restPath,
        IReadOnlyList<ApiPlanField> primaryKey)
    {
        if (string.Equals(serviceName, "List", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiPlanService("List", "GET", restPath, apiName + ".List");
        }

        if (string.Equals(serviceName, "Get", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiPlanService("Get", "GET", AppendKeyPath(restPath, primaryKey), apiName + ".Get");
        }

        if (string.Equals(serviceName, "Create", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiPlanService("Create", "POST", restPath, apiName + ".Create");
        }

        if (string.Equals(serviceName, "Update", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiPlanService("Update", "PUT", AppendKeyPath(restPath, primaryKey), apiName + ".Update");
        }

        if (string.Equals(serviceName, "Delete", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiPlanService("Delete", "DELETE", AppendKeyPath(restPath, primaryKey), apiName + ".Delete");
        }

        return new ApiPlanService(serviceName, string.Empty, restPath, apiName + "." + serviceName);
    }

    private static string AppendKeyPath(string restPath, IReadOnlyList<ApiPlanField> primaryKey)
    {
        if (primaryKey.Count == 0)
        {
            return restPath;
        }

        return restPath + "/" + string.Join("/", primaryKey.Select(item => "{&" + item.Name + "}"));
    }

    private static ApiPlanField Field(
        int order,
        string guid,
        string name,
        string dataType,
        int length,
        int decimals,
        bool isPrimaryKey,
        bool isNullable,
        bool writableCreate,
        bool writableUpdate,
        bool filterEligible)
    {
        return new ApiPlanField(
            order,
            guid,
            name,
            dataType,
            length,
            decimals,
            isPrimaryKey,
            isNullable,
            false,
            false,
            ClassificationSource,
            NotSensitiveReason,
            ClassificationSource,
            NotAuditReason,
            false,
            false,
            false,
            writableCreate,
            writableUpdate,
            filterEligible);
    }
}

internal sealed class ApiPlanGenerationBaselineFixture
{
    public ApiPlanGenerationBaselineFixture(string name, ApiPlan plan)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
    }

    public string Name { get; }

    public ApiPlan Plan { get; }
}

internal sealed class ApiPlanGenerationBaselineSnapshot
{
    public ApiPlanGenerationBaselineSnapshot(
        string createSource,
        string updateSource,
        string getSource,
        string listSource,
        string serviceSource,
        string sdtPlanJson)
    {
        CreateSource = createSource ?? throw new ArgumentNullException(nameof(createSource));
        UpdateSource = updateSource ?? throw new ArgumentNullException(nameof(updateSource));
        GetSource = getSource ?? throw new ArgumentNullException(nameof(getSource));
        ListSource = listSource ?? throw new ArgumentNullException(nameof(listSource));
        ServiceSource = serviceSource ?? throw new ArgumentNullException(nameof(serviceSource));
        SdtPlanJson = sdtPlanJson ?? throw new ArgumentNullException(nameof(sdtPlanJson));
    }

    public string CreateSource { get; }

    public string UpdateSource { get; }

    public string GetSource { get; }

    public string ListSource { get; }

    public string ServiceSource { get; }

    public string SdtPlanJson { get; }

    public IReadOnlyDictionary<string, string> ToFileMap()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Create.source.txt"] = CreateSource,
            ["Update.source.txt"] = UpdateSource,
            ["Get.source.txt"] = GetSource,
            ["List.source.txt"] = ListSource,
            ["ApiObject.serviceSource.txt"] = ServiceSource,
            ["SdtPlan.json"] = SdtPlanJson,
        };
    }
}
