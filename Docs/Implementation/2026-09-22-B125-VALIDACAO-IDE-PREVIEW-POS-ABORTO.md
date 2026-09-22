# B125 — Validação IDE: Preview do Remover pós-aborto

Data: 2026-09-22.

Item de backlog: `B125` — Preview do Remover, após aborto parcial, não pode anunciar como
exclusão objetos já ausentes da Knowledge Base.

Commit exercido: `ba310f1` (`B125: corrige preview apos aborto parcial`). A DLL Release foi
instalada manualmente pelo mantenedor com a IDE GeneXus fechada; a validação foi feita depois na
KB de teste `wsEducacaoSpTeste`, Transaction `Teste`.

## Escopo

Esta sessão cobre somente `B125`: Preview após uma remoção abortada, sem reduzir o inventário
durável, e retomada da mesma fila para devolver a KB ao estado terminal. Não cobre `B126`
(anúncio de Folder com Description/contêiner divergente) nem `B127` (re-smoke dos hardenings
de GUID e homônimo).

## Cenário e evidência

1. O Preview inicial encontrou 5 Procedures e 18 SDTs próprios presentes e iniciou uma remoção
   com `PlannedDeletes=25`; o diário existente `FileId=88` foi reaberto com
   `OperationId=8c7a19e9-61a4-4348-b006-419305eb191a`.
2. O mantenedor abortou a fila depois de 10 exclusões: `apiTeste`, as 5 Procedures e os SDTs
   `ListResponse`, `ListResponse_Item`, `ListFilters` e `Response`. O diário confirmou
   `Partial/RemovalPartial`, `Recibos=10` e inventário de 30 itens; o relatório final registrou
   `Removidos=10`, `Bloqueados=1` e aborto `B082`.
3. Sem usar Recuperar, um segundo `Remover API gerada` abriu o Preview. Ele mostrou
   `Procedures presentes na KB (0)`, `SDTs próprios presentes na KB (14)` e a seção
   `Já ausentes na KB (não serão apagados nesta execução) (10)` com exatamente os 10 objetos
   removidos no passo anterior. Os SDTs compartilhados permaneceram preservados. O usuário
   recusou a confirmação; a Output registrou que nenhuma alteração foi feita na KB.
4. Para limpar a KB, `Recuperar operação interrompida` releu o inventário e autorizou retomar a
   mesma fila: 15 alvos `Delete` continuavam presentes, os 10 já ausentes permaneceram fora da
   fila, e os itens `Preserve` continuaram preservados. A execução fechou o mesmo `OperationId`
   em `Removed/Removed`, com `Removidos=15`, `Bloqueados=0`, `Avisos=0` e `DuraçãoMs=1731`.

## Resultado

**PASS.** O Preview passou a distinguir presença real da KB e o inventário do diário permaneceu
intacto para a recuperação. A retomada comprovou que a correção de apresentação não alterou a
fila durável nem incluiu alvo fora do inventário original.

## Evolução de UX validada após B125

Os commits `1f2589f` (`Melhora relatório da retomada de remoção`) e `4999d6f` (`Evita diálogo
duplicado na recuperação`) absorveram no relatório final o resumo da continuação e retiraram o
diálogo informativo redundante que aparecia depois dele.

Em nova execução na mesma KB e Transaction, o diário `OperationId=6e03fe65-77d1-4222-b5ac-3bb84e418caa`
registrou `Partial/RemovalPartial` após aborto do usuário. A recuperação identificou 17 alvos
`Delete` ainda presentes, retomou a fila no mesmo envelope e a fechou em `Removed/Removed`.
O Output registrou `Removidos=17`, `Bloqueados=0`, `Avisos=0`, `Informações=1` e
`PersistenceReceipts=17`. O relatório final exibiu o texto
`A remoção foi retomada e concluída: 17 objeto(s) saíram da KB nesta continuação.` na seção
`Informações (1)`, mantendo `Avisos: (nenhum)`. O mantenedor confirmou que o aviso extra não
foi mais mostrado após o relatório.

## Limite da evidência

A melhoria de UX foi exercida somente no caminho de `ContinueRemovePass` após uma remoção
interrompida. Os outros desfechos de recuperação preservam seus próprios diálogos informativos
ou de bloqueio e não foram reexecutados nesta sessão.

## Rastreabilidade

- Backlog: `B125` em `Docs/Foundation/06-BACKLOG_v0.1.md`.
- Checkpoint: item 161 de `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`.
- Release em preparação: entrada `B125` de `### Fixed` em `[Unreleased]` no `CHANGELOG.md`.
- Implementação: `ba310f1` (Preview), `1f2589f` (informação no relatório) e `4999d6f`
  (supressão do diálogo redundante).
