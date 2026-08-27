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
- Required aninhado com caminho/índice: B099a expõe required de linha na UI; o writer BC ainda valida só o cabeçalho (não recapturar ouro desta frente).
- Neste recorte o Wizard ainda não populava `Levels`. Desde B099a o Wizard poda `Levels` e o apply hierárquico é permitido.

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

- B099 (Wizard/metadata), Fase 7, B100.
- Ligar o Wizard ao leitor hierárquico.
- Smoke IDE / HTTP multinível e comprovação de remoção de netos com a linha pai (gate da sprint, não pendência de spec).
- Required com caminho `Parcelas[0].Campo` e inventário dinâmico de remoção.

## Dívidas conscientemente adiadas (2026-08-26)

Registradas no checkpoint; **não** bloqueiam B098 nem alteram o contrato do List.

1. **Update + PK autonumerada de linha.** Com `Replace` e chave autonumerada, o Source faz `Clear`+Add e ainda atribui a PK a partir do request; o Create omite o campo. Resolver mais tarde: omitir a PK no Update como no Create (ajuste local de Source) e, se desejado, alinhar o `UpdateRequest` do B096 para não publicar o campo. Confirmar com smoke IDE.
2. **`BcCollectionName` `<unnamed>`.** A fixture `InheritedPrimaryKey` (nível sem nome no B095) gera `&Header.<unnamed>` no BC e `Level1` no SDT. Em Transaction real o nível tem nome. Resolver mais tarde: recusar emissão BC sem nome de nível e/ou renomear a fixture de Source; manter o teste de `<unnamed>` só no leitor/naming. Quem consumir o mapa (ex.: contadores B098) não deve tratar `<unnamed>` como nome de produção.
