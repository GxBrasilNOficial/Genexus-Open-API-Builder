# S-B111 · F3 — P2: o diário durável passa a existir na KB

**Data:** 2026-09-14. **Sprint:** `S-B111`. **Fase:** F3, etapa P2.
**Plano governante:** [`2026-09-04-B111-F3-PLANO-DURABILIDADE-E-REMOCAO.md`](2026-09-04-B111-F3-PLANO-DURABILIDADE-E-REMOCAO.md), seções 4.4 e 5.3.
**Etapa anterior:** [`2026-09-14-S-B111-F3-P0-P1-IMPLEMENTACAO-OFFLINE.md`](2026-09-14-S-B111-F3-P0-P1-IMPLEMENTACAO-OFFLINE.md).

Esta é a primeira etapa da F3 que **escreve na KB**. O diário deixa de ser um contrato de
dados e passa a ser um File real, gravado antes de qualquer objeto de negócio em Apply e
Sync. **A etapa foi validada na IDE em 2026-09-14**, nos quatro fluxos que ela cobre — Apply
de criação, Apply de reencontro, Apply abortado com bloqueio da operação seguinte e Sync com
delta —, em duas KBs. As seções 6.1 a 6.8 registram cada execução, incluindo os três defeitos
encontrados e corrigidos no caminho.

## 1. O que entrou

| Componente | Responsabilidade |
|---|---|
| `ApiPlanOperationJournalCheckpoints` | máquina de transições: para onde o envelope vai a cada fronteira, e o que o contrato recusa. SDK-free |
| `ApiPlanOperationJournalPlans` | montagem do bloco `plan` para geração, remoção e recuperação. SDK-free |
| `ApiPlanOperationJournalStore` | o File na KB: localizar, criar, gravar, reler por `FileId` e confirmar por bytes e hash |
| `ApiPlanOperationJournalSession` | conduz a operação: decide se ela pode começar, grava cada checkpoint e trava quando a durabilidade deixa de ser confirmável |

A separação entre a máquina e o armazenamento não é estética: é o que permite exercitar a
matriz de checkpoints offline, sem KB, e deixar para a IDE só o que de fato depende dela.

## 2. A política de checkpoints, como implementada

| Operação | CP1 | CP2 | CP3 | CP4 | Físicos |
|---|---|---|---|---|---|
| Apply / Sync | `Prepared` + `Pending/IntentionRecorded` | `Active` + `Running/NotStarted` | `Running/ApiPhysicallySaved` | terminal | 4 |
| Remove | `Prepared` + `Pending/IntentionRecorded` | `Active` + `Running/RemovalInProgress` | uma por passada | terminal | 3 + P |
| Recovery autônomo | `Prepared` + `Pending/IntentionRecorded` | `Active` + `Running/MetadataPending` | `Running/MetadataRecovered` | terminal | 4 |

Cada célula é exatamente um `File.Save()` seguido de releitura por `FileId` e validação. Não
há gravação implícita entre os pontos, e o `Save()` do diário não entra na contagem de
persistências de negócio da F2 — são duas contagens diferentes, e misturá-las tornaria o
relatório mentiroso.

**Remove e Recovery estão implementados na máquina, mas ainda não acionados**: a integração
do Remove é a etapa P4, e a do Recovery, a P5. O que existe hoje em produção é Apply e Sync.

## 3. Onde o diário entra no fluxo

Em `Package.cs`, nos dois fluxos que gravam:

- **Apply do Wizard** — logo depois do preflight agregado aprovado e **antes** da habilitação
  de Business Component, que é a primeira gravação de negócio do caminho. Se o diário não
  abrir, o Apply é bloqueado com relatório e **nada** é gravado;
- **Sincronizar** — depois do preflight de sincronização aprovado, antes dos SDTs.

Os dois fecham o diário em todas as saídas: conclusão normal (`Completed`), falha de etapa
(`Partial` + `blockReason=StageFailed`) e aborto do usuário (idem). A sessão vive fora do
`try` justamente para que o aborto seja registrado em vez de desaparecer com o escopo.

**Remissão — 2026-09-14:** o aborto do usuário deixou de compartilhar o motivo da falha de
etapa. A decisão 58 acrescentou `UserAborted` ao enum, e é ele que o Apply e o Sync gravam
nessa saída — ver a seção 6.5 deste documento e a seção 4 da P3.

Na conclusão, quando o API Object foi persistido, a fronteira `ApiPhysicallySaved` é
registrada antes do terminal. O GUID vem, nesta ordem, do contexto transitório da F1, do
objeto principal persistido no relatório final, ou do plano — o que existir primeiro.

**Remissão — 2026-09-14:** «quando o API Object foi persistido» era a intenção, mas a
implementação da P2 usava a existência de um GUID como prova de gravação, e a cadeia acima
sempre devolvia algum. Medido na validação da P3: um Apply sem gravação de API registrava a
fronteira com `ApiSaveCount=0`. Desde a correção, a fronteira exige gravação confirmada, e a
cadeia de GUID serve apenas para dizer **qual** identidade registrar — nunca **se** registra.
Detalhe em [`2026-09-14-S-B111-F3-P3-GATE-ESTENDIDO.md`](2026-09-14-S-B111-F3-P3-GATE-ESTENDIDO.md),
seções 6.7 e 9.

## 4. Duas decisões que a implementação forçou

### 4.1 Identidade da API no CP1 de uma criação nova

A decisão 24 diz que o plano de Apply e Sync «exige `plannedApiGuid`». Na F1, porém, a
identidade **é lida do `API.Create`**, que acontece dentro do pipeline — depois do envelope.
Numa criação nova não existe GUID nenhum no momento de registrar a intenção.

Exigi-lo no CP1 tornaria impossível abrir o diário para qualquer API nova; ignorá-lo
apagaria a regra. A validação passou a exigir o campo **a partir do estágio em que a
identidade tem de existir** — `ApiPhysicallySaved`, `ApiSaveOutcomeUnknown` e `Completed` —,
que é o mesmo momento em que a decisão 50 manda bloquear: antes do write do API. Um GUID
vazio continua inválido em qualquer estágio, e a sessão recusa trocar uma identidade já
registrada por outra.

### 4.2 O Remove registra a intenção com o inventário

A validação exige inventário não vazio no plano de remoção, e o CP1 do Remove é justamente
«registrar a intenção de remoção, com o conjunto completo de alvos validados» (seção 4.3 do
plano). A criação do envelope passou a aceitar o inventário, senão a própria regra seria
incumprível. Isso apareceu no gate, não na IDE.

## 5. Custo

Quatro gravações de `WikiFileKBObject` por Apply ou Sync. Pelas sondas de 2026-09-04:
**~130 ms** cada na KB pequena e **~1,1 s** na KB grande — ou seja, cerca de **0,5 s** e
**4,4 s** de acréscimo, respectivamente. É o orçamento declarado na seção 4.4 do plano, e a
medição real contra ele é parte da validação da seção 6.

Localizar o diário usa o índice já montado (0 ms) e as releituras vão por `FileId` (0 ms). O
índice nunca é remontado para reencontrá-lo: isso custaria ~3,1 s na KB grande.

## 6. Validação na IDE — roteiro

Precisa de DLL instalada. **Todos executados e aceitos em 2026-09-14** (seções 6.1 a 6.8),
em duas KBs: `wsEducacaoSpTeste` (pequena) e `FabricaBrasil18Test` (grande, 52 objetos
planejados e 13 subníveis). O item 5 — desbloqueio pela ferramenta — não é executável nesta
etapa, porque o comando de recuperação é a P6; o contorno manual foi exercido duas vezes.

1. **Apply completo numa Transaction nova.** Conferir na Output as linhas `[B111/F3]`: diário
   aberto com `FileId`, checkpoints `Prepared`→`Active`→`ApiPhysicallySaved`→`Completed`. Na
   KB, o File `GxOpenApiBuilder_OperationJournal` deve existir, com Description própria e o
   arquivo externo `GxOpenApiBuilder_OperationJournal.json`. Exportar e conferir o JSON:
   `operationState=Completed`, `logicalStage=Completed`, `journalDurability=Confirmed`.
2. **Segundo Apply na mesma KB.** O mesmo File é reutilizado — não pode surgir um segundo
   diário — e o `operationId` muda.
3. **Sync com delta.** Mesma sequência, com `operationKind=Sync`.
4. **Aborto durante o Apply.** O diário deve terminar em `Partial` com
   `blockReason=StageFailed`, e a **operação seguinte deve ser bloqueada** com a mensagem de
   estado não terminal. Esse é o teste que prova que o diário serve para alguma coisa.
5. **Desbloqueio.** Hoje, a única saída de um envelope `Partial` é apagar o File do diário à
   mão. A saída pela ferramenta é o comando de recuperação, que é a etapa P6 — ver a seção 7.
6. **Custo medido** na KB grande, comparado com os ~4,4 s previstos.

## 6.1 Primeira execução real — `Escola`, 2026-09-14

Apply completo de criação nova na KB `wsEducacaoSpTeste`, DLL instalada:

| Observação | Valor |
|---|---|
| Diário criado | `FileId=84`, `Created=True`, 987 bytes no `Prepared` |
| Checkpoints | 4, `Durability=Confirmed` |
| Custo | **314 ms** — abaixo dos ~520 ms previstos para a KB pequena |
| Estado final | `Active` / `Completed` / `Completed`, `blockReason: null` |
| Metadata | `GOAB_API_METADATA_B060_V3` na primeira gravação |
| F1 preservada | `ApiSaveCount=1`, `FinalWriter='List'` |

O cruzamento que só existe com as duas gravações reais — critério de aceite 8 — fechou:
`journal.applicationId` e `metadata.ownership.applicationId` são o mesmo
`46e63949-c26e-4daa-9a57-60fb0c6a3bf5`, e `plan.plannedApiGuid` é o
`34491e46-4ca0-4d07-80ca-304d8aad5433` do API Object. O `contractHash` do plano é o mesmo
`D39DCB84…` que o B067 gravou como `PlannedContractHash`.

A forma canônica se confirmou no arquivo gravado: 1022 bytes no snapshot terminal, sem BOM,
sem CR, sem espaço após dois-pontos, `abandonment` e `blockReason` como `null`.

**A execução também revelou uma lacuna**, invisível na Output e visível no JSON exportado:
`inventory` e `receipts` saíram vazios. A matriz 4.4 manda gravar os recibos nas fronteiras,
e a implementação inicial da P2 gravava só as dimensões de estado. Foram 18 recibos
confirmados que não ficaram duráveis. Corrigido na mesma data — seção 6.2.

## 6.2 Recibos e inventário no envelope

`ApiPlanOperationJournalReceiptMapper` leva os `PersistenceReceipt` da F2 para o envelope e
deriva o inventário deles. A sessão reconstrói os dois a cada checkpoint: o snapshot
descreve o estado observado naquele instante, não um acúmulo.

Três decisões que a conversão forçou:

- **o mapa de tipos é fechado.** `Transaction`, `Folder`, `API`, `Procedure`, `SDT` e `File`
  têm correspondência no schema; qualquer outro vira exceção, que a sessão converte em
  bloqueio visível do diário. Gravar um recibo com tipo inventado seria pior que não gravar;
- **`Create` contra `Update` vem da lista de criados do relatório final.** O recibo prova que
  o alvo foi gravado, não que ele nasceu agora. O collector passou a expor
  `CreatedObjectNames` para esse cruzamento;
- **`emptyConfirmed` deixou de ser exigido fora da remoção.** A regra da decisão 24 é da fila
  destrutiva — só se apaga um Folder próprio depois de confirmá-lo vazio. Num Apply, o mesmo
  Folder aparece como alvo de criação ou reuso, e exigir vazio ali não teria sentido.

A identidade de File da F2 é o GUID do objeto, não o `Id` numérico; o item de inventário
registra `identityKind=Guid` com o hash esperado ao lado. Gate: `tests.operationJournalReceipts`.

## 6.3 Segundo Apply — três defeitos meus, corrigidos

O reencontro da `Escola`, minutos depois, **falhou**: um diálogo de erro com
`composite.apiGuid é obrigatório` quatro vezes e `inventory repete o mesmo alvo` cinco. Os
objetos de negócio já estavam todos gravados — SDTs, Procedures, API, metadata e integridade
— e o relatório final não chegou a aparecer.

O defeito mais grave não é nenhuma das duas mensagens:

1. **O diário derrubou a operação.** O serializer recusa o envelope inválido antes do
   `File.Save()`, como manda o contrato, mas a sessão não tratava essa recusa e a exceção
   subia até o comando. Um instrumento de diagnóstico pode bloquear a si mesmo; não pode
   levar junto a operação que observa, ainda mais depois de tudo gravado. A sessão passou a
   capturar qualquer falha de gravação do diário e convertê-la em bloqueio visível, com o
   relatório final preservado. O mesmo vale para a abertura.
2. **`composite.apiGuid` era exigência indevida num Save.** O writer de Business Component
   identifica a Procedure por `CompositeIdentity`, e no reencontro o GUID chega vazio. A
   identidade histórica completa é exigência de quem **autoriza exclusão** — «nome isolado
   nunca autoriza exclusão» —, não de quem registra um recibo de gravação. Os GUIDs e a
   Description canônica passaram a ser exigidos apenas quando `action=Delete`.
3. **A chave de agregação do inventário divergia da chave de unicidade do validador.** O
   mapeador agrupava por identidade da F2; o validador comparava por tipo, espécie de
   identidade e nome. Dois recibos do mesmo alvo com identidades diferentes — antes e depois
   de o objeto existir — viravam dois itens que colidiam. As duas chaves passaram a ser a
   mesma função, exposta pelo validador.

Os três estão cobertos por teste: o gate de recibos reproduz o cenário exato do reencontro —
identidade composta com `transactionGuid` preenchido e `apiGuid` vazio, o mesmo alvo com duas
identidades — e exige envelope válido no Save e bloqueio no Delete. O primeiro defeito, por
depender do store e da KB, só se comprova na IDE: é o reteste do Apply de reencontro.

## 6.4 Terceiro Apply — recibos duráveis, e a ordem da F1 provada por dado

Reteste do reencontro da `Escola` com a DLL corrigida, depois de o File do diário ter sido
apagado à mão (contorno previsto até a P6). Resultado: `SuccessWithWarnings`, `Bloqueados=0`,
relatório final apresentado.

| Observação | Valor |
|---|---|
| Diário | `FileId=86`, `Created=True` (nasceu de novo após a limpeza) |
| Recibos duráveis | **12**, batendo com `PersistenceReceipts=12` do relatório |
| Inventário | **7** alvos |
| Checkpoints | 4, `Durability=Confirmed` |
| Custo | **305 ms**, com envelope de 7616 bytes |
| Estado final | `Active` / `Completed` / `Completed`, `blockReason: null` |

O custo praticamente não mudou em relação aos 314 ms do envelope de 1022 bytes. É a
confirmação em campo do que a sonda de 2026-09-04 mediu — 247 bytes e 20 KB com o mesmo
tempo de gravação —, e significa que carregar recibos no diário não compromete o orçamento
da seção 4.4.

`Recibos=12` contra `Atualizados=15` no relatório não é divergência: o recibo só existe onde
houve gravação física, e objetos reencontrados sem alteração não geram `Save`.

O inventário exportado:

| Alvo | Tipo | Ação | Estado | Confirmação | Recibos |
|---|---|---|---|---|---|
| `procEscola_API_List` | Procedure | Update | Present | Confirmed | 1, 10 |
| `procEscola_API_Get` | Procedure | Update | Present | Confirmed | 2, 6 |
| `procEscola_API_Create` | Procedure | Update | Present | Confirmed | 3, 7 |
| `procEscola_API_Update` | Procedure | Update | Present | Confirmed | 4, 8 |
| `procEscola_API_Delete` | Procedure | Update | Present | Confirmed | 5, 9 |
| `apiEscola` | ApiObject | Update | Present | Confirmed | 11 |
| `apiEscola_Metadata` | MetadataFile | Update | Present | Confirmed | 12 |

Cada Procedure carrega **duas** sequências — uma do estágio `Procedures`, outra do
`Business Component` ou do `List`. É exatamente o caso que a chave divergente de antes
transformava em alvo repetido, e que agora colapsa num item só.

**A ordem da F1 deixou de depender da Output.** As sequências mostram as cinco Procedures
(1–5), o Business Component (6–9), o List (10), o API Object (**11**) e a metadata (12). O
API é gravado depois de todos os consumidores, uma única vez, e isso está agora persistido
na KB por identidade, com o estágio que gravou cada um — não mais apenas afirmado por uma
linha de log e pelo `ApiSaveCount=1`.

## 6.5 Aborto e bloqueio — o teste que justifica a fase

KB grande `FabricaBrasil18Test`, Transaction `Empresa` (52 objetos planejados, 13 subníveis),
API criada no dia anterior. Duas execuções seguidas.

**Aborto.** O usuário abortou logo no início, antes de qualquer gravação:

| Observação | Valor |
|---|---|
| Relatório | `Aplicação abortada pelo usuário`, `Bloqueados=1` |
| Diário | `Partial` / `NotStarted`, `FileId=137` |
| Recibos e inventário | 0 e 0 — nada chegou a ser gravado |
| Checkpoints | 3, `Durability=Confirmed` |
| Custo | **125 ms** na KB grande |

`Criados=0`, `Atualizados=0`, `PersistenceReceipts=0`: a KB **não** ficou inconsistente
neste caso. O aviso do B082 sobre possível inconsistência é conservador e genérico.

**Bloqueio da operação seguinte.** Novo Apply na mesma Transaction, sem apagar nada:

```
Bloqueados (1):
  - [Diário de operação] B111/F3 — O diário da KB registra a operação Apply em estado
    Partial/NotStarted, que não é terminal. Reconcilie ou continue essa operação antes
    de iniciar outra.
```

`ApiSaveAttempted=False`, `Criados=0`, `Atualizados=0`, `PersistenceReceipts=0`, e **nenhuma**
linha de B040-B046 ou B050-B053 depois do bloqueio: a operação foi recusada antes da primeira
gravação, não interrompida no meio. É a prova de que o diário cumpre a função para a qual
existe — impedir que uma operação nova passe por cima de uma que ficou pela metade.

### O custo previsto está superestimado

125 ms para três gravações na KB grande, ou seja, ~42 ms por checkpoint — contra os ~1,1 s
por gravação que a sonda de 2026-09-04 mediu e que sustentam o orçamento de ~4,4 s da seção
4.4 do plano. A medição é parcial: envelope pequeno, três gravações, sem concorrência. O
fechamento do teste 4 exige um Apply completo na `Empresa`, com o envelope carregando os
recibos de ~50 objetos. Se confirmar essa ordem de grandeza, o orçamento do plano precisa ser
revisto — e a revisão é do orçamento, não da política de checkpoints.

### `blockReason=StageFailed` num aborto é impreciso

O mapeamento normativo da decisão 24 amarra `StageFailed` a «falha conhecida e não retryable
comunicada pelo orquestrador por `NoteStageFailed`». Um aborto do usuário não é isso. O enum
`blockReason` do schema V1 é fechado e não tem valor para interrupção deliberada, e
acrescentar um é mudança incompatível de schema, que exige decisão explícita.

Decisão desta etapa: **manter `StageFailed`**, registrando a imprecisão, e resolver na P3,
que é onde a precedência de motivos é normatizada. Inventar valor novo no meio da validação
seria pior que conviver com a imprecisão documentada.

**Resolvido na P3 — 2026-09-14:** o enum recebeu `UserAborted`, e o Apply e o Sync passaram a
gravá-lo no aborto do usuário. A janela de compatibilidade justificou a mudança de schema: o
V1 nunca saiu num release. Detalhe em
[`2026-09-14-S-B111-F3-P3-GATE-ESTENDIDO.md`](2026-09-14-S-B111-F3-P3-GATE-ESTENDIDO.md),
seção 4.

## 6.6 Custo na KB grande — o orçamento do plano está 24 vezes acima do medido

Apply completo de reencontro na `Empresa` da `FabricaBrasil18Test`, 47 SDTs e 4 Procedures
reencontrados, 53 objetos atualizados, sem aborto:

| Medida | Valor |
|---|---|
| Custo do diário | **182 ms**, 4 checkpoints (~45 ms cada) |
| Apply completo | 44.190 ms |
| Peso do diário no Apply | **0,4%** |
| Envelope | 6534 bytes, 10 recibos, 6 alvos |
| Orçamento da seção 4.4 | ~4.400 ms |

O aborto anterior, com três checkpoints, custou 125 ms — ~42 ms cada. Duas medições
independentes na mesma KB, com a mesma ordem de grandeza.

A seção 2.3 do plano da F3 registra «gravar um `WikiFileKBObject`: ~1,1 s na KB grande»,
medição de 2026-09-04 que sustenta o orçamento de ~4,4 s. A medição de campo diz ~45 ms. Não
investiguei a diferença e não afirmo a causa; o fato registrado é que o orçamento não
descreve o custo observado, e por uma margem grande demais para ser ruído.

O que isso muda: nada na política de checkpoints, que é contrato. Muda o risco «o Modo A
acrescentar segundos ao Apply de forma percebida como regressão», da seção 11 do plano — que
não se materializou. O plano recebeu nota de remissão nas duas seções.

Ressalva da medição: foi num **reencontro**, com 10 recibos. Uma criação nova na KB grande
teria ~50 recibos e envelope maior. A sonda original mediu 247 bytes e 20 KB com o mesmo
tempo de gravação, e os dois Applies da `Escola` (1022 e 7616 bytes, 314 e 305 ms)
confirmam isso; ainda assim, a medição com envelope grande continua não feita.

## 6.7 Reuso do File, e a prova incidental da promoção V3

Segundo Apply seguido na `Empresa`, sem apagar nada:

| Observação | Apply anterior | Este Apply |
|---|---|---|
| `Created` | True | **False** |
| `FileId` | 138 | **138** — o mesmo |
| `OperationId` | `9a9e3a9b…` | `ad9bfa1f…` — novo |
| `ApplicationId` | `5b8dc95a…` | `6973d4f4…` — novo |
| Custo do diário | 182 ms | 151 ms |
| Recibos / inventário | 10 / 6 | 10 / 6 |

O File é reutilizado e substituído, como manda a decisão 44: há **um** diário por KB, e o
envelope terminal anterior dá lugar ao novo. Cada operação recebe `operationId` e
`applicationId` novos, que é o contrato da matriz de identidade da seção 4.1.1 para «Apply
ou Sync novos».

Terceira medição de custo na KB grande: 125 ms (3 checkpoints), 182 ms e 151 ms (4
checkpoints). Consistentes entre si e uma ordem de grandeza abaixo do orçamento.

### A promoção V3 se provou sozinha, por acidente

Comparando a metadata gravada nos dois Applies:

| | Apply anterior | Este Apply |
|---|---|---|
| `Bytes` | 1344725 | 1344725 — **idêntico** |
| `Sha256` do File | `B2A6C311…` | `22F11F96…` — **diferente** |
| `PlannedContractHash` B067 | `41DBB659…` | `41DBB659…` — **idêntico** |

Mesmo tamanho, mesmo contrato, conteúdo diferente. A única coisa que mudou no payload foi
`ownership.applicationId` — um GUID trocado por outro, daí o tamanho idêntico. É exatamente o
que a decisão 24 afirma sobre a promoção V2→V3: «`ownership.applicationId` passa a integrar o
payload e o fingerprint, portanto um valor novo muda o fingerprint por definição».

E confirma, do outro lado, que o `PlannedContractHash` do B067 **não** inclui ownership —
razão pela qual o reencontro conservador não passou a bloquear com a promoção. Isso era uma
verificação de código feita na revisão pré-push da P0; agora é observação de campo.

## 6.8 Sync com delta — o último fluxo integrado

`Escola` da `wsEducacaoSpTeste`, com `EscolaEndereco` alterado de `VarChar(70)` para
`VarChar(71)`. Diff do B085: `Modificados: 1`, nenhum campo adicionado, nenhum conflito de
SDT.

| Observação | Valor |
|---|---|
| Diário | `OperationKind='Sync'`, `Created=False`, `FileId=86` reutilizado |
| Checkpoints | 4, `Completed/Completed`, `Durability=Confirmed` |
| Recibos / inventário | 12 / 7, batendo com `PersistenceReceipts=12` |
| Custo | **156 ms** sobre 4.358 ms de operação |
| Resultado | `SuccessWithWarnings`, `Atualizados=15`, `Bloqueados=0` |

O `PlannedContractHash` do B067 mudou de `D39DCB84…` para `1F87DD9E…`, como deve: o contrato
mudou junto com o campo. O diário registra esse mesmo valor em `plan.contractHash`, de modo
que envelope e metadata continuam concordando sobre qual contrato foi aplicado — agora
verificado nos dois fluxos que gravam.

Com isso, **os quatro fluxos que o diário cobre nesta etapa foram exercidos**: Apply de
criação, Apply de reencontro, Apply abortado e Sync. Remove e Recovery continuam fora, nas
etapas P4 e P5.

## 7. Riscos e lacunas assumidos nesta etapa

- **Um envelope não terminal trava a KB para novas operações, e ainda não há saída pela
  ferramenta.** É o comportamento correto segundo a decisão 44 — sobrescrever apagaria a
  prova do que ficou pela metade —, mas a recuperação explícita só chega na P5/P6. Até lá, o
  contorno é apagar o File do diário manualmente. Quem validar na IDE precisa saber disso
  **antes** de abortar um Apply de propósito.
- **O gate estendido da seção 4.2 ainda não existe.** O que a P2 implementa é a regra de
  reutilização da decisão 44, dentro da sessão. A precedência completa de `GateDiagnostic`
  (`JournalUnavailable`, `DurabilityUnknown`, `GateBlocked`, `PreconditionFailed`) com
  `reasonCode` estável é a P3; hoje o bloqueio chega como uma mensagem única no relatório.
  **Entregue na P3 — 2026-09-14**, ainda sem validação na IDE.
- **Sem localização trilíngue.** As mensagens do diário estão em português, direto no
  código. A tradução é a P7.
- **O Apply que não grava API** — só SDTs e Procedures — faz três checkpoints, não quatro,
  porque não existe fronteira de API a registrar. A matriz fala do «caminho completo»; este
  não é. **Remissão — 2026-09-14:** isto era o comportamento pretendido, não o medido. A
  validação da P3 exercitou esse caminho pela primeira vez e encontrou **quatro** checkpoints,
  com `ApiPhysicallySaved` registrada e `ApiSaveCount=0`: a fronteira dependia de um GUID
  conhecido, não de gravação confirmada. A afirmação acima só passou a ser verdadeira depois da
  correção registrada em
  [`2026-09-14-S-B111-F3-P3-GATE-ESTENDIDO.md`](2026-09-14-S-B111-F3-P3-GATE-ESTENDIDO.md),
  seções 6.7 e 9.
- **Nenhuma atomicidade.** Uma falha de checkpoint depois do primeiro `Save()` de negócio não
  desfaz nada; ela impede continuar às cegas e preserva o último snapshot durável. Continua
  valendo o que o checkpoint operacional já registra sobre gravações multiobjeto.

## 7.1 Condição para o próximo corte de release

A P2 muda o que o usuário final vê na própria KB, e a documentação pública ainda descreve a
Alpha sem o diário. **Não é gap de texto: é condição de publicação.**

O que muda para quem usa a extensão:

1. um objeto novo aparece na KB, `GxOpenApiBuilder_OperationJournal`, no Root Module, com
   Description própria e o arquivo externo `GxOpenApiBuilder_OperationJournal.json`. O
   `DEMO.md` hoje cita o File de preferências `GxOpenApiBuilder_Settings` e não este;
2. Apply e Sincronizar passam a poder ser **bloqueados** por um estado do diário, com uma
   mensagem que fala de «operação em estado não terminal» — vocabulário que não existe em
   nenhum texto público;
3. **enquanto a P6 não existir, a única saída desse bloqueio é apagar o File à mão.** Um
   usuário que aborte um Apply e não saiba disso fica com a KB travada para gerar, sem
   caminho pela ferramenta.

Por isso, ao preparar o corte, uma destas duas condições precisa estar satisfeita:

- **a P6 está entregue** e existe o comando explícito de recuperação; ou
- **`Docs/Public/DEMO.md` e os três `README` explicam** o File do diário, o bloqueio e o
  contorno manual, e as notas de release avisam do comportamento novo.

Publicar a P2 sem uma das duas entrega ao usuário um modo de travar a KB sem saída
documentada. O registro da pendência em mensagem de commit não basta, e é por isso que ela
está aqui.

## 8. Gates

Dois novos, ambos registrados no orquestrador e no teste do checker:

- `tests.operationJournalCheckpoints` — matriz de checkpoints por operação, transições
  recusadas, abandono explícito, identidade da API por estágio e reutilização do diário entre
  operações;
- `tests.operationJournalReceipts` — mapa fechado de tipos, recusa de tipo desconhecido,
  transporte dos recibos, identidades preservadas, `Create` contra `Update` e validação do
  envelope resultante pelo mesmo validador do schema.

A sentinela de cobertura do seam da F2 passou a aceitar o `file.Save()` do store como
chamada física fora do seam, com o motivo escrito na própria allowlist: o diário tem rotina
própria de durabilidade por decisão de contrato.

Verificação executada nesta rodada: build Release com 0 avisos e 0 erros; checker de
comandos; `tests.operationJournalSchema`, `tests.operationJournalCheckpoints`,
`tests.persistenceCore`, `tests.persistenceExecutor`, `tests.persistenceSeamCoverage`,
`tests.transactionSyncComparer`, `tests.applicationFinalReport`,
`tests.wizardContractExistingApiFilters` e o teste do checker pré-push.
