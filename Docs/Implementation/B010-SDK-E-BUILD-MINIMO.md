# B010 — SDK e Build Mínimo

## Estado

Concluído em 2026-07-15. Esta evidência cobre somente a solução e o projeto mínimos; o carregamento da extensão na IDE permanece no spike `B000`.

## Objetivo da evidência

Registrar de forma reproduzível como o Extensibility SDK do GeneXus 18 Upgrade 15 foi localizado, referenciado e usado para compilar a solution e o projeto mínimos da extensão.

## Ambiente-base

- sistema operacional: Windows 11 x64 (10.0.26200).
- versão e upgrade do GeneXus: GeneXus 18 Upgrade 15.
- origem e forma de obtenção do SDK: instalação local oficial do GeneXus, consultada somente em modo leitura.
- versão efetivamente encontrada: build `18.0.15.188745`; assemblies `Artech.Architecture.*` na versão `11.0.0.0`.
- pré-requisitos instalados: .NET Framework 4.8 Targeting Pack e .NET SDK 10.0.302 (MSBuild 18.6.11).

O resultado deve ser reproduzível em outra máquina Windows que tenha o GeneXus 18 Upgrade 15 e os pré-requisitos registrados. Caminhos absolutos específicos da máquina podem aparecer como evidência local, mas não podem ser necessários nem versionados como configuração do build.

## Localização reproduzível das dependências

- método de descoberta: verificar `Artech.Architecture.Common.dll`; a convenção padrão é `$(MSBuildProgramFiles32)\GeneXus\GeneXus18`.
- assemblies necessários: `Artech.Architecture.Common.dll`, `Artech.Architecture.BL.Framework.dll` e `Artech.Architecture.UI.Framework.dll`.
- mecanismo usado pelo projeto para resolver as referências: `Src/Extension/GeneXusSdk.props` aplica a convenção padrão e falha com mensagem objetiva se o SDK não for localizado.
- variáveis, propriedades ou convenções de caminho: em instalação não padrão, executar com `/p:GeneXusInstallDir="C:\caminho\para\GeneXus18"`. Nenhum caminho absoluto específico da máquina é versionado.

## Artefatos mínimos

- solution esperada: `Src/GenexusOpenApiBuilder.sln`.
- projeto esperado: `Src/Extension/GenexusOpenApiBuilder.Extension.csproj`.
- target framework confirmado durante `B010`: `.NET Framework 4.8` (`net48`).
- tipo de projeto e empacotamento confirmados durante `B010`: biblioteca de classes mínima; o empacotamento e o manifesto de carregamento serão validados no `B000`.

## Build reproduzível

- comando executado: `dotnet restore Src/GenexusOpenApiBuilder.sln` e `dotnet build Src/GenexusOpenApiBuilder.sln --configuration Release --no-restore`.
- diretório de execução: raiz do repositório.
- resultado: sucesso, 0 erros; um aviso `MSB3277` de conflito de `mscorlib` nas dependências legadas do SDK.
- data da validação: 2026-07-15.

## Evidência

- trecho relevante do log: `0 Erro(s)`; o build resolve as três referências `Artech.Architecture.*` a partir da instalação do GeneXus.
- arquivos produzidos: `Src/Extension/bin/Release/net48/GenexusOpenApiBuilder.Extension.dll` e `.pdb`.
- observações e limitações: a solução confirma a resolução e a compilação das referências públicas do SDK, sem declarar ainda um ponto de entrada ou compatibilidade de carregamento na IDE. O aviso de `mscorlib` deve ser reavaliado durante o spike `B000`.

## Critério de encerramento

Todas as seções aplicáveis foram preenchidas e o build mínimo foi bem-sucedido. O checkpoint foi atualizado para promover `B011`. O carregamento da extensão na IDE pertence a `B000`, não a `B010`.