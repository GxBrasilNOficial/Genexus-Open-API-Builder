# B032 - Revisar Paths e Seguranca no Wizard

Concluido no GeneXus 18 Upgrade 15: a extensao abriu o terceiro passo do prototipo navegavel do wizard a partir do menu de contexto de uma `Transaction`, acionou B031 automaticamente quando o contrato ainda nao estava em memoria, revisou paths, seguranca, paginacao e ordenacao, manteve as decisoes somente em memoria e nao realizou persistencia nem escrita na KB.

## Objetivo

Implementar o Passo 3 do wizard para revisar seguranca, paginacao, ordenacao, `Services base path` e `RestPath`, partindo da `Transaction` selecionada e das decisoes acumuladas por B031, preservando o contrato de prototipo navegavel sem criacao, alteracao ou exclusao de objetos.

## Escopo validado

- o comando e acionado por `Genexus Open API Builder > Revisar Paths e Segurança (B032)` no menu de contexto da `Transaction`;
- o comando revalida a `Transaction` do contexto na KB ativa antes de abrir o fluxo;
- quando o contrato B031 esta ausente ou incompativel com a `Transaction`, B032 abre B031 automaticamente;
- B031 mantem sua navegacao sequencial por `Servicos`, `Requests`, `Response`, `Filtros List` e `Resumo B032`;
- apos fechar B031, B032 abre o Passo 3 sem exigir outro comando manual;
- a janela B032 apresenta `ApiName`, `Services base path`, `RestPath`, paths por servico, `Security Level`, `Default Page Size`, `Maximum Page Size` e ordenacao estatica;
- `ApiName` e `Services base path` iniciam como `api<NomeDaTransacao>`;
- enquanto nao editado manualmente, `Services base path` acompanha mudancas em `ApiName`; apos edicao manual, o valor manual e preservado;
- `RestPath` inicia com o nome da `Transaction` em minusculas e hifenizado, sem pluralizacao automatica;
- `List` e `Create` usam o caminho comum; `Get` e `Update` acrescentam a chave completa ao `RestPath`;
- `Security Level` inicia como `Authentication`;
- `Default Page Size` inicia como `50` e `Maximum Page Size` inicia como `200`;
- ordenacao inicial usa a chave primaria completa ascendente;
- `Voltar` em B032 reabre B031 sem persistir escolhas;
- `Cancelar` descarta a sessao do wizard em memoria;
- `Fechar`, no resumo, conclui B032 em memoria e habilita a continuidade para B033;
- nenhum `ApiPlan` definitivo e criado;
- nenhum objeto GeneXus e criado, alterado ou excluido pela extensao.

## Implementacao

`Src/Extension/Package.cs` registra o comando B032, resolve a KB ativa, revalida a `Transaction` do menu de contexto e orquestra a chamada automatica a B031 quando o contrato em memoria nao existe ou pertence a outra `Transaction`.

`Src/Extension/Diagnostics/PrototypeWizardReview.cs` monta o snapshot somente leitura do Passo 3: defaults de `ApiName`, `Services base path`, `RestPath`, seguranca, paginacao, paths por servico e ordenacao estatica. Esse snapshot e deliberadamente transitorio e nao e `ApiPlan`.

`Src/Extension/PrototypeWizardReviewDialog.cs` implementa a janela modal WinForms do Passo 3. A navegacao e sequencial e acumula as decisoes em `PrototypeWizardReviewSessionState` apenas quando o usuario conclui o resumo. O dialogo sincroniza `Services base path` com `ApiName` somente ate a primeira edicao manual do base path.

## Evidencia manual no U15

A navegacao visual foi validada manualmente no U15 com `Transaction='Escola'`, acionada pelo menu de contexto da propria `Transaction`. O fluxo confirmou B032 chamando B031 automaticamente e depois abrindo o Passo 3:

```text
[Genexus Open API Builder][B032] Transaction resolvida para o wizard: Name='Escola', Module='Root Module', SelectionSource='Contexto'.
[Genexus Open API Builder][B032] Contrato B031 ausente ou incompativel para Transaction='Escola'. Abrindo B031 automaticamente.
[Genexus Open API Builder][B031] Wizard Passo 2 concluido em memoria durante o fluxo B032: Transaction='Escola', Services='List,Get,Create,Update'.
[Genexus Open API Builder][B031] Campos selecionados: Create=20, Update=20, Response=23, ListFilters=3.
[Genexus Open API Builder][B032] Wizard Passo 3 concluido em memoria: Transaction='Escola', ApiName='apiEscola', ServicesBasePath='apiEscola', RestPath='/escola', SecurityLevel='Authentication'.
[Genexus Open API Builder][B032] Paginacao e ordenacao: DefaultPageSize=50, MaximumPageSize=200, StaticOrder='EscolaCodigo ASC'.
[Genexus Open API Builder][B032] Proximo passo habilitado para B033. Nenhum ApiPlan foi criado, nenhuma escolha foi persistida e nenhum objeto foi criado, alterado ou excluido.
```

A validacao visual confirmou as abas `Paths`, `Seguranca`, `Paginacao`, `Ordenacao` e `Resumo B033`, incluindo os paths `List/Get/Create/Update`, `Security Level=Authentication`, paginacao `50/200`, ordenacao `EscolaCodigo ASC` e resumo final sem persistencia.

A validacao complementar pos-correcao confirmou que `Services base path` acompanha `ApiName` ate a primeira edicao manual e depois preserva o valor manual. O mesmo teste executou B031 direto e cancelou o Passo 2, cobrindo a limpeza de revisao B032 que e possivel validar antes de B033 existir no menu:

```text
[Genexus Open API Builder][B032] Transaction resolvida para o wizard: Name='Escola', Module='Root Module', SelectionSource='Contexto'.
[Genexus Open API Builder][B032] Contrato B031 ausente ou incompativel para Transaction='Escola'. Abrindo B031 automaticamente.
[Genexus Open API Builder][B031] Wizard Passo 2 concluido em memoria durante o fluxo B032: Transaction='Escola', Services='List,Get,Create,Update'.
[Genexus Open API Builder][B031] Campos selecionados: Create=20, Update=20, Response=23, ListFilters=1.
[Genexus Open API Builder][B032] Wizard Passo 3 concluido em memoria: Transaction='Escola', ApiName='apiEscola2', ServicesBasePath='apiEscola1a', RestPath='/escola', SecurityLevel='Authentication'.
[Genexus Open API Builder][B032] Paginacao e ordenacao: DefaultPageSize=50, MaximumPageSize=200, StaticOrder='EscolaCodigo ASC'.
[Genexus Open API Builder][B032] Proximo passo habilitado para B033. Nenhum ApiPlan foi criado, nenhuma escolha foi persistida e nenhum objeto foi criado, alterado ou excluido.
[Genexus Open API Builder][B031] Wizard cancelado no Passo 2 para Transaction='Escola'. Escolhas em memoria descartadas; nenhuma alteracao foi feita na KB.
```

Como B033 ainda nao existe no menu, a validacao funcional de consumo da revisao B032 por B033 fica pendente para a implementacao de B033. O comportamento atual ja limpa a revisao B032 quando B031 direto cancela, volta, fecha sem conclusao ou grava novo contrato.

## Validacao local

- `pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1`: OK, com 10 comandos registrados e sincronizados;
- `dotnet build Src\GenexusOpenApiBuilder.sln -c Release`: compilacao com sucesso, 0 erros;
- `git diff --check`: sem erros de whitespace;
- avisos NU1900 ocorreram apenas por indisponibilidade de consulta de vulnerabilidades nos feeds NuGet durante o build interativo.

## Resultado

Criterio atendido em 2026-07-21: o Passo 3 do wizard revisa paths, seguranca, paginacao e ordenacao, chama B031 automaticamente quando necessario, preserva a regra de sincronizacao entre `ApiName` e `Services base path`, guarda as decisoes apenas em memoria e deixa o prototipo pronto para B033, sem `ApiPlan`, sem persistencia e sem escrita na KB.