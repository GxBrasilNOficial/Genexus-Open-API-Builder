using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Artech.Architecture.Common.Descriptors;
using Artech.Architecture.Common.Helpers;
using Artech.Architecture.Common.Objects;
using Artech.Architecture.Common.Packages;
using Artech.Architecture.Common.Services;
using Artech.Architecture.UI.Framework.Packages;
using Artech.Architecture.UI.Framework.Services;
using Artech.Common.Framework.Commands;
using Artech.Genexus.Common.Objects;
using GenexusOpenApiBuilder.Extension.Diagnostics;
using GenexusOpenApiBuilder.Extension.Domain;

[assembly: Package(typeof(GenexusOpenApiBuilder.Extension.Package))]

namespace GenexusOpenApiBuilder.Extension;

/// <summary>
/// Ponto de entrada da extensão. As sondas B001-B006 permanecem como
/// evidências históricas e não são invocadas em runtime nem na abertura de KBs.
/// O menu principal expõe preferências, Wizard, Sincronizar com a Transaction e Remover API gerada (nesta ordem);
/// o contexto da Transaction expõe Wizard, Sincronizar com a Transaction e Remover API gerada.
/// </summary>
public sealed class Package : AbstractPackageUI
{
    public override string Name => "Genexus Open API Builder";

    public override void Initialize(IGxServiceProvider services)
    {
        base.Initialize(services);

        AddCommand(new CommandKey(Id, "Configurar Preferências do Wizard"), ExecuteConfigureWizardPreferences, QueryConfigureWizardPreferencesPortuguese);
        AddCommand(new CommandKey(Id, "Configurar preferencias del Wizard"), ExecuteConfigureWizardPreferences, QueryConfigureWizardPreferencesSpanish);
        AddCommand(new CommandKey(Id, "Configure Wizard Preferences"), ExecuteConfigureWizardPreferences, QueryConfigureWizardPreferencesEnglish);
        AddCommand(new CommandKey(Id, "Wizard"), ExecuteOpenWizardStepOne, QueryOpenWizardStepOne);
        AddCommand(new CommandKey(Id, "Sincronizar com a Transaction"), ExecuteSynchronizeWithTransaction, QuerySynchronizeWithTransactionPortuguese);
        AddCommand(new CommandKey(Id, "Sincronizar con la Transaction"), ExecuteSynchronizeWithTransaction, QuerySynchronizeWithTransactionSpanish);
        AddCommand(new CommandKey(Id, "Synchronize with the Transaction"), ExecuteSynchronizeWithTransaction, QuerySynchronizeWithTransactionEnglish);
        AddCommand(new CommandKey(Id, "Remover API gerada"), ExecuteRemoveGeneratedApi, QueryRemoveGeneratedApiPortuguese);
        AddCommand(new CommandKey(Id, "Eliminar API generada"), ExecuteRemoveGeneratedApi, QueryRemoveGeneratedApiSpanish);
        AddCommand(new CommandKey(Id, "Remove generated API"), ExecuteRemoveGeneratedApi, QueryRemoveGeneratedApiEnglish);
        AddCommand(new CommandKey(Id, "Recuperar operação interrompida"), ExecuteRecoverInterruptedOperation, QueryRecoverInterruptedOperationPortuguese);
        AddCommand(new CommandKey(Id, "Recuperar operación interrumpida"), ExecuteRecoverInterruptedOperation, QueryRecoverInterruptedOperationSpanish);
        AddCommand(new CommandKey(Id, "Recover interrupted operation"), ExecuteRecoverInterruptedOperation, QueryRecoverInterruptedOperationEnglish);

    }

    private static bool QueryConfigureWizardPreferencesPortuguese(CommandData data, ref CommandStatus status)
    {
        return QueryLocalizedCommand(data, ref status, ExtensionLanguage.PortugueseBrazil);
    }

    private static bool QueryConfigureWizardPreferencesSpanish(CommandData data, ref CommandStatus status)
    {
        return QueryLocalizedCommand(data, ref status, ExtensionLanguage.Spanish);
    }

    private static bool QueryConfigureWizardPreferencesEnglish(CommandData data, ref CommandStatus status)
    {
        return QueryLocalizedCommand(data, ref status, ExtensionLanguage.English);
    }

    private static bool QueryLocalizedCommand(CommandData data, ref CommandStatus status, ExtensionLanguage language)
    {
        status.Visible(ExtensionLocalization.IsCurrentKnowledgeBase(language));
        return true;
    }

    private static bool ExecuteConfigureWizardPreferences(CommandData data)
    {
        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        if (knowledgeBase is null)
        {
            WriteOutput("[Genexus Open API Builder][Prefs] Nenhuma Knowledge Base ativa foi encontrada. Abra uma KB e execute o comando novamente.");
            return true;
        }

        var texts = ExtensionLocalization.For(knowledgeBase);
        var loadResult = PrototypeWizardPreferencesStore.Load(knowledgeBase.DesignModel);
        // Sem owner, o CenterParent do diálogo não tem pai e o WinForms centraliza na tela
        // primária — a janela abria em outro monitor que não o da IDE. O Wizard, o quadro de
        // progresso e o Sync já ancoravam; só as Preferências tinham ficado de fora.
        var preferencesOwner = ExtensionIdeScreenPlacement.ResolveOwner();
        using var dialog = new PrototypeWizardPreferencesDialog(loadResult.Preferences, loadResult.Status, texts);
        ExtensionIdeScreenPlacement.CenterOnIdeScreen(dialog, preferencesOwner);
        var result = preferencesOwner is null
            ? dialog.ShowDialog()
            : dialog.ShowDialog(preferencesOwner);
        if (result != System.Windows.Forms.DialogResult.OK || dialog.Preferences is null)
        {
            WriteOutput("[Genexus Open API Builder][Prefs] Configuracao de preferencias do wizard cancelada. Nenhuma alteracao foi feita na KB.");
            return true;
        }

        try
        {
            var saveResult = PrototypeWizardPreferencesStore.Save(knowledgeBase.DesignModel, dialog.Preferences);
            var statusText = saveResult.Created ? "Created" : "Updated";
            WriteOutput($"[Genexus Open API Builder][Prefs] Preferencias do wizard gravadas na KB ativa: File='{saveResult.FileName}', Status='{statusText}', Guid='{saveResult.Guid}', Bytes={saveResult.Bytes}. O proximo wizard aplicara esses defaults quando a etapa estiver habilitada pelo estado da KB.");
        }
        catch (Exception ex)
        {
            var errorDetail = ex.InnerException is null ? ex.Message : $"{ex.Message} | Inner='{ex.InnerException.Message}'";
            // B109: sonda temporaria - publica a stack completa, que o log de uma linha descarta.
            foreach (var b109Line in B109ExceptionProbe.Describe(ex, "Preferências do Wizard")) { WriteOutput("[Genexus Open API Builder]" + b109Line); }
            WriteOutput($"[Genexus Open API Builder][Prefs] Gravacao de preferencias bloqueada ou falhou antes de concluir: Error='{errorDetail}'");
        }

        return true;
    }

    private static bool TryCreateSdts(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        string triggerSource,
        ApiPlanKbObjectNameIndex kbIndex,
        IReadOnlyCollection<string>? preserveSdtNames = null,
        ApiPlanApplicationFinalReportCollector? report = null,
        ApiPlanBusyProgressSession? progress = null)
    {
        if (kbIndex is null)
        {
            throw new ArgumentNullException(nameof(kbIndex));
        }

        try
        {
            Action<ApiPlanSdtWriteItemResult> onSdtWrite = item =>
            {
                AppendSdtWriteItemToReport(report, item);
                if (!string.IsNullOrWhiteSpace(item.StructureMismatch))
                {
                    WriteOutput($"[Genexus Open API Builder][B082] SDT diverge: Name='{item.Name}', Motivo='{item.StructureMismatch}'.");
                }
            };
            var result = preserveSdtNames is null
                ? ApiPlanSdtWriter.CreateOrReencounter(designModel, transaction, apiPlan, preserveSdtNames: null, kbIndex: kbIndex, onSdtWrite: onSdtWrite, progress: progress)
                : ApiPlanSdtWriter.CreateOrReencounter(designModel, transaction, apiPlan, preserveSdtNames, kbIndex, onSdtWrite, progress);
            WriteOutput($"[Genexus Open API Builder][B040-B046] Escrita de SDTs concluida: Transaction='{transaction.Name}', Trigger='{triggerSource}', PlannedOwnSdts={result.PlannedOwnSdts}, PlannedSharedSdts={result.PlannedSharedSdts}, Created={result.CreatedSdts}, Reencountered={result.ReencounteredSdts}, TransactionFolder='{result.TransactionFolderName}', TransactionFolderGuid='{result.TransactionFolderGuid}'. Nenhuma Procedure, API Object ou metadata persistente definitiva foi criada.");

            return true;
        }
        catch (ApiPlanBusyAbortedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            WriteOutput($"[Genexus Open API Builder][B040-B046] Criacao de SDTs bloqueada por preflight ou falhou antes de concluir: Trigger='{triggerSource}', Error='{ex.Message}'");
            report?.AddBlocked("SDTs", "B040-B046", ex.Message);
            return false;
        }
    }

    private static bool TryCreateProcedures(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        string triggerSource,
        ApiPlanKbObjectNameIndex kbIndex,
        ApiPlanApplicationFinalReportCollector? report = null,
        ApiPlanBusyProgressSession? progress = null)
    {
        if (kbIndex is null)
        {
            throw new ArgumentNullException(nameof(kbIndex));
        }

        try
        {
            var result = ApiPlanProcedureWriter.CreateOrReencounter(designModel, transaction, apiPlan, kbIndex, progress);
            var procedureStage = ApiPlanProcedureWriter.FormatOutputStage(apiPlan);
            WriteOutput($"[Genexus Open API Builder][{procedureStage}] Escrita de Procedures concluida: Transaction='{transaction.Name}', Trigger='{triggerSource}', PlannedProcedures={result.PlannedProcedures}, ReencounteredSdts={result.ReencounteredSdts}, Created={result.CreatedProcedures}, Reencountered={result.ReencounteredProcedures}, TransactionFolder='{result.TransactionFolderName}', TransactionFolderGuid='{result.TransactionFolderGuid}'. Nenhum API Object, REST completo ou metadata persistente definitiva foi criado.");
            foreach (var item in result.Items)
            {
                report?.AddFromWriteStatus("Procedure", item.Name, item.Status, item.ServiceName);
            }

            return true;
        }
        catch (ApiPlanBusyAbortedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            WriteOutput($"[Genexus Open API Builder][{ApiPlanProcedureWriter.FormatOutputStage(apiPlan)}] Criacao de Procedures bloqueada por preflight ou falhou antes de concluir: Trigger='{triggerSource}', Error='{ex.Message}'");
            report?.AddBlocked("Procedures", ApiPlanProcedureWriter.FormatOutputStage(apiPlan), ex.Message);
            return false;
        }
    }

    private static bool TryCreateApiObject(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        string triggerSource,
        ApiPlanKbObjectNameIndex kbIndex,
        ApiPlanBusyProgressSession? progress,
        ApiPlanApplicationFinalReportCollector? report = null,
        bool allowIntentionalContractRefresh = false)
    {
        try
        {
            report?.MarkApiSavePathEntered();
            var result = ApiPlanApiObjectWriter.CreateOrReencounter(
                designModel,
                transaction,
                apiPlan,
                allowIntentionalContractRefresh,
                kbIndex,
                progress,
                onApiSaveCompleted: guid => report?.SetPersistedMainObject(apiPlan.ApiName, guid),
                onApiPhysicalSave: guid =>
                {
                    report?.SetApiWriter("B054");
                    report?.RecordApiSave();
                },
                onApiSaveAttempted: () => report?.MarkApiSaveAttempted());
            WriteOutput($"[Genexus Open API Builder][B054] Escrita de API Object concluida: Transaction='{transaction.Name}', Trigger='{triggerSource}', ApiName='{result.ApiName}', Status='{result.Status}', ReencounteredSdts={result.ReencounteredSdts}, ReencounteredProcedures={result.ReencounteredProcedures}, PlannedServices={result.PlannedServices}, TransactionFolder='{result.TransactionFolderName}', TransactionFolderGuid='{result.TransactionFolderGuid}'. Nenhum REST completo, seguranca definitiva ou metadata persistente definitiva foi criado.");
            foreach (var procedure in result.Procedures)
            {
                WriteOutput($"[Genexus Open API Builder][B054] Procedure reencontrada para API Object: Backlog='{procedure.BacklogId}', Service='{procedure.ServiceName}', Name='{procedure.Name}', Guid='{procedure.Guid}'.");
            }

            WriteOutput($"[Genexus Open API Builder][B054] API Object {result.Status}: Name='{result.ApiName}', Guid='{result.Guid}'.");
            WriteOutput($"[Genexus Open API Builder][B056] Descricoes aplicadas no API Object real: Transaction='{transaction.Name}', Trigger='{triggerSource}', ApiName='{result.ApiName}', DescribedServices={apiPlan.ServiceDescriptions.Count}. Sem antecipar REST completo, codigo HTTP, seguranca definitiva ou metadata persistente.");
            report?.AddFromWriteStatus("API Object", result.ApiName, result.Status);
            report?.SetPlannedApiName(result.ApiName);
            report?.SetApiName(result.ApiName);
            return true;
        }
        catch (ApiPlanBusyAbortedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            WriteOutput($"[Genexus Open API Builder][B054] Criacao de API Object bloqueada por preflight ou falhou antes de concluir: Trigger='{triggerSource}', Error='{ex.Message}'");
            report?.AddBlocked("API Object", apiPlan.ApiName, ex.Message);
            return false;
        }
    }

    private static bool TryPrepareApiObject(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        string triggerSource,
        ApiPlanKbObjectNameIndex kbIndex,
        bool allowIntentionalContractRefresh,
        bool persistApiObject,
        bool businessComponentParticipated,
        string finalWriter,
        ApiPlanBusyProgressSession? progress,
        ApiPlanApplicationFinalReportCollector? report,
        ApiPlanTransientApiSelection? selection,
        IReadOnlyCollection<string>? preserveSdtNames,
        out ApiPlanTransientApiContext? context)
    {
        context = null;
        try
        {
            context = ApiPlanApiObjectWriter.PrepareOrReencounter(
                designModel,
                transaction,
                apiPlan,
                allowIntentionalContractRefresh,
                kbIndex,
                persistApiObject,
                businessComponentParticipated,
                finalWriter,
                selection,
                preserveSdtNames,
                progress);
            report?.SetPlannedApiName(context.PlannedApiName);
            report?.SetApiName(context.PlannedApiName);
            WriteOutput($"[Genexus Open API Builder][B054] API Object preparado: Transaction='{transaction.Name}', Trigger='{triggerSource}', PlannedApiName='{context.PlannedApiName}', PlannedApiGuid='{context.PlannedApiGuid}', ApiWasCreated={context.ApiWasCreated}, PersistApiObject={context.PersistApiObject}, FinalWriter='{context.FinalWriter}'. Nenhum Save de API Object foi solicitado nesta etapa.");
            return true;
        }
        catch (ApiPlanBusyAbortedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            WriteOutput($"[Genexus Open API Builder][B054] Preparacao de API Object bloqueada por preflight ou falhou antes de concluir: Trigger='{triggerSource}', Error='{ex.Message}'");
            report?.AddBlocked("API Object", apiPlan.ApiName, ex.Message);
            return false;
        }
    }

    private static bool TryWriteMetadataFile(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        string triggerSource,
        ApiPlanKbObjectNameIndex kbIndex,
        bool allowIntentionalContractRefresh = false,
        ApiPlanApplicationFinalReportCollector? report = null)
    {
        if (kbIndex is null)
        {
            throw new ArgumentNullException(nameof(kbIndex));
        }

        try
        {
            var result = ApiPlanMetadataFileWriter.CreateOrReencounter(
                designModel,
                transaction,
                apiPlan,
                allowIntentionalContractRefresh,
                kbIndex);
            WriteOutput($"[Genexus Open API Builder][B060] Metadata persistente inicial gravada: Transaction='{transaction.Name}', Trigger='{triggerSource}', File='{result.FileName}', Status='{result.Status}', Guid='{result.Guid}', SchemaVersion='{result.SchemaVersion}', Bytes={result.Bytes}, Sha256='{result.Sha256}'. A metadata registra o snapshot do ApiPlan e dos artefatos ja aplicados; seguranca definitiva permanece fora desta etapa.");
            var baselineMessage = allowIntentionalContractRefresh
                ? "Alteracoes deliberadas pelo Wizard/Sincronizar atualizam esse baseline; alteracoes diretas nos objetos continuam bloqueadas antes de qualquer Save()."
                : "Reexecucoes com descricoes, ownership, Service Source ou baseline divergente serao bloqueadas antes de qualquer Save().";
            WriteOutput($"[Genexus Open API Builder][B067] Metadata de integridade gravada: Transaction='{transaction.Name}', Trigger='{triggerSource}', File='{result.FileName}', IntegrityVersion='{result.IntegrityVersion}', PlannedContractHash='{result.PlannedContractHash}'. {baselineMessage}");
            report?.AddFromWriteStatus("File", result.FileName, result.Status, $"Bytes={result.Bytes}");
            return true;
        }
        catch (Exception ex)
        {
            var errorDetail = ex.InnerException is null ? ex.Message : $"{ex.Message} | Inner='{ex.InnerException.Message}'";
            // B109: sonda temporaria - publica a stack completa, que o log de uma linha descarta.
            foreach (var b109Line in B109ExceptionProbe.Describe(ex, "Metadata File")) { WriteOutput("[Genexus Open API Builder]" + b109Line); }
            WriteOutput($"[Genexus Open API Builder][B060] Gravacao de metadata bloqueada por preflight ou falhou antes de concluir: Trigger='{triggerSource}', Error='{errorDetail}'");
            report?.AddBlocked("File", apiPlan.MetadataFileName, errorDetail);
            return false;
        }
    }

    private static bool TryApplyBusinessComponent(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        string triggerSource,
        ApiPlanKbObjectNameIndex kbIndex,
        bool allowIntentionalContractRefresh = false,
        IReadOnlyCollection<string>? preserveSdtNames = null,
        ApiPlanApplicationFinalReportCollector? report = null,
        ApiPlanBusyProgressSession? progress = null,
        ApiPlanTransientApiContext? apiContext = null)
    {
        if (kbIndex is null)
        {
            throw new ArgumentNullException(nameof(kbIndex));
        }

        var apiObjectSaveExpected = apiContext is null
            || (apiContext.PersistApiObject && string.Equals(apiContext.FinalWriter, "Business Component", StringComparison.Ordinal));
        try
        {
            if (apiObjectSaveExpected)
            {
                report?.MarkApiSavePathEntered();
            }

            var result = ApiPlanBusinessComponentWriter.Apply(
                designModel,
                transaction,
                apiPlan,
                allowIntentionalContractRefresh,
                preserveSdtNames,
                kbIndex,
                onSdtWrite: item => AppendSdtWriteItemToReport(report, item),
                progress: progress,
                apiContext: apiContext,
                onSaveCompleted: null,
                onApiSaveCompleted: guid => report?.SetPersistedMainObject(apiPlan.ApiName, guid),
                onApiPhysicalSave: guid =>
                {
                    report?.SetApiWriter("Business Component");
                    report?.RecordApiSave();
                },
                onApiSaveAttempted: () => report?.MarkApiSaveAttempted());
            var deleteGuidPart = result.DeleteProcedureGuid == Guid.Empty
                ? string.Empty
                : $", DeleteProcedureGuid='{result.DeleteProcedureGuid}'";
            var apiObjectSavedByBusinessComponent = apiObjectSaveExpected;
            var apiObjectStageStatus = apiObjectSavedByBusinessComponent ? "API Object sincronizado" : "API Object nao gravado nesta etapa";
            WriteOutput($"[Genexus Open API Builder][B071-B073/B079] REST via Business Component aplicado; {apiObjectStageStatus}: Transaction='{transaction.Name}', Trigger='{triggerSource}', GetProcedureGuid='{result.GetProcedureGuid}', CreateProcedureGuid='{result.CreateProcedureGuid}', UpdateProcedureGuid='{result.UpdateProcedureGuid}'{deleteGuidPart}, ApiObjectGuid='{result.ApiObjectGuid}', PrimaryKeyParts={result.PrimaryKeyParts}, CreateFields={result.CreateFields}, UpdateFields={result.UpdateFields}, ResponseFields={result.ResponseFields}. Status HTTP controlado por RestCode no API Object; ErrorResponse exposto como saida publica dos servicos; Location de Create emitido nativamente via HttpResponse.");
            var descriptionStageStatus = apiObjectSavedByBusinessComponent ? "Descricoes reaplicadas no API Object real" : "Descricoes do API Object nao reaplicadas nesta etapa";
            WriteOutput($"[Genexus Open API Builder][B056] {descriptionStageStatus} durante B071-B073/B079: Transaction='{transaction.Name}', Trigger='{triggerSource}', ApiObjectGuid='{result.ApiObjectGuid}', DescribedServices={apiPlan.ServiceDescriptions.Count}. Service Source preserva o contrato Procedure/API Object atual.");
            foreach (var procedureName in apiPlan.ProcedureNames.Where(name =>
                         name.EndsWith("_API_Get", StringComparison.OrdinalIgnoreCase)
                         || name.EndsWith("_API_Create", StringComparison.OrdinalIgnoreCase)
                         || name.EndsWith("_API_Update", StringComparison.OrdinalIgnoreCase)
                         || name.EndsWith("_API_Delete", StringComparison.OrdinalIgnoreCase)))
            {
                report?.AddUpdated("Procedure", procedureName, "Business Component");
            }

            report?.SetPlannedApiName(apiPlan.ApiName);
            if (apiContext is null || (apiContext.PersistApiObject && string.Equals(apiContext.FinalWriter, "Business Component", StringComparison.Ordinal)))
            {
                var status = apiContext is not null && apiContext.ApiWasCreated ? "Created" : "Updated";
                report?.AddFromWriteStatus("API Object", apiPlan.ApiName, status, "Business Component");
            }
            report?.SetApiName(apiPlan.ApiName);
            return true;
        }
        catch (ApiPlanBusyAbortedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var errorDetail = ex.InnerException is null ? ex.Message : $"{ex.Message} | Inner='{ex.InnerException.Message}'";
            // B109: sonda temporaria - publica a stack completa, que o log de uma linha descarta.
            foreach (var b109Line in B109ExceptionProbe.Describe(ex, "Business Component")) { WriteOutput("[Genexus Open API Builder]" + b109Line); }
            WriteOutput($"[Genexus Open API Builder][B071-B073/B079] Aplicacao REST via Business Component bloqueada por preflight ou falhou antes de concluir: Trigger='{triggerSource}', Error='{errorDetail}'");
            report?.AddBlocked("Business Component", "REST", errorDetail);
            return false;
        }
    }

    private static bool TryApplyList(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        string triggerSource,
        ApiPlanKbObjectNameIndex kbIndex,
        bool allowIntentionalContractRefresh = false,
        IReadOnlyCollection<string>? preserveSdtNames = null,
        ApiPlanApplicationFinalReportCollector? report = null,
        ApiPlanBusyProgressSession? progress = null,
        ApiPlanTransientApiContext? apiContext = null)
    {
        var apiObjectSaveExpected = apiContext is null
            || (apiContext.PersistApiObject && string.Equals(apiContext.FinalWriter, "List", StringComparison.Ordinal));
        try
        {
            if (apiObjectSaveExpected)
            {
                report?.MarkApiSavePathEntered();
            }

            var result = ApiPlanListProcedureWriter.Apply(
                designModel,
                transaction,
                apiPlan,
                allowIntentionalContractRefresh,
                preserveSdtNames,
                kbIndex,
                onSdtWrite: item => AppendSdtWriteItemToReport(report, item),
                progress: progress,
                apiContext: apiContext,
                onSaveCompleted: null,
                onApiSaveCompleted: guid => report?.SetPersistedMainObject(apiPlan.ApiName, guid),
                onApiPhysicalSave: guid =>
                {
                    report?.SetApiWriter("List");
                    report?.RecordApiSave();
                },
                onApiSaveAttempted: () => report?.MarkApiSaveAttempted());
            var apiObjectSavedByList = apiObjectSaveExpected;
            var apiObjectStageStatus = apiObjectSavedByList ? "API Object sincronizado" : "API Object nao gravado nesta etapa";
            WriteOutput($"[Genexus Open API Builder][B070] List aplicado; {apiObjectStageStatus}: Transaction='{transaction.Name}', Trigger='{triggerSource}', ListProcedureGuid='{result.ListProcedureGuid}', ApiObjectGuid='{result.ApiObjectGuid}', Filters={result.Filters}, OrderParts={result.OrderParts}, DefaultPageSize={result.DefaultPageSize}, MaximumPageSize={result.MaximumPageSize}. B076 requer validacao HTTP em etapa separada.");
            var listProcedure = apiPlan.ProcedureNames.FirstOrDefault(name =>
                name.EndsWith("_API_List", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(listProcedure))
            {
                report?.AddUpdated("Procedure", listProcedure, "List");
            }

            report?.SetPlannedApiName(apiPlan.ApiName);
            if (apiContext is null || (apiContext.PersistApiObject && string.Equals(apiContext.FinalWriter, "List", StringComparison.Ordinal)))
            {
                var status = apiContext is not null && apiContext.ApiWasCreated ? "Created" : "Updated";
                report?.AddFromWriteStatus("API Object", apiPlan.ApiName, status, "List");
            }
            report?.SetApiName(apiPlan.ApiName);
            return true;
        }
        catch (ApiPlanBusyAbortedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var errorDetail = ex.InnerException is null ? ex.Message : $"{ex.Message} | Inner='{ex.InnerException.Message}'";
            // B109: sonda temporaria - publica a stack completa, que o log de uma linha descarta.
            foreach (var b109Line in B109ExceptionProbe.Describe(ex, "List")) { WriteOutput("[Genexus Open API Builder]" + b109Line); }
            WriteOutput($"[Genexus Open API Builder][B070] Aplicacao do List bloqueada por preflight ou falhou antes de concluir: Trigger='{triggerSource}', Error='{errorDetail}'");
            report?.AddBlocked("List", "B070", errorDetail);
            return false;
        }
    }

    private static bool QuerySynchronizeWithTransactionPortuguese(CommandData data, ref CommandStatus status)
    {
        return QueryLocalizedCommand(data, ref status, ExtensionLanguage.PortugueseBrazil);
    }

    private static bool QuerySynchronizeWithTransactionSpanish(CommandData data, ref CommandStatus status)
    {
        return QueryLocalizedCommand(data, ref status, ExtensionLanguage.Spanish);
    }

    private static bool QuerySynchronizeWithTransactionEnglish(CommandData data, ref CommandStatus status)
    {
        return QueryLocalizedCommand(data, ref status, ExtensionLanguage.English);
    }

    /// <summary>
    /// B111/F3 P6 — comando explícito de recuperação, nas duas superfícies de menu.
    ///
    /// Ele lê o diário durável da KB, cruza o inventário registrado com o que a KB mostra agora
    /// e oferece **uma** ação, sempre com confirmação: abandonar um envelope que nunca gravou
    /// nada, fechar o registro de uma remoção que já terminou, ou retomar a fila de uma remoção
    /// interrompida. Quando a reconciliação não é determinística, o comando não age: publica o
    /// diagnóstico e devolve a decisão a quem sabe o que aconteceu.
    ///
    /// Antes dele, um envelope não terminal bloqueava as operações seguintes e a única saída era
    /// apagar o File `GxOpenApiBuilder_OperationJournal` à mão.
    /// </summary>
    private static bool ExecuteRecoverInterruptedOperation(CommandData data)
    {
        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        if (knowledgeBase is null)
        {
            WriteOutput("[Genexus Open API Builder][B111/F3] Nenhuma Knowledge Base ativa foi encontrada. Abra uma KB e execute o comando novamente.");
            return true;
        }

        var texts = ExtensionLocalization.For(knowledgeBase);
        var owner = ResolveFinalReportOwner();
        try
        {
            RunRecovery(knowledgeBase, texts, owner);
        }
        catch (Exception ex)
        {
            var detail = DescribeException(ex);
            foreach (var b109Line in B109ExceptionProbe.Describe(ex, "Recuperar")) { WriteOutput("[Genexus Open API Builder]" + b109Line); }
            WriteOutput($"[Genexus Open API Builder][B111/F3] Recuperação falhou: Error='{detail}'. Nenhuma alteração foi feita.");
            System.Windows.Forms.MessageBox.Show(
                owner,
                detail,
                texts.RecoveryDialogTitle,
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
        }

        return true;
    }

    /// <summary>
    /// O fluxo da recuperação, separado do handler para que a oferta proativa — quando o diário
    /// bloqueia Apply, Sync ou Remover — leve ao mesmo lugar, e não a uma segunda implementação.
    /// </summary>
    private static void RunRecovery(
        KnowledgeBase knowledgeBase,
        ExtensionTexts texts,
        System.Windows.Forms.IWin32Window? owner)
    {
        var designModel = knowledgeBase.DesignModel;
        ApiPlanKbObjectNameIndex kbIndex;
        using (var loading = ExtensionBusyProgressScope.Show(owner, texts.RecoveryDialogTitle, texts))
        {
            kbIndex = ApiPlanKbObjectNameIndex.Create(designModel, loading.Session);
        }

        var validated = ApiPlanRecoveryReader.ReadAndValidate(designModel, kbIndex, knowledgeBase.Guid);
        if (!validated.IsValid)
        {
            var diagnostic = validated.Diagnostic!;
            WriteOutput($"[Genexus Open API Builder][B111/F3] Recuperação: {diagnostic.Describe()}");
            var message = string.Equals(diagnostic.ReasonCode, JournalGateReasonCodes.JournalMissing, StringComparison.Ordinal)
                ? texts.RecoveryNoJournal
                : diagnostic.Message;
            System.Windows.Forms.MessageBox.Show(
                owner,
                message,
                texts.RecoveryDialogTitle,
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);
            return;
        }

        var envelope = validated.Journal!;
        var observations = ApiPlanRecoveryReader.ObserveTargets(designModel, envelope, kbIndex);
        var operation = ApiPlanRecoveryRehydrator.Rehydrate(envelope, observations);
        foreach (var line in ApiPlanRecoveryReport.Describe(operation))
        {
            WriteOutput($"[Genexus Open API Builder][B111/F3] {line}");
        }

        if (operation.AlreadyTerminal)
        {
            System.Windows.Forms.MessageBox.Show(
                owner,
                texts.RecoveryNothingToDo,
                texts.RecoveryDialogTitle,
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);
            return;
        }

        if (!operation.CanExecute)
        {
            ExtensionRecoveryDialog.Inform(
                owner,
                texts,
                texts.RecoveryBlockedIntro + Environment.NewLine + Environment.NewLine + operation.Summary,
                DescribeRecoveryTargets(operation),
                warning: true);
            return;
        }

        var question = operation.NextStep switch
        {
            RecoveryNextStep.Abandon => texts.RecoveryConfirmAbandon,
            RecoveryNextStep.Complete => texts.RecoveryConfirmReconcile,
            RecoveryNextStep.Discard => operation.NothingWasWritten
                ? texts.RecoveryConfirmDiscardNothingWritten
                : texts.RecoveryConfirmDiscard,
            _ => texts.RecoveryConfirmContinueRemoval,
        };

        // Encerrar o registro não conclui a operação: quem confirma precisa ter visto o que
        // ficou na KB. Por isso o resumo encabeça o diálogo e o inventário aparece em bloco
        // próprio, em vez de derreter dentro do parágrafo da pergunta.
        var confirmed = ExtensionRecoveryDialog.Ask(
            owner,
            texts,
            operation.Summary,
            DescribeRecoveryTargets(operation),
            question);
        if (!confirmed)
        {
            WriteOutput("[Genexus Open API Builder][B111/F3] Recuperação recusada pelo usuário. Nenhuma alteração foi feita.");
            System.Windows.Forms.MessageBox.Show(
                owner,
                texts.RecoveryDeclined,
                texts.RecoveryDialogTitle,
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);
            return;
        }

        // A autorização vincula o consentimento ao snapshot exato que foi lido: identidade,
        // FileId, updatedUtc e hash canônico. Se o diário mudar entre a leitura e a ação, o
        // executor recusa antes de qualquer mutação.
        var authorization = new RecoveryAuthorization(
            humanConfirmed: true,
            envelope.OperationId,
            envelope.ApplicationId,
            validated.FileId,
            envelope.UpdatedUtc,
            validated.SnapshotHash,
            operation.NextStep,
            Environment.UserName);

        var result = ApiPlanRecoveryExecutor.Continue(
            designModel,
            validated,
            operation,
            authorization,
            rehydrated => ContinueInterruptedRemoval(knowledgeBase, texts, owner, validated, rehydrated));

        if (!result.Executed)
        {
            var diagnostic = result.Diagnostic;
            WriteOutput($"[Genexus Open API Builder][B111/F3] Recuperação bloqueada: {diagnostic?.Describe() ?? result.Summary}");
            ExtensionRecoveryDialog.Inform(
                owner,
                texts,
                texts.RecoveryBlockedIntro + Environment.NewLine + Environment.NewLine + result.Summary,
                DescribeRecoveryTargets(operation),
                warning: true);
            return;
        }

        WriteOutput($"[Genexus Open API Builder][B111/F3] Recuperação concluída: Etapa='{operation.NextStep}', OperationId='{envelope.OperationId}', Estado='{result.Envelope?.OperationState}'. {result.Summary}");
        ExtensionRecoveryDialog.Inform(owner, texts, result.Summary, Array.Empty<string>(), warning: false);
    }

    /// <summary>
    /// A orientação que faltava quando a remoção para no API Object ausente.
    ///
    /// Esse estado — API Object apagado fora da ferramenta, metadata e objetos próprios no
    /// lugar — recebia três recusas corretas e nenhuma explicação: o Wizard travava por
    /// ownership, o Remover parava aqui, e a recuperação de metadata órfã não se oferecia,
    /// porque exige exatamente um API Object presente. As duas saídas reais são estas, e ambas
    /// dependem de uma decisão que é do usuário, não da ferramenta.
    /// </summary>
    private static string DescribeRemovalGuidance(
        ApiPlanRemovalQueueResult queue,
        ApiPlanGeneratedApiRemovalResult result)
    {
        if (queue.BlockReason != JournalBlockReason.TargetAbsentBeforeDelete
            || queue.BlockingTarget?.ObjectType != JournalObjectType.ApiObject)
        {
            return string.Empty;
        }

        var metadataName = result.Plan?.MetadataFileName ?? string.Empty;
        return " O API Object previsto não está na KB, e quem o apagou não foi esta operação."
            + " Há duas saídas, e a escolha é sua: para regerar a API sobre o que restou, apague o File"
            + (string.IsNullOrEmpty(metadataName) ? " de metadata" : $" '{metadataName}'")
            + " e reaplique pelo Wizard, que reencontra SDTs e Procedures — paginação, ordenação e campos"
            + " obrigatórios voltam aos padrões das preferências; para descartar o que restou, apague os"
            + " objetos listados acima pela KB Explorer.";
    }

    /// <summary>
    /// O inventário como o diálogo o mostra: uma linha por alvo, com o que a intenção previa e
    /// o que a KB mostra agora. É a informação que sustenta a decisão de encerrar um registro.
    /// </summary>
    private static IReadOnlyList<string> DescribeRecoveryTargets(ApiPlanRehydratedOperation operation) =>
        operation.Targets.Select(target => target.Describe()).ToArray();

    /// <summary>
    /// Retoma a fila de uma remoção interrompida **no mesmo envelope**, com o inventário durável
    /// que o diário guarda. É por isso que a intenção é registrada antes da primeira exclusão: o
    /// File de metadata é o penúltimo da fila, e depois que ele sai não há outra fonte do que
    /// faltava.
    /// </summary>
    private static ApiPlanRecoveryResult ContinueInterruptedRemoval(
        KnowledgeBase knowledgeBase,
        ExtensionTexts texts,
        System.Windows.Forms.IWin32Window? owner,
        ApiPlanValidatedJournal validated,
        ApiPlanRehydratedOperation operation)
    {
        var envelope = validated.Journal!;
        var journal = ApiPlanOperationJournalSession.Resume(knowledgeBase.DesignModel, validated.File!, envelope);
        if (!journal.ResumeRemoval())
        {
            WriteJournalDiagnostics(journal);
            return ApiPlanRecoveryResult.Blocked(
                operation,
                ApiPlanOperationJournalGate.SaveUnconfirmed(validated.FileId, journal.BlockDetail));
        }

        WriteJournalDiagnostics(journal);

        var observed = operation.Targets.ToDictionary(
            target => ApiPlanOperationJournalValidator.BuildIdentityKey(target.Item),
            target => target.PhysicalState,
            StringComparer.Ordinal);
        var targets = ApiPlanRemovalIntent.FromInventory(envelope.Inventory, observed);
        var context = ApiPlanRemovalContext.FromJournal(envelope);
        var telemetry = new ApiPlanScanTelemetry();
        var deleted = new List<string>();
        var persistenceLog = new ApiPlanPersistenceLog();
        using var persistenceScope = ApiPlanSaveBoundaryProbe.BeginPersistence(persistenceLog);
        journal.AttachPersistence(persistenceLog, null);
        journal.AttachRemovalInventory(() => ApiPlanRemovalIntent.BuildInventory(targets));

        var stopwatch = Stopwatch.StartNew();
        ApiPlanGeneratedApiRemovalResult removal;
        using (var busy = ExtensionBusyProgressScope.Show(owner, texts.BusyProgressTitleRemove, texts))
        {
            try
            {
                removal = ApiPlanGeneratedApiRemover.Execute(
                    knowledgeBase.DesignModel,
                    context,
                    targets,
                    telemetry,
                    ApiPlanRemovalIntent.ResolveMaxPasses(targets),
                    busy.Session,
                    deleted,
                    pass =>
                    {
                        var confirmed = journal.NoteRemovalPassCompleted();
                        WriteJournalDiagnostics(journal);
                        return confirmed;
                    });
            }
            catch (ApiPlanBusyAbortedException abortEx)
            {
                stopwatch.Stop();
                journal.Interrupt(JournalOperationState.Partial, JournalBlockReason.UserAborted);
                WriteJournalDiagnostics(journal);
                WriteOutput($"[Genexus Open API Builder][B111/F3] Retomada abortada: Error='{abortEx.Message}', JaRemovidos={deleted.Count}.");
                var abortReport = new ApiPlanApplicationFinalReportCollector("Recuperar", envelope.TransactionName, context.ApiName);
                abortReport.SetApiName(context.ApiName);
                abortReport.AddDeletedItems(deleted.ToArray());
                abortReport.AddBlocked("Recuperar", envelope.TransactionName, "Abortado [B082]");
                ShowFinalReport(abortReport, stopwatch.Elapsed, knowledgeBase.DesignModel, persistenceLog: persistenceLog);
                return ApiPlanRecoveryResult.Blocked(
                    operation,
                    ApiPlanOperationJournalGate.SaveUnconfirmed(validated.FileId, abortEx.Message));
            }
        }

        stopwatch.Stop();
        var report = new ApiPlanApplicationFinalReportCollector("Recuperar", envelope.TransactionName, removal.ApiName);
        report.SetApiName(removal.ApiName);
        report.AddDeletedItems(removal.DeletedItems.ToArray());
        CloseRemovalJournal(journal, report, removal, envelope.TransactionName);
        foreach (var telemetryLine in removal.TelemetryLines)
        {
            WriteOutput($"[Genexus Open API Builder][B082] Retomada {telemetryLine}");
        }

        ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, persistenceLog: persistenceLog);

        var queue = removal.Queue;
        if (queue is not null && !queue.IsComplete)
        {
            return ApiPlanRecoveryResult.Blocked(
                operation,
                new ApiPlanOperationJournalGateDiagnostic(
                    JournalGateDiagnosticCode.GateBlocked,
                    JournalGateReasonCodes.JournalNonTerminal,
                    JournalGatePrecondition.PriorIntentReconciled,
                    "A retomada não concluiu a remoção: " + queue.Describe(),
                    Array.Empty<KeyValuePair<string, string>>()));
        }

        return ApiPlanRecoveryResult.Applied(
            operation,
            envelope,
            string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "A remoção foi retomada e concluída: {0} objeto(s) saíram da KB nesta continuação.",
                removal.DeletedItems.Count));
    }

    /// <summary>
    /// Oferta proativa da P6: quando o diário recusa uma operação por haver outra interrompida,
    /// a saída fica à mão. Sem isso, o bloqueio só informava — e a única saída praticada na
    /// validação da P3 foi apagar o File do diário à mão.
    /// </summary>
    private static void OfferRecoveryAfterJournalBlock(
        KnowledgeBase knowledgeBase,
        ExtensionTexts texts,
        ApiPlanOperationJournalStart journalStart)
    {
        if (journalStart.Diagnostic is null || journalStart.CurrentEnvelope is null)
        {
            return;
        }

        var preferences = PrototypeWizardPreferencesStore.Load(knowledgeBase.DesignModel).Preferences;
        if (!preferences.ShowRecoveryOptionProactively)
        {
            return;
        }

        var owner = ResolveFinalReportOwner();
        var answer = System.Windows.Forms.MessageBox.Show(
            owner,
            texts.RecoveryOfferAfterBlock,
            texts.RecoveryDialogTitle,
            System.Windows.Forms.MessageBoxButtons.YesNo,
            System.Windows.Forms.MessageBoxIcon.Warning,
            System.Windows.Forms.MessageBoxDefaultButton.Button2);
        if (answer != System.Windows.Forms.DialogResult.Yes)
        {
            return;
        }

        RunRecovery(knowledgeBase, texts, owner);
    }

    private static bool QueryRecoverInterruptedOperationPortuguese(CommandData data, ref CommandStatus status)
    {
        return QueryLocalizedCommand(data, ref status, ExtensionLanguage.PortugueseBrazil);
    }

    private static bool QueryRecoverInterruptedOperationSpanish(CommandData data, ref CommandStatus status)
    {
        return QueryLocalizedCommand(data, ref status, ExtensionLanguage.Spanish);
    }

    private static bool QueryRecoverInterruptedOperationEnglish(CommandData data, ref CommandStatus status)
    {
        return QueryLocalizedCommand(data, ref status, ExtensionLanguage.English);
    }

    private static bool QueryRemoveGeneratedApiPortuguese(CommandData data, ref CommandStatus status)
    {
        return QueryLocalizedCommand(data, ref status, ExtensionLanguage.PortugueseBrazil);
    }

    private static bool QueryRemoveGeneratedApiSpanish(CommandData data, ref CommandStatus status)
    {
        return QueryLocalizedCommand(data, ref status, ExtensionLanguage.Spanish);
    }

    private static bool QueryRemoveGeneratedApiEnglish(CommandData data, ref CommandStatus status)
    {
        return QueryLocalizedCommand(data, ref status, ExtensionLanguage.English);
    }

    private static bool ExecuteSynchronizeWithTransaction(CommandData data)
    {
        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        if (knowledgeBase is null)
        {
            WriteOutput("[Genexus Open API Builder][B085] Nenhuma Knowledge Base ativa foi encontrada. Abra uma KB e execute o comando novamente.");
            return true;
        }

        var texts = ExtensionLocalization.For(knowledgeBase);
        Transaction? transaction;
        try
        {
            transaction = ResolveTransactionForCommand(data, knowledgeBase, texts.SynchronizeWithTransaction);
        }
        catch (InvalidOperationException ex)
        {
            WriteOutput($"[Genexus Open API Builder][B085] {ex.Message}");
            return true;
        }

        if (transaction is null)
        {
            WriteOutput("[Genexus Open API Builder][B085] Nenhuma Transaction foi selecionada. Nenhuma alteracao foi feita na KB.");
            return true;
        }

        try
        {
            var owner = ResolveFinalReportOwner();
            ApiPlanTransactionSyncPreview preview;
            // B082: o Preview e fase distinta do Apply/Sync e cria o proprio indice.
            // Escopo separado para que seu custo nao se misture ao da escrita.
            var syncPreviewTelemetry = new ApiPlanScanTelemetry();
            using (ApiPlanScanProbe.Begin(syncPreviewTelemetry, t => WriteScanTelemetry("Sync Preview", t)))
            using (var loading = ExtensionBusyProgressScope.Show(owner, texts.BusyProgressTitleLoadingSync, texts))
            {
                var previewWatch = Stopwatch.StartNew();
                try
                {
                    var kbIndex = ApiPlanKbObjectNameIndex.Create(knowledgeBase.DesignModel, loading.Session);
                    preview = ApiPlanTransactionSyncOrchestrator.Preview(
                        knowledgeBase.DesignModel,
                        transaction,
                        loading.Session,
                        kbIndex);
                }
                catch (ApiPlanBusyAbortedException abortEx)
                {
                    WriteOutput($"[Genexus Open API Builder][B082] Sync preview abortado: Transaction='{transaction.Name}', Error='{abortEx.Message}'");
                    return true;
                }

                previewWatch.Stop();
                WriteOutput($"[Genexus Open API Builder][B082] Sync PreviewMs={previewWatch.ElapsedMilliseconds}.");
            }

            WriteOutput($"[Genexus Open API Builder][B085] Diff para Transaction='{transaction.Name}':{Environment.NewLine}{preview.Diff.BuildSummary()}");
            if (preview.SdtConflicts.Count > 0)
            {
                WriteOutput($"[Genexus Open API Builder][B085] Conflitos de SDT: {string.Join("; ", preview.SdtConflicts.Select(item => item.SdtName))}");
            }

            if (!preview.Diff.HasDifferences && preview.SdtConflicts.Count == 0)
            {
                WriteOutput($"[Genexus Open API Builder][B085] Nenhuma diferenca entre Transaction e metadata. Nenhuma alteracao foi feita na KB.");
                var apiName = preview.Metadata.SelectToken("ownership.apiName")?.ToString()
                    ?? $"api{transaction.Name}";
                var noOpReport = new ApiPlanApplicationFinalReportCollector("Sincronizar", transaction.Name, apiName);
                noOpReport.HeadlineOverride = "Nenhuma sincronizacao necessaria.";
                noOpReport.AddWarning("Nenhuma diferenca entre Transaction e metadata. Nenhuma alteracao foi feita na KB.");
                ShowFinalReport(noOpReport, TimeSpan.Zero, knowledgeBase.DesignModel);
                return true;
            }

            using var dialog = new ApiPlanTransactionSyncDialog(preview, texts);
            ExtensionIdeScreenPlacement.CenterOnIdeScreen(dialog, owner);
            var dialogResult = owner is null
                ? dialog.ShowDialog()
                : dialog.ShowDialog(owner);
            if (dialogResult != System.Windows.Forms.DialogResult.OK || dialog.Choices is null || dialog.Choices.Cancel)
            {
                WriteOutput($"[Genexus Open API Builder][B085] Sincronizacao cancelada pelo usuario para Transaction='{transaction.Name}'. Nenhuma alteracao foi feita na KB.");
                return true;
            }

            var selection = ApiPlanTransactionSyncOrchestrator.BuildSelection(preview, dialog.Choices);
            var preserveSdts = ApiPlanTransactionSyncOrchestrator.ResolvePreservedSdtNames(preview, dialog.Choices);
            var allowedAddedSdtMemberNamesByRole = ApiPlanTransactionSyncOrchestrator.ResolveSelectedAddedSdtMemberNamesByRole(preview, dialog.Choices);
            var apiPlan = ApiPlanBuilder.Build(knowledgeBase.DesignModel, transaction, selection);
            var b111ManagedApply = selection.GenerateApiObject || selection.GenerateMetadata || selection.ApplyBusinessComponent || selection.ApplyList;
            var report = new ApiPlanApplicationFinalReportCollector("Sincronizar", transaction.Name, apiPlan.ApiName);
            var persistenceLog = new ApiPlanPersistenceLog();
            using var persistenceScope = ApiPlanSaveBoundaryProbe.BeginPersistence(persistenceLog);
            report.SetPersistenceLog(persistenceLog);
            var stopwatch = Stopwatch.StartNew();
            AppendPlanWarnings(report, apiPlan);
            // B111/F3: o diário do Sync segue o mesmo contrato do Apply e fica fora do try
            // para que o aborto também seja registrado como interrupção.
            ApiPlanOperationJournalSession? syncJournal = null;
            // B082: mede o custo das varreduras de catalogo ao longo do Sync que escreve.
            var syncScanTelemetry = new ApiPlanScanTelemetry();
            using var syncScanScope = ApiPlanScanProbe.Begin(
                syncScanTelemetry,
                telemetry => WriteScanTelemetry("Sync", telemetry));
            try
            {
                using var busy = ExtensionBusyProgressScope.Show(ResolveFinalReportOwner(), texts.BusyProgressTitleSync, texts);
                WriteOutput($"[Genexus Open API Builder][B082] Sync iniciado: Transaction='{transaction.Name}'.");
                var (syncState, syncKbIndex) = ApiPlanGenerationStateReader.ReadForSyncWithIndex(
                    knowledgeBase.DesignModel,
                    transaction,
                    apiPlan,
                    busy.Session);
                AppendTransactionFolderWarning(report, syncState);
                foreach (var preserved in preserveSdts.OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
                {
                    report.AddWarning($"SDT preservado (Keep): {preserved}.");
                }

                busy.Report("Validando", 0, 0, "Preflight");
                busy.Session.PumpAndThrowIfAbortRequested();
                try
                {
                    PrototypeWizardBusinessComponentNavigationPolicy.ThrowIfDeleteWithoutBusinessComponent(
                        apiPlan.Services.Select(service => service.Name),
                        selection.ApplyBusinessComponent);
                    ApiPlanWritePreflight.ValidateForSync(knowledgeBase.DesignModel, transaction, apiPlan, syncKbIndex);
                    ApiPlanWritePreflight.ValidateForF1(
                        knowledgeBase.DesignModel,
                        transaction,
                        apiPlan,
                        selection.GenerateSdts,
                        selection.GenerateProcedures,
                        selection.GenerateApiObject,
                        selection.GenerateMetadata,
                        selection.ApplyList,
                        selection.ApplyBusinessComponent,
                        syncKbIndex,
                        preserveSdts,
                        allowedAddedSdtMemberNamesByRole);
                }
                catch (Exception ex) when (ex is not ApiPlanBusyAbortedException)
                {
                    var errorDetail = ex.InnerException is null ? ex.Message : $"{ex.Message} | Inner='{ex.InnerException.Message}'";
                    // B109: sonda temporaria - publica a stack completa, que o log de uma linha descarta.
                    foreach (var b109Line in B109ExceptionProbe.Describe(ex, "Sync")) { WriteOutput("[Genexus Open API Builder]" + b109Line); }
                    WriteOutput($"[Genexus Open API Builder][B085] Sincronizacao bloqueada ou falhou: Transaction='{transaction.Name}', Error='{errorDetail}'");
                    AppendCollisionConflictsToReport(report, syncState.CollectCollisionConflicts());
                    if (!report.HasInterrupted)
                    {
                        report.AddBlocked("Preflight", "Sync", errorDetail);
                    }

                    stopwatch.Stop();
                    ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, apiPlan);
                    return true;
                }

                WriteOutput($"[Genexus Open API Builder][B085] Preflight de sincronizacao aprovado. Aplicando para Transaction='{transaction.Name}', ApiName='{apiPlan.ApiName}'.");

                var syncJournalStart = ApiPlanOperationJournalSession.Start(
                    knowledgeBase.DesignModel,
                    syncKbIndex,
                    knowledgeBase.Guid,
                    transaction,
                    JournalOperationKind.Sync,
                    ApiPlanOperationJournalPlans.ForGeneration(
                        apiPlan.PlannedApiGuid,
                        ApiPlanMetadataFileWriter.ComputePlannedContractHash(apiPlan),
                        selection.GenerateApiObject,
                        selection.GenerateSdts,
                        selection.GenerateProcedures,
                        selection.GenerateMetadata,
                        apiPlan.Services.Select(service => service.Name)),
                    apiPlan.ApplicationId,
                    JournalIntentKind.Current,
                    selection.GenerateMetadata ? ApiPlanMetadataFileWriter.SchemaVersion : null);
                if (!syncJournalStart.IsStarted)
                {
                    WriteOutput($"[Genexus Open API Builder][B111/F3] Sincronizacao bloqueada pelo diário durável: Transaction='{transaction.Name}'. {syncJournalStart.Detail} Nenhuma gravação foi solicitada.");
                    report.AddBlocked("Diário de operação", "B111/F3", syncJournalStart.Detail);
                    stopwatch.Stop();
                    ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, apiPlan);
                    OfferRecoveryAfterJournalBlock(knowledgeBase, texts, syncJournalStart);
                    return true;
                }

                syncJournal = syncJournalStart.Session!;
                syncJournal.AttachPersistence(persistenceLog, () => report.CreatedObjectNames);
                WriteJournalDiagnostics(syncJournal);

                if (!TryCreateSdts(knowledgeBase.DesignModel, transaction, apiPlan, "SyncB085", syncKbIndex, preserveSdts, report, busy.Session))
                {
                    InterruptJournal(syncJournal, report, JournalBlockReason.StageFailed);
                    stopwatch.Stop();
                    ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, apiPlan);
                    return true;
                }

                syncKbIndex.RefreshSdts(knowledgeBase.DesignModel);

                if (!TryCreateProcedures(knowledgeBase.DesignModel, transaction, apiPlan, "SyncB085", syncKbIndex, report, busy.Session))
                {
                    InterruptJournal(syncJournal, report, JournalBlockReason.StageFailed);
                    stopwatch.Stop();
                    ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, apiPlan);
                    return true;
                }

                busy.ThrowIfAbortRequested();
                ApiPlanTransientApiContext? syncApiContext = null;
                var syncHasConsumers = selection.ApplyBusinessComponent || selection.ApplyList;
                if (syncHasConsumers)
                {
                    var syncFinalWriter = selection.ApplyList ? "List" : "Business Component";
                    busy.Report("API Object", 0, 0, apiPlan.ApiName);
                    var apiMs = busy.Measure(() =>
                    {
                        if (!TryPrepareApiObject(
                            knowledgeBase.DesignModel,
                            transaction,
                            apiPlan,
                            "SyncB085",
                            syncKbIndex,
                            allowIntentionalContractRefresh: true,
                            persistApiObject: selection.GenerateApiObject,
                            businessComponentParticipated: selection.ApplyBusinessComponent,
                            finalWriter: syncFinalWriter,
                            progress: busy.Session,
                            report: report,
                            selection: new ApiPlanTransientApiSelection(selection.GenerateSdts, selection.GenerateProcedures, selection.GenerateApiObject, selection.GenerateMetadata, selection.ApplyList, selection.ApplyBusinessComponent),
                            preserveSdtNames: preserveSdts,
                            context: out syncApiContext))
                        {
                            throw new InvalidOperationException("SYNC_API_OBJECT_FAILED");
                        }
                    });
                    busy.Report("API Object", 1, 1, apiPlan.ApiName, apiMs);
                }
                else if (selection.GenerateApiObject)
                {
                    busy.Report("API Object", 0, 0, apiPlan.ApiName);
                    var apiMs = busy.Measure(() =>
                    {
                        if (!TryCreateApiObject(
                            knowledgeBase.DesignModel,
                            transaction,
                            apiPlan,
                            "SyncB085",
                            syncKbIndex,
                            busy.Session,
                            report,
                            allowIntentionalContractRefresh: true))
                        {
                            throw new InvalidOperationException("SYNC_API_OBJECT_FAILED");
                        }
                    });
                    busy.Report("API Object", 1, 1, apiPlan.ApiName, apiMs);
                }

                busy.ThrowIfAbortRequested();
                if (selection.ApplyBusinessComponent)
                {
                    var bcFailed = !TryApplyBusinessComponent(
                        knowledgeBase.DesignModel,
                        transaction,
                        apiPlan,
                        "SyncB085",
                        allowIntentionalContractRefresh: true,
                        preserveSdtNames: preserveSdts,
                        report: report,
                        progress: busy.Session,
                        kbIndex: syncKbIndex,
                        apiContext: syncApiContext);
                    if (bcFailed)
                    {
                        InterruptJournal(syncJournal, report, JournalBlockReason.StageFailed);
                        stopwatch.Stop();
                        ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, apiPlan);
                        return true;
                    }
                }

                busy.ThrowIfAbortRequested();
                if (selection.ApplyList)
                {
                    var listFailed = !TryApplyList(
                        knowledgeBase.DesignModel,
                        transaction,
                        apiPlan,
                        "SyncB085",
                        syncKbIndex,
                        allowIntentionalContractRefresh: true,
                        preserveSdtNames: preserveSdts,
                        report: report,
                        progress: busy.Session,
                        apiContext: syncApiContext);
                    if (listFailed)
                    {
                        InterruptJournal(syncJournal, report, JournalBlockReason.StageFailed);
                        stopwatch.Stop();
                        ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, apiPlan);
                        return true;
                    }
                }

                busy.ThrowIfAbortRequested();
                if (selection.GenerateMetadata)
                {
                    busy.Report("Metadata", 0, 0, apiPlan.MetadataFileName);
                    var metaMs = busy.Measure(() =>
                    {
                        if (!TryWriteMetadataFile(
                            knowledgeBase.DesignModel,
                            transaction,
                            apiPlan,
                            "SyncB085",
                            syncKbIndex,
                            allowIntentionalContractRefresh: true,
                            report: report))
                        {
                            throw new InvalidOperationException("SYNC_METADATA_FAILED");
                        }
                    });
                    busy.Report("Metadata", 1, 1, apiPlan.MetadataFileName, metaMs);
                }

                CompleteJournal(syncJournal, report, syncApiContext?.PlannedApiGuid ?? report.PersistedMainObjectGuid ?? apiPlan.PlannedApiGuid);
            }
            catch (ApiPlanBusyAbortedException abortEx)
            {
                WriteOutput($"[Genexus Open API Builder][B082] Sync abortado: Transaction='{transaction.Name}', Error='{abortEx.Message}'");
                report.HeadlineOverride = "Sincronização abortada pelo usuário.";
                report.AddWarning(abortEx.Message);
                report.AddBlocked("Sync", transaction.Name, "Abortado [B082]");
                // B111/F3 P3: aborto é decisão do usuário, não falha de etapa.
                InterruptJournal(syncJournal, report, JournalBlockReason.UserAborted);
                stopwatch.Stop();
                ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, apiPlan);
                return true;
            }
            catch (InvalidOperationException ex) when (
                ex.Message == "SYNC_API_OBJECT_FAILED" || ex.Message == "SYNC_METADATA_FAILED")
            {
                InterruptJournal(syncJournal, report, JournalBlockReason.StageFailed);
                stopwatch.Stop();
                ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, apiPlan);
                return true;
            }

            WriteOutput($"[Genexus Open API Builder][B085] Sincronizacao concluida para Transaction='{transaction.Name}', ApiName='{apiPlan.ApiName}', PreservedSdts={preserveSdts.Count}.");
            stopwatch.Stop();
            ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, apiPlan);
        }
        catch (Exception ex)
        {
            var errorDetail = ex.InnerException is null ? ex.Message : $"{ex.Message} | Inner='{ex.InnerException.Message}'";
            // B109: sonda temporaria - publica a stack completa, que o log de uma linha descarta.
            foreach (var b109Line in B109ExceptionProbe.Describe(ex, "Sync")) { WriteOutput("[Genexus Open API Builder]" + b109Line); }
            WriteOutput($"[Genexus Open API Builder][B085] Sincronizacao bloqueada ou falhou: Transaction='{transaction.Name}', Error='{errorDetail}'");
            var report = new ApiPlanApplicationFinalReportCollector("Sincronizar", transaction.Name, null);
            report.AddBlocked("Sincronizar", transaction.Name, errorDetail);
            ShowFinalReport(report, TimeSpan.Zero, knowledgeBase.DesignModel);
        }

        return true;
    }

    private static bool ExecuteRemoveGeneratedApi(CommandData data)
    {
        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        if (knowledgeBase is null)
        {
            WriteOutput("[Genexus Open API Builder][B086] Nenhuma Knowledge Base ativa foi encontrada. Abra uma KB e execute o comando novamente.");
            return true;
        }

        var texts = ExtensionLocalization.For(knowledgeBase);
        Transaction? transaction;
        try
        {
            transaction = ResolveTransactionForCommand(data, knowledgeBase, texts.RemoveGeneratedApi);
        }
        catch (InvalidOperationException ex)
        {
            WriteOutput($"[Genexus Open API Builder][B086] {ex.Message}");
            return true;
        }

        if (transaction is null)
        {
            WriteOutput("[Genexus Open API Builder][B086] Nenhuma Transaction foi selecionada. Nenhuma alteracao foi feita na KB.");
            return true;
        }

        // Declarado fora do try para o catch enxergar o que já saiu da KB quando a remoção
        // é interrompida no meio.
        var deletedBeforeFailure = new List<string>();
        ApiPlanPersistenceLog? persistenceLog = null;
        // B111/F3 P4: fora do try pelo mesmo motivo da lista acima — o catch externo precisa
        // fechar o diário com a intenção preservada quando a remoção falha no meio.
        ApiPlanOperationJournalSession? removeJournal = null;
        try
        {
            var owner = ResolveFinalReportOwner();
            ApiPlanGeneratedApiRemovalPlan plan;
            // B082: mesma separação do Sync — o Preview do Remover cria o próprio índice
            // e é medido à parte da exclusão, que tem telemetria própria.
            var removePreviewTelemetry = new ApiPlanScanTelemetry();
            using (ApiPlanScanProbe.Begin(removePreviewTelemetry, t => WriteScanTelemetry("Remover Preview", t)))
            using (var loading = ExtensionBusyProgressScope.Show(owner, texts.BusyProgressTitleLoadingRemove, texts))
            {
                var previewWatch = Stopwatch.StartNew();
                try
                {
                    var kbIndex = ApiPlanKbObjectNameIndex.Create(knowledgeBase.DesignModel, loading.Session);
                    plan = ApiPlanGeneratedApiRemover.Preview(
                        knowledgeBase.DesignModel,
                        transaction,
                        loading.Session,
                        kbIndex);
                }
                catch (ApiPlanBusyAbortedException abortEx)
                {
                    WriteOutput($"[Genexus Open API Builder][B082] Remover preview abortado: Transaction='{transaction.Name}', Error='{abortEx.Message}'");
                    return true;
                }

                previewWatch.Stop();
                WriteOutput($"[Genexus Open API Builder][B082] Remover PreviewMs={previewWatch.ElapsedMilliseconds}.");
            }

            WriteOutput($"[Genexus Open API Builder][B086] Plano de remocao para Transaction='{transaction.Name}':{Environment.NewLine}{plan.BuildConfirmationSummary()}");
            using var confirmationDialog = new ExtensionConfirmDialog(
                texts.RemoveGeneratedApi,
                texts.RemovalConfirmationIntro,
                plan,
                texts.ConfirmDeletion,
                texts,
                owner);
            var confirmationResult = owner is null
                ? confirmationDialog.ShowDialog()
                : confirmationDialog.ShowDialog(owner);
            if (confirmationResult != System.Windows.Forms.DialogResult.Yes)
            {
                WriteOutput($"[Genexus Open API Builder][B086] Remocao cancelada pelo usuario para Transaction='{transaction.Name}'. Nenhuma alteracao foi feita na KB.");
                return true;
            }

            persistenceLog = new ApiPlanPersistenceLog();
            using var persistenceScope = ApiPlanSaveBoundaryProbe.BeginPersistence(persistenceLog);
            var stopwatch = Stopwatch.StartNew();
            ApiPlanGeneratedApiRemovalResult result;
            // `deletedBeforeFailure` é preenchido durante a remoção: numa interrupção, é a
            // única forma de o relatório dizer o que já saiu da KB.
            using (var busy = ExtensionBusyProgressScope.Show(owner, texts.BusyProgressTitleRemove, texts))
            {
                WriteOutput($"[Genexus Open API Builder][B082] Remover iniciado: Transaction='{transaction.Name}', PlannedDeletes={ApiPlanGeneratedApiRemover.CountPlannedDeletes(plan)}.");

                // A intenção é resolvida **antes** de qualquer exclusão: identidade de cada alvo,
                // o que sai e o que fica. Sem ela registrada, uma interrupção no meio deixaria a
                // KB num estado que ninguém reconstitui.
                ApiPlanGeneratedApiRemovalIntent intent;
                try
                {
                    var removeIndex = ApiPlanKbObjectNameIndex.Create(knowledgeBase.DesignModel, busy.Session);
                    intent = ApiPlanGeneratedApiRemover.ResolveIntent(
                        knowledgeBase.DesignModel,
                        transaction,
                        busy.Session,
                        removeIndex);
                }
                catch (ApiPlanBusyAbortedException abortEx)
                {
                    stopwatch.Stop();
                    WriteOutput($"[Genexus Open API Builder][B082] Remover abortado antes da intencao: Transaction='{transaction.Name}', Error='{abortEx.Message}'. Nenhuma alteracao foi feita na KB.");
                    return true;
                }

                var removalApiGuid = Guid.TryParse(intent.Plan.ApiGuid, out var parsedApiGuid) ? parsedApiGuid : (Guid?)null;
                var journalStart = ApiPlanOperationJournalSession.Start(
                    knowledgeBase.DesignModel,
                    intent.KbIndex,
                    knowledgeBase.Guid,
                    transaction,
                    JournalOperationKind.Remove,
                    ApiPlanOperationJournalPlans.ForRemoval(
                        removalApiGuid,
                        intent.ContractHash,
                        Array.Empty<string>()),
                    // Metadata V3 reutiliza o `applicationId` do ownership; metadata legada
                    // recebe um identificador novo, registrado só no diário como adoção tardia.
                    intent.ApplicationId ?? Guid.NewGuid(),
                    intent.IntentKind,
                    intent.MetadataSchemaVersion,
                    intent.BuildInventory());
                if (!journalStart.IsStarted)
                {
                    stopwatch.Stop();
                    WriteOutput($"[Genexus Open API Builder][B111/F3] Remocao bloqueada pelo diário durável: Transaction='{transaction.Name}'. {journalStart.Detail} Nenhum objeto foi excluído.");
                    var blockedReport = new ApiPlanApplicationFinalReportCollector("Remover", transaction.Name, intent.Plan.ApiName);
                    blockedReport.SetApiName(intent.Plan.ApiName);
                    blockedReport.AddBlocked("Diário de operação", "B111/F3", journalStart.Detail);
                    ShowFinalReport(blockedReport, stopwatch.Elapsed, knowledgeBase.DesignModel, persistenceLog: persistenceLog);
                    OfferRecoveryAfterJournalBlock(knowledgeBase, texts, journalStart);
                    return true;
                }

                removeJournal = journalStart.Session!;
                removeJournal.AttachPersistence(persistenceLog, null);
                removeJournal.AttachRemovalInventory(intent.BuildInventory);
                WriteJournalDiagnostics(removeJournal);

                try
                {
                    result = ApiPlanGeneratedApiRemover.Remove(
                        knowledgeBase.DesignModel,
                        intent,
                        busy.Session,
                        deletedBeforeFailure,
                        pass =>
                        {
                            var confirmed = removeJournal.NoteRemovalPassCompleted();
                            WriteJournalDiagnostics(removeJournal);
                            return confirmed;
                        });
                }
                catch (ApiPlanBusyAbortedException abortEx)
                {
                    stopwatch.Stop();
                    WriteOutput($"[Genexus Open API Builder][B082] Remover abortado: Transaction='{transaction.Name}', Error='{abortEx.Message}', JaRemovidos={deletedBeforeFailure.Count}, Items='{string.Join("; ", deletedBeforeFailure)}'");
                    var abortReport = new ApiPlanApplicationFinalReportCollector("Remover", transaction.Name, plan.ApiName);
                    abortReport.SetApiName(plan.ApiName);
                    abortReport.HeadlineOverride = "Remoção abortada pelo usuário.";
                    abortReport.AddDeletedItems(deletedBeforeFailure.ToArray());
                    abortReport.AddWarning(abortEx.Message);
                    if (deletedBeforeFailure.Count > 0)
                    {
                        abortReport.AddWarning($"Remocao parcial: {deletedBeforeFailure.Count} objeto(s) ja foram excluidos e estao listados como removidos. A API ficou incompleta; reaplique pelo Wizard ou repita a remocao.");
                    }

                    abortReport.AddBlocked("Remover", transaction.Name, "Abortado [B082]");
                    // Aborto é decisão do usuário, não falha de etapa: a intenção fica preservada.
                    InterruptJournal(removeJournal, abortReport, JournalBlockReason.UserAborted);
                    ShowFinalReport(abortReport, stopwatch.Elapsed, knowledgeBase.DesignModel, persistenceLog: persistenceLog);
                    return true;
                }
            }

            stopwatch.Stop();
            var queue = result.Queue;
            WriteOutput($"[Genexus Open API Builder][B086] Remocao encerrada: Transaction='{transaction.Name}', ApiName='{result.ApiName}', Estado='{queue?.Outcome.ToString() ?? "Removed"}', Deleted={result.DeletedItems.Count}, Items='{string.Join("; ", result.DeletedItems)}'. SDTs compartilhados e Business Component da Transaction nao foram alterados.");
            WriteOutput($"[Genexus Open API Builder][B082] Remover TotalMs={stopwatch.ElapsedMilliseconds}.");
            foreach (var telemetryLine in result.TelemetryLines)
            {
                WriteOutput($"[Genexus Open API Builder][B082] Remover {telemetryLine}");
            }

            var report = new ApiPlanApplicationFinalReportCollector("Remover", transaction.Name, result.ApiName);
            report.SetApiName(result.ApiName);
            report.AddDeletedItems(result.DeletedItems.ToArray());
            CloseRemovalJournal(removeJournal, report, result, transaction.Name);
            ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, persistenceLog: persistenceLog);
        }
        catch (Exception ex)
        {
            var errorDetail = ex.InnerException is null ? ex.Message : $"{ex.Message} | Inner='{ex.InnerException.Message}'";
            // B109: sonda temporaria - publica a stack completa, que o log de uma linha descarta.
            foreach (var b109Line in B109ExceptionProbe.Describe(ex, "Remover")) { WriteOutput("[Genexus Open API Builder]" + b109Line); }
            WriteOutput($"[Genexus Open API Builder][B086] Remocao bloqueada ou falhou: Transaction='{transaction.Name}', Error='{errorDetail}', JaRemovidos={deletedBeforeFailure.Count}, Items='{string.Join("; ", deletedBeforeFailure)}'");
            var report = new ApiPlanApplicationFinalReportCollector("Remover", transaction.Name, null);
            report.AddDeletedItems(deletedBeforeFailure.ToArray());
            if (deletedBeforeFailure.Count > 0)
            {
                report.AddWarning($"Remocao parcial: {deletedBeforeFailure.Count} objeto(s) ja foram excluidos e estao listados como removidos. A API ficou incompleta; reaplique pelo Wizard ou repita a remocao.");
            }

            report.AddBlocked("Remover", transaction.Name, errorDetail);
            // A falha veio de fora da fila (preflight, metadata, catálogo). O envelope fecha em
            // Partial: o que já saiu está no inventário, e a intenção continua registrada.
            InterruptJournal(removeJournal, report, JournalBlockReason.StageFailed);
            ShowFinalReport(report, TimeSpan.Zero, knowledgeBase.DesignModel, persistenceLog: persistenceLog);
        }

        return true;
    }

    /// <summary>
    /// Fecha o diário da remoção segundo o estado terminal da fila. <c>Removed</c> só quando
    /// todos os alvos previstos saíram confirmados; qualquer outro desfecho grava o motivo do
    /// enum fechado e preserva a intenção para a reconciliação.
    /// </summary>
    private static void CloseRemovalJournal(
        ApiPlanOperationJournalSession? journal,
        ApiPlanApplicationFinalReportCollector report,
        ApiPlanGeneratedApiRemovalResult result,
        string transactionName)
    {
        if (journal is null)
        {
            return;
        }

        var queue = result.Queue;
        if (queue is null || queue.IsComplete)
        {
            journal.Complete();
            WriteJournalDiagnostics(journal);
            if (journal.IsBlocked)
            {
                report.AddWarning($"Diário de operação: {journal.BlockDetail}");
            }

            return;
        }

        journal.Interrupt(queue.Outcome, queue.BlockReason ?? JournalBlockReason.OutcomeUnknown);
        WriteJournalDiagnostics(journal);

        var pending = queue.Pending.Count == 0
            ? string.Empty
            : " Pendentes: " + string.Join("; ", queue.Pending.Select(target => target.Describe())) + ".";
        var guidance = DescribeRemovalGuidance(queue, result);
        WriteOutput($"[Genexus Open API Builder][B111/F3] Remocao interrompida: Transaction='{transactionName}', {queue.Describe()}{pending}");
        report.AddWarning(
            $"Remoção interrompida ({queue.BlockReason}): {queue.Deleted.Count} objeto(s) saíram da KB e {queue.Pending.Count} continuam lá."
            + (string.IsNullOrEmpty(result.BlockDetail) ? string.Empty : " " + result.BlockDetail)
            + guidance);
        report.AddBlocked("Remover", transactionName, queue.Describe());
        if (journal.IsBlocked)
        {
            report.AddWarning($"Diário de operação: {journal.BlockDetail}");
        }
    }

    private static Transaction? ResolveTransactionForCommand(CommandData data, KnowledgeBase knowledgeBase, string commandLabel)
    {
        var transaction = TryResolveTransactionFromContext(data);
        if (transaction is not null)
        {
            var transactionGuid = transaction.Guid;
            transaction = Transaction.GetAll(knowledgeBase.DesignModel)
                .SingleOrDefault(item => item.Guid == transactionGuid);
            if (transaction is null)
            {
                throw new InvalidOperationException($"A Transaction do menu de contexto nao foi reencontrada na Knowledge Base ativa ({commandLabel}).");
            }

            return transaction;
        }

        if (!UIServices.IsSelectObjectDialogAvailable)
        {
            throw new InvalidOperationException($"O dialogo publico de selecao nao esta disponivel nesta IDE ({commandLabel}).");
        }

        var options = new SelectObjectOptions
        {
            MultipleSelection = false,
            DialogTitle = $"Selecionar Transaction para {commandLabel}",
            SupportCreateAction = false
        };
        options.ObjectTypes.Add(KBObjectDescriptor.Get<Transaction>());

        var selectedObject = UIServices.SelectObjectDialog.SelectObject(options);
        if (selectedObject is null)
        {
            return null;
        }

        if (selectedObject is not Transaction selectedTransaction)
        {
            throw new InvalidOperationException($"A selecao retornada nao e uma Transaction ({commandLabel}).");
        }

        var selectedGuid = selectedTransaction.Guid;
        transaction = Transaction.GetAll(knowledgeBase.DesignModel)
            .SingleOrDefault(item => item.Guid == selectedGuid);
        if (transaction is null)
        {
            throw new InvalidOperationException($"A Transaction selecionada nao foi reencontrada na Knowledge Base ativa ({commandLabel}).");
        }

        return transaction;
    }

    private static bool QueryOpenWizardStepOne(CommandData data, ref CommandStatus status)
    {
        status.Visible(true);
        return true;
    }

    private static bool ExecuteOpenWizardStepOne(CommandData data)
    {
        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        if (knowledgeBase is null)
        {
            WriteOutput("[Genexus Open API Builder][B030] Nenhuma Knowledge Base ativa foi encontrada. Abra uma KB e execute o comando novamente.");
            return true;
        }

        var texts = ExtensionLocalization.For(knowledgeBase);
        PrototypeTransactionSelectionState.ClearIfKnowledgeBaseChanged(knowledgeBase);

        var transaction = TryResolveTransactionFromContext(data);
        var selectionSource = "Contexto";
        if (transaction is not null)
        {
            var transactionGuid = transaction.Guid;
            transaction = Transaction.GetAll(knowledgeBase.DesignModel)
                .SingleOrDefault(item => item.Guid == transactionGuid);
            if (transaction is null)
            {
                ClearPrototypeWizardMemory(clearTransaction: true);
                WriteOutput("[Genexus Open API Builder][B034] A Transaction do menu de contexto não foi reencontrada na Knowledge Base ativa. Estado anterior do wizard descartado; nenhuma escolha foi persistida.");
                return true;
            }
        }
        else
        {
            selectionSource = "Seletor";

            if (!UIServices.IsSelectObjectDialogAvailable)
            {
                ClearPrototypeWizardMemory(clearTransaction: true);
                WriteOutput("[Genexus Open API Builder][B034] O diálogo público de seleção não está disponível nesta IDE. Estado anterior do wizard descartado; nenhuma escolha foi persistida.");
                return true;
            }

            var options = new SelectObjectOptions
            {
                MultipleSelection = false,
                DialogTitle = $"Selecionar Transaction para o {texts.Wizard} (B030)",
                SupportCreateAction = false
            };
            options.ObjectTypes.Add(KBObjectDescriptor.Get<Transaction>());

            var selectedObject = UIServices.SelectObjectDialog.SelectObject(options);
            if (selectedObject is null)
            {
                ClearPrototypeWizardMemory(clearTransaction: true);
                WriteOutput("[Genexus Open API Builder][B034] Nenhuma Transaction foi selecionada. Estado anterior do wizard descartado; nenhum ApiPlan foi criado e nenhuma alteracao foi feita na KB.");
                return true;
            }

            if (selectedObject is not Transaction selectedTransaction)
            {
                ClearPrototypeWizardMemory(clearTransaction: true);
                WriteOutput("[Genexus Open API Builder][B034] A seleção retornada não é uma Transaction. Estado anterior do wizard descartado; nenhuma escolha foi mantida.");
                return true;
            }

            var transactionGuid = selectedTransaction.Guid;
            transaction = Transaction.GetAll(knowledgeBase.DesignModel)
                .SingleOrDefault(item => item.Guid == transactionGuid);
            if (transaction is null)
            {
                ClearPrototypeWizardMemory(clearTransaction: true);
                WriteOutput("[Genexus Open API Builder][B034] A Transaction selecionada não foi reencontrada na Knowledge Base ativa. Estado do wizard descartado; nenhuma escolha foi persistida.");
                return true;
            }
        }

        var module = transaction.Module;
        if (module is null)
        {
            ClearPrototypeWizardMemory(clearTransaction: true);
            WriteOutput($"[Genexus Open API Builder][B034] A Transaction selecionada não possui módulo disponível: Name='{transaction.Name}'. Estado do wizard descartado; nenhuma escolha foi persistida.");
            return true;
        }

        ClearPrototypeWizardMemory(clearTransaction: false);
        PrototypeTransactionSelectionState.Store(knowledgeBase, transaction);

        PrototypeWizardPreferencesLoadResult? preferencesLoadResult = null;
        PrototypeWizardContractSnapshot? snapshot = null;
        PrototypeBusinessComponentSnapshot? businessComponentSnapshot = null;
        PrototypeWizardDialog? dialog = null;
        var openingWatch = Stopwatch.StartNew();
        using (var loading = ExtensionBusyProgressScope.Show(ResolveFinalReportOwner(), texts.BusyProgressTitleLoadingWizard, texts))
        {
            try
            {
                loading.Report("Preferências", 0, 0, "GxOpenApiBuilder_Settings");
                var prefsWatch = Stopwatch.StartNew();
                preferencesLoadResult = PrototypeWizardPreferencesStore.Load(knowledgeBase.DesignModel);
                prefsWatch.Stop();
                WriteOutput($"[Genexus Open API Builder][Prefs] {preferencesLoadResult.Status}");
                WriteOutput($"[Genexus Open API Builder][B082] Abertura PrefsMs={prefsWatch.ElapsedMilliseconds}.");
                loading.Report("Preferências", 1, 1, "GxOpenApiBuilder_Settings", prefsWatch.ElapsedMilliseconds);

                loading.ThrowIfAbortRequested();
                loading.Report("Contrato", 0, 0, transaction.Name);
                var contractWatch = Stopwatch.StartNew();
                snapshot = PrototypeWizardContractReader.Read(knowledgeBase.DesignModel, transaction);
                contractWatch.Stop();
                WriteOutput($"[Genexus Open API Builder][B082] Abertura ContratoMs={contractWatch.ElapsedMilliseconds}.");
                foreach (var line in ApiPlanMetadataVisibilityProbe.Run(knowledgeBase.DesignModel, transaction))
                {
                    WriteOutput($"[Genexus Open API Builder]{line}");
                }
                loading.Report("Contrato", 1, 1, transaction.Name, contractWatch.ElapsedMilliseconds);

                var duplicateServices = snapshot.ExistingApiContract.DuplicateServiceNames;
                if (duplicateServices.Count > 0)
                {
                    WriteOutput($"[Genexus Open API Builder][B034] Service Source do API Object declara servico duplicado: ApiName='{snapshot.ExistingApiContract.ApiName ?? "api" + transaction.Name}', Servicos='{string.Join(", ", duplicateServices)}'. O wizard usou a primeira declaracao de cada servico e nenhuma alteracao foi feita na KB; revise o API Object na IDE.");
                }

                loading.ThrowIfAbortRequested();
                loading.Report("Business Component", 0, 0, transaction.Name);
                var bcWatch = Stopwatch.StartNew();
                businessComponentSnapshot = PrototypeBusinessComponentReader.Read(transaction);
                bcWatch.Stop();
                WriteOutput($"[Genexus Open API Builder][B082] Abertura BusinessComponentMs={bcWatch.ElapsedMilliseconds}.");
                loading.Report("Business Component", 1, 1, transaction.Name, bcWatch.ElapsedMilliseconds);

                loading.ThrowIfAbortRequested();
                loading.Report("Interface", 0, 0, texts.Wizard);
                var uiWatch = Stopwatch.StartNew();
                dialog = new PrototypeWizardDialog(
                    knowledgeBase.DesignModel,
                    transaction,
                    snapshot,
                    businessComponentSnapshot,
                    preferencesLoadResult.Preferences,
                    WriteOutput,
                    texts);
                uiWatch.Stop();
                WriteOutput($"[Genexus Open API Builder][B082] Abertura InterfaceMs={uiWatch.ElapsedMilliseconds}.");
                loading.Report("Interface", 1, 1, texts.Wizard, uiWatch.ElapsedMilliseconds);
            }
            catch (ApiPlanBusyAbortedException abortEx)
            {
                openingWatch.Stop();
                WriteOutput($"[Genexus Open API Builder][B082] Abertura abortada apos {openingWatch.ElapsedMilliseconds} ms: {abortEx.Message}");
                ClearPrototypeWizardMemory(clearTransaction: true);
                dialog?.Dispose();
                return true;
            }
        }

        openingWatch.Stop();
        WriteOutput($"[Genexus Open API Builder][B082] Abertura total ate ShowDialog={openingWatch.ElapsedMilliseconds} ms.");

        // B115: a oferta vem ANTES do diálogo, e não depois de ele concluir. Sem a metadata o
        // Wizard abre bloqueado, e a única ação disponível é Cancelar — o que fazia a oferta,
        // posicionada após a conclusão, nunca ser alcançada no único cenário que ela resolve.
        var recoveryOutcome = OfferOrphanMetadataRecoveryIfEnabled(
            ResolveFinalReportOwner(),
            knowledgeBase.DesignModel,
            transaction,
            preferencesLoadResult!.Preferences,
            texts);
        if (recoveryOutcome is OrphanMetadataRecoveryOutcome.Recovered or OrphanMetadataRecoveryOutcome.Failed)
        {
            ClearPrototypeWizardMemory(clearTransaction: false);
            dialog!.Dispose();
            return true;
        }

        using (dialog!)
        {
        var wizardOwner = ResolveFinalReportOwner();
        ExtensionIdeScreenPlacement.CenterOnIdeScreen(dialog, wizardOwner);
        var result = wizardOwner is null
            ? dialog.ShowDialog()
            : dialog.ShowDialog(wizardOwner);
        var businessComponentExitStatus = dialog.BusinessComponentEnabledDuringWizard
            ? "Habilitacao de Business Component foi confirmada em memoria; a gravacao na KB ocorrera depois do preflight agregado."
            : "Nenhuma alteracao foi feita na KB.";

        // O wizard único não emite mais Retry: a primeira aba oculta Voltar. O ramo permanece
        // como defesa contra um DialogResult residual — a justificativa anterior citava os
        // diálogos B031/B032, cujo fluxo automático foi removido em 2026-09-06.
        if (result == System.Windows.Forms.DialogResult.Retry)
        {
            ClearPrototypeWizardMemory(clearTransaction: true);
            WriteOutput($"[Genexus Open API Builder][B034] Voltar acionado no início do wizard único. Transaction='{transaction.Name}' e decisões em memoria foram descartadas; nenhum ApiPlan foi criado. {businessComponentExitStatus}");
            return true;
        }

        if (result == System.Windows.Forms.DialogResult.Cancel)
        {
            ClearPrototypeWizardMemory(clearTransaction: true);
            WriteOutput($"[Genexus Open API Builder][B034] Wizard único cancelado ou fechado para Transaction='{transaction.Name}'. Transaction e decisões em memoria descartadas; nenhum ApiPlan foi criado. {businessComponentExitStatus}");
            return true;
        }

        if (result != System.Windows.Forms.DialogResult.OK || dialog.Selection is null)
        {
            ClearPrototypeWizardMemory(clearTransaction: true);
            WriteOutput($"[Genexus Open API Builder][B034] Wizard único fechado sem conclusao para Transaction='{transaction.Name}'. Estado em memoria descartado; nenhum ApiPlan foi criado. {businessComponentExitStatus}");
            return true;
        }

        var selection = dialog.Selection;
        var createRequiredCount = selection.RequiredFields.Count(item => item.RequestName == "CreateRequest" && item.IsRequired);
        var updateRequiredCount = selection.RequiredFields.Count(item => item.RequestName == "UpdateRequest" && item.IsRequired);
        var createBlockedCount = snapshot.Attributes.Count(item => !item.IsPayloadEligible);
        var updateBlockedCount = snapshot.Attributes.Count(item => !item.IsUpdatePayloadEligible);
        var filterBlockedCount = snapshot.Attributes.Count(item => !item.IsFilterEligible);
        var classifiedSensitiveCount = snapshot.Attributes.Count(item => item.IsSensitive);
        var classifiedAuditCount = snapshot.Attributes.Count(item => item.IsAudit);
        var apiPlan = ApiPlanBuilder.Build(knowledgeBase.DesignModel, transaction, selection);
        var b111ManagedApply = selection.GenerateApiObject || selection.GenerateMetadata || selection.ApplyBusinessComponent || selection.ApplyList;
        var applyOwner = ResolveFinalReportOwner();
        var sdtGenerationPlan = ApiPlanSdtGenerationPlanBuilder.Create(apiPlan);
        var classificationConfiguration = apiPlan.FieldClassificationConfiguration;
        var classificationMetadataContract = classificationConfiguration.MetadataContract;
        var serviceDescriptionsPendingCount = apiPlan.ServiceDescriptions.Count(item => string.Equals(item.Description, ApiPlan.UnresolvedB056ServiceDescription, StringComparison.Ordinal));
        var serviceDescriptionsResolvedCount = apiPlan.ServiceDescriptions.Count - serviceDescriptionsPendingCount;
        PrototypeWizardFlowSessionState.Store(selection);
        PrototypeWizardSessionState.StoreContractSelection(selection.ContractSelection);
        PrototypeWizardReviewSessionState.StoreReviewSelection(selection.ReviewSelection);
        ApiPlanSessionState.Store(apiPlan);
        if (System.Diagnostics.Debugger.IsAttached)
        {
        WriteOutput($"[Genexus Open API Builder][B030] Wizard único concluido em memoria: Transaction='{transaction.Name}', Module='{module.Name}', SelectionSource='{selectionSource}'.");
        WriteOutput($"[Genexus Open API Builder][B031] Contrato de API da Transacao='{transaction.Name}' em memoria: Services='{string.Join(",", selection.ContractSelection.SelectedServices)}', Create={selection.ContractSelection.CreateFields.Count}, Update={selection.ContractSelection.UpdateFields.Count}, Response={selection.ContractSelection.ResponseFields.Count}, ListFilters={selection.ContractSelection.ListFilters.Count}.");
        WriteOutput($"[Genexus Open API Builder][B032] Paths, segurança e paginacao em memoria: ApiName='{selection.ReviewSelection.ApiName}', ServicesBasePath='{selection.ReviewSelection.ServicesBasePath}', RestPath='{selection.ReviewSelection.RestPath}', SecurityLevel='{selection.ReviewSelection.SecurityLevel}', DefaultPageSize={selection.ReviewSelection.DefaultPageSize}, MaximumPageSize={selection.ReviewSelection.MaximumPageSize}.");
        WriteOutput($"[Genexus Open API Builder][B033] Obrigatoriedade em memoria: CreateRequired={createRequiredCount}, UpdateRequired={updateRequiredCount}. Required marca membro obrigatorio no payload, recusado com 400 quando ausente ou com o valor default do tipo (vazio, false ou 0).");
        WriteOutput($"[Genexus Open API Builder][B037] Obrigatorio no payload consolidado: CreateRequired={createRequiredCount}, UpdateRequired={updateRequiredCount}. Create/Update respondem 400 quando o obrigatorio chega ausente ou com o valor default do tipo; o GeneXus nao expoe presenca de membro JSON sem comando csharp. UpdateRequest segue PUT completo.");
        WriteOutput($"[Genexus Open API Builder][B036] Campos bloqueados visiveis no wizard: CreateRequest={createBlockedCount}, UpdateRequest={updateBlockedCount}, ListFilters={filterBlockedCount}. Itens bloqueados ficaram desmarcados, com motivo, e nao podem ser selecionados.");
        WriteOutput($"[Genexus Open API Builder][B090/B091] Classificacao em memoria: SensitiveFields={classifiedSensitiveCount}, AuditFields={classifiedAuditCount}. ConfigScope='{classificationConfiguration.Scope}', ConfigSource='{classificationConfiguration.Source}', ConfigStatus='{classificationConfiguration.Status}', PersistedMetadata={classificationConfiguration.IsPersistedMetadata}, KbConfigured={classificationConfiguration.IsKnowledgeBaseConfigured}, SensitiveRules={classificationConfiguration.SensitiveExactNames.Count}, AuditRules={classificationConfiguration.AuditSuffixes.Count}. Contrato por KB preparado no ApiPlan, ainda sem metadata persistente e sem geracao.");
        WriteOutput($"[Genexus Open API Builder][B090/B091] Metadata futura no ApiPlan: SchemaVersion='{classificationMetadataContract.SchemaVersion}', Section='{classificationMetadataContract.SectionName}', SensitiveMember='{classificationMetadataContract.SensitiveExactNamesMember}', AuditExactMember='{classificationMetadataContract.AuditExactNamesMember}', AuditSuffixMember='{classificationMetadataContract.AuditSuffixesMember}', RequiredMembers={classificationMetadataContract.RequiredMembers.Count}. Ainda sem ler ou gravar File de metadata.");
        WriteOutput($"[Genexus Open API Builder][B035] Business Component em memoria: IsBusinessComponent={selection.BusinessComponentSelection.IsBusinessComponent}, EnabledDuringWizard={selection.BusinessComponentSelection.EnabledDuringWizard}, Status='{selection.BusinessComponentSelection.Status}'.");
        WriteOutput($"[Genexus Open API Builder][B038] ApiPlan em memoria criado: Transaction='{apiPlan.TransactionName}', ModuleTarget='{apiPlan.ModuleTarget}', ApiName='{apiPlan.ApiName}', MetadataFile='{apiPlan.MetadataFileName}', EndpointsCount={apiPlan.EndpointsCount}.");
        WriteOutput($"[Genexus Open API Builder][B038] ApiPlan cobre: PrimaryKey={apiPlan.PrimaryKey.Count}, CreateFields={apiPlan.CreateRequestFields.Count}, UpdateFields={apiPlan.UpdateRequestFields.Count}, ResponseFields={apiPlan.ResponseFields.Count}, ListFilters={apiPlan.ListFilters.Count}, RequiredFields={apiPlan.RequiredFields.Count}, Procedures={apiPlan.ProcedureNames.Count}, SharedSdts={apiPlan.SharedSdtNames.Count}. Sem persistir metadata e sem gerar SDT, Procedure, API Object ou File na KB.");
        WriteOutput($"[Genexus Open API Builder][Sprint4] Preview de engine SDT: Phase='{sdtGenerationPlan.Phase}', Status='{sdtGenerationPlan.Status}', WritesKnowledgeBase={sdtGenerationPlan.WritesKnowledgeBase}, OwnSdts={sdtGenerationPlan.OwnSdts.Count}, SharedSdts={sdtGenerationPlan.SharedSdts.Count}. Sem criar, alterar ou excluir objetos na KB.");
        foreach (var sdt in sdtGenerationPlan.SharedSdts.Concat(sdtGenerationPlan.OwnSdts))
        {
            WriteOutput($"[Genexus Open API Builder][Sprint4] SDT planejado: Backlog='{sdt.BacklogId}', Kind='{sdt.Kind}', Name='{sdt.Name}', Scope='{sdt.Scope}', Members={sdt.Members.Count}.");
        }

        WriteOutput($"[Genexus Open API Builder][Sprint3] Campos de engine no ApiPlan: GeneratorTarget='{apiPlan.GeneratorTarget}' como gerador prioritario inicial do MVP, ConflictMode='{apiPlan.ConflictMode}' para colisao externa/incompativel, ReexecutionMode='{apiPlan.ReexecutionMode}', ServiceDescriptionsPending={serviceDescriptionsPendingCount}/{apiPlan.ServiceDescriptions.Count}, ServiceDescriptionLanguage='{apiPlan.ServiceDescriptionLanguage}', ServiceDescriptionFallbackUsed={apiPlan.ServiceDescriptionFallbackUsed}, IsEngineReady={apiPlan.IsEngineReady}. Sem validar engine real e sem gerar objetos.");
        WriteOutput($"[Genexus Open API Builder][B056] Descricoes no ApiPlan: Resolved={serviceDescriptionsResolvedCount}/{apiPlan.ServiceDescriptions.Count}, Language='{apiPlan.ServiceDescriptionLanguage}', LanguageSource='{apiPlan.ServiceDescriptionLanguageSource}', FallbackUsed={apiPlan.ServiceDescriptionFallbackUsed}, FallbackReason='{apiPlan.ServiceDescriptionFallbackReason}'. Sem aplicar [Description] em objeto API real e sem gerar objetos.");
        WriteOutput($"[Genexus Open API Builder][B092] Seguranca no ApiPlan: SecurityLevel='{apiPlan.Security.SecurityLevel}', GamCondition='{apiPlan.Security.GamCondition}', RequiresGenerationConfirmation={apiPlan.Security.RequiresGenerationConfirmation}. Sem aplicar seguranca em objetos reais.");
        WriteOutput($"[Genexus Open API Builder][B034] Wizard concluido sem acionar cancelamento. Decisoes e ApiPlan permanecem em memoria. GenerateSdts={selection.GenerateSdts}, GenerateProcedures={selection.GenerateProcedures}, GenerateApiObject={selection.GenerateApiObject}, GenerateMetadata={selection.GenerateMetadata}, ApplyList={selection.ApplyList}, ApplyBusinessComponent={selection.ApplyBusinessComponent}; escritas confirmadas no wizard exigem preflight completo antes de qualquer Save().");
        }
        var applyFromConfirm = Stopwatch.StartNew();
        var phaseWatch = Stopwatch.StartNew();
        var report = new ApiPlanApplicationFinalReportCollector("Wizard", transaction.Name, apiPlan.ApiName);
        var stopwatch = Stopwatch.StartNew();
        // B082: mede o custo das varreduras de catalogo ao longo de todo o Apply.
        var scanTelemetry = new ApiPlanScanTelemetry();
        using var scanScope = ApiPlanScanProbe.Begin(scanTelemetry);
        var saveBoundaryLog = new ApiPlanSaveBoundaryLog();
        using var saveBoundaryScope = ApiPlanSaveBoundaryProbe.Begin(saveBoundaryLog);
        using var saveBoundaryPublisher = new ApiPlanSaveBoundaryPublisher(saveBoundaryLog, "Wizard");
        var persistenceLog = new ApiPlanPersistenceLog();
        using var persistenceScope = ApiPlanSaveBoundaryProbe.BeginPersistence(persistenceLog);
        report.SetPersistenceLog(persistenceLog);
        // B111/F3: o diário vive fora do try para que o aborto do usuário também seja
        // registrado como interrupção, e não desapareça com o escopo.
        ApiPlanOperationJournalSession? journal = null;
        var suppressProgressPump = preferencesLoadResult!.Preferences.SuppressProgressPumpDuringSaves;
        try
        {
            using var busy = ExtensionBusyProgressScope.Show(applyOwner, texts.BusyProgressTitleApply, texts, suppressProgressPump);
            WriteOutput($"[Genexus Open API Builder][B082] Apply Wizard iniciado: Transaction='{transaction.Name}'.");
            if (suppressProgressPump)
            {
                // Sem esta linha, um Apply com a tela congelada chega ao log indistinguivel
                // de um Apply travado.
                WriteOutput("[Genexus Open API Builder][B109] Atualizacao da tela suprimida por preferencia durante as gravacoes: a janela nao responde ate o fim, e o botao Abortar fica inativo. Experimento da hipotese de reentrancia do Pump.");
            }
            var (generationState, kbIndexForApply) = ApiPlanGenerationStateReader.ReadForIntentionalChangeWithIndex(
                knowledgeBase.DesignModel,
                transaction,
                apiPlan,
                busy.Session);
            WriteProbePhase("IndiceKb", phaseWatch.ElapsedMilliseconds);
            WriteApiObjectBaselineDiagnostic(generationState);
            var preflightScope = ApiPlanWritePreflightScope.FromSelection(
                selection.GenerateSdts,
                selection.GenerateProcedures,
                selection.GenerateApiObject,
                selection.GenerateMetadata,
                selection.ApplyList,
                selection.ApplyBusinessComponent);
            var blockedGenerationStages = new[]
                {
                    preflightScope.RequireSdts ? generationState.Sdts : null,
                    preflightScope.RequireProcedures ? generationState.Procedures : null,
                    preflightScope.RequireApiObject ? generationState.ApiObject : null,
                    preflightScope.RequireMetadataFile ? generationState.MetadataFile : null,
                }
                .Where(stage => stage is not null)
                .Cast<ApiPlanGenerationStageState>()
                .Where(stage => stage.IsBlocked)
                .ToArray();
            if (blockedGenerationStages.Length > 0)
            {
                var collisions = generationState.CollectCollisionConflicts(
                    preflightScope.RequireSdts,
                    preflightScope.RequireProcedures,
                    preflightScope.RequireApiObject,
                    preflightScope.RequireMetadataFile);
                var collisionText = collisions.Count == 0
                    ? string.Empty
                    : Environment.NewLine + ApiPlanCollisionConflict.FormatList(collisions);
                WriteOutput($"[Genexus Open API Builder][B063/B064/B067] Estado bloqueado detectado no wizard antes de confirmar escrita: Transaction='{transaction.Name}', BlockedStages='{string.Join(",", blockedGenerationStages.Select(stage => stage.StageName))}', Details='{string.Join(" | ", blockedGenerationStages.Select(stage => stage.Detail))}'{collisionText}. Nenhum Save foi solicitado.");
            }
            if (!b111ManagedApply && !selection.GenerateSdts && !selection.GenerateProcedures)
            {
                WriteOutput($"[Genexus Open API Builder][B040-B046/B060] Nenhuma etapa de escrita foi confirmada no wizard para Transaction='{transaction.Name}'. Nenhuma escrita foi solicitada.");
                return true;
            }

            AppendPlanWarnings(report, apiPlan);
            AppendTransactionFolderWarning(report, generationState);
            foreach (var stage in new[] { generationState.Sdts, generationState.Procedures, generationState.ApiObject, generationState.MetadataFile }
                .Where(stage => stage.IsBlocked))
            {
                report.AddWarning($"Etapa '{stage.StageName}' bloqueada na KB: {stage.Detail}");
            }

            AppendCollisionConflictsToReport(
                report,
                generationState.CollectCollisionConflicts(
                    preflightScope.RequireSdts,
                    preflightScope.RequireProcedures,
                    preflightScope.RequireApiObject,
                    preflightScope.RequireMetadataFile));

            busy.Report("Validando", 0, 0, "Preflight");
            busy.Session.PumpAndThrowIfAbortRequested();
            phaseWatch.Restart();
            try
            {
                PrototypeWizardBusinessComponentNavigationPolicy.ThrowIfDeleteWithoutBusinessComponent(
                    apiPlan.Services.Select(service => service.Name),
                    selection.ApplyBusinessComponent);
                ApiPlanWritePreflight.ValidateForIntentionalChange(
                    knowledgeBase.DesignModel,
                    transaction,
                    apiPlan,
                    preflightScope.RequireSdts,
                    preflightScope.RequireProcedures,
                    preflightScope.RequireApiObject,
                    preflightScope.RequireMetadataFile,
                    kbIndexForApply);
                ApiPlanWritePreflight.ValidateForF1(
                    knowledgeBase.DesignModel,
                    transaction,
                    apiPlan,
                    selection.GenerateSdts,
                    selection.GenerateProcedures,
                    selection.GenerateApiObject,
                    selection.GenerateMetadata,
                    selection.ApplyList,
                    selection.ApplyBusinessComponent,
                    kbIndexForApply,
                    businessComponentEnablementPending: selection.BusinessComponentSelection.EnabledDuringWizard);
            }
            catch (Exception ex) when (ex is not ApiPlanBusyAbortedException)
            {
                WriteProbePhase("PreflightAgregado", phaseWatch.ElapsedMilliseconds);
                WriteOutput($"[Genexus Open API Builder][B063/B064/B067] Preflight agregado bloqueou o wizard antes do primeiro Save(): Transaction='{transaction.Name}', Error='{ex.Message}'");
                if (!report.HasInterrupted)
                {
                    report.AddBlocked("Preflight", "B063/B064/B067", ex.Message);
                }

                stopwatch.Stop();
                WriteApplyScanTelemetry(scanTelemetry, applyFromConfirm.ElapsedMilliseconds);
                ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, apiPlan);
                return true;
            }

            WriteProbePhase("PreflightAgregado", phaseWatch.ElapsedMilliseconds);
            WriteOutput($"[Genexus Open API Builder][B063/B064/B067] Preflight agregado aprovado antes do primeiro Save(): Transaction='{transaction.Name}', ConflictMode='{apiPlan.ConflictMode}', ReexecutionMode='{apiPlan.ReexecutionMode}'.");

            // B111/F3: a intenção fica durável antes de qualquer gravação de negócio — a
            // habilitação de Business Component logo abaixo já é uma delas.
            phaseWatch.Restart();
            var journalStart = ApiPlanOperationJournalSession.Start(
                knowledgeBase.DesignModel,
                kbIndexForApply,
                knowledgeBase.Guid,
                transaction,
                JournalOperationKind.Apply,
                ApiPlanOperationJournalPlans.ForGeneration(
                    apiPlan.PlannedApiGuid,
                    ApiPlanMetadataFileWriter.ComputePlannedContractHash(apiPlan),
                    selection.GenerateApiObject,
                    selection.GenerateSdts,
                    selection.GenerateProcedures,
                    selection.GenerateMetadata,
                    apiPlan.Services.Select(service => service.Name)),
                apiPlan.ApplicationId,
                JournalIntentKind.Current,
                selection.GenerateMetadata ? ApiPlanMetadataFileWriter.SchemaVersion : null);
            WriteProbePhase("DiarioAbertura", phaseWatch.ElapsedMilliseconds);
            if (!journalStart.IsStarted)
            {
                WriteOutput($"[Genexus Open API Builder][B111/F3] Apply bloqueado pelo diário durável: Transaction='{transaction.Name}'. {journalStart.Detail} Nenhuma gravação foi solicitada.");
                report.AddBlocked("Diário de operação", "B111/F3", journalStart.Detail);
                stopwatch.Stop();
                WriteApplyScanTelemetry(scanTelemetry, applyFromConfirm.ElapsedMilliseconds);
                ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, apiPlan);
                OfferRecoveryAfterJournalBlock(knowledgeBase, texts, journalStart);
                return true;
            }

            journal = journalStart.Session!;
            journal.AttachPersistence(persistenceLog, () => report.CreatedObjectNames);
            WriteJournalDiagnostics(journal);

            if (selection.BusinessComponentSelection.EnabledDuringWizard && !transaction.IsBusinessComponent)
            {
                try
                {
                    PersistBusinessComponentEnablement(transaction);
                }
                catch (Exception ex) when (ex is not ApiPlanBusyAbortedException)
                {
                    WriteOutput($"[Genexus Open API Builder][B035] Habilitacao de Business Component falhou depois do preflight agregado: Transaction='{transaction.Name}', Error='{ex.Message}'. Nenhum objeto dependente sera persistido.");
                    report.AddBlocked("Business Component", "B035", ex.Message);
                    InterruptJournal(journal, report, JournalBlockReason.StageFailed);
                    stopwatch.Stop();
                    WriteApplyScanTelemetry(scanTelemetry, applyFromConfirm.ElapsedMilliseconds);
                    ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, apiPlan, persistenceLog);
                    return true;
                }
            }

            phaseWatch.Restart();
            var sdtsReady = true;
            if (selection.GenerateSdts)
            {
                sdtsReady = TryCreateSdts(knowledgeBase.DesignModel, transaction, apiPlan, "Wizard", report: report, progress: busy.Session, kbIndex: kbIndexForApply);
            }
            else if (selection.GenerateProcedures || selection.GenerateApiObject || selection.GenerateMetadata || selection.ApplyList || selection.ApplyBusinessComponent)
            {
                WriteOutput($"[Genexus Open API Builder][B040-B046] Etapa de SDTs nao confirmada no wizard para Transaction='{transaction.Name}'. A dependencia sera reencontrada e validada pelo preflight da etapa seguinte.");
            }

            WriteProbePhase("SDTs", phaseWatch.ElapsedMilliseconds);
            if (sdtsReady && selection.GenerateSdts)
            {
                phaseWatch.Restart();
                kbIndexForApply.RefreshSdts(knowledgeBase.DesignModel);
                WriteProbePhase("IndiceSdtAposGravacao", phaseWatch.ElapsedMilliseconds);
            }

            if (!sdtsReady)
            {
                if (selection.GenerateProcedures)
                {
                    WriteOutput($"[Genexus Open API Builder][{ApiPlanProcedureWriter.FormatOutputStage(apiPlan)}] Etapa de Procedures nao executada pelo wizard para Transaction='{transaction.Name}' porque B040-B046 falhou ou foi bloqueado neste fluxo. Nenhuma Procedure foi criada pelo wizard.");
                }

                if (selection.GenerateApiObject)
                {
                    WriteOutput($"[Genexus Open API Builder][B054] Etapa de API Object nao executada pelo wizard para Transaction='{transaction.Name}' porque B040-B046 falhou ou foi bloqueado neste fluxo. Nenhum API Object foi criado pelo wizard.");
                }

                if (selection.ApplyBusinessComponent)
                {
                    WriteOutput($"[Genexus Open API Builder][B071-B073/B079] REST via Business Component nao foi aplicado para Transaction='{transaction.Name}' porque os SDTs requeridos falharam ou foram bloqueados neste fluxo.");
                }

                if (selection.ApplyList)
                {
                    WriteOutput($"[Genexus Open API Builder][B070] List nao foi aplicado para Transaction='{transaction.Name}' porque B040-B046 falhou ou foi bloqueado neste fluxo.");
                }

                if (selection.GenerateMetadata)
                {
                    WriteOutput($"[Genexus Open API Builder][B060] Metadata nao foi gravada para Transaction='{transaction.Name}' porque B040-B046 falhou ou foi bloqueado neste fluxo.");
                }

                InterruptJournal(journal, report, JournalBlockReason.StageFailed);
                stopwatch.Stop();
                WriteApplyScanTelemetry(scanTelemetry, applyFromConfirm.ElapsedMilliseconds);
                ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, apiPlan);
                return true;
            }

            phaseWatch.Restart();
            var proceduresReady = true;
            if (selection.GenerateProcedures)
            {
                proceduresReady = TryCreateProcedures(knowledgeBase.DesignModel, transaction, apiPlan, "Wizard", kbIndexForApply, report, busy.Session);
            }
            else if (selection.GenerateApiObject || selection.GenerateMetadata || selection.ApplyList || selection.ApplyBusinessComponent)
            {
                WriteOutput($"[Genexus Open API Builder][{ApiPlanProcedureWriter.FormatOutputStage(apiPlan)}] Etapa de Procedures nao confirmada no wizard para Transaction='{transaction.Name}'. A dependencia sera reencontrada e validada pelo preflight da etapa seguinte.");
            }

            WriteProbePhase("Procedures", phaseWatch.ElapsedMilliseconds);
            if (!proceduresReady)
            {
                if (selection.GenerateApiObject)
                {
                    WriteOutput($"[Genexus Open API Builder][B054] Etapa de API Object nao executada pelo wizard para Transaction='{transaction.Name}' porque a etapa de Procedures falhou ou foi bloqueada neste fluxo. Nenhum API Object foi criado pelo wizard.");
                }

                if (selection.ApplyBusinessComponent)
                {
                    WriteOutput($"[Genexus Open API Builder][B071-B073/B079] REST via Business Component nao foi aplicado para Transaction='{transaction.Name}' porque as Procedures requeridas falharam ou foram bloqueadas neste fluxo.");
                }

                if (selection.ApplyList)
                {
                    WriteOutput($"[Genexus Open API Builder][B070] List nao foi aplicado para Transaction='{transaction.Name}' porque a etapa de Procedures falhou ou foi bloqueada neste fluxo.");
                }

                if (selection.GenerateMetadata)
                {
                    WriteOutput($"[Genexus Open API Builder][B060] Metadata nao foi gravada para Transaction='{transaction.Name}' porque a etapa de Procedures falhou ou foi bloqueada neste fluxo.");
                }

                InterruptJournal(journal, report, JournalBlockReason.StageFailed);
                stopwatch.Stop();
                WriteApplyScanTelemetry(scanTelemetry, applyFromConfirm.ElapsedMilliseconds);
                ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, apiPlan);
                return true;
            }

            busy.ThrowIfAbortRequested();
            phaseWatch.Restart();
            var apiObjectReady = true;
            ApiPlanTransientApiContext? wizardApiContext = null;
            var wizardHasConsumers = selection.ApplyBusinessComponent || selection.ApplyList;
            if (wizardHasConsumers)
            {
                var wizardFinalWriter = selection.ApplyList ? "List" : "Business Component";
                busy.Report("API Object", 0, 0, apiPlan.ApiName);
                var apiMs = busy.Measure(() =>
                {
                    apiObjectReady = TryPrepareApiObject(
                        knowledgeBase.DesignModel,
                        transaction,
                        apiPlan,
                        "Wizard",
                        kbIndexForApply,
                        allowIntentionalContractRefresh: true,
                        persistApiObject: selection.GenerateApiObject,
                        businessComponentParticipated: selection.ApplyBusinessComponent,
                        finalWriter: wizardFinalWriter,
                        progress: busy.Session,
                        report: report,
                        selection: new ApiPlanTransientApiSelection(selection.GenerateSdts, selection.GenerateProcedures, selection.GenerateApiObject, selection.GenerateMetadata, selection.ApplyList, selection.ApplyBusinessComponent),
                        preserveSdtNames: null,
                        context: out wizardApiContext);
                });
                busy.Report("API Object", 1, 1, apiPlan.ApiName, apiMs);
            }
            else if (selection.GenerateApiObject)
            {
                busy.Report("API Object", 0, 0, apiPlan.ApiName);
                var apiMs = busy.Measure(() =>
                {
                    apiObjectReady = TryCreateApiObject(
                        knowledgeBase.DesignModel,
                        transaction,
                        apiPlan,
                        "Wizard",
                        kbIndexForApply,
                        busy.Session,
                        report,
                        allowIntentionalContractRefresh: true);
                });
                busy.Report("API Object", 1, 1, apiPlan.ApiName, apiMs);
            }
            else if (selection.ApplyBusinessComponent)
            {
                WriteOutput($"[Genexus Open API Builder][B054] Etapa de API Object nao confirmada no wizard para Transaction='{transaction.Name}'. A dependencia sera reencontrada e validada pelo preflight de Business Component.");
            }

            WriteProbePhase("ApiObject", phaseWatch.ElapsedMilliseconds);
            if (!apiObjectReady)
            {
                if (selection.ApplyBusinessComponent)
                {
                    WriteOutput($"[Genexus Open API Builder][B071-B073/B079] REST via Business Component nao foi aplicado para Transaction='{transaction.Name}' porque o API Object falhou ou foi bloqueado neste fluxo.");
                }

                if (selection.ApplyList)
                {
                    WriteOutput($"[Genexus Open API Builder][B070] List nao foi aplicado para Transaction='{transaction.Name}' porque B054 falhou ou foi bloqueado neste fluxo.");
                }

                if (selection.GenerateMetadata)
                {
                    WriteOutput($"[Genexus Open API Builder][B060] Metadata nao foi gravada para Transaction='{transaction.Name}' porque B054 falhou ou foi bloqueado neste fluxo.");
                }

                InterruptJournal(journal, report, JournalBlockReason.StageFailed);
                stopwatch.Stop();
                WriteApplyScanTelemetry(scanTelemetry, applyFromConfirm.ElapsedMilliseconds);
                ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, apiPlan);
                return true;
            }

            busy.ThrowIfAbortRequested();
            phaseWatch.Restart();
            var businessComponentReady = true;
            if (selection.ApplyBusinessComponent)
            {
                businessComponentReady = TryApplyBusinessComponent(
                    knowledgeBase.DesignModel,
                    transaction,
                    apiPlan,
                    "Wizard",
                    kbIndexForApply,
                    allowIntentionalContractRefresh: true,
                    report: report,
                    progress: busy.Session,
                    apiContext: wizardApiContext);
            }

            WriteProbePhase("BusinessComponent", phaseWatch.ElapsedMilliseconds);
            if (!businessComponentReady)
            {
                if (selection.GenerateMetadata)
                {
                    WriteOutput($"[Genexus Open API Builder][B060] Metadata nao foi gravada para Transaction='{transaction.Name}' porque B071-B073/B079 falhou ou foi bloqueado neste fluxo.");
                }

                InterruptJournal(journal, report, JournalBlockReason.StageFailed);
                stopwatch.Stop();
                WriteApplyScanTelemetry(scanTelemetry, applyFromConfirm.ElapsedMilliseconds);
                ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, apiPlan);
                return true;
            }

            busy.ThrowIfAbortRequested();
            phaseWatch.Restart();
            var listReady = true;
            if (selection.ApplyList)
            {
                listReady = TryApplyList(
                    knowledgeBase.DesignModel,
                    transaction,
                    apiPlan,
                    "Wizard",
                    kbIndexForApply,
                    allowIntentionalContractRefresh: true,
                    report: report,
                    progress: busy.Session,
                    apiContext: wizardApiContext);
            }

            WriteProbePhase("List", phaseWatch.ElapsedMilliseconds);
            if (!listReady)
            {
                if (selection.GenerateMetadata)
                {
                    WriteOutput($"[Genexus Open API Builder][B060] Metadata nao foi gravada para Transaction='{transaction.Name}' porque B070 falhou ou foi bloqueado neste fluxo.");
                }

                InterruptJournal(journal, report, JournalBlockReason.StageFailed);
                stopwatch.Stop();
                WriteApplyScanTelemetry(scanTelemetry, applyFromConfirm.ElapsedMilliseconds);
                ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, apiPlan);
                return true;
            }

            busy.ThrowIfAbortRequested();
            phaseWatch.Restart();
            if (selection.GenerateMetadata)
            {
                busy.Report("Metadata", 0, 0, apiPlan.MetadataFileName);
                var metaMs = busy.Measure(() =>
                {
                    TryWriteMetadataFile(
                        knowledgeBase.DesignModel,
                        transaction,
                        apiPlan,
                        "Wizard",
                        kbIndexForApply,
                        allowIntentionalContractRefresh: true,
                        report: report);
                });
                busy.Report("Metadata", 1, 1, apiPlan.MetadataFileName, metaMs);
            }

            WriteProbePhase("Metadata", phaseWatch.ElapsedMilliseconds);
            // Fronteira terminal: com API persistido, o diário registra antes o
            // ApiPhysicallySaved — é o que impede qualquer recuperação de repetir a
            // gravação do API Object.
            CompleteJournal(journal, report, wizardApiContext?.PlannedApiGuid ?? report.PersistedMainObjectGuid ?? apiPlan.PlannedApiGuid);
        }
        catch (ApiPlanBusyAbortedException abortEx)
        {
            WriteOutput($"[Genexus Open API Builder][B082] Apply Wizard abortado: Transaction='{transaction.Name}', Error='{abortEx.Message}'");
            report.HeadlineOverride = "Aplicação abortada pelo usuário.";
            report.AddWarning(abortEx.Message);
            report.AddBlocked("Apply", transaction.Name, "Abortado [B082]");
            // B111/F3 P3: aborto é decisão do usuário, não falha de etapa.
            InterruptJournal(journal, report, JournalBlockReason.UserAborted);
            stopwatch.Stop();
            WriteApplyScanTelemetry(scanTelemetry, applyFromConfirm.ElapsedMilliseconds);
            ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, apiPlan);
            return true;
        }

        stopwatch.Stop();
        WriteApplyScanTelemetry(scanTelemetry, applyFromConfirm.ElapsedMilliseconds);
        ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, apiPlan);
        return true;
        } // using dialog [B082]
    }

    /// <summary>
    /// Publica na Output o que o diário registrou desde a última publicação. As linhas são
    /// drenadas para não repetir o mesmo diagnóstico a cada fronteira.
    /// </summary>
    private static void WriteJournalDiagnostics(ApiPlanOperationJournalSession? journal)
    {
        if (journal is null)
        {
            return;
        }

        foreach (var line in journal.DrainDiagnostics())
        {
            WriteOutput($"[Genexus Open API Builder][B111/F3] {line}");
        }
    }

    /// <summary>
    /// Fecha o diário numa interrupção. A intenção e os checkpoints já duráveis permanecem:
    /// é isso que permite dizer depois o que ficou pela metade.
    /// </summary>
    private static void InterruptJournal(
        ApiPlanOperationJournalSession? journal,
        ApiPlanApplicationFinalReportCollector report,
        JournalBlockReason blockReason)
    {
        if (journal is null)
        {
            return;
        }

        journal.Interrupt(JournalOperationState.Partial, blockReason);
        WriteJournalDiagnostics(journal);
        if (journal.IsBlocked)
        {
            report.AddWarning($"Diário de operação: {journal.BlockDetail}");
        }
    }

    /// <summary>
    /// Fecha o diário no caminho feliz. Quando o API Object foi persistido, registra antes a
    /// fronteira que impede qualquer recuperação de repetir essa gravação.
    ///
    /// B111/F3 P3: a fronteira exige **gravação confirmada**, não apenas um GUID conhecido.
    /// Medido na IDE em 2026-09-14: um Apply de SDTs e Procedures, com a etapa de API Object
    /// desmarcada, registrava `ApiPhysicallySaved` com `ApiSaveCount=0` — o GUID vinha do
    /// objeto apenas identificado na KB. Afirmar essa fronteira sem gravação faz a
    /// recuperação recusar justamente o passo que precisaria executar.
    /// </summary>
    private static void CompleteJournal(
        ApiPlanOperationJournalSession? journal,
        ApiPlanApplicationFinalReportCollector report,
        Guid? persistedApiGuid)
    {
        if (journal is null)
        {
            return;
        }

        var apiPhysicallySaved = report.ApiSaveCount > 0;
        if (apiPhysicallySaved && persistedApiGuid.HasValue && persistedApiGuid.Value != Guid.Empty)
        {
            journal.SetPlannedApiGuid(persistedApiGuid.Value);
            journal.NoteApiPhysicallySaved();
        }

        journal.Complete();
        WriteJournalDiagnostics(journal);
        if (journal.IsBlocked)
        {
            report.AddWarning($"Diário de operação: {journal.BlockDetail}");
        }
    }

    private static bool PersistBusinessComponentEnablement(Transaction transaction)
    {
        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        if (!transaction.IsBusinessComponent)
        {
            var receipt = ApiPlanSaveBoundaryProbe.Persist(
                PersistenceFaultPoint.BusinessComponentEnablementSave,
                "Save",
                "Transaction",
                "Business Component",
                transaction.Name,
                new GuidIdentity(transaction.Guid),
                () =>
                {
                    transaction.SetPropertyValue("idISBUSINESSCOMPONENT", true);
                    transaction.Save();
                },
                () => transaction.IsBusinessComponent
                    ? PersistenceConfirmation.Confirmed(transaction.Guid.ToString())
                    : PersistenceConfirmation.Divergent(transaction.Guid.ToString(), "A Transaction persistida continua sem Business Component."));
            if (receipt is not null && receipt.Outcome != PersistenceOutcome.Confirmed)
            {
                throw new InvalidOperationException(
                    $"Persistência da habilitação de Business Component não foi confirmada: Outcome='{receipt.Outcome}', Confirmation='{receipt.Confirmation}', Detail='{receipt.ConfirmationDetail}'.");
            }

            if (!transaction.IsBusinessComponent)
            {
                throw new InvalidOperationException("A Transaction continuou com Business Component desabilitado após o Save.");
            }
        }

        return transaction.IsBusinessComponent;
    }

    private static void ClearPrototypeWizardMemory(bool clearTransaction)
    {
        PrototypeWizardFlowSessionState.Clear();
        PrototypeWizardSessionState.ClearContractSelection();
        PrototypeWizardReviewSessionState.ClearReviewSelection();
        ApiPlanSessionState.Clear();
        if (clearTransaction)
        {
            PrototypeTransactionSelectionState.Clear();
        }
    }

    private enum OrphanMetadataRecoveryOutcome
    {
        NotOffered,
        Declined,
        Recovered,
        Failed,
    }

    private static OrphanMetadataRecoveryOutcome OfferOrphanMetadataRecoveryIfEnabled(
        System.Windows.Forms.IWin32Window? owner,
        KBModel designModel,
        Transaction transaction,
        PrototypeWizardPreferences preferences,
        ExtensionTexts texts)
    {
        if (!preferences.OfferOrphanMetadataRecovery)
        {
            return OrphanMetadataRecoveryOutcome.NotOffered;
        }

        OrphanMetadataRecoveryPlan plan;
        string eligibilityDetail;
        try
        {
            if (!ApiPlanOrphanMetadataRecovery.TryPrepare(designModel, transaction, out plan, out eligibilityDetail))
            {
                WriteOutput($"[Genexus Open API Builder][B115] Recuperação não oferecida: {eligibilityDetail}");
                return OrphanMetadataRecoveryOutcome.NotOffered;
            }
        }
        catch (Exception ex)
        {
            WriteOutput($"[Genexus Open API Builder][B115] Não foi possível avaliar recuperação de metadata: {DescribeException(ex)}");
            return OrphanMetadataRecoveryOutcome.NotOffered;
        }

        WriteOutput($"[Genexus Open API Builder][B115] Recuperação disponível: {eligibilityDetail}");
        var metadataName = plan.MetadataFileName;
        var messageKey = plan.StaleFile is null
            ? "Foi encontrada uma API gerada pela extensao sem o File de metadata '{0}'. A recuperacao criara somente esse File, com o inventario dos objetos encontrados na KB ({1} Procedures, {2} SDTs proprios, {3} compartilhados). Ela devolve a possibilidade de remover a API gerada, mas nao recupera o contrato original: paginacao, ordenacao, campos obrigatorios e a estrutura hierarquica nao existem fora da metadata perdida, e o Sincronizar seguira bloqueado ate uma nova aplicacao completa. Nada mais sera alterado. Deseja recuperar agora?"
            : "O File de metadata '{0}' existe, mas registra um API Object que nao esta mais na KB — tipicamente porque ele foi removido e outro foi gerado com o mesmo nome. Nesse estado o Wizard e o Remover ficam bloqueados. A recuperacao regravara esse File com o inventario atual ({1} Procedures, {2} SDTs proprios, {3} compartilhados) e o API Object que existe agora. O File ja era uma metadata reconstruida, sem o contrato original, entao nada de contrato se perde e o Sincronizar segue bloqueado ate uma nova aplicacao completa. Nenhum API Object, Procedure ou SDT sera alterado. Deseja recuperar agora?";
        var message = string.Format(
            texts.Translate(messageKey),
            metadataName,
            plan.ProcedureNames.Count,
            plan.OwnSdtNames.Count,
            plan.SharedSdtNames.Count);
        var answer = System.Windows.Forms.MessageBox.Show(
            owner,
            message,
            texts.Wizard,
            System.Windows.Forms.MessageBoxButtons.YesNo,
            System.Windows.Forms.MessageBoxIcon.Warning,
            System.Windows.Forms.MessageBoxDefaultButton.Button2);
        if (answer != System.Windows.Forms.DialogResult.Yes)
        {
            WriteOutput($"[Genexus Open API Builder][B115] Recuperação recusada pelo usuário para File='{metadataName}'. Nenhuma alteração foi feita por B115.");
            return OrphanMetadataRecoveryOutcome.Declined;
        }

        try
        {
            var result = ApiPlanOrphanMetadataRecovery.Recover(designModel, transaction, plan);
            WriteOutput($"[Genexus Open API Builder][B115] Metadata órfã recuperada: File='{result.FileName}', Guid='{result.Guid}', Bytes={result.Bytes}, Procedures={result.ProcedureCount}, SdtsProprios={result.OwnSdtCount}, SdtsCompartilhados={result.SharedSdtCount}. Marcada como intenção importada: 'Remover API gerada' volta a funcionar e 'Sincronizar' permanece bloqueado ate uma aplicacao completa reescrever a metadata. Nenhum API Object, Procedure ou SDT foi alterado por B115.");
            System.Windows.Forms.MessageBox.Show(
                owner,
                string.Format(
                    texts.Translate("A metadata '{0}' foi recuperada. Reabra o Wizard para continuar; nenhuma outra etapa foi executada nesta aplicacao."),
                    result.FileName),
                texts.Wizard,
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);
            return OrphanMetadataRecoveryOutcome.Recovered;
        }
        catch (Exception ex)
        {
            var detail = DescribeException(ex);
            WriteOutput($"[Genexus Open API Builder][B115] Recuperação de metadata falhou: File='{metadataName}', Error='{detail}'. Nenhuma etapa posterior foi executada.");
            System.Windows.Forms.MessageBox.Show(
                owner,
                string.Format(texts.Translate("A recuperacao da metadata '{0}' falhou: {1}"), metadataName, detail),
                texts.Wizard,
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
            return OrphanMetadataRecoveryOutcome.Failed;
        }
    }

    private static string DescribeException(Exception exception)
    {
        var inner = exception.InnerException;
        return inner is null
            ? exception.GetType().Name + ": " + exception.Message
            : exception.GetType().Name + ": " + exception.Message + " | Inner=" + inner.GetType().Name + ": " + inner.Message;
    }

    private static Transaction? TryResolveTransactionFromContext(CommandData data)
    {
        return KBObjectSelectionHelper.TryGetOnlyOneKBObjectFrom(data.Context) as Transaction;
    }

    private static void AppendCollisionConflictsToReport(
        ApiPlanApplicationFinalReportCollector report,
        IReadOnlyList<ApiPlanCollisionConflict> collisions)
    {
        foreach (var conflict in collisions)
        {
            var detail = $"Modulo='{conflict.ModuleName}' | Folder='{conflict.FolderName}'";
            if (!string.IsNullOrWhiteSpace(conflict.DiagnosticDetails))
            {
                detail += Environment.NewLine + conflict.DiagnosticDetails;
            }

            report.AddBlocked(
                conflict.ObjectType,
                conflict.Name,
                detail);
        }
    }

    private static void AppendPlanWarnings(ApiPlanApplicationFinalReportCollector report, ApiPlan apiPlan)
    {
        if (apiPlan.ServiceDescriptionFallbackUsed)
        {
            report.AddWarning("Descricoes de servico usaram fallback em ingles (idioma da KB ainda nao validado por API publica).");
        }

        var sensitive = apiPlan.CreateRequestFields
            .Concat(apiPlan.UpdateRequestFields)
            .Concat(apiPlan.ResponseFields)
            .Where(field => field.IsSensitive)
            .Select(field => field.Name)
            .Concat(apiPlan.ListFilters.Where(filter => filter.Field.IsSensitive).Select(filter => filter.Field.Name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (sensitive.Length > 0)
        {
            report.AddWarning($"Campos sensiveis no plano: {string.Join(", ", sensitive)}.");
        }
    }

    private static void AppendSdtWriteItemToReport(
        ApiPlanApplicationFinalReportCollector? report,
        ApiPlanSdtWriteItemResult item)
    {
        report?.AddFromWriteStatus("SDT", item.Name, item.Status, item.Kind);
    }

    private static void AppendPlanSideEffects(
        ApiPlanApplicationFinalReportCollector report,
        ApiPlan? apiPlan)
    {
        if (apiPlan is null)
        {
            return;
        }

        if (apiPlan.TransactionFolderWasCreated)
        {
            report.AddCreated(
                "Folder",
                apiPlan.TransactionFolderName,
                "criado pela extensão; apagar só se ficar vazio");
        }

        if (apiPlan.SharedSdtFolderWasCreated)
        {
            report.AddCreated(
                "Folder",
                ApiPlanSdtWriter.SharedFolderName,
                "criado pela extensão como contêiner compartilhado de SDTs; preservado pela remoção de uma API");
        }

        var businessComponentWasPersisted = report.PersistenceLog?.Receipts.Any(receipt =>
            string.Equals(receipt.OperationKind, "Save", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(receipt.ObjectType, "Transaction", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(receipt.Stage, "Business Component", StringComparison.OrdinalIgnoreCase) &&
            receipt.Outcome == PersistenceOutcome.Confirmed) == true;
        if (apiPlan.BusinessComponent.EnabledDuringWizard && businessComponentWasPersisted)
        {
            report.AddUpdated(
                "Transaction",
                apiPlan.TransactionName,
                "Business Component habilitado durante o Wizard");
        }
    }

    private static void AppendTransactionFolderWarning(
        ApiPlanApplicationFinalReportCollector report,
        ApiPlanGenerationState generationState)
    {
        var warning = generationState.TransactionFolderWarning;
        if (!string.IsNullOrWhiteSpace(warning))
        {
            report.AddWarning(warning!);
        }
    }

    private static void ShowFinalReport(
        ApiPlanApplicationFinalReportCollector collector,
        TimeSpan elapsed,
        KBModel? designModel,
        ApiPlan? apiPlan = null,
        ApiPlanPersistenceLog? persistenceLog = null)
    {
        // A janela de progresso sai de cena antes do relatório: os comandos mostram o relatório
        // dentro do escopo dela, e deixá-la viva põe um `Abortar` ativo atrás de uma operação
        // que já terminou. Fica aqui, e não em cada chamador, porque vale para todos os
        // caminhos — sucesso, bloqueio, falha de etapa e aborto.
        ExtensionBusyProgressScope.CloseCurrent();

        // B082: a apresentacao do relatorio roda dentro do escopo de medicao do Sync,
        // mas nao faz parte da operacao medida. Suspender evita atribuir a ela as
        // leituras de TryResolveMainObjectFromKb e de qualquer consulta futura daqui.
        using var scanSuspension = ApiPlanScanProbe.Suspend();
        using var persistenceSuspension = ApiPlanSaveBoundaryProbe.SuspendPersistence();
        AppendPlanSideEffects(collector, apiPlan);
        if (persistenceLog is not null)
        {
            collector.SetPersistenceLog(persistenceLog);
        }
        TryResolveMainObjectFromKb(collector, designModel, apiPlan);
        var report = collector.Build(elapsed);
        WriteOutput(report.BuildOutputSummary());
        foreach (var item in report.Blocked)
        {
            WriteOutput($"[Genexus Open API Builder][B081] Bloqueado: Kind='{item.ObjectKind}', Name='{item.Name}', Detail='{item.Detail}'.");
        }

        foreach (var warning in report.Warnings)
        {
            WriteOutput($"[Genexus Open API Builder][B081] Aviso: {warning}");
        }

        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        var owner = ResolveFinalReportOwner();
        using var dialog = new ApiPlanApplicationFinalReportDialog(
            report,
            designModel,
            ExtensionLocalization.For(knowledgeBase),
            owner);
        if (owner is null)
        {
            dialog.ShowDialog();
        }
        else
        {
            dialog.ShowDialog(owner);
        }
    }

    private static void TryResolveMainObjectFromKb(
        ApiPlanApplicationFinalReportCollector collector,
        KBModel? designModel,
        ApiPlan? apiPlan = null)
    {
        if (designModel is null
            || collector.MainObjectGuid.HasValue
            || (collector.ApiSavePathEntered && !collector.PersistedMainObjectGuid.HasValue)
            || string.Equals(collector.Operation, "Remover", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Fluxos legados que nao carregam ApiPlan mantem a compatibilidade de
        // exibicao. No fluxo gerenciado da S-B111, o nome nunca e uma prova de
        // identidade: somente o GUID planejado e confirmado por releitura pode
        // criar o link para o API Object principal.
        if (apiPlan is null)
        {
            if (string.IsNullOrWhiteSpace(collector.ApiName))
            {
                return;
            }

            var legacyApiObject = API.GetAll(designModel)
                .FirstOrDefault(item => string.Equals(item.Name, collector.ApiName, StringComparison.OrdinalIgnoreCase));
            if (legacyApiObject is not null)
            {
                collector.SetMainObject(legacyApiObject.Name, legacyApiObject.Guid);
            }

            return;
        }

        ApiPlanMainObjectCandidate? candidateByGuid = null;
        string? guidLookupError = null;
        if (apiPlan.PlannedApiGuid.HasValue && apiPlan.PlannedApiGuid.Value != Guid.Empty)
        {
            try
            {
                var apiObjectByGuid = API.Get(designModel, apiPlan.PlannedApiGuid.Value);
                if (apiObjectByGuid is not null)
                {
                    candidateByGuid = new ApiPlanMainObjectCandidate(apiObjectByGuid.Guid, apiObjectByGuid.Name);
                }
            }
            catch (Exception ex)
            {
                guidLookupError = ex.Message;
            }
        }

        ApiPlanMainObjectCandidate[] nameMatches = Array.Empty<ApiPlanMainObjectCandidate>();
        if (candidateByGuid is null)
        {
            try
            {
                nameMatches = API.GetAll(designModel)
                    .Where(item => string.Equals(item.Name, apiPlan.ApiName, StringComparison.OrdinalIgnoreCase))
                    .Select(item => new ApiPlanMainObjectCandidate(item.Guid, item.Name))
                    .ToArray();
            }
            catch (Exception ex)
            {
                collector.AddWarning($"API Object principal nao foi associado: a consulta secundaria por nome falhou para '{apiPlan.ApiName}' ({ex.Message}). Nenhum link foi criado.");
                return;
            }
        }

        var resolution = ApiPlanMainObjectResolver.Resolve(
            apiPlan.PlannedApiGuid,
            apiPlan.ApiName,
            candidateByGuid,
            nameMatches);
        if (resolution.IsConfirmed && resolution.Candidate is not null)
        {
            collector.SetMainObject(resolution.Candidate.Name, resolution.Candidate.Guid);
            return;
        }

        var warning = $"API Object principal nao foi associado com seguranca: {resolution.Diagnostic}";
        if (!string.IsNullOrWhiteSpace(guidLookupError))
        {
            warning += $" Erro na consulta por GUID: '{guidLookupError}'.";
        }

        collector.AddWarning(warning);
    }

    internal static void WriteApiObjectBaselineDiagnostic(ApiPlanGenerationState generationState)
    {
        WriteApiObjectBaselineDiagnostic(WriteOutput, generationState);
    }

    internal static void WriteApiObjectBaselineDiagnostic(Action<string> write, ApiPlanGenerationState generationState)
    {
        if (write is null)
        {
            throw new ArgumentNullException(nameof(write));
        }

        if (generationState is null || !generationState.ApiObject.IsBlocked)
        {
            return;
        }

        var conflicts = generationState.ApiObject.CollisionConflicts;
        if (conflicts.Count == 0)
        {
            write($"[Genexus Open API Builder][B087] Diagnostico de posse do API Object: etapa bloqueada sem lista de conflito. Detail='{generationState.ApiObject.Detail}'.");
            return;
        }

        foreach (var conflict in conflicts)
        {
            write($"[Genexus Open API Builder][B087] Diagnostico de posse do API Object (baseline de alteracao intencional): Causa='{conflict.DiagnosticReason}' | Name='{conflict.Name}' | Tipo='{conflict.ObjectType}' | Modulo='{conflict.ModuleName}' | Folder='{conflict.FolderName}'.");
            var details = conflict.FormatDiagnosticDetails();
            if (string.IsNullOrWhiteSpace(details))
            {
                continue;
            }

            foreach (var line in details.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    write($"[Genexus Open API Builder][B087] {line}");
                }
            }
        }
    }

    private static System.Windows.Forms.IWin32Window? ResolveFinalReportOwner()
    {
        // Prefere a janela principal do processo GeneXus (monitor da IDE).
        // ActiveForm primeiro posicionava o Wizard no monitor primário quando o
        // owner não era um Form WinForms.
        return ExtensionIdeScreenPlacement.ResolveOwner();
    }

    /// <summary>
    /// B109 — publica a sonda temporária das fronteiras Pump/Save no final do Apply.
    /// A publicação nunca pode alterar o resultado da operação medida.
    /// </summary>
    private sealed class ApiPlanSaveBoundaryPublisher : IDisposable
    {
        private readonly ApiPlanSaveBoundaryLog _log;
        private readonly string _operation;
        private bool _disposed;

        public ApiPlanSaveBoundaryPublisher(ApiPlanSaveBoundaryLog log, string operation)
        {
            _log = log;
            _operation = operation;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                if (!_log.HasAnomaly)
                {
                    return;
                }

                foreach (var line in _log.Render())
                {
                    WriteOutput($"[Genexus Open API Builder][B109][{_operation}] {line}");
                }
            }
            catch
            {
            }
        }
    }

    private static void WriteProbePhase(string phaseName, long elapsedMs)
    {
        if (System.Diagnostics.Debugger.IsAttached)
        {
            WriteOutput($"[Genexus Open API Builder][B082] Fase {phaseName}={elapsedMs} ms.");
        }
    }

    /// <summary>
    /// B082: fecha a fase total do Apply e publica o custo das varreduras de catálogo.
    /// Diagnóstico apenas; não participa de nenhuma decisão de escrita.
    /// </summary>
    private static void WriteApplyScanTelemetry(ApiPlanScanTelemetry telemetry, long elapsedMs)
    {
        WriteProbePhase("TotalAposConcluir", elapsedMs);
        WriteScanTelemetry("Apply", telemetry);
    }

    /// <summary>
    /// B082: publica o custo das varreduras de catálogo de uma operação. Diagnóstico apenas.
    /// </summary>
    private static void WriteScanTelemetry(string operation, ApiPlanScanTelemetry telemetry)
    {
        if (!System.Diagnostics.Debugger.IsAttached || telemetry is null || telemetry.ScanCount == 0)
        {
            return;
        }

        foreach (var line in telemetry.BuildOutputLines())
        {
            WriteOutput($"[Genexus Open API Builder][B082] {operation} {line}");
        }
    }

    private static void WriteOutput(string message)
    {
        WriteOutputCore(message, forceShow: true);
    }

    private static void WriteOutputCore(string message, bool forceShow)
    {
        if (!CommonServices.IsOutputAvailable)
        {
            return;
        }

        var output = CommonServices.Output;
        if (output is not IOutputService2 outputWithDefault)
        {
            return;
        }

        var outputId = outputWithDefault.DefaultOutputId;
        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        var language = ExtensionLocalization.Resolve(knowledgeBase);
        output.AddLine(outputId, ExtensionOutputLocalization.Translate(message, language));
        if (forceShow)
        {
            output.Show(outputId);
        }
    }

}
