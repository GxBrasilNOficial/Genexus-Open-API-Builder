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
    public static ApiPlanApiObjectWriteResult CreateOrReencounter(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        bool allowIntentionalContractRefresh,
        ApiPlanKbObjectNameIndex kbIndex,
        ApiPlanBusyProgressSession? progress = null,
        System.Action<Guid>? onApiSaveCompleted = null,
        System.Action<Guid>? onApiPhysicalSave = null,
        System.Action? onApiSaveAttempted = null,
        ApiPlanPersistenceLog? persistenceLog = null)
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

        if (kbIndex is null)
        {
            throw new ArgumentNullException(nameof(kbIndex));
        }

        ApiPlanWritePreflight.ValidateTransactionIdentity(transaction, apiPlan, "Criacao de API Object");

        progress?.PumpAndThrowIfAbortRequested();
        var reencounteredSdts = PreflightRequiredSdts(designModel, apiPlan, kbIndex);
        var reencounteredProcedures = PreflightRequiredProcedures(designModel, apiPlan);
        var context = PrepareOrReencounter(
            designModel,
            transaction,
            apiPlan,
            allowIntentionalContractRefresh,
            kbIndex,
            persistApiObject: true,
            businessComponentParticipated: false,
            finalWriter: "B054",
            progress: progress);
        var result = SavePreparedApiObject(
            designModel,
            context,
            apiPlan,
            kbIndex,
            onApiSaveCompleted,
            onApiPhysicalSave,
            onApiSaveAttempted);

        return new ApiPlanApiObjectWriteResult(
            apiPlan.ApiName,
            result.Status,
            result.Guid,
            reencounteredSdts.Count,
            reencounteredProcedures.Count,
            context.TransactionFolder.Name,
            context.TransactionFolder.Guid,
            reencounteredProcedures,
            apiPlan.Services.Count);
    }

    internal static ApiPlanTransientApiContext PrepareOrReencounter(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        bool allowIntentionalContractRefresh,
        ApiPlanKbObjectNameIndex kbIndex,
        bool persistApiObject,
        bool businessComponentParticipated,
        string finalWriter,
        ApiPlanTransientApiSelection? selection = null,
        IReadOnlyCollection<string>? preserveSdtNames = null,
        ApiPlanBusyProgressSession? progress = null)
    {
        if (designModel is null) throw new ArgumentNullException(nameof(designModel));
        if (transaction is null) throw new ArgumentNullException(nameof(transaction));
        if (apiPlan is null) throw new ArgumentNullException(nameof(apiPlan));
        if (kbIndex is null) throw new ArgumentNullException(nameof(kbIndex));
        if (string.IsNullOrWhiteSpace(finalWriter)) throw new ArgumentException("Final writer e obrigatorio.", nameof(finalWriter));
        ApiPlanWritePreflight.ValidateTransactionIdentity(transaction, apiPlan, "Preparacao de API Object");

        progress?.PumpAndThrowIfAbortRequested();
        // Consumidor nunca cria ou corrige dependencias. A validacao de estrutura
        // ocorre antes de qualquer Save de Procedure/API.
        ApiPlanSdtWriter.PreflightStrict(designModel, transaction, apiPlan, kbIndex, preserveSdtNames);
        PreflightRequiredSdts(designModel, apiPlan, kbIndex);
        PreflightRequiredProcedures(designModel, apiPlan);
        var preflight = PreflightApiObject(designModel, apiPlan, allowIntentionalContractRefresh, kbIndex);
        var transactionFolder = ApiPlanTransactionFolder.GetOrReencounterStrict(designModel, transaction, apiPlan);

        API api;
        var apiWasCreated = preflight.ExistingApiObject is null;
        if (preflight.ExistingApiObject is not null)
        {
            api = preflight.ExistingApiObject;
        }
        else
        {
            api = API.Create(designModel);
            api.Name = apiPlan.ApiName;
            api.Description = CreateOwnedDescription(apiPlan);
            api.Parent = transactionFolder;
            api.ServiceGroupSource.Source = ApiPlanBusinessComponentWriter.CreateB054ServiceGroupSource(apiPlan);
        }

        if (api.Guid == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Preparacao de API Object bloqueada: o API Object '{apiPlan.ApiName}' nao recebeu GUID estavel do SDK. Nenhuma alteracao foi feita.");
        }

        var existingApiHasBusinessComponentParameters = !apiWasCreated &&
            (ApiPlanBusinessComponentWriter.IsB055ApiObject(designModel, kbIndex, apiPlan, api) ||
             ApiPlanListProcedureWriter.IsB070ApiObjectWithBusinessComponentParameters(designModel, kbIndex, apiPlan, api));
        apiPlan.PlannedApiGuid = api.Guid;
        return new ApiPlanTransientApiContext(
            api,
            transactionFolder,
            persistApiObject,
            businessComponentParticipated,
            apiWasCreated,
            existingApiHasBusinessComponentParameters,
            finalWriter,
            apiPlan,
            selection);
    }

    internal static void PreflightRequiredProceduresStrict(KBModel designModel, ApiPlan apiPlan)
    {
        if (designModel is null) throw new ArgumentNullException(nameof(designModel));
        if (apiPlan is null) throw new ArgumentNullException(nameof(apiPlan));
        PreflightRequiredProcedures(designModel, apiPlan);
    }

    /// <summary>
    /// B060 posterior ao Save intencional do API Object: confirma identidade e
    /// posse, mas nao exige que o Source atual ainda corresponda ao baseline
    /// anterior. O baseline ja foi validado por PreflightExistingApiObjectStrict
    /// antes do primeiro Save; aqui o contrato novo sera materializado na
    /// metadata desta mesma operacao.
    /// </summary>
    internal static API PreflightExistingApiObjectForMetadataRefresh(
        KBModel designModel,
        ApiPlan apiPlan,
        ApiPlanKbObjectNameIndex kbIndex)
    {
        if (designModel is null) throw new ArgumentNullException(nameof(designModel));
        if (apiPlan is null) throw new ArgumentNullException(nameof(apiPlan));
        if (kbIndex is null) throw new ArgumentNullException(nameof(kbIndex));

        var apiObject = RequirePlannedApiObject(designModel, apiPlan);
        if (!IsOwnedApiObjectForIntentionalWrite(designModel, kbIndex, apiPlan, apiObject))
        {
            throw new InvalidOperationException(
                $"API Object com GUID planejado '{apiObject.Guid}' e nome '{apiPlan.ApiName}' " +
                "e externo ou incompativel. Nenhuma alteracao foi feita.");
        }

        return apiObject;
    }

    internal static API PreflightExistingApiObjectStrict(
        KBModel designModel,
        ApiPlan apiPlan,
        bool allowIntentionalContractRefresh,
        ApiPlanKbObjectNameIndex kbIndex)
    {
        if (designModel is null) throw new ArgumentNullException(nameof(designModel));
        if (apiPlan is null) throw new ArgumentNullException(nameof(apiPlan));
        if (kbIndex is null) throw new ArgumentNullException(nameof(kbIndex));

        var apiObject = RequirePlannedApiObject(designModel, apiPlan);
        var owned = allowIntentionalContractRefresh
            ? IsOwnedApiObjectForIntentionalChange(designModel, kbIndex, apiPlan, apiObject)
            : IsOwnedApiObject(designModel, kbIndex, apiPlan, apiObject);
        if (!owned)
        {
            throw new InvalidOperationException(
                $"API Object com GUID planejado '{apiObject.Guid}' e nome '{apiPlan.ApiName}' " +
                "e externo ou incompativel. Nenhuma alteracao foi feita.");
        }

        return apiObject;
    }

    private static API RequirePlannedApiObject(KBModel designModel, ApiPlan apiPlan)
    {
        if (!apiPlan.PlannedApiGuid.HasValue || apiPlan.PlannedApiGuid.Value == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"API Object '{apiPlan.ApiName}' nao foi associado por GUID planejado. " +
                "A associacao por nome nao autoriza a gravacao de consumidores. Nenhuma alteracao foi feita.");
        }

        var apiObject = API.Get(designModel, apiPlan.PlannedApiGuid.Value);
        if (apiObject is null)
        {
            throw new InvalidOperationException(
                $"API Object com GUID planejado '{apiPlan.PlannedApiGuid.Value}' nao foi reencontrado. " +
                "Nenhuma alteracao foi feita.");
        }

        if (apiObject.Guid == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"API Object com GUID planejado '{apiPlan.PlannedApiGuid.Value}' foi reencontrado sem GUID persistido valido. Nenhuma alteracao foi feita.");
        }

        var resolution = ApiPlanMainObjectResolver.Resolve(
            apiPlan.PlannedApiGuid,
            apiPlan.ApiName,
            new ApiPlanMainObjectCandidate(apiObject.Guid, apiObject.Name),
            Array.Empty<ApiPlanMainObjectCandidate>());
        if (!resolution.IsConfirmed)
        {
            throw new InvalidOperationException(
                $"API Object bloqueado: {resolution.Diagnostic} Nenhuma alteracao foi feita.");
        }
        return apiObject;
    }

    /// <summary>
    /// B111/F1: valida o reencontro e a compatibilidade do caminho B054
    /// antes das fases que podem gravar SDTs ou Procedures. A defesa no
    /// SavePreparedApiObject permanece necessaria para chamadas fora do
    /// preflight agregado.
    /// </summary>
    internal static void PreflightB054ApiObjectStrict(
        KBModel designModel,
        ApiPlan apiPlan,
        bool allowIntentionalContractRefresh,
        ApiPlanKbObjectNameIndex kbIndex)
    {
        if (designModel is null) throw new ArgumentNullException(nameof(designModel));
        if (apiPlan is null) throw new ArgumentNullException(nameof(apiPlan));
        if (kbIndex is null) throw new ArgumentNullException(nameof(kbIndex));

        var existingApiObject = PreflightApiObject(
            designModel,
            apiPlan,
            allowIntentionalContractRefresh,
            kbIndex).ExistingApiObject;
        if (existingApiObject is null ||
            ApiPlanBusinessComponentWriter.IsB055ApiObject(designModel, kbIndex, apiPlan, existingApiObject))
        {
            return;
        }

        ApiPlanServiceSourceContract.ThrowIfB054WouldDowngradeRestContract(existingApiObject.ServiceGroupSource.Source);
    }

    internal static ApiPlanApiObjectWriteCoreResult SavePreparedApiObject(
        KBModel designModel,
        ApiPlanTransientApiContext context,
        ApiPlan apiPlan,
        ApiPlanKbObjectNameIndex kbIndex,
        System.Action<Guid>? onApiSaveCompleted = null,
        System.Action<Guid>? onApiPhysicalSave = null,
        System.Action? onApiSaveAttempted = null)
    {
        if (designModel is null) throw new ArgumentNullException(nameof(designModel));
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (apiPlan is null) throw new ArgumentNullException(nameof(apiPlan));
        if (kbIndex is null) throw new ArgumentNullException(nameof(kbIndex));
        if (!context.PersistApiObject)
        {
            throw new InvalidOperationException("Save do API Object solicitado para um contexto que nao permite persistencia.");
        }

        if (context.PlannedApiGuid == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Save de API Object bloqueado: o API Object '{apiPlan.ApiName}' nao possui GUID planejado estavel. Nenhuma alteracao foi feita.");
        }

        var api = context.Api;
        api.Parent = context.TransactionFolder;
        var isB055ApiObject = ApiPlanBusinessComponentWriter.IsB055ApiObject(designModel, kbIndex, apiPlan, api);
        if (isB055ApiObject)
        {
            if (!ApiPlanBusinessComponentWriter.IsCurrentB055ApiObject(designModel, kbIndex, apiPlan, api))
            {
                api.ServiceGroupSource.Source = ApiPlanBusinessComponentWriter.CreateB055ServiceGroupSource(apiPlan);
            }
        }
        else
        {
            ApiPlanServiceSourceContract.ThrowIfB054WouldDowngradeRestContract(api.ServiceGroupSource.Source);
            api.ServiceGroupSource.Source = ApiPlanBusinessComponentWriter.CreateB054ServiceGroupSource(apiPlan);
        }

        onApiSaveAttempted?.Invoke();
        var receipt = ApiPlanSaveBoundaryProbe.Persist(
            PersistenceFaultPoint.ApiSave,
            "Save",
            "API",
            isB055ApiObject ? "B055" : "B054",
            api.Name,
            new GuidIdentity(api.Guid),
            () =>
            {
                api.Save();
                onApiPhysicalSave?.Invoke(api.Guid);
            },
            () => ConfirmApiObject(designModel, api, apiPlan, isB055ApiObject));
        if (receipt is not null && receipt.Outcome != PersistenceOutcome.Confirmed)
        {
            throw new InvalidOperationException(
                $"Persistência do API Object '{api.Name}' não foi confirmada: Outcome='{receipt.Outcome}', Confirmation='{receipt.Confirmation}', Detail='{receipt.ConfirmationDetail}'.");
        }

        var persisted = RequirePersistedApiObject(designModel, api.Guid, apiPlan.ApiName, isB055ApiObject ? "B055" : "B054");
        var persistedSource = ApiPlanBusinessComponentWriter.NormalizeForComparison(persisted.ServiceGroupSource.Source);
        var sourceMatchesContract = isB055ApiObject
            ? ApiPlanBusinessComponentWriter.IsB055ServiceGroupSource(apiPlan, persistedSource)
            : ApiPlanBusinessComponentWriter.IsB054ServiceGroupSource(apiPlan, persistedSource);
        if (!sourceMatchesContract)
        {
            throw new InvalidOperationException(
                $"{(isB055ApiObject ? "B055" : "B054")} bloqueado: o API Object '{apiPlan.ApiName}' foi salvo, mas o Service Source persistido nao corresponde ao contrato planejado. Nenhuma outra alteracao sera feita.");
        }

        onApiSaveCompleted?.Invoke(persisted.Guid);
        return new ApiPlanApiObjectWriteCoreResult(
            context.ApiWasCreated ? ApiPlanApiObjectWriteStatus.Created : ApiPlanApiObjectWriteStatus.Reencountered,
            persisted.Guid);
    }

    private static PersistenceConfirmation ConfirmApiObject(
        KBModel designModel,
        API api,
        ApiPlan apiPlan,
        bool isB055ApiObject)
    {
        try
        {
            var persisted = API.Get(designModel, api.Guid);
            if (persisted is null)
            {
                return PersistenceConfirmation.Absent("API Object não foi reencontrado pelo GUID.");
            }

            if (!string.Equals(persisted.Name, apiPlan.ApiName, StringComparison.OrdinalIgnoreCase))
            {
                return PersistenceConfirmation.Divergent(
                    persisted.Guid.ToString(),
                    $"O API Object persistido tem nome '{persisted.Name}', esperado '{apiPlan.ApiName}'.");
            }

            var persistedSource = ApiPlanBusinessComponentWriter.NormalizeForComparison(persisted.ServiceGroupSource.Source);
            var sourceMatchesContract = isB055ApiObject
                ? ApiPlanBusinessComponentWriter.IsB055ServiceGroupSource(apiPlan, persistedSource)
                : ApiPlanBusinessComponentWriter.IsB054ServiceGroupSource(apiPlan, persistedSource);
            return sourceMatchesContract
                ? PersistenceConfirmation.Confirmed(persisted.Guid.ToString())
                : PersistenceConfirmation.Divergent(
                    persisted.Guid.ToString(),
                    "O Service Source persistido não corresponde ao contrato planejado.");
        }
        catch (Exception exception)
        {
            return PersistenceConfirmation.Unreadable(exception.GetType().FullName + ": " + exception.Message);
        }
    }

    internal static API RequirePersistedApiObject(
        KBModel designModel,
        Guid expectedGuid,
        string expectedName,
        string operationCode)
    {
        if (designModel is null) throw new ArgumentNullException(nameof(designModel));
        if (expectedGuid == Guid.Empty) throw new ArgumentException("O GUID esperado do API Object nao pode ser vazio.", nameof(expectedGuid));
        if (string.IsNullOrWhiteSpace(expectedName)) throw new ArgumentException("O nome esperado do API Object e obrigatorio.", nameof(expectedName));
        if (string.IsNullOrWhiteSpace(operationCode)) throw new ArgumentException("O codigo da operacao e obrigatorio.", nameof(operationCode));

        var persisted = API.Get(designModel, expectedGuid);
        if (persisted is null)
        {
            throw new InvalidOperationException(
                $"{operationCode} bloqueado: o API Object '{expectedName}' nao foi reencontrado apos o Save pelo GUID '{expectedGuid}'. Nenhuma outra alteracao sera feita.");
        }

        if (persisted.Guid == Guid.Empty || persisted.Guid != expectedGuid)
        {
            throw new InvalidOperationException(
                $"{operationCode} bloqueado: o API Object '{expectedName}' retornou GUID persistido invalido apos o Save. Esperado='{expectedGuid}', Persistido='{persisted.Guid}'. Nenhuma outra alteracao sera feita.");
        }

        if (!string.Equals(persisted.Name, expectedName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{operationCode} bloqueado: o API Object persistido pelo GUID '{expectedGuid}' possui nome '{persisted.Name}', mas o esperado era '{expectedName}'. Nenhuma outra alteracao sera feita.");
        }

        return persisted;
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
    internal static bool IsOwnedApiObject(KBModel designModel, ApiPlanKbObjectNameIndex kbIndex, ApiPlan apiPlan, API apiObject)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (kbIndex is null)
        {
            throw new ArgumentNullException(nameof(kbIndex));
        }

        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        if (apiObject is null)
        {
            throw new ArgumentNullException(nameof(apiObject));
        }

        var kind = ResolveOwnership(designModel, kbIndex, apiPlan, apiObject);
        return ApiPlanApiObjectOwnership.IsOwned(kind);
    }

    /// <summary>
    /// B085/B034: posse para uma regravacao intencional — apenas ownership da metadata
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

        var metadataLookup = FindMetadataFile(designModel, apiPlan);
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
            ApiPlanMetadataFileWriter.SupportedSchemaVersions,
            apiPlan.ApiName,
            apiObject.Guid.ToString());
    }

    /// <summary>
    /// B070/B060 confirmados no Wizard: a posse vem da metadata quando o File existe.
    /// Na primeira geracao o File ainda nao foi gravado — o List roda antes do B060 —,
    /// entao a posse cai no fallback historico pela Description e pelo contrato gerenciado.
    /// </summary>
    internal static bool IsOwnedApiObjectForIntentionalWrite(KBModel designModel, ApiPlanKbObjectNameIndex kbIndex, ApiPlan apiPlan, API apiObject)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (kbIndex is null)
        {
            throw new ArgumentNullException(nameof(kbIndex));
        }

        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        if (apiObject is null)
        {
            throw new ArgumentNullException(nameof(apiObject));
        }

        var metadataLookup = FindMetadataFile(designModel, apiPlan);
        switch (ApiPlanApiObjectOwnership.ResolveIntentionalWriteOwnership(
            metadataLookup.Ambiguous,
            metadataLookup.FilePresent))
        {
            case ApiPlanApiObjectOwnership.IntentionalWriteOwnership.DescriptionFallback:
                return IsOwnedApiObject(designModel, kbIndex, apiPlan, apiObject);
            case ApiPlanApiObjectOwnership.IntentionalWriteOwnership.MetadataOwnership:
                return IsOwnedApiObjectForSync(designModel, apiPlan, apiObject);
            default:
                return false;
        }
    }

    /// <summary>
    /// Preflight do Wizard/Sincronizar: a posse continua sendo confirmada
    /// pela metadata, mas o contrato desejado pode ser diferente do ultimo
    /// contrato gerado. A protecao contra edicao direta e feita pelo estado
    /// do baseline antes do primeiro Save().
    /// </summary>
    internal static bool IsOwnedApiObjectForIntentionalChange(KBModel designModel, ApiPlanKbObjectNameIndex kbIndex, ApiPlan apiPlan, API apiObject)
    {
        if (!IsOwnedApiObjectForIntentionalWrite(designModel, kbIndex, apiPlan, apiObject))
        {
            return false;
        }

        var metadataLookup = FindMetadataFile(designModel, apiPlan);
        if (!metadataLookup.FilePresent)
        {
            // Antes da primeira metadata persistida nao existe baseline gravado
            // para comparar; a posse ja foi confirmada pelo fallback historico.
            return true;
        }

        return metadataLookup.File is not null &&
            TryParseMetadata(metadataLookup.File, out var metadata) &&
            metadata is not null &&
            ApiPlanMetadataFileWriter.HasCompatibleGeneratedBaseline(metadata, apiObject);
    }

    internal static ApiPlanIntentionalChangeOwnershipDiagnosis DiagnoseIntentionalChangeOwnership(
        KBModel designModel,
        ApiPlan apiPlan,
        IReadOnlyList<API> matches)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        matches ??= Array.Empty<API>();
        var matchCount = matches.Count;
        if (matchCount == 0)
        {
            return ApiPlanIntentionalChangeOwnershipDiagnosis.NotOwned("ApiObjectMissing", matchCount);
        }

        if (matchCount != 1)
        {
            return ApiPlanIntentionalChangeOwnershipDiagnosis.NotOwned("ApiObjectAmbiguous", matchCount);
        }

        var apiObject = matches[0];
        var metadataLookup = FindMetadataFile(designModel, apiPlan);
        if (metadataLookup.Ambiguous)
        {
            return ApiPlanIntentionalChangeOwnershipDiagnosis.NotOwned(
                "MetadataAmbiguous",
                matchCount,
                apiObject.Guid.ToString());
        }

        if (!metadataLookup.FilePresent)
        {
            return ApiPlanIntentionalChangeOwnershipDiagnosis.NotOwned(
                "MetadataMissing",
                matchCount,
                apiObject.Guid.ToString());
        }

        if (!metadataLookup.OwnedFilePresent)
        {
            return ApiPlanIntentionalChangeOwnershipDiagnosis.NotOwned(
                "MetadataDescriptionNotOwned",
                matchCount,
                apiObject.Guid.ToString(),
                metadataPresent: true,
                metadataDescriptionOwned: false);
        }

        if (!TryParseMetadata(metadataLookup.File!, out var metadata) || metadata is null)
        {
            return ApiPlanIntentionalChangeOwnershipDiagnosis.NotOwned(
                "MetadataUnreadable",
                matchCount,
                apiObject.Guid.ToString(),
                metadataPresent: true,
                metadataDescriptionOwned: true,
                metadataParseOk: false);
        }

        var storedApiName = metadata.SelectToken("ownership.apiName")?.Value<string>() ?? string.Empty;
        var storedApiGuid = metadata.SelectToken("ownership.apiGuid")?.Value<string>() ?? string.Empty;
        var storedSchema = metadata["schemaVersion"]?.Value<string>() ?? string.Empty;
        var ownershipOk = ApiPlanApiObjectOwnership.MatchesMetadataOwnership(
            metadata,
            ApiPlanMetadataFileWriter.SupportedSchemaVersions,
            apiPlan.ApiName,
            apiObject.Guid.ToString());
        if (!ownershipOk)
        {
            return ApiPlanIntentionalChangeOwnershipDiagnosis.NotOwned(
                "OwnershipSchemaApiNameOrGuidMismatch",
                matchCount,
                apiObject.Guid.ToString(),
                storedApiGuid,
                metadataPresent: true,
                metadataDescriptionOwned: true,
                metadataParseOk: true,
                ownershipOk: false,
                storedSchema: storedSchema,
                storedApiName: storedApiName,
                expectedApiName: apiPlan.ApiName);
        }

        var fingerprint = ApiPlanMetadataIntegrity.DiagnoseMetadataFingerprint(metadata);
        var integrity = metadata["integrity"] as JObject;
        var integrityPresent = integrity is not null;
        var serviceNames = ((JArray?)integrity?.SelectToken("generatedDescriptions.services"))
            ?.Select(item => item["serviceName"]?.Value<string>() ?? string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();
        GeneratedBaselineDiagnosis? baseline = null;
        if (serviceNames is not null && serviceNames.Length > 0)
        {
            var source = apiObject.ServiceGroupSource?.Source ?? string.Empty;
            baseline = ApiPlanMetadataIntegrity.DiagnoseGeneratedBaseline(
                metadata,
                ApiPlanMetadataIntegrity.ComputeJsonSha256(
                    ApiPlanMetadataIntegrity.CreateServiceDescriptionsContractFromSource(source, serviceNames)),
                ApiPlanMetadataIntegrity.ComputeNormalizedTextSha256(source),
                apiObject.Description ?? string.Empty,
                apiObject.Guid.ToString());
        }

        if (!fingerprint.IsCompatible)
        {
            return ApiPlanIntentionalChangeOwnershipDiagnosis.FromInspectedMetadata(
                isOwned: false,
                failingClause: "MetadataFingerprintMismatch",
                matchCount,
                apiObject.Guid.ToString(),
                storedApiGuid,
                storedSchema,
                storedApiName,
                apiPlan.ApiName,
                fingerprint,
                integrityPresent,
                baseline);
        }

        if (!integrityPresent)
        {
            return ApiPlanIntentionalChangeOwnershipDiagnosis.FromInspectedMetadata(
                isOwned: true,
                failingClause: "None",
                matchCount,
                apiObject.Guid.ToString(),
                storedApiGuid,
                storedSchema,
                storedApiName,
                apiPlan.ApiName,
                fingerprint,
                integrityPresent: false,
                baseline: null);
        }

        if (serviceNames is null || serviceNames.Length == 0)
        {
            return ApiPlanIntentionalChangeOwnershipDiagnosis.FromInspectedMetadata(
                isOwned: false,
                failingClause: "IntegrityServiceNamesMissing",
                matchCount,
                apiObject.Guid.ToString(),
                storedApiGuid,
                storedSchema,
                storedApiName,
                apiPlan.ApiName,
                fingerprint,
                integrityPresent: true,
                baseline: null);
        }

        if (baseline is null)
        {
            throw new InvalidOperationException("Diagnostico de posse do API Object: baseline B067 ausente apos validar integrity e nomes de servico.");
        }

        return ApiPlanIntentionalChangeOwnershipDiagnosis.FromInspectedMetadata(
            isOwned: baseline.IsCompatible,
            failingClause: baseline.IsCompatible ? "None" : baseline.FailingClause,
            matchCount,
            baseline.ActualGuid,
            baseline.StoredGuid,
            storedSchema,
            storedApiName,
            apiPlan.ApiName,
            fingerprint,
            integrityPresent: baseline.IntegrityPresent,
            baseline);
    }

    internal static ApiPlanApiObjectOwnership.OwnershipKind ResolveOwnership(KBModel designModel, ApiPlanKbObjectNameIndex kbIndex, ApiPlan apiPlan, API apiObject)
    {
        return DiagnoseOwnership(designModel, kbIndex, apiPlan, apiObject).OwnershipKind;
    }

    internal static ApiPlanApiObjectOwnership.Diagnostic DiagnoseOwnership(KBModel designModel, ApiPlanKbObjectNameIndex kbIndex, ApiPlan apiPlan, API apiObject)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (kbIndex is null)
        {
            throw new ArgumentNullException(nameof(kbIndex));
        }

        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        if (apiObject is null)
        {
            throw new ArgumentNullException(nameof(apiObject));
        }

        var serviceSourceManaged = ApiPlanBusinessComponentWriter.IsManagedApiObject(designModel, kbIndex, apiPlan, apiObject);
        var metadataLookup = FindMetadataFile(designModel, apiPlan);
        JObject? metadata = null;
        var metadataParsed = false;
        var metadataOwnershipMatches = false;
        var integrityCompatible = false;
        string? metadataApiGuid = null;

        if (metadataLookup.File is not null && TryParseMetadata(metadataLookup.File, out metadata) && metadata is not null)
        {
            metadataParsed = true;
            metadataApiGuid = metadata.SelectToken("ownership.apiGuid")?.Value<string>();
            if (metadataLookup.OwnedFilePresent)
            {
                metadataOwnershipMatches = ApiPlanApiObjectOwnership.MatchesMetadataOwnership(
                    metadata,
                    ApiPlanMetadataFileWriter.SupportedSchemaVersions,
                    apiPlan.ApiName,
                    apiObject.Guid.ToString());
                if (metadataOwnershipMatches)
                {
                    integrityCompatible = ApiPlanMetadataFileWriter.HasCompatibleB067Integrity(metadata, kbIndex, apiPlan, apiObject);
                }
            }
        }

        var descriptionFallbackMatches = CreateOwnedDescriptionCandidates(apiPlan)
            .Any(expected => string.Equals(apiObject.Description, expected, StringComparison.Ordinal));

        return ApiPlanApiObjectOwnership.Diagnose(
            metadataLookup.FilePresent,
            metadataLookup.DescriptionOwned,
            metadataLookup.Ambiguous,
            metadataParsed,
            metadataOwnershipMatches,
            integrityCompatible,
            serviceSourceManaged,
            descriptionFallbackMatches,
            apiObject.Guid.ToString(),
            metadataApiGuid);
    }

    private static MetadataLookupResult FindMetadataFile(KBModel designModel, ApiPlan apiPlan)
    {
        var matches = WikiFileKBObject.GetAll(designModel)
            .Where(file => string.Equals(file.Name, apiPlan.MetadataFileName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matches.Length > 1)
        {
            return new MetadataLookupResult(true, true, false, false, null);
        }

        if (matches.Length == 0)
        {
            return new MetadataLookupResult(false, false, false, false, null);
        }

        var file = matches[0];
        var descriptionOwned = ApiPlanOwnedObjectDescription.IsOwnedMetadataFile(file.Description, apiPlan.MetadataFileName, apiPlan.TransactionName);
        return new MetadataLookupResult(true, false, descriptionOwned, descriptionOwned, file);
    }

    private sealed class MetadataLookupResult
    {
        public MetadataLookupResult(
            bool filePresent,
            bool ambiguous,
            bool descriptionOwned,
            bool ownedFilePresent,
            WikiFileKBObject? file)
        {
            FilePresent = filePresent;
            Ambiguous = ambiguous;
            DescriptionOwned = descriptionOwned;
            OwnedFilePresent = ownedFilePresent;
            File = file;
        }

        public bool FilePresent { get; }

        public bool Ambiguous { get; }

        public bool DescriptionOwned { get; }

        public bool OwnedFilePresent { get; }

        public WikiFileKBObject? File { get; }
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
            metadata = ApiPlanMetadataIntegrity.ParseMetadataBytes(bytes);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static IReadOnlyList<Guid> PreflightRequiredSdts(
        KBModel designModel,
        ApiPlan apiPlan,
        ApiPlanKbObjectNameIndex kbIndex)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (kbIndex is null)
        {
            throw new ArgumentNullException(nameof(kbIndex));
        }

        var generationPlan = ApiPlanSdtGenerationPlanBuilder.Create(apiPlan);
        var resolved = new List<Guid>();
        foreach (var definition in generationPlan.SharedSdts.Concat(generationPlan.OwnSdts))
        {
            var matches = kbIndex.FindSdts(definition.Name);

            if (matches.Count == 0)
            {
                throw new InvalidOperationException($"Criacao de API Object bloqueada: SDT requerido nao foi reencontrado: '{definition.Name}'. Gere os SDTs pelo Wizard antes. Nenhuma alteracao foi feita.");
            }

            if (matches.Count > 1)
            {
                throw new InvalidOperationException($"Criacao de API Object bloqueada: foram encontrados {matches.Count} SDTs chamados '{definition.Name}'. Nenhuma alteracao foi feita.");
            }

            var sdt = matches[0];
            if (!ApiPlanOwnedObjectDescription.IsOwnedSdt(sdt.Description, definition.Name))
            {
                throw new InvalidOperationException($"Criacao de API Object bloqueada: SDT requerido externo ou incompativel chamado '{definition.Name}'. Gere ou reencontre os SDTs pelo Wizard antes. Nenhuma alteracao foi feita.");
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
            var matches = ApiPlanScanProbe.Scan("Procedure", "apiobject-preflight-procedure", () => Procedure.GetAll(designModel)
                .Where(procedure => string.Equals(procedure.Name, definition.Name, StringComparison.OrdinalIgnoreCase))
                .ToArray());

            if (matches.Length == 0)
            {
                throw new InvalidOperationException($"Criacao de API Object bloqueada: Procedure requerida nao foi reencontrada: '{definition.Name}'. Gere as Procedures pelo Wizard antes. Nenhuma alteracao foi feita.");
            }

            if (matches.Length > 1)
            {
                throw new InvalidOperationException($"Criacao de API Object bloqueada: foram encontradas {matches.Length} Procedures chamadas '{definition.Name}'. Nenhuma alteracao foi feita.");
            }

            var procedure = matches[0];
            if (!ApiPlanOwnedObjectDescription.IsOwnedProcedure(procedure.Description, definition.Name))
            {
                throw new InvalidOperationException($"Criacao de API Object bloqueada: Procedure requerida externa ou incompativel chamada '{definition.Name}'. Gere ou reencontre as Procedures pelo Wizard antes. Nenhuma alteracao foi feita.");
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

        if (string.Equals(serviceName, "Delete", StringComparison.OrdinalIgnoreCase))
        {
            return "B100";
        }

        return "B050-B053";
    }

    private static ApiPlanApiObjectPreflightResult PreflightApiObject(
        KBModel designModel,
        ApiPlan apiPlan,
        bool allowIntentionalContractRefresh,
        ApiPlanKbObjectNameIndex kbIndex)
    {
        if (apiPlan.PlannedApiGuid.HasValue && apiPlan.PlannedApiGuid.Value != Guid.Empty)
        {
            return new ApiPlanApiObjectPreflightResult(
                PreflightExistingApiObjectStrict(designModel, apiPlan, allowIntentionalContractRefresh, kbIndex));
        }

        var existing = API.GetAll(designModel)
            .Where(api => string.Equals(api.Name, apiPlan.ApiName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (existing.Length > 0)
        {
            throw new InvalidOperationException(
                $"Criacao de API Object bloqueada: existem {existing.Length} API Objects chamados '{apiPlan.ApiName}', " +
                "mas nenhum GUID planejado foi confirmado. A associacao por nome nao autoriza escolher um objeto. Nenhuma alteracao foi feita.");
        }

        return new ApiPlanApiObjectPreflightResult(null);
    }

}

internal sealed class ApiPlanIntentionalChangeOwnershipDiagnosis
{
    public ApiPlanIntentionalChangeOwnershipDiagnosis(
        bool isOwned,
        string failingClause,
        int matchCount,
        string actualApiGuid = "",
        string? metadataApiGuid = null,
        bool metadataPresent = false,
        bool metadataDescriptionOwned = false,
        bool metadataParseOk = false,
        bool ownershipOk = false,
        bool fingerprintOk = false,
        bool integrityPresent = false,
        bool versionOk = false,
        bool guidOk = false,
        bool descriptionOk = false,
        bool serviceSourceHashOk = false,
        bool serviceDescriptionsHashOk = false,
        string storedSchema = "",
        string storedApiName = "",
        string expectedApiName = "",
        string storedDescription = "",
        string actualDescription = "",
        string storedSourceHash = "",
        string actualSourceHash = "",
        string storedDescriptionsHash = "",
        string actualDescriptionsHash = "",
        bool fingerprintPresent = false,
        bool fingerprintAlgorithmOk = false,
        bool fingerprintScopeOk = false,
        bool fingerprintValuePresent = false,
        bool fingerprintHashMatch = false,
        string fingerprintAlgorithm = "",
        string fingerprintScope = "",
        string fingerprintStored = "",
        string fingerprintActual = "",
        int fingerprintSnapshotLength = 0,
        string fingerprintDetail = "")
    {
        IsOwned = isOwned;
        FailingClause = failingClause ?? "None";
        MatchCount = matchCount;
        ActualApiGuid = actualApiGuid ?? string.Empty;
        MetadataApiGuid = metadataApiGuid;
        MetadataPresent = metadataPresent;
        MetadataDescriptionOwned = metadataDescriptionOwned;
        MetadataParseOk = metadataParseOk;
        OwnershipOk = ownershipOk;
        FingerprintOk = fingerprintOk;
        IntegrityPresent = integrityPresent;
        VersionOk = versionOk;
        GuidOk = guidOk;
        DescriptionOk = descriptionOk;
        ServiceSourceHashOk = serviceSourceHashOk;
        ServiceDescriptionsHashOk = serviceDescriptionsHashOk;
        StoredSchema = storedSchema ?? string.Empty;
        StoredApiName = storedApiName ?? string.Empty;
        ExpectedApiName = expectedApiName ?? string.Empty;
        StoredDescription = storedDescription ?? string.Empty;
        ActualDescription = actualDescription ?? string.Empty;
        StoredSourceHash = storedSourceHash ?? string.Empty;
        ActualSourceHash = actualSourceHash ?? string.Empty;
        StoredDescriptionsHash = storedDescriptionsHash ?? string.Empty;
        ActualDescriptionsHash = actualDescriptionsHash ?? string.Empty;
        FingerprintPresent = fingerprintPresent;
        FingerprintAlgorithmOk = fingerprintAlgorithmOk;
        FingerprintScopeOk = fingerprintScopeOk;
        FingerprintValuePresent = fingerprintValuePresent;
        FingerprintHashMatch = fingerprintHashMatch;
        FingerprintAlgorithm = fingerprintAlgorithm ?? string.Empty;
        FingerprintScope = fingerprintScope ?? string.Empty;
        FingerprintStored = fingerprintStored ?? string.Empty;
        FingerprintActual = fingerprintActual ?? string.Empty;
        FingerprintSnapshotLength = fingerprintSnapshotLength;
        FingerprintDetail = fingerprintDetail ?? string.Empty;
    }

    public bool IsOwned { get; }

    public string FailingClause { get; }

    public int MatchCount { get; }

    public string ActualApiGuid { get; }

    public string? MetadataApiGuid { get; }

    public bool MetadataPresent { get; }

    public bool MetadataDescriptionOwned { get; }

    public bool MetadataParseOk { get; }

    public bool OwnershipOk { get; }

    public bool FingerprintOk { get; }

    public bool IntegrityPresent { get; }

    public bool VersionOk { get; }

    public bool GuidOk { get; }

    public bool DescriptionOk { get; }

    public bool ServiceSourceHashOk { get; }

    public bool ServiceDescriptionsHashOk { get; }

    public string StoredSchema { get; }

    public string StoredApiName { get; }

    public string ExpectedApiName { get; }

    public string StoredDescription { get; }

    public string ActualDescription { get; }

    public string StoredSourceHash { get; }

    public string ActualSourceHash { get; }

    public string StoredDescriptionsHash { get; }

    public string ActualDescriptionsHash { get; }

    public bool FingerprintPresent { get; }

    public bool FingerprintAlgorithmOk { get; }

    public bool FingerprintScopeOk { get; }

    public bool FingerprintValuePresent { get; }

    public bool FingerprintHashMatch { get; }

    public string FingerprintAlgorithm { get; }

    public string FingerprintScope { get; }

    public string FingerprintStored { get; }

    public string FingerprintActual { get; }

    public int FingerprintSnapshotLength { get; }

    public string FingerprintDetail { get; }

    public static ApiPlanIntentionalChangeOwnershipDiagnosis FromInspectedMetadata(
        bool isOwned,
        string failingClause,
        int matchCount,
        string actualApiGuid,
        string? metadataApiGuid,
        string storedSchema,
        string storedApiName,
        string expectedApiName,
        MetadataFingerprintDiagnosis fingerprint,
        bool integrityPresent,
        GeneratedBaselineDiagnosis? baseline)
    {
        if (fingerprint is null)
        {
            throw new ArgumentNullException(nameof(fingerprint));
        }

        return new ApiPlanIntentionalChangeOwnershipDiagnosis(
            isOwned,
            failingClause,
            matchCount,
            actualApiGuid,
            metadataApiGuid,
            metadataPresent: true,
            metadataDescriptionOwned: true,
            metadataParseOk: true,
            ownershipOk: true,
            fingerprintOk: fingerprint.IsCompatible,
            integrityPresent: integrityPresent,
            versionOk: baseline?.VersionOk ?? false,
            guidOk: baseline?.GuidOk ?? false,
            descriptionOk: baseline?.DescriptionOk ?? false,
            serviceSourceHashOk: baseline?.ServiceSourceHashOk ?? false,
            serviceDescriptionsHashOk: baseline?.ServiceDescriptionsHashOk ?? false,
            storedSchema: storedSchema,
            storedApiName: storedApiName,
            expectedApiName: expectedApiName,
            storedDescription: baseline?.StoredDescription ?? string.Empty,
            actualDescription: baseline?.ActualDescription ?? string.Empty,
            storedSourceHash: baseline?.StoredSourceHash ?? string.Empty,
            actualSourceHash: baseline?.ActualSourceHash ?? string.Empty,
            storedDescriptionsHash: baseline?.StoredDescriptionsHash ?? string.Empty,
            actualDescriptionsHash: baseline?.ActualDescriptionsHash ?? string.Empty,
            fingerprintPresent: fingerprint.FingerprintPresent,
            fingerprintAlgorithmOk: fingerprint.AlgorithmOk,
            fingerprintScopeOk: fingerprint.ScopeOk,
            fingerprintValuePresent: fingerprint.ValuePresent,
            fingerprintHashMatch: fingerprint.HashMatch,
            fingerprintAlgorithm: fingerprint.Algorithm,
            fingerprintScope: fingerprint.Scope,
            fingerprintStored: fingerprint.StoredValue,
            fingerprintActual: fingerprint.ActualValue,
            fingerprintSnapshotLength: fingerprint.SnapshotLength,
            fingerprintDetail: fingerprint.FailingClause);
    }

    public static ApiPlanIntentionalChangeOwnershipDiagnosis NotOwned(
        string failingClause,
        int matchCount,
        string actualApiGuid = "",
        string? metadataApiGuid = null,
        bool metadataPresent = false,
        bool metadataDescriptionOwned = false,
        bool metadataParseOk = false,
        bool ownershipOk = false,
        bool fingerprintOk = false,
        bool integrityPresent = false,
        string storedSchema = "",
        string storedApiName = "",
        string expectedApiName = "")
    {
        return new ApiPlanIntentionalChangeOwnershipDiagnosis(
            isOwned: false,
            failingClause: failingClause,
            matchCount: matchCount,
            actualApiGuid: actualApiGuid,
            metadataApiGuid: metadataApiGuid,
            metadataPresent: metadataPresent,
            metadataDescriptionOwned: metadataDescriptionOwned,
            metadataParseOk: metadataParseOk,
            ownershipOk: ownershipOk,
            fingerprintOk: fingerprintOk,
            integrityPresent: integrityPresent,
            storedSchema: storedSchema,
            storedApiName: storedApiName,
            expectedApiName: expectedApiName);
    }

    public static ApiPlanIntentionalChangeOwnershipDiagnosis Owned(
        int matchCount,
        string actualApiGuid,
        string metadataApiGuid,
        string storedSchema,
        string storedApiName,
        string expectedApiName)
    {
        return new ApiPlanIntentionalChangeOwnershipDiagnosis(
            isOwned: true,
            failingClause: "None",
            matchCount: matchCount,
            actualApiGuid: actualApiGuid,
            metadataApiGuid: metadataApiGuid,
            metadataPresent: true,
            metadataDescriptionOwned: true,
            metadataParseOk: true,
            ownershipOk: true,
            fingerprintOk: true,
            integrityPresent: false,
            versionOk: true,
            guidOk: true,
            descriptionOk: true,
            serviceSourceHashOk: true,
            serviceDescriptionsHashOk: true,
            storedSchema: storedSchema,
            storedApiName: storedApiName,
            expectedApiName: expectedApiName);
    }

    public string FormatDetails()
    {
        return string.Join(
            Environment.NewLine,
            $"ClausulaQueFalhou='{FailingClause}'",
            $"ApiObjectCount={MatchCount}",
            $"MetadataPresente={MetadataPresent}",
            $"MetadataDescriptionPropria={MetadataDescriptionOwned}",
            $"MetadataParseOk={MetadataParseOk}",
            $"OwnershipSchemaApiNameGuid={OwnershipOk}",
            $"FingerprintOk={FingerprintOk}",
            $"FingerprintPresente={FingerprintPresent}",
            $"FingerprintAlgoritmoOk={FingerprintAlgorithmOk}",
            $"FingerprintEscopoOk={FingerprintScopeOk}",
            $"FingerprintValorPresente={FingerprintValuePresent}",
            $"FingerprintHashOk={FingerprintHashMatch}",
            $"FingerprintDetalhe='{FingerprintDetail}'",
            $"FingerprintAlgoritmo='{FingerprintAlgorithm}'",
            $"FingerprintEscopo='{FingerprintScope}'",
            $"FingerprintGravado='{FingerprintStored}'",
            $"FingerprintRecalculado='{FingerprintActual}'",
            $"FingerprintSnapshotLength={FingerprintSnapshotLength}",
            $"IntegrityPresente={IntegrityPresent}",
            $"BaselineVersionOk={VersionOk}",
            $"BaselineGuidOk={GuidOk}",
            $"BaselineDescriptionOk={DescriptionOk}",
            $"BaselineServiceSourceHashOk={ServiceSourceHashOk}",
            $"BaselineServiceDescriptionsHashOk={ServiceDescriptionsHashOk}",
            $"ApiObjectGuid='{ActualApiGuid}'",
            $"MetadataApiGuid='{MetadataApiGuid ?? string.Empty}'",
            $"SchemaGravado='{StoredSchema}'",
            $"ApiNameGravado='{StoredApiName}'",
            $"ApiNameEsperado='{ExpectedApiName}'",
            $"DescriptionAtual='{ActualDescription}'",
            $"DescriptionSentinel='{StoredDescription}'",
            $"ServiceSourceHashAtual='{ActualSourceHash}'",
            $"ServiceSourceHashGravado='{StoredSourceHash}'",
            $"DescriptionsHashAtual='{ActualDescriptionsHash}'",
            $"DescriptionsHashGravado='{StoredDescriptionsHash}'");
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
        if (guid == Guid.Empty) throw new ArgumentException("O GUID persistido do API Object nao pode ser vazio.", nameof(guid));
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
