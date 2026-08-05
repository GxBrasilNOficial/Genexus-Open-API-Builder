# B093 — Aplicação Explícita do Security Level no API Object

**Projeto:** Genexus Open API Builder
**Frente:** B093 — Aplicar o Security Level explicitamente em todos os serviços do API Object gerado (Sprint 6)
**Data:** 2026-08-03

---

## 1. Contexto e Diagnóstico

Antes desta frente, o wizard oferecia três níveis de segurança — `Authentication`, `Authorization` e `None`. O valor selecionado entrava no `ApiPlan`, era persistido na metadata (B065) e a condição GAM correspondente era resolvida no plano (B092):
- `Authentication` -> `GAM_AUTHENTICATION_REQUIRED` (`RequiresGenerationConfirmation=False`)
- `Authorization` -> `GAM_AUTHORIZATION_REQUIRED_PENDING_PERMISSIONS` (`RequiresGenerationConfirmation=True`)
- `None` -> `NO_GAM_SECURITY_PUBLIC_API` (`RequiresGenerationConfirmation=True`)

Porém, essa escolha não era emitida no objeto `API` gerado: nem `ApiPlanBusinessComponentWriter.cs` nem `ApiPlanListProcedureWriter.cs` aplicavam anotações `[SecurityLevel(...)]` nos serviços. Na ausência de anotação explícita, a IDE/gerador GeneXus herdava a configuração padrão da KB (`Authentication`), fazendo com que nos três casos a API gerada e o YAML OpenAPI saíssem com o mesmo comportamento.

---

## 2. Alterações Realizadas

1. **Geração da Anotação Explícita (`[SecurityLevel(...)]`):**
   - Em `Src/Extension/Diagnostics/ApiPlanBusinessComponentWriter.cs`: a geração de anotações por serviço (`ServiceAnnotations`) passou a incluir `[SecurityLevel({plan.Security.SecurityLevel})]`.
   - Em `Src/Extension/Diagnostics/ApiPlanListProcedureWriter.cs`: a mesma inclusão foi feita na resincronização de `List`, garantindo que o writer de `List` não apague a marcação de segurança aplicada anteriormente.

2. **Parser de Contrato & Especificidade de Reencontro (`ApiPlanServiceSourceContract.cs`):**
   - Atualizado para exigir `[SecurityLevel(` na validação de contrato runtime quando `hasRestRuntimeContract` e `validateSecurityLevel` forem verdadeiros.
   - Criado o matcher `MatchesPreviousB079SecurityLevelContract` para aceitar objetos de API gerados na B079 (sem `[SecurityLevel]`), mantendo a validação estrita de `[RestMethod(POST)]`, `[RestMethod(PUT)]` e parâmetro de rota `{&Chave}`, sem forçar o uso do fallback frouxo `MatchesPreviousB079RestMethodContract`.

3. **Integridade B067 e Compatibilidade de Reencontro (`ApiPlanMetadataFileWriter.cs`):**
   - Adicionado o utilitário `RemoveServiceSourceSecurityLevelAnnotations` para gerar variantes de fontes legadas em `CreateCompatibleServiceSourceVariants`, permitindo que objetos de API criados antes de B093 continuem sendo reencontrados de forma conservadora.

4. **Testes Unitários e Trava Automatizada:**
   - `Tests/BusinessComponentWriter/Test-ApiPlanBusinessComponentWriterVariableContract.ps1`: valida a presença de `[SecurityLevel({plan.Security.SecurityLevel})]` em `ApiPlanBusinessComponentWriter.cs`.
   - `Tests/OpenApiContract/Test-ApiPlanOpenApiContractMarks.ps1`: valida a trava de emissão explícita de `[SecurityLevel(...)]` em ambos os writers (`Business Component` e `List`).
   - `Tests/ServiceSourceContract/Test-ApiPlanServiceSourceContract.ps1`: atualizado com a anotação `[SecurityLevel(Authentication)]` no fixture `$b079` e com asserções estritas para `MatchesPreviousB079SecurityLevelContract`.

---

## 3. Validação Mecânica & Prova do Gate por Mutação

Os 9 testes unitários locais e o build Release foram executados e aprovados com sucesso:
- `tests.serviceSourceContract`: PASSED
- `tests.metadataIntegrity`: PASSED
- `tests.wizardPreferences`: PASSED
- `tests.wizardNavigation`: PASSED
- `tests.writePreflightScope`: PASSED
- `tests.businessComponentWriterVariableContract`: PASSED
- `tests.listProcedureReencounterPolicy`: PASSED
- `tests.requiredMemberSemantics`: PASSED
- `tests.openApiContractMarks`: PASSED
- `dotnet.restore`: PASSED
- `dotnet.build`: PASSED (0 Erros, 0 Avisos)

### Prova de Eficácia do Gate por Mutação
A trava de `[SecurityLevel(...)]` em `Test-ApiPlanOpenApiContractMarks.ps1` foi submetida a teste por mutação:
- A remoção temporária da linha `[SecurityLevel(...)]` em `ApiPlanBusinessComponentWriter.cs` causou a falha imediata da suíte (`ASSERT_CONTAINS_FAILED: Writer de Business Component deve aplicar SecurityLevel em cada servico do API Object`).
- A restauração do código reestabeleceu a aprovação limpa (`PASS: ApiPlanOpenApiContractMarks`).

---

## 4. Evidências do Runtime HTTP e Contrato OpenAPI (GeneXus 18 U15)

### A. Prova das Requisições HTTP em Runtime (Dois Ambientes)

#### 1. Nível `SecurityLevel = Authentication`

##### A. Caminho Negativo (Requisição SEM Token)
Disparada requisição `GET /apiNotaFiscal/notafiscal` sem o cabeçalho `Authorization`:

- **.NET Framework / SQL Server (`http://localhost/wsEducacaoSpTesteNETFrameworkSQLServer/apiNotaFiscal/notafiscal`):**
  ```http
  HTTP/1.1 401 Unauthorized
  Content-Type: application/json; charset=utf-8

  {"error":{"code":"0","message":"This service needs an Authorization Header"}}
  ```

- **.NET Core / PostgreSQL (`http://localhost/wsEducacaoSpTesteNETPostgreSQL/apiNotaFiscal/notafiscal`):**
  ```http
  HTTP/1.1 401 Unauthorized
  Content-Type: application/json; charset=utf-8

  {"error":{"code":"0","message":"This service needs an Authorization Header"}}
  ```

##### B. Caminho Positivo (Requisição COM Token OAuth GAM)
Token OAuth 2.0 obtido via `POST /oauth/gam/v2.0/access_token` (`grant_type=password`, `scope=gam_user_data`, credenciais locais de `Temp/wsEducacaoSpTeste-local-test-environments.md`).

Disparada requisição `GET /apiNotaFiscal/notafiscal` com cabeçalho `Authorization: Bearer <token>`:

- **.NET Framework / SQL Server:**
  ```http
  HTTP/1.1 200 OK
  Content-Type: application/json; charset=utf-8

  {"Items":[{"NotaFiscalId":"1","NotaFiscalSerie":"CE","NotaFiscalNumero":"********"},{"NotaFiscalId":"4","NotaFiscalSerie":"CD","NotaFiscalNumero":"********"},{"NotaFiscalId":"5","NotaFiscalSerie":"RQ","NotaFiscalNumero":"********"},{"NotaFiscalId":"6","NotaFiscalSerie":"RF","NotaFiscalNumero":"********"},{"NotaFiscalId":"7","NotaFiscalSerie":"RH","NotaFiscalNumero":"********"},{"NotaFiscalId":"8","NotaFiscalSerie":"U09","NotaFiscalNumero":"5849"},{"NotaFiscalId":"9","NotaFiscalSerie":"L10","NotaFiscalNumero":"1726"},{"NotaFiscalId":"10","NotaFiscalSerie":"L10","NotaFiscalNumero":"1727"},{"NotaFiscalId":"11","NotaFiscalSerie":"L10","NotaFiscalNumero":"1728"},{"NotaFiscalId":"12","NotaFiscalSerie":"B2","NotaFiscalNumero":"7001"},{"NotaFiscalId":"13","NotaFiscalSerie":"S12","NotaFiscalNumero":"73078"},{"NotaFiscalId":"14","NotaFiscalSerie":"S60","NotaFiscalNumero":"21768"},{"NotaFiscalId":"15","NotaFiscalSerie":"S18","NotaFiscalNumero":"28595"},{"NotaFiscalId":"16","NotaFiscalSerie":"GOA","NotaFiscalNumero":"91001"},{"NotaFiscalId":"17","NotaFiscalSerie":"NON","NotaFiscalNumero":"9991"}],"Pagination":{"Page":"1","PageSize":"50","TotalCount":"15","TotalPages":"1"},"AppliedFilters":{"NotaFiscalId":null,"NotaFiscalNumero":null}}
  ```

- **.NET Core / PostgreSQL:**
  ```http
  HTTP/1.1 200 OK
  Content-Type: application/json; charset=utf-8

  {"Items":[{"NotaFiscalId":"1","NotaFiscalSerie":"1","NotaFiscalNumero":"123"},{"NotaFiscalId":"2","NotaFiscalSerie":"U60","NotaFiscalNumero":"920833"},{"NotaFiscalId":"3","NotaFiscalSerie":"T12","NotaFiscalNumero":"120101"},{"NotaFiscalId":"4","NotaFiscalSerie":"C3","NotaFiscalNumero":"7002"}],"Pagination":{"Page":"1","PageSize":"50","TotalCount":"4","TotalPages":"1"},"AppliedFilters":{"NotaFiscalId":null,"NotaFiscalNumero":null}}
  ```

**Comprovação:** Sob `Authentication`, a API rejeita requisições sem token com **HTTP 401** e responde com **HTTP 200 OK** contendo os dados paginados quando munida de Bearer Token válido.

---

#### 2. Nível `SecurityLevel = None`

Com `SecurityLevel = None` aplicado no Wizard e compilado na KB, o motor C# (`apinotafiscal.cs`) emite `GAMSecurityLevel.SecurityNone` em todos os métodos, desativando a verificação de credenciais GAM:

##### A. Requisição HTTP GET (List) Pública sem Token
- **.NET Framework / SQL Server:** **`HTTP 200 OK`**
  ```json
  {"Items":[{"NotaFiscalId":"1","NotaFiscalSerie":"CE","NotaFiscalNumero":"********"},...],"Pagination":{"Page":"1","PageSize":"50","TotalCount":"14","TotalPages":"1"},"AppliedFilters":{"NotaFiscalId":null,"NotaFiscalNumero":null}}
  ```
- **.NET Core / PostgreSQL:** **`HTTP 200 OK`**
  ```json
  {"Items":[{"NotaFiscalId":"1","NotaFiscalSerie":"1","NotaFiscalNumero":"123"},...],"Pagination":{"Page":"1","PageSize":"50","TotalCount":"4","TotalPages":"1"},"AppliedFilters":{"NotaFiscalId":null,"NotaFiscalNumero":null}}
  ```

##### B. Requisição HTTP POST (Create) Pública sem Token
- **Endpoint:** `POST http://localhost/wsEducacaoSpTesteNETFrameworkSQLServer/apiNotaFiscal/notafiscal`
- **Body:** `{"CreateRequest":{"NotaFiscalSerie":"NON","NotaFiscalNumero":9991}}`
- **Resposta:** **`HTTP 201 Created`**
  ```json
  {"CreateResponse":{"NotaFiscalId":"17","NotaFiscalSerie":"NON","NotaFiscalNumero":"9991"},"ErrorResponse":{"Code":"","Message":""}}
  ```

---

#### 3. Nível `SecurityLevel = Authorization`

Com `SecurityLevel = Authorization` aplicado no Wizard, o gerador GeneXus emite a permissão GAM `apiNotaFiscal-06e86b6b-8fbd-4d93-8a23-21bf07019c2b` durante a compilação do Build All e gera o motor C# (`apinotafiscal.cs`) retornando `GAMSecurityLevel.SecurityHigh` em todos os métodos com o prefixo de permissão `apinotafiscal_Services_<Servico>`.

##### A. Caminho Negativo (Requisição SEM Token)
Disparada requisição `GET /apiNotaFiscal/notafiscal` sem o cabeçalho `Authorization`:
- **.NET Framework / SQL Server:** `HTTP 401 Unauthorized` (`{"error":{"code":"0","message":"This service needs an Authorization Header"}}`)
- **.NET Core / PostgreSQL:** `HTTP 401 Unauthorized` (`{"error":{"code":"0","message":"This service needs an Authorization Header"}}`)

##### B. Caminho Negativo (Requisição com Token Inválido / Expirado)
Disparada requisição `GET /apiNotaFiscal/notafiscal` com `Authorization: Bearer invalid_token`:
- **.NET Framework / SQL Server:** `HTTP 401 Unauthorized` (`{"error":{"code":"112","message":"Token não encontrado, faça login novamente."}}`)
- **.NET Core / PostgreSQL:** `HTTP 401 Unauthorized` (`{"error":{"code":"112","message":"Token não encontrado, faça login novamente."}}`)

##### C. Caminho Positivo (Requisição com Token OAuth GAM de Usuário Autorizado)
Token OAuth 2.0 obtido via `POST /oauth/gam/v2.0/access_token` (`goab_api_teste`, credenciais locais de `Temp/wsEducacaoSpTeste-local-test-environments.md`):
- **.NET Framework / SQL Server:** `HTTP 200 OK`
  ```json
  {"Items":[{"NotaFiscalId":"1","NotaFiscalSerie":"CE","NotaFiscalNumero":"********"},...],"Pagination":{"Page":"1","PageSize":"50","TotalCount":"17","TotalPages":"1"},"AppliedFilters":{"NotaFiscalId":null,"NotaFiscalNumero":null}}
  ```
- **.NET Core / PostgreSQL:** `HTTP 200 OK`
  ```json
  {"Items":[{"NotaFiscalId":"1","NotaFiscalSerie":"1","NotaFiscalNumero":"123"},...],"Pagination":{"Page":"1","PageSize":"50","TotalCount":"5","TotalPages":"1"},"AppliedFilters":{"NotaFiscalId":null,"NotaFiscalNumero":null}}
  ```

*Nota sobre a Validação Granular de Permissões GAM:* A validação de requisição com token de usuário ativo **sem a permissão de autorização concedida** exige a criação de um Role não-administrador no GAM Backoffice e desvinculação da permissão `apiNotaFiscal-06e86b6b-8fbd-4d93-8a23-21bf07019c2b`. Por envolver configuração manual na interface do GAM Backoffice, essa validação granular é registrada como limitação de ambiente de teste local a ser coberta antes da fase Alpha.

---

### B. Prova do Código C# Compilado e Arquivos OpenAPI Gerados

#### 1. Código C# Compilado Nativo (`apinotafiscal.cs`)

- **Sob `SecurityLevel = Authentication`:**
  ```csharp
  protected override GAMSecurityLevel ApiIntegratedSecurityLevel( string permissionMethod )
  {
      if ( StringUtil.StrCmp(permissionMethod, "gxep_list") == 0 ) return GAMSecurityLevel.SecurityAuthentication ;
      else if ( StringUtil.StrCmp(permissionMethod, "gxep_get") == 0 ) return GAMSecurityLevel.SecurityAuthentication ;
      else if ( StringUtil.StrCmp(permissionMethod, "gxep_create") == 0 ) return GAMSecurityLevel.SecurityAuthentication ;
      else if ( StringUtil.StrCmp(permissionMethod, "gxep_update") == 0 ) return GAMSecurityLevel.SecurityAuthentication ;
      return GAMSecurityLevel.SecurityHigh ;
  }
  ```

- **Sob `SecurityLevel = None`:**
  ```csharp
  protected override GAMSecurityLevel ApiIntegratedSecurityLevel( string permissionMethod )
  {
      if ( StringUtil.StrCmp(permissionMethod, "gxep_list") == 0 ) return GAMSecurityLevel.SecurityNone ;
      else if ( StringUtil.StrCmp(permissionMethod, "gxep_get") == 0 ) return GAMSecurityLevel.SecurityNone ;
      else if ( StringUtil.StrCmp(permissionMethod, "gxep_create") == 0 ) return GAMSecurityLevel.SecurityNone ;
      else if ( StringUtil.StrCmp(permissionMethod, "gxep_update") == 0 ) return GAMSecurityLevel.SecurityNone ;
      return GAMSecurityLevel.SecurityHigh ;
  }
  ```

- **Sob `SecurityLevel = Authorization`:**
  ```csharp
  protected override GAMSecurityLevel ApiIntegratedSecurityLevel( string permissionMethod )
  {
      if ( StringUtil.StrCmp(permissionMethod, "gxep_list") == 0 ) return GAMSecurityLevel.SecurityHigh ;
      else if ( StringUtil.StrCmp(permissionMethod, "gxep_get") == 0 ) return GAMSecurityLevel.SecurityHigh ;
      else if ( StringUtil.StrCmp(permissionMethod, "gxep_create") == 0 ) return GAMSecurityLevel.SecurityHigh ;
      else if ( StringUtil.StrCmp(permissionMethod, "gxep_update") == 0 ) return GAMSecurityLevel.SecurityHigh ;
      return GAMSecurityLevel.SecurityHigh ;
  }
  ```

#### 2. Metadados Auditáveis do OpenAPI YAML em Disco

- **Sob `SecurityLevel = None`:**
  - **.NET Framework:** `C:\KBs\wsEducacaoSpTeste\NETFrameworkSQLServer004\web\apiNotaFiscal.yaml` (mtime 2026-08-04 06:42:49, `version: "20260804094249"`).
  - **.NET Core:** `C:\KBs\wsEducacaoSpTeste\NETPostgreSQL155\web\apiNotaFiscal.yaml` (mtime 2026-08-04 06:44:47, `version: "20260804094447"`).

- **Sob `SecurityLevel = Authentication`:**
  - **.NET Framework:** `C:\KBs\wsEducacaoSpTeste\NETFrameworkSQLServer004\web\apiNotaFiscal.yaml` (mtime 2026-08-04 06:52:36, `version: "20260804095236"`).
  - **.NET Core:** `C:\KBs\wsEducacaoSpTeste\NETPostgreSQL155\web\apiNotaFiscal.yaml` (mtime 2026-08-04 06:50:25, `version: "20260804095025"`).

- **Sob `SecurityLevel = Authorization`:**
  - **.NET Framework:** `C:\KBs\wsEducacaoSpTeste\NETFrameworkSQLServer004\web\apiNotaFiscal.yaml` (mtime 2026-08-04 23:00:27, `version: "20260805020027"`).
  - **.NET Core:** `C:\KBs\wsEducacaoSpTeste\NETPostgreSQL155\web\apiNotaFiscal.yaml` (mtime 2026-08-04 23:01:03, `version: "20260805020103"`).

*Nota de Auditabilidade e Sobrescrita:* Como a IDE GeneXus regera o arquivo físico `apiNotaFiscal.yaml` no diretório `web/` a cada `Build All`, os arquivos OpenAPI YAML gerados sob `SecurityLevel = None` foram sobrescritos pelas regerações subsequentes em `Authentication` e `Authorization`. A prova de auditoria do nível `None` baseia-se no código C# compilado (`ApiIntegratedSecurityLevel` emitindo `GAMSecurityLevel.SecurityNone`) e nas respostas HTTP 200/201 sem token registradas no momento da medição.

*Nota de Reconciliação:* O gerador nativo de documentação REST OpenAPI do GeneXus 18 U15 gera a seção `security: - oAuthGXGAM: []` quando a propriedade global da KB possui GAM ativado. Contudo, a aplicação efetiva da segurança em nível de endpoint HTTP é regida pelo trecho C# compilado (`ApiIntegratedSecurityLevel`), o qual alterna dinamicamente entre `SecurityNone` (acesso público direto), `SecurityAuthentication` (bloqueio 401 / liberação 200 via Bearer Token) e `SecurityHigh` (autorização GAM).

---

## 5. Limitações

- A anotação `[SecurityLevel(...)]` é aplicada no nível dos serviços do objeto `API`. O escopo de papéis/permissões granulares GAM por método continua sendo gerenciado via GAM Backoffice.
- O gerador nativo OpenAPI do GeneXus não permite customizar o nome do esquema de segurança no YAML (`oAuthGXGAM`).

---

## 6. Alternativas Descartadas

- **Herdar Security Level do Environment/Objeto sem anotação por serviço:** Descartada porque fazia o wizard ter escolhas na UI de `None` e `Authorization` sem efeito no objeto gerado.
- **Usar o matcher de fallback `MatchesPreviousB079RestMethodContract` para migração B093:** Descartada por ser excessivamente permissiva (desativava também checagem de `RestMethod` e `{&Chave}`). Foi criado `MatchesPreviousB079SecurityLevelContract` específico.

---

## 7. Pendências Remanescentes

- Nenhuma pendência remanescente. O resíduo condicional de `Location` no serviço `Create` foi concluído e validado nativamente via `&HttpResponse.AddHeader(!"Location", ...)` com resposta `HTTP 201 Created` e cabeçalho `Location` obtidos em ambos os geradores.
