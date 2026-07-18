# B002 — Listagem de Transactions Reais

## Estado

Concluído no GeneXus 18 Upgrade 15: a extensão listou Transactions reais da KB de teste, usando APIs públicas e somente leitura.

## Objetivo

Comprovar que a extensão lista as Transactions reais da Knowledge Base recém-aberta usando APIs públicas e somente leitura.

## Contrato oficial usado

```csharp
Transaction.GetAll(knowledgeBase.DesignModel)
```

- `knowledgeBase.DesignModel` fornece o modelo de design da KB aberta;
- `Transaction.GetAll` retorna as Transactions existentes nesse modelo;
- a sonda lê somente o nome de cada Transaction e ordena o resultado em memória.

## Roteiro de validação manual

1. Instalar manualmente a DLL Release atualizada.
2. Iniciar o GeneXus 18 U15 com a extensão marcada.
3. Abrir uma KB de teste já existente.
4. Confirmar na janela Output a linha de total e ao menos uma linha iniciada por `[Genexus Open API Builder][B002] Transaction:`.
5. Confirmar que a extensão não criou, salvou, fechou ou alterou objetos da KB.

## Evidência de compilação

- `dotnet build Src\GenexusOpenApiBuilder.sln --configuration Release --no-restore` concluído em 2026-07-18, com 0 avisos e 0 erros.

## Evidência do teste manual

- GeneXus 18 Upgrade 15, com a extensão marcada no Extensions Manager;
- KB de teste aberta: `wsEducacaoSpTeste`;
- total observado na janela Output: 10 Transactions;
- Transactions observadas: `Carga`, `Contrato`, `DiretoriaDeEnsino`, `Distribuidora`, `Escola`, `GuiaPed`, `Laudo`, `Lote`, `NotaFiscal` e `Produto`;
- a grade da IDE confirmou as mesmas 10 Transactions no Root Module;
- nenhuma operação de criação, salvamento, fechamento ou alteração de objeto foi acionada pela extensão.

## Critério de conclusão

Critério atendido em 2026-07-18: 10 Transactions reais foram observadas em uma KB de teste existente e a evidência está registrada neste documento e no checkpoint operacional.
