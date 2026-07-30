# B022 — Leitura do Módulo da Transaction no Protótipo

## Estado

Concluído no GeneXus 18 Upgrade 15: a extensão abriu o seletor nativo filtrado para `Transaction`, leu o módulo da Transaction escolhida e exibiu os dois nomes na Output, sem persistência nem operações de escrita.

## Objetivo

Selecionar manualmente uma `Transaction` no protótipo navegável e ler seu módulo por API pública, mantendo a escolha somente em memória para as verificações posteriores.

## Contrato aplicado

- o comando foi acionado, durante a validação da frente, por `Genexus Open API Builder > Selecionar Transaction e Ler Módulo (B022)`;
- a KB ativa é obtida pelo fluxo público manual de B020;
- o diálogo público `UIServices.SelectObjectDialog` recebe seleção única filtrada para `Transaction`;
- o retorno é validado como `Transaction`, cujo módulo é lido por `transaction.Module`;
- a identidade da KB, o GUID e o nome da Transaction ficam somente em memória e são descartados quando a KB muda;
- os nomes da Transaction e do módulo são exibidos na janela Output padrão da IDE;
- nenhuma escolha é persistida e nenhum objeto GeneXus é criado, alterado ou excluído.

## Implementação

`Src/Extension/Package.cs` registrou o comando B022 e concentrou o fluxo manual: verifica a KB ativa, abre o seletor nativo, lê o módulo e escreve o resultado na Output. `Src/Extension/Diagnostics/PrototypeTransactionSelection.cs` mantém o estado efêmero que foi reutilizado pela frente B023.

O manifesto `Src/Extension/GenexusOpenApiBuilder.package` mantém o mesmo ID do comando nas duas camadas XML: `CommandDefinition` e `Command refid` no grupo usado pelo submenu.

## Evidência do teste manual

- GeneXus 18 Upgrade 15, com a extensão reinstalada e marcada no Extensions Manager;
- o seletor exibiu 10 objetos, todos do tipo `Transaction`;
- a Transaction `Escola` foi selecionada;
- Output observada: `Transaction selecionada: Name='Escola'` e `Módulo da Transaction: Name='Root Module'`;
- `Tools/Test-InstalledExtension.ps1` confirmou que a DLL instalada corresponde à build Release pelo SHA-256;
- nenhuma criação, alteração ou exclusão de objeto GeneXus foi relatada durante o acionamento manual.

## Critério de conclusão

Critério atendido em 2026-07-20: uma Transaction selecionada no fluxo manual teve seu módulo lido por API pública e ambos os nomes foram apresentados sem persistência nem escrita na KB.
