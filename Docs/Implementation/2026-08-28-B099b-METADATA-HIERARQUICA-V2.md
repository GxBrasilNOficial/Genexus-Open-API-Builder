# B099b — Metadata hierárquica V2 (Fase 6)

Data: 2026-08-28.
Frente: Sprint 9 — Fase 6 (`B099b`).
Escopo: `schemaVersion` V2 com árvore de níveis, leitura tolerante de V1, `PlannedContractHash` cobrindo a árvore, Sync alinhado à hierarquia, inventário `objects.sdts.own` para Remover, remoção do aviso V1 de ciclo de vida.

## Decisões de recorte

- Gravação sempre `GOAB_API_METADATA_B060_V2`; leitura aceita V1 e V2 (V1 = nível único).
- Sem migração autônoma na abertura do Wizard: V1 só vira V2 quando a geração aplica e regrava o File.
- `levels` só entra no JSON e no contrato B067 quando há subníveis selecionados; API plana mantém hash compatível com o contrato flat anterior.
- `objects.sdts.own` lista SDTs próprios na ordem de remoção (inventário gravado na apply). A Fase 7 acrescentou inventário dinâmico quando `own` estiver ausente (ver `Docs/Implementation/2026-08-28-FASE7-CICLO-VIDA-HIERARQUIA.md`).
- Critérios 6 (`Gx_FabricaBrasil`) e 10 (smoke U13) ficaram fora desta fase na época; **fechados** em 2026-08-28 / 2026-08-29 — ver `2026-08-28-CRITERIO6-GX-FABRICABRASIL.md` e `2026-08-29-CRITERIO10-SMOKE-GX18U13.md`.

## Peças

| Peça | Alteração |
|---|---|
| `ApiPlanMetadataFileWriter.cs` | `SchemaVersion=V2`, `SchemaVersionV1`, `SupportedSchemaVersions`, `levels`, `objects.sdts.own`, `levels` no `CreatePlannedContract` |
| `ApiPlanMetadataLevelsCodec.cs` | serialização/leitura da árvore, `HasHierarchicalLevels`, flatten para Sync |
| `ApiPlanApiObjectOwnership.cs` | overload de posse com lista de schemas |
| `ApiPlanGenerationStateReader.cs` / `ApiPlanApiObjectWriter.cs` | aceitam V1+V2 |
| `ApiPlanGeneratedApiRemovalPlan.cs` | V1+V2; usa `objects.sdts.own` quando presente |
| `ApiPlanTransactionSyncOrchestrator.cs` | diff por GUID na árvore; restaura `HierarchicalSelection` via `ApplyPersistedPrune` |
| `ApiPlanHierarchicalWizardSelection.cs` | `ApplyPersistedPrune`; removido `LifecycleV1WarningText` |
| `PrototypeWizardDialog.cs` / `ExtensionLocalization.cs` | sem aviso V1 de Remover/Sync |
| `Tests/MetadataHierarchical/` + gate `tests.metadataHierarchical` | round-trip levels, schemas, `ApplyPersistedPrune` |

## Validação offline

- Build Release canônico: 0 avisos, 0 erros.
- Build satélite U13: 0 erros (avisos MSB3277 ambientais conhecidos).
- `tests.metadataIntegrity`, `tests.generatedApiRemovalPlan`, `tests.metadataHierarchical`, `tests.wizardHierarchical`, `tests.apiObjectOwnership`, checker de comandos: PASS.

## Validação IDE — Wizard apply (2026-08-28)

KB `wsEducacaoSpTeste`, Transaction `Teste`, API `apiTeste`, GeneXus 18 U15. Reencontro hierárquico de quatro níveis; apply `SuccessWithWarnings`, `Updated=27`, `Blocked=0`, `Avisos=2`, `DuraçãoMs=24904`. Resumo sem aviso V1 de Remover/Sync.

| Check | Resultado |
|---|---|
| Output B060 `SchemaVersion` | `GOAB_API_METADATA_B060_V2` |
| Bytes / SHA-256 | `115730` / `E76E9C14ED6EDBDC187967BD4F39094D51505F43DF653D32481A830A00EC1A28` |
| B067 `PlannedContractHash` | `88AF3B4A9FAF0B2514AF8291C8212E8F871ECFE8C4DEA33677CCD5CD4A835553` (V1 estável `21F955426F5A1163…` substituído — árvore no contrato) |
| File Guid / Status | `bcc8bbbe-b3ee-46f5-a5a0-ef3a44e9aa37` / `Reencountered` |
| Export JSON | `D:\Temp\apiTeste_Metadata.json` |

Conferência do JSON exportado:

- `schemaVersion`: V2.
- `levels`: raiz `Teste` com filhos `TesteItem` → `TesteItemFolio` → `TesteItemFolioDoc` e irmão `TestePortfolio`; `includeListCount` nos filhos diretos; `selectedCreateFieldNames` / `selectedUpdateFieldNames` / `selectedResponseFieldNames` por nível.
- `objects.sdts.own`: 18 SDTs próprios na ordem de remoção (ListResponse → … → CreateRequest folha).
- `integrity.plannedContract.contract.levels`: espelha a árvore gravada no snapshot B067.

Avisos esperados (não bloqueantes): fallback de descrições em inglês; Folder `TesteOpenApi` reutilizado.

## Validação IDE — Sync apply (2026-08-28)

Mesma KB/API. Preview: diff hierárquico sem falso positivo de conflito SDT raiz (corrigido na Fase 7); apply com **Replace** nos três SDTs quando necessário; `PreservedSdts=0`.

| Check | Resultado |
|---|---|
| Trigger | `SyncB085` em SDTs, Procedures, BC/List, metadata |
| Preflight | Aprovado |
| Relatório B081 | `Operação='Sincronizar'`, `SuccessWithWarnings`, `Updated=27`, `Blocked=0`, `DuraçãoMs=23208` |
| B060 pós-Sync | V2, `Bytes=115730`, SHA-256 `99F1E2220B53B795B03344CB5CE9B0837F1363849403A37CD6617E9756981689` (≠ Wizard — `generatedAtUtc` atualizado; contrato estável) |
| B067 `PlannedContractHash` | `88AF3B4A9FAF0B2514AF8291C8212E8F871ECFE8C4DEA33677CCD5CD4A835553` (inalterado — esperado sem diff na Transaction) |

Conclusão Sync: diff hierárquico por GUID operacional; apply completo reencontra os 27 objetos; metadata V2 e baseline B067 preservados.

## Validação IDE — Remover preview (2026-08-28)

Mesma KB/API. Plano B086 lido da metadata V2 (`objects.sdts.own`); usuário respondeu **Não** — KB intacta.

| Check | Resultado |
|---|---|
| Procedures | 4 (`List`, `Get`, `Create`, `Update`) |
| SDTs próprios | 18 — ordem de `objects.sdts.own` (ListResponse → … → CreateRequest folha) |
| SDTs compartilhados | 3 preservados (`ErrorMessage`, `ErrorResponse`, `Pagination`) |
| Folder | `TesteOpenApi` reutilizado; nunca apagar |
| Business Component | não revertido |
| Cancel | `[B086] Remocao cancelada pelo usuario — Nenhuma alteracao foi feita na KB` |

Conclusão Remover (preview): inventário hierárquico V2 operacional; preview alinhado ao plano offline `ApiPlanGeneratedApiRemovalPlan`.

## Validação IDE — Remover apply (2026-08-28)

Mesmo plano B086; usuário respondeu **Sim**.

| Check | Resultado |
|---|---|
| Relatório B081 | `Operação='Remover'`, `Success`, `Removidos=24`, `Bloqueados=0`, `Avisos=0`, `DuraçãoMs=4037` |
| Composição | 1 API Object + 4 Procedures + 18 SDTs próprios + 1 File metadata |
| SDTs compartilhados | 3 intactos (Output B086) |
| Folder / BC | `TesteOpenApi` preservado; BC da Transaction não revertido |
| Órfãos | nenhum reportado |

Conclusão Remover (apply): remoção hierárquica completa via `objects.sdts.own`; KB limpa para regeneração futura (Wizard necessário antes de novo smoke HTTP).

## Validação IDE — Reencontro plano V1→V2 (2026-08-28)

KB `wsEducacaoSpTeste`, Transaction `NotaFiscal`, API `apiFiscalPublica` (plana, sem subníveis). Baseline V1 exportado antes do apply (`D:\Temp\apiNotaFiscal_Metadata.json`, `Bytes=83812`).

| Check | Antes (V1) | Depois (apply B099b) |
|---|---|---|
| `schemaVersion` | `GOAB_API_METADATA_B060_V1` | `GOAB_API_METADATA_B060_V2` |
| `levels` | ausente | `null` (plano explícito) |
| `objects.sdts.own` | ausente | 5 SDTs flat |
| Bytes / SHA-256 | 83 812 | 84 068 / `CFA7913E58C75F5CAA4B3D140D7338D6D94D2FDADC3BF234D5D6F4A49963A7FA` |
| B067 `PlannedContractHash` | `8F737F1ADDDEC1FDBDF9D2BEE1B09788FA5C5D40587C61DFC5AB8C25AF8BCABF` | **inalterado** (contrato flat estável) |
| Relatório B081 | — | `Updated=14`, `Blocked=0`, `SuccessWithWarnings`, `DuraçãoMs=11113` |

Ordem de `objects.sdts.own`: ListResponse → CreateRequest → UpdateRequest → ListFilters → Response.

Conclusão: leitura V1 + reencontro + apply grava V2 com inventário `own` **sem** inventar hierarquia; hash B067 preservado em API plana.

## Validação IDE — Build pós-Remover (2026-08-28)

Após o smoke de Remover na `Teste`, um **Build All** incremental da `apiFiscalPublica` no environment `NETPostgreSQL155` falhou na compilação de `type_SdtsdtTeste_API_ListResponse.cs` (`GxSimpleCollection<>`, `AppliedFilters` sem tipo). **Work With Objects** no Design não listava mais `apiTeste`, `procTeste_API_*` nem `sdtTeste_API_*` — o Design estava limpo.

Diagnóstico: resíduo de **cache de especificação/geração do environment**, não objeto vivo no Design. O incremental ainda tinha specs em `GXSPC155\GEN41\` (`apiTeste.sp1`, `procTeste_API_*.sp1`), entradas em `GeneXus.Programs.Common.sdts.targets` e `.cs` gerados da `Teste`. Consulta ao SQL (`ModelEntityVersion`, `ModelId = 1` Design) confirmou ausência dos objetos; registros órfãos permaneciam em snapshots/versions antigas e no cache local do generator.

| Ação | Environment | Resultado |
|---|---|---|
| Build All incremental | `NETPostgreSQL155` | **Falhou** (SDT Teste corrompido no cache) |
| **Rebuild All** | `NETPostgreSQL155` | **OK** — limpou cache e `sdts.targets` |
| Build All | `NETFrameworkSQLServer004` | **OK** (sem Rebuild All necessário nesta KB) |

**Lição operacional:** após `Remover API gerada`, confirmar limpeza no Design (Work With Objects) **e** executar **Rebuild All em cada environment** afetado antes de confiar em Build All incremental — especialmente em KBs de produção, onde Build All sozinho pode regenerar lixo por horas até falhar do mesmo modo. Build All incremental no segundo environment pode passar se o cache local já estiver alinhado; Rebuild All permanece o passo seguro quando houve Remover.

## Fechamento B099b

Fase 6 encerrada em 2026-08-28: código, testes offline, smoke IDE hierárquico (`Teste`: Wizard, Sync, Remover) e smoke plano V1→V2 (`NotaFiscal`). Residual Sync flat vs hierárquico **encerrado na Fase 7** — ver `Docs/Implementation/2026-08-28-FASE7-CICLO-VIDA-HIERARQUIA.md`.
