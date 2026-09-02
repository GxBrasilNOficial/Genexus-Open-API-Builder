using System;
using System.Collections.Generic;
using System.Linq;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common;
using Artech.Genexus.Common.Objects;
using Artech.Genexus.Common.Types;
using GenexusOpenApiBuilder.Extension.Domain;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

internal static class ApiPlanListProcedureWriter
{
    private const string PageVariableName = "ApiPage";
    private const string PageSizeVariableName = "ApiPageSize";
    private const string PageParameterName = "pApiPage";
    private const string PageSizeParameterName = "pApiPageSize";

    public static ApiPlanListProcedureWriteResult Apply(KBModel model, Transaction transaction, ApiPlan plan)
    {
        return Apply(model, transaction, plan, allowIntentionalContractRefresh: false, preserveSdtNames: null);
    }

    public static ApiPlanListProcedureWriteResult Apply(
        KBModel model,
        Transaction transaction,
        ApiPlan plan,
        bool allowIntentionalContractRefresh)
    {
        return Apply(model, transaction, plan, allowIntentionalContractRefresh, preserveSdtNames: null);
    }

    public static ApiPlanListProcedureWriteResult Apply(
        KBModel model,
        Transaction transaction,
        ApiPlan plan,
        bool allowIntentionalContractRefresh,
        IReadOnlyCollection<string>? preserveSdtNames,
        System.Action<ApiPlanSdtWriteItemResult>? onSdtWrite = null,
        ApiPlanBusyProgressSession? progress = null,
        ApiPlanKbObjectNameIndex? kbIndex = null)
    {
        if (model is null) throw new ArgumentNullException(nameof(model));
        if (transaction is null) throw new ArgumentNullException(nameof(transaction));
        if (plan is null) throw new ArgumentNullException(nameof(plan));
        if (!string.Equals(transaction.Name, plan.TransactionName, StringComparison.Ordinal))
            throw new InvalidOperationException("B070 bloqueado: o ApiPlan nao pertence a Transaction atual. Nenhuma alteracao foi feita.");
        if (!HasService(plan, "List"))
            throw new InvalidOperationException("B070 bloqueado: o ApiPlan precisa conter List. Nenhuma alteracao foi feita.");

        progress?.Report("List", 0, 0, "Preparando");
        progress?.PumpAndThrowIfAbortRequested();
        ApiPlanSdtWriter.Preflight(model, transaction, plan);
        var procedure = FindListProcedure(model, plan);
        var api = FindApi(model, plan, allowIntentionalContractRefresh);
        var source = CreateListSource(plan);
        var rules = CreateListRules(plan);
        var procedureVariables = ProcedureVariableSpecs(plan);
        var includeBusinessComponentParameters =
            ApiPlanBusinessComponentWriter.IsB055ApiObject(model, plan, api) ||
            IsB070ApiObjectWithBusinessComponentParameters(model, plan, api);
        var apiSource = CreateB070ServiceGroupSource(plan, includeBusinessComponentParameters);
        var apiVariables = CoalesceVariableSpecs(ApiVariableSpecs(plan, includeBusinessComponentParameters));

        ValidateGeneratedVariableNames(procedureVariables.Concat(apiVariables));
        EnsureProcedure(procedure, plan, source, rules, procedureVariables, allowIntentionalContractRefresh);
        if (!allowIntentionalContractRefresh)
        {
            EnsureApi(model, api, plan);
        }
        ValidateVariableSpecs(model, procedure, procedureVariables);
        ValidateVariableSpecs(model, api, apiVariables);

        progress?.PumpAndThrowIfAbortRequested();
        ApiPlanSdtWriter.CreateOrReencounter(model, transaction, plan, preserveSdtNames, onSdtWrite, progress, kbIndex);
        progress?.PumpAndThrowIfAbortRequested();
        var transactionFolder = ApiPlanTransactionFolder.CreateOrReencounter(model, transaction, plan);

        var saveSteps = new (string Label, System.Action Save)[]
        {
            (api.Name, () => SaveApi(model, api, transactionFolder, plan, apiSource, apiVariables)),
            (procedure.Name, () => SaveProcedure(model, procedure, source, procedureVariables, rules)),
        };
        var saveIndex = 0;
        foreach (var step in saveSteps)
        {
            progress?.ThrowIfAbortRequested();
            saveIndex++;
            progress?.Report("List", saveIndex, saveSteps.Length, step.Label);
            progress?.Pump();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            step.Save();
            sw.Stop();
            progress?.Report("List", saveIndex, saveSteps.Length, step.Label, sw.ElapsedMilliseconds);
        }

        return new ApiPlanListProcedureWriteResult(
            procedure.Guid,
            api.Guid,
            plan.ListFilters.Count,
            plan.StaticOrder.Count,
            plan.DefaultPageSize,
            plan.MaximumPageSize);
    }

    internal static bool IsB070ApiObject(KBModel model, ApiPlan plan, API api)
    {
        if (api is null)
        {
            return false;
        }

        return (IsB070ServiceGroupSource(plan, api.ServiceGroupSource.Source, includeBusinessComponentParameters: false) &&
                (HasExpectedVariables(model, api, CoalesceVariableSpecs(ApiVariableSpecs(plan, includeBusinessComponentParameters: false))) ||
                 HasExpectedVariables(model, api, CoalesceVariableSpecs(PreviousB070ApiVariableSpecs(plan, includeBusinessComponentParameters: false)))))
            || (IsB070ServiceGroupSource(plan, api.ServiceGroupSource.Source, includeBusinessComponentParameters: true) &&
                (HasExpectedVariables(model, api, CoalesceVariableSpecs(ApiVariableSpecs(plan, includeBusinessComponentParameters: true))) ||
                 HasExpectedVariables(model, api, CoalesceVariableSpecs(PreviousB070ApiVariableSpecs(plan, includeBusinessComponentParameters: true)))) &&
                ApiPlanBusinessComponentWriter.HasManagedApiEvents(api, plan));
    }

    private static bool IsB070ApiObjectWithBusinessComponentParameters(KBModel model, ApiPlan plan, API api)
    {
        if (api is null)
        {
            return false;
        }

        return IsB070ServiceGroupSource(plan, api.ServiceGroupSource.Source, includeBusinessComponentParameters: true) &&
            HasExpectedVariables(model, api, CoalesceVariableSpecs(ApiVariableSpecs(plan, includeBusinessComponentParameters: true)));
    }

    private static bool IsB070ServiceGroupSource(ApiPlan plan, string source, bool includeBusinessComponentParameters)
    {
        return string.Equals(
                NormalizeForComparison(source),
                NormalizeForComparison(CreateB070ServiceGroupSource(plan, includeBusinessComponentParameters)),
                StringComparison.Ordinal) ||
            ApiPlanServiceSourceContract.MatchesCurrentB070(
                source,
                plan.ApiName,
                plan.TransactionName,
                plan.ModuleTarget,
                plan.Services.Select(service => service.Name),
                plan.PrimaryKey.Select(field => field.Name),
                plan.ListFilters.SelectMany(FilterVariableNames),
                includeBusinessComponentParameters) ||
            ApiPlanServiceSourceContract.MatchesB070(
                source,
                plan.ApiName,
                plan.TransactionName,
                plan.ModuleTarget,
                plan.Services.Select(service => service.Name),
                plan.PrimaryKey.Select(field => field.Name),
                plan.ListFilters.SelectMany(FilterVariableNames),
                includeBusinessComponentParameters) ||
            (includeBusinessComponentParameters &&
             ApiPlanServiceSourceContract.MatchesB079(
                 source,
                 plan.ApiName,
                 plan.TransactionName,
                 plan.ModuleTarget,
                 plan.Services.Select(service => service.Name),
                 plan.PrimaryKey.Select(field => field.Name),
                 plan.ListFilters.SelectMany(FilterVariableNames),
                 hasListContract: true) ||
             ApiPlanServiceSourceContract.MatchesB079InternalErrorOnly(
                 source,
                 plan.ApiName,
                 plan.TransactionName,
                 plan.ModuleTarget,
                 plan.Services.Select(service => service.Name),
                 plan.PrimaryKey.Select(field => field.Name),
                 plan.ListFilters.SelectMany(FilterVariableNames),
                 hasListContract: true) ||
             ApiPlanServiceSourceContract.MatchesPreviousB079SecurityLevelContract(
                 source,
                 plan.ApiName,
                 plan.TransactionName,
                 plan.ModuleTarget,
                 plan.Services.Select(service => service.Name),
                 plan.PrimaryKey.Select(field => field.Name),
                 plan.ListFilters.SelectMany(FilterVariableNames),
                 hasListContract: true) ||
             ApiPlanServiceSourceContract.MatchesPreviousB079RestMethodContract(
                 source,
                 plan.ApiName,
                 plan.TransactionName,
                 plan.ModuleTarget,
                 plan.Services.Select(service => service.Name),
                 plan.PrimaryKey.Select(field => field.Name),
                 plan.ListFilters.SelectMany(FilterVariableNames),
                 hasListContract: true));
    }

    private static Procedure FindListProcedure(KBModel model, ApiPlan plan)
    {
        var name = $"proc{plan.TransactionName}_API_List";
        var matches = ApiPlanScanProbe.Scan("Procedure", "list-find-procedure", () => Procedure.GetAll(model).Where(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)).ToArray());
        if (matches.Length != 1 || !ApiPlanOwnedObjectDescription.IsOwnedProcedure(matches.Single().Description, name))
            throw new InvalidOperationException($"B070 bloqueado: Procedure propria '{name}' nao foi reencontrada com seguranca. Gere as Procedures pelo Wizard antes. Nenhuma alteracao foi feita.");
        return matches.Single();
    }

    private static API FindApi(KBModel model, ApiPlan plan, bool allowIntentionalContractRefresh = false)
    {
        var matches = API.GetAll(model).Where(item => string.Equals(item.Name, plan.ApiName, StringComparison.OrdinalIgnoreCase)).ToArray();
        var owned = matches.Length == 1 &&
            (allowIntentionalContractRefresh
                ? ApiPlanApiObjectWriter.IsOwnedApiObjectForIntentionalWrite(model, plan, matches.Single())
                : ApiPlanApiObjectWriter.IsOwnedApiObject(model, plan, matches.Single()));
        if (!owned)
            throw new InvalidOperationException($"B070 bloqueado: API Object proprio '{plan.ApiName}' nao foi reencontrado com seguranca. Gere o API Object pelo Wizard antes. Nenhuma alteracao foi feita.");
        return matches.Single();
    }

    private static void EnsureProcedure(
        Procedure procedure,
        ApiPlan plan,
        string source,
        string rules,
        IReadOnlyList<VariableSpec> variables,
        bool allowIntentionalContractRefresh = false)
    {
        // Sync reconstrói Source/Rules/variáveis de propósito a partir do novo ApiPlan.
        // Ownership já foi comprovada por FindListProcedure (nome + Description).
        if (allowIntentionalContractRefresh)
        {
            return;
        }

        var currentSource = NormalizeForComparison(procedure.ProcedurePart.Source);
        var skeleton = Skeleton();
        var previousGeneratedSkeleton = PreviousGeneratedSkeleton();
        var legacySkeleton = LegacySkeleton();
        var legacySource = NormalizeForComparison(CreateLegacyListSource(plan));
        var previousB070Source = NormalizeForComparison(CreatePreviousB070ListSource(plan));
        var previousB077Source = NormalizeForComparison(CreatePreviousB077ListSource(plan));
        var previousConditionalB077Source = NormalizeForComparison(CreatePreviousConditionalB077ListSource(plan));
        var manualB077Source = NormalizeForComparison(CreateManualB077ListSource(plan));
        var invalidB077Source = NormalizeForComparison(CreateInvalidB077ListSource(plan));
        if (!ApiPlanListProcedureReencounterPolicy.IsSourceAllowed(
            currentSource,
            source,
            new[]
            {
                skeleton,
                previousGeneratedSkeleton,
                legacySkeleton,
                invalidB077Source,
                manualB077Source,
                previousConditionalB077Source,
                previousB077Source,
                previousB070Source,
                legacySource,
            }))
        {
            throw new InvalidOperationException($"B070 bloqueado: Procedure propria '{procedure.Name}' possui Source divergente da geracao B050/B070. Nenhuma alteracao foi feita.");
        }

        var currentRules = NormalizeForComparison(procedure.Rules.Source);
        var legacyRules = NormalizeForComparison(CreateLegacyListRules(plan));
        var previousB070Rules = NormalizeForComparison(CreatePreviousB070ListRules(plan));
        if (!ApiPlanListProcedureReencounterPolicy.IsRulesAllowed(currentRules, rules, legacyRules, new[] { previousB070Rules }))
        {
            throw new InvalidOperationException($"B070 bloqueado: Procedure propria '{procedure.Name}' possui Rules divergentes da geracao B070. Nenhuma alteracao foi feita.");
        }

        var currentVariables = procedure.Variables.Variables.Where(variable => !variable.IsStandard).Select(variable => variable.Name).ToArray();
        var variablesAllowed = currentVariables.Length == 0 ||
            ApiPlanListProcedureReencounterPolicy.AreVariablesAllowed(
                true,
                HasExpectedVariables(procedure.Model, procedure, variables),
                HasExpectedVariables(procedure.Model, procedure, PreviousConditionalB077ProcedureVariableSpecs(plan)),
                HasExpectedVariables(procedure.Model, procedure, InvalidB077ProcedureVariableSpecs(plan)),
                HasExpectedVariables(procedure.Model, procedure, PreviousB070ProcedureVariableSpecs(plan)),
                HasExpectedVariables(procedure.Model, procedure, LegacyProcedureVariableSpecs(plan)));
        if (!variablesAllowed)
        {
            throw new InvalidOperationException($"B070 bloqueado: Procedure propria '{procedure.Name}' possui variaveis divergentes da geracao B070. Nenhuma alteracao foi feita.");
        }
    }

    private static void EnsureApi(KBModel model, API api, ApiPlan plan)
    {
        if (!ApiPlanBusinessComponentWriter.IsManagedApiObject(model, plan, api))
        {
            throw new InvalidOperationException($"B070 bloqueado: API Object proprio '{api.Name}' possui fonte ou variaveis divergentes da geracao B054/B055/B070. Nenhuma alteracao foi feita.");
        }
    }

    /// <summary>
    /// Emissão atual de Source List para a linha de base offline da Fase 0.
    /// </summary>
    internal static string CreateCurrentListSource(ApiPlan plan) => CreateListSource(plan);

    private static string CreateListSource(ApiPlan plan)
    {
        return CreateListSource(plan, includeParameterCopy: true, initializeAppliedFilters: true, useAppliedFiltersVariable: true, attachAppliedFiltersImmediately: true, assignAppliedFiltersThroughResponse: false, trackAppliedFilters: false, useExplicitErrors: true);
    }

    private static string CreateLegacyListSource(ApiPlan plan)
    {
        return CreateListSource(plan, includeParameterCopy: false, initializeAppliedFilters: false, useExplicitErrors: false);
    }

    private static string CreatePreviousB070ListSource(ApiPlan plan)
    {
        return CreateListSource(plan, includeParameterCopy: true, initializeAppliedFilters: false, useExplicitErrors: false);
    }

    private static string CreateInvalidB077ListSource(ApiPlan plan)
    {
        return CreateListSource(plan, includeParameterCopy: true, initializeAppliedFilters: true, useAppliedFiltersVariable: false, assignAppliedFiltersThroughResponse: true, trackAppliedFilters: false, useExplicitErrors: false);
    }

    private static string CreatePreviousB077ListSource(ApiPlan plan)
    {
        return CreateListSource(plan, includeParameterCopy: true, initializeAppliedFilters: true, useAppliedFiltersVariable: true, attachAppliedFiltersImmediately: true, assignAppliedFiltersThroughResponse: true, trackAppliedFilters: false, useExplicitErrors: false);
    }

    private static string CreatePreviousConditionalB077ListSource(ApiPlan plan)
    {
        return CreateListSource(plan, includeParameterCopy: true, initializeAppliedFilters: true, useAppliedFiltersVariable: true, assignAppliedFiltersThroughResponse: false, trackAppliedFilters: true, useExplicitErrors: false);
    }

    private static string CreateManualB077ListSource(ApiPlan plan)
    {
        return CreateListSource(plan, includeParameterCopy: true, initializeAppliedFilters: true, useAppliedFiltersVariable: true, assignAppliedFiltersThroughResponse: false, trackAppliedFilters: true, commentConditionalAppliedFiltersAttachment: true, useExplicitErrors: false);
    }

    private static string CreateListSource(
        ApiPlan plan,
        bool includeParameterCopy,
        bool initializeAppliedFilters,
        bool useAppliedFiltersVariable = true,
        bool attachAppliedFiltersImmediately = false,
        bool assignAppliedFiltersThroughResponse = false,
        bool trackAppliedFilters = true,
        bool commentConditionalAppliedFiltersAttachment = false,
        bool useExplicitErrors = false)
    {
        var lines = new List<string>();
        if (useExplicitErrors)
        {
            lines.Add("&RestStatusCode = 200");
        }

        lines.Add("&ListResponse = new()");
        if (initializeAppliedFilters)
        {
            if (useAppliedFiltersVariable)
            {
                lines.Add("&AppliedFilters = new()");
                if (attachAppliedFiltersImmediately)
                {
                    lines.Add("&ListResponse.AppliedFilters = &AppliedFilters");
                }

                if (trackAppliedFilters)
                {
                    lines.Add("&AppliedFiltersWereApplied = 0");
                }
            }
            else
            {
                lines.Add("&ListResponse.AppliedFilters = new()");
            }
        }

        if (includeParameterCopy)
        {
            lines.Add($"&{PageVariableName} = &{PageParameterName}");
            lines.Add($"&{PageSizeVariableName} = &{PageSizeParameterName}");
        }

        lines.AddRange(new[]
        {
            $"If &{PageVariableName}.IsEmpty()",
            $"    &{PageVariableName} = 1",
            "EndIf",
            $"If &{PageSizeVariableName}.IsEmpty()",
            $"    &{PageSizeVariableName} = {plan.DefaultPageSize}",
            "EndIf",
        });

        lines.AddRange(InvalidRequestCondition(
            $"If &{PageVariableName} < 1",
            useExplicitErrors,
            "page must be greater than or equal to 1"));
        lines.AddRange(InvalidRequestCondition(
            $"If &{PageSizeVariableName} < 1",
            useExplicitErrors,
            "pageSize must be greater than or equal to 1"));
        lines.AddRange(InvalidRequestCondition(
            $"If &{PageSizeVariableName} > {plan.MaximumPageSize}",
            useExplicitErrors,
            "pageSize exceeds the configured maximum"));

        lines.AddRange(ValidateRanges(plan, useExplicitErrors));
        lines.Add($"&FirstRecord = ((&{PageVariableName} - 1) * &{PageSizeVariableName}) + 1");
        lines.Add($"&LastRecord = &{PageVariableName} * &{PageSizeVariableName}");
        lines.Add("&TotalCount = 0");
        lines.AddRange(AssignAppliedFilters(plan, assignAppliedFiltersThroughResponse, trackAppliedFilters));
        if (initializeAppliedFilters && useAppliedFiltersVariable && trackAppliedFilters && !attachAppliedFiltersImmediately)
        {
            lines.Add((commentConditionalAppliedFiltersAttachment ? "//" : string.Empty) + "If &AppliedFiltersWereApplied = 1");
            lines.Add("    &ListResponse.AppliedFilters = &AppliedFilters");
            lines.Add((commentConditionalAppliedFiltersAttachment ? "//" : string.Empty) + "EndIf");
        }

        lines.Add("For each");
        var order = ResolveDeterministicOrder(plan);
        if (order.Count > 0)
        {
            lines.Add("    order " + string.Join(", ", order.Select(FormatOrderPart)));
        }

        lines.AddRange(FilterWhereClauses(plan));
        lines.Add("    &TotalCount += 1");
        lines.Add("    If &TotalCount >= &FirstRecord and &TotalCount <= &LastRecord");
        lines.Add("        &Item = new()");
        lines.AddRange(plan.ResponseFields.Select(field => $"        &Item.{field.Name} = {field.Name}"));
        foreach (var count in ResolveListCountAssignments(plan))
        {
            lines.Add($"        &Item.{count.MemberName} = count({count.AggregateAttributeName})");
        }

        lines.Add("        &ListResponse.Items.Add(&Item)");
        lines.Add("    EndIf");
        lines.Add("EndFor");
        lines.Add($"&ListResponse.Pagination.Page = &{PageVariableName}");
        lines.Add($"&ListResponse.Pagination.PageSize = &{PageSizeVariableName}");
        lines.Add("&ListResponse.Pagination.TotalCount = &TotalCount");
        lines.Add("If &TotalCount = 0");
        lines.Add("    &ListResponse.Pagination.TotalPages = 0");
        lines.Add("Else");
        lines.Add($"    &ListResponse.Pagination.TotalPages = Int((&TotalCount + &{PageSizeVariableName} - 1) / &{PageSizeVariableName})");
        lines.Add("EndIf");
        return string.Join(Environment.NewLine, lines);
    }

    private static IReadOnlyList<ApiPlanListCountMember> ResolveListCountAssignments(ApiPlan plan)
    {
        if (!ApiPlanSdtHierarchicalNaming.HasSelectedSublevels(plan))
        {
            return Array.Empty<ApiPlanListCountMember>();
        }

        return ApiPlanListHierarchicalContractBuilder.Create(plan).Counts;
    }

    internal static string ResolveListItemSdtName(ApiPlan plan)
    {
        if (!ApiPlanSdtHierarchicalNaming.HasSelectedSublevels(plan))
        {
            return plan.ResponseSdtName;
        }

        var contract = ApiPlanListHierarchicalContractBuilder.Create(plan);
        if (!contract.HasListResponseItem)
        {
            throw new InvalidOperationException("List hierarquico bloqueado: ListResponse_Item ausente no contrato. Nenhuma alteracao foi feita.");
        }

        return contract.ListResponseItemSdtName;
    }

    private static IEnumerable<string> ValidateRanges(ApiPlan plan, bool useExplicitErrors)
    {
        foreach (var filter in plan.ListFilters.Where(item => item.UsesRange || item.UsesPeriod))
        {
            var first = FilterVariableName(filter, filter.UsesPeriod ? "From" : "Min");
            var second = FilterVariableName(filter, filter.UsesPeriod ? "To" : "Max");
            yield return $"If not &{first}.IsEmpty() and not &{second}.IsEmpty() and &{first} > &{second}";
            if (useExplicitErrors)
            {
                yield return "    &RestStatusCode = 400";
                yield return "    &ErrorResponse = new()";
                yield return "    &ErrorResponse.Code = !\"invalid_request\"";
                yield return "    &ErrorResponse.Message = !\"filter minimum is greater than maximum\"";
            }
            else
            {
                yield return "    msg(!\"invalid_request: filter minimum is greater than maximum\", status)";
            }

            yield return "    return";
            yield return "EndIf";
        }
    }

    private static IEnumerable<string> InvalidRequestCondition(string condition, bool useExplicitErrors, string message)
    {
        yield return condition;
        if (useExplicitErrors)
        {
            yield return "    &RestStatusCode = 400";
            yield return "    &ErrorResponse = new()";
            yield return "    &ErrorResponse.Code = !\"invalid_request\"";
            yield return $"    &ErrorResponse.Message = !\"{message}\"";
        }
        else
        {
            yield return $"    msg(!\"invalid_request: {message}\", status)";
        }

        yield return "    return";
        yield return "EndIf";
    }

    private static IEnumerable<string> AssignAppliedFilters(ApiPlan plan, bool assignThroughResponse, bool trackAppliedFilters)
    {
        foreach (var filter in plan.ListFilters.Where(item => !item.Field.IsSensitive))
        {
            foreach (var variable in FilterVariableNames(filter))
            {
                yield return $"If not &{variable}.IsEmpty()";
                yield return assignThroughResponse
                    ? $"    &ListResponse.AppliedFilters.{variable} = &{variable}"
                    : $"    &AppliedFilters.{variable} = &{variable}";
                if (trackAppliedFilters)
                {
                    yield return "    &AppliedFiltersWereApplied = 1";
                }

                yield return "EndIf";
            }
        }
    }

    private static IEnumerable<string> FilterWhereClauses(ApiPlan plan)
    {
        foreach (var filter in plan.ListFilters)
        {
            if (filter.UsesPeriod)
            {
                var from = FilterVariableName(filter, "From");
                var to = FilterVariableName(filter, "To");
                yield return $"    where {filter.Field.Name} >= &{from} when not &{from}.IsEmpty()";
                yield return $"    where {filter.Field.Name} <= &{to} when not &{to}.IsEmpty()";
                continue;
            }

            if (filter.UsesRange)
            {
                var min = FilterVariableName(filter, "Min");
                var max = FilterVariableName(filter, "Max");
                yield return $"    where {filter.Field.Name} >= &{min} when not &{min}.IsEmpty()";
                yield return $"    where {filter.Field.Name} <= &{max} when not &{max}.IsEmpty()";
                continue;
            }

            var variable = filter.Field.Name;
            if (string.Equals(filter.FilterOperator, "Contem", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(filter.FilterOperator, "Contém", StringComparison.OrdinalIgnoreCase))
            {
                yield return $"    where {filter.Field.Name} like !\"%\" + &{variable}.Trim() + !\"%\" when not &{variable}.IsEmpty()";
            }
            else if (string.Equals(filter.FilterOperator, "ComecaCom", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(filter.FilterOperator, "Começa com", StringComparison.OrdinalIgnoreCase))
            {
                yield return $"    where {filter.Field.Name} like &{variable}.Trim() + !\"%\" when not &{variable}.IsEmpty()";
            }
            else
            {
                yield return $"    where {filter.Field.Name} = &{variable} when not &{variable}.IsEmpty()";
            }
        }
    }

    private static IReadOnlyList<ApiPlanStaticOrder> ResolveDeterministicOrder(ApiPlan plan)
    {
        var ordered = plan.StaticOrder.OrderBy(item => item.Order).ToList();
        foreach (var key in plan.PrimaryKey.OrderBy(item => item.Order))
        {
            if (ordered.All(item => !string.Equals(item.AttributeName, key.Name, StringComparison.OrdinalIgnoreCase)))
            {
                ordered.Add(new ApiPlanStaticOrder(ordered.Count + 1, key.Name, "ASC"));
            }
        }

        return ordered;
    }

    private static string FormatOrderPart(ApiPlanStaticOrder order)
    {
        return string.Equals(order.Direction, "DESC", StringComparison.OrdinalIgnoreCase)
            ? "(" + order.AttributeName + ")"
            : order.AttributeName;
    }

    private static string CreateListRules(ApiPlan plan)
    {
        var parameters = new List<string> { $"in:&{PageParameterName}", $"in:&{PageSizeParameterName}" };
        parameters.AddRange(plan.ListFilters.SelectMany(FilterVariableNames).Select(name => "in:&" + name));
        parameters.AddRange(new[] { "out:&ListResponse", "out:&ErrorResponse", "out:&RestStatusCode" });
        return "parm(" + string.Join(", ", parameters) + ");";
    }

    private static string CreateLegacyListRules(ApiPlan plan)
    {
        var parameters = new List<string> { $"in:&{PageVariableName}", $"in:&{PageSizeVariableName}" };
        parameters.AddRange(plan.ListFilters.SelectMany(FilterVariableNames).Select(name => "in:&" + name));
        parameters.Add("out:&ListResponse");
        return "parm(" + string.Join(", ", parameters) + ");";
    }

    private static string CreatePreviousB070ListRules(ApiPlan plan)
    {
        var parameters = new List<string> { $"in:&{PageParameterName}", $"in:&{PageSizeParameterName}" };
        parameters.AddRange(plan.ListFilters.SelectMany(FilterVariableNames).Select(name => "in:&" + name));
        parameters.Add("out:&ListResponse");
        return "parm(" + string.Join(", ", parameters) + ");";
    }

    internal static string CreateB070ServiceGroupSource(ApiPlan plan, bool includeBusinessComponentParameters)
    {
        return CreateB070ServiceGroupSource(plan, includeBusinessComponentParameters, exposeErrorResponse: true);
    }

    internal static string CreateB070InternalErrorOnlyServiceGroupSource(ApiPlan plan, bool includeBusinessComponentParameters)
    {
        return CreateB070ServiceGroupSource(plan, includeBusinessComponentParameters, exposeErrorResponse: false);
    }

    private static string CreateB070ServiceGroupSource(ApiPlan plan, bool includeBusinessComponentParameters, bool exposeErrorResponse)
    {
        var services = plan.Services.Select(service => ServiceSource(plan, service.Name, includeBusinessComponentParameters, exposeErrorResponse));
        return $"{plan.ApiName}{Environment.NewLine}{{{Environment.NewLine}{string.Join(Environment.NewLine + Environment.NewLine, services)}{Environment.NewLine}}}";
    }

    private static string ServiceSource(ApiPlan plan, string service, bool includeBusinessComponentParameters, bool exposeErrorResponse)
    {
        var procedure = ExpectedProcedureReference(plan, $"proc{plan.TransactionName}_API_{service}");
        var annotation = ServiceAnnotations(plan, service);
        if (string.Equals(service, "List", StringComparison.OrdinalIgnoreCase))
        {
            var parameters = new List<string> { $"in: &{PageVariableName}", $"in: &{PageSizeVariableName}" };
            parameters.AddRange(plan.ListFilters.SelectMany(FilterVariableNames).Select(name => "in: &" + name));
            parameters.Add("out: &ListResponse");
            if (exposeErrorResponse)
            {
                parameters.Add("out: &ErrorResponse");
            }

            var arguments = new List<string> { $"&{PageVariableName}", $"&{PageSizeVariableName}" };
            arguments.AddRange(plan.ListFilters.SelectMany(FilterVariableNames).Select(name => "&" + name));
            arguments.Add("&ListResponse");
            if (exposeErrorResponse)
            {
                arguments.AddRange(new[] { "&ErrorResponse", "&RestStatusCode" });
            }

            return annotation + $"    List({string.Join(", ", parameters)}){Environment.NewLine}        => {procedure}({string.Join(", ", arguments)});";
        }

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

        if (includeBusinessComponentParameters && string.Equals(service, "Delete", StringComparison.OrdinalIgnoreCase))
        {
            var parameters = string.Join(", ", plan.PrimaryKey.Select(field => $"in: &{field.Name}").Concat(exposeErrorResponse ? new[] { "out: &ErrorResponse" } : Array.Empty<string>()));
            var arguments = string.Join(", ", plan.PrimaryKey.Select(field => $"&{field.Name}").Concat(new[] { "&ErrorResponse", "&RestStatusCode" }));
            return annotation + $"    Delete({parameters}){Environment.NewLine}        => {procedure}({arguments});";
        }

        return annotation + $"    {service}(){Environment.NewLine}        => {procedure}();";
    }

    private static IReadOnlyList<VariableSpec> ProcedureVariableSpecs(ApiPlan plan)
    {
        var variables = new List<VariableSpec>
        {
            new(PageParameterName, "Numeric(9.0)"),
            new(PageSizeParameterName, "Numeric(9.0)"),
            new(PageVariableName, "Numeric(9.0)"),
            new(PageSizeVariableName, "Numeric(9.0)"),
            new("ListResponse", plan.ListResponseSdtName),
            new("ErrorResponse", "sdt_API_ErrorResponse"),
            new("RestStatusCode", "Numeric(3.0)"),
            new("AppliedFilters", plan.ListFiltersSdtName),
            new("Item", ResolveListItemSdtName(plan)),
            new("FirstRecord", "Numeric(18.0)"),
            new("LastRecord", "Numeric(18.0)"),
            new("TotalCount", "Numeric(18.0)"),
        };
        variables.AddRange(plan.ListFilters.SelectMany(FilterVariableSpecs));
        return CoalesceVariableSpecs(variables);
    }

    private static IReadOnlyList<VariableSpec> PreviousConditionalB077ProcedureVariableSpecs(ApiPlan plan)
    {
        var variables = new List<VariableSpec>
        {
            new(PageParameterName, "Numeric(9.0)"),
            new(PageSizeParameterName, "Numeric(9.0)"),
            new(PageVariableName, "Numeric(9.0)"),
            new(PageSizeVariableName, "Numeric(9.0)"),
            new("ListResponse", plan.ListResponseSdtName),
            new("AppliedFilters", plan.ListFiltersSdtName),
            new("AppliedFiltersWereApplied", "Numeric(1.0)"),
            new("Item", plan.ResponseSdtName),
            new("FirstRecord", "Numeric(18.0)"),
            new("LastRecord", "Numeric(18.0)"),
            new("TotalCount", "Numeric(18.0)"),
        };
        variables.AddRange(plan.ListFilters.SelectMany(FilterVariableSpecs));
        return CoalesceVariableSpecs(variables);
    }

    private static IReadOnlyList<VariableSpec> InvalidB077ProcedureVariableSpecs(ApiPlan plan)
    {
        var variables = new List<VariableSpec>
        {
            new(PageParameterName, "Numeric(9.0)"),
            new(PageSizeParameterName, "Numeric(9.0)"),
            new(PageVariableName, "Numeric(9.0)"),
            new(PageSizeVariableName, "Numeric(9.0)"),
            new("ListResponse", plan.ListResponseSdtName),
            new("AppliedFilters", plan.ListFiltersSdtName),
            new("Item", plan.ResponseSdtName),
            new("FirstRecord", "Numeric(18.0)"),
            new("LastRecord", "Numeric(18.0)"),
            new("TotalCount", "Numeric(18.0)"),
        };
        variables.AddRange(plan.ListFilters.SelectMany(FilterVariableSpecs));
        return CoalesceVariableSpecs(variables);
    }

    private static IReadOnlyList<VariableSpec> PreviousB070ProcedureVariableSpecs(ApiPlan plan)
    {
        var variables = new List<VariableSpec>
        {
            new(PageParameterName, "Numeric(9.0)"),
            new(PageSizeParameterName, "Numeric(9.0)"),
            new(PageVariableName, "Numeric(9.0)"),
            new(PageSizeVariableName, "Numeric(9.0)"),
            new("ListResponse", plan.ListResponseSdtName),
            new("Item", plan.ResponseSdtName),
            new("FirstRecord", "Numeric(18.0)"),
            new("LastRecord", "Numeric(18.0)"),
            new("TotalCount", "Numeric(18.0)"),
        };
        variables.AddRange(plan.ListFilters.SelectMany(FilterVariableSpecs));
        return CoalesceVariableSpecs(variables);
    }

    private static IReadOnlyList<VariableSpec> LegacyProcedureVariableSpecs(ApiPlan plan)
    {
        var variables = new List<VariableSpec>
        {
            new(PageVariableName, "Numeric(9.0)"),
            new(PageSizeVariableName, "Numeric(9.0)"),
            new("ListResponse", plan.ListResponseSdtName),
            new("Item", plan.ResponseSdtName),
            new("FirstRecord", "Numeric(18.0)"),
            new("LastRecord", "Numeric(18.0)"),
            new("TotalCount", "Numeric(18.0)"),
        };
        variables.AddRange(plan.ListFilters.SelectMany(FilterVariableSpecs));
        return CoalesceVariableSpecs(variables);
    }

    private static IReadOnlyList<VariableSpec> ApiVariableSpecs(ApiPlan plan, bool includeBusinessComponentParameters)
    {
        var variables = new List<VariableSpec>
        {
            new(PageVariableName, "Numeric(9.0)"),
            new(PageSizeVariableName, "Numeric(9.0)"),
            new("ListResponse", plan.ListResponseSdtName),
            new("ErrorResponse", "sdt_API_ErrorResponse"),
            new("RestStatusCode", "Numeric(3.0)"),
        };
        variables.AddRange(plan.ListFilters.SelectMany(FilterVariableSpecs));
        if (includeBusinessComponentParameters)
        {
            variables.AddRange(plan.PrimaryKey.Select(field => new VariableSpec(field.Name, $"Attribute:{field.Name}")));
            variables.Add(new VariableSpec("GetResponse", plan.ResponseSdtName));
            variables.Add(new VariableSpec("CreateRequest", plan.CreateRequestSdtName, isServiceRequired: true));
            variables.Add(new VariableSpec("CreateResponse", plan.ResponseSdtName));
            variables.Add(new VariableSpec("UpdateRequest", plan.UpdateRequestSdtName, isServiceRequired: true));
            variables.Add(new VariableSpec("UpdateResponse", plan.ResponseSdtName));
            variables.Add(new VariableSpec("ErrorResponse", "sdt_API_ErrorResponse"));
            variables.Add(new VariableSpec("RestStatusCode", "Numeric(3.0)"));
        }

        return CoalesceVariableSpecs(variables);
    }

    private static IReadOnlyList<VariableSpec> PreviousB070ApiVariableSpecs(ApiPlan plan, bool includeBusinessComponentParameters)
    {
        var variables = new List<VariableSpec>
        {
            new(PageVariableName, "Numeric(9.0)"),
            new(PageSizeVariableName, "Numeric(9.0)"),
            new("ListResponse", plan.ListResponseSdtName),
        };
        variables.AddRange(plan.ListFilters.SelectMany(FilterVariableSpecs));
        if (includeBusinessComponentParameters)
        {
            variables.AddRange(plan.PrimaryKey.Select(field => new VariableSpec(field.Name, $"Attribute:{field.Name}")));
            variables.Add(new VariableSpec("GetResponse", plan.ResponseSdtName));
            variables.Add(new VariableSpec("CreateRequest", plan.CreateRequestSdtName, isServiceRequired: true));
            variables.Add(new VariableSpec("CreateResponse", plan.ResponseSdtName));
            variables.Add(new VariableSpec("UpdateRequest", plan.UpdateRequestSdtName, isServiceRequired: true));
            variables.Add(new VariableSpec("UpdateResponse", plan.ResponseSdtName));
            variables.Add(new VariableSpec("ErrorResponse", "sdt_API_ErrorResponse"));
            variables.Add(new VariableSpec("RestStatusCode", "Numeric(3.0)"));
        }

        return CoalesceVariableSpecs(variables);
    }

    private static IReadOnlyList<VariableSpec> CoalesceVariableSpecs(IEnumerable<VariableSpec> variables)
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
                throw new InvalidOperationException($"B070 bloqueado: variavel '&{variable.Name}' foi planejada com tipos divergentes: '{existing.DataType}' e '{variable.DataType}'. Nenhuma alteracao foi feita.");
            }
        }

        return resolved;
    }

    private static IEnumerable<VariableSpec> FilterVariableSpecs(ApiPlanFilter filter)
    {
        foreach (var name in FilterVariableNames(filter))
        {
            yield return new VariableSpec(name, FilterVariableDataType(filter));
        }
    }

    private static string FilterVariableDataType(ApiPlanFilter filter)
    {
        if (filter.UsesPeriod && string.Equals(filter.Field.DataType, "DateTime", StringComparison.OrdinalIgnoreCase))
        {
            return "Date";
        }

        return $"Attribute:{filter.Field.Name}";
    }

    private static IEnumerable<string> FilterVariableNames(ApiPlanFilter filter)
    {
        if (filter.UsesPeriod)
        {
            yield return FilterVariableName(filter, "From");
            yield return FilterVariableName(filter, "To");
            yield break;
        }

        if (filter.UsesRange)
        {
            yield return FilterVariableName(filter, "Min");
            yield return FilterVariableName(filter, "Max");
            yield break;
        }

        yield return filter.Field.Name;
    }

    private static string FilterVariableName(ApiPlanFilter filter, string suffix) => filter.Field.Name + suffix;

    private static void ValidateGeneratedVariableNames(IEnumerable<VariableSpec> variables)
    {
        foreach (var variable in variables)
        {
            if (IsReservedVariableName(variable.Name))
            {
                throw new InvalidOperationException($"B070 bloqueado: nome de variavel reservado ou interno nao pode ser usado por B070: '&{variable.Name}'. Ajuste o contrato antes de gravar. Nenhuma alteracao foi feita.");
            }
        }
    }

    private static bool IsReservedVariableName(string name)
    {
        var reserved = new[]
        {
            "page",
            "pageSize",
            "pgmname",
            "pgmdesc",
            "context",
            "restMethod",
            "restCode",
            "webSession",
        };

        return reserved.Any(item => string.Equals(item, name, StringComparison.OrdinalIgnoreCase)) ||
            name.StartsWith("Http", StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateVariableSpecs(KBModel model, Procedure procedure, IReadOnlyList<VariableSpec> variables)
    {
        var index = 0;
        foreach (var variable in variables)
        {
            var item = new Variable("GOABB070TypeProbe" + index, procedure.Variables);
            if (!TrySetAttributeBasedOn(model, item, variable.DataType) && !DataType.ParseInto(model, variable.DataType, item))
                throw new InvalidOperationException($"B070 bloqueado: tipo da variavel '&{variable.Name}' nao foi resolvido antes da escrita: '{variable.DataType}'. Nenhuma alteracao foi feita.");
            index++;
        }
    }

    private static void ValidateVariableSpecs(KBModel model, API api, IReadOnlyList<VariableSpec> variables)
    {
        var index = 0;
        foreach (var variable in variables)
        {
            var item = new Variable("GOABB070ApiTypeProbe" + index, api.Variables);
            if (!TrySetAttributeBasedOn(model, item, variable.DataType) && !DataType.ParseInto(model, variable.DataType, item))
                throw new InvalidOperationException($"B070 bloqueado: tipo da variavel de API '&{variable.Name}' nao foi resolvido antes da escrita: '{variable.DataType}'. Nenhuma alteracao foi feita.");
            index++;
        }
    }

    private static void SaveProcedure(KBModel model, Procedure procedure, string source, IReadOnlyList<VariableSpec> variables, string rules)
    {
        ReplaceVariables(model, procedure, variables);
        procedure.Rules.Source = rules;
        procedure.ProcedurePart.Source = source;
        procedure.Save();

        var persisted = Procedure.Get(model, procedure.Guid);
        if (!string.Equals(NormalizeForComparison(persisted.ProcedurePart.Source), NormalizeForComparison(source), StringComparison.Ordinal) ||
            !string.Equals(NormalizeForComparison(persisted.Rules.Source), NormalizeForComparison(rules), StringComparison.Ordinal) ||
            !HasExpectedVariables(model, persisted, variables))
        {
            throw new InvalidOperationException($"B070 bloqueado: a Procedure '{procedure.Name}' foi salva, mas o contrato persistido nao corresponde ao List planejado. Nenhuma outra alteracao sera feita.");
        }
    }

    private static void SaveApi(KBModel model, API api, Folder transactionFolder, ApiPlan plan, string source, IReadOnlyList<VariableSpec> variables)
    {
        api.Parent = transactionFolder;
        api.ServiceGroupSource.Source = source;
        if (variables.Any(variable => string.Equals(variable.Name, "RestStatusCode", StringComparison.OrdinalIgnoreCase)))
        {
            api.Events.Source = ApiPlanBusinessComponentWriter.CreateB079ApiEventsForPlan(plan);
        }

        ReplaceVariables(model, api, variables);
        api.Save();

        var persisted = API.Get(model, api.Guid);
        if (!IsB070ApiObject(model, plan, persisted))
        {
            throw new InvalidOperationException($"B070 bloqueado: o API Object '{api.Name}' foi salvo, mas o Service Source persistido nao corresponde ao contrato List planejado. Nenhuma outra alteracao sera feita.");
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
                throw new InvalidOperationException($"B070 bloqueado: tipo da variavel '&{variable.Name}' nao foi resolvido: '{variable.DataType}'. Nenhuma alteracao foi feita.");

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
                throw new InvalidOperationException($"B070 bloqueado: tipo da variavel de API '&{variable.Name}' nao foi resolvido: '{variable.DataType}'. Nenhuma alteracao foi feita.");

            ConfigureServiceRequired(item, variable);
            api.Variables.Variables.Add(item);
        }
    }

    private static void ConfigureServiceRequired(Variable variable, VariableSpec spec)
    {
        if (!spec.IsServiceRequired || !variable.ContainsPropertyDefinition(ApiPlanBusinessComponentWriter.ServiceRequiredPropertyId))
        {
            return;
        }

        variable.SetPropertyValue(ApiPlanBusinessComponentWriter.ServiceRequiredPropertyId, true);
    }

    private static bool HasExpectedVariables(KBModel model, Procedure procedure, IReadOnlyList<VariableSpec> variables)
    {
        var currentVariables = procedure.Variables.Variables.Where(variable => !variable.IsStandard).Select(variable => variable.Name).ToArray();
        var expectedVariables = new HashSet<string>(variables.Select(variable => variable.Name), StringComparer.OrdinalIgnoreCase);
        return currentVariables.Length == expectedVariables.Count &&
            currentVariables.All(variable => expectedVariables.Contains(variable)) &&
            variables.All(variable => MatchesVariableSpec(model, procedure, variable));
    }

    private static bool HasExpectedVariables(KBModel model, API api, IReadOnlyList<VariableSpec> variables)
    {
        var currentVariables = api.Variables.Variables.Where(variable => !variable.IsStandard).Select(variable => variable.Name).ToArray();
        var expectedVariables = new HashSet<string>(variables.Select(variable => variable.Name), StringComparer.OrdinalIgnoreCase);
        return currentVariables.Length == expectedVariables.Count &&
            currentVariables.All(variable => expectedVariables.Contains(variable)) &&
            variables.All(variable => MatchesVariableSpec(model, api, variable));
    }

    private static bool MatchesVariableSpec(KBModel model, Procedure procedure, VariableSpec variable)
    {
        var current = procedure.Variables.GetVariable(variable.Name, false);
        if (current is null) return false;
        var expected = new Variable(variable.Name, procedure.Variables);
        if (!TrySetAttributeBasedOn(model, expected, variable.DataType) && !DataType.ParseInto(model, variable.DataType, expected)) return false;
        return MatchesVariableSpec(current, expected);
    }

    private static bool MatchesVariableSpec(KBModel model, API api, VariableSpec variable)
    {
        var current = api.Variables.GetVariable(variable.Name, false);
        if (current is null) return false;
        var expected = new Variable(variable.Name, api.Variables);
        if (!TrySetAttributeBasedOn(model, expected, variable.DataType) && !DataType.ParseInto(model, variable.DataType, expected)) return false;
        return MatchesVariableSpec(current, expected);
    }

    private static bool MatchesVariableSpec(Variable current, Variable expected) =>
        current.Type == expected.Type &&
        SameKbObject(current.AttributeBasedOn, expected.AttributeBasedOn) &&
        SameKbObject(current.DomainBasedOn, expected.DomainBasedOn) &&
        Equals(current.DomainKey, expected.DomainKey) &&
        SameKbObject(current.KBObject, expected.KBObject);

    private static bool SameKbObject(KBObject? current, KBObject? expected)
    {
        if (current is null || expected is null)
        {
            return current is null && expected is null;
        }

        return current.Guid == expected.Guid && string.Equals(current.Name, expected.Name, StringComparison.OrdinalIgnoreCase);
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
        var attributeName = dataType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? dataType.Substring(prefix.Length).Trim() : dataType.Trim();
        var matches = ApiPlanScanProbe.Scan("Attribute", "list-find-attribute", () => Artech.Genexus.Common.Objects.Attribute.GetAll(model).Where(attribute => string.Equals(attribute.Name, attributeName, StringComparison.OrdinalIgnoreCase)).ToArray());
        if (matches.Length != 1)
        {
            throw new InvalidOperationException($"B070 bloqueado: atributo base da variavel '&{variableName}' nao foi reencontrado com seguranca: '{attributeName}'. Nenhuma alteracao foi feita.");
        }

        return matches[0];
    }

    private static string ExpectedProcedureReference(ApiPlan plan, string procedure)
    {
        if (string.IsNullOrWhiteSpace(plan.ModuleTarget) || string.Equals(plan.ModuleTarget, "Root Module", StringComparison.Ordinal))
        {
            return procedure;
        }

        return plan.ModuleTarget + "." + procedure;
    }

    private static string DescriptionAnnotation(ApiPlan plan, string service) => $"    [Description(\"{EscapeDescription(ResolveServiceDescription(plan, service))}\")]";

    private static string ServiceAnnotations(ApiPlan plan, string service)
    {
        var servicePlan = ResolveService(plan, service);
        var annotations = new List<string>
        {
            DescriptionAnnotation(plan, service),
        };

        if (!string.Equals(servicePlan.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
        {
            annotations.Add($"    [RestMethod({servicePlan.HttpMethod.Trim().ToUpperInvariant()})]");
        }

        annotations.Add($"    [RestPath(\"{EscapeDescription(servicePlan.RestPath.Trim())}\")]");
        annotations.Add($"    [SecurityLevel({servicePlan.ResolveSecurityLevel(plan.Security.SecurityLevel)})]");
        return string.Join(Environment.NewLine, annotations) + Environment.NewLine;
    }

    private static ApiPlanService ResolveService(ApiPlan plan, string service)
    {
        var matches = plan.Services
            .Where(item => string.Equals(item.Name, service, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (matches.Length != 1)
        {
            throw new InvalidOperationException($"B070 bloqueado: servico '{service}' nao foi reencontrado de forma unica no ApiPlan. Nenhuma alteracao foi feita.");
        }

        return matches[0];
    }

    private static string ResolveServiceDescription(ApiPlan plan, string service)
    {
        var matches = plan.ServiceDescriptions.Where(item => string.Equals(item.ServiceName, service, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException($"B070 bloqueado: descricao do servico '{service}' nao foi reencontrada de forma unica no ApiPlan. Nenhuma alteracao foi feita.");
        var description = matches[0].Description?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(description) || string.Equals(description, ApiPlan.UnresolvedB056ServiceDescription, StringComparison.Ordinal))
            throw new InvalidOperationException($"B070 bloqueado: descricao do servico '{service}' nao esta resolvida no ApiPlan. Nenhuma alteracao foi feita.");
        if (description.IndexOfAny(new[] { '\r', '\n' }) >= 0)
            throw new InvalidOperationException($"B070 bloqueado: descricao do servico '{service}' contem quebra de linha. Nenhuma alteracao foi feita.");
        return description;
    }

    private static string EscapeDescription(string description) => description.Replace("\\", "\\\\").Replace("\"", "\\\"");
    private static string NormalizeForComparison(string? value) => (value ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n").Trim();

    private static bool HasService(ApiPlan plan, string name) => plan.Services.Any(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
    private static string Skeleton() => "// Genexus Open API Builder: Procedure skeleton for List.";

    private static string PreviousGeneratedSkeleton() => "// Genexus Open API Builder: Procedure skeleton for List." + Environment.NewLine + "msg(!\"Genexus Open API Builder List skeleton.\", status)";

    private static string LegacySkeleton() => "// Genexus Open API Builder B050: Procedure skeleton for List. REST behavior remains pending Sprint 6." + Environment.NewLine + "msg(!\"Genexus Open API Builder B050 List skeleton. REST behavior pending Sprint 6.\", status)";
}

internal sealed class ApiPlanListProcedureWriteResult
{
    public ApiPlanListProcedureWriteResult(Guid listProcedureGuid, Guid apiObjectGuid, int filters, int orderParts, int defaultPageSize, int maximumPageSize)
    {
        ListProcedureGuid = listProcedureGuid;
        ApiObjectGuid = apiObjectGuid;
        Filters = filters;
        OrderParts = orderParts;
        DefaultPageSize = defaultPageSize;
        MaximumPageSize = maximumPageSize;
    }

    public Guid ListProcedureGuid { get; }
    public Guid ApiObjectGuid { get; }
    public int Filters { get; }
    public int OrderParts { get; }
    public int DefaultPageSize { get; }
    public int MaximumPageSize { get; }
}
