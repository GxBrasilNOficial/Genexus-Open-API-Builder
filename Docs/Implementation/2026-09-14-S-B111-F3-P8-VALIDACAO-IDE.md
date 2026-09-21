# S-B111 · F3 — P8: validação na IDE

**Data:** 2026-09-14; bateria continuada e concluída em 2026-09-15 (cenários 7 a 9, seções 11 a 14).
**Sprint:** `S-B111`. **Fase:** F3, etapa P8.
**Plano governante:** [`2026-09-04-B111-F3-PLANO-DURABILIDADE-E-REMOCAO.md`](2026-09-04-B111-F3-PLANO-DURABILIDADE-E-REMOCAO.md), seção 9.
**Implementação validada aqui:** [`2026-09-14-S-B111-F3-P4-P7-IMPLEMENTACAO-OFFLINE.md`](2026-09-14-S-B111-F3-P4-P7-IMPLEMENTACAO-OFFLINE.md).

**Documento concluído em 2026-09-15.** Ele foi escrito enquanto a bateria acontecia, um cenário
por vez, com os números colados da janela Output. Os nove cenários da seção 9 do plano estão
registrados: oito passaram e um foi reformulado pelo que mediu. A ordem das seções é a da
execução, não a da numeração dos cenários.

## 1. Ambiente

| Item | Valor |
|---|---|
| DLL inicial | build Release do commit `a32798b` (P4 a P7 + encerramento de registro), instalada com `genexus /install` porque o manifesto ganhou três comandos. **Não foi a única** — ver a tabela de DLL por cenário abaixo |
| IDE | GeneXus 18 U15 |
| KB | `wsEducacaoSpTeste` |
| Transaction | `Teste` — chave de três partes (`TesteId`, `TesteDate`, `TesteCodigo`) e quatro subníveis (`TestePortfolio`, `TesteItem`, `TesteItemFolio`, `TesteItemFolioDoc`) |
| Volume | 25 objetos próprios: 1 API Object, 5 Procedures (List/Get/Create/Update/Delete), 18 SDTs, 1 File de metadata |
| Preservados | 3 SDTs compartilhados (`sdt_API_ErrorMessage`, `sdt_API_ErrorResponse`, `sdt_API_Pagination`), o Folder `TesteOpenApi` (reutilizado, `FolderWasCreated=False`) e a própria Transaction |

O diário da KB é o File `GxOpenApiBuilder_OperationJournal`, `FileId=88`, preexistente desde a
validação da P2 — todas as operações abaixo reutilizaram esse mesmo File (`Created=False`),
como o contrato exige: há exatamente um por KB.

### 1.1 Qual DLL exerceu cada cenário

A bateria não correu sobre uma DLL só: cada correção saída dela foi compilada e instalada antes
do cenário seguinte. Registrar isso não é burocracia — **evidência de runtime vale para a DLL que
a produziu, e só para ela** (regra de contrato runtime do `AGENTS.md`). Um cenário validado com
DLL anterior a uma mudança de emissor que o afete **precisa ser reexercido**; a data da captura
não basta.

A tabela abaixo é **derivada da evidência de cada seção**, não de memória: cada linha diz o que a
própria seção registra ter sido corrigido depois dela. Os commits estão em ordem cronológica —
`98112d8` → `01d491c` → `8283467` → `42cdf0e` → `256f537` —, e um commit que **registra** que um
cenário passou é posterior à execução dele.

| Cenário | DLL usada | Como se sabe |
|---|---|---|
| 1, 2, 3 | `a32798b` | primeira instalação da bateria |
| 4 | posterior a `a32798b` | o reapply de reencontro da seção 6.1 foi «com a DLL corrigida», depois do achado do módulo |
| 5 | **anterior** a `8283467` | a janela de progresso viva atrás do diálogo foi **descoberta neste cenário** (7.1); a DLL não podia ter a correção |
| 6, primeira passagem | **anterior** a `42cdf0e` | o texto falso do envelope sem recibo foi **produzido aqui** (8.1) |
| 6, confirmação | posterior a `42cdf0e` | 8.1 registra o diálogo com o texto novo, «no mesmo envelope» |
| 7 | `b788cf4` | build Release instalada em 2026-09-15, antes da retomada da bateria; o manifesto não mudou desde `19139b7`, então bastou trocar a DLL |
| 8 | `9ab74bb` | build com as duas correções de texto do cenário 7, instalada em 2026-09-15 antes das três variantes |
| 9a | `734aaa0` | **confirmado por hash**, não por memória: a DLL em `GeneXus18\Packages` e a build local têm o mesmo SHA-256 (`CEE7B6B6…`) e o mesmo carimbo de 2026-09-15 12:51:56 — é o mesmo arquivo. Esse build é posterior às edições de código de `734aaa0`; o que entrou depois dele naquele commit foi documentação e backlog, que não produzem DLL |
| 9b | `734aaa0` | mesma sessão e mesma instalação do 9a |

**Reexercício devido — e o que não é.** Entre o cenário 5 e hoje entraram, além de texto,
diagnóstico e localização, **um ramo de decisão no rehydrator**: `42cdf0e` acrescentou
`Receipts.Count == 0 → Discard` com resumo e pergunta de confirmação próprios. Isso não é texto,
e a distinção é exatamente o que esta tabela existe para fazer.

Ele **não** alcança o cenário 5: o envelope daquele aborto tem `Recibos=5` — as cinco Procedures
que chegaram a ser gravadas (seção 7) —, então ele toma o ramo **com** recibos, que `42cdf0e` não
tocou. E o ramo novo **foi exercido na IDE**: o cenário 6 produziu o envelope de zero recibos e a
seção 8.1 registra a confirmação com o texto novo, no mesmo envelope. O que mudou ali foi qual
dos dois resumos aparece e qual pergunta de confirmação é feita — a ação apurada é `Discard` nos
dois ramos, antes e depois.

Nada mais no intervalo toca o contrato de remoção nem o do diário, então os cenários 1 a 6
continuam valendo para o que provaram. **A medição de tempo é a exceção**, e a razão precisa de
escopo, porque a primeira redação desta seção dizia apenas «número de desempenho capturado com DLL
anterior não descreve a atual» — genérico assim, isso vence os **dezenove** números de tempo
publicados no `CHANGELOG.md`, nenhum dos quais alguém marcou nem pretende remedir. `Setor ~18 s`,
de agosto, é registro datado de uma frente encerrada; envelhecer não o torna falso.

O critério que separa um caso do outro: **vence a medição que sustenta uma decisão em aberto.** Os
182 ms do Apply sustentam o orçamento da seção 4.4, que é contrato vivo e acabou de ganhar uma
tabela derivada em cima dele — por isso precisam ser atuais. É por isso que o item 7a manda
refazer, e é só por isso.

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

#### 8.2.2 E o gate e o store, que eram a outra metade da mesma tela

Ao fechar a 8.2.1 declarei o que tinha ficado de fora: as mensagens do **gate estendido** e do
**store** do diário. Elas não pertencem ao comando de recuperação — são o que bloqueia Apply,
Sync e Remover quando existe um envelope não terminal —, mas chegam à mesma superfície: o
relatório final localiza cada linha pelo catálogo, e a Output também. Eram a metade do texto que
o usuário lê quando o diário barra uma operação.

Trinta e duas entradas novas, cobrindo:

| Origem | O que faltava |
| --- | --- |
| Identidade do diário | diário de outra KB, durabilidade não confirmada |
| Estado global | envelope preparado, envelope não terminal, resultado indeterminado, autorização de continuação vencida, serviço de continuação ausente |
| Rótulos do diagnóstico | `Pré-condição '...'` e `Contexto: ...`, que o próprio `Describe()` acrescenta |
| Store — confirmação | `Save()` que lançou, Id inutilizável, releitura que falhou ou não achou o File, `FileId` que resolveu para outro objeto, bytes, digest e hash canônico divergentes |
| Store — localização | File sem conteúdo, duplicidade de Files, File homônimo de outro dono |

**Uma mudança de texto no gate, e a razão dela.** Duas mensagens irmãs diziam a mesma coisa com
conectores diferentes: o bloqueio por envelope não terminal usava `em estado {estado}/{etapa}`, e
a indeterminação usava só `em {estado}/{etapa}`. O conector curto não pode entrar no catálogo —
` em ` sozinho recortaria qualquer outra frase —, e o longo já era o que a P2 publicou. Alinhar a
indeterminação ao conector longo é o que tornou as duas traduzíveis, sem mudar informação nenhuma.

**Um efeito colateral que precisou ser terminado.** `Foram encontrados N Files chamados 'X'` é
partilhado entre o diário e o aviso de **preferências duplicadas**. Cadastrar o começo sem a
cauda do segundo deixaria aquela mensagem meio em inglês — pior que inteira em português —, então
a cauda `Defaults conservadores em memoria aplicados.` entrou junto, com asserção própria.

**O que eu tinha declarado como fora, e não deveria estar** — ver 8.2.3: a lista de violações de
schema do validador e do leitor.

Vinte e quatro asserções novas no gate, entre elas a linha inteira do bloqueio como o relatório final a
recebe — código, reason, pré-condição, mensagem e contexto —, com os enums preservados. Build
Release 0 avisos e 0 erros; orquestrador mecânico com 64 `passed` e 1 `skipped`.

#### 8.2.3 O argumento que eu usei para deixar o schema de fora não se sustenta

Fechei a 8.2.2 dizendo que as violações de schema do `ApiPlanOperationJournalValidator` e do
leitor do `ApiPlanOperationJournalSerializer` ficariam em português de propósito: são assertivas
que nomeiam campo de JSON e valor de enum, só alcançáveis com um envelope corrompido ou editado à
mão, e mais próximas de um `reasonCode` do que de prosa.

A pergunta que derrubou isso foi direta: *«acha mesmo válido deixar quem não domina português
sofrer?»*. Não acho, e o argumento tinha um erro de método. Raridade não é critério de idioma:
uma mensagem rara é exatamente a que o leitor não conhece de cor, e o momento em que ela aparece
— o diário ilegível, a operação bloqueada sem saída óbvia — é o pior momento possível para
entregar texto num idioma que a pessoa não lê. «Técnico» também não é o mesmo que «não é prosa»:
`o abandono não admite recibos de gravação de negócio` é uma frase inteira, e o que ela tem de
técnico são os dois substantivos, não a sintaxe.

**Oitenta e cinco entradas novas**, cobrindo as duas origens:

| Origem | O que entrou |
| --- | --- |
| Leitor (`...Serializer`) | forma do JSON, `schemaVersion` e `journalKind` divergentes, e os sufixos do leitor de campos — obrigatório/opcional para string, inteiro, booleano, GUID, timestamp e enum |
| Validador | identidade e tempo, as quatro dimensões do envelope, abandono e encerramento, `blockReason`, plano, recibos, inventário e identidade dos alvos, metadata |

**O que não muda de idioma, por decisão:** caminho de JSON (`receipts[].retryOfSequence`), valor
de enum (`operationState=Removed`), nome de tipo (`JournalBlockReason`) e máscara de data. São o
que se procura dentro do arquivo; traduzi-los quebraria a única ponte entre a mensagem e o
conteúdo do diário.

**Ordem no catálogo importa mais aqui do que em qualquer lugar.** A substituição é por substring,
e os sufixos do leitor de campos são fragmentos curtos: ` é obrigatório.` recortaria o meio de
` é obrigatório e deve ser um GUID.` se viesse antes. As frases inteiras vêm primeiro, os sufixos
genéricos depois, e ` ou null.` por último — só sobra para o enum opcional, que tem o nome do tipo
entre o prefixo e o fecho.

**Rede de segurança no gate, além das asserções pontuais:** uma varredura passa sessenta e quatro
frases reais pelo tradutor e exige duas coisas de cada uma — que a versão inglesa **difira** da
portuguesa (igualdade prova entrada ausente no catálogo) e que nenhuma das duas versões contenha
marcadores que só existem em português (`deve ser`, `é obrigatório`, `só `, `não `, `pertence`,
`aceita`, `repete`). A lista espanhola de marcadores é menor de propósito: `exige` e `admite` são
as mesmas palavras nos dois idiomas, e uma regra feita só de enums pode coincidir legitimamente.

Build Release 0 avisos e 0 erros; orquestrador mecânico com 64 `passed` e 1 `skipped`.

#### 8.2.4 O que ainda sai em português — medido, não estimado

Ao fechar a 8.2.3 eu escrevi que «não há mais texto do diário que saia em português numa KB que
não seja pt-BR». **A frase era forte demais.** Fui medir em vez de repeti-la, e ela é falsa.

**Como foi medido.** Uma sonda varre os arquivos da F3 — `ApiPlanOperationJournal*`,
`ApiPlanRecovery*`, `ApiPlanRemoval*` e `ApiPlanGeneratedApiRemover` —, recolhe cada literal de
string fora de comentário com pelo menos doze caracteres e algum marcador de português corrido
(`ção`, `não`, `é`, `deve`, `exige`, `só`, `já`…), e verifica se ele aparece no catálogo, inteiro
ou como fragmento em volta dos `{N}`. É heurística, não prova: serve para dimensionar o resíduo
em vez de adivinhá-lo. A sonda ficou no scratchpad da sessão; o que vale é o resultado abaixo.

**Sessenta e cinco literais sinalizados.** Classificados um a um:

| Classe | Qtd. | Chega ao usuário? |
| --- | --- | --- |
| Falso positivo — já coberto pelo sufixo ` é obrigatório.` | 5 | — |
| Contrato de programador (`ArgumentException`, asserção interna) | 5 | não: só dispara com chamador errado |
| Dado gravado no diário (`abandonment.reason`, `authorizedBy`) | 2 | não é tela: é conteúdo do registro |
| `Detail` de observação, hoje não renderizado em lugar nenhum | 6 | não hoje — mudaria se o inventário passar a mostrá-lo |
| **Texto que o usuário lê** | **47** | **sim** |

Os 47, por origem:

| Arquivo | Qtd. | O que são |
| --- | --- | --- |
| `ApiPlanOperationJournalCheckpoints.cs` | 24 | recusas de transição ilegal (`Só um envelope Prepared/Pending pode ser promovido a Active. Estado atual: …`). Chegam à tela **coladas** ao prefixo que já é trilíngue, tanto pelo executor (`A transição autorizada não é válida no estado revalidado: `) quanto pela sessão |
| `ApiPlanOperationJournalSession.cs` | 12 | checkpoint não gravado, transição inválida, falha ao gravar, confirmação impossível — saem na Output e viram `BlockDetail` no diálogo |
| `ApiPlanGeneratedApiRemover.cs` | 8 | os `Remocao bloqueada: …` do preflight de metadata, ainda em ASCII sem acento, de uma leva anterior à F3 |
| `ApiPlanRecovery.cs` | 1 | a linha de cabeçalho do relatório de recuperação (`Operação {0} sobre '{1}': estado {2}/{3}, …`) |
| `ApiPlanRecoveryServices.cs` | 1 | `A KB não tem diário de operação: nenhuma operação desta ferramenta ficou pendente aqui.` |
| `ApiPlanOperationJournalValidator.cs` | 1 | `plan.plannedApiGuid é obrigatório em Apply e Sync a partir do estágio {0}.` — escapou da 8.2.3 porque só a irmã dela, a do `contractHash`, tinha sido cadastrada |

**O padrão que explica os 24 maiores.** As recusas de transição não nascem como mensagem de
tela: nascem como `InvalidOperationException` de um método de checkpoint, e só viram tela porque
alguém as concatena depois. Uma frase que atravessa uma exceção até o diálogo é exatamente o tipo
de texto que uma varredura por arquivo de UI não encontra — foi por isso que a P7 não a viu, e
por isso a sonda acima vale mais que uma nova leitura atenta.

**Decisão a tomar antes de traduzir:** parte desses 24 termina em `Estado atual: ` seguido de um
par de enums, e parte é frase inteira. Cadastrar o prefixo de cada uma é mecânico; o que merece
uma decisão é se `Estado atual: ` deve virar um único fragmento compartilhado — provavelmente sim,
pela mesma razão que ` em estado ` foi unificado na 8.2.2.

#### 8.2.5 Dívida fechada — e a sonda que precisou ser refeita

A 8.2.4 mediu 47 frases que o usuário lê e que ainda saíam em português, e as deixou como dívida.
Perguntado por que elas não podiam ser traduzidas como as outras, a resposta honesta foi: **podem,
e não havia impedimento nenhum** — era trabalho pendente, não limitação. Fechado na mesma data.

**Noventa e três entradas novas no catálogo**, porque quase toda frase concatenada precisa de duas.

**O que exigiu decisão, e não só digitação:**

- **A pré-condição `Active/Running` entrou inteira, uma frase por ação.** A tentação era cadastrar
  o prefixo `Para ` e o sufixo ` o envelope precisa estar Active/Running. Estado atual: `. Mas
  `Para ` tem cinco caracteres e recortaria qualquer outra frase do catálogo que o contivesse. Seis
  frases inteiras custam cinco entradas a mais e não têm esse risco.
- **Os nomes de transição entraram com as aspas simples que os cercam.** `abandono` solto
  recortaria `o abandono mantém operationState=Completed`, `somente um envelope Prepared pode ser
  abandonado` e mais três frases já cadastradas. `'abandono'` — com aspas — só casa onde a
  transição é nomeada. Há uma asserção no gate exatamente para isso: a frase que fala de abandono
  tem de sobreviver intacta.
- **`Operação ` obrigou a traduzir o aborto do usuário.** O cabeçalho do relatório de recuperação
  começa assim, e o mesmo prefixo aparece em `Operação abortada pelo usuário…`, que nunca estivera
  no catálogo. Cadastrar um sem o outro deixaria o aborto meio em inglês. Entraram juntos.
- **`: esperado ` é compartilhado por quatro mensagens.** Mesma regra: as três vizinhas — metadata
  de remoção incompatível, `schemaVersion` das preferências, `plan.planKind` — entraram na mesma
  rodada, cada uma com asserção que compara a frase **inteira** com `-ceq`.

**A sonda estava errada, e isso é o achado desta rodada.** A da 8.2.4 só considerava um literal se
ele tivesse acento ou uma palavra-marcador em português. As mensagens do preflight da remoção são
de uma leva anterior à F3 e estão escritas em **ASCII sem acento** — `nao e proprio da extensao`,
`API Object ambiguo` —, então passaram invisíveis. Uma segunda sonda, sem esse filtro e capaz de
partir o literal também nos buracos de interpolação do C# (`{nome}`, não só `{0}`), achou sete
causas de bloqueio da remoção, quatro alvos ausentes antes do `Delete()`, a falha de abertura do
diário e duas regras de schema. **Heurística de idioma por acento não encontra texto antigo** — e
a primeira sonda tinha produzido um número que eu publiquei como medição.

**O que resta, agora classificado e estável:**

| Classe | Chega ao usuário? |
| --- | --- |
| Contrato de programador (`ArgumentException`, asserção interna) | não — só dispara com chamador errado |
| Rótulos `Chave=valor` das linhas de diagnóstico da Output | a prosa em volta está traduzida; as chaves são identificadores |
| `Detail` de observação do inventário | não é renderizado em lugar nenhum hoje |
| `abandonment.reason` e `authorizedBy` | são conteúdo gravado no diário, não tela |

**Rede no gate:** trinta e quatro asserções novas. As das causas de remoção comparam a frase inteira
com `-ceq`, não `Contains`, porque meia tradução passaria por `Contains` sem reclamar.

#### 8.2.6 O catálogo corrompia a si mesmo — e o gate estava verde

As quatro rodadas anteriores perguntaram «esta frase está cadastrada?». Faltava a pergunta
seguinte: **«o que está cadastrado é o que sai?»**. Medido em 2026-09-15, com o catálogo já em 664
entradas: **não**, em oito frases.

**O mecanismo.** A substituição é por substring, sequencial, na ordem em que as frases foram
escritas. Uma entrada curta aplicada antes de uma longa que a contenha recorta o meio da longa:
quando a longa chega, o texto já mudou e ela não casa mais. Sobra meia frase em cada idioma.

| O que estava cadastrado | O que saía |
|---|---|
| `Arquivo de metadata: ` → `Metadata file: ` | `Arquivo metadata: ` |
| `Ainda sem ler ou gravar File de metadata.` → `Still without reading or writing the metadata File.` | `Ainda sem ler ou gravar File metadata.` |
| `Wizard Passo 2 concluido em memoria:` → `Wizard Step 2 completed in memory:` | `Wizard Passo 2 concluido in memory:` |

Quatro entradas curtas precediam entradas que as contêm — ` de metadata`, ` em memoria:`,
`API Object:` e `compatível` —, mas só duas produziam inconsistência observável. **A relação de
contenção não é o defeito; o defeito é o resultado divergir do cadastrado.** Por isso o gate testa
a invariante, não a estrutura.

**É o pior modo de falha possível para texto.** Não quebra, não lança, não avisa, e produz frase
plausível. Um leitor espanhol vê `Arquivo de metadatos:` e não tem como saber se é defeito ou
termo que a ferramenta deixou em português de propósito. O defeito não chega como reclamação —
chega como impressão de produto mal-acabado.

**Publicado.** Verificado contra a tag, não de memória: extraí
`ExtensionOutputLocalization.cs` de `v0.1.0-alpha.7` e rodei a mesma sonda. Aquele catálogo tinha
404 entradas e **três frases inconsistentes**, das quais duas são visíveis ao usuário —
`Wizard Passo 2 concluido em memoria:` e a irmã do Passo 3 saíam meio em inglês para quem usasse a
IDE em inglês. O grupo da metadata é posterior à tag: nasceu e morreu dentro deste bloco.

**A correção é estrutural, não caso a caso.** Reordenar à mão resolveria as oito e manteria a
fragilidade: cada frase nova cadastrada pode quebrar uma antiga, dependendo só de onde cair na
lista. `Translate` passou a iterar uma ordem derivada — **por comprimento de `Source`
decrescente** —, o que elimina a classe inteira: se A é substring de B, então A é mais curta, logo
B é aplicada primeiro e casa. O array literal continua sendo a fonte editável; a ordenação é
`OrderByDescending`, que é estável, então entradas de mesmo comprimento preservam a ordem de
escrita.

**Medido antes e depois, não deduzido.** Capturei a tradução de todos os 664 `Source` nos dois
idiomas antes da mudança e de novo depois. O diff tem **exatamente quinze linhas**, todas
correções — nenhuma outra frase mudou de comportamento. As 216 asserções do gate irmão
continuaram passando.

**O gate novo: `tests.outputLocalizationSelfConsistency`.** Para cada entrada, `Translate(Source)`
tem de reproduzir a tradução cadastrada, em espanhol e inglês, e o português tem de sair intacto.
Recusa também `Source` duplicado. Verificado por mutação: revertendo a ordenação, ele acusa
quatorze ocorrências.

**Uma armadilha que custou uma medição errada.** A primeira contagem de duplicatas deu catorze,
onze com tradução divergente. Era falso: `Dictionary` do PowerShell criado como `@{}` é
**case-insensitive**, e o catálogo cadastra pares de capitalização de propósito — `Nenhum ApiPlan
foi criado.` e `nenhum ApiPlan foi criado.` são entradas distintas e ambas legítimas, uma para
início de frase e outra para o meio. Com comparador ordinal, as duplicatas reais eram **quatro**.
As quatro foram removidas — eram inertes, e a remoção não mudou saída nenhuma, o que o diff do
corpus confirma. O gate usa `[StringComparer]::Ordinal` e o teste diz por quê.

**O que isto não resolve.** A substituição sequencial ainda permite cascata: a saída em espanhol
de uma entrada pode conter o `Source` de outra e ser reescrita. Um caso existe hoje —
`Metadata File:` produzia `Archivo de metadatos:` em vez do `Archivo de metadata:` cadastrado, e o
valor produzido é o melhor dos dois. A entrada passou a declarar o que faz, e o gate agora falha
se aparecer outra. É vigilância, não imunidade.

#### 8.2.7 A hipótese de custo do catálogo, medida e descartada

O catálogo passou de 404 para 660 entradas nesta frente, e `Translate` roda em toda linha de
Output. A pergunta é inevitável: isso encareceu o Apply?

Medido em 2026-09-15, chamando o método sobre um corpus de seis linhas reais de Output, três
rodadas de 12.000 chamadas cada, com aquecimento antes:

| Caminho | Custo por chamada |
|---|---|
| espanhol ou inglês, catálogo inteiro | **28 a 34 µs** |
| português do Brasil | **6 µs** — `Translate` devolve a mensagem antes do laço |

Mesmo mil linhas de Output custariam ~30 ms num Apply de 44 s. E numa KB em português, que é o
caso corrente, o laço nem roda. **A hipótese não sobrevive**, e fica registrada medida para que
ninguém a levante de novo por intuição.

Isso não dispensa o item 7a: o que ele mede é o custo dos `File.Save()` do diário, que é outra
coisa e é o que o orçamento de 4.4 governa.

## 9. Cenários restantes

| # | Cenário | Estado |
|---|---|---|
| 1 | Remoção completa (fila nova) | **passou** — seção 2 |
| 2 | Alvo previsto ausente antes do `Delete()` | **passou** — seção 4 |
| 3 | Recuperação sobre o envelope `Partial`: encerrar o registro | **passou** — seção 5 |
| 4 | Devolver a KB ao normal | **passou** — seção 6 |
| 5 | Abortar um Apply no meio; oferta proativa; recuperação | **passou** — seção 7 |
| 6 | Envelope `Prepared` e abandono | **reformulado** — não é alcançável pela interface; ver seção 8 |
| 7 | Interromper uma remoção no meio e retomar a fila — contra os critérios 9 a 12 da seção 10 do plano | **passou** — seção 11 |
| 8 | Remoção de API legado, com metadata válida e com metadata insuficiente | **passou** — seção 12 |
| 9 | Acréscimo de tempo do diário na KB grande — **duas** medições: **9a** Apply e **9b** remoção retomável, contra a tabela derivada de 4.4 | **passou** — seções 13 (9a) e 14 (9b) |

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

## 11. Cenário 7 — interromper uma remoção e retomar a fila

**Passou**, em 2026-09-15, com a DLL do commit `b788cf4`. É o único caminho que exercita
`ContinueRemovePass`, e nenhuma passada de retomada tinha sido vista em campo até aqui.

KB `wsEducacaoSpTeste`, Transaction `Teste`, os mesmos 25 objetos próprios da seção 1. O aborto é
testado **antes de cada alvo** da fila (`ApiPlanGeneratedApiRemover.Remove`), então o clique em
Abortar durante a remoção interrompe entre dois `Delete()`, que é exatamente a condição pedida.

### 11.1 As duas metades da execução

| Medida | Remoção abortada | Retomada pela recuperação |
|---|---|---|
| Recibos | 11 — API Object, as 5 Procedures e 5 SDTs, na ordem canônica da fila | 14 — os 13 SDTs restantes e o File de metadata |
| Estado terminal | `Partial`/`RemovalPartial`, `blockReason=UserAborted` | `Removed`/`Removed` |
| Inventário | 30 | 30, o mesmo |
| Checkpoints / custo | 3 / 258 ms | 3 / 73 ms |
| Relatório | `Resultado='Interrupted'`, `Removidos=11`, `Bloqueados=1` | `Resultado='Success'`, `Removidos=14`, `Bloqueados=0`, 2,2 s |

O envelope: `OperationId='9f55fc57-e601-467f-8f74-25bcdbb6a3aa'`,
`ApplicationId='de24d342-7c2d-4024-90fd-c6e4c3e4054c'`, `FileId=88`, `Durability=Confirmed` nos
dois momentos.

**Os quatro critérios da retomada (seção 10 do plano, itens 9 a 12), verificados na Output:**

| # | Critério | Como se comprovou |
|---|---|---|
| 9 | mesmo `operationId` e `applicationId`; não abre envelope novo | os dois GUIDs acima são idênticos antes e depois; o envelope reabriu como `Active` no mesmo `FileId=88` |
| 10 | nenhum alvo fora do inventário original | `Inventário=30` constante nos três checkpoints da retomada; os 14 removidos são exatamente os `Delete` que restavam |
| 11 | alvo já apagado não conta como falha nem dispara `TargetAbsentBeforeDelete` | os 11 `Absent` foram lidos, publicados no diagnóstico e **não** enfileirados: `Passadas=1/14`, `Pendentes=0`, `BlockReason=<nenhum>` |
| 12 | terminal `Removed`, não um segundo `Partial` | `Removed/Removed`, com `Outcome=Removed` na nota de encerramento da fila |

Três coisas que a execução mostrou sem terem sido pedidas:

- **`P=1`.** Uma passada bastou para os 14 alvos, contra um orçamento de `max(1, 14)`. A pergunta
  que a seção 4.4 deixou aberta — se `P` cresce com dependências — teve, nesta KB e com quatro
  níveis de subníveis, a resposta mais barata possível. Não generaliza para a KB grande, que é o
  cenário 9;
- **o diário custou ~24 ms por gravação** na retomada, bem abaixo do teto de 60 ms declarado no
  item 7 da seção 9. É medição de KB pequena e não substitui o cenário 9, que mede na grande;
- **o diálogo de recuperação diz a verdade sobre o que vai fazer**: «14 alvos previstos continuam
  na KB» — 13 SDTs mais a metadata, com o Folder de fora por ser `Preserve` —, e lista cada item
  com o previsto ao lado do observado. Os 3 SDTs compartilhados, o Folder `TesteOpenApi` e a
  Transaction aparecem como `Preserve; Present`, fora da fila destrutiva.

Ao fim, o Folder `TesteOpenApi` ficou na KB, vazio: ele é reutilizado (`FolderWasCreated=False`) e
o contrato manda preservá-lo.

### 11.2 Dois defeitos de texto que o cenário produziu

Nenhum dos dois invalida o cenário — o contrato se cumpriu inteiro —, mas os dois estão no
caminho que acabou de ser percorrido, e o segundo empurra para a ação errada.

| Onde | O que dizia | O que passou a dizer |
|---|---|---|
| Título do relatório final da recuperação | «API gerada com sucesso.» logo depois de uma remoção retomada | «Operação recuperada com sucesso.» |
| Aviso do aborto (`ApiPlanBusyProgress`) e aviso de remoção parcial (`Package.cs`, os dois caminhos) | «Use Remover / Wizard / Sync para reparar» e «reaplique pelo Wizard ou repita a remoção» | o comando `Recuperar operação interrompida`, dizendo que retomar usa o mesmo registro e que **repetir a remoção do zero bloqueia** |

A causa do primeiro é localizada: `ApiPlanApplicationFinalReport.ResolveVerb` conhecia os verbos
de `Remover` e `Sincronizar` e caía no default «API gerada» para qualquer outro — `Recuperar` não
existia quando aquele método foi escrito. `ResolveInterruptedHeadline` tinha o mesmo buraco, e
anunciaria «Geracao interrompida.» se a própria retomada fosse abortada.

O segundo é uma orientação que **envelheceu com o contrato**: «repita a remoção» era verdade
enquanto a remoção era idempotente por omissão, e deixou de ser na P4 — hoje a segunda remoção
bloqueia em `TargetAbsentBeforeDelete` (seção 4.3 do plano). O texto mandava fazer exatamente o
que o contrato novo recusa.

De passagem, o aviso de remoção parcial estava escrito em ASCII sem acento e **não tinha entrada
no catálogo**: saía em português em qualquer idioma. É a mesma classe de resíduo que a segunda
sonda da seção 8.2.5 encontrou, e a reescrita o traz para o catálogo trilíngue junto com o resto.

Asserções novas em `tests.extensionOutputLocalization` (aviso partido em prefixo e sufixo, para a
contagem no meio; título da recuperação nos três idiomas) e em
`tests.generatedApiRemovalResilience` (os dois avisos de remoção parcial apontam a recuperação).

## 12. Cenário 8 — remoção de API legado

**Passou**, em 2026-09-15, com a DLL do commit `9ab74bb`, em três execuções sobre a mesma geração
da `Teste`: duas de metadata insuficiente, que bloqueiam sem apagar nada, e uma de metadata legada
válida, que remove. A ordem não é detalhe — as duas primeiras não consomem a API, então uma
geração serve às três.

### 12.1 Como o legado foi fabricado, e por que isso é legítimo

Não foi gerado por uma DLL antiga: o JSON exportado da metadata V3 foi editado, produzindo três
variantes. A alternativa — instalar a `0.1.0-alpha.7`, gerar, reinstalar a DLL atual, repetir —
pagaria três instalações para exercitar o **mesmo consumidor**, que é o caminho de leitura de
metadata sem `ownership.applicationId`. Para ele, quem escreveu o arquivo é indiferente.

O que autoriza a edição à mão é uma verificação de código, não conveniência: a remoção **não**
valida o `fingerprint` do topo da metadata — quem o valida é o caminho do Wizard
(`ApiPlanApiObjectWriter`). O plano de remoção exige `schemaVersion` suportada, os campos de
`ownership` e o inventário de `objects`, e revalida cada alvo na KB. Por isso o roteiro proibiu
abrir o Wizard entre o import e o Remover: ali o fingerprint desatualizado bloquearia, e o
bloqueio não teria nada a ver com o cenário.

| Variante | O que foi alterado | Resultado esperado |
|---|---|---|
| 8-1 | V2, sem `applicationId`, **sem `ownership.apiGuid`** | bloqueio |
| 8-2 | V2, sem `applicationId`, sem `objects.sdts.own`, childLevel `TesteItem` **sem `levelName`** | bloqueio |
| 8-3 | V2, sem `applicationId`, sem `objects.sdts.own`, levels íntegros | remoção com adoção tardia |

### 12.2 As duas recusas, e o que elas protegem

Nas duas, `Removidos=0`, `PersistenceReceipts=0`, `Tempo: 0 ms` e **nenhuma linha `B111/F3`** na
Output: o diário não chegou a abrir envelope. Isso é consequência da ordem da P4 — a intenção é
resolvida antes de o diário ser aberto —, e significa que metadata insuficiente **não deixa
rastro para limpar**.

- **8-1** parou em `ApiPlanGeneratedApiRemovalPlan.RequirePresent`, antes do diálogo de
  confirmação: sem `apiGuid` confirmado não há como identificar o API Object, e o plano é
  explícito em que nome ou Description isolados nunca autorizam exclusão;
- **8-2** parou em `ApiPlanGeneratedApiRemovalInventory.ResolveOwnSdtNames`. É a recusa mais
  valiosa das três: a metadata anunciava `levels` e, ao não conseguir lê-los, a remoção **não**
  caiu no inventário plano. Se tivesse caído, teria apagado cinco SDTs, deixado treze de subnível
  órfãos e levado junto a metadata que os descrevia — o estado mais difícil de reparar que esta
  ferramenta consegue produzir.

### 12.3 A remoção legada, e a adoção tardia

| Medida | Resultado |
|---|---|
| Inventário reconstruído | 18 SDTs a partir de `levels`, **nome a nome e na mesma ordem** do inventário explícito; `PlannedDeletes=25` |
| `applicationId` | `f9b9eb17-8e01-43dd-bca1-3c9b851043b3` — **novo**, diferente do `662c6e53…` que o Apply da mesma manhã gravou, porque o arquivo legado não traz o campo |
| Metadata legada | **não** regravada para ganhar o campo; saiu da KB como penúltima da fila |
| Desfecho | `Removed/Removed`, 25 confirmados, `Passadas=1/25`, `Pendentes=0`, `Preservados=5` |
| Custo | 4 checkpoints, 81 ms de diário em 5,5 s |

A reconstrução pelos levels reproduzir exatamente os nomes gerados é o que sustenta o critério de
aceite 6: a intenção **importada** da metadata legada descreve o mesmo conjunto que a geração
criou. Um único nome divergente teria virado `TargetAbsentBeforeDelete` e encerrado a operação em
`Partial`.

O custo de 1,5% do tempo total não compara com o limiar de 1% do item 7 da seção 9: aquele limiar
é sobre a KB grande, onde o denominador é outro. Aqui o diário levou 81 ms porque a operação
inteira levou 5,5 s.

### 12.4 O que o cenário produziu: recusas mudas e sem tradução

As três recusas do plano de remoção — campo ausente, campo incompatível e `schemaVersion` fora de
V1/V2/V3 (**lista aceita na data desta validação**; desde o `B123` a mensagem cita V1–V4) — bloqueavam **sem dizer o que fazer**, ao contrário da recusa da metadata hierárquica
ilegível, que já terminava com «Corrija a metadata ou regenere a API». É o mesmo critério da
seção 10, aplicado onde ainda não estava. As três passaram a terminar com uma saída única, numa
constante compartilhada; o nome do campo continua no começo, porque quem edita metadata legada
precisa dele.

Ao cadastrar isso, apareceu o que a leitura das mensagens não mostrava: **nenhuma das quatro
estava no catálogo de tradução** — nem a que eu tinha elogiado por ter saída —, junto com a causa
interna `levels.levelName é obrigatório.`, que chega ao relatório final. Saíam em português em
qualquer KB. Sete entradas novas, com asserções no gate.

É o quarto resíduo da mesma classe nesta frente: mensagem de caminho raro que ninguém lê até
precisar. A sonda de 8.2.5 não as pegou porque elas nascem como `InvalidOperationException` num
validador, não como texto de tela.

### 12.5 Achado de contrato — o Folder vazio é permanente

Depois da remoção, o Folder `TesteOpenApi` ficou na KB, vazio. **Na data desta validação, era o
comportamento correto**: a fila só apagava Folder próprio criado pela operação corrente
(`wasCreated=true`), e o diálogo avisava «reutilizado; nunca apagar». O efeito era de mão única —
assim que um Folder sobrevivia a uma remoção, toda geração seguinte o reencontrava como
reutilizado, e **nenhuma remoção futura o apagaria**, porque `wasCreated` descreve a operação
corrente, não quem criou o Folder.

A saída barata — apagar Folder vazio que carregue a Description canônica — foi **recusada pelo
usuário na mesma data**, por ser menos segura: Description isolada nunca autorizou exclusão neste
projeto, e um Folder homônimo de terceiro com a mesma marca seria apagado. A saída aprovada é a
cara, e virou o item de backlog **`B123`**: a metadata registrar a posse histórica do Folder.

**Remissão — 2026-09-20 (`B123` fechado).** O absoluto «nenhuma remoção futura» e o «até lá» abaixo
deixaram de ser o contrato vigente. Com `GOAB_API_METADATA_B060_V4`, `ownedByThisApi` + `guid`
autorizam apagar Folder próprio reencontrado se ficar vazio; homônimo com GUID divergente não
herda posse. Plano: `Docs/Implementation/2026-09-20-B123-PLANO-POSSE-HISTORICA-FOLDER.md`.

Até o fechamento do `B123`, o resíduo era um Folder vazio, inofensivo, que o usuário apagava à mão
se quisesse.

## 13. Cenário 9a — o acréscimo do diário no Apply da KB grande

**Aprovado**, em 2026-09-15, na `Empresa` da `FabricaBrasil18Test` — 47 SDTs e 4 Procedures
reencontrados, 53 objetos atualizados, envelope de 6.534 bytes com 10 recibos. É a mesma condição
da medição da P2, reproduzida de propósito: Apply **de reencontro**, sem alterar nada entre as
execuções.

| Execução | Diário | Por gravação | Apply | Peso |
|---|---|---|---|---|
| 1 | 234 ms | 58,5 ms | 46.389 ms | 0,50% |
| 2 | 132 ms | 33,0 ms | 43.331 ms | 0,30% |
| 3 | 106 ms | 26,5 ms | 47.788 ms | 0,22% |
| **Mediana** | **132 ms** | **33,0 ms** | 46.389 ms | **0,30%** |

Contra o critério do item 7 da seção 9 do plano, declarado antes de medir: mediana de **33,0 ms**
por gravação contra teto de **60** (folga de 45%), e **0,30%** do tempo total contra limiar de
**1%** (folga de 3,3×). O orçamento da seção 4.4 fica sustentado por medição feita com a DLL
corrente, que era exatamente o que o item 7a exigia ao mandar refazer a medição da P2.

### 13.1 O que a série mostra e uma captura não mostraria

A dispersão foi **maior** que a registrada na P2: entre 58,5 e 26,5 ms por gravação, a primeira
121% acima da última, contra os ±17% que a P2 observou. O critério de três execuções foi escrito
com base nos ±17%; o campo mostrou que a margem real é maior, o que reforça a regra em vez de
enfraquecê-la.

**A primeira execução é o outlier, não a última** — 58,5 → 33,0 → 26,5, monotônico. O padrão é
compatível com custo de primeira gravação depois de abrir a KB, seja cache de disco ou da própria
IDE. É **hipótese, não causa medida**: ninguém instrumentou o `Save()` para separar I/O de
overhead da IDE, e a série tem três pontos.

O valor prático da regra fica explícito aqui: parando na primeira, o número publicado seria
**58,5 ms**, a 2,5% do teto, e a conclusão provável seria mexer no teto ou na política de
checkpoints — uma decisão de projeto tomada sobre ruído. As outras duas execuções custaram cerca
de um minuto e meio de máquina.

O peso no Apply também vale registrar por outro motivo: o Apply da KB grande leva ~45 s, e o
diário responde por menos de meio décimo desse tempo. O que domina a operação continua sendo a
gravação dos objetos de negócio, e é lá que qualquer trabalho futuro de desempenho tem retorno.

## 14. Cenário 9b — o acréscimo do diário na remoção da KB grande

**Aprovado**, em 2026-09-15, na mesma sessão e na mesma `Empresa` — 50 alvos na fila (1 API
Object, 4 Procedures, 44 SDTs e o File de metadata), 55 no inventário, 5 preservados. Nunca havia
sido medido: o orçamento da remoção existia apenas como derivação do custo do Apply.

Cada remoção consome a API, então a série exigiu intercalar Applies de criação completa: remover,
repor, remover, repor, remover, repor.

| Execução | Diário | Por gravação | Remoção | Peso | `P` |
|---|---|---|---|---|---|
| 1 | 141 ms | 35,3 ms | 34.625 ms | 0,41% | 1 |
| 2 | 131 ms | 32,8 ms | 34.713 ms | 0,38% | 1 |
| 3 | 123 ms | 30,8 ms | 35.324 ms | 0,35% | 1 |
| **Mediana** | **131 ms** | **32,8 ms** | 34.713 ms | **0,38%** | **1** |

Contra o critério do item 7 da seção 9: **32,8 ms** por gravação contra teto de 60, e **0,38%** do
tempo total contra limiar de 1%. O orçamento derivado da seção 4.4 previa `(3 + 1) × ~45 ms ≈ 180
ms` para `P=1`; o medido foi 131 ms de mediana — a derivação **superestima em 37%**, que é a
direção segura para um orçamento.

### 14.1 `P` não cresce com as dependências — a pergunta de 4.4, respondida

A seção 4.4 declarou em voz alta o que não sabia: «o que a medição precisa responder é se `P` se
mantém baixo quando há muitas dependências — uma remoção que precise de dezenas de passadas custa
pouco em I/O de diário e muito em tentativas de `Delete()`, e é a segunda parte que dominaria o
tempo».

**`P=1` nas três execuções**, com 50 alvos e 13 subníveis, `Pendentes=0` ao fim da primeira
passada, contra um orçamento de 50 passadas. Somado ao `P=1` da `Teste` (25 alvos, 4 níveis), o
que se observa é que a ordem canônica da fila — API Object, Procedures, SDTs em ordem de
dependência, metadata, Folder — entrega as dependências já resolvidas. O requeue por
`StillPresent` existe para o caso que não aconteceu em nenhuma das quatro remoções medidas.

Isso não prova que `P` nunca cresce: prova que, nas duas formas de hierarquia exercitadas, a
ordem basta. O mecanismo de passadas continua sendo a defesa para o caso contrário.

### 14.2 O tempo da remoção está nas varreduras, não no diário nem no `Delete()`

O achado maior da medição não é sobre o diário. Das três execuções, com variação desprezível:

| Fase | Tempo | Fração |
|---|---|---|
| Operação inteira | ~34,9 s | 100% |
| Fila de remoção | ~32,4 s | 93% |
| **Varreduras (`Scans=149`)** | **~24,4 s** | **70%** |
| Diário | ~131 ms | 0,38% |

São três varreduras por alvo — localizar, revalidar antes do `Delete()`, confirmar depois —, e nos
44 SDTs elas custam ~18,4 s sozinhas, cerca de 134 ms por varredura de SDT. As três são exigência
de contrato: a classificação vem **sempre** da releitura do alvo, nunca do texto da exceção da
IDE, e é isso que impede um relatório de afirmar o que não mediu.

Registrado aqui como insumo para o residual do `B082`, não como defeito desta frente: se algum dia
houver trabalho de desempenho na remoção, é nas varreduras que está o tempo. O diário, que era a
preocupação da seção 11, responde por menos de meio por cento.

### 14.3 Os Applies de reposição, de graça

Não faziam parte do roteiro, mas são a única medição de Apply de **criação** na KB grande com esta
DLL — os três do 9a são de reencontro:

| Apply de criação | Diário | Operação | Peso |
|---|---|---|---|
| 1ª reposição | 178 ms | 75.834 ms | 0,23% |
| 2ª reposição | 104 ms | 72.327 ms | 0,14% |

A operação é bem mais longa que a de reencontro (~45 s) e o peso relativo do diário cai. Nenhum
dos dois entra na mediana do 9a, que é de reencontro por definição; ficam como contexto.

### 14.4 A remoção é reprodutível; o diário é que varia

34.625, 34.713 e 35.324 ms — 2% entre a maior e a menor. As varreduras: 24.278, 24.433 e 24.442
ms. A operação que hospeda o diário é notavelmente estável nesta KB, e toda a dispersão observada
no 9a (121% entre execuções) está no custo de gravar o File, não no trabalho em volta.

Com o 9b, os nove cenários da seção 9 do plano estão exercidos: oito passaram e um foi
reformulado pelo que mediu.

## 15. Os oito critérios de aceite comuns, cruzados com evidência

**Escrito em 2026-09-15, depois do encerramento**, porque uma conferência externa notou que a F3
estava sendo fechada contra a **seção 9** do plano — que diz o que exercitar — e não contra a
**seção 10**, que diz o que precisa ser verdade no fim. Os critérios 9 a 12, da retomada, têm
conferência registrada na seção 11; os oito comuns não tinham. A observação procede, e esta
tabela existe para que o fecho da fase se apoie no contrato dela.

| # | Critério | Evidência | Onde |
|---|---|---|---|
| 1 | a intenção é registrada e confirmada antes da primeira gravação do pipeline | Apply e Sync abrem o diário com `Prepared` e `Active` confirmados por releitura **antes** de qualquer objeto de negócio; na remoção, o inventário completo é gravado antes do primeiro `Delete()` — `PlannedDeletes=50`, `Inventário=55`, e só então a fila corre | P2 §6.1–6.4; seções 2, 11 e 12.3 |
| 2 | estado físico e estágio lógico são reportados separadamente | todo checkpoint publica o par: `Running/RemovalInProgress`, `Partial/RemovalPartial`, `Completed/Discarded`, `Removed/Removed`. O par divergente é informativo por desenho — `Completed/Discarded` diz que a operação terminou fisicamente e que o registro foi encerrado por decisão | seções 2 a 14, em toda Output |
| 3 | resultado indeterminado bloqueia continuação automática | **coberto offline, não observado em campo.** `OutcomeUnknown` bloqueia sem retry na fila, a recuperação responde `Block` com `UnreconciledOutcome`, e o encerramento de registro recusa o caso. Nenhuma das operações medidas produziu indeterminação: exigiria releitura ilegível ou ambígua da IDE, que não se provoca por clique | `tests.removalQueue`, `tests.operationJournalRecovery`; P4–P7 §3.1 |
| 4 | reaplicação não cria API duplicado nem sobrescreve intenção parcial em silêncio | o Apply seguinte a um aborto **bloqueou** em vez de sobrescrever, com a oferta proativa (cenário 5); e a reaplicação sobre KB limpa reencontrou tudo — `Criados=2`, `Atualizados=26`, `PlannedContractHash` idêntico — sem segundo API Object | seções 6 e 7 |
| 5 | nenhuma exclusão ocorre sem intenção de remoção confirmada | as duas metadatas insuficientes pararam no `Preview`, antes do diálogo: `Removidos=0`, `PersistenceReceipts=0`, **sem abrir envelope**. E a ordem intenção → diário → fila é verificada por gate | seções 12.2; `tests.generatedApiRemovalPreflight` |
| 6 | remoção de legado importa a intenção da metadata ou bloqueia antes do primeiro `Delete()` | as duas saídas exercidas no mesmo dia: metadata V2 legada válida importou a intenção e removeu os 25, com adoção tardia; sem `apiGuid` e com `levels` ilegível, bloqueou | seção 12 inteira |
| 7 | nome, Description canônica ou prefixo nunca autorizam exclusão sozinhos | a recusa por `ownership.apiGuid` ausente é exatamente este critério em campo: o File tinha nome, Description canônica e todos os demais campos, e ainda assim a remoção parou. A posse por Description continua sendo condição necessária, nunca suficiente — foi também o argumento que derrubou a saída barata do Folder e gerou o `B123` | seções 12.2 e 12.5 |
| 8 | ciclo de `operationId`/`applicationId` pela matriz de 4.1.1, e fingerprint V3 cobrindo `ownership.applicationId` | cruzamento `journal.applicationId` = `metadata.ownership.applicationId` medido na P2; a retomada preservou os dois identificadores (critério 9); e a adoção tardia gerou `applicationId` novo **sem** regravar a metadata legada, que é a linha da matriz para legado sem o campo | P2 §6.2; seções 11.1 e 12.3 |

**Resultado: oito de oito atendidos, com uma ressalva declarada** — o critério 3 está provado
offline e não foi observado em campo, porque indeterminação não se produz por clique. Nenhum
critério ficou sem evidência, e nenhum foi verificado apenas por leitura de código: sete têm
observação de campo com números nesta bateria ou na P2.
