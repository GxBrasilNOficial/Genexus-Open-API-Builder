# B111 · Fase F3 — durabilidade da intenção e remoção segura

**Sprint:** S-B111 — gravação única do API Object.
**Fase:** F3 de 3 (F1 ordem e writer final · F2 seam e recibos · **F3 durabilidade e remoção**).
**Data:** 2026-09-04. **Item:** `B111` em `Docs/Foundation/06-BACKLOG_v0.1.md`.

**Status:** Modo A selecionado para consolidação após a avaliação técnica inicial; a
revisão por pares permanece em aberto. **Não** autoriza implementação, alteração de código,
instalação, commit ou push.

**Pré-requisito:** F1 e F2 aceitas. A F3 consome os recibos da F2; sem eles, não há como
distinguir o que foi gravado do que ficou indeterminado, e qualquer recuperação seria
adivinhação.

**Decisão registrada:** o Modo A — diário durável — foi escolhido. A comparação com o
Modo B permanece apenas como histórico de alternativas; o contrato vigente desta F3 é o
diário único por KB, com recuperação explícita.

**Código já em campo neste território (2026-09-06):** a recuperação de metadata órfã foi
implementada fora desta fase, por necessidade de campo, e ocupa parte do que a seção 4.3
normatiza. Validada na IDE no mesmo dia (13.6), exceto o `Remover` sob a metadata
recuperada. A seção 13 registra o que ela grava e o que ela deliberadamente não grava;
essas decisões agora são absorvidas pelo contrato vigente da F3. **Ler a 13 antes de
revisar a 4.2 e a 4.3.**

---

## 1. Histórico da elaboração da F3

A F3 foi escrita antes da decisão do modo por um motivo concreto: sem este documento, o
único plano disponível para o conteúdo da F3 era o manuscrito expandido v24 — e **quatro
pontos dele foram desmentidos pelas medições de 2026-09-04**. Quem retomasse a frente lendo
esse manuscrito implementaria quatro coisas erradas, e a correção existia apenas no registro
de evidência, que não é leitura obrigatória de quem procura “o plano da F3”.

O Modo A foi escolhido posteriormente e está consolidado no contrato vigente desta F3. As
referências abaixo à alternativa B devem ser lidas como histórico comparativo, não como
decisão pendente.

A seção 7 lista essas quatro correções de forma explícita, e é a parte deste documento que
mais importa preservar.

Este plano é deliberadamente menos detalhado que F1 e F2 em pontos que dependem de
experiência de campo com a F1 implementada. Onde isso ocorre, está dito.

---

## 2. Estado de partida

### 2.1 O que F1 e F2 já terão entregue

- um único `API.Save()` por aplicação, no writer final, em Sync e Wizard;
- identidade planejada como o `Guid` lido do `API.Create`;
- gate reduzido antes de qualquer gravação;
- seam de persistência com recibos, e as classes de falha C, D e E distinguíveis;
- sequência de recibos no relatório final.

A F3 acrescenta **durabilidade da intenção** e **recuperação**, que são exatamente o que
recibos em memória não dão: eles morrem com o processo.

### 2.2 O que o remover já faz hoje

Ao contrário do que o manuscrito expandido sugere, `ApiPlanGeneratedApiRemover.Remove` **não** apaga às
cegas. Ele já:

1. cria o índice da KB (`ApiPlanGeneratedApiRemover.cs:49`);
2. localiza a metadata própria por nome canônico e posse;
3. reconstrói o plano de remoção a partir da metadata (`FromMetadata`, com nome e GUID da
   Transaction);
4. valida ambiguidade e posse de API, Procedures e SDTs **antes de qualquer `Delete()`**
   (`ValidateRemovalTargets`, `ApiPlanGeneratedApiRemover.cs:315`);
5. confirma a ausência de cada objeto depois do `Delete()`, lançando se ainda existir.

Ou seja: a intenção de remoção já é reconstruída a partir de uma fonte durável — a
metadata B060 — e validada antes de destruir qualquer coisa.

**O que falta**, e é o escopo da F3 na remoção:

- registrar a intenção de forma durável **antes** do primeiro `Delete()`, para que uma
  interrupção no meio deixe rastro do que foi previsto e do que foi feito;
- tratar o caso de metadata ausente, corrompida ou insuficiente sem cair em inferência por
  nome;
- distinguir remoção concluída de remoção interrompida.

Isso é evolução do que existe, não reconstrução.

### 2.3 O que as sondas mediram e que restringe o desenho

Do registro `2026-09-04-B111-SONDAS-IDENTIDADE-E-DIARIO.md`, seis execuções sobre duas KBs:

| Operação | KB pequena | KB grande |
|---|---|---|
| gravar um `WikiFileKBObject` | ~130 ms | **~1,1 s** |
| gravar Folder, SDT, Procedure, API | 11–35 ms | 12–98 ms |
| reler File por `Id` | 0 ms | 0 ms |
| localizar por nome no índice já montado | 0 ms | 0 ms |
| remontar o índice | ~100 ms | **~3,1 s** |
| varrer Files por prefixo (medição histórica) | 5 ms | 22–28 ms |

Três restrições saem daí, e valem para qualquer desenho de diário:

1. **escrever é caro, ler é grátis** — o desenho deve minimizar gravações, não leituras;
2. **nunca remontar o índice** para reencontrar um objeto que se acabou de criar;
3. **guardar o `Id`** do File na criação: `WikiFileKBObject.Get` aceita `int`, não `Guid`.

---

## 3. Decisão adotada: Modo A — diário durável

O Modo A foi selecionado em 2026-09-07. A tabela abaixo preserva a comparação que orientou
a decisão, mas somente a coluna do Modo A é contrato vigente. O Modo B é histórico e não
pode reaparecer como implementação parcial ou como decisão ainda pendente.

| | **Modo A — diário durável** | **Modo B — checkpoint manual** |
|---|---|---|
| onde a intenção vive | um `File` próprio na KB | fora da KB, registrado pelo operador |
| custo no Apply, KB grande | 2,2 s a 11 s, conforme granularidade (4.4) | ~zero |
| resíduo na KB | um `File` único por KB, preservado após a operação terminal | nenhum |
| recuperação | explícita, no comando próprio da F3; bloqueia em qualquer ambiguidade | humana e bloqueante |
| complexidade acrescentada | alta: máquina de estados, reconciliação, ciclo de vida do diário | baixa |
| o que promete | reconstruir intenção e ponto de falha sem intervenção | tornar impossível reaplicar às cegas |

Nenhum dos dois entrega atomicidade ou rollback. A diferença histórica é **quem** reconstrói
a intenção depois de uma falha: o código ou a pessoa. No contrato vigente, a recuperação é
explícita e pertence à F3.

### 3.1 Decisões fechadas e detalhes de execução

- há um único diário por KB, no objeto técnico `File`/`WikiFileKBObject`, sem módulo;
- o nome lógico fixo é `GxOpenApiBuilder_OperationJournal` e o arquivo externo é
  `GxOpenApiBuilder_OperationJournal.json`;
- o diário é preservado durante a vida da KB e só é reutilizado ou substituído quando a
  operação anterior estiver em estado terminal confirmado (`Completed` ou `Removed`) e
  com `journalDurability=Confirmed`;
- o comando de recuperação é próprio, fica no menu principal, atua sobre a KB inteira e
  só libera nova operação depois de confirmar um estado terminal;
- falha, divergência, leitura ambígua ou mais de um candidato válido bloqueiam; não há
  desempate por nome, prefixo, data ou “mais recente”;
- a implementação deve manter separadas a identidade da operação, a intenção, o estado
  físico, a durabilidade do diário e os recibos definidos na decisão registrada.

As comparações com o Modo B permanecem neste documento apenas para explicar a decisão
histórica. Não há uma segunda implementação a especificar nem uma decisão A/B pendente.

---

## 4. Núcleo do Modo A — estados e gate

### 4.1 Estados

Os eixos abaixo são sempre registrados separadamente e nunca colapsados num enum só:

- **estado físico do objeto**: ausente, confirmado, divergente, indeterminado — vem dos
  recibos da F2 mais leitura da KB;
- **estágio lógico da aplicação**: o ponto do pipeline em que a operação está.

- **durabilidade da intenção**: confirmada ou desconhecida, conforme o diário possa ser
  relido e validado.

Estágios mínimos: `NotStarted`, `GateBlocked`, `IntentionRecorded`, `TransactionPending`,
`FolderPending`, `SdtsPending`, `ProceduresPending`, `ApiPending`, `ApiSaveOutcomeUnknown`,
`ApiPhysicallySaved`, `MetadataPending`, `Partial`, `Completed`, `RemovalInProgress`,
`RemovalPartial`, `Removed`.

Regra que atravessa tudo: **um `Save()` que lançou, expirou ou foi cancelado é
`OutcomeUnknown` até consulta por identidade.** Nunca “não gravou”.

### 4.2 Gate estendido

O gate reduzido da F1 ganha as validações que dependem de intenção durável:

1. ausência de intenção anterior em estado parcial, indeterminado ou ambíguo;
2. disponibilidade e integridade do diário único da KB;
3. identidade, versão e durabilidade confirmadas do diário, sem divergência física.

Falhando qualquer uma, o resultado é bloqueio antes da primeira gravação.

### 4.3 Remoção com intenção confirmada

Antes do primeiro `Delete()`, e além do que o remover já faz hoje (2.2):

1. registrar a intenção de remoção, com o conjunto completo de alvos validados, e
   confirmá-la por releitura;
2. só então excluir, na ordem definida, emitindo recibo por exclusão;
3. marcar `Removed` apenas quando todos os alvos previstos estiverem confirmadamente
   ausentes;
4. marcar `RemovalPartial` em interrupção, **preservando** o registro da intenção.

Para API criada antes desta frente, sem intenção registrada, duas saídas — e só essas:

- reconstruir a intenção a partir de metadata válida, validando Transaction GUID,
  ApplicationId, contrato, nomes e todas as referências, e marcá-la como importada; ou
- **bloquear antes do primeiro `Delete()`**, com instrução de recuperação manual.

Se a metadata não permitir reconstruir o conjunto **completo** de alvos, não apagar
parcialmente por inferência. `ApiPlanOwnedObjectDescription.IsCanonical` continua útil
para reconhecer posse histórica, mas nome, Description canônica ou prefixo **nunca**
autorizam exclusão sozinhos.

### 4.4 Orçamento de gravação

Vale para o Modo A, e é o motivo de a granularidade ser uma decisão de projeto e não um
detalhe:

| Política | Gravações | Acréscimo ao Apply, KB grande |
|---|---|---|
| uma por etapa confirmada, como no manuscrito expandido | ~10 | ~11 s |
| três checkpoints: início, pós-API, conclusão | 4 | ~4,4 s |
| mínimo: criação e conclusão | 2 | ~2,2 s |

O plano deve escolher e declarar a política. A recomendação é a de três checkpoints: ela
cobre as fronteiras que importam — antes de qualquer gravação, no momento em que o API
passa a existir, e na conclusão — a um quarto do custo da política do manuscrito expandido.

### 4.5 Mensagens

Devem distinguir, no mínimo: gate bloqueado; Sync sem BC habilitado; Wizard com
habilitação de BC pendente; intenção não confirmada; API inexistente com
`GenerateApiObject=false`; API com identidade ambígua; API gravado; resultado
indeterminado; recuperação bloqueada; API legado sem metadata suficiente; remoção parcial.

Seguem o mecanismo de internacionalização vigente e não expõem hashes ou identificadores
além do necessário ao diagnóstico.

---

## 5. Modo A — diário durável e único por KB

### 5.1 Identidade e localização do diário

O diário é um `File` próprio (`WikiFileKBObject` é o tipo técnico do SDK), distinto da
metadata de negócio e sem módulo. Há exatamente um por KB:

- nome do objeto na KB: `GxOpenApiBuilder_OperationJournal`;
- nome do arquivo externo: `GxOpenApiBuilder_OperationJournal.json`;
- conteúdo: schema, identidade da KB e da operação, intenção, estado, durabilidade,
  inventário e recibos;
- o nome fixo apenas localiza o candidato; posse, identidade e validade continuam sendo
  confirmadas pelo conteúdo e pela releitura por `FileId`.

Localização, medida e não suposta:

1. **ao abrir o fluxo**: buscar o nome fixo no índice já montado — 0 ms;
2. **durante o fluxo**: guardar o `Id` do File na criação e reler por `Id` — 0 ms;
3. **em recuperação**: resolver o nome fixo no índice; qualquer ausência, duplicidade,
   colisão externa ou conteúdo inválido bloqueia;
4. **nunca**: remontar o índice para reencontrar o diário — ~3,1 s.

### 5.2 Conteúdo

Schema version; Transaction GUID e nome; ApplicationId, ativo e histórico; API planejado e,
quando confirmado, API persistido; `PlannedApiGuid`; `PersistedMainObjectName` e GUID;
hash do contrato; flags da seleção; serviços BC/List; Folder, SDTs, Procedures e metadata
planejados; estado físico do API; estágio lógico; durabilidade do próprio diário; recibos;
sequência de gravações; versão do gerador; marca de intenção importada, quando reconstruída
para remoção; timestamps e mensagens de bloqueio.

O conteúdo pode crescer sem custo mensurável: a sonda mediu 247 bytes e 20 KB com o mesmo
tempo de gravação.

### 5.3 Ciclo de vida

Antes da primeira gravação de Transaction, Folder, SDT, Procedure, API ou metadata: criar
o diário, salvá-lo, reler **pelo `Id`**, confirmar Transaction GUID, ApplicationId, hash e
intenção. Só então começar o pipeline.

Se a criação ou a confirmação falhar, abortar antes de gravar qualquer objeto. Um diário
que existe mas não pôde ser confirmado fica com durabilidade desconhecida: não prosseguir,
e **não criar um segundo diário**.

Nas transições seguintes, conforme a política de checkpoints de 4.4: confirmar o Save do
objeto pelo recibo; depois atualizar e reler o diário; se a atualização não for confirmada,
preservar o último estado durável e marcar durabilidade desconhecida. Nunca transformar
estado desconhecido em `Completed`; nunca iniciar uma segunda persistência de API para
“corrigir” um estado desconhecido.

### 5.4 Recuperação

**API com resultado indeterminado.** Bloquear qualquer novo `API.Save()`; reler por
`PlannedApiGuid` — que existe desde o `Create` e é a chave direta; validar Transaction
GUID, ApplicationId, hash e contrato; tratar zero, um confirmado e múltiplos candidatos
como resultados distintos; concluir ausência só após consulta suficiente e registro;
bloquear em conflito. **Nunca criar um novo API por nome porque a primeira chamada
“pareceu falhar”.**

Comparado ao manuscrito expandido, esta recuperação é curta justamente porque a identidade é conhecida
antes da gravação.

**Diário com durabilidade desconhecida.** Não continuar a sequência; reler pelo `Id`;
resolver o nome fixo no índice; comparar ApplicationId, contrato, recibos e estados;
corrigir apenas se houver exatamente um diário próprio e uma versão recuperável; bloquear
em ausência, duplicidade, colisão, conflito ou leitura ambígua; **nunca criar um segundo
diário para ocultar a incerteza**.

**Falha antes do API final.** Ler o diário e os recibos; inventariar cada alvo por
identidade e hash; separar ausente, confirmado, divergente e desconhecido; comparar com a
intenção durável; então continuar apenas etapas ainda não executadas, reconciliar
manualmente ou bloquear. Nunca repetir a persistência do API havendo estado físico
confirmado ou desconhecido. **Reabrir o Wizard pelo caminho normal não é recuperação.**

---

## 6. Modo B — checkpoint manual (histórico)

> **Histórico:** o Modo B não foi selecionado. Esta seção preserva a alternativa que foi
> comparada durante a decisão, mas não define comportamento da implementação S-B111.

O checkpoint registra, antes da primeira gravação e antes de qualquer reaplicação: a
seleção, a ordem prevista, nomes e identidades planejadas, e o próximo passo autorizado.
Após uma falha, registra também os objetos parciais observados.

A extensão **bloqueia** reaplicação automática. “Continuar” não pode significar chamar o
Wizard de novo.

O que o modo B **não pode fazer**, e precisa estar visível na UI e na documentação:
alegar que detecta ou reconcilia sozinha todo estado parcial, ou apresentar como
durabilidade algo que depende de confirmação humana. Gate, ordem física, identidade,
recibos, relatório e testes de falha continuam obrigatórios — some a recuperação
automática, não o rigor.

---

## 7. Correções obrigatórias ao manuscrito expandido v24

Esta seção é a razão de o documento existir agora.

**A qual documento estas correções se aplicam.** Existem dois documentos anteriores, e só um
deles contém o material corrigido aqui:

| Documento | Contém diário, seam, três dimensões? | Corrigido por esta seção? |
|---|---|---|
| [`2026-09-04-PLANO-API-OBJECT-GRAVACAO-UNICA.md`](2026-09-04-PLANO-API-OBJECT-GRAVACAO-UNICA.md) — plano aprovado em 2026-09-04 | **não**; escopo enxuto de reordenação | **não** — nada nele é desmentido aqui |
| [`2026-09-04-B111-MANUSCRITO-EXPANDIDO-V24.md`](2026-09-04-B111-MANUSCRITO-EXPANDIDO-V24.md) — expansão nunca aprovada | sim | **sim** |

O manuscrito expandido continua útil como origem das exigências de diário, recuperação e
remoção, mas os quatro pontos abaixo **estão errados nele** e foram medidos em campo:

| o manuscrito expandido diz | Medição de 2026-09-04 | O que vale |
|---|---|---|
| identidade por `PlannedApiGuid` **ou** `NewApiIdentityKey`, com marcador `GOAB-B111-IDENTITY` na `Description` | o `Guid` existe desde o `API.Create`, sobrevive ao `Save()` e reencontra o objeto | só `PlannedApiGuid`, **lido** e nunca atribuído; sem marcador, sem chave alternativa, sem helper de identidade |
| “ao abrir um fluxo, enumerar todos os diários B111” | localizar pelo nome no índice já montado custa 0 ms; remontar o índice custa ~3,1 s | resolver pelo nome fixo no índice na abertura e na recuperação; não enumerar por prefixo |
| reler o diário por GUID | `WikiFileKBObject.Get` aceita `int`, não `Guid` | guardar o **`Id`** na criação e reler por ele |
| “atualizar e reler o diário a cada etapa confirmada” | gravar um File custa ~1,1 s na KB grande; ~10 gravações somam ~11 s | checkpoints agrupados, com a política declarada (4.4) |

Some, junto com o marcador, a exigência do manuscrito de validar truncamento da `Description` —
que existe (256 caracteres, em silêncio) mas deixa de afetar esta frente e está registrada
como `B112`.

---

## 8. Testes da F3

Sobre o seam da F2, que permite injetar falha em qualquer fronteira:

1. intenção não confirmável → aborta antes do primeiro objeto;
2. falha antes e depois de cada gravação → estado físico e estágio lógico corretos e
   independentes;
3. `API.Save()` com resultado ambíguo → `OutcomeUnknown` e **nenhum** segundo
   `API.Save()`;
4. atualização do diário não confirmada → durabilidade desconhecida e bloqueio; ausência,
   duplicidade, colisão externa e conteúdo inválido no nome fixo tratados como casos
   distintos;
5. reaplicação sobre estado parcial → só etapas não confirmadas; nunca API duplicado;
   nunca intenção parcial sobrescrita em silêncio;
6. remoção com intenção própria → intenção registrada antes do primeiro `Delete()`,
   recibo por exclusão, `Removed` só com todos os alvos ausentes;
7. remoção de legado com metadata válida → intenção importada antes do primeiro `Delete()`;
8. remoção de legado com metadata ausente, corrompida ou insuficiente → bloqueio, sem
   nenhum `Delete()`;
9. interrupção durante a remoção → `RemovalPartial`, com a intenção preservada.

Um teste que apenas chama `Apply` de novo pelo caminho normal **não** comprova
recuperação.

---

## 9. Validação na IDE

Reinstalar a DLL conforme a política do repositório e validar depois dela.

1. Apply completo, Sync e Wizard, conferindo a intenção registrada e concluída;
2. cancelamento em cada fronteira, conferindo o estado reportado;
3. reentrada após cada cancelamento: recuperada ou bloqueada, nunca reaplicada às cegas;
4. remoção de API gerada por esta frente;
5. remoção de API legado, com metadata válida e com metadata insuficiente;
6. interrupção no meio da remoção;
7. medir o acréscimo real ao tempo de Apply na KB grande e comparar com o orçamento de 4.4;
8. executar o comando de recuperação explícita sobre a KB inteira, incluindo um caso
   recuperável e casos de divergência ou ambiguidade que permaneçam bloqueados;
9. iniciar uma nova operação depois da recuperação e confirmar que somente um diário fixo
   por KB é reutilizado.

---

## 10. Critérios de aceite

**Comuns:**

1. a intenção é registrada e confirmada antes da primeira gravação do pipeline;
2. estado físico e estágio lógico são reportados separadamente;
3. resultado indeterminado bloqueia continuação automática;
4. reaplicação não cria API duplicado nem sobrescreve intenção parcial em silêncio;
5. nenhuma exclusão ocorre sem intenção de remoção confirmada;
6. remoção de legado importa a intenção da metadata ou bloqueia antes do primeiro
   `Delete()`;
7. nome, Description canônica ou prefixo nunca autorizam exclusão sozinhos.

**Contrato ativo do Modo A:** a durabilidade do diário é uma terceira dimensão registrada;
recuperação compara intenção durável com inventário físico; a política de checkpoints está
declarada e o custo medido bate com o orçamento; nunca existe um segundo diário para a KB;
recuperação ambígua ou divergente continua bloqueada para correção humana.

As condições do antigo Modo B não são critérios de aceite: permanecem apenas na seção 6
como comparação histórica.

---

## 11. Riscos

| Risco | Mitigação prevista |
|---|---|
| o Modo A acrescentar segundos ao Apply de forma percebida como regressão | política de checkpoints declarada e medida (4.4); comparar com o Apply pós-F1 |
| o diário acumular registros na KB | manter um único File por KB; reutilizar ou substituir somente após estado terminal confirmado e `journalDurability=Confirmed` |
| reintroduzir “um pouco de A e um pouco de B” | o Modo A é o contrato selecionado; a comparação com B é histórica e não autoriza comportamento alternativo |
| a remoção de legado bloquear casos que hoje funcionam | o remover já reconstrói plano a partir da metadata (2.2); a F3 acrescenta registro de intenção, não restringe o que já valida |
| planejar sobre um sistema que ainda vai mudar | este plano mantém dependências explícitas de F1 e F2; a implementação deve revalidar os contratos contra o código vigente |

---

## 12. Fontes

- `Docs/Implementation/2026-09-04-B111-F1-PLANO-ORDEM-E-WRITER-FINAL.md`
- `Docs/Implementation/2026-09-04-B111-F2-PLANO-SEAM-E-RECIBOS.md`
- `Docs/Implementation/2026-09-04-B111-SONDAS-IDENTIDADE-E-DIARIO.md` (medições que impõem a seção 7)
- `Docs/Implementation/2026-09-04-PLANO-API-OBJECT-GRAVACAO-UNICA.md` (plano aprovado em 2026-09-04, escopo enxuto; superado como execução)
- `Docs/Implementation/2026-09-04-B111-MANUSCRITO-EXPANDIDO-V24.md` (expansão nunca aprovada, origem das exigências desta fase — ler junto com a seção 7)
- `Src/Extension/Diagnostics/ApiPlanGeneratedApiRemover.cs`
- `Src/Extension/Diagnostics/ApiPlanOwnedObjectDescription.cs`
- `Src/Extension/Diagnostics/ApiPlanKbObjectNameIndex.cs`
- `Docs/Foundation/06-BACKLOG_v0.1.md` — `B111`, e os colaterais `B112`, `B113`, `B114`

---

## 13. Anexo — recuperação de metadata órfã implementada em 2026-09-06

Este anexo não altera o plano. Ele registra código que **já existe** e que ocupa parte do
território da seção 4.3, para que a revisão por pares não desenhe por cima sem saber.

### 13.1 Por que foi implementado fora da fase

O caso da seção 2.2 — «metadata ausente» — deixou de ser hipótese. Em 2026-09-06, numa KB de
teste, o File de metadata foi apagado à mão e a ferramenta ficou sem saída: `Remover` e
`Sincronizar` bloqueiam por metadata ausente, e o `Wizard` abre bloqueado, com a única ação
disponível sendo Cancelar. É o `B115`.

Havia uma opção de recuperação no código desde 2026-09-05, mas **inalcançável**: ela era
oferecida depois de o Wizard concluir com sucesso, e o estado que ela resolve impede a
conclusão. O teste de campo de 2026-09-06 provou isso. A correção move a oferta para a
abertura do Wizard, antes do diálogo.

Esperar a F3 significaria manter a KB sem saída até a decisão do modo, que ainda estava
pendente quando este anexo foi escrito. O Modo A foi selecionado em 2026-09-07; portanto,
esta recuperação passa a ser uma capacidade absorvida pelo comando explícito da F3, sem
criar um contrato paralelo.

### 13.2 O que a recuperação grava

Uma metadata com o `schemaVersion` corrente e **apenas** o que a remoção consome, conforme
`ApiPlanGeneratedApiRemovalPlan.FromMetadata`:

| Bloco | Origem |
|---|---|
| `ownership.transactionName` / `transactionGuid` | a Transaction selecionada |
| `ownership.apiName` / `apiGuid` | o API Object encontrado e confirmado como próprio |
| `ownership.metadataFileName` | nome canônico |
| `objects.transactionFolder` | Folder canônico da Transaction, sempre com `wasCreated=false` |
| `objects.procedures` | as Procedures `proc<T>_API_*` **encontradas na KB** |
| `objects.sdts.own` / `.shared` | os SDTs **encontrados na KB**, por posse verificada |
| `recovery` | marca de intenção importada, seção 13.3 |

`wasCreated=false` é deliberado e conservador: não há como saber se o Folder foi criado pela
extensão, e a remoção não deve apagar um Folder que talvez seja do usuário.

### 13.3 O que ela deliberadamente **não** grava

Os blocos `fields`, `pagination`, `order`, `services`, `levels` e `transactionStructure`
ficam **ausentes** — não vazios. A diferença importa: o leitor de contrato existente
(`PrototypeWizardExistingApiContractReader`) tem fallback para a KB quando a chave falta, e
não tem quando ela está presente e vazia.

O motivo é que esses dados **não são recuperáveis**. Medido em 2026-09-06: o reader
reconstrói serviços, campos de Create/Update/Response, filtros, RestPath e SecurityLevel a
partir do API Object e dos SDTs; mas `servicesBasePath`, `defaultPageSize`,
`maximumPageSize`, ordenação estática, campos obrigatórios e a estrutura hierárquica
persistida existem **somente** na metadata. Reconstruí-los seria inventar.

É o mesmo empobrecimento que o `B110` mediu por outro caminho. Uma metadata que os
inventasse faria o `Sincronizar` comparar a API real contra uma descrição falsa e propor
mudanças destrutivas. Por isso a marca da seção 13.4 bloqueia o Sync.

### 13.4 A marca

```json
"recovery": {
  "imported": true,
  "importedAtUtc": "…",
  "source": "KbInventory",
  "notRecovered": ["fields", "pagination", "order", "services", "levels", "transactionStructure"]
}
```

O nome `imported` vem da seção 4.3 deste plano — «marcá-la como importada» —, não de
vocabulário novo. Enquanto a marca existir:

- **`Remover` funciona.** Tem o inventário completo de alvos, que é tudo o que consome.
- **`Sincronizar` recusa**, com mensagem própria: não há contrato com que comparar.
- Um `Wizard` + Apply completo reescreve a metadata inteira e a marca desaparece.

### 13.5 Decisões aprovadas para a consolidação

As três decisões abaixo fecham a interação entre a recuperação de metadata órfã e o
contrato do Modo A. Elas não encerram a revisão por pares: ainda precisam ser refletidas
na implementação e validadas na matriz da F3.

1. **Gate e metadata importada:** metadata importada completa quanto aos alvos não é
   `Partial` nem `OutcomeUnknown`. Um diário da S-B111 em estado não terminal (`Pending`,
   `Running`, `Partial` ou `OutcomeUnknown`) bloqueia novas operações e a recuperação de
   metadata. Metadata importada validada pode liberar somente `Remove`, com confirmação
   explícita; `Apply` completo pode substituir sua marca, e `Sync` não pode usá-la como
   contrato completo.

2. **Suficiência do inventário:** a recuperação produz explicitamente
   `InventorySufficient` ou `InventoryInsufficient`. O primeiro exige schema, Transaction
   GUID, ApplicationId, API GUID, referências completas, distinção entre SDTs próprios e
   compartilhados e revalidação sem ambiguidade de todos os alvos; só ele permite `Remove`,
   ainda com confirmação explícita. Campo ausente, referência desconhecida, conflito,
   múltiplos candidatos ou identificação baseada apenas em nome produz
   `InventoryInsufficient` e bloqueia. `InventorySufficient` é suficiente para a remoção,
   mas não prova que nenhum objeto histórico ficou fora do inventário.

3. **Precedência entre diário e metadata:** o diário é autoritativo para estado da
   operação, recibos e intenção atual; a metadata é a fonte do último contrato completo
   conhecido da API. Diário não terminal sempre bloqueia. Diário terminal e metadata podem
   coexistir somente quando identidade e contrato forem compatíveis; qualquer divergência
   bloqueia e exige reconciliação explícita. Nenhuma fonte substitui silenciosamente a
   outra, e a precedência do diário não transforma metadata incompatível em contrato válido.

### 13.6 Evidência de campo — 2026-09-06, KB `wsEducacaoSpTeste`, Transaction `Teste`

Primeira execução real do caminho. O File `apiTeste_Metadata` foi apagado à mão.

| Passo | Resultado |
|---|---|
| Abrir o Wizard, recusar a oferta | `Recuperação recusada pelo usuário. Nenhuma alteração foi feita por B115.` O Wizard seguiu abrindo, bloqueado, e o usuário cancelou. Nada gravado. |
| Abrir de novo, aceitar | `Metadata órfã recuperada: File='apiTeste_Metadata', Guid='43be3a1b…', Bytes=2290, Procedures=5, SdtsProprios=18, SdtsCompartilhados=3.` |
| `Sincronizar` | Bloqueou. Relatório final `Interrupted`, `Bloqueados=1`, com a mensagem da 13.4. |
| Reabrir o Wizard | Abriu **destravado** — «Estado: teste de reencontro», `Concluir e aplicar` habilitado. A oferta não reapareceu: `File 'apiTeste_Metadata' encontrado 1 vez(es); recuperação órfã não se aplica.` |
| Apply completo | `SuccessWithWarnings`, `Criados=0`, `Atualizados=7`, `Bloqueados=0`. A metadata foi reescrita **no mesmo File** (`Guid='43be3a1b…'`), passando de 2290 para `Bytes=117926`. A marca desapareceu com a substituição do JSON. |

Dois números confirmam o desenho: **2290 bytes** contra os **117926** da metadata completa — a diferença é exatamente o contrato que a 13.3 diz não recuperar —, e o **mesmo GUID** nas duas pontas, provando que o Apply reencontra e sobrescreve o File da recuperação em vez de criar um segundo.

### 13.7 O `Remover` sobre a metadata recuperada — e o defeito que ele expôs

Testado no mesmo dia, e **falhou na primeira tentativa**. A remoção parou no meio com o API
Object, 5 Procedures e 5 SDTs já apagados:

```
'sdtTeste_API_ListFilters' is referenced at least by 'sdtTeste_API_ListResponse'
```

A causa não era o inventário — estava completo — e sim a **ordem**. `ResolveOwnSdtNames` usa
`objects.sdts.own` como está, e essa ordem carrega as dependências entre SDTs; a recuperação
a gravava em ordem alfabética, que não é ordem de dependência nenhuma. O teste que
acompanhava a recuperação verificava *o quê* era gravado, nunca *em que ordem*, e por isso
passava.

Dois defeitos colaterais apareceram junto:

1. o relatório final informou `Removidos: (nenhum)` com onze objetos fora da KB — a lista era
   local ao remover e a exceção subia antes de ela chegar ao relatório;
2. a metadata recuperada guarda o `apiGuid` do API Object daquele momento. Removido esse
   objeto e recriado outro pelo Wizard, a metadata passou a apontar para um GUID inexistente,
   o Wizard travou em `OwnershipSchemaApiNameOrGuidMismatch` e a recuperação **não se
   oferecia** — o File existia. Saída: apagar o File à mão e recuperar de novo.

O segundo deles foi corrigido no mesmo dia: a recuperação passou a se oferecer também com o
File presente, sob três condições — ele é único, está marcado como intenção importada, e o
`apiGuid` que registra não é o do API Object que existe. Nesse caso ela regrava o inventário
**sobre o mesmo File**; criar um segundo homônimo apenas trocaria o bloqueio por ambiguidade
de metadata. Metadata **completa** com o mesmo descompasso continua fora do alcance, e é
deliberado: o fingerprint `B067` cobre o conteúdo inteiro, então corrigir só o `apiGuid`
levaria de `OwnershipSchemaApiNameOrGuidMismatch` a `FingerprintHashMismatch`, sem sair do
lugar. Recalcular o fingerprint significaria revalidar o contrato — outra frente.

Essa modalidade está coberta por teste de contrato e **nunca foi executada na IDE**: recriar o
cenário exige remover a API e gerá-la de novo por cima da metadata recuperada.

A correção escolhida não foi acertar a ordem na recuperação, e sim **tornar a remoção
resiliente a ela**: o que a IDE recusa volta para a fila e é tentado na passada seguinte;
enquanto cada passada apagar ao menos um objeto há progresso; uma passada inteira sem
progresso encerra e reporta os pendentes. Resolve para qualquer metadata, não só a
recuperada. A recusa não é interpretada pela mensagem da exceção — só o abort do usuário é
relançado na hora.

**Segunda execução, com a correção:**

```
Remocao de SDTs passada 1: apagados=17, adiados=1.
Remocao de SDTs passada 2: apagados=1,  adiados=0.
Relatório final: Resultado='Success', Removidos=24, Bloqueados=0, DuraçãoMs=4110.
```

O adiado foi o `sdtTeste_API_ListFilters` — o mesmo que travara a primeira tentativa —, e ele
sai como penúltimo da lista de removidos, depois que `ListResponse` deixou de referenciá-lo.
O Folder `TesteOpenApi` ficou vazio e **não foi apagado** (`wasCreated=false`, 13.2), e os três
SDTs compartilhados foram preservados.

O ciclo foi repetido logo depois sobre a API **completa** — 5 Procedures, com o `Delete` que
faltava na primeira —, e o resultado foi `Removidos=25` com o mesmo `passada 1: apagados=17,
adiados=1` e `passada 2: apagados=1, adiados=0`. O inventário mudou de 24 para 25 objetos e o
número de adiados não: com a lista em ordem alfabética o adiamento é **determinístico**, não
acidental — `ListFilters` sempre precede o `ListResponse` que o referencia, e sempre exige
exatamente uma segunda passada.

A execução também confirmou o que a 13.2 promete sobre o inventário: a metadata recuperada
listou **4** Procedures, não 5, porque o `procTeste_API_Delete` não existia mais na KB, e a
validação as aceitou por Description, sem depender de GUID.

**O `B115` está atendido:** a recuperação devolve a capacidade de remover uma API gerada cuja
metadata se perdeu. O que ela não devolve — e continua não devolvendo — é o contrato (13.3).

**Fechamento do ciclo.** Reaplicada a API pelo Wizard, o `PlannedContractHash` voltou a
`16DF0B0A0572C17A6D0462594060693E71214A47DE978B8239CFDDCDEF3BFA6C` — **o mesmo de antes de
todo o experimento**, provando que o contrato foi restaurado sem perda. O `Build All`
concluiu com `Success` nos dois environments da KB, `NETPostgreSQL155` e
`NETFrameworkSQLServer004`: as 5 Procedures, o API Object e os 18 SDTs hierárquicos foram
especificados e gerados nos dois, com documentação REST. Nas permissões GAM o `apiTeste`
aparece com o GUID gravado pela última aplicação. Os avisos do build são todos preexistentes
e de outros objetos da KB.

### 13.8 Custo

Uma gravação de `WikiFileKBObject` por recuperação: ~130 ms na KB pequena, **~1,1 s** na
grande (seção 2.3, e item `B114` do backlog). A recuperação é opt-in, ocorre uma vez por
incidente e não entra no orçamento de gravação da seção 4.4.
