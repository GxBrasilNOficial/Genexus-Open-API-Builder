#nullable enable

using System;
using System.Linq;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;
using Artech.Genexus.Common.Wiki;
using GenexusOpenApiBuilder.Extension.Domain;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Recuperação explícita de um API Object próprio que ficou sem o File de metadata
/// depois de uma aplicação parcial.
///
/// A recuperação não altera o API Object, Procedures ou SDTs. Ela só é elegível quando
/// o SDK encontra exatamente um API Object, nenhum File com o nome planejado e confirma
/// a posse pelo contrato gerenciado e pela Description própria. A confirmação visual ao
/// usuário é responsabilidade do chamador.
/// </summary>
internal static class ApiPlanOrphanMetadataRecovery
{
    public static bool TryPrepareOrphanMetadataRecovery(
        KBModel designModel,
        ApiPlan apiPlan,
        out ApiPlanKbObjectNameIndex index,
        out string detail)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        // B082: as duas perguntas de elegibilidade custam duas varreduras; o índice completo
        // custa sete, entre elas o Attribute.GetAll de ~1300 ms na KB grande. No caso comum
        // — não há órfã — o índice seria montado e descartado, então ele só nasce depois de
        // as duas perguntas passarem.
        var apiMatches = ApiPlanScanProbe.Scan(
            "API",
            "orphan-recovery-preflight",
            () => API.GetAll(designModel)
                .Where(item => string.Equals(item.Name, apiPlan.ApiName, StringComparison.OrdinalIgnoreCase))
                .ToArray());
        if (apiMatches.Length != 1)
        {
            index = null!;
            detail = $"API Object '{apiPlan.ApiName}' encontrado {apiMatches.Length} vez(es); esperado exatamente 1.";
            return false;
        }

        var metadataMatchCount = ApiPlanScanProbe.Scan(
            "File",
            "orphan-recovery-preflight",
            () => WikiFileKBObject.GetAll(designModel)
                .Count(item => string.Equals(item.Name, apiPlan.MetadataFileName, StringComparison.OrdinalIgnoreCase)));
        if (metadataMatchCount != 0)
        {
            index = null!;
            detail = $"File '{apiPlan.MetadataFileName}' encontrado {metadataMatchCount} vez(es); recuperação órfã não se aplica.";
            return false;
        }

        index = ApiPlanKbObjectNameIndex.Create(designModel);
        var apiObject = apiMatches[0];
        var ownership = ApiPlanApiObjectWriter.DiagnoseOwnership(designModel, index, apiPlan, apiObject);
        if (!ownership.IsOwned)
        {
            detail = $"API Object '{apiPlan.ApiName}' não foi confirmado como próprio: {ownership.ReasonText}";
            return false;
        }

        detail = $"API Object próprio confirmado por Description/contrato; metadata '{apiPlan.MetadataFileName}' ausente.";
        return true;
    }

    public static ApiPlanMetadataFileWriteResult Recover(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        ApiPlanKbObjectNameIndex index)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        if (index is null)
        {
            throw new ArgumentNullException(nameof(index));
        }

        var result = ApiPlanMetadataFileWriter.CreateOrReencounter(
            designModel,
            transaction,
            apiPlan,
            allowIntentionalContractRefresh: false,
            index);
        if (!string.Equals(result.Status, ApiPlanMetadataFileWriteStatus.Created, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Recuperação esperava criar metadata, mas o status foi '{result.Status}'.");
        }

        return result;
    }
}
