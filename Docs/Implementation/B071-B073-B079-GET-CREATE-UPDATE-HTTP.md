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
- validar membros obrigatórios em `Create` e `Update` comparando cada membro recebido com o valor default do mesmo membro em uma instância vazia do próprio SDT de request, sem comando C# embutido;
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

`ApplyList` reconhece esse estado e, quando precisa sincronizar List, preserva `Get`, `Create`, `Update`, as anotações REST explícitas de `Create`/`Update`, `RestPath` com parâmetros GeneXus no formato `{&Chave}`, `ErrorResponse` como saída pública, `RestStatusCode` interno e Events.

A integridade B067 da metadata foi ajustada para reconhecer `[Description]` seguida de outras anotações, como `[RestMethod(POST)]`, antes da assinatura do serviço. Esse caso ocorre no `Create` B079 e não deve ser tratado como alteração manual de descrição.

Após a correção de `RestPath` para a sintaxe GeneXus `{&Chave}`, B067 também passou a aceitar hashes de contrato planejado e de `ServiceGroupSource` esperado de variantes anteriores geradas pelo próprio wizard. Essa compatibilidade é restrita a contratos conhecidos e não libera metadata externa, ownership divergente, descrição alterada manualmente ou `ServiceGroupSource` que não seja reconhecido semanticamente pelo preflight atual.

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
- B060 reencontrou `apiNotaFiscal_Metadata` com `Bytes=30374` e `Sha256='45C8169419491D10E4C50833A12E3DE14A9E6156EE58BB4BE07223B77951E7B7'`;
- B067 gravou integridade com `PlannedContractHash='6B2781C5A6E6970A03428858E286B1091E609E9B278ECB7428402AFBA3722536'`.
- Build All especificou e gerou `apiNotaFiscal`, `procNotaFiscal_API_Create`, `procNotaFiscal_API_Get`, `procNotaFiscal_API_Update`, `procNotaFiscal_API_List`, os SDTs REST e a documentação REST, concluindo com sucesso; a validação inicial ainda emitia `spc0087` por usar comando C# embutido, removido depois em favor de validação GeneXus nativa por valor default do SDT.

Correções feitas durante a validação:

- variável item de mensagens do BC alterada para `Messages.Message, GeneXus.Common`;
- tela de preferências passou a exibir `listagem` e `metadata da API`;
- textos visíveis do wizard deixaram de expor IDs internos de backlog;
- parser de integridade B067 passou a aceitar `[RestMethod(POST)]` entre `[Description]` e o serviço;
- integridade B067 passou a aceitar hashes de contrato planejado e de `ServiceGroupSource` esperado de variantes próprias anteriores com `RestPath` legado `{Chave}` ou anotações REST ainda incompletas, mantendo bloqueio para metadata externa ou contrato semântico divergente;
- parser de contrato do `ServiceGroupSource` passou a rejeitar B079 quando `Create` não está anotado como `[RestMethod(POST)]`;
- `ErrorResponse` passou a ser saída pública em `Get`, `Create` e `Update`, deixando de ser apenas argumento interno Procedure/API Object;
- reexecução de Procedures B079 passou a usar equivalência canônica restrita do Source, bloqueando código extra.
- a geração atual deixou de preencher `ErrorResponse.Errors[]` por `ErrorItem` nested, depois que a IDE rejeitou a validação da Procedure com item de subestrutura SDT; `ErrorResponse.Code` e `ErrorResponse.Message` continuam compondo o corpo de erro público e `&Messages.ToJson()` continua registrado no Output técnico.
- o estado intermediário com `ErrorItem` tipado como `sdt_API_ErrorResponse.Error`, `sdt_API_ErrorResponse.Errors`, `sdt_API_ErrorResponse.ErrorsItem` ou `sdt:sdt_API_ErrorResponse.Errors` é aceito apenas como migração conservadora para regravar a Procedure para o contrato top-level atual.
- o estado intermediário que validava presença de membros obrigatórios por `csharp`/`IsDirty` do SDT é aceito apenas como migração conservadora; a tentativa intermediária com `HttpRequest.ToString()` + `Properties` também é aceita apenas para reencontro, pois o body bruto não ficou disponível dentro da Procedure chamada pelo API Object.

A geração sem `ErrorItem` nested foi validada mecanicamente no repositório e reexecutada na IDE até gravar Procedures, API Object, List e metadata. O preenchimento de `Errors[]` permanece pendente de prova específica sobre a tipagem real de item de subestrutura SDT no SDK/GeneXus.

### Validação de membros obrigatórios: por que deixou de ser por presença

A intenção original era distinguir membro JSON **ausente** de membro **enviado com valor default**. Quatro caminhos foram testados e todos foram descartados, nesta ordem:

| Caminho | Resultado |
| --- | --- |
| `csharp` com `IsDirty` interno do SDT gerado | Funciona em runtime, mas emite `spc0087` (`C# language statements are used`). Rejeitado por decisão do projeto: o código gerado não deve conter comando GeneXus `csharp`. |
| `&HttpRequest.ToString()` + `&Properties.FromJson()` dentro da Procedure | Compila e remove `spc0087`, mas não funciona: o corpo bruto não chega na Procedure chamada pelo API Object. `Update` completo respondeu 400 indevido em 2026-08-02. |
| `&Sdt.IsDirty(!"Membro")` como chamada nativa no Source | Não existe. Os métodos de variável SDT no Source são `Clone`, `FromJson`, `FromJsonFile`, `FromXml`, `FromXmlFile`, `ToJson` e `ToXml`. Confirmado pelo IntelliSense da IDE e pela documentação oficial. Nunca chegou a ser gravado em KB. |
| `&HttpRequest.ToString()` no evento `Before` do API Object | Descartado por sonda em 2026-08-03. O evento `Before` existe, executa antes da Procedure e permite saída antecipada com `&RestCode`, mas `ToString()` devolveu string vazia (`len=0`) tanto em .NET/PostgreSQL quanto em .NET Framework/SQL Server: o corpo já foi consumido pelo pipeline REST antes de qualquer código GeneXus executar. |

Conclusão registrada: **o GeneXus não expõe, sem comando `csharp`, a informação de presença de membro no JSON recebido.** A informação não sobrevive à desserialização, e o único ponto onde existiria — o stream bruto da requisição — já foi consumido em todos os lugares onde código GeneXus pode executar.

A geração passou então a validar **preenchimento**, não presença. Cada membro obrigatório é comparado com o valor default do mesmo membro em uma instância vazia do próprio SDT de request:

```
&EmptyUpdateRequest = new()
If &UpdateRequest.NotaFiscalSerie = &EmptyUpdateRequest.NotaFiscalSerie
    ...
EndIf
```

A comparação contra instância vazia dispensa ramificar por tipo de dado e vale para qualquer tipo de membro, sem depender do vocabulário de `DataType` do plano.

**Limitação assumida:** membro obrigatório cujo valor legítimo seja igual ao default do tipo — numérico `0`, caractere vazio, data nula — é recusado com `400`. Para os cenários validados por HTTP o comportamento observável é idêntico ao da validação por presença, porque membro ausente também chega com o valor default. A mensagem de erro passou a ser `Required JSON member(s) missing or empty: %1.`, refletindo a semântica real.

Como consequência, os textos do wizard (aba `Obrigatórios`, resumo de decisões e endpoints) e as mensagens de Output `B033` e `B037` deixaram de declarar `Required` como presença de membro JSON. `B033` e `B037` receberam nota de revisão apontando para este documento, preservando as evidências originais daquelas frentes.

A sonda do evento `Before` usou variáveis temporárias `&ProbeHttpRequest` e `&ProbeBody` no API Object. Elas devem ser removidas da KB após a sonda: o preflight compara o conjunto de variáveis do API Object por igualdade exata e, com variáveis extras, trata o objeto como não gerenciado.

## Validação Runtime HTTP

Validação executada em 2026-08-02:

- .NET Framework/SQL Server: token OAuth, List 200, Get inexistente 404, Create 201, Get do criado 200, Update 200, Get atualizado 200, Update parcial sem membro obrigatório 400, Update em ID inexistente 404, List paginado e List filtrado por ID/número.
- .NET/PostgreSQL: antes da recriação do banco, Create retornou 500 por erro físico do datastore (`relaçao "notafiscal" não existe`), reproduzido também pela tela da Transaction; após recriar o banco e recadastrar o usuário/cliente GAM, token OAuth v2.0 passou e a bateria List 200, Create 201, Get 200/404, Update 200/404, validação 400 e List filtrado passou.
- A forma JSON com múltiplos `out` foi confirmada como envelope contendo `GetResponse`/`CreateResponse`/`UpdateResponse` e `ErrorResponse`.
- `Location` de Create segue pendente de confirmação nativa simples no GeneXus/API Object.

Revalidação executada em 2026-08-03 no .NET/PostgreSQL, após a troca para validação por valor default, com wizard reaplicado e Build All sem `spc0087`:

| Caso | Esperado | Obtido |
| --- | --- | --- |
| `Create` completo | 201 | 201, `NotaFiscalId=4` |
| `Get` do criado | 200 | 200 |
| `Update` completo | 200 | 200 — caso que respondia 400 indevido na tentativa `Properties` |
| `Update` sem `NotaFiscalSerie` | 400 | 400, `Required JSON member(s) missing or empty: NotaFiscalSerie.` |
| `Update` com `NotaFiscalSerie` vazia | 400 | 400, mesma mensagem |
| `Update` em ID inexistente | 404 | 404, `not_found` |
| `Get` inexistente | 404 | 404, `not_found` |
| `Get` do atualizado | 200 | 200, refletindo o valor gravado |
| `List` filtrado | 200 | 200, `TotalCount=1` |

A mesma bateria foi executada em 2026-08-03 no .NET Framework/SQL Server, com os nove casos aprovados: `Create` 201, `Get` 200, `Update` completo 200, `Update` sem membro obrigatório 400, `Update` com membro obrigatório vazio 400, `Update` e `Get` em ID inexistente 404, `Get` do atualizado 200 e `List` filtrado 200 com `TotalCount=1`. Essa execução exigiu antes a correção do verbo `PUT` no IIS descrita adiante, e foi repetida depois de um Build All completo para comprovar que a correção sobrevive ao rebuild.

#### Verbo PUT bloqueado pelo IIS no ambiente .NET Framework

A primeira execução da bateria no .NET Framework/SQL Server em 2026-08-03 devolveu `404` do IIS em **todos** os `PUT`, com página de erro apontando `Módulo=IIS Web Core`, `Notificação=MapRequestHandler`, `Manipulador=StaticFile` e caminho físico `...\Web\apiNotaFiscal\notafiscal\12`. Nenhum código GeneXus executava; `GET` e `POST` na mesma rota funcionavam normalmente.

Causa isolada em `applicationHost.config`: o handler `ExtensionlessUrlHandler-Integrated-4.0`, que atende URLs sem extensão, vem com `verb="GET,HEAD,POST,DEBUG"` por padrão no IIS. `PUT` não está na lista, então a requisição cai no handler de arquivo estático. Dois testes confirmaram o isolamento: `PUT` numa rota reescrita para `.aspx` chegou na aplicação e recebeu resposta do GAM, enquanto `PUT` nas rotas REST alternativas (`/rest/...` e `..._services.svc/rest/...`) também devolveu `404` do IIS.

O código gerado estava correto: `[WebInvoke(Method="PUT", UriTemplate="/apiNotaFiscal/notafiscal/{notafiscalid}")]` é o esperado. A correção era de ambiente.

**A primeira tentativa de correção não é durável e não deve ser repetida.** Acrescentar `<remove>` + `<add>` do handler à seção `<handlers>` do `web.config` do aplicativo gerado em `NETFrameworkSQLServer004\web` faz o `PUT` funcionar imediatamente, mas o Build All executa a etapa `Web config update`, que regenera a seção e descarta o acréscimo. Comprovado por medição: após o rebuild o arquivo não continha mais nenhuma ocorrência de `ExtensionlessUrlHandler`, e o `PUT` voltou a devolver `404` HTML do IIS.

**A correção durável é no `applicationHost.config`**, feita pelo IIS Manager executado como administrador, no **nó do servidor** — não no site nem na aplicação, porque nesses níveis a alteração é gravada de volta no `web.config` gerado. Caminho: nó do servidor → `Mapeamentos de Manipulador` → `ExtensionlessUrlHandler-Integrated-4.0` → `Restrições da Solicitação…` → aba `Verbos` → `Um dos seguintes verbos`, resultando em:

```xml
<add name="ExtensionlessUrlHandler-Integrated-4.0" path="*." verb="GET,HEAD,POST,DEBUG,PUT,DELETE,PATCH" type="System.Web.Handlers.TransferRequestHandler" resourceType="Unspecified" requireAccess="Script" preCondition="integratedMode,runtimeVersionv4.0" responseBufferLimit="0" />
```

`DELETE` e `PATCH` foram incluídos antecipando serviços futuros.

Riscos avaliados antes da mudança, medidos na máquina de desenvolvimento usada: `WebDAVModule` não registrado e authoring do WebDAV desabilitado — afastando o risco de `PUT` virar canal de gravação de arquivo; um único site no IIS (`Default Web Site`), com 15 aplicações hospedadas. O efeito colateral aceito é que essas 15 aplicações deixam de contar com o filtro implícito do IIS para `PUT`, `DELETE` e `PATCH`; autenticação e autorização de cada aplicação continuam valendo, mas comportamento inesperado com esses verbos em outra aplicação da mesma máquina deve considerar esta mudança como primeira hipótese.

Durabilidade comprovada por medição: após a correção no `applicationHost.config` e um Build All completo no ambiente .NET Framework, a bateria de nove casos foi reexecutada e passou integralmente, com o `web.config` gerado sem nenhuma linha de `ExtensionlessUrlHandler`.

Ressalva registrada sobre a evidência anterior: a linha de 2026-08-02 registrada acima afirma `Update 200` e `Update parcial 400` no .NET Framework/SQL Server. Com o handler no default do IIS isso não se reproduz, porque nenhum `PUT` alcança a aplicação. Essa linha permanece preservada como registro histórico, mas **não deve ser tratada como validação confiável do `PUT` naquele ambiente**; a validação confiável é a de 2026-08-03, posterior à correção do handler.
