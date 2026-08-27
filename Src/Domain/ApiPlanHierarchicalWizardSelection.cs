using System;
using System.Collections.Generic;
using System.Linq;

namespace GenexusOpenApiBuilder.Extension.Domain;

/// <summary>
/// B099a — seleção hierárquica do Wizard, sem WinForms.
/// A raiz está sempre incluída; o cabeçalho continua nas listas flat do contrato.
/// Filhos entram só se marcados e o pai também. Subnível sem campo marcado e sem
/// filhos sobreviventes não é gerado.
/// </summary>
internal sealed class ApiPlanHierarchicalWizardSelection
{
    public const int ValidatedDepth = 4;

    public const string DepthWarningText =
        "Profundidade não validada: a evidência da sprint cobre até 4 níveis. A geração não é bloqueada; desmarque os níveis mais profundos se não quiser incluí-los.";

    public const string LifecycleV1WarningText =
        "Metadata, sincronização e remoção ainda são V1: não use Remover API gerada nem Sincronizar com a Transaction nesta API até B099b.";

    private readonly ApiPlanLevel _root;
    private readonly Dictionary<string, LevelNode> _nodes;
    private readonly Dictionary<string, bool> _included;
    private readonly Dictionary<string, HashSet<string>> _createFields;
    private readonly Dictionary<string, HashSet<string>> _updateFields;
    private readonly Dictionary<string, HashSet<string>> _responseFields;
    private readonly Dictionary<string, bool> _includeListCount;

    private ApiPlanHierarchicalWizardSelection(ApiPlanLevel root, IReadOnlyList<LevelNode> nodes)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
        _nodes = nodes.ToDictionary(item => item.PathKey, StringComparer.Ordinal);
        _included = new Dictionary<string, bool>(StringComparer.Ordinal);
        _createFields = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        _updateFields = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        _responseFields = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        _includeListCount = new Dictionary<string, bool>(StringComparer.Ordinal);
        Options = nodes;
        RootPathKey = nodes[0].PathKey;
        MaxDepth = nodes.Max(item => item.Depth);
        foreach (var node in nodes)
        {
            if (!node.IsRoot)
            {
                _included[node.PathKey] = true;
            }

            _createFields[node.PathKey] = CreateDefaultFieldSet(node.Level, "CreateRequest");
            _updateFields[node.PathKey] = CreateDefaultFieldSet(node.Level, "UpdateRequest");
            _responseFields[node.PathKey] = CreateDefaultFieldSet(node.Level, "Response");
            if (node.CanIncludeListCount)
            {
                _includeListCount[node.PathKey] = true;
            }
        }
    }

    public string RootPathKey { get; }

    public int MaxDepth { get; }

    public bool WarnUnvalidatedDepth => MaxDepth > ValidatedDepth;

    public IReadOnlyList<LevelNode> Options { get; }

    public bool HasSublevels => Options.Count > 1;

    public static ApiPlanHierarchicalWizardSelection CreateDefault(ApiPlanLevel root)
    {
        if (root is null)
        {
            throw new ArgumentNullException(nameof(root));
        }

        return new ApiPlanHierarchicalWizardSelection(root, Flatten(root));
    }

    public LevelNode GetNode(string pathKey)
    {
        if (!_nodes.TryGetValue(pathKey, out var node))
        {
            throw new InvalidOperationException("Nível desconhecido: " + pathKey);
        }

        return node;
    }

    public bool IsLevelIncluded(string pathKey)
    {
        var node = GetNode(pathKey);
        return node.IsRoot || _included[pathKey];
    }

    public void SetLevelIncluded(string pathKey, bool included)
    {
        var node = GetNode(pathKey);
        if (node.IsRoot)
        {
            return;
        }

        if (included)
        {
            foreach (var ancestor in node.AncestorPathKeys)
            {
                if (!string.Equals(ancestor, RootPathKey, StringComparison.Ordinal))
                {
                    _included[ancestor] = true;
                }
            }

            _included[pathKey] = true;
            return;
        }

        _included[pathKey] = false;
        foreach (var descendant in Options.Where(item => item.IsDescendantOf(pathKey)))
        {
            _included[descendant.PathKey] = false;
        }
    }

    public IReadOnlyList<string> GetSelectedFields(string pathKey, string role)
    {
        return GetFieldSet(pathKey, role).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public bool IsFieldSelected(string pathKey, string role, string fieldName)
    {
        return GetFieldSet(pathKey, role).Contains(fieldName);
    }

    public void SetFieldSelected(string pathKey, string role, string fieldName, bool selected)
    {
        var set = GetFieldSet(pathKey, role);
        if (selected)
        {
            set.Add(fieldName);
            return;
        }

        set.Remove(fieldName);
    }

    public void ReplaceSelectedFields(string pathKey, string role, IEnumerable<string> fieldNames)
    {
        var set = GetFieldSet(pathKey, role);
        set.Clear();
        foreach (var name in fieldNames)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                set.Add(name);
            }
        }
    }

    public bool GetIncludeListCount(string pathKey)
    {
        return _includeListCount.TryGetValue(pathKey, out var value) && value;
    }

    public void SetIncludeListCount(string pathKey, bool include)
    {
        if (!_includeListCount.ContainsKey(pathKey))
        {
            return;
        }

        _includeListCount[pathKey] = include;
    }

    public ApiPlanLevel Prune()
    {
        return PruneLevel(_root, RootPathKey)
            ?? throw new InvalidOperationException("A poda hierárquica não pode omitir o cabeçalho.");
    }

    public bool HasSelectedSublevels()
    {
        return Prune().ChildLevels.Count > 0;
    }

    public int CountSelectedSublevels()
    {
        return CountChildren(Prune());
    }

    public string Fingerprint()
    {
        var parts = new List<string>(Options.Count * 4);
        foreach (var node in Options)
        {
            parts.Add(node.PathKey);
            parts.Add(IsLevelIncluded(node.PathKey) ? "1" : "0");
            parts.Add(string.Join(",", GetSelectedFields(node.PathKey, "CreateRequest")));
            parts.Add(string.Join(",", GetSelectedFields(node.PathKey, "UpdateRequest")));
            parts.Add(string.Join(",", GetSelectedFields(node.PathKey, "Response")));
            if (node.CanIncludeListCount)
            {
                parts.Add(GetIncludeListCount(node.PathKey) ? "1" : "0");
            }
        }

        return string.Join("|", parts);
    }

    public static string? FieldDisabledReason(ApiPlanLevelField field, string role)
    {
        if (field is null)
        {
            throw new ArgumentNullException(nameof(field));
        }

        if (ApiPlanSdtGenerationPlanBuilder.IsLevelFieldEligible(field, role))
        {
            return null;
        }

        if (string.Equals(role, "Response", StringComparison.Ordinal))
        {
            return null;
        }

        if (field.IsFormula)
        {
            return "Fórmula";
        }

        if (field.IsNoAccept)
        {
            return "NoAccept";
        }

        if (field.IsInferred)
        {
            return "Atributo inferido";
        }

        if (field.IsRedundant)
        {
            return "Atributo redundante";
        }

        if (string.Equals(role, "CreateRequest", StringComparison.Ordinal))
        {
            if (field.IsAutonumber)
            {
                return "Chave autonumerada";
            }

            if (field.IsPrimaryKey && field.IsForeignKey)
            {
                return "Chave herdada do nível pai";
            }
        }

        return "Campo tecnicamente inadequado";
    }

    private HashSet<string> GetFieldSet(string pathKey, string role)
    {
        GetNode(pathKey);
        if (string.Equals(role, "CreateRequest", StringComparison.Ordinal))
        {
            return _createFields[pathKey];
        }

        if (string.Equals(role, "UpdateRequest", StringComparison.Ordinal))
        {
            return _updateFields[pathKey];
        }

        if (string.Equals(role, "Response", StringComparison.Ordinal))
        {
            return _responseFields[pathKey];
        }

        throw new ArgumentException("Role must be CreateRequest, UpdateRequest or Response.", nameof(role));
    }

    private ApiPlanLevel? PruneLevel(ApiPlanLevel level, string pathKey)
    {
        var node = GetNode(pathKey);
        if (!node.IsRoot && !_included[pathKey])
        {
            return null;
        }

        var children = new List<ApiPlanLevel>();
        for (var index = 0; index < level.ChildLevels.Count; index++)
        {
            var child = level.ChildLevels[index];
            var childKey = node.ChildPathKeys[index];
            var prunedChild = PruneLevel(child, childKey);
            if (prunedChild is not null)
            {
                children.Add(prunedChild);
            }
        }

        IReadOnlyList<ApiPlanLevelField> fields;
        IReadOnlyCollection<string>? selectedCreate = null;
        IReadOnlyCollection<string>? selectedUpdate = null;
        IReadOnlyCollection<string>? selectedResponse = null;
        if (node.IsRoot)
        {
            fields = level.Fields;
        }
        else
        {
            selectedCreate = SnapshotSelectedNames(pathKey, "CreateRequest", level);
            selectedUpdate = SnapshotSelectedNames(pathKey, "UpdateRequest", level);
            selectedResponse = SnapshotSelectedNames(pathKey, "Response", level);

            var selectedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in selectedCreate)
            {
                selectedNames.Add(name);
            }

            foreach (var name in selectedUpdate)
            {
                selectedNames.Add(name);
            }

            foreach (var name in selectedResponse)
            {
                selectedNames.Add(name);
            }

            fields = level.Fields.Where(field => selectedNames.Contains(field.Name)).ToArray();
            if (fields.Count == 0 && children.Count > 0)
            {
                fields = level.PrimaryKey;
                var primaryKeyNames = level.PrimaryKey
                    .Select(field => field.Name)
                    .ToArray();
                selectedCreate = primaryKeyNames;
                selectedUpdate = primaryKeyNames;
                selectedResponse = primaryKeyNames;
            }

            if (fields.Count == 0 && children.Count == 0)
            {
                return null;
            }
        }

        var includeListCount = node.CanIncludeListCount
            ? GetIncludeListCount(pathKey)
            : level.IncludeListCount;
        return new ApiPlanLevel(
            level.LevelName,
            level.Depth,
            level.ParentLevelName,
            level.LevelOrder,
            level.PrimaryKey,
            fields,
            children,
            includeListCount,
            selectedCreate,
            selectedUpdate,
            selectedResponse);
    }

    private IReadOnlyCollection<string> SnapshotSelectedNames(string pathKey, string role, ApiPlanLevel level)
    {
        var selected = GetFieldSet(pathKey, role);
        var names = new List<string>();
        foreach (var field in level.Fields)
        {
            if (selected.Contains(field.Name))
            {
                names.Add(field.Name);
            }
        }

        return names;
    }

    private static int CountChildren(ApiPlanLevel level)
    {
        var count = level.ChildLevels.Count;
        foreach (var child in level.ChildLevels)
        {
            count += CountChildren(child);
        }

        return count;
    }

    private static HashSet<string> CreateDefaultFieldSet(ApiPlanLevel level, string role)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in level.Fields)
        {
            if (ApiPlanSdtGenerationPlanBuilder.IsLevelFieldEligible(field, role))
            {
                set.Add(field.Name);
            }
        }

        return set;
    }

    private static IReadOnlyList<LevelNode> Flatten(ApiPlanLevel root)
    {
        var nodes = new List<LevelNode>();
        Append(root, parentPathKey: string.Empty, parentDisplayParts: Array.Empty<string>(), ancestorPathKeys: Array.Empty<string>(), nodes);
        return nodes;
    }

    private static void Append(
        ApiPlanLevel level,
        string parentPathKey,
        IReadOnlyList<string> parentDisplayParts,
        IReadOnlyList<string> ancestorPathKeys,
        List<LevelNode> nodes)
    {
        var pathKey = string.IsNullOrEmpty(parentPathKey)
            ? FormatSegment(level)
            : parentPathKey + "/" + FormatSegment(level);
        var displayName = string.IsNullOrWhiteSpace(level.LevelName) ? "<unnamed>" : level.LevelName;
        IReadOnlyList<string> displayParts;
        if (level.Depth <= 1)
        {
            displayParts = new[] { displayName };
        }
        else
        {
            var parts = new string[parentDisplayParts.Count + 1];
            for (var index = 0; index < parentDisplayParts.Count; index++)
            {
                parts[index] = parentDisplayParts[index];
            }

            parts[parentDisplayParts.Count] = displayName;
            displayParts = parts;
        }

        var childPathKeys = new string[level.ChildLevels.Count];
        var node = new LevelNode(
            pathKey,
            level,
            ancestorPathKeys,
            childPathKeys,
            FormatDisplayPath(displayParts, level.Depth));
        nodes.Add(node);
        var childAncestors = new string[ancestorPathKeys.Count + 1];
        for (var index = 0; index < ancestorPathKeys.Count; index++)
        {
            childAncestors[index] = ancestorPathKeys[index];
        }

        childAncestors[ancestorPathKeys.Count] = pathKey;
        var childDisplayParts = level.Depth <= 1 ? Array.Empty<string>() : displayParts;
        for (var index = 0; index < level.ChildLevels.Count; index++)
        {
            var before = nodes.Count;
            Append(level.ChildLevels[index], pathKey, childDisplayParts, childAncestors, nodes);
            childPathKeys[index] = nodes[before].PathKey;
        }
    }

    private static string FormatSegment(ApiPlanLevel level)
    {
        var name = string.IsNullOrWhiteSpace(level.LevelName) ? "<unnamed>" : level.LevelName;
        return level.Depth.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + ":"
            + level.LevelOrder.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + ":"
            + name;
    }

    private static string FormatDisplayPath(IReadOnlyList<string> parts, int depth)
    {
        if (depth <= 1)
        {
            return parts[0];
        }

        return string.Join(" / ", parts);
    }

    internal sealed class LevelNode
    {
        public LevelNode(
            string pathKey,
            ApiPlanLevel level,
            IReadOnlyList<string> ancestorPathKeys,
            IReadOnlyList<string> childPathKeys,
            string displayPath)
        {
            PathKey = pathKey;
            Level = level;
            AncestorPathKeys = ancestorPathKeys;
            ChildPathKeys = childPathKeys;
            DisplayPath = displayPath;
        }

        public string PathKey { get; }

        public ApiPlanLevel Level { get; }

        public IReadOnlyList<string> AncestorPathKeys { get; }

        public IReadOnlyList<string> ChildPathKeys { get; }

        public string DisplayPath { get; }

        public int Depth => Level.Depth;

        public bool IsRoot => Depth <= 1;

        public bool CanIncludeListCount => Depth == 2;

        public string HeaderLabel => IsRoot ? "Cabeçalho (" + DisplayPath + ")" : DisplayPath;

        public bool IsDescendantOf(string ancestorPathKey)
        {
            return AncestorPathKeys.Any(item => string.Equals(item, ancestorPathKey, StringComparison.Ordinal));
        }

        public override string ToString()
        {
            return HeaderLabel;
        }
    }
}
