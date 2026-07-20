# B023 — Detecção de Objetos Existentes no Protótipo

## Estado

Concluído no GeneXus 18 Upgrade 15: a extensão reutilizou a `Transaction` selecionada em memória pelo B022, derivou os nomes planejados da API e verificou por APIs públicas se já existiam objetos com esses nomes, sem persistência nem operações de escrita.

## Objetivo

Verificar, para a `Transaction` selecionada no protótipo navegável, os objetos existentes relevantes para a futura geração da API, mantendo o fluxo somente leitura e sem assumir propriedade de objetos por nome.

## Contrato aplicado

- o comando é acionado por `Genexus Open API Builder > Detectar Objetos Existentes (B023)`;
- a KB ativa é obtida pelo mesmo fluxo público manual de B020;
- a `Transaction` é a escolha mantida em memória por B022;
- se nenhuma `Transaction` estiver em memória, o comando informa a necessidade de executar B022 primeiro;
- a verificação usa os nomes planejados conforme `Docs/Foundation/11-CONVENCOES_NOMES_E_OUTPUTS.md`;
- o `File` de metadata segue `api<NomeBase>_Metadata`;
- as consultas usam `GetAll` dos tipos públicos validados anteriormente: `API`, `Procedure`, `SDT`, `Folder` e `WikiFileKBObject`;
- o resultado é apresentado na janela Output padrão da IDE;
- nenhuma escolha é persistida e nenhum objeto GeneXus é criado, alterado ou excluído.

## Implementação

`Src/Extension/Package.cs` registra o comando B023 e concentra o fluxo manual: verifica a KB ativa, exige a seleção em memória de B022, reencontra a `Transaction` pelo GUID e escreve o resultado na Output.

`Src/Extension/Diagnostics/PrototypeExistingObjectReader.cs` deriva e verifica os nomes planejados:

- `api<NomeBase>`;
- `api<NomeBase>_Metadata`;
- `<NomeBase>OpenApi`;
- `proc<NomeBase>_API_List`, `Get`, `Create` e `Update`;
- `sdt<NomeBase>_API_CreateRequest`, `UpdateRequest`, `Response`, `ListFilters` e `ListResponse`;
- `GxOpenAPI`;
- `sdt_API_ErrorResponse`;
- `sdt_API_Pagination`.

O manifesto `Src/Extension/GenexusOpenApiBuilder.package` mantém o mesmo ID do comando nas duas camadas XML: `CommandDefinition` e `Command refid` no grupo usado pelo submenu.

## Evidência do teste manual

- GeneXus 18 Upgrade 15, com a extensão reinstalada e marcada no Extensions Manager;
- primeiro acionamento direto de B023 informou corretamente que não havia `Transaction` selecionada em memória;
- B022 selecionou a `Transaction` `Laudo` e leu o módulo `Root Module`;
- B023 verificou 15 nomes planejados para `Laudo`;
- Output observada: `MetadataFile='apiLaudo_Metadata'`;
- resultado observado: `Total=15`, `Existentes=0`, `Ausentes=15`;
- todos os objetos planejados retornaram `Count=0` e `Status='Ausente'`;
- nenhuma criação, alteração ou exclusão de objeto GeneXus foi relatada durante o acionamento manual.

## Validações locais

- `pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1` concluiu com `Status=OK` e 5 comandos registrados;
- `dotnet build Src\GenexusOpenApiBuilder.sln --configuration Release --no-restore` concluiu com 0 avisos e 0 erros;
- `git diff --check` não reportou problemas.

## Critério de conclusão

Critério atendido em 2026-07-20: os objetos existentes foram verificados por API pública para a `Transaction` selecionada, com nome da `Transaction` e resultado apresentados sem persistência nem escrita na KB. A base está pronta para B024, que verificará a capacidade de operar como `Business Component`.
