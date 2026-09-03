using System;
using System.Collections.Generic;
using System.Linq;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;
using GenexusOpenApiBuilder.Extension.Domain;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

internal static class ApiPlanWritePreflight
{
    /// <summary>
    /// Preflight do B085: exige objetos proprios e baseline intacto, mas
    /// permite divergencia intencional do contrato planejado.
    /// </summary>
    public static void ValidateForSync(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        ApiPlanKbObjectNameIndex kbIndex)
    {
        ValidateForIntentionalChange(designModel, transaction, apiPlan, true, true, true, true, "B085", kbIndex);
    }

    public static void ValidateForIntentionalChange(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        bool requireSdts,
        bool requireProcedures,
        bool requireApiObject,
        bool requireMetadataFile,
        ApiPlanKbObjectNameIndex kbIndex)
    {
        ValidateForIntentionalChange(
            designModel,
            transaction,
            apiPlan,
            requireSdts,
            requireProcedures,
            requireApiObject,
            requireMetadataFile,
            "B063/B064/B067",
            kbIndex);
    }

    private static void ValidateForIntentionalChange(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        bool requireSdts,
        bool requireProcedures,
        bool requireApiObject,
        bool requireMetadataFile,
        string operationCode,
        ApiPlanKbObjectNameIndex kbIndex)
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

        if (kbIndex is null)
        {
            throw new ArgumentNullException(nameof(kbIndex));
        }

        if (!string.Equals(transaction.Name, apiPlan.TransactionName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{operationCode} bloqueado: o ApiPlan em memoria nao pertence a Transaction selecionada atual. Nenhuma alteracao foi feita.");
        }

        ApiPlanHierarchicalContractMapBuilder.ValidateStructuralSublevelNames(apiPlan);

        var state = ApiPlanGenerationStateReader.ReadUsingExistingIndex(
            designModel,
            transaction,
            apiPlan,
            forSyncContractRefresh: true,
            kbIndex);
        var scope = ApiPlanWritePreflightScope.FromRequirements(requireSdts, requireProcedures, requireApiObject, requireMetadataFile);
        var blocked = scope.SelectBlockedStageNames(new[]
            {
                ToStageBlock(ApiPlanWritePreflightStageKind.Sdts, state.Sdts),
                ToStageBlock(ApiPlanWritePreflightStageKind.Procedures, state.Procedures),
                ToStageBlock(ApiPlanWritePreflightStageKind.ApiObject, state.ApiObject),
                ToStageBlock(ApiPlanWritePreflightStageKind.MetadataFile, state.MetadataFile),
            });
        if (blocked.Length == 0)
        {
            return;
        }

        var collisions = state.CollectCollisionConflicts(requireSdts, requireProcedures, requireApiObject, requireMetadataFile);
        throw new InvalidOperationException(BuildBlockedMessage(
            $"{operationCode} bloqueado antes do primeiro Save(): baseline da extensao ou objetos proprios ausentes, externos ou ambiguos em ",
            blocked,
            collisions,
            ". Nenhum objeto planejado foi criado ou alterado."));
    }

    private static string BuildBlockedMessage(
        string prefix,
        IReadOnlyList<string> blockedStages,
        IReadOnlyList<ApiPlanCollisionConflict> collisions,
        string suffix)
    {
        var message = prefix + string.Join(", ", blockedStages);
        if (collisions.Count > 0)
        {
            message += ". " + ApiPlanCollisionConflict.FormatList(collisions);
        }

        return message + suffix;
    }

    private static ApiPlanWritePreflightStageBlock ToStageBlock(ApiPlanWritePreflightStageKind stageKind, ApiPlanGenerationStageState stage)
    {
        return new ApiPlanWritePreflightStageBlock(stageKind, stage.StageName, stage.IsBlocked);
    }
}
