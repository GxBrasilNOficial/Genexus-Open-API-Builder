# 04-REQUISITOS_MVP_Genexus_Open_API_Builder

## Requisitos Estruturados do Produto Mínimo Viável

**Projeto:** Genexus Open API Builder  
**Versão:** v1.1  
**Objetivo:** definir o escopo mínimo executável do produto, com requisitos claros, rastreáveis e testáveis.  
**Idioma:** Português BR  
**Público principal:** mantenedores humanos, colaboradores técnicos e apoio por IA  
**Data:** Abril/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- transformar hipóteses estratégicas em escopo executável
- limitar o MVP ao essencial
- definir requisitos objetivos
- preparar backlog técnico posterior

Este documento não define arquitetura interna detalhada, não cria tarefas técnicas e não amplia escopo além do MVP.

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

## [F01] 01-LEVANTAMENTO_PUBLICO_DE_NECESSIDADE_E_OPORTUNIDADE.md

Base consolidada de necessidade e oportunidade.

## [F02] 02-COMPARATIVO_PUBLICO_DE_ABORDAGENS_NO_ECOSSISTEMA_GENEXUS.md

Comparação pública de abordagens relevantes no ecossistema GeneXus.

## [F03] 03-GAPS_E_OPORTUNIDADES_EM_PRODUTIVIDADE_E_APIS_GENEXUS.md

Lacunas plausíveis e oportunidades práticas.

## [F04] Decisões internas do projeto

Definições estratégicas do Genexus Open API Builder.

---

# 4. Definição Oficial de MVP

Para este projeto, MVP significa:

> menor conjunto de funcionalidades capaz de gerar APIs REST funcionais por meio de API Object oficial dentro da IDE GeneXus.

[DP-F04]

Não significa:

- protótipo descartável
- produto incompleto
- suíte corporativa completa
- plataforma definitiva

---

# 5. Problema Principal a Resolver

Há sinais consistentes de que times GeneXus frequentemente precisam:

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

Selecionar uma Transaction existente.

Exemplos:

- Cliente
- Produto
- Pedido
- Fornecedor

[DP-F04]

---

## Saída principal

Gerar API REST funcional para essa Transaction.

[DP-F04]

---

## 7.1 Definição de API REST Funcional no MVP

Para este projeto, API REST funcional significa:

- API Object oficial criado na KB
- objetos gerados com convenção consistente
- serviços REST `List`, `Get`, `Create` e `Update` disponíveis
- estrutura compilável em cenário simples
- passível de teste inicial
- gerada de forma conservadora, sem tratar edição manual como fonte de verdade
- customizações avançadas fora do escopo do MVP

[DP-F04]

---

# 8. Funcionalidades Obrigatórias (Must Have)

## 8.1 Geração por Transaction [DP-F04]

Selecionar Transaction válida e iniciar geração da API correspondente.

### Critério de aceite

Usuário consegue iniciar geração sem editar código manual.

---

## 8.2 Serviços REST básicos [DP-F04]

Gerar estrutura para:

- `List`
- `Get`
- `Create`
- `Update`

### Critério de aceite

Serviços básicos gerados e prontos para teste inicial em cenário simples, com contrato tipado e delegação via Business Component.

### Nota operacional

`Delete` não compõe o endpoint REST do MVP. A remoção de API gerada é uma operação de tooling distinta e deve seguir metadata de geração, não o contrato público da API.

---

## 8.3 Organização automática de objetos [DP-F04]

Criar objetos com padrão previsível.

Exemplos:

- apiCliente
- apiProduto
- apiPedido

### Critério de aceite

Objetos gerados seguem convenção única configurada.

---

## 8.4 Contratos próprios e estáveis [DP-F04]

Gerar SDTs próprios da API, tipados e rastreáveis, sem reuso arbitrário de SDTs externos do usuário.

### Critério de aceite

Geração cria ou reencontra apenas os contratos que pertencem à própria API gerada, identificados por metadata persistente.

---

## 8.5 Criação de SDTs quando necessário [DP-F04]

Criar novos contratos quando inexistentes ou incompatíveis.

### Critério de aceite

Geração ocorre sem depender de SDTs pré-existentes do usuário.

---

## 8.6 Fluxo simples para iniciar geração [DP-F04]

Processo curto e objetivo dentro da IDE.

### Critério de aceite

Usuário inicia e conclui geração sem depender de ferramenta externa.

---

## 8.7 Reexecução segura [DP-F04]

Ao rodar novamente:

- detectar objetos próprios por metadata persistente
- permitir atualizar de forma conservadora
- permitir cancelar
- bloquear colisões incompatíveis
- evitar sobrescrita silenciosa

### Critério de aceite

Usuário entende claramente o impacto antes de confirmar.

---

## 8.8 Resumo final da geração [DP-F04]

Exibir resultado final com:

- objetos criados
- objetos atualizados
- objetos ignorados
- avisos relevantes
- erros encontrados

### Critério de aceite

Usuário entende o resultado sem investigar manualmente a KB.

---

## 8.9 Segurança e campos sensíveis [DP-F04]

Classificar campos sensíveis e de auditoria por regras explícitas, configuráveis por KB, sem omissão silenciosa por heurística ampla.

Exemplos:

- senha
- hash
- token
- auditoria interna

### Critério de aceite

Campos sensíveis aparecem desmarcados por padrão e com alerta; campos de auditoria seguem política própria. A decisão final precisa ser visível ao usuário.

---

# 9. Funcionalidades Desejáveis (Should Have)

## 9.1 Seleção de atributos expostos [DP-F04]

Capacidade desejável para versões evoluídas, sem compor a linha mínima obrigatória do MVP.

Permitir escolher campos expostos pela API REST quando a base principal estiver estável.

---

## 9.2 Escolha de módulo destino [DP-F04]

Organizar saída na KB em versões futuras. No MVP, os objetos gerados ficam no mesmo módulo da Transaction e o módulo não é editável pelo wizard.

---

## 9.3 Nome customizado da estrutura gerada [DP-F04]

Exemplos:

- apiCliente
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
- revisão manual detalhada de atributos
- endpoint `Delete`
- reuso arbitrário de SDTs externos
- versionamento automático por sufixo `_v2`
- escolha livre de módulo destino

[DP-F04]

---

# 11. Requisitos de UX

Fluxo mínimo claro e de baixo atrito, compatível com processo curto de seleção, confirmação e geração dentro da IDE.

## Meta operacional [HP-F04]

Primeira geração em cenário simples com baixo atrito operacional.

---

# 12. Requisitos Técnicos

## Versão inicial suportada [DP-F04]

- GeneXus 18 Upgrade 14 ou superior
- Ambiente inicial de validação: GeneXus 18 Upgrade 15

## Gerador prioritário inicial [DP-F04]

- .NET

## Expansão futura possível [HP-F04]

- Java

## Execução local integrada [DP-F04]

Operação dentro da IDE sem dependência de aplicação externa.

---

# 13. Requisitos de Qualidade

Código gerado deve ser:

- legível
- previsível
- consistente
- rastreável por metadata persistente
- sem dependência externa crítica não declarada

## Critérios objetivos mínimos [DP-F04]

- nomes coerentes entre objetos
- estrutura repetível entre execuções
- geração sem erros em cenário simples
- regeneração conservadora sem sobrescrita silenciosa
- edição manual tratada como conflito ou preservação explícita

---

# 14. Critérios de Sucesso do MVP

## Considerar sucesso quando:

### Caso A

Usuário gera primeira API REST funcional rapidamente.

### Caso B

Usuário entende claramente o que foi criado.

### Caso C

Usuário reutiliza o processo em segunda Transaction sem reaprendizado.

[DP-F04]

---

# 15. Gate Técnico Crítico

Se não for tecnicamente viável criar ou manipular API Objects oficiais por caminho suportado dentro da IDE GeneXus, a tese atual do produto deve ser revisada ou encerrada.

[DP-F04]

---

# 16. Riscos do MVP

| Risco | Grau | Mitigação |
|---|---|---|
| Escopo inflado | Alto | foco estrito |
| Wizard confuso | Médio | poucas opções |
| Código pouco legível ou inconsistente | Alto | templates simples |
| Falha em KB grande | Médio | testes progressivos |
| Excesso de promessas | Médio | roadmap realista |

---

# 17. Uso Correto por Agentes de IA

## Pode assumir com boa confiança

- MVP focado em CRUD REST inicial via API Object oficial
- CRUD no MVP significa `List`, `Get`, `Create` e `Update`; `Delete` é pós-MVP
- integração à IDE é requisito central
- simplicidade é prioridade
- geração rastreável e conservadora é valor importante

## Deve tratar com cautela

- MVP não significa produto final
- critérios futuros podem mudar após testes reais
- metas dependem de implementação
- features fora do MVP não devem contaminar backlog inicial

---

# 18. Saídas Esperadas para Próximos Docs

Este documento alimenta:

- 05-ARQUITETURA_FUNCIONAL_MVP.md
- 06-BACKLOG_v0.1.md
- 07-UX_WIZARD_INICIAL.md

---

# 19. Grau de Confiança

| Área | Grau |
|---|---|
| Necessidade de geração rápida de APIs | Alto |
| Foco correto do MVP em CRUD REST | Alto |
| Prioridade de UX simples | Alto |
| Compatibilidade inicial GX18 + .NET | Alto |
| Contratos próprios e rastreáveis no MVP | Alto |

---

# 20. Conclusão Objetiva

O MVP deve entregar geração inicial de APIs REST funcionais com rapidez, previsibilidade e baixo atrito operacional, dentro da IDE GeneXus, por meio de API Objects oficiais.
