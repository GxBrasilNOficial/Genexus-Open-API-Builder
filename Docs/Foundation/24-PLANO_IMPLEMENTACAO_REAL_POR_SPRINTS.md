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

Solution mínima reproduzível, construída pelo mecanismo oficial disponível a partir do GeneXus 18 U14 e usada no spike. O build usa feed NuGet e MSBuild SDKs oficiais registrados por `B010`; o `B000` posterior validou carregamento e compatibilidade prática inicial no U15 local. A validação do limite inferior no U14 continua dependendo de colegas da comunidade, sem data definida e sem bloquear o MVP.

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

Gate aprovado no U15: o pacote inicial comprovou carregamento, leitura e ciclo de vida dos objetos necessários, incluindo persistência de metadata em File. A validação do limite inferior U14 continua pendente e não bloqueia o MVP.

[F09][SPR-F24]

---

## Gates técnicos transversais do MVP

Os gates abaixo são comprovados progressivamente nas Sprints 1–7. A Sprint 1 inicia essa comprovação com `B000`–`B006`; ela não precisa concluir antecipadamente capacidades que dependem do engine e dos contratos posteriores:

1. extensão carregou no GeneXus 18 U15; a confirmação do limite inferior U14 permanece pendente
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
- `B075`: comprovar ausência de endpoint Delete no MVP
- `B076`: distinguir filtro de `List` ausente de vazio, `false` e zero por `Json Null Serialization`, e recusar com 400 campo obrigatório que chegue ausente ou com o valor default do tipo, conforme a nota de revisão da Fase 6 no documento 06
- `B077`: comprovar `totalCount`, `totalPages` e `appliedFilters`
- `B078`: validar operationIds no padrão `apiNome.Serviço`
- `B079`: validar códigos HTTP, corpos e `Location`
- `B093`: aplicar o `Security Level` explicitamente em todos os serviços
- `B047`: validar no YAML gerado rotas, métodos, SDTs, segurança e nomes `_API_`

## Gate

Gate da Sprint 6 aprovado com ressalva em 2026-08-04: List, Get, Create e Update estão funcionais, seguros e refletidos corretamente no YAML gerado nos dois geradores (.NET Framework/SQL Server e .NET/PostgreSQL) com GAM OAuth2. Os elementos de contrato (rotas, métodos, operationIds no padrão `apiNome.Serviço`, SDTs sem o nível `Errors` e bloco `security` com `oAuthGXGAM` por serviço B093) estão validados com evidência registrada em `Docs/Implementation/2026-08-04-VALIDACAO-YAML-SPRINT6-EIXOS-SEGURANCA.md`. O cabeçalho `Location` no serviço `Create` é emitido nativamente via `HttpResponse.AddHeader(!"Location", ...)`. A ressalva limita-se às respostas HTTP declaradas no YAML nativo, mantidas restritas aos status 200 e 404 pelo template `Swagger.Yaml.stg` da instalação GeneXus, enquanto em runtime o serviço `Create` responde HTTP 201 e falhas respondem 401/404/400. A conformidade de identificadores `_API_` e `operationIds` para geradores de cliente OpenAPI está atestada pelo teste automatizado off-line `Test-OpenApiClientContractValidity.ps1`.

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

## Entregas obrigatórias restantes

Ordem acordada de execução:

1. `B087`: ancorar a posse do API Object na metadata de integridade e liberar a `Description` para edição humana — **concluído** (U15 2026-08-07)
2. `B086`: comando explícito `Remover API gerada` (preservar Folder reutilizado e `GxOpenAPI`; não reverter Business Component) — **concluído** (U15 2026-08-08/09)
3. `B085`: comando explícito `Sincronizar com a Transaction` com comparação/metadata e confirmação antes de gravar — **concluído** (U15 2026-08-08)
4. `B081`: relatório final pós-aplicação (criados / atualizados / bloqueados / avisos), sem depender só da Output técnica, incluindo efeitos colaterais do plano — **concluído** (U15 2026-08-08/09; criação com `Created=12` incluindo o Folder; Wizard `1200x912` e relatório adaptativo)
5. UX mínima de conflitos alinhada à decisão do MVP: para cada conflito, nome, tipo, módulo e Folder (`B083` residual de apresentação) — **concluído** (U15 2026-08-08)
6. alinhar Folder preexistente `NomeOpenApi` no módulo correto à decisão de reutilização com aviso — **concluído** (U15 2026-08-09; caminho feliz e bloqueios para contêiner incorreto, duplicidade e sentinela alheia; evidência `Docs/Implementation/2026-08-08-FOLDER-REUTILIZADO-COM-AVISO.md`)
7. comprovação integrada dos dez gates e declaração do marco **wizard funcional do MVP concluído** — **próxima ação**

## Fora do gate obrigatório desta sprint (pré-Alpha separados)

- `B088`: investigar/documentar limitações do template nativo `Swagger.Yaml.stg` (respostas declaradas só `200`/`404`; não emissão de `required:` nos schemas) — conclusão possível só com relatório de inviabilidade e notas de consumo
- `B089`: evidência HTTP `403` com role GAM não-administradora via automação Programmatic GAM API
- `B082`: tempo de execução no wizard (prioridade média; fora da linha de corte do MVP)

## Gate

Ciclo de vida conservador completo na IDE (posse, regeneração, sincronização e remoção sem overwrite indevido) e dez gates técnicos transversais comprovados.

Ao concluir esta sprint, o projeto atinge o marco **wizard funcional do MVP concluído**. Esse marco é pré-condição para iniciar a Alpha da Sprint 8.

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

[F18][SPR-F24]

---

# 15. Sprint 9 — Correções Reais

## Objetivo

Aprender com uso externo.

## Entregas

- bugs prioritários corrigidos
- docs melhores
- onboarding melhorado
- UX refinada

## Gate

Adoção melhora.

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
