# B081 — Relatório final pós-aplicação

## Objetivo

Após Wizard, Sync ou Remover, exibir relatório estruturado (criados / atualizados / removidos / bloqueados / avisos) sem depender só da janela Output técnica. Contagens e nomes devem bater com a Output e com a KB.

## Comportamento

1. Acumula resultados dos writers durante a aplicação (`ApiPlanApplicationFinalReportCollector`).
2. Abre diálogo `ApiPlanApplicationFinalReportDialog` com headline, corpo legível, tempo e botão **Abrir objeto principal** quando houver API Object (exceto Remover).
3. Espelha o mesmo resumo na Output com prefixo `[B081]`.
4. Headlines: sucesso / com avisos / interrompida; Sync sem diff → `Nenhuma sincronizacao necessaria.`; Remover sucesso → `API removida com sucesso.`
5. UX: wrap de linhas longas; altura ajustada ao conteúdo; rolagem vertical recalculada após o layout e ao redimensionar; sem seleção azul ao abrir.

## Código / testes

- `Src/Extension/Diagnostics/ApiPlanApplicationFinalReport.cs`
- `Src/Extension/ApiPlanApplicationFinalReportDialog.cs`
- Integração em `Package.cs` (Wizard, Sync incluindo no-op, Remover)
- O relatório inclui efeitos colaterais do plano: Folder da Transaction criado pela extensão, Folder compartilhado `GxOpenAPI` criado pela extensão, Transaction atualizada pelo Business Component e SDTs escritos pelos writers internos.
- Testes: `Tests/ApplicationFinalReport/Test-ApiPlanApplicationFinalReport.ps1` e `Tests/OpenApiContract/Test-ApiPlanOpenApiContractMarks.ps1` (pré-push)
- Manifesto **não** alterado (só DLL)

## Validação manual U15 (2026-08-08)

Transaction `Teste`, KB de teste.

1. **Wizard completo** (após remoção prévia): `Created=11`, `Updated=2` (SDTs compartilhados), `Warnings=1` (fallback inglês), `DurationMs≈10455`; botão Abrir objeto principal habilitado; Output alinhada.
2. **Sync sem diff**: relatório `Nenhuma sincronizacao necessaria.` + aviso correspondente; zero escrita.
3. **Sync com add** (`TesteObs3`, `TesteObs4`): UI de Sync; Aplicar; `Updated=13`, `Warnings=1` (fallback), `DurationMs≈12126`; Output alinhada.
4. **Remover**: confirmação; `Deleted=11` (API, 4 Procedures, 5 SDTs próprios, metadata); Folder reutilizado e SDTs `GxOpenAPI`/BC preservados; `Outcome='Success'`, `DurationMs≈1789`.
5. **Remover sem metadata** (corrida anterior): `Remocao interrompida` com bloqueio explícito — esperado.

Status: **concluído**.

## Correção pós-validação — 2026-08-09

Na reexecução do Wizard para a Transaction `Teste`, o relatório final retornou `Created=0`, `Updated=13`, `Blocked=0` e `Warnings=2`, incluindo o aviso de Folder reutilizado. A janela passou a exibir a barra de rolagem vertical e a última linha do aviso ficou acessível ao rolar até o fim; o Output confirmou o texto completo. A correção está em `ApiPlanApplicationFinalReportDialog.cs` e foi coberta pelo teste `Tests/ApplicationFinalReport/Test-ApiPlanApplicationFinalReport.ps1`.

Após remover o Folder específico e recriar a API, o Wizard retornou `Created=12`, `Updated=2`, `Removed=(nenhum)`, `Blocked=(nenhum)` e `Warnings=1`. O relatório passou a listar explicitamente `[Folder] TesteOpenApi — criado pela extensão; apagar só se ficar vazio`, além dos cinco SDTs próprios, quatro Procedures, API Object e metadata. A confirmação de remoção também identificou o Folder como criado pela extensão, mantendo a regra B086 de removê-lo somente quando `wasCreated=true`.

## Validação final e dimensões U15 — 2026-08-09

Após os cenários negativos de Folder e a limpeza da KB, a API foi recriada mantendo o Folder `Root Module\TesteOpenApi` com Description humana. O relatório final registrou `Created=11`, `Updated=2`, `Removed=(nenhum)`, `Blocked=0` e `Warnings=2`; o Folder não foi alterado e a metadata permaneceu com `transactionFolder.wasCreated=false`.

- O Wizard principal passou a iniciar com `1200x912`, com `MinimumSize=900x640`.
- O diálogo do relatório calcula a altura preferencial com acréscimo de 10% e a largura com acréscimo de 20%, preservando limites relativos à área útil da tela (`WorkingArea - 60` na altura e `WorkingArea - 80` na largura) e a rolagem vertical para textos longos.

## Monitor e owner — 2026-08-16

O relatório deixa de escolher o monitor pela posição do cursor. `Package.ShowFinalReport` resolve o owner com `Form.ActiveForm` **somente se a janela estiver visível**; se for nulo, oculto (caso típico: Wizard já fechado ainda no `using`) ou disposed, cai em `Process.MainWindowHandle`. O diálogo abre com `ShowDialog(owner)` quando há owner e calcula `WorkingArea` a partir do handle dessa janela (depois handle próprio, depois janela principal do processo, depois tela primária). Sem `Application.OpenForms`.

A cobertura desta mudança é mecânica (`Tests/ApplicationFinalReport/Test-ApiPlanApplicationFinalReport.ps1`). Evidência U15 do owner oculto: 2026-08-26, primeiro apply da `Teste` gravou `[B081]` na Output e o diálogo só apareceu na segunda chamada. Apply de quatro níveis no mesmo dia (após a correção do owner): diálogo visível, `SuccessWithWarnings`, `Created=3`, `Updated=24`, `Blocked=0`. ~~Não há evidência U15 de multi-monitor.~~ **Atualização 2026-09-03:** Wizard, Sync e Remover foram fumados no monitor da IDE (KB pequena, secundário); o diálogo B081 pós-Apply não foi o objeto desse smoke. Detalhe: aceite 1A, seção Monitor da IDE.
