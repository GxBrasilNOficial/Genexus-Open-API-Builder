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

- o JSON do diário terá schema versionado, começando por uma versão explícita `V1`;
- versão desconhecida bloqueará a operação e a recuperação;
- não haverá migração destrutiva automática;
- estado da operação, durabilidade, intenção e recibos serão registrados como
  dimensões distintas;
- cada recibo identificará `Save` ou `Delete`, tipo de objeto, identidade
  planejada, identidade persistida, resultado e confirmação;
- mudanças incompatíveis exigirão nova versão explícita do schema.

### 25. Campos de identidade e correlação do diário

Decisão aprovada:

O grupo obrigatório de identidade e correlação conterá:

- `schemaVersion`;
- `journalKind`;
- `knowledgeBaseGuid`;
- `transactionGuid`;
- `transactionName`;
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

`GateBlocked` não será um `operationState`; será apenas um `logicalStage` de
diagnóstico. Falha de gate antes de qualquer mutação não criará uma nova
operação recuperável nem um bloqueio operacional. Uma operação anterior, se
existir, continuará governando a KB até atingir estado terminal.

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

- `sequence` e `attempt`;
- `retryOfSequence`, quando houver retry;
- `action`, com `Save` ou `Delete`;
- `objectType`, com `API`, `Procedure`, `SDT`, `File`, `Folder` ou
  `Transaction`;
- `stage` e `label`;
- `plannedName` e `plannedGuid`;
- `persistedName` e `persistedGuid`, quando confirmados;
- `startedUtc`, `endedUtc` e `durationMs`;
- `outcome`, com `Confirmed`, `Failed` ou `OutcomeUnknown`;
- `retryEligible`, booleano que só pode ser verdadeiro para falha retryable de
  `Delete`;
- `retryableReason`, enum fechado, inicialmente `StillPresentAfterDelete`, ou ausente quando
  `retryEligible` for falso;
- `confirmation`, com `NotAttempted`, `Confirmed`, `Absent`, `Divergent` ou
  `Unreadable`;
- resumo de exceção ou cancelamento, quando aplicável.

Regras do recibo:

- cada tentativa de `Delete` terá recibo próprio;
- retry não transforma recibo anterior em sucesso;
- `OutcomeUnknown` nunca autoriza retry posterior;
- somente `Delete` pode produzir `retryEligible=true`, e isso exige
  `retryableReason=StillPresentAfterDelete`;
- `Save`, falha de preparação e `OutcomeUnknown` sempre produzem
  `retryEligible=false`;
- `Absent` pode confirmar `Delete`, mas não `Save`;
- `Divergent` ou `Unreadable` produz `OutcomeUnknown`.

### 30. Inventário por objeto

Decisão aprovada:

Cada objeto planejado no diário conterá:

- `objectType`, com `API`, `Procedure`, `SDT`, `File`, `Folder` ou
  `Transaction`;
- `role`, como `MainApi`, `Get`, `Create`, `Update`, `Delete`, `OwnSdt` ou
  `SharedSdt`;
- `plannedName`;
- `plannedGuid`, quando já existir;
- `persistedName` e `persistedGuid`, quando confirmados;
- `ownership`, com `Own`, `Shared`, `External` ou `Unknown`;
- `expectedAction`, com `Create`, `Update`, `Delete` ou `Preserve`;
- `physicalState`, com `Absent`, `Confirmed`, `Divergent` ou `Unknown`;
- `receiptSequences` relacionados.

Regras do inventário:

- nome sozinho nunca identifica posse;
- SDT compartilhado nunca será alvo de remoção;
- `Unknown` ou `External` bloqueará a etapa correspondente;
- objeto novo poderá começar sem `plannedGuid`, mas deverá receber identidade
  no recibo após criação;
- o inventário servirá para Apply, Sync, Recovery e Remove.

### 31. Estágios lógicos do pipeline

Decisão aprovada:

Os valores de `logicalStage` serão:

- `NotStarted`;
- `GateBlocked`;
- `IntentionRecorded`;
- `TransactionPending`;
- `FolderPending`;
- `SdtsPending`;
- `ProceduresPending`;
- `ApiPending`;
- `ApiSaveOutcomeUnknown`;
- `ApiPhysicallySaved`;
- `MetadataPending`;
- `RemovalInProgress`;
- `RemovalPartial`;
- `Completed`;
- `Removed`.

Regras de estágio:

- o estágio só avançará depois de recibo confirmado;
- `ApiSaveOutcomeUnknown` exigirá reconciliação por identidade;
- `RemovalPartial` preservará alvos e recibos já processados;
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
3. `Groups`, com o mesmo `refid`, no menu principal.

Regras do comando:

- o mesmo ID literal será usado nas três camadas;
- não haverá registro no menu contextual de Transaction;
- o comando operará sobre a KB atual sem exigir Transaction selecionada;
- `Tools/Test-ExtensionCommandRegistration.ps1` deverá passar;
- build bem-sucedido não substituirá a verificação de sincronização das três
  camadas.

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

Decisão aprovada após a revisão do Cursor Auto:

- o seam da F2 deve devolver ou expor ao chamador o recibo final de cada tentativa,
  inclusive quando relança a exceção original do `Delete()`;
- a F3 não interpretará mensagem de exceção nem dependerá de exceção artificial para
  reconhecer a falha retryable;
- o executor de `Remove` será o único consumidor que transforma um recibo `Failed`
  retryable de ordem de dependência em item da passada seguinte;
- `Confirmed` remove o item da fila e `OutcomeUnknown` bloqueia, sem requeue;
- o formato técnico exato do canal (resultado, handle da tentativa ou equivalente)
  será definido na implementação, mas deverá ser exercitável no teste offline da F2.

## Itens ainda não implementados

As decisões acima já foram consolidadas documentalmente nos planos F1, F2 e F3 e no
checkpoint operacional. Ainda não foram alterados código, manifesto ou testes; a revisão
por pares da sprint permanece em aberto.

Permanecem como detalhamento técnico posterior:

- schema JSON e número de versão do diário;
- nomes exatos dos campos de estado e recibo;
- sincronização do novo comando nas camadas de runtime e manifesto;
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
- `Prepared`, `Active`, `OutcomeUnknown`, `RemovalPartial` e `Failed` bloqueiam nova
  operação até decisão explícita.

### 45. Preparação em duas fases

- a substituição começa gravando o envelope corrente como `Prepared`;
- a extensão relê o File pelo `FileId` e confirma identidade, conteúdo e hash;
- depois grava `Active` e confirma novamente;
- nenhum objeto de negócio é gravado antes dessas confirmações;
- se `Prepared` ficar interrompido, o humano pode continuar com o mesmo `OperationId`
  e `ApplicationId`, ou abandonar explicitamente;
- se uma operação `Active` for interrompida, o diagnóstico é somente leitura; só depois
  de confirmação humana pode ser marcada como concluída ou continuada.

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
- o mesmo ID deve existir em `Package.cs`, `CommandDefinition` e `Groups`;
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
