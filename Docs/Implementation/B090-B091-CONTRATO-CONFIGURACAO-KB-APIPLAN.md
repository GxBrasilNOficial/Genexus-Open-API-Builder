# B090/B091 - Contrato de configuracao por KB no ApiPlan

Validado manualmente no GeneXus 18 Upgrade 15 em 2026-07-25: o wizard unico continua classificando sensiveis e auditoria apenas em memoria, mas agora explicita no snapshot e no `ApiPlan` o contrato minimo da futura configuracao por KB.

## Objetivo

Preparar a substituicao da politica inicial hardcoded por configuracao explicita por KB em metadata persistente futura, sem persistir metadata nesta frente e sem gerar objetos na KB.

## Escopo implementado

- `PrototypeWizardFieldClassificationConfiguration` representa a configuracao de classificacao no snapshot do wizard;
- `ApiPlanFieldClassificationConfiguration` preserva essa configuracao no `ApiPlan`;
- sensiveis e auditoria operacional continuam separados;
- a fonte permanece `DefaultInMemoryHardcodedB090B091Policy`;
- o status fica `PendingPersistentMetadata`;
- `IsPersistedMetadata=False` e `IsKnowledgeBaseConfigured=False` deixam explicito que ainda nao existe configuracao carregada de metadata;
- a Output B090/B091 passou a exibir escopo, fonte, status, flags de metadata/configuracao por KB e quantidade de regras;
- nenhuma metadata persistente, SDT, Procedure, API Object ou File e criado, alterado ou excluido pela geracao.

## Arquivos principais

- `Src/Extension/Diagnostics/PrototypeWizardContract.cs`
- `Src/Domain/ApiPlan.cs`
- `Src/Extension/Package.cs`

## Evidencia manual U15

Validacao recebida em 2026-07-25 para a Transaction `Contrato`:

```text
[Genexus Open API Builder][B030] Wizard único concluido em memoria: Transaction='Contrato', Module='Root Module', SelectionSource='Contexto'.
[Genexus Open API Builder][B031] Contrato em memoria: Services='List,Get,Create,Update', Create=1, Update=1, Response=2, ListFilters=1.
[Genexus Open API Builder][B032] Paths e segurança em memoria: ApiName='apiContrato', ServicesBasePath='apiContrato', RestPath='/contrato', SecurityLevel='Authentication'.
[Genexus Open API Builder][B033] Obrigatoriedade em memoria: CreateRequired=0, UpdateRequired=1. Required significa presença do membro JSON, nao valor nao-vazio.
[Genexus Open API Builder][B037] Obrigatorio no payload consolidado: CreateRequired=0, UpdateRequired=1. Required e presenca do membro JSON; vazio, false e 0 continuam valores enviados. UpdateRequest segue PUT completo.
[Genexus Open API Builder][B036] Campos bloqueados visiveis no wizard: CreateRequest=1, UpdateRequest=1, ListFilters=0. Itens bloqueados ficaram desmarcados, com motivo, e nao podem ser selecionados.
[Genexus Open API Builder][B090/B091] Classificacao em memoria: SensitiveFields=0, AuditFields=0. ConfigScope='KnowledgeBase', ConfigSource='DefaultInMemoryHardcodedB090B091Policy', ConfigStatus='PendingPersistentMetadata', PersistedMetadata=False, KbConfigured=False, SensitiveRules=5, AuditRules=6. Contrato por KB preparado no ApiPlan, ainda sem metadata persistente e sem geracao.
[Genexus Open API Builder][B035] Business Component em memoria: IsBusinessComponent=True, EnabledDuringWizard=False, Status='Apta via Business Component'.
[Genexus Open API Builder][B038] ApiPlan em memoria criado: Transaction='Contrato', ModuleTarget='Root Module', ApiName='apiContrato', MetadataFile='apiContrato_Metadata', EndpointsCount=4.
[Genexus Open API Builder][B038] ApiPlan cobre: PrimaryKey=1, CreateFields=1, UpdateFields=1, ResponseFields=2, ListFilters=1, RequiredFields=2, Procedures=4, SharedSdts=2. Sem persistir metadata e sem gerar SDT, Procedure, API Object ou File na KB.
[Genexus Open API Builder][Sprint3] Campos de engine no ApiPlan: GeneratorTarget='.NET' como gerador prioritario inicial do MVP, ConflictMode='BlockOnCollision' para colisao externa/incompativel, ReexecutionMode='Safe', ServiceDescriptionsPending=4/4, ServiceDescriptionLanguage='UNRESOLVED_B056_DESCRIPTION_LANGUAGE', ServiceDescriptionFallbackUsed=False, IsEngineReady=False. Sem validar engine real e sem gerar objetos.
[Genexus Open API Builder][B092] Seguranca no ApiPlan: SecurityLevel='Authentication', GamCondition='UNRESOLVED_B092_GAM_CONDITION', RequiresGenerationConfirmation=False. Sem aplicar seguranca em objetos reais.
[Genexus Open API Builder][B034] Wizard concluido sem acionar cancelamento. Decisoes e ApiPlan permanecem somente em memoria; nenhuma geracao de objetos de API foi executada.
```

Observacao posterior: a captura acima preserva o estado manual desta frente B090/B091. A condicao GAM foi resolvida depois no escopo de plano em [B092 - Condicao GAM resolvida no ApiPlan](B092-CONDICAO-GAM-APIPLAN.md), ainda sem aplicar seguranca em objeto API real.

## Validacoes mecanicas

Executadas durante a implementacao local:

```powershell
dotnet build Src/GenexusOpenApiBuilder.sln -c Release
```

Resultado: build Release OK com 0 erros. O build emitiu avisos NU1900 por indisponibilidade dos indices NuGet para auditoria de vulnerabilidade, sem impedir a compilacao.

## Criterio de aceite

Criterio atendido em 2026-07-25 quando:

- o wizard continua sem persistir metadata e sem gerar objetos;
- a classificacao continua visivel no wizard e no `ApiPlan`;
- o `ApiPlan` preserva a configuracao de classificacao como contrato por KB ainda pendente de metadata persistente;
- a Output mostra que `PersistedMetadata=False` e `KbConfigured=False`.

B090/B091 canonicos continuam abertos ate carregarem regras explicitas por KB a partir de metadata persistente futura.
