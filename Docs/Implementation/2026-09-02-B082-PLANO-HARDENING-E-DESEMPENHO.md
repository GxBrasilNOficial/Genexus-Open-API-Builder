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

**Esta frente passa a ser ativa**, por decisão humana de 2026-09-02, e
`Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md` foi atualizado no mesmo passo: a próxima ação única passa
a ser a Etapa 1A deste plano, e o `B108` — cujo plano continua aprovado e gravado — recua para a
posição seguinte. A instrução anterior de «não reabrir o desenho do B082» valia para a linha do
corte `0.1.0-alpha.7`, já publicado; ela é substituída por esta frente, que reabre o B082
deliberadamente, com escopo e medição próprios.

A primeira redação deste documento afirmava que o checkpoint não mudaria. Isso se sustentava
enquanto ele fosse planejamento; deixou de valer quando a implementação foi autorizada, e a
contradição entre os dois documentos canônicos foi apontada por revisão externa.

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

**Reconferido na IDE em 2026-09-02**, com a DLL posterior às mudanças de instrumentação
(escopo do Sync, callback de encerramento e `Suspend` no relatório). O Apply de `Empresa` repetiu
**155 varreduras** e o Remover, **205**, com todas as contagens por categoria idênticas às da
primeira coleta. A linha de base desta seção está confirmada; as mudanças de instrumentação não
alteraram o que é medido.

**Três execuções de `Empresa`, e a variância separa-se em duas naturezas:**

| Execução | Apply total | Apply varredura | Remove total | Remove varredura |
|---|---|---|---|---|
| 1 | 161,5 s | 52,2 s | 41,2 s | 33,6 s |
| 2 | 171,9 s | 52,5 s | 38,7 s | 30,9 s |
| 3 | 175,2 s | 52,9 s | 39,5 s | 31,5 s |
| **Amplitude** | **8,5%** | **1,4%** | **6,5%** | **8,5%** |

As **contagens** repetiram exatamente nas três: 155 varreduras no Apply, 205 no Remover, com o
mesmo número por categoria. O que oscila é tempo, não trabalho.

A varredura do **Apply** é o número mais confiável que temos: 52,2, 52,5 e 52,9 s — amplitude de
1,4%. Nele, uma diferença de 3% já é sinal. A do **Remover** oscila mais (8,5%), o que é
coerente: o catálogo encolhe a cada exclusão, então cada varredura seguinte é sobre um conjunto
menor. Os **totais** carregam o tempo de escrita do SDK e variam de 6,5% a 8,5%.

Regra para o aceite, derivada disso:

- comparar **primeiro a contagem** de varreduras — ela é determinística e qualquer mudança é sinal;
- na **varredura do Apply**, tratar diferença acima de 3% como real;
- em **totais** e na varredura do Remover, exigir 10% para afirmar ganho ou regressão.

Comparar uma execução única antes e depois pode fabricar um ganho de 8% que não existe — ou
esconder um real.

**O Sync que grava foi medido em 2026-09-02** na KB pequena, provocado por um atributo acrescentado
à Transaction depois de gerada a API. Ver a seção própria abaixo. Não há mais fluxo sem linha de
base.

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
resistir a ser arrastada. **Causa medida:** o processo consome 0,94 de uma thread lógica — um
núcleo saturado — e essa thread é a da interface, devolvida apenas pelo `DoEvents()` entre
operações. Não é I/O nem contenção: é CPU ocupada. Ver «O gargalo é CPU numa única thread».

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

### O ganho é proporcional ao tamanho da KB

Medido em 2026-09-02 na KB pequena `wsEducacaoSpTeste`, transação `NotaFiscal`. **O custo de uma
varredura depende do tamanho da KB, não da operação:**

| Varredura | `wsEducacaoSpTeste` | `Fabrica Brasil Test` | Razão |
|---|---|---|---|
| `Attribute.GetAll` | ~30 ms | ~1300 ms | **44×** |
| `Procedure.GetAll` | ~27 ms | ~500 ms | 19× |
| `SDT.GetAll` | ~18 ms | ~125 ms | 7× |

E o peso da varredura no total muda de figura:

| Caso | Objetos | Total | Varredura |
|---|---|---|---|
| `Setor` (KB grande) | 12 | 84,3 s | 68,1 s — **81%** |
| `Empresa` (KB grande) | 51 | 171,9 s | 52,5 s — 31% |
| `NotaFiscal` (KB pequena) | 12 | 9,8 s | 2,1 s — **21%** |
| Sync que grava (KB pequena) | 15 atualizados | 8,6 s | 1,9 s — 22% |

**Na KB pequena a varredura não é o gargalo:** 79% do tempo é escrita no SDK, e a Etapa 1A levaria
o Apply de `NotaFiscal` de 9,8 s para talvez 8,3 s — 15%, não 4×.

Isso não enfraquece a frente, mas delimita o que ela promete: **as metas de aceite deste documento
valem para a KB grande.** Numa KB pequena a extensão já é rápida o bastante, e o ganho será
modesto. O caso que justifica a frente é o do usuário com KB grande — que é também o usuário cuja
adoção depende do desempenho.

### O Sync que grava

Medido em 2026-09-02 (`NotaFiscal`, atributo acrescentado à Transaction depois de gerada a API):
**84 varreduras, 1,9 s, 22% de um total de 8,6 s**. Encerra a lacuna registrada nas versões
anteriores deste documento.

O Sync repete o padrão do Apply e **não precisa de tratamento próprio na Etapa 1A**: cria o índice
**quatro vezes** e chama `Attribute.GetAll` em laço — 24 vezes no writer de List e 13 no de
Business Component. A correlação com filtros aparece de novo: o Sync tinha 3 filtros contra 2 do
Apply, e as chamadas do List subiram de 18 para 24.

Também validou dois caminhos na prática: o Folder **preexistente** foi preservado corretamente
(`FolderWasCreated=False`, exibido como «reutilizado; nunca apagar», e o Remover não o incluiu na
fila), e o serviço `Delete` do `B100` participou de todas as fases.

### Previews de Sync e Remover

Medidos em 2026-09-02, depois de ganharem escopo próprio:

| | Varreduras | Tempo |
|---|---|---|
| Sync Preview | 7 | 2,0 s |
| Remover Preview | 7 | 2,1 s |

As sete são exatamente uma criação de índice por tipo de objeto — o Preview cria o índice **uma
vez**, ao contrário do Apply. E a distribuição confirma onde está o custo: `Attribute` responde
por **1,3 s dos 2,0 s**, contra 0,47 s de `Procedure`, 0,13 s de `SDT` e menos de 0,1 s para os
demais.

O índice paga 1,3 s para montar o mapa de atributos, e ele **já tem um consumidor**:
`ApiPlanSdtWriter.EnsureAttributeExists` usa `kbIndex.TryGetSingleAttribute`. O que falta é os
outros dois `EnsureAttributeExists` — de Business Component e de List — fazerem o mesmo, em vez
de varrer o catálogo. Depois da Etapa 1A o mapa passa a substituir também as ~50 s de varredura
desses dois, e as três criações extras de índice (~4 s) deixam de existir.

**Isso é uma boa notícia para o risco da 1A:** existe precedente funcionando no repositório. O
`ApiPlanSdtWriter` prova que consumir o mapa de atributos é correto e seguro; a 1A copia o padrão
dele para os outros dois writers, em vez de inventar um.

### O gargalo é CPU numa única thread

Medido em 2026-09-02, na máquina de desenvolvimento — **AMD Ryzen 7 3800X**, 8 núcleos físicos e
**16 threads lógicas** —, com o Gerenciador de Tarefas durante Apply e Remover de `Empresa`: o
processo do GeneXus (32 bits) ficou em **5,9% de CPU**, com pico de 9,6%. Uma thread saturada
equivale a 6,25% do total, então 5,9% são **0,94 thread** — praticamente um núcleo a 100% e quinze
ociosos. O disco ficou em 0,5 MB/s.

Três conclusões, todas com efeito sobre este plano:

- **Não é I/O.** O tempo de `GetAll` é processamento, não espera. Eliminar varreduras converte-se
  diretamente em tempo economizado, e as projeções deste plano não dependem de suposição sobre
  disco ou rede.
- **A D11 tem causa provada.** A IDE fica irresponsiva porque essa thread saturada é a thread da
  interface — o `DoEvents()` só a devolve entre operações.
- **Paralelizar não é alternativa.** O SDK do GeneXus não é thread-safe e as mutações exigem a
  thread da UI. Com quinze núcleos ociosos, a tentação é distribuir o trabalho; o caminho viável é
  o oposto — **fazer menos trabalho**, que é o que a Etapa 1A faz.

### Fato colateral medido

`MetadataFile Parent='Root Module'` nas três transações. O metadata File **não** fica dentro do
Folder da Transaction, e por isso o `WikiFileKBObject.GetAll` de `IsFolderEmpty` nunca o conta.

Isso respondia ao gate que a `v20` exigia antes de inverter a ordem para Folder → File. Como a
inversão foi abandonada (decisão 4), o dado permanece apenas como observação: **três transações
de uma KB não provam o comportamento de toda instalação**, e a ordem atual não depende disso.

## Decisões fechadas

1. **O índice é construído uma vez por operação**, e passa a ser reaproveitado em dois níveis de
   risco deliberadamente distintos, que não devem ser confundidos nem entregues juntos:

   - **Nível A — seguro com o índice como está hoje.** Três coisas, e só estas:
     (a) os `Attribute.GetAll` de `ApiPlanBusinessComponentWriter.EnsureAttributeExists` e
     `ApiPlanListProcedureWriter.EnsureAttributeExists`, porque a extensão **nunca** cria, altera
     ou apaga atributos — são seguros em qualquer ponto do fluxo;
     (b) criar o índice **uma vez** por operação em vez de quatro;
     (c) os lookups cujo tipo **não foi mutado desde a última visão do índice**:
     `ApiPlanProcedureWriter.PreflightProcedures` (roda antes de as Procedures serem criadas) e,
     **depois do `RefreshSdts` que já existe**, `ApiPlanApiObjectWriter.PreflightRequiredSdts` e
     `ApiPlanBusinessComponentWriter.EnsureSdts`.
   - **Fora do Nível A, mesmo parecendo simples:** `ApiPlanApiObjectWriter.PreflightRequiredProcedures`,
     `ApiPlanBusinessComponentWriter.FindProcedure` e `ApiPlanListProcedureWriter.FindListProcedure`.
     Os três rodam **depois** de as Procedures serem gravadas, e **não existe `RefreshProcedures`** —
     `_procedures` é um `ILookup` `readonly`. Ligá-los ao índice inicial faria o Apply de geração
     nova falhar com «Procedure requerida não foi reencontrada», justamente nos casos que este
     plano usa como aceite. Ou ganham um `RefreshProcedures` no molde do `RefreshSdts` (uma
     varredura, sem índice mutável), ou permanecem em leitura corrente até a 1B.
   - **Nível B — atravessa mutação.** Manter o índice coerente enquanto a extensão cria e apaga,
     em vez de reconstruí-lo. É o que o Remove precisa, e exige contrato explícito: invariantes,
     momento da atualização em relação ao sucesso da mutação, comportamento após exceção,
     duplicidade e mudança de contêiner. **Não é "baixo risco" e não deve ser tratado como tal.**

   **A ordem real do Apply**, que torna essa distinção obrigatória e que a primeira redação deste
   plano descreveu errado: índice → preflight agregado → grava SDTs → `RefreshSdts` → grava
   Procedures → fase API Object → fase Business Component → fase List. Os preflights das três
   últimas fases correm **depois** de mutações, não antes.

   Revoga a regra da `v20` que proibia usar o índice depois da primeira exclusão. Base: a IDE não
   expõe edição de File; alterá-lo exige manipulá-lo fora da IDE e reimportar por um fluxo manual
   de vários cliques, incompatível com o intervalo de uma operação — e a guarda de operação única
   fecha o caminho pelo qual a própria extensão poderia interferir. **A guarda não cobre comandos
   nativos da IDE durante o `DoEvents()`**, e é por isso que o Nível B precisa de contrato em vez
   de confiança.
2. **Os writers passam a usar o mapa de atributos que já existe.** `ApiPlanKbObjectNameIndex` já
   constrói o mapa (`GxAttribute` é alias de `Artech.Genexus.Common.Objects.Attribute`) e expõe
   `FindAttributes` e `TryGetSingleAttribute` — este último **já usado** por
   `ApiPlanSdtWriter.EnsureAttributeExists`, que serve de modelo a copiar.
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

   **O Preview captura a identidade de cada alvo destrutivo:** GUID e contêiner de cada API
   Object, Procedure, SDT próprio e Folder, além do GUID e ownership do metadata File e do GUID
   da Transaction. Hoje o metadata guarda GUID apenas da API; Procedures, SDTs e Folder existem
   no plano só como nome. Sem GUID capturado, a promessa de "executar exatamente a lista exibida"
   é **nominal**: nome, descrição de posse e cardinalidade não distinguem um objeto de outro
   homônimo que tenha ocupado seu lugar. A captura é barata porque o Preview **já enumera** esses
   objetos em `ValidateRemovalTargets` — o GUID está em `matches[0]` e hoje é descartado.

   Antes da primeira exclusão, valida-se, além do que `ValidateRemovalTargets` já faz hoje por
   alvo — cardinalidade e descrição de posse —, o **GUID capturado de cada alvo**, a identidade da
   Transaction por GUID, o metadata File único com mesmo GUID e ownership, e o SHA-256 dos bytes
   igual ao capturado no Preview. Divergência em qualquer um bloqueia com zero exclusões. Daí em
   diante, a lista confirmada é a autorização.

   Isto **não** reintroduz a `v20`: continuam fora os seis estados derivados, a revalidação de
   identidade repetida a cada alvo e o segundo checkpoint de SHA. É um campo a mais no que já se
   captura, comparado uma única vez.

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

**Etapa 1A — desempenho com o índice como está hoje.** Escopo exato, conforme o Nível A da
decisão 1:

- `Attribute` pelo mapa que o índice já constrói, nos dois `EnsureAttributeExists`;
- índice criado **uma vez** por operação, eliminando as **quatro** origens supérfluas: os dois
  `kbIndex ??= Create(...)` de `ApiPlanProcedureWriter` e `ApiPlanSdtWriter`, a criação direta em
  `ApiPlanSdtWriter`, e — a que é fácil esquecer — a de
  `ApiPlanGenerationStateReader.ReadForIntentionalChange`, alcançada por
  `ApiPlanWritePreflight.ValidateForIntentionalChange` **sem `progress`**. Esta última é a razão de
  `Fase IndiceKb` e `Fase PreflightAgregado` medirem quase o mesmo tempo. A origem legítima é
  `ReadForIntentionalChangeWithIndex`. Ver o inventário completo no item A2 do apêndice;
- `PreflightProcedures` pelo índice inicial; `PreflightRequiredSdts` e `EnsureSdts` pelo índice
  **depois do `RefreshSdts` que já existe**;
- no Remover, apenas a validação agregada, que roda antes de qualquer exclusão.

**A Etapa 1A não altera a estrutura do índice.** Ele continua `ILookup`, e `RefreshFolders` e
`RefreshSdts` **continuam existindo** — removê-los reabriria o defeito já corrigido em que um
segundo `CreateOrReencounter` no mesmo Apply tentava criar `GxOpenAPI` de novo, e há teste textual
que exige essas chamadas.

**Fora da 1A:** as buscas de Procedure posteriores à gravação delas (`PreflightRequiredProcedures`,
`FindProcedure`, `FindListProcedure`). Elas exigem um `RefreshProcedures` no molde do `RefreshSdts`
— uma varredura só, sem tornar o índice mutável. Se esse método for criado, elas entram; se não,
permanecem em leitura corrente. **As metas de aceite abaixo não dependem delas**, e valem sem esse
refresh.

**Etapa 1B — desempenho atravessando mutação.** Índice mantido coerente conforme a extensão cria
e apaga, com o contrato exigido pelo Nível B da decisão 1 escrito antes do código. Cobre a
validação agregada, a localização e a revalidação do Remover. **Não** cobre as confirmações
pós-`Delete`, que permanecem individuais e por leitura corrente.

**Etapa 2 — segurança.** Guarda de operação única nos quatro handlers (`ExecuteOpenWizardStepOne`,
`ExecuteSynchronizeWithTransaction`, `ExecuteRemoveGeneratedApi`, `ExecuteConfigureWizardPreferences`),
viva até o retorno do handler, inclusive durante o relatório final; remoção do `DoEvents()`
aninhado em `OnAbortClicked`; protocolo de abort escrito como sequência explícita — **reportar,
processar eventos, verificar abort, e só então mutar**; plano do Preview entregue ao Remove como
instância única, conforme a decisão 7; e **verificação de contêiner e GUID no `MaybeDeleteFolder`**.

**A sequência de abort não acrescenta revalidação de identidade por alvo.** Uma redação anterior
dizia «verificar abort, revalidar o alvo, só então mutar», o que contradizia a decisão 7 e
reintroduziria pela porta dos fundos o aparato da `v20`. O que falta hoje no `ReportDelete` é
apenas o `ThrowIfAbortRequested` entre o `Report` — que já executa `DoEvents` e portanto processa
o clique — e o `Delete()`. A localização por GUID que já existe no fluxo permanece como está;
nenhuma verificação nova por item entra aqui.

**A verificação do Folder** exige três mudanças concretas, não uma menção: `ApiPlanTransactionFolder.IsInExpectedContainer`
é privado e recebe o objeto `Transaction`, então precisa ser tornado compartilhável; `MaybeDeleteFolder`
recebe hoje apenas `plan`, e precisa passar a receber a `Transaction` que `Remove` já tem em mãos; e o
Folder passa a ser conferido também por **GUID capturado no Preview**, já que o metadata grava dele
apenas `name` e `wasCreated`. A permissividade de `IsReusable` para Description vazia serve à
reutilização durante o Apply e **não** pode ser transportada para autorizar um `Delete`.

**Etapa 3 — comunicação e UX.** Fechamento da casca antes do relatório final em todos os
caminhos; correção do `DEMO.md:144-146` e do trecho correspondente do plano de 2026-08-31;
ancoragem das janelas na tela do owner; e Folder preservado como item estruturado — o que exige
tocar, no mesmo passo, toda a cadeia que hoje transporta a informação como texto:

1. `ApiPlanGeneratedApiRemover.MaybeDeleteFolder` produz `Folder:{nome}:PreservedNonEmpty` e a
   insere na lista de **removidos**;
2. `Package.cs` a entrega por `report.AddDeletedItems(result.DeletedItems.ToArray())`;
3. `ApiPlanApplicationFinalReport.AddDeletedItems` chama `TryParsePreservedFolder`, que faz o
   *parsing* da string e a reclassifica;
4. `BuildOutputSummary` e `BuildReadableBody` renderizam o resultado — o primeiro publicado por
   `WriteOutput` em `Package.cs`, o segundo pelo diálogo do relatório final via `ShowFinalReport`.

Trocar apenas o produtor deixa `TryParsePreservedFolder` interpretando uma string que já não é
produzida. Os testes de `Tests/ApplicationFinalReport/` cobrem esse caminho e precisam afirmar a
forma nova, não apenas perder as asserções antigas.

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
Sincronizar e Remover — com a instrumentação ligada, **na KB grande `Fabrica Brasil Test`**. As
metas abaixo valem só para ela: na KB pequena a varredura é 21% do tempo e o ganho esperado fica
em torno de 15%, o que não serve de critério.

| Operação | Hoje | Projetado | Meta 1A |
|---|---|---|---|
| Apply `Setor` | 84,3 s | 22,2 s | ≤ 28 s |
| Apply `Empresa` | 169,5 s (média de 3) | 122,8 s | ≤ 135 s |
| Apply `DocumentoFiscal` | 187,5 s | 127,3 s | ≤ 140 s |
| Remove `Setor` | 12,8 s | 10,4 s | ≤ 12 s |
| Remove `Empresa` | 39,8 s (média de 3) | 32,2 s | ≤ 36 s |
| Remove `DocumentoFiscal` | 14,5 s | 12,0 s | ≤ 13 s |

A coluna **Projetado** soma apenas o que o escopo da 1A converte: os dois `Attribute` em laço,
três das quatro criações de índice, `PreflightProcedures`, e `PreflightRequiredSdts` /
`EnsureSdts` após o `RefreshSdts` existente. **Não** inclui as buscas de Procedure posteriores à
gravação — se um `RefreshProcedures` for criado, sobram ainda 3,6 s em `Empresa` e 3,7 s em
`Setor`, que viram folga adicional.

A **Meta** acrescenta ao projetado a folga da variância medida (8,5%), porque o aceite exige que
as **três** execuções fiquem abaixo, não a média. Uma redação anterior deste plano trazia metas
de 25 s, 120 s e 130 s: elas pressupunham converter também as buscas de Procedure pós-gravação,
o que faria o Apply de geração nova falhar.

O ganho do Remove na 1A é modesto de propósito: só a validação agregada — que roda antes de
qualquer exclusão e portanto é Nível A — passa a usar o índice. Localização e revalidação
pertencem à 1B.

**Etapa 1B**, se e quando for executada:

| Operação | Meta 1B |
|---|---|
| Remove `Setor` | ≤ 7 s |
| Remove `Empresa` | ≤ 20 s |
| Remove `DocumentoFiscal` | ≤ 9 s |

**Marcas estruturais.** Elas cobrem exatamente o que a coluna «Projetado» soma, para que passar
nas marcas e falhar no relógio seja impossível — e para que, se o tempo não bater, as marcas digam
qual conversão faltou.

Devem **desaparecer** do relatório de varreduras:

| Linha | Vale, na KB grande |
|---|---|
| `Attribute/bc-find-attribute` | 14,9 s em `Empresa`, 28,9 s em `Setor` |
| `Attribute/list-find-attribute` | 11,6 s em `Empresa`, 23,0 s em `Setor` |
| `Procedure/procedure-preflight` | ~2,0 s |
| `SDT/apiobject-preflight-sdt` | 6,1 s em `Empresa` |
| `SDT/bc-ensure-sdt` | 5,9 s em `Empresa` |

Devem **permanecer**, e sua ausência é sinal de que a conversão perigosa foi feita:

| Linha | Por quê |
|---|---|
| `Procedure/apiobject-preflight-procedure` | Roda depois de gravar Procedures, sem `RefreshProcedures` |
| `Procedure/bc-find-procedure` | Idem |
| `Procedure/list-find-procedure` | Idem |

Só somem se um `RefreshProcedures` for criado **e chamado** — e nesse caso a linha
`Procedure/indice-refresh` passa a aparecer, do mesmo modo que `SDT/indice-refresh` hoje.

Demais marcas: `indice-create` aparece **uma vez** por tipo, não quatro; e as confirmações
`confirmacao-pos-delete` **continuam presentes**, uma por objeto — se sumirem, a decisão 3 foi
violada.

**O Sync entra por marcas estruturais, não por tempo.** Ele foi medido apenas na KB pequena, onde
a varredura é 22% do total; não há linha de base dele na KB grande, e inventar uma meta de tempo
sem número anterior seria arbitrário.

O que se exige do Sync que grava, na KB grande, são **as mesmas duas tabelas acima, sem exceção**:
as cinco linhas que devem desaparecer e as três que devem permanecer. Ele atravessa exatamente os
mesmos writers do Apply — `EnsureAttributeExists`, `PreflightProcedures`, `PreflightRequiredSdts`,
`EnsureSdts`, `FindProcedure`, `FindListProcedure` — e a coleta na KB pequena confirmou o padrão
idêntico, incluindo as quatro criações de índice. Uma 1A bem feita o cobre sem trabalho próprio; se
alguma das cinco linhas persistir só no Sync, é porque um call site do fluxo de Sincronizar ficou
para trás.

Se quiser meta de tempo para o Sync numa frente futura, **colete antes a linha de base na KB
grande** — provocando a diferença com um atributo novo — e só então defina o alvo.

**Disciplina de medição.** As tabelas da seção «Medições» vêm de **uma execução única por
transação**, sem repetição, sem variância e sem distinção entre KB fria e quente. Elas servem
como ordem de grandeza, não como linha de base estatística. Para o aceite:

- executar **três vezes** cada combinação e registrar as três, não só a melhor;
- declarar se a KB estava recém-aberta ou já em uso, e manter a mesma condição no antes e no
  depois;
- considerar aprovado quando as três execuções ficarem abaixo da meta, não a média;
- aplicar os limiares por natureza de número, medidos em três execuções: **contagem** de varreduras
  é determinística e qualquer mudança é sinal; **varredura do Apply** tem amplitude de 1,4%, então
  3% já é real; **totais** e **varredura do Remover** oscilam até 8,5%, e exigem 10% para afirmar
  ganho ou regressão;
- conferir a CPU durante a execução: se continuar em torno de 6% — uma thread saturada num
  processador de 16 threads — o trabalho segue ligado a CPU e single-thread, e a comparação de
  tempos é válida. Uma queda expressiva de CPU com tempo igual indicaria que o gargalo mudou de
  natureza, e aí a comparação deixa de valer.

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

**A linha satélite `gx18u13` não é dispensada.** Nada nesta frente é específico de versão — são
lookups e escopo de medição —, mas o rito do repositório exige as duas DLLs sempre que a canônica
U14+ mudar, e um corte publicado só com a canônica seria lido como abandono da linha U13. Ao
fechar cada etapa, gerar também o artefato satélite; a medição pode ficar só na canônica, já que
a diferença é de compilação, não de comportamento.

## Riscos e limites declarados

- **A otimização não resolve o Apply de transações muito grandes.** Em `Empresa` e
  `DocumentoFiscal`, cerca de 110 e 120 segundos são o SDK gravando objetos, fora do alcance de
  qualquer decisão sobre índice. O ganho é de aproximadamente um minuto fixo por Apply: decisivo
  nas transações medianas, que são a maioria da KB, e modesto nas gigantes.
- **Nem resolve nada relevante em KB pequena.** Medido em `wsEducacaoSpTeste`: a varredura é 21%
  do tempo, contra 81% na KB grande, porque o custo de um `GetAll` acompanha o tamanho da KB —
  `Attribute.GetAll` custa 30 ms lá e 1300 ms aqui. O Apply de `NotaFiscal` sairia de 9,8 s para
  cerca de 8,3 s. **A frente se justifica pelo usuário de KB grande**, e prometer ganho geral
  seria falso.
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

### A1 — O índice hoje é imutável, e a 1A não muda isso

`ApiPlanKbObjectNameIndex` guarda os sete mapas como `ILookup<string, T>`, que **não permite
inserção nem remoção**. Cinco dos sete campos são `readonly` — `_procedures`, `_apis`,
`_files`, `_transactions` e `_attributes` — e por isso nem podem ser reatribuídos. Apenas
`_folders` e `_sdts` não são `readonly`, o que é exatamente a razão de só eles terem
`RefreshFolders` e `RefreshSdts`, ambos reconstruindo o mapa inteiro com um novo `GetAll`.

**Isso é assunto da Etapa 1B, não da 1A.** Uma redação anterior deste apêndice mandava tornar o
índice mutável como primeira decisão técnica da Etapa 1 — está revogada. A 1A **não altera a
estrutura do índice**: ele continua `ILookup`, e `RefreshFolders` e `RefreshSdts` continuam
existindo e sendo chamados. Removê-los reabriria o defeito em que um segundo `CreateOrReencounter`
no mesmo Apply tentava criar `GxOpenAPI` de novo, e há teste textual que exige essas chamadas.

O que a imutabilidade impede, e que por isso fica na 1B: manter o índice fiel **através** das
mutações. Enquanto ela não existir, todo lookup posterior a uma gravação depende de um
`Refresh<Tipo>` daquele tipo — que só existe para Folder e SDT — ou permanece em leitura corrente.

**A ausência de `RefreshProcedures` é a consequência prática mais importante.** `_procedures` é
`readonly`, então nem reatribuir é possível hoje. Criar um `RefreshProcedures` no molde exato do
`RefreshSdts` — uma varredura, reatribuindo o campo, sem tornar o índice incremental — é opção
legítima **dentro da 1A**, e liberaria `PreflightRequiredProcedures`, `FindProcedure` e
`FindListProcedure`. As metas deste plano não dependem disso; é ganho adicional de cerca de 3,6 s
por Apply na KB grande.

São três passos, e **os três são obrigatórios juntos**: tirar o `readonly` de `_procedures`;
acrescentar o método; e **chamá-lo nos dois fluxos que gravam Procedures**, logo após a respectiva
fase, exatamente como `RefreshSdts` já é chamado após a fase de SDTs —
`kbIndexForApply.RefreshSdts(...)` em `Package.cs:1430` no Apply e `syncKbIndex.RefreshSdts(...)`
em `Package.cs:767` no Sincronizar. Um `RefreshProcedures` só no Apply deixaria o Sync com mapa
desatualizado.

O repositório tem **três** call sites de refresh, não dois: além desses dois de `RefreshSdts`,
há `kbIndex.RefreshFolders(designModel)` dentro de `ApiPlanSdtWriter` — chamado pelo writer, não
pelo handler, e é ele que impede o segundo `CreateOrReencounter` de tentar criar `GxOpenAPI` de
novo. Ao procurar os refreshes, varra `Src/` inteiro: limitar a busca a `Package.cs` esconde esse
terceiro, que é justamente o que a 1A não pode remover.

Criar o método sem os call sites é o erro natural aqui — e o mais perigoso, porque as buscas
passariam a consultar um mapa que continua sem as Procedures recém-criadas, com o mesmo efeito de
não ter refresh nenhum, mas com aparência de resolvido. Se optar por não fazer os três, mantenha
as três buscas em leitura corrente.

### A2 — As cinco origens de criação do índice, e as quatro criações observadas

Distinção que a redação anterior confundia: abaixo estão **cinco pontos de código** capazes de
criar um índice; a telemetria observou **quatro criações** por Apply, porque nem toda origem
dispara em toda execução. O alvo da correção são as origens, não a contagem.

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

**A telemetria não substitui verificação estática.** Ela mostra o que executou, não o que deixou
de ser alcançado numa execução específica. Ao fim da Etapa 1A deve existir um teste que leia o
fonte e falhe se `ApiPlanKbObjectNameIndex.Create` aparecer fora das fronteiras declaradas. É um
lint pequeno, com lista fechada de call sites permitidos, e não é o manifesto de todos os `GetAll`
que este plano cortou: verifica uma única chamada, não uma matriz.

**A lista tem de ser por símbolo, não por arquivo.** Liberar «qualquer `Create` dentro de
`ApiPlanGenerationStateReader`» não serviria: **há dois** ali — o de
`ReadForIntentionalChangeWithIndex`, que é a origem legítima, e o de `ReadForIntentionalChange`,
que é justamente a criação supérflua a eliminar. Um lint por classe passaria com a duplicação
intacta. Os permitidos são, nominalmente: `ReadForIntentionalChangeWithIndex` e os dois Previews
de `Package.cs` (Sync e Remover), que são fases distintas por decisão explícita.

### A3 — Símbolos que varrem o catálogo em laço

**Atributos** — a maior fatia, cerca de 50 s por Apply quando a PK tem 2 partes e há 2 filtros:

- `ApiPlanBusinessComponentWriter.EnsureAttributeExists`, chamado por `TrySetAttributeBasedOn`
- `ApiPlanListProcedureWriter.EnsureAttributeExists`

Ambos recebem apenas `KBModel model`. O índice **já expõe** `FindAttributes(string)` e
`TryGetSingleAttribute(string, out GxAttribute)`. **`TryGetSingleAttribute` já tem consumidor:**
`ApiPlanSdtWriter.EnsureAttributeExists` o usa. Os dois `EnsureAttributeExists` de Business
Component e de List é que ainda varrem — são esses os alvos da 1A, e o do SdtWriter é o modelo
pronto a copiar, não algo a mexer.

**Preflights que varrem uma vez por objeto, separados pelo que importa — se o tipo já foi mutado
quando eles rodam:**

| Símbolo | Fase | Varre | Cabe na 1A? |
|---|---|---|---|
| `ApiPlanProcedureWriter.PreflightProcedures` | Procedures, **antes** de criá-las | `Procedure.GetAll` | **Sim**, índice inicial serve |
| `ApiPlanApiObjectWriter.PreflightRequiredSdts` | API Object | `SDT.GetAll` | **Sim**, após o `RefreshSdts` existente |
| `ApiPlanBusinessComponentWriter.EnsureSdts` | Business Component | `SDT.GetAll` | **Sim**, idem |
| `ApiPlanApiObjectWriter.PreflightRequiredProcedures` | API Object | `Procedure.GetAll` | **Não** — Procedures já gravadas, sem refresh |
| `ApiPlanBusinessComponentWriter.FindProcedure` | Business Component | `Procedure.GetAll` | **Não** — idem |
| `ApiPlanListProcedureWriter.FindListProcedure` | List | `Procedure.GetAll` | **Não** — idem |

Em `Empresa`, `PreflightRequiredSdts` e `EnsureSdts` fizeram 47 varreduras cada — são o maior
ganho desta tabela, e são seguros porque o `RefreshSdts` já roda entre a gravação dos SDTs e essas
fases.

Os três «Não» só entram se um `RefreshProcedures` for criado (ver A1). Ligá-los ao índice sem esse
refresh **quebra o Apply de geração nova**: as Procedures acabaram de ser criadas e não estão no
mapa, e o writer aborta com «Procedure requerida não foi reencontrada». O reencontro — reaplicar
sobre API existente — mascararia o defeito, porque aí as Procedures já constam do índice inicial.

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

`ShowFinalReport` abre `ApiPlanScanProbe.Suspend()` no início: a apresentação do relatório roda
dentro do escopo do Sync, mas não faz parte da operação medida, e `TryResolveMainObjectFromKb`
consulta a KB dali. Sem a suspensão, uma leitura de relatório seria contada como custo do Sync.
Ao acrescentar consulta nova em qualquer ponto de apresentação, verifique se ela cai dentro de um
escopo de medição que não lhe pertence.

**O instrumento tem teste próprio:** `Tests/ScanProbe/Test-ApiPlanScanProbe.ps1`, registrado no
orquestrador como `tests.scanProbe`. Cobre valor devolvido com e sem escopo, callback de
encerramento exatamente uma vez sob `Dispose` repetido, escopos aninhados restaurando o anterior,
`Suspend` zerando e restaurando a medição, e exceção no callback sem derrubar o fluxo nem
inutilizar a medição seguinte. Ele existe porque um vazamento de escopo atribuiria a uma operação
o custo de outra **em silêncio**, contaminando justamente os números que sustentam este plano.
Foi validado por mutação: quebrar o `Suspend` faz a asserção correspondente falhar.

Procedimento, na ordem:

1. `dotnet build-server shutdown` e build Release pelo procedimento do repositório;
2. fechar a IDE por completo;
3. `Install-ExtensionForGeneXus18.bat` como administrador, na raiz — sem `genexus /install`,
   a menos que o manifesto ou o registro tenham mudado;
4. reabrir a IDE e executar, em cada uma das três transações, Apply, Sincronizar e Remover;
5. coletar do Output as linhas `[B082] Apply ...`, `[B082] Sync ...`, `[B082] Sync Preview ...`,
   `[B082] Remover ...` e `[B082] Remover Preview ...`, e comparar com as tabelas da seção
   «Medições».

Os Previews de Sync e Remover têm **escopo de medição próprio**, aberto no handler antes da casca
de progresso e fechado antes da confirmação. São fase distinta: criam o próprio índice e não devem
ter o custo somado ao da escrita. No Remover, a exclusão continua com telemetria própria, passada
por parâmetro; o helper interno cai no probe de escopo ambiente apenas quando não a recebe, que é
justamente o caminho do Preview.

**Sobre o Sync:** ele não era instrumentado até 2026-09-02 — `ApiPlanScanProbe.Begin` existia
apenas no fluxo do Wizard. O escopo foi acrescentado, publicando ao encerrar para cobrir os vários
pontos de retorno do handler, e o Sync **que de fato escreve já foi medido** no mesmo dia, na KB
pequena: 84 varreduras, 1,9 s. Ver «O Sync que grava» na seção «Medições».

Um Sync sem diferenças retorna antes da fase de escrita e produz apenas as sete varreduras do
Preview. Para exercitar o caminho que grava, altere a Transaction depois de gerada a API —
acrescentar um atributo basta.

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
- **O Sync que escreve na KB grande.** Ele foi medido na KB pequena (84 varreduras, 1,9 s de
  8,6 s), o que basta para confirmar que repete o padrão do Apply, mas **não há linha de base dele
  na `Fabrica Brasil Test`** — e é lá que as metas valem. Ver a regra de aceite do Sync na seção
  «Critérios de aceite».
- **O custo do `Save()` por tipo de objeto.** Sabemos que em `Empresa` a criação de 44 SDTs
  levou 30 s — cerca de 685 ms cada — mas não instrumentamos as mutações individualmente.
- **A causa dos 7 s de montagem de interface** na abertura do Wizard de `DocumentoFiscal`.
- **A reentrância acontecendo de fato.** A D1 é comprovada por leitura de código e pela
  possibilidade de mexer no menu durante a operação; nunca foi reproduzida até a corrupção.
