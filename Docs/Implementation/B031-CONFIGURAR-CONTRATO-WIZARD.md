# B031 - Configurar Contrato no Wizard

Concluído no GeneXus 18 Upgrade 15: a extensão abriu o segundo passo do protótipo navegável do wizard, configurou serviços, campos e filtros essenciais da `Transaction` selecionada, manteve as decisões somente em memória e não realizou persistência nem escrita na KB.

## Objetivo

Implementar o Passo 2 do wizard para configurar o contrato inicial da API a partir da `Transaction` selecionada por B030, preservando o contrato de protótipo navegável sem criação, alteração ou exclusão de objetos.

## Escopo validado

- o comando é acionado por `Genexus Open API Builder > Configurar Contrato (B031)`;
- o comando exige uma `Transaction` já selecionada em memória por B030;
- a `Transaction` selecionada é reencontrada na KB ativa por GUID antes de o passo abrir;
- a janela modal navega sequencialmente por `Servicos`, `Requests`, `Response`, `Filtros List` e `Resumo B032`;
- os serviços `List`, `Get`, `Create` e `Update` iniciam habilitados;
- campos de `CreateRequest`, `UpdateRequest`, `Response` e filtros de `List` são apresentados a partir dos atributos do primeiro nível da `Transaction`;
- campos tecnicamente inadequados, sensíveis, inferidos, redundantes, fórmulas e auditoria recebem tratamento inicial conservador no protótipo;
- partes da chave primária aparecem desabilitadas no `CreateRequest` até validação pública de autonumeração e no `UpdateRequest`, onde a chave identifica o registro no `RestPath`;
- `Voltar` retrocede uma página por vez e, no primeiro passo B031, retorna para B030 sem persistir contrato;
- `Cancelar` descarta a sessão do wizard em memória;
- `Fechar`, no resumo, conclui B031 em memória e habilita a continuidade para B032;
- nenhum `ApiPlan` definitivo é criado;
- nenhum objeto GeneXus é criado, alterado ou excluído pela extensão.

## Implementação

`Src/Extension/Package.cs` registra o comando B031 e concentra a resolução da KB ativa, da seleção em memória e da abertura da janela modal. O manifesto `Src/Extension/GenexusOpenApiBuilder.package` mantém o mesmo ID do comando nas duas camadas XML: `CommandDefinition` e `Command refid`.

`Src/Extension/Diagnostics/PrototypeWizardContract.cs` monta o snapshot somente leitura do contrato prototípico: serviços, campos de payload, response e filtros candidatos. O snapshot desabilita fórmulas em requests por propriedade pública `Formula`, desabilita toda chave primária no `CreateRequest` até validação pública de autonumeração e desabilita toda chave primária no `UpdateRequest`. Esse snapshot é deliberadamente transitório e não é `ApiPlan`.

`Src/Extension/PrototypeWizardContractDialog.cs` implementa a janela modal WinForms do Passo 2. A navegação é sequencial e acumula as decisões em `PrototypeWizardSessionState` apenas quando o usuário conclui o resumo.

## Evidência manual no U15

A navegação visual foi validada manualmente no U15 com `Transaction='Distribuidora'`, selecionada pelo B030 via seletor nativo. Após as correções de revisão, o teste B030 -> B031 confirmou a contagem abaixo:

```text
[Genexus Open API Builder][B030] Wizard Passo 1 concluido em memoria: Transaction='Distribuidora', Module='Root Module', SelectionSource='Seletor'.
[Genexus Open API Builder][B030] Proximo passo habilitado para B031. Nenhuma escolha foi persistida e nenhum objeto foi criado, alterado ou excluido.
[Genexus Open API Builder][B031] Wizard Passo 2 concluido em memoria: Transaction='Distribuidora', Services='List,Get,Create,Update'.
[Genexus Open API Builder][B031] Campos selecionados: Create=1, Update=1, Response=2, ListFilters=2.
[Genexus Open API Builder][B031] Proximo passo habilitado para B032. Nenhum ApiPlan foi criado, nenhuma escolha foi persistida e nenhum objeto foi criado, alterado ou excluido.
```

A validação visual confirmou a navegação sequencial por `Requests`, `Response`, `Filtros List` e `Resumo B032`, com `Voltar`, `Cancelar` e `Fechar` conforme o estado da página.

## Validação local

- `pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1`: OK, com 9 comandos registrados e sincronizados;
- `dotnet build Src\GenexusOpenApiBuilder.sln -c Release`: compilação com sucesso, 0 erros;
- `git diff --check`: sem erros de whitespace;
- avisos NU1900 ocorreram apenas por indisponibilidade de consulta de vulnerabilidades nos feeds NuGet durante o build interativo.

## Resultado

Critério atendido em 2026-07-20: o Passo 2 do wizard configura serviços, campos e filtros essenciais, guarda as decisões apenas em memória e deixa o protótipo pronto para B032, sem `ApiPlan`, sem persistência e sem escrita na KB.
