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
- implementação prática ainda não iniciada.

## Frente ativa

**Sprint 0 — Preparação**, correspondente à **Fase 0 — Setup** do backlog.

## Próxima ação única

Executar `B010`:

> Localizar e confirmar o SDK de Extensibility do GeneXus 18 Upgrade 15 e criar em `Src` a solução e o projeto mínimos da extensão, com dependências documentadas e build reproduzível.

## Critério de conclusão e evidência esperada

- versão e origem do SDK registradas;
- método reproduzível de localizar as dependências, sem versionar caminho absoluto específico da máquina;
- solução e projeto salvos em caminho relativo dentro de `Src`;
- comando de build documentado;
- evidência de build mínimo bem-sucedido;
- nenhum wizard funcional nem alteração de KB nesta ação.

## Ação imediatamente posterior

Na **Sprint 1 — Spike SDK Real**, executar `B000`:

> Carregar a extensão mínima na IDE GeneXus 18 Upgrade 15 e registrar a evidência do resultado.

## Sequência operacional vigente

1. Sprint 0 executa a Fase 0 (`B010`–`B012`) e prepara o terreno.
2. Sprint 1 executa o pacote inicial de viabilidade da Fase -1 (`B000`–`B006`).
3. As Fases 1–8 dependem da aprovação desse spike.
4. Sprint 2 entrega apenas o protótipo navegável e não persistente do wizard.
5. Sprint 3 cria metadata e `ApiPlan`.
6. Sprint 4 integra o wizard ao engine pela primeira vez.
7. Sprints 5–7 completam os contratos e a segurança operacional.
8. O marco **wizard funcional do MVP concluído** ocorre ao final da Sprint 7, antes da Alpha.

## Bloqueios e fatos ainda não validados

- disponibilidade e forma oficial de obtenção do SDK no ambiente local;
- compatibilidade prática das APIs do SDK com GeneXus 18 Upgrade 15;
- comprovação progressiva dos gates técnicos transversais definidos nos documentos 09, 15 e 24.

## Documentos governantes

- [06 — Backlog](Foundation/06-BACKLOG_v0.1.md)
- [09 — Integração com o SDK](Foundation/09-INTEGRACAO_GeneXus_Extensibility_SDK.md)
- [15 — Testes e qualidade](Foundation/15-TESTES_VALIDACAO_E_QUALIDADE.md)
- [24 — Plano por sprints](Foundation/24-PLANO_IMPLEMENTACAO_REAL_POR_SPRINTS.md)

## Marcos ainda não iniciados

- solução de Extensibility;
- spike real na IDE;
- protótipo navegável do wizard;
- `ApiPlan`;
- engine de geração;
- Alpha público.

## Protocolo de atualização

Toda mudança de marco, frente ativa ou próxima ação deve atualizar este checkpoint no mesmo commit que produz a mudança. O checkpoint deve manter uma única próxima ação e apontar para os contratos, sem duplicá-los.
