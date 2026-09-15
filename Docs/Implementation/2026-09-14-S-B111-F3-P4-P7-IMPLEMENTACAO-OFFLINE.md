# S-B111 · F3 — P4 a P7 implementadas offline

**Data:** 2026-09-14. **Sprint:** `S-B111`. **Fase:** F3 (durabilidade e remoção).
**Plano governante:** [`2026-09-04-B111-F3-PLANO-DURABILIDADE-E-REMOCAO.md`](2026-09-04-B111-F3-PLANO-DURABILIDADE-E-REMOCAO.md).
**Etapas anteriores:** [P0/P1](2026-09-14-S-B111-F3-P0-P1-IMPLEMENTACAO-OFFLINE.md),
[P2](2026-09-14-S-B111-F3-P2-DIARIO-NA-KB.md), [P3](2026-09-14-S-B111-F3-P3-GATE-ESTENDIDO.md).

Esta rodada entrega as quatro etapas que restavam antes da validação na IDE: a remoção com
intenção registrada e fila por passadas (P4), os serviços de recuperação (P5), o comando
explícito e a preferência (P6), e a localização trilíngue com os gates novos (P7). **Nada foi
validado na IDE nesta rodada**: a P8 é a validação integrada, e ela pertence a quem instala a
DLL — ver a remissão da seção 6.

## 1. Estado das etapas

| Etapa | Conteúdo | Estado |
|---|---|---|
| P0 | metadata V3 com `ownership.applicationId` | entregue (2026-09-14) |
| P1 | schema V1 do diário | entregue (2026-09-14) |
| P2 | o diário na KB, em Apply e Sync | entregue e validada na IDE (2026-09-14) |
| P3 | gate estendido e precedência de `GateDiagnostic` | entregue e validada na IDE (2026-09-14) |
| P4 | remoção com intenção, passadas e orçamento | **entregue offline nesta rodada** |
| P5 | reader, rehydrator, executor e relatório de recuperação | **entregue offline nesta rodada, com o recorte da seção 4** |
| P6 | comando explícito e `ShowRecoveryOptionProactively` | **entregue offline nesta rodada** |
| P7 | localização trilíngue e gates restantes | **entregue offline nesta rodada** |
| P8 | validação na IDE (seção 9 do plano) | **em andamento** — quatro cenários passaram em 2026-09-14 |

## 2. P4 — a remoção passa a ter intenção, fila e orçamento

O `Remover API gerada` deixou de ser uma sequência de exclusões e passou a ser três tempos que
não se misturam:

1. **`ApiPlanGeneratedApiRemover.ResolveIntent`** lê a metadata, roda o preflight agregado e
   resolve a identidade de cada alvo. Nada é excluído aqui;
2. o comando registra essa intenção no diário — o inventário **completo**, antes do primeiro
   `Delete()` —, com `Prepared` e `Active` confirmados por releitura;
3. **`ApiPlanGeneratedApiRemover.Execute`** roda a fila por passadas, com um checkpoint ao fim
   de cada uma.

### 2.1 A fila deixou de ser só de SDTs

`DeleteOwnSdtsResilientToOrder` saiu. A fila (`ApiPlanRemovalQueue`) abrange API Object,
Procedures, SDTs, File de metadata e Folder próprio, nessa ordem, e o `P` da matriz de
checkpoints da seção 4.4 passou a ser o número de passadas dessa fila completa — que é o que o
plano sempre disse, e que a implementação anterior não cumpria.

As regras da seção 4.3, implementadas e exercitadas offline:

| Resultado da tentativa | O que a fila faz |
|---|---|
| `Confirmed` — o alvo não foi reencontrado | sai da fila |
| `StillPresent` — releitura comprova que continua lá | **único** caso que volta para a fila |
| `AbsentBeforeDelete` — o alvo previsto já não estava lá | `Partial` + `TargetAbsentBeforeDelete` |
| `StageFailed` — falha conhecida, não retryable | `Partial` + `StageFailed` |
| `OutcomeUnknown` — releitura ilegível, divergente ou ambígua | `OutcomeUnknown`, sem retry |
| `Preserved` — Folder próprio que não ficou vazio | sai da fila sem impedir `Removed` |

O orçamento é `max(1, itens da fila)`; atingido com pendentes, o envelope termina em `Partial`
com `RetryBudgetExhausted`. A classificação vem **sempre** da releitura do alvo, nunca do texto
da exceção da IDE — a mesma invariante que o laço antigo protegia, agora aplicada a todos os
tipos.

### 2.2 Uma mudança de comportamento que precisa ser vista na IDE

Antes, um alvo listado que já não estava na KB era aceito em silêncio: a remoção era
idempotente por omissão. Agora ele encerra a operação em `Partial` com
`TargetAbsentBeforeDelete`, porque o plano é explícito — «`NotAttempted`/`Absent` antes do
primeiro Delete não é sucesso implícito». Quem apagou aquele objeto não foi esta operação, e
tratar isso como sucesso é o que faz um relatório dizer «Removidos: nenhum» com objetos
apagados.

O efeito prático aparece no cenário mais banal: mandar remover duas vezes. A segunda execução
agora **bloqueia**, e a saída é o comando de recuperação da P6. Isso é o contrato; se na
validação da IDE ele se mostrar hostil demais no uso normal, a discussão é sobre a regra, não
sobre contorná-la no chamador.

### 2.3 O Folder e a confirmação de vazio

O schema exige `emptyConfirmed=true` para apagar um Folder, e essa medição só existe depois que
os filhos saem. Declarar `true` na intenção seria afirmar o que ninguém mediu. A solução: o
Folder próprio entra na **fila** com `Action=Preserve` e `emptyConfirmed=false` — a marca de
«previsto, ainda não medido» — e só passa a `Delete` com `emptyConfirmed=true` no snapshot em
que a exclusão foi confirmada. O Folder reutilizado não traz o campo, e é assim que a retomada
distingue um do outro.

`Queued` (entra na fila) deixou de ser o mesmo que `Action=Delete` (o que o inventário declara).

## 3. P5 — recuperação: ler, reidratar, executar, relatar

Quatro contratos, nomeados como a seção 5.4.1 os fecha:

| Tipo | Responsabilidade |
|---|---|
| `ApiPlanRecoveryReader` | resolve o diário pelo nome fixo no índice, valida identidade e conteúdo, e **relê cada alvo do inventário por identidade** |
| `ApiPlanRecoveryRehydrator` | cruza envelope e observações e devolve **uma** `NextStep` autorizada, ou bloqueia com razão classificada |
| `ApiPlanRecoveryExecutor` | revalida o snapshot, confere a autorização, toma o lock local da KB e executa |
| `ApiPlanRecoveryReport` | descreve. Não grava, não decide, não cria recibos |

`RecoveryAuthorization` vincula o consentimento ao snapshot exato que o produziu: identidade,
`journalFileId`, `updatedUtc` e hash canônico. Qualquer divergência entre a leitura e a ação
recusa com `RecoveryAuthorizationStale` **antes** da mutação. O lock é local ao processo e
indexado pela KB: coordena duas continuações na mesma instância da extensão e não promete
atomicidade entre duas IDEs — onde ele não alcança, a revalidação otimista é a defesa.

### 3.1 O que a recuperação executa, e o que ela recusa

| Situação do envelope | Decisão |
|---|---|
| `Completed` / `Removed` | nada a recuperar |
| `Prepared`/`Pending` sem recibos | **`Abandon`** — libera a KB preservando o registro da decisão |
| `Prepared` **com** recibos | bloqueia: o envelope se contradiz, e abandonar apagaria a prova |
| `journalDurability=Unknown` | bloqueia (`DurabilityUnknown`) |
| `OutcomeUnknown` | bloqueia (`UnreconciledOutcome`) |
| `Remove` interrompido com alvos ainda na KB | **`ContinueRemovePass`** — retoma a fila no mesmo envelope |
| `Remove` interrompido com todos os alvos ausentes | **`Complete`** — fecha o registro como `Removed` |
| `Remove` com alvo ilegível ou sem leitura | bloqueia (`IdentityDivergent`) |
| `Remove` parado em `TargetAbsentBeforeDelete` | **`Discard`** — retomar às cegas não é possível; encerrar o registro é (ver 3.4) |
| `Apply` / `Sync` não terminais | **`Discard`** — ver 3.2 e 3.4 |

### 3.2 Recorte declarado: Apply e Sync não são retomados

O envelope guarda a identidade da API, o hash do contrato e as flags de geração — **não o
contrato**. Reconstruir um `ApiPlan` a partir disso seria inventá-lo, e um plano inventado grava
objetos que ninguém pediu. Por isso um `Apply` ou `Sync` interrompido recebe `Block`, com o
diagnóstico dizendo o que existe e quais são as duas saídas reais: concluir a geração pelo
Wizard sobre o estado atual, ou remover o que ficou pela metade.

Isso não é uma lacuna contornada: é o que a própria seção 5.4.1 chama de «reconciliação não
determinística → bloqueio». Mas é **menos** do que a leitura apressada da F3 sugere, e fica
declarado aqui para que a P8 não seja o lugar onde alguém descobre isso.

O que esses envelopes **recebem** é a ação da seção 3.4: encerrar o registro. Não é continuar a
operação — é declarar que ela acabou e assumir o estado em que a KB ficou.

### 3.3 A retomada da remoção usa o diário, não a metadata

O File de metadata é o **penúltimo** da fila. Depois que ele sai, a metadata não pode mais
descrever o que faltava — só o diário pode. Por isso a retomada:

- reconstrói os alvos de `inventory` (`ApiPlanRemovalIntent.FromInventory`), enfileirando apenas
  o que a intenção declarou `Delete` e a KB ainda mostra, mais o Folder próprio ainda não medido;
- reconstrói o contexto de validação (`ApiPlanRemovalContext.FromJournal`) — nome e GUID da API,
  Transaction e os SDTs compartilhados que **nunca** podem ser tocados, tirados dos itens
  `Preserve` do inventário;
- reabre o **mesmo** envelope (`ResumeRemoval`), preservando `operationId` e `applicationId`.

Para isso, as exclusões deixaram de depender do `ApiPlanGeneratedApiRemovalPlan` e passaram a
depender do `ApiPlanRemovalContext`. A retomada só cobre envelopes parados por
`RetryBudgetExhausted`, `StageFailed` ou `UserAborted`: ausência antes do Delete e indeterminação
não voltam para a fila sozinhas.

### 3.4 Encerrar o registro de uma operação interrompida

**Acrescentado em 2026-09-14, depois do resto desta rodada.** Ao explicar o recorte de 3.2, a
consequência apareceu inteira: um envelope `Apply` interrompido bloqueia todas as operações
seguintes pelo gate, a recuperação respondia `Block`, e o círculo se fechava — a única saída
voltava a ser apagar o File do diário à mão, que é justamente o que a frente existe para
eliminar. O mesmo valia para um `Remove` parado em `TargetAbsentBeforeDelete`.

A base contratual está na seção 4.1.1 do plano: voltar a uma situação sem intenção ativa é
admitido **mediante confirmação humana de que nenhuma operação ativa ou parcial foi provada**.
A ação implementa exatamente isso, e nada além:

- **estágio novo `Discarded`** no enum `JournalLogicalStage`, distinto de `Abandoned`. O
  abandono pertence a um envelope que nunca tocou a KB e por isso não admite recibos; o
  encerramento pertence a um que gravou e parou, e **preserva** recibos e inventário. Mudança
  incompatível de schema, feita enquanto o V1 não saiu em release — mesmo critério com que a P3
  acrescentou `UserAborted` a `blockReason`;
- **`RecoveryNextStep.Discard`**, valor novo no enum que a seção 5.4.1 declarava fechado. Ele é
  efêmero — não é persistido —, e a justificativa está no XML doc do próprio valor;
- o que a transição **recusa**, e é onde está o rigor: `OutcomeUnknown` (não se sabe se a última
  gravação aconteceu, e encerrar transformaria dúvida em certeza), envelope `Prepared` (esse é
  abandono, não encerramento) e durabilidade não confirmada (encerrar esconderia justamente o
  que não se sabe);
- a confirmação mostra o inventário junto da pergunta, além do diagnóstico já publicado na
  Output, e o texto diz nas três línguas que **nada é apagado**.

Depois do encerramento, as duas saídas são as de sempre: reaplicar pelo Wizard sobre o estado
atual, com o reencontro conservador cuidando do que já existe, ou remover a API gerada.

## 4. P6 — o comando e a preferência

`Recuperar operação interrompida` / `Recuperar operación interrumpida` /
`Recover interrupted operation`, registrado nas três camadas — `Package.cs`,
`CommandDefinition` e `Command refid` nos dois grupos — e visível no menu principal e no menu
de contexto da Transaction. O checker de comandos passou com **13** comandos.

O comando sempre publica o diagnóstico completo na Output antes de qualquer pergunta, e só age
depois de confirmação explícita.

**Diálogo próprio, não `MessageBox`** (ajustado em 2026-09-14, durante a P8). O `MessageBox` do
Windows escolhe a própria largura, e a decisão de encerrar um registro chegava espremida numa
coluna estreita, com o inventário de vinte e quatro objetos derretido dentro do parágrafo —
mais os asteriscos de Markdown aparecendo literais, porque ali não há formatação. O
`ExtensionRecoveryDialog` segue o desenho do diálogo do Remover: mensagem com largura de
leitura, inventário em bloco monoespaçado e rolável — uma linha por alvo, com o previsto e o
observado —, pergunta no rodapé e o botão seguro com o foco, respondendo a Enter e a Esc. Os
resumos deixaram de enumerar nomes: contam quantos alvos continuam na KB e deixam a lista para
o bloco. A preferência nova `ShowRecoveryOptionProactively` — gravada
como `showRecoveryOptionProactively` no File de preferências, **ligada por padrão** — faz o
bloqueio de Apply, Sync e Remover oferecer a recuperação na hora.

O motivo de ela nascer ligada está registrado na validação da P3: com o envelope não terminal
bloqueando tudo e sem comando de saída, a única saída praticada foi apagar o File
`GxOpenApiBuilder_OperationJournal` à mão — duas vezes.

## 5. P7 — localização e gates

Os textos do comando, das confirmações, do bloqueio e da oferta proativa existem nos três
idiomas, e o gate de localização passou a exigir isso: um comando que só aparece em português é
um comando que metade dos usuários não encontra.

Gates novos, ambos registrados no orquestrador e cobertos pelo teste do próprio checker:

| Gate | Cobre |
|---|---|
| `tests.removalQueue` | ordem canônica, passadas, requeue só por `StillPresent`, orçamento, os quatro desfechos de bloqueio, Folder preservado e inventário sem alvo repetido |
| `tests.operationJournalRecovery` | as dez decisões da tabela 3.1, o relatório somente-leitura, as seis recusas da autorização, a reconstrução da fila a partir do inventário e as três recusas do encerramento de registro, com a preservação de recibos e disposição |

Gates existentes que mudaram junto com o contrato:

- `tests.generatedApiRemovalResilience` — a resiliência à ordem passou a ser verificada na fila
  unificada, e não no laço de SDTs. As invariantes preservadas são as mesmas: abort nunca é
  adiado, a recusa não é lida por texto, o relatório sabe o que saiu da KB. Acrescentou-se a
  exigência de o diário fechar com o estado real da fila;
- `tests.generatedApiRemovalPreflight` — o preflight mudou de lugar (vive na resolução da
  intenção) e o teste passou a verificar também a ordem intenção → diário → fila no comando;
- `tests.kbIndexReuse` — a allowlist de `ApiPlanKbObjectNameIndex.Create` trocou `Remove` por
  `ResolveIntent` e ganhou `RunRecovery`.

## 6. Verificação executada

| Verificação | Resultado |
|---|---|
| `dotnet build Src\GenexusOpenApiBuilder.sln --configuration Release` | 0 avisos, 0 erros |
| `tests.operationJournalSchema` (mensagem do validador ajustada em 3.4) | PASS |
| `Tools/Test-ExtensionCommandRegistration.ps1` | OK, 13 comandos |
| `tests.removalQueue` (novo) | PASS |
| `tests.operationJournalRecovery` (novo) | PASS |
| `scripts/Invoke-PrePushMechanicalChecks.ps1 -AsJson` | 65 checks, 64 `passed` e 1 `skipped` (`git.statusPre`, working tree suja antes do commit) |

Nada foi exercido na IDE **nesta rodada**.

**Remissão — 2026-09-14, mesma data:** a P8 começou logo depois e quatro cenários passaram —
remoção completa pela fila nova, alvo previsto ausente antes do `Delete()`, recuperação
encerrando o registro e devolução da KB ao normal. A bateria produziu seis correções, todas
registradas em [`2026-09-14-S-B111-F3-P8-VALIDACAO-IDE.md`](2026-09-14-S-B111-F3-P8-VALIDACAO-IDE.md):
o diálogo próprio da recuperação, três rodadas de orientação nas mensagens de bloqueio, a
limpeza do bloco técnico de diagnóstico e o módulo do File do diário. O que este documento
descreve continua sendo o que as etapas P4 a P7 entregaram offline.

## 7. Riscos assumidos e abertos

- **A segunda remoção da mesma API bloqueia.** É o contrato (2.2), mas muda o comportamento
  observado na Alpha. Precisa de uma decisão consciente na P8: manter, ou tratar «inventário
  inteiro ausente» como reconciliação automática em vez de bloqueio.
- **`emptyConfirmed=false` é um discriminador escolhido nesta etapa.** O schema V1 não fixa
  como distinguir «Folder previsto, não medido» de «Folder reutilizado»; a P4 usa a presença do
  campo. Um diário gravado antes desta rodada não tem essa marca — e a retomada, nesse caso,
  não reenfileira o Folder. Não há diário anterior em produção: a P2 estreou ontem.
- **A continuação de Apply e Sync não existe** (3.2). Um Apply interrompido exige decisão
  humana entre reaplicar pelo Wizard ou remover; o que a ferramenta faz por ele é encerrar o
  registro (3.4), liberando a KB sem apagar nada.
- **O encerramento de registro é a ação mais delicada da recuperação**: ela libera a KB sem
  concluir a operação, e a KB pode ficar com objetos pela metade. A defesa é a confirmação
  informada — inventário à vista — e as três recusas de 3.4. Se na P8 ficar claro que o texto
  não deixa isso evidente o bastante, é o texto que muda, não a regra.
- **O lock é local ao processo.** Duas IDEs sobre a mesma KB continuam cobertas apenas pela
  revalidação otimista, como o plano prevê.
- **A oferta proativa nasce ligada.** Se na IDE ela se mostrar intrusiva, a preferência já
  existe para desligá-la — e a decisão de inverter o padrão é de quem testar.
