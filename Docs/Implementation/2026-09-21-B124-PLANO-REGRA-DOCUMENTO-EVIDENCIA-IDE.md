# B124 — Plano: regra do documento de evidência de aceite/smoke IDE

Data: 2026-09-21.
Item de backlog: `B124` — `Docs/Foundation/06-BACKLOG_v0.1.md`, linha 283, e nota operacional de
2026-09-16 (linhas 347–381).
Checkpoint: `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md` — «Próxima ação única» vigente (linhas 144–151).
Estado: **aprovado para execução** (decisão humana de 2026-09-21). A revisão por pares foi
encerrada por **congelamento do design** — `resubmissionDeclinedByHuman`, motivo «prova transferida
para implementação/self-test», `RoundId 20260921-b124` (§11). Este arquivo **não** autoriza, por si
só, instalação, push ou corte de release.

**Revisão — 2026-09-21 (parecer externo).** A decisão de §3 foi reformulada para uma régua
auditável nos dois sentidos — na dúvida, cria-se; a exceção é justificada por escrito no
checkpoint —, em vez da condicional N1/N2, que classificava casos mas não dava mecanismo ao smoke
trivial. O molde de §3.4 ganhou profundidade proporcional. A primeira aplicação (§6) permanece: o
princípio de não migrar histórico por padrão não a dispensa.

**Revisão 2 — 2026-09-21 (parecer externo).** O §6 mandava o `Validated` da `alpha.8` (linha 88)
passar a citar o doc retroativo — e essa linha está em seção de versão **publicada**, imutável pelo
`AGENTS.md`. Corrigido: linhas 62/88 não são tocadas; o laço fecha pela entrada nova de
`[Unreleased]` e pela rastreabilidade do próprio doc retroativo (§3.5, §4 e §6).

**Revisão 3 — 2026-09-21 (painel de pares: Codex GPT-5.6-terra e Claude Opus 5).** Os dois pareceres
apontaram gaps P1 de ancoragem, de alcance do G6 e de protocolo do checkpoint; as correções estão em
§3.3, §3.5, §4, §5 e §7, com a disposição item a item em §11. Nenhuma decisão D1–D8 foi revertida; a
discordância sobre D8 ficou registrada.

**Revisão 4 — 2026-09-21 (segunda rodada do painel).** Fechados: a matriz passou a ser devida só nos
campos que a sessão produziu (`B123` §6 declarado piso mínimo); G6 virou «gera entrada de release»,
verificável no diff; o prazo ganhou definição objetiva de fechamento e condição para adiar; a
dispensa passou a exigir sessão e gatilhos declarados; o §7 foi reordenado (promoção no mesmo
commit); D6 ganhou aviso não bloqueante no checker. Disposição na terceira rodada de §11.

**Revisão 5 — 2026-09-21 (terceira rodada do painel).** Os dois pareceres convergiram no mesmo P1: o
aviso do checker não pode ir em `manualRequired` — vai no canal `warnings`, com as duas afirmações
«só spike B000–B006» atualizadas (`AGENTS.md` 277 e `notCovered`) e contrato de teste. Também
fechados: D5 (seção `Added` e redação), G6 restrito a entrada que cite sessão de campo, derrogação
expressa da exceção retroativa, recorte 142–151 × 142–149, rascunho promovido declarado e `B127`
fora da ação única. Disposição na quarta rodada de §11.

**Revisão 6 — 2026-09-21 (quarta rodada do painel).** Fechados: piso × prazo («Piso não é prazo» em
§3.3 — o G6 não antecipa o prazo de §3.5), o resíduo de D8 (o `B127` sai do escopo), a detecção do
aviso por **conteúdo do patch** com cardinalidade e negativos específicos, o núcleo de §3.4 (commit
**ou** evidência de instalação, com «não registrado») e a justificativa de §3.4.2 (o alcance expira,
não o registro). Disposição na quinta rodada de §11.

**Revisão 7 — 2026-09-21 (quinta rodada do painel).** Fechados: o detector do aviso cobre `Fixed`
além de `Validated`, com detecção **posicional** dentro do bloco `[Unreleased]` (o `### Validated` é
homônimo nas versões publicadas), código como prefixo textual e a reavaliação da asserção de canal
vazio do teste; `warnings` entra nos passos 3–4 da «Revisão pré-push»; a frase fixa ganha remissão
no «Protocolo de atualização»; e a proveniência da DLL da Sessão B passa a **não registrada** — a
cronologia desmente o `687a494` como origem da build instalada (12:46:03 × 22:43:13).

**Revisão 8 — 2026-09-21 (sexta rodada do painel — incompleta).** O Codex apontou que a redação
operacional reabria a exceção no G6 (a entrada do `CHANGELOG` e a mensagem do checker diziam «doc
**ou** exceção»); corrigido na v7: no `:validated`/`:fixed` o documento é obrigatório sem exceção, e
a frase de dispensa só aparece no aviso de `:checkpoint`. O Claude Code ficou em **quota** nesta
rodada (`state=quota`, `reason=quota-signal`) — a rodada tem uma só família e **não fecha o piso**;
o reenvio ao Claude depende do ciclo de uso, e a decisão de congelar o papel ou aguardar é humana
(§11, sétima rodada).

**Revisão 9 — 2026-09-21 (segunda opinião solo, Codex GPT-5.6-terra).** O parecer apontou um único
gap bloqueante — a entrada nova de `[Unreleased]` não citava o doc retroativo, contrariando o laço
de §6 —, corrigido na v8: D5, §4.6 e §6 passam a exigir a citação do plano e do doc retroativo na
entrada `Added`. Os demais pontos (D8 como decisão humana a registrar no commit; aviso do checker
como candidato, com negativos de teste) já estavam no plano. A segunda opinião foi pedida ao Codex
porque o Claude Code está em limite mensal de uso. A **releitura de confirmação da v8** ficou
bloqueada: Claude Code em limite mensal (ciclo às 21:30) e Codex em limite de uso (retry às 23:31);
o parecer da v7 vale como insumo datado, e a confirmação fica pendente de nova janela.

**Revisão 10 — 2026-09-21 (segunda opinião solo, subagente nativo `reviewer-ro`).** O parecer
apontou um P1 bloqueante — o contrato de teste do aviso prometia um «negativo offline» incompatível
com a detecção posicional —, corrigido na v9: o disparo offline passa a **falso positivo aceito e
documentado**, e as duas enumerações de teste foram equalizadas. Também corrigidos: a mensagem do
aviso remete ao prazo de §3.5, as asserções do teste que travam o texto de
`notCovered`/`AGENTS.md` entram na revisão, as entradas do `[Unreleased]` do `B123` ficam declaradas
como satisfeitas pela remissão ao plano §6, e quatro precisões (token de validação, justificativa do
`OperationId`, linhas do `.gitignore`, mecanismo de edição ancorada). O texto promovido passa a ser
o da **v9**, que é o lido nesta rodada.

**Revisão 11 — 2026-09-21 (confirmação do subagente nativo).** O P1 e os P2-1 a P2-4 da v9 foram
fechados; a confirmação apontou um P1 novo de uma frase — o §5 reabria a exceção no
`Validated`/`Fixed` e omitia `### Fixed` —, corrigido na v10, junto com três precisões (marcar
supersessões no §11, qualificar a remissão das entradas do `B123` e tornar a mensagem do checker
autocontida).

**Revisão 12 — 2026-09-21 (confirmação final do subagente nativo).** Veredito: **plano pronto para
execução, sem gap bloqueante**. As duas precisões P3 da confirmação foram incorporadas na v11 (o §5
passa a condicionar o «sem exceção» a «quando a entrada cita sessão de campo como prova»; a frase do
passo 4 da «Revisão pré-push» é qualificada ao prefixo `evidence-doc-required:*`), e a redação de
§3.5 foi limpa. O subagente dispensou nova rodada.

**Proveniência das rodadas de pares.** Os manuscritos, vereditos e resumos vivem em
`Temp\revisao-por-pares\20260921-b124\` (`manuscrito-vN.md`, `dispatch\<RoundId>\…`) — pasta
ignorada pelo Git (`.gitignore`, linhas 88–89). São **prova local, não versionada**: o plano os cita
como registro datado, e o commit de fechamento não os carrega.

Local: `Temp/` é ignorado pelo Git (`.gitignore`, linhas 88–89). Se e quando aprovado, o plano deve
ser promovido para `Docs/Implementation/2026-09-21-B124-PLANO-REGRA-DOCUMENTO-EVIDENCIA-IDE.md` e o
item `B124` passa a apontar para lá, no molde de `B122` e `B123`.

## 0. Cobertura da leitura

Li, para este plano:

- o item `B124` e a nota operacional no documento 06 (linhas 283 e 347–381), incluindo os quatro
  pontos que mandam «estudar e decidir»;
- o checkpoint: «Próxima ação única» (144–151), os itens 142–149 do log (642–649), os itens 155–159
  (655–659), «Evidência da frente encerrada» (433–454) e o «Protocolo de atualização» (746–759);
- o `CHANGELOG.md`: bloco `[Unreleased]` (11–39) e as seções `Validated`/`Fixed` das versões
  `0.1.0-alpha.8` (59–94) e anteriores próximas;
- o documento 15, seção 18 «Evidências Esperadas» (359–372) e 18.1 «KBs de Teste» (376–383);
- o plano `B082` (linhas 37–66, «Retomada em duas sessões»), o plano `B123` (integral) e o cabeçalho
  do aceite `2026-09-03-B082-ETAPA-1A-ACEITE.md`;
- o documento 24 nos trechos que citam evidência por item (332–339, 393–415) e o documento 15 nos
  trechos que citam evidência de runtime (165–177);
- a lista de gates do `scripts/Invoke-PrePushMechanicalChecks.ps1` e o `AGENTS.md` nas seções
  «Revisão pré-push do repositório», «Promoção de frente e próximo passo» e «Fechamento de spikes e
  sondas temporárias»;
- os commits de 2026-09-16 que produziram a Sessão B (`9341f4f`, `78abfe1`, `585af4b`, `687a494`).

Não reli por inteiro os documentos 05, 11, 14, 24 e 28. Risco residual: alguma convenção de
nomenclatura de documento em seção que a sondagem por `evidência` não cruzou. A varredura por
`evidência` no `Docs/Foundation/*.md` não encontrou norma concorrente à do documento 15; o que
existe é prática (documentos 24 e checkpoint citando arquivos de `Docs/Implementation/`).

## 1. Problema

A regra nunca foi escrita. O resultado é assimétrico:

- **Há evidência dedicada** para os aceites de etapa e de frente recentes: 1A, 1B, F1, F2, a série
  P0–P1/P2/P3/P4–P7 e o aceite da P8, B099v e B100. O documento 24 cita esse arquivo por item
  («evidência `Docs/Implementation/...`»), e o `Validated` do `CHANGELOG` cita o arquivo. A prática
  **não é universal**: na «Evidência da frente encerrada» do checkpoint, `B095`–`B099a` citam gates
  e fixtures, não documento, e o `B102` aparece lá sem citar o próprio doc.
- **Não há evidência dedicada** para a Sessão B do `B082` (itens **142–149** do checkpoint): os
  **oito cenários** de §6 — um deles com falha, conserto e reteste — registrados só no log do
  checkpoint, no
  parágrafo de progresso do plano `B082` e no `Validated` do `CHANGELOG` (linha 88). Quem retoma a
  frente numa sessão nova precisa remontar a bateria a partir de um parágrafo longo do log — foi
  exatamente esse o sintoma que abriu o `B124` (nota do documento 06, linhas 355–361).

O custo não é hipotético: o item 142 registrou, «observado sem correção», o Preview do segundo
Remover listando alvos já ausentes; sem doc, aquele achado virou o `B125` apenas como texto, e a
reprodução do cenário continua pendente. O mesmo padrão de dívida se repete no `B127` (hardenings
de 2026-09-21 ainda sem IDE).

A prática também mostra o caminho aceitável: o smoke §6 do `B123` ficou **dentro do plano**
(seções 289–319) e o item 159 delimitou o alcance da medição («não cobre `86ef414` nem
`c358fb2`»). Isso é evidência dedicada em `Docs/Implementation/`, mesmo sem arquivo próprio. O que
falta é o **critério** que decide quando esse registro é exigido, e não deixado ao acaso.

## 2. Estado atual do contrato (evidência coletada)

### 2.1 O que já está escrito

| Fonte | O que diz hoje |
|---|---|
| Documento 15, §18 (linhas 359–372) | «Registrar: prints, logs, KB teste usada, versão GeneXus, casos executados, falhas encontradas.» Não define obrigatoriedade, molde, momento nem onde vive |
| Documento 15, §18.1 | KB de teste pequena, fora de produção, com backup |
| Documento 24 | Prática consolidada: cita o arquivo de evidência de cada item/frente (ex.: linhas 332–333, 395–403, 415) |
| `CHANGELOG.md` `Validated` | Cita arquivo de `Docs/Implementation/` quando existe (linhas 87, 90–94) ou apenas itens do checkpoint quando não existe (linha 88) |
| `AGENTS.md`, «Revisão semântica de contrato runtime» (item 6) | Exige, para cada mudança de contrato, declarar evidência de runtime e «evidência de runtime cuja DLL precede alguma mudança de emissor» — escopo de contrato, não regra de evidência de aceite |
| `AGENTS.md`, «Promoção de frente» | Varredura do ID concluído e do próximo; **receberá** (proposto) o item de conferência da régua — **não existe** uma seção «checklist de fechamento de frente» |
| `AGENTS.md`, «Fechamento de spikes» | Checklist de retirada de sonda e reinstalação; vale só para `B000`–`B006` |

### 2.2 O caso que provou o gap (142–149)

A Sessão B do `B082` (2026-09-16) exercitou, na KB `wsEducacaoSpTeste` / Transaction `Teste`:

| Item | Cenário |
|---|---|
| 142 (c) | Recuperar sob guarda; diálogo «nada a recuperar»; Remover abortado; segundo Remover → `GateBlocked` + oferta; menu Recuperar → `ContinueRemovePass` → `Removed` |
| 143 | Guarda inversa; abort sem apagar o objeto do clique; conferência por `genexus_inspect` |
| 144 | Folder movido para fora do contêiner esperado (D10); Folder permanece |
| 145 | Preview divergente → zero exclusões (`ApiObject 'apiTeste'`) |
| 146 | Casca × relatório nos três desfechos (sucesso, abort, bloqueio) |
| 147 | Folder tipado não-vazio: **falhou** com `RetryBudgetExhausted`; conserto `IsFolderEmpty` |
| 148 | Reteste do caso 6: `PreservedNonEmptyFolders`, aviso `nao ficou vazio`, `SuccessWithWarnings` |
| 149 | `DEMO.md` §21 conferido contra o exercido |

Proveniência registrada na época: instalação da DLL em `GeneXus18\Packages` batendo
`LastWriteTime` 12:46:03 (item 143). O item 143 não nomeia commit, e a cronologia fecha a porta: o
commit `687a494` (Fatia B + pendência 4) é das **22:43:13** do mesmo dia — depois da instalação —,
então **não** pode ser a origem da DLL instalada; o que a antecedeu foi o código de `78abfe1`
(11:52) e o que ainda não estava commitado. A build instalada fica marcada como **não registrada**.
Os itens 142 (a) e (b) antecedem esse install e também não registram a DLL — o doc retroativo
declara isso, não infere.

### 2.3 O que o release promete

O `Validated` da `0.1.0-alpha.8` (linha 88) cita os itens 142–151 do checkpoint como evidência de
uma entrega que **saiu em release**. A nota do corte em `Docs/Releases/0.1.0-alpha.8.md` linka
`Docs/Implementation/` para outro assunto (`B088`, linha 77), mas não aponta doc dedicado para o
aceite da Sessão B — que não existe. É o caso mais forte de obrigatoriedade: quem lê o release não
encontra a bateria em lugar nenhum além de um log numerado de 759 linhas. (O corpo do GitHub
Release não foi inspecionado neste plano.)

## 3. Contrato proposto — a regra

### 3.1 Termos

- **Evidência de campo**: registro de sessão na IDE, no runtime HTTP ou na KB real que prova o
  estado de uma DLL contra KB/ambiente nomeados. Não inclui gate offline (tem o próprio gate) nem
  corte de release (tem o próprio rito). O enunciado do item no documento 06 fala em «aceite ou
  smoke IDE»; esta regra **amplia** o escopo para runtime HTTP (o `B120` tem o mesmo modo de falha)
  e o enunciado do 06 será realinhado no fechamento (§7, passo 4).
- **Registro mínimo**: item do checkpoint, no mesmo commit da sessão, mais a entrada correspondente
  no `CHANGELOG` (`Validated` e/ou `Fixed`) quando aplicável.
- **Documento dedicado**: arquivo próprio em `Docs/Implementation/`, **ou** seção delimitada e
  intitulada como evidência no documento da frente (plano/aceite), com o núcleo de §3.4. Citável
  por link estável.

### 3.2 Regra de decisão (régua)

**Na dúvida, cria-se o documento dedicado.** A regra é auditável nos dois sentidos: ou existe o
documento, ou existe, **no item do checkpoint**, a exceção justificada com a frase fixa:

> `B124: sem documento dedicado porque <motivo objetivo>` — com a sessão identificada e os gatilhos
> G1–G6 declarados como não aplicáveis

Não há terceiro estado — o silêncio. Foi o silêncio que produziu o caso 142–149: nenhum documento e
nenhuma justificativa, só um parágrafo de log. A frase obriga quem dispensou a nomear o motivo **e a
dizer qual sessão foi dispensada e por que nenhum dos seis gatilhos incide**; sem isso, a dispensa
não é auditável.

### 3.3 Piso: seis situações sem exceção

Nestas, o documento dedicado é devido e a frase de §3.2 não se aplica:

| # | Situação (piso) | Exemplo do repositório |
|---|---|---|
| G1 | **Fechamento** — a sessão é o aceite que fecha etapa, frente, sprint ou residual, ou que promove a próxima ação única | 1A, 1B, F1, F2, P8, `B123` §6 |
| G2 | **Bateria** — ≥2 cenários/casos na mesma sessão, ou 1 caso com ≥2 dimensões de verificação (dois environments, duas variantes de estado da KB) | 142–149; `B099v`; cenário 8 da P8 |
| G3 | **Medição durável** — a sessão produz número que vira orçamento, meta, linha de base ou limiar de comparação futura | 9a/9b da P8; tabela do aceite 1A |
| G4 | **Retomada** — a sessão continua uma bateria já documentada (parte N, nova sessão, novo cenário da mesma frente) | P3 após P2; Sessão B após Sessão A |
| G5 | **Residual operacional** — a sessão gera item de backlog cuja reprodução/conserto depende de passos além do texto do item | item 142 → `B125`; `B127` |
| G6 | **Entrada de release** — a sessão de campo é citada como prova por entrada `Validated`/`Fixed` em `[Unreleased]` ou em release ainda não publicado (um `Fixed` puramente offline não incide) | `Validated` da `alpha.8` — caso histórico preexistente, mantido intacto; a exigência é prospectiva |

Fora do piso, vale a régua: o smoke de um clique, sem medição, sem residual e com a DLL registrada
pode ficar no item — **desde que a dispensa esteja escrita**.

O piso não depende do número de cenários: G1, G3, G5 e G6 incidem até com um único cenário; G2 e
G4 já supõem mais de um.

**Alcance real do G6:** como `Validated` é «o que foi exercido na IDE», toda sessão de campo que
gera entrada de release cai nele, mesmo com um cenário — o caminho leve de §3.2 fica para sessões
que **não** geram entrada. O plano assume isso: não é buraco, é o preço de a evidência ser achável
por quem lê o release. A formulação é verificável no diff: basta ver se a entrada existe e se cita
sessão de campo.

**FAIL também é evidência:** sessão que só produziu falha e conserto não dispensa o documento — ele
registra a falha e aponta o reteste (caso do item 147).

**Piso não é prazo:** o piso elimina a *dispensa* — a frase de §3.2 não se aplica —, mas não
antecipa o *prazo*: o documento continua devido até o fechamento (§3.5), e a entrada pode citar o
item do checkpoint enquanto ele não existir. Vale inclusive para o G6.

### 3.4 Molde proporcional

**Núcleo (sempre — e cabe em uma página quando o ensaio é pequeno):**

1. **Cabeçalho** — data da sessão; frente/item; KB e Transaction (ou rota/serviço); versão do
   GeneXus quando importa; data de redação, se diferente; resultado (`PASS`, `FAIL`, aceito com
   ressalva, observação sem decisão).
2. **Proveniência e alcance** — commit curto **ou** evidência de instalação (ex.: `LastWriteTime`),
   e **o que a DLL medida não inclui** — o «alcance da medição» que o item 159 exercitou para o
   `B123`. Campo que a sessão não produziu é declarado «não registrado» (mesma cláusula da matriz),
   e a versão do GeneXus entra quando importa. Sem isso, a evidência não diz a que DLL se aplica, e
   o **alcance** — não o registro — expira em silêncio (`AGENTS.md`, revisão semântica de contrato
   runtime, item 5).
3. **Cenário(s)** — preparação; passos na ordem; resultado obtido (Output, diálogo, inspeção na
   KB); PASS/FAIL. Registrar só o observado; hipótese ou leitura de código, se houver, separadas.
4. **Aberto** — o que não foi coberto, observações sem correção na sessão, residuais novos por ID.
5. **Rastreabilidade** — plano/ões envolvidos; itens do checkpoint; entrada do `CHANGELOG` que
   cita ou registra a sessão — inclusive em seção publicada, **citada, nunca editada**.

**Aprofundamento (matriz por cenário)** quando o ensaio libera remoção ou exclusão, medição durável,
fechamento de frente/etapa ou entrada de release: além do núcleo, tabela com estado inicial,
esperado × obtido linha a linha, `Output`/`OperationId`/GUID, estado final da KB e limpeza manual.
**A matriz é devida nos campos que a sessão produziu**; campo que a sessão não produziu é declarado
«não registrado» — preencher casa vazia é o que §3.7 condena. O precedente `B123` §6 é o **piso
mínimo de formato** — ainda que a própria seção se declare **parcial quanto ao alcance** —: ele
libera exclusão e fecha frente, e não traz `OperationId` porque a seção não os registrou — o que
existe nos itens 155–159 do checkpoint são GUIDs de Folder, contagens e o conserto do `IntentKind`. A profundidade é proporcional ao que a sessão libera; o núcleo não cresce por
burocracia.

### 3.5 Momento

- **Preferido:** redigir na mesma sessão, enquanto Output, GUIDs e `OperationId`s existem.
- **Aceitável:** manter no item do checkpoint o essencial da sessão, no mesmo commit; o documento é
  devido **até o fechamento** da etapa/frente — definido como **o commit que registra o encerramento
  e altera a próxima ação única** —, nunca depois dele. O adiamento só é legítimo se o item já
  contiver cabeçalho, proveniência e alcance (§3.4.1 e §3.4.2); adiar sem isso é o que tornou
  142 (a)(b) não reconstituível.
- Enquanto o documento não existir, o `Validated`/`Fixed` pode citar o item do checkpoint; no
  fechamento, a citação passa a apontar o doc, e o checkpoint fica com o resumo — é a regra «uma
  entrega, uma entrada» do `AGENTS.md` (bloco `[Unreleased]`), não uma invenção desta frente.
- **Citação sem reescrita:** seção de versão publicada é imutável (`AGENTS.md`, bloco
  `[Unreleased]`). Se ela já saiu sem o doc, a ligação se faz por entrada nova em `[Unreleased]` ou
  por remissão datada — nunca reescrevendo a citação da versão publicada.
- **Regra do corte:** antes de publicar, toda entrada que vá citar uma sessão **como prova** exige o
  documento dedicado (ou remissão a um doc existente); entrada já publicada sem doc só comporta
  citação sem reescrita.
- **Entradas do `[Unreleased]` anteriores ao `B124`:** as duas que citam a sessão do `B123` (linhas
  27 e 31) são **anteriores à regra** — a 27 remete ao plano `B123` §6 e a 31 nomeia a sessão sem
  remissão; ambas ficam dispensadas por serem prévias, e a exigência é prospectiva.
- **Sessão avulsa** (sem evento de fechamento): o documento ou a exceção é devido **no mesmo commit**
  do item da sessão — não há fechamento para ancorar o prazo.
- **Exceção única e nomeada:** a primeira aplicação `B124` (§6, itens 142–149) é a única
  retroatividade admitida e **derroga expressamente o «nunca depois do fechamento»** desta seção;
  não vira precedente.
- **Nota do G1:** quando a própria sessão é o fechamento, o adiamento não existe — o prazo colapsa no
  mesmo commit; a folga de §3.5 vale para sessões intermediárias (G2–G5).
- **Proibido:** criar o documento antes do critério e preenchê-lo com resultado presumido.

### 3.6 Nome e local

- Arquivo próprio: `Docs/Implementation/AAAA-MM-DD-<ID ou frente>-<recorte>-ACEITE[-IDE].md` para
  aceite; `…-VALIDACAO-<AMBIENTE>[-<recorte>].md` para bateria/validação — `-VALIDACAO-IDE` na IDE,
  `-VALIDACAO-RUNTIME-…` em runtime HTTP/KB (precedente
  `2026-08-28-B099v-VALIDACAO-RUNTIME-MULTINIVEL.md`). A data do nome é a **da sessão**; a de
  redação, se diferente, entra no cabeçalho.
- Seção em documento existente: mesma estrutura, título próprio no nível **imediatamente abaixo da
  seção hospedeira** (ex.: `### Evidência smoke IDE — AAAA-MM-DD (alcance: DLL X)` dentro de um
  `## Aceite`), citável como `<doc> §N`. É o caso do `B123` §6.

### 3.7 O que não satisfaz o documento dedicado

- parágrafo de progresso em plano (foi o que a Sessão B teve, e não bastou);
- item do checkpoint sozinho, mensagem de commit, descrição de PR, chat da sessão;
- Output bruto ou print sem cabeçalho de proveniência, KB e passos;
- doc que não declara o que a medição não cobre.

### 3.8 Fora de escopo

- Exigir documento dedicado para todo clique ou para gates offline.
- Migração do histórico por padrão: não reabrir o `B082` nem reescrever aceites antigos para o
  molde. A única retroatividade é a dívida nomeada — Sessão B (142–149), primeira aplicação (§6) —,
  e ela não reabre a frente `B082`, encerrada por decisão humana em 2026-09-17.
- Substituir o rito de corte de release ou o checklist de fechamento de spikes: a régua acrescenta
  uma conferência a cada um, não os reescreve.
- Mudar qualquer contrato de runtime da extensão.

## 4. Consumidores (mesmo passo da implementação)

Documentos:

1. `Docs/Foundation/15-TESTES_VALIDACAO_E_QUALIDADE.md` §18 — receber a regra como **fonte
   canônica** (régua, piso, molde proporcional, momento, nomeação), em subseção própria no nível do
   arquivo (ex.: `# 18.2`, como `# 18.1`), no estilo `[QA-F15]`. A subseção declara que o prefixo
   `B124:` da frase fixa é o **marcador estável da convenção**, não do item que se encerra.
2. `AGENTS.md` — subseção curta de acionamento (o agente é quem produz o documento na mesma
   sessão), apontando o documento 15 como contrato e sem reproduzi-lo; **item na seção «Promoção de
   frente e próximo passo»** (não existe «checklist de fechamento de frente» — o item entra lá) e
   **linha na «Revisão pré-push do repositório»** (revisão semântica, passo 5) para o critério de
   §5; e **linha na «Corte de release»** para a conferência do G6. A seção «Revisão semântica de
   contrato runtime» **não** é a âncora: o escopo dela é contrato de runtime, e a régua vale também
   para fechamento documental e medição. O **passo 3 da «Revisão pré-push»** (linha 271) passa a
   listar `warnings` entre os campos lidos, com uma frase no passo 4: aviso de evidência
   (`evidence-doc-required:*`) não impede push, mas exige conferência declarada no relatório
   semântico.
3. `Docs/Foundation/06-BACKLOG_v0.1.md` — item `B124` fechado, com link do plano; nota operacional
   recebe remissão.
4. `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md` — entrada de log do fechamento; promoção da próxima ação
   única (**`B125`**, decidida em 2026-09-21 — os planos de `B124` disponíveis, dois em `Temp/`,
   não a decidiam) **atualizando em conjunto as cinco seções do «Protocolo de atualização»**:
   `Último marco concluído`, `Próxima ação única`, `Evidência da frente encerrada`, `Critério de
   conclusão da revisão pré-push` e `Sequência operacional vigente`; e a conferência da régua
   antes de promover. O «Protocolo de atualização» recebe também uma remissão curta à frase fixa de
   §3.2, que mora no item de log — quem escreve o item consulta o Protocolo, não o documento 15.
5. `Docs/Implementation/2026-09-02-B082-PLANO-HARDENING-E-DESEMPENHO.md` linha 56 — «Evidência
   142–149 — `B124`» passa a apontar o doc retroativo.
6. `CHANGELOG.md` — **proposta revista: entrada curta em `[Unreleased]`**, como no precedente
   `B122`, registrando a regra como mudança de processo (sem mudança de runtime). **Não tocar as
   seções publicadas** — em particular a `0.1.0-alpha.8` (linhas 59–94, incluindo as linhas 62 e
   88). A entrada nova **cita o plano e o doc retroativo** (`…-SESSAO-B-ACEITE-IDE.md`), e a
   rastreabilidade do próprio doc cita a linha 88 — é esse par que fecha o laço, sem editar a seção
   publicada. A alternativa «sem entrada» está descartada por D5; se voltasse, o registro seria **no
   próprio checkpoint/plano `B124`**, não no corte — o corte apenas reflete o que foi publicado.
7. Varredura de `B124` no repositório — **cinco documentos** o citam: 06, checkpoint, plano `B082`
   (linha 56), plano `B108` (linha 6) e plano `B123` (linha 6 e checklist linha 343). Atualizar só
   as frases operacionais; registros datados permanecem.
8. `Docs/Foundation/24-PLANO_IMPLEMENTACAO_REAL_POR_SPRINTS.md` — **não recebe a regra**: registra
   evidência por item, não normatiza. Declarado para não virar gap depois.
9. `scripts/Invoke-PrePushMechanicalChecks.ps1` e
   `Tests/PrePushChecker/Test-OpenApiBuilderPrePushChecks.ps1` — **aviso não bloqueante no canal
   `warnings`** (nunca em `manualRequired`, que bloqueia `pushReadiness` e força exit 3): quando o
   intervalo tocar `## Próxima ação única` do checkpoint, ou a entrada `Validated`/`Fixed` de
   `[Unreleased]` (o G6 cobre os dois), sinalizar a conferência do documento dedicado ou da frase de
   §3.2. **Detecção posicional, não literal:** localizar a seção pela posição da linha alterada no
   conteúdo anterior/novo — o bloco `[Unreleased]` (entre `## [Unreleased]` e o primeiro
   `# [0.1.0-…]`) e, dentro dele, as subseções `### Validated`/`### Fixed` —, porque a edição normal
   acrescenta entrada distante do cabeçalho e o hunk traz o cabeçalho só como contexto; `### Validated`
   é homônimo nas versões publicadas. **Mensagem por seção:** no aviso de `:validated`/`:fixed`, o
   texto é de **candidato a G6** — «se a entrada cita sessão de campo como prova, o documento é
   devido até o fechamento (plano `B124` §3.5); este aviso não bloqueia» —, sem prometer exceção e
   sem exigir o doc neste push; a frase de dispensa de §3.2 aparece **só** no aviso de
   `:checkpoint` (casos fora do piso). **O detector posicional não distingue entrada de campo de entrada offline:** o disparo
   em `Validated`/`Fixed` offline é **falso positivo aceito e documentado**, dispensado na revisão
   semântica — não há caso de teste «negativo offline», porque não seria implementável.
   **Cardinalidade:** um aviso por seção tocada (até dois), com código estável como **prefixo
   textual** da própria string (`evidence-doc-required:checkpoint`, `:validated`, `:fixed`) — o
   canal é `List[string]`, e objeto estruturado quebraria o contrato. Atualizar as duas afirmações
   de escopo «só spike B000–B006» — `AGENTS.md` linha 277 (incluindo a referência obsoleta «hoje,
   `B111`/`S-B111`») e a terceira entrada de `notCovered` —, acrescentar a `notCovered` uma linha de
   simetria («aviso de evidência não comprova fechamento semântico») e **revisar as asserções do
   teste que travam esse texto**: linhas 87–88 (`B00[0-6]`, «lista vazia com próxima ação B007+») e
   linha 342 (`notCovered` com `B000-B006` em cardinalidade 1); a asserção de canal vazio da linha
   325 (`warnings.Count -eq 0`) entra na mesma revisão. Contrato de teste: positivos (só checkpoint,
   só `Validated`, só `Fixed`, ambos); negativos (checkpoint fora das seções, `### Validated` de
   versão publicada, alteração histórica); e prova de que `incompleteReasons`, `exitCode` e
   `pushReadiness` não mudam.

Artefato novo:

10. `Docs/Implementation/2026-09-16-B082-ETAPAS-2-3-SESSAO-B-ACEITE-IDE.md` — primeira aplicação
    (§6). Nome com a data da sessão; redação de 2026-09-21 declarada no cabeçalho.

Código: nenhum na extensão; a única mudança de código é o aviso do checker (consumidor 9).

**Edição dos documentos canônicos:** as substituições literais em lote nos `.md` usam
`scripts/Apply-TextPatch.ps1` com manifesto JSON, conforme o contrato D16/`B122` do `AGENTS.md` —
não edição ad hoc.

## 5. Verificação

- Sem gate mecânico **bloqueante** novo. O gatilho é semântico e não textual; um check por palavra
  («aceite», «smoke») acusaria falso positivo em documentação histórica e daria a impressão de
  cobertura.
- A verificação é dupla: (a) **item na seção «Promoção de frente e próximo passo»** do `AGENTS.md`;
  (b) **linha na «Revisão pré-push do repositório»** (revisão semântica, passo 5): fechamento de
  frente/etapa sem documento dedicado — ou exceção da régua de §3.2 sem a frase de motivo — é
  **gap P1 documental**, mesmo com o checker mecânico verde. A âncora **não** é a «Revisão
  semântica de contrato runtime», cujo escopo é contrato de runtime.
- **Aviso mecânico não bloqueante (D6 revista):** o checker pré-push sinaliza no canal `warnings`
  (nunca em `manualRequired`, que bloqueia) quando o intervalo tocar a `## Próxima ação única` do
  checkpoint ou as subseções `### Validated`/`### Fixed` de `[Unreleased]`; quando a entrada cita
  sessão de campo como prova (G6), o documento é obrigatório **sem exceção** — um `Fixed` puramente
  offline não incide —, e a frase de dispensa de §3.2 cabe **só** no aviso do checkpoint. O P1
  semântico continua por cima. Substitui o «candidato a gate futuro» da versão anterior.

## 6. Primeira aplicação — evidência de 2026-09-16 (itens 142–149)

Não inventar conteúdo: as fontes são os itens 142–149 do checkpoint e os parágrafos «Progresso da
Sessão A/B» do plano `B082`. Estrutura proposta do doc:

**Isto não é migração de histórico.** É a dívida que a própria Sessão B registrou como `B124` e que
a nota do documento 06 manda redigir sob a regra aprovada. O restante do histórico **não** é
migrado, e o `B082` **não** é reaberto — seguiu encerrado por decisão humana em 2026-09-17. O
princípio «não migrar por padrão» vale fora desta dívida nomeada, como reforço de §3.8.

| # | Cenário do doc | Fonte | Observação de proveniência |
|---|---|---|---|
| 1 | Recuperar sob a guarda; diálogos unificados; smoke do diário terminal/Recuperar | item 142 (a)(b)(c) | DLL **não registrada** no item; install 12:46:03 é posterior |
| 2 | Guarda inversa | item 143, caso 1 | DLL **não registrada** — install 12:46:03 antecede o commit da Fatia B (22:43:13) |
| 3 | Abort sem apagar o objeto do clique | item 143, caso 2 | idem; conferência por `genexus_inspect` |
| 4 | Folder fora do contêiner (D10) | item 144 | idem |
| 5 | Preview divergente → zero exclusões | item 145 | idem |
| 6 | Casca × relatório (sucesso/abort/bloqueio) | item 146 | idem |
| 7 | Folder tipado não-vazio: falha, conserto, reteste | itens 147–148 | idem; conserto `IsFolderEmpty` |
| 8 | `DEMO` §21 conferido | item 149 | sem clique novo |

O doc deve declarar, no cabeçalho: «redigido em 2026-09-21 sob a regra `B124`, a partir dos itens
142–149; onde o item não registrou dado, o doc marca "não registrado" em vez de inferir». O laço de
citação fecha **sem tocar seção publicada**: a linha 88 do `CHANGELOG` (entrada `Validated` da
`alpha.8`, seção 85–94) permanece como está — é registro datado —, e o encadeamento se faz (a) pela
entrada nova de `[Unreleased]` (D5), cuja redação mínima **cita este doc**, e (b) pelo campo de
rastreabilidade do próprio
doc, que cita a linha 88 como a entrada que registrou a sessão. **Recorte:** a entrada da linha 88
abrange **142–151**; 150–151 (Etapa 1B) têm doc próprio citado na linha 87, e este doc retroativo
cobre **142–149** — a rastreabilidade declara isso. A linha 56 do plano `B082` é documento de
trabalho e passa a apontar o doc.

Fora do doc: o `B125` (Preview mentiroso) permanece no documento 06 com a nota própria; o doc
retroativo o referencia como achado da sessão, não o revalida.

## 7. Ordem sugerida

1. Decisões D1–D8 fechadas em 2026-09-21 (§9); regra de §3 aprovada.
2. **Promover este plano** — `Temp/2026-09-21-B124-PLANO-REGRA-DOCUMENTO-EVIDENCIA-IDE.md`, o único
   promovido; o outro rascunho (`…-POLITICA-EVIDENCIA-IDE.md`) é descartado — **para
   `Docs/Implementation/` antes de qualquer edição de consumidor**: a rastreabilidade de §3.4.5 não
   pode apontar para arquivo fora do versionamento.
3. Escrever o doc retroativo de 142–149 (§6).
4. Atualizar o documento 15 §18, o `AGENTS.md` («Promoção de frente», «Revisão pré-push» e «Corte de
   release»), o documento 06 (enunciado alinhado a «campo: IDE ou runtime HTTP»), o checkpoint (as
   cinco seções do protocolo, **promovendo somente `B125` como próxima ação única**; o re-smoke do
   `B127` fica aberto e apenas se registra, no log, que ele é candidato a aproveitar a mesma
   instalação, sem incorporar escopo), a entrada nova de `[Unreleased]` no `CHANGELOG` (seções
   publicadas intactas), a linha 56 do plano `B082` e o aviso do checker (consumidor 9).
5. Varredura final de `B124` e das frases de «próxima frente».
6. Commit único com todos os artefatos — a promoção da próxima ação entra **no mesmo commit** que
   registra o encerramento, como manda o protocolo do checkpoint.
7. Pré-push — incluindo `pwsh -NoProfile -File Tests/PrePushChecker/Test-OpenApiBuilderPrePushChecks.ps1`,
   porque o checker muda; sem push.

## 8. Checklist de encerramento

- [ ] Regra publicada no documento 15 §18 (régua, piso, molde proporcional, momento, nomeação).
- [ ] Acionamento no `AGENTS.md`: item em «Promoção de frente», linha na «Revisão pré-push» e linha
      em «Corte de release».
- [ ] Doc retroativo de 142–149 em `Docs/Implementation/`, com proveniência declarada.
- [ ] `CHANGELOG` com entrada nova em `[Unreleased]` e seções publicadas intactas; documento 06,
      checkpoint (as cinco seções do protocolo) e plano `B082` alinhados.
- [ ] Varredura de `B124` no repositório; registros datados intactos.
- [ ] Aviso do checker no canal `warnings` (não `manualRequired`), com os casos de teste e a
      atualização das duas afirmações «só spike B000–B006»; sem bloquear push.
- [ ] Nenhuma mudança de runtime; nenhuma instalação; nenhum gate novo **bloqueante**.
- [ ] Revisão pré-push com a rotina local e relatório semântico.

## 9. Decisões do plano (D1–D8, fechadas em 2026-09-21)

- **D1 — régua de §3.2 e piso de §3.3**, no lugar da condicional N1/N2: padrão é criar; a exceção é
  a frase registrada no checkpoint; seis situações não a admitem. É a resposta ao ponto 1 da nota
  operacional — auditável nos dois sentidos, e sem burocracia para o smoke trivial.
  **Decidida em 2026-09-21: opção A — régua + piso.**
- **D2 — molde proporcional de §3.4**, com núcleo de uma página e matriz exigida para remoção,
  medição durável, fechamento ou citação pelo release; o «alcance da medição» (o que a DLL não
  cobre) é campo do núcleo. É a resposta ao ponto 2.
  **Decidida em 2026-09-21: opção A — núcleo + matriz condicional.**
- **D3 — momento de §3.5**: documento devido até o fechamento, nunca depois da promoção da próxima
  ação. É a resposta ao ponto 3.
  **Decidida em 2026-09-21: opção A — sessão quando possível, prazo no fechamento.**
- **D4 — primeira aplicação retroativa de 142–149**, sob a regra aprovada. É a resposta ao ponto 4.
  **Decidida em 2026-09-21: opção A — evidência completa, redigida na implementação do `B124`,
  datada de 2026-09-16, com «não registrado» onde a fonte não tem o dado; não reabre o `B082`.**
- **D5 — `CHANGELOG`: entrada curta em `[Unreleased]`** (precedente `B122`), registrando a regra
  como mudança de processo.
  **Decidida em 2026-09-21: opção A — entrada curta em `[Unreleased]`; seções publicadas intactas.**
  **Fechada na v4:** seção **`Added`** (a frente passa a ter componente mecânico — o aviso do
  checker), com a redação mínima: «`B124` — regra do documento de evidência de campo: sessão citada
  como prova por entrada de release exige documento dedicado em `Docs/Implementation/`; fora dos
  gatilhos de piso, a dispensa é exceção registrada; aviso não bloqueante no checker. Não altera a
  extensão. Primeira aplicação:
  `Docs/Implementation/2026-09-16-B082-ETAPAS-2-3-SESSAO-B-ACEITE-IDE.md`; plano:
  `Docs/Implementation/2026-09-21-B124-PLANO-REGRA-DOCUMENTO-EVIDENCIA-IDE.md`.» — **a exceção da
  frase fixa não vale para o G6** (piso), e a citação do doc retroativo é o que fecha o laço com a
  linha 88 publicada. Se o aviso do checker for cortado, a seção volta a exigir decisão (`Changed`).
- **D6 — sem gate mecânico** nesta versão; verificação por checklist e pré-push semântico.
  **Decidida em 2026-09-21: opção A — julgamento com P1 ancorado na revisão semântica; check
  automático só como ideia futura.**
  **Revista na v3 (parecer do Claude Opus 5):** entra um **aviso mecânico não bloqueante** no checker,
  no canal `warnings` (nunca `manualRequired`); o P1 semântico permanece, e o «sem gate mecânico»
  vale só para gate **bloqueante**.
- **D7 — documento dedicado pode ser seção de documento da frente** (precedente `B123` §6), desde
  que com título próprio e núcleo completo; a alternativa de exigir arquivo próprio sempre foi
  descartada.
  **Decidida em 2026-09-21: opção A — arquivo próprio ou seção delimitada com o núcleo completo.**
- **D8 — próxima ação única = `B125`** (Preview pós-aborto), por recomendação do parecer externo.
  **Decidida em 2026-09-21: opção A — `B125`.** Decisão humana desta sessão; não há registro no
  repositório até o commit de fechamento. O parecer do Claude Opus 5, na rodada de pares de
  2026-09-21, recomendou `B127` (hardening no caminho de exclusão, sem evidência IDE) e fica
  registrado como dissent. **Superado na v4:** o `B127` **não** entra na ação única — promove-se
  somente `B125`, e o `B127` fica aberto, com o ride-along apenas no log (§7, passo 4). Nota do
  Opus: se o `B125` não reproduzir rapidamente, promover o `B127` em seguida, sem esperar uma
  terceira frente. A promoção de `B125` no checkpoint só ocorre com a decisão humana registrada no
  próprio commit de fechamento.

## 10. O que este plano não decide sozinho

- A redação exata das frases no documento 15 e no `AGENTS.md` (feita na implementação, sob
  aprovação).
- O conteúdo do doc retroativo, que é reconstrução fiel dos itens 142–149 — inclusive do que ficou
  «não registrado».
- A promoção de `B127` a frente depois desta regra — a de `B125` está decidida (D8); ambos
  continuam residuais independentes citados aqui como casos-limite e como insumo da régua.

## 11. Pareceres externos recebidos em 2026-09-21 e disposição

| Ponto do parecer | Disposição neste plano |
|---|---|
| Preferir a régua «dúvida → cria; exceção justificada no checkpoint» à condicional N1/N2 | Adotada em §3.2; os rótulos N1/N2 saem, ficam «registro mínimo» e «documento dedicado» |
| Molde fixo vira burocracia; profundidade proporcional | Adotada em §3.4: núcleo de uma página + matriz quando libera remoção, medição, fechamento ou citação |
| «Não reabrir o `B082` nem migrar histórico por padrão» sem cancelar a Primeira aplicação | Reforçado em §3.8 e §6: princípio mantido, dívida 142–149 preservada |
| P1 de §5 solto, sem âncora no `AGENTS.md` | Consumidor 2 de §4 e §5 passam a exigir a alteração da seção «Revisão semântica de contrato runtime» — **superado na v2**: a âncora correta é «Promoção de frente» + «Revisão pré-push» (segunda rodada, abaixo) |
| Próxima ação única em aberto; recomendação `B125` | Registrada em §4 (consumidor 4), §7 e §9 (D8) |
| D5: precedente `B122` pende para entrada curta | D5 revista: proposta passa a ser entrada curta em `[Unreleased]`; «sem entrada» exige registro no corte |
| §6 mandava o `Validated` da `alpha.8` (linha 88) citar o doc retroativo — seção publicada e imutável | Corrigido: linhas 62/88 intocadas; ligação pela entrada nova de `[Unreleased]` (D5) e pela rastreabilidade do doc retroativo; regra geral em §3.5 |

### Segunda rodada — painel de pares de 2026-09-21 (Codex e Claude Opus)

| Ponto do parecer | Disposição (v2) |
|---|---|
| Codex: a âncora na seção de runtime não cobre fechamento documental e medição; «checklist de fechamento de frente» não existe | Corrigido: consumidor 2 e §5 apontam «Promoção de frente» e «Revisão pré-push»; a seção de runtime sai da âncora (§2.1 corrigido) |
| Codex/Opus: G6 com buraco temporal — a citação só se decide no corte | Corrigido: G6 restrito a citação **como prova** em `[Unreleased]` ou release não publicado; «Regra do corte» em §3.5 e linha em «Corte de release» |
| Opus: G6 engole a régua | Assumido em §3.3 («Alcance real do G6»): quem sai em release e é citado como prova cai nele mesmo com um cenário |
| Codex: a retroatividade precisa ser exceção formal em §3.5 | Corrigido: «Exceção única e nomeada» em §3.5 |
| Opus: o consumidor 4 ignora as cinco seções do «Protocolo de atualização» | Corrigido: consumidor 4 lista as cinco seções |
| Codex/Opus: contagens erradas — documentos do `B124`, casos da Sessão B e planos | Corrigidas em §1, §4.4 e §4.7 (cinco documentos; oito cenários; dois planos no disco) |
| Opus: `687a494` afirmado em §2.2 e «a confirmar» em §6 | Corrigido: §2.2 passa a «candidato»; o doc retroativo declara «não registrado» onde não há prova |
| Opus: prazo sem evento de fechamento | Corrigido: «Sessão avulsa» em §3.5 — devido no mesmo commit do item |
| Codex: D5 sem dono operacional | Corrigido: a alternativa «sem entrada» seria registrada no checkpoint/plano, não no corte |
| Codex: ordem do §7 ambígua | Corrigido: a promoção da próxima ação é o último passo, após commit e pré-push |
| Opus: heading `18.2` fora do padrão do arquivo | Corrigido: `# 18.2`, no nível de `# 18.1` |
| Opus: a promoção do plano deve preceder edição de consumidor | Corrigido: §7 passo 2 explícito |
| Opus: D8 deveria ser `B127` | Dissent registrado em D8; decisão humana mantida, com mitigação de mesma sessão/instalação para o `B127` |

### Terceira rodada — segunda passagem do painel (Codex e Claude Opus), disposição na v3

| Ponto do parecer | Disposição (v3) |
|---|---|
| Codex: §7 contraditório — promoção antes do commit vs depois | Corrigido: a promoção entra no **mesmo commit** do encerramento (§7, passos 4 e 6); o passo posterior foi removido |
| Codex: D8 «decidida» sem registro verificável no repositório | Corrigido: D8 declara a proveniência (decisão humana da sessão; registro entra no commit de fechamento) |
| Codex: «hoje só remissão» conflita com o `Validated` da `alpha.8` | Corrigido: G6 descreve o caso `alpha.8` como histórico preexistente, mantido intacto; a exigência é prospectiva |
| Codex: «fechamento» ainda interpretável | Corrigido: definido como **o commit que registra o encerramento e altera a próxima ação única** |
| Codex: dispensa auditável só pelo motivo livre | Corrigido: a frase passa a exigir sessão identificada e gatilhos G1–G6 declarados como não aplicáveis |
| Opus P1-1: o exemplar `B123` §6 reprova na matriz obrigatória | Corrigido: matriz devida **nos campos que a sessão produziu**, com «não registrado» no resto; `B123` §6 declarado piso mínimo aceito |
| Opus P1-2: G6 sem teste operacional | Corrigido: G6 reformulado para «gera entrada `Validated`/`Fixed` em `[Unreleased]`», conferível no diff |
| Opus P1-3: a verificação depende de alguém lembrar | Corrigido: **aviso não bloqueante** no checker (`manualRequired`/`notCovered`), consumidor 9; D6 revista |
| Opus P2-4: adiamento reabre o buraco em frente longa | Corrigido: adiar só é legítimo com cabeçalho, proveniência e alcance já no item (§3.5) |
| Opus P2-5: escopo HTTP ampliado sem declaração | Corrigido: ampliação declarada em §3.1 e realinhamento do enunciado do 06 no fechamento |
| Opus P2-6: documento 24 sem posição | Declarado **não consumidor** (consumidor 8) |
| Opus P2-7: `FAIL` não tratado | Corrigido: nota «FAIL também é evidência» em §3.3 |
| Opus P2-8: mitigação de D8 sem dono | Corrigido: pareamento entra na «Próxima ação única» (§7, passo 4) — **superado na v4**: o `B127` sai da ação única e fica só no log |
| Opus P3-9: §2.1 descreve o futuro como presente | Corrigido: «receberá (proposto)» |
| Opus P3-10: nível do heading do exemplo | Corrigido: nível imediatamente abaixo da seção hospedeira |
| Opus P3-11: a frase fixa envelhece após o `B124` | Mantida com o prefixo `B124:` — o repositório usa prefixo por ID em outros lugares; observação registrada, sem mudança |

### Quarta rodada — terceira passagem do painel (Codex e Claude Opus), disposição na v4

| Ponto do parecer | Disposição (v4) |
|---|---|
| Codex P1 / Opus P1 (convergentes): o aviso «não bloqueante» em `manualRequired` **bloqueia** — `incompleteReasons` → `pushReadiness=blocked`, exit 3 | Corrigido: o aviso vai no canal **`warnings`** (existente, sem efeito em prontidão); consumidor 9 e §5/D6 atualizados |
| Opus: faltam as duas afirmações «só spike B000–B006» na lista de consumidores | Corrigido: `AGENTS.md` linha 277 e a terceira entrada de `notCovered` entram no consumidor 9 |
| Opus: o passo de rodar `Tests/PrePushChecker/...` não está explícito | Corrigido: §7, passo 7, e §8 |
| Codex P1: D8 incorporava o `B127` à ação única, contra o escopo do item | Corrigido: promove-se **somente `B125`**; o `B127` fica aberto, com o ride-along apenas no log (§7, passo 4) |
| Codex P1: evidência local do painel incompleta | **Falso positivo de leitura no meio do despacho:** no instante da leitura, o `panel-summary` e o veredito do Codex ainda não estavam gravados; ao fim do despacho, os dois vereditos e o resumo existem no ledger v3 |
| Codex P2: G6 deve exigir que a entrada cite **sessão de campo** como prova | Corrigido: G6 restrito; `Fixed` puramente offline não incide |
| Codex P2: a exceção retroativa deve derrogar expressamente o «nunca depois do fechamento» | Corrigido: «derroga expressamente» em §3.5 |
| Codex P2: fechar D5 (seção e redação) | Corrigido: D5 fechada — seção `Added`, redação mínima registrada |
| Codex P2: contrato de teste do aviso | Corrigido: positivos (só checkpoint, só `Validated`, ambos) e negativo (histórico) no consumidor 9 |
| Opus P2: para G1 o adiamento colapsa no mesmo commit | Corrigido: «Nota do G1» em §3.5 |
| Opus P2: a entrada da linha 88 cobre 142–151; o doc cobre 142–149 | Corrigido: recorte declarado em §6 |
| Opus P2: declarar qual rascunho de `Temp/` é promovido | Corrigido: §7, passo 2, nomeia o promovido e descarta o outro |
| Opus P3: nota do 06 vai até 381; «P8 (partes 1–4)» e `B102` não se sustentam; qualificar «(parcial)» do `B123` §6 | Corrigidos: §0 (347–381), §1 (série P0–P7 + ressalva do `B102`) e §3.4 («parcial quanto ao alcance») |

### Quinta rodada — quarta passagem do painel (Codex e Claude Opus), disposição na v5

| Ponto do parecer | Disposição (v5) |
|---|---|
| Codex P1: G6 contradiz §3.5 (piso «sem exceção» vs janela de citação do checkpoint) | Corrigido: «Piso não é prazo» em §3.3 — o piso elimina a dispensa, o prazo de §3.5 vale inclusive para o G6 |
| Codex P1: resíduo de D8 ainda descrevia a mitigação superada | Corrigido: D8 declara a v4 — `B127` fora da ação única, ride-along só no log |
| Codex P2: contrato do aviso sem cardinalidade nem mensagem | Corrigido: um aviso por seção (códigos `evidence-doc-required:checkpoint` / `:validated`), «ambos» = dois avisos |
| Codex P2: registros das rodadas sem caminho verificável | Corrigido: nota de proveniência — `Temp\revisao-por-pares\20260921-b124\`, prova local não versionada |
| Opus R1: o exemplar `B123` §6 reprova no núcleo (sem instalação/GeneXus) | Corrigido: núcleo aceita commit **ou** evidência de instalação; campo não produzido vira «não registrado» |
| Opus R2: detecção do aviso sem mecanismo — risco de disparar sempre | Corrigido: detecção por **conteúdo do patch**, com negativo específico (checkpoint fora das duas seções) |
| Opus R3: justificativa de §3.4.2 invertia o recorte do `AGENTS.md` | Corrigido: «o alcance — não o registro — expira em silêncio» |
| Opus R4: piso × prazo podiam ser lidos como conflito | Resolvido junto com o P1 do Codex (§3.3) |
| Opus R5: §0 ainda citava 347–379 | Corrigido: 347–381 |
| Opus R6: a mitigação de D8 é a mais frágil disponível | Registrado em D8: se o `B125` não reproduzir rapidamente, promover o `B127` em seguida |
| Opus R7: §5 abria com «sem gate mecânico» | Corrigido: «sem gate mecânico **bloqueante**» |

### Sexta rodada — quinta passagem do painel (Codex e Claude Opus), disposição na v6

| Ponto do parecer | Disposição (v6) |
|---|---|
| Codex P1: G6 cobre `Fixed`, mas o detector e os testes só veem `Validated` | Corrigido: detector cobre `Validated` e `Fixed`; códigos `:validated`/`:fixed`; positivos de teste com `Fixed` |
| Codex P1 / Opus P2-C: detecção por hunk não alcança entrada distante; `### Validated` é homônimo entre `[Unreleased]` e versões publicadas | Corrigido: detecção **posicional** — linha alterada dentro do bloco `[Unreleased]` e de suas subseções —, com negativo do `Validated` publicado |
| Codex P1: a cronologia desmente `687a494` como origem da DLL instalada (12:46:03 × 22:43:13) | Corrigido: §2.2 e §6 passam a marcar a build como **não registrada**; o candidato foi retirado |
| Codex P2: D8 não é verificável nas fontes versionadas | Corrigido: a promoção de `B125` só ocorre com a decisão humana registrada no commit de fechamento |
| Opus P1-A: `warnings` fora da lista de campos lidos da «Revisão pré-push» (passo 3) | Corrigido: consumidor 2 acrescenta `warnings` aos passos 3–4, com a frase de conferência obrigatória |
| Opus P1-B: a asserção de canal vazio do teste (linha 325) pode quebrar com o aviso | Corrigido: reavaliação explícita no consumidor 9 |
| Opus P2-D: o canal é `List[string]` | Corrigido: código estável como prefixo textual da string |
| Opus P3-E: a frase fixa não tem casa no «Protocolo de atualização» | Corrigido: remissão no Protocolo (consumidor 4) |
| Opus P3-F: falta simetria em `notCovered` | Corrigido: linha «aviso de evidência não comprova fechamento semântico» no consumidor 9 |
| Opus: citar a regra «uma entrega, uma entrada» do `AGENTS.md` em §3.5 | Corrigido: §3.5 remete ao bloco `[Unreleased]` |

### Sétima rodada — sexta passagem do painel (Codex respondeu; Claude Opus em quota), disposição na v7

| Ponto do parecer | Disposição (v7) |
|---|---|
| Codex P1: a redação operacional reabria a exceção no G6 (D5 e mensagem do checker diziam «doc **ou** exceção») | Corrigido: no `:validated`/`:fixed` o documento é obrigatório **sem exceção**; a frase de dispensa só aparece no aviso de `:checkpoint`; D5 reflete a distinção |
| Codex P1: a rodada ainda não tinha prova local concluída no instante da leitura | Parcialmente procedente: o Codex leu durante o despacho; a rodada, porém, **ficou incompleta de fato** — o Claude Code entrou em `quota` (`reason=quota-signal`) e não emitiu veredito |
| Codex P2: o detector posicional não sabe se a entrada cita sessão de campo | Corrigido: o aviso é «candidato a G6», com negativos para `Validated` offline e para `Fixed`/`Validated` publicados — **superado na v9**: não há negativo offline; o disparo é falso positivo aceito |
| Codex P2: a próxima ação efetiva continua `B124` até o commit | Mantido: o checkpoint proposto preserva a ressalva; a promoção de `B125` exige a decisão registrada no commit |

### Oitava rodada — segunda opinião solo (Codex GPT-5.6-terra), disposição na v8

| Ponto do parecer | Disposição (v8) |
|---|---|
| P1: §6 prometia que a entrada de `[Unreleased]` cita o doc retroativo, mas D5 e §4.6 não garantiam a citação | Corrigido: D5, §4.6 e §6 passam a exigir, na entrada `Added`, a citação do plano e do doc retroativo |
| D8 ainda não é verificável nas fontes versionadas | Mantido: a promoção de `B125` só ocorre com a decisão humana registrada no commit de fechamento |
| O aviso do checker é candidato, não prova de incidência do G6 | Mantido: os negativos de teste (offline, publicado) são indispensáveis e estão no consumidor 9 — **superado na v9**: o offline é falso positivo aceito; os negativos são o publicado e o checkpoint fora das seções |
| Claude Code em limite mensal de uso | Registrado: a segunda opinião foi pedida ao Codex; o Claude fica fora até o ciclo virar |
| Codex em limite de uso na releitura de confirmação da v8 | Registrado: a confirmação fica pendente de nova janela (retry às 23:31); a correção do P1 está aplicada e datada |

### Nona rodada — segunda opinião solo (subagente nativo `reviewer-ro`), disposição na v9

| Ponto do parecer | Disposição (v9) |
|---|---|
| P1: «negativo offline» incompatível com a detecção posicional; duas listas de teste no mesmo item | Corrigido: o disparo offline é **falso positivo aceito e documentado**; contrato de teste unificado |
| P2-1: o texto promovido diferia do texto revisado | Corrigido: a **v9 é o texto lido e o promovido**; a linha de proveniência da quota fica declarada |
| P2-2: a mensagem do aviso podia ser lida como «exija o doc neste push» | Corrigido: a mensagem remete ao prazo de §3.5 e mantém o caráter de candidato |
| P2-3: asserções do teste que travam o texto de `notCovered`/`AGENTS.md` | Corrigido: linhas 87–88 e 342 entram na revisão do consumidor 9 |
| P2-4: entradas do `[Unreleased]` do `B123` e a regra do corte | Corrigido: declaradas satisfeitas pela remissão ao plano `B123` §6; a regra é prospectiva |
| P3-1: referência obsoleta «hoje, `B111`/`S-B111`» no `AGENTS.md` | Corrigido: entra na edição do consumidor 9 |
| P3-2: token `VALIDACAO-IDE` para escopo que inclui runtime HTTP/KB | Corrigido: `-VALIDACAO-<AMBIENTE>`, com o precedente `VALIDACAO-RUNTIME-MULTINIVEL` |
| P3-3: justificativa do `OperationId` apontava itens que não o registram | Corrigido: «a seção não os registrou» |
| P3-4: linhas do `.gitignore` (88–89, não 86–89) | Corrigido nas duas citações |
| P3-5: mecanismo de edição textual não declarado | Corrigido: `Apply-TextPatch.ps1` declarado em §4 |
| P3-6: durabilidade do prefixo `B124:` | Corrigido: o documento 15 declara o prefixo como marcador da convenção |

### Décima rodada — confirmação do subagente nativo, disposição na v10

| Ponto do parecer | Disposição (v10) |
|---|---|
| P1: o §5 reabria a exceção no `Validated`/`Fixed` e omitia `### Fixed` | Corrigido: §5 alinhado ao §4.9 — documento obrigatório sem exceção; dispensa só no aviso do checkpoint |
| P3: §11 com afirmações superadas («negativo offline») | Corrigido: as duas linhas foram marcadas «superado na v9» |
| P3: «as duas têm remissão» impreciso para a linha 31 do `CHANGELOG` | Corrigido: a linha 27 remete ao plano; a 31 é anterior à regra |
| P3: mensagem do checker com «(§3.5)» fora de contexto | Corrigido: «(plano `B124` §3.5)» |

### Décima primeira rodada — confirmação final do subagente nativo, disposição na v11

| Ponto do parecer | Disposição (v11) |
|---|---|
| P1 e três P3 da v9 confirmados fechados | Registrado |
| P3-a: o §5 resumia «sem exceção» sem a condição do G6 (sessão de campo) | Corrigido: «quando a entrada cita sessão de campo como prova (G6)… um `Fixed` puramente offline não incide» |
| P3-b: a frase do passo 4 genérica para todo o canal `warnings` | Corrigido: qualificada ao prefixo `evidence-doc-required:*` |
| Redação de §3.5 (oração inicial contraditória) | Limpa: as duas entradas do `B123` são anteriores à regra |
| **Veredito final** | **Plano pronto para execução; não resta gap bloqueante** (nova rodada dispensada) |
