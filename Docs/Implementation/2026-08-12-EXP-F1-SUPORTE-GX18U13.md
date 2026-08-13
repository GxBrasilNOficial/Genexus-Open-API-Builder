# Fase 1 — Suporte paralelo GeneXus 18 U13

**Data:** 2026-08-12
**Plano:** `Docs/Decisions/2026-08-12-PLANO_SUPORTE_PARALELO_GX18U13_OPCAO_B.md` v12
**Escopo:** início da primeira fatia de execução (D43, Exp-ParidadeEmpacote, Exp-RefsNomes e preparação do Exp-Compat)

## Resultado resumido

A Fase 1 foi iniciada em isolamento, em paralelo explícito à Sprint 9. O projeto de produto não foi alterado, nenhum arquivo foi copiado da instalação U15 e nenhum artefato foi instalado na IDE.

O nome lógico do manifesto canônico foi medido, o rascunho de nomes de referências foi produzido e uma DLL-sonda mínima foi compilada com o SDK canônico. A parte que depende do GeneXus 18 U13 continua pendente e foi preparada para o contribuidor Igor.

## Exp-ParidadeEmpacote

Entrada local:

`C:\Dev\Knowledge\Genexus-Open-API-Builder\Src\Extension\bin\Release\net471\GenexusOpenApiBuilder.Extension.dll`

Resultado:

- SHA-256: `CC4A1CB0A58E5A3E677897B1BFFE0B97835DB61137E9CCD41F788CA67A08DFF9`
- tamanho: `555520` bytes;
- recurso `.package`: `GenexusOpenApiBuilder.Extension.GenexusOpenApiBuilder.package`;
- `id` do manifesto: `7be72bf4-8884-40dc-955d-ed9d31b69b74`;
- `name` do manifesto: `Genexus Open API Builder`;
- tipo de entrada: `GenexusOpenApiBuilder.Extension.Package`;
- classe-base: `Artech.Architecture.UI.Framework.Packages.AbstractPackageUI`;
- `PackageCompatibility` do build: `143920`.

A DLL instalada em `C:\Program Files (x86)\GeneXus\GeneXus18\Packages\GenexusOpenApiBuilder.Extension.dll` existe, mas não corresponde ao build medido: SHA-256 `3A5FD008B9B4D971D03DC10E50BF6C7D97813824FC5D6417498F4FDEC63D63EF`. Portanto, esta execução fecha somente o nome lógico do recurso e não constitui reteste de carga U15.

## Exp-RefsNomes

O rascunho descartável está em:

`C:\Dev\Knowledge\Genexus-Open-API-Builder\Temp\Exp-RefsNomes\Lib.Gx18u13.References.props`

Ele não é importado por nenhum projeto. Os nomes foram derivados dos namespaces usados e dos assets `compile` do `project.assets.json`/`packages.lock.json` canônicos:

| Origem usada | Nome de DLL candidato |
|---|---|
| `Artech.Architecture.Common.*` | `Artech.Architecture.Common.dll` |
| `Artech.Architecture.UI.Framework.*` | `Artech.Architecture.UI.Framework.dll` |
| `Artech.Common` | `Artech.Common.dll` |
| `Artech.Common.Framework.Commands` | `Artech.Common.Framework.dll` |
| `Artech.Common.Helpers` | `Artech.Common.Helpers.dll` |
| `Artech.Common.Properties` | `Artech.Common.Properties.dll` |
| `Artech.Genexus.Common.*` | `Artech.Genexus.Common.dll` |
| `Artech.Udm.Framework` | `Artech.Udm.Framework.dll` |
| `Newtonsoft.Json.*` | `Newtonsoft.Json.dll` |
| referências compartilhadas do projeto | `System.Drawing.dll`, `System.Windows.Forms.dll` |

O rascunho mantém esses nomes com `HintPath` relativo no formato que será usado pela Fase 2. A lista não é considerada fechada: os arquivos físicos, versões e hashes U13 só podem ser obtidos na instalação U13 do Igor (D44/D48). Não foi usado o `C:\Program Files (x86)\GeneXus` local.

### Inventário PE da DLL canônica

Foi criado o utilitário offline `Tools/Test-ExtensionAssemblyInventory.ps1`. Ele usa `PEReader`/`MetadataReader` para ler `AssemblyRef`, tipos e atributos, e abre somente o recurso incorporado para conferir o manifesto; não acessa IDE, KB, instalação do GeneXus ou rede.

O inventário da DLL canônica confirmou estas referências diretas:

```text
Artech.Architecture.Common
Artech.Architecture.UI.Framework
Artech.Common
Artech.Common.Framework
Artech.Common.Helpers
Artech.Common.Properties
Artech.Genexus.Common
Artech.Udm.Framework
Newtonsoft.Json
System.Drawing
System.Windows.Forms
```

`mscorlib`, `System` e `System.Core` também aparecem como referências do framework. As dependências transitivas do SDK — por exemplo `Artech.Architecture.BL.Framework`, `Artech.Architecture.Interfaces`, `Artech.Common.Controls`, `Artech.FrameworkDE`, `Artech.Udm.Architecture.Common` e `Artech.Udm.Layers.Common` — aparecem no lockfile, mas não no `AssemblyRef` direto da DLL. Por isso, não foram mantidas automaticamente na lista pinada preliminar; a compilação contra os arquivos U13 será a validação posterior dessa decisão.

O teste `Tests/ExtensionAssemblyInventory/Test-ExtensionAssemblyInventory.ps1` protege o inventário atual e falha se o recurso, a classe-base, o `PackageCompatibility` ou a lista direta de referências mudar sem revisão explícita.

## Exp-Compat

Fonte descartável:

`C:\Dev\Knowledge\Genexus-Open-API-Builder\Temp\Exp-Compat`

A sonda contém apenas uma classe `Package : AbstractPackageUI`, um manifesto mínimo sem comandos e o SDK `GeneXus.Package.UI.Sdk`. Ela não referencia o produto, não usa `Src\Lib\Gx18u13` e não altera `Package.cs`, o manifesto ou o `csproj` canônicos.

Comando executado:

```powershell
dotnet build Temp\Exp-Compat\ExpCompat.csproj -c Release
```

Resultado:

- build: sucesso;
- DLL: `C:\Dev\Knowledge\Genexus-Open-API-Builder\Temp\Exp-Compat\bin\Release\net471\GenexusOpenApiBuilder.ExpCompat.dll`;
- tamanho: `6656` bytes;
- SHA-256: `4B029AC30EC3D2BF8C02CF4A8574BE8F7457A6787DDA5F4EBE0F591CDD793F23`;
- recurso lógico: `GenexusOpenApiBuilder.ExpCompat.ExpCompat.package`;
- `PackageCompatibility` gerado pelo SDK: `143920`.

O build emitiu seis avisos `NU1900` porque os índices de vulnerabilidade de `pkgs.dev.azure.com` e `api.nuget.org` não estavam acessíveis. Não houve erro de restauração ou compilação.

### Validação offline da sonda

O mesmo inventário foi executado contra a sonda, parametrizando o nome do recurso, o tipo de entrada e o manifesto específicos dela:

```powershell
pwsh -NoProfile -File Tools\Test-ExtensionAssemblyInventory.ps1 `
  -DllPath Temp\Exp-Compat\bin\Release\net471\GenexusOpenApiBuilder.ExpCompat.dll `
  -ExpectedManifestResource GenexusOpenApiBuilder.ExpCompat.ExpCompat.package `
  -ExpectedEntryType GenexusOpenApiBuilder.ExpCompat.Package `
  -ExpectedEntryBaseType Artech.Architecture.UI.Framework.Packages.AbstractPackageUI `
  -ExpectedPackageCompatibility 143920 `
  -AsJson
```

Resultado: `Status=OK`, SHA-256 `4B029AC30EC3D2BF8C02CF4A8574BE8F7457A6787DDA5F4EBE0F591CDD793F23`, um recurso `.package`, manifesto `d147d99d-2dd5-4d8e-bb4d-68b1a61b01e5` e somente as referências esperadas para a sonda (`Artech.Architecture.Common`, `Artech.Architecture.UI.Framework` e `mscorlib`). Isso comprova que o artefato está internamente consistente antes do teste externo; não comprova que o U13 o aceite.

## Roteiro de entrega ao Igor

Quando for oportuno, entregar a DLL `Temp\Exp-Compat\bin\Release\net471\GenexusOpenApiBuilder.ExpCompat.dll` e pedir somente este registro:

1. No GeneXus 18 U13, usar `Add > Local` ou copiar a DLL para a pasta `Packages` da instalação e executar `genexus /install`.
2. Registrar qual caminho foi usado e se houve elevação.
3. Capturar se a sonda foi carregada; se não, copiar a mensagem completa, especialmente qualquer trecho `expecting version 'N'`.
4. Registrar o nome do arquivo `packages.*.xml` existente na instalação U13.

Não é necessário testar Wizard, menu, geração ou Build All nesta etapa: a sonda não contém comandos de produto. Também não é necessário executar o teste agora; o roteiro pode aguardar o horário adequado.

## Transição para a Fase 2 e próxima ação

A instalação local do GeneXus 18 U13 eliminou a dependência operacional do contribuidor para a coleta do SDK:

- executável: `C:\Program Files (x86)\GeneXus\GeneXus18up13\GeneXus.exe`, versão `18.0.13.186676`;
- catálogo: `C:\ProgramData\GeneXus\GeneXus\18\packages.143920.xml`;
- catálogo com 54 pacotes instalados e somente `CompatibilityVersion=143920`;
- os oito assemblies Artech e `Newtonsoft.Json` da lista pinada foram copiados para `Src/Lib/Gx18u13/` local, fora do git.

Com `N=143920` corroborado e a lista de nomes fechada, a Fase 2 foi iniciada. O satélite compila em `Src/GenexusOpenApiBuilder.Gx18u13.sln` e sua evidência está em [2026-08-12-FASE2-SATELITE-GX18U13.md](2026-08-12-FASE2-SATELITE-GX18U13.md).

A próxima ação é instalar manualmente a DLL de build `artifacts/gx18u13/bin/Release/net471/GenexusOpenApiBuilder.Extension.dll` na instalação U13 e confirmar carga da extensão. O asset renomeado `artifacts/gx18u13/GenexusOpenApiBuilder.Extension-gx18u13.dll` fica reservado para o pacote de Release. Esse teste ainda não foi executado por este agente porque alteraria a instalação protegida do GeneXus. Até a confirmação na IDE, permanecem abertos menu, Wizard e Build All no U13.
