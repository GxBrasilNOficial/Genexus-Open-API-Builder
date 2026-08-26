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
/// Leitor à parte do caminho flat do Wizard (<see cref="PrototypeWizardContractReader"/>).
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
        var rootLevel = ReadLevel(root, depth: 1, parentLevelName: string.Empty, levelOrder: 1, noAcceptNames);
        return new TransactionStructureSnapshot(transaction.Name, rootLevel);
    }

    /// <summary>
    /// Monta o snapshot a partir de uma árvore já normalizada (fixtures offline).
    /// </summary>
    public static TransactionStructureSnapshot FromRootLevel(string transactionName, ApiPlanLevel rootLevel)
    {
        if (string.IsNullOrWhiteSpace(transactionName))
        {
            throw new ArgumentException("Transaction name is required.", nameof(transactionName));
        }

        if (rootLevel is null)
        {
            throw new ArgumentNullException(nameof(rootLevel));
        }

        if (rootLevel.Depth != 1)
        {
            throw new ArgumentException("Root level Depth must be 1.", nameof(rootLevel));
        }

        if (!string.IsNullOrEmpty(rootLevel.ParentLevelName))
        {
            throw new ArgumentException("Root ParentLevelName must be empty.", nameof(rootLevel));
        }

        return new TransactionStructureSnapshot(transactionName, rootLevel);
    }

    public static IReadOnlyList<TransactionStructureFixture> CreateFixtures()
    {
        return new[]
        {
            CreateOneSublevelFixture(),
            CreateParallelSublevelsFixture(),
            CreateThreeDeepFixture(),
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

    private static ApiPlanLevel ReadLevel(
        TransactionLevel level,
        int depth,
        string parentLevelName,
        int levelOrder,
        ISet<string> noAcceptNames)
    {
        var levelName = string.IsNullOrWhiteSpace(level.Name) ? "<unnamed>" : level.Name;
        var primaryKeyNames = new HashSet<string>(
            level.PrimaryKey.Select(part => part.Name),
            StringComparer.OrdinalIgnoreCase);
        var primaryKeyPartCount = primaryKeyNames.Count;
        var fields = level.Attributes
            .Select((item, index) => CreateField(index + 1, item, primaryKeyNames, primaryKeyPartCount, noAcceptNames))
            .ToArray();
        var primaryKey = fields.Where(item => item.IsPrimaryKey).OrderBy(item => item.Order).ToArray();
        var childLevels = level.Levels
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
        TransactionAttribute item,
        ISet<string> primaryKeyNames,
        int primaryKeyPartCount,
        ISet<string> noAcceptNames)
    {
        var attribute = item.Attribute;
        var name = item.Name;
        var isPrimaryKey = primaryKeyNames.Contains(name);
        var isFormula = attribute?.Formula is not null;
        var isNoAccept = noAcceptNames.Contains(name);
        var isAutonumber = isPrimaryKey && IsAutonumber(item, primaryKeyPartCount);

        return new ApiPlanLevelField(
            order,
            (attribute?.Guid ?? item.Guid).ToString(),
            name,
            attribute?.Type.ToString() ?? string.Empty,
            attribute?.Length ?? 0,
            attribute?.Decimals ?? 0,
            isPrimaryKey,
            IsNullable(item.IsNullable),
            item.IsInferred,
            item.IsRedundant,
            item.IsForeignKey,
            isFormula,
            isNoAccept,
            isAutonumber);
    }

    private static bool IsAutonumber(TransactionAttribute item, int primaryKeyPartCount)
    {
        try
        {
            if (item?.Attribute == null)
            {
                return true;
            }

            if (primaryKeyPartCount > 1)
            {
                return false;
            }

            var value = item.Attribute.GetPropertyValueString("Autonumber")
                ?? item.Attribute.GetPropertyValueString("idAUTONUMBER");
            if (string.Equals(value, "False", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "0", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }
        catch
        {
            return true;
        }
    }

    private static bool IsNullable(object value)
    {
        var text = value?.ToString() ?? string.Empty;
        return string.Equals(text, "True", StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, "Yes", StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, "Nullable", StringComparison.OrdinalIgnoreCase);
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
        // Cabeçalho + um subnível; PK de linha informada; fórmula e NoAccept na linha.
        var headerId = Field(1, "a1000001-0001-4000-8000-000000000001", "OrderId", "Numeric", 8, 0, true, false, false, false, false, false, false, true);
        var headerDesc = Field(2, "a1000001-0001-4000-8000-000000000002", "OrderDesc", "VarChar", 40, 0, false, true, false, false, false, false, false, false);
        var lineId = Field(1, "a1000001-0002-4000-8000-000000000001", "LineId", "Numeric", 4, 0, true, false, false, false, false, false, false, false);
        var lineQty = Field(2, "a1000001-0002-4000-8000-000000000002", "LineQty", "Numeric", 8, 2, false, false, false, false, false, false, false, false);
        var lineTotal = Field(3, "a1000001-0002-4000-8000-000000000003", "LineTotal", "Numeric", 12, 2, false, false, false, false, false, true, false, false);
        var lineStamp = Field(4, "a1000001-0002-4000-8000-000000000004", "LineStamp", "DateTime", 0, 0, false, false, false, false, false, false, true, false);

        var lines = new ApiPlanLevel(
            "Lines",
            2,
            "Order",
            1,
            new[] { lineId },
            new[] { lineId, lineQty, lineTotal, lineStamp },
            Array.Empty<ApiPlanLevel>());

        var root = new ApiPlanLevel(
            "Order",
            1,
            string.Empty,
            1,
            new[] { headerId },
            new[] { headerId, headerDesc },
            new[] { lines });

        return new TransactionStructureFixture("OneSublevel", FromRootLevel("Order", root));
    }

    private static TransactionStructureFixture CreateParallelSublevelsFixture()
    {
        // Dois subníveis irmãos; NoAccept em um campo de Tags.
        var docId = Field(1, "a2000001-0001-4000-8000-000000000001", "DocId", "Numeric", 8, 0, true, false, false, false, false, false, false, true);
        var noteId = Field(1, "a2000001-0002-4000-8000-000000000001", "NoteId", "Numeric", 4, 0, true, false, false, false, false, false, false, false);
        var noteText = Field(2, "a2000001-0002-4000-8000-000000000002", "NoteText", "VarChar", 60, 0, false, true, false, false, false, false, false, false);
        var tagId = Field(1, "a2000001-0003-4000-8000-000000000001", "TagId", "Numeric", 4, 0, true, false, false, false, false, false, false, false);
        var tagCode = Field(2, "a2000001-0003-4000-8000-000000000002", "TagCode", "VarChar", 20, 0, false, false, false, false, false, false, true, false);

        var notes = new ApiPlanLevel(
            "Notes",
            2,
            "Document",
            1,
            new[] { noteId },
            new[] { noteId, noteText },
            Array.Empty<ApiPlanLevel>());

        var tags = new ApiPlanLevel(
            "Tags",
            2,
            "Document",
            2,
            new[] { tagId },
            new[] { tagId, tagCode },
            Array.Empty<ApiPlanLevel>());

        var root = new ApiPlanLevel(
            "Document",
            1,
            string.Empty,
            1,
            new[] { docId },
            new[] { docId },
            new[] { notes, tags });

        return new TransactionStructureFixture("ParallelSublevels", FromRootLevel("Document", root));
    }

    private static TransactionStructureFixture CreateThreeDeepFixture()
    {
        // Três níveis; fórmula no nível 3; PK autonumerada no nível 3 (PK simples do nível).
        var dayId = Field(1, "a3000001-0001-4000-8000-000000000001", "DayId", "Numeric", 8, 0, true, false, false, false, false, false, false, true);
        var shiftId = Field(1, "a3000001-0002-4000-8000-000000000001", "ShiftId", "Numeric", 4, 0, true, false, false, false, false, false, false, false);
        var shiftName = Field(2, "a3000001-0002-4000-8000-000000000002", "ShiftName", "VarChar", 40, 0, false, false, false, false, false, false, false, false);
        var workerId = Field(1, "a3000001-0003-4000-8000-000000000001", "WorkerId", "Numeric", 8, 0, true, false, false, false, false, false, false, true);
        var workerName = Field(2, "a3000001-0003-4000-8000-000000000002", "WorkerName", "VarChar", 60, 0, false, false, false, false, false, false, false, false);
        var workerScore = Field(3, "a3000001-0003-4000-8000-000000000003", "WorkerScore", "Numeric", 8, 2, false, false, false, false, false, true, false, false);

        var workers = new ApiPlanLevel(
            "Worker",
            3,
            "Shift",
            1,
            new[] { workerId },
            new[] { workerId, workerName, workerScore },
            Array.Empty<ApiPlanLevel>());

        var shifts = new ApiPlanLevel(
            "Shift",
            2,
            "Day",
            1,
            new[] { shiftId },
            new[] { shiftId, shiftName },
            new[] { workers });

        var root = new ApiPlanLevel(
            "Day",
            1,
            string.Empty,
            1,
            new[] { dayId },
            new[] { dayId },
            new[] { shifts });

        return new TransactionStructureFixture("ThreeDeep", FromRootLevel("Day", root));
    }

    private static ApiPlanLevelField Field(
        int order,
        string guid,
        string name,
        string dataType,
        int length,
        int decimals,
        bool isPrimaryKey,
        bool isNullable,
        bool isInferred,
        bool isRedundant,
        bool isForeignKey,
        bool isFormula,
        bool isNoAccept,
        bool isAutonumber)
    {
        return new ApiPlanLevelField(
            order,
            guid,
            name,
            dataType,
            length,
            decimals,
            isPrimaryKey,
            isNullable,
            isInferred,
            isRedundant,
            isForeignKey,
            isFormula,
            isNoAccept,
            isAutonumber);
    }
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
