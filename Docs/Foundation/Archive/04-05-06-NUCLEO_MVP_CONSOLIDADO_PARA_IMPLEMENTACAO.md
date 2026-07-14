# 04-05-06-NUCLEO_MVP_CONSOLIDADO_PARA_IMPLEMENTACAO

> Documento arquivado em 2026-07-14. Não é fonte normativa vigente do Foundation; foi preservado apenas para histórico porque é um artefato derivado local antigo, não versionado, e contradiz decisões posteriores do checkpoint da entrevista funcional do MVP.

## Documento Operacional Consolidado para Execução do MVP por IA

**Projeto:** Genexus Open API Builder
**Versão:** v1.2
**Tipo:** Artefato derivado operacional
**Origem oficial:**
- 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md v1.1
- 05-ARQUITETURA_FUNCIONAL_MVP.md v1.1
- 06-BACKLOG_v0.1.md v1.1

**Objetivo:** consolidar os documentos-base em um pacote único, claro e implementável para agentes técnicos (ex.: Codex), reduzindo ambiguidade e acelerando o início do MVP.

---

# Governança deste Documento

Este arquivo **não substitui** os documentos 04, 05 e 06.

Ele existe como **documento auxiliar derivado para implementação**.

## Regra de precedência

Em caso de conflito:

1. prevalecem os documentos-base 04, 05 e 06
2. salvo decisão posterior formalmente registrada

---

# 1. Missão do MVP

Provar que é possível gerar valor **dentro da IDE GeneXus**, via extensão oficial, transformando uma Transaction em um API Object inicial utilizável.

---

# 2. Decisão Fundacional (Gate Absoluto)

Se **não for tecnicamente viável criar ou manipular API Objects oficiais** por caminho suportado dentro da IDE GeneXus:

> a tese atual do produto deve ser revisada ou encerrada.

## Não aceitável como substituto

- Procedure REST
- app externo
- CLI paralela
- automações fora da IDE
- hacks não oficiais
- alternativas REST fora de API Object

---

# 3. Definição Oficial do MVP

MVP = menor conjunto funcional capaz de:

1. operar dentro da IDE
2. ler uma Transaction real
3. gerar API Object oficial
4. gerar contratos básicos (SDTs)
5. salvar na KB
6. permitir teste inicial simples
7. repetir o fluxo com previsibilidade

---

# 4. Definição Operacional de API Funcional

Para este projeto, no MVP, API funcional significa:

- API Object oficial criado na KB
- objetos gerados com convenção consistente
- estrutura compilável em cenário simples
- endpoints CRUD básicos disponíveis
- utilizável para teste inicial
- editável manualmente na IDE

---

# 5. Escopo Inicial Obrigatório

## Entrada

Uma Transaction existente.

Exemplos:

- Cliente
- Produto
- Pedido
- Fornecedor

## Saída

- `<NomeBase>Api`
- `<NomeBase>Request`
- `<NomeBase>Response`
- `<NomeBase>ListResponse`

---

# 6. Limitações Iniciais Aceitas

## MVP pode limitar para:

- PK simples prioritária
- GeneXus 18 U14+
- .NET prioritário
- UX simples
- casos comuns

## Não precisa resolver agora:

- chave composta total
- todos edge cases
- UX sofisticada
- Java completo
- naming perfeito em 100%
- templates avançados

---

# 7. Arquitetura Técnica Obrigatória

## Princípio central

Arquitetura simples, coesa e implementável.

## Componentes mínimos

### IDE Entry Point

Inicia fluxo dentro da IDE.

### Metadata Reader

Lê Transaction e atributos mínimos.

### Generation Planner

Transforma metadata em plano interno.

### Conflict Resolver

Detecta conflitos e riscos.

### Object Generator

Cria SDTs e API Object pelo caminho validado.

### Result Reporter

Mostra resultado final.

---

# 8. Anti-Overengineering e Anti-Hack

No MVP, evitar abstrações desnecessárias e atalhos não oficiais.

## Não criar salvo necessidade real:

- container DI
- Factory complexa
- Strategy excessiva
- múltiplas camadas artificiais
- arquitetura enterprise precoce

## Proibido como estratégia principal:

- scraping de UI
- automação por clique
- reflection em internals privados
- manipulação interna não documentada
- processos externos paralelos

## Preferir:

- serviços simples
- classes coesas
- código direto
- baixo acoplamento
- evolução incremental

## Exceção

Somente se exigido e suportado oficialmente pelo SDK.

---

# 9. Fluxo Oficial do Produto

Transaction selecionada
→ leitura metadata
→ GenerationPlan
→ análise de conflitos
→ geração SDTs
→ criação API Object
→ persistência KB
→ relatório final

---

# 10. Ordem Real de Execução

# FASE -1 — Spike Técnico Crítico

## Objetivo

Provar viabilidade real.

## Entregas

- extensão carrega na IDE
- comando/menu disponível
- detectar KB ativa
- listar Transactions reais via API oficial disponível
- criar objeto simples suportado pelo SDK
- validar criação/manipulação de API Object oficial

## Gate

Se não houver caminho oficial viável para API Object:

> revisar ou encerrar a tese atual.

---

# FASE 0 — Setup

- solução extensibility
- estrutura de pastas
- convenções provisórias
- build local
- logs básicos

---

# FASE 1 — Wizard Inicial

- selecionar Transaction
- opções essenciais
- confirmar geração
- cancelamento seguro

---

# FASE 2 — Metadata + GenerationPlan

- ler atributos
- detectar PK
- marcar campos sensíveis
- identificar módulo
- gerar plano interno

---

# FASE 3 — SDTs Básicos

Gerar por padrão:

- `<NomeBase>Request`
- `<NomeBase>Response`
- `<NomeBase>ListResponse`

---

# FASE 4 — Reuso Opcional de SDTs

Somente quando:

- compatibilidade for clara
- risco for baixo
- usuário confirmar

---

# FASE 5 — Organização

- gerar `<NomeBase>Api`
- aplicar módulo destino
- aplicar convenção de nomes

---

# FASE 6 — CRUD REST

Prioridade operacional inicial:

- GET lista
- GET por id

Depois completar:

- POST
- PUT
- DELETE

## Regra

GET inicial valida pipeline técnico.
POST/PUT/DELETE continuam parte oficial do MVP.

---

# FASE 7 — Operação IDE

- integrar menu/contexto
- exibir relatório final
- detectar conflito antes salvar
- bloquear overwrite silencioso
- permitir versionamento seguro quando aplicável

---

# FASE 8 — Segurança

- excluir senha automaticamente
- excluir hash automaticamente
- excluir auditoria interna
- permitir revisão manual quando necessário

---

# 11. Definition of Done — MVP v0.1

Considera-se MVP v0.1 pronto quando:

- extensão abre sem erro
- Transaction real é selecionável
- metadata lida corretamente
- `<NomeBase>Api` criado
- SDTs básicos criados
- GET lista funcional
- GET item funcional
- POST funcional inicial
- PUT funcional inicial
- DELETE funcional inicial
- objetos salvos na KB
- rerun seguro disponível
- fluxo repetível ao menos 3x

---

# 12. Falhas e Critérios de Parada

## Parar e revisar se ocorrer:

- impossibilidade oficial de API Object
- corrupção de KB
- save inconsistente
- falhas imprevisíveis recorrentes
- dependência externa anti-tese
- arquitetura excessivamente complexa

## Falha parcial

Se parte dos objetos for criada e outra parte falhar:

- informar claramente
- listar artefatos criados
- evitar resíduos silenciosos
- orientar ação manual

---

# 13. O Que a IA Pode Assumir

- simplicidade vence sofisticação no MVP
- foco inicial é provar valor
- docs-base são fonte oficial
- refactor virá depois
- UX básica é aceitável
- CRUD completo permanece escopo do MVP

---

# 14. O Que a IA Não Deve Assumir

- SDK faz tudo automaticamente
- criação de API Object já está garantida
- GET inicial encerra o MVP
- edge cases podem ser ignorados para sempre
- arquitetura enterprise é necessária
- Java entra no Sprint 1

---

# 15. Primeira Vitória Concreta

Selecionar `Cliente`
→ gerar `ClienteApi`
→ gerar SDTs
→ salvar KB
→ testar GET lista

Se isso funcionar:

> o projeto deixa de ser tese documental e vira software real.

---

# 16. Próximo Passo Recomendado ao Agente

Ler este documento + docs:

- 09-INTEGRACAO_GeneXus_Extensibility_SDK.md
- 10-ENGINE_GERACAO_OBJETOS.md

Depois propor:

- Sprint 0 técnico
- Spike SDK
- arquitetura mínima
- plano inicial de 2 semanas

---

# 17. Mensagem Final

Menos preparação infinita.
Mais prova real.
Mais produto existente.
