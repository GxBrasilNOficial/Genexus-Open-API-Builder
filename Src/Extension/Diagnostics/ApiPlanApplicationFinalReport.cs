#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
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
        Guid? mainObjectGuid,
        string? plannedApiName = null,
        string? persistedMainObjectName = null,
        Guid? persistedMainObjectGuid = null,
        string? finalApiWriter = null,
        int apiSaveCount = 0,
        bool apiSaveAttempted = false,
        ApiPlanPersistenceLog? persistenceLog = null,
        IReadOnlyList<string>? information = null)
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
        PlannedApiName = plannedApiName ?? apiName;
        // B111/F3 P3: sem fallback para o objeto apenas identificado. O fallback fazia o
        // relatório repor aqui o que o collector deixou vazio de propósito, e foi assim que
        // operações sem gravação nenhuma reportaram PersistedMainObject preenchido — medido
        // na IDE em 2026-09-14.
        PersistedMainObjectName = persistedMainObjectName;
        PersistedMainObjectGuid = persistedMainObjectGuid;
        FinalApiWriter = finalApiWriter;
        ApiSaveCount = apiSaveCount;
        ApiSaveAttempted = apiSaveAttempted;
        PersistenceLog = persistenceLog;
        Information = information ?? Array.Empty<string>();
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

    /// <summary>Informações de conclusão que não representam aviso nem bloqueio.</summary>
    public IReadOnlyList<string> Information { get; }

    public string? MainObjectName { get; }

    public Guid? MainObjectGuid { get; }

    public string? PlannedApiName { get; }

    public string? PersistedMainObjectName { get; }

    public Guid? PersistedMainObjectGuid { get; }

    public string? FinalApiWriter { get; }

    public int ApiSaveCount { get; }

    public bool ApiSaveAttempted { get; }

    public ApiPlanPersistenceLog? PersistenceLog { get; }

    public IReadOnlyList<PersistenceReceipt> PersistenceReceipts => PersistenceLog?.Receipts ?? Array.Empty<PersistenceReceipt>();

    public IReadOnlyList<PersistenceStageFailure> PersistenceStageFailures => PersistenceLog?.StageFailures ?? Array.Empty<PersistenceStageFailure>();

    public int CreatedCount => Created.Count;

    public int UpdatedCount => Updated.Count;

    public int DeletedCount => Deleted.Count;

    public int BlockedCount => Blocked.Count;

    public int WarningCount => Warnings.Count;

    public string BuildOutputSummary()
    {
        var builder = new StringBuilder();
        builder.Append("[Genexus Open API Builder][B081] Relatório final: ");
        builder.Append($"Operação='{Operation}', Transaction='{TransactionName}', ApiName='{ApiName ?? string.Empty}', ");
        builder.Append($"PlannedApiName='{PlannedApiName ?? string.Empty}', PersistedMainObjectName='{PersistedMainObjectName ?? string.Empty}', PersistedMainObjectGuid='{PersistedMainObjectGuid?.ToString() ?? string.Empty}', ");
        builder.Append($"FinalApiWriter='{FinalApiWriter ?? string.Empty}', ApiSaveAttempted={ApiSaveAttempted}, ApiSaveCount={ApiSaveCount}, ");
        builder.Append($"Resultado='{Outcome}', Criados={CreatedCount}, Atualizados={UpdatedCount}, Removidos={DeletedCount}, ");
        builder.Append($"Bloqueados={BlockedCount}, Avisos={WarningCount}, Informações={Information.Count}, PersistenceReceipts={PersistenceReceipts.Count}, PersistenceStageFailures={PersistenceStageFailures.Count}, DuraçãoMs={(int)Elapsed.TotalMilliseconds}, Título='{Headline}'.");
        return builder.ToString();
    }

    public string BuildReadableBody(bool includeHeadline = true, Func<string, string>? localize = null)
    {
        string Localize(string value) => localize is null ? value : localize(value);
        var persistenceHasAnomaly = HasPersistenceAnomaly();
        var requiresTechnicalDetails = Blocked.Count > 0 || persistenceHasAnomaly;

        var builder = new StringBuilder();
        if (includeHeadline)
        {
            builder.AppendLine(Localize(Headline));
            builder.AppendLine();
        }

        builder.AppendLine(Localize($"Operação: {Operation}"));
        builder.AppendLine($"Transaction: {TransactionName}");
        if (!string.IsNullOrWhiteSpace(ApiName))
        {
            builder.AppendLine($"API: {ApiName}");
        }

        if (ApiSaveAttempted || !string.IsNullOrWhiteSpace(FinalApiWriter))
        {
            var writer = string.IsNullOrWhiteSpace(FinalApiWriter) ? "não informado" : FinalApiWriter;
            var saveSummary = ApiSaveCount == 1 ? "1 salvamento confirmado" : $"{ApiSaveCount} salvamentos confirmados";
            builder.AppendLine($"API Object: writer {writer}; {saveSummary}.");
            if (requiresTechnicalDetails)
            {
                builder.AppendLine($"API planejada: {PlannedApiName ?? string.Empty}");
                builder.AppendLine($"Objeto principal persistido: {PersistedMainObjectName ?? string.Empty}");
                builder.AppendLine($"Guid persistido do objeto principal: {PersistedMainObjectGuid?.ToString() ?? string.Empty}");
                builder.AppendLine($"Tentativa de salvamento do API Object: {(ApiSaveAttempted ? "sim" : "não")}");
            }
        }

        AppendPersistenceSummary(builder, Localize, persistenceHasAnomaly);

        builder.AppendLine(Localize($"Resultado: Criados={CreatedCount}; Atualizados={UpdatedCount}; Removidos={DeletedCount}."));
        builder.AppendLine(Localize($"Tempo: {FormatElapsed(Elapsed)}"));
        builder.AppendLine();

        if (requiresTechnicalDetails)
        {
            AppendSection(builder, "Bloqueados", Blocked, Localize);
        }

        if (Information.Count > 0)
        {
            builder.AppendLine(Localize($"Informações ({Information.Count}):"));
            foreach (var information in Information)
            {
                foreach (var line in WrapText("  - " + Localize(information), 96))
                {
                    builder.AppendLine(line);
                }
            }

            builder.AppendLine();
        }

        if (Warnings.Count == 0)
        {
            builder.AppendLine(Localize("Avisos: (nenhum)"));
        }
        else
        {
            builder.AppendLine(Localize($"Avisos ({Warnings.Count}):"));
            foreach (var warning in Warnings)
            {
                foreach (var line in WrapText("  - " + Localize(warning), 96))
                {
                    builder.AppendLine(line);
                }
            }
        }

        return builder.ToString().TrimEnd();
    }

    private bool HasPersistenceAnomaly() =>
        PersistenceStageFailures.Count > 0
        || PersistenceReceipts.Any(receipt => receipt.Outcome != PersistenceOutcome.Confirmed);

    private void AppendPersistenceSummary(
        StringBuilder builder,
        Func<string, string> localize,
        bool persistenceHasAnomaly)
    {
        if (PersistenceLog is null)
        {
            return;
        }

        var confirmed = PersistenceReceipts.Count(receipt => receipt.Outcome == PersistenceOutcome.Confirmed);
        var anomalies = PersistenceReceipts.Count - confirmed + PersistenceStageFailures.Count;
        builder.AppendLine(localize($"Persistência: Confirmados={confirmed}; Pendências={anomalies}."));

        if (!persistenceHasAnomaly)
        {
            return;
        }

        builder.AppendLine(localize("Diagnóstico de persistência:"));
        foreach (var receipt in PersistenceReceipts.Where(receipt => receipt.Outcome != PersistenceOutcome.Confirmed))
        {
            var detail = string.IsNullOrWhiteSpace(receipt.ExceptionMessage)
                ? receipt.ConfirmationDetail ?? string.Empty
                : receipt.ExceptionMessage;
            builder.AppendLine($"  - [{receipt.Stage}/{receipt.ObjectType}] {receipt.PlannedName}: Outcome={receipt.Outcome}; Confirmação={receipt.Confirmation}; {detail}".TrimEnd());
        }

        foreach (var failure in PersistenceStageFailures)
        {
            builder.AppendLine($"  - [{failure.Stage}] {failure.ReasonCode}: {failure.Detail}");
        }

        builder.AppendLine();
    }

    private static void AppendSection(
        StringBuilder builder,
        string title,
        IReadOnlyList<ApiPlanApplicationFinalReportItem> items,
        Func<string, string> localize)
    {
        if (items.Count == 0)
        {
            builder.AppendLine(localize($"{title}: (nenhum)"));
            builder.AppendLine();
            return;
        }

        builder.AppendLine(localize($"{title} ({items.Count}):"));
        foreach (var item in items)
        {
            var detail = string.IsNullOrWhiteSpace(item.Detail) ? string.Empty : $" — {item.Detail}";
            foreach (var line in WrapText(localize($"  - [{item.ObjectKind}] {item.Name}{detail}"), 96))
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
    private string[] _information = Array.Empty<string>();
    private string[] _warnings = Array.Empty<string>();
    private string[] _createdKeys = Array.Empty<string>();
    private string[] _updatedKeys = Array.Empty<string>();
    private string[] _deletedKeys = Array.Empty<string>();
    private string[] _informationKeys = Array.Empty<string>();
    private string[] _warningKeys = Array.Empty<string>();
    private ApiPlanPersistenceLog? _persistenceLog;

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

    public string? PlannedApiName { get; private set; }

    public string? PersistedMainObjectName { get; private set; }

    public Guid? PersistedMainObjectGuid { get; private set; }

    public string? FinalApiWriter { get; private set; }

    public int ApiSaveCount { get; private set; }

    public bool ApiSaveAttempted { get; private set; }

    public ApiPlanPersistenceLog? PersistenceLog => _persistenceLog;

    /// <summary>
    /// Nomes dos objetos criados nesta operação. O diário B111/F3 usa esta lista para
    /// distinguir `Create` de `Update` no inventário: o recibo de persistência prova que o
    /// alvo foi gravado, mas não diz se ele nasceu agora.
    /// </summary>
    public IReadOnlyList<string> CreatedObjectNames =>
        _created.Select(item => item.Name).ToArray();

    public void SetPersistenceLog(ApiPlanPersistenceLog? persistenceLog)
    {
        _persistenceLog = persistenceLog;
    }

    public bool ApiSavePathEntered { get; private set; }

    public string? HeadlineOverride { get; set; }

    public bool HasInterrupted => _blocked.Length > 0;

    public void SetApiName(string? apiName)
    {
        if (!string.IsNullOrWhiteSpace(apiName))
        {
            ApiName = apiName;
            PlannedApiName ??= apiName;
        }
    }

    public void SetPlannedApiName(string? apiName)
    {
        if (!string.IsNullOrWhiteSpace(apiName))
        {
            PlannedApiName = apiName;
            ApiName ??= apiName;
        }
    }

    /// <summary>
    /// Identifica o objeto principal da operacao — o que o botao «Abrir objeto principal»
    /// usa. Identificar **nao** e persistir: um reencontro, um bloqueio antes da primeira
    /// gravacao ou um Apply que nao escreve o API Object tambem passam por aqui. Quem
    /// declara persistencia e <see cref="SetPersistedMainObject"/>, chamado apenas quando
    /// um Save foi confirmado.
    /// </summary>
    public void SetMainObject(string name, Guid guid)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Nome do objeto principal e obrigatorio.", nameof(name));
        }

        MainObjectName = name;
        MainObjectGuid = guid;
    }

    public void SetPersistedMainObject(string name, Guid guid)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Nome do objeto persistido e obrigatorio.", nameof(name));
        }

        if (guid == Guid.Empty)
        {
            throw new ArgumentException("O GUID do objeto persistido nao pode ser vazio.", nameof(guid));
        }

        PersistedMainObjectName = name;
        PersistedMainObjectGuid = guid;
        MainObjectName = name;
        MainObjectGuid = guid;
    }

    public void SetApiWriter(string? writer)
    {
        if (!string.IsNullOrWhiteSpace(writer))
        {
            FinalApiWriter = writer;
        }
    }

    public void RecordApiSave()
    {
        ApiSaveCount++;
    }

    public void MarkApiSaveAttempted()
    {
        ApiSaveAttempted = true;
    }

    public void MarkApiSavePathEntered()
    {
        ApiSavePathEntered = true;
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

    public void AddInformation(string information)
    {
        if (string.IsNullOrWhiteSpace(information))
        {
            return;
        }

        var trimmed = information.Trim();
        if (ContainsKey(_informationKeys, trimmed))
        {
            return;
        }

        AppendString(ref _informationKeys, trimmed);
        AppendWarning(ref _information, trimmed);
    }

    public void AddFromWriteStatus(string objectKind, string name, string status, string? detail = null)
    {
        if (string.Equals(status, "Created", StringComparison.OrdinalIgnoreCase))
        {
            AddCreated(objectKind, name, detail);
            return;
        }

        if (string.Equals(status, "Unchanged", StringComparison.OrdinalIgnoreCase))
        {
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
            ParseDeletedItem(raw, out var kind, out var name);
            AddDeleted(kind, name);
            if (string.Equals(kind, "API Object", StringComparison.OrdinalIgnoreCase))
            {
                MainObjectName = name;
            }
        }
    }

    /// <summary>
    /// B082 Fatia B: Folder preservado por não estar vazio — aviso tipado, nunca via lista de
    /// removidos com string mágica <c>Folder:{nome}:PreservedNonEmpty</c>.
    /// </summary>
    public void AddPreservedNonEmptyFolder(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
        {
            throw new ArgumentException("Nome do Folder preservado e obrigatorio.", nameof(folderName));
        }

        AddWarning($"Folder '{folderName.Trim()}' nao foi apagado porque nao ficou vazio.");
    }

    public void AddPreservedNonEmptyFolders(IReadOnlyList<string> folderNames)
    {
        if (folderNames is null)
        {
            throw new ArgumentNullException(nameof(folderNames));
        }

        for (var index = 0; index < folderNames.Count; index++)
        {
            AddPreservedNonEmptyFolder(folderNames[index]);
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
            MainObjectGuid,
            PlannedApiName,
            PersistedMainObjectName,
            PersistedMainObjectGuid,
            FinalApiWriter,
            ApiSaveCount,
            ApiSaveAttempted,
            _persistenceLog,
            _information);
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

        // B111/F3 P8: a recuperação não gera nada — ela retoma, reconcilia, abandona ou
        // encerra o registro. Sem este caso, o default anunciava «API gerada com sucesso.»
        // logo depois de uma remoção retomada, que é o oposto do que aconteceu.
        if (string.Equals(Operation, "Recuperar", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Operation, "Recover", StringComparison.OrdinalIgnoreCase))
        {
            return "Operação recuperada";
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

        if (string.Equals(Operation, "Recuperar", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Operation, "Recover", StringComparison.OrdinalIgnoreCase))
        {
            return "Recuperação interrompida.";
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
