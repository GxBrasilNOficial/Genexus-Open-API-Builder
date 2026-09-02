# B082 — Plano de hardening e desempenho (2026-09-02)

## Propósito e relação com documentos anteriores

Este plano substitui a série de manuscritos `B082 v1`–`v20`, mantida em `Temp/revisao-por-pares/`
e nunca versionada. Aquela série cresceu ao longo de vinte rodadas de revisão por leitura de
código, sem nenhuma medição, até 488 linhas de texto normativo.

Em 2026-09-02 a extensão foi instrumentada e medida em uma KB real. A medição inverteu a
prioridade da frente, cortou cerca de um terço do que estava planejado e corrigiu três decisões
que a `v20` dava como fechadas. Este documento registra o que ficou de pé, com o número que
sustenta cada escolha.

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

### Hipotética — sem caso observado

**D10 — Remoção indevida do Folder.** A `v20` dedicou espaço extenso a endurecer a regra de
posse. Não foi encontrado caso concreto: o código já exige nome único, descrição canônica ou
legada da extensão, e pasta comprovadamente vazia. Endurecimento preventivo, não dor observada.

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
Folder da Transaction. Isso responde, sem smoke separado, o gate que a `v20` exigia antes de
aceitar a ordem Folder → File.

## Decisões fechadas

1. **O índice é construído uma vez e mantido**, ajustado conforme a própria extensão apaga ou
   cria. Revoga a regra da `v20` que proibia usar o índice depois da primeira exclusão. Base: a
   IDE não expõe edição de File; alterá-lo exige manipulá-lo fora da IDE e reimportar por um
   fluxo manual de vários cliques, incompatível com o intervalo de uma operação — e a guarda de
   operação única fecha o caminho pelo qual a própria extensão poderia interferir.
2. **Os writers passam a usar o mapa de atributos que já existe.** `ApiPlanKbObjectNameIndex` já
   constrói o mapa (`GxAttribute` é alias de `Artech.Genexus.Common.Objects.Attribute`) e expõe
   `FindAttributes` e `TryGetSingleAttribute`, sem nenhum consumidor.
3. **As confirmações pós-`Delete` passam a ser agregadas ao fim**, uma varredura por tipo em vez
   de uma por objeto. Perde-se a parada precoce, que protege pouco: os objetos já apagados não
   voltam, e nenhuma falha silenciosa de `Delete()` foi observada em 75 exclusões medidas.
4. **A ordem terminal do Remover passa a ser API → Procedures → SDTs → Folder → metadata File**,
   com o File como última mutação, disponível para revalidação de identidade até o fim. Medido
   como seguro: o File está no `Root Module`, então `IsFolderEmpty` não o conta.
   Cláusula pré-escrita, caso alguma instalação histórica difira: `IsFolderEmpty` desconsidera
   **o GUID do File terminal autorizado** — nunca Files como classe.
5. **A sessão de progresso expõe três primitivos**, não dois:
   `ReportAndCheckBeforeWork`, `ReportAndCheckBeforeMutation` e `ReportCompleted(..., elapsedMs)`.
   O terceiro não verifica abort: um abort observado após um `Delete()` concluído não desfaz
   nada, e lançar ali produziria relatório de "abortado" para mutação já feita. `Report` e
   `Pump` crus ficam proibidos nos caminhos B082, o que torna o lint por intenção verificável.
6. **A guarda de operação única é a correção real da reentrância.** Aumentar a janela de
   progresso é reforço visual, não proteção: não impede Alt+Tab, atalhos de teclado da IDE, nem
   o processamento de eventos, que é o mecanismo da reentrada.
7. **O vínculo Preview → Remove fica enxuto.** O Remove recebe o plano do Preview em memória e,
   antes da primeira exclusão, valida uma vez: Transaction por GUID, metadata File único com
   mesmo GUID e ownership, e SHA-256 dos bytes igual ao capturado no Preview. Divergência
   bloqueia com zero exclusões. Daí em diante, a lista confirmada é a autorização.
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
| Predicado normativo estrito de ownership de Folder | Endurecimento da D10, hipotética. O predicado atual já exige contêiner, descrição canônica ou legada, e pasta vazia |
| Manifesto `Tests/B082/GetAllManifest.json` com lint bidirecional | Inventário de `GetAll` como artefato versionado, quando a instrumentação já mede os mesmos call sites em runtime e com custo. Reavaliar depois da otimização, se ainda fizer sentido |

## Ordem de execução

**Etapa 1 — desempenho.** Índice criado uma vez por operação; writers de Business Component e
List consumindo o mapa de atributos; preflights de API Object, Procedure e SDT consumindo o
índice em vez de varrer em laço; no Remover, índice mantido e confirmações agregadas.
Nenhuma regra de escrita ou exclusão muda.

**Etapa 2 — segurança.** Guarda de operação única nos quatro handlers (`ExecuteOpenWizardStepOne`,
`ExecuteSynchronizeWithTransaction`, `ExecuteRemoveGeneratedApi`, `ExecuteConfigureWizardPreferences`),
viva até o retorno do handler, inclusive durante o relatório final; remoção do `DoEvents()`
aninhado em `OnAbortClicked`; verificação de abort entre o report e a mutação; plano do Preview
entregue ao Remove com validação única; ordem terminal Folder → File.

**Etapa 3 — comunicação e UX.** Fechamento da casca antes do relatório final em todos os
caminhos; correção do `DEMO.md:144-146` e do trecho correspondente do plano de 2026-08-31;
Folder preservado como item estruturado; ancoragem das janelas na tela do owner.

**Fora desta frente, registrado:** a abertura do Wizard (D8) e a responsividade da IDE (D11)
têm causa distinta — montagem de interface e trabalho na thread da UI. Merecem frente própria.

A Etapa 1 vem primeiro por três razões medidas: é a maior melhoria isolada disponível; é a de
menor risco, porque trocar uma varredura por consulta a um mapa de objetos que a extensão nunca
modifica não altera comportamento e um erro quebra o build, não a KB; e ela reduz a janela de
exposição que a D1 explora — cada Apply passa cerca de um minuto a menos dentro do `DoEvents()`.

## Critérios de aceite

**Etapa 1.** Medir de novo `Setor`, `Empresa` e `DocumentoFiscal`, nas três operações, com a
instrumentação ligada, e comparar com a tabela acima. Metas derivadas dos números:

| Operação | Hoje | Meta |
|---|---|---|
| Apply `Setor` | 84,3 s | ≤ 25 s |
| Apply `Empresa` | 161,5 s | ≤ 120 s |
| Apply `DocumentoFiscal` | 187,5 s | ≤ 130 s |
| Remove `Setor` | 12,8 s | ≤ 5 s |
| Remove `Empresa` | 41,2 s | ≤ 12 s |

Além dos tempos: `indice-create` aparece **uma vez** por tipo, não quatro; `bc-find-attribute` e
`list-find-attribute` desaparecem do relatório de varreduras; e os artefatos gerados são
idênticos aos de antes — mesmos nomes, mesma quantidade, mesmo trio API/Procedure/SDT.

**Etapa 2.** Uma segunda entrada durante uma operação longa é recusada, com mensagem localizada
no Output quando ele estiver disponível e sem abrir UI nova quando não estiver. O teste
vinculante é reentrada aninhada na mesma thread. Abortar durante a exclusão não apaga mais
nenhum objeto após o clique. O Remove executa exatamente a lista exibida na confirmação, e uma
divergência de identidade ou de SHA bloqueia com zero exclusões.

**Etapa 3.** O relatório final abre com a casca já fechada em sucesso, erro, bloqueio e abort.
O `DEMO.md` descreve a casca como modeless e afirma que Abortar após o primeiro `Save()` pode
deixar estado parcial. Folder preservado aparece como item preservado, não entre os removidos.
Wizard, progresso, confirmação e relatório abrem na mesma tela da IDE, com a IDE em monitor
secundário.

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
