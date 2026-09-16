# S-B111 · F3 — validação na IDE: leitura de diário legado (FabricaBrasil18Test)

**Data:** 2026-09-15.
**Motivo:** a tolerância a diário legado (itens 137–139 do checkpoint) estava provada
por gate e por sonda offline; faltava exercê-la na IDE sobre um File real gravado
**antes** das correções de schema V1 (tempo no recibo, `role` canônico, `null` honesto
na regravação). O diário da KB grande `FabricaBrasil18Test` (`FileId=138`) ainda estava
nesse formato.

**Commits de código (pré-requisito):** `c8d3038` (leitura tolerante), `bba55cb`
(regravação sem tempo sintético). Esta evidência não altera o emissor.

**Ambiente:** GeneXus 18 U15 · KB `FabricaBrasil18Test` · Transaction `Empresa` ·
File `GxOpenApiBuilder_OperationJournal` `FileId=138` · DLL Release corrente
instalada com `Install-ExtensionForGeneXus18.bat` (sem `genexus /install`;
manifesto inalterado).

## 1. Estado pré-Apply (legado)

Export em `d:\Temp\GxOpenApiBuilder_OperationJournal.json` antes do Apply:

| Campo | Valor |
|---|---|
| `operationId` | `0e5f2323-0223-497b-a834-6322de4e2b18` |
| Estado | `Completed` / `Completed` (terminal — não bloqueia Apply novo) |
| `updatedUtc` | `2026-09-15T16:38:06.676Z` (era P8 / medição 9b) |
| Recibos | 54, **sem** `startedUtc` / `endedUtc` / `durationMs` |
| `composite.role` | `Generated` (vocabulário antigo) |
| `plan.inventorySufficiency` | ausente |

## 2. Apply de reencontro

Wizard na `Empresa` → reencontro → Concluir e aplicar.

Sinais do Output:

- Preflight agregado aprovado antes do primeiro `Save()`.
- `Diário aberto`: `OperationKind=Apply`,
  `OperationId=2577722b-c13b-4b82-8f57-82f15c87c6c0`,
  `ApplicationId=b5ad3600-d2eb-4f73-b3bf-aa2c3db44ed5`, `FileId=138`,
  `Created=False` (reusou o File legado).
- Checkpoint `conclusão`: `Completed/Completed`, `Recibos=10`, `Inventário=6`,
  `Bytes=7490`.
- Custo do diário: `Checkpoints=4`, `TotalMs=240`, `Durability=Confirmed`.
- Relatório: `SuccessWithWarnings`, `Criados=0`, `Atualizados=53`, `Bloqueados=0`,
  `PersistenceReceipts=10`, `DuraçãoMs=46753`.

Se o leitor ainda exigisse tempo nos recibos, o File pré-correção teria sido
recusado e o Apply não abriria o diário.

## 3. Estado pós-Apply (envelope novo)

Mesmo path de export, após o Apply:

| Campo | Valor |
|---|---|
| `operationId` | `2577722b-c13b-4b82-8f57-82f15c87c6c0` (bate o Output) |
| Estado | `Completed` / `Completed`, `Durability=Confirmed` |
| Bytes | 7490 (bate o checkpoint) |
| Recibos | 10, **com** `startedUtc` / `endedUtc` / `durationMs` reais |
| Sintético `0001-01-01` / `durationMs=0` | **0** ocorrências |
| `composite.role` | `List`, `Get`, `Create`, `Update` |

## 4. Alcance desta prova

- **Provado:** leitura na IDE de diário legado terminal sem bloqueio de schema, no
  mesmo `FileId`, seguida de escrita de envelope V1 estrito que o substitui
  (decisão 44: um diário por KB).
- **Fora deste caminho:** a regravação com `null` honesto de um envelope
  **reidratado** (mesmo material legado salvo de novo). Apply terminal substitui;
  esse detalhe permanece nos gates offline (`tests.operationJournalSchema`) e no
  commit `bba55cb`.
