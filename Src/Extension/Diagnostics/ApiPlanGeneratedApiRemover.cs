#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;
using Artech.Genexus.Common.Wiki;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// B086 — remoção da API gerada, sob o contrato de intenção durável da F3 (P4).
///
/// A remoção acontece em três tempos que não se misturam:
///
/// 1. <see cref="ResolveIntent"/> lê a metadata, valida todos os alvos e resolve a identidade
///    de cada um. Nada é excluído aqui, e um alvo ambíguo ou não próprio bloqueia antes de
///    qualquer mutação;
/// 2. o chamador registra essa intenção no diário durável — o inventário completo, antes do
///    primeiro <c>Delete()</c>;
/// 3. <see cref="Remove"/> executa a fila por passadas, com orçamento fechado, emitindo um
///    checkpoint ao fim de cada passada.
///
/// A ordem dos dois primeiros tempos é o ponto da frente: sem intenção registrada antes,
/// uma remoção interrompida deixa a KB num estado que ninguém reconstitui. Em 2026-09-06,
/// na `Teste` de `wsEducacaoSpTeste`, o API Object, cinco Procedures e cinco SDTs já tinham
/// sido apagados quando a operação parou, e o relatório final informou «Removidos: nenhum».
/// </summary>
internal static class ApiPlanGeneratedApiRemover
{
    /// <summary>
    /// Resolve o plano e a identidade de cada alvo, validando tudo antes de qualquer exclusão.
    ///
    /// O que sai daqui é a **intenção**: o conjunto completo do que será apagado e do que será
    /// preservado, com a identidade por onde cada alvo é relido. Nome, Description ou prefixo
    /// isolados nunca entram nessa identidade.
    /// </summary>
    internal static ApiPlanGeneratedApiRemovalIntent ResolveIntent(
        KBModel designModel,
        Transaction transaction,
        ApiPlanBusyProgressSession? progress,
        ApiPlanKbObjectNameIndex? kbIndex)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        // B082: instrumentacao de custo. So observa; nao altera ordem nem condicao.
        var telemetry = new ApiPlanScanTelemetry();
        var phaseWatch = Stopwatch.StartNew();

        // Nível A: um índice só para a validação agregada, antes de qualquer exclusão.
        // Localização, revalidação e confirmação pós-Delete permanecem em leitura corrente.
        var index = kbIndex ?? ApiPlanKbObjectNameIndex.Create(designModel, progress);

        var metadataFileName = $"api{transaction.Name}_Metadata";
        var metadataFile = FindOwnedMetadataFile(designModel, metadataFileName, transaction.Name, index, telemetry);
        var metadata = ParseMetadata(metadataFile);
        var plan = ApiPlanGeneratedApiRemovalPlan.FromMetadata(metadata, transaction.Name, transaction.Guid.ToString());
        telemetry.MarkPhase("ResolucaoMetadata", phaseWatch.ElapsedMilliseconds);

        // B082: mede o contenedor real do metadata File. IsFolderEmpty conta Files,
        // entao saber se o File esta dentro do Folder decide se a ordem Folder->File e viavel.
        telemetry.AddNote(string.Format(
            CultureInfo.InvariantCulture,
            "MetadataFile Parent='{0}' ParentGuid='{1}' Module='{2}' FolderPlanejado='{3}' FolderWasCreated={4}",
            metadataFile.Parent is null ? "<null>" : metadataFile.Parent.Name,
            metadataFile.Parent is null ? "<null>" : metadataFile.Parent.Guid.ToString(),
            metadataFile.Module is null ? "<null>" : metadataFile.Module.Name,
            plan.FolderName ?? "<null>",
            plan.FolderWasCreated));

        phaseWatch.Restart();
        ValidateRemovalTargets(designModel, plan, progress: null, index, telemetry);
        telemetry.MarkPhase("ValidacaoAgregada", phaseWatch.ElapsedMilliseconds);

        var targets = BuildTargets(designModel, transaction, plan, metadataFile, index, telemetry);
        var hasApplicationId = ApiPlanMetadataFileWriter.TryReadApplicationId(metadata, out var applicationId);
        var contractHash = metadata.SelectToken("integrity.plannedContract.hash")?.Value<string>();

        return new ApiPlanGeneratedApiRemovalIntent(
            plan,
            metadataFile,
            metadata.SelectToken("schemaVersion")?.Value<string>(),
            targets,
            hasApplicationId ? applicationId : (Guid?)null,
            string.IsNullOrWhiteSpace(contractHash) ? null : contractHash,
            index,
            telemetry);
    }

    /// <summary>
    /// Executa a fila destrutiva a partir de uma intenção já resolvida e registrada.
    /// </summary>
    /// <param name="deletedSink">
    /// Coletor preenchido **durante** a remoção. Sem ele, uma interrupção no meio leva embora a
    /// lista do que já saiu, e o relatório final informa «Removidos: nenhum» com objetos
    /// apagados. Quem chama passa a própria lista e a lê no catch.
    /// </param>
    /// <param name="onPassCompleted">
    /// Checkpoint de fim de passada. Devolver <see langword="false"/> significa que o diário
    /// deixou de ser confirmável: a fila para, porque a seção 4.4 proíbe gravar objeto de
    /// negócio depois de um checkpoint não confirmado.
    /// </param>
    internal static ApiPlanGeneratedApiRemovalResult Remove(
        KBModel designModel,
        ApiPlanGeneratedApiRemovalIntent intent,
        ApiPlanBusyProgressSession? progress,
        List<string>? deletedSink = null,
        Func<int, bool>? onPassCompleted = null)
    {
        if (intent is null)
        {
            throw new ArgumentNullException(nameof(intent));
        }

        return Execute(
            designModel,
            intent.Context,
            intent.Targets,
            intent.Telemetry,
            intent.MaxPasses,
            progress,
            deletedSink,
            onPassCompleted,
            intent.Plan);
    }

    /// <summary>
    /// Executa a fila destrutiva sobre alvos já validados. É por aqui que passam os dois
    /// caminhos: a remoção que nasce da metadata e a **retomada** de uma remoção interrompida,
    /// que nasce do inventário durável do diário. O segundo caminho existe porque o File de
    /// metadata é o penúltimo da fila: depois que ele sai, só o diário sabe o que faltava.
    /// </summary>
    internal static ApiPlanGeneratedApiRemovalResult Execute(
        KBModel designModel,
        ApiPlanRemovalContext context,
        IReadOnlyList<ApiPlanRemovalTarget> targets,
        ApiPlanScanTelemetry telemetry,
        int maxPasses,
        ApiPlanBusyProgressSession? progress,
        List<string>? deletedSink,
        Func<int, bool>? onPassCompleted,
        ApiPlanGeneratedApiRemovalPlan? plan = null)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (targets is null)
        {
            throw new ArgumentNullException(nameof(targets));
        }

        if (telemetry is null)
        {
            throw new ArgumentNullException(nameof(telemetry));
        }
        var deleted = deletedSink ?? new List<string>();
        deleted.Clear();

        var total = targets.Count(target => target.Queued);
        var current = 0;
        var blockDetail = string.Empty;
        var phaseWatch = Stopwatch.StartNew();

        ApiPlanRemovalQueueResult queue;
        try
        {
            queue = ApiPlanRemovalQueue.Run(
                targets,
                target =>
                {
                    progress?.ThrowIfAbortRequested();
                    current++;
                    var label = DescribeKind(target.ObjectType);
                    progress?.Report("Removendo " + label, current, Math.Max(total, current), target.Name);
                    var watch = Stopwatch.StartNew();
                    var result = Attempt(designModel, context, target, deleted, telemetry, out var detail);
                    watch.Stop();
                    progress?.Report("Removendo " + label, current, Math.Max(total, current), target.Name, watch.ElapsedMilliseconds);
                    if (!string.IsNullOrEmpty(detail))
                    {
                        blockDetail = detail;
                    }

                    return result;
                },
                (pass, pending) =>
                {
                    telemetry.AddNote(string.Format(
                        CultureInfo.InvariantCulture,
                        "Fila de remocao passada {0}: pendentes ao fim={1}.",
                        pass,
                        pending.Count));
                    if (onPassCompleted is not null && !onPassCompleted(pass))
                    {
                        throw new ApiPlanRemovalJournalBlockedException(
                            "O checkpoint da passada " + pass.ToString(CultureInfo.InvariantCulture)
                            + " não pôde ser confirmado no diário; a remoção parou para não continuar sem estado durável.");
                    }
                },
                maxPasses);
        }
        catch (ApiPlanRemovalJournalBlockedException exception)
        {
            telemetry.MarkPhase("FilaRemocao", phaseWatch.ElapsedMilliseconds);
            var pending = targets
                .Where(target => target.Queued && target.Confirmation != JournalConfirmation.Absent)
                .ToArray();
            var interrupted = new ApiPlanRemovalQueueResult(
                JournalOperationState.OutcomeUnknown,
                JournalBlockReason.OutcomeUnknown,
                passesExecuted: 0,
                maxPasses: maxPasses,
                deleted: targets.Where(target => target.Confirmation == JournalConfirmation.Absent).ToArray(),
                preserved: Array.Empty<ApiPlanRemovalTarget>(),
                pending: pending,
                blockingTarget: null);
            return new ApiPlanGeneratedApiRemovalResult(
                plan,
                context,
                deleted,
                telemetry.BuildOutputLines(),
                interrupted,
                exception.Message);
        }

        telemetry.MarkPhase("FilaRemocao", phaseWatch.ElapsedMilliseconds);
        telemetry.AddNote(string.Format(
            CultureInfo.InvariantCulture,
            "Fila de remocao encerrada: {0}",
            queue.Describe()));

        return new ApiPlanGeneratedApiRemovalResult(plan, context, deleted, telemetry.BuildOutputLines(), queue, blockDetail);
    }

    public static int CountPlannedDeletes(ApiPlanGeneratedApiRemovalPlan plan)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        var total = 1 + plan.ProcedureNames.Count + plan.OwnSdtNames.Count + 1;
        if (plan.FolderWasCreated && !string.IsNullOrWhiteSpace(plan.FolderName))
        {
            total++;
        }

        return total;
    }

    public static ApiPlanGeneratedApiRemovalPlan Preview(KBModel designModel, Transaction transaction)
    {
        return Preview(designModel, transaction, progress: null, kbIndex: null);
    }

    public static ApiPlanGeneratedApiRemovalPlan Preview(
        KBModel designModel,
        Transaction transaction,
        ApiPlanBusyProgressSession? progress,
        ApiPlanKbObjectNameIndex? kbIndex)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        var metadataFileName = $"api{transaction.Name}_Metadata";
        progress?.Report("Metadata", 0, 0, metadataFileName);
        progress?.PumpAndThrowIfAbortRequested();
        var metadataFile = FindOwnedMetadataFile(designModel, metadataFileName, transaction.Name, kbIndex, telemetry: null);
        var metadata = ParseMetadata(metadataFile);
        var plan = ApiPlanGeneratedApiRemovalPlan.FromMetadata(metadata, transaction.Name, transaction.Guid.ToString());
        ValidateRemovalTargets(designModel, plan, progress, kbIndex);
        return plan;
    }

    /// <summary>
    /// Resolve a identidade de cada alvo e monta a lista completa — o que sai e o que fica.
    ///
    /// Um alvo presente é identificado pelo GUID lido agora. Um alvo previsto que já não está na
    /// KB recebe a identidade histórica composta inteira: nome exato, tipo, papel, Description
    /// canônica e os GUIDs da Transaction e da API. É a forma de declarar o alvo sem inventar um
    /// GUID que não existe — e a tentativa sobre ele termina em <c>TargetAbsentBeforeDelete</c>,
    /// não em sucesso silencioso.
    /// </summary>
    private static IReadOnlyList<ApiPlanRemovalTarget> BuildTargets(
        KBModel designModel,
        Transaction transaction,
        ApiPlanGeneratedApiRemovalPlan plan,
        WikiFileKBObject metadataFile,
        ApiPlanKbObjectNameIndex kbIndex,
        ApiPlanScanTelemetry telemetry)
    {
        Guid.TryParse(plan.ApiGuid, out var apiGuid);
        var targets = new List<ApiPlanRemovalTarget>();

        var api = kbIndex.FindApis(plan.ApiName).FirstOrDefault();
        targets.Add(CreateTarget(
            JournalObjectType.ApiObject,
            plan.ApiName,
            api?.Guid,
            ApiPlanJournalRoles.MainApi,
            "API",
            transaction.Guid,
            apiGuid));

        foreach (var name in plan.ProcedureNames)
        {
            var procedure = kbIndex.FindProcedures(name).FirstOrDefault();
            targets.Add(CreateTarget(
                JournalObjectType.Procedure,
                name,
                procedure?.Guid,
                ApiPlanJournalRoles.RequireForProcedureName(name, "B086"),
                "Procedure",
                transaction.Guid,
                apiGuid));
        }

        foreach (var name in plan.OwnSdtNames)
        {
            var sdt = kbIndex.FindSdts(name).FirstOrDefault();
            targets.Add(CreateTarget(
                JournalObjectType.Sdt,
                name,
                sdt?.Guid,
                ApiPlanJournalRoles.OwnSdt,
                "SDT",
                transaction.Guid,
                apiGuid));
        }

        targets.Add(new ApiPlanRemovalTarget
        {
            ObjectType = JournalObjectType.MetadataFile,
            Name = metadataFile.Name,
            Action = JournalInventoryAction.Delete,
            Queued = true,
            IdentityKind = JournalIdentityKind.Guid,
            Guid = metadataFile.Guid,
            ExpectedHash = ApiPlanMetadataFileWriter.ComputeSha256(metadataFile.BlobPart?.Data?.GetBytes() ?? Array.Empty<byte>()),
            PhysicalState = JournalPhysicalState.Present,
        });

        if (plan.FolderWasCreated && !string.IsNullOrWhiteSpace(plan.FolderName))
        {
            // O Folder entra na fila, mas o inventário só pode declará-lo `Delete` quando a
            // confirmação de vazio existir — e ela só é medida depois de os filhos saírem. Até
            // lá ele é `Preserve`: declarar `emptyConfirmed` antes de medir seria afirmar o que
            // ninguém verificou.
            targets.Add(new ApiPlanRemovalTarget
            {
                ObjectType = JournalObjectType.Folder,
                Name = plan.FolderName!,
                Action = JournalInventoryAction.Preserve,
                Queued = true,
                IdentityKind = JournalIdentityKind.Folder,
                OwnershipValidated = true,
                // `false` não é «vazio desconhecido»: é a marca de que este Folder está na fila
                // e ainda não foi medido. O Folder reutilizado não traz o campo.
                EmptyConfirmed = false,
                PhysicalState = JournalPhysicalState.Present,
            });
        }
        else if (!string.IsNullOrWhiteSpace(plan.FolderName))
        {
            targets.Add(new ApiPlanRemovalTarget
            {
                ObjectType = JournalObjectType.Folder,
                Name = plan.FolderName!,
                Action = JournalInventoryAction.Preserve,
                Queued = false,
                IdentityKind = JournalIdentityKind.Folder,
                OwnershipValidated = true,
                PhysicalState = JournalPhysicalState.Present,
            });
        }

        foreach (var name in plan.SharedSdtNamesPreserved)
        {
            targets.Add(new ApiPlanRemovalTarget
            {
                ObjectType = JournalObjectType.Sdt,
                Name = name,
                Action = JournalInventoryAction.Preserve,
                Queued = false,
                IdentityKind = JournalIdentityKind.None,
                OwnershipValidated = false,
                PhysicalState = JournalPhysicalState.Present,
            });
        }

        // A Transaction nunca entra na fila destrutiva: o Business Component não é revertido.
        targets.Add(new ApiPlanRemovalTarget
        {
            ObjectType = JournalObjectType.Transaction,
            Name = transaction.Name,
            Action = JournalInventoryAction.Preserve,
            Queued = false,
            IdentityKind = JournalIdentityKind.Guid,
            Guid = transaction.Guid,
            PhysicalState = JournalPhysicalState.Present,
        });

        telemetry.AddNote(string.Format(
            CultureInfo.InvariantCulture,
            "Intencao de remocao: Alvos={0}, NaFila={1}, Preservados={2}.",
            targets.Count,
            targets.Count(target => target.Queued),
            targets.Count(target => !target.Queued)));

        return targets;
    }

    private static ApiPlanRemovalTarget CreateTarget(
        JournalObjectType objectType,
        string name,
        Guid? guid,
        string role,
        string objectTypeName,
        Guid transactionGuid,
        Guid apiGuid)
    {
        if (guid.HasValue && guid.Value != Guid.Empty)
        {
            return new ApiPlanRemovalTarget
            {
                ObjectType = objectType,
                Name = name,
                Action = JournalInventoryAction.Delete,
                Queued = true,
                IdentityKind = JournalIdentityKind.Guid,
                Guid = guid,
                PhysicalState = JournalPhysicalState.Present,
            };
        }

        return new ApiPlanRemovalTarget
        {
            ObjectType = objectType,
            Name = name,
            Action = JournalInventoryAction.Delete,
            Queued = true,
            IdentityKind = JournalIdentityKind.Composite,
            Composite = new ApiPlanOperationJournalCompositeIdentity
            {
                ExactName = name,
                ObjectTypeName = objectTypeName,
                Role = role,
                CanonicalDescription = ApiPlanOwnedObjectDescription.Create(name),
                TransactionGuid = transactionGuid,
                ApiGuid = apiGuid,
            },
            PhysicalState = JournalPhysicalState.Absent,
        };
    }

    /// <summary>
    /// Uma tentativa de exclusão. A recusa da IDE **nunca** é interpretada pelo texto: quem
    /// classifica é a releitura do alvo depois da chamada. O que escapa daqui — ambiguidade,
    /// posse perdida, catálogo mudado depois do preflight — é falha de etapa e interrompe.
    /// </summary>
    private static ApiPlanRemovalAttemptResult Attempt(
        KBModel designModel,
        ApiPlanRemovalContext context,
        ApiPlanRemovalTarget target,
        List<string> deleted,
        ApiPlanScanTelemetry telemetry,
        out string blockDetail)
    {
        blockDetail = string.Empty;
        try
        {
            switch (target.ObjectType)
            {
                case JournalObjectType.ApiObject:
                    return DeleteApiObject(designModel, context, target, deleted, telemetry);

                case JournalObjectType.Procedure:
                    return DeleteSingleProcedure(designModel, target, deleted, telemetry);

                case JournalObjectType.Sdt:
                    return DeleteSingleOwnSdt(designModel, context, target, deleted, telemetry);

                case JournalObjectType.MetadataFile:
                    return DeleteMetadataFile(designModel, target, deleted, telemetry);

                case JournalObjectType.Folder:
                    return DeleteOwnFolder(designModel, context, target, deleted, telemetry);

                default:
                    blockDetail = "Tipo fora da fila destrutiva: " + target.ObjectType + ".";
                    return ApiPlanRemovalAttemptResult.StageFailed;
            }
        }
        catch (ApiPlanBusyAbortedException)
        {
            // Abortar é decisão do usuário, não falha adiável: nunca volta para a fila.
            throw;
        }
        catch (Exception exception)
        {
            blockDetail = target.Describe() + ": " + Clean(exception.Message);
            ApiPlanSaveBoundaryProbe.NoteStageFailed("Remove", "removal.stage_failed", blockDetail);
            return ApiPlanRemovalAttemptResult.StageFailed;
        }
    }

    /// <summary>
    /// Valida ambiguidade e posse de API Object, Procedures e SDTs proprios antes de qualquer Delete().
    /// Ausencia de um alvo listado e aceita aqui (a fila a classifica como TargetAbsentBeforeDelete);
    /// ambiguidade ou objeto nao proprio bloqueiam.
    /// </summary>
    internal static void ValidateRemovalTargets(KBModel designModel, ApiPlanGeneratedApiRemovalPlan plan)
    {
        // kbIndex nulo e contrato deliberado: leitura corrente. O wrapper de 2 args
        // existe para testes textuais; o Remove efetivo passa o indice da validacao agregada.
        ValidateRemovalTargets(designModel, plan, progress: null, kbIndex: null);
    }

    internal static void ValidateRemovalTargets(
        KBModel designModel,
        ApiPlanGeneratedApiRemovalPlan plan,
        ApiPlanBusyProgressSession? progress,
        ApiPlanKbObjectNameIndex? kbIndex,
        ApiPlanScanTelemetry? telemetry = null)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        var total = 1 + plan.ProcedureNames.Count + plan.OwnSdtNames.Count;
        var current = 0;
        progress?.ThrowIfAbortRequested();
        current++;
        progress?.Report("Validando", current, total, plan.ApiName);
        progress?.Pump();
        ValidateApiObjectTarget(
            designModel,
            plan.ApiName,
            Guid.TryParse(plan.ApiGuid, out var plannedApiGuid) ? plannedApiGuid : (Guid?)null,
            beforeAnyDelete: true,
            kbIndex,
            telemetry);
        foreach (var name in plan.ProcedureNames)
        {
            progress?.ThrowIfAbortRequested();
            current++;
            progress?.Report("Validando", current, total, name);
            progress?.Pump();
            ValidateProcedureTarget(designModel, name, beforeAnyDelete: true, kbIndex, telemetry);
        }

        foreach (var name in plan.OwnSdtNames)
        {
            progress?.ThrowIfAbortRequested();
            current++;
            progress?.Report("Validando", current, total, name);
            progress?.Pump();
            ValidateOwnSdtTarget(designModel, plan.SharedSdtNamesPreserved, name, beforeAnyDelete: true, kbIndex, telemetry);
        }
    }

    private static WikiFileKBObject FindOwnedMetadataFile(
        KBModel designModel,
        string metadataFileName,
        string transactionName,
        ApiPlanKbObjectNameIndex? kbIndex,
        ApiPlanScanTelemetry? telemetry)
    {
        var matches = kbIndex is null
            ? Scan(telemetry, "File", "resolucao-metadata", () => WikiFileKBObject.GetAll(designModel)
                .Where(file => string.Equals(file.Name, metadataFileName, StringComparison.OrdinalIgnoreCase))
                .ToArray())
            : kbIndex.FindFiles(metadataFileName).ToArray();

        if (matches.Length == 0)
        {
            throw new InvalidOperationException($"Remocao bloqueada: File de metadata '{metadataFileName}' nao foi encontrado. Nenhuma alteracao foi feita.");
        }

        if (matches.Length > 1)
        {
            throw new InvalidOperationException($"Remocao bloqueada: foram encontrados {matches.Length} Files chamados '{metadataFileName}'. Nenhuma alteracao foi feita.");
        }

        var file = matches[0];
        if (ApiPlanOwnedObjectDescription.IsOwnedMetadataFile(file.Description, metadataFileName, transactionName))
        {
            return file;
        }

        throw new InvalidOperationException($"Remocao bloqueada: File '{metadataFileName}' nao e metadata propria da extensao. Nenhuma alteracao foi feita.");
    }

    private static JObject ParseMetadata(WikiFileKBObject file)
    {
        var bytes = file.BlobPart?.Data?.GetBytes();
        if (bytes is null || bytes.Length == 0)
        {
            throw new InvalidOperationException($"Remocao bloqueada: File '{file.Name}' nao possui JSON persistido. Nenhuma alteracao foi feita.");
        }

        try
        {
            return ApiPlanMetadataIntegrity.ParseMetadataBytes(bytes);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Remocao bloqueada: File '{file.Name}' possui JSON invalido. Nenhuma alteracao foi feita.", ex);
        }
    }

    /// <summary>
    /// B082: executa a varredura sob medicao quando ha instrumentacao ativa.
    /// O delegate precisa conter o pipeline inteiro, ate a materializacao, porque
    /// <c>GetAll</c> e preguicoso e o custo esta na enumeracao.
    /// </summary>
    private static T Scan<T>(
        ApiPlanScanTelemetry? telemetry,
        string objectType,
        string phase,
        Func<T> scan)
    {
        // Sem telemetria propria (caminho do Preview), cai no probe de escopo ambiente,
        // que o handler abre para medir a fase de Preview separadamente da exclusao.
        return telemetry is null
            ? ApiPlanScanProbe.Scan(objectType, phase, scan)
            : telemetry.MeasureScan(objectType, phase, scan);
    }

    // kbIndex nulo e contrato deliberado de leitura corrente: localizacao e revalidacao
    // apos o catalogo ter comecado a mudar (Nível B / confirmacao pos-Delete). A validacao
    // agregada, antes de qualquer exclusao, passa o indice criado na resolucao da intencao.
    private static void ValidateApiObjectTarget(
        KBModel designModel,
        string apiName,
        Guid? expectedGuid,
        bool beforeAnyDelete,
        ApiPlanKbObjectNameIndex? kbIndex = null,
        ApiPlanScanTelemetry? telemetry = null)
    {
        var matches = kbIndex is null
            ? Scan(telemetry, "API", beforeAnyDelete ? "validacao-agregada" : "revalidacao-pre-delete", () => API.GetAll(designModel)
                .Where(item => string.Equals(item.Name, apiName, StringComparison.OrdinalIgnoreCase))
                .ToArray())
            : kbIndex.FindApis(apiName).ToArray();
        if (matches.Length == 0)
        {
            return;
        }

        if (matches.Length > 1)
        {
            throw new InvalidOperationException(BuildBlockedMessage(
                $"API Object ambiguo '{apiName}'",
                beforeAnyDelete));
        }

        var api = matches[0];
        if (!expectedGuid.HasValue || expectedGuid.Value == Guid.Empty || api.Guid != expectedGuid.Value)
        {
            throw new InvalidOperationException(BuildBlockedMessage(
                $"API Object '{apiName}' nao corresponde ao Guid registrado",
                beforeAnyDelete));
        }
    }

    private static void ValidateProcedureTarget(
        KBModel designModel,
        string name,
        bool beforeAnyDelete,
        ApiPlanKbObjectNameIndex? kbIndex = null,
        ApiPlanScanTelemetry? telemetry = null)
    {
        var matches = kbIndex is null
            ? Scan(telemetry, "Procedure", beforeAnyDelete ? "validacao-agregada" : "revalidacao-pre-delete", () => Procedure.GetAll(designModel)
                .Where(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                .ToArray())
            : kbIndex.FindProcedures(name).ToArray();
        if (matches.Length == 0)
        {
            return;
        }

        if (matches.Length > 1)
        {
            throw new InvalidOperationException(BuildBlockedMessage(
                $"Procedure ambigua '{name}'",
                beforeAnyDelete));
        }

        var procedure = matches[0];
        if (!ApiPlanOwnedObjectDescription.IsOwnedProcedure(procedure.Description, name))
        {
            throw new InvalidOperationException(BuildBlockedMessage(
                $"Procedure '{name}' nao e propria da extensao",
                beforeAnyDelete));
        }
    }

    private static void ValidateOwnSdtTarget(
        KBModel designModel,
        IReadOnlyList<string> preservedSharedSdtNames,
        string name,
        bool beforeAnyDelete,
        ApiPlanKbObjectNameIndex? kbIndex = null,
        ApiPlanScanTelemetry? telemetry = null)
    {
        if (preservedSharedSdtNames.Contains(name, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(BuildBlockedMessage(
                $"tentativa de apagar SDT compartilhado '{name}'",
                beforeAnyDelete));
        }

        var matches = kbIndex is null
            ? Scan(telemetry, "SDT", beforeAnyDelete ? "validacao-agregada" : "revalidacao-pre-delete", () => SDT.GetAll(designModel)
                .Where(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                .ToArray())
            : kbIndex.FindSdts(name).ToArray();
        if (matches.Length == 0)
        {
            return;
        }

        if (matches.Length > 1)
        {
            throw new InvalidOperationException(BuildBlockedMessage(
                $"SDT ambiguo '{name}'",
                beforeAnyDelete));
        }

        var sdt = matches[0];
        if (!ApiPlanOwnedObjectDescription.IsOwnedSdt(sdt.Description, name))
        {
            throw new InvalidOperationException(BuildBlockedMessage(
                $"SDT '{name}' nao e proprio da extensao",
                beforeAnyDelete));
        }
    }

    private static string BuildBlockedMessage(string reason, bool beforeAnyDelete)
    {
        if (beforeAnyDelete)
        {
            return $"Remocao bloqueada: {reason}. Nenhuma alteracao foi feita.";
        }

        return $"Remocao bloqueada: {reason}. O estado da KB mudou apos o preflight; interrompendo para evitar mais exclusoes.";
    }

    private static ApiPlanRemovalAttemptResult DeleteSingleProcedure(
        KBModel designModel,
        ApiPlanRemovalTarget target,
        List<string> deleted,
        ApiPlanScanTelemetry? telemetry)
    {
        var name = target.Name;
        ValidateProcedureTarget(designModel, name, beforeAnyDelete: false, kbIndex: null, telemetry);

        var matches = Scan(telemetry, "Procedure", "localizacao-delete", () => Procedure.GetAll(designModel)
            .Where(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
            .ToArray());
        if (matches.Length == 0)
        {
            return NotAttempted(
                target,
                "Procedure",
                "Procedures",
                ApiPlanJournalRoles.RequireForProcedureName(name, "B086"),
                "Procedure ausente antes do Delete.");
        }

        var procedure = matches[0];
        var guid = procedure.Guid;
        Func<PersistenceConfirmation> confirm = () => ConfirmDelete(
            () => Scan(telemetry, "Procedure", "confirmacao-pos-delete", () => Procedure.GetAll(designModel).Any(item => item.Guid == guid)),
            guid.ToString());
        return Execute(
            () => ApiPlanSaveBoundaryProbe.Persist(
                PersistenceFaultPoint.ProcedureDelete,
                "Delete",
                "Procedure",
                "Procedures",
                name,
                new GuidIdentity(guid),
                procedure.Delete,
                confirm),
            confirm,
            target,
            deleted,
            $"Procedure:{name}");
    }

    private static ApiPlanRemovalAttemptResult DeleteApiObject(
        KBModel designModel,
        ApiPlanRemovalContext context,
        ApiPlanRemovalTarget target,
        List<string> deleted,
        ApiPlanScanTelemetry? telemetry)
    {
        ValidateApiObjectTarget(designModel, context.ApiName, context.ApiGuid, beforeAnyDelete: false, kbIndex: null, telemetry);

        var matches = Scan(telemetry, "API", "localizacao-delete", () => API.GetAll(designModel)
            .Where(item => string.Equals(item.Name, context.ApiName, StringComparison.OrdinalIgnoreCase))
            .ToArray());
        if (matches.Length == 0)
        {
            return NotAttempted(
                target,
                "API",
                "ApiObject",
                ApiPlanJournalRoles.MainApi,
                "API Object ausente antes do Delete.");
        }

        var api = matches[0];
        var guid = api.Guid;
        Func<PersistenceConfirmation> confirm = () => ConfirmDelete(
            () => Scan(telemetry, "API", "confirmacao-pos-delete", () => API.GetAll(designModel).Any(item => item.Guid == guid)),
            guid.ToString());
        return Execute(
            () => ApiPlanSaveBoundaryProbe.Persist(
                PersistenceFaultPoint.ApiDelete,
                "Delete",
                "API",
                "ApiObject",
                context.ApiName,
                new GuidIdentity(guid),
                api.Delete,
                confirm),
            confirm,
            target,
            deleted,
            $"API:{context.ApiName}");
    }

    private static ApiPlanRemovalAttemptResult DeleteSingleOwnSdt(
        KBModel designModel,
        ApiPlanRemovalContext context,
        ApiPlanRemovalTarget target,
        List<string> deleted,
        ApiPlanScanTelemetry? telemetry)
    {
        var name = target.Name;
        ValidateOwnSdtTarget(designModel, context.PreservedSharedSdtNames, name, beforeAnyDelete: false, kbIndex: null, telemetry);

        var matches = Scan(telemetry, "SDT", "localizacao-delete", () => SDT.GetAll(designModel)
            .Where(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
            .ToArray());
        if (matches.Length == 0)
        {
            return NotAttempted(
                target,
                "SDT",
                "OwnSdts",
                ApiPlanJournalRoles.OwnSdt,
                "SDT ausente antes do Delete.");
        }

        var sdt = matches[0];
        var guid = sdt.Guid;
        Func<PersistenceConfirmation> confirm = () => ConfirmDelete(
            () => Scan(telemetry, "SDT", "confirmacao-pos-delete", () => SDT.GetAll(designModel).Any(item => item.Guid == guid)),
            guid.ToString());
        return Execute(
            () => ApiPlanSaveBoundaryProbe.Persist(
                PersistenceFaultPoint.SdtDelete,
                "Delete",
                "SDT",
                "OwnSdts",
                name,
                new GuidIdentity(guid),
                sdt.Delete,
                confirm),
            confirm,
            target,
            deleted,
            $"SDT:{name}");
    }

    private static ApiPlanRemovalAttemptResult DeleteMetadataFile(
        KBModel designModel,
        ApiPlanRemovalTarget target,
        List<string> deleted,
        ApiPlanScanTelemetry? telemetry)
    {
        var name = target.Name;
        var matches = Scan(telemetry, "File", "localizacao-delete", () => WikiFileKBObject.GetAll(designModel)
            .Where(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
            .ToArray());
        if (matches.Length == 0)
        {
            return NotAttempted(
                target,
                "File",
                "Metadata",
                ApiPlanJournalRoles.Metadata,
                "File de metadata ausente antes do Delete.");
        }

        var metadataFile = matches[0];
        var guid = metadataFile.Guid;
        var expectedBytes = metadataFile.BlobPart?.Data?.GetBytes() ?? Array.Empty<byte>();
        Func<PersistenceConfirmation> confirm = () => ConfirmDelete(
            () => Scan(telemetry, "File", "confirmacao-pos-delete", () => WikiFileKBObject.GetAll(designModel).Any(item => item.Guid == guid)),
            guid.ToString());
        return Execute(
            () => ApiPlanSaveBoundaryProbe.Persist(
                PersistenceFaultPoint.MetadataDelete,
                "Delete",
                "File",
                "Metadata",
                name,
                new FileIdentity(guid, name, ApiPlanMetadataFileWriter.ComputeSha256(expectedBytes)),
                metadataFile.Delete,
                confirm),
            confirm,
            target,
            deleted,
            $"File:{name}");
    }

    /// <summary>
    /// O Folder próprio é o último da fila, e é o único alvo que pode sair dela **preservado**:
    /// quem o reutilizou, quem deixou conteúdo dentro ou quem trocou a Description manda mais
    /// que o plano. Preservar não impede a operação de terminar em <c>Removed</c>.
    /// </summary>
    private static ApiPlanRemovalAttemptResult DeleteOwnFolder(
        KBModel designModel,
        ApiPlanRemovalContext context,
        ApiPlanRemovalTarget target,
        List<string> deleted,
        ApiPlanScanTelemetry? telemetry)
    {
        var matches = Scan(telemetry, "Folder", "localizacao-delete", () => Folder.GetAll(designModel)
            .Where(item => string.Equals(item.Name, target.Name, StringComparison.OrdinalIgnoreCase))
            .ToArray());
        if (matches.Length != 1)
        {
            return ApiPlanRemovalAttemptResult.Preserved;
        }

        var folder = matches[0];
        var expectedDescription = ApiPlanOwnedObjectDescription.CreateTransactionFolderDescription(target.Name);
        var legacyDescription = ApiPlanOwnedObjectDescription.CreateLegacyTransactionFolderDescription(context.TransactionName);
        if (!string.Equals(folder.Description, expectedDescription, StringComparison.Ordinal)
            && !string.Equals(folder.Description, legacyDescription, StringComparison.Ordinal))
        {
            return ApiPlanRemovalAttemptResult.Preserved;
        }

        if (!IsFolderEmpty(designModel, folder, telemetry))
        {
            deleted.Add($"Folder:{target.Name}:PreservedNonEmpty");
            return ApiPlanRemovalAttemptResult.Preserved;
        }

        var guid = folder.Guid;
        Func<PersistenceConfirmation> confirm = () => ConfirmDelete(
            () => Scan(telemetry, "Folder", "confirmacao-pos-delete", () => Folder.GetAll(designModel).Any(item => item.Guid == guid)),
            guid.ToString());
        return Execute(
            () => ApiPlanSaveBoundaryProbe.Persist(
                PersistenceFaultPoint.FolderDelete,
                "Delete",
                "Folder",
                "TransactionFolder",
                target.Name,
                new FolderIdentity(target.Name, owned: true, emptyConfirmed: true),
                folder.Delete,
                confirm),
            confirm,
            target,
            deleted,
            $"Folder:{target.Name}");
    }

    /// <summary>
    /// Registra um alvo previsto que já não estava lá. Não é sucesso implícito: o recibo sai com
    /// <c>NotAttempted</c>, sem chamar <c>Delete()</c>, e a fila encerra a operação em
    /// <c>Partial</c>. A identidade histórica composta vai inteira, porque é ela que o inventário
    /// do envelope exige para declarar o alvo. <paramref name="stage"/> alimenta o recibo
    /// (<c>receipts[].stage</c> / relatório <c>[stage/objectType]</c>) e deve coincidir com o
    /// <c>Persist</c> irmão do mesmo tipo — <c>Procedures</c>, <c>ApiObject</c>, <c>OwnSdts</c>,
    /// <c>Metadata</c>. <paramref name="role"/> alimenta só o fallback de
    /// <see cref="CompositeIdentity"/> quando o alvo não carrega composto; nunca substitui o stage.
    /// </summary>
    private static ApiPlanRemovalAttemptResult NotAttempted(
        ApiPlanRemovalTarget target,
        string objectType,
        string stage,
        string role,
        string detail)
    {
        var composite = target.Composite;
        ApiPlanSaveBoundaryProbe.RecordNotAttempted(
            "Delete",
            objectType,
            stage,
            target.Name,
            composite is null
                ? new CompositeIdentity(target.Name, objectType, role, string.Empty, Guid.Empty, Guid.Empty)
                : new CompositeIdentity(
                    composite.ExactName,
                    composite.ObjectTypeName,
                    composite.Role,
                    composite.CanonicalDescription,
                    composite.TransactionGuid,
                    composite.ApiGuid),
            PersistenceConfirmation.NotAttempted(PersistencePhysicalState.Absent, detail));
        return ApiPlanRemovalAttemptResult.AbsentBeforeDelete;
    }

    /// <summary>
    /// Executa a exclusão física e classifica o resultado pela **evidência**: o recibo da F2,
    /// quando existe, e a releitura do alvo quando o <c>Delete()</c> lançou. Um alvo que
    /// continua na KB volta para a fila; um resultado ilegível bloqueia a operação.
    /// </summary>
    private static ApiPlanRemovalAttemptResult Execute(
        Func<PersistenceReceipt?> persist,
        Func<PersistenceConfirmation> confirm,
        ApiPlanRemovalTarget target,
        List<string> deleted,
        string deletedLabel)
    {
        PersistenceReceipt? receipt;
        try
        {
            receipt = persist();
        }
        catch (ApiPlanBusyAbortedException)
        {
            throw;
        }
        catch (Exception)
        {
            // O `Delete()` lançou. Isso não diz se o objeto saiu: só a releitura diz, e é ela
            // que classifica. O seam já completou o recibo com a mesma regra antes de relançar.
            return Classify(SafeConfirm(confirm), deleted, deletedLabel);
        }

        if (receipt is null)
        {
            // Sem log de persistência ativo, o seam não confirma por conta própria.
            return Classify(SafeConfirm(confirm), deleted, deletedLabel);
        }

        switch (receipt.Outcome)
        {
            case PersistenceOutcome.Confirmed:
                deleted.Add(deletedLabel);
                return ApiPlanRemovalAttemptResult.Confirmed;

            case PersistenceOutcome.Failed:
                return ApiPlanRemovalAttemptResult.StillPresent;

            default:
                return ApiPlanRemovalAttemptResult.OutcomeUnknown;
        }
    }

    private static ApiPlanRemovalAttemptResult Classify(
        PersistenceConfirmation observation,
        List<string> deleted,
        string deletedLabel)
    {
        if (observation.Status == PersistenceConfirmationStatus.Absent)
        {
            deleted.Add(deletedLabel);
            return ApiPlanRemovalAttemptResult.Confirmed;
        }

        if (observation.Status == PersistenceConfirmationStatus.Confirmed
            && observation.PhysicalState == PersistencePhysicalState.Present)
        {
            return ApiPlanRemovalAttemptResult.StillPresent;
        }

        return ApiPlanRemovalAttemptResult.OutcomeUnknown;
    }

    private static PersistenceConfirmation SafeConfirm(Func<PersistenceConfirmation> confirm)
    {
        try
        {
            return confirm();
        }
        catch (Exception exception)
        {
            return PersistenceConfirmation.Unreadable(exception.GetType().FullName + ": " + Clean(exception.Message));
        }
    }

    private static PersistenceConfirmation ConfirmDelete(Func<bool> stillExists, string observedIdentity)
    {
        try
        {
            return stillExists()
                ? PersistenceConfirmation.Confirmed(observedIdentity, "O alvo ainda existe após Delete.")
                : PersistenceConfirmation.Absent("O alvo não foi reencontrado após Delete.");
        }
        catch (Exception exception)
        {
            return PersistenceConfirmation.Unreadable(exception.GetType().FullName + ": " + exception.Message);
        }
    }

    // O curto-circuito de && e preservado: a instrumentacao envolve cada operando
    // isoladamente, entao uma varredura so e medida quando de fato executa.
    private static bool IsFolderEmpty(KBModel designModel, Folder folder, ApiPlanScanTelemetry? telemetry = null)
    {
        return !Scan(telemetry, "API", "folder-vazio", () => API.GetAll(designModel).Any(item => item.Parent is not null && item.Parent.Guid == folder.Guid))
            && !Scan(telemetry, "Procedure", "folder-vazio", () => Procedure.GetAll(designModel).Any(item => item.Parent is not null && item.Parent.Guid == folder.Guid))
            && !Scan(telemetry, "SDT", "folder-vazio", () => SDT.GetAll(designModel).Any(item => item.Parent is not null && item.Parent.Guid == folder.Guid))
            && !Scan(telemetry, "File", "folder-vazio", () => WikiFileKBObject.GetAll(designModel).Any(item => item.Parent is not null && item.Parent.Guid == folder.Guid))
            && !Scan(telemetry, "Folder", "folder-vazio", () => Folder.GetAll(designModel).Any(item => item.Guid != folder.Guid && item.Parent is not null && item.Parent.Guid == folder.Guid));
    }

    private static string DescribeKind(JournalObjectType objectType) => objectType switch
    {
        JournalObjectType.ApiObject => "API Object",
        JournalObjectType.Procedure => "Procedure",
        JournalObjectType.Sdt => "SDT",
        JournalObjectType.MetadataFile => "File",
        JournalObjectType.Folder => "Folder",
        _ => objectType.ToString(),
    };

    private static string Clean(string value) => (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
}

/// <summary>
/// A intenção de remoção resolvida: plano, inventário completo e a identidade com que a
/// operação será registrada no diário.
/// </summary>
internal sealed class ApiPlanGeneratedApiRemovalIntent
{
    internal ApiPlanGeneratedApiRemovalIntent(
        ApiPlanGeneratedApiRemovalPlan plan,
        WikiFileKBObject metadataFile,
        string? metadataSchemaVersion,
        IReadOnlyList<ApiPlanRemovalTarget> targets,
        Guid? applicationId,
        string? contractHash,
        ApiPlanKbObjectNameIndex kbIndex,
        ApiPlanScanTelemetry telemetry)
    {
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        MetadataFile = metadataFile ?? throw new ArgumentNullException(nameof(metadataFile));
        MetadataSchemaVersion = metadataSchemaVersion;
        Targets = targets ?? throw new ArgumentNullException(nameof(targets));
        ApplicationId = applicationId;
        ContractHash = contractHash;
        KbIndex = kbIndex;
        Telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
    }

    internal ApiPlanGeneratedApiRemovalPlan Plan { get; }

    internal WikiFileKBObject MetadataFile { get; }

    /// <summary>Versão da metadata de negócio associada, para o envelope.</summary>
    internal string? MetadataSchemaVersion { get; }

    internal IReadOnlyList<ApiPlanRemovalTarget> Targets { get; }

    /// <summary>
    /// <c>ownership.applicationId</c> da metadata V3. Nulo em metadata legada: nesse caso o
    /// diário registra uma adoção tardia, com identificador novo, e **não** regrava o File.
    /// </summary>
    internal Guid? ApplicationId { get; }

    /// <summary>Hash do contrato planejado gravado pela integridade B067, quando existir.</summary>
    internal string? ContractHash { get; }

    internal ApiPlanKbObjectNameIndex KbIndex { get; }

    internal ApiPlanScanTelemetry Telemetry { get; }

    /// <summary>Os poucos valores de que a fila precisa para validar posse antes de excluir.</summary>
    internal ApiPlanRemovalContext Context => ApiPlanRemovalContext.FromPlan(Plan);

    internal int QueuedCount => Targets.Count(target => target.Queued);

    internal int MaxPasses => ApiPlanRemovalIntent.ResolveMaxPasses(Targets);

    /// <summary>
    /// Suficiência do inventário que sustentou a intenção. Só existe intenção quando a
    /// avaliação aprovou os alvos, então o valor carregado aqui é sempre suficiente — o
    /// negativo bloqueia antes, na recusa P8 §12, e nunca chega ao diário.
    /// </summary>
    internal JournalInventorySufficiency Sufficiency => JournalInventorySufficiency.InventorySufficient;

    /// <summary>
    /// Intenção importada: a metadata não trazia identidade de aplicação própria, então o
    /// contrato do envelope aceita <c>contractHash</c> ausente e o identificador é adotado agora.
    /// </summary>
    internal JournalIntentKind IntentKind =>
        ApplicationId.HasValue ? JournalIntentKind.Current : JournalIntentKind.Imported;

    internal IReadOnlyList<ApiPlanOperationJournalInventoryItem> BuildInventory() =>
        ApiPlanRemovalIntent.BuildInventory(Targets);
}

/// <summary>
/// O mínimo que a fila precisa saber para validar posse antes de cada exclusão: qual API, com
/// qual identidade, de qual Transaction, e quais SDTs compartilhados **nunca** podem ser
/// tocados.
///
/// Ele existe para que a retomada de uma remoção interrompida não dependa do File de metadata:
/// esse File é o penúltimo da fila, e depois que ele sai só o diário sabe o que faltava.
/// </summary>
internal sealed class ApiPlanRemovalContext
{
    private ApiPlanRemovalContext(
        string apiName,
        Guid? apiGuid,
        string transactionName,
        IReadOnlyList<string> preservedSharedSdtNames)
    {
        ApiName = apiName ?? string.Empty;
        ApiGuid = apiGuid;
        TransactionName = transactionName ?? string.Empty;
        PreservedSharedSdtNames = preservedSharedSdtNames ?? Array.Empty<string>();
    }

    internal string ApiName { get; }

    internal Guid? ApiGuid { get; }

    internal string TransactionName { get; }

    internal IReadOnlyList<string> PreservedSharedSdtNames { get; }

    internal static ApiPlanRemovalContext FromPlan(ApiPlanGeneratedApiRemovalPlan plan)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        return new ApiPlanRemovalContext(
            plan.ApiName,
            Guid.TryParse(plan.ApiGuid, out var apiGuid) ? apiGuid : (Guid?)null,
            plan.TransactionName,
            plan.SharedSdtNamesPreserved);
    }

    /// <summary>
    /// Reconstrói o contexto a partir do envelope durável. A lista de preservados sai dos itens
    /// <c>Preserve</c> do inventário: eles foram registrados antes da primeira exclusão
    /// justamente para que uma retomada soubesse no que não encostar.
    /// </summary>
    internal static ApiPlanRemovalContext FromJournal(ApiPlanOperationJournal journal)
    {
        if (journal is null)
        {
            throw new ArgumentNullException(nameof(journal));
        }

        var api = journal.Inventory.FirstOrDefault(item => item.ObjectType == JournalObjectType.ApiObject);
        var preserved = journal.Inventory
            .Where(item => item.ObjectType == JournalObjectType.Sdt && item.Action == JournalInventoryAction.Preserve)
            .Select(item => item.Name)
            .ToArray();

        return new ApiPlanRemovalContext(
            api?.Name ?? string.Empty,
            api?.Guid ?? journal.Plan?.PlannedApiGuid,
            journal.TransactionName,
            preserved);
    }
}

/// <summary>
/// Sinaliza que um checkpoint do diário deixou de ser confirmável no meio da remoção. Não é
/// falha da KB: é a proibição de continuar sem estado durável.
/// </summary>
internal sealed class ApiPlanRemovalJournalBlockedException : Exception
{
    internal ApiPlanRemovalJournalBlockedException(string message)
        : base(message)
    {
    }
}

internal sealed class ApiPlanGeneratedApiRemovalResult
{
    public ApiPlanGeneratedApiRemovalResult(
        ApiPlanGeneratedApiRemovalPlan? plan,
        ApiPlanRemovalContext context,
        IReadOnlyList<string> deletedItems,
        IReadOnlyList<string>? telemetryLines = null,
        ApiPlanRemovalQueueResult? queue = null,
        string blockDetail = "")
    {
        Plan = plan;
        Context = context ?? throw new ArgumentNullException(nameof(context));
        DeletedItems = deletedItems ?? throw new ArgumentNullException(nameof(deletedItems));
        TelemetryLines = telemetryLines ?? Array.Empty<string>();
        Queue = queue;
        BlockDetail = blockDetail ?? string.Empty;
    }

    /// <summary>
    /// Plano da metadata. Nulo na retomada de uma remoção interrompida: ali o inventário vem do
    /// diário, justamente porque o File de metadata pode já ter saído da KB.
    /// </summary>
    public ApiPlanGeneratedApiRemovalPlan? Plan { get; }

    public ApiPlanRemovalContext Context { get; }

    /// <summary>Nome da API para relatório e Output, venha ele do plano ou do diário.</summary>
    public string ApiName => Plan?.ApiName ?? Context.ApiName;

    public IReadOnlyList<string> DeletedItems { get; }

    /// <summary>B082: linhas de medição de custo, para a janela Output. Diagnóstico apenas.</summary>
    public IReadOnlyList<string> TelemetryLines { get; }

    /// <summary>Estado terminal da fila: o que decide <c>Removed</c>, <c>Partial</c> ou bloqueio.</summary>
    public ApiPlanRemovalQueueResult? Queue { get; }

    /// <summary>Detalhe humano da última falha observada, para o relatório final.</summary>
    public string BlockDetail { get; }

    public bool IsComplete => Queue is null || Queue.IsComplete;
}
