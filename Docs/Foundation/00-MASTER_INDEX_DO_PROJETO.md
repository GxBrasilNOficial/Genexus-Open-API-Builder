# 00-MASTER_INDEX_DO_PROJETO

## Índice Mestre, Estado Atual e Direção do Projeto

**Projeto:** Genexus Open API Builder
**Status documental:** Coleção Foundation consolidada; checkpoint operacional em `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`
**Status técnico:** Extensão em linha Alpha; Sprint 9 em andamento (Fase 5/`B099a` concluída; próxima = Fase 5-A/`B099v`)
**Status público:** Repositório público; releases Alpha no GitHub (corte vigente `0.1.0-alpha.4`)
**Idioma:** Português BR
**Público principal:** mantenedor principal, futuros colaboradores técnicos e comunidade interessada
**Data:** 2026-08-26

---

# 1. Objetivo deste Documento

Este é o documento principal da pasta `Docs/Foundation`.

Ele existe para:

- apresentar o projeto com clareza
- registrar o estado atual real
- orientar a leitura dos demais documentos
- consolidar decisões já tomadas
- reduzir interpretações erradas
- servir como referência inicial para evolução futura

Este documento não substitui código-fonte, backlog, testes, decisões futuras ou validação prática.

## Fonte primária das decisões do MVP

O registro consolidado da entrevista funcional de julho de 2026 é a fonte primária das decisões do MVP:

[Registro de decisões funcionais do MVP — 2026-07-14](../Decisions/2026-07-14-REGISTRO_DECISOES_FUNCIONAIS_MVP.md)

Os documentos desta coleção materializam essas decisões nos contratos organizados por assunto. Mudanças posteriores devem atualizar explicitamente o registro de decisões ou seu sucessor e todos os documentos `Foundation` afetados.

O estado operacional e a próxima ação executável são mantidos no [checkpoint do projeto](../STATUS_ATUAL_E_PROXIMO_PASSO.md).

---

# 2. Resumo Executivo

O **Genexus Open API Builder** é um projeto open source criado para acelerar a geração inicial de APIs REST baseadas em **Transactions GeneXus**.

Seu foco é reduzir trabalho repetitivo, aumentar consistência técnica e entregar uma base inicial útil, rastreável e regenerável dentro do ecossistema GeneXus.

O projeto ainda está no início, mas já possui direção documental clara.

---

# 3. Problema Central

Times GeneXus frequentemente enfrentam:

- criação manual repetitiva de CRUDs REST
- falta de padrão entre projetos
- retrabalho técnico recorrente
- demora até a primeira API útil
- esforço excessivo para demandas previsíveis

## Resultado comum

Tempo valioso consumido em tarefas repetitivas.

---

# 4. Solução Proposta

Transformar uma `Transaction` em estrutura inicial REST por meio de um fluxo previsível:

Transaction
→ leitura de metadata
→ plano interno de geração
→ criação de SDTs, Procedures e API Object
→ metadata persistente
→ saída rastreável para evolução segura

## Saídas esperadas do MVP

- API principal
- Procedures de apoio
- SDTs próprios de Create, Update, Response, filtros e lista
- serviços `List`, `Get`, `Create` e `Update`
- naming consistente
- metadata persistente

---

# 5. Escopo Inicial (MVP)

## Inclui

- foco em casos simples e frequentes
- serviços `List`, `Get`, `Create` e `Update`
- chave simples e composta
- interface inicial dentro da IDE
- geração previsível
- reexecução segura
- logs básicos

## Não inclui inicialmente

- todos os cenários existentes
- automação de regras complexas
- cobertura total de edge cases
- promessas irreais de produtividade
- substituição de desenvolvedores

---

# 6. Decisões Estratégicas Atuais

- **Plataforma-alvo:** GeneXus 18 U14 ou posterior, com Upgrade 15 como ambiente inicial de validação
- **Forma do produto:** extensão para a IDE GeneXus
- **Entrada principal:** ação contextual sobre Transaction
- **Público inicial:** comunidade GeneXus
- **Natureza:** open source
- **Licença:** MIT
- **Prioridade atual:** utilidade real
- **Meta inicial:** MVP funcional

---

# 7. Filosofia do Projeto

## O projeto valoriza

- simplicidade
- utilidade prática
- código editável
- transparência
- evolução incremental
- feedback real
- foco técnico

## O projeto evita

- hype vazio
- promessas mágicas
- escopo infinito
- complexidade precoce
- teoria sem entrega

---

# 8. Estrutura Geral do Repositório

- Docs
- Src
- Tests
- Samples
- Tools
- Temp

## Intenção resumida

- `Docs` → conhecimento e alinhamento
- `Docs/Implementation` → evidências reproduzíveis da execução prática
- `Src` → produto real
- `Tests` → validação
- `Samples` → exemplos
- `Tools` → apoio interno

---

# 9. Ordem Recomendada de Leitura

## Mercado e oportunidade

- 01 - LEVANTAMENTO_PUBLICO_DE_NECESSIDADE_E_OPORTUNIDADE
- 02 - COMPARATIVO_PUBLICO_DE_ABORDAGENS_NO_ECOSSISTEMA_GENEXUS
- 03 - GAPS_E_OPORTUNIDADES_EM_PRODUTIVIDADE_E_APIS_GENEXUS

## Produto inicial

- 04 - REQUISITOS_MVP_Genexus_Open_API_Builder
- 05 - ARQUITETURA_FUNCIONAL_MVP
- 06 - BACKLOG_v0.1
- 07 - UX_WIZARD_INICIAL

## Base técnica

- 08 - MODELO_DADOS_E_METADATA
- 09 - INTEGRACAO_GeneXus_Extensibility_SDK
- 10 - ENGINE_GERACAO_OBJETOS
- 11 - CONVENCOES_NOMES_E_OUTPUTS
- 12 - REGRAS_CRIACAO_API_OBJECTS
- 13 - REUSO_E_GERACAO_SDTS
- 14 - CONFLITOS_REEXECUCAO_E_VERSIONAMENTO
- 26 - CONTRATO_FILTROS_PAGINACAO_ORDENACAO
- 27 - CONTRATO_HTTP_ERROS_E_SDTS_COMPARTILHADOS
- 28 - METADATA_REGENERACAO_SINCRONIZACAO_E_REMOCAO

## Validação, evolução e comunidade

- 15 - TESTES_VALIDACAO_E_QUALIDADE
- 16 - ROADMAP_POS_MVP_E_EXPANSAO
- 17 - POSICIONAMENTO_PUBLICO_E_VALOR_COMUNITARIO
- 18 - LANCAMENTO_OPEN_SOURCE_E_ADOCAO_COMUNIDADE
- 19 - OPERACAO_INTERNA_SUPORTE_E_GOVERNANCA_OPEN_SOURCE
- 20 - GUIA_CONTRIBUICAO_E_COLABORADORES
- 21 - CHECKLIST_RELEASE_PUBLICA_E_MATURIDADE
- 22 - FAQ_TECNICO_E_DECISOES_DE_PROJETO
- 23 - RISCOS_LIMITACOES_E_NAO_OBJETIVOS

## Consolidação e execução prática

- 24 - PLANO_IMPLEMENTACAO_REAL_POR_SPRINTS
- 25 - MASTER_SUMARIO_EXECUTIVO_FINAL

## Arquivo morto

- Archive - documentos históricos não normativos

---

# 10. Como Interpretar Esta Coleção

Os documentos representam:

- visão consolidada
- hipóteses bem pensadas
- decisões iniciais
- direcionamento técnico
- preparação para execução real

Se a prática mostrar conflito entre documento e realidade técnica validada, a realidade deve prevalecer e a documentação deve ser atualizada depois.

---

# 11. Próximo Movimento Natural do Projeto

Consultar o [checkpoint operacional](../STATUS_ATUAL_E_PROXIMO_PASSO.md) e executar a única próxima ação nele registrada. Este índice não mantém uma segunda definição independente do próximo passo.

---

# 12. Critério de Sucesso Inicial

O projeto começa a provar valor quando conseguir:

- gerar algo útil de verdade
- economizar tempo real
- ser usado no dia a dia
- receber feedback externo
- evoluir com consistência

---

# 13. Mensagem do Projeto

Menos repetição.
Mais entrega.
Mais valor para a comunidade GeneXus.

---

# 14. Conclusão Final

O Genexus Open API Builder ainda está no começo.

Mas já começa com algo raro:

clareza de direção, foco correto e intenção de construir valor real.
