using System;
using System.Collections.Generic;
using System.Linq;
using GenexusOpenApiBuilder.Extension.Domain;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// B096 — fixtures offline do plano de SDT hierárquico. Reusa as árvores B095
/// via <see cref="TransactionStructureReader.Build"/> e acrescenta colisão,
/// encurtamento e cabeçalho sem filhos.
/// </summary>
internal static class ApiPlanSdtHierarchicalPlanBaseline
{
    private static readonly string[] AllServices = { "List", "Get", "Create", "Update" };
    private static readonly string[] SharedSdtNames =
    {
        "sdt_API_ErrorMessage",
        "sdt_API_ErrorResponse",
        "sdt_API_Pagination",
    };

    private const string ClassificationSource = "DefaultInMemoryHardcodedB090B091Policy";
    private const string NotSensitiveReason = "Nenhuma regra explicita de sensibilidade aplicavel.";
    private const string NotAuditReason = "Nenhuma regra explicita de auditoria operacional aplicavel.";

    public static IReadOnlyList<ApiPlanSdtHierarchicalPlanFixture> CreateFixtures()
    {
        var fromReader = TransactionStructureReader.CreateFixtures()
            .Select(fixture => FromSnapshot(fixture.Name, fixture.Snapshot))
            .ToList();
        fromReader.Add(CreateMemberCollision());
        fromReader.Add(CreateLongQualifier());
        fromReader.Add(CreateHeaderOnly());
        return fromReader;
    }

    public static string Capture(ApiPlan plan)
    {
        return ApiPlanGenerationBaseline.SerializeSdtPlan(ApiPlanSdtGenerationPlanBuilder.Create(plan));
    }

    public static string NormalizeForComparison(string value)
    {
        return ApiPlanGenerationBaseline.NormalizeForComparison(value);
    }

    public static void AssertUnresolvableMemberCollisionThrows()
    {
        var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Item" };
        reserved.Add("Item1");
        for (var attempt = 2; attempt <= ApiPlanSdtHierarchicalNaming.MaxDisambiguationAttempts; attempt++)
        {
            reserved.Add("Item1_" + attempt.ToString());
        }

        try
        {
            ApiPlanSdtHierarchicalNaming.AllocateMemberName("Item", 1, reserved);
        }
        catch (InvalidOperationException exception)
            when (exception.Message.IndexOf("irresoluvel", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return;
        }

        throw new InvalidOperationException("EXPECTED_UNRESOLVABLE_COLLISION_THROW_MISSING");
    }

    public static int MeasureShortenedSdtNameLength()
    {
        var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var name = ApiPlanSdtHierarchicalNaming.AllocateSdtName(
            new string('X', 90),
            "CreateRequest",
            new[] { new string('Y', 40), new string('Z', 40) },
            reserved);
        return name.Length;
    }

    private static ApiPlanSdtHierarchicalPlanFixture FromSnapshot(
        string fixtureName,
        TransactionStructureSnapshot snapshot)
    {
        return BuildFromRoot(fixtureName, snapshot.TransactionName, snapshot.RootLevel);
    }

    private static ApiPlanSdtHierarchicalPlanFixture CreateMemberCollision()
    {
        var headerId = Attr("c1000001-0001-4000-8000-000000000001", "DocId", "Numeric", 8, 0, false, false, false, false, false, true, "True");
        var notesField = Attr("c1000001-0001-4000-8000-000000000002", "Notes", "VarChar", 40, 0, true, false, false, false, false, true, null);
        var noteId = Attr("c1000001-0002-4000-8000-000000000001", "NoteId", "Numeric", 4, 0, false, false, false, false, false, true, "False");
        var noteText = Attr("c1000001-0002-4000-8000-000000000002", "NoteText", "VarChar", 60, 0, true, false, false, false, false, true, null);
        var notes = new TransactionStructureLevelSource(
            "Notes",
            new[] { noteId, noteText },
            new[] { "NoteId" },
            Array.Empty<TransactionStructureLevelSource>());
        var root = new TransactionStructureLevelSource(
            "CollisionDoc",
            new[] { headerId, notesField },
            new[] { "DocId" },
            new[] { notes });
        var snapshot = TransactionStructureReader.Build("CollisionDoc", root);
        return BuildFromRoot("MemberCollision", snapshot.TransactionName, snapshot.RootLevel);
    }

    private static ApiPlanSdtHierarchicalPlanFixture CreateLongQualifier()
    {
        var transactionName = "LongTx";
        var levelName = "Branch" + new string('L', 100);
        var headerId = Attr("c2000001-0001-4000-8000-000000000001", "LongId", "Numeric", 8, 0, false, false, false, false, false, true, "True");
        var lineId = Attr("c2000001-0002-4000-8000-000000000001", "LongLineId", "Numeric", 4, 0, false, false, false, false, false, true, "False");
        var line = new TransactionStructureLevelSource(
            levelName,
            new[] { lineId },
            new[] { "LongLineId" },
            Array.Empty<TransactionStructureLevelSource>());
        var root = new TransactionStructureLevelSource(
            transactionName,
            new[] { headerId },
            new[] { "LongId" },
            new[] { line });
        var snapshot = TransactionStructureReader.Build(transactionName, root);
        return BuildFromRoot("LongQualifier", snapshot.TransactionName, snapshot.RootLevel);
    }

    private static ApiPlanSdtHierarchicalPlanFixture CreateHeaderOnly()
    {
        var headerId = Attr("c3000001-0001-4000-8000-000000000001", "SoloId", "Numeric", 8, 0, false, false, false, false, false, true, "True");
        var headerName = Attr("c3000001-0001-4000-8000-000000000002", "SoloName", "VarChar", 40, 0, false, false, false, false, false, true, null);
        var root = new TransactionStructureLevelSource(
            "HeaderOnly",
            new[] { headerId, headerName },
            new[] { "SoloId" },
            Array.Empty<TransactionStructureLevelSource>());
        var snapshot = TransactionStructureReader.Build("HeaderOnly", root);
        return BuildFromRoot("HeaderOnly", snapshot.TransactionName, snapshot.RootLevel);
    }

    internal static ApiPlanSdtHierarchicalPlanFixture BuildFromRoot(
        string fixtureName,
        string transactionName,
        ApiPlanLevel root)
    {
        var createFields = root.Fields
            .Where(field => ApiPlanSdtGenerationPlanBuilder.IsLevelFieldEligible(field, "CreateRequest"))
            .Select(ToPlanField)
            .ToArray();
        var updateFields = root.Fields
            .Where(field => ApiPlanSdtGenerationPlanBuilder.IsLevelFieldEligible(field, "UpdateRequest"))
            .Select(ToPlanField)
            .ToArray();
        var responseFields = root.Fields.Select(ToPlanField).ToArray();
        var primaryKey = root.PrimaryKey.Select(ToPlanField).ToArray();
        var plan = BuildPlan(
            transactionName,
            "/" + transactionName.ToLowerInvariant(),
            primaryKey,
            createFields,
            updateFields,
            responseFields,
            Array.Empty<ApiPlanFilter>(),
            Array.Empty<ApiPlanRequiredField>(),
            primaryKey.Length == 0
                ? Array.Empty<ApiPlanStaticOrder>()
                : new[] { new ApiPlanStaticOrder(1, primaryKey[0].Name, "ASC") },
            new[] { root });
        return new ApiPlanSdtHierarchicalPlanFixture(fixtureName, plan);
    }

    private static ApiPlan BuildPlan(
        string transactionName,
        string restPath,
        IReadOnlyList<ApiPlanField> primaryKey,
        IReadOnlyList<ApiPlanField> createRequestFields,
        IReadOnlyList<ApiPlanField> updateRequestFields,
        IReadOnlyList<ApiPlanField> responseFields,
        IReadOnlyList<ApiPlanFilter> listFilters,
        IReadOnlyList<ApiPlanRequiredField> requiredFields,
        IReadOnlyList<ApiPlanStaticOrder> staticOrder,
        IReadOnlyList<ApiPlanLevel> levels)
    {
        var names = ApiPlanNames.Create(transactionName, AllServices);
        var services = AllServices
            .Select(service => CreateService(service, names.ApiName, restPath, primaryKey))
            .ToArray();
        var descriptions = services
            .Select(service => new ApiPlanServiceDescription(service.Name, service.Name + " " + transactionName))
            .ToArray();
        var classification = ApiPlanFieldClassificationConfiguration.Create(
            PrototypeWizardFieldClassificationConfiguration.CreateDefaultInMemory(
                Array.Empty<string>(),
                Array.Empty<string>()));
        return new ApiPlan(
            transactionName,
            "Root Module",
            ApiPlan.GeneratorTargetDotNet,
            names.ApiName,
            names.ApiName,
            restPath,
            names.ProcedureNames,
            names.CreateRequestSdtName,
            names.UpdateRequestSdtName,
            names.ResponseSdtName,
            names.ListFiltersSdtName,
            names.ListResponseSdtName,
            SharedSdtNames,
            names.TransactionFolderName,
            false,
            "Authentication",
            ApiPlanSecurity.CreateResolved("Authentication"),
            classification,
            50,
            200,
            staticOrder,
            descriptions,
            ApiPlan.ServiceDescriptionLanguageEnglish,
            ApiPlan.ServiceDescriptionLanguageSourcePendingKbLanguageApi,
            true,
            ApiPlan.ServiceDescriptionFallbackReasonPendingKbLanguageApi,
            services.Length,
            names.MetadataFileName,
            ApiPlan.ConflictModeBlockOnCollision,
            ApiPlan.ReexecutionModeSafe,
            ApiPlan.RestArtifactTargetApiObject,
            true,
            Array.Empty<string>(),
            primaryKey,
            createRequestFields,
            updateRequestFields,
            responseFields,
            listFilters,
            requiredFields,
            services,
            new PrototypeWizardBusinessComponentSelection(transactionName, true, false, "AlreadyEnabled"),
            includeBusinessComponentErrorMessages: true,
            levels);
    }

    private static ApiPlanService CreateService(
        string serviceName,
        string apiName,
        string restPath,
        IReadOnlyList<ApiPlanField> primaryKey)
    {
        if (string.Equals(serviceName, "List", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiPlanService("List", "GET", restPath, apiName + ".List");
        }

        if (string.Equals(serviceName, "Get", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiPlanService("Get", "GET", AppendKeyPath(restPath, primaryKey), apiName + ".Get");
        }

        if (string.Equals(serviceName, "Create", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiPlanService("Create", "POST", restPath, apiName + ".Create");
        }

        if (string.Equals(serviceName, "Update", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiPlanService("Update", "PUT", AppendKeyPath(restPath, primaryKey), apiName + ".Update");
        }

        return new ApiPlanService(serviceName, string.Empty, restPath, apiName + "." + serviceName);
    }

    private static string AppendKeyPath(string restPath, IReadOnlyList<ApiPlanField> primaryKey)
    {
        if (primaryKey.Count == 0)
        {
            return restPath;
        }

        return restPath + "/" + string.Join("/", primaryKey.Select(item => "{&" + item.Name + "}"));
    }

    private static ApiPlanField ToPlanField(ApiPlanLevelField field)
    {
        return new ApiPlanField(
            field.Order,
            field.AttributeGuid,
            field.Name,
            field.DataType,
            field.Length,
            field.Decimals,
            field.IsPrimaryKey,
            field.IsNullable,
            false,
            false,
            ClassificationSource,
            NotSensitiveReason,
            ClassificationSource,
            NotAuditReason,
            field.IsFormula,
            field.IsInferred,
            field.IsRedundant,
            ApiPlanSdtGenerationPlanBuilder.IsLevelFieldEligible(field, "CreateRequest"),
            ApiPlanSdtGenerationPlanBuilder.IsLevelFieldEligible(field, "UpdateRequest"),
            !field.IsFormula);
    }

    private static TransactionStructureAttributeSource Attr(
        string guid,
        string name,
        string dataType,
        int length,
        int decimals,
        bool isNullable,
        bool isInferred,
        bool isRedundant,
        bool isForeignKey,
        bool isFormula,
        bool hasAttributeMetadata,
        string? autonumberPropertyValue)
    {
        return new TransactionStructureAttributeSource(
            name,
            guid,
            dataType,
            length,
            decimals,
            isNullable,
            isInferred,
            isRedundant,
            isForeignKey,
            isFormula,
            hasAttributeMetadata,
            autonumberPropertyValue);
    }
}

internal sealed class ApiPlanSdtHierarchicalPlanFixture
{
    public ApiPlanSdtHierarchicalPlanFixture(string name, ApiPlan plan)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
    }

    public string Name { get; }

    public ApiPlan Plan { get; }
}
