# B038 follow-up - Campos escalares de engine no ApiPlan

Implementado para validacao manual: o `ApiPlan` em memoria passa a resolver os campos escalares iniciais da engine que ja possuem decisao suficiente no MVP, sem persistir metadata e sem gerar objetos de API.

## Objetivo

Evoluir o plano criado em B038 sem reabrir a evidencia historica de B038 nem antecipar B056:

- `GeneratorTarget='.NET'`, como gerador prioritario inicial do MVP;
- `ConflictMode='BlockOnCollision'`, politica conservadora inicial para colisao externa ou incompativel;
- `ReexecutionMode='Safe'`, modo padrao de reexecucao do MVP;
- `ServiceDescriptions` e `ServiceDescriptionLanguage` permanecem pendentes para B056;
- `ServiceDescriptionFallbackUsed=false` permanece apenas como default tecnico enquanto idioma/fallback nao foram resolvidos;
- `IsEngineReady=false` continua bloqueando entrega para engine real.

## Escopo implementado

- o plano continua sendo montado apenas ao concluir o wizard unico sem cancelamento;
- cancelamentos continuam descartando `Transaction`, decisoes e `ApiPlan` em memoria;
- nenhuma metadata e persistida;
- nenhum SDT, Procedure, API Object ou File e criado, alterado ou excluido pela geracao;
- `BlockOnCollision` governa colisao externa ou incompativel, como nome ocupado sem metadata compativel, tipo divergente, modulo divergente ou objeto externo;
- update conservador de objeto proprio reconhecido por metadata permanece responsabilidade futura de `ReexecutionMode`/`ResolvedGenerationPlan`;
- descricoes finais, idioma das descricoes, fallback de idioma e aplicacao de `[Description]` no objeto `API` continuam para B056;
- naquele momento historico, condicao GAM permanecia `UNRESOLVED_B092_GAM_CONDITION`; ela foi resolvida depois no escopo de plano em [B092 - Condicao GAM resolvida no ApiPlan](B092-CONDICAO-GAM-APIPLAN.md).

## Arquivos principais

- `Src/Domain/ApiPlan.cs`
- `Src/Extension/Package.cs`

## Evidencia esperada na Output

A validacao manual deve confirmar uma linha diagnostica equivalente a:

```text
[Genexus Open API Builder][Sprint3] Campos de engine no ApiPlan: GeneratorTarget='.NET' como gerador prioritario inicial do MVP, ConflictMode='BlockOnCollision' para colisao externa/incompativel, ReexecutionMode='Safe', ServiceDescriptionsPending=4/4, ServiceDescriptionLanguage='UNRESOLVED_B056_DESCRIPTION_LANGUAGE', ServiceDescriptionFallbackUsed=False, IsEngineReady=False. Sem validar engine real e sem gerar objetos.
```

## Validacoes mecanicas esperadas

Antes de gerar DLL para validacao manual:

```powershell
dotnet build Src/GenexusOpenApiBuilder.sln -c Release
pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1
git diff --check
```

## Criterio de aceite

A frente ficou pronta quando a IDE confirmou que o wizard unico registrava os campos escalares resolvidos e os bloqueios remanescentes na Output, sem persistir metadata e sem gerar SDT, Procedure, API Object ou File na KB. B056 continuava sendo a frente responsavel por descricoes finais, idioma/fallback e aplicacao de `[Description]`; B092 foi resolvido posteriormente apenas no escopo de plano.
