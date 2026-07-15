# 26-CONTRATO_FILTROS_PAGINACAO_ORDENACAO

## Contrato Funcional para List no MVP

**Projeto:** Genexus Open API Builder
**Versão:** v1.0
**Base Primária:** [Registro de decisões funcionais do MVP — 2026-07-14](../Decisions/2026-07-14-REGISTRO_DECISOES_FUNCIONAIS_MVP.md)
**Objetivo:** definir o contrato transversal de filtros, paginação e ordenação para o serviço `List` gerado pelo MVP.
**Idioma:** Português BR
**Público principal:** mantenedores humanos, colaboradores técnicos e apoio por IA
**Data:** Julho/2026

---

# 1. Papel do Documento

Este documento é fonte normativa para o comportamento do serviço `List`.

Ele deve ser referenciado por:

- 07-UX_WIZARD_INICIAL.md
- 08-MODELO_DADOS_E_METADATA.md
- 10-ENGINE_GERACAO_OBJETOS.md
- 12-REGRAS_CRIACAO_API_OBJECTS.md
- 15-TESTES_VALIDACAO_E_QUALIDADE.md

---

# 2. Escopo do MVP

O MVP deve gerar `List` com:

- filtros por atributos elegíveis
- paginação
- ordenação determinística
- envelope de resposta com itens, paginação e filtros aplicados

Não faz parte do MVP:

- linguagem dinâmica de consulta
- filtros por subnível
- ordenação arbitrária informada por string livre
- busca textual global

---

# 3. Elegibilidade de Atributos

Um atributo só pode participar de filtros quando:

- pertence ao primeiro nível da Transaction
- é legível via Business Component
- tem tipo suportado pelo contrato de filtros
- não é campo interno de auditoria bloqueado por configuração da KB
- não é atributo inferido ou redundante sem leitura confiável no contexto do BC

Campos sensíveis podem existir como candidatos, mas devem iniciar desmarcados e com alerta explícito.

---

# 4. Tipos de Filtro

## Texto

Operadores mínimos:

- igual
- contém
- começa com

## Numérico

Operadores mínimos:

- igual
- maior ou igual
- menor ou igual
- intervalo

## Data e DateTime

Operadores mínimos:

- data inicial
- data final
- período fechado

## Boolean

Operador mínimo:

- igual

---

# 5. Paginação

Parâmetros públicos mínimos:

- `page`
- `pageSize`

Regras:

- `page` inicia em 1
- `pageSize` deve respeitar valor padrão e valor máximo definidos na configuração da geração
- valores inválidos retornam erro de validação

O envelope de resposta deve incluir dados suficientes para o consumidor entender a página retornada.

---

# 6. Ordenação

A ordenação deve ser determinística.

Regra mínima:

- usar ordenação estática definida pela geração
- aplicar desempate por chave primária completa

Não usar ordenação dinâmica por string livre no MVP.

---

# 7. ListResponse

`ListResponse` deve conter:

- `items`
- `pagination`
- `appliedFilters`

`items` contém elementos do SDT de resposta principal.

`pagination` deve usar contrato compartilhado documentado em `27-CONTRATO_HTTP_ERROS_E_SDTS_COMPARTILHADOS.md`.

`appliedFilters` deve refletir os filtros aceitos e efetivamente aplicados, sem inventar filtros omitidos pelo request.

---

# 8. Ausência vs Valor Vazio

A geração deve distinguir:

- membro ausente
- membro presente com string vazia
- membro presente com `false`
- membro presente com `0`

Não usar campos auxiliares `Specified` no contrato público do MVP.

---

# 9. Critérios de Aceite

- `List` compila e executa em cenário simples
- filtros aceitos aparecem em `appliedFilters`
- filtros omitidos não são tratados como valores enviados
- `false`, `0` e string vazia não são confundidos com ausência
- paginação respeita padrão e limite máximo
- ordenação é estável entre chamadas equivalentes
