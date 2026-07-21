# B033 - Validar Campos Obrigatorios no Wizard

Concluido no GeneXus 18 Upgrade 15 em 2026-07-21: a validacao prototipica de campos obrigatorios foi absorvida pelo wizard unico aberto por `Abrir Wizard (B030)`. B031, B032 e B033 deixam de exigir comandos independentes no menu e passam a ser paginas sequenciais do mesmo fluxo modal, mantendo decisoes apenas em memoria e sem escrita na KB.

## Objetivo

Implementar a validacao navegavel de obrigatoriedade de membros JSON a partir da `Transaction` selecionada e das escolhas de contrato, paths, seguranca, paginacao e ordenacao acumuladas no wizard unico.

## Escopo implementado

- o unico comando operacional do wizard e `Genexus Open API Builder > Abrir Wizard (B030)`;
- o fluxo seleciona a `Transaction` pelo contexto ou pelo seletor nativo e abre uma unica janela WinForms sequencial;
- a janela unificada percorre `Servicos`, `Requests`, `Response`, `Filtros List`, `Paths`, `Seguranca`, `Paginacao`, `Ordenacao`, `Obrigatorios` e `Resumo B034`;
- B031, B032 e B033 continuam identificados nas mensagens da Output, mas nao sao comandos separados no menu;
- `CreateRequest` calcula `Required` por campo selecionado, separando obrigatoriedade de presenca do membro JSON de valor nao vazio;
- campos sensiveis selecionados no prototipo permanecem opcionais no `CreateRequest`;
- campos nullable selecionados no prototipo permanecem opcionais no `CreateRequest`;
- campos selecionados sem nulabilidade conhecida ficam obrigatorios no `CreateRequest`;
- `UpdateRequest` marca todo membro selecionado como obrigatorio, seguindo a decisao funcional de PUT completo;
- o resultado e guardado em `PrototypeWizardFlowSessionState` somente quando o usuario conclui o resumo;
- as selecoes historicas `PrototypeWizardSessionState` e `PrototypeWizardReviewSessionState` tambem sao preenchidas ao concluir, preservando compatibilidade interna para proximas frentes;
- `Voltar`, `Cancelar` e fechamento sem conclusao descartam a selecao consolidada conforme o ponto de saida;
- nenhum `ApiPlan` definitivo e criado;
- nenhum objeto GeneXus e criado, alterado ou excluido pela extensao.

## Implementacao

`Src/Extension/PrototypeWizardDialog.cs` implementa o fluxo unico do wizard e consolida as escolhas em `PrototypeWizardFlowSelection`, incluindo contrato, revisao de paths/seguranca e decisoes de obrigatoriedade.

`Src/Extension/Package.cs` mantem apenas o comando `Abrir Wizard (B030)` como entrada operacional do wizard. Ao concluir o dialogo, ele registra em memoria os estados B031, B032 e B033 e escreve as evidencias correspondentes na Output.

`Src/Extension/GenexusOpenApiBuilder.package` mantem apenas o `CommandDefinition` e o `Command refid` de `Abrir Wizard (B030)` para o wizard, evitando tres chamadas manuais para partes do mesmo fluxo.

## Validacao local

- `dotnet build Src\GenexusOpenApiBuilder.sln -c Release`: OK, com avisos NU1900 de consulta de vulnerabilidade bloqueada por rede/sandbox, sem erros de compilacao;
- `pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1`: OK, com 8 comandos registrados e sincronizados.

## Validacao manual pendente

A DLL ainda precisa ser instalada manualmente no GeneXus 18 Upgrade 15 para validar visualmente o wizard unico, confirmar a remocao operacional dos comandos B031/B032 do menu e conferir a aba `Obrigatorios` com uma `Transaction` real.

## Resultado

Criterio atendido em 2026-07-21: o wizard passa a ser uma unica chamada operacional, inclui a novidade B033 no mesmo fluxo, registra a obrigatoriedade em memoria e deixa o prototipo pronto para B034, sem `ApiPlan`, sem persistencia e sem escrita na KB.
