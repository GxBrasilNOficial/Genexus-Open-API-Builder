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

        var state = ApiPlanGenerationStateReader.Read(designModel, apiPlan);
        var blockedStages = new[]
            {
                requireSdts ? state.Sdts : null,
                requireProcedures ? state.Procedures : null,
                requireApiObject ? state.ApiObject : null,
                requireMetadataFile ? state.MetadataFile : null,
            }
            .Where(stage => stage is not null)
            .Cast<ApiPlanGenerationStageState>()
            .Where(stage => stage.IsBlocked)
            .Select(stage => stage.StageName)
            .ToArray();

        if (blockedStages.Length == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            "B063/B064/B067 bloqueado antes do primeiro Save(): foram detectadas colisao(oes) externa(s), incompativel(is), ambigua(s) ou metadata de integridade divergente em " +
            string.Join(", ", blockedStages) +
            ". Nenhum objeto planejado foi criado, alterado ou recebeu sufixo _v2.");
    }
}
