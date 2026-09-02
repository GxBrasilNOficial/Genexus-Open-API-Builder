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
internal static class ApiPlanScanProbe
{
    [ThreadStatic]
    private static ApiPlanScanTelemetry? _current;

    /// <summary>
    /// Ativa a medição até o <c>Dispose</c>. Escopos aninhados restauram o anterior,
    /// então um escopo interno nunca desliga a medição de quem o chamou.
    /// </summary>
    public static IDisposable Begin(ApiPlanScanTelemetry telemetry)
    {
        if (telemetry is null)
        {
            throw new ArgumentNullException(nameof(telemetry));
        }

        var previous = _current;
        _current = telemetry;
        return new Scope(previous);
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

    public static void Note(string note)
    {
        _current?.AddNote(note);
    }

    private sealed class Scope : IDisposable
    {
        private readonly ApiPlanScanTelemetry? _previous;
        private bool _disposed;

        public Scope(ApiPlanScanTelemetry? previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _current = _previous;
        }
    }
}
