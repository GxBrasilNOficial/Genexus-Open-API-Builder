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
| -1 | F05 | Spike técnico crítico |
| 0 | Setup | Estrutura mínima |
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

## FASE -1 — Spike Técnico Crítico

| ID | Item | Prioridade |
|---|---|---|
| B000 | Confirmar extensão carrega na IDE | Alta |
| B001 | Detectar KB ativa | Alta |
| B002 | Listar Transactions reais via API oficial disponível | Alta |
| B003 | Criar objeto simples de teste suportado pelo SDK | Alta |
| B004 | Validar criação/manipulação de API Object oficial | Altíssima |
| B005 | Validar criação/manipulação de Procedure, SDT, Folder e File | Altíssima |
| B006 | Validar persistência e releitura de metadata em File após reabrir KB | Altíssima |

### Gate

Se B004 falhar sem alternativa oficial viável:

> revisar ou encerrar a tese atual do produto.

---

## FASE 0 — Setup

| ID | Item | Prioridade |
|---|---|---|
| B010 | Criar solução extensibility | Alta |
| B011 | Estruturar pastas internas | Alta |
| B012 | Definir convenções provisórias de nomes | Alta |

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
| B032 | Passo 3 revisar segurança, paginação, ordenação e RestPath | Alta |
| B033 | Validar campos obrigatórios | Alta |
| B034 | Cancelamento seguro | Média |
| B035 | Bloquear geração sem BC ou oferecer habilitação explícita | Alta |

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

---

## FASE 5 — Organização

| ID | Item | Prioridade |
|---|---|---|
| B060 | Gravar metadata persistente em File | Alta |
| B061 | Aplicar mesmo módulo da Transaction | Alta |
| B062 | Aplicar nomenclatura padrão | Alta |
| B063 | Detectar colisões por metadata e por nome | Alta |
| B064 | Bloquear colisões incompatíveis sem criar `_v2` | Alta |

---

## FASE 6 — Serviços REST

| ID | Item | Prioridade |
|---|---|---|
| B070 | Gerar `List` com filtros, paginação e ordenação determinística | Alta |
| B071 | Gerar `Get` por chave simples ou composta | Alta |
| B072 | Gerar `Create` | Alta |
| B073 | Gerar `Update` com resposta 200 completa | Alta |
| B074 | Gerar paths e operationIds conforme convenção | Alta |
| B075 | Validar ausência de endpoint `Delete` no MVP | Alta |

### Nota operacional

`Delete` é pós-MVP como endpoint REST. A remoção de uma API gerada pertence ao ciclo de vida da ferramenta e depende da metadata persistente.

---

## FASE 7 — Operação IDE

| ID | Item | Prioridade |
|---|---|---|
| B080 | Integrar menu/contexto IDE | Alta |
| B081 | Exibir relatório final interno | Alta |
| B082 | Mostrar tempo execução | Média |
| B083 | Detectar conflito antes salvar | Alta |
| B084 | Bloquear overwrite silencioso | Alta |
| B085 | Sincronizar com a Transaction usando metadata | Alta |
| B086 | Remover API gerada por metadata, sem reverter BC | Média |

---

## FASE 8 — Segurança

| ID | Item | Prioridade |
|---|---|---|
| B090 | Classificar sensíveis por configuração explícita | Alta |
| B091 | Classificar auditoria separadamente | Alta |
| B092 | Configurar Security Level e GAM/None quando aplicável | Alta |
| B093 | Permitir revisão manual de campos no wizard | Média |

---

# 7. Critérios de Aceite por Itens-Chave

| ID | Aceite |
|---|---|
| B004 | Existe evidência prática de criação/manipulação oficial de API Object |
| B005 | Existe evidência prática de criação/manipulação de Procedure, SDT, Folder e File |
| B006 | Metadata em File sobrevive ao fechamento e reabertura da KB |
| B060 | Cliente grava metadata de geração persistente |
| B040 | Cliente gera `sdtCliente_API_CreateRequest` |
| B041 | Cliente gera `sdtCliente_API_UpdateRequest` |
| B042 | Cliente gera `sdtCliente_API_Response` |
| B070 | Existe `List` funcional |
| B071 | Existe `Get` funcional para chave simples e composta |
| B072 | Existe `Create` funcional |
| B073 | Existe `Update` funcional com HTTP 200 e Response completo |
| B075 | Não existe endpoint `Delete` no MVP |
| B080 | Menu/contexto acessível dentro IDE |
| B081 | Relatório lista criados/atualizados |

[BG-F06]

---

# 8. MVP Real (linha de corte)

Obrigatórios:

- B000
- B001
- B002
- B003
- B004
- B020
- B021
- B030
- B031
- B032
- B040
- B041
- B060
- B070
- B071
- B072
- B073
- B075
- B080
- B081
- B083
- B084
- B090

[BG-F06]

---

# 9. Ordem Recomendada de Execução

1. Fase -1 completa
2. Fase 0 completa
3. Fase 1 completa
4. Fase 2 mínima
5. Fase 3 mínima
6. Fase 4 mínima
7. Fase 5 mínima
8. Fase 6 List/Get inicial
9. Completar Create/Update
10. Fase 7 mínima
11. Fase 8 mínima

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
| Todas as fases | Spike técnico aprovado |
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
- gate técnico vem antes de tudo
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
