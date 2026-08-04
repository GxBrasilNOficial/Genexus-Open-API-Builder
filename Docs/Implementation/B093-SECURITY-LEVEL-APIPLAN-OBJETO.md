# B093 — Aplicação Explícita do Security Level no API Object

**Projeto:** Genexus Open API Builder  
**Frente:** B093 — Aplicar o Security Level explicitamente em todos os serviços do API Object gerado (Sprint 6)  
**Data:** 2026-08-03  

---

## 1. Contexto e Diagnóstico

Antes desta frente, o wizard oferecia três níveis de segurança — `Authentication`, `Authorization` e `None`. O valor selecionado entrava no `ApiPlan`, era persistido na metadata (B065) e a condição GAM correspondente era resolvida no plano (B092):
- `Authentication` ➔ `GAM_AUTHENTICATION_REQUIRED` (`RequiresGenerationConfirmation=False`)
- `Authorization` ➔ `GAM_AUTHORIZATION_REQUIRED_PENDING_PERMISSIONS` (`RequiresGenerationConfirmation=True`)
- `None` ➔ `NO_GAM_SECURITY_PUBLIC_API` (`RequiresGenerationConfirmation=True`)

Porém, essa escolha não era emitida no objeto `API` gerado: nem `ApiPlanBusinessComponentWriter.cs` nem `ApiPlanListProcedureWriter.cs` aplicavam anotações `[SecurityLevel(...)]` nos serviços. Na ausência de anotação explícita, a IDE/gerador GeneXus herdava a configuração padrão da KB (`Authentication`), fazendo com que nos três casos a API gerada e o YAML OpenAPI saíssem idênticos.

---

## 2. Alterações Realizadas

1. **Geração da Anotação Explícita (`[SecurityLevel(...)]`):**
   - Em `Src/Extension/Diagnostics/ApiPlanBusinessComponentWriter.cs`: a geração de anotações por serviço (`ServiceAnnotations`) passou a incluir `[SecurityLevel({plan.Security.SecurityLevel})]`.
   - Em `Src/Extension/Diagnostics/ApiPlanListProcedureWriter.cs`: a mesma inclusão foi feita na resincronização de `List`, garantindo que o writer de `List` não apague a marcação de segurança aplicada anteriormente.

2. **Parser de Contrato (`ApiPlanServiceSourceContract.cs`):**
   - Atualizado para exigir `[SecurityLevel(` na validação de contrato runtime quando `hasRestRuntimeContract` e `validateRestMethods` forem verdadeiros.

3. **Integridade B067 e Compatibilidade Reencontro (`ApiPlanMetadataFileWriter.cs`):**
   - Adicionado o utilitário `RemoveServiceSourceSecurityLevelAnnotations` para gerar variantes de fontes legadas em `CreateCompatibleServiceSourceVariants`, permitindo que objetos de API criados antes de B093 continuem sendo reencontrados de forma conservadora.

4. **Testes Unitários e Trava Automatizada:**
   - `Tests/BusinessComponentWriter/Test-ApiPlanBusinessComponentWriterVariableContract.ps1`: valida a presença de `[SecurityLevel({plan.Security.SecurityLevel})]` em `ApiPlanBusinessComponentWriter.cs`.
   - `Tests/OpenApiContract/Test-ApiPlanOpenApiContractMarks.ps1`: valida a trava de emissão explícita de `[SecurityLevel(...)]` em ambos os writers (`Business Component` e `List`).
   - `Tests/ServiceSourceContract/Test-ApiPlanServiceSourceContract.ps1`: atualizado com a anotação `[SecurityLevel(Authentication)]` no fixture de contrato `$b079`.

---

## 3. Validação Mecânica

Os 9 testes unitários locais e o build Release da solução foram executados e aprovados com sucesso:
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

---

## 4. Evidência da Validação Manual na IDE (GeneXus 18 Upgrade 15)

A DLL foi instalada via `Install-ExtensionForGeneXus18.bat` e a regeração do objeto `apiNotaFiscal` foi testada nos três níveis de segurança:

1. **Nível `Authorization`:**
   - Wizard configurado com `SecurityLevel='Authorization'`, resolvendo `GAM_AUTHORIZATION_REQUIRED_PENDING_PERMISSIONS`.
   - `Service Source` regerado com `[SecurityLevel(Authorization)]` nos 4 serviços (`List`, `Get`, `Create`, `Update`).
   - `Build All` especificou, gerou C#, SDTs, documentação REST e acionou `GAM Permissions Creation`: `Generating Permission apiNotaFiscal-06e86b6b-8fbd-4d93-8a23-21bf07019c2b (1 of 1)`. `Success: Build All`.

2. **Nível `None`:**
   - Wizard configurado com `SecurityLevel='None'`, resolvendo `NO_GAM_SECURITY_PUBLIC_API`.
   - `Service Source` regerado com `[SecurityLevel(None)]` nos 4 serviços.
   - `Build All` especificou, gerou C#, SDTs e documentação REST OpenAPI sem exigir autenticação GAM nos endpoints. `Success: Build All`.

3. **Nível `Authentication`:**
   - Wizard configurado com `SecurityLevel='Authentication'`, resolvendo `GAM_AUTHENTICATION_REQUIRED`.
   - `Service Source` regerado com `[SecurityLevel(Authentication)]` nos 4 serviços.
   - Metadata B060 e integridade B067 reencontradas e regravadas com sucesso. `Success: Build All`.

4. **Validação nos Dois Geradores / Ambientes:**
   - **Environment `.NET Framework` / SQL Server (`NETFrameworkSQLServer004`):** `Build All` especificou `apiNotaFiscal` e as Procedures, gerou o código C#, compilou a solução com `MSBuild.exe`, atualizou `web.config` e criou a permissão GAM com sucesso.
   - **Environment `.NET` / PostgreSQL (`NETPostgreSQL155`):** `Build All` especificou `apiNotaFiscal` e as Procedures, gerou `apinotafiscal_services.cs`, compilou a solução com `dotnet publish`, atualizou `appsettings.json` e criou a permissão GAM com sucesso.


