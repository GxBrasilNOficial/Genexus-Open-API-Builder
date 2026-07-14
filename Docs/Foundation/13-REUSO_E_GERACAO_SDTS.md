# 13-REUSO_E_GERACAO_SDTS.md

## Regras Oficiais de Criação e Reencontro de SDTs no MVP

**Projeto:** Genexus Open API Builder  
**Versão:** v1.0  
**Base Primária:** 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md v1.0  
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

# 10. Campos Obrigatórios Mínimos

Para considerar compatível:

- PK principal
- campos IsRequired = true
- campos essenciais de negócio
- tipos compatíveis

## Exemplo Cliente

- ClienteId
- ClienteNome

## Campos essenciais de negócio

Preferencialmente:

- nome/descrição principal
- identificador público
- atributo central do cadastro

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

[NOM-F11][MD-F08][SDT-F13]

---

# 12. Regras por Tipo

## CreateRequest

Preferir campos editáveis.

## UpdateRequest

Preferir campos editáveis, sem PK no corpo quando a chave vier pela rota.

## Response

Campos públicos úteis.

## ListResponse

Nome sempre distinto de Response.

Estrutura deve conter envelope com `items`, `pagination` e `appliedFilters`.

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
