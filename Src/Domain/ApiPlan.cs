using System;
using System.Collections.Generic;
using System.Linq;
using Artech.Architecture.Common.Objects;
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
        "sdt_API_ErrorMessage",
        "sdt_API_ErrorResponse",
        "sdt_API_Pagination",
    };

    public static ApiPlan Build(Transaction transaction, PrototypeWizardFlowSelection selection)
    {
        return BuildInternal(transaction, selection, null);
    }

    public static ApiPlan Build(KBModel designModel, Transaction transaction, PrototypeWizardFlowSelection selection)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        return BuildInternal(transaction, selection, PrototypeWizardExistingApiContractReader.Read(designModel, transaction));
    }

    private static ApiPlan BuildInternal(
        Transaction transaction,
        PrototypeWizardFlowSelection selection,
        PrototypeWizardExistingApiContract? existingApiContract)
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
        var classificationConfiguration = ApiPlanFieldClassificationConfiguration.Create(snapshot.FieldClassificationConfiguration);
        var attributesByName = snapshot.Attributes.ToDictionary(item => item.Name, StringComparer.OrdinalIgnoreCase);
        var primaryKey = snapshot.Attributes
            .Where(item => item.IsPrimaryKey)
            .OrderBy(item => item.Order)
            .Select(CreateField)
            .ToArray();
        var createFields = CreateSelectedFields(contract.CreateFields, attributesByName, "CreateRequest");
        var updateFields = CreateSelectedFields(contract.UpdateFields, attributesByName, "UpdateRequest");
        var responseFields = CreateSelectedFields(contract.ResponseFields, attributesByName);
        var filters = CreateFilters(contract.ListFilters, attributesByName);
        var createFieldNames = new HashSet<string>(createFields.Select(item => item.Name), StringComparer.OrdinalIgnoreCase);
        var updateFieldNames = new HashSet<string>(updateFields.Select(item => item.Name), StringComparer.OrdinalIgnoreCase);
        var requiredFields = selection.RequiredFields
            .Where(item => IsSelectedRequestField(item, createFieldNames, updateFieldNames))
            .Select(item => new ApiPlanRequiredField(item.RequestName, item.FieldName, item.IsRequired, item.Reason))
            .ToArray();
        var preserveExistingServiceContract = existingApiContract is not null &&
            string.Equals(review.ApiName, existingApiContract.ApiName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(review.RestPath, existingApiContract.RestPath, StringComparison.Ordinal);
        var services = CreateServices(
            contract.SelectedServices,
            review.ApiName,
            review.RestPath,
            primaryKey,
            preserveExistingServiceContract ? existingApiContract : null);
        var security = ApiPlanSecurity.CreateResolved(review.SecurityLevel);
        var names = ApiPlanNames.Create(transaction.Name, contract.SelectedServices);
        var levels = ResolveHierarchicalLevels(selection);

        return new ApiPlan(
            transaction.Name,
            snapshot.ModuleName,
            ApiPlan.GeneratorTargetDotNet,
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
            classificationConfiguration,
            review.DefaultPageSize,
            review.MaximumPageSize,
            review.StaticOrder.Select(item => new ApiPlanStaticOrder(item.Order, item.AttributeName, item.Direction)).ToArray(),
            CreateServiceDescriptions(services, ResolveServiceDescriptionSubject(transaction), existingApiContract),
            ApiPlan.ServiceDescriptionLanguageEnglish,
            ApiPlan.ServiceDescriptionLanguageSourcePendingKbLanguageApi,
            true,
            ApiPlan.ServiceDescriptionFallbackReasonPendingKbLanguageApi,
            services.Count,
            names.MetadataFileName,
            ApiPlan.ConflictModeBlockOnCollision,
            ApiPlan.ReexecutionModeSafe,
            ApiPlan.RestArtifactTargetApiObject,
            false,
            ApiPlan.Sprint3EngineReadinessNotes,
            primaryKey,
            createFields,
            updateFields,
            responseFields,
            filters,
            requiredFields,
            services,
            selection.BusinessComponentSelection,
            review.IncludeBusinessComponentErrorMessages,
            levels);
    }

    private static IReadOnlyList<ApiPlanLevel>? ResolveHierarchicalLevels(PrototypeWizardFlowSelection selection)
    {
        var hierarchical = selection.HierarchicalSelection;
        if (hierarchical is null || !hierarchical.HasSublevels)
        {
            return null;
        }

        var pruned = hierarchical.Prune();
        if (pruned.ChildLevels.Count == 0)
        {
            return null;
        }

        return new[] { pruned };
    }

    private static IReadOnlyList<ApiPlanField> CreateSelectedFields(
        IEnumerable<string> names,
        IReadOnlyDictionary<string, PrototypeWizardAttributeDecision> attributesByName,
        string? requestName = null)
    {
        return names
            .Select(name => CreateField(GetAttribute(attributesByName, name)))
            .Where(field => requestName switch
            {
                "CreateRequest" => field.IsWritableByCreate,
                "UpdateRequest" => field.IsWritableByUpdate,
                _ => true,
            })
            .ToArray();
    }

    private static bool IsSelectedRequestField(
        PrototypeWizardRequiredFieldDecision field,
        ISet<string> createFieldNames,
        ISet<string> updateFieldNames)
    {
        var selectedNames = string.Equals(field.RequestName, "UpdateRequest", StringComparison.OrdinalIgnoreCase)
            ? updateFieldNames
            : createFieldNames;
        return selectedNames.Contains(field.FieldName);
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
            attribute.AttributeGuid,
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
        IReadOnlyList<ApiPlanField> primaryKey,
        PrototypeWizardExistingApiContract? existingApiContract)
    {
        return selectedServices
            .Select(service => existingApiContract is not null && existingApiContract.TryGetService(service, out var existingService)
                ? new ApiPlanService(
                    existingService.Name,
                    existingService.HttpMethod,
                    existingService.RestPath ?? CreateService(service, apiName, restPath, primaryKey).RestPath,
                    existingService.OperationId ?? apiName + "." + existingService.Name)
                : CreateService(service, apiName, restPath, primaryKey))
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

    private static IReadOnlyList<ApiPlanServiceDescription> CreateServiceDescriptions(
        IEnumerable<ApiPlanService> services,
        string transactionDescriptionSubject,
        PrototypeWizardExistingApiContract? existingApiContract)
    {
        return services
            .Select(service => new ApiPlanServiceDescription(
                service.Name,
                existingApiContract is not null && existingApiContract.ServiceDescriptions.TryGetValue(service.Name, out var description)
                    ? description
                    : CreateServiceDescription(service.Name, transactionDescriptionSubject)))
            .ToArray();
    }

    private static string ResolveServiceDescriptionSubject(Transaction transaction)
    {
        var description = transaction.Description;
        return string.IsNullOrWhiteSpace(description) ? transaction.Name : description.Trim();
    }

    private static string CreateServiceDescription(string serviceName, string transactionDescriptionSubject)
    {
        if (string.Equals(serviceName, "List", StringComparison.OrdinalIgnoreCase))
        {
            return "List " + transactionDescriptionSubject;
        }

        if (string.Equals(serviceName, "Get", StringComparison.OrdinalIgnoreCase))
        {
            return "Get " + transactionDescriptionSubject;
        }

        if (string.Equals(serviceName, "Create", StringComparison.OrdinalIgnoreCase))
        {
            return "Create " + transactionDescriptionSubject;
        }

        if (string.Equals(serviceName, "Update", StringComparison.OrdinalIgnoreCase))
        {
            return "Update " + transactionDescriptionSubject;
        }

        return serviceName + " " + transactionDescriptionSubject;
    }

    private static string AppendKeyPath(string restPath, IReadOnlyList<ApiPlanField> primaryKey)
    {
        if (primaryKey.Count == 0)
        {
            return restPath;
        }

        return restPath + "/" + string.Join("/", primaryKey.Select(item => "{&" + item.Name + "}"));
    }
}

internal sealed class ApiPlan
{
    public const string GeneratorTargetDotNet = ".NET";
    public const string ConflictModeBlockOnCollision = "BlockOnCollision";
    public const string ReexecutionModeSafe = "Safe";
    public const string UnresolvedB056ServiceDescription = "UNRESOLVED_B056_SERVICE_DESCRIPTION";
    public const string UnresolvedB056DescriptionLanguage = "UNRESOLVED_B056_DESCRIPTION_LANGUAGE";
    public const string ServiceDescriptionLanguageEnglish = "English";
    public const string ServiceDescriptionLanguageSourcePendingKbLanguageApi = "PendingKbLanguageApiValidation";
    public const string ServiceDescriptionFallbackReasonPendingKbLanguageApi = "Idioma principal da KB ainda nao validado por API publica; fallback tecnico em ingles registrado no ApiPlan.";
    public const string RestArtifactTargetApiObject = "API Object";

    public static readonly IReadOnlyList<string> Sprint3EngineReadinessNotes = new[]
    {
        "Sprint 3 resolveu GeneratorTarget como gerador prioritario inicial do MVP e ReexecutionMode como Safe.",
        "ConflictMode usa BlockOnCollision como politica conservadora inicial para colisao externa ou incompativel; update conservador de objeto proprio permanece governado por ReexecutionMode e ResolvedGenerationPlan futuro.",
        "ServiceDescriptions resolvidas no ApiPlan por B056 com fallback tecnico em ingles enquanto a API publica para idioma principal da KB nao for validada.",
        "GamCondition de B092 fica resolvida no ApiPlan conforme SecurityLevel selecionado, sem detectar GAM real da KB e sem aplicar seguranca em objetos reais.",
        "Configuracao por KB para B090/B091 esta representada no ApiPlan, mas ainda usa politica inicial em memoria sem metadata persistente.",
        "O plano ainda nao e entrada valida da engine real e nenhuma geracao foi validada.",
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
        ApiPlanFieldClassificationConfiguration fieldClassificationConfiguration,
        int defaultPageSize,
        int maximumPageSize,
        IReadOnlyList<ApiPlanStaticOrder> staticOrder,
        IReadOnlyList<ApiPlanServiceDescription> serviceDescriptions,
        string serviceDescriptionLanguage,
        string serviceDescriptionLanguageSource,
        bool serviceDescriptionFallbackUsed,
        string serviceDescriptionFallbackReason,
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
        PrototypeWizardBusinessComponentSelection businessComponent,
        bool includeBusinessComponentErrorMessages,
        IReadOnlyList<ApiPlanLevel>? levels = null)
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
        FieldClassificationConfiguration = fieldClassificationConfiguration ?? throw new ArgumentNullException(nameof(fieldClassificationConfiguration));
        DefaultPageSize = defaultPageSize;
        MaximumPageSize = maximumPageSize;
        StaticOrder = staticOrder ?? throw new ArgumentNullException(nameof(staticOrder));
        ServiceDescriptions = serviceDescriptions ?? throw new ArgumentNullException(nameof(serviceDescriptions));
        ServiceDescriptionLanguage = serviceDescriptionLanguage ?? throw new ArgumentNullException(nameof(serviceDescriptionLanguage));
        ServiceDescriptionLanguageSource = serviceDescriptionLanguageSource ?? throw new ArgumentNullException(nameof(serviceDescriptionLanguageSource));
        ServiceDescriptionFallbackUsed = serviceDescriptionFallbackUsed;
        ServiceDescriptionFallbackReason = serviceDescriptionFallbackReason ?? throw new ArgumentNullException(nameof(serviceDescriptionFallbackReason));
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
        IncludeBusinessComponentErrorMessages = includeBusinessComponentErrorMessages;
        // B095–B099a: árvore hierárquica opcional. O plano de SDT consome Levels (B096+),
        // o List emite ListResponse_Item (B098) e o Wizard popula a árvore podada desde B099a.
        Levels = levels ?? Array.Empty<ApiPlanLevel>();
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

    public bool TransactionFolderWasCreated { get; internal set; }

    public bool SharedSdtFolderWasCreated { get; internal set; }

    public string SecurityLevel { get; }

    public ApiPlanSecurity Security { get; }

    public ApiPlanFieldClassificationConfiguration FieldClassificationConfiguration { get; }

    public int DefaultPageSize { get; }

    public int MaximumPageSize { get; }

    public IReadOnlyList<ApiPlanStaticOrder> StaticOrder { get; }

    public IReadOnlyList<ApiPlanServiceDescription> ServiceDescriptions { get; }

    public string ServiceDescriptionLanguage { get; }

    public string ServiceDescriptionLanguageSource { get; }

    public bool ServiceDescriptionFallbackUsed { get; }

    public string ServiceDescriptionFallbackReason { get; }

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

    public bool IncludeBusinessComponentErrorMessages { get; }

    /// <summary>
    /// Árvore de níveis da Transaction (B095). Vazia enquanto o plano flat não carregar a leitura hierárquica.
    /// Depth 1 = cabeçalho; ParentLevelName vazio no raiz.
    /// </summary>
    public IReadOnlyList<ApiPlanLevel> Levels { get; }
}

internal sealed class ApiPlanSecurity
{
    public const string PendingGamCondition = "UNRESOLVED_B092_GAM_CONDITION";
    public const string AuthenticationGamCondition = "GAM_AUTHENTICATION_REQUIRED";
    public const string AuthorizationGamCondition = "GAM_AUTHORIZATION_REQUIRED_PENDING_PERMISSIONS";
    public const string NoneGamCondition = "NO_GAM_SECURITY_PUBLIC_API";

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

    public static ApiPlanSecurity CreateResolved(string securityLevel)
    {
        if (securityLevel is null)
        {
            throw new ArgumentNullException(nameof(securityLevel));
        }

        if (string.Equals(securityLevel, "Authentication", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiPlanSecurity(
                "Authentication",
                AuthenticationGamCondition,
                false,
                new[]
                {
                    "B092 resolveu Authentication no ApiPlan como autenticacao GAM obrigatoria para a futura geracao.",
                    "Ainda nao detecta GAM real da KB e nao aplica SecurityLevel em objetos reais.",
                });
        }

        if (string.Equals(securityLevel, "Authorization", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiPlanSecurity(
                "Authorization",
                AuthorizationGamCondition,
                true,
                new[]
                {
                    "B092 resolveu Authorization no ApiPlan como autorizacao GAM pendente de permissoes coerentes.",
                    "Geracao definitiva deve bloquear ate permissao GAM segura ou confirmacao posterior do fluxo de seguranca.",
                });
        }

        if (string.Equals(securityLevel, "None", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiPlanSecurity(
                "None",
                NoneGamCondition,
                true,
                new[]
                {
                    "B092 resolveu None no ApiPlan como API publica sem seguranca GAM.",
                    "Geracao definitiva deve exigir confirmacao explicita antes de aplicar SecurityLevel None nos servicos.",
                });
        }

        throw new InvalidOperationException($"Security Level nao suportado para B092: {securityLevel}.");
    }
}

internal sealed class ApiPlanFieldClassificationConfiguration
{
    private ApiPlanFieldClassificationConfiguration(
        string scope,
        string source,
        string status,
        bool isPersistedMetadata,
        bool isKnowledgeBaseConfigured,
        IReadOnlyList<string> sensitiveExactNames,
        IReadOnlyList<string> auditSuffixes,
        ApiPlanFieldClassificationMetadataContract metadataContract,
        IReadOnlyList<string> notes)
    {
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Status = status ?? throw new ArgumentNullException(nameof(status));
        IsPersistedMetadata = isPersistedMetadata;
        IsKnowledgeBaseConfigured = isKnowledgeBaseConfigured;
        SensitiveExactNames = sensitiveExactNames ?? throw new ArgumentNullException(nameof(sensitiveExactNames));
        AuditSuffixes = auditSuffixes ?? throw new ArgumentNullException(nameof(auditSuffixes));
        MetadataContract = metadataContract ?? throw new ArgumentNullException(nameof(metadataContract));
        Notes = notes ?? throw new ArgumentNullException(nameof(notes));
    }

    public string Scope { get; }

    public string Source { get; }

    public string Status { get; }

    public bool IsPersistedMetadata { get; }

    public bool IsKnowledgeBaseConfigured { get; }

    public IReadOnlyList<string> SensitiveExactNames { get; }

    public IReadOnlyList<string> AuditSuffixes { get; }

    public ApiPlanFieldClassificationMetadataContract MetadataContract { get; }

    public IReadOnlyList<string> Notes { get; }

    public static ApiPlanFieldClassificationConfiguration Create(PrototypeWizardFieldClassificationConfiguration configuration)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        return new ApiPlanFieldClassificationConfiguration(
            configuration.Scope,
            configuration.Source,
            configuration.Status,
            configuration.IsPersistedMetadata,
            configuration.IsKnowledgeBaseConfigured,
            configuration.SensitiveExactNames.ToArray(),
            configuration.AuditSuffixes.ToArray(),
            ApiPlanFieldClassificationMetadataContract.Create(configuration.MetadataContract),
            configuration.Notes.ToArray());
    }
}

internal sealed class ApiPlanFieldClassificationMetadataContract
{
    private ApiPlanFieldClassificationMetadataContract(
        string schemaVersion,
        string sectionName,
        string sensitiveExactNamesMember,
        string auditExactNamesMember,
        string auditSuffixesMember,
        IReadOnlyList<string> requiredMembers)
    {
        SchemaVersion = schemaVersion ?? throw new ArgumentNullException(nameof(schemaVersion));
        SectionName = sectionName ?? throw new ArgumentNullException(nameof(sectionName));
        SensitiveExactNamesMember = sensitiveExactNamesMember ?? throw new ArgumentNullException(nameof(sensitiveExactNamesMember));
        AuditExactNamesMember = auditExactNamesMember ?? throw new ArgumentNullException(nameof(auditExactNamesMember));
        AuditSuffixesMember = auditSuffixesMember ?? throw new ArgumentNullException(nameof(auditSuffixesMember));
        RequiredMembers = requiredMembers ?? throw new ArgumentNullException(nameof(requiredMembers));
    }

    public string SchemaVersion { get; }

    public string SectionName { get; }

    public string SensitiveExactNamesMember { get; }

    public string AuditExactNamesMember { get; }

    public string AuditSuffixesMember { get; }

    public IReadOnlyList<string> RequiredMembers { get; }

    public static ApiPlanFieldClassificationMetadataContract Create(PrototypeWizardFieldClassificationMetadataContract contract)
    {
        if (contract is null)
        {
            throw new ArgumentNullException(nameof(contract));
        }

        return new ApiPlanFieldClassificationMetadataContract(
            contract.SchemaVersion,
            contract.SectionName,
            contract.SensitiveExactNamesMember,
            contract.AuditExactNamesMember,
            contract.AuditSuffixesMember,
            contract.RequiredMembers.ToArray());
    }
}

internal sealed class ApiPlanField
{
    public ApiPlanField(int order, string attributeGuid, string name, string dataType, int length, int decimals, bool isPrimaryKey, bool isNullable, bool isSensitive, bool isAuditField, string sensitiveClassificationSource, string sensitiveClassificationReason, string auditClassificationSource, string auditClassificationReason, bool isFormula, bool isInferred, bool isRedundant, bool isWritableByCreate, bool isWritableByUpdate, bool isFilterEligible)
    {
        Order = order;
        if (string.IsNullOrWhiteSpace(attributeGuid))
        {
            throw new ArgumentException("Attribute GUID is required.", nameof(attributeGuid));
        }

        AttributeGuid = attributeGuid;
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

    public string AttributeGuid { get; }

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

/// <summary>
/// Nível da Transaction no modelo interno (B095). Filho do cabeçalho ou de outro subnível.
/// </summary>
internal sealed class ApiPlanLevel
{
    public ApiPlanLevel(
        string levelName,
        int depth,
        string parentLevelName,
        int levelOrder,
        IReadOnlyList<ApiPlanLevelField> primaryKey,
        IReadOnlyList<ApiPlanLevelField> fields,
        IReadOnlyList<ApiPlanLevel> childLevels,
        bool includeListCount = true)
    {
        if (string.IsNullOrWhiteSpace(levelName))
        {
            throw new ArgumentException("LevelName is required.", nameof(levelName));
        }

        if (depth < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(depth), depth, "Depth starts at 1 for the header.");
        }

        if (levelOrder < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(levelOrder), levelOrder, "LevelOrder starts at 1 among siblings.");
        }

        LevelName = levelName;
        Depth = depth;
        ParentLevelName = parentLevelName ?? string.Empty;
        LevelOrder = levelOrder;
        PrimaryKey = primaryKey ?? throw new ArgumentNullException(nameof(primaryKey));
        Fields = fields ?? throw new ArgumentNullException(nameof(fields));
        ChildLevels = childLevels ?? throw new ArgumentNullException(nameof(childLevels));
        // B098: contador de List ligado por padrao; o Wizard (B099a) desliga por subnivel direto.
        IncludeListCount = includeListCount;
    }

    public string LevelName { get; }

    public int Depth { get; }

    /// <summary>Vazio no cabeçalho (raiz).</summary>
    public string ParentLevelName { get; }

    public int LevelOrder { get; }

    public IReadOnlyList<ApiPlanLevelField> PrimaryKey { get; }

    public IReadOnlyList<ApiPlanLevelField> Fields { get; }

    public IReadOnlyList<ApiPlanLevel> ChildLevels { get; }

    /// <summary>
    /// Quando true e o nivel e filho direto do cabecalho, o List emite <c>&lt;Subnivel&gt;Count</c>
    /// em <c>ListResponse_Item</c> (B098). Neto nao recebe contador.
    /// </summary>
    public bool IncludeListCount { get; }

    public ApiPlanLevel WithIncludeListCount(bool includeListCount)
    {
        if (includeListCount == IncludeListCount)
        {
            return this;
        }

        return new ApiPlanLevel(
            LevelName,
            Depth,
            ParentLevelName,
            LevelOrder,
            PrimaryKey,
            Fields,
            ChildLevels,
            includeListCount);
    }
}

/// <summary>
/// Campo candidato lido na estrutura de um nível (B095). A seleção do Wizard (B099a) poda a árvore, sem flags neste tipo.
/// </summary>
internal sealed class ApiPlanLevelField
{
    public ApiPlanLevelField(
        int order,
        string attributeGuid,
        string name,
        string dataType,
        int length,
        int decimals,
        bool isPrimaryKey,
        bool isNullable,
        bool isInferred,
        bool isRedundant,
        bool isForeignKey,
        bool isFormula,
        bool isNoAccept,
        bool isAutonumber)
    {
        Order = order;
        if (string.IsNullOrWhiteSpace(attributeGuid))
        {
            throw new ArgumentException("Attribute GUID is required.", nameof(attributeGuid));
        }

        AttributeGuid = attributeGuid;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
        Length = length;
        Decimals = decimals;
        IsPrimaryKey = isPrimaryKey;
        IsNullable = isNullable;
        IsInferred = isInferred;
        IsRedundant = isRedundant;
        IsForeignKey = isForeignKey;
        IsFormula = isFormula;
        IsNoAccept = isNoAccept;
        IsAutonumber = isAutonumber;
    }

    public int Order { get; }

    public string AttributeGuid { get; }

    public string Name { get; }

    public string DataType { get; }

    public int Length { get; }

    public int Decimals { get; }

    public bool IsPrimaryKey { get; }

    public bool IsNullable { get; }

    public bool IsInferred { get; }

    public bool IsRedundant { get; }

    public bool IsForeignKey { get; }

    public bool IsFormula { get; }

    public bool IsNoAccept { get; }

    public bool IsAutonumber { get; }
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
