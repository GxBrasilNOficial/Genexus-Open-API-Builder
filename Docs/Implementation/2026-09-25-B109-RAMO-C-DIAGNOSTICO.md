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
