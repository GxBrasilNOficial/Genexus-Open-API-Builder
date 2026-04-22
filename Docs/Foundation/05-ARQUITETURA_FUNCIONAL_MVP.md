# 05-ARQUITETURA_FUNCIONAL_MVP.md

## Arquitetura Funcional do Produto Mínimo Viável

**Projeto:** Genexus Open API Builder  
**Versão:** v3.1  
**Base Primária:** 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md v2.2  
**Objetivo:** detalhar como implementar funcionalmente cada requisito oficial do MVP.  
**Idioma:** Português BR  
**Público principal:** Agentes de IA + mantenedores humanos  
**Data:** Abril/2026

---

# 1. Objetivo do Documento

Este documento converte o F04 em especificação operacional.

Função principal:

- explicar como cada requisito será executado
- remover ambiguidades de implementação
- orientar backlog técnico
- permitir execução assistida por IA

Este documento **não substitui o F04**.

---

# 2. Taxonomia

| Código | Tipo | Significado |
|---|---|---|
| DP-F04 | Decisão oficial | Requisito aprovado no documento 04 |
| AF-F05 | Arquitetura Funcional | Decisão derivada para implementar F04 |
| HP-F05 | Hipótese Técnica | Depende validação prática |

---

# 3. Fontes e Rastreabilidade

## [F04]

04-REQUISITOS_MVP_Genexus_Open_API_Builder.md

Documento base obrigatório.

Consultado em: 21/04/2026

---

# 4. Matriz de Implementação

| F04 | Requisito | Implementação F05 |
|---|---|---|
| 8.1 | Geração por Transaction | Seções 5 e 6 |
| 8.2 | CRUD REST | Seção 10 |
| 8.3 | Organização automática | Seção 9 |
| 8.4 | Reuso SDT | Seção 7 |
| 8.5 | Criar SDT | Seção 8 |
| 8.6 | Wizard 3 passos | Seção 6 |
| 8.7 | Dentro da IDE | Seção 11 |
| 12 | Compatibilidade técnica | Seção 12 |
| 13 | Qualidade | Seção 13 |

[AF-F05]

---

# 5. Pipeline Oficial do MVP (ordem derivada do F04)

## Etapa 1 — F04 8.1 Geração por Transaction

- localizar KB ativa
- listar Transactions elegíveis
- selecionar Transaction

## Etapa 2 — F04 8.6 Wizard Simples

Passo 1: escolher Transaction  
Passo 2: confirmar opções mínimas  
Passo 3: gerar

## Etapa 3 — F04 8.4 Reuso SDT

- procurar SDTs compatíveis
- decidir reutilização

## Etapa 4 — F04 8.5 Criar SDT

- criar contratos faltantes

## Etapa 5 — F04 8.3 Organização Automática

- aplicar nomes padrão
- definir módulo destino

## Etapa 6 — F04 8.2 CRUD REST

- gerar endpoints mínimos

## Etapa 7 — F04 8.7 Operação IDE

- salvar objetos
- mostrar relatório final

[DP-F04][AF-F05]

---

# 6. Wizard Oficial (até 3 passos)

| Passo | Tela | Campos |
|---|---|---|
| 1 | Seleção | Transaction |
| 2 | Configuração | Nome API, módulo, reutilizar SDT |
| 3 | Execução | Confirmar geração |

## Regras

- sem abrir app externo
- fluxo linear
- cancelamento disponível
- se conflito detectado, exibir opções no Passo 2

[DP-F04][AF-F05]

---

# 7. Regra de Reuso de SDT

## SDT compatível quando atender todos:

| Regra | Obrigatório |
|---|---|
| Nome relacionado à Transaction | Sim |
| Possui chave principal | Sim |
| Possui campos essenciais | Sim |
| Estrutura válida entrada/saída | Sim |

## Campos essenciais mínimos

- Id principal
- Nome/Descrição principal (quando existir)
- Campos obrigatórios simples

## Resultado

- Reutilizar automático
- Perguntar usuário
- Rejeitar

[DP-F04][AF-F05]

---

# 8. Regra de Criação de SDT

Criar novo quando:

- nenhum compatível encontrado
- usuário exigir novo
- existente incompleto
- conflito de nome

## Prioridade de criação

1. Request
2. Response
3. ListResponse

[DP-F04][AF-F05]

---

# 9. Convenções de Nomenclatura (F04 8.3)

## Transaction = Cliente

| Tipo | Nome Exato |
|---|---|
| API Principal | ClienteApi |
| Request | ClienteRequest |
| Response | ClienteResponse |
| Lista | ClienteListResponse |

## Regras Gerais

`<Transaction>Api`  
`<Transaction>Request`  
`<Transaction>Response`  
`<Transaction>ListResponse`

## Módulo Destino

Prioridade:

1. módulo escolhido pelo usuário no wizard
2. Property Module da Transaction
3. Root Module da KB

[DP-F04][AF-F05]

---

# 10. Endpoints CRUD Oficiais (F04 8.2)

## Transaction = Cliente

| Endpoint | Método | Path |
|---|---|---|
| Listar | GET | /api/clientes |
| Obter | GET | /api/clientes/{id} |
| Criar | POST | /api/clientes |
| Atualizar | PUT | /api/clientes/{id} |
| Remover | DELETE | /api/clientes/{id} |

## Regra inicial de pluralização

| Singular | Plural |
|---|---|
| Cliente | clientes |
| Produto | produtos |
| Pedido | pedidos |

## Demais casos no MVP

Adicionar `s` ao nome base.

## Fora do MVP

Pluralizações especiais:

- país → países
- animal → animais
- mão → mãos

[DP-F04][AF-F05]

---

# 11. Operação Dentro da IDE (F04 8.7)

Após gerar:

- objetos aparecem na KB
- usuário pode abrir objetos
- relatório exibido em painel/modal interno

## Relatório mínimo

- objetos criados
- objetos atualizados
- conflitos
- avisos
- tempo total de execução

[DP-F04][AF-F05]

---

# 12. Compatibilidade Técnica Inicial

## Escopo MVP

- GeneXus 18
- .NET inicial

## Futuro

- Java

[DP-F04][HP-F05]

---

# 13. Qualidade Obrigatória

Artefatos gerados devem ser:

- legíveis
- consistentes
- repetíveis
- editáveis manualmente
- sem dependência oculta

## Verificação mínima

- abre na IDE
- recompila normalmente
- nomes previsíveis
- segunda geração controlada

[DP-F04][AF-F05]

---

# 14. Reexecução Segura

Se objetos existirem:

## Momento de exibição

Passo 2 do wizard.

| Opção | Ação |
|---|---|
| Atualizar | Regerar compatível |
| Novo Nome | Duplicar com novo nome |
| Cancelar | Nenhuma alteração |

Nunca sobrescrever silenciosamente.

[DP-F04][AF-F05]

---

# 15. Tratamento de Erros

| Erro | Resposta |
|---|---|
| Sem Transaction | Avisar e encerrar |
| Nome em conflito | Solicitar ajuste |
| Falha salvar | Interromper |
| SDT inválido | Informar motivo |

[AF-F05]

---

# 16. Uso Correto por Agentes de IA

## Pode assumir

- Documento 05 implementa F04
- Wizard tem 3 passos fixos
- CRUD REST é núcleo do MVP
- Reusar SDT vem antes de criar novo

## Deve tratar com cautela

- APIs reais da extensibility podem impor limites
- detalhes UI dependem SDK real
- pluralização avançada está fora MVP

---

# 17. Grau de Confiança

| Área | Grau | Evidência |
|---|---|---|
| Pipeline oficial | Alto | [DP-F04][AF-F05] |
| Wizard 3 passos | Alto | [DP-F04] |
| CRUD mínimo | Alto | [DP-F04] |
| Reuso SDT | Alto | [DP-F04][AF-F05] |
| Suporte futuro Java | Médio | [HP-F05] |

---

# 18. Conclusão Objetiva

O F05 operacionaliza o F04 de forma direta:

Selecionar Transaction → Configurar → Decidir SDTs → Gerar CRUD → Salvar na IDE → Revisar relatório.