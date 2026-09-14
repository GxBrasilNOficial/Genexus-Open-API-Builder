# S-B111 · F3 — P0 e P1 implementadas offline

**Data:** 2026-09-14. **Sprint:** `S-B111`. **Fase:** F3 (durabilidade e remoção).
**Plano governante:** [`2026-09-04-B111-F3-PLANO-DURABILIDADE-E-REMOCAO.md`](2026-09-04-B111-F3-PLANO-DURABILIDADE-E-REMOCAO.md).
**Decisões:** [`2026-09-07-S-B111-DECISOES-APROVADAS.md`](2026-09-07-S-B111-DECISOES-APROVADAS.md), seções 24 a 27 e 44 a 50.

Esta rodada entrega as duas primeiras etapas da F3 e **nada além delas**. Nenhum fluxo de
Apply, Sync ou Remove passou a gravar o diário; nenhuma recuperação nova foi implementada;
nenhum comando novo foi registrado no menu. O que existe agora é o contrato de dados sobre
o qual o resto da fase será construído, exercitado inteiramente offline.

## 1. Recorte

A F3 foi dividida em oito etapas, nesta ordem de dependência:

| Etapa | Conteúdo | Estado |
|---|---|---|
| P0 | promoção da metadata V2→V3 com `ownership.applicationId` | **entregue** |
| P1 | schema V1 do diário: modelo, serializer canônico, validador | **entregue** |
| P2 | ciclo de vida do diário na KB e checkpoints da matriz 4.4 | **entregue em 2026-09-14**, depois desta rodada — ver [o registro da P2](2026-09-14-S-B111-F3-P2-DIARIO-NA-KB.md); pendente de validação na IDE |
| P3 | gate estendido e `GateDiagnostic` com a precedência fechada | não iniciada |
| P4 | remoção com intenção, passadas e orçamento `maxPasses` | não iniciada |
| P5 | `ApiPlanRecoveryReader` / `Rehydrator` / `Executor` / `Report` | não iniciada |
| P6 | comando explícito de recuperação e `ShowRecoveryOptionProactively` | não iniciada |
| P7 | localização trilíngue e gates restantes | não iniciada |
| P8 | validação na IDE (seção 9 do plano) | não iniciada |

O recorte P0+P1 foi escolhido porque P0 é pré-condição declarada da fase — o plano a chama
de «pré-condição P1 da implementação da F3» na seção 5.2 — e porque ela muda material de
integridade: entrar junto com comportamento tornaria mais difícil isolar uma regressão de
fingerprint.

## 2. P0 — metadata V3

A promoção é **aditiva**. `ownership.applicationId` passa a integrar o payload e, como o
fingerprint cobre a metadata inteira menos o próprio campo `fingerprint`, ele muda por
definição quando o identificador muda. O mecanismo de hash não precisou de alteração:
`ApiPlanMetadataIntegrity.DiagnoseMetadataFingerprint` já serializa o snapshot sem o campo
`fingerprint`, então o campo novo entra no material sozinho.

| Arquivo | Mudança |
|---|---|
| `ApiPlanMetadataFileWriter.cs` | `SchemaVersionV2` passa a existir como constante própria; `SchemaVersion` emitido vira `..._V3`; `SupportedSchemaVersions` aceita V1, V2 e V3; `ownership.applicationId` é gravado a partir de `ApiPlan.ApplicationId`; `TryReadApplicationId` expõe a leitura tolerante do campo |
| `ApiPlanGeneratedApiRemovalPlan.cs` | aceita V3 e a mensagem de bloqueio passa a citar «V1, V2 ou V3» |
| `ApiPlanOrphanMetadataRecovery.cs` | o plano de recuperação carrega um `ApplicationId` próprio — novo quando B115 é autônomo — e a metadata importada grava o campo |

Os outros três consumidores obrigatórios — `ApiPlanGenerationStateReader`,
`ApiPlanApiObjectOwnership` e `ApiPlanApiObjectWriter` — **não precisaram de mudança**:
todos já validavam a versão pela lista central `ApiPlanMetadataFileWriter.SupportedSchemaVersions`,
e passaram a aceitar V3 pela mesma porta. A sobrecarga de `MatchesMetadataOwnership` que
recebe uma versão única continua existindo, mas nenhum chamador de produção a usa; ela é
exercitada só pelo teste de posse.

O que deliberadamente **não** foi feito:

- metadata legada V1/V2 não é regravada para preencher `applicationId`. A adoção tardia
  pertence ao diário, e o `Remove` sobre metadata legada não toca o File;
- o preflight sobre metadata existente não passou a exigir o campo novo: exigir V3 de quem
  foi gerado antes da frente transformaria toda API da Alpha em bloqueio.

## 3. P1 — schema V1 do diário

Três arquivos novos, todos SDK-free, no mesmo lugar e com o mesmo critério de testabilidade
do seam da F2:

| Arquivo | Responsabilidade |
|---|---|
| `Src/Extension/Diagnostics/ApiPlanOperationJournal.cs` | modelo do envelope e os treze enums fechados; constantes do nome lógico fixo `GxOpenApiBuilder_OperationJournal` e do arquivo externo |
| `Src/Extension/Diagnostics/ApiPlanOperationJournalSerializer.cs` | escrita canônica, leitura com validação estrita de tipos e `ComputeSnapshotHash` |
| `Src/Extension/Diagnostics/ApiPlanOperationJournalValidator.cs` | regras da decisão 24 e das decisões 44 a 47 |

### 3.1 O material canônico

O JSON persistido **é** o material canônico — não há uma segunda forma para hash. As regras
implementadas: propriedades na ordem do schema, arrays na ordem persistida, campos anuláveis
presentes como `null`, arrays vazios como `[]`, GUIDs em `D` minúsculo, timestamps UTC em
`yyyy-MM-dd'T'HH:mm:ss.fff'Z'`, `Formatting.None` e apenas os escapes obrigatórios do JSON.
O `snapshotHash` é o SHA-256 hexadecimal minúsculo desse material; ele não inclui
`journalFileId` — que é vinculação externa, não campo JSON —, não inclui autorização de
recuperação e nunca inclui a si mesmo.

A garantia é verificada pelo caminho mais duro disponível: o gate parte de um literal
canônico, lê, revalida e **reserializa**, exigindo igualdade byte a byte. Qualquer mudança
de ordem, de espaçamento ou de formato de valor quebra o teste.

### 3.2 A validação

O serializer valida antes de produzir bytes: um envelope inválido não chega ao `File.Save()`,
porque um diário inválido é indistinguível de um ausente na recuperação, e os dois bloqueiam
a operação. A leitura, ao contrário, **não lança** — devolve o resultado com os erros, porque
a indisponibilidade do diário é diagnóstico, não exceção de fluxo.

As regras implementadas, agrupadas:

- **identidade** — `knowledgeBaseGuid`, `transactionGuid`, `operationId` e `applicationId`
  obrigatórios e não vazios; `operationId` distinto de `applicationId`; `updatedUtc` nunca
  anterior a `createdUtc`;
- **envelope** — `Prepared` acompanha `Pending`, com a única exceção do abandono explícito;
  `Active` não admite `Pending`; `Completed` exige estágio `Completed` ou `Abandoned`;
  `Removed` pertence só a `Remove` e exige o estágio homônimo; `Partial` em `Remove` exige
  `RemovalPartial`; `Recovery` autônomo exige `intentKind=Imported` e não termina em
  `Removed` nem `Partial`;
- **bloqueio** — `blockReason` é obrigatório em `Partial` e `OutcomeUnknown` e proibido nos
  demais estados; `RetryBudgetExhausted` só existe em `Remove`;
- **abandono** — exige o objeto `abandonment` completo, envelope `Prepared`,
  `journalDurability=Confirmed` e **nenhum** recibo de gravação de negócio;
- **plano** — `planKind` amarrado ao `operationKind`; `Generation` exige `plannedApiGuid`,
  `contractHash` e as quatro flags (**remissão — 2026-09-14:** a exigência de
  `plannedApiGuid` passou a valer a partir do estágio em que a identidade tem de existir,
  porque numa criação nova o GUID só nasce do `API.Create`, dentro do pipeline — ver a seção
  4.1 do registro da P2); `Removal` exige inventário não vazio e recusa as flags de
  geração; `MetadataRecovery` recusa `contractHash`, porque a recuperação devolve inventário
  e não contrato;
- **recibos** — `sequence` positivo, único e monotônico; `attempt` positivo;
  `retryOfSequence` apontando para recibo anterior existente; `retryEligible=true` só com
  `Delete` + `Failed` + `Present` + `StillPresentAfterDelete`, e `retryableReason` inexistente
  fora disso;
- **inventário** — domínio de `action` por operação; `Guid` exige `guid`; `FileId` exige
  `fileId` positivo **e** `expectedHash`; `Composite` exige a identidade histórica inteira;
  `Folder` exige `emptyConfirmed=true` e posse validada; `None` só em item `Preserve`; a
  Transaction nunca entra na fila destrutiva; cada alvo aparece uma vez;
  `receiptSequences` só referencia recibos existentes;
- **metadata associada** — `metadataSchemaVersion` fechada em V1/V2/V3 e obrigatória quando
  a operação envolve metadata.

Versão de schema desconhecida bloqueia a leitura: não há migração destrutiva automática.

### 3.3 O que a P1 não decide

O modelo e a validação são o contrato de dados. **Nenhuma** política de quando gravar, quantos
checkpoints, como confirmar durabilidade ou como continuar uma operação entrou nesta etapa —
isso é P2 em diante. Em particular, o diário ainda não existe como File em nenhuma KB.

## 4. Gate novo

`Tests/OperationJournal/Test-ApiPlanOperationJournalSchema.ps1`, registrado no orquestrador
como `tests.operationJournalSchema` e coberto pelo teste do próprio checker.

Ele cobre: identidade fixa do schema; round-trip byte a byte para os três formatos de
envelope (Apply ativo, Remove parcial e abandono explícito); estabilidade e sensibilidade do
`snapshotHash`; versão e `journalKind` divergentes; JSON inválido como diagnóstico; e trinta e
dois casos de violação, cada um exigindo a mensagem específica — não basta bloquear, é preciso
bloquear pelo motivo certo. Um caso final mutila um envelope válido em memória e exige que o
**serializer** recuse antes de produzir bytes.

## 5. Ajuste na sentinela de cobertura da F2

`Test-ApiPlanPersistenceSeamCoverage.ps1` reconhece chamadas físicas por texto,
casando `<qualificador>.Save|Delete`. Os enums novos têm membros chamados `Save` e `Delete`,
e `JournalReceiptOperation.Delete` passou a ser contado como chamada física — três falsos
positivos. A sentinela passa a descartar `Journal*.Save|Delete`, que são valores de enum.

O inventário de chamadas reais continua fechado em 19 e nenhuma delas saiu do seam.

## 6. Verificação executada

| Verificação | Resultado |
|---|---|
| `dotnet build Src\GenexusOpenApiBuilder.sln --configuration Release` | 0 avisos, 0 erros |
| `tests.operationJournalSchema` (novo) | PASS |
| `tests.metadataIntegrity` | PASS |
| `tests.metadataHierarchical` | PASS |
| `tests.generatedApiRemovalPlan` | PASS |
| `tests.orphanMetadataRecovery` | PASS |
| `tests.apiObjectOwnership` | PASS |
| `tests.persistenceCore` / `tests.persistenceExecutor` | PASS |
| `tests.persistenceSeamCoverage` | PASS após o ajuste da seção 5 |
| `tests.generatedApiRemovalPreflight` / `...Resilience` | PASS |
| `Tests/PrePushChecker/Test-OpenApiBuilderPrePushChecks.ps1` | OK |

Nada foi validado na IDE nesta rodada, e nada precisava ser: as duas etapas são offline. A
primeira etapa da F3 que exigirá DLL instalada é a P2, quando o diário passar a existir como
File numa KB.

## 7. Riscos assumidos e abertos

- **Fingerprint de metadata muda para toda API regravada.** É consequência declarada da
  promoção aditiva. A primeira reaplicação de uma API existente produzirá um fingerprint novo;
  o reencontro continua funcionando porque a validação lê a versão gravada, não a esperada.
  A recaptura na IDE pertence à P2.
- **`ownership.applicationId` ainda não tem consumidor.** Ele é gravado e pode ser lido, mas
  só a P2 o cruzará com o diário. Até lá, é dado correto sem uso — deliberadamente, porque a
  ordem inversa exigiria gravar o diário antes de a metadata saber se identificar.
- **A validação de `blockReason` obrigatório em `Partial`/`OutcomeUnknown` é interpretação
  operacional** da regra «obrigatória em um checkpoint confirmado que registre bloqueio». Se
  a P2 encontrar um estado legítimo `Partial` sem motivo, a regra precisa ser revista aqui e
  não contornada no chamador.
- **`composite` e `emptyConfirmed` são nomes JSON escolhidos nesta etapa.** A decisão 24 exige
  os dados («a identidade histórica completa registrada no item», «`emptyConfirmed=true`») mas
  não fixa o nome do objeto que os carrega. Ficam registrados aqui como parte do V1.
