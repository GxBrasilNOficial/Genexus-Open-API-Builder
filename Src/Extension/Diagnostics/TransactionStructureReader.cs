using System;
using System.Collections.Generic;
using System.Linq;
using Artech.Genexus.Common.Objects;
using Artech.Genexus.Common.Parts;
using GenexusOpenApiBuilder.Extension.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// B095 — leitura hierárquica recursiva de <c>transaction.Structure.Root.Levels</c>.
/// O contrato flat do cabeçalho permanece em <see cref="PrototypeWizardContractReader"/>.
/// Desde B099a o Wizard consome <see cref="Read"/> para subníveis.
/// O núcleo recursivo opera sobre <see cref="TransactionStructureLevelSource"/> para ser
/// exercitado offline; o adaptador SDK só traduz <see cref="TransactionLevel"/>.
/// </summary>
internal static class TransactionStructureReader
{
    public static TransactionStructureSnapshot Read(Transaction transaction)
    {
        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        var root = transaction.Structure?.Root
            ?? throw new InvalidOperationException("Transaction.Structure.Root is required.");
        var noAcceptNames = new HashSet<string>(
            PrototypeWizardNoAcceptRuleReader.ReadAttributeNames(transaction.Rules?.Source ?? string.Empty),
            StringComparer.OrdinalIgnoreCase);
        var rootSource = MapLevel(root);
        return Build(transaction.Name, rootSource, noAcceptNames);
    }

    /// <summary>
    /// Núcleo recursivo: a mesma travessia usada após o adaptador SDK.
    /// </summary>
    public static TransactionStructureSnapshot Build(
        string transactionName,
        TransactionStructureLevelSource rootLevel,
        ISet<string>? noAcceptAttributeNames = null)
    {
        if (string.IsNullOrWhiteSpace(transactionName))
        {
            throw new ArgumentException("Transaction name is required.", nameof(transactionName));
        }

        if (rootLevel is null)
        {
            throw new ArgumentNullException(nameof(rootLevel));
        }

        var noAcceptNames = noAcceptAttributeNames
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var root = ReadLevel(rootLevel, depth: 1, parentLevelName: string.Empty, levelOrder: 1, noAcceptNames);
        return new TransactionStructureSnapshot(transactionName, root);
    }

    public static IReadOnlyList<TransactionStructureFixture> CreateFixtures()
    {
        return new[]
        {
            CreateOneSublevelFixture(),
            CreateParallelSublevelsFixture(),
            CreateThreeDeepFixture(),
            CreateInheritedPrimaryKeyFixture(),
        };
    }

    public static string SerializeSnapshot(TransactionStructureSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        var root = new JObject
        {
            ["transactionName"] = snapshot.TransactionName,
            ["maxDepth"] = snapshot.MaxDepth,
            ["levelCount"] = snapshot.FlattenLevels().Count,
            ["root"] = SerializeLevel(snapshot.RootLevel),
        };

        return root.ToString(Formatting.Indented) + "\n";
    }

    public static string NormalizeForComparison(string value)
    {
        return (value ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n");
    }

    private static TransactionStructureLevelSource MapLevel(TransactionLevel level)
    {
        var attributes = level.Attributes
            .Select(MapAttribute)
            .ToArray();
        var primaryKeyNames = level.PrimaryKey
            .Select(part => part.Name)
            .ToArray();

        // Partes de PK ausentes de Attributes (ex.: herdadas) entram no mapa para não sumirem.
        var attributeNames = new HashSet<string>(attributes.Select(item => item.Name), StringComparer.OrdinalIgnoreCase);
        var extras = new List<TransactionStructureAttributeSource>();
        foreach (var part in level.PrimaryKey)
        {
            if (!attributeNames.Contains(part.Name))
            {
                extras.Add(MapAttribute(part));
                attributeNames.Add(part.Name);
            }
        }

        if (extras.Count > 0)
        {
            attributes = attributes.Concat(extras).ToArray();
        }

        var children = level.Levels.Select(MapLevel).ToArray();
        return new TransactionStructureLevelSource(level.Name ?? string.Empty, attributes, primaryKeyNames, children);
    }

    private static TransactionStructureAttributeSource MapAttribute(TransactionAttribute item)
    {
        var attribute = item.Attribute;
        string? autonumberValue = null;
        var hasMetadata = false;
        if (attribute != null)
        {
            hasMetadata = true;
            try
            {
                autonumberValue = attribute.GetPropertyValueString("Autonumber")
                    ?? attribute.GetPropertyValueString("idAUTONUMBER");
            }
            catch
            {
                hasMetadata = false;
            }
        }

        return new TransactionStructureAttributeSource(
            item.Name,
            (attribute?.Guid ?? item.Guid).ToString(),
            attribute?.Type.ToString() ?? string.Empty,
            attribute?.Length ?? 0,
            attribute?.Decimals ?? 0,
            TransactionAttributeKeyTraits.IsNullable(item.IsNullable),
            item.IsInferred,
            item.IsRedundant,
            item.IsForeignKey,
            attribute?.Formula is not null,
            hasMetadata,
            autonumberValue);
    }

    private static ApiPlanLevel ReadLevel(
        TransactionStructureLevelSource level,
        int depth,
        string parentLevelName,
        int levelOrder,
        ISet<string> noAcceptNames)
    {
        var levelName = string.IsNullOrWhiteSpace(level.Name) ? "<unnamed>" : level.Name;
        var primaryKeyNames = new HashSet<string>(level.PrimaryKeyNames, StringComparer.OrdinalIgnoreCase);
        var primaryKeyPartCount = level.PrimaryKeyNames.Count;
        var attributesByName = new Dictionary<string, TransactionStructureAttributeSource>(StringComparer.OrdinalIgnoreCase);
        foreach (var attribute in level.Attributes)
        {
            attributesByName[attribute.Name] = attribute;
        }

        var fields = level.Attributes
            .Select((item, index) => CreateField(index + 1, item, primaryKeyNames, primaryKeyPartCount, noAcceptNames))
            .ToArray();
        var fieldsByName = fields.ToDictionary(item => item.Name, StringComparer.OrdinalIgnoreCase);

        // Ordem da PK = ordem declarada em PrimaryKeyNames (espelha level.PrimaryKey do SDK), não a ordem de Attributes.
        var primaryKey = new List<ApiPlanLevelField>();
        for (var index = 0; index < level.PrimaryKeyNames.Count; index++)
        {
            var name = level.PrimaryKeyNames[index];
            if (fieldsByName.TryGetValue(name, out var existing))
            {
                primaryKey.Add(existing);
                continue;
            }

            if (!attributesByName.TryGetValue(name, out var source))
            {
                throw new InvalidOperationException(
                    "Primary key part '" + name + "' was listed on level '" + levelName + "' but has no attribute metadata.");
            }

            primaryKey.Add(CreateField(index + 1, source, primaryKeyNames, primaryKeyPartCount, noAcceptNames));
        }

        var childLevels = level.ChildLevels
            .Select((child, index) => ReadLevel(child, depth + 1, levelName, index + 1, noAcceptNames))
            .ToArray();

        return new ApiPlanLevel(
            levelName,
            depth,
            parentLevelName,
            levelOrder,
            primaryKey,
            fields,
            childLevels);
    }

    private static ApiPlanLevelField CreateField(
        int order,
        TransactionStructureAttributeSource item,
        ISet<string> primaryKeyNames,
        int primaryKeyPartCount,
        ISet<string> noAcceptNames)
    {
        var isPrimaryKey = primaryKeyNames.Contains(item.Name);
        var isAutonumber = isPrimaryKey
            && TransactionAttributeKeyTraits.IsAutonumberCore(
                primaryKeyPartCount,
                item.HasAttributeMetadata,
                item.AutonumberPropertyValue);

        return new ApiPlanLevelField(
            order,
            item.AttributeGuid,
            item.Name,
            item.DataType,
            item.Length,
            item.Decimals,
            isPrimaryKey,
            item.IsNullable,
            item.IsInferred,
            item.IsRedundant,
            item.IsForeignKey,
            item.IsFormula,
            noAcceptNames.Contains(item.Name),
            isAutonumber);
    }

    private static JObject SerializeLevel(ApiPlanLevel level)
    {
        return new JObject
        {
            ["levelName"] = level.LevelName,
            ["depth"] = level.Depth,
            ["parentLevelName"] = level.ParentLevelName,
            ["levelOrder"] = level.LevelOrder,
            ["primaryKey"] = new JArray(level.PrimaryKey.Select(SerializeField)),
            ["fields"] = new JArray(level.Fields.Select(SerializeField)),
            ["childLevels"] = new JArray(level.ChildLevels.Select(SerializeLevel)),
        };
    }

    private static JObject SerializeField(ApiPlanLevelField field)
    {
        return new JObject
        {
            ["order"] = field.Order,
            ["attributeGuid"] = field.AttributeGuid,
            ["name"] = field.Name,
            ["dataType"] = field.DataType,
            ["length"] = field.Length,
            ["decimals"] = field.Decimals,
            ["isPrimaryKey"] = field.IsPrimaryKey,
            ["isNullable"] = field.IsNullable,
            ["isInferred"] = field.IsInferred,
            ["isRedundant"] = field.IsRedundant,
            ["isForeignKey"] = field.IsForeignKey,
            ["isFormula"] = field.IsFormula,
            ["isNoAccept"] = field.IsNoAccept,
            ["isAutonumber"] = field.IsAutonumber,
        };
    }

    private static TransactionStructureFixture CreateOneSublevelFixture()
    {
        // Cabeçalho + um subnível; PK de linha informada (Autonumber=False); fórmula; NoAccept.
        var headerId = Attr("a1000001-0001-4000-8000-000000000001", "OrderId", "Numeric", 8, 0, false, false, false, false, false, true, "True");
        var headerDesc = Attr("a1000001-0001-4000-8000-000000000002", "OrderDesc", "VarChar", 40, 0, true, false, false, false, false, true, null);
        var lineId = Attr("a1000001-0002-4000-8000-000000000001", "LineId", "Numeric", 4, 0, false, false, false, false, false, true, "False");
        var lineQty = Attr("a1000001-0002-4000-8000-000000000002", "LineQty", "Numeric", 8, 2, false, false, false, false, false, true, null);
        var lineTotal = Attr("a1000001-0002-4000-8000-000000000003", "LineTotal", "Numeric", 12, 2, false, false, false, false, true, true, null);
        var lineStamp = Attr("a1000001-0002-4000-8000-000000000004", "LineStamp", "DateTime", 0, 0, false, false, false, false, false, true, null);

        var lines = new TransactionStructureLevelSource(
            "Lines",
            new[] { lineId, lineQty, lineTotal, lineStamp },
            new[] { "LineId" },
            Array.Empty<TransactionStructureLevelSource>());

        var root = new TransactionStructureLevelSource(
            "Order",
            new[] { headerId, headerDesc },
            new[] { "OrderId" },
            new[] { lines });

        var noAccept = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "LineStamp" };
        return new TransactionStructureFixture("OneSublevel", Build("Order", root, noAccept));
    }

    private static TransactionStructureFixture CreateParallelSublevelsFixture()
    {
        var docId = Attr("a2000001-0001-4000-8000-000000000001", "DocId", "Numeric", 8, 0, false, false, false, false, false, true, "True");
        var noteId = Attr("a2000001-0002-4000-8000-000000000001", "NoteId", "Numeric", 4, 0, false, false, false, false, false, true, "False");
        var noteText = Attr("a2000001-0002-4000-8000-000000000002", "NoteText", "VarChar", 60, 0, true, false, false, false, false, true, null);
        var tagId = Attr("a2000001-0003-4000-8000-000000000001", "TagId", "Numeric", 4, 0, false, false, false, false, false, true, "False");
        var tagCode = Attr("a2000001-0003-4000-8000-000000000002", "TagCode", "VarChar", 20, 0, false, false, false, false, false, true, null);

        var notes = new TransactionStructureLevelSource(
            "Notes",
            new[] { noteId, noteText },
            new[] { "NoteId" },
            Array.Empty<TransactionStructureLevelSource>());

        var tags = new TransactionStructureLevelSource(
            "Tags",
            new[] { tagId, tagCode },
            new[] { "TagId" },
            Array.Empty<TransactionStructureLevelSource>());

        var root = new TransactionStructureLevelSource(
            "Document",
            new[] { docId },
            new[] { "DocId" },
            new[] { notes, tags });

        var noAccept = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "TagCode" };
        return new TransactionStructureFixture("ParallelSublevels", Build("Document", root, noAccept));
    }

    private static TransactionStructureFixture CreateThreeDeepFixture()
    {
        // Três níveis; WorkerId com Autonumber=True (PK simples do nível); fórmula em WorkerScore.
        var dayId = Attr("a3000001-0001-4000-8000-000000000001", "DayId", "Numeric", 8, 0, false, false, false, false, false, true, "True");
        var shiftId = Attr("a3000001-0002-4000-8000-000000000001", "ShiftId", "Numeric", 4, 0, false, false, false, false, false, true, "False");
        var shiftName = Attr("a3000001-0002-4000-8000-000000000002", "ShiftName", "VarChar", 40, 0, false, false, false, false, false, true, null);
        var workerId = Attr("a3000001-0003-4000-8000-000000000001", "WorkerId", "Numeric", 8, 0, false, false, false, false, false, true, "True");
        var workerName = Attr("a3000001-0003-4000-8000-000000000002", "WorkerName", "VarChar", 60, 0, false, false, false, false, false, true, null);
        var workerScore = Attr("a3000001-0003-4000-8000-000000000003", "WorkerScore", "Numeric", 8, 2, false, false, false, false, true, true, null);

        var workers = new TransactionStructureLevelSource(
            "Worker",
            new[] { workerId, workerName, workerScore },
            new[] { "WorkerId" },
            Array.Empty<TransactionStructureLevelSource>());

        var shifts = new TransactionStructureLevelSource(
            "Shift",
            new[] { shiftId, shiftName },
            new[] { "ShiftId" },
            new[] { workers });

        var root = new TransactionStructureLevelSource(
            "Day",
            new[] { dayId },
            new[] { "DayId" },
            new[] { shifts });

        return new TransactionStructureFixture("ThreeDeep", Build("Day", root));
    }

    private static TransactionStructureFixture CreateInheritedPrimaryKeyFixture()
    {
        // PK composta na ordem HeaderId, LineId — HeaderId herdado entra nas Attributes e em PrimaryKeyNames.
        // Autonumber deve ser false porque primaryKeyPartCount > 1.
        // Nível sem nome exercita o fallback <unnamed>.
        var headerId = Attr("a4000001-0001-4000-8000-000000000001", "HeaderId", "Numeric", 8, 0, false, false, false, false, false, true, "True");
        var inheritedHeader = Attr("a4000001-0001-4000-8000-000000000001", "HeaderId", "Numeric", 8, 0, false, false, false, true, false, true, "True");
        var lineId = Attr("a4000001-0002-4000-8000-000000000001", "LineId", "Numeric", 4, 0, false, false, false, false, false, true, "True");
        var lineText = Attr("a4000001-0002-4000-8000-000000000002", "LineText", "VarChar", 40, 0, false, false, false, false, false, true, null);

        var line = new TransactionStructureLevelSource(
            string.Empty,
            new[] { lineId, lineText, inheritedHeader },
            new[] { "HeaderId", "LineId" },
            Array.Empty<TransactionStructureLevelSource>());

        var root = new TransactionStructureLevelSource(
            "Header",
            new[] { headerId },
            new[] { "HeaderId" },
            new[] { line });

        return new TransactionStructureFixture("InheritedPrimaryKey", Build("Header", root));
    }

    /// <summary>
    /// Fixture da profundidade validada (4) no Wizard B099a. Não entra em <see cref="CreateFixtures"/>
    /// para não recapturar o ouro B095.
    /// </summary>
    public static TransactionStructureFixture CreateFourDeepFixture()
    {
        var l4Id = Attr("a5000001-0004-4000-8000-000000000001", "LeafId", "Numeric", 4, 0, false, false, false, false, false, true, "False");
        var l3Id = Attr("a5000001-0003-4000-8000-000000000001", "NodeId", "Numeric", 4, 0, false, false, false, false, false, true, "False");
        var l2Id = Attr("a5000001-0002-4000-8000-000000000001", "BranchId", "Numeric", 4, 0, false, false, false, false, false, true, "False");
        var l1Id = Attr("a5000001-0001-4000-8000-000000000001", "RootId", "Numeric", 8, 0, false, false, false, false, false, true, "True");

        var leaf = new TransactionStructureLevelSource(
            "Leaf",
            new[] { l4Id },
            new[] { "LeafId" },
            Array.Empty<TransactionStructureLevelSource>());
        var node = new TransactionStructureLevelSource(
            "Node",
            new[] { l3Id },
            new[] { "NodeId" },
            new[] { leaf });
        var branch = new TransactionStructureLevelSource(
            "Branch",
            new[] { l2Id },
            new[] { "BranchId" },
            new[] { node });
        var root = new TransactionStructureLevelSource(
            "Root",
            new[] { l1Id },
            new[] { "RootId" },
            new[] { branch });
        return new TransactionStructureFixture("FourDeep", Build("Root", root));
    }

    /// <summary>
    /// Fixture só para o aviso de profundidade B099a quando MaxDepth &gt; ValidatedDepth.
    /// Não entra em <see cref="CreateFixtures"/> para não recapturar o ouro B095.
    /// </summary>
    public static TransactionStructureFixture CreateFiveDeepFixture()
    {
        var l5Id = Attr("a5000001-0005-4000-8000-000000000001", "TipId", "Numeric", 4, 0, false, false, false, false, false, true, "False");
        var l4Id = Attr("a5000001-0004-4000-8000-000000000001", "LeafId", "Numeric", 4, 0, false, false, false, false, false, true, "False");
        var l3Id = Attr("a5000001-0003-4000-8000-000000000001", "NodeId", "Numeric", 4, 0, false, false, false, false, false, true, "False");
        var l2Id = Attr("a5000001-0002-4000-8000-000000000001", "BranchId", "Numeric", 4, 0, false, false, false, false, false, true, "False");
        var l1Id = Attr("a5000001-0001-4000-8000-000000000001", "RootId", "Numeric", 8, 0, false, false, false, false, false, true, "True");

        var tip = new TransactionStructureLevelSource(
            "Tip",
            new[] { l5Id },
            new[] { "TipId" },
            Array.Empty<TransactionStructureLevelSource>());
        var leaf = new TransactionStructureLevelSource(
            "Leaf",
            new[] { l4Id },
            new[] { "LeafId" },
            new[] { tip });
        var node = new TransactionStructureLevelSource(
            "Node",
            new[] { l3Id },
            new[] { "NodeId" },
            new[] { leaf });
        var branch = new TransactionStructureLevelSource(
            "Branch",
            new[] { l2Id },
            new[] { "BranchId" },
            new[] { node });
        var root = new TransactionStructureLevelSource(
            "Root",
            new[] { l1Id },
            new[] { "RootId" },
            new[] { branch });
        return new TransactionStructureFixture("FiveDeep", Build("Root", root));
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

/// <summary>
/// Forma neutra da árvore de níveis, independente do SDK, para o núcleo recursivo e fixtures.
/// </summary>
internal sealed class TransactionStructureLevelSource
{
    public TransactionStructureLevelSource(
        string name,
        IReadOnlyList<TransactionStructureAttributeSource> attributes,
        IReadOnlyList<string> primaryKeyNames,
        IReadOnlyList<TransactionStructureLevelSource> childLevels)
    {
        Name = name ?? string.Empty;
        Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
        PrimaryKeyNames = primaryKeyNames ?? throw new ArgumentNullException(nameof(primaryKeyNames));
        ChildLevels = childLevels ?? throw new ArgumentNullException(nameof(childLevels));
    }

    public string Name { get; }

    public IReadOnlyList<TransactionStructureAttributeSource> Attributes { get; }

    /// <summary>Ordem das partes da chave neste nível (espelha <c>TransactionLevel.PrimaryKey</c>).</summary>
    public IReadOnlyList<string> PrimaryKeyNames { get; }

    public IReadOnlyList<TransactionStructureLevelSource> ChildLevels { get; }
}

internal sealed class TransactionStructureAttributeSource
{
    public TransactionStructureAttributeSource(
        string name,
        string attributeGuid,
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
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Attribute name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(attributeGuid))
        {
            throw new ArgumentException("Attribute GUID is required.", nameof(attributeGuid));
        }

        Name = name;
        AttributeGuid = attributeGuid;
        DataType = dataType ?? string.Empty;
        Length = length;
        Decimals = decimals;
        IsNullable = isNullable;
        IsInferred = isInferred;
        IsRedundant = isRedundant;
        IsForeignKey = isForeignKey;
        IsFormula = isFormula;
        HasAttributeMetadata = hasAttributeMetadata;
        AutonumberPropertyValue = autonumberPropertyValue;
    }

    public string Name { get; }

    public string AttributeGuid { get; }

    public string DataType { get; }

    public int Length { get; }

    public int Decimals { get; }

    public bool IsNullable { get; }

    public bool IsInferred { get; }

    public bool IsRedundant { get; }

    public bool IsForeignKey { get; }

    public bool IsFormula { get; }

    public bool HasAttributeMetadata { get; }

    public string? AutonumberPropertyValue { get; }
}

internal sealed class TransactionStructureSnapshot
{
    public TransactionStructureSnapshot(string transactionName, ApiPlanLevel rootLevel)
    {
        TransactionName = transactionName ?? throw new ArgumentNullException(nameof(transactionName));
        RootLevel = rootLevel ?? throw new ArgumentNullException(nameof(rootLevel));
    }

    public string TransactionName { get; }

    public ApiPlanLevel RootLevel { get; }

    public int MaxDepth => FlattenLevels().Max(level => level.Depth);

    public IReadOnlyList<ApiPlanLevel> FlattenLevels()
    {
        var result = new List<ApiPlanLevel>();
        AppendDepthFirst(RootLevel, result);
        return result;
    }

    private static void AppendDepthFirst(ApiPlanLevel level, List<ApiPlanLevel> buffer)
    {
        buffer.Add(level);
        foreach (var child in level.ChildLevels)
        {
            AppendDepthFirst(child, buffer);
        }
    }
}

internal sealed class TransactionStructureFixture
{
    public TransactionStructureFixture(string name, TransactionStructureSnapshot snapshot)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    public string Name { get; }

    public TransactionStructureSnapshot Snapshot { get; }
}
