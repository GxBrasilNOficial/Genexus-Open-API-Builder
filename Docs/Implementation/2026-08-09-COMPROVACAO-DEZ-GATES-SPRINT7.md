# Comprovação integrada dos dez gates — Sprint 7

## Estado

Concluído em 2026-08-09 por consolidação documental. Os dez gates técnicos transversais do MVP foram comprovados progressivamente nas Sprints 1–7 no GeneXus 18 Upgrade 15; esta frente não reexecutou bateria U15 nova e não altera código da extensão.

Marco declarado: **wizard funcional do MVP concluído**.

## Objetivo

Empacotar, gate a gate, a evidência já existente (Implementation, testes mecânicos e validação U15) e registrar gaps residuais conhecidos, sem inventar aprovação U14 nem absorver frentes pré-Alpha.

## Decisões de fechamento (mantenedor, 2026-08-09)

- Carregamento real no Upgrade 14 permanece **não validado nesta máquina** e **não bloqueia** o marco; quem usar U14 assume o risco até haver evidência própria.
- Antes da Alpha (Sprint 8), investigar em frentes separadas e **uma de cada vez**: (1) limitações do YAML OpenAPI gerado nativamente pelo GeneXus; (2) evidência HTTP 403 com papel GAM não-administrador.
- Confiança no acervo U15 já registrado: não houve revalidação integrada só para carimbar este pacote.

## Critério aplicado

Fonte canônica da matriz: `Docs/Foundation/06-BACKLOG_v0.1.md` §7.1. Listas equivalentes: documentos 09, 15, 24 e registro de decisões do MVP.

Se algum gate falhasse sem alternativa nativa segura, o marco **não** seria declarado. Nenhum gap bloqueante desse tipo foi encontrado no fechamento.

## Matriz integrada

| Gate | Status | Evidências principais | Remissões de backlog |
|---|---|---|---|
| 1. Carregamento no GeneXus 18 U14+ (U15 como validação inicial) | **Aprovado no U15**; U14 = residual documentado | `B000-CARREGAMENTO-IDE.md`; checkpoint STATUS (U15 marcado/carregado; U14 pendente e não bloqueante) | B000 |
| 2. Ciclo de vida dos objetos nativos pelo SDK | **Aprovado** | `B003-CRIACAO-FOLDER-TESTE.md`; `B004-CICLO-DE-VIDA-API-OBJECT.md`; `B005-CICLO-DE-VIDA-PROCEDURE-SDT-FOLDER-FILE.md` | B003–B005 |
| 3. Delegação, propriedades e segurança do API Object | **Aprovado** (403 granular fora deste marco) | B004; B054 no STATUS; `B056-DESCRICOES-SERVICO-APIPLAN.md`; B065 em STATUS/Sprint 5; `B092-CONDICAO-GAM-APIPLAN.md`; `B093-SECURITY-LEVEL-APIPLAN-OBJETO.md`; Authorization 401/200 em fechamento Sprint 6 | B004, B054, B056, B065, B074, B092, B093 |
| 4. Contrato refletido no YAML gerado | **Aprovado com ressalva** | `2026-08-04-VALIDACAO-YAML-SPRINT6-EIXOS-SEGURANCA.md`; `2026-08-03-CONTRATO-OPENAPI-GAPS.md`; emenda 2026-08-04 no registro de decisões; testes `Tests/OpenApiContract/` | B047, B054, B070–B079 |
| 5. Create/Update via BC com chaves simples e compostas | **Aprovado**; gap residual de reexecução | `B025-LEITURA-CHAVE-PRIMARIA-PROTOTIPO.md`; `B055-BUSINESS-COMPONENT-PROCEDURES.md`; `B071-B073-B079-GET-CREATE-UPDATE-HTTP.md` (HTTP dois geradores) | B025, B052–B055, B071–B073 |
| 6. Filtro ausente ≠ vazio/false/0; obrigatório não preenchido → 400 | **Aprovado** (dois casos reconciliados) | `B070-B077-LIST-FILTROS-PAGINACAO.md`; reconciliação B076 no STATUS/06/09/15/24 e emenda 2026-08-03 no registro de decisões; bateria HTTP de preenchimento em B071 | B037, B070, B076 |
| 7. Códigos HTTP, corpos e Location | **Aprovado** | `B071-B073-B079-GET-CREATE-UPDATE-HTTP.md`; matriz Location 2026-08-06 e pós-correção; Create 201, Get/Update 200/404, validação 400 | B046, B052–B053, B072–B073, B079 |
| 8. List com filtros, períodos, paginação, totais e ordem | **Aprovado** | `B070-B077-LIST-FILTROS-PAGINACAO.md`; HTTP List filtrado/paginado no U15 (dois environments) | B031, B043–B044, B050, B070, B077 |
| 9. Metadata persistente e reconhecimento seguro | **Aprovado** | `B006-PERSISTENCIA-METADATA-FILE.md`; `B060-METADATA-FILE-APIPLAN.md`; B063–B067; `B085-SINCRONIZAR-COM-TRANSACTION.md`; `B086-REMOVER-API-GERADA.md`; `B087-POSSE-API-OBJECT-METADATA.md` (posse do API Object pela metadata, sem travar `Description`) | B006, B060, B063, B065–B067, B085–B087 |
| 10. Colisão, regeneração e remoção conservadoras | **Aprovado** | `B063-B064-COLISOES-CONSERVADORAS.md`; `B083-UX-CONFLITOS-COLISAO.md`; B085; B086; `2026-08-08-FOLDER-REUTILIZADO-COM-AVISO.md`; `B081-RELATORIO-FINAL-POS-APLICACAO.md` | B063–B064, B083–B086 |

## Ressalvas e gaps residuais (não bloqueiam o marco)

1. **Upgrade 14 nesta máquina:** não testado; baseline de produto continua U14+, evidência prática desta instalação é U15.
2. **YAML nativo incompleto:** respostas declaradas no OpenAPI gerado pelo GeneXus ficam restritas a 200/404 no template nativo; lista `required:` de schemas pode não ser emitida. Runtime HTTP está correto. Frente pré-Alpha `B088` (fora deste marco); **fechada em 2026-08-10** — ver `Docs/Implementation/2026-08-10-B088-LIMITACOES-YAML-NATIVO.md`.
3. **HTTP 403 com papel GAM comum:** Authorization já comprovado (401 sem token / 200 com token). Frente pré-Alpha `B089` (fora deste marco); **fechada em 2026-08-10** com Get 200 / Create 403 — ver `Docs/Implementation/B093-SECURITY-LEVEL-APIPLAN-OBJETO.md` §4.A.3.D.
4. **Atomicidade / rollback multiobjeto:** preflight valida o trio afetado antes do primeiro `Save()` planejado; falha interna da IDE/SDK no meio de uma sequência pode exigir reparação manual. Já listado no STATUS.
5. **Reexecução CreateRequired em Procedure Create já própria:** preflight pode bloquear em vez de migrar; contorno atual é recriar a API. Gap residual conhecido, fora do escopo desta sessão.

## O que esta frente não cobre

- implementação de feature nova;
- Alpha pública (Sprint 8);
- as duas investigações pré-Alpha acima (tratadas depois deste fechamento; ambas concluídas em 2026-08-10);
- reinstalação da DLL (sem mudança de código nesta frente).

## Critério de conclusão

- matriz dos dez gates preenchida com status e evidências;
- nenhum gap bloqueante sem alternativa nativa segura;
- marco **wizard funcional do MVP concluído** declarado no checkpoint e no plano por sprints;
- Sprint 7 encerrada; na data do fechamento, a próxima ação era a primeira frente pré-Alpha (limitações do YAML nativo / `B088`), depois a evidência 403 GAM (`B089`), antes da Alpha. Em 2026-08-10 `B088` e `B089` foram concluídos e o pacote documental da Alpha `0.1.0-alpha.1` foi preparado; em 2026-08-11 a documentação pública foi alinhada ao `B094` e a Alpha foi publicada. A próxima ação vigente fica no checkpoint (gate da Sprint 8 com usuário externo).

## Remissões

- Checkpoint: `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`
- Matriz: `Docs/Foundation/06-BACKLOG_v0.1.md` §7.1
- Listas: `Docs/Foundation/09-INTEGRACAO_GeneXus_Extensibility_SDK.md` §17; `Docs/Foundation/15-TESTES_VALIDACAO_E_QUALIDADE.md` §10.1; `Docs/Foundation/24-PLANO_IMPLEMENTACAO_REAL_POR_SPRINTS.md` (Sprint 7 item 7)
- Decisões: `Docs/Decisions/2026-07-14-REGISTRO_DECISOES_FUNCIONAIS_MVP.md` (gates transversais + emendas 2026-08-03/04)
