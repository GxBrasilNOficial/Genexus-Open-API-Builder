# README.md

# Genexus Open API Builder

Ferramenta open source para acelerar a geração de APIs REST baseadas em **Transactions GeneXus**.

Transforma tarefas repetitivas em automação útil, previsível e rastreável.

---

# Objetivo

Reduzir o tempo necessário para criar estruturas iniciais de APIs REST no ecossistema GeneXus.

Em vez de criar tudo manualmente, o projeto gera uma base pronta para evolução.

---

# O Que Gera

A partir de uma Transaction, o projeto busca gerar:

- API principal
- Procedures de apoio
- SDTs próprios de Create, Update, Response, filtros e lista
- SDTs compartilhados de erro e paginação
- serviços `List`, `Get`, `Create` e `Update`
- naming consistente
- metadata persistente para regeneração conservadora

---

# Público-Alvo

- Software houses GeneXus
- Times corporativos internos
- Consultores independentes
- Comunidade técnica
- Estudantes

---

# Estado Atual

A consolidação documental posterior à entrevista funcional do MVP foi concluída.

A base de build mínima foi validada pelo mecanismo oficial disponível a partir do GeneXus 18 U14. A compatibilidade prática com U14 e com o U15 local ainda depende do spike de carregamento do pacote na IDE.

Para retomar o trabalho em uma nova sessão, consulte o checkpoint operacional:

[Estado atual e próximo passo](Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md)

---

# Fonte Primária das Decisões do MVP

O registro consolidado da entrevista funcional de julho de 2026 é a fonte primária das decisões do MVP:

[Registro de decisões funcionais do MVP — 2026-07-14](Docs/Decisions/2026-07-14-REGISTRO_DECISOES_FUNCIONAIS_MVP.md)

Esse registro preserva as decisões funcionais consolidadas. Os documentos em `Docs/Foundation` materializam os contratos organizados por assunto; mudanças posteriores devem atualizar explicitamente as fontes afetadas.

---

# Estrutura do Repositório

- Docs
- Src
- Tests
- Samples
- Tools
- Temp

---

# Documentação Base

A fundação estratégica do projeto está em:

Docs/Foundation/

Evidências reproduzíveis da implementação prática ficam em:

Docs/Implementation/

---

# Filosofia

- Open Source real
- Valor prático
- Simplicidade inicial
- Código rastreável
- Evolução pública
- Sem hype vazio

---

# Como Contribuir

Contribuições são bem-vindas:

- bugs
- melhorias
- documentação
- testes
- ideias úteis
- Pull Requests

Leia também:

CONTRIBUTING.md

---

# Roadmap Resumido

## Fase 1

MVP funcional.

## Fase 2

Produto confiável.

## Fase 3

Expansão técnica.

---

# Mensagem Oficial

Menos repetição.
Mais entrega.
Mais valor para a comunidade GeneXus.

---

# Status

Fundação documental concluída. A próxima ação vigente está no [checkpoint operacional](Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md).
