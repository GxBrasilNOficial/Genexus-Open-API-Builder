# B071-B073/B079 - Get, Create, Update e Status HTTP

## Objetivo

Completar a primeira versão runtime de `Get`, `Create` e `Update`, além de alinhar o contrato HTTP de `List`, sobre os objetos já gerados por B040-B046, B050-B053, B054, B055 e B070, preservando o trio API Object/Procedure/SDT e preparando uma única validação manual na IDE.

## Implementação Local

A etapa acionada no wizard como conclusão REST de Get/Create/Update passa a:

- regravar `proc<Transacao>_API_Get` com chave simples ou composta, `GetResponse`, `ErrorResponse` e `RestStatusCode`;
- regravar `proc<Transacao>_API_Create` via Business Component com `CreateResponse`, `ErrorResponse` e `RestStatusCode`;
- regravar `proc<Transacao>_API_Update` via Business Component com chave simples ou composta, `UpdateResponse`, `ErrorResponse` e `RestStatusCode`;
- regravar `proc<Transacao>_API_List` com `ErrorResponse` e `RestStatusCode` para falhas de paginação e filtros, preservando paginação, filtros, ordenação e resposta paginada;
- sincronizar o API Object com `List(in:&ApiPage, in:&ApiPageSize, ..., out:&ListResponse, out:&ErrorResponse)`, `Get(in:&PK..., out:&GetResponse, out:&ErrorResponse)`, `Create(in:&CreateRequest, out:&CreateResponse, out:&ErrorResponse)` e `Update(in:&PK..., in:&UpdateRequest, out:&UpdateResponse, out:&ErrorResponse)`;
- expor `ErrorResponse` como saída pública dos serviços e manter `RestStatusCode` como variável interna do API Object usada na chamada às Procedures;
- gravar Events no API Object para aplicar `&RestCode = &RestStatusCode` nos eventos `List.After`, `Get.After`, `Create.After` e `Update.After`;
- validar membros obrigatórios em `Create` e `Update` comparando cada membro recebido com o valor default do mesmo membro em uma instância vazia do próprio SDT de request, sem comando C# embutido;
- preservar o contrato B070 quando `List` for aplicado no mesmo fluxo, mantendo todos os serviços no mesmo `ServiceGroupSource`.

Status planejados:

- `Get` encontrado: `200`;
- `Get` inexistente: `404` com `ErrorResponse.Code = "not_found"`;
- `Create` bem-sucedido: `201` com cabeçalho `Location` apontando para a URL do recurso recém-criado (ex: `Location: /notafiscal/123` para chave simples ou `Location: /notafiscal/1/123` para chave composta);
- `Create` rejeitado pelo Business Component: `422` com `ErrorResponse.Code = "validation_error"`;
- `Update` encontrado e salvo: `200`;
- `Update` inexistente: `404` com `ErrorResponse.Code = "not_found"`;
- `Update` rejeitado pelo Business Component: `422` com `ErrorResponse.Code = "validation_error"`.

`Location` de `Create` foi implementado via suporte nativo do GeneXus 18 (`&HttpResponse.AddHeader(!"Location", ...)`), montando a URL relativa do endpoint `Get` do recurso a partir do `RestPath` e da(s) chave(s) primária(s) obtida(s) no Business Component após a gravação (`&bc.Save()`). A solução é 100% nativa GeneXus, sem exigir bloco C# nem `External Object`.

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

`ApplyList` reconhece esse estado e, quando precisa sincronizar List, preserva `Get`, `Create`, `Update`, as anotações REST explícitas de `Create`/`Update`, `RestPath` com parâmetros GeneXus no formato `{&Chave}`, `ErrorResponse` como saída pública, `RestStatusCode` interno e Events. A `List` atual também usa `ErrorResponse`/`RestStatusCode` para validações de paginação e filtros, sem `msg()`; o contrato histórico sem essas saídas continua aceito somente para migração.

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
- `ErrorResponse` passou a ser saída pública em `List`, `Get`, `Create` e `Update`, deixando de ser apenas argumento interno Procedure/API Object;
- reexecução de Procedures B079 passou a usar equivalência canônica restrita do Source, bloqueando código extra.
- a geração atual deixou de preencher `ErrorResponse.Errors[]` por `ErrorItem` nested, depois que a IDE rejeitou a validação da Procedure com item de subestrutura SDT; `ErrorResponse.Code` e `ErrorResponse.Message` continuam compondo o corpo de erro público, sem usar `msg()` para transportar mensagens do Business Component.
- o estado intermediário com `ErrorItem` tipado como `sdt_API_ErrorResponse.Error`, `sdt_API_ErrorResponse.Errors`, `sdt_API_ErrorResponse.ErrorsItem` ou `sdt:sdt_API_ErrorResponse.Errors` é aceito apenas como migração conservadora para regravar a Procedure para o contrato top-level atual.
- o estado intermediário que validava presença de membros obrigatórios por `csharp`/`IsDirty` do SDT é aceito apenas como migração conservadora; a tentativa intermediária com `HttpRequest.ToString()` + `Properties` também é aceita apenas para reencontro, pois o body bruto não ficou disponível dentro da Procedure chamada pelo API Object.

A geração sem `ErrorItem` nested foi validada mecanicamente no repositório e reexecutada na IDE até gravar Procedures, API Object, List e metadata. O preenchimento de `Errors[]` permanece pendente de prova específica sobre a tipagem real de item de subestrutura SDT no SDK/GeneXus.

**Remissão — 2026-08-24.** A pendência acima ficou datada nesta evidência de B071–B079; `B102` fechou o corpo de erro com `Messages[]` tipado por `sdt_API_ErrorMessage`, sem reintroduzir `Errors[]`. Ver documento 27 e `Docs/Implementation/2026-08-24-B102-EXPERIMENTO-E-GATE-HTTP.md`.

Atualização de 2026-08-03: a subestrutura `Errors` foi retirada também do SDT compartilhado `sdt_API_ErrorResponse`, porque continuava aparecendo no contrato OpenAPI como array que nunca é preenchido. Ver `2026-08-03-CONTRATO-OPENAPI-GAPS.md`. O reconhecimento das Procedures com `ErrorItem` descrito acima permanece ativo, e é justamente ele que permite regravar uma Procedure antiga para o contrato atual depois da mudança do SDT.

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
- `Location` de `Create` foi implementado com suporte nativo via `&HttpResponse.AddHeader(!"Location", ...)`.

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

### Validação HTTP do Cabeçalho Location no serviço Create

Validação de emissão em runtime executada em 2026-08-04 em ambos os ambientes com autenticação Bearer Token GAM:

| Ambiente Gerado | Status HTTP | Cabeçalho `Location` Obtido | Corpo da Resposta |
| --- | --- | --- | --- |
| **.NET Framework / SQL Server** | `201 Created` | `Location: /notafiscal/19` | `{"CreateResponse":{"NotaFiscalId":"19","NotaFiscalSerie":"FW","NotaFiscalNumero":"9001"},"ErrorResponse":{"Code":"","Message":""}}` |
| **.NET Core / PostgreSQL** | `201 Created` | `Location: /notafiscal/5` | `{"CreateResponse":{"NotaFiscalId":"5","NotaFiscalSerie":"NET","NotaFiscalNumero":"9002"},"ErrorResponse":{"Code":"","Message":""}}` |

Evidência capturada por requisição HTTP real disparada contra as duas instalações geradas. A emissão do cabeçalho `Location` foi confirmada em ambos os geradores.

#### Validação HTTP do Cabeçalho Location com Chave Primária Composta (Transaction `Teste`)

##### Reorganização do Modelo de Dados da KB
A Transaction de testes funcionais `Teste` teve seu modelo reorganizado para possuir chave primária composta de três partes: `TesteId` (Numeric), `TesteDate` (Date) e `TesteCodigo` (VarChar 20). Houve reestruturação física das tabelas e execução de Database Reorganization nos dois ambientes (.NET Framework em IIS e .NET / PostgreSQL atrás de IIS).

##### Correção de geração (2026-08-06)
A gravação em 1 clique do Create falhava na validação GeneXus quando o `Location` usava `URLEncode({membro}.Trim())` em uma expressão única. O gerador passou a montar `&LocationUrl` segmento a segmento e a emitir `URLEncode(Trim({membro}))`. Build All nos dois environments regenerou:

- `C:\KBs\wsEducacaoSpTeste\NETFrameworkSQLServer004\web\procteste_api_create.cs` — 11815 bytes, mtime `2026-08-06 12:29:02`, com `GXUtil.UrlEncode(StringUtil.Trim(...))` e `AppendHeader("Location", AV12LocationUrl)`
- `C:\KBs\wsEducacaoSpTeste\NETPostgreSQL155\web\procteste_api_create.cs` — 11784 bytes, mtime `2026-08-06 12:30:37`, mesmo padrão

##### Medição de URLEncode na Chave de Texto (`TesteCodigo`) — captura 2026-08-06

Medição HTTP real com Bearer GAM, `TesteCodigo` em `CreateFields`, navegando o cabeçalho `Location` do `POST` **sem reescrever a URL**. Captura bruta local (não versionada): `Temp/location-matrix-2026-08-06.json` (mtime `2026-08-06 12:36:09`).

A matriz publicada em 2026-08-05 que afirmava `COD%2001` com GET `200` nos dois geradores e GET `200` para `%2F` no Kestrel **fica superada** por esta captura; não reproduzir aquelas afirmações.

| Caso (`TesteCodigo`) | Ambiente | POST | `Location` emitido | GET no `Location` (sem reescrita) | Observação capturada |
| --- | --- | --- | --- | --- | --- |
| espaço (`COD 01`) | .NET Framework | `201` | `/teste/9101/2026-08-06/COD+01` | `404` JSON `not_found` | `URLEncode` emitiu `+`. Diagnóstico separado: GET com `%20` no mesmo recurso → `200` e corpo com `TesteCodigo":"COD 01"`. |
| espaço (`COD 01`) | .NET / PostgreSQL (IIS) | `201` | `/teste/9201/2026-08-06/COD+01` | `404.11` HTML IIS (`RequestFilteringModule`, double escaping) | O `+` no path foi barrado pelo IIS antes da app. Diagnóstico: GET com `%20` → `200`. |
| acento (`AÇÃO`) | .NET Framework | `201` | `/teste/9102/2026-08-06/A%c3%87%c3%83O` | `200` | Navegável. Corpo GET com `TesteId":"9102"` e `TesteCodigo":"AÇÃO"`. |
| acento (`AÇÃO`) | .NET / PostgreSQL | `201` | `/teste/9202/2026-08-06/A%C3%87%C3%83O` | `200` | Navegável. Corpo GET com `TesteId":"9202"` e `TesteCodigo":"AÇÃO"`. |
| barra (`COD/01`) | .NET Framework | `201` | `/teste/9103/2026-08-06/COD%2f01` | `404` JSON GeneXus `not_found` | A requisição **chegou na aplicação** (não é página HTML do IIS). Create gravou `TesteCodigo` `COD/01`. |
| barra (`COD/01`) | .NET / PostgreSQL | `201` | `/teste/9203/2026-08-06/COD%2F01` | `404` corpo vazio | Divergência de ambiente frente ao Framework (lá há JSON GeneXus; aqui corpo vazio). |

*Observação técnica de navegabilidade (2026-08-06):*

1. Datas no `Location` saem em ISO `YYYY-MM-DD` via `Year`/`Month`/`Day` no fonte GeneXus (`DateTimeUtil.Year/...` no C# gerado).
2. Espaço: `URLEncode` nativo produz `+` (estilo form-urlencoded). Em segmento de path, `+` **não** é equivalente a espaço; o `Location` assim emitido **não é navegável** nos dois ambientes medidos. `%20` recupera o recurso.
3. Acento UTF-8 em percent-encoding: navegável nos dois.
4. Barra `%2F`: `404` nos dois; no Framework a app GeneXus responde `not_found` (não é bloqueio IIS pré-app nesta medição); no .NET/PostgreSQL o `404` veio sem corpo JSON. Não generalizar como “IIS sempre bloqueia `%2F`” nem como “Kestrel sempre aceita”.

**Esta matriz de espaço (`COD+01` / GET `404`) fica superada** pela captura pós-correção abaixo (`Temp/location-matrix-2026-08-06-pos-correcao.json`).

##### Correção de path-encoding e recusa de barra — captura HTTP 2026-08-06 (pós-correção)

Após instalar a DLL com `StrReplace(URLEncode(Trim(...)), !"+" , !"%20")` e a guarda de `/` (`Rollback` + `400`), Build All nos dois environments regenerou `procteste_api_create.cs` com `StringUtil.StringReplace(GXUtil.UrlEncode(...), "+", "%20")`.

Medição HTTP real com Bearer GAM, `TesteCodigo` em CreateFields, navegando o `Location` do POST **sem reescrever a URL**. Captura bruta local (não versionada): `Temp/location-matrix-2026-08-06-pos-correcao.json` (mtime `2026-08-06 23:18:44`).

| Caso (`TesteCodigo`) | Ambiente | POST | `Location` emitido | GET no `Location` (sem reescrita) | Observação capturada |
| --- | --- | --- | --- | --- | --- |
| espaço (`COD 01`) | .NET Framework | `201` | `/teste/9311/2026-08-06/COD%2001` | `200` | `%20` no path; corpo GET com `TesteCodigo":"COD 01"`. |
| espaço (`COD 01`) | .NET / PostgreSQL | `201` | `/teste/9411/2026-08-06/COD%2001` | `200` | Idem; sem `404.11` do IIS (não há `+`). |
| acento (`AÇÃO`) | .NET Framework | `201` | `/teste/9312/2026-08-06/A%c3%87%c3%83O` | `200` | Sem regressão. |
| acento (`AÇÃO`) | .NET / PostgreSQL | `201` | `/teste/9412/2026-08-06/A%C3%87%C3%83O` | `200` | Sem regressão. |
| simples (`COD01`) | .NET Framework | `201` | `/teste/9313/2026-08-06/COD01` | `200` | Sem regressão. |
| simples (`COD01`) | .NET / PostgreSQL | `201` | `/teste/9413/2026-08-06/COD01` | `200` | Sem regressão. |
| barra (`COD/01`) | .NET Framework | `400` | (ausente) | n/a (recurso não gravado; GET de tentativa `9314` → `404 not_found`) | `ErrorResponse.Code=invalid_request`; mensagem sobre `/` no path Location. |
| barra (`COD/01`) | .NET / PostgreSQL | `400` | (ausente) | n/a (GET de tentativa `9414` → `404` corpo vazio) | Mesmo `invalid_request`. |

*Conclusão da correção:*

1. Espaço no `Location` passa a `%20` e o GET no header navega com `200` nos dois geradores.
2. Acento e valor simples sem regressão.
3. Barra: Create recusa com `400` e `Rollback` (não grava; não emite `Location`). Limitação de endereçamento por path permanece para registros históricos que já tenham `/`.

##### Create sem partes da PK (Passo 4) — captura 2026-08-06

Com o contrato Passo 4 (PK não autonumerada opcional no CreateRequest), POST autenticado só com `TesteDesc` em `CreateRequest`:

| Ambiente | POST | `Location` | GET no `Location` | Corpo Create |
| --- | --- | --- | --- | --- |
| .NET Framework / SQL Server | `201` | `/teste/1/2026-08-06/1` | `200` | `TesteId=1`, `TesteDate=2026-08-06`, `TesteCodigo=1` |
| .NET / PostgreSQL | `201` | `/teste/1/2026-08-06/1` | `200` | Idem |

Pré-condição nas rules da Transaction `Teste`: preenchimento com `on BeforeInsert` (não apenas `if insert`). Com `if insert`, o mesmo POST devolveu `201` com `TesteId=0` e `TesteCodigo` vazio e o GET no `Location` falhou. Script local (não versionado): `Temp/Invoke-Passo4CreateSmoke.ps1`.

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

##### Revalidação após remoção e recriação da API `NotaFiscal` — captura 2026-08-13

Para validar o contrato atualmente gerado pela extensão, o comando B086 removeu da Transaction `NotaFiscal` os 11 objetos próprios de `apiNotaFiscal` (API Object, quatro Procedures, cinco SDTs próprios e metadata); o Folder `NotaFiscalOpenApi` preexistente foi preservado. O Wizard recriou a API com `SecurityLevel='Authorization'`, `Create=6`, `Update=6`, `Response=7`, `ListFilters=7`, `CreateRequired=0`, `UpdateRequired=6`, `Created=11`, `Updated=2`, `Blocked=0` e os dois avisos esperados de fallback de descrições em inglês e reuso do Folder.

O `Build All` foi executado nos dois environments e terminou com `Success: Build All`, gerando `apiNotaFiscal`, as quatro Procedures, os SDTs REST, a documentação REST, a compilação do DeveloperMenu e as permissões GAM. Os logs também continham warnings preexistentes de outros objetos da KB, sem erro de especificação ou geração da API.

A bateria foi executada com OAuth GAM usando o arquivo local ignorado `Temp/wsEducacaoSpTeste-local-test-environments.md`; os valores de credencial não fazem parte desta evidência versionada.

| Caso | .NET Framework / SQL Server | .NET / PostgreSQL |
| --- | --- | --- |
| Token OAuth | `200` | `200` |
| List sem token | `401` | `401` |
| List autenticado | `200` | `200` |
| Get de registro existente | `200` | `200` |
| Create | `201`, `Location=/notafiscal/21` | `201`, `Location=/notafiscal/8` |
| Get do registro criado | `200` | `200` |
| Update do registro criado | `200` | `200` |
| Get após Update | `200` | `200` |
| List filtrado pelo ID criado | `200`, `TotalCount=1` | `200`, `TotalCount=1` |
| Get inexistente | `404`, `not_found` | `404`, `not_found` |
| Update inválido | `400`, `invalid_request` | `400`, `invalid_request` |
| Update inexistente | `404`, `not_found` | `404`, `not_found` |

Os registros de teste `21` e `8` permanecem nas respectivas bases: a API atual não possui serviço DELETE e nenhuma limpeza direta no banco foi executada. Esta captura fecha a validação runtime HTTP do contrato recriado; a bateria específica do `NoAccept(EmployeeAddedDate)` permanece coberta separadamente pelos builds U13/U15.

##### Revalidação do contrato atual e diagnóstico do artefato PostgreSQL — captura HTTP 2026-09-10

Após a instalação da DLL atual, o Wizard foi aplicado na `NotaFiscal` com `List`, `Get`, `Create`, `Update` e `Delete` selecionados e com `Completar listagem` e `Completar REST via Business Component` ativos. O relatório final registrou `Updated=15`, `Blocked=0`, API Object escrito como `List`, metadata atualizada e somente os dois avisos conhecidos de fallback de descrições e reuso do Folder. O `Build All` terminou com `Success: Build All` em `NETPostgreSQL155` e `NETFrameworkSQLServer004`; no Framework houve apenas o warning preexistente de cópia de `FBiTextSharp.dll` para o mesmo diretório.

A DLL usada nesta rodada foi `Src/Extension/bin/Release/net471/GenexusOpenApiBuilder.Extension.dll`, SHA-256 `3D7517FA2999EC5956E7B4BB4B47B8EFE5028052EA8DB1EE08115D583C47DBA6`.

A validação HTTP do repasse de múltiplos erros de payload também passou nos dois ambientes: `400`, `ErrorCode=attribute_limit_exceeded`, `MessagesCount=2`, com as mensagens de `NotaFiscalSerie` e `NotaFiscalObs2` no corpo, sem gravação.

O teste de `Update` foi executado com registro temporário e limpeza posterior:

| Caso | .NET Framework / SQL Server | .NET / PostgreSQL |
| --- | --- | --- |
| Criação de preparação | `201`, `CreatedId=27` | `201`, `CreatedId=15` |
| `PUT` válido | `200`; `GET` confirmou a alteração | `200`; `GET` confirmou a alteração |
| `PUT` com dois limites inválidos | `400`, `attribute_limit_exceeded`, 2 mensagens; `GET` confirmou ausência de alteração | `400`, `attribute_limit_exceeded`, 2 mensagens; `GET` confirmou ausência de alteração |
| `PUT` sem membro obrigatório | `400`, `invalid_request` | `400`, `invalid_request` |
| `PUT` em ID inexistente | `404`, `not_found` | `404`, `not_found` |
| Limpeza | `DELETE=200` | `DELETE=200` |

Durante a preparação desse teste, o .NET / PostgreSQL chegou a responder HTTP 500 com `TypeLoadException` para `GeneXus.Programs.SdtsdtTeste_API_CreateRequest_RESTInterface` em `GeneXus.Programs.Common`. O diagnóstico confirmado foi um `apiteste.dll` antigo publicado embora o objeto `apiteste` não existisse na KB, deixando assemblies incompatíveis após um Build All incremental incompleto. A criação do objeto na KB e um novo Build All corrigiram o ambiente; a execução acima passou em ambos os ambientes. Portanto, esse incidente não foi causado pelo PostgreSQL, pelo IIS, pelo navegador nem por `_app_offline.htm`.

##### Primeiro smoke HTTP de `List` após os Build All — captura 2026-09-10

Como primeiro teste HTTP da rodada seguinte, foi executado `GET /notafiscal` sem credencial e com token OAuth nos dois environments, sem alterar dados. O token foi obtido nos dois casos; sem token, ambos responderam `401` em JSON; com token, ambos iniciaram a operação e responderam `200` em JSON.

| Verificação | .NET Framework / SQL Server | .NET / PostgreSQL |
| --- | --- | --- |
| Sem token | `401`, mensagem de cabeçalho `Authorization` ausente | `401`, mesma mensagem |
| Com token | `200`; 22 itens; página 1, tamanho 50, total 22, total de páginas 1 | `200`; 10 itens; página 1, tamanho 50, total 10, total de páginas 1 |
| Envelope efetivamente recebido | `ListResponse` + `ErrorResponse` no nível raiz; paginação e filtros dentro de `ListResponse` | `Items` + `Pagination` + `AppliedFilters` no nível raiz; sem propriedade `ErrorResponse` |

O resultado não é aceito como contrato equivalente entre os environments. A leitura dos dois `apiNotaFiscal.yaml` gerados confirmou que ambos declaram `ListOutput` com as propriedades `ListResponse` e `ErrorResponse`; o Framework entregou esse formato, enquanto o PostgreSQL devolveu somente o conteúdo interno de `ListResponse`. A diferença é de serialização do runtime gerado e não indica falha de autenticação, de inicialização ou específica do banco PostgreSQL. O `List` fica pendente de classificação/correção do envelope antes de ser considerado validado de forma uniforme; este achado também não substitui os cenários de Sync ainda pendentes na validação F1 da IDE.

##### Tentativa de validação HTTP do `List` da `Laudo` — captura 2026-09-11

Após a aplicação da F1 na Transaction `Laudo` e o Build All nos dois
environments, foi repetida a validação HTTP do endpoint
`GET /apiLaudo/laudo`, sem alterar dados. A rota correta foi confirmada nos
dois environments locais:

| Verificação | .NET Framework / SQL Server | .NET / PostgreSQL |
| --- | --- | --- |
| `GET /apiLaudo/laudo` sem token | `401` JSON | `401` JSON |
| Obtenção do token OAuth/GAM | `200` | `200` |
| `GET /apiLaudo/laudo` com token | `403`, `code=139`, `Não autorizado: acesso negado.` | `403`, `code=139`, `Não autorizado: acesso negado.` |

Os artefatos gerados confirmam que o serviço `List` está sob
`GAMSecurityLevel.SecurityHigh` e usa a permissão
`apilaudo_Services_List`. Assim, a requisição autenticada chegou ao runtime,
mas o principal de teste não possui a permissão específica dessa API. Como o
HTTP `200` não foi alcançado, não foram medidos itens, `AppliedFilters`,
`Pagination`, filtro válido, filtro sem resultado ou página 2. B076 não é
marcado como aprovado por esta tentativa; não houve gravação nem alteração na
KB. Para continuar, é necessário conceder no GAM a permissão
`apilaudo_Services_List` ao principal de teste nos dois environments e repetir
as chamadas somente de leitura.

##### Reteste após permissão GAM — primeira tentativa — captura 2026-09-11

No primeiro environment, o usuário concedeu `apilaudo_Services_List` diretamente
ao principal `goab_api_teste` na aplicação `wsEducacaoSpTeste`. A permissão foi
confirmada no GAM como `Permitir`, com `Herdado=0`. A nova chamada HTTP confirmou
que o bloqueio de autorização deixou de ocorrer:

| Verificação | .NET Framework / SQL Server |
| --- | --- |
| Sem token | `401` |
| Token OAuth/GAM | `200` |
| `GET /apiLaudo/laudo` autenticado | `200`, mas corpo `{}` |
| Filtro sem resultado | `200`, mas corpo `{}` |
| Página 2 com tamanho 1 | `200`, mas corpo `{}` |

O corpo vazio não permite medir `Items`, `Pagination`, `AppliedFilters` ou
filtro válido. A leitura do artefato gerado fechou a causa: o
`apiLaudo.yaml`/`OpenApi3/apiLaudo.json` não contém parâmetros nem schemas; e o
`apilaudo.cs` declara `gxep_list()` como `void`, executando a Procedure sem
devolver `ListResponse` ou `ErrorResponse`. A Procedure
`proclaudo_api_list.cs` monta o `ListResponse` corretamente, mas o API Object
persistido continua no formato B054 `List() => procLaudo_API_List()`, sem os
parâmetros e saídas do contrato B070. O registro detalhado está em
`Docs/Implementation/2026-09-11-B076-LIST-HTTP-POS-PERMISSAO.md`.

Assim, naquela primeira tentativa, a autenticação e a autorização do primeiro
environment passaram, mas B076 continuava sem aceite funcional. O próximo passo
era salvar o API Object com
o contrato de List parametrizado, gerar novamente os artefatos com `Build All`
e só então repetir os cenários de leitura. O segundo environment não foi
alterado no GAM nesta rodada.

##### Reteste do `List` da `Laudo` após contrato B070 — captura 2026-09-11

O Wizard foi reaplicado com o contrato parametrizado do `List` no API Object e
houve `Build All` nos dois environments. O artefato gerado passou a expor
`gxep_list` com página, tamanho, filtro, `ListResponse` e `ErrorResponse`.

| Verificação | .NET Framework / SQL Server | .NET / PostgreSQL |
| --- | --- | --- |
| Sem token | `401` | `401` |
| Token OAuth/GAM | `200` | `200` |
| List autenticado sem filtro | `200`; 1 item (`753159`); total 1; 1 página | `200`; 2 itens (`B076-001`, `B076-002`); total 2; 1 página |
| Filtro válido | `753159`: 1 item | `B076-001`: 1 item |
| Página 1, tamanho 1 | 1 item; total 1; 1 página | 1 item; total 2; 2 páginas |
| Página 2, tamanho 1 | sem item; total 1; 1 página | `B076-002`; total 2; 2 páginas |
| Filtro sem resultado | `200`; total 0 | `200`; total 0 |

O `List` passou funcionalmente nos dois environments. A forma do corpo ainda
diverge: o Framework entrega `ListResponse` + `ErrorResponse` no nível raiz,
enquanto o PostgreSQL entrega `Items` + `Pagination` + `AppliedFilters` no
nível raiz. Os dois YAML declaram `ListOutput` com o wrapper; portanto, a
equivalência do contrato permanece pendente no `B120`. Os avisos `spc0024` de
`Get`, `Create` e `Update` são uma questão separada da validação do `List`.
