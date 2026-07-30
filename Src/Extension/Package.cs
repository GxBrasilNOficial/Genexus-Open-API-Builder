using System;
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
/// O placeholder mantém o submenu do produto visível, os comandos B020-B025
/// executam leituras manuais, as preferências do wizard são persistidas por KB
/// e B030 abre o wizard navegável com geração incremental.
/// </summary>
public sealed class Package : AbstractPackageUI
{
    public override string Name => "Genexus Open API Builder";

    public override void Initialize(IGxServiceProvider services)
    {
        base.Initialize(services);

        AddCommand(new CommandKey(Id, "Futura Primeira Opção"), ExecuteFutureFirstOption, QueryFutureFirstOption);
        AddCommand(new CommandKey(Id, "Configurar Preferências do Wizard"), ExecuteConfigureWizardPreferences, QueryConfigureWizardPreferences);
        AddCommand(new CommandKey(Id, "Detectar KB Ativa (B020)"), ExecuteDetectActiveKnowledgeBase, QueryDetectActiveKnowledgeBase);
        AddCommand(new CommandKey(Id, "Listar Transactions Elegíveis (B021)"), ExecuteListEligibleTransactions, QueryListEligibleTransactions);
        AddCommand(new CommandKey(Id, "Selecionar Transaction e Ler Módulo (B022)"), ExecuteSelectTransactionAndReadModule, QuerySelectTransactionAndReadModule);
        AddCommand(new CommandKey(Id, "Detectar Objetos Existentes (B023)"), ExecuteDetectExistingObjects, QueryDetectExistingObjects);
        AddCommand(new CommandKey(Id, "Verificar Business Component (B024)"), ExecuteCheckBusinessComponent, QueryCheckBusinessComponent);
        AddCommand(new CommandKey(Id, "Ler Chave Primária (B025)"), ExecuteReadPrimaryKey, QueryReadPrimaryKey);
        AddCommand(new CommandKey(Id, "Criar SDTs (B040-B046)"), ExecuteCreateSdts, QueryCreateSdts);
        AddCommand(new CommandKey(Id, "Criar Procedures (B050-B053)"), ExecuteCreateProcedures, QueryCreateProcedures);
        AddCommand(new CommandKey(Id, "Criar API Object (B054)"), ExecuteCreateApiObject, QueryCreateApiObject);
        AddCommand(new CommandKey(Id, "Abrir Wizard (B030)"), ExecuteOpenWizardStepOne, QueryOpenWizardStepOne);
    }

    private static bool QueryFutureFirstOption(CommandData data, ref CommandStatus status)
    {
        status.Visible(true);
        return true;
    }

    private static bool ExecuteFutureFirstOption(CommandData data)
    {
        return true;
    }

    private static bool QueryConfigureWizardPreferences(CommandData data, ref CommandStatus status)
    {
        status.Visible(true);
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

        var loadResult = PrototypeWizardPreferencesStore.Load(knowledgeBase.DesignModel);
        using var dialog = new PrototypeWizardPreferencesDialog(loadResult.Preferences, loadResult.Status);
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

    private static bool QueryCreateSdts(CommandData data, ref CommandStatus status)
    {
        status.Visible(true);
        return true;
    }

    private static bool ExecuteCreateSdts(CommandData data)
    {
        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        if (knowledgeBase is null)
        {
            WriteOutput("[Genexus Open API Builder][B040-B046] Nenhuma Knowledge Base ativa foi encontrada. Abra uma KB e execute o comando novamente.");
            return true;
        }

        PrototypeTransactionSelectionState.ClearIfKnowledgeBaseChanged(knowledgeBase);
        var apiPlan = ApiPlanSessionState.Current;
        if (apiPlan is null)
        {
            WriteOutput("[Genexus Open API Builder][B040-B046] Nenhum ApiPlan em memoria foi encontrado. Execute e conclua primeiro o comando Abrir Wizard (B030). Nenhuma alteracao foi feita na KB.");
            return true;
        }

        var selectedTransaction = PrototypeTransactionSelectionState.Current;
        if (selectedTransaction is null)
        {
            WriteOutput("[Genexus Open API Builder][B040-B046] Nenhuma Transaction selecionada em memoria foi encontrada. Execute e conclua primeiro o comando Abrir Wizard (B030). Nenhuma alteracao foi feita na KB.");
            return true;
        }

        var transaction = Transaction.GetAll(knowledgeBase.DesignModel)
            .SingleOrDefault(item => item.Guid == selectedTransaction.TransactionGuid);
        if (transaction is null)
        {
            WriteOutput($"[Genexus Open API Builder][B040-B046] A Transaction selecionada em memoria nao foi reencontrada: Name='{selectedTransaction.TransactionName}', Guid='{selectedTransaction.TransactionGuid}'. Nenhuma alteracao foi feita na KB.");
            return true;
        }

        if (!string.Equals(transaction.Name, apiPlan.TransactionName, StringComparison.Ordinal))
        {
            WriteOutput($"[Genexus Open API Builder][B040-B046] ApiPlan em memoria pertence a Transaction='{apiPlan.TransactionName}', mas a selecao atual e Transaction='{transaction.Name}'. Execute novamente o wizard. Nenhuma alteracao foi feita na KB.");
            return true;
        }

        TryConfirmAndCreateSdts(knowledgeBase.DesignModel, transaction, apiPlan, "Comando");
        return true;
    }

    private static bool QueryCreateProcedures(CommandData data, ref CommandStatus status)
    {
        status.Visible(true);
        return true;
    }

    private static bool ExecuteCreateProcedures(CommandData data)
    {
        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        if (knowledgeBase is null)
        {
            WriteOutput("[Genexus Open API Builder][B050-B053] Nenhuma Knowledge Base ativa foi encontrada. Abra uma KB e execute o comando novamente.");
            return true;
        }

        PrototypeTransactionSelectionState.ClearIfKnowledgeBaseChanged(knowledgeBase);
        var apiPlan = ApiPlanSessionState.Current;
        if (apiPlan is null)
        {
            WriteOutput("[Genexus Open API Builder][B050-B053] Nenhum ApiPlan em memoria foi encontrado. Execute e conclua primeiro o comando Abrir Wizard (B030). Nenhuma alteracao foi feita na KB.");
            return true;
        }

        var selectedTransaction = PrototypeTransactionSelectionState.Current;
        if (selectedTransaction is null)
        {
            WriteOutput("[Genexus Open API Builder][B050-B053] Nenhuma Transaction selecionada em memoria foi encontrada. Execute e conclua primeiro o comando Abrir Wizard (B030). Nenhuma alteracao foi feita na KB.");
            return true;
        }

        var transaction = Transaction.GetAll(knowledgeBase.DesignModel)
            .SingleOrDefault(item => item.Guid == selectedTransaction.TransactionGuid);
        if (transaction is null)
        {
            WriteOutput($"[Genexus Open API Builder][B050-B053] A Transaction selecionada em memoria nao foi reencontrada: Name='{selectedTransaction.TransactionName}', Guid='{selectedTransaction.TransactionGuid}'. Nenhuma alteracao foi feita na KB.");
            return true;
        }

        if (!string.Equals(transaction.Name, apiPlan.TransactionName, StringComparison.Ordinal))
        {
            WriteOutput($"[Genexus Open API Builder][B050-B053] ApiPlan em memoria pertence a Transaction='{apiPlan.TransactionName}', mas a selecao atual e Transaction='{transaction.Name}'. Execute novamente o wizard. Nenhuma alteracao foi feita na KB.");
            return true;
        }

        TryConfirmAndCreateProcedures(knowledgeBase.DesignModel, transaction, apiPlan, "Comando");
        return true;
    }

    private static bool QueryCreateApiObject(CommandData data, ref CommandStatus status)
    {
        status.Visible(true);
        return true;
    }

    private static bool ExecuteCreateApiObject(CommandData data)
    {
        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        if (knowledgeBase is null)
        {
            WriteOutput("[Genexus Open API Builder][B054] Nenhuma Knowledge Base ativa foi encontrada. Abra uma KB e execute o comando novamente.");
            return true;
        }

        PrototypeTransactionSelectionState.ClearIfKnowledgeBaseChanged(knowledgeBase);
        var apiPlan = ApiPlanSessionState.Current;
        if (apiPlan is null)
        {
            WriteOutput("[Genexus Open API Builder][B054] Nenhum ApiPlan em memoria foi encontrado. Execute e conclua primeiro o comando Abrir Wizard (B030). Nenhuma alteracao foi feita na KB.");
            return true;
        }

        var selectedTransaction = PrototypeTransactionSelectionState.Current;
        if (selectedTransaction is null)
        {
            WriteOutput("[Genexus Open API Builder][B054] Nenhuma Transaction selecionada em memoria foi encontrada. Execute e conclua primeiro o comando Abrir Wizard (B030). Nenhuma alteracao foi feita na KB.");
            return true;
        }

        var transaction = Transaction.GetAll(knowledgeBase.DesignModel)
            .SingleOrDefault(item => item.Guid == selectedTransaction.TransactionGuid);
        if (transaction is null)
        {
            WriteOutput($"[Genexus Open API Builder][B054] A Transaction selecionada em memoria nao foi reencontrada: Name='{selectedTransaction.TransactionName}', Guid='{selectedTransaction.TransactionGuid}'. Nenhuma alteracao foi feita na KB.");
            return true;
        }

        if (!string.Equals(transaction.Name, apiPlan.TransactionName, StringComparison.Ordinal))
        {
            WriteOutput($"[Genexus Open API Builder][B054] ApiPlan em memoria pertence a Transaction='{apiPlan.TransactionName}', mas a selecao atual e Transaction='{transaction.Name}'. Execute novamente o wizard. Nenhuma alteracao foi feita na KB.");
            return true;
        }

        TryConfirmAndCreateApiObject(knowledgeBase.DesignModel, transaction, apiPlan, "Comando");
        return true;
    }
    private static bool TryConfirmAndCreateSdts(KBModel designModel, Transaction transaction, ApiPlan apiPlan, string triggerSource)
    {
        var confirmation = System.Windows.Forms.MessageBox.Show(
            "B040-B046 vai criar ou reencontrar 2 SDTs compartilhados e 5 SDTs proprios a partir do ApiPlan em memoria. Nao cria Procedures, API Object ou metadata persistente definitiva. Confirma a escrita desses SDTs na KB ativa?",
            "Confirmar criacao de SDTs B040-B046",
            System.Windows.Forms.MessageBoxButtons.YesNo,
            System.Windows.Forms.MessageBoxIcon.Warning,
            System.Windows.Forms.MessageBoxDefaultButton.Button2);
        if (confirmation != System.Windows.Forms.DialogResult.Yes)
        {
            WriteOutput($"[Genexus Open API Builder][B040-B046] Criacao de SDTs cancelada pelo usuario para Transaction='{transaction.Name}', Trigger='{triggerSource}'. Nenhuma alteracao foi feita na KB.");
            return false;
        }

        return TryCreateSdts(designModel, transaction, apiPlan, triggerSource);
    }

    private static bool TryCreateSdts(KBModel designModel, Transaction transaction, ApiPlan apiPlan, string triggerSource)
    {
        try
        {
            var result = ApiPlanSdtWriter.CreateOrReencounter(designModel, transaction, apiPlan);
            WriteOutput($"[Genexus Open API Builder][B040-B046] Escrita de SDTs concluida: Transaction='{transaction.Name}', Trigger='{triggerSource}', PlannedOwnSdts={result.PlannedOwnSdts}, PlannedSharedSdts={result.PlannedSharedSdts}, Created={result.CreatedSdts}, Reencountered={result.ReencounteredSdts}, TransactionFolder='{result.TransactionFolderName}', TransactionFolderGuid='{result.TransactionFolderGuid}'. Nenhuma Procedure, API Object ou metadata persistente definitiva foi criada.");
            foreach (var item in result.Items)
            {
                WriteOutput($"[Genexus Open API Builder][B040-B046] SDT {item.Status}: Backlog='{item.BacklogId}', Kind='{item.Kind}', Name='{item.Name}', Scope='{item.Scope}', Guid='{item.Guid}'.");
            }

            return true;
        }
        catch (Exception ex)
        {
            WriteOutput($"[Genexus Open API Builder][B040-B046] Criacao de SDTs bloqueada por preflight ou falhou antes de concluir: Trigger='{triggerSource}', Error='{ex.Message}'");
            return false;
        }
    }

    private static bool TryConfirmAndCreateProcedures(KBModel designModel, Transaction transaction, ApiPlan apiPlan, string triggerSource)
    {
        var confirmation = System.Windows.Forms.MessageBox.Show(
            "B050-B053 vai reencontrar os 7 SDTs de B040-B046 e criar ou reencontrar 4 Procedures skeleton a partir do ApiPlan em memoria. Nao cria API Object, REST completo ou metadata persistente definitiva. Confirma a escrita dessas Procedures na KB ativa?",
            "Confirmar criacao de Procedures B050-B053",
            System.Windows.Forms.MessageBoxButtons.YesNo,
            System.Windows.Forms.MessageBoxIcon.Warning,
            System.Windows.Forms.MessageBoxDefaultButton.Button2);
        if (confirmation != System.Windows.Forms.DialogResult.Yes)
        {
            WriteOutput($"[Genexus Open API Builder][B050-B053] Criacao de Procedures cancelada pelo usuario para Transaction='{transaction.Name}', Trigger='{triggerSource}'. Nenhuma alteracao foi feita na KB.");
            return false;
        }

        return TryCreateProcedures(designModel, transaction, apiPlan, triggerSource);
    }

    private static bool TryCreateProcedures(KBModel designModel, Transaction transaction, ApiPlan apiPlan, string triggerSource)
    {
        try
        {
            var result = ApiPlanProcedureWriter.CreateOrReencounter(designModel, transaction, apiPlan);
            WriteOutput($"[Genexus Open API Builder][B050-B053] Escrita de Procedures concluida: Transaction='{transaction.Name}', Trigger='{triggerSource}', PlannedProcedures={result.PlannedProcedures}, ReencounteredSdts={result.ReencounteredSdts}, Created={result.CreatedProcedures}, Reencountered={result.ReencounteredProcedures}, TransactionFolder='{result.TransactionFolderName}', TransactionFolderGuid='{result.TransactionFolderGuid}'. Nenhum API Object, REST completo ou metadata persistente definitiva foi criado.");
            foreach (var item in result.Items)
            {
                WriteOutput($"[Genexus Open API Builder][B050-B053] Procedure {item.Status}: Backlog='{item.BacklogId}', Service='{item.ServiceName}', Name='{item.Name}', Guid='{item.Guid}'.");
            }

            return true;
        }
        catch (Exception ex)
        {
            WriteOutput($"[Genexus Open API Builder][B050-B053] Criacao de Procedures bloqueada por preflight ou falhou antes de concluir: Trigger='{triggerSource}', Error='{ex.Message}'");
            return false;
        }
    }
    private static bool TryConfirmAndCreateApiObject(KBModel designModel, Transaction transaction, ApiPlan apiPlan, string triggerSource)
    {
        var confirmation = System.Windows.Forms.MessageBox.Show(
            $"B054 vai reencontrar os 7 SDTs de B040-B046 e as 4 Procedures de B050-B053 para criar ou reencontrar o API Object '{apiPlan.ApiName}'. Nao completa REST, seguranca definitiva ou metadata persistente definitiva. Confirma a escrita desse API Object na KB ativa?",
            "Confirmar criacao de API Object B054",
            System.Windows.Forms.MessageBoxButtons.YesNo,
            System.Windows.Forms.MessageBoxIcon.Warning,
            System.Windows.Forms.MessageBoxDefaultButton.Button2);
        if (confirmation != System.Windows.Forms.DialogResult.Yes)
        {
            WriteOutput($"[Genexus Open API Builder][B054] Criacao de API Object cancelada pelo usuario para Transaction='{transaction.Name}', Trigger='{triggerSource}'. Nenhuma alteracao foi feita na KB.");
            return false;
        }

        return TryCreateApiObject(designModel, transaction, apiPlan, triggerSource);
    }

    private static bool TryCreateApiObject(KBModel designModel, Transaction transaction, ApiPlan apiPlan, string triggerSource)
    {
        try
        {
            var result = ApiPlanApiObjectWriter.CreateOrReencounter(designModel, transaction, apiPlan);
            WriteOutput($"[Genexus Open API Builder][B054] Escrita de API Object concluida: Transaction='{transaction.Name}', Trigger='{triggerSource}', ApiName='{result.ApiName}', Status='{result.Status}', ReencounteredSdts={result.ReencounteredSdts}, ReencounteredProcedures={result.ReencounteredProcedures}, PlannedServices={result.PlannedServices}, TransactionFolder='{result.TransactionFolderName}', TransactionFolderGuid='{result.TransactionFolderGuid}'. Nenhum REST completo, seguranca definitiva ou metadata persistente definitiva foi criado.");
            foreach (var procedure in result.Procedures)
            {
                WriteOutput($"[Genexus Open API Builder][B054] Procedure reencontrada para API Object: Backlog='{procedure.BacklogId}', Service='{procedure.ServiceName}', Name='{procedure.Name}', Guid='{procedure.Guid}'.");
            }

            WriteOutput($"[Genexus Open API Builder][B054] API Object {result.Status}: Name='{result.ApiName}', Guid='{result.Guid}'.");
            WriteOutput($"[Genexus Open API Builder][B056] Descricoes aplicadas no API Object real: Transaction='{transaction.Name}', Trigger='{triggerSource}', ApiName='{result.ApiName}', DescribedServices={apiPlan.ServiceDescriptions.Count}. Sem antecipar REST completo, codigo HTTP, seguranca definitiva ou metadata persistente.");
            return true;
        }
        catch (Exception ex)
        {
            WriteOutput($"[Genexus Open API Builder][B054] Criacao de API Object bloqueada por preflight ou falhou antes de concluir: Trigger='{triggerSource}', Error='{ex.Message}'");
            return false;
        }
    }

    private static bool TryWriteMetadataFile(KBModel designModel, Transaction transaction, ApiPlan apiPlan, string triggerSource)
    {
        try
        {
            var result = ApiPlanMetadataFileWriter.CreateOrReencounter(designModel, transaction, apiPlan);
            WriteOutput($"[Genexus Open API Builder][B060] Metadata persistente inicial gravada: Transaction='{transaction.Name}', Trigger='{triggerSource}', File='{result.FileName}', Status='{result.Status}', Guid='{result.Guid}', SchemaVersion='{result.SchemaVersion}', Bytes={result.Bytes}, Sha256='{result.Sha256}'. Sem completar REST, codigos HTTP finais ou seguranca definitiva.");
            WriteOutput($"[Genexus Open API Builder][B067] Metadata de integridade gravada: Transaction='{transaction.Name}', Trigger='{triggerSource}', File='{result.FileName}', IntegrityVersion='{result.IntegrityVersion}', PlannedContractHash='{result.PlannedContractHash}'. Reexecucoes com descricoes, ownership, Service Source ou contrato essencial divergente serao bloqueadas antes de qualquer Save().");
            return true;
        }
        catch (Exception ex)
        {
            var errorDetail = ex.InnerException is null ? ex.Message : $"{ex.Message} | Inner='{ex.InnerException.Message}'";
            WriteOutput($"[Genexus Open API Builder][B060] Gravacao de metadata bloqueada por preflight ou falhou antes de concluir: Trigger='{triggerSource}', Error='{errorDetail}'");
            return false;
        }
    }

    private static bool TryApplyBusinessComponent(KBModel designModel, Transaction transaction, ApiPlan apiPlan, string triggerSource)
    {
        try
        {
            var result = ApiPlanBusinessComponentWriter.Apply(designModel, transaction, apiPlan);
            WriteOutput($"[Genexus Open API Builder][B055] Create/Update aplicados via Business Component e API Object sincronizado: Transaction='{transaction.Name}', Trigger='{triggerSource}', CreateProcedureGuid='{result.CreateProcedureGuid}', UpdateProcedureGuid='{result.UpdateProcedureGuid}', ApiObjectGuid='{result.ApiObjectGuid}', PrimaryKeyParts={result.PrimaryKeyParts}, CreateFields={result.CreateFields}, UpdateFields={result.UpdateFields}, ResponseFields={result.ResponseFields}. Nenhum REST completo, codigo HTTP, seguranca definitiva ou metadata persistente foi criado.");
            WriteOutput($"[Genexus Open API Builder][B056] Descricoes reaplicadas no API Object real durante B055: Transaction='{transaction.Name}', Trigger='{triggerSource}', ApiObjectGuid='{result.ApiObjectGuid}', DescribedServices={apiPlan.ServiceDescriptions.Count}. Service Source permaneceu limitado ao contrato Procedure/API Object atual.");
            return true;
        }
        catch (Exception ex)
        {
            var errorDetail = ex.InnerException is null ? ex.Message : $"{ex.Message} | Inner='{ex.InnerException.Message}'";
            WriteOutput($"[Genexus Open API Builder][B055] Aplicacao de Create/Update via Business Component bloqueada por preflight ou falhou antes de concluir: Trigger='{triggerSource}', Error='{errorDetail}'");
            return false;
        }
    }

    private static bool TryApplyList(KBModel designModel, Transaction transaction, ApiPlan apiPlan, string triggerSource)
    {
        try
        {
            var result = ApiPlanListProcedureWriter.Apply(designModel, transaction, apiPlan);
            WriteOutput($"[Genexus Open API Builder][B070] List aplicado e API Object sincronizado: Transaction='{transaction.Name}', Trigger='{triggerSource}', ListProcedureGuid='{result.ListProcedureGuid}', ApiObjectGuid='{result.ApiObjectGuid}', Filters={result.Filters}, OrderParts={result.OrderParts}, DefaultPageSize={result.DefaultPageSize}, MaximumPageSize={result.MaximumPageSize}. B076 e codigos HTTP finais permanecem pendentes de validacao runtime.");
            return true;
        }
        catch (Exception ex)
        {
            var errorDetail = ex.InnerException is null ? ex.Message : $"{ex.Message} | Inner='{ex.InnerException.Message}'";
            WriteOutput($"[Genexus Open API Builder][B070] Aplicacao do List bloqueada por preflight ou falhou antes de concluir: Trigger='{triggerSource}', Error='{errorDetail}'");
            return false;
        }
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
                DialogTitle = "Selecionar Transaction para o wizard (B030)",
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

        var preferencesLoadResult = PrototypeWizardPreferencesStore.Load(knowledgeBase.DesignModel);
        WriteOutput($"[Genexus Open API Builder][Prefs] {preferencesLoadResult.Status}");

        var snapshot = PrototypeWizardContractReader.Read(transaction);
        var businessComponentSnapshot = PrototypeBusinessComponentReader.Read(transaction);
        using var dialog = new PrototypeWizardDialog(
            knowledgeBase.DesignModel,
            transaction,
            snapshot,
            businessComponentSnapshot,
            preferencesLoadResult.Preferences,
            () => EnableBusinessComponentForWizard(transaction),
            WriteOutput);
        var result = dialog.ShowDialog();
        var businessComponentExitStatus = dialog.BusinessComponentEnabledDuringWizard
            ? "Business Component foi habilitado por confirmacao explicita antes da saida; essa alteracao foi gravada na KB e nao foi revertida automaticamente."
            : "Nenhuma alteracao foi feita na KB.";

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
        var apiPlan = ApiPlanBuilder.Build(transaction, selection);
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
        WriteOutput($"[Genexus Open API Builder][B033] Obrigatoriedade em memoria: CreateRequired={createRequiredCount}, UpdateRequired={updateRequiredCount}. Required significa presença do membro JSON, nao valor nao-vazio.");
        WriteOutput($"[Genexus Open API Builder][B037] Obrigatorio no payload consolidado: CreateRequired={createRequiredCount}, UpdateRequired={updateRequiredCount}. Required e presenca do membro JSON; vazio, false e 0 continuam valores enviados. UpdateRequest segue PUT completo.");
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
        var generationState = ApiPlanGenerationStateReader.Read(knowledgeBase.DesignModel, apiPlan);
        var requireSdts = selection.GenerateSdts || selection.GenerateProcedures || selection.GenerateApiObject || selection.GenerateMetadata || selection.ApplyList || selection.ApplyBusinessComponent;
        var requireProcedures = selection.GenerateProcedures || selection.GenerateApiObject || selection.GenerateMetadata || selection.ApplyList || selection.ApplyBusinessComponent;
        var requireApiObject = selection.GenerateApiObject || selection.GenerateMetadata || selection.ApplyList || selection.ApplyBusinessComponent;
        var requireMetadataFile = selection.GenerateMetadata;
        var blockedGenerationStages = new[]
            {
                requireSdts ? generationState.Sdts : null,
                requireProcedures ? generationState.Procedures : null,
                requireApiObject ? generationState.ApiObject : null,
                requireMetadataFile ? generationState.MetadataFile : null,
            }
            .Where(stage => stage is not null)
            .Cast<ApiPlanGenerationStageState>()
            .Where(stage => stage.IsBlocked)
            .ToArray();
        if (blockedGenerationStages.Length > 0)
        {
            WriteOutput($"[Genexus Open API Builder][B063/B064/B067] Estado bloqueado detectado no wizard antes de confirmar escrita: Transaction='{transaction.Name}', BlockedStages='{string.Join(",", blockedGenerationStages.Select(stage => stage.StageName))}', Details='{string.Join(" | ", blockedGenerationStages.Select(stage => stage.Detail))}'. Nenhum Save foi solicitado.");
        }
        if (!selection.GenerateSdts && !selection.GenerateProcedures && !selection.GenerateApiObject && !selection.GenerateMetadata && !selection.ApplyList && !selection.ApplyBusinessComponent)
        {
            WriteOutput($"[Genexus Open API Builder][B040-B046/B060] Nenhuma etapa de escrita foi confirmada no wizard para Transaction='{transaction.Name}'. Nenhuma escrita foi solicitada.");
            return true;
        }

        try
        {
            ApiPlanWritePreflight.Validate(
                knowledgeBase.DesignModel,
                transaction,
                apiPlan,
                requireSdts,
                requireProcedures,
                requireApiObject,
                requireMetadataFile);
        }
        catch (Exception ex)
        {
            WriteOutput($"[Genexus Open API Builder][B063/B064/B067] Preflight agregado bloqueou o wizard antes do primeiro Save(): Transaction='{transaction.Name}', Error='{ex.Message}'");
            return true;
        }

        WriteOutput($"[Genexus Open API Builder][B063/B064/B067] Preflight agregado aprovado antes do primeiro Save(): Transaction='{transaction.Name}', ConflictMode='{apiPlan.ConflictMode}', ReexecutionMode='{apiPlan.ReexecutionMode}'.");

        var sdtsReady = true;
        if (selection.GenerateSdts)
        {
            sdtsReady = TryCreateSdts(knowledgeBase.DesignModel, transaction, apiPlan, "Wizard");
        }
        else if (selection.GenerateProcedures || selection.GenerateApiObject || selection.GenerateMetadata || selection.ApplyList || selection.ApplyBusinessComponent)
        {
            WriteOutput($"[Genexus Open API Builder][B040-B046] Etapa de SDTs nao confirmada no wizard para Transaction='{transaction.Name}'. A dependencia sera reencontrada e validada pelo preflight da etapa seguinte.");
        }

        if (!sdtsReady)
        {
            if (selection.GenerateProcedures)
            {
                WriteOutput($"[Genexus Open API Builder][B050-B053] Etapa de Procedures nao executada pelo wizard para Transaction='{transaction.Name}' porque B040-B046 falhou ou foi bloqueado neste fluxo. Nenhuma Procedure foi criada pelo wizard.");
            }

            if (selection.GenerateApiObject)
            {
                WriteOutput($"[Genexus Open API Builder][B054] Etapa de API Object nao executada pelo wizard para Transaction='{transaction.Name}' porque B040-B046 falhou ou foi bloqueado neste fluxo. Nenhum API Object foi criado pelo wizard.");
            }

            if (selection.ApplyBusinessComponent)
            {
                WriteOutput($"[Genexus Open API Builder][B055] Create/Update via Business Component nao foi aplicado para Transaction='{transaction.Name}' porque os SDTs requeridos falharam ou foram bloqueados neste fluxo.");
            }

            if (selection.ApplyList)
            {
                WriteOutput($"[Genexus Open API Builder][B070] List nao foi aplicado para Transaction='{transaction.Name}' porque B040-B046 falhou ou foi bloqueado neste fluxo.");
            }

            if (selection.GenerateMetadata)
            {
                WriteOutput($"[Genexus Open API Builder][B060] Metadata nao foi gravada para Transaction='{transaction.Name}' porque B040-B046 falhou ou foi bloqueado neste fluxo.");
            }

            return true;
        }

        var proceduresReady = true;
        if (selection.GenerateProcedures)
        {
            proceduresReady = TryCreateProcedures(knowledgeBase.DesignModel, transaction, apiPlan, "Wizard");
        }
        else if (selection.GenerateApiObject || selection.GenerateMetadata || selection.ApplyList || selection.ApplyBusinessComponent)
        {
            WriteOutput($"[Genexus Open API Builder][B050-B053] Etapa de Procedures nao confirmada no wizard para Transaction='{transaction.Name}'. A dependencia sera reencontrada e validada pelo preflight da etapa seguinte.");
        }

        if (!proceduresReady)
        {
            if (selection.GenerateApiObject)
            {
                WriteOutput($"[Genexus Open API Builder][B054] Etapa de API Object nao executada pelo wizard para Transaction='{transaction.Name}' porque B050-B053 falhou ou foi bloqueado neste fluxo. Nenhum API Object foi criado pelo wizard.");
            }

            if (selection.ApplyBusinessComponent)
            {
                WriteOutput($"[Genexus Open API Builder][B055] Create/Update via Business Component nao foi aplicado para Transaction='{transaction.Name}' porque as Procedures requeridas falharam ou foram bloqueadas neste fluxo.");
            }

            if (selection.ApplyList)
            {
                WriteOutput($"[Genexus Open API Builder][B070] List nao foi aplicado para Transaction='{transaction.Name}' porque B050-B053 falhou ou foi bloqueado neste fluxo.");
            }

            if (selection.GenerateMetadata)
            {
                WriteOutput($"[Genexus Open API Builder][B060] Metadata nao foi gravada para Transaction='{transaction.Name}' porque B050-B053 falhou ou foi bloqueado neste fluxo.");
            }

            return true;
        }

        var apiObjectReady = true;
        if (selection.GenerateApiObject && !selection.ApplyBusinessComponent)
        {
            apiObjectReady = TryCreateApiObject(knowledgeBase.DesignModel, transaction, apiPlan, "Wizard");
        }
        else if (selection.GenerateApiObject && selection.ApplyBusinessComponent)
        {
            if (API.GetAll(knowledgeBase.DesignModel).Any(api => string.Equals(api.Name, apiPlan.ApiName, StringComparison.OrdinalIgnoreCase)))
            {
                WriteOutput($"[Genexus Open API Builder][B054] API Object ja existe para Transaction='{transaction.Name}'. Como B055 tambem foi confirmado, a atualizacao do API Object sera absorvida pelo preflight de Business Component.");
            }
            else
            {
                apiObjectReady = TryCreateApiObject(knowledgeBase.DesignModel, transaction, apiPlan, "Wizard");
            }
        }
        else if (selection.ApplyBusinessComponent)
        {
            WriteOutput($"[Genexus Open API Builder][B054] Etapa de API Object nao confirmada no wizard para Transaction='{transaction.Name}'. A dependencia sera reencontrada e validada pelo preflight de Business Component.");
        }

        if (!apiObjectReady)
        {
            if (selection.ApplyBusinessComponent)
            {
                WriteOutput($"[Genexus Open API Builder][B055] Create/Update via Business Component nao foi aplicado para Transaction='{transaction.Name}' porque o API Object falhou ou foi bloqueado neste fluxo.");
            }

            if (selection.ApplyList)
            {
                WriteOutput($"[Genexus Open API Builder][B070] List nao foi aplicado para Transaction='{transaction.Name}' porque B054 falhou ou foi bloqueado neste fluxo.");
            }

            if (selection.GenerateMetadata)
            {
                WriteOutput($"[Genexus Open API Builder][B060] Metadata nao foi gravada para Transaction='{transaction.Name}' porque B054 falhou ou foi bloqueado neste fluxo.");
            }

            return true;
        }

        var businessComponentReady = true;
        if (selection.ApplyBusinessComponent)
        {
            businessComponentReady = TryApplyBusinessComponent(knowledgeBase.DesignModel, transaction, apiPlan, "Wizard");
        }

        if (!businessComponentReady)
        {
            if (selection.GenerateMetadata)
            {
                WriteOutput($"[Genexus Open API Builder][B060] Metadata nao foi gravada para Transaction='{transaction.Name}' porque B055 falhou ou foi bloqueado neste fluxo.");
            }

            return true;
        }

        var listReady = true;
        if (selection.ApplyList)
        {
            listReady = TryApplyList(knowledgeBase.DesignModel, transaction, apiPlan, "Wizard");
        }

        if (!listReady)
        {
            if (selection.GenerateMetadata)
            {
                WriteOutput($"[Genexus Open API Builder][B060] Metadata nao foi gravada para Transaction='{transaction.Name}' porque B070 falhou ou foi bloqueado neste fluxo.");
            }

            return true;
        }

        if (selection.GenerateMetadata)
        {
            TryWriteMetadataFile(knowledgeBase.DesignModel, transaction, apiPlan, "Wizard");
        }

        return true;
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
        using var dialog = new PrototypeWizardContractDialog(snapshot);
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
            using var dialog = new PrototypeWizardReviewDialog(snapshot);
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
        using var dialog = new PrototypeWizardContractDialog(snapshot);
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
        output.AddLine(outputId, message);
        output.Show(outputId);
    }

}
