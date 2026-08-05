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

    /// <summary>
    /// Propriedade publica 'Required' de variavel de parametro de servico. Aplicada apenas nas
    /// variaveis de request do API Object: e a unica marcacao que o gerador de YAML honra,
    /// emitindo 'requestBody: required: true'.
    /// </summary>
    internal const string ServiceRequiredPropertyId = "idVarServiceRequired";

    public static ApiPlanBusinessComponentWriteResult Apply(KBModel model, Transaction transaction, ApiPlan plan)
    {
        if (model is null) throw new ArgumentNullException(nameof(model));
        if (transaction is null) throw new ArgumentNullException(nameof(transaction));
        if (plan is null) throw new ArgumentNullException(nameof(plan));
        if (!string.Equals(transaction.Name, plan.TransactionName, StringComparison.Ordinal))
            throw new InvalidOperationException("B055 bloqueado: o ApiPlan nao pertence a Transaction atual. Nenhuma alteracao foi feita.");
        if (!transaction.IsBusinessComponent)
            throw new InvalidOperationException($"B055 bloqueado: Transaction='{transaction.Name}' esta com Business Component desabilitado. Nenhuma alteracao foi feita.");
        if (!HasService(plan, "Get") || !HasService(plan, "Create") || !HasService(plan, "Update"))
            throw new InvalidOperationException("B071-B073/B079 bloqueado: o ApiPlan precisa conter Get, Create e Update. Nenhuma alteracao foi feita.");

        EnsureSdts(model, plan);
        ApiPlanTransactionFolder.Preflight(model, plan);
        var getContent = GetContent(plan);
        var getRules = GetRules(plan);
        var getVariables = CoalesceVariableSpecs(GetVariables(plan), "B071-B073/B079");
        var createContent = CreateContent(plan);
        var createRules = CreateRules();
        var createVariables = CoalesceVariableSpecs(CreateVariables(plan), "B071-B073/B079");
        var updateContent = UpdateContent(plan);
        var updateRules = UpdateRules(plan);
        var updateVariables = CoalesceVariableSpecs(UpdateVariables(plan), "B071-B073/B079");
        var apiSource = CreateB055ServiceGroupSource(plan);
        var apiVariables = CoalesceVariableSpecs(ApiVariableSpecs(plan), "B071-B073/B079");

        var get = FindProcedure(model, plan, "Get", "B051");
        var create = FindProcedure(model, plan, "Create", "B052");
        var update = FindProcedure(model, plan, "Update", "B053");
        var api = FindApi(model, plan);
        EnsureProcedure(get, plan, "B051", "Get", Skeleton("B051", "Get"), getContent, getVariables, getRules, IsManagedGetSource);
        EnsureProcedure(create, plan, "B052", "Create", Skeleton("B052", "Create"), createContent, createVariables, createRules, IsManagedCreateSource, LegacyCreateContent(plan), LegacyCreateRules(), LegacyCreateVariables(plan), PreviousB079CreateVariables(plan));
        EnsureProcedure(update, plan, "B053", "Update", Skeleton("B053", "Update"), updateContent, updateVariables, updateRules, IsManagedUpdateSource, LegacyUpdateContent(plan), LegacyUpdateRules(plan), LegacyUpdateVariables(plan), PreviousB079UpdateVariables(plan));
        EnsureApi(api, plan);
        ValidateProcedureVariableSpecs(model, get, getVariables);
        ValidateProcedureVariableSpecs(model, create, createVariables);
        ValidateProcedureVariableSpecs(model, update, updateVariables);
        ValidateApiVariableSpecs(model, api, apiVariables);

        ApiPlanSdtWriter.CreateOrReencounter(model, transaction, plan);
        var transactionFolder = ApiPlanTransactionFolder.CreateOrReencounter(model, transaction, plan);
        SaveApi(model, api, transactionFolder, plan, apiSource, apiVariables);
        SaveProcedure(model, get, getContent, getVariables, getRules);
        SaveProcedure(model, create, createContent, createVariables, createRules);
        SaveProcedure(model, update, updateContent, updateVariables, updateRules);
        return new ApiPlanBusinessComponentWriteResult(get.Guid, create.Guid, update.Guid, api.Guid, plan.PrimaryKey.Count, plan.CreateRequestFields.Count, plan.UpdateRequestFields.Count, plan.ResponseFields.Count);
    }

    internal static bool IsManagedApiObject(KBModel model, ApiPlan plan, API api)
    {
        if (plan is null) throw new ArgumentNullException(nameof(plan));
        if (api is null) throw new ArgumentNullException(nameof(api));
        var source = NormalizeForComparison(api.ServiceGroupSource.Source);
        return ApiPlanListProcedureWriter.IsB070ApiObject(model, plan, api)
            || IsB054ServiceGroupSource(plan, source)
            || (HasNoNonStandardVariables(api) && IsSemanticallyB054ServiceGroupSource(plan, source))
            || (IsB055ServiceGroupSource(plan, source) && HasManagedB055ApiVariables(model, plan, api) && HasExpectedApiEvents(api));
    }

    internal static bool IsB055ApiObject(KBModel model, ApiPlan plan, API api) =>
        api is not null && IsB055ServiceGroupSource(plan, NormalizeForComparison(api.ServiceGroupSource.Source)) &&
        HasManagedB055ApiVariables(model, plan, api) &&
        HasExpectedApiEvents(api);

    internal static bool IsCurrentB055ApiObject(KBModel model, ApiPlan plan, API api) =>
        api is not null &&
        ((string.Equals(NormalizeForComparison(api.ServiceGroupSource.Source), NormalizeForComparison(CreateB055ServiceGroupSource(plan)), StringComparison.Ordinal) &&
          HasExpectedVariables(model, api, CoalesceVariableSpecs(ApiVariableSpecs(plan), "B071-B073/B079")) &&
          HasExpectedApiEvents(api)) ||
         (IsSemanticallyB055ServiceGroupSource(plan, NormalizeForComparison(api.ServiceGroupSource.Source)) &&
          HasExpectedVariables(model, api, CoalesceVariableSpecs(LegacyApiVariableSpecs(plan), "B055 legacy")) &&
          string.IsNullOrWhiteSpace(api.Events.Source)));

    internal static string CreateB054ServiceGroupSource(ApiPlan plan) => CreateServiceGroupSource(plan, includeBusinessComponentParameters: false, includeDescriptions: true);

    internal static string CreateB055ServiceGroupSource(ApiPlan plan) => CreateServiceGroupSource(plan, includeBusinessComponentParameters: true, includeDescriptions: true, exposeErrorResponse: true);

    internal static string CreateB079InternalErrorOnlyServiceGroupSource(ApiPlan plan) => CreateServiceGroupSource(plan, includeBusinessComponentParameters: true, includeDescriptions: true);

    private static bool IsB054ServiceGroupSource(ApiPlan plan, string normalizedSource) =>
        string.Equals(normalizedSource, NormalizeForComparison(CreateB054ServiceGroupSource(plan)), StringComparison.Ordinal) ||
        string.Equals(normalizedSource, NormalizeForComparison(CreateLegacyB054ServiceGroupSource(plan)), StringComparison.Ordinal);

    private static bool IsB055ServiceGroupSource(ApiPlan plan, string normalizedSource) =>
        string.Equals(normalizedSource, NormalizeForComparison(CreateB055ServiceGroupSource(plan)), StringComparison.Ordinal) ||
        string.Equals(normalizedSource, NormalizeForComparison(CreateLegacyB055ServiceGroupSource(plan)), StringComparison.Ordinal) ||
        IsSemanticallyB055ServiceGroupSource(plan, normalizedSource);

    private static bool HasNoNonStandardVariables(API api) => !api.Variables.Variables.Any(variable => !variable.IsStandard);

    private static bool IsSemanticallyB054ServiceGroupSource(ApiPlan plan, string normalizedSource)
    {
        return ApiPlanServiceSourceContract.MatchesB054(
            normalizedSource,
            plan.ApiName,
            plan.TransactionName,
            plan.ModuleTarget,
            plan.Services.Select(service => service.Name));
    }

    private static bool IsSemanticallyB055ServiceGroupSource(ApiPlan plan, string normalizedSource)
    {
        return ApiPlanServiceSourceContract.MatchesB055(
            normalizedSource,
            plan.ApiName,
            plan.TransactionName,
            plan.ModuleTarget,
            plan.Services.Select(service => service.Name),
            plan.PrimaryKey.Select(field => field.Name)) ||
            ApiPlanServiceSourceContract.MatchesB079(
                normalizedSource,
                plan.ApiName,
                plan.TransactionName,
                plan.ModuleTarget,
                plan.Services.Select(service => service.Name),
                plan.PrimaryKey.Select(field => field.Name),
                Array.Empty<string>(),
                hasListContract: false) ||
            ApiPlanServiceSourceContract.MatchesB079InternalErrorOnly(
                normalizedSource,
                plan.ApiName,
                plan.TransactionName,
                plan.ModuleTarget,
                plan.Services.Select(service => service.Name),
                plan.PrimaryKey.Select(field => field.Name),
                Array.Empty<string>(),
                hasListContract: false) ||
            ApiPlanServiceSourceContract.MatchesPreviousB079SecurityLevelContract(
                normalizedSource,
                plan.ApiName,
                plan.TransactionName,
                plan.ModuleTarget,
                plan.Services.Select(service => service.Name),
                plan.PrimaryKey.Select(field => field.Name),
                Array.Empty<string>(),
                hasListContract: false) ||
            ApiPlanServiceSourceContract.MatchesPreviousB079RestMethodContract(
                normalizedSource,
                plan.ApiName,
                plan.TransactionName,
                plan.ModuleTarget,
                plan.Services.Select(service => service.Name),
                plan.PrimaryKey.Select(field => field.Name),
                Array.Empty<string>(),
                hasListContract: false);
    }

    private static bool ContainsB055ServiceCall(string compactSource, ApiPlan plan, string serviceName)
    {
        var servicePrefix = B055ServiceSignature(plan, serviceName) + "=>";
        if (!TryReadProcedureCall(compactSource, servicePrefix, out var calledObject, out var calledArguments))
        {
            return false;
        }

        var procedure = $"proc{plan.TransactionName}_API_{serviceName}";
        return IsExpectedProcedureName(plan, calledObject, procedure) &&
            string.Equals(calledArguments, B055ProcedureArguments(plan, serviceName), StringComparison.Ordinal);
    }
    private static bool ContainsB054ServiceCall(string compactSource, ApiPlan plan, string serviceName)
    {
        var servicePrefix = serviceName + "()=>";
        if (!TryReadProcedureCall(compactSource, servicePrefix, out var calledObject, out var calledArguments))
        {
            return false;
        }

        var procedure = $"proc{plan.TransactionName}_API_{serviceName}";
        return IsExpectedProcedureName(plan, calledObject, procedure) && string.IsNullOrEmpty(calledArguments);
    }

    private static bool TryReadProcedureCall(string compactSource, string servicePrefix, out string calledObject, out string calledArguments)
    {
        calledObject = string.Empty;
        calledArguments = string.Empty;

        var serviceIndex = compactSource.IndexOf(servicePrefix, StringComparison.Ordinal);
        if (serviceIndex < 0)
        {
            return false;
        }

        var callStart = serviceIndex + servicePrefix.Length;
        var argumentsStart = compactSource.IndexOf("(", callStart, StringComparison.Ordinal);
        if (argumentsStart < 0)
        {
            return false;
        }

        var argumentsEnd = compactSource.IndexOf(");", argumentsStart, StringComparison.Ordinal);
        if (argumentsEnd < 0)
        {
            return false;
        }

        calledObject = compactSource.Substring(callStart, argumentsStart - callStart);
        calledArguments = compactSource.Substring(argumentsStart + 1, argumentsEnd - argumentsStart - 1);
        return true;
    }

    private static bool IsExpectedProcedureName(ApiPlan plan, string calledObject, string procedure)
    {
        return string.Equals(calledObject, ExpectedProcedureReference(plan, procedure), StringComparison.Ordinal);
    }

    private static string ExpectedProcedureReference(ApiPlan plan, string procedure)
    {
        if (string.IsNullOrWhiteSpace(plan.ModuleTarget) || string.Equals(plan.ModuleTarget, "Root Module", StringComparison.Ordinal))
        {
            return procedure;
        }

        return plan.ModuleTarget + "." + procedure;
    }

    private static string B055ServiceSignature(ApiPlan plan, string serviceName)
    {
        if (string.Equals(serviceName, "Create", StringComparison.OrdinalIgnoreCase))
        {
            return "Create(in:&CreateRequest,out:&CreateResponse)";
        }

        if (string.Equals(serviceName, "Update", StringComparison.OrdinalIgnoreCase))
        {
            var parameters = string.Join(",", plan.PrimaryKey.Select(field => $"in:&{field.Name}").Concat(new[] { "in:&UpdateRequest", "out:&UpdateResponse" }));
            return "Update(" + parameters + ")";
        }

        return serviceName + "()";
    }

    private static string B055ProcedureArguments(ApiPlan plan, string serviceName)
    {
        if (string.Equals(serviceName, "Create", StringComparison.OrdinalIgnoreCase))
        {
            return "&CreateRequest,&CreateResponse";
        }

        if (string.Equals(serviceName, "Update", StringComparison.OrdinalIgnoreCase))
        {
            return string.Join(",", plan.PrimaryKey.Select(field => $"&{field.Name}").Concat(new[] { "&UpdateRequest", "&UpdateResponse" }));
        }

        return string.Empty;
    }
    private static int CountOccurrences(string value, string token)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }

    private static string RemoveWhitespace(string value) => new(value.Where(character => !char.IsWhiteSpace(character)).ToArray());
    private static string CreateLegacyB054ServiceGroupSource(ApiPlan plan) => CreateServiceGroupSource(plan, includeBusinessComponentParameters: false, includeDescriptions: false);

    private static string CreateLegacyB055ServiceGroupSource(ApiPlan plan) => CreateServiceGroupSource(plan, includeBusinessComponentParameters: true, includeDescriptions: false);

    private static string CreateServiceGroupSource(ApiPlan plan, bool includeBusinessComponentParameters, bool includeDescriptions, bool exposeErrorResponse = false)
    {
        var services = plan.Services.Select(service => ServiceSource(plan, service.Name, includeBusinessComponentParameters, includeDescriptions, exposeErrorResponse));
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

        if (!HasExpectedApiEvents(api))
        {
            throw new InvalidOperationException($"B071-B073/B079 bloqueado: API Object proprio '{api.Name}' possui Events divergentes da geracao REST runtime. Nenhuma alteracao foi feita.");
        }
    }

    private static bool HasManagedB055ApiVariables(KBModel model, ApiPlan plan, API api) =>
        HasExpectedVariables(model, api, CoalesceVariableSpecs(ApiVariableSpecs(plan), "B071-B073/B079")) ||
        HasExpectedVariables(model, api, CoalesceVariableSpecs(LegacyApiVariableSpecs(plan), "B055 legacy"));

    private static void EnsureProcedure(
        Procedure procedure,
        ApiPlan plan,
        string backlog,
        string service,
        string skeleton,
        string content,
        IReadOnlyList<VariableSpec> variables,
        string rules,
        Func<string, ApiPlan, bool>? isManagedCurrentSource = null,
        string? legacyContent = null,
        string? legacyRules = null,
        IReadOnlyList<VariableSpec>? legacyVariables = null,
        IReadOnlyList<VariableSpec>? previousB079Variables = null)
    {
        var currentSource = NormalizeForComparison(procedure.ProcedurePart.Source);
        if (!string.IsNullOrWhiteSpace(currentSource) &&
            !string.Equals(currentSource, NormalizeForComparison(skeleton), StringComparison.Ordinal) &&
            !string.Equals(currentSource, NormalizeForComparison(content), StringComparison.Ordinal) &&
            (isManagedCurrentSource is null || !isManagedCurrentSource(currentSource, plan)) &&
            (string.IsNullOrWhiteSpace(legacyContent) || !string.Equals(currentSource, NormalizeForComparison(legacyContent), StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"B055 bloqueado: Procedure propria '{procedure.Name}' possui Source divergente da geracao {backlog}/{service}. Nenhuma alteracao foi feita.");
        }

        var currentRules = NormalizeForComparison(procedure.Rules.Source);
        if (!string.IsNullOrWhiteSpace(currentRules) &&
            !string.Equals(currentRules, NormalizeForComparison(rules), StringComparison.Ordinal) &&
            (string.IsNullOrWhiteSpace(legacyRules) || !string.Equals(currentRules, NormalizeForComparison(legacyRules), StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"B055 bloqueado: Procedure propria '{procedure.Name}' possui Rules divergentes da geracao {backlog}/{service}. Nenhuma alteracao foi feita.");
        }

        var currentVariables = procedure.Variables.Variables
            .Where(variable => !variable.IsStandard)
            .Select(variable => variable.Name)
            .ToArray();
        if (currentVariables.Length == 0)
        {
            if (IsPreB055ProcedureSkeleton(currentSource, currentRules, skeleton))
            {
                return;
            }

            throw new InvalidOperationException($"B055 bloqueado: Procedure propria '{procedure.Name}' nao possui as variaveis esperadas da geracao {backlog}/{service}. Nenhuma alteracao foi feita.");
        }

        if (!HasExpectedVariables(procedure.Model, procedure, variables) &&
            (legacyVariables is null || !HasExpectedVariables(procedure.Model, procedure, legacyVariables)) &&
            (previousB079Variables is null || !HasMigrablePreviousB079Variables(procedure.Model, procedure, previousB079Variables)))
        {
            throw new InvalidOperationException($"B055 bloqueado: Procedure propria '{procedure.Name}' possui variaveis divergentes da geracao {backlog}/{service}. Nenhuma alteracao foi feita.");
        }
    }

    private static string NormalizeForComparison(string? value)
    {
        return (value ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n").Trim();
    }

    private static bool IsPreB055ProcedureSkeleton(string currentSource, string currentRules, string skeleton) =>
        string.Equals(currentSource, NormalizeForComparison(skeleton), StringComparison.Ordinal) && string.IsNullOrWhiteSpace(currentRules);

    private static bool IsManagedGetSource(string source, ApiPlan plan)
    {
        return HasEquivalentGeneratedSource(source, GetContent(plan));
    }

    private static bool IsManagedCreateSource(string source, ApiPlan plan)
    {
        return HasEquivalentGeneratedSource(source, CreateContent(plan)) ||
            HasEquivalentGeneratedSource(source, PreviousB079CreateContentWithGenericToStringLocationHeader(plan)) ||
            HasEquivalentGeneratedSource(source, PreviousB079CreateContentWithoutLocationHeader(plan)) ||
            HasEquivalentGeneratedSource(source, PreviousB079CreateContentWithNativeJsonValidation(plan)) ||
            HasEquivalentGeneratedSource(source, PreviousB079CreateContentWithSdtDirtyValidation(plan)) ||
            HasEquivalentGeneratedSource(source, PreviousB079CreateContentWithOriginalMemberDirtyValidation(plan)) ||
            HasEquivalentGeneratedSource(source, PreviousB079CreateContentWithUnwrappedRequiredMemberValidation(plan)) ||
            HasEquivalentGeneratedSource(source, PreviousB079CreateContentWithWrappedRequiredMemberValidation(plan)) ||
            HasEquivalentGeneratedSource(source, PreviousB079CreateContentWithNewtonsoftRequiredMemberValidation(plan)) ||
            HasEquivalentGeneratedSource(source, PreviousB079CreateContentWithoutRequiredMemberValidation(plan)) ||
            HasEquivalentGeneratedSource(source, PreviousB079CreateContentWithoutCommit(plan)) ||
            HasEquivalentGeneratedSource(source, PreviousB079CreateContent(plan));
    }

    private static bool IsManagedUpdateSource(string source, ApiPlan plan)
    {
        return HasEquivalentGeneratedSource(source, UpdateContent(plan)) ||
            HasEquivalentGeneratedSource(source, PreviousB079UpdateContentWithNativeJsonValidation(plan)) ||
            HasEquivalentGeneratedSource(source, PreviousB079UpdateContentWithSdtDirtyValidation(plan)) ||
            HasEquivalentGeneratedSource(source, PreviousB079UpdateContentWithOriginalMemberDirtyValidation(plan)) ||
            HasEquivalentGeneratedSource(source, PreviousB079UpdateContentWithUnwrappedRequiredMemberValidation(plan)) ||
            HasEquivalentGeneratedSource(source, PreviousB079UpdateContentWithWrappedRequiredMemberValidation(plan)) ||
            HasEquivalentGeneratedSource(source, PreviousB079UpdateContentWithNewtonsoftRequiredMemberValidation(plan)) ||
            HasEquivalentGeneratedSource(source, PreviousB079UpdateContentWithoutRequiredMemberValidation(plan)) ||
            HasEquivalentGeneratedSource(source, PreviousB079UpdateContentWithoutCommit(plan)) ||
            HasEquivalentGeneratedSource(source, PreviousB079UpdateContent(plan));
    }

    private static bool HasEquivalentGeneratedSource(string source, string expectedSource)
    {
        return string.Equals(RemoveWhitespace(source), RemoveWhitespace(expectedSource), StringComparison.Ordinal);
    }

    private static void SaveProcedure(KBModel model, Procedure procedure, string content, IReadOnlyList<VariableSpec> variables, string rules)
    {
        ReplaceVariables(model, procedure, variables);
        procedure.Rules.Source = rules;
        procedure.ProcedurePart.Source = content;
        try
        {
            procedure.Save();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"B071-B073/B079 falhou ao salvar a Procedure '{procedure.Name}': {ex.Message}. {DescribeProcedureSaveState(procedure, variables)}",
                ex);
        }

        var persisted = Procedure.Get(model, procedure.Guid);
        if (!HasEquivalentGeneratedSource(persisted.ProcedurePart.Source, content))
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

    private static string DescribeProcedureSaveState(Procedure procedure, IReadOnlyList<VariableSpec> variables)
    {
        var expectedVariables = string.Join(", ", variables.Select(variable => $"&{variable.Name}:{variable.DataType}"));
        var currentVariables = string.Join(", ", procedure.Variables.Variables
            .Where(variable => !variable.IsStandard)
            .Select(variable => $"&{variable.Name}:Type={variable.Type};ATTCUSTOMTYPE={variable.GetPropertyValueString("ATTCUSTOMTYPE")};KBObject={variable.KBObject?.Name ?? string.Empty};Domain={variable.DomainBasedOn?.Name ?? string.Empty};Attribute={variable.AttributeBasedOn?.Name ?? string.Empty}"));
        var relevantSource = ExtractRelevantProcedureLines(procedure.ProcedurePart.Source);
        return $"Rules='{procedure.Rules.Source}'. ExpectedVariables=[{expectedVariables}]. CurrentVariables=[{currentVariables}]. SourceLines=[{relevantSource}]";
    }

    private static string ExtractRelevantProcedureLines(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return string.Empty;
        }

        return string.Join(" | ", source
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Select(line => line.Trim())
            .Where(line =>
                line.IndexOf("ErrorResponse", StringComparison.Ordinal) >= 0 ||
                line.IndexOf("Messages", StringComparison.Ordinal) >= 0 ||
                line.IndexOf("RestStatusCode", StringComparison.Ordinal) >= 0)
            .Take(30));
    }

    private static void SaveApi(KBModel model, API api, Folder transactionFolder, ApiPlan plan, string source, IReadOnlyList<VariableSpec> variables)
    {
        api.Parent = transactionFolder;
        api.ServiceGroupSource.Source = source;
        api.Events.Source = CreateB079ApiEvents();
        ReplaceVariables(model, api, variables);
        api.Save();

        var persisted = API.Get(model, api.Guid);
        if (!IsB055ServiceGroupSource(plan, NormalizeForComparison(persisted.ServiceGroupSource.Source)))
        {
            throw new InvalidOperationException($"B055 bloqueado: o API Object '{api.Name}' foi salvo, mas o Service Source persistido nao corresponde ao contrato API/Procedure planejado. Nenhuma outra alteracao sera feita.");
        }

        if (!HasExpectedVariables(model, persisted, variables) || !HasExpectedApiEvents(persisted))
        {
            throw new InvalidOperationException($"B055 bloqueado: o API Object '{api.Name}' foi salvo, mas eventos ou variaveis persistidas nao correspondem ao contrato API/Procedure planejado. Nenhuma outra alteracao sera feita.");
        }
    }

    private static void ValidateProcedureVariableSpecs(KBModel model, Procedure procedure, IReadOnlyList<VariableSpec> variables)
    {
        foreach (var variable in variables)
        {
            var item = new Variable(variable.Name, procedure.Variables);
            if (!TrySetVariableType(model, item, variable.DataType))
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
            if (!TrySetVariableType(model, item, variable.DataType))
            {
                throw new InvalidOperationException($"B055 bloqueado: tipo da variavel de API '&{variable.Name}' nao foi resolvido antes da escrita: '{variable.DataType}'. Nenhuma alteracao foi feita.");
            }
        }
    }

    internal static void ReplaceVariables(KBModel model, Procedure procedure, IReadOnlyList<VariableSpec> variables)
    {
        foreach (var existing in procedure.Variables.Variables.Where(variable => !variable.IsStandard).ToArray())
        {
            procedure.Variables.Variables.Remove(existing);
        }

        foreach (var variable in variables)
        {
            var item = new Variable(variable.Name, procedure.Variables);
            if (!TrySetVariableType(model, item, variable.DataType))
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
            if (!TrySetVariableType(model, item, variable.DataType))
            {
                throw new InvalidOperationException($"B055 bloqueado: tipo da variavel de API '&{variable.Name}' nao foi resolvido: '{variable.DataType}'. Nenhuma alteracao foi feita.");
            }

            ConfigureServiceRequired(item, variable);
            api.Variables.Variables.Add(item);
        }
    }

    private static void ConfigureServiceRequired(Variable variable, VariableSpec spec)
    {
        if (!spec.IsServiceRequired || !variable.ContainsPropertyDefinition(ServiceRequiredPropertyId))
        {
            return;
        }

        variable.SetPropertyValue(ServiceRequiredPropertyId, true);
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

    private static bool HasMigrablePreviousB079Variables(KBModel model, Procedure procedure, IReadOnlyList<VariableSpec> variables)
    {
        var currentVariables = procedure.Variables.Variables
            .Where(variable => !variable.IsStandard)
            .Select(variable => variable.Name)
            .ToArray();
        var fullVariables = variables.ToArray();
        var variablesWithoutNestedErrorItems = variables
            .Where(variable =>
                !string.Equals(variable.Name, "ErrorItem", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(variable.Name, "Message", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        return MatchesVariableSet(model, procedure, currentVariables, fullVariables, skipTypeCheckForErrorItem: true) ||
            MatchesVariableSet(model, procedure, currentVariables, variablesWithoutNestedErrorItems, skipTypeCheckForErrorItem: false) ||
            MatchesVariableSetAllowingRequiredMemberMigrationVariables(model, procedure, currentVariables, variablesWithoutNestedErrorItems);
    }

    private static bool MatchesVariableSet(KBModel model, Procedure procedure, IReadOnlyList<string> currentVariables, IReadOnlyList<VariableSpec> variables, bool skipTypeCheckForErrorItem)
    {
        var expectedVariables = new HashSet<string>(variables.Select(variable => variable.Name), StringComparer.OrdinalIgnoreCase);
        return currentVariables.Count == expectedVariables.Count &&
            currentVariables.All(variable => expectedVariables.Contains(variable)) &&
            variables
                .Where(variable => !skipTypeCheckForErrorItem || !string.Equals(variable.Name, "ErrorItem", StringComparison.OrdinalIgnoreCase))
                .All(variable => MatchesVariableSpec(model, procedure, variable));
    }

    private static bool MatchesVariableSetAllowingRequiredMemberMigrationVariables(KBModel model, Procedure procedure, IReadOnlyList<string> currentVariables, IReadOnlyList<VariableSpec> baseVariables)
    {
        var optionalMigrationVariables = new[]
        {
            new VariableSpec("HttpRequest", "HttpRequest"),
            new VariableSpec("RequestBody", "LongVarChar"),
            new VariableSpec("RequestProperties", "Properties"),
            new VariableSpec("RequestPayloadJson", "LongVarChar"),
            new VariableSpec("RequestPayloadProperties", "Properties"),
            new VariableSpec("JsonProperty", "Property"),
            new VariableSpec("RequiredMemberFound", "Boolean"),
            new VariableSpec("RequestJsonHasRequiredMembers", "Boolean"),
            new VariableSpec("MissingRequiredFields", "VarChar(1K)"),
        };
        var allowedVariables = baseVariables.Concat(optionalMigrationVariables).ToArray();

        // Procedures geradas antes da validacao por valor default nao possuem a instancia vazia do SDT de
        // request, entao ela nao pode ser exigida para reconhecer o objeto como migravel.
        var absentInPreviousVariants = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            EmptyRequestVariableName("CreateRequest"),
            EmptyRequestVariableName("UpdateRequest"),
        };
        var baseNames = new HashSet<string>(
            baseVariables
                .Where(variable => !absentInPreviousVariants.Contains(variable.Name))
                .Select(variable => variable.Name),
            StringComparer.OrdinalIgnoreCase);
        var allowedNames = new HashSet<string>(allowedVariables.Select(variable => variable.Name), StringComparer.OrdinalIgnoreCase);
        return baseNames.All(name => currentVariables.Any(current => string.Equals(current, name, StringComparison.OrdinalIgnoreCase))) &&
            currentVariables.All(variable => allowedNames.Contains(variable)) &&
            allowedVariables
                .Where(variable => currentVariables.Any(current => string.Equals(current, variable.Name, StringComparison.OrdinalIgnoreCase)))
                .All(variable => MatchesVariableSpec(model, procedure, variable));
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
        if (!TrySetVariableType(model, expected, variable.DataType))
        {
            return false;
        }

        return MatchesVariableSpec(current, expected);
    }

    private static bool MatchesVariableSpec(KBModel model, API api, VariableSpec variable)
    {
        var current = api.Variables.GetVariable(variable.Name, false);
        if (current is null)
        {
            return false;
        }

        var expected = new Variable(variable.Name, api.Variables);
        if (!TrySetVariableType(model, expected, variable.DataType))
        {
            return false;
        }

        return MatchesVariableSpec(current, expected);
    }

    private static bool MatchesVariableSpec(Variable current, Variable expected) =>
        current.Type == expected.Type &&
        SameKbObject(current.AttributeBasedOn, expected.AttributeBasedOn) &&
        SameKbObject(current.DomainBasedOn, expected.DomainBasedOn) &&
        Equals(current.DomainKey, expected.DomainKey) &&
        SameKbObject(current.KBObject, expected.KBObject) &&
        string.Equals(current.GetPropertyValueString("ATTCUSTOMTYPE"), expected.GetPropertyValueString("ATTCUSTOMTYPE"), StringComparison.Ordinal);

    private static bool TrySetVariableType(KBModel model, Variable variable, string dataType) =>
        TrySetAttributeBasedOn(model, variable, dataType) ||
        DataType.ParseInto(model, dataType, variable);

    private static bool SameKbObject(KBObject? current, KBObject? expected)
    {
        if (current is null || expected is null)
        {
            return current is null && expected is null;
        }

        return current.Guid == expected.Guid &&
            string.Equals(current.Name, expected.Name, StringComparison.OrdinalIgnoreCase);
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

    internal static string CreateContent(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var requiredFields = RequiredFieldsFor(plan, "CreateRequest", plan.CreateRequestFields);
        var guarded = requiredFields.Count > 0;
        var bodyIndent = guarded ? "    " : string.Empty;
        var successIndent = guarded ? 8 : 4;
        var lines = new List<string> { "&RestStatusCode = 201" };
        lines.AddRange(RequiredMemberPresenceValidation("CreateRequest", "&CreateRequest", requiredFields, 0));
        if (guarded)
        {
            lines.Add("If &RestStatusCode = 201");
        }

        lines.Add($"{bodyIndent}{bc} = new()");
        lines.AddRange(plan.CreateRequestFields.Select(field => $"{bodyIndent}{bc}.{field.Name} = &CreateRequest.{field.Name}"));
        lines.Add($"{bodyIndent}{bc}.Save()");
        lines.Add($"{bodyIndent}If {bc}.Success()");
        lines.Add($"{bodyIndent}    Commit");
        lines.Add($"{bodyIndent}    {bc}.Load({LoadArguments(plan, bc)})");
        lines.Add($"{bodyIndent}    &CreateResponse = new()");
        lines.AddRange(ResponseAssignments(plan, bc, "&CreateResponse", successIndent));
        lines.Add($"{bodyIndent}    &HttpResponse.AddHeader(!\"Location\", {CreateLocationUrlExpression(plan, bc)})");
        lines.Add($"{bodyIndent}    &RestStatusCode = 201");
        lines.Add($"{bodyIndent}Else");
        lines.AddRange(BusinessRuleFailureMessages(bc, successIndent));
        lines.Add($"{bodyIndent}EndIf");
        if (guarded)
        {
            lines.Add("EndIf");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string CreateLocationUrlExpression(ApiPlan plan, string bc)
    {
        var basePath = plan.RestPath.TrimEnd('/');
        var keyParts = plan.PrimaryKey.Select(field => PrimaryKeyLocationPartExpression(bc, field));
        return $"!\"{basePath}/\" + " + string.Join(" + !\"/\" + ", keyParts);
    }

    private static string PrimaryKeyLocationPartExpression(string bc, ApiPlanField field)
    {
        var member = $"{bc}.{field.Name}";
        var dataType = field.DataType?.Trim() ?? string.Empty;
        if (string.Equals(dataType, "Date", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "DateTime", StringComparison.OrdinalIgnoreCase))
        {
            return $"PadL(Trim(Str(Year({member}))), 4, !\"0\") + !\"-\" + PadL(Trim(Str(Month({member}))), 2, !\"0\") + !\"-\" + PadL(Trim(Str(Day({member}))), 2, !\"0\")";
        }

        if (string.Equals(dataType, "VarChar", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "LongVarChar", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "Character", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "Char", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dataType, "String", StringComparison.OrdinalIgnoreCase))
        {
            return $"URLEncode({member}.Trim())";
        }

        return $"{member}.ToString().Trim()";
    }

    internal static string GetContent(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var lines = new List<string>
        {
            "&RestStatusCode = 200",
            $"{bc}.Load({LoadArguments(plan, "&")})",
            $"If {bc}.Success()",
            "    &GetResponse = new()",
        };
        lines.AddRange(ResponseAssignments(plan, bc, "&GetResponse", 4));
        lines.Add("    &RestStatusCode = 200");
        lines.Add("Else");
        lines.AddRange(NotFoundMessages(plan, 4));
        lines.Add("EndIf");
        return string.Join(Environment.NewLine, lines);
    }

    internal static string UpdateContent(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var requiredFields = RequiredFieldsFor(plan, "UpdateRequest", plan.UpdateRequestFields);
        var guarded = requiredFields.Count > 0;
        var bodyIndent = guarded ? "    " : string.Empty;
        var assignmentIndent = guarded ? 8 : 4;
        var nestedIndent = guarded ? 12 : 8;
        var failureIndent = guarded ? 8 : 4;
        var lines = new List<string> { "&RestStatusCode = 200" };
        lines.AddRange(RequiredMemberPresenceValidation("UpdateRequest", "&UpdateRequest", requiredFields, 0));
        if (guarded)
        {
            lines.Add("If &RestStatusCode = 200");
        }

        lines.Add($"{bodyIndent}{bc}.Load({LoadArguments(plan, "&")})");
        lines.Add($"{bodyIndent}If {bc}.Success()");
        lines.AddRange(plan.UpdateRequestFields.Select(field => $"{new string(' ', assignmentIndent)}{bc}.{field.Name} = &UpdateRequest.{field.Name}"));
        lines.Add($"{new string(' ', assignmentIndent)}{bc}.Save()");
        lines.Add($"{new string(' ', assignmentIndent)}If {bc}.Success()");
        lines.Add($"{new string(' ', nestedIndent)}Commit");
        lines.Add($"{new string(' ', nestedIndent)}{bc}.Load({LoadArguments(plan, "&")})");
        lines.Add($"{new string(' ', nestedIndent)}&UpdateResponse = new()");
        lines.AddRange(ResponseAssignments(plan, bc, "&UpdateResponse", nestedIndent));
        lines.Add($"{new string(' ', nestedIndent)}&RestStatusCode = 200");
        lines.Add($"{new string(' ', assignmentIndent)}Else");
        lines.AddRange(BusinessRuleFailureMessages(bc, nestedIndent));
        lines.Add($"{new string(' ', assignmentIndent)}EndIf");
        lines.Add($"{bodyIndent}Else");
        lines.AddRange(NotFoundMessages(plan, failureIndent));
        lines.Add($"{bodyIndent}EndIf");
        if (guarded)
        {
            lines.Add("EndIf");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static IEnumerable<string> ResponseAssignments(ApiPlan plan, string bc, string response, int spaces) =>
        plan.ResponseFields.Select(field => $"{new string(' ', spaces)}{response}.{field.Name} = {bc}.{field.Name}");

    private static IReadOnlyList<ApiPlanField> RequiredFieldsFor(ApiPlan plan, string requestName, IReadOnlyList<ApiPlanField> candidateFields)
    {
        var fieldNames = new HashSet<string>(candidateFields.Select(field => field.Name), StringComparer.OrdinalIgnoreCase);
        return plan.RequiredFields
            .Where(field => field.IsRequired &&
                string.Equals(field.RequestName, requestName, StringComparison.OrdinalIgnoreCase) &&
                fieldNames.Contains(field.FieldName))
            .Select(field => candidateFields.Single(candidate => string.Equals(candidate.Name, field.FieldName, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }

    private static IEnumerable<string> RequiredMemberPresenceValidation(string requestName, string requestVariable, IReadOnlyList<ApiPlanField> requiredFields, int spaces)
    {
        return DefaultValueRequiredMemberValidation(requestName, requestVariable, requiredFields, spaces);
    }

    private static IEnumerable<string> PreviousB079OriginalMemberDirtyPresenceValidation(string requestVariable, IReadOnlyList<ApiPlanField> requiredFields, int spaces)
    {
        if (requiredFields.Count == 0)
        {
            yield break;
        }

        var indent = new string(' ', spaces);
        var requiredNames = string.Join(", ", requiredFields.Select(field => $"\"{field.Name}\""));
        yield return $"{indent}&RequestJsonHasRequiredMembers = False";
        yield return $"{indent}&MissingRequiredFields = !\"\"";
        yield return $"{indent}csharp try {{ var __goabSdt = [!{requestVariable}!]; var __goabMissing = new System.Collections.Generic.List<string>(); foreach (var __goabName in new[] {{ {requiredNames} }}) {{ if (__goabSdt == null || !__goabSdt.IsDirty(__goabName)) __goabMissing.Add(__goabName); }} [!&MissingRequiredFields!] = string.Join(\", \", __goabMissing); [!&RequestJsonHasRequiredMembers!] = __goabMissing.Count == 0; }} catch {{ [!&MissingRequiredFields!] = \"{string.Join(", ", requiredFields.Select(field => field.Name))}\"; [!&RequestJsonHasRequiredMembers!] = false; }}";
        yield return $"{indent}If not &RequestJsonHasRequiredMembers";
        yield return $"{indent}    &RestStatusCode = 400";
        yield return $"{indent}    &ErrorResponse = new()";
        yield return $"{indent}    &ErrorResponse.Code = !\"invalid_request\"";
        yield return $"{indent}    &ErrorResponse.Message = Format(!\"Required JSON member(s) missing: %1.\", &MissingRequiredFields.Trim())";
        yield return $"{indent}EndIf";
    }

    private static IEnumerable<string> NativeJsonRequiredMemberPresenceValidation(string requestName, IReadOnlyList<ApiPlanField> requiredFields, int spaces)
    {
        if (requiredFields.Count == 0)
        {
            yield break;
        }

        var indent = new string(' ', spaces);
        yield return $"{indent}&RequestBody = &HttpRequest.ToString()";
        yield return $"{indent}&RequestJsonHasRequiredMembers = False";
        yield return $"{indent}&MissingRequiredFields = !\"\"";
        yield return $"{indent}&RequestPayloadJson = !\"\"";
        yield return $"{indent}&RequestProperties.FromJson(&RequestBody)";
        yield return $"{indent}&RequestPayloadJson = &RequestProperties.Get(!\"{requestName}\")";
        yield return $"{indent}If &RequestPayloadJson.IsEmpty()";
        yield return $"{indent}    &RequestPayloadJson = &RequestBody";
        yield return $"{indent}EndIf";
        yield return $"{indent}&RequestPayloadProperties.FromJson(&RequestPayloadJson)";

        foreach (var field in requiredFields)
        {
            yield return $"{indent}&RequiredMemberFound = False";
            yield return $"{indent}For &JsonProperty in &RequestPayloadProperties";
            yield return $"{indent}    If &JsonProperty.Key.Trim().ToLower() = !\"{field.Name.ToLowerInvariant()}\"";
            yield return $"{indent}        &RequiredMemberFound = True";
            yield return $"{indent}    EndIf";
            yield return $"{indent}EndFor";
            yield return $"{indent}If not &RequiredMemberFound";
            yield return $"{indent}    &MissingRequiredFields = iif(&MissingRequiredFields.IsEmpty(), !\"{field.Name}\", &MissingRequiredFields + !\", {field.Name}\")";
            yield return $"{indent}EndIf";
        }

        yield return $"{indent}&RequestJsonHasRequiredMembers = &MissingRequiredFields.IsEmpty()";
        yield return $"{indent}If not &RequestJsonHasRequiredMembers";
        yield return $"{indent}    &RestStatusCode = 400";
        yield return $"{indent}    &ErrorResponse = new()";
        yield return $"{indent}    &ErrorResponse.Code = !\"invalid_request\"";
        yield return $"{indent}    &ErrorResponse.Message = Format(!\"Required JSON member(s) missing: %1.\", &MissingRequiredFields.Trim())";
        yield return $"{indent}EndIf";
    }

    /// <summary>
    /// Valida membros JSON obrigatorios comparando cada membro recebido com o valor default do mesmo membro
    /// em uma instancia vazia do proprio SDT de request.
    /// </summary>
    /// <remarks>
    /// O GeneXus nao expoe, sem comando csharp, a informacao de presenca de membro no JSON recebido:
    /// o corpo bruto ja foi consumido pelo pipeline REST antes de qualquer codigo GeneXus executar,
    /// tanto na Procedure quanto no evento Before do API Object, nos geradores .NET e .NET Framework.
    /// Por isso a validacao passa a ser por preenchimento, e nao por presenca. A comparacao contra
    /// instancia vazia dispensa ramificar por tipo de dado e vale para qualquer tipo de membro.
    /// Limitacao assumida: membro obrigatorio cujo valor legitimo seja igual ao default do tipo
    /// (por exemplo, numerico zero) e recusado com 400.
    /// </remarks>
    private static IEnumerable<string> DefaultValueRequiredMemberValidation(string requestName, string requestVariable, IReadOnlyList<ApiPlanField> requiredFields, int spaces)
    {
        if (requiredFields.Count == 0)
        {
            yield break;
        }

        var indent = new string(' ', spaces);
        var emptyVariable = "&" + EmptyRequestVariableName(requestName);
        yield return $"{indent}&RequestJsonHasRequiredMembers = True";
        yield return $"{indent}&MissingRequiredFields = !\"\"";
        yield return $"{indent}{emptyVariable} = new()";

        foreach (var field in requiredFields)
        {
            yield return $"{indent}If {requestVariable}.{field.Name} = {emptyVariable}.{field.Name}";
            yield return $"{indent}    &RequestJsonHasRequiredMembers = False";
            yield return $"{indent}    If &MissingRequiredFields.IsEmpty()";
            yield return $"{indent}        &MissingRequiredFields = !\"{field.Name}\"";
            yield return $"{indent}    Else";
            yield return $"{indent}        &MissingRequiredFields = &MissingRequiredFields + !\", {field.Name}\"";
            yield return $"{indent}    EndIf";
            yield return $"{indent}EndIf";
        }

        yield return $"{indent}If not &RequestJsonHasRequiredMembers";
        yield return $"{indent}    &RestStatusCode = 400";
        yield return $"{indent}    &ErrorResponse = new()";
        yield return $"{indent}    &ErrorResponse.Code = !\"invalid_request\"";
        yield return $"{indent}    &ErrorResponse.Message = Format(!\"Required JSON member(s) missing or empty: %1.\", &MissingRequiredFields.Trim())";
        yield return $"{indent}EndIf";
    }

    private static string EmptyRequestVariableName(string requestName) => "Empty" + requestName;

    private static string RequestSdtName(ApiPlan plan, string requestName) =>
        string.Equals(requestName, "CreateRequest", StringComparison.OrdinalIgnoreCase)
            ? plan.CreateRequestSdtName
            : plan.UpdateRequestSdtName;

    private static IEnumerable<string> PreviousB079SdtDirtyMemberPresenceValidation(string requestVariable, IReadOnlyList<ApiPlanField> requiredFields, int spaces, bool useDirtyMemberNames)
    {
        if (requiredFields.Count == 0)
        {
            yield break;
        }

        var indent = new string(' ', spaces);
        var dirtyNames = string.Join(", ", requiredFields.Select(field => $"\"{(useDirtyMemberNames ? SdtDirtyMemberName(field.Name) : field.Name)}\""));
        var displayNames = string.Join(", ", requiredFields.Select(field => $"\"{field.Name}\""));
        yield return $"{indent}&RequestJsonHasRequiredMembers = False";
        yield return $"{indent}&MissingRequiredFields = !\"\"";
        yield return $"{indent}csharp try {{ var __goabSdt = [!{requestVariable}!]; var __goabMissing = new System.Collections.Generic.List<string>(); var __goabDirtyNames = new[] {{ {dirtyNames} }}; var __goabDisplayNames = new[] {{ {displayNames} }}; for (var __goabIndex = 0; __goabIndex < __goabDirtyNames.Length; __goabIndex++) {{ if (__goabSdt == null || !__goabSdt.IsDirty(__goabDirtyNames[__goabIndex])) __goabMissing.Add(__goabDisplayNames[__goabIndex]); }} [!&MissingRequiredFields!] = string.Join(\", \", __goabMissing); [!&RequestJsonHasRequiredMembers!] = __goabMissing.Count == 0; }} catch {{ [!&MissingRequiredFields!] = \"{string.Join(", ", requiredFields.Select(field => field.Name))}\"; [!&RequestJsonHasRequiredMembers!] = false; }}";
        yield return $"{indent}If not &RequestJsonHasRequiredMembers";
        yield return $"{indent}    &RestStatusCode = 400";
        yield return $"{indent}    &ErrorResponse = new()";
        yield return $"{indent}    &ErrorResponse.Code = !\"invalid_request\"";
        yield return $"{indent}    &ErrorResponse.Message = Format(!\"Required JSON member(s) missing: %1.\", &MissingRequiredFields.Trim())";
        yield return $"{indent}EndIf";
    }

    private static string SdtDirtyMemberName(string name)
    {
        return string.IsNullOrEmpty(name)
            ? name
            : char.ToUpperInvariant(name[0]) + name.Substring(1).ToLowerInvariant();
    }

    private static IEnumerable<string> PreviousB079NewtonsoftRequiredMemberPresenceValidation(string requestName, IReadOnlyList<ApiPlanField> requiredFields, int spaces)
    {
        if (requiredFields.Count == 0)
        {
            yield break;
        }

        var indent = new string(' ', spaces);
        var requiredNames = string.Join(", ", requiredFields.Select(field => $"\"{field.Name}\""));
        yield return $"{indent}&RequestBody = &HttpRequest.ToString()";
        yield return $"{indent}&RequestJsonHasRequiredMembers = False";
        yield return $"{indent}&MissingRequiredFields = !\"\"";
        yield return $"{indent}csharp try {{ var __goabJson = Newtonsoft.Json.Linq.JObject.Parse([!&RequestBody!]); var __goabPayload = __goabJson[\"{requestName}\"] as Newtonsoft.Json.Linq.JObject; var __goabMissing = new System.Collections.Generic.List<string>(); foreach (var __goabName in new[] {{ {requiredNames} }}) {{ if (__goabPayload == null || __goabPayload.Property(__goabName, System.StringComparison.OrdinalIgnoreCase) == null) __goabMissing.Add(__goabName); }} [!&MissingRequiredFields!] = string.Join(\", \", __goabMissing); [!&RequestJsonHasRequiredMembers!] = __goabMissing.Count == 0; }} catch {{ [!&MissingRequiredFields!] = \"{string.Join(", ", requiredFields.Select(field => field.Name))}\"; [!&RequestJsonHasRequiredMembers!] = false; }}";
        yield return $"{indent}If not &RequestJsonHasRequiredMembers";
        yield return $"{indent}    &RestStatusCode = 400";
        yield return $"{indent}    &ErrorResponse = new()";
        yield return $"{indent}    &ErrorResponse.Code = !\"invalid_request\"";
        yield return $"{indent}    &ErrorResponse.Message = Format(!\"Required JSON member(s) missing: %1.\", &MissingRequiredFields.Trim())";
        yield return $"{indent}EndIf";
    }

    private static IEnumerable<string> PreviousB079WrappedRequiredMemberPresenceValidation(string requestName, IReadOnlyList<ApiPlanField> requiredFields, int spaces)
    {
        if (requiredFields.Count == 0)
        {
            yield break;
        }

        var indent = new string(' ', spaces);
        var requiredNames = string.Join(", ", requiredFields.Select(field => $"\"{field.Name}\""));
        yield return $"{indent}&RequestBody = &HttpRequest.ToString()";
        yield return $"{indent}&RequestJsonHasRequiredMembers = False";
        yield return $"{indent}&MissingRequiredFields = !\"\"";
        yield return $"{indent}csharp try {{ var __goabBody = [!&RequestBody!] ?? string.Empty; var __goabMissing = new System.Collections.Generic.List<string>(); var __goabHasPayload = System.Text.RegularExpressions.Regex.IsMatch(__goabBody, \"\\\\\\\"{requestName}\\\\\\\"\\\\s*:\", System.Text.RegularExpressions.RegexOptions.IgnoreCase); foreach (var __goabName in new[] {{ {requiredNames} }}) {{ if (!__goabHasPayload || !System.Text.RegularExpressions.Regex.IsMatch(__goabBody, \"\\\\\\\"\" + System.Text.RegularExpressions.Regex.Escape(__goabName) + \"\\\\\\\"\\\\s*:\", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) __goabMissing.Add(__goabName); }} [!&MissingRequiredFields!] = string.Join(\", \", __goabMissing); [!&RequestJsonHasRequiredMembers!] = __goabMissing.Count == 0; }} catch {{ [!&MissingRequiredFields!] = \"{string.Join(", ", requiredFields.Select(field => field.Name))}\"; [!&RequestJsonHasRequiredMembers!] = false; }}";
        yield return $"{indent}If not &RequestJsonHasRequiredMembers";
        yield return $"{indent}    &RestStatusCode = 400";
        yield return $"{indent}    &ErrorResponse = new()";
        yield return $"{indent}    &ErrorResponse.Code = !\"invalid_request\"";
        yield return $"{indent}    &ErrorResponse.Message = Format(!\"Required JSON member(s) missing: %1.\", &MissingRequiredFields.Trim())";
        yield return $"{indent}EndIf";
    }

    private static IEnumerable<string> PreviousB079UnwrappedRequiredMemberPresenceValidation(IReadOnlyList<ApiPlanField> requiredFields, int spaces)
    {
        if (requiredFields.Count == 0)
        {
            yield break;
        }

        var indent = new string(' ', spaces);
        var requiredNames = string.Join(", ", requiredFields.Select(field => $"\"{field.Name}\""));
        yield return $"{indent}&RequestBody = &HttpRequest.ToString()";
        yield return $"{indent}&RequestJsonHasRequiredMembers = False";
        yield return $"{indent}&MissingRequiredFields = !\"\"";
        yield return $"{indent}csharp try {{ var __goabBody = [!&RequestBody!] ?? string.Empty; var __goabMissing = new System.Collections.Generic.List<string>(); foreach (var __goabName in new[] {{ {requiredNames} }}) {{ if (!System.Text.RegularExpressions.Regex.IsMatch(__goabBody, \"\\\\\\\"\" + System.Text.RegularExpressions.Regex.Escape(__goabName) + \"\\\\\\\"\\\\s*:\", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) __goabMissing.Add(__goabName); }} [!&MissingRequiredFields!] = string.Join(\", \", __goabMissing); [!&RequestJsonHasRequiredMembers!] = __goabMissing.Count == 0; }} catch {{ [!&MissingRequiredFields!] = \"{string.Join(", ", requiredFields.Select(field => field.Name))}\"; [!&RequestJsonHasRequiredMembers!] = false; }}";
        yield return $"{indent}If not &RequestJsonHasRequiredMembers";
        yield return $"{indent}    &RestStatusCode = 400";
        yield return $"{indent}    &ErrorResponse = new()";
        yield return $"{indent}    &ErrorResponse.Code = !\"invalid_request\"";
        yield return $"{indent}    &ErrorResponse.Message = Format(!\"Required JSON member(s) missing: %1.\", &MissingRequiredFields.Trim())";
        yield return $"{indent}EndIf";
    }

    private static string LegacyCreateContent(ApiPlan plan)
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
        lines.AddRange(LegacyFailureMessages(bc, 4));
        lines.Add("EndIf");
        return string.Join(Environment.NewLine, lines);
    }

    private static string PreviousB079CreateContentWithoutLocationHeader(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var requiredFields = RequiredFieldsFor(plan, "CreateRequest", plan.CreateRequestFields);
        var guarded = requiredFields.Count > 0;
        var bodyIndent = guarded ? "    " : string.Empty;
        var successIndent = guarded ? 8 : 4;
        var lines = new List<string> { "&RestStatusCode = 201" };
        lines.AddRange(RequiredMemberPresenceValidation("CreateRequest", "&CreateRequest", requiredFields, 0));
        if (guarded)
        {
            lines.Add("If &RestStatusCode = 201");
        }

        lines.Add($"{bodyIndent}{bc} = new()");
        lines.AddRange(plan.CreateRequestFields.Select(field => $"{bodyIndent}{bc}.{field.Name} = &CreateRequest.{field.Name}"));
        lines.Add($"{bodyIndent}{bc}.Save()");
        lines.Add($"{bodyIndent}If {bc}.Success()");
        lines.Add($"{bodyIndent}    Commit");
        lines.Add($"{bodyIndent}    {bc}.Load({LoadArguments(plan, bc)})");
        lines.Add($"{bodyIndent}    &CreateResponse = new()");
        lines.AddRange(ResponseAssignments(plan, bc, "&CreateResponse", successIndent));
        lines.Add($"{bodyIndent}    &RestStatusCode = 201");
        lines.Add($"{bodyIndent}Else");
        lines.AddRange(BusinessRuleFailureMessages(bc, successIndent));
        lines.Add($"{bodyIndent}EndIf");
        if (guarded)
        {
            lines.Add("EndIf");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string PreviousB079CreateContentWithGenericToStringLocationHeader(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var requiredFields = RequiredFieldsFor(plan, "CreateRequest", plan.CreateRequestFields);
        var guarded = requiredFields.Count > 0;
        var bodyIndent = guarded ? "    " : string.Empty;
        var successIndent = guarded ? 8 : 4;
        var lines = new List<string> { "&RestStatusCode = 201" };
        lines.AddRange(RequiredMemberPresenceValidation("CreateRequest", "&CreateRequest", requiredFields, 0));
        if (guarded)
        {
            lines.Add("If &RestStatusCode = 201");
        }

        lines.Add($"{bodyIndent}{bc} = new()");
        lines.AddRange(plan.CreateRequestFields.Select(field => $"{bodyIndent}{bc}.{field.Name} = &CreateRequest.{field.Name}"));
        lines.Add($"{bodyIndent}{bc}.Save()");
        lines.Add($"{bodyIndent}If {bc}.Success()");
        lines.Add($"{bodyIndent}    Commit");
        lines.Add($"{bodyIndent}    {bc}.Load({LoadArguments(plan, bc)})");
        lines.Add($"{bodyIndent}    &CreateResponse = new()");
        lines.AddRange(ResponseAssignments(plan, bc, "&CreateResponse", successIndent));
        lines.Add($"{bodyIndent}    &HttpResponse.AddHeader(!\"Location\", {PreviousB079GenericToStringCreateLocationUrlExpression(plan, bc)})");
        lines.Add($"{bodyIndent}    &RestStatusCode = 201");
        lines.Add($"{bodyIndent}Else");
        lines.AddRange(BusinessRuleFailureMessages(bc, successIndent));
        lines.Add($"{bodyIndent}EndIf");
        if (guarded)
        {
            lines.Add("EndIf");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string PreviousB079GenericToStringCreateLocationUrlExpression(ApiPlan plan, string bc)
    {
        var basePath = plan.RestPath.TrimEnd('/');
        var keyParts = plan.PrimaryKey.Select(field => $"{bc}.{field.Name}.ToString().Trim()");
        return $"!\"{basePath}/\" + " + string.Join(" + !\"/\" + ", keyParts);
    }

    private static string PreviousB079CreateContent(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var lines = new List<string> { "&RestStatusCode = 201", $"{bc} = new()" };
        lines.AddRange(plan.CreateRequestFields.Select(field => $"{bc}.{field.Name} = &CreateRequest.{field.Name}"));
        lines.Add($"{bc}.Save()");
        lines.Add($"If {bc}.Success()");
        lines.Add($"    {bc}.Load({LoadArguments(plan, bc)})");
        lines.Add("    &CreateResponse = new()");
        lines.AddRange(ResponseAssignments(plan, bc, "&CreateResponse", 4));
        lines.Add("    &RestStatusCode = 201");
        lines.Add("Else");
        lines.AddRange(PreviousB079BusinessRuleFailureMessages(bc, 4));
        lines.Add("EndIf");
        return string.Join(Environment.NewLine, lines);
    }

    private static string PreviousB079CreateContentWithoutCommit(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var lines = new List<string> { "&RestStatusCode = 201", $"{bc} = new()" };
        lines.AddRange(plan.CreateRequestFields.Select(field => $"{bc}.{field.Name} = &CreateRequest.{field.Name}"));
        lines.Add($"{bc}.Save()");
        lines.Add($"If {bc}.Success()");
        lines.Add($"    {bc}.Load({LoadArguments(plan, bc)})");
        lines.Add("    &CreateResponse = new()");
        lines.AddRange(ResponseAssignments(plan, bc, "&CreateResponse", 4));
        lines.Add("    &RestStatusCode = 201");
        lines.Add("Else");
        lines.AddRange(BusinessRuleFailureMessages(bc, 4));
        lines.Add("EndIf");
        return string.Join(Environment.NewLine, lines);
    }

    private static string PreviousB079CreateContentWithoutRequiredMemberValidation(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var lines = new List<string> { "&RestStatusCode = 201", $"{bc} = new()" };
        lines.AddRange(plan.CreateRequestFields.Select(field => $"{bc}.{field.Name} = &CreateRequest.{field.Name}"));
        lines.Add($"{bc}.Save()");
        lines.Add($"If {bc}.Success()");
        lines.Add("    Commit");
        lines.Add($"    {bc}.Load({LoadArguments(plan, bc)})");
        lines.Add("    &CreateResponse = new()");
        lines.AddRange(ResponseAssignments(plan, bc, "&CreateResponse", 4));
        lines.Add("    &RestStatusCode = 201");
        lines.Add("Else");
        lines.AddRange(BusinessRuleFailureMessages(bc, 4));
        lines.Add("EndIf");
        return string.Join(Environment.NewLine, lines);
    }

    private static string PreviousB079CreateContentWithNewtonsoftRequiredMemberValidation(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var requiredFields = RequiredFieldsFor(plan, "CreateRequest", plan.CreateRequestFields);
        var guarded = requiredFields.Count > 0;
        var bodyIndent = guarded ? "    " : string.Empty;
        var successIndent = guarded ? 8 : 4;
        var lines = new List<string> { "&RestStatusCode = 201" };
        lines.AddRange(PreviousB079NewtonsoftRequiredMemberPresenceValidation("CreateRequest", requiredFields, 0));
        if (guarded)
        {
            lines.Add("If &RestStatusCode = 201");
        }

        lines.Add($"{bodyIndent}{bc} = new()");
        lines.AddRange(plan.CreateRequestFields.Select(field => $"{bodyIndent}{bc}.{field.Name} = &CreateRequest.{field.Name}"));
        lines.Add($"{bodyIndent}{bc}.Save()");
        lines.Add($"{bodyIndent}If {bc}.Success()");
        lines.Add($"{bodyIndent}    Commit");
        lines.Add($"{bodyIndent}    {bc}.Load({LoadArguments(plan, bc)})");
        lines.Add($"{bodyIndent}    &CreateResponse = new()");
        lines.AddRange(ResponseAssignments(plan, bc, "&CreateResponse", successIndent));
        lines.Add($"{bodyIndent}    &RestStatusCode = 201");
        lines.Add($"{bodyIndent}Else");
        lines.AddRange(BusinessRuleFailureMessages(bc, successIndent));
        lines.Add($"{bodyIndent}EndIf");
        if (guarded)
        {
            lines.Add("EndIf");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string PreviousB079CreateContentWithWrappedRequiredMemberValidation(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var requiredFields = RequiredFieldsFor(plan, "CreateRequest", plan.CreateRequestFields);
        var guarded = requiredFields.Count > 0;
        var bodyIndent = guarded ? "    " : string.Empty;
        var successIndent = guarded ? 8 : 4;
        var lines = new List<string> { "&RestStatusCode = 201" };
        lines.AddRange(PreviousB079WrappedRequiredMemberPresenceValidation("CreateRequest", requiredFields, 0));
        if (guarded)
        {
            lines.Add("If &RestStatusCode = 201");
        }

        lines.Add($"{bodyIndent}{bc} = new()");
        lines.AddRange(plan.CreateRequestFields.Select(field => $"{bodyIndent}{bc}.{field.Name} = &CreateRequest.{field.Name}"));
        lines.Add($"{bodyIndent}{bc}.Save()");
        lines.Add($"{bodyIndent}If {bc}.Success()");
        lines.Add($"{bodyIndent}    Commit");
        lines.Add($"{bodyIndent}    {bc}.Load({LoadArguments(plan, bc)})");
        lines.Add($"{bodyIndent}    &CreateResponse = new()");
        lines.AddRange(ResponseAssignments(plan, bc, "&CreateResponse", successIndent));
        lines.Add($"{bodyIndent}    &RestStatusCode = 201");
        lines.Add($"{bodyIndent}Else");
        lines.AddRange(BusinessRuleFailureMessages(bc, successIndent));
        lines.Add($"{bodyIndent}EndIf");
        if (guarded)
        {
            lines.Add("EndIf");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string PreviousB079CreateContentWithUnwrappedRequiredMemberValidation(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var requiredFields = RequiredFieldsFor(plan, "CreateRequest", plan.CreateRequestFields);
        var guarded = requiredFields.Count > 0;
        var bodyIndent = guarded ? "    " : string.Empty;
        var successIndent = guarded ? 8 : 4;
        var lines = new List<string> { "&RestStatusCode = 201" };
        lines.AddRange(PreviousB079UnwrappedRequiredMemberPresenceValidation(requiredFields, 0));
        if (guarded)
        {
            lines.Add("If &RestStatusCode = 201");
        }

        lines.Add($"{bodyIndent}{bc} = new()");
        lines.AddRange(plan.CreateRequestFields.Select(field => $"{bodyIndent}{bc}.{field.Name} = &CreateRequest.{field.Name}"));
        lines.Add($"{bodyIndent}{bc}.Save()");
        lines.Add($"{bodyIndent}If {bc}.Success()");
        lines.Add($"{bodyIndent}    Commit");
        lines.Add($"{bodyIndent}    {bc}.Load({LoadArguments(plan, bc)})");
        lines.Add($"{bodyIndent}    &CreateResponse = new()");
        lines.AddRange(ResponseAssignments(plan, bc, "&CreateResponse", successIndent));
        lines.Add($"{bodyIndent}    &RestStatusCode = 201");
        lines.Add($"{bodyIndent}Else");
        lines.AddRange(BusinessRuleFailureMessages(bc, successIndent));
        lines.Add($"{bodyIndent}EndIf");
        if (guarded)
        {
            lines.Add("EndIf");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string PreviousB079CreateContentWithOriginalMemberDirtyValidation(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var requiredFields = RequiredFieldsFor(plan, "CreateRequest", plan.CreateRequestFields);
        var guarded = requiredFields.Count > 0;
        var bodyIndent = guarded ? "    " : string.Empty;
        var successIndent = guarded ? 8 : 4;
        var lines = new List<string> { "&RestStatusCode = 201" };
        lines.AddRange(PreviousB079OriginalMemberDirtyPresenceValidation("&CreateRequest", requiredFields, 0));
        if (guarded)
        {
            lines.Add("If &RestStatusCode = 201");
        }

        lines.Add($"{bodyIndent}{bc} = new()");
        lines.AddRange(plan.CreateRequestFields.Select(field => $"{bodyIndent}{bc}.{field.Name} = &CreateRequest.{field.Name}"));
        lines.Add($"{bodyIndent}{bc}.Save()");
        lines.Add($"{bodyIndent}If {bc}.Success()");
        lines.Add($"{bodyIndent}    Commit");
        lines.Add($"{bodyIndent}    {bc}.Load({LoadArguments(plan, bc)})");
        lines.Add($"{bodyIndent}    &CreateResponse = new()");
        lines.AddRange(ResponseAssignments(plan, bc, "&CreateResponse", successIndent));
        lines.Add($"{bodyIndent}    &RestStatusCode = 201");
        lines.Add($"{bodyIndent}Else");
        lines.AddRange(BusinessRuleFailureMessages(bc, successIndent));
        lines.Add($"{bodyIndent}EndIf");
        if (guarded)
        {
            lines.Add("EndIf");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string PreviousB079CreateContentWithNativeJsonValidation(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var requiredFields = RequiredFieldsFor(plan, "CreateRequest", plan.CreateRequestFields);
        var guarded = requiredFields.Count > 0;
        var bodyIndent = guarded ? "    " : string.Empty;
        var successIndent = guarded ? 8 : 4;
        var lines = new List<string> { "&RestStatusCode = 201" };
        lines.AddRange(NativeJsonRequiredMemberPresenceValidation("CreateRequest", requiredFields, 0));
        if (guarded)
        {
            lines.Add("If &RestStatusCode = 201");
        }

        lines.Add($"{bodyIndent}{bc} = new()");
        lines.AddRange(plan.CreateRequestFields.Select(field => $"{bodyIndent}{bc}.{field.Name} = &CreateRequest.{field.Name}"));
        lines.Add($"{bodyIndent}{bc}.Save()");
        lines.Add($"{bodyIndent}If {bc}.Success()");
        lines.Add($"{bodyIndent}    Commit");
        lines.Add($"{bodyIndent}    {bc}.Load({LoadArguments(plan, bc)})");
        lines.Add($"{bodyIndent}    &CreateResponse = new()");
        lines.AddRange(ResponseAssignments(plan, bc, "&CreateResponse", successIndent));
        lines.Add($"{bodyIndent}    &RestStatusCode = 201");
        lines.Add($"{bodyIndent}Else");
        lines.AddRange(BusinessRuleFailureMessages(bc, successIndent));
        lines.Add($"{bodyIndent}EndIf");
        if (guarded)
        {
            lines.Add("EndIf");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string PreviousB079CreateContentWithSdtDirtyValidation(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var requiredFields = RequiredFieldsFor(plan, "CreateRequest", plan.CreateRequestFields);
        var guarded = requiredFields.Count > 0;
        var bodyIndent = guarded ? "    " : string.Empty;
        var successIndent = guarded ? 8 : 4;
        var lines = new List<string> { "&RestStatusCode = 201" };
        lines.AddRange(PreviousB079SdtDirtyMemberPresenceValidation("&CreateRequest", requiredFields, 0, useDirtyMemberNames: true));
        if (guarded)
        {
            lines.Add("If &RestStatusCode = 201");
        }

        lines.Add($"{bodyIndent}{bc} = new()");
        lines.AddRange(plan.CreateRequestFields.Select(field => $"{bodyIndent}{bc}.{field.Name} = &CreateRequest.{field.Name}"));
        lines.Add($"{bodyIndent}{bc}.Save()");
        lines.Add($"{bodyIndent}If {bc}.Success()");
        lines.Add($"{bodyIndent}    Commit");
        lines.Add($"{bodyIndent}    {bc}.Load({LoadArguments(plan, bc)})");
        lines.Add($"{bodyIndent}    &CreateResponse = new()");
        lines.AddRange(ResponseAssignments(plan, bc, "&CreateResponse", successIndent));
        lines.Add($"{bodyIndent}    &RestStatusCode = 201");
        lines.Add($"{bodyIndent}Else");
        lines.AddRange(BusinessRuleFailureMessages(bc, successIndent));
        lines.Add($"{bodyIndent}EndIf");
        if (guarded)
        {
            lines.Add("EndIf");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string LegacyUpdateContent(ApiPlan plan)
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
        lines.AddRange(LegacyFailureMessages(bc, 8));
        lines.Add("    EndIf");
        lines.Add("Else");
        lines.AddRange(LegacyFailureMessages(bc, 4));
        lines.Add("EndIf");
        return string.Join(Environment.NewLine, lines);
    }

    private static string PreviousB079UpdateContent(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var lines = new List<string> { "&RestStatusCode = 200", $"{bc}.Load({LoadArguments(plan, "&")})", $"If {bc}.Success()" };
        lines.AddRange(plan.UpdateRequestFields.Select(field => $"    {bc}.{field.Name} = &UpdateRequest.{field.Name}"));
        lines.Add($"    {bc}.Save()");
        lines.Add($"    If {bc}.Success()");
        lines.Add($"        {bc}.Load({LoadArguments(plan, "&")})");
        lines.Add("        &UpdateResponse = new()");
        lines.AddRange(ResponseAssignments(plan, bc, "&UpdateResponse", 8));
        lines.Add("        &RestStatusCode = 200");
        lines.Add("    Else");
        lines.AddRange(PreviousB079BusinessRuleFailureMessages(bc, 8));
        lines.Add("    EndIf");
        lines.Add("Else");
        lines.AddRange(NotFoundMessages(plan, 4));
        lines.Add("EndIf");
        return string.Join(Environment.NewLine, lines);
    }

    private static string PreviousB079UpdateContentWithoutCommit(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var lines = new List<string> { "&RestStatusCode = 200", $"{bc}.Load({LoadArguments(plan, "&")})", $"If {bc}.Success()" };
        lines.AddRange(plan.UpdateRequestFields.Select(field => $"    {bc}.{field.Name} = &UpdateRequest.{field.Name}"));
        lines.Add($"    {bc}.Save()");
        lines.Add($"    If {bc}.Success()");
        lines.Add($"        {bc}.Load({LoadArguments(plan, "&")})");
        lines.Add("        &UpdateResponse = new()");
        lines.AddRange(ResponseAssignments(plan, bc, "&UpdateResponse", 8));
        lines.Add("        &RestStatusCode = 200");
        lines.Add("    Else");
        lines.AddRange(BusinessRuleFailureMessages(bc, 8));
        lines.Add("    EndIf");
        lines.Add("Else");
        lines.AddRange(NotFoundMessages(plan, 4));
        lines.Add("EndIf");
        return string.Join(Environment.NewLine, lines);
    }

    private static string PreviousB079UpdateContentWithoutRequiredMemberValidation(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var lines = new List<string> { "&RestStatusCode = 200", $"{bc}.Load({LoadArguments(plan, "&")})", $"If {bc}.Success()" };
        lines.AddRange(plan.UpdateRequestFields.Select(field => $"    {bc}.{field.Name} = &UpdateRequest.{field.Name}"));
        lines.Add($"    {bc}.Save()");
        lines.Add($"    If {bc}.Success()");
        lines.Add("        Commit");
        lines.Add($"        {bc}.Load({LoadArguments(plan, "&")})");
        lines.Add("        &UpdateResponse = new()");
        lines.AddRange(ResponseAssignments(plan, bc, "&UpdateResponse", 8));
        lines.Add("        &RestStatusCode = 200");
        lines.Add("    Else");
        lines.AddRange(BusinessRuleFailureMessages(bc, 8));
        lines.Add("    EndIf");
        lines.Add("Else");
        lines.AddRange(NotFoundMessages(plan, 4));
        lines.Add("EndIf");
        return string.Join(Environment.NewLine, lines);
    }

    private static string PreviousB079UpdateContentWithNewtonsoftRequiredMemberValidation(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var requiredFields = RequiredFieldsFor(plan, "UpdateRequest", plan.UpdateRequestFields);
        var guarded = requiredFields.Count > 0;
        var bodyIndent = guarded ? "    " : string.Empty;
        var assignmentIndent = guarded ? 8 : 4;
        var nestedIndent = guarded ? 12 : 8;
        var failureIndent = guarded ? 8 : 4;
        var lines = new List<string> { "&RestStatusCode = 200" };
        lines.AddRange(PreviousB079NewtonsoftRequiredMemberPresenceValidation("UpdateRequest", requiredFields, 0));
        if (guarded)
        {
            lines.Add("If &RestStatusCode = 200");
        }

        lines.Add($"{bodyIndent}{bc}.Load({LoadArguments(plan, "&")})");
        lines.Add($"{bodyIndent}If {bc}.Success()");
        lines.AddRange(plan.UpdateRequestFields.Select(field => $"{new string(' ', assignmentIndent)}{bc}.{field.Name} = &UpdateRequest.{field.Name}"));
        lines.Add($"{new string(' ', assignmentIndent)}{bc}.Save()");
        lines.Add($"{new string(' ', assignmentIndent)}If {bc}.Success()");
        lines.Add($"{new string(' ', nestedIndent)}Commit");
        lines.Add($"{new string(' ', nestedIndent)}{bc}.Load({LoadArguments(plan, "&")})");
        lines.Add($"{new string(' ', nestedIndent)}&UpdateResponse = new()");
        lines.AddRange(ResponseAssignments(plan, bc, "&UpdateResponse", nestedIndent));
        lines.Add($"{new string(' ', nestedIndent)}&RestStatusCode = 200");
        lines.Add($"{new string(' ', assignmentIndent)}Else");
        lines.AddRange(BusinessRuleFailureMessages(bc, nestedIndent));
        lines.Add($"{new string(' ', assignmentIndent)}EndIf");
        lines.Add($"{bodyIndent}Else");
        lines.AddRange(NotFoundMessages(plan, failureIndent));
        lines.Add($"{bodyIndent}EndIf");
        if (guarded)
        {
            lines.Add("EndIf");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string PreviousB079UpdateContentWithWrappedRequiredMemberValidation(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var requiredFields = RequiredFieldsFor(plan, "UpdateRequest", plan.UpdateRequestFields);
        var guarded = requiredFields.Count > 0;
        var bodyIndent = guarded ? "    " : string.Empty;
        var assignmentIndent = guarded ? 8 : 4;
        var nestedIndent = guarded ? 12 : 8;
        var failureIndent = guarded ? 8 : 4;
        var lines = new List<string> { "&RestStatusCode = 200" };
        lines.AddRange(PreviousB079WrappedRequiredMemberPresenceValidation("UpdateRequest", requiredFields, 0));
        if (guarded)
        {
            lines.Add("If &RestStatusCode = 200");
        }

        lines.Add($"{bodyIndent}{bc}.Load({LoadArguments(plan, "&")})");
        lines.Add($"{bodyIndent}If {bc}.Success()");
        lines.AddRange(plan.UpdateRequestFields.Select(field => $"{new string(' ', assignmentIndent)}{bc}.{field.Name} = &UpdateRequest.{field.Name}"));
        lines.Add($"{new string(' ', assignmentIndent)}{bc}.Save()");
        lines.Add($"{new string(' ', assignmentIndent)}If {bc}.Success()");
        lines.Add($"{new string(' ', nestedIndent)}Commit");
        lines.Add($"{new string(' ', nestedIndent)}{bc}.Load({LoadArguments(plan, "&")})");
        lines.Add($"{new string(' ', nestedIndent)}&UpdateResponse = new()");
        lines.AddRange(ResponseAssignments(plan, bc, "&UpdateResponse", nestedIndent));
        lines.Add($"{new string(' ', nestedIndent)}&RestStatusCode = 200");
        lines.Add($"{new string(' ', assignmentIndent)}Else");
        lines.AddRange(BusinessRuleFailureMessages(bc, nestedIndent));
        lines.Add($"{new string(' ', assignmentIndent)}EndIf");
        lines.Add($"{bodyIndent}Else");
        lines.AddRange(NotFoundMessages(plan, failureIndent));
        lines.Add($"{bodyIndent}EndIf");
        if (guarded)
        {
            lines.Add("EndIf");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string PreviousB079UpdateContentWithUnwrappedRequiredMemberValidation(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var requiredFields = RequiredFieldsFor(plan, "UpdateRequest", plan.UpdateRequestFields);
        var guarded = requiredFields.Count > 0;
        var bodyIndent = guarded ? "    " : string.Empty;
        var assignmentIndent = guarded ? 8 : 4;
        var nestedIndent = guarded ? 12 : 8;
        var failureIndent = guarded ? 8 : 4;
        var lines = new List<string> { "&RestStatusCode = 200" };
        lines.AddRange(PreviousB079UnwrappedRequiredMemberPresenceValidation(requiredFields, 0));
        if (guarded)
        {
            lines.Add("If &RestStatusCode = 200");
        }

        lines.Add($"{bodyIndent}{bc}.Load({LoadArguments(plan, "&")})");
        lines.Add($"{bodyIndent}If {bc}.Success()");
        lines.AddRange(plan.UpdateRequestFields.Select(field => $"{new string(' ', assignmentIndent)}{bc}.{field.Name} = &UpdateRequest.{field.Name}"));
        lines.Add($"{new string(' ', assignmentIndent)}{bc}.Save()");
        lines.Add($"{new string(' ', assignmentIndent)}If {bc}.Success()");
        lines.Add($"{new string(' ', nestedIndent)}Commit");
        lines.Add($"{new string(' ', nestedIndent)}{bc}.Load({LoadArguments(plan, "&")})");
        lines.Add($"{new string(' ', nestedIndent)}&UpdateResponse = new()");
        lines.AddRange(ResponseAssignments(plan, bc, "&UpdateResponse", nestedIndent));
        lines.Add($"{new string(' ', nestedIndent)}&RestStatusCode = 200");
        lines.Add($"{new string(' ', assignmentIndent)}Else");
        lines.AddRange(BusinessRuleFailureMessages(bc, nestedIndent));
        lines.Add($"{new string(' ', assignmentIndent)}EndIf");
        lines.Add($"{bodyIndent}Else");
        lines.AddRange(NotFoundMessages(plan, failureIndent));
        lines.Add($"{bodyIndent}EndIf");
        if (guarded)
        {
            lines.Add("EndIf");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string PreviousB079UpdateContentWithOriginalMemberDirtyValidation(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var requiredFields = RequiredFieldsFor(plan, "UpdateRequest", plan.UpdateRequestFields);
        var guarded = requiredFields.Count > 0;
        var bodyIndent = guarded ? "    " : string.Empty;
        var assignmentIndent = guarded ? 8 : 4;
        var nestedIndent = guarded ? 12 : 8;
        var failureIndent = guarded ? 8 : 4;
        var lines = new List<string> { "&RestStatusCode = 200" };
        lines.AddRange(PreviousB079OriginalMemberDirtyPresenceValidation("&UpdateRequest", requiredFields, 0));
        if (guarded)
        {
            lines.Add("If &RestStatusCode = 200");
        }

        lines.Add($"{bodyIndent}{bc}.Load({LoadArguments(plan, "&")})");
        lines.Add($"{bodyIndent}If {bc}.Success()");
        lines.AddRange(plan.UpdateRequestFields.Select(field => $"{new string(' ', assignmentIndent)}{bc}.{field.Name} = &UpdateRequest.{field.Name}"));
        lines.Add($"{new string(' ', assignmentIndent)}{bc}.Save()");
        lines.Add($"{new string(' ', assignmentIndent)}If {bc}.Success()");
        lines.Add($"{new string(' ', nestedIndent)}Commit");
        lines.Add($"{new string(' ', nestedIndent)}{bc}.Load({LoadArguments(plan, "&")})");
        lines.Add($"{new string(' ', nestedIndent)}&UpdateResponse = new()");
        lines.AddRange(ResponseAssignments(plan, bc, "&UpdateResponse", nestedIndent));
        lines.Add($"{new string(' ', nestedIndent)}&RestStatusCode = 200");
        lines.Add($"{new string(' ', assignmentIndent)}Else");
        lines.AddRange(BusinessRuleFailureMessages(bc, nestedIndent));
        lines.Add($"{new string(' ', assignmentIndent)}EndIf");
        lines.Add($"{bodyIndent}Else");
        lines.AddRange(NotFoundMessages(plan, failureIndent));
        lines.Add($"{bodyIndent}EndIf");
        if (guarded)
        {
            lines.Add("EndIf");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string PreviousB079UpdateContentWithNativeJsonValidation(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var requiredFields = RequiredFieldsFor(plan, "UpdateRequest", plan.UpdateRequestFields);
        var guarded = requiredFields.Count > 0;
        var bodyIndent = guarded ? "    " : string.Empty;
        var assignmentIndent = guarded ? 8 : 4;
        var nestedIndent = guarded ? 12 : 8;
        var failureIndent = guarded ? 8 : 4;
        var lines = new List<string> { "&RestStatusCode = 200" };
        lines.AddRange(NativeJsonRequiredMemberPresenceValidation("UpdateRequest", requiredFields, 0));
        if (guarded)
        {
            lines.Add("If &RestStatusCode = 200");
        }

        lines.Add($"{bodyIndent}{bc}.Load({LoadArguments(plan, "&")})");
        lines.Add($"{bodyIndent}If {bc}.Success()");
        lines.AddRange(plan.UpdateRequestFields.Select(field => $"{new string(' ', assignmentIndent)}{bc}.{field.Name} = &UpdateRequest.{field.Name}"));
        lines.Add($"{new string(' ', assignmentIndent)}{bc}.Save()");
        lines.Add($"{new string(' ', assignmentIndent)}If {bc}.Success()");
        lines.Add($"{new string(' ', nestedIndent)}Commit");
        lines.Add($"{new string(' ', nestedIndent)}{bc}.Load({LoadArguments(plan, "&")})");
        lines.Add($"{new string(' ', nestedIndent)}&UpdateResponse = new()");
        lines.AddRange(ResponseAssignments(plan, bc, "&UpdateResponse", nestedIndent));
        lines.Add($"{new string(' ', nestedIndent)}&RestStatusCode = 200");
        lines.Add($"{new string(' ', assignmentIndent)}Else");
        lines.AddRange(BusinessRuleFailureMessages(bc, nestedIndent));
        lines.Add($"{new string(' ', assignmentIndent)}EndIf");
        lines.Add($"{bodyIndent}Else");
        lines.AddRange(NotFoundMessages(plan, failureIndent));
        lines.Add($"{bodyIndent}EndIf");
        if (guarded)
        {
            lines.Add("EndIf");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string PreviousB079UpdateContentWithSdtDirtyValidation(ApiPlan plan)
    {
        var bc = "&" + plan.TransactionName;
        var requiredFields = RequiredFieldsFor(plan, "UpdateRequest", plan.UpdateRequestFields);
        var guarded = requiredFields.Count > 0;
        var bodyIndent = guarded ? "    " : string.Empty;
        var assignmentIndent = guarded ? 8 : 4;
        var nestedIndent = guarded ? 12 : 8;
        var failureIndent = guarded ? 8 : 4;
        var lines = new List<string> { "&RestStatusCode = 200" };
        lines.AddRange(PreviousB079SdtDirtyMemberPresenceValidation("&UpdateRequest", requiredFields, 0, useDirtyMemberNames: true));
        if (guarded)
        {
            lines.Add("If &RestStatusCode = 200");
        }

        lines.Add($"{bodyIndent}{bc}.Load({LoadArguments(plan, "&")})");
        lines.Add($"{bodyIndent}If {bc}.Success()");
        lines.AddRange(plan.UpdateRequestFields.Select(field => $"{new string(' ', assignmentIndent)}{bc}.{field.Name} = &UpdateRequest.{field.Name}"));
        lines.Add($"{new string(' ', assignmentIndent)}{bc}.Save()");
        lines.Add($"{new string(' ', assignmentIndent)}If {bc}.Success()");
        lines.Add($"{new string(' ', nestedIndent)}Commit");
        lines.Add($"{new string(' ', nestedIndent)}{bc}.Load({LoadArguments(plan, "&")})");
        lines.Add($"{new string(' ', nestedIndent)}&UpdateResponse = new()");
        lines.AddRange(ResponseAssignments(plan, bc, "&UpdateResponse", nestedIndent));
        lines.Add($"{new string(' ', nestedIndent)}&RestStatusCode = 200");
        lines.Add($"{new string(' ', assignmentIndent)}Else");
        lines.AddRange(BusinessRuleFailureMessages(bc, nestedIndent));
        lines.Add($"{new string(' ', assignmentIndent)}EndIf");
        lines.Add($"{bodyIndent}Else");
        lines.AddRange(NotFoundMessages(plan, failureIndent));
        lines.Add($"{bodyIndent}EndIf");
        if (guarded)
        {
            lines.Add("EndIf");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static IEnumerable<string> LegacyFailureMessages(string bc, int spaces) => new[]
    {
        $"{new string(' ', spaces)}&Messages = {bc}.GetMessages()",
        $"{new string(' ', spaces)}msg(Format(!\"Genexus Open API Builder B055 BC failure: %1\", &Messages.ToJson()), status)",
    };

    private static IEnumerable<string> BusinessRuleFailureMessages(string bc, int spaces)
    {
        var indent = new string(' ', spaces);
        yield return $"{indent}&RestStatusCode = 422";
        yield return $"{indent}&ErrorResponse = new()";
        yield return $"{indent}&ErrorResponse.Code = !\"validation_error\"";
        yield return $"{indent}&ErrorResponse.Message = !\"Business rules rejected the request.\"";
        yield return $"{indent}&Messages = {bc}.GetMessages()";
        yield return $"{indent}msg(Format(!\"Genexus Open API Builder B079 BC failure: %1\", &Messages.ToJson()), status)";
    }

    private static IEnumerable<string> PreviousB079BusinessRuleFailureMessages(string bc, int spaces)
    {
        var indent = new string(' ', spaces);
        yield return $"{indent}&RestStatusCode = 422";
        yield return $"{indent}&ErrorResponse = new()";
        yield return $"{indent}&ErrorResponse.Code = !\"validation_error\"";
        yield return $"{indent}&ErrorResponse.Message = !\"Business rules rejected the request.\"";
        yield return $"{indent}&Messages = {bc}.GetMessages()";
        yield return $"{indent}msg(Format(!\"Genexus Open API Builder B079 BC failure: %1\", &Messages.ToJson()), status)";
    }

    private static IEnumerable<string> NotFoundMessages(ApiPlan plan, int spaces)
    {
        var indent = new string(' ', spaces);
        yield return $"{indent}&RestStatusCode = 404";
        yield return $"{indent}&ErrorResponse = new()";
        yield return $"{indent}&ErrorResponse.Code = !\"not_found\"";
        yield return $"{indent}&ErrorResponse.Message = !\"{plan.TransactionName} was not found.\"";
    }

    internal static IReadOnlyList<VariableSpec> GetVariables(ApiPlan plan) => plan.PrimaryKey.Select(field => new VariableSpec(field.Name, $"Attribute:{field.Name}"))
        .Concat(new[]
        {
            new VariableSpec("GetResponse", plan.ResponseSdtName),
            new VariableSpec("ErrorResponse", "sdt_API_ErrorResponse"),
            new VariableSpec("RestStatusCode", "Numeric(3.0)"),
            new VariableSpec(plan.TransactionName, plan.TransactionName),
        })
        .ToArray();

    internal static IReadOnlyList<VariableSpec> CreateVariables(ApiPlan plan) => new[]
    {
        new VariableSpec("CreateRequest", plan.CreateRequestSdtName),
        new VariableSpec("CreateResponse", plan.ResponseSdtName),
        new VariableSpec("ErrorResponse", "sdt_API_ErrorResponse"),
        new VariableSpec("HttpResponse", "HttpResponse"),
        new VariableSpec("RestStatusCode", "Numeric(3.0)"),
        new VariableSpec(plan.TransactionName, plan.TransactionName),
        new VariableSpec("Messages", "Messages, GeneXus.Common"),
    }.Concat(RequiredMemberPresenceVariables(plan, "CreateRequest", plan.CreateRequestFields)).ToArray();

    private static IReadOnlyList<VariableSpec> LegacyCreateVariables(ApiPlan plan) => new[]
    {
        new VariableSpec("CreateRequest", plan.CreateRequestSdtName),
        new VariableSpec("CreateResponse", plan.ResponseSdtName),
        new VariableSpec(plan.TransactionName, plan.TransactionName),
        new VariableSpec("Messages", "Messages, GeneXus.Common"),
    };

    private static IReadOnlyList<VariableSpec> PreviousB079CreateVariables(ApiPlan plan) => new[]
    {
        new VariableSpec("CreateRequest", plan.CreateRequestSdtName),
        new VariableSpec("CreateResponse", plan.ResponseSdtName),
        new VariableSpec("ErrorResponse", "sdt_API_ErrorResponse"),
        new VariableSpec("RestStatusCode", "Numeric(3.0)"),
        new VariableSpec(plan.TransactionName, plan.TransactionName),
        new VariableSpec("Messages", "Messages, GeneXus.Common"),
    };

    internal static IReadOnlyList<VariableSpec> UpdateVariables(ApiPlan plan) => plan.PrimaryKey.Select(field => new VariableSpec(field.Name, $"Attribute:{field.Name}"))
        .Concat(new[]
        {
            new VariableSpec("UpdateRequest", plan.UpdateRequestSdtName),
            new VariableSpec("UpdateResponse", plan.ResponseSdtName),
            new VariableSpec("ErrorResponse", "sdt_API_ErrorResponse"),
            new VariableSpec("RestStatusCode", "Numeric(3.0)"),
            new VariableSpec(plan.TransactionName, plan.TransactionName),
            new VariableSpec("Messages", "Messages, GeneXus.Common"),
        })
        .Concat(RequiredMemberPresenceVariables(plan, "UpdateRequest", plan.UpdateRequestFields))
        .ToArray();

    private static IReadOnlyList<VariableSpec> LegacyUpdateVariables(ApiPlan plan) => plan.PrimaryKey.Select(field => new VariableSpec(field.Name, $"Attribute:{field.Name}"))
        .Concat(new[]
        {
            new VariableSpec("UpdateRequest", plan.UpdateRequestSdtName),
            new VariableSpec("UpdateResponse", plan.ResponseSdtName),
            new VariableSpec(plan.TransactionName, plan.TransactionName),
            new VariableSpec("Messages", "Messages, GeneXus.Common"),
        })
        .ToArray();

    private static IReadOnlyList<VariableSpec> PreviousB079UpdateVariables(ApiPlan plan) => plan.PrimaryKey.Select(field => new VariableSpec(field.Name, $"Attribute:{field.Name}"))
        .Concat(new[]
        {
            new VariableSpec("UpdateRequest", plan.UpdateRequestSdtName),
            new VariableSpec("UpdateResponse", plan.ResponseSdtName),
            new VariableSpec("ErrorResponse", "sdt_API_ErrorResponse"),
            new VariableSpec("ErrorItem", "sdt_API_ErrorResponse.Error"),
            new VariableSpec("RestStatusCode", "Numeric(3.0)"),
            new VariableSpec(plan.TransactionName, plan.TransactionName),
            new VariableSpec("Message", "Messages.Message, GeneXus.Common"),
            new VariableSpec("Messages", "Messages, GeneXus.Common"),
        })
        .ToArray();

    private static IEnumerable<VariableSpec> RequiredMemberPresenceVariables(ApiPlan plan, string requestName, IReadOnlyList<ApiPlanField> candidateFields)
    {
        if (RequiredFieldsFor(plan, requestName, candidateFields).Count == 0)
        {
            return Array.Empty<VariableSpec>();
        }

        return new[]
        {
            new VariableSpec("RequestJsonHasRequiredMembers", "Boolean"),
            new VariableSpec("MissingRequiredFields", "VarChar(1K)"),
            new VariableSpec(EmptyRequestVariableName(requestName), RequestSdtName(plan, requestName)),
        };
    }

    private static IReadOnlyList<VariableSpec> ApiVariableSpecs(ApiPlan plan) => plan.PrimaryKey.Select(field => new VariableSpec(field.Name, $"Attribute:{field.Name}"))
        .Concat(new[]
        {
            new VariableSpec("GetResponse", plan.ResponseSdtName),
            new VariableSpec("CreateRequest", plan.CreateRequestSdtName, isServiceRequired: true),
            new VariableSpec("CreateResponse", plan.ResponseSdtName),
            new VariableSpec("UpdateRequest", plan.UpdateRequestSdtName, isServiceRequired: true),
            new VariableSpec("UpdateResponse", plan.ResponseSdtName),
            new VariableSpec("ErrorResponse", "sdt_API_ErrorResponse"),
            new VariableSpec("RestStatusCode", "Numeric(3.0)"),
        })
        .ToArray();

    private static IReadOnlyList<VariableSpec> LegacyApiVariableSpecs(ApiPlan plan) => plan.PrimaryKey.Select(field => new VariableSpec(field.Name, $"Attribute:{field.Name}"))
        .Concat(new[]
        {
            new VariableSpec("CreateRequest", plan.CreateRequestSdtName),
            new VariableSpec("CreateResponse", plan.ResponseSdtName),
            new VariableSpec("UpdateRequest", plan.UpdateRequestSdtName),
            new VariableSpec("UpdateResponse", plan.ResponseSdtName),
        })
        .ToArray();

    private static IReadOnlyList<VariableSpec> CoalesceVariableSpecs(IEnumerable<VariableSpec> variables, string backlog)
    {
        var resolved = new List<VariableSpec>();
        foreach (var variable in variables)
        {
            var existing = resolved.FirstOrDefault(item => string.Equals(item.Name, variable.Name, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                resolved.Add(variable);
                continue;
            }

            if (!string.Equals(existing.DataType, variable.DataType, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"{backlog} bloqueado: variavel '&{variable.Name}' foi planejada com tipos divergentes: '{existing.DataType}' e '{variable.DataType}'. Nenhuma alteracao foi feita.");
            }
        }

        return resolved;
    }

    private static string ApiVariables(ApiPlan plan) => string.Join(Environment.NewLine, ApiVariableSpecs(plan).Select(variable => $"{variable.Name} [ DataType = '{variable.DataType}' ]"));

    internal static string GetRules(ApiPlan plan) => $"parm({string.Join(", ", plan.PrimaryKey.Select(field => $"in:&{field.Name}").Concat(new[] { "out:&GetResponse", "out:&ErrorResponse", "out:&RestStatusCode" }))});";
    internal static string CreateRules() => "parm(in:&CreateRequest, out:&CreateResponse, out:&ErrorResponse, out:&RestStatusCode);";
    internal static string UpdateRules(ApiPlan plan) => $"parm({string.Join(", ", plan.PrimaryKey.Select(field => $"in:&{field.Name}").Concat(new[] { "in:&UpdateRequest", "out:&UpdateResponse", "out:&ErrorResponse", "out:&RestStatusCode" }))});";
    private static string LegacyCreateRules() => "parm(in:&CreateRequest, out:&CreateResponse);";
    private static string LegacyUpdateRules(ApiPlan plan) => $"parm({string.Join(", ", plan.PrimaryKey.Select(field => $"in:&{field.Name}").Concat(new[] { "in:&UpdateRequest", "out:&UpdateResponse" }))});";
    private static string LoadArguments(ApiPlan plan, string prefix) => string.Join(", ", plan.PrimaryKey.Select(field => prefix == "&" ? $"&{field.Name}" : $"{prefix}.{field.Name}"));
    private static bool HasService(ApiPlan plan, string name) => plan.Services.Any(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
    private static string Skeleton(string backlog, string service) => $"// Genexus Open API Builder {backlog}: Procedure skeleton for {service}. REST behavior remains pending Sprint 6." + Environment.NewLine + $"msg(!\"Genexus Open API Builder {backlog} {service} skeleton. REST behavior pending Sprint 6.\", status)";
    private static string ServiceSource(ApiPlan plan, string service, bool includeBusinessComponentParameters, bool includeDescriptions, bool exposeErrorResponse)
    {
        var procedure = ExpectedProcedureReference(plan, $"proc{plan.TransactionName}_API_{service}");
        var annotation = ServiceAnnotations(plan, service, includeDescriptions, includeRestMethod: true);
        if (includeBusinessComponentParameters && string.Equals(service, "Get", StringComparison.OrdinalIgnoreCase))
        {
            var parameters = string.Join(", ", plan.PrimaryKey.Select(field => $"in: &{field.Name}").Concat(exposeErrorResponse ? new[] { "out: &GetResponse", "out: &ErrorResponse" } : new[] { "out: &GetResponse" }));
            var arguments = string.Join(", ", plan.PrimaryKey.Select(field => $"&{field.Name}").Concat(new[] { "&GetResponse", "&ErrorResponse", "&RestStatusCode" }));
            return annotation + $"    Get({parameters}){Environment.NewLine}        => {procedure}({arguments});";
        }

        if (includeBusinessComponentParameters && string.Equals(service, "Create", StringComparison.OrdinalIgnoreCase))
            return annotation + $"    Create(in: &CreateRequest, out: &CreateResponse{(exposeErrorResponse ? ", out: &ErrorResponse" : string.Empty)}){Environment.NewLine}        => {procedure}(&CreateRequest, &CreateResponse, &ErrorResponse, &RestStatusCode);";
        if (includeBusinessComponentParameters && string.Equals(service, "Update", StringComparison.OrdinalIgnoreCase))
        {
            var parameters = string.Join(", ", plan.PrimaryKey.Select(field => $"in: &{field.Name}").Concat(exposeErrorResponse ? new[] { "in: &UpdateRequest", "out: &UpdateResponse", "out: &ErrorResponse" } : new[] { "in: &UpdateRequest", "out: &UpdateResponse" }));
            var arguments = string.Join(", ", plan.PrimaryKey.Select(field => $"&{field.Name}").Concat(new[] { "&UpdateRequest", "&UpdateResponse", "&ErrorResponse", "&RestStatusCode" }));
            return annotation + $"    Update({parameters}){Environment.NewLine}        => {procedure}({arguments});";
        }
        return annotation + $"    {service}(){Environment.NewLine}        => {procedure}();";
    }

    internal static string CreateB079ApiEvents()
    {
        return string.Join(Environment.NewLine, new[]
        {
            "Event Get.After",
            "    &RestCode = &RestStatusCode",
            "EndEvent",
            string.Empty,
            "Event Create.After",
            "    &RestCode = &RestStatusCode",
            "EndEvent",
            string.Empty,
            "Event Update.After",
            "    &RestCode = &RestStatusCode",
            "EndEvent",
        });
    }

    internal static bool HasExpectedApiEvents(API api)
    {
        if (api is null)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(api.Events.Source) ||
            string.Equals(NormalizeForComparison(api.Events.Source), NormalizeForComparison(CreateB079ApiEvents()), StringComparison.Ordinal);
    }

    private static string ServiceAnnotations(ApiPlan plan, string service, bool includeDescriptions, bool includeRestMethod)
    {
        var annotations = new List<string>();
        if (includeDescriptions)
        {
            annotations.Add($"    [Description(\"{EscapeDescription(ResolveServiceDescription(plan, service))}\")]");
        }

        if (includeRestMethod)
        {
            var method = ResolveService(plan, service).HttpMethod.Trim();
            if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                annotations.Add($"    [RestMethod({method.ToUpperInvariant()})]");
            }
        }

        annotations.Add($"    [RestPath(\"{EscapeDescription(ResolveService(plan, service).RestPath.Trim())}\")]");
        annotations.Add($"    [SecurityLevel({plan.Security.SecurityLevel})]");
        return string.Join(Environment.NewLine, annotations) + Environment.NewLine;
    }

    private static string DescriptionAnnotation(ApiPlan plan, string service) => $"    [Description(\"{EscapeDescription(ResolveServiceDescription(plan, service))}\")]";

    private static ApiPlanService ResolveService(ApiPlan plan, string service)
    {
        var matches = plan.Services
            .Where(item => string.Equals(item.Name, service, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (matches.Length != 1)
        {
            throw new InvalidOperationException($"B071-B073/B079 bloqueado: servico '{service}' nao foi reencontrado de forma unica no ApiPlan. Nenhuma alteracao foi feita.");
        }

        return matches[0];
    }

    private static string ResolveServiceDescription(ApiPlan plan, string service)
    {
        var matches = plan.ServiceDescriptions
            .Where(item => string.Equals(item.ServiceName, service, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (matches.Length != 1)
        {
            throw new InvalidOperationException($"B056 bloqueado: descricao do servico '{service}' nao foi reencontrada de forma unica no ApiPlan. Nenhuma alteracao foi feita.");
        }

        var description = matches[0].Description?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(description) || string.Equals(description, ApiPlan.UnresolvedB056ServiceDescription, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"B056 bloqueado: descricao do servico '{service}' nao esta resolvida no ApiPlan. Nenhuma alteracao foi feita.");
        }

        if (description.IndexOfAny(new[] { '\r', '\n' }) >= 0)
        {
            throw new InvalidOperationException($"B056 bloqueado: descricao do servico '{service}' contem quebra de linha. Nenhuma alteracao foi feita.");
        }

        return description;
    }

    private static string EscapeDescription(string description) => description.Replace("\\", "\\\\").Replace("\"", "\\\"");
}

internal sealed class VariableSpec
{
    public VariableSpec(string name, string dataType, bool isServiceRequired = false)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
        IsServiceRequired = isServiceRequired;
    }

    public string Name { get; }
    public string DataType { get; }
    public bool IsServiceRequired { get; }
}

internal sealed class ApiPlanBusinessComponentWriteResult
{
    public ApiPlanBusinessComponentWriteResult(Guid getProcedureGuid, Guid createProcedureGuid, Guid updateProcedureGuid, Guid apiObjectGuid, int primaryKeyParts, int createFields, int updateFields, int responseFields)
    {
        GetProcedureGuid = getProcedureGuid;
        CreateProcedureGuid = createProcedureGuid;
        UpdateProcedureGuid = updateProcedureGuid;
        ApiObjectGuid = apiObjectGuid;
        PrimaryKeyParts = primaryKeyParts;
        CreateFields = createFields;
        UpdateFields = updateFields;
        ResponseFields = responseFields;
    }

    public Guid GetProcedureGuid { get; }
    public Guid CreateProcedureGuid { get; }
    public Guid UpdateProcedureGuid { get; }
    public Guid ApiObjectGuid { get; }
    public int PrimaryKeyParts { get; }
    public int CreateFields { get; }
    public int UpdateFields { get; }
    public int ResponseFields { get; }
}
