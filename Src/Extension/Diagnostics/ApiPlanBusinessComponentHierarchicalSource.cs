using System;
using System.Collections.Generic;
using System.Linq;
using GenexusOpenApiBuilder.Extension.Domain;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// B097 — emissão de Source GeneXus para Get/Create/Update com subníveis e
/// marcador &lt;Subnível&gt;Replace. Transação plana permanece no caminho original
/// do <see cref="ApiPlanBusinessComponentWriter"/>.
/// </summary>
internal static class ApiPlanBusinessComponentHierarchicalSource
{
    public static IEnumerable<string> EmitGetCollectionAssignments(
        ApiPlan plan,
        string bc,
        string responseVariable,
        int spaces,
        string responseItemPrefix = "Get_")
    {
        var map = ApiPlanHierarchicalContractMapBuilder.Create(plan);
        return EmitGetNodes(map.Response.Children, bc, responseVariable, spaces, responseItemPrefix);
    }

    public static IEnumerable<string> EmitCreateCollectionAssignments(
        ApiPlan plan,
        string bc,
        string requestVariable,
        int spaces)
    {
        var map = ApiPlanHierarchicalContractMapBuilder.Create(plan);
        return EmitCreateNodes(map.CreateRequest.Children, bc, requestVariable, spaces);
    }

    public static IEnumerable<string> EmitUpdateCollectionAssignments(
        ApiPlan plan,
        string bc,
        string requestVariable,
        int spaces)
    {
        var map = ApiPlanHierarchicalContractMapBuilder.Create(plan);
        return EmitUpdateNodes(map.UpdateRequest.Children, bc, requestVariable, spaces, parentIsNew: false);
    }

    public static IEnumerable<VariableSpec> CollectGetVariables(ApiPlan plan)
    {
        var map = ApiPlanHierarchicalContractMapBuilder.Create(plan);
        var variables = new List<VariableSpec>();
        CollectGetNodeVariables(map.Response.Children, variables);
        return variables;
    }

    public static IEnumerable<VariableSpec> CollectCreateVariables(ApiPlan plan)
    {
        var map = ApiPlanHierarchicalContractMapBuilder.Create(plan);
        var variables = new List<VariableSpec>();
        CollectCreateNodeVariables(map.CreateRequest.Children, variables);
        // CreateResponse reusa o mapeamento Response (prefixo Get_).
        CollectGetNodeVariables(map.Response.Children, variables);
        return DeduplicateVariables(variables);
    }

    public static IEnumerable<VariableSpec> CollectUpdateVariables(ApiPlan plan)
    {
        var map = ApiPlanHierarchicalContractMapBuilder.Create(plan);
        var variables = new List<VariableSpec>();
        CollectUpdateNodeVariables(map.UpdateRequest.Children, variables);
        CollectGetNodeVariables(map.Response.Children, variables);
        return DeduplicateVariables(variables);
    }

    private static IEnumerable<VariableSpec> DeduplicateVariables(IReadOnlyList<VariableSpec> variables)
    {
        var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var variable in variables)
        {
            if (!seen.TryGetValue(variable.Name, out var existingType))
            {
                seen[variable.Name] = variable.DataType;
                yield return variable;
                continue;
            }

            if (!string.Equals(existingType, variable.DataType, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Source BC hierarquico bloqueado: variavel '" +
                    variable.Name +
                    "' colide com tipos distintos ('" +
                    existingType +
                    "' vs '" +
                    variable.DataType +
                    "'). Nenhuma alteracao foi feita.");
            }
        }
    }

    private static IEnumerable<string> EmitGetNodes(
        IReadOnlyList<ApiPlanHierarchicalNode> nodes,
        string bcParent,
        string responseParent,
        int spaces,
        string responseItemPrefix)
    {
        var indent = new string(' ', spaces);
        foreach (var node in nodes)
        {
            var bcItem = "&Bc_" + node.VariableToken;
            var responseItem = "&" + responseItemPrefix + node.VariableToken;
            yield return $"{indent}For {bcItem} in {bcParent}.{node.BcCollectionName}";
            yield return $"{indent}    {responseItem} = new()";
            foreach (var field in node.EligibleFields)
            {
                yield return $"{indent}    {responseItem}.{field.Name} = {bcItem}.{field.Name}";
            }

            foreach (var line in EmitGetNodes(node.Children, bcItem, responseItem, spaces + 4, responseItemPrefix))
            {
                yield return line;
            }

            yield return $"{indent}    {responseParent}.{node.CollectionMemberName}.Add({responseItem})";
            yield return $"{indent}EndFor";
        }
    }

    private static IEnumerable<string> EmitCreateNodes(
        IReadOnlyList<ApiPlanHierarchicalNode> nodes,
        string bcParent,
        string requestParent,
        int spaces)
    {
        var indent = new string(' ', spaces);
        foreach (var node in nodes)
        {
            var bcItem = "&Bc_" + node.VariableToken;
            var requestItem = "&Create_" + node.VariableToken;
            yield return $"{indent}For {requestItem} in {requestParent}.{node.CollectionMemberName}";
            yield return $"{indent}    {bcItem} = new()";
            foreach (var field in node.EligibleFields)
            {
                yield return $"{indent}    {bcItem}.{field.Name} = {requestItem}.{field.Name}";
            }

            foreach (var line in EmitCreateNodes(node.Children, bcItem, requestItem, spaces + 4))
            {
                yield return line;
            }

            yield return $"{indent}    {bcParent}.{node.BcCollectionName}.Add({bcItem})";
            yield return $"{indent}EndFor";
        }
    }

    private static IEnumerable<string> EmitUpdateNodes(
        IReadOnlyList<ApiPlanHierarchicalNode> nodes,
        string bcParent,
        string requestParent,
        int spaces,
        bool parentIsNew)
    {
        foreach (var node in nodes)
        {
            if (parentIsNew)
            {
                // Pai novo: inserir filhos do request; Replace é irrelevante.
                foreach (var line in EmitCreateLikeNodes(node, bcParent, requestParent, spaces, "&Update_"))
                {
                    yield return line;
                }

                continue;
            }

            if (string.IsNullOrEmpty(node.ReplaceMemberName))
            {
                continue;
            }

            var indent = new string(' ', spaces);
            yield return $"{indent}If {requestParent}.{node.ReplaceMemberName}";
            if (node.HasAutonumberPrimaryKey || node.MatchKeyFields.Count == 0)
            {
                foreach (var line in EmitAutonumberReplace(node, bcParent, requestParent, spaces + 4))
                {
                    yield return line;
                }
            }
            else
            {
                foreach (var line in EmitMatchKeyReplace(node, bcParent, requestParent, spaces + 4))
                {
                    yield return line;
                }
            }

            yield return $"{indent}EndIf";
        }
    }

    private static IEnumerable<string> EmitAutonumberReplace(
        ApiPlanHierarchicalNode node,
        string bcParent,
        string requestParent,
        int spaces)
    {
        var indent = new string(' ', spaces);
        yield return $"{indent}{bcParent}.{node.BcCollectionName}.Clear()";
        foreach (var line in EmitCreateLikeNodes(node, bcParent, requestParent, spaces, "&Update_"))
        {
            yield return line;
        }
    }

    private static IEnumerable<string> EmitMatchKeyReplace(
        ApiPlanHierarchicalNode node,
        string bcParent,
        string requestParent,
        int spaces)
    {
        var indent = new string(' ', spaces);
        var bcItem = "&Bc_" + node.VariableToken;
        var requestItem = "&Update_" + node.VariableToken;
        var indexVar = "&Idx_" + node.VariableToken;
        var foundVar = "&Found_" + node.VariableToken;

        // Remover linhas omitidas (do fim para o início).
        yield return $"{indent}{indexVar} = {bcParent}.{node.BcCollectionName}.Count";
        yield return $"{indent}Do While {indexVar} >= 1";
        yield return $"{indent}    {bcItem} = {bcParent}.{node.BcCollectionName}.Item({indexVar})";
        yield return $"{indent}    {foundVar} = False";
        yield return $"{indent}    For {requestItem} in {requestParent}.{node.CollectionMemberName}";
        yield return $"{indent}        If {BuildPkMatchCondition(node, requestItem, bcItem)}";
        yield return $"{indent}            {foundVar} = True";
        yield return $"{indent}            Exit";
        yield return $"{indent}        EndIf";
        yield return $"{indent}    EndFor";
        yield return $"{indent}    If not {foundVar}";
        yield return $"{indent}        {bcParent}.{node.BcCollectionName}.Remove({indexVar})";
        yield return $"{indent}    EndIf";
        yield return $"{indent}    {indexVar} = {indexVar} - 1";
        yield return $"{indent}EndDo";

        // Atualizar existentes / inserir novos.
        yield return $"{indent}For {requestItem} in {requestParent}.{node.CollectionMemberName}";
        yield return $"{indent}    {foundVar} = False";
        yield return $"{indent}    For {bcItem} in {bcParent}.{node.BcCollectionName}";
        yield return $"{indent}        If {BuildPkMatchCondition(node, requestItem, bcItem)}";
        yield return $"{indent}            {foundVar} = True";
        foreach (var field in node.EligibleFields)
        {
            yield return $"{indent}            {bcItem}.{field.Name} = {requestItem}.{field.Name}";
        }

        foreach (var line in EmitUpdateNodes(node.Children, bcItem, requestItem, spaces + 12, parentIsNew: false))
        {
            yield return line;
        }

        yield return $"{indent}            Exit";
        yield return $"{indent}        EndIf";
        yield return $"{indent}    EndFor";
        yield return $"{indent}    If not {foundVar}";
        yield return $"{indent}        {bcItem} = new()";
        foreach (var field in node.EligibleFields)
        {
            yield return $"{indent}            {bcItem}.{field.Name} = {requestItem}.{field.Name}";
        }

        foreach (var line in EmitUpdateNodes(node.Children, bcItem, requestItem, spaces + 8, parentIsNew: true))
        {
            yield return line;
        }

        yield return $"{indent}        {bcParent}.{node.BcCollectionName}.Add({bcItem})";
        yield return $"{indent}    EndIf";
        yield return $"{indent}EndFor";
    }

    private static IEnumerable<string> EmitCreateLikeNodes(
        ApiPlanHierarchicalNode node,
        string bcParent,
        string requestParent,
        int spaces,
        string requestPrefix)
    {
        var indent = new string(' ', spaces);
        var bcItem = "&Bc_" + node.VariableToken;
        var requestItem = requestPrefix + node.VariableToken;
        yield return $"{indent}For {requestItem} in {requestParent}.{node.CollectionMemberName}";
        yield return $"{indent}    {bcItem} = new()";
        foreach (var field in node.EligibleFields)
        {
            yield return $"{indent}    {bcItem}.{field.Name} = {requestItem}.{field.Name}";
        }

        foreach (var child in node.Children)
        {
            foreach (var line in EmitCreateLikeNodes(child, bcItem, requestItem, spaces + 4, requestPrefix))
            {
                yield return line;
            }
        }

        yield return $"{indent}    {bcParent}.{node.BcCollectionName}.Add({bcItem})";
        yield return $"{indent}EndFor";
    }

    private static string BuildPkMatchCondition(
        ApiPlanHierarchicalNode node,
        string requestItem,
        string bcItem)
    {
        return string.Join(
            " and ",
            node.MatchKeyFields.Select(field => $"{requestItem}.{field.Name} = {bcItem}.{field.Name}"));
    }

    private static void CollectGetNodeVariables(
        IReadOnlyList<ApiPlanHierarchicalNode> nodes,
        List<VariableSpec> variables)
    {
        foreach (var node in nodes)
        {
            variables.Add(new VariableSpec("Bc_" + node.VariableToken, node.BcLevelType));
            variables.Add(new VariableSpec("Get_" + node.VariableToken, node.ItemSdtName));
            CollectGetNodeVariables(node.Children, variables);
        }
    }

    private static void CollectCreateNodeVariables(
        IReadOnlyList<ApiPlanHierarchicalNode> nodes,
        List<VariableSpec> variables)
    {
        foreach (var node in nodes)
        {
            variables.Add(new VariableSpec("Bc_" + node.VariableToken, node.BcLevelType));
            variables.Add(new VariableSpec("Create_" + node.VariableToken, node.ItemSdtName));
            CollectCreateNodeVariables(node.Children, variables);
        }
    }

    private static void CollectUpdateNodeVariables(
        IReadOnlyList<ApiPlanHierarchicalNode> nodes,
        List<VariableSpec> variables)
    {
        foreach (var node in nodes)
        {
            variables.Add(new VariableSpec("Bc_" + node.VariableToken, node.BcLevelType));
            variables.Add(new VariableSpec("Update_" + node.VariableToken, node.ItemSdtName));
            if (!node.HasAutonumberPrimaryKey && node.MatchKeyFields.Count > 0)
            {
                variables.Add(new VariableSpec("Idx_" + node.VariableToken, "Numeric(9.0)"));
                variables.Add(new VariableSpec("Found_" + node.VariableToken, "Boolean"));
            }

            CollectUpdateNodeVariables(node.Children, variables);
        }
    }
}
