# 24-PLANO_IMPLEMENTACAO_REAL_POR_SPRINTS.md

## Plano Oficial de Execução Prática do Projeto em Sprints Reais

**Projeto:** Genexus Open API Builder
**Versão:** v1.0
**Base Primária:** 23-RISCOS_LIMITACOES_E_NAO_OBJETIVOS.md v1
**Dependência direta:** 10-ENGINE_GERACAO_OBJETOS.md v1.0
**Relacionamento adicional:** 01 a 23 e contratos 26 a 28 consolidados
**Objetivo:** converter toda a documentação consolidada em um plano realista de implementação incremental, validável e executável.
**Idioma:** Português BR
**Público principal:** maintainer principal + contribuidores técnicos + agentes de IA
**Data:** Abril/2026
**Última revisão:** Julho/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- transformar teoria em execução
- reduzir paralisia por excesso de planejamento
- organizar prioridades reais
- criar entregas incrementais
- acelerar primeiro release utilizável

Este documento **não exige metodologia rígida**, **não congela datas**, **não impede adaptação prática**.

As sprints que implementam `List`, contratos HTTP/erros e ciclo de vida devem seguir, respectivamente, `26-CONTRATO_FILTROS_PAGINACAO_ORDENACAO.md`, `27-CONTRATO_HTTP_ERROS_E_SDTS_COMPARTILHADOS.md` e `28-METADATA_REGENERACAO_SINCRONIZACAO_E_REMOCAO.md`.

---

# 2. Taxonomia

| Código | Tipo | Significado |
|------|------|-------------|
| MVP-F04 | Escopo base | Produto inicial |
| ENG-F10 | Engine | Núcleo técnico |
| OPS-F24 | Operação prática | Definição deste documento |
| SPR-F24 | Sprint | Ciclo curto |
| HP-F24 | Hipótese | Ajustável durante execução |

---

# 3. Fontes e Rastreabilidade

| Código | Fonte |
|------|-------|
| F04 | REQUISITOS_MVP |
| F07 | UX_WIZARD |
| F09 | INTEGRACAO_SDK |
| F10 | ENGINE_GERACAO |
| F15 | TESTES_QUALIDADE |
| F23 | RISCOS_LIMITACOES |

---

# 4. Estratégia Oficial

Executar em ciclos curtos:

1. construir base mínima
2. validar rápido
3. corrigir cedo
4. expandir com controle
5. publicar incrementalmente

[OPS-F24]

---

# 5. Regra Principal

Versão simples funcionando vale mais que arquitetura perfeita parada.

[OPS-F24]

---

# 6. Sprint 0 — Preparação

## Objetivo

Executar a Fase 0 do backlog (`B010`–`B012`) e deixar o terreno técnico reproduzível.

## Entregas

- `B010`: versão e origem do SDK registradas
- `B010`: dependências localizáveis sem caminho absoluto específico da máquina
- `B010`: `Src/GenexusOpenApiBuilder.sln` e `Src/Extension/GenexusOpenApiBuilder.Extension.csproj` criados conforme o layout do documento 05
- `B010`: comando e evidência de build mínimo registrados em `Docs/Implementation/B010-SDK-E-BUILD-MINIMO.md`
- `B011`: estrutura interna confirmada conforme o documento 05, seção 5.7
- `B012`: convenções de nomes congeladas confirmadas e aplicadas

## Saída esperada

Solution mínima reproduzível, construída pelo mecanismo oficial disponível a partir do GeneXus 18 U14 e usada no spike. O build usa feed NuGet e MSBuild SDKs oficiais registrados por `B010`; o `B000` posterior validou carregamento e compatibilidade prática inicial no U15 local. A validação do limite inferior no U14 continua dependendo de colegas da comunidade, sem data definida e sem bloquear o MVP.

[SPR-F24]

---

# 7. Sprint 1 — Spike SDK Real

## Objetivo

Executar o pacote inicial de viabilidade da Fase -1 (`B000`–`B006`).

## Entregas

- `B000` (concluído): extensão mínima carregou na IDE U15
- `B001` (concluído): KB ativa detectada no U15, em modo somente leitura
- `B002` (concluído): 10 Transactions reais listadas no U15 por API oficial, em modo somente leitura
- `B003` (concluído): Folder de teste criado no U15 com autorização explícita e sem alterar objetos existentes
- `B004` (concluído): ciclo de vida de API Object oficial validado no U15
- `B005` (concluído): ciclo de vida de Procedure, SDT, Folder e File validado no U15
- `B006` (concluído): metadata JSON em File preservou identidade, descrição e bytes após fechar e reabrir a KB

## Gate

Gate aprovado no U15: o pacote inicial comprovou carregamento, leitura e ciclo de vida dos objetos necessários, incluindo persistência de metadata em File. A validação do limite inferior U14 continua pendente e não bloqueia o MVP.

[F09][SPR-F24]

---

## Gates técnicos transversais do MVP

Os gates abaixo são comprovados progressivamente nas Sprints 1–7. A Sprint 1 inicia essa comprovação com `B000`–`B006`; ela não precisa concluir antecipadamente capacidades que dependem do engine e dos contratos posteriores:

1. extensão carregou no GeneXus 18 U15; a confirmação do limite inferior U14 permanece pendente
2. SDK cria, salva, reabre, altera e exclui objetos nativos `API`, `Procedure`, `SDT`, `Folder` e `File`
3. objeto `API` delega às Procedures e persiste `RestMethod`, `RestPath`, `Description` e `SecurityLevel`
4. YAML gerado pelo GeneXus reflete rotas, métodos, parâmetros, SDTs e nomes `_API_`
5. `Create` e `Update` via BC funcionam com chave simples e composta, preservando regras e mensagens
6. ausência JSON é distinguida de vazio, `false` e zero sem membros públicos `Specified`
7. implementação controla códigos HTTP, corpo e `Location`, respeitando seu caráter opcional
8. `List` funciona com filtros opcionais, períodos, paginação, totalização e ordenação determinística
9. metadata em `File` sobrevive a fechar/reabrir a KB e reconhece objetos próprios
10. colisão, regeneração e remoção não sobrescrevem nem apagam objetos alheios

Se qualquer gate falhar sem alternativa nativa segura, revisar o desenho antes de declarar concluído o wizard funcional do MVP.

Não bloqueiam o MVP: associação visual sob a Transaction, objeto `Documentation` como fonte de metadata, uniformidade de erros interceptados antes da Procedure, migração assistida após renomear/mover Transaction, GeneXus Next, base `api/v1` e otimizações de build.

---

# 8. Sprint 2 — Protótipo Navegável do Wizard

## Objetivo

Validar navegação, captura de decisões e cancelamento seguro sem persistir nem gerar objetos.

## Entregas

- `B020`–`B025`: detectar KB, listar e selecionar uma Transaction, ler módulo, objetos existentes, BC e chave completa em modo somente leitura
- `B020`–`B025` (concluídos): KB ativa, Transactions elegíveis, módulo, objetos planejados, Business Component e chave primária completa verificados no U15 sem persistência e sem escrita pela extensão
- `B030` (concluído): Passo 1 do wizard selecionou `Transaction` pelo menu principal e pelo contexto no U15, mantendo estado apenas em memória
- `B031` (concluído): Passo 2 do wizard configurou serviços, campos e filtros essenciais no U15, mantendo decisões apenas em memória
- `B032` (concluído): Passo 3 do wizard revisou paths, segurança, paginação e ordenação no U15, acionado pelo contexto da `Transaction` e chamando B031 automaticamente quando necessário
- `B033` (concluído): campos obrigatórios foram incorporados ao wizard único aberto por B030 e validados manualmente no U15 sem persistência e sem escrita pela extensão
- `B034` (concluído): cancelamento seguro do wizard único foi validado manualmente no U15, descartando estado em memória sem `ApiPlan`, persistência ou escrita na KB
- `B035` (concluído): Business Component foi verificado no wizard único, com avanço bloqueado sem BC e habilitação persistente somente após confirmação explícita no U15
- `B036` (concluído): campos tecnicamente inadequados foram exibidos desabilitados, com motivo, contagens na Output e seleção impedida no wizard único no U15
- `B037` (concluído): obrigatoriedade técnica no payload foi consolidada para `CreateRequest` e `UpdateRequest` no wizard único no U15
- manter as escolhas apenas em memória
- avançar, voltar e cancelar sem alterar a KB, exceto pela habilitação explícita de `Business Component` em B035
- exibir resumo não persistente das escolhas
- não criar `ApiPlan` definitivo
- não chamar engine nem gerar objetos reais

## Gate

Fases 1 e 2 do backlog cobertas e validadas no protótipo navegável, com escolhas em memória, sem criação de `ApiPlan` e sem geração de objetos de API. A habilitação de `Business Component` é o único efeito persistente admitido nesta sprint e exige confirmação explícita do usuário.

[F07][SPR-F24]

---

# 9. Sprint 3 — Metadata + ApiPlan

## Objetivo

Transformar a Transaction e as escolhas do wizard em um `ApiPlan` completo, ainda sem gerar objetos.

## Entregas

- `B038` (concluído): wizard único montou `ApiPlan` interno em memória no U15, cobrindo contrato, paths, segurança, paginação, ordenação, nomes planejados, required por request e precondição de `Business Component`, sem persistir metadata e sem gerar objetos na KB
- ler atributos
- identificar chave simples ou composta completa
- `B090`: classificar campos sensíveis por configuração explícita
- `B091`: classificar auditoria separadamente
- `B092`: registrar no plano o `Security Level` e GAM/None quando aplicável
- módulo alvo
- montar decisões de filtros, payload, paginação, ordenação e segurança
- montar `ApiPlan`

## Gate

`ApiPlan` consistente, completo e sem escrita na KB.

[F08][SPR-F24]

---

# 10. Sprint 4 — Engine Base e SDTs

## Objetivo

Realizar a primeira integração efetiva wizard → `ApiPlan` → engine, criando primeiro os contratos SDT dos quais Procedures e serviços dependerão.

## Entregas

- receber o `ApiPlan` produzido a partir das decisões do wizard e entregá-lo ao engine
- `B040`: criar `sdtCliente_API_CreateRequest`
- `B041`: criar `sdtCliente_API_UpdateRequest`
- `B042`: criar `sdtCliente_API_Response`
- `B043`: criar `sdtCliente_API_ListFilters`
- `B044`: criar `sdtCliente_API_ListResponse`
- `B045`: criar ou reencontrar os SDTs compartilhados em `GxOpenAPI`
- `B046`: validar `sdt_API_ErrorResponse` e `sdt_API_Pagination`
- registrar logs da primeira escrita real na KB

## Gate

SDTs próprios e compartilhados criados pelo engine a partir do `ApiPlan`, sem criar ainda Procedures nem API Object.

[F10][F13][SPR-F24]

---

# 11. Sprint 5 — Procedures, API Object e Metadata

## Objetivo

Criar as Procedures e o API Object sobre os SDTs já existentes, organizando e registrando todos os objetos por metadata.

## Entregas

- `B050`–`B053`: criar as Procedures de List, Get, Create e Update
- `B054`: criar `apiCliente` delegando para as Procedures
- `B055`: validar o uso via Business Component
- `B056`: gerar `[Description]` para os serviços selecionados
- `B060`: gravar o File JSON de metadata
- `B061`: manter os objetos no módulo da Transaction
- `B062`: aplicar as convenções de nomes congeladas
- `B063`: detectar colisões por metadata e por nome
- `B064`: bloquear colisões incompatíveis sem criar `_v2`
- `B065`: persistir paths, campos, filtros, paginação, ordenação e segurança na metadata
- `B066`: distinguir Folder criado de Folder reutilizado
- `B067`: registrar descrições geradas para detectar alteração manual posterior
- preparar operationIds no padrão `apiNome.Serviço`
- não completar ainda o comportamento REST, reservado à Sprint 6

## Gate

API Object, Procedures e metadata criados e reencontráveis, sem duplicar os SDTs já produzidos na Sprint 4.

[F10][F12][F28][SPR-F24]

---

# 12. Sprint 6 — Serviços REST e Segurança

## Objetivo

Completar o comportamento REST sobre os objetos já criados e aplicar explicitamente a segurança planejada.

## Entregas

- `B070`: completar List com filtros, paginação e ordenação determinística
- `B071`: completar Get para chave simples ou composta
- `B072`: completar Create com HTTP 201 e `Location` quando controlável com segurança
- `B073`: completar Update com PUT, HTTP 200 e Response completo
- `B074`: aplicar paths e operationIds convencionados
- `B075`: comprovar ausência de endpoint Delete no MVP
- `B076`: distinguir ausência JSON de vazio, `false` e zero sem campos públicos `Specified`
- `B077`: comprovar `totalCount`, `totalPages` e `appliedFilters`
- `B078`: validar operationIds no padrão `apiNome.Serviço`
- `B079`: validar códigos HTTP, corpos e `Location`
- `B093`: aplicar o `Security Level` explicitamente em todos os serviços
- `B047`: validar no YAML gerado rotas, métodos, SDTs, segurança e nomes `_API_`

## Gate

List, Get, Create e Update funcionais, seguros e refletidos corretamente no YAML gerado pelo GeneXus.

[F12][F26][F27][SPR-F24]

---

# 13. Sprint 7 — Conflitos e Reexecução

## Objetivo

Fechar a operação na IDE, o ciclo de vida conservador e a comprovação integrada dos dez gates.

## Entregas

- `B080`: integrar menu/contexto na IDE
- `B081`: exibir relatório final interno
- `B083`: detectar conflito antes de salvar
- `B084`: bloquear overwrite silencioso e `_v2`
- `B085`: sincronizar com a Transaction por comparação explícita de metadata
- `B086`: remover por comando explícito, preservando Folder reutilizado e `GxOpenAPI`
- comprovar rerun consistente e cancelamento sem efeitos colaterais
- executar a validação integrada final, inclusive dos gates de segurança B092/B093

## Gate

Sem overwrite indevido e com os dez gates técnicos transversais comprovados.

Ao concluir esta sprint, o projeto atinge o marco **wizard funcional do MVP concluído**. Esse marco é pré-condição para iniciar a Alpha da Sprint 8.

[F14][F28][SPR-F24]

---

# 13.1 KBs de Teste

A validação prática deve começar por uma KB menor, fora de produção, com backup disponível.

Depois, deve avançar para uma cópia de teste atualizada da KB principal.

Não executar validação diretamente na KB principal de produção.

---

# 14. Sprint 8 — Release Alpha Público

## Objetivo

Primeira versão aberta utilizável.

## Entregas

- README forte
- install guide
- changelog
- release tag
- demo curta

## Gate

Usuário externo testa.

[F18][SPR-F24]

---

# 15. Sprint 9 — Correções Reais

## Objetivo

Aprender com uso externo.

## Entregas

- bugs prioritários corrigidos
- docs melhores
- onboarding melhorado
- UX refinada

## Gate

Adoção melhora.

[SPR-F24]

---

# 16. Sprint 10 — Beta Estável

## Objetivo

Produto confiável inicial.

## Entregas

- regressões reduzidas
- fluxo principal sólido
- comunidade ativa mínima
- releases previsíveis

## Gate

Caminho para v1.

[SPR-F24]

---

# 17. Ritmo Recomendado

| Tipo de Sprint | Duração |
|------|---------|
| pessoal intenso | 1 semana |
| realista paralelo | 2 semanas |
| voluntário comunitário | 3 semanas |

[HP-F24]

---

# 18. O Que Não Fazer Durante Execução

Evitar:

- refatorar cedo demais
- feature creep
- sprint gigante
- reescrever sem motivo
- ignorar feedback real

[OPS-F24]

---

# 19. Uso Correto por Agentes de IA

## Pode assumir

- entrega incremental vence perfeccionismo
- gates evitam desperdício
- feedback externo acelera maturidade

## Deve tratar com cautela

- datas rígidas
- excesso de escopo
- dependências não validadas

---

# 20. Conclusão Objetiva

Projeto cresce quando planejamento vira sprint.

E sprint vira software funcionando.
