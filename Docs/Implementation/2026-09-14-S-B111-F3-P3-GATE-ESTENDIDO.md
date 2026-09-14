# S-B111 · F3 — P3: gate estendido e precedência de `GateDiagnostic`

**Data:** 2026-09-14. **Sprint:** `S-B111`. **Fase:** F3, etapa P3.
**Plano governante:** [`2026-09-04-B111-F3-PLANO-DURABILIDADE-E-REMOCAO.md`](2026-09-04-B111-F3-PLANO-DURABILIDADE-E-REMOCAO.md), seção 4.2.
**Etapa anterior:** [`2026-09-14-S-B111-F3-P2-DIARIO-NA-KB.md`](2026-09-14-S-B111-F3-P2-DIARIO-NA-KB.md).

A P2 deixou o diário gravando e bloqueando, mas o bloqueio chegava como **uma frase**. Quem
lê essa frase — o relatório final hoje, a reconciliação da P5 e o comando de recuperação da
P6 amanhã — não tem como distinguir «o diário não pôde ser lido» de «o diário está íntegro e
o estado impede a operação». São saídas diferentes: apagar, reconciliar ou continuar.

A implementação é offline. **A validação na IDE foi executada em 2026-09-14** e passou nos
cinco cenários do roteiro; um sexto cenário nasceu de um defeito encontrado na bateria e
continua pendente. Tudo na seção 6.

## 1. O que entrou

| Componente | Responsabilidade |
|---|---|
| `ApiPlanOperationJournalGate` | avalia se uma operação pode começar e, quando não pode, classifica o motivo. SDK-free |
| `ApiPlanOperationJournalGateDiagnostic` | o diagnóstico: código, `reasonCode`, pré-condição, contexto estruturado e mensagem humana |
| `JournalGateReasonCodes` | as razões estáveis e, em `OwnerOf`, o mapeamento fechado razão → código |
| `ApiPlanOperationJournalContinuationAuthorization` | o consentimento que a P6 vai preencher; nenhum chamador atual passa um valor |

O gate é SDK-free pelo mesmo motivo da máquina de checkpoints: o que depende da IDE é
localizar o File, e isso continua no store. O resultado da busca entra no gate já em forma
neutra (`JournalGateLookupState`), e é isso que torna a matriz de precedência exercitável
sem KB.

## 2. A precedência, como implementada

| Código | Quando | `reasonCode` emitidos hoje |
|---|---|---|
| `JournalUnavailable` | o diário não pode ser lido, validado ou identificado | `JournalDuplicate`, `JournalInvalid`, `JournalIdentityDivergent` |
| `DurabilityUnknown` | o snapshot não pôde ser confirmado | `JournalReloadDivergent`, `JournalSaveUnconfirmed` |
| `GateBlocked` | o diário é legível, válido e durável, e é o estado global que impede | `JournalNonTerminal`, `PreparedContinuationNotAuthorized`, `UnreconciledOutcome`, `RecoveryAuthorizationStale`, `RecoveryAuthorizationLockUnavailable` |
| `PreconditionFailed` | pré-condição específica falha antes da primeira mutação | nenhum ainda — pertence ao Remove (P4) e à recuperação (P5) |

`PreconditionFailed` **não** é subcausa de `GateBlocked`, e o gate mecânico trava isso dos
dois lados: cada razão de pré-condição pertence ao seu próprio código em `OwnerOf`, e nenhum
caminho que produz `GateBlocked` emite uma delas.

Que `PreconditionFailed` ainda não tenha emissor não é lacuna: suas razões
(`IdentityAmbiguous`, `IdentityDivergent`, `InventoryInsufficient`, `AuthorizationMismatch`)
descrevem pré-condições de operações que a P3 não integra. Elas estão no mapeamento fechado
para que a P4 e a P5 não inventem uma taxonomia paralela.

## 3. As cinco validações da seção 4.2, e o que faltava

| # | Validação | Antes da P3 | Agora |
|---|---|---|---|
| 1 | ausência de intenção anterior parcial, indeterminada ou ambígua | existia, sem código | `GateBlocked`/`JournalNonTerminal`, pré-condição `PriorIntentReconciled` |
| 2 | disponibilidade e integridade do diário | existia, sem código | `JournalUnavailable`/`JournalDuplicate` ou `JournalInvalid` |
| 3 | identidade, versão e durabilidade, sem divergência física | **parcial** | fechada — ver 3.1 |
| 4 | ausência de intenção ativa, ou transição autorizada de `Prepared` | **não distinguia** | ver 3.2 |
| 5 | ausência de diagnóstico irremediado e `OutcomeUnknown` não reconciliado | existia, sem código | `GateBlocked`/`UnreconciledOutcome` |

### 3.1 Um diário de outra KB passava

`schemaVersion` e `journalKind` já eram validados na leitura, e o store confirma cada
gravação por `FileId`, bytes e hash. Ninguém comparava o `knowledgeBaseGuid` **do envelope
lido** com a KB aberta. Um File do diário copiado entre KBs — ou restaurado de outro lugar —
era aceito como governante desta.

O gate passa a recusá-lo com `JournalUnavailable`/`JournalIdentityDivergent`, e a mensagem
nomeia as duas KBs. O caso é exercitado no gate mecânico com um envelope `Completed` e
durável: sem a checagem, ele liberaria a operação.

### 3.2 `Prepared` deixou de ser «mais um não terminal»

`EvaluateReuse` tratava `Prepared/Pending` e `Active/Running` com a mesma frase. A seção
4.1.1 os separa: `Prepared` é o único estado não terminal que admite continuação explícita,
porque nada de negócio foi gravado ainda.

Agora ele tem razão própria, `PreparedContinuationNotAuthorized`, e o input do gate carrega
a autorização que a P6 vai preencher. O comportamento observável **não muda** — continua
bloqueando —, mas três caminhos ficam distinguíveis e testados:

- sem autorização: `PreparedContinuationNotAuthorized`;
- autorização de **outra** operação: `RecoveryAuthorizationStale`, porque entre o
  consentimento e agora o envelope mudou;
- autorização coerente: `Outcome=ContinuationAuthorized`, que **não** é «pode começar».
  Continuar preservando `operationId` e `applicationId` é serviço da P5/P6; enquanto ele não
  existe, a sessão recusa com `RecoveryAuthorizationLockUnavailable`.

A regra tem um dono só: `EvaluateReuse` continua existindo como leitura simplificada, mas
delega ao gate, para que as duas não divirjam.

## 4. `UserAborted`: o enum V1 ganhou um valor

A seção 6.5 da P2 registrou a imprecisão: um aborto do usuário terminava com
`blockReason=StageFailed`, valor que a decisão 24 amarra a «falha conhecida e não retryable
comunicada por `NoteStageFailed`». Um aborto não é falha nenhuma.

**Decisão tomada nesta etapa: acrescentar `UserAborted` ao enum.** É mudança incompatível de
schema, e o que a torna barata agora é a janela: o schema V1 do diário **nunca saiu num
release** — o último publicado é o `0.1.0-alpha.7`, anterior à P0 —, e os únicos envelopes V1
existentes estão nas duas KBs de teste desta máquina. Depois do próximo corte, o mesmo
acréscimo exigiria schema V2 e caminho de migração.

A distinção não é cosmética: a reconciliação da P5 lê esse campo para decidir o que oferecer,
e o que se oferece a quem parou de propósito não é o que se oferece a quem quebrou.

Regra nova no validador: `UserAborted` exige `operationState=Partial`. Um aborto nunca produz
`OutcomeUnknown` — quem parou de propósito sabe que parou.

Os dois `catch (ApiPlanBusyAbortedException)` — Apply e Sync — passaram a gravá-lo. As demais
interrupções continuam `StageFailed`.

## 5. Onde o diagnóstico aparece

Apply e Sincronizar publicam a linha do diagnóstico na Output e no relatório final, no lugar
da frase única. A forma:

```
[GateBlocked/JournalNonTerminal] Pré-condição 'PriorIntentReconciled'. O diário da KB
registra a operação Apply em estado Partial/NotStarted, que não é terminal. Reconcilie ou
continue essa operação antes de iniciar outra. Contexto: lookupState=Found,
journalFileId=86, operationId=…, operationKind=Apply, envelopePhase=Active,
operationState=Partial, logicalStage=NotStarted, blockReason=UserAborted,
journalDurability=Confirmed.
```

O contexto é o que a decisão 24 manda carregar ao lado da mensagem. `reasonCode` e
`blockReason` continuam em namespaces separados no código: nada copia um para o outro.

## 6. Validação na IDE — cinco de seis executados

**Executada em 2026-09-14**, na KB `wsEducacaoSpTeste`, Transaction `Escola`, com a DLL desta
etapa instalada. Os cinco cenários do roteiro original passaram. Um sexto nasceu durante a
bateria, a partir de um achado, e está pendente — seção 6.7.

### 6.1 Aborto com `UserAborted`

Apply de reencontro, abortado cerca de três segundos depois de confirmado — mais tarde que o
aborto da P2, que parou antes de qualquer gravação.

| Observação | Valor |
|---|---|
| Diário | `Partial` / `NotStarted`, `FileId=86`, `Bytes=6421` |
| `blockReason` | **`UserAborted`** — era `StageFailed` na P2 |
| Checkpoints | 3, `Durability=Confirmed`, **219 ms** (~73 ms cada) |
| Recibos e inventário | 10 e 5 |
| Relatório | `Atualizados=13`, `ApiSaveAttempted=False`, `ApiSaveCount=0` |

O JSON exportado confirmou o valor persistido, e não apenas a linha da Output. As sequências
mostram Procedures (1–5), Business Component (6–9) e List (10): o aborto pegou depois do BC e
antes do writer final, então o API Object não chegou a ser gravado — a ordem da F1 se manteve
sob interrupção.

**Nota para a P5.** Os dois abortos medidos até agora — o da P2, com 0 recibos, e este, com 10
recibos e 13 objetos atualizados — terminaram no mesmo `logicalStage=NotStarted`. Não é
defeito: o estágio só muda em `ApiPhysicallySaved`, porque a política de quatro checkpoints
não grava a cada etapa, e isso foi decisão de custo. Mas significa que **a reconciliação não
pode decidir olhando só `logicalStage`**; quem distingue os dois casos é o inventário.

### 6.2 Apply bloqueado, com o diagnóstico classificado

Novo Apply sobre o envelope acima, sem apagar nada:

```
[GateBlocked/JournalNonTerminal] Pré-condição 'PriorIntentReconciled'. … Contexto:
lookupState=Found, journalFileId=86, operationId=69541d0b-…, operationKind=Apply,
envelopePhase=Active, operationState=Partial, logicalStage=NotStarted,
blockReason=UserAborted, journalDurability=Confirmed.
```

`Criados=0`, `Atualizados=0`, `PersistenceReceipts=0`, nenhuma linha `[B040-B046]` ou
`[B050-B053/B100]` depois do bloqueio, e **319 ms** contra os 6,2 s do Apply abortado: a
operação foi recusada antes da primeira gravação, não interrompida no meio. O envelope não foi
tocado — mesmo `updatedUtc`, mesmo `operationId`, 10 recibos e 5 alvos.

### 6.3 Sync recusado pelo envelope do Apply

`EscolaEndereco` de `VarChar(71)` para `VarChar(70)` criou o delta; sem ele o Sincronizar
encerra antes de alcançar o gate. Mesmo diagnóstico da 6.2, com **`operationKind=Apply`** no
contexto: quem bloqueia é o envelope registrado, e a operação recusada é o Sync. 334 ms, nada
gravado, envelope intacto.

### 6.4 Diário de outra KB — e a precedência provada em campo

O File do diário recebeu uma cópia do próprio conteúdo com **um único campo trocado**, o
`knowledgeBaseGuid`:

```
[JournalUnavailable/JournalIdentityDivergent] Pré-condição 'JournalIdentityConfirmed'.
O diário encontrado pertence à KB '7c0ffee0-…', e a KB aberta é '39e12e41-…'.
```

Este é o cenário que fecha a lacuna da seção 3.1, e ele prova mais do que isso: o envelope
continuava `Partial`/`UserAborted`, que nas 6.2 e 6.3 produziu `GateBlocked`. A identidade
prevaleceu sobre o estado global **no fluxo real**, e não apenas no gate offline. O contexto
seguiu trazendo `operationState=Partial` e `blockReason=UserAborted`, como deve: ele descreve o
que foi lido; o código de alto nível é que diz por que a operação foi recusada.

### 6.5 Colisão externa

File criado à mão com o nome do diário e Description alheia, `FileId=87`. Mesmo par
código/razão da 6.4 — os dois são divergência de identidade — distinguidos pelo contexto:

```
lookupState=ExternalCollision, journalFileId=87, journalDurability=Confirmed
```

Sem `operationId`, sem `operationState`, sem `blockReason`. Não havia envelope, e o diagnóstico
não inventou um.

### 6.6 Dois defeitos que a bateria expôs

Nenhum dos dois é da P3; os dois foram encontrados por ela, porque o envelope persistido
tornou visível o que antes só existia em memória.

**Defeito 1 — `composite.apiGuid` carrega o GUID do próprio objeto.** No inventário exportado
na 6.1, cada Procedure gravou em `composite.apiGuid` o GUID **dela mesma**: `procEscola_API_Get`
ficou com o `GetProcedureGuid`, `procEscola_API_Create` com o `CreateProcedureGuid`, e assim
por diante. O GUID do API Object, `34491e46-…`, não aparece em nenhum item.

A origem é da F2: `ApiPlanBusinessComponentWriter` e `ApiPlanListProcedureWriter` passam
`procedure.Guid` no parâmetro `apiGuid` do `CompositeIdentity`; o `ApiPlanProcedureWriter`
passa `Guid.Empty`, que é honesto. Só apareceu agora porque a P2 foi quem começou a persistir
esse campo, e a sua seção 6.3 tratou justamente o caso vazio.

Hoje nada consome o campo. Quem vai consumi-lo é a **P4**: identidade composta é exatamente o
que autoriza exclusão de Procedures e SDTs legados. Um vínculo errado ali é dado durável que
sustenta — ou recusa — uma exclusão.

**Defeito 2 — o relatório afirma persistência sobre objeto não tocado.** Nas 6.2 a 6.5, todas
operações que não gravaram nada, o B081 reportou `PersistedMainObjectName='apiEscola'` e
`PersistedMainObjectGuid='34491e46-…'`, com `ApiSaveAttempted=False`. Na 6.1, que gravou 13
objetos, esses campos vieram **vazios**.

A causa é `SetMainObject`, que serve para *identificar* o API Object na KB — é o que alimenta o
botão «Abrir objeto principal» — e chama `SetPersistedMainObject` por dentro. Identificar
passou a significar «persistido».

### 6.7 Teste 6 — pendente

O defeito 2 tem uma consequência possível que **não foi medida**, e por isso vira teste: o
`CompleteJournal` usa `report.PersistedMainObjectGuid` como uma das fontes do GUID que dispara
`NoteApiPhysicallySaved`, sem checar `ApiSaveAttempted`. Num Apply que conclua **sem gravar o
API Object** — só SDTs e Procedures —, o campo estaria preenchido por identificação e o diário
registraria a fronteira `ApiPhysicallySaved`.

Se isso ocorrer, o envelope afirma uma gravação física que não aconteceu, e é justamente a
fronteira que impede qualquer recuperação de repetir o Save do API Object. A seção 7 da P2 diz
que esse Apply «faz três checkpoints, não quatro, porque não existe fronteira de API a
registrar»; o teste é se a implementação cumpre isso.

**Roteiro:** Wizard na `Escola` com Delete desmarcado em Serviços e, nas abas de geração,
apenas SDTs e Procedures. Sinais de confirmação: a linha `Checkpoint 'API Object confirmado'`
na Output, `Checkpoints=4` no custo, e `ApiSaveAttempted=False` / `ApiSaveCount=0` no B081.

## 7. O que a P3 deliberadamente não faz

- **não autoriza continuação.** O tipo existe, o gate o entende, e ninguém o preenche: a UI é
  a P6 e o serviço que continua um envelope é a P5;
- **não toca o Remove nem a recuperação.** São P4 e P5. As razões de `PreconditionFailed`
  ficam reservadas a elas;
- **não traduz.** As mensagens continuam em português, direto no código. A localização
  trilíngue é a P7;
- **não muda a política de checkpoints**, que é contrato, nem o custo medido na P2.

## 8. Gates

Gate novo `tests.operationJournalGate`, registrado no orquestrador e no teste do checker
pré-push: a matriz de precedência, cada `reasonCode`, os três caminhos de `Prepared`, o
contexto estruturado e a prova de que `PreconditionFailed` não virou subcausa de
`GateBlocked`. Um `reasonCode` fora do mapeamento fechado é recusado em vez de aceito em
silêncio.

`tests.operationJournalSchema` ganhou os dois lados de `UserAborted`: válido em `Partial`,
recusado em `OutcomeUnknown`.

Verificação executada nesta rodada: build Release com 0 avisos e 0 erros;
`tests.operationJournalSchema`, `tests.operationJournalCheckpoints`,
`tests.operationJournalReceipts`, `tests.operationJournalGate` e o teste do checker pré-push.

## 9. Pendências abertas ao fim desta etapa

1. **Teste 6** — medir se um Apply sem gravação de API Object registra `ApiPhysicallySaved`
   (seção 6.7).
2. **Defeito 1** — `composite.apiGuid` com o GUID do próprio objeto, em
   `ApiPlanBusinessComponentWriter` e `ApiPlanListProcedureWriter` (seção 6.6). Corrigir antes
   da P4, que é quem consome identidade composta para autorizar exclusão.
3. **Defeito 2** — `SetMainObject` declarando persistência ao identificar (seção 6.6).
4. Decidido com o usuário em 2026-09-14: as correções entram **depois** da bateria, numa única
   reinstalação de DLL, e o teste 6 é refeito junto com o teste 1 para conferir o inventário.

Nenhuma delas invalida o que a seção 6 registra: os cinco cenários foram exercidos com a DLL
desta etapa, e os dois defeitos são anteriores à P3.
