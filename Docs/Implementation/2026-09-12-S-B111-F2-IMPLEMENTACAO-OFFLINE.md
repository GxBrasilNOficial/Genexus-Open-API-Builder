# S-B111 F2 — implementação local do seam de persistência e recibos

**Data:** 2026-09-12
**Status:** implementação local concluída; cenário positivo do `Wizard` aceito manualmente na IDE GeneXus, com a F2 ainda aberta para as fronteiras de falha e a habilitação diferida de Business Component.
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

## Limites e próxima validação

O cenário positivo agora tem evidência manual na IDE, mas o teste offline e esse cenário isolado não encerram a F2. Falta repetir os fluxos aplicáveis da matriz da F1, exercitar a falha controlada de cada fronteira e validar a habilitação diferida de Business Component. A instalação manual da DLL foi realizada pelo usuário, não pelo agente. A contagem `Atualizados=14` contém a ressalva conhecida dos SDTs reencontrados sem gravação; o ajuste fica para frente posterior. Diário durável, recuperação/reconciliação e decisão de retry permanecem no escopo da F3.
