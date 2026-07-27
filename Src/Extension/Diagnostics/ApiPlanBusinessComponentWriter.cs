using System;
using System.Collections.Generic;
using System.Linq;
using Artech.Architecture.Common.Objects;
using Artech.Architecture.Common.Services;
using Artech.Genexus.Common;
using Artech.Genexus.Common.Objects;
using Artech.Genexus.Common.Types;
using GenexusOpenApiBuilder.Extension.Domain;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

internal static class ApiPlanBusinessComponentWriter
{
    private const string ProcedureDescriptionPrefix = "Genexus Open API Builder B050-B053 Procedure";

    public static ApiPlanBusinessComponentWriteResult Apply(KBModel model, Transaction transaction, ApiPlan plan)
    {
        if (model is null) throw new ArgumentNullException(nameof(model));
        if (transaction is null) throw new ArgumentNullException(nameof(transaction));
        if (plan is null) throw new ArgumentNullException(nameof(plan));
        if (!string.Equals(transaction.Name, plan.TransactionName, StringComparison.Ordinal))
            throw new InvalidOperationException("B055 bloqueado: o ApiPlan nao pertence a Transaction atual. Nenhuma alteracao foi feita.");
        if (!transaction.IsBusinessComponent)
            throw new InvalidOperationException($"B055 bloqueado: Transaction='{transaction.Name}' esta com Business Component desabilitado. Nenhuma alteracao foi feita.");
        if (!HasService(plan, "Create") || !HasService(plan, "Update"))
            throw new InvalidOperationException("B055 bloqueado: o ApiPlan precisa conter Create e Update. Nenhuma alteracao foi feita.");

        EnsureSdts(model, plan);
        ApiPlanTransactionFolder.Preflight(model, plan);
        var createContent = CreateContent(plan);
        var createRules = CreateRules();
        var createVariables = CreateVariables(plan);
        var updateContent = UpdateContent(plan);
        var updateRules = UpdateRules(plan);
        var updateVariables = UpdateVariables(plan);
        var apiSource = CreateServiceGroupSource(plan);
        var apiVariables = ApiVariableSpecs(plan);

        var create = FindProcedure(model, plan, "Create", "B052");
        var update = FindProcedure(model, plan, "Update", "B053");
        var api = FindApi(model, plan);
        EnsureProcedure(create, "B052", "Create", Skeleton("B052", "Create"), createContent, createVariables, createRules);
        EnsureProcedure(update, "B053", "Update", Skeleton("B053", "Update"), updateContent, updateVariables, updateRules);
        EnsureApi(api, plan);
        ValidateProcedureVariableSpecs(model, create, createVariables);
        ValidateProcedureVariableSpecs(model, update, updateVariables);
        ValidateApiVariableSpecs(model, api, apiVariables);

        ApiPlanSdtWriter.CreateOrReencounter(model, transaction, plan);
        SaveProcedure(model, create, createContent, createVariables, createRules);
        SaveProcedure(model, update, updateContent, updateVariables, updateRules);
        var transactionFolder = ApiPlanTransactionFolder.CreateOrReencounter(model, transaction, plan);
        SaveApi(model, api, transactionFolder, apiSource, apiVariables);
        return new ApiPlanBusinessComponentWriteResult(create.Guid, update.Guid, api.Guid, plan.PrimaryKey.Count, plan.CreateRequestFields.Count, plan.UpdateRequestFields.Count, plan.ResponseFields.Count);
    }

    internal static bool IsManagedApiObject(KBModel model, ApiPlan plan, API api)
    {
        if (plan is null) throw new ArgumentNullException(nameof(plan));
        if (api is null) throw new ArgumentNullException(nameof(api));
        var source = NormalizeForComparison(api.ServiceGroupSource.Source);
        return string.Equals(source, NormalizeForComparison(B054Source(plan)), StringComparison.Ordinal)
            || (string.Equals(source, NormalizeForComparison(CreateServiceGroupSource(plan)), StringComparison.Ordinal) && HasExpectedVariables(model, api, ApiVariableSpecs(plan)));
    }

    internal static bool IsB055ApiObject(KBModel model, ApiPlan plan, API api) =>
        api is not null && string.Equals(NormalizeForComparison(api.ServiceGroupSource.Source), NormalizeForComparison(CreateServiceGroupSource(plan)), StringComparison.Ordinal) &&
        HasExpectedVariables(model, api, ApiVariableSpecs(plan));

    internal static string CreateServiceGroupSource(ApiPlan plan)
    {
        var services = plan.Services.Select(service => ServiceSource(plan, service.Name));
        return $"{plan.ApiName}{Environment.NewLine}{{{Environment.NewLine}{string.Join(Environment.NewLine + Environment.NewLine, services)}{Environment.NewLine}}}";
    }

    private static API FindApi(KBModel model, ApiPlan plan)
    {
        var matches = API.GetAll(model).Where(item => string.Equals(item.Name, plan.ApiName, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length != 1 || !string.Equals(matches.Single().Description, ApiPlanApiObjectWriter.CreateOwnedDescription(plan), StringComparison.Ordinal))
            throw new InvalidOperationException($"B055 bloqueado: API Object proprio '{plan.ApiName}' nao foi reencontrado com seguranca. Execute B054 antes. Nenhuma alteracao foi feita.");
        return matches.Single();
    }

    private static Procedure FindProcedure(KBModel model, ApiPlan plan, string service, string backlog)
    {
        var name = $"proc{plan.TransactionName}_API_{service}";
        var matches = Procedure.GetAll(model).Where(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length != 1 || !string.Equals(matches.Single().Description, $"{ProcedureDescriptionPrefix} - {backlog} - {service}", StringComparison.Ordinal))
            throw new InvalidOperationException($"B055 bloqueado: Procedure propria '{name}' nao foi reencontrada com seguranca. Execute B050-B053 antes. Nenhuma alteracao foi feita.");
        return matches.Single();
    }

    private static void EnsureSdts(KBModel model, ApiPlan plan)
    {
        var definitions = ApiPlanSdtGenerationPlanBuilder.Create(plan);
        foreach (var definition in definitions.SharedSdts.Concat(definitions.OwnSdts))
        {
            var matches = SDT.GetAll(model).Where(item => string.Equals(item.Name, definition.Name, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1 || !string.Equals(matches.Single().Description, ApiPlanSdtWriter.CreateOwnedDescriptionFor(definition.BacklogId, definition.Kind), StringComparison.Ordinal))
                throw new InvalidOperationException($"B055 bloqueado: SDT proprio requerido '{definition.Name}' nao foi reencontrado com seguranca. Nenhuma alteracao foi feita.");
        }
    }

    private static void EnsureApi(API api, ApiPlan plan)
    {
        if (!IsManagedApiObject(api.Model, plan, api))
        {
            throw new InvalidOperationException($"B055 bloqueado: API Object proprio '{api.Name}' possui fonte ou variaveis divergentes da geracao B054/B055. Nenhuma alteracao foi feita.");
        }
    }

    private static void EnsureProcedure(Procedure procedure, string backlog, string service, string skeleton, string content, IReadOnlyList<VariableSpec> variables, string rules)
    {
        var currentSource = NormalizeForComparison(procedure.ProcedurePart.Source);
        if (!string.IsNullOrWhiteSpace(currentSource) &&
            !string.Equals(currentSource, NormalizeForComparison(skeleton), StringComparison.Ordinal) &&
            !string.Equals(currentSource, NormalizeForComparison(content), StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"B055 bloqueado: Procedure propria '{procedure.Name}' possui Source divergente da geracao {backlog}/{service}. Nenhuma alteracao foi feita.");
        }

        var currentRules = NormalizeForComparison(procedure.Rules.Source);
        if (!string.IsNullOrWhiteSpace(currentRules) && !string.Equals(currentRules, NormalizeForComparison(rules), StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"B055 bloqueado: Procedure propria '{procedure.Name}' possui Rules divergentes da geracao {backlog}/{service}. Nenhuma alteracao foi feita.");
        }

        var currentVariables = procedure.Variables.Variables
            .Where(variable => !variable.IsStandard)
            .Select(variable => variable.Name)
            .ToArray();
        if (currentVariables.Length == 0)
        {
            return;
        }

        if (!HasExpectedVariables(procedure.Model, procedure, variables))
        {
            throw new InvalidOperationException($"B055 bloqueado: Procedure propria '{procedure.Name}' possui variaveis divergentes da geracao {backlog}/{service}. Nenhuma alteracao foi feita.");
        }
    }

    private static string NormalizeForComparison(string? value)
    {
        return (value ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n").Trim();
    }

    private static void SaveProcedure(KBModel model, Procedure procedure, string content, IReadOnlyList<VariableSpec> variables, string rules)
    {
        procedure.ProcedurePart.Source = content;
        procedure.Rules.Source = rules;
        ReplaceVariables(model, procedure, variables);
        procedure.Save();

        var persisted = Procedure.Get(model, procedure.Guid);
        if (!string.Equals(NormalizeForComparison(persisted.ProcedurePart.Source), NormalizeForComparison(content), StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"B055 bloqueado: a Procedure '{procedure.Name}' foi salva, mas o Source persistido nao corresponde ao conteudo Business Component planejado. Nenhuma outra alteracao sera feita.");
        }

        if (!string.Equals(NormalizeForComparison(persisted.Rules.Source), NormalizeForComparison(rules), StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"B055 bloqueado: a Procedure '{procedure.Name}' foi salva, mas as Rules persistidas nao correspondem ao conteudo Business Component planejado. Nenhuma outra alteracao sera feita.");
        }

        if (!HasExpectedVariables(model, persisted, variables))
        {
            throw new InvalidOperationException($"B055 bloqueado: a Procedure '{procedure.Name}' foi salva, mas as variaveis persistidas nao correspondem ao contrato Business Component planejado. Nenhuma outra alteracao sera feita.");
        }
    }

    private static void SaveApi(KBModel model, API api, Folder transactionFolder, string source, IReadOnlyList<VariableSpec> variables)
    {
        api.Parent = transactionFolder;
        api.ServiceGroupSource.Source = source;
        ReplaceVariables(model, api, variables);
        api.Save();

        var persisted = API.Get(model, api.Guid);
        if (!string.Equals(NormalizeForComparison(persisted.ServiceGroupSource.Source), NormalizeForComparison(source), StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"B055 bloqueado: o API Object '{api.Name}' foi salvo, mas o Service Source persistido nao corresponde ao contrato API/Procedure planejado. Nenhuma outra alteracao sera feita.");
        }

        if (!HasExpectedVariables(model, persisted, variables))
        {
            throw new InvalidOperationException($"B055 bloqueado: o API Object '{api.Name}' foi salvo, mas as variaveis persistidas nao correspondem ao contrato API/Procedure planejado. Nenhuma outra alteracao sera feita.");
        }
    }

    private static void ValidateProcedureVariableSpecs(KBModel model, Procedure procedure, IReadOnlyList<VariableSpec> variables)
    {
        foreach (var variable in variables)
        {
            var item = new Variable(variable.Name, procedure.Variables);
            if (!TrySetAttributeBasedOn(model, item, variable.DataType) && !DataType.ParseInto(model, variable.DataType, item))
            {
                throw new InvalidOperationException($"B055 bloqueado: tipo da variavel '&{variable.Name}' nao foi resolvido antes da escrita: '{variable.DataType}'. Nenhuma alteracao foi feita.");
            }
        }
    }

    private static void ValidateApiVariableSpecs(KBModel model, API api, IReadOnlyList<VariableSpec> variables)
    {
        foreach (var variable in variables)
        {
            var item = new Variable(variable.Name, api.Variables);
            if (!TrySetAttributeBasedOn(model, item, variable.DataType) && !DataType.ParseInto(model, variable.DataType, item))
            {
                throw new InvalidOperationException($"B055 bloqueado: tipo da variavel de API '&{variable.Name}' nao foi resolvido antes da escrita: '{variable.DataType}'. Nenhuma alteracao foi feita.");
            }
        }
    }

    private static void ReplaceVariables(KBModel model, Procedure procedure, IReadOnlyList<VariableSpec> variables)
    {
        foreach (var existing in procedure.Variables.Variables.Where(variable => !variable.IsStandard).ToArray())
        {
            procedure.Variables.Variables.Remove(existing);
        }

        foreach (var variable in variables)
        {
            var item = new Variable(variable.Name, procedure.Variables);
            if (!TrySetAttributeBasedOn(model, item, variable.DataType) && !DataType.ParseInto(model, variable.DataType, item))
            {
                throw new InvalidOperationException($"B055 bloqueado: tipo da variavel '&{variable.Name}' nao foi resolvido: '{variable.DataType}'. Nenhuma alteracao foi feita.");
            }

            procedure.Variables.Variables.Add(item);
        }
    }

    private static void ReplaceVariables(KBModel model, API api, IReadOnlyList<VariableSpec> variables)
    {
        foreach (var existing in api.Variables.Variables.Where(variable => !variable.IsStandard).ToArray())
        {
            api.Variables.Variables.Remove(existing);
        }

        foreach (var variable in variables)
        {
            var item = new Variable(variable.Name, api.Variables);
            if (!TrySetAttributeBasedOn(model, item, variable.DataType) && !DataType.ParseInto(model, variable.DataType, item))
            {
                throw new InvalidOperationException($"B055 bloqueado: tipo da variavel de API '&{variable.Name}' nao foi resolvido: '{variable.DataType}'. Nenhuma alteracao foi feita.");
            }

            api.Variables.Variables.Add(item);
        }
    }

    private static bool HasExpectedVariables(KBModel model, Procedure procedure, IReadOnlyList<VariableSpec> variables)
    {
        var currentVariables = procedure.Variables.Variables
            .Where(variable => !variable.IsStandard)
            .Select(variable => variable.Name)
            .ToArray();
        var expectedVariables = new HashSet<string>(variables.Select(variable => variable.Name), StringComparer.OrdinalIgnoreCase);
        return currentVariables.Length == expectedVariables.Count &&
            currentVariables.All(variable => expectedVariables.Contains(variable)) &&
            variables.All(variable => MatchesVariableSpec(model, procedure, variable));
    }

    private static bool HasExpectedVariables(KBModel model, API api, IReadOnlyList<VariableSpec> variables)
    {
        var currentVariables = api.Variables.Variables
            .Where(variable => !variable.IsStandard)
            .Select(variable => variable.Name)
            .ToArray();
        var expectedVariables = new HashSet<string>(variables.Select(variable => variable.Name), StringComparer.OrdinalIgnoreCase);
        return currentVariables.Length == expectedVariables.Count &&
            currentVariables.All(variable => expectedVariables.Contains(variable)) &&
            variables.All(variable => MatchesVariableSpec(model, api, variable));
    }

    private static bool MatchesVariableSpec(KBModel model, Procedure procedure, VariableSpec variable)
    {
        var current = procedure.Variables.GetVariable(variable.Name, false);
        if (current is null)
        {
            return false;
        }

        var expected = new Variable(variable.Name, procedure.Variables);
        if (!TrySetAttributeBasedOn(model, expected, variable.DataType) && !DataType.ParseInto(model, variable.DataType, expected))
        {
            return false;
        }

        return current.Type == expected.Type &&
            string.Equals(current.AttributeBasedOn?.Name ?? string.Empty, expected.AttributeBasedOn?.Name ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesVariableSpec(KBModel model, API api, VariableSpec variable)
    {
        var current = api.Variables.GetVariable(variable.Name, false);
        if (current is null)
        {
            return false;
        }

        var expected = new Variable(variable.Name, api.Variables);
        if (!TrySetAttributeBasedOn(model, expected, variable.DataType) && !DataType.ParseInto(model, variable.DataType, expected))
        {
            return false;
        }

        return current.Type == expected.Type &&
            string.Equals(current.AttributeBasedOn?.Name ?? string.Empty, expected.AttributeBasedOn?.Name ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
    private static bool TrySetAttributeBasedOn(KBModel model, Variable variable, string dataType)
    {
        const string prefix = "Attribute:";
        if (!dataType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        variable.AttributeBasedOn = EnsureAttributeExists(model, variable.Name, dataType);
        return true;
    }

    private static Artech.Genexus.Common.Objects.Attribute EnsureAttributeExists(KBModel model, string variableName, string dataType)
    {
        const string prefix = "Attribute:";
        var attributeName = dataType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? dataType.Substring(prefix.Length).Trim()
            : dataType.Trim();
        var matches = Artech.Genexus.Common.Objects.Attribute.GetAll(model)
            .Where(attribute => string.Equals(attribute.Name, attributeName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (matches.Length != 1)
        {
            throw new InvalidOperationException($"B055 bloqueado: atributo base da variavel '&{variableName}' nao foi reencontrado com seguranca: '{attributeName}'. Nenhuma alteracao foi feita.");
        }

        return matches[0];
    }

    private static string CreateContent(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var lines = new List<string> { $"{bc} = new()" };
        lines.AddRange(plan.CreateRequestFields.Select(field => $"{bc}.{field.Name} = &CreateRequest.{field.Name}"));
        lines.Add($"{bc}.Save()");
        lines.Add($"If {bc}.Success()");
        lines.Add($"    {bc}.Load({LoadArguments(plan, bc)})");
        lines.Add("    &CreateResponse = new()");
        lines.AddRange(ResponseAssignments(plan, bc, "&CreateResponse", 4));
        lines.Add("Else");
        lines.AddRange(FailureMessages(bc, 4));
        lines.Add("EndIf");
        return string.Join(Environment.NewLine, lines);
    }

    private static string UpdateContent(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var lines = new List<string> { $"{bc}.Load({LoadArguments(plan, "&")})", $"If {bc}.Success()" };
        lines.AddRange(plan.UpdateRequestFields.Select(field => $"    {bc}.{field.Name} = &UpdateRequest.{field.Name}"));
        lines.Add($"    {bc}.Save()");
        lines.Add($"    If {bc}.Success()");
        lines.Add($"        {bc}.Load({LoadArguments(plan, "&")})");
        lines.Add("        &UpdateResponse = new()");
        lines.AddRange(ResponseAssignments(plan, bc, "&UpdateResponse", 8));
        lines.Add("    Else");
        lines.AddRange(FailureMessages(bc, 8));
        lines.Add("    EndIf");
        lines.Add("Else");
        lines.AddRange(FailureMessages(bc, 4));
        lines.Add("EndIf");
        return string.Join(Environment.NewLine, lines);
    }

    private static IEnumerable<string> ResponseAssignments(ApiPlan plan, string bc, string response, int spaces) =>
        plan.ResponseFields.Select(field => $"{new string(' ', spaces)}{response}.{field.Name} = {bc}.{field.Name}");

    private static IEnumerable<string> FailureMessages(string bc, int spaces) => new[]
    {
        $"{new string(' ', spaces)}&Messages = {bc}.GetMessages()",
        $"{new string(' ', spaces)}msg(Format(!\"Genexus Open API Builder B055 BC failure: %1\", &Messages.ToJson()), status)",
    };

    private static IReadOnlyList<VariableSpec> CreateVariables(ApiPlan plan) => new[]
    {
        new VariableSpec("CreateRequest", plan.CreateRequestSdtName),
        new VariableSpec("CreateResponse", plan.ResponseSdtName),
        new VariableSpec(plan.TransactionName, plan.TransactionName),
        new VariableSpec("Messages", "Messages, GeneXus.Common"),
    };

    private static IReadOnlyList<VariableSpec> UpdateVariables(ApiPlan plan) => plan.PrimaryKey.Select(field => new VariableSpec(field.Name, $"Attribute:{field.Name}"))
        .Concat(new[]
        {
            new VariableSpec("UpdateRequest", plan.UpdateRequestSdtName),
            new VariableSpec("UpdateResponse", plan.ResponseSdtName),
            new VariableSpec(plan.TransactionName, plan.TransactionName),
            new VariableSpec("Messages", "Messages, GeneXus.Common"),
        })
        .ToArray();

    private static IReadOnlyList<VariableSpec> ApiVariableSpecs(ApiPlan plan) => plan.PrimaryKey.Select(field => new VariableSpec(field.Name, $"Attribute:{field.Name}"))
        .Concat(new[]
        {
            new VariableSpec("CreateRequest", plan.CreateRequestSdtName),
            new VariableSpec("CreateResponse", plan.ResponseSdtName),
            new VariableSpec("UpdateRequest", plan.UpdateRequestSdtName),
            new VariableSpec("UpdateResponse", plan.ResponseSdtName),
        })
        .ToArray();

    private static string ApiVariables(ApiPlan plan) => string.Join(Environment.NewLine, ApiVariableSpecs(plan).Select(variable => $"{variable.Name} [ DataType = '{variable.DataType}' ]"));

    private static string CreateRules() => "parm(in:&CreateRequest, out:&CreateResponse);";
    private static string UpdateRules(ApiPlan plan) => $"parm({string.Join(", ", plan.PrimaryKey.Select(field => $"in:&{field.Name}").Concat(new[] { "in:&UpdateRequest", "out:&UpdateResponse" }))});";
    private static string LoadArguments(ApiPlan plan, string prefix) => string.Join(", ", plan.PrimaryKey.Select(field => prefix == "&" ? $"&{field.Name}" : $"{prefix}.{field.Name}"));
    private static bool HasService(ApiPlan plan, string name) => plan.Services.Any(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
    private static string Skeleton(string backlog, string service) => $"// Genexus Open API Builder {backlog}: Procedure skeleton for {service}. REST behavior remains pending Sprint 6." + Environment.NewLine + $"msg(!\"Genexus Open API Builder {backlog} {service} skeleton. REST behavior pending Sprint 6.\", status)";
    private static string B054Source(ApiPlan plan) => $"{plan.ApiName}{Environment.NewLine}{{{Environment.NewLine}{string.Join(Environment.NewLine + Environment.NewLine, plan.Services.Select(service => $"    {service.Name}(){Environment.NewLine}        => proc{plan.TransactionName}_API_{service.Name}();"))}{Environment.NewLine}}}";

    private static string ServiceSource(ApiPlan plan, string service)
    {
        var procedure = $"proc{plan.TransactionName}_API_{service}";
        if (string.Equals(service, "Create", StringComparison.OrdinalIgnoreCase))
            return $"    Create(in: &CreateRequest, out: &CreateResponse){Environment.NewLine}        => {procedure}(&CreateRequest, &CreateResponse);";
        if (string.Equals(service, "Update", StringComparison.OrdinalIgnoreCase))
        {
            var parameters = string.Join(", ", plan.PrimaryKey.Select(field => $"in: &{field.Name}").Concat(new[] { "in: &UpdateRequest", "out: &UpdateResponse" }));
            var arguments = string.Join(", ", plan.PrimaryKey.Select(field => $"&{field.Name}").Concat(new[] { "&UpdateRequest", "&UpdateResponse" }));
            return $"    Update({parameters}){Environment.NewLine}        => {procedure}({arguments});";
        }
        return $"    {service}(){Environment.NewLine}        => {procedure}();";
    }
}

internal sealed class VariableSpec
{
    public VariableSpec(string name, string dataType)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
    }

    public string Name { get; }
    public string DataType { get; }
}

internal sealed class ApiPlanBusinessComponentWriteResult
{
    public ApiPlanBusinessComponentWriteResult(Guid createProcedureGuid, Guid updateProcedureGuid, Guid apiObjectGuid, int primaryKeyParts, int createFields, int updateFields, int responseFields)
    {
        CreateProcedureGuid = createProcedureGuid;
        UpdateProcedureGuid = updateProcedureGuid;
        ApiObjectGuid = apiObjectGuid;
        PrimaryKeyParts = primaryKeyParts;
        CreateFields = createFields;
        UpdateFields = updateFields;
        ResponseFields = responseFields;
    }

    public Guid CreateProcedureGuid { get; }
    public Guid UpdateProcedureGuid { get; }
    public Guid ApiObjectGuid { get; }
    public int PrimaryKeyParts { get; }
    public int CreateFields { get; }
    public int UpdateFields { get; }
    public int ResponseFields { get; }
}
