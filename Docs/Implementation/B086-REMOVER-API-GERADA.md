# B086 — Remover API gerada

## Objetivo

Comando explícito que remove somente objetos próprios identificados pela metadata da Transaction, sem reverter Business Component e sem apagar SDTs compartilhados em `GxOpenAPI`.

## Comportamento

1. Resolve a Transaction pelo menu de contexto ou seletor nativo.
2. Lê o File `api<Transaction>_Metadata` próprio.
3. Monta o plano a partir de `ownership` + `objects` (Procedures, SDTs próprios, API Object, Folder).
4. Mostra resumo e exige confirmação Yes/No (default No).
5. Apaga nesta ordem: API Object → Procedures → SDTs próprios (ListResponse antes de Response) → Metadata File → Folder (só se `wasCreated=true` e ficar vazio). A IDE bloqueia exclusão quando ainda há referência; por isso o dependente sai antes do referenciado.
6. Confirmação lista Procedures e SDTs **um por linha** (com seções separadas).
7. Folder: `wasCreated=false` → texto “reutilizado; nunca apagar”; `wasCreated=true` → “criado pela extensão; apagar só se ficar vazio”.

Preserva: Transaction, Business Component, Folder reutilizado, SDTs `sdt_API_*` compartilhados.

## Código / menu

- `ApiPlanGeneratedApiRemovalPlan` / `ApiPlanGeneratedApiRemover`
- Comando `Remover API gerada` em `Package.cs`, `GenexusOpenApiBuilder.package` (menu principal e contexto)
- Teste: `Tests/GeneratedApiRemoval/Test-ApiPlanGeneratedApiRemovalPlan.ps1`

## Validação manual U15 (2026-08-08)

Transaction `Teste`, KB de teste.

1. **Cancelamento:** confirmação com **Não** → Output `Remocao cancelada`; KB intacta.
2. **Ordem (correções):** falha Procedure referenciada pelo API → API primeiro; falha Response referenciado por ListResponse → ListResponse antes de Response.
3. **Folder reutilizado:** com `wasCreated=false`, remoção limpou objetos próprios e metadata; Folder `TesteOpenApi` vazio permaneceu; `sdt_API_ErrorResponse` / `sdt_API_Pagination` e BC=`True` preservados.
4. **Folder criado:** Folder vazio apagado manualmente; Wizard recriou API (`Created=5` SDTs próprios, Folder Guid `8a89a63f-...`, metadata Guid `a76f90ae-...`); confirmação mostrou “criado pela extensão”; remoção `Deleted=12` incluindo `Folder:TesteOpenApi`. Folder e conteúdo sumiram; SDTs compartilhados e BC intactos.

## Reteste final do Folder reutilizado — 2026-08-09

Na Transaction `Teste`, após a geração e o reencontro com `transactionFolder.wasCreated=false`, o comando **Remover API gerada** foi executado pelo menu.

- A confirmação exibiu `Folder: TesteOpenApi (reutilizado; nunca apagar)` e informou que o Business Component não seria revertido.
- O Output e o relatório B081 registraram `Outcome='Success'`, `Deleted=11`, `Blocked=0` e `Warnings=0`.
- Foram removidos o API Object, quatro Procedures, cinco SDTs próprios e o File de metadata.
- O Folder `TesteOpenApi` permaneceu na KB, assim como os dois SDTs compartilhados e o Business Component.

Status: **concluído**.
