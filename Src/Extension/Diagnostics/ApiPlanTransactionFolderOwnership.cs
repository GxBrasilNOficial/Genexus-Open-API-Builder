#nullable enable

using System;
using Newtonsoft.Json.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// B123: posse histórica do Folder da Transaction. <c>wasCreated</c> descreve
/// a operação corrente; <c>ownedByThisApi</c> autoriza a fila de remoção.
/// Description, nome e Folder vazio não entram nesta decisão.
/// </summary>
internal static class ApiPlanTransactionFolderOwnership
{
    /// <summary>
    /// Decide a posse a gravar nesta execução. Criação nesta run autoriza.
    /// Regravação só preserva posse da metadata anterior quando o Folder vivo
    /// é o mesmo objeto (GUID contínuo); homônimo com GUID novo não herda
    /// <c>ownedByThisApi</c> — evita transferir exclusão a Folder de terceiro.
    /// Metadata legada sem <c>guid</c> (V1–V3) continua adotável nesta regravação.
    /// </summary>
    internal static bool ResolveOwnedByThisApi(
        bool createdThisRun,
        JObject? previousMetadata,
        Guid? liveFolderGuid)
    {
        if (createdThisRun)
        {
            return true;
        }

        if (!ReadOwnedByThisApi(previousMetadata))
        {
            return false;
        }

        var previousGuid = TryReadFolderGuid(previousMetadata);
        if (!previousGuid.HasValue || previousGuid.Value == Guid.Empty)
        {
            return true;
        }

        return MatchesPersistedGuid(previousGuid, liveFolderGuid);
    }

    /// <summary>
    /// Lê a posse histórica. Metadata V4 com o campo usa só ele. Legado V1–V3
    /// sem o campo adota <c>wasCreated=true</c> como última evidência.
    /// </summary>
    internal static bool ReadOwnedByThisApi(JObject? metadata)
    {
        if (metadata is null)
        {
            return false;
        }

        var owned = metadata.SelectToken("objects.transactionFolder.ownedByThisApi");
        if (owned is not null && owned.Type == JTokenType.Boolean)
        {
            return owned.Value<bool>();
        }

        return metadata.SelectToken("objects.transactionFolder.wasCreated")?.Value<bool>() == true;
    }

    internal static Guid? TryReadFolderGuid(JObject? metadata)
    {
        var token = metadata?.SelectToken("objects.transactionFolder.guid");
        if (token is null || token.Type != JTokenType.String)
        {
            return null;
        }

        var value = token.Value<string>();
        if (string.IsNullOrWhiteSpace(value) || !Guid.TryParse(value, out var parsed) || parsed == Guid.Empty)
        {
            return null;
        }

        return parsed;
    }

    /// <summary>
    /// Sem GUID persistido (legado), a identidade não restringe. GUID persistido
    /// diferente do Folder na KB recusa exclusão — homônimo de terceiro.
    /// </summary>
    internal static bool MatchesPersistedGuid(Guid? persisted, Guid? current)
    {
        if (!persisted.HasValue || persisted.Value == Guid.Empty)
        {
            return true;
        }

        return current.HasValue && current.Value != Guid.Empty && persisted.Value == current.Value;
    }
}
