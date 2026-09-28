# B129 — Plano: rever a D45 (cobertura da DLL satélite U13 no pré-push)

**Criado:** 2026-09-27 · **Versão:** v8 (final), 2026-09-28
**Histórico de versões:** v1 (2026-09-28 manhã: decisões D-B129-1 a 9 e Fase 0 medida) →
revisão por pares, rodada `B129-20260928-v1` (quatro vereditos «revisa») → v2 (triagem humana
dos 15 pontos da rodada, seção 2.2) → rodada `B129-20260928-v2` (quatro vereditos «revisa»; a
arquitetura não foi reaberta) → v3 (correções de precisão de especificação, seção 2.3,
autoradas pelo agente por delegação do mantenedor) → rodada `B129-20260928-v3`, painel reduzido
por decisão humana aos revisores do opencode → v4 (medições novas e revisão própria do autor,
seção 2.4) → rodada `B129-20260928-v4`, mesmo painel reduzido (dois «revisa», nenhum P1) → v5
(seção 2.5) → segunda opinião sobre a v5, por pedido do mantenedor: subagente nativo Opus 5.5 e
Space Bunny Alpha (Command Code, via opencode), que substituiu o Big Pickle na lista preferida —
um único criador contável, então **não** é revisão por pares → v6 (seção 2.6) → parecer solo do
GPT 6 Sol (Codex), por pedido do mantenedor → v7 (seções 2.7 e 2.8) → parecer solo de subagente
nativo Opus 5.5 → **v8** (seções 2.9 e 2.10), versão final.
**Item:** `B129` (`Docs/Foundation/06-BACKLOG_v0.1.md`, linha da tabela e «Nota operacional — B129»)
**Estado deste documento:** **versão final, promovida e executada.** Design congelado por decisão
do mantenedor em 2026-09-28 («é a última versão»); a prova passou para a implementação e o
meta-teste. Isso **não** é convergência de painel: a v8 não foi revisada por consulta alguma.
Promovido de `Temp/2026-09-27-B129-PLANO-REVISAO-D45.md` para este arquivo em 2026-09-28, como
passo 0 da execução (seção 6), autorizada pelo mantenedor na mesma data. O corpo (seções 0 a 10
e 12) é o texto da v8, com duas redações trocadas só para não formar citação móvel de linha C#
(regra B128); a seção 11 é a **evidência** da execução (D-B129-7) e registra os desvios de
implementação em relação ao texto.

**Guia para quem executa.** Leia a seção 0 e depois o **corpo**, seções 3 a 12 — ele é normativo
e já incorpora todas as decisões. A seção 2 é registro da triagem das consultas, na ordem em que
aconteceram: serve para entender o porquê de uma regra, não como regra (ver a nota no início da
seção 2). Os pareceres e manuscritos de cada consulta estão em `Temp/revisao-por-pares/B129-*`.

---

## 0. Resumo

A D45 deixa a DLL satélite U13 fora do pré-push mecânico. O `B129` a emenda com três checks no
orquestrador, sem mudar a forma canônica de invocação e sem trazer DLL proprietária ao
repositório:

1. **`msbuild.compileSetParity`** — paridade dos itens `Compile` **avaliados** pelos dois
   `.csproj`, com regras direcionais para `Line.*` e detecção de duplicata. Não precisa das DLLs
   U13; depende do SDK canônico, como os checks `dotnet.*` já dependem.
2. **`dotnet.buildSatellite`** — compila `Src/GenexusOpenApiBuilder.Gx18u13.sln` em Release, no
   caminho padrão, quando `Src/Lib/Gx18u13` existe. Pasta ausente = `skipped` fora de `warnings[]`
   (D45 preservada); pasta presente com referência pinada faltando = `environmentBlocked`. O
   contrato do `.props` de referências (D34) é validado sempre, em qualquer clone.
3. **`source.packageNoIfDirective`** — cumpre a guarda da D15 (proibido `#if` em `Package.cs`),
   prometida no plano da Opção B e nunca implementada.

E corrige a deriva que o check 1 pega: `Compile.Shared.props` não exclui `Temp\**`, que o canônico
exclui desde `f2967cc`; de passagem, passa a excluir `Line.*\**`, evitando duplicata futura.

---

## 1. Contexto

- **Origem:** no pré-push do `B109` ramo C (`c4202c6`), o orquestrador compilou só a solution
  canônica; a satélite foi compilada à mão, depois.
- **Decisão emendada:** D45 e invariante 13 de
  `Docs/Decisions/2026-08-12-PLANO_SUPORTE_PARALELO_GX18U13_OPCAO_B.md`, a linha do §2 («Não
  colocar build do satélite no checker pré-push público») e o risco do §10.
- **Motivo original da D45:** as refs U13 ficam em `Src/Lib/Gx18u13`, gitignored (D44); um clone
  sem elas não compila o satélite, e o meta-teste exige `warnings` vazio e proíbe o checker de
  tocar `Tools/`, Program Files ou DLL.
- **Relacionados:** `B117` (localização da saída satélite), `B118` (build satélite dentro do
  Codex), D15 (guarda de `#if`), D31 (build do asset a partir da tag no corte), D36/D46 (glob
  compartilhado e `Line.*`).

---

## 2. Decisões

**Precedência de leitura.** As subseções 2.2 a 2.8 são **registro** da triagem de cada rodada, na
ordem em que aconteceu; linhas posteriores refinam ou substituem anteriores. O que vale para a
implementação é o corpo (seções 3 a 12), que já incorpora todas as decisões. Em caso de conflito
entre uma linha de triagem e o corpo, **o corpo prevalece**.

### 2.1 Decisões de desenho (v1)

| ID | Decisão | Alternativa descartada e porquê |
|---|---|---|
| D-B129-1 | Build condicional do satélite **+** check de paridade; uma linha no `AGENTS.md` manda o relatório pré-push declarar o status do satélite | Só checklist (hábito escrito); script separado de pré-corte (o corte já compila o satélite); manifesto versionado da superfície das DLLs U13 (publicaria metadado de binário proprietário) |
| D-B129-2 | Pasta `Src/Lib/Gx18u13` **presente com referência pinada faltando** → `environmentBlocked`, bloqueia push. **Ausente** → `skipped`, fora de `warnings[]`. Justificativa: é defeito da máquina, não do commit — o commit não está errado, a máquina é que não consegue verificá-lo; por isso não `failed`. Repetir **não** resolve: a mensagem lista os arquivos faltantes e aponta a D44 (repopular a partir da instalação U13) | `skipped` + aviso: o mantenedor preferiu não aceitar push sem cobertura para os usuários até U13 |
| D-B129-3 | Build no caminho padrão (`artifacts/gx18u13/`), com o target `PrepareGx18u13Asset` rodando, simétrico ao canônico | Diretório temporário: a proteção do asset que o justificaria é a mesma que o canônico já não tem; a proteção real de ambos é o rito do corte (build a partir da tag + SHA-256 antes do upload) |
| D-B129-4 | Com restore ou build canônico falho, satélite `skipped`; matriz completa na seção 4.5 | Rodar sempre: segunda lista de erros quase sempre é ruído |
| D-B129-5 | Grupos `MSB3277` de `mscorlib` → `knownWarnings`, fora de `warnings[]`; outros avisos do satélite → `warnings[]` com prefixo `satellite-build:`; linha de resumo fora dos canais. Regra detalhada na seção 4.2 | — |
| D-B129-6 | Corrigir `Temp\**` no `Compile.Shared.props` nesta frente | — |
| D-B129-7 | Evidência = este plano promovido a `Docs/Implementation/`, com o corpo alinhado às decisões e a seção 11 como evidência. A **exigência** de documento vem do `AGENTS.md` (fechamento de frente, B124); o documento 15 §18.2 fornece o **formato** aceito («seção delimitada e intitulada como evidência no documento da frente»), mas trata de evidência de campo e exclui gate offline | Documento separado (divide raciocínio e prova); dispensa `B124:` (perderia a razão da emenda) |
| D-B129-8 | A guarda da D15 entra no `B129`; não se cria item próprio | Item próprio: mesma família de proteção de paridade, mesmos arquivos, custo pequeno |
| D-B129-9 | Controle positivo com sentinela em `Src/Extension/Temp/` autorizado | — |

### 2.2 Triagem da rodada `B129-20260928-v1` (decisões humanas de 2026-09-28)

| # | Ponto (quem apontou) | Decisão |
|---|---|---|
| T1 | Paridade só **tolerava** `Line.*`; hoje o canônico compilaria `Line.Gx18u13` e passaria verde (GPT, DeepSeek, Opus) | Regras **direcionais obrigatórias** + detecção de duplicata (seção 4.1). Alinhar `Compile.Shared.props` ao esqueleto D36 fica para quando `Line.*` nascer, com remissão na D55 |
| T2 | Caso negativo da paridade inexequível na fixture; multi-alvo não exercitado (DeepSeek, Opus) | Canônico da fixture com itens default, `<TargetFrameworks>` plural e `Compile Remove` de pasta-sentinela `Only.Sat/` (seção 4.7) |
| T3 | Agrupamento do meta-teste fundia exits incompatíveis (Muse) | Matriz execução × asserção sem fusão; custo medido, sem promessa de corte (seção 4.7) |
| T4 | Filtro `MSB3277` verdadeiro por vacuidade, dependente de idioma, sem teste negativo (Opus, Muse, GPT) | Idioma forçado em inglês, ≥1 cabeçalho reconhecido, filtro só no satélite, casos negativos (seção 4.2) |
| T5 | `satelliteRefs`: contrato e alcance (DeepSeek, Opus, GPT) | D55 declara a **substituição** de `absent\|present`; `complete` = refs pinadas presentes, **não** identidade U13 (seções 4.6 e 5). A parte «substituição» foi **superada pela R12**: o campo nasce no `B129` |
| T6 | Leitura do `.props`: namespace, referências sem `HintPath`, `.props` ausente (Opus, DeepSeek) | Seção 4.2 |
| T7 | Classificação de falhas (DeepSeek, Muse, Opus) | Opção mínima: `MSB4019`/`MSB4236` → `sdkUnavailable`; `MSB3021`/`MSB3027` com mensagem de arquivo em uso → `fileLocked`; acesso negado continua `failed`. Só nos checks novos (seção 4.4). Refinada na v3 pela R6 (`MSB4019`) e pela R19 (acesso negado passa a `environmentBlocked`) |
| T8 | Matriz de dependência entre os checks (Muse, Opus) | Seção 4.5 |
| T9 | Semântica de `environmentBlocked` para refs incompletas (Muse) | Justificativa e ação corretiva escritas na D-B129-2 e na D55 |
| T10 | §3.5 e limites da paridade afirmavam além do medido (GPT, Muse) | §3.5 reescrita como hipótese; limites em `notCovered` (seções 3.5 e 4.6) |
| T11 | «A DLL canônica é build local» era inferência (DeepSeek) | **Confirmado pelo mantenedor em 2026-09-28**: a DLL canônica do release é compilada na máquina dele, do código da tag |
| T12 | `CHANGELOG` com uma só entrada `Added` (Muse) | `Added` (checks) + `Fixed` (`Temp\**`), visibilidade conferida contra as tags (seção 5) |
| T13 | Menores (vários) | Aplicados: `B132` não é citado (o número já existe no backlog); remissões extras da Opção B; sem referência numérica a linha de `.cs` nas remissões (regra B128); base da D-B129-7; ressalva da estimativa de custo; sentinela e `incomplete`; redação da ordem; `.gitignore` da fixture; critério de aceite sem contagem fixa; restore possivelmente não offline |
| T14 | Big Pickle falhou em `exit 1` sem stderr; log do opencode sem causa | Excluído pelo resto da sessão por decisão humana (`skippedByHumanDecision`); lista preferida inalterada |
| T15 | Marcador local para tornar `absent` bloqueante na máquina do mantenedor (Opus) | Não fazer: estado invisível; `satelliteRefs=absent` no JSON e a linha do `AGENTS.md` bastam |

### 2.3 Correções da rodada `B129-20260928-v2` (autoria da v3)

Nenhum revisor reabriu a arquitetura. As correções abaixo são de precisão de especificação;
o mantenedor delegou a autoria da v3 ao agente em 2026-09-28, reservando para si as questões que
exigem decisão (marcadas **[decisão humana]**).

| # | Ponto (quem apontou) | Correção na v3 |
|---|---|---|
| R1 | Filtro `MSB3277` agrupava todas as linhas num só grupo, contradizendo o caso de teste com dois assemblies (Opus, GPT, DeepSeek — P1) | Agrupamento **por diagnóstico**: cada cabeçalho abre um grupo; linhas `MSB3277` antes de qualquer cabeçalho reconhecido formam grupo órfão → `warnings[]` (seção 4.2) |
| R2 | Idioma forçado pode não valer com nó MSBuild reutilizado; cabeçalho só em inglês (DeepSeek, Muse, Opus) | `-nodeReuse:false` no build satélite; cabeçalho reconhecido em inglês **e** português; `evidence` registra o idioma observado (seção 4.2). O `-clp:NoSummary` da v3 foi retirado na v4 (V1) |
| R3 | Fixture subespecificada: `.sln` satélite, caminhos reais, isolamento de `obj/`, `Include`s sobrepostos, restauração de estado (Opus, Muse, DeepSeek) | Seção 4.7 reescrita com a estrutura completa da fixture e a sequência das execuções |
| R4 | Regras 1, 2 e 4 da paridade sem caso negativo (Opus, Muse) | Casos de função no módulo novo (R5) |
| R5 | «Casos de função» sem mecanismo: o checker é monolítico e o meta-teste copiava funções (Opus) | Funções puras extraídas para `scripts/B129-SatelliteChecks.ps1`, carregado por dot-source pelo checker e pelo teste, no padrão do `B128` |
| R6 | `MSB4019` também dispara com import ausente **do repositório** (Opus) | `MSB4019` só é `sdkUnavailable` quando o caminho ausente está fora da raiz do repositório; dentro dela, `failed` (`repositoryImportMissing`) |
| R7 | `.props` só validado com a pasta presente; `HintPath` fora do contrato D34 contaria como `complete` (GPT, Opus, Muse) | O `.props` é validado **sempre**; `HintPath` absoluto ou que resolva fora de `Src/Lib/Gx18u13` → `failed` (seção 4.2) |
| R8 | Regra 4 (duplicata) dispararia contra o `Include` explícito de `Line.Gx18u13` do csproj satélite, que é desenho (DeepSeek) | `Compile.Shared.props` passa a excluir também `Line.*\**` (seção 4.8): o satélite recebe `Line.Gx18u13` só pelo `Include` explícito, sem duplicata. Quando `Line.Gx18u13` nascer, a regra 1 reprova o canônico — que é o sinal pretendido pela D46 (o canônico precisa deixar de compilar itens default nesse dia) |
| R9 | Paridade não depende de pacotes; pular por restore falho perdia cobertura (DeepSeek) | A paridade roda sempre; só sai `environmentBlocked` se a própria avaliação falhar (seção 4.5). Substitui a linha correspondente da T8 |
| R10 | Afrouxamento «referência ≠ invocar DLL» precisa de trava estática (Muse) | Asserções estáticas: o checker e o módulo não contêm `LoadFile`, `LoadFrom`, `Add-Type -Path` nem `[Reflection.Assembly]` (seção 4.7) |
| R11 | `Equals`/`GetHashCode` contados como API faltante (Muse) | §3.5 separa overrides ausentes de API ausente de fato |
| R12 | `satelliteRefs` não existe hoje; «substitui» sugeria migração (Opus, Muse) | D55 diz que o campo **nasce** no `B129`; o `absent\|present` só existia no papel, sem consumidor |
| R13 | Saída de `-getItem`/`-getProperty` ilegível sem status definido (Opus) | `failed`, com a saída bruta em `evidence` |
| R14 | Critério de aceite 2 sugeria `warnings[]` vazio, mas a frente gera `evidence-doc-required:*` (Opus, Muse) | Critério lê «nenhuma entrada `satellite-build:`» e declara os avisos esperados |
| R15 | «Não tocar `artifacts/`» contradizia a D-B129-3 (GPT, Muse) | Seção 10: não editar nem versionar; a reescrita pelo build é esperada |
| R16 | Regex do `#if` sob `-match` é insensível a caixa (Muse) | Diretivas C# são minúsculas: usar `-cmatch` |
| R17 | Estimativa do meta-teste ignorava as avaliações `dotnet msbuild` somadas às execuções existentes (Opus, DeepSeek) | Estimativa removida; o tempo é medido e registrado na seção 11 |
| R18 | Status global com o sentinela (Muse: `failed`; v2: `incomplete`) | Esclarecido: o sentinela é criado **antes** da execução, entra como alteração preexistente e o checker sai `incomplete`; o critério lê o status do check (seção 6) |
| R19 | Acesso negado como `failed` contradiz o princípio da D-B129-2 (Opus) | **Decisão humana de 2026-09-28:** acesso negado no satélite passa a `environmentBlocked`, kind `accessDenied`, com a mensagem «pare e reporte; repetir não resolve» (seção 4.4). Substitui essa parte da T7 |

### 2.4 Autoria da v4 (medições novas e revisão própria)

O mantenedor pediu que a v4 refletisse o **juízo do autor**, e não a soma das opiniões do painel.
Cada ponto da rodada `B129-20260928-v3` foi conferido — por medição quando possível — e
aceito ou recusado com motivo; o autor também acrescentou correções próprias.

**Medições de 2026-09-28 (seção 3.7):** idioma forçado com `-nodeReuse:false`; repetição do bloco
de avisos; duplicata de item `Compile` no `-getItem`; tarefa `Warning` com código; shape do
`-getProperty`; restore do satélite; tempo de recompilação.

| # | Origem | Ponto | Juízo do autor |
|---|---|---|---|
| V1 | autor (medição) | O bloco `MSB3277` sai **duas vezes** na mesma build — durante a compilação e repetido depois de «Build succeeded» — e o `-clp:NoSummary` **não** evita isso neste SDK | Retirado o `-clp:NoSummary` (flag sem efeito). O agrupamento por diagnóstico já trata a repetição: são dois grupos `mscorlib`, ambos conhecidos. `knownWarnings` registra a contagem de grupos |
| V2 | autor (medição) | Com `DOTNET_CLI_UI_LANGUAGE=en` e `-nodeReuse:false`, depois de uma build canônica, o cabeçalho saiu em inglês | Mantido o reconhecimento bilíngue como rede de segurança; `headerLanguage` registra o observado |
| V3 | Muse, P1 | «O MSBuild elimina duplicatas; a regra 4 nunca dispara» | **Recusado por medição**: glob + `Include` explícito sobre o mesmo arquivo devolveu o item **duas vezes** no `-getItem:Compile`. A regra 4 é observável e fica |
| V4 | Muse, P3 | «A tarefa `Warning` não emite código» | **Recusado por medição**: `<Warning Code="MSB3277" …/>` emite `warning MSB3277: …`. A fixture funciona como especificada |
| V5 | DeepSeek, P2 | `FullPath` pode não vir no JSON do `-getItem` | **Recusado**: a Fase 0 leu `FullPath` nas 104 entradas, e a medição nova confirmou o metadado |
| V6 | DeepSeek e Muse, P2 | `satelliteRefs` indefinido com o contrato do `.props` violado | Aceito, com desenho do autor: `satelliteRefs` passa a ser informado **sempre**. Pasta ausente continua `absent`, independentemente do `.props`; com a pasta presente e o contrato violado, o valor novo `unverifiable` diz que não dá para julgar as refs (seção 4.2) |
| V7 | DeepSeek, P2 | Acesso negado sem padrão de reconhecimento | Aceito: padrões em inglês e português (seção 4.4) |
| V8 | Muse, P2 | Falha de rede/feed no satélite sem status declarado | Aceito: `networkOrFeedUnavailable` e `sdkUnavailable` → `environmentBlocked`, como no canônico. Medido: nesta máquina o restore do satélite não traz nenhum pacote (targeting pack `net471` instalado); em máquina sem o targeting pack, o SDK pode buscar pacote de referência no feed |
| V9 | Muse, P2 | Deixar explícito que a saída do satélite não passa por `Add-DiagnosticWarnings` | Aceito (seção 4.2) |
| V10 | Muse, P3 | Comparação de caminhos canonicalizada e sem distinção de caixa; absoluto por `IsPathRooted`; raiz = raiz do repositório do checker | Aceito (seções 4.1, 4.2 e 4.4) |
| V11 | DeepSeek, P3 | `Reference` sem `HintPath` na fixture pode gerar aviso de resolução numa build `net10.0` | Aceito, com desenho do autor: o props **de build** da fixture tem só o `HintPath` do `FakeRef.dll`; a entrada sem `HintPath` fica nos casos de função, que não compilam |
| V12 | DeepSeek e Muse, P3 | T5 × R12 | Aceito: nota na T5 |
| V13 | DeepSeek, P3 | Contagem de chamadas `msbuild` | Aceito, medido: `-getProperty` e `-getItem` exigem duas invocações por projeto (a primeira descobre o alvo; sem alvo, o `-getItem` volta a devolver 9 itens), quatro no total |
| V14 | DeepSeek, P3 | Paridade compara só o primeiro alvo | Aceito: vai para `notCovered` |
| V15 | DeepSeek, P3 | `#if` em comentário ou string dá falso positivo | Aceito como limite declarado: o erro é para o lado seguro (reprova a mais, nunca a menos); vai para a seção 4.3 |
| V16 | autor (v4) | Medido: cada bloco `MSB3277` é **contíguo** (62 linhas seguidas), e os dois blocos são separados por linhas de outro tipo (saída do `csc`, «Build succeeded»). Pela regra da v3, uma linha sem cabeçalho logo depois da repetição seria anexada ao último grupo aberto, que podia ser `mscorlib` — isto é, absorvida como conhecida | Regra endurecida: um grupo **termina** na primeira linha que não seja `warning MSB3277:`. Continuação separada do cabeçalho vira grupo órfão e vai para `warnings[]`. Coincide com a saída medida e erra para o lado visível |

### 2.5 Autoria da v5 (rodada `B129-20260928-v4`)

Mesmo método da 2.4: cada ponto conferido e aceito ou recusado com motivo. Nenhum dos dois
revisores contestou as recusas V3–V5 da v4.

**Medição nova (2026-09-28):** build canônica seguida da build satélite **sem** `-nodeReuse:false`,
com `DOTNET_CLI_UI_LANGUAGE=en`: os dois cabeçalhos saíram em inglês. Ou seja, nesta máquina o
idioma é forçado pela variável, não pela flag.

| # | Origem | Ponto | Juízo do autor |
|---|---|---|---|
| W1 | DeepSeek, P2 | §3.2 diz «um único cabeçalho» e §3.7 mede dois | **Aceito, e a causa é do autor:** na primeira medição a contagem de cabeçalhos passou por `Sort-Object -Unique`, que fundiu os dois cabeçalhos idênticos. §3.2 ganha remissão; critério 2 e D-B129-5 passam ao plural («os grupos `mscorlib`») |
| W2 | DeepSeek, P2 | A v4 afirmava que `-nodeReuse:false` era o que impedia o cabeçalho em português, sem isolar a causa | **Aceito e confirmado por medição:** sem a flag o cabeçalho também sai em inglês. A frase causal sai. A flag fica por outro motivo, declarado como tal: não deixar nós MSBuild vivos depois da build satélite (causa 1 do `AGENTS.md`). `--disable-build-servers` não é adotado sem medição |
| W3 | DeepSeek, P2 | `Directory.Build.props` da fixture sem conteúdo delimitado | Aceito: **só** o bloco D47, sem `RestorePackagesWithLockFile` nem `GeneXusPackageReferenceVersion` |
| W4 | DeepSeek, P2 | «Roda em qualquer clone» exagera: a avaliação do canônico depende do SDK GeneXus | Aceito: redação passa a «não precisa das DLLs U13; depende do SDK canônico, como os checks `dotnet.*`» |
| W5 | Muse, P2 | Extração do caminho do `MSB4019` não especificada | Aceito: padrão e queda definidos na seção 4.4; caminho não extraível → `failed` com a saída bruta, lado visível |
| W6 | Muse, P2 | Mensagem do `fileLocked` sem conteúdo | Aceito: mensagem com `dotnet build-server shutdown` e a lembrança de fechar a IDE se ela segurar a DLL (seção 4.4) |
| W7 | Muse, P2 | Mensagens do checker podem conter literais que o meta-teste proíbe (`Program Files`, `C:\GxModels`, `Start-Process … dll`) | **Aceito — achado real.** Mensagens apontam por símbolo (D44, seção do `AGENTS.md`), nunca por caminho absoluto; a asserção estática estende-se ao módulo novo (seção 4.7) |
| W8 | Muse, P2 | Campos de `evidence` subespecificados | Aceito: `refsMissing=[]` com `absent`; `headerLanguage` ∈ `en\|pt\|mixed\|none`; `duplicates[]` com caminho e contagem; cabeçalho sem continuação é grupo conhecido com 0 linhas de continuação |
| W9 | DeepSeek, P3 | Fixture sem `..\Domain` | Aceito: a fixture ganha `Src\Domain\` com um `.cs`, incluído pelos dois projetos como em produção |
| W10 | DeepSeek, P3 | JSON do `-getItem` lido de qual stream | Aceito: parse só do **stdout**; stderr vai para `evidence` e não invalida o JSON |
| W11 | DeepSeek, P3 | `satelliteRefs` «sempre» sob `Set-StrictMode` e no caminho de exceção | Aceito: inicializado antes do `try` com `$null` e emitido em todos os caminhos; `null` só quando o check nem chegou a rodar (falha do próprio checker) |
| W12 | DeepSeek e Muse, P3 | `Temp\**` × `Temp\**\*.cs` | Aceito em parte: no `Compile.Shared.props` fica `Temp\**`, porque um `Exclude` de pasta cobre tudo o que o glob `**\*.cs` pode trazer dali — equivalente para `.cs`. Na fixture, o canônico usa `Temp\**\*.cs`, **igual** ao de produção |
| W13 | DeepSeek, P3 | Critério 2 falharia com qualquer aviso real do satélite | Aceito com ajuste: o critério continua exigindo `warnings[]` sem `satellite-build:`; se aparecer algum, a frente não fecha sem registro na seção 11 da origem e da decisão (corrigir ou aceitar com justificativa) |
| W14 | Muse, P3 | Hashes `f2967cc`/`c4202c6` sem como verificar no snapshot | Sem mudança de desenho: o autor conferiu os dois no histórico do repositório (`git log -S` e checkpoint); o plano os cita como remissão |
| W15 | Muse, P3 | «Não há `#if` em `Src/`» conferido só em `Src/Extension` | **Recusado:** o autor varreu todos os `.cs` de `Src/` recursivamente (Domain inclusive), sem ocorrência |

### 2.6 Autoria da v6 (segunda opinião sobre a v5)

Mesmo método: cada ponto conferido, por medição quando possível, e aceito ou recusado com motivo.
Os dois revisores deram «revisa»; o Opus sem P1, o Space Bunny com um P1 (Y1), que procede.

**Medições de 2026-09-28 (seção 3.7):** cópia com destino somente leitura; decodificação UTF-8 do
cabeçalho em português; `Exclude="Line.*\**"` com curinga no segmento da pasta; avaliação de
projeto nunca restaurado; contagem de `.cs` rastreados.

| # | Origem | Ponto | Juízo do autor |
|---|---|---|---|
| X1 | Opus, P2 | Acesso negado pode terminar em `MSB3026`/`MSB3027` e sair `fileLocked` («repita») | **Aceito como ordem**, premissa não confirmada: com destino somente leitura o MSBuild deu `MSB3021` direto, com «Access to the path … is denied», sem tentativas. Fixar a ordem não custa nada: `MSB4019`/`MSB4236` → acesso negado em qualquer linha → `MSB3027`/`MSB3021` com arquivo em uso → demais. Caso de função com `MSB3026` + `MSB3027` + acesso negado, esperando `accessDenied` |
| X2 | Opus, P2 | `Get-FailureKind` sobre a saída inteira transforma erro do commit em ambiente (`proxy`, `timed out`, `NETSDK*` do csproj, `SDK … not found` do `MSB4019`) | **Aceito.** Nos checks novos, a classificação lê só as linhas de erro (`: error `) e os avisos `MSB3026`; as regras específicas vêm antes do `Get-FailureKind`; caso negativo com erro `CS*` contendo «Proxy» e «timed out», esperando `failed` |
| X3 | Opus, P2 | O caso «satélite presente» ignora a repetição do bloco medida | **Aceito.** Entradas idênticas de `warnings[]` vindas do satélite são deduplicadas, com o número de ocorrências em `evidence`; as asserções passam a ter contagem (seção 4.7) |
| X4 | Opus, P3 | Justificativa do `-nodeReuse:false` diz mais do que a flag cobre | **Aceito:** a flag só impede nós MSBuild reutilizáveis; o `VBCSCompiler` segue vivo, e só `dotnet build-server shutdown` cobre os dois (`AGENTS.md`). Redação corrigida e limite em `notCovered` |
| X5 | Opus, P3 | Modo do logger implícito; regra 6 com `\bwarning\b` solto | **Aceito.** Medido: com saída redirecionada o formato já é o clássico, uma linha por aviso. `-tl:off` explícito mesmo assim; regra 6 casa `: warning [A-Z]+[0-9]+:` |
| X6 | Opus, P3 | Cabeçalho em português pode chegar com acentuação quebrada | **Recusado por medição:** lendo a saída como UTF-8, igual ao `Invoke-ExternalProcess`, o cabeçalho chegou íntegro («versões»). Mantido o padrão exato. Aceita a sugestão de a fixture afirmar `headerLanguage = mixed` |
| X7 | Opus, P3 | `HintPath` com curinga passaria; `Test-Path` sem `-LiteralPath`; `Reference` fora do `.props` escapa | **Aceito:** curinga (`*`, `?`) no `HintPath` → `failed`; presença verificada com `-LiteralPath`; `Reference` declarado fora do `.props` vai para `notCovered` |
| X8 | Opus, P3 | Refs `incomplete` com build canônico falho sairia `environmentBlocked`, mascarando o defeito do commit | **Aceito:** ordem contrato → falha canônica (`skipped`) → estado das refs; `satelliteRefs` continua informado |
| X9 | Opus, P3 | Critério 3 nunca produz o «antes» pelo check | **Aceito:** na implementação, o orquestrador roda com o sentinela **antes** de aplicar a correção do `Compile.Shared.props` |
| X10 | Opus, P3 | O meta-teste atual não usa `git revert` | **Aceito:** redação corrigida — o revert é novo, adotado pelo `B129` |
| X11 | Opus, P3 | `Only.Sat\**` sem `*.cs` | **Aceito:** `Only.Sat\**\*.cs` |
| X12 | Opus P3 e Space Bunny P2 | Bloco D47 da fixture ambíguo e incompleto (produção tem sete propriedades; a v6 disse seis, corrigido pela G3) | **Aceito:** a fixture copia o bloco condicional D47 **inteiro**, igual ao de produção, e nada do grupo incondicional |
| Y1 | Space Bunny, P1 | O `git add` inicial da fixture não inclui `Directory.Build.props` da raiz: a base sairia `incomplete` com aviso | **Aceito — defeito certo.** Todos os arquivos novos da fixture entram no commit inicial; a base afirma `git.statusPre = passed` |
| Y2 | Space Bunny, P2 | `Exclude="Line.*\**"` (curinga no segmento) sem controle positivo | **Aceito e medido:** num projeto de teste, `Line.Gx18u13\A.cs` veio **uma vez** (glob excluído, `Include` explícito mantido) e `Line.Gx18u14plus\C.cs` ficou fora. A execução «defeitos de fonte» ganha `Line.Gx18u13/B129Line.cs`, com o satélite recebendo o arquivo uma vez, paridade com ele em `forbiddenInCanonical[]` |
| Y3 | Space Bunny, P2 | Critério 2 depende da máquina | **Aceito:** condicional ao `headerLanguage` observado |
| Y4 | Space Bunny, P2 | O canônico da fixture nunca é restaurado | **Aceito como nota, medido:** a avaliação de projeto `Microsoft.NET.Sdk` sem restore (sem `obj/`) devolveu os itens. A origem do multi-alvo (SDK GeneXus × `TargetFrameworks` declarado) vai para `notCovered` |
| Y5 | Space Bunny, P2 | A linha «Release U13 (mantenedor)» do §6.5 da Opção B («**não** o checker pré-push») fica ambígua | **Aceito:** entra nas remissões, e o radical `não o checker pré-push` entra na varredura |
| Y6 | Space Bunny, P3 | `Items.Compile` vazio ou ausente sob `Set-StrictMode` | **Aceito:** leitura com queda para `@()` e caso de função |
| Y7 | Space Bunny, P3 | A D55 afirmava mais do que a trava estática garante | **Aceito:** a D55 diz «o checker não carrega assembly», não «a única interação é o compilador» |
| Y8 | Space Bunny, P3 | «Contei 102 `.cs`, o plano diz 104» | **Recusado por medição:** `git ls-files` devolve 104 `.cs` rastreados em `Src/Extension` e `Src/Domain` |
| Y9 | Space Bunny, P3 | O MSBuild resolve `HintPath` relativo ao diretório do projeto, não do `.props` | **Aceito:** a regra usa o diretório do projeto satélite; hoje coincide com o do `.props` |
| Y10 | Space Bunny, P3 | Multi-alvo da fixture ≠ origem real | Absorvido no Y4 |
| Y11 | Space Bunny, P3 | Acoplamento com a D46 quando `Line.*` nascer ausente dos riscos | **Aceito:** linha na seção 7 |
| Z1 | autor (revisão final da v6) | A regra 6 (`: warning [A-Z]+[0-9]+:`) também casaria as linhas `MSB3277` | Regra 6 passa a excluir `MSB3277`, tratado só pelas regras 2 a 5 |
| Z2 | autor | Grupo não conhecido ia «inteiro» para `warnings[]` — 60 entradas por grupo | Cada grupo vira **uma** entrada, com contagem; as linhas vão para `evidence` |
| Z3 | autor (medição) | Erro de avaliação do `-getItem` sai no stdout no lugar do JSON | A classificação roda antes do «JSON ilegível → `failed`» (seção 4.1) |
| Z4 | autor (medição) | `-tl:off` não tinha sido testado no `dotnet build` | Medido: aceito, mesma saída |

### 2.7 Autoria da v7 (parecer solo do GPT 6 Sol sobre a v6)

| # | Ponto | Juízo do autor |
|---|---|---|
| G1 | **P1:** a v6 mandava o `Get-FailureKind` ler as linhas de erro e, ao mesmo tempo, prometia que um erro `CS*` com «proxy»/«timed out» sairia `failed` — mas a heurística casa essas palavras em qualquer linha | **Aceito — contradição real, do autor.** A heurística de rede/SDK passa a considerar só linhas de erro cujo código seja de infraestrutura (`NU\d+`, `NETSDK\d+`, `MSB\d+`) ou sem código; erro de compilação `CS\d+` vai direto para `failed` (seção 4.4) |
| G2 | **P2:** ignorar todo `Reference` sem `HintPath` deixaria passar uma referência Artech nova sem pino | **Aceito:** sem `HintPath`, só `System.Drawing` e `System.Windows.Forms` (lista fechada); qualquer outro `Reference` sem `HintPath` → `failed`. Todo `Reference` com `HintPath` precisa de `Private=false` (§5.1 da Opção B) |
| G3 | **P3:** a fixture fala em «seis propriedades» do bloco D47 e enumera sete | **Aceito:** são sete |

### 2.8 Revisão integral do autor sobre a v7

O mantenedor observou que cada rodada de remendo deixava gaps novos. Antes da consulta seguinte, o
autor releu o corpo inteiro contra ele mesmo, procurando contradição entre seções, e não só o
trecho alterado.

| # | Achado | Correção |
|---|---|---|
| A1 | As subseções de triagem guardam decisões superadas (T8, R2, T5…) no meio do texto normativo; um leitor pode tomá-las como regra vigente | Nota de precedência no início da seção 2: o corpo prevalece |
| A2 | A seção 4.7 ainda dizia «a única interação com as DLLs U13 é a leitura de metadado pelo compilador», frase que a Y7 tinha abrandado só na seção 5 | Alinhada: a trava garante que nem o checker nem o módulo carregam assembly |
| A3 | A seção 6 listava a correção do `Compile.Shared.props` **antes** dos checks, mas o sentinela exige rodar o check **antes** da correção (critério 3) | Ordem da fase 1 invertida: módulo, checks e meta-teste primeiro; sentinela «antes»; só então a correção do props e o sentinela «depois» |
| A4 | O `AGENTS.md` manda rodar o meta-teste quando o checker ou o teste mudam; o módulo novo não estava na regra | A atualização do `AGENTS.md` (seção 5) inclui `scripts/B129-SatelliteChecks.ps1` no gatilho do passo 6 |
| A5 | A seção 12 ainda falava do «destino da v6» | Atualizada |

### 2.9 Autoria da v8 (parecer solo do subagente Opus 5.5 sobre a v7)

O revisor rastreou o caso «satélite presente» regra a regra e confirmou o resultado afirmado; não
achou P1. Os três P2 são de coerência interna e procedem. Medição nova: avaliação externa de um
projeto `Microsoft.NET.Sdk` com `TargetFrameworks` devolveu **0** itens; com `-p:TargetFramework`,
devolveu 1 (seção 3.7).

| # | Ponto | Juízo do autor |
|---|---|---|
| B1 | **P2:** a seção 6 pedia o orquestrador «com a árvore limpa» antes de qualquer commit — ordem impossível; o `AGENTS.md` só aceita a rotina sobre a frente commitada | **Aceito.** Um commit local da implementação entra entre a correção do props e a validação; as execuções do sentinela são diagnóstico intermediário (seção 6) |
| B2 | **P2:** a G1 mandava `NETSDK*`/`MSB*` para a heurística, que classifica **qualquer** `NETSDK` como SDK ausente e casa «proxy» em qualquer texto — um `NETSDK1013` do csproj ou um `MSB3030` citando `ProxyHelper.dll` sairiam «ambiente» | **Aceito — erro do autor na G1.** A heurística sai: `sdkUnavailable` e `networkOrFeedUnavailable` passam a listas fechadas de códigos e frases (seção 4.4); todo o resto é `failed`. Casos de função `NETSDK1013` e `MSB3030` com «Proxy» → `failed` |
| B3 | **P2:** «refs incompletas» não tem mudança rastreada; seguido ao pé da letra, o `git revert` desfaria o revert anterior e reintroduziria o `.cs` inválido | **Aceito.** Cenário sem mudança rastreada não faz commit nem revert; todo `git commit`/`git revert` novo confere o código de saída (seção 4.7) |
| B4 | **P3:** com o sentinela, a execução «antes» sai `failed` (exit 1), não `incomplete` | **Aceito:** texto corrigido |
| B5 | **P3:** em que campo do JSON aparece o motivo (`accessDenied`, `canonicalBuildFailed`…) | **Aceito:** `evidence.kind` em todos os checks novos; as asserções usam esse campo, não o `summary` |
| B6 | **P3:** `.props` com XML malformado cairia no `catch` global como ambiente | **Aceito:** parse inválido → `failed`, `satelliteRefs=unverifiable` com a pasta presente; caso de função |
| B7 | **P3:** a regra 6 perdia avisos sem código (`warning : …`) | **Aceito:** `: warning( [A-Z]+[0-9]+)?:`, excluído o `MSB3277` |
| B8 | **P3:** `Private` como elemento ou atributo; caixa dos booleanos e dos cabeçalhos | **Aceito:** as duas formas, sem distinção de caixa; cabeçalhos também sem distinção de caixa |
| B9 | **P3:** a seção 10 dizia «duas linhas» do `AGENTS.md`, e a seção 5 lista três mudanças | **Aceito:** três |
| B10 | **P3:** a mensagem que cita a D46 não estava especificada | **Aceito:** texto na seção 4.1 |
| B11 | **P3:** nada prova que o multi-alvo da fixture é exercitado | **Aceito e medido:** sem alvo, a avaliação devolve 0 itens. A execução base afirma `evidence.targetFrameworks` = `net10.0` para os dois projetos, prova de que a descoberta do alvo rodou |
| B12 | **P3:** `Phase` do `Get-FailureKind` no satélite | Absorvido pela B2: os checks novos não usam mais o `Get-FailureKind` |
| B13 | autor | Com a B2, a tabela de regras substitui as descrições em prosa da seção 4.4 das versões anteriores (G1, X1, X2, R6, V7, V8, W5, W6) | Seção 4.4 reescrita como tabela ordenada e fechada |

### 2.10 Fechamento da v8 (revisão final do autor)

O mantenedor declarou a v8 a última versão: o que ficar aqui vai para a execução. O autor releu o
corpo inteiro mais uma vez e fechou o que ainda estava implícito:

| # | Achado | Correção |
|---|---|---|
| F1 | A seção 4.4 não dizia **quando** a classificação roda, nem o que acontece com processo que falha sem linha de erro | Roda só com código de saída diferente de zero; falha sem linha de erro → `failed`, kind `unclassified` |
| F2 | A lista de valores de `evidence.kind` cobria só alguns motivos | Lista completa na seção 4.4, inclusive os da paridade e do contrato |
| F3 | O `FakeRef.dll` era compilado «num diretório temporário irmão» sem dizer quem o apaga | Dentro da raiz temporária do meta-teste, apagada pelo `finally` existente; ciclo por cenário declarado |
| F4 | A promoção do plano a `Docs/Implementation/` e ao checkpoint não aparecia nas fases | Passo 0 da seção 6 |
| F5 | O documento não dizia a quem executa o que é normativo | Guia no cabeçalho: corpo normativo, seção 2 é registro |
| F6 | As contagens do caso «satélite presente» supunham, sem medir, que a tarefa `Warning` da fixture é repetida depois de «Build succeeded» como o `MSB3277` real | Medido: é repetida, contígua e na mesma ordem (seção 3.7); as contagens valem |

---

## 3. Fase 0 — resultados medidos (2026-09-28)

SDK `dotnet` 10.0.401. `dotnet build-server shutdown` antes e depois. Repositório limpo no fim;
sentinela e cópias temporárias removidos. Medições da máquina do mantenedor; não reproduzíveis
num clone sem cache NuGet e sem `Src/Lib/Gx18u13`.

### 3.1 Linha de base

| Medida | Valor |
|---|---|
| Orquestrador pré-push atual, completo | 74,7 s, `status=passed`, `warnings=0` |
| Meta-teste atual (`Test-OpenApiBuilderPrePushChecks.ps1`) | 481 s (8 min), exit 0 |
| Build satélite incremental **sem mudança de fonte**, caminho padrão (primeira medição) | 4,3 s |
| Build satélite com os parâmetros da v4, depois de uma build canônica, **sem mudança de fonte** | 1,3 s |
| Build satélite com os parâmetros da v4, **com fonte alterada** (recompilação real) | 4,3 s |

O acréscimo ao pré-push fica, então, entre ~1 e ~5 s, mais as quatro avaliações da paridade
(~0,5–0,6 s cada). O custo que pesa é o do meta-teste (seção 4.7), medido na implementação.

### 3.2 `MSB3277` é um bloco, não uma linha

A build satélite (MSBuild em português) emitiu **124 linhas** contendo `MSB3277` e o cabeçalho
abaixo. **Remissão (v5):** esta medição contou cabeçalhos **distintos** (`Sort-Object -Unique`); a
seção 3.7 mostra que são dois cabeçalhos idênticos, um por bloco. O texto: `foram encontrados conflitos entre diferentes versões do "mscorlib" que não puderam
ser resolvidas.` As linhas de continuação citam `Artech.*`, `Newtonsoft.Json` e caminhos, **sem**
`mscorlib`. Filtro por linha vazaria ~120 linhas. Fora do `MSB3277`, a única linha com «aviso» foi
o resumo `1 Aviso(s)`. A regra adotada está na seção 4.2.

### 3.3 Paridade por avaliação exige `TargetFramework`

- O canônico é **multi-alvo** pelo SDK GeneXus (`TargetFrameworks=net471`,
  `IsCrossTargetingBuild=true` na avaliação externa). `dotnet msbuild <csproj> -getItem:Compile`
  sem alvo devolve **9 itens** (só o `Include` explícito de `..\Domain`), porque os itens default
  do SDK só existem na avaliação interna.
- Com `-p:TargetFramework=net471`: canônico **104** itens, satélite **104**, diferença **0**.
  ~0,5–0,6 s por projeto.

### 3.4 Controle positivo da paridade (sentinela)

`Src/Extension/Temp/B129Sentinel.cs`, criado e apagado na mesma etapa: satélite **105** itens,
canônico **104**, `onlySatellite = Extension\Temp\B129Sentinel.cs`; a DLL canônica compilada não
continha o tipo (e continha `B109ExceptionProbe`, controle da leitura). A divergência `Temp` está
confirmada. A metade «depois da correção» fica para a validação.

### 3.5 Superfície de API: SDK de compilação canônico × DLLs U13

Comparação por metadados (PEReader) entre `artech.*.sdk/18.13.2/lib/net471` do cache NuGet (o que
o canônico compila) e `Src/Lib/Gx18u13` (o que o satélite compila), nos oito `Artech.*`
referenciados:

- **tipos públicos:** nenhum tipo do SDK canônico falta no U13;
- **membros públicos por nome e aridade:** 5 existem no canônico e faltam no U13. Dois deles
  (`BasedOnReference.Equals/1` e `BasedOnReference.GetHashCode/0`) são **overrides** de
  `System.Object` cuja ausência não quebra chamador; API ausente de fato são **3**:
  `IPBag.AndroidFlexibleClientVersion`, `IPBag.AndroidFlexibleClientVersionBuilding` e
  `GeneratorDefinitionFactory.GeneratorsExtensionWithImports`.

**Controle positivo**, numa cópia descartável do fonte e das refs: um arquivo com
`nameof(Artech.Common.Helpers.SharedMemory.IPBag.AndroidFlexibleClientVersion)` compilou no
canônico com 0 erros e falhou no satélite com `CS0117`. O gate pega o que promete.

**Alcance desta medição (hipótese, não conclusão):** a comparação foi por nome e aridade, não por
assinatura completa nem comportamento. Ela é compatível com a hipótese de que, na superfície
medida, o SDK de compilação canônico (`GeneXusPackageReferenceVersion=18.13.2`) está próximo da API
U13 e que a diferença U13 × U14+ se manifeste sobretudo em runtime — mas **não** a prova, nem
quantifica o risco. Build satélite e validação na IDE U13 continuam evidências distintas.

### 3.6 Asset

A build satélite no caminho padrão reescreve `artifacts/gx18u13/GenexusOpenApiBuilder.Extension-gx18u13.dll`.
É o comportamento aceito na D-B129-3.

### 3.7 Medições da autoria da v4 (2026-09-28, depois da rodada v3)

- **Idioma:** build canônica e, em seguida, build satélite com `DOTNET_CLI_UI_LANGUAGE=en` e
  `-nodeReuse:false`: o cabeçalho saiu em inglês (`Found conflicts between different versions of
  "mscorlib" that could not be resolved.`). Repetido **sem** `-nodeReuse:false` (v5): os dois
  cabeçalhos também saíram em inglês — é a variável que força o idioma.
- **Repetição do bloco:** a saída tem 134 linhas; as 124 com `warning MSB3277:` formam **dois**
  blocos de 62, com um cabeçalho cada — um durante a compilação, outro repetido depois de «Build
  succeeded». Entre eles há linhas que não são `MSB3277` (restore, saída do `csc`, «Build
  succeeded»). Com `-clp:NoSummary` o resultado foi o mesmo: a flag não tem efeito aqui. Fora do
  `MSB3277`, só as linhas de contagem (`1 Warning(s)`, `0 Error(s)`).
- **Duplicata de item:** num projeto de teste com `EnableDefaultCompileItems=false`,
  `Include="**\*.cs"` e `Include="Line.X\**\*.cs"`, o `-getItem:Compile` devolveu `Line.X\A.cs`
  **duas vezes**.
- **Tarefa `Warning`:** `<Warning Code="MSB3277" Text="…"/>` emite `warning MSB3277: …`.
- **`-getProperty`:** com duas propriedades, devolve JSON (`{"Properties":{"TargetFramework":"",
  "TargetFrameworks":"net471"}}` no canônico). Com `-p:TargetFramework=net471`, o mesmo comando
  aceita também `-getItem:Compile` (104 itens, com `FullPath`).
- **Restore do satélite:** o `project.assets.json` do satélite não tem nenhum pacote (`libraries`
  vazio) — nesta máquina o restore não toca o feed.
- **(v6) Cópia com destino somente leitura:** `error MSB3021: Unable to copy file … Access to the
  path '…' is denied.`, sem avisos de nova tentativa (`MSB3026`).
- **(v6) Codificação:** com a saída redirecionada e lida como UTF-8 (como o `Invoke-ExternalProcess`
  faz), sem forçar idioma, o cabeçalho em português chegou íntegro («versões»), no formato clássico
  de uma linha por aviso (124 linhas).
- **(v6) `Exclude` com curinga:** projeto de teste com `Include="**\*.cs"`
  `Exclude="bin\**;obj\**;Temp\**;Line.*\**"` mais `Include="Line.Gx18u13\**\*.cs"`:
  `Line.Gx18u13\A.cs` veio uma vez e `Line.Gx18u14plus\C.cs` ficou fora.
- **(v6) Projeto nunca restaurado:** o mesmo projeto de teste, sem `obj/`, respondeu ao
  `-getItem:Compile`.
- **(v6) Contagem:** `git ls-files` devolve 104 `.cs` rastreados em `Src/Extension` e `Src/Domain` —
  o mesmo número da avaliação da seção 3.3.
- **(v6, revisão do autor) `-tl:off`:** `dotnet build` do satélite com `-nodeReuse:false -tl:off`
  aceitou as flags e deu a mesma saída (124 linhas, 2 cabeçalhos).
- **(v8) Multi-alvo em `Microsoft.NET.Sdk`:** projeto com `<TargetFrameworks>net10.0</TargetFrameworks>`
  e um arquivo C#; o `-getItem:Compile` sem alvo devolveu **0** itens; com `-p:TargetFramework=net10.0`, 1.
  O canônico da fixture reproduz, portanto, o comportamento do canônico real da seção 3.3.
- **(v8 final) Repetição de avisos da tarefa `Warning`:** projeto `net10.0` com um target que emite,
  em ordem, uma linha `MSB3277` órfã, um grupo `mscorlib` (cabeçalho em inglês + continuação) e um
  grupo de outro assembly (cabeçalho em português + continuação), compilado com
  `DOTNET_CLI_UI_LANGUAGE=en -nodeReuse:false -tl:off`: as cinco linhas saíram contíguas e foram
  **repetidas**, na mesma ordem e contíguas, depois de «Build succeeded»; o texto em português veio
  íntegro. É o comportamento em que se apoiam as contagens do caso «satélite presente» (seção 4.7).
- **(v6, revisão do autor) Erro de avaliação:** `dotnet msbuild <csproj inexistente> -getItem:Compile`
  escreve o erro (`MSBUILD : error MSB1009: …`) no **stdout**, no lugar do JSON.

---

## 4. Desenho

### 4.1 `msbuild.compileSetParity` — paridade dos itens `Compile` avaliados

- **Projetos:** `Src\Extension\GenexusOpenApiBuilder.Extension.csproj` e
  `Src\Extension\GenexusOpenApiBuilder.Extension.Gx18u13.csproj`.
- **Avaliação:** para cada um, `dotnet msbuild <csproj> -getProperty:TargetFramework
  -getProperty:TargetFrameworks`; alvo = `TargetFramework` ou o primeiro de `TargetFrameworks`;
  depois `dotnet msbuild <csproj> -getItem:Compile -p:Configuration=Release
  -p:TargetFramework=<alvo>`. São duas invocações por projeto, quatro no total: a primeira descobre
  o alvo, e sem alvo o `-getItem` do canônico devolve só 9 itens (seção 3.3). Não fixar `net471`
  no checker. O JSON é lido **só do stdout**; o stderr vai para `evidence` e não invalida o parse.
  Erro de avaliação sai no stdout **no lugar** do JSON (seção 3.7): nesse caso a classificação da
  seção 4.4 roda sobre as linhas de erro dos dois streams, e só depois, se nenhuma regra casar, vale
  o «JSON ilegível → `failed`».
  `Items.Compile` vazio ou ausente é lido como lista vazia (`@()`), sem exceção sob `Set-StrictMode`.
- **Normalização:** `FullPath` canonicalizado (`[IO.Path]::GetFullPath`), relativo a `Src/`,
  separador `/`, comparação sem distinção de caixa (Windows).
- **Regras (todas obrigatórias):**
  1. o canônico **não** contém `Extension/Line.Gx18u13/**`;
  2. o satélite **não** contém `Extension/Line.Gx18u14plus/**`;
  3. fora dessas duas pastas, os conjuntos são **iguais**;
  4. nenhum projeto contém o mesmo arquivo duas vezes (comparação como multiconjunto; o
     `-getItem` preserva duplicatas, seção 3.7).

  Violação de qualquer regra → `failed`, com `onlyCanonical[]`, `onlySatellite[]`,
  `forbiddenInCanonical[]`, `forbiddenInSatellite[]` e `duplicates[]` (projeto, caminho e
  contagem) em `evidence`. Em qualquer status, `evidence.targetFrameworks` registra o alvo usado
  em cada projeto e `evidence.itemCounts` o número de itens de cada um.
- **Hoje** não existe pasta `Line.*`: as regras 1, 2 e 4 nascem verdes. Com a exclusão de
  `Line.*\**` no `Compile.Shared.props` (seção 4.8), o satélite recebe `Line.Gx18u13` só pelo
  `Include` explícito do csproj, sem duplicata. Quando `Line.Gx18u13` nascer, a regra 1 reprova o
  canônico (itens default) — é o sinal pretendido pela D46: nesse dia o canônico passa a
  `EnableDefaultCompileItems=false` e importa o props compartilhado. A mensagem do check, quando
  `forbiddenInCanonical[]` não estiver vazio, diz: «o projeto canônico compila código de
  `Line.Gx18u13`; pela D46, ao criar `Line.*` o canônico passa a `EnableDefaultCompileItems=false`
  e importa `Compile.Shared.props`».
- **Status adicionais:** `environmentBlocked` se a avaliação falhar por SDK/rede (seção 4.4);
  `failed` se a saída de `-getProperty`/`-getItem` não for JSON legível, com a saída bruta em
  `evidence`; `skipped` só se **os dois** `.csproj` estiverem ausentes; um presente e outro
  ausente = `failed`.
- **Implementação:** a normalização e as quatro regras ficam como função pura no módulo
  `scripts/B129-SatelliteChecks.ps1` (seção 4.9).
- **Limites (vão para `notCovered`):** compara os itens `Compile` **avaliados**, não o conjunto final
  depois de targets de geração; não compara `LangVersion`, `DefineConstants`, recurso embutido
  `.package`, carimbo `GxLine` nem `PackageCompatibility`; compara só o primeiro alvo de cada
  projeto (hoje cada um tem um só); vê o disco no momento (um `.cs` não rastreado conta, e working
  tree suja já deixa o checker `incomplete`); na fixture, o multi-alvo vem de `TargetFrameworks`
  declarado, não do SDK GeneXus como em produção.

### 4.2 `dotnet.buildSatellite`

**Contrato do `.props` (validado sempre, com ou sem a pasta de DLLs).** Ler
`Src/Extension/Lib.Gx18u13.References.props` como XML **com o namespace do MSBuild**
(`local-name()` ou namespace manager). `Reference` sem `HintPath` só é aceito para a lista fechada
de referências de framework `System.Drawing` e `System.Windows.Forms`. Reprova com `failed`
(defeito do repositório) se: o `.props` falta; há `Reference` sem `HintPath` fora dessa lista; algum
`Reference` com `HintPath` não tem `Private=false` (§5.1 da Opção B; aceito como elemento-filho ou
atributo, valor sem distinção de caixa, como o MSBuild aceita); o XML não é legível (parse
inválido — nunca deixar a exceção cair no `catch` global do checker); não há nenhum `HintPath`; algum `HintPath` tem curinga (`*`,
`?`; invariante 4 da Opção B); algum é absoluto (`[IO.Path]::IsPathRooted`); ou algum, resolvido
relativo ao diretório do **projeto satélite** (é assim que o MSBuild resolve; hoje coincide com o
diretório do `.props`) e canonicalizado, cai fora de `Src/Lib/Gx18u13` (contrato D34; comparação
sem distinção de caixa). A presença de cada arquivo é verificada com `-LiteralPath`. A validação do
contrato vem **antes** de qualquer outra condição da seção 4.5: contrato violado é `failed` mesmo
com o build canônico falho, e o satélite não compila. `Reference` declarado diretamente no csproj
satélite, fora do `.props`, não é validado (`notCovered`).

**Estado das refs** (`satelliteRefs`, informado **sempre**):

| `satelliteRefs` | Condição | Status do check |
|---|---|---|
| `absent` | `Src/Lib/Gx18u13` não existe (qualquer que seja o estado do `.props`) | `skipped` (`satelliteRefsAbsent`) se o contrato for válido; `failed` se violado. Em ambos, fora de `warnings[]` |
| `unverifiable` | pasta existe e o contrato do `.props` está violado | `failed` (contrato) |
| `incomplete` | contrato válido, pasta existe, falta ≥1 arquivo pinado | `environmentBlocked` (`satelliteRefsIncomplete`); mensagem: repetir não resolve, lista dos faltantes, D44 |
| `complete` | contrato válido, todos os arquivos pinados presentes | executa (salvo seção 4.5) |

`complete` significa **«referências pinadas presentes»**, não «DLLs U13 comprovadas»: arquivos de
outra instalação também compilariam. Identidade/proveniência U13 fica fora da cobertura do
pré-push (`notCovered`) e continua com a D31 no corte.

**Comando:** `dotnet build Src\GenexusOpenApiBuilder.Gx18u13.sln --configuration Release
-nodeReuse:false -tl:off`, com a variável `DOTNET_CLI_UI_LANGUAGE=en` **só no processo filho**
(estender `Invoke-ExternalProcess` com parâmetro opcional de ambiente). A variável força o
cabeçalho em inglês, com ou sem `-nodeReuse:false` (seção 3.7). A flag fica por outro motivo: não
deixar nós MSBuild reutilizáveis vivos depois da build satélite — cobertura parcial da causa 1 do
`AGENTS.md`, porque o `VBCSCompiler` continua vivo e só `dotnet build-server shutdown` cobre os
dois (`notCovered`). `-tl:off` fixa o logger clássico, de uma linha por aviso, que o classificador
pressupõe; com saída redirecionada ele já é o padrão, e a flag foi aceita sem mudar a saída
(seção 3.7). O reconhecimento bilíngue abaixo fica como rede de segurança. Restore implícito:
nesta máquina não traz pacote nenhum (seção 3.7); numa máquina sem o targeting pack `net471`, o SDK
pode buscar pacote de referência no feed — falha de feed é `environmentBlocked` (seção 4.4) e o
limite vai para `notCovered`.

**Avisos.** A saída do build satélite **não** passa por `Add-DiagnosticWarnings`, que continua
servindo só ao canônico; passa apenas pelo classificador abaixo:

1. excluir as linhas de contagem (`N Warning(s)`, `N Error(s)`, `N Aviso(s)`, `N Erro(s)`) de todos
   os canais;
2. **cabeçalho reconhecido** = linha `warning MSB3277:` que casa, sem distinção de caixa, em
   inglês `Found conflicts between different versions of "<asm>"` ou, em português,
   `foram encontrados conflitos entre diferentes versões do "<asm>"`;
3. **agrupamento por diagnóstico**, na ordem da saída: cada cabeçalho reconhecido abre um grupo novo
   com o seu `<asm>`; as linhas `warning MSB3277:` **imediatamente** seguintes pertencem a esse
   grupo; o grupo **termina** no próximo cabeçalho ou na primeira linha que não seja
   `warning MSB3277:`; uma linha `warning MSB3277:` que não seja cabeçalho e não esteja dentro de
   um grupo aberto forma **grupo órfão**;
4. grupo com `<asm> = mscorlib` → `evidence.knownWarnings`;
5. grupo de outro assembly, ou grupo órfão → **uma** entrada em `warnings[]`, com prefixo
   `satellite-build:`, o texto do cabeçalho (ou da primeira linha, no órfão) e o número de linhas
   do grupo — o grupo inteiro vai para `evidence`, não 60 entradas soltas;
6. qualquer outro aviso — linha que casa `: warning( [A-Z]+[0-9]+)?:` (com ou sem código) e **não**
   é `MSB3277`, que as regras 2 a 5 tratam com exclusividade — → `warnings[]` com prefixo
   `satellite-build:`;
7. entradas idênticas de `warnings[]` vindas do satélite são deduplicadas (a saída repete o bloco
   de avisos, seção 3.7), com o número de ocorrências em `evidence`.

A saída real tem o bloco **duas vezes** (durante a compilação e repetido depois de «Build
succeeded»), cada bloco contíguo e separado do outro por linhas de outro tipo (seção 3.7): são dois
grupos `mscorlib`, ambos conhecidos. Se alguma continuação chegar separada do seu cabeçalho, ela
vira órfã e vai para `warnings[]` (ruído visível); nunca é absorvida por um grupo conhecido.

**`evidence`:** `refsState`; `refsExpected[]`; `refsMissing[]` (vazio quando `absent`);
`knownWarnings` (número de grupos, cabeçalhos e total de linhas; cabeçalho sem continuação é grupo
conhecido com 0 linhas de continuação); `headerLanguage` (`en`, `pt`, `mixed` ou `none`);
`durationMs`.

**Implementação:** leitura do contrato do `.props`, classificação dos avisos e classificação de
falha ficam como funções puras no módulo `scripts/B129-SatelliteChecks.ps1` (seção 4.9).

### 4.3 `source.packageNoIfDirective` (D15)

Lê `Src/Extension/Package.cs`; `failed` se alguma linha casar `^\s*#\s*if\b` **com `-cmatch`**
(diretivas C# são minúsculas), com as linhas em `evidence`. Escopo literal da D15: só
`Package.cs`. Arquivo ausente: `failed`. Hoje nasce `passed` (não há `#if` em `Src/`). Limite
declarado: a leitura é por linha e não distingue comentário nem string; um `#if` no início de linha
dentro de comentário de bloco ou string literal reprova — erro para o lado seguro.

### 4.4 Classificação de falhas nos checks novos

**Princípio.** Uma falha só sai `environmentBlocked` quando casa uma regra **fechada** de
ambiente; tudo o que não casar é `failed`. Os checks novos **não** usam o `Get-FailureKind` atual,
que classifica qualquer `NETSDK*` como SDK ausente e casa «proxy»/«timed out» em qualquer texto — o
que transformaria defeito do commit (um `NETSDK1013` de `TargetFramework` inválido, um `MSB3030`
citando `ProxyHelper.dll`) em ambiente.

**Quando roda.** Só quando o processo (`dotnet msbuild` da paridade ou `dotnet build` do satélite)
sai com código diferente de zero. Se sair com erro e **nenhuma** linha de erro for encontrada, o
resultado é `failed` com kind `unclassified` e a saída bruta em `evidence`.

**Entrada.** Só as linhas de erro (que contêm `: error `) e os avisos `MSB3026` dos dois streams.
Nunca a saída inteira: o bloco `MSB3277` traz nomes de assembly e caminhos que casariam padrões de
texto. Os padrões de texto são sem distinção de caixa e existem em inglês e português; códigos
`MSB*`/`NU*`/`NETSDK*` não dependem de idioma.

**Regras, em ordem — a primeira que casa decide:**

| # | Regra | Kind | Status |
|---|---|---|---|
| 1 | `MSB4236` (SDK do projeto não encontrado) | `sdkUnavailable` | `environmentBlocked` |
| 2 | `MSB4019` com caminho **fora** da raiz do repositório | `sdkUnavailable` | `environmentBlocked` |
| 3 | `MSB4019` com caminho **dentro** da raiz (por exemplo, `.props` versionado apagado) | `repositoryImportMissing` | `failed` |
| 4 | `MSB4019` com caminho não extraível | `unclassified` | `failed` (saída bruta em `evidence`) |
| 5 | acesso negado: `Access to the path … is denied` / `Acesso ao caminho … foi negado` / `Access is denied` / `Acesso negado`, em qualquer linha da entrada | `accessDenied` | `environmentBlocked` |
| 6 | `MSB3027`, ou `MSB3021` com `being used by another process` / `sendo usado por outro processo` | `fileLocked` | `environmentBlocked` |
| 7 | `NETSDK1045` (SDK instalado não suporta o alvo), ou linha **sem código** com `A compatible installed .NET SDK` / `compatible .NET SDK was not found` | `sdkUnavailable` | `environmentBlocked` |
| 8 | `NU1301`, ou linha `NU\d+` **ou sem código** com `Unable to load the service index`, `No such host`, `Name or service not known`, `connection (refused\|reset)` | `networkOrFeedUnavailable` | `environmentBlocked` |
| 9 | qualquer outra linha de erro (inclusive `CS*`, outros `NETSDK*`, outros `MSB*`, outros `NU*`) | `compilationOrBuildFailure` | `failed` |

**Detalhes:**

- No `MSB4019`, o caminho é o primeiro trecho entre aspas depois de `MSB4019:` (a mensagem cita o
  projeto importado entre aspas, em inglês e em português). «Dentro/fora da raiz» compara caminhos
  canonicalizados, sem distinção de caixa, contra a raiz que o checker já calcula.
- Mensagem do `fileLocked`: rodar `dotnet build-server shutdown` e repetir; se persistir com a IDE
  GeneXus aberta, fechá-la (causa 1 do `AGENTS.md`, transitória).
- Mensagem do `accessDenied` (R19): as origens possíveis são da máquina, não do commit — artefato
  de outra identidade (causa 2 do `AGENTS.md`) ou `artifacts/` sem permissão de escrita, pasta
  ignorada pelo Git. Como a causa 2 é persistente, a mensagem diz «pare e reporte; repetir não
  resolve» e aponta a seção «Build local da extensão» do `AGENTS.md`.
- «timed out» e «proxy» **não** estão em nenhuma regra: aparecem em mensagens de compilação e de
  cópia comuns e, sozinhos, não distinguem ambiente de commit.
- O kind vai em `evidence.kind` de todo check novo que não saia `passed` (inclusive os motivos de
  `skipped`: `satelliteRefsAbsent`, `canonicalRestoreFailed`, `canonicalBuildFailed`; e os de
  contrato e de estado das refs: `propsContractViolation`, `satelliteRefsIncomplete`; e os da
  paridade: `parityMismatch`, `projectMissing` quando falta um dos dois `.csproj`, `projectsAbsent`
  no `skipped` com os dois ausentes, `unreadableOutput`); as asserções do meta-teste
  usam esse campo, nunca o texto do `summary`.
- **Mensagens** citam documentos e decisões por nome (D44, seção «Build local da extensão» do
  `AGENTS.md`), nunca por caminho absoluto: o meta-teste reprova o fonte do checker que contenha
  caminho de Program Files, `C:\GxModels` ou `Start-Process` com DLL — e passa a varrer também o
  módulo novo (seção 4.7).
- A classificação do canônico (`dotnet.restore`/`dotnet.build`) **não** muda neste item.

### 4.5 Ordem e dependência entre os checks

O check de `#if` só lê um arquivo e roda sempre, antes do bloco `dotnet`. O bloco `dotnet` passa a
ser: `dotnet.restore` → `dotnet.build` (canônico) → `msbuild.compileSetParity` →
`dotnet.buildSatellite`.

| Situação | Paridade | Satélite |
|---|---|---|
| restore canônico `failed`/`environmentBlocked` | **roda** (só avalia; se a avaliação falhar, `environmentBlocked` pela seção 4.4) | `skipped` (`canonicalRestoreFailed`) |
| build canônico `failed`/`environmentBlocked` | roda | `skipped` (`canonicalBuildFailed`) |
| paridade `failed` | — | **roda** (mede outra coisa) |
| contrato do `.props` violado (seção 4.2) — avaliado antes das linhas acima | roda | `failed`, sem compilar |
| tudo ok | roda | roda |

Precedência no satélite: **contrato** do `.props` → **falha canônica** (`skipped`) → **estado das
refs** (`absent`/`incomplete`) → build. Assim, refs incompletas com o build canônico falho saem
`skipped` (`canonicalBuildFailed`), e o `environmentBlocked` das refs não mascara o defeito do
commit, que já aparece como `failed` no `dotnet.build`.

`satelliteRefs` é informado em **todos** os casos (seção 4.2). A primeira linha substitui a regra
da T8 (que pulava a paridade com restore falho), pela R9.

### 4.6 JSON

- Campo de topo `satelliteRefs`: `absent` | `unverifiable` | `incomplete` | `complete`. O campo
  **nasce** no `B129`: o `absent|present` da D45 só existia no papel e nunca teve emissor nem
  consumidor. É inicializado com `$null` antes do bloco principal (`Set-StrictMode`) e emitido em
  todos os caminhos; `null` significa que o check não chegou a rodar (falha do próprio checker,
  `checker.execution`).
- `commands[]` com as chamadas novas, sanitizadas como as demais.
- `notCovered` ganha:
  - «Sem `Src/Lib/Gx18u13`, a compilação da DLL satélite U13 não é verificada (check `skipped`).»
  - «`satelliteRefs=complete` indica referências pinadas presentes, não a identidade U13 delas;
    proveniência fica com a D31 no corte.»
  - «A paridade compara itens `Compile` avaliados, só no primeiro alvo de cada projeto; não cobre
    targets de geração, flags de compilação, recurso `.package`, carimbo `GxLine` nem
    `PackageCompatibility`.»
  - «Em máquina sem o targeting pack `net471`, o restore do satélite pode depender do feed.»
  - «`-nodeReuse:false` não encerra o `VBCSCompiler`; só `dotnet build-server shutdown` cobre os
    dois processos da causa 1 do `AGENTS.md`, e o checker não o executa.»
  - «Só o `Lib.Gx18u13.References.props` é validado; `Reference` declarado direto no csproj
    satélite não passa pelo contrato D34.»
  - «Na fixture do meta-teste, o multi-alvo do canônico vem de `TargetFrameworks` declarado, não do
    SDK GeneXus.»
  - «A guarda de `#if` lê por linha e não distingue comentário nem string.»
  - «Divergência de comportamento do SDK U13 em runtime não é coberta por gate de build.»

### 4.7 Meta-teste

**Estrutura da fixture** (criada antes do primeiro commit, junto com o que já existe), nos caminhos
relativos de produção:

- `Directory.Build.props` na raiz, contendo **só** o bloco condicional da D47, copiado **inteiro**
  do de produção (as sete propriedades sob
  `MSBuildProjectName == GenexusOpenApiBuilder.Extension.Gx18u13`: lockfile desligado, raiz
  `artifacts\gx18u13\`, `NuGetLockFilePath`, `BaseOutputPath`, `BaseIntermediateOutputPath`,
  `OutputPath` e `IntermediateOutputPath`). Nada do grupo incondicional de produção
  (`RestorePackagesWithLockFile=true`, `GeneXusPackageReferenceVersion`), que mudaria o restore
  `--locked-mode` da solution canônica da fixture. Sem o bloco, os dois projetos da mesma pasta
  disputariam `obj/project.assets.json`.
- `Src\Domain\` com um `.cs`, incluído pelos dois projetos por `..\Domain\**\*.cs`, como em
  produção.
- `Src\Extension\GenexusOpenApiBuilder.Extension.csproj` (canônico da fixture): `Microsoft.NET.Sdk`,
  itens default, `<TargetFrameworks>net10.0</TargetFrameworks>` (plural, exercita o multi-alvo da
  seção 3.3), `Include` de `..\Domain\**\*.cs`, `Compile Remove` de `Temp\**\*.cs` (igual ao de
  produção) e de `Only.Sat\**`. Fica **fora** da solution canônica da fixture, que continua
  compilando só `Src\Fixture`; a paridade o lê pelo caminho.
- `Src\Extension\Compile.Shared.props` da fixture: `Include="**\*.cs"` com
  `Exclude="bin\**;obj\**;Temp\**;Line.*\**;Only.Sat\**"`, mais `Include` de `..\Domain\**\*.cs`.
- `Src\Extension\GenexusOpenApiBuilder.Extension.Gx18u13.csproj` (satélite da fixture):
  `Microsoft.NET.Sdk`, `net10.0`, `EnableDefaultCompileItems=false`, importa o props compartilhado
  e `Lib.Gx18u13.References.props`, e inclui explicitamente `Only.Sat\**\*.cs` e
  `Line.Gx18u13\**\*.cs` (conjuntos disjuntos do glob compartilhado); um target opcional, ativo só se existir o arquivo
  `Src\Extension\B129.warnings.flag`, emite os grupos de aviso do caso «satélite presente».
- `Src\Extension\Lib.Gx18u13.References.props` da fixture, com namespace MSBuild e um único
  `Reference` com `HintPath` `..\Lib\Gx18u13\FakeRef.dll` e `Private=false`. A entrada sem `HintPath` fica só nos
  casos de função, para que a build `net10.0` da fixture não ganhe aviso de resolução de
  referência de framework.
- `Src\GenexusOpenApiBuilder.Gx18u13.sln` com só o satélite.
- `Src\Extension\Package.cs` mínimo, sem `#if`.
- `.gitignore` da fixture acrescido de `artifacts/gx18u13/` e `Src/Lib/Gx18u13/`.
- `FakeRef.dll`: classlib compilada pela fixture **fora** do repositório da fixture, num diretório
  irmão **dentro** da raiz temporária do meta-teste (apagada pelo `finally` que já existe), e
  copiada para `Src\Lib\Gx18u13\` só nos casos que pedem refs presentes. Ciclo: copiada em
  «satélite presente», mantida em «satélite quebrado», apagada em «refs incompletas».
- **Commit inicial:** todos os arquivos novos acima entram no primeiro commit da fixture —
  inclusive o `Directory.Build.props` da raiz, que o `git add` atual (`.gitignore README.md Src
  scripts Tests`) não pega. Arquivo novo não rastreado deixaria a base `incomplete`.

**Sequência.** A execução base é a primeira execução que o meta-teste já faz: passa a ter as
asserções novas. Os quatro cenários novos entram **depois** de toda a bateria existente. Cada um
aplica a mudança, **commita** (alteração não commitada deixaria o checker `incomplete` e mascararia
o exit), roda o checker e desfaz com `git revert --no-edit` + push antes do cenário seguinte — o
revert é padrão novo, adotado aqui; a bateria existente só acumula commits. **Cenário sem mudança
rastreada** («refs incompletas», que só apaga um arquivo ignorado) **não** faz commit nem revert.
Todo `git commit`, `git revert` e `git push` novo confere o código de saída
(`Assert-True ($LASTEXITCODE -eq 0)`), ao contrário do padrão atual, que o descarta. Arquivos
ignorados (`Src/Lib/Gx18u13`, `artifacts/`) são criados e apagados explicitamente.

| Execução do checker | Estado da fixture | Exit | Asserções |
|---|---|---|---|
| base (existente) | pares iguais, `Package.cs` limpo, sem `Src/Lib/Gx18u13` | 0 | `git.statusPre` `passed`; paridade e `#if` `passed`; paridade com `evidence.targetFrameworks` = `net10.0` nos dois projetos (prova de que a descoberta do alvo rodou: sem ela o canônico da fixture devolveria 0 itens, seção 3.7); satélite `skipped` com `evidence.kind=satelliteRefsAbsent`; `satelliteRefs=absent`; `warnings[]` vazio |
| defeitos de fonte | commit com `.cs` em `Only.Sat/`, `.cs` em `Line.Gx18u13/` e `#if` no `Package.cs` | 1 | paridade `failed` com o arquivo de `Only.Sat/` em `onlySatellite[]` e o de `Line.Gx18u13/` em `forbiddenInCanonical[]`, sem `duplicates[]` (a exclusão com curinga funciona); `#if` `failed` |
| satélite presente | `FakeRef.dll` copiada; commit com `B129.warnings.flag`; o target emite, em ordem: 1 linha `warning MSB3277:` órfã, grupo `mscorlib` (cabeçalho em inglês + 2 linhas de continuação sem o nome), grupo de outro assembly (cabeçalho em português + 1 continuação), num target `BeforeTargets="CoreCompile"`; a saída repete tudo depois de «Build succeeded» (medido, seção 3.7) | 0 | satélite `passed`; `satelliteRefs=complete`; `knownWarnings` com **2** grupos, todos `mscorlib`; em `warnings[]`, com `satellite-build:`, o grupo do outro assembly e a linha órfã, **uma vez cada** (deduplicados), com 2 ocorrências em `evidence`; `headerLanguage=mixed` |
| satélite quebrado | `FakeRef.dll` presente; commit com `.cs` inválido em `Only.Sat/` | 1 | satélite `failed`; a paridade também falha (arquivo só no satélite) — asserção sobre o status de cada check, não só o exit |
| refs incompletas | `FakeRef.dll` apagada, pasta mantida | 2 | satélite `environmentBlocked` com `evidence.kind=satelliteRefsIncomplete`; `satelliteRefs=incomplete` |

**Casos de função** (carregam `scripts/B129-SatelliteChecks.ps1` por dot-source, sem execução do
checker):

- paridade: conjuntos iguais; `onlyCanonical`/`onlySatellite`; `Line.Gx18u13` no canônico (regra 1);
  `Line.Gx18u14plus` no satélite (regra 2); arquivo duplicado (regra 4); saída JSON ilegível;
  `Items.Compile` vazio e ausente;
- contrato do `.props`: namespace; entrada sem `HintPath`; `.props` ausente; nenhum `HintPath`;
  `Reference` sem `HintPath` fora da lista fechada; `Reference` com `HintPath` sem `Private=false`;
  `Private=false` como atributo e como elemento, em caixas diferentes; XML malformado (→ `failed`, sem
  exceção; `satelliteRefs=unverifiable` com a pasta presente e `absent` sem ela);
  `HintPath` absoluto; `HintPath` com curinga; `HintPath` fora de `Src/Lib/Gx18u13` (inclusive por
  `..` e por diferença de caixa); os quatro valores de `satelliteRefs`, inclusive `unverifiable` e `absent` com contrato
  violado;
- avisos: os grupos do caso «satélite presente» em texto, mais só `mscorlib`, só órfão, cabeçalho
  intercalado e continuação separada do cabeçalho por outra linha (tem de virar órfã, não
  conhecida); aviso sem código (`warning : …`), que vai para `warnings[]`;
- avisos: também a saída real medida (dois blocos `mscorlib` separados por linhas de outro tipo),
  que tem de resultar em dois grupos conhecidos e nada em `warnings[]`;
- falhas — uma linha de caso por regra da tabela da seção 4.4, mais: `MSB4019` com caminho não
  extraível; acesso negado em inglês e português (inclusive vindo com `MSB3021`, e com `MSB3026` +
  `MSB3027`, que tem de dar `accessDenied`, não `fileLocked`); `MSB3021` sem arquivo em uso
  (→ `failed`); `NETSDK1045` (→ ambiente) × `NETSDK1013` (→ `failed`); `NU1301` (→ ambiente) ×
  `NU1101` (→ `failed`); erro `CS*` e `MSB3030` cujo texto contém «Proxy» ou «timed out» (→
  `failed`); bloco `MSB3277` com nome de assembly que contém «proxy», que não pode influenciar a
  classificação, porque avisos `MSB3277` não entram na entrada da seção 4.4.

**Asserções estáticas novas:** o checker referencia `GenexusOpenApiBuilder.Gx18u13.sln` e carrega
`scripts/B129-SatelliteChecks.ps1`; nem o checker nem o módulo contêm `LoadFile`, `LoadFrom`,
`Add-Type -Path` ou `[Reflection.Assembly]` — a trava garante que nem o checker nem o módulo
carregam assembly, que é exatamente o que a D55 afirma; as proibições atuais (`Tools\`
invocado, caminho de Program Files ou `C:\GxModels`, `Start-Process` com IDE ou DLL, invocação
dinâmica) passam a valer também para o módulo.

**Custo:** quatro execuções novas do checker, mais quatro invocações `dotnet msbuild` da paridade
somadas a cada execução existente. Sem estimativa: o tempo é medido e registrado na seção 11.

### 4.8 `Src/Extension/Compile.Shared.props`

`Exclude="bin\**;obj\**"` → `Exclude="bin\**;obj\**;Temp\**;Line.*\**"`. Afeta só o satélite (D46).
A DLL satélite atual não muda (as duas pastas não têm `.cs`). A exclusão de `Line.*\**` evita que o
glob compartilhado e o `Include` explícito de `Line.Gx18u13` do csproj satélite peguem o mesmo
arquivo quando a pasta nascer, e aproxima o props do esqueleto D36 (invariante 5), que já exclui
`Line.*`. O restante do alinhamento ao D36 (enumerar `Diagnostics\**` e a raiz em vez de `**\*.cs`)
não é necessário para o `B129` e fica registrado na D55.

### 4.9 Módulo `scripts/B129-SatelliteChecks.ps1`

Funções puras, sem efeito colateral e sem invocar processo: normalização e regras da paridade;
leitura e validação do contrato do `.props`; agrupamento e classificação dos avisos; classificação
das falhas da seção 4.4. O checker as carrega por dot-source (padrão do
`scripts/B128-ReferenceTokenizer.ps1`) e o meta-teste também, copiando o módulo para a fixture como
já faz com o tokenizer. O check `powershell.parse` já cobre o arquivo, por estar em `scripts/`.

---

## 5. Documentação na mesma frente

Nenhuma remissão cita linha de arquivo `.cs` por número (regra B128); usar símbolo ou tipo.

- **Plano da Opção B:** nova linha **D55** («D45 emendada pelo `B129` em <data>»): pré-push roda
  paridade sempre e build satélite quando a pasta de refs existe; ausente = `skipped` fora de
  `warnings[]`; incompleta = `environmentBlocked` (com a justificativa da D-B129-2); compilar contra
  `Src/Lib/Gx18u13` é leitura de referência pelo MSBuild, não «invocar DLL»; «mesma régua em
  qualquer clone, cobertura declarada no JSON» (afrouxamento travado por asserção estática, que
  garante exatamente isto: nem o checker nem o módulo carregam assembly); o campo `satelliteRefs` **nasce** no `B129` com
  `absent|unverifiable|incomplete|complete` — o `absent|present` da D45 só existia no papel, sem emissor nem
  consumidor; `complete` não prova identidade U13; o `.props` de referências é validado sempre
  (contrato D34); `Compile.Shared.props` passa a excluir `Temp\**` e `Line.*\**`, e o restante do
  alinhamento ao esqueleto D36 (hoje o props usa `**\*.cs`, divergente do texto da invariante 5)
  fica para quando `Line.*` existir.
  Remissões datadas, sem reescrever: linha da D45; invariante 13; §2 (não-objetivo); risco «Warning
  de Lib quebra o meta-teste» do §10; D15 e invariante 10 («guarda implementada pelo `B129`»);
  lista da Fase 2 (§8, «checker `#if`»); critério do §12 («checker `#if`» e «pré-push sem warning
  por `Lib/` ausente»); linha «Release U13 (mantenedor)» do §6.5 («build satélite … **não** o
  checker pré-push» — continua verdadeira para o asset de release, que sai da build a partir da
  tag; o pré-push passa a compilar o satélite como verificação, não como origem do asset); §1,
  §1.2 e a linha «Release U14+» do §6.5 («CI/build oficial» — **declarado
  pelo mantenedor em 2026-09-28**: a DLL canônica do release é compilada na máquina dele, do código
  da tag; o workflow só reempacota o asset publicado).
- **`AGENTS.md`**, «Revisão pré-push do repositório»: o relatório declara o status de
  `dotnet.buildSatellite` e o `satelliteRefs`; `skipped` por `satelliteRefsAbsent` **na máquina do
  mantenedor** é anomalia a reportar; o passo 6 (rodar o meta-teste quando o checker ou o teste
  mudam) passa a incluir `scripts/B129-SatelliteChecks.ps1`. «Build local da extensão»: o pré-push também compila o
  satélite em `artifacts/gx18u13/`, reescrevendo a DLL e o asset local — o asset de release sai
  sempre da build a partir da tag, no corte.
- **Backlog (06):** fechar o `B129` com remissão; nota operacional fica como registro.
- **Checkpoint:** promover o `B129` a próxima ação única ao iniciar a implementação; item numerado
  de fechamento.
- **`CHANGELOG.md` `[Unreleased]`:**
  - `Added` — três checks novos no pré-push; não altera a extensão;
  - `Fixed` — `Compile.Shared.props` do satélite não excluía `Src/Extension/Temp/`; defeito latente.
    Antes de escrever se atingiu ou não versão publicada, conferir contra as tags (regra do
    `AGENTS.md`): o canônico passou a excluir `Temp` em `f2967cc`; verificar por `git ls-tree` se
    alguma tag posterior contém `.cs` em `Src/Extension/Temp/`. Se nenhuma contiver, a entrada diz
    que o defeito nunca chegou a DLL publicada. A exclusão de `Line.*\**` é preventiva (pasta
    inexistente) e não entra como `Fixed`.

---

## 6. Fases restantes

0. **Promoção e checkpoint:** promover este plano a
   `Docs/Implementation/<data>-B129-EMENDA-D45-COBERTURA-SATELITE.md` e o `B129` a próxima ação
   única no checkpoint, com autorização do mantenedor.
1. **Implementação:** módulo `scripts/B129-SatelliteChecks.ps1`; três checks, `satelliteRefs` e
   `notCovered` no orquestrador; meta-teste 4.7. Parse dos scripts e revisão contra as armadilhas
   de StrictMode do `AGENTS.md` global. **Ainda sem** a correção do `Compile.Shared.props`.
2. **Sentinela — diagnóstico intermediário, com a árvore suja pela implementação:**
   1. criar um `.cs` compilável mínimo em `Src/Extension/Temp/` e rodar o orquestrador: o check
      `msbuild.compileSetParity` sai `failed`, com o sentinela em `onlySatellite[]` (status global
      `failed`, exit 1);
   2. aplicar a correção do `Compile.Shared.props` (seção 4.8) e rodar de novo: o check sai
      `passed` (status global `incomplete`, exit 3, por causa da árvore suja);
   3. apagar o sentinela.

   Nas duas execuções o critério lê o status **do check**, não o global. Elas não são a rotina
   pré-push, que o `AGENTS.md` só aceita sobre a frente commitada.
3. **Meta-teste** verde e cronometrado (ele monta a própria fixture e não depende da árvore do
   repositório).
4. **Commit local da implementação** em `main`: módulo, checker, meta-teste e
   `Compile.Shared.props`.
5. **Validação sobre a árvore limpa:**
   - orquestrador no repositório real — critério 2 da seção 8;
   - build satélite com fonte alterada (mudar só a data de um `.cs`), medida;
   - idioma: conferir em `evidence.headerLanguage` se o `DOTNET_CLI_UI_LANGUAGE=en` foi obedecido;
   - o caso «sem refs» fica provado pela fixture; não renomear a pasta do mantenedor.
6. **Documentação** (seção 5), varredura (seção 9) e seção 11 preenchida; segundo commit local.
7. **Pré-push** da frente sobre os dois commits (incluindo o meta-teste, porque o checker mudou) e
   push só com autorização.

---

## 7. Riscos

| Risco | Mitigação |
|---|---|
| Meta-teste mais lento | medido e registrado; matriz sem fusão |
| `MSB3277` novo mascarado | agrupamento por diagnóstico que termina na primeira linha de outro tipo; órfão e intercalado caem em `warnings[]`; idioma forçado pela variável, com cabeçalho bilíngue de reserva; casos negativos no meta-teste |
| Sessão do Codex bate na causa 2 em `artifacts/gx18u13` | acesso negado sai `environmentBlocked`/`accessDenied` com «pare e reporte» e evidência, para o humano decidir (`B118`) |
| Veredito diferente entre clones com e sem refs | assumido na D55; `satelliteRefs` e `notCovered` declaram a cobertura |
| Falsa confiança sobre runtime ou identidade U13 | `notCovered`; D31 e §6.5.1 da Opção B continuam valendo |
| Quando `Line.Gx18u13` nascer, a regra 1 reprova o canônico e o pré-push bloqueia a frente que criar a pasta | é o desenho pretendido (D46): a mesma frente precisa passar o canônico a `EnableDefaultCompileItems=false` com o props compartilhado; a mensagem do check cita a D46 |
| Avaliação MSBuild muda com outro SDK | `MSB4236` e `MSB4019` fora do repositório → `environmentBlocked`; saída ilegível → `failed` com a saída bruta em `evidence` |

---

## 8. Critérios de aceite

1. Meta-teste verde com as cinco execuções e os casos de função da seção 4.7, tempo registrado.
2. Orquestrador no repositório real: nenhum check `failed`; os três checks novos `passed`;
   `satelliteRefs=complete`; `knownWarnings` só com grupos `mscorlib` (nesta máquina, dois; numa
   máquina sem o conflito, `headerLanguage=none` e zero grupos também é o resultado correto);
   **nenhuma** entrada `satellite-build:` em `warnings[]` — se aparecer alguma, a frente só fecha
   com a origem e a decisão (corrigir ou aceitar com justificativa) registradas na seção 11;
   `git.statusPost` limpo. O `headerLanguage` observado vai para a seção 11. Avisos `evidence-doc-required:*` são esperados nesta frente (entrada `Fixed` no
   `CHANGELOG` e promoção no checkpoint), não bloqueiam push e são tratados pela seção 11.
3. Sentinela: pelo próprio check, paridade `failed` antes da correção do `Compile.Shared.props` e
   `passed` depois (seção 6, passo 2).
4. D55 e remissões, `AGENTS.md`, backlog, checkpoint e `CHANGELOG` coerentes; seção 11 preenchida.
5. Pré-push da frente sem impedimento.

---

## 9. Varredura de fechamento

Radicais: `D45`, `D15`, `só canônico`, `apenas` + `GenexusOpenApiBuilder.sln`, `satelliteRefs`,
`absent|present`, `à mão`, `fora do orquestrador`, `não a compila`, `Não colocar build do satélite`,
`não o checker pré-push`,
checker + `#if`, `CI/build oficial`, `B129`, `MSB3277`, `Compile.Shared`, `Temp\**`, e termos de
estado (`pendente`, `falta`, `ainda não`). Ocorrência histórica datada fica; afirmação operacional
vencida é atualizada.

---

## 10. Arquivos

| Arquivo | Mudança |
|---|---|
| `Src/Extension/Compile.Shared.props` | `Temp\**` e `Line.*\**` no `Exclude` |
| `scripts/B129-SatelliteChecks.ps1` (novo) | funções puras da seção 4.9 |
| `scripts/Invoke-PrePushMechanicalChecks.ps1` | três checks, `satelliteRefs`, `notCovered`, ambiente opcional em `Invoke-ExternalProcess` |
| `Tests/PrePushChecker/Test-OpenApiBuilderPrePushChecks.ps1` | matriz da seção 4.7 |
| `Docs/Decisions/2026-08-12-PLANO_SUPORTE_PARALELO_GX18U13_OPCAO_B.md` | D55 e remissões |
| `AGENTS.md` | três mudanças (seção 5) |
| `Docs/Foundation/06-BACKLOG_v0.1.md` | fechamento do `B129` |
| `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md` | promoção e fechamento |
| `CHANGELOG.md` | `[Unreleased]` → `Added` e `Fixed` |
| `Docs/Implementation/<data>-B129-EMENDA-D45-COBERTURA-SATELITE.md` | este plano, promovido |

Não editar nem versionar: csproj canônico, csproj satélite, `Directory.Build.props`, `Tools/`,
`Src/Lib/Gx18u13`, `C:\Program Files (x86)\GeneXus`. `artifacts/gx18u13/` também não é editado
nem versionado à mão, mas **é reescrito** pela build satélite do pré-push, por desenho (D-B129-3).

---

## 11. Execução e evidência

Executado em 2026-09-28, numa sessão do Claude Code (Opus 5.5), com autorização do mantenedor
na própria sessão: «se não achar nada contra, execute o plano». Medições da máquina do mantenedor,
com `Src/Lib/Gx18u13` presente; a Fase 0 está na seção 3.

### 11.1 Conferência prévia

Nada contra a execução foi encontrado: a próxima ação do checkpoint era escolher a próxima frente;
o `B129` constava do backlog; `main` coincidia com `origin/main`; o checker, o meta-teste, os dois
`.csproj`, o `Compile.Shared.props`, o `.props` de referências e o `Directory.Build.props`
coincidiam com o que as seções 1 a 4 descrevem. De passagem, o checkpoint ainda dizia que o push
de `B109`/`B110` aguardava autorização, mas os commits já estavam em `origin/main`; a frase foi
marcada como superada.

### 11.2 Commits

- `46ce082` — módulo `scripts/B129-SatelliteChecks.ps1`, três checks e `satelliteRefs` no
  orquestrador, meta-teste, correção do `Compile.Shared.props`, este documento promovido e o
  `B129` escolhido no checkpoint;
- commit seguinte — D55 e remissões, `AGENTS.md`, backlog, checkpoint, `CHANGELOG.md` e esta
  seção.

### 11.3 Tempos

| Medida | Antes (seção 3.1) | Depois |
|---|---|---|
| Orquestrador completo, árvore limpa, com o satélite compilado | 74,7 s | 79 s (79 checks) |
| Meta-teste (`Test-OpenApiBuilderPrePushChecks.ps1`) | 481 s | 697 s, verde na primeira execução |
| Build satélite dentro do orquestrador, sem mudança de fonte (`durationMs`) | — | 1,3 s |
| Build satélite com fonte alterada (só a data do `Package.cs`), mesmos parâmetros | 4,3 s | 2,0 s |

A primeira execução com o sentinela levou 348 s, dos quais 55 s na build satélite, que recompilou
tudo; a segunda, 106 s. A diferença não foi investigada: pode ser recompilação depois da troca de
DLL e das builds do meta-teste, mas isso não foi medido. O restore do satélite não foi remedido —
vale a medição da seção 3.7 (`libraries` vazio, sem acesso ao feed).

### 11.4 Sentinela (critério 3)

`Src/Extension/Temp/B129Sentinel.cs`, compilável, criado com a implementação ainda não
commitada:

- **antes** da correção do `Compile.Shared.props`: `msbuild.compileSetParity` = `failed`, kind
  `parityMismatch`, canônico 104 × satélite 105, `onlySatellite` =
  `Extension/Temp/B129Sentinel.cs`; status global `failed`, exit 1;
- **depois** da correção: `passed`, 104 × 104; status global `incomplete`, exit 3, pela árvore
  suja.

O sentinela foi apagado em seguida. As duas execuções são diagnóstico intermediário, não a rotina
pré-push.

### 11.5 Orquestrador no repositório real (critério 2)

Sobre `46ce082`, com a árvore limpa (os documentos desta frente estavam guardados em `git stash`):

- `msbuild.compileSetParity` = `passed`: alvo `net471` nos dois projetos, 104 × 104 itens;
- `dotnet.buildSatellite` = `passed`: `satelliteRefs=complete`, nove referências esperadas e
  nenhuma faltando; `knownWarnings` com 2 grupos, 2 cabeçalhos e 124 linhas, todos `mscorlib`;
  `headerLanguage=en`, isto é, o `DOTNET_CLI_UI_LANGUAGE=en` foi obedecido; nenhuma entrada
  `satellite-build:` em `warnings[]`;
- `source.packageNoIfDirective` = `passed`;
- `git.statusPost` = `passed`; `warnings[]` só com `evidence-doc-required:checkpoint`, esperado.

Um check saiu `failed`, e não era dos novos: `git.diffInterval`, por espaço em branco no fim de
duas linhas deste documento, herdado do texto em `Temp/`. A correção entrou no commit seguinte, e
o critério 2 fecha com a execução do pré-push da frente sobre os dois commits (seção 11.8).

### 11.6 Conferência das tags para o `Fixed`

O canônico passou a excluir `Temp\**\*.cs` em `f2967cc` (2026-09-13); o `Compile.Shared.props`
nasceu em `711f086` (2026-08-12) e está em todas as tags desde `v0.1.0-alpha.2`. Nenhuma tag, de
`v0.1.0-alpha.1` a `v0.1.0-alpha.9`, contém `.cs` em `Src/Extension/Temp/` (`git ls-tree`), e
nenhum commit de nenhum ramo jamais versionou arquivo ali (`git log --all`). Como o asset sai da
build a partir da tag (D31), o defeito nunca chegou a DLL publicada.

### 11.7 Desvios de implementação em relação ao texto da v8

1. **Regra 6 dos avisos.** O padrão implementado é `: warning( [A-Z]+[0-9]+)? ?:`, não
   `: warning( [A-Z]+[0-9]+)?:`: o MSBuild escreve aviso sem código como
   `arquivo : warning : texto`, com espaço antes dos dois-pontos, e o padrão literal não pegaria
   justamente o caso que a B7 quis cobrir. Coberto por caso de função.
2. **`evidence.kind` do check de `#if`.** A lista fechada da seção 4.4 não trazia os motivos
   desse check; a regra «kind em todo check novo que não saia `passed`» exigiu dois:
   `ifDirectiveFound` e `packageSourceMissing`.
3. **Falha da avaliação da paridade.** Com código de saída diferente de zero, vale direto o kind
   da tabela da seção 4.4 — a regra 9 casa sempre que há linha de erro, e sem linha de erro o kind
   é `unclassified` —; `unreadableOutput` fica para saída zero com JSON ilegível.
4. **`commands[]` da paridade.** Em sucesso, o stdout do `-getItem` (cerca de 98 KB de JSON por
   projeto) não é copiado; fica só o stderr. Em falha, a saída entra inteira.
5. **Linhas órfãs consecutivas** formam um só grupo órfão, e portanto uma entrada em `warnings[]`.
6. **`knownWarnings`** traz `groups`, `headers`, `continuationLines` e `totalLines`; o check
   satélite registra também `warningOccurrences` (deduplicação) e `unknownWarningGroups`.
7. **Ordem dos commits.** A promoção deste documento e a escolha no checkpoint (passo 0) entraram
   no commit da implementação, para que a validação do passo 5 rodasse sobre árvore limpa; a D55 e
   o `AGENTS.md`, redigidos antes, ficaram de fora por `git stash` durante a validação.
8. **Duas redações** do corpo foram trocadas na promoção para não formar citação móvel de linha
   C# (B128): a da Y2 e a da medição de multi-alvo da seção 3.7.

### 11.8 Pré-push da frente, cobertura e revisão

- O pré-push da frente roda sobre os dois commits, com o meta-teste repetido porque o checker
  mudou; o resultado vai no relatório da sessão e não é reescrito aqui, para não mudar o intervalo
  revisado.
- **Revisão por pares:** nenhuma rodada nesta execução. Os recibos das consultas de desenho
  (rodadas `B129-20260928-v1` a `v4` e os pareceres da v5 à v7) estão em
  `Temp/revisao-por-pares/B129-*`, ignorado pelo Git.
- **Fora de cobertura** — além do `notCovered` do JSON (seção 4.6): o caso «sem refs» está provado
  só pela fixture, e a pasta do mantenedor não foi renomeada; a identidade U13 das referências não
  é verificada; nenhuma validação na IDE, porque a extensão não muda — `Temp/` e `Line.*` não têm
  `.cs`, e a DLL satélite reescrita pelo orquestrador em `artifacts/gx18u13/` sai do mesmo
  código.

---

## 12. Questões abertas para decisão humana

1. ~~**R19 — acesso negado no satélite.**~~ **Decidido em 2026-09-28:** `environmentBlocked`,
   kind `accessDenied` (seção 4.4).
2. ~~**Processo da revisão.**~~ **Decidido em 2026-09-28:** a v3 foi à rodada
   `B129-20260928-v3` e a v4 à rodada `B129-20260928-v4`, ambas só com os revisores do opencode
   (DeepSeek e Meta), por decisão do mantenedor. A v5 foi a uma segunda opinião (subagente Opus e
   Space Bunny Alpha), a v6 a um parecer solo do GPT 6 Sol e a v7 a um subagente nativo, sempre por
   decisão dele. **Em 2026-09-28 o mantenedor declarou a v8 a última versão**: design congelado,
   execução em nova sessão, sem nova consulta. O mantenedor declarou que a v4 deve refletir o
   juízo do autor, não a soma das opiniões do painel; os pareceres são insumo, e cada ponto é
   aceito ou recusado com motivo (seção 2.4).
