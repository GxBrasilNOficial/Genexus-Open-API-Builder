# B111 · Fase F1 — ordem de gravação e writer final único

**Sprint:** S-B111 — gravação única do API Object.
**Fase:** F1 de 3 (F1 ordem e writer final · F2 seam e receipts · F3 durabilidade e remoção).
**Data:** 2026-09-04. **Item:** `B111` em `Docs/Foundation/06-BACKLOG_v0.1.md`.

**Status:** decisões de escopo e contrato da F1 consolidadas em 2026-09-07; o plano ainda
não foi implementado. A revisão por pares da sprint não está encerrada. **Não** autoriza
alteração de código, instalação, commit ou push.

### Documentos que este plano substitui, e como

Há **dois** documentos anteriores, com escopos diferentes. Confundi-los é fácil e foi o que
ocorreu na redação inicial deste plano, corrigido em 2026-09-05.

| Documento | O que é | Situação |
|---|---|---|
| [`2026-09-04-PLANO-API-OBJECT-GRAVACAO-UNICA.md`](2026-09-04-PLANO-API-OBJECT-GRAVACAO-UNICA.md) | plano **aprovado** em 2026-09-04, escopo enxuto: reordenar os passos, `deferApiSave`, guarda de parâmetros, relatório e trilha na Output | superado como execução; **as mudanças 1 a 7 da sua seção 4 estão preservadas nesta F1**, ver 4.8 e 4.9 |
| [`2026-09-04-B111-MANUSCRITO-EXPANDIDO-V24.md`](2026-09-04-B111-MANUSCRITO-EXPANDIDO-V24.md) | expansão **nunca aprovada**, produzida na revisão por pares: diário, seam, três dimensões de estado, remoção legada | superado; origem das exigências herdadas por F2 e F3, com quatro afirmações desmentidas por medição |

O plano aprovado **refuta explicitamente** o seam de persistência, por não haver projeto de
testes .NET onde exercitá-lo. A F2 reabre esse ponto por um caminho que ele não considerou —
escopo ambiente e teste offline por `Add-Type`, ambos já usados no repositório —, e isso está
declarado no plano da F2. Não é reintrodução silenciosa de ponto refutado.

O apêndice de refutações do plano aprovado (seção 10) **continua valendo** para as três fases.

---

## 1. Por que existe uma F1 separada

O manuscrito expandido v24 tratava numa única frente quatro coisas de tamanhos muito diferentes: a ordem de
gravação, um seam de persistência transversal, um diário durável com máquina de estados
de três dimensões, e a remoção de APIs legadas. Aplicadas juntas, o risco de introduzir a
degradação que a frente quer evitar fica maior que o risco atual.

A F1 isola o **benefício central de `B111`** — o API Object gravado uma única vez, pelo
consumidor final — e nada mais. Ela não depende do modo do diário, decidido em 2026-09-07
e vigente na F3.

As sondas de 2026-09-04 encolheram a F1 ainda mais do que o previsto: com identidade
estável disponível desde o `API.Create`, o aparato de marcador na `Description`, chave
alternativa e helper de identidade **deixa de ser necessário**.

---

## 2. Estado medido de partida

### 2.1 O que o código faz hoje

Confirmado por leitura em 2026-09-04:

1. `ApiPlanApiObjectWriter.CreateOrReencounter` prepara Folder, SDTs e Procedures e chama
   `Save()` tanto para API reencontrado (`ApiPlanApiObjectWriter.cs:707`) quanto para API
   criado (`ApiPlanApiObjectWriter.cs:718`), relendo por `API.Get(designModel, guid)` em
   seguida.
2. `ApiPlanBusinessComponentWriter` tem três `Save()` reais, e o do API está em
   `ApiPlanBusinessComponentWriter.cs:626`.
3. `ApiPlanListProcedureWriter` tem três `Save()` reais; o da Procedure em
   `ApiPlanListProcedureWriter.cs:953` e o do API em `ApiPlanListProcedureWriter.cs:975`.
3.1. Os dois writers montam uma lista `saveSteps` de triplas `(Label, Action Save, Snapshot)` e a
   executam em laço com progresso e cronômetro — `ApiPlanBusinessComponentWriter.cs:98` e
   `ApiPlanListProcedureWriter.cs:66`. **Em ambos, o primeiro passo da lista é o API.**
   A inversão de ordem exigida por esta frente não está só em `Package.cs`: dentro de cada
   writer, o passo do API precisa passar de primeiro a último.
4. O Apply existe em **dois blocos** no mesmo `Package.cs`: o do Sync, em
   `ExecuteSynchronizeWithTransaction` (`Package.cs:423`), cuja escrita de SDTs só ocorre depois
   do preflight aprovado em `Package.cs:566`, na chamada de `Package.cs:568`; e o do Wizard, em
   torno de `Package.cs:1189`. Os dois desembocam no mesmo helper `TryCreateSdts`, onde está a
   chamada efetiva ao writer (`Package.cs:139-140`) — por isso a linha do helper **não**
   identifica de qual dos dois blocos veio a execução.
5. Os dois blocos já contêm um deferimento **parcial** de B054, com a mesma regra:

   | Seleção | Comportamento atual | Consequência |
   |---|---|---|
   | `GenerateApiObject` sem BC | chama `TryCreateApiObject` | B054 salva o API antes de List, se houver |
   | `GenerateApiObject` + BC, API já existe por nome | não chama; BC absorve | um único Save, no BC |
   | `GenerateApiObject` + BC, API ausente | **chama** `TryCreateApiObject` | B054 cria e salva **antes** do BC |

6. `ApplyList` **não participa** dessa decisão. Em List-only, B054 salva o API antes do
   List — que depois salva de novo.
7. A existência do API é decidida por comparação de **nome**
   (`API.GetAll(...).Any(api => string.Equals(api.Name, apiPlan.ApiName, ...))`), tanto
   no Sync quanto no Wizard.

O diagnóstico dos dois documentos anteriores se confirma, e fica mais preciso: o deferimento não é inexistente —
é **incompleto**. Ele cobre BC com API preexistente e ignora List por inteiro.

### 2.2 O que as sondas mediram

Do registro `2026-09-04-B111-SONDAS-IDENTIDADE-E-DIARIO.md`, seis execuções sobre duas
KBs:

- o `Guid` de um API Object existe desde o `API.Create`, sobrevive a atribuições de
  `Name`/`Description` e ao `Save()`, e o objeto é reencontrável por ele;
- a `Description` preserva 256 caracteres e trunca o excedente em silêncio (`B112`);
- `Save()` sem alteração pendente custa ~0 ms; com alteração, Folder, SDT, Procedure e API
  custam dezenas de milissegundos, inclusive na KB grande;
- a contagem consolidada dos pontos físicos e a fronteira de instrumentação ficam na F2;
  para a F1, o requisito determinante é a ordem dos writers e do único `API.Save()` final.

**Consequência direta para a F1:** a identidade planejada do API é o `Guid` **lido** do
`Create`. Não se atribui `Guid`, não se grava marcador na `Description`, não existe chave
alternativa. Essa é a maior simplificação em relação ao manuscrito expandido.

---

## 3. Escopo da F1

### 3.1 O que entra

1. o predicado que decide se a frente assume a orquestração;
2. um contexto transient de API, carregando o `Guid` lido do `Create`;
3. a ordem física de gravação, com um único `API.Save()` no writer final;
4. um gate de pré-escrita **reduzido**, definido em 4.5;
5. separação entre nome planejado e nome persistido no relatório;
6. sentinelas de contrato e testes de fluxo listados na seção 6.

### 3.2 O que **não** entra, e sai declarado

| Fora da F1 | Vai para |
|---|---|
| seam de persistência injetável, `PersistenceReceipt` | F2 |
| classes de falha C, D e E distinguíveis | F2 |
| diário B111 durável, intenção importada (`intentKind=Imported`) | F3 |
| quatro dimensões de estado, incluindo `journalDurability=Unknown` | F3 |
| recuperação explícita, reconciliação e inventário físico | F3 |
| remoção de API legado com diário importado | F3 |
| implementação do Modo A selecionado e ciclo de vida do diário | F3 |
| habilitação de BC diferida no Wizard com Save recibado | F2 (depende de receipt) |
| expansão da UI do Sync para a matriz do Wizard | fora da sprint |

A F1 **não promete** atomicidade, rollback, recuperação de estado parcial nem detecção de
Save com resultado indeterminado. Uma interrupção no meio da F1 ainda deixa objetos
parciais, mas a forma esperada muda: consumidores podem existir enquanto o API Object
ainda não foi persistido. Essa forma é deliberada, deve ser diagnosticada como estado
parcial e será responsabilidade da F3 quando houver diário. O que a F1 garante é que o
API Object não será persistido antes dos seus consumidores — que é a causa da
degradação observada em campo.

---

## 4. Contrato-alvo

### 4.1 Predicado

```
b111ManagedApply =
    GenerateApiObject || GenerateMetadata || ApplyBusinessComponent || ApplyList
```

Quando falso, o Apply segue pelo contrato existente sem assumir a orquestração da frente.
`GenerateMetadata` entra porque a metadata exige um API reencontrável e precisa participar
da mesma intenção.

O predicado deve existir nos **dois blocos** de `Package.cs`, com a mesma definição.

### 4.2 Writer final por seleção

**A tabela abaixo vale exclusivamente para `GenerateApiObject=true`.** Com a flag falsa não
existe writer final de API e nenhuma linha desta tabela se aplica; o caso está na matriz de
4.6, que é a única fonte para ele.

| Seleção, com `GenerateApiObject=true` | Preparação B054 | Writer final | `API.Save()` físicos |
|---|---|---|---|
| API-only | prepara e persiste | B054 | 1 |
| BC-only | prepara sem salvar | BC | 1 |
| List-only | prepara sem salvar | List | 1 |
| BC + List | prepara sem salvar | List (BC não salva o API) | 1 |

Vale para Sync e Wizard. Com `GenerateApiObject=false`, nenhum writer chama `API.Save()`,
e o API precisa existir e ser validado antes do pipeline.

### 4.3 Contexto transient

Quando BC ou List participar, `ApiPlanApiObjectWriter` deixa de persistir e passa a
entregar um contexto ao writer final. Nome sugerido: `ApiPlanTransientApiContext`.

Conteúdo mínimo:

- o objeto `API` em memória, já configurado (Name, Description, Parent, ServiceGroupSource);
- `PlannedApiGuid` — o `Guid` **lido** do objeto, nunca atribuído;
- `PlannedApiName`;
- `TransactionGuid` e nome da Transaction;
- `ApplicationId` e `OperationId` da aplicação corrente;
- hash do contrato planejado;
- flags da seleção;
- writer final esperado;
- se o API é novo ou reencontrado;
- **se a etapa de Business Component contribuiu para o contrato** — fato resolvido no
  preflight e capturado antes de qualquer mutação do `ServiceGroupSource`; campo distinto
  do anterior e exigido pelo terceiro ramo de 4.8; “API novo” e “BC rodou” são independentes;
- quando o API for reencontrado, o snapshot de identidade, posse e Source persistido usado
  para o preflight, antes da mutação em memória.

Regras:

1. B054 não chama `API.Save()` quando BC ou List participa;
2. B054 não cria um segundo API temporário na KB;
3. os writers de BC e List **não podem** validar o contexto chamando `API.GetAll`,
   `FindApi` ou equivalente para um API novo — o objeto transitório é a fonte autorizada
   até o único Save final;
4. para um API que já existe, a resolução é por identidade validada, não por nome;
5. o writer de List não reavalia posse ou variante do contrato contra um API já mutado em
   memória: usa os fatos validados no preflight e o contexto transitório.

### 4.4 Ordem física

Depois do gate:

1. Folder, se necessária;
2. SDTs selecionados;
3. Procedures-esqueleto e Procedures de consumidores selecionadas;
4. BC, quando selecionado — **sem** salvar o API;
5. List, quando selecionado;
6. o único `API.Save()`, no writer final da tabela 4.2;
7. metadata, quando selecionada.

Nenhum consumidor pode salvar o API antes de seu Source, Rules e Variables estarem
finalizados. A metadata nunca precede o API fisicamente confirmado. Etapa não selecionada
é omitida, e omiti-la não autoriza gravação oculta.

#### 4.4.1 Responsabilidade única por Folder e SDT

A ordem acima **só é verdadeira se uma etapa for dona de cada tipo de objeto**. Hoje não é o
caso, e isso precisa ser resolvido nesta fase, sob pena de a ordem declarada não descrever a
execução real.

Medido por leitura em 2026-09-05:

| Chamada | Onde ocorre |
|---|---|
| `ApiPlanTransactionFolder.CreateOrReencounter` | **cinco** pontos: `ApiPlanSdtWriter.cs:72` (o interno do próprio SdtWriter), `ApiPlanProcedureWriter.cs:53`, `ApiPlanApiObjectWriter.cs:53`, `ApiPlanBusinessComponentWriter.cs:96` e `ApiPlanListProcedureWriter.cs:64` |
| `ApiPlanSdtWriter.CreateOrReencounter` | **quatro** ocorrências em três lugares: os dois ramos do ternário no helper `TryCreateSdts` (`Package.cs:139-140`), `ApiPlanBusinessComponentWriter.cs:94` e `ApiPlanListProcedureWriter.cs:62` |

Numa aplicação com SDTs, Procedures, API, BC e List, o writer de SDT roda três vezes e o de
Folder mais de cinco. Isso não é acidente — é o motivo de existirem `RefreshSdts` e
`RefreshFolders` no índice, cujo comentário registra que “um segundo `CreateOrReencounter`
no mesmo índice (BC/List) pode tentar `CreateSharedFolder` de novo”.

Sem uma regra, três consequências decorrem, e a terceira é a mais séria:

1. a ordem física declarada em 4.4 não corresponde à execução, porque Folder e SDT são
   tocados fora das suas posições;
2. a atribuição de autoria no relatório fica ambígua entre a fase dedicada e o consumidor;
3. na F2, cada passagem vira um ponto de gravação instrumentado, e a sequência de recibos
   passa a mostrar o mesmo objeto várias vezes — arruinando o instrumento que deveria provar
   a ordem.

**Medição de 2026-09-05 — o comportamento medido não registra gravações extras nos casos
cobertos.** Sonda de call sites sobre dois Applies reais na KB `wseducacaospteste`,
Transaction `Teste` com subníveis,
seleção completa (SDTs, Procedures, API, BC, List, metadata):

| Ponto | Primeira geração | Reaplicação |
|---|---|---|
| `SdtWriter.CreateOrReencounter` — entradas | 3 | 3 |
| SDT criado (gravação) | 18, **todas na fase dedicada** | 0 |
| SDT reencontrado — gravações | **0** | **0** |
| SDT reencontrado — skips | 45 | 63 |
| `TransactionFolder.CreateOrReencounter` — entradas | 7 | 6 |
| Folder — gravações | **0** | **0** |

**Nenhuma das passagens extras grava.** As 18 criações de SDT ocorreram todas na primeira
passagem, dentro da fase dedicada, e as passagens de BC e de List só reencontraram. O Folder
foi reencontrado em todas as sete entradas.

Confirmado também na KB grande, Transaction `Empresa` com 44 SDTs próprios: 44 criações,
todas na fase dedicada; **97 reencontros nas passagens de BC e de List, zero gravações**;
Folder reencontrado em sete entradas, zero gravações. Somando as execuções, são mais de 200
reencontros em passagens extras sem uma única gravação.

**Qualificação necessária.** Numa reaplicação sobre estado divergente, o reencontro **pode**
gravar: o `wroteKb` do writer grava quando o plano diverge do persistido. Isso foi observado
uma vez, em `sdtEmpresa_API_ListFilters`, e ocorreu na **fase dedicada** — a única que
executou naquele fluxo —, portanto continuaria autorizado sob a regra. A afirmação correta
não é “o reencontro nunca grava”, e sim “**as passagens de BC e de List não gravam**”. Ver
`2026-09-04-B111-SONDAS-IDENTIDADE-E-DIARIO.md` §10.3.

Portanto, nos estados cobertos pela medição, a regra abaixo **descreve o comportamento
observado**: torná-la explícita preserva as gravações que já ocorrem na fase dedicada e
evita gravação oculta nos writers consumidores. Nos estados de dependência ausente ou
divergente, o reencontro estrito é uma mudança de comportamento intencional; o risco de
gravação extra nos casos medidos não se materializou.

**Regra desta fase:**

> As **fases dedicadas** do `Package` são as únicas autorizadas a **criar** Folder e SDT. As
> chamadas dentro dos writers de consumidor operam em **reencontro estrito**: reencontram e
> validam, e **falham** se precisarem criar ou alterar.

O reencontro estrito é uma mudança de comportamento intencional nos caminhos em que hoje
um writer poderia criar ou alterar uma dependência. Ele é coerente com o que a F1 já exige
em 4.6 — “o writer pode exigir que dependências já existam e estejam coerentes, mas não
pode salvá-las ocultamente” —, mas não deve ser descrito como mera declaração sem efeito
observável.

Consequência prática a validar: com `GenerateSdts=false` e BC selecionado, o SDT que só a
etapa de BC usaria precisa **já existir**; se não existir, o gate bloqueia — em vez de o
writer criá-lo em silêncio, como pode ocorrer hoje.

Cobertura exigida: sentinela de que nenhum writer de consumidor cria Folder ou SDT; fluxo
executável com `GenerateSdts=false` + BC sobre KB sem os SDTs, esperando bloqueio; e
contagem de gravações de Folder e SDT por aplicação, que deve corresponder à ordem de 4.4.

#### 4.4.2 Reencontro e posse sob contexto transitório

Quando o API for reencontrado, `EnsureApi`, `FindApi`, `IsB055ApiObject` e equivalentes não
podem observar um `ServiceGroupSource` já reescrito em memória pelo B054 e tratá-lo como
prova de posse ou de variante do contrato. O preflight deve resolver a identidade, a posse
e o Source persistido antes da mutação; os writers consumidores recebem esses fatos pelo
contexto transitório. A busca por nome pode aparecer somente como diagnóstico de ambiguidade,
nunca como fallback autoritativo.

O teste de reencontro estrito deve incluir Folder ou SDT existente, porém divergente, e
comprovar zero gravações ocultas nos writers de BC e List.

**Custo das passagens redundantes — fica fora desta frente.** A medição mostrou que, mesmo
sem gravar, as passagens extras percorrem todos os SDTs comparando estrutura para decidir o
skip: **42 comparações por Apply** na primeira geração e **42** na reaplicação, além das 21
da fase dedicada. Isso é desperdício mensurável, mas é otimização de desempenho — pertence a
`B082`, não a `B111`. A F1 declara a responsabilidade; não muda quem chama quem.

A habilitação de BC por `transaction.Save()` **não** entra na F1: no Sync ela permanece
bloqueio de preflight, como já é; no Wizard, o callback continua sendo uma dependência
explicitamente não coberta pela garantia isolada da F1 até a F2 transformar a habilitação
em etapa planejada, posterior ao gate e com recibo. A F1 não deve ser declarada segura para
esse cenário Wizard antes da F2.

### 4.5 Gate reduzido

O gate do manuscrito expandido tinha dezesseis validações, várias dependentes de diário e recibos. A F1
mantém as que não dependem de F2/F3:

1. Transaction resolvida por identidade estável, não apenas por nome;
2. nome planejado e nome persistido tratados como campos distintos;
3. contrato final completo, com hash determinístico;
4. seleção e flags coerentes com a entrada;
5. dependências necessárias presentes conforme as flags (SDTs, Procedures, Folder);
6. colisões de nome e posse de objetos existentes;
7. writer final resolvido e disponível para a seleção;
8. ausência de API existente com identidade conflitante;
9. nenhum writer fará descoberta por nome para substituir a identidade planejada.

O gate é barreira de escrita: se falhar, nenhum objeto do pipeline pode ter sido gravado.
As validações que dependem de diário, checkpoint ou estado de durabilidade ficam fora, e
o plano da F3 as reintroduz.

### 4.6 Matriz de `GenerateApiObject`

| `GenerateApiObject` | BC/List/metadata | API existente e próprio | API ausente |
|---|---|---|---|
| true | nenhum | qualquer | permitido — B054 é writer final |
| true | BC ou List | qualquer | permitido — writer final cria e salva uma vez |
| true | metadata | qualquer | permitido se o contrato criar API; metadata vem depois |
| false | qualquer | obrigatório | **bloqueio antes do primeiro Save** |

“API próprio” significa posse validada por identidade e contrato, não nome igual. Havendo
mais de um candidato ou identidade não confirmável, o resultado é bloqueio — nunca escolha
arbitrária.

No Sync, apenas as linhas com `GenerateApiObject=true`, `GenerateSdts=true`,
`GenerateProcedures=true` e `GenerateMetadata=true` são alcançáveis, porque
`BuildSelection` monta um perfil fixo. Os testes das linhas `false` devem declarar-se
Wizard-only, e a documentação não deve sugerir que o Sync oferece toggles que sua UI não
tem.

### 4.7 Relatório

O relatório deve distinguir:

- `PlannedApiName` e `PersistedMainObjectName`;
- `PersistedMainObjectGuid`;
- writer final efetivo;
- número de chamadas físicas a `API.Save()`.

`TryResolveMainObjectFromKb` não pode reencontrar o resultado final apenas por
`collector.ApiName` num fluxo gerenciado pela frente: a resolução usa o `Guid` planejado,
confirmado por releitura. Busca por nome fica como diagnóstico secundário e precisa
reportar ambiguidade.

Além disso, o relatório deve **atribuir a atualização do API Object à etapa que de fato o
gravou**. Move-se apenas a autoria; a designação do API Object como objeto principal
permanece, porque usa o identificador do objeto reencontrado. (Item 6 da seção 4 do plano
aprovado.)

### 4.8 Guarda dos parâmetros de Business Component no writer de List

Este é o ponto mais sutil herdado do plano aprovado — item 5 da sua seção 4 — e o que mais
facilmente quebra em silêncio se for implementado por aproximação.

O writer de List decide `includeBusinessComponentParameters` a partir de um fato resolvido
no preflight. A leitura do API persistido ainda pode ser a fonte do fato para um API
reencontrado, mas deve ocorrer antes da mutação em memória; depois que o API for adiado,
o writer não pode deduzir o valor lendo uma instância possivelmente já reescrita.
A regra:

> Passar `includeBusinessComponentParameters = true` **somente quando a etapa de Business
> Component tiver rodado** — nunca amarrado ao booleano de "List será aplicado".

A condição de guarda é **“a etapa de Business Component rodou”**, não “a etapa de List será
aplicada”. Amarrar o valor forçado ao booleano do List produziria, no caminho legítimo
“List sem Business Component”, um API Object declarando serviços que delegam a Procedures
inexistentes e referenciando SDTs que só a etapa de Business Component cria.

#### O caso `List-only` com API novo — o buraco que o adiamento abre

O plano aprovado dizia: fora do caso “BC rodou”, o writer **continua deduzindo o valor do
API Object persistido**. Isso funcionava porque, naquele desenho, B054 já tinha persistido
o API antes de a etapa de List rodar.

**Nesta F1 isso deixa de valer.** Na seleção `List-only`, B054 prepara sem salvar e o List é
o writer final: no momento em que ele precisa do valor, o API novo **ainda não existe na
KB**. Não há o que ler. Manter a redação do plano aprovado aqui produziria uma leitura de
objeto inexistente — falha imediata, ou pior, dedução a partir de um objeto homônimo alheio.

A regra completa, em três ramos, avaliados nesta ordem:

| Situação | `includeBusinessComponentParameters` | Origem do valor |
|---|---|---|
| a etapa de Business Component rodou nesta aplicação | **`true`** | explícito, pela guarda acima |
| BC não rodou **e** existe API persistido e próprio | dedução, como hoje | fato capturado no preflight a partir do API persistido, antes da mutação |
| BC não rodou **e** o API é novo (contexto transient) | **`false`** | fato capturado no preflight e transportado no contexto transient |

O terceiro ramo é a novidade desta fase, e é correto por construção: um API que está sendo
criado agora, numa aplicação em que a etapa de Business Component não participou, não tem —
nem poderia ter — parâmetros de Business Component no contrato.

Por isso o `ApiPlanTransientApiContext` de 4.3 precisa carregar explicitamente **se a etapa
de Business Component contribuiu para o contrato** e, quando aplicável, o fato capturado
do Source persistido antes da mutação. Não basta o writer de List saber que recebeu um
contexto transient: “API novo” e “BC rodou” são independentes, e a combinação BC+List cai
no primeiro ramo mesmo com API novo.

#### Cobertura exigida

- sentinela: o writer de List não lê o API persistido para decidir o valor quando recebe
  contexto transient;
- fluxo executável **`List-only` com API novo**: o Source resultante não declara parâmetros
  de Business Component, e nenhuma leitura de API persistido ocorre no caminho;
- fluxo executável **“List sem Business Component” sobre API preexistente**: segundo ramo,
  fato capturado antes da mutação, sem leitura posterior do API já reescrito;
- fluxo executável **BC+List com API novo**: primeiro ramo, valor `true` mesmo sem API
  persistido.
- fluxo executável **BC+List sobre API preexistente na variante B055**: o valor e a posse
  são resolvidos antes da mutação e não dependem do Source transitório já alterado.

### 4.9 Trilha de gravação na Output

O plano aprovado especificou, em sete alíneas, uma linha por objeto gravado nos dois
writers, para que a ordem efetiva fique registrada — hoje ela é observável ao vivo na janela
de andamento, mas não chega à Output. A especificação é preservada integralmente:

- **a.** o molde é o callback de escrita de SDT da **etapa dedicada de SDTs**, que emite na
  Output e alimenta o relatório — não o homônimo das etapas de BC e de List, que só alimenta
  o relatório;
- **b.** a emissão é por callback **dentro do laço**, não por lista devolvida ao
  orquestrador: quando o writer lança no meio, não haveria lista, e é nas interrupções que a
  evidência importa;
- **c.** a linha sai **depois** de a gravação retornar; não emitir linha “gravando” antes;
- **d.** a linha carrega o **rótulo da etapa**, para que autoria no relatório e trilha contem
  a mesma história;
- **e.** alimenta **somente** a Output, nunca o relatório final, sob pena de duplicar as
  Procedures;
- **f.** usa a variante de escrita que **não força a exibição** do painel — várias chamadas
  de dentro do laço, com o diálogo modal aberto, disputariam primeiro plano;
- **g.** limitação aceita: os SDTs gravados dentro dessas duas etapas continuam sem aparecer
  na Output; a trilha terá um intervalo sem SDTs, e quem ler a captura precisa saber que é
  esperado.

Esta trilha é diagnóstico, não recibo. Ela não substitui o seam da F2, e a F2 não a torna
supérflua: uma vive na Output para leitura humana, o outro vive em memória para verificação
executável.

---

## 5. Mudanças previstas por arquivo

| Arquivo | Mudança |
|---|---|
| `Src/Extension/Package.cs` | predicado nos dois blocos; deferimento de B054 estendido a List e a BC com API ausente; ordem física; resolução final por identidade |
| `Src/Domain/ApiPlan.cs` | transportar `TransactionGuid`, `ApplicationId`, `OperationId`, identidade planejada e fatos resolvidos no contrato do plano |
| `Diagnostics/ApiPlanApiObjectWriter.cs` | separar preparação de persistência; devolver contexto transient; manter o Save só no caminho API-only |
| `Diagnostics/ApiPlanGenerationStateReader.cs` | fornecer identidade e estado persistido ao preflight sem substituir a identidade planejada por nome |
| `Diagnostics/ApiPlanBusinessComponentWriter.cs` | aceitar o contexto transient; mover o passo do API para o **fim** de `saveSteps`; não salvar o API quando List participa; salvar uma vez quando for o writer final |
| `Diagnostics/ApiPlanListProcedureWriter.cs` | aceitar o contexto transient; mover o passo do API para o **fim** de `saveSteps`; receber `includeBusinessComponentParameters` explícito conforme a guarda de 4.8; executar o único `API.Save()` quando participar |
| ambos os writers de consumidor | callback de trilha na Output conforme 4.9 |
| `Diagnostics/ApiPlanWritePreflight.cs` | gate reduzido da seção 4.5 |
| `Diagnostics/ApiPlanApplicationFinalReport.*` | campos planejado × persistido e contador de `API.Save()` |
| `Tests/` | sentinelas e fluxos da seção 6 |

Nenhum caminho antigo pode sobreviver chamando B054 com `API.Save()` antes de BC ou List
quando `ApplyBusinessComponent` ou `ApplyList` for verdadeiro.

---

## 6. Testes da F1

### 6.1 Sentinelas de contrato

1. o predicado existe e é idêntico nos dois blocos de `Package.cs`;
2. `ApiPlanApiObjectWriter` só chama `API.Save()` no caminho API-only;
3. `ApiPlanBusinessComponentWriter` não chama `API.Save()` quando List participa;
4. existe exatamente um ponto de `API.Save()` por fluxo, no writer previsto;
5. B054 oferece contexto transient quando BC ou List participa;
6. os writers não chamam `API.GetAll`/`FindApi` para validar um API novo vindo do contexto;
7. `PlannedApiName` e `PersistedMainObjectName` são campos distintos;
8. não existe resolução final por nome isolado;
9. o Sync declara o perfil fixo e não anuncia flags que a UI não oferece;
10. a guarda de 4.8 depende de a etapa de Business Component ter rodado, e não do booleano
    de List;
11. a trilha de 4.9 emite depois da gravação, com rótulo de etapa, sem alimentar o relatório
    e sem forçar a exibição do painel;
12. a guarda de 4.8 usa somente fatos capturados antes da mutação do API reencontrado;
13. a **quantidade** de gravações do API Object em cada writer, não só a presença dos
     trechos.

As sentinelas 10 a 13 vêm do plano aprovado e devem ser hospedadas onde ele indicou: no
teste do relatório final de aplicação, que já lê os três arquivos e já está registrado no
checker pré-push. **Sem criar gate novo**, e com eficácia verificada por mutação.

Sentinelas são necessárias e insuficientes: elas provam forma, não comportamento.

### 6.2 Fluxos executáveis

Nas oito primeiras linhas, `GenerateApiObject=true`; nas seguintes, o que a coluna disser.

| Entrada | Seleção | Resultado exigido |
|---|---|---|
| Wizard | API-only | um `API.Save()`, em B054 |
| Wizard | BC-only | um `API.Save()`, no BC |
| Wizard | List-only | um `API.Save()`, no List |
| Wizard | BC + List | um `API.Save()`, no List |
| Sync | sem BC/List | um `API.Save()`, em B054 |
| Sync | com BC | um `API.Save()`, no BC |
| Sync | com List | um `API.Save()`, no List |
| Sync | com BC + List | um `API.Save()`, no List |
| Wizard | `GenerateApiObject=false` + BC, API próprio existente | Procedures de BC atualizadas; **zero** `API.Save()` |
| Wizard | `GenerateApiObject=false` + List, API próprio existente | Procedure de List atualizada; **zero** `API.Save()` |
| Wizard | `GenerateApiObject=false` + BC + List + metadata, API próprio | todos os consumidores usam o mesmo API existente; **zero** `API.Save()`; metadata gravada por último |
| Wizard | `GenerateApiObject=false` + qualquer combinação, API ausente | bloqueio antes do primeiro Save de qualquer objeto |
| Wizard | `GenerateApiObject=false`, API próprio | consumidores atualizados sem `API.Save()` |
| Wizard | `GenerateApiObject=false`, API ausente | bloqueio antes do primeiro Save |
| Wizard | SDT/Procedure `false` | nenhuma gravação da etapa desmarcada |
| Sync | BC sem habilitação na Transaction | bloqueio antes de qualquer Save |
| Wizard | **List-only com API novo** | `includeBusinessComponentParameters=false` vindo do contexto transient; **nenhuma leitura de API persistido** no caminho — terceiro ramo de 4.8 |
| Wizard | **List sem Business Component**, sobre API preexistente | segundo ramo de 4.8: dedução pelo API persistido, comportamento preservado |
| Wizard | **BC+List com API novo** | primeiro ramo de 4.8: valor `true` mesmo sem API persistido |
| Wizard | API preexistente na variante de List sem parâmetros de BC, reaplicado com as duas etapas | os parâmetros de BC aparecem, provando que a guarda é necessária |

Os dois últimos são os cenários D e G do plano aprovado, e a receita do G está na seção 6
daquele documento: gerar com Business Component desabilitado marcando Get/Create/Update e
List; habilitar Business Component na Transaction; reaplicar com as duas etapas. O estado
final do cenário D é o inicial do G — encadeie os dois.

A contagem de `API.Save()` é o critério central. Sem o seam da F2, ela é verificada por
instrumentação simples do caminho de gravação, e não por interceptação injetada — o que a
F1 declara como limitação, não como equivalente.

---

## 7. Validação na IDE

Reinstalar a DLL conforme a política do repositório e repetir a validação **depois** da
DLL que contém a alteração; evidência de DLL anterior não vale para o gerador novo.

Numa Transaction de teste com objetos suficientes para exercitar SDT, Procedure, API, BC
e List:

1. Sync sem BC/List, com BC, com List, com BC+List;
2. Sync com BC selecionado numa Transaction sem BC habilitado — deve bloquear antes de
   qualquer gravação;
3. Wizard em API-only, BC-only, List-only, BC+List;
4. Wizard com `GenerateApiObject=false`, com API próprio e com API ausente;
5. Wizard com SDT e Procedure desmarcados;
6. em todos: o API aparece uma vez, e o relatório traz `Guid` persistido e writer final.

Registrar, na mesma passagem, o tempo de Apply antes e depois da mudança na KB grande. A
F1 não é frente de desempenho, mas a ordem física muda e a medição é barata.

---

## 8. Critérios de aceite

1. Os quatro fluxos de API têm exatamente uma chamada física a `API.Save()`.
2. O Save ocorre no writer final da tabela 4.2, em Sync e em Wizard.
3. B054 não salva o API quando BC ou List participa, inclusive quando o API é novo.
4. O gate reduzido ocorre antes de qualquer gravação do pipeline.
5. A identidade planejada é o `Guid` lido do `Create`; nenhum código atribui `Guid` nem
   grava marcador de identidade na `Description`.
6. API existente é resolvido por identidade e contrato, não por nome isolado.
7. O relatório separa planejado de persistido e informa o writer final.
8. `GenerateApiObject=false` com API ausente bloqueia antes do primeiro Save.
9. Flags de SDT e Procedure em `false` não produzem gravação oculta.
10. Nenhuma regressão nos fluxos existentes de Sync, Wizard e relatório.
11. A guarda de 4.8 depende de a etapa de Business Component ter rodado; “List sem Business
    Component” continua produzindo um Source correto, e `List-only` com API novo resolve o
    valor pelo contexto transient, sem ler API persistido.
12. A trilha de 4.9 registra a ordem efetiva na Output, conforme as sete alíneas.
13. O relatório atribui a atualização do API Object à etapa que de fato o gravou.

Os critérios 11 a 13 vêm do plano aprovado de 2026-09-04 e não são novidade desta fase:
estão aqui para que o fatiamento não os perca.

Falhando qualquer critério, a F1 não está pronta para aceite.

---

## 9. Riscos

| Risco | Mitigação prevista |
|---|---|
| o writer de List deriva parâmetros do contrato **persistido** do API; com o API ainda em memória, essa derivação quebra — em `List-only` com API novo não há objeto a ler | **endereçado na seção 4.8**, com a regra de três ramos e um campo novo no contexto transient. Segue sendo o ponto mais delicado da F1: cobrir os três fluxos executáveis de 4.8 antes de tocar em qualquer outra coisa |
| dois blocos de Apply divergirem de novo | sentinela que compara o predicado e a ordem entre os dois blocos |
| o deferimento parcial existente mascarar regressão em BC com API preexistente | fluxo executável específico para “BC com API já existente”, comparado ao comportamento atual |
| ausência de seam tornar a contagem de Saves frágil | declarada como limitação; a F2 substitui a instrumentação por interceptação |

---

### 9.1 Consolidação de 2026-09-07

- A F1 permanece independente da escolha do diário: ela define preflight, ordem e writer
  final, enquanto F2 e F3 definem observabilidade e durabilidade.
- Cada aplicação recebe um `OperationId` novo; o `ApplicationId` nasce antes da primeira
  gravação gerenciada e segue no contexto transient. A F1 não mantém histórico e não cria
  um objeto adicional da KB para isso.
- API existente é associado por `PlannedApiGuid` validado; nome é apenas diagnóstico. API
  novo sem identidade estável bloqueia antes da primeira gravação.
- B109 e B110 permanecem fora da S-B111; esta seção não os transforma em pré-requisitos.

## 10. Fontes

- `Docs/Implementation/2026-09-04-PLANO-API-OBJECT-GRAVACAO-UNICA.md` (plano aprovado em 2026-09-04; suas mudanças 1 a 7 estão preservadas em 4.8 e 4.9)
- `Docs/Implementation/2026-09-04-B111-MANUSCRITO-EXPANDIDO-V24.md` (expansão nunca aprovada; referência de F2 e F3)
- `Docs/Implementation/2026-09-04-B111-SONDAS-IDENTIDADE-E-DIARIO.md` (medições)
- `Docs/Implementation/2026-09-04-EVIDENCIA-IDE-DRIFT-API-OBJECT.md` (linha de base de campo)
- `Src/Extension/Package.cs`, blocos de Sync e de Wizard
- `Src/Extension/Diagnostics/ApiPlanApiObjectWriter.cs`
- `Src/Extension/Diagnostics/ApiPlanBusinessComponentWriter.cs`
- `Src/Extension/Diagnostics/ApiPlanListProcedureWriter.cs`
- `Src/Extension/Diagnostics/ApiPlanWritePreflight.cs`
- `Docs/Foundation/06-BACKLOG_v0.1.md` — `B111`, e os colaterais `B112`, `B113`, `B114`
