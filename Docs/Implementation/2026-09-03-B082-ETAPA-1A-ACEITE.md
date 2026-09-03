# B082 Etapa 1A — aceite de desempenho e medição

Data: 2026-09-03.
KB grande: `FabricaBrasil18Test` (`Fabrica Brasil Test`), GeneXus 18 U15, DLL canônica desta frente (índice único + reencontro idempotente de SDT).
KB pequena (Sync que grava): Transaction `NotaFiscal` (Delete opt-in ligado).

Plano: `Docs/Implementation/2026-09-02-B082-PLANO-HARDENING-E-DESEMPENHO.md`.
Código de índice: commit `c646ed9`. Este registro fecha a medição e o reencontro de SDT sem segundo `Save()`.

## Escopo entregue

- Índice criado uma vez por operação de escrita (Apply, Sync, validação agregada do Remover) e propagado por parâmetro.
- `EnsureAttributeExists` de Business Component e List usa o mapa do índice (`TryGetSingleAttribute`).
- Fora da 1A, de propósito: `PreflightRequiredProcedures`, `FindProcedure`, `FindListProcedure` (sem `RefreshProcedures`).
- `RefreshFolders` / `RefreshSdts` permanecem.

## Correção de reencontro (mesma DLL da medição)

O Apply gravava o mesmo SDT na fase de SDTs e de novo em Business Component e List. Com o intervalo da 1A mais curto, o SpecifierDaemon da IDE emitia `error: Error specifying SDT` falso no Output durante o Apply (B081 `SuccessWithWarnings`). `SpecifyObjects` no meio do Apply e `Save(SkipValidation)` foram tentados e **revertidos**.

O reencontro passou a pular `ConfigureSdt`/`Save()` quando a estrutura **e a ordem física** dos membros já batem com o plano (`MatchesPlannedSdtStructure`). O skip mecânico por lista (`PlannedSdtNames`) nas fases Business Component e List do Wizard foi removido; o Sync continua a honrar resolução **Keep** explícita por SDT. Comparação de membro `AttributeBasedOn` não exige Length/Decimals/Type do seed CHARACTER. Sem `Save()`, o status do item é `Unchanged` e o B081 não o lista como «Atualizado».

## Marcas estruturais (Apply e Sync)

Desapareceram: `Attribute/bc-find-attribute`, `Attribute/list-find-attribute`, `Procedure/procedure-preflight`, `SDT/apiobject-preflight-sdt`, `SDT/bc-ensure-sdt`.

Permanecem (1A padrão): `Procedure/bc-find-procedure`, `Procedure/list-find-procedure`, e `Procedure/apiobject-preflight-procedure` na geração nova (some no reaplicar quando a API já existe). `indice-create` uma vez por tipo; `SDT/indice-refresh` após a fase de SDTs.

## Apply — KB grande (metas só nesta KB)

| Operação | Meta 1A | Medido (recriar) | Reaplicar | Scans recrear / reaplicar |
|---|---|---|---|---|
| `Setor` | ≤ 25 s | ~17,7 s | ~11,4 s | 16 / 12 |
| `Empresa` | ≤ 134 s | ~60,7 s | ~46,6 s | 16 / 12 |
| `DocumentoFiscal` | ≤ 139 s | 64680 ms | 29919 ms; terceira corrida 29069 ms | 16 / 12 |

`DocumentoFiscal`: GUIDs estáveis no reaplicar (`apiDocumentoFiscal` `740c85fe…`, Response `94fe3d3b…`); `PlannedContractHash` `05F719B3…`; Criados=0 nas duas reaplicações. Sem `error: Error specifying SDT` no Apply após o skip de `Save()`.

## Remover — KB grande

Ganho 1A só na validação agregada. Localização/revalidação/confirmação pós-`Delete` continuam em leitura corrente (Etapa 1B).

| Operação | Meta 1A | Medido |
|---|---|---|
| `Empresa` | ≤ 35 s | ~35,3 s; **155 scans** (GetAll por delete; esperado na 1A) |
| `DocumentoFiscal` | ≤ 14 s | 15488 ms; 38 scans; Deleted=12 incluindo pasta criada vazia |

`Setor` foi removida e recriada no início do ciclo desta DLL; as três APIs ficaram na KB para o Build All. Uma corrida de `DocumentoFiscal` ficou ~1,5 s acima do teto; o plano admite oscilação de totais do Remover (~8,5%) e não trata isso como falha da conversão de índice.

## Sincronizar

- Sem mudança (`Setor` na KB grande; `NotaFiscal` na KB pequena): diff zerado, `DuraçãoMs=0`, aviso B081 «Nenhuma sincronizacao necessaria». Preview com 7 `indice-create`.
- Que grava (KB pequena, `+ NotaFiscalObs8` VARCHAR): Create/Update 9→10, Response 10→11, filtros 2→3; GUIDs iguais; hash B067 `AAF617A5…` → `382E8A08…`; **Sync Scans=13**; sem as cinco linhas proibidas; `bc-find-procedure` 4× e `list-find-procedure` 1× (Delete). Tempo ~5818 ms — o aceite do Sync é por marcas, não por relógio. Remover sem metadata na `NotaFiscal` bloqueou com zero exclusão (`apiNotaFiscal_Metadata` ausente).

## Build All

Mesmas APIs (`apiSetor`, `apiEmpresa` `1768ef26…`, `apiDocumentoFiscal` `740c85fe…`).

- `CSharpModel` (SQL Server): **Success** na primeira passagem; Specifier Daemon não reiniciou.
- `NETFrameworkPostgreSQL`: primeira passagem **Failed** em `type_SdtsdtEmpresa_API_CreateRequest.cs` (CS0029/CS0031) depois de **três** `Auto restarting Specifier daemon`. Specification tinha sido Success. `Build With These Only` da `apiDocumentoFiscal` falhou no mesmo C# da Empresa (`LastBuild.sln` / `GeneXus.Programs.Common`). `Build With These Only` da `apiEmpresa` regenerou os SDTs e compilou; Build All seguinte **Success** (`No objects to Specify`).

Classificação: geração C# inconsistente após crash do especificador, não tipo errado na KB. Isolar a API grande (Empresa) reespecifica os `type_Sdt*` e limpa o projeto comum.

## Monitor da IDE (D12, carona nesta DLL)

`ExtensionIdeScreenPlacement` entrou no commit de aceite da 1A (`ce30374`). É pauta da Etapa 3 do plano de 2026-09-02 (âncora no monitor da IDE), não do índice. Smoke U15 em 2026-09-03, KB pequena, GeneXus no monitor secundário: Wizard, Sincronizar e Remover abriram nesse monitor, na frente da IDE (janelas de comando, não o relatório B081 pós-Apply). Preferências do Wizard (`CenterParent`) e o restante da Etapa 3 (casca atrás do relatório, Folder preservado, DEMO) ficam fora.

## Validação manual — ordem física e Sync Keep/Replace (2026-09-03)

KB `wsEducacaoSpTeste`, Transaction `NotaFiscal` / `apiNotaFiscal`, DLL desta correção (pós-`ce30374`).

### Wizard — permuta Obs/Obs2 e controle

1. Permuta manual `NotaFiscalObs` ↔ `NotaFiscalObs2` na Transaction; Wizard Apply → `sdtNotaFiscal_API_Response` **Reencountered**, ordem corrigida no SDT, GUID `08dd0a7d-ef05-4f85-87f4-6f575419b655` estável, sem `error: Error specifying SDT`.
2. Segundo Apply idêntico → Response/Create/Update/ListFilters **Unchanged**; B081 sem esses SDTs em Atualizados; `Fase SDTs` ~595 ms vs ~1486 ms na rodada anterior.

### Sync — conflito de SDT (edição manual `NotaFiscalObs3` → `NotaFiscalObs3Manual`)

3. **Keep:** conflito listado; resolução Keep → aviso `SDT preservado (Keep): sdtNotaFiscal_API_Response`; Response **Unchanged** na fase SDTs; membro manual preservado; Sync **interrompido** na fase BC (`procNotaFiscal_API_Get` — Source do plano referencia `NotaFiscalObs3`, membro manual diverge). Comportamento esperado da tensão Keep + Procedures canônicas.
4. Após **Remover API gerada** + Wizard (recuperação de baseline B067), novo ensaio com **Replace** → `PreservedSdts=0`; Response **Reencountered**; BC, List e metadata concluídos; B081 `SuccessWithWarnings`, `Blocked=0`.

### Observação lateral — reencontro de coleções (fechada)

- **Fechado na IDE (2026-09-03):** Apply idêntico na `NotaFiscal` — 8/8 SDTs **`Unchanged`** (`ErrorResponse` e `ListResponse` inclusos); B081 sem SDT em Atualizados; `Fase SDTs=339 ms`. Causas: `CollectionItemName` pós-specifier + `StructureTypeReference`/`Id` em `ATTCUSTOMTYPE` (ex.: `Pagination`).
- Sync Keep interrompido pode deixar drift API Object ↔ metadata (BC grava API antes das Procedures; ver `Docs/Implementation/B085-SINCRONIZAR-COM-TRANSACTION.md`, seção «Escrita parcial do BC»).

## Descartado nesta frente

- `SpecifyObjects` / `ISpecifierService` no meio do Apply.
- `Save(..., SkipValidation)` nos SDTs.
- Filtrar a mensagem `error:` na Output.
- Meta de tempo de Sync na KB grande (sem linha de base; marcas na KB pequena bastam para call site).

## Status

Etapa 1A **aceita**. Próxima ação única: `B108` (plano aprovado). Residual `B082` (Etapas 1B, 2 e 3) permanece no plano de 2026-09-02, sem ser a pauta imediata.
