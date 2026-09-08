# B111 · Fase F2 — seam de persistência e recibos

**Sprint:** S-B111 — gravação única do API Object.
**Fase:** F2 de 3 (F1 ordem e writer final · **F2 seam e recibos** · F3 durabilidade e remoção).
**Data:** 2026-09-04. **Item:** `B111` em `Docs/Foundation/06-BACKLOG_v0.1.md`.

**Status:** decisões de escopo e contrato da F2 consolidadas em 2026-09-07; o plano ainda
não foi implementado. A revisão por pares da sprint não está encerrada. **Não** autoriza
alteração de código, instalação, commit ou push.

**Pré-requisito:** F1 aceita
(`Docs/Implementation/2026-09-04-B111-F1-PLANO-ORDEM-E-WRITER-FINAL.md`). A F2 não
reordena gravações nem muda quem é o writer final; ela torna observável e testável a
ordem que a F1 estabeleceu.

---

## 1. O problema que a F2 resolve

A F1 entrega a ordem correta, mas a verifica por instrumentação do próprio caminho de
gravação — o que ela mesma declara como limitação. Duas consequências ficam abertas:

1. **Não há prova executável de que a ordem se mantém.** Uma sentinela textual encontra
   um `Save()` fora de lugar; ela não prova que, numa execução real, o API foi gravado
   depois dos consumidores, nem quantas vezes foi gravado.
2. **Não há como distinguir modos de falha.** Hoje, uma falha antes do Save, um Save que
   retornou erro e um Save que concluiu mas cuja confirmação não pôde ser lida chegam ao
   Apply como a mesma coisa: uma exceção ou um `false`. A F3 depende dessa distinção para
   decidir o que fazer; sem ela, qualquer recuperação seria adivinhação.

A F2 fecha as duas coisas e para aí.

---

## 2. Estado de partida

### 2.1 Os pontos de gravação reais

Excluídas as sondas e o código GeneXus emitido como string, o mapa consolidado tem
**15 chamadas físicas de `Save()`** e dois pontos de execução de laço que transportam
essas chamadas em BC e List. Os laços não são Saves adicionais e não podem ser somados
às chamadas físicas.

| Arquivo:linha | O que grava |
|---|---|
| `ApiPlanTransactionFolder.cs:45` | Folder da Transaction |
| `ApiPlanSdtWriter.cs:230` | Folder compartilhado |
| `ApiPlanSdtWriter.cs:266` | SDT reencontrado |
| `ApiPlanSdtWriter.cs:302` | SDT novo |
| `ApiPlanProcedureWriter.cs:221` | Procedure reencontrada |
| `ApiPlanProcedureWriter.cs:235` | Procedure nova |
| `ApiPlanApiObjectWriter.cs:707` | API reencontrado |
| `ApiPlanApiObjectWriter.cs:718` | API novo |
| `ApiPlanBusinessComponentWriter.cs:111` | laço de `saveSteps` |
| `ApiPlanBusinessComponentWriter.cs:560` | `SaveProcedure` |
| `ApiPlanBusinessComponentWriter.cs:626` | `SaveApi` |
| `ApiPlanListProcedureWriter.cs:72` | laço de `saveSteps` |
| `ApiPlanListProcedureWriter.cs:953` | `SaveProcedure` |
| `ApiPlanListProcedureWriter.cs:975` | `SaveApi` |
| `ApiPlanMetadataFileWriter.cs:98` | metadata B060 |
| `ApiPlanOrphanMetadataRecovery.cs:261` | File de metadata reconstituído pela recuperação B115 |
| `Package.cs:1566` | `transaction.Save()` para habilitar BC, em `EnableBusinessComponentForWizard` |

Além das 15 chamadas físicas de `Save()`, o remover possui hoje cinco pontos físicos de
`Delete()`: API Object, Procedure, SDT, File de metadata e Folder. O `Delete()` do SDT
é chamado dentro da fila de passadas; cada tentativa física continua sendo um recibo
independente. O invólucro de relatório não substitui a instrumentação do `Delete()`.

### 2.2 Dois padrões que o repositório já resolveu

**Proto-seam nos writers de consumidor.** `ApiPlanBusinessComponentWriter.cs:98` e
`ApiPlanListProcedureWriter.cs:66` já montam `saveSteps` como lista de
`(Label, Action Save)` e a executam em laço com progresso e cronômetro por passo. Falta
pouco para um seam: identidade e tipo do objeto, resultado da operação, ordem monotônica
global e um único laço em vez de dois duplicados.

**Escopo ambiente em vez de parâmetro propagado.** `ApiPlanScanProbe` resolveu, para a
telemetria de varredura, exatamente o problema que um seam enfrentaria: instrumentar
dezenas de pontos sem propagar um parâmetro por dezenas de assinaturas. Usa estado
`[ThreadStatic]`, ativado por `Begin(...)`, com custo zero e comportamento idêntico
quando não há escopo ativo, porque todo o fluxo roda na thread da UI.

**O seam da F2 deve seguir esse padrão, não inventar outro.** Isso evita um refactor de
assinaturas que atravessaria os writers inteiros — que é justamente o tipo de mudança
ampla que motivou fatiar esta sprint.

O `file.Save()` da recuperação B115 também está no escopo gerenciado da S-B111. A
confirmação desse ponto deve validar a identidade persistida do File e os bytes esperados;
uma releitura que selecione apenas pelo nome não é confirmação suficiente para o contrato
da sprint.

**Teste executável offline já tem precedente.** `Tests/ScanProbe/Test-ApiPlanScanProbe.ps1`
compila o próprio arquivo de produção com `Add-Type -Path` e exercita a classe fora da
IDE. O mesmo caminho serve para o seam.

---

## 3. Escopo da F2

### 3.1 O que entra

1. um seam de persistência de escopo ambiente, no padrão de `ApiPlanScanProbe`, promovendo
   `ApiPlanSaveBoundaryProbe`;
2. `PersistenceReceipt`, com o conteúdo da seção 4.2;
3. a classificação de falha em C, D e E da seção 4.3;
4. unificação dos dois laços de `saveSteps` num único executor;
5. a habilitação de BC diferida no Wizard, que a F1 adiou por depender de recibo;
6. testes executáveis que injetam falha antes, falha depois e resultado ambíguo em todos os
   `Save()` e `Delete()` gerenciados, incluindo a recuperação B115;
7. exposição da sequência de recibos no relatório final e na trilha da Output.

### 3.2 O que **não** entra

| Fora da F2 | Vai para |
|---|---|
| diário B111 durável | F3 |
| três dimensões de estado, `JournalDurabilityUnknown` | F3 |
| recuperação explícita, reconciliação e inventário físico | F3 |
| remoção de API legado | F3 |
| implementação do Modo A selecionado e ciclo de vida do diário | F3 |
| mudança de ordem física ou de writer final | já feita na F1 |

A F2 **não** promete recuperação. Ela promete que, quando algo falhar, o relatório dirá
com precisão o que foi gravado, o que não foi e o que ficou indeterminado. Decidir o que
fazer com isso é a F3.

### 3.3 Um ponto refutado que esta fase reabre, e por quê

O plano aprovado de 2026-09-04 **refutou** a extração de um seam testável, com este motivo:
não existe projeto de testes .NET onde exercitá-lo, e criar essa infraestrutura seria outra
frente. A refutação está no apêndice daquele plano e continua correta **nos termos em que
foi feita**.

A F2 reabre o ponto por um caminho que aquela análise não considerou, e que já existe no
repositório:

- o seam não exige refactor de assinaturas, porque usa escopo ambiente, como
  `ApiPlanScanProbe` (2.2);
- o teste não exige projeto .NET, porque compila o arquivo de produção com `Add-Type`, como
  `Tests/ScanProbe/Test-ApiPlanScanProbe.ps1`.

Isto está declarado aqui para que a reabertura seja **consciente e verificável**, não uma
reintrodução silenciosa de ponto já descartado. Se o painel de revisão discordar de que
esses dois precedentes bastam, a refutação original prevalece e a F2 precisa ser
redesenhada.

---

## 4. Contrato-alvo

### 4.1 O ponto comum de persistência

O único ponto comum será a classe existente `ApiPlanSaveBoundaryProbe`. Aqui, “ponto
comum” é a fronteira técnica por onde passam as operações reais de persistência para
serem registradas e confirmadas; não é um objeto da KB nem um novo tipo GeneXus.

A F2 amplia essa classe, em vez de criar um `ApiPlanPersistenceProbe` paralelo. Ela
preserva os eventos atuais de Pump/Save e passa a ser também a dona dos recibos de
`Save` e `Delete`.

Contrato mínimo:

- `Begin(ApiPlanPersistenceLog log)` abre escopo `[ThreadStatic]` e devolve `IDisposable`;
  escopos aninhados restauram o anterior;
- `Persist(string operationKind, string objectType, string stage, string plannedName,
  Guid? plannedGuid, Action persist, Func<PersistenceConfirmation> confirm)` executa
  `Save` ou `Delete`, mede a operação e registra o recibo quando há escopo ativo;
- a confirmação é obrigatória para todo ponto de produção. Não existe sobrecarga sem
  `confirm`: para `Save`, ela relê o objeto esperado; para `Delete`, confirma a ausência
  do objeto pelo `Guid` planejado;
- `Suspend()` interrompe apenas a captura de trechos que rodam dentro da operação mas
  não pertencem a ela, como a apresentação do relatório final.

Regras:

1. sem escopo ativo, o comportamento é idêntico ao código não instrumentado;
2. o seam nunca altera o resultado da gravação nem engole exceção — ele registra e
   relança;
3. a publicação do log nunca pode derrubar o fluxo medido;
4. o ponto comum não decide nada: não bloqueia, não repete, não escolhe writer;
5. a tentativa sem confirmação não é um sucesso: o ponto não pode ser considerado
   coberto pela F2 até que sua leitura de confirmação exista.
6. o recibo final de cada tentativa deve ser devolvido ou exposto ao chamador,
   inclusive quando o delegate lança e a exceção original é relançada; a F3 não pode
   deduzir o resultado pela mensagem da exceção. O canal pode ser um resultado ou um
   handle associado à tentativa no log, mas deve permitir que o executor de `Remove`
   leia `Confirmed`, `Failed` retryable ou `OutcomeUnknown`.

### 4.2 O recibo

Cada `PersistenceReceipt` carrega:

- ordem monotônica global dentro da operação;
- operação (`Save` ou `Delete`), etapa e tipo de objeto;
- nome planejado e, quando houver, `Guid` planejado;
- nome e `Guid` persistidos, ou ausência confirmada no caso de `Delete`;
- início, fim e duração;
- resultado: `Confirmed`, `Failed` ou `OutcomeUnknown`;
- `retryEligible`, booleano que só pode ser verdadeiro para uma falha retryable de
  `Delete`;
- `retryableReason`, enum fechado, inicialmente `StillPresentAfterDelete`, ou ausente quando
  `retryEligible` for falso;
- exceção ou cancelamento, quando houver;
- se houve leitura de confirmação e qual foi seu resultado.

O recibo é registro, não decisão. Nada no código deve mudar de caminho por causa de um
recibo dentro da F2.

### 4.3 As três classes de falha

| Classe | Situação | Resultado do recibo |
|---|---|---|
| **C** | falhou **antes** de chamar `Save()` ou `Delete()` | `Failed`, sem efeito produzido |
| **D** | `Save()` ou `Delete()` lançou ou foi cancelado; em `Delete`, uma releitura imediata ainda pode provar o objeto presente ou a ausência | `OutcomeUnknown`, exceto por `Failed` retryable quando presente ou `Confirmed` quando ausente |
| **E** | a operação retornou; em `Delete`, a confirmação pode provar ausência, provar que o objeto ainda está presente, ou ser ilegível, divergente ou ambígua | `Confirmed` quando ausente; `Failed` retryable quando presente; caso contrário, `OutcomeUnknown` |

Um `Save()` ou `Delete()` que lançou **não** pode ser classificado como “não produziu
efeito” sem confirmação. Há uma exceção controlada para `Delete`: se a releitura imediata
por identidade comprovar que o objeto ainda existe, seja porque o `Delete()` lançou ou
porque retornou sem removê-lo, o recibo será `Failed` com
`retryableReason=StillPresentAfterDelete`; isso é falha conhecida, não `OutcomeUnknown`, e só
autoriza nova tentativa dentro do mesmo `Remove`. Se a releitura confirmar ausência, o
`Delete` é `Confirmed`; se for ilegível, divergente ou ambígua, permanece
`OutcomeUnknown` e não pode ser repetido.

#### Como cada classe é produzida, concretamente

Um delegate `Action save` sozinho **não** distingue as três classes. O contrato precisa ser
explícito, senão os testes da seção 6.3 não têm o que exercitar.

**Ciclo de vida do recibo dentro de `Persist`:**

1. o recibo é criado e registrado no log **antes** de o delegate ser invocado, com resultado
   `Started` e a ordem monotônica já atribuída;
2. o delegate é invocado;
3. se lançar — inclusive `OperationCanceledException` —, registra a exceção e relança sem
   alteração. Para `Delete`, uma releitura imediata por identidade pode classificar o
   resultado como `Failed` retryable quando o objeto ainda existir, tenha o delegate
   lançado ou retornado sem removê-lo, ou `Confirmed` quando estiver ausente; releitura
   ilegível ou divergente produz `OutcomeUnknown` sem retry.
   Para `Save`, lançamento ou cancelamento permanece `OutcomeUnknown`;
4. se retornar, roda a leitura de confirmação obrigatória do ponto;
5. confirmação bem-sucedida e coerente → `Confirmed`; para `Delete`, ausência comprovada
   → `Confirmed`, presença comprovada do alvo → `Failed` retryable, e releitura ilegível,
   divergente ou ambígua → `OutcomeUnknown`, com a divergência registrada. Para `Save`,
   confirmação ausente, divergente ou ilegível permanece `OutcomeUnknown`.

Um recibo que permaneça em `Started` ao fim da operação é, por si, um sinal: significa que o
processo morreu dentro daquela gravação.

**A classe C não é observável pelo seam** — se a falha ocorre antes de chamar `Persist`, o
seam não é invocado e não há recibo. Ela é registrada pelo **orquestrador**, com um método
próprio do log, do tipo `NoteStageFailed(stage, reason)`, chamado onde hoje já existe o
teste de resultado de cada etapa. Sem isso, “etapa falhou antes de gravar” seria
indistinguível de “etapa nunca foi selecionada”, que é uma distinção que o relatório precisa
fazer.

**Correção herdada: “timeout” não é observável.** O manuscrito expandido falava em timeout
como uma das origens de resultado indeterminado. O SDK não expõe timeout nas operações de
gravação; o que existe, e é observável, é **exceção** e **cancelamento**. A classe D fica
definida por esses dois mais “confirmação impossível de ler”. Os testes injetam exceção e
cancelamento — não simulam um timeout que a API não produz.

**Ponto de injeção da classe C nos testes:** o log expõe `NoteStageFailed`, então o teste
chama o orquestrador com uma etapa configurada para falhar na preparação, antes de qualquer
`Persist`, e verifica que o relatório distingue as três classes.

A distinção C × D também exige que o delegate contenha **somente** a gravação: preparação de
conteúdo fica fora do trecho medido, ponto a ponto.

### 4.4 Granularidade — evitar contagem dupla

Em BC e List, `SaveApi` e `SaveProcedure` são chamados **de dentro** dos `saveSteps`.
Instrumentar os dois níveis contaria cada gravação duas vezes e produziria uma sequência
de recibos falsa.

Regra: **o ponto comum envolve o passo, não a função interna.** Depois de unificar os dois
laços num executor único, existe um só lugar que chama `Persist(...)` para esses writers.
`ApiPlanSdtWriter`, `ApiPlanProcedureWriter`, `ApiPlanApiObjectWriter`,
`ApiPlanTransactionFolder`, `ApiPlanMetadataFileWriter` e o `transaction.Save()` de
`Package.cs` são instrumentados no próprio ponto. Os cinco `Delete()` físicos de
`ApiPlanGeneratedApiRemover` seguem a mesma regra, inclusive a tentativa de cada passada
do SDT.

Uma sentinela deve falhar se um `Save()` ou `Delete()` de produção aparecer fora de um
`Persist(...)`,
e outra deve falhar se um ponto for instrumentado nos dois níveis.

### 4.5 O log sobrevive aos retornos antecipados

`Package.cs` tem **22** chamadas a `ShowFinalReport` e, só no Apply do Wizard, **8** pontos
de `return` após falha ou bloqueio de etapa. Se a sequência de recibos dependesse do
`Dispose` do escopo para ser publicada, o relatório sairia sem os últimos recibos em
exatamente os casos que mais importam — os de falha.

Contrato:

1. o `ApiPlanPersistenceLog` é instanciado **antes** de abrir o escopo, e é uma variável
   local do Apply;
2. `Begin(log)` só ativa a captura; o log não pertence ao escopo e não é publicado por ele;
3. `ShowFinalReport` recebe o log **diretamente**, em todos os 22 pontos de chamada, e por
   isso funciona em qualquer retorno antecipado;
4. o escopo continua sendo `using`, para garantir restauração do estado `[ThreadStatic]`
   mesmo em exceção;
5. a apresentação do relatório roda dentro de `Suspend()`, como já faz a telemetria de
   varredura, para que as leituras do próprio relatório não entrem na sequência medida.

Uma sentinela deve falhar se algum `ShowFinalReport` de um fluxo gerenciado pela frente for
chamado sem o log.

### 4.6 Habilitação de BC diferida no Wizard

A F1 manteve, no Sync, o bloqueio de preflight quando BC é pedido sem a Transaction
habilitada, e adiou o caso do Wizard. Com recibo disponível, a F2 fecha:

1. abrir o diálogo **não** salva a Transaction;
2. a seleção registra `PendingBusinessComponentEnablement`;
3. o gate verifica a mutação pendente;
4. `transaction.Save()` ocorre como etapa planejada, depois do gate, com recibo;
5. falha ou resultado indeterminado dessa etapa interrompe o pipeline antes do API.

No Sync nada muda: continua bloqueando antes de qualquer gravação.

---

## 5. Mudanças previstas por arquivo

| Arquivo | Mudança |
|---|---|
| `Diagnostics/ApiPlanSaveBoundaryProbe.cs` (existente) | único ponto comum; preservar eventos B109 e absorver recibos de `Save` e `Delete` |
| `Diagnostics/ApiPlanPersistenceLog.cs` (novo) | coleção ordenada de recibos, no espírito de `ApiPlanScanTelemetry` |
| `Diagnostics/ApiPlanSaveStepExecutor.cs` (novo) | executor único dos `saveSteps`, hoje duplicado |
| `ApiPlanBusinessComponentWriter.cs`, `ApiPlanListProcedureWriter.cs` | passar a usar o executor único |
| `ApiPlanSdtWriter.cs`, `ApiPlanProcedureWriter.cs`, `ApiPlanApiObjectWriter.cs`, `ApiPlanTransactionFolder.cs`, `ApiPlanMetadataFileWriter.cs` | envolver cada `Save()` em `Persist(...)` |
| `ApiPlanOrphanMetadataRecovery.cs` | envolver o `file.Save()` da recuperação B115 em `Persist(...)`, com confirmação de identidade e bytes |
| `ApiPlanGeneratedApiRemover.cs` | envolver cada `Delete()` físico em `Persist(...)`, com confirmação obrigatória de ausência |
| `Package.cs` | abrir o escopo por operação; `transaction.Save()` recibado; habilitação de BC diferida no Wizard; sequência de recibos no relatório |
| `ApiPlanApplicationFinalReport.*` | sequência de recibos e contagem por tipo |
| `Tests/PersistenceProbe/` (novo) | teste executável do seam, no padrão de `Tests/ScanProbe/` |

---

## 6. Testes da F2

### 6.1 Teste executável do ponto comum

No padrão de `Tests/ScanProbe/Test-ApiPlanScanProbe.ps1`, compilando os arquivos de
produção com `Add-Type -Path` e exercitando fora da IDE:

1. sem escopo ativo, `Persist` apenas executa o delegate e não registra nada;
2. com escopo, registra ordem monotônica, tipo, nome e duração;
3. exceção dentro do delegate de `Save` é registrada como `OutcomeUnknown`, com
   `retryEligible=false`, e
   **relançada**; em `Delete`, a confirmação pós-tentativa exercita presença
   (`Failed` retryable, `retryEligible=true`, `retryableReason=StillPresentAfterDelete`),
   ausência (`Confirmed`, `retryEligible=false`) e releitura indeterminada
   (`OutcomeUnknown`, `retryEligible=false`), sempre relançando a exceção original
   quando houver;
4. falha antes da chamada é registrada como `Failed`;
5. leitura de confirmação divergente produz `OutcomeUnknown` com a divergência;
6. confirmação ausente não possui sobrecarga válida e não passa no contrato;
7. `Delete` confirmado pela ausência produz `Confirmed`;
8. escopos aninhados restauram o anterior;
9. `Suspend()` não encerra o escopo ativo;
10. falha na publicação do log não derruba o fluxo medido.

### 6.2 Contagem e ordem por fluxo

Com o seam injetado, cada fluxo da matriz da F1 passa a ter prova executável:

| Fluxo | Exigido |
|---|---|
| API-only | exatamente um recibo de API, em B054 |
| BC-only | exatamente um recibo de API, no BC, **depois** dos recibos das Procedures |
| List-only | exatamente um recibo de API, no List, depois do recibo da Procedure |
| BC + List | exatamente um recibo de API, no List; nenhum recibo de API no BC |
| Sync, todas as combinações **alcançáveis** | mesma sequência do Wizard equivalente |
| `GenerateApiObject=false` — **Wizard-only** | nenhum recibo de API |
| SDT/Procedure `false` — **Wizard-only** | nenhum recibo da etapa desmarcada |

As duas últimas linhas são **exclusivas do Wizard**. O `BuildSelection` do Sync monta um
perfil fixo com `GenerateApiObject`, `GenerateSdts`, `GenerateProcedures` e
`GenerateMetadata` sempre verdadeiras, de modo que “todas as combinações” do Sync são apenas
as quatro de BC/List. Ver a seção 4.6 da F1, que é a fonte desta regra: não criar falsa
paridade copiando testes do Wizard para uma UI que não oferece esses toggles.

Isto substitui, com execução, a instrumentação frágil que a F1 declarou como limitação.

### 6.3 Falha em cada fronteira

Para cada ponto de `Save` e `Delete`, injetar as três classes e validar a sequência de
recibos resultante:

1. falha antes de Folder, SDT, Procedure, API, metadata e recuperação B115;
2. falha depois de cada `Save` físico, inclusive `ApiPlanOrphanMetadataRecovery`;
3. resultado ambíguo em cada `Save` físico;
4. cancelamento durante cada `Save` físico;
5. falha, cancelamento e confirmação divergente em cada `Delete` do remover;
6. recuperação B115 com File novo e File reutilizado, confirmando identidade e bytes;
7. ausência de confirmação em qualquer ponto é rejeitada pelo contrato;
8. falha e resultado ambíguo no `transaction.Save()` da habilitação de BC.

O que se valida aqui é **descrição**, não recuperação: que o relatório diga corretamente o
que foi gravado, o que não foi e o que ficou indeterminado. Nenhum teste da F2 exige que o
sistema se recupere.

---

## 7. Validação na IDE

Reinstalar a DLL conforme a política do repositório e validar depois dela.

1. repetir os fluxos da F1 e conferir que a sequência de recibos aparece no relatório e na
   Output, com um único recibo de API por aplicação;
2. Wizard com habilitação de BC pendente: confirmar que abrir o diálogo não grava, e que a
   Transaction é salva depois do gate, com recibo;
3. cancelar um Apply no meio e conferir que o relatório distingue o que foi gravado do que
   não foi tentado;
4. comparar o tempo de Apply na KB grande antes e depois: o seam não deve acrescentar
   custo mensurável, já que não faz I/O.

---

## 8. Critérios de aceite

1. Todo `Save()` e `Delete()` de produção do pipeline passa por `Persist(...)`; uma
   sentinela falha se algum ficar fora.
2. Nenhum ponto é instrumentado em dois níveis; uma sentinela falha se houver contagem
   dupla.
3. Sem escopo ativo, o comportamento é idêntico ao anterior.
4. O ponto comum nunca engole exceção nem altera o resultado de uma persistência.
5. As classes C, D e E são distinguíveis no recibo e no relatório, e testadas por injeção.
6. A contagem de um único `API.Save()` por fluxo é provada por execução, não por texto.
7. Os dois laços de `saveSteps` foram unificados num executor único.
8. No Wizard, abrir o diálogo não salva a Transaction, e a habilitação ocorre depois do
   gate, com recibo.
9. O relatório final expõe a sequência de recibos.
10. Nenhuma regressão nos fluxos de Sync, Wizard, remoção e relatório.

---

## 9. Riscos

| Risco | Mitigação prevista |
|---|---|
| contagem dupla em BC e List, produzindo sequência de recibos falsa | regra explícita de granularidade em 4.4, mais sentinela dedicada |
| unificar os dois laços alterar comportamento de progresso ou de cancelamento | o executor único deve preservar `Report`, `Pump` e `ThrowIfAbortRequested` como estão hoje; cobrir com fluxo executável antes de mexer em qualquer outra coisa |
| distinção C × D depender de o delegate conter só a gravação | manter preparação de conteúdo fora do trecho medido; revisar ponto a ponto |
| escopo `[ThreadStatic]` não cobrir execução fora da thread da UI | mesma limitação já aceita em `ApiPlanScanProbe`: o que roda fora simplesmente não é medido, nunca medido errado |
| o seam virar porta de entrada para lógica de decisão | critério 4 e revisão: recibo é registro, decisão é F3 |

---

### 9.1 Consolidação de 2026-09-07

- O seam cobre todo `Save()` e `Delete()` gerenciado pela S-B111, inclusive o `file.Save()`
  do B115; o `Save()` do File de preferências da KB fica fora do escopo operacional.
- Cada recibo expõe o `OperationId` da operação corrente. O recibo é observação: a F2 não
  decide requeue, recuperação ou continuação.
- `StillPresentAfterDelete` é o único motivo retryable aprovado para `Delete`: a releitura
  por identidade comprovou que o alvo continua presente. Ausência confirmada é sucesso;
  leitura ambígua é `OutcomeUnknown` e não autoriza retry.
- A exceção original continua sendo relançada. A F3 consome o recibo final pelo canal
  exposto pelo seam, sem interpretar texto de exceção.

## 10. Fontes

- `Docs/Implementation/2026-09-04-B111-F1-PLANO-ORDEM-E-WRITER-FINAL.md` (pré-requisito)
- `Docs/Implementation/2026-09-04-B111-SONDAS-IDENTIDADE-E-DIARIO.md` (contagem real de `Save()`)
- `Docs/Implementation/2026-09-04-PLANO-API-OBJECT-GRAVACAO-UNICA.md` (plano aprovado em 2026-09-04; **refuta** o seam por não haver projeto de testes .NET — ver 3.3)
- `Docs/Implementation/2026-09-04-B111-MANUSCRITO-EXPANDIDO-V24.md` (expansão nunca aprovada; origem das exigências de seam e de falhas C/D/E)
- `Src/Extension/Diagnostics/ApiPlanScanProbe.cs` e `ApiPlanScanTelemetry.cs` (padrão a seguir)
- `Tests/ScanProbe/Test-ApiPlanScanProbe.ps1` (padrão de teste executável offline)
- as 15 chamadas físicas e os dois laços de gravação descritos na seção 2.1
