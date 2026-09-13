# S-B111 F2 — implementação local do seam de persistência e recibos

**Data:** 2026-09-13
**Status:** implementação local concluída; cenário positivo do `Wizard`, reteste da habilitação diferida de Business Component, cancelamento cooperativo durante o Apply, recomposição posterior da `Produto` e criação em escala da `Empresa` aceitos manualmente na IDE GeneXus, com a F2 ainda aberta para as fronteiras de falha controlada.
**Escopo:** F2 do plano `2026-09-04-B111-F2-PLANO-SEAM-E-RECIBOS.md`.

## Resultado

A F2 foi implementada localmente sem alterar o manifesto da extensão, a instalação do GeneXus ou a KB. O seam comum ficou separado em um núcleo SDK-free, com adaptador para os writers acoplados ao SDK, recibos ordenados por operação e confirmação delegada após cada `Save()` ou `Delete()` físico.

## Alterações principais

- `ApiPlanPersistenceCore` concentra escopo, sequência, tentativa, confirmação, estados `Confirmed`, `Failed`, `OutcomeUnknown` e registro de falha de etapa sem recibo.
- `ApiPlanPersistenceLog` expõe recibos e falhas de etapa no relatório final e na trilha de Output.
- Procedures, SDTs, Folder, API Object, File de metadata, recuperação B115, habilitação diferida de Business Component e remoções físicas passaram pelo adaptador comum.
- BC e List usam um executor único de etapas: preparação, operação física, confirmação por releitura e callback de conclusão.
- A habilitação de Business Component no Wizard permanece somente em memória até o preflight agregado; o `Transaction.Save()` ocorre depois do gate e recebe confirmação.
- O relatório final distingue tentativa, confirmação, ausência, divergência, alvo não tentado e falha de etapa.

## Validação local

Passaram:

- `Tests/PersistenceProbe/Test-ApiPlanPersistenceCore.ps1`
- `Tests/ApplicationFinalReport/Test-ApiPlanApplicationFinalReport.ps1`
- testes de preflight, BC/List, Folder, recuperação de metadata e remoção resiliente;
- `Tests/PrePushChecker/Test-OpenApiBuilderPrePushChecks.ps1`, incluindo a fixture de repositório limpo e o gate `tests.persistenceCore`;
- build Release sem restore, com 0 avisos e 0 erros.

Artefato gerado: `Src/Extension/bin/Release/net471/GenexusOpenApiBuilder.Extension.dll`.

## Aceite manual parcial na IDE

Em 2026-09-12, após a atualização manual da DLL pelo usuário, o cenário positivo do `Wizard`
para a `Transaction` `Laudo` foi executado na IDE. O plano usou `List`, `Get`, `Create` e
`Update`, sem `Delete`, com `Business Component` apto e `FinalWriter='List'`.

O relatório final registrou `SuccessWithWarnings`, `Criados=0`, `Atualizados=14`,
`Removidos=0`, `Bloqueados=0`, `ApiSaveCount=1`, `PersistenceReceipts=10` e
`PersistenceStageFailures=0`. Todos os recibos foram confirmados e presentes, sem retry e
sem exceção. O preflight agregado ocorreu antes da primeira gravação; os SDTs e a pasta
preexistente foram reencontrados sem gravação física.

O aceite e a leitura detalhada dos avisos estão em
`Docs/Implementation/2026-09-12-S-B111-F2-ACEITE-IDE.md`.

## Aceite manual adicional 4 — criação em KB grande

Em 2026-09-13, a `Transaction` `Empresa` foi aplicada na KB
`FabricaBrasil18Test` (GeneXus 18 U15), em criação nova com 13 subníveis e 48
SDTs ausentes. O relatório terminou com `SuccessWithWarnings`,
`FinalApiWriter='List'`, `ApiSaveCount=1`, `Criados=55`, `Atualizados=0`,
`Removidos=0`, `Bloqueados=0`, `PersistenceReceipts=59`,
`PersistenceStageFailures=0` e `DuraçãoMs=79041`. Todos os recibos ficaram
confirmados e presentes; o único aviso foi o fallback das descrições para
inglês.

O teste fica aceito como prova positiva de escala e persistência. A duração e
as fases (`SDTs=39942 ms`, `BusinessComponent=24298 ms`, `List=4625 ms`, entre
outras) ficam como medição atual. Não foi feita comparação estatística antes/
depois, pois as referências históricas usam DLL e estado de operação distintos.
O registro completo está em
`Docs/Implementation/2026-09-12-S-B111-F2-ACEITE-IDE.md`.

## Limites e próxima validação

O teste offline e os cenários manuais já aceitos não encerram a F2. O reteste na
`Produto` validou a habilitação diferida de Business Component e a criação de
SDTs dependentes; o teste adicional 3 confirmou o cancelamento cooperativo
durante o Apply, depois de seis persistências confirmadas e antes do API Object.
Antes do teste adicional 4, a `Produto` foi recomposta por um Apply completo.
Essa recomposição foi concluída com `SuccessWithWarnings`,
`ApiSaveCount=1`, `PersistenceReceipts=10`, `PersistenceStageFailures=0`,
`Criados=0`, `Atualizados=14` e `Bloqueados=0`. A criação em escala da
`Empresa` também foi aceita com 59 recibos confirmados e nenhum bloqueio.
Permanece o exercício da falha controlada de cada fronteira.
A instalação manual da DLL foi realizada pelo usuário, não pelo agente. A contagem `Atualizados=14` do cenário de
reencontro da `Laudo` contém a ressalva conhecida dos SDTs reencontrados sem
gravação; o ajuste fica para frente posterior. Diário durável,
recuperação/reconciliação e decisão de retry permanecem no escopo da F3.
