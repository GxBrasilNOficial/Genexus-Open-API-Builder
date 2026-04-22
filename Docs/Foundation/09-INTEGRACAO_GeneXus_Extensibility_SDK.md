# 09-INTEGRACAO_GeneXus_Extensibility_SDK.md

## Integração com GeneXus Extensibility SDK para o MVP

**Projeto:** Genexus Open API Builder  
**Versão:** v3.1  
**Base Primária:** 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md v2.2  
**Dependência direta:** 05-ARQUITETURA_FUNCIONAL_MVP.md v3.1  
**Relacionamento adicional:** 07-UX_WIZARD_INICIAL.md v1.3 / 08-MODELO_DADOS_E_METADATA.md v1.4  
**Objetivo:** definir integração técnica realista com GeneXus 18 via Extensibility SDK, separando fatos confirmados, hipóteses validáveis e fallbacks oficiais.  
**Idioma:** Português BR  
**Público principal:** Agentes de IA + mantenedores humanos  
**Data:** Abril/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- reduzir risco técnico do projeto
- evitar assumir capacidades inexistentes
- orientar spikes objetivos
- conectar UX + metadata + geração
- preparar primeira prova real de funcionamento

Este documento **não substitui teste prático**, **não garante APIs internas**, **não autoriza hacks**.

---

# 2. Taxonomia

| Código | Tipo | Significado |
|------|------|-------------|
| DP-F04 | Decisão oficial | Requisito aprovado |
| AF-F05 | Arquitetura funcional | Fluxo oficial |
| UX-F07 | UX oficial | Wizard e interação |
| MD-F08 | Modelo interno | Dados e estruturas |
| SDK-F09 | Integração SDK | Definição deste documento |
| FP-F09 | Fato público | Evidência pública razoável |
| HP-F09 | Hipótese | Precisa spike |
| FB-F09 | Fallback | Caminho alternativo |
| NA-F09 | Não assumido | Fora do MVP inicial |

---

# 3. Fontes e Rastreabilidade

| Código | Fonte |
|------|-------|
| F01 | Documentação pública GeneXus Extensibility / Platform SDK |
| F02 | Samples públicos oficiais |
| F03 | Documentos internos 04-08 do projeto |

---

# 4. Estratégia Oficial

No MVP:

1. provar integração mínima cedo
2. usar APIs públicas ou reproduzíveis
3. preferir simplicidade operacional
4. manter fallback funcional
5. não travar projeto por feature ideal

[SDK-F09]

---

# 5. Capacidades Confirmadas Publicamente

| Capacidade | Evidência | Grau |
|------|-----------|------|
| Criar extensão para IDE | Docs públicas | Alto |
| Menus / comandos gerais | Samples públicos | Alto |
| Janela / pane / UI básica | Docs + samples | Médio |
| Empacotamento extensão | Docs públicas | Alto |
| Carregamento extensão na IDE | Samples | Alto |

## Observação

Confirmado significa existência geral.  
Não significa garantia do fluxo exato deste produto.

[FP-F09]

---

# 6. Hipóteses Críticas com Critério de Aprovação

| Código | Hipótese | Aprovado quando |
|------|----------|-----------------|
| S01 | Extensão carrega | extensão inicia sem erro na IDE |
| S02 | Menu/comando disponível | comando aparece em algum canal suportado |
| S03 | UI abre | janela abre sem travar IDE |
| S04 | Contexto atual detectável | retorna nome/tipo do objeto selecionado |
| S05 | Ler KB atual | lista ao menos 1 Transaction real |
| S06 | Criar SDT | SDT teste criado e salvo |
| S07 | Criar artefato REST | objeto REST utilizável criado |
| S08 | Persistir alterações | save/update simples concluído |

[HP-F09]

---

# 7. Não Assumido no MVP

Itens desejáveis, porém não pré-condições:

| Item | Status |
|------|--------|
| Context menu específico perfeito | Opcional |
| Wizard nativo sofisticado | Opcional |
| Toolbar dedicada | Opcional |
| Atualização in-place avançada | Opcional |
| Theme visual avançado | Opcional |
| Zero cliques extras | Opcional |

## Regra

Se ausente, o MVP continua viável.

[NA-F09]

---

# 8. Fluxo Técnico Oficial (alinhado F05/F07/F08)

Comando IDE  
→ Resolver contexto  
→ Abrir Wizard (F07)  
→ Ler metadata (F08)  
→ Montar ApiPlan (F08)  
→ Executar geração (F05)  
→ Persistir objetos  
→ Mostrar resultado

[AF-F05][UX-F07][MD-F08]

---

# 9. Entrada na IDE

## Nome desejado

Generate Open API

## Prioridade de canais

1. Context menu Transaction  
2. Menu Tools  
3. Comando geral IDE

## Regra

Sempre usar o melhor canal realmente suportado.

[FB-F09]

---

# 10. UI Oficial do MVP

A UI operacional deve seguir documento 07:

- wizard 3 passos
- navegação simples
- conflito no passo 2
- resumo final
- mensagem de sucesso/erro

## Se SDK limitar UI

Usar janela modal simples com steps internos.

[UX-F07][FB-F09]

---

# 11. Dados Operacionais

Os dados lidos e produzidos devem seguir documento 08:

- TransactionInfo
- AttributeInfo
- SdtInfo
- ApiPlan
- ExecutionResult

[MD-F08]

---

# 12. Artefatos REST Suportados

## Ordem preferencial

| Ordem | Artefato |
|------:|----------|
| 1 | API Object oficial |
| 2 | Objeto REST equivalente suportado |
| 3 | Procedure REST inicial |

## Regra

O objetivo do MVP é expor CRUD REST inicial, não impor tipo específico.

[DP-F04][FB-F09]

---

# 13. Fallback Oficial

Se artefato ideal falhar:

Procedure REST inicial  
+ SDTs  
+ endpoints básicos  
+ naming oficial

Isso continua aderente ao objetivo do MVP.

[FB-F09]

---

# 14. Persistência / Save

## Necessário validar

- criar objeto novo
- salvar sem erro
- update simples
- refresh explorer

## MVP conservador

Se update for arriscado:

- criar novo objeto versionado
- manter existente intacto

[HP-F09][FB-F09]

---

# 15. Política Anti-Hack

## Proibido

- scraping UI
- automação por clique
- reflection em internals privados
- editar arquivos ocultos manualmente
- dependência de IDs secretos

## Permitido

- APIs públicas
- samples oficiais reproduzíveis
- workaround documentado e estável

[SDK-F09]

---

# 16. Riscos de Compatibilidade

| Risco | Impacto |
|------|---------|
| Mudança assemblies GX18 updates | Médio |
| Namespace variar | Médio |
| Samples antigos | Médio |
| Context IDs limitados | Alto |
| Save API restrita | Alto |
| Tipo REST ideal indisponível | Médio |

[HP-F09]

---

# 17. Plano Oficial de Spikes

| Ordem | Spike | Meta |
|------:|------|------|
| 1 | S01 | extensão carrega |
| 2 | S02 | comando aparece |
| 3 | S03 | janela abre |
| 4 | S04 | contexto detectado |
| 5 | S05 | KB consultada |
| 6 | S06 | SDT criado |
| 7 | S07 | REST inicial criado |
| 8 | S08 | save/update simples |

[SDK-F09]

---

# 18. Critérios de Aceite da Integração MVP

| Critério | Resultado Esperado |
|------|--------------------|
| Existe entrada viável IDE | Sim |
| Existe UI mínima viável | Sim |
| Hipóteses possuem teste objetivo | Sim |
| Fallback funcional definido | Sim |
| Docs 07 e 08 conectados | Sim |
| Anti-hack formalizado | Sim |

[SDK-F09]

---

# 19. Uso Correto por Agentes de IA

## Pode assumir

- há indícios razoáveis de suporte a comandos/menu
- UI simples é provável, porém validar
- CRUD REST pode usar fallback
- hipótese precisa prova prática

## Deve tratar com cautela

- nomes reais namespaces/classes
- APIs exatas da build instalada
- diferenças entre updates GX18

---

# 20. Próxima Etapa Recomendada

Executar inicialmente:

1. S01 extensão carrega  
2. S02 comando disponível  
3. S03 UI abre

Depois evoluir conforme resultados de S04 a S08.

Sem núcleo mínimo validado, evitar código grande.

---

# 21. Conclusão Objetiva

O documento 09 transforma sonho em validação prática.

Primeiro provar integração mínima.  
Depois automatizar geração REST com segurança.