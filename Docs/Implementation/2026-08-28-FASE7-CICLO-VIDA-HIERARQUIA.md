# Fase 7 — Ciclo de vida sob hierarquia (Sprint 9)

Data: 2026-08-28.
Frente: Sprint 9 — Fase 7 (pós-`B099b`).
Escopo: releitura de `levels` no reencontro do Wizard, tolerância de leitura em preferências do Wizard, inventário dinâmico de SDTs próprios na remoção quando `objects.sdts.own` estiver ausente, e correção do falso positivo de conflito SDT no Sync hierárquico.

## Decisões de recorte

- Releitura hierárquica só quando a metadata V2 contém `levels` com subníveis (`HasHierarchicalLevels`); API plana permanece inalterada.
- Preferências: JSON legado **sem** `schemaVersion` continua válido (mesma política V1/V2 da metadata).
- Inventário dinâmico: se `objects.sdts.own` estiver ausente mas houver `levels`, reconstruir a ordem de remoção via stub `ApiPlan` + `ApiPlanSdtGenerationPlanBuilder` (inverso da pós-ordem de criação); fallback flat nos cinco nomes fixos quando não houver hierarquia.
- Sync: `DetectSdtConflicts` retorna vazio quando a metadata é hierárquica — o residual B099b (membros flat vs SDTs raiz) deixa de bloquear o preview.
- Corte `0.1.0-alpha.5` permanece **após** autorização humana explícita (AGENTS.md).

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

- Inventário dinâmico depende de stub mínimo: metadata hierárquica incompleta (SDTs ausentes ou `levels` inválidos) cai no fallback flat.
- Corte `0.1.0-alpha.5` exige autorização humana explícita (notas trilíngues + dois assets DLL).
- Critérios 6 (`Gx_FabricaBrasil`) e 10 (smoke U13) continuam fora desta fase, gates da sprint.

## Conclusão

Fase 7 encerrada em 2026-08-28: código, testes offline, gate `tests.wizardLifecycle` e smoke IDE (apply, critério 8, Sync, Remover preview). Próximos passos operacionais: corte `0.1.0-alpha.5` (com autorização) e `B100` (Delete).
