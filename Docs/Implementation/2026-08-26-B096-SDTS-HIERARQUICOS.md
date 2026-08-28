# B096 — Geração de SDTs hierárquicos (Fase 2)

Data: 2026-08-26.
Frente: Sprint 9 — somente Fase 2 (`B096`).
Escopo: plano de SDT por contrato e por subnível, regra de nomes, desambiguação e encurtamento. Sem Procedures, sem Wizard, sem metadata V2, sem `ListResponse_Item`.

## Decisões de recorte (entrevista)

- Builder ramifica só quando `ApiPlan.Levels` tem filhos; transação plana permanece no caminho dos cinco SDTs próprios.
- Neste recorte, todo subnível presente na árvore conta como selecionado. Desde B099a o Wizard poda `Levels`.
- Elegibilidade intra-subnível aplicada pelos flags B095: fórmula, `NoAccept`, inferido e redundante fora de Create/Update; PK autonumerada e PK herdada (`IsPrimaryKey` + `IsForeignKey`) fora do Create da linha.
- Marcador `<Subnível>Replace` no `UpdateRequest` do pai (nível 2 no corpo; nível 3 dentro do item do pai).
- `ListResponse.Items` continua coleção de `Response`. `ListResponse_Item` fica para B098.
- Neste recorte o Wizard flat não consumia o naming nem o plano hierárquico. Desde B099a o Wizard consome o naming via `Levels` podados.
- Limite de nome de objeto GeneXus 18: **128** caracteres (plataforma desde GX15). Confirmado offline nesta fase; escrita real na KB fica para smoke posterior, antes da primeira API multinível. O teto **não** se aplica a nomes de membro: coleção e `Replace` usam o identificador sanitizado do nível; o ouro `LongQualifier` congela 106 e 113 caracteres.

## O que o teste offline realmente cobre

| Camada | O que cobre offline | O que não cobre |
|---|---|---|
| `ApiPlanSdtGenerationPlanBuilder` + `Levels` | SDTs derivados por contrato, pós-ordem, Replace, elegibilidade, `Items` ainda tipado em `Response` | `SDTStructure` físico na IDE |
| `ApiPlanSdtHierarchicalNaming` | desambiguação estável, `<unnamed>` → `Level{n}`, encurtamento quando o nome completo estoura 128 ou colide (folha ≤32 tenta reusar a folha; senão hash de 8 hex; Transaction pode ser truncada), membros sem teto, colisão irresolúvel | objeto SDT gravado na KB; aceitação de membro longo na IDE |
| Ouro `Baselines/*.json` | forma serializada estável do plano | diff contra XPZ de cliente |
| Writer | inalterado; já cria coleção tipada por outro SDT na ordem de `OwnSdts` | smoke de `Save()` hierárquico |

## Entrega

| Peça | Caminho |
|---|---|
| Naming | `Src/Domain/ApiPlanSdtHierarchicalNaming.cs` |
| Plano | `Src/Domain/ApiPlanSdtGenerationPlan.cs` (ramo hierárquico; caminho plano intacto) |
| Fixtures | `Src/Extension/Diagnostics/ApiPlanSdtHierarchicalPlanBaseline.cs` |
| Teste | `Tests/SdtHierarchicalPlan/Test-ApiPlanSdtHierarchicalPlan.ps1` |
| Ouro | `Tests/SdtHierarchicalPlan/Baselines/*.json` |
| Gate | `scripts/Invoke-PrePushMechanicalChecks.ps1` → `tests.sdtHierarchicalPlan` |

Fixtures: `OneSublevel`, `ParallelSublevels`, `ThreeDeep`, `InheritedPrimaryKey` (árvores B095 via `Build`), mais `MemberCollision`, `LongQualifier` e `HeaderOnly`.

## Validação mecânica

- `dotnet build Src/GenexusOpenApiBuilder.sln --configuration Release` — 0 avisos, 0 erros.
- `dotnet build Src/GenexusOpenApiBuilder.Gx18u13.sln --configuration Release` — 0 erros (aviso MSB3277 conhecido da linha satélite).
- `pwsh -NoProfile -File Tests/SdtHierarchicalPlan/Test-ApiPlanSdtHierarchicalPlan.ps1` — `PASS`.
- `pwsh -NoProfile -File Tests/GenerationBaseline/Test-ApiPlanGenerationBaseline.ps1` — `PASS` (não regressão plana).

## Fora deste fechamento

- B098 (`List` + `ListResponse_Item`), B099 (Wizard/metadata), Fase 7, B100.
- Ligar o Wizard ao leitor hierárquico e à seleção por subnível.
- Smoke IDE com escrita de SDTs hierárquicos, conferência do limite 128 no objeto e aceitação dos nomes de membro (coleção e `Replace`) sem teto nesta fase.
- Inventário dinâmico de remoção (Fase 7): na época do B096 o B086 ainda lia a lista plana; **desde `B099b`/Fase 7** o inventário usa `objects.sdts.own` ou reconstrução a partir de `levels` — ver `Docs/Implementation/2026-08-28-FASE7-CICLO-VIDA-HIERARQUIA.md`.
