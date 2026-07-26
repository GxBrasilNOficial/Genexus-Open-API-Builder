# B040-B046 — Criação real de SDTs a partir do ApiPlan

## Estado

B040-B046 foram validados manualmente no GeneXus 18 U15 como primeira escrita real de SDTs a partir do `ApiPlan` em memória.

O comando adicionado é `Criar SDTs (B040-B046)`. Depois da validação inicial, a mesma etapa foi integrada ao encerramento de `Abrir Wizard (B030)`, mantendo confirmação modal própria e os mesmos limites de escrita.

## Escopo implementado

- registro do comando em `Package.cs`;
- registro do comando no manifesto `GenexusOpenApiBuilder.package`;
- criação de `ApiPlanSdtWriter` para receber o `ApiPlan` em memória validado em B039;
- confirmação modal obrigatória antes de qualquer escrita na KB;
- criação ou reencontro dos SDTs compartilhados e próprios planejados;
- bloqueio conservador quando já existe SDT com mesmo nome sem descrição sentinela do gerador;
- registro na Output de cada SDT criado ou reencontrado.

## Comportamento de segurança

O comando não executa sem:

- KB ativa;
- `ApiPlan` em memória criado pelo wizard;
- `Transaction` em memória reencontrada na KB ativa;
- correspondência entre `ApiPlan.TransactionName` e a Transaction selecionada;
- confirmação explícita do usuário no modal da IDE.

Se qualquer condição falhar, a Output registra o bloqueio e nenhuma alteração é feita na KB.

## Objetos que podem ser escritos após confirmação

SDTs compartilhados:

- `sdt_API_ErrorResponse`;
- `sdt_API_Pagination`.

SDTs próprios da Transaction selecionada:

- `sdt<NomeBase>_API_CreateRequest`;
- `sdt<NomeBase>_API_UpdateRequest`;
- `sdt<NomeBase>_API_Response`;
- `sdt<NomeBase>_API_ListFilters`;
- `sdt<NomeBase>_API_ListResponse`.

Também pode criar ou reencontrar o Folder `GxOpenAPI` para os SDTs compartilhados.

## Limites explícitos

B040-B046 não criam:

- Procedures;
- API Object;
- File de metadata persistente definitiva;
- permissões GAM;
- descrição `[Description]` em serviços reais.

A política de reencontro ainda usa descrição sentinela do gerador, não metadata persistente definitiva.

## Validação local

Validações executadas localmente:

```powershell
dotnet build Src\GenexusOpenApiBuilder.sln -c Release --no-restore
pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1
git diff --check
```

Resultado local: build Release com 0 erros, registro de 9 comandos sincronizado e whitespace limpo.

## Validação manual na IDE

A validação manual foi registrada em 2026-07-25 na Transaction `Contrato`.

A IDE exibiu o modal de confirmação explícita antes da escrita na KB e a operação foi autorizada pelo usuário.

A Output registrou:

```text
[Genexus Open API Builder][B040-B046] Escrita de SDTs concluida: Transaction='Contrato', PlannedOwnSdts=5, PlannedSharedSdts=2, Created=7, Reencountered=0. Nenhuma Procedure, API Object ou metadata persistente definitiva foi criada.
```

SDTs compartilhados criados:

- `sdt_API_ErrorResponse`;
- `sdt_API_Pagination`.

SDTs próprios criados:

- `sdtContrato_API_CreateRequest`;
- `sdtContrato_API_UpdateRequest`;
- `sdtContrato_API_Response`;
- `sdtContrato_API_ListFilters`;
- `sdtContrato_API_ListResponse`.

A evidência visual da IDE confirmou a presença dos cinco SDTs próprios no módulo da Transaction. A Output confirmou também os dois SDTs compartilhados no escopo `RootModuleFolder:GxOpenAPI`.

## Integração com o wizard

Após a validação dos comandos separados, B040-B046 foi incorporado ao encerramento de `Abrir Wizard (B030)`. O wizard conclui o `ApiPlan`, solicita confirmação explícita para criar ou reencontrar os SDTs e registra a Output com `Trigger='Wizard'`. Se essa etapa for cancelada ou bloqueada, o wizard não deve oferecer a criação de Procedures no mesmo fluxo.

A integração foi validada manualmente em 2026-07-25 na Transaction `Contrato`. O wizard registrou B040-B046 com `Trigger='Wizard'`, reencontrou os 7 SDTs existentes (`Created=0`, `Reencountered=7`) e preservou o limite de não criar Procedure, API Object ou metadata persistente definitiva.

## Próximo passo

B040-B046 estão concluídos como comando separado e como etapa integrada ao wizard. A próxima ação executável vigente deve ser consultada no checkpoint `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`.
