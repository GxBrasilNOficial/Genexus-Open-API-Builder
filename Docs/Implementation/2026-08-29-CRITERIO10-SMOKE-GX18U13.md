# Critério 10 — Smoke da linha `Gx18u13` (subníveis)

Data: 2026-08-29.
Gate: Sprint 9, critério 10 (smoke satélite U13 no corte de subníveis).
Escopo do gate: Wizard sobre Transaction **multinível**, geração e `Build All` **sem `spc0018`**. Chamada HTTP permanece exclusividade do U15.

## Pré-condições

| Item | Resultado |
|---|---|
| IDE | GeneXus 18 Upgrade 13 (`C:\Program Files (x86)\GeneXus\GeneXus18up13`) |
| KB | `wsEducacaoSpTeste` (após regressão de versão: `Rebuild All` nos dois environments sem falha) |
| DLL satélite | `Install-ExtensionForGx18u13.bat` + `Register-ExtensionForGx18u13.bat` / `genexus /install` |
| Hash instalado = build | `InstalledMatchesBuild=True` |
| SHA-256 | `5C0C47075901DB352417176D14813358DB0427D4E756F174B37492C8A74ECA5B` |
| `GxLine` | `Gx18u13` |
| `PackageCompatibility` | `143920` |
| Versão assembly | `0.1.0-alpha.4` |

Validação mecânica: `Tools/Test-InstalledExtension.ps1` (paths U13) e inventário/Release (`GxLine` / `PackageCompatibility`).

Nota operacional: a janela `GeneXus.com` do `/install` fecha sozinha ao terminar (sem pausa); o prompt do Register (`cmd /k`) é outra janela. Confirmação prática = Extensions Manager + Wizard.

## Transaction

`Teste` — hierarquia de quatro níveis no ramo mais profundo:

`Teste` → `TesteItem` → `TesteItemFolio` → `TesteItemFolioDoc`, mais irmão `TestePortfolio`.

PK composta na raiz: `TesteId` + `TesteDate` + `TesteCodigo`.

## Wizard (2026-08-29)

Estado: `teste de reencontro`. Serviços: List, Get, Create, Update. `ApiName` / base path: `apiTeste`. `SecurityLevel=Authorization`. BC apto.

| Check | Resultado |
|---|---|
| B081 | `SuccessWithWarnings` |
| Criados | 0 |
| Atualizados | 27 (21 SDT + 4 Procedure + API Object + File) |
| Removidos / Bloqueados | 0 / 0 |
| Duração | ~29 s |
| Avisos | 2 (fallback inglês de descrições; Folder `TesteOpenApi` reutilizado) |

Plano hierárquico emitiu SDTs de subnível (`…_TesteItem`, `…_TesteItemFolio`, `…_TesteItemFolioDoc`, `…_TestePortfolio`) em Create/Update/Response, além de ListFilters / ListResponse / ListResponse_Item e compartilhados `GxOpenAPI`.

Metadata: `apiTeste_Metadata` reencontrada, `Bytes=116942`, `Sha256='A26C39E7…'`, `PlannedContractHash='C1640995…'`.

## Build All (pós-Wizard)

| Environment | Resultado | `spc0018` |
|---|---|---|
| `NETFrameworkSQLServer004` (.NET Framework / SQL Server) | `Success: Build All` | ausente |
| `NETPostgreSQL155` (.NET / PostgreSQL) | `Success: Build All` | ausente |

Specification/geração cobriu `procTeste_API_Get`, `List`, `Create`, `Update` e `apiTeste`. Avisos ambientais conhecidos (`pmm0003`, cópia `FBiTextSharp`) fora do gate.

## Fora de escopo (explícito)

- Smoke HTTP / OAuth / bateria CRUD — critério 10 e plano de subníveis reservam HTTP ao U15.
- Validação de menus/Extensions Manager linha a linha — Wizard + Build All já exercitam carga da extensão.

## Status

**Aprovado.** Gate da sprint fechado em 2026-08-29.
