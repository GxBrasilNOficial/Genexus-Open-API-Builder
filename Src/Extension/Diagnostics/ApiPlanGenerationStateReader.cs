using System;
using System.Linq;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;
using GenexusOpenApiBuilder.Extension.Domain;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

internal static class ApiPlanGenerationStateReader
{
    private const string ProcedureDescriptionPrefix = "Genexus Open API Builder B050-B053 Procedure";

    public static ApiPlanGenerationState Read(KBModel designModel, ApiPlan apiPlan)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        var sdtPlan = ApiPlanSdtGenerationPlanBuilder.Create(apiPlan);
        var folder = InspectFolder(designModel, apiPlan);
        var sdts = InspectSdts(designModel, sdtPlan);
        var sdtState = CreateState("SDTs", sdts, folder);

        var procedures = InspectProcedures(designModel, apiPlan);
        var procedureState = sdtState.IsBlocked
            ? ApiPlanGenerationStageState.Blocked("Procedures", "Bloqueado: o estado dos SDTs precisa ser resolvido antes.")
            : CreateState("Procedures", procedures, null);

        var apiObject = InspectApiObject(designModel, apiPlan);
        var apiState = sdtState.IsBlocked || procedureState.IsBlocked
            ? ApiPlanGenerationStageState.Blocked("API Object", "Bloqueado: o estado dos SDTs ou Procedures precisa ser resolvido antes.")
            : CreateState("API Object", apiObject, null);

        return new ApiPlanGenerationState(sdtState, procedureState, apiState);
    }

    private static ApiPlanGenerationStageState CreateState(string stageName, ApiPlanGenerationInspection inspection, ApiPlanGenerationInspection? folder)
    {
        var conflicts = inspection.Conflicts + (folder?.Conflicts ?? 0);
        if (conflicts > 0)
        {
            return ApiPlanGenerationStageState.Blocked(stageName, $"Bloqueado: {conflicts} colisao(oes) externa(s), incompativel(is) ou ambigua(s) detectada(s). Nenhuma escrita sera permitida.");
        }

        var missing = inspection.Missing + (folder?.Missing ?? 0);
        var managed = inspection.Managed + (folder?.Managed ?? 0);
        var action = missing == 0
            ? "Reencontrar e validar"
            : managed == 0
                ? "Criar"
                : "Completar";
        var detail = $"{action}: gerenciados={managed}, ausentes={missing}, planejados={inspection.Planned + (folder?.Planned ?? 0)}. A confirmacao continua obrigatoria antes de qualquer escrita.";
        return new ApiPlanGenerationStageState(stageName, action, detail, false);
    }

    private static ApiPlanGenerationInspection InspectFolder(KBModel designModel, ApiPlan apiPlan)
    {
        var matches = Folder.GetAll(designModel)
            .Where(folder => string.Equals(folder.Name, apiPlan.TransactionFolderName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (matches.Length == 0)
        {
            return new ApiPlanGenerationInspection(1, 0, 1, 0);
        }

        if (matches.Length > 1 || !string.Equals(matches[0].Description, ApiPlanTransactionFolder.CreateOwnedDescription(apiPlan), StringComparison.Ordinal))
        {
            return new ApiPlanGenerationInspection(1, 0, 0, 1);
        }

        return new ApiPlanGenerationInspection(1, 1, 0, 0);
    }

    private static ApiPlanGenerationInspection InspectSdts(KBModel designModel, ApiPlanSdtGenerationPlan generationPlan)
    {
        var managed = 0;
        var missing = 0;
        var conflicts = 0;
        foreach (var definition in generationPlan.SharedSdts.Concat(generationPlan.OwnSdts))
        {
            var matches = SDT.GetAll(designModel).Where(sdt => string.Equals(sdt.Name, definition.Name, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length == 0)
            {
                missing++;
            }
            else if (matches.Length == 1 && string.Equals(matches[0].Description, ApiPlanSdtWriter.CreateOwnedDescriptionFor(definition.BacklogId, definition.Kind), StringComparison.Ordinal))
            {
                managed++;
            }
            else
            {
                conflicts++;
            }
        }

        return new ApiPlanGenerationInspection(generationPlan.SharedSdts.Count + generationPlan.OwnSdts.Count, managed, missing, conflicts);
    }

    private static ApiPlanGenerationInspection InspectProcedures(KBModel designModel, ApiPlan apiPlan)
    {
        var managed = 0;
        var missing = 0;
        var conflicts = 0;
        foreach (var service in apiPlan.Services)
        {
            var name = $"proc{apiPlan.TransactionName}_API_{service.Name}";
            var matches = Procedure.GetAll(designModel).Where(procedure => string.Equals(procedure.Name, name, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length == 0)
            {
                missing++;
            }
            else if (matches.Length == 1 && string.Equals(matches[0].Description, $"{ProcedureDescriptionPrefix} - {ResolveBacklogId(service.Name)} - {service.Name}", StringComparison.Ordinal))
            {
                managed++;
            }
            else
            {
                conflicts++;
            }
        }

        return new ApiPlanGenerationInspection(apiPlan.Services.Count, managed, missing, conflicts);
    }

    private static ApiPlanGenerationInspection InspectApiObject(KBModel designModel, ApiPlan apiPlan)
    {
        var matches = API.GetAll(designModel).Where(api => string.Equals(api.Name, apiPlan.ApiName, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length == 0)
        {
            return new ApiPlanGenerationInspection(1, 0, 1, 0);
        }

        if (matches.Length == 1 && string.Equals(matches[0].Description, ApiPlanApiObjectWriter.CreateOwnedDescription(apiPlan), StringComparison.Ordinal))
        {
            return new ApiPlanGenerationInspection(1, 1, 0, 0);
        }

        return new ApiPlanGenerationInspection(1, 0, 0, 1);
    }

    private static string ResolveBacklogId(string serviceName)
    {
        if (string.Equals(serviceName, "List", StringComparison.OrdinalIgnoreCase)) return "B050";
        if (string.Equals(serviceName, "Get", StringComparison.OrdinalIgnoreCase)) return "B051";
        if (string.Equals(serviceName, "Create", StringComparison.OrdinalIgnoreCase)) return "B052";
        return string.Equals(serviceName, "Update", StringComparison.OrdinalIgnoreCase) ? "B053" : "B050-B053";
    }
}

internal sealed class ApiPlanGenerationState
{
    public ApiPlanGenerationState(ApiPlanGenerationStageState sdts, ApiPlanGenerationStageState procedures, ApiPlanGenerationStageState apiObject)
    {
        Sdts = sdts;
        Procedures = procedures;
        ApiObject = apiObject;
    }

    public ApiPlanGenerationStageState Sdts { get; }
    public ApiPlanGenerationStageState Procedures { get; }
    public ApiPlanGenerationStageState ApiObject { get; }
}

internal sealed class ApiPlanGenerationStageState
{
    public ApiPlanGenerationStageState(string stageName, string action, string detail, bool isBlocked)
    {
        StageName = stageName;
        Action = action;
        Detail = detail;
        IsBlocked = isBlocked;
    }

    public string StageName { get; }
    public string Action { get; }
    public string Detail { get; }
    public bool IsBlocked { get; }

    public static ApiPlanGenerationStageState Blocked(string stageName, string detail)
    {
        return new ApiPlanGenerationStageState(stageName, "Bloqueado", detail, true);
    }
}

internal sealed class ApiPlanGenerationInspection
{
    public ApiPlanGenerationInspection(int planned, int managed, int missing, int conflicts)
    {
        Planned = planned;
        Managed = managed;
        Missing = missing;
        Conflicts = conflicts;
    }

    public int Planned { get; }
    public int Managed { get; }
    public int Missing { get; }
    public int Conflicts { get; }
}