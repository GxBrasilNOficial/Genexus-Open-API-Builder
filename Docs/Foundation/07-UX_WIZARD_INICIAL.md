# 07-UX_WIZARD_INICIAL.md

## Experiência Inicial do Usuário e Fluxo do Wizard MVP

**Projeto:** Genexus Open API Builder  
**Versão:** v1.0  
**Base Primária:** 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md v1.0  
**Dependência direta:** 05-ARQUITETURA_FUNCIONAL_MVP.md v1.0  
**Relacionamento operacional:** 06-BACKLOG_v0.1.md v2  
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
| B030-B035 | wizard mínimo com decisões obrigatórias |
| B070 | Entrada via menu/contexto |
| B071 | Tela final resultado |
| B083 | Revisão manual futura |

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

## Opcional pós-MVP

- menu Tools
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
| RestPath | Sim |
| Serviços `List/Get/Create/Update` | Sim |
| Filtros e paginação | Sim |
| Security Level/GAM | Sim quando aplicável |

## Valores padrão

| Campo | Default |
|---|---|
| Nome API | `api<Transaction>` |
| Módulo | módulo da Transaction, não editável no MVP |
| Serviços | List/Get/Create/Update habilitados |

## Aviso de Segurança

Campos sensíveis comuns começam desmarcados e com alerta explícito:

- senha
- hash
- auditoria interna segue política separada

## SDTs

O MVP não oferece reuso arbitrário de SDTs externos. O wizard pode mostrar apenas contratos próprios reencontrados por metadata.

- Cancelar, ajustar o SDT antigo e tentar novamente
- Desmarcar a opção e permitir que o gerador crie SDTs novos

## Revisão Manual de Campos

Não faz parte do MVP v1.0.

Planejado para versão futura.

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
| Gerar novo nome | sugere sufixo |
| Cancelar | aborta fluxo |

## Regra

Nunca sobrescrever silenciosamente.

[AF-F05][UX-F07]

---

# 10. Passo 3 — Gerar

## Mostrar resumo final

- Transaction escolhida
- Nome API
- Módulo destino
- Estratégia SDT
- Possíveis objetos a criar

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
- campos sensíveis comuns foram omitidos automaticamente
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
