#nullable enable

using System;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// B082 — progresso cooperativo no thread da UI (Apply / Remover / Sync / abertura).
/// Abortar pede parada entre objetos; o Save/Delete em curso termina.
/// </summary>
internal sealed class ApiPlanBusyProgressSession
{
    private readonly Action<ApiPlanBusyProgressUpdate> _onUpdate;
    private readonly Action? _pump;

    public ApiPlanBusyProgressSession(Action<ApiPlanBusyProgressUpdate> onUpdate, Action? pump = null)
    {
        _onUpdate = onUpdate ?? throw new ArgumentNullException(nameof(onUpdate));
        _pump = pump;
    }

    public bool AbortRequested { get; private set; }

    public bool WasAborted { get; private set; }

    public void RequestAbort()
    {
        AbortRequested = true;
    }

    public void ThrowIfAbortRequested()
    {
        if (!AbortRequested)
        {
            return;
        }

        WasAborted = true;
        throw new ApiPlanBusyAbortedException(
            "Operação abortada pelo usuário. O objeto em curso foi concluído; a KB pode ter ficado inconsistente. Use Remover / Wizard / Sync para reparar.");
    }

    public void Report(string stage, int current, int total, string itemName, long elapsedMs = -1)
    {
        _onUpdate(new ApiPlanBusyProgressUpdate(stage, current, total, itemName ?? string.Empty, elapsedMs));
    }

    /// <summary>
    /// Processa mensagens pendentes da UI (ex.: clique em Abortar) entre Saves longos.
    /// </summary>
    public void Pump()
    {
        _pump?.Invoke();
    }

    public void PumpAndThrowIfAbortRequested()
    {
        Pump();
        ThrowIfAbortRequested();
    }
}

internal readonly struct ApiPlanBusyProgressUpdate
{
    public ApiPlanBusyProgressUpdate(string stage, int current, int total, string itemName, long elapsedMs)
    {
        Stage = stage ?? string.Empty;
        Current = current;
        Total = total;
        ItemName = itemName ?? string.Empty;
        ElapsedMs = elapsedMs;
    }

    public string Stage { get; }

    public int Current { get; }

    public int Total { get; }

    public string ItemName { get; }

    public long ElapsedMs { get; }
}

internal sealed class ApiPlanBusyAbortedException : InvalidOperationException
{
    public ApiPlanBusyAbortedException(string message)
        : base(message)
    {
    }
}
