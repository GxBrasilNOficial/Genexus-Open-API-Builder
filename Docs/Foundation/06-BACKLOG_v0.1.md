# 06-BACKLOG_v0.1

## Backlog Inicial Priorizado do MVP

**Projeto:** Genexus Open API Builder
**Versão:** v1.1
**Base Primária:** 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md v1.1
**Dependência direta:** 05-ARQUITETURA_FUNCIONAL_MVP.md v1.1
**Objetivo:** converter requisitos e arquitetura em entregas incrementais rastreáveis.
**Idioma:** Português BR
**Público principal:** Agentes de IA + mantenedores humanos
**Data:** Abril/2026
**Última revisão:** Julho/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- transformar F04 + F05 em plano executável
- priorizar entregas reais
- seguir pipeline oficial
- reduzir risco inicial
- orientar execução assistida por IA

Este documento **não substitui requisitos**, **não congela roadmap**, **não define datas fixas**.

---

# 2. Taxonomia

| Código | Tipo | Significado |
|---|---|---|
| DP-F04 | Decisão oficial | Requisito aprovado no documento 04 |
| AF-F05 | Arquitetura Funcional | Implementação aprovada no documento 05 |
| BG-F06 | Backlog | Item planejado neste documento |
| HP-F06 | Hipótese | Depende validação prática |

---

# 3. Fontes e Rastreabilidade

## [F04]

04-REQUISITOS_MVP_Genexus_Open_API_Builder.md

## [F05]

05-ARQUITETURA_FUNCIONAL_MVP.md

---

# 4. Estratégia Oficial

Prioridade máxima:

1. validar viabilidade técnica oficial
2. gerar primeira API funcional
3. operar dentro da IDE
4. repetir sem erro
5. evitar exposição indevida
6. preparar evolução futura

[BG-F06]

---

# 5. Fases Oficiais (alinhadas ao F05)

| Fase | Base | Meta |
|---|---|---|
| 0 | Setup | Estrutura mínima e reproduzível |
| -1 | F05 | Pacote inicial de viabilidade do SDK |
| 1 | F04 8.1 | Seleção Transaction |
| 2 | F04 8.6 | Wizard mínimo com decisões obrigatórias |
| 3 | F04 8.5 | Criar contratos próprios da API |
| 4 | F04 8.2/F05 | Procedures e API Object |
| 5 | F04 8.3 | Organização e metadata |
| 6 | F04 8.2 | Serviços REST List/Get/Create/Update |
| 7 | F04 8.7 | Operação IDE |
| 8 | Segurança | Campos sensíveis, auditoria e Security Level |

[BG-F06]

---

# 6. Backlog Priorizado

As subseções abaixo preservam a numeração histórica dos pacotes. A ordem operacional vigente está na seção 9: primeiro a Fase 0, depois o pacote inicial da Fase -1.

## FASE -1 — Pacote Inicial de Viabilidade do SDK

Esta fase executa o primeiro pacote do spike técnico. Ela não concentra sozinha os dez gates transversais do MVP, que serão comprovados progressivamente até o fim da Sprint 7.

| ID | Item | Prioridade |
|---|---|---|
| B000 | Confirmar extensão carrega na IDE | Alta |
| B001 | Detectar KB ativa | Alta |
| B002 | Listar Transactions reais via API oficial disponível | Alta |
| B003 | Criar objeto simples de teste suportado pelo SDK | Alta |
| B004 | Validar criação, alteração, releitura e exclusão de API Object oficial | Altíssima |
| B005 | Validar criação, alteração, releitura e exclusão de Procedure, SDT, Folder e File | Altíssima |
| B006 | Validar persistência e releitura de metadata em File após reabrir KB | Altíssima |

### Gate

Se B004 falhar sem alternativa oficial viável:

> revisar ou encerrar a tese atual do produto.

---

## FASE 0 — Setup

| ID | Item | Prioridade |
|---|---|---|
| B010 | Localizar SDK e criar solution/projeto extensibility com build reproduzível | Alta |
| B011 | Estruturar pastas internas | Alta |
| B012 | Confirmar e aplicar as convenções de nomes já congeladas na documentação | Alta |

---

## FASE 1 — Seleção Transaction

| ID | Item | Prioridade |
|---|---|---|
| B020 | Detectar KB ativa | Alta |
| B021 | Listar Transactions elegíveis | Alta |
| B022 | Ler módulo da Transaction | Alta |
| B023 | Detectar objetos existentes | Média |
| B024 | Verificar se a Transaction pode operar como Business Component | Alta |
| B025 | Ler chave simples ou composta completa | Alta |

---

## FASE 2 — Wizard

| ID | Item | Prioridade |
|---|---|---|
| B030 | Passo 1 selecionar Transaction | Alta |
| B031 | Passo 2 selecionar serviços, campos e filtros essenciais | Alta |
| B032 | Passo 3 revisar segurança, paginação, ordenação, Services base path e RestPath | Alta |
| B033 | Validar campos obrigatórios | Alta |
| B034 | Cancelamento seguro | Média |
| B035 | Bloquear geração sem BC ou oferecer habilitação explícita | Alta |
| B036 | Exibir campos tecnicamente inadequados desabilitados com motivo | Alta |
| B037 | Configurar `Obrigatório no payload` para Create e Update | Alta |

---

## SPRINT 3 — Metadata + ApiPlan

| ID | Item | Prioridade |
|---|---|---|
| B038 | Montar `ApiPlan` inicial em memória, ainda não pronto para engine | Alta |

---

## FASE 3 — Criar SDTs

| ID | Item | Prioridade |
|---|---|---|
| B040 | Gerar `sdt<Nome>_API_CreateRequest` | Alta |
| B041 | Gerar `sdt<Nome>_API_UpdateRequest` | Alta |
| B042 | Gerar `sdt<Nome>_API_Response` | Alta |
| B043 | Gerar `sdt<Nome>_API_ListFilters` | Alta |
| B044 | Gerar `sdt<Nome>_API_ListResponse` com envelope | Alta |
| B045 | Gerar/reencontrar SDTs compartilhados em `GxOpenAPI` | Alta |
| B046 | Validar `sdt_API_ErrorResponse` e `sdt_API_Pagination` conforme documento 27 | Alta |
| B047 | Validar no YAML gerado rotas, métodos, SDTs, segurança, nomes `_API_` e gerador de cliente OpenAPI | Alta |

---

## FASE 4 — Procedures e API Object

| ID | Item | Prioridade |
|---|---|---|
| B050 | Gerar `proc<Nome>_API_List` | Alta |
| B051 | Gerar `proc<Nome>_API_Get` | Alta |
| B052 | Gerar `proc<Nome>_API_Create` | Alta |
| B053 | Gerar `proc<Nome>_API_Update` | Alta |
| B054 | Gerar API Object `api<Nome>` delegando para as Procedures | Alta |
| B055 | Validar uso via Business Component | Alta |
| B056 | Gerar `[Description]` por serviço, sem campo no wizard, com fallback de idioma registrado | Alta |

---

## FASE 5 — Organização

| ID | Item | Prioridade |
|---|---|---|
| B060 | Gravar metadata persistente em File | Alta |
| B061 | Aplicar mesmo módulo da Transaction | Alta |
| B062 | Aplicar nomenclatura padrão | Alta |
| B063 | Detectar colisões por metadata e por nome | Alta |
| B064 | Bloquear colisões incompatíveis sem criar `_v2` | Alta |
| B065 | Gravar Services base path, RestPath, campos, filtros, paginação, ordenação e Security Level na metadata | Alta |
| B066 | Diferenciar Folder específico criado de Folder reutilizado, reutilizando com aviso o Folder preexistente no contêiner correto | Alta — concluído (código + U15 2026-08-09: reuso com Description humana, contêiner incorreto, duplicidade e sentinela alheia) |
| B067 | Gravar descrições geradas e dados para detectar alteração manual posterior | Alta |

---

## FASE 6 — Serviços REST

| ID | Item | Prioridade |
|---|---|---|
| B070 | Gerar `List` com filtros, paginação e ordenação determinística | Alta |
| B071 | Gerar `Get` por chave simples ou composta | Alta |
| B072 | Gerar `Create` | Alta |
| B073 | Gerar `Update` com `PUT` e resposta 200 completa | Alta |
| B074 | Gerar paths e operationIds conforme convenção | Alta |
| B075 | Validar ausência de endpoint `Delete` enquanto o serviço estiver desmarcado (padrão; opt-in `B100`) | Alta |
| B076 | Distinguir filtro de `List` ausente de `false`, `0` e string vazia; recusar campo obrigatório não preenchido em `Create` e `Update` | Alta |
| B077 | Retornar paginação com `totalCount` e `totalPages` confiáveis | Alta |
| B078 | Validar `operationId` no padrão `apiNome.Serviço` | Alta |
| B079 | Validar códigos HTTP, corpos de resposta e `Location` opcional de `Create` | Alta |

### Nota operacional

Os quatro serviços obrigatórios são `List`, `Get`, `Create` e `Update`. O endpoint REST `Delete` é opt-in (`B100`, concluído em 2026-08-30), desligado por padrão. A remoção de uma API gerada pertence ao ciclo de vida da ferramenta e depende da metadata persistente. A distinção entre remover um **registro** (serviço `Delete`, via BC) e remover a **API gerada** (tooling, por metadata) permanece integralmente.

### Nota de revisão sobre `B076`

O enunciado original de `B076` era «Distinguir parâmetro ausente de `false`, `0` e string vazia», tratado como um problema único. A implementação mostrou que ele se divide em dois casos com desfechos diferentes.

**Filtros de `List`, na query string — resolvido conforme o enunciado original.** O SDT writer grava os membros nullable de `ListFilters` com a propriedade GeneXus `idJsonInclude=idJsonJsonNull`, correspondente a `Json Null Serialization = JSON null`. Sem ela, membro numérico não informado serializa como `0` e indicaria falsamente filtro aplicado. `B070`/`B077` validou o comportamento em runtime: sem filtro, `AppliedFilters.ContratoNumero=null`; com filtro, o valor informado.

**Membros obrigatórios no corpo de `Create` e `Update` — inviável como enunciado.** Revisto em 2026-08-03, no fechamento de `B071`-`B073`/`B079`, depois que quatro caminhos foram testados e descartados na IDE: comando `csharp` com `IsDirty`, que emite `spc0087` e foi recusado por decisão do projeto; `HttpRequest.ToString()` dentro da Procedure, onde o corpo bruto não chega; `&Sdt.IsDirty()` nativo, que não existe na linguagem; e `HttpRequest.ToString()` no evento `Before` do API Object, que devolveu `len=0` nos dois geradores porque o corpo já foi consumido pelo pipeline REST.

Conclusão registrada: o GeneXus não expõe presença de membro JSON no corpo de request sem comando `csharp`. A geração passou a validar preenchimento, comparando cada campo obrigatório com o valor default do mesmo membro em instância vazia do próprio SDT de request. `Create` e `Update` respondem 400 quando o obrigatório chega ausente ou com o valor default do tipo — vazio, `false` ou `0`.

Limitação assumida e documentada: campo obrigatório cujo valor legítimo seja igual ao default do tipo é recusado com 400. Os textos do wizard e as mensagens de Output de `B033` e `B037` foram corrigidos na mesma frente, porque ainda prometiam semântica de presença.

---

## FASE 7 — Operação IDE

| ID | Item | Prioridade |
|---|---|---|
| B080 | Integrar menu/contexto IDE | Alta — atendido em substância (Wizard + preferências); residual cosmético de nome/rótulo |
| B081 | Exibir relatório final interno | Alta — concluído (código + U15 2026-08-08/09; efeitos colaterais, Folder criado, reuso e dimensões da UI) |
| B082 | Mostrar tempo execução / sinal de vida | Média — **Fases A+B em código** (2026-08-31); Preview do Sync (2026-09-01); corte `0.1.0-alpha.7`. Etapa 1A **aceita** em 2026-09-03 (`Docs/Implementation/2026-09-03-B082-ETAPA-1A-ACEITE.md`). Residual 1B/2/3 no plano de 2026-09-02; **não** é a próxima ação única (`B108`). Não misturar com `B108` |
| B083 | Detectar conflito antes salvar | Alta — concluído (núcleo no preflight; residual UX nome/tipo/módulo/Folder validado U15 2026-08-08) |
| B084 | Bloquear overwrite silencioso | Alta — atendido (sem `_v2`) |
| B085 | Sincronizar com a Transaction usando metadata | Alta — concluído (código + validação U15 2026-08-08; SecurityLevel do Delete no Sync, U15 2026-08-31) |
| B086 | Remover API gerada por metadata, sem reverter BC | Alta — concluído (código + validação U15 2026-08-08/09; Folder criado e reutilizado) |
| B087 | Ancorar posse na metadata e liberar a `Description` do API Object | Alta — concluído (código + validação U15 2026-08-07) |
| B088 | Reconciliar restrições do template nativo Swagger.Yaml.stg (respostas declaradas 200/404 e emissão de required em schemas) | Alta — pré-Alpha separado; concluído (2026-08-10; limitação intransponível documentada) |
| B089 | Automatar validação de permissões granulares GAM por roles não-administradoras | Alta — pré-Alpha separado; concluído (2026-08-10; GAM Backoffice + HTTP Get 200 / Create 403) |
| B094 | Comprovar instalação por usuário externo sem clonar o repositório / sem `.bat` de administrador | Alta — Sprint 8 / evidência; concluído (2026-08-10; correção de captura 2026-08-11) |
| B095 | Leitura hierárquica recursiva da estrutura no SDK e modelo de domínio multinível (`ApiPlanLevel`) | Alta — Sprint 9 / Fase 1; concluído (2026-08-25; offline) |
| B096 | Geração de SDTs hierárquicos por subnível e por contrato, com regra de nomes e desambiguação | Alta — Sprint 9 / Fase 2; concluído (2026-08-26; offline) |
| B097 | Geração de código Business Component nas Procedures para subníveis, com substituição completa sob marcador `<Subnível>Replace` | Alta — Sprint 9 / Fase 3; concluído 2026-08-26 |
| B098 | Procedimento de List com contadores numéricos de subníveis diretos | Alta — Sprint 9 / Fase 4; concluído (2026-08-26; offline) |
| B099a | Interface do Wizard (UX) hierárquica: agrupamento por nível, dependência entre níveis, controle de contador e aviso de profundidade | Alta — Sprint 9 / Fase 5; concluído (2026-08-26; smoke U15 de 3 e 4 níveis com `Build All` nos dois environments) |
| B099v | Validação em runtime do que as Fases 2 a 5 emitiram: correção da agregação `count()` com PK composta herdada, smoke HTTP multinível nos dois environments e critério 9 (contrato OpenAPI publicado) | Alta — Sprint 9 / Fase 5-A; concluído (2026-08-28; smoke HTTP + OpenAPI) |
| B099b | Sincronização com metadata hierárquica (`schemaVersion` V2) e integridade | Alta — Sprint 9 / Fase 6; concluído (2026-08-28; smoke IDE Wizard/Sync/Remover) |
| B100 | Serviço `Delete` opt-in, com confirmação consciente, `SecurityLevel` próprio e documentação pública | Alta — Sprint 9 / após a Fase 7, corte `0.1.0-alpha.6`; concluído (2026-08-30; HTTP 401/404/200 nos dois environments, 422 no Framework); corte **publicado** em 2026-08-31 |
| B101 | Experimento: membro nullable no SDT de request para distinguir membro ausente de membro vazio | Média — candidato à Sprint 10; planejado |
| B102 | Repasse do texto emitido pelo Business Component na `Message` do `422`, com `Message` em `LongVarChar`, `Messages[]` como coleção tipada por `sdt_API_ErrorMessage`, filtro por mensagens de erro e opção de desligar por KB e por API | Alta — Sprint 9 / **primeiro item**; concluído (2026-08-24; gate HTTP nos dois environments) |
| B103 | Substituir o reconhecimento de source gerado por catálogo textual de variantes por carimbo de versão do contrato na metadata | Média — candidato à Sprint 10; planejado |
| B104 | Reorganizar `Src` conforme o layout da seção 5.7 da arquitetura, ou revisar o layout para refletir a organização real | Média — após a Sprint 9; planejado |
| B105 | Escolha do chamador sobre o detalhe do corpo de erro, restringindo o default da API e nunca ampliando | Média — Sprint 9 se houver folga, senão Sprint 10; planejado |
| B106 | Alinhar `Docs/Public/DEMO.md` à Alpha `0.1.0-alpha.4` | Baixa — higiene documental; concluído (2026-08-24; checkbox B102 e link das notas alinhados) |
| B107 | `Test-OpenApiClientContractValidity.ps1` validava YAML fora do repositório (falso verde / amarre à máquina) | Alta — antes da Fase 0; concluído (2026-08-25) |
| B108 | Preferências da KB só na criação; no reencontro checkboxes espelham a KB; desmarcar confirma e rebaixa/remove no Apply (Delete some com BC) | Alta — planejado e aprovado (2026-08-31); **próxima ação única** desde 2026-09-03; plano em `Docs/Implementation/2026-08-31-B108-PLANO-PREFERENCIAS-E-RETRACAO.md` |

**B106 — concluído em 2026-08-24.** O roteiro foi atualizado para a Alpha `0.1.0-alpha.4`, passou a registrar o checkbox de repasse das mensagens do Business Component e aponta para as notas da Alpha 4. A captura de Segurança foi explicitamente marcada como referência visual anterior; uma nova captura da UI permanece uma melhoria visual separada, sem bloquear a documentação textual.

**B107 — concluído em 2026-08-25.** O teste deixou de ler YAML publicado pelo Build da KB (`C:\KBs\...`): esse artefato pertence ao ambiente GeneXus, não ao pré-push deste repositório. A trava permanece offline sobre `Src/Domain/ApiPlan.cs` e, a partir de B096, também sobre `Src/Domain/ApiPlanSdtHierarchicalNaming.cs`, agora incluindo `sdt_API_ErrorMessage` além de `sdt_API_ErrorResponse` e dos padrões `_API_*` / serviços. Não foi absorvido pela Fase 0 (`Tests/GenerationBaseline/` cobre Source / Service Source / plano de SDT; a conferência de YAML publicado continua evidência pontual na IDE, prevista ao fim da Fase 4). Encontrado na revisão pré-push retroativa de 2026-08-24.

**B108 — preferências só na criação + checkboxes alinhados à KB com retração (planejado 2026-08-31; próxima ação única desde 2026-09-03).** O File `GxOpenApiBuilder_Settings` (B068) é default por KB. Hoje o Wizard aplica esses defaults também no reencontro (`ApplyPreference`, `_applyBusinessComponentWhenReady`, `ResolveApplyBusinessComponentAfterGenerationRefresh`). Observado na `NotaFiscal`: após Apply com “Completar REST via Business Component” desmarcado, a reabertura religou o checkbox porque a preferência da KB estava marcada — e desmarcar BC hoje só pula o writer; o Source REST/BC permanece; o B054 ainda bloqueia rebaixamento acidental.

Decisão aprovada na mesma data (escopo ampliado): (1) defaults da KB **somente na criação**; (2) no reencontro, checkboxes de geração **espelham a KB** (não o File); (3) Apply com etapa desmarcada **rebaixa** (List/BC) ou **remove** objetos (SDTs próprios, Procedures, API Object, metadata), com MessageBox ao desmarcar (default Não) e cascata de dependências; (4) Delete **some junto** com BC. Plano normativo: `Docs/Implementation/2026-08-31-B108-PLANO-PREFERENCIAS-E-RETRACAO.md`. ~~`B108` recuou em 2026-09-02 para a ação seguinte (1A do `B082`).~~ **Desde 2026-09-03** `B108` é de novo a **próxima ação única** (1A aceita).

### Nota operacional — B095–B099 (Suporte a Transactions com Subníveis), registrada em 2026-08-20 (revisada em 2026-08-22 e em 2026-08-23)

A frente de subníveis (B095–B099) absorve a expansão estrutural do MVP durante a Sprint 9 para viabilizar o uso da extensão em KBs de produção reais (10,2% das transações em `Gx_FabricaBrasil` são multinível, com até 3 níveis e múltiplos subníveis paralelos). O plano e decisões estão detalhados em `Docs/Implementation/2026-08-20-SUPORTE-TRANSACTIONS-SUBNIVEIS.md`, na `Emenda técnica — 2026-08-20` e na `Emenda técnica — 2026-08-23` do registro de decisões do MVP.

**Revisão de 2026-08-23.** A especificação passou por revisão dirigida e ganhou duas fases além de B095–B099: a **Fase 0** (linha de base de não regressão para transações planas, por arquivos de referência) e a **Fase 7** (ciclo de vida sob hierarquia: releitura de contrato existente, preferências do Wizard e inventário dinâmico de remoção). A metadata passa a `schemaVersion` V2 com leitura tolerante a V1 e conversão apenas no apply.

### Nota operacional — B100 a B105, registrada em 2026-08-23

Os itens nasceram de duas revisões da mesma data — a primeira sobre a especificação de subníveis, a segunda sobre o plano de trabalho da sprint — e **não pertencem** à frente de subníveis; cada um tem gate próprio. `B102` e `B100` executam dentro da Sprint 9; `B105` entra na Sprint 9 se houver folga; `B101` e `B103` são candidatos à Sprint 10; `B104` fica para depois da Sprint 9.

**B105 — detalhe do erro escolhido pelo chamador.** O consumidor pode pedir menos detalhe do que o default configurado na API, nunca mais: se o mantenedor desligou o repasse porque a API é pública, nenhum parâmetro de requisição pode religá-lo. Sem essa regra de teto, a opção de desligar de `B102` viraria decorativa. Fica **fora** de `B102` porque acrescenta parâmetro aos serviços `Create` e `Update`, muda a assinatura no API Object, muda o YAML publicado e pede caso de teste HTTP próprio — enquanto o que a linha de base da Fase 0 precisa ter estabilizado é apenas o default por API.

**B102 — urgente, primeiro item da Sprint 9; concluído em 2026-08-24.** O registro de decisões estabelece, desde 2026-07-14, que a `Message` de erro é texto legível produzido pela aplicação e que a extensão não traduz mensagens do Business Component. Até 2026-08-23 a geração não cumpria essa decisão: em falha de `Save()`, respondia `422` com o texto fixo `"Business rules rejected the request."` e descartava as mensagens do BC. O código de 2026-08-24 implementa o repasse; o gate HTTP nos dois environments da KB `wsEducacaoSpTeste` (`apiTeste`) fechou na mesma data. Decisões fechadas na revisão de plano de 2026-08-23:

- **`Message` passa a `LongVarChar`**, com truncamento explícito pela geração em cerca de 2K e reticência final, no lugar do `VarChar(256)` que cortava em silêncio. A mudança é no SDT compartilhado `sdt_API_ErrorResponse`, que atinge toda API — e por isso precisa acontecer aqui, antes da linha de base da Fase 0. Nota de coerência e comprimento declarado do tipo: ver o **gate humano** no documento 27.
- **Forma do corpo, fechada em 2026-08-24.** O experimento na IDE aceitou `Messages[]` tipado por `sdt_API_ErrorMessage`. A geração atual emite essa coleção e mantém `Message` top-level concatenada por `" | "`, truncada visivelmente em cerca de 2K. O ramo de concatenação como forma única não se aplica. Evidência: `Docs/Implementation/2026-08-24-B102-EXPERIMENTO-E-GATE-HTTP.md`. Gate HTTP fechado nos dois environments: texto da rule, acento, truncamento 2045 + `...` = 2048, `Messages[]` com `business_rule`, opção desligada com texto genérico, filtro de `Msg()` (tipo 0) fora do 422. YAML de `apiTeste` declara `Messages` e não emite `maxLength`. Reencontro Alpha: cobertura parcial.
- **Somente mensagens de erro** são repassadas. No gate HTTP, `Error()` entra como tipo 1 e `Msg()` como tipo 0; o Create copia só `Type == 1`. Não afirmar `Warning = 2`.
- **Ligado por padrão**, com aviso quando `SecurityLevel = None`. Desligado por padrão manteria na prática o defeito atual para quem não sabe que a opção existe.
- **Preferência em dois lugares:** default por KB no File `GxOpenApiBuilder_Settings` e escolha por API persistida na metadata, sem a qual reabrir o Wizard perderia a decisão. Isso acrescenta `PrototypeWizardPreferencesCodec.cs` e `ApiPlanMetadataFileWriter.cs` ao escopo do item.

**Requisito obrigatório de compatibilidade:** o formato atual do bloco de erro precisa ser acrescentado às variantes reconhecidas pelo writer, sob pena de toda API gerada na Alpha passar a ser vista como source estranho no reencontro. Executou **antes** da Fase 0, porque altera o código gerado para todas as transações e obrigaria a recapturar a linha de base no meio da sprint.

**B100 — serviço `Delete`; concluído em 2026-08-30.** Frente própria após a Fase 7. Contrato: `200` com a chave primária removida; `404` em registro inexistente (a não idempotência do status fica declarada na documentação); `422` com `validation_error` em recusa do BC, inclusive por integridade referencial, sem classificar erro por texto de mensagem. Quatro camadas anti acidente: opt-in com padrão desligado; confirmação consciente ao marcar, reaproveitando `RequiresGenerationConfirmation`, com aviso de que apagar o cabeçalho apaga todas as linhas filhas na mesma transação atômica; `SecurityLevel` próprio por serviço, com aviso destacado quando `None`; e recusa do BC respeitada, sem exclusão forçada. Documentação pública revisada no mesmo fechamento (README ×3 e `DEMO`). HTTP: 401/404/200 nos dois environments da `apiNotaFiscal`; 422 de integridade no Framework (PostgreSQL dispensado). Evidência: `Docs/Implementation/2026-08-30-B100-DELETE-OPT-IN.md`. **Nota de 2026-08-24 — o condicionamento antecipado foi revertido** (histórica): entre 2026-08-23 e 2026-08-24, o documento 15 e o critério de aceite do documento 27 passaram a condicionar o `Delete` ao serviço "estar marcado no Wizard" antes de a UI existir; ambos foram restaurados à forma absoluta até este fechamento.

**B103 — reconhecimento de source por versão de contrato.** Para decidir se uma Procedure da KB foi gerada pela extensão, `ApiPlanBusinessComponentWriter` compara o source atual contra um catálogo de formas conhecidas. **Medição de 2026-08-23**, contando as ramificações de `IsManagedCreateSource` e `IsManagedUpdateSource` e incluindo o formato vigente: **16** no `Create` e **11** no `Update` — ou seja, 15 e 10 formas históricas preservadas além da atual. O número é retrato daquela data, não compromisso de atualização: ele cresce por construção a cada mudança de emissor, e a Sprint 9 o empurra em três frentes ao mesmo tempo (`B102`, subníveis, `B100`). Cada mudança obriga a preservar mais uma versão literal do código gerado. O item propõe carimbar na metadata a **versão do contrato gerado**, de modo que o reconhecimento passe a perguntar por qual versão do emissor a API foi produzida, em vez de comparar texto contra N formas conhecidas. Fica fora da Sprint 9: mexeria no mesmo ponto que a linha de base da Fase 0 protege.

**B104 — organização de `Src`.** O layout declarado na seção 5.7 de `Docs/Foundation/05-ARQUITETURA_FUNCIONAL_MVP.md` reserva `Src/Core/` para orquestração, `Src/Infrastructure/` para adaptadores do SDK e `Src/UI/` para o wizard. As três permanecem vazias, com `.gitkeep`, por decisão consciente registrada em `B011`, enquanto o código real ficou em `Src/Extension/` — o wizard em `PrototypeWizardDialog.cs`, a orquestração em `Package.cs` e os adaptadores, escritores e comparadores em `Src/Extension/Diagnostics/`, que concentra a maior parte do código do projeto e abriga geração sob um nome que anuncia diagnóstico. O item decide entre mover o código para o layout declarado ou revisar o layout para refletir a organização real. Executa **após** a Sprint 9: mover arquivos durante a frente invalidaria a linha de base de não regressão da Fase 0.

**B101 — experimento de membro nullable.** A `Emenda técnica — 2026-08-03` assumiu como limitação que campo obrigatório cujo valor legítimo seja igual ao default do tipo é recusado com `400`. Os quatro caminhos descartados naquele fechamento atacavam o mesmo ponto: recuperar a presença **depois** que o SDT já foi materializado. O experimento ataca ângulo distinto — o tipo do membro —, verificando se o deserializador de entrada preenche `null` (e não `0`) em membro nullable ausente, o que tornaria a distinção testável nativamente com `IsNull()`, sem comando `csharp` e sem acesso ao corpo bruto. Escopo: uma transação, um SDT com membro numérico nullable, dois `POST` (um com `0` explícito, outro sem o membro), resultado registrado. Fica **fora** da Sprint 9: mexeria no contrato de request de todas as transações e colidiria com a linha de base da Fase 0. **Ponto público a revisar caso o experimento derrube a limitação:** a lista *Limitações honestas* dos três READMEs, que hoje declara a validação de obrigatoriedade por preenchimento "com a limitação conhecida de valores iguais ao default do tipo".

### Nota operacional — revisão da Sprint 7 / Fase 7, registrada em 2026-08-07 (fechada em 2026-08-09)

Após a Sprint 6, o pacote histórico “conflitos e reexecução” da Fase 7 ficou defasado: colisão, overwrite, `_v2` e integridade de metadata já foram entregues. A linha de corte obrigatória do marco **wizard funcional do MVP** foi concluída:

1. `B087` (posse sem travar `Description`) — concluído
2. `B086` (`Remover API gerada`) — concluído (U15 2026-08-08)
3. `B085` (`Sincronizar com a Transaction`) — concluído (U15 2026-08-08; preservação do SecurityLevel do Delete no Apply do Sync, U15 2026-08-31)
4. `B081` (relatório final pós-aplicação) — concluído (U15 2026-08-08/09; criação reportada como `Created=12`, incluindo o Folder)
5. residual de apresentação de `B083` — concluído (U15 2026-08-08)
6. alinhamento de Folder reutilizado à decisão do MVP (reutilizar `NomeOpenApi` preexistente no módulo correto com aviso) — concluído (U15 2026-08-09; caminho feliz e bloqueios para contêiner incorreto, duplicidade e sentinela alheia)
7. comprovação integrada dos dez gates — concluído (2026-08-09; `Docs/Implementation/2026-08-09-COMPROVACAO-DEZ-GATES-SPRINT7.md`); marco **wizard funcional do MVP concluído**; U14 residual na data do fechamento, confirmado depois (2026-08-12) por usuário externo

`B088` e `B089` ficaram **fora** do gate obrigatório da Sprint 7 e foram tratados **uma frente de cada vez** antes da Alpha. Em 2026-08-10, `B088` e `B089` foram concluídos e o pacote documental da Alpha `0.1.0-alpha.1` foi preparado. Em 2026-08-11 a documentação pública foi alinhada ao `B094` e a Alpha foi publicada; em 2026-08-12 o gate da Sprint 8 fechou com usuário externo (U14; issue #1). A Fase 2 do suporte `Gx18u13` foi concluída. Em 2026-08-16 fecharam a localização residual e a relitura estável do fingerprint B060; a Sprint 9 é a próxima ação operacional no checkpoint.

### Nota operacional — B087, registrada em 2026-08-03 (atualizada em 2026-08-05)

A `Description` do API Object acumula dois papéis: é copiada pelo gerador para `info.description` do contrato OpenAPI, portanto documentação pública, e é a sentinela de posse comparada por igualdade exata antes de qualquer reescrita.

Enquanto o texto era `Genexus Open API Builder B054 API Object - Transaction=... - Procedures=B050-B053`, a acumulação se protegia pela própria feiura: ninguém tentaria melhorar aquela string. Ao retirar o jargão interno do contrato público, a frente registrada em `Docs/Implementation/2026-08-03-CONTRATO-OPENAPI-GAPS.md` trocou o texto por uma frase de documentação legível — e, com isso, aumentou a chance de um usuário querer traduzi-la, encurtá-la ou personalizá-la. Qualquer edição faz a API deixar de ser reconhecida como própria e bloqueia a regeração.

B087 separa os dois papéis: a posse passa a ser verificada apenas pela metadata de integridade B067, e a `Description` fica livre para edição humana. O item é anterior à Alpha, porque a Alpha expõe a ferramenta a usuários que não conhecem essa armadilha.

**Pontos do código afetados pela sentinela de Description:**
- `ApiPlanApiObjectWriter.CreateOwnedDescription(apiPlan)`
- `ApiPlanGenerationStateReader.IsOwnedApiObject` e `IsCurrentB055ApiObject`
- `ApiPlanBusinessComponentWriter.IsCurrentB055ApiObject` e `IsB054ApiObject`
- `ApiPlanListProcedureWriter.IsCurrentB070ApiObject`
- `ApiPlanMetadataFileWriter.cs` (`descriptionSentinel`)

**Especificação do comportamento em B087 (Sprint 7):**
- A verificação de posse do API Object consultará obrigatoriamente o File de metadata B060/B067 (`ownership.apiName` e `PlannedContractHash`);
- Para preservar o reencontro conservador de APIs já geradas anteriormente sem metadata ou durante transição, o leitor aceitará fallback para a `Description` legível quando o File de metadata ainda não existir;
- Uma vez associado à metadata, alterações manuais na `Description` do API Object na IDE GeneXus não causarão perda de posse nem bloquearão regerações.

### Nota operacional — B088, registrada em 2026-08-04 (atualizada em 2026-08-10)

O gerador nativo de documentação REST OpenAPI do GeneXus (`Swagger.Yaml.stg`) gera um bloco estático declarando apenas códigos de resposta 200 e 404 por operação, sem refletir respostas como 201, 400 ou 422 devolvidas pelo pipeline REST em runtime. Adicionalmente, o gerador nativo não emite a lista `required:` nos schemas de request/response mesmo quando a propriedade `Required` está gravada e persistida nos itens de SDT/API Object.

B088 investigou extensibilidade do gerador de documentação REST e a inclusão de notas de compatibilidade para geradores de clientes a partir do YAML produzido.

**Critérios de aceite para conclusão de B088 (pré-Alpha separado; não gate da Sprint 7):**
1. *Investigação de Extensibilidade*: mapear e provar se o mecanismo de extensibilidade da IDE/SDK permite substituir ou interceptar o template `Swagger.Yaml.stg` sem modificar a instalação central do GeneXus;
2. *Ressalva e Compatibilidade*: caso a alteração do template nativo exija modificar `C:\Program Files (x86)\GeneXus` (o que é proibido), registrar formalmente a limitação intransponível em `Docs/Foundation/12-REGRAS_CRIACAO_API_OBJECTS.md` e `Docs/Foundation/27-CONTRATO_HTTP_ERROS_E_SDTS_COMPARTILHADOS.md`, definindo as orientações de consumo para o `openapi-generator-cli`;
3. *Definição de Concluído*: o item estará pronto com o relatório técnico de viabilidade e as ressalvas de compatibilidade incorporadas à documentação.

**Fechamento 2026-08-10:** os três critérios foram atendidos. Override inviável sem alterar a instalação; relatório em `Docs/Implementation/2026-08-10-B088-LIMITACOES-YAML-NATIVO.md`; ressalvas e orientação de consumo nos documentos 12 e 27. Sem mudança de código da extensão.

### Nota operacional — B089, registrada em 2026-08-04 (atualizada em 2026-08-10)

Quando um API Object opera sob `SecurityLevel = Authorization`, o GeneXus gera permissões granulares por serviço REST (ex: `apinotafiscal_Services_Get`, `apinotafiscal_Services_Create`, etc.). O teste granular exige role não-administradora com Get permitido e Create não atribuído.

B089 foi registrado como item de backlog pré-Alpha para evidenciar HTTP **403 Forbidden** quando a role autenticada não tem a permissão do serviço. O enunciado original privilegiava Programmatic GAM API; o fechamento usou as telas nativas do GAM Backoffice, suficientes para o aceite.

**Critérios de aceite e requisitos de ambiente para B089 (pré-Alpha separado; não gate da Sprint 7):**
1. *Ambiente*: dispor de ambiente de teste com GAM (`SecurityLevel = Authorization`) e configurar role `Role_GOAB_Test_Denied` com Get permitido e Create não atribuído (Backoffice ou Programmatic GAM API);
2. *Evidência HTTP 403*: executar requisições HTTP autenticadas com usuário vinculado a essa role restrita e comprovar retorno **403 Forbidden** no `POST` (Create) e **200 OK** no `GET`;
3. *Definição de Concluído*: o item estará pronto quando a resposta 403 estiver capturada e documentada em `Docs/Implementation/B093-SECURITY-LEVEL-APIPLAN-OBJETO.md`.

**Fechamento 2026-08-10:** os três critérios foram atendidos via GAM Backoffice (role `Role_GOAB_Test_Denied`, usuário `goab_role_denied`) nos environments .NET Framework/SQL Server e .NET/PostgreSQL: GET **200**, POST Create **403** (`code` 139); evidência em `Docs/Implementation/B093-SECURITY-LEVEL-APIPLAN-OBJETO.md` §4.A.3.D. Sem mudança de código da extensão. Programmatic GAM API não foi obrigatória.

### Nota operacional — B094, registrada em 2026-08-10 (atualizada em 2026-08-11)

B094 investigou, no GeneXus 18 U15, qual artefato distribuir e se um usuário externo consegue instalar a extensão Alpha sem clonar o repositório e sem os `.bat` elevados do mantenedor. Evidência em `Docs/Implementation/B094-INSTALACAO-APENAS-COM-A-DLL-SEM-CLONAR.md`. Sem mudança de código da extensão. A atualização da documentação pública (`README`, `INSTALL`, `Releases`, `CHANGELOG`), declarada fora do escopo do B094, foi concluída em 2026-08-11.

**Fechamento e correção:** em 2026-08-10 o relatório inicial errou o argumento decisivo por captura incompleta do `/install`. Em 2026-08-11, redo Add > Local + `/install` ativou marcada + menus (com UAC); captura em cmd já elevado registrou literalmente `Package 'GenexusOpenApiBuilder.Extension.dll' added`. Caminho sem elevação alguma continua não comprovado; observações de extensão desmarcada após Add > Local e marcação só na UI que não persiste permanecem válidas.

---

## FASE 8 — Segurança

| ID | Item | Prioridade |
|---|---|---|
| B090 | Classificar sensíveis por configuração explícita | Alta |
| B091 | Classificar auditoria separadamente | Alta |
| B092 | Configurar Security Level e GAM/None quando aplicável | Alta |
| B093 | Aplicar Security Level explicitamente em todos os serviços | Alta |

---

# 7. Critérios de Aceite por Itens-Chave

| ID | Aceite |
|---|---|
| B010 | SDK identificado por versão e origem; dependências localizáveis sem caminho absoluto da máquina; `Src/GenexusOpenApiBuilder.sln` e `Src/Extension/GenexusOpenApiBuilder.Extension.csproj` criados; comando e evidência registrados em `Docs/Implementation/B010-SDK-E-BUILD-MINIMO.md` |
| B011 | Estrutura interna confirmada conforme o layout do documento 05, seção 5.7 |
| B012 | Convenções congeladas confirmadas e aplicadas à estrutura inicial |
| B004 | Existe evidência prática de criação, alteração, releitura e exclusão de API Object oficial |
| B005 | Existe evidência prática de criação, alteração, releitura e exclusão de Procedure, SDT, Folder e File |
| B006 | Metadata em File sobrevive ao fechamento e reabertura da KB |
| B060 | Cliente grava metadata de geração persistente |
| B040 | Cliente gera `sdtCliente_API_CreateRequest` |
| B041 | Cliente gera `sdtCliente_API_UpdateRequest` |
| B042 | Cliente gera `sdtCliente_API_Response` |
| B070 | Existe `List` funcional |
| B071 | Existe `Get` funcional para chave simples e composta |
| B072 | Existe `Create` funcional |
| B073 | Existe `Update` funcional com HTTP 200 e Response completo |
| B075 | Sem endpoint `Delete` enquanto o serviço estiver desmarcado; marcado, vale o contrato `B100` |
| B076 | Filtros de `List` distinguem ausência de valores válidos `false`, `0` e string vazia; `Create` e `Update` respondem 400 quando campo obrigatório chega ausente ou com o valor default do tipo, conforme a nota de revisão da Fase 6 |
| B077 | ListResponse retorna `items`, `pagination` e `appliedFilters` |
| B078 | OperationIds seguem `apiCliente.List`, `apiCliente.Get`, `apiCliente.Create` e `apiCliente.Update` |
| B079 | Códigos HTTP e corpos respeitam o contrato; `Location` é emitido em `Create` quando o runtime permitir controle seguro |
| B080 | Menu/contexto acessível dentro IDE |
| B081 | Relatório lista criados/atualizados |

## 7.1 Rastreabilidade dos Gates Técnicos Transversais

| Gate | Evidência principal no backlog |
|---|---|
| 1. Carregamento no GeneXus 18 U14 ou posterior (U15 como validação inicial) | B000 |
| 2. Ciclo de vida dos objetos nativos pelo SDK | B003–B005 |
| 3. Delegação, propriedades e segurança do API Object | B004, B054, B056, B065, B074, B092 e B093 |
| 4. Contrato refletido no YAML gerado | B047, B054 e B070–B079 |
| 5. Create/Update via BC com chaves simples e compostas | B025, B052, B053, B055 e B071–B073 |
| 6. Filtro ausente distinto de vazio, `false` e zero; obrigatório não preenchido recusado com 400 | B037, B070 e B076 |
| 7. Códigos HTTP, corpos e `Location` | B046, B052, B053, B072, B073 e B079 |
| 8. List com filtros, períodos, paginação, totais e ordem determinística | B031, B043, B044, B050, B070 e B077 |
| 9. Metadata persistente e reconhecimento seguro | B006, B060, B063, B065–B067, B085–B087 |
| 10. Colisão, regeneração e remoção conservadoras | B063, B064 e B083–B086 |

Esses gates foram comprovados progressivamente nas Sprints 1–7 e estão aprovados no pacote integrado de 2026-08-09 (`Docs/Implementation/2026-08-09-COMPROVACAO-DEZ-GATES-SPRINT7.md`), com o marco **wizard funcional do MVP concluído**. U14 nesta máquina do mantenedor permanece sem bateria completa; em 2026-08-12 usuário externo confirmou carregamento + geração em U14 (issue #1). Em 2026-08-10, `B088` e `B089` fecharam as frentes pré-Alpha e o pacote documental `0.1.0-alpha.1` foi preparado; em 2026-08-11 a documentação pública foi alinhada ao `B094` e a Alpha foi publicada; em 2026-08-12 o gate da Sprint 8 fechou. A Fase 2 do suporte `Gx18u13` foi concluída; a localização residual e o fingerprint B060 fecharam em 2026-08-16, e a próxima ação operacional está no checkpoint (Sprint 9).

[BG-F06]

---

# 8. MVP Real (linha de corte)

Os itens e intervalos abaixo formam a linha de corte exaustiva do MVP. Um item omitido desta lista não é necessário para declarar o MVP concluído; qualquer mudança nessa interpretação exige atualizar esta seção e a matriz de gates em conjunto.

- Fase 0: B010–B012
- Fase -1: B000–B006
- Fase 1: B020–B025
- Fase 2: B030–B037
- Fase 3: B040–B047
- Fase 4: B050–B056
- Fase 5: B060–B067
- Fase 6: B070–B079
- Fase 7: B080, B081 e B083–B087 (B088 e B089 saem do gate obrigatório da Sprint 7 e ficam pré-Alpha separados; ver nota de 2026-08-07)
- Fase 8: B090–B093

`B082` Fases A+B estão em código desde 2026-08-31, integrando o corte `0.1.0-alpha.7`. Desde 2026-09-02 o residual (casca × B081 no Apply/Sync; `progress` na revalidação do Remove; índice incompleto) foi medido no plano `Docs/Implementation/2026-09-02-B082-PLANO-HARDENING-E-DESEMPENHO.md`; a Etapa 1A foi **aceita** em 2026-09-03 (`Docs/Implementation/2026-09-03-B082-ETAPA-1A-ACEITE.md`). Residual 1B/2/3 permanece nesse plano, sem ser a próxima ação única (`B108`). A `Empresa` em 2026-08-29 motivou o sinal de vida; Apply ~4,2 min em 2026-08-31; Preview do Sync (`5089` ms) e do Remover (`2525` ms, Não) em 2026-09-01.

`B088` e `B089` foram concluídos em 2026-08-10 e não bloqueiam o marco **wizard funcional do MVP** da Sprint 7 revisada.

[BG-F06]

---

# 9. Ordem Operacional por Dependência

1. Fase 0 completa (`B010`–`B012`)
2. Pacote inicial da Fase -1 completo (`B000`–`B006`)
3. Fases 1 e 2 (`B020`–`B037`) no protótipo navegável e não persistente
4. Planejamento de segurança (`B090`–`B092`) dentro do `ApiPlan`
5. Fase 3 até `B046`, criando os SDTs antes de seus consumidores
6. Fases 4 e 5 (`B050`–`B067`), criando Procedures, API Object e metadata
7. `B047`, Fase 6 (`B070`–`B079`) e aplicação da segurança em `B093`
8. Fase 7 revisada concluída (`B087`, `B086`, `B085`, `B081`, residual `B083`, Folder reutilizado e comprovação integrada dos dez gates; marco wizard funcional do MVP); `B088`/`B089` concluídos em 2026-08-10; pacote Alpha `0.1.0-alpha.1` preparado; documentação pública alinhada ao `B094` e Alpha publicada em 2026-08-11; gate Sprint 8 fechado em 2026-08-12 (usuário externo U14; issue #1); Fase 2 do suporte `Gx18u13` concluída; localização residual e fingerprint B060 encerrados em 2026-08-16; próxima ação operacional no checkpoint (Sprint 9)

`B047` é validado somente depois do API Object e dos serviços porque depende do YAML gerado pelo GeneXus; esse deslocamento de evidência não antecipa consumidores antes dos SDTs.

[BG-F06]

---

# 10. Fora do MVP

- IA generativa
- GraphQL
- OpenAPI avançado
- OAuth avançado
- analytics
- marketplace
- suporte Java
- múltiplos templates
- endpoint REST `Delete`
- reuso arbitrário de SDTs externos
- versionamento automático por `_v2`

[DP-F04]

---

# 11. Dependências Técnicas

| Item | Depende de |
|---|---|
| Fase 0 | Consolidação documental concluída |
| Fase -1 | Fase 0 concluída |
| Fases 1–8 | Pacote inicial do spike (`B000`–`B006`) aprovado |
| Wizard | Seleção Transaction |
| Criar SDT | Wizard |
| Procedures/API Object | Criar SDT |
| Organização/metadata | Procedures/API Object |
| Serviços REST | Organização/metadata |
| Operação IDE | Serviços iniciais |
| Segurança | Serviços iniciais |

[AF-F05]

---

# 12. Definição de Pronto

Todo item concluído deve:

- funcionar no fluxo real
- ser testável manualmente
- não quebrar fase anterior
- possuir commit rastreável
- atender critério explícito quando existir

[BG-F06]

---

# 13. Critérios de Parada

Parar e revisar se ocorrer:

- impossibilidade oficial de API Object
- corrupção de KB
- falhas imprevisíveis recorrentes
- dependência externa anti-tese
- arquitetura excessivamente complexa

[HP-F06]

---

# 14. Riscos Iniciais

| Risco | Mitigação |
|---|---|
| SDK limitado | spike técnico cedo |
| geração quebrar KB | ambiente teste |
| escopo inflar | seguir linha MVP |
| UX ruim | testar cedo |
| naming ruim | congelar no momento certo |

[HP-F06]

---

# 15. Uso Correto por Agentes de IA

## Pode assumir

- backlog segue ordem do F05
- a Fase 0 precede o pacote inicial do spike; os dez gates técnicos são comprovados progressivamente até o fim da Sprint 7
- itens Alta entram primeiro
- segurança mínima já está no MVP

## Deve tratar com cautela

- backlog muda após descoberta real do SDK
- itens podem virar subtarefas
- ordem pode ajustar por bloqueio técnico

---

# 16. Grau de Confiança

| Área | Grau | Evidência |
|---|---|---|
| Ordem geral execução | Alto | [F04][F05] |
| MVP definido corretamente | Alto | [F04] |
| Dependências técnicas | Alto | [AF-F05] |
| Estimativa futura esforço | Baixo | [HP-F06] |

---

# 17. Conclusão Objetiva

O backlog v1.1 prioriza:

Spike técnico → Transaction → Wizard → SDTs próprios → Procedures/API Object → metadata → List/Get → Create/Update → IDE → Segurança.

Tudo além disso fica para versões futuras.
