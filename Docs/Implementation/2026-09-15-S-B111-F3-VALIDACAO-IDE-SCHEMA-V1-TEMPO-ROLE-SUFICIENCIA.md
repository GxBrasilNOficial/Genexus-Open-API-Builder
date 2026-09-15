# S-B111 · F3 — validação na IDE: schema V1 (tempo, role, suficiência)

**Data:** 2026-09-15.
**Motivo:** depois do encerramento da sprint, a conferência cruzou as decisões aprovadas com
o código e achou três divergências de escrita no diário. Nenhuma decide exclusão de objeto; o
que se perdia era diagnóstico e auditabilidade. O schema V1 **ainda não saiu em release**
(ausente na tag `v0.1.0-alpha.7`), então a correção foi aditiva sem migração.

**Commits de código:** `e20e0aa` (escrita: tempo, `ApiPlanJournalRoles`,
`inventorySufficiency`) e `c8d3038` (leitura tolerante a diário legado). Gates e esta
evidência fecham a frente.

**Ambiente:** GeneXus 18 U15 · KB `wsEducacaoSpTeste` · Transaction `Teste` · File do diário
`GxOpenApiBuilder_OperationJournal` `FileId=88` · DLL Release instalada com
`Install-ExtensionForGeneXus18.bat` (sem `genexus /install`; manifesto inalterado).

## O que cada gap precisava provar

| Gap | Decisão | Prova na IDE |
|---|---|---|
| 1 — recibo sem tempo | 29: `startedUtc`, `endedUtc`, `durationMs` | Envelope novo aceito em Apply, Remover, aborto e recuperação. Descompasso serializer/validador bloqueia antes de `File.Save()` |
| 2 — vocabulário de `role` | 30: `MainApi`, serviços, `OwnSdt`, … | Escrita fechada em `ApiPlanJournalRoles`; no Remover com GUID o `composite` não viaja (identidade por GUID). Distinção das Procedures no plano e nos gates; Apply/Recovery aceitam o inventário |
| 3 — suficiência implícita | 21: `InventorySufficient` / `InventoryInsufficient` | Linha explícita no Remover: `Inventario de remocao avaliado: InventorySufficient` |

## 1. Apply completo

`OperationId=e0fd7aa0-375b-4176-847e-d9c002cd1a2d`,
`ApplicationId=9513c86b-c70c-4cd7-9dc5-04e762be5d32`.

- Diário aberto: `OperationKind=Apply`, `FileId=88`, `Created=False`, `Bytes=1014`.
- Checkpoint `API Object confirmado`: `Running/ApiPhysicallySaved`, `Bytes=21071`,
  `Recibos=30`, `Inventário=25`.
- Checkpoint `conclusão`: `Completed/Completed`, `Bytes=21064`, `Recibos=30`,
  `Inventário=25`.
- Custo do diário: `Checkpoints=4`, `TotalMs=334`, `Durability=Confirmed`.
- Relatório: `SuccessWithWarnings`, `Criados=25`, `Atualizados=3`, `Bloqueados=0`,
  `PersistenceReceipts=30`, `DuraçãoMs=13447`.

## 2. Remover API gerada

`OperationId=b5806422-02d9-4ab8-83ca-68e7e82e8b3c` (mesmo `ApplicationId` do Apply).

- **`Inventario de remocao avaliado: InventorySufficient — Alvos=30, NaFila=25.`**
- Diário aberto: `OperationKind=Remove`, `Bytes=10766`.
- Checkpoint `passada de remoção`: `Running/RemovalInProgress`, `Recibos=25`,
  `Inventário=30`.
- Remoção: `Estado=Removed`, `Deleted=25`, Folder `TesteOpenApi` reutilizado preservado,
  SDTs compartilhados intocados.
- Checkpoint `conclusão`: `Removed/Removed`, `Recibos=25`, `Inventário=30`.
- Custo do diário: `Checkpoints=4`, `TotalMs=114`, `Durability=Confirmed`.
- Relatório: `Success`, `Removidos=25`, `PersistenceReceipts=25`, `DuraçãoMs=5206`.

## 3. Apply abortado

`OperationId=86810295-690a-420c-b63f-c78c81fc3ab4`,
`ApplicationId=3fab4cd2-fcd4-4285-ac0f-656ba675bcb4`.

- Diário aberto: `OperationKind=Apply`, `Bytes=1014`.
- Aborto pelo usuário; checkpoint `interrupção (UserAborted)`:
  `Partial/NotStarted`, `Bytes=10638`, `Recibos=14`, `Inventário=14`.
- Custo do diário: `Checkpoints=3`, `TotalMs=93`, `Durability=Confirmed`,
  `Estado=Partial/NotStarted`.
- Relatório: `Interrupted`, `Criados=14`, `Bloqueados=1`, `PersistenceReceipts=14`,
  `DuraçãoMs=2336`, aviso apontando `Recuperar operação interrompida`.

`logicalStage=NotStarted` é coerente: o aborto caiu durante a criação de SDTs, antes de
qualquer Save de API Object. O envelope ainda assim gravou 14 recibos confirmados — o
validador aceitou o formato novo sob interrupção.

## 4. Recuperar operação interrompida

Mesmo `OperationId=86810295-690a-420c-b63f-c78c81fc3ab4`.

- Leitura: `Partial/NotStarted`, envelope `Active`, `Durability=Confirmed`,
  `blockReason=UserAborted`.
- Próxima etapa: `Discard` (Apply interrompido não retoma o pipeline; o diário não carrega
  o contrato).
- Inventário reidratado e listado (14 SDTs `Create` / `Present`).
- `Recuperação concluída: Etapa=Discard`, `Estado=Completed`. Nenhum objeto apagado; KB
  liberada; inventário do que ficou pela metade permanece no diário.

A recuperação é o consumidor mais estrito: lê de volta recibos e inventário gravados pelo
formato novo e regrava o encerramento sem recusa de schema.

## Conclusão

Os quatro passos do roteiro passaram. Gap 3 ficou explícito no Output do Remover. Gaps 1 e
2 ficam sustentados pelo aceite do envelope em escrita e na leitura da recuperação, pelos
gates offline (`tests.operationJournalSchema`, `tests.operationJournalReceipts`,
`tests.removalQueue`, `tests.operationJournalRecovery`) e pelas remissões datadas nas
decisões 21, 24, 29 e 30.

**Estado residual na KB após o passo 4:** 14 SDTs parciais da `Teste` e o Folder
`TesteOpenApi` reutilizado. Não é defeito da frente — é o contrato do `Discard`. Limpeza
fica a cargo de Remover/Wizard na sessão seguinte, se desejado.
