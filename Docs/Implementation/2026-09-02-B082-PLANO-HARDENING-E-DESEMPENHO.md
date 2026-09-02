# B082 — Plano de hardening e desempenho (2026-09-02)

## Propósito e relação com documentos anteriores

Este plano substitui a série de manuscritos `B082 v1`–`v20`, mantida em `Temp/revisao-por-pares/`
e nunca versionada. Aquela série cresceu ao longo de vinte rodadas de revisão por leitura de
código, sem nenhuma medição, até 488 linhas de texto normativo.

Em 2026-09-02 a extensão foi instrumentada e medida em uma KB real. A medição inverteu a
prioridade da frente, cortou cerca de um terço do que estava planejado e corrigiu três decisões
que a `v20` dava como fechadas. Este documento registra o que ficou de pé, com o número que
sustenta cada escolha.

**Revisão do mesmo dia.** A primeira redação deste plano foi submetida a revisão externa e
corrigida em três pontos, todos na mesma direção: ela havia enfraquecido garantias que já
existiam no código. A ordem terminal do Remover e as confirmações individuais pós-`Delete`
voltaram ao comportamento atual, e a falta de verificação de contêiner no `MaybeDeleteFolder`,
antes descartada com base num fato errado, virou item da Etapa 2. Onde a `v20` errava por excesso
de aparato, a primeira redação errou por remoção. As reversões estão marcadas nas decisões 3 e 4
e na dor D10.

`Docs/Implementation/2026-08-31-B082-PLANO-UX-PROGRESSO.md` permanece válido como registro da
entrega do `0.1.0-alpha.7`. Deste plano, **revoga-se apenas o item 4 da sua seção «Fora da fila
operacional»**, que classificava o índice compartilhado incompleto como resíduo de performance
de prioridade P2. A medição mostra que é a maior fatia isolada de custo da extensão inteira, e
ele passa a ser a primeira ação de código desta frente.

`Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md` não muda por este documento: `B108` continua sendo a
próxima frente do checkpoint.

## Como foi medido

Instrumentação nos commits `7fd4a0d` (Remover) e `c4d62ee` (Apply/Sync), com
`ApiPlanScanTelemetry` e `ApiPlanScanProbe`. Ambos apenas contam e cronometram: sem escopo ativo
o delegate executa igual ao código não instrumentado. O curto-circuito de `&&` em
`IsFolderEmpty` foi preservado, para não medir varredura que não chega a executar.

KB `Fabrica Brasil Test`, 196 transações. Três transações escolhidas para isolar variáveis
distintas: `Setor` (10 KB, 1 nível), `Empresa` (82 KB, 14 níveis) e `DocumentoFiscal` (495 KB,
1 nível, 171 campos).

**Toda medição registrada aqui vale para as DLLs desses dois commits.** Qualquer alteração
posterior nos writers ou no removedor invalida a comparação; a aferição do ganho exige medir de
novo, nas mesmas três transações.

## As dores, classificadas

### Destrutivas — comprovadas no código

**D1 — Nada impede uma segunda operação durante a escrita.** A janela de progresso é modeless e
chama `Application.DoEvents()`; a IDE segue respondendo a cliques de menu durante um Apply.
Nenhuma barreira existe. É o único caminho realista para corromper a KB.

**D2 — O que o usuário confirma não é o que é apagado.** No Remover, o Preview lê o metadata e
exibe a lista; após a confirmação, `Package.cs:980` chama `Remove(designModel, transaction,
session)`, que **relê o metadata e monta um plano novo**. São dois planos distintos. Sintoma
menor do mesmo defeito: o `PlannedDeletes` anunciado vem do primeiro plano e a fila executada
vem do segundo.

**D3 — Abortar durante a exclusão ainda apaga mais um objeto.** `ReportDelete` atualiza o
progresso — o que processa os eventos pendentes, inclusive o clique — e emenda direto no
`Delete()`, sem verificar o pedido de abort. Ele só é observado na volta seguinte do laço.

### De comunicação e UX — comprovadas

**D4 — O `DEMO.md:144-146` promete o que o Abortar não cumpre**, afirmando cancelamento "com
segurança antes de qualquer gravação". Abortar depois do primeiro `Save()` deixa a KB pela
metade. Atenuante medido: o *hint* da própria janela diz a verdade, então o usuário vê o texto
correto no momento da decisão. O mesmo trecho descreve a casca como modal, sendo modeless.

**D5 — O próprio Abortar abre uma janela de reentrada.** `ExtensionBusyProgressDialog.
OnAbortClicked` chama `Application.DoEvents()` dentro do clique.

**D6 — A casca de progresso fica viva atrás do relatório final**, em todos os caminhos de
Apply e Sync e no abort do Remover (`Package.cs:991`, dentro do `using`).

**D7 — O relatório comunica resultado por texto solto.** Folder preservado por não estar vazio
entra na lista de itens *apagados* como a string `Folder:{nome}:PreservedNonEmpty`.

**D12 — Diálogos abrem no monitor errado.** `PrototypeWizardDialog` usa `CenterParent`, que o
WinForms ignora quando o owner não é um `Control` — e `ResolveFinalReportOwner` devolve um
`NativeWindowHandle` sempre que `Form.ActiveForm` é nulo, o caso normal nesta IDE.
`ExtensionBusyProgressDialog` usa `CenterScreen`, que centraliza na tela **primária**, não na
tela onde a IDE está. Observado em uso; causa identificada por leitura.

### Desempenho — medidas

**D9 — Varredura de catálogo domina o tempo de todas as operações.** Detalhado abaixo.

**D8 — A abertura do Wizard escala mal:** 3,2 s (`Setor`), 5,3 s (`Empresa`), **11,8 s**
(`DocumentoFiscal`). Nesta última, 4,6 s lendo o contrato e 7,0 s montando a interface. Causa
distinta das demais: montagem de UI, não varredura.

**D11 — A IDE fica pouco responsiva durante a operação**, a ponto de a janela de progresso
resistir a ser arrastada. Sintoma de trabalho pesado na thread da UI, com `DoEvents()` só entre
operações.

### Estreita, mas real — comprovada no código

**D10 — O Remover não verifica o contêiner do Folder.**
`ApiPlanGeneratedApiRemover.MaybeDeleteFolder` exige cardinalidade única na KB inteira, descrição
canônica ou legada da extensão, e pasta vazia — mas **não** verifica se o Folder está no contêiner
esperado da Transaction. A regra de contêiner existe apenas em
`ApiPlanTransactionFolder.IsInExpectedContainer`, usada por `IsReusable` no fluxo de Apply.

O cenário exposto é estreito: exige exatamente um Folder com aquele nome em toda a KB, situado em
contêiner diferente do esperado, com a descrição canônica da extensão e vazio. É plausível com
transações homônimas em módulos distintos. A `v20` propunha um predicado normativo extenso e novo;
o necessário é bem menor — reaproveitar no Remover a regra de contêiner que o Apply já tem.

**Correção de rumo registrada:** a primeira versão deste plano classificou a D10 como hipotética,
afirmando que "o predicado atual já exige contêiner". Isso é falso para o Remover, e o erro foi
apontado por revisão externa em 2026-09-02. O corte estava justificado por um fato errado.

## Medições

### Custo de uma varredura completa de catálogo

| Tipo | Custo unitário |
|---|---|
| `Attribute.GetAll` | **~1300 ms** |
| `Procedure.GetAll` | ~500 ms |
| `SDT.GetAll` | ~125 ms |
| `Folder.GetAll` | ~56 ms |
| `Transaction.GetAll` | ~40 ms |
| `WikiFileKBObject.GetAll` | ~16 ms |
| `API.GetAll` | ~0 ms |

Atributos são a varredura mais cara — e a extensão **nunca os cria, altera ou apaga**.

### Apply

| | `Setor` | `Empresa` | `DocumentoFiscal` |
|---|---|---|---|
| Campos / Response | 6 / 13 | 102 / 162 | 171 / 510 |
| PK partes / filtros | 2 / 2 | 1 / 1 | 2 / 2 |
| Objetos criados | 12 | 51 | 12 |
| **Varreduras** | **97** | 155 | **97** |
| Tempo de varredura | 68,1 s | 52,2 s | 66,0 s |
| Trabalho real de escrita | 16,2 s | 109,3 s | 121,5 s |
| **Total** | 84,3 s | 161,5 s | 187,5 s |

`Setor` e `DocumentoFiscal` fazem o **mesmo** número de varreduras, apesar de uma ter 6 campos e
a outra 171. `bc-find-attribute` roda 22 vezes quando a PK tem 2 partes e há 2 filtros, e 11
vezes quando são 1 e 1. **O custo de varredura por Apply é praticamente constante, em torno de
60 s, e não escala com o tamanho da transação.**

O índice é criado **quatro vezes** por Apply em vez de uma, em todos os três casos: cerca de
6 s desperdiçados, também constantes. `Fase IndiceKb` e `Fase PreflightAgregado` medem quase o
mesmo tempo justamente porque o preflight refaz o índice inteiro.

### Remove

| | `Setor` | `Empresa` | `DocumentoFiscal` |
|---|---|---|---|
| Objetos | 12 | 51 | 12 |
| Varreduras | 49 | 205 | 49 |
| Tempo de varredura | 11,3 s (88%) | 33,6 s (82%) | 11,5 s (79%) |
| **Total** | 12,8 s | 41,2 s | 14,5 s |

Escala linearmente com o número de objetos: quatro varreduras por objeto — validação agregada,
localização, revalidação e confirmação pós-`Delete`.

### Fato colateral medido

`MetadataFile Parent='Root Module'` nas três transações. O metadata File **não** fica dentro do
Folder da Transaction, e por isso o `WikiFileKBObject.GetAll` de `IsFolderEmpty` nunca o conta.

Isso respondia ao gate que a `v20` exigia antes de inverter a ordem para Folder → File. Como a
inversão foi abandonada (decisão 4), o dado permanece apenas como observação: **três transações
de uma KB não provam o comportamento de toda instalação**, e a ordem atual não depende disso.

## Decisões fechadas

1. **O índice é construído uma vez por operação**, e passa a ser reaproveitado em dois níveis de
   risco deliberadamente distintos, que não devem ser confundidos nem entregues juntos:

   - **Nível A — não atravessa mutação.** Usar o mapa de atributos existente; criar o índice uma
     vez em vez de quatro; consumir o índice recém-criado nos preflights que rodam **antes de
     qualquer `Save()`**. Nada aqui depende de o índice continuar fiel após uma escrita. Risco
     praticamente nulo, e é onde está a maior parte do ganho do Apply.
   - **Nível B — atravessa mutação.** Manter o índice coerente enquanto a extensão cria e apaga,
     em vez de reconstruí-lo. É o que o Remove precisa, e exige contrato explícito: invariantes,
     momento da atualização em relação ao sucesso da mutação, comportamento após exceção,
     duplicidade e mudança de contêiner. **Não é "baixo risco" e não deve ser tratado como tal.**

   Revoga a regra da `v20` que proibia usar o índice depois da primeira exclusão. Base: a IDE não
   expõe edição de File; alterá-lo exige manipulá-lo fora da IDE e reimportar por um fluxo manual
   de vários cliques, incompatível com o intervalo de uma operação — e a guarda de operação única
   fecha o caminho pelo qual a própria extensão poderia interferir. **A guarda não cobre comandos
   nativos da IDE durante o `DoEvents()`**, e é por isso que o Nível B precisa de contrato em vez
   de confiança.
2. **Os writers passam a usar o mapa de atributos que já existe.** `ApiPlanKbObjectNameIndex` já
   constrói o mapa (`GxAttribute` é alias de `Artech.Genexus.Common.Objects.Attribute`) e expõe
   `FindAttributes` e `TryGetSingleAttribute`, sem nenhum consumidor.
3. **As confirmações pós-`Delete` permanecem individuais.** Uma versão anterior deste plano as
   agregava ao fim; a decisão foi revertida após revisão externa. Refeita a conta com os dados
   medidos, elas custam 18,8% do Remove em `Empresa`, 21,4% em `Setor` e 18,6% em
   `DocumentoFiscal`, enquanto as outras três varreduras por objeto — validação agregada,
   localização e revalidação — somam 60%. Agregar trocaria a única verificação que constata a
   realidade após a mutação por um quinto do ganho. Um `Delete()` sem efeito passaria despercebido
   e o processo seguiria apagando os demais. **Investigar** se o SDK oferece consulta direta por
   GUID, que daria a mesma garantia sem varredura completa.
4. **A ordem terminal do Remover permanece API → Procedures → SDTs → metadata File → Folder.**
   Uma versão anterior deste plano invertia os dois últimos; a decisão foi revertida após revisão
   externa. A inversão era resíduo da `v20`, que a queria para manter o File disponível à
   revalidação **por item** até o fim — revalidação que a decisão 7 cortou. Sem ela, inverter não
   compra nada e exigiria uma exceção no `IsFolderEmpty` para ignorar o File terminal, exceção
   capaz de liberar um Folder que ainda contenha outro File. Mantida a ordem atual, some a
   exceção e some o risco.
   O fato medido continua registrado, agora como observação e não como premissa: nas três
   transações o metadata File ficou no `Root Module`, fora do Folder.
5. **A sessão de progresso expõe três primitivos**, não dois:
   `ReportAndCheckBeforeWork`, `ReportAndCheckBeforeMutation` e `ReportCompleted(..., elapsedMs)`.
   O terceiro não verifica abort: um abort observado após um `Delete()` concluído não desfaz
   nada, e lançar ali produziria relatório de "abortado" para mutação já feita. `Report` e
   `Pump` crus ficam proibidos nos caminhos B082, o que torna o lint por intenção verificável.
6. **A guarda de operação única é a correção real da reentrância.** Aumentar a janela de
   progresso é reforço visual, não proteção: não impede Alt+Tab, atalhos de teclado da IDE, nem
   o processamento de eventos, que é o mecanismo da reentrada.
7. **O vínculo Preview → Remove fica enxuto.** O Remove recebe o plano do Preview **por
   referência, em memória**, e não reconstrói plano nenhum a partir do File corrente. A instância
   que alimenta a renderização da confirmação é **a mesma** que alimenta a fila executada e o
   contador de planejados; nenhum call site recalcula um total ou uma lista por caminho próprio.

   Antes da primeira exclusão, valida-se, além do que `ValidateRemovalTargets` já faz hoje por
   alvo — API por GUID, Procedure e SDT por descrição de posse, com bloqueio em cardinalidade
   ambígua —, a identidade da Transaction por GUID, o metadata File único com mesmo GUID e
   ownership, e o SHA-256 dos bytes igual ao capturado no Preview. Divergência em qualquer um
   bloqueia com zero exclusões. Daí em diante, a lista confirmada é a autorização.

   O que **não** entra: revalidação de identidade repetida a cada alvo intermediário. Ela é o
   aparato que a `v20` construía para o cenário de alteração externa durante a exclusão, cortado
   pelas razões da tabela abaixo.
8. **Diálogos passam a se ancorar na tela do owner.** Posicionamento calculado a partir do
   retângulo da janela dona, incluindo o caso de owner nativo; `CenterScreen` e `CenterParent`
   deixam de ser usados onde não funcionam.

## O que foi cortado da v20, e por quê

| Item da v20 | Motivo do corte |
|---|---|
| Segundo checkpoint de SHA, antes do Delete do File | Protege contra alteração do metadata no meio da exclusão — cenário que exige manipular o arquivo fora da IDE e reimportá-lo manualmente |
| Política de bytes intermediários e exclusão parcial | Consequência do item acima; sem ele, não existe |
| Estados `PresentOwned` / `AbsentAtPreview` / `PreservedExisting` / `PreservedAbsent` / `PreservedNonEmpty` / `Blocked` como modelo multidimensional | Complexidade em código destrutivo é risco próprio; a validação única do item 7 cobre o que é real |
| `ApiPlanGeneratedApiRemovalPartialResult` com motivos terminais | Sem exclusão parcial planejada, não há resultado parcial a modelar. Permanece apenas o item estruturado de Folder preservado (D7) |
| Chave `RemovalConfirmationPartialWarning` e variantes PT/ES/EN | Advertia sobre a política cortada |
| Gate de decisão humana com `human-decision.md` e `manuscriptSha256` | Existia unicamente para autorizar a política de bytes. Como efeito colateral, some o problema de o registro viver em `Temp/`, que é ignorado pelo git |
| Predicado normativo **novo** de ownership de Folder, com dimensões separadas de presença, procedência e disposição | **Corte parcial, revisto.** A falta de verificação de contêiner no Remover é real (D10) e entra na Etapa 2, mas resolvida por reúso da regra que o Apply já tem, não por um predicado normativo novo |
| Manifesto `Tests/B082/GetAllManifest.json` com lint bidirecional | Inventário de `GetAll` como artefato versionado, quando a instrumentação já mede os mesmos call sites em runtime e com custo. Reavaliar depois da otimização, se ainda fizer sentido |

## Ordem de execução

**Etapa 1A — desempenho sem atravessar mutação.** Writers de Business Component e List
consumindo o mapa de atributos; índice criado uma vez por operação, eliminando os dois
`kbIndex ??= Create(...)` e a criação direta; preflights de API Object, Procedure e SDT
consumindo o índice já construído, todos eles anteriores ao primeiro `Save()`. Nenhuma regra de
escrita ou exclusão muda, e nenhum mapa precisa continuar fiel depois de uma mutação.

**Etapa 1B — desempenho atravessando mutação.** Índice mantido coerente conforme a extensão cria
e apaga, com o contrato exigido pelo Nível B da decisão 1 escrito antes do código. Cobre a
validação agregada, a localização e a revalidação do Remover. **Não** cobre as confirmações
pós-`Delete`, que permanecem individuais e por leitura corrente.

**Etapa 2 — segurança.** Guarda de operação única nos quatro handlers (`ExecuteOpenWizardStepOne`,
`ExecuteSynchronizeWithTransaction`, `ExecuteRemoveGeneratedApi`, `ExecuteConfigureWizardPreferences`),
viva até o retorno do handler, inclusive durante o relatório final; remoção do `DoEvents()`
aninhado em `OnAbortClicked`; protocolo de abort escrito como sequência explícita — reportar,
processar eventos, verificar abort, revalidar o alvo, só então mutar — e não apenas "verificar
entre o report e a mutação"; plano do Preview entregue ao Remove como instância única, conforme
a decisão 7; e **verificação de contêiner no `MaybeDeleteFolder`**, reaproveitando a regra de
`ApiPlanTransactionFolder.IsInExpectedContainer` em vez de duplicá-la.

**Etapa 3 — comunicação e UX.** Fechamento da casca antes do relatório final em todos os
caminhos; correção do `DEMO.md:144-146` e do trecho correspondente do plano de 2026-08-31;
ancoragem das janelas na tela do owner; e Folder preservado como item estruturado — o que exige
tocar, no mesmo passo, toda a cadeia que hoje transporta a informação como texto:
`ApiPlanGeneratedApiRemover` produz a string `Folder:{nome}:PreservedNonEmpty`,
`ApiPlanApplicationFinalReport.AddDeletedItems` a recebe, `TryParsePreservedFolder` a interpreta,
e `BuildOutputSummary` a publica em `Package.cs`. Trocar apenas o produtor quebra o consumidor.

**Fora desta frente, registrado:** a abertura do Wizard (D8) e a responsividade da IDE (D11)
têm causa distinta — montagem de interface e trabalho na thread da UI. Merecem frente própria.

A Etapa 1A vem primeiro por três razões medidas: é a maior melhoria isolada disponível; é a de
menor risco, porque trocar uma varredura por consulta a um mapa de objetos que a extensão nunca
modifica não altera comportamento e um erro quebra o build, não a KB; e ela reduz a janela de
exposição que a D1 explora — cada Apply passa cerca de um minuto a menos dentro do `DoEvents()`.

A 1B **não herda** esse argumento de risco e pode ser adiada sem prejuízo da 1A: o Apply melhora
quase tudo que tem para melhorar já na 1A, e o ganho restante do Remove não justifica entregar um
índice mutável sem contrato. Se a 1B for adiada, o Remover continua com leitura corrente em todos
os pontos, exatamente como hoje.

## Critérios de aceite

**Etapa 1A.** Medir de novo `Setor`, `Empresa` e `DocumentoFiscal` nas três operações — Apply,
Sincronizar e Remover — com a instrumentação ligada.

| Operação | Hoje | Meta 1A |
|---|---|---|
| Apply `Setor` | 84,3 s | ≤ 25 s |
| Apply `Empresa` | 161,5 s | ≤ 120 s |
| Apply `DocumentoFiscal` | 187,5 s | ≤ 130 s |
| Remove `Setor` | 12,8 s | ≤ 12 s |
| Remove `Empresa` | 41,2 s | ≤ 36 s |
| Remove `DocumentoFiscal` | 14,5 s | ≤ 13 s |

O ganho do Remove na 1A é modesto de propósito: só a validação agregada — que roda antes de
qualquer exclusão e portanto é Nível A — passa a usar o índice. Localização e revalidação
pertencem à 1B.

**Etapa 1B**, se e quando for executada:

| Operação | Meta 1B |
|---|---|
| Remove `Setor` | ≤ 7 s |
| Remove `Empresa` | ≤ 20 s |
| Remove `DocumentoFiscal` | ≤ 9 s |

**Marcas estruturais, em ambas:** `indice-create` aparece **uma vez** por tipo, não quatro;
`bc-find-attribute` e `list-find-attribute` desaparecem do relatório de varreduras; e as
confirmações `confirmacao-pos-delete` **continuam presentes**, uma por objeto — se sumirem, a
decisão 3 foi violada.

**Disciplina de medição.** As tabelas da seção «Medições» vêm de **uma execução única por
transação**, sem repetição, sem variância e sem distinção entre KB fria e quente. Elas servem
como ordem de grandeza, não como linha de base estatística. Para o aceite:

- executar **três vezes** cada combinação e registrar as três, não só a melhor;
- declarar se a KB estava recém-aberta ou já em uso, e manter a mesma condição no antes e no
  depois;
- considerar aprovado quando as três execuções ficarem abaixo da meta, não a média.

O **overhead da própria instrumentação não foi medido**. Por construção é um `Stopwatch` e uma
inserção em lista por varredura de 100 a 1300 ms, o que o torna desprezível — mas isso é
argumento, não medição, e ambos os lados da comparação o carregam igualmente.

**Equivalência dos artefatos.** Comparar nomes e quantidades **não** prova equivalência. Conforme
o `AGENTS.md` deste repositório, conferir também o trio: `Procedure.Rules.Source` com o `parm(...)`,
a chamada gerada em `API.ServiceGroupSource.Source`, e as variáveis em
`API.Variables.Content.Content` e `Procedure.Variables`. Acrescentar descrições de serviço,
hierarquia de SDTs e o conteúdo do File de metadata — cujo SHA-256 é publicado no Output
`[B060]` e serve de comparação direta entre antes e depois.

**Etapa 2.** Uma segunda entrada durante uma operação longa é recusada, com mensagem localizada
no Output quando ele estiver disponível e sem abrir UI nova quando não estiver. O teste
vinculante é reentrada aninhada na mesma thread. Abortar durante a exclusão não apaga mais
nenhum objeto após o clique. O Remove executa exatamente a lista exibida na confirmação, e uma
divergência de identidade ou de SHA bloqueia com zero exclusões.

**Etapa 2, adicional.** Um Folder homônimo situado em contêiner diferente do esperado **não** é
apagado, mesmo com descrição canônica e vazio; o teste cobre esse caso explicitamente.

**Etapa 3.** O relatório final abre com a casca já fechada em sucesso, erro, bloqueio e abort.
O `DEMO.md` descreve a casca como modeless e afirma que Abortar após o primeiro `Save()` pode
deixar estado parcial. Folder preservado aparece como item preservado, não entre os removidos —
com produtor e consumidores atualizados no mesmo passo, e `TryParsePreservedFolder` removido ou
reescrito, nunca deixado a interpretar uma string que já não é produzida. Wizard, progresso,
confirmação e relatório abrem na mesma tela da IDE, com a IDE em monitor secundário.

**Em todas:** build Release pelo procedimento do repositório, reinstalação manual da DLL, e
smoke na IDE. Lint e teste unitário não substituem o smoke.

## Riscos e limites declarados

- **A otimização não resolve o Apply de transações muito grandes.** Em `Empresa` e
  `DocumentoFiscal`, cerca de 110 e 120 segundos são o SDK gravando objetos, fora do alcance de
  qualquer decisão sobre índice. O ganho é de aproximadamente um minuto fixo por Apply: decisivo
  nas transações medianas, que são a maioria da KB, e modesto nas gigantes.
- **A guarda não promete exclusividade contra comandos nativos da IDE** executados durante o
  `DoEvents()`. Risco residual, a declarar no smoke.
- **A guarda é global ao processo**, não por KB: uma operação em uma KB recusa uma segunda em
  outra enquanto o token viver. Comportamento conservador, a documentar.
- **As notas de release do `0.1.0-alpha.7` são histórico publicado** e não fazem parte da
  correção documental. Alterá-las exige decisão própria ou errata posterior.
- **Nenhuma medição aqui vale para uma DLL diferente** das dos commits `7fd4a0d` e `c4d62ee`.

## Apêndice — mapa para quem for implementar

Este apêndice entrega o inventário levantado durante a medição e as armadilhas já
encontradas. **Ele não desenha a solução:** que estrutura usar no lugar de `ILookup`, como
propagar o índice e se vale extrair um contexto explícito são decisões de quem implementa, com
o código à frente. O que segue é o mapa, não o caminho.

Todas as referências são por **arquivo e símbolo**. Números de linha aparecem só como auxílio
de navegação e podem ter mudado; confirme pelo símbolo.

### A1 — O índice hoje é imutável

`ApiPlanKbObjectNameIndex` guarda os sete mapas como `ILookup<string, T>`, que **não permite
inserção nem remoção**. Cinco dos sete campos são `readonly` — `_procedures`, `_apis`,
`_files`, `_transactions` e `_attributes` — e por isso nem podem ser reatribuídos. Apenas
`_folders` e `_sdts` não são `readonly`, o que é exatamente a razão de só eles terem
`RefreshFolders` e `RefreshSdts`, ambos reconstruindo o mapa inteiro com um novo `GetAll`.

**A decisão 1 deste plano — índice construído uma vez e ajustado conforme a extensão apaga ou
cria — não é implementável sobre `ILookup`.** Ela exige uma estrutura mutável. Essa é a
primeira decisão técnica da Etapa 1, e vem antes de qualquer outra alteração.

Consequência a considerar: `RefreshFolders` e `RefreshSdts`, que hoje custam uma varredura
completa cada, tornam-se desnecessários se o índice passar a registrar as próprias criações.
No Apply de `Empresa` o `indice-refresh` de SDT custou 173 ms; é pequeno, mas some de graça.

### A2 — Onde o índice é criado quatro vezes

| Origem | Natureza |
|---|---|
| `ApiPlanGenerationStateReader.ReadForIntentionalChangeWithIndex` | A criação legítima do Apply, com `progress` |
| `ApiPlanGenerationStateReader.ReadForIntentionalChange` | **Sem `progress`**, alcançada por `ApiPlanWritePreflight.ValidateForIntentionalChange`. É por isso que `Fase IndiceKb` e `Fase PreflightAgregado` medem quase o mesmo tempo |
| `ApiPlanProcedureWriter` (`kbIndex ??= ApiPlanKbObjectNameIndex.Create(...)`) | Fallback oculto quando o índice não chega |
| `ApiPlanSdtWriter` (`kbIndex ??= ApiPlanKbObjectNameIndex.Create(...)`) | Fallback oculto, mesmo padrão |
| `ApiPlanSdtWriter` (`ApiPlanKbObjectNameIndex.Create(designModel)` direto) | Criação direta, sem sequer tentar receber um índice |

Fora do Apply há mais duas, legítimas por serem fases distintas: `Package.cs` no Preview do
Sync e no Preview do Remover. Não confundir com as acima.

Os dois `kbIndex ??=` são o mecanismo pelo qual um índice deixa de chegar sem que ninguém
perceba — o código continua correto e fica lento em silêncio. Enquanto existirem, nenhum lint
consegue provar que o índice foi propagado.

### A3 — Símbolos que varrem o catálogo em laço

**Atributos** — a maior fatia, cerca de 50 s por Apply quando a PK tem 2 partes e há 2 filtros:

- `ApiPlanBusinessComponentWriter.EnsureAttributeExists`, chamado por `TrySetAttributeBasedOn`
- `ApiPlanListProcedureWriter.EnsureAttributeExists`

Ambos recebem apenas `KBModel model`. O índice **já expõe** `FindAttributes(string)` e
`TryGetSingleAttribute(string, out GxAttribute)`, sem nenhum consumidor hoje.

**Preflights que varrem uma vez por objeto:**

| Símbolo | Varre |
|---|---|
| `ApiPlanApiObjectWriter.PreflightRequiredSdts` | `SDT.GetAll` por SDT do plano |
| `ApiPlanApiObjectWriter.PreflightRequiredProcedures` | `Procedure.GetAll` por Procedure |
| `ApiPlanProcedureWriter.PreflightProcedures` | `Procedure.GetAll` por definição |
| `ApiPlanBusinessComponentWriter.EnsureSdts` | `SDT.GetAll` por SDT |
| `ApiPlanBusinessComponentWriter.FindProcedure` | `Procedure.GetAll` por serviço |
| `ApiPlanListProcedureWriter.FindListProcedure` | `Procedure.GetAll` |

Em `Empresa`, `PreflightRequiredSdts` e `EnsureSdts` fizeram 47 varreduras cada.

**O índice chega até a porta, mas não entra.** `ApiPlanBusinessComponentWriter.Apply` e
`ApiPlanListProcedureWriter.Apply` já recebem `kbIndex` nas assinaturas públicas e o repassam a
`ApiPlanSdtWriter.CreateOrReencounter` — mas **não** aos métodos privados acima, que continuam
recebendo só `KBModel`. Já `ApiPlanApiObjectWriter` **não tem parâmetro `kbIndex` em nenhuma
assinatura**: ali a propagação começa do zero.

### A4 — No Remover

`ApiPlanGeneratedApiRemover` faz quatro varreduras por objeto, hoje todas com `kbIndex: null`
depois da validação agregada:

- validação agregada (`ValidateRemovalTargets`) — roda **antes de qualquer exclusão**, portanto é
  Nível A: pode usar o índice já na Etapa 1A;
- localização antes do `Delete` (`DeleteSingleProcedure`, `DeleteApiObject`, `DeleteSingleOwnSdt`,
  `MaybeDeleteFolder`) — Nível B, exige índice mantido;
- revalidação de identidade imediatamente antes do `Delete` — Nível B, idem;
- confirmação depois do `Delete` — **permanece individual e por leitura corrente**. Ela existe
  para constatar que o `Delete()` do SDK surtiu efeito; um índice, mantido ou não, não responde a
  essa pergunta. Não otimizar por agregação (ver decisão 3).

`IsFolderEmpty` faz cinco varreduras completas de uma vez, e com as de localização e confirmação
do Folder a exclusão de um único Folder custa sete. Mede 1,4 a 1,7 s — não é o gargalo. As de
localização podem usar o índice mantido na 1B; a de confirmação, não.

O parâmetro booleano `beforeAnyDelete`, usado para variar a mensagem de bloqueio, é o ponto onde
a distinção entre "antes de qualquer exclusão" e "durante" está codificada hoje.

`MaybeDeleteFolder` é também onde entra a correção da D10 (Etapa 2): hoje ele não verifica
contêiner, e a regra a reaproveitar é `ApiPlanTransactionFolder.IsInExpectedContainer`, hoje
privada e usada só por `IsReusable`. Note que a permissividade de `IsReusable` para Description
vazia serve à reutilização durante o Apply e **não** deve ser transportada para autorizar um
`Delete`.

### A5 — Testes que reprovam por casamento textual

Vários lints do repositório leem o fonte como texto e comparam assinaturas literais. **Uma
reprovação desses testes após mudança de assinatura não é regressão de comportamento** — é o
lint precisando ser atualizado junto.

Precedente desta sessão: `Tests/GeneratedApiRemoval/Test-ApiPlanGeneratedApiRemovalPreflight.ps1`
reprovou ao acrescentarmos um parâmetro, porque procurava `DeleteApiObject(designModel, plan,
deleted)` com o parêntese final. Foi corrigido para casar por prefixo, preservando a intenção da
asserção — que é provar que o preflight ocorre antes da primeira exclusão, não congelar a lista
de parâmetros. Ao encontrar caso semelhante, **preserve a intenção da asserção**; não a remova.

O mesmo arquivo também exige que `ValidateRemovalTargets(designModel, plan` apareça exatamente
três vezes. Alterar o número de pontos de entrada do preflight exige atualizar essa contagem
conscientemente.

Rodar sempre, na raiz do repositório:

```powershell
pwsh -NoProfile -File scripts/Invoke-PrePushMechanicalChecks.ps1 -AsJson
```

`exit 0` mecânico não substitui a revisão semântica exigida pelo `AGENTS.md`.

### A6 — Como medir o depois

A instrumentação **já está instalada e deve permanecer**: `ApiPlanScanTelemetry` e
`ApiPlanScanProbe`, ligados no Remover e no Apply/Sync. Sem escopo ativo, `ApiPlanScanProbe.Scan`
apenas executa o delegate — custo zero fora da medição. Ao envolver uma varredura nova, inclua
no delegate **o pipeline inteiro até a materialização** (`ToArray`, `Any`, `ToLookup`), porque
`GetAll` é preguiçoso e cronometrar só a chamada não mede nada.

Procedimento, na ordem:

1. `dotnet build-server shutdown` e build Release pelo procedimento do repositório;
2. fechar a IDE por completo;
3. `Install-ExtensionForGeneXus18.bat` como administrador, na raiz — sem `genexus /install`,
   a menos que o manifesto ou o registro tenham mudado;
4. reabrir a IDE e executar, em cada uma das três transações, Apply, Sincronizar e Remover;
5. coletar do Output as linhas `[B082] Apply ...` e `[B082] Remover ...` e comparar com as
   tabelas da seção «Medições».

**Reinstalar não é opcional.** Medição vale para a DLL que a produziu: sem reinstalar, ou os
números são do código antigo, ou as linhas novas simplesmente não aparecem — e ausência de linha
parece resultado.

As três transações são `Setor`, `Empresa` e `DocumentoFiscal`, na KB `Fabrica Brasil Test`. Elas
foram escolhidas para isolar variáveis distintas: `Setor` e `DocumentoFiscal` têm a mesma PK de
2 partes e 2 filtros com 6 e 171 campos, e `Empresa` tem PK de 1 parte, 1 filtro e 14 níveis.
Trocar de transação invalida a comparação com as tabelas deste documento.

### A7 — O que não medimos

- **O ganho real.** Todas as projeções deste plano são aritmética sobre os tempos medidos, não
  resultado observado. Trate-as como hipótese a confirmar no aceite da Etapa 1.
- **Variância, estado frio/quente e overhead da instrumentação.** Cada combinação foi executada
  **uma vez**. As metas de aceite exigem três execuções justamente porque a linha de base não as
  tem.
- **Se `Delete()` do SDK pode falhar em silêncio.** Não sabemos. Foi por assumir que não que uma
  versão anterior deste plano agregava as confirmações; a decisão foi revertida, e é essa
  ignorância que justifica manter a verificação individual.
- **Cobertura da instrumentação.** Ela foi aplicada seletivamente, aos tipos caros e aos laços
  conhecidos — não a todos os `GetAll` do repositório. Um call site não instrumentado não aparece
  no relatório de varreduras e pode passar por inexistente. Ao investigar um tempo que não fecha,
  suspeite primeiro de varredura não instrumentada.
- **O custo do `Save()` por tipo de objeto.** Sabemos que em `Empresa` a criação de 44 SDTs
  levou 30 s — cerca de 685 ms cada — mas não instrumentamos as mutações individualmente.
- **A causa dos 7 s de montagem de interface** na abertura do Wizard de `DocumentoFiscal`.
- **A reentrância acontecendo de fato.** A D1 é comprovada por leitura de código e pela
  possibilidade de mexer no menu durante a operação; nunca foi reproduzida até a corrupção.
