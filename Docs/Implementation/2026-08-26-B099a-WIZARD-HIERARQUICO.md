# B099a — Wizard hierárquico (Fase 5)

Data: 2026-08-26.
Frente: Sprint 9 — somente Fase 5 (`B099a`).
Escopo: interface do Wizard com agrupamento por nível, dependência pai/filho, contador de List desligável e aviso de profundidade; `ApiPlan.Levels` podado a partir da seleção. Sem metadata V2, sem sync, sem remoção, sem smoke HTTP multinível.

## Decisões de recorte

- Transação plana (ou hierárquica com todos os subníveis desmarcados): `Levels` vazio, `HasSelectedSublevels == false`, linha de base da Fase 0 intacta.
- Cabeçalho permanece nas listas flat (`PrototypeWizardContractReader`); subnível usa listas próprias (`_levelCreateFieldsList` etc.).
- Seletor compartilhado (ComboBox com caminho `Shift / Worker`), não abas internas.
- Marcar um neto inclui os ancestrais; desmarcar o pai desmarca os descendentes.
- Subnível sem nenhum atributo marcado e sem filhos sobreviventes não entra na poda.
- Contador de List só no filho direto (`Depth == 2`), ligado por padrão.
- Profundidade máxima > 3: aviso, sem bloquear.
- Apply de objetos hierárquicos é permitido; metadata/sync/remoção continuam V1 até `B099b`. O resumo avisa para não usar Remover nem Sync nesta API.
- Required de linha aparece na UI e não alimenta o writer BC (400 com caminho `Parcelas[0].Campo` permanece para frente posterior, para não recapturar o ouro B097).
- Dívida `count(HeaderId)` em `InheritedPrimaryKey` permanece: não bloqueia esta UI; bloqueia confiança no smoke HTTP/List.

## Entrega

| Peça | Caminho |
|---|---|
| Seleção / poda | `Src/Domain/ApiPlanHierarchicalWizardSelection.cs` |
| Wizard | `Src/Extension/PrototypeWizardDialog.cs` |
| Builder | `ApiPlanBuilder.ResolveHierarchicalLevels` |
| Leitor | `TransactionStructureReader.Read` no load do Wizard; `CreateFourDeepFixture` fora do ouro B095 |
| Teste | `Tests/WizardHierarchical/Test-ApiPlanHierarchicalWizardSelection.ps1` |
| Gate | `tests.wizardHierarchical` |

## Validação desta frente

- Teste offline: dependência pai/filho, nível vazio omitido, `IncludeListCount=false`, aviso em profundidade 4, transação sem filhos selecionados.
- Build Release canônico: 0 avisos, 0 erros.
- Build Release satélite U13: 0 erros (aviso MSB3277 ambiental conhecido).
- Linha de base Fase 0 e ouro B096 intactos.
- Manifesto/registro da extensão: inalterado. Atualização para teste na IDE = só a DLL canônica (`Install-ExtensionForGeneXus18.bat` como administrador, IDE fechada); sem `genexus /install`.

## Validação ainda humana

- Wizard em Transaction plana: visual idêntico ao atual.
- Wizard em Transaction com subníveis: seletor, dependência, contador, aviso se depth>3, resumo com contagem e aviso V1.
- Apply opcional + `Build All` sem `spc0018`.
- Não Remover, não Sync, não ida-e-volta da árvore até B099b.

Pré-requisito: Transaction multinível na KB `wsEducacaoSpTeste` (um subnível, paralelos, três níveis) se ainda não existir.
