# CHANGELOG.md

# Changelog

Todas as mudanças relevantes deste projeto serão registradas neste arquivo.

O formato segue princípios de changelog legível e versionamento progressivo.

---

# [Unreleased]

## Changed

## Fixed

## Added

---

# [0.1.0-alpha.1] - 2026-08-10

Primeira Alpha pública do Genexus Open API Builder (Sprint 8).

## Added

- Pacote público da Alpha: README orientado a visitante, [guia de instalação](Docs/Public/INSTALL.md), [demo rápida](Docs/Public/DEMO.md) e [notas de release](Docs/Releases/0.1.0-alpha.1.md).
- Galeria visual em `Docs/Images/` (Wizard completo, Preferências, Sync, Remover, metadata File, Folder e relatório de sucesso).
- Versão do pacote da extensão alinhada a `0.1.0-alpha.1`.

## Summary

- Marco **wizard funcional do MVP** concluído (2026-08-09), com comprovação integrada dos dez gates no GeneXus 18 U15.
- Ciclo de vida na IDE: Wizard, preferências por KB, sincronização com a Transaction, remoção conservadora, relatório final e UX mínima de colisão.
- Serviços `List`, `Get`, `Create` e `Update` com metadata persistente, posse via metadata e regeneração conservadora.
- Frentes pré-Alpha documentadas: limitações do YAML nativo (`B088`) e evidência HTTP 403 com role GAM restrita (`B089`).

## Changed

- Correção documental (2026-08-11): INSTALL do usuário final passa a seguir a ordem comprovada no B094 (Add > Local com a IDE aberta → fechar → `genexus /install` → reabrir). README e INSTALL deixam de ensinar atualização por Add > Local sobre DLL já em `Packages` — no B094 isso falhou com `Error installing extension`; atualização só com a DLL permanece não comprovada, e o caminho estável de atualização continua sendo o dos `.bat` de mantenedor.

- Documentação pública alinhada à evidência `B094` (2026-08-11): [INSTALL](Docs/Public/INSTALL.md) passa a distinguir instalação do usuário final (DLL via Add > Local + `genexus /install`, sem clonar) do fluxo de mantenedor (`.bat`); notas da Alpha deixam de negar o pacote instalável fora do repositório e listam o que ainda não entra (elevação, canal Web, desinstalação). Checkpoint, backlog 06, plano 24 e comprovação dos dez gates alinhados à ordem publicação → gate de usuário externo. Sem mudança de código da extensão. (A redação inicial de atualização por Add > Local no README foi corrigida em seguida; ver entrada acima.)

- `B094` (2026-08-10; correção 2026-08-11): evidência de instalação por usuário externo sem clonar o repositório — Add > Local + `genexus /install` ativou marcada + menus no GeneXus 18 U15, ainda com elevação (UAC). Correção do argumento falso “só Scanning / sem `added`” (captura incompleta); em cmd já elevado o log registrou `Package 'GenexusOpenApiBuilder.Extension.dll' added`. Premissa “`/install` elevado pode não varrer” refutada. Sem mudança de código da extensão. Evidência: `Docs/Implementation/B094-INSTALACAO-APENAS-COM-A-DLL-SEM-CLONAR.md`. Documentação pública não alterada nesta frente.

- Nota de revisão em `B000` (2026-08-11): `Docs/Implementation/B000-CARREGAMENTO-IDE.md` afirmava, em dois pontos, que `genexus /install` elevado não varria os pacotes no U15. A premissa foi refutada pelo `B094`; a observação original vinha de captura incompleta da saída. O texto de época é preservado e a nota registra a refutação. O contrato operacional do repositório não muda: `Register-ExtensionForGeneXus18.bat` continua recusando execução elevada. Sem mudança de código da extensão.

- `B089` concluído (2026-08-10): evidência HTTP **403 Forbidden** com papel GAM não-administrador sob `SecurityLevel = Authorization` em `apiNotaFiscal`. Setup via GAM Backoffice (role `Role_GOAB_Test_Denied`, Get Permitir, Create não atribuído, usuário `goab_role_denied`); GET **200** e POST Create **403** (`code` 139) nos environments .NET Framework/SQL Server e .NET/PostgreSQL; controle POST com usuário autorizado **201**. Documentado em `Docs/Implementation/B093-SECURITY-LEVEL-APIPLAN-OBJETO.md` §4.A.3.D. Sem mudança de código da extensão.

- `B088` concluído (2026-08-10): investigação read-only do gerador OpenAPI nativo (`Swagger.Yaml.stg` / `TypeDefinitions.Yaml.stg` / `Artech.Packages.RestServiceDL.Generator`) comprovou inviabilidade de substituir ou interceptar os templates sem alterar a instalação GeneXus. Limitação intransponível documentada; ressalvas e orientação de consumo (`openapi-generator-cli`, leitores/agentes do YAML cruzando Source/Events) incorporadas aos documentos 12 e 27. Sem mudança de código da extensão. Evidência: `Docs/Implementation/2026-08-10-B088-LIMITACOES-YAML-NATIVO.md`.

- Correção documental pós-Sprint 7 (2026-08-09): no gate 9 (metadata e reconhecimento seguro), a remissão de backlog passa a incluir `B087` junto de `B085`–`B086`, alinhando a matriz canônica do documento 06 §7.1 e a coluna de remissões do pacote `Docs/Implementation/2026-08-09-COMPROVACAO-DEZ-GATES-SPRINT7.md` à evidência de posse via metadata já citada.

- Sprint 7 fechada (2026-08-09): comprovação integrada dos dez gates técnicos transversais e marco **wizard funcional do MVP concluído**, por consolidação do acervo U15 das Sprints 1–7 (sem bateria U15 nova e sem mudança de código). Upgrade 14 permanece residual não validado nesta máquina e não bloqueia o marco. Evidência: `Docs/Implementation/2026-08-09-COMPROVACAO-DEZ-GATES-SPRINT7.md`.

- B083 residual (2026-08-08): UX mínima de conflitos de colisão — para cada objeto conflitante, lista `Nome | Tipo | Modulo | Folder` no Wizard, preflight, Output e relatório quando a escrita é barrada por colisão. Núcleo de detecção/bloqueio sem overwrite/`_v2` já existia. File usa `Folder='(n/a)'`. Validado no U15 em `Teste` (colisão com SDT externo; após remoção, geração completa e Build All nos dois environments). Teste `Tests/CollisionUx/Test-ApiPlanCollisionConflict.ps1`. Manifesto inalterado (só DLL). Evidência: `Docs/Implementation/B083-UX-CONFLITOS-COLISAO.md`.

- B081 (2026-08-08/09): relatório final pós-aplicação após Wizard, Sync (incluindo sem diff) e Remover — diálogo com criados/atualizados/removidos/bloqueados/avisos, efeitos colaterais do plano (Folder criado, Transaction atualizada pelo Business Component e SDTs escritos pelos writers), Output `[B081]`, Abrir objeto principal, wrap, altura ajustada em 10% e largura em 20%, limites relativos à área útil e rolagem vertical recalculada após o layout. Wizard principal inicia em `1200x912`, com mínimo `900x640`. Validado no U15 em `Teste`: criação com `Created=12`, incluindo `TesteOpenApi`; reuso com `Created=0`, `Updated=13`, `Warnings=2`; restauração final com `Created=11`, `Updated=2`; remoção com Folder reutilizado preservado. Teste `Tests/ApplicationFinalReport/Test-ApiPlanApplicationFinalReport.ps1`. Manifesto inalterado (só DLL). Evidência: `Docs/Implementation/B081-RELATORIO-FINAL-POS-APLICACAO.md`.

- Folder reutilizado (2026-08-08/09; alinhamento B066): Folder preexistente `<Transaction>OpenApi` no contêiner correto passa a ser reutilizado com aviso explícito, sem `Save()`/realinhamento e sem alterar Description; Folder em contêiner incorreto, sentinela divergente ou ocorrência ambígua continua bloqueando. Metadata mantém `wasCreated=true` para criação e `false` para reencontro; remoção preserva Folder reutilizado. Validado no U15 em `Teste`: caminho feliz com Description humana; contêiner incorreto, duplicidade entre módulos e sentinela de outra Transaction bloqueados antes de qualquer escrita; restauração final com `Created=11`, `Updated=2`, `Blocked=0`, `Warnings=2`; relatório rolável e metadata com `transactionFolder.wasCreated=false`. Teste `Tests/TransactionFolder/Test-ApiPlanTransactionFolderReusePolicy.ps1`. Manifesto inalterado (só DLL). Evidência: `Docs/Implementation/2026-08-08-FOLDER-REUTILIZADO-COM-AVISO.md`.

- B085 (2026-08-08): comando `Sincronizar com a Transaction` (menu principal e contexto); diff Transaction×metadata por `attributeGuid`; UI 2x2 de escolhas no delta; confirmação; gravação via writers/preflight sem reabrir o wizard. Metadata passa a persistir `transactionStructure` e `attributeGuid` nos campos. Preflight de Sync permite atualizar o contrato B067 e Source/Rules B055/B070 de propósito. Ordem do menu: Preferências → Wizard → Sincronizar → Remover API gerada. Validado no U15 em `Teste`: sem diff; add; rename por GUID; cancelar; conflito Replace; conflito Keep (`ManualKeep` preservado). Teste `Tests/TransactionSync/Test-ApiPlanTransactionSyncComparer.ps1`. Manifesto alterado (`genexus /install` necessário). Evidência: `Docs/Implementation/B085-SINCRONIZAR-COM-TRANSACTION.md`.

- B086 (2026-08-07/09): comando `Remover API gerada` no menu principal e no contexto da Transaction; plano a partir da metadata; confirmação; exclusão conservadora. Ordem: API Object → Procedures → SDTs (ListResponse antes de Response) → metadata → Folder se `wasCreated`. Validado no U15 em `Teste`: cancelamento; Folder reutilizado preservado; Folder criado removido (`Deleted=12` incluindo `TesteOpenApi`); reteste do Folder reutilizado com `Deleted=11`, `Blocked=0`, `Warnings=0` e Folder preservado; SDTs `GxOpenAPI` e BC preservados. Confirmação com Procedures/SDTs um por linha. Manifesto alterado. Teste `Tests/GeneratedApiRemoval/Test-ApiPlanGeneratedApiRemovalPlan.ps1`. Evidência: `Docs/Implementation/B086-REMOVER-API-GERADA.md`.

- B087 (2026-08-07): posse do API Object passa a ser resolvida pela metadata (`ownership` + integridade B067), com fallback na `Description` apenas quando o File ainda não existe. Reencontro deixa de sobrescrever a `Description`. Validado no U15 em `apiTeste` com Description humana preservada após Wizard. Description gerada padrão sem ponto terminal; forma legada com ponto aceita no fallback/integridade. Teste `Tests/ApiObjectOwnership/Test-ApiPlanApiObjectOwnership.ps1` integrado ao pré-push. Evidência: `Docs/Implementation/B087-POSSE-API-OBJECT-METADATA.md`.

- Revisão documental da Sprint 7 (2026-08-07): o objetivo vigente deixa de ser “revalidar conflitos/reexecução” (já entregues nas Sprints 5–6) e passa a ser o ciclo de vida operacional na IDE — posse do API Object via metadata (`B087`), `Remover API gerada` (`B086`), `Sincronizar com a Transaction` (`B085`), relatório final (`B081`), UX mínima de conflitos e comprovação dos dez gates. `B088` (YAML nativo) e `B089` (403 GAM) ficam pré-Alpha separados. Documentos: `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`, `Docs/Foundation/24-PLANO_IMPLEMENTACAO_REAL_POR_SPRINTS.md`, `Docs/Foundation/06-BACKLOG_v0.1.md`.

## Fixed

- Sync B085 (2026-08-10): posse do API Object no preflight deixa de exigir `IsManagedApiObject` contra o ApiPlan reconstruído; ownership na metadata (`schemaVersion` + `apiName` + `apiGuid`) basta. Corrige bloqueio falso ao adicionar campos. A seleção de campos no Sync preserva a ordem da metadata e anexa adds no fim (dedupe por GUID). Validado no U15 em `NotaFiscal`/`NotaFiscalObs3` (`Updated=13`, `Blocked=0`).

- Descriptions de produto sem IDs de backlog (2026-08-09): objetos gerados passam a usar `{Nome} - by Genexus Open API Builder` (Procedures, SDTs, API Object, metadata, Folders criados, preferências). Sentinelas legadas com `B0xx` / prosa `REST API for …` continuam reconhecidas no reencontro, Sync e Remover, desde que a descrição inteira seja compatível com o serviço, tipo, API e Transaction esperados. Divergências deixam de herdar posse por prefixo. Validado no U15 em `Teste`: Descriptions na IDE; Remover `Deleted=12` com posse canônica; Sync/Remover sem metadata bloqueiam; Wizard restaura `Created=12`; Sync sem diff; cancelamento do Remover. Manifesto inalterado (só DLL). Evidência: `Docs/Implementation/2026-08-09-DESCRIPTIONS-PRODUTO-SEM-BACKLOG.md`.

- B086 preflight de remoção (2026-08-09): `ValidateRemovalTargets` confere ambiguidade e posse de API Object, Procedures e SDTs próprios em `Preview` e no início de `Remove`, antes de qualquer `Delete()`. Evita remoção parcial quando um alvo divergente só seria detectado após o API Object já ter sido apagado. Manifesto inalterado (só DLL). Evidência: `Docs/Implementation/B086-REMOVER-API-GERADA.md`.

- Ownership legado e pré-voo B086 (2026-08-09): Procedures e SDTs só reconhecem a combinação legada exata de backlog + serviço/tipo; metadata também exige `Transaction` e `Api` compatíveis. O B086 ganhou regressão automatizada para confirmar o preflight de API Object, Procedures e SDTs antes do primeiro `Delete()`. Testes `Tests/OwnershipDescriptions/Test-ApiPlanOwnedObjectDescription.ps1` e `Tests/GeneratedApiRemoval/Test-ApiPlanGeneratedApiRemovalPreflight.ps1`. Manifesto inalterado (só DLL).

- Cobertura mecânica dos testes de ownership e B086 (2026-08-09): os dois testes acima passaram a ser executados pelo `scripts/Invoke-PrePushMechanicalChecks.ps1`, com fixtures, status e comandos verificados pelo teste do próprio checker. Isso evita que a cobertura fique restrita à execução manual.

- B085 Keep (2026-08-08): Sync passa `preserveSdts` também para writers BC (B055) e List (B070); antes o Keep só omitia `ConfigureSdt` na 1ª etapa e BC/List regravavam o SDT. Validado no U15 em `Teste` com membro `ManualKeep` preservado e `PreservedSdts=1`.

- Correção do `Location` para chave de texto (2026-08-06):
  - Path-encoding: `StrReplace(URLEncode(Trim(...)), !"+" , !"%20")` para emitir espaço como `%20` em segmento de path (a forma `URLEncode` sozinha emitia `+` e o GET no `Location` falhava).
  - Recusa de `/` em parte de PK texto no Create: após `Save` e antes do `Commit`, `Rollback` + `400 invalid_request` — `/` não é endereçável de forma confiável no path REST do MVP.
  - Preflight migra a forma anterior `URLEncode(Trim(...))` via `&LocationUrl`; teste de contrato atualizado com verificação por mutação.
  - Validação HTTP nos dois geradores: espaço `COD%2001` com GET `200`; acento e simples sem regressão; barra `400` sem gravação. Captura `Temp/location-matrix-2026-08-06-pos-correcao.json`; doc `Docs/Implementation/B071-B073-B079-GET-CREATE-UPDATE-HTTP.md`.

- Passo 4 (opção 2, 2026-08-06): chave primária não autonumerada no CreateRequest inicia **opcional** (não required); a aba Obrigatorios do Create ficou editável para exigir o campo no payload quando desejado. Evita 400 quando rules/BC preenchem chave omitida ou com default do tipo. Smoke HTTP em `apiTeste` (dois environments): POST só com `TesteDesc` → `201`, `Location` navegável e GET `200` após rules `on BeforeInsert` na Transaction `Teste` (rules `if insert` não preencheram `TesteId`/`TesteCodigo` via BC).

- Autonumeração no wizard (Passo 3, 2026-08-06): chave composta (`PrimaryKeyParts > 1`) deixa de bloquear campos no CreateRequest; PK simples continua lendo `Autonumber`/`idAUTONUMBER` com fallback conservador. Evidência em `Teste` vs `NotaFiscal`; sonda TEMP `AutonumberProbe` removida após a captura.

- Performance do wizard (2026-08-06): `ReadGenerationState` passou a um `GetAll` por tipo com lookup por nome; preview evita reentrância, só recalcula em abas de geração e usa cache por fingerprint (Resumo força refresh). Testes em `Tests/WizardContract/`.

- Recaptura 2026-08-06 do cabeçalho `Location` com chave composta e `URLEncode` na Transaction `Teste`:
  - Gerador Create passou a montar `&LocationUrl` e a usar `URLEncode(Trim(...))` após `Validation of Procedure` rejeitar a forma anterior com `.Trim()` encadeado na gravação em 1 clique.
  - Matriz HTTP real documentada em `Docs/Implementation/B071-B073-B079-GET-CREATE-UPDATE-HTTP.md`: acento navegável; espaço emitido como `+` e não navegável no `Location`; `%2F` com `404` nos dois ambientes (com corpos distintos). A matriz anterior que afirmava `200` para espaço/`%20` e sucesso de `%2F` no Kestrel foi marcada como superada.
  - Checkpoint `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md` alinhado à evidência capturada.

- Correções sobre os commits `ee09fa3` e `b4a70f6`:
  - Restaurado o reencontro de SDTs `ApiPlanSdtWriter.CreateOrReencounter` no fluxo `ApplyList` (`ApiPlanListProcedureWriter.cs`) para suporte a reaplicação isolada.
  - Invertido o fallback de autonumeração em `PrototypeWizardContract.cs` para bloqueio defensivo no `CreateRequest` em caso de erro ou incerteza.
  - Revertida a visibilidade dos 13 métodos auxiliares de writers de `internal` de volta para `private`.
  - Documentadas as alterações estruturais do commit `b4a70f6` e o defeito histórico do `ErrorItem` em `Docs/Implementation/2026-08-05-REGISTRO-MUDANCAS-ESTRUTURAIS-B4A70F6-E-DEFEITO-ERRORITEM.md`.
  - Corrigida a referência do documento 27 no backlog `06-BACKLOG_v0.1.md`.

## Added

- Fechamento dos gaps de evidência da Sprint 6 (Gaps 1, 2 e 3):
  - **GAP 1**: Provado em runtime HTTP o nível `SecurityLevel = Authorization` em `apiNotaFiscal` nos dois geradores (.NET Framework/SQL Server e .NET Core/PostgreSQL), registrando respostas 401 sem token / token inválido e 200 OK com token OAuth GAM válido do usuário de teste.
  - **GAP 2**: Corrigido o `CreateLocationUrlExpression` no gerador `ApiPlanBusinessComponentWriter` para serializar atributos de data no formato ISO estrito `YYYY-MM-DD` e aplicar `EncodeUrl` em campos alfanuméricos. Provada em runtime HTTP a emissão e navegabilidade direta do cabeçalho `Location` com chave primária composta (`TesteId` + `TesteDate`) na Transaction `Teste` (POST `201 Created` e GET `200 OK` direto na URL exata do `Location` retornado sem reescrita de URL, nos geradores .NET Framework e .NET Core).
  - **GAP 3**: Registrados os itens de backlog pré-Alpha `B088` (limitações do gerador nativo `Swagger.Yaml.stg`) e `B089` (validação automatizada de permissões granulares GAM por roles) em `Docs/Foundation/06-BACKLOG_v0.1.md` e a nota de auditabilidade e sobrescrita de arquivos YAML em `Docs/Implementation/B093-SECURITY-LEVEL-APIPLAN-OBJETO.md`.
- Tratamento do resíduo condicional do cabeçalho `Location` no serviço `Create` (B072/B079) para fecho integral da Sprint 6: o gerador de Procedure Business Component (`ApiPlanBusinessComponentWriter`) passou a emitir `&HttpResponse.AddHeader(!"Location", ...)` nativo e a declarar a variável `HttpResponse` (tipo `HttpResponse, GeneXus.Http`) ao retornar `201 Created`, montando a URL relativa do endpoint `Get` a partir do `RestPath` e da(s) chave(s) primária(s) simples ou composta(s).
- Fechamento da Sprint 6 com validação das três frentes de encerramento:
  - **Frente 1 (B047)**: Criado o teste automatizado off-line [`Tests/OpenApiContract/Test-OpenApiClientContractValidity.ps1`](file:///c:/Dev/Knowledge/Genexus-Open-API-Builder/Tests/OpenApiContract/Test-OpenApiClientContractValidity.ps1) para atestar a validade de identificadores `_API_` e `operationIds` para geradores de cliente OpenAPI, integrado à rotina pré-push `scripts/Invoke-PrePushMechanicalChecks.ps1`.
  - **Frente 2**: Auditoria minuciosa do YAML nos geradores `.NET Framework / SQL Server` e `.NET / PostgreSQL` cobrindo os seis eixos exigidos (rotas, métodos, `operationId`s `apiNome.Serviço`, ausência de DELETE, schemas `_API_` congelados em B062, bloco `security` por serviço B093 e schemas de request/response sem `Errors`), com evidência registrada em [`Docs/Implementation/2026-08-04-VALIDACAO-YAML-SPRINT6-EIXOS-SEGURANCA.md`](file:///c:/Dev/Knowledge/Genexus-Open-API-Builder/Docs/Implementation/2026-08-04-VALIDACAO-YAML-SPRINT6-EIXOS-SEGURANCA.md).
  - **Frente 3**: Declaração formal do Gate da Sprint 6 e Gate Técnico Transversal 4 como **Aprovados com Ressalva**, com alinhamento documental em `09`, `15`, `24` e emenda técnica datada de 2026-08-04 no registro primário de decisões `Docs/Decisions/2026-07-14-REGISTRO_DECISOES_FUNCIONAIS_MVP.md`.
- Estrutura inicial do repositório
- Pasta Docs organizada
- Foundation Docs 00 até 28
- checkpoint operacional `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`
- template de evidência reproduzível de `B010` em `Docs/Implementation`
- consolidação documental da entrevista funcional do MVP
- README inicial
- LICENSE MIT
- Planejamento da fase prática
- Sprint 0 concluído: build mínima reproduzível (`B010`–`B012`), solution e projeto de extensão em `Src`
- B000 concluído no U15: extensão mínima registrada, marcada e carregada na IDE com a DLL Release estável e os metadados públicos corrigidos
- B001 concluído no U15: detecção da KB ativa por API oficial, em modo somente leitura
- B002 concluído no U15: listagem de 10 Transactions reais por API oficial, em modo somente leitura
- B003 concluído no U15: criação controlada de Folder de teste com autorização explícita
- correção de segurança pós-B003 validada: a DLL atual não executa sondas automaticamente ao abrir uma KB
- B004 concluído no U15: ciclo de vida de API Object oficial comprovado com criação, alteração, releitura após reinstalação e exclusão confirmada
- B005 concluído no U15: ciclo de vida de Procedure, SDT, Folder e File comprovado com criação, alteração, releitura e exclusão confirmada
- B006 concluído no U15: metadata JSON em File preservou GUID, nome, descrição, bytes UTF-8 e SHA-256 após fechar e reabrir a KB
- B020 concluído no U15: detecção manual da KB ativa no fluxo do protótipo navegável, em modo somente leitura
- B021 concluído no U15: listagem manual de 10 Transactions da KB ativa no fluxo do protótipo navegável, em modo somente leitura
- B022 concluído no U15: seleção nativa manual de Transaction e leitura de seu módulo no fluxo do protótipo, em modo somente leitura
- B023 concluído no U15: detecção manual dos objetos planejados para a Transaction selecionada, incluindo `api<NomeBase>_Metadata`, em modo somente leitura
- B024 concluído no U15: verificação manual de `Business Component` da Transaction selecionada, em modo somente leitura
- B025 concluído no U15: leitura manual da chave primária simples e composta completa da Transaction selecionada, em modo somente leitura
- Menu principal `Genexus Open API Builder` validado no U15 com acesso aos comandos B020-B030, preservando o submenu de contexto da Transaction
- B030 validado no U15: `Abrir Wizard (B030)` seleciona `Transaction` pelo menu principal via seletor nativo e pelo contexto da Transaction, mantendo a escolha apenas em memória
- B031 validado no U15: `Configurar Contrato (B031)` navega sequencialmente por serviços, requests, response, filtros e resumo B032, acumulando escolhas apenas em memória sem criar `ApiPlan` nem alterar a KB
- B032 validado no U15: `Revisar Paths e Segurança (B032)` funciona pelo menu de contexto da `Transaction`, chama B031 automaticamente quando necessário e revisa paths, segurança, paginação e ordenação apenas em memória
- B033 validado no U15: `Abrir Wizard (B030)` passa a abrir o wizard único, absorvendo B031, B032 e B033 como páginas sequenciais e validando campos obrigatórios apenas em memória
- B034 validado no U15: cancelamento seguro do wizard único descarta `Transaction`, contrato, paths/segurança e obrigatoriedade em memória sem criar `ApiPlan` nem alterar a KB
- B035 validado no U15: wizard único verifica `Business Component`, bloqueia avanço sem BC e habilita a propriedade somente com confirmação explícita, mantendo decisões apenas em memória e sem criar `ApiPlan`
- B036 validado no U15: wizard único exibe campos tecnicamente inadequados desabilitados com motivo em `Requests` e `Filtros List`, mantém bloqueados não selecionáveis e registra contagens apenas em memória sem criar `ApiPlan`
- B037 validado no U15: wizard único consolida `Obrigatório no payload` para `CreateRequest` e `UpdateRequest`, explicita `Required` como presença do membro JSON e mantém decisões apenas em memória — a semântica de presença foi revista em 2026-08-03 e substituída por validação de preenchimento; ver a entrada correspondente em `Changed` e a nota de revisão de `B076` no documento 06
- B038 validado no U15: wizard único monta `ApiPlan` inicial em memória para `Contrato`, com `MetadataFile='apiContrato_Metadata'`, 4 endpoints, 4 Procedures planejadas e 2 SDTs compartilhados, marcado como `IsEngineReady=false` e com pendências `UNRESOLVED_B038_*`, sem persistir metadata nem gerar objetos na KB
- Representação provisória de B090/B091 e B092 validada no U15 dentro do `ApiPlan`: wizard único classifica sensíveis e auditoria por política inicial hardcoded em memória, registra origem/razão no `ApiPlan` e grava `SecurityLevel` com `GamCondition='UNRESOLVED_B092_GAM_CONDITION'`, mantendo B090/B091 canônicos abertos até configuração por KB/metadata e sem persistir metadata nem gerar objetos na KB
- Follow-up da Sprint 3 validado manualmente: `ApiPlan` em memória resolve `GeneratorTarget='.NET'`, `ConflictMode='BlockOnCollision'` e `ReexecutionMode='Safe'`, mantendo naquele momento condição GAM e prontidão da engine explicitamente pendentes, sem persistir metadata nem gerar objetos na KB
- Contrato preparatório de configuração por KB para B090/B091 validado no U15 dentro do `ApiPlan`, ainda sem metadata persistente e sem gerar objetos na KB
- B056 validado no U15 dentro do `ApiPlan`: descrições de serviço resolvidas em memória com `ServiceDescriptionsPending=0/4`, idioma `English` e fallback técnico registrado, ainda sem aplicar `[Description]` em objeto `API` real naquele recorte preparatório
- B092 validado no U15 dentro do `ApiPlan`: `Authentication`, `Authorization` e `None` agora resolvem `GamCondition` explicitamente no plano, com confirmação obrigatória para `Authorization` e `None`, ainda sem aplicar segurança em objeto `API` real e sem gerar objetos na KB
- B039 validado no U15: preview de engine SDT recebe o `ApiPlan`, resolve cinco SDTs próprios e dois compartilhados com status `ResolvedSdtContractPreviewNoKbWrite`, registra `WritesKnowledgeBase=False` e não escreve na KB
- B040-B046 validados no U15: comando `Criar SDTs (B040-B046)` criou 7 SDTs a partir do `ApiPlan` após confirmação modal explícita na IDE, com 5 próprios e 2 compartilhados, sem Procedures, API Object ou metadata persistente definitiva
- B050-B053 validados no U15: comando `Criar Procedures (B050-B053)` bloqueou execução sem `ApiPlan`, depois criou 4 Procedures skeleton a partir do `ApiPlan` e reencontrou 7 SDTs B040-B046, sem API Object, REST completo ou metadata persistente definitiva
- `Abrir Wizard (B030)` validado no U15 oferecendo B040-B046 e B050-B053 em abas próprias de confirmação no wizard, com Output marcada com `Trigger='Wizard'`, reencontro de 7 SDTs e 4 Procedures existentes e preservação dos comandos separados para reexecução/diagnóstico
- Correção pós-revisão B040-B046/B050-B053 validada no U15: abas `SDTs` e `Procedures` do wizard confirmam a escrita antes do resumo, Output registra `GenerateSdts=True`, `GenerateProcedures=True`, `Trigger='Wizard'`, reencontro de 7 SDTs e 4 Procedures, sem modais pós-wizard e sem criar API Object, REST completo ou metadata persistente definitiva
- B054 validado no U15: `apiCarga` exibe `List`, `Get`, `Create` e `Update` na aba `Service Source`, cada serviço delegando sem parâmetros à Procedure skeleton B050-B053 correspondente; reexecução bloqueia fonte divergente
- wizard passou a reconhecer em leitura o estado atual de SDTs, Procedures e API Object, apresentando criação, complementação, reencontro ou bloqueio antes de qualquer escrita e usando `Concluir Teste` quando não há etapa confirmada
- Folder `<Transaction>OpenApi` criado ou reencontrado como irmão físico da Transaction, com realinhamento conservador em reexecuções
- B055 validado no U15: Create e Update passaram a ser aplicados via Business Component nas Procedures já geradas, com variáveis reais, Source/Rules persistidos nas partes públicas corretas, preflight conservador, validação em chave simples (`Carga`) e composta (`Teste`) e API Object sincronizado com assinatura/variáveis compatíveis
- B056 aplicado e validado no U15 em API Object real: `apiGuiaPed` recebeu `[Description]` nos serviços `List`, `Get`, `Create` e `Update`, preservou as assinaturas parametrizadas de B055 em `Create`/`Update`, e `Build All` passou gerando documentação REST
- B060 concluído no U15: wizard grava ou reencontra File JSON de metadata, persiste `External File Name` via `BlobPart.FileName`, bloqueia JSON inválido, identidade incompatível e colisão externa antes da escrita, preserva descrições especiais B056 com aspas, barra invertida e caracteres incomuns, exporta JSON válido e mantém o escopo sem completar REST, códigos HTTP finais ou segurança definitiva
- B061/B062 validados no U15: objetos específicos que suportam Folder permanecem no Folder `<Transaction>OpenApi` dentro do módulo da `Transaction`, cobrindo `ContratoOpenApi` no `Root Module` e `SimulationResultOpenApi` no módulo não-root `Entities`; SDTs compartilhados permanecem em `GxOpenAPI`, File de metadata permanece no módulo da Transaction e nomes persistidos seguem as convenções congeladas
- B063-B066 validados no U15: preflight agregado bloqueia colisões externas/incompatíveis antes do primeiro `Save()`, metadata compatível permite reencontro conservador, paths/filtros/paginação/ordenação/segurança são persistidos e `transactionFolder.wasCreated` distingue Folder criado de Folder reutilizado
- B067 validado no U15: metadata grava integridade de descrições geradas, contrato planejado e Service Source; alteração manual posterior em `[Description]` bloqueia o wizard antes do primeiro `Save()` e a restauração da descrição original permite reencontro conservador
- B070/B077 validados no U15: `List` real sincroniza Procedure e API Object com filtros elegíveis, paginação, ordenação determinística, `totalCount`, `totalPages` e `AppliedFilters`; membros nullable de `ListFilters` são gerados com `Json Null Serialization = JSON null`, preservando `AppliedFilters.ContratoNumero=null` quando o filtro não é informado
- B068 implementado e validado funcionalmente no U15: novo comando `Configurar Preferências do Wizard` persiste defaults por KB no File `GxOpenApiBuilder_Settings`, e `Abrir Wizard (B030)` aplica esses defaults somente quando a etapa está habilitada pelo estado da KB; a configuração foi ampliada para serviços REST, `Security Level`, `Default Page Size` e `Maximum Page Size`; o preflight agregado respeita etapas selecionadas e B070 aceita reconfiguração segura de paginação em Source próprio conhecido
- Menu `Genexus Open API Builder` simplificado: no menu principal ficam `Configurar Preferências do Wizard` e `Wizard`; no contexto da Transaction fica somente `Wizard`; comandos incrementais B020-B025, B040-B054 e o placeholder `Futura Primeira Opção` foram removidos do runtime e do manifesto após o wizard absorver o fluxo.
- Preferência de `ApplyBusinessComponent` passa a ser aplicada somente quando a Transaction está apta via Business Component ou foi habilitada explicitamente no wizard, evitando bloqueio indevido de B055 e preservando List/Metadata quando BC está desabilitado.
- Navegação do wizard deixa de depender da passagem sequencial pela aba `Business Component`: a habilitação explícita pendente agora é avaliada também ao abrir o `Resumo` diretamente e antes da conclusão.
- Intenção de aplicar Create/Update via Business Component é preservada enquanto a habilitação explícita de BC ainda está pendente, permitindo que B055 rode no mesmo wizard após a Transaction ser habilitada.
- Ao marcar `Habilitar Business Component agora`, a aba passa a liberar `Aplicar Create/Update via Business Component após habilitar` como decisão pendente, sem antecipar gravação na KB.
- `Aplicar Create/Update via Business Component` passa a exigir SDTs, Procedures e API Object disponíveis ou confirmados no wizard, evitando B055 isolado quando a KB não tem dependências seguras.
- Aba `Business Component` foi reposicionada depois de `SDTs`, `Procedures` e `API Object`, alinhando a navegação com as dependências de B055.
- Após habilitar Business Component ao entrar direto no `Resumo`, o wizard recalcula as dependências antes de montar a seleção final, preservando `ApplyBusinessComponent=True` quando B055 estava marcado.
- Cobertura automatizada da navegação B055 passou a simular refresh local sem dependências seguido de refresh completo, garantindo restauração da intenção pendente antes da seleção final.
- B071-B073/B079 implementa a etapa REST de Get/Create/Update para regravar `proc<Transacao>_API_Get`, `proc<Transacao>_API_Create` e `proc<Transacao>_API_Update`, sincronizar o API Object com variáveis de `GetResponse`, `ErrorResponse` e `RestStatusCode`, expor `ErrorResponse` como saída pública dos três serviços, e usar Events do API Object para aplicar `&RestCode` em Get 200/404, Create 201/422 e Update 200/404/422; B055 próprio legado e variantes intermediárias B079 são reconhecidos como estados migráveis; a geração/reencontro foi validada na IDE em `NotaFiscal`; runtime HTTP foi validado em .NET Framework/SQL Server e .NET/PostgreSQL com List, Get, Create, Update, 400 e 404; `Location` de Create permanece pendente de confirmação nativa simples.
- B093 implementa a aplicação explícita do `Security Level` no API Object gerado: os writers de Business Component e List passam a emitir `[SecurityLevel(Authentication)]`, `[SecurityLevel(Authorization)]` ou `[SecurityLevel(None)]` em todos os serviços do API Object; o parser de contrato `ApiPlanServiceSourceContract.cs` e a integridade B067 em `ApiPlanMetadataFileWriter.cs` foram alinhados com suporte a migração conservadora de objetos anteriores, travados por testes unitários e validados pelo checker pré-push.

## Fixed

- contrato OpenAPI gerado passa a declarar `requestBody: required: true` em `Create` e `Update`, pela propriedade pública `Required` das variáveis de request do API Object, reaplicada também pelo writer de `List`, que recria essas variáveis em etapa posterior;
- `info.description` do contrato gerado deixa de expor a sentinela interna com IDs de backlog e passa a `REST API for <Transaction>, generated by Genexus Open API Builder.`; a sentinela de posse foi trocada sem lista de compatibilidade, então API gerada com a descrição anterior precisa ser apagada e regerada;
- `sdt_API_ErrorResponse` perde o nível `Errors`, que a geração nunca preenchia desde a retirada do `ErrorItem` em B071-B073/B079; o schema derivado `sdt_API_ErrorResponse.Errors_Error` deixa de aparecer no contrato público, e os documentos 12 e 27 receberam notas de revisão correspondentes;
- variável item das mensagens do Business Component em B071-B073/B079 passa a usar `Messages.Message, GeneXus.Common`, alinhando o preflight ao tipo GeneXus real;
- preenchimento detalhado de `ErrorResponse.Errors[]` em B071-B073/B079 foi retirado da geração atual porque a IDE rejeitou a validação da Procedure com `ErrorItem` de subestrutura SDT; o corpo de erro público permanece via `ErrorResponse.Code` e `ErrorResponse.Message`, e as mensagens do Business Component continuam registradas no Output técnico;
- preflight de B071-B073/B079 aceita `ErrorItem` de tentativas intermediárias próprias como migração conservadora quando o conjunto de variáveis bate e todas as demais variáveis continuam com tipo exato, permitindo regravar a Procedure para o contrato top-level atual;
- tela de preferências do wizard passa a exibir `listagem` e `metadata da API` nos defaults de geração e remove rótulos internos do usuário final;
- wizard remove IDs internos de backlog dos textos visíveis de confirmação, resumo e ajuda das abas, mantendo-os apenas nos logs técnicos;
- integridade B067 passa a aceitar `[Description]` seguida de anotações intermediárias, como `[RestMethod(POST)]`, antes da assinatura do serviço;
- integridade B067 passa a aceitar hashes de contrato planejado e de `ServiceGroupSource` esperado de variantes próprias anteriores com `RestPath` legado `{Chave}` ou anotações REST ainda incompletas, mantendo bloqueio para metadata externa ou contrato semântico divergente;
- parser de contrato B079 passa a exigir `Create` anotado como `[RestMethod(POST)]`, evitando aceitar API Object com verbo padrão incorreto;
- preflight de reexecução B071-B073/B079 passa a aceitar Sources próprios já gerados somente por equivalência canônica restrita, tolerando whitespace mas bloqueando código extra antes de sobrescrever;
- contrato B079 passa a expor `ErrorResponse` como `out` público em `Get`, `Create` e `Update`, mantendo a variante anterior com erro apenas interno como estado migrável.

- checkpoint preserva `B011` e `B012` antes de promover `B000`
- linha de corte do MVP passa a cobrir exaustivamente os itens necessários aos dez gates
- Sprints 3–7 distinguem ApiPlan, SDTs, Procedures/API/metadata, REST/segurança e operação conservadora
- referências de backlog, versões documentais e conflitos no wizard foram alinhadas
- layout inicial de `Src`, destino das evidências e ambiente-base de `B010` foram explicitados
- `Docs/Temp` foi protegido contra inclusão acidental no repositório público
- comandos experimentais B004 removidos do runtime após a validação do ciclo de vida do API Object
- comandos experimentais B005 removidos do runtime após a validação; o placeholder não operacional `Futura Primeira Opção` foi mantido apenas até o menu ganhar comandos permanentes do wizard
- comandos experimentais B006 removidos do runtime após a validação de persistência; a sonda permanece apenas como evidência histórica não invocada
- B031 limpa contrato em memória ao trocar a Transaction no B030 ou ao detectar ausência de seleção válida, evitando reutilização de decisões antigas por passos posteriores
- B031 desabilita partes da chave primária no `UpdateRequest`, preservando a regra de chave completa no `RestPath`
- B031 desabilita fórmulas em requests por API pública e bloqueia chave primária no `CreateRequest` até validação pública de autonumeração, sem reflexão em internos do SDK
- B032 sincroniza `Services base path` com `ApiName` até edição manual e o consumo por B033 foi validado posteriormente dentro do wizard único
- B090/B091 alinhados como representação provisória hardcoded em memória; os itens canônicos permanecem abertos até configuração explícita por KB em metadata persistente
- B040-B046 e B050-B053 agora executam preflight completo antes de qualquer `Save()`: SDTs validam colisões, descrições sentinela e tipos; Procedures validam todos os SDTs próprios/compatíveis e todas as Procedures planejadas antes de gravar a primeira
- Texto do wizard alinhado ao contrato provisório: preflight B040-B046/B050-B053 não promete validação de escopo físico enquanto ownership depender de descrição sentinela e metadata persistente ainda estiver pendente
- SDT writer ampliado para tipos públicos usados na validação composta (BITMAP, BINARY, BINARYFILE, VIDEO, AUDIO, GEOGRAPHY, GEOPOINT, GEOPOLYGON e GEOLINE)
- Correção pós-revisão B055 sincroniza API Object e Procedures: o Service Source parametrizado de Create/Update agora é acompanhado das variáveis reais do API Object, gravadas pela coleção pública de variáveis, com preflight do contrato antes das gravações
- Correção pós-revisão B055 preserva domínios nos SDTs próprios de request/response usando membros baseados nos atributos da Transaction; rerun em `GuiaPed` reconfigurou SDTs próprios e `Build All` passou com `apiGuiaPed`, `procGuiaPed_API_Create` e `procGuiaPed_API_Update` no U15
- Correção pré-push B055 força reconfiguração dos SDTs requeridos mesmo quando somente Business Component é aplicado, adia o realinhamento do Folder até depois do preflight principal e bloqueia Procedures B055 e API Object B055 reencontrados com variáveis extras, ausentes ou com tipo, atributo base, domínio ou objeto nomeado incompatível; Procedure já B055 sem variáveis não padrão também deixa de ser reparada silenciosamente
- Correção B056 faz B054 atualizar API Object B055 legado para a variante B055 com descrições, sem remover parâmetros de `Create`/`Update`, e mantém B054 legado atualizado para B054 com descrições
- Correção B060 alinha o preflight visual do wizard ao writer real para validar também `ownership.transactionGuid` e `ownership.apiGuid` antes de apresentar o File de metadata como reencontro válido
- Correção B060/B061 aceita reencontro de API Object B054 normalizado pela IDE com chamada qualificada de Procedure e grava o File de metadata no módulo da `Transaction`, validado em `SimulationResult` no módulo não-root `Entities`
- Rotina pré-push passa a executar o teste unitário do parser Service Source B054/B055 como gate automático
- Rotina pré-push passa a executar o teste unitário da integridade B067 como gate automático
- Correção B067 deixa o hash textual do Service Source como evidência e usa contrato semântico para bloqueio, preservando bloqueio de `[Description]` divergente
- Correção pré-push B070/B077 valida SDTs, Folder, Procedure, API Object e tipos planejados antes do primeiro `Save()` do `ApplyList`; depois reconfigura SDTs próprios mesmo sem a aba `SDTs` marcada e aceita reencontro semântico do API Object B070 para evitar falso bloqueio por normalização inofensiva do Service Source
- Cobertura automatizada pós-revisão B068 adicionada para serialização/parsing das preferências do wizard, escopo do preflight agregado por etapa e reencontro B070 quando somente literais de paginação mudam em Source próprio conhecido.
- Correção B068 remove gate indevido ao passar pela aba `Business Component`: navegar aba-a-aba não exige habilitar BC quando a etapa de aplicação via BC está desmarcada/bloqueada.
- Rotina pré-push passa a executar os testes unitários de preferências do wizard, escopo de preflight de escrita e política de reencontro B070.
- Parser de Service Source passa a validar o contrato B079 com Get parametrizado por `{&Chave}`, Create POST, Update PUT, `RestPath` explícito, `ErrorResponse` público, `RestStatusCode` interno e preservação do List B070 no mesmo API Object.
- Create/Update B079 passam a executar `Commit` após `Save()` bem-sucedido via Business Component e a validar membros obrigatórios comparando cada membro recebido com o valor default do mesmo membro em instância vazia do próprio SDT de request, sem comando C# embutido, retornando 400 antes do `Save()` quando o membro obrigatório chega ausente ou com o valor default do tipo (vazio, false ou 0).
- Rotina pré-push passa a executar o teste de coerência semântica de `Required`, que falha quando os textos do wizard, as mensagens de Output ou a documentação da frente voltam a descrever obrigatoriedade como presença de membro JSON, ou reduzem a limitação a uma formulação que omita `false` e `0`.
- Documentos de fundação reconciliados com a semântica efetivamente entregue em `B076`, cujo enunciado original tratava como problema único a distinção entre parâmetro ausente e valor vazio, `false` ou `0`. Nos filtros de `List` a distinção foi entregue conforme o enunciado, por `Json Null Serialization = JSON null`, e validada em runtime por `B070`/`B077`; no corpo de `Create` e `Update` ela é inviável sem comando `csharp`, e a geração passou a recusar com 400 o campo obrigatório não preenchido. O documento 06 recebeu nota de revisão separando os dois casos e preservando o enunciado original, os quatro caminhos descartados e a limitação assumida; o documento 24 e o gate transversal 6 dos documentos 09, 15 e 24 foram alinhados à mesma redação. A restrição de não expor campos públicos `Specified` no contrato permanece inalterada.
- Registro de decisões funcionais do MVP emendado pelo protocolo do próprio documento, que exige registrar nele as decisões revistas por validação técnica posterior: a `Emenda técnica — 2026-08-03` documenta o experimento previsto em `CreateRequest` e `UpdateRequest`, separa o caso resolvido dos filtros de `List` do caso revisto no corpo das requisições, e os trechos afetados receberam remissão apontando para ela, incluindo o gate técnico transversal 6.
- Duas divergências entre o contrato OpenAPI e o comportamento real foram caracterizadas como limitação do gerador GeneXus, com evidência registrada em `Docs/Implementation/2026-08-03-CONTRATO-OPENAPI-GAPS.md`: os códigos HTTP declarados por operação de API Object continuam restritos a `200` e `404`, porque o bloco de respostas é literal no template `Packages/RestDLTemplates/Swagger.Yaml.stg` da instalação; e o bloco `required:` dos schemas de request não é emitido mesmo com a propriedade `Required` gravada e persistida no item de SDT, o que foi comprovado por sonda temporária de releitura do modelo, removida antes do fechamento. Os documentos 12 e 27 registram as duas limitações e a marcação que produz efeito.
- `README.md` passa a documentar o requisito de ambiente para publicar a API gerada em IIS com o gerador .NET Framework: `PUT` não é entregue à aplicação enquanto o handler `ExtensionlessUrlHandler-Integrated-4.0` mantiver os verbos default do IIS, e a correção durável é no nó do servidor pelo IIS Manager, não no `web.config` do aplicativo gerado.

## Planned

- Confirmar suporte nativo simples para emitir `Location` no Create sem acoplamento frágil ao runtime gerado.

---

# [0.1.0] - 2026-04

## Added

- Criação oficial do projeto
- Definição de visão open source
- Coleção documental completa
- Estrutura base de diretórios
- Preparação documental para a futura fase de implementação

---

# Tipos de Mudança

- Added: nova funcionalidade
- Changed: alteração relevante
- Fixed: correção
- Removed: removido
- Deprecated: obsoleto
- Security: segurança

---

# Observação

Versões iniciais podem evoluir rapidamente durante a fase MVP.
