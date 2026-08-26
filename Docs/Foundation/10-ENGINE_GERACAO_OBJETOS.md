# 10-ENGINE_GERACAO_OBJETOS.md

## Engine de Geração de Objetos do MVP

**Projeto:** Genexus Open API Builder
**Versão:** v1.0
**Base Primária:** 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md v1.1
**Dependência direta:** 05-ARQUITETURA_FUNCIONAL_MVP.md v1.1
**Relacionamento adicional:** 08-MODELO_DADOS_E_METADATA.md v1.0 / 09-INTEGRACAO_GeneXus_Extensibility_SDK.md v1.0
**Objetivo:** definir como a engine interna transforma metadata + ApiPlan em objetos reais dentro da KB GeneXus.
**Idioma:** Português BR
**Público principal:** Agentes de IA + mantenedores humanos
**Data:** Abril/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- definir pipeline interno de geração
- separar planejamento de execução
- reduzir risco de objetos inconsistentes
- padronizar ordem de criação
- preparar implementação técnica real

Este documento **não define SDK específica**, **não substitui o doc 09**, **não trata UX**.

---

# 2. Taxonomia

| Código | Tipo | Significado |
|------|------|-------------|
| DP-F04 | Decisão oficial | Requisito aprovado |
| AF-F05 | Arquitetura funcional | Fluxo aprovado |
| MD-F08 | Modelo interno | Dados e metadata |
| SDK-F09 | Integração IDE/SDK |
| ENG-F10 | Engine geração | Definição deste documento |
| HP-F10 | Hipótese | Requer ajuste técnico |

---

# 3. Fontes e Rastreabilidade

| Código | Fonte |
|------|-------|
| F04 | REQUISITOS_MVP |
| F05 | ARQUITETURA_FUNCIONAL_MVP |
| F08 | MODELO_DADOS_E_METADATA |
| F09 | INTEGRACAO_GeneXus_Extensibility_SDK |

---

# 4. Estratégia Oficial

A geração deve ocorrer em duas fases:

1. Planejamento
2. Execução

## Regra

Nunca criar objetos diretamente sem plano validado.

[ENG-F10]

---

# 5. Contrato de Entrada da Engine

## Entrada principal

Receber `ApiPlan`.

## Campos mínimos esperados

| Campo | Uso |
|------|-----|
| TransactionName | base da geração |
| ApiName | objeto principal |
| ServicesBasePath | propriedade Services base path do objeto API |
| RestPath | caminho comum dos serviços |
| ModuleTarget | destino |
| GeneratorTarget | .NET / Java |
| ProcedureNames | Procedures de execução |
| CreateRequestSdtName | entrada de Create |
| UpdateRequestSdtName | entrada de Update |
| ResponseSdtName | saída |
| ListFiltersSdtName | filtros de List |
| ListResponseSdtName | envelope de List |
| SharedSdtNames | SDTs compartilhados em `GxOpenAPI` |
| SecurityLevel | valor aplicado aos serviços |
| DefaultPageSize | paginação padrão |
| MaximumPageSize | limite máximo de paginação |
| StaticOrder | ordenação definida no wizard |
| ServiceDescriptions | descrições `[Description]` previstas para os serviços |
| MetadataFileName | metadata persistente |
| ConflictMode | tratar colisões |
| ReexecutionMode | safe/update/cancel |
| RestArtifactTarget | API Object |

**Nota de revisão — 2026-08-23 — Suporte a Subníveis** (atualizada em 2026-08-26 após `B095`): a tabela acima descreve um plano plano, com um nome de SDT por contrato. Com subníveis (B095–B099), o `ApiPlan` passa a carregar também:

- **Já em B095:** a **árvore de níveis** (`ApiPlanLevel`: nome, profundidade, nível pai, ordem, chave primária própria e campos candidatos da estrutura por nível — `Fields`; seleção por contrato = B099+), conforme a seção 21 de `08-MODELO_DADOS_E_METADATA.md`, exposta de forma aditiva em `ApiPlan.Levels` (ainda sem consumidor na geração nem no Wizard flat).
- **Fases seguintes (B096+):** os **nomes dos SDTs derivados** por nível e por contrato (`sdt<NomeBase>_API_<Papel>_<Subnível>`), que deixam de ser deriváveis de um nome único; o nome do **SDT de item de lista** (`sdt<NomeBase>_API_ListResponse_Item`), presente somente quando há subnível selecionado; e quais subníveis têm **contador de `List`** ativo.

Os campos existentes continuam com o mesmo significado, e transação de nível único produz exatamente o mesmo plano de hoje. Detalhes em `Docs/Implementation/2026-08-20-SUPORTE-TRANSACTIONS-SUBNIVEIS.md`.

## Regra

Sem contrato mínimo válido, a engine não inicia.

O contrato de filtros, paginação e ordenação é detalhado em `26-CONTRATO_FILTROS_PAGINACAO_ORDENACAO.md`. O contrato HTTP, erros e SDTs compartilhados é detalhado em `27-CONTRATO_HTTP_ERROS_E_SDTS_COMPARTILHADOS.md`. O contrato de metadata, regeneração, sincronização e remoção é detalhado em `28-METADATA_REGENERACAO_SINCRONIZACAO_E_REMOCAO.md`.

[MD-F08][ENG-F10]

---

# 6. Saída Formal da Engine

| Item | Tipo |
|------|------|
| ObjetosCriados | lista |
| ObjetosAtualizados | lista |
| Warnings | lista |
| Errors | lista |
| ExecutionResult | objeto final |

## Regra

Toda execução deve devolver resultado estruturado.

[ENG-F10]

---

# 7. Plano Resolvido Interno

## Nome oficial

`ResolvedGenerationPlan`

## Finalidade

Converter intenção inicial em plano executável.

## Exemplos de resolução

| Entrada | Saída Resolvida |
|--------|------------------|
| Nome ocupado sem metadata compatível | bloquear colisão |
| Update inseguro | modo Safe |
| REST ideal indisponível | abortar geração |
| Objeto próprio encontrado por metadata | atualizar conservadoramente |

## Regra

Persistência só ocorre após plano resolvido válido.

[ENG-F10]

---

# 8. Pipeline Interno Oficial

ApiPlan recebido
→ validar entrada
→ resolver conflitos
→ montar ResolvedGenerationPlan
→ verificar todos os nomes planejados
→ criar dependências (SDTs)
→ criar Procedures
→ criar API Object
→ gravar metadata em File
→ persistir
→ validar resultado
→ retornar ExecutionResult

[AF-F05][ENG-F10]

---

# 9. Fase 1 — Validação

Verificar:

- nomes válidos
- módulo existente
- Transaction existe
- metadata mínima disponível
- capacidade de criar objeto no destino
- capacidade de salvar via SDK
- `Business Component` habilitado ou autorização explícita para habilitar
- `Security Level`, paginação, ordenação, RestPath e Services base path resolvidos
- descrições `[Description]` resolvidas por idioma da KB, com fallback registrado quando aplicável
- ausência de colisão em qualquer nome planejado

## Se falhar

Abortar antes de criar qualquer objeto.

[ENG-F10]

---

# 10. Fase 2 — Geração de Dependências

## Ordem obrigatória

1. SDTs compartilhados em `GxOpenAPI`
2. SDTs próprios da API
3. Procedures `proc<Nome>_API_*`
4. Metadata inicial em File

## Regra

API Object só pode ser criado após contratos e Procedures estarem prontos.

[ENG-F10]

---

# 11. Fase 3 — Geração REST Principal

Criar API Object, único artefato REST aceito conforme documento 09.

## Deve conter serviços MVP.

Serviços públicos:

- `List`
- `Get`
- `Create`
- `Update`

Não gerar endpoint `Delete` no MVP.

Se API Object não for viável, abortar geração.

[DP-F04][SDK-F09][ENG-F10]

---

# 12. Fase 4 — Persistência

Salvar objetos em ordem:

1. SDTs
2. Procedures
3. API Object
4. File de metadata

## Regra

Se erro parcial:

- interromper sequência
- registrar falha
- impedir novas gravações inseguras

[ENG-F10]

---

# 13. Política de Resíduo em Falha Parcial

## Exemplo

SDTs e Procedures salvos, API Object falhou.

## MVP

Se falha parcial ocorrer, a engine deve **pausar e informar** o usuário:

- quais objetos foram salvos com sucesso
- qual etapa falhou e o motivo
- oferecer opção: manter os objetos salvos e tentar novamente, ou removê-los

Nunca deixar resíduo silencioso na KB sem o usuário saber.

## Obrigatório registrar

- quais objetos ficaram salvos
- etapa que falhou
- decisão do usuário (manter ou remover)
- sugestão de reexecução

[ENG-F10]

---

# 14. Política de Conflitos

## Definição

Conflito = colisão pontual de nome/objeto durante execução atual.

| Situação | Ação |
|---------|------|
| Nome esperado existe sem metadata compatível | abortar |
| Objeto próprio reconhecido por metadata | atualizar conservadoramente |
| Metadata ausente ou corrompida | abortar |
| Qualquer colisão entre nomes planejados | abortar antes de gravar |
| Dúvida | pedir decisão usuário |

## MVP conservador

Preferir abortar a sobrescrever.

O MVP não sobrescreve, adota, apaga nem cria sufixo automaticamente para objetos preexistentes.

[ENG-F10]

---

# 15. Política de Reexecução

## Definição

Reexecução = nova rodada intencional para mesma Transaction.

| Modo | Ação |
|------|------|
| Safe | atualizar apenas objetos próprios reconhecidos |
| Update | atualizar objetos próprios após validação de metadata |
| Cancel | não gerar |

## Default MVP

Safe.

Não criar versão nova por sufixo `_v2` automaticamente.

[AF-F05][ENG-F10]

---

# 16. Geração Idempotente (Meta)

Mesmas entradas devem gerar comportamento previsível:

- naming estável
- mesma regra de conflito
- mesma decisão de geração
- mesmo resultado lógico

## MVP

Aceitar diferenças de timestamp/log.

[HP-F10]

---

# 17. Performance Esperada (Meta Não Contratual)

| Cenário | Meta |
|--------|------|
| Até 20 atributos | rápida |
| 20-80 atributos | normal |
| 80+ atributos | pode degradar |

## Regra

Não processar KB inteira sem necessidade.

[HP-F10]

---

# 18. Logs Técnicos

Registrar:

- início
- fim
- objetos criados
- objetos atualizados
- warnings
- erro final

[ENG-F10]

---

# 19. Critérios de Aceite da Engine

| Critério | Resultado Esperado |
|------|--------------------|
| ApiPlan válido | geração inicia |
| Contrato inválido | aborta cedo |
| Plano resolvido | criado |
| Conflito detectado | tratado corretamente |
| SDTs criados | Sim |
| Procedures criadas | Sim |
| API Object criado | Sim |
| Metadata em File criada e relida | Sim |
| Falha parcial | resultado estruturado |
| Resultado final | ExecutionResult preenchido |

[ENG-F10]

---

# 20. Uso Correto por Agentes de IA

## Pode assumir

- engine trabalha por fases
- ApiPlan é entrada oficial
- ResolvedGenerationPlan governa execução
- falhar cedo é melhor que corromper KB

## Deve tratar com cautela

- save transacional pode variar
- SDK real define detalhes
- tipo REST final depende doc 09

---

# 21. Conclusão Objetiva

A engine do produto deve agir como compilador operacional:

recebe plano, resolve conflitos, gera dependências, cria REST, persiste com segurança e devolve resultado estruturado.
