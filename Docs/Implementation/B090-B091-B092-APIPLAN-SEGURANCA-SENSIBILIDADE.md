# B090/B091/B092 - ApiPlan com seguranca, sensibilidade e auditoria

Representacao provisoria validada no GeneXus 18 Upgrade 15 em 2026-07-23 dentro do `ApiPlan` em memoria: o wizard unico aberto por `Abrir Wizard (B030)` passou a registrar classificacao explicita inicial de campos sensiveis, auditoria operacional separada e seguranca, sem persistir metadata e sem gerar objetos de API. Esse marco nao conclui B090/B091 canonicos, que permanecem abertos ate existir configuracao por KB em metadata persistente.

## Objetivo

Registrar no plano interno a proxima camada da Sprint 3:

- representacao provisoria de `B090`: campos sensiveis classificados por politica explicita inicial hardcoded em memoria;
- representacao provisoria de `B091`: auditoria operacional classificada separadamente por politica explicita inicial hardcoded em memoria;
- `B092`: `Security Level` registrado no `ApiPlan`, com condicao GAM mantida explicitamente pendente ate validacao publica segura.

## Escopo validado

- a classificacao usa politica inicial hardcoded em memoria, sem metadata persistente e sem configuracao por KB;
- a origem e a razao da classificacao sao preservadas no snapshot do wizard e no `ApiPlan`, incluindo fonte de politica hardcoded;
- sensibilidade e auditoria operacional ficam separadas;
- `SecurityLevel` e registrado no bloco de seguranca do `ApiPlan`;
- `GamCondition` fica como `UNRESOLVED_B092_GAM_CONDITION`;
- `None` e `Authorization` ficam marcados como exigindo confirmacao antes da geracao;
- `Authentication` nao exige confirmacao adicional no plano atual;
- nenhum SDT, Procedure, API Object ou File de metadata e criado, alterado ou excluido pela geracao;
- no estado validado nesta frente, `IsEngineReady` continuava `false` enquanto `GeneratorTarget`, `ConflictMode`, `ReexecutionMode` e descricoes de servico estivessem pendentes.

## Evolucao posterior

O follow-up [B038 follow-up - Campos escalares de engine no ApiPlan](B038-FOLLOWUP-CAMPOS-ESCALARES-ENGINE.md) resolveu posteriormente `GeneratorTarget`, `ConflictMode` e `ReexecutionMode` em memoria, mantendo descricoes/idioma/fallback pendentes naquele momento historico. A frente [B056 - Descricoes de servico no ApiPlan](B056-DESCRICOES-SERVICO-APIPLAN.md) validou depois as descricoes no plano. O follow-up [B092 - Condicao GAM resolvida no ApiPlan](B092-CONDICAO-GAM-APIPLAN.md) resolveu posteriormente as condicoes `Authentication`, `Authorization` e `None` no plano; `IsEngineReady=false` permanece pela engine real ainda inexistente e pela ausencia de aplicacao efetiva de seguranca em objetos reais.

O follow-up [B090/B091 - Contrato de configuracao por KB no ApiPlan](B090-B091-CONTRATO-CONFIGURACAO-KB-APIPLAN.md) validou posteriormente o contrato preparatorio de configuracao por KB no snapshot do wizard e no `ApiPlan`, ainda sem metadata persistente e sem concluir B090/B091 canonicos.

## Arquivos principais

- `Src/Extension/Diagnostics/PrototypeWizardContract.cs`
- `Src/Domain/ApiPlan.cs`
- `Src/Extension/Package.cs`

## Evidencia manual U15

Validacao recebida em 2026-07-23 para a Transaction `Contrato`:

Observacao posterior: o bloco abaixo preserva a captura manual historica de 2026-07-23. Depois dessa validacao, o runtime mudou duas vezes: primeiro passou a explicitar a ausencia de metadata/configuracao por KB, e em 2026-07-25 passou a emitir o contrato preparatorio com `ConfigScope`, `ConfigSource`, `ConfigStatus`, `PersistedMetadata`, `KbConfigured`, `SensitiveRules` e `AuditRules`. A captura atual desse follow-up esta registrada em `B090-B091-CONTRATO-CONFIGURACAO-KB-APIPLAN.md`.

```text
[Genexus Open API Builder][B030] Wizard único concluido em memoria: Transaction='Contrato', Module='Root Module', SelectionSource='Contexto'.
[Genexus Open API Builder][B031] Contrato em memoria: Services='List,Get,Create,Update', Create=1, Update=1, Response=2, ListFilters=1.
[Genexus Open API Builder][B032] Paths e segurança em memoria: ApiName='apiContrato', ServicesBasePath='apiContrato', RestPath='/contrato', SecurityLevel='Authentication'.
[Genexus Open API Builder][B033] Obrigatoriedade em memoria: CreateRequired=0, UpdateRequired=1. Required significa presença do membro JSON, nao valor nao-vazio.
[Genexus Open API Builder][B037] Obrigatorio no payload consolidado: CreateRequired=0, UpdateRequired=1. Required e presenca do membro JSON; vazio, false e 0 continuam valores enviados. UpdateRequest segue PUT completo.
[Genexus Open API Builder][B036] Campos bloqueados visiveis no wizard: CreateRequest=1, UpdateRequest=1, ListFilters=0. Itens bloqueados ficaram desmarcados, com motivo, e nao podem ser selecionados.
[Genexus Open API Builder][B090/B091] Classificacao em memoria: SensitiveFields=0, AuditFields=0. Politica inicial explicita aplicada sem metadata persistente.
[Genexus Open API Builder][B035] Business Component em memoria: IsBusinessComponent=True, EnabledDuringWizard=False, Status='Apta via Business Component'.
[Genexus Open API Builder][B038] ApiPlan em memoria criado: Transaction='Contrato', ModuleTarget='Root Module', ApiName='apiContrato', MetadataFile='apiContrato_Metadata', EndpointsCount=4.
[Genexus Open API Builder][B038] ApiPlan cobre: PrimaryKey=1, CreateFields=1, UpdateFields=1, ResponseFields=2, ListFilters=1, RequiredFields=2, Procedures=4, SharedSdts=2. Sem persistir metadata e sem gerar SDT, Procedure, API Object ou File na KB.
[Genexus Open API Builder][B092] Seguranca no ApiPlan: SecurityLevel='Authentication', GamCondition='UNRESOLVED_B092_GAM_CONDITION', RequiresGenerationConfirmation=False. Sem aplicar seguranca em objetos reais.
[Genexus Open API Builder][B034] Wizard concluido sem acionar cancelamento. Decisoes e ApiPlan permanecem somente em memoria; nenhuma geracao de objetos de API foi executada.
```

## Validacoes mecanicas

Executadas antes da validacao manual:

```powershell
dotnet build Src/GenexusOpenApiBuilder.sln -c Release
pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1
git diff --check
```

Resultados: build Release OK com 0 erros, checker de comandos OK com 8 comandos e `git diff --check` sem apontamentos. O build emitiu avisos NU1900 por indisponibilidade dos indices NuGet para auditoria de vulnerabilidade, sem impedir a compilacao.

## Criterio de aceite

Criterio atendido em 2026-07-23 para a representacao provisoria no `ApiPlan` em memoria: foram registradas classificacao inicial explicita de sensiveis, auditoria operacional separada e seguranca no plano. A frente nao persistiu metadata nem gerou SDT, Procedure, API Object ou File na KB. B090/B091 canonicos continuam abertos ate configuracao por KB em metadata persistente. No estado validado naquela frente, a Sprint 3 continuava com `IsEngineReady=false` ate resolver `GeneratorTarget`, `ConflictMode`, `ReexecutionMode`, descricoes de servico e condicao GAM. A condicao GAM foi resolvida posteriormente no escopo de plano em `B092-CONDICAO-GAM-APIPLAN.md`, ainda sem aplicar seguranca em objetos reais.
