# Status Atual e Próximo Passo

## Autoridade deste checkpoint

Este documento é a fonte canônica apenas para o estado operacional do projeto e para a próxima ação executável.

Ele não define requisitos funcionais nem contratos técnicos. Para essas decisões, prevalecem o [registro de decisões funcionais do MVP](Decisions/2026-07-14-REGISTRO_DECISOES_FUNCIONAIS_MVP.md) e os contratos ativos em [Foundation](Foundation/00-MASTER_INDEX_DO_PROJETO.md).

## Última atualização

2026-07-15.

## Último marco concluído

- entrevista funcional do MVP consolidada;
- documentos Foundation alinhados e auditados;
- base documental publicada na branch `main`;
- `B010` concluído: SDK localizado, solution e projeto mínimo criados e build reproduzível validado.
- `B011` concluído: estrutura interna confirmada sem introduzir projetos ou camadas vazias.

## Frente ativa

**Sprint 0 — Preparação**, correspondente à **Fase 0 — Setup** do backlog.

## Próxima ação única

Executar `B012`:

> Confirmar e aplicar as convenções de nomes já congeladas na documentação, sem antecipar objetos do wizard ou alterações de KB.

## Critério de conclusão e evidência esperada

- convenções aplicáveis ao código e à extensão identificadas nas fontes governantes;
- nomes da solution, projeto, namespace e artefatos mínimos confrontados com essas convenções;
- decisões e eventuais lacunas registradas na documentação de implementação;
- nenhum wizard funcional nem alteração de KB nesta ação.

A compatibilidade das APIs e o carregamento da extensão na IDE permanecem no pacote de spike `B000`–`B006`.

## Sequência operacional vigente

1. Sprint 0 executa a Fase 0 (`B010`–`B012`) e prepara o terreno.
2. Sprint 1 executa o pacote inicial de viabilidade da Fase -1 (`B000`–`B006`).
3. As Fases 1–8 dependem da aprovação desse spike.
4. Sprint 2 entrega apenas o protótipo navegável e não persistente do wizard.
5. Sprint 3 cria metadata e `ApiPlan`.
6. Sprint 4 integra o wizard ao engine pela primeira vez e cria os SDTs.
7. Sprints 5–7 completam Procedures/API/metadata, serviços REST/segurança e o ciclo conservador de conflitos, regeneração e remoção.
8. O marco **wizard funcional do MVP concluído** ocorre ao final da Sprint 7, antes da Alpha.

## Bloqueios e fatos ainda não validados

- compatibilidade prática das APIs do SDK com GeneXus 18 Upgrade 15;
- comprovação progressiva dos gates técnicos transversais definidos nos documentos 09, 15 e 24.

## Documentos governantes

- [06 — Backlog](Foundation/06-BACKLOG_v0.1.md)
- [09 — Integração com o SDK](Foundation/09-INTEGRACAO_GeneXus_Extensibility_SDK.md)
- [15 — Testes e qualidade](Foundation/15-TESTES_VALIDACAO_E_QUALIDADE.md)
- [24 — Plano por sprints](Foundation/24-PLANO_IMPLEMENTACAO_REAL_POR_SPRINTS.md)

## Marcos ainda não iniciados

- spike real na IDE;
- protótipo navegável do wizard;
- `ApiPlan`;
- engine de geração;
- Alpha público.

## Protocolo de atualização

Toda mudança de marco, frente ativa ou próxima ação deve atualizar este checkpoint no mesmo commit que produz a mudança. O checkpoint deve manter uma única próxima ação e apontar para os contratos, sem duplicá-los.

Ao concluir `B010`, a próxima atualização deste arquivo deve promover `B011`; `B000` somente poderá ser promovido depois da conclusão de `B010`–`B012`.
