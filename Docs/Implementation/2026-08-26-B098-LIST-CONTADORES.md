# B098 — List com contadores e ListResponse_Item (Fase 4)

Data: 2026-08-26.
Frente: Sprint 9 — somente Fase 4 (`B098`).
Escopo: plano de SDT com `ListResponse_Item` condicionado, Source `List` com `count()` nos subníveis diretos, ouro offline e gate `tests.listHierarchical`. Sem Wizard, sem metadata V2, sem smoke IDE multinível.

## Decisões de recorte

- Ramifica só quando `ApiPlanSdtHierarchicalNaming.HasSelectedSublevels`; caminho plano permanece byte a byte com a Fase 0 (`Items` continua coleção de `Response`).
- Com subníveis: `ListResponse.Items` tipa `sdt<Tx>_API_ListResponse_Item` (cabeçalho sem coleções + `<Subnível>Count` dos filhos diretos com `IncludeListCount`).
- Contadores só em profundidade 2; neto não entra. `IncludeListCount` default `true` no modelo; UI de desligar fica em B099.
- Agregação nativa `&Item.<Count> = count(<1ª PK do filho>)` dentro do `For each` do cabeçalho; sem `For each` aninhado.
- Nomes de contador alinhados ao reserved/desambiguação do plano de SDT via `ApiPlanListHierarchicalContractBuilder`.
- Wizard flat não popula `Levels`; geração na IDE continua plana até B099.

## Entrega

| Peça | Caminho |
|---|---|
| Flag | `ApiPlanLevel.IncludeListCount` |
| Naming | `AllocateCountMemberName`, `ListResponseItemNamePattern` |
| Contrato List | `Src/Domain/ApiPlanListHierarchicalContract.cs` |
| Plano SDT | `ApiPlanSdtGenerationPlan.cs` (`ListResponse_Item` + Items tipado) |
| Source List | `ApiPlanListProcedureWriter.cs` |
| Baseline | `ApiPlanListHierarchicalBaseline.cs` |
| Teste | `Tests/ListHierarchical/Test-ApiPlanListHierarchical.ps1` |
| Ouro | `Tests/ListHierarchical/Baselines/` (+ ouro B096 atualizado) |
| Gate | `tests.listHierarchical` + trava OpenAPI `_API_ListResponse_Item` |

Fixtures: reuso B096/B097 + `CountsDisabled` (mesmo `OneSublevel` com contadores desligados; ainda emite `ListResponse_Item`).

## Validação mecânica

- `dotnet build Src/GenexusOpenApiBuilder.sln --configuration Release` — 0 avisos, 0 erros.
- `dotnet build Src/GenexusOpenApiBuilder.Gx18u13.sln --configuration Release` — 0 erros (aviso MSB3277 conhecido da linha satélite).
- `pwsh -NoProfile -File Tests/ListHierarchical/Test-ApiPlanListHierarchical.ps1` — `PASS`.
- `pwsh -NoProfile -File Tests/SdtHierarchicalPlan/Test-ApiPlanSdtHierarchicalPlan.ps1` — `PASS`.
- `pwsh -NoProfile -File Tests/BusinessComponentHierarchical/Test-ApiPlanBusinessComponentHierarchical.ps1` — `PASS`.
- `pwsh -NoProfile -File Tests/GenerationBaseline/Test-ApiPlanGenerationBaseline.ps1` — `PASS`.
- `pwsh -NoProfile -File Tests/OpenApiContract/Test-OpenApiClientContractValidity.ps1` — `PASS`.

## Fora deste fechamento

- B099 (Wizard/metadata), Fase 7, B100.
- Ligar o Wizard ao leitor hierárquico e ao controle de contador.
- Smoke IDE / HTTP / YAML multinível (gate da sprint ao fim da Fase 4 no plano original = conferência pontual; esta entrega é só offline).
- Dívidas adiadas do B097 (PK autonumerada no Update; `<unnamed>` no BC).
