# B030 - Seleção de Transaction no Wizard

Concluído no GeneXus 18 Upgrade 15: a extensão abriu o primeiro passo do protótipo navegável do wizard, selecionou uma `Transaction` pelo menu principal e pelo menu de contexto, manteve a escolha somente em memória e não realizou persistência nem escrita na KB.

## Objetivo

Implementar o Passo 1 do wizard para selecionar uma `Transaction`, usando os aprendizados de B020-B025 e preservando o contrato de protótipo navegável sem criação, alteração ou exclusão de objetos.

## Escopo validado

- o comando é acionado por `Genexus Open API Builder > Abrir Wizard (B030)`;
- pelo menu principal, a extensão abre o seletor nativo filtrado para `Transaction`, com seleção única;
- pelo menu de contexto de uma `Transaction`, a extensão resolve diretamente a `Transaction` clicada;
- a `Transaction` selecionada é reencontrada na KB ativa por GUID antes de ser aceita;
- o módulo da `Transaction` é lido por `transaction.Module`;
- a escolha é armazenada apenas em `PrototypeTransactionSelectionState`;
- cancelamento, ausência de seleção e falhas de resolução encerram o fluxo sem persistir escolha;
- nenhum objeto GeneXus é criado, alterado ou excluído pela extensão.

## Implementação

`Src/Extension/Package.cs` registra o comando B030 e concentra o fluxo do Passo 1. O comando usa `KBObjectSelectionHelper.TryGetOnlyOneKBObjectFrom(data.Context)` quando acionado pelo contexto de uma `Transaction`; quando não há contexto de `Transaction`, usa `UIServices.SelectObjectDialog` com `KBObjectDescriptor.Get<Transaction>()`.

O manifesto `Src/Extension/GenexusOpenApiBuilder.package` mantém o mesmo ID do comando nas duas camadas XML: `CommandDefinition` e `Command refid` no grupo usado pelo menu principal e pelo submenu de contexto.

## Evidência manual no U15

Menu principal, com seleção pelo seletor nativo:

```text
[Genexus Open API Builder][B030] Wizard Passo 1 concluido em memoria: Transaction='Carga', Module='Root Module', SelectionSource='Seletor'.
[Genexus Open API Builder][B030] Proximo passo habilitado para B031. Nenhuma escolha foi persistida e nenhum objeto foi criado, alterado ou excluido.
```

Menu de contexto de `Transaction`:

```text
[Genexus Open API Builder][B030] Wizard Passo 1 concluido em memoria: Transaction='Contrato', Module='Root Module', SelectionSource='Contexto'.
[Genexus Open API Builder][B030] Proximo passo habilitado para B031. Nenhuma escolha foi persistida e nenhum objeto foi criado, alterado ou excluido.
```

A imagem do teste confirmou que o menu principal `Genexus Open API Builder` abriu o seletor nativo com 10 objetos do tipo `Transaction`, filtrados para `Transaction` e exibindo nome, tipo, módulo, descrição e datas.

## Validação local

- `pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1`: OK, com 8 comandos registrados e sincronizados;
- `dotnet build Src\GenexusOpenApiBuilder.sln -c Release`: compilação com sucesso, 0 erros;
- avisos NU1900 ocorreram apenas por indisponibilidade de consulta de vulnerabilidades nos feeds NuGet durante o build.

## Resultado

Critério atendido em 2026-07-20: o Passo 1 do wizard seleciona uma `Transaction` pelo menu principal e pelo contexto, guarda a escolha apenas em memória e deixa o protótipo pronto para B031, sem persistência e sem escrita na KB.
