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

    internal static void ValidateForF1(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        bool generateSdts,
        bool generateProcedures,
        bool generateApiObject,
        bool generateMetadata,
        bool applyList,
        bool applyBusinessComponent,
        ApiPlanKbObjectNameIndex kbIndex,
        IReadOnlyCollection<string>? preserveSdtNames = null)
    {
        if (designModel is null) throw new ArgumentNullException(nameof(designModel));
        if (transaction is null) throw new ArgumentNullException(nameof(transaction));
        if (apiPlan is null) throw new ArgumentNullException(nameof(apiPlan));
        if (kbIndex is null) throw new ArgumentNullException(nameof(kbIndex));
        ValidateTransactionIdentity(transaction, apiPlan, "B111/F1");

        var requiresConsumersOrApi = generateApiObject || generateMetadata || applyList || applyBusinessComponent;
        if (!generateApiObject && (generateMetadata || applyList || applyBusinessComponent))
        {
            ApiPlanApiObjectWriter.PreflightExistingApiObjectStrict(
                designModel,
                apiPlan,
                allowIntentionalContractRefresh: true,
                kbIndex: kbIndex);
        }

        if (!generateSdts && requiresConsumersOrApi)
        {
            ApiPlanSdtWriter.PreflightStrict(designModel, transaction, apiPlan, kbIndex, preserveSdtNames);
        }

        if (!generateProcedures && requiresConsumersOrApi)
        {
            ApiPlanApiObjectWriter.PreflightRequiredProceduresStrict(designModel, apiPlan);
        }
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

        ValidateTransactionIdentity(transaction, apiPlan, operationCode);

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

    internal static void ValidateTransactionIdentity(Transaction transaction, ApiPlan apiPlan, string operationCode)
    {
        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        if (operationCode is null)
        {
            throw new ArgumentNullException(nameof(operationCode));
        }

        if (!ApiPlanTransactionIdentity.Matches(
                transaction.Guid,
                transaction.Name,
                apiPlan.TransactionGuid,
                apiPlan.TransactionName))
        {
            throw new InvalidOperationException(ApiPlanTransactionIdentity.BuildMismatchMessage(
                operationCode,
                transaction.Guid,
                transaction.Name,
                apiPlan.TransactionGuid,
                apiPlan.TransactionName));
        }
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
