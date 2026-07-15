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

SDTs mínimos:

- `sdt_API_ErrorResponse`
- `sdt_API_Pagination`

Esses SDTs pertencem ao gerador e devem ser reencontrados por metadata quando aplicável.

---

# 3. sdt_API_ErrorResponse

Estrutura mínima:

- `Code`
- `Message`
- `Errors[]`

Cada item de `Errors[]` deve conter, no mínimo:

- `Field`
- `Message`

`Code` deve ser estável e legível por máquina. Usar `snake_case`.

---

# 4. sdt_API_Pagination

Estrutura mínima:

- `page`
- `pageSize`
- `totalCount`
- `hasNextPage`

Se uma limitação técnica impedir `totalCount` confiável no MVP, o documento de implementação deve declarar a limitação e ajustar testes.

---

# 5. Status HTTP Mínimos

| Situação | Status |
|---|---|
| Sucesso em `List` | 200 |
| Sucesso em `Get` | 200 |
| Sucesso em `Create` | 201 ou 200, conforme viabilidade do API Object validada |
| Sucesso em `Update` | 200 |
| Request inválido | 400 |
| Não autenticado | 401 |
| Não autorizado | 403 |
| Registro não encontrado | 404 |
| Conflito de negócio ou concorrência | 409 |
| Erro de validação de regra de negócio | 422 |
| Erro inesperado | 500 |

`Update` não deve usar 204 no MVP; deve retornar Response completo.

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

Quando a KB usar GAM, o gerador deve explicitar a decisão de segurança por serviço:

- Authentication habilitada
- Security Level configurado
- None apenas quando confirmado e coerente com o contexto

---

# 8. Critérios de Aceite

- erros usam `sdt_API_ErrorResponse`
- paginação usa `sdt_API_Pagination`
- `Update` retorna 200 com Response completo
- não há endpoint `Delete` no MVP
- status de erro são testáveis em cenário simples
