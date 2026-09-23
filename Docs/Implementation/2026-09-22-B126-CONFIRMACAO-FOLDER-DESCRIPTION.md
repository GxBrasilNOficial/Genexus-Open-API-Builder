# B126 — Confirmação do Remover para Folder com Description ou contêiner divergente

Data: 2026-09-22.

Item de backlog: `B126`.

## Escopo

Quando o Folder é próprio (`ownedByThisApi=true`), a confirmação do Remover não pode
prometer «apaga se ficar vazio» se a Description viva não for canônica/legada ou se o
contêiner for inesperado — nesses casos `DeleteOwnFolder` preserva. Esta frente alinha
anúncio, contagem e relatório ao comportamento seguro. Não muda schema da metadata.
Não cobre `B127` (re-smoke GUID/homônimo) nem `B125` (Preview pós-aborto).

## Implementação offline (2026-09-22)

1. O Preview captura `FolderDescriptionMatchesOwned` e `FolderInExpectedContainer`.
2. Com posse e GUID ok, mas Description/contêiner divergentes, o plano marca
   `FolderPreservedAtPreviewForSafety=true`: confirmação anuncia preservação; `CountPlannedDeletes`
   exclui o Folder; a fila de posse permanece.
3. `DeleteOwnFolder` registra preservação por Description/contêiner em
   `PreservedOwnershipGateFolders`; o B081 emite aviso tipado distinto do caso não-vazio.
4. Localização pt/es/en para o anúncio e o aviso.
5. Gates: `tests.generatedApiRemovalPlan`, `tests.b082Etapa2Safety`, `tests.applicationFinalReport`.
6. Build Release: 0 avisos, 0 erros.

## Smoke IDE (2026-09-22) — PASS

KB: `wsEducacaoSpTeste` / Transaction `Teste` / Folder `TesteOpenApi`.
DLL Release instalada manualmente (só DLL; manifesto inalterado).

Preparação necessária (o Folder legado sem posse mostrava «reutilizado; nunca apagar» e
não exercia o `B126`): Remover a API, apagar o Folder vazio à mão, Wizard recriar tudo
com posse (`criado pela extensão; apagar só se ficar vazio`).

1. Baseline com Description canônica: confirmação mostrou
   `Folder: TesteOpenApi (criado pela extensão; apagar só se ficar vazio)` — cancelado com Não.
2. Description alterada para `TesteOpenApi - smoke B126`.
3. Remover de novo: confirmação mostrou
   `Folder: TesteOpenApi (criado pela extensão; será preservado — Description ou contêiner divergente)`.
4. Sim: `Removidos=25`, Folder `TesteOpenApi` permaneceu na KB.
5. Relatório B081: `API removida com avisos.`, `Avisos=1` —
   `Folder 'TesteOpenApi' nao foi apagado porque a Description ou o contenedor divergem do esperado.`
   Persistência `Confirmados=25; Pendências=0`. Tempo ~4,7 s.

**Conclusão:** anúncio, preservação e aviso tipado alinhados. `B126` fechado.
