#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common;
using Artech.Genexus.Common.Objects;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// B111 — sonda de custo de gravação por tipo de objeto.
///
/// A sonda do diário mediu, em <c>WikiFileKBObject</c>, que um <c>Save()</c> sem alteração
/// pendente custa 0 ms e um <c>Save()</c> com alteração custa cerca de 1,3 s na KB grande,
/// independentemente do payload e do que mudou. Se esse custo por gravação valer também
/// para SDT, Procedure e API, então o número de Saves do pipeline — e não as varreduras
/// de catálogo — passa a ser a alavanca de desempenho do Apply.
///
/// Esta sonda mede, por tipo: a primeira gravação, um Save sem alteração, gravações
/// sucessivas com alteração trivial, e a exclusão. Cria os próprios objetos e os exclui.
///
/// <para>
/// <b>Os quatro tipos medidos são exatamente os que a extensão cria</b> — Folder, SDT,
/// Procedure e API. Transaction fica fora porque a extensão nunca cria uma: o único
/// <c>transaction.Save()</c> previsto no plano B111 incide sobre uma Transaction que já
/// existe, para habilitar Business Component. Medir a criação de uma Transaction mediria
/// uma operação que o pipeline não executa, e medir o caso real exigiria alterar um
/// objeto pré-existente da KB. Esse custo permanece não medido; o lugar natural para
/// obtê-lo é a validação na IDE da própria implementação, quando o Wizard executar a
/// habilitação de BC.
/// </para>
///
/// É sonda temporária: deve sair das três camadas de registro no fechamento da frente.
/// </summary>
internal static class B111SaveCostProbe
{
    private const string FolderName = "GoabB111SaveCostProbeFolder";
    private const string SdtName = "SdtGoabB111SaveCostProbe";
    private const string ProcedureName = "prcGoabB111SaveCostProbe";
    private const string ApiName = "apiGoabB111SaveCostProbe";
    private const string BaseDescription = "Gx Open API Builder B111 Save Cost Probe";

    /// <summary>Gravações sucessivas com alteração trivial, após a primeira.</summary>
    private const int UpdateRounds = 3;

    public static IReadOnlyList<string> Run(KBModel designModel)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        var lines = new List<string>
        {
            "== S5 — custo de gravação por tipo de objeto ==",
            "Tipos medidos: os quatro que a extensão cria. Transaction fica fora porque a extensão nunca cria uma; ela só altera Transaction existente para habilitar BC.",
        };

        if (!EnsureNoLeftovers(designModel, lines))
        {
            return lines;
        }

        MeasureFolder(designModel, lines);
        MeasureSdt(designModel, lines);
        MeasureProcedure(designModel, lines);
        MeasureApi(designModel, lines);

        lines.Add(string.Empty);
        return lines;
    }

    private static bool EnsureNoLeftovers(KBModel designModel, List<string> lines)
    {
        var leftovers = new List<string>();
        if (Folder.GetAll(designModel).Any(item => Matches(item.Name, FolderName)))
        {
            leftovers.Add(FolderName);
        }

        if (SDT.GetAll(designModel).Any(item => Matches(item.Name, SdtName)))
        {
            leftovers.Add(SdtName);
        }

        if (Procedure.GetAll(designModel).Any(item => Matches(item.Name, ProcedureName)))
        {
            leftovers.Add(ProcedureName);
        }

        if (API.GetAll(designModel).Any(item => Matches(item.Name, ApiName)))
        {
            leftovers.Add(ApiName);
        }

        if (leftovers.Count == 0)
        {
            return true;
        }

        lines.Add($"BLOQUEIO: sobraram objetos de sonda de execução anterior ({string.Join(", ", leftovers)}). Exclua-os antes de repetir. Nenhuma alteração foi feita.");
        return false;
    }

    private static bool Matches(string? name, string expected) =>
        string.Equals(name, expected, StringComparison.OrdinalIgnoreCase);

    // ---------- um tipo por vez ----------

    private static void MeasureFolder(KBModel designModel, List<string> lines)
    {
        Measure(
            lines,
            "Folder",
            create: () =>
            {
                var folder = new Folder(designModel, FolderName)
                {
                    Description = BaseDescription,
                };
                folder.Save();
                return folder;
            },
            touch: (obj, round) => ((Folder)obj).Description = BaseDescription + " u" + round,
            save: obj => ((Folder)obj).Save(),
            delete: obj => ((Folder)obj).Delete());
    }

    private static void MeasureSdt(KBModel designModel, List<string> lines)
    {
        Measure(
            lines,
            "SDT",
            create: () =>
            {
                var sdt = new SDT(designModel)
                {
                    Name = SdtName,
                    Description = BaseDescription,
                };

                // Um SDT sem estrutura não passa na validação do Save. O mínimo válido é
                // Root nomeado com pelo menos um item, como faz ApiPlanSdtWriter.ConfigureSdt.
                var root = sdt.SDTStructure.Root;
                root.Items.Clear();
                root.Name = SdtName;
                root.AddItem("ProbeField", eDBType.CHARACTER, 20, 0);

                sdt.Save();
                return sdt;
            },
            touch: (obj, round) => ((SDT)obj).Description = BaseDescription + " u" + round,
            save: obj => ((SDT)obj).Save(),
            delete: obj => ((SDT)obj).Delete());
    }

    private static void MeasureProcedure(KBModel designModel, List<string> lines)
    {
        Measure(
            lines,
            "Procedure",
            create: () =>
            {
                var procedure = new Procedure(designModel)
                {
                    Name = ProcedureName,
                    Description = BaseDescription,
                };
                procedure.Save();
                return procedure;
            },
            touch: (obj, round) => ((Procedure)obj).Description = BaseDescription + " u" + round,
            save: obj => ((Procedure)obj).Save(),
            delete: obj => ((Procedure)obj).Delete());
    }

    private static void MeasureApi(KBModel designModel, List<string> lines)
    {
        Measure(
            lines,
            "API",
            create: () =>
            {
                var api = API.Create(designModel);
                api.Name = ApiName;
                api.Description = BaseDescription;
                api.Save();
                return api;
            },
            touch: (obj, round) => ((API)obj).Description = BaseDescription + " u" + round,
            save: obj => ((API)obj).Save(),
            delete: obj => ((API)obj).Delete());
    }

    // ---------- roteiro comum de medição ----------

    private static void Measure(
        List<string> lines,
        string label,
        Func<KBObject> create,
        Action<KBObject, int> touch,
        Action<KBObject> save,
        Action<KBObject> delete)
    {
        KBObject? subject = null;
        var watch = new Stopwatch();

        try
        {
            watch.Restart();
            subject = create();
            watch.Stop();
            lines.Add(Format(label, "criar + 1º Save", watch.ElapsedMilliseconds));

            watch.Restart();
            save(subject);
            watch.Stop();
            lines.Add(Format(label, "Save() sem alteração", watch.ElapsedMilliseconds));

            for (var round = 1; round <= UpdateRounds; round++)
            {
                touch(subject, round);
                watch.Restart();
                save(subject);
                watch.Stop();
                lines.Add(Format(label, "Save() com alteração trivial #" + round, watch.ElapsedMilliseconds));
            }
        }
        catch (Exception ex)
        {
            lines.Add($"S5 [{label}]: FALHOU — {Describe(ex)}");
        }
        finally
        {
            if (subject is not null)
            {
                try
                {
                    watch.Restart();
                    delete(subject);
                    watch.Stop();
                    lines.Add(Format(label, "Delete()", watch.ElapsedMilliseconds));
                }
                catch (Exception ex)
                {
                    lines.Add($"S5 [{label}]: limpeza falhou — {Describe(ex)}. Exclua o objeto manualmente.");
                }
            }
        }
    }

    private static string Format(string label, string step, long elapsedMs) =>
        string.Format(CultureInfo.InvariantCulture, "S5 [{0}] {1}: {2} ms.", label, step, elapsedMs);

    private static string Describe(Exception ex) =>
        ex.InnerException is null ? ex.Message : $"{ex.Message} | Inner='{ex.InnerException.Message}'";
}
