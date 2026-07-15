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

As sprints que implementam `List`, contratos HTTP/erros e ciclo de vida devem seguir, respectivamente, `26-CONTRATO_FILTROS_PAGINACAO_ORDENACAO.md`, `27-CONTRATO_HTTP_ERROS_E_SDTS_COMPARTILHADOS.md` e `28-METADATA_REGENERACAO_SINCRONIZACAO_E_REMOCAO.md`.

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

Executar a Fase 0 do backlog (`B010`–`B012`) e deixar o terreno técnico reproduzível.

## Entregas

- `B010`: versão e origem do SDK registradas
- `B010`: dependências localizáveis sem caminho absoluto específico da máquina
- `B010`: solution e projeto em caminho relativo dentro de `Src`
- `B010`: comando e evidência de build mínimo registrados
- `B011`: pastas internas estruturadas
- `B012`: convenções de nomes congeladas confirmadas e aplicadas

## Saída esperada

Solution mínima reproduzível e pronta para o spike. O carregamento na IDE pertence ao `B000`, não ao `B010`.

[SPR-F24]

---

# 7. Sprint 1 — Spike SDK Real

## Objetivo

Executar o pacote inicial de viabilidade da Fase -1 (`B000`–`B006`).

## Entregas

- `B000`: extensão mínima carrega na IDE
- `B001`: KB ativa é detectada
- `B002`: Transactions reais são listadas por API oficial disponível
- `B003`: objeto simples suportado pelo SDK é criado
- `B004`: ciclo de vida de API Object oficial é validado
- `B005`: ciclo de vida de Procedure, SDT, Folder e File é validado
- `B006`: metadata em File persiste após fechar e reabrir a KB

## Gate

Se `B004` falhar sem alternativa oficial viável, revisar ou encerrar a tese atual do produto.

[F09][SPR-F24]

---

## Gates técnicos transversais do MVP

Os gates abaixo são comprovados progressivamente nas Sprints 1–7. A Sprint 1 inicia essa comprovação com `B000`–`B006`; ela não precisa concluir antecipadamente capacidades que dependem do engine e dos contratos posteriores:

1. extensão carrega no GeneXus 18 Upgrade 15
2. SDK cria, salva, reabre, altera e exclui objetos nativos `API`, `Procedure`, `SDT`, `Folder` e `File`
3. objeto `API` delega às Procedures e persiste `RestMethod`, `RestPath`, `Description` e `SecurityLevel`
4. YAML gerado pelo GeneXus reflete rotas, métodos, parâmetros, SDTs e nomes `_API_`
5. `Create` e `Update` via BC funcionam com chave simples e composta, preservando regras e mensagens
6. ausência JSON é distinguida de vazio, `false` e zero sem membros públicos `Specified`
7. implementação controla códigos HTTP, corpo e `Location`, respeitando seu caráter opcional
8. `List` funciona com filtros opcionais, períodos, paginação, totalização e ordenação determinística
9. metadata em `File` sobrevive a fechar/reabrir a KB e reconhece objetos próprios
10. colisão, regeneração e remoção não sobrescrevem nem apagam objetos alheios

Se qualquer gate falhar sem alternativa nativa segura, revisar o desenho antes de declarar concluído o wizard funcional do MVP.

Não bloqueiam o MVP: associação visual sob a Transaction, objeto `Documentation` como fonte de metadata, uniformidade de erros interceptados antes da Procedure, migração assistida após renomear/mover Transaction, GeneXus Next, base `api/v1` e otimizações de build.

---

# 8. Sprint 2 — Protótipo Navegável do Wizard

## Objetivo

Validar navegação, captura de decisões e cancelamento seguro sem persistir nem gerar objetos.

## Entregas

- selecionar uma Transaction a partir de estado somente leitura
- navegar pelos passos previstos para nomes, serviços, campos, filtros, paginação, ordenação e segurança
- manter as escolhas apenas em memória
- avançar, voltar e cancelar sem alterar a KB
- exibir resumo não persistente das escolhas
- não criar `ApiPlan` definitivo
- não chamar engine nem gerar objetos reais

## Gate

Fluxo navegável completo, com estado em memória e cancelamento sem efeitos colaterais.

[F07][SPR-F24]

---

# 9. Sprint 3 — Metadata + ApiPlan

## Objetivo

Transformar Transaction em plano interno.

## Entregas

- ler atributos
- identificar PK
- marcar sensíveis
- reconhecer auditoria operacional por nomes/sufixos específicos
- módulo alvo
- montar decisões de filtros, payload, paginação, ordenação e segurança
- montar ApiPlan

## Gate

Plano consistente gerado.

[F08][SPR-F24]

---

# 10. Sprint 4 — Engine Base

## Objetivo

Realizar a primeira integração efetiva wizard → `ApiPlan` → engine e gerar os primeiros objetos reais.

## Entregas

- receber o `ApiPlan` produzido a partir das decisões do wizard e entregá-lo ao engine
- apiCliente
- procCliente_API_List/Get/Create/Update
- sdtCliente_API_CreateRequest
- sdtCliente_API_UpdateRequest
- sdtCliente_API_Response
- sdtCliente_API_ListFilters
- sdtCliente_API_ListResponse
- sdt_API_ErrorResponse
- sdt_API_Pagination
- File JSON de metadata
- `[Description]` nos serviços selecionados
- operationIds no padrão `apiNome.Serviço`
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
- Create retornando 201
- Update usando PUT e retornando Response completo
- ListResponse com `items`, `pagination` e `appliedFilters`
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
- validar SDTs compartilhados no Folder `GxOpenAPI`
- validar ausência de campos públicos `Specified`

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
- sincronização com comparação explícita antes de alterar
- remoção por comando explícito preservando Folder reutilizado e `GxOpenAPI`

## Gate

Sem overwrite indevido e com os dez gates técnicos transversais comprovados.

Ao concluir esta sprint, o projeto atinge o marco **wizard funcional do MVP concluído**. Esse marco é pré-condição para iniciar a Alpha da Sprint 8.

[F14][SPR-F24]

---

# 13.1 KBs de Teste

A validação prática deve começar por uma KB menor, fora de produção, com backup disponível.

Depois, deve avançar para uma cópia de teste atualizada da KB principal.

Não executar validação diretamente na KB principal de produção.

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
