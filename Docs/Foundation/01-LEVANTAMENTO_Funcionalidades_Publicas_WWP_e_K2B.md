# 01-LEVANTAMENTO_Funcionalidades_Publicas_WWP_e_K2B.md

## Coleta Estruturada de Informações Públicas

**Projeto relacionado:** Genexus Open API Builder  
**Versão:** v2.1  
**Objetivo:** registrar evidências públicas relevantes sobre soluções existentes do mercado GeneXus voltadas à geração de APIs/serviços.  
**Idioma:** Português BR  
**Público principal:** Agentes de IA + mantenedores humanos  
**Data:** Abril/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- consolidar informações públicas verificáveis
- separar fatos de inferências
- reduzir ambiguidade para agentes de IA
- servir de base para análises futuras

Este documento **não define roadmap**, **não decide MVP** e **não compara produtos em profundidade**.

---

# 2. Metodologia

As informações abaixo foram classificadas em quatro níveis:

| Código | Tipo | Significado |
|---|---|---|
| FP | Fato Público | Informação explicitamente comunicada na fonte |
| IP | Inferência Plausível | Dedução razoável baseada na fonte |
| HP | Hipótese | Suposição ainda não confirmada |
| DP | Decisão de Produto | Escolha interna do projeto (não usada neste doc) |

---

# 3. Fontes e Rastreabilidade

## [F01] WorkWithPlus Services Layer

Fonte pública oficial:  
https://docs.workwithplus.com/wiki?4605,Toc%3AWorkWithPlus+Services+Layer

Tipo: documentação oficial pública  
Consultado em: 21/04/2026

---

## [F02] K2B Tools Service Builder

Fonte pública oficial:  
https://web.k2btools.com/es/soluciones/service-builder

Tipo: página oficial pública  
Consultado em: 21/04/2026

---

## [F03] Padrões comuns do mercado GeneXus

Baseado em observação geral de soluções corporativas GeneXus e posicionamentos públicos.

Tipo: contexto de mercado  
Consultado em: 21/04/2026

---

# 4. Produto A — WorkWithPlus Services Layer

## 4.1 Identificação

- Nome público: WorkWithPlus Services Layer [FP-F01]
- Associado ao ecossistema WorkWithPlus [FP-F01]

---

## 4.2 Sinais públicos observáveis

- Existe produto/módulo dedicado a Services Layer [FP-F01]
- O nome indica foco em camada de serviços/APIs [IP-F01]
- Está vinculado a marca já conhecida no ecossistema GeneXus [FP-F01]
- Provável integração com outros produtos WorkWithPlus [IP-F01]

---

## 4.3 Valor comercial comunicado (interpretação controlada)

- produtividade [IP-F01]
- aceleração de desenvolvimento [IP-F01]
- reaproveitamento de ecossistema existente [IP-F01]

---

## 4.4 Itens ainda não confirmados publicamente

- formato exato de geração de APIs [HP]
- nível de customização [HP]
- qualidade do código gerado [HP]
- suporte detalhado OpenAPI/Swagger [HP]
- compatibilidade específica GeneXus 18 [HP]

---

# 5. Produto B — K2B Tools Service Builder

## 5.1 Identificação

- Nome público: K2B Tools Service Builder [FP-F02]
- Associado ao ecossistema K2B Tools [FP-F02]

---

## 5.2 Sinais públicos observáveis

- Existe produto/módulo dedicado a Service Builder [FP-F02]
- O nome indica foco em construção de serviços [IP-F02]
- Associado a suíte GeneXus corporativa conhecida [IP-F02]
- Provável uso em cenários enterprise [IP-F02]

---

## 5.3 Valor comercial comunicado (interpretação controlada)

- produtividade [IP-F02]
- padronização [IP-F02]
- aceleração de integrações [IP-F02]

---

## 5.4 Itens ainda não confirmados publicamente

- fluxo exato de uso dentro da IDE [HP]
- qualidade do código gerado [HP]
- extensibilidade interna [HP]
- suporte OpenAPI detalhado [HP]
- compatibilidade específica GeneXus 18 [HP]

---

# 6. Padrões de Mercado Derivados

Com base em [F01], [F02] e [F03], existem indícios consistentes de demanda por:

- geração de APIs no ecossistema GeneXus [IP-F03]
- produtividade superior ao processo manual [IP-F03]
- padronização de serviços [IP-F03]
- aceleração de integração entre sistemas [IP-F03]

---

# 7. Limitações Importantes

Este documento **não comprova**:

- superioridade técnica de nenhum produto
- performance interna
- facilidade real de uso
- satisfação de clientes
- qualidade arquitetural

Para isso seriam necessários:

- trial oficial
- uso real
- benchmark prático
- documentação técnica restrita
- entrevistas com usuários

---

# 8. Uso Correto por Agentes de IA

Ao consumir este documento:

## Pode assumir com boa confiança

- os produtos existem [FP-F01][FP-F02]
- são relacionados a serviços/APIs [IP-F01][IP-F02]
- pertencem a suites consolidadas [FP-F01][FP-F02]

## Deve tratar com cautela

- funcionalidades específicas não explicitadas → classificar como [HP] até validação prática
- promessas implícitas de marketing → validar com uso real ou documentação técnica adicional
- conclusões técnicas profundas → exigir benchmark, trial ou evidência empírica
- comparações entre produtos → evitar afirmar superioridade sem teste controlado

---

# 9. Saídas Esperadas para Próximos Docs

Este documento alimenta:

- 02-COMPARATIVO_Detalhado_WWP_vs_K2B.md
- 03-GAPS_e_Oportunidades_Mercado.md
- 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md

---

# 10. Grau de Confiança

| Área | Grau | Evidência |
|---|---|---|
| Existência dos produtos | Alto | [FP-F01][FP-F02] |
| Foco em serviços/APIs | Alto | [IP-F01][IP-F02] |
| Integração com suites maiores | Médio | [IP-F01][IP-F02] |
| Detalhes internos | Baixo | [HP] |
| Oportunidade de mercado | Médio | [IP-F03] |

---

# 11. Conclusão Objetiva

Há evidência pública suficiente para afirmar que o mercado GeneXus possui soluções comerciais voltadas à geração/construção de serviços e APIs.

Os detalhes técnicos internos permanecem parcialmente opacos e devem ser tratados como hipóteses até validação adicional.

---