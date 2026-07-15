# 22-FAQ_TECNICO_E_DECISOES_DE_PROJETO.md

## FAQ Oficial, Perguntas Recorrentes e Decisões Técnicas do Projeto

**Projeto:** Genexus Open API Builder
**Versão:** v1.0
**Base Primária:** 21-CHECKLIST_RELEASE_PUBLICA_E_MATURIDADE.md v1
**Dependência direta:** 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md v1.1
**Relacionamento adicional:** 01 a 21 aprovados
**Objetivo:** consolidar respostas oficiais para dúvidas recorrentes, explicar escolhas técnicas e reduzir ruído em discussões futuras.
**Idioma:** Português BR
**Público principal:** comunidade técnica + usuários + contribuidores
**Data:** Abril/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- responder dúvidas comuns
- explicar decisões do projeto
- reduzir repetição em issues
- alinhar expectativas públicas
- apoiar novos usuários

Este documento **não substitui documentação técnica detalhada**, **não congela evolução futura**, **não invalida roadmap**.

---

# 2. Taxonomia

| Código | Tipo | Significado |
|------|------|-------------|
| MVP-F04 | Escopo MVP | Base funcional |
| OSS-F18 | Open Source | Projeto público |
| FAQ-F22 | FAQ oficial | Definição deste documento |
| GOV-F19 | Governança | Operação pública |
| HP-F22 | Hipótese | Pode evoluir |

---

# 3. Fontes e Rastreabilidade

| Código | Fonte |
|------|-------|
| F04 | REQUISITOS_MVP |
| F16 | ROADMAP_POS_MVP |
| F18 | LANCAMENTO_OPEN_SOURCE |
| F19 | GOVERNANCA_OPEN_SOURCE |
| F21 | RELEASE_PUBLICA |

---

# 4. FAQ — O Projeto é Gratuito?

## Resposta

Sim.

O projeto nasce como open source e gratuito para a comunidade GeneXus.

[FAQ-F22]

---

# 5. FAQ — O Projeto Substitui Desenvolvedores?

## Resposta

Não.

O objetivo é acelerar tarefas repetitivas e gerar base inicial útil.

Decisões de negócio e evolução continuam humanas.

[FAQ-F22]

---

# 6. FAQ — O Projeto Gera Sistemas Completos?

## Resposta

Não.

O foco principal atual é geração inicial de APIs REST baseadas em Transactions.

[FAQ-F22]

---

# 7. FAQ — Por Que Foco em List/Get/Create/Update Primeiro?

## Resposta

Porque `List`, `Get`, `Create` e `Update` resolvem dores frequentes, geram valor rápido e reduzem complexidade inicial. Endpoint `Delete` fica pós-MVP.

[MVP-F04][FAQ-F22]

---

# 8. FAQ — O Código Gerado Pode Ser Editado?

## Resposta

Sim.

O código e objetos gerados devem ser legíveis. Edições manuais precisam ser tratadas com cuidado, porque a regeneração usa metadata persistente e deve detectar conflito ou preservar alterações explicitamente.

[FAQ-F22]

---

# 9. FAQ — Por Que GeneXus é o Nicho Inicial?

## Resposta

Porque o projeto nasce para resolver dores reais observadas nesse ecossistema.

Foco claro aumenta utilidade.

[FAQ-F22]

---

# 10. FAQ — Vai Suportar Tudo no Futuro?

## Resposta

Não necessariamente.

O projeto evolui conforme valor real, demanda e capacidade de manutenção.

[HP-F22]

---

# 11. FAQ — Usa Inteligência Artificial?

## Resposta

A base do projeto é automação previsível.

Recursos de IA podem complementar tarefas específicas no futuro, sem depender de promessas mágicas.

[FAQ-F22]

---

# 12. FAQ — Por Que Open Source?

## Resposta

Para permitir transparência, colaboração e benefício coletivo ao ecossistema.

[OSS-F18][FAQ-F22]

---

# 13. FAQ — Como Posso Contribuir?

## Resposta

- reportando bugs
- sugerindo melhorias
- melhorando docs
- enviando Pull Requests
- testando versões

[GOV-F19][FAQ-F22]

---

# 14. FAQ — O Projeto Está Pronto para Produção?

## Resposta

Depende da fase atual de maturidade.

Consultar releases e notas oficiais.

[FAQ-F22]

---

# 15. FAQ — Por Que Nem Toda Feature Entra?

## Resposta

Porque foco, simplicidade e manutenção saudável valem mais que escopo infinito.

[FAQ-F22]

---

# 16. FAQ — O Projeto Vai Virar Pago?

## Resposta

Este repositório é público e open source.

Estratégias externas futuras não alteram a natureza aberta deste projeto.

[FAQ-F22]

---

# 17. FAQ — Como Reportar Problemas?

## Resposta

Preferencialmente via GitHub Issues com:

- versão usada
- passos reproduzíveis
- comportamento esperado
- erro encontrado

[GOV-F19][FAQ-F22]

---

# 18. FAQ — O Que Diferencia Este Projeto?

## Resposta

- foco específico em GeneXus
- utilidade prática imediata
- código editável
- open source real
- evolução pública

[FAQ-F22]

---

# 19. Uso Correto por Agentes de IA

## Pode assumir

- respostas devem ser objetivas
- transparência aumenta confiança
- foco atual vale mais que promessas futuras

## Deve tratar com cautela

- datas futuras
- comparações absolutas
- garantias irreais

---

# 20. Conclusão Objetiva

Projetos sérios explicam o que fazem.

Projetos maduros também explicam o que não fazem.
