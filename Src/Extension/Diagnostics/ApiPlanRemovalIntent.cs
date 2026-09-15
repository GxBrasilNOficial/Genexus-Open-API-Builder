#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// S-B111 / F3 P4 — a intenção de remoção, antes do primeiro <c>Delete()</c>.
///
/// A seção 4.3 do plano da F3 exige registrar o conjunto **completo** de alvos validados antes
/// de excluir qualquer um deles, e preservar esse registro numa interrupção: é ele que permite
/// dizer, depois, o que foi previsto e o que chegou a sair da KB. Sem intenção durável, uma
/// remoção interrompida deixa a KB num estado que ninguém consegue reconstituir — foi o que
/// aconteceu em 2026-09-06, com o API Object, cinco Procedures e cinco SDTs já apagados e o
/// relatório final informando «Removidos: nenhum».
///
/// Este arquivo é SDK-free de propósito: o que depende da IDE é ler as identidades, e isso fica
/// no remover. Aqui entram valores, para que ordem, inventário e orçamento sejam exercitados
/// offline.
/// </summary>
public static class ApiPlanRemovalIntent
{
    /// <summary>
    /// Ordem obrigatória da fila destrutiva (seção 4.3): API Object, Procedures, SDTs na ordem
    /// de dependência recebida, File de metadata e, por último, o Folder próprio vazio.
    ///
    /// A ordenação é **estável** dentro de cada tipo: a lista de SDTs já vem na ordem em que a
    /// metadata a gravou, e reordená-la aqui desfaria a dependência que ela preserva.
    /// </summary>
    public static IReadOnlyList<ApiPlanRemovalTarget> Order(IEnumerable<ApiPlanRemovalTarget> targets)
    {
        if (targets is null)
        {
            throw new ArgumentNullException(nameof(targets));
        }

        return targets
            .Where(target => target is not null)
            .OrderBy(target => RemovalRank(target.ObjectType))
            .ToArray();
    }

    /// <summary>Os alvos que a fila destrutiva vai tentar, na ordem canônica.</summary>
    public static IReadOnlyList<ApiPlanRemovalTarget> OrderQueued(IEnumerable<ApiPlanRemovalTarget> targets) =>
        Order(targets).Where(target => target.Queued).ToArray();

    /// <summary>
    /// Orçamento de tentativas: <c>max(1, número de itens da fila destrutiva)</c>. Uma passada
    /// só pode reencaminhar um alvo comprovadamente presente depois do <c>Delete()</c>, então N
    /// itens não podem exigir mais de N passadas para sair na ordem de dependência.
    /// </summary>
    public static int ResolveMaxPasses(IEnumerable<ApiPlanRemovalTarget> targets)
    {
        if (targets is null)
        {
            throw new ArgumentNullException(nameof(targets));
        }

        return Math.Max(1, targets.Count(target => target is not null && target.Queued));
    }

    /// <summary>
    /// Monta o inventário do envelope a partir dos alvos validados. Cada alvo aparece uma vez, e
    /// o que não entra na fila destrutiva — SDTs compartilhados, Folder reutilizado, a própria
    /// Transaction — aparece como <c>Preserve</c>, nunca omitido: um inventário que esconde o
    /// preservado não distingue «não era para apagar» de «esqueci de listar».
    /// </summary>
    public static IReadOnlyList<ApiPlanOperationJournalInventoryItem> BuildInventory(
        IEnumerable<ApiPlanRemovalTarget> targets)
    {
        var ordered = Order(targets);
        var items = new List<ApiPlanOperationJournalInventoryItem>(ordered.Count);
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var target in ordered)
        {
            var item = target.ToInventoryItem();
            var key = ApiPlanOperationJournalValidator.BuildIdentityKey(item);
            if (!keys.Add(key))
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    "O inventário de remoção repete o mesmo alvo: '{0}' ({1}).",
                    target.Name,
                    target.ObjectType));
            }

            items.Add(item);
        }

        return items;
    }

    /// <summary>
    /// Reconstrói a fila a partir do inventário durável do diário — a retomada de uma remoção
    /// interrompida.
    ///
    /// Volta para a fila apenas o que a intenção declarou <c>Delete</c> e a KB ainda mostra, mais
    /// o Folder próprio que estava enfileirado e nunca chegou a ser medido. Tudo o mais é
    /// <c>Preserve</c>: um item que a intenção não declarou como alvo não vira alvo numa
    /// retomada, por mais parecido que o nome seja.
    /// </summary>
    public static IReadOnlyList<ApiPlanRemovalTarget> FromInventory(
        IEnumerable<ApiPlanOperationJournalInventoryItem> inventory,
        IReadOnlyDictionary<string, JournalPhysicalState>? observed = null)
    {
        if (inventory is null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        var targets = new List<ApiPlanRemovalTarget>();
        foreach (var item in inventory)
        {
            var target = new ApiPlanRemovalTarget
            {
                ObjectType = item.ObjectType,
                Name = item.Name,
                Action = item.Action,
                IdentityKind = item.IdentityKind,
                Guid = item.Guid,
                FileId = item.FileId,
                ExpectedHash = item.ExpectedHash,
                Composite = item.Composite,
                EmptyConfirmed = item.EmptyConfirmed,
                OwnershipValidated = item.OwnershipValidated,
                PhysicalState = item.PhysicalState,
                Confirmation = item.Confirmation,
            };

            var state = item.PhysicalState;
            if (observed is not null
                && observed.TryGetValue(ApiPlanOperationJournalValidator.BuildIdentityKey(item), out var current))
            {
                state = current;
                target.PhysicalState = current;
            }

            var pendingDelete = item.Action == JournalInventoryAction.Delete && state != JournalPhysicalState.Absent;
            var pendingFolder = item.ObjectType == JournalObjectType.Folder
                && item.Action == JournalInventoryAction.Preserve
                && item.EmptyConfirmed == false
                && state != JournalPhysicalState.Absent;
            target.Queued = pendingDelete || pendingFolder;
            targets.Add(target);
        }

        return Order(targets);
    }

    private static int RemovalRank(JournalObjectType objectType) => objectType switch
    {
        JournalObjectType.ApiObject => 0,
        JournalObjectType.Procedure => 1,
        JournalObjectType.Sdt => 2,
        JournalObjectType.MetadataFile => 3,
        JournalObjectType.Folder => 4,

        // A Transaction nunca entra na fila destrutiva; ela só aparece como Preserve, no fim.
        _ => 5,
    };
}

/// <summary>
/// Um alvo da remoção, com a identidade pela qual ele será relido antes de cada tentativa.
///
/// Nome, Description ou prefixo isolados **nunca** autorizam exclusão: o que autoriza é a
/// identidade validada que este tipo carrega — GUID, <c>FileId</c> + hash esperado, ou a
/// identidade histórica composta inteira.
/// </summary>
public sealed class ApiPlanRemovalTarget
{
    public JournalObjectType ObjectType { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// O que o inventário do envelope declara sobre este alvo **agora**. Não é o mesmo que
    /// <see cref="Queued"/>: o Folder próprio entra na fila, mas só pode ser declarado
    /// <c>Delete</c> quando a confirmação de vazio existir — antes disso, declará-lo assim
    /// afirmaria uma medição que ninguém fez.
    /// </summary>
    public JournalInventoryAction Action { get; set; } = JournalInventoryAction.Delete;

    /// <summary>
    /// Verdadeiro para os alvos que a fila destrutiva vai tentar. SDTs compartilhados, Folder
    /// reutilizado e a Transaction ficam fora dela e aparecem somente como <c>Preserve</c>.
    /// </summary>
    public bool Queued { get; set; } = true;

    public JournalIdentityKind IdentityKind { get; set; } = JournalIdentityKind.Guid;

    public Guid? Guid { get; set; }

    public int? FileId { get; set; }

    public string? ExpectedHash { get; set; }

    public ApiPlanOperationJournalCompositeIdentity? Composite { get; set; }

    /// <summary>Exigido <c>true</c> para apagar um Folder próprio.</summary>
    public bool? EmptyConfirmed { get; set; }

    public bool OwnershipValidated { get; set; } = true;

    /// <summary>Estado observado no preflight, antes de qualquer tentativa.</summary>
    public JournalPhysicalState PhysicalState { get; set; } = JournalPhysicalState.Present;

    public JournalConfirmation Confirmation { get; set; } = JournalConfirmation.NotAttempted;

    /// <summary>Recibos da F2 já associados a este alvo nesta operação.</summary>
    public IList<int> ReceiptSequences { get; } = new List<int>();

    /// <summary>
    /// Chave de igualdade do alvo dentro da operação. É a mesma que o validador usa, para que
    /// dois recibos do mesmo objeto não virem dois itens de inventário.
    /// </summary>
    public string Key => ApiPlanOperationJournalValidator.BuildIdentityKey(ToInventoryItem());

    /// <summary>Descrição curta para Output e relatório — diagnóstico, nunca autorização.</summary>
    public string Describe() => ObjectType + ":" + Name;

    public ApiPlanOperationJournalInventoryItem ToInventoryItem()
    {
        var item = new ApiPlanOperationJournalInventoryItem
        {
            ObjectType = ObjectType,
            IdentityKind = IdentityKind,
            Guid = Guid,
            FileId = FileId,
            Composite = Composite,
            EmptyConfirmed = EmptyConfirmed,
            Name = Name,
            OwnershipValidated = OwnershipValidated,
            Action = Action,
            PhysicalState = PhysicalState,
            Confirmation = Confirmation,
            ExpectedHash = ExpectedHash,
        };

        foreach (var sequence in ReceiptSequences)
        {
            item.ReceiptSequences.Add(sequence);
        }

        return item;
    }

    /// <summary>
    /// Registra a observação de uma tentativa no próprio alvo, para que o checkpoint seguinte
    /// leve ao envelope o estado físico atual — e não o que se supunha no preflight.
    /// </summary>
    public void Observe(ApiPlanRemovalAttemptResult result)
    {
        switch (result)
        {
            case ApiPlanRemovalAttemptResult.Confirmed:
                // O Folder próprio chega aqui como Preserve e só vira Delete no snapshot em que
                // a confirmação de vazio, medida imediatamente antes da exclusão, já existe.
                Action = JournalInventoryAction.Delete;
                if (ObjectType == JournalObjectType.Folder)
                {
                    EmptyConfirmed = true;
                }

                PhysicalState = JournalPhysicalState.Absent;
                Confirmation = JournalConfirmation.Absent;
                break;

            case ApiPlanRemovalAttemptResult.StillPresent:
                PhysicalState = JournalPhysicalState.Present;
                Confirmation = JournalConfirmation.Confirmed;
                break;

            case ApiPlanRemovalAttemptResult.AbsentBeforeDelete:
                // Ausência antes do primeiro Delete não é sucesso implícito: o alvo não foi
                // tentado, e quem o apagou não foi esta operação.
                PhysicalState = JournalPhysicalState.Absent;
                Confirmation = JournalConfirmation.NotAttempted;
                break;

            case ApiPlanRemovalAttemptResult.Preserved:
                Action = JournalInventoryAction.Preserve;
                PhysicalState = JournalPhysicalState.Present;
                Confirmation = JournalConfirmation.NotAttempted;
                break;

            default:
                PhysicalState = JournalPhysicalState.Unknown;
                Confirmation = JournalConfirmation.Unreadable;
                break;
        }
    }
}

/// <summary>
/// Resultado de uma tentativa de exclusão, já classificado por **evidência** — releitura do
/// alvo depois da chamada —, nunca pelo texto da exceção da IDE.
/// </summary>
public enum ApiPlanRemovalAttemptResult
{
    /// <summary>O alvo não foi reencontrado depois do <c>Delete()</c>. Sai da fila.</summary>
    Confirmed = 0,

    /// <summary>
    /// O <c>Delete()</c> lançou ou retornou, e a releitura comprova que o alvo continua lá.
    /// Único caso que devolve o alvo à fila, para a passada seguinte.
    /// </summary>
    StillPresent = 1,

    /// <summary>
    /// O alvo previsto não foi localizado antes do <c>Delete()</c>. Não é sucesso: encerra a
    /// operação em <c>Partial</c> com <c>TargetAbsentBeforeDelete</c>.
    /// </summary>
    AbsentBeforeDelete = 2,

    /// <summary>Falha conhecida e não retryable da etapa. Encerra em <c>Partial</c>.</summary>
    StageFailed = 3,

    /// <summary>
    /// Releitura ilegível, divergente ou ambígua. Bloqueia sem retry automático: um resultado
    /// que não se conhece nunca vira «não apagou».
    /// </summary>
    OutcomeUnknown = 4,

    /// <summary>
    /// O alvo sai da fila sem ser excluído, por decisão do próprio contrato — hoje, o Folder
    /// próprio que não ficou vazio. Vira <c>Preserve</c> no inventário e não impede
    /// <c>Removed</c>.
    /// </summary>
    Preserved = 5,
}
