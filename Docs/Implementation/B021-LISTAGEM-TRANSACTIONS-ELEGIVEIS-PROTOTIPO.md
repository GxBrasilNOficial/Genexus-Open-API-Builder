# B021 — Listagem de Transactions Elegíveis no Protótipo

## Estado

Concluído no GeneXus 18 Upgrade 15: a extensão listou manualmente as Transactions da KB ativa na Output, sem persistência nem operações de escrita.

## Objetivo

Listar, por comando manual e somente leitura, as Transactions elegíveis da Knowledge Base ativa, preparando a futura seleção da Transaction sem persistir escolhas nem alterar objetos GeneXus.

## Contrato aplicado

- o comando é acionado por `Genexus Open API Builder > Listar Transactions Elegíveis (B021)`;
- a KB ativa é obtida pelo mesmo fluxo público manual de B020;
- a enumeração usa `Transaction.GetAll(knowledgeBase.DesignModel)`, evidência pública confirmada no B002;
- nesta etapa, elegibilidade significa ser uma `Transaction` retornada pela API pública; módulo, `Business Component` e chave serão avaliados nas frentes B022, B024 e B025;
- nomes são ordenados somente em memória e exibidos na janela Output padrão da IDE;
- nenhuma escolha é persistida e nenhum objeto GeneXus é criado, alterado ou excluído.

## Implementação

`Src/Extension/Diagnostics/EligibleTransactionReader.cs` concentra a enumeração somente leitura. `Src/Extension/Package.cs` registra o comando B021 e apresenta o total seguido dos nomes na Output. O placeholder `Futura Primeira Opção` e o comando B020 permanecem ativos durante a Sprint 2.

O manifesto `Src/Extension/GenexusOpenApiBuilder.package` mantém o mesmo ID do comando nas duas camadas XML: `CommandDefinition` e `Command refid` dentro do grupo usado pelo submenu.

## Evidência do teste manual

- GeneXus 18 Upgrade 15, com a extensão reinstalada e marcada no Extensions Manager;
- KB de teste aberta: `wsEducacaoSpTeste`;
- saída capturada: total de 10 Transactions — `Carga`, `Contrato`, `DiretoriaDeEnsino`, `Distribuidora`, `Escola`, `GuiaPed`, `Laudo`, `Lote`, `NotaFiscal` e `Produto`;
- nenhuma criação, alteração ou exclusão de objeto GeneXus foi relatada durante o acionamento manual.

## Critério de conclusão

Critério atendido em 2026-07-19: a lista de 10 Transactions foi exibida a partir da KB ativa sem persistência nem escrita na KB.
