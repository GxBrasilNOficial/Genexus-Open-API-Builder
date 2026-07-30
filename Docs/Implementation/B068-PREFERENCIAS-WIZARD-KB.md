# B068 - Preferencias do Wizard por KB

## Objetivo

Reduzir repeticao operacional no wizard permitindo que a KB ativa guarde defaults para etapas recorrentes de geracao.

## Decisao

A configuracao segue o modelo conceitual observado no WorkWithWeb: preferencia geral da KB, nao arquivo por usuario. O WorkWithWeb declara `SettingsName=WorkWithForWeb`, especificacao propria de settings e default settings importavel para a KB. Como esta extensao ainda nao implementa um Pattern GeneXus proprio, o caminho conservador adotado foi persistir um File JSON versionado na KB ativa.

## Contrato implementado

- comando novo no menu `Genexus Open API Builder`: `Configurar Preferencias do Wizard`;
- File proprio da KB: `GxOpenApiBuilder_Settings`;
- `External File Name`: `GxOpenApiBuilder_Settings.json`;
- descricao sentinela: `Genexus Open API Builder Wizard Preferences`;
- schema: `GOAB_WIZARD_PREFERENCES_V1`;
- escopo declarado no JSON: `KnowledgeBase`;
- defaults cobertos: SDTs, Procedures, API Object, Metadata, List, Create/Update via Business Component, servicos REST selecionados, `Security Level`, `Default Page Size` e `Maximum Page Size`.

O wizard carrega o File ao abrir. Quando o File nao existe, esta corrompido ou colide com um File externo, o wizard aplica defaults conservadores em memoria e registra a situacao na Output. A gravacao de preferencias bloqueia colisao externa ou ambigua antes de salvar.

## Aplicacao dos defaults

As preferencias nao ignoram dependencias tecnicas. O wizard so marca automaticamente uma etapa quando o controle esta habilitado pelo estado atual da KB e pelo preflight visual. Assim, uma preferencia por `Procedures`, `API Object`, `List` ou `Metadata` continua dependente dos artefatos anteriores estarem confirmados nesta execucao ou reencontraveis na KB.

Os defaults de servico, seguranca e paginacao sao aplicados antes de montar o `ApiPlan` em memoria:

- servicos: `List`, `Get`, `Create` e `Update` podem iniciar marcados ou desmarcados por KB, exigindo ao menos um servico marcado ao salvar a configuracao e ao concluir o wizard;
- seguranca: `Authentication`, `Authorization` ou `None`; o fluxo de seguranca do `ApiPlan` continua resolvendo confirmacoes e notas de risco conforme B092;
- paginacao: `Default Page Size` e `Maximum Page Size` devem ser inteiros positivos e `Default <= Maximum`.

Politicas por campo (`CreateRequest`, `UpdateRequest`, `Response` e `ListFilters`) ficaram fora desta ampliacao. Elas continuam derivadas da leitura tecnica da Transaction e devem ser tratadas em frente propria para nao misturar preferencias gerais com regras de classificacao B090/B091.

## Validacao mecanica

- `pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1`: OK, inicialmente com 12 comandos sincronizados entre `Package.cs` e manifesto.
- `dotnet build Src\GenexusOpenApiBuilder.sln --configuration Release --no-restore`: OK, 0 avisos, 0 erros.

Depois da ampliacao de servicos, seguranca e paginacao, a validacao mecanica foi repetida com o mesmo resultado: checker de comandos OK com 12 comandos e build Release OK com 0 avisos e 0 erros. No fechamento da frente, o menu foi limpo: no menu principal ficam `Configurar Preferencias do Wizard` e `Wizard`; no contexto da Transaction fica somente `Wizard`; novo checker OK com 2 comandos.

No reteste com `Produto`, a preferencia `ApplyBusinessComponent=True` revelou um gating insuficiente: a Transaction estava com Business Component desabilitado e, ainda assim, a opcao tentou executar B055, bloqueando a metadata no mesmo fluxo. O wizard foi ajustado para desabilitar e desmarcar essa opcao quando `IsBusinessComponentReady()` for falso; a preferencia so volta a marcar a etapa quando a Transaction ja e BC ou quando o usuario habilita BC explicitamente no wizard.

## Cobertura automatizada pós-revisão

A revisão externa de B068 aceitou o comportamento runtime, mas apontou dívida de teste automatizado. A cobertura adicionada após a validação funcional cobre:

- `PrototypeWizardPreferencesCodec`: defaults conservadores, serialização/parsing do schema atual, normalização de `SecurityLevel`, validação de `DefaultPageSize <= MaximumPageSize`, fallback de campos opcionais e preservação de serviços, paginação e flags de geração;
- `ApiPlanWritePreflightScope`: seleção de etapas exigidas pelo preflight agregado, garantindo que `GenerateMetadata=False` não selecione `Metadata File`, enquanto `List`, `Business Component` e `Metadata` preservam dependências de SDTs, Procedures e API Object;
- `ApiPlanListProcedureReencounterPolicy`: reencontro B070 aceitando Source próprio conhecido quando apenas os literais `&ApiPageSize = N` e `If &ApiPageSize > N` mudam, e mantendo bloqueio para Source externo, Rules, variáveis e contrato B070 de API Object divergentes.

Esses testes passaram a ser executados pela rotina pré-push local junto com os testes existentes de Service Source e integridade B067.

## Validacao manual

Validacao funcional no GeneXus 18 U15 em 2026-07-29:

- cancelamento do comando `Configurar Preferencias do Wizard` registrou cancelamento sem alteracao na KB;
- gravacao criou o File `GxOpenApiBuilder_Settings` com `Status='Created'`, `Guid='312bb1f6-ec00-4a82-8019-adc43a0aa0ed'` e `Bytes=338`;
- reabertura do wizard registrou carregamento das preferencias da KB ativa a partir de `GxOpenApiBuilder_Settings`;
- conclusao do wizard registrou `GenerateSdts=True`, `GenerateProcedures=True`, `GenerateApiObject=True`, `GenerateMetadata=True`, `ApplyList=True` e `ApplyBusinessComponent=True`;
- o preflight foi aprovado e o fluxo aplicou as etapas habilitadas: SDTs e Procedures reencontrados, API Object sincronizado por B055, List aplicado por B070, metadata escrita por B060 e integridade registrada por B067.

Validacao visual/runtime posterior confirmou que o dialogo de preferencias abre com tamanho adequado, salva os novos defaults e que o wizard aplica servicos, seguranca e paginacao no proximo fluxo.

Validacao parcial da ampliacao no GeneXus 18 U15 em 2026-07-29:

- gravacao do formato ampliado atualizou o File `GxOpenApiBuilder_Settings` existente com `Status='Updated'`, preservando `Guid='312bb1f6-ec00-4a82-8019-adc43a0aa0ed'` e registrando `Bytes=579`.

Validacao de aplicacao da ampliacao no GeneXus 18 U15 em 2026-07-29:

- o wizard carregou `GxOpenApiBuilder_Settings` apos a gravacao ampliada;
- o contrato em memoria iniciou com `Services='List,Get,Create,Update'`;
- a revisao B032 iniciou com `SecurityLevel='Authorization'`, confirmando aplicacao do default de seguranca salvo;
- a conclusao B034 refletiu defaults de geracao atualizados: `GenerateSdts=True`, `GenerateProcedures=True`, `GenerateApiObject=True`, `GenerateMetadata=False`, `ApplyList=True` e `ApplyBusinessComponent=True`;
- o preflight agregado bloqueou antes do primeiro `Save()` por colisao/integridade divergente em `Metadata File`, sem criar, alterar ou sufixar objetos.

Esse ultimo item revelou um bug de escopo no preflight agregado: `Metadata File` era validado mesmo quando `GenerateMetadata=False`. A correcao limita o preflight final as etapas confirmadas e as dependencias realmente necessarias:

- `Metadata File` so bloqueia o fluxo quando `GenerateMetadata=True`;
- SDTs, Procedures e API Object continuam bloqueando quando a etapa correspondente foi selecionada ou quando uma etapa posterior confirmada depende deles;
- a mensagem visual de bloqueio passa a listar apenas os estagios exigidos pela selecao atual.

Para facilitar a validacao final, a Output B032 do wizard unico passou a registrar explicitamente `DefaultPageSize` e `MaximumPageSize` junto com paths e seguranca.

Validacao final da correcao de escopo no GeneXus 18 U15 em 2026-07-29:

- o wizard carregou `GxOpenApiBuilder_Settings`;
- B032 registrou `SecurityLevel='Authorization'`, `DefaultPageSize=40` e `MaximumPageSize=100`, confirmando aplicacao dos defaults ampliados de seguranca e paginacao;
- B034 registrou `GenerateMetadata=False`;
- o preflight agregado foi aprovado antes do primeiro `Save()`, confirmando que `Metadata File` divergente deixou de bloquear quando metadata nao foi selecionada;
- SDTs e Procedures foram reencontrados, B055 aplicou Create/Update via Business Component e reaplicou descricoes B056 no API Object real.

O bloqueio restante ocorreu em B070: `procContrato_API_List` possui Source divergente da geracao B050/B070. Esse bloqueio e conservador e pertence a revalidacao/recuperacao de B070, nao ao mecanismo de preferencias B068.

Correcao B070 decorrente da ampliacao de B068:

- a Procedure `procContrato_API_List` reencontrada era propria e seguia template B070/B077 conhecido, mas ainda continha paginacao anterior `DefaultPageSize=50` e `MaximumPageSize=200`;
- o plano novo da KB passou a esperar `DefaultPageSize=40` e `MaximumPageSize=100`;
- o preflight B070 agora aceita esse caso restrito como reconfiguracao segura: compara Sources proprios conhecidos ignorando apenas os literais de paginacao `&ApiPageSize = N` e `If &ApiPageSize > N`;
- Rules, variaveis, descricao propria, API Object e demais trechos do Source continuam bloqueando se divergirem;
- ao aplicar B070, a Procedure e regravada com os valores atuais do plano.

Validacao runtime da correcao B070 no GeneXus 18 U15 em 2026-07-29:

- B032 registrou `DefaultPageSize=40` e `MaximumPageSize=100`;
- B034 registrou `GenerateMetadata=False` e `ApplyList=True`;
- o preflight agregado foi aprovado antes do primeiro `Save()`;
- SDTs e Procedures foram reencontrados, B055 sincronizou Create/Update via Business Component e B070 aplicou List;
- B070 registrou `ListProcedureGuid='0e770b84-4c92-41f4-9368-e18176ac6f89'`, `Filters=1`, `OrderParts=1`, `DefaultPageSize=40`, `MaximumPageSize=100`;
- a IDE recarregou `procContrato_API_List` apos a escrita.
