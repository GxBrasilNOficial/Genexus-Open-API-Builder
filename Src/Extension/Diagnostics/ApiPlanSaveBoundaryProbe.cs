#nullable enable

using System;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Adaptador do seam de persistência da F2: os pontos de gravação acoplados ao SDK chegam ao
/// núcleo <see cref="ApiPlanPersistenceCore"/> por aqui.
///
/// Até 2026-09-25 esta classe também tirava impressões digitais (SHA-256 de Source e Rules,
/// assinatura das variáveis) de cada objeto antes e depois do Pump e do Save, para testar a
/// hipótese de reentrância por <c>Application.DoEvents()</c> no <c>B109</c>. A stack completa de
/// 2026-09-25 localizou a falha numa corrida do SDK, síncrona, e as impressões foram retiradas.
/// O nome da classe ficou para não mexer nos pontos de gravação.
/// </summary>
internal static class ApiPlanSaveBoundaryProbe
{
    /// <summary>
    /// Adaptador único entre os pontos de persistência acoplados ao SDK e o núcleo
    /// SDK-free da F2.
    /// </summary>
    public static IDisposable BeginPersistence(
        ApiPlanPersistenceLog log,
        Action<ApiPlanPersistenceLog>? onDispose = null) =>
        ApiPlanPersistenceCore.Begin(log, onDispose);

    public static IDisposable SuspendPersistence() => ApiPlanPersistenceCore.Suspend();

    /// <summary>
    /// Hook interno para testes do adaptador SDK. Produção não o ativa nem
    /// expõe configuração persistente para injeção de falhas.
    /// </summary>
    internal static IDisposable BeginFaultInjection(IApiPlanPersistenceFaultInjector injector) =>
        ApiPlanPersistenceCore.BeginFaultInjection(injector);

    public static PersistenceReceipt? Persist(
        string operationKind,
        string objectType,
        string stage,
        string plannedName,
        PersistenceIdentity identity,
        Action persist,
        Func<PersistenceConfirmation> confirm) =>
        ApiPlanPersistenceCore.Persist(
            operationKind,
            objectType,
            stage,
            plannedName,
            identity,
            persist,
            confirm);

    public static PersistenceReceipt? Persist(
        PersistenceFaultPoint faultPoint,
        string operationKind,
        string objectType,
        string stage,
        string plannedName,
        PersistenceIdentity identity,
        Action persist,
        Func<PersistenceConfirmation> confirm) =>
        ApiPlanPersistenceCore.Persist(
            faultPoint,
            operationKind,
            objectType,
            stage,
            plannedName,
            identity,
            persist,
            confirm);

    public static PersistenceReceipt? RecordNotAttempted(
        string operationKind,
        string objectType,
        string stage,
        string plannedName,
        PersistenceIdentity identity,
        PersistenceConfirmation observation) =>
        ApiPlanPersistenceCore.RecordNotAttempted(
            operationKind,
            objectType,
            stage,
            plannedName,
            identity,
            observation);

    public static void NoteStageFailed(string stage, string reasonCode, string detail) =>
        ApiPlanPersistenceCore.NoteStageFailed(stage, reasonCode, detail);
}
