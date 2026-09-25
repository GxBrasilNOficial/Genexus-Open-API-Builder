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

## 2. Estado atual da KB (conferido em 2026-09-25)

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

## 5. O que continua aberto

- A **causa** da `TargetInvocationException`. Hipótese não testada: a releitura em
  `ConfirmProcedure` (`Procedure.Get`, leitura de `Variables`, resolução de tipos por
  `TrySetVariableType`) sobre uma Procedure de ~153 KB. Sem o inner, qualquer hipótese é
  palpite.
- **Validação na IDE desta correção.** Não há como provocar a falha por clique. A correção só
  prova seu valor na próxima ocorrência, que deve trazer na Output a cadeia completa sob
  `[B109]`.
- **Próximo passo sugerido:** instalar a DLL com esta correção, repetir o Apply da `Empresa` na
  `fabricabrasil18test` (Remover e Wizard) algumas vezes e, se a falha voltar, ler o inner. Se
  não voltar, o ramo C fica como o ramo A: condicionado à reprodução, com a captura já pronta.

Manifesto e registro da extensão não mudaram; a instalação da DLL não exige `genexus /install`.
