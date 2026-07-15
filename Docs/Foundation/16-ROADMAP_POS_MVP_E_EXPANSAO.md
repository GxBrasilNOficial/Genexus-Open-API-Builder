# 16-ROADMAP_POS_MVP_E_EXPANSAO.md

## Roadmap Oficial Pós-MVP e Evolução Estratégica do Produto

**Projeto:** Genexus Open API Builder
**Versão:** v1.0
**Base Primária:** 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md v1.1
**Dependência direta:** 15-TESTES_VALIDACAO_E_QUALIDADE.md v1
**Relacionamento adicional:** 09 a 14 aprovados
**Objetivo:** definir a evolução disciplinada do produto após validação do MVP, evitando crescimento precoce e maximizando valor real.
**Idioma:** Português BR
**Público principal:** Agentes de IA + mantenedores humanos
**Data:** Abril/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- organizar próximos passos
- evitar feature creep
- priorizar valor real
- transformar MVP em produto
- orientar decisões futuras

Este documento **não altera escopo do MVP**, **não invalida docs anteriores**, **não promete datas**.

---

# 2. Taxonomia

| Código | Tipo | Significado |
|------|------|-------------|
| MVP-F04 | Escopo MVP | Base validada |
| QA-F15 | Qualidade | Critério de avanço |
| RDM-F16 | Roadmap | Definição deste documento |
| HP-F16 | Hipótese | Sujeito a mercado |
| EP-F16 | Expansão | Fase futura |

---

# 3. Fontes e Rastreabilidade

| Código | Fonte |
|------|-------|
| F04 | REQUISITOS_MVP |
| F09 | INTEGRACAO_SDK |
| F10 | ENGINE_GERACAO |
| F11 | NAMING |
| F12 | API_OBJECTS |
| F13 | SDTS |
| F14 | CONFLITOS |
| F15 | TESTES_E_QUALIDADE |

---

# 4. Estratégia Oficial

Evoluir em camadas:

1. provar utilidade
2. estabilizar operação
3. ampliar cobertura
4. melhorar experiência
5. escalar comercialmente

[RDM-F16]

---

# 5. Critério para Encerrar MVP

O MVP só termina quando:

- geração principal funciona repetidamente
- rerun seguro funciona
- conflitos previsíveis
- serviços `List`, `Get`, `Create` e `Update` operacionais
- sem bug crítico recorrente
- uso real interno validado

[QA-F15][RDM-F16]

---

# 6. FASE 1 — MVP Validado

## Objetivo

Produto utilizável internamente.

## Entregas mínimas

- geração por Transaction
- apiCliente padrão
- CreateRequest / UpdateRequest / Response / ListResponse
- 4 serviços básicos
- regeneração por metadata confiável
- logs claros

## Resultado esperado

Confiança operacional inicial.

[MVP-F04][RDM-F16]

---

# 7. FASE 2 — Produto Usável

## Objetivo

Sair de protótipo técnico para ferramenta diária.

## Prioridades

- refinamentos avançados de chave composta
- wizard refinado
- override manual de naming
- melhoria de mensagens
- update mode mais seguro
- performance melhorada
- templates extras

## Resultado esperado

Uso recorrente por times internos.

[EP-F16]

---

# 8. FASE 3 — Produto Forte

## Objetivo

Ferramenta madura e vendável.

## Prioridades

- recursos avançados sobre OpenAPI nativo
- autenticação JWT
- políticas avançadas sobre GAM
- soft delete configurável
- múltiplos estilos REST
- políticas enterprise
- branding visual melhorado

## Resultado esperado

Valor comercial real.

[EP-F16]

---

# 9. FASE 4 — Plataforma

## Objetivo

Virar ecossistema.

## Prioridades

- templates por setor
- plugins adicionais
- marketplace interno
- presets por empresa
- analytics de uso
- geração avançada por IA

## Resultado esperado

Produto escalável.

[HP-F16]

---

# 10. O Que NÃO Fazer Cedo Demais

Evitar antes da hora:

- UI sofisticada excessiva
- suporte a tudo
- regras complexas demais
- multi-framework cedo
- automações arriscadas
- micro features sem demanda

## Motivo

Desvia foco do valor principal.

[RDM-F16]

---

# 11. Ordem Recomendada de Features

| Ordem | Feature |
|------:|---------|
| 1 | estabilidade |
| 2 | conflitos seguros |
| 3 | performance |
| 4 | chave composta |
| 5 | OpenAPI export |
| 6 | auth |
| 7 | templates premium |
| 8 | IA avançada |

[RDM-F16]

---

# 12. Critério para Subir de Fase

## Só avançar quando fase atual tiver:

- adoção real
- bugs sob controle
- feedback claro
- arquitetura sustentável
- benefício comprovado

[RDM-F16]

---

# 13. Métricas Importantes

| Métrica | Valor |
|------|------|
| tempo médio geração | cair |
| erros por geração | cair |
| rerun sucesso | subir |
| uso semanal | subir |
| ajustes manuais pós-geração | cair |

[RDM-F16]

---

# 14. Expansão e Ideias Paralelas

Este repositório é exclusivamente open source, gratuito e comunitário.

Qualquer discussão sobre monetização, licenciamento comercial ou modelos de receita pertence ao repositório privado `Genexus-Open-API-Builder-PrivateMap` e não deve constar neste repositório público.

[HP-F16]

---

# 15. Riscos Estratégicos

| Risco | Mitigação |
|------|-----------|
| crescer cedo demais | foco no MVP |
| produto complexo | simplicidade |
| baixa adoção | feedback rápido |
| bugs recorrentes | disciplina QA |
| escopo infinito | fases claras |

[RDM-F16]

---

# 16. Uso Correto por Agentes de IA

## Pode assumir

- roadmap depende validação real
- fase atual vale mais que fase sonhada
- foco operacional vem antes de marketing

## Deve tratar com cautela

- datas exatas
- promessas comerciais
- features sem demanda provada

---

# 17. Conclusão Objetiva

O MVP prova que funciona.

O roadmap garante que evolua sem perder foco.
