# B109 ramo C — confirmação ilegível sem causa: diagnóstico e correção da captura

Data: 2026-09-25. Item: `B109` (`Docs/Foundation/06-BACKLOG_v0.1.md`). Estado do ramo C
**aberto**: esta frente corrige a **captura** do diagnóstico, não a causa da falha.

## 1. O que aconteceu em campo (2026-09-24)

Registro original no checkpoint e no backlog. No Apply do Wizard da `Empresa`, na KB
`fabricabrasil18test`:

- `procEmpresa_API_Get` e `procEmpresa_API_Create` foram gravadas e confirmadas na etapa de
  Business Component;
- a confirmação pós-Save de `procEmpresa_API_Update` falhou com `Outcome='OutcomeUnknown'`,
  `Confirmation='Unreadable'` e `System.Reflection.TargetInvocationException`, com mensagem
  genérica e **sem `InnerException`** na Output;
- Source preparado com cerca de 153 KB e 82 variáveis; List, API Object e metadata não rodaram;
  diário `Partial`; relatório `Interrupted`, `Criados=49`, `Bloqueados=1`.

## 2. Estado da KB antes dos testes de 2026-09-25

Leitura pela MCP `genexus18mcp`, somente leitura, e pelo File do diário exportado pelo usuário
(`GxOpenApiBuilder_OperationJournal`, cujo conteúdo a leitura por `Source` da MCP não expõe):

- a API foi reaplicada em 2026-09-24, entre 17:24:49 e 17:26:33 UTC, pela DLL
  `generatorVersion=0.1.0-alpha.8`;
- diário: `operationKind=Apply`, `operationState=Completed`, `logicalStage=Completed`,
  `journalDurability=Confirmed`, `blockReason=null`, 56 recibos, todos `Confirmed`;
- a etapa de Business Component confirmou as quatro Procedures, incluindo
  `procEmpresa_API_Update` (recibo 52, 6.132 ms), seguida de List, API Object e metadata;
- `apiEmpresa` e as Procedures `procEmpresa_API_List`, `_Get`, `_Create` e `_Update` existem;
  `procEmpresa_API_Update` tem 152.747 bytes e 2.219 linhas.

A reaplicação **não reproduziu** a falha. Isso é ausência de reprodução, não explicação.

## 3. Por que a causa não chegou à Output

Três pontos em sequência, lidos no código em `413a5c3`:

1. `ConfirmProcedure`, em `ApiPlanBusinessComponentWriter`, captura a exceção da releitura e
   devolve `PersistenceConfirmation.Unreadable(tipo + ": " + mensagem)`. Para uma
   `TargetInvocationException`, essa mensagem é sempre a genérica; a causa está no inner, e o
   objeto da exceção é descartado ali.
2. `ApiPlanSaveStepExecutor` lança uma `InvalidOperationException` nova, «Persistência de ...
   não foi confirmada», **sem inner**.
3. `B109ExceptionProbe.Describe`, chamada no `catch` da etapa em `Package`, percorre a cadeia
   dessa exceção nova, que termina no primeiro nível.

A falta do `InnerException` era, portanto, garantida pelo desenho, e não acaso. O mesmo padrão
estava em catorze `catch` de confirmação (SDT, Procedure, List, API Object, metadata, Folder,
Remover, recuperação B115 e o próprio núcleo do seam) e em doze pontos que lançam a não
confirmação. O padrão entrou com o seam da F2 (`5e0556a`, 2026-09-12) e saiu publicado na
`0.1.0-alpha.8`; a `0.1.0-alpha.7` não o contém.

## 4. Correção

- `PersistenceConfirmation` ganhou `Cause` (a exceção, só em memória) e a fábrica
  `Unreadable(Exception)`. O `Detail` passa a resumir a cadeia inteira, `Tipo: mensagem → Tipo:
  mensagem`, com até seis níveis e até 1.000 caracteres. O diário não grava `Detail` nos
  recibos, então o schema e o custo de gravação do File não mudam.
- `PersistenceReceipt` ganhou `ConfirmationCause`, preenchida a partir da confirmação.
- Os catorze `catch` passaram a usar `Unreadable(exception)`; os doze pontos que lançam «não foi
  confirmada» repassam `receipt.ConfirmationCause` ou `confirmation.Cause` como inner.
- O fluxo não muda: o resultado continua `OutcomeUnknown`, a etapa continua interrompida e
  nada é gravado a mais ou a menos.

Gates:

- `Tests/PersistenceProbe/Test-ApiPlanSaveStepExecutor.ps1` — cenário novo: a confirmação
  devolve `Unreadable(TargetInvocationException(InvalidOperationException))`; a exceção do
  executor chega com a `TargetInvocationException` como inner e com o inner dela preservado, e o
  `Detail` do recibo contém o tipo e a mensagem do nível interno;
- `Tests/PersistenceProbe/Test-ApiPlanPersistenceSeamCoverage.ps1` — trava estática: nenhum
  `Unreadable(exception.GetType()...)` no código de produção, e toda exceção «não foi
  confirmada» repassa a causa.

Build Release da DLL canônica com 0 avisos e 0 erros. Os dois gates acima,
`Test-ApiPlanPersistenceCore.ps1` e `Test-ApiPlanOperationJournalReceipts.ps1` passaram.
Depois do commit `c4202c6`, a build Release da DLL satélite U13
(`GenexusOpenApiBuilder.Gx18u13.sln`), feita à mão porque o orquestrador não a compila (D45;
revisão registrada em `B129`), também passou com 0 erros e só o `MSB3277` conhecido.

## 5. O que continua aberto

- A **causa** da `TargetInvocationException`. Hipótese não testada: a releitura em
  `ConfirmProcedure` (`Procedure.Get`, leitura de `Variables`, resolução de tipos por
  `TrySetVariableType`) sobre uma Procedure de ~153 KB. Sem o inner, qualquer hipótese é
  palpite.
- **Validação na IDE desta correção.** Não há como provocar a falha por clique. A correção só
  prova seu valor na próxima ocorrência, que deve trazer na Output a cadeia completa sob
  `[B109]`. **Atualização — mesma data:** a ocorrência veio, o `Detail` trouxe o inner, e a stack
  faltou por uma segunda lacuna; ver a seção 6.
- **Próximo passo sugerido:** instalar a DLL com esta correção, repetir o Apply da `Empresa` na
  `fabricabrasil18test` (Remover e Wizard) algumas vezes e, se a falha voltar, ler o inner. Se
  não voltar, o ramo C fica como o ramo A: condicionado à reprodução, com a captura já pronta.
  **Feito na mesma data:** uma reprodução (seção 6) e dois retestes sem falha (seção 7); o ramo C
  ficou condicionado à reprodução.

Manifesto e registro da extensão não mudaram; a instalação da DLL não exige `genexus /install`.

## 6. Primeira ocorrência depois da correção (2026-09-25)

DLL instalada: build anterior ao commit `c4202c6`, com a correção da seção 4 (o binário contém o
separador `→` da cadeia; o `Test-InstalledExtension.ps1` acusou hash diferente porque o pré-push
recompilou a DLL depois, com diferença só de comentário no fonte).

Sequência na `fabricabrasil18test`, `Empresa`:

1. `Remover API gerada`: `Removed`, 51 removidos numa passada, diário `Removed/Removed`,
   `TotalMs=24622` (no aceite 1B de 2026-09-17 foram 20.063 ms; registro, não parte do B109).
2. IDE **fechada e reaberta** antes do Wizard.
3. Wizard com List, Get, Create, Update, Delete e Business Component; 13 subníveis, 52 objetos
   planejados. A Output registrou `warning: Could not refresh Stencil 'GAM_FooterEntry'` entre a
   abertura e o Apply.
4. Apply: preflight aprovado; Folder confirmado; o **primeiro SDT**,
   `sdtEmpresa_API_CreateRequest_CriacaoVolumes`, foi gravado e a releitura de confirmação falhou:

```text
Confirmation='Unreadable'
Artech.Udm.Framework.Exceptions.UdmException: Unable to Deserialize Data.
  → System.InvalidOperationException: Collection was modified; enumeration operation may not execute.
```

Resultado `Interrupted`, `Criados=1`, `Bloqueados=1`, 5,5 s; diário `Partial/NotStarted` com
dois recibos. Nenhuma Procedure, API Object ou metadata.

**O que isso muda.**

- O `Collection was modified` do **ramo A** apareceu como **causa interna** de uma falha do SDK
  ao desserializar o objeto na releitura — o mecanismo do **ramo C**. Os dois ramos podem ser
  a mesma falha vista por camadas diferentes. É hipótese: a `TargetInvocationException` de
  2026-09-24 não teve o inner capturado.
- A falha veio no primeiro SDT, 5,5 s depois do início, e não numa Procedure de 153 KB: o tamanho
  do objeto deixa de explicar.
- A IDE foi reaberta entre o Remover e o Apply; a hipótese «trabalho de fundo da IDE depois de
  51 deletes» perde força. Continua possível trabalho de fundo iniciado pela abertura da KB (o
  aviso de Stencil aparece nessa janela), além da reentrância por `Application.DoEvents()` do
  ramo A.

**Por que ainda não há stack.** O `catch` da etapa de SDTs em `Package` não chamava a
`B109ExceptionProbe`; o inner chegou até ali e só a mensagem foi escrita. O mesmo valia para
Procedures, API Object (criação e preparação), habilitação do Business Component e recuperação
de metadata B115. Os seis passaram a chamar a sonda, e
`Test-ApiPlanPersistenceSeamCoverage.ps1` ganhou uma trava: todo `catch` de etapa em `Package`
que registra bloqueio no relatório precisa chamar a sonda (dez blocos verificados; a remoção da
sonda de SDTs é detectada). O preflight agregado fica fora, porque seus bloqueios são validações
esperadas. Build canônica com 0 avisos e satélite U13 com 0 erros.

**Próximo passo.** Reinstalar a DLL, executar `Recuperar operação interrompida` e repetir o Apply
sem mudar nada. Se reproduzir, a stack mostra quem enumera a coleção; em seguida repetir com a
preferência «Suprimir a atualização da tela durante as gravações» ligada, para separar
reentrância por `DoEvents` de trabalho da IDE em outra thread.

**Recuperação antes do reteste.** `Recuperar operação interrompida` apurou `Discard` sobre o
envelope `Partial/NotStarted` (`StageFailed`) e encerrou em `Completed`, com o mesmo
`OperationId` e nada apagado. O diálogo listou o Folder e o SDT como «previsto: Update», embora
os dois tenham sido criados nesse Apply; a causa e a correção proposta ficaram registradas em
`B130`, fora desta frente, para não mudar os writers no meio da medição. DLL instalada conferida
por `Test-InstalledExtension.ps1` (`InstalledMatchesBuild : True`), já com as sondas das seis
etapas.

## 7. Retestes sem reprodução (2026-09-25)

Mesma DLL da seção 6 (sondas nas seis etapas), `fabricabrasil18test`, `Empresa`, mesmas opções.

**Reteste 1 — depois da recuperação.** `SuccessWithWarnings`, `Criados=49`, `Atualizados=4`,
`Bloqueados=0`, 54 recibos confirmados, diário `Completed/Completed`, um `API.Save()` pelo
writer List, 1 min 53 s. O SDT `sdtEmpresa_API_CreateRequest_CriacaoVolumes`, que ficara da
tentativa falhada, foi **reencontrado sem divergência**: aquele `Save()` tinha sido completo, e o
que falhou foi só a releitura de confirmação. Reforça que o defeito está em **ler** o objeto, não
em gravá-lo.

Efeito colateral no Folder: o `EmpresaOpenApi`, criado pelo Apply interrompido antes da metadata,
foi tratado como reutilizado e perdeu a posse; o Remover seguinte o preservou («reutilizado; nunca
apagar»). Registrado em `B130`, cuja urgência subiu para média.

**Reteste 2 — mesmas condições da falha.** Remover (`Removed`, 50 removidos, Folder preservado
pelo motivo acima), Folder apagado à mão, IDE fechada e reaberta, Wizard logo em seguida:
`SuccessWithWarnings`, `Criados=51`, `Atualizados=3`, `Bloqueados=0`, 56 recibos confirmados,
diário `Completed/Completed`, 2 min. O aviso `Could not refresh Stencil 'GAM_FooterEntry'`
apareceu entre a abertura do Wizard e o Apply **e também durante a etapa de Business
Component**, sem falha — a coincidência com o aviso deixa de sustentar hipótese.

**Balanço do dia.** Três Applies da `Empresa` sob condições equivalentes, uma falha, no primeiro
SDT. O ramo C passa a ser tratado como o ramo A: **condicionado à reprodução**, agora com captura
completa (inner no `Detail` e stack pela sonda em todas as etapas). Hipóteses em aberto:
reentrância por `Application.DoEvents()` durante a releitura e enumeração concorrente por trabalho
da IDE em outra thread; a stack da próxima ocorrência separa as duas.

## 8. Segunda reprodução, com stack completa (2026-09-25, tarde)

DLL do commit `f033c6d` (sondas nas seis etapas). Sequência na `fabricabrasil18test`, `Empresa`:
`Build All` nos dois environments com sucesso; KB fechada e reaberta; Remover com sucesso; KB
fechada e reaberta; Wizard com as mesmas opções.

SDTs (43 criados), Procedures (5), Business Component e List passaram; o API Object foi gravado
(`ApiSaveCount=1`, writer List). A falha veio na **etapa de metadata**, antes do `Save()` do File:
`Resultado='Interrupted'`, `Criados=50`, `Bloqueados=1` (`apiEmpresa_Metadata`).

Stack publicada pela sonda (resumida; o registro completo está na Output da sessão):

```text
exceção: Artech.Udm.Framework.Exceptions.UdmException: Unable to Deserialize Data.
  at Artech.Udm.Framework.Entity.EnsureDeserialization()
  at Artech.Architecture.Common.Objects.KBObjectPart.OnPartRead()
  ...
  at Artech.Genexus.Common.Objects.SDT.get_SDTStructure()
  at Artech.Genexus.Common.Types.SDTTypeInfo..ctor(SDT sdtObject)
  at Artech.Genexus.Common.Objects.Sdt.SDTLoader.GetDatatype(KBModel, Boolean, String)
  ...
  at Artech.Genexus.Common.Types.DataType.ParseInto(KBModel, String, ITypedObject)
  at ApiPlanListProcedureWriter.MatchesVariableSpec(..., API api, VariableSpec variable)
  at ApiPlanListProcedureWriter.HasExpectedVariables(...)
  at ApiPlanListProcedureWriter.IsB070ApiObject(...)
  at ApiPlanBusinessComponentWriter.IsManagedApiObject(...)
  at ApiPlanApiObjectWriter.DiagnoseOwnership / IsOwnedApiObject / IsOwnedApiObjectForIntentionalWrite
  at ApiPlanApiObjectWriter.PreflightExistingApiObjectForMetadataRefresh(...)
  at ApiPlanMetadataFileWriter.PreflightApiObject / CreateOrReencounter(...)
  at Package.TryWriteMetadataFile(...)
inner 1: System.InvalidOperationException: Collection was modified; enumeration operation may not execute.
  at System.Collections.Generic.List`1.Enumerator.MoveNext()
  at Artech.Common.Properties.PropertyManager.SetInitialValues()
  at Artech.Common.Properties.PropertiesObject..ctor()
  at Artech.Udm.Framework.Entity..ctor(UdmKnowledgeBase kb, Model model, Guid typeId)
  at Artech.Genexus.Common.Parts.SDT.SDTItemEntity..ctor(SDTStructurePart part)
  at Artech.Genexus.Common.Parts.SDT.SDTItem.ReadItemInfo(XmlNode node)
  ...
  at Artech.Genexus.Common.Parts.SDTStructurePart.DeserializeData(BinaryStream data)
  at Artech.Udm.Framework.Entity.EnsureDeserialization()
```

**Leitura.**

- A falha está **dentro do SDK**, na desserialização sob demanda da estrutura de um SDT: cada
  item vira uma `SDTItemEntity`, cujo construtor percorre uma `List<>` em
  `PropertyManager.SetInitialValues()`, e essa lista muda durante a enumeração. A extensão só
  dispara a leitura — aqui, ao resolver por nome o tipo de uma variável do API Object
  (`DataType.ParseInto`) na verificação de posse que precede a gravação da metadata.
- Isso explica a dispersão das ocorrências: releitura de SDT, releitura de Procedure grande,
  etapa de List, `Save()` de API. Todos os caminhos resolvem tipos SDT; a falha aparece onde a
  primeira desserialização calhar.
- **Ramos A e C são o mesmo defeito**: o A é o `Collection was modified` chegando sem camada
  externa; o C, a mesma exceção embrulhada (`TargetInvocationException`, `UdmException`).
- **Reentrância por `Application.DoEvents()` praticamente descartada para esta ocorrência**: a
  cadeia é síncrona do `TryWriteMetadataFile` até o `MoveNext()`, sem frame de interface. Restam
  duas hipóteses, sem decisão: (i) outra thread da IDE alterando a mesma lista de definições de
  propriedades, que parece compartilhada e sem trava; (ii) o corpo do laço alterando a própria
  lista numa primeira inicialização. A (ii) casa mal com a intermitência.
- Padrão de campo: as duas falhas de 2026-09-25 vieram no primeiro Apply depois de reabrir a KB,
  quando nada estava desserializado; um dos Applies bem-sucedidos também veio logo após
  reabrir. É tendência, não regra.

**Achado colateral 1 — diário `Completed` com metadata ausente.** No Wizard, o retorno de
`TryWriteMetadataFile` é ignorado e o `CompleteJournal` roda em seguida; no Sync, a mesma falha
lança `SYNC_METADATA_FAILED`. O relatório disse `Interrupted`, o diário `Completed/Completed`.
Efeito observado: depois de reabrir a IDE, `Recuperar operação interrompida` respondeu «A última
operação registrada está encerrada. Não há nada a recuperar» — com a KB no estado `B115` (API
Object, Procedures, SDTs e Folder, sem `apiEmpresa_Metadata`). Pode ser escolha da F3 — depois do
API gravado não há o que a recuperação refaça —, mas o diário deixa de sinalizar o estado que o
`B110`/`B115` descrevem. A conferir contra o plano da F3 antes de classificar como defeito. A
saída prevista para esse estado é a recuperação de metadata órfã, oferecida na **abertura do
Wizard**, e não o comando `Recuperar`.

**Achado colateral 2 — `B130` também em operação concluída.** O inventário desse Apply, concluído,
lista `Folder EmpresaOpenApi — previsto: Update`, embora o Folder tenha sido criado nesse Apply (o
Remover anterior o apagara, por ter posse). SDTs, Procedures e API Object saem como `Create`. O
Folder, ao que parece, nunca entra na lista de criados do relatório. Somado ao `B130`.

**Decisão da sessão.** Opção escolhida para mitigar: **repetir a leitura** quando uma leitura
(confirmação, preflight, verificação de posse) receber `UdmException` com inner `Collection was
modified`, relendo depois de pausa curta, com número máximo de tentativas e linha de medição na
Output; nunca repetir `Save()`. Desenho e diff a aprovar antes da implementação.

## 9. Mitigação: repetição de leitura (implementada offline em 2026-09-25)

**Leitura do SDK, só leitura, antes do código.** Por reflexão sobre as DLLs da instalação:

- `Artech.Udm.Framework.Entity.EnsureDeserialization` tem no disco o corpo `nop nop nop ret`: o
  assembly é protegido e o código real é trocado em tempo de execução. Não foi possível saber
  estaticamente se uma desserialização interrompida deixa a parte marcada como carregada. A
  interface `IEntityDeserializationStatus` (`DeserializedData`, `DeserializedProperties`) existe,
  o que indica que o estado é rastreado, mas o momento da marca não é observável daqui.
- `Artech.Common.Properties` não é protegido e confirma o mecanismo. O construtor sem parâmetros
  de `PropertiesObject` chama `SetExtendedType` → `LoadPropDefinition`, que busca a coleção de
  definições num cache **estático por tipo** (`ConcurrentDictionary.GetOrAdd`): todas as
  instâncias de um tipo — por exemplo, todas as `SDTItemEntity` — compartilham a mesma
  `PropDefinitionCollection`. `PropertyManager.SetInitialValues()` percorre essa coleção **sem
  trava**; `PropertyManager.AddDefinition` a altera sob `Monitor.Enter(this)`, trava **da
  instância**, que não protege as demais. Qualquer `AddDefinition` de outra instância durante a
  enumeração produz o `Collection was modified`. O corpo do laço de `SetInitialValues` só escreve
  em `m_InitializedDefinitions`, outra lista: a hipótese de o laço alterar a própria coleção
  perde força, e a de outra thread da IDE ganha.

**Desenho.** `ApiPlanSdkReadRetry`, no mesmo arquivo do núcleo do seam e sem dependência do SDK:

- critério estreito: a cadeia contém `Artech.Udm.Framework.Exceptions.UdmException` **e** uma
  `InvalidOperationException` com `PropertyManager.SetInitialValues` na stack; nenhum outro erro
  é repetido (**ampliado na mesma data** para dispensar a `UdmException`; ver a seção 10);
- até 3 tentativas, pausas de 200 ms e 500 ms, sem `DoEvents`;
- cada recuperação ou esgotamento vira uma linha `[B109] Leitura repetida: Ponto='…',
  Tentativa=n/3, Resultado=Recuperada|Esgotada`, publicada na Output no início do relatório final;
- esgotadas as tentativas, a exceção original segue, com a stack pela sonda.

**Onde se aplica — só leituras; `Save()` nunca é repetido:**

- confirmação pós-Save, num ponto só: `ApiPlanPersistenceCore.Persist` relê o `confirm()` quando
  ele volta `Unreadable` com causa que casa — cobre as catorze confirmações do seam;
- resolução de tipo por nome, `DataType.ParseInto`: uma chamada no writer de Business Component,
  seis no de List, uma no de SDT;
- estrutura de SDT, `SDTStructure.Root`: três no writer de SDT, uma no Sync, uma no leitor do
  contrato existente.

Fica fora, declarado: falha dentro do próprio `Save()` (o SDK também resolve tipos ali) e a
confirmação do caminho sem log de persistência do executor, que não ocorre no Apply, no Sync nem
no Remover.

**Risco residual.** Como a marca de desserialização não é observável, uma leitura repetida
poderia, em tese, ver uma estrutura incompleta sem erro. As comparações que seguem são estritas:
estrutura incompleta aparece como `Divergent` e bloqueia, em vez de passar calada. A linha
`Resultado=Recuperada` na Output é o que permite auditar cada caso.

**Gates.** `Tests/PersistenceProbe/Test-ApiPlanPersistenceCore.ps1`: critério com cadeias
fabricadas com os nomes reais do SDK (casa com `UdmException` → `SetInitialValues`, casa
embrulhada em `TargetInvocationException`, não casa com `Collection was modified` de outra origem
nem sem `UdmException`); recuperação na segunda tentativa; esgotamento na terceira com a exceção
original; outro erro sem repetição; confirmação recuperada e esgotada; e o seam relendo a
confirmação sem repetir o delegate físico. `Test-ApiPlanPersistenceSeamCoverage.ps1`: nenhuma
chamada a `DataType.ParseInto` ou `SDTStructure` fora da repetição (13 encontradas). Build
canônica com 0 avisos; satélite U13 com 0 erros.

**Validação na IDE pendente.** Instalar a DLL e repetir o ciclo que reproduziu — Remover, reabrir
a KB, Wizard — observando as linhas `[B109] Leitura repetida`. Ausência de linha é ausência de
corrida, não prova da mitigação.

**Primeira rodada na IDE com a DLL da mitigação (2026-09-25).** Antes da rodada, o usuário apagou
à mão a API da `Empresa` e o File do diário, e fechou a IDE. Reabertura e Wizard com as mesmas
opções: `SuccessWithWarnings`, `Criados=51`, `Atualizados=3`, `Bloqueados=0`, 56 recibos
confirmados, diário recriado (`Created=True`, `FileId=148`) e `Completed/Completed`. Nenhuma linha
`[B109] Leitura repetida`: a corrida não ocorreu e a mitigação não foi exercida; a rodada prova só
que o caminho normal não regrediu. Observação sem hipótese: 29,8 s, contra 110 s a 120 s dos
Applies bem-sucedidos do dia; abertura do contrato em 570 ms, contra 1,6 s a 2,3 s; nenhum aviso
de Stencil — mas o reteste 1, de 113 s, também não teve aviso.

## 10. Unificação e ampliação do critério (2026-09-25)

**Um defeito só.** O `B109` deixa de ser família de ramos:

- antigo **ramo C** (confirmação `Unreadable` com `TargetInvocationException` ou `UdmException`):
  reproduzido duas vezes, com stack na segunda — é a corrida do SDK;
- antigo **ramo A** (`Collection was modified` sem embrulho; quatro ocorrências em 2026-09-04 e
  2026-09-05, sem stack): atribuído à mesma corrida **por hipótese** — mesma exceção, mesmos
  caminhos (Business Component, List, `Save()` de API). A hipótese própria dele, reentrância por
  `Application.DoEvents()`, perdeu a base com a stack síncrona da seção 8;
- antigo **ramo B** (`ValidationException` em `KBObjectManager.PrepareSave`): ocorrência distinta,
  encerrada em 2026-09-05, fora da família.

Um `Collection was modified` fora do `SetInitialValues` seria outro defeito, com item próprio. A
sonda, instalada em todas as etapas, mostra de onde vem.

**Critério ampliado.** As ocorrências do antigo ramo A chegaram sem o embrulho `UdmException`. Com
o critério original, a mesma corrida nesse formato não seria repetida. O critério de
`ApiPlanSdkReadRetry.IsSdkDeserializationRace` passou a exigir só uma `InvalidOperationException`
com `PropertyManager.SetInitialValues` na stack, em qualquer nível da cadeia. O frame identifica a
corrida; o embrulho não acrescentava segurança. Continua sem repetição um `Collection was
modified` de outra origem, e `Save()` continua nunca repetido. O gate do núcleo ganhou o caso da
corrida sem embrulho (repete) e manteve o de `Collection was modified` sem o frame (não repete).
Build canônica com 0 avisos; satélite U13 com 0 erros.
