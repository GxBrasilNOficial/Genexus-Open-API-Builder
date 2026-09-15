# S-B111 · F3 — P8: validação na IDE

**Data:** 2026-09-14. **Sprint:** `S-B111`. **Fase:** F3, etapa P8.
**Plano governante:** [`2026-09-04-B111-F3-PLANO-DURABILIDADE-E-REMOCAO.md`](2026-09-04-B111-F3-PLANO-DURABILIDADE-E-REMOCAO.md), seção 9.
**Implementação validada aqui:** [`2026-09-14-S-B111-F3-P4-P7-IMPLEMENTACAO-OFFLINE.md`](2026-09-14-S-B111-F3-P4-P7-IMPLEMENTACAO-OFFLINE.md).

**Documento em andamento.** Ele é escrito enquanto a bateria acontece, um cenário por vez, com
os números colados da janela Output. Cenário sem registro aqui é cenário que ainda não foi
exercido — não presuma o contrário.

## 1. Ambiente

| Item | Valor |
|---|---|
| DLL | build Release do commit `a32798b` (P4 a P7 + encerramento de registro), instalada com `genexus /install` porque o manifesto ganhou três comandos |
| IDE | GeneXus 18 U15 |
| KB | `wsEducacaoSpTeste` |
| Transaction | `Teste` — chave de três partes (`TesteId`, `TesteDate`, `TesteCodigo`) e quatro subníveis (`TestePortfolio`, `TesteItem`, `TesteItemFolio`, `TesteItemFolioDoc`) |
| Volume | 25 objetos próprios: 1 API Object, 5 Procedures (List/Get/Create/Update/Delete), 18 SDTs, 1 File de metadata |
| Preservados | 3 SDTs compartilhados (`sdt_API_ErrorMessage`, `sdt_API_ErrorResponse`, `sdt_API_Pagination`), o Folder `TesteOpenApi` (reutilizado, `FolderWasCreated=False`) e a própria Transaction |

O diário da KB é o File `GxOpenApiBuilder_OperationJournal`, `FileId=88`, preexistente desde a
validação da P2 — todas as operações abaixo reutilizaram esse mesmo File (`Created=False`),
como o contrato exige: há exatamente um por KB.

## 2. Cenário 1 — remoção completa pela fila nova

**Setup:** API gerada e íntegra na Transaction `Teste`. Nada alterado à mão.
**Ação:** menu de contexto da Transaction → `Remover API gerada` → `Sim`.

| Medida | Resultado |
|---|---|
| Estado terminal | `Removed` / `Removed` |
| Fila | `Outcome=Removed`, `Passadas=1/25`, `Removidos=25`, `Pendentes=0` |
| Intenção registrada | `Alvos=30, NaFila=25, Preservados=5` |
| Snapshot do checkpoint | `Recibos=25, Inventário=30` |
| Diário | `Checkpoints=4`, `TotalMs=216`, `Durability=Confirmed` |
| Relatório B081 | `Confirmados=25, Pendências=0, Removidos=25`, 5,1 s |
| Custo total | `TotalMs=5114`, dos quais `FilaRemocao=4714ms` |
| Identidade | `OperationId='22090d87-…'`, `ApplicationId='1a7c456d-…'` |

**O que isso prova.** A intenção completa — 30 alvos, 25 na fila — foi gravada **antes** do
primeiro `Delete()`, e o inventário do checkpoint (30) não é derivação dos recibos (25): é o
registro do que se pretendia, que sobrevive a uma interrupção. Os `Checkpoints=4` batem com a
política `3 + P` para `P=1`. Os três SDTs compartilhados, o Folder reutilizado e o Business
Component da Transaction ficaram intactos.

## 3. Regeração intermediária — Apply completo

**Setup do cenário 2.** API regerada pelo Wizard com a mesma seleção (cinco serviços, quatro
subníveis, todas as etapas de geração). Nada alterado nas abas.

| Medida | Resultado |
|---|---|
| Relatório | `Criados=25, Atualizados=3, Avisos=2`, 11,0 s, `Resultado='SuccessWithWarnings'` |
| API Object | `ApiSaveAttempted=True`, `ApiSaveCount=1`, `FinalApiWriter='List'` |
| Diário | `OperationId='ae98e2cb-…'`, `ApplicationId='27c47222-…'`, `Checkpoints=4`, `TotalMs=141` |
| Metadata | `GOAB_API_METADATA_B060_V3`, `Bytes=117988`, `Sha256='46C7BE98…'` |
| Integridade | `PlannedContractHash='16DF0B0A…'` |

Os dois avisos são conhecidos e não bloqueiam: fallback em inglês nas descrições de serviço e o
Folder preexistente que será reutilizado e nunca removido.

A ordem da F1 aparece intacta na Output: SDTs, Procedures, preparação do API Object sem gravar,
Business Component, List — e **um** `API.Save()` ao fim, pelo writer `List`.

## 4. Cenário 2 — alvo previsto ausente antes do `Delete()`

**A mudança de comportamento da P4, exercida em campo.**

**Setup:** com a API íntegra, o **API Object `apiTeste` foi apagado à mão** pela KB Explorer.
Escolhido por ser o único objeto da API que ninguém referencia — qualquer SDT ou Procedure seria
recusado pela IDE por estar referenciado. A metadata `apiTeste_Metadata` foi deixada no lugar:
é ela que declara o API Object como alvo previsto, e é o descompasso entre o previsto e o real
que o cenário exercita.

**Ação:** menu de contexto da Transaction → `Remover API gerada` → `Sim`.

| Medida | Resultado |
|---|---|
| Estado terminal | `Partial` / `RemovalPartial` |
| Motivo persistido | `TargetAbsentBeforeDelete` |
| Fila | `Passadas=1/25`, `Removidos=0`, `Pendentes=25`, `Bloqueado='ApiObject:apiTeste'` |
| Snapshot do checkpoint | `Recibos=1, Inventário=30` |
| Diário | `Checkpoints=3`, `TotalMs=121`, `Durability=Confirmed` |
| Relatório B081 | `Resultado='Interrupted'`, `Removidos=0`, `Bloqueados=1`, `Avisos=1`, 351 ms |
| Identidade | `OperationId='0eee2cc9-…'`, `ApplicationId='27c47222-…'` |

**O que isso prova.**

1. **`Removidos=0` com 25 pendentes.** A fila parou no primeiro alvo e não encostou nas cinco
   Procedures, nos dezoito SDTs nem na metadata. Antes desta DLL a ausência seria engolida como
   sucesso implícito e a remoção seguiria em frente — que é a origem do relatório «Removidos:
   nenhum» com objetos apagados, de 2026-09-06;
2. **`Checkpoints=3`.** Nenhuma passada chegou ao fim, então `P=0` e a matriz `3 + P` dá três:
   `Prepared`, `Active` e o terminal. O checkpoint de passada só existe quando a passada se
   completa;
3. **`Recibos=1` contra `Inventário=30`.** Um único recibo — o `NotAttempted` do API Object —
   e a intenção inteira preservada. É essa assimetria que permite dizer depois o que era
   previsto e o que aconteceu;
4. **`ApplicationId='27c47222-…'` é o mesmo do Apply da seção 3.** Confirma em campo a linha da
   matriz de identidade da seção 4.1.1: um `Remove` sobre metadata V3 cria `operationId` novo e
   **reutiliza** o `applicationId` do ownership, sem regravar a metadata. No cenário 1, o
   `applicationId` era outro (`1a7c456d-…`), o da geração anterior.

### 4.1 Anotação de apresentação, sem ação nesta etapa

O diagnóstico de persistência do relatório mostra, para o alvo ausente:

```
[ApiObject/API] apiTeste: Outcome=OutcomeUnknown; Confirmação=NotAttempted; API Object ausente antes do Delete.
```

e o relatório conta isso como `Pendências=1`. O estado, porém, é **conhecido**: o objeto está
comprovadamente ausente. `OutcomeUnknown` vem da resolução de desfecho do seam da F2 para um
`RecordNotAttempted`, é anterior a esta frente e **não influencia decisão nenhuma** — a fila
classificou `AbsentBeforeDelete` por evidência própria e o envelope gravou
`TargetAbsentBeforeDelete`. Fica registrado como candidato a ajuste de vocabulário depois da
P8, não como defeito de comportamento.

## 5. Cenário 3 — recuperação sobre o envelope interrompido

**Passou**, em três passagens: a primeira exerceu a recusa e expôs o problema da janela, a
segunda e a terceira ajustaram as medidas, e a última executou o encerramento.

**Setup:** o envelope `Partial/RemovalPartial` deixado pelo cenário 4. Nada alterado à mão.
**Ação:** menu de contexto da Transaction → `Recuperar operação interrompida`.

A reidratação leu o envelope e apurou a etapa certa:

```
Operação Remove sobre 'Teste': estado Partial/RemovalPartial, envelope Active, durabilidade Confirmed.
OperationId=0eee2cc9-…, ApplicationId=27c47222-…, atualizado em 2026-09-15 01:15:45Z.
Motivo registrado no envelope: TargetAbsentBeforeDelete.
Próxima etapa apurada: Discard.
```

As **trinta** linhas de inventário saíram com o cruzamento correto: `ApiObject apiTeste —
previsto: Delete; na KB: Absent`, os 24 alvos restantes `Delete`/`Present`, e os cinco
preservados (3 SDTs compartilhados, Folder e Transaction) como `Preserve`/`Present`. O
`OperationId` é o mesmo do cenário 4: a recuperação agiu sobre o envelope existente, sem criar
outro.

**Resposta `Não`:** `Recuperação recusada pelo usuário. Nenhuma alteração foi feita.` — o
caminho seguro fecha sem tocar em nada, como esperado.

**Resposta `Sim`:**

```
Recuperação concluída: Etapa='Discard', OperationId='0eee2cc9-…', Estado='Completed'.
O registro da operação interrompida foi encerrado. A Knowledge Base está liberada para a
próxima operação; nenhum objeto foi apagado, e o inventário do que ficou pela metade continua
gravado no diário.
```

O `OperationId` é o mesmo desde o cenário 4 — três operações de recuperação sobre o **mesmo**
envelope, sem que nenhuma criasse outro. O envelope foi de `Partial/RemovalPartial` a
`Completed/Discarded`, e os 24 objetos, a metadata e o Folder continuaram na KB.

Com isso, a saída que antes exigia apagar o File do diário à mão passou a existir dentro da
ferramenta — que era o ponto da P6.

### 5.1 Correção de apresentação saída deste cenário

A janela era um `MessageBox` nativo, que não aceita largura customizada: o texto chegava numa
coluna estreita, o inventário de 24 nomes derretia dentro do parágrafo e os asteriscos de
Markdown apareciam literais. Trocada pelo `ExtensionRecoveryDialog`, com o desenho do diálogo
do Remover — largura de leitura, inventário em bloco monoespaçado e rolável, pergunta no rodapé
e o botão seguro com o foco. Os resumos deixaram de enumerar nomes.

Numa segunda passagem, com o diálogo já em uso, as medidas foram aumentadas em 30% na largura
e na altura — 1404 × 624, com o bloco de inventário até 546 px, tudo limitado pela área útil do
monitor. Nomes de SDT hierárquico desta Transaction passam de sessenta caracteres, e é a
largura que decide se o inventário se lê ou se quebra no meio do nome.

As medidas finais, depois de duas passagens sobre a lista real de trinta alvos: **1685 × 749**,
com o bloco de inventário até 655 px e piso de 1123 px de largura ao encolher.

## 6. Cenário 4 — devolver a KB ao normal

**Passou.** Depois de apagar o par API Object + metadata, o Wizard reaplicou sobre o que restou:

| Medida | Resultado |
|---|---|
| Relatório | `Criados=2`, `Atualizados=26`, `Removidos=0`, `Bloqueados=0`, 9,3 s |
| O que foi criado | o API Object (`d8448562-…`) e a metadata |
| O que foi reencontrado | 21 SDTs (18 próprios + 3 compartilhados) e as 5 Procedures, `Created=0` em ambos |
| Metadata | `GOAB_API_METADATA_B060_V3`, `Bytes=117988` |
| Integridade | `PlannedContractHash='16DF0B0A…'` — **idêntico** ao da geração original |
| Diário | `OperationId` novo, `Checkpoints=4`, `TotalMs=250`, `Completed/Completed` |

O `PlannedContractHash` igual ao de antes é a prova de que o contrato reconstruído é o mesmo:
as preferências da KB descreviam a mesma API que a metadata apagada descrevia. Num caso em que
as preferências tivessem mudado, ele seria outro — e o aviso sobre paginação, ordenação e
obrigatórios existe justamente para esse caso.

### 6.1 Achado — o File do diário nascia sem módulo

O LSI.Extensions, extensão de terceiros instalada na mesma IDE, avisou:

```
warning: Object GxOpenApiBuilder_OperationJournal has no folder/module assigned
```

Procedia. O store criava o File com nome, Description e conteúdo, e não atribuía módulo — a IDE
aceita, mas o objeto fica fora da organização que todo o resto da extensão segue: a metadata de
negócio herda o módulo da Transaction, e o File de preferências está no `Root Module`.

O diário pertence à **KB**, não a uma Transaction, então o lugar dele é o `Root Module`, via
`Module.GetRoot`. A atribuição é idempotente e corrige também um diário criado antes desta
versão, na primeira gravação seguinte — não é preciso apagar nada.

O aviso veio de fora e apontou um objeto nosso; sem ele, isso passaria despercebido por tempo
indefinido, porque não quebra nada.

**Verificado na IDE em 2026-09-14**, depois de um reapply de reencontro com a DLL corrigida
(`Criados=0`, `Atualizados=28`, `Bloqueados=0`, diário `Completed/Completed`): as Properties do
File passaram a mostrar `Module: Root Module`.

A organização deixou de depender de inspeção manual: a linha de abertura do diário passou a
publicar `Module='Root Module'` — ou `'<sem módulo>'` — ao lado de `FileId` e `Bytes`.

#### O aviso do LSI continua, e está certo assim

A leitura seguinte do log da extensão de terceiros desmentiu a conclusão de que o aviso sumiria:

```
warning: Object GxOpenApiBuilder_OperationJournal has no folder/module assigned
warning: Object apiTeste_Metadata has no folder/module assigned
```

A metadata **tem** módulo — `Root Module`, tanto nas Properties quanto na Nota de telemetria do
Remover. Se ela aparece na mesma lista, o que o LSI cobra é a ausência de **Folder**, não de
módulo. E nenhum dos dois Files está em Folder, por decisão desta extensão, registrada em
2026-07-28 nas evidências de B061/B062: **File é organizado por módulo, não por Folder**. Os
SDTs, as Procedures e o API Object vão para o Folder da Transaction; os Files, não.

Ou seja: a correção do módulo era legítima e independente — o diário nascia sem módulo nenhum,
ao contrário da metadata e das preferências —, mas ela não resolve o aviso, e não deveria. O
LSI tem outra convenção de organização, e avisar é o trabalho dele.

O outro aviso da mesma leitura — `Object apiTeste: There are unused variables: apipage,
apipagesize, …` — é conhecido desde 2026-07-26 e já classificado como não bloqueante: são as
variáveis que o Service Source usa pelo contrato REST, e o build nativo do API Object as
reconhece.

**A lição, que vale além deste caso:** a primeira leitura tinha uma explicação plausível — o
aviso roda no start, antes da gravação — e ela sobreviveu porque ninguém procurou o segundo
objeto na mesma lista. Foi a metadata aparecendo ao lado do diário que mostrou qual era a regra
de verdade.

## 7. Cenário 5 — abortar um Apply no meio

**Passou**, em duas passagens: a primeira exerceu aborto, bloqueio e oferta, e expôs um defeito
de apresentação; a segunda, na versão corrigida, executou o encerramento e confirmou que a KB
voltou a aceitar operações.

Este é **o cenário que motivou a quarta ação da recuperação**: um `Apply` interrompido não pode
ser retomado, porque o envelope guarda o hash do contrato e não o contrato. Antes desta frente,
ele travava a Knowledge Base e a única saída era apagar o File do diário à mão.

**Passo 1 — aborto.** `Wizard` → `Concluir e aplicar` → `Abortar` por volta dos 3 s. A parada
efetiva veio depois das Procedures e antes do API Object: os 21 SDTs foram **reencontrados sem
gravação** — por isso não têm recibo — e as 5 Procedures foram salvas, que é o que o inventário
registra. O aborto para depois do objeto em curso, não no instante do clique.

| Medida | Resultado |
|---|---|
| Envelope | `Partial` / `NotStarted`, `blockReason=UserAborted` |
| Checkpoints | 3 — `Prepared`, `Active` e o terminal; nenhuma fronteira intermediária alcançada |
| Snapshot | `Recibos=5, Inventário=5` — as cinco **Procedures** que chegaram a ser gravadas |
| Relatório | `Atualizados=5`, `Bloqueados=1`, `ApiSaveCount=0`, 2,2 s |

`ApiSaveCount=0` importa: o aborto pegou antes do API Object, e a fronteira `ApiPhysicallySaved`
**não** foi registrada — que é exatamente a correção que a P3 fez em campo. A linha de abertura
já trouxe `Module='Root Module'`, a instrumentação da seção 6.1 em uso.

**Nota de vocabulário, sem ação.** `Partial/NotStarted` parece dizer que nada começou, enquanto
cinco objetos foram atualizados. É coerente com o desenho — no Apply o estágio só avança nas
fronteiras declaradas, e o que aconteceu no meio está no inventário e nos recibos —, mas quem lê
o envelope precisa saber que `logicalStage` não é uma barra de progresso.

A recuperação foi **recusada** de propósito nesta passagem, por causa do defeito da seção 7.1; o
envelope ficou intacto para o encerramento ser exercido na versão corrigida.

**Passo 3 — encerrar o registro**, na versão corrigida:

```
Recuperação concluída: Etapa='Discard',
OperationId='87526386-b226-48f6-91cb-b7785bd50f34', Estado='Completed'
```

O `OperationId` é o do aborto: nenhuma operação nova foi criada. As três janelas — relatório,
oferta e diálogo da recuperação — apareceram uma de cada vez, sem nada por baixo.

**Passo 4 — a KB voltou a aceitar operações.** `Wizard` → `Concluir e aplicar`:

| Medida | Resultado |
|---|---|
| Relatório | `Criados=0`, `Atualizados=28`, `Bloqueados=0`, 8,8 s |
| Diário | `OperationId` **novo** (`5d453ded-…`), `Checkpoints=4`, `TotalMs=126`, `Completed/Completed` |
| API Object | `ApiSaveCount=1`, writer `List` |
| Integridade | `PlannedContractHash='16DF0B0A…'`, o mesmo de sempre |

É esta última linha que fecha o cenário: o encerramento destravou de fato, e não apenas trocou o
texto do envelope.

**Passo 2 — a operação seguinte bloqueia.** `Wizard` → `Concluir e aplicar`:

```
[GateBlocked/JournalNonTerminal] Pré-condição 'PriorIntentReconciled'. O diário da KB registra a
operação Apply em estado Partial/NotStarted, que não é terminal. … operationKind=Apply,
envelopePhase=Active, operationState=Partial, logicalStage=NotStarted, blockReason=UserAborted,
journalDurability=Confirmed
```

`Criados=0, Atualizados=0, Removidos=0`: nada foi tocado antes de bloquear. Em seguida apareceu a
oferta proativa, e a recuperação apurou `Discard` com o inventário correto — as cinco Procedures
`previsto: Update; na KB: Present`.

### 7.1 A janela de progresso ficava viva atrás dos diálogos

O caminho de bloqueio pelo diário encerra a operação dentro do escopo da janela de progresso, e
ela continuava aberta — com o botão `Abortar` ativo — atrás do relatório final e da oferta de
recuperação. Um `Abortar` que já não aborta coisa nenhuma, numa operação que terminou.

A primeira correção tratou os três caminhos de bloqueio pelo diário. Ao documentá-la, ficou
claro que o problema era maior: **todos** os relatórios finais são mostrados dentro do escopo da
janela — sucesso, falha de etapa e aborto inclusive —, e só o bloqueio tinha sido coberto.

A correção final é na origem: o escopo ativo passou a ser conhecido por thread, e
`ShowFinalReport` fecha a janela antes de aparecer. Vale para todos os caminhos e para os que
vierem, sem depender de cada chamador lembrar. `Dispose` continua idempotente, então o `using`
de quem abriu segue correto.

**Visibilidade verificada contra a tag**, como manda a regra da casa: o escopo de progresso
entrou no commit `3e7ca61` (`B082`), que é ancestral de `v0.1.0-alpha.7`. Ou seja, **o defeito
está na versão publicada** — não é interno a esta frente. Ele só ficou visível agora porque o
bloqueio pelo diário somou um segundo diálogo por cima; com um diálogo só, a janela atrás passa
despercebida.

## 8. Cenário 6 — o envelope `Prepared` não é alcançável pela interface

**Reformulado pelo próprio teste.** O roteiro pedia um envelope `Prepared` — aquele que registra
a intenção e nunca toca a KB — para exercer o **abandono**, que é a saída distinta do
encerramento. Ele não acontece.

A abertura do diário grava `Prepared` **e** promove a `Active` na mesma chamada, antes de
devolver a sessão: é o que a matriz da seção 4.4 manda, e não há ponto de aborto entre os dois
`Save`. Um envelope `Prepared` durável só sobrevive se o processo morrer entre eles — crash ou
kill da IDE, não clique. Medido em 2026-09-15: aborto no primeiro segundo produz
`Partial/NotStarted` com `Recibos=0` e `Inventário=0`.

Consequência a registrar: **`Checkpoints.Abandon` e `RecoveryNextStep.Abandon` existem sem
caminho de entrada pela interface.** Não são código morto — o contrato da F3 prevê a fase
`Prepared` e o abandono como sua saída, e um crash entre os dois checkpoints é possível —, mas
nenhuma sessão de teste vai produzi-los clicando. Fica declarado para que ninguém procure esse
cenário de novo.

### 8.1 O defeito que o cenário produziu no lugar do planejado

O envelope abortado cedo é `Partial` **com zero recibos**, e a recuperação o tratava como
qualquer outro: oferecia encerrar o registro com o texto «a operação registrada gravou objetos na
Knowledge Base e parou no meio». **Falso** — não gravou nada, e essa é a frase que a pessoa lê
antes de decidir.

O rehydrator passou a distinguir os dois casos pelo número de recibos:

| Envelope | O que o resumo e a pergunta dizem |
|---|---|
| com recibos | gravou objetos e parou no meio; o que ficou pela metade continua como está |
| **sem recibo nenhum** | foi interrompida antes de gravar qualquer objeto; a KB está como estava antes dela |

É o caso mais comum de aborto — quem desiste, desiste cedo —, e era exatamente o que ele lia
errado. Caso novo no gate `tests.operationJournalRecovery`, com a asserção de que o resumo **não**
pode falar em «o que ficou pela metade» quando nada foi gravado, e asserções trilíngues no gate
de localização.

**Verificado na IDE em 2026-09-15**, no mesmo envelope: o bloqueio saiu com
`logicalStage=NotStarted` e `blockReason=UserAborted`, a oferta apareceu, e o diálogo trouxe o
texto novo — «foi registrada e interrompida antes de gravar qualquer objeto: o diário não tem
nenhum recibo». O encerramento fechou em `Completed`, preservando o `operationId`.

**A mesma frase falsa reapareceu no passo seguinte**, e foi corrigida junto: a mensagem de
desfecho ainda prometia «o inventário do que ficou pela metade continua gravado no diário» a
quem não gravara nada. O executor passou a escolher o desfecho pela mesma regra do resumo.

### 8.2 Dívida de localização encontrada de passagem

Ao corrigir o desfecho, ficou visível que **os resumos e desfechos da recuperação não passavam
pelo catálogo**: nascem no rehydrator e no executor, que são SDK-simples e não conhecem idioma, e
chegavam à tela em português em qualquer KB. Era a única parte da recuperação que a P7 não tinha
coberto — os textos de comando, confirmação e bloqueio já estavam nos três idiomas.

A tradução passou a acontecer onde o texto vira tela, no comando, e as frases entraram no
catálogo com asserções no gate. Sem a bateria na IDE, isso só apareceria para quem usasse a
extensão numa KB em espanhol ou inglês.

#### 8.2.1 A correção de 2026-09-14 cobria só uma parte — fechada em 2026-09-15

Perguntado se a dívida estava fechada, fui conferir em vez de responder de memória: **não
estava**. O commit `256f537` cadastrou os quatro desfechos do executor, o resumo do envelope sem
recibo e os dois rótulos de operação, e ligou `texts.Translate(...)` nos quatro pontos do
`Package.cs` onde o `Summary` vira tela. O que ficou de fora era o resto do que a mesma tela diz:

| Superfície | O que faltava |
| --- | --- |
| Bloqueios do rehydrator | durabilidade não confirmada, `Prepared` com recibos, resultado desconhecido, alvos ilegíveis |
| Resumos autorizados | abandono do `Prepared`, encerramento com recibo, reconciliação da remoção, `TargetAbsentBeforeDelete`, retomada da fila, envelope já terminal |
| Inventário do diálogo | os rótulos `— previsto:` e `; na KB:` de cada linha de alvo |
| Recusas do executor | lock ocupado, transição inválida no estado revalidado, etapa sem execução, continuação não fornecida |
| Autorização vencida | as seis recusas do `RecoveryAuthorization.Validate`, que viram `Summary` no caminho bloqueado |

Trinta entradas novas no catálogo, em espanhol e inglês. Três delas exigiram partir a frase em
duas metades, porque o miolo carrega enum (`A operação Apply parou em Partial/ApiObjectWritten.`)
e um valor variável não pode ser cadastrado por substring; os enums continuam como estão, que é o
que se cola num relato. Os rótulos do inventário só chegam traduzidos porque
`DescribeRecoveryTargets` passou a receber `ExtensionTexts` — a lista de detalhes do diálogo não
passava por tradução nenhuma.

O gate `tests.extensionOutputLocalization` ganhou onze asserções novas, entre elas a que fecha as
duas metades em volta do enum e a que exige o inventário traduzido com o enum preservado
(`Procedure procTesteList — planned: Delete; in the KB: Present`). Build Release com 0 avisos e
0 erros; orquestrador mecânico com 64 `passed` e 1 `skipped`.

## 9. Cenários restantes

| # | Cenário | Estado |
|---|---|---|
| 1 | Remoção completa (fila nova) | **passou** — seção 2 |
| 2 | Alvo previsto ausente antes do `Delete()` | **passou** — seção 4 |
| 3 | Recuperação sobre o envelope `Partial`: encerrar o registro | **passou** — seção 5 |
| 4 | Devolver a KB ao normal | **passou** — seção 6 |
| 5 | Abortar um Apply no meio; oferta proativa; recuperação | **passou** — seção 7 |
| 6 | Envelope `Prepared` e abandono | **reformulado** — não é alcançável pela interface; ver seção 8 |
| 7 | Interromper uma remoção no meio e retomar a fila | não iniciado |
| 8 | Remoção de API legado, com metadata válida e com metadata insuficiente | não iniciado |
| 9 | Acréscimo de tempo do diário na KB grande, contra o orçamento de 4.4 | não iniciado |

O cenário 3 dependia do envelope `Partial` deixado pelo cenário 2: **não apagar o File
`GxOpenApiBuilder_OperationJournal` à mão** entre um e outro, sob pena de destruir a condição.

## 10. Achado — metadata completa com API Object apagado à mão não tem saída pela ferramenta

Descoberto ao planejar o cenário 4, conferindo o código antes de propor o caminho.

O estado em que a KB ficou depois do cenário 2 — metadata **completa** e válida, API Object
ausente, 24 objetos próprios presentes — não é recuperável por nenhum dos três comandos:

| Comando | O que faz nesse estado | Por quê |
|---|---|---|
| `Wizard` | trava em `OwnershipSchemaApiNameOrGuidMismatch` | a metadata registra um `apiGuid` que não existe mais |
| `Remover API gerada` | `Partial` com `TargetAbsentBeforeDelete` | o primeiro alvo previsto está ausente — é o cenário 2 |
| Recuperação de metadata órfã (`B115`) | **não é oferecida** | exige exatamente **um** API Object presente (`TryPrepare`), e aqui há zero |

O `B115` cobre o caso vizinho — metadata **recuperada** apontando para um API Object que foi
removido e regerado com o mesmo nome —, e recusa deliberadamente a metadata completa: o
fingerprint B067 cobre o conteúdo inteiro, e corrigir só o `apiGuid` trocaria um bloqueio por
outro. A recusa está certa; o que falta é **orientação**: nenhuma das três mensagens diz o que
fazer.

A saída prática, exercida no cenário 4, é apagar **a metadata** à mão — um objeto, em vez dos
vinte e quatro — e reaplicar pelo Wizard, que reencontra SDTs e Procedures e regera API Object
e metadata. O plano volta aos defaults das preferências: paginação, ordenação e obrigatórios
específicos não sobrevivem, porque só existiam na metadata descartada.

**Corrigido na mesma data**, por decisão do usuário: texto não vira backlog. As duas mensagens
que a pessoa efetivamente encontra passaram a dizer o que fazer, nos três idiomas.

| Onde | O que passou a dizer |
|---|---|
| `ApiPlanMetadataFileWriter`, descompasso de `ownership.apiGuid` | que o File registra um API Object que não existe mais, que a saída é apagar **esse File** e reaplicar pelo Wizard, e o que se perde ao fazer isso |
| Relatório do `Remover`, quando o alvo ausente é o próprio API Object | as duas saídas — regerar sobre o que restou apagando a metadata, ou descartar apagando os objetos listados — deixando a escolha com quem decide |

A mensagem do `apiGuid` deixou de nomear o campo incompatível: nomear um campo de JSON não é
diagnóstico para quem está na IDE. A recusa continua a mesma; o que mudou é que ela agora
termina com um caminho. Asserções trilíngues no gate `tests.extensionOutputLocalization`.

### 10.1 A primeira correção estava no lugar errado — medido na IDE

A correção acima foi escrita presumindo que o Wizard travaria ao **gravar** a metadata. Ele não
trava: o leitor de estado desliga a etapa **antes**, e o Apply conclui tudo o mais. Na IDE, em
2026-09-14, o Apply sobre o estado do cenário 2 terminou com `Criados=1` — um API Object novo,
com GUID novo — `Atualizados=26`, `Bloqueados=0` e este aviso:

```
Etapa 'Metadata File' bloqueada na KB: Bloqueado: 1 colisao(oes) externa(s), incompativel(is)
ou ambigua(s) detectada(s). Nenhuma escrita sera permitida.
```

A orientação que eu tinha acabado de escrever vive no writer da metadata, e o writer nunca foi
alcançado. Presumir o caminho em vez de medir custou uma rodada.

**O ponto certo é `ApiPlanGenerationStateReader.InspectMetadataFile`**, no ramo que bloqueia por
ownership divergente em File próprio. Ele agora reconhece especificamente o descompasso de
`ownership.apiGuid` — a metadata registra um API Object que não está na KB, ou outro — e emite
uma colisão com causa e a instrução de qual File apagar, com os dois GUIDs ao lado. Os demais
bloqueios da etapa continuam como estavam: em integridade B067 divergente ou Business Component
fora do contrato, a metadata descreve o objeto certo, e apagá-la destruiria o baseline por um
problema que é de outro lugar.

**Efeito colateral do teste:** a KB ficou com um API Object novo (`69407f89-…`) e a metadata
antiga apontando para o GUID morto (`af422c2e-…`).

### 10.2 E a segunda também — o conflito é do API Object, não da metadata

A execução seguinte mostrou que, **neste** estado, quem bloqueia não é a etapa de metadata: é a
do **API Object**, e a de metadata apenas herda («Bloqueado: o API Object precisa estar
disponível antes»). A causa aparecia assim, na aba `List` do Wizard:

```
Conflitos (1):
  - Nome='apiTeste' | Tipo='API Object' | Modulo='Root Module' | Folder='TesteOpenApi'
    | Causa='OwnershipSchemaApiNameOrGuidMismatch'
    | ApiObjectGuid='69407f89-…' | MetadataApiGuid='af422c2e-…'
```

Os dois GUIDs já estavam ali, e o diagnóstico técnico inteiro abaixo — o que faltava era a
frase que diz o que fazer com isso.

`InspectApiObject` passou a acrescentar a orientação à causa **quando os dois GUIDs divergem** —
sinal objetivo, não nome de cláusula. A cláusula continua na frente, porque é o que se cita num
relato; a orientação vem depois do travessão. Os outros descompassos de posse — Description
alheia, integridade divergente, Service Source fora do contrato — continuam sem sugestão, porque
não se resolvem apagando a metadata.

**As três correções cobrem três estados distintos**, e é por isso que nenhuma delas era
suficiente sozinha:

| Estado da KB | Quem bloqueia | Onde a orientação vive |
|---|---|---|
| API ausente, metadata presente | etapa de metadata, ownership divergente | `InspectMetadataFile` (7.1) |
| API presente com GUID diferente do registrado | etapa do API Object | `InspectApiObject` (7.2) |
| gravação de metadata alcançada por outro caminho | writer da metadata | `ApiPlanMetadataFileWriter` (7) |

O estado a corrigir continua o mesmo, e o caminho também — apagar a metadata e reaplicar —,
agora com a ferramenta dizendo isso nos três pontos.

### 10.3 O bloco técnico deixou de competir com a orientação

Verificada a orientação na IDE, sobrou o ruído em volta dela: o diagnóstico do bloqueio
publicava **trinta e três linhas fixas**, onze delas vazias e cinco derivadas de uma medição
que nem tinha acontecido. Quando não há fingerprint gravado, `FingerprintHashOk=False` não é
resultado — é consequência de não haver o que comparar —, e `ClausulaQueFalhou` repetia o que a
causa já encabeçava.

`ApiPlanIntentionalChangeOwnershipDiagnosis.FormatDetails` passou a mostrar só o que foi
medido: a cláusula não se repete, os blocos de fingerprint e de baseline só se abrem quando
existem, e campo textual vazio não vira linha. No caso desta bateria, de trinta e três para dez
linhas, todas informativas:

```
ApiObjectCount=1
MetadataPresente=True
MetadataDescriptionPropria=True
MetadataParseOk=True
OwnershipSchemaApiNameGuid=False
FingerprintPresente=False
IntegrityPresente=False
SchemaGravado='GOAB_API_METADATA_B060_V3'
ApiNameGravado='apiTeste'
ApiNameEsperado='apiTeste'
```

Nada se perdeu: o que saiu era derivável do que ficou. O GUID atual e o da metadata continuam
na linha da causa, onde já estavam.

### 10.4 A orientação estava errada: os dois objetos saem juntos

A instrução das seções 10 a 10.2 mandava apagar **o File de metadata** e reaplicar. Exercida na
IDE, ela não funciona quando o API Object existe: sem metadata, a posse dele não pode ser
confirmada, e o Wizard volta a bloquear — agora com `Causa='MetadataMissing'`, `ApiObjectCount=1`.
Um bloqueio trocado pelo outro.

A saída correta é apagar **o par**: o API Object e o File de metadata. Depois disso o Wizard
cria os dois e reencontra SDTs e Procedures. As três mensagens passaram a dizer isso, e a dizer
**por quê** — «os dois, porque um sem o outro apenas troca este bloqueio pelo seguinte» —, para
que ninguém repita meio caminho.

`MetadataMissing` com API Object presente ganhou a mesma orientação: é o outro lado da mesma
moeda, e antes chegava à tela como uma palavra só.

Há um segundo caminho, oferecido pela própria ferramenta e **não exercido nesta bateria**: com a
metadata ausente e o API Object presente, a recuperação de metadata órfã (`B115`) se oferece na
abertura do Wizard e reconstrói uma metadata inventário-apenas, que devolve a posse. Ela foi
recusada aqui de propósito — o objetivo era metadata **completa** —, e fica como cenário à
parte.

**Três correções de orientação em três rodadas, todas pelo mesmo erro meu:** presumir o caminho
em vez de medi-lo. A primeira foi para o ponto que não é alcançado, a segunda para o estado
errado, e a terceira dizia meia verdade. O que as corrigiu, em todos os casos, foi executar na
IDE e ler o que apareceu.

O que **não** mudou, de propósito: a recusa do `B115` sobre metadata completa. Ela está certa
pelo motivo que o próprio código explica — o fingerprint B067 cobre o conteúdo inteiro, e
corrigir só o `apiGuid` trocaria um bloqueio por outro.
