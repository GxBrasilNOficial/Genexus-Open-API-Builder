# B094 — Instalação por usuário externo sem administrador

## Estado

Concluído em 2026-08-10 no GeneXus 18 Upgrade 15 (`18.0.15.188745`): comprovação ponta a ponta, com o mantenedor na IDE, de que **não existe caminho estável** para um usuário externo instalar a extensão Alpha `0.1.0-alpha.1` **somente** via Extensions Manager (Add > Local) e `genexus /install` sem elevação, deixando a extensão marcada e com menus. O caminho que ativa menus continua sendo o dos instaladores do repositório: cópia elevada da DLL para `Packages` + registro sem elevação.

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

No U15 local, isso alinha com `Register-ExtensionForGeneXus18.bat`, que **recusa** execução elevada, e com a evidência histórica do B000 de que `/install` elevado pode não varrer pacotes. O atrito de administrador, quando existe, está na **escrita em** `C:\Program Files (x86)\GeneXus\...\Packages`, não no comando `/install` em si.

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

### 6. `genexus /install` após Add > Local (DLL já em Packages)

- Varredura sem elevação.
- Trecho decisivo:

```text
Scanning package 'GenexusOpenApiBuilder.Extension.dll'
Scanning package 'K2B.Packages.Editors.UI.dll'
Package 'K2B.Packages.Editors.UI.dll' added
```

- Pacotes válidos emitem `Package '...' added` (ou `Package Attribute not found`). A DLL desta extensão foi **apenas escaneada**, sem `added`.
- Reabertura da IDE: continua **desmarcada**.
- Teste residual: marcar de novo após esse `/install` → Apply → reinício → **travou de novo** → reabertura ainda **desmarcada**.

### 7. Controle positivo — fluxo dos `.bat` do repositório

1. `Install-ExtensionForGeneXus18.bat` como Administrador (só após matar processos GeneXus órfãos deixados pelos hangs).
2. `genexus /install` sem Administrador.
3. Reabertura: extensão **marcada**; menu principal `Genexus Open API Builder` antes de Help com os quatro comandos (`Configurar Preferências do Wizard`, `Wizard`, `Sincronizar com a Transaction`, `Remover API gerada`); submenu no contexto da Transaction com os três comandos aplicáveis.

## Resultado

**Não existe, nesta máquina/U15, caminho comprovado de usuário externo que ative a extensão (marcada + menus) sem escrita administrativa em `Packages` via o instalador controlado (ou equivalente manual elevado).**

Em letras claras:

1. Distribuir a **DLL** (o `.nupkg` não entra no Add > Local).
2. Add > Local com a DLL **pode copiar** o arquivo e listar a extensão, mas deixa **desmarcada**.
3. Marcar na UI **não estabiliza** a ativação (e o reinício pedido pode hangar, deixando processos `GeneXus` vivos).
4. `genexus /install` **não exige** administrador e **é necessário** no fluxo oficial/wiki, porém após Add > Local **não emitiu** `Package 'GenexusOpenApiBuilder.Extension.dll' added` e **não** produziu ativação.
5. O caminho comprovado que funciona: **cópia elevada** da DLL para `Packages` (`Install-ExtensionForGeneXus18.bat`) + **`genexus /install` sem elevação**.

## Fricções observadas

- Extensions Manager sem desinstalação; limpeza exige apagar a DLL de `Packages`.
- Hang recorrente em “Restart now?” após Apply de marcação no fluxo Add > Local.
- Processos GeneXus órfãos bloqueiam o instalador (“GeneXus aberto”).
- Diálogo Add > Local com erro genérico `Error installing extension` quando a extensão/DLL já existem.
- Coluna Description do Extensions Manager pode permanecer vazia (limitação já conhecida no B000); não bloqueia identificação por Nome/Fabricante/Versão.

## O que ficou sem comprovação

- Comportamento idêntico em GeneXus 18 U14.
- Add > Local em máquina **nunca** usada com esta extensão, por um usuário sem nenhuma permissão elevada (aqui a limpeza e o controle positivo usaram escrita em Program Files).
- Se algum dia o `/install` passar a emitir `Package '...GenexusOpenApiBuilder.Extension.dll' added` após Add > Local e isso ativar a marcação.
- Publicação/consumo do `.nupkg` por canal Web do Extensions Manager (fora do escopo Local).

## Critério de conclusão

- Artefato necessário identificado por inspeção de build/script e por teste na IDE — atendido.
- Sequência do usuário final tentada e observada passo a passo — atendida.
- Resposta objetiva sobre existência de caminho sem administrador para ativação completa — atendida: **não comprovado; o caminho estável exige cópia elevada + `/install` sem elevação**.
- Documentação pública não alterada nesta frente — atendido.
