# 27-CONTRATO_HTTP_ERROS_E_SDTS_COMPARTILHADOS

## Contrato HTTP, Erros e SDTs Compartilhados

**Projeto:** Genexus Open API Builder
**Versão:** v1.0
**Base Primária:** [Registro de decisões funcionais do MVP — 2026-07-14](../Decisions/2026-07-14-REGISTRO_DECISOES_FUNCIONAIS_MVP.md)
**Objetivo:** definir o contrato HTTP mínimo e os SDTs compartilhados gerados no Root Module.
**Idioma:** Português BR
**Público principal:** mantenedores humanos, colaboradores técnicos e apoio por IA
**Data:** Julho/2026

---

# 1. Papel do Documento

Este documento é fonte normativa para:

- status HTTP mínimos
- estrutura de erro
- SDTs compartilhados
- Folder `GxOpenAPI`
- resposta de paginação

Ele deve ser referenciado por 10, 12, 15 e 26.

---

# 2. SDTs Compartilhados

O MVP deve criar ou reencontrar os SDTs compartilhados no Root Module, dentro do Folder `GxOpenAPI`.

O conjunto de SDTs compartilhados do MVP é fechado:

- `sdt_API_ErrorMessage`
- `sdt_API_ErrorResponse`
- `sdt_API_Pagination`

Esses SDTs pertencem ao gerador e devem ser reencontrados por metadata quando aplicável.

Não criar no MVP:

- `sdt_API_ErrorDetail`
- `sdt_API_ListOptions`
- `sdt_API_SuccessResponse`
- SDTs genéricos para filtros aplicados
- SDTs genéricos para períodos
- SDTs genéricos para ordenação
- SDTs genéricos para auditoria
- SDTs genéricos para links de paginação

Novos compartilhados só entram futuramente quando tiverem estrutura idêntica entre APIs, significado independente da Transaction e benefício concreto de reutilização.

---

# 3. sdt_API_ErrorResponse

Estrutura obrigatória:

- `Code`
- `Message` (`LongVarChar` com `Length = 2097152`, truncada pela geração em cerca de 2K com reticência final)
- `Messages[]` (coleção tipada pelo SDT compartilhado `sdt_API_ErrorMessage`)

Cada item de `Messages[]` deve conter:

- `Code`
- `Message` (`LongVarChar` com `Length = 2097152`)

`Field` **não** integra o contrato entregue do MVP.

Regras:

- `Code` principal é estável, em inglês e `snake_case`
- códigos principais previstos: `invalid_request`, `unauthorized`, `forbidden`, `not_found`, `conflict`, `validation_error`, `internal_error`
- `Message` e `Messages[].Message` são legíveis no idioma usado pela aplicação e pela KB
- a extensão não traduz mensagens produzidas pelo BC
- `Messages[].Code` preserva o identificador da mensagem do BC quando existir
- sem identificador do BC, `Messages[].Code` usa `business_rule`
- a extensão não tenta descobrir campo analisando texto de mensagem
- não há membro separado `Location` no contrato de erro do MVP
- não criar `sdt_API_ErrorDetail` separado; `Messages` é coleção tipada por `sdt_API_ErrorMessage`, não subestrutura aninhada no próprio `sdt_API_ErrorResponse`

## Nota de revisão — 2026-08-03

O enunciado normativo original previa `Errors[]` com `Code`, `Message` e `Field`. A geração entregue **não** contém `Errors[]`.

Em B071-B073/B079 a tentativa de preencher `Errors[]` por `ErrorItem` de subestrutura SDT foi descartada depois que a IDE manteve a rejeição da validação da Procedure. O erro público passou a ser top-level, com `Code` e `Message`; a geração atual não usa `msg()` como transporte para mensagens do Business Component. O SDT compartilhado, porém, preservou a subestrutura, que continuou aparecendo no contrato OpenAPI como array que nunca é preenchido.

A frente registrada em `Docs/Implementation/2026-08-03-CONTRATO-OPENAPI-GAPS.md` removeu o nível `Errors` de `sdt_API_ErrorResponse`. Naquela frente o SDT gerado passou a conter apenas `Code` e `Message`, e o schema derivado `sdt_API_ErrorResponse.Errors_Error` deixou de existir no YAML. A coleção `Messages[]` entra depois, no fechamento de `B102` — ver Acréscimo e Gate humano abaixo.

Continuam válidas as regras de `Code` principal, de idioma de `Message` e de não tradução das mensagens do BC. Ficam suspensas, até existir caminho viável de preenchimento, as regras específicas de `Errors[].Code`, `Errors[].Message` e `Errors[].Field`.

**Correção de premissa — 2026-08-23.** "Caminho viável" era leitura forte demais da evidência. O que a IDE recusou foi **subestrutura aninhada** dentro do próprio SDT (`sdt_API_ErrorResponse.Error`). Membro coleção tipado por um SDT **separado** é outro mecanismo — o mesmo que já funciona em `ListResponse.Items` — e nunca foi testado no corpo de erro. `B102` executa esse experimento: aceito, o corpo ganha `Messages`, coleção de `sdt_API_ErrorMessage` preenchida a partir de `GetMessages()`, e as regras suspensas voltam a ser decidíveis; recusado, as mensagens vão concatenadas em `Message` e a recusa fica registrada como evidência, agora sim do mecanismo certo. Em qualquer dos dois desfechos `Message` permanece top-level e preenchida, e nenhuma das formas correlaciona mensagem com índice de linha de subnível.

**Remissão — 2026-08-24.** O experimento descrito no parágrafo anterior foi executado e aceito: as regras de `Errors[]` permanecem fora do contrato entregue, e o caminho viável fechado por `B102` é `Messages[]` tipado por SDT separado, sem `Field`. Não ler os dois parágrafos acima como estado aberto. Ver Acréscimo e Gate humano abaixo.

## Nota de revisão — 2026-08-23 — `Message` do `422` (`B102`)

A regra de idioma acima pressupõe que a `Message` carregue texto produzido pela aplicação. Na data desta revisão, a geração **ainda não** cumpria isso: em recusa do Business Component ela emitia o texto fixo `"Business rules rejected the request."` e descartava as mensagens do BC, de modo que uma rule `error` da KB nunca chegava ao consumidor — que sabia apenas que foi recusado, sem saber por quê. O `B102` foi definido para fechar esse gap.

O `B102` foi especificado com o repasse e estas salvaguardas: repasse apenas em falha de validação — nunca em erro de infraestrutura, onde o texto pode conter detalhe de banco —, somente mensagens de **erro** do Business Component, e opção para desligar quando a API for exposta publicamente. O `Code` permanece `validation_error`, e a regra de decidir por `Code`, nunca pelo texto, continua valendo.

**Complemento de 2026-08-23 — tipo, limite e forma.** O membro `Message` passa de `VarChar(256)` para `LongVarChar`, com truncamento explícito pela geração em cerca de 2K e reticência final: tipo sem limite não é conteúdo sem limite, e o corte silencioso do `VarChar` era pior do que um truncamento visível. A opção de desligar fica **ligada por padrão**, com aviso quando `SecurityLevel = None`, com default por KB no File de preferências e escolha por API na metadata. A forma do corpo — uma `Message` concatenada ou um membro coleção `Messages` — depende do experimento descrito na nota seguinte.

**Remissão — 2026-08-24.** O experimento e o comprimento declarado fecharam na mesma data; ver Acréscimo e Gate humano abaixo. Não ler este complemento isolado como estado aberto.

**Acréscimo — 2026-08-24.** O experimento da coleção foi aceito na IDE. O código de `B102` passou a gerar o repasse: `Message` top-level concatenada e membro coleção `Messages` tipado por `sdt_API_ErrorMessage`, preenchido a partir de `GetMessages()`. O ramo de concatenação como forma única não se aplica. Gate HTTP fechado na mesma data nos dois environments (`apiTeste`).

**Gate humano — fechado em 2026-08-24.** Duas decisões de `B102` não se resolviam por leitura de código nem por teste offline. Foram observadas na IDE por sonda temporária, numa KB de teste. Evidência, método e resultado bruto em `Docs/Implementation/2026-08-24-B102-EXPERIMENTO-E-GATE-HTTP.md`. **Não repetir o experimento.**

1. **Forma do corpo — coleção aceita.** A IDE aceitou o membro coleção `Messages` tipado pelo SDT **separado**, com `isCollection=true` e `collectionItemName` preservados após `Save` e releitura por GUID. O corpo de erro ganha `Messages`, tipado por `sdt_API_ErrorMessage`; o ramo de contingência da concatenação como forma única **não** se aplica. `Message` permanece top-level e preenchida, concatenada por `" | "`, para não quebrar consumidores da Alpha. A recusa de 2026-08-03 fica esclarecida: era a subestrutura aninhada, não o conceito de coleção.
2. **Comprimento declarado do `LongVarChar` — o SDK não determina.** Os valores `0`, `2048`, `1048576` e `2097152` foram todos aceitos e devolvidos **sem normalização**, com `typeObserved=LONGVARCHAR` em todos. Não há valor imposto pela plataforma, e a escolha é decisão de design. **Fica decidido `Length = 2097152`**, por alinhamento ao tamanho convencional de `LongVarChar` no GeneXus, para `sdt_API_ErrorResponse.Message` e para o membro de texto de `sdt_API_ErrorMessage`.

Não confundir os dois limites. O truncamento em cerca de 2K acontece no **código GeneXus gerado** (`SubStr`); o `Length` é **declaração ao SDK** na criação do SDT. São independentes. O YAML publicado de `apiTeste` (os dois environments, 2026-08-24) **não** emite `maxLength` em nenhum membro — zero ocorrências no arquivo inteiro. `Message` e o texto de `sdt_API_ErrorMessage` saem como `type: string`. A decisão `Length = 2097152` permanece como declaração ao SDK e é inconsequente para o contrato OpenAPI publicado pelo gerador nativo.

**Gate HTTP — fechado em 2026-08-24.** KB `wsEducacaoSpTeste`, Transaction `Teste`, `apiTeste`, environments `.NET`/PostgreSQL e `.NET Framework`/SQL Server.

| Caso | Evidência |
|---|---|
| Ligado, 422 com texto da rule | os dois environments |
| Acento UTF-8 | os dois |
| Truncamento em 2045 + `...` = 2048 | os dois |
| `Messages[]` preenchido, `business_rule` | os dois; schema publicado com `type: array` e `$ref` para `sdt_API_ErrorMessage` |
| Desligado → texto genérico | os dois, e o fonte gerado sem `GetMessages()` |
| Warning excluído | os dois, com o aviso comprovadamente emitido |
| Reencontro de API Alpha | cobertura parcial: Wizard na `NotaFiscal`/`apiFiscalPublica` em estado de reencontro, cancelado sem escrita; catálogo mecânico de variantes Alpha; regravação `Updated=14`, `Blocked=0` na `Teste` |

**Tipos de mensagem — só o que está provado.** A Procedure gerada compara `gxTpr_Type == 1` a partir de `MessageTypes.Error`. No `Teste_BC`, `Error()` entra como tipo 1 e `Msg()` como tipo 0. Não afirmar `Warning = 2`: essa equivalência não foi medida nesta frente. A leitura do objeto de exemplo do GAM distribuído pelo GeneXus (`MessageTypes.Error = 1`) foi evidência independente na revisão por pares, não repetida nesta sessão.

**Nota de coerência.** O Wizard classifica `LongVarChar` como tipo tecnicamente inadequado para **atributos da Transaction** entrando no payload. `Message` é membro fixo do contrato de erro, gerado pela extensão, e a regra não se aplica a ele.

A regra da seção 5, de usar `422` e não presumir `409` quando não é possível distinguir a natureza da recusa, permanece e passa a valer também para a recusa por integridade referencial no serviço `Delete` (`B100`).

---

# 4. sdt_API_Pagination

Estrutura obrigatória dentro da KB:

- `Page`
- `PageSize`
- `TotalCount`
- `TotalPages`

Nomes externos em JSON/OpenAPI:

- `page`
- `pageSize`
- `totalCount`
- `totalPages`

`TotalCount` é obrigatório e confiável depois da aplicação dos filtros.

`hasNextPage` não integra o contrato do MVP.

---

# 5. Status HTTP Mínimos

| Situação | Status |
|---|---|
| Sucesso em `List` | 200 |
| `Get` encontrado | 200 |
| `Get` inexistente | 404 |
| Sucesso em `Create` | 201 |
| Sucesso em `Update` | 200 |
| Request inválido | 400 |
| Não autenticado | 401 |
| Não autorizado | 403 |
| Conflito seguramente identificado | 409 |
| Rejeição por regras de negócio via BC | 422 |
| Erro inesperado | 500 |

`Update` não deve usar 204 no MVP; deve retornar Response completo.

Se não for possível distinguir com segurança conflito de regra de negócio, usar `422`, não presumir `409`.

O cabeçalho `Location` no `Create` é gerado nativamente via `&HttpResponse.AddHeader(!"Location", ...)` com a URL relativa do recurso criado, sem exigir DLL externa ou `External Object`.

## Nota de revisão — B088 (2026-08-10)

A tabela desta seção descreve o **runtime HTTP** do MVP. O YAML OpenAPI gerado nativamente pelo GeneXus para API Object **não** declara esse conjunto: o template `Packages/RestDLTemplates/Swagger.Yaml.stg` emite tipicamente só `200` e `404` por operação. O bloco `required:` dos schemas também não é emitido a partir da propriedade `Required` em item de SDT (`TypeDefinitions.Yaml.stg`).

`B088` comprovou que não há extensão segura dessa documentação sem alterar a instalação GeneXus. Relatório: `Docs/Implementation/2026-08-10-B088-LIMITACOES-YAML-NATIVO.md`. Remissão complementar no documento 12.

Orientação de consumo:

- confiar nesta tabela e no Source/Events gerados (`&RestStatusCode` / `&RestCode`), inclusive `List`, não no bloco `responses:` do YAML, para saber quais status o MVP pode devolver;
- `openapi-generator-cli` permanece útil para rotas, métodos, `operationId`, security e schemas básicos; o mapa de status do cliente gerado fica incompleto frente ao runtime;
- agentes de IA que leiam o YAML devem ser avisados da limitação e cruzar Procedures/Events (ou C# pós-Build) e este contrato; `401`/`403` do GAM e falhas de infra podem não aparecer no Source da Procedure;
- não usar `Description` do API Object como substituto da lista estruturada de status.

---

# 6. Operações MVP

Operações públicas:

- `List`
- `Get`
- `Create`
- `Update`
- `Delete` (opt-in, padrão desligado; `B100` concluído em 2026-08-30)

**B100:** quando marcado, responde `200` com a chave removida, `404` em registro inexistente e `422` com `Code = "validation_error"` na recusa do Business Component. Evidência: `Docs/Implementation/2026-08-30-B100-DELETE-OPT-IN.md`. A revisão de 2026-08-23 que descrevia o serviço como futuro permanece só como histórico.

---

# 7. Segurança

O wizard possui um único campo `Security Level`, aplicado inicialmente a todos os serviços.

KB com GAM:

- opções oficiais `Authentication`, `Authorization` e `None`
- `Authentication` selecionada por padrão
- `Authorization` exige permissões GAM coerentes antes da geração definitiva
- `None` exige confirmação explícita

KB sem GAM:

- somente `None`
- aviso explícito de API sem autenticação

O valor deve ser gravado explicitamente em todos os serviços. Os serviços `List`/`Get`/`Create`/`Update` compartilham o `Security Level` do wizard. **Remissão `B100` (2026-08-30):** o `Delete` tem `SecurityLevel` próprio na aba Segurança, fora das preferências da KB.

`SecurityPermission` granular por serviço fica para evolução posterior.

---

# 8. Erros Controlados e Runtime

Erros controlados pelas Procedures e pelo objeto `API` usam `sdt_API_ErrorResponse`.

Quando falhas do BC produzirem mensagens:

- o erro principal usa `Code = validation_error`
- a mensagem principal é o texto das rules concatenado em `Message` (ou o texto genérico quando o repasse está desligado)
- `Messages[]` deriva das mensagens de **erro** do BC conforme as regras da seção 3; `Errors[]` permanece fora da geração entregue

Um spike deve verificar se erros interceptados pelo GAM ou pelo runtime antes da Procedure podem preservar o mesmo corpo. A uniformidade nesses casos não é prometida antes dessa validação.

---

# 9. Critérios de Aceite

- erros usam `sdt_API_ErrorResponse`
- paginação usa `sdt_API_Pagination`
- `sdt_API_Pagination` contém `TotalPages`, não `hasNextPage`
- `sdt_API_ErrorResponse` contém `Code`, `Message` (`LongVarChar` 2097152) e `Messages[]` tipado por `sdt_API_ErrorMessage` (`Code`, `Message`); `Field` e `Errors[]` ficam fora do contrato entregue — ver seção 3 e o fechamento de `B102`
- `Update` retorna 200 com Response completo
- `Create` retorna 201
- `Delete` é opt-in (`PrototypeWizardContract.ServiceNames` inclui `Delete`): desmarcado, não há rota; marcado, valem `200`, `404` e `422` (`B100`, 2026-08-30)
- status de erro são testáveis em cenário simples
