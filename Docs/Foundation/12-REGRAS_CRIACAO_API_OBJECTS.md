# 12-REGRAS_CRIACAO_API_OBJECTS.md

## Regras Oficiais de Criação dos Objetos REST do MVP

**Projeto:** Genexus Open API Builder  
**Versão:** v1.1  
**Base Primária:** 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md v2.2  
**Dependência direta:** 10-ENGINE_GERACAO_OBJETOS.md v1.1  
**Relacionamento adicional:** 09-INTEGRACAO_GeneXus_Extensibility_SDK.md v3.1 / 11-CONVENCOES_NOMES_E_OUTPUTS.md v1.1  
**Objetivo:** definir o conteúdo mínimo e comportamento esperado dos objetos REST gerados automaticamente no MVP.  
**Idioma:** Português BR  
**Público principal:** Agentes de IA + mantenedores humanos  
**Data:** Abril/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- definir estrutura mínima dos objetos REST
- padronizar CRUD inicial
- garantir consistência entre gerações
- orientar engine de criação
- reduzir decisões implícitas

Este documento **não define UX**, **não redefine naming**, **não impõe tecnologia REST única**.

---

# 2. Taxonomia

| Código | Tipo | Significado |
|------|------|-------------|
| DP-F04 | Decisão oficial | Requisito aprovado |
| ENG-F10 | Engine geração | Processo técnico |
| NOM-F11 | Naming oficial | Convenções de nomes |
| API-F12 | Regras REST | Definição deste documento |
| HP-F12 | Hipótese | Dependente do artefato final |

---

# 3. Fontes e Rastreabilidade

| Código | Fonte |
|------|-------|
| F04 | REQUISITOS_MVP |
| F09 | INTEGRACAO_GeneXus_Extensibility_SDK |
| F10 | ENGINE_GERACAO_OBJETOS |
| F11 | CONVENCOES_NOMES_E_OUTPUTS |

---

# 4. Estratégia Oficial

No MVP, todo objeto REST gerado deve:

1. gerar estrutura utilizável
2. expor CRUD básico quando suportado
3. usar SDTs oficiais
4. seguir naming previsível
5. permitir ajuste manual simples

## Meta qualitativa

Quando possível, compilar imediatamente.

[API-F12]

---

# 5. Artefato REST Alvo

## Ordem preferencial

| Ordem | Tipo |
|------:|------|
| 1 | API Object oficial |
| 2 | Objeto REST equivalente suportado |
| 3 | Procedure REST |

## Regra

A implementação concreta depende do que a SDK suportar.

[DP-F04][API-F12]

---

# 6. Nome do Objeto Principal

Seguir documento 11:

<NomeBase>Api

## Exemplos

- ClienteApi
- ProdutoApi

[NOM-F11][API-F12]

---

# 7. Endpoints Obrigatórios do MVP

## Para PK simples

| Método | Path | Finalidade |
|------|------|------------|
| GET | /api/clientes | listar |
| GET | /api/clientes/{id} | buscar por id |
| POST | /api/clientes | criar |
| PUT | /api/clientes/{id} | atualizar |
| DELETE | /api/clientes/{id} | excluir |

## Regra

Estrutura gerada deve ficar pronta para teste inicial simples.

[DP-F04][API-F12]

---

# 8. Regras para Chave Composta

## MVP inicial

Não gerar CRUD automático completo.

## Permitido gerar

| Recurso | Status |
|--------|--------|
| GET lista | Sim |
| Estrutura base do objeto | Sim |
| GET por chave composta | Não automático |
| PUT por chave composta | Não automático |
| DELETE por chave composta | Não automático |

## Regra

Informar limitação ao usuário.

[API-F12]

---

# 9. Contratos de Entrada e Saída

| Operação | Contrato |
|--------|----------|
| POST | <NomeBase>Request |
| PUT | <NomeBase>Request |
| GET item | <NomeBase>Response |
| GET lista | <NomeBase>ListResponse |

[NOM-F11][API-F12]

---

# 10. Regras Objetivas de Implementação

| Endpoint | Estrutura mínima |
|---------|------------------|
| GET lista | rota criada + retorno ListResponse |
| GET item | rota criada + parâmetro PK |
| POST | rota criada + Request |
| PUT | rota criada + PK + Request |
| DELETE | rota criada + PK |

## Regra

Lógica interna pode variar conforme artefato final.

[API-F12]

---

# 11. Estratégia Delete

## Default MVP

Delete físico simples, quando nenhuma convenção especial for detectada.

## Evolução futura

- soft delete
- flags de inativação
- auditoria avançada

[API-F12]

---

# 12. Códigos HTTP de Referência

| Operação | Resposta comum |
|--------|----------------|
| GET lista | 200 |
| GET item | 200 / 404 |
| POST | 201 / 400 |
| PUT | 200 / 204 / 404 |
| DELETE | 200 / 204 / 404 |

## Regra

Valores exatos dependem do artefato REST final.

[HP-F12]

---

# 13. Campos Sensíveis

## Não expor automaticamente em Response

- senha
- password
- hash
- token
- secret

## Não aceitar automaticamente em Request quando fizer sentido

- hash interno
- token interno
- campos de auditoria técnica

## Exemplos

| Campo | Resultado |
|------|-----------|
| ClienteSenha | omitido |
| UserToken | omitido |
| Nome | mantido |

[API-F12]

---

# 14. Tratamento de Erros

## Estrutura lógica mínima

| Campo | Obrigatório |
|------|-------------|
| message | Sim |
| code | Opcional |
| details | Opcional |

## Regras

- sem stack trace público
- linguagem simples
- log técnico interno separado

[API-F12]

---

# 15. Estrutura de Código Gerado

## Meta

Código simples e editável.

## Preferir

- separação clara por endpoint
- nomes legíveis
- baixa duplicação
- fácil manutenção manual

## Evitar

- blocos confusos
- nomes obscuros
- dependências ocultas

[API-F12]

---

# 16. Reexecução / Update

Se objeto já existir:

| Modo | Ação |
|------|------|
| Safe | novo nome versionado |
| Update | atualizar estrutura compatível |
| Cancel | abortar |

[ENG-F10][API-F12]

---

# 17. Critérios de Aceite

| Critério | Resultado Esperado |
|------|--------------------|
| Cliente gera objeto | ClienteApi |
| Cliente gera 5 rotas CRUD | Sim |
| POST usa Request | Sim |
| GET item usa Response | Sim |
| GET lista usa ListResponse | Sim |
| Campo senha | omitido |
| PK composta | geração parcial + aviso |

[API-F12]

---

# 18. Uso Correto por Agentes de IA

## Pode assumir

- CRUD simples é foco do MVP
- PK simples é cenário principal
- SDTs oficiais devem ser usados
- estrutura gerada deve ser editável

## Deve tratar com cautela

- detalhes variam por artefato REST final
- status HTTP variam por tecnologia
- soft delete fica fora do MVP

---

# 19. Próxima Etapa Recomendada

Criar:

13-REUSO_E_GERACAO_SDTS.md

Para detalhar matching, criação e reaproveitamento dos SDTs.

---

# 20. Conclusão Objetiva

O objeto REST gerado no MVP deve resolver o básico muito bem:

listar, buscar, criar, atualizar e excluir com previsibilidade.