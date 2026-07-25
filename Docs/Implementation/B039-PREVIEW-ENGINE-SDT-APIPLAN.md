# B039 — Preview de engine para SDTs a partir do ApiPlan

## Estado

B039 preparou a primeira integração `wizard -> ApiPlan -> engine` em modo de preview, sem escrever na KB.

O objetivo foi transformar o `ApiPlan` em um contrato de SDTs resolvido em memória antes da primeira criação real de objetos da Sprint 4.

## Escopo implementado

- novo builder `ApiPlanSdtGenerationPlanBuilder` em `Src/Domain/ApiPlanSdtGenerationPlan.cs`;
- criação de `ApiPlanSdtGenerationPlan` a partir do `ApiPlan` produzido pelo wizard único;
- definição em memória de cinco SDTs próprios da Transaction:
  - `CreateRequest` (`B040`);
  - `UpdateRequest` (`B041`);
  - `Response` (`B042`);
  - `ListFilters` (`B043`);
  - `ListResponse` (`B044`);
- definição em memória dos SDTs compartilhados:
  - `sdt_API_ErrorResponse` (`B045/B046`);
  - `sdt_API_Pagination` (`B045/B046`);
- registro na Output da IDE, no fechamento do wizard, da fase, status, contagem de SDTs e membros planejados.

## Limite explícito

B039 não cria, altera, exclui nem salva objetos na KB.

O plano fica marcado como:

- `Phase='Sprint4SdtEnginePreviewOnly'`;
- `Status='ResolvedSdtContractPreviewNoKbWrite'`;
- `WritesKnowledgeBase=False`.

Também não cria `Procedure`, `API Object`, `Folder` ou `File` de metadata.

## Regras aplicadas

- nomes dos SDTs próprios vêm do `ApiPlan`;
- SDTs próprios ficam no escopo lógico `TransactionModule`;
- SDTs compartilhados ficam no escopo lógico `RootModuleFolder:GxOpenAPI`;
- `ListResponse` contém `Items`, `Pagination` e `AppliedFilters`;
- `Pagination` aponta para `sdt_API_Pagination`;
- `AppliedFilters` aponta para o SDT específico de filtros da Transaction;
- períodos de `DateTime` em `ListFilters` são planejados como `Date`, conforme contrato funcional de List.

## Validação local

Build local executado com sucesso:

```powershell
dotnet build Src\GenexusOpenApiBuilder.sln -c Release --no-restore
```

Resultado: compilação com sucesso, 0 avisos e 0 erros.

## Validação pendente na IDE

A validação manual no GeneXus 18 U15 ainda deve confirmar as linhas `[Sprint4]` na Output após executar `Abrir Wizard (B030)` e concluir o wizard.

Essa validação continua sem escrita na KB.

## Próximo passo
