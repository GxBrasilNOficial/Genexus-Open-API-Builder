# B038 - ApiPlan em memoria

Concluido no GeneXus 18 Upgrade 15 em 2026-07-23: o wizard unico aberto por `Abrir Wizard (B030)` passou a montar um `ApiPlan` inicial em memoria a partir da `Transaction` e das decisoes acumuladas do wizard, sem persistir metadata e sem gerar objetos de API. Esse plano inicial nao e contrato minimo valido da engine.

## Objetivo

Transformar as escolhas ja capturadas pelo wizard em uma representacao interna inicial de `ApiPlan`, cobrindo `Transaction`, modulo, servicos, campos, obrigatoriedade no payload, paths, seguranca, paginacao, ordenacao, nomes planejados e pre-condicao de `Business Component`.

## Escopo validado

- o fluxo parte do wizard unico aberto por `Abrir Wizard (B030)`;
- o `ApiPlan` e montado somente ao concluir o wizard sem cancelamento;
- cancelamentos continuam descartando `Transaction`, decisoes e `ApiPlan` em memoria;
- o plano guarda servicos, requests, response, filtros, required por request, paths, seguranca, paginacao, ordenacao, chave primaria, nomes de SDTs, Procedures, File de metadata e SDTs compartilhados;
- nenhuma escolha e persistida como metadata;
- nenhum SDT, Procedure, API Object ou File e criado, alterado ou excluido pela geracao;
- `IsEngineReady=false` deixa explicito que o plano inicial ainda nao pode ser entregue a engine;
- `GeneratorTarget`, `ConflictMode`, `ReexecutionMode` e descricoes de servico ficam marcados como `UNRESOLVED_B038_*`;
- `RestArtifactTarget` fica registrado como `API Object`, por ser alvo ja definido pelo contrato F10.

## Arquivos principais

- `Src/Domain/ApiPlan.cs`
- `Src/Extension/Package.cs`
- `Src/Extension/PrototypeWizardDialog.cs`

## Evidencia manual U15

Validacao recebida em 2026-07-23 para a Transaction `Contrato`:

```text
[Genexus Open API Builder][B034] Wizard único cancelado ou fechado para Transaction='Contrato'. Transaction e decisões em memoria descartadas; nenhum ApiPlan foi criado. Nenhuma alteracao foi feita na KB.
[Genexus Open API Builder][B034] Wizard único cancelado ou fechado para Transaction='Distribuidora'. Transaction e decisões em memoria descartadas; nenhum ApiPlan foi criado. Nenhuma alteracao foi feita na KB.
[Genexus Open API Builder][B030] Wizard único concluido em memoria: Transaction='Contrato', Module='Root Module', SelectionSource='Contexto'.
[Genexus Open API Builder][B031] Contrato em memoria: Services='List,Get,Create,Update', Create=1, Update=1, Response=2, ListFilters=1.
[Genexus Open API Builder][B032] Paths e segurança em memoria: ApiName='apiContrato', ServicesBasePath='apiContrato', RestPath='/contrato', SecurityLevel='Authentication'.
[Genexus Open API Builder][B033] Obrigatoriedade em memoria: CreateRequired=0, UpdateRequired=1. Required significa presença do membro JSON, nao valor nao-vazio.
[Genexus Open API Builder][B037] Obrigatorio no payload consolidado: CreateRequired=0, UpdateRequired=1. Required e presenca do membro JSON; vazio, false e 0 continuam valores enviados. UpdateRequest segue PUT completo.
[Genexus Open API Builder][B036] Campos bloqueados visiveis no wizard: CreateRequest=1, UpdateRequest=1, ListFilters=0. Itens bloqueados ficaram desmarcados, com motivo, e nao podem ser selecionados.
[Genexus Open API Builder][B035] Business Component em memoria: IsBusinessComponent=True, EnabledDuringWizard=False, Status='Apta via Business Component'.
[Genexus Open API Builder][B038] ApiPlan em memoria criado: Transaction='Contrato', ModuleTarget='Root Module', ApiName='apiContrato', MetadataFile='apiContrato_Metadata', EndpointsCount=4.
[Genexus Open API Builder][B038] ApiPlan cobre: PrimaryKey=1, CreateFields=1, UpdateFields=1, ResponseFields=2, ListFilters=1, RequiredFields=2, Procedures=4, SharedSdts=2. Sem persistir metadata e sem gerar SDT, Procedure, API Object ou File na KB.
[Genexus Open API Builder][B034] Wizard concluido sem acionar cancelamento. Decisoes e ApiPlan permanecem somente em memoria; nenhuma geracao de objetos de API foi executada.
```

## Validacoes mecanicas

Executadas antes da validacao manual:

```powershell
dotnet build Src/GenexusOpenApiBuilder.sln -c Release
pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1
git diff --check
```

Resultados: build Release OK com 0 erros, checker de comandos OK com 8 comandos e `git diff --check` sem apontamentos.

## Criterio de aceite

Criterio atendido em 2026-07-23: B038 criou e validou o primeiro `ApiPlan` em memoria a partir do wizard unico, preservando cancelamento seguro e mantendo a KB sem novos SDTs, Procedures, API Object ou File de metadata. A frente deixa a Sprint 3 pronta para aprofundar as regras de seguranca, sensibilidade/auditoria, resolucao dos campos pendentes do contrato minimo da engine e preparacao futura da metadata persistente.
