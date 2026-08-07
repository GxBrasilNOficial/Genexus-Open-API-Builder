# B086 — Remover API gerada

## Objetivo

Comando explícito que remove somente objetos próprios identificados pela metadata da Transaction, sem reverter Business Component e sem apagar SDTs compartilhados em `GxOpenAPI`.

## Comportamento

1. Resolve a Transaction pelo menu de contexto ou seletor nativo.
2. Lê o File `api<Transaction>_Metadata` próprio.
3. Monta o plano a partir de `ownership` + `objects` (Procedures, SDTs próprios, API Object, Folder).
4. Mostra resumo e exige confirmação Yes/No (default No).
5. Apaga nesta ordem: Procedures → API Object → SDTs próprios → Metadata File → Folder (só se `wasCreated=true` e ficar vazio).

Preserva: Transaction, Business Component, Folder reutilizado, SDTs `sdt_API_*` compartilhados.

## Código / menu

- `ApiPlanGeneratedApiRemovalPlan` / `ApiPlanGeneratedApiRemover`
- Comando `Remover API gerada` em `Package.cs`, `GenexusOpenApiBuilder.package` (menu principal e contexto)
- Teste: `Tests/GeneratedApiRemoval/Test-ApiPlanGeneratedApiRemovalPlan.ps1`

## Instalação para teste

Manifesto mudou: após `Install-ExtensionForGeneXus18.bat` (admin), executar `Register-ExtensionForGeneXus18.bat` e no prompt `genexus /install`, depois `exit`.

## Validação manual pendente

Em Transaction com API gerada (ex.: `Teste`):

1. Menu contexto → Remover API gerada → conferir lista → confirmar.
2. Verificar ausência de API/Procedures/SDTs próprios/metadata; `GxOpenAPI` permanece; BC da Transaction permanece True.
3. Registrar Output e resultado no fechamento.
