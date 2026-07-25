# Status Atual e Próximo Passo

## Autoridade deste checkpoint

Este documento é a fonte canônica apenas para o estado operacional do projeto e para a próxima ação executável.

Ele não define requisitos funcionais nem contratos técnicos. Para essas decisões, prevalecem o [registro de decisões funcionais do MVP](Decisions/2026-07-14-REGISTRO_DECISOES_FUNCIONAIS_MVP.md) e os contratos ativos em [Foundation](Foundation/00-MASTER_INDEX_DO_PROJETO.md).

## Última atualização

2026-07-25.

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
- Follow-up da Sprint 3 validado manualmente em 2026-07-25: o `ApiPlan` em memória resolve `GeneratorTarget='.NET'` como gerador prioritário inicial do MVP, `ConflictMode='BlockOnCollision'` como política conservadora inicial para colisão externa/incompatível e `ReexecutionMode='Safe'`; condição GAM e engine real permanecem pendentes, com `IsEngineReady=false`, sem persistir metadata nem gerar SDT, Procedure, API Object ou File na KB.
- Contrato preparatório de configuração por KB para B090/B091 validado manualmente em 2026-07-25 na Transaction `Contrato`: a Output registrou `ConfigScope='KnowledgeBase'`, `ConfigSource='DefaultInMemoryHardcodedB090B091Policy'`, `ConfigStatus='PendingPersistentMetadata'`, `PersistedMetadata=False`, `KbConfigured=False`, `SensitiveRules=5` e `AuditRules=6`, preservando decisões apenas em memória, sem metadata persistente e sem gerar SDT, Procedure, API Object ou File na KB.
- Contrato mínimo da metadata persistente futura para B090/B091 validado manualmente em 2026-07-25 na Transaction `Contrato`: a Output registrou `SchemaVersion='B090B091_KB_FIELD_CLASSIFICATION_V1'`, `Section='fieldClassification'`, `SensitiveMember='sensitiveExactNames'`, `AuditExactMember='auditExactNames'`, `AuditSuffixMember='auditSuffixes'` e `RequiredMembers=5`, ainda sem ler ou gravar File de metadata e sem gerar objetos na KB.
- B056 validado manualmente em 2026-07-25 na Transaction `Contrato`: a Output registrou `ServiceDescriptionsPending=0/4`, `ServiceDescriptionLanguage='English'`, `ServiceDescriptionFallbackUsed=True`, `Resolved=4/4`, `LanguageSource='PendingKbLanguageApiValidation'` e fallback técnico em inglês, sem aplicar `[Description]` em objeto `API` real e sem gerar SDT, Procedure, API Object ou File na KB.

## Frente ativa

**Sprint 3 — Metadata + ApiPlan**, aprofundando o plano interno em memória depois de B038 e da representação provisória de B090/B091/B092. O plano já resolve os campos escalares `GeneratorTarget`, `ConflictMode` e `ReexecutionMode` para o alvo inicial do MVP, já preserva no `ApiPlan` o contrato preparatório de configuração por KB para B090/B091 validado manualmente no U15, e também preserva o contrato mínimo da futura metadata persistente validado manualmente no U15. B056 também foi validado manualmente no U15 com descrições resolvidas em memória e fallback técnico em inglês até validação pública da leitura do idioma principal da KB. B090/B091 canônicos ainda dependem de regras carregadas de metadata persistente real, e o plano ainda não é entrada válida da engine porque a condição GAM segue pendente e a engine real ainda não foi implementada; não gerar objetos de API e não escrever na KB.

## Próxima ação única

Continuar Sprint 3 preparando a resolução da condição GAM de B092 no `ApiPlan`, ainda sem aplicar segurança em objetos reais e sem gerar objetos:

> Definir como o `ApiPlan` deve representar a condição final de GAM para `SecurityLevel='Authentication'`, `None` e `Authorization`, substituindo `GamCondition='UNRESOLVED_B092_GAM_CONDITION'` apenas quando houver contrato seguro. Ainda sem persistir metadata e sem gerar SDT, Procedure, API Object ou File na KB.

## Critério de conclusão e evidência esperada

- o `ApiPlan` em memória parte da seleção consolidada pelo wizard único aberto por `Abrir Wizard (B030)`;
- sensibilidade e auditoria operacional ficam representadas de forma explícita e separada no plano, com política inicial hardcoded marcada como provisória;
- o contrato preparatório de configuração por KB já fica preservado no `ApiPlan`, com `ConfigStatus='PendingPersistentMetadata'`, `PersistedMetadata=False` e `KbConfigured=False`;
- o contrato mínimo da metadata persistente futura fica preservado no `ApiPlan`, com schema, seção e membros mínimos para sensíveis e auditoria, sem ainda criar ou gravar o File de metadata;
- o plano registra `Security Level` e a condição aplicável de GAM/None sem aplicar segurança em objetos reais;
- o plano registra `GeneratorTarget='.NET'`, `ConflictMode='BlockOnCollision'` e `ReexecutionMode='Safe'` como decisões em memória da Sprint 3;
- B056 preserva descrições de serviço, idioma e fallback no plano, sem aplicar `[Description]` em objeto `API` real;
- `Business Component` continua tratado como pré-condição, com a exceção persistente já validada em B035;
- nenhuma geração de SDT, Procedure, API Object ou metadata persistente é executada pela extensão;
- base pronta para evoluir para metadata persistente, incluindo configuração por KB de sensíveis/auditoria, e geração posterior somente depois que a prontidão da engine deixar de ser `IsEngineReady=false`.

## Sequência operacional vigente

1. Sprint 0 executou a Fase 0 (`B010`–`B012`) e deixou a base de build reproduzível.
2. Sprint 1 concluiu e aprovou no U15 o pacote inicial de viabilidade da Fase -1 (`B000`–`B006`).
3. Sprint 2 concluiu `B020`, `B021`, `B022`, `B023`, `B024`, `B025`, `B030`, `B031`, `B032`, `B033`, `B034`, `B035`, `B036` e `B037`, encerrando as Fases 1 e 2 do protótipo navegável com escolhas em memória, sem `ApiPlan` e sem geração de objetos de API.
4. Sprint 3 iniciou metadata e `ApiPlan` com B038, registrou representação provisória de B090/B091/B092 em memória, resolveu e validou os campos escalares `GeneratorTarget`, `ConflictMode` e `ReexecutionMode`, validou manualmente o contrato preparatório de configuração por KB, o contrato mínimo da metadata persistente futura para B090/B091 e B056 no escopo de plano, e continua com B090/B091 canônicos abertos para regras carregadas de metadata persistente real e condição GAM pendente.
5. Sprint 4 integra o wizard ao engine pela primeira vez e cria os SDTs.
6. Sprints 5–7 completam Procedures/API/metadata, serviços REST/segurança e o ciclo conservador de conflitos, regeneração e remoção.
7. O marco **wizard funcional do MVP concluído** ocorre ao final da Sprint 7, antes da Alpha.

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

- engine de geração;
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
