# 15-TESTES_VALIDACAO_E_QUALIDADE.md

## Regras Oficiais de Testes, Validação e Critérios de Qualidade do MVP

**Projeto:** Genexus Open API Builder
**Versão:** v1.0
**Base Primária:** 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md v1.0
**Dependência direta:** 10-ENGINE_GERACAO_OBJETOS.md v1.0
**Relacionamento adicional:** 12-REGRAS_CRIACAO_API_OBJECTS.md v1.0 / 13-REUSO_E_GERACAO_SDTS.md v1.0 / 14-CONFLITOS_REEXECUCAO_E_VERSIONAMENTO.md v1
**Objetivo:** definir como validar se o produto gera objetos corretos, previsíveis e seguros antes de ser considerado pronto para uso interno.
**Idioma:** Português BR
**Público principal:** Agentes de IA + mantenedores humanos
**Data:** Abril/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- padronizar critérios de pronto
- reduzir regressões
- validar geração automática
- medir estabilidade mínima
- apoiar evolução segura

Este documento **não define roadmap**, **não substitui QA humano**, **não trata marketing**.

---

# 2. Taxonomia

| Código | Tipo | Significado |
|------|------|-------------|
| ENG-F10 | Engine geração | Processo técnico |
| API-F12 | Objetos REST | Saída funcional |
| SDT-F13 | SDTs | Estruturas auxiliares |
| CFG-F14 | Conflitos/versionamento | Segurança operacional |
| QA-F15 | Qualidade/testes | Definição deste documento |
| HP-F15 | Hipótese | Pode evoluir |

---

# 3. Fontes e Rastreabilidade

| Código | Fonte |
|------|-------|
| F10 | ENGINE_GERACAO_OBJETOS |
| F12 | REGRAS_CRIACAO_API_OBJECTS |
| F13 | REUSO_E_GERACAO_SDTS |
| F14 | CONFLITOS_REEXECUCAO_E_VERSIONAMENTO |

---

# 4. Estratégia Oficial

No MVP:

1. testar fluxo principal primeiro
2. automatizar o que for repetível
3. validar cenários de erro
4. priorizar previsibilidade
5. corrigir regressão antes de expandir features

[QA-F15]

---

# 5. Pirâmide de Testes do MVP

| Nível | Foco |
|------|------|
| Unitário | regras puras |
| Integração | SDK + geração |
| Funcional | objeto gerado utilizável |
| Regressão | reruns e versões |
| Manual guiado | UX final |

[QA-F15]

---

# 6. Casos Unitários Obrigatórios

## Validar funções puras

- naming base
- bloqueio de `_v2` automático
- RestPath singular sem pluralização automática
- classificação explícita de sensíveis/auditoria
- reencontro de SDT próprio por metadata
- decisão Safe / Update / Cancel

## Resultado esperado

Determinístico.

[QA-F15]

---

# 7. Casos de Integração Obrigatórios

## Ambiente real GeneXus

- extensão carrega
- wizard abre
- metadata lida
- objeto criado
- save executa
- logs retornam

## Resultado esperado

Fluxo completo sem travar IDE.

[QA-F15]

---

# 8. Casos Funcionais REST

## Transaction simples Cliente

Validar geração de:

- apiCliente
- procCliente_API_List/Get/Create/Update
- sdtCliente_API_CreateRequest
- sdtCliente_API_UpdateRequest
- sdtCliente_API_Response
- sdtCliente_API_ListResponse

## Validar rotas

- List
- Get
- Create
- Update

Não deve existir endpoint `Delete` no MVP.

## Resultado esperado

Estrutura pronta para teste inicial.

[API-F12][QA-F15]

---

# 9. Casos de SDT

## Reencontro

SDT próprio por metadata deve ser reencontrado.

## Novo

SDT externo incompatível deve bloquear se colidir.

## Sensível

Campos senha/token devem iniciar desmarcados com alerta.

[SDT-F13][QA-F15]

---

# 10. Casos de Conflito

| Cenário | Resultado Esperado |
|--------|--------------------|
| apiCliente externo existe + Safe | bloqueia |
| apiCliente externo existe + Cancel | aborta |
| apiCliente próprio + Update | tenta atualizar |
| dúvida estrutural | fallback seguro |

[CFG-F14][QA-F15]

---

# 11. Casos de Reexecução

Gerar duas vezes mesma Transaction.

## Validar

- previsibilidade naming
- ausência de overwrite indevido
- logs corretos
- rerun consistente

[QA-F15]

---

# 12. Casos Negativos

## Entradas inválidas

- sem Transaction
- módulo inexistente
- KB sem permissão
- metadata incompleta
- nome inválido

## Resultado esperado

Erro claro + sem lixo técnico novo.

[QA-F15]

---

# 13. Critérios de Qualidade do Código Gerado

| Critério | Esperado |
|---------|----------|
| Nome legível | Sim |
| Estrutura editável | Sim |
| Dependência oculta crítica | Não |
| Duplicação extrema | Não |
| Campos sensíveis expostos | Não |

[QA-F15]

---

# 14. Critérios de Performance (Meta)

| Cenário | Meta |
|--------|------|
| Transaction pequena | rápida |
| média | aceitável |
| grande | degradar controlado |

## Regra

Sem travar IDE.

[HP-F15]

---

# 15. Critérios de Estabilidade

Após múltiplas execuções:

- sem crash da extensão
- sem corrupção aparente KB
- sem crescimento anormal de erro
- logs utilizáveis

[QA-F15]

---

# 16. Critérios de Pronto Interno

Para uso interno inicial:

- fluxo principal passa
- conflito Safe confiável
- geração básica REST pronta
- SDTs corretos
- erros compreensíveis
- sem bug crítico aberto

[QA-F15]

---

# 17. Critérios de Não Pronto

Bloquear liberação se houver:

- overwrite indevido
- perda de objetos
- crash recorrente IDE
- geração inconsistente
- vazamento de campos sensíveis

[QA-F15]

---

# 18. Evidências Esperadas

Registrar:

- prints
- logs
- KB teste usada
- versão GeneXus
- casos executados
- falhas encontradas

[QA-F15]

---

# 19. Uso Correto por Agentes de IA

## Pode assumir

- teste principal vem antes de edge case
- regressão precisa repetir cenários
- logs são parte do produto
- qualidade inclui segurança

## Deve tratar com cautela

- performance depende ambiente
- REST real depende artefato final
- UX final precisa teste humano

---

# 20. Conclusão Objetiva

No MVP, qualidade significa confiança operacional.

Se gerar certo repetidas vezes sem quebrar ambiente, está no caminho correto.
