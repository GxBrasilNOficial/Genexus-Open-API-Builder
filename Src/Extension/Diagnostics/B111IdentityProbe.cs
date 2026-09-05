#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// B111 — sonda de identidade e de capacidade da Description (planejamento da frente).
///
/// Responde duas perguntas que a análise estática do SDK não conseguiu responder, porque
/// os corpos de método de <c>Artech.Udm.Framework.Entity</c> na instalação vêm como stub:
///
/// 1. o <c>Guid</c> de um API Object já existe (e é estável) ANTES do primeiro Save, e é
///    atribuível pelo chamador? Disso depende o desenho de identidade planejada do plano;
/// 2. quanto texto a Description preserva num round-trip Save/reler? Disso depende se o
///    marcador de identidade cabe nela ou precisa de outro campo.
///
/// A sonda cria UM API Object de teste, mede e o exclui ao final. Não toca em nenhum
/// objeto pré-existente da KB e não participa de nenhum fluxo de Apply.
/// É diagnóstico temporário: deve sair das três camadas de registro no fechamento da frente.
/// </summary>
internal static class B111IdentityProbe
{
    private const string ProbeName = "apiGoabB111IdentityProbe";
    private const string AssignmentProbeName = "apiGoabB111GuidAssignProbe";
    private const string ProbeDescription = "Gx Open API Builder B111 Identity Probe";

    /// <summary>Tamanhos de Description medidos no round-trip, em caracteres.</summary>
    private static readonly int[] DescriptionSizes = { 120, 250, 500, 1000, 2000, 4000 };

    /// <summary>Marcador real proposto pela seção 4.7 do plano, para medir o caso concreto.</summary>
    private const string SampleMarker =
        "GOAB-B111-IDENTITY;TransactionGuid=00000000-0000-0000-0000-000000000000;ApplicationId=probe;ContractHash=0000000000000000000000000000000000000000000000000000000000000000";

    public static IReadOnlyList<string> Run(KBModel designModel)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        var lines = new List<string>();

        var existing = API.GetAll(designModel)
            .Where(api => string.Equals(api.Name, ProbeName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(api.Name, AssignmentProbeName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (existing.Length > 0)
        {
            var names = string.Join(", ", existing.Select(api => api.Name).Distinct());
            lines.Add($"BLOQUEIO: sobraram {existing.Length} API Object(s) de sonda anterior ({names}). Exclua-os antes de repetir. Nenhuma alteração foi feita.");
            return lines;
        }

        API? probe = null;
        try
        {
            probe = MeasureIdentity(designModel, lines);
            MeasureDescriptionCapacity(designModel, probe, lines);
        }
        catch (Exception ex)
        {
            lines.Add($"ERRO durante a sonda: {Describe(ex)}");
        }
        finally
        {
            CleanUp(designModel, probe, lines);
        }

        // Fora do try acima de propósito: a hipótese B109 é justamente provocar um erro,
        // e ela não pode impedir a limpeza do objeto principal nem as sondas seguintes.
        TestGuidAssignment(designModel, lines);

        return lines;
    }

    // ---------- S1: identidade ----------

    /// <summary>
    /// Mede o caminho recomendado: ler o Guid que o Create já atribuiu e nunca escrevê-lo.
    /// A atribuição por reflexão saiu daqui e virou teste isolado em <see cref="TestGuidAssignment"/>,
    /// depois de uma execução em que ela precedeu imediatamente um
    /// "Collection was modified; enumeration operation may not execute" que abortou a sonda.
    /// </summary>
    private static API MeasureIdentity(KBModel designModel, List<string> lines)
    {
        lines.Add("== S1 — identidade do API Object antes e depois do primeiro Save ==");

        var probe = API.Create(designModel);
        var guidOnCreate = ReadGuid(probe);
        lines.Add($"S1.1 após API.Create, antes de qualquer atribuição: Guid='{guidOnCreate}'");

        probe.Name = ProbeName;
        probe.Description = ProbeDescription;
        lines.Add($"S1.2 após Name/Description, ainda sem Save:       Guid='{ReadGuid(probe)}'");

        var watch = Stopwatch.StartNew();
        probe.Save();
        watch.Stop();
        var guidAfterSave = ReadGuid(probe);
        lines.Add($"S1.3 primeiro Save (sem atribuir Guid):           {watch.ElapsedMilliseconds} ms.");
        lines.Add($"S1.4 imediatamente após o Save:                   Guid='{guidAfterSave}'");
        lines.Add($"S1.5 Guid do Create sobreviveu ao Save sem mudar? {(string.Equals(guidOnCreate, guidAfterSave, StringComparison.OrdinalIgnoreCase) ? "SIM" : "NÃO")}");

        watch.Restart();
        var persisted = API.Get(designModel, probe.Guid);
        watch.Stop();
        lines.Add(persisted is null
            ? $"S1.6 API.Get pelo Guid pós-Save: NÃO reencontrou o objeto ({watch.ElapsedMilliseconds} ms)."
            : $"S1.6 API.Get pelo Guid pós-Save: reencontrou Name='{persisted.Name}' em {watch.ElapsedMilliseconds} ms.");

        lines.Add(string.Empty);
        return probe;
    }

    /// <summary>
    /// S1b — hipótese sobre B109. Numa execução na KB grande, a sequência
    /// "atribuir Guid por reflexão" → "Save" produziu
    /// <c>Collection was modified; enumeration operation may not execute</c>, o mesmo
    /// sintoma que B109 registra como intermitente na etapa de Business Component.
    ///
    /// O teste roda sobre um objeto próprio e descartável, isolado em try/catch, para
    /// que a hipótese possa ser exercitada sem derrubar o restante da sonda. Não é o
    /// caminho recomendado do plano: é uma tentativa de reproduzir o erro de propósito.
    /// </summary>
    private static void TestGuidAssignment(KBModel designModel, List<string> lines)
    {
        lines.Add("== S1b — hipótese B109: atribuir Guid antes do Save reproduz o erro? ==");

        API? subject = null;
        try
        {
            subject = API.Create(designModel);
            subject.Name = AssignmentProbeName;
            subject.Description = ProbeDescription + " (S1b)";

            var chosen = Guid.NewGuid();
            lines.Add(TrySetGuid(subject, chosen, out var accepted)
                ? $"S1b.1 atribuição de Guid '{chosen}': ACEITA; leitura devolveu '{accepted}'"
                : $"S1b.1 atribuição de Guid '{chosen}': RECUSADA — {accepted}");

            var watch = Stopwatch.StartNew();
            subject.Save();
            watch.Stop();
            lines.Add($"S1b.2 Save após atribuir Guid: concluído em {watch.ElapsedMilliseconds} ms, SEM erro.");

            var persisted = API.Get(designModel, subject.Guid);
            lines.Add(persisted is null
                ? "S1b.3 releitura pelo Guid atribuído: NÃO reencontrou."
                : $"S1b.3 releitura pelo Guid atribuído: reencontrou Name='{persisted.Name}'.");
        }
        catch (Exception ex)
        {
            lines.Add($"S1b.2 Save após atribuir Guid: FALHOU — {Describe(ex)}");
            lines.Add("S1b.RESULTADO: a hipótese se sustenta nesta execução — atribuir Guid antes do Save produziu erro.");
        }
        finally
        {
            CleanUpAssignmentSubject(designModel, subject, lines);
            lines.Add(string.Empty);
        }
    }

    private static void CleanUpAssignmentSubject(KBModel designModel, API? subject, List<string> lines)
    {
        if (subject is null)
        {
            return;
        }

        try
        {
            var stillThere = API.GetAll(designModel)
                .Where(api => string.Equals(api.Name, AssignmentProbeName, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            foreach (var leftover in stillThere)
            {
                leftover.Delete();
            }

            lines.Add(stillThere.Length == 0
                ? "S1b limpeza: nada foi persistido; nada a excluir."
                : $"S1b limpeza: {stillThere.Length} objeto(s) '{AssignmentProbeName}' excluído(s).");
        }
        catch (Exception ex)
        {
            lines.Add($"S1b limpeza: falhou — {Describe(ex)}. Exclua '{AssignmentProbeName}' manualmente se existir.");
        }
    }

    private static bool TrySetGuid(API probe, Guid value, out string detail)
    {
        try
        {
            var property = probe.GetType().GetProperty(
                "Guid",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property is null || !property.CanWrite)
            {
                detail = "a propriedade Guid não expõe setter alcançável por reflexão.";
                return false;
            }

            property.SetValue(probe, value);
            detail = ReadGuid(probe);
            return true;
        }
        catch (Exception ex)
        {
            detail = Describe(ex);
            return false;
        }
    }

    // ---------- S2: capacidade da Description ----------

    private static void MeasureDescriptionCapacity(KBModel designModel, API probe, List<string> lines)
    {
        lines.Add("== S2 — round-trip da Description (Save + releitura) ==");

        foreach (var size in DescriptionSizes)
        {
            lines.Add(MeasureOneDescription(designModel, probe, BuildFiller(size), $"{size} chars"));
        }

        var withMarker = ProbeDescription + " " + SampleMarker;
        lines.Add(MeasureOneDescription(
            designModel,
            probe,
            withMarker,
            $"descrição canônica + marcador real ({withMarker.Length} chars)"));

        lines.Add(string.Empty);
    }

    private static string MeasureOneDescription(KBModel designModel, API probe, string value, string label)
    {
        try
        {
            probe.Description = value;
            var watch = Stopwatch.StartNew();
            probe.Save();
            watch.Stop();

            var persisted = API.Get(designModel, probe.Guid);
            var persistedValue = persisted?.Description ?? string.Empty;

            if (string.Equals(persistedValue, value, StringComparison.Ordinal))
            {
                return $"S2 [{label}]: PRESERVADA integralmente ({persistedValue.Length} chars relidos); Save em {watch.ElapsedMilliseconds} ms.";
            }

            return $"S2 [{label}]: DIVERGIU — gravados {value.Length}, relidos {persistedValue.Length}. "
                + $"Prefixo comum={CommonPrefixLength(value, persistedValue)} chars; Save em {watch.ElapsedMilliseconds} ms.";
        }
        catch (Exception ex)
        {
            return $"S2 [{label}]: FALHOU no Save — {Describe(ex)}";
        }
    }

    private static string BuildFiller(int size)
    {
        // Conteúdo determinístico e não repetitivo, para que um truncamento seja localizável.
        var builder = new StringBuilder(size);
        var block = 0;
        while (builder.Length < size)
        {
            var marker = block.ToString("D4", CultureInfo.InvariantCulture) + "-abcdefghij-";
            builder.Append(marker, 0, Math.Min(marker.Length, size - builder.Length));
            block++;
        }

        return builder.ToString();
    }

    private static int CommonPrefixLength(string left, string right)
    {
        var max = Math.Min(left.Length, right.Length);
        var i = 0;
        while (i < max && left[i] == right[i])
        {
            i++;
        }

        return i;
    }

    // ---------- limpeza ----------

    private static void CleanUp(KBModel designModel, API? probe, List<string> lines)
    {
        if (probe is null)
        {
            lines.Add("Limpeza: nenhum objeto de sonda chegou a ser criado.");
            return;
        }

        try
        {
            var probeGuid = probe.Guid;
            probe.Delete();

            var stillExists = API.GetAll(designModel).Any(api => api.Guid == probeGuid);
            lines.Add(stillExists
                ? $"Limpeza: a exclusão do API '{ProbeName}' NÃO foi confirmada. Exclua-o manualmente (Guid='{probeGuid}')."
                : $"Limpeza: API '{ProbeName}' excluído e ausência confirmada (Guid='{probeGuid}').");
        }
        catch (Exception ex)
        {
            lines.Add($"Limpeza: falhou ao excluir o API '{ProbeName}' — {Describe(ex)}. Exclua-o manualmente.");
        }
    }

    private static string ReadGuid(API probe)
    {
        try
        {
            return probe.Guid.ToString();
        }
        catch (Exception ex)
        {
            return "<erro: " + Describe(ex) + ">";
        }
    }

    private static string Describe(Exception ex) =>
        ex.InnerException is null ? ex.Message : $"{ex.Message} | Inner='{ex.InnerException.Message}'";
}
