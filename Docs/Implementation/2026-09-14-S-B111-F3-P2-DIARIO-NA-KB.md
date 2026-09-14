# S-B111 · F3 — P2: o diário durável passa a existir na KB

**Data:** 2026-09-14. **Sprint:** `S-B111`. **Fase:** F3, etapa P2.
**Plano governante:** [`2026-09-04-B111-F3-PLANO-DURABILIDADE-E-REMOCAO.md`](2026-09-04-B111-F3-PLANO-DURABILIDADE-E-REMOCAO.md), seções 4.4 e 5.3.
**Etapa anterior:** [`2026-09-14-S-B111-F3-P0-P1-IMPLEMENTACAO-OFFLINE.md`](2026-09-14-S-B111-F3-P0-P1-IMPLEMENTACAO-OFFLINE.md).

Esta é a primeira etapa da F3 que **escreve na KB**. O diário deixa de ser um contrato de
dados e passa a ser um File real, gravado antes de qualquer objeto de negócio em Apply e
Sync. O primeiro Apply real foi executado na IDE em 2026-09-14 e está na seção 6.1; os
demais testes do roteiro da seção 6 continuam pendentes.

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

Na conclusão, quando o API Object foi persistido, a fronteira `ApiPhysicallySaved` é
registrada antes do terminal. O GUID vem, nesta ordem, do contexto transitório da F1, do
objeto principal persistido no relatório final, ou do plano — o que existir primeiro.

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

Precisa de DLL instalada. O item 1 foi executado (seção 6.1); os demais seguem pendentes:

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
- **Sem localização trilíngue.** As mensagens do diário estão em português, direto no
  código. A tradução é a P7.
- **O Apply que não grava API** — só SDTs e Procedures — faz três checkpoints, não quatro,
  porque não existe fronteira de API a registrar. A matriz fala do «caminho completo»; este
  não é.
- **Nenhuma atomicidade.** Uma falha de checkpoint depois do primeiro `Save()` de negócio não
  desfaz nada; ela impede continuar às cegas e preserva o último snapshot durável. Continua
  valendo o que o checkpoint operacional já registra sobre gravações multiobjeto.

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
