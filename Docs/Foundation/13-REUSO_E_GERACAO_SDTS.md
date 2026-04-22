# 13-REUSO_E_GERACAO_SDTS.md

## Regras Oficiais de Reuso, Matching e Criação de SDTs no MVP

**Projeto:** Genexus Open API Builder  
**Versão:** v1.1  
**Base Primária:** 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md v2.2  
**Dependência direta:** 08-MODELO_DADOS_E_METADATA.md v1.4  
**Relacionamento adicional:** 10-ENGINE_GERACAO_OBJETOS.md v1.1 / 11-CONVENCOES_NOMES_E_OUTPUTS.md v1.1 / 12-REGRAS_CRIACAO_API_OBJECTS.md v1.1  
**Objetivo:** definir como o produto decide entre reutilizar SDTs existentes ou criar novos SDTs automaticamente.  
**Idioma:** Português BR  
**Público principal:** Agentes de IA + mantenedores humanos  
**Data:** Abril/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- evitar SDTs duplicados desnecessários
- acelerar geração
- manter consistência estrutural
- permitir reaproveitamento seguro
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

1. reutilizar quando houver alta confiança
2. criar novo quando houver dúvida
3. evitar alterar SDT existente automaticamente
4. manter previsibilidade
5. permitir modo assistido futuro

[SDT-F13]

---

# 5. Tipos de SDT Alvo

| Finalidade | Nome padrão |
|-----------|-------------|
| Entrada | <NomeBase>Request |
| Saída item | <NomeBase>Response |
| Saída lista | <NomeBase>ListResponse |

[NOM-F11][SDT-F13]

---

# 6. Fluxo de Decisão Oficial

Transaction selecionada  
→ procurar SDTs candidatos  
→ classificar compatibilidade  
→ decidir reuso ou novo  
→ registrar decisão no plano final

[ENG-F10][SDT-F13]

---

# 7. Busca de SDTs Candidatos

## Prioridade

1. mesmo módulo da Transaction  
2. módulo escolhido no wizard  
3. demais módulos da KB

## Filtro inicial

- nome relacionado ao NomeBase
- estrutura semelhante
- tipo SDT válido

## Exemplos

- ClienteRequest
- ClienteResponse
- ClienteDTO

[SDT-F13]

---

# 8. Faixas Oficiais de Compatibilidade

## Alta Compatibilidade

Todos os itens abaixo:

- nome compatível ou relacionado
- PK presente
- >= 80% campos esperados presentes
- campos obrigatórios presentes
- tipos compatíveis

## Média Compatibilidade

Atende parcialmente os critérios.

## Baixa Compatibilidade

Falta PK, baixa cobertura ou estrutura distante.

## Regra

No MVP, preferir simplicidade sobre matemática artificial.

[SDT-F13]

---

# 9. Regra de Decisão

| Faixa | Decisão Automática MVP |
|------|------------------------|
| Alta | reutilizar |
| Média | criar novo |
| Baixa | criar novo |

## Evolução futura

Modo assistido pode sugerir reuso em faixa média.

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

Se não reutilizar:

| Tipo | Nome |
|------|------|
| Entrada | ClienteRequest |
| Saída | ClienteResponse |
| Lista | ClienteListResponse |

## Estrutura

Derivada da metadata da Transaction.

[NOM-F11][MD-F08][SDT-F13]

---

# 12. Regras por Tipo

## Request

Preferir campos editáveis.

## Response

Campos públicos úteis.

## ListResponse

Nome sempre distinto de Response.

Estrutura pode inicialmente ser igual à Response no MVP.

[SDT-F13]

---

# 13. Campos Sensíveis

Nunca incluir automaticamente:

- senha
- password
- hash
- token
- secret
- auditoria interna

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

## Se reutilizado

Apenas referenciar.

## Evolução futura

Patch assistido opcional.

[SDT-F13]

---

# 15. Conflito de Nome

Se nome oficial já existir mas incompatível:

| Situação | Ação |
|---------|------|
| modo Safe | ClienteRequest_v2 |
| modo Update | criar novo compatível ou perguntar |
| modo Cancel | abortar |

[ENG-F10][NOM-F11][SDT-F13]

---

# 16. Critérios de Aceite

| Critério | Resultado Esperado |
|------|--------------------|
| ClienteRequest alta compatibilidade | reutiliza |
| ClienteDTO compatibilidade média | cria novo |
| SDT sem PK | cria novo |
| Campo senha | omitido |
| Nome ocupado | versiona ou aborta |
| ListResponse | nome distinto |

[SDT-F13]

---

# 17. Uso Correto por Agentes de IA

## Pode assumir

- alta compatibilidade favorece reuso
- dúvida favorece novo SDT
- SDT externo não deve ser alterado automaticamente
- nome oficial deve ser preservado

## Deve tratar com cautela

- critérios podem evoluir
- heurísticas por negócio variam
- modo assistido é futuro

---

# 18. Próxima Etapa Recomendada

Criar:

14-CONFLITOS_REEXECUCAO_E_VERSIONAMENTO.md

Para consolidar colisões, reruns e updates.

---

# 19. Conclusão Objetiva

O MVP deve reutilizar SDTs apenas quando houver alta confiança estrutural.

Na dúvida, criar novo é mais seguro que degradar ativos existentes.