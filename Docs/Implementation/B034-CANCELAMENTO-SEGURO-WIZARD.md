# B034 - Cancelamento Seguro no Wizard

Concluido no GeneXus 18 Upgrade 15 em 2026-07-22: o wizard unico aberto por `Abrir Wizard (B030)` validou cancelamento seguro em pontos de saida distintos, descartando `Transaction` e decisoes mantidas em memoria, sem criar `ApiPlan`, sem persistencia e sem escrita na KB.

## Objetivo

Implementar e validar o comportamento de cancelamento seguro do prototipo navegavel do wizard unico, garantindo que decisoes acumuladas em memoria sejam descartadas quando o usuario aborta o fluxo antes da conclusao.

## Escopo implementado

- o fechamento implicito da janela do wizard unico, incluindo `X` e Alt+F4, passa a ser tratado como cancelamento;
- o cancelamento pelo botao `Cancelar` e pela tecla Esc descarta a selecao consolidada do wizard;
- `Voltar` no inicio do wizard descarta a `Transaction` selecionada e as decisoes em memoria;
- cancelar o seletor nativo de `Transaction` tambem limpa estado anterior do wizard;
- falhas de resolucao de `Transaction`, ausencia de dialogo publico de selecao ou ausencia de modulo limpam o estado anterior do wizard;
- o estado unificado `PrototypeWizardFlowSessionState` e sempre limpo nas saidas de cancelamento;
- os estados historicos `PrototypeWizardSessionState` e `PrototypeWizardReviewSessionState` tambem sao limpos, preservando compatibilidade interna com as frentes anteriores;
- `PrototypeTransactionSelectionState` e limpo quando o fluxo e cancelado, abortado ou fechado sem conclusao;
- o fluxo concluido sem cancelamento continua guardando as decisoes apenas em memoria;
- nenhuma extensao do escopo cria, altera ou exclui objetos GeneXus.

## Implementacao

`Src/Extension/PrototypeWizardDialog.cs` sobrescreve `OnFormClosing` para converter fechamento sem `DialogResult` explicito em `DialogResult.Cancel`, garantindo que fechamento pela janela siga o mesmo caminho seguro do cancelamento.

`Src/Extension/Package.cs` centraliza a limpeza em `ClearPrototypeWizardMemory`, limpando o estado unificado, os estados historicos de contrato/revisao e, quando aplicavel, a `Transaction` selecionada. As mensagens da Output passaram a identificar B034 nos caminhos de descarte e no fluxo concluido sem acionar cancelamento.

## Validacao local

- `dotnet build Src\GenexusOpenApiBuilder.sln -c Release`: OK, com avisos NU1900 de consulta de vulnerabilidade bloqueada por rede/sandbox, sem erros de compilacao;
- `pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1`: OK, com 8 comandos registrados e sincronizados.

## Validacao manual no U15

Validacao manual concluida no U15 usando o wizard unico por `Abrir Wizard (B030)`. Foram exercitados cancelamento no seletor nativo, cancelamento/fechamento do wizard apos escolha de `Transaction`, `Voltar` no inicio do fluxo e conclusao normal sem cancelamento.

A Output confirmou o descarte seguro nos cenarios de cancelamento:

```text
[Genexus Open API Builder][B034] Nenhuma Transaction foi selecionada. Estado anterior do wizard descartado; nenhum ApiPlan foi criado e nenhuma alteracao foi feita na KB.
[Genexus Open API Builder][B034] Wizard unico cancelado ou fechado para Transaction='Contrato'. Transaction e decisoes em memoria descartadas; nenhum ApiPlan foi criado e nenhuma alteracao foi feita na KB.
[Genexus Open API Builder][B034] Wizard unico cancelado ou fechado para Transaction='Escola'. Transaction e decisoes em memoria descartadas; nenhum ApiPlan foi criado e nenhuma alteracao foi feita na KB.
[Genexus Open API Builder][B034] Wizard unico cancelado ou fechado para Transaction='Contrato'. Transaction e decisoes em memoria descartadas; nenhum ApiPlan foi criado e nenhuma alteracao foi feita na KB.
[Genexus Open API Builder][B034] Voltar acionado no inicio do wizard unico. Transaction='Escola' e decisoes em memoria foram descartadas; nenhum ApiPlan foi criado e nenhuma alteracao foi feita na KB.
```

A conclusao normal tambem foi validada, mantendo decisoes somente em memoria:

```text
[Genexus Open API Builder][B030] Wizard unico concluido em memoria: Transaction='Contrato', Module='Root Module', SelectionSource='Contexto'.
[Genexus Open API Builder][B031] Contrato em memoria: Services='List,Get,Create,Update', Create=1, Update=1, Response=2, ListFilters=1.
[Genexus Open API Builder][B032] Paths e seguranca em memoria: ApiName='apiContrato', ServicesBasePath='apiContrato', RestPath='/contrato', SecurityLevel='Authentication'.
[Genexus Open API Builder][B033] Obrigatoriedade em memoria: CreateRequired=0, UpdateRequired=1. Required significa presenca do membro JSON, nao valor nao-vazio.
[Genexus Open API Builder][B034] Wizard concluido sem acionar cancelamento. Decisoes permanecem somente em memoria; nenhum ApiPlan foi criado e nenhum objeto foi criado, alterado ou excluido.
```

## Resultado

Criterio atendido em 2026-07-22: B034 comprovou descarte completo das decisoes em memoria nos pontos de saida do wizard unico e confirmou que a conclusao normal preserva apenas memoria de sessao, sem `ApiPlan`, sem persistencia e sem escrita na KB.
