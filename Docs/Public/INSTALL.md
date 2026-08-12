# Instalação — Genexus Open API Builder

Guia para instalar a extensão no **GeneXus 18**. Validação principal (bateria completa) no **Upgrade 15**. O Upgrade 14 foi confirmado por usuário externo com a Alpha `0.1.0-alpha.1` (carregamento + geração); ver [evidência 2026-08-12](../Implementation/2026-08-12-EVIDENCIA-USUARIO-EXTERNO-U14-ALPHA.md).

Há dois caminhos:

1. **Usuário final** — só a DLL (sem clonar este repositório).
2. **Desenvolvedor / mantenedor** — clone, build e instaladores `.bat` deste repositório.

---

## Caminho do usuário final (só a DLL)

Fonte factual: evidência [B094](../Implementation/B094-INSTALACAO-APENAS-COM-A-DLL-SEM-CLONAR.md) no GeneXus 18 U15.

### Pré-requisitos

- GeneXus 18 instalado (caminho típico: `C:\Program Files (x86)\GeneXus`)
- A DLL `GenexusOpenApiBuilder.Extension.dll` da versão desejada (build Release `net471`)

### De onde vem a DLL

Obtenha o anexo `GenexusOpenApiBuilder.Extension.dll` no **GitHub Release** da versão (por exemplo a Alpha `0.1.0-alpha.1`). O arquivo `.nupkg` **não** é aceito pelo diálogo Add > Local (o botão OK fica desabilitado).

### Instalação

1. Com a IDE GeneXus **aberta**, use o **Extensions Manager** → **Add > Local** apontando para a DLL Release.
2. Confirme a instalação. A extensão deve aparecer na lista (nome/fabricante/versão), porém **desmarcada**.
3. **Não** tente ativar só marcando o checkbox na UI: a marcação **não persiste** entre reinícios. Quem registra a extensão é o `genexus /install`.
4. Feche **completamente** a IDE GeneXus.
5. Na pasta de instalação do GeneXus (tipicamente `C:\Program Files (x86)\GeneXus\GeneXus18`), execute:

```bat
genexus /install
```

6. Reabra a IDE e confira a verificação abaixo.

### Atualização (usuário final)

**Não comprovada** como guia operacional. No B094, Add > Local com a DLL **já presente** em `Packages` falhou com `Error installing extension`. A reinstalação limpa só funcionou depois de apagar `Packages\GenexusOpenApiBuilder.Extension.dll` (escrita em Program Files; tipicamente exige elevação), rodar `genexus /install` até a extensão sumir da lista e repetir o fluxo de instalação acima. Não inventamos um atalho: o caminho comprovado de atualização de DLL continua sendo o do mantenedor (`Install-ExtensionForGeneXus18.bat`).

### Validação da sequência publicada

Em 2026-08-11 o mantenedor reexecutou limpeza real (apagar a DLL em `Packages` + `genexus /install` até sumir da lista), Add > Local, fechar a IDE, `genexus /install` e verificação de menus no GeneXus 18 U15. A ordem deste guia segue essa evidência ([B094 §6](../Implementation/B094-INSTALACAO-APENAS-COM-A-DLL-SEM-CLONAR.md)).

Em 2026-08-12 um usuário externo (Igor C. Menin) instalou a DLL do Release `0.1.0-alpha.1` no GeneXus 18 U14 copiando-a para `Packages` e executando `genexus /install`, com menus e geração confirmados. Essa cópia manual é **variante observada**, não o caminho oficial deste guia. Evidência: [2026-08-12 — usuário externo U14](../Implementation/2026-08-12-EVIDENCIA-USUARIO-EXTERNO-U14-ALPHA.md); issue [#1](https://github.com/GxBrasilNOficial/Genexus-Open-API-Builder/issues/1).

### Atritos e o que ainda não foi comprovado

- Pode aparecer **UAC** no `genexus /install`. A escrita em `C:\Program Files (x86)\GeneXus\...\Packages` também exige permissão adequada. Instalação **sem elevação alguma** não foi comprovada.
- Add > Local por usuário externo em máquina **nunca** usada com esta extensão ainda não foi observado (o relato U14 usou cópia em `Packages`).
- Atualização só com Add > Local sobre DLL já instalada **não** está comprovada (única observação: falha; ver seção acima).
- **Marketplace / Add > Web** não são usáveis nesta máquina no estado observado em 2026-08-12 (feed/RSS quebrado pós-migração; 403 / erro de leitura). Não há guia operacional por esse canal.
- **GitHub Packages** (e o `.nupkg`) **não** instalam a extensão na IDE. Continuar usando o anexo **DLL** do GitHub Release. O workflow `.github/workflows/publish-github-packages.yml` pode republicar o assembly no feed NuGet da org a cada Release (ou via `workflow_dispatch`); isso é artefato técnico, não caminho de instalação.
- **ZIP:** Add > Local com `.zip` (DLL na raiz) + fechar IDE + `genexus /install` **funcionou** nesta máquina (equivalente à DLL). **Install from file** (Start Page) com o mesmo ZIP **falhou** (`Error installing extension`). O guia oficial permanece a **DLL** do Release; ZIP não é promovido como anexo obrigatório — ver [evidência 2026-08-12](../Implementation/2026-08-12-CANAIS-DISTRIBUICAO-MARKETPLACE-ZIP-GITHUB-PACKAGES.md).

### Verificação (usuário final)

Na IDE, o menu **Genexus Open API Builder** deve aparecer antes de **Help**, com:

- Configurar Preferências do Wizard
- Wizard
- Sincronizar com a Transaction
- Remover API gerada

No menu de contexto de uma Transaction, o submenu deve expor Wizard, Sincronizar e Remover.

---

## Caminho do desenvolvedor / mantenedor (repositório clonado)

### Pré-requisitos

- GeneXus 18 instalado (caminho típico: `C:\Program Files (x86)\GeneXus`)
- PowerShell 7 (`pwsh`) disponível no PATH
- Clone ou cópia deste repositório
- Build **Release** da extensão já gerada (ou gere com o comando abaixo)

### Gerar a build Release

Na raiz do repositório:

```powershell
dotnet build Src\GenexusOpenApiBuilder.sln --configuration Release
```

### Instalação da DLL

1. Feche **completamente** a IDE GeneXus.
2. Na raiz do repositório, execute `Install-ExtensionForGeneXus18.bat` com **Executar como administrador**.
3. Confira a mensagem de cópia e a validação de hash ao final.

O instalador copia a DLL, faz backup da anterior e confere se a instalação coincide com a build atual (`Tools/Test-InstalledExtension.ps1`).

### Registro na IDE (quando necessário)

Execute `Register-ExtensionForGeneXus18.bat` **somente** se, desde o último `genexus /install` bem-sucedido, houve alteração em:

- `Src/Extension/GenexusOpenApiBuilder.package`
- identidade do pacote
- registro de comandos do menu

Passos:

1. Execute `Register-ExtensionForGeneXus18.bat` normalmente (sem Administrador).
2. No prompt aberto, digite `genexus /install`, confira a varredura e digite `exit`.
3. Abra a IDE.

Se apenas a DLL mudou, o passo de registro não é necessário.

### Verificação (desenvolvedor / mantenedor)

Com a IDE fechada ou aberta, por leitura:

```powershell
pwsh -NoProfile -File Tools/Test-InstalledExtension.ps1
```

Na IDE, o menu **Genexus Open API Builder** deve aparecer antes de **Help**, com:

- Configurar Preferências do Wizard
- Wizard
- Sincronizar com a Transaction
- Remover API gerada

No menu de contexto de uma Transaction, o submenu deve expor Wizard, Sincronizar e Remover.

---

## Publicação em IIS (.NET Framework)

Se a API gerada for publicada em **IIS** com gerador **.NET Framework**, o verbo `PUT` (serviço `Update`) exige ajuste no handler `ExtensionlessUrlHandler-Integrated-4.0` no **nó do servidor** do IIS Manager. Detalhes e cuidados com WebDAV estão no [README](../../README.md#requisito-de-ambiente-put-delete-e-patch-em-iis).

O gerador **.NET** não apresenta esse comportamento.

## Próximo passo

Siga o roteiro curto em [DEMO.md](DEMO.md).

## Notas da Alpha

- Notas de release: [0.1.0-alpha.1](../Releases/0.1.0-alpha.1.md)
- Changelog: [CHANGELOG.md](../../CHANGELOG.md)
