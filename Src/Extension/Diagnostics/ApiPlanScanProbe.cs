#nullable enable

using System;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// B082 — probe de escopo por operação para medir o custo das varreduras de catálogo
/// (<c>GetAll</c>) ao longo de Apply/Sync, que atravessam muitos writers.
///
/// Existe para não propagar um parâmetro de instrumentação por dezenas de assinaturas.
/// Quando não há escopo ativo, <see cref="Scan"/> apenas executa o delegate: custo zero
/// e comportamento idêntico ao código não instrumentado.
///
/// O estado é <c>[ThreadStatic]</c> porque todo o fluxo da extensão roda na thread da UI;
/// uma eventual execução em outra thread simplesmente não é medida, nunca medida errado.
/// </summary>
public static class ApiPlanScanProbe
{
    [ThreadStatic]
    private static ApiPlanScanTelemetry? _current;

    /// <summary>
    /// Ativa a medição até o <c>Dispose</c>. Escopos aninhados restauram o anterior,
    /// então um escopo interno nunca desliga a medição de quem o chamou.
    /// </summary>
    /// <param name="onDispose">
    /// Publicação opcional ao encerrar o escopo. Útil em fluxos com vários pontos de retorno,
    /// onde repetir a publicação em cada um deixaria algum caminho sem medição.
    /// </param>
    public static IDisposable Begin(ApiPlanScanTelemetry telemetry, Action<ApiPlanScanTelemetry>? onDispose = null)
    {
        if (telemetry is null)
        {
            throw new ArgumentNullException(nameof(telemetry));
        }

        var previous = _current;
        _current = telemetry;
        return new Scope(previous, telemetry, onDispose);
    }

    /// <summary>
    /// Executa a varredura, medindo-a quando há escopo ativo. O delegate precisa conter
    /// o pipeline inteiro até a materialização, porque <c>GetAll</c> é preguiçoso.
    /// </summary>
    public static T Scan<T>(string objectType, string phase, Func<T> scan)
    {
        if (scan is null)
        {
            throw new ArgumentNullException(nameof(scan));
        }

        var telemetry = _current;
        return telemetry is null ? scan() : telemetry.MeasureScan(objectType, phase, scan);
    }

    /// <summary>
    /// Suspende a medição até o <c>Dispose</c>, sem encerrar o escopo ativo.
    /// Serve para trechos que rodam dentro da operação mas não fazem parte dela — a
    /// apresentação do relatório final, por exemplo, cujas leituras não devem ser
    /// atribuídas ao Apply, ao Sync ou ao Remover que acabou de terminar.
    /// </summary>
    public static IDisposable Suspend()
    {
        var previous = _current;
        _current = null;
        return new Scope(previous, ApiPlanScanTelemetry.Empty, onDispose: null);
    }

    public static void Note(string note)
    {
        _current?.AddNote(note);
    }

    private sealed class Scope : IDisposable
    {
        private readonly ApiPlanScanTelemetry? _previous;
        private readonly ApiPlanScanTelemetry _telemetry;
        private readonly Action<ApiPlanScanTelemetry>? _onDispose;
        private bool _disposed;

        public Scope(
            ApiPlanScanTelemetry? previous,
            ApiPlanScanTelemetry telemetry,
            Action<ApiPlanScanTelemetry>? onDispose)
        {
            _previous = previous;
            _telemetry = telemetry;
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _current = _previous;

            // A publicacao nunca pode derrubar o fluxo que esta sendo medido.
            try
            {
                _onDispose?.Invoke(_telemetry);
            }
            catch
            {
            }
        }
    }
}
