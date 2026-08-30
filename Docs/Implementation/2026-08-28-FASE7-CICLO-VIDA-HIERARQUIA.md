# Fase 7 — Ciclo de vida sob hierarquia (Sprint 9)

Data: 2026-08-28.
Frente: Sprint 9 — Fase 7 (pós-`B099b`).
Escopo: releitura de `levels` no reencontro do Wizard, tolerância de leitura em preferências do Wizard, inventário dinâmico de SDTs próprios na remoção quando `objects.sdts.own` estiver ausente, e correção do falso positivo de conflito SDT no Sync hierárquico.

## Decisões de recorte

- Releitura hierárquica só quando a metadata V2 contém `levels` com subníveis (`HasHierarchicalLevels`); API plana permanece inalterada.
- Preferências: JSON legado **sem** `schemaVersion` continua válido (mesma política V1/V2 da metadata).
- Inventário dinâmico: se `objects.sdts.own` estiver ausente mas houver `levels` legível, reconstruir a ordem de remoção via stub `ApiPlan` + `ApiPlanSdtGenerationPlanBuilder` (inverso da pós-ordem de criação); fallback flat nos cinco nomes fixos quando **não** há hierarquia, ou quando o stub não monta (ex.: SDTs raiz ausentes). `levels` anunciado porém ilegível **falha** — não cai no flat.
- Sync: `DetectSdtConflicts` retorna vazio quando a metadata é hierárquica — o residual B099b (membros flat vs SDTs raiz) deixa de bloquear o preview.
- Corte `0.1.0-alpha.5` publicado em 2026-08-30 (tag + GitHub Release pre-release).

## Peças

| Peça | Alteração |
|---|---|
| `ApiPlanGeneratedApiRemovalInventory.cs` | **Novo** — `ResolveOwnSdtNames`, `BuildOwnSdtNamesForRemoval`, stub `ApiPlan` a partir de metadata |
| `ApiPlanGeneratedApiRemovalPlan.cs` | delega inventário a `ApiPlanGeneratedApiRemovalInventory` |
| `ApiPlanMetadataFileWriter.cs` | grava `own` via inventário compartilhado |
| `PrototypeWizardExistingApiContractReader.cs` | expõe `PersistedHierarchicalRoot` relendo `levels` V2 |
| `PrototypeWizardDialog.cs` | `TryLoadHierarchicalSelection` aplica `ApplyPersistedPrune` no reencontro |
| `PrototypeWizardPreferencesCodec.cs` | `SupportedSchemaVersions`, tolerância a `schemaVersion` ausente |
| `ApiPlanTransactionSyncOrchestrator.cs` | `DetectSdtConflicts` vazio em metadata hierárquica |
| `Tests/GeneratedApiRemoval/Test-ApiPlanGeneratedApiRemovalPlan.ps1` | carrega DLL Release; casos flat, V2 com `own` e inventário dinâmico |
| `Tests/WizardLifecycle/Test-ApiPlanWizardHierarchicalLifecycle.ps1` | **Novo** — contrato textual do ciclo de vida |
| `scripts/Invoke-PrePushMechanicalChecks.ps1` | gate `tests.wizardLifecycle` |

## Validação offline (2026-08-28)

- Build Release canônico: 0 avisos, 0 erros.
- `tests.generatedApiRemovalPlan`: PASS (flat, V2+`own`, dinâmico sem `own`).
- `tests.wizardLifecycle`: PASS.
- `tests.wizardPreferences`: PASS (JSON legado sem `schemaVersion`).
- `tests.wizardContract` (`Test-PrototypeWizardExistingApiFilters.ps1`): PASS.
- `tests.metadataHierarchical`: PASS.

## Validação IDE — Wizard apply (2026-08-28)

KB `wsEducacaoSpTeste`, Transaction `Teste`, GeneXus 18 U15. Regeneração após Remover do B099b (KB sem `apiTeste`). Quatro subníveis: `TesteItem` → `TesteItemFolio` → `TesteItemFolioDoc` e irmão `TestePortfolio`. Linha de base visual em Requests (filho direto com contador; netos sem contador) antes do apply.

| Check | Resultado |
|---|---|
| Relatório B081 | `Operação='Wizard'`, `SuccessWithWarnings`, `Criados=24`, `Atualizados=3`, `Bloqueados=0`, `Avisos=2`, `DuraçãoMs=25685` |
| Composição | 18 SDTs próprios + 4 Procedures + `apiTeste` + File metadata; 3 SDTs compartilhados atualizados |
| B060 | `SchemaVersion='GOAB_API_METADATA_B060_V2'`, `Status='Created'`, `Guid='60d3c289-f037-4ae1-b262-769c87be99b7'`, `Bytes=115730`, `Sha256='00A9D356FAF5E96729524190FE6E66BDF9343BFC9DA3B7F4A24E10E45319C8A0'` |
| B067 `PlannedContractHash` | `88AF3B4A9FAF0B2514AF8291C8212E8F871ECFE8C4DEA33677CCD5CD4A835553` (igual ao smoke B099b hierárquico) |
| Avisos | fallback de descrições em inglês; Folder `TesteOpenApi` reutilizado |

## Validação IDE — Critério 8 / reencontro (2026-08-28)

Reabertura do Wizard na mesma Transaction (`Estado: teste de reencontro`). Wizard **cancelado** sem escrita após a captura.

| Nível no seletor Requests | Incluir subnível | Contador List | Campos |
|---|---|---|---|
| `TesteItem` | marcado | marcado | `TesteItemId`, `TesteItemObs` (Create/Update) |
| `TesteItem / TesteItemFolio` | marcado | ausente (correto) | `TesteItemFolioId`, `TesteItemFolioObs` (Create/Update) |

Conclusão: `ApplyPersistedPrune` restaurou a seleção hierárquica da metadata V2; critério 8 aprovado.

## Validação IDE — Sync (2026-08-28)

Menu **Sincronizar com a Transaction** na `Teste`/`apiTeste`.

| Check | Resultado |
|---|---|
| Diff B085 | `Adicionados=0; Removidos=0; Renomeados=0; Modificados=0; Inalterados=16` |
| Conflito SDT | **nenhum** (residual B099b ausente) |
| Relatório B081 | `Operação='Sincronizar'`, `SuccessWithWarnings`, `DuraçãoMs=0`, título «Nenhuma sincronizacao necessaria.» |
| KB | sem alteração |

## Validação IDE — Remover preview (2026-08-28)

Plano B086 lido da metadata V2 (`objects.sdts.own`); usuário respondeu **Não** — KB intacta (`apiTeste` permanece).

| Check | Resultado |
|---|---|
| Procedures | 4 (`List`, `Get`, `Create`, `Update`) |
| SDTs próprios | 18 — ordem de remoção (ListResponse → … → CreateRequest folha) |
| SDTs compartilhados | 3 preservados |
| Folder | `TesteOpenApi` reutilizado; nunca apagar |
| Business Component | não revertido |
| Cancel | `[B086] Remocao cancelada pelo usuario … Nenhuma alteracao foi feita na KB` |

Inventário dinâmico sem bloco `own` ficou coberto pelo teste offline (`tests.generatedApiRemovalPlan`); não exigiu export manual na IDE neste smoke.

## Residual consciente

- Inventário dinâmico: sem `own`, com `levels` legível e stub montável → ordem hierárquica; sem hierarquia ou stub incompleto (ex.: SDTs raiz ausentes) → fallback flat dos cinco nomes. **`levels` presente mas ilegível** (`levelName`/`attributeGuid` etc.) → a remoção **falha** com erro explícito; não usa flat (evita órfãos de subnível). Sem teste IDE desse caso corrompido; coberto offline em `tests.generatedApiRemovalPlan`.
- Corte `0.1.0-alpha.5` publicado em 2026-08-30 (tag + GitHub Release pre-release, dois assets DLL).
- Critérios 6 (`Gx_FabricaBrasil`) e 10 (smoke U13): eram gates da sprint fora desta fase; **fechados** em 2026-08-28 e 2026-08-29 — evidências `2026-08-28-CRITERIO6-GX-FABRICABRASIL.md` e `2026-08-29-CRITERIO10-SMOKE-GX18U13.md`.
- Lacuna Sync ADDED/rename em subnível (apply IncludeAdded / remap por GUID): **fechada** em 2026-08-28 (offline + smoke IDE com delta; ver seção seguinte).
- **Sync hierárquico — falso `Added` por campo omitido de propósito (corrigido 2026-08-28):** a poda de subnível passa a gravar em `Fields` o catálogo completo do nível e deixa a omissão só em `Selected*`. O Sync (`FlattenToSyncSnapshots`) deixa de tratar campo desmarcado como `Added`. Metadata já gravada no formato antigo (união em `fields`) só melhora após Wizard/Sync que reescreva `levels`. Teste offline: `tests.wizardHierarchical` (asserção `LineTotal` permanece em `Fields` fora de `Selected*`). **Smoke U15 2026-08-28:** Wizard em `Teste`/`apiTeste` desmarcou `TesteItemObs2` em Create/Update (`Updated=27`, `Blocked=0`, metadata `Bytes=116942`, `PlannedContractHash='C1640995…'`); Sync imediato sem mudança na Transaction → `Adicionados=0; Removidos=0; Renomeados=0; Modificados=0; Inalterados=17` (`Nenhuma sincronizacao necessaria`).

## Sync hierárquico — IncludeAdded e Selected* por GUID (2026-08-28)

Gap: o diff do Sync achata a árvore e enxerga ADDED em subnível, mas o apply fazia `CreateDefault` + `ApplyPersistedPrune` (apagava a seleção do campo novo) e as listas flat só resolviam GUID na raiz. Rename pelo mesmo GUID também perdia o nome em `Selected*`.

| Peça | Alteração |
|---|---|
| `ApiPlanHierarchicalWizardSelection.cs` | `ResolvePersistedNamesToCurrent` no prune; `IncludeAddedFieldsByGuid` |
| `ApiPlanTransactionSyncOrchestrator.cs` | `ApplyHierarchicalIncludeAdded` após o prune em `BuildSelection` |
| `Tests/WizardHierarchical/Test-ApiPlanHierarchicalWizardSelection.ps1` | casos rename (Old→New por GUID) e ADDED (`NewAttr` + `Prune`) |

Validação offline: Build Release 0 erros; `tests.wizardHierarchical` PASS (rename + ADDED).

### Smoke IDE Sync com delta (2026-08-28)

KB `wsEducacaoSpTeste`, Transaction `Teste` / `apiTeste`. Atributo novo `TesteItemObs2` (VARCHAR) em `TesteItem`. Include marcado em Create/Update/Response; ListFilters vazio. Manifesto inalterado.

| Check | Resultado |
|---|---|
| Diff B085 | `Adicionados=1; Removidos=0; Renomeados=0; Modificados=0; Inalterados=16` — `+ TesteItemObs2` |
| Conflito SDT | nenhum |
| Relatório B081 | `Operação='Sincronizar'`, `SuccessWithWarnings`, `Criados=0`, `Atualizados=27`, `Bloqueados=0`, `Avisos=2`, `DuraçãoMs=24434` |
| Metadata B060 | `Reencountered`, V2, `Bytes=117066`, SHA-256 `B3DC4F39…4096A31C` |
| B067 `PlannedContractHash` | `B04A8DFB9C1B511BB2D8D8173A0D4E8998A6416804B2C406A61C813DFE6B8F58` |
| SDT Create | `sdtTeste_API_CreateRequest_TesteItem` contém `TesteItemObs2` |
| SDT Update | `sdtTeste_API_UpdateRequest_TesteItem` contém `TesteItemObs2` |
| SDT Response | `sdtTeste_API_Response_TesteItem` contém `TesteItemObs2` |

## Conclusão

Fase 7 encerrada em 2026-08-28: código, testes offline, gate `tests.wizardLifecycle` e smoke IDE (apply, critério 8, Sync zero-diff, Remover preview). Lacuna Sync ADDED/rename fechada no mesmo dia (offline + smoke com delta em `TesteItemObs2`). Corte `0.1.0-alpha.5` publicado em 2026-08-30; próxima frente = `B100`.
