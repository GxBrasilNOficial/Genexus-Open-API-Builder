# B128 — implementação offline e gates

Data: 2026-09-23. Estado: implementação, revisão e validação offline concluídas; commit local em `main` em 2026-09-23. Sem validação na IDE ou em runtime.

## Escopo adotado

O manuscrito v8 (`Temp/revisao-por-pares/B128-20260923-0925/manuscrito-v8.md`) é proveniência local da discussão e da decisão; `Temp/*` é ignorado pelo Git, e o manuscrito não é normativo. O contrato versionado do comportamento entregue está em `Docs/Implementation/2026-09-23-B128-CONTRATO-REFERENCIAS-C.md`. O gate controla incrementos globais de referências numéricas C# por linha móvel e valida citações históricas fixas novas. Não migra o legado nem verifica se a afirmação ao redor da citação corresponde semanticamente ao código.

A leitura que embasou o plano mediu 45 ocorrências em 37 linhas de cinco documentos B082/B111. É contagem textual; não classifica a correção semântica de cada afirmação.

## Implementação

- `scripts/B128-ReferenceTokenizer.ps1` contém o tokenizer e as regras de chave compartilhados pelo checker e por `Tests/PrePushChecker/Test-B128ReferenceTokenizer.ps1`.
- `scripts/Invoke-PrePushMechanicalChecks.ps1` registra `docs.csharpLineReferences` no array `checks` e integra `failed` e `environmentBlocked` ao resultado geral. Sem `origin/main`, o check fica `skipped` e o bloqueio de `git.remoteBase` permanece; com remoto à frente, fica `skipped` como `notEvaluated/remoteBehind`.
- Para referências móveis em Markdown de `Docs/`, o gate compara multiconjuntos globais de `origin/main` e `HEAD`, lendo blobs Git. Normaliza barras no caminho, mantém comparação ordinal de caixa e reprova qualquer acréscimo reconhecido, inclusive candidato com localização malformada. Formas com formatação Markdown e escape compartilham a mesma chave; a unidade numérica é consumida integralmente para que sufixos não passem como prefixos válidos.
- Para citações fixas com SHA completo, caminho C# relativo e linhas ancoradas, compara a chave bruta e valida somente os excedentes: SHA de commit ancestral, path relativo sem traversal como blob `.cs`, faixa positiva e crescente dentro das linhas físicas do blob. Exit 1 de ancestralidade identifica commit lateral; falhas operacionais e ancestralidade inconclusiva em clone raso resultam em `environmentBlocked`. Os limites usam `BigInteger`; conteúdo é lido por bytes e validado como UTF-8.
- O aviso `b128-changelog-line-reference` é heurístico e não bloqueante. Observa somente linhas adicionadas no diff commitado de `Docs/` que associem `CHANGELOG` a `linha(s)`, `line(s)` ou `línea(s)` com número/faixa na mesma linha ou linha anterior. Falha Git/decodificação gera aviso de cobertura incompleta.
- No aceite B082 Sessão B e no plano B124, substituí localizadores móveis do `CHANGELOG` por versão, seção `Validated` e início distintivo da entrada. No B124, backlog/checkpoint e as regras de `Temp/` agora são referidos por item, seção e padrão `.gitignore`; as faixas mantidas em registros de leitura/revisão estão explicitamente datadas.
- O JSON `notCovered` declara legado deslocado, formatos não reconhecidos, arquivos fora de `Docs/`, formas `Arquivo.cs(linha,coluna)`/`Arquivo.cs:line N`, limites da heurística CHANGELOG e ausência de prova semântica.

## Validação executada

- `pwsh -NoProfile -File Tests/PrePushChecker/Test-B128ReferenceTokenizer.ps1`: 35 assertions passaram. Inclui gramáticas móvel/fixa, formatação, pontuação, intervalos sintáticos e validados, linha acima de `Int32`, posições com CRLF/LF/CR, contagem física de linhas, path Unicode com espaço, SHA inválido, prefixo URL inválido, blob UTF-8 inválido, aviso explícito para falha Git/decode, erro operacional Git, objeto que não é commit, commit lateral, path ausente, diretório `.cs`, clone raso e conjunto sem candidatos.
- `pwsh -NoProfile -File Tests/PrePushChecker/Test-OpenApiBuilderPrePushChecks.ps1`: harness completo passou com exit 0. A fixture executou o checker contra Git descartável: aceitou citação fixa ancestral válida, retornou `environmentBlocked` para blob C# não decodificável, bloqueou citação móvel malformada, conferiu avisos numéricos CHANGELOG em português/inglês/espanhol somente no diff commitado, verificou `notCovered` e preservou os cenários existentes de B124.
- O checker, o helper e os dois testes passaram no parser PowerShell 7.6.6.

## Limites e estado

As citações móveis antigas permanecem como estão se sua contagem global não aumentar. Mover uma ocorrência existente de um documento para outro pode compensar uma ocorrência nova. O check é sintático: não confirma que o símbolo ainda expressa o comportamento descrito. Falsos negativos são possíveis fora da gramática declarada e na heurística de CHANGELOG; falsos positivos da heurística são avisos, não bloqueios.

O teste vinculante criou e removeu seus repositórios bare e clones rasos em diretórios temporários. Nenhuma alteração de IDE/KB, DLL ou manifesto da extensão foi feita. O commit é local; não houve push.
