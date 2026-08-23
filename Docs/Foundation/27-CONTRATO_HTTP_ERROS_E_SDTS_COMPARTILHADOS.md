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
- `Message`
- `Errors[]`

Cada item de `Errors[]` deve conter:

- `Code`
- `Message`
- `Field`

Regras:

- `Code` principal é estável, em inglês e `snake_case`
- códigos principais previstos: `invalid_request`, `unauthorized`, `forbidden`, `not_found`, `conflict`, `validation_error`, `internal_error`
- `Message` e `Errors[].Message` são legíveis no idioma usado pela aplicação e pela KB
- a extensão não traduz mensagens produzidas pelo BC
- `Errors[].Code` preserva o identificador da mensagem do BC quando existir
- sem identificador do BC, `Errors[].Code` usa `business_rule`
- `Errors[].Field` usa exatamente o nome público da entrada quando houver associação confiável
- `Errors[].Field` não expõe variáveis internas de Procedures
- a extensão não tenta descobrir campo analisando texto de mensagem
- regras gerais, mensagens sem associação confiável ou regras envolvendo vários campos deixam `Field` vazio
- não há membro separado `Location` no contrato de erro do MVP
- não criar `sdt_API_ErrorDetail` separado; `Errors` é subestrutura interna de `sdt_API_ErrorResponse`

## Nota de revisão — 2026-08-03

O enunciado acima permanece registrado como o contrato pretendido. A geração entregue **não** contém `Errors[]`.

Em B071-B073/B079 a tentativa de preencher `Errors[]` por `ErrorItem` de subestrutura SDT foi descartada depois que a IDE manteve a rejeição da validação da Procedure. O erro público passou a ser top-level, com `Code` e `Message`; a geração atual não usa `msg()` como transporte para mensagens do Business Component. O SDT compartilhado, porém, preservou a subestrutura, que continuou aparecendo no contrato OpenAPI como array que nunca é preenchido.

A frente registrada em `Docs/Implementation/2026-08-03-CONTRATO-OPENAPI-GAPS.md` removeu o nível `Errors` de `sdt_API_ErrorResponse`. O SDT gerado passa a conter apenas `Code` e `Message`, e o schema derivado `sdt_API_ErrorResponse.Errors_Error` deixa de existir no YAML.

Continuam válidas as regras de `Code` principal, de idioma de `Message` e de não tradução das mensagens do BC. Ficam suspensas, até existir caminho viável de preenchimento, as regras específicas de `Errors[].Code`, `Errors[].Message` e `Errors[].Field`.

## Nota de revisão — 2026-08-23 — `Message` do `422` (`B102`)

A regra de idioma acima pressupõe que a `Message` carregue texto produzido pela aplicação. A geração entregue **não** cumpre isso: em recusa do Business Component ela emite o texto fixo `"Business rules rejected the request."` e descarta as mensagens do BC, de modo que uma rule `error` da KB nunca chega ao consumidor — que sabe apenas que foi recusado, sem saber por quê.

`B102` implementa o repasse, com salvaguardas: concatenação quando houver várias mensagens, limite de tamanho compatível com o membro do SDT, repasse apenas em falha de validação — nunca em erro de infraestrutura, onde o texto pode conter detalhe de banco — e opção no Wizard para desligar quando a API for exposta publicamente. O `Code` permanece `validation_error`, e a regra de decidir por `Code`, nunca pelo texto, continua valendo.

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

`Delete` é pós-MVP como endpoint REST.

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

O valor deve ser gravado explicitamente em todos os serviços. O MVP não oferece configuração diferente por serviço.

`SecurityPermission` granular por serviço fica para evolução posterior.

---

# 8. Erros Controlados e Runtime

Erros controlados pelas Procedures e pelo objeto `API` usam `sdt_API_ErrorResponse`.

Quando falhas do BC produzirem mensagens:

- o erro principal usa `Code = validation_error`
- a mensagem principal é um resumo
- `Errors[]` deriva das mensagens do BC conforme as regras deste documento; ver a nota de revisão da seção 3, que registra a retirada de `Errors[]` da geração entregue

Um spike deve verificar se erros interceptados pelo GAM ou pelo runtime antes da Procedure podem preservar o mesmo corpo. A uniformidade nesses casos não é prometida antes dessa validação.

---

# 9. Critérios de Aceite

- erros usam `sdt_API_ErrorResponse`
- paginação usa `sdt_API_Pagination`
- `sdt_API_Pagination` contém `TotalPages`, não `hasNextPage`
- `sdt_API_ErrorResponse` contém `Errors[].Code`, `Errors[].Message` e `Errors[].Field` — critério suspenso pela nota de revisão da seção 3; a geração entregue expõe erro top-level com `Code` e `Message`
- `Update` retorna 200 com Response completo
- `Create` retorna 201
- não há endpoint `Delete` no MVP
- status de erro são testáveis em cenário simples
