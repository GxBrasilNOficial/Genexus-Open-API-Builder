using System;
using System.Collections.Generic;
using System.Linq;

namespace GenexusOpenApiBuilder.Extension.Domain;

/// <summary>
/// B098 — nomes do <c>ListResponse_Item</c> e contadores de subníveis diretos,
/// alinhados ao reserved/desambiguação do plano de SDT.
/// </summary>
internal static class ApiPlanListHierarchicalContractBuilder
{
    public static ApiPlanListHierarchicalContract Create(ApiPlan apiPlan)
    {
        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        if (!ApiPlanSdtHierarchicalNaming.HasSelectedSublevels(apiPlan) ||
            !ApiPlanSdtHierarchicalNaming.TryGetRoot(apiPlan, out var root))
        {
            return ApiPlanListHierarchicalContract.Empty;
        }

        var reservedSdtNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            apiPlan.CreateRequestSdtName,
            apiPlan.UpdateRequestSdtName,
            apiPlan.ResponseSdtName,
            apiPlan.ListFiltersSdtName,
            apiPlan.ListResponseSdtName,
        };
        foreach (var sharedName in apiPlan.SharedSdtNames)
        {
            reservedSdtNames.Add(sharedName);
        }

        // Reserva os mesmos nomes aninhados que o plano de SDT aloca antes do ListResponse_Item.
        ReserveNestedSdtNames(apiPlan, root, "CreateRequest", reservedSdtNames);
        ReserveNestedSdtNames(apiPlan, root, "UpdateRequest", reservedSdtNames);
        ReserveNestedSdtNames(apiPlan, root, "Response", reservedSdtNames);

        var itemSdtName = ApiPlanSdtHierarchicalNaming.AllocateListResponseItemSdtName(
            apiPlan.TransactionName,
            reservedSdtNames);

        var reservedMembers = new HashSet<string>(
            apiPlan.ResponseFields.Select(field => field.Name),
            StringComparer.OrdinalIgnoreCase);
        var counts = new List<ApiPlanListCountMember>(root.ChildLevels.Count);
        foreach (var child in root.ChildLevels)
        {
            var sanitized = ApiPlanSdtHierarchicalNaming.SanitizeLevelIdentifier(child.LevelName, child.LevelOrder);
            var collectionName = ApiPlanSdtHierarchicalNaming.AllocateMemberName(
                sanitized,
                child.LevelOrder,
                reservedMembers);
            if (!child.IncludeListCount)
            {
                continue;
            }

            var countName = ApiPlanSdtHierarchicalNaming.AllocateCountMemberName(
                collectionName,
                child.LevelOrder,
                reservedMembers);
            var aggregateAttribute = ResolveAggregateAttributeName(child);
            counts.Add(new ApiPlanListCountMember(countName, aggregateAttribute, child.LevelName, child.LevelOrder));
        }

        return new ApiPlanListHierarchicalContract(itemSdtName, counts);
    }

    private static void ReserveNestedSdtNames(
        ApiPlan apiPlan,
        ApiPlanLevel parent,
        string role,
        ISet<string> reservedSdtNames)
    {
        foreach (var child in parent.ChildLevels)
        {
            var sanitized = ApiPlanSdtHierarchicalNaming.SanitizeLevelIdentifier(child.LevelName, child.LevelOrder);
            WalkReservePostOrder(apiPlan, child, new[] { sanitized }, role, reservedSdtNames);
        }
    }

    private static void WalkReservePostOrder(
        ApiPlan apiPlan,
        ApiPlanLevel level,
        IReadOnlyList<string> qualifierParts,
        string role,
        ISet<string> reservedSdtNames)
    {
        foreach (var child in level.ChildLevels)
        {
            var sanitized = ApiPlanSdtHierarchicalNaming.SanitizeLevelIdentifier(child.LevelName, child.LevelOrder);
            var next = new List<string>(qualifierParts.Count + 1);
            next.AddRange(qualifierParts);
            next.Add(sanitized);
            WalkReservePostOrder(apiPlan, child, next, role, reservedSdtNames);
        }

        ApiPlanSdtHierarchicalNaming.AllocateSdtName(
            apiPlan.TransactionName,
            role,
            qualifierParts,
            reservedSdtNames);
    }

    internal static string ResolveAggregateAttributeName(ApiPlanLevel child)
    {
        if (child.PrimaryKey.Count > 0)
        {
            return child.PrimaryKey[0].Name;
        }

        if (child.Fields.Count > 0)
        {
            return child.Fields[0].Name;
        }

        throw new InvalidOperationException(
            "List com contadores bloqueado: subnivel '" + child.LevelName + "' sem atributo para count(). Nenhuma alteracao foi feita.");
    }
}

internal sealed class ApiPlanListHierarchicalContract
{
    public static ApiPlanListHierarchicalContract Empty { get; } =
        new ApiPlanListHierarchicalContract(string.Empty, Array.Empty<ApiPlanListCountMember>());

    public ApiPlanListHierarchicalContract(string listResponseItemSdtName, IReadOnlyList<ApiPlanListCountMember> counts)
    {
        ListResponseItemSdtName = listResponseItemSdtName ?? throw new ArgumentNullException(nameof(listResponseItemSdtName));
        Counts = counts ?? throw new ArgumentNullException(nameof(counts));
    }

    public string ListResponseItemSdtName { get; }

    public IReadOnlyList<ApiPlanListCountMember> Counts { get; }

    public bool HasListResponseItem => !string.IsNullOrEmpty(ListResponseItemSdtName);
}

internal sealed class ApiPlanListCountMember
{
    public ApiPlanListCountMember(string memberName, string aggregateAttributeName, string levelName, int levelOrder)
    {
        MemberName = memberName ?? throw new ArgumentNullException(nameof(memberName));
        AggregateAttributeName = aggregateAttributeName ?? throw new ArgumentNullException(nameof(aggregateAttributeName));
        LevelName = levelName ?? throw new ArgumentNullException(nameof(levelName));
        LevelOrder = levelOrder;
    }

    public string MemberName { get; }

    public string AggregateAttributeName { get; }

    public string LevelName { get; }

    public int LevelOrder { get; }
}
