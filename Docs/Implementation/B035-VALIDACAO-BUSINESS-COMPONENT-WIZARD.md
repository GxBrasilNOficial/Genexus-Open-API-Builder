# B035 - Validacao de Business Component no Wizard

Concluido no GeneXus 18 Upgrade 15 em 2026-07-22: o wizard unico aberto por `Abrir Wizard (B030)` incorporou a verificacao de `Business Component`, bloqueou o avanco sem BC e habilitou a propriedade somente apos confirmacao explicita do usuario.

## Objetivo

Validar `Business Component` dentro do fluxo do wizard unico, sem depender do comando separado B024, preservando a regra do MVP de que a API nao avanca sem BC.

## Escopo implementado

- a aba `Business Component` foi inserida no wizard unico antes do `Resumo B035`;
- a verificacao reaproveita `PrototypeBusinessComponentReader.Read`, ja validado no B024 para leitura publica de `Transaction.IsBusinessComponent`;
- quando `Business Component=True`, o wizard segue para o resumo e registra aptidao via BC;
- quando `Business Component=False`, o wizard bloqueia o avanco se a checkbox de habilitacao explicita nao estiver marcada;
- a checkbox `Habilitar Business Component agora` inicia desmarcada;
- ao marcar e avancar, o wizard abre confirmacao modal informando que a operacao altera a `Transaction` na KB e nao sera revertida automaticamente;
- se a confirmacao for negada, nenhuma alteracao e feita na KB e o fluxo permanece bloqueado;
- se a confirmacao for aceita, a extensao grava a propriedade `Business Component=True` na `Transaction` por `SetPropertyValue("idISBUSINESSCOMPONENT", true)` e `Save()`;
- a conclusao do wizard continua mantendo contrato, paths, seguranca, obrigatoriedade e BC apenas em memoria de sessao;
- nenhum `ApiPlan` foi criado e nenhum objeto de API foi criado, alterado ou excluido pela geracao.

## Implementacao

`Src/Extension/PrototypeWizardDialog.cs` passa a receber o snapshot de BC, uma funcao de habilitacao e uma funcao de escrita na Output. O dialogo controla bloqueio, checkbox, confirmacao modal, mensagens B035 e resumo final com `IsBusinessComponent`, `EnabledDuringWizard` e `Status`.

`Src/Extension/Package.cs` passa a criar o snapshot de BC antes de abrir o wizard e implementa `EnableBusinessComponentForWizard`, que altera apenas a propriedade `idISBUSINESSCOMPONENT` da `Transaction` quando o usuario confirmou explicitamente a operacao.

As saidas B034 de voltar, cancelar ou fechar sem conclusao foram ajustadas para distinguir o caso em que BC ja foi habilitado antes da saida. Essa alteracao persistente nao e revertida automaticamente, conforme contrato do MVP.

## Validacao local

- `dotnet build Src\GenexusOpenApiBuilder.sln --configuration Release --no-restore`: OK, 0 erros;
- `pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1`: OK, com 8 comandos registrados e sincronizados;
- `git diff --check`: OK.

## Validacao manual no U15

Validacao manual concluida no U15 usando o wizard unico por `Abrir Wizard (B030)` no contexto da `Transaction` `Contrato`.

A UI confirmou:

- aba `Business Component` exibida antes do resumo;
- `IsBusinessComponent=False` e `Status: Bloqueada: Business Component desabilitado` antes da habilitacao;
- checkbox de habilitacao explicita desmarcada por padrao;
- tentativa de avancar sem marcar a checkbox bloqueou o fluxo com aviso;
- checkbox marcada abriu confirmacao modal avisando que a `Transaction` seria alterada na KB e que a alteracao nao seria revertida automaticamente;
- apos confirmacao, a propriedade `Business Component` da `Transaction` `Contrato` ficou `True` no painel Properties.

A Output observada confirmou o fluxo:

```text
[Genexus Open API Builder][B035] Transaction='Contrato' bloqueada: Business Component desabilitado e habilitacao explicita nao confirmada. Nenhum ApiPlan foi criado e nenhuma alteracao foi feita na KB.
========== Pattern generation (WorkWithWebContrato) started ==========
Instance 'WorkWithWebContrato' is up to date.
Success: Pattern generation (WorkWithWebContrato)
[Genexus Open API Builder][B035] Business Component habilitado por confirmacao explicita para Transaction='Contrato'. A alteracao foi gravada na KB e nao sera revertida automaticamente.
Reloading Transaction 'Contrato'...Done
[Genexus Open API Builder][B030] Wizard único concluido em memoria: Transaction='Contrato', Module='Root Module', SelectionSource='Contexto'.
[Genexus Open API Builder][B031] Contrato em memoria: Services='List,Get,Create,Update', Create=1, Update=1, Response=2, ListFilters=1.
[Genexus Open API Builder][B032] Paths e segurança em memoria: ApiName='apiContrato', ServicesBasePath='apiContrato', RestPath='/contrato', SecurityLevel='Authentication'.
[Genexus Open API Builder][B033] Obrigatoriedade em memoria: CreateRequired=0, UpdateRequired=1. Required significa presença do membro JSON, nao valor nao-vazio.
[Genexus Open API Builder][B035] Business Component em memoria: IsBusinessComponent=True, EnabledDuringWizard=True, Status='Apta via Business Component'.
[Genexus Open API Builder][B034] Wizard concluido sem acionar cancelamento. Decisoes permanecem somente em memoria; nenhum ApiPlan foi criado e nenhuma geracao de objetos de API foi executada.
```

O GeneXus disparou a verificacao/geracao do pattern `WorkWithWebContrato` e recarregou a `Transaction` apos a alteracao da propriedade. Esse efeito pertence a IDE ao habilitar BC e foi tratado como evidencia do efeito colateral esperado da confirmacao explicita.

## Resultado

Criterio atendido em 2026-07-22: B035 incorporou `Business Component` ao wizard unico, bloqueou Transaction sem BC, gravou BC apenas com confirmacao explicita e manteve as demais decisoes somente em memoria, sem `ApiPlan` e sem geracao de objetos de API.

Nota posterior: B036 foi concluido em seguida e passou a exibir campos tecnicamente inadequados desabilitados com motivo dentro do mesmo wizard unico.
