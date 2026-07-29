# B067 - Integridade de Metadata para Descrições e Contrato

## Objetivo

Persistir na metadata própria dados suficientes para detectar alteração manual posterior em descrições geradas e contrato essencial antes de qualquer escrita na KB.

## Implementação

A metadata B060 passa a incluir o bloco `integrity` com versão `GOAB_B067_INTEGRITY_V1`.

Esse bloco registra:

- descrições geradas por serviço e hash consolidado;
- hash do contrato planejado relevante para paths, serviços, campos, filtros, paginação, ordenação e segurança;
- sentinela de descrição do API Object próprio;
- GUID do API Object reencontrado;
- modo, hash atual e hash esperado do Service Source como evidência.

Na reexecução, o reencontro conservador do API Object exige metadata compatível e integridade B067 compatível. Quando a metadata tem bloco `integrity`, divergência em descrição, ownership, contrato planejado ou contrato semântico do Service Source bloqueia o estado antes do primeiro `Save()`.

O hash textual completo do Service Source é mantido para auditoria. O bloqueio do Service Source usa o parser semântico B054/B055, para tolerar diferenças inofensivas de formatação quando serviços, Procedures chamadas, argumentos e módulo esperado continuam compatíveis.

Metadata legada sem bloco `integrity` continua aceita para permitir o primeiro upgrade conservador. Depois que B067 grava o bloco, a ausência ou divergência deixa de ser reparada silenciosamente.

## Evidência Manual

Validado manualmente no GeneXus 18 U15 em 2026-07-28, usando a Transaction `Transaction2`.

Fluxo validado:

- gravação somente de metadata com objetos já gerados;
- reencontro limpo com `PlannedContractHash='888D69C88DD83F1A4C521E0A1539C75BC52B513FAC7099C6EA6A6138D075980C'`;
- alteração manual da descrição do serviço `List` de `[Description("List Transaction2")]` para `[Description("List Transaction 2")]`;
- bloqueio visual na aba `Metadata B060` com estado `Bloqueado` e menção a `integridade B067 divergente`;
- Output confirmando `[B063/B064/B067] Estado bloqueado detectado no wizard antes de confirmar escrita` e `Nenhum Save foi solicitado`;
- restauração da descrição original e nova execução aprovada com o mesmo hash de contrato planejado.

## Limites Mantidos

B067 não completa REST final, segurança definitiva, códigos HTTP finais, sincronização ampla por alteração de Transaction nem remoção de API gerada. Esses itens permanecem nas Sprints 6 e 7.

## Resultado

B067 fecha o escopo de integridade inicial da metadata da Sprint 5. Na conclusão desta frente, a próxima frente operacional passava a ser B070, iniciando a Sprint 6 para completar `List` com filtros, paginação e ordenação determinística. Esse encaminhamento foi consumado posteriormente no fechamento B070/B077.
