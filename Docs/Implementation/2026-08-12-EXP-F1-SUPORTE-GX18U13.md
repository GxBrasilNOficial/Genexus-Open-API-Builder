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

## Pendência externa e próxima ação

O artefato ainda não foi enviado por este agente a nenhum canal externo. A próxima ação é o mantenedor disponibilizar a DLL acima ao Igor e registrar, na instalação GeneXus 18 U13, uma destas evidências:

1. a sonda carrega com `143920`, ou
2. o log informa `expecting version 'N'`, com o texto completo e o arquivo `packages.*.xml` correspondente.

Somente depois desse retorno `N` poderá ser usado como `PackageCompatibility` do satélite. A sonda não autoriza a Fase 2 nem prova carga, menu, Wizard ou Build All no U13.
