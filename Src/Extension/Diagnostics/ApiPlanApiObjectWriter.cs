using System;
using System.Collections.Generic;
using System.Linq;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;
using GenexusOpenApiBuilder.Extension.Domain;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

internal static class ApiPlanApiObjectWriter
{
    private const string OwnedDescriptionPrefix = "Genexus Open API Builder B054 API Object";
    private const string ProcedureOwnedDescriptionPrefix = "Genexus Open API Builder B050-B053 Procedure";

    public static ApiPlanApiObjectWriteResult CreateOrReencounter(KBModel designModel, Transaction transaction, ApiPlan apiPlan)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        if (!string.Equals(transaction.Name, apiPlan.TransactionName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Criacao de API Object bloqueada: o ApiPlan em memoria nao pertence a Transaction selecionada atual. Nenhuma alteracao foi feita.");
        }

        var reencounteredSdts = PreflightRequiredSdts(designModel, apiPlan);
        var reencounteredProcedures = PreflightRequiredProcedures(designModel, apiPlan);
        var preflight = PreflightApiObject(designModel, apiPlan);
        var transactionFolder = ApiPlanTransactionFolder.CreateOrReencounter(designModel, transaction, apiPlan);
        var result = CreateOrReencounterApiObject(designModel, transactionFolder, apiPlan, preflight);

        return new ApiPlanApiObjectWriteResult(
            apiPlan.ApiName,
            result.Status,
            result.Guid,
            reencounteredSdts.Count,
            reencounteredProcedures.Count,
            transactionFolder.Name,
            transactionFolder.Guid,
            reencounteredProcedures,
            apiPlan.Services.Count);
    }

    internal static string CreateOwnedDescription(ApiPlan apiPlan)
    {
        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        return $"{OwnedDescriptionPrefix} - Transaction={apiPlan.TransactionName} - Procedures=B050-B053";
    }

    private static IReadOnlyList<Guid> PreflightRequiredSdts(KBModel designModel, ApiPlan apiPlan)
    {
        var generationPlan = ApiPlanSdtGenerationPlanBuilder.Create(apiPlan);
        var resolved = new List<Guid>();
        foreach (var definition in generationPlan.SharedSdts.Concat(generationPlan.OwnSdts))
        {
            var matches = SDT.GetAll(designModel)
                .Where(sdt => string.Equals(sdt.Name, definition.Name, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (matches.Length == 0)
            {
                throw new InvalidOperationException($"Criacao de API Object bloqueada: SDT requerido nao foi reencontrado: '{definition.Name}'. Execute B040-B046 antes. Nenhuma alteracao foi feita.");
            }

            if (matches.Length > 1)
            {
                throw new InvalidOperationException($"Criacao de API Object bloqueada: foram encontrados {matches.Length} SDTs chamados '{definition.Name}'. Nenhuma alteracao foi feita.");
            }

            var expectedDescription = ApiPlanSdtWriter.CreateOwnedDescriptionFor(definition.BacklogId, definition.Kind);
            var sdt = matches[0];
            if (!string.Equals(sdt.Description, expectedDescription, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Criacao de API Object bloqueada: SDT requerido externo ou incompativel chamado '{definition.Name}'. Execute B040-B046 para reencontrar SDTs proprios antes. Nenhuma alteracao foi feita.");
            }

            resolved.Add(sdt.Guid);
        }

        return resolved;
    }

    private static IReadOnlyList<ApiPlanApiObjectProcedureDependency> PreflightRequiredProcedures(KBModel designModel, ApiPlan apiPlan)
    {
        var definitions = CreateProcedureDefinitions(apiPlan);
        var resolved = new List<ApiPlanApiObjectProcedureDependency>();
        foreach (var definition in definitions)
        {
            var matches = Procedure.GetAll(designModel)
                .Where(procedure => string.Equals(procedure.Name, definition.Name, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (matches.Length == 0)
            {
                throw new InvalidOperationException($"Criacao de API Object bloqueada: Procedure requerida nao foi reencontrada: '{definition.Name}'. Execute B050-B053 antes. Nenhuma alteracao foi feita.");
            }

            if (matches.Length > 1)
            {
                throw new InvalidOperationException($"Criacao de API Object bloqueada: foram encontradas {matches.Length} Procedures chamadas '{definition.Name}'. Nenhuma alteracao foi feita.");
            }

            var expectedDescription = CreateExpectedProcedureDescription(definition);
            var procedure = matches[0];
            if (!string.Equals(procedure.Description, expectedDescription, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Criacao de API Object bloqueada: Procedure requerida externa ou incompativel chamada '{definition.Name}'. Execute B050-B053 para reencontrar Procedures proprias antes. Nenhuma alteracao foi feita.");
            }

            resolved.Add(new ApiPlanApiObjectProcedureDependency(definition.BacklogId, definition.ServiceName, definition.Name, procedure.Guid));
        }

        return resolved;
    }

    private static IReadOnlyList<ApiPlanApiObjectProcedureDefinition> CreateProcedureDefinitions(ApiPlan apiPlan)
    {
        return apiPlan.Services
            .Select(service => new ApiPlanApiObjectProcedureDefinition(ResolveBacklogId(service.Name), service.Name, $"proc{apiPlan.TransactionName}_API_{service.Name}"))
            .ToArray();
    }

    private static string ResolveBacklogId(string serviceName)
    {
        if (string.Equals(serviceName, "List", StringComparison.OrdinalIgnoreCase))
        {
            return "B050";
        }

        if (string.Equals(serviceName, "Get", StringComparison.OrdinalIgnoreCase))
        {
            return "B051";
        }

        if (string.Equals(serviceName, "Create", StringComparison.OrdinalIgnoreCase))
        {
            return "B052";
        }

        if (string.Equals(serviceName, "Update", StringComparison.OrdinalIgnoreCase))
        {
            return "B053";
        }

        return "B050-B053";
    }

    private static string CreateExpectedProcedureDescription(ApiPlanApiObjectProcedureDefinition definition)
    {
        return $"{ProcedureOwnedDescriptionPrefix} - {definition.BacklogId} - {definition.ServiceName}";
    }

    private static ApiPlanApiObjectPreflightResult PreflightApiObject(KBModel designModel, ApiPlan apiPlan)
    {
        var existing = API.GetAll(designModel)
            .Where(api => string.Equals(api.Name, apiPlan.ApiName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (existing.Length > 1)
        {
            throw new InvalidOperationException($"Criacao de API Object bloqueada: foram encontrados {existing.Length} API Objects chamados '{apiPlan.ApiName}'. Nenhuma alteracao foi feita.");
        }

        if (existing.Length == 0)
        {
            return new ApiPlanApiObjectPreflightResult(null);
        }

        var apiObject = existing[0];
        var expectedDescription = CreateOwnedDescription(apiPlan);
        if (!string.Equals(apiObject.Description, expectedDescription, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Criacao de API Object bloqueada: ja existe API Object externo ou incompativel chamado '{apiPlan.ApiName}'. Nenhuma alteracao foi feita.");
        }

        var expectedSource = CreateServiceGroupSource(apiPlan);
        var currentSource = apiObject.ServiceGroupSource.Source;
        if (!string.IsNullOrWhiteSpace(currentSource) && !string.Equals(currentSource, expectedSource, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Criacao de API Object bloqueada: o API Object '{apiPlan.ApiName}' possui servicos diferentes da geracao B054. Nenhuma alteracao foi feita.");
        }

        return new ApiPlanApiObjectPreflightResult(apiObject);
    }

    private static string CreateServiceGroupSource(ApiPlan apiPlan)
    {
        var services = CreateProcedureDefinitions(apiPlan)
            .Select(definition => $"    {definition.ServiceName}(){Environment.NewLine}        => {definition.Name}();")
            .ToArray();

        return $"{apiPlan.ApiName}{Environment.NewLine}{{{Environment.NewLine}{string.Join(Environment.NewLine + Environment.NewLine, services)}{Environment.NewLine}}}";
    }

    private static ApiPlanApiObjectWriteCoreResult CreateOrReencounterApiObject(KBModel designModel, Folder transactionFolder, ApiPlan apiPlan, ApiPlanApiObjectPreflightResult preflight)
    {
        if (preflight.ExistingApiObject is not null)
        {
            preflight.ExistingApiObject.Parent = transactionFolder;
            preflight.ExistingApiObject.ServiceGroupSource.Source = CreateServiceGroupSource(apiPlan);
            preflight.ExistingApiObject.Save();
            return new ApiPlanApiObjectWriteCoreResult(ApiPlanApiObjectWriteStatus.Reencountered, preflight.ExistingApiObject.Guid);
        }

        var apiObject = API.Create(designModel);
        apiObject.Name = apiPlan.ApiName;
        apiObject.Description = CreateOwnedDescription(apiPlan);
        apiObject.Parent = transactionFolder;
        apiObject.ServiceGroupSource.Source = CreateServiceGroupSource(apiPlan);

        apiObject.Save();

        var persisted = API.Get(designModel, apiObject.Guid);
        return new ApiPlanApiObjectWriteCoreResult(ApiPlanApiObjectWriteStatus.Created, persisted.Guid);
    }
}

internal static class ApiPlanApiObjectWriteStatus
{
    public const string Created = "Created";
    public const string Reencountered = "Reencountered";
}

internal sealed class ApiPlanApiObjectPreflightResult
{
    public ApiPlanApiObjectPreflightResult(API? existingApiObject)
    {
        ExistingApiObject = existingApiObject;
    }

    public API? ExistingApiObject { get; }
}

internal sealed class ApiPlanApiObjectWriteCoreResult
{
    public ApiPlanApiObjectWriteCoreResult(string status, Guid guid)
    {
        Status = status ?? throw new ArgumentNullException(nameof(status));
        Guid = guid;
    }
    public string Status { get; }

    public Guid Guid { get; }
}

internal sealed class ApiPlanApiObjectProcedureDefinition
{
    public ApiPlanApiObjectProcedureDefinition(string backlogId, string serviceName, string name)
    {
        BacklogId = backlogId ?? throw new ArgumentNullException(nameof(backlogId));
        ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public string BacklogId { get; }

    public string ServiceName { get; }

    public string Name { get; }
}

internal sealed class ApiPlanApiObjectProcedureDependency
{
    public ApiPlanApiObjectProcedureDependency(string backlogId, string serviceName, string name, Guid guid)
    {
        BacklogId = backlogId ?? throw new ArgumentNullException(nameof(backlogId));
        ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Guid = guid;
    }

    public string BacklogId { get; }

    public string ServiceName { get; }

    public string Name { get; }

    public Guid Guid { get; }
}

internal sealed class ApiPlanApiObjectWriteResult
{
    public ApiPlanApiObjectWriteResult(
        string apiName,
        string status,
        Guid guid,
        int reencounteredSdts,
        int reencounteredProcedures,
        string transactionFolderName,
        Guid transactionFolderGuid,
        IReadOnlyList<ApiPlanApiObjectProcedureDependency> procedures,
        int plannedServices)
    {
        ApiName = apiName ?? throw new ArgumentNullException(nameof(apiName));
        Status = status ?? throw new ArgumentNullException(nameof(status));
        Guid = guid;
        ReencounteredSdts = reencounteredSdts;
        ReencounteredProcedures = reencounteredProcedures;
        TransactionFolderName = transactionFolderName ?? throw new ArgumentNullException(nameof(transactionFolderName));
        TransactionFolderGuid = transactionFolderGuid;
        Procedures = procedures ?? throw new ArgumentNullException(nameof(procedures));
        PlannedServices = plannedServices;
    }

    public string ApiName { get; }

    public string Status { get; }

    public Guid Guid { get; }

    public int ReencounteredSdts { get; }

    public int ReencounteredProcedures { get; }

    public string TransactionFolderName { get; }

    public Guid TransactionFolderGuid { get; }

    public IReadOnlyList<ApiPlanApiObjectProcedureDependency> Procedures { get; }

    public int PlannedServices { get; }
}