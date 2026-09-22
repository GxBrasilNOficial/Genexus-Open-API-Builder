# Instruções locais para agentes

## Proteção da instalação do GeneXus

- É proibido alterar, criar, mover, renomear ou excluir qualquer arquivo ou pasta em `C:\Program Files (x86)\GeneXus` ou em suas subpastas.
- Essa instalação pode ser consultada somente em modo leitura para localizar e inspecionar o Extensibility SDK e suas dependências.
- Artefatos do projeto devem ser criados apenas dentro deste repositório.

## Higiene de documentação Markdown

- Após qualquer alteração em arquivos `.md`, validar que o arquivo termina com LF final, especialmente porque `.gitattributes` define `*.md text eol=lf`.
- Em PowerShell, uma verificação direta é `[IO.File]::ReadAllBytes($path)[-1] -eq 10`; não considerar a edição concluída enquanto o último byte não for `10`.
- `git diff --check` não acusa ausência de newline final, então esta conferência deve ser explícita antes de commitar documentação.

## Edição textual ancorada em lote (B122)

- Para várias substituições literais no mesmo arquivo (ou quando a edição precisa de unicidade
  estrita da âncora, política de EOL/BOM e prévia sem gravar), preferir
  `scripts/Apply-TextPatch.ps1` com manifesto JSON (`-ManifestPath`) e, se couber, `-WhatIf`.
- Não reinventar script descartável de `contains` + primeira ocorrência: o contrato (D16,
  `count == 1`, recusa de MIXED/BOM, gate `tests.textPatch`) está no plano
  `Docs/Implementation/2026-09-18-B122-PLANO-EDICAO-TEXTUAL-ANCORADA.md`.
- Esta ferramenta **não** substitui `Apply-ApprovedPatch` das skills XPZ (backup de unified
  diff Git); são complementares.

## Skills transversais de GeneXus-XPZ-Skills

O repositório irmão `C:\Dev\Knowledge\GeneXus-XPZ-Skills` (ou a raiz de repositório de skills
publicada pela sessão que contenha a pasta `scripts\` e o documento `15-revisao-por-pares.md`,
aqui referida como `<skills-root>`) é a fonte canônica de skills e metodologia compartilhada
do ecossistema. O prefixo `xpz-` é marcador de família, não restrição de escopo a arquivos
`.xpz` ou XML de KB: parte do catálogo é transversal e se aplica a este repositório de
extensão C#. Caso a pasta de skills não esteja acessível nem resolvida na sessão, o agente
deve avisar o usuário e não inferir caminhos alternativos.

- `xpz-llm-delegate` (`<skills-root>\xpz-llm-delegate\SKILL.md`) — revisão por pares multi-modelo,
  segunda opinião e delegação a LLM secundário. A própria skill se declara transversal.
- `xpz-codex-apply-patch-alternative` (`<skills-root>\xpz-codex-apply-patch-alternative\SKILL.md`) —
  backup da rota nativa `apply_patch`, aplicável apenas a agentes que a usem (Codex). Exige
  passar a raiz deste repositório em `-RepositoryRoot` e não permite copiar o motor para cá.

**Termo operacional reservado (Revisão por Pares).** Quando o usuário pedir `revisão por pares`,
`peer review`, `painel multi-modelo` ou `validar plano multi-modelo`, carregar
`<skills-root>\xpz-llm-delegate\SKILL.md` e `<skills-root>\15-revisao-por-pares.md` antes de
responder, e seguir o contrato de entrada ali definido na íntegra — inclusive gate de
autorização por destino, piso de diversidade (≥2 Criadores de Modelos distintos), recibo mínimo
obrigatório e closeout. Esse contrato não é reproduzido aqui: as fontes acima são normativas,
junto com os documentos que elas próprias mandam consultar. É proibido rotular como `revisão por
pares` um parecer solo ou de uma única família de modelo; sem painel válido, rotular `parecer
solo` ou `segunda opinião (N)`.

**Alvo pré-push.** Se o alvo da revisão for o pré-push, a metodologia do `15` aplica-se sobre a
rotina pré-push local deste `AGENTS.md` (seção "Revisão pré-push do repositório").
`14-revisao-pre-push-reforcada.md` e os documentos que ele referencia são consultivos: servem
como referência metodológica e nunca substituem nem importam a rotina pré-push daquele
repositório.

**Segunda opinião e tarefas pontuais.** Para pedidos de `segunda opinião` ou tarefas delegáveis,
o agente pode consultar um modelo secundário individual via `xpz-llm-delegate`, cumprindo gate
de autorização por destino e rotulando a resposta como parecer solo ou segunda opinião, sem
exigir painel multi-modelo, piso de diversidade ou convergência.

**Timeout de consultas delegadas.** Para consultas a outros agentes, usar `1200` segundos como
timeout padrão do adapter, salvo orientação diferente do usuário. O polling e as esperas entre
atualizações devem continuar em intervalos controlados, sem interpretar ausência de saída parcial
como conclusão.

**Acionamento sempre humano.** O agente pode sugerir delegar, nunca acionar por conta própria. A
saída de qualquer modelo ou painel é insumo de avaliação, não autorização para editar, commitar
ou concluir convergência.

**Lista de revisores preferidos e capacidades.** Resolver preferência por
`Resolve-LlmDelegatePreferredReviewers.ps1`. Os arquivos `preferred-reviewers.json` e
`capabilities.json` são machine-level, vivem em `%LOCALAPPDATA%\xpz-llm-delegate\` e **não**
existem neste nem em nenhum repositório — não buscá-lo com `Glob`/`Grep`/`ls`; a única fonte de
verdade sobre preferências é o `hasPreferences` devolvido pelo resolvedor.

**Invocação de scripts compartilhados.** A seção "Forma canônica de invocação dos adapters" do
`SKILL.md` é normativa para adapters de invocação (`Invoke-*`, `Start-*Job`): comando atômico,
prompt sempre por `-MessagePath <arquivo>`, zero aspas embutidas. Com o `cwd` neste repositório
(fora da raiz de skills), usar a forma absoluta — pela ferramenta **PowerShell**,
`& "<skills-root>\scripts\<Adapter>.ps1" -MessagePath <arquivo>`; pela ferramenta **Bash**,
`pwsh -NoProfile -File "<skills-root>/scripts/<Adapter>.ps1" -MessagePath <arquivo>`. Nunca usar
path relativo pela ferramenta PowerShell. Resolvedores e scripts de suporte (`Resolve-*`, `Set-*`,
`New-*`, `Build-*`, `Watch-*Job`) seguem suas respectivas assinaturas de parâmetros.

**Trava contra colisão de nomes e escopo.** `<skills-root>\scripts` contém um
`Invoke-PrePushMechanicalChecks.ps1` homônimo ao deste repositório, além de scripts de importação
e build de KB (`Invoke-GeneXusXpz*`, `Invoke-GeneXusKb*`, `Invoke-XpzKbParallel*`). Nenhum deles é
autorizado por esta seção. `Resolve-LlmDelegationPolicyPath.ps1` (`Delegation`, não `Delegate`)
fica fora do curinga e fora do escopo: resolve política por pasta paralela de KB (`-ParallelKbRoot`)
e não se aplica a este repositório. A rotina pré-push válida aqui é sempre a local
(`scripts/Invoke-PrePushMechanicalChecks.ps1`, seção "Revisão pré-push do repositório"). Desta
seção, a origem permitida de scripts do repositório irmão restringe-se a esta lista fechada:

- diagnóstico e governança: `Resolve-LlmDelegate*.ps1`, `Resolve-*ModelLocality.ps1`,
  `Set-LlmDelegatePreferredReviewers.ps1`, `New-LlmDelegatePeerReviewArtifacts.ps1`,
  `Build-LlmDelegateCapabilityManifest.ps1`, `LlmDelegateTargetFamilySupport.ps1`;
- despacho do painel: `Invoke-LlmDelegatePanelDispatch.ps1`;
- adapters síncronos: `Invoke-Codex.ps1`, `Invoke-ClaudeCode.ps1`, `Invoke-ClaudeCodeAsync.ps1`,
  `Invoke-OpenCode.ps1`, `Invoke-Gemini.ps1`, `Invoke-Copilot.ps1`, `Invoke-Antigravity.ps1`;
- runners e monitores: `Start-CodexJob.ps1`, `Start-OpenCodeJob.ps1`, `Start-ClaudeCodeJob.ps1`,
  `Watch-CodexJob.ps1`, `Watch-OpenCodeJob.ps1`, `Watch-ClaudeCodeJob.ps1`;
- backup de patch textual: `Apply-ApprovedPatch.ps1` (sob o contrato de
  `xpz-codex-apply-patch-alternative`, exigindo `-RepositoryRoot` apontando para a raiz deste
  repositório e aprovação prévia dos caminhos).

Qualquer outro script de `<skills-root>\scripts` está fora do escopo desta seção. A lista delimita
os scripts permitidos, sem autorizar execução automática nem substituir o contrato próprio de
cada um.

## Build local da extensão

Duas causas distintas travam o build local com sintomas parecidos. Antes de contornar qualquer uma delas, identificar qual é: mensagem de **arquivo em uso** aponta para a causa 1; **negação de escrita** sem nenhum processo de IDE ou compilador vivo aponta para a causa 2. O dono, por `(Get-Acl -LiteralPath <arquivo>).Owner` divergindo de `[Security.Principal.WindowsIdentity]::GetCurrent().Name`, é indício e não confirmação: antivírus, indexador e sincronizador também seguram arquivo, e a permissão relevante pode estar no diretório-pai.

**Causa 1 — node de build sobrevivente (arquivo em uso).** O `dotnet build` mantém vivos o MSBuild Server e o compilador VB/C# (`VBCSCompiler`), com handles abertos em `Src/Extension/obj/`. Um node de execução anterior bloqueia a build seguinte. Antes de compilar, e novamente ao terminar, executar:

```powershell
dotnet build-server shutdown
```

**Causa 2 — artefato de outro principal (negação de escrita).** Agentes às vezes executam sob conta própria — medido no Codex, que roda como `CodexSandboxOffline`. Artefatos criados por uma identidade em `obj/` e `bin/` podem negar escrita à seguinte, **sem lock algum**. O mecanismo não foi isolado: token restrito, ACE de `CREATOR OWNER`, nível de integridade e política da própria ferramenta são todos compatíveis com o observado — numa amostra, `Usuários autenticados` já tinha `Modify` no arquivo negado, o que desfavorece a explicação por ACL simples. Por isso **não** conserte por permissão: `icacls` amplo deixa a árvore permanentemente afrouxada e pode nem resolver. Também não eleve o build nem use `takeown`. O sentido inverso — artefato do usuário negando escrita ao agente — não foi medido.

Nesta ordem, e tudo reversível de propósito:

1. **Redirecionar a saída** sem tocar na árvore bloqueada. No MSBuild são duas propriedades distintas: `BaseOutputPath` governa `bin/` e `BaseIntermediateOutputPath` governa `obj/` — redirecionar só a primeira deixa o build escrevendo no `obj/` bloqueado.
2. **Renomear** o diretório bloqueado, com `-LiteralPath`, para um nome que ainda não exista (`obj` → `obj.orphan-<data>`). Preserva o conteúdo e costuma passar onde escrever falha, porque depende de direito no diretório-pai. Renomeie apenas o que a ferramenta recria, e apenas o mais interno que resolva o bloqueio. Vá ao passo 3 se houver conteúdo versionado dentro, se o diretório tiver `ReparsePoint` em `Attributes`, ou se você não souber dizer.
3. **Parar e reportar ao usuário da sessão** qual identidade é dona e qual está tentando escrever. Apagar o que ficou é decisão dele, não sua.

**Não apague** `obj/`, `bin/` ou qualquer diretório gerado para resolver isto.

- Diante de qualquer das duas, o agente **não** encerra processos de terceiros nem altera a instalação do GeneXus.
- Se o bloqueio persistir **com a IDE GeneXus aberta**, quem segura a DLL é a IDE, e a única saída é fechá-la. Nenhuma configuração de MSBuild ou limpeza de artefato alcança esse caso.
- `MSBUILDDISABLENODEREUSE=1` cobre apenas os nodes do MSBuild, não o `VBCSCompiler`, e não tem efeito sobre a causa 2; o `build-server shutdown` cobre os dois processos da causa 1.

Ambas as causas foram diagnosticadas em 2026-08-23, em rodadas distintas de agente sobre este repositório: a causa 1 com bloqueio em `Src\Extension\obj\Release\net471\GenexusOpenApiBuilder.Extension.dll` e build limpa após o shutdown; a causa 2 com os artefatos de `obj/` e `bin/` divididos entre `ANTONIOJOSE` e `CodexSandboxOffline`.

A remediação da causa 2 foi revista em 2026-08-25, depois de revisão por pares em painel multi-modelo. A redação original afirmava o mecanismo como fato, alegava simetria não medida e mandava **apagar** `obj/` e `bin/` como primeira ação — numa árvore cujo conteúdo o agente não inspecionara. A escada de três passos substitui a deleção por ações reversíveis e devolve ao humano a única decisão irreversível. Apagar artefatos entre rodadas continua válido para quem é dono das duas identidades e conhece o conteúdo; o que a seção proíbe é o agente fazê-lo por conta própria.

## Corte de release

Publicação exige **autorização humana explícita a cada corte**. `git push`, criação de tag e publicação de GitHub Release nunca são inferidos de "a frente terminou".

Cada corte produz três notas no repositório, em `Docs/Releases/`: `<versão>.md` (pt-BR), `<versão>.es.md` e `<versão>.en.md`. Links entre elas são relativos, porque a navegação é dentro da pasta.

O **corpo do GitHub Release não é cópia dessas notas.** É texto próprio, escrito para a página, com três seções de idioma no mesmo corpo, nesta ordem:

```markdown
# Português (Brasil)
# Español
# English
```

Cada seção traz: resumo curto da entrega, a tabela de escolha obrigatória da DLL (canônica U14+ e satélite `-gx18u13`), instalação, e **link absoluto** para a nota detalhada daquele idioma, fixada na tag do corte — nunca link relativo, que não resolve na página de release.

Depois das três seções, uma seção `## Checksums` **única**, com o SHA-256 de cada asset. Ela não se repete por idioma: é tabela de dados. Os valores têm de coincidir com o `digest` que o GitHub calcula, verificável por `gh release view <tag> --json assets`. O checksum fica na página porque é lá que o download acontece; mandar conferir nas notas do repositório é pedir para sair de onde o arquivo foi baixado.

Modelo canônico: `v0.1.0-alpha.3`. Antes de publicar, comparar o corpo montado com `gh release view v0.1.0-alpha.3 --json body` e confirmar que **as três seções de idioma e a seção de checksums** existem.

Cada corte leva **dois assets DLL** e é publicado como **pre-release** enquanto a linha for Alpha. Conferir o SHA-256 dos assets após o download.

Também atualizar, no mesmo corte: `CHANGELOG.md`, a versão em `Src/Extension/Version.Shared.props`, os três `README`, `Docs/Public/INSTALL.md` e `Docs/Public/DEMO.md` quando a entrega mudar comportamento visível ao consumidor. Os dois últimos entraram nesta lista em 2026-09-15: `INSTALL.md` manda conferir o menu **item a item** depois de instalar, e `DEMO.md` tabela os comandos disponíveis — um comando novo os desatualiza tanto quanto desatualiza os `README`, e nenhum dos dois estava aqui. Atenção ao escopo dentro do `INSTALL.md`: a verificação do **usuário final** descreve a DLL publicada e só muda no corte, enquanto a do **desenvolvedor / mantenedor** descreve a build deste repositório e muda junto com o código. Ao promover o bloco `[Unreleased]` a seção de versão, lê-lo antes como o leitor do release o lerá — a regra e o porquê estão em «Bloco `[Unreleased]` do `CHANGELOG.md`», na seção de promoção de frente.

Antes de publicar, conferir a régua de evidência (documento 15 §18.2, G6): toda entrada de
`[Unreleased]` que cite sessão de campo como prova por `Validated`/`Fixed` exige o documento
dedicado em `Docs/Implementation/` (ou remissão a um doc existente); entrada já publicada sem doc
só comporta citação sem reescrita.
Registrado em 2026-08-24, depois de o corte `0.1.0-alpha.4` sair com o corpo do release em português apenas, montado por cópia da nota pt-BR e com links relativos que não resolvem na página. A convenção trilíngue existia só como padrão nos cortes anteriores, sem estar escrita em lugar nenhum.

## Atualização manual da extensão para testes

Sempre que uma nova DLL precisar ser instalada para teste no GeneXus 18, o agente deve primeiro distinguir atualização de código de atualização de manifesto/registro.

Para atualização apenas da DLL canônica U14+, sem alteração em `Src/Extension/GenexusOpenApiBuilder.package`, na identidade do pacote ou no registro da extensão:

1. fechar completamente a IDE GeneXus;
2. executar `Install-ExtensionForGeneXus18.bat`, na raiz do repositório, usando **Executar como administrador**; quando a IDE estiver fora do diretório padrão, passar o diretório como primeiro argumento (por exemplo, `Install-ExtensionForGeneXus18.bat "C:\Program Files (x86)\GeneXus\GeneXus18up15"`);
3. abrir novamente a IDE e executar a validação funcional indicada para a frente.

O instalador canônico já executa `Tools/Test-InstalledExtension.ps1` ao final e falha quando a DLL instalada não corresponde à build atual.

Para atualização da DLL satélite U13, usar um fluxo separado:

1. fechar completamente a IDE GeneXus;
2. executar `Install-ExtensionForGx18u13.bat`, na raiz do repositório, usando **Executar como administrador** e passando o diretório da instalação U13 como primeiro argumento (por exemplo, `Install-ExtensionForGx18u13.bat "C:\Program Files (x86)\GeneXus\GeneXus18up13"`);
3. abrir novamente a IDE e executar a validação funcional indicada para a frente.

Esse BAT usa exclusivamente `artifacts/gx18u13/bin/Release/net471/GenexusOpenApiBuilder.Extension.dll` e valida o hash da mesma DLL instalada. Ele não registra o manifesto.

Quando houver, desde o ultimo `genexus /install` bem-sucedido, alteracao em `Src/Extension/GenexusOpenApiBuilder.package`, na identidade do pacote ou no registro da extensao, acrescentar entre os passos 2 e 3:

1. para U14+: executar `Register-ExtensionForGeneXus18.bat` normalmente, sem Administrador, passando o mesmo diretório quando ele não for o padrão; para U13: executar `Register-ExtensionForGx18u13.bat` (default `GeneXus18up13`), sem Administrador, passando o diretório só se a instalação U13 não estiver no path padrão;
2. no prompt aberto pelo segundo arquivo, digitar `genexus /install`, conferir a varredura e depois digitar `exit`.

- `Install-ExtensionForGeneXus18.bat` é o caminho operacional primário para a DLL canônica U14+; `Install-ExtensionForGx18u13.bat` é o caminho primário exclusivo da DLL satélite U13. `Register-ExtensionForGeneXus18.bat` é condicional à atualização de manifesto/registro na linha U14+; `Register-ExtensionForGx18u13.bat` é o equivalente dedicado da linha U13 (mesmo contrato: cmd normal, `genexus /install`, `exit`).
- O agente não executa esses arquivos nem altera `C:\Program Files (x86)\GeneXus`; apenas orienta a execução manual.
- Não substituir a instalação por uma chamada direta a `Tools/Copy-ExtensionForGeneXus18.ps1`. O `.ps1` é implementação interna exclusiva da etapa de cópia e validação; ele não registra a extensão.
- Ao avisar que chegou a hora de atualizar e testar, declarar explicitamente se o manifesto/registro mudou e solicitar `genexus /install` somente nesse caso.

## Registro de comandos no menu de contexto

Cada inclusão, alteração ou remoção de comando do menu de contexto deve manter sincronizadas, no mesmo passo, estas três camadas:

1. registro em runtime por `AddCommand(new CommandKey(...))` em `Src/Extension/Package.cs`;
2. `CommandDefinition` em `Src/Extension/GenexusOpenApiBuilder.package`;
3. `Command refid` dentro do grupo de comandos em `Groups` no mesmo manifesto, grupo que o submenu referencia.

- O ID deve ser exatamente igual nas três camadas.
- Para comandos de menu, registrar o ID em `Package.cs` como string literal no `CommandKey`, no formato `new CommandKey(Id, "Nome do Comando")`. O checker `Tools/Test-ExtensionCommandRegistration.ps1` valida esse contrato por leitura textual e não resolve constantes ou campos intermediários.
- O build bem-sucedido não comprova essa sincronização.
- Antes de gerar uma DLL para atualização manual, executar:

```powershell
pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1
```

- O menu principal deve manter somente comandos operacionais vigentes. O placeholder histórico `Futura Primeira Opção` foi removido quando o menu passou a ter `Configurar Preferências do Wizard` e `Wizard` como comandos permanentes; no menu de contexto da Transaction, expor `Wizard`, `Sincronizar com a Transaction`, `Remover API gerada` e `Recuperar operação interrompida`.
- No fechamento de uma sonda, remover seus comandos das três camadas e executar novamente o teste.

## Escrita na janela Output da IDE

Para mensagens da extensão na janela Output do GeneXus, reutilizar o padrão validado no B001/B020:

1. conferir `CommonServices.IsOutputAvailable`;
2. obter `CommonServices.Output`;
3. exigir `IOutputService2` e usar `DefaultOutputId`;
4. escrever com `AddLine(outputId, mensagem)`;
5. chamar `Show(outputId)` após escrever.

Não escrever em um Output customizado sem criação/seleção comprovada na IDE. O primeiro teste manual de B020 mostrou que o comando podia aparecer e executar sem mensagem visível quando a implementação tentava usar um `outputId` customizado. O Output padrão da IDE foi o caminho confirmado no U15.

## Promoção de frente e próximo passo

Ao concluir uma frente e promover a próxima ação, não atualizar apenas o checkpoint operacional. Antes de commitar, buscar no repositório inteiro pelo ID concluído, pelo ID seguinte e por expressões como `próxima frente`, `próxima missão`, `próxima ação`, `próxima responsabilidade operacional`, nomes de comandos adicionados/removidos e nomes de classes de sonda.

Quando a frente alterar o comportamento em runtime da extensão, revisar também comentários XML/C#, descrições de classe/método e termos de transição como `passivo`, `placeholder`, `temporário`, `sonda`, `runtime`, `manual`, `somente leitura`, `comando` e `protótipo`. Comentários e documentos devem declarar se o comando ou comportamento permanece, foi removido ou será absorvido por fluxo futuro.

Para cada ocorrência encontrada:

- atualizar quando ela afirmar um próximo passo que deixou de ser vigente;
- manter quando for range, histórico da própria frente ou referência governante ainda correta;
- registrar mentalmente a justificativa para flags descartadas, para reportar na revisão pré-push quando aplicável.

O checkpoint `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md` continua sendo a fonte canônica do próximo passo, mas documentos antigos não devem contradizê-lo com frases operacionais obsoletas.

Na varredura, tratar **contagens como termo de estado**: `dois defeitos`, `três correções`, `cinco cenários`, `N checkpoints`. Números envelhecem como qualquer afirmação — um defeito a mais encontrado no meio da frente deixa divergentes todos os documentos que citavam o total anterior. Varrer também os termos do que **ainda falta**, que é o que mais envelhece: `pendente`, `restam`, `falta`, `aguarda`, `ainda não`, `sem validação`. E, depois de editar um documento longo por trecho, varrer o **próprio arquivo** pelo fato que mudou, porque editar seção a seção deixa contradição interna.

### Documento de evidência de campo (B124)

Ao fechar uma frente/etapa — e antes de commitar a promoção da próxima ação —, conferir a régua
do documento 15 §18.2: fechamento sem documento dedicado de evidência em `Docs/Implementation/`
— ou sem a frase de dispensa `B124: sem documento dedicado porque <motivo objetivo>` no item do
checkpoint — é **gap P1 documental**. Vale também para sessão citada como prova por entrada
`Validated`/`Fixed` de release (G6). O agente é quem produz o documento na mesma sessão; o
contrato completo está no documento 15 §18.2, não reproduzido aqui.

### Bloco `[Unreleased]` do `CHANGELOG.md`

O bloco `[Unreleased]` é **rascunho do próximo release, não registro histórico**. Cada entrada descreve o estado no instante em que foi escrita, e as entradas seguintes mudam esse estado sem tocar nas anteriores — mas no corte todas serão lidas como um texto só, pelo leitor do release, que não acompanhou a cronologia interna.

Por isso, enquanto a versão não for publicada:

- **uma entrega, uma entrada.** Ao avançar dentro da mesma frente — validar o que estava sem validação, corrigir um defeito da própria entrega, medir o que era estimativa —, **editar a entrada existente** em vez de acrescentar outra ao lado;
- **classificar pela pergunta do leitor**, usando as seções que o arquivo já tem: `Added` para o que passou a existir, `Fixed` para defeito corrigido — dizendo se ele era visível em versão publicada ou se atinge mecanismo que estreia no mesmo bloco —, `Changed` para comportamento alterado, `Validated` para o que foi exercido na IDE;
- **afirmação de visibilidade se verifica contra a tag, nunca de memória.** Antes de escrever que um defeito existia «desde a `0.1.0-alpha.N`» — ou que é interno e nunca saiu —, localizar o commit que o introduziu (`git log -S '<trecho do código>' -- <arquivo>`) e conferir se ele é ancestral da tag (`git merge-base --is-ancestor <commit> <tag>`). Uma frente iniciada depois do último corte costuma introduzir **o defeito e a correção**, e nesse caso o defeito nunca chegou a ninguém. A conta é traiçoeira porque a frente parece antiga a quem a viveu: o que importa é a data da tag, não a da sprint;
- **ao fechar uma etapa, reler o bloco inteiro como se fosse o leitor do release**, não apenas a entrada que se acabou de escrever. É aqui que aparecem o «sem validação na IDE» numa entrega validada horas depois e o «defeito ainda aberto» logo abaixo da entrada que o fecha;
- a **cronologia interna** — quem descobriu o quê, quando, em que ordem — pertence ao log numerado do checkpoint e aos documentos de `Docs/Implementation/`, que são o lugar onde ela serve a alguém.

Depois de publicada, a seção da versão é imutável: correção posterior entra como entrada nova ou como remissão datada, nunca reescrevendo o que já saiu.

## Revisão pré-push do repositório

Antes de qualquer push:

**Aviso obrigatório:** a rotina pré-push deste repositório deve ser executada somente depois de a frente estar commitada. Não tratar execução sobre working tree suja como rotina pré-push válida; nesse caso, no máximo é diagnóstico intermediário. Primeiro criar o commit local da frente e só então executar os passos abaixo para revisar o intervalo commitado contra `origin/main`.

1. executar `git fetch origin` separadamente para atualizar `origin/main`;
2. na raiz do repositório, executar exatamente:

```powershell
pwsh -NoProfile -File scripts/Invoke-PrePushMechanicalChecks.ps1 -AsJson
```

3. ler `pushReadiness`, `incompleteReasons`, `manualRequired`, `warnings` e `notCovered` no JSON;
4. na resposta final da rotina, terminar sempre com uma frase explícita: `Sem impedimento para push.` quando `pushReadiness` estiver pronto, `behind=0`, working tree limpa, `manualRequired=[]`, `incompleteReasons=[]` e a revisão semântica não tiver gap bloqueante; caso contrário, terminar com `Com impedimento para push:` seguido do motivo objetivo; aviso `evidence-doc-required:*` no canal `warnings` não impede push, mas exige a conferência da régua de evidência declarada no relatório semântico.
5. concluir a revisão semântica exigida pelas instruções globais; `exit 0` mecânico não substitui essa revisão; fechamento de frente/etapa sem documento dedicado de evidência — ou sem a frase de dispensa `B124:` no item — é gap P1 documental (documento 15 §18.2).
6. quando o checker ou seu teste mudar, executar também `pwsh -NoProfile -File Tests/PrePushChecker/Test-OpenApiBuilderPrePushChecks.ps1`.

- `scripts/Invoke-PrePushMechanicalChecks.ps1` é o nome canônico e não deve divergir do contrato global.
- `manualRequired` no JSON **não** é a revisão semântica. Só dispara quando a `Próxima ação única` do checkpoint é um spike `B000`–`B006` e o intervalo menciona esse ID (checklist de encerramento de sonda). Lista vazia com próxima ação `B007+` é o comportamento esperado, não falso verde. Os avisos `evidence-doc-required:*` do canal `warnings` (B124) valem também para `B007+` e são outra coisa: sinalizam a conferência da régua de evidência, não bloqueiam push e não substituem a revisão semântica.
- Gaps confirmados, flags descartados e áreas não cobertas pertencem ao relatório da revisão semântica (passo 5), independentemente de `manualRequired`.
- Após alterar `.github/ISSUE_TEMPLATE/`, abrir uma vez o seletor **New issue** no navegador: o check `tests.issueForms` reduz o risco, mas só o GitHub confirma se o formulário aparece (YAML inválido some sem aviso).

**Afrouxamento de regra normativa é evento de risco, não detalhe de redação.** Quando o intervalo revisado transformar uma regra de `não existe` em `não existe por padrão`, de `deve` em `deveria`, de absoluto em condicional, ou passar a descrever comportamento no presente condicional, isso vira **verificação obrigatória contra o código** — mesmo que todos os documentos concordem entre si. A varredura semântica procura divergência; convergência na direção errada passa limpo por ela. Um critério de aceite que proíbe reprova uma implementação parcial; um que já prevê o caso não reprova nada.

Registrado em 2026-08-24, depois de três documentos normativos passarem a descrever, no presente condicional, um checkbox de `Delete` no Wizard e objetos `proc<Nome>_API_Delete` que **não existem** — a lista de serviços é fechada em `PrototypeWizardContract.ServiceNames`. O afrouxamento aumentou a coerência entre os documentos e, por isso, sobreviveu a três rodadas de revisão semântica; uma delas chegou a citar a redação afrouxada como texto vigente correto. Só caiu quando alguém foi ler o código em vez de comparar documentos entre si.

### Revisão semântica de contrato runtime

Além de buscar termos e inconsistências documentais, a revisão semântica pré-push deve reconstruir o fluxo runtime afetado pelo commit.

Para cada mudança que altere assinatura, `Source`, `Rules`, variáveis, parâmetros, chamada entre objetos, geração de artefatos ou estado persistido:

1. Identificar todos os produtores e consumidores do contrato alterado. Exemplos:
   - `Procedure.Rules.Source` com `parm(...)` alterado exige revisar todos os callers gerados;
   - `API.ServiceGroupSource.Source` alterado exige revisar variáveis, parâmetros e Procedures chamadas;
   - SDT alterado exige revisar variáveis, payloads, API Object e Procedures que usam o SDT.
2. Conferir se cada consumidor foi atualizado no mesmo commit. Build Release da extensão não valida semanticamente o código GeneXus gerado.
3. Conferir se o preflight cobre todos os objetos que serão gravados antes do primeiro `Save()`. Quando o fluxo grava mais de um objeto, procurar risco de escrita parcial em resolução de tipos, criação/remoção de variáveis, `Source`, `Rules`, objetos ausentes, colisões externas e validação pós-save.
4. Conferir se a evidência manual cobre o consumidor final do contrato. Se uma Procedure é chamada por API Object, validar também especificação/build do API Object, não só da Procedure.
5. Conferir a **data do gerador contra a data do artefato**. Evidência de runtime — smoke na IDE, `Build All`, chamada HTTP — vale para a DLL que a produziu, e só para ela. Sempre que um commit posterior tocar o emissor (`ApiPlan*`, writers de Source, mapa de contrato, plano de SDT), os objetos que ficaram na KB deixam de corresponder ao gerador vigente: não podem ser citados como estado atual nem reaproveitados como base de medição nova. Antes de planejar qualquer medição sobre artefatos que já estão na KB, comparar a data da captura com o último commit que alterou o emissor; havendo mudança no meio, o plano começa por reinstalar a DLL e reaplicar o Wizard. Evidência antiga permanece válida para o que provou **na data em que foi capturada** — é o alcance dela que expira, não o registro. **Escopo, acrescentado em 2026-09-15:** o que obriga a remedir é a medição que **sustenta uma decisão em aberto** — um orçamento vigente, um critério de aceite, um limiar que alguém vai comparar. Número publicado de frente encerrada é registro datado e não vence: o `CHANGELOG.md` tem dezenove marcas de tempo, todas de DLLs anteriores, e nenhuma delas pede remedição. Sem esse recorte a regra é lida como «todo número publicado vence a cada build», que ninguém pratica — e regra que ninguém pratica é regra que não protege nada.
6. Declarar no relatório da revisão:
   - contratos alterados;
   - produtores;
   - consumidores;
   - evidência de atualização de cada consumidor;
   - risco de escrita parcial encontrado ou descartado;
   - validação manual/runtime ainda faltante;
   - evidência de runtime cuja DLL precede alguma mudança de emissor no intervalo revisado.

Registrado em 2026-08-27, depois de a rotina pré-push planejar um smoke HTTP multinível "sobre a `apiTeste` de quatro níveis já gerada e buildada". A geração era de 2026-08-26 e o commit `8f80f39`, do dia seguinte, mudara a poda por papel, o mapa BC e a desambiguação de `VariableToken`. Executada como estava escrita, a bateria mediria o gerador anterior e o resultado pareceria válido. O defeito nasceu de tratar "os objetos já existem na KB" como economia, sem confrontar a data deles com a do emissor.

### GeneXus Open API Builder — trio API/Procedure/SDT

Quando a mudança afetar `API Object`, `Procedure`, `SDT`, `Rules parm`, `Variables` ou `ServiceGroupSource`, a revisão pré-push deve verificar obrigatoriamente o trio:

- assinatura declarada em `Procedure.Rules.Source`;
- chamada gerada em `API.ServiceGroupSource.Source`;
- variáveis declaradas em `API.Variables.Content.Content` e `Procedure.Variables`.

Qualquer divergência entre esses três pontos é gap P1 e bloqueia push.

## Fechamento de spikes e sondas temporárias

Antes de concluir e commitar qualquer item de spike `B000`–`B006`, o agente deve:

- distinguir a evidência histórica do comportamento que deve permanecer no runtime;
- remover eventos, comandos, menus e gatilhos temporários após a validação, salvo decisão explícita e documentada para mantê-los;
- não deixar sondas capazes de ler ou escrever automaticamente em qualquer KB;
- não deixar comandos experimentais de escrita disponíveis fora do escopo autorizado para o teste;
- preservar o código da sonda somente quando ele tiver valor técnico ou documental e garantir, por busca, que o runtime não o invoque;
- recompilar a extensão e solicitar ao usuário a reinstalação manual da DLL passiva, sem o agente alterar `C:\Program Files (x86)\GeneXus`;
- confirmar por teste de leitura que a DLL instalada coincide com a build e que a sonda encerrada não está mais registrada ou ativa;
- atualizar no mesmo fechamento o `CHANGELOG.md`, `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`, `Docs/Foundation/24-PLANO_IMPLEMENTACAO_REAL_POR_SPRINTS.md` e os documentos que ainda indiquem a frente encerrada como próxima;
- buscar no repositório inteiro o ID encerrado, o ID seguinte, os nomes dos comandos e os nomes das classes de sonda para localizar referências operacionais contraditórias;
- só considerar o marco pronto para revisão pré-push depois dessas validações.
