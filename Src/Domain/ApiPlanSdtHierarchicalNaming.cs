using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace GenexusOpenApiBuilder.Extension.Domain;

/// <summary>
/// B096 — nomes de SDT/membro hierárquicos, desambiguação estável e encurtamento.
/// Limite de objeto GeneXus 18: 128 caracteres (plataforma desde GX15). Confirmado
/// offline nesta fase; escrita real na KB fica para smoke posterior, antes da
/// primeira API multinível.
/// </summary>
internal static class ApiPlanSdtHierarchicalNaming
{
    public const int GeneXusObjectNameMaxLength = 128;
    public const int MaxDisambiguationAttempts = 32;
    public const string UnnamedLevelToken = "<unnamed>";
    public const string NestedCreateRequestNamePattern = "_API_CreateRequest_";
    public const string NestedUpdateRequestNamePattern = "_API_UpdateRequest_";
    public const string NestedResponseNamePattern = "_API_Response_";

    public static bool TryGetRoot(ApiPlan apiPlan, out ApiPlanLevel root)
    {
        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        if (apiPlan.Levels.Count == 0)
        {
            root = null!;
            return false;
        }

        foreach (var level in apiPlan.Levels)
        {
            if (string.Equals(level.LevelName, apiPlan.TransactionName, StringComparison.OrdinalIgnoreCase))
            {
                root = level;
                return true;
            }
        }

        root = apiPlan.Levels[0];
        return true;
    }

    public static bool HasSelectedSublevels(ApiPlan apiPlan)
    {
        return TryGetRoot(apiPlan, out var root) && root.ChildLevels.Count > 0;
    }

    public static string SanitizeLevelIdentifier(string levelName, int levelOrder)
    {
        var fallback = "Level" + levelOrder.ToString(CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(levelName) ||
            string.Equals(levelName, UnnamedLevelToken, StringComparison.OrdinalIgnoreCase))
        {
            return fallback;
        }

        var builder = new StringBuilder(levelName.Length);
        foreach (var ch in levelName)
        {
            if (char.IsLetterOrDigit(ch) || ch == '_')
            {
                builder.Append(ch);
            }
        }

        if (builder.Length == 0)
        {
            return fallback;
        }

        if (char.IsDigit(builder[0]))
        {
            builder.Insert(0, 'L');
        }

        return builder.ToString();
    }

    public static string AllocateMemberName(string preferred, int levelOrder, ISet<string> reserved)
    {
        if (string.IsNullOrWhiteSpace(preferred))
        {
            throw new ArgumentException("Preferred member name is required.", nameof(preferred));
        }

        if (reserved is null)
        {
            throw new ArgumentNullException(nameof(reserved));
        }

        if (reserved.Add(preferred))
        {
            return preferred;
        }

        var withOrder = preferred + levelOrder.ToString(CultureInfo.InvariantCulture);
        if (reserved.Add(withOrder))
        {
            return withOrder;
        }

        for (var attempt = 2; attempt <= MaxDisambiguationAttempts; attempt++)
        {
            var candidate = withOrder + "_" + attempt.ToString(CultureInfo.InvariantCulture);
            if (reserved.Add(candidate))
            {
                return candidate;
            }
        }

        throw CreateUnresolvableCollision(preferred);
    }

    public static string AllocateReplaceMemberName(string collectionMemberName, int levelOrder, ISet<string> reserved)
    {
        if (string.IsNullOrWhiteSpace(collectionMemberName))
        {
            throw new ArgumentException("Collection member name is required.", nameof(collectionMemberName));
        }

        return AllocateMemberName(collectionMemberName + "Replace", levelOrder, reserved);
    }

    public static string AllocateSdtName(
        string transactionName,
        string role,
        IReadOnlyList<string> qualifierParts,
        ISet<string> reserved)
    {
        if (string.IsNullOrWhiteSpace(transactionName))
        {
            throw new ArgumentException("Transaction name is required.", nameof(transactionName));
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Role is required.", nameof(role));
        }

        if (qualifierParts is null || qualifierParts.Count == 0)
        {
            throw new ArgumentException("Qualifier path is required for nested SDT names.", nameof(qualifierParts));
        }

        if (reserved is null)
        {
            throw new ArgumentNullException(nameof(reserved));
        }

        var qualifier = string.Join("_", qualifierParts);
        var full = "sdt" + transactionName + "_API_" + role + "_" + qualifier;
        if (full.Length <= GeneXusObjectNameMaxLength && reserved.Add(full))
        {
            return full;
        }

        var hash = ComputeStableHash8(full);
        var leaf = qualifierParts[qualifierParts.Count - 1];
        const int maxLeafToKeep = 32;
        if (leaf.Length <= maxLeafToKeep)
        {
            var byLeaf = TryAllocateFitted(
                BuildShortenedSdtName(transactionName, role, leaf),
                reserved);
            if (byLeaf is not null)
            {
                return byLeaf;
            }
        }

        var byHash = TryAllocateFitted(
            BuildShortenedSdtName(transactionName, role, hash),
            reserved);
        if (byHash is not null)
        {
            return byHash;
        }

        throw CreateUnresolvableCollision(full);
    }

    private static string? TryAllocateFitted(string candidate, ISet<string> reserved)
    {
        if (candidate.Length > GeneXusObjectNameMaxLength)
        {
            candidate = candidate.Substring(0, GeneXusObjectNameMaxLength);
        }

        if (reserved.Add(candidate))
        {
            return candidate;
        }

        for (var attempt = 2; attempt <= MaxDisambiguationAttempts; attempt++)
        {
            var suffix = "_" + attempt.ToString(CultureInfo.InvariantCulture);
            var withSuffix = candidate.Length + suffix.Length <= GeneXusObjectNameMaxLength
                ? candidate + suffix
                : candidate.Substring(0, GeneXusObjectNameMaxLength - suffix.Length) + suffix;
            if (reserved.Add(withSuffix))
            {
                return withSuffix;
            }
        }

        return null;
    }

    private static string BuildShortenedSdtName(string transactionName, string role, string tail)
    {
        const string prefix = "sdt";
        const string apiMarker = "_API_";
        var overhead = prefix.Length + apiMarker.Length + role.Length + 1 + tail.Length;
        var maxTransactionLength = GeneXusObjectNameMaxLength - overhead;
        if (maxTransactionLength < 1)
        {
            var emergency = prefix + apiMarker + role + "_" + tail;
            return emergency.Length <= GeneXusObjectNameMaxLength
                ? emergency
                : emergency.Substring(0, GeneXusObjectNameMaxLength);
        }

        var usedTransaction = transactionName.Length <= maxTransactionLength
            ? transactionName
            : transactionName.Substring(0, maxTransactionLength);
        return prefix + usedTransaction + apiMarker + role + "_" + tail;
    }

    private static string ComputeStableHash8(string value)
    {
        using (var sha = SHA256.Create())
        {
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
            return BitConverter.ToString(bytes, 0, 4).Replace("-", string.Empty);
        }
    }

    private static InvalidOperationException CreateUnresolvableCollision(string name)
    {
        return new InvalidOperationException(
            "Criacao de SDTs bloqueada: colisao de nome irresoluvel para '" + name + "'. Nenhuma alteracao foi feita.");
    }
}
