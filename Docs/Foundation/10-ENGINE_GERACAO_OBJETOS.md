# 10-ENGINE_GERACAO_OBJETOS.md

## Engine de Geração de Objetos do MVP

**Projeto:** Genexus Open API Builder  
**Versão:** v1.1  
**Base Primária:** 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md v2.2  
**Dependência direta:** 05-ARQUITETURA_FUNCIONAL_MVP.md v3.1  
**Relacionamento adicional:** 08-MODELO_DADOS_E_METADATA.md v1.4 / 09-INTEGRACAO_GeneXus_Extensibility_SDK.md v3.1  
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
| ModuleTarget | destino |
| GeneratorTarget | .NET / Java |
| ReuseSdt | reuso ou novo |
| RequestSdtName | entrada |
| ResponseSdtName | saída |
| ListSdtName | lista |
| ConflictMode | tratar colisões |
| ReexecutionMode | safe/update/cancel |
| RestArtifactTarget | tipo REST desejado |

## Regra

Sem contrato mínimo válido, a engine não inicia.

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
| Nome ocupado | ClienteApi_v2 |
| Update inseguro | modo Safe |
| REST ideal indisponível | Procedure REST |
| SDT compatível achado | ReuseSdt = true |

## Regra

Persistência só ocorre após plano resolvido válido.

[ENG-F10]

---

# 8. Pipeline Interno Oficial

ApiPlan recebido  
→ validar entrada  
→ resolver conflitos  
→ montar ResolvedGenerationPlan  
→ criar dependências (SDTs)  
→ criar artefato REST  
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

## Se falhar

Abortar antes de criar qualquer objeto.

[ENG-F10]

---

# 10. Fase 2 — Geração de Dependências

## Ordem obrigatória

1. Request SDT
2. Response SDT
3. List SDT

## Regra

Artefato REST principal só pode ser criado após dependências prontas.

[ENG-F10]

---

# 11. Fase 3 — Geração REST Principal

Criar artefato escolhido no doc 09:

| Prioridade | Tipo |
|----------:|------|
| 1 | API Object |
| 2 | Equivalente suportado |
| 3 | Procedure REST |

## Deve conter objetivo CRUD MVP.

[DP-F04][SDK-F09][ENG-F10]

---

# 12. Fase 4 — Persistência

Salvar objetos em ordem:

1. SDTs
2. Artefato principal

## Regra

Se erro parcial:

- interromper sequência
- registrar falha
- impedir novas gravações inseguras

[ENG-F10]

---

# 13. Política de Resíduo em Falha Parcial

## Exemplo

SDTs salvos, REST falhou.

## MVP

Aceitar resíduo controlado quando rollback não for suportado.

## Obrigatório registrar

- quais objetos ficaram salvos
- etapa que falhou
- sugestão de limpeza manual ou reexecução safe

[ENG-F10]

---

# 14. Política de Conflitos

## Definição

Conflito = colisão pontual de nome/objeto durante execução atual.

| Situação | Ação |
|---------|------|
| Novo obrigatório | abortar |
| Update seguro permitido | atualizar |
| Dúvida | pedir decisão usuário |

## MVP conservador

Preferir abortar a sobrescrever.

[ENG-F10]

---

# 15. Política de Reexecução

## Definição

Reexecução = nova rodada intencional para mesma Transaction.

| Modo | Ação |
|------|------|
| Safe | criar versão nova |
| Update | atualizar existente |
| Cancel | não gerar |

## Default MVP

Safe.

[AF-F05][ENG-F10]

---

# 16. Geração Idempotente (Meta)

Mesmas entradas devem gerar comportamento previsível:

- naming estável
- mesma regra de conflito
- mesma escolha de fallback
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
| Artefato REST criado | Sim |
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

# 21. Próxima Etapa Recomendada

Detalhar convenções de nomes em:

`11-CONVENCOES_NOMES_E_OUTPUTS.md`

Sem naming estável, engine perde previsibilidade.

---

# 22. Conclusão Objetiva

A engine do produto deve agir como compilador operacional:

recebe plano, resolve conflitos, gera dependências, cria REST, persiste com segurança e devolve resultado estruturado.