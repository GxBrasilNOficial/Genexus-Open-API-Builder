# S-B111 — decisões aprovadas para F1, F2 e F3

**Data do registro:** 2026-09-07
**Natureza:** registro versionado e canônico das decisões humanas aprovadas para a S-B111.
Ele orienta os planos F1, F2 e F3 e futuras revisões, mas não é um plano executável nem
autoriza implementação, build, instalação, commit ou push.
**Repositório:** `Genexus-Open-API-Builder`

> **Instrução para agentes futuros:** leia este registro antes de propor alterações na
> S-B111. Ele preserva decisões já tomadas, não é um parecer de revisão por pares e não
> deve ser reinterpretado como autorização para executar código. Se um plano ou outra
> fonte divergir deste registro, sinalize a divergência e peça decisão humana; não escolha
> silenciosamente uma versão. Novas decisões devem ser acrescentadas em seção datada e em
> commit próprio.

## Contexto

A primeira revisão técnica dos planos F1, F2 e F3 identificou seis gaps. As decisões abaixo foram tomadas antes da continuidade da revisão e devem orientar a consolidação dos planos.

O registro usa nomes funcionais. Códigos internos de backlog não fazem parte da nomenclatura da operação para o usuário.

## Decisões de contrato

### 1. Identidade da Transaction

O contrato carregará o `TransactionGuid` estável junto com o nome da Transaction.

Validações de preflight, contexto transitório, produtores, consumidores e relatório deverão usar a identidade por GUID + nome. Colisão apenas por nome não será considerada suficiente.

### 2. Relatório final

Em falha, colisão ou `OutcomeUnknown`, o fluxo gerenciado não fará fallback para resolver a API apenas pelo nome.

O relatório deverá indicar que o objeto não foi identificado com segurança. Link para a API somente será emitido quando o GUID tiver sido validado.

### 3. Persistência e recibos

O seam de persistência cobrirá tanto `Save()` quanto `Delete()`.

Cada operação deverá possuir recibo individual. Uma confirmação explícita será necessária para classificar a operação como `Confirmed`.

Retorno normal sem confirmação, confirmação ausente ou confirmação divergente será `OutcomeUnknown`.

`OutcomeUnknown` não significa que nada foi persistido; significa que o resultado não pôde ser provado. Não haverá repetição automática da persistência nesses casos.

### 4. Diário durável

Será usado o Modo A: um único `File object` da KB para registrar o estado durável das operações.

O File será:

- único por KB;
- reutilizado entre operações;
- mantido após uma operação concluída;
- sem histórico ilimitado;
- reutilizado ou substituído somente quando estiver em estado terminal
  confirmado (`Completed` ou `Removed`) e com `journalDurability=Confirmed`;
- sem atribuição explícita de módulo, seguindo o padrão do File de preferências da KB.

O mesmo diário cobrirá:

- `Apply`: aplicação pelo Wizard;
- `Sync`: comando Sincronizar;
- `Remove`: comando Remover API gerada.

O conteúdo registrará o tipo da operação, correlação, identidade da Transaction/API, plano, objetos planejados, recibos e estados duráveis.

### 5. Nome e posse do File

Nome do `File object`:

```text
GxOpenApiBuilder_OperationJournal
```

Nome externo do conteúdo:

```text
GxOpenApiBuilder_OperationJournal.json
```

Descrição canônica de posse:

```text
GxOpenApiBuilder_OperationJournal - by Genexus Open API Builder
```

Na documentação, usar `File object`. No código, `WikiFileKBObject` poderá permanecer apenas como nome técnico da classe do SDK, ou receber alias interno se isso melhorar a legibilidade.

Política de colisão:

- zero File com o nome: criar;
- exatamente um File próprio e válido: reutilizar;
- File manual, externo ou inválido: bloquear sem sobrescrever;
- mais de um File com o nome: bloquear por ambiguidade.

### 6. Checkpoints

Serão usados três checkpoints lógicos:

1. antes de qualquer alteração na KB;
2. depois da confirmação do API Object;
3. na conclusão da operação.

O plano registra quatro gravações físicas para essa política na KB grande, com acréscimo medido de aproximadamente 4,4 segundos. Não será usado o modelo de uma gravação por objeto.

## Recuperação

### 7. Início explícito

A recuperação será iniciada exclusivamente pelo comando:

```text
Recuperar operação interrompida
```

O comando ficará primariamente no menu de contexto da Transaction. O menu principal
`Genexus Open API Builder` oferecerá o mesmo comando como fallback para diagnosticar e
recuperar a KB inteira, sem exigir uma Transaction selecionada.

Abrir o Wizard ou executar Sync não iniciará recuperação automaticamente.

### 8. Bloqueio de operação concorrente ou incompleta

Enquanto o diário estiver em estado não terminal, ficam bloqueados:

- Apply;
- Sync;
- Remove;
- recuperação de metadata órfã.

Estados não terminais incluem `Pending`, `Running`, `Partial` e `OutcomeUnknown`. A única saída normal será o comando explícito de recuperação.

### 9. Regra conservadora de recuperação

A recuperação poderá continuar somente etapas ausentes e claramente identificadas.

Ela não deverá:

- repetir objetos com recibo `Confirmed`;
- repetir objetos com estado `OutcomeUnknown`;
- criar uma nova API por nome;
- prosseguir diante de divergência, múltiplos candidatos ou identidade ambígua.

Quando não houver evidência suficiente, o comando informará o bloqueio e manterá o diário intacto.

Não haverá recuperação forçada nem limpeza automática do diário.

### 10. Relação com metadata da API

O diário e a metadata `api<Transação>_Metadata` são artefatos diferentes:

- metadata da API: último contrato completo conhecido;
- diário: estado da operação atual ou interrompida.

O fluxo de recuperação de metadata órfã não poderá executar enquanto o diário estiver em estado não terminal. Metadata marcada como importada ou reconstruída não substitui o diário.

### 11. Retry limitado de Delete dentro da mesma operação

Decisão aprovada após a revisão do painel:

- um `Delete()` que lança não é presumido como sucesso nem como ausência de efeito;
- se uma releitura imediata, pela identidade validada, comprovar que o objeto ainda
  existe, o recibo será `Failed` com falha conhecida retryable de ordem de dependência,
  tenha o `Delete()` lançado ou retornado sem remover o objeto; uma nova tentativa
  poderá ocorrer dentro da mesma operação de Remove;
- se a releitura imediata comprovar a ausência do objeto, o recibo será `Confirmed` e
  não haverá requeue;
- cada tentativa deverá ter recibo próprio, sem transformar a tentativa anterior
  em sucesso;
- se a releitura não puder determinar com segurança a existência ou a identidade do
  objeto, permanece `OutcomeUnknown` e não haverá nova tentativa;
- nenhuma nova operação ou recuperação posterior poderá repetir um objeto em
  `OutcomeUnknown`.

Essa exceção é limitada ao Remove em andamento, com progresso e limite de passadas,
e não autoriza retry entre operações. O ponto comum da F2 apenas registra e relança;
o requeue e a nova passada pertencem à F3.

### 12. Base do seam de persistência da F2

Decisão aprovada:

- reutilizar e promover o `ApiPlanSaveBoundaryProbe` como base do seam
  definitivo da F2;
- não criar um segundo `ApiPlanPersistenceProbe` com escopo e instrumentação
  paralelos;
- preservar os eventos e fingerprints úteis do B109 enquanto ainda forem
  necessários;
- planejar explicitamente a retirada ou a absorção do comportamento temporário
  do B109 quando essa frente for encerrada.

Essa decisão não implementa ainda os recibos, a confirmação pós-`Save`, o
tratamento de `Delete` nem o diário durável; ela escolhe apenas a base de
instrumentação que deverá receber esses contratos.

### 13. Contrato de preservação do executor único da F2

Decisão aprovada:

- a unificação dos laços de `saveSteps` somente poderá substituir os dois
  fluxos atuais depois de teste executável de equivalência comportamental;
- o teste deverá cobrir `Report`, `Pump`, `ThrowIfAbortRequested`, snapshots
  antes e depois do `Pump`, eventos do `ApiPlanSaveBoundaryProbe`, cronômetro,
  rótulo de estágio (`List` ou `Business Component`), caminho opcional de
  `Delete`, `Saved`, `Failed` e relançamento da exceção;
- o executor será um orquestrador do fluxo de gravação e não decidirá
  recuperação, retry ou interpretação de estado.

Essa decisão reforça a aceitação da F2 sem alterar ainda o código, os planos,
os testes ou o manifesto.

### 14. Identidade única da Transaction

Decisão aprovada:

- reutilizar o `TransactionGuid` real da Transaction selecionada como origem
  da identidade da operação;
- propagar esse GUID pelo `ApiPlan`, contexto transitório, preflight, writers,
  relatório e diário;
- manter o nome da Transaction junto do GUID para exibição e validação;
- bloquear divergência entre GUID e nome;
- não criar uma segunda identidade, como `PlannedTransactionGuid`, para o
  fluxo da S-B111.

O `TransactionGuid` já existente em seleção, metadata, remoção, leitura de
estado e sincronização será tratado como a identidade única do fluxo.

### 15. Fallback por nome no relatório final

Decisão aprovada:

- no fluxo gerenciado da S-B111, o relatório somente poderá associar o API
  Object principal mediante GUID planejado e confirmado;
- a busca por nome não poderá escolher automaticamente o objeto principal;
- sem GUID confirmado, o relatório permanecerá sem link para o API Object;
- ausência, múltiplos candidatos ou ambiguidade deverão aparecer como
  diagnóstico explícito;
- uma busca por nome poderá existir apenas como diagnóstico secundário, nunca
  como fallback autoritativo.

### 16. Bloqueio por KB e recuperabilidade demonstrada

Decisões aprovadas:

- o diário único representa no máximo uma operação ativa por KB;
- estados não terminais (`Pending`, `Running`, `Partial` e
  `OutcomeUnknown`) bloqueiam novas operações na KB inteira;
- não será criado um segundo diário para contornar o bloqueio;
- a F3 somente poderá ser considerada concluída quando um teste end-to-end
  demonstrar uma recuperação explícita capaz de concluir uma operação
  interrompida e liberar a KB para uma nova operação;
- a recuperação deverá validar o diário, a Transaction, os GUIDs, a aplicação,
  o contrato e os recibos antes de continuar;
- em estado determinístico, deverá continuar apenas etapas ausentes e não
  ambíguas, marcar o diário como terminal e permitir sua reutilização;
- em estado ambíguo, deverá informar o conflito e a correção manual necessária,
  sem adivinhar nem limpar o diário, permitindo uma nova tentativa após a
  resolução humana.

Essa decisão exige prova executável da capacidade de destravar a KB; não
promete que a ferramenta possa resolver automaticamente toda ambiguidade.

### 17. Comando de recuperação dentro da F3

Decisão aprovada:

- o comando de recuperação fará parte da F3, não de uma futura F4;
- ficará primariamente no menu de contexto da Transaction, usando a Transaction
  selecionada;
- terá fallback no menu principal da extensão para diagnosticar e recuperar a KB inteira;
- atuará sobre o diário e o estado da KB atual;
- no fallback do menu principal, não exigirá uma Transaction selecionada;
- bloqueará diante de diário ausente, múltiplos candidatos ou identidade
  ambígua;
- validará o diário e os recibos antes de executar qualquer continuação;
- será incluído no teste end-to-end que demonstra o desbloqueio da KB.

### 18. Nome fixo do diário na documentação

Decisão aprovada:

- retirar dos planos da S-B111 o desenho baseado em
  `B111_J_<hash(TransactionGuid + PlannedApiName)>`;
- retirar a busca por prefixo e a premissa de um diário por aplicação;
- consolidar em toda a documentação o objeto único por KB
  `GxOpenApiBuilder_OperationJournal`;
- consolidar o nome externo
  `GxOpenApiBuilder_OperationJournal.json`;
- usar o GUID, a aplicação, o contrato e os recibos no conteúdo para validar
  posse e identidade, não para gerar o nome do File.

### 19. Metadata importada versus operação parcial

Decisão aprovada:

- metadata importada e estado de operação parcial ou indeterminada são eixos
  distintos;
- um diário da S-B111 em estado não terminal (`Pending`, `Running`, `Partial`
  ou `OutcomeUnknown`) sempre bloqueia novas operações e a recuperação de
  metadata;
- metadata importada completa e validada não será tratada como `Partial` ou
  `OutcomeUnknown`;
- metadata importada poderá liberar somente `Remove`, com confirmação
  explícita e inventário completo dos alvos;
- metadata importada incompleta ou ambígua bloqueará a remoção;
- `Apply` completo poderá substituir a marca de metadata importada;
- `Sync` não poderá usar metadata importada como se fosse contrato completo
  para comparação.

### 20. Precedência entre diário e metadata

Decisão aprovada:

- o diário será a fonte de verdade do estado da operação, dos recibos e da
  intenção atual;
- a metadata será a fonte do último contrato completo conhecido da API;
- diário não terminal sempre bloqueará, mesmo que a metadata pareça válida;
- diário terminal e metadata poderão coexistir quando identidade e contrato
  forem compatíveis;
- divergência entre diário e metadata não será resolvida automaticamente;
- nessa divergência, o fluxo bloqueará e exigirá reconciliação explícita;
- a precedência do diário vale para decidir se a operação pode continuar, mas
  não transforma metadata incompatível em contrato válido.

Não haverá combinação silenciosa em que metadata importada substitua o diário
ou o diário substitua metadata incompatível.

### 21. Suficiência do inventário para `Remove`

Decisão aprovada:

- a recuperação de metadata deverá produzir um resultado explícito de
  `InventorySufficient` ou `InventoryInsufficient`;
- `InventorySufficient` exigirá schema válido, Transaction GUID,
  ApplicationId, API GUID, referências completas, distinção entre SDTs
  próprios e compartilhados e revalidação de todos os alvos sem ambiguidade;
- `Remove` poderá prosseguir somente com `InventorySufficient` e confirmação
  explícita;
- qualquer campo ausente, referência desconhecida, conflito, candidato
  múltiplo ou identificação baseada apenas em nome produzirá
  `InventoryInsufficient`;
- `InventoryInsufficient` bloqueará `Remove` e exigirá Apply completo ou
  correção manual;
- `InventorySufficient` significa inventário suficiente para a remoção, não
  prova histórica de que nenhum objeto antigo ficou fora do inventário.

### 22. Alcance das provas da F2

Decisão aprovada:

- testes offline provarão o seam, os recibos, o executor e os cenários
  injetáveis usando delegates ou doubles;
- sentinelas e testes textuais provarão a cobertura dos pontos de integração
  dos writers;
- somente a validação na IDE poderá provar o fluxo real, a contagem de
  `Save()` e a ordem efetiva de persistência dos objetos;
- a documentação não poderá apresentar `Add-Type` como prova do pipeline
  completo dependente do SDK.

### 23. Política SDK-free para `includeBusinessComponentParameters`

Decisão aprovada:

- extrair a decisão dos três ramos para uma unidade pequena e testável sem
  referências ao SDK GeneXus;
- a unidade receberá apenas fatos resolvidos pelo adaptador do writer;
- BC participado produzirá `true`;
- API próprio preexistente sem BC usará o valor persistido;
- API novo sem BC produzirá `false` a partir do contexto transitório;
- API ambíguo ou externo não poderá ser tratado como API novo e bloqueará;
- o writer continuará responsável por consultar a KB e gerar Source e
  Variables, mas não será a unidade primária da regra de decisão;
- testes offline cobrirão os três ramos e a ambiguidade.

### 24. Schema versionado do diário

Decisão aprovada:

- o JSON do diário terá schema de fio versionado, começando por `schemaVersion: 1`
  (inteiro; não a string `V1`);
- versão desconhecida bloqueará a operação e a recuperação;
- não haverá migração destrutiva automática;
- estado da operação, durabilidade, intenção e recibos serão registrados como
  dimensões distintas;
- cada recibo identificará `Save` ou `Delete`, tipo de objeto, identidade
  planejada, identidade persistida, resultado e confirmação;
- mudanças incompatíveis exigirão nova versão explícita do schema.

O contrato executável do schema V1 do diário é este. Os nomes abaixo são os nomes
JSON; propriedades C# podem seguir a convenção PascalCase, mas não podem alterar os
nomes serializados.

| Caminho | Tipo e nulabilidade | Regra |
|---|---|---|
| `schemaVersion` | inteiro obrigatório | valor exato `1` |
| `journalKind` | string obrigatória | valor exato `GOAB_OPERATION_JOURNAL` |
| `knowledgeBaseGuid` | GUID obrigatório | deve corresponder à KB atual |
| `transactionGuid` | GUID obrigatório | identidade autoritativa da Transaction |
| `transactionName` | string não vazia | exibição e conferência, nunca identidade isolada |
| `operationId` | GUID obrigatório | correlação da operação; distinto de `applicationId` |
| `applicationId` | GUID obrigatório | novo por tentativa; preservado ao continuar a mesma operação |
| `operationKind` | enum obrigatório | `Apply`, `Sync`, `Remove` ou `Recovery` |
| `generatorVersion` | string não vazia | versão do gerador que criou ou atualizou o diário |
| `createdUtc`, `updatedUtc` | `date-time` UTC obrigatórios | ISO-8601 com deslocamento `Z` |
| `envelopePhase` | enum obrigatório | `Prepared` ou `Active` |
| `operationState` | enum obrigatório | `Pending`, `Running`, `Partial`, `OutcomeUnknown`, `Completed` ou `Removed` |
| `logicalStage` | enum obrigatório | `NotStarted`, `IntentionRecorded`, `TransactionPending`, `FolderPending`, `SdtsPending`, `ProceduresPending`, `ApiPending`, `ApiSaveOutcomeUnknown`, `ApiPhysicallySaved`, `MetadataPending`, `MetadataRecovered`, `RemovalInProgress`, `RemovalPartial`, `RecoveryInProgress`, `Abandoned`, `Completed` ou `Removed` |
| `journalDurability` | enum obrigatório | `Confirmed` ou `Unknown` |
| `intentKind` | enum obrigatório | `Current` ou `Imported` |
| `metadataSchemaVersion` | enum anulável | `GOAB_API_METADATA_B060_V1`, `GOAB_API_METADATA_B060_V2` ou `GOAB_API_METADATA_B060_V3`; obrigatório quando houver metadata |
| `plan` | objeto obrigatório | varia conforme `operationKind` |
| `inventory` | array obrigatório, possivelmente vazio | cada alvo e preservação aparecem uma vez |
| `receipts` | array obrigatório, possivelmente vazio | sequência monotônica dentro da operação |
| `abandonment` | objeto anulável | obrigatório somente quando `logicalStage=Abandoned` |
| `blockReason` | enum anulável | obrigatória em um checkpoint confirmado que registre bloqueio, `OutcomeUnknown` ou reconciliação pendente; valores V1 persistíveis: `OutcomeUnknown`, `InventoryInsufficient`, `IdentityAmbiguous`, `IdentityDivergent`, `UnreconciledNotAttempted`, `TargetAbsentBeforeDelete`, `StageFailed` ou `RetryBudgetExhausted` |

O contrato separa o motivo persistido no envelope do diagnóstico que impede uma
gravação. `blockReason` só pode ser alterado junto de um snapshot de diário que
tenha `journalDurability=Confirmed`. Quando o diário não existe, está inválido ou
quando o `Save()`/a releitura não permitem confirmar o novo snapshot, não há campo
JSON novo a emitir: o resultado usa `GateDiagnostic` somente no relatório. Esse
diagnóstico tem os códigos `GateBlocked`, `JournalUnavailable`, `DurabilityUnknown`
e `PreconditionFailed`; o payload também carrega uma `reasonCode` estável e os detalhes
da pré-condição em estrutura própria, não apenas em texto livre. `GateBlocked` exige
que o journal já esteja legível, válido e com durabilidade confirmada; portanto,
`JournalUnavailable` e `DurabilityUnknown` prevalecem quando a indisponibilidade ou a
durabilidade desconhecida impedirem confirmar o journal. `PreconditionFailed` é um
diagnóstico de pré-condição da operação e não uma subcausa de `GateBlocked`.
`GateBlocked` também não é um `logicalStage` persistido.

O mapeamento de diagnóstico é fechado e segue esta precedência:

| Código | Quando é usado | Exemplos de `reasonCode` | Efeito de persistência |
|---|---|---|---|
| `JournalUnavailable` | o diário está ausente, inválido, duplicado ou não pode ter identidade, schema ou hash validados antes de uma nova intenção | `JournalMissing`, `JournalInvalid`, `JournalDuplicate`, `JournalIdentityDivergent` | somente relatório; não cria `blockReason` novo |
| `DurabilityUnknown` | o `Save()` ou a releitura do diário foi tentado, mas o snapshot novo não pôde ser confirmado | `JournalSaveUnconfirmed`, `JournalReloadDivergent` | preserva o último snapshot durável; não afirma o motivo novo |
| `GateBlocked` | o diário é legível, válido e durável, mas o estado global impede a operação solicitada | `JournalNonTerminal`, `PreparedContinuationNotAuthorized`, `UnreconciledOutcome`, `RecoveryAuthorizationStale`, `RecoveryAuthorizationLockUnavailable` | não cria nova operação nem altera o envelope |
| `PreconditionFailed` | uma pré-condição específica da operação falha antes da primeira mutação, com o diário ainda utilizável | `IdentityAmbiguous`, `IdentityDivergent`, `InventoryInsufficient`, `AuthorizationMismatch` | somente relatório; não cria `blockReason` novo |

`reasonCode` é estável e destinado a máquinas; `message` e `context` carregam a
explicação humana e os dados estruturados (`operationId`, `journalFileId`, fase,
`logicalStage`, `journalDurability` e, quando já existente, `blockReason`).
`reasonCode` e `blockReason` são namespaces distintos: uma coincidência textual entre
eles nunca autoriza copiar um diagnóstico efêmero para o envelope persistido.
O evento `NoteStageFailed` usa `reasonCode` no namespace estável `stage.*`; o identificador
da etapa é contrato de máquina, enquanto `detail` permanece explicação humana. A F3 pode
mapear o evento para `blockReason=StageFailed` somente pelas condições da tabela abaixo,
nunca pela coincidência textual entre os dois namespaces.

O mapeamento normativo de `blockReason` persistido é fechado por cenário:

| Valor | Cenário que o produz |
|---|---|
| `OutcomeUnknown` | persistência de negócio ou releitura física sem determinação segura, quando não houver motivo mais específico nesta tabela |
| `InventoryInsufficient` | metadata importada ou legado sem inventário completo, inequívoco e validável para `Remove` |
| `IdentityAmbiguous` | zero ou múltiplos candidatos quando a operação exige uma identidade única e a ausência não puder ser classificada como `TargetAbsentBeforeDelete` |
| `IdentityDivergent` | identidade, posse, GUID, `FileId` ou hash observado diverge do plano ou do diário |
| `UnreconciledNotAttempted` | item `NotAttempted` com estado físico desconhecido ou continuação sem reconciliação determinística |
| `TargetAbsentBeforeDelete` | item ausente antes do primeiro `Delete()`, sem recibo durável anterior `Confirmed` para a mesma identidade |
| `StageFailed` | falha conhecida e não retryable comunicada pelo orquestrador por `NoteStageFailed` depois de existir um envelope `Active` confirmado, antes de uma nova chamada física; somente a F3 confirma o snapshot `Partial` com esse `blockReason`; sem `Active` confirmado, permanece diagnóstico; não cria `PersistenceReceipt` |
| `RetryBudgetExhausted` | o limite `maxPasses` foi atingido enquanto permanecem itens `StillPresentAfterDelete` pendentes |

Quando houver mais de uma descrição possível, a decisão segue esta ordem: (1) falta
de diário ou de durabilidade confirmável produz somente `GateDiagnostic`; (2) uma
identidade única divergente produz `IdentityDivergent`, enquanto zero ou múltiplos
candidatos produzem `IdentityAmbiguous`; (3) ausência antes do primeiro `Delete()`
produz `TargetAbsentBeforeDelete`, e estado desconhecido ou continuação sem
reconciliação produz `UnreconciledNotAttempted`; (4) somente então a incerteza
física ou de persistência cai em `OutcomeUnknown`; (5) falha conhecida de etapa
depois de um envelope confirmado usa `StageFailed`. A tentativa de iniciar nova
operação enquanto o diário corrente está em estado não terminal é bloqueada pelo
gate antes de criar um novo envelope e gera apenas `GateDiagnostic=GateBlocked`.

A classificação do recibo/da releitura em §41 é anterior a esse mapeamento. Assim,
`OutcomeUnknown` no recibo não impede uma razão mais específica no envelope quando
a observação também prova identidade ambígua, ausência prévia ou divergência; sem
essa prova adicional, permanece `blockReason=OutcomeUnknown`. `IdentityDivergent`
é reservado para uma identidade única que não corresponde ao plano/diário, não para
a simples impossibilidade de localizar um alvo.

`journalFileId` não é um campo JSON: é a vinculação externa entre o envelope e o
`WikiFileKBObject.Id`. O runtime deve guardá-lo após a criação e conferir o mesmo ID em
cada releitura; o `FileId` dos alvos, quando aplicável, permanece no inventário e nos
recibos conforme a tabela acima.

Cada item de `inventory` tem `objectType` (`Transaction`, `Folder`, `ApiObject`,
`Procedure`, `Sdt` ou `MetadataFile`), `identityKind` (`Guid`, `FileId`, `Composite`,
`Folder` ou `None`), `guid` GUID anulável, `fileId` inteiro positivo anulável, `name`
não vazio para exibição, `ownershipValidated` booleano obrigatório, `action`
(`Delete` ou `Preserve`), `physicalState` (`Present`, `Absent` ou `Unknown`),
`confirmation` (`NotAttempted`, `Confirmed`, `Absent`, `Divergent` ou
`Unreadable`), `expectedHash` SHA-256 hexadecimal anulável e `receiptSequences`
array de inteiros. As combinações são fechadas: `Guid` exige `guid`; `FileId`
exige `fileId` e `expectedHash`; `Composite` exige a identidade histórica
completa registrada no item; `Folder` exige posse própria e `emptyConfirmed=true`;
`None` só é permitido para um item `Preserve`.

Cada `receipt` tem `sequence` inteiro positivo, único entre os receipts duravelmente
persistidos do mesmo `OperationId` (uma reserva perdida antes do checkpoint confirmado
pode ser reutilizada após crash), `operation` (`Save` ou `Delete`), `stage` não vazio,
`objectType`, `attempt` inteiro positivo,
`retryOfSequence` inteiro anulável, `attemptState` (`Started`, `Finished` ou
`Interrupted`), `result` (`Confirmed`, `Failed` ou `OutcomeUnknown`),
`confirmation`, `physicalState`, `retryEligible` booleano e
`retryableReason` anulável. `retryEligible=true` exige `operation=Delete`,
`result=Failed`, `physicalState=Present` e
`retryableReason=StillPresentAfterDelete`; qualquer outro caso é inválido.

Para `Apply` e `Sync`, `plan` exige `plannedApiGuid`, `contractHash`, as flags
`generateApiObject`, `generateSdts`, `generateProcedures` e `generateMetadata`,
além da lista de serviços. Para `Remove`, `plan` exige o inventário completo e
`plannedApiGuid` quando houver API; `contractHash` pode ser nulo somente para
metadata legada importada. Para `Recovery` autônomo de B115, `plan` usa o tipo
`MetadataRecovery`, `intentKind=Imported` e não autoriza Apply ou Sync. Uma
recuperação que continua Apply, Sync ou Remove conserva `operationKind`,
`operationId`, `applicationId` e `plan` do envelope existente; `Recovery` não
substitui silenciosamente a operação original.

O abandono de um envelope `Prepared/Pending` grava `abandonment` com `reason`,
`authorizedUtc` e `authorizedBy`, muda `logicalStage` para `Abandoned`, mantém
`operationState=Completed` e exige `journalDurability=Confirmed`. Isso é uma
disposição terminal sem gravação de negócio; `Active`, `Partial` e
`OutcomeUnknown` não podem usar esse caminho.

A relação entre a metadata de negócio e o diário também fica fechada:

- V1 e V2 (`GOAB_API_METADATA_B060_V1` e `..._V2`) são somente entradas de
  leitura. V1 normaliza `levels` ausente como plano e pode derivar
  `objects.sdts.own` apenas pela rotina de inventário já validada; não se inventa
  contrato ausente.
- Apply, Sync e B115 gravam V3 (`GOAB_API_METADATA_B060_V3`). A forma completa
  contém `schemaVersion`, `generator`, `generatedAtUtc`, `ownership`, `api`,
  `objects`, `services`, `transactionStructure`, `levels`, `fields`,
  `pagination`, `order`, `security`, `errorDetail`, `descriptions`, `integrity`,
  `classification`, `businessComponent`, `engine`, `scope`, `fingerprint` e
  `ownership.applicationId`. O fingerprint cobre esses dados, exceto o próprio
  campo `fingerprint`, usando UTF-8 e SHA-256.
- A promoção V2→V3 é aditiva: `ownership.applicationId` passa a integrar o payload e o
  fingerprint, portanto um valor novo muda o fingerprint por definição. V2 pode ser lida
  e normalizada, mas não é equivalente a V3 nem deve ser regravada silenciosamente como
  se a integridade permanecesse igual. Antes de qualquer writer emitir V3, devem ser
  atualizados em conjunto os consumidores de versão e ownership:
  `ApiPlanMetadataFileWriter`, `ApiPlanGeneratedApiRemovalPlan`,
  `ApiPlanGenerationStateReader`, `ApiPlanApiObjectOwnership` e
  `ApiPlanApiObjectWriter`. Enquanto isso não ocorrer, `ownership.applicationId` é uma
  pré-condição de implementação da F3, não uma capacidade já disponível no `Src/`.
- A forma V3 importada de B115 mantém `recovery.imported=true` e a lista fechada
  `notRecovered=[fields,pagination,order,services,levels,transactionStructure]`;
  esses seis caminhos ficam ausentes, não vazios. Ela exige, em contrapartida,
  inventário completo de remoção, posse e identidades confirmadas. Só Remove é
  liberado; Sync exige a forma completa.
- Metadata legada não é regravada durante Remove apenas para preencher
  `applicationId`. A adoção tardia é registrada no diário. A gravação V3 ocorre
  quando Apply, Sync ou B115 já estiverem autorizados.

### 25. Campos de identidade e correlação do diário

Decisão aprovada:

O grupo obrigatório de identidade e correlação conterá:

- `schemaVersion` do diário (distinto do `schemaVersion` da metadata);
- `journalKind`;
- `knowledgeBaseGuid`;
- `transactionGuid`;
- `transactionName`;
- `operationId`;
- `applicationId`;
- `operationKind`, com `Apply`, `Sync`, `Remove` ou `Recovery`;
- `generatorVersion`;
- `createdUtc`;
- `updatedUtc`.

Regras do grupo:

- `transactionGuid` é a identidade autoritativa;
- `transactionName` serve para exibição e validação;
- `applicationId` é um GUID novo para cada tentativa de operação;
- `applicationId` não é o `operationId` dos serviços OpenAPI;
- `knowledgeBaseGuid` participa da validação contra diário copiado ou de outra
  KB;
- `operationKind` identifica o fluxo que produziu o diário.

No comando explícito de recuperação, `operationKind` continua identificando a operação
que está sendo continuada (`Apply`, `Sync` ou `Remove`); o fato de a execução ter sido
iniciada pelo comando `Recovery` é contexto do executor e do relatório. O valor
`Recovery` fica reservado para uma operação de recuperação que não esteja continuando
um envelope de negócio existente. Em particular, a recuperação de um `Remove` continua
seguindo o protocolo de passadas de `Remove` e termina em `Removed`, `Partial` ou
`OutcomeUnknown`, nunca em um `Completed` genérico.

### 26. Dimensões separadas de estado

Decisão aprovada:

O JSON manterá separadas estas quatro dimensões:

- `operationState`: resultado global da operação;
- `logicalStage`: ponto exato do pipeline em que a operação está;
- `journalDurability`: confirmação ou indeterminação da última atualização
  do próprio diário;
- `intentKind`: `Current` para intenção criada pela operação atual ou
  `Imported` para intenção reconstruída de metadata órfã.

Regras semânticas:

- `Partial` descreve operação de negócio incompleta;
- `journalDurability=Unknown` descreve incerteza sobre a persistência do
  próprio diário;
- `logicalStage` orienta a recuperação;
- `intentKind=Imported` descreve a origem da intenção e não significa que a
  operação esteja parcial.

### 27. Estados operacionais e bloqueio da KB

Decisão aprovada:

| `operationState` | Significado | Bloqueia a KB |
|---|---|---|
| `Pending` | intenção registrada, operação ainda não iniciada | sim |
| `Running` | operação em andamento | sim |
| `Partial` | parte da operação confirmada | sim |
| `OutcomeUnknown` | gravação sem confirmação conclusiva | sim |
| `Completed` | Apply, Sync ou Recovery concluído | não |
| `Removed` | Remove concluído | não |

`GateBlocked` não será um `operationState` nem um `logicalStage` persistido; será
apenas um `GateDiagnostic` do relatório. Falha de gate antes de qualquer mutação
não criará uma nova operação recuperável nem um bloqueio operacional. Uma operação
anterior, se existir, continuará governando a KB até atingir estado terminal.

Para evitar que estados do envelope, estágios e resultados de recibo sejam
misturados, vale também a seguinte separação canônica:

- `operationState` usa exclusivamente a tabela acima. `Pending`, `Running`,
  `Partial` e `OutcomeUnknown` são não terminais e bloqueiam; `Completed` e
  `Removed` são terminais e não bloqueiam quando `journalDurability=Confirmed`;
- `Prepared` e `Active` são fases do envelope do diário, não valores de
  `operationState`. Um envelope `Prepared` ainda representa uma operação
  `Pending`; um envelope `Active` representa uma operação `Running` ou
  `Partial`, conforme os recibos já confirmados;
- `RemovalPartial`, `ApiSaveOutcomeUnknown` e demais pontos do pipeline são
  `logicalStage`, não estados globais;
- `Failed` pertence ao `outcome` de um recibo ou de uma falha de etapa
  registrada pelo orquestrador. Não será usado como `operationState`.

### 28. Confirmação da durabilidade do próprio diário

Decisão aprovada:

- `journalDurability=Confirmed` somente após `Save()` e releitura pelo
  `FileId`, confirmando identidade, sequência e conteúdo esperados;
- `journalDurability=Unknown` quando a releitura falhar, for ambígua ou não
  permitir determinar qual versão do diário está persistida;
- essa incerteza bloqueará novas operações;
- a extensão não tentará gravar `Unknown` por meio de uma segunda gravação
  incerta;
- o último snapshot confirmado será preservado;
- a recuperação comparará snapshot, recibos e estado físico da KB antes de
  continuar;
- comparação inconclusiva manterá a recuperação bloqueada.

### 29. Formato mínimo dos recibos

Decisão aprovada:

Cada `PersistenceReceipt` conterá:

- `sequence` e `attempt`; `sequence` é atribuída em memória antes do delegate para permitir
  `TryGetReceipt`, mas só se torna compromisso durável quando o checkpoint que a contém é
  confirmado. Entre receipts duravelmente persistidos, é global, positivo, monotônico e
  append-only dentro da operação identificada pelo mesmo `OperationId`, começando em `1`. A
  unicidade exigida é a dos valores já presentes em receipts duravelmente persistidos:
  um número reservado em memória e perdido antes do checkpoint confirmado pode ser
  reutilizado após crash; nenhum número presente em snapshot confirmado pode ser
  reutilizado. `attempt` começa
  em `1` para a primeira tentativa física daquele alvo dentro da operação e só é
  incrementado quando o mesmo alvo é reencaminhado na mesma operação. A exceção é
  `RecordNotAttempted`, que usa `attempt=1` sem tentativa física e sem consumir
  `maxPasses`;
- `retryOfSequence`, quando houver retry, aponta para o recibo imediatamente
  anterior da mesma cadeia de tentativas; a cadeia não pode apontar para outro
  envelope. `inventory.receiptSequences` só pode apontar para receipts presentes no
  mesmo snapshot confirmado, e `retryOfSequence` só pode apontar para receipt confirmado
  anterior da mesma cadeia;
- `operation`, com `Save` ou `Delete`;
- `objectType`, com `ApiObject`, `Procedure`, `Sdt`, `MetadataFile`, `Folder` ou
  `Transaction`;
- `stage` não vazio;
- `PersistenceIdentity` planejada e identidade persistida observada;
- `startedUtc`, `endedUtc` e `durationMs`;
- `attemptState`, com `Started`, `Finished` ou `Interrupted`;
- `result`, com `Confirmed`, `Failed` ou `OutcomeUnknown`;
- `physicalState`, com `Present`, `Absent` ou `Unknown`;
- `retryEligible`, booleano que só pode ser verdadeiro para falha retryable de
  `Delete`;
- `retryableReason`, enum fechado, inicialmente `StillPresentAfterDelete`, ou ausente quando
  `retryEligible` for falso;
- `confirmation`, com `NotAttempted`, `Confirmed`, `Absent`, `Divergent` ou
  `Unreadable`;
- resumo de exceção ou cancelamento, quando aplicável.

Regras do recibo:

- cada tentativa de `Delete` terá recibo próprio;
- a continuação do mesmo envelope e do mesmo `OperationId` lê o maior `sequence` e o maior
  `attempt` confirmados no último snapshot durável, usa os próximos valores e acrescenta
  os novos receipts ao mesmo array; se houve crash antes desse checkpoint, uma reserva
  perdida pode ser reutilizada. Uma operação nova, depois de `Completed` ou `Removed`,
  começa `sequence` e `attempt` em `1`;
- cada item de `inventory.receiptSequences` recebe os `sequence` correspondentes
  em ordem de registro, sem substituir nem remover referências anteriores;
- retry não transforma recibo anterior em sucesso;
- `OutcomeUnknown` nunca autoriza retry posterior;
- somente `Delete` pode produzir `retryEligible=true`, e isso exige
  `retryableReason=StillPresentAfterDelete`;
- `Save`, falha de preparação e `OutcomeUnknown` sempre produzem
  `retryEligible=false`;
- um alvo que se tornou ausente antes do primeiro `Delete()` não é uma falha física de
  `Delete`: o remover usa o `RecordNotAttempted` da F2 para registrar um receipt
  `Finished` com `attempt=1`, `result=OutcomeUnknown`, `confirmation=NotAttempted`,
  `retryEligible=false` e o próximo `sequence` durável; a F3 só persiste o checkpoint e
  o bloqueio. Não há chamada física, consumo de `maxPasses` nem retry implícito. Se não
  houver receipt anterior `Confirmed`, o envelope usa
  `blockReason=TargetAbsentBeforeDelete` quando esse snapshot puder ser confirmado;
- `Absent` pode confirmar `Delete`, mas não `Save`;
- `Divergent` ou `Unreadable` produz `OutcomeUnknown`.

### 30. Inventário por objeto

Decisão aprovada:

Cada objeto planejado no diário conterá:

- `objectType`, com `ApiObject`, `Procedure`, `Sdt`, `MetadataFile`, `Folder` ou
  `Transaction`;
- `role`, como `MainApi`, `Get`, `Create`, `Update`, `Delete`, `OwnSdt` ou
  `SharedSdt`;
- `identityKind`, `guid`, `fileId`, `expectedHash` e `name`, conforme a
  `PersistenceIdentity` fechada na decisão 24;
- `ownershipValidated`;
- `action`, com `Delete` ou `Preserve` para o inventário de remoção;
- `physicalState`, com `Present`, `Absent` ou `Unknown`;
- `confirmation`, com `NotAttempted`, `Confirmed`, `Absent`, `Divergent` ou
  `Unreadable`;
- `receiptSequences` relacionados.

Regras do inventário:

- nome sozinho nunca identifica posse;
- SDT compartilhado nunca será alvo de remoção;
- `ownershipValidated=false`, identidade ambígua ou `Unknown` bloqueará a etapa
  correspondente;
- objeto novo poderá começar sem identidade persistida, mas deverá receber a
  identidade confirmada no recibo após criação;
- o inventário servirá para Apply, Sync, Recovery e Remove.

### 31. Estágios lógicos do pipeline

Decisão aprovada:

Os valores de `logicalStage` serão:

- `NotStarted`;
- `IntentionRecorded`;
- `TransactionPending`;
- `FolderPending`;
- `SdtsPending`;
- `ProceduresPending`;
- `ApiPending`;
- `ApiSaveOutcomeUnknown`;
- `ApiPhysicallySaved`;
- `MetadataPending`;
- `MetadataRecovered`;
- `RemovalInProgress`;
- `RemovalPartial`;
- `RecoveryInProgress`;
- `Abandoned`;
- `Completed`;
- `Removed`.

`GateBlocked` fica fora deste enum: é o diagnóstico efêmero produzido quando o gate
impede a primeira gravação.

Regras de estágio:

- um estágio posterior a uma tentativa de negócio só avançará depois do recibo ou
  checkpoint de diário correspondente confirmado;
- `ApiSaveOutcomeUnknown` exigirá reconciliação por identidade;
- `RemovalPartial` preservará alvos e recibos já processados;
- `RecoveryInProgress` será usado somente enquanto uma recuperação explícita reidrata,
  aguarda autorização humana ou confirma a transição para a próxima etapa de um envelope
  existente; depois do checkpoint seguro, o diário volta ao estágio original da operação
  (`RemovalInProgress`, `RemovalPartial` ou o próximo estágio de `Apply`/`Sync`);
- a recuperação usará o estágio para continuar somente a próxima etapa segura;
- `Completed` e `Removed` deverão coincidir com os estados globais homônimos;
- o estágio nunca autorizará sozinho uma ação sem suporte do inventário e dos
  recibos.

### 32. Contrato de relatório final e Output

Decisão aprovada:

Todos os caminhos — sucesso, falha, cancelamento, bloqueio e recuperação —
apresentarão o mesmo resumo mínimo:

- `operationState`;
- `journalDurability`;
- `logicalStage`;
- `operationKind`;
- `applicationId`;
- Transaction e `transactionGuid`;
- quantidades de recibos `Confirmed`, `Failed` e `OutcomeUnknown`;
- objetos confirmados, ausentes, divergentes ou desconhecidos;
- motivo do bloqueio, quando houver;
- próxima ação explícita.

Regras de apresentação:

- sem GUID validado, não haverá link para API Object;
- `OutcomeUnknown` aparecerá como indeterminado, nunca como sucesso;
- `Recovery` só informará KB liberada depois de o diário estar em estado
  terminal confirmado;
- `ShowFinalReport` receberá a trilha de recibos também em erro,
  cancelamento e bloqueio;
- o relatório não criará recibos nem alterará decisões.

### 33. Registro triplo do comando de recuperação

Decisão aprovada:

O comando de recuperação deverá existir simultaneamente em:

1. `Package.cs`, por `AddCommand(new CommandKey(...))`;
2. `GenexusOpenApiBuilder.package`, como `CommandDefinition`;
3. `Groups`, com o mesmo `refid`, no grupo do menu contextual da Transaction e no grupo
   do menu principal.

Regras do comando:

- o comando segue a localização vigente: cada variante pt-BR, espanhol e inglês tem
  um ID literal próprio, e esse mesmo ID da variante deve aparecer em `Package.cs`, no
  `CommandDefinition` e como `refid` nos dois grupos correspondentes; não se deve
  presumir que as três strings localizadas sejam um único ID;
- haverá registro no menu contextual da Transaction e no menu principal, com o
  `refid` da mesma variante em cada grupo correspondente;
- no menu contextual, o comando usará a Transaction selecionada e bloqueará se
  o diário pertencer a outra Transaction;
- no menu principal, o comando operará sobre a KB atual sem exigir Transaction
  selecionada e funcionará como fallback para a KB inteira;
- `Tools/Test-ExtensionCommandRegistration.ps1` deverá passar;
- build bem-sucedido não substituirá a verificação de sincronização das três
  camadas.

Os literais da primeira implementação ficam fixos nesta matriz. Como o runtime usa o
literal localizado no `CommandKey`, cada linha é um ID completo, não apenas um rótulo:

| Variante | ID literal / `CommandKey` | label mostrado | `CommandDefinition` | `refid` no grupo contextual | `refid` no grupo principal |
|---|---|---|---|---|---|
| pt-BR | `Recuperar operação do Open API Builder` | o mesmo literal | o mesmo literal | o mesmo literal | o mesmo literal |
| es | `Recuperar operación de Open API Builder` | o mesmo literal | o mesmo literal | o mesmo literal | o mesmo literal |
| en | `Recover Open API Builder operation` | o mesmo literal | o mesmo literal | o mesmo literal | o mesmo literal |

Não haverá um ID neutro compartilhado nem tradução em tempo de execução para substituir
essa matriz. O teste deve conferir as doze ocorrências esperadas: cada literal em
`Package.cs`, no `CommandDefinition` e nos dois `Groups`.

### 34. Matriz obrigatória de validação da F3

Decisão aprovada:

As provas de encerramento da F3 serão separadas em três níveis.

**Offline:**

- schema válido e versão desconhecida;
- transições de estado;
- recibos de `Save` e `Delete`;
- retry limitado de `Delete`;
- `OutcomeUnknown`;
- colisões e inventário insuficiente;
- política `includeBusinessComponentParameters`.

**Integração textual e mecânica:**

- todos os `Save()` passam pelo seam;
- todos os `Delete()` do remover passam pelo seam;
- nenhum ponto é instrumentado duas vezes;
- o executor preserva progresso, cancelamento e exceções;
- o relatório recebe recibos em todos os caminhos;
- o comando está registrado nas três camadas.

**IDE e fluxo real:**

- Apply, Sync e Remove normais;
- interrupção após cada checkpoint;
- falha de `Delete` com retry limitado;
- `OutcomeUnknown`;
- diário inválido ou divergente;
- metadata importada suficiente e insuficiente;
- recuperação explícita;
- nova operação após a recuperação, comprovando que a KB foi liberada.

### 35. Fallback por nome em todos os fluxos gerenciados

Decisão aprovada:

- `Apply`, `Sync`, `Remove` e `Recovery` gerenciados pela S-B111 somente
  poderão associar o API Object por GUID validado;
- nenhum desses fluxos poderá usar nome para definir `MainObjectGuid`;
- busca por nome poderá permanecer apenas como diagnóstico de candidatos e
  ambiguidade;
- a regra de controle não será uma exclusão por string de operação, como
  `Operation == "Remover"`;
- fluxos legados fora do contrato da S-B111 não poderão contaminar o resultado
  dos fluxos gerenciados.

### 36. Referências antigas ao diário baseado em hash

Decisão aprovada:

- o plano vigente da F3 será atualizado para o File fixo por KB;
- referências ao esquema `B111_J_<hash>` em sondas e manuscritos históricos
  serão preservadas como evidência;
- esses documentos receberão marca explícita de que o esquema baseado em hash
  foi superado pela decisão de 2026-09-07;
- nenhum documento histórico poderá ser interpretado como contrato operacional
  vigente.

### 37. Consolidação documental do Modo A

Decisão aprovada:

- a F3 e o checkpoint operacional deixam de apresentar a escolha Modo A/Modo B
  como pendente;
- o Modo A é o contrato selecionado em 2026-09-07: um diário `File` único por
  KB, com nome lógico fixo `GxOpenApiBuilder_OperationJournal`, sem módulo;
- a recuperação é explícita, fica na F3, aparece primariamente no menu de contexto da
  Transaction e tem fallback no menu principal para atuar sobre a KB inteira;
- o Modo B permanece apenas como comparação histórica;
- a dependência ativa da F3 é F2 mais as decisões consolidadas, e não uma nova
  escolha entre modos.

### 38. Ponto comum de persistência da F2

Decisão aprovada após o primeiro parecer da revisão por pares:

- `ApiPlanSaveBoundaryProbe`, já existente no repositório, será o único ponto comum de
  persistência da F2;
- a F2 não criará um `ApiPlanPersistenceProbe` paralelo. A classe existente preservará
  os eventos atuais de Pump/Save e absorverá os recibos de `Save` e `Delete`;
- todos os `Save()` de produção gerenciados pela sprint e todos os `Delete()` físicos do
  remover passarão por esse ponto comum, sem instrumentação dupla;
- a confirmação será obrigatória em cada ponto: releitura coerente para `Save()` e
  ausência confirmada pelo `Guid` para `Delete()`;
- não haverá sobrecarga de persistência sem confirmação. Exceção, cancelamento,
  confirmação ilegível ou divergente resultam em `OutcomeUnknown`;
- o executor unificado de `saveSteps` será apenas executor: preservará progresso,
  cancelamento, exceções, snapshots, labels e eventos, sem se tornar um segundo seam;
- retry e decisões de recuperação continuam fora do ponto comum e pertencem à F3.

### 39. Recuperação B115 dentro do escopo da S-B111

Decisão aprovada após os pareceres nativos e DeepSeek da v3:

- a recuperação explícita de metadata B115 faz parte do fluxo gerenciado da S-B111;
- o `file.Save()` de `ApiPlanOrphanMetadataRecovery` será incluído no inventário do
  ponto comum da F2;
- esse `Save()` terá recibo e confirmação obrigatória da identidade persistida do File e
  dos bytes esperados;
- confirmação baseada apenas em reenumeração por nome não atende ao contrato de
  identidade da sprint;
- a contagem documental distinguirá as 15 chamadas físicas de `Save()` dos dois laços
  executores de BC/List, que não são Saves adicionais;
- o teste da F2 cobrirá a recuperação com File novo e com File reutilizado, além das
  falhas e resultados indeterminados desse `Save()`.

### 40. Adoção tardia de `ApplicationId` para metadata legada

Decisão aprovada após a discussão do gap de compatibilidade:

- `ApplicationId` continua sendo um GUID novo para cada tentativa de operação S-B111;
- metadata nova, gravada pela S-B111, deve conter esse campo e mantê-lo compatível com
  o diário;
- metadata legada sem `ApplicationId` não será automaticamente classificada como
  `InventoryInsufficient` quando os demais requisitos de identidade, posse, unicidade e
  inventário forem suficientes;
- ao iniciar a primeira operação S-B111 sobre essa metadata, a extensão gerará um novo
  `ApplicationId` para a tentativa atual e o registrará no diário como adoção tardia;
- durante `Remove`, a metadata legada não será regravada apenas para preencher o campo;
- a ausência de `ApplicationId` não dispensa as demais validações nem autoriza
  identificação por nome.

### 41. Identidade histórica de Procedures e SDTs no B115

Decisão aprovada para o inventário de metadata legada:

- o API Object exige `apiGuid` confirmado e compatível com a Transaction e a metadata;
- Procedures e SDTs legados podem ser validados sem GUID persistido quando a identidade
  histórica composta for completa: nome exato, tipo, papel, `Description` canônica
  vinculada à Transaction/API, unicidade na KB e ausência de conflito;
- nome, `Description` ou prefixo isolados nunca autorizam `Remove`;
- qualquer candidato ambíguo, vínculo incompatível, campo essencial ausente ou
  inventário incompleto resulta em `InventoryInsufficient` e bloqueia;
- essa exceção de identidade histórica não autoriza `Sync` nem substitui o GUID exigido
  para o API Object.

Para a confirmação física do `Delete`, a identidade de cada tipo é igualmente fechada:

| Tipo | Identidade para localizar e reler | Se não houver exatamente um alvo |
|---|---|---|
| API Object | `PlannedApiGuid`, Transaction GUID e posse | `OutcomeUnknown`, sem retry |
| Procedure / SDT legado | nome exato + tipo + papel + `Description` canônica vinculada + unicidade | `OutcomeUnknown`, sem retry |
| Metadata File | `FileId` + nome canônico + hash esperado | `OutcomeUnknown`, sem retry |
| Folder | nome + posse própria + vazio confirmado | `OutcomeUnknown`, sem retry |
| Transaction, diário, preferências, SDT compartilhado | não são alvo destrutivo | `Preserve`; nunca chamar `Delete()` |

O nome exibido ou o `Guid` planejado isolado não pode ser usado como fallback. A mesma
identidade validada precisa aparecer no inventário, no recibo F2 e na releitura pós-
`Delete`; divergência, leitura ilegível ou múltiplos candidatos produzem
`OutcomeUnknown` e não podem ser reexecutados automaticamente.

### 42. Distinção entre falha retryable e `OutcomeUnknown` no `Delete`

Decisão aprovada durante a consolidação do gap de retry:

- a F2 deve registrar a falha conhecida de dependência como `Failed` retryable quando
  o `Delete()` lançar ou retornar sem remover o objeto e a releitura imediata, por
  identidade validada, provar que ele ainda existe;
- a F3 poderá recolocar somente esse caso na fila da passada seguinte da mesma
  operação de `Remove`;
- ausência comprovada após a tentativa é `Confirmed`, não é item para requeue;
- releitura ilegível, divergente ou ambígua é `OutcomeUnknown`, bloqueia a operação e
  não pode ser repetida automaticamente nem por recuperação posterior;
- essa distinção não altera o contrato de `Save()`: lançamento ou cancelamento de
  `Save()` continua `OutcomeUnknown` até existir confirmação suficiente.

### 43. Canal de sinalização do `Delete` retryable

Decisão aprovada durante a consolidação do gap de retry:

- o seam da F2 deve devolver ou expor ao chamador o recibo final de cada tentativa,
  inclusive quando relança a exceção original do `Delete()`;
- a F3 não interpretará mensagem de exceção nem dependerá de exceção artificial para
  reconhecer a falha retryable;
- o executor de `Remove` será o único consumidor que transforma um recibo `Failed`
  retryable de ordem de dependência em item da passada seguinte;
- `Confirmed` remove o item da fila e `OutcomeUnknown` bloqueia, sem requeue;
- a tentativa receberá uma sequência monotônica antes do delegate; o seam registrará o
  recibo final no log por essa sequência antes de relançar a exceção, e o executor poderá
  recuperá-lo pelo log sem interpretar texto de exceção;
- esse canal deverá ser exercitável no teste offline do núcleo da F2 e na validação IDE do
  adaptador GeneXus.

O contrato de injeção também fica fechado para que “falha em cada Save” seja executável:

- os pontos são `TransactionSave`, `FolderSave`, `SdtSave`, `ProcedureSave`, `ApiSave`,
  `MetadataSave`, `B115MetadataSave`, `BusinessComponentEnablementSave`, `ApiDelete`,
  `ProcedureDelete`, `SdtDelete`, `MetadataDelete` e `FolderDelete`;
- `IApiPlanPersistenceFaultInjector.Before(point, attempt)` injeta falha antes do
  delegate e `After(point, attempt)` injeta o resultado posterior; as ações fechadas são
  `None`, `Throw`, `Cancel`, `ReturnWithoutMutation`, `DivergentConfirmation` e
  `UnreadableConfirmation`;
- o núcleo SDK-free recebe o injetor por dependência de teste; o adaptador recebe um hook
  interno exclusivo da assembly de testes; a produção usa o injetor nulo e não há ativação
  por arquivo, variável de ambiente ou preferência da KB;
- cada escopo instala o injetor por `IDisposable`, restaura o injetor nulo no `Dispose` e
  falha se um ponto esperado não for visitado. O hook não interpreta texto de exceção e
  não permanece ativo entre testes.

## Itens ainda não implementados no código

As decisões acima já foram consolidadas documentalmente nos planos F1, F2 e F3 e no
checkpoint operacional. Ainda não foram alterados código, manifesto ou testes; a revisão
por pares da sprint permanece em aberto.

O contrato documental dos pontos criticados está fechado nesta rodada. O que permanece é
implementação e evidência, não uma lacuna de decisão:

- serializer/validador do schema V1 do diário e normalização da metadata V1/V2→V3;
- implementação dos serviços de leitura, reidratação, continuação e relatório;
- sincronização dos doze literais do novo comando nas camadas de runtime e manifesto;
- implementação do injetor determinístico e dos pontos de falha fechados;
- testes offline, testes de contrato e validação funcional na IDE;
- eventuais ajustes adicionais de referências operacionais identificados durante a revisão
  por pares ou a implementação.

## Consolidação adicional das decisões posteriores

Este bloco foi acrescentado depois do registro inicial, para preservar as decisões tomadas
na continuação da entrevista de gaps. Ele não autoriza implementação, build, instalação,
commit ou push.

### 44. Journal sem histórico e reutilização segura

- o journal contém somente a operação corrente: `OperationId`, `ApplicationId`, estado,
  receipts e dados necessários para recuperação;
- não haverá lista de operações anteriores, ApplicationIds antigos, reaplicações ou
  receipts acumulados;
- o estado terminal pode permanecer até a próxima operação, quando o mesmo File será
  reutilizado e substituído;
- somente `Completed` ou `Removed`, com `journalDurability=Confirmed`, permitem iniciar
  outra operação;
- `Pending`, `Running`, `Partial` e `OutcomeUnknown` bloqueiam nova operação até
  decisão explícita; `Prepared` e `Active` qualificam o envelope e não substituem
  esses estados;
- `RemovalPartial` qualifica o `logicalStage`, e `Failed` permanece restrito a
  recibos ou falhas de etapa.

### 45. Preparação em duas fases

- a substituição começa gravando o envelope corrente como `Prepared`;
- a extensão relê o File pelo `FileId` e confirma identidade, conteúdo e hash;
- depois grava `Active` e confirma novamente;
- nenhum objeto de negócio é gravado antes dessas confirmações;
- se `Prepared` ficar interrompido, o humano pode continuar com o mesmo `OperationId`
  e `ApplicationId` enquanto a operação permanecer `Pending`, ou abandonar
  explicitamente;
- se uma operação `Active` for interrompida, o diagnóstico é somente leitura; só depois
  de confirmação humana pode ser continuada com `Running`/`Partial`, conforme os
  recibos. Se não houver continuação determinística e segura, ela permanece bloqueada:
  a recuperação não pode abandoná-la, limpá-la, marcá-la como terminal ou criar novos
  IDs silenciosamente. A correção externa e a disposição escolhida pelo humano devem
  ser explicitamente registradas antes de qualquer nova operação.

### 46. Falha na finalização

Se os objetos de negócio forem gravados, mas a gravação ou releitura final do journal
falhar, o resultado será `OutcomeUnknown`. A recuperação poderá confirmar o estado
terminal somente depois de conferir os objetos; não haverá repetição automática de toda a
operação.

### 47. Retry e motivo baseado em evidência

O nome aprovado para o motivo retryable é `StillPresentAfterDelete`; o motivo não infere
a causa. A fila cobre todos os tipos removíveis, não apenas SDTs; cada passada relê os
alvos, recoloca somente objetos comprovadamente ainda presentes
e termina quando uma passada inteira não apagar nada. `OutcomeUnknown` interrompe sem
requeue.

O orçamento é `maxPasses = max(1, número de itens Delete do inventário)`. Ao atingir o
limite com itens `StillPresentAfterDelete` pendentes, o estado é `Partial` e
`blockReason=RetryBudgetExhausted`; não há loop infinito nem nova operação automática.
Continuação posterior exige o mesmo envelope, confirmação humana e novo orçamento.
`NotAttempted`,
falha não retryable e alvo ausente antes da primeira tentativa também impedem `Removed`,
salvo recibo durável anterior `Confirmed` para a mesma identidade. Transaction, diário,
preferências, SDTs compartilhados e Folders não próprios nunca entram na fila destrutiva.

### 48. Recuperação e preferências

- a recuperação não será automática durante o Wizard;
- `OfferOrphanMetadataRecoveryIfEnabled` será retirado do fluxo operacional;
- `OfferOrphanMetadataRecovery` será lida apenas como compatibilidade de KB antiga para
  inicializar `ShowRecoveryOptionProactively`;
- a nova preferência controla somente a visibilidade preventiva;
- havendo pendência, inconsistência ou estado desconhecido, o comando aparece sempre;
- a recuperação exige diagnóstico somente leitura e confirmação humana antes da primeira
  gravação.

### 49. Comando explícito

- no menu de contexto da Transaction, o comando usa a Transaction selecionada e bloqueia
  se o journal pertencer a outra;
- no menu principal, ele faz diagnóstico da KB sem depender de Transaction selecionada;
- para cada variante localizada, o mesmo ID deve existir em `Package.cs`,
  `CommandDefinition` e `Groups`, nos dois grupos de menu;
- mensagens novas usam pt-BR, espanhol e inglês e o Output padrão da IDE;
- um estado corrompido ou ambíguo nunca deve ocultar o comando de recuperação.

### 50. Identidade da API

Apply e Sync associam a API principal somente pelo `PlannedApiGuid` validado. Nome serve
para diagnóstico ou descoberta, nunca para autorizar associação. API nova sem GUID estável
ou com `Guid.Empty` bloqueia antes do primeiro write. `DescriptionFallback` fica restrito
ao preflight explícito do B115 com inventário completo.

### 51. Escopo de B109 e B110

B109 e B110 permanecem fora do escopo da S-B111:

- B109 continua sendo a frente das falhas internas ou da instabilidade do SDK durante
  `Save()`; a S-B111 registra o resultado e evita continuar às cegas, mas não promete
  corrigir a causa do SDK;
- B110 continua sendo a frente da degradação em cascata quando a etapa do API é bloqueada
  e consumidores ainda são gravados; a F1 reduz a janela de escrita parcial, mas não
  encerra todo esse problema;
- ambos permanecem registrados como frentes separadas, sem impedir a implementação da
  S-B111.

### 52. Escopo de B112, B113 e B114

B112, B113 e B114 permanecem fora do escopo da S-B111:

- B112 trata o truncamento silencioso de `Description`;
- B113 trata a gravação ocasionalmente muito lenta;
- B114 trata o custo elevado de `WikiFileKBObject`;
- a S-B111 pode medir e respeitar esses comportamentos, inclusive o custo do File no
  journal, mas não promete corrigi-los;
- os três permanecem como frentes separadas de hardening e diagnóstico do SDK.

## Consolidação técnica aprovada após o fechamento das decisões

As decisões funcionais e de escopo estão fechadas. As seguintes diretrizes técnicas foram
aprovadas para orientar a atualização dos planos, sem autorizar ainda implementação, build,
instalação, commit ou push:

- o journal será `GxOpenApiBuilder_OperationJournal`, com envelope da operação corrente e sem
  histórico;
- metadata V1/V2 será lida e normalizada, mas Apply, Sync e B115 gravarão V3;
- API será associada por GUID validado e File por `FileId`, GUID, `FileName` e hash;
- B115 exigirá inventário completo e inequívoco de Procedures e SDTs referenciados;
- F1 deverá preparar consumidores antes do único `API.Save()` final;
- F2 usará o seam único, receipts confirmados e executor de `saveSteps` sem lógica de
  decisão;
- a fila de remoção abrangerá todos os tipos e tratará `StillPresentAfterDelete`,
  `Confirmed` e `OutcomeUnknown` conforme o contrato aprovado;
- o journal será atualizado e confirmado ao longo da operação e no estado terminal;
- recuperação será comandada explicitamente, com diagnóstico somente leitura e confirmação
  humana antes da primeira gravação;
- testes offline, testes do seam e validação posterior na IDE serão obrigatórios;
- os planos e documentos serão consolidados antes da implementação para remover referências
  contraditórias.

### 53. Clarificações após parecer solo do MiMo V2.5 Pro — 2026-09-08

O parecer externo foi tratado como insumo de revisão, não como autoridade para alterar o
repositório. Após conferência cruzada dos planos e deste registro, ficam incorporadas somente
estas clarificações:

- `StageFailed` é sinalizado pelo orquestrador: depois de um envelope `Active` confirmado e
  antes de qualquer nova chamada física, `NoteStageFailed` deve ser processado pela F3,
  que grava o snapshot durável `Partial` + `blockReason=StageFailed`, sem
  `PersistenceReceipt`; sem confirmação do novo snapshot, o resultado é
  `GateDiagnostic=DurabilityUnknown` e não se afirma que a razão foi persistida;
- `GateDiagnostic` mantém seus códigos de alto nível, mas o payload deve identificar a
  precondição que falhou por `reasonCode` estável e contexto estruturado; isso não cria campo
  persistido nem transforma `GateBlocked` em estado da operação;
- `sequence` é monotônica e append-only dentro do mesmo `OperationId`; uma continuação não
  abre outra cadeia de recibos, e `OutcomeUnknown` continua sem autorização de retry;
- a vinculação de `RecoveryAuthorization` a `journalFileId`, `updatedUtc` e hash é uma defesa
  de frescor/integridade contra alteração concorrente entre leitura e ação (TOCTOU), não um
  mecanismo de histórico.

### 54. Clarificações após parecer solo do Codex GPT-5.6-luna — 2026-09-08

O parecer externo foi tratado como insumo de revisão, não como autoridade para alterar o
repositório. Após conferência cruzada dos planos e deste registro, ficam incorporadas somente
estas clarificações:

- F2 emite `NoteStageFailed(OperationId, stage, reasonCode, detail)` como sinal de falha;
  F3 é responsável por persistir o encerramento `Partial` com `blockReason=StageFailed`
  somente quando houver envelope `Active` confirmado. Sem `Active` confirmado, o sinal é
  diagnóstico e não cria `blockReason=StageFailed`;
- `GateDiagnostic` agora tem precedência e taxonomia fechadas: `JournalUnavailable` para
  journal ausente ou não validável, `DurabilityUnknown` para snapshot novo não confirmado,
  `GateBlocked` para estado global bloqueante com journal legível e durável, e
  `PreconditionFailed` para pré-condição específica antes da primeira mutação. O último não
  é subcausa de `GateBlocked`;
- a unicidade de `sequence` é exigida entre receipts duravelmente persistidos no mesmo
  `OperationId`; a sequência é atribuída em memória antes do delegate, mas uma reserva
  perdida antes do checkpoint confirmado pode ser reutilizada após crash. Alvo ausente antes
  de `Delete` passa por `RecordNotAttempted` no log comum, com `operation=Delete`,
  `OutcomeUnknown`, `confirmation=NotAttempted` e sem retry, quando esse checkpoint puder
  ser confirmado;
- `RecoveryAuthorization` usa o SHA-256 do JSON canônico do envelope do journal com
  `schemaVersion=1` — não da metadata V3 — e exige revalidação imediata de
  `journalFileId`, IDs, `updatedUtc`, hash e `NextStep` antes da primeira mutação. Sem CAS do
  SDK, a garantia é otimista; o lock local por KB deve cobrir a revalidação e a primeira
  mutação, e a indisponibilidade do lock bloqueia antes da mutação. Qualquer corrida residual
  é classificada explicitamente como `OutcomeUnknown` ou `DurabilityUnknown`.

### 55. Clarificações após parecer solo do Claude Code Opus 5 — 2026-09-08

O parecer externo foi tratado como insumo de revisão, não como autoridade para alterar o
repositório. O veredito foi `APROVAR COM RESSALVAS`; após conferência no código e nos planos,
foram incorporadas estas clarificações:

- a promoção V2→V3 da metadata é aditiva, mas ainda exige atualizar os consumidores
  `ApiPlanMetadataFileWriter`, `ApiPlanGeneratedApiRemovalPlan`,
  `ApiPlanGenerationStateReader`, `ApiPlanApiObjectOwnership` e
  `ApiPlanApiObjectWriter` antes de qualquer writer emitir V3; `ownership.applicationId`
  ainda não existe no `Src/`, e seu valor entra no fingerprint V3;
- o remover chama `RecordNotAttempted` pelo seam entregue na F2; o receipt usa `attempt=1`,
  não consome `maxPasses`, fica disponível em memória e só é relacionado ao checkpoint
  durável pela F3;
- `NoteStageFailed` usa `reasonCode` no namespace `stage.*`, sem cópia automática para
  `blockReason`; o lock por KB é processo-local e não coordena duas IDEs ou processos;
- a canonização do hash agora fecha `null`, `[]`, GUID `D` minúsculo, timestamps UTC com
  milissegundos fixos, escapes JSON obrigatórios, números invariáveis, booleanos JSON e a
  separação entre o hash semântico do snapshot e o digest dos bytes crus do File.
