# 06-BACKLOG_v0.1

## Backlog Inicial Priorizado do MVP

**Projeto:** Genexus Open API Builder
**Versão:** v1.1
**Base Primária:** 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md v1.1
**Dependência direta:** 05-ARQUITETURA_FUNCIONAL_MVP.md v1.1
**Objetivo:** converter requisitos e arquitetura em entregas incrementais rastreáveis.
**Idioma:** Português BR
**Público principal:** Agentes de IA + mantenedores humanos
**Data:** Abril/2026
**Última revisão:** Julho/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- transformar F04 + F05 em plano executável
- priorizar entregas reais
- seguir pipeline oficial
- reduzir risco inicial
- orientar execução assistida por IA

Este documento **não substitui requisitos**, **não congela roadmap**, **não define datas fixas**.

---

# 2. Taxonomia

| Código | Tipo | Significado |
|---|---|---|
| DP-F04 | Decisão oficial | Requisito aprovado no documento 04 |
| AF-F05 | Arquitetura Funcional | Implementação aprovada no documento 05 |
| BG-F06 | Backlog | Item planejado neste documento |
| HP-F06 | Hipótese | Depende validação prática |

---

# 3. Fontes e Rastreabilidade

## [F04]

04-REQUISITOS_MVP_Genexus_Open_API_Builder.md

## [F05]

05-ARQUITETURA_FUNCIONAL_MVP.md

---

# 4. Estratégia Oficial

Prioridade máxima:

1. validar viabilidade técnica oficial
2. gerar primeira API funcional
3. operar dentro da IDE
4. repetir sem erro
5. evitar exposição indevida
6. preparar evolução futura

[BG-F06]

---

# 5. Fases Oficiais (alinhadas ao F05)

| Fase | Base | Meta |
|---|---|---|
| 0 | Setup | Estrutura mínima e reproduzível |
| -1 | F05 | Pacote inicial de viabilidade do SDK |
| 1 | F04 8.1 | Seleção Transaction |
| 2 | F04 8.6 | Wizard mínimo com decisões obrigatórias |
| 3 | F04 8.5 | Criar contratos próprios da API |
| 4 | F04 8.2/F05 | Procedures e API Object |
| 5 | F04 8.3 | Organização e metadata |
| 6 | F04 8.2 | Serviços REST List/Get/Create/Update |
| 7 | F04 8.7 | Operação IDE |
| 8 | Segurança | Campos sensíveis, auditoria e Security Level |

[BG-F06]

---

# 6. Backlog Priorizado

As subseções abaixo preservam a numeração histórica dos pacotes. A ordem operacional vigente está na seção 9: primeiro a Fase 0, depois o pacote inicial da Fase -1.

## FASE -1 — Pacote Inicial de Viabilidade do SDK

Esta fase executa o primeiro pacote do spike técnico. Ela não concentra sozinha os dez gates transversais do MVP, que serão comprovados progressivamente até o fim da Sprint 7.

| ID | Item | Prioridade |
|---|---|---|
| B000 | Confirmar extensão carrega na IDE | Alta |
| B001 | Detectar KB ativa | Alta |
| B002 | Listar Transactions reais via API oficial disponível | Alta |
| B003 | Criar objeto simples de teste suportado pelo SDK | Alta |
| B004 | Validar criação, alteração, releitura e exclusão de API Object oficial | Altíssima |
| B005 | Validar criação, alteração, releitura e exclusão de Procedure, SDT, Folder e File | Altíssima |
| B006 | Validar persistência e releitura de metadata em File após reabrir KB | Altíssima |

### Gate

Se B004 falhar sem alternativa oficial viável:

> revisar ou encerrar a tese atual do produto.

---

## FASE 0 — Setup

| ID | Item | Prioridade |
|---|---|---|
| B010 | Localizar SDK e criar solution/projeto extensibility com build reproduzível | Alta |
| B011 | Estruturar pastas internas | Alta |
| B012 | Confirmar e aplicar as convenções de nomes já congeladas na documentação | Alta |

---

## FASE 1 — Seleção Transaction

| ID | Item | Prioridade |
|---|---|---|
| B020 | Detectar KB ativa | Alta |
| B021 | Listar Transactions elegíveis | Alta |
| B022 | Ler módulo da Transaction | Alta |
| B023 | Detectar objetos existentes | Média |
| B024 | Verificar se a Transaction pode operar como Business Component | Alta |
| B025 | Ler chave simples ou composta completa | Alta |

---

## FASE 2 — Wizard

| ID | Item | Prioridade |
|---|---|---|
| B030 | Passo 1 selecionar Transaction | Alta |
| B031 | Passo 2 selecionar serviços, campos e filtros essenciais | Alta |
| B032 | Passo 3 revisar segurança, paginação, ordenação, Services base path e RestPath | Alta |
| B033 | Validar campos obrigatórios | Alta |
| B034 | Cancelamento seguro | Média |
| B035 | Bloquear geração sem BC ou oferecer habilitação explícita | Alta |
| B036 | Exibir campos tecnicamente inadequados desabilitados com motivo | Alta |
| B037 | Configurar `Obrigatório no payload` para Create e Update | Alta |

---

## SPRINT 3 — Metadata + ApiPlan

| ID | Item | Prioridade |
|---|---|---|
| B038 | Montar `ApiPlan` inicial em memória, ainda não pronto para engine | Alta |

---

## FASE 3 — Criar SDTs

| ID | Item | Prioridade |
|---|---|---|
| B040 | Gerar `sdt<Nome>_API_CreateRequest` | Alta |
| B041 | Gerar `sdt<Nome>_API_UpdateRequest` | Alta |
| B042 | Gerar `sdt<Nome>_API_Response` | Alta |
| B043 | Gerar `sdt<Nome>_API_ListFilters` | Alta |
| B044 | Gerar `sdt<Nome>_API_ListResponse` com envelope | Alta |
| B045 | Gerar/reencontrar SDTs compartilhados em `GxOpenAPI` | Alta |
| B046 | Validar `sdt_API_ErrorResponse` e `sdt_API_Pagination` conforme documento 27 | Alta |
| B047 | Validar nomes `_API_` no YAML e em ao menos um gerador de cliente OpenAPI | Alta |

---

## FASE 4 — Procedures e API Object

| ID | Item | Prioridade |
|---|---|---|
| B050 | Gerar `proc<Nome>_API_List` | Alta |
| B051 | Gerar `proc<Nome>_API_Get` | Alta |
| B052 | Gerar `proc<Nome>_API_Create` | Alta |
| B053 | Gerar `proc<Nome>_API_Update` | Alta |
| B054 | Gerar API Object `api<Nome>` delegando para as Procedures | Alta |
| B055 | Validar uso via Business Component | Alta |
| B056 | Gerar `[Description]` por serviço, sem campo no wizard, com fallback de idioma registrado | Alta |

---

## FASE 5 — Organização

| ID | Item | Prioridade |
|---|---|---|
| B060 | Gravar metadata persistente em File | Alta |
| B061 | Aplicar mesmo módulo da Transaction | Alta |
| B062 | Aplicar nomenclatura padrão | Alta |
| B063 | Detectar colisões por metadata e por nome | Alta |
| B064 | Bloquear colisões incompatíveis sem criar `_v2` | Alta |
| B065 | Gravar Services base path, RestPath, campos, filtros, paginação, ordenação e Security Level na metadata | Alta |
| B066 | Diferenciar Folder específico criado de Folder reutilizado | Alta |
| B067 | Gravar descrições geradas e dados para detectar alteração manual posterior | Alta |

---

## FASE 6 — Serviços REST

| ID | Item | Prioridade |
|---|---|---|
| B070 | Gerar `List` com filtros, paginação e ordenação determinística | Alta |
| B071 | Gerar `Get` por chave simples ou composta | Alta |
| B072 | Gerar `Create` | Alta |
| B073 | Gerar `Update` com `PUT` e resposta 200 completa | Alta |
| B074 | Gerar paths e operationIds conforme convenção | Alta |
| B075 | Validar ausência de endpoint `Delete` no MVP | Alta |
| B076 | Distinguir filtro de `List` ausente de `false`, `0` e string vazia; recusar campo obrigatório não preenchido em `Create` e `Update` | Alta |
| B077 | Retornar paginação com `totalCount` e `totalPages` confiáveis | Alta |
| B078 | Validar `operationId` no padrão `apiNome.Serviço` | Alta |
| B079 | Validar códigos HTTP, corpos de resposta e `Location` opcional de `Create` | Alta |

### Nota operacional

`Delete` é pós-MVP como endpoint REST. A remoção de uma API gerada pertence ao ciclo de vida da ferramenta e depende da metadata persistente.

### Nota de revisão sobre `B076`

O enunciado original de `B076` era «Distinguir parâmetro ausente de `false`, `0` e string vazia», tratado como um problema único. A implementação mostrou que ele se divide em dois casos com desfechos diferentes.

**Filtros de `List`, na query string — resolvido conforme o enunciado original.** O SDT writer grava os membros nullable de `ListFilters` com a propriedade GeneXus `idJsonInclude=idJsonJsonNull`, correspondente a `Json Null Serialization = JSON null`. Sem ela, membro numérico não informado serializa como `0` e indicaria falsamente filtro aplicado. `B070`/`B077` validou o comportamento em runtime: sem filtro, `AppliedFilters.ContratoNumero=null`; com filtro, o valor informado.

**Membros obrigatórios no corpo de `Create` e `Update` — inviável como enunciado.** Revisto em 2026-08-03, no fechamento de `B071`-`B073`/`B079`, depois que quatro caminhos foram testados e descartados na IDE: comando `csharp` com `IsDirty`, que emite `spc0087` e foi recusado por decisão do projeto; `HttpRequest.ToString()` dentro da Procedure, onde o corpo bruto não chega; `&Sdt.IsDirty()` nativo, que não existe na linguagem; e `HttpRequest.ToString()` no evento `Before` do API Object, que devolveu `len=0` nos dois geradores porque o corpo já foi consumido pelo pipeline REST.

Conclusão registrada: o GeneXus não expõe presença de membro JSON no corpo de request sem comando `csharp`. A geração passou a validar preenchimento, comparando cada campo obrigatório com o valor default do mesmo membro em instância vazia do próprio SDT de request. `Create` e `Update` respondem 400 quando o obrigatório chega ausente ou com o valor default do tipo — vazio, `false` ou `0`.

Limitação assumida e documentada: campo obrigatório cujo valor legítimo seja igual ao default do tipo é recusado com 400. Os textos do wizard e as mensagens de Output de `B033` e `B037` foram corrigidos na mesma frente, porque ainda prometiam semântica de presença.

---

## FASE 7 — Operação IDE

| ID | Item | Prioridade |
|---|---|---|
| B080 | Integrar menu/contexto IDE | Alta |
| B081 | Exibir relatório final interno | Alta |
| B082 | Mostrar tempo execução | Média |
| B083 | Detectar conflito antes salvar | Alta |
| B084 | Bloquear overwrite silencioso | Alta |
| B085 | Sincronizar com a Transaction usando metadata | Alta |
| B086 | Remover API gerada por metadata, sem reverter BC | Média |
| B087 | Ancorar posse na metadata e liberar a `Description` do API Object | Alta |

### Nota operacional — B087, registrada em 2026-08-03

A `Description` do API Object acumula dois papéis: é copiada pelo gerador para `info.description` do contrato OpenAPI, portanto documentação pública, e é a sentinela de posse comparada por igualdade exata antes de qualquer reescrita.

Enquanto o texto era `Genexus Open API Builder B054 API Object - Transaction=... - Procedures=B050-B053`, a acumulação se protegia pela própria feiura: ninguém tentaria melhorar aquela string. Ao retirar o jargão interno do contrato público, a frente registrada em `Docs/Implementation/2026-08-03-CONTRATO-OPENAPI-GAPS.md` trocou o texto por uma frase de documentação legível — e, com isso, aumentou a chance de um usuário querer traduzi-la, encurtá-la ou personalizá-la. Qualquer edição faz a API deixar de ser reconhecida como própria e bloqueia a regeração.

B087 separa os dois papéis: a posse passa a ser verificada apenas pela metadata de integridade B067, e a `Description` fica livre para edição humana. O item é anterior à Alpha, porque a Alpha expõe a ferramenta a usuários que não conhecem essa armadilha.

---

## FASE 8 — Segurança

| ID | Item | Prioridade |
|---|---|---|
| B090 | Classificar sensíveis por configuração explícita | Alta |
| B091 | Classificar auditoria separadamente | Alta |
| B092 | Configurar Security Level e GAM/None quando aplicável | Alta |
| B093 | Aplicar Security Level explicitamente em todos os serviços | Alta |

---

# 7. Critérios de Aceite por Itens-Chave

| ID | Aceite |
|---|---|
| B010 | SDK identificado por versão e origem; dependências localizáveis sem caminho absoluto da máquina; `Src/GenexusOpenApiBuilder.sln` e `Src/Extension/GenexusOpenApiBuilder.Extension.csproj` criados; comando e evidência registrados em `Docs/Implementation/B010-SDK-E-BUILD-MINIMO.md` |
| B011 | Estrutura interna confirmada conforme o layout do documento 05, seção 5.7 |
| B012 | Convenções congeladas confirmadas e aplicadas à estrutura inicial |
| B004 | Existe evidência prática de criação, alteração, releitura e exclusão de API Object oficial |
| B005 | Existe evidência prática de criação, alteração, releitura e exclusão de Procedure, SDT, Folder e File |
| B006 | Metadata em File sobrevive ao fechamento e reabertura da KB |
| B060 | Cliente grava metadata de geração persistente |
| B040 | Cliente gera `sdtCliente_API_CreateRequest` |
| B041 | Cliente gera `sdtCliente_API_UpdateRequest` |
| B042 | Cliente gera `sdtCliente_API_Response` |
| B070 | Existe `List` funcional |
| B071 | Existe `Get` funcional para chave simples e composta |
| B072 | Existe `Create` funcional |
| B073 | Existe `Update` funcional com HTTP 200 e Response completo |
| B075 | Não existe endpoint `Delete` no MVP |
| B076 | Filtros de `List` distinguem ausência de valores válidos `false`, `0` e string vazia; `Create` e `Update` respondem 400 quando campo obrigatório chega ausente ou com o valor default do tipo, conforme a nota de revisão da Fase 6 |
| B077 | ListResponse retorna `items`, `pagination` e `appliedFilters` |
| B078 | OperationIds seguem `apiCliente.List`, `apiCliente.Get`, `apiCliente.Create` e `apiCliente.Update` |
| B079 | Códigos HTTP e corpos respeitam o contrato; `Location` é emitido em `Create` quando o runtime permitir controle seguro |
| B080 | Menu/contexto acessível dentro IDE |
| B081 | Relatório lista criados/atualizados |

## 7.1 Rastreabilidade dos Gates Técnicos Transversais

| Gate | Evidência principal no backlog |
|---|---|
| 1. Carregamento no GeneXus 18 U14 ou posterior (U15 como validação inicial) | B000 |
| 2. Ciclo de vida dos objetos nativos pelo SDK | B003–B005 |
| 3. Delegação, propriedades e segurança do API Object | B004, B054, B056, B065, B074, B092 e B093 |
| 4. Contrato refletido no YAML gerado | B047, B054 e B070–B079 |
| 5. Create/Update via BC com chaves simples e compostas | B025, B052, B053, B055 e B071–B073 |
| 6. Filtro ausente distinto de vazio, `false` e zero; obrigatório não preenchido recusado com 400 | B037, B070 e B076 |
| 7. Códigos HTTP, corpos e `Location` | B046, B052, B053, B072, B073 e B079 |
| 8. List com filtros, períodos, paginação, totais e ordem determinística | B031, B043, B044, B050, B070 e B077 |
| 9. Metadata persistente e reconhecimento seguro | B006, B060, B063, B065–B067, B085 e B086 |
| 10. Colisão, regeneração e remoção conservadoras | B063, B064 e B083–B086 |

Esses gates são comprovados progressivamente. Todos devem estar aprovados antes do marco **wizard funcional do MVP concluído**, ao fim da Sprint 7, e antes da Alpha.

[BG-F06]

---

# 8. MVP Real (linha de corte)

Os itens e intervalos abaixo formam a linha de corte exaustiva do MVP. Um item omitido desta lista não é necessário para declarar o MVP concluído; qualquer mudança nessa interpretação exige atualizar esta seção e a matriz de gates em conjunto.

- Fase 0: B010–B012
- Fase -1: B000–B006
- Fase 1: B020–B025
- Fase 2: B030–B037
- Fase 3: B040–B047
- Fase 4: B050–B056
- Fase 5: B060–B067
- Fase 6: B070–B079
- Fase 7: B080, B081 e B083–B087
- Fase 8: B090–B093

`B082` fica fora da linha de corte: mostrar o tempo de execução é útil, mas não comprova contrato funcional, segurança nem ciclo de vida.

[BG-F06]

---

# 9. Ordem Operacional por Dependência

1. Fase 0 completa (`B010`–`B012`)
2. Pacote inicial da Fase -1 completo (`B000`–`B006`)
3. Fases 1 e 2 (`B020`–`B037`) no protótipo navegável e não persistente
4. Planejamento de segurança (`B090`–`B092`) dentro do `ApiPlan`
5. Fase 3 até `B046`, criando os SDTs antes de seus consumidores
6. Fases 4 e 5 (`B050`–`B067`), criando Procedures, API Object e metadata
7. `B047`, Fase 6 (`B070`–`B079`) e aplicação da segurança em `B093`
8. Fase 7 (`B080`, `B081`, `B083`–`B086`) e comprovação integrada dos dez gates

`B047` é validado somente depois do API Object e dos serviços porque depende do YAML gerado pelo GeneXus; esse deslocamento de evidência não antecipa consumidores antes dos SDTs.

[BG-F06]

---

# 10. Fora do MVP

- IA generativa
- GraphQL
- OpenAPI avançado
- OAuth avançado
- analytics
- marketplace
- suporte Java
- múltiplos templates
- endpoint REST `Delete`
- reuso arbitrário de SDTs externos
- versionamento automático por `_v2`

[DP-F04]

---

# 11. Dependências Técnicas

| Item | Depende de |
|---|---|
| Fase 0 | Consolidação documental concluída |
| Fase -1 | Fase 0 concluída |
| Fases 1–8 | Pacote inicial do spike (`B000`–`B006`) aprovado |
| Wizard | Seleção Transaction |
| Criar SDT | Wizard |
| Procedures/API Object | Criar SDT |
| Organização/metadata | Procedures/API Object |
| Serviços REST | Organização/metadata |
| Operação IDE | Serviços iniciais |
| Segurança | Serviços iniciais |

[AF-F05]

---

# 12. Definição de Pronto

Todo item concluído deve:

- funcionar no fluxo real
- ser testável manualmente
- não quebrar fase anterior
- possuir commit rastreável
- atender critério explícito quando existir

[BG-F06]

---

# 13. Critérios de Parada

Parar e revisar se ocorrer:

- impossibilidade oficial de API Object
- corrupção de KB
- falhas imprevisíveis recorrentes
- dependência externa anti-tese
- arquitetura excessivamente complexa

[HP-F06]

---

# 14. Riscos Iniciais

| Risco | Mitigação |
|---|---|
| SDK limitado | spike técnico cedo |
| geração quebrar KB | ambiente teste |
| escopo inflar | seguir linha MVP |
| UX ruim | testar cedo |
| naming ruim | congelar no momento certo |

[HP-F06]

---

# 15. Uso Correto por Agentes de IA

## Pode assumir

- backlog segue ordem do F05
- a Fase 0 precede o pacote inicial do spike; os dez gates técnicos são comprovados progressivamente até o fim da Sprint 7
- itens Alta entram primeiro
- segurança mínima já está no MVP

## Deve tratar com cautela

- backlog muda após descoberta real do SDK
- itens podem virar subtarefas
- ordem pode ajustar por bloqueio técnico

---

# 16. Grau de Confiança

| Área | Grau | Evidência |
|---|---|---|
| Ordem geral execução | Alto | [F04][F05] |
| MVP definido corretamente | Alto | [F04] |
| Dependências técnicas | Alto | [AF-F05] |
| Estimativa futura esforço | Baixo | [HP-F06] |

---

# 17. Conclusão Objetiva

O backlog v1.1 prioriza:

Spike técnico → Transaction → Wizard → SDTs próprios → Procedures/API Object → metadata → List/Get → Create/Update → IDE → Segurança.

Tudo além disso fica para versões futuras.
