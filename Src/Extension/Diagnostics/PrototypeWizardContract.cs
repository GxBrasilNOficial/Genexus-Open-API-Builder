using System;
using System.Collections.Generic;
using System.Linq;
using Artech.Genexus.Common.Objects;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Monta, em modo somente leitura, as decisoes iniciais do Passo 2 do wizard.
/// Este snapshot e prototipico: nao e ApiPlan e nao deve ser persistido na KB.
/// </summary>
internal static class PrototypeWizardContractReader
{
    private static readonly string[] ServiceNames = { "List", "Get", "Create", "Update" };

    public static PrototypeWizardContractSnapshot Read(Transaction transaction)
    {
        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        var moduleName = transaction.Module?.Name ?? "<sem modulo>";
        var root = transaction.Structure.Root;
        var primaryKeyNames = new HashSet<string>(
            root.PrimaryKey.Select(part => part.Name),
            StringComparer.OrdinalIgnoreCase);
        var descriptionAttributeName = root.DescriptionAttribute?.Name;

        var services = ServiceNames
            .Select(name => new PrototypeWizardServiceDecision(name, true))
            .ToArray();

        var classificationPolicy = PrototypeWizardFieldClassificationPolicy.CreateDefault();
        var attributes = root.Attributes
            .Select((item, index) => CreateAttributeDecision(index + 1, item, primaryKeyNames, descriptionAttributeName, classificationPolicy))
            .ToArray();

        return new PrototypeWizardContractSnapshot(transaction.Name, moduleName, services, attributes, classificationPolicy.Configuration);
    }

    private static PrototypeWizardAttributeDecision CreateAttributeDecision(
        int order,
        Artech.Genexus.Common.Parts.TransactionAttribute item,
        ISet<string> primaryKeyNames,
        string? descriptionAttributeName,
        PrototypeWizardFieldClassificationPolicy classificationPolicy)
    {
        var attribute = item.Attribute;
        var name = item.Name;
        var type = attribute.Type.ToString();
        var isPrimaryKey = primaryKeyNames.Contains(name);
        var isDescription = string.Equals(name, descriptionAttributeName, StringComparison.OrdinalIgnoreCase) || item.IsDescriptionAttribute;
        var sensitiveClassification = classificationPolicy.ClassifySensitivity(name);
        var auditClassification = classificationPolicy.ClassifyAudit(name);
        var isSensitive = sensitiveClassification.IsMatch;
        var isAudit = auditClassification.IsMatch;
        var isFormula = IsFormula(attribute);
        var isTechnicallyInadequate = IsTechnicallyInadequate(type) || item.IsImageAttribute;
        var payloadDisabledReason = DescribePayloadDisabledReason(item, isPrimaryKey, isAudit, isFormula, isTechnicallyInadequate);
        var updatePayloadDisabledReason = DescribeUpdatePayloadDisabledReason(isPrimaryKey, payloadDisabledReason);
        var filter = ResolveFilter(type, isPrimaryKey, isDescription, isSensitive, isAudit, isTechnicallyInadequate);
        var isCreatePayloadCandidate = payloadDisabledReason.Length == 0;
        var isUpdatePayloadCandidate = updatePayloadDisabledReason.Length == 0;
        var defaultCreateSelected = isCreatePayloadCandidate && !isSensitive;
        var defaultUpdateSelected = isUpdatePayloadCandidate && !isSensitive;

        return new PrototypeWizardAttributeDecision(
            order,
            name,
            type,
            attribute.Length,
            attribute.Decimals,
            isPrimaryKey,
            isDescription,
            IsNullable(item.IsNullable),
            item.IsInferred,
            item.IsRedundant,
            item.IsForeignKey,
            isSensitive,
            isFormula,
            isAudit,
            sensitiveClassification.Source,
            sensitiveClassification.Reason,
            payloadDisabledReason,
            updatePayloadDisabledReason,
            auditClassification.Source,
            auditClassification.Reason,
            defaultCreateSelected,
            defaultUpdateSelected,
            !isSensitive,
            filter.IsEligible,
            filter.DefaultSelected,
            filter.Operator,
            filter.UsesPeriod,
            filter.UsesRange,
            filter.DisabledReason);
    }

    private static string DescribePayloadDisabledReason(
        Artech.Genexus.Common.Parts.TransactionAttribute item,
        bool isPrimaryKey,
        bool isAudit,
        bool isFormula,
        bool isTechnicallyInadequate)
    {
        if (item.IsInferred)
        {
            return "Desabilitado: atributo inferido";
        }

        if (item.IsRedundant)
        {
            return "Desabilitado: atributo redundante";
        }

        if (isFormula)
        {
            return "Desabilitado: formula nao atribuivel via BC";
        }

        if (isPrimaryKey)
        {
            return "Desabilitado no CreateRequest: chave primaria aguarda validacao publica de autonumeracao";
        }

        if (isAudit)
        {
            return "Desabilitado em request: auditoria operacional";
        }

        if (isTechnicallyInadequate)
        {
            return "Desabilitado: tipo tecnico inadequado";
        }

        return string.Empty;
    }

    private static string DescribeUpdatePayloadDisabledReason(bool isPrimaryKey, string payloadDisabledReason)
    {
        if (isPrimaryKey)
        {
            return "Desabilitado no UpdateRequest: chave primaria fica no RestPath";
        }

        return payloadDisabledReason;
    }

    private static PrototypeWizardFilterDefaults ResolveFilter(
        string type,
        bool isPrimaryKey,
        bool isDescription,
        bool isSensitive,
        bool isAudit,
        bool isTechnicallyInadequate)
    {
        if (isSensitive)
        {
            return PrototypeWizardFilterDefaults.Disabled("Desabilitado: campo sensivel nao retorna em appliedFilters");
        }

        if (isTechnicallyInadequate)
        {
            return PrototypeWizardFilterDefaults.Disabled("Desabilitado: tipo tecnico inadequado para filtro MVP");
        }

        if (IsText(type))
        {
            return PrototypeWizardFilterDefaults.Enabled(isPrimaryKey ? "Igual" : "Contem", isPrimaryKey || isDescription, false, false);
        }

        if (IsDateOrDateTime(type))
        {
            return PrototypeWizardFilterDefaults.Enabled("Periodo", isPrimaryKey || isDescription, true, false);
        }

        if (IsNumeric(type) || IsBoolean(type) || IsGuid(type))
        {
            return PrototypeWizardFilterDefaults.Enabled("Igual", isPrimaryKey || isDescription, false, false);
        }

        if (isAudit)
        {
            return PrototypeWizardFilterDefaults.Enabled("Igual", false, false, false);
        }

        return PrototypeWizardFilterDefaults.Disabled("Desabilitado: tipo ainda nao validado para filtro MVP");
    }

    private static bool IsNullable(object value)
    {
        var text = value.ToString();
        return string.Equals(text, "True", StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, "Yes", StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, "Nullable", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFormula(Artech.Genexus.Common.Objects.Attribute attribute)
    {
        return attribute.Formula is not null;
    }

    private static bool IsTechnicallyInadequate(string type)
    {
        return ContainsAny(type, "LongVarChar", "Image", "Audio", "Video", "Blob");
    }

    private static bool IsText(string type)
    {
        return ContainsAny(type, "Character", "VarChar", "Char", "LongVarChar") && !IsTechnicallyInadequate(type);
    }

    private static bool IsNumeric(string type)
    {
        return ContainsAny(type, "Numeric", "Integer", "SmallInt", "Int", "Decimal", "Float", "Double");
    }

    private static bool IsDateOrDateTime(string type)
    {
        return ContainsAny(type, "DateTime", "Date");
    }

    private static bool IsBoolean(string type)
    {
        return ContainsAny(type, "Boolean");
    }

    private static bool IsGuid(string type)
    {
        return ContainsAny(type, "Guid", "GUID");
    }

    private static bool ContainsAny(string value, params string[] tokens)
    {
        return tokens.Any(token => value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
    }
}

internal sealed class PrototypeWizardFieldClassificationPolicy
{
    private static readonly string[] SensitiveNames = { "Password", "Senha", "Hash", "Token", "Secret" };

    private static readonly string[] AuditSuffixes =
    {
        "InclusaoDataHora",
        "InclusaoUsuarioId",
        "InclusaoUsuarioNome",
        "UltimaAtualizacaoDataHora",
        "UltimaAtualizacaoUsuarioId",
        "UltimaAtualizacaoUsuarioNome",
    };

    public PrototypeWizardFieldClassificationConfiguration Configuration { get; }

    private readonly IReadOnlyList<string> _sensitiveNames;
    private readonly IReadOnlyList<string> _auditSuffixes;

    private PrototypeWizardFieldClassificationPolicy(PrototypeWizardFieldClassificationConfiguration configuration)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _sensitiveNames = configuration.SensitiveExactNames;
        _auditSuffixes = configuration.AuditSuffixes;
    }

    public static PrototypeWizardFieldClassificationPolicy CreateDefault()
    {
        return FromConfiguration(PrototypeWizardFieldClassificationConfiguration.CreateDefaultInMemory(SensitiveNames, AuditSuffixes));
    }

    public static PrototypeWizardFieldClassificationPolicy FromConfiguration(PrototypeWizardFieldClassificationConfiguration configuration)
    {
        return new PrototypeWizardFieldClassificationPolicy(configuration);
    }

    public PrototypeWizardFieldClassification ClassifySensitivity(string name)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        var token = _sensitiveNames.FirstOrDefault(item => string.Equals(name, item, StringComparison.OrdinalIgnoreCase));
        if (token is null)
        {
            return PrototypeWizardFieldClassification.NotMatched(Configuration.Source, "Nenhuma regra explicita de sensibilidade aplicavel.");
        }

        return PrototypeWizardFieldClassification.Matched(
            Configuration.Source,
            $"Nome igual a regra sensivel explicita '{token}'.");
    }

    public PrototypeWizardFieldClassification ClassifyAudit(string name)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        var suffix = _auditSuffixes.FirstOrDefault(item => name.EndsWith(item, StringComparison.OrdinalIgnoreCase));
        if (suffix is null)
        {
            return PrototypeWizardFieldClassification.NotMatched(Configuration.Source, "Nenhuma regra explicita de auditoria operacional aplicavel.");
        }

        return PrototypeWizardFieldClassification.Matched(
            Configuration.Source,
            $"Nome termina com sufixo de auditoria operacional explicito '{suffix}'.");
    }
}

internal sealed class PrototypeWizardFieldClassificationConfiguration
{
    private const string DefaultPolicySource = "DefaultInMemoryHardcodedB090B091Policy";

    private PrototypeWizardFieldClassificationConfiguration(
        string scope,
        string source,
        string status,
        bool isPersistedMetadata,
        bool isKnowledgeBaseConfigured,
        IReadOnlyList<string> sensitiveExactNames,
        IReadOnlyList<string> auditSuffixes,
        PrototypeWizardFieldClassificationMetadataContract metadataContract,
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

    public PrototypeWizardFieldClassificationMetadataContract MetadataContract { get; }

    public IReadOnlyList<string> Notes { get; }

    public static PrototypeWizardFieldClassificationConfiguration CreateDefaultInMemory(
        IReadOnlyList<string> sensitiveExactNames,
        IReadOnlyList<string> auditSuffixes)
    {
        if (sensitiveExactNames is null)
        {
            throw new ArgumentNullException(nameof(sensitiveExactNames));
        }

        if (auditSuffixes is null)
        {
            throw new ArgumentNullException(nameof(auditSuffixes));
        }

        var notes = new[]
        {
            "Contrato minimo de configuracao por KB preparado em memoria; metadata persistente ainda nao existe.",
            "B090/B091 canonicos continuam abertos ate carregar regras explicitas por KB a partir de metadata persistente.",
        };

        return new PrototypeWizardFieldClassificationConfiguration(
            "KnowledgeBase",
            DefaultPolicySource,
            "PendingPersistentMetadata",
            false,
            false,
            sensitiveExactNames.ToArray(),
            auditSuffixes.ToArray(),
            PrototypeWizardFieldClassificationMetadataContract.CreateV1(),
            notes);
    }
}

internal sealed class PrototypeWizardFieldClassificationMetadataContract
{
    private static readonly string[] V1RequiredMembers =
    {
        "schemaVersion",
        "fieldClassification",
        "sensitiveExactNames",
        "auditExactNames",
        "auditSuffixes",
    };

    private PrototypeWizardFieldClassificationMetadataContract(
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

    public static PrototypeWizardFieldClassificationMetadataContract CreateV1()
    {
        return new PrototypeWizardFieldClassificationMetadataContract(
            "B090B091_KB_FIELD_CLASSIFICATION_V1",
            "fieldClassification",
            "sensitiveExactNames",
            "auditExactNames",
            "auditSuffixes",
            V1RequiredMembers);
    }
}

internal sealed class PrototypeWizardFieldClassification
{
    private PrototypeWizardFieldClassification(bool isMatch, string source, string reason)
    {
        IsMatch = isMatch;
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
    }

    public bool IsMatch { get; }

    public string Source { get; }

    public string Reason { get; }

    public static PrototypeWizardFieldClassification Matched(string source, string reason)
    {
        return new PrototypeWizardFieldClassification(true, source, reason);
    }

    public static PrototypeWizardFieldClassification NotMatched(string source, string reason)
    {
        return new PrototypeWizardFieldClassification(false, source, reason);
    }
}

internal sealed class PrototypeWizardContractSnapshot
{
    public PrototypeWizardContractSnapshot(
        string transactionName,
        string moduleName,
        IReadOnlyList<PrototypeWizardServiceDecision> services,
        IReadOnlyList<PrototypeWizardAttributeDecision> attributes,
        PrototypeWizardFieldClassificationConfiguration fieldClassificationConfiguration)
    {
        TransactionName = transactionName ?? throw new ArgumentNullException(nameof(transactionName));
        ModuleName = moduleName ?? throw new ArgumentNullException(nameof(moduleName));
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
        FieldClassificationConfiguration = fieldClassificationConfiguration ?? throw new ArgumentNullException(nameof(fieldClassificationConfiguration));
    }

    public string TransactionName { get; }

    public string ModuleName { get; }

    public IReadOnlyList<PrototypeWizardServiceDecision> Services { get; }

    public IReadOnlyList<PrototypeWizardAttributeDecision> Attributes { get; }

    public PrototypeWizardFieldClassificationConfiguration FieldClassificationConfiguration { get; }
}

internal sealed class PrototypeWizardServiceDecision
{
    public PrototypeWizardServiceDecision(string name, bool defaultSelected)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DefaultSelected = defaultSelected;
    }

    public string Name { get; }

    public bool DefaultSelected { get; }
}

internal sealed class PrototypeWizardAttributeDecision
{
    public PrototypeWizardAttributeDecision(
        int order,
        string name,
        string dataType,
        int length,
        int decimals,
        bool isPrimaryKey,
        bool isDescription,
        bool isNullable,
        bool isInferred,
        bool isRedundant,
        bool isForeignKey,
        bool isSensitive,
        bool isFormula,
        bool isAudit,
        string sensitiveClassificationSource,
        string sensitiveClassificationReason,
        string payloadDisabledReason,
        string updatePayloadDisabledReason,
        string auditClassificationSource,
        string auditClassificationReason,
        bool defaultCreateSelected,
        bool defaultUpdateSelected,
        bool defaultResponseSelected,
        bool isFilterEligible,
        bool defaultFilterSelected,
        string filterOperator,
        bool usesPeriod,
        bool usesRange,
        string filterDisabledReason)
    {
        Order = order;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
        Length = length;
        Decimals = decimals;
        IsPrimaryKey = isPrimaryKey;
        IsDescription = isDescription;
        IsNullable = isNullable;
        IsInferred = isInferred;
        IsRedundant = isRedundant;
        IsForeignKey = isForeignKey;
        IsSensitive = isSensitive;
        IsFormula = isFormula;
        IsAudit = isAudit;
        SensitiveClassificationSource = sensitiveClassificationSource ?? throw new ArgumentNullException(nameof(sensitiveClassificationSource));
        SensitiveClassificationReason = sensitiveClassificationReason ?? throw new ArgumentNullException(nameof(sensitiveClassificationReason));
        PayloadDisabledReason = payloadDisabledReason ?? throw new ArgumentNullException(nameof(payloadDisabledReason));
        UpdatePayloadDisabledReason = updatePayloadDisabledReason ?? throw new ArgumentNullException(nameof(updatePayloadDisabledReason));
        AuditClassificationSource = auditClassificationSource ?? throw new ArgumentNullException(nameof(auditClassificationSource));
        AuditClassificationReason = auditClassificationReason ?? throw new ArgumentNullException(nameof(auditClassificationReason));
        DefaultCreateSelected = defaultCreateSelected;
        DefaultUpdateSelected = defaultUpdateSelected;
        DefaultResponseSelected = defaultResponseSelected;
        IsFilterEligible = isFilterEligible;
        DefaultFilterSelected = defaultFilterSelected;
        FilterOperator = filterOperator ?? throw new ArgumentNullException(nameof(filterOperator));
        UsesPeriod = usesPeriod;
        UsesRange = usesRange;
        FilterDisabledReason = filterDisabledReason ?? throw new ArgumentNullException(nameof(filterDisabledReason));
    }

    public int Order { get; }

    public string Name { get; }

    public string DataType { get; }

    public int Length { get; }

    public int Decimals { get; }

    public bool IsPrimaryKey { get; }

    public bool IsDescription { get; }

    public bool IsNullable { get; }

    public bool IsInferred { get; }

    public bool IsRedundant { get; }

    public bool IsForeignKey { get; }

    public bool IsSensitive { get; }

    public bool IsFormula { get; }

    public bool IsAudit { get; }

    public string SensitiveClassificationSource { get; }

    public string SensitiveClassificationReason { get; }

    public string AuditClassificationSource { get; }

    public string AuditClassificationReason { get; }

    public string PayloadDisabledReason { get; }

    public string UpdatePayloadDisabledReason { get; }

    public bool IsPayloadEligible => PayloadDisabledReason.Length == 0;

    public bool IsUpdatePayloadEligible => UpdatePayloadDisabledReason.Length == 0;

    public bool DefaultCreateSelected { get; }

    public bool DefaultUpdateSelected { get; }

    public bool DefaultResponseSelected { get; }

    public bool IsFilterEligible { get; }

    public bool DefaultFilterSelected { get; }

    public string FilterOperator { get; }

    public bool UsesPeriod { get; }

    public bool UsesRange { get; }

    public string FilterDisabledReason { get; }
}

internal sealed class PrototypeWizardFilterDefaults
{
    private PrototypeWizardFilterDefaults(bool isEligible, bool defaultSelected, string @operator, bool usesPeriod, bool usesRange, string disabledReason)
    {
        IsEligible = isEligible;
        DefaultSelected = defaultSelected;
        Operator = @operator;
        UsesPeriod = usesPeriod;
        UsesRange = usesRange;
        DisabledReason = disabledReason;
    }

    public bool IsEligible { get; }

    public bool DefaultSelected { get; }

    public string Operator { get; }

    public bool UsesPeriod { get; }

    public bool UsesRange { get; }

    public string DisabledReason { get; }

    public static PrototypeWizardFilterDefaults Enabled(string @operator, bool defaultSelected, bool usesPeriod, bool usesRange)
    {
        return new PrototypeWizardFilterDefaults(true, defaultSelected, @operator, usesPeriod, usesRange, string.Empty);
    }

    public static PrototypeWizardFilterDefaults Disabled(string reason)
    {
        return new PrototypeWizardFilterDefaults(false, false, string.Empty, false, false, reason);
    }
}

internal sealed class PrototypeWizardContractSelection
{
    public PrototypeWizardContractSelection(
        string transactionName,
        IReadOnlyList<string> selectedServices,
        IReadOnlyList<string> createFields,
        IReadOnlyList<string> updateFields,
        IReadOnlyList<string> responseFields,
        IReadOnlyList<string> listFilters)
    {
        TransactionName = transactionName ?? throw new ArgumentNullException(nameof(transactionName));
        SelectedServices = selectedServices ?? throw new ArgumentNullException(nameof(selectedServices));
        CreateFields = createFields ?? throw new ArgumentNullException(nameof(createFields));
        UpdateFields = updateFields ?? throw new ArgumentNullException(nameof(updateFields));
        ResponseFields = responseFields ?? throw new ArgumentNullException(nameof(responseFields));
        ListFilters = listFilters ?? throw new ArgumentNullException(nameof(listFilters));
    }

    public string TransactionName { get; }

    public IReadOnlyList<string> SelectedServices { get; }

    public IReadOnlyList<string> CreateFields { get; }

    public IReadOnlyList<string> UpdateFields { get; }

    public IReadOnlyList<string> ResponseFields { get; }

    public IReadOnlyList<string> ListFilters { get; }
}

internal static class PrototypeWizardSessionState
{
    public static PrototypeWizardContractSelection? ContractSelection { get; private set; }

    public static void StoreContractSelection(PrototypeWizardContractSelection selection)
    {
        ContractSelection = selection ?? throw new ArgumentNullException(nameof(selection));
    }

    public static void ClearContractSelection()
    {
        ContractSelection = null;
    }
}
