# B010 — SDK e Build Mínimo

## Estado

Concluído originalmente em 2026-07-15 pelo método legado e revalidado em 2026-07-16 pelo método oficial para GeneXus 18 Upgrade 14 ou posterior. Esta evidência cobre somente a solução, o projeto e o pacote mínimos; o carregamento na IDE permanece no spike `B000`.

## Objetivo da evidência

Registrar de forma reproduzível a compilação da extensão mínima com os pacotes NuGet e os MSBuild SDKs oficiais do GeneXus. A instalação local do GeneXus 18 Upgrade 15 é somente ambiente de validação futuro, não fonte de referências de build.

## Decisão superadora

A página oficial informa que, a partir do GeneXus 18 Upgrade 14, o instalador do Platform SDK foi descontinuado. As assemblies de referência são distribuídas pelo feed GeneXus Azure Artifacts e os tipos de projeto são MSBuild SDKs. Esta decisão substitui, para U14+, a abordagem anterior de referências diretas a `Artech.Architecture.*` sob `C:\Program Files (x86)\GeneXus`.

Fonte oficial: [GeneXus Platform SDK Download](https://docs.genexus.com/en/wiki?27521,GeneXus+Platform+SDK+Download).

## Contrato de build versionado

- `nuget.config` fixa as fontes `genexus-build-sdk` oficial e `nuget.org`, sem herdar fontes da máquina;
- `global.json` fixa `GeneXus.Base.Sdk`, `GeneXus.Build.Sdk`, `GeneXus.Package.BL.Sdk` e `GeneXus.Package.UI.Sdk` em `3.0.0-beta5`;
- `Directory.Build.props` fixa `GeneXusPackageReferenceVersion` em `18.13.2`, `GeneXusSdkTargetFrameworks` em `net471` e habilita lockfile;
- `Src/Extension/GenexusOpenApiBuilder.Extension.csproj` usa `GeneXus.Package.UI.Sdk` e não contém `HintPath`, `Import` ou variável que aponte para a instalação do GeneXus;
- `Src/Extension/packages.lock.json` fixa as dependências transitivas restauradas.

O SDK UI requer sua cadeia de SDKs inferior (`Package.BL`, `Build` e `Base`); por isso todos constam no `global.json`. O exemplo da documentação apresenta `net471;net8.0` como padrão compartilhado, mas a compilação deste pacote de IDE confirmou `net471` como alvo compatível: `net8.0` falha por exigir plataforma Windows nas dependências UI atuais.

## Artefatos mínimos

- solution: `Src/GenexusOpenApiBuilder.sln`;
- projeto: `Src/Extension/GenexusOpenApiBuilder.Extension.csproj`;
- alvo compilado: `.NET Framework 4.7.1` (`net471`);
- pacote produzido: `Src/Packages/Release/GenexusOpenApiBuilder.Extension.0.1.0-preview.1.nupkg`;
- símbolos produzidos: `Src/Packages/Release/GenexusOpenApiBuilder.Extension.0.1.0-preview.1.snupkg`.

O `.nupkg` contém `lib/net471/GenexusOpenApiBuilder.Extension.dll`. Isso confirma o formato de saída do SDK de pacote, mas não prova ainda o mecanismo de instalação/descoberta pela IDE.

## Build reproduzível

Executar na raiz do repositório:

```powershell
dotnet restore Src\GenexusOpenApiBuilder.sln --locked-mode
dotnet build Src\GenexusOpenApiBuilder.sln --configuration Release --no-restore
```

Validação em 2026-07-16: `restore` e build `Release` concluídos com 0 avisos e 0 erros. A validação utiliza os feeds configurados e os arquivos versionados; não lê DLLs de `C:\Program Files (x86)\GeneXus`.

## Limites da evidência

- a versão de referência `18.13.2` e os SDKs `3.0.0-beta5` são as versões do exemplo oficial vigente; a compatibilidade prática deverá ser verificada tanto em U14 quanto no U15 local;
- não há ponto de entrada funcional, comando ou UI implementados;
- não foi copiado arquivo para a instalação do GeneXus;
- a instalação e o carregamento do pacote continuam pertencendo ao `B000`.

## Critério de encerramento

A solução e o pacote mínimos são restauráveis e compiláveis sem depender da instalação do GeneXus. O próximo passo técnico é identificar e testar manualmente o fluxo oficial de instalação do pacote, sem alterar uma Knowledge Base.