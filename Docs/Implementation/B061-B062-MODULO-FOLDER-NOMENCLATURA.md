# B061/B062 - Módulo, Folder e nomenclatura dos objetos gerados

## Status

Concluído no GeneXus 18 Upgrade 15.

## Objetivo

Validar que os objetos específicos da API gerada permanecem no módulo da `Transaction`, organizados no Folder específico quando o tipo de objeto suporta Folder, e que os nomes efetivos seguem as convenções congeladas em B012/B062.

## Escopo validado

### Caso Root Module

- `Transaction`: `Contrato`.
- Módulo da `Transaction`: `Root Module`.
- Folder específico: `ContratoOpenApi`.
- API Object: `apiContrato`.
- Procedures: `procContrato_API_List`, `procContrato_API_Get`, `procContrato_API_Create` e `procContrato_API_Update`.
- SDTs próprios: `sdtContrato_API_CreateRequest`, `sdtContrato_API_UpdateRequest`, `sdtContrato_API_Response`, `sdtContrato_API_ListFilters` e `sdtContrato_API_ListResponse`.
- SDTs compartilhados: `sdt_API_ErrorResponse` e `sdt_API_Pagination`.
- File de metadata: `apiContrato_Metadata`.

### Caso módulo não-root

- `Transaction`: `SimulationResult`.
- Módulo da `Transaction`: `Entities`.
- Folder específico: `SimulationResultOpenApi`.
- API Object: `apiSimulationResult`.
- Procedures: `procSimulationResult_API_List`, `procSimulationResult_API_Get`, `procSimulationResult_API_Create` e `procSimulationResult_API_Update`.
- SDTs próprios: `sdtSimulationResult_API_CreateRequest`, `sdtSimulationResult_API_UpdateRequest`, `sdtSimulationResult_API_Response`, `sdtSimulationResult_API_ListFilters` e `sdtSimulationResult_API_ListResponse`.
- SDTs compartilhados: `sdt_API_ErrorResponse` e `sdt_API_Pagination`.
- File de metadata: `apiSimulationResult_Metadata`.

## Evidência manual

### Contrato em Root Module

O wizard foi executado para `Contrato` com SDTs, Procedures, API Object e Metadata confirmados para escrita. A Output registrou reencontro conservador, sem duplicação:

- B040-B046 reencontrou 7 SDTs, com os 5 SDTs próprios no escopo `TransactionModuleFolder:ContratoOpenApi` e os 2 SDTs compartilhados no escopo `RootModuleFolder:GxOpenAPI`.
- B050-B053 reencontrou 4 Procedures com `TransactionFolder='ContratoOpenApi'`.
- B054 reencontrou `apiContrato` com `TransactionFolder='ContratoOpenApi'`.
- B060 reencontrou e regravou o File `apiContrato_Metadata` com schema `GOAB_API_METADATA_B060_V1`.

A inspeção visual da IDE confirmou, dentro de `ContratoOpenApi`, a presença de `apiContrato`, das quatro Procedures `procContrato_API_*` e dos cinco SDTs `sdtContrato_API_*`.

O File `apiContrato_Metadata` foi conferido pelas Properties da IDE com `Module='Root Module'`, `Qualified Name='apiContrato_Metadata'` e `External File Name='apiContrato_Metadata.json'`.

O JSON exportado em `Temp/apiContrato_Metadata.json` parseou com:

- `ownership.transactionModule='Root Module'`;
- `objects.transactionFolder.name='ContratoOpenApi'`;
- `objects.apiObject.name='apiContrato'`;
- quatro Procedures no padrão `procContrato_API_*`;
- cinco SDTs próprios no padrão `sdtContrato_API_*`;
- dois SDTs compartilhados `sdt_API_ErrorResponse` e `sdt_API_Pagination`.

### SimulationResult em módulo não-root

Após correção de runtime, o wizard foi executado para `SimulationResult` no módulo `Entities`, com SDTs, Procedures, API Object e Metadata confirmados para escrita. A Output registrou reencontro conservador e criação da metadata:

- B040-B046 reencontrou 7 SDTs, com os 5 SDTs próprios no escopo `TransactionModuleFolder:SimulationResultOpenApi` e os 2 SDTs compartilhados no escopo `RootModuleFolder:GxOpenAPI`.
- B050-B053 reencontrou 4 Procedures com `TransactionFolder='SimulationResultOpenApi'`.
- B054 reencontrou `apiSimulationResult` com `TransactionFolder='SimulationResultOpenApi'`.
- B056 aplicou descrições nos 4 serviços do API Object real.
- B060 criou o File `apiSimulationResult_Metadata` com `Status='Created'`, `Guid='802ad125-da76-45de-baeb-8a7e8b81f5e4'`, `Bytes=26673` e `Sha256='38FDD22961C30FC299A603A99B85AE45CD57EA716D5E4637D40962073FFABBC3'`.

A inspeção visual da IDE confirmou, dentro de `SimulationResultOpenApi`, a presença de `apiSimulationResult`, das quatro Procedures `procSimulationResult_API_*` e dos cinco SDTs `sdtSimulationResult_API_*`.

O File `apiSimulationResult_Metadata` foi conferido pelas Properties da IDE com `Module='Entities'`, `Qualified Name='Entities.apiSimulationResult_Metadata'` e `External File Name='apiSimulationResult_Metadata.json'`.

O JSON exportado em `Temp/apiSimulationResult_Metadata.json` parseou com:

- `ownership.transactionModule='Entities'`;
- `objects.transactionFolder.name='SimulationResultOpenApi'`;
- `objects.apiObject.name='apiSimulationResult'`;
- quatro Procedures no padrão `procSimulationResult_API_*`;
- cinco SDTs próprios no padrão `sdtSimulationResult_API_*`;
- dois SDTs compartilhados `sdt_API_ErrorResponse` e `sdt_API_Pagination`.

O `Build All` da KB especificou e gerou `Entities.apiSimulationResult` e as quatro Procedures `Entities.procSimulationResult_API_*`, gerou documentação REST para `Entities.apiSimulationResult` e concluiu com `Success: Build All`.

## Observação sobre File

A validação não exige Folder para `File`: a referência `object-file.md` da skill `nexa` indica que Files são organizados por módulo, não por Folder; a wiki oficial do GeneXus lista `Module` como propriedade de File e não apresenta Folder como propriedade aplicável. O critério validado para metadata é o módulo da `Transaction`.

## Conclusão

B061/B062 foram validados: os objetos específicos que suportam Folder permanecem no Folder `<Transaction>OpenApi` dentro do módulo da `Transaction`, os SDTs compartilhados permanecem em `GxOpenAPI`, o File de metadata permanece no módulo da `Transaction` e os nomes persistidos seguem as convenções congeladas. O passo não completa REST, códigos HTTP finais, segurança definitiva nem ciclo completo de regeneração.
