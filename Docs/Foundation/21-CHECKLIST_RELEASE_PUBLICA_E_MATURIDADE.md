# 21-CHECKLIST_RELEASE_PUBLICA_E_MATURIDADE.md

## Checklist Oficial para Releases Públicas e Evolução Madura do Projeto

**Projeto:** Genexus Open API Builder  
**Versão:** v1.0
**Base Primária:** 19-OPERACAO_INTERNA_SUPORTE_E_GOVERNANCA_OPEN_SOURCE.md v1  
**Dependência direta:** 20-GUIA_CONTRIBUICAO_E_COLABORADORES.md v1  
**Relacionamento adicional:** 01 a 20 aprovados  
**Objetivo:** definir critérios objetivos para publicar novas versões públicas com qualidade, previsibilidade e confiança da comunidade.  
**Idioma:** Português BR  
**Público principal:** Maintainers + contribuidores + comunidade técnica  
**Data:** Abril/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- evitar releases apressadas
- padronizar qualidade mínima
- reduzir regressões públicas
- melhorar previsibilidade
- reforçar confiança do projeto

Este documento **não substitui julgamento técnico**, **não exige perfeição absoluta**, **não impede releases pequenas úteis**.

---

# 2. Taxonomia

| Código | Tipo | Significado |
|------|------|-------------|
| GOV-F19 | Governança | Operação do projeto |
| CTR-F20 | Contribuição | Fluxo colaborativo |
| REL-F21 | Release | Definição deste documento |
| QA-F15 | Qualidade | Base técnica |
| HP-F21 | Hipótese | Pode evoluir |

---

# 3. Fontes e Rastreabilidade

| Código | Fonte |
|------|-------|
| F15 | TESTES_E_QUALIDADE |
| F18 | LANCAMENTO_OPEN_SOURCE |
| F19 | GOVERNANCA_OPEN_SOURCE |
| F20 | GUIA_CONTRIBUICAO |

---

# 4. Estratégia Oficial

Toda release pública deve buscar:

1. estabilidade maior que pressa  
2. clareza maior que volume  
3. valor real ao usuário  
4. risco controlado  
5. comunicação transparente

[REL-F21]

---

# 5. Tipos de Release

| Tipo | Uso |
|------|-----|
| Patch | correções e pequenos ajustes |
| Minor | melhorias compatíveis |
| Major | mudanças relevantes ou novas bases |

## Exemplos

- v0.1.1  
- v0.2.0  
- v1.0.0

[REL-F21]

---

# 6. Checklist Técnico Mínimo

Antes de publicar:

- build principal ok
- fluxo básico funcionando
- geração principal testada
- sem bug crítico conhecido
- docs mínimas atualizadas
- versão identificada corretamente

[QA-F15][REL-F21]

---

# 7. Checklist Funcional

Validar ao menos:

- gerar API simples
- gerar SDTs padrão
- conflito Safe funciona
- rerun previsível
- logs úteis
- instalação segue funcionando

[REL-F21]

---

# 8. Checklist de Documentação

Confirmar:

- README atualizado
- changelog resumido
- novidades descritas
- limitações conhecidas visíveis
- instruções válidas

[REL-F21]

---

# 9. Checklist Comunitário

Confirmar:

- issues críticas revisadas
- PRs relevantes avaliados
- dúvidas recentes respondidas
- roadmap coerente
- comunicação pronta

[GOV-F19][REL-F21]

---

# 10. O Que Bloqueia Release

Bloquear publicação se houver:

- crash recorrente
- corrupção conhecida
- overwrite indevido
- falha grave instalação
- regressão principal
- bug de segurança relevante

[REL-F21]

---

# 11. O Que NÃO Bloqueia Release

Pode publicar mesmo com:

- melhoria pequena pendente
- issue cosmética aberta
- feature futura atrasada
- refactor desejado não feito
- otimização não crítica

## Regra

Buscar progresso constante.

[REL-F21]

---

# 12. Changelog Oficial

Toda release deve informar:

- o que mudou
- o que corrigiu
- impacto esperado
- migração se houver
- limitações relevantes

## Preferir texto curto e claro.

[REL-F21]

---

# 13. Comunicação Pública

Mensagem ideal:

Nova versão disponível.  
Melhorias reais, correções importantes e evolução contínua.

## Evitar

- hype exagerado
- promessas irreais
- linguagem confusa

[REL-F21]

---

# 14. Ritmo Saudável de Releases

Preferir:

- pequenas releases frequentes
- correções rápidas
- minors consistentes

Evitar:

- longos silêncios sem contexto
- mega releases raras e instáveis

[HP-F21]

---

# 15. Critério de Maturidade do Projeto

## Inicial

Funciona e evolui.

## Intermediário

Confiável e previsível.

## Maduro

Confiável, colaborativo e sustentável.

[REL-F21]

---

# 16. Checklist para v1.0.0

Antes da versão 1.0:

- fluxo principal sólido
- docs fortes
- bugs críticos baixos
- instalação clara
- comunidade ativa mínima
- roadmap coerente
- arquitetura estável

[REL-F21]

---

# 17. Uso Correto por Agentes de IA

## Pode assumir

- release boa entrega valor claro
- confiança demora a construir
- changelog importa
- pequenos avanços contam

## Deve tratar com cautela

- correr por vaidade
- lançar quebrado
- esconder problemas conhecidos

---

# 18. Conclusão Objetiva

Release pública não é só publicar arquivo.

É renovar a confiança de quem usa o projeto.