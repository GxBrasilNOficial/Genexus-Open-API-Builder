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

internal static class ApiPlanGenerationStateReader
{
    public static ApiPlanGenerationState Read(KBModel designModel, Transaction transaction, ApiPlan apiPlan)
    {
        return Read(designModel, transaction, apiPlan, forSyncContractRefresh: false);
    }

    public static ApiPlanGenerationState ReadForSync(KBModel designModel, Transaction transaction, ApiPlan apiPlan)
    {
        return ReadForIntentionalChange(designModel, transaction, apiPlan);
    }

    public static ApiPlanGenerationState ReadForIntentionalChange(KBModel designModel, Transaction transaction, ApiPlan apiPlan)
    {
        return Read(designModel, transaction, apiPlan, forSyncContractRefresh: true);
    }

    public static (ApiPlanGenerationState State, ApiPlanKbObjectNameIndex Index) ReadForIntentionalChangeWithIndex(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan)
    {
        var index = ApiPlanKbObjectNameIndex.Create(designModel);
        var state = Read(designModel, transaction, apiPlan, forSyncContractRefresh: true, index);
        return (state, index);
    }

    public static (ApiPlanGenerationState State, ApiPlanKbObjectNameIndex Index) ReadForSyncWithIndex(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan)
    {
        return ReadForIntentionalChangeWithIndex(designModel, transaction, apiPlan);
    }

    private static ApiPlanGenerationState Read(KBModel designModel, Transaction transaction, ApiPlan apiPlan, bool forSyncContractRefresh)
    {
        var index = ApiPlanKbObjectNameIndex.Create(designModel);
        return Read(designModel, transaction, apiPlan, forSyncContractRefresh, index);
    }

    private static ApiPlanGenerationState Read(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        bool forSyncContractRefresh,
        ApiPlanKbObjectNameIndex index)
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

        // Uma varredura por tipo: GetAll repetido por objeto planejado era O(n*m) e dominava a abertura do wizard.
        var sdtPlan = ApiPlanSdtGenerationPlanBuilder.Create(apiPlan);
        var folder = InspectFolder(index, transaction, apiPlan);
        var sdts = InspectSdts(index, sdtPlan);
        var sdtState = CreateState("SDTs", sdts, folder, forSyncContractRefresh);

        var procedures = InspectProcedures(index, apiPlan);
        var procedureState = sdtState.IsBlocked
            ? ApiPlanGenerationStageState.Blocked("Procedures", "Bloqueado: o estado dos SDTs precisa ser resolvido antes.")
            : CreateState("Procedures", procedures, null, forSyncContractRefresh);

        var apiObject = InspectApiObject(designModel, index, apiPlan, forSyncContractRefresh);
        var apiState = sdtState.IsBlocked || procedureState.IsBlocked
            ? ApiPlanGenerationStageState.Blocked("API Object", "Bloqueado: o estado dos SDTs ou Procedures precisa ser resolvido antes.")
            : CreateState("API Object", apiObject, null, forSyncContractRefresh);

        var metadataFile = InspectMetadataFile(designModel, index, apiPlan, forSyncContractRefresh);
        var metadataState = apiState.IsBlocked
            ? ApiPlanGenerationStageState.Blocked("Metadata File", "Bloqueado: o API Object precisa estar disponivel antes.")
            : CreateState("Metadata File", metadataFile, null, forSyncContractRefresh);

        return new ApiPlanGenerationState(sdtState, procedureState, apiState, metadataState, folder.Warning);
    }

    private static ApiPlanGenerationStageState CreateState(string stageName, ApiPlanGenerationInspection inspection, ApiPlanGenerationInspection? folder, bool forSyncContractRefresh)
    {
        var conflicts = inspection.Conflicts + (folder?.Conflicts ?? 0);
        var collisionConflicts = ApiPlanCollisionConflict.Merge(
            inspection.CollisionConflicts,
            folder?.CollisionConflicts ?? Array.Empty<ApiPlanCollisionConflict>());
        if (conflicts > 0)
        {
            var reason = string.Equals(stageName, "Metadata File", StringComparison.Ordinal) && !forSyncContractRefresh
                ? "colisao(oes) externa(s), incompativel(is), ambigua(s) ou integridade B067 divergente"
                : "colisao(oes) externa(s), incompativel(is) ou ambigua(s)";
            var detail = $"Bloqueado: {conflicts} {reason} detectada(s). Nenhuma escrita sera permitida.";
            if (collisionConflicts.Count > 0)
            {
                detail += Environment.NewLine + ApiPlanCollisionConflict.FormatList(collisionConflicts);
            }

            return ApiPlanGenerationStageState.Blocked(stageName, detail, collisionConflicts);
        }

        var missing = inspection.Missing + (folder?.Missing ?? 0);
        var managed = inspection.Managed + (folder?.Managed ?? 0);
        var action = missing == 0
            ? "Reencontrar e validar"
            : managed == 0
                ? "Criar"
                : "Completar";
        var detailOk = $"{action}: gerenciados={managed}, ausentes={missing}, planejados={inspection.Planned + (folder?.Planned ?? 0)}. A confirmacao continua obrigatoria antes de qualquer escrita.";
        var folderWarning = folder?.Warning;
        if (!string.IsNullOrWhiteSpace(folderWarning))
        {
            detailOk += " " + folderWarning;
        }

        return new ApiPlanGenerationStageState(stageName, action, detailOk, false);
    }

    private static ApiPlanGenerationInspection InspectFolder(ApiPlanKbObjectNameIndex index, Transaction transaction, ApiPlan apiPlan)
    {
        var matches = index.FindFolders(apiPlan.TransactionFolderName);
        if (matches.Count == 0)
        {
            return new ApiPlanGenerationInspection(1, 0, 1, 0);
        }

        if (matches.Count > 1 || !ApiPlanTransactionFolder.IsReusable(matches[0], transaction, apiPlan))
        {
            return new ApiPlanGenerationInspection(1, 0, 0, matches.Count, matches.Select(item => ToCollision(item, "Folder", folderApplicable: true)).ToArray());
        }

        return new ApiPlanGenerationInspection(1, 1, 0, 0, warning: ApiPlanTransactionFolder.CreateReuseWarning(apiPlan));
    }

    private static ApiPlanGenerationInspection InspectSdts(ApiPlanKbObjectNameIndex index, ApiPlanSdtGenerationPlan generationPlan)
    {
        var managed = 0;
        var missing = 0;
        var conflicts = 0;
        var collisionConflicts = new List<ApiPlanCollisionConflict>();
        foreach (var definition in generationPlan.SharedSdts.Concat(generationPlan.OwnSdts))
        {
            var matches = index.FindSdts(definition.Name);
            if (matches.Count == 0)
            {
                missing++;
            }
            else if (matches.Count == 1 && ApiPlanOwnedObjectDescription.IsOwnedSdt(matches[0].Description, definition.Name))
            {
                managed++;
            }
            else
            {
                conflicts += matches.Count;
                collisionConflicts.AddRange(matches.Select(item => ToCollision(item, "SDT", folderApplicable: true)));
            }
        }

        return new ApiPlanGenerationInspection(generationPlan.SharedSdts.Count + generationPlan.OwnSdts.Count, managed, missing, conflicts, collisionConflicts);
    }

    private static ApiPlanGenerationInspection InspectProcedures(ApiPlanKbObjectNameIndex index, ApiPlan apiPlan)
    {
        var managed = 0;
        var missing = 0;
        var conflicts = 0;
        var collisionConflicts = new List<ApiPlanCollisionConflict>();
        foreach (var service in apiPlan.Services)
        {
            var name = $"proc{apiPlan.TransactionName}_API_{service.Name}";
            var matches = index.FindProcedures(name);
            if (matches.Count == 0)
            {
                missing++;
            }
            else if (matches.Count == 1 && ApiPlanOwnedObjectDescription.IsOwnedProcedure(matches[0].Description, name))
            {
                managed++;
            }
            else
            {
                conflicts += matches.Count;
                collisionConflicts.AddRange(matches.Select(item => ToCollision(item, "Procedure", folderApplicable: true)));
            }
        }

        return new ApiPlanGenerationInspection(apiPlan.Services.Count, managed, missing, conflicts, collisionConflicts);
    }

    private static ApiPlanGenerationInspection InspectApiObject(KBModel designModel, ApiPlanKbObjectNameIndex index, ApiPlan apiPlan, bool forSyncContractRefresh)
    {
        var matches = index.FindApis(apiPlan.ApiName);
        if (matches.Count == 0)
        {
            return new ApiPlanGenerationInspection(1, 0, 1, 0);
        }

        ApiPlanApiObjectOwnership.Diagnostic? ownershipDiagnostic = null;
        ApiPlanIntentionalChangeOwnershipDiagnosis? intentionalDiagnosis = null;
        var owned = false;
        if (forSyncContractRefresh)
        {
            intentionalDiagnosis = ApiPlanApiObjectWriter.DiagnoseIntentionalChangeOwnership(designModel, apiPlan, matches);
            owned = intentionalDiagnosis.IsOwned;
        }
        else if (matches.Count == 1)
        {
            ownershipDiagnostic = ApiPlanApiObjectWriter.DiagnoseOwnership(designModel, apiPlan, matches[0]);
            owned = ownershipDiagnostic.IsOwned;
        }

        if (owned)
        {
            return new ApiPlanGenerationInspection(1, 1, 0, 0);
        }

        return new ApiPlanGenerationInspection(
            1,
            0,
            0,
            matches.Count,
            matches.Select(item => ToCollision(
                item,
                "API Object",
                folderApplicable: true,
                diagnosticReason: intentionalDiagnosis?.FailingClause ?? ownershipDiagnostic?.ReasonText,
                apiObjectGuid: intentionalDiagnosis?.ActualApiGuid ?? ownershipDiagnostic?.ActualApiGuid,
                metadataApiGuid: intentionalDiagnosis?.MetadataApiGuid ?? ownershipDiagnostic?.MetadataApiGuid,
                diagnosticDetails: intentionalDiagnosis?.FormatDetails() ?? ownershipDiagnostic?.FormatDetails())).ToArray());
    }

    private static ApiPlanGenerationInspection InspectMetadataFile(KBModel designModel, ApiPlanKbObjectNameIndex index, ApiPlan apiPlan, bool forSyncContractRefresh)
    {
        var matches = index.FindFiles(apiPlan.MetadataFileName);
        if (matches.Count == 0)
        {
            return new ApiPlanGenerationInspection(1, 0, 1, 0);
        }

        if (matches.Count == 1 &&
            ApiPlanOwnedObjectDescription.IsOwnedMetadataFile(matches[0].Description, apiPlan.MetadataFileName, apiPlan.TransactionName) &&
            HasCompatibleMetadata(designModel, index, matches[0], apiPlan, forSyncContractRefresh))
        {
            return new ApiPlanGenerationInspection(1, 1, 0, 0);
        }

        // Baseline B067 / ownership divergente em File proprio: bloqueia sem lista de colisao externa.
        if (matches.Count == 1 &&
            ApiPlanOwnedObjectDescription.IsOwnedMetadataFile(matches[0].Description, apiPlan.MetadataFileName, apiPlan.TransactionName))
        {
            return new ApiPlanGenerationInspection(1, 0, 0, 1);
        }

        return new ApiPlanGenerationInspection(1, 0, 0, matches.Count, matches.Select(item => ToCollision(item, "File", folderApplicable: false)).ToArray());
    }

    private static ApiPlanCollisionConflict ToCollision(
        KBObject kbObject,
        string objectType,
        bool folderApplicable,
        string? diagnosticReason = null,
        string? apiObjectGuid = null,
        string? metadataApiGuid = null,
        string? diagnosticDetails = null)
    {
        var moduleName = kbObject.Module?.Name;
        string folderName;
        if (!folderApplicable)
        {
            folderName = ApiPlanCollisionConflict.NotApplicable;
        }
        else if (kbObject is Folder)
        {
            folderName = kbObject.Name;
        }
        else if (kbObject.Parent is Folder parentFolder)
        {
            folderName = parentFolder.Name;
        }
        else
        {
            folderName = ApiPlanCollisionConflict.NotApplicable;
        }

        return new ApiPlanCollisionConflict(
            kbObject.Name,
            objectType,
            moduleName ?? ApiPlanCollisionConflict.NotApplicable,
            folderName,
            diagnosticReason,
            apiObjectGuid,
            metadataApiGuid,
            diagnosticDetails);
    }

    private static bool HasCompatibleMetadata(KBModel designModel, ApiPlanKbObjectNameIndex index, WikiFileKBObject file, ApiPlan apiPlan, bool forSyncContractRefresh)
    {
        var bytes = file.BlobPart?.Data?.GetBytes();
        if (bytes is null || bytes.Length == 0)
        {
            return false;
        }

        JObject metadata;
        try
        {
            metadata = ApiPlanMetadataIntegrity.ParseMetadataBytes(bytes);
        }
        catch (JsonException)
        {
            return false;
        }

        var transactionMatches = index.FindTransactions(apiPlan.TransactionName);
        var apiMatches = index.FindApis(apiPlan.ApiName);
        if (transactionMatches.Count != 1 || apiMatches.Count != 1)
        {
            return false;
        }

        var transaction = transactionMatches[0];
        var apiObject = apiMatches[0];

        var ownershipOk = ApiPlanMetadataFileWriter.IsSupportedSchemaVersion(metadata["schemaVersion"]?.Value<string>())
            && HasString(metadata.SelectToken("ownership.transactionName"), apiPlan.TransactionName)
            && HasString(metadata.SelectToken("ownership.transactionGuid"), transaction.Guid.ToString())
            && HasString(metadata.SelectToken("ownership.apiName"), apiPlan.ApiName)
            && HasString(metadata.SelectToken("ownership.apiGuid"), apiObject.Guid.ToString())
            && HasString(metadata.SelectToken("ownership.metadataFileName"), apiPlan.MetadataFileName);
        if (!ownershipOk)
        {
            return false;
        }

        // Wizard/Sync: o contrato desejado pode divergir, mas o estado atual
        // ainda precisa corresponder ao ultimo baseline gravado pela extensao.
        if (forSyncContractRefresh)
        {
            return ApiPlanMetadataFileWriter.HasCompatibleGeneratedBaseline(metadata, apiObject);
        }

        if (!ApiPlanBusinessComponentWriter.IsManagedApiObject(designModel, apiPlan, apiObject))
        {
            return false;
        }

        return ApiPlanMetadataFileWriter.HasCompatibleB067Integrity(metadata, apiPlan, apiObject);
    }

    private static bool HasString(JToken? token, string expectedValue)
    {
        return token is not null && token.Type == JTokenType.String && string.Equals(token.Value<string>(), expectedValue, StringComparison.Ordinal);
    }

    private static string ResolveBacklogId(string serviceName)
    {
        if (string.Equals(serviceName, "List", StringComparison.OrdinalIgnoreCase)) return "B050";
        if (string.Equals(serviceName, "Get", StringComparison.OrdinalIgnoreCase)) return "B051";
        if (string.Equals(serviceName, "Create", StringComparison.OrdinalIgnoreCase)) return "B052";
        if (string.Equals(serviceName, "Update", StringComparison.OrdinalIgnoreCase)) return "B053";
        return string.Equals(serviceName, "Delete", StringComparison.OrdinalIgnoreCase) ? "B100" : "B050-B053";
    }
}

internal sealed class ApiPlanGenerationState
{
    public ApiPlanGenerationState(
        ApiPlanGenerationStageState sdts,
        ApiPlanGenerationStageState procedures,
        ApiPlanGenerationStageState apiObject,
        ApiPlanGenerationStageState metadataFile,
        string? transactionFolderWarning = null)
    {
        Sdts = sdts;
        Procedures = procedures;
        ApiObject = apiObject;
        MetadataFile = metadataFile;
        TransactionFolderWarning = transactionFolderWarning;
    }

    public ApiPlanGenerationStageState Sdts { get; }
    public ApiPlanGenerationStageState Procedures { get; }
    public ApiPlanGenerationStageState ApiObject { get; }
    public ApiPlanGenerationStageState MetadataFile { get; }
    public string? TransactionFolderWarning { get; }

    public IReadOnlyList<ApiPlanCollisionConflict> CollectCollisionConflicts(
        bool includeSdts = true,
        bool includeProcedures = true,
        bool includeApiObject = true,
        bool includeMetadataFile = true)
    {
        var stages = new List<ApiPlanGenerationStageState>();
        if (includeSdts)
        {
            stages.Add(Sdts);
        }

        if (includeProcedures)
        {
            stages.Add(Procedures);
        }

        if (includeApiObject)
        {
            stages.Add(ApiObject);
        }

        if (includeMetadataFile)
        {
            stages.Add(MetadataFile);
        }

        return stages
            .Where(stage => stage.IsBlocked)
            .SelectMany(stage => stage.CollisionConflicts)
            .ToArray();
    }
}

internal sealed class ApiPlanGenerationStageState
{
    public ApiPlanGenerationStageState(
        string stageName,
        string action,
        string detail,
        bool isBlocked,
        IReadOnlyList<ApiPlanCollisionConflict>? collisionConflicts = null)
    {
        StageName = stageName;
        Action = action;
        Detail = detail;
        IsBlocked = isBlocked;
        CollisionConflicts = collisionConflicts ?? Array.Empty<ApiPlanCollisionConflict>();
    }

    public string StageName { get; }
    public string Action { get; }
    public string Detail { get; }
    public bool IsBlocked { get; }
    public IReadOnlyList<ApiPlanCollisionConflict> CollisionConflicts { get; }

    public static ApiPlanGenerationStageState Blocked(
        string stageName,
        string detail,
        IReadOnlyList<ApiPlanCollisionConflict>? collisionConflicts = null)
    {
        return new ApiPlanGenerationStageState(stageName, "Bloqueado", detail, true, collisionConflicts);
    }
}

internal sealed class ApiPlanGenerationInspection
{
    public ApiPlanGenerationInspection(
        int planned,
        int managed,
        int missing,
        int conflicts,
        IReadOnlyList<ApiPlanCollisionConflict>? collisionConflicts = null,
        string? warning = null)
    {
        Planned = planned;
        Managed = managed;
        Missing = missing;
        Conflicts = conflicts;
        CollisionConflicts = collisionConflicts ?? Array.Empty<ApiPlanCollisionConflict>();
        Warning = warning;
    }

    public int Planned { get; }
    public int Managed { get; }
    public int Missing { get; }
    public int Conflicts { get; }
    public IReadOnlyList<ApiPlanCollisionConflict> CollisionConflicts { get; }
    public string? Warning { get; }
}
