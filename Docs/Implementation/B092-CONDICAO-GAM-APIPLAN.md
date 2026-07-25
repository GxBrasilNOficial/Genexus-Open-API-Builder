# B092 - Condicao GAM resolvida no ApiPlan

Validacao manual executada no GeneXus 18 Upgrade 15 em 2026-07-25 para a Transaction `Contrato`, dentro do wizard unico aberto por `Abrir Wizard (B030)`.

## Objetivo

Resolver no `ApiPlan` em memoria a condicao de seguranca de `B092` para os tres valores oficiais de `SecurityLevel` ja expostos pelo wizard, ainda sem aplicar seguranca em objetos reais e sem gerar objetos na KB.

## Escopo validado

- `Authentication` fica representado como `GamCondition='GAM_AUTHENTICATION_REQUIRED'`;
- `Authorization` fica representado como `GamCondition='GAM_AUTHORIZATION_REQUIRED_PENDING_PERMISSIONS'`;
- `None` fica representado como `GamCondition='NO_GAM_SECURITY_PUBLIC_API'`;
- `Authorization` e `None` exigem `RequiresGenerationConfirmation=True` antes da geracao definitiva;
- `Authentication` permanece sem confirmacao extra no plano em memoria;
- a extensao ainda nao detecta por API publica se a KB possui GAM habilitado;
- permissoes GAM coerentes para `Authorization` ainda nao sao criadas nem validadas;
- nenhum `SecurityLevel` e aplicado em objeto `API` real nesta frente;
- nenhum SDT, Procedure, API Object ou File de metadata e criado, alterado ou excluido pela geracao.

## Arquivos principais

- `Src/Domain/ApiPlan.cs`
- `Src/Extension/Package.cs`
- `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`

## Evidencia manual U15

Captura relevante da janela Output para os tres valores de `SecurityLevel`:

```text
[Genexus Open API Builder][B092] Seguranca no ApiPlan: SecurityLevel='Authentication', GamCondition='GAM_AUTHENTICATION_REQUIRED', RequiresGenerationConfirmation=False. Sem aplicar seguranca em objetos reais.
[Genexus Open API Builder][B092] Seguranca no ApiPlan: SecurityLevel='Authorization', GamCondition='GAM_AUTHORIZATION_REQUIRED_PENDING_PERMISSIONS', RequiresGenerationConfirmation=True. Sem aplicar seguranca em objetos reais.
[Genexus Open API Builder][B092] Seguranca no ApiPlan: SecurityLevel='None', GamCondition='NO_GAM_SECURITY_PUBLIC_API', RequiresGenerationConfirmation=True. Sem aplicar seguranca em objetos reais.
```

A mesma captura confirmou o escopo sem geracao:

```text
[Genexus Open API Builder][B038] ApiPlan cobre: PrimaryKey=1, CreateFields=1, UpdateFields=1, ResponseFields=2, ListFilters=1, RequiredFields=2, Procedures=4, SharedSdts=2. Sem persistir metadata e sem gerar SDT, Procedure, API Object ou File na KB.
[Genexus Open API Builder][B034] Wizard concluido sem acionar cancelamento. Decisoes e ApiPlan permanecem somente em memoria; nenhuma geracao de objetos de API foi executada.
```

## Validacoes mecanicas

Executado antes da validacao manual:

```powershell
dotnet build Src\GenexusOpenApiBuilder.sln -c Release --no-restore
```

Resultado: build Release OK, com 0 erros e 0 avisos.

## Criterio de aceite
