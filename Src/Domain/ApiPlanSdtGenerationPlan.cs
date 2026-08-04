using System;
using System.Collections.Generic;
using System.Linq;

namespace GenexusOpenApiBuilder.Extension.Domain;

internal static class ApiPlanSdtGenerationPlanBuilder
{
    public static ApiPlanSdtGenerationPlan Create(ApiPlan apiPlan)
    {
        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        var responseSdtName = apiPlan.ResponseSdtName;
        var listFiltersSdtName = apiPlan.ListFiltersSdtName;
        var transactionFolderScope = ApiPlanSdtScope.CreateTransactionModuleFolderScope(apiPlan.TransactionFolderName);

        var ownSdts = new[]
        {
            CreateFieldBackedSdt(
                apiPlan.CreateRequestSdtName,
                "B040",
                "CreateRequest",
                transactionFolderScope,
                apiPlan.CreateRequestFields,
                "CreateRequest"),
            CreateFieldBackedSdt(
                apiPlan.UpdateRequestSdtName,
                "B041",
                "UpdateRequest",
                transactionFolderScope,
                apiPlan.UpdateRequestFields,
                "UpdateRequest"),
            CreateFieldBackedSdt(
                apiPlan.ResponseSdtName,
                "B042",
                "Response",
                transactionFolderScope,
                apiPlan.ResponseFields,
                "Response"),
            CreateListFiltersSdt(apiPlan, transactionFolderScope),
            CreateListResponseSdt(apiPlan.ListResponseSdtName, responseSdtName, listFiltersSdtName, transactionFolderScope),
        };

        var sharedSdts = new[]
        {
            CreateErrorResponseSdt(),
            CreatePaginationSdt(),
        };

        return new ApiPlanSdtGenerationPlan(
            "Sprint4SdtEnginePreviewOnly",
            false,
            "ResolvedSdtContractPreviewNoKbWrite",
            ownSdts,
            sharedSdts);
    }

    private static ApiPlanSdtDefinition CreateFieldBackedSdt(
        string name,
        string backlogId,
        string kind,
        string scope,
        IReadOnlyList<ApiPlanField> fields,
        string source)
    {
        return new ApiPlanSdtDefinition(
            name,
            backlogId,
            kind,
            scope,
            fields.Select(field => CreateMember(field, source)).ToArray());
    }

    private static ApiPlanSdtDefinition CreateListFiltersSdt(ApiPlan apiPlan, string scope)
    {
        return new ApiPlanSdtDefinition(
            apiPlan.ListFiltersSdtName,
            "B043",
            "ListFilters",
            scope,
            apiPlan.ListFilters.SelectMany(CreateFilterMembers).ToArray());
    }

    private static IEnumerable<ApiPlanSdtMember> CreateFilterMembers(ApiPlanFilter filter)
    {
        if (filter.UsesPeriod)
        {
            yield return CreateFilterMember(filter.Field, filter.Field.Name + "From", ResolvePeriodDataType(filter.Field));
            yield return CreateFilterMember(filter.Field, filter.Field.Name + "To", ResolvePeriodDataType(filter.Field));
            yield break;
        }

        if (filter.UsesRange)
        {
            yield return CreateFilterMember(filter.Field, filter.Field.Name + "Min", filter.Field.DataType);
            yield return CreateFilterMember(filter.Field, filter.Field.Name + "Max", filter.Field.DataType);
            yield break;
        }

        yield return CreateFilterMember(filter.Field, filter.Field.Name, filter.Field.DataType);
    }

    private static string ResolvePeriodDataType(ApiPlanField field)
    {
        return string.Equals(field.DataType, "DateTime", StringComparison.OrdinalIgnoreCase)
            ? "Date"
            : field.DataType;
    }

    private static ApiPlanSdtMember CreateFilterMember(ApiPlanField field, string name, string dataType)
    {
        return new ApiPlanSdtMember(
            name,
            dataType,
            field.Length,
            field.Decimals,
            true,
            false,
            string.Empty,
            "ListFilters");
    }

    private static ApiPlanSdtDefinition CreateListResponseSdt(string name, string responseSdtName, string listFiltersSdtName, string scope)
    {
        return new ApiPlanSdtDefinition(
            name,
            "B044",
            "ListResponse",
            scope,
            new[]
            {
                new ApiPlanSdtMember("Items", responseSdtName, 0, 0, false, true, responseSdtName, "ListResponse"),
                new ApiPlanSdtMember("Pagination", "sdt_API_Pagination", 0, 0, false, false, string.Empty, "ListResponse"),
                new ApiPlanSdtMember("AppliedFilters", listFiltersSdtName, 0, 0, true, false, string.Empty, "ListResponse"),
            });
    }

    private static ApiPlanSdtDefinition CreateErrorResponseSdt()
    {
        return new ApiPlanSdtDefinition(
            "sdt_API_ErrorResponse",
            "B045/B046",
            "SharedErrorResponse",
            ApiPlanSdtScope.RootModuleGxOpenApiFolder,
            new[]
            {
                new ApiPlanSdtMember("Code", "VarChar", 64, 0, false, false, string.Empty, "SharedErrorResponse"),
                new ApiPlanSdtMember("Message", "VarChar", 256, 0, false, false, string.Empty, "SharedErrorResponse"),
            });
    }

    private static ApiPlanSdtDefinition CreatePaginationSdt()
    {
        return new ApiPlanSdtDefinition(
            "sdt_API_Pagination",
            "B045/B046",
            "SharedPagination",
            ApiPlanSdtScope.RootModuleGxOpenApiFolder,
            new[]
            {
                new ApiPlanSdtMember("Page", "Numeric", 9, 0, false, false, string.Empty, "SharedPagination"),
                new ApiPlanSdtMember("PageSize", "Numeric", 9, 0, false, false, string.Empty, "SharedPagination"),
                new ApiPlanSdtMember("TotalCount", "Numeric", 18, 0, false, false, string.Empty, "SharedPagination"),
                new ApiPlanSdtMember("TotalPages", "Numeric", 9, 0, false, false, string.Empty, "SharedPagination"),
            });
    }

    private static ApiPlanSdtMember CreateMember(ApiPlanField field, string source)
    {
        return new ApiPlanSdtMember(
            field.Name,
            $"Attribute:{field.Name}",
            field.Length,
            field.Decimals,
            field.IsNullable,
            false,
            string.Empty,
            source);
    }
}

internal static class ApiPlanSdtScope
{
    private const string TransactionModuleFolderPrefix = "TransactionModuleFolder:";
    public const string RootModuleGxOpenApiFolder = "RootModuleFolder:GxOpenAPI";

    public static string CreateTransactionModuleFolderScope(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
        {
            throw new ArgumentException("Folder name is required.", nameof(folderName));
        }

        return TransactionModuleFolderPrefix + folderName;
    }
}

internal sealed class ApiPlanSdtGenerationPlan
{
    public ApiPlanSdtGenerationPlan(
        string phase,
        bool writesKnowledgeBase,
        string status,
        IReadOnlyList<ApiPlanSdtDefinition> ownSdts,
        IReadOnlyList<ApiPlanSdtDefinition> sharedSdts)
    {
        Phase = phase ?? throw new ArgumentNullException(nameof(phase));
        WritesKnowledgeBase = writesKnowledgeBase;
        Status = status ?? throw new ArgumentNullException(nameof(status));
        OwnSdts = ownSdts ?? throw new ArgumentNullException(nameof(ownSdts));
        SharedSdts = sharedSdts ?? throw new ArgumentNullException(nameof(sharedSdts));
    }

    public string Phase { get; }

    public bool WritesKnowledgeBase { get; }

    public string Status { get; }

    public IReadOnlyList<ApiPlanSdtDefinition> OwnSdts { get; }

    public IReadOnlyList<ApiPlanSdtDefinition> SharedSdts { get; }
}

internal sealed class ApiPlanSdtDefinition
{
    public ApiPlanSdtDefinition(string name, string backlogId, string kind, string scope, IReadOnlyList<ApiPlanSdtMember> members)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        BacklogId = backlogId ?? throw new ArgumentNullException(nameof(backlogId));
        Kind = kind ?? throw new ArgumentNullException(nameof(kind));
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        Members = members ?? throw new ArgumentNullException(nameof(members));
    }

    public string Name { get; }

    public string BacklogId { get; }

    public string Kind { get; }

    public string Scope { get; }

    public IReadOnlyList<ApiPlanSdtMember> Members { get; }
}
internal sealed class ApiPlanSdtMember
{
    public ApiPlanSdtMember(
        string name,
        string dataType,
        int length,
        int decimals,
        bool isNullable,
        bool isCollection,
        string collectionItemType,
        string source)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
        Length = length;
        Decimals = decimals;
        IsNullable = isNullable;
        IsCollection = isCollection;
        CollectionItemType = collectionItemType ?? throw new ArgumentNullException(nameof(collectionItemType));
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }
    public string Name { get; }
    public string DataType { get; }
    public int Length { get; }
    public int Decimals { get; }
    public bool IsNullable { get; }
    public bool IsCollection { get; }
    public string CollectionItemType { get; }
    public string Source { get; }
}
