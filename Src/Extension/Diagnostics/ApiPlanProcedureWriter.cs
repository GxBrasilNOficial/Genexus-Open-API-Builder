using System;
using System.Collections.Generic;
using System.Linq;
using Artech.Architecture.Common.Objects;
using Artech.Architecture.Common.Services;
using Artech.Genexus.Common.Objects;
using GenexusOpenApiBuilder.Extension.Domain;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

internal static class ApiPlanProcedureWriter
{
    private const string OwnedDescriptionPrefix = "Genexus Open API Builder B050-B053 Procedure";

    public static ApiPlanProcedureWriteResult CreateOrReencounter(KBModel designModel, Transaction transaction, ApiPlan apiPlan)
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
            throw new InvalidOperationException("Criacao de Procedures bloqueada: o ApiPlan em memoria nao pertence a Transaction selecionada atual. Nenhuma alteracao foi feita.");
        }

        var sdtGenerationPlan = ApiPlanSdtGenerationPlanBuilder.Create(apiPlan);
        var resolvedSdts = PreflightRequiredSdts(designModel, sdtGenerationPlan);
        var definitions = CreateProcedureDefinitions(apiPlan);
        var preflight = PreflightProcedures(designModel, definitions);
        var transactionFolder = ApiPlanTransactionFolder.CreateOrReencounter(designModel, transaction, apiPlan);
        var results = new List<ApiPlanProcedureWriteItemResult>();

        foreach (var definition in definitions)
        {
            results.Add(CreateOrReencounterProcedure(designModel, transaction, transactionFolder, definition, preflight));
        }

        return new ApiPlanProcedureWriteResult(
            definitions.Count,
            resolvedSdts.Count,
            results.Count(item => item.Status == ApiPlanProcedureWriteStatus.Created),
            results.Count(item => item.Status == ApiPlanProcedureWriteStatus.Reencountered),
            transactionFolder.Name,
            transactionFolder.Guid,
            results);
    }

    private static IReadOnlyList<Guid> PreflightRequiredSdts(KBModel designModel, ApiPlanSdtGenerationPlan generationPlan)
    {
        var resolved = new List<Guid>();
        foreach (var definition in generationPlan.SharedSdts.Concat(generationPlan.OwnSdts))
        {
            var matches = SDT.GetAll(designModel)
                .Where(sdt => string.Equals(sdt.Name, definition.Name, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (matches.Length == 0)
            {
                throw new InvalidOperationException($"Criacao de Procedures bloqueada: SDT requerido nao foi reencontrado: '{definition.Name}'. Execute B040-B046 antes. Nenhuma alteracao foi feita.");
            }

            if (matches.Length > 1)
            {
                throw new InvalidOperationException($"Criacao de Procedures bloqueada: foram encontrados {matches.Length} SDTs chamados '{definition.Name}'. Nenhuma alteracao foi feita.");
            }

            var expectedDescription = ApiPlanSdtWriter.CreateOwnedDescriptionFor(definition.BacklogId, definition.Kind);
            var sdt = matches[0];
            if (!string.Equals(sdt.Description, expectedDescription, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Criacao de Procedures bloqueada: SDT requerido externo ou incompativel chamado '{definition.Name}'. Execute B040-B046 para reencontrar SDTs proprios antes. Nenhuma alteracao foi feita.");
            }

            resolved.Add(sdt.Guid);
        }

        return resolved;
    }
    private static IReadOnlyList<ApiPlanProcedureDefinition> CreateProcedureDefinitions(ApiPlan apiPlan)
    {
        return apiPlan.Services
            .Select(service => new ApiPlanProcedureDefinition(ResolveBacklogId(service.Name), service.Name, $"proc{apiPlan.TransactionName}_API_{service.Name}"))
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

    private static ApiPlanProcedurePreflightResult PreflightProcedures(KBModel designModel, IReadOnlyList<ApiPlanProcedureDefinition> definitions)
    {
        var existingByName = new Dictionary<string, Procedure>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in definitions)
        {
            var existing = Procedure.GetAll(designModel)
                .Where(procedure => string.Equals(procedure.Name, definition.Name, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (existing.Length > 1)
            {
                throw new InvalidOperationException($"Criacao de Procedure bloqueada: foram encontradas {existing.Length} Procedures chamadas '{definition.Name}'. Nenhuma alteracao foi feita.");
            }

            var description = CreateOwnedDescription(definition);
            if (existing.Length == 1)
            {
                var existingProcedure = existing[0];
                if (!string.Equals(existingProcedure.Description, description, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Criacao de Procedure bloqueada: ja existe Procedure externa ou incompativel chamada '{definition.Name}'. Nenhuma alteracao foi feita.");
                }

                existingByName.Add(definition.Name, existingProcedure);
            }
        }

        return new ApiPlanProcedurePreflightResult(existingByName);
    }

    private static ApiPlanProcedureWriteItemResult CreateOrReencounterProcedure(KBModel designModel, Transaction transaction, Folder transactionFolder, ApiPlanProcedureDefinition definition, ApiPlanProcedurePreflightResult preflight)
    {
        if (preflight.ExistingProceduresByName.TryGetValue(definition.Name, out var existingProcedure))
        {
            existingProcedure.Parent = transactionFolder;
            existingProcedure.Save();
            return new ApiPlanProcedureWriteItemResult(definition.BacklogId, definition.ServiceName, definition.Name, ApiPlanProcedureWriteStatus.Reencountered, existingProcedure.Guid);
        }

        var procedure = new Procedure(designModel)
        {
            Name = definition.Name,
            Description = CreateOwnedDescription(definition),
        };

        procedure.Parent = transactionFolder;

        ConfigureProcedure(procedure, definition);
        procedure.Save();

        var persisted = Procedure.Get(designModel, procedure.Guid);
        return new ApiPlanProcedureWriteItemResult(definition.BacklogId, definition.ServiceName, definition.Name, ApiPlanProcedureWriteStatus.Created, persisted.Guid);
    }

    private static string CreateOwnedDescription(ApiPlanProcedureDefinition definition)
    {
        return $"{OwnedDescriptionPrefix} - {definition.BacklogId} - {definition.ServiceName}";
    }

    private static void ConfigureProcedure(Procedure procedure, ApiPlanProcedureDefinition definition)
    {
        procedure.ProcedurePart.Content.ContentType = ContentType.PlainText;
        procedure.ProcedurePart.Content.Content =
            $"// Genexus Open API Builder {definition.BacklogId}: Procedure skeleton for {definition.ServiceName}. REST behavior remains pending Sprint 6." + Environment.NewLine +
            $"msg(!\"Genexus Open API Builder {definition.BacklogId} {definition.ServiceName} skeleton. REST behavior pending Sprint 6.\", status)";
        procedure.Rules.Content.ContentType = ContentType.PlainText;
        procedure.Rules.Content.Content = string.Empty;
    }
}

internal static class ApiPlanProcedureWriteStatus
{
    public const string Created = "Created";
    public const string Reencountered = "Reencountered";
}

internal sealed class ApiPlanProcedureDefinition
{
    public ApiPlanProcedureDefinition(string backlogId, string serviceName, string name)
    {
        BacklogId = backlogId ?? throw new ArgumentNullException(nameof(backlogId));
        ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
    public string BacklogId { get; }

    public string ServiceName { get; }

    public string Name { get; }
}

internal sealed class ApiPlanProcedurePreflightResult
{
    public ApiPlanProcedurePreflightResult(IReadOnlyDictionary<string, Procedure> existingProceduresByName)
    {
        ExistingProceduresByName = existingProceduresByName ?? throw new ArgumentNullException(nameof(existingProceduresByName));
    }

    public IReadOnlyDictionary<string, Procedure> ExistingProceduresByName { get; }
}
internal sealed class ApiPlanProcedureWriteResult
{
    public ApiPlanProcedureWriteResult(int plannedProcedures, int reencounteredSdts, int createdProcedures, int reencounteredProcedures, string transactionFolderName, Guid transactionFolderGuid, IReadOnlyList<ApiPlanProcedureWriteItemResult> items)
    {
        PlannedProcedures = plannedProcedures;
        ReencounteredSdts = reencounteredSdts;
        CreatedProcedures = createdProcedures;
        ReencounteredProcedures = reencounteredProcedures;
        TransactionFolderName = transactionFolderName ?? throw new ArgumentNullException(nameof(transactionFolderName));
        TransactionFolderGuid = transactionFolderGuid;
        Items = items ?? throw new ArgumentNullException(nameof(items));
    }

    public int PlannedProcedures { get; }

    public int ReencounteredSdts { get; }

    public int CreatedProcedures { get; }

    public int ReencounteredProcedures { get; }

    public string TransactionFolderName { get; }

    public Guid TransactionFolderGuid { get; }

    public IReadOnlyList<ApiPlanProcedureWriteItemResult> Items { get; }
}

internal sealed class ApiPlanProcedureWriteItemResult
{
    public ApiPlanProcedureWriteItemResult(string backlogId, string serviceName, string name, string status, Guid guid)
    {
        BacklogId = backlogId ?? throw new ArgumentNullException(nameof(backlogId));
        ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Status = status ?? throw new ArgumentNullException(nameof(status));
        Guid = guid;
    }

    public string BacklogId { get; }

    public string ServiceName { get; }

    public string Name { get; }

    public string Status { get; }

    public Guid Guid { get; }
}
