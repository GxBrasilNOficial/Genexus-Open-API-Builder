using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;
using Artech.Genexus.Common.Wiki;
using GenexusOpenApiBuilder.Extension.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

internal static class ApiPlanApiObjectWriter
{
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

        // Description do objeto API (também publicada em info.description pelo gerador GeneXus).
        return ApiPlanOwnedObjectDescription.CreateApiObjectDescription(apiPlan.ApiName);
    }

    /// <summary>
    /// Formas aceitas no fallback B087 e na integridade: canônica atual e legados sem IDs de backlog.
    /// </summary>
    internal static IReadOnlyList<string> CreateOwnedDescriptionCandidates(ApiPlan apiPlan)
    {
        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        return ApiPlanOwnedObjectDescription.CreateApiObjectDescriptionFallbacks(apiPlan.ApiName, apiPlan.TransactionName);
    }

    /// <summary>
    /// B087: reconhece API Object próprio pela metadata; Description só como fallback sem File.
    /// </summary>
    internal static bool IsOwnedApiObject(KBModel designModel, ApiPlan apiPlan, API apiObject)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        if (apiObject is null)
        {
            throw new ArgumentNullException(nameof(apiObject));
        }

        var kind = ResolveOwnership(designModel, apiPlan, apiObject);
        return ApiPlanApiObjectOwnership.IsOwned(kind);
    }

    /// <summary>
    /// B085: posse para sincronização intencional — apenas ownership da metadata
    /// (schema + apiName + apiGuid). Não exige IsManagedApiObject contra o ApiPlan novo:
    /// o Sync regrava Service Source/variáveis de propósito e o Source atual pode divergir
    /// do plano reconstruído (campos novos, ordem de filtros, etc.).
    /// </summary>
    internal static bool IsOwnedApiObjectForSync(KBModel designModel, ApiPlan apiPlan, API apiObject)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        if (apiObject is null)
        {
            throw new ArgumentNullException(nameof(apiObject));
        }

        var metadataLookup = TryFindOwnedMetadataFile(designModel, apiPlan);
        if (metadataLookup.Ambiguous || !metadataLookup.OwnedFilePresent)
        {
            return false;
        }

        if (!TryParseMetadata(metadataLookup.File!, out var metadata) || metadata is null)
        {
            return false;
        }

        return ApiPlanApiObjectOwnership.MatchesMetadataOwnership(
            metadata,
            ApiPlanMetadataFileWriter.SchemaVersion,
            apiPlan.ApiName,
            apiObject.Guid.ToString());
    }

    internal static ApiPlanApiObjectOwnership.OwnershipKind ResolveOwnership(KBModel designModel, ApiPlan apiPlan, API apiObject)
    {
        var serviceSourceManaged = ApiPlanBusinessComponentWriter.IsManagedApiObject(designModel, apiPlan, apiObject);
        var metadataLookup = TryFindOwnedMetadataFile(designModel, apiPlan);
        if (metadataLookup.Ambiguous)
        {
            return ApiPlanApiObjectOwnership.OwnershipKind.NotOwned;
        }

        JObject? metadata = null;
        var integrityCompatible = false;
        if (metadataLookup.OwnedFilePresent)
        {
            if (!TryParseMetadata(metadataLookup.File!, out metadata) || metadata is null)
            {
                return ApiPlanApiObjectOwnership.OwnershipKind.NotOwned;
            }

            integrityCompatible = ApiPlanMetadataFileWriter.HasCompatibleB067Integrity(metadata, apiPlan, apiObject);
        }

        return ApiPlanApiObjectOwnership.Resolve(
            metadataLookup.OwnedFilePresent,
            metadata,
            ApiPlanMetadataFileWriter.SchemaVersion,
            apiPlan.ApiName,
            apiObject.Guid.ToString(),
            integrityCompatible,
            serviceSourceManaged,
            apiObject.Description,
            CreateOwnedDescriptionCandidates(apiPlan));
    }

    private static (bool OwnedFilePresent, bool Ambiguous, WikiFileKBObject? File) TryFindOwnedMetadataFile(KBModel designModel, ApiPlan apiPlan)
    {
        var matches = WikiFileKBObject.GetAll(designModel)
            .Where(file => string.Equals(file.Name, apiPlan.MetadataFileName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matches.Length > 1)
        {
            return (false, true, null);
        }

        if (matches.Length == 0)
        {
            return (false, false, null);
        }

        var file = matches[0];
        if (!ApiPlanOwnedObjectDescription.IsOwnedMetadataFile(file.Description, apiPlan.MetadataFileName, apiPlan.TransactionName))
        {
            return (false, false, null);
        }

        return (true, false, file);
    }

    private static bool TryParseMetadata(WikiFileKBObject file, out JObject? metadata)
    {
        metadata = null;
        var bytes = file.BlobPart?.Data?.GetBytes();
        if (bytes is null || bytes.Length == 0)
        {
            return false;
        }

        try
        {
            metadata = JObject.Parse(Encoding.UTF8.GetString(bytes));
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
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

            var sdt = matches[0];
            if (!ApiPlanOwnedObjectDescription.IsOwnedSdt(sdt.Description, definition.Name))
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

            var procedure = matches[0];
            if (!ApiPlanOwnedObjectDescription.IsOwnedProcedure(procedure.Description, definition.Name))
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
        if (!IsOwnedApiObject(designModel, apiPlan, apiObject))
        {
            throw new InvalidOperationException($"Criacao de API Object bloqueada: ja existe API Object externo ou incompativel chamado '{apiPlan.ApiName}'. Nenhuma alteracao foi feita.");
        }

        return new ApiPlanApiObjectPreflightResult(apiObject);
    }


    private static ApiPlanApiObjectWriteCoreResult CreateOrReencounterApiObject(KBModel designModel, Folder transactionFolder, ApiPlan apiPlan, ApiPlanApiObjectPreflightResult preflight)
    {
        if (preflight.ExistingApiObject is not null)
        {
            preflight.ExistingApiObject.Parent = transactionFolder;
            if (ApiPlanBusinessComponentWriter.IsB055ApiObject(designModel, apiPlan, preflight.ExistingApiObject))
            {
                if (!ApiPlanBusinessComponentWriter.IsCurrentB055ApiObject(designModel, apiPlan, preflight.ExistingApiObject))
                {
                    preflight.ExistingApiObject.ServiceGroupSource.Source = ApiPlanBusinessComponentWriter.CreateB055ServiceGroupSource(apiPlan);
                }
            }
            else
            {
                preflight.ExistingApiObject.ServiceGroupSource.Source = ApiPlanBusinessComponentWriter.CreateB054ServiceGroupSource(apiPlan);
            }

            preflight.ExistingApiObject.Save();
            return new ApiPlanApiObjectWriteCoreResult(ApiPlanApiObjectWriteStatus.Reencountered, preflight.ExistingApiObject.Guid);
        }

        var apiObject = API.Create(designModel);
        apiObject.Name = apiPlan.ApiName;
        // Description inicial e documentacao publica; B087 nao a usa mais como cadeado de posse apos metadata.
        apiObject.Description = CreateOwnedDescription(apiPlan);
        apiObject.Parent = transactionFolder;
        apiObject.ServiceGroupSource.Source = ApiPlanBusinessComponentWriter.CreateB054ServiceGroupSource(apiPlan);

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
