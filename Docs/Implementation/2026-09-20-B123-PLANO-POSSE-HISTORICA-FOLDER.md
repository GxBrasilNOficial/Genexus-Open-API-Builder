# B123 — Plano: posse histórica do Folder na metadata

Data: 2026-09-20.
Item de backlog: `B123` (documento 06).
Checkpoint: `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md` (próxima ação única vigente).
Estado: **frente fechada** (2026-09-20). Smoke §6 completo **para o núcleo medido** (DLL de `05da79a` + reteste IntentKind); checkpoint promove `B124`. Hardenings `86ef414`/`c358fb2` → gates offline + residual `B127`.

**Não** autorizava, neste arquivo, alteração de código, instalação, commit ou push. **Remissão — 2026-09-20:** código e gates da seção 5 executados.

## 0. Cobertura da leitura

Não li o checkpoint nem o documento 06 por inteiro. Li: o item `B123`; o achado da P8
(§12.5 de `2026-09-14-S-B111-F3-P8-VALIDACAO-IDE.md`); o contrato V3 da P0
(`2026-09-14-S-B111-F3-P0-P1-IMPLEMENTACAO-OFFLINE.md`); e, em 2026-09-20, o código e os
testes listados na seção 2. Documentos 08 e 14 foram só sondados por `transactionFolder` /
`wasCreated` — não relidos por inteiro. Risco residual: regra de Folder em seção que essa
sondagem não cruzou.

## 1. Problema

A fila de remoção só apaga o Folder da Transaction quando
`objects.transactionFolder.wasCreated == true`. Esse flag descreve a **operação corrente**,
não quem criou o Folder.

O efeito permanente medido na P8 — Folder vazio que nenhuma remoção futura apaga — não
nasce só de uma remoção que sobrevive. Nasce de **qualquer segunda gravação da metadata**
depois que o Folder já existe: o `ApiPlanBuilder` inicializa `TransactionFolderWasCreated`
com `false`; só `CreateOrReencounter` vira `true` quando **esta** execução cria o Folder;
o writer persiste o valor do plano. O reencontro (segundo Apply, Sync que regrava metadata,
Wizard sobre API já gerada) escreve `wasCreated=false` e a posse da criação original some.

A saída barata — apagar Folder vazio com Description canônica — foi **recusada** em
2026-09-15: Description isolada nunca autorizou exclusão neste projeto (seção 4.3 do plano
da F3), e um Folder homônimo de terceiro com a mesma marca seria apagado.

A saída aprovada: a metadata registrar a **posse histórica** do Folder. É mudança de schema
(V3→V4), com leitura legada V1/V2/V3, fingerprint e consumidores.

## 2. Evidência coletada (2026-09-20, só offline)

Nenhuma bateria da IDE. O recorte abaixo é leitura de código e de testes. Smoke na IDE
fica para depois da implementação.

### 2.1 `wasCreated` é da operação, e o plano nasce sempre `false`

`ApiPlanBuilder.BuildInternal` passa `false` no construtor. A única atribuição a `true` no
repositório é `ApiPlanTransactionFolder.CreateOrReencounter`, depois do `Save()` de um
Folder **novo**. Reencontro devolve o Folder existente sem tocar no flag.

O writer grava o valor do plano:

```csharp
["transactionFolder"] = new JObject {
    ["name"] = apiPlan.TransactionFolderName,
    ["wasCreated"] = apiPlan.TransactionFolderWasCreated
}
```

Não há leitura de `wasCreated` de volta para o `ApiPlan`, salvo o stub do inventário de
remoção. `PrototypeWizardExistingApiContractReader` não carrega o campo.

Consequência: o caminho feliz do B086 (`Deleted` incluindo o Folder, `wasCreated=true`) só
vale se a remoção ocorrer **antes** de qualquer regravação da metadata. Depois do segundo
Apply, a confirmação já mostra «reutilizado; nunca apagar» mesmo quando a extensão criou
o Folder na geração anterior. Isso está documentado como contrato do B066, não como defeito
— e é exatamente o que o `B123` tem de separar em dois campos.

### 2.2 A remoção decide pela cópia na metadata, não pelo Folder na KB

`ApiPlanGeneratedApiRemovalPlan.FromMetadata` e
`ApiPlanGeneratedApiRemovalInventory` leem só `objects.transactionFolder.wasCreated`.
`CountPlannedDeletes` e `BuildTargets` só enfileiram o Folder quando esse bool é `true`.
Caso contrário o alvo entra como `Preserve` / `Queued=false`.

O GUID do Folder **já existe** no Preview (`PreviewCapture.FolderGuid`) e nos resultados de
escrita de SDT/Procedure — e, desde esta frente, também na metadata. A Description **não**
enfileira o Folder (`ownedByThisApi` + nome + GUID o fazem). Em `DeleteOwnFolder`, porém,
Description canônica/legada e contêiner esperado ainda **preservam** se divergirem — gate de
execução, não de fila. Residual de confirmação mentindo «apaga se vazio» quando a Description
foi editada: `B126`.

`IsFolderEmpty` já conta WebPanel (correção de 2026-09-16, item 147 do checkpoint). Folder
próprio que não ficou vazio não deve ser apagado; isso permanece.

### 2.3 B115 é conservador de propósito

`ApiPlanOrphanMetadataRecovery` grava `wasCreated=false` com comentário explícito: não há
como saber se o Folder foi criado pela extensão. O teste
`Tests/OrphanMetadataRecovery/Test-ApiPlanOrphanMetadataRecovery.ps1` trava esse `false`.
O `B123` **não** afrouxa isso.

### 2.4 Três listas de schema, e um teste que já usa V4 como inválido

Versão emitida hoje: `GOAB_API_METADATA_B060_V3`.

Listas fechadas (cópias independentes):

| Onde | Papel |
|---|---|
| `ApiPlanMetadataFileWriter.SupportedSchemaVersions` | canônica para writer, posse do API Object e a maior parte dos leitores |
| `ApiPlanGeneratedApiRemovalPlan` (array **privado**) | aceitação da remoção; mensagem «esperado V1, V2 ou V3» |
| `ApiPlanOperationJournalValidator` (array **privado**) | `metadataSchemaVersion` do diário |

Fragmento de localização: `"V1, V2 ou V3"` / `"V1, V2 or V3"` em
`ExtensionOutputLocalization.cs`, com asserções em
`Tests/Localization/Test-ExtensionOutputLocalization.ps1`.

Armadilha: `Tests/GeneratedApiRemoval/Test-ApiPlanGeneratedApiRemovalPlan.ps1` atribui
`GOAB_API_METADATA_B060_V4` ao caso **rejeitado**. Sem atualizar esse fixture, o teste
passa a exigir que V4 seja inválido — o contrário desta frente.

Outros testes que cravam V3 como emitido: `tests.metadataHierarchical`,
`tests.metadataIntegrity`, `tests.operationJournalSchema` (literal canônico),
`tests.operationJournalCheckpoints`, `tests.operationJournalGate`,
`tests.operationJournalReceipts`.

O fingerprint B067 cobre o snapshot inteiro menos o próprio campo `fingerprint` (mesmo
mecanismo da P0). Campo novo em `transactionFolder` entra sozinho no hash. O
`PlannedContractHash` é outro material (`integrity.plannedContract`); não substitui o
fingerprint e não precisa de regra extra só por causa do Folder.

### 2.5 Textos que hoje prometem «nunca apagar»

- `ApiPlanTransactionFolder.CreateReuseWarning` — qualquer Folder preexistente, inclusive
  o que nós criamos na geração anterior.
- `ApiPlanGeneratedApiRemovalPlan.BuildConfirmationSummary` e
  `ExtensionConfirmDialog.BuildNotes` — ramificam só em `FolderWasCreated`.
- Catálogo: `"reutilizado; nunca apagar"` e o sufixo de
  `CreateReuseWarning` em `ExtensionOutputLocalization.cs`.

Depois do `B123`, «nunca apagar» só é verdade para Folder **sem** posse histórica.

## 3. Contrato proposto

### 3.1 Schema

- Versão emitida: `GOAB_API_METADATA_B060_V4`.
- Leitura: V1, V2, V3 e V4.
- Promoção **aditiva**, no molde da P0: metadata legada não é regravada só para preencher
  campo novo; a conversão ocorre quando Apply/Sync/B115 **já ia gravar** o File.
- Higiene obrigatória nesta frente: `ApiPlanGeneratedApiRemovalPlan` e
  `ApiPlanOperationJournalValidator` passam a consultar
  `ApiPlanMetadataFileWriter.SupportedSchemaVersions` (ou um helper único no mesmo tipo).
  Não nascer uma quarta cópia. A mensagem de recusa cita «V1, V2, V3 ou V4».

### 3.2 Forma JSON

`objects.transactionFolder` passa a:

```json
"transactionFolder": {
  "name": "TesteOpenApi",
  "wasCreated": false,
  "guid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "ownedByThisApi": true
}
```

| Campo | Papel |
|---|---|
| `name` | inalterado |
| `wasCreated` | **esta** execução criou o Folder? Relatório B081 e aviso de criação. Não autoriza mais a fila sozinho |
| `guid` | identidade do objeto Folder, análogo a `objects.apiObject.guid`. Sempre gravado quando o Folder existe nesta execução. **Não** prova posse: também se grava no reuso de terceiro |
| `ownedByThisApi` | posse histórica. Único bool que autoriza enfileirar o Folder para `Delete()` |

Não usar `ownership.applicationId` como dono do Folder: o builder ainda emite
`Guid.NewGuid()` a cada abertura do Wizard, e o `applicationId` é identidade da aplicação
no diário, não «quem criou este Folder».

### 3.3 Quando `ownedByThisApi` vira `true`

1. Esta execução **criou** o Folder (`CreateOrReencounter` criou e confirmou).
2. A metadata já persistida tem `ownedByThisApi=true` **e** o Folder vivo tem o mesmo
   `guid` — preservar no reencontro, mesmo com `wasCreated=false`. Homônimo com GUID
   novo **não** herda posse (grava `ownedByThisApi=false` + `guid` atual).
3. Adoção pontual do legado V1–V3: metadata existente com `wasCreated=true` e sem o campo
   novo. É a última evidência de «esta API criou o Folder e a metadata nunca foi
   regravada depois». Grava `ownedByThisApi=true` e o `guid` atual.

### 3.4 Quando fica `false` (e permanece `false`)

1. Reencontro de Folder preexistente **sem** os casos 2 ou 3 — terceiro, Folder deixado
   vazio por remoção antiga, ou segundo Apply já gravado com `wasCreated=false`.
2. B115 / metadata importada: `wasCreated=false` **e** `ownedByThisApi=false`. Sem
   `guid` obrigatório se o Folder nem for reivindicado; se o objeto existir, gravar `guid`
   não autoriza exclusão.

Não inferir posse de Description, prefixo legado, nome `*OpenApi` ou Folder vazio.

### 3.5 Quando a remoção apaga o Folder

Enfileirar se, e somente se:

1. `ownedByThisApi == true` (V4), **ou** metadata V1–V3 ainda com `wasCreated=true`
   (compatibilidade até a primeira regravação V4);
2. o Folder na KB tem o mesmo `guid` persistido, quando o campo existe — homônimo com GUID
   diferente não é apagado;
3. depois dos outros `Delete()`, `IsFolderEmpty` confirma vazio (contrato já existente,
   incluindo WebPanel).

Metadata V1–V3 com `wasCreated=false`: comportamento atual, Folder preservado. Não há
adoção retroativa para a maioria das APIs que já passaram por reencontro — o resíduo
continua sendo Folder vazio inofensivo, apagável à mão. O `B123` impede que **novas**
gravações V4 repitam a mão única.

### 3.6 Avisos e confirmação

- Folder criado nesta execução: texto atual («criado pela extensão; apagar só se ficar
  vazio»).
- Folder reencontrado **com** posse histórica: deixar de dizer «nunca apagar». Informar
  que a remoção desta API apaga o Folder se ele ficar vazio.
- Folder reencontrado **sem** posse: manter «reutilizado; nunca apagar».

### 3.7 Fora de escopo

- Apagar Folder de terceiro, homônimo, ou com GUID divergente.
- Posse por Description.
- Regravar metadata só para adotar Folder legado já marcado `wasCreated=false`.
- Continuação de Apply/Sync interrompido.
- Remoção direta sem metadata (`B115` no sentido de apagar sem File).
- Folder compartilhado `GxOpenAPI` (`SharedSdtFolderWasCreated`).

## 4. Consumidores (mesmo passo de implementação)

Código:

- `ApiPlanMetadataFileWriter.cs` — emitir V4; gravar `guid` e `ownedByThisApi`; ler o
  JSON anterior na regravação para preservar posse (não confiar só no bool da operação).
- `ApiPlan.cs` / `ApiPlanBuilder.cs` — transportar posse e GUID; o builder continua
  nascendo com `wasCreated=false`.
- `ApiPlanTransactionFolder.cs` — marcar posse ao criar; aviso de reuso ramificado.
- `ApiPlanGeneratedApiRemovalPlan.cs` — fila pela posse histórica; lista de schema
  compartilhada; mensagem V1–V4.
- `ApiPlanGeneratedApiRemovalInventory.cs` — **não alterado** neste passo: o stub de
  `ApiPlan` ainda lê `wasCreated` só como campo do plano; a autorização de exclusão do
  Folder ficou em `RemovalPlan` / Ownership, não no inventário de SDTs.
- `ApiPlanGeneratedApiRemover.cs` — `CountPlannedDeletes` / `BuildTargets` / conferência
  de GUID.
- `ApiPlanOrphanMetadataRecovery.cs` — `ownedByThisApi=false`.
- `ApiPlanOperationJournalValidator.cs` e comentário em `ApiPlanOperationJournal.cs`.
- `ExtensionConfirmDialog.cs`, relatório B081 se o texto de Folder criado/reutilizado
  passar a depender da posse.
- `ExtensionOutputLocalization.cs` — fragmento de versões e avisos novos.

Documentos na implementação (não neste arquivo): item `B123` no 06 apontando este plano;
modelo 08 (`TransactionFolderWasCreated` e o JSON V4); critério «Folder reutilizado»
no 14, distinguindo reuso de terceiro de reencontro próprio; evidência B086 (a regra
`wasCreated` deixa de ser a autorização); `CHANGELOG.md` `[Unreleased]` — uma entrada,
classificada pela pergunta do leitor (`Added` o campo / `Changed` a remoção).

Checkpoint: só depois de o contrato estar no código, ou quando o usuário pedir o
andamento operacional. Este plano **não** muda a próxima ação única.

## 5. Testes e gates

Reusar gates existentes; não inventar orquestrador paralelo.

| Gate | O que passa a exigir |
|---|---|
| `tests.transactionFolderReuse` | persistir `guid` e `ownedByThisApi`; reuso **não** marca posse; aviso ramificado |
| `tests.generatedApiRemovalPlan` | V4 aceito; recusa vira V5 ou V0 (nunca mais V4); fila por `ownedByThisApi`; V3+`wasCreated=true` ainda enfileira; V3+`wasCreated=false` não |
| `tests.orphanMetadataRecovery` | `ownedByThisApi=false` além de `wasCreated=false` |
| `tests.metadataHierarchical` / `tests.metadataIntegrity` | versão emitida = V4; V1–V3 continuam na lista |
| `tests.operationJournalSchema` e satélites do diário | `metadataSchemaVersion` aceita V4; literal canônico pode permanecer V3 **ou** passar a V4, desde que a lista fechada aceite os quatro |
| `tests.localization` | «V1, V2, V3 ou V4» nos três idiomas; avisos novos |

Casos offline mínimos da regra nova (no teste de remoção ou no de Folder):

1. V4, `ownedByThisApi=true`, `wasCreated=false` → Folder **entra** na fila.
2. V4, `ownedByThisApi=false`, `wasCreated=true` → Folder **não** entra (o flag da
   operação sozinho não autoriza).
3. GUID persistido ≠ GUID na KB → não entra, mesmo com posse.
4. Regravação preserva `ownedByThisApi=true` vindo do JSON anterior.

## 6. Aceite na IDE (depois do código)

DLL reinstalada. KB de teste, Transaction cujo Folder a extensão possa criar e apagar.

1. **Criação nova + Remover** — Folder some (regressão do B086 feliz).
2. **Criação + segundo Apply + Remover** — Folder some se ficou vazio. Este é o caso que
   hoje falha por contrato, e o que o `B123` existe para corrigir.
3. **Folder de terceiro** (nome canônico, Description humana, criado à mão) + Wizard +
   Remover — Folder permanece.
4. **B115** sobre API sem metadata — Remover não apaga o Folder.

Folder legado já vazio de sessão anterior **não** é critério de aceite: o plano recusa
adotar esse caso.

### Evidência smoke IDE — 2026-09-20 (parcial)

KB `wsEducacaoSpTeste`, Transaction `Teste`, Folder `TesteOpenApi`, File `apiTeste_Metadata`.
Hierarquia típica: 5 Procedures (incl. Delete), 18 SDTs próprios, 3 compartilhados `sdt_API_*`.

**Alcance desta medição.** Os quatro PASS abaixo valem para a DLL do commit `05da79a`
(2026-09-20 ~20:02) e o reteste IntentKind daquele ciclo. **Não** cobrem
`86ef414` (Preview/contagem desanuncia Folder com GUID divergente) nem `c358fb2`
(homônimo com GUID novo não herda `ownedByThisApi` na escrita) — ambos com gate offline;
re-smoke IDE numerado em `B127`.

1. **PASS** — Apply criação (`SchemaVersion` V4, Folder Guid `138a527d-…`, metadata
   `wasCreated=true` / `ownedByThisApi=true`). Remover: confirmação «criado pela extensão»;
   `FolderShouldBeRemoved=True`; `Deleted=26` incluindo `Folder:TesteOpenApi`; Folder sumiu
   da KB. B081 sucesso.
2. **PASS** — Apply criação (Folder Guid `a0db1027-…`, posse `true`/`true`); segundo Apply:
   `wasCreated=false`, `ownedByThisApi=true`, mesmo `guid`; aviso «a remocao desta API
   apagara o Folder se ele ficar vazio» (não «nunca sera removido»). Remover: confirmação
   «próprio da API; a remoção apaga se ficar vazio»; `Deleted=26` com `Folder:TesteOpenApi`;
   Folder sumiu. Caso que o contrato antigo perdia no segundo Apply.
3. **PASS** — Folder criado à mão (Description humana); Wizard reencontrou
   (`Criados=25`, Guid `3f401e9f-…`, aviso «nunca sera removido»); metadata
   `wasCreated=false` / `ownedByThisApi=false`. Remover: «reutilizado; nunca apagar»;
   `PlannedDeletes=25`; `FolderShouldBeRemoved=False`; `Deleted=25` sem Folder; B081
   sucesso; Folder **permaneceu** na KB (confirmado pelo operador). O sumiço posterior
   foi apagar à mão na preparação do teste 4 — não faz parte deste critério.
4. **PASS** (reteste pós-conserto) — B115: `recovery.imported`, `ownedByThisApi=false`.
   Preview «nunca apagar» / `PlannedDeletes=25`. Remover: diário abriu com
   `ApplicationId` do B115; `FolderShouldBeRemoved=False`; `Deleted=25` sem Folder
   (2 passadas: `ListFilters` saiu na 2ª após `ListResponse`); B081 `Success`.
   Folder permanece (critério). O bloqueio inicial do diário ficou no item 156 do checkpoint.

## 7. Ordem sugerida

1. Schema e writer (V4 + campos), leitura preservando posse.
2. Remoção pela posse + GUID; B115 conservador.
3. Avisos e localização.
4. Gates da seção 5.
5. Smoke da seção 6.
6. Documentos da seção 4.

Não misturar com `B125` (Preview mentiroso após aborto), `B121` nem `B108`.

## 8. Checklist de encerramento

- [x] Código emite V4 e lê V1–V4.
- [x] Listas de schema unificadas; recusa cita as quatro versões.
- [x] `ownedByThisApi` só pelos três casos da §3.3.
- [x] Remoção enfileira pela posse histórica + GUID; `DeleteOwnFolder` ainda exige
      Description própria, contêiner e vazio; B115 sem posse.
- [x] Aviso «nunca apagar» só sem posse.
- [x] Gates da seção 5 verdes.
- [x] Smoke da seção 6 na IDE, com evidência (1–4 PASS 2026-09-20; conserto IntentKind no 4) — núcleo; ver alcance acima e `B127`.
- [x] Documentos da seção 4 alinhados; `CHANGELOG` com uma entrada.
- [x] Checkpoint: frente `B123` fechada; próxima ação única = `B124` (2026-09-20).

## 9. Decisões que este plano já trava

1. Dois bools, papéis distintos: `wasCreated` (operação) e `ownedByThisApi` (história).
2. `guid` do Folder persistido; mismatch recusa exclusão.
3. Sem adoção de Folder cujo legado já está `wasCreated=false`.
4. B115 continua sem reivindicar dono.
5. Description **não** autoriza enfileirar; em `DeleteOwnFolder` ainda pode preservar
   (canônica/legada + contêiner). Ver `B126` para o anúncio da confirmação.
