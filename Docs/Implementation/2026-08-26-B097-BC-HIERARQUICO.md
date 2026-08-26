# B097 — Source Business Component hierárquico (Fase 3)

Data: 2026-08-26.
Frente: Sprint 9 — somente Fase 3 (`B097`).
Escopo: emissão offline de Source `Get` / `Create` / `Update` com subníveis e marcador `<Subnível>Replace`. Sem Wizard, sem `List`/`ListResponse_Item`, sem metadata V2, sem smoke IDE multinível.

## Decisões de recorte

- Ramifica só quando `ApiPlanSdtHierarchicalNaming.HasSelectedSublevels`; caminho plano permanece byte a byte com a Fase 0.
- Mapa `ApiPlanHierarchicalContractMap` alinha nomes de coleção/`Replace`/SDT ao B096 (mesmo reserved e pós-ordem); o BC usa `LevelName` estrutural.
- **Get:** `For` nas coleções do BC → preenche `&GetResponse.<membro SDT>`.
- **Create:** `For` no request → `Add` no BC (recursivo) → `Save`.
- **Update:** cabeçalho sempre; coleção só se `<Subnível>Replace`; match-by-PK com remoção de omitidos; autonumerado → `Clear` + reinserção; pai novo ignora Replace dos filhos; `Replace` ausente/`False` não toca a coleção.
- Required aninhado com caminho/índice fica para quando o Wizard gravar Required por nível (B099); header Required permanece.
- Wizard flat não popula `Levels`; geração na IDE continua plana até B099.

## Entrega

| Peça | Caminho |
|---|---|
| Mapa | `Src/Domain/ApiPlanHierarchicalContractMap.cs` |
| Emissor Source | `Src/Extension/Diagnostics/ApiPlanBusinessComponentHierarchicalSource.cs` |
| Writer | `Src/Extension/Diagnostics/ApiPlanBusinessComponentWriter.cs` (ramo hierárquico) |
| Baseline | `Src/Extension/Diagnostics/ApiPlanBusinessComponentHierarchicalBaseline.cs` |
| Teste | `Tests/BusinessComponentHierarchical/Test-ApiPlanBusinessComponentHierarchical.ps1` |
| Ouro | `Tests/BusinessComponentHierarchical/Baselines/` |
| Gate | `scripts/Invoke-PrePushMechanicalChecks.ps1` → `tests.businessComponentHierarchical` |

Fixtures (reuso B096): `OneSublevel`, `ParallelSublevels`, `ThreeDeep`, `InheritedPrimaryKey`, `MemberCollision`, `HeaderOnly` (controle plano).

## Validação mecânica

- `dotnet build Src/GenexusOpenApiBuilder.sln --configuration Release` — 0 avisos, 0 erros.
- `dotnet build Src/GenexusOpenApiBuilder.Gx18u13.sln --configuration Release` — 0 erros (aviso MSB3277 conhecido da linha satélite).
- `pwsh -NoProfile -File Tests/BusinessComponentHierarchical/Test-ApiPlanBusinessComponentHierarchical.ps1` — `PASS`.
- `pwsh -NoProfile -File Tests/GenerationBaseline/Test-ApiPlanGenerationBaseline.ps1` — `PASS`.
- `pwsh -NoProfile -File Tests/SdtHierarchicalPlan/Test-ApiPlanSdtHierarchicalPlan.ps1` — `PASS`.

## Fora deste fechamento

- B098 (`List` + `ListResponse_Item` + contadores), B099 (Wizard/metadata), Fase 7, B100.
- Ligar o Wizard ao leitor hierárquico.
- Smoke IDE / HTTP multinível e comprovação de remoção de netos com a linha pai (gate da sprint, não pendência de spec).
- Required com caminho `Parcelas[0].Campo` e inventário dinâmico de remoção.
