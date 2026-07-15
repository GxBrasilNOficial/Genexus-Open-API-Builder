# 08-MODELO_DADOS_E_METADATA.md

## Modelo de Dados Interno e Metadata do Gerador MVP

**Projeto:** Genexus Open API Builder
**Versão:** v1.0
**Base Primária:** 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md v1.0
**Dependência direta:** 05-ARQUITETURA_FUNCIONAL_MVP.md v1.0
**Relacionamento adicional:** 07-UX_WIZARD_INICIAL.md v1.0
**Objetivo:** definir quais dados internos o produto precisa ler, persistir como metadata de geração e processar para gerar APIs no MVP.
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

Este documento **não define banco externo**, **exige metadata persistente em objeto File**, **não substitui F04-F07**.

As decisões internas de filtros, paginação e ordenação devem seguir `26-CONTRATO_FILTROS_PAGINACAO_ORDENACAO.md`.

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
2. manter estado transitório em memória durante a geração
3. persistir metadata de geração em objeto `File`
4. evitar banco próprio

[MD-F08]

---

# 5. Entidades Internas Principais

| Entidade | Finalidade |
|---|---|
| ProjectContext | KB ativa e ambiente |
| TransactionInfo | Dados da Transaction escolhida |
| AttributeInfo | Campos da Transaction |
| GeneratedObjectInfo | Objetos próprios gerados |
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
- gerar serviços REST
- gerar SDTs
- gerar Procedures
- compor metadata persistente

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
| IsAuditField | boolean |
| IsWritableByBC | boolean |
| IsFormula | boolean |
| IsInferred | boolean |
| IsRedundant | boolean |
| IsAutonumber | boolean |
| IsFilterEligible | boolean |
| FilterOperator | texto |
| UsesPeriod | boolean |
| UsesRange | boolean |
| IsPayloadRequired | boolean |
| IsSelectedForCreate | boolean |
| IsSelectedForUpdate | boolean |
| IsSelectedForResponse | boolean |
| DomainName | texto |

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
- marcar campos sensíveis e de auditoria
- gerar contratos corretos
- validar campos obrigatórios
- definir filtros elegíveis

[DP-F04][MD-F08]

---

# 9. Regras de Sensibilidade e Auditoria

Classificar sensibilidade e auditoria por configuração explícita da KB, com nomes e sufixos exatos.

## Sensíveis iniciais

- password
- senha
- hash
- token
- secret

## Auditoria operacional inicial

- InclusaoDataHora
- InclusaoUsuarioId
- InclusaoUsuarioNome
- UltimaAtualizacaoDataHora
- UltimaAtualizacaoUsuarioId
- UltimaAtualizacaoUsuarioNome

## Regra MVP

Campos sensíveis não devem ser omitidos silenciosamente. Eles entram desmarcados por padrão e com alerta no wizard.

Campos de auditoria operacional são reconhecidos por nomes exatos ou sufixos suficientemente específicos. Fragmentos genéricos como `Atualizacao`, `ResumoAtualizacao`, `Usuario` ou `DataHora` não devem ser usados isoladamente.

Campos de auditoria ficam desabilitados em `CreateRequest` e `UpdateRequest`, entram normalmente no `Response` e podem ser filtros de `List`, desmarcados por padrão.

Campos de origem de migração não são auditoria operacional. Um campo como `PessoaOrigemResumoAtualizacao` continua candidato normal a `CreateRequest` e `UpdateRequest` quando atribuível via BC.

[HP-F08][MD-F08]

---

# 10. GeneratedObjectInfo

## Campos mínimos

| Campo | Tipo |
|---|---|
| Name | texto |
| Module | texto |
| ObjectType | texto |
| Role | texto |
| MetadataId | texto |
| LastKnownFingerprint | texto |
| OwnedByGenerator | boolean |

## Uso

- reencontrar objetos próprios
- diferenciar colisão externa de regeneração segura
- compor remoção de API gerada

[AF-F05][MD-F08]

---

# 11. ApiPlan

## Objeto central antes da geração

| Campo | Tipo |
|---|---|
| TransactionName | texto |
| ModuleTarget | texto |
| GeneratorTarget | texto |
| ApiName | texto |
| ServicesBasePath | texto |
| RestPath | texto |
| ProcedureNames | lista texto |
| CreateRequestSdtName | texto |
| UpdateRequestSdtName | texto |
| ResponseSdtName | texto |
| ListFiltersSdtName | texto |
| ListResponseSdtName | texto |
| SharedSdtNames | lista texto |
| TransactionFolderName | texto |
| TransactionFolderWasCreated | boolean |
| SecurityLevel | texto |
| DefaultPageSize | número |
| MaximumPageSize | número |
| StaticOrder | lista |
| ServiceDescriptions | lista |
| ServiceDescriptionLanguage | texto |
| ServiceDescriptionFallbackUsed | boolean |
| EndpointsCount | número |
| MetadataFileName | texto |

## Regra MVP

Serviços padrão geram:

`EndpointsCount = 4`

Os serviços são `List`, `Get`, `Create` e `Update`. `Delete` é pós-MVP como endpoint REST.

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
| CanUpdate | boolean |

## Tipos iniciais

- NameAlreadyExists
- InvalidName
- ModuleBlocked
- ExternalObjectCollision
- IncompatibleMetadata

## Uso

- painel de conflitos no passo 2
- bloquear execução sem sugerir sufixo automático

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

- metadata de geração em objeto `File`, em JSON

## Opcional simples

- último módulo usado
- tamanho da janela
- preferências locais de interface

## Não usar no MVP

- banco próprio
- telemetria remota
- histórico complexo

O contrato detalhado de metadata, regeneração, sincronização e remoção está em `28-METADATA_REGENERACAO_SINCRONIZACAO_E_REMOCAO.md`.

Esta metadata deve preservar também as decisões de campos selecionados, obrigatoriedade no payload, filtros, operadores, períodos, paginação, ordenação, `Services base path`, `RestPath`, `Security Level`, descrições geradas, idioma das descrições, fallback usado e indicação de Folder criado ou reutilizado.

As descrições preservadas na metadata devem permitir detectar alteração manual posterior no objeto `API`, sem sobrescrever silenciosamente o texto editado pelo usuário.

[MD-F08]

---

# 15. Fluxo de Dados Oficial

ProjectContext
→ TransactionInfo
→ AttributeInfo
→ GeneratedObjectInfo
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
| Detectar sensíveis | IsSensitive definido por configuração explícita |
| Detectar auditoria | IsAuditField definido separadamente |
| Confirmar wizard | ApiPlan pronto |
| Serviços padrão | EndpointsCount = 4 |
| Persistir metadata | File JSON criado e relido |
| Finalizar geração | ExecutionResult preenchido |

[MD-F08]

---

# 18. Uso Correto por Agentes de IA

## Pode assumir

- modelo é interno ao produto
- entidades podem virar classes/records/DTOs
- metadata persistente em File é obrigatória
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
| Configuração explícita de sensíveis/auditoria | Médio | [HP-F08] |
| Campos exatos disponíveis via SDK | Médio | [HP-F08] |

---

# 20. Conclusão Objetiva

O MVP precisa de poucos dados internos bem organizados:

contexto → transaction → atributos → SDTs → plano → execução → resultado.

Sem banco próprio e sem complexidade desnecessária.
