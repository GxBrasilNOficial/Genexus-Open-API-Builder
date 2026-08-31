# 05-ARQUITETURA_FUNCIONAL_MVP

## Arquitetura Funcional do Produto Mínimo Viável

**Projeto:** Genexus Open API Builder
**Versão:** v1.1
**Base Primária:** 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md
**Objetivo:** descrever a arquitetura funcional mínima para transformar uma Transaction GeneXus em um API Object inicial, de forma segura, simples e executável dentro da IDE.
**Idioma:** Português BR
**Público principal:** mantenedores humanos, colaboradores técnicos e apoio por IA
**Data:** Abril/2026
**Última revisão:** Julho/2026

---

# 1. Objetivo do Documento

Este documento converte os requisitos do documento 04 em arquitetura funcional inicial.

Ele existe para:

- explicar o fluxo interno do MVP
- definir responsabilidades funcionais mínimas
- orientar implementação inicial
- reduzir ambiguidades técnicas
- manter coerência com limitações reais do GeneXus 18 U14 ou posterior, usando Upgrade 15 como ambiente inicial de validação

Este documento não substitui o documento 04, não define design final de classes e não presume capacidades ainda não validadas do SDK.

---

# 2. Princípios Arquiteturais do MVP

A arquitetura inicial deve seguir:

- simplicidade antes de sofisticação
- segurança antes de automação agressiva
- previsibilidade antes de customização ampla
- integração nativa antes de ferramentas externas
- aderência oficial antes de alternativas não compatíveis com a tese
- evolução incremental antes de complexidade estrutural
- plano interno antes da criação de objetos

[AF-F05]

---

# 3. Decisões Funcionais Base

| Tema | Direção Atual |
|---|---|
| Entrada principal | Transaction GeneXus |
| Saída principal | API Object funcional inicial |
| Execução | Dentro da IDE |
| Entradas MVP | Menu principal com seleção nativa filtrada para Transaction; menu de contexto de uma Transaction |
| Prioridade inicial | Gerar valor rápido com segurança |
| SDTs padrão | Criar contratos próprios da API |
| Reuso de SDTs | Fora do MVP, exceto reencontro dos próprios por metadata |
| Reexecução | Sempre confirmar e usar metadata persistente |
| Operações MVP | List, Get, Create, Update (obrigatórios); Delete opt-in, desligado por padrão (`B100`) |
| Delete | Endpoint REST opt-in (`B100`, 2026-08-30); remoção da API gerada continua sendo tooling |
| Camada de execução | API Object delega para Procedures, que usam BC |
| Configuração no wizard | campos de Create/Update, filtros de List, paginação, ordenação, Security Level, API name, Services base path e RestPath |
| Gerador prioritário | .NET |
| Expansão futura | Java |

[DP-F04][AF-F05]

---

# 4. Fluxo Central do Produto

Transaction
→ leitura de metadata
→ GenerationPlan mínimo
→ análise de conflitos
→ geração de contratos (SDTs)
→ geração de Procedures de apoio
→ geração do API Object
→ gravação de metadata persistente em File
→ persistência na KB
→ relatório final

## Regra principal

Nenhum objeto deve ser criado sem plano validado e sem checagem mínima de conflitos.

[AF-F05]

---

# 5. Componentes Funcionais Mínimos

Arquitetura modular na medida certa: organizada o suficiente para evoluir, simples o suficiente para entregar rápido.

## 5.1 IDE Entry Point

Responsável por iniciar o fluxo dentro da IDE.

Funções:

- detectar contexto atual
- receber comando do usuário
- abrir fluxo de geração

---

## 5.2 Metadata Reader

Responsável por ler dados mínimos da Transaction.

Funções:

- nome
- atributos
- chave primária simples ou composta
- módulo
- tipos básicos
- elegibilidade mínima para BC, filtros e contratos

---

## 5.3 Generation Planner

Responsável por transformar metadata em plano interno.

Funções:

- nome base
- contratos previstos
- artefatos previstos
- operações pretendidas (`List`, `Get`, `Create`, `Update`)
- destino no mesmo módulo da Transaction
- Folder específico da Transaction e SDTs compartilhados em `GxOpenAPI`
- Services base path e RestPath
- filtros, paginação, ordenação e Security Level
- decisões pendentes

---

## 5.4 Conflict Resolver

Responsável por detectar riscos.

Funções:

- nomes já existentes
- colisões de módulo
- artefatos incompatíveis
- reexecução
- metadata ausente, divergente ou corrompida

---

## 5.5 Object Generator

Responsável por criar objetos suportados pelo caminho técnico validado.

Funções:

- criar SDTs
- criar Procedures `proc<Nome>_API_*`
- criar API Object
- configurar operações básicas
- associar contratos necessários
- salvar objetos
- aplicar convenções mínimas
- gravar metadata persistente em File

---

## 5.6 Result Reporter

Responsável por apresentar o resultado final.

Funções:

- criados
- ignorados
- avisos
- erros
- próximos passos

---

## 5.7 Layout inicial do código-fonte

O layout abaixo é o contrato de organização inicial de `Src`. Ele define o destino dos primeiros artefatos sem congelar antecipadamente framework, assemblies ou detalhes de empacotamento que pertencem à investigação de `B010`.

| Caminho | Responsabilidade inicial |
|---|---|
| `Src/GenexusOpenApiBuilder.sln` | solution principal do produto |
| `Src/Extension/GenexusOpenApiBuilder.Extension.csproj` | projeto mínimo da extensão, ponto de entrada na IDE e referências ao Extensibility SDK |
| `Src/Core/` | orquestração dos casos de uso e contratos internos independentes da UI |
| `Src/Domain/` | modelos, decisões e invariantes do domínio, incluindo a evolução futura do `ApiPlan` |
| `Src/Infrastructure/` | adaptadores para SDK, persistência, serialização e demais integrações técnicas |
| `Src/UI/` | wizard e componentes de interação com o usuário dentro da IDE |

`B010` cria somente a solution e o projeto mínimo em `Extension`. A criação ou separação de projetos adicionais nas demais pastas depende de necessidade comprovada; `B011` confirma e materializa a estrutura interna sem introduzir camadas vazias por obrigação arquitetural.

**Nota de realidade — 2026-08-23.** O layout acima permanece como contrato de destino, mas **não** descreve a organização atual do código. `Src/Core/`, `Src/Infrastructure/` e `Src/UI/` continuam vazias, com `.gitkeep`, e o código real ficou concentrado em `Src/Extension/`: o wizard em `PrototypeWizardDialog.cs`, a orquestração dos comandos em `Package.cs` e os adaptadores do SDK, escritores de objetos, comparador de sincronização e integridade de metadata em `Src/Extension/Diagnostics/`, que concentra a maior parte do código do projeto sob um nome que sugere diagnóstico e hoje abriga geração.

**Intenção registrada:** a divergência será resolvida em `B104`, **depois** da Sprint 9, decidindo entre mover o código para o layout declarado ou revisar o layout para refletir a organização real. A reorganização não ocorre durante a Sprint 9 porque mover arquivos invalidaria a linha de base de não regressão da Fase 0, que compara byte a byte a saída gerada para transações de nível único. Até lá, quem procurar o wizard ou os adaptadores deve olhar em `Src/Extension/`, não nas pastas reservadas.

[AF-F05]

---

# 6. Fluxo Operacional Detalhado

## Etapa 1 — Início

Entradas obrigatórias do MVP:

- menu contextual sobre Transaction
- menu geral da IDE com seleção manual oficial

As duas entradas usam o mesmo wizard e o mesmo motor de geração. O núcleo técnico aceita coleção de Transactions, mas o MVP executa uma Transaction por vez.

[HP-F05]

---

## Etapa 2 — Leitura

Ler:

- nome da Transaction
- atributos
- chave
- módulo
- viabilidade mínima

Se inválida, encerrar sem alterar KB.

---

## Etapa 3 — GenerationPlan

Plano mínimo contendo:

- origem
- nome base
- contratos
- artefatos
- destino
- conflitos
- estratégia técnica escolhida

---

## Etapa 4 — Conflitos

Verificar:

- nomes existentes
- objetos existentes
- risco de sobrescrita
- limitações conhecidas

---

## Etapa 5 — Confirmação

Usuário confirma com visão clara do impacto.

---

## Etapa 6 — Geração

Criar apenas objetos compatíveis com o caminho técnico validado.

---

## Etapa 7 — Persistência

Salvar alterações.

Se falhar, informar claramente.

Falhas parciais devem:

- registrar o que foi criado
- evitar resíduos silenciosos
- orientar ação manual quando necessária

---

## Etapa 8 — Relatório

Exibir:

- criados
- não criados
- avisos
- erros
- próximos passos

[AF-F05]

---

# 7. Interface do MVP

Fluxo mínimo e rápido, compatível com processo curto dentro da IDE:

1. confirmar origem
2. revisar opções essenciais
3. confirmar geração
4. visualizar resultado

Detalhes de UX pertencem ao documento 07.

[DP-F04]

---

# 8. Estratégia de SDTs no MVP

## Caminho padrão inicial

Criar SDTs próprios da API por padrão.

## Motivo

Reduz:

- ambiguidade
- matching complexo
- dependência de padrões prévios
- risco estrutural

## Reuso de SDTs externos

Não compõe o MVP. O MVP só pode reencontrar e atualizar contratos que pertencem à própria API gerada, identificados por metadata persistente.

Reuso arbitrário por similaridade de estrutura fica pós-MVP.

## Exemplos de contratos

- `sdt<Nome>_API_CreateRequest`
- `sdt<Nome>_API_UpdateRequest`
- `sdt<Nome>_API_Response`
- `sdt<Nome>_API_ListFilters`
- `sdt<Nome>_API_ListResponse`

[DP-F04][AF-F05]

---

# 9. Convenções e Naming

Naming segue prioritariamente o documento:

- 11-CONVENCOES_NOMES_E_OUTPUTS.md

Neste estágio, o objetivo é:

- previsibilidade
- legibilidade
- consistência

Exemplo base:

- apiCliente
- sdtCliente_API_CreateRequest
- sdtCliente_API_Response
- procCliente_API_List

Este documento não congela o padrão final de naming.
As convenções oficiais pertencem ao documento 11.

Detalhes de pluralização, path final e refinamentos dependem da implementação real.

O `Services base path` do objeto `API` e o `RestPath` dos serviços são conceitos distintos e pertencem ao documento 11. O MVP não pluraliza automaticamente o `RestPath`.

[AF-F05]

---

# 10. API Object do MVP

Para Transactions compatíveis com o escopo inicial do MVP, o objetivo funcional inicial é suportar operações equivalentes a:

- `List`
- `Get`
- `Create`
- `Update`

`Delete` é opt-in e fica desligado por padrão (`B100`, 2026-08-30): só entra no API Object quando o operador marca o serviço no Wizard.

O API Object deve delegar a execução para Procedures geradas, e essas Procedures devem usar a Transaction como Business Component quando aplicável.

Sem `Business Component`, o MVP não gera a API. `Create` deve retornar `201`; `Update` usa `PUT`, retorna `200` e devolve `Response` completo.

A operação de **remover uma API gerada** (tooling) permanece distinta do serviço REST `Delete` e continua tratada por metadata, não como serviço público da API.

## Observação crítica

A viabilidade de criação e configuração automática de API Objects depende da validação prática no documento 09.

Se não houver caminho oficial viável para API Object dentro da IDE, a tese atual do produto perde viabilidade.

Não existe fallback funcional fora de API Object oficial.

[HP-F05]

---

# 11. Reexecução Segura

Ao rodar novamente:

- detectar existentes
- mostrar impacto
- pedir confirmação
- permitir cancelamento
- atualizar apenas objetos próprios reconhecidos por metadata
- bloquear colisões incompatíveis
- evitar sobrescrita silenciosa

## Regra principal

Nunca sobrescrever silenciosamente e nunca criar variações automáticas por sufixo `_v2` como solução de conflito.

Antes de qualquer gravação, a engine deve verificar todos os nomes planejados. Qualquer colisão incompatível bloqueia a execução inteira.

[DP-F04]

---

# 12. Tratamento de Erros

| Situação | Comportamento |
|---|---|
| Nenhuma Transaction | informar |
| Transaction inválida | informar motivo |
| Recurso não suportado | informar limitação |
| Nome existente | pedir decisão |
| Falha ao criar | interromper |
| Falha ao salvar | informar |
| Falha parcial | detalhar artefatos criados e próximos passos |

[AF-F05]

---

# 13. Compatibilidade Técnica

## Base inicial

- GeneXus 18 como versão mínima
- Compatibilidade mínima: GeneXus 18 Upgrade 14; ambiente de referência inicial: GeneXus 18 Upgrade 15
- .NET prioritário

## Expansão possível

- Java
- GeneXus Next desejável futuramente, sem bloquear o MVP

## Hipótese Técnica Dependente de Spike

Capacidade de criar e configurar API Objects via Extensibility deve ser validada em prova prática.

## Documento-chave

- 09-INTEGRACAO_GeneXus_Extensibility_SDK.md

[HP-F05]

---

# 14. Qualidade Esperada

Saída gerada deve ser:

- legível
- previsível
- consistente
- rastreável por metadata persistente
- útil em cenário simples
- sem dependência externa crítica não declarada

Verificação mínima:

- objetos aparecem na KB
- objetos abrem normalmente
- convenções respeitadas
- fluxo conclui sem erro crítico
- metadata sobrevive à reabertura da KB
- edição manual é detectada como conflito ou preservação explícita

[DP-F04]

---

# 15. Fora da Arquitetura Inicial

Não fazem parte do MVP:

- plugin system
- IA generativa
- GraphQL
- marketplace
- múltiplos templates complexos
- engine aberta a terceiros
- pluralização avançada
- matching complexo de SDTs
- reuso arbitrário de SDTs externos
- `Delete` ligado por padrão (o MVP só gera o serviço quando o operador marca o opt-in)
- versionamento automático por `_v2`
- escolha livre de módulo destino
- hacks fora do SDK oficial
- scraping de UI
- automação por clique
- reflection em internals privados
- alternativas REST fora de API Object

[DP-F04]

---

# 16. Relação com Outros Documentos

Este documento orienta:

- 06-BACKLOG_v0.1.md
- 07-UX_WIZARD_INICIAL.md
- 08-MODELO_DADOS_E_METADATA.md
- 09-INTEGRACAO_GeneXus_Extensibility_SDK.md
- 10-ENGINE_GERACAO_OBJETOS.md
- 11-CONVENCOES_NOMES_E_OUTPUTS.md
- 14-CONFLITOS_REEXECUCAO_E_VERSIONAMENTO.md
- 15-TESTES_VALIDACAO_E_QUALIDADE.md
- documentos transversais de filtros, contrato HTTP e metadata/ciclo de vida

---

# 17. Uso Correto por Agentes de IA

## Pode assumir

- arquitetura pragmática
- fluxo Transaction → plano → geração → relatório
- contratos próprios como padrão
- API Object delegando para Procedures e BC
- reexecução segura obrigatória
- API Object é objetivo funcional central

## Deve tratar com cautela

- viabilidade real depende do documento 09
- UX detalhada pertence ao 07
- naming detalhado pertence ao 11
- criação via SDK depende spike real
- sem API Object o MVP perde viabilidade atual

---

# 18. Grau de Confiança

| Área | Grau |
|---|---|
| Fluxo central proposto | Alto |
| Contratos próprios como padrão | Alto |
| Reexecução segura | Alto |
| Entrada contextual IDE | Médio |
| Criação de API Object via SDK | Baixo/Médio |
| Expansão futura Java | Baixo |

---

# 19. Conclusão Objetiva

A arquitetura funcional do MVP deve permanecer simples, segura e adaptável à realidade do SDK.

Fluxo central:

Transaction
→ Metadata
→ Plan
→ Conflitos
→ SDTs
→ API Object
→ Persistência
→ Relatório

Essa base permite gerar valor rápido, desde que a viabilidade técnica de API Objects via Extensibility seja confirmada.
