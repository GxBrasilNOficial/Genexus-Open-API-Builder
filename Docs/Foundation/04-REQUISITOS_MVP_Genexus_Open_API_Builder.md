# 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md

## Requisitos Estruturados do Produto Mínimo Viável

**Projeto:** Genexus Open API Builder  
**Versão:** v2.2  
**Objetivo:** definir o escopo mínimo executável do produto, com requisitos claros, rastreáveis e testáveis.  
**Idioma:** Português BR  
**Público principal:** Agentes de IA + mantenedores humanos  
**Data:** Abril/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- transformar hipóteses estratégicas em escopo executável
- limitar o MVP ao essencial
- definir requisitos objetivos
- preparar backlog técnico posterior

Este documento **não define arquitetura interna detalhada**, **não cria tarefas técnicas** e **não amplia escopo além do MVP**.

---

# 2. Metodologia

As afirmações utilizam a seguinte taxonomia:

| Código | Tipo | Significado |
|---|---|---|
| FP | Fato Público | Informação explicitamente comunicada na fonte |
| IP | Inferência Plausível | Dedução razoável baseada na fonte |
| HP | Hipótese | Suposição ainda não validada |
| DP | Decisão de Produto | Escolha oficial do projeto |

---

# 3. Fontes e Rastreabilidade

## [F01] 01-LEVANTAMENTO_Funcionalidades_Publicas_WWP_e_K2B.md

Base de mercado e existência de demanda.

Tipo: documento interno consolidado  
Consultado em: 21/04/2026

---

## [F02] 02-COMPARATIVO_Detalhado_WWP_vs_K2B.md

Comparação pública de posicionamento.

Tipo: documento interno consolidado  
Consultado em: 21/04/2026

---

## [F03] 03-GAPS_e_Oportunidades_Mercado.md

Lacunas plausíveis e oportunidades.

Tipo: documento interno consolidado  
Consultado em: 21/04/2026

---

## [F04] Decisões internas do projeto

Definições estratégicas do Genexus Open API Builder.

Tipo: governança interna  
Consultado em: 21/04/2026

---

# 4. Definição Oficial de MVP

Para este projeto, MVP significa:

> menor conjunto de funcionalidades capaz de gerar APIs REST funcionais dentro da IDE GeneXus.

[DP-F04]

Não significa:

- protótipo descartável
- produto incompleto
- suíte corporativa completa
- plataforma definitiva

---

# 5. Problema Principal a Resolver

Há indícios de que times GeneXus frequentemente precisam:

- expor dados via API
- integrar sistemas externos
- evitar repetição manual
- acelerar entregas
- melhorar padronização técnica

[IP-F01][IP-F02][IP-F03]

---

# 6. Persona Inicial

## Usuário-alvo prioritário

Desenvolvedor GeneXus que:

- já possui Transactions prontas
- precisa gerar APIs rapidamente
- valoriza produtividade
- quer controle técnico do código gerado

[DP-F04]

---

# 7. Escopo Funcional do MVP

## Entrada principal

Selecionar uma Transaction existente. [DP-F04]

Exemplos:

- Cliente
- Produto
- Pedido
- Fornecedor

---

## Saída principal

Gerar API REST funcional para essa Transaction. [DP-F04]

---

## 7.1 Definição de API Funcional no MVP

Para este projeto, API funcional significa:

- objetos gerados com convenção consistente
- endpoints CRUD básicos disponíveis
- estrutura compilável em cenário simples
- passível de teste inicial
- customizações avançadas fora do escopo inicial

[DP-F04]

---

# 8. Funcionalidades Obrigatórias (Must Have)

## 8.1 Geração por Transaction [DP-F04]

Selecionar Transaction e iniciar geração da API correspondente.

### Critério de aceite

Usuário consegue selecionar Transaction válida e iniciar geração sem editar código manual.

---

## 8.2 Endpoints CRUD básicos [DP-F04]

Gerar estrutura para:

- GET lista
- GET por id
- POST
- PUT
- DELETE

### Critério de aceite

Endpoints básicos gerados e prontos para teste inicial em cenário simples.

---

## 8.3 Organização automática de objetos [DP-F04]

Criar objetos com padrão previsível.

Exemplo:

- ClienteApi
- ProdutoApi
- PedidoApi

### Critério de aceite

Objetos gerados seguem convenção única configurada.

---

## 8.4 Reuso opcional de SDTs existentes [DP-F04]

Se houver SDT compatível, permitir reaproveitamento.

### Definição de compatível

SDT com estrutura suficiente para representar entrada e/ou saída sem ausência obrigatória de campos essenciais.

### Critério de aceite

Usuário pode optar por reutilizar SDT detectado.

---

## 8.5 Criação de SDTs quando necessário [DP-F04]

Criar novos contratos quando inexistentes ou incompatíveis.

### Critério de aceite

Geração ocorre sem bloquear ausência prévia de SDT.

---

## 8.6 Wizard simples [DP-F04]

Fluxo curto e objetivo.

### Critério de aceite

Fluxo padrão concluído em até 3 passos sem navegação para ferramenta externa.

---

## 8.7 Operação dentro da IDE [DP-F04]

Acesso via menu/contexto do GeneXus.

### Critério de aceite

Usuário não depende de ferramenta externa para iniciar geração.

---

# 9. Funcionalidades Desejáveis (Should Have)

## 9.1 Seleção de atributos expostos [DP-F04]

Escolher campos da API.

---

## 9.2 Exclusão automática de campos sensíveis [DP-F04]

Exemplos:

- senha
- token
- hash
- auditoria interna

---

## 9.3 Escolha de módulo destino [DP-F04]

Organizar saída na KB.

---

## 9.4 Nome customizado da API [DP-F04]

Exemplo:

- ClienteApi
- ClientesService
- CustomerApi

---

# 10. Fora do MVP (Not Now)

Não entram nesta fase:

- IA generativa
- GraphQL
- Webhooks
- OAuth avançado
- SDK generator
- marketplace
- analytics
- múltiplos templates complexos
- suíte corporativa completa

[DP-F04]

---

# 11. Requisitos de UX

## Fluxo ideal [DP-F04]

1. Selecionar Transaction  
2. Confirmar opções  
3. Gerar

---

## Meta operacional [HP-F04]

Primeira geração concluída em menos de 30 segundos em cenário simples.

---

# 12. Requisitos Técnicos

## Compatibilidade inicial [DP-F04]

- GeneXus 18

---

## Gerador prioritário [DP-F04]

- .NET

---

## Futuro possível [HP-F04]

- Java

---

## Reexecução segura [DP-F04]

Ao rodar novamente:

- detectar objetos existentes
- permitir atualizar
- permitir cancelar
- evitar sobrescrita silenciosa

---

# 13. Requisitos de Qualidade

Código gerado deve ser:

- legível
- previsível
- consistente
- editável manualmente
- sem dependência externa crítica não declarada

[DP-F04]

---

## Critérios objetivos mínimos [DP-F04]

- nomes coerentes entre objetos
- estrutura repetível entre execuções
- geração sem erros em cenário simples
- manutenção manual possível após geração
- objetos podem ser abertos e ajustados manualmente

---

# 14. Critérios de Sucesso do MVP

## Considerar sucesso quando:

### Caso A

Usuário gera primeira API funcional rapidamente.

### Caso B

Usuário entende claramente o que foi criado.

### Caso C

Usuário reutiliza o processo em segunda Transaction sem reaprendizado.

[DP-F04]

---

# 15. Riscos do MVP

| Risco | Tipo | Mitigação | Evidência |
|---|---|---|---|
| Escopo inflado | Alto | foco estrito | [DP-F04] |
| Wizard confuso | Médio | poucas opções | [DP-F04] |
| Código ruim | Alto | templates simples | [DP-F04] |
| Falha em KB grande | Médio | testes progressivos | [DP-F04] |
| Excesso de promessas | Médio | roadmap realista | [DP-F04] |

---

# 16. Uso Correto por Agentes de IA

## Pode assumir com boa confiança

- MVP será focado em CRUD REST inicial [DP-F04]
- integração à IDE é requisito central [DP-F04]
- simplicidade é prioridade [DP-F04]

## Deve tratar com cautela

- MVP não significa produto final
- critérios futuros podem mudar após testes reais
- metas de tempo dependem de implementação
- features fora do MVP não devem contaminar backlog inicial

---

# 17. Saídas Esperadas para Próximos Docs

Este documento alimenta:

- 05-ARQUITETURA_FUNCIONAL_MVP.md
- 06-BACKLOG_v0.1.md
- 07-UX_WIZARD_INICIAL.md

---

# 18. Grau de Confiança

| Área | Grau | Evidência |
|---|---|---|
| Necessidade de geração rápida de APIs | Alto | [IP-F01][IP-F02][IP-F03] |
| Foco correto do MVP em CRUD REST | Alto | [DP-F04] |
| Prioridade de UX simples | Alto | [DP-F04] |
| Compatibilidade inicial GX18 + .NET | Alto | [DP-F04] |
| Meta de 30 segundos | Médio | [HP-F04] |

---

# 19. Conclusão Objetiva

O MVP deve entregar geração inicial de APIs REST funcionais com rapidez, previsibilidade e baixo atrito operacional.

---