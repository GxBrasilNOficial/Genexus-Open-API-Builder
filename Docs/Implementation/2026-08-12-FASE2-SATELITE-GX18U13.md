# Evidência — Fase 2 do satélite GeneXus 18 U13

**Data:** 2026-08-12

**Plano:** `Docs/Decisions/2026-08-12-PLANO_SUPORTE_PARALELO_GX18U13_OPCAO_B.md` v12

**Escopo:** esqueleto MSBuild do satélite, referências privadas da instalação U13 local e primeiro build Release.

## Entrada U13

- instalação: `C:\Program Files (x86)\GeneXus\GeneXus18up13`;
- executável: `GeneXus.exe` com `ProductVersion=18.0.13.186676` e `FileVersion=18.0.13.55604`;
- catálogo: `C:\ProgramData\GeneXus\GeneXus\18\packages.143920.xml`;
- catálogo: 54 entradas, todas com `CompatibilityVersion=143920`;
- `PackageCompatibility` adotado pelo satélite: `143920`.

O catálogo foi lido sem alterar a instalação. O arquivo `.package` do satélite mantém o mesmo ID, nome, grupos e recurso lógico do pacote canônico; a distinção U13 é feita pelo asset, pelo `GxLine` e pelas referências de build.

## Referências pinadas

Foram copiados somente os arquivos abaixo para `Src/Lib/Gx18u13/`, que é local e ignorado pelo git. `System.Drawing` e `System.Windows.Forms` continuam sendo referências do framework e não foram copiados.

| Arquivo | SHA-256 |
|---|---|
| `Artech.Architecture.Common.dll` | `7E5B3519FB815BC5CDEEC84280FBDDD7951A916E26778FCFB298011061371BF4` |
| `Artech.Architecture.UI.Framework.dll` | `FFD077207B183E485087567CD5930457D719F338CCAA82A10377871A383B9BFF` |
| `Artech.Common.Framework.dll` | `7D660FB9069D16608E2EA7BE65F8FE52AABDD1BB086EAF8735849C5A12D1E989` |
| `Artech.Common.Helpers.dll` | `E53A408C6D32CE42A42B8E1C2D3946A90639623C22F7F5DC5D0893A2982FFF18` |
| `Artech.Common.Properties.dll` | `DE81522261CFE640A24EF062F0B9B6BF80B5EEB39B3218D6E325596527832D82` |
| `Artech.Common.dll` | `0C6D7A207D197E9AA71426AE2FBA94C73DC9A36AC109FD724BC58BA15C4A4008` |
| `Artech.Genexus.Common.dll` | `2F10191DD82CE6CDD4B0019E7EFBCDF89A05AB3DA09FECFBB123C0BE848EB43A` |
| `Artech.Udm.Framework.dll` | `33F27378D68535E36118CD00255E66C3F9B687B3A2A6F89CA24CC02AE6613588` |
| `Newtonsoft.Json.dll` | `973FD70CE48E5AC433A101B42871680C51E2FEBA2AEEC3D400DEA4115AF3A278` |

As versões de arquivo dos oito assemblies Artech são `18.0.13.55604`; `Newtonsoft.Json.dll` é `10.0.3.21018`.

## Estrutura implementada

- `Src/Extension/GenexusOpenApiBuilder.Extension.Gx18u13.csproj`: `Microsoft.NET.Sdk`, `net471`, compile explícito das fontes compartilhadas, referências sem glob, manifesto embutido e `GxLine=Gx18u13`;
- `Src/Extension/Lib.Gx18u13.References.props`: lista explícita de `HintPath` relativos;
- `Src/Extension/Compile.Shared.props` e `Version.Shared.props`;
- `Src/GenexusOpenApiBuilder.Gx18u13.sln`;
- `Directory.Build.props`: lockfile, `bin` e `obj` do satélite isolados em `artifacts/gx18u13/`;
- `.gitignore`: `Src/Lib/Gx18u13/` e `artifacts/gx18u13/`.

O projeto canônico passou a importar apenas `Version.Shared.props`; não houve alteração no código de runtime nem no manifesto canônico.

## Build e validação offline

Comando:

```powershell
dotnet build Src\GenexusOpenApiBuilder.Gx18u13.sln -c Release
```

Resultado: build concluído com zero erros. O MSBuild emitiu um aviso `MSB3277` de unificação entre `mscorlib, Version=4.0.0.0` e as referências GeneXus sem versão explícita; as DLLs U13 foram preservadas sem regravação ou substituição.

Saídas:

- DLL de build: `artifacts/gx18u13/bin/Release/net471/GenexusOpenApiBuilder.Extension.dll`;
- asset: `artifacts/gx18u13/GenexusOpenApiBuilder.Extension-gx18u13.dll`;
- SHA-256 do asset: `05AADA095DF06BA47F2FBBF49265FDD75675E7333DC42E6422D20A26C2AC7D51`.

O inventário PEReader do asset retornou `Status=OK` com:

- `PackageCompatibility=143920`;
- `GxLine=Gx18u13` no `AssemblyInfo` gerado;
- entrada `GenexusOpenApiBuilder.Extension.Package` baseada em `AbstractPackageUI`;
- recurso `GenexusOpenApiBuilder.Extension.GenexusOpenApiBuilder.package`;
- manifesto `id=7be72bf4-8884-40dc-955d-ed9d31b69b74`;
- as 14 referências diretas esperadas, sem cópia das referências de framework.

Também passaram:

- build canônico Release sem erros e sem avisos;
- `Tools/Test-ExtensionCommandRegistration.ps1` com `Status=OK` e quatro comandos;
- `git check-ignore -v` para as duas áreas locais do satélite.

## Carga funcional U13 — confirmada com ressalva

A evidência recebida nesta sessão confirma a primeira carga manual do satélite na instalação `C:\Program Files (x86)\GeneXus\GeneXus18up13`:

- o `genexus /install` registrou `Scanning package 'GenexusOpenApiBuilder.Extension.dll'` e `Package 'GenexusOpenApiBuilder.Extension.dll' added`;
- o Extensions Manager exibiu `Genexus Open API Builder` marcado na lista de extensões;
- o menu principal `Genexus Open API Builder` apareceu com `Configurar Preferências do Wizard`, `Wizard`, `Sincronizar com a Transaction` e `Remover API gerada`.
- o menu de contexto da Transaction `Employee` exibiu o submenu `Genexus Open API Builder` com `Wizard`, `Sincronizar com a Transaction` e `Remover API gerada`;
- o Wizard foi executado na Transaction `Employee`, criou 15 objetos, atualizou 1, não bloqueou nenhum item e terminou com `Outcome='SuccessWithWarnings'`;
- o único aviso foi o fallback das descrições de serviço para inglês, porque a língua da KB ainda não foi validada pela API pública.

Essa evidência confirma a carga da extensão, os menus principal e de contexto e a geração pelo Wizard no U13. O aviso de idioma é não bloqueante e está registrado para não ser confundido com falha.

## Build funcional U13 — concluído com descoberta de contrato

O usuário executou `Build All` na Transaction `Employee` e o resultado foi sucesso: pattern generation, especificação de `WWEmployee`, `Employee` e `Employee_BC`, geração .NET Framework, compilação do DeveloperMenu e atualização do web config. Permaneceu somente o aviso conhecido `pmm0003` sobre atualização do módulo built-in `GeneXus` para `4.13.43`.

O Build All da Transaction não era suficiente para validar as Procedures da API, então foram executados builds focados. O resultado A/B foi determinante:

- removendo temporariamente somente `noaccept(EmployeeAddedDate);`, com `default(EmployeeAddedDate, &Today);` preservado, `procEmployee_API_Create` e `procEmployee_API_Update` passaram por especificação, geração e compilação;
- restaurando `noaccept(EmployeeAddedDate);`, as duas Procedures falharam com `spc0018`, informando que `EmployeeAddedDate` era read-only e não podia ser atribuído;
- a evidência detalhada e a correção implementada estão em `Docs/Implementation/2026-08-12-NOACCEPT-READONLY-BUSINESS-COMPONENT.md`.

O resultado confirma que o problema não deve ser ignorado nem tratado como particularidade do campo `Date`: a causa é a tentativa de atribuir via BC um atributo marcado `NoAccept`.

## Validação funcional U13 — concluída

1. a DLL satélite foi instalada e validada por hash com `Install-ExtensionForGx18u13.bat`;
2. B086 removeu os 12 objetos próprios da API anterior, preservando SDTs compartilhados e Business Component;
3. o wizard recriou a API com `Created=12`, `Updated=2`, `Blocked=0` e um único aviso de fallback de idioma;
4. o `Build All` especificou e compilou Create, Update, Get, List e `apiEmployee`, gerou SDTs e documentação REST e terminou com sucesso; não houve `spc0018`.

Regressão opcional: repetir a mesma validação no U15 com a DLL canônica quando a janela U13 estiver fechada.
