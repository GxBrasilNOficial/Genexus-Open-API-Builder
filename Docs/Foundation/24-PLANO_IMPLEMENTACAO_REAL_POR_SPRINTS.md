# 24-PLANO_IMPLEMENTACAO_REAL_POR_SPRINTS.md

## Plano Oficial de Execução Prática do Projeto em Sprints Reais

**Projeto:** Genexus Open API Builder
**Versão:** v1.0
**Base Primária:** 23-RISCOS_LIMITACOES_E_NAO_OBJETIVOS.md v1
**Dependência direta:** 10-ENGINE_GERACAO_OBJETOS.md v1.0
**Relacionamento adicional:** 01 a 23 e contratos 26 a 28 consolidados
**Objetivo:** converter toda a documentação consolidada em um plano realista de implementação incremental, validável e executável.
**Idioma:** Português BR
**Público principal:** maintainer principal + contribuidores técnicos + agentes de IA
**Data:** Abril/2026
**Última revisão:** Agosto/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- transformar teoria em execução
- reduzir paralisia por excesso de planejamento
- organizar prioridades reais
- criar entregas incrementais
- acelerar primeiro release utilizável

Este documento **não exige metodologia rígida**, **não congela datas**, **não impede adaptação prática**.

As sprints que implementam `List`, contratos HTTP/erros e ciclo de vida devem seguir, respectivamente, `26-CONTRATO_FILTROS_PAGINACAO_ORDENACAO.md`, `27-CONTRATO_HTTP_ERROS_E_SDTS_COMPARTILHADOS.md` e `28-METADATA_REGENERACAO_SINCRONIZACAO_E_REMOCAO.md`.

---

# 2. Taxonomia

| Código | Tipo | Significado |
|------|------|-------------|
| MVP-F04 | Escopo base | Produto inicial |
| ENG-F10 | Engine | Núcleo técnico |
| OPS-F24 | Operação prática | Definição deste documento |
| SPR-F24 | Sprint | Ciclo curto |
| HP-F24 | Hipótese | Ajustável durante execução |

---

# 3. Fontes e Rastreabilidade

| Código | Fonte |
|------|-------|
| F04 | REQUISITOS_MVP |
| F07 | UX_WIZARD |
| F09 | INTEGRACAO_SDK |
| F10 | ENGINE_GERACAO |
| F15 | TESTES_QUALIDADE |
| F23 | RISCOS_LIMITACOES |

---

# 4. Estratégia Oficial

Executar em ciclos curtos:

1. construir base mínima
2. validar rápido
3. corrigir cedo
4. expandir com controle
5. publicar incrementalmente

[OPS-F24]

---

# 5. Regra Principal

Versão simples funcionando vale mais que arquitetura perfeita parada.

[OPS-F24]

---

# 6. Sprint 0 — Preparação

## Objetivo

Executar a Fase 0 do backlog (`B010`–`B012`) e deixar o terreno técnico reproduzível.

## Entregas

- `B010`: versão e origem do SDK registradas
- `B010`: dependências localizáveis sem caminho absoluto específico da máquina
- `B010`: `Src/GenexusOpenApiBuilder.sln` e `Src/Extension/GenexusOpenApiBuilder.Extension.csproj` criados conforme o layout do documento 05
- `B010`: comando e evidência de build mínimo registrados em `Docs/Implementation/B010-SDK-E-BUILD-MINIMO.md`
- `B011`: estrutura interna confirmada conforme o documento 05, seção 5.7
- `B012`: convenções de nomes congeladas confirmadas e aplicadas

## Saída esperada

Solution mínima reproduzível, construída pelo mecanismo oficial disponível a partir do GeneXus 18 U14 e usada no spike. O build usa feed NuGet e MSBuild SDKs oficiais registrados por `B010`; o `B000` posterior validou carregamento e compatibilidade prática inicial no U15 local. O limite inferior U14 foi confirmado em 2026-08-12 por usuário externo na Alpha (carregamento + geração).

[SPR-F24]

---

# 7. Sprint 1 — Spike SDK Real

## Objetivo

Executar o pacote inicial de viabilidade da Fase -1 (`B000`–`B006`).

## Entregas

- `B000` (concluído): extensão mínima carregou na IDE U15
- `B001` (concluído): KB ativa detectada no U15, em modo somente leitura
- `B002` (concluído): 10 Transactions reais listadas no U15 por API oficial, em modo somente leitura
- `B003` (concluído): Folder de teste criado no U15 com autorização explícita e sem alterar objetos existentes
- `B004` (concluído): ciclo de vida de API Object oficial validado no U15
- `B005` (concluído): ciclo de vida de Procedure, SDT, Folder e File validado no U15
- `B006` (concluído): metadata JSON em File preservou identidade, descrição e bytes após fechar e reabrir a KB

## Gate

Gate aprovado no U15: o pacote inicial comprovou carregamento, leitura e ciclo de vida dos objetos necessários, incluindo persistência de metadata em File. O limite inferior U14 foi confirmado depois (2026-08-12) por usuário externo na Alpha — carregamento + geração; ver checkpoint.

[F09][SPR-F24]

---

## Gates técnicos transversais do MVP

Os gates abaixo são comprovados progressivamente nas Sprints 1–7. A Sprint 1 inicia essa comprovação com `B000`–`B006`; ela não precisa concluir antecipadamente capacidades que dependem do engine e dos contratos posteriores:

1. extensão carregou no GeneXus 18 U15; o limite inferior U14 foi confirmado depois (2026-08-12) por usuário externo na Alpha
2. SDK cria, salva, reabre, altera e exclui objetos nativos `API`, `Procedure`, `SDT`, `Folder` e `File`
3. objeto `API` delega às Procedures e persiste `RestMethod`, `RestPath`, `Description` e `SecurityLevel`
4. YAML gerado pelo GeneXus reflete rotas, métodos, parâmetros, SDTs e nomes `_API_` (aprovado com ressalva das respostas HTTP declaradas 200/404 no YAML nativo)
5. `Create` e `Update` via BC funcionam com chave simples e composta, preservando regras e mensagens
6. filtro de `List` ausente é distinguido de vazio, `false` e zero, e campo obrigatório não preenchido é recusado com 400, sem membros públicos `Specified`
7. implementação controla códigos HTTP, corpo e `Location`, respeitando seu caráter opcional
8. `List` funciona com filtros opcionais, períodos, paginação, totalização e ordenação determinística
9. metadata em `File` sobrevive a fechar/reabrir a KB e reconhece objetos próprios
10. colisão, regeneração e remoção não sobrescrevem nem apagam objetos alheios

Se qualquer gate falhar sem alternativa nativa segura, revisar o desenho antes de declarar concluído o wizard funcional do MVP.

Não bloqueiam o MVP: associação visual sob a Transaction, objeto `Documentation` como fonte de metadata, uniformidade de erros interceptados antes da Procedure, migração assistida após renomear/mover Transaction, GeneXus Next, base `api/v1` e otimizações de build.

---

# 8. Sprint 2 — Protótipo Navegável do Wizard

## Objetivo

Validar navegação, captura de decisões e cancelamento seguro sem persistir nem gerar objetos.

## Entregas

- `B020`–`B025`: detectar KB, listar e selecionar uma Transaction, ler módulo, objetos existentes, BC e chave completa em modo somente leitura
- `B020`–`B025` (concluídos): KB ativa, Transactions elegíveis, módulo, objetos planejados, Business Component e chave primária completa verificados no U15 sem persistência e sem escrita pela extensão
- `B030` (concluído): Passo 1 do wizard selecionou `Transaction` pelo menu principal e pelo contexto no U15, mantendo estado apenas em memória
- `B031` (concluído): Passo 2 do wizard configurou serviços, campos e filtros essenciais no U15, mantendo decisões apenas em memória
- `B032` (concluído): Passo 3 do wizard revisou paths, segurança, paginação e ordenação no U15, acionado pelo contexto da `Transaction` e chamando B031 automaticamente quando necessário
- `B033` (concluído): campos obrigatórios foram incorporados ao wizard único aberto por B030 e validados manualmente no U15 sem persistência e sem escrita pela extensão
- `B034` (concluído): cancelamento seguro do wizard único foi validado manualmente no U15, descartando estado em memória sem `ApiPlan`, persistência ou escrita na KB
- `B035` (concluído): Business Component foi verificado no wizard único, com avanço bloqueado sem BC e habilitação persistente somente após confirmação explícita no U15
- `B036` (concluído): campos tecnicamente inadequados foram exibidos desabilitados, com motivo, contagens na Output e seleção impedida no wizard único no U15
- `B037` (concluído): obrigatoriedade técnica no payload foi consolidada para `CreateRequest` e `UpdateRequest` no wizard único no U15
- manter as escolhas apenas em memória
- avançar, voltar e cancelar sem alterar a KB, exceto pela habilitação explícita de `Business Component` em B035
- exibir resumo não persistente das escolhas
- não criar `ApiPlan` definitivo
- não chamar engine nem gerar objetos reais

## Gate

Fases 1 e 2 do backlog cobertas e validadas no protótipo navegável, com escolhas em memória, sem criação de `ApiPlan` e sem geração de objetos de API. A habilitação de `Business Component` é o único efeito persistente admitido nesta sprint e exige confirmação explícita do usuário.

[F07][SPR-F24]

---

# 9. Sprint 3 — Metadata + ApiPlan

## Objetivo

Transformar a Transaction e as escolhas do wizard em um `ApiPlan` progressivamente completo, ainda sem gerar objetos. B038 criou apenas o plano inicial em memória; a Sprint 3 também fixa os campos escalares iniciais de engine quando houver contrato suficiente, registra políticas provisórias de sensibilidade/auditoria e mantém pendências que pertencem à metadata persistente, B056 e à engine real; B092 foi resolvido no escopo de plano antes da entrada na Sprint 4.

## Entregas

- `B038` (concluído): wizard único montou `ApiPlan` inicial em memória no U15, cobrindo contrato, paths, segurança, paginação, ordenação, nomes planejados, required por request e precondição de `Business Component`, com `IsEngineReady=false` e marcadores `UNRESOLVED_B038_*` para campos ainda não resolvidos do contrato mínimo da engine, sem persistir metadata e sem gerar objetos na KB
- ler atributos
- identificar chave simples ou composta completa
- planejamento inicial de `B090` registrado no `ApiPlan`: wizard único classificou campos sensíveis por política inicial hardcoded em memória, preservando origem/razão; `B090` canônico permanece aberto até configuração por KB/metadata
- planejamento inicial de `B091` registrado no `ApiPlan`: wizard único classificou auditoria operacional separadamente por política inicial hardcoded em memória, preservando origem/razão; `B091` canônico permanece aberto até configuração por KB/metadata
- contrato preparatório de configuração por KB para `B090`/`B091` validado manualmente no `ApiPlan`: `ConfigScope='KnowledgeBase'`, `ConfigStatus='PendingPersistentMetadata'`, `PersistedMetadata=False` e `KbConfigured=False`, sem metadata persistente e sem geração; `B090`/`B091` canônicos permanecem abertos até regras carregadas de metadata persistente
- contrato mínimo da metadata persistente futura para `B090`/`B091` validado manualmente no `ApiPlan`: schema `B090B091_KB_FIELD_CLASSIFICATION_V1`, seção `fieldClassification`, membros `sensitiveExactNames`, `auditExactNames` e `auditSuffixes`, sem ler ou gravar File de metadata
- `B092` (concluído no escopo de plano): wizard único registrou `Security Level` no `ApiPlan` e resolveu a condição de segurança como `GAM_AUTHENTICATION_REQUIRED`, `GAM_AUTHORIZATION_REQUIRED_PENDING_PERMISSIONS` ou `NO_GAM_SECURITY_PUBLIC_API` conforme a escolha `Authentication`, `Authorization` ou `None`, sem aplicar segurança em objetos reais
- follow-up da Sprint 3 (validado manualmente): `GeneratorTarget='.NET'` como gerador prioritário inicial do MVP, `ConflictMode='BlockOnCollision'` para colisão externa/incompatível e `ReexecutionMode='Safe'`; engine real e aplicação efetiva de segurança permanecem pendentes, sem gerar objetos
- `B056` validado manualmente primeiro no escopo de plano: `ServiceDescriptions` resolvidas para os serviços selecionados, `ServiceDescriptionLanguage='English'`, `ServiceDescriptionLanguageSource='PendingKbLanguageApiValidation'`, `ServiceDescriptionFallbackUsed=true` e fallback técnico registrado, ainda sem aplicar `[Description]` em objeto `API` real naquele recorte preparatório
- módulo alvo
- montar decisões de filtros, payload, paginação, ordenação e segurança
- montar `ApiPlan`

## Gate

`ApiPlan` inicial consistente e sem escrita na KB, com sensibilidade, auditoria operacional, segurança e campos escalares de engine registrados explicitamente em memória. A representação provisória de B090/B091 cobre a política inicial hardcoded em memória, o contrato preparatório por KB já validado no `ApiPlan` e o contrato mínimo da metadata persistente futura; os itens canônicos permanecem abertos até configuração explícita carregada de metadata persistente real. O gate de prontidão para engine permanece aberto enquanto a engine real e a validação de geração estiverem pendentes; B092 já resolve a condição de segurança no plano, mas a aplicação efetiva de `SecurityLevel` nos serviços permanece para B093. `ConflictMode='BlockOnCollision'` governa colisão externa/incompatível e não substitui a reexecução conservadora de objeto próprio.

[F08][SPR-F24]

---

# 10. Sprint 4 — Engine Base e SDTs

## Objetivo

Realizar a primeira integração efetiva wizard → `ApiPlan` → engine, criando primeiro os contratos SDT dos quais Procedures e serviços dependerão.

## Entregas

- receber o `ApiPlan` produzido a partir das decisões do wizard e entregá-lo ao engine
- `B039`: preparar preview de engine SDT em memória, sem escrita na KB, antes da primeira criação real
- `B040`: criar `sdtCliente_API_CreateRequest`
- `B041`: criar `sdtCliente_API_UpdateRequest`
- `B042`: criar `sdtCliente_API_Response`
- `B043`: criar `sdtCliente_API_ListFilters`
- `B044`: criar `sdtCliente_API_ListResponse`
- `B045`: criar ou reencontrar os SDTs compartilhados em `GxOpenAPI`
- `B046`: validar `sdt_API_ErrorResponse` e `sdt_API_Pagination`
- registrar logs da primeira escrita real na KB

## Gate

SDTs próprios e compartilhados criados pelo engine a partir do `ApiPlan`, sem criar ainda Procedures nem API Object.

[F10][F13][SPR-F24]

---

# 11. Sprint 5 — Procedures, API Object e Metadata

## Objetivo

Criar as Procedures e o API Object sobre os SDTs já existentes, organizando e registrando todos os objetos por metadata.

## Entregas

- `B050`–`B053`: criar as Procedures de List, Get, Create e Update
- `B054`: criar `apiCliente` delegando para as Procedures
- `B055`: validar o uso via Business Component
- `B056`: gerar `[Description]` para os serviços selecionados
- `B060`: gravar o File JSON de metadata
- `B061`: manter os objetos no módulo da Transaction
- `B062`: aplicar as convenções de nomes congeladas
- `B063`: detectar colisões por metadata e por nome
- `B064`: bloquear colisões incompatíveis sem criar `_v2`
- `B065`: persistir paths, campos, filtros, paginação, ordenação e segurança na metadata
- `B066`: distinguir Folder criado de Folder reutilizado; U15 cobre reuso com Description humana e bloqueio conservador para contêiner incorreto, duplicidade e sentinela alheia
- `B067`: registrar descrições geradas para detectar alteração manual posterior
- preparar operationIds no padrão `apiNome.Serviço`
- não completar ainda o comportamento REST, reservado à Sprint 6

## Status atual

B040-B046, B050-B053, B054, B055, B056 e B060-B067 foram validados no U15. B054 cria ou reencontra `api<NomeBase>` e grava os serviços selecionados delegando para as Procedures; B055 substitui Create/Update skeleton por código via Business Component, com Source, Rules e variáveis reais persistidos por APIs públicas nas Procedures, e sincroniza o API Object com Service Source parametrizado e variáveis compatíveis. A validação manual cobriu chave simples (`Carga`) e chave composta (`TesteDate` + `TesteId`), incluindo `Build With This Only` das Procedures e do API Object `apiTeste`. Também cobriu atributos baseados em domínio nos SDTs de request/response em `GuiaPed`, com rerun do wizard reconfigurando SDTs próprios e `Build All` passando para `apiGuiaPed`, `procGuiaPed_API_Create` e `procGuiaPed_API_Update`. A correção pré-push posterior tornou B055 responsável por reconfigurar os SDTs requeridos mesmo quando somente Business Component é aplicado, adiou o realinhamento de Folder até depois do preflight principal e bloqueia Procedures B055 e API Object B055 reencontrados com variáveis extras, ausentes ou com tipo, atributo base, domínio ou objeto nomeado incompatível; Procedure já B055 sem variáveis não padrão também deixa de ser reparada silenciosamente. B056 aplica `[Description]` nos serviços reais do API Object a partir de `ServiceDescriptions`, preservando o Service Source parametrizado de B055 quando existente; a validação em `apiGuiaPed` passou por `Build All` e geração de documentação REST. B060 grava ou reencontra o File JSON inicial de metadata, persistindo `External File Name` via `BlobPart.FileName`, validando JSON próprio no preflight visual, bloqueando conteúdo inválido, identidade incompatível e colisão externa antes da escrita, e preservando descrições especiais B056 com aspas, barra invertida e caracteres incomuns. B061/B062 confirmam API Object, Procedures e SDTs específicos no Folder `<Transaction>OpenApi` dentro do módulo da `Transaction`, cobrindo `ContratoOpenApi` no `Root Module` e `SimulationResultOpenApi` no módulo não-root `Entities`; SDTs compartilhados permanecem em `GxOpenAPI`, File de metadata fica no módulo da Transaction por contrato do objeto File e nomes persistidos seguem as convenções congeladas. B063/B064 bloqueiam colisões externas, incompatíveis ou ambíguas antes do primeiro `Save()`, sem overwrite silencioso e sem `_v2`; o parser semântico B054/B055 ganhou cobertura unitária para vínculo serviço-Procedure, argumentos e módulo esperado. B065 persiste paths, filtros, paginação, ordenação e segurança na metadata. B066 persiste `transactionFolder.wasCreated=true` na criação e `false` no reencontro; no U15, o Folder correto foi reutilizado com Description humana e os casos de contêiner incorreto, duplicidade e sentinela alheia bloquearam antes de qualquer escrita. B067 grava integridade de descrições, contrato planejado e Service Source, bloqueando alteração manual posterior em `[Description]` antes do primeiro `Save()` e permitindo reencontro após restauração do valor gerado. A Sprint 5 fica concluída dentro do escopo de API Object, Procedures e metadata. A frente ainda não completa REST, segurança definitiva, códigos HTTP finais nem ciclo completo de regeneração; esses itens iniciam na Sprint 6.

## Gate

API Object, Procedures e metadata criados e reencontráveis, sem duplicar os SDTs já produzidos na Sprint 4, com integridade B067 validada para bloquear alteração manual posterior em descrições e contrato essencial.

[F10][F12][F28][SPR-F24]

---

# 12. Sprint 6 — Serviços REST e Segurança

## Objetivo

Completar o comportamento REST sobre os objetos já criados e aplicar explicitamente a segurança planejada.

## Entregas

- `B070`: completar List com filtros, paginação e ordenação determinística
- `B068`: validar preferências do wizard por KB, incluindo defaults de geração, serviços, segurança e paginação, antes de retomar Get
- `B071`: completar Get para chave simples ou composta
- `B072`: completar Create com HTTP 201 e `Location` quando controlável com segurança
- `B073`: completar Update com PUT, HTTP 200 e Response completo
- `B074`: aplicar paths e operationIds convencionados
- `B075`: comprovar ausência de endpoint Delete no MVP (Sprint 6; o `B100` passou a permitir Delete só como opt-in)
- `B076`: distinguir filtro de `List` ausente de vazio, `false` e zero por `Json Null Serialization`, e recusar com 400 campo obrigatório que chegue ausente ou com o valor default do tipo, conforme a nota de revisão da Fase 6 no documento 06
- `B077`: comprovar `totalCount`, `totalPages` e `appliedFilters`
- `B078`: validar operationIds no padrão `apiNome.Serviço`
- `B079`: validar códigos HTTP, corpos e `Location`
- `B093`: aplicar o `Security Level` explicitamente em todos os serviços
- `B047`: validar no YAML gerado rotas, métodos, SDTs, segurança e nomes `_API_`

## Gate

Gate da Sprint 6 aprovado com ressalva em 2026-08-04: List, Get, Create e Update estão funcionais, seguros e refletidos corretamente no YAML gerado nos dois geradores (.NET Framework/SQL Server e .NET/PostgreSQL) com GAM OAuth2. Os elementos de contrato (rotas, métodos, operationIds no padrão `apiNome.Serviço`, SDTs sem o nível `Errors` e bloco `security` com `oAuthGXGAM` por serviço B093) estão validados com evidência registrada em `Docs/Implementation/2026-08-04-VALIDACAO-YAML-SPRINT6-EIXOS-SEGURANCA.md`. O cabeçalho `Location` no serviço `Create` é emitido nativamente via `HttpResponse.AddHeader(!"Location", ...)`. A ressalva limita-se às respostas HTTP declaradas no YAML nativo, mantidas restritas aos status 200 e 404 pelo template `Swagger.Yaml.stg` da instalação GeneXus, enquanto em runtime o serviço `Create` responde HTTP 201 e falhas respondem 401/404/400. A conformidade offline dos padrões `_API_*` / SDTs compartilhados e da lista fechada de serviços (`List`, `Get`, `Create`, `Update`) em `ApiPlan.cs` está atestada pelo teste automatizado `Test-OpenApiClientContractValidity.ps1` (desde `B107` esse teste não lê YAML publicado nem valida `operationId` no artefato GeneXus; essa conferência permanece na evidência datada acima e nas validações pontuais na IDE).

[F12][F26][F27][SPR-F24]

---

# 13. Sprint 7 — Ciclo de vida operacional na IDE

## Objetivo

Fechar o ciclo de vida operacional do wizard na IDE e declarar o marco **wizard funcional do MVP concluído**, com os dez gates técnicos transversais comprovados.

## Revisão de escopo (2026-08-07)

O enunciado histórico “Conflitos e Reexecução” ficou defasado: preflight de colisão externa, bloqueio sem overwrite e sem `_v2`, reencontro conservador de objetos próprios e integridade de metadata já foram entregues e validados nas Sprints 5 e 6 (`B063`, `B064`, `B067` e fluxo do wizard). A entrada por menu/contexto (`B080`) também já existe em substância (`Wizard` + preferências).

O que permanece obrigatório nesta sprint é o ciclo de vida que o usuário ainda não tem: posse segura, remoção, sincronização, relatório pós-geração e fechamento dos gates.

## Já atendido antes desta sprint (não reabrir como frente principal)

- `B080`: menu principal e contexto com `Wizard` (e preferências por KB)
- `B083` / `B064`: detectar conflito / colisão antes do primeiro `Save()`
- `B084`: bloquear overwrite silencioso e nunca criar `_v2`
- reexecução Safe de objetos próprios reconhecidos
- integridade de metadata (`B067`) bloqueando Service Source / descrições incompatíveis

## Entregas obrigatórias (todas concluídas em 2026-08-09)

Ordem acordada de execução:

1. `B087`: ancorar a posse do API Object na metadata de integridade e liberar a `Description` para edição humana — **concluído** (U15 2026-08-07)
2. `B086`: comando explícito `Remover API gerada` (preservar Folder reutilizado e `GxOpenAPI`; não reverter Business Component) — **concluído** (U15 2026-08-08/09)
3. `B085`: comando explícito `Sincronizar com a Transaction` com comparação/metadata e confirmação antes de gravar — **concluído** (U15 2026-08-08; SecurityLevel do Delete no Sync, U15 2026-08-31)
4. `B081`: relatório final pós-aplicação (criados / atualizados / bloqueados / avisos), sem depender só da Output técnica, incluindo efeitos colaterais do plano — **concluído** (U15 2026-08-08/09; criação com `Created=12` incluindo o Folder; Wizard `1200x912` e relatório adaptativo)
5. UX mínima de conflitos alinhada à decisão do MVP: para cada conflito, nome, tipo, módulo e Folder (`B083` residual de apresentação) — **concluído** (U15 2026-08-08)
6. alinhar Folder preexistente `NomeOpenApi` no módulo correto à decisão de reutilização com aviso — **concluído** (U15 2026-08-09; caminho feliz e bloqueios para contêiner incorreto, duplicidade e sentinela alheia; evidência `Docs/Implementation/2026-08-08-FOLDER-REUTILIZADO-COM-AVISO.md`)
7. comprovação integrada dos dez gates e declaração do marco **wizard funcional do MVP concluído** — **concluído** (2026-08-09; evidência `Docs/Implementation/2026-08-09-COMPROVACAO-DEZ-GATES-SPRINT7.md`; U14 residual na data do fechamento, confirmado depois em 2026-08-12)

## Fora do gate obrigatório desta sprint (pré-Alpha separados)

- `B088`: investigar/documentar limitações do template nativo `Swagger.Yaml.stg` (respostas declaradas só `200`/`404`; não emissão de `required:` nos schemas) — **concluído** (2026-08-10; limitação intransponível; evidência `Docs/Implementation/2026-08-10-B088-LIMITACOES-YAML-NATIVO.md`)
- `B089`: evidência HTTP `403` com role GAM não-administradora — **concluído** (2026-08-10; GAM Backoffice + HTTP Get 200 / Create 403 nos dois environments; evidência `Docs/Implementation/B093-SECURITY-LEVEL-APIPLAN-OBJETO.md` §4.A.3.D)
- `B082`: sinal de vida no Wizard, Sync e Remover — **Fases A+B em código**; Preview do Sync (2026-09-01); corte `0.1.0-alpha.7` **publicado em 2026-09-01**; registro `Docs/Implementation/2026-08-31-B082-PLANO-UX-PROGRESSO.md`. Etapa 1A **aceita** em 2026-09-03 (`Docs/Implementation/2026-09-03-B082-ETAPA-1A-ACEITE.md`). Residual 1B/2/3 no plano `Docs/Implementation/2026-09-02-B082-PLANO-HARDENING-E-DESEMPENHO.md`; próxima ação única do checkpoint = `B108`.

## Gate

Ciclo de vida conservador completo na IDE (posse, regeneração, sincronização e remoção sem overwrite indevido) e dez gates técnicos transversais comprovados.

**Sprint 7 concluída em 2026-08-09.** O projeto atingiu o marco **wizard funcional do MVP concluído**. Esse marco é pré-condição para a Alpha da Sprint 8. Em 2026-08-10, `B088` e `B089` fecharam as frentes pré-Alpha. Em seguida o pacote documental `0.1.0-alpha.1` foi preparado; em 2026-08-11 a documentação pública foi alinhada ao `B094` e a Alpha foi publicada. Em 2026-08-12 o gate da Sprint 8 fechou com usuário externo em U14 (issue #1). O `Build All` da Transaction `Employee` foi concluído no U13 e, em 2026-08-13, a correção de elegibilidade `NoAccept` foi validada manualmente no U13 e teve a compatibilidade confirmada no U15 após remoção e recriação da API. A Fase 2 do suporte `Gx18u13` foi concluída. Em 2026-08-16 fecharam a localização residual, a relitura estável do fingerprint B060, o aborto na primeira aba do wizard único e o `Build All` pós-reencontro de `apiNotaFiscal` no U15; a Sprint 9 é a próxima ação operacional no checkpoint.

[F14][F28][SPR-F24]

---

# 13.1 KBs de Teste

A validação prática deve começar por uma KB menor, fora de produção, com backup disponível.

Depois, deve avançar para uma cópia de teste atualizada da KB principal.

Não executar validação diretamente na KB principal de produção.

---

# 14. Sprint 8 — Release Alpha Público

## Objetivo

Primeira versão aberta utilizável.

## Entregas

- README forte
- install guide
- changelog
- release tag
- demo curta

## Gate

Usuário externo testa.

**Pacote documental da Alpha `0.1.0-alpha.1` preparado em 2026-08-10:** README público, [Docs/Public/INSTALL.md](../Public/INSTALL.md), [Docs/Public/DEMO.md](../Public/DEMO.md), [Docs/Releases/0.1.0-alpha.1.md](../Releases/0.1.0-alpha.1.md), corte no CHANGELOG, versão do pacote alinhada e galeria em [Docs/Images/](../Images/). Em 2026-08-11 o INSTALL/README/notas de release foram alinhados à evidência `B094` (instalação só com a DLL). Em 2026-08-11 a Alpha foi publicada: tag `v0.1.0-alpha.1` no remoto e GitHub Release **pre-release** com a DLL anexada. Em 2026-08-12 o gate desta sprint fechou com usuário externo em GeneXus 18 U14 (Igor C. Menin; issue [#1](https://github.com/GxBrasilNOficial/Genexus-Open-API-Builder/issues/1); evidência [2026-08-12](../Implementation/2026-08-12-EVIDENCIA-USUARIO-EXTERNO-U14-ALPHA.md)). Sprint 9 absorve correções a partir do feedback.

[F18][SPR-F24]

---

# 15. Sprint 9 — Correções Reais e Suporte a Subníveis

## Objetivo

Aprender com o uso externo da Alpha e expandir o gerador para cobrir transações multinível (cabeçalho e subníveis) da KB de produção (`Gx_FabricaBrasil`).

## Entregas

- `B102`: Repasse do texto emitido pelo Business Component na `Message` do `422`, com `Message` em `LongVarChar` truncada pela geração, `Messages[]` como coleção tipada por `sdt_API_ErrorMessage`, repasse restrito a mensagens de erro, e opção de desligar por KB e por API (**primeiro item da sprint**; concluído em 2026-08-24, gate HTTP nos dois environments)
- Fase 0: linha de base de não regressão para transações planas, em duas camadas — arquivos de referência offline ligados ao checker mecânico e export XPZ dos SDTs na IDE, no início e no fim da sprint (**camada offline e captura IDE de início em 2026-08-25**; **conferência de fim fechada em 2026-08-28**, `Tests/GenerationBaseline/IdeXpz/CAPTURE-FIM.md` — ver `Docs/Implementation/2026-08-25-FASE0-LINHA-DE-BASE-NAO-REGRESSAO.md`)
- `B095`: Leitura hierárquica recursiva da estrutura no SDK e modelo de domínio multinível (`ApiPlanLevel`) (**concluído em 2026-08-25**; evidência `Docs/Implementation/2026-08-25-B095-LEITURA-HIERARQUICA.md`)
- `B096`: Geração de SDTs hierárquicos por subnível e por contrato, com regra de nomes e desambiguação (**concluído em 2026-08-26**; evidência `Docs/Implementation/2026-08-26-B096-SDTS-HIERARQUICOS.md`)
- `B097`: Geração de código Business Component nas Procedures para subníveis, com substituição completa sob marcador `<Subnível>Replace` (**concluído em 2026-08-26**; correção do tipo BC aninhado no mesmo dia; evidência `Docs/Implementation/2026-08-26-B097-BC-HIERARQUICO.md`)
- `B098`: Procedimento de `List` com contadores numéricos de subníveis diretos e `ListResponse_Item` condicionado (**concluído em 2026-08-26**; evidência `Docs/Implementation/2026-08-26-B098-LIST-CONTADORES.md`)
- `B099a`: Interface do Wizard com agrupamento por nível, dependência entre níveis, controle de contador e aviso de profundidade (**concluído em 2026-08-26**; smoke U15 de 3 e 4 níveis na `Teste` com `Build All` nos dois environments; evidência `Docs/Implementation/2026-08-26-B099a-WIZARD-HIERARQUICO.md`)
- `B099v` (Fase 5-A): validação em runtime do que as Fases 2 a 5 emitiram, antes que a Fase 6 grave metadata V2 sobre ele — correção da agregação `count()` com PK composta herdada, smoke HTTP multinível nos dois environments e o critério 9 (contrato OpenAPI publicado) (**concluído em 2026-08-28**; evidência `Docs/Implementation/2026-08-28-B099v-VALIDACAO-RUNTIME-MULTINIVEL.md`)
- `B099b`: Sincronização com metadata hierárquica (`schemaVersion` V2) e integridade (**concluído em 2026-08-28**; evidência `Docs/Implementation/2026-08-28-B099b-METADATA-HIERARQUICA-V2.md`)
- Fase 7: ciclo de vida sob hierarquia — releitura de contrato existente, preferências do Wizard e inventário dinâmico de remoção (**concluída em 2026-08-28**)
- `B100`: Serviço `Delete` opt-in, com as quatro camadas anti acidente (**concluído em 2026-08-30**; evidência `Docs/Implementation/2026-08-30-B100-DELETE-OPT-IN.md`)
- `B105`: escolha do chamador sobre o detalhe do corpo de erro, podendo apenas restringir o default da API — nesta sprint se houver folga, senão Sprint 10
- triagem do feedback da Alpha e documentação pública alinhada

**Ordem de execução:** `B102` (concluído) → Fase 0 (concluída: camada offline + captura IDE de início em 2026-08-25; conferência de fim em 2026-08-28, `CAPTURE-FIM.md`) → Fase 1/`B095` (concluída em 2026-08-25) → Fase 2/`B096` (concluída em 2026-08-26) → Fase 3/`B097` (concluída em 2026-08-26) → Fase 4/`B098` (concluída em 2026-08-26) → Fase 5/`B099a` (concluída em 2026-08-26) → Fase 5-A/`B099v` (concluída em 2026-08-28) → Fase 6/`B099b` (concluída em 2026-08-28) → Fase 7 (concluída em 2026-08-28) → `B100` (concluído em 2026-08-30). Detalhamento em [Docs/Implementation/2026-08-20-SUPORTE-TRANSACTIONS-SUBNIVEIS.md](../Implementation/2026-08-20-SUPORTE-TRANSACTIONS-SUBNIVEIS.md).

## Gate

1. Suporte prático a transações multinível comprovado na IDE, com `Build All` sem `spc0018` e chamadas HTTP reais nos environments `NETPostgreSQL155` e `NETFrameworkSQLServer004`.
2. Não regressão comprovada para transações de nível único contra a linha de base da Fase 0, no escopo que a própria Fase 0 declara sob comparação automática e sob conferência manual.
3. Toda issue aberta na Alpha até a **entrada da Fase 7** — que é a data de corte — **triada**: respondida e classificada como corrigida, convertida em item de backlog com identificador próprio, ou recusada com justificativa registrada. O corte é um marco do plano, e não uma data de calendário: data fixa ou expira antes do fim da sprint, deixando issues novas fora sem critério, ou é folgada a ponto de travar o fechamento por uma issue aberta na véspera. Issue aberta depois desse ponto é triada na Sprint 10.
4. **Repasse da `Message` (`B102`) comprovado por HTTP real nos dois environments (fechado em 2026-08-24, `apiTeste`):** transação com rule `Error()` responde `422` com o texto da rule em `Message` e `Messages[]` preenchido (`business_rule`); com a opção desligada, responde o texto genérico e o fonte gerado não chama `GetMessages()`; a `Message` sobrevive a acento e a mensagem longa, com truncamento visível em 2045 + `...` = 2048. `Msg()` no mesmo caso é emitido pelo BC (tipo 0) e não entra no corpo (filtro `Type == 1`). YAML publicado declara `Messages` e não emite `maxLength`. Reencontro Alpha: cobertura parcial.
5. **Serviço `Delete` (`B100`) — fechado em 2026-08-30:** HTTP real em `apiNotaFiscal`: `200` com a chave removida e `404` em inexistente nos environments `NETPostgreSQL155` e `NETFrameworkSQLServer004`; `422` `validation_error` por integridade referencial comprovado no Framework. O 422 no PostgreSQL foi dispensado por decisão operacional na mesma data (mesmo gerador; 200/404 já nos dois). Quatro camadas anti acidente na IDE: opt-in desligado por padrão, confirmação ao marcar, `SecurityLevel` próprio com aviso quando `None`, recusa do BC sem exclusão forçada. Em 2026-08-31 o Sync (`B085`) passou a preservar esse `SecurityLevel` próprio no Apply intencional. Evidência: `Docs/Implementation/2026-08-30-B100-DELETE-OPT-IN.md` e `Docs/Implementation/B085-SINCRONIZAR-COM-TRANSACTION.md`.

Os gates 4 e 5 exigem os **dois** environments porque corpo de erro e `DELETE` atravessam o pipeline REST que já mostrou comportamento divergente por gerador: o `404` do IIS em todo `PUT` só apareceu no .NET Framework, e a matriz do cabeçalho `Location` diferiu entre os dois.

Adoção é **sinal observado** no checkpoint, não condição de fechamento: ela não depende de trabalho do mantenedor e um gate que o mantenedor não governa vira carimbo automático ou trava indefinida. Bugs prioritários entram no gate apenas quando convertidos em itens de backlog com identificador, nunca como adjetivo.

## Publicação

Quatro cortes de release, e não dois:

- `0.1.0-alpha.4`, logo após `B102`. As notas trazem seção própria de **mudança de comportamento**, e não um item na lista de melhorias: quem hoje compara a string `"Business rules rejected the request."` para detectar recusa de regra passa a receber o texto real do Business Component; o schema do corpo de erro muda para toda API regenerada; e API já gerada só muda quando o Wizard for reaberto sobre ela. As mesmas notas declaram que a geração cobre apenas o primeiro nível da Transaction (**expectativa deste plano em 2026-08-23:** a documentação pública ainda não registrava essa limitação; na prática ela já constava dos três `README` e do `DEMO` desde `ecef3a6`, anterior a esta frente).
- `0.1.0-alpha.5`, ao fim da Fase 7, com os subníveis (**este corte**, 2026-08-30). As notas destacam o marcador `<Subnível>Replace`, ponto onde um consumidor desatento perde dados, e substituem a limitação de primeiro nível pela descrição do suporte com as limitações remanescentes: sem endpoints próprios de subnível, contadores só para subníveis diretos, profundidade acima de 4 avisa sem bloquear (escala `Empresa` com 13 subníveis passou no `Build All`) e impossibilidade de alterar netos sem substituir o nível pai.
- `0.1.0-alpha.6`, com `B100` (**publicado em 2026-08-31**; tag `v0.1.0-alpha.6` + GitHub Release pre-release). Corte próprio para desacoplar: amarrado ao mesmo corte dos subníveis, um atraso no `Delete` atrasaria a publicação de tudo; separado, ele desliza para a Sprint 10 sem renegociar o release anterior nem reabrir gate. Os três `README` e o `DEMO` deixaram de afirmar "Sem serviço `DELETE` no MVP" no fechamento de 2026-08-30.
- `0.1.0-alpha.7`, com `B082` (**publicado em 2026-09-01**; tag `v0.1.0-alpha.7` + GitHub Release pre-release). Corte próprio para UX de progresso, cancelamento cooperativo e indexação da KB, sem reabrir gate de subníveis nem `Delete`. O `DEMO` passa a descrever o diálogo de progresso e o botão Abortar.

Cada corte mantém o rito já estabelecido: CHANGELOG, notas de release nos três idiomas, `README`/`INSTALL`/`DEMO` alinhados, tag e GitHub Release pre-release. Desde o `0.1.0-alpha.3` o Release publica **dois assets DLL** — a canônica U14+ e a satélite `-gx18u13` —, com tabela de escolha e SHA-256 por asset; os quatro cortes mantêm os dois. Publicar um release só com a DLL canônica seria lido como abandono da linha U13, ou levaria alguém a instalar a errada.

O custo é honesto: quatro cortes são quatro vezes o rito completo. O ganho é que nenhum atraso de um item segura a publicação do anterior.

[SPR-F24]

---

# 16. Sprint 10 — Beta Estável

## Objetivo

Produto confiável inicial.

## Entregas

- regressões reduzidas
- fluxo principal sólido
- comunidade ativa mínima
- releases previsíveis
- `B101` (candidato): experimento de membro nullable para distinguir membro ausente de membro vazio, que pode rever a limitação assumida na `Emenda técnica — 2026-08-03`
- `B103` (candidato): reconhecimento de source gerado por carimbo de versão do contrato na metadata, no lugar do catálogo textual de variantes históricas

## Gate

Caminho para v1.

[SPR-F24]

---

# 17. Ritmo Recomendado

| Tipo de Sprint | Duração |
|------|---------|
| pessoal intenso | 1 semana |
| realista paralelo | 2 semanas |
| voluntário comunitário | 3 semanas |

[HP-F24]

---

# 18. O Que Não Fazer Durante Execução

Evitar:

- refatorar cedo demais
- feature creep
- sprint gigante
- reescrever sem motivo
- ignorar feedback real

[OPS-F24]

---

# 19. Uso Correto por Agentes de IA

## Pode assumir

- entrega incremental vence perfeccionismo
- gates evitam desperdício
- feedback externo acelera maturidade

## Deve tratar com cautela

- datas rígidas
- excesso de escopo
- dependências não validadas

---

# 20. Conclusão Objetiva

Projeto cresce quando planejamento vira sprint.

E sprint vira software funcionando.
