using System;
using System.Collections.Generic;
using System.Linq;
using Artech.Genexus.Common.Objects;
using GenexusOpenApiBuilder.Extension.Diagnostics;

namespace GenexusOpenApiBuilder.Extension.Domain;

/// <summary>
/// Monta o plano interno inicial da Sprint 3 a partir das decisoes do wizard.
/// O plano permanece somente em memoria: nao persiste metadata e nao gera objetos na KB.
/// </summary>
internal static class ApiPlanBuilder
{
    private static readonly string[] SharedSdtNames =
    {
        "sdt_API_ErrorResponse",
        "sdt_API_Pagination",
    };

    public static ApiPlan Build(Transaction transaction, PrototypeWizardFlowSelection selection)
    {
        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        if (selection is null)
        {
            throw new ArgumentNullException(nameof(selection));
        }

        var contract = selection.ContractSelection;
        var review = selection.ReviewSelection;
        if (!string.Equals(contract.TransactionName, transaction.Name, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A selecao de contrato nao pertence a Transaction informada.");
        }

        if (!string.Equals(review.TransactionName, transaction.Name, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A selecao de revisao nao pertence a Transaction informada.");
        }

        var snapshot = PrototypeWizardContractReader.Read(transaction);
        var attributesByName = snapshot.Attributes.ToDictionary(item => item.Name, StringComparer.OrdinalIgnoreCase);
        var primaryKey = snapshot.Attributes
            .Where(item => item.IsPrimaryKey)
            .OrderBy(item => item.Order)
            .Select(CreateField)
            .ToArray();
        var createFields = CreateSelectedFields(contract.CreateFields, attributesByName);
        var updateFields = CreateSelectedFields(contract.UpdateFields, attributesByName);
        var responseFields = CreateSelectedFields(contract.ResponseFields, attributesByName);
        var filters = CreateFilters(contract.ListFilters, attributesByName);
        var requiredFields = selection.RequiredFields
            .Select(item => new ApiPlanRequiredField(item.RequestName, item.FieldName, item.IsRequired, item.Reason))
            .ToArray();
        var services = CreateServices(contract.SelectedServices, review.ApiName, review.RestPath, primaryKey);
        var security = ApiPlanSecurity.CreatePending(review.SecurityLevel);
        var names = ApiPlanNames.Create(transaction.Name, contract.SelectedServices);

        return new ApiPlan(
            transaction.Name,
            snapshot.ModuleName,
            ApiPlan.UnresolvedGeneratorTarget,
            review.ApiName,
            review.ServicesBasePath,
            review.RestPath,
            names.ProcedureNames,
            names.CreateRequestSdtName,
            names.UpdateRequestSdtName,
            names.ResponseSdtName,
            names.ListFiltersSdtName,
            names.ListResponseSdtName,
            SharedSdtNames,
            names.TransactionFolderName,
            false,
            review.SecurityLevel,
            security,
            review.DefaultPageSize,
            review.MaximumPageSize,
            review.StaticOrder.Select(item => new ApiPlanStaticOrder(item.Order, item.AttributeName, item.Direction)).ToArray(),
            CreateServiceDescriptions(services),
            ApiPlan.UnresolvedValue,
            true,
            services.Count,
            names.MetadataFileName,
            ApiPlan.UnresolvedConflictMode,
            ApiPlan.UnresolvedReexecutionMode,
            ApiPlan.RestArtifactTargetApiObject,
            false,
            ApiPlan.B038EngineReadinessNotes,
            primaryKey,
            createFields,
            updateFields,
            responseFields,
            filters,
            requiredFields,
            services,
            selection.BusinessComponentSelection);
    }

    private static IReadOnlyList<ApiPlanField> CreateSelectedFields(
        IEnumerable<string> names,
        IReadOnlyDictionary<string, PrototypeWizardAttributeDecision> attributesByName)
    {
        return names
            .Select(name => CreateField(GetAttribute(attributesByName, name)))
            .ToArray();
    }

    private static IReadOnlyList<ApiPlanFilter> CreateFilters(
        IEnumerable<string> names,
        IReadOnlyDictionary<string, PrototypeWizardAttributeDecision> attributesByName)
    {
        return names
            .Select(name =>
            {
                var attribute = GetAttribute(attributesByName, name);
                return new ApiPlanFilter(
                    CreateField(attribute),
                    attribute.FilterOperator,
                    attribute.UsesPeriod,
                    attribute.UsesRange);
            })
            .ToArray();
    }

    private static PrototypeWizardAttributeDecision GetAttribute(
        IReadOnlyDictionary<string, PrototypeWizardAttributeDecision> attributesByName,
        string name)
    {
        if (!attributesByName.TryGetValue(name, out var attribute))
        {
            throw new InvalidOperationException($"Atributo selecionado nao foi reencontrado na Transaction: {name}.");
        }

        return attribute;
    }

    private static ApiPlanField CreateField(PrototypeWizardAttributeDecision attribute)
    {
        return new ApiPlanField(
            attribute.Order,
            attribute.Name,
            attribute.DataType,
            attribute.Length,
            attribute.Decimals,
            attribute.IsPrimaryKey,
            attribute.IsNullable,
            attribute.IsSensitive,
            attribute.IsAudit,
            attribute.SensitiveClassificationSource,
            attribute.SensitiveClassificationReason,
            attribute.AuditClassificationSource,
            attribute.AuditClassificationReason,
            attribute.IsFormula,
            attribute.IsInferred,
            attribute.IsRedundant,
            attribute.IsPayloadEligible,
            attribute.IsUpdatePayloadEligible,
            attribute.IsFilterEligible);
    }

    private static IReadOnlyList<ApiPlanService> CreateServices(
        IReadOnlyList<string> selectedServices,
        string apiName,
        string restPath,
        IReadOnlyList<ApiPlanField> primaryKey)
    {
        return selectedServices
            .Select(service => CreateService(service, apiName, restPath, primaryKey))
            .ToArray();
    }

    private static ApiPlanService CreateService(string serviceName, string apiName, string restPath, IReadOnlyList<ApiPlanField> primaryKey)
    {
        var upperService = serviceName.ToUpperInvariant();
        if (upperService == "LIST")
        {
            return new ApiPlanService("List", "GET", restPath, apiName + ".List");
        }

        if (upperService == "GET")
        {
            return new ApiPlanService("Get", "GET", AppendKeyPath(restPath, primaryKey), apiName + ".Get");
        }

        if (upperService == "CREATE")
        {
            return new ApiPlanService("Create", "POST", restPath, apiName + ".Create");
        }

        if (upperService == "UPDATE")
        {
            return new ApiPlanService("Update", "PUT", AppendKeyPath(restPath, primaryKey), apiName + ".Update");
        }

        return new ApiPlanService(serviceName, string.Empty, restPath, apiName + "." + serviceName);
    }

    private static IReadOnlyList<ApiPlanServiceDescription> CreateServiceDescriptions(IEnumerable<ApiPlanService> services)
    {
        return services
            .Select(service => new ApiPlanServiceDescription(service.Name, ApiPlan.UnresolvedServiceDescription))
            .ToArray();
    }

    private static string AppendKeyPath(string restPath, IReadOnlyList<ApiPlanField> primaryKey)
    {
        if (primaryKey.Count == 0)
        {
            return restPath;
        }

        return restPath + "/" + string.Join("/", primaryKey.Select(item => "{" + item.Name + "}"));
    }
}

internal sealed class ApiPlan
{
    public const string UnresolvedValue = "UNRESOLVED_B038";
    public const string UnresolvedGeneratorTarget = "UNRESOLVED_B038_GENERATOR_TARGET";
    public const string UnresolvedConflictMode = "UNRESOLVED_B038_CONFLICT_MODE";
    public const string UnresolvedReexecutionMode = "UNRESOLVED_B038_REEXECUTION_MODE";
    public const string UnresolvedServiceDescription = "UNRESOLVED_B038_SERVICE_DESCRIPTION";
    public const string RestArtifactTargetApiObject = "API Object";

    public static readonly IReadOnlyList<string> B038EngineReadinessNotes = new[]
    {
        "B038 criou um ApiPlan inicial em memoria para rastrear decisoes do wizard.",
        "O plano ainda nao e entrada valida da engine: GeneratorTarget, ConflictMode, ReexecutionMode e descricoes de servico exigem resolucao posterior.",
    };

    public ApiPlan(
        string transactionName,
        string moduleTarget,
        string generatorTarget,
        string apiName,
        string servicesBasePath,
        string restPath,
        IReadOnlyList<string> procedureNames,
        string createRequestSdtName,
        string updateRequestSdtName,
        string responseSdtName,
        string listFiltersSdtName,
        string listResponseSdtName,
        IReadOnlyList<string> sharedSdtNames,
        string transactionFolderName,
        bool transactionFolderWasCreated,
        string securityLevel,
        ApiPlanSecurity security,
        int defaultPageSize,
        int maximumPageSize,
        IReadOnlyList<ApiPlanStaticOrder> staticOrder,
        IReadOnlyList<ApiPlanServiceDescription> serviceDescriptions,
        string serviceDescriptionLanguage,
        bool serviceDescriptionFallbackUsed,
        int endpointsCount,
        string metadataFileName,
        string conflictMode,
        string reexecutionMode,
        string restArtifactTarget,
        bool isEngineReady,
        IReadOnlyList<string> engineReadinessNotes,
        IReadOnlyList<ApiPlanField> primaryKey,
        IReadOnlyList<ApiPlanField> createRequestFields,
        IReadOnlyList<ApiPlanField> updateRequestFields,
        IReadOnlyList<ApiPlanField> responseFields,
        IReadOnlyList<ApiPlanFilter> listFilters,
        IReadOnlyList<ApiPlanRequiredField> requiredFields,
        IReadOnlyList<ApiPlanService> services,
        PrototypeWizardBusinessComponentSelection businessComponent)
    {
        TransactionName = transactionName ?? throw new ArgumentNullException(nameof(transactionName));
        ModuleTarget = moduleTarget ?? throw new ArgumentNullException(nameof(moduleTarget));
        GeneratorTarget = generatorTarget ?? throw new ArgumentNullException(nameof(generatorTarget));
        ApiName = apiName ?? throw new ArgumentNullException(nameof(apiName));
        ServicesBasePath = servicesBasePath ?? throw new ArgumentNullException(nameof(servicesBasePath));
        RestPath = restPath ?? throw new ArgumentNullException(nameof(restPath));
        ProcedureNames = procedureNames ?? throw new ArgumentNullException(nameof(procedureNames));
        CreateRequestSdtName = createRequestSdtName ?? throw new ArgumentNullException(nameof(createRequestSdtName));
        UpdateRequestSdtName = updateRequestSdtName ?? throw new ArgumentNullException(nameof(updateRequestSdtName));
        ResponseSdtName = responseSdtName ?? throw new ArgumentNullException(nameof(responseSdtName));
        ListFiltersSdtName = listFiltersSdtName ?? throw new ArgumentNullException(nameof(listFiltersSdtName));
        ListResponseSdtName = listResponseSdtName ?? throw new ArgumentNullException(nameof(listResponseSdtName));
        SharedSdtNames = sharedSdtNames ?? throw new ArgumentNullException(nameof(sharedSdtNames));
        TransactionFolderName = transactionFolderName ?? throw new ArgumentNullException(nameof(transactionFolderName));
        TransactionFolderWasCreated = transactionFolderWasCreated;
        SecurityLevel = securityLevel ?? throw new ArgumentNullException(nameof(securityLevel));
        Security = security ?? throw new ArgumentNullException(nameof(security));
        DefaultPageSize = defaultPageSize;
        MaximumPageSize = maximumPageSize;
        StaticOrder = staticOrder ?? throw new ArgumentNullException(nameof(staticOrder));
        ServiceDescriptions = serviceDescriptions ?? throw new ArgumentNullException(nameof(serviceDescriptions));
        ServiceDescriptionLanguage = serviceDescriptionLanguage ?? throw new ArgumentNullException(nameof(serviceDescriptionLanguage));
        ServiceDescriptionFallbackUsed = serviceDescriptionFallbackUsed;
        EndpointsCount = endpointsCount;
        MetadataFileName = metadataFileName ?? throw new ArgumentNullException(nameof(metadataFileName));
        ConflictMode = conflictMode ?? throw new ArgumentNullException(nameof(conflictMode));
        ReexecutionMode = reexecutionMode ?? throw new ArgumentNullException(nameof(reexecutionMode));
        RestArtifactTarget = restArtifactTarget ?? throw new ArgumentNullException(nameof(restArtifactTarget));
        IsEngineReady = isEngineReady;
        EngineReadinessNotes = engineReadinessNotes ?? throw new ArgumentNullException(nameof(engineReadinessNotes));
        PrimaryKey = primaryKey ?? throw new ArgumentNullException(nameof(primaryKey));
        CreateRequestFields = createRequestFields ?? throw new ArgumentNullException(nameof(createRequestFields));
        UpdateRequestFields = updateRequestFields ?? throw new ArgumentNullException(nameof(updateRequestFields));
        ResponseFields = responseFields ?? throw new ArgumentNullException(nameof(responseFields));
        ListFilters = listFilters ?? throw new ArgumentNullException(nameof(listFilters));
        RequiredFields = requiredFields ?? throw new ArgumentNullException(nameof(requiredFields));
        Services = services ?? throw new ArgumentNullException(nameof(services));
        BusinessComponent = businessComponent ?? throw new ArgumentNullException(nameof(businessComponent));
    }

    public string TransactionName { get; }

    public string ModuleTarget { get; }

    public string GeneratorTarget { get; }

    public string ApiName { get; }

    public string ServicesBasePath { get; }

    public string RestPath { get; }

    public IReadOnlyList<string> ProcedureNames { get; }

    public string CreateRequestSdtName { get; }

    public string UpdateRequestSdtName { get; }

    public string ResponseSdtName { get; }

    public string ListFiltersSdtName { get; }

    public string ListResponseSdtName { get; }

    public IReadOnlyList<string> SharedSdtNames { get; }

    public string TransactionFolderName { get; }

    public bool TransactionFolderWasCreated { get; }

    public string SecurityLevel { get; }

    public ApiPlanSecurity Security { get; }

    public int DefaultPageSize { get; }

    public int MaximumPageSize { get; }

    public IReadOnlyList<ApiPlanStaticOrder> StaticOrder { get; }

    public IReadOnlyList<ApiPlanServiceDescription> ServiceDescriptions { get; }

    public string ServiceDescriptionLanguage { get; }

    public bool ServiceDescriptionFallbackUsed { get; }

    public int EndpointsCount { get; }

    public string MetadataFileName { get; }

    public string ConflictMode { get; }

    public string ReexecutionMode { get; }

    public string RestArtifactTarget { get; }

    public bool IsEngineReady { get; }

    public IReadOnlyList<string> EngineReadinessNotes { get; }

    public IReadOnlyList<ApiPlanField> PrimaryKey { get; }

    public IReadOnlyList<ApiPlanField> CreateRequestFields { get; }

    public IReadOnlyList<ApiPlanField> UpdateRequestFields { get; }

    public IReadOnlyList<ApiPlanField> ResponseFields { get; }

    public IReadOnlyList<ApiPlanFilter> ListFilters { get; }

    public IReadOnlyList<ApiPlanRequiredField> RequiredFields { get; }

    public IReadOnlyList<ApiPlanService> Services { get; }

    public PrototypeWizardBusinessComponentSelection BusinessComponent { get; }
}

internal sealed class ApiPlanSecurity
{
    public const string PendingGamCondition = "UNRESOLVED_B092_GAM_CONDITION";

    private ApiPlanSecurity(string securityLevel, string gamCondition, bool requiresGenerationConfirmation, IReadOnlyList<string> notes)
    {
        SecurityLevel = securityLevel ?? throw new ArgumentNullException(nameof(securityLevel));
        GamCondition = gamCondition ?? throw new ArgumentNullException(nameof(gamCondition));
        RequiresGenerationConfirmation = requiresGenerationConfirmation;
        Notes = notes ?? throw new ArgumentNullException(nameof(notes));
    }

    public string SecurityLevel { get; }

    public string GamCondition { get; }

    public bool RequiresGenerationConfirmation { get; }

    public IReadOnlyList<string> Notes { get; }

    public static ApiPlanSecurity CreatePending(string securityLevel)
    {
        if (securityLevel is null)
        {
            throw new ArgumentNullException(nameof(securityLevel));
        }

        var requiresConfirmation = string.Equals(securityLevel, "None", StringComparison.OrdinalIgnoreCase)
            || string.Equals(securityLevel, "Authorization", StringComparison.OrdinalIgnoreCase);
        var notes = new[]
        {
            "B092 registrou Security Level no ApiPlan sem aplicar seguranca em objetos reais.",
            "Condicao GAM/None ainda depende de deteccao por API publica ou confirmacao antes da geracao definitiva.",
        };

        return new ApiPlanSecurity(securityLevel, PendingGamCondition, requiresConfirmation, notes);
    }
}

internal sealed class ApiPlanField
{
    public ApiPlanField(int order, string name, string dataType, int length, int decimals, bool isPrimaryKey, bool isNullable, bool isSensitive, bool isAuditField, string sensitiveClassificationSource, string sensitiveClassificationReason, string auditClassificationSource, string auditClassificationReason, bool isFormula, bool isInferred, bool isRedundant, bool isWritableByCreate, bool isWritableByUpdate, bool isFilterEligible)
    {
        Order = order;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
        Length = length;
        Decimals = decimals;
        IsPrimaryKey = isPrimaryKey;
        IsNullable = isNullable;
        IsSensitive = isSensitive;
        IsAuditField = isAuditField;
        SensitiveClassificationSource = sensitiveClassificationSource ?? throw new ArgumentNullException(nameof(sensitiveClassificationSource));
        SensitiveClassificationReason = sensitiveClassificationReason ?? throw new ArgumentNullException(nameof(sensitiveClassificationReason));
        AuditClassificationSource = auditClassificationSource ?? throw new ArgumentNullException(nameof(auditClassificationSource));
        AuditClassificationReason = auditClassificationReason ?? throw new ArgumentNullException(nameof(auditClassificationReason));
        IsFormula = isFormula;
        IsInferred = isInferred;
        IsRedundant = isRedundant;
        IsWritableByCreate = isWritableByCreate;
        IsWritableByUpdate = isWritableByUpdate;
        IsFilterEligible = isFilterEligible;
    }

    public int Order { get; }

    public string Name { get; }

    public string DataType { get; }

    public int Length { get; }

    public int Decimals { get; }

    public bool IsPrimaryKey { get; }

    public bool IsNullable { get; }

    public bool IsSensitive { get; }

    public bool IsAuditField { get; }

    public string SensitiveClassificationSource { get; }

    public string SensitiveClassificationReason { get; }

    public string AuditClassificationSource { get; }

    public string AuditClassificationReason { get; }

    public bool IsFormula { get; }

    public bool IsInferred { get; }

    public bool IsRedundant { get; }

    public bool IsWritableByCreate { get; }

    public bool IsWritableByUpdate { get; }

    public bool IsFilterEligible { get; }
}

internal sealed class ApiPlanFilter
{
    public ApiPlanFilter(ApiPlanField field, string filterOperator, bool usesPeriod, bool usesRange)
    {
        Field = field ?? throw new ArgumentNullException(nameof(field));
        FilterOperator = filterOperator ?? throw new ArgumentNullException(nameof(filterOperator));
        UsesPeriod = usesPeriod;
        UsesRange = usesRange;
    }

    public ApiPlanField Field { get; }

    public string FilterOperator { get; }

    public bool UsesPeriod { get; }

    public bool UsesRange { get; }
}

internal sealed class ApiPlanRequiredField
{
    public ApiPlanRequiredField(string requestName, string fieldName, bool isRequired, string reason)
    {
        RequestName = requestName ?? throw new ArgumentNullException(nameof(requestName));
        FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
        IsRequired = isRequired;
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
    }

    public string RequestName { get; }

    public string FieldName { get; }

    public bool IsRequired { get; }

    public string Reason { get; }
}

internal sealed class ApiPlanStaticOrder
{
    public ApiPlanStaticOrder(int order, string attributeName, string direction)
    {
        Order = order;
        AttributeName = attributeName ?? throw new ArgumentNullException(nameof(attributeName));
        Direction = direction ?? throw new ArgumentNullException(nameof(direction));
    }

    public int Order { get; }

    public string AttributeName { get; }

    public string Direction { get; }
}

internal sealed class ApiPlanServiceDescription
{
    public ApiPlanServiceDescription(string serviceName, string description)
    {
        ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }

    public string ServiceName { get; }

    public string Description { get; }
}

internal sealed class ApiPlanService
{
    public ApiPlanService(string name, string httpMethod, string restPath, string operationId)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        HttpMethod = httpMethod ?? throw new ArgumentNullException(nameof(httpMethod));
        RestPath = restPath ?? throw new ArgumentNullException(nameof(restPath));
        OperationId = operationId ?? throw new ArgumentNullException(nameof(operationId));
    }

    public string Name { get; }

    public string HttpMethod { get; }

    public string RestPath { get; }

    public string OperationId { get; }
}

internal sealed class ApiPlanNames
{
    private ApiPlanNames(string apiName, string metadataFileName, string transactionFolderName, IReadOnlyList<string> procedureNames, string createRequestSdtName, string updateRequestSdtName, string responseSdtName, string listFiltersSdtName, string listResponseSdtName)
    {
        ApiName = apiName;
        MetadataFileName = metadataFileName;
        TransactionFolderName = transactionFolderName;
        ProcedureNames = procedureNames;
        CreateRequestSdtName = createRequestSdtName;
        UpdateRequestSdtName = updateRequestSdtName;
        ResponseSdtName = responseSdtName;
        ListFiltersSdtName = listFiltersSdtName;
        ListResponseSdtName = listResponseSdtName;
    }

    public string ApiName { get; }

    public string MetadataFileName { get; }

    public string TransactionFolderName { get; }

    public IReadOnlyList<string> ProcedureNames { get; }

    public string CreateRequestSdtName { get; }

    public string UpdateRequestSdtName { get; }

    public string ResponseSdtName { get; }

    public string ListFiltersSdtName { get; }

    public string ListResponseSdtName { get; }

    public static ApiPlanNames Create(string baseName, IEnumerable<string> selectedServices)
    {
        var procedures = selectedServices
            .Select(service => $"proc{baseName}_API_{service}")
            .ToArray();

        return new ApiPlanNames(
            $"api{baseName}",
            $"api{baseName}_Metadata",
            $"{baseName}OpenApi",
            procedures,
            $"sdt{baseName}_API_CreateRequest",
            $"sdt{baseName}_API_UpdateRequest",
            $"sdt{baseName}_API_Response",
            $"sdt{baseName}_API_ListFilters",
            $"sdt{baseName}_API_ListResponse");
    }
}

internal static class ApiPlanSessionState
{
    private static ApiPlan? _current;

    public static ApiPlan? Current => _current;

    public static void Store(ApiPlan plan)
    {
        _current = plan ?? throw new ArgumentNullException(nameof(plan));
    }

    public static void Clear()
    {
        _current = null;
    }
}
