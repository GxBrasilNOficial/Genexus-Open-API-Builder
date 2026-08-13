# 13-REUSO_E_GERACAO_SDTS.md

## Regras Oficiais de Criação e Reencontro de SDTs no MVP

**Projeto:** Genexus Open API Builder
**Versão:** v1.0
**Base Primária:** 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md v1.1
**Dependência direta:** 08-MODELO_DADOS_E_METADATA.md v1.0
**Relacionamento adicional:** 10-ENGINE_GERACAO_OBJETOS.md v1.0 / 11-CONVENCOES_NOMES_E_OUTPUTS.md v1.0 / 12-REGRAS_CRIACAO_API_OBJECTS.md v1.0
**Objetivo:** definir como o produto cria SDTs próprios da API e reencontra apenas SDTs previamente gerados pelo próprio produto.
**Idioma:** Português BR
**Público principal:** Agentes de IA + mantenedores humanos
**Data:** Abril/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- evitar reuso arbitrário de SDTs externos
- acelerar regeneração segura por metadata
- manter consistência estrutural
- permitir reencontro de contratos próprios
- reduzir lixo técnico na KB

Este documento **não define endpoints REST**, **não trata UX**, **não substitui contrato de metadata**.

O contrato de metadata, regeneração, sincronização e remoção está em `28-METADATA_REGENERACAO_SINCRONIZACAO_E_REMOCAO.md`.

---

# 2. Taxonomia

| Código | Tipo | Significado |
|------|------|-------------|
| DP-F04 | Decisão oficial | Requisito aprovado |
| MD-F08 | Modelo de dados | Metadata e estruturas |
| ENG-F10 | Engine geração | Execução técnica |
| NOM-F11 | Naming oficial | Convenções |
| SDT-F13 | Regras SDT | Definição deste documento |
| HP-F13 | Hipótese | Pode evoluir |

---

# 3. Fontes e Rastreabilidade

| Código | Fonte |
|------|-------|
| F04 | REQUISITOS_MVP |
| F08 | MODELO_DADOS_E_METADATA |
| F10 | ENGINE_GERACAO_OBJETOS |
| F11 | CONVENCOES_NOMES_E_OUTPUTS |
| F12 | REGRAS_CRIACAO_API_OBJECTS |

---

# 4. Estratégia Oficial

No MVP:

1. criar SDTs próprios da API
2. reencontrar apenas SDTs próprios por metadata persistente
3. não reutilizar SDTs externos por compatibilidade estrutural
4. não alterar SDT externo automaticamente
5. manter previsibilidade

[SDT-F13]

---

# 5. Tipos de SDT Alvo

| Finalidade | Nome padrão |
|-----------|-------------|
| Create | sdt<NomeBase>_API_CreateRequest |
| Update | sdt<NomeBase>_API_UpdateRequest |
| Saída item | sdt<NomeBase>_API_Response |
| Filtros | sdt<NomeBase>_API_ListFilters |
| Saída lista | sdt<NomeBase>_API_ListResponse |

[NOM-F11][SDT-F13]

---

# 6. Fluxo de Decisão Oficial

Transaction selecionada
→ consultar metadata persistente
→ reencontrar SDTs próprios quando houver metadata compatível
→ criar SDTs próprios quando não houver metadata
→ bloquear colisão com SDT externo de mesmo nome
→ registrar decisão no plano final

[ENG-F10][SDT-F13]

---

# 7. Reencontro de SDTs Próprios

## Prioridade

1. metadata persistente em File
2. mesmo módulo da Transaction
3. nome oficial do contrato

## Regra

Sem metadata compatível, SDT existente é tratado como externo e não pode ser reutilizado automaticamente.

## Exemplos

- sdtCliente_API_CreateRequest
- sdtCliente_API_UpdateRequest
- sdtCliente_API_Response

[SDT-F13]

---

# 8. Compatibilidade Estrutural

Compatibilidade estrutural de SDT externo não autoriza reuso no MVP.

Ela pode ser usada apenas como diagnóstico para explicar por que um objeto existente bloqueou a geração.

[SDT-F13]

---

# 9. Regra de Decisão

| Situação | Decisão Automática MVP |
|------|------------------------|
| SDT próprio por metadata compatível | atualizar conservadoramente |
| SDT externo com mesmo nome | bloquear colisão |
| SDT externo semelhante | ignorar para reuso |

## Evolução futura

Modo assistido de reuso externo fica pós-MVP.

[SDT-F13]

---

# 10. Contratos e Responsabilidades

Cada SDT próprio tem responsabilidade específica e não deve ser reencontrado por similaridade estrutural externa.

| SDT | Responsabilidade |
|------|------------------|
| `sdt<NomeBase>_API_CreateRequest` | entrada selecionada para criação via BC |
| `sdt<NomeBase>_API_UpdateRequest` | entrada selecionada para substituição completa via `PUT` |
| `sdt<NomeBase>_API_Response` | representação pública do registro |
| `sdt<NomeBase>_API_ListFilters` | filtros reconhecidos e aplicados na resposta |
| `sdt<NomeBase>_API_ListResponse` | envelope de lista |

[SDT-F13]

---

# 11. Criação de Novo SDT

Ao criar:

| Tipo | Nome |
|------|------|
| Create | sdtCliente_API_CreateRequest |
| Update | sdtCliente_API_UpdateRequest |
| Saída | sdtCliente_API_Response |
| Lista | sdtCliente_API_ListResponse |

## Estrutura

Derivada da metadata da Transaction.

Os SDTs compartilhados `sdt_API_ErrorResponse` e `sdt_API_Pagination` ficam no `Root Module`, no Folder `GxOpenAPI`, conforme documento 27.

[NOM-F11][MD-F08][SDT-F13]

---

# 12. Regras por Tipo

## CreateRequest

Contém somente atributos selecionados no wizard e atribuíveis ao BC antes de `Save()`.

Padrões:

- partes não autonumeradas da chave, atributos armazenados atribuíveis, chaves estrangeiras armazenadas e campos com regra `Default` vêm marcados
- nullable e opcionais continuam elegíveis e marcados
- chave autonumerada, fórmula, inferido da tabela estendida, redundante automático, subnível, `NoAccept` e não atribuível via BC ficam desabilitados
- tipos `Image`, `Video`, `Audio`, `Blob` e `BlobFile` ficam desabilitados no MVP
- campos sensíveis elegíveis ficam desmarcados com alerta
- auditoria operacional fica desabilitada
- `Obrigatório no payload` é separado da seleção do membro
- partes não autonumeradas da chave devem estar presentes
- campos necessários para criar o registro, sem `Default` e sem preenchimento automático conhecido, devem estar presentes
- campos com `Default`, nullable, opcionais ou preenchidos por regras aplicáveis via BC podem ser omitidos
- campos de origem de migração selecionados são opcionais por padrão, salvo escolha explícita segura
- membro obrigatório ausente retorna `400`
- membro opcional ausente não é atribuído ao BC
- vazio, `false` e `0` presentes são atribuídos como recebidos
- a presença obrigatória não significa valor não vazio
- preserva a ordem da estrutura da Transaction
- preserva nome exato do atributo no SDT, JSON e OpenAPI
- preserva tipo, domínio, tamanho, decimais, nulabilidade e características aplicáveis
- não contém envelope, metadata, subníveis nem campos exclusivos de resposta
- não há campos públicos com sufixo `Specified`

## UpdateRequest

Representa substituição completa via `PUT`.

Regras:

- chave completa fica no `RestPath`, não no corpo
- partes da chave aparecem desabilitadas no wizard
- contém somente atributos selecionados e atribuíveis ao BC carregado
- campos ordinários graváveis vêm selecionados por padrão
- auditoria, fórmulas, inferidos, redundantes, subníveis, `NoAccept` e não atribuíveis ficam desabilitados
- tipos `Image`, `Video`, `Audio`, `Blob` e `BlobFile` ficam desabilitados no MVP
- campos sensíveis elegíveis ficam desmarcados com alerta
- todos os membros selecionados têm `Required = True`
- ausência de qualquer membro selecionado retorna `400` antes de atribuir ao BC
- presença obrigatória não significa valor não vazio
- vazio, `false` e `0` são valores realmente enviados
- preserva ordem, tipos e nomes dos atributos
- fluxo obrigatório: carregar BC pela chave simples ou composta, retornar `404` quando inexistente, preservar chave, autonumeração, auditoria e campos controlados pelo sistema, validar presença integral, atribuir valores recebidos, salvar via BC, recarregar pela chave final e devolver `sdt<NomeBase>_API_Response` completo
- não há `PATCH` no MVP
- não há campos públicos com sufixo `Specified`

## Response

Contém todos os atributos do primeiro nível explicitamente declarados na estrutura da Transaction:

- chave completa
- armazenados
- inferidos ou da tabela estendida declarados
- fórmulas
- calculados
- campos somente de leitura
- campos de auditoria

Não inclui automaticamente atributos alcançáveis pela tabela estendida que não estejam declarados na estrutura. Não inclui subníveis nem campos sintéticos no MVP.

Preserva a ordem da estrutura. Cada membro é baseado no atributo original e preserva domínio, tipo, tamanho, decimais, nulabilidade e características aplicáveis.

Usa exatamente o nome do atributo na KB e no JSON. Dados da Transaction preservam nomes GeneXus; somente o envelope genérico usa lower camel case.

`Get`, `Create`, `Update` e cada item de `List` usam o mesmo `Response`.

## ListFilters

Representa apenas, na resposta, os filtros reconhecidos e aplicados.

Regras:

- não é parâmetro de entrada
- é o tipo de `AppliedFilters`
- contém somente membros correspondentes aos filtros escolhidos no wizard
- filtros de entrada continuam parâmetros planos
- igualdade, `Contém` e `Começa com` usam membro com mesmo nome e tipo público do parâmetro
- períodos usam `NomeDoAtributoFrom` e `NomeDoAtributoTo`
- intervalos numéricos usam `NomeDoAtributoMin` e `NomeDoAtributoMax`
- não contém paginação
- não repete o operador fixado na geração
- o operador deve estar descrito no contrato OpenAPI
- membros permitem `null`
- membros nulos significam filtro não aplicado
- `false`, `0` e string vazia preservam valor informado
- não cria membros auxiliares como `NomeDoAtributoApplied`
- não devolve campos sensíveis, tokens ou credenciais
- um spike deve validar `AllowNull` e serialização JSON no GeneXus 18

## ListResponse

Nome sempre distinto de Response.

Estrutura deve conter exatamente três membros:

- `Items`
- `Pagination`
- `AppliedFilters`

Regras:

- `Items` é coleção de `sdt<NomeBase>_API_Response`
- `Pagination` usa `sdt_API_Pagination`
- `AppliedFilters` usa `sdt<NomeBase>_API_ListFilters`
- os três membros aparecem em toda resposta `200`
- sem registros, `Items` é coleção vazia, `TotalCount = 0` e `TotalPages = 0`
- `Pagination` reflete página e tamanho efetivamente aplicados
- não inclui `Success`, `Message`, `Status`, links nem outro envelope
- dentro da KB usa PascalCase
- externamente usa `items`, `pagination`, `appliedFilters`, `page`, `pageSize`, `totalCount` e `totalPages`
- um spike deve validar a estrutura no YAML gerado pelo GeneXus

[SDT-F13]

---

# 13. Campos Sensíveis

Campos sensíveis entram desmarcados por padrão e com alerta. Campos de auditoria seguem política separada.

## Mesmo em SDT novo.

[SDT-F13]

---

# 14. Política de Update em SDT Existente

## Definição prática de SDT externo

Considerar externo quando:

- fora do módulo alvo
- nome fora padrão oficial
- sem evidência de criação pelo gerador

## MVP conservador

Não alterar automaticamente.

## Se for próprio por metadata

Atualizar conservadoramente.

## Evolução futura

Patch assistido opcional.

[SDT-F13]

---

# 15. Conflito de Nome

Se nome oficial já existir mas incompatível:

| Situação | Ação |
|---------|------|
| modo Safe | bloquear colisão |
| modo Update | atualizar apenas se metadata compatível |
| modo Cancel | abortar |

[ENG-F10][NOM-F11][SDT-F13]

---

# 16. Critérios de Aceite

| Critério | Resultado Esperado |
|------|--------------------|
| SDT próprio por metadata | atualiza conservadoramente |
| ClienteDTO compatibilidade média | ignorado para reuso |
| SDT externo sem metadata | bloqueia se colidir |
| Campo senha | desmarcado com alerta |
| Nome ocupado | bloqueia se metadata incompatível |
| ListResponse | envelope completo |

[SDT-F13]

---

# 17. Uso Correto por Agentes de IA

## Pode assumir

- metadata compatível favorece atualização
- dúvida favorece bloqueio ou criação própria sem colisão
- SDT externo não deve ser alterado automaticamente
- nome oficial deve ser preservado

## Deve tratar com cautela

- critérios podem evoluir
- heurísticas por negócio variam
- modo assistido é futuro

---

# 18. Conclusão Objetiva

O MVP deve reencontrar SDTs apenas quando houver metadata compatível.

Na dúvida, bloquear colisão é mais seguro que degradar ativos existentes.
