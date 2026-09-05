#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// B111 · F1 — sonda de contagem de chamadas e de gravações efetivas, para decidir o custo
/// da regra de responsabilidade única por Folder e SDT (seção 4.4.1 do plano da F1).
///
/// A leitura de código mostrou que <c>ApiPlanTransactionFolder.CreateOrReencounter</c> é
/// chamado em seis pontos e <c>ApiPlanSdtWriter.CreateOrReencounter</c> em quatro. O que a
/// leitura **não** diz é quantas dessas passagens efetivamente **gravam** numa aplicação
/// real. A diferença decide o risco da regra:
///
/// <list type="bullet">
/// <item>se as passagens extras já forem reencontro puro, tornar isso explícito é barato e
/// sem efeito observável;</item>
/// <item>se alguma delas grava hoje, proibi-la de gravar muda comportamento, e a mudança
/// precisa de cuidado maior do que a F1 previu.</item>
/// </list>
///
/// <para>
/// <b>Diferença em relação às sondas B111 anteriores.</b> Aquelas criavam os próprios
/// objetos e mediam em isolamento. Esta observa um Apply ou Sync **real**, disparado pelo
/// usuário, e por isso instrumenta caminhos de produção. A instrumentação é aditiva: ela
/// conta e nunca altera fluxo, condição, ordem ou resultado. Sem escopo ativo, cada chamada
/// é um teste de nulo e retorna.
/// </para>
///
/// É sonda temporária: sai junto com as demais no fechamento da sprint S-B111, conforme o
/// checklist em <c>Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md</c>.
/// </summary>
public static class B111CallSiteProbe
{
    [ThreadStatic]
    private static B111CallSiteLog? _current;

    public static IDisposable Begin(B111CallSiteLog log)
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        var previous = _current;
        _current = log;
        return new Scope(previous);
    }

    /// <summary>Registra que um ponto de entrada foi alcançado.</summary>
    public static void Enter(string site) => _current?.Enter(site);

    /// <summary>Registra uma gravação efetiva na KB.</summary>
    public static void Wrote(string site, string objectName) => _current?.Wrote(site, objectName);

    /// <summary>Registra uma passagem que reencontrou sem gravar.</summary>
    public static void Skipped(string site, string objectName) => _current?.Skipped(site, objectName);

    private sealed class Scope : IDisposable
    {
        private readonly B111CallSiteLog? _previous;
        private bool _disposed;

        public Scope(B111CallSiteLog? previous) => _previous = previous;

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

/// <summary>
/// Coleção ordenada das observações de <see cref="B111CallSiteProbe"/>. Pública apenas para
/// permitir teste offline por <c>Add-Type</c>, como <c>ApiPlanScanTelemetry</c>.
/// </summary>
public sealed class B111CallSiteLog
{
    private readonly List<string> _sequence = new List<string>();
    private readonly Dictionary<string, int> _entries = new Dictionary<string, int>(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _writes = new Dictionary<string, int>(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _skips = new Dictionary<string, int>(StringComparer.Ordinal);

    public void Enter(string site)
    {
        Increment(_entries, site);
        _sequence.Add("entra  " + site);
    }

    public void Wrote(string site, string objectName)
    {
        Increment(_writes, site);
        _sequence.Add("GRAVA  " + site + " -> " + objectName);
    }

    public void Skipped(string site, string objectName)
    {
        Increment(_skips, site);
        _sequence.Add("skip   " + site + " -> " + objectName);
    }

    /// <summary>Linhas prontas para a Output. Vazio quando nada foi observado.</summary>
    public IReadOnlyList<string> Render()
    {
        if (_entries.Count == 0 && _writes.Count == 0 && _skips.Count == 0)
        {
            return Array.Empty<string>();
        }

        var lines = new List<string> { "== B111/F1 — chamadas e gravações por ponto ==" };

        var sites = _entries.Keys
            .Concat(_writes.Keys)
            .Concat(_skips.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(item => item, StringComparer.Ordinal);

        foreach (var site in sites)
        {
            lines.Add(string.Format(
                CultureInfo.InvariantCulture,
                "  {0}: entradas={1} gravacoes={2} reencontros-sem-gravar={3}",
                site,
                Value(_entries, site),
                Value(_writes, site),
                Value(_skips, site)));
        }

        lines.Add("  -- sequencia observada --");
        for (var i = 0; i < _sequence.Count; i++)
        {
            lines.Add(string.Format(CultureInfo.InvariantCulture, "  {0,3}. {1}", i + 1, _sequence[i]));
        }

        return lines;
    }

    private static void Increment(IDictionary<string, int> counters, string key)
    {
        counters.TryGetValue(key, out var current);
        counters[key] = current + 1;
    }

    private static int Value(IDictionary<string, int> counters, string key)
    {
        counters.TryGetValue(key, out var value);
        return value;
    }
}
