# B037 - Obrigatorio no Payload no Wizard

Concluido no GeneXus 18 Upgrade 15 em 2026-07-23: o wizard unico aberto por `Abrir Wizard (B030)` consolidou a leitura de `Obrigatorio no payload` para `CreateRequest` e `UpdateRequest`, distinguindo presenca do membro JSON de valor nao vazio.

## Objetivo

Revisar e consolidar no wizard unico a decisao de obrigatoriedade tecnica no payload para `CreateRequest` e `UpdateRequest`, mantendo todas as escolhas apenas em memoria e sem criar `ApiPlan` ou objetos de API.

## Escopo implementado

- a aba `Obrigatorios` passou a separar visualmente `CreateRequest` e `UpdateRequest`;
- cada decisao mostra campo, valor de `Required` e motivo legivel;
- `CreateRequest` mantem campos nullable como opcionais no payload;
- `CreateRequest` mantem campos sensiveis selecionados como opcionais no prototipo;
- `CreateRequest` marca como `Required=True` campos selecionados sem nulabilidade conhecida;
- `UpdateRequest` marca todo membro selecionado como `Required=True`, seguindo a regra de PUT completo;
- a UI e o resumo indicam que `Required` significa presenca do membro JSON, nao valor nao vazio;
- vazio, `false` e `0` continuam tratados como valores enviados e sujeitos ao BC;
- nenhuma escolha foi persistida e nenhum objeto foi criado, alterado ou excluido pela geracao.

## Implementacao

`Src/Extension/PrototypeWizardDialog.cs` substitui o texto unico da aba `Obrigatorios` por duas areas read-only, uma para `CreateRequest` e outra para `UpdateRequest`. As decisoes continuam sendo calculadas a partir de `PrototypeWizardRequiredFieldDecision` e armazenadas somente em `PrototypeWizardFlowSelection` quando o usuario conclui o wizard.

`Src/Extension/Package.cs` adiciona a evidencia B037 na janela Output ao concluir o wizard, com as contagens de `CreateRequired` e `UpdateRequired` e a declaracao explicita de que `Required` representa presenca do membro JSON.

## Validacao local

- `dotnet build Src\GenexusOpenApiBuilder.sln --configuration Release`: OK, 0 erros; avisos `NU1900` ocorreram por indisponibilidade de consulta de vulnerabilidades nos feeds NuGet;
- `pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1`: OK, com 8 comandos registrados e sincronizados;
- `git diff --check`: OK.

## Validacao manual no U15

Validacao funcional concluida no GeneXus 18 U15 pelo wizard unico por `Abrir Wizard (B030)`, usando `Transaction='Contrato'` pelo contexto.

- a aba `Obrigatorios` exibiu `CreateRequest` e `UpdateRequest` em areas separadas;
- `ContratoProcessoDeCompraNumero` apareceu com `Required=False` no `CreateRequest` por nullable;
- `ContratoProcessoDeCompraNumero` apareceu com `Required=True` no `UpdateRequest`, seguindo PUT completo;
- o resumo `B037` deixou claro que `Required` significa presenca do membro JSON, nao valor nao vazio;
- a Output incluiu `[B037] Obrigatorio no payload consolidado: CreateRequired=0, UpdateRequired=1`;
- a sequencia visual das abas foi preservada com `TabControl` nativo e o cabecalho passou a indicar `Aba atual`;
- nenhuma escolha foi persistida, nenhum `ApiPlan` foi criado e nenhum objeto de API foi gerado.

## Resultado

Criterio atendido em 2026-07-23: B037 consolidou a obrigatoriedade tecnica no payload no wizard unico, registrou a decisao em memoria e deixou as Fases 1 e 2 do prototipo navegavel prontas para encerramento, sem `ApiPlan`, sem persistencia das escolhas e sem geracao de objetos de API.
