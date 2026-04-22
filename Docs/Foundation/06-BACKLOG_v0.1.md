# 06-BACKLOG_v0.1.md

## Backlog Inicial Priorizado do MVP

**Projeto:** Genexus Open API Builder  
**Versão:** v2  
**Base Primária:** 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md v2.2  
**Dependência direta:** 05-ARQUITETURA_FUNCIONAL_MVP.md v3.1  
**Objetivo:** converter requisitos e arquitetura em entregas incrementais rastreáveis.  
**Idioma:** Português BR  
**Público principal:** Agentes de IA + mantenedores humanos  
**Data:** Abril/2026

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

1. gerar primeira API funcional
2. operar dentro da IDE
3. repetir sem erro
4. evitar exposição indevida
5. preparar evolução futura

[BG-F06]

---

# 5. Fases Oficiais (alinhadas ao F05)

| Fase | Base | Meta |
|---|---|---|
| 0 | Setup | Estrutura mínima |
| 1 | F04 8.1 | Seleção Transaction |
| 2 | F04 8.6 | Wizard 3 passos |
| 3 | F04 8.4 | Reuso SDT |
| 4 | F04 8.5 | Criar SDTs |
| 5 | F04 8.3 | Organização |
| 6 | F04 8.2 | CRUD REST |
| 7 | F04 8.7 | Operação IDE |
| 8 | Segurança | Campos sensíveis |

[BG-F06]

---

# 6. Backlog Priorizado

## FASE 0 — Setup

| ID | Item | Prioridade |
|---|---|---|
| B001 | Criar solução extensibility | Alta |
| B002 | Estruturar pastas internas | Alta |
| B003 | Definir convenções de nomes | Alta |

---

## FASE 1 — Seleção Transaction

| ID | Item | Prioridade |
|---|---|---|
| B010 | Detectar KB ativa | Alta |
| B011 | Listar Transactions elegíveis | Alta |
| B012 | Ler módulo da Transaction | Alta |
| B013 | Detectar objetos existentes | Média |

---

## FASE 2 — Wizard

| ID | Item | Prioridade |
|---|---|---|
| B020 | Passo 1 selecionar Transaction | Alta |
| B021 | Passo 2 configurações | Alta |
| B022 | Passo 3 confirmar geração | Alta |
| B023 | Validar campos obrigatórios | Alta |
| B024 | Cancelamento seguro | Média |

---

## FASE 3 — Reuso SDT

| ID | Item | Prioridade |
|---|---|---|
| B030 | Detectar SDTs existentes | Alta |
| B031 | Avaliar compatibilidade SDT | Alta |
| B032 | Reutilizar SDT aprovado | Alta |

---

## FASE 4 — Criar SDTs

| ID | Item | Prioridade |
|---|---|---|
| B040 | Gerar Request SDT | Alta |
| B041 | Gerar Response SDT | Alta |
| B042 | Gerar ListResponse SDT | Média |

---

## FASE 5 — Organização

| ID | Item | Prioridade |
|---|---|---|
| B050 | Gerar `<Transaction>Api` | Alta |
| B051 | Aplicar módulo destino | Alta |
| B052 | Aplicar nomenclatura padrão | Alta |

---

## FASE 6 — CRUD REST

| ID | Item | Prioridade |
|---|---|---|
| B060 | Gerar GET lista | Alta |
| B061 | Gerar GET por id | Alta |
| B062 | Gerar POST | Alta |
| B063 | Gerar PUT | Alta |
| B064 | Gerar DELETE | Alta |
| B065 | Gerar rotas padrão | Alta |

---

## FASE 7 — Operação IDE

| ID | Item | Prioridade |
|---|---|---|
| B070 | Integrar menu/contexto IDE | Alta |
| B071 | Exibir relatório final interno | Alta |
| B072 | Mostrar tempo execução | Média |
| B073 | Detectar conflito antes salvar | Alta |
| B074 | Bloquear overwrite silencioso | Alta |

---

## FASE 8 — Segurança

| ID | Item | Prioridade |
|---|---|---|
| B080 | Excluir senha automaticamente | Alta |
| B081 | Excluir hash automaticamente | Alta |
| B082 | Excluir auditoria interna | Alta |
| B083 | Permitir revisão manual campos | Média |

---

# 7. Critérios de Aceite por Itens-Chave

| ID | Aceite |
|---|---|
| B050 | Cliente gera ClienteApi no módulo correto |
| B040 | Cliente gera ClienteRequest |
| B041 | Cliente gera ClienteResponse |
| B060 | Existe GET /api/clientes |
| B061 | Existe GET /api/clientes/{id} |
| B062 | Existe POST /api/clientes |
| B063 | Existe PUT /api/clientes/{id} |
| B064 | Existe DELETE /api/clientes/{id} |
| B070 | Menu/contexto acessível dentro IDE |
| B071 | Relatório lista criados/atualizados |

[BG-F06]

---

# 8. MVP Real (linha de corte)

Obrigatórios:

- B001
- B010
- B011
- B020
- B021
- B022
- B030
- B031
- B040
- B041
- B050
- B060
- B061
- B062
- B063
- B064
- B070
- B071
- B073
- B074
- B080

[BG-F06]

---

# 9. Ordem Recomendada de Execução

1. Fase 0 completa
2. Fase 1 completa
3. Fase 2 completa
4. Fase 3 mínima
5. Fase 4 mínima
6. Fase 5 completa
7. Fase 6 completa
8. Fase 7 mínima
9. Fase 8 mínima

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

[DP-F04]

---

# 11. Dependências Técnicas

| Item | Depende de |
|---|---|
| Wizard | Seleção Transaction |
| Reuso SDT | Wizard |
| Criar SDT | Reuso SDT |
| Organização | Criar SDT |
| CRUD | Organização |
| Operação IDE | CRUD |
| Segurança | CRUD |

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

# 13. Riscos Iniciais

| Risco | Mitigação |
|---|---|
| SDK limitado | spikes rápidos |
| geração quebrar KB | ambiente teste |
| escopo inflar | seguir linha MVP |
| UX ruim | testar cedo |
| naming ruim | congelar cedo |

[HP-F06]

---

# 14. Uso Correto por Agentes de IA

## Pode assumir

- backlog segue ordem do F05
- itens Alta entram primeiro
- segurança mínima já está no MVP

## Deve tratar com cautela

- backlog muda após descoberta real da SDK
- itens podem virar subtarefas
- ordem pode ajustar por bloqueio técnico

---

# 15. Grau de Confiança

| Área | Grau | Evidência |
|---|---|---|
| Ordem geral execução | Alto | [F04][F05] |
| MVP definido corretamente | Alto | [F04] |
| Dependências técnicas | Alto | [AF-F05] |
| Estimativa futura esforço | Baixo | [HP-F06] |

---

# 16. Conclusão Objetiva

O backlog v2 prioriza:

Selecionar Transaction → Wizard → SDTs → API → CRUD → IDE → Segurança.

Tudo além disso fica para versões futuras.