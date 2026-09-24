#nullable enable

using System;
using System.Linq;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;
using GenexusOpenApiBuilder.Extension.Domain;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// B120 A2 — remove o SDT envelope <c>*_API_ListResponse</c> que deixou de fazer parte do plano
/// depois que Procedure/API passaram a outs flat. Só apaga se o nome for o canônico do plano,
/// houver exatamente um match e a Description indicar posse da extensão.
/// <para>
/// Não usa <c>Persist(..., operationKind=Delete)</c>: o diário do Apply não admite
/// <c>inventory[].action=Delete</c> (schema V1). Confirma ausência só por leitura corrente
/// (<c>SDT.GetAll</c>), nunca pelo índice — o índice fica stale após <c>Delete()</c> e gerava
/// falso "ainda presente" (campo 2026-09-24, <c>apiTeste</c>).
/// </para>
/// </summary>
internal static class ApiPlanListResponseOrphanCleanup
{
    internal static string? TryDeleteOwnedOrphan(
        KBModel model,
        ApiPlan plan,
        ApiPlanKbObjectNameIndex kbIndex,
        ApiPlanBusyProgressSession? progress)
    {
        if (model is null) throw new ArgumentNullException(nameof(model));
        if (plan is null) throw new ArgumentNullException(nameof(plan));
        if (kbIndex is null) throw new ArgumentNullException(nameof(kbIndex));

        var orphanName = plan.ListResponseSdtName;
        if (string.IsNullOrWhiteSpace(orphanName))
        {
            return null;
        }

        var stillPlanned = ApiPlanSdtGenerationPlanBuilder.Create(plan).OwnSdts
            .Any(definition => string.Equals(definition.Name, orphanName, StringComparison.OrdinalIgnoreCase));
        if (stillPlanned)
        {
            return null;
        }

        progress?.Report("List", 0, 0, "Removendo SDT ListResponse órfão");
        progress?.PumpAndThrowIfAbortRequested();

        var matches = kbIndex.FindSdts(orphanName).ToArray();
        if (matches.Length == 0)
        {
            // Índice vazio: conferir KB ao vivo (órfão pode existir só no GetAll).
            matches = SDT.GetAll(model)
                .Where(item => string.Equals(item.Name, orphanName, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matches.Length == 0)
            {
                return null;
            }
        }

        if (matches.Length != 1)
        {
            throw new InvalidOperationException(
                "B120 bloqueado: SDT ListResponse órfão '" + orphanName +
                "' não é único na KB (" + matches.Length + " matches). Nenhuma exclusão foi feita.");
        }

        var sdt = matches[0];
        if (!ApiPlanOwnedObjectDescription.IsOwnedSdt(sdt.Description, orphanName))
        {
            throw new InvalidOperationException(
                "B120 bloqueado: SDT '" + orphanName +
                "' existe mas não tem Description de posse da extensão. Nenhuma exclusão foi feita.");
        }

        var guid = sdt.Guid;

        try
        {
            sdt.Delete();
        }
        catch (Exception exception)
        {
            // Delete pode lançar mesmo quando o objeto já saiu; a releitura decide.
            if (!SdtStillPresent(model, guid))
            {
                kbIndex.ForgetRemovedSdt(guid);
                return orphanName;
            }

            throw new InvalidOperationException(
                "B120 bloqueado: Delete do SDT ListResponse órfão '" + orphanName +
                "' falhou e o objeto permanece na KB: " + exception.Message,
                exception);
        }

        if (SdtStillPresent(model, guid))
        {
            throw new InvalidOperationException(
                "B120 bloqueado: exclusão do SDT ListResponse órfão '" + orphanName +
                "' não confirmada (objeto ainda presente após Delete).");
        }

        kbIndex.ForgetRemovedSdt(guid);
        return orphanName;
    }

    private static bool SdtStillPresent(KBModel model, Guid objectGuid) =>
        SDT.GetAll(model).Any(item => item.Guid == objectGuid);
}
