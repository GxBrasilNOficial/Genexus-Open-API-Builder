# 24-PLANO_IMPLEMENTACAO_REAL_POR_SPRINTS.md

## Plano Oficial de Execução Prática do Projeto em Sprints Reais

**Projeto:** Genexus Open API Builder
**Versão:** v1.0
**Base Primária:** 23-RISCOS_LIMITACOES_E_NAO_OBJETIVOS.md v1
**Dependência direta:** 10-ENGINE_GERACAO_OBJETOS.md v1.0
**Relacionamento adicional:** 01 a 23 aprovados
**Objetivo:** converter toda a documentação consolidada em um plano realista de implementação incremental, validável e executável.
**Idioma:** Português BR
**Público principal:** maintainer principal + contribuidores técnicos + agentes de IA
**Data:** Abril/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- transformar teoria em execução
- reduzir paralisia por excesso de planejamento
- organizar prioridades reais
- criar entregas incrementais
- acelerar primeiro release utilizável

Este documento **não exige metodologia rígida**, **não congela datas**, **não impede adaptação prática**.

---

# 2. Taxonomia

| Código | Tipo | Significado |
|------|------|-------------|
| MVP-F04 | Escopo base | Produto inicial |
| ENG-F10 | Engine | Núcleo técnico |
| OPS-F24 | Operação prática | Definição deste documento |
| SPR-F24 | Sprint | Ciclo curto |
| HP-F24 | Hipótese | Ajustável durante execução |

---

# 3. Fontes e Rastreabilidade

| Código | Fonte |
|------|-------|
| F04 | REQUISITOS_MVP |
| F07 | UX_WIZARD |
| F09 | INTEGRACAO_SDK |
| F10 | ENGINE_GERACAO |
| F15 | TESTES_QUALIDADE |
| F23 | RISCOS_LIMITACOES |

---

# 4. Estratégia Oficial

Executar em ciclos curtos:

1. construir base mínima
2. validar rápido
3. corrigir cedo
4. expandir com controle
5. publicar incrementalmente

[OPS-F24]

---

# 5. Regra Principal

Versão simples funcionando vale mais que arquitetura perfeita parada.

[OPS-F24]

---

# 6. Sprint 0 — Preparação

## Objetivo

Deixar terreno pronto.

## Entregas

- repositório organizado
- estrutura solução/projeto
- padrões de branch
- backlog inicial
- docs mínimas
- ambiente buildando

## Saída esperada

Projeto pronto para codar.

[SPR-F24]

---

# 7. Sprint 1 — Spike SDK Real

## Objetivo

Validar integração GeneXus real.

## Entregas

- extensão carrega
- comando/menu acessível
- detectar objeto selecionado
- abrir UI simples
- log básico

## Gate

Se falhar, ajustar estratégia antes de crescer.

[F09][SPR-F24]

---

# 8. Sprint 2 — Wizard Inicial

## Objetivo

Fluxo mínimo utilizável.

## Entregas

- selecionar Transaction
- confirmar opções básicas
- executar geração
- tela resultado simples

## Gate

Usuário consegue operar sozinho.

[F07][SPR-F24]

---

# 9. Sprint 3 — Metadata + ApiPlan

## Objetivo

Transformar Transaction em plano interno.

## Entregas

- ler atributos
- identificar PK
- marcar sensíveis
- módulo alvo
- montar ApiPlan

## Gate

Plano consistente gerado.

[F08][SPR-F24]

---

# 10. Sprint 4 — Engine Base

## Objetivo

Gerar primeiros objetos reais.

## Entregas

- apiCliente
- procCliente_API_List/Get/Create/Update
- sdtCliente_API_CreateRequest
- sdtCliente_API_UpdateRequest
- sdtCliente_API_Response
- sdtCliente_API_ListResponse
- File JSON de metadata
- logs execução

## Gate

Objetos criados corretamente.

[F10][SPR-F24]

---

# 11. Sprint 5 — Serviços REST Iniciais

## Objetivo

Gerar serviços base.

## Entregas

- List
- Get
- Create
- Update
- validação de ausência de endpoint Delete no MVP

## Gate

Estrutura funcional pronta.

[F12][SPR-F24]

---

# 12. Sprint 6 — SDTs Próprios e Metadata

## Objetivo

Garantir contratos próprios e reencontro seguro.

## Entregas

- criar SDTs próprios
- reencontrar próprios por metadata
- bloquear SDT externo em colisão

## Gate

Reencontro previsível por metadata.

[F13][SPR-F24]

---

# 13. Sprint 7 — Conflitos e Reexecução

## Objetivo

Segurança operacional.

## Entregas

- Safe mode
- bloqueio de colisão sem `_v2`
- Update controlado
- Cancel seguro
- rerun consistente

## Gate

Sem overwrite indevido.

[F14][SPR-F24]

---

# 14. Sprint 8 — Release Alpha Público

## Objetivo

Primeira versão aberta utilizável.

## Entregas

- README forte
- install guide
- changelog
- release tag
- demo curta

## Gate

Usuário externo testa.

[F18][SPR-F24]

---

# 15. Sprint 9 — Correções Reais

## Objetivo

Aprender com uso externo.

## Entregas

- bugs prioritários corrigidos
- docs melhores
- onboarding melhorado
- UX refinada

## Gate

Adoção melhora.

[SPR-F24]

---

# 16. Sprint 10 — Beta Estável

## Objetivo

Produto confiável inicial.

## Entregas

- regressões reduzidas
- fluxo principal sólido
- comunidade ativa mínima
- releases previsíveis

## Gate

Caminho para v1.

[SPR-F24]

---

# 17. Ritmo Recomendado

| Tipo de Sprint | Duração |
|------|---------|
| pessoal intenso | 1 semana |
| realista paralelo | 2 semanas |
| voluntário comunitário | 3 semanas |

[HP-F24]

---

# 18. O Que Não Fazer Durante Execução

Evitar:

- refatorar cedo demais
- feature creep
- sprint gigante
- reescrever sem motivo
- ignorar feedback real

[OPS-F24]

---

# 19. Uso Correto por Agentes de IA

## Pode assumir

- entrega incremental vence perfeccionismo
- gates evitam desperdício
- feedback externo acelera maturidade

## Deve tratar com cautela

- datas rígidas
- excesso de escopo
- dependências não validadas

---

# 20. Conclusão Objetiva

Projeto cresce quando planejamento vira sprint.

E sprint vira software funcionando.
