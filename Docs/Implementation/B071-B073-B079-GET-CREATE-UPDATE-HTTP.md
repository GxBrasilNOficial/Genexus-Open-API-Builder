# B071-B073/B079 - Get, Create, Update e Status HTTP

## Objetivo

Completar a primeira versão runtime de `Get`, `Create` e `Update` sobre os objetos já gerados por B040-B046, B050-B053, B054, B055 e B070, preservando o trio API Object/Procedure/SDT e preparando uma única validação manual na IDE.

## Implementação Local

A etapa acionada no wizard como conclusão REST de Get/Create/Update passa a:

- regravar `proc<Transacao>_API_Get` com chave simples ou composta, `GetResponse`, `ErrorResponse` e `RestStatusCode`;
- regravar `proc<Transacao>_API_Create` via Business Component com `CreateResponse`, `ErrorResponse` e `RestStatusCode`;
- regravar `proc<Transacao>_API_Update` via Business Component com chave simples ou composta, `UpdateResponse`, `ErrorResponse` e `RestStatusCode`;
- sincronizar o API Object com `Get(in:&PK..., out:&GetResponse, out:&ErrorResponse)`, `Create(in:&CreateRequest, out:&CreateResponse, out:&ErrorResponse)` e `Update(in:&PK..., in:&UpdateRequest, out:&UpdateResponse, out:&ErrorResponse)`;
- expor `ErrorResponse` como saída pública dos serviços e manter `RestStatusCode` como variável interna do API Object usada na chamada às Procedures;
- gravar Events no API Object para aplicar `&RestCode = &RestStatusCode` nos eventos `Get.After`, `Create.After` e `Update.After`;
- preservar o contrato B070 quando `List` for aplicado no mesmo fluxo, mantendo todos os serviços no mesmo `ServiceGroupSource`.

Status planejados:

- `Get` encontrado: `200`;
- `Get` inexistente: `404` com `ErrorResponse.Code = "not_found"`;
- `Create` bem-sucedido: `201`;
- `Create` rejeitado pelo Business Component: `422` com `ErrorResponse.Code = "validation_error"`;
- `Update` encontrado e salvo: `200`;
- `Update` inexistente: `404` com `ErrorResponse.Code = "not_found"`;
- `Update` rejeitado pelo Business Component: `422` com `ErrorResponse.Code = "validation_error"`.

`Location` de Create continua desejável, mas não foi implementado nesta mudança local porque ainda depende de confirmação de suporte nativo simples no GeneXus/API Object. O contrato documentado em Foundation 27 não justifica DLL, External Object ou solução complexa para esse cabeçalho.

## Preflight e Reencontro

Antes de gravar, o writer valida:

- Transaction do `ApiPlan`;
- Business Component habilitado;
- presença de `Get`, `Create` e `Update` no plano;
- SDTs próprios e compartilhados reencontrados;
- Folder da Transaction;
- Procedures próprias B051-B053;
- API Object próprio;
- Source, Rules, variáveis e Events já existentes quando houver reexecução.

O API Object B079 é tratado como evolução conservadora do estado B055. O preflight aceita como migrável o B055 próprio legado quando `ServiceGroupSource`, `Rules` e variáveis batem exatamente com a geração B055 anterior. Esse aceite não amplia escopo para objetos externos: qualquer Source, Rules, variável ou Event divergente continua bloqueando a escrita.

Após a primeira validação na IDE, o preflight de reexecução também passou a aceitar Sources B079 já gerados por equivalência canônica restrita. A comparação remove apenas whitespace para tolerar normalização textual inofensiva da IDE, mas bloqueia qualquer código manual extra antes de sobrescrever a Procedure.

`ApplyList` reconhece esse estado e, quando precisa sincronizar List, preserva `Get`, `Create`, `Update`, a anotação `[RestMethod(POST)]` do `Create`, `ErrorResponse` como saída pública, `RestStatusCode` interno e Events.

A integridade B067 da metadata foi ajustada para reconhecer `[Description]` seguida de outras anotações, como `[RestMethod(POST)]`, antes da assinatura do serviço. Esse caso ocorre no `Create` B079 e não deve ser tratado como alteração manual de descrição.

## Validação Mecânica Local

Executado localmente em 2026-08-01:

- `pwsh -NoProfile -File Tests\ServiceSourceContract\Test-ApiPlanServiceSourceContract.ps1`;
- `pwsh -NoProfile -File Tests\ListProcedure\Test-ApiPlanListProcedureReencounterPolicy.ps1`;
- `pwsh -NoProfile -File Tests\MetadataIntegrity\Test-ApiPlanMetadataIntegrity.ps1`;
- `pwsh -NoProfile -File Tests\WizardPreferences\Test-PrototypeWizardPreferences.ps1`;
- `pwsh -NoProfile -File Tests\WizardNavigation\Test-PrototypeWizardBusinessComponentNavigationPolicy.ps1`;
- `pwsh -NoProfile -File Tests\WritePreflight\Test-ApiPlanWritePreflightScope.ps1`;
- `pwsh -NoProfile -File Tools\Test-ExtensionCommandRegistration.ps1`;
- `dotnet build Src\GenexusOpenApiBuilder.sln --configuration Release --no-restore`.

Resultado: todos passaram. O build Release terminou com 0 avisos e 0 erros.

## Validação Manual no GeneXus 18 U15

Avanço validado manualmente em 2026-08-01 na Transaction `NotaFiscal`:

- wizard com SDTs, Procedures, API Object, Get/Create/Update REST, List e metadata confirmados;
- `Business Component` habilitado e depois reencontrado como apto;
- B071-B073/B079 aplicou `Get`, `Create` e `Update`, sincronizando API Object e Procedures;
- B070 aplicou a listagem no mesmo fluxo, preservando o contrato B079;
- B060 criou e depois reencontrou `apiNotaFiscal_Metadata`;
- B067 regravou a integridade após reencontro;
- preferências do wizard gravaram `List`, metadata e Get/Create/Update REST como defaults visíveis;
- reexecução em modo `teste de reencontro` reaplicou B071-B073/B079, B070 e B060 sem bloquear;
- B071-B073/B079 aplicou após migrar a variante intermediária com `ErrorItem` para a geração atual sem item nested;
- B056 reaplicou descrições no API Object real durante B071-B073/B079;
- B060 reencontrou `apiNotaFiscal_Metadata` com `Bytes=30370` e `Sha256='CBD7D75F3D8FE7031F9591CDAD03F72B934F809B9FA904CAF61FBA5269FF96BA'`;
- B067 gravou integridade com `PlannedContractHash='028857EF0713350C3D01326262A034228006209F04FD852A5601A3C1AB890F14'`.
- Build All especificou e gerou `apiNotaFiscal`, `procNotaFiscal_API_Create`, `procNotaFiscal_API_Get`, `procNotaFiscal_API_Update`, `procNotaFiscal_API_List`, os SDTs REST e a documentação REST, concluindo com sucesso; permaneceu apenas o warning ambiental conhecido de `FBiTextSharp.dll`.

Correções feitas durante a validação:

- variável item de mensagens do BC alterada para `Messages.Message, GeneXus.Common`;
- tela de preferências passou a exibir `listagem` e `metadata da API`;
- textos visíveis do wizard deixaram de expor IDs internos de backlog;
- parser de integridade B067 passou a aceitar `[RestMethod(POST)]` entre `[Description]` e o serviço;
- parser de contrato do `ServiceGroupSource` passou a rejeitar B079 quando `Create` não está anotado como `[RestMethod(POST)]`;
- `ErrorResponse` passou a ser saída pública em `Get`, `Create` e `Update`, deixando de ser apenas argumento interno Procedure/API Object;
- reexecução de Procedures B079 passou a usar equivalência canônica restrita do Source, bloqueando código extra.
- a geração atual deixou de preencher `ErrorResponse.Errors[]` por `ErrorItem` nested, depois que a IDE rejeitou a validação da Procedure com item de subestrutura SDT; `ErrorResponse.Code` e `ErrorResponse.Message` continuam compondo o corpo de erro público e `&Messages.ToJson()` continua registrado no Output técnico.
- o estado intermediário com `ErrorItem` tipado como `sdt_API_ErrorResponse.Error`, `sdt_API_ErrorResponse.Errors`, `sdt_API_ErrorResponse.ErrorsItem` ou `sdt:sdt_API_ErrorResponse.Errors` é aceito apenas como migração conservadora para regravar a Procedure para o contrato top-level atual.

A geração sem `ErrorItem` nested foi validada mecanicamente no repositório e reexecutada na IDE até gravar Procedures, API Object, List e metadata. O preenchimento de `Errors[]` permanece pendente de prova específica sobre a tipagem real de item de subestrutura SDT no SDK/GeneXus.

## Validação Runtime Pendente

Ainda falta validar no GeneXus 18 U15:

- chamada HTTP de `Get` encontrado e inexistente;
- chamada HTTP de `Create` com sucesso e erro de regra de negócio;
- chamada HTTP de `Update` com sucesso, inexistente e erro de regra de negócio;
- confirmação da forma JSON efetivamente emitida pelo API Object quando há múltiplos `out` em erros controlados;
- decisão final sobre `Location` de Create após inspecionar suporte nativo simples.
