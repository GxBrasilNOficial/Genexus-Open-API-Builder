# B121 — Tornar explícita a seleção de BC/List no Sync

**Estado:** planejado; melhoria futura, fora da sprint `S-B111`.

**Correlato de backlog:** [`B121`](../Foundation/06-BACKLOG_v0.1.md).

**Escopo:** tornar explícita no diálogo de `Sincronizar com a Transaction` a
escolha das etapas consumidoras `Business Component` e `List`, sem inferir a
intenção somente pela lista de serviços persistida na metadata.

## Decisão de encaminhamento

`B121` fica fora da sprint `S-B111` e não altera a próxima ação única da F1 nem
reclassifica a lacuna de aceite manual que permanece registrada para a sprint.
O problema de seleção dos consumidores é separado da regressão de preflight
de SDT registrada na validação do Sync da `Laudo`. Este backlog não autoriza
alteração do runtime nesta sprint.

O Sync deve continuar protegido pelo preflight completo e pela regra de zero
gravação quando houver bloqueio. A melhoria futura deve preservar essa barreira
ao tornar a intenção do usuário explícita.

## Problema observado

O diálogo de Sync permite escolher onde incluir campos adicionados:
`Response`, `CreateRequest`, `UpdateRequest` e `ListFilters`. Ele não apresenta
uma escolha equivalente para `Completar REST via Business Component` e
`Completar listagem`.

No orquestrador, `BuildSelection` deriva `ApplyList` quando a metadata contém o
serviço `List` e deriva `ApplyBusinessComponent` quando contém `Get`, `Create`,
`Update` ou `Delete`. A presença de um serviço no contrato não é a mesma coisa
que a etapa consumidora ter sido aplicada.

A evidência da F1 já mostrou essa diferença: uma API podia declarar
`List, Get, Create, Update` enquanto `Completar listagem` e
`Completar REST via Business Component` permaneciam `False` no resumo do
Wizard. Portanto, reabrir o Sync não permite reconstruir com segurança qual
intenção de consumidor deveria ser aplicada.

## Riscos

- não é possível reproduzir e aceitar isoladamente os perfis nenhum, somente
  BC, somente List e BC+List;
- a intenção do usuário fica ambígua quando o Sync reconstrói o plano;
- uma atualização de campo pode disparar consumidores não escolhidos
  explicitamente;
- a cobertura manual do Sync fica dependente de como a metadata foi
  originalmente gerada;
- uma correção apressada poderia enfraquecer o preflight ou reintroduzir
  gravação parcial.

## Plano futuro

1. Definir o contrato de persistência das escolhas de consumidor, sem
   confundir serviço declarado com etapa aplicada.
2. Expor no diálogo as opções `Business Component` e `List`, com estado inicial
   coerente e explicação das dependências.
3. Fazer o plano transportar as escolhas explícitas e validar combinações
   inválidas antes do primeiro `Save()`.
4. Cobrir os quatro perfis — nenhum, somente BC, somente List e BC+List — e o
   bloqueio de BC sem habilitação na Transaction.
5. Repetir os testes na IDE e confirmar que o relatório registra as etapas
   realmente executadas.
6. Atualizar os testes e a documentação sem alterar por inferência o contrato
   de APIs já geradas.

## Critério de aceite

- o Sync permite distinguir nenhum, somente BC, somente List e BC+List;
- `metadata.services` não é usado sozinho para decidir a intenção das etapas;
- combinações inválidas bloqueiam antes de qualquer gravação;
- o preflight e a ordem de gravação da F1 permanecem intactos;
- a matriz manual da F1 é registrada com relatório final e Output;
- builds e testes aplicáveis passam.

## Limites

Este registro não corrige a regressão do reencontro estrito de SDT observada
na `Laudo`, não reabre a decisão da F1 e não muda os checkboxes ou os objetos da
KB nesta frente. A implementação fica para uma sessão futura, depois do
encerramento da sprint `S-B111`.
