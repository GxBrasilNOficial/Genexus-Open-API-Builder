using System;
using System.Collections.Generic;
using System.Linq;
using GenexusOpenApiBuilder.Extension.Domain;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// B097 — fixtures offline do Source BC hierárquico. Reusa os planos B096
/// (<see cref="ApiPlanSdtHierarchicalPlanBaseline"/>) e congela Get/Create/Update.
/// </summary>
internal static class ApiPlanBusinessComponentHierarchicalBaseline
{
    public static IReadOnlyList<ApiPlanSdtHierarchicalPlanFixture> CreateFixtures()
    {
        return ApiPlanSdtHierarchicalPlanBaseline.CreateFixtures()
            .Where(fixture => ApiPlanSdtHierarchicalNaming.HasSelectedSublevels(fixture.Plan) ||
                string.Equals(fixture.Name, "HeaderOnly", StringComparison.Ordinal))
            .ToArray();
    }

    public static ApiPlanBusinessComponentHierarchicalSnapshot Capture(ApiPlan plan)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        return new ApiPlanBusinessComponentHierarchicalSnapshot(
            ApiPlanBusinessComponentWriter.CreateCurrentGetSource(plan),
            ApiPlanBusinessComponentWriter.CreateCurrentSource(plan),
            ApiPlanBusinessComponentWriter.CreateCurrentUpdateSource(plan));
    }

    public static string NormalizeForComparison(string value) =>
        ApiPlanGenerationBaseline.NormalizeForComparison(value);

    /// <summary>
    /// Garante que os membros coleção/Replace do mapa coincidem com o plano de SDT B096.
    /// </summary>
    public static void AssertMapMatchesSdtPlan(ApiPlan plan)
    {
        if (!ApiPlanSdtHierarchicalNaming.HasSelectedSublevels(plan))
        {
            return;
        }

        var map = ApiPlanHierarchicalContractMapBuilder.Create(plan);
        var sdtPlan = ApiPlanSdtGenerationPlanBuilder.Create(plan);
        AssertRoleMatches(sdtPlan, "CreateRequest", plan.CreateRequestSdtName, map.CreateRequest, expectReplace: false);
        AssertRoleMatches(sdtPlan, "UpdateRequest", plan.UpdateRequestSdtName, map.UpdateRequest, expectReplace: true);
        AssertRoleMatches(sdtPlan, "Response", plan.ResponseSdtName, map.Response, expectReplace: false);
    }

    private static void AssertRoleMatches(
        ApiPlanSdtGenerationPlan sdtPlan,
        string role,
        string headerSdtName,
        ApiPlanHierarchicalRoleTree tree,
        bool expectReplace)
    {
        var header = sdtPlan.OwnSdts.Single(sdt =>
            string.Equals(sdt.Name, headerSdtName, StringComparison.Ordinal) &&
            string.Equals(sdt.Kind, role, StringComparison.Ordinal));
        AssertNodesMatch(sdtPlan, header, tree.Children, expectReplace, role);
    }

    private static void AssertNodesMatch(
        ApiPlanSdtGenerationPlan sdtPlan,
        ApiPlanSdtDefinition parentSdt,
        IReadOnlyList<ApiPlanHierarchicalNode> nodes,
        bool expectReplace,
        string role)
    {
        foreach (var node in nodes)
        {
            var collection = parentSdt.Members.SingleOrDefault(member =>
                member.IsCollection &&
                string.Equals(member.Name, node.CollectionMemberName, StringComparison.Ordinal));
            if (collection is null)
            {
                throw new InvalidOperationException(
                    "MAP_SDT_MISMATCH: colecao '" + node.CollectionMemberName + "' ausente em " + parentSdt.Name + " (" + role + ").");
            }

            if (!string.Equals(collection.CollectionItemType, node.ItemSdtName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "MAP_SDT_MISMATCH: tipo de item '" + node.ItemSdtName + "' != '" + collection.CollectionItemType +
                    "' em " + parentSdt.Name + "." + node.CollectionMemberName);
            }

            if (expectReplace)
            {
                var replace = parentSdt.Members.SingleOrDefault(member =>
                    !member.IsCollection &&
                    string.Equals(member.Name, node.ReplaceMemberName, StringComparison.Ordinal) &&
                    string.Equals(member.DataType, "Boolean", StringComparison.OrdinalIgnoreCase));
                if (replace is null)
                {
                    throw new InvalidOperationException(
                        "MAP_SDT_MISMATCH: Replace '" + node.ReplaceMemberName + "' ausente em " + parentSdt.Name + ".");
                }
            }

            var itemSdt = sdtPlan.OwnSdts.SingleOrDefault(sdt =>
                string.Equals(sdt.Name, node.ItemSdtName, StringComparison.Ordinal) &&
                string.Equals(sdt.Kind, role, StringComparison.Ordinal));
            if (itemSdt is null)
            {
                throw new InvalidOperationException(
                    "MAP_SDT_MISMATCH: SDT de item '" + node.ItemSdtName + "' ausente no plano (" + role + ").");
            }

            AssertNodesMatch(sdtPlan, itemSdt, node.Children, expectReplace, role);
        }
    }
}

internal sealed class ApiPlanBusinessComponentHierarchicalSnapshot
{
    public ApiPlanBusinessComponentHierarchicalSnapshot(string getSource, string createSource, string updateSource)
    {
        GetSource = getSource ?? throw new ArgumentNullException(nameof(getSource));
        CreateSource = createSource ?? throw new ArgumentNullException(nameof(createSource));
        UpdateSource = updateSource ?? throw new ArgumentNullException(nameof(updateSource));
    }

    public string GetSource { get; }

    public string CreateSource { get; }

    public string UpdateSource { get; }

    public IReadOnlyDictionary<string, string> ToFileMap()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Get.source.txt"] = GetSource,
            ["Create.source.txt"] = CreateSource,
            ["Update.source.txt"] = UpdateSource,
        };
    }
}
