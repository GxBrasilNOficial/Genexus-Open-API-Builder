# Status Atual e Próximo Passo

## Autoridade deste checkpoint

Este documento é a fonte canônica apenas para o estado operacional do projeto e para a próxima ação executável.

Ele não define requisitos funcionais nem contratos técnicos. Para essas decisões, prevalecem o [registro de decisões funcionais do MVP](Decisions/2026-07-14-REGISTRO_DECISOES_FUNCIONAIS_MVP.md) e os contratos ativos em [Foundation](Foundation/00-MASTER_INDEX_DO_PROJETO.md).

## Última atualização

2026-07-28.

## Último marco concluído

- entrevista funcional do MVP consolidada;
- documentos Foundation alinhados e auditados;
- `B010` revalidado pelo método oficial U14+: feed NuGet, MSBuild SDKs, lockfile, solution, projeto e pacote mínimos;
- `B011` concluído: estrutura interna confirmada sem introduzir projetos ou camadas vazias;
- `B012` concluído: convenções aplicáveis confirmadas sem antecipar objetos da geração.
- `B000` concluído no U15: extensão mínima compilada, registrada, marcada e carregada sem erro de compatibilidade com a DLL Release estável e os metadados públicos corrigidos; a coluna Description vazia foi aceita como limitação não bloqueante.
- `B001` concluído no U15: a extensão detectou a KB de teste `wsEducacaoSpTeste` pelo evento público `OnAfterOpenKB` e pela KB recebida em `e.KB`, exibindo nome, GUID e localização na janela Output sem operações de escrita.
- `B002` concluído no U15: a extensão listou 10 Transactions reais da KB de teste `wsEducacaoSpTeste` com `Transaction.GetAll(knowledgeBase.DesignModel)`, exibindo o total e os nomes na janela Output sem operações de escrita.
- `B003` concluído no U15: a extensão criou o Folder de teste `GxOpenApi_B003_Probe` no Root Module da KB `wsEducacaoSpTeste`, confirmado na janela Output e no painel Properties, sem alterar objetos preexistentes.
- correção de segurança pós-B003 validada no U15: a DLL atual foi reinstalada e a abertura de uma KB não emitiu mensagens B001–B003 na Output nem realizou escrita automática.
- `B004` concluído no U15: um API Object de teste foi criado, alterado, relido após reinstalação da extensão e excluído com ausência confirmada pelo GUID, exclusivamente por APIs públicas.
- `B005` concluído no U15: Procedure, SDT, Folder e File de teste foram criados, alterados, relidos e excluídos com ausência confirmada, exclusivamente por APIs públicas e com autorização explícita antes de cada fase de escrita.
- `B006` concluído no U15: metadata JSON em File preservou GUID, nome, descrição, 316 bytes UTF-8 e SHA-256 após fechar e reabrir a KB; o File temporário foi excluído com ausência confirmada.
- `B020` concluído no U15: a extensão detectou manualmente a KB ativa `wsEducacaoSpTeste` no fluxo do protótipo, exibindo nome, GUID e localização na Output sem persistência e sem operações de escrita.
- `B021` concluído no U15: a extensão listou manualmente 10 Transactions da KB ativa `wsEducacaoSpTeste` na Output, sem persistência e sem operações de escrita.
- `B022` concluído no U15: a extensão selecionou manualmente a Transaction `Escola` no diálogo nativo e leu seu módulo `Root Module` na Output, sem persistência e sem operações de escrita.
- `B023` concluído no U15: a extensão detectou manualmente 15 objetos planejados para a Transaction `Laudo`, incluindo o File `apiLaudo_Metadata`, com 0 existentes e 15 ausentes na Output, sem persistência e sem operações de escrita.
- `B024` concluído no U15: a extensão verificou manualmente a propriedade `Business Component` da Transaction `Carga`, reportando `IsBusinessComponent=False` como bloqueio e `IsBusinessComponent=True` como aptidão após habilitação manual temporária da propriedade, sem persistência pela extensão e sem operações de escrita pela extensão. Após o fechamento funcional, o menu principal `Genexus Open API Builder` também foi validado antes de `Help`, com B020-B024 acionáveis e B023/B024 mantendo a proteção quando B022 ainda não selecionou uma `Transaction` em memória.
- `B025` concluído no U15: a extensão leu manualmente a chave primária completa da `Transaction` pelo menu de contexto e pelo fluxo com seleção em memória, reportando `Carga` com chave simples `CargaId` e `AbateOrdem` com chave composta `AbateOrdemEmpresaId` + `AbateOrdemId`, preservando ordem, tipos `NUMERIC`, tamanho e casas decimais, sem persistência pela extensão e sem operações de escrita pela extensão.
- `B030` concluído no U15: o primeiro passo do protótipo navegável do wizard selecionou `Transaction` pelo menu principal via seletor nativo e pelo menu de contexto, reportando `Carga` com `SelectionSource='Seletor'` e `Contrato` com `SelectionSource='Contexto'`, mantendo a escolha somente em memória e sem operações de escrita pela extensão.
- `B031` concluído no U15: o segundo passo do protótipo navegável configurou serviços `List`, `Get`, `Create` e `Update`, campos de `CreateRequest`, `UpdateRequest`, `Response` e filtros de `List` para a Transaction `Distribuidora`, navegando sequencialmente por `Servicos`, `Requests`, `Response`, `Filtros List` e `Resumo B032`, com fórmulas desabilitadas em requests por API pública, chave primária bloqueada no `CreateRequest` até validação pública de autonumeração, chave primária desabilitada no `UpdateRequest`, decisões apenas em memória e sem criar `ApiPlan` nem alterar a KB.
- `B032` concluído no U15: o terceiro passo do protótipo navegável revisou `ApiName`, `Services base path`, `RestPath`, paths por serviço, `Security Level`, paginação e ordenação para a Transaction `Escola`, acionado diretamente pelo menu de contexto da `Transaction`, abrindo B031 automaticamente quando o contrato estava ausente, com `ApiName='apiEscola'`, `ServicesBasePath='apiEscola'`, `RestPath='/escola'`, `SecurityLevel='Authentication'`, `DefaultPageSize=50`, `MaximumPageSize=200` e `StaticOrder='EscolaCodigo ASC'`, mantendo decisões apenas em memória e sem criar `ApiPlan` nem alterar a KB. A validação complementar confirmou que `Services base path` acompanha `ApiName` até edição manual e depois preserva o valor manual.
- `B033` concluído no U15: o wizard foi unificado em uma única chamada operacional por `Abrir Wizard (B030)`, absorvendo B031, B032 e B033 como páginas sequenciais da mesma janela; a aba `Obrigatorios` validou presença obrigatória de membros JSON para `DiretoriaDeEnsino`, com `CreateRequired=0` por campo nullable no `CreateRequest` e `UpdateRequired=1` por PUT completo no `UpdateRequest`, mantendo o resultado em memória e seguindo sem `ApiPlan`, sem persistência e sem escrita na KB.
- `B034` concluído no U15: o wizard único validou cancelamento seguro no seletor nativo, no botão `Cancelar`, na tecla Esc/fechamento, em `Voltar` no início do fluxo e na conclusão normal sem cancelamento; em todos os abortos descartou `Transaction`, contrato, paths/segurança e obrigatoriedade em memória, sem criar `ApiPlan`, sem persistência e sem escrita na KB.
- `B035` concluído no U15: o wizard único incorporou a verificação de `Business Component`, bloqueou avanço quando `Contrato` estava com `Business Component=False`, exigiu checkbox e confirmação modal antes de habilitar a propriedade, gravou `Business Component=True` na `Transaction` após autorização explícita, observou a geração/reload do pattern `WorkWithWebContrato` pela IDE e concluiu mantendo decisões apenas em memória, sem criar `ApiPlan` nem objetos de API.
- `B036` concluído no U15: o wizard único exibiu campos tecnicamente inadequados desabilitados e com motivo em `Requests` e `Filtros List`, impediu seleção de bloqueados, registrou contagens B036 na Output e manteve contrato, paths, obrigatoriedade, BC e resumo apenas em memória para `Contrato`, `Escola` e `GuiaPed`, sem criar `ApiPlan` nem gerar objetos de API.
- `B037` concluído no U15: o wizard único consolidou `Obrigatório no payload` para `CreateRequest` e `UpdateRequest`, separou as decisões por request na aba `Obrigatórios`, registrou `CreateRequired=0` e `UpdateRequired=1` para `Contrato`, esclareceu que `Required` significa presença do membro JSON e manteve decisões apenas em memória, sem criar `ApiPlan` nem gerar objetos de API.
- `B038` concluído no U15: o wizard único montou `ApiPlan` inicial em memória para `Contrato`, com `MetadataFile='apiContrato_Metadata'`, `EndpointsCount=4`, chave primária, campos de `CreateRequest`, `UpdateRequest`, `Response`, filtros, required por request, 4 Procedures planejadas e 2 SDTs compartilhados. O plano fica marcado como `IsEngineReady=false`, com `UNRESOLVED_B038_*` nos campos ainda não resolvidos do contrato mínimo da engine, sem persistir metadata nem gerar SDT, Procedure, API Object ou File na KB.
- Representação provisória de B090/B091 e B092 validada no U15 dentro do `ApiPlan` em memória: o wizard registrou `SensitiveFields=0` e `AuditFields=0` para `Contrato` pela política inicial hardcoded em memória, preservou origem/razão de classificação no plano e registrou `SecurityLevel='Authentication'`, `GamCondition='UNRESOLVED_B092_GAM_CONDITION'` e `RequiresGenerationConfirmation=False`, sem persistir metadata nem gerar SDT, Procedure, API Object ou File na KB. B090/B091 canônicos permanecem abertos até existir configuração explícita por KB em metadata persistente.
- Follow-up da Sprint 3 validado manualmente em 2026-07-25: o `ApiPlan` em memória resolve `GeneratorTarget='.NET'` como gerador prioritário inicial do MVP, `ConflictMode='BlockOnCollision'` como política conservadora inicial para colisão externa/incompatível e `ReexecutionMode='Safe'`; naquele momento, condição GAM e engine real permaneciam pendentes, com `IsEngineReady=false`, sem persistir metadata nem gerar SDT, Procedure, API Object ou File na KB.
- Contrato preparatório de configuração por KB para B090/B091 validado manualmente em 2026-07-25 na Transaction `Contrato`: a Output registrou `ConfigScope='KnowledgeBase'`, `ConfigSource='DefaultInMemoryHardcodedB090B091Policy'`, `ConfigStatus='PendingPersistentMetadata'`, `PersistedMetadata=False`, `KbConfigured=False`, `SensitiveRules=5` e `AuditRules=6`, preservando decisões apenas em memória, sem metadata persistente e sem gerar SDT, Procedure, API Object ou File na KB.
- Contrato mínimo da metadata persistente futura para B090/B091 validado manualmente em 2026-07-25 na Transaction `Contrato`: a Output registrou `SchemaVersion='B090B091_KB_FIELD_CLASSIFICATION_V1'`, `Section='fieldClassification'`, `SensitiveMember='sensitiveExactNames'`, `AuditExactMember='auditExactNames'`, `AuditSuffixMember='auditSuffixes'` e `RequiredMembers=5`, ainda sem ler ou gravar File de metadata e sem gerar objetos na KB.
- B056 validado manualmente em 2026-07-25 na Transaction `Contrato` como contrato preparatório no `ApiPlan`: a Output registrou `ServiceDescriptionsPending=0/4`, `ServiceDescriptionLanguage='English'`, `ServiceDescriptionFallbackUsed=True`, `Resolved=4/4`, `LanguageSource='PendingKbLanguageApiValidation'` e fallback técnico em inglês, ainda sem aplicar `[Description]` em objeto `API` real naquele recorte.
- B092 validado manualmente em 2026-07-25 na Transaction `Contrato`: a Output registrou `Authentication` com `GamCondition='GAM_AUTHENTICATION_REQUIRED'` e `RequiresGenerationConfirmation=False`, `Authorization` com `GamCondition='GAM_AUTHORIZATION_REQUIRED_PENDING_PERMISSIONS'` e `RequiresGenerationConfirmation=True`, e `None` com `GamCondition='NO_GAM_SECURITY_PUBLIC_API'` e `RequiresGenerationConfirmation=True`, ainda sem aplicar segurança em objeto `API` real e sem gerar SDT, Procedure, API Object ou File na KB.
- B039 validado manualmente em 2026-07-25 na Transaction `Contrato`: a Output registrou `Phase='Sprint4SdtEnginePreviewOnly'`, `Status='ResolvedSdtContractPreviewNoKbWrite'`, `WritesKnowledgeBase=False`, `OwnSdts=5` e `SharedSdts=2`, listando dois SDTs compartilhados e cinco SDTs próprios planejados, sem criar, alterar ou excluir objetos na KB.
- B040-B046 validados manualmente em 2026-07-25 na Transaction `Contrato`: após confirmação explícita no modal da IDE, o comando `Criar SDTs (B040-B046)` criou 7 SDTs a partir do `ApiPlan` (`PlannedOwnSdts=5`, `PlannedSharedSdts=2`, `Created=7`, `Reencountered=0`), incluindo `sdt_API_ErrorResponse`, `sdt_API_Pagination` e os cinco SDTs próprios `sdtContrato_API_*`, sem criar Procedure, API Object ou metadata persistente definitiva.
- B050-B053 validados manualmente em 2026-07-25 na Transaction `Contrato`: o comando bloqueou corretamente a execução sem `ApiPlan`; depois, com `ApiPlan` recriado pelo wizard, reencontrou 7 SDTs, criou 4 Procedures skeleton (`procContrato_API_List`, `procContrato_API_Get`, `procContrato_API_Create`, `procContrato_API_Update`) e registrou `Created=4`, `Reencountered=0`, sem criar API Object, REST completo ou metadata persistente definitiva.
- Integração do wizard com B040-B046 e B050-B053 validada manualmente em 2026-07-25 na Transaction `Contrato`: `Abrir Wizard (B030)` concluiu o `ApiPlan`, ofereceu a escrita de SDTs e Procedures no próprio fluxo e reencontrou 7 SDTs (`Created=0`, `Reencountered=7`) e 4 Procedures (`Created=0`, `Reencountered=4`) com `Trigger='Wizard'`, sem criar API Object, REST completo ou metadata persistente definitiva.

- Correção pós-revisão validada manualmente em 2026-07-26 na Transaction `Contrato`: `Abrir Wizard (B030)` exibiu abas próprias `SDTs` e `Procedures`, registrou no resumo `Gerar SDTs B040-B046=True` e `Gerar Procedures B050-B053=True`, concluiu sem modais pós-wizard e a Output registrou `GenerateSdts=True`, `GenerateProcedures=True`, B040-B046 com `Trigger='Wizard'`, `Created=0`, `Reencountered=7`, e B050-B053 com `Trigger='Wizard'`, `Created=0`, `Reencountered=4`. A validação confirmou o reencontro conservador dos objetos existentes e manteve o limite de não criar API Object, REST completo ou metadata persistente definitiva.

- B054 validado manualmente em 2026-07-26 na Transaction `Carga`: após reencontrar os 7 SDTs e as 4 Procedures, o wizard salvou `apiCarga`; a aba `Service Source` exibiu `List`, `Get`, `Create` e `Update`, cada serviço delegando sem parâmetros para a Procedure skeleton `procCarga_API_*` correspondente. A reexecução bloqueia fonte de serviços divergente. O passo não aplica ainda paths/métodos HTTP, segurança definitiva, descrições ou metadata persistente.
- O wizard passou a reconhecer o estado atual dos artefatos planejados antes da escrita: apresenta `plano em memoria`, teste de criação, complementação, reencontro ou bloqueio; as abas SDTs, Procedures e API Object refletem os objetos reencontráveis e permitem aplicar somente a etapa pendente. O resumo usa `Concluir Teste` sem escrita e `Concluir e aplicar` quando há geração confirmada.
- Os objetos gerados foram organizados no Folder irmão `<Transaction>OpenApi`; reexecuções realinham o Folder existente ao módulo e Folder físico da Transaction quando a API pública o permite.
- B055 validado manualmente em 2026-07-26: na Transaction `Carga`, as Procedures `procCarga_API_Create` e `procCarga_API_Update` passaram a persistir Source, Rules e variáveis reais via APIs públicas e foram especificadas/geradas/compiladas com sucesso; na Transaction `Teste`, com chave composta `TesteDate` + `TesteId`, o wizard criou novamente SDTs, Procedures e `apiTeste`, aplicou Create/Update via Business Component com `PrimaryKeyParts=2`, gerou `parm(in:&TesteDate, in:&TesteId, in:&UpdateRequest, out:&UpdateResponse);`, usou `&Teste.Load(&TesteDate, &TesteId)` e as duas Procedures especificaram, geraram e compilaram com sucesso. A correção pós-revisão sincronizou também o API Object: `apiTeste` passou a declarar Create/Update parametrizados, a aba Variables exibiu chaves, requests e responses compatíveis, e `Build With This Only` de `apiTeste` passou por especificação, geração, documentação REST, Protocol Buffer, compilação e atualização de configuração web. O warning de `LSI.Extensions` sobre variáveis não usadas foi descartado como bloqueante porque o build nativo do API Object reconheceu o contrato. A correção posterior de SDTs baseados em domínio foi validada na Transaction `GuiaPed`: o wizard reencontrou e reconfigurou 7 SDTs próprios/compartilhados, sincronizou `apiGuiaPed` via B055 e `Build All` passou especificando `apiGuiaPed`, `procGuiaPed_API_Create` e `procGuiaPed_API_Update`, gerando os SDTs de request/response e documentação REST sem erro. A correção pré-push posterior tornou B055 responsável por reconfigurar os SDTs requeridos mesmo quando somente a opção de Business Component é aplicada, adiou o realinhamento de Folder até depois do preflight principal e passou a bloquear Procedures B055 e API Object B055 reencontrados com variáveis extras, ausentes ou com tipo, atributo base, domínio ou objeto nomeado incompatível; Procedure já B055 sem variáveis não padrão também deixa de ser reparada silenciosamente. O passo não completa REST, códigos HTTP, segurança definitiva ou metadata persistente.
- B056 aplicado e validado manualmente em 2026-07-27 na Transaction `GuiaPed`: com somente `API Object` marcado, B054 reencontrou SDTs e Procedures, atualizou `apiGuiaPed` de B055 legado para B055 com `[Description]` em `List`, `Get`, `Create` e `Update`, preservou `Create`/`Update` parametrizados e variáveis compatíveis, e `Build All` passou especificando `apiGuiaPed`, gerando documentação REST e permissão GAM sem erro relacionado. O warning de `FBiTextSharp.dll` foi classificado como ambiental e não relacionado ao B056.
- B060 concluído manualmente em 2026-07-27 no GeneXus 18 U15: na Transaction `GuiaPed`, o wizard gravou/reencontrou o File `apiGuiaPed_Metadata`, persistiu `External File Name` via `BlobPart.FileName`, abriu/exportou o File na IDE, bloqueou JSON inválido e `ownership.apiName` incompatível no preflight visual, restaurou reencontro válido e registrou `Status='Reencountered'`, `Guid='1b943d80-5961-4b25-a89a-2454dae9f45d'`, `Bytes=99432`, `Sha256='C518A9EEFF4F9785E5B63D27F92D1E451D2E70B8E0FCE5BE8F98472D6095DF54'`; na Transaction `Contrato`, o preflight bloqueou colisão externa do File manual `apiContrato_Metadata` sem escrita, depois validou descrições especiais B056/B060 com `Contrato "Especial" \ Ação Ç`, gravou `apiContrato_Metadata` com `Status='Created'`, `Guid='b901d9e0-b213-4369-be79-18db0129cb82'`, `Bytes=12616`, `Sha256='3E7D0E5EF54B171D348030E6D46A08ED5DC8EA9F7E394B933E41D42975752D39'`, JSON válido e `Build All` aprovado mantendo apenas o warning ambiental de `FBiTextSharp.dll`. B060 permanece limitada ao snapshot inicial de metadata, sem completar REST, códigos HTTP finais ou segurança definitiva.
- B061/B062 validados manualmente em 2026-07-27 e 2026-07-28: em `Contrato` no `Root Module`, a Output confirmou 5 SDTs próprios em `TransactionModuleFolder:ContratoOpenApi`, 2 SDTs compartilhados em `RootModuleFolder:GxOpenAPI`, 4 Procedures e o API Object `apiContrato` reencontrados em `TransactionFolder='ContratoOpenApi'`, com inspeção visual dos objetos dentro de `ContratoOpenApi` e File `apiContrato_Metadata` em `Module='Root Module'`; em `SimulationResult` no módulo não-root `Entities`, após correção de runtime, a Output confirmou 5 SDTs próprios em `TransactionModuleFolder:SimulationResultOpenApi`, 4 Procedures e o API Object `apiSimulationResult` em `TransactionFolder='SimulationResultOpenApi'`, B060 criou `apiSimulationResult_Metadata`, as Properties confirmaram `Module='Entities'` e `Qualified Name='Entities.apiSimulationResult_Metadata'`, o JSON exportado em `Temp/apiSimulationResult_Metadata.json` parseou com `ownership.transactionModule='Entities'`, 4 Procedures, 5 SDTs próprios e 2 SDTs compartilhados com nomes conforme B062, e o `Build All` especificou/gerou `Entities.apiSimulationResult` e concluiu com sucesso. A referência `object-file.md` da skill nexa e a wiki oficial confirmam que File é organizado por módulo, não por Folder.

- B063–B066 validados em 2026-07-28 no U15: B063/B064 bloquearam colisão externa e metadata incompatível antes do primeiro Save(), sem _v2; B065 persistiu paths, filtros, paginação, ordenação e segurança; B066 persistiu transactionFolder.wasCreated=true na criação e false no reencontro, com Build All aprovado.
## Frente ativa

**Sprint 5 — Procedures, API Object e Metadata**, com B040-B046, B050-B053, B054-B056 e B060-B066 validados. A próxima frente operacional é B067: registrar descrições geradas e dados para detectar alteração manual posterior.

## Próxima ação única

Implementar e validar B067: persistir na metadata descrições geradas e dados de integridade para detectar e bloquear alteração manual posterior antes de qualquer escrita.

## Critério de conclusão e evidência esperada

- metadata própria passa a registrar descrições geradas e dados suficientes para reconhecer alteração manual posterior;
- alteração manual em descrição, ownership ou contrato essencial é detectada antes do primeiro Save();
- divergência bloqueia a aplicação sem sobrescrever objeto alheio, sem _v2 e sem reparo silencioso;
- a Output confirma a detecção e o bloqueio conservador.

## Sequência operacional vigente

1. Sprint 0 executou a Fase 0 (`B010`–`B012`) e deixou a base de build reproduzível.
2. Sprint 1 concluiu e aprovou no U15 o pacote inicial de viabilidade da Fase -1 (`B000`–`B006`).
3. Sprint 2 concluiu `B020`, `B021`, `B022`, `B023`, `B024`, `B025`, `B030`, `B031`, `B032`, `B033`, `B034`, `B035`, `B036` e `B037`, encerrando as Fases 1 e 2 do protótipo navegável com escolhas em memória, sem `ApiPlan` e sem geração de objetos de API.
4. Sprint 3 iniciou metadata e `ApiPlan` com B038, registrou representação provisória de B090/B091/B092 em memória, resolveu e validou os campos escalares `GeneratorTarget`, `ConflictMode` e `ReexecutionMode`, validou manualmente o contrato preparatório de configuração por KB, o contrato mínimo da metadata persistente futura para B090/B091, B056 e a condição B092 no escopo de plano; B090/B091 canônicos continuam abertos para regras carregadas de metadata persistente real.
5. Sprint 4 validou B039 como preview de engine SDT em memória e concluiu B040-B046 com a primeira escrita real de SDTs próprios e compartilhados a partir do `ApiPlan`.
6. Sprint 5 validou B050-B053 com a criação das Procedures skeleton sobre os SDTs existentes, ainda sem API Object, REST completo ou metadata persistente definitiva.
7. Sprint 5 validou B040-B046 e B050-B053 também no encerramento integrado do wizard, com Trigger='Wizard', reencontro dos objetos existentes e preservação dos comandos independentes para reexecução/diagnóstico.
8. A correção pós-revisão de B040-B046/B050-B053 foi validada na IDE antes de B054, cobrindo abas próprias de confirmação no wizard, ausência de modais pós-wizard e reencontro conservador dos objetos existentes com preflight completo.
9. Sprint 5 validou B054: `api<NomeBase>` cria ou reencontra de forma conservadora e expõe os serviços selecionados delegando às Procedures skeleton B050-B053; o wizard aplica somente etapas pendentes conforme o estado atual de geração.
10. Sprint 5 validou B055: Create e Update usam Business Component nas Procedures já geradas e o API Object é sincronizado com assinatura e variáveis compatíveis, cobrindo chave simples, chave composta e atributos baseados em domínio nos SDTs de request/response; o preflight também bloqueia variáveis B055 ausentes, extras ou com referência divergente, sem completar REST, códigos HTTP, segurança definitiva ou metadata persistente.
11. Sprint 5 validou B056: descrições resolvidas no `ApiPlan` são aplicadas nos serviços reais do API Object, preservando o Service Source parametrizado de B055 quando existente.
12. Sprint 5 concluiu B060: metadata persistente inicial em File com JSON versionado, abertura/exportação na IDE, reencontro após restauração do JSON válido, bloqueio conservador de JSON inválido, identidade incompatível e colisão externa, além da preservação de descrições especiais B056 com aspas, barra invertida e caracteres incomuns.
13. Sprint 5 validou B061/B062: API Object, Procedures e SDTs específicos permanecem no Folder `<Transaction>OpenApi` dentro do módulo da `Transaction`, cobrindo `ContratoOpenApi` no `Root Module` e `SimulationResultOpenApi` no módulo não-root `Entities`; SDTs compartilhados permanecem em `GxOpenAPI`, File de metadata permanece no módulo da Transaction por contrato do objeto File, e todos os nomes persistidos seguem as convenções congeladas.
14. Sprint 5 validou B063/B064: preflight bloqueia colisões externas, incompatíveis ou ambíguas antes do primeiro Save(), sem sobrescrever objetos nem criar _v2; o parser de Service Source B054/B055 tem cobertura unitária.
15. Sprint 5 validou B065: paths, filtros, paginação, ordenação e segurança são persistidos na metadata B060.
16. Sprint 5 validou B066: a metadata distingue Folder criado de Folder reutilizado.
17. A próxima validação operacional é B067: registrar descrições geradas e dados para detectar alteração manual posterior.
18. Sprints 5–7 completam metadata persistente, serviços REST/segurança e o ciclo conservador de conflitos, regeneração e remoção.
19. O marco **wizard funcional do MVP concluído** ocorre ao final da Sprint 7, antes da Alpha.

## Bloqueios e fatos ainda não validados

- carregamento real do pacote em U14;
- compatibilidade prática das APIs do SDK com U14;
- comprovação progressiva dos gates técnicos transversais definidos nos documentos 09, 15 e 24.

A ausência do instalador Platform SDK não é bloqueio para U14+, porque a compilação usa o feed NuGet e os MSBuild SDKs oficiais. A proteção da instalação do GeneXus continua válida: o agente não escreve em `C:\Program Files (x86)\GeneXus`; o instalador controlado só copia a DLL quando o usuário o executa manualmente como administrador.

## Documentos governantes

- [06 — Backlog](Foundation/06-BACKLOG_v0.1.md)
- [09 — Integração com o SDK](Foundation/09-INTEGRACAO_GeneXus_Extensibility_SDK.md)
- [15 — Testes e qualidade](Foundation/15-TESTES_VALIDACAO_E_QUALIDADE.md)
- [24 — Plano por sprints](Foundation/24-PLANO_IMPLEMENTACAO_REAL_POR_SPRINTS.md)

## Marcos ainda não iniciados

- serviços REST/segurança finais;
- Alpha público.

## Protocolo de atualização

Toda mudança de marco, frente ativa ou próxima ação deve atualizar este checkpoint no mesmo commit que produz a mudança. O checkpoint deve manter uma única próxima ação e apontar para os contratos, sem duplicá-los.

Ao promover uma frente concluída para a próxima ação, atualizar em conjunto:

- a seção `Último marco concluído`, registrando a frente encerrada e sua evidência mínima;
- a seção `Próxima ação única`;
- a seção `Critério de conclusão e evidência esperada`;
- a seção `Sequência operacional vigente`.

Divergência entre essas seções é gap documental confirmado na revisão pré-push.

O fechamento de cada spike `B000`–`B006` também deve cumprir o checklist obrigatório de retirada de sondas temporárias, reinstalação da DLL passiva e alinhamento documental definido em `AGENTS.md`, antes de o marco ser considerado pronto para revisão pré-push.
