# 07-UX_WIZARD_INICIAL.md

## Experiência Inicial do Usuário e Fluxo do Wizard MVP

**Projeto:** Genexus Open API Builder
**Versão:** v1.0
**Base Primária:** 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md v1.1
**Dependência direta:** 05-ARQUITETURA_FUNCIONAL_MVP.md v1.1
**Relacionamento operacional:** 06-BACKLOG_v0.1.md v1.1
**Objetivo:** definir a experiência inicial do usuário no wizard oficial do MVP dentro da IDE GeneXus.
**Idioma:** Português BR
**Público principal:** Agentes de IA + mantenedores humanos
**Data:** Abril/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- transformar requisitos do wizard em UX objetiva
- reduzir atrito no primeiro uso
- definir comportamento visual e funcional
- orientar implementação da interface

Este documento **não define código-fonte**, **não escolhe framework UI**, **não substitui F04/F05/F06**.

---

# 2. Taxonomia

| Código | Tipo | Significado |
|---|---|---|
| DP-F04 | Decisão oficial | Requisito aprovado no documento 04 |
| AF-F05 | Arquitetura Funcional | Fluxo aprovado no documento 05 |
| BG-F06 | Backlog | Item planejado no documento 06 |
| UX-F07 | Decisão de UX | Definição deste documento |
| HP-F07 | Hipótese | Precisa validação prática |

---

# 3. Fontes e Rastreabilidade

## [F04]

04-REQUISITOS_MVP_Genexus_Open_API_Builder.md

## [F05]

05-ARQUITETURA_FUNCIONAL_MVP.md

## [F06]

06-BACKLOG_v0.1.md

## Mapeamento relevante F06 → F07

| Backlog | Reflexo UX |
|---|---|
| B020/B021/B022 | seleção de Transaction |
| B030-B037 | wizard mínimo, campos elegíveis e obrigatoriedade no payload |
| B080 | Entrada via menu/contexto |
| B081 | Tela final resultado |
| B083/B084 | painel de conflitos antes de salvar e bloqueio de overwrite silencioso |

---

# 4. Princípios de UX do MVP

Prioridades:

1. clareza imediata
2. poucos cliques
3. baixo risco operacional
4. feedback claro
5. velocidade percebida

Evitar:

- excesso de opções
- telas técnicas demais
- texto longo
- surpresas destrutivas

[UX-F07]

---

# 5. Ponto de Entrada na IDE

## Obrigatório no MVP

- menu contextual de Transaction
- menu principal com seleção nativa filtrada para Transaction e seleção única

O SDK público já demonstrou diálogo de seleção por tipo e suporte a seleção múltipla; o wizard do MVP usa apenas seleção única de Transaction.

## Opcional pós-MVP

- botão contextual dedicado

## Nome inicial da ação

`Generate Open API`

[DP-F04][BG-F06][UX-F07]

---

# 6. Estrutura Oficial do Wizard

Wizard com etapas mínimas e possibilidade de subdivisão visual conforme a implementação.

| Passo | Nome | Objetivo |
|---|---|---|
| 1 | Selecionar Transaction | escolher Transaction |
| 2 | Configurar contrato | revisar serviços, campos, filtros, segurança e paginação |
| 3 | Gerar | confirmar impacto e executar |

[DP-F04][AF-F05][UX-F07]

---

# 7. Passo 1 — Selecionar Transaction

## Elementos visuais

- título claro
- campo busca
- lista de Transactions
- nome + módulo
- botão Próximo
- botão Cancelar

## Regras

- Próximo desabilitado sem seleção
- duplo clique seleciona e avança
- busca filtra em tempo real

## Estado vazio

"Nenhuma Transaction elegível encontrada."

## Comportamento

- sem seleção: permanecer no Passo 1
- nenhuma elegível: permitir fechar wizard

[UX-F07]

---

# 8. Passo 2 — Configurar API

## Campos mínimos

| Campo | Obrigatório |
|---|---|
| Nome da API | Sim |
| Services base path | Sim |
| Caminho comum dos serviços (RestPath) | Sim |
| Serviços `List/Get/Create/Update` | Sim |
| Campos de Create e Update | Sim |
| Filtros de List | Sim |
| Paginação e ordenação | Sim |
| Security Level | Sim |

O wizard não terá campo de descrição de serviço. As descrições são geradas automaticamente conforme as convenções do documento 11.

## Valores padrão

| Campo | Default |
|---|---|
| Nome API | `api<NomeDaTransacao>` |
| Services base path | acompanha Nome API até edição manual |
| RestPath | nome da Transaction em minúsculas separadas por hífen, sem pluralização automática |
| Módulo | módulo da Transaction, não editável no MVP |
| Serviços | List/Get/Create/Update habilitados |
| Default Page Size | 50 |
| Maximum Page Size | 200 |
| Ordenação | chave primária completa ascendente |

## Aviso de Segurança

Campos sensíveis elegíveis começam visíveis, desmarcados e com alerta explícito.

- senha
- hash
- auditoria interna segue política separada

Campos tecnicamente inadequados aparecem desabilitados, com motivo. Campos de auditoria operacional aparecem desabilitados nos Requests e podem aparecer como filtros desmarcados por padrão.

## SDTs

O MVP não oferece reuso arbitrário de SDTs externos. O wizard pode mostrar apenas contratos próprios reencontrados por metadata.

Colisão com SDT externo de mesmo nome bloqueia a geração até o usuário resolver na KB e executar novamente.

## Revisão de Campos

Faz parte do MVP:

- seleção de campos de `CreateRequest`
- seleção de campos de `UpdateRequest`
- campo visível `Obrigatório no payload`
- seleção de filtros de `List`
- escolha de operador/período/intervalo conforme `26-CONTRATO_FILTROS_PAGINACAO_ORDENACAO.md`
- seleção de ordenação estática

**Nota de revisão — 2026-08-23 — Suporte a Subníveis (Fase 5 / B099a):** a lista acima permanece exata para transação de nível único. Havendo subníveis, esta tela recebe quatro acréscimos, entregues em 2026-08-26:

- **agrupamento por nível** — seletor compartilhado (ComboBox com caminho `Shift / Worker`) nas abas Requests, Response e Obrigatórios; o cabeçalho permanece nas listas flat e cada subnível tem listas próprias;
- **dependência entre níveis** — marcar um neto inclui os ancestrais; desmarcar o pai desmarca os descendentes; subnível vazio não é gerado;
- **controle de contador por subnível** — cada filho direto (`Depth == 2`) exibe o contador de `List` ligado por padrão, podendo ser desmarcado;
- **aviso de profundidade** — transação com mais de 4 níveis exibe aviso de profundidade não validada, sem bloquear a geração.

Required de linha aparece na UI e não alimenta o writer BC nesta fase. Detalhes na `Emenda técnica — 2026-08-23` do registro de decisões do MVP e em `Docs/Implementation/2026-08-26-B099a-WIZARD-HIERARQUICO.md`.

## Ações

- Voltar
- Próximo
- Cancelar

[DP-F04][BG-F06][UX-F07]

---

# 9. Painel de Conflitos dentro do Passo 2

Se nome já existir ou objeto conflitar:

| Opção | Resultado |
|---|---|
| Atualizar existente | usa metadata para atualizar objeto próprio |
| Cancelar | aborta fluxo |

## Regra

Nunca sobrescrever silenciosamente.

O MVP não cria sufixos automáticos, não adota objeto externo e não altera nenhum objeto planejado se houver colisão incompatível.

[AF-F05][UX-F07]

---

# 10. Passo 3 — Gerar

## Mostrar resumo final

- Transaction escolhida
- Nome API
- Services base path
- RestPath
- Módulo destino
- Folder específico da Transaction
- SDTs compartilhados em `GxOpenAPI`
- Possíveis objetos a criar
- campos e filtros selecionados
- paginação, ordenação e `Security Level`
- idioma usado nos `[Description]` dos serviços e eventual fallback para inglês

## Botões

- Gerar Agora
- Voltar
- Cancelar

## Durante execução

- barra de progresso simples
- texto de status

Exemplo:

- Lendo metadata...
- Gerando SDTs...
- Criando API...

[UX-F07]

---

# 11. Tela Final de Resultado

## Estado sucesso

Mensagem principal:

"API gerada com sucesso."

## Exibir:

- objetos criados
- objetos atualizados
- tempo total
- avisos
- campos sensíveis selecionados ou mantidos desmarcados
- colisões ou itens que exigem ação manual
- fallback de descrições para inglês, quando o idioma principal da KB não tiver modelo próprio
- botão Abrir objeto principal

## Estado parcial

"API gerada com avisos."

## Estado erro

"Geração interrompida."

[UX-F07]

---

# 12. Microcopy Inicial

## Botões

- Próximo
- Voltar
- Cancelar
- Gerar Agora
- Concluir

## Mensagens

- Selecione uma Transaction.
- Informe um nome válido.
- Já existe objeto com esse nome.
- Processo concluído com sucesso.

[UX-F07]

---

# 13. Regras de Usabilidade

## Obrigatórias

- Enter no Passo 1 com item selecionado = Próximo
- Enter no Passo 2 com campos válidos = Próximo
- Enter no Passo 3 = Gerar
- Esc cancela
- Tab navega campos
- foco inicial no campo principal
- labels claros

## Desejáveis

- lembrar último módulo usado
- lembrar tamanho da janela

[UX-F07]

---

# 14. Regras Visuais

## MVP

- layout limpo
- largura média fixa
- sem poluição visual
- ícones discretos
- espaçamento consistente

## Evitar

- cores excessivas
- linguagem técnica
- popup múltiplo em cascata

[UX-F07]

---

# 15. Estados de Erro

| Situação | Resposta |
|---|---|
| Sem seleção | permanecer no Passo 1 |
| Nenhuma Transaction elegível | informar e permitir fechar |
| Nome inválido | bloquear avanço |
| Falha salvar | mostrar erro claro |
| Conflito nome | abrir painel de conflitos dentro do Passo 2 |
| Timeout interno | permitir fechar |

[UX-F07]

---

# 16. Critérios de Aceite UX

| Critério | Resultado Esperado |
|---|---|
| Abrir wizard | < 2s |
| Fluxo simples completo | < 30s |
| Passos totais | 3 |
| Cliques médios | <= 6 |
| Sem seleção | Próximo desabilitado |
| Conflito nome | abrir painel de conflitos dentro do Passo 2 |
| Cancelar | fechar sem alterar KB |
| Enter Passo 1 | avança com item selecionado |
| Enter Passo 2 | avança com campos válidos |
| Enter Passo 3 | executa geração |

[HP-F07]

---

# 17. Uso Correto por Agentes de IA

## Pode assumir

- UX prioriza simplicidade
- wizard possui 3 passos fixos
- painel de conflitos dentro do Passo 2
- resultado final precisa transparência

## Deve tratar com cautela

- SDK real pode limitar componentes UI
- métricas dependem máquina real
- visual final pode adaptar ao tema IDE

---

# 18. Grau de Confiança

| Área | Grau | Evidência |
|---|---|---|
| Wizard mínimo com decisões obrigatórias | Alto | [F04][F05] |
| Fluxo simples | Alto | [UX-F07] |
| Painel de conflitos dentro do Passo 2 | Alto | [F05] |
| Métricas de tempo | Médio | [HP-F07] |

---

# 19. Conclusão Objetiva

O primeiro uso do produto deve transmitir:

rápido, simples, seguro e previsível.

Se o usuário gerar a primeira API sem ler manual, a UX inicial venceu.
