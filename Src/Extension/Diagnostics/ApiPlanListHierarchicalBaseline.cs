using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GenexusOpenApiBuilder.Extension.Domain;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// B098 — fixtures offline do List hierárquico (<c>ListResponse_Item</c> + <c>count()</c>).
/// Reusa os planos B096 e congela Source List + tipo de <c>&amp;Item</c>.
/// </summary>
internal static class ApiPlanListHierarchicalBaseline
{
    public static IReadOnlyList<ApiPlanSdtHierarchicalPlanFixture> CreateFixtures()
    {
        var fixtures = ApiPlanSdtHierarchicalPlanBaseline.CreateFixtures()
            .Where(fixture => ApiPlanSdtHierarchicalNaming.HasSelectedSublevels(fixture.Plan) ||
                string.Equals(fixture.Name, "HeaderOnly", StringComparison.Ordinal))
            .ToList();
        fixtures.Add(CreateCountsDisabled());
        return fixtures;
    }

    public static ApiPlanListHierarchicalSnapshot Capture(ApiPlan plan)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        var contract = ApiPlanListHierarchicalContractBuilder.Create(plan);
        return new ApiPlanListHierarchicalSnapshot(
            ApiPlanListProcedureWriter.CreateCurrentListSource(plan),
            ApiPlanListProcedureWriter.ResolveListItemSdtName(plan),
            contract.ListResponseItemSdtName,
            contract.Counts.Select(count => count.MemberName + "=" + count.AggregateAttributeName).ToArray());
    }

    public static string Serialize(ApiPlanListHierarchicalSnapshot snapshot)
    {
        var builder = new StringBuilder();
        builder.AppendLine("itemSdt=" + snapshot.ItemVariableSdtName);
        builder.AppendLine("listResponseItemSdt=" + snapshot.ListResponseItemSdtName);
        builder.AppendLine("counts=" + string.Join(",", snapshot.CountAssignments));
        builder.AppendLine("---");
        builder.Append(snapshot.ListSource);
        if (!snapshot.ListSource.EndsWith("\n", StringComparison.Ordinal) &&
            !snapshot.ListSource.EndsWith("\r", StringComparison.Ordinal))
        {
            builder.AppendLine();
        }

        return builder.ToString();
    }

    public static string NormalizeForComparison(string value) =>
        ApiPlanGenerationBaseline.NormalizeForComparison(value);

    public static void AssertContractMatchesSdtPlan(ApiPlan plan)
    {
        if (!ApiPlanSdtHierarchicalNaming.HasSelectedSublevels(plan))
        {
            return;
        }

        var contract = ApiPlanListHierarchicalContractBuilder.Create(plan);
        var sdtPlan = ApiPlanSdtGenerationPlanBuilder.Create(plan);
        var item = sdtPlan.OwnSdts.Single(sdt =>
            string.Equals(sdt.Kind, "ListResponse_Item", StringComparison.Ordinal));
        if (!string.Equals(item.Name, contract.ListResponseItemSdtName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "ListResponse_Item name diverged between contract and SDT plan: '" +
                contract.ListResponseItemSdtName + "' vs '" + item.Name + "'.");
        }

        var listResponse = sdtPlan.OwnSdts.Single(sdt =>
            string.Equals(sdt.Kind, "ListResponse", StringComparison.Ordinal));
        var items = listResponse.Members.Single(member =>
            string.Equals(member.Name, "Items", StringComparison.Ordinal));
        if (!items.IsCollection ||
            !string.Equals(items.CollectionItemType, contract.ListResponseItemSdtName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "ListResponse.Items must be a collection of ListResponse_Item.");
        }

        foreach (var count in contract.Counts)
        {
            var member = item.Members.SingleOrDefault(candidate =>
                string.Equals(candidate.Name, count.MemberName, StringComparison.Ordinal));
            if (member is null ||
                member.IsCollection ||
                !string.Equals(member.DataType, "Numeric", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Count member '" + count.MemberName + "' missing or mistyped on ListResponse_Item.");
            }
        }

        if (item.Members.Any(member => member.IsCollection))
        {
            throw new InvalidOperationException("ListResponse_Item must not publish collection members.");
        }
    }

    private static ApiPlanSdtHierarchicalPlanFixture CreateCountsDisabled()
    {
        var source = TransactionStructureReader.CreateFixtures()
            .Single(fixture => string.Equals(fixture.Name, "OneSublevel", StringComparison.Ordinal));
        var root = DisableDirectListCounts(source.Snapshot.RootLevel);
        return ApiPlanSdtHierarchicalPlanBaseline.BuildFromRoot(
            "CountsDisabled",
            source.Snapshot.TransactionName,
            root);
    }

    private static ApiPlanLevel DisableDirectListCounts(ApiPlanLevel root)
    {
        var children = root.ChildLevels
            .Select(child => child.WithIncludeListCount(false))
            .ToArray();
        return new ApiPlanLevel(
            root.LevelName,
            root.Depth,
            root.ParentLevelName,
            root.LevelOrder,
            root.PrimaryKey,
            root.Fields,
            children,
            root.IncludeListCount);
    }
}

internal sealed class ApiPlanListHierarchicalSnapshot
{
    public ApiPlanListHierarchicalSnapshot(
        string listSource,
        string itemVariableSdtName,
        string listResponseItemSdtName,
        IReadOnlyList<string> countAssignments)
    {
        ListSource = listSource ?? throw new ArgumentNullException(nameof(listSource));
        ItemVariableSdtName = itemVariableSdtName ?? throw new ArgumentNullException(nameof(itemVariableSdtName));
        ListResponseItemSdtName = listResponseItemSdtName ?? throw new ArgumentNullException(nameof(listResponseItemSdtName));
        CountAssignments = countAssignments ?? throw new ArgumentNullException(nameof(countAssignments));
    }

    public string ListSource { get; }

    public string ItemVariableSdtName { get; }

    public string ListResponseItemSdtName { get; }

    public IReadOnlyList<string> CountAssignments { get; }
}
