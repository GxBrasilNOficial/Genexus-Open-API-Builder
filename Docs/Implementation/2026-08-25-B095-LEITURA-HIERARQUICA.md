# B095 — Leitura hierárquica da estrutura (Fase 1)

Data: 2026-08-25.
Frente: Sprint 9 — somente Fase 1 (`B095`).
Escopo: leitor à parte, modelo `ApiPlanLevel`, testes offline. Sem geração, Wizard, metadata nem B096+.

## Decisões de recorte (entrevista)

- Leitor **à parte** do caminho flat do Wizard.
- Três árvores de teste, com casos especiais embutidos (chave gerada, chave digitada, fórmula, só leitura).
- Nome do cabeçalho = nome que o GeneXus já dá ao nível raiz.
- Pai do cabeçalho = vazio.
- Teste ligado ao checker pré-push (`tests.transactionStructure`).
- Documentação no mesmo fechamento.

## Entrega

| Peça | Caminho |
|---|---|
| Modelo | `ApiPlanLevel`, `ApiPlanLevelField` e `ApiPlan.Levels` em `Src/Domain/ApiPlan.cs` |
| Leitor | `Src/Extension/Diagnostics/TransactionStructureReader.cs` |
| Teste | `Tests/TransactionStructure/Test-TransactionStructureReader.ps1` |
| Gate | `scripts/Invoke-PrePushMechanicalChecks.ps1` → `tests.transactionStructure` |

**Alinhamento pós-revisão pré-push (2026-08-25).** A tabela de fases em `2026-08-20-SUPORTE-TRANSACTIONS-SUBNIVEIS.md` ainda listava `PrototypeWizardContract.cs` e `PrototypePrimaryKeyReader.cs` como componentes da Fase 1. Isso divergia da entrega (leitor à parte). A tabela foi corrigida para nomear `TransactionStructureReader.cs` e declarar explicitamente que o caminho flat do Wizard e o leitor de PK de cabeçalho **não** foram alterados.

## Fixtures offline

- `OneSublevel` — cabeçalho + um subnível; PK de linha informada; fórmula; `NoAccept`.
- `ParallelSublevels` — dois subníveis irmãos; `NoAccept` em um campo.
- `ThreeDeep` — três níveis; PK autonumerada e fórmula no nível mais fundo.

## Validação mecânica

- `dotnet build Src/GenexusOpenApiBuilder.sln --configuration Release` — 0 avisos, 0 erros.
- `dotnet build Src/GenexusOpenApiBuilder.Gx18u13.sln --configuration Release` — 0 erros (aviso MSB3277 conhecido da linha satélite).
- `pwsh -NoProfile -File Tests/TransactionStructure/Test-TransactionStructureReader.ps1` — `PASS`.

## Fora deste fechamento

- B096 (SDTs), B097 (BC), B098 (List), B099 (Wizard/metadata), Fase 7, B100.
- Ligar `ApiPlanBuilder` / Wizard ao leitor hierárquico.
- Conferência XPZ de fim da Sprint 9 (Fase 0).
- Smoke U13 com Transaction multinível (critério do corte de subníveis).
