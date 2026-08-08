#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// B081 — relatório final pós-aplicação (criados / atualizados / removidos / bloqueados / avisos).
/// </summary>
public sealed class ApiPlanApplicationFinalReport
{
    public ApiPlanApplicationFinalReport(
        string operation,
        string transactionName,
        string? apiName,
        ApiPlanApplicationFinalOutcome outcome,
        string headline,
        TimeSpan elapsed,
        IReadOnlyList<ApiPlanApplicationFinalReportItem> created,
        IReadOnlyList<ApiPlanApplicationFinalReportItem> updated,
        IReadOnlyList<ApiPlanApplicationFinalReportItem> deleted,
        IReadOnlyList<ApiPlanApplicationFinalReportItem> blocked,
        IReadOnlyList<string> warnings,
        string? mainObjectName,
        Guid? mainObjectGuid)
    {
        Operation = operation ?? throw new ArgumentNullException(nameof(operation));
        TransactionName = transactionName ?? throw new ArgumentNullException(nameof(transactionName));
        ApiName = apiName;
        Outcome = outcome;
        Headline = headline ?? throw new ArgumentNullException(nameof(headline));
        Elapsed = elapsed;
        Created = created ?? throw new ArgumentNullException(nameof(created));
        Updated = updated ?? throw new ArgumentNullException(nameof(updated));
        Deleted = deleted ?? throw new ArgumentNullException(nameof(deleted));
        Blocked = blocked ?? throw new ArgumentNullException(nameof(blocked));
        Warnings = warnings ?? throw new ArgumentNullException(nameof(warnings));
        MainObjectName = mainObjectName;
        MainObjectGuid = mainObjectGuid;
    }

    public string Operation { get; }

    public string TransactionName { get; }

    public string? ApiName { get; }

    public ApiPlanApplicationFinalOutcome Outcome { get; }

    public string Headline { get; }

    public TimeSpan Elapsed { get; }

    public IReadOnlyList<ApiPlanApplicationFinalReportItem> Created { get; }

    public IReadOnlyList<ApiPlanApplicationFinalReportItem> Updated { get; }

    public IReadOnlyList<ApiPlanApplicationFinalReportItem> Deleted { get; }

    public IReadOnlyList<ApiPlanApplicationFinalReportItem> Blocked { get; }

    public IReadOnlyList<string> Warnings { get; }

    public string? MainObjectName { get; }

    public Guid? MainObjectGuid { get; }

    public int CreatedCount => Created.Count;

    public int UpdatedCount => Updated.Count;

    public int DeletedCount => Deleted.Count;

    public int BlockedCount => Blocked.Count;

    public int WarningCount => Warnings.Count;

    public string BuildOutputSummary()
    {
        var builder = new StringBuilder();
        builder.Append("[Genexus Open API Builder][B081] Relatorio final: ");
        builder.Append($"Operation='{Operation}', Transaction='{TransactionName}', ApiName='{ApiName ?? string.Empty}', ");
        builder.Append($"Outcome='{Outcome}', Created={CreatedCount}, Updated={UpdatedCount}, Deleted={DeletedCount}, ");
        builder.Append($"Blocked={BlockedCount}, Warnings={WarningCount}, DurationMs={(int)Elapsed.TotalMilliseconds}, Headline='{Headline}'.");
        return builder.ToString();
    }

    public string BuildReadableBody(bool includeHeadline = true)
    {
        var builder = new StringBuilder();
        if (includeHeadline)
        {
            builder.AppendLine(Headline);
            builder.AppendLine();
        }

        builder.AppendLine($"Operação: {Operation}");
        builder.AppendLine($"Transaction: {TransactionName}");
        if (!string.IsNullOrWhiteSpace(ApiName))
        {
            builder.AppendLine($"API: {ApiName}");
        }

        builder.AppendLine($"Tempo: {FormatElapsed(Elapsed)}");
        builder.AppendLine();
        AppendSection(builder, "Criados", Created);
        AppendSection(builder, "Atualizados", Updated);
        AppendSection(builder, "Removidos", Deleted);
        AppendSection(builder, "Bloqueados", Blocked);
        if (Warnings.Count == 0)
        {
            builder.AppendLine("Avisos: (nenhum)");
        }
        else
        {
            builder.AppendLine($"Avisos ({Warnings.Count}):");
            foreach (var warning in Warnings)
            {
                foreach (var line in WrapText("  - " + warning, 96))
                {
                    builder.AppendLine(line);
                }
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendSection(StringBuilder builder, string title, IReadOnlyList<ApiPlanApplicationFinalReportItem> items)
    {
        if (items.Count == 0)
        {
            builder.AppendLine($"{title}: (nenhum)");
            builder.AppendLine();
            return;
        }

        builder.AppendLine($"{title} ({items.Count}):");
        foreach (var item in items)
        {
            var detail = string.IsNullOrWhiteSpace(item.Detail) ? string.Empty : $" — {item.Detail}";
            foreach (var line in WrapText($"  - [{item.ObjectKind}] {item.Name}{detail}", 96))
            {
                builder.AppendLine(line);
            }
        }

        builder.AppendLine();
    }

    private static string[] WrapText(string text, int maxWidth)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxWidth)
        {
            return new[] { text };
        }

        var lines = Array.Empty<string>();
        var continuationPrefix = text.StartsWith("  - ", StringComparison.Ordinal) ? "    " : string.Empty;
        var remaining = text;
        var first = true;
        while (remaining.Length > 0)
        {
            var width = first ? maxWidth : Math.Max(20, maxWidth - continuationPrefix.Length);
            if (remaining.Length <= width)
            {
                AppendLine(ref lines, first ? remaining : continuationPrefix + remaining);
                break;
            }

            var splitAt = remaining.LastIndexOf(' ', width);
            if (splitAt <= 0)
            {
                splitAt = width;
            }

            var chunk = remaining.Substring(0, splitAt).TrimEnd();
            AppendLine(ref lines, first ? chunk : continuationPrefix + chunk);
            remaining = remaining.Substring(splitAt).TrimStart();
            first = false;
        }

        return lines;
    }

    private static void AppendLine(ref string[] lines, string line)
    {
        var next = new string[lines.Length + 1];
        Array.Copy(lines, next, lines.Length);
        next[lines.Length] = line;
        lines = next;
    }

    private static string FormatElapsed(TimeSpan elapsed)
    {
        if (elapsed.TotalSeconds < 1)
        {
            return $"{(int)elapsed.TotalMilliseconds} ms";
        }

        if (elapsed.TotalMinutes < 1)
        {
            return $"{elapsed.TotalSeconds:0.0} s";
        }

        return $"{(int)elapsed.TotalMinutes} min {elapsed.Seconds} s";
    }
}

public enum ApiPlanApplicationFinalOutcome
{
    Success = 0,
    SuccessWithWarnings = 1,
    Interrupted = 2,
}

public sealed class ApiPlanApplicationFinalReportItem
{
    public ApiPlanApplicationFinalReportItem(string objectKind, string name, string? detail = null)
    {
        ObjectKind = objectKind ?? throw new ArgumentNullException(nameof(objectKind));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Detail = detail;
    }

    public string ObjectKind { get; }

    public string Name { get; }

    public string? Detail { get; }
}

/// <summary>
/// Acumulador mutável usado durante Wizard / Sync / Remover para montar o relatório B081.
/// </summary>
public sealed class ApiPlanApplicationFinalReportCollector
{
    private ApiPlanApplicationFinalReportItem[] _created = Array.Empty<ApiPlanApplicationFinalReportItem>();
    private ApiPlanApplicationFinalReportItem[] _updated = Array.Empty<ApiPlanApplicationFinalReportItem>();
    private ApiPlanApplicationFinalReportItem[] _deleted = Array.Empty<ApiPlanApplicationFinalReportItem>();
    private ApiPlanApplicationFinalReportItem[] _blocked = Array.Empty<ApiPlanApplicationFinalReportItem>();
    private string[] _warnings = Array.Empty<string>();
    private string[] _createdKeys = Array.Empty<string>();
    private string[] _updatedKeys = Array.Empty<string>();
    private string[] _deletedKeys = Array.Empty<string>();
    private string[] _warningKeys = Array.Empty<string>();

    public ApiPlanApplicationFinalReportCollector(string operation, string transactionName, string? apiName)
    {
        Operation = operation ?? throw new ArgumentNullException(nameof(operation));
        TransactionName = transactionName ?? throw new ArgumentNullException(nameof(transactionName));
        ApiName = apiName;
    }

    public string Operation { get; }

    public string TransactionName { get; }

    public string? ApiName { get; private set; }

    public string? MainObjectName { get; private set; }

    public Guid? MainObjectGuid { get; private set; }

    public string? HeadlineOverride { get; set; }

    public bool HasInterrupted => _blocked.Length > 0;

    public void SetApiName(string? apiName)
    {
        if (!string.IsNullOrWhiteSpace(apiName))
        {
            ApiName = apiName;
        }
    }

    public void SetMainObject(string name, Guid guid)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Nome do objeto principal e obrigatorio.", nameof(name));
        }

        MainObjectName = name;
        MainObjectGuid = guid;
    }

    public void AddCreated(string objectKind, string name, string? detail = null)
    {
        AddUniqueItem(ref _created, ref _createdKeys, objectKind, name, detail);
    }

    public void AddUpdated(string objectKind, string name, string? detail = null)
    {
        var key = BuildKey(objectKind, name);
        if (ContainsKey(_createdKeys, key))
        {
            return;
        }

        AddUniqueItem(ref _updated, ref _updatedKeys, objectKind, name, detail);
    }

    public void AddDeleted(string objectKind, string name, string? detail = null)
    {
        AddUniqueItem(ref _deleted, ref _deletedKeys, objectKind, name, detail);
    }

    public void AddBlocked(string objectKind, string name, string? detail = null)
    {
        AppendItem(ref _blocked, new ApiPlanApplicationFinalReportItem(objectKind, name, detail));
    }

    public void AddWarning(string warning)
    {
        if (string.IsNullOrWhiteSpace(warning))
        {
            return;
        }

        var trimmed = warning.Trim();
        if (ContainsKey(_warningKeys, trimmed))
        {
            return;
        }

        AppendString(ref _warningKeys, trimmed);
        AppendWarning(ref _warnings, trimmed);
    }

    public void AddFromWriteStatus(string objectKind, string name, string status, string? detail = null)
    {
        if (string.Equals(status, "Created", StringComparison.OrdinalIgnoreCase))
        {
            AddCreated(objectKind, name, detail);
            return;
        }

        if (string.Equals(status, "Reencountered", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Updated", StringComparison.OrdinalIgnoreCase))
        {
            AddUpdated(objectKind, name, detail);
            return;
        }

        AddUpdated(objectKind, name, string.IsNullOrWhiteSpace(detail) ? status : detail);
    }

    public void AddDeletedItems(string[] rawItems)
    {
        if (rawItems is null)
        {
            throw new ArgumentNullException(nameof(rawItems));
        }

        for (var index = 0; index < rawItems.Length; index++)
        {
            var raw = rawItems[index];
            if (TryParsePreservedFolder(raw, out var preservedFolder))
            {
                AddWarning($"Folder '{preservedFolder}' nao foi apagado porque nao ficou vazio.");
                continue;
            }

            ParseDeletedItem(raw, out var kind, out var name);
            AddDeleted(kind, name);
            if (string.Equals(kind, "API Object", StringComparison.OrdinalIgnoreCase))
            {
                MainObjectName = name;
            }
        }
    }

    public ApiPlanApplicationFinalReport Build(TimeSpan elapsed)
    {
        var outcome = ResolveOutcome();
        var headline = string.IsNullOrWhiteSpace(HeadlineOverride)
            ? ResolveHeadline(outcome)
            : HeadlineOverride!;
        return new ApiPlanApplicationFinalReport(
            Operation,
            TransactionName,
            ApiName,
            outcome,
            headline,
            elapsed,
            _created,
            _updated,
            _deleted,
            _blocked,
            _warnings,
            MainObjectName,
            MainObjectGuid);
    }

    private ApiPlanApplicationFinalOutcome ResolveOutcome()
    {
        if (_blocked.Length > 0)
        {
            return ApiPlanApplicationFinalOutcome.Interrupted;
        }

        if (_warnings.Length > 0)
        {
            return ApiPlanApplicationFinalOutcome.SuccessWithWarnings;
        }

        return ApiPlanApplicationFinalOutcome.Success;
    }

    private string ResolveHeadline(ApiPlanApplicationFinalOutcome outcome)
    {
        var verb = ResolveVerb();
        if (outcome == ApiPlanApplicationFinalOutcome.Success)
        {
            return verb + " com sucesso.";
        }

        if (outcome == ApiPlanApplicationFinalOutcome.SuccessWithWarnings)
        {
            return verb + " com avisos.";
        }

        return ResolveInterruptedHeadline();
    }

    private string ResolveVerb()
    {
        if (string.Equals(Operation, "Remover", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Operation, "Remove", StringComparison.OrdinalIgnoreCase))
        {
            return "API removida";
        }

        if (string.Equals(Operation, "Sincronizar", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Operation, "Sync", StringComparison.OrdinalIgnoreCase))
        {
            return "API sincronizada";
        }

        return "API gerada";
    }

    private string ResolveInterruptedHeadline()
    {
        if (string.Equals(Operation, "Remover", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Operation, "Remove", StringComparison.OrdinalIgnoreCase))
        {
            return "Remocao interrompida.";
        }

        if (string.Equals(Operation, "Sincronizar", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Operation, "Sync", StringComparison.OrdinalIgnoreCase))
        {
            return "Sincronizacao interrompida.";
        }

        return "Geracao interrompida.";
    }

    private static void AddUniqueItem(
        ref ApiPlanApplicationFinalReportItem[] target,
        ref string[] keys,
        string objectKind,
        string name,
        string? detail)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var key = BuildKey(objectKind, name);
        if (ContainsKey(keys, key))
        {
            return;
        }

        AppendString(ref keys, key);
        AppendItem(ref target, new ApiPlanApplicationFinalReportItem(objectKind, name, detail));
    }

    private static bool ContainsKey(string[] keys, string key)
    {
        for (var index = 0; index < keys.Length; index++)
        {
            if (string.Equals(keys[index], key, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void AppendItem(ref ApiPlanApplicationFinalReportItem[] target, ApiPlanApplicationFinalReportItem item)
    {
        var next = new ApiPlanApplicationFinalReportItem[target.Length + 1];
        Array.Copy(target, next, target.Length);
        next[target.Length] = item;
        target = next;
    }

    private static void AppendString(ref string[] target, string value)
    {
        var next = new string[target.Length + 1];
        Array.Copy(target, next, target.Length);
        next[target.Length] = value;
        target = next;
    }

    private static void AppendWarning(ref string[] target, string warning)
    {
        AppendString(ref target, warning);
    }

    private static string BuildKey(string objectKind, string name) => objectKind + "|" + name;

    private static void ParseDeletedItem(string raw, out string kind, out string name)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            kind = "Object";
            name = raw ?? string.Empty;
            return;
        }

        var separator = raw.IndexOf(':');
        if (separator <= 0 || separator >= raw.Length - 1)
        {
            kind = "Object";
            name = raw;
            return;
        }

        kind = NormalizeDeletedKind(raw.Substring(0, separator).Trim());
        name = raw.Substring(separator + 1).Trim();
    }

    private static bool TryParsePreservedFolder(string raw, out string folderName)
    {
        folderName = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var parts = raw.Split(new[] { ':' }, StringSplitOptions.None);
        if (parts.Length < 3)
        {
            return false;
        }

        if (!string.Equals(parts[0], "Folder", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.Equals(parts[parts.Length - 1], "PreservedNonEmpty", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        folderName = string.Join(":", parts, 1, parts.Length - 2);
        return !string.IsNullOrWhiteSpace(folderName);
    }

    private static string NormalizeDeletedKind(string kind)
    {
        if (string.Equals(kind, "API", StringComparison.OrdinalIgnoreCase)
            || string.Equals(kind, "ApiObject", StringComparison.OrdinalIgnoreCase)
            || string.Equals(kind, "API Object", StringComparison.OrdinalIgnoreCase))
        {
            return "API Object";
        }

        if (string.Equals(kind, "Procedure", StringComparison.OrdinalIgnoreCase))
        {
            return "Procedure";
        }

        if (string.Equals(kind, "SDT", StringComparison.OrdinalIgnoreCase))
        {
            return "SDT";
        }

        if (string.Equals(kind, "File", StringComparison.OrdinalIgnoreCase)
            || string.Equals(kind, "Metadata", StringComparison.OrdinalIgnoreCase))
        {
            return "File";
        }

        if (string.Equals(kind, "Folder", StringComparison.OrdinalIgnoreCase))
        {
            return "Folder";
        }

        return kind;
    }
}
