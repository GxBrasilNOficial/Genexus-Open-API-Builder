#nullable enable

using System;

namespace GenexusOpenApiBuilder.Extension;

/// <summary>
/// B082 Etapa 2 — exclusão mútua entre os quatro comandos longos da extensão.
/// Global ao processo (não por KB): uma operação em andamento recusa a segunda.
/// Não cobre comandos nativos da IDE durante <c>DoEvents</c>.
/// </summary>
internal static class ExtensionOperationGuard
{
    private static readonly object SyncRoot = new();
    private static string? _currentOperation;

    /// <summary>
    /// Tenta adquirir a guarda. Em caso de sucesso, o chamador **deve** liberar com
    /// <see cref="Exit"/> em <c>finally</c>, inclusive nos retornos antecipados.
    /// </summary>
    internal static bool TryEnter(string operationName)
    {
        if (string.IsNullOrWhiteSpace(operationName))
        {
            throw new ArgumentException("Operation name is required.", nameof(operationName));
        }

        lock (SyncRoot)
        {
            if (_currentOperation is not null)
            {
                return false;
            }

            _currentOperation = operationName;
            return true;
        }
    }

    internal static string? CurrentOperation
    {
        get
        {
            lock (SyncRoot)
            {
                return _currentOperation;
            }
        }
    }

    internal static void Exit()
    {
        lock (SyncRoot)
        {
            _currentOperation = null;
        }
    }

    /// <summary>
    /// Mensagem em português (fonte do Output); a tradução passa por
    /// <c>ExtensionOutputLocalization</c>.
    /// </summary>
    internal static string BuildBusyMessage(string? runningOperation)
    {
        var running = string.IsNullOrWhiteSpace(runningOperation) ? "outra operação" : runningOperation!;
        return "Operação recusada: já há '" + running + "' em andamento. Aguarde o término antes de iniciar outra.";
    }
}
