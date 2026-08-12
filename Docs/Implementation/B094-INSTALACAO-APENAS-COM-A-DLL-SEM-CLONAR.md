# B094 — Instalação apenas com a DLL (sem clonar o repositório)

## Estado

Concluído em 2026-08-10 no GeneXus 18 Upgrade 15 (`18.0.15.188745`), com **correção de evidência em 2026-08-11**: a captura incompleta do `/install` no primeiro fechamento levou a um argumento decisivo errado (“só `Scanning`, sem `added`”). Com captura confiável e redo do cenário Add > Local, o pacote **é** reconhecido (`Package '...' added`) e Add > Local + `genexus /install` **ativou** a extensão (marcada + menus) nesta máquina — ainda com atrito de elevação (UAC no `/install` a partir de cmd normal em Program Files; escrita em `Packages` via Add > Local).

Caminho **sem elevação alguma** continua **não** comprovado. O fluxo dos instaladores do repositório (cópia elevada da DLL + `/install`) permanece caminho estável de mantenedor.

Esta evidência **não** reescreve `README.md`, `Docs/Public/INSTALL.md`, `Docs/Public/DEMO.md` nem `Docs/Releases/`. A atualização da documentação pública é frente separada.

## Objetivo

Descobrir e comprovar qual artefato distribuir e qual sequência um usuário externo pode seguir para instalar a extensão no GeneXus 18 **sem clonar o repositório** e **sem executar os `.bat` como administrador**, registrando limitações reais em vez de um caminho não comprovado.

## Artefato a distribuir

| Artefato | Papel |
|---|---|
| `GenexusOpenApiBuilder.Extension.dll` (Release `net471`) | **Único binário necessário** em `Packages`. É o que `Tools/Copy-ExtensionForGeneXus18.ps1` copia. |
| `GenexusOpenApiBuilder.Extension.0.1.0-alpha.1.nupkg` | Empacota a **mesma** DLL em `lib/net471/`; grupo de dependências NuGet vazio para `net471`. Útil como veículo de distribuição/build, **não** como entrada do Add > Local. |
| PDB / outras DLLs do projeto | Não são copiadas pelo instalador controlado; a pasta `bin\Release\net471` contém só DLL + PDB. |

Hash SHA-256 da build usada nesta sessão (idêntica no `bin` e dentro do `.nupkg`):

`3A5FD008B9B4D971D03DC10E50BF6C7D97813824FC5D6417498F4FDEC63D63EF`

A DLL incorpora o manifesto `.package`, declara ponto de entrada UI e contém `PackageCompatibility` com versão `143920` (inteiro no assembly; não é o bug antigo de versão `0` do B000).

## Wiki oficial e elevação de `genexus /install`

Fonte: [HowTo: Install GX extensions](https://docs.genexus.com/en/wiki?7623,HowTo:+Install+GX+extensions) (válido para GeneXus 18).

A wiki descreve instalação manual como: colocar as `.dll` em `Packages` e executar `genexus /install` (varre `Packages` e registra; não abre a IDE). **Não exige administrador** para `/install`.

No U15 local, `Register-ExtensionForGeneXus18.bat` **recusa** execução elevada (contrato do wrapper do repositório). A premissa histórica do B000 de que “`/install` elevado pode não varrer pacotes” ficou **refutada** em 2026-08-11: em cmd já elevado, `/install` varreu `Packages`, emitiu `Package '...' added` para esta extensão e também publicou/instalou módulos NuGet em `.gxmodules`. O atrito de elevação observado nesta máquina inclui:

1. escrita em `C:\Program Files (x86)\GeneXus\...\Packages` (Add > Local / cópia manual);
2. UAC ao iniciar `genexus /install` a partir de cmd **normal** na pasta de instalação (janela elevada `GeneXus.com` separada).

Não se afirma que a ativação “só funciona se o cmd do `/install` já estiver elevado”: no redo Add > Local a ativação veio do fluxo com UAC a partir de cmd normal. O cmd já elevado serviu para captura confiável do log.

## Sequência executada e observações

Ambiente: Windows 11; GeneXus 18 U15; extensão Alpha `0.1.0-alpha.1`; fabricante esperado `GxBrasilNOficial`.

### 1. Estado limpo parcial (desmarcar)

- Extensão desmarcada no Extensions Manager → Apply → fechar/reabrir.
- Menus da extensão sumiram.
- Extensão **permaneceu listada** (não há opção de desinstalar no U15).
- Conclusão parcial: desmarcar não remove o registro/arquivo.

### 2. Tentativa A — Add > Local com `.nupkg`

- Arquivo: `Src\Packages\Release\GenexusOpenApiBuilder.Extension.0.1.0-alpha.1.nupkg`
- Resultado: botão **OK desabilitado**. O diálogo Local não aceita esse artefato.

### 3. Tentativa B — Add > Local com DLL (ainda com extensão listada/desmarcada)

- Arquivo: `Src\Extension\bin\Release\net471\GenexusOpenApiBuilder.Extension.dll`
- OK habilitado → “Install now?” → **Error installing extension** (sem detalhe útil no diálogo; `GXLogging.log` não registrou o erro).
- Hipótese: tentativa de recopiar sobre DLL já presente em `Packages` / estado já registrado.

### 4. Limpeza real e Add > Local em estado limpo

1. GeneXus fechado.
2. Usuário apagou `Packages\GenexusOpenApiBuilder.Extension.dll` (escrita em Program Files; tipicamente exige permissão elevada).
3. `genexus /install` sem elevação → extensão **sumiu da lista**.
4. Add > Local com a mesma DLL → instalação sem a mensagem de erro anterior.
5. Extensão voltou à lista com fabricante/versão corretos, porém **desmarcada**.
6. DLL reapareceu em `Packages` com o mesmo SHA-256 da build.

### 5. Tentativa de ativação só pela UI

- Marcar → Apply → “Restart now?”.
- Em mais de uma tentativa, o reinício pedido **travou**; foi necessário encerrar pelo Gerenciador de Tarefas.
- Após reabertura (fechamento forçado ou fechamento normal após responder Não ao reinício), a extensão **seguiu desmarcada**. A marcação **não persiste**.

### 6. `genexus /install` após Add > Local — correção de evidência (2026-08-11)

A versão de 2026-08-10 deste passo registrou apenas `Scanning package 'GenexusOpenApiBuilder.Extension.dll'` sem `added`, e concluiu que o pacote não era reconhecido. Essa observação veio de captura incompleta: em cmd normal, `genexus /install` abre janela própria que roda e fecha sem pausa; o documento não registrava o método de captura.

#### 6.A Redo do cenário Add > Local (cmd normal + redirect)

1. Estado limpo: DLL apagada de `Packages`, `/install`, extensão sumiu da lista.
2. Add > Local com a DLL Release → listada e **desmarcada**.
3. GeneXus fechado (sem órfãos).
4. Em cmd **normal**, na pasta GeneXus18:

```bat
genexus /install > C:\Temp\gxinstall.log 2>&1
```

5. Apareceu autorização de elevação (UAC). O trabalho real ocorreu em janela elevada `GeneXus.com` (Publishing/Installing module / `nuget.exe` para vários módulos em `.gxmodules`).
6. `C:\Temp\gxinstall.log` ficou **0 KB**. `findstr /i "GenexusOpenApiBuilder" C:\Temp\gxinstall.log` não retornou linhas: o redirect no processo pai **não captura** a saída do processo elevado filho.
7. Reabertura da IDE: extensão **marcada**; menu `Genexus Open API Builder` visível.

Conclusão deste momento: no cenário Add > Local + `/install` (com UAC), a extensão **ativou**. O log redirecionado neste modo **não** é evidência do texto `added`/`Scanning`.

#### 6.B Captura confiável (cmd já elevado)

Com a DLL já em `Packages`, em cmd **Administrador** (sem segundo UAC):

```bat
cd /d "C:\Program Files (x86)\GeneXus\GeneXus18"
genexus /install > C:\Temp\gxinstall-elevated.log 2>&1
findstr /i "GenexusOpenApiBuilder" C:\Temp\gxinstall-elevated.log
```

Trecho literal:

```text
Scanning package 'GenexusOpenApiBuilder.Extension.dll'
Package 'GenexusOpenApiBuilder.Extension.dll' added
```

O pacote **é** reconhecido e adicionado. A falha de método do passo 6 original (e do redirect em cmd normal) explica o argumento errado de 2026-08-10; não um “não-reconhecimento” do pacote.

### 7. Controle positivo — fluxo dos `.bat` do repositório

1. `Install-ExtensionForGeneXus18.bat` como Administrador (só após matar processos GeneXus órfãos deixados pelos hangs).
2. `genexus /install` sem Administrador (contrato do `Register-ExtensionForGeneXus18.bat`).
3. Reabertura: extensão **marcada**; menu principal `Genexus Open API Builder` antes de Help com os quatro comandos (`Configurar Preferências do Wizard`, `Wizard`, `Sincronizar com a Transaction`, `Remover API gerada`); submenu no contexto da Transaction com os três comandos aplicáveis.

## Resultado

Nesta máquina/U15:

1. Distribuir a **DLL** (o `.nupkg` não entra no Add > Local).
2. Add > Local com a DLL **copia** o arquivo e lista a extensão, mas deixa **desmarcada**.
3. Marcar na UI **não estabiliza** a ativação (e o reinício pedido pode hangar, deixando processos `GeneXus` vivos). Observação **não** contestada.
4. Após Add > Local, `genexus /install` **reconhece** o pacote (`Package 'GenexusOpenApiBuilder.Extension.dll' added`, captura elevada) e, no redo com UAC a partir de cmd normal, **ativou** marcada + menus. O argumento de 2026-08-10 (“só Scanning, sem `added`, sem ativação”) estava **errado**.
5. Existe caminho **sem clonar o repositório e sem `Install-*.bat`** que chegou a marcada + menus: Add > Local + `genexus /install`. Esse caminho **não** comprovou instalação **sem elevação** (UAC no `/install`; escrita em `Packages`).
6. O fluxo dos `.bat` (cópia elevada + `/install` conforme wrappers) permanece caminho estável de mantenedor.

## Fricções observadas

- Extensions Manager sem desinstalação; limpeza exige apagar a DLL de `Packages`.
- Hang recorrente em “Restart now?” após Apply de marcação no fluxo Add > Local. Hipótese plausível (não causa única comprovada): o `/install` / reinício coincide com trabalho pesado de publicação e instalação de módulos NuGet em `.gxmodules`, observado na janela elevada.
- Processos GeneXus órfãos bloqueiam o instalador (“GeneXus aberto”).
- Diálogo Add > Local com erro genérico `Error installing extension` quando a extensão/DLL já existem.
- Coluna Description do Extensions Manager pode permanecer vazia (limitação já conhecida no B000); não bloqueia identificação por Nome/Fabricante/Versão.
- Redirect `> arquivo 2>&1` a partir de cmd normal **falha** quando há UAC e janela filha elevada: log fica vazio.

## O que ficou sem comprovação

- Add > Local em máquina **nunca** usada com esta extensão, por um usuário sem nenhuma permissão elevada.
- Se Add > Local eleva internamente a cópia para `Packages` (mecanismo exato não instrumentado).
- Causa da não-persistência da marcação **somente** pela UI (hipótese não comprovada: permissão de escrita no registro/estado da IDE — não inventar como fato).
- Causa única dos hangs em “Restart now?” (trabalho de módulos NuGet é hipótese, não prova).

## Nota de revisão — U14 por usuário externo (2026-08-12)

O item “comportamento idêntico em GeneXus 18 U14” ficou **fechado com evidência externa**: usuário Igor C. Menin, DLL do Release `0.1.0-alpha.1`, cópia em `Packages` + `genexus /install`, menus e geração na KB `KbTesteGx18U14`. Não substitui o caminho Add > Local documentado neste B094. Evidência: [2026-08-12 — usuário externo U14](2026-08-12-EVIDENCIA-USUARIO-EXTERNO-U14-ALPHA.md); issue [#1](https://github.com/GxBrasilNOficial/Genexus-Open-API-Builder/issues/1).

## Nota de revisão — canais adicionais (2026-08-12)

O item aberto “Publicação/consumo do `.nupkg` por canal Web do Extensions Manager” ficou **fechado com evidência negativa**:

- não há canal do Extensions Manager que consuma `.nupkg`;
- GitHub Packages não instala a extensão na IDE;
- o canal Web depende do Marketplace/RSS, inutilizável nesta máquina após a migração observada;
- Add > Local aceita `*.dll` | `*.zip`; Install from file (Start Page) aceita só `.zip`. Com ZIP só-DLL: Install from file **falhou**; Add > Local + `genexus /install` **ok** (menus). ZIP não é caminho oficial público; a DLL do Release permanece canônica — ver evidência abaixo.

Evidência: [2026-08-12 — Canais de distribuição](2026-08-12-CANAIS-DISTRIBUICAO-MARKETPLACE-ZIP-GITHUB-PACKAGES.md).

## Critério de conclusão

- Artefato necessário identificado por inspeção de build/script e por teste na IDE — atendido.
- Sequência do usuário final tentada e observada passo a passo — atendida.
- Resposta objetiva sobre caminho sem clonar/`Install-*.bat` — atendida com correção 2026-08-11: **comprovado** Add > Local + `/install` ativando nesta máquina, **ainda com elevação**; caminho **sem elevação alguma** não comprovado.
- Documentação pública não alterada nesta frente — atendido.
