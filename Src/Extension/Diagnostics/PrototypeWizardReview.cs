using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Artech.Genexus.Common.Objects;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Monta, em modo somente leitura, as decisoes prototipicas do Passo 3 do wizard.
/// Este snapshot complementa B031, mas ainda nao e ApiPlan e nao deve ser persistido na KB.
/// </summary>
internal static class PrototypeWizardReviewReader
{
    public static PrototypeWizardReviewSnapshot Read(Transaction transaction, PrototypeWizardContractSelection contractSelection)
    {
        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        if (contractSelection is null)
        {
            throw new ArgumentNullException(nameof(contractSelection));
        }

        var root = transaction.Structure.Root;
        var primaryKeyParts = root.PrimaryKey
            .Select((part, index) => new PrototypeWizardPrimaryKeyPathPart(index + 1, part.Name, part.Attribute.Type.ToString()))
            .ToArray();
        var apiName = "api" + transaction.Name;
        var restPath = "/" + ToKebabCase(transaction.Name);
        var selectedServices = contractSelection.SelectedServices.ToArray();
        var endpoints = selectedServices
            .Select(service => CreateEndpoint(service, restPath, primaryKeyParts))
            .ToArray();
        var staticOrder = primaryKeyParts
            .Select(part => new PrototypeWizardStaticOrderPart(part.Order, part.Name, "ASC"))
            .ToArray();

        return new PrototypeWizardReviewSnapshot(
            transaction.Name,
            transaction.Module?.Name ?? "<sem modulo>",
            apiName,
            apiName,
            restPath,
            "Authentication",
            50,
            200,
            selectedServices,
            primaryKeyParts,
            endpoints,
            staticOrder,
            contractSelection);
    }

    private static PrototypeWizardEndpointDecision CreateEndpoint(
        string service,
        string restPath,
        IReadOnlyList<PrototypeWizardPrimaryKeyPathPart> primaryKeyParts)
    {
        var upperService = service.ToUpperInvariant();
        if (upperService == "LIST")
        {
            return new PrototypeWizardEndpointDecision("List", "GET", restPath);
        }

        if (upperService == "GET")
        {
            return new PrototypeWizardEndpointDecision("Get", "GET", AppendKeyPath(restPath, primaryKeyParts));
        }

        if (upperService == "CREATE")
        {
            return new PrototypeWizardEndpointDecision("Create", "POST", restPath);
        }

        if (upperService == "UPDATE")
        {
            return new PrototypeWizardEndpointDecision("Update", "PUT", AppendKeyPath(restPath, primaryKeyParts));
        }

        return new PrototypeWizardEndpointDecision(service, "<nao definido>", restPath);
    }

    private static string AppendKeyPath(string restPath, IReadOnlyList<PrototypeWizardPrimaryKeyPathPart> primaryKeyParts)
    {
        if (primaryKeyParts.Count == 0)
        {
            return restPath;
        }

        return restPath + "/" + string.Join("/", primaryKeyParts.Select(part => "{" + part.Name + "}"));
    }

    private static string ToKebabCase(string value)
    {
        var builder = new StringBuilder();
        var previousWasSeparator = false;

        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (!char.IsLetterOrDigit(current))
            {
                AppendSeparator(builder, ref previousWasSeparator);
                continue;
            }

            if (char.IsUpper(current) && builder.Length > 0 && ShouldSplitBeforeUpper(value, index))
            {
                AppendSeparator(builder, ref previousWasSeparator);
            }

            builder.Append(char.ToLowerInvariant(current));
            previousWasSeparator = false;
        }

        return builder.ToString().Trim('-');
    }

    private static bool ShouldSplitBeforeUpper(string value, int index)
    {
        var previous = value[index - 1];
        if (char.IsLower(previous) || char.IsDigit(previous))
        {
            return true;
        }

        if (index + 1 >= value.Length)
        {
            return false;
        }

        return char.IsUpper(previous) && char.IsLower(value[index + 1]);
    }

    private static void AppendSeparator(StringBuilder builder, ref bool previousWasSeparator)
    {
        if (builder.Length == 0 || previousWasSeparator)
        {
            return;
        }

        builder.Append('-');
        previousWasSeparator = true;
    }
}

internal sealed class PrototypeWizardReviewSnapshot
{
    public PrototypeWizardReviewSnapshot(
        string transactionName,
        string moduleName,
        string apiName,
        string servicesBasePath,
        string restPath,
        string securityLevel,
        int defaultPageSize,
        int maximumPageSize,
        IReadOnlyList<string> selectedServices,
        IReadOnlyList<PrototypeWizardPrimaryKeyPathPart> primaryKeyParts,
        IReadOnlyList<PrototypeWizardEndpointDecision> endpoints,
        IReadOnlyList<PrototypeWizardStaticOrderPart> staticOrder,
        PrototypeWizardContractSelection contractSelection)
    {
        TransactionName = transactionName ?? throw new ArgumentNullException(nameof(transactionName));
        ModuleName = moduleName ?? throw new ArgumentNullException(nameof(moduleName));
        ApiName = apiName ?? throw new ArgumentNullException(nameof(apiName));
        ServicesBasePath = servicesBasePath ?? throw new ArgumentNullException(nameof(servicesBasePath));
        RestPath = restPath ?? throw new ArgumentNullException(nameof(restPath));
        SecurityLevel = securityLevel ?? throw new ArgumentNullException(nameof(securityLevel));
        DefaultPageSize = defaultPageSize;
        MaximumPageSize = maximumPageSize;
        SelectedServices = selectedServices ?? throw new ArgumentNullException(nameof(selectedServices));
        PrimaryKeyParts = primaryKeyParts ?? throw new ArgumentNullException(nameof(primaryKeyParts));
        Endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
        StaticOrder = staticOrder ?? throw new ArgumentNullException(nameof(staticOrder));
        ContractSelection = contractSelection ?? throw new ArgumentNullException(nameof(contractSelection));
    }

    public string TransactionName { get; }

    public string ModuleName { get; }

    public string ApiName { get; }

    public string ServicesBasePath { get; }

    public string RestPath { get; }

    public string SecurityLevel { get; }

    public int DefaultPageSize { get; }

    public int MaximumPageSize { get; }

    public IReadOnlyList<string> SelectedServices { get; }

    public IReadOnlyList<PrototypeWizardPrimaryKeyPathPart> PrimaryKeyParts { get; }

    public IReadOnlyList<PrototypeWizardEndpointDecision> Endpoints { get; }

    public IReadOnlyList<PrototypeWizardStaticOrderPart> StaticOrder { get; }

    public PrototypeWizardContractSelection ContractSelection { get; }
}

internal sealed class PrototypeWizardPrimaryKeyPathPart
{
    public PrototypeWizardPrimaryKeyPathPart(int order, string name, string dataType)
    {
        Order = order;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
    }

    public int Order { get; }

    public string Name { get; }

    public string DataType { get; }
}

internal sealed class PrototypeWizardEndpointDecision
{
    public PrototypeWizardEndpointDecision(string serviceName, string httpMethod, string restPath)
    {
        ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        HttpMethod = httpMethod ?? throw new ArgumentNullException(nameof(httpMethod));
        RestPath = restPath ?? throw new ArgumentNullException(nameof(restPath));
    }

    public string ServiceName { get; }

    public string HttpMethod { get; }

    public string RestPath { get; }
}

internal sealed class PrototypeWizardStaticOrderPart
{
    public PrototypeWizardStaticOrderPart(int order, string attributeName, string direction)
    {
        Order = order;
        AttributeName = attributeName ?? throw new ArgumentNullException(nameof(attributeName));
        Direction = direction ?? throw new ArgumentNullException(nameof(direction));
    }

    public int Order { get; }

    public string AttributeName { get; }

    public string Direction { get; }
}

internal sealed class PrototypeWizardReviewSelection
{
    public PrototypeWizardReviewSelection(string transactionName, string apiName, string servicesBasePath, string restPath, string securityLevel, int defaultPageSize, int maximumPageSize, IReadOnlyList<PrototypeWizardStaticOrderPart> staticOrder)
    {
        TransactionName = transactionName ?? throw new ArgumentNullException(nameof(transactionName));

        ApiName = apiName ?? throw new ArgumentNullException(nameof(apiName));
        ServicesBasePath = servicesBasePath ?? throw new ArgumentNullException(nameof(servicesBasePath));
        RestPath = restPath ?? throw new ArgumentNullException(nameof(restPath));
        SecurityLevel = securityLevel ?? throw new ArgumentNullException(nameof(securityLevel));
        DefaultPageSize = defaultPageSize;
        MaximumPageSize = maximumPageSize;
        StaticOrder = staticOrder ?? throw new ArgumentNullException(nameof(staticOrder));
    }

    public string TransactionName { get; }

    public string ApiName { get; }

    public string ServicesBasePath { get; }

    public string RestPath { get; }

    public string SecurityLevel { get; }

    public int DefaultPageSize { get; }

    public int MaximumPageSize { get; }

    public IReadOnlyList<PrototypeWizardStaticOrderPart> StaticOrder { get; }
}

internal static class PrototypeWizardReviewSessionState
{
    public static PrototypeWizardReviewSelection? ReviewSelection { get; private set; }

    public static void StoreReviewSelection(PrototypeWizardReviewSelection selection)
    {
        ReviewSelection = selection ?? throw new ArgumentNullException(nameof(selection));
    }

    public static void ClearReviewSelection()
    {
        ReviewSelection = null;
    }
}