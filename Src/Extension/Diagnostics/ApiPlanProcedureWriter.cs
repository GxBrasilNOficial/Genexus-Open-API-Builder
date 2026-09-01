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
    public static ApiPlanProcedureWriteResult CreateOrReencounter(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        ApiPlanBusyProgressSession? progress = null,
        ApiPlanKbObjectNameIndex? kbIndex = null)
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
        progress?.Report("Procedures", 0, 0, "Preflight");
        progress?.PumpAndThrowIfAbortRequested();
        kbIndex ??= ApiPlanKbObjectNameIndex.Create(designModel, progress);
        var resolvedSdts = PreflightRequiredSdts(sdtGenerationPlan, kbIndex, progress);
        var definitions = CreateProcedureDefinitions(apiPlan);
        progress?.PumpAndThrowIfAbortRequested();
        var preflight = PreflightProcedures(designModel, definitions);
        progress?.PumpAndThrowIfAbortRequested();
        var transactionFolder = ApiPlanTransactionFolder.CreateOrReencounter(designModel, transaction, apiPlan);
        var results = new List<ApiPlanProcedureWriteItemResult>();
        var current = 0;

        foreach (var definition in definitions)
        {
            progress?.ThrowIfAbortRequested();
            current++;
            progress?.Report("Procedures", current, definitions.Count, definition.Name);
            progress?.Pump();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var item = CreateOrReencounterProcedure(designModel, transaction, transactionFolder, definition, preflight, progress);
            sw.Stop();
            progress?.Report("Procedures", current, definitions.Count, definition.Name, sw.ElapsedMilliseconds);
            results.Add(item);
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

    private static IReadOnlyList<Guid> PreflightRequiredSdts(
        ApiPlanSdtGenerationPlan generationPlan,
        ApiPlanKbObjectNameIndex kbIndex,
        ApiPlanBusyProgressSession? progress = null)
    {
        var definitions = generationPlan.SharedSdts.Concat(generationPlan.OwnSdts).ToArray();
        var resolved = new List<Guid>();
        var index = 0;
        foreach (var definition in definitions)
        {
            index++;
            progress?.Report("Procedures", index, definitions.Length, $"SDT {definition.Name}");
            progress?.PumpAndThrowIfAbortRequested();
            var existingCount = kbIndex.GetSdtCount(definition.Name);

            if (existingCount == 0)
            {
                throw new InvalidOperationException($"Criacao de Procedures bloqueada: SDT requerido nao foi reencontrado: '{definition.Name}'. Gere os SDTs pelo Wizard antes. Nenhuma alteracao foi feita.");
            }

            if (existingCount > 1)
            {
                throw new InvalidOperationException($"Criacao de Procedures bloqueada: foram encontrados {existingCount} SDTs chamados '{definition.Name}'. Nenhuma alteracao foi feita.");
            }

            if (!kbIndex.TryGetSingleSdt(definition.Name, out var sdt))
            {
                throw new InvalidOperationException($"Criacao de Procedures bloqueada: SDT requerido nao foi reencontrado: '{definition.Name}'. Gere os SDTs pelo Wizard antes. Nenhuma alteracao foi feita.");
            }

            if (!ApiPlanOwnedObjectDescription.IsOwnedSdt(sdt.Description, definition.Name))
            {
                throw new InvalidOperationException($"Criacao de Procedures bloqueada: SDT requerido externo ou incompativel chamado '{definition.Name}'. Gere ou reencontre os SDTs pelo Wizard antes. Nenhuma alteracao foi feita.");
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

        if (string.Equals(serviceName, "Delete", StringComparison.OrdinalIgnoreCase))
        {
            return "B100";
        }

        return "B050-B053";
    }

    internal static string FormatOutputStage(ApiPlan apiPlan)
    {
        if (apiPlan?.Services is null)
        {
            return "B050-B053";
        }

        return apiPlan.Services.Any(service => string.Equals(service.Name, "Delete", StringComparison.OrdinalIgnoreCase))
            ? "B050-B053/B100"
            : "B050-B053";
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

            if (existing.Length == 1)
            {
                var existingProcedure = existing[0];
                if (!ApiPlanOwnedObjectDescription.IsOwnedProcedure(existingProcedure.Description, definition.Name))
                {
                    throw new InvalidOperationException($"Criacao de Procedure bloqueada: ja existe Procedure externa ou incompativel chamada '{definition.Name}'. Nenhuma alteracao foi feita.");
                }

                existingByName.Add(definition.Name, existingProcedure);
            }
        }

        return new ApiPlanProcedurePreflightResult(existingByName);
    }

    private static ApiPlanProcedureWriteItemResult CreateOrReencounterProcedure(
        KBModel designModel,
        Transaction transaction,
        Folder transactionFolder,
        ApiPlanProcedureDefinition definition,
        ApiPlanProcedurePreflightResult preflight,
        ApiPlanBusyProgressSession? progress = null)
    {
        if (preflight.ExistingProceduresByName.TryGetValue(definition.Name, out var existingProcedure))
        {
            existingProcedure.Parent = transactionFolder;
            progress?.PumpAndThrowIfAbortRequested();
            existingProcedure.Save();
            return new ApiPlanProcedureWriteItemResult(definition.BacklogId, definition.ServiceName, definition.Name, ApiPlanProcedureWriteStatus.Reencountered, existingProcedure.Guid);
        }

        var procedure = new Procedure(designModel)
        {
            Name = definition.Name,
            Description = ApiPlanOwnedObjectDescription.Create(definition.Name),
        };

        procedure.Parent = transactionFolder;

        ConfigureProcedure(procedure, definition);
        progress?.PumpAndThrowIfAbortRequested();
        procedure.Save();

        var persisted = Procedure.Get(designModel, procedure.Guid);
        return new ApiPlanProcedureWriteItemResult(definition.BacklogId, definition.ServiceName, definition.Name, ApiPlanProcedureWriteStatus.Created, persisted.Guid);
    }

    private static void ConfigureProcedure(Procedure procedure, ApiPlanProcedureDefinition definition)
    {
        procedure.ProcedurePart.Source =
            $"// Genexus Open API Builder: Procedure skeleton for {definition.ServiceName}.";
        procedure.Rules.Source = string.Empty;
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
