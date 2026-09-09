#nullable enable

using System;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;
using GenexusOpenApiBuilder.Extension.Domain;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Contexto transitório do API Object de uma única aplicação. O objeto pode ser
/// novo e ainda não persistido; consumidores devem usar este GUID e esta instância
/// em vez de resolver novamente pelo nome.
/// </summary>
internal sealed class ApiPlanTransientApiContext
{
    public ApiPlanTransientApiContext(
        API api,
        Folder transactionFolder,
        bool persistApiObject,
        bool businessComponentParticipated,
        bool apiWasCreated,
        bool existingApiHasBusinessComponentParameters,
        string finalWriter,
        ApiPlan apiPlan,
        ApiPlanTransientApiSelection? selection = null)
    {
        Api = api ?? throw new ArgumentNullException(nameof(api));
        TransactionFolder = transactionFolder ?? throw new ArgumentNullException(nameof(transactionFolder));
        FinalWriter = finalWriter ?? throw new ArgumentNullException(nameof(finalWriter));
        PersistApiObject = persistApiObject;
        BusinessComponentParticipated = businessComponentParticipated;
        ApiWasCreated = apiWasCreated;
        ExistingApiHasBusinessComponentParameters = existingApiHasBusinessComponentParameters;
        TransactionGuid = apiPlan?.TransactionGuid ?? throw new ArgumentNullException(nameof(apiPlan));
        ApplicationId = apiPlan.ApplicationId;
        OperationId = apiPlan.OperationId;
        PlannedContractHash = ApiPlanMetadataFileWriter.ComputePlannedContractHash(apiPlan);
        Selection = selection ?? ApiPlanTransientApiSelection.Empty;
        ExistingApiGuid = apiWasCreated ? null : api.Guid;
        ExistingApiName = apiWasCreated ? null : api.Name;
        ExistingApiDescription = apiWasCreated ? null : api.Description;
        ExistingApiServiceGroupSource = apiWasCreated ? null : api.ServiceGroupSource.Source;
    }

    public API Api { get; }

    public Folder TransactionFolder { get; }

    public Guid PlannedApiGuid => Api.Guid;

    public string PlannedApiName => Api.Name;

    public bool PersistApiObject { get; }

    public bool BusinessComponentParticipated { get; }

    public bool ApiWasCreated { get; }

    public bool ExistingApiHasBusinessComponentParameters { get; }

    public string FinalWriter { get; }

    public Guid TransactionGuid { get; }

    public Guid ApplicationId { get; }

    public Guid OperationId { get; }

    public string PlannedContractHash { get; }

    public ApiPlanTransientApiSelection Selection { get; }

    public Guid? ExistingApiGuid { get; }

    public string? ExistingApiName { get; }

    public string? ExistingApiDescription { get; }

    public string? ExistingApiServiceGroupSource { get; }
}

internal sealed class ApiPlanTransientApiSelection
{
    public static readonly ApiPlanTransientApiSelection Empty = new(false, false, false, false, false, false);

    public ApiPlanTransientApiSelection(
        bool generateSdts,
        bool generateProcedures,
        bool generateApiObject,
        bool generateMetadata,
        bool applyList,
        bool applyBusinessComponent)
    {
        GenerateSdts = generateSdts;
        GenerateProcedures = generateProcedures;
        GenerateApiObject = generateApiObject;
        GenerateMetadata = generateMetadata;
        ApplyList = applyList;
        ApplyBusinessComponent = applyBusinessComponent;
    }

    public bool GenerateSdts { get; }
    public bool GenerateProcedures { get; }
    public bool GenerateApiObject { get; }
    public bool GenerateMetadata { get; }
    public bool ApplyList { get; }
    public bool ApplyBusinessComponent { get; }
}
