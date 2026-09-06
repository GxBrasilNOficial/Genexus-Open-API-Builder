#nullable enable

using System;
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

        index = ApiPlanKbObjectNameIndex.Create(designModel);
        var apiMatches = index.FindApis(apiPlan.ApiName);
        if (apiMatches.Count != 1)
        {
            detail = $"API Object '{apiPlan.ApiName}' encontrado {apiMatches.Count} vez(es); esperado exatamente 1.";
            return false;
        }

        var metadataMatches = index.FindFiles(apiPlan.MetadataFileName);
        if (metadataMatches.Count != 0)
        {
            detail = $"File '{apiPlan.MetadataFileName}' encontrado {metadataMatches.Count} vez(es); recuperação órfã não se aplica.";
            return false;
        }

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
