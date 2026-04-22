# 19-OPERACAO_INTERNA_SUPORTE_E_GOVERNANCA_OPEN_SOURCE.md

## Regras Oficiais de Operação, Manutenção, Suporte e Governança do Projeto

**Projeto:** Genexus Open API Builder  
**Versão:** v1  
**Base Primária:** 18-LANCAMENTO_OPEN_SOURCE_E_ADOCAO_COMUNIDADE.md v2  
**Dependência direta:** 15-TESTES_VALIDACAO_E_QUALIDADE.md v1  
**Relacionamento adicional:** 01 a 18 aprovados  
**Objetivo:** definir como manter o projeto saudável após o lançamento público, garantindo continuidade, organização e confiança da comunidade.  
**Idioma:** Português BR  
**Público principal:** Maintainers + contribuidores + comunidade técnica  
**Data:** Abril/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- organizar manutenção contínua
- estruturar suporte comunitário
- definir governança leve
- reduzir caos operacional
- aumentar confiança pública

Este documento **não trata monetização**, **não define empresa**, **não substitui bom senso técnico**.

---

# 2. Taxonomia

| Código | Tipo | Significado |
|------|------|-------------|
| OSS-F18 | Open Source | Fase pública |
| QA-F15 | Qualidade | Base técnica |
| GOV-F19 | Governança | Definição deste documento |
| OPS-F19 | Operação | Rotina contínua |
| HP-F19 | Hipótese | Pode evoluir |

---

# 3. Fontes e Rastreabilidade

| Código | Fonte |
|------|-------|
| F15 | TESTES_E_QUALIDADE |
| F16 | ROADMAP_POS_MVP |
| F17 | POSICIONAMENTO_PUBLICO |
| F18 | LANCAMENTO_OPEN_SOURCE |

---

# 4. Estratégia Oficial

Após o lançamento:

1. manter estabilidade
2. responder comunidade
3. priorizar bugs reais
4. evoluir com disciplina
5. preservar simplicidade

[GOV-F19]

---

# 5. Estrutura Inicial Recomendada

## Papéis mínimos

| Papel | Responsabilidade |
|------|------------------|
| Maintainer principal | direção técnica |
| Colaborador eventual | PRs pontuais |
| Usuário ativo | feedback real |
| Comunidade | uso e sugestões |

## MVP organizacional

Pode começar com 1 maintainer.

[GOV-F19]

---

# 6. Regras para Issues

## Toda issue deve ter ao menos:

- título claro
- passos reproduzíveis
- versão usada
- comportamento esperado
- comportamento atual

## Etiquetas recomendadas

- bug
- enhancement
- question
- docs
- good first issue

[OPS-F19]

---

# 7. Prioridade Oficial

| Ordem | Tipo |
|------:|------|
| 1 | bug crítico |
| 2 | quebra de geração |
| 3 | segurança |
| 4 | UX bloqueadora |
| 5 | melhoria útil |
| 6 | ideia futura |

[GOV-F19]

---

# 8. Política de Pull Requests

## Requisitos mínimos

- descrição clara
- objetivo definido
- sem quebrar fluxo principal
- código legível
- respeitar docs do projeto

## Preferir

PR pequeno > PR gigante.

[GOV-F19]

---

# 9. Política de Releases

## Modelo simples inicial

| Tipo | Uso |
|------|-----|
| patch | correções |
| minor | melhorias compatíveis |
| major | mudanças relevantes |

## Exemplo

- v0.1.1
- v0.2.0
- v1.0.0

[OPS-F19]

---

# 10. Frequência Saudável

## Melhor ritmo inicial

- pequenos releases frequentes
- correções rápidas
- roadmap público simples

## Evitar

meses de silêncio sem contexto.

[GOV-F19]

---

# 11. Suporte Comunitário

## Canais ideais

- GitHub Issues
- GitHub Discussions
- README / FAQ
- exemplos práticos

## Regra

Suporte público escala melhor que privado.

[OPS-F19]

---

# 12. Como Responder Comunidade

Usar tom:

- respeitoso
- técnico
- objetivo
- transparente
- sem defensividade

## Mesmo em críticas.

[GOV-F19]

---

# 13. O Que NÃO Fazer

Evitar:

- ignorar bugs graves
- discutir de forma hostil
- prometer e sumir
- aceitar PR quebrado
- mudar tudo sem aviso
- centralizar caos

[GOV-F19]

---

# 14. Backlog Público Saudável

## Categorias

- Agora
- Próximo
- Futuro
- Ideias

## Benefício

Comunidade entende direção.

[OPS-F19]

---

# 15. Métricas de Saúde do Projeto

| Métrica | Desejado |
|------|----------|
| tempo resposta issue | cair |
| bugs reabertos | cair |
| contribuições externas | subir |
| releases consistentes | subir |
| abandono percebido | cair |

[HP-F19]

---

# 16. Critério para Aceitar Feature Nova

Só aceitar se houver ao menos um:

- resolve dor recorrente
- simplifica uso
- melhora estabilidade
- reduz retrabalho
- alinha roadmap oficial

[GOV-F19]

---

# 17. Critério para Recusar Feature

Recusar ou adiar se:

- complexidade alta demais
- nicho estreito sem demanda
- quebra foco principal
- manutenção cara
- confunde produto

[GOV-F19]

---

# 18. Uso Correto por Agentes de IA

## Pode assumir

- projeto open source precisa ordem
- bugs reais valem mais que ideias brilhantes
- documentação também é produto
- comunidade observa consistência

## Deve tratar com cautela

- roadmap inchado
- decisões impulsivas
- discussões emocionais

---

# 19. Próxima Etapa Recomendada

Criar:

20-GUIA_CONTRIBUICAO_E_COLABORADORES.md

Para facilitar entrada de contribuidores externos.

---

# 20. Conclusão Objetiva

Projetos open source crescem com código bom.

Mas sobrevivem com manutenção séria.