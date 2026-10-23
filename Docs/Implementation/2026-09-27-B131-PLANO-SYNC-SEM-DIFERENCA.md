# B131 — Plano: relatório do `Sincronizar` sem diferenças

- **Data do plano:** 2026-09-27
- **Item:** `B131` (`Docs/Foundation/06-BACKLOG_v0.1.md`, linha da tabela e «Nota operacional — B131»)
- **Versão:** v6, de 2026-10-22 — **versão congelada para execução**. Depois de cinco rodadas do
  painel de revisão por pares, sem nenhuma voz reabrindo o desenho, o mantenedor decidiu congelar
  o plano e executá-lo em sessão nova: a prova passa ao código, ao gate e à validação na IDE. Isso
  **não** é convergência do painel: a v6 não foi submetida a ele (seção 14). Redação delegada pelo
  mantenedor; precisão da D6 ratificada por ele (seção 5).
- **Estado:** decisões D1–D7 tomadas pelo mantenedor em 2026-09-28 (seção 5); implementado em
  2026-10-22, aguardando validação na IDE. No Commit B o campo passa a «executado em <data>»
  (seção 9, item 10).
- **Local:** redigido em `Temp/` em 2026-09-27 e movido para `Docs/Implementation/` em 2026-09-28
  por decisão D7 (regra do `AGENTS.md` sobre `Temp/`).
- **Conferência de deriva em 2026-09-28:** entre `f043416` (base do plano) e `d570163` o
  `Package.cs` mudou em outras frentes, mas o ramo sem diferença do Sync e o `previewWatch`
  seguem como descritos na seção 3.2. Reconferido em 2026-10-22: `main` e `origin/main` continuam
  em `d570163`, sem commit novo, release ou issue desde 2026-09-28. Antes de codar, refazer a
  conferência (`git fetch origin`, `git log origin/main --oneline -5`, `git tag`); havendo commit
  novo que toque `Package.cs` ou o relatório, reler a seção 3.2 antes de seguir.

---

## 1. Objetivo

Quando o `Sincronizar com a Transaction` não encontra diferença entre a Transaction e a metadata,
o relatório final (`B081`) deve:

1. sair com `Resultado='Success'`, e não `SuccessWithWarnings` — a ausência de diferença é o
   resultado esperado de uma sincronização em dia, não um risco;
2. registrar a ausência de diferença como **informação** (`Informações=1`, `Avisos=0`);
3. mostrar como duração o tempo da pré-visualização, que é a única fase medida do Sync nesse
   caminho, em vez de `DuraçãoMs=0`. Ficam fora da duração medida a escrita do resumo do diff na
   Output, a montagem do relatório em `ShowFinalReport` — inclusive a resolução do objeto
   principal por `TryResolveMainObjectFromKb` — e o tempo em que o diálogo fica aberto. No Sync
   com diferença, o cronômetro do relatório começa depois do diálogo de escolhas e da montagem do
   plano, e não inclui a pré-visualização; isso não muda com o `B131`.

Não muda nada na KB, no diário, na metadata nem no Sync com diferença.

## 2. Cobertura da leitura que sustenta o plano

Lido integralmente:

- `Src/Extension/Diagnostics/ApiPlanApplicationFinalReport.cs` (relatório, coletor, `ResolveOutcome`, `BuildReadableBody`, `BuildOutputSummary`);
- `Tests/ApplicationFinalReport/Test-ApiPlanApplicationFinalReport.ps1` (gate `tests.applicationFinalReport`);
- `README.md`.

Lido por trechos (arquivos grandes):

- `Src/Extension/Package.cs`: `ExecuteSynchronizeWithTransactionCore` do início até o `catch` externo,
  `ShowFinalReport`, o trecho de recuperação que usa `AddInformation` e os dois `catch` externos
  de Sync e Remover que também passam `TimeSpan.Zero`;
- `Docs/Foundation/06-BACKLOG_v0.1.md`: linhas `B080`–`B131` e as notas operacionais `B129`, `B131`, `B130`;
- `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`: linhas 1–230 (marcos, frente atual, próxima ação única)
  e o fim do log numerado por busca (itens 181–188); em 2026-10-22, por busca, as seções
  «Frente atual», «Evidência da frente encerrada», «Sequência operacional vigente» e «Protocolo de
  atualização»;
- `Docs/Foundation/15-TESTES_VALIDACAO_E_QUALIDADE.md`: §18.2 (régua do documento de evidência, `B124`);
- `Src/Domain/ExtensionOutputLocalization.cs`: `Translate` e as entradas dos textos envolvidos;
- `Tests/Localization/Test-ExtensionOutputLocalization.ps1`: asserção do título do Sync sem diferença.

Varreduras por busca no repositório inteiro: `noOpReport`, `TimeSpan.Zero`, `AddInformation(`,
`SuccessWithWarnings`, `Nenhuma sincronizacao`, `Nenhuma diferenca`, `sem diff`/`zero-diff`, consumidores
de `ApiPlanApplicationFinalOutcome` e de `.Outcome` em `Src/`, e menções ao caso em `README*`,
`CHANGELOG.md`, `Docs/Public/`, `Docs/Releases/` e `Docs/Foundation/`.

Não lido: o restante do log numerado do checkpoint (itens anteriores a 181 fora das linhas 1–230)
e o corpo do diálogo `ApiPlanApplicationFinalReportDialog` além da busca por `Outcome`,
`Information`, `Warning` e `Headline`. Nenhuma regra desses trechos foi encontrada pelas buscas
que afete o plano, mas a ausência é de busca, não de leitura.

## 3. Diagnóstico

### 3.1 Observado (registro de 2026-09-25, nota operacional do `B131`)

Na `Empresa` da `fabricabrasil18test`, logo depois de um Wizard, o `Sincronizar` não encontrou
diferença (221 campos inalterados). O relatório final saiu com `Resultado='SuccessWithWarnings'`,
`Avisos=1` («Nenhuma diferença entre Transaction e metadata…») e `DuraçãoMs=0`, enquanto a Output
registrava `Sync PreviewMs=4920`.

### 3.2 Lido no código (2026-09-27)

- No ramo sem diferença de `Package.ExecuteSynchronizeWithTransactionCore` (condição
  `!preview.Diff.HasDifferences && preview.SdtConflicts.Count == 0`), o relatório é montado com
  `noOpReport.HeadlineOverride = "Nenhuma sincronizacao necessaria."`,
  `noOpReport.AddWarning("Nenhuma diferenca entre Transaction e metadata. Nenhuma alteracao foi feita na KB.")`
  e exibido com `ShowFinalReport(noOpReport, TimeSpan.Zero, knowledgeBase.DesignModel)`.
- `ApiPlanApplicationFinalReportCollector.ResolveOutcome` devolve `SuccessWithWarnings` sempre que
  há pelo menos um aviso. Informação (`AddInformation`) não altera o resultado.
- O cronômetro `previewWatch` existe, mas é declarado **dentro** do bloco `using` da
  pré-visualização; o ramo sem diferença fica fora desse bloco e não o enxerga. Por isso a
  correção precisa elevar o valor medido para fora do bloco, e não só trocar o argumento.
- `ShowFinalReport` escreve cada aviso na Output como `[B081] Aviso: …`; informações só aparecem
  no diálogo e na contagem `Informações=N` do resumo. A mensagem de ausência de diferença já sai
  na Output pela linha `[B085] Nenhuma diferenca entre Transaction e metadata. …`, escrita antes
  do relatório.
- **Precedente:** a recuperação que conclui uma remoção retomada (`B125`) já registra o resumo
  final com `AddInformation`, e o gate `tests.applicationFinalReport` trava que informação não
  transforma o resultado em aviso.

### 3.3 Visibilidade do defeito (conferida contra a tag, não de memória)

- `git log -S 'noOpReport.AddWarning'` e `git log -S 'ShowFinalReport(noOpReport, TimeSpan.Zero'`
  apontam o mesmo commit de origem: `abb585b` (B081, relatório final pós-aplicação).
- `git merge-base --is-ancestor abb585b v0.1.0-alpha.1` → verdadeiro.
- Conclusão: o comportamento está em **todas as Alphas publicadas**, da `0.1.0-alpha.1` à
  `0.1.0-alpha.9`. A entrada do `CHANGELOG` vai em `Fixed`, dizendo que era visível em versão
  publicada.

## 4. Contrato afetado — produtores e consumidores

| Elemento | Produtor | Consumidores | Efeito da mudança |
|---|---|---|---|
| `Outcome` do relatório | `ResolveOutcome` no coletor | `BuildOutputSummary` (`Resultado=…`) e `ResolveHeadline` (título default) | Resumo passa a `Success`; o título não muda, porque este ramo usa `HeadlineOverride` |
| Lista de avisos | `AddWarning` | `BuildReadableBody` («Avisos (N)»), `BuildOutputSummary` (`Avisos=N`), `ShowFinalReport` (linha `[B081] Aviso:`) | Some o aviso; o corpo mostra «Avisos: (nenhum)» e a linha `[B081] Aviso:` deixa de sair |
| Lista de informações | `AddInformation` | `BuildReadableBody` («Informações (N)»), `BuildOutputSummary` (`Informações=N`) | Passa a conter a frase de ausência de diferença |
| Duração | argumento `elapsed` de `ShowFinalReport` | `BuildOutputSummary` (`DuraçãoMs`) e corpo («Tempo:») | Passa a ser o tempo da pré-visualização |

- Busca em `Src/` pelo tipo `ApiPlanApplicationFinalOutcome` e pela propriedade
  `ApiPlanApplicationFinalReport.Outcome`: **nenhum consumidor** fora do próprio relatório. (A
  busca textual por `.Outcome` também acha `queue.Outcome`, `receipt.Outcome` e outros, que são
  de outros tipos e não entram aqui.) O diálogo `ApiPlanApplicationFinalReportDialog` não lê
  `Outcome`.
- Objeto principal: com `apiPlan` nulo, `TryResolveMainObjectFromKb` procura o API Object pelo
  nome (`API.GetAll`) e, se achar, preenche `MainObjectName` e `MainObjectGuid`, o que habilita
  «Abrir objeto principal» no diálogo. É leitura, já acontece hoje e não muda com o `B131`.
- Diário (`GxOpenApiBuilder_OperationJournal`), metadata e KB: o ramo sem diferença retorna antes
  de abrir o diário e antes de qualquer gravação. **Risco de escrita parcial: nenhum** — o caminho
  não escreve.
- Documentos públicos (`README*`, `Docs/Public/`, `Docs/Releases/`): nenhum descreve o
  `SuccessWithWarnings` desse caso. A imagem pública `Docs/Images/alpha-relatorio-final.png`
  mostra um relatório de **Wizard**, não de Sync sem diferença — não é afetada.
- Gates: nenhum gate atual trava o aviso nem a duração desse ramo. O gate de localização
  (`Tests/Localization/Test-ExtensionOutputLocalization.ps1`) cobre apenas o **título** deste caso,
  que não muda; a frase «Nenhuma alteracao foi feita na KB.» aparece nele em outro caso (o `Voltar`
  do Wizard). O gate do relatório já tem casos de `Sincronizar`, mas nenhum do ramo sem diferença.

## 5. Decisões (tomadas pelo mantenedor em 2026-09-28, uma por vez)

| # | Questão | Decisão | Motivo |
|---|---|---|---|
| D1 | Classificar a ausência de diferença como informação (`Success`) em vez de aviso | **Sim** | É o resultado esperado de uma sincronização em dia; o precedente do `B125` já usa informação para desfecho normal |
| D2 | Qual duração o relatório mostra | **Tempo da pré-visualização** (`previewWatch`) | É a única fase executada, e o valor fica idêntico ao da linha `[B082] Sync PreviewMs=` da mesma execução, o que torna o aceite verificável número a número. O cronômetro do comando inteiro foi descartado: deixaria de bater com `PreviewMs` sem ganho para o leitor |
| D3 | Passar a escrever as informações na Output (`[B081] Informação: …`) | **Não** | Mudaria também a Output da recuperação, que é outra frente; a linha `[B085] Nenhuma diferenca …` já cobre o caso |
| D4 | Os dois `catch` externos (Sync e Remover) também passam `TimeSpan.Zero` e saem com `DuraçãoMs=0` em falha | **Fora do `B131`, registrado** | O enunciado é o caso sem diferença; nos `catch` o cronômetro está declarado dentro do `try` e a semântica é de falha, que merece decisão própria. Entra como observação no documento de evidência; a abertura de item de backlog é perguntada ao mantenedor no fechamento |
| D5 | Acentuar em pt-BR o título e a informação («sincronizacao», «diferenca», «alteracao») | **Não** | `Translate` devolve o texto original em pt-BR; corrigir mexe no catálogo e no gate de localização, e acentuar só estes textos criaria exceção isolada |
| D6 | Verificação na IDE: só a KB pequena ou também a `Empresa` da KB grande | **As duas** | A pequena prova o mecanismo; a grande reproduz o cenário do registro, com `PreviewMs` na casa dos segundos, e exercita a formatação da duração em segundos no diálogo |
| D7 | Promover este plano a `Docs/Implementation/` | **Sim** | Feito em 2026-09-28 |

**Precisão da D6, em 2026-10-22.** A decisão e o motivo acima ficam como foram tomados. A terceira
rodada do painel apontou que o roteiro da v3 aceitava, na KB grande, qualquer Transaction e
qualquer duração, o que não cumpria os dois motivos registrados da D6. Sob delegação do mantenedor
(«autore nova versão do plano como achar que deve»), o agente escolheu a forma que preserva os
dois motivos — a opção B apresentada na triagem —, aplicada na seção 8.2:

- o cenário 2 tenta primeiro a `Empresa`;
- vale a execução cujo `PreviewMs` fique entre 1 s, inclusive, e 60 s, exclusive
  (`1000 ≤ PreviewMs < 60000`): abaixo de 1 s, `FormatElapsed` escreve em ms; a partir de 60 s,
  em minutos — nos dois casos o diálogo não mostra a duração em segundos. Nos cerca de 50 ms
  anteriores a 60 s, o formato `{TotalSeconds:0.0}` pode arredondar para «60,0 s», que ainda é
  formato em segundos e é aceito;
- se a `Empresa` não estiver em condição ou ficar fora dessa faixa, outra Transaction da
  `fabricabrasil18test` vale nas mesmas condições. Nesse caso, a evidência declara que a sessão
  provou a duração em segundos numa KB grande, mas **não** reproduziu o caso registrado em
  2026-09-25 (a `Empresa` logo depois de um Wizard);
- se nenhuma servir, a D6 fica pendente, e fechar sem ela exige nova decisão do mantenedor.

**Ratificada pelo mantenedor em 2026-10-22**, nesta forma, depois da quarta rodada do painel. Não
altera as decisões D1–D5 e D7.

## 6. Mudança de código proposta

Arquivo único de produto: `Src/Extension/Package.cs`, método `ExecuteSynchronizeWithTransactionCore`.

Esboço (conforme D1 e D2; comentários no estilo ASCII do trecho):

```csharp
            ApiPlanTransactionSyncPreview preview;
            TimeSpan previewElapsed;
            ...
                previewWatch.Stop();
                previewElapsed = previewWatch.Elapsed;
                WriteOutput($"[Genexus Open API Builder][B082] Sync PreviewMs={previewWatch.ElapsedMilliseconds}.");
            ...
                var noOpReport = new ApiPlanApplicationFinalReportCollector("Sincronizar", transaction.Name, apiName);
                noOpReport.HeadlineOverride = "Nenhuma sincronizacao necessaria.";
                // B131: ausencia de diferenca e o desfecho esperado, nao aviso; a duracao e a da
                // pre-visualizacao, unica fase executada neste caminho.
                noOpReport.AddInformation("Nenhuma diferenca entre Transaction e metadata. Nenhuma alteracao foi feita na KB.");
                ShowFinalReport(noOpReport, previewElapsed, knowledgeBase.DesignModel);
```

Notas:

- `previewElapsed` segue o mesmo padrão de atribuição definida de `preview`: é atribuído no fim do
  corpo do `using`, e a única outra saída do bloco é o `return` do `catch` de aborto.
- O texto da informação é o mesmo do aviso atual, então o catálogo de tradução não muda: as
  entradas «Nenhuma diferenca entre Transaction e metadata.» e «Nenhuma alteracao foi feita na KB.»
  já existem, e «Informações (» já é traduzida no corpo do relatório.
- Nenhum outro arquivo de `Src/` muda. Manifesto, registro e menus **não** mudam.
- A DLL satélite U13 compila o mesmo fonte; a mudança não usa API nova do SDK.

## 7. Gates offline

### 7.1 Extensão do gate `tests.applicationFinalReport`

Em `Tests/ApplicationFinalReport/Test-ApiPlanApplicationFinalReport.ps1`:

1. **Comportamental (coletor):** um coletor `Sincronizar` com `HeadlineOverride` do caso e
   `AddInformation` da frase, construído com `Build([timespan]::FromMilliseconds(4920))`, deve dar
   `Outcome=Success`, `WarningCount=0`, `Information.Count=1`, título igual ao override; o resumo
   deve conter `Resultado='Success'`, `Avisos=0`, `Informações=1` e `DuraçãoMs=4920`; o corpo deve
   conter `Informações (1):`, `Avisos: (nenhum)` e `Tempo: 4,9 s`. Como `FormatElapsed` usa a
   cultura corrente (`{TotalSeconds:0.0}`), este caso guarda `[cultureinfo]::CurrentCulture`, fixa
   `pt-BR` e chama `BuildReadableBody` **dentro do mesmo bloco `try`**, restaurando a cultura
   anterior no `finally`, para o gate dar o mesmo resultado em qualquer máquina.
2. **Estrutural (`Package.cs`), delimitado ao método do Sync:** recortar o texto que começa na
   primeira ocorrência de `private static bool ExecuteSynchronizeWithTransactionCore(` e termina
   na primeira ocorrência, **depois desse início**, de `private static bool ExecuteRemoveGeneratedApi(`
   — o método que vem logo a seguir no arquivo. O padrão inclui `private static bool` e o
   parêntese porque o nome `ExecuteRemoveGeneratedApi` também aparece antes, no registro do
   comando, e é prefixo de `ExecuteRemoveGeneratedApiCore`, que tem outro `previewWatch`.

   O teste lê o `Package.cs` a partir de `$PSScriptRoot`, como já faz com os demais fontes, e não
   do diretório corrente.

   Ordem de processamento: (1) recortar o método; (2) retirar do recorte as linhas cujo primeiro
   conteúdo não branco é `//`; (3) verificar `R1` e `R2` sobre o método limpo; (4) dentro do método
   limpo, recortar o **ramo sem diferença** — do texto
   `if (!preview.Diff.HasDifferences && preview.SdtConflicts.Count == 0)` até o primeiro
   `return true;` depois dele, inclusive; (5) verificar `R3` sobre o ramo; (6) rodar as asserções.
   Limite declarado: comentário no fim de uma linha de código e bloco `/* */` não são retirados; o
   código atual deste método não usa nenhum dos dois para os termos verificados.

   Travas: **R1**, sobre o método limpo — exatamente uma ocorrência de `previewWatch.Stop()`;
   **R2**, sobre o método limpo — nenhuma ocorrência de `Remover PreviewMs`; **R3**, depois do
   recorte do ramo — o ramo foi encontrado, não está vazio e termina em `return true;`. Elas
   impedem que um recorte errado passe pelo motivo errado.

   Asserções:
   - **A1**, sobre o método limpo — ordem: a posição de `previewWatch.Stop()` é menor que a de
     `previewElapsed = previewWatch.Elapsed`, e as duas existem;
   - **A2**, sobre o ramo — a chamada `noOpReport.AddInformation(` com o texto literal completo
     `"Nenhuma diferenca entre Transaction e metadata. Nenhuma alteracao foi feita na KB."`, para
     que uma troca da frase não passe pelo gate;
   - **A3**, sobre o ramo — nenhuma chamada `noOpReport.AddWarning(`;
   - **A4**, sobre o ramo — a chamada `ShowFinalReport\(noOpReport,\s*previewElapsed` presente;
   - **A5**, sobre o ramo — nenhuma chamada `ShowFinalReport\(noOpReport,\s*TimeSpan\.Zero`.
   Recortar A2–A5 no ramo impede que um `AddInformation` ou uma chamada correta em outro ponto do
   método satisfaça a asserção. Cada trava e cada asserção falha com mensagem própria que começa
   pelo seu identificador (`R1` … `R3`, `A1` … `A5`).
3. **Autoteste negativo permanente (eficácia do gate):** as travas e as asserções do item 2 ficam
   numa função do próprio teste, que recebe o texto de um método e devolve a lista de
   identificadores reprovados. O teste roda sob `Set-StrictMode -Version Latest`, então o chamador
   embrulha o retorno em `@(...)` antes de usar `.Count` — uma lista vazia volta como `$null`. A
   função roda sobre:
   - o recorte real, que tem de dar lista vazia (controle positivo);
   - um recorte sintético por asserção, montado a partir de um recorte correto com **um único**
     defeito, e cada um tem de reprovar exatamente pela asserção esperada:
     - `A1`: a atribuição `previewElapsed = previewWatch.Elapsed` movida para antes do `Stop()`;
     - `A2`: o texto da frase trocado por outro dentro da chamada `AddInformation` (e, num segundo
       caso, a linha retirada);
     - `A3`: uma linha `noOpReport.AddWarning(…)` acrescentada, mantendo o `AddInformation`;
     - `A4`: a chamada trocada por `ShowFinalReport(noOpReport, TimeSpan.FromSeconds(1), …)` —
       nem `previewElapsed` nem zero, para não reprovar também `A5`;
     - `A5`: o recorte correto **mais** uma chamada extra `ShowFinalReport(noOpReport, TimeSpan.Zero, …)`,
       que mantém a chamada com `previewElapsed` e por isso não reprova `A4`;
   - um recorte sintético correto com `// noOpReport.AddWarning(` comentado, que tem de dar lista
     vazia (prova de que comentário não reprova).
   - um recorte sintético correto com uma segunda chamada `noOpReport.AddInformation(` **fora**
     do ramo e o ramo sem ela, que tem de reprovar só por `A2` (prova de que o recorte do ramo
     funciona).
   Regras dos recortes sintéticos:
   - cada um é gerado por substituição de texto sobre um modelo correto embutido no teste, e o
     teste confere que a substituição de fato alterou o texto antes de avaliá-lo — uma
     substituição que não casa é erro do próprio teste, não reprovação;
   - todo defeito de `A2` a `A5` é inserido **dentro do ramo, antes do `return true;`** — um
     defeito depois dele ficaria fora do recorte e não provaria nada;
   - o modelo embutido passa pelas mesmas travas `R1` a `R3` e tem as mesmas âncoras do código
     real (a condição do ramo, `previewWatch.Stop()`, a chamada `ShowFinalReport(noOpReport`); o
     teste confere que essas âncoras também existem no recorte real, para o modelo não envelhecer
     sem aviso quando o ramo mudar de forma.
   Assim a prova de que cada asserção detecta o seu defeito roda a cada pré-push, e não numa
   mutação avulsa. Nenhuma cópia ou edição do `Package.cs` do repositório.

   Limite declarado do gate: o caso comportamental usa `FromMilliseconds(4920)` e não exercita a
   conversão de um `Stopwatch` real; essa conversão foi medida à parte (seção 8.2). Uma troca
   futura da fórmula de `DuraçãoMs` não fica coberta por este gate.

O item 1 trava o contrato do coletor, que já funciona hoje; a trava contra a regressão do `B131`
são os itens 2 e 3, porque o ramo vive em `Package` e não roda fora da IDE. A igualdade observada
na IDE (seção 8) corrobora que o valor medido chega ao relatório; ela sozinha não distinguiria a
correção do código antigo se os dois valores fossem 0.

### 7.2 Sequência de validação local

1. `dotnet build-server shutdown`
2. build Release da solution canônica `Src/GenexusOpenApiBuilder.sln`
3. build Release da solution satélite `Src/GenexusOpenApiBuilder.Gx18u13.sln` (`Src/Lib/Gx18u13`
   está presente nesta máquina). O pré-push também a compila desde o `B129`
   (`dotnet.buildSatellite`), mas só depois do Commit B; compilar aqui antecipa uma eventual
   quebra para antes da entrega da DLL. O relatório pré-push continua declarando o status desse
   check e o valor de `satelliteRefs`
4. `pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1` (exigido antes de gerar DLL
   para atualização manual, mesmo sem mudança de menu)
5. `pwsh -NoProfile -File Tests/ApplicationFinalReport/Test-ApiPlanApplicationFinalReport.ps1`
6. `pwsh -NoProfile -File Tests/Localization/Test-ExtensionOutputLocalization.ps1` (controle: nada
   do catálogo deve mudar)
7. Commit A (seção 10).
8. **Depois do Commit A**, com a árvore limpa, refazer as builds dos passos 2 e 3 e registrar o
   SHA-256 da DLL canônica gerada. A DLL não carrega o hash do commit, então a proveniência da DLL
   medida é: commit A + build feita a partir dele + SHA-256. É essa DLL que vai ao mantenedor, e o
   `Tools/Test-InstalledExtension.ps1` confere a instalada contra ela.
9. `dotnet build-server shutdown`

Se o compilador recusar `previewElapsed` sem inicialização (`CS0165`), a saída é extrair a
pré-visualização para uma função que devolva a pré-visualização e o tempo juntos — **nunca**
inicializar com `TimeSpan.Zero`, que reproduziria o próprio `B131` sem erro de compilação.

## 8. Validação na IDE (o agente compila; o mantenedor instala e testa)

### 8.1 Instalação

- **Só troca de DLL.** Manifesto e registro não mudaram: **não** é preciso `genexus /install`.
- Fechar a IDE; executar `Install-ExtensionForGeneXus18.bat` como administrador (com o diretório
  da IDE como primeiro argumento, se não for o padrão); reabrir a IDE.
- O instalador roda `Tools/Test-InstalledExtension.ps1`; o agente confere de novo, por leitura, que
  a DLL instalada é a da build feita a partir do Commit A, pelo SHA-256 registrado na seção 7.2,
  passo 8.

### 8.2 Cenários

**Idioma.** O texto esperado abaixo é o português. Antes de cada cenário, conferir a propriedade
`Kb Language` da KB aberta e registrar o valor na evidência — a `wsEducacaoSpTeste` já foi
alternada para espanhol, inglês e italiano em validações anteriores
(`2026-08-15-LOCALIZACAO-TRILINGUE-E-FILTROS-EXISTENTES.md`). Se não estiver em português, voltar
para pt-BR antes do cenário (é propriedade da KB, decidida pelo mantenedor) ou comparar com o texto
traduzido e declarar isso na evidência.

| # | KB / Transaction | Preparação | Passos | Esperado |
|---|---|---|---|---|
| 1 (obrigatório) | `wsEducacaoSpTeste` — uma Transaction com API, metadata e sem delta (ex.: `Teste`/`apiTeste` ou `NotaFiscal`/`apiNotaFiscal`) | idioma conferido; ver «Se a Transaction não estiver em condição» abaixo | menu de contexto → `Sincronizar com a Transaction` | Output: `[B082] Sync PreviewMs=N`; `[B085] Nenhuma diferenca …`; `[B081] Relatório final: … Resultado='Success' … Avisos=0, Informações=1 … DuraçãoMs=N` com o **mesmo N**; nenhuma linha `[B081] Aviso:`. Diálogo: frase de cabeçalho «Nenhuma sincronizacao necessaria.», seção «Informações (1):» com a frase, «Avisos: (nenhum)» e linha «Tempo:» presente |
| 2 (obrigatório, D6) | `fabricabrasil18test` — `Empresa` primeiro (precisão da D6, seção 5) | idioma conferido; ver as duas regras abaixo | idem | o mesmo do cenário 1 e, além disso, `1000 ≤ PreviewMs < 60000`, com a linha «Tempo:» do diálogo em segundos |

**Critério da duração.** Nos dois cenários, o critério é a **igualdade exata** entre o
`DuraçãoMs` do resumo e o `PreviewMs` da mesma execução, inclusive se os dois forem 0. A
igualdade foi medida em 2026-10-22 no runtime .NET Framework 4.0.30319, fora da IDE e em processo próprio (programa
compilado com o `csc.exe` do próprio Framework e executado em processo de 32 e de 64 bits). Método:
para cada `k` de 0 a 3 999 999, os ticks `10000k − 1`, `10000k`, `10000k + 1` e `10000k + 9999` —
as bordas de cada milissegundo de 0 a cerca de 66 minutos, o que cobre com folga a faixa da D6 —,
comparando `ticks / TimeSpan.TicksPerMillisecond` com `(int)new TimeSpan(ticks).TotalMilliseconds`;
mais 2 000 `Stopwatch` reais, comparando `ElapsedMilliseconds` com
`(int)Elapsed.TotalMilliseconds`. Resultado: 16 002 000 casos (4 000 000 × 4 + 2 000; uma versão anterior deste texto dizia 16 001 999, erro de conta corrigido em 2026-10-22) e nenhuma divergência. O
documento de evidência transcreve o programa (cerca de dez linhas) e a saída, porque o arquivo
ficou fora do repositório. Por isso uma diferença de 1 ms não é tolerada: é anomalia, registrada
e investigada antes de aprovar o cenário. No cenário 1 não se exige formato em
segundos: abaixo de 1 s, `FormatElapsed` escreve em ms. No cenário 2, a faixa
`1000 ≤ PreviewMs < 60000` é exigência da D6 (seção 5); no registro de 2026-09-25 o `PreviewMs`
da `Empresa` foi 4920.

**Se a Transaction não estiver em condição** (vale para os dois cenários). O estado das KBs não é
presumido; a `Empresa`, em particular, passou por Remover, Apply interrompido, recuperação e
reaplicação depois de 2026-09-25 (nota do `B130` no backlog):

- se o Sync mostrar diferença, cancelar o diálogo (nada é aplicado) e tentar outra Transaction da
  mesma KB que tenha API e metadata;
- se o Sync bloquear por falta de metadata, registrar e tentar outra Transaction da mesma KB;
- **só no cenário 2:** se a `Empresa` estiver sem diferença mas fora da faixa
  `1000 ≤ PreviewMs < 60000`, registrar e tentar outra Transaction da KB grande nas mesmas
  condições; se uma substituta for usada, a evidência declara que provou a duração em segundos
  numa KB grande, mas não reproduziu o caso registrado em 2026-09-25 (seção 5);
- se nenhuma Transaction da KB cumprir as condições, o cenário fica **não executado**. A linha
  dele continua na matriz da evidência, com o motivo, as Transactions tentadas e a Output obtida,
  sem FAIL;
- reaplicar o Wizard para preparar a KB grava na KB e só acontece com autorização do mantenedor
  naquele momento, registrada na evidência.

**Efeito de um cenário não executado sobre o fechamento.** A D6 exige as duas KBs. Se um dos
cenários ficar não executado, a D6 fica **pendente** e o `B131` não fecha por este plano: vale o
caminho parcial da seção 9. Fechar com cobertura reduzida exige nova decisão explícita do
mantenedor, registrada no documento de evidência e no checkpoint; com essa decisão, segue-se o
caminho completo.

**O que o mantenedor devolve, por cenário:** a Transaction usada; as linhas
`[B082] Sync PreviewMs=`, o resumo do `[B085] Diff` (`Inalterados: …`) e o
`[B081] Relatório final:`; e, **obrigatoriamente**, a captura ou a transcrição do diálogo do
relatório: a frase de cabeçalho (não confundir com o título da janela, «Genexus Open API Builder -
Relatório final»), a seção «Informações», a linha «Avisos», a linha «Tempo» e se o botão
«Abrir objeto principal» aparece. A Output prova as contagens e a duração; só o diálogo prova o
que o usuário vê.

**Hipótese declarada.** Com `apiPlan` nulo, `ShowFinalReport` chama `TryResolveMainObjectFromKb`,
que lê os API Objects pelo nome sem `try` próprio. Se essa leitura lançar exceção, o `catch`
externo do método substitui o relatório esperado pelo de falha (bloqueado, duração zero). É
caminho anterior ao `B131` e não esperado; se acontecer, a evidência registra a exceção como
anomalia, o cenário não passa, e o caso é avaliado à parte, sem mudar este plano.

### 8.3 Fora da cobertura desta validação (declarar no documento de evidência)

- Sync **com** diferença: caminho não alterado.
- Renderização em espanhol e inglês: **não coberta**. O catálogo não muda e a frase continua a
  mesma, mas o gate de localização só verifica o título deste caso e a frase «Nenhuma alteracao foi
  feita na KB.»; não verifica «Nenhuma diferenca entre …» nem o cabeçalho «Informações (».
- Lacuna anterior ao `B131`, registrada e fora do escopo: o resumo da Output escreve o rótulo
  `Informações=N`, que não tem entrada no catálogo, ao contrário de `Criados=` e `Avisos=`, que
  têm; em KB em espanhol ou inglês ele tende a sair em português. Vale para qualquer relatório com
  informação, como o da recuperação do `B125`, e não só para este caso.
- IDE U13 com a DLL satélite: só a compilação é verificada.

## 9. Fechamento documental

Há dois caminhos, conforme o resultado da seção 8:

- **completo** — os dois cenários passaram, ou o mantenedor decidiu explicitamente fechar com
  cobertura reduzida: itens 1 a 10 abaixo;
- **parcial** — algum cenário ficou não executado e a D6 está pendente: só os itens 1 (o documento
  registra a sessão, com resultado «D6 pendente»), 2, 4 e 10 (cada um na forma parcial descrita
  nele), 8 e 9. Ficam **sem gravar** até o caminho completo: o «Fechado» do backlog (item 3), a
  remissão no `B081` (item 5) e o `B131` em «Último marco concluído» e em «Evidência da frente
  encerrada».

A entrada do `CHANGELOG` nasce no **Commit A** (item 2), para que a mudança de comportamento
nunca fique em `main` sem registro — nem entre o Commit A e o Commit B, nem no caminho parcial.

Um FAIL em qualquer cenário não é nenhum dos dois caminhos: volta ao código, com nova DLL e nova
sessão de validação; o documento de evidência registra a falha e aponta o reteste.

1. **Documento de evidência (obrigatório — pisos G1, G2 e G6 do documento 15 §18.2):** fecha a
   frente, tem dois cenários e é citado pela entrada `Fixed`.
   `Docs/Implementation/<data-da-sessão>-B131-SYNC-SEM-DIFERENCA-ACEITE-IDE.md`, no molde
   proporcional:
   - cabeçalho: data da sessão, item, KB, idioma (`Kb Language`) e Transaction de cada cenário,
     versão do GeneXus (U15), resultado (PASS, FAIL, ressalva, não executado, D6 pendente);
   - proveniência e alcance: commit curto da DLL e conferência da instalação; o que a DLL **não**
     inclui; campo não produzido declarado «não registrado»;
   - matriz por cenário, inclusive cenário não executado: estado inicial, esperado × obtido,
     linhas de Output, transcrição ou captura do diálogo, estado final da KB (inalterada) e limpeza
     manual («não aplicável»; o caminho não grava). `OperationId`: «não aplicável», porque o
     caminho não abre diário. Objeto principal: registrar se o botão «Abrir objeto principal»
     aparece (seção 4); o GUID dele fica «não exposto pelo relatório», porque nem a Output (que só
     mostra `PersistedMainObjectGuid`, vazio aqui) nem o diálogo sem anomalia o exibem;
   - resultado do autoteste negativo do gate (seção 7.1, item 3);
   - aberto: D4 e D5 como observações, com a resposta do mantenedor sobre abrir item de backlog
     para a D4; a lacuna do rótulo `Informações=` (seção 8.3); e o registro de que a validação de
     2026-08-08 do `B085-SINCRONIZAR-COM-TRANSACTION.md` diz «sem janela» para o Sync sem
     mudança, o que hoje não vale (o relatório final abre diálogo desde o `B081`) — sem editar
     aquele registro datado;
   - rastreabilidade: plano, itens do checkpoint, entrada do `CHANGELOG`, e a **saída literal**
     dos comandos que provam a visibilidade desde a `0.1.0-alpha.1`: os dois
     `git log -S '…' --oneline -- Src/Extension/Package.cs` da seção 3.3, o
     `git merge-base --is-ancestor abb585b v0.1.0-alpha.1` com o código de saída, e
     `git tag --contains abb585b` e `git tag --contains 6e8b28f` (que sustenta a frase do
     `CHANGELOG` sobre os rótulos);
   - a medição da igualdade de tempo (seção 8.2): o programa transcrito, o runtime, as duas
     arquiteturas e a saída.
2. **`CHANGELOG.md` — `[Unreleased]` → `### Fixed`**, **uma** entrada (regra «uma entrega, uma
   entrada» do `AGENTS.md`), escrita pelo significado e sem citar rótulos da Output: na
   `0.1.0-alpha.1` e na `0.1.0-alpha.2` o resumo usava `Outcome=`, `Warnings=` e `DurationMs=`; os
   rótulos atuais (`Resultado=`, `Avisos=`, `DuraçãoMs=`) vieram em `6e8b28f`, presente a partir da
   `0.1.0-alpha.3`. Ela muda de estado em três momentos, sempre editando a mesma entrada:
   - **Commit A** — a correção, sem citar sessão de campo (o G6 do documento 15 §18.2 não incide
     sobre `Fixed` puramente offline). Rascunho: «Relatório final do `Sincronizar` sem
     diferenças: a ausência de diferença saía como aviso, com o resultado «com avisos», e a
     duração saía zero, embora a pré-visualização tivesse rodado. Agora o resultado é de
     sucesso, a frase aparece em «Informações» e a duração é a da pré-visualização. Visível desde
     a `0.1.0-alpha.1`; só apresentação, sem efeito na KB. Validação na IDE pendente.»;
   - **Commit B, caminho completo** — troca «Validação na IDE pendente» por «Validado na IDE nas
     KBs pequena e grande» e acrescenta «Evidência:
     `Docs/Implementation/<data-da-sessão>-B131-SYNC-SEM-DIFERENCA-ACEITE-IDE.md`»;
   - **Commit B, caminho parcial** — troca por «Validado na IDE só na KB <qual>; a validação na
     outra KB está pendente» e acrescenta a mesma linha de evidência.
3. **`Docs/Foundation/06-BACKLOG_v0.1.md`:** linha do `B131` → «**Fechado em <data>**» com o link da
   evidência; na nota operacional, remissão datada no fim, sem reescrever o texto de 2026-09-25.
4. **`Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`**, em duas etapas. O «Protocolo de atualização» do
   próprio checkpoint manda atualizá-lo no mesmo commit que muda a frente ativa, e manda atualizar
   **em conjunto** «Último marco concluído», «Próxima ação única», «Evidência da frente
   encerrada», «Critério de conclusão da revisão pré-push» e «Sequência operacional vigente» —
   divergência entre elas é gap documental. Reler essas seções com `git fetch origin` antes de cada
   etapa.
   - **no Commit A:** «Última atualização»; item 189 do log (conferindo o último número no
     momento) registrando a escolha do `B131` e a implementação offline; «Próxima ação única»
     passa a «instalar a DLL e validar o `B131` na IDE (plano, seção 8)», riscando o trecho
     superado no estilo do documento; «Sequência operacional vigente» alinhada à mesma próxima
     ação; e correção, na «Próxima ação única», da frase que ainda diz que o push do `B129`
     depende de autorização (`d570163` já está em `origin/main`). O item 188 do log, que diz o
     mesmo, é registro datado e não é editado. «Frente atual», que ainda descreve a sprint
     `S-B111` encerrada em 2026-09-15, passa a descrever o `B131` como frente ativa, com a
     `S-B111` registrada como frente anterior. «Evidência da frente encerrada» ganha as entradas
     que já faltavam antes do `B131` — `B109` (2026-09-25), `B110` (2026-09-27) e `B129`
     (2026-09-28), cada uma com o documento de evidência citado no log do checkpoint —, porque o
     protocolo trata divergência entre essas seções como gap documental. «Último marco
     concluído» e «Critério de conclusão da revisão pré-push» não mudam no Commit A, porque nada
     fechou; conferir que não contradizem a frente ativa.
   - **no Commit B, caminho completo:** item 190 do log com o fechamento; «Último marco
     concluído» e «Evidência da frente encerrada» com o `B131` e o documento de evidência;
     «Critério de conclusão da revisão pré-push» conferido e alinhado; «Próxima ação única» e
     «Sequência operacional vigente» passam a «escolher a próxima frente do backlog».
   - **no Commit B, caminho parcial:** item 190 do log com a D6 pendente; «Próxima ação única» e
     «Sequência operacional vigente» passam a «concluir o cenário pendente do `B131` ou obter nova
     decisão do mantenedor sobre a D6»; as demais seções não mudam. A entrada do `CHANGELOG` já
     existe desde o Commit A e diz qual validação falta (item 2), então um corte nesse intervalo
     publica a mudança com o alcance da validação declarado.
   - **nos dois commits:** reconferir o último número do log antes de escrever — se outro item
     entrar no meio, 189 e 190 passam aos números seguintes. Conferir também que «Missão
     estacionada», «Bloqueios e fatos ainda não validados» e «Marcos ainda não iniciados» não
     contradizem a frente ativa; elas não estão no protocolo e só mudam se contradisserem.
5. **`Docs/Implementation/B081-RELATORIO-FINAL-POS-APLICACAO.md`:** remissão datada só no item da
   validação que diz «Sync sem diff: relatório … + aviso correspondente». O item da lista de
   comportamento que cita o título «Nenhuma sincronizacao necessaria.» continua correto, porque o
   título não muda, e não recebe remissão.
6. **Não editar** os documentos de evidência datados que registraram o comportamento antigo
   (`2026-08-09-DESCRIPTIONS-PRODUTO-SEM-BACKLOG.md`, `2026-08-28-FASE7-CICLO-VIDA-HIERARQUIA.md`,
   `2026-08-31-B082-PLANO-UX-PROGRESSO.md`, `2026-09-03-B082-ETAPA-1A-ACEITE.md`,
   `2026-09-10-S-B111-F1-ACEITE-IDE.md`, `2026-09-24-B120-ACEITE-IDE-SMOKE-HTTP.md`,
   `2026-08-15-LOCALIZACAO-TRILINGUE-E-FILTROS-EXISTENTES.md`, que cita só o título): descrevem a
   DLL que mediram, e seguem corretos para a data em que foram escritos. O
   `B085-SINCRONIZAR-COM-TRANSACTION.md` é outro caso: mistura seções de comportamento com
   validações datadas. Também não é editado, porque a seção «Comportamento» não descreve o relatório
   sem diferença e as validações que citam o caso são registro datado.
7. **Sem mudança pública agora:** `README*`, `Docs/Public/INSTALL.md` e `Docs/Public/DEMO.md` não
   descrevem o caso. Pendência do próximo corte, registrada no item 190 do log: o `DEMO.md` diz
   que o relatório lista «criados, atualizados, bloqueados e avisos», o que já estava incompleto
   desde a recuperação do `B125`, que mostra «Informações», e continua incompleto com o Sync sem
   diferença. Os documentos públicos descrevem a DLL publicada e mudam no corte (regra «Corte de
   release» do `AGENTS.md`).
8. **Higiene:** em `Docs/`, citar símbolos (`ExecuteSynchronizeWithTransactionCore`, `ResolveOutcome`)
   e nunca `Package.cs:<linha>` (gate `docs.csharpLineReferences`, `B128`); LF final em todo `.md`
   alterado (`[IO.File]::ReadAllBytes($path)[-1] -eq 10`).
9. **Varredura de promoção** (regra «Promoção de frente e próximo passo» do `AGENTS.md`): buscar
   pelos radicais `B131`, o ID seguinte do backlog (`B132` e seguintes, para confirmar que nenhum
   é anunciado como próxima ação por engano), `noOpReport`, `Nenhuma diferenca`,
   `sincronizacao necessaria`, `SuccessWithWarnings`, `DuraçãoMs=0`, `B129`, `d570163`, e os
   radicais do que ainda falta que o `AGENTS.md` lista — `próxima ação`, `pendente`, `restam`,
   `falta`, `aguarda`, `ainda não`, `sem validação`, `não iniciada` —, e as contagens tratadas como
   termo de estado (número de cenários, de decisões, de rodadas). Confirmar que nenhum documento
   vigente contradiz o novo estado, e varrer o próprio plano e o próprio checkpoint pelo fato que
   mudou, porque foram editados por trecho.
10. **Estado deste plano:** no Commit A, o cabeçalho passa a «implementado em <data>, aguardando
    validação na IDE» e perde a frase «Ainda não versionado»; no Commit B, passa a «executado em
    <data>», com remissão ao documento de evidência, ou, no caminho parcial, a «validado em parte;
    D6 pendente».

## 10. Commits, pré-push e publicação

1. Commit A — este plano (com o cabeçalho da seção 9, item 10) + código + gate + a entrada do
   `CHANGELOG` na forma do Commit A (seção 9, item 2) + a etapa do checkpoint da seção 9, item 4,
   antes de entregar a DLL, para que o plano passe a ser versionado, o checkpoint diga qual é a
   frente ativa, a mudança não fique em `main` sem registro e a evidência cite o commit da DLL
   medida.
2. Build a partir do Commit A e SHA-256 da DLL (seção 7.2, passo 8); validação na IDE (seção 8).
3. Commit B — fechamento documental (seção 9).
4. `git fetch origin` isolado; depois, na raiz, exatamente
   `pwsh -NoProfile -File scripts/Invoke-PrePushMechanicalChecks.ps1 -AsJson`; ler `pushReadiness`,
   `incompleteReasons`, `manualRequired`, `warnings` e `notCovered`.
5. Revisão semântica: trio API/Procedure/SDT não é afetado (nenhum emissor muda); declarar
   contratos alterados (tabela da seção 4), produtores, consumidores, risco de escrita parcial
   (nenhum) e validação faltante (seção 8.3).
6. `git push` **só com autorização explícita** do mantenedor. Sem corte de release neste item.

## 11. Riscos

| Risco | Avaliação | Tratamento |
|---|---|---|
| Alguém comparar `SuccessWithWarnings` no Sync sem diferença | Nenhum gate ou documento público faz isso; os documentos antigos são registro datado | Entrada `Fixed` no `CHANGELOG` explica a mudança |
| Output perde a linha `[B081] Aviso: Nenhuma diferenca …` | Intencional; a linha `[B085]` equivalente continua | Declarado no documento de evidência |
| Quebra de compilação da satélite | Improvável: sem API nova | Build manual da satélite na seção 7.2, antes da entrega da DLL; o pré-push repete em `dotnet.buildSatellite` |
| KB fora da condição de um cenário (em especial a `Empresa`) | O estado dela mudou depois de 2026-09-25 | Regra de decisão da seção 8.2; cenário não executado deixa a D6 pendente até nova decisão do mantenedor |
| Planejar a validação sobre estado presumido da IDE | O estado da máquina do mantenedor envelhece | O agente só verifica o que é verificável (DLL instalada); horário e KB ficam a critério do mantenedor |

## 12. Fora do escopo

- Duração zero nos `catch` externos de Sync e Remover (D4).
- Acentuação pt-BR dos textos do caso (D5).
- Escrever informações na Output (D3).
- Tempo do Sync **com** diferença, que hoje mede só a fase de escrita e não inclui a
  pré-visualização — observação de consistência, não defeito registrado.
- Qualquer mudança no Wizard, no Remover, na recuperação ou no diário.

## 13. Ordem de execução resumida

1. ~~Decisões D1–D7~~ — tomadas em 2026-09-28 (seção 5).
2. ~~Promover o plano~~ — feito em 2026-09-28.
3. Conferência de deriva (cabeçalho), código (seção 6) + gate com autoteste negativo (seção 7.1)
   + validação local, incluindo a build satélite (seção 7.2).
4. Commit A, com este plano (cabeçalho «implementado»), a entrada do `CHANGELOG` e a primeira
   etapa do checkpoint.
5. Build a partir do Commit A, SHA-256 da DLL e roteiro de instalação e teste ao mantenedor
   (seções 7.2 e 8).
6. Documento de evidência + edição da entrada do `CHANGELOG` + backlog + checkpoint + remissão no
   `B081` + estado deste plano (seção 9), no caminho completo ou no parcial.
7. Commit B, pré-push, relatório e pedido de autorização para push (seção 10).

## 14. Histórico de versões

- **v1 (2026-09-27/28):** plano inicial, decisões D1–D7.
- **v2 (2026-10-22):** primeira rodada do painel (DeepSeek, Meta, OpenAI e Claude Opus nativo;
  stealth sem parecer), sem gap de desenho. Triagem aceita pelo mantenedor; o que mudou:
  - plano entra no Git no Commit A (cabeçalho e seção 10);
  - transcrição ou captura do diálogo passa a obrigatória, e o gate verifica a linha de tempo
    sem depender da cultura (seções 7.1 e 8.2);
  - regra de decisão para o cenário 2 quando a `Empresa` não estiver em condição (seção 8.2);
  - gate estrutural recortado ao método do Sync, com ordem da atribuição e autoteste negativo
    permanente no lugar da mutação avulsa (seção 7.1);
  - seção 8.3 corrigida: es/en não coberto; lacuna do rótulo `Informações=` registrada;
  - build satélite manual antes da entrega da DLL (seções 7.2 e 11);
  - remissão no `B081` restrita ao item que cita o aviso; `B085` reclassificado (seção 9);
  - evidência com G1, G2 e G6 e matriz por cenário; varredura ampliada; estado do plano
    atualizado no fechamento (seção 9);
  - `PreviewMs` na casa dos segundos passou de critério a observação (seção 8.2).
  - Descartados com justificativa: diferença de 1 ms entre `DuraçãoMs` e `PreviewMs` (os dois
    vêm dos mesmos ticks, com o cronômetro parado); refazer a prova de visibilidade na execução
    (já feita por `git log -S` e `merge-base`, citada na evidência); explicitar a ausência de
    `[B081] Aviso:` (já estava no esperado da seção 8.2).
  - Decisões D1–D7 inalteradas.
- **v3 (2026-10-22):** segunda rodada do painel (DeepSeek, Meta, OpenAI e Claude Opus nativo;
  stealth sem parecer de novo), sem nenhuma voz reabrindo o desenho. Triagem aceita pelo
  mantenedor; o que mudou:
  - cenário não executado deixa a D6 **pendente**; fechar com uma KB só exige nova decisão do
    mantenedor (seção 8.2) — objeção tratada como bloqueante pelo revisor OpenAI;
  - autoteste negativo com um recorte por asserção (A1–A5), cada um reprovado pela asserção
    esperada, mais controle positivo e caso de `AddWarning` comentado (seção 7.1);
  - comentários retirados do recorte antes das verificações; ordem conferida por posição
    (seção 7.1);
  - teste do tempo com cultura `pt-BR` fixada, no lugar da regex com dois separadores (seção 7.1);
  - critério da duração passa a ser só a igualdade com `PreviewMs`, inclusive zero; 1 ms de
    diferença vira observação; formato em segundos deixa de ser exigido (seção 8.2);
  - regra «Transaction fora de condição» vale para os dois cenários, e o cenário não executado
    fica na matriz (seções 8.2 e 9);
  - roteiro distingue a frase de cabeçalho do título da janela e pede o botão «Abrir objeto
    principal» (seção 8.2); evidência separa `OperationId` do GUID do objeto principal (seção 9);
  - checkpoint e cabeçalho do plano atualizados já no Commit A, e de novo no Commit B
    (seções 9 e 10);
  - evidência com a saída literal dos comandos de Git e o registro do «sem janela» do `B085`
    (seção 9);
  - redação: «única fase do Sync», escopo da busca por `Outcome`, gate de localização, documento
    datado que faltava na lista (seções 1, 4 e 9).
  - Descartado com justificativa: inicializar `previewElapsed` com `TimeSpan.Zero` — sem
    inicialização, o compilador garante a atribuição; com zero, um sumiço da atribuição
    reproduziria o próprio `B131` sem erro de compilação. A diferença de 1 ms, levantada de novo,
    continua sem fundamento no mérito, mas passou a ter tolerância no critério.
  - Decisões D1–D7 inalteradas.
- **v4 (2026-10-22):** terceira rodada do painel (DeepSeek, Meta, OpenAI e Claude Opus nativo;
  stealth fora por decisão do mantenedor), de novo sem nenhuma voz reabrindo o desenho. O
  mantenedor delegou a redação da v4; o que mudou:
  - **precisão da D6** (seção 5), escolhida pelo agente sob essa delegação, por preservar os dois
    motivos registrados da D6: `Empresa` primeiro; outra Transaction da KB grande só com
    `PreviewMs` de 1 s ou mais; senão D6 pendente — objeção tratada como bloqueante pelo revisor
    OpenAI em duas rodadas;
  - checkpoint: as cinco seções que o protocolo manda atualizar em conjunto passam a constar,
    por commit e por caminho (seção 9, item 4) — objeção tratada como bloqueante pelo revisor
    DeepSeek;
  - fechamento em dois caminhos, completo e parcial, dizendo o que fica sem gravar com a D6
    pendente, e FAIL como volta ao código (seção 9);
  - testes negativos de `A4` e `A5` construíveis sem reprovar um ao outro; travas `R1` e `R2` do
    recorte; padrão exato de início e fim do recorte; limite declarado da retirada de
    comentários; troca de cultura no mesmo `try/finally` da montagem do corpo (seção 7.1);
  - idioma da KB conferido antes de cada cenário (seção 8.2);
  - GUID do objeto principal registrado como «não exposto pelo relatório» (seção 9);
  - entrada do `CHANGELOG` descrita pelo significado, porque os rótulos da Output mudaram na
    `0.1.0-alpha.3` (`6e8b28f`) (seção 9);
  - igualdade de tempo na IDE tratada como corroboração; a trava contra regressão é o gate
    (seção 7.1);
  - varredura com os radicais do `AGENTS.md`, contagens, `B129` e `d570163`; o item 188 do log
    fica como registro datado (seção 9);
  - conferência de deriva refeita antes de codar (cabeçalho); redação da duração medida (seção 1).
  - Descartados com justificativa: reabrir os documentos do `B081` e do `B085` para confirmar o
    alvo da remissão (três revisores já o confirmaram lendo os documentos); a diferença de 1 ms,
    levantada pela terceira vez (já é observação no critério).
  - Decisões D1–D5 e D7 inalteradas; D6 inalterada na decisão e no motivo, com a precisão acima.
- **v5 (2026-10-22):** quarta rodada do painel (DeepSeek, Meta, OpenAI e Claude Opus nativo;
  stealth fora por decisão do mantenedor), sem gap bloqueante; o revisor Meta respondeu «sem
  gaps». O que mudou:
  - precisão da D6 **ratificada pelo mantenedor**, com a faixa `1000 ≤ PreviewMs < 60000` (a
    partir de 60 s o diálogo mostra minutos) e a declaração, na evidência, de que uma Transaction
    substituta não reproduz o caso registrado (seções 5 e 8.2);
  - igualdade exata entre `DuraçãoMs` e `PreviewMs`, depois de **medida** no runtime da IDE sem
    nenhuma divergência; 1 ms deixa de ser tolerado e vira anomalia (seção 8.2). A disputa entre
    os revisores (tolerância × igualdade exata) foi resolvida por medição, não por escolha;
  - asserções A2–A5 recortadas no ramo sem diferença, com a trava `R3` e um caso de teste que
    prova o recorte; ordem fixa de processamento (recorte, comentários, travas, asserções);
    cuidados de `Set-StrictMode` e conferência de que cada substituição alterou o texto
    (seção 7.1);
  - checkpoint no Commit A: «Frente atual» passa ao `B131`; «Evidência da frente encerrada»
    recebe `B109`, `B110` e `B129`, que já faltavam (seção 9, item 4);
  - caminho parcial registra o que fazer com o `CHANGELOG` se houver corte com a D6 pendente
    (seção 9, item 4);
  - pendência do `DEMO.md` para o próximo corte (seção 9, item 7);
  - cobertura de leitura até o item 188 do log; redação do cronômetro do Sync com diferença
    (seções 1 e 2).
  - Descartado com justificativa: reconferir os rótulos de `6e8b28f` (já conferido por
    `git show v0.1.0-alpha.1` e `git tag --contains 6e8b28f`).
  - Decisões D1–D7 inalteradas; a precisão da D6 passou de delegada a ratificada.
- **v6 (2026-10-22) — congelada:** quinta rodada do painel (DeepSeek, Meta, OpenAI e Claude Opus
  nativo; stealth fora por decisão do mantenedor). Opus e Meta concordaram; o único bloqueante
  (OpenAI) foi de procedimento — a mudança ficaria em `main` sem entrada no `CHANGELOG` —, também
  apontado por Meta e Opus. O que mudou:
  - entrada `Fixed` criada já no Commit A, com «validação na IDE pendente», e editada no Commit B
    nos dois caminhos (seção 9, item 2; seção 10);
  - `R3` verificada depois do recorte do ramo; defeitos sintéticos de `A2`–`A5` sempre dentro do
    ramo; `A2` com o texto literal da frase; modelo sintético com as mesmas travas e âncoras do
    código real; leitura por `$PSScriptRoot`; limite declarado do gate sobre a conversão do
    `Stopwatch` (seção 7.1);
  - build refeita a partir do Commit A, com SHA-256 da DLL entregue; saída prevista para `CS0165`
    sem voltar ao zero (seções 7.2 e 8.1);
  - método e faixa da medição de 1 ms registrados; programa transcrito na evidência; hipótese
    declarada sobre `TryResolveMainObjectFromKb` (seção 8.2);
  - arredondamento para «60,0 s» aceito; `DEMO.md` «continua» incompleto; reconferência do número
    do log e das demais seções do checkpoint nos dois commits; `git tag --contains 6e8b28f` na
    evidência (seções 5, 8.2 e 9).
  - Descartado com justificativa: a crítica à justificativa do padrão de fim do recorte — o padrão
    inclui `private static bool` justamente porque o registro do comando não tem esse prefixo.
  - Decisões D1–D7 inalteradas.
  - **Congelamento:** a v6 não foi submetida ao painel. O mantenedor decidiu em 2026-10-22
    («escreva v6 como achar que deve. e vamos usar ela numa nova sessão, pra executar o plano»)
    encerrar o ciclo em papel e executar. Registro no fechamento da revisão por pares:
    `vNextState=resubmissionDeclinedByHuman`, motivo «prova transferida para
    implementação/self-test», rodada `B131-20261022-v5`.
  - **Correção de conta (2026-10-22, aceita pelo mantenedor):** a contagem da medição da igualdade
    passa a 16 002 000 casos, em vez de 16 001 999 (seção 8.2). A medição foi refeita em processo
    novo, em x86 e x64, com zero divergências e controle positivo. Nenhuma decisão, regra ou
    faixa muda.
