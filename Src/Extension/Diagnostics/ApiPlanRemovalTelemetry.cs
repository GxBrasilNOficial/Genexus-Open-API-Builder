#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// B082 — instrumentação de custo do Remover: conta as varreduras completas de catálogo
/// (<c>GetAll</c>) e mede o tempo por tipo de objeto e por fase.
/// Apenas observa: não altera ordem, condição ou resultado de nenhuma operação.
/// </summary>
internal sealed class ApiPlanRemovalTelemetry
{
    private readonly List<ScanEntry> _scans = new List<ScanEntry>();
    private readonly List<PhaseEntry> _phases = new List<PhaseEntry>();
    private readonly List<string> _notes = new List<string>();

    /// <summary>
    /// Executa e cronometra uma varredura completa de catálogo. O delegate deve conter
    /// o pipeline inteiro, incluindo a materialização (<c>ToArray</c>/<c>Any</c>), porque
    /// <c>GetAll</c> é preguiçoso e o custo está na enumeração, não na chamada.
    /// </summary>
    public T MeasureScan<T>(string objectType, string phase, Func<T> scan)
    {
        if (scan is null)
        {
            throw new ArgumentNullException(nameof(scan));
        }

        var watch = Stopwatch.StartNew();
        var result = scan();
        watch.Stop();
        _scans.Add(new ScanEntry(objectType ?? "?", phase ?? "?", watch.ElapsedMilliseconds));
        return result;
    }

    public void MarkPhase(string phase, long elapsedMs)
    {
        _phases.Add(new PhaseEntry(phase ?? "?", elapsedMs));
    }

    /// <summary>
    /// Observação factual coletada durante a execução (ex.: o contêiner real do metadata File).
    /// </summary>
    public void AddNote(string note)
    {
        if (!string.IsNullOrWhiteSpace(note))
        {
            _notes.Add(note);
        }
    }

    public int ScanCount => _scans.Count;

    public long TotalScanMs => _scans.Sum(entry => entry.ElapsedMs);

    /// <summary>
    /// Linhas prontas para a janela Output da IDE, no mesmo formato das demais medições B082.
    /// </summary>
    public IReadOnlyList<string> BuildOutputLines()
    {
        var lines = new List<string>
        {
            string.Format(
                CultureInfo.InvariantCulture,
                "Scans={0}, TotalScanMs={1}",
                _scans.Count,
                TotalScanMs),
        };

        if (_phases.Count > 0)
        {
            lines.Add("Fases: " + string.Join(
                "; ",
                _phases.Select(entry => string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}={1}ms",
                    entry.Phase,
                    entry.ElapsedMs))));
        }

        var grouped = _scans
            .GroupBy(entry => entry.ObjectType + "/" + entry.Phase, StringComparer.Ordinal)
            .Select(group => new
            {
                Key = group.Key,
                Count = group.Count(),
                ElapsedMs = group.Sum(entry => entry.ElapsedMs),
            })
            .OrderByDescending(item => item.ElapsedMs)
            .ThenByDescending(item => item.Count)
            .ThenBy(item => item.Key, StringComparer.Ordinal)
            .ToArray();

        foreach (var item in grouped)
        {
            lines.Add(string.Format(
                CultureInfo.InvariantCulture,
                "Scan {0}: {1}x, {2}ms",
                item.Key,
                item.Count,
                item.ElapsedMs));
        }

        foreach (var note in _notes)
        {
            lines.Add("Nota: " + note);
        }

        return lines;
    }

    private readonly struct ScanEntry
    {
        public ScanEntry(string objectType, string phase, long elapsedMs)
        {
            ObjectType = objectType;
            Phase = phase;
            ElapsedMs = elapsedMs;
        }

        public string ObjectType { get; }

        public string Phase { get; }

        public long ElapsedMs { get; }
    }

    private readonly struct PhaseEntry
    {
        public PhaseEntry(string phase, long elapsedMs)
        {
            Phase = phase;
            ElapsedMs = elapsedMs;
        }

        public string Phase { get; }

        public long ElapsedMs { get; }
    }
}
