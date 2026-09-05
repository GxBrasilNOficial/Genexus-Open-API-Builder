# B111 · Fase F2 — seam de persistência e recibos

**Sprint:** S-B111 — gravação única do API Object.
**Fase:** F2 de 3 (F1 ordem e writer final · **F2 seam e recibos** · F3 durabilidade e remoção).
**Data:** 2026-09-04. **Item:** `B111` em `Docs/Foundation/06-BACKLOG_v0.1.md`.

**Status:** plano para decisão humana. **Não** autoriza implementação, alteração de
código, instalação, commit ou push.

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

Excluídas as sondas e o código GeneXus emitido como string, o pipeline tem **16 pontos**
de gravação em código de produção. Vários rodam em laço, de modo que uma aplicação
executa mais que 16 Saves.

| Arquivo:linha | O que grava |
|---|---|
| `ApiPlanTransactionFolder.cs:41` | Folder da Transaction |
| `ApiPlanSdtWriter.cs:227` | Folder compartilhado |
| `ApiPlanSdtWriter.cs:262` | SDT reencontrado |
| `ApiPlanSdtWriter.cs:293` | SDT novo |
| `ApiPlanProcedureWriter.cs:221` | Procedure reencontrada |
| `ApiPlanProcedureWriter.cs:235` | Procedure nova |
| `ApiPlanApiObjectWriter.cs:707` | API reencontrado |
| `ApiPlanApiObjectWriter.cs:718` | API novo |
| `ApiPlanBusinessComponentWriter.cs:118` | laço de `saveSteps` |
| `ApiPlanBusinessComponentWriter.cs:541` | `SaveProcedure` |
| `ApiPlanBusinessComponentWriter.cs:606` | `SaveApi` |
| `ApiPlanListProcedureWriter.cs:79` | laço de `saveSteps` |
| `ApiPlanListProcedureWriter.cs:934` | `SaveProcedure` |
| `ApiPlanListProcedureWriter.cs:955` | `SaveApi` |
| `ApiPlanMetadataFileWriter.cs:98` | metadata B060 |
| `Package.cs:2039` | `transaction.Save()` para habilitar BC |

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

**Teste executável offline já tem precedente.** `Tests/ScanProbe/Test-ApiPlanScanProbe.ps1`
compila o próprio arquivo de produção com `Add-Type -Path` e exercita a classe fora da
IDE. O mesmo caminho serve para o seam.

---

## 3. Escopo da F2

### 3.1 O que entra

1. um seam de persistência de escopo ambiente, no padrão de `ApiPlanScanProbe`;
2. `PersistenceReceipt`, com o conteúdo da seção 4.2;
3. a classificação de falha em C, D e E da seção 4.3;
4. unificação dos dois laços de `saveSteps` num único executor;
5. a habilitação de BC diferida no Wizard, que a F1 adiou por depender de recibo;
6. testes executáveis que injetam falha antes, falha depois e resultado ambíguo;
7. exposição da sequência de recibos no relatório final e na trilha da Output.

### 3.2 O que **não** entra

| Fora da F2 | Vai para |
|---|---|
| diário B111 durável | F3 |
| três dimensões de estado, `JournalDurabilityUnknown` | F3 |
| recuperação automática e inventário físico | F3 |
| remoção de API legado | F3 |
| decisão entre modo A e modo B | F3 |
| mudança de ordem física ou de writer final | já feita na F1 |

A F2 **não** promete recuperação. Ela promete que, quando algo falhar, o relatório dirá
com precisão o que foi gravado, o que não foi e o que ficou indeterminado. Decidir o que
fazer com isso é a F3.

---

## 4. Contrato-alvo

### 4.1 O seam

Nome sugerido: `ApiPlanPersistenceProbe`, deliberadamente paralelo a `ApiPlanScanProbe`.

Contrato mínimo:

- `Begin(ApiPlanPersistenceLog log)` abre escopo `[ThreadStatic]` e devolve `IDisposable`;
  escopos aninhados restauram o anterior;
- `Persist(string objectType, string stage, string plannedName, Guid? plannedGuid, Action save)`
  executa a gravação, medindo-a e registrando o recibo quando há escopo ativo, e apenas
  executa o delegate quando não há;
- uma sobrecarga com função de leitura de confirmação, para os pontos que já releem o
  objeto depois do Save;
- `Suspend()` para trechos que rodam dentro da operação mas não pertencem a ela, como a
  apresentação do relatório final.

Regras:

1. sem escopo ativo, o comportamento é idêntico ao código não instrumentado;
2. o seam nunca altera o resultado da gravação nem engole exceção — ele registra e
   relança;
3. a publicação do log nunca pode derrubar o fluxo medido;
4. o seam não decide nada: não bloqueia, não repete, não escolhe writer.

### 4.2 O recibo

Cada `PersistenceReceipt` carrega:

- ordem monotônica global dentro da operação;
- etapa e tipo de objeto;
- nome planejado e, quando houver, `Guid` planejado;
- nome e `Guid` persistidos, quando a leitura de confirmação ocorreu;
- início, fim e duração;
- resultado: `Confirmed`, `Failed` ou `OutcomeUnknown`;
- exceção ou cancelamento, quando houver;
- se houve leitura de confirmação e qual foi seu resultado.

O recibo é registro, não decisão. Nada no código deve mudar de caminho por causa de um
recibo dentro da F2.

### 4.3 As três classes de falha

| Classe | Situação | Resultado do recibo |
|---|---|---|
| **C** | falhou **antes** de chamar `Save()` | `Failed`, sem efeito produzido |
| **D** | `Save()` lançou, expirou, foi cancelado ou devolveu resposta não determinável | `OutcomeUnknown` |
| **E** | `Save()` concluiu, mas a leitura de confirmação falhou ou divergiu | `OutcomeUnknown`, com a divergência registrada |

A distinção entre C e D é o ponto delicado: exige que o seam saiba se a chamada chegou a
ser feita. Por isso o delegate de gravação precisa ser a **única** coisa dentro do trecho
medido — preparação de conteúdo fica fora.

Um `Save()` que lançou **não** pode ser classificado como “não gravou”. Essa é a regra que
a F3 vai depender.

### 4.4 Granularidade — evitar contagem dupla

Em BC e List, `SaveApi` e `SaveProcedure` são chamados **de dentro** dos `saveSteps`.
Instrumentar os dois níveis contaria cada gravação duas vezes e produziria uma sequência
de recibos falsa.

Regra: **o seam envolve o passo, não a função interna.** Depois de unificar os dois laços
num executor único, existe um só lugar que chama `Persist(...)` para esses writers.
`ApiPlanSdtWriter`, `ApiPlanProcedureWriter`, `ApiPlanApiObjectWriter`,
`ApiPlanTransactionFolder`, `ApiPlanMetadataFileWriter` e o `transaction.Save()` de
`Package.cs` são instrumentados no próprio ponto.

Uma sentinela deve falhar se um `Save()` de produção aparecer fora de um `Persist(...)`,
e outra deve falhar se um ponto for instrumentado nos dois níveis.

### 4.5 Habilitação de BC diferida no Wizard

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
| `Diagnostics/ApiPlanPersistenceProbe.cs` (novo) | seam de escopo ambiente |
| `Diagnostics/ApiPlanPersistenceLog.cs` (novo) | coleção ordenada de recibos, no espírito de `ApiPlanScanTelemetry` |
| `Diagnostics/ApiPlanSaveStepExecutor.cs` (novo) | executor único dos `saveSteps`, hoje duplicado |
| `ApiPlanBusinessComponentWriter.cs`, `ApiPlanListProcedureWriter.cs` | passar a usar o executor único |
| `ApiPlanSdtWriter.cs`, `ApiPlanProcedureWriter.cs`, `ApiPlanApiObjectWriter.cs`, `ApiPlanTransactionFolder.cs`, `ApiPlanMetadataFileWriter.cs` | envolver cada `Save()` em `Persist(...)` |
| `Package.cs` | abrir o escopo por operação; `transaction.Save()` recibado; habilitação de BC diferida no Wizard; sequência de recibos no relatório |
| `ApiPlanApplicationFinalReport.*` | sequência de recibos e contagem por tipo |
| `Tests/PersistenceProbe/` (novo) | teste executável do seam, no padrão de `Tests/ScanProbe/` |

---

## 6. Testes da F2

### 6.1 Teste executável do seam

No padrão de `Tests/ScanProbe/Test-ApiPlanScanProbe.ps1`, compilando os arquivos de
produção com `Add-Type -Path` e exercitando fora da IDE:

1. sem escopo ativo, `Persist` apenas executa o delegate e não registra nada;
2. com escopo, registra ordem monotônica, tipo, nome e duração;
3. exceção dentro do delegate é registrada como `OutcomeUnknown` e **relançada**;
4. falha antes da chamada é registrada como `Failed`;
5. leitura de confirmação divergente produz `OutcomeUnknown` com a divergência;
6. escopos aninhados restauram o anterior;
7. `Suspend()` não encerra o escopo ativo;
8. falha na publicação do log não derruba o fluxo medido.

### 6.2 Contagem e ordem por fluxo

Com o seam injetado, cada fluxo da matriz da F1 passa a ter prova executável:

| Fluxo | Exigido |
|---|---|
| API-only | exatamente um recibo de API, em B054 |
| BC-only | exatamente um recibo de API, no BC, **depois** dos recibos das Procedures |
| List-only | exatamente um recibo de API, no List, depois do recibo da Procedure |
| BC + List | exatamente um recibo de API, no List; nenhum recibo de API no BC |
| Sync, todas as combinações | mesma sequência do Wizard equivalente |
| `GenerateApiObject=false` | nenhum recibo de API |
| SDT/Procedure `false` | nenhum recibo da etapa desmarcada |

Isto substitui, com execução, a instrumentação frágil que a F1 declarou como limitação.

### 6.3 Falha em cada fronteira

Para cada ponto de gravação, injetar as três classes e validar a sequência de recibos
resultante:

1. falha antes de Folder, SDT, Procedure, API e metadata;
2. falha depois de cada um;
3. resultado ambíguo em cada um;
4. cancelamento durante cada um;
5. falha e resultado ambíguo no `transaction.Save()` da habilitação de BC.

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

1. Todo `Save()` de produção do pipeline passa por `Persist(...)`; uma sentinela falha se
   algum ficar fora.
2. Nenhum ponto é instrumentado em dois níveis; uma sentinela falha se houver contagem
   dupla.
3. Sem escopo ativo, o comportamento é idêntico ao anterior.
4. O seam nunca engole exceção nem altera o resultado de uma gravação.
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

## 10. Fontes

- `Docs/Implementation/2026-09-04-B111-F1-PLANO-ORDEM-E-WRITER-FINAL.md` (pré-requisito)
- `Docs/Implementation/2026-09-04-B111-SONDAS-IDENTIDADE-E-DIARIO.md` (contagem real de `Save()`)
- `Docs/Implementation/2026-09-04-PLANO-API-OBJECT-GRAVACAO-UNICA.md` (v24, origem das exigências de seam e de falhas C/D/E)
- `Src/Extension/Diagnostics/ApiPlanScanProbe.cs` e `ApiPlanScanTelemetry.cs` (padrão a seguir)
- `Tests/ScanProbe/Test-ApiPlanScanProbe.ps1` (padrão de teste executável offline)
- os 16 pontos de gravação da seção 2.1
