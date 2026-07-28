# B061/B062 - Modulo, Folder e nomenclatura dos objetos gerados

## Status

Concluido no GeneXus 18 Upgrade 15.

## Objetivo

Validar que os objetos especificos da API gerada permanecem no modulo da `Transaction`, organizados no Folder especifico quando o tipo de objeto suporta Folder, e que os nomes efetivos seguem as convencoes congeladas em B012/B062.

## Escopo validado

- `Transaction`: `Contrato`.
- Modulo da `Transaction`: `Root Module`.
- Folder especifico: `ContratoOpenApi`.
- API Object: `apiContrato`.
- Procedures: `procContrato_API_List`, `procContrato_API_Get`, `procContrato_API_Create` e `procContrato_API_Update`.
- SDTs proprios: `sdtContrato_API_CreateRequest`, `sdtContrato_API_UpdateRequest`, `sdtContrato_API_Response`, `sdtContrato_API_ListFilters` e `sdtContrato_API_ListResponse`.
- SDTs compartilhados: `sdt_API_ErrorResponse` e `sdt_API_Pagination`.
- File de metadata: `apiContrato_Metadata`.

## Evidencia manual

O wizard foi executado para `Contrato` com SDTs, Procedures, API Object e Metadata confirmados para escrita. A Output registrou reencontro conservador, sem duplicacao:

- B040-B046 reencontrou 7 SDTs, com os 5 SDTs proprios no escopo `TransactionModuleFolder:ContratoOpenApi` e os 2 SDTs compartilhados no escopo `RootModuleFolder:GxOpenAPI`.
- B050-B053 reencontrou 4 Procedures com `TransactionFolder='ContratoOpenApi'`.
- B054 reencontrou `apiContrato` com `TransactionFolder='ContratoOpenApi'`.
- B060 reencontrou e regravou o File `apiContrato_Metadata` com schema `GOAB_API_METADATA_B060_V1`.

A inspecao visual da IDE confirmou, dentro de `ContratoOpenApi`, a presenca de `apiContrato`, das quatro Procedures `procContrato_API_*` e dos cinco SDTs `sdtContrato_API_*`.

O File `apiContrato_Metadata` foi conferido pelas Properties da IDE com `Module='Root Module'`, `Qualified Name='apiContrato_Metadata'` e `External File Name='apiContrato_Metadata.json'`. A validacao nao exige Folder para `File`: a referencia `object-file.md` da skill `nexa` indica que Files sao organizados por modulo, nao por Folder; a wiki oficial do GeneXus lista `Module` como propriedade de File e nao apresenta Folder como propriedade aplicavel.

O JSON exportado em `Temp/apiContrato_Metadata.json` parseou com:

- `ownership.transactionModule='Root Module'`;
- `objects.transactionFolder.name='ContratoOpenApi'`;
- `objects.apiObject.name='apiContrato'`;
- quatro Procedures no padrao `procContrato_API_*`;
- cinco SDTs proprios no padrao `sdtContrato_API_*`;
- dois SDTs compartilhados `sdt_API_ErrorResponse` e `sdt_API_Pagination`.

## Conclusao

B061/B062 foram validados: os objetos especificos que suportam Folder estao em `ContratoOpenApi`, os compartilhados estao em `GxOpenAPI`, o File de metadata esta no modulo correto e os nomes persistidos seguem as convencoes congeladas. O passo nao completa REST, codigos HTTP finais, seguranca definitiva nem ciclo completo de regeneracao.
