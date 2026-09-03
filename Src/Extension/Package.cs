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
        using var dialog = new PrototypeWizardPreferencesDialog(loadResult.Preferences, loadResult.Status, texts);
        var result = dialog.ShowDialog();
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
            WriteOutput($"[Genexus Open API Builder][Prefs] Gravacao de preferencias bloqueada ou falhou antes de concluir: Error='{errorDetail}'");
        }

        return true;
    }

    private static bool QueryDetectActiveKnowledgeBase(CommandData data, ref CommandStatus status)
    {
        status.Visible(true);
        return true;
    }

    private static bool ExecuteDetectActiveKnowledgeBase(CommandData data)
    {
        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        var snapshot = ActiveKnowledgeBaseProbe.TryRead(knowledgeBase);

        WriteOutput(
            snapshot is null
                ? "[Genexus Open API Builder][B020] Nenhuma Knowledge Base ativa foi encontrada. Abra uma KB e execute o comando novamente."
                : $"[Genexus Open API Builder][B020] Knowledge Base ativa detectada: Name='{snapshot.Name}', Guid='{snapshot.Guid}', Location='{snapshot.Location}'.");

        return true;
    }

    private static bool QueryListEligibleTransactions(CommandData data, ref CommandStatus status)
    {
        status.Visible(true);
        return true;
    }

    private static bool ExecuteListEligibleTransactions(CommandData data)
    {
        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        if (knowledgeBase is null)
        {
            WriteOutput("[Genexus Open API Builder][B021] Nenhuma Knowledge Base ativa foi encontrada. Abra uma KB e execute o comando novamente.");
            return true;
        }

        var transactionNames = EligibleTransactionReader.ReadNames(knowledgeBase);
        if (transactionNames.Count == 0)
        {
            WriteOutput("[Genexus Open API Builder][B021] Nenhuma Transaction elegível foi encontrada na Knowledge Base ativa.");
            return true;
        }

        WriteOutput($"[Genexus Open API Builder][B021] Transactions elegíveis encontradas: Total={transactionNames.Count}.");
        foreach (var transactionName in transactionNames)
        {
            WriteOutput($"[Genexus Open API Builder][B021] Transaction elegível: Name='{transactionName}'.");
        }

        return true;
    }

    private static bool QuerySelectTransactionAndReadModule(CommandData data, ref CommandStatus status)
    {
        status.Visible(true);
        return true;
    }

    private static bool ExecuteSelectTransactionAndReadModule(CommandData data)
    {
        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        if (knowledgeBase is null)
        {
            WriteOutput("[Genexus Open API Builder][B022] Nenhuma Knowledge Base ativa foi encontrada. Abra uma KB e execute o comando novamente.");
            return true;
        }

        PrototypeTransactionSelectionState.ClearIfKnowledgeBaseChanged(knowledgeBase);

        if (!UIServices.IsSelectObjectDialogAvailable)
        {
            WriteOutput("[Genexus Open API Builder][B022] O diálogo público de seleção não está disponível nesta IDE.");
            return true;
        }

        var options = new SelectObjectOptions
        {
            MultipleSelection = false,
            DialogTitle = "Selecionar Transaction para ler módulo (B022)",
            SupportCreateAction = false
        };
        options.ObjectTypes.Add(KBObjectDescriptor.Get<Transaction>());

        var selectedObject = UIServices.SelectObjectDialog.SelectObject(options);
        if (selectedObject is null)
        {
            WriteOutput("[Genexus Open API Builder][B022] Nenhuma Transaction foi selecionada.");
            return true;
        }

        if (selectedObject is not Transaction transaction)
        {
            WriteOutput("[Genexus Open API Builder][B022] A seleção retornada não é uma Transaction. Nenhuma escolha foi mantida.");
            return true;
        }

        var module = transaction.Module;
        if (module is null)
        {
            WriteOutput($"[Genexus Open API Builder][B022] A Transaction selecionada não possui módulo disponível: Name='{transaction.Name}'.");
            return true;
        }

        PrototypeTransactionSelectionState.Store(knowledgeBase, transaction);
        WriteOutput($"[Genexus Open API Builder][B022] Transaction selecionada: Name='{transaction.Name}'.");
        WriteOutput($"[Genexus Open API Builder][B022] Módulo da Transaction: Name='{module.Name}'.");

        return true;
    }

    private static bool QueryDetectExistingObjects(CommandData data, ref CommandStatus status)
    {
        status.Visible(true);
        return true;
    }

    private static bool ExecuteDetectExistingObjects(CommandData data)
    {
        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        if (knowledgeBase is null)
        {
            WriteOutput("[Genexus Open API Builder][B023] Nenhuma Knowledge Base ativa foi encontrada. Abra uma KB e execute o comando novamente.");
            return true;
        }

        PrototypeTransactionSelectionState.ClearIfKnowledgeBaseChanged(knowledgeBase);
        var selectedTransaction = PrototypeTransactionSelectionState.Current;
        if (selectedTransaction is null)
        {
            WriteOutput("[Genexus Open API Builder][B023] Nenhuma Transaction selecionada em memória. Execute primeiro o comando B022.");
            return true;
        }

        var transaction = Transaction.GetAll(knowledgeBase.DesignModel)
            .SingleOrDefault(item => item.Guid == selectedTransaction.TransactionGuid);
        if (transaction is null)
        {
            WriteOutput($"[Genexus Open API Builder][B023] A Transaction selecionada em memória não foi reencontrada: Name='{selectedTransaction.TransactionName}', Guid='{selectedTransaction.TransactionGuid}'. Nenhuma escolha foi persistida.");
            return true;
        }

        var snapshot = PrototypeExistingObjectReader.Read(knowledgeBase.DesignModel, transaction);
        WriteOutput($"[Genexus Open API Builder][B023] Transaction selecionada: Name='{snapshot.TransactionName}', MetadataFile='{snapshot.MetadataFileName}'.");
        WriteOutput($"[Genexus Open API Builder][B023] Objetos planejados verificados: Total={snapshot.Results.Count}, Existentes={snapshot.ExistingCount}, Ausentes={snapshot.MissingCount}.");
        foreach (var result in snapshot.Results)
        {
            WriteOutput($"[Genexus Open API Builder][B023] {result.ObjectType}: Name='{result.Name}', Count={result.Count}, Status='{result.Status}'.");
        }

        return true;
    }

    private static bool QueryCheckBusinessComponent(CommandData data, ref CommandStatus status)
    {
        status.Visible(true);
        return true;
    }

    private static bool ExecuteCheckBusinessComponent(CommandData data)
    {
        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        if (knowledgeBase is null)
        {
            WriteOutput("[Genexus Open API Builder][B024] Nenhuma Knowledge Base ativa foi encontrada. Abra uma KB e execute o comando novamente.");
            return true;
        }

        PrototypeTransactionSelectionState.ClearIfKnowledgeBaseChanged(knowledgeBase);
        var selectedTransaction = PrototypeTransactionSelectionState.Current;
        if (selectedTransaction is null)
        {
            WriteOutput("[Genexus Open API Builder][B024] Nenhuma Transaction selecionada em memória. Execute primeiro o comando B022.");
            return true;
        }

        var transaction = Transaction.GetAll(knowledgeBase.DesignModel)
            .SingleOrDefault(item => item.Guid == selectedTransaction.TransactionGuid);
        if (transaction is null)
        {
            WriteOutput($"[Genexus Open API Builder][B024] A Transaction selecionada em memória não foi reencontrada: Name='{selectedTransaction.TransactionName}', Guid='{selectedTransaction.TransactionGuid}'. Nenhuma escolha foi persistida.");
            return true;
        }

        var snapshot = PrototypeBusinessComponentReader.Read(transaction);
        WriteOutput($"[Genexus Open API Builder][B024] Transaction selecionada: Name='{snapshot.TransactionName}', IsBusinessComponent={snapshot.IsBusinessComponent}.");
        WriteOutput($"[Genexus Open API Builder][B024] Resultado da verificação: Status='{snapshot.Status}'.");

        return true;
    }

    private static bool QueryReadPrimaryKey(CommandData data, ref CommandStatus status)
    {
        status.Visible(true);
        return true;
    }

    private static bool ExecuteReadPrimaryKey(CommandData data)
    {
        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        if (knowledgeBase is null)
        {
            WriteOutput("[Genexus Open API Builder][B025] Nenhuma Knowledge Base ativa foi encontrada. Abra uma KB e execute o comando novamente.");
            return true;
        }

        PrototypeTransactionSelectionState.ClearIfKnowledgeBaseChanged(knowledgeBase);

        var transaction = TryResolveTransactionFromContext(data);
        if (transaction is not null)
        {
            var transactionGuid = transaction.Guid;
            transaction = Transaction.GetAll(knowledgeBase.DesignModel)
                .SingleOrDefault(item => item.Guid == transactionGuid);
            if (transaction is null)
            {
                WriteOutput("[Genexus Open API Builder][B025] A Transaction do menu de contexto não foi reencontrada na Knowledge Base ativa. Nenhuma escolha foi persistida.");
                return true;
            }

            PrototypeTransactionSelectionState.Store(knowledgeBase, transaction);
        }
        else
        {
            var selectedTransaction = PrototypeTransactionSelectionState.Current;
            if (selectedTransaction is null)
            {
                WriteOutput("[Genexus Open API Builder][B025] Nenhuma Transaction selecionada. Use o menu de contexto de uma Transaction ou execute primeiro o comando B022.");
                return true;
            }

            transaction = Transaction.GetAll(knowledgeBase.DesignModel)
                .SingleOrDefault(item => item.Guid == selectedTransaction.TransactionGuid);
            if (transaction is null)
            {
                WriteOutput($"[Genexus Open API Builder][B025] A Transaction selecionada em memória não foi reencontrada: Name='{selectedTransaction.TransactionName}', Guid='{selectedTransaction.TransactionGuid}'. Nenhuma escolha foi persistida.");
                return true;
            }
        }

        var snapshot = PrototypePrimaryKeyReader.Read(transaction);
        WriteOutput($"[Genexus Open API Builder][B025] Transaction selecionada: Name='{snapshot.TransactionName}', PrimaryKeyParts={snapshot.Count}, HasCompositeKey={snapshot.HasCompositeKey}.");
        foreach (var part in snapshot.Parts)
        {
            WriteOutput($"[Genexus Open API Builder][B025] KeyPart: Order={part.Order}, Name='{part.Name}', Type='{part.Type}', Length={part.Length}, Decimals={part.Decimals}.");
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
                WriteOutput($"[Genexus Open API Builder][B082] SDT {item.Status}: Name='{item.Name}', Scope='{item.Scope}'.");
            };
            var result = preserveSdtNames is null
                ? ApiPlanSdtWriter.CreateOrReencounter(designModel, transaction, apiPlan, preserveSdtNames: null, kbIndex: kbIndex, onSdtWrite: onSdtWrite, progress: progress)
                : ApiPlanSdtWriter.CreateOrReencounter(designModel, transaction, apiPlan, preserveSdtNames, kbIndex, onSdtWrite, progress);
            WriteOutput($"[Genexus Open API Builder][B040-B046] Escrita de SDTs concluida: Transaction='{transaction.Name}', Trigger='{triggerSource}', PlannedOwnSdts={result.PlannedOwnSdts}, PlannedSharedSdts={result.PlannedSharedSdts}, Created={result.CreatedSdts}, Reencountered={result.ReencounteredSdts}, TransactionFolder='{result.TransactionFolderName}', TransactionFolderGuid='{result.TransactionFolderGuid}'. Nenhuma Procedure, API Object ou metadata persistente definitiva foi criada.");
            foreach (var item in result.Items)
            {
                WriteOutput($"[Genexus Open API Builder][B040-B046] SDT {item.Status}: Backlog='{item.BacklogId}', Kind='{item.Kind}', Name='{item.Name}', Scope='{item.Scope}', Guid='{item.Guid}'.");
            }

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
                WriteOutput($"[Genexus Open API Builder][{procedureStage}] Procedure {item.Status}: Backlog='{item.BacklogId}', Service='{item.ServiceName}', Name='{item.Name}', Guid='{item.Guid}'.");
                WriteOutput($"[Genexus Open API Builder][B082] Procedure {item.Status}: Name='{item.Name}'.");
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
            var result = ApiPlanApiObjectWriter.CreateOrReencounter(
                designModel,
                transaction,
                apiPlan,
                allowIntentionalContractRefresh,
                kbIndex,
                progress);
            WriteOutput($"[Genexus Open API Builder][B054] Escrita de API Object concluida: Transaction='{transaction.Name}', Trigger='{triggerSource}', ApiName='{result.ApiName}', Status='{result.Status}', ReencounteredSdts={result.ReencounteredSdts}, ReencounteredProcedures={result.ReencounteredProcedures}, PlannedServices={result.PlannedServices}, TransactionFolder='{result.TransactionFolderName}', TransactionFolderGuid='{result.TransactionFolderGuid}'. Nenhum REST completo, seguranca definitiva ou metadata persistente definitiva foi criado.");
            foreach (var procedure in result.Procedures)
            {
                WriteOutput($"[Genexus Open API Builder][B054] Procedure reencontrada para API Object: Backlog='{procedure.BacklogId}', Service='{procedure.ServiceName}', Name='{procedure.Name}', Guid='{procedure.Guid}'.");
            }

            WriteOutput($"[Genexus Open API Builder][B054] API Object {result.Status}: Name='{result.ApiName}', Guid='{result.Guid}'.");
            WriteOutput($"[Genexus Open API Builder][B056] Descricoes aplicadas no API Object real: Transaction='{transaction.Name}', Trigger='{triggerSource}', ApiName='{result.ApiName}', DescribedServices={apiPlan.ServiceDescriptions.Count}. Sem antecipar REST completo, codigo HTTP, seguranca definitiva ou metadata persistente.");
            report?.AddFromWriteStatus("API Object", result.ApiName, result.Status);
            report?.SetMainObject(result.ApiName, result.Guid);
            report?.SetApiName(result.ApiName);
            return true;
        }
        catch (Exception ex)
        {
            WriteOutput($"[Genexus Open API Builder][B054] Criacao de API Object bloqueada por preflight ou falhou antes de concluir: Trigger='{triggerSource}', Error='{ex.Message}'");
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
        ApiPlanBusyProgressSession? progress = null)
    {
        if (kbIndex is null)
        {
            throw new ArgumentNullException(nameof(kbIndex));
        }

        try
        {
            var result = ApiPlanBusinessComponentWriter.Apply(
                designModel,
                transaction,
                apiPlan,
                allowIntentionalContractRefresh,
                preserveSdtNames,
                kbIndex,
                onSdtWrite: item => AppendSdtWriteItemToReport(report, item),
                progress: progress);
            var deleteGuidPart = result.DeleteProcedureGuid == Guid.Empty
                ? string.Empty
                : $", DeleteProcedureGuid='{result.DeleteProcedureGuid}'";
            WriteOutput($"[Genexus Open API Builder][B071-B073/B079] REST via Business Component aplicado e API Object sincronizado: Transaction='{transaction.Name}', Trigger='{triggerSource}', GetProcedureGuid='{result.GetProcedureGuid}', CreateProcedureGuid='{result.CreateProcedureGuid}', UpdateProcedureGuid='{result.UpdateProcedureGuid}'{deleteGuidPart}, ApiObjectGuid='{result.ApiObjectGuid}', PrimaryKeyParts={result.PrimaryKeyParts}, CreateFields={result.CreateFields}, UpdateFields={result.UpdateFields}, ResponseFields={result.ResponseFields}. Status HTTP controlado por RestCode no API Object; ErrorResponse exposto como saida publica dos servicos; Location de Create emitido nativamente via HttpResponse.");
            WriteOutput($"[Genexus Open API Builder][B056] Descricoes reaplicadas no API Object real durante B071-B073/B079: Transaction='{transaction.Name}', Trigger='{triggerSource}', ApiObjectGuid='{result.ApiObjectGuid}', DescribedServices={apiPlan.ServiceDescriptions.Count}. Service Source preserva o contrato Procedure/API Object atual.");
            foreach (var procedureName in apiPlan.ProcedureNames.Where(name =>
                         name.EndsWith("_API_Get", StringComparison.OrdinalIgnoreCase)
                         || name.EndsWith("_API_Create", StringComparison.OrdinalIgnoreCase)
                         || name.EndsWith("_API_Update", StringComparison.OrdinalIgnoreCase)
                         || name.EndsWith("_API_Delete", StringComparison.OrdinalIgnoreCase)))
            {
                report?.AddUpdated("Procedure", procedureName, "Business Component");
            }

            report?.AddUpdated("API Object", apiPlan.ApiName, "Business Component");
            report?.SetMainObject(apiPlan.ApiName, result.ApiObjectGuid);
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
        ApiPlanBusyProgressSession? progress = null)
    {
        try
        {
            var result = ApiPlanListProcedureWriter.Apply(
                designModel,
                transaction,
                apiPlan,
                allowIntentionalContractRefresh,
                preserveSdtNames,
                kbIndex,
                onSdtWrite: item => AppendSdtWriteItemToReport(report, item),
                progress: progress);
            WriteOutput($"[Genexus Open API Builder][B070] List aplicado e API Object sincronizado: Transaction='{transaction.Name}', Trigger='{triggerSource}', ListProcedureGuid='{result.ListProcedureGuid}', ApiObjectGuid='{result.ApiObjectGuid}', Filters={result.Filters}, OrderParts={result.OrderParts}, DefaultPageSize={result.DefaultPageSize}, MaximumPageSize={result.MaximumPageSize}. B076 e validacao runtime do List permanecem pendentes.");
            var listProcedure = apiPlan.ProcedureNames.FirstOrDefault(name =>
                name.EndsWith("_API_List", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(listProcedure))
            {
                report?.AddUpdated("Procedure", listProcedure, "List");
            }

            report?.AddUpdated("API Object", apiPlan.ApiName, "List");
            report?.SetMainObject(apiPlan.ApiName, result.ApiObjectGuid);
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
            var apiPlan = ApiPlanBuilder.Build(knowledgeBase.DesignModel, transaction, selection);
            var report = new ApiPlanApplicationFinalReportCollector("Sincronizar", transaction.Name, apiPlan.ApiName);
            var stopwatch = Stopwatch.StartNew();
            AppendPlanWarnings(report, apiPlan);
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
                }
                catch (Exception ex) when (ex is not ApiPlanBusyAbortedException)
                {
                    var errorDetail = ex.InnerException is null ? ex.Message : $"{ex.Message} | Inner='{ex.InnerException.Message}'";
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

                if (!TryCreateSdts(knowledgeBase.DesignModel, transaction, apiPlan, "SyncB085", syncKbIndex, preserveSdts, report, busy.Session))
                {
                    stopwatch.Stop();
                    ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, apiPlan);
                    return true;
                }

                syncKbIndex.RefreshSdts(knowledgeBase.DesignModel);

                if (!TryCreateProcedures(knowledgeBase.DesignModel, transaction, apiPlan, "SyncB085", syncKbIndex, report, busy.Session))
                {
                    stopwatch.Stop();
                    ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, apiPlan);
                    return true;
                }

                busy.ThrowIfAbortRequested();
                if (selection.GenerateApiObject && !selection.ApplyBusinessComponent)
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
                else if (selection.GenerateApiObject && selection.ApplyBusinessComponent)
                {
                    if (!API.GetAll(knowledgeBase.DesignModel).Any(api => string.Equals(api.Name, apiPlan.ApiName, StringComparison.OrdinalIgnoreCase)))
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
                        kbIndex: syncKbIndex);
                    if (bcFailed)
                    {
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
                        progress: busy.Session);
                    if (listFailed)
                    {
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
            }
            catch (ApiPlanBusyAbortedException abortEx)
            {
                WriteOutput($"[Genexus Open API Builder][B082] Sync abortado: Transaction='{transaction.Name}', Error='{abortEx.Message}'");
                report.HeadlineOverride = "Sincronização abortada pelo usuário.";
                report.AddWarning(abortEx.Message);
                report.AddBlocked("Sync", transaction.Name, "Abortado [B082]");
                stopwatch.Stop();
                ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, apiPlan);
                return true;
            }
            catch (InvalidOperationException ex) when (
                ex.Message == "SYNC_API_OBJECT_FAILED" || ex.Message == "SYNC_METADATA_FAILED")
            {
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

            var stopwatch = Stopwatch.StartNew();
            ApiPlanGeneratedApiRemovalResult result;
            using (var busy = ExtensionBusyProgressScope.Show(owner, texts.BusyProgressTitleRemove, texts))
            {
                WriteOutput($"[Genexus Open API Builder][B082] Remover iniciado: Transaction='{transaction.Name}', PlannedDeletes={ApiPlanGeneratedApiRemover.CountPlannedDeletes(plan)}.");
                try
                {
                    result = ApiPlanGeneratedApiRemover.Remove(knowledgeBase.DesignModel, transaction, busy.Session);
                }
                catch (ApiPlanBusyAbortedException abortEx)
                {
                    stopwatch.Stop();
                    WriteOutput($"[Genexus Open API Builder][B082] Remover abortado: Transaction='{transaction.Name}', Error='{abortEx.Message}'");
                    var abortReport = new ApiPlanApplicationFinalReportCollector("Remover", transaction.Name, plan.ApiName);
                    abortReport.SetApiName(plan.ApiName);
                    abortReport.HeadlineOverride = "Remoção abortada pelo usuário.";
                    abortReport.AddWarning(abortEx.Message);
                    abortReport.AddBlocked("Remover", transaction.Name, "Abortado [B082]");
                    ShowFinalReport(abortReport, stopwatch.Elapsed, knowledgeBase.DesignModel);
                    return true;
                }
            }

            stopwatch.Stop();
            WriteOutput($"[Genexus Open API Builder][B086] Remocao concluida: Transaction='{transaction.Name}', ApiName='{result.Plan.ApiName}', Deleted={result.DeletedItems.Count}, Items='{string.Join("; ", result.DeletedItems)}'. SDTs compartilhados e Business Component da Transaction nao foram alterados.");
            WriteOutput($"[Genexus Open API Builder][B082] Remover TotalMs={stopwatch.ElapsedMilliseconds}.");
            foreach (var telemetryLine in result.TelemetryLines)
            {
                WriteOutput($"[Genexus Open API Builder][B082] Remover {telemetryLine}");
            }
            var report = new ApiPlanApplicationFinalReportCollector("Remover", transaction.Name, result.Plan.ApiName);
            report.SetApiName(result.Plan.ApiName);
            report.AddDeletedItems(result.DeletedItems.ToArray());
            ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel);
        }
        catch (Exception ex)
        {
            var errorDetail = ex.InnerException is null ? ex.Message : $"{ex.Message} | Inner='{ex.InnerException.Message}'";
            WriteOutput($"[Genexus Open API Builder][B086] Remocao bloqueada ou falhou: Transaction='{transaction.Name}', Error='{errorDetail}'");
            var report = new ApiPlanApplicationFinalReportCollector("Remover", transaction.Name, null);
            report.AddBlocked("Remover", transaction.Name, errorDetail);
            ShowFinalReport(report, TimeSpan.Zero, knowledgeBase.DesignModel);
        }

        return true;
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
                    () => EnableBusinessComponentForWizard(transaction),
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

        using (dialog!)
        {
        var wizardOwner = ResolveFinalReportOwner();
        ExtensionIdeScreenPlacement.CenterOnIdeScreen(dialog, wizardOwner);
        var result = wizardOwner is null
            ? dialog.ShowDialog()
            : dialog.ShowDialog(wizardOwner);
        var businessComponentExitStatus = dialog.BusinessComponentEnabledDuringWizard
            ? "Business Component foi habilitado por confirmacao explicita antes da saida; essa alteracao foi gravada na KB e nao foi revertida automaticamente."
            : "Nenhuma alteracao foi feita na KB.";

        // O wizard único não emite mais Retry: a primeira aba oculta Voltar.
        // O ramo permanece para um DialogResult residual e para os diálogos B031/B032.
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
        var sdtGenerationPlan = ApiPlanSdtGenerationPlanBuilder.Create(apiPlan);
        var classificationConfiguration = apiPlan.FieldClassificationConfiguration;
        var classificationMetadataContract = classificationConfiguration.MetadataContract;
        var serviceDescriptionsPendingCount = apiPlan.ServiceDescriptions.Count(item => string.Equals(item.Description, ApiPlan.UnresolvedB056ServiceDescription, StringComparison.Ordinal));
        var serviceDescriptionsResolvedCount = apiPlan.ServiceDescriptions.Count - serviceDescriptionsPendingCount;
        PrototypeWizardFlowSessionState.Store(selection);
        PrototypeWizardSessionState.StoreContractSelection(selection.ContractSelection);
        PrototypeWizardReviewSessionState.StoreReviewSelection(selection.ReviewSelection);
        ApiPlanSessionState.Store(apiPlan);
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
        var applyFromConfirm = Stopwatch.StartNew();
        var phaseWatch = Stopwatch.StartNew();
        var applyOwner = ResolveFinalReportOwner();
        var report = new ApiPlanApplicationFinalReportCollector("Wizard", transaction.Name, apiPlan.ApiName);
        var stopwatch = Stopwatch.StartNew();
        // B082: mede o custo das varreduras de catalogo ao longo de todo o Apply.
        var scanTelemetry = new ApiPlanScanTelemetry();
        using var scanScope = ApiPlanScanProbe.Begin(scanTelemetry);
        try
        {
            using var busy = ExtensionBusyProgressScope.Show(applyOwner, texts.BusyProgressTitleApply, texts);
            WriteOutput($"[Genexus Open API Builder][B082] Apply Wizard iniciado: Transaction='{transaction.Name}'.");
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
            if (!selection.GenerateSdts && !selection.GenerateProcedures && !selection.GenerateApiObject && !selection.GenerateMetadata && !selection.ApplyList && !selection.ApplyBusinessComponent)
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

                stopwatch.Stop();
                WriteApplyScanTelemetry(scanTelemetry, applyFromConfirm.ElapsedMilliseconds);
                ShowFinalReport(report, stopwatch.Elapsed, knowledgeBase.DesignModel, apiPlan);
                return true;
            }

            busy.ThrowIfAbortRequested();
            phaseWatch.Restart();
            var apiObjectReady = true;
            if (selection.GenerateApiObject && !selection.ApplyBusinessComponent)
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
            else if (selection.GenerateApiObject && selection.ApplyBusinessComponent)
            {
                if (API.GetAll(knowledgeBase.DesignModel).Any(api => string.Equals(api.Name, apiPlan.ApiName, StringComparison.OrdinalIgnoreCase)))
                {
                    WriteOutput($"[Genexus Open API Builder][B054] API Object ja existe para Transaction='{transaction.Name}'. Como B071-B073/B079 tambem foi confirmado, a atualizacao do API Object sera absorvida pelo preflight de Business Component.");
                }
                else
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
                    progress: busy.Session);
            }

            WriteProbePhase("BusinessComponent", phaseWatch.ElapsedMilliseconds);
            if (!businessComponentReady)
            {
                if (selection.GenerateMetadata)
                {
                    WriteOutput($"[Genexus Open API Builder][B060] Metadata nao foi gravada para Transaction='{transaction.Name}' porque B071-B073/B079 falhou ou foi bloqueado neste fluxo.");
                }

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
                    progress: busy.Session);
            }

            WriteProbePhase("List", phaseWatch.ElapsedMilliseconds);
            if (!listReady)
            {
                if (selection.GenerateMetadata)
                {
                    WriteOutput($"[Genexus Open API Builder][B060] Metadata nao foi gravada para Transaction='{transaction.Name}' porque B070 falhou ou foi bloqueado neste fluxo.");
                }

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
        }
        catch (ApiPlanBusyAbortedException abortEx)
        {
            WriteOutput($"[Genexus Open API Builder][B082] Apply Wizard abortado: Transaction='{transaction.Name}', Error='{abortEx.Message}'");
            report.HeadlineOverride = "Aplicação abortada pelo usuário.";
            report.AddWarning(abortEx.Message);
            report.AddBlocked("Apply", transaction.Name, "Abortado [B082]");
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

    private static bool QueryConfigureWizardContract(CommandData data, ref CommandStatus status)
    {
        status.Visible(true);
        return true;
    }

    private static bool ExecuteConfigureWizardContract(CommandData data)
    {
        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        if (knowledgeBase is null)
        {
            PrototypeWizardSessionState.ClearContractSelection();
            PrototypeWizardReviewSessionState.ClearReviewSelection();
            WriteOutput("[Genexus Open API Builder][B031] Nenhuma Knowledge Base ativa foi encontrada. Abra uma KB e execute o comando novamente.");
            return true;
        }

        PrototypeTransactionSelectionState.ClearIfKnowledgeBaseChanged(knowledgeBase);
        var selectedTransaction = PrototypeTransactionSelectionState.Current;
        if (selectedTransaction is null)
        {
            PrototypeWizardSessionState.ClearContractSelection();
            PrototypeWizardReviewSessionState.ClearReviewSelection();
            WriteOutput("[Genexus Open API Builder][B031] Nenhuma Transaction selecionada em memoria. Execute primeiro o comando Abrir Wizard (B030).");
            return true;
        }

        var transaction = Transaction.GetAll(knowledgeBase.DesignModel)
            .SingleOrDefault(item => item.Guid == selectedTransaction.TransactionGuid);
        if (transaction is null)
        {
            PrototypeWizardSessionState.ClearContractSelection();
            PrototypeWizardReviewSessionState.ClearReviewSelection();
            WriteOutput($"[Genexus Open API Builder][B031] A Transaction selecionada em memoria nao foi reencontrada: Name='{selectedTransaction.TransactionName}', Guid='{selectedTransaction.TransactionGuid}'. Nenhuma escolha foi persistida.");
            return true;
        }

        var snapshot = PrototypeWizardContractReader.Read(transaction);
        using var dialog = new PrototypeWizardContractDialog(snapshot, ExtensionLocalization.For(knowledgeBase));
        var result = dialog.ShowDialog();

        if (result == System.Windows.Forms.DialogResult.Retry)
        {
            PrototypeWizardSessionState.ClearContractSelection();
            PrototypeWizardReviewSessionState.ClearReviewSelection();
            WriteOutput($"[Genexus Open API Builder][B031] Voltar acionado no Passo 2. Transaction='{transaction.Name}' permaneceu selecionada em memoria; nenhuma escolha de contrato foi persistida.");
            return true;
        }

        if (result == System.Windows.Forms.DialogResult.Cancel)
        {
            PrototypeWizardSessionState.ClearContractSelection();
            PrototypeWizardReviewSessionState.ClearReviewSelection();
            PrototypeTransactionSelectionState.Clear();
            WriteOutput($"[Genexus Open API Builder][B031] Wizard cancelado no Passo 2 para Transaction='{transaction.Name}'. Escolhas em memoria descartadas; nenhuma alteracao foi feita na KB.");
            return true;
        }

        if (result != System.Windows.Forms.DialogResult.OK || dialog.Selection is null)
        {
            PrototypeWizardSessionState.ClearContractSelection();
            PrototypeWizardReviewSessionState.ClearReviewSelection();
            WriteOutput($"[Genexus Open API Builder][B031] Passo 2 fechado sem conclusao para Transaction='{transaction.Name}'. Nenhuma escolha foi persistida.");
            return true;
        }

        var selection = dialog.Selection;
        PrototypeWizardSessionState.StoreContractSelection(selection);
        PrototypeWizardReviewSessionState.ClearReviewSelection();
        WriteOutput($"[Genexus Open API Builder][B031] Wizard Passo 2 concluido em memoria: Transaction='{selection.TransactionName}', Services='{string.Join(",", selection.SelectedServices)}'.");
        WriteOutput($"[Genexus Open API Builder][B031] Campos selecionados: Create={selection.CreateFields.Count}, Update={selection.UpdateFields.Count}, Response={selection.ResponseFields.Count}, ListFilters={selection.ListFilters.Count}.");
        WriteOutput("[Genexus Open API Builder][B031] Proximo passo habilitado para B032. Nenhum ApiPlan foi criado, nenhuma escolha foi persistida e nenhum objeto foi criado, alterado ou excluido.");

        return true;
    }

    private static bool QueryReviewWizardPathsAndSecurity(CommandData data, ref CommandStatus status)
    {
        status.Visible(true);
        return true;
    }

    private static bool ExecuteReviewWizardPathsAndSecurity(CommandData data)
    {
        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        if (knowledgeBase is null)
        {
            PrototypeWizardReviewSessionState.ClearReviewSelection();
            PrototypeWizardSessionState.ClearContractSelection();
            WriteOutput("[Genexus Open API Builder][B032] Nenhuma Knowledge Base ativa foi encontrada. Abra uma KB e execute o comando novamente.");
            return true;
        }

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
                PrototypeWizardReviewSessionState.ClearReviewSelection();
                PrototypeWizardSessionState.ClearContractSelection();
                WriteOutput("[Genexus Open API Builder][B032] A Transaction do menu de contexto nao foi reencontrada na Knowledge Base ativa. Nenhuma escolha foi persistida.");
                return true;
            }

            var current = PrototypeTransactionSelectionState.Current;
            if (current is null || current.TransactionGuid != transaction.Guid)
            {
                PrototypeWizardReviewSessionState.ClearReviewSelection();
                PrototypeWizardSessionState.ClearContractSelection();
            }

            PrototypeTransactionSelectionState.Store(knowledgeBase, transaction);
        }
        else
        {
            selectionSource = "Memoria";
            var selectedTransaction = PrototypeTransactionSelectionState.Current;
            if (selectedTransaction is null)
            {
                PrototypeWizardReviewSessionState.ClearReviewSelection();
                PrototypeWizardSessionState.ClearContractSelection();
                WriteOutput("[Genexus Open API Builder][B032] Nenhuma Transaction selecionada. Use o menu de contexto de uma Transaction ou execute primeiro o comando Abrir Wizard (B030).");
                return true;
            }

            transaction = Transaction.GetAll(knowledgeBase.DesignModel)
                .SingleOrDefault(item => item.Guid == selectedTransaction.TransactionGuid);
            if (transaction is null)
            {
                PrototypeWizardReviewSessionState.ClearReviewSelection();
                PrototypeWizardSessionState.ClearContractSelection();
                WriteOutput($"[Genexus Open API Builder][B032] A Transaction selecionada em memoria nao foi reencontrada: Name='{selectedTransaction.TransactionName}', Guid='{selectedTransaction.TransactionGuid}'. Nenhuma escolha foi persistida.");
                return true;
            }
        }

        var module = transaction.Module;
        if (module is null)
        {
            PrototypeWizardReviewSessionState.ClearReviewSelection();
            PrototypeWizardSessionState.ClearContractSelection();
            WriteOutput($"[Genexus Open API Builder][B032] A Transaction selecionada nao possui modulo disponivel: Name='{transaction.Name}'. Nenhuma escolha foi persistida.");
            return true;
        }

        WriteOutput($"[Genexus Open API Builder][B032] Transaction resolvida para o wizard: Name='{transaction.Name}', Module='{module.Name}', SelectionSource='{selectionSource}'.");

        if (!EnsureContractSelectionForB032(transaction))
        {
            return true;
        }

        while (true)
        {
            var contractSelection = PrototypeWizardSessionState.ContractSelection;
            if (contractSelection is null)
            {
                PrototypeWizardReviewSessionState.ClearReviewSelection();
                WriteOutput($"[Genexus Open API Builder][B032] Contrato B031 ausente para Transaction='{transaction.Name}'. Nenhuma escolha foi persistida.");
                return true;
            }

            var snapshot = PrototypeWizardReviewReader.Read(transaction, contractSelection);
            using var dialog = new PrototypeWizardReviewDialog(snapshot, ExtensionLocalization.For(knowledgeBase));
            var result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.Retry)
            {
                PrototypeWizardReviewSessionState.ClearReviewSelection();
                WriteOutput($"[Genexus Open API Builder][B032] Voltar acionado no Passo 3. Reabrindo B031 para Transaction='{transaction.Name}' sem persistir escolhas.");
                if (!RunContractDialogForB032(transaction))
                {
                    return true;
                }

                continue;
            }

            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                PrototypeWizardReviewSessionState.ClearReviewSelection();
                PrototypeWizardSessionState.ClearContractSelection();
                PrototypeTransactionSelectionState.Clear();
                WriteOutput($"[Genexus Open API Builder][B032] Wizard cancelado no Passo 3 para Transaction='{transaction.Name}'. Escolhas em memoria descartadas; nenhuma alteracao foi feita na KB.");
                return true;
            }

            if (result != System.Windows.Forms.DialogResult.OK || dialog.Selection is null)
            {
                PrototypeWizardReviewSessionState.ClearReviewSelection();
                WriteOutput($"[Genexus Open API Builder][B032] Passo 3 fechado sem conclusao para Transaction='{transaction.Name}'. Nenhuma escolha foi persistida.");
                return true;
            }

            var selection = dialog.Selection;
            PrototypeWizardReviewSessionState.StoreReviewSelection(selection);
            WriteOutput($"[Genexus Open API Builder][B032] Wizard Passo 3 concluido em memoria: Transaction='{selection.TransactionName}', ApiName='{selection.ApiName}', ServicesBasePath='{selection.ServicesBasePath}', RestPath='{selection.RestPath}', SecurityLevel='{selection.SecurityLevel}'.");
            WriteOutput($"[Genexus Open API Builder][B032] Paginacao e ordenacao: DefaultPageSize={selection.DefaultPageSize}, MaximumPageSize={selection.MaximumPageSize}, StaticOrder='{string.Join(",", selection.StaticOrder.Select(item => item.AttributeName + " " + item.Direction))}'.");
            WriteOutput("[Genexus Open API Builder][B032] Proximo passo habilitado para B033. Nenhum ApiPlan foi criado, nenhuma escolha foi persistida e nenhum objeto foi criado, alterado ou excluido.");
            return true;
        }
    }

    private static bool EnsureContractSelectionForB032(Transaction transaction)
    {
        var contractSelection = PrototypeWizardSessionState.ContractSelection;
        if (contractSelection is not null && string.Equals(contractSelection.TransactionName, transaction.Name, StringComparison.Ordinal))
        {
            return true;
        }

        WriteOutput($"[Genexus Open API Builder][B032] Contrato B031 ausente ou incompativel para Transaction='{transaction.Name}'. Abrindo B031 automaticamente.");
        return RunContractDialogForB032(transaction);
    }

    private static bool RunContractDialogForB032(Transaction transaction)
    {
        var snapshot = PrototypeWizardContractReader.Read(transaction);
        using var dialog = new PrototypeWizardContractDialog(
            snapshot,
            ExtensionLocalization.For(UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null));
        var result = dialog.ShowDialog();

        if (result == System.Windows.Forms.DialogResult.Retry)
        {
            PrototypeWizardSessionState.ClearContractSelection();
            PrototypeWizardReviewSessionState.ClearReviewSelection();
            WriteOutput($"[Genexus Open API Builder][B031] Voltar acionado durante o fluxo B032. Transaction='{transaction.Name}' permaneceu selecionada em memoria; nenhuma escolha de contrato foi persistida.");
            return false;
        }

        if (result == System.Windows.Forms.DialogResult.Cancel)
        {
            PrototypeWizardSessionState.ClearContractSelection();
            PrototypeWizardReviewSessionState.ClearReviewSelection();
            PrototypeTransactionSelectionState.Clear();
            WriteOutput($"[Genexus Open API Builder][B031] Wizard cancelado durante o fluxo B032 para Transaction='{transaction.Name}'. Escolhas em memoria descartadas; nenhuma alteracao foi feita na KB.");
            return false;
        }

        if (result != System.Windows.Forms.DialogResult.OK || dialog.Selection is null)
        {
            PrototypeWizardSessionState.ClearContractSelection();
            PrototypeWizardReviewSessionState.ClearReviewSelection();
            WriteOutput($"[Genexus Open API Builder][B031] Passo 2 fechado sem conclusao durante o fluxo B032 para Transaction='{transaction.Name}'. Nenhuma escolha foi persistida.");
            return false;
        }

        var selection = dialog.Selection;
        PrototypeWizardSessionState.StoreContractSelection(selection);
        PrototypeWizardReviewSessionState.ClearReviewSelection();
        WriteOutput($"[Genexus Open API Builder][B031] Wizard Passo 2 concluido em memoria durante o fluxo B032: Transaction='{selection.TransactionName}', Services='{string.Join(",", selection.SelectedServices)}'.");
        WriteOutput($"[Genexus Open API Builder][B031] Campos selecionados: Create={selection.CreateFields.Count}, Update={selection.UpdateFields.Count}, Response={selection.ResponseFields.Count}, ListFilters={selection.ListFilters.Count}.");
        return true;
    }

    private static bool EnableBusinessComponentForWizard(Transaction transaction)
    {
        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        if (!transaction.IsBusinessComponent)
        {
            transaction.SetPropertyValue("idISBUSINESSCOMPONENT", true);
            transaction.Save();
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

        if (apiPlan.BusinessComponent.EnabledDuringWizard)
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
        ApiPlan? apiPlan = null)
    {
        // B082: a apresentacao do relatorio roda dentro do escopo de medicao do Sync,
        // mas nao faz parte da operacao medida. Suspender evita atribuir a ela as
        // leituras de TryResolveMainObjectFromKb e de qualquer consulta futura daqui.
        using var scanSuspension = ApiPlanScanProbe.Suspend();
        AppendPlanSideEffects(collector, apiPlan);
        TryResolveMainObjectFromKb(collector, designModel);
        var report = collector.Build(elapsed);
        WriteOutput(report.BuildOutputSummary());
        foreach (var item in report.Created)
        {
            WriteOutput($"[Genexus Open API Builder][B081] Criado: Kind='{item.ObjectKind}', Name='{item.Name}'.");
        }

        foreach (var item in report.Updated)
        {
            WriteOutput($"[Genexus Open API Builder][B081] Atualizado: Kind='{item.ObjectKind}', Name='{item.Name}'.");
        }

        foreach (var item in report.Deleted)
        {
            WriteOutput($"[Genexus Open API Builder][B081] Removido: Kind='{item.ObjectKind}', Name='{item.Name}'.");
        }

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

    private static void TryResolveMainObjectFromKb(ApiPlanApplicationFinalReportCollector collector, KBModel? designModel)
    {
        if (designModel is null
            || collector.MainObjectGuid.HasValue
            || string.IsNullOrWhiteSpace(collector.ApiName)
            || string.Equals(collector.Operation, "Remover", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var apiObject = API.GetAll(designModel)
            .FirstOrDefault(item => string.Equals(item.Name, collector.ApiName, StringComparison.OrdinalIgnoreCase));
        if (apiObject is not null)
        {
            collector.SetMainObject(apiObject.Name, apiObject.Guid);
        }
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

    private static void WriteProbePhase(string phaseName, long elapsedMs)
    {
        WriteOutput($"[Genexus Open API Builder][B082] Fase {phaseName}={elapsedMs} ms.");
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
        if (telemetry is null || telemetry.ScanCount == 0)
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
        output.Show(outputId);
    }

}
