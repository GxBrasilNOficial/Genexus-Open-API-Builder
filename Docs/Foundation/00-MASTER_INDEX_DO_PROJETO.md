# 00-MASTER_INDEX_DO_PROJETO.md

## Índice Mestre do Projeto

| Campo | Valor |
|------|-------|
| Projeto | Genexus Open API Builder |
| Repositório esperado | Genexus-Open-API-Builder |
| Objetivo | Índice oficial, estado atual, ordem de leitura e decisões centrais |
| Idioma | Português BR |
| Público principal | Agentes de IA + mantenedores humanos |
| Data inicial | Abril/2026 |
| Status geral | Planejamento avançado pré-codificação |

---

# 1. Missão do Projeto

Criar uma alternativa **Open Source** para geração de APIs em GeneXus, inspirada em soluções comerciais do mercado, com foco inicial em:

- produtividade real
- padronização
- uso dentro da IDE GeneXus 18
- suporte a grandes KBs
- colaboração comunitária
- futura integração com agentes de IA

---

# 2. Escopo Inicial (MVP)

Gerar rapidamente APIs REST básicas a partir de Transactions GeneXus, com:

- wizard simples
- criação automática de API Object
- CRUD padrão inicial
- Request/Response SDTs
- reuso opcional de SDTs existentes
- operação dentro da IDE
- tratamento de conflitos
- proteção básica contra campos sensíveis

---

# 3. Decisões Estratégicas Congeladas

| Tema | Decisão |
|------|---------|
| Nome produto | Genexus Open API Builder |
| Licença | Open Source |
| Foco inicial | GeneXus 18 |
| Ambiente inicial | IDE desktop GeneXus |
| Prioridade técnica | .NET primeiro |
| Java | Pós-MVP |
| Entrada principal | Menu contextual de Transaction |
| UX inicial | Wizard 3 passos |
| CRUD MVP | 5 endpoints padrão |
| Persistência interna | Mínima |
| Banco próprio | Fora do MVP |

---

# 4. Ordem Oficial de Leitura dos Documentos

| Ordem | Arquivo | Status |
|------:|---------|--------|
| 00 | MASTER_INDEX_DO_PROJETO.md | Atual |
| 01 | LEVANTAMENTO_Funcionalidades_Publicas_WWP_e_K2B.md | Aprovado |
| 02 | COMPARATIVO_ESTRATEGICO_Mercado_e_Oportunidade.md | Aprovado |
| 03 | LACUNAS_DOR_REAL_Desenvolvedor_GeneXus.md | Aprovado |
| 04 | REQUISITOS_MVP_Genexus_Open_API_Builder.md | Aprovado |
| 05 | ARQUITETURA_FUNCIONAL_MVP.md | Aprovado |
| 06 | BACKLOG_v0.1.md | Aprovado |
| 07 | UX_WIZARD_INICIAL.md | Aprovado |
| 08 | MODELO_DADOS_E_METADATA.md | Aprovado |
| 09 | INTEGRACAO_GeneXus_Extensibility_SDK.md | Pendente |
| 10 | ENGINE_GERACAO_OBJETOS.md | Pendente |
| 11 | CONVENCOES_NOMES_E_OUTPUTS.md | Pendente |
| 12 | REGRAS_CRIACAO_API_OBJECTS.md | Futuro |
| 13 | REUSO_E_GERACAO_SDTS.md | Futuro |
| 14 | DETECCAO_CONFLITOS_E_REEXECUCAO.md | Futuro |
| 15 | TESTES_MVP_E_VALIDACAO.md | Futuro |
| 16 | LOGGING_DIAGNOSTICO_E_SUPORTE.md | Futuro |
| 17 | PERFORMANCE_ESCALABILIDADE_KBS_GRANDES.md | Futuro |
| 18 | SEGURANCA_E_BOAS_PRATICAS.md | Futuro |
| 19 | GOVERNANCA_OPEN_SOURCE_GITHUB.md | Futuro |
| 20 | ROADMAP_POS_MVP.md | Futuro |
| 21 | GUIA_CONTRIBUIDORES_COMUNIDADE.md | Futuro |
| 22 | COMPATIBILIDADE_GX18_GX19_FUTURO.md | Futuro |
| 23 | SUPORTE_MULTIGERADOR_DOTNET_JAVA.md | Futuro |
| 24 | OPENAPI_SWAGGER_FUTURO.md | Futuro |
| 25 | INTEGRACAO_IA_AGENTES_COPILOT.md | Futuro |

---

# 5. Estado Atual do Projeto

| Área | Situação |
|------|----------|
| Benchmark inicial | Concluído |
| Oportunidade mercado | Concluído |
| MVP definido | Concluído |
| Arquitetura funcional | Concluído |
| UX inicial | Concluído |
| Modelo interno dados | Concluído |
| Integração SDK real | Próximo passo |
| Geração real objetos | Próximo passo |

---

# 6. Roadmap Curto Prazo

| Prioridade | Documento |
|-----------|-----------|
| Alta | 09-INTEGRACAO_GeneXus_Extensibility_SDK.md |
| Alta | 10-ENGINE_GERACAO_OBJETOS.md |
| Alta | 11-CONVENCOES_NOMES_E_OUTPUTS.md |
| Média | 12-REGRAS_CRIACAO_API_OBJECTS.md |
| Média | 13-REUSO_E_GERACAO_SDTS.md |
| Média | 14-DETECCAO_CONFLITOS_E_REEXECUCAO.md |
| Média | 15-TESTES_MVP_E_VALIDACAO.md |

---

# 7. Padrão Editorial Oficial

Todos os documentos devem seguir:

- português BR
- foco em agentes de IA
- linguagem objetiva
- rastreabilidade entre arquivos
- versões claras
- decisões separadas de hipóteses
- prontos para execução

---

# 8. Como Continuar em Nova Conversa

Usar prompt base:

```text
Estamos continuando o projeto Genexus Open API Builder.

Leia este índice mestre como fonte principal.
Documentos 01 a 08 já estão aprovados.

Agora vamos trabalhar no documento XX.
Mantenha consistência técnica e editorial.