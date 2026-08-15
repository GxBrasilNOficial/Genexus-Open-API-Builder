using System;
using System.Collections.Generic;
using System.Linq;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;
using GenexusOpenApiBuilder.Extension.Domain;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

internal static class ApiPlanWritePreflight
{
    public static void Validate(KBModel designModel, Transaction transaction, ApiPlan apiPlan)
    {
        Validate(designModel, transaction, apiPlan, true, true, true, true);
    }

    public static void Validate(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        bool requireSdts,
        bool requireProcedures,
        bool requireApiObject,
        bool requireMetadataFile)
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

        if (!string.Equals(transaction.Name, apiPlan.TransactionName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("B063/B064 bloqueado: o ApiPlan em memoria nao pertence a Transaction selecionada atual. Nenhuma alteracao foi feita.");
        }

        var state = ApiPlanGenerationStateReader.Read(designModel, transaction, apiPlan);
        var scope = ApiPlanWritePreflightScope.FromRequirements(requireSdts, requireProcedures, requireApiObject, requireMetadataFile);
        var blockedStages = scope.SelectBlockedStageNames(new[]
        {
            ToStageBlock(ApiPlanWritePreflightStageKind.Sdts, state.Sdts),
            ToStageBlock(ApiPlanWritePreflightStageKind.Procedures, state.Procedures),
            ToStageBlock(ApiPlanWritePreflightStageKind.ApiObject, state.ApiObject),
            ToStageBlock(ApiPlanWritePreflightStageKind.MetadataFile, state.MetadataFile),
        });

        if (blockedStages.Length == 0)
        {
            return;
        }

        var collisions = state.CollectCollisionConflicts(requireSdts, requireProcedures, requireApiObject, requireMetadataFile);
        throw new InvalidOperationException(BuildBlockedMessage(
            "B063/B064/B067 bloqueado antes do primeiro Save(): foram detectadas colisao(oes) externa(s), incompativel(is), ambigua(s) ou metadata de integridade divergente em ",
            blockedStages,
            collisions,
            ". Nenhum objeto planejado foi criado, alterado ou recebeu sufixo _v2."));
    }

    /// <summary>
    /// Preflight do B085: exige objetos proprios e baseline intacto, mas
    /// permite divergencia intencional do contrato planejado.
    /// </summary>
    public static void ValidateForSync(KBModel designModel, Transaction transaction, ApiPlan apiPlan)
    {
        ValidateForIntentionalChange(designModel, transaction, apiPlan, true, true, true, true, "B085");
    }

    public static void ValidateForIntentionalChange(KBModel designModel, Transaction transaction, ApiPlan apiPlan)
    {
        ValidateForIntentionalChange(designModel, transaction, apiPlan, true, true, true, true);
    }

    public static void ValidateForIntentionalChange(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        bool requireSdts,
        bool requireProcedures,
        bool requireApiObject,
        bool requireMetadataFile)
    {
        ValidateForIntentionalChange(
            designModel,
            transaction,
            apiPlan,
            requireSdts,
            requireProcedures,
            requireApiObject,
            requireMetadataFile,
            "B063/B064/B067");
    }

    private static void ValidateForIntentionalChange(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        bool requireSdts,
        bool requireProcedures,
        bool requireApiObject,
        bool requireMetadataFile,
        string operationCode)
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

        if (!string.Equals(transaction.Name, apiPlan.TransactionName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{operationCode} bloqueado: o ApiPlan em memoria nao pertence a Transaction selecionada atual. Nenhuma alteracao foi feita.");
        }

        var state = ApiPlanGenerationStateReader.ReadForIntentionalChange(designModel, transaction, apiPlan);
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
