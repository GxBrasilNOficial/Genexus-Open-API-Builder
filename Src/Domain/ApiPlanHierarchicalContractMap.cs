using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GenexusOpenApiBuilder.Extension.Domain;

/// <summary>
/// B097 — mapa de contrato hierárquico alinhado ao naming B096.
/// Expõe, por papel (Create/Update/Response), o nome estrutural do nível no BC
/// e os membros SDT (coleção / Replace / tipo do item) sem divergir do plano de SDT.
/// </summary>
internal static class ApiPlanHierarchicalContractMapBuilder
{
    public static ApiPlanHierarchicalContractMap Create(ApiPlan apiPlan)
    {
        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        if (!ApiPlanSdtHierarchicalNaming.HasSelectedSublevels(apiPlan))
        {
            return ApiPlanHierarchicalContractMap.Empty;
        }

        if (!ApiPlanSdtHierarchicalNaming.TryGetRoot(apiPlan, out var root))
        {
            throw new InvalidOperationException("Mapa hierarquico bloqueado: ApiPlan sem nivel raiz. Nenhuma alteracao foi feita.");
        }

        // Mesmo reservedSdtNames e mesma ordem Create → Update → Response do B096.
        var reservedSdtNames = CreateReservedSdtNames(apiPlan);
        return new ApiPlanHierarchicalContractMap(
            BuildRoleTree(apiPlan, root, "CreateRequest", includeReplace: false, reservedSdtNames),
            BuildRoleTree(apiPlan, root, "UpdateRequest", includeReplace: true, reservedSdtNames),
            BuildRoleTree(apiPlan, root, "Response", includeReplace: false, reservedSdtNames));
    }

    private static ApiPlanHierarchicalRoleTree BuildRoleTree(
        ApiPlan apiPlan,
        ApiPlanLevel root,
        string role,
        bool includeReplace,
        ISet<string> reservedSdtNames)
    {
        IReadOnlyList<ApiPlanField> headerFields = role switch
        {
            "CreateRequest" => apiPlan.CreateRequestFields,
            "UpdateRequest" => apiPlan.UpdateRequestFields,
            "Response" => apiPlan.ResponseFields,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported hierarchical role."),
        };

        var reservedMembers = new HashSet<string>(
            headerFields.Select(field => field.Name),
            StringComparer.OrdinalIgnoreCase);
        var reservedVariableTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var children = BuildChildren(
            apiPlan,
            root.ChildLevels,
            Array.Empty<string>(),
            Array.Empty<string>(),
            role,
            includeReplace,
            reservedMembers,
            reservedSdtNames,
            reservedVariableTokens);
        return new ApiPlanHierarchicalRoleTree(role, children);
    }

    private static IReadOnlyList<ApiPlanHierarchicalNode> BuildChildren(
        ApiPlan apiPlan,
        IReadOnlyList<ApiPlanLevel> children,
        IReadOnlyList<string> ancestorQualifiers,
        IReadOnlyList<string> ancestorBcNames,
        string role,
        bool includeReplace,
        ISet<string> parentReservedMembers,
        ISet<string> reservedSdtNames,
        ISet<string> reservedVariableTokens)
    {
        if (children.Count == 0)
        {
            return Array.Empty<ApiPlanHierarchicalNode>();
        }

        var nodes = new List<ApiPlanHierarchicalNode>(children.Count);
        foreach (var child in children)
        {
            if (string.IsNullOrWhiteSpace(child.LevelName) ||
                string.Equals(child.LevelName, ApiPlanSdtHierarchicalNaming.UnnamedLevelToken, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Mapa hierarquico bloqueado: subnivel sem nome estrutural na Transaction (LevelOrder="
                    + child.LevelOrder.ToString(CultureInfo.InvariantCulture)
                    + "). Em Transaction real todo subnivel tem nome; corrija a estrutura antes do apply. Nenhuma alteracao foi feita.");
            }

            var sanitized = ApiPlanSdtHierarchicalNaming.SanitizeLevelIdentifier(child.LevelName, child.LevelOrder);
            var childQualifiers = new List<string>(ancestorQualifiers.Count + 1);
            childQualifiers.AddRange(ancestorQualifiers);
            childQualifiers.Add(sanitized);
            var childBcNames = new List<string>(ancestorBcNames.Count + 1);
            childBcNames.AddRange(ancestorBcNames);
            childBcNames.Add(child.LevelName);
            var eligible = ApiPlanSdtGenerationPlanBuilder.SelectLevelFieldsForRole(child, role).ToArray();
            var childReservedMembers = new HashSet<string>(
                eligible.Select(field => field.Name),
                StringComparer.OrdinalIgnoreCase);
            // Pós-ordem: netos reservam nomes de SDT antes do pai (igual EmitNestedSdt / B096).
            var nested = BuildChildren(
                apiPlan,
                child.ChildLevels,
                childQualifiers,
                childBcNames,
                role,
                includeReplace,
                childReservedMembers,
                reservedSdtNames,
                reservedVariableTokens);
            var itemSdtName = ApiPlanSdtHierarchicalNaming.AllocateSdtName(
                apiPlan.TransactionName,
                role,
                childQualifiers,
                reservedSdtNames);
            var collectionName = ApiPlanSdtHierarchicalNaming.AllocateMemberName(
                sanitized,
                child.LevelOrder,
                parentReservedMembers);
            var replaceName = includeReplace
                ? ApiPlanSdtHierarchicalNaming.AllocateReplaceMemberName(
                    collectionName,
                    child.LevelOrder,
                    parentReservedMembers)
                : string.Empty;
            var variableToken = AllocateVariableToken(
                ancestorQualifiers,
                sanitized,
                child.LevelOrder,
                reservedVariableTokens);
            nodes.Add(new ApiPlanHierarchicalNode(
                child,
                child.LevelName,
                collectionName,
                replaceName,
                itemSdtName,
                eligible,
                nested,
                variableToken,
                BuildBcLevelType(apiPlan.TransactionName, ancestorBcNames, child.LevelName)));
        }

        return nodes;
    }

    private static string BuildBcLevelType(
        string transactionName,
        IReadOnlyList<string> ancestorBcNames,
        string levelName)
    {
        var parts = new List<string>(ancestorBcNames.Count + 2) { transactionName };
        parts.AddRange(ancestorBcNames);
        parts.Add(levelName);
        return string.Join(".", parts);
    }

    private static string AllocateVariableToken(
        IReadOnlyList<string> ancestorQualifiers,
        string sanitizedLevel,
        int levelOrder,
        ISet<string> reservedTokens)
    {
        var candidate = BuildVariableToken(ancestorQualifiers, sanitizedLevel, levelOrder);
        if (reservedTokens.Add(candidate))
        {
            return candidate;
        }

        for (var suffix = 2; suffix < 1000; suffix++)
        {
            var disambiguated = candidate + "_V" + suffix.ToString(CultureInfo.InvariantCulture);
            if (reservedTokens.Add(disambiguated))
            {
                return disambiguated;
            }
        }

        throw new InvalidOperationException(
            "Mapa hierarquico bloqueado: esgotou desambiguacao de VariableToken para '" +
            candidate +
            "'. Nenhuma alteracao foi feita.");
    }

    private static string BuildVariableToken(
        IReadOnlyList<string> ancestorQualifiers,
        string sanitizedLevel,
        int levelOrder)
    {
        var parts = new List<string>(ancestorQualifiers.Count + 1);
        parts.AddRange(ancestorQualifiers);
        parts.Add(sanitizedLevel);
        var joined = string.Join("_", parts);
        if (joined.Length <= 48)
        {
            return joined;
        }

        return "L" + levelOrder.ToString(CultureInfo.InvariantCulture) + "_" +
            ApiPlanSdtHierarchicalNaming.SanitizeLevelIdentifier(sanitizedLevel, levelOrder);
    }

    private static HashSet<string> CreateReservedSdtNames(ApiPlan apiPlan)
    {
        var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            apiPlan.CreateRequestSdtName,
            apiPlan.UpdateRequestSdtName,
            apiPlan.ResponseSdtName,
            apiPlan.ListFiltersSdtName,
            apiPlan.ListResponseSdtName,
        };
        foreach (var sharedName in apiPlan.SharedSdtNames)
        {
            reserved.Add(sharedName);
        }

        return reserved;
    }
}

internal sealed class ApiPlanHierarchicalContractMap
{
    public static ApiPlanHierarchicalContractMap Empty { get; } = new(
        ApiPlanHierarchicalRoleTree.Empty("CreateRequest"),
        ApiPlanHierarchicalRoleTree.Empty("UpdateRequest"),
        ApiPlanHierarchicalRoleTree.Empty("Response"));

    public ApiPlanHierarchicalContractMap(
        ApiPlanHierarchicalRoleTree createRequest,
        ApiPlanHierarchicalRoleTree updateRequest,
        ApiPlanHierarchicalRoleTree response)
    {
        CreateRequest = createRequest ?? throw new ArgumentNullException(nameof(createRequest));
        UpdateRequest = updateRequest ?? throw new ArgumentNullException(nameof(updateRequest));
        Response = response ?? throw new ArgumentNullException(nameof(response));
    }

    public ApiPlanHierarchicalRoleTree CreateRequest { get; }

    public ApiPlanHierarchicalRoleTree UpdateRequest { get; }

    public ApiPlanHierarchicalRoleTree Response { get; }

    public bool HasChildren =>
        CreateRequest.Children.Count > 0 ||
        UpdateRequest.Children.Count > 0 ||
        Response.Children.Count > 0;
}

internal sealed class ApiPlanHierarchicalRoleTree
{
    public static ApiPlanHierarchicalRoleTree Empty(string role) =>
        new(role, Array.Empty<ApiPlanHierarchicalNode>());

    public ApiPlanHierarchicalRoleTree(string role, IReadOnlyList<ApiPlanHierarchicalNode> children)
    {
        Role = role ?? throw new ArgumentNullException(nameof(role));
        Children = children ?? throw new ArgumentNullException(nameof(children));
    }

    public string Role { get; }

    public IReadOnlyList<ApiPlanHierarchicalNode> Children { get; }
}

internal sealed class ApiPlanHierarchicalNode
{
    public ApiPlanHierarchicalNode(
        ApiPlanLevel level,
        string bcCollectionName,
        string collectionMemberName,
        string replaceMemberName,
        string itemSdtName,
        IReadOnlyList<ApiPlanLevelField> eligibleFields,
        IReadOnlyList<ApiPlanHierarchicalNode> children,
        string variableToken,
        string bcLevelType)
    {
        Level = level ?? throw new ArgumentNullException(nameof(level));
        BcCollectionName = bcCollectionName ?? throw new ArgumentNullException(nameof(bcCollectionName));
        CollectionMemberName = collectionMemberName ?? throw new ArgumentNullException(nameof(collectionMemberName));
        ReplaceMemberName = replaceMemberName ?? string.Empty;
        ItemSdtName = itemSdtName ?? throw new ArgumentNullException(nameof(itemSdtName));
        EligibleFields = eligibleFields ?? throw new ArgumentNullException(nameof(eligibleFields));
        Children = children ?? throw new ArgumentNullException(nameof(children));
        VariableToken = variableToken ?? throw new ArgumentNullException(nameof(variableToken));
        BcLevelType = bcLevelType ?? throw new ArgumentNullException(nameof(bcLevelType));
    }

    public ApiPlanLevel Level { get; }

    /// <summary>Nome estrutural no Business Component (LevelName da Transaction).</summary>
    public string BcCollectionName { get; }

    /// <summary>Membro coleção no SDT do papel (pode divergir do BC em colisão).</summary>
    public string CollectionMemberName { get; }

    /// <summary>Membro booleano Replace no UpdateRequest; vazio nos demais papéis.</summary>
    public string ReplaceMemberName { get; }

    public string ItemSdtName { get; }

    public IReadOnlyList<ApiPlanLevelField> EligibleFields { get; }

    public IReadOnlyList<ApiPlanHierarchicalNode> Children { get; }

    /// <summary>Sufixo estável para nomes de variáveis de loop no Source.</summary>
    public string VariableToken { get; }

    /// <summary>
    /// Tipo GeneXus do item de nível no Business Component. Filho direto:
    /// <c>Transaction.Nivel</c>. Nível aninhado: caminho completo
    /// <c>Transaction.Pai.Neto</c>, nunca <c>Transaction.Neto</c>.
    /// </summary>
    public string BcLevelType { get; }

    public bool HasAutonumberPrimaryKey =>
        Level.PrimaryKey.Any(levelField => levelField.IsAutonumber);

    public IReadOnlyList<ApiPlanLevelField> MatchKeyFields =>
        Level.PrimaryKey
            .Where(levelField => !levelField.IsAutonumber)
            .ToArray();
}
