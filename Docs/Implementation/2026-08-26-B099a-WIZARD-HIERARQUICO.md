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
- Profundidade máxima > 4: aviso, sem bloquear.
- Apply de objetos hierárquicos é permitido; metadata/sync/remoção continuam V1 até `B099b`. O resumo avisa para não usar Remover nem Sync nesta API.
- Required de linha aparece na UI e não alimenta o writer BC (400 com caminho `Parcelas[0].Campo` permanece para frente posterior, para não recapturar o ouro B097).
- Seleção Create/Update/Response por subnível é preservada na poda (`Selected*FieldNames`) e consumida pelo plano de SDT / mapa BC; `Fields` fica como união estrutural.
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

- Teste offline: dependência pai/filho, nível vazio omitido, `IncludeListCount=false`, aviso em profundidade 5, transação sem filhos selecionados; combinações Create-only / Update-only / Create+Update / papéis com campos distintos; colisão de `VariableToken` no alocador e fixture end-to-end `VariableTokenCollision` (`L1_SameLeaf` / `L1_SameLeaf_V2` no mapa e no Source BC).
- 2026-08-27: removido o helper morto `SelectLevelFields` em `ApiPlanSdtGenerationPlanBuilder`. Não tinha chamadores; o filtro vigente permanece `SelectLevelFieldsForRole`. Sem mudança de contrato.
- Build Release canônico: 0 avisos, 0 erros.
- Build Release satélite U13: 0 erros (aviso MSB3277 ambiental conhecido).
- Linha de base Fase 0 e ouro B096 intactos.
- Manifesto/registro da extensão: inalterado. Atualização para teste na IDE = só a DLL canônica (`Install-ExtensionForGeneXus18.bat` como administrador, IDE fechada); sem `genexus /install`.

## Smoke U15 — 2026-08-26

KB `wsEducacaoSpTeste`, Transaction `Teste`, API `apiTeste`, folder `TesteOpenApi`. Sem Remover/Sync; metadata permanece `GOAB_API_METADATA_B060_V1`. Sem smoke HTTP multinível.

### Três níveis (`TesteItem` → `TesteItemFolio`)

Wizard e SDTs do Folio corretos; o primeiro apply bloqueou B055 em `&Bc_TesteItem_TesteItemFolio` com tipo `Teste.TesteItemFolio`. Correção: tipo BC aninhado usa o caminho completo (`Teste.TesteItem.TesteItemFolio`). Reapply: `SuccessWithWarnings`, `Blocked=0`, metadata reencontrada. `Build All` passou em `NETPostgreSQL155` e `NETFrameworkSQLServer004` (especificou Get/Create/Update/List e gerou os SDTs do Folio; sem `spc0018`; aviso ambiental `FBiTextSharp.dll` só no Framework). Relatório B081: a primeira chamada gravou Output sem diálogo (owner = Wizard oculto); a segunda mostrou o diálogo. Correção do owner em `ResolveFinalReportOwner`.

### Quatro níveis (`…Folio` → `TesteItemFolioDoc`, irmão `TestePortfolio`)

Wizard em complemento: seletor `Cabeçalho (Teste)`, `TesteItem`, `TesteItem / TesteItemFolio`, `TesteItem / TesteItemFolio / TesteItemFolioDoc`, `TestePortfolio`. Contador de List visível só nos filhos diretos (`TesteItem`, `TestePortfolio`); ausente em Folio e FolioDoc. Naquele apply o aviso ainda citava 3 níveis (`ValidatedDepth` da época); a geração não bloqueou. `ValidatedDepth` passou a 4 depois do `Build All`. Resumo: `Subníveis selecionados: 4`; SDTs `Completar: gerenciados=19, ausentes=3, planejados=22` (a pasta reutilizada entra na conta). Apply: diálogo B081 visível, `SuccessWithWarnings`, `Created=3` (`sdtTeste_API_{CreateRequest,UpdateRequest,Response}_TesteItem_TesteItemFolio_TesteItemFolioDoc`), `Updated=24`, `Blocked=0`, `DuraçãoMs=27243`. B071/B070 aplicados; metadata `Reencountered`, `Bytes=81932`, `Sha256='5EAB5D532CB9760C818C4A7F11BDB88B2EE5DF0E47E882256EE10D5FA0E364B6'`. O `PlannedContractHash` V1 ficou igual ao apply de três níveis (`21F955426F5A11630ECEBC05BA582D56F6B64CB8D6B8E157D6B97381A87F22CD`): o contrato planejado B067 ainda não inclui a árvore hierárquica (Fase 6). `Build All` passou nos dois environments: especificou `apiTeste` e as quatro Procedures, gerou os três SDTs de FolioDoc, sem `spc0018`; o `.cs` de `procTeste_API_List` não reapareceu na geração (List especificado e compile OK; incremental). Aviso `FBiTextSharp.dll` só no Framework.

## Ressalva de datação — 2026-08-27

O smoke acima é de 2026-08-26 e foi feito com a DLL daquele dia. O commit `8f80f39` (2026-08-27) mudou depois a poda por papel (`Selected*FieldNames`), o mapa BC, a desambiguação de `VariableToken` e o tratamento de falha na leitura hierárquica. A `apiTeste` que ficou na KB, portanto, **não** corresponde ao gerador atual: a evidência continua válida para o que provou naquela data (Wizard, apply, `Build All` sem `spc0018`), e não serve de base para medição de runtime posterior. Por isso a Fase 5-A (`B099v`) começa por reinstalar a DLL e reaplicar o Wizard antes do smoke HTTP.

## Validação ainda humana

- Wizard em Transaction plana: visual idêntico ao atual (não refeito neste smoke).
- HTTP multinível (GET/POST com linhas de Folio/FolioDoc) fora deste recorte.
