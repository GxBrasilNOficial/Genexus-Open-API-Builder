# 08-MODELO_DADOS_E_METADATA.md

## Modelo de Dados Interno e Metadata do Gerador MVP

**Projeto:** Genexus Open API Builder  
**Versão:** v1.4  
**Base Primária:** 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md v2.2  
**Dependência direta:** 05-ARQUITETURA_FUNCIONAL_MVP.md v3.1  
**Relacionamento adicional:** 07-UX_WIZARD_INICIAL.md v1.3  
**Objetivo:** definir quais dados internos o produto precisa ler, armazenar temporariamente e processar para gerar APIs no MVP.  
**Idioma:** Português BR  
**Público principal:** Agentes de IA + mantenedores humanos  
**Data:** Abril/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- definir entidades internas mínimas do gerador
- padronizar metadata necessária
- orientar implementação técnica
- reduzir ambiguidade estrutural
- preparar evolução futura

Este documento **não define banco externo**, **não exige persistência obrigatória**, **não substitui F04-F07**.

---

# 2. Taxonomia

| Código | Tipo | Significado |
|---|---|---|
| DP-F04 | Decisão oficial | Requisito aprovado |
| AF-F05 | Arquitetura funcional | Fluxo oficial |
| UX-F07 | UX oficial | Decisão visual/fluxo |
| MD-F08 | Modelo de Dados | Definição deste documento |
| HP-F08 | Hipótese | Depende SDK real |

---

# 3. Fontes e Rastreabilidade

## [F04]

04-REQUISITOS_MVP_Genexus_Open_API_Builder.md

## [F05]

05-ARQUITETURA_FUNCIONAL_MVP.md

## [F07]

07-UX_WIZARD_INICIAL.md

---

# 4. Estratégia Geral

No MVP, preferir:

1. ler metadata da KB em tempo real
2. manter estado temporário em memória
3. persistir apenas preferências simples se necessário
4. evitar banco próprio

[MD-F08]

---

# 5. Entidades Internas Principais

| Entidade | Finalidade |
|---|---|
| ProjectContext | KB ativa e ambiente |
| TransactionInfo | Dados da Transaction escolhida |
| AttributeInfo | Campos da Transaction |
| SdtInfo | SDTs existentes |
| ApiPlan | Plano de geração |
| ConflictInfo | Conflitos detectados |
| ExecutionResult | Resultado final |

[MD-F08]

---

# 6. ProjectContext

## Campos mínimos

| Campo | Tipo |
|---|---|
| KnowledgeBaseName | texto |
| Version | texto |
| ActiveModule | texto |
| GeneratorTarget | texto |
| RootPath | texto |

## Uso

- detectar ambiente atual
- decidir compatibilidade
- compor logs técnicos

[MD-F08]

---

# 7. TransactionInfo

## Campos mínimos

| Campo | Tipo |
|---|---|
| Name | texto |
| Description | texto |
| Module | texto |
| PrimaryKeys | lista texto |
| HasCompositeKey | boolean |
| Attributes | lista AttributeInfo |
| AttributesCount | número |
| IsEligible | boolean |

## Regras

- `HasCompositeKey = true` quando existir mais de uma chave primária
- `PrimaryKeys` contém nomes dos atributos chave
- `AttributesCount = quantidade de Attributes`
- `Attributes` é composição interna de filhos `AttributeInfo`
- `AttributeInfo` não referencia `TransactionInfo`
- carregar apenas atributos da Transaction selecionada

## Uso

- alimentar wizard passo 1
- definir nomes padrão
- gerar endpoints
- gerar SDTs

[AF-F05][UX-F07][MD-F08]

---

# 8. AttributeInfo

## Campos mínimos

| Campo | Tipo |
|---|---|
| Name | texto |
| DataType | texto |
| Length | número |
| Decimals | número |
| IsPrimaryKey | boolean |
| IsRequired | boolean |
| DefaultValue | valor tipado compatível |
| IsSensitive | boolean |
| IsNullable | boolean |

## Exemplos DataType

- Character
- VarChar
- Numeric
- Date
- DateTime
- Boolean
- Blob

## Uso

- montar SDTs
- excluir campos sensíveis
- gerar contratos corretos
- validar campos obrigatórios

[DP-F04][MD-F08]

---

# 9. Regra Inicial de Sensibilidade

Marcar como sensível quando nome sugerir:

- password
- senha
- hash
- token
- secret
- audituser
- auditdate

## MVP

Heurística por nome (case-insensitive).

## Futuro

Leitura avançada por metadata real.

[HP-F08][MD-F08]

---

# 10. SdtInfo

## Campos mínimos

| Campo | Tipo |
|---|---|
| Name | texto |
| Module | texto |
| ItemsCount | número |
| IsCompatible | boolean |
| CompatibilityScore | número |

## Campos essenciais (MVP)

Considerar essenciais:

- todas PrimaryKeys
- atributos com IsRequired = true
- atributo descritivo principal

## Regra para atributo descritivo principal

Prioridade:

1. Name
2. Description
3. Desc
4. primeiro atributo texto que não seja PK

## Regra de incompatibilidade imediata

Se faltar qualquer PrimaryKey:

- `IsCompatible = false`
- `CompatibilityScore = 0`

## Regra mínima de CompatibilityScore

| Critério | Pontos |
|---|---|
| Contém todas PrimaryKeys | +40 |
| Contém campos essenciais | +30 |
| Nome relacionado à Transaction | +20 |
| Mesmo módulo | +10 |

Total máximo: 100

## Uso

- reuso de SDT
- decisão automática
- ranking de candidatos

[AF-F05][MD-F08]

---

# 11. ApiPlan

## Objeto central antes da geração

| Campo | Tipo |
|---|---|
| TransactionName | texto |
| ApiName | texto |
| ModuleTarget | texto |
| GeneratorTarget | texto |
| ReuseSdt | boolean |
| RequestSdtName | texto |
| ResponseSdtName | texto |
| ListSdtName | texto |
| EndpointsCount | número |

## Regra MVP

CRUD padrão gera:

`EndpointsCount = 5`

Exceções futuras ficam fora do escopo atual.

## Uso

- resumo no passo 3
- execução final
- distinguir .NET / Java
- auditoria técnica futura

[AF-F05][UX-F07][MD-F08]

---

# 12. ConflictInfo

## Campos mínimos

| Campo | Tipo |
|---|---|
| ObjectName | texto |
| ConflictType | texto |
| SuggestedName | texto |
| CanUpdate | boolean |

## Tipos iniciais

- NameAlreadyExists
- InvalidName
- ModuleBlocked

## Uso

- painel de conflitos no passo 2

[AF-F05][UX-F07][MD-F08]

---

# 13. ExecutionResult

## Campos mínimos

| Campo | Tipo |
|---|---|
| Success | boolean |
| CreatedCount | número |
| UpdatedCount | número |
| WarningCount | número |
| DurationMs | número |
| MainObjectName | texto |

## Uso

- tela final
- métricas internas
- logs futuros

[UX-F07][MD-F08]

---

# 14. Persistência no MVP

## Obrigatório

- nenhuma persistência externa

## Opcional simples

- último módulo usado
- tamanho da janela
- última opção SDT

## Não usar no MVP

- banco próprio
- telemetria remota
- histórico complexo

[MD-F08]

---

# 15. Fluxo de Dados Oficial

ProjectContext  
→ TransactionInfo  
→ AttributeInfo  
→ SdtInfo  
→ ApiPlan  
→ ConflictInfo (se existir)  
→ ExecutionResult

[AF-F05][MD-F08]

---

# 16. Estruturas que Devem Ser Simples

Evitar no MVP:

- herança complexa
- ORM interno
- cache distribuído
- eventos assíncronos complexos
- banco local obrigatório

[MD-F08]

---

# 17. Critérios de Aceite

| Critério | Resultado Esperado |
|---|---|
| Selecionar Cliente | TransactionInfo preenchido |
| Chave composta | HasCompositeKey = true e PrimaryKeys preenchido |
| Ler atributos | lista AttributeInfo carregada |
| Detectar padrões sensíveis | IsSensitive = true para senha/hash/token |
| Reusar SDT | SdtInfo compatível encontrado |
| Confirmar wizard | ApiPlan pronto |
| CRUD padrão | EndpointsCount = 5 |
| Finalizar geração | ExecutionResult preenchido |

[MD-F08]

---

# 18. Uso Correto por Agentes de IA

## Pode assumir

- modelo é interno ao produto
- entidades podem virar classes/records/DTOs
- persistência mínima é preferida
- ApiPlan é núcleo operacional

## Deve tratar com cautela

- nomes reais dependem linguagem final
- SDK GeneXus pode impor tipos próprios
- metadata disponível pode variar

---

# 19. Grau de Confiança

| Área | Grau | Evidência |
|---|---|---|
| Necessidade dessas entidades | Alto | [F05][F07] |
| Persistência mínima | Alto | [MD-F08] |
| Heurística sensível por nome | Médio | [HP-F08] |
| Campos exatos disponíveis via SDK | Médio | [HP-F08] |

---

# 20. Conclusão Objetiva

O MVP precisa de poucos dados internos bem organizados:

contexto → transaction → atributos → SDTs → plano → execução → resultado.

Sem banco próprio e sem complexidade desnecessária.