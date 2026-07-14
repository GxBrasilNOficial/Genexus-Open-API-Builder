# 20-GUIA_CONTRIBUICAO_E_COLABORADORES.md

## Guia Oficial para Contribuições Externas e Colaboração no Projeto

**Projeto:** Genexus Open API Builder  
**Versão:** v1.0
**Base Primária:** 19-OPERACAO_INTERNA_SUPORTE_E_GOVERNANCA_OPEN_SOURCE.md v1  
**Dependência direta:** 15-TESTES_VALIDACAO_E_QUALIDADE.md v1  
**Relacionamento adicional:** 01 a 19 aprovados  
**Objetivo:** facilitar entrada de contribuidores externos com regras claras, ambiente saudável e colaboração produtiva.  
**Idioma:** Português BR  
**Público principal:** contribuidores + maintainers + comunidade técnica  
**Data:** Abril/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- facilitar contribuições externas
- reduzir atrito inicial
- manter padrão técnico
- evitar retrabalho em PRs
- fortalecer comunidade

Este documento **não substitui revisão técnica**, **não garante merge automático**, **não elimina curadoria**.

---

# 2. Taxonomia

| Código | Tipo | Significado |
|------|------|-------------|
| GOV-F19 | Governança | Base organizacional |
| QA-F15 | Qualidade | Critérios técnicos |
| CTR-F20 | Contribuição | Definição deste documento |
| OSS-F20 | Comunidade | Colaboração aberta |
| HP-F20 | Hipótese | Pode evoluir |

---

# 3. Fontes e Rastreabilidade

| Código | Fonte |
|------|-------|
| F15 | TESTES_E_QUALIDADE |
| F18 | LANCAMENTO_OPEN_SOURCE |
| F19 | GOVERNANCA_OPEN_SOURCE |

---

# 4. Estratégia Oficial

Contribuições devem buscar:

1. melhorar produto real
2. manter simplicidade
3. respeitar arquitetura
4. reduzir bugs
5. gerar valor claro

[CTR-F20]

---

# 5. Como Começar

## Passos iniciais

1. ler README  
2. instalar ambiente  
3. rodar projeto  
4. revisar backlog aberto  
5. escolher issue adequada

## Preferência inicial

Issues pequenas.

[OSS-F20]

---

# 6. Tipos de Contribuição Bem-Vindos

- correção de bug
- melhoria de docs
- testes
- UX simples
- refactor seguro
- exemplos reais
- tradução
- feedback técnico

[CTR-F20]

---

# 7. Tipos de Contribuição Sensíveis

Exigem alinhamento prévio:

- mudanças grandes arquitetura
- rewrite completo
- alteração de naming oficial
- mudanças breaking
- dependência pesada nova
- mudança de escopo produto

[GOV-F19]

---

# 8. Fluxo Oficial de Contribuição

Issue  
→ discussão curta  
→ branch própria  
→ implementação  
→ testes  
→ Pull Request  
→ revisão  
→ merge ou ajustes

[CTR-F20]

---

# 9. Pull Request Ideal

## Deve conter

- problema resolvido
- solução aplicada
- impacto esperado
- evidência/teste
- prints se houver UI

## Preferir

PR pequeno e focado.

[CTR-F20]

---

# 10. Padrão Técnico Esperado

- código legível
- nomes claros
- baixo acoplamento
- sem gambiarra oculta
- coerência com docs oficiais

[QA-F15]

---

# 11. Testes Antes do PR

Validar ao menos:

- build ok
- fluxo principal não quebrou
- cenário alterado funciona
- sem regressão óbvia

[QA-F15]

---

# 12. Como Sugerir Features

Abrir issue contendo:

- problema real
- quem sofre isso
- ganho esperado
- alternativa atual
- impacto percebido

## Melhor que pedir só “seria legal”.

[CTR-F20]

---

# 13. Como Revisões Devem Ser

Tom:

- respeitoso
- direto
- técnico
- colaborativo
- sem ego

Mesmo em discordância.

[OSS-F20]

---

# 14. O Que Evitar

Evitar:

- PR gigante sem contexto
- código sem teste mínimo
- discussão hostil
- insistir feature recusada sem novo argumento
- quebrar foco do projeto

[GOV-F19]

---

# 15. Good First Issues

Boas tarefas iniciais:

- docs confusas
- mensagens de erro
- labels
- pequenos bugs
- exemplos README
- testes simples

[CTR-F20]

---

# 16. Reconhecimento Comunitário

Valorizar:

- primeiros PRs
- reports bons
- ajuda em issues
- melhoria de docs
- suporte entre usuários

## Comunidade cresce assim.

[OSS-F20]

---

# 17. Uso Correto por Agentes de IA

## Pode assumir

- contribuição boa resolve problema real
- PR pequeno revisa melhor
- docs também contam
- colaboração depende respeito

## Deve tratar com cautela

- rewrites impulsivos
- mudanças sem contexto
- discussões pessoais

---

# 18. Conclusão Objetiva

Código aberto atrai pessoas.

Processo claro transforma pessoas em colaboradores.