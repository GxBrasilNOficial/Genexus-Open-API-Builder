# B111 · Fase F3 — durabilidade da intenção e remoção segura

**Sprint:** S-B111 — gravação única do API Object.
**Fase:** F3 de 3 (F1 ordem e writer final · F2 seam e recibos · **F3 durabilidade e remoção**).
**Data:** 2026-09-04. **Item:** `B111` em `Docs/Foundation/06-BACKLOG_v0.1.md`.

**Status:** Modo A selecionado; decisões de escopo e contrato consolidadas em 2026-09-07.
O plano ainda não foi implementado e a revisão por pares da sprint não está encerrada.
**Não** autoriza alteração de código, instalação, commit ou push.

**Pré-requisito:** F1 e F2 aceitas. A F3 consome os recibos da F2; sem eles, não há como
distinguir o que foi gravado do que ficou indeterminado, e qualquer recuperação seria
adivinhação.

**Decisão registrada:** o Modo A — diário durável — foi escolhido. A comparação com o
Modo B permanece apenas como histórico de alternativas; o contrato vigente desta F3 é o
diário único por KB, com recuperação explícita.

**Código já em campo neste território (2026-09-06):** a recuperação de metadata órfã foi
implementada fora desta fase, por necessidade de campo, e ocupa parte do que a seção 4.3
normatiza. Validada na IDE no mesmo dia (13.6), exceto o `Remover` sob a metadata
recuperada. A seção 13 registra o que ela grava e o que ela deliberadamente não grava;
essas decisões agora são absorvidas pelo contrato vigente da F3. **Ler a 13 antes de
revisar a 4.2 e a 4.3.**

---

## 1. Histórico da elaboração da F3

A F3 foi escrita antes da decisão do modo por um motivo concreto: sem este documento, o
único plano disponível para o conteúdo da F3 era o manuscrito expandido v24 — e **quatro
pontos dele foram desmentidos pelas medições de 2026-09-04**. Quem retomasse a frente lendo
esse manuscrito implementaria quatro coisas erradas, e a correção existia apenas no registro
de evidência, que não é leitura obrigatória de quem procura “o plano da F3”.

O Modo A foi escolhido posteriormente e está consolidado no contrato vigente desta F3. As
referências abaixo à alternativa B devem ser lidas como histórico comparativo, não como
decisão pendente.

A seção 7 lista essas quatro correções de forma explícita, e é a parte deste documento que
mais importa preservar.

Este plano é deliberadamente menos detalhado que F1 e F2 em pontos que dependem de
experiência de campo com a F1 implementada. Onde isso ocorre, está dito.

---

## 2. Estado de partida

### 2.1 O que F1 e F2 já terão entregue

- um único `API.Save()` por aplicação, no writer final, em Sync e Wizard;
- identidade planejada como o `Guid` lido do `API.Create`;
- gate reduzido antes de qualquer gravação;
- seam de persistência com recibos, e as classes de falha C, D e E distinguíveis;
- sequência de recibos no relatório final.

A F3 acrescenta **durabilidade da intenção** e **recuperação**, que são exatamente o que
recibos em memória não dão: eles morrem com o processo.

### 2.2 O que o remover já faz hoje

Ao contrário do que o manuscrito expandido sugere, `ApiPlanGeneratedApiRemover.Remove` **não** apaga às
cegas. Ele já:

1. cria o índice da KB (`ApiPlanGeneratedApiRemover.cs:49`);
2. localiza a metadata própria por nome canônico e posse;
3. reconstrói o plano de remoção a partir da metadata (`FromMetadata`, com nome e GUID da
   Transaction);
4. valida ambiguidade e posse de API, Procedures e SDTs **antes de qualquer `Delete()`**
   (`ValidateRemovalTargets`, `ApiPlanGeneratedApiRemover.cs:315`);
5. confirma a ausência de cada objeto depois do `Delete()`, lançando se ainda existir.

Ou seja: a intenção de remoção já é reconstruída a partir de uma fonte durável — a
metadata B060 — e validada antes de destruir qualquer coisa.

**O que falta**, e é o escopo da F3 na remoção:

- registrar a intenção de forma durável **antes** do primeiro `Delete()`, para que uma
  interrupção no meio deixe rastro do que foi previsto e do que foi feito;
- tratar o caso de metadata ausente, corrompida ou insuficiente sem cair em inferência por
  nome;
- distinguir remoção concluída de remoção interrompida.

Isso é evolução do que existe, não reconstrução.

### 2.3 O que as sondas mediram e que restringe o desenho

Do registro `2026-09-04-B111-SONDAS-IDENTIDADE-E-DIARIO.md`, seis execuções sobre duas KBs:

| Operação | KB pequena | KB grande |
|---|---|---|
| gravar um `WikiFileKBObject` | ~130 ms | **~1,1 s** |
| gravar Folder, SDT, Procedure, API | 11–35 ms | 12–98 ms |
| reler File por `Id` | 0 ms | 0 ms |
| localizar por nome no índice já montado | 0 ms | 0 ms |
| remontar o índice | ~100 ms | **~3,1 s** |
| varrer Files por prefixo (medição histórica) | 5 ms | 22–28 ms |

Três restrições saem daí, e valem para qualquer desenho de diário:

1. **escrever é caro, ler é grátis** — o desenho deve minimizar gravações, não leituras;
2. **nunca remontar o índice** para reencontrar um objeto que se acabou de criar;
3. **guardar o `Id`** do File na criação: `WikiFileKBObject.Get` aceita `int`, não `Guid`.

---

## 3. Decisão adotada: Modo A — diário durável

O Modo A foi selecionado em 2026-09-07. A tabela abaixo preserva a comparação que orientou
a decisão, mas somente a coluna do Modo A é contrato vigente. O Modo B é histórico e não
pode reaparecer como implementação parcial ou como decisão ainda pendente.

| | **Modo A — diário durável** | **Modo B — checkpoint manual** |
|---|---|---|
| onde a intenção vive | um `File` próprio na KB | fora da KB, registrado pelo operador |
| custo no Apply, KB grande | 2,2 s a 11 s, conforme granularidade (4.4) | ~zero |
| resíduo na KB | um `File` único por KB, preservado após a operação terminal | nenhum |
| recuperação | explícita, no comando próprio da F3; bloqueia em qualquer ambiguidade | humana e bloqueante |
| complexidade acrescentada | alta: máquina de estados, reconciliação, ciclo de vida do diário | baixa |
| o que promete | reconstruir intenção e ponto de falha sem intervenção | tornar impossível reaplicar às cegas |

Nenhum dos dois entrega atomicidade ou rollback. A diferença histórica é **quem** reconstrói
a intenção depois de uma falha: o código ou a pessoa. No contrato vigente, a recuperação é
explícita e pertence à F3.

### 3.1 Decisões fechadas e detalhes de execução

- há um único diário por KB, no objeto técnico `File`/`WikiFileKBObject`, sem módulo;
- o nome lógico fixo é `GxOpenApiBuilder_OperationJournal` e o arquivo externo é
  `GxOpenApiBuilder_OperationJournal.json`;
- o diário guarda somente a operação corrente, sem histórico de operações; o mesmo File é
  reutilizado depois de um estado terminal confirmado (`Completed` ou `Removed`) e de
  `journalDurability=Confirmed`;
- o comando unificado de recuperação/reconciliação cobre B115 e `OutcomeUnknown`: aparece
  preventivamente conforme `ShowRecoveryOptionProactively`, sempre aparece quando há
  pendência, fica primariamente no menu de contexto da Transaction e tem fallback no menu
  principal para a KB inteira; o diagnóstico é somente leitura até confirmação explícita;
  Transaction selecionada que não coincide com a intenção bloqueia;
- falha, divergência, leitura ambígua ou mais de um candidato válido bloqueiam; não há
  desempate por nome, prefixo, data ou “mais recente”;
- a implementação deve manter separadas a identidade da operação, a intenção, o estado
  físico, a durabilidade do diário e os recibos definidos na decisão registrada.

As comparações com o Modo B permanecem neste documento apenas para explicar a decisão
histórica. Não há uma segunda implementação a especificar nem uma decisão A/B pendente.

---

## 4. Núcleo do Modo A — estados e gate

### 4.1 Estados

As quatro dimensões do JSON são sempre registradas separadamente e nunca colapsadas
num enum só:

- **`operationState`**: resultado global da operação;
- **`logicalStage`**: ponto do pipeline em que a operação está;
- **`journalDurability`**: confirmação ou indeterminação da última atualização do diário;
- **`intentKind`**: `Current` para intenção criada pela operação atual ou `Imported` para
  intenção reconstruída de metadata órfã.

O **estado físico do objeto** (`Absent`, `Confirmed`, `Divergent` ou `Unknown`) não é uma
quinta dimensão do envelope: é observação por item, derivada dos recibos da F2 e da leitura
da KB.

Estados da operação: `Pending`, `Running`, `Partial`, `OutcomeUnknown`, `Completed` e
`Removed`, exatamente como na decisão canônica 27. `Prepared` e `Active` são fases do
envelope do diário; `RemovalPartial` e `ApiSaveOutcomeUnknown` são `logicalStage`;
`Failed` é resultado de recibo ou falha de etapa. `JournalUnavailable` é um
`GateDiagnostic`, não um estado que autorize prosseguir nem um `blockReason` persistido
sem snapshot confirmado.

Estágios mínimos: `NotStarted`, `IntentionRecorded`, `TransactionPending`,
`FolderPending`, `SdtsPending`, `ProceduresPending`, `ApiPending`, `ApiSaveOutcomeUnknown`,
`ApiPhysicallySaved`, `MetadataPending`, `MetadataRecovered`, `RemovalInProgress`,
`RemovalPartial` e `RecoveryInProgress`.

`Partial` pertence somente a `operationState`; durante remoção ele deve ser acompanhado de
`logicalStage=RemovalPartial`. Nunca usar `Partial` como `logicalStage`.

Regra que atravessa tudo: **um `Save()` que lançou, expirou ou foi cancelado é
`OutcomeUnknown` até consulta por identidade.** Nunca “não gravou”.

### 4.1.1 Envelope corrente e abandono explícito

Cada operação recebe um `OperationId` novo, distinto do `ApplicationId` da geração. Antes
de qualquer `Save()` de negócio, o File do diário é gravado como fase de envelope
`Prepared` e relido por `FileId`/hash. Só depois é promovido à fase `Active`, com nova
gravação e confirmação. Os dois passos precisam ser confirmados antes da primeira
gravação de Transaction, Folder, SDT, Procedure, API ou metadata.

`Prepared` pode ser continuado explicitamente com os mesmos identificadores ou abandonado.
O abandono não apaga o File nem cria um novo `operationState`: grava uma disposição
explícita de abandono, confirmada pela durabilidade do journal, removendo a intenção ativa
antes de permitir nova operação. `Pending` sem `Save()` de negócio é a única situação que
pode seguir esse caminho. `Active`, `Partial` ou `OutcomeUnknown` não podem ser abandonados
como se nada tivesse ocorrido: exigem reconciliação ou continuação explícita com os mesmos IDs.

Após `Completed` ou `Removed`, a próxima operação substitui o envelope corrente pelo novo
`Prepared`; não há arquivo, campo ou coleção de histórico. Se o diário estiver ausente,
corrompido, duplicado ou não puder ser confirmado, o resultado é
`GateDiagnostic=JournalUnavailable`: bloqueia Apply, Sync e Remove, permite apenas
diagnóstico de leitura e não cria um novo envelope. Só admite voltar a uma situação sem
intenção ativa mediante confirmação humana de que nenhuma operação ativa ou parcial foi
provada.

### 4.2 Gate estendido

O gate reduzido da F1 ganha as validações que dependem de intenção durável:

1. ausência de intenção anterior em estado parcial, indeterminado ou ambíguo;
2. disponibilidade e integridade do diário único da KB;
3. identidade, versão e durabilidade confirmadas do diário, sem divergência física;
4. ausência de intenção ativa, ou transição explicitamente autorizada de `Prepared`;
5. ausência de `GateDiagnostic` irremediado e de `OutcomeUnknown` não reconciliado.

Falhando qualquer uma, o resultado é `GateDiagnostic=GateBlocked` antes da primeira
gravação. O gate não cria `Prepared`, não grava `blockReason` e não inventa um
`logicalStage`; se houver envelope confirmado anterior, o relatório referencia o snapshot
existente sem alterá-lo. O payload estruturado do diagnóstico deve carregar uma `reasonCode`
estável e a precondição que falhou, além da mensagem para leitura humana; não basta reduzir
causas diferentes ao texto livre `GateBlocked`. Quando o journal está legível, os códigos de
subcausa previstos são `JournalNonTerminal`, `JournalIdentityDivergent`,
`PreparedContinuationNotAuthorized`, `UnreconciledOutcome` e `PreconditionFailed`. Se a
indisponibilidade ou a durabilidade desconhecida impedirem confirmar o journal, o código de
alto nível permanece `JournalUnavailable` ou `DurabilityUnknown`, conforme o caso. Esses
detalhes são efêmeros do diagnóstico, não valores novos do journal nem substitutos de
`blockReason`.

### 4.3 Remoção com intenção confirmada

Antes do primeiro `Delete()`, e além do que o remover já faz hoje (2.2):

1. registrar a intenção de remoção, com o conjunto completo de alvos validados, e
   confirmá-la por releitura;
2. só então excluir, na ordem definida, emitindo recibo por exclusão;
3. marcar `Removed` apenas quando todos os alvos previstos estiverem confirmadamente
   ausentes;
4. marcar `RemovalPartial` em interrupção, **preservando** o registro da intenção.

Para API criada antes desta frente, sem intenção registrada, duas saídas — e só essas:

- reconstruir a intenção a partir de metadata válida, validando Transaction GUID,
  contrato, nomes e todas as referências, e marcá-la como importada. Se a metadata
  legada não tiver `ApplicationId`, a operação S-B111 gera um GUID novo para a tentativa
  atual e o registra no diário como adoção tardia; não regrava a metadata legada apenas
  para preencher esse campo; ou
- **bloquear antes do primeiro `Delete()`**, com instrução de recuperação manual.

Se a metadata não permitir reconstruir o conjunto **completo** de alvos, não apagar
parcialmente por inferência. `ApiPlanOwnedObjectDescription.IsCanonical` continua útil
para reconhecer posse histórica. Para o API Object, o `apiGuid` confirmado continua
obrigatório. Para Procedures e SDTs legados, a identidade histórica composta poderá ser
aceita quando houver nome exato, tipo e papel esperados, `Description` canônica vinculada
à Transaction/API, unicidade na KB e inventário completo sem conflito. Nome isolado,
Description isolada ou prefixo isolado **nunca** autorizam exclusão.

A fila de remoção não é exclusiva de SDTs. Ela deve abranger todos os tipos removíveis
que o plano tenha validado — API Object, Procedures, SDTs, File de metadata e Folder
quando a posse permitir sua exclusão. `P` na matriz de checkpoints é o número de
passadas da fila completa, não apenas do laço de SDTs. Em cada passada, somente uma
falha `Failed` com `StillPresentAfterDelete` pode recolocar um alvo comprovadamente
presente; `Confirmed` o retira e `OutcomeUnknown` bloqueia a operação. A implementação
atual, que reencaminha exceções apenas em `DeleteOwnSdtsResilientToOrder`, é comportamento
histórico a ser substituído pelo contrato da F2/F3, não o contrato final.

O ciclo de vida da fila é fechado assim:

- a fila destrutiva contém somente `ApiObject`, `Procedure`, `Sdt`, `MetadataFile` e
  `Folder` próprio criado pela extensão e vazio depois dos filhos. Transaction, o
  próprio diário, o File de preferências, SDTs compartilhados, Folder reutilizado e
  qualquer objeto sem posse validada ficam fora da fila e aparecem somente como
  `Preserve` no inventário;
- a ordem é API Object, Procedures, SDTs na ordem de dependência, metadata File e,
  por último, Folder próprio vazio. Cada item é relido pela identidade validada antes
  da tentativa e recebe um recibo, inclusive quando não chega a chamar `Delete()`;
- `NotAttempted`/`Absent` antes do primeiro Delete não é sucesso implícito. Sem recibo
  durável anterior `Confirmed` para a mesma identidade, encerra a operação em `Partial`
  com `blockReason=TargetAbsentBeforeDelete`. Uma falha de etapa não retryable,
  registrada por `NoteStageFailed`, também encerra em `Partial`, com
  `blockReason=StageFailed`, o item preservado e
  sem nova passada;
- no início da operação, `maxPasses = max(1, número de itens Delete do inventário)`.
  Uma passada só pode reencaminhar `StillPresentAfterDelete`. Se o limite for atingido
  com itens pendentes, o diário termina com `operationState=Partial` e
  `blockReason=RetryBudgetExhausted`; não há loop infinito nem retry silencioso em outra
  operação. Uma continuação explícita do mesmo envelope pode receber novo orçamento depois
  de confirmação humana;
- somente quando todos os itens `Delete` tiverem recibo `Confirmed` ou um recibo
  anterior equivalente e durável, sem `NotAttempted`, `Failed` ou `OutcomeUnknown`,
  a operação pode terminar em `Removed`.

### 4.4 Orçamento de gravação

Vale para o Modo A, e é o motivo de a granularidade ser uma decisão de projeto e não um
detalhe. As três linhas abaixo são apenas referência histórica das alternativas medidas;
não definem a política de implementação e não substituem a matriz fechada logo abaixo:

| Política | Gravações | Acréscimo ao Apply, KB grande |
|---|---|---|
| uma por etapa confirmada, como no manuscrito expandido | ~10 | ~11 s |
| três checkpoints agrupados: início, pós-API, conclusão | 4 | ~4,4 s |
| mínimo: criação e conclusão | 2 | ~2,2 s |

O diário deve ser atualizado e confirmado ao longo da operação e no estado terminal, mas
isso não significa um `File.Save()` para cada recibo individual. A política normativa
agrupa recibos nas fronteiras que mudam a capacidade de continuar ou reconciliar:
`Prepared`, `Active`, confirmação física do API, cada passada de `Remove` e estado
terminal. A implementação não pode escolher o mínimo de duas gravações apenas para
economizar I/O se isso deixar uma fronteira sem estado durável.

Antes da implementação, a matriz abaixo fecha a política por operação, sem aproximações
como “~4”. Cada checkpoint físico corresponde a exatamente um `File.Save()` do diário,
seguido de releitura e validação; não há outro `File.Save()` implícito entre os pontos. A
reidratação inicial da continuação é somente leitura e não conta como checkpoint físico.
O `Save()` do diário não entra na contagem de persistências de negócio da F2.

| Operação | Checkpoint 1 | Checkpoint 2 | Checkpoint 3 | Checkpoint 4 | Contagem física |
|---|---|---|---|---|---|
| Apply | `Prepared` + `Pending/IntentionRecorded` | `Active` + `Running/NotStarted` | recibos até API confirmado + `Running/ApiPhysicallySaved` | `Completed`, `Partial` ou `OutcomeUnknown` + estágio final | exatamente 4 |
| Sync | `Prepared` + `Pending/IntentionRecorded` | `Active` + `Running/NotStarted` | recibos até API confirmado + `Running/ApiPhysicallySaved` | `Completed`, `Partial` ou `OutcomeUnknown` + estágio final | exatamente 4 |
| Remove | `Prepared` + `Pending/IntentionRecorded` | `Active` + `Running/RemovalInProgress` | inventário atualizado ao fim de cada passada | `Removed`, `Partial` ou `OutcomeUnknown` + estágio final | `3 + P`, sendo `P` o número de passadas executadas |
| Recovery autônomo de B115, sem envelope de negócio | `Prepared` + `Pending/IntentionRecorded` | `Active` + `Running/MetadataPending` | recibo de metadata confirmado + `Running/MetadataRecovered` | `Completed` ou `OutcomeUnknown` + estágio final | exatamente 4 |
| Recovery de continuação de envelope existente | reidratação somente leitura do snapshot, sem novo `Prepared`/`Active` | `RecoveryInProgress` somente se houver checkpoint confirmado de reidratação | próxima etapa segura da operação original | estado terminal ou bloqueio explícito confirmado | checkpoints físicos restantes da operação original, além de passadas de `Remove` |

Cada célula inclui o snapshot do plano/inventário, a transição de `operationState` e
`logicalStage`, e o resultado de durabilidade. Se qualquer `Save()` ou releitura produzir
`journalDurability=Unknown`, o executor conserva o último estado durável, não avança para a
célula seguinte, não grava objeto de negócio e expõe bloqueio para reconciliação. A tentativa
física não é contada como checkpoint confirmado e não pode ser repetida em silêncio. A
implementação não poderá reduzir a quantidade para economizar I/O nem misturar `Prepared`/
`Active` ao enum de estado global.

Para tornar explícita a fronteira entre o comando de recuperação e a operação persistida,
as transições são estas:

| Situação | `operationKind` persistido | Identidade e intenção | `logicalStage` da recuperação | Estados terminais possíveis |
|---|---|---|---|---|
| B115 autônomo, sem envelope de negócio a continuar | `Recovery` | novos `operationId`/`applicationId`, `intentKind=Imported` | `IntentionRecorded` → `MetadataPending` → `MetadataRecovered` → `Completed` | `Completed` ou `OutcomeUnknown` |
| Recuperação explícita de `Apply` ou `Sync` | `Apply` ou `Sync` original | mesmos IDs, `applicationId` e `plan` do envelope | `RecoveryInProgress` durante a reidratação; depois, o próximo estágio original ainda não confirmado | terminal previsto pela operação original, inclusive `Completed`, `Partial` ou `OutcomeUnknown` |
| Recuperação explícita de `Remove` | `Remove` original | mesmos IDs, `applicationId` e inventário do envelope | `RecoveryInProgress` → `RemovalInProgress` ou `RemovalPartial` conforme a passada | `Removed`, `Partial` ou `OutcomeUnknown` |

O rótulo do comando (`Recovery`) não é gravado como substituto de uma operação de
negócio. Ele apenas seleciona a reidratação e a confirmação humana da continuação.

As duas linhas de `Recovery` não significam quatro gravações novas em qualquer recuperação.
Somente o B115 autônomo tem quatro checkpoints fixos. A continuação de envelope existente
não cria novas fases `Prepared`/`Active`, preserva `operationKind`, `operationId`,
`applicationId` e `plan`, e conta apenas os checkpoints ainda necessários na operação
original. `RecoveryInProgress` só é persistido quando esse novo checkpoint puder ser
confirmado; se a durabilidade for desconhecida, permanece diagnóstico efêmero e não há
novo `Save()`.

A linha de continuação descreve o comando que reidrata um envelope existente; ela não autoriza
criar uma segunda operação nem trocar silenciosamente `operationKind`. Ao continuar
`Apply` ou `Sync`, aplicam-se as fronteiras e o estado terminal dessas operações. Ao
continuar `Remove`, aplicam-se as passadas da linha `Remove` e o terminal possível é
`Removed`, `Partial` ou `OutcomeUnknown`. Uma recuperação autônoma, sem operação de negócio
a continuar, é o único caso em que `operationKind=Recovery` pode ser persistido.

### 4.5 Mensagens

Devem distinguir, no mínimo: gate bloqueado; Sync sem BC habilitado; Wizard com
habilitação de BC pendente; intenção não confirmada; API inexistente com
`GenerateApiObject=false`; API com identidade ambígua; API gravado; resultado
indeterminado; recuperação bloqueada; API legado sem metadata suficiente; remoção parcial.
Também devem existir mensagens distintas para diário indisponível (`JournalUnavailable`),
`Prepared` aguardando `Continue` ou `Abandon`, e `Active`/`Partial` exigindo
reconciliação explícita.

Seguem o mecanismo de internacionalização vigente e não expõem hashes ou identificadores
além do necessário ao diagnóstico.

---

## 5. Modo A — diário durável e único por KB

### 5.1 Identidade e localização do diário

O diário é um `File` próprio (`WikiFileKBObject` é o tipo técnico do SDK), distinto da
metadata de negócio e sem módulo. Há exatamente um por KB:

- nome do objeto na KB: `GxOpenApiBuilder_OperationJournal`;
- nome do arquivo externo: `GxOpenApiBuilder_OperationJournal.json`;
- conteúdo: schema, identidade da KB e da operação, intenção, estado, durabilidade,
  inventário e recibos;
- o nome fixo apenas localiza o candidato; posse, identidade e validade continuam sendo
  confirmadas pelo conteúdo e pela releitura por `FileId`.

Localização, medida e não suposta:

1. **ao abrir o fluxo**: buscar o nome fixo no índice já montado — 0 ms;
2. **durante o fluxo**: guardar o `Id` do File na criação e reler por `Id` — 0 ms;
3. **em recuperação**: resolver o nome fixo no índice; qualquer ausência, duplicidade,
   colisão externa ou conteúdo inválido bloqueia;
4. **nunca**: remontar o índice para reencontrar o diário — ~3,1 s.

### 5.2 Conteúdo

O diário adota integralmente o schema executável da decisão 24: `schemaVersion=1`
(inteiro), `journalKind=GOAB_OPERATION_JOURNAL`, identidade/correlação, envelope,
estado, inventário, recibos, abandono e bloqueio com tipos, nulabilidade e enums
fechados. A F3 não pode criar uma segunda nomenclatura nem aceitar campos livres para
substituir `Guid`, `FileId`, identidade composta ou hash.

O conteúdo pode crescer sem custo mensurável: a sonda mediu 247 bytes e 20 KB com o mesmo
tempo de gravação. O serializer deve rejeitar, antes do `File.Save()`, qualquer violação
da tabela da decisão 24, inclusive `retryEligible` incompatível, combinação inválida de
identidade e campo obrigatório ausente para o `operationKind`.

A metadata mantém os valores literais `GOAB_API_METADATA_B060_V1`,
`GOAB_API_METADATA_B060_V2` e `GOAB_API_METADATA_B060_V3`. V1/V2 entram somente por
leitura; Apply, Sync e B115 emitem V3. A normalização V1/V2→V3 preserva o contrato
completo existente, trata `levels` ausente como plano, materializa
`objects.sdts.own` somente quando o inventário puder ser validado e registra
`ownership.applicationId`. B115 é a exceção inventory-only explicitamente marcada por
`recovery.imported=true`; os caminhos não recuperados permanecem ausentes, e não
vazios. Remove legado não regrava a metadata só para preencher `applicationId`.

### 5.3 Ciclo de vida

Antes da primeira gravação de Transaction, Folder, SDT, Procedure, API ou metadata: criar
o envelope `Prepared`, salvá-lo, reler **pelo `Id`**, confirmar Transaction GUID,
OperationId, ApplicationId, hash e intenção; depois gravar e confirmar a transição para
`Active`. Só então começar o pipeline.

Se a criação ou a confirmação falhar, abortar antes de gravar qualquer objeto. Um diário
que existe mas não pôde ser confirmado fica com durabilidade desconhecida: não prosseguir,
e **não criar um segundo diário**.

Nas transições seguintes, conforme as fronteiras agrupadas de 4.4: confirmar os Saves e
Deletes pelo recibo; atualizar e reler o diário; se a atualização não for confirmada,
preservar o último estado durável e marcar durabilidade desconhecida. Nunca transformar
estado desconhecido em `Completed`; nunca iniciar uma segunda persistência de API para
“corrigir” um estado desconhecido.

As gravações do próprio journal não passam pelo `Persist(...)` da F2: usam a rotina de
durabilidade do diário, com confirmação por `FileId`, bytes e hash, e atualizam
`journalDurability` separadamente dos `PersistenceReceipt` dos objetos de negócio.

### 5.4 Recuperação

**API com resultado indeterminado.** Bloquear qualquer novo `API.Save()`; reler por
`PlannedApiGuid` — que existe desde o `Create` e é a chave direta; validar Transaction
GUID, ApplicationId, hash e contrato; tratar zero, um confirmado e múltiplos candidatos
como resultados distintos; concluir ausência só após consulta suficiente e registro;
bloquear em conflito. **Nunca criar um novo API por nome porque a primeira chamada
“pareceu falhar”.**

Comparado ao manuscrito expandido, esta recuperação é curta justamente porque a identidade é conhecida
antes da gravação.

**Diário com durabilidade desconhecida.** Não continuar a sequência; reler pelo `Id`;
resolver o nome fixo no índice; comparar ApplicationId, contrato, recibos e estados;
corrigir apenas se houver exatamente um diário próprio e uma versão recuperável; bloquear
em ausência, duplicidade, colisão, conflito ou leitura ambígua; **nunca criar um segundo
diário para ocultar a incerteza**.

**Falha antes do API final.** Ler o diário e os recibos; inventariar cada alvo por
identidade e hash; separar ausente, confirmado, divergente e desconhecido; comparar com a
intenção durável; então continuar apenas etapas ainda não executadas, reconciliar
manualmente ou bloquear. Nunca repetir a persistência do API havendo estado físico
confirmado ou desconhecido. **Reabrir o Wizard pelo caminho normal não é recuperação.**

Essa recuperação durável não substitui o B115. O B115 reconstrói metadata suficiente para
um `Remove` legado quando a metadata de negócio está ausente ou marcada como importada;
ele não reidrata consumidores parcialmente gravados por uma falha da F1 nem inventa um
contrato completo. Para esse caso, somente o diário com inventário e recibos da operação
atual pode autorizar continuação; se a intenção não for determinística, o resultado é
bloqueio e intervenção humana.

---

#### 5.4.1 Reidratação do plano e do inventário

A recuperação não reabre o Wizard nem depende de reconstruir um plano apenas por nomes. O
executor deverá:

1. ler e validar o envelope do diário (`schemaVersion=1`), `knowledgeBaseGuid`, `FileId`, hash, Transaction,
   `ApplicationId`, `OperationId` e `operationKind`;
2. reidratar o inventário persistido, associando cada alvo à identidade validada, à posse,
   à ação esperada, ao estado físico e aos recibos da tentativa;
3. reler cada alvo por GUID/FileId e classificar `Confirmed`, `Absent`, `Divergent`,
   `Unknown` ou `NotAttempted`;
4. selecionar somente a próxima etapa explicitamente autorizada pelo par
   `operationState`/`logicalStage`, sem repetir uma persistência confirmada ou
   `OutcomeUnknown`;
5. gravar e confirmar o novo checkpoint antes de continuar, ou produzir diagnóstico
   somente leitura e bloqueio quando a reconciliação não for determinística.

O contrato abaixo nomeia o serviço de reidratação, o executor de continuação e o
relatório que os consome, além de declarar a entrada e a saída de cada um: envelope
validado, inventário reidratado, próxima etapa autorizada, recibos novos e estado
terminal ou bloqueio. O executor não pode reabrir o Wizard nem resolver um alvo somente
por nome.

Os contratos ficam nomeados e fechados assim:

- `ApiPlanRecoveryReader.ReadAndValidate(WikiFileKBObject journalFile, Guid knowledgeBaseGuid)`
  recebe o File único e a identidade da KB e devolve `ValidatedJournal` ou
  `GateDiagnostic=JournalUnavailable`; valida schema, FileId externo, bytes, hash,
  identidade, durabilidade e a compatibilidade do envelope;
- `ApiPlanRecoveryRehydrator.Rehydrate(ValidatedJournal journal,
  IReadOnlyList<RecoveryTargetObservation> observations)` devolve
  `ApiPlanRehydratedOperation`, contendo `operationKind`, `operationId`,
  `applicationId`, inventário classificado, recibos já relacionados e a única
  `NextStep` autorizada, um `blockReason` persistível do enum fechado da decisão 24
  ou um `GateDiagnostic` quando não houver snapshot confirmável;
- `ApiPlanRecoveryExecutor.Continue(ApiPlanRehydratedOperation operation,
  RecoveryAuthorization authorization)` devolve `ApiPlanRecoveryResult`, com recibos
  novos, checkpoint durável, estado terminal ou bloqueio. `authorization` é obrigatória
  antes da primeira gravação de negócio da continuação;
- `ApiPlanRecoveryReport.FromResult(ApiPlanRecoveryResult result)` produz o relatório
  somente de apresentação. O relatório não grava, não decide e não cria recibos.

`RecoveryTargetObservation` carrega `objectType`, `identityKind`, GUID/FileId ou
identidade composta validada, nome apenas para diagnóstico, hash esperado quando
aplicável, `physicalState` e `confirmation`. `NextStep` é fechado em
`ContinueTransaction`, `ContinueFolder`, `ContinueSdts`, `ContinueProcedures`,
`ContinueApi`, `ContinueMetadata`, `ContinueRemovePass`, `Complete`, `Abandon` ou
`Block`. `RecoveryAuthorization` exige `humanConfirmed=true`, os IDs do envelope, o
`journalFileId`, `updatedUtc` e o hash do snapshot validado, além da confirmação de que
a etapa indicada é exatamente a `NextStep` autorizada e pode produzir a próxima gravação.
Essa vinculação é uma defesa de frescor e integridade contra alteração concorrente entre a
leitura e a ação (TOCTOU); não cria histórico nem uma segunda identidade para o journal.
O executor rejeita a autorização se qualquer parte dessa vinculação divergir do diário
revalidado, evitando continuar sobre um snapshot substituído entre a leitura e a ação.

As transições permitidas são fechadas: `Prepared/Pending` pode continuar com os mesmos
IDs ou ser abandonado; `Active/Running` pode continuar somente a próxima etapa ainda não
confirmada; `Partial` de Remove pode continuar apenas itens `Failed` retryable; qualquer
`OutcomeUnknown`, `NotAttempted` sem reconciliação, identidade divergente, leitura
ilegível ou `journalDurability=Unknown` bloqueia sem novo `Save`/`Delete`; se a
reconciliação não for determinística, o envelope permanece bloqueado e a recuperação não
abandona, limpa, terminaliza nem cria novos IDs. `Completed` e `Removed` são terminais e
exigem uma nova operação. Apply/Sync nunca repetem um API com estado confirmado ou
indeterminado.

### 5.5 Mudanças previstas por arquivo

| Arquivo ou área | Responsabilidade documental da F3 |
|---|---|
| `Src/Extension/Diagnostics/ApiPlanOperationJournal.*` | schema V1, serializer, `FileId`, hash, fases do envelope, checkpoints e confirmação de durabilidade |
| `Src/Extension/Diagnostics/ApiPlanRecoveryReader.*`, `ApiPlanRecoveryRehydrator.*`, `ApiPlanRecoveryExecutor.*` | leitura/validação do diário, reidratação do plano/inventário, seleção da próxima etapa segura e bloqueio explícito |
| `Src/Extension/Diagnostics/ApiPlanGeneratedApiRemovalPlan.cs` e `ApiPlanGeneratedApiRemover.cs` | inventário por identidade de todos os tipos removíveis, intenção antes do primeiro `Delete()`, `NotAttempted`, `StillPresentAfterDelete`, fila por passadas e ausência de retry para `OutcomeUnknown` |
| `Src/Extension/Package.cs` | comando contextual e fallback principal, gate do diário, recuperação explícita e retirada da oferta automática B115; literais conforme a matriz da decisão 33 |
| `Src/Extension/GenexusOpenApiBuilder.package` e `Groups` | `CommandDefinition` e `refid` de cada variante localizada do comando nas duas superfícies de menu, sem ID neutro compartilhado |
| `Src/Extension/Diagnostics/PrototypeWizardPreferences.cs`, `PrototypeWizardPreferencesCodec.cs`, `PrototypeWizardPreferencesDialog.cs` | compatibilidade de `OfferOrphanMetadataRecovery` e nova preferência `ShowRecoveryOptionProactively` |
| `Src/Extension/Diagnostics/ApiPlanOrphanMetadataRecovery.cs` e writers de metadata | leitura/normalização V1/V2 e gravação V3 em Apply, Sync e B115, sem regravar metadata legada apenas para preencher `ApplicationId` durante Remove |
| `Src/Extension/ExtensionLocalization.cs`, `Src/Domain/ExtensionOutputLocalization.cs`, `Tests/Localization/` | mensagens pt-BR, espanhol e inglês para bloqueio, recuperação, inventário e estado indeterminado |
| `Tools/Test-ExtensionCommandRegistration.ps1` e `Tests/` | sincronização do comando, schema, checkpoints, reidratação, inventário e validação IDE |

## 6. Modo B — checkpoint manual (histórico)

> **Histórico:** o Modo B não foi selecionado. Esta seção preserva a alternativa que foi
> comparada durante a decisão, mas não define comportamento da implementação S-B111.

O checkpoint registra, antes da primeira gravação e antes de qualquer reaplicação: a
seleção, a ordem prevista, nomes e identidades planejadas, e o próximo passo autorizado.
Após uma falha, registra também os objetos parciais observados.

A extensão **bloqueia** reaplicação automática. “Continuar” não pode significar chamar o
Wizard de novo.

O que o modo B **não pode fazer**, e precisa estar visível na UI e na documentação:
alegar que detecta ou reconcilia sozinha todo estado parcial, ou apresentar como
durabilidade algo que depende de confirmação humana. Gate, ordem física, identidade,
recibos, relatório e testes de falha continuam obrigatórios — some a recuperação
automática, não o rigor.

---

## 7. Correções obrigatórias ao manuscrito expandido v24

Esta seção é a razão de o documento existir agora.

**A qual documento estas correções se aplicam.** Existem dois documentos anteriores, e só um
deles contém o material corrigido aqui:

| Documento | Contém diário, seam, três dimensões? | Corrigido por esta seção? |
|---|---|---|
| [`2026-09-04-PLANO-API-OBJECT-GRAVACAO-UNICA.md`](2026-09-04-PLANO-API-OBJECT-GRAVACAO-UNICA.md) — plano aprovado em 2026-09-04 | **não**; escopo enxuto de reordenação | **não** — nada nele é desmentido aqui |
| [`2026-09-04-B111-MANUSCRITO-EXPANDIDO-V24.md`](2026-09-04-B111-MANUSCRITO-EXPANDIDO-V24.md) — expansão nunca aprovada | sim | **sim** |

O manuscrito expandido continua útil como origem das exigências de diário, recuperação e
remoção, mas os quatro pontos abaixo **estão errados nele** e foram medidos em campo:

| o manuscrito expandido diz | Medição de 2026-09-04 | O que vale |
|---|---|---|
| identidade por `PlannedApiGuid` **ou** `NewApiIdentityKey`, com marcador `GOAB-B111-IDENTITY` na `Description` | o `Guid` existe desde o `API.Create`, sobrevive ao `Save()` e reencontra o objeto | só `PlannedApiGuid`, **lido** e nunca atribuído; sem marcador, sem chave alternativa, sem helper de identidade |
| “ao abrir um fluxo, enumerar todos os diários B111” | localizar pelo nome no índice já montado custa 0 ms; remontar o índice custa ~3,1 s | resolver pelo nome fixo no índice na abertura e na recuperação; não enumerar por prefixo |
| reler o diário por GUID | `WikiFileKBObject.Get` aceita `int`, não `Guid` | guardar o **`Id`** na criação e reler por ele |
| “atualizar e reler o diário a cada etapa confirmada” | gravar um File custa ~1,1 s na KB grande; ~10 gravações somam ~11 s | checkpoints agrupados, com a política declarada (4.4) |

Some, junto com o marcador, a exigência do manuscrito de validar truncamento da `Description` —
que existe (256 caracteres, em silêncio) mas deixa de afetar esta frente e está registrada
como `B112`.

---

## 8. Testes da F3

Sobre o seam da F2, que permite injetar falha em qualquer fronteira:

1. intenção não confirmável → aborta antes do primeiro objeto;
2. falha antes e depois de cada gravação → estado físico e estágio lógico corretos e
   independentes;
3. `API.Save()` com resultado ambíguo → `OutcomeUnknown` e **nenhum** segundo
   `API.Save()`;
4. atualização do diário não confirmada → durabilidade desconhecida e bloqueio; ausência,
   duplicidade, colisão externa e conteúdo inválido no nome fixo tratados como casos
   distintos;
5. reaplicação sobre estado parcial → só etapas não confirmadas; nunca API duplicado;
   nunca intenção parcial sobrescrita em silêncio;
6. remoção com intenção própria → intenção registrada antes do primeiro `Delete()`,
   recibo por exclusão, `Removed` só com todos os alvos ausentes;
7. alvo previsto não localizado antes de `Delete()` → `NotAttempted`, nunca sucesso
   silencioso; a operação só pode ser `Removed` se o inventário completo confirmar a
   ausência de todos os alvos;
8. remoção de legado com metadata válida → intenção importada antes do primeiro `Delete()`;
9. remoção de legado com metadata ausente, corrompida ou insuficiente → bloqueio, sem
   nenhum `Delete()`;
10. `Delete()` que lança, mas cuja releitura imediata comprova que o objeto ainda existe
   → recibo `Failed` retryable de ordem de dependência e nova tentativa somente na
   passada seguinte do mesmo `Remove`;
11. `Delete()` que retorna, mas cuja confirmação imediata comprova que o objeto ainda
   existe → o mesmo recibo `Failed` retryable e a mesma regra de nova passada;
12. `Delete()` que lança ou retorna e cuja releitura comprova ausência → `Confirmed`,
   sem requeue;
13. `Delete()` que lança ou retorna com releitura ilegível, divergente ou ambígua →
   `OutcomeUnknown`, bloqueio e nenhum retry automático ou por recuperação;
14. interrupção durante a remoção → `operationState=Partial` e
    `logicalStage=RemovalPartial`, com a intenção preservada.

Um teste que apenas chama `Apply` de novo pelo caminho normal **não** comprova
recuperação.

---

## 9. Validação na IDE

Reinstalar a DLL conforme a política do repositório e validar depois dela.

1. Apply completo, Sync e Wizard, conferindo a intenção registrada e concluída;
2. cancelamento em cada fronteira, conferindo o estado reportado;
3. reentrada após cada cancelamento: recuperada ou bloqueada, nunca reaplicada às cegas;
4. remoção de API gerada por esta frente;
5. remoção de API legado, com metadata válida e com metadata insuficiente;
6. interrupção no meio da remoção;
7. medir o acréscimo real ao tempo de Apply na KB grande e comparar com o orçamento de 4.4;
8. executar o comando de recuperação explícita sobre a KB inteira, incluindo um caso
   recuperável e casos de divergência ou ambiguidade que permaneçam bloqueados;
9. iniciar uma nova operação depois da recuperação e confirmar que somente um diário fixo
   por KB é reutilizado.

---

## 10. Critérios de aceite

**Comuns:**

1. a intenção é registrada e confirmada antes da primeira gravação do pipeline;
2. estado físico e estágio lógico são reportados separadamente;
3. resultado indeterminado bloqueia continuação automática;
4. reaplicação não cria API duplicado nem sobrescreve intenção parcial em silêncio;
5. nenhuma exclusão ocorre sem intenção de remoção confirmada;
6. remoção de legado importa a intenção da metadata ou bloqueia antes do primeiro
   `Delete()`;
7. nome, Description canônica ou prefixo nunca autorizam exclusão sozinhos.

**Contrato ativo do Modo A:** a durabilidade do diário é uma terceira dimensão registrada;
recuperação compara intenção durável com inventário físico; a política de checkpoints está
declarada e o custo medido bate com o orçamento; nunca existe um segundo diário para a KB;
recuperação ambígua ou divergente continua bloqueada para correção humana.

As condições do antigo Modo B não são critérios de aceite: permanecem apenas na seção 6
como comparação histórica.

---

## 11. Riscos

| Risco | Mitigação prevista |
|---|---|
| o Modo A acrescentar segundos ao Apply de forma percebida como regressão | política de checkpoints declarada e medida (4.4); comparar com o Apply pós-F1 |
| o diário acumular registros na KB | manter um único File por KB; reutilizar ou substituir somente após estado terminal confirmado e `journalDurability=Confirmed` |
| reintroduzir “um pouco de A e um pouco de B” | o Modo A é o contrato selecionado; a comparação com B é histórica e não autoriza comportamento alternativo |
| a remoção de legado bloquear casos que hoje funcionam | o remover já reconstrói plano a partir da metadata (2.2); a F3 acrescenta registro de intenção, não restringe o que já valida |
| planejar sobre um sistema que ainda vai mudar | este plano mantém dependências explícitas de F1 e F2; a implementação deve revalidar os contratos contra o código vigente |

---

## 12. Fontes

- `Docs/Implementation/2026-09-04-B111-F1-PLANO-ORDEM-E-WRITER-FINAL.md`
- `Docs/Implementation/2026-09-04-B111-F2-PLANO-SEAM-E-RECIBOS.md`
- `Docs/Implementation/2026-09-04-B111-SONDAS-IDENTIDADE-E-DIARIO.md` (medições que impõem a seção 7)
- `Docs/Implementation/2026-09-04-PLANO-API-OBJECT-GRAVACAO-UNICA.md` (plano aprovado em 2026-09-04, escopo enxuto; superado como execução)
- `Docs/Implementation/2026-09-04-B111-MANUSCRITO-EXPANDIDO-V24.md` (expansão nunca aprovada, origem das exigências desta fase — ler junto com a seção 7)
- `Src/Extension/Diagnostics/ApiPlanGeneratedApiRemover.cs`
- `Src/Extension/Diagnostics/ApiPlanOwnedObjectDescription.cs`
- `Src/Extension/Diagnostics/ApiPlanKbObjectNameIndex.cs`
- `Docs/Foundation/06-BACKLOG_v0.1.md` — `B111`, e os colaterais `B112`, `B113`, `B114`

---

## 13. Anexo — recuperação de metadata órfã implementada em 2026-09-06

Este anexo não altera o plano. Ele registra código que **já existe** e que ocupa parte do
território da seção 4.3, para que a revisão por pares não desenhe por cima sem saber.

### 13.1 Por que foi implementado fora da fase

O caso da seção 2.2 — «metadata ausente» — deixou de ser hipótese. Em 2026-09-06, numa KB de
teste, o File de metadata foi apagado à mão e a ferramenta ficou sem saída: `Remover` e
`Sincronizar` bloqueiam por metadata ausente, e o `Wizard` abre bloqueado, com a única ação
disponível sendo Cancelar. É o `B115`.

Havia uma opção de recuperação no código desde 2026-09-05, mas **inalcançável**: ela era
oferecida depois de o Wizard concluir com sucesso, e o estado que ela resolve impede a
conclusão. O teste de campo de 2026-09-06 provou isso. A correção move a oferta para a
abertura do Wizard, antes do diálogo.

Esperar a F3 significaria manter a KB sem saída até a decisão do modo, que ainda estava
pendente quando este anexo foi escrito. O Modo A foi selecionado em 2026-09-07; portanto,
esta recuperação passa a ser uma capacidade absorvida pelo comando explícito da F3, sem
criar um contrato paralelo.

### 13.2 O que a recuperação grava

Uma metadata com o `schemaVersion` corrente e **apenas** o que a remoção consome, conforme
`ApiPlanGeneratedApiRemovalPlan.FromMetadata`:

| Bloco | Origem |
|---|---|
| `ownership.transactionName` / `transactionGuid` | a Transaction selecionada |
| `ownership.apiName` / `apiGuid` | o API Object encontrado e confirmado como próprio |
| `ownership.metadataFileName` | nome canônico |
| `objects.transactionFolder` | Folder canônico da Transaction, sempre com `wasCreated=false` |
| `objects.procedures` | as Procedures `proc<T>_API_*` **encontradas na KB** |
| `objects.sdts.own` / `.shared` | os SDTs **encontrados na KB**, por posse verificada |
| `recovery` | marca de intenção importada, seção 13.3 |

`wasCreated=false` é deliberado e conservador: não há como saber se o Folder foi criado pela
extensão, e a remoção não deve apagar um Folder que talvez seja do usuário.

### 13.3 O que ela deliberadamente **não** grava

Os blocos `fields`, `pagination`, `order`, `services`, `levels` e `transactionStructure`
ficam **ausentes** — não vazios. A diferença importa: o leitor de contrato existente
(`PrototypeWizardExistingApiContractReader`) tem fallback para a KB quando a chave falta, e
não tem quando ela está presente e vazia.

O motivo é que esses dados **não são recuperáveis**. Medido em 2026-09-06: o reader
reconstrói serviços, campos de Create/Update/Response, filtros, RestPath e SecurityLevel a
partir do API Object e dos SDTs; mas `servicesBasePath`, `defaultPageSize`,
`maximumPageSize`, ordenação estática, campos obrigatórios e a estrutura hierárquica
persistida existem **somente** na metadata. Reconstruí-los seria inventar.

É o mesmo empobrecimento que o `B110` mediu por outro caminho. Uma metadata que os
inventasse faria o `Sincronizar` comparar a API real contra uma descrição falsa e propor
mudanças destrutivas. Por isso a marca da seção 13.4 bloqueia o Sync.

### 13.4 A marca

```json
"recovery": {
  "imported": true,
  "importedAtUtc": "…",
  "source": "KbInventory",
  "notRecovered": ["fields", "pagination", "order", "services", "levels", "transactionStructure"]
}
```

O nome `imported` vem da seção 4.3 deste plano — «marcá-la como importada» —, não de
vocabulário novo. Enquanto a marca existir:

- **`Remover` funciona.** Tem o inventário completo de alvos, que é tudo o que consome.
- **`Sincronizar` recusa**, com mensagem própria: não há contrato com que comparar.
- Um `Wizard` + Apply completo reescreve a metadata inteira e a marca desaparece.

### 13.5 Decisões aprovadas para a consolidação

As três decisões abaixo fecham a interação entre a recuperação de metadata órfã e o
contrato do Modo A. Elas não encerram a revisão por pares: ainda precisam ser refletidas
na implementação e validadas na matriz da F3.

1. **Gate e metadata importada:** metadata importada completa quanto aos alvos não é
   `Partial` nem `OutcomeUnknown`. Um diário da S-B111 em fase de envelope `Prepared` ou
   `Active`, com `operationState` `Pending`, `Running`, `Partial` ou `OutcomeUnknown`,
   bloqueia novas operações e a recuperação de
   metadata. Metadata importada validada pode liberar somente `Remove`, com confirmação
   explícita; `Apply` completo pode substituir sua marca, e `Sync` não pode usá-la como
   contrato completo.

2. **Suficiência do inventário:** a recuperação produz explicitamente
   `InventorySufficient` ou `InventoryInsufficient`. O primeiro exige schema, Transaction
   GUID, API GUID confirmado, referências completas, distinção entre SDTs próprios e
   compartilhados e revalidação sem ambiguidade de todos os alvos; só ele permite
   `Remove`, ainda com confirmação explícita. Para Procedures e SDTs legados, a
   revalidação pode usar identidade histórica composta por nome exato, tipo, papel,
   `Description` canônica vinculada à Transaction/API, unicidade e ausência de conflito;
   nome isolado não basta. Metadata produzida pela S-B111 deve conter `ApplicationId` e
   mantê-lo compatível com o diário. Metadata legada sem esse campo pode ser suficiente:
   nesse caso, a primeira operação S-B111 gera um `ApplicationId` novo e o registra no
   diário, sem regravar o File legado durante `Remove`. Campo ausente além desse caso,
   referência desconhecida, conflito, múltiplos candidatos ou identificação insuficiente
   produz `InventoryInsufficient` e bloqueia. `InventorySufficient` é suficiente para a
   remoção, mas não prova que nenhum objeto histórico ficou fora do inventário.

3. **Precedência entre diário e metadata:** o diário é autoritativo para estado da
   operação, recibos e intenção atual; a metadata é a fonte do último contrato completo
   conhecido da API. Diário não terminal sempre bloqueia. Diário terminal e metadata podem
   coexistir somente quando identidade e contrato forem compatíveis; qualquer divergência
   bloqueia e exige reconciliação explícita. Nenhuma fonte substitui silenciosamente a
   outra, e a precedência do diário não transforma metadata incompatível em contrato válido.

Para não deixar o B115 fora do protocolo durável, sua execução também é fechada. Quando
não estiver continuando um envelope de negócio existente, ela usa `operationKind=Recovery`,
`intentKind=Imported` e novos `operationId`/`applicationId`. O diário segue exatamente
quatro checkpoints: (1) `Prepared/Pending/IntentionRecorded`, antes de tocar a metadata;
(2) `Active/Running/MetadataPending`, confirmado antes do `File.Save()`;
(3) recibo F2 do File confirmado por `FileId`, bytes e hash, com
`Running/ApiPhysicallySaved` substituído pelo estágio `MetadataRecovered`; e (4)
`Completed/Completed`, com `journalDurability=Confirmed`. Se o Save ou sua confirmação
for indeterminado, o diário termina em `OutcomeUnknown`/bloqueio e o File não é regravado
de novo. Se o File for reutilizado, a confirmação continua sendo pelo mesmo `FileId`, não
por enumeração de nome. O resultado de B115 é somente inventário para Remove; não autoriza
Sync nem uma reidratação de consumidores parciais da F1.

### 13.6 Evidência de campo — 2026-09-06, KB `wsEducacaoSpTeste`, Transaction `Teste`

Primeira execução real do caminho. O File `apiTeste_Metadata` foi apagado à mão.

| Passo | Resultado |
|---|---|
| Abrir o Wizard, recusar a oferta | `Recuperação recusada pelo usuário. Nenhuma alteração foi feita por B115.` O Wizard seguiu abrindo, bloqueado, e o usuário cancelou. Nada gravado. |
| Abrir de novo, aceitar | `Metadata órfã recuperada: File='apiTeste_Metadata', Guid='43be3a1b…', Bytes=2290, Procedures=5, SdtsProprios=18, SdtsCompartilhados=3.` |
| `Sincronizar` | Bloqueou. Relatório final `Interrupted`, `Bloqueados=1`, com a mensagem da 13.4. |
| Reabrir o Wizard | Abriu **destravado** — «Estado: teste de reencontro», `Concluir e aplicar` habilitado. A oferta não reapareceu: `File 'apiTeste_Metadata' encontrado 1 vez(es); recuperação órfã não se aplica.` |
| Apply completo | `SuccessWithWarnings`, `Criados=0`, `Atualizados=7`, `Bloqueados=0`. A metadata foi reescrita **no mesmo File** (`Guid='43be3a1b…'`), passando de 2290 para `Bytes=117926`. A marca desapareceu com a substituição do JSON. |

Dois números confirmam o desenho: **2290 bytes** contra os **117926** da metadata completa — a diferença é exatamente o contrato que a 13.3 diz não recuperar —, e o **mesmo GUID** nas duas pontas, provando que o Apply reencontra e sobrescreve o File da recuperação em vez de criar um segundo.

### 13.7 O `Remover` sobre a metadata recuperada — e o defeito que ele expôs

Testado no mesmo dia, e **falhou na primeira tentativa**. A remoção parou no meio com o API
Object, 5 Procedures e 5 SDTs já apagados:

```
'sdtTeste_API_ListFilters' is referenced at least by 'sdtTeste_API_ListResponse'
```

A causa não era o inventário — estava completo — e sim a **ordem**. `ResolveOwnSdtNames` usa
`objects.sdts.own` como está, e essa ordem carrega as dependências entre SDTs; a recuperação
a gravava em ordem alfabética, que não é ordem de dependência nenhuma. O teste que
acompanhava a recuperação verificava *o quê* era gravado, nunca *em que ordem*, e por isso
passava.

Dois defeitos colaterais apareceram junto:

1. o relatório final informou `Removidos: (nenhum)` com onze objetos fora da KB — a lista era
   local ao remover e a exceção subia antes de ela chegar ao relatório;
2. a metadata recuperada guarda o `apiGuid` do API Object daquele momento. Removido esse
   objeto e recriado outro pelo Wizard, a metadata passou a apontar para um GUID inexistente,
   o Wizard travou em `OwnershipSchemaApiNameOrGuidMismatch` e a recuperação **não se
   oferecia** — o File existia. Saída: apagar o File à mão e recuperar de novo.

O segundo deles foi corrigido no mesmo dia: a recuperação passou a se oferecer também com o
File presente, sob três condições — ele é único, está marcado como intenção importada, e o
`apiGuid` que registra não é o do API Object que existe. Nesse caso ela regrava o inventário
**sobre o mesmo File**; criar um segundo homônimo apenas trocaria o bloqueio por ambiguidade
de metadata. Metadata **completa** com o mesmo descompasso continua fora do alcance, e é
deliberado: o fingerprint `B067` cobre o conteúdo inteiro, então corrigir só o `apiGuid`
levaria de `OwnershipSchemaApiNameOrGuidMismatch` a `FingerprintHashMismatch`, sem sair do
lugar. Recalcular o fingerprint significaria revalidar o contrato — outra frente.

Essa modalidade está coberta por teste de contrato e **nunca foi executada na IDE**: recriar o
cenário exige remover a API e gerá-la de novo por cima da metadata recuperada.

**Comportamento implementado e medido em campo em 2026-09-06:** a correção tornou a
remoção resiliente à ordem dos SDTs por meio de requeue entre passadas. Quando `Delete()`
retorna, o código faz uma releitura imediata pela identidade validada: ausência confirma a
exclusão; presença lança uma falha comum que o laço externo reencaminha para a fila. Quando
`Delete()` lança, a exceção sobe diretamente ao laço externo, que também faz requeue
incondicional, sem uma releitura nesse caminho. Esse comportamento não produz
`OutcomeUnknown` nem distingue ainda falha retryable de resultado indeterminado por um
recibo do seam.

Enquanto cada passada apagar ao menos um objeto há progresso; uma passada inteira sem
progresso encerra e reporta os pendentes. A recusa não é interpretada pela mensagem da
exceção — só o abort do usuário é relançado na hora. Essa é a implementação observada no
campo, não o contrato final da S-B111.

**Contrato-alvo ainda não implementado da F2/F3:** depois de cada `Delete()`, tenha ele
lançado ou retornado, o seam deverá publicar um recibo com a confirmação possível pela
identidade validada. Somente se a releitura comprovar que o objeto ainda existe a falha
será classificada como conhecida e retryable por ordem de dependência; esse item voltará
para a fila e será tentado na passada seguinte da mesma operação. Se a releitura comprovar
ausência, a exclusão estará confirmada e o item não voltará para a fila. Se a releitura for
ilegível, divergente ou ambígua, o resultado será `OutcomeUnknown`, a operação ficará
bloqueada e não haverá retry automático nem recuperação posterior desse item. A política de
retry não se aplica a `Save()` nem permite repetir o objeto em uma operação posterior. A
F2 classificará e exporá o resultado, mas somente a F3 transformará `Failed` retryable em
item da passada seguinte.

**Segunda execução, com a correção:**

```
Remocao de SDTs passada 1: apagados=17, adiados=1.
Remocao de SDTs passada 2: apagados=1,  adiados=0.
Relatório final: Resultado='Success', Removidos=24, Bloqueados=0, DuraçãoMs=4110.
```

O adiado foi o `sdtTeste_API_ListFilters` — o mesmo que travara a primeira tentativa —, e ele
sai como penúltimo da lista de removidos, depois que `ListResponse` deixou de referenciá-lo.
O Folder `TesteOpenApi` ficou vazio e **não foi apagado** (`wasCreated=false`, 13.2), e os três
SDTs compartilhados foram preservados.

O ciclo foi repetido logo depois sobre a API **completa** — 5 Procedures, com o `Delete` que
faltava na primeira —, e o resultado foi `Removidos=25` com o mesmo `passada 1: apagados=17,
adiados=1` e `passada 2: apagados=1, adiados=0`. O inventário mudou de 24 para 25 objetos e o
número de adiados não: com a lista em ordem alfabética o adiamento é **determinístico**, não
acidental — `ListFilters` sempre precede o `ListResponse` que o referencia, e sempre exige
exatamente uma segunda passada.

A execução também confirmou o que a 13.2 promete sobre o inventário: a metadata recuperada
listou **4** Procedures, não 5, porque o `procTeste_API_Delete` não existia mais na KB, e a
validação as aceitou por Description, sem depender de GUID.

**O `B115` está atendido:** a recuperação devolve a capacidade de remover uma API gerada cuja
metadata se perdeu. O que ela não devolve — e continua não devolvendo — é o contrato (13.3).

**Fechamento do ciclo.** Reaplicada a API pelo Wizard, o `PlannedContractHash` voltou a
`16DF0B0A0572C17A6D0462594060693E71214A47DE978B8239CFDDCDEF3BFA6C` — **o mesmo de antes de
todo o experimento**, provando que o contrato foi restaurado sem perda. O `Build All`
concluiu com `Success` nos dois environments da KB, `NETPostgreSQL155` e
`NETFrameworkSQLServer004`: as 5 Procedures, o API Object e os 18 SDTs hierárquicos foram
especificados e gerados nos dois, com documentação REST. Nas permissões GAM o `apiTeste`
aparece com o GUID gravado pela última aplicação. Os avisos do build são todos preexistentes
e de outros objetos da KB.

### 13.8 Custo

Uma gravação de `WikiFileKBObject` por recuperação: ~130 ms na KB pequena, **~1,1 s** na
grande (seção 2.3, e item `B114` do backlog). A recuperação é opt-in, ocorre uma vez por
incidente e não entra no orçamento de gravação da seção 4.4.
