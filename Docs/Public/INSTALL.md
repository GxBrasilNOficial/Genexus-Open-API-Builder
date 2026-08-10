# Instalação — Genexus Open API Builder

Guia para instalar a extensão no **GeneXus 18**. Validação principal no **Upgrade 15**. O Upgrade 14 permanece residual e não bloqueia o uso da Alpha.

## Pré-requisitos

- GeneXus 18 instalado (caminho típico: `C:\Program Files (x86)\GeneXus`)
- PowerShell 7 (`pwsh`) disponível no PATH
- Clone ou cópia deste repositório
- Build **Release** da extensão já gerada (ou gere com o comando abaixo)

### Gerar a build Release

Na raiz do repositório:

```powershell
dotnet build Src\GenexusOpenApiBuilder.sln --configuration Release
```

## Instalação da DLL

1. Feche **completamente** a IDE GeneXus.
2. Na raiz do repositório, execute `Install-ExtensionForGeneXus18.bat` com **Executar como administrador**.
3. Confira a mensagem de cópia e a validação de hash ao final.

O instalador copia a DLL, faz backup da anterior e confere se a instalação coincide com a build atual (`Tools/Test-InstalledExtension.ps1`).

## Registro na IDE (quando necessário)

Execute `Register-ExtensionForGeneXus18.bat` **somente** se, desde o último `genexus /install` bem-sucedido, houve alteração em:

- `Src/Extension/GenexusOpenApiBuilder.package`
- identidade do pacote
- registro de comandos do menu

Passos:

1. Execute `Register-ExtensionForGeneXus18.bat` normalmente (sem Administrador).
2. No prompt aberto, digite `genexus /install`, confira a varredura e digite `exit`.
3. Abra a IDE.

Se apenas a DLL mudou, o passo de registro não é necessário.

## Verificação

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

## Publicação em IIS (.NET Framework)

Se a API gerada for publicada em **IIS** com gerador **.NET Framework**, o verbo `PUT` (serviço `Update`) exige ajuste no handler `ExtensionlessUrlHandler-Integrated-4.0` no **nó do servidor** do IIS Manager. Detalhes e cuidados com WebDAV estão no [README](../../README.md#requisito-de-ambiente-put-delete-e-patch-em-iis).

O gerador **.NET** não apresenta esse comportamento.

## Próximo passo

Siga o roteiro curto em [DEMO.md](DEMO.md).

## Notas da Alpha

- Notas de release: [0.1.0-alpha.1](../Releases/0.1.0-alpha.1.md)
- Changelog: [CHANGELOG.md](../../CHANGELOG.md)
