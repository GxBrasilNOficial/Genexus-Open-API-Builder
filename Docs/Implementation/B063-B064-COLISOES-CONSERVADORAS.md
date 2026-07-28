# B063/B064 — Colisões por Nome e Metadata

## Escopo

B063 detecta colisões por nome e metadata antes da escrita. B064 bloqueia a execução inteira para colisões externas, incompatíveis ou ambíguas, sem sobrescrever objetos nem criar variantes `_v2`.

## Implementação

O wizard executa `ApiPlanWritePreflight` depois de montar o `ApiPlan` e antes de chamar qualquer writer. O preflight agrega o estado de SDTs, Procedures, API Object e File de metadata. Se qualquer etapa estiver bloqueada, a Output registra `[B063/B064]` e o fluxo retorna antes do primeiro `Save()`.

A inspeção do API Object inclui a validação semântica do `Service Source`. O parser foi isolado em `ApiPlanServiceSourceContract` e coberto pelo teste `Tests/ServiceSourceContract/Test-ApiPlanServiceSourceContract.ps1`, verificando vínculo serviço-Procedure, argumentos e módulo esperado.

## Validação manual no U15

Em 2026-07-28, na Transaction `Entities.Checkpoint_`:

1. uma Procedure externa `procCheckpoint__API_List` bloqueou o wizard antes do primeiro `Save()`; a KB não recebeu Folder, SDT, API, File ou variante `_v2`;
2. após remover a colisão, o preflight foi aprovado e o wizard criou os 5 SDTs próprios, 4 Procedures, `apiCheckpoint_` e `apiCheckpoint__Metadata`; o Build All aprovou;
3. a reexecução reencontrou 7 SDTs, 4 Procedures e a metadata com o mesmo GUID, sem criação;
4. um JSON de metadata com `ownership.apiName` incompatível bloqueou o wizard antes do primeiro `Save()`; o JSON original foi restaurado.

O escopo não completa REST, códigos HTTP finais, segurança definitiva nem regeneração completa.
