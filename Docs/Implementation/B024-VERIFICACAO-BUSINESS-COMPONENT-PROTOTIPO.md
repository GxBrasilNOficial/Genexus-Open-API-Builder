# B024 — Verificação de Business Component no Protótipo

## Estado

Concluído no GeneXus 18 Upgrade 15: a extensão reutilizou a `Transaction` selecionada em memória pelo B022 e verificou a propriedade pública `IsBusinessComponent`, sem persistência nem operações de escrita pela extensão.

## Objetivo

Verificar, para a `Transaction` selecionada no protótipo navegável, se ela pode operar como `Business Component`, mantendo o fluxo somente leitura.

## Contrato aplicado

- o comando é acionado por `Genexus Open API Builder > Verificar Business Component (B024)`, tanto no menu de contexto da `Transaction` quanto no menu principal da IDE;
- a KB ativa é obtida pelo mesmo fluxo público manual de B020;
- a `Transaction` é a escolha mantida em memória por B022;
- se nenhuma `Transaction` estiver em memória, o comando informa a necessidade de executar B022 primeiro;
- a verificação usa a propriedade pública `Transaction.IsBusinessComponent` do SDK;
- o resultado é apresentado na janela Output padrão da IDE;
- nenhuma escolha é persistida e a extensão não cria, altera nem exclui objetos GeneXus.

## Implementação

`Src/Extension/Package.cs` registra o comando B024 e concentra o fluxo manual: verifica a KB ativa, exige a seleção em memória de B022, reencontra a `Transaction` pelo GUID e escreve o resultado na Output.

`Src/Extension/Diagnostics/PrototypeBusinessComponentReader.cs` encapsula a leitura somente leitura de `IsBusinessComponent` e produz o status operacional:

- `Apta via Business Component`, quando a propriedade está habilitada;
- `Bloqueada: Business Component desabilitado`, quando a propriedade está desabilitada.

O manifesto `Src/Extension/GenexusOpenApiBuilder.package` mantém o mesmo ID do comando nas duas camadas XML: `CommandDefinition` e `Command refid` no grupo usado pelo submenu.

## Evidência do teste manual

- GeneXus 18 Upgrade 15, com a extensão reinstalada e marcada no Extensions Manager;
- primeiro acionamento direto de B024 informou corretamente que não havia `Transaction` selecionada em memória;
- B022 selecionou a `Transaction` `Carga` e leu o módulo `Root Module`;
- B024 verificou `Carga` por API pública e reportou `IsBusinessComponent=False`;
- Output observada: `Status='Bloqueada: Business Component desabilitado'`;
- após habilitação manual da propriedade `Business Component` na `Transaction` `Carga`, a IDE executou geração do pattern `WorkWithWebCarga` e reportou sucesso;
- B022 selecionou novamente `Carga`;
- B024 verificou `Carga` por API pública e reportou `IsBusinessComponent=True`;
- Output observada: `Status='Apta via Business Component'`;
- nenhuma criação, alteração ou exclusão de objeto GeneXus foi realizada pela extensão durante os acionamentos manuais.

## Evidência adicional do menu principal

- o menu principal `Genexus Open API Builder` apareceu na IDE antes de `Help`, contendo os comandos `Futura Primeira Opção`, B020, B021, B022, B023 e B024;
- B024 acionado diretamente pelo menu principal sem seleção em memória reportou: `Nenhuma Transaction selecionada em memória. Execute primeiro o comando B022.`;
- B023 acionado diretamente pelo menu principal sem seleção em memória reportou a mesma proteção operacional;
- B021 acionado pelo menu principal listou 10 Transactions elegíveis na KB `wsEducacaoSpTeste`: `Carga`, `Contrato`, `DiretoriaDeEnsino`, `Distribuidora`, `Escola`, `GuiaPed`, `Laudo`, `Lote`, `NotaFiscal` e `Produto`;
- B020 acionado pelo menu principal detectou a KB ativa `wsEducacaoSpTeste`, GUID `39e12e41-51a7-466f-a448-dbc3a05f17c7`, em `C:\KBs\wsEducacaoSpTeste`;
- B022 selecionou a `Transaction` `Contrato` e leu o módulo `Root Module`;
- B024 verificou `Contrato` e reportou `IsBusinessComponent=False` com `Status='Bloqueada: Business Component desabilitado'`;
- B023 verificou os objetos planejados para `Contrato`, manteve o File de metadata `apiContrato_Metadata` e reportou `Total=15`, `Existentes=0`, `Ausentes=15`;
- a validação confirmou que os comandos B020-B024 permanecem acessíveis pelo menu principal sem persistência de escolhas fora da seleção em memória de B022.

## Validações locais

- `pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1` concluiu com `Status=OK` e 6 comandos registrados;
- `dotnet build Src\GenexusOpenApiBuilder.sln --configuration Release --no-restore` concluiu com 0 avisos e 0 erros;
- `git diff --check` não reportou problemas.

## Critério de conclusão

Critério atendido em 2026-07-20: a capacidade de operar via `Business Component` foi verificada por API pública para a `Transaction` selecionada, com nome da `Transaction` e resultado apresentados sem persistência nem escrita pela extensão na KB. A base está pronta para B025, que lerá chave simples ou composta completa.
