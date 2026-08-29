# B086 — Remover API gerada

## Objetivo

Comando explícito que remove somente objetos próprios identificados pela metadata da Transaction, sem reverter Business Component e sem apagar SDTs compartilhados em `GxOpenAPI`.

## Comportamento

1. Resolve a Transaction pelo menu de contexto ou seletor nativo.
2. Lê o File `api<Transaction>_Metadata` próprio.
3. Monta o plano a partir de `ownership` + `objects` (Procedures, SDTs próprios, API Object, Folder).
4. Mostra resumo e exige confirmação Yes/No (default No). Antes do Yes/No e de novo imediatamente antes do primeiro `Delete()`, o preflight `ValidateRemovalTargets` confere ambiguidade e posse do API Object, de cada Procedure e de cada SDT próprio listados na metadata. Ausência de um alvo é aceita (idempotente); ambiguidade ou objeto não próprio bloqueiam com **nenhuma alteração feita**.
5. Apaga nesta ordem: API Object → Procedures → SDTs próprios (ListResponse antes de Response) → Metadata File → Folder (só se `wasCreated=true` e ficar vazio). A IDE bloqueia exclusão quando ainda há referência; por isso o dependente sai antes do referenciado. As checagens de posse/ambiguidade se repetem em cada etapa como defesa; se o estado da KB mudar após o preflight, a remoção interrompe sem novas exclusões (residual de falha IDE/SDK no meio da sequência permanece).
6. Confirmação lista Procedures e SDTs **um por linha** (o mesmo texto da Output, sem wrap). Com dezenas de SDTs próprios (ex.: `Empresa`, 44), a lista rola; a pergunta e os botões Sim/Não permanecem visíveis dentro da área útil do monitor. Esc e Enter continuam em **Não**.
7. Folder: `wasCreated=false` → texto “reutilizado; nunca apagar”; `wasCreated=true` → “criado pela extensão; apagar só se ficar vazio”.

Preserva: Transaction, Business Component, Folder reutilizado, SDTs `sdt_API_*` compartilhados.

## Código / menu

- `ApiPlanGeneratedApiRemovalPlan` / `ApiPlanGeneratedApiRemover`
- Comando `Remover API gerada` em `Package.cs`, `GenexusOpenApiBuilder.package` (menu principal e contexto)
- Testes: `Tests/GeneratedApiRemoval/Test-ApiPlanGeneratedApiRemovalPlan.ps1` e `Tests/GeneratedApiRemoval/Test-ApiPlanGeneratedApiRemovalPreflight.ps1`

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

## Correção — preflight antes do primeiro Delete (2026-08-09)

Gap da revisão pré-push: ambiguidade/posse de Procedure e SDT eram avaliadas só dentro dos métodos de exclusão, depois do API Object já poder ter sido removido, o que permitia remoção parcial em cenário divergente.

Correção em `ApiPlanGeneratedApiRemover`: `ValidateRemovalTargets` roda em `Preview` (antes da confirmação) e no início de `Remove` (antes de qualquer `Delete`). Manifesto inalterado (só DLL).

### Reteste manual U15 (2026-08-09)

Transaction `Teste`, Folder reutilizado.

1. **Caminho feliz:** após regeneração, `Remover API gerada` concluiu `Deleted=11`, `Blocked=0`, `Outcome='Success'`; Folder `TesteOpenApi` permaneceu; metadata removida de Files; SDTs `GxOpenAPI` e BC preservados.
2. **Bloqueio por posse antes de qualquer Delete:** Description de `procTeste_API_List` alterada para texto humano; comando bloqueou em Preview com `Remocao bloqueada: Procedure 'procTeste_API_List' nao e propria da extensao. Nenhuma alteracao foi feita.`; relatório `Outcome='Interrupted'`, `Deleted=0`, `Blocked=1`, `DurationMs=0`; API/metadata/demais objetos próprios intactos.

### Regressão automatizada de ownership e pré-voo

O teste `Tests/OwnershipDescriptions/Test-ApiPlanOwnedObjectDescription.ps1` confirma que as sentinelas legadas de Procedure, SDT e metadata só são aceitas quando backlog, serviço/tipo, API e Transaction correspondem ao objeto reencontrado. O teste `Tests/GeneratedApiRemoval/Test-ApiPlanGeneratedApiRemovalPreflight.ps1` confirma que `Preview` e `Remove` chamam `ValidateRemovalTargets` antes do primeiro `Delete()` e mantêm as validações de ambiguidade e posse para API Object, Procedures e SDTs próprios.

### Smoke com Descriptions canônicas (2026-08-09)

Após a troca para `{Nome} - by Genexus Open API Builder` (evidência `2026-08-09-DESCRIPTIONS-PRODUTO-SEM-BACKLOG.md`):

1. **Remover com posse canônica:** `Deleted=12` (API, 4 Procedures, 5 SDTs, metadata, Folder criado), `Blocked=0`.
2. **Sem metadata:** bloqueio sem escrita (`Deleted=0`).
3. **Cancelamento:** plano montado; usuário recusou; nenhuma alteração.

## Correção — preview com rolagem (2026-08-29)

Na cópia `Gx_FabricaBrasil` / Transaction `Empresa` (44 SDTs próprios), `ExtensionConfirmDialog` dimensionava a janela pela altura total das duas colunas, sem teto nem rolagem: o texto cortava e os botões Sim/Não saíam da área útil.

O diálogo passou a limitar largura e altura à `WorkingArea` do monitor da IDE (`WorkingArea - 32`). A lista de Procedures/SDTs usa o mesmo texto da Output (`BuildConfirmationLists`), em fonte monoespaçada, **uma linha por objeto e sem wrap** — nomes longos como `…TipoDeRomaneioAntigoParaBusifrigTipolancto` não partem no meio da palavra. A pergunta e os botões Sim/Não ficam fora da rolagem. Default seguro permanece **Não** (Esc/Enter). Teste: `Tests/GeneratedApiRemoval/Test-ApiPlanGeneratedApiRemovalPlan.ps1`.

Na mesma medição o usuário confirmou **Sim**: `Deleted=50`, B081 `Success` / `DuraçãoMs=31836`, Design sem `*Empresa_API*`. A pasta `EmpresaOpenApi` vazia permaneceu (`wasCreated=false`). Evidência de escala: `Docs/Implementation/2026-08-29-CRITERIO11-ESCALA-EMPRESA.md`.
