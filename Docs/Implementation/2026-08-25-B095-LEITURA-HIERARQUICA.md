# B095 — Leitura hierárquica da estrutura (Fase 1)

Data: 2026-08-25.
Frente: Sprint 9 — somente Fase 1 (`B095`).
Escopo: leitor à parte, modelo `ApiPlanLevel`, núcleo recursivo testável offline. Sem geração, metadata nem B096+.

## Decisões de recorte (entrevista)

- Leitor **à parte** do caminho flat do Wizard (a árvore hierárquica não entra no Wizard flat).
- Três árvores de teste iniciais, depois uma quarta (`InheritedPrimaryKey`), com casos especiais embutidos.
- Nome do cabeçalho = nome que o GeneXus já dá ao nível raiz.
- Pai do cabeçalho = vazio.
- Teste ligado ao checker pré-push (`tests.transactionStructure`).
- Documentação no mesmo fechamento.

## O que o teste offline realmente cobre

Revisão pós-crítica: a primeira entrega montava `ApiPlanLevel` à mão nas fixtures e só releia as constantes — falso verde para “leitura hierárquica recursiva”. A correção **melhorou o código**, não afrouxou a claim:

| Camada | O que cobre offline | O que não cobre |
|---|---|---|
| `TransactionStructureLevelSource` + `Build` / `ReadLevel` | Travessia recursiva, ordem de PK, `<unnamed>`, fórmula, `NoAccept`, autonumeração via `IsAutonumberCore` | Objeto `Transaction` real da IDE |
| `TransactionAttributeKeyTraits.IsAutonumberCore` | Critério puro (PK composta, True/False, fail-open) | Propriedade SDK em Attribute vivo |
| Ouro `SerializeSnapshot` ↔ `Baselines/*.json` | Forma serializada estável da árvore | Diff contra XPZ de cliente |
| Adaptador `MapLevel` / `Read(Transaction)` | Existe no código e é o caminho IDE | Exercício com Transaction real (smoke futuro) |

## Entrega

| Peça | Caminho |
|---|---|
| Modelo | `ApiPlanLevel`, `ApiPlanLevelField` e `ApiPlan.Levels` em `Src/Domain/ApiPlan.cs` |
| Leitor | `Src/Extension/Diagnostics/TransactionStructureReader.cs` (`Build` + adaptador SDK) |
| Critério compartilhado | `Src/Extension/Diagnostics/TransactionAttributeKeyTraits.cs` (Wizard flat e B095) |
| Teste | `Tests/TransactionStructure/Test-TransactionStructureReader.ps1` |
| Ouro | `Tests/TransactionStructure/Baselines/*.json` |
| Gate | `scripts/Invoke-PrePushMechanicalChecks.ps1` → `tests.transactionStructure` |

**`ApiPlanLevel.Fields`.** São candidatos da estrutura do nível (todos os atributos lidos), não a seleção Create/Update/Response do Wizard. Seleção por contrato = B099+.

**Wizard flat.** O caminho de leitura plana da Transaction **não** passa a usar `TransactionStructureReader`. Houve apenas extração do critério de autonumeração para o helper compartilhado (antes duplicado), para o Wizard e o B095 não divergirem.

## Fixtures offline (todas passam por `Build`)

- `OneSublevel` — cabeçalho + um subnível; PK de linha com `Autonumber=False`; fórmula; `NoAccept`.
- `ParallelSublevels` — dois subníveis irmãos; `NoAccept` em um campo.
- `ThreeDeep` — três níveis; PK com `Autonumber=True` e fórmula no nível mais fundo.
- `InheritedPrimaryKey` — PK composta na ordem declarada; nível sem nome → `<unnamed>`; autonumeração negada por contagem de partes.

## Validação mecânica

- `dotnet build Src/GenexusOpenApiBuilder.sln --configuration Release` — 0 avisos, 0 erros.
- `dotnet build Src/GenexusOpenApiBuilder.Gx18u13.sln --configuration Release` — 0 erros (aviso MSB3277 conhecido da linha satélite).
- `pwsh -NoProfile -File Tests/TransactionStructure/Test-TransactionStructureReader.ps1` — `PASS`.
- `pwsh -NoProfile -File Tests/WizardContract/Test-PrototypeWizardAutonumberCompositeKey.ps1` — `PASS`.

## Fora deste fechamento

- B096 (SDTs), B097 (BC), B098 (List), B099 (Wizard/metadata), Fase 7, B100.
- Ligar `ApiPlanBuilder` / Wizard ao leitor hierárquico.
- Smoke IDE com `Read(Transaction)` em Transaction multinível real.
- Conferência XPZ de fim da Sprint 9 (Fase 0).
