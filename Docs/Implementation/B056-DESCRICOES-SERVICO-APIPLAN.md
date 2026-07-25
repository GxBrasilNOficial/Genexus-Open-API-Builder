# B056 - Descricoes de servico no ApiPlan

Validado manualmente no GeneXus 18 Upgrade 15 em 2026-07-25: o `ApiPlan` resolve descricoes de servico em memoria, ainda sem aplicar `[Description]` em objeto `API` real e sem gerar objetos na KB.

## Objetivo

Preparar o contrato de B056 no plano interno, cobrindo descricoes dos servicos selecionados, idioma usado e fallback registrado, conforme decisoes do MVP.

## Escopo implementado

- `ServiceDescriptions` deixa de usar `UNRESOLVED_B056_SERVICE_DESCRIPTION` quando o wizard conclui;
- as descricoes sao criadas para os servicos selecionados `List`, `Get`, `Create` e `Update`;
- cada descricao usa a descricao legivel da `Transaction` quando existir;
- quando a descricao da `Transaction` estiver vazia, o plano usa o nome da `Transaction`;
- `ServiceDescriptionLanguage` fica `English` como fallback tecnico inicial;
- `ServiceDescriptionLanguageSource` fica `PendingKbLanguageApiValidation`;
- `ServiceDescriptionFallbackUsed` fica `true`;
- `ServiceDescriptionFallbackReason` explicita que o idioma principal da KB ainda nao foi validado por API publica;
- a Output B056 exibe contagem resolvida, idioma, origem e razao do fallback;
- nenhuma metadata persistente, SDT, Procedure, API Object ou File e criado, lido, alterado ou excluido pela geracao.

## Arquivos principais

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
[Genexus Open API Builder][B090/B091] Metadata futura no ApiPlan: SchemaVersion='B090B091_KB_FIELD_CLASSIFICATION_V1', Section='fieldClassification', SensitiveMember='sensitiveExactNames', AuditExactMember='auditExactNames', AuditSuffixMember='auditSuffixes', RequiredMembers=5. Ainda sem ler ou gravar File de metadata.
[Genexus Open API Builder][B035] Business Component em memoria: IsBusinessComponent=True, EnabledDuringWizard=False, Status='Apta via Business Component'.
[Genexus Open API Builder][B038] ApiPlan em memoria criado: Transaction='Contrato', ModuleTarget='Root Module', ApiName='apiContrato', MetadataFile='apiContrato_Metadata', EndpointsCount=4.
[Genexus Open API Builder][B038] ApiPlan cobre: PrimaryKey=1, CreateFields=1, UpdateFields=1, ResponseFields=2, ListFilters=1, RequiredFields=2, Procedures=4, SharedSdts=2. Sem persistir metadata e sem gerar SDT, Procedure, API Object ou File na KB.
[Genexus Open API Builder][Sprint3] Campos de engine no ApiPlan: GeneratorTarget='.NET' como gerador prioritario inicial do MVP, ConflictMode='BlockOnCollision' para colisao externa/incompativel, ReexecutionMode='Safe', ServiceDescriptionsPending=0/4, ServiceDescriptionLanguage='English', ServiceDescriptionFallbackUsed=True, IsEngineReady=False. Sem validar engine real e sem gerar objetos.
[Genexus Open API Builder][B056] Descricoes no ApiPlan: Resolved=4/4, Language='English', LanguageSource='PendingKbLanguageApiValidation', FallbackUsed=True, FallbackReason='Idioma principal da KB ainda nao validado por API publica; fallback tecnico em ingles registrado no ApiPlan.'. Sem aplicar [Description] em objeto API real e sem gerar objetos.
[Genexus Open API Builder][B092] Seguranca no ApiPlan: SecurityLevel='Authentication', GamCondition='UNRESOLVED_B092_GAM_CONDITION', RequiresGenerationConfirmation=False. Sem aplicar seguranca em objetos reais.
[Genexus Open API Builder][B034] Wizard concluido sem acionar cancelamento. Decisoes e ApiPlan permanecem somente em memoria; nenhuma geracao de objetos de API foi executada.
```

Observacao posterior: a captura acima preserva o estado manual da frente B056. A condicao GAM foi resolvida depois no escopo de plano em [B092 - Condicao GAM resolvida no ApiPlan](B092-CONDICAO-GAM-APIPLAN.md), ainda sem aplicar seguranca em objeto API real.

## Validacoes mecanicas

Executadas durante a implementacao local:

```powershell
dotnet build Src/GenexusOpenApiBuilder.sln -c Release
```

Resultado: build Release OK com 0 erros. O build emitiu avisos NU1900 por indisponibilidade dos indices NuGet para auditoria de vulnerabilidade, sem impedir a compilacao.

## Criterio de aceite

Criterio atendido em 2026-07-25 quando:

- `ServiceDescriptionsPending=0/4` para os quatro servicos selecionados;
- B056 exibe `Resolved=4/4`, `Language='English'`, `LanguageSource='PendingKbLanguageApiValidation'` e `FallbackUsed=True`;
- a mensagem preserva que `[Description]` nao foi aplicado em objeto `API` real;
- o plano permanece em memoria, sem metadata persistente e sem gerar SDT, Procedure, API Object ou File na KB.

B056 canonico continua aberto fora deste escopo de plano: ainda sera necessario aplicar e persistir `[Description]` nos servicos do objeto `API` real durante a geracao, com idioma/fallback resolvidos pela leitura validada do idioma principal da KB por API publica. Esta frente validou apenas as descricoes no `ApiPlan` em memoria.
