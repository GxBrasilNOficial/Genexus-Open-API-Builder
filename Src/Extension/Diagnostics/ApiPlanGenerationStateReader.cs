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

        // Uma varredura por tipo: GetAll repetido por objeto planejado era O(n*m) e dominava a abertura do wizard.
        var index = KbObjectNameIndex.Create(designModel);
        var sdtPlan = ApiPlanSdtGenerationPlanBuilder.Create(apiPlan);
        var folder = InspectFolder(index, apiPlan);
        var sdts = InspectSdts(index, sdtPlan);
        var sdtState = CreateState("SDTs", sdts, folder);

        var procedures = InspectProcedures(index, apiPlan);
        var procedureState = sdtState.IsBlocked
            ? ApiPlanGenerationStageState.Blocked("Procedures", "Bloqueado: o estado dos SDTs precisa ser resolvido antes.")
            : CreateState("Procedures", procedures, null);

        var apiObject = InspectApiObject(designModel, index, apiPlan);
        var apiState = sdtState.IsBlocked || procedureState.IsBlocked
            ? ApiPlanGenerationStageState.Blocked("API Object", "Bloqueado: o estado dos SDTs ou Procedures precisa ser resolvido antes.")
            : CreateState("API Object", apiObject, null);

        var metadataFile = InspectMetadataFile(designModel, index, apiPlan);
        var metadataState = apiState.IsBlocked
            ? ApiPlanGenerationStageState.Blocked("Metadata File", "Bloqueado: o API Object precisa estar disponivel antes.")
            : CreateState("Metadata File", metadataFile, null);

        return new ApiPlanGenerationState(sdtState, procedureState, apiState, metadataState);
    }

    private static ApiPlanGenerationStageState CreateState(string stageName, ApiPlanGenerationInspection inspection, ApiPlanGenerationInspection? folder)
    {
        var conflicts = inspection.Conflicts + (folder?.Conflicts ?? 0);
        if (conflicts > 0)
        {
            var reason = string.Equals(stageName, "Metadata File", StringComparison.Ordinal)
                ? "colisao(oes) externa(s), incompativel(is), ambigua(s) ou integridade B067 divergente"
                : "colisao(oes) externa(s), incompativel(is) ou ambigua(s)";
            return ApiPlanGenerationStageState.Blocked(stageName, $"Bloqueado: {conflicts} {reason} detectada(s). Nenhuma escrita sera permitida.");
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

    private static ApiPlanGenerationInspection InspectFolder(KbObjectNameIndex index, ApiPlan apiPlan)
    {
        var matches = index.FindFolders(apiPlan.TransactionFolderName);
        if (matches.Count == 0)
        {
            return new ApiPlanGenerationInspection(1, 0, 1, 0);
        }

        if (matches.Count > 1 || !string.Equals(matches[0].Description, ApiPlanTransactionFolder.CreateOwnedDescription(apiPlan), StringComparison.Ordinal))
        {
            return new ApiPlanGenerationInspection(1, 0, 0, 1);
        }

        return new ApiPlanGenerationInspection(1, 1, 0, 0);
    }

    private static ApiPlanGenerationInspection InspectSdts(KbObjectNameIndex index, ApiPlanSdtGenerationPlan generationPlan)
    {
        var managed = 0;
        var missing = 0;
        var conflicts = 0;
        foreach (var definition in generationPlan.SharedSdts.Concat(generationPlan.OwnSdts))
        {
            var matches = index.FindSdts(definition.Name);
            if (matches.Count == 0)
            {
                missing++;
            }
            else if (matches.Count == 1 && string.Equals(matches[0].Description, ApiPlanSdtWriter.CreateOwnedDescriptionFor(definition.BacklogId, definition.Kind), StringComparison.Ordinal))
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

    private static ApiPlanGenerationInspection InspectProcedures(KbObjectNameIndex index, ApiPlan apiPlan)
    {
        var managed = 0;
        var missing = 0;
        var conflicts = 0;
        foreach (var service in apiPlan.Services)
        {
            var name = $"proc{apiPlan.TransactionName}_API_{service.Name}";
            var matches = index.FindProcedures(name);
            if (matches.Count == 0)
            {
                missing++;
            }
            else if (matches.Count == 1 && string.Equals(matches[0].Description, $"{ProcedureDescriptionPrefix} - {ResolveBacklogId(service.Name)} - {service.Name}", StringComparison.Ordinal))
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

    private static ApiPlanGenerationInspection InspectApiObject(KBModel designModel, KbObjectNameIndex index, ApiPlan apiPlan)
    {
        var matches = index.FindApis(apiPlan.ApiName);
        if (matches.Count == 0)
        {
            return new ApiPlanGenerationInspection(1, 0, 1, 0);
        }

        if (matches.Count == 1 &&
            ApiPlanApiObjectWriter.IsOwnedApiObject(designModel, apiPlan, matches[0]))
        {
            return new ApiPlanGenerationInspection(1, 1, 0, 0);
        }

        return new ApiPlanGenerationInspection(1, 0, 0, 1);
    }

    private static ApiPlanGenerationInspection InspectMetadataFile(KBModel designModel, KbObjectNameIndex index, ApiPlan apiPlan)
    {
        var matches = index.FindFiles(apiPlan.MetadataFileName);
        if (matches.Count == 0)
        {
            return new ApiPlanGenerationInspection(1, 0, 1, 0);
        }

        if (matches.Count == 1 && string.Equals(matches[0].Description, ApiPlanMetadataFileWriter.CreateOwnedDescription(apiPlan), StringComparison.Ordinal) && HasCompatibleMetadata(designModel, index, matches[0], apiPlan))
        {
            return new ApiPlanGenerationInspection(1, 1, 0, 0);
        }

        return new ApiPlanGenerationInspection(1, 0, 0, 1);
    }

    private static bool HasCompatibleMetadata(KBModel designModel, KbObjectNameIndex index, WikiFileKBObject file, ApiPlan apiPlan)
    {
        var bytes = file.BlobPart?.Data?.GetBytes();
        if (bytes is null || bytes.Length == 0)
        {
            return false;
        }

        JObject metadata;
        try
        {
            metadata = JObject.Parse(Encoding.UTF8.GetString(bytes));
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
        // B087: metadata compatível não exige Description do API Object igual à sentinela.
        if (!ApiPlanBusinessComponentWriter.IsManagedApiObject(designModel, apiPlan, apiObject))
        {
            return false;
        }

        return HasString(metadata["schemaVersion"], ApiPlanMetadataFileWriter.SchemaVersion)
            && HasString(metadata.SelectToken("ownership.transactionName"), apiPlan.TransactionName)
            && HasString(metadata.SelectToken("ownership.transactionGuid"), transaction.Guid.ToString())
            && HasString(metadata.SelectToken("ownership.apiName"), apiPlan.ApiName)
            && HasString(metadata.SelectToken("ownership.apiGuid"), apiObject.Guid.ToString())
            && HasString(metadata.SelectToken("ownership.metadataFileName"), apiPlan.MetadataFileName)
            && ApiPlanMetadataFileWriter.HasCompatibleB067Integrity(metadata, apiPlan, apiObject);
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
        return string.Equals(serviceName, "Update", StringComparison.OrdinalIgnoreCase) ? "B053" : "B050-B053";
    }

    private sealed class KbObjectNameIndex
    {
        private readonly ILookup<string, Folder> _folders;
        private readonly ILookup<string, SDT> _sdts;
        private readonly ILookup<string, Procedure> _procedures;
        private readonly ILookup<string, API> _apis;
        private readonly ILookup<string, WikiFileKBObject> _files;
        private readonly ILookup<string, Transaction> _transactions;

        private KbObjectNameIndex(
            ILookup<string, Folder> folders,
            ILookup<string, SDT> sdts,
            ILookup<string, Procedure> procedures,
            ILookup<string, API> apis,
            ILookup<string, WikiFileKBObject> files,
            ILookup<string, Transaction> transactions)
        {
            _folders = folders;
            _sdts = sdts;
            _procedures = procedures;
            _apis = apis;
            _files = files;
            _transactions = transactions;
        }

        public static KbObjectNameIndex Create(KBModel designModel)
        {
            return new KbObjectNameIndex(
                Folder.GetAll(designModel).ToLookup(item => item.Name, StringComparer.OrdinalIgnoreCase),
                SDT.GetAll(designModel).ToLookup(item => item.Name, StringComparer.OrdinalIgnoreCase),
                Procedure.GetAll(designModel).ToLookup(item => item.Name, StringComparer.OrdinalIgnoreCase),
                API.GetAll(designModel).ToLookup(item => item.Name, StringComparer.OrdinalIgnoreCase),
                WikiFileKBObject.GetAll(designModel).ToLookup(item => item.Name, StringComparer.OrdinalIgnoreCase),
                Transaction.GetAll(designModel).ToLookup(item => item.Name, StringComparer.OrdinalIgnoreCase));
        }

        public IReadOnlyList<Folder> FindFolders(string name) => _folders[name].ToArray();
        public IReadOnlyList<SDT> FindSdts(string name) => _sdts[name].ToArray();
        public IReadOnlyList<Procedure> FindProcedures(string name) => _procedures[name].ToArray();
        public IReadOnlyList<API> FindApis(string name) => _apis[name].ToArray();
        public IReadOnlyList<WikiFileKBObject> FindFiles(string name) => _files[name].ToArray();
        public IReadOnlyList<Transaction> FindTransactions(string name) => _transactions[name].ToArray();
    }
}

internal sealed class ApiPlanGenerationState
{
    public ApiPlanGenerationState(ApiPlanGenerationStageState sdts, ApiPlanGenerationStageState procedures, ApiPlanGenerationStageState apiObject, ApiPlanGenerationStageState metadataFile)
    {
        Sdts = sdts;
        Procedures = procedures;
        ApiObject = apiObject;
        MetadataFile = metadataFile;
    }

    public ApiPlanGenerationStageState Sdts { get; }
    public ApiPlanGenerationStageState Procedures { get; }
    public ApiPlanGenerationStageState ApiObject { get; }
    public ApiPlanGenerationStageState MetadataFile { get; }
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
