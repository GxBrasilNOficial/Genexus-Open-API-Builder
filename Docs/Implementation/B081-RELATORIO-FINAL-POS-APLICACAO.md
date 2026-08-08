# B081 — Relatório final pós-aplicação

## Objetivo

Após Wizard, Sync ou Remover, exibir relatório estruturado (criados / atualizados / removidos / bloqueados / avisos) sem depender só da janela Output técnica. Contagens e nomes devem bater com a Output e com a KB.

## Comportamento

1. Acumula resultados dos writers durante a aplicação (`ApiPlanApplicationFinalReportCollector`).
2. Abre diálogo `ApiPlanApplicationFinalReportDialog` com headline, corpo legível, tempo e botão **Abrir objeto principal** quando houver API Object (exceto Remover).
3. Espelha o mesmo resumo na Output com prefixo `[B081]`.
4. Headlines: sucesso / com avisos / interrompida; Sync sem diff → `Nenhuma sincronizacao necessaria.`; Remover sucesso → `API removida com sucesso.`
5. UX: wrap de linhas longas; altura ajustada ao conteúdo; sem seleção azul ao abrir.

## Código / testes

- `Src/Extension/Diagnostics/ApiPlanApplicationFinalReport.cs`
- `Src/Extension/ApiPlanApplicationFinalReportDialog.cs`
- Integração em `Package.cs` (Wizard, Sync incluindo no-op, Remover)
- Teste: `Tests/ApplicationFinalReport/Test-ApiPlanApplicationFinalReport.ps1` (pré-push)
- Manifesto **não** alterado (só DLL)

## Validação manual U15 (2026-08-08)

Transaction `Teste`, KB de teste.

1. **Wizard completo** (após remoção prévia): `Created=11`, `Updated=2` (SDTs compartilhados), `Warnings=1` (fallback inglês), `DurationMs≈10455`; botão Abrir objeto principal habilitado; Output alinhada.
2. **Sync sem diff**: relatório `Nenhuma sincronizacao necessaria.` + aviso correspondente; zero escrita.
3. **Sync com add** (`TesteObs3`, `TesteObs4`): UI de Sync; Aplicar; `Updated=13`, `Warnings=1` (fallback), `DurationMs≈12126`; Output alinhada.
4. **Remover**: confirmação; `Deleted=11` (API, 4 Procedures, 5 SDTs próprios, metadata); Folder reutilizado e SDTs `GxOpenAPI`/BC preservados; `Outcome='Success'`, `DurationMs≈1789`.
5. **Remover sem metadata** (corrida anterior): `Remocao interrompida` com bloqueio explícito — esperado.

Status: **concluído**.
