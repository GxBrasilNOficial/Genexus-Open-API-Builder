# B010 — SDK e Build Mínimo

## Estado

Concluído originalmente em 2026-07-15 pelo método legado e revalidado em 2026-07-16 pelo método oficial U14+. Esta evidência encerra apenas a preparação de build: não comprova compatibilidade prática, descoberta ou carregamento em GeneXus 18 U14 ou U15. Essas verificações pertencem ao `B000`.

## Objetivo da evidência

Registrar um projeto mínimo restaurável e compilável sem usar DLLs da instalação do GeneXus, e preservar com precisão o que a documentação oficial, o build e as decisões do projeto realmente sustentam.

## Classificação das afirmações

| Classe | Afirmação | Fonte ou limite |
|---|---|---|
| Fato oficial | A partir de GeneXus 18 Upgrade 14, o instalador do Platform SDK não é fornecido; as assemblies de referência são distribuídas no feed GeneXus Azure Artifacts e os tipos de projeto são MSBuild SDKs. | [GeneXus Platform SDK Download](https://docs.genexus.com/en/wiki?27521,GeneXus+Platform+SDK+Download). |
| Fato oficial | `GeneXus.Package.UI.Sdk` é o SDK indicado para pacotes UI, incluindo extensões de IDE com componentes visuais. | Mesma documentação oficial. |
| Fato de build | A resolução do SDK UI exigiu que `Package.BL`, `Build` e `Base` também estivessem fixados no `global.json`. | Erro de resolução inicial e build posterior bem-sucedido. |
| Fato oficial | A página oficial exemplifica `GeneXusPackageReferenceVersion` `18.13.2`, os SDKs `3.0.0-beta5` e `net471;net8.0`. | Exemplo vigente da documentação, não matriz de compatibilidade por upgrade. |
| Fato de build | Este repositório restaurou e compilou em `net471` com os SDKs/pacotes fixados abaixo, gerando `.nupkg` e `.snupkg`. | Validação local de 2026-07-16. |
| Fato de build | A tentativa inicial de incluir `net8.0` falhou com `NETSDK1136`, por dependência UI que requer alvo Windows; `net471` foi o alvo que compilou sem avisos. | Não prova que `net8.0-windows` ou outra configuração futura seja impossível. |
| Inferência pendente | O pacote atual será descoberto e carregado por U14 ou U15. | Ainda não instalado nem testado na IDE. |
| Decisão de produto | O baseline suportado do MVP é U14+; U15 é o primeiro ambiente disponível para validação. | Aprovada pelo mantenedor em 2026-07-16; exige teste antes de anunciar suporte prático. |

## Racional da versão mínima U14+

U14 foi escolhido porque é o primeiro upgrade para o qual a documentação oficial estabelece o mecanismo moderno único de build. Isso permite que o repositório tenha uma única configuração versionada de feed NuGet, MSBuild SDKs e lockfile.

GeneXus 18 U13 e anteriores não foram declarados incompatíveis. Eles podem, em tese, ser atendidos pelo Platform SDK legado; porém isso exigiria manter, documentar e testar uma segunda cadeia de build e de empacotamento. O MVP não assumiu esse custo. A exclusão de U13 e anteriores é, portanto, uma decisão deliberada de escopo e manutenção — não uma conclusão técnica de que uma extensão não possa existir nesses upgrades.

A existência do mecanismo moderno desde U14 não equivale a compatibilidade prática do produto com U14. O carregamento real continua pendente tanto em U14 quanto no U15 local. O mantenedor pode rever o baseline se preferir aceitar a manutenção do caminho legado ou se os testes práticos refutarem a hipótese U14+.

## Contrato de build versionado

- `nuget.config` fixa as fontes `genexus-build-sdk` oficial e `nuget.org`, sem herdar fontes da máquina;
- `global.json` fixa `GeneXus.Base.Sdk`, `GeneXus.Build.Sdk`, `GeneXus.Package.BL.Sdk` e `GeneXus.Package.UI.Sdk` em `3.0.0-beta5`;
- `Directory.Build.props` fixa `GeneXusPackageReferenceVersion` em `18.13.2`, `GeneXusSdkTargetFrameworks` em `net471` e habilita lockfile;
- `Src/Extension/GenexusOpenApiBuilder.Extension.csproj` usa `GeneXus.Package.UI.Sdk` e não contém `HintPath`, `Import` ou variável que aponte para a instalação do GeneXus;
- `Src/Extension/packages.lock.json` fixa as dependências transitivas restauradas.

### Motivos das escolhas versionadas

`GeneXus.Package.UI.Sdk` foi escolhido pela classificação oficial para pacotes UI/extensões de IDE. Nesta frente, foi a escolha aplicada que produziu o pacote NuGet mínimo sem referências diretas a `C:\Program Files (x86)\GeneXus`.

As referências `18.13.2` e os SDKs `3.0.0-beta5` foram fixados porque são os valores publicados no exemplo oficial vigente. Fixá-los e registrar o lockfile evita que um restore futuro derive versões transitivas diferentes. Não há nesta evidência uma tabela oficial que prove que esses valores são a combinação certificada para U14 ou para o build local U15; essa compatibilidade é hipótese a testar.

`net471` foi mantido porque a build local foi bem-sucedida nesse alvo. O `net8.0` do exemplo oficial não foi mantido após a falha observada; não houve investigação de `net8.0-windows` porque o objetivo de B010 é o menor pacote compilável, não uma matriz de frameworks.

## Ambiente de validação

- sistema operacional: Windows 11 x64 (10.0.26200);
- comando `dotnet` usado: .NET SDK `10.0.302` (MSBuild `18.6.11`);
- GeneXus instalado localmente: 18 Upgrade 15, build `18.0.15.188745`, consultado somente em modo leitura.

A versão do .NET SDK foi registrada como evidência, mas não foi fixada na seção `sdk` de `global.json`. O arquivo é necessário aqui para resolver os MSBuild SDKs GeneXus; impor também um SDK .NET específico seria uma decisão adicional de ambiente, ainda não avaliada para contribuidores. Assim, a reprodutibilidade comprovada nesta frente é a do grafo de pacotes e do build na versão registrada, não uma garantia de que qualquer SDK .NET futuro produzirá o mesmo resultado.

## Artefatos produzidos

- solution: `Src/GenexusOpenApiBuilder.sln`;
- projeto: `Src/Extension/GenexusOpenApiBuilder.Extension.csproj`;
- alvo compilado: `.NET Framework 4.7.1` (`net471`);
- pacote: `Src/Packages/Release/GenexusOpenApiBuilder.Extension.0.1.0-preview.1.nupkg`;
- símbolos: `Src/Packages/Release/GenexusOpenApiBuilder.Extension.0.1.0-preview.1.snupkg`.

Na evidência original de B010, o `.nupkg` continha apenas `lib/net471/GenexusOpenApiBuilder.Extension.dll` e metadados NuGet, sem manifesto `.package` nem ponto de entrada de extensão. Isso não foi falha de B010: sua missão era obter o menor pacote compilável. O B000 posterior criou esses elementos, confirmou o carregamento no U15 e registrou a evidência em `Docs/Implementation/B000-CARREGAMENTO-IDE.md`.

## Build reproduzível

Executar na raiz do repositório:

```powershell
dotnet restore Src\GenexusOpenApiBuilder.sln --locked-mode
dotnet build Src\GenexusOpenApiBuilder.sln --configuration Release --no-restore
```

Validação em 2026-07-16: restore em modo bloqueado e build `Release` concluíram com 0 avisos e 0 erros. A validação prova restauração, resolução da cadeia de SDKs, compilação de `net471` e geração dos artefatos acima. Não prova instalação, descoberta, carregamento, comandos, UI, acesso à KB ou compatibilidade prática com U14/U15.

## Limites da evidência B010 e continuidade posterior

- a evidência B010, isoladamente, não comprovava ponto de entrada funcional, comando, UI ou instalação na IDE;
- o B000 posterior criou o manifesto e o ponto de entrada, validou o procedimento de cópia/registro e comprovou o carregamento no U15;
- nenhum resultado de B010 ou B000 autoriza o agente a escrever em `C:\Program Files (x86)\GeneXus`.

## Critério de encerramento

A solução e o pacote mínimos são restauráveis e compiláveis sem depender da instalação do GeneXus. B000–B004 foram concluídos posteriormente no U15; a próxima frente operacional é B005, conforme `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`.
