#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// S-B111 / F3 P4 — a fila de remoção por passadas, com orçamento fechado.
///
/// A fila **não é exclusiva de SDTs**: ela abrange todos os tipos removíveis que o plano
/// validou — API Object, Procedures, SDTs, File de metadata e Folder próprio vazio. O `P` da
/// matriz de checkpoints da seção 4.4 é o número de passadas desta fila completa, não do laço
/// de SDTs que existia antes.
///
/// As regras de cada passada vêm da seção 4.3 e são fechadas:
///
/// - somente <see cref="ApiPlanRemovalAttemptResult.StillPresent"/> recoloca um alvo na fila.
///   Ele é o único caso em que a releitura **comprova** que o objeto continua na KB, e é assim
///   que uma ordem de dependência imperfeita se resolve sozinha na passada seguinte;
/// - <see cref="ApiPlanRemovalAttemptResult.Confirmed"/> retira o alvo;
/// - <see cref="ApiPlanRemovalAttemptResult.OutcomeUnknown"/> bloqueia a operação inteira, sem
///   retry automático: um resultado que não se conhece não vira «não apagou»;
/// - <see cref="ApiPlanRemovalAttemptResult.AbsentBeforeDelete"/> encerra em <c>Partial</c>,
///   porque um alvo previsto que já não está lá não foi apagado por esta operação;
/// - o orçamento é <c>max(1, itens Delete)</c>. Atingido o limite com itens pendentes, o
///   envelope termina em <c>Partial</c> com <c>RetryBudgetExhausted</c> — não há loop infinito
///   nem retry silencioso em outra operação.
///
/// A decisão nunca lê o texto da exceção da IDE: quem classifica é a releitura do alvo, e este
/// tipo recebe a classificação já pronta. Isso o mantém SDK-free e testável offline.
/// </summary>
public static class ApiPlanRemovalQueue
{
    /// <summary>
    /// Executa a fila até o estado terminal.
    /// </summary>
    /// <param name="targets">Alvos validados; só os marcados como enfileirados são tentados.</param>
    /// <param name="attempt">Tentativa física de exclusão, já classificada por releitura.</param>
    /// <param name="onPassCompleted">
    /// Checkpoint de fim de passada (CP3 do Remove). Recebe o número da passada e o que restou
    /// pendente. Não é chamado quando a passada termina por bloqueio: nesse caso o checkpoint é
    /// o terminal, e gravar dois seguidos afirmaria uma passada que não se completou.
    /// </param>
    /// <param name="maxPasses">
    /// Orçamento explícito. Omitido, vale <c>max(1, itens da fila)</c> — que é o contrato.
    /// </param>
    public static ApiPlanRemovalQueueResult Run(
        IEnumerable<ApiPlanRemovalTarget> targets,
        Func<ApiPlanRemovalTarget, ApiPlanRemovalAttemptResult> attempt,
        Action<int, IReadOnlyList<ApiPlanRemovalTarget>>? onPassCompleted = null,
        int? maxPasses = null)
    {
        if (targets is null)
        {
            throw new ArgumentNullException(nameof(targets));
        }

        if (attempt is null)
        {
            throw new ArgumentNullException(nameof(attempt));
        }

        var ordered = ApiPlanRemovalIntent.Order(targets);
        var budget = maxPasses ?? ApiPlanRemovalIntent.ResolveMaxPasses(ordered);
        if (budget < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPasses), budget, "O orçamento de passadas é sempre positivo.");
        }

        var pending = ordered.Where(target => target.Queued).ToList();
        var deleted = new List<ApiPlanRemovalTarget>();
        var preserved = new List<ApiPlanRemovalTarget>();
        var passes = 0;

        while (pending.Count > 0)
        {
            if (passes >= budget)
            {
                // O limite existe para que uma dependência circular, ou um objeto que a IDE
                // recusa sempre, pare de consumir tentativas. O que sobrou fica declarado.
                return new ApiPlanRemovalQueueResult(
                    JournalOperationState.Partial,
                    JournalBlockReason.RetryBudgetExhausted,
                    passes,
                    budget,
                    deleted,
                    preserved,
                    pending,
                    blockingTarget: pending[0]);
            }

            passes++;
            var next = new List<ApiPlanRemovalTarget>();
            foreach (var target in pending)
            {
                var result = attempt(target);
                target.Observe(result);
                switch (result)
                {
                    case ApiPlanRemovalAttemptResult.Confirmed:
                        deleted.Add(target);
                        break;

                    case ApiPlanRemovalAttemptResult.StillPresent:
                        next.Add(target);
                        break;

                    case ApiPlanRemovalAttemptResult.Preserved:
                        preserved.Add(target);
                        break;

                    case ApiPlanRemovalAttemptResult.AbsentBeforeDelete:
                        return Interrupted(
                            JournalOperationState.Partial,
                            JournalBlockReason.TargetAbsentBeforeDelete,
                            passes,
                            budget,
                            deleted,
                            preserved,
                            Remaining(pending, target, next),
                            target);

                    case ApiPlanRemovalAttemptResult.StageFailed:
                        return Interrupted(
                            JournalOperationState.Partial,
                            JournalBlockReason.StageFailed,
                            passes,
                            budget,
                            deleted,
                            preserved,
                            Remaining(pending, target, next),
                            target);

                    case ApiPlanRemovalAttemptResult.OutcomeUnknown:
                        return Interrupted(
                            JournalOperationState.OutcomeUnknown,
                            JournalBlockReason.OutcomeUnknown,
                            passes,
                            budget,
                            deleted,
                            preserved,
                            Remaining(pending, target, next),
                            target);

                    default:
                        throw new InvalidOperationException(string.Format(
                            CultureInfo.InvariantCulture,
                            "Resultado de tentativa fora do contrato da fila: {0}.",
                            result));
                }
            }

            onPassCompleted?.Invoke(passes, next);
            pending = next;
        }

        return new ApiPlanRemovalQueueResult(
            JournalOperationState.Removed,
            blockReason: null,
            passes,
            budget,
            deleted,
            preserved,
            pending,
            blockingTarget: null);
    }

    /// <summary>
    /// O que ainda não saiu quando a passada foi interrompida: o alvo que bloqueou, os que já
    /// tinham voltado para a fila e os que nem chegaram a ser tentados nesta passada.
    /// </summary>
    private static IReadOnlyList<ApiPlanRemovalTarget> Remaining(
        IReadOnlyList<ApiPlanRemovalTarget> pending,
        ApiPlanRemovalTarget blocking,
        IReadOnlyList<ApiPlanRemovalTarget> requeued)
    {
        var remaining = new List<ApiPlanRemovalTarget>(requeued);
        var reached = false;
        foreach (var target in pending)
        {
            if (ReferenceEquals(target, blocking))
            {
                reached = true;
                remaining.Add(target);
                continue;
            }

            if (reached)
            {
                remaining.Add(target);
            }
        }

        return remaining;
    }

    private static ApiPlanRemovalQueueResult Interrupted(
        JournalOperationState state,
        JournalBlockReason blockReason,
        int passes,
        int budget,
        IReadOnlyList<ApiPlanRemovalTarget> deleted,
        IReadOnlyList<ApiPlanRemovalTarget> preserved,
        IReadOnlyList<ApiPlanRemovalTarget> pending,
        ApiPlanRemovalTarget blockingTarget) =>
        new ApiPlanRemovalQueueResult(state, blockReason, passes, budget, deleted, preserved, pending, blockingTarget);
}

/// <summary>
/// Resultado terminal da fila. <c>Removed</c> só existe quando todos os itens <c>Delete</c>
/// saíram confirmados — nenhum <c>NotAttempted</c>, <c>Failed</c> ou <c>OutcomeUnknown</c>
/// pendente.
/// </summary>
public sealed class ApiPlanRemovalQueueResult
{
    internal ApiPlanRemovalQueueResult(
        JournalOperationState outcome,
        JournalBlockReason? blockReason,
        int passesExecuted,
        int maxPasses,
        IReadOnlyList<ApiPlanRemovalTarget> deleted,
        IReadOnlyList<ApiPlanRemovalTarget> preserved,
        IReadOnlyList<ApiPlanRemovalTarget> pending,
        ApiPlanRemovalTarget? blockingTarget)
    {
        Outcome = outcome;
        BlockReason = blockReason;
        PassesExecuted = passesExecuted;
        MaxPasses = maxPasses;
        Deleted = deleted;
        Preserved = preserved;
        Pending = pending;
        BlockingTarget = blockingTarget;
    }

    public JournalOperationState Outcome { get; }

    /// <summary>Motivo persistível, do enum fechado do envelope. Nulo somente em <c>Removed</c>.</summary>
    public JournalBlockReason? BlockReason { get; }

    public int PassesExecuted { get; }

    public int MaxPasses { get; }

    public IReadOnlyList<ApiPlanRemovalTarget> Deleted { get; }

    /// <summary>Alvos que saíram da fila sem exclusão, por decisão do contrato.</summary>
    public IReadOnlyList<ApiPlanRemovalTarget> Preserved { get; }

    /// <summary>O que continuava na KB quando a fila parou.</summary>
    public IReadOnlyList<ApiPlanRemovalTarget> Pending { get; }

    public ApiPlanRemovalTarget? BlockingTarget { get; }

    public bool IsComplete => Outcome == JournalOperationState.Removed;

    public string Describe() => string.Format(
        CultureInfo.InvariantCulture,
        "Outcome={0}, BlockReason={1}, Passadas={2}/{3}, Removidos={4}, Preservados={5}, Pendentes={6}, Bloqueado='{7}'.",
        Outcome,
        BlockReason?.ToString() ?? "<nenhum>",
        PassesExecuted,
        MaxPasses,
        Deleted.Count,
        Preserved.Count,
        Pending.Count,
        BlockingTarget?.Describe() ?? string.Empty);
}
