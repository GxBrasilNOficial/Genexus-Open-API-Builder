# B083 — UX residual de conflitos de colisão

## Objetivo

Residual de apresentação: para cada conflito de **colisão** (objeto com nome planejado que não é reconhecido como próprio, ou ambíguo), exibir de forma legível **nome, tipo, módulo e Folder**, alinhado à decisão do MVP. O núcleo de detecção e bloqueio antes do primeiro `Save()` (sem overwrite e sem `_v2`) já estava atendido.

## Comportamento

1. O leitor de estado da geração monta uma lista por objeto conflitante (`ApiPlanCollisionConflict`).
2. Formato de linha: `Nome='…' | Tipo='…' | Modulo='…' | Folder='…'`.
3. File de metadata: `Folder='(n/a)'` (File não vive em Folder). Objeto sem Parent Folder: também `(n/a)`.
4. Ambiguidade: uma linha por ocorrência.
5. A lista aparece no Wizard (abas de geração / detalhe de etapa e cabeçalho com contagem), na mensagem de preflight, na Output e no relatório final quando o Sync ou o Wizard são barrados por colisão.
6. Fora do residual: Keep/Replace de SDT editado no Sync; divergência de integridade B067 em File próprio; reuso de Folder `NomeOpenApi` preexistente, tratado na evidência `Docs/Implementation/2026-08-08-FOLDER-REUTILIZADO-COM-AVISO.md`.

## Código / testes

- `Src/Extension/Diagnostics/ApiPlanCollisionConflict.cs`
- `Src/Extension/Diagnostics/ApiPlanGenerationStateReader.cs`
- `Src/Extension/Diagnostics/ApiPlanWritePreflight.cs`
- Integração em `Package.cs` (Wizard e Sync) e cabeçalho em `PrototypeWizardDialog.cs`
- Teste: `Tests/CollisionUx/Test-ApiPlanCollisionConflict.ps1` (pré-push)
- Manifesto **não** alterado (só DLL)

## Validação manual U15 (2026-08-08)

Transaction `Teste`, KB `wsEducacaoSpTeste`.

1. **Colisão**: SDT externo `sdtTeste_API_Response` (Description não própria). Wizard em estado `teste bloqueado (1 conflito(s))`; aba SDTs listou
   `Nome='sdtTeste_API_Response' | Tipo='SDT' | Modulo='…' | Folder='(n/a)'` (ou `Modulo='General'` após mover o objeto). Checkbox de escrita desabilitado; sem `Save()`.
2. **Desbloqueio**: após apagar o SDT externo, Wizard em `teste de complementacao`; etapas confirmáveis; preflight agregado aprovado.
3. **Aplicação**: `Created=11`, `Updated=2`, `Blocked=0`, aviso de fallback inglês; metadata `apiTeste_Metadata` criada.
4. **Build All**: sucesso nos environments `.NET`/PostgreSQL e `.NET Framework`/SQL Server (warning ambiental `FBiTextSharp.dll` no Framework).

Status: **concluído**.
