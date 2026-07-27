# B056 - Descricoes de servico no ApiPlan e no API Object

Validado manualmente no GeneXus 18 Upgrade 15 em duas etapas:

- em 2026-07-25, o `ApiPlan` passou a resolver descricoes de servico em memoria;
- em 2026-07-27, as descricoes resolvidas foram aplicadas como `[Description]` nos servicos reais do objeto `API`, preservando o contrato B055 quando existente.

## Objetivo

Aplicar descricoes nos servicos do API Object real ja criado ou reencontrado, reaproveitando `ServiceDescriptions` do `ApiPlan`, sem antecipar comportamento REST completo, codigos HTTP finais, seguranca definitiva ou metadata persistente.

## Escopo implementado

- `ServiceDescriptions` deixa de usar `UNRESOLVED_B056_SERVICE_DESCRIPTION` quando o wizard conclui;
- as descricoes sao criadas para os servicos selecionados `List`, `Get`, `Create` e `Update`;
- cada descricao usa a descricao legivel da `Transaction` quando existir;
- quando a descricao da `Transaction` estiver vazia, o plano usa o nome da `Transaction`;
- `ServiceDescriptionLanguage` fica `English` como fallback tecnico inicial;
- `ServiceDescriptionLanguageSource` fica `PendingKbLanguageApiValidation`;
- `ServiceDescriptionFallbackUsed` fica `true`;
- `ServiceDescriptionFallbackReason` explicita que o idioma principal da KB ainda nao foi validado por API publica;
- B054 grava ou regrava o `ServiceGroupSource.Source` B054 com `[Description]` quando o API Object ainda esta no formato B054;
- B054 atualiza API Object B055 legado para B055 com `[Description]`, sem remover parametros de `Create`/`Update`;
- B055 grava `Create` e `Update` parametrizados com `[Description]` quando a opcao `Business Component` e aplicada;
- reexecucao reconhece fontes B054/B055 legadas sem descricoes como proprias e atualizaveis, mas continua bloqueando fontes divergentes;
- nenhuma metadata persistente, codigo HTTP final, seguranca definitiva ou comportamento REST completo e criado por esta frente.

## Arquivos principais

- `Src/Domain/ApiPlan.cs`
- `Src/Extension/Diagnostics/ApiPlanApiObjectWriter.cs`
- `Src/Extension/Diagnostics/ApiPlanBusinessComponentWriter.cs`
- `Src/Extension/Package.cs`

## Evidencia manual U15 - ApiPlan

Validacao recebida em 2026-07-25 para a Transaction `Contrato`:

```text
[Genexus Open API Builder][Sprint3] Campos de engine no ApiPlan: GeneratorTarget='.NET' como gerador prioritario inicial do MVP, ConflictMode='BlockOnCollision' para colisao externa/incompativel, ReexecutionMode='Safe', ServiceDescriptionsPending=0/4, ServiceDescriptionLanguage='English', ServiceDescriptionFallbackUsed=True, IsEngineReady=False. Sem validar engine real e sem gerar objetos.
[Genexus Open API Builder][B056] Descricoes no ApiPlan: Resolved=4/4, Language='English', LanguageSource='PendingKbLanguageApiValidation', FallbackUsed=True, FallbackReason='Idioma principal da KB ainda nao validado por API publica; fallback tecnico em ingles registrado no ApiPlan.'. Sem aplicar [Description] em objeto API real e sem gerar objetos.
```

Essa captura preserva o estado preparatorio da frente: descricoes resolvidas no plano, ainda sem escrita em API Object real.

## Evidencia manual U15 - API Object real

Validacao recebida em 2026-07-27 para a Transaction `GuiaPed`, com somente `API Object` marcado no wizard:

```genexus
apiGuiaPed
{
    [Description("List Guia Ped")]
    List()
        => procGuiaPed_API_List();

    [Description("Get Guia Ped")]
    Get()
        => procGuiaPed_API_Get();

    [Description("Create Guia Ped")]
    Create(in: &CreateRequest, out: &CreateResponse)
        => procGuiaPed_API_Create(&CreateRequest, &CreateResponse);

    [Description("Update Guia Ped")]
    Update(in: &GuiaPedIdboleto, in: &UpdateRequest, out: &UpdateResponse)
        => procGuiaPed_API_Update(&GuiaPedIdboleto, &UpdateRequest, &UpdateResponse);
}
```

Resultado do `Build All` na KB `wsEducacaoSpTeste`:

```text
Specifying apiGuiaPed ...
Success: Specification
Success: Default (.NET Framework) Generation
Success: Rest API Documentation Generation
Success: DeveloperMenu Compilation for Default (.NET Framework)
Success: GAM Permissions Creation
Success: Build All
```

O warning de copia de `FBiTextSharp.dll` foi classificado como ambiental e nao relacionado ao contrato B056.

## Validacoes mecanicas

Executadas durante a implementacao local:

```powershell
dotnet build Src/GenexusOpenApiBuilder.sln --configuration Release --no-restore
pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1
pwsh -NoProfile -File Tests/PrePushChecker/Test-OpenApiBuilderPrePushChecks.ps1
git diff --check
```

Resultado: build Release OK com 0 erros; registro de comandos OK; teste do checker pre-push OK; diff sem erro de whitespace. Avisos NU1900 apareceram apenas quando houve tentativa de consultar indices NuGet para auditoria de vulnerabilidade, sem impedir a compilacao.

## Criterio de aceite

Criterio atendido em 2026-07-27 quando:

- `ServiceDescriptionsPending=0/4` para os quatro servicos selecionados;
- B056 exibiu `Resolved=4/4`, `Language='English'`, `LanguageSource='PendingKbLanguageApiValidation'` e `FallbackUsed=True` no `ApiPlan`;
- o API Object real recebeu `[Description]` em todos os servicos selecionados;
- o caso B055 legado preservou `Create`/`Update` parametrizados e recebeu descricoes;
- `Build All` especificou `apiGuiaPed`, gerou documentacao REST e concluiu sem erro relacionado.

B056 esta concluido no escopo canônico atual. Permanecem fora desta frente: metadata persistente, codigos HTTP finais, seguranca definitiva e comportamento REST completo da Sprint 6.
