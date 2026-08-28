# B099v — Validação em runtime multinível (Fase 5-A)

Data: 2026-08-28.
Frente: Sprint 9 — Fase 5-A (`B099v`).
Escopo: corrigir a agregação `count()` com PK composta herdada; regerar `apiTeste` de quatro níveis com o gerador vigente; smoke HTTP multinível nos dois environments; critério 9 (YAML publicado + geração de cliente). Metadata V2, sync e remoção permanecem na Fase 6 (`B099b`).

## Decisões de recorte

- A Fase 5 (`B099a`) fechou Wizard e apply IDE; esta fase fecha **runtime** (HTTP + contrato OpenAPI publicado) antes que a Fase 6 grave metadata V2 sobre o contrato hierárquico.
- Critérios 6 (`Gx_FabricaBrasil`) e 10 (smoke `Gx18u13`) **fora** deste recorte — permanecem gates da sprint.
- Script de smoke e capturas JSON ficam em `Temp/` (ignorado pelo Git): `Invoke-B099vHierarchicalSmoke.ps1`, `b099v-smoke-hierarchical-2026-08-28.json`, saída de clientes em `Temp/b099v-openapi-client/`.

## Item 1 — correção `count()` (PK própria)

| Peça | Alteração |
|---|---|
| `Src/Domain/ApiPlanListHierarchicalContract.cs` | `ResolveAggregateAttributeName` passa a preferir a primeira PK com `!IsForeignKey`; fallback estável para `PrimaryKey[0]` e `Fields[0]`. Residual consciente: quando **todas** as partes da PK do subnível são FK (ex. `InvoiceId`+`ProductId`), o fallback ainda pode devolver o atributo do cabeçalho — ver dívida residual em `2026-08-26-B098-LIST-CONTADORES.md` |
| `Tests/ListHierarchical/Baselines/InheritedPrimaryKey.txt` | ouro atualizado: `count(LineId)` / `LineCount=LineId` |
| `Tests/ListHierarchical/Test-ApiPlanListHierarchical.ps1` | asserção que rejeita `count(HeaderId)` na fixture `InheritedPrimaryKey` |

Validação offline: `PASS: ApiPlanListHierarchicalBaseline`. Build Release canônico: 0 avisos, 0 erros.

## Item 2 — regeração IDE + Build All

KB `wsEducacaoSpTeste`, Transaction `Teste`, API `apiTeste`, GeneXus 18 U15.

1. DLL canônica reinstalada (`Install-ExtensionForGeneXus18.bat` como administrador).
2. Wizard em reencontro, quatro subníveis (`TesteItem` → `TesteItemFolio` → `TesteItemFolioDoc`, irmão `TestePortfolio`); contador ligado em `TesteItem` e `TestePortfolio`.
3. Apply: `SuccessWithWarnings`, `Created=0`, `Updated=27`, `Blocked=0`, `Avisos=2`, `DuraçãoMs=25630`.
4. Metadata `apiTeste_Metadata`: `Reencountered`, `Bytes=81932`, `Sha256='255D344F994CA9FE9C76693CA88079F45E359DE1F312E28FA90E23BEDB0D5BB6'`, `PlannedContractHash='21F955426F5A11630ECEBC05BA582D56F6B64CB8D6B8E157D6B97381A87F22CD'`.
5. `Build All` passou em `NETPostgreSQL155` e `NETFrameworkSQLServer004` sem `spc0018`; SDTs hierárquicos e documentação REST de `apiTeste` gerados nos dois environments. Aviso `FBiTextSharp.dll` só no Framework (ambiental).

Source de `procTeste_API_List` (runtime pós-apply): `&Item.TesteItemCount = count(TesteItemId)` e `&Item.TestePortfolioCount = count(TestePortfolioId)` — não `count(TesteId)`.

## Item 3 — smoke HTTP multinível

Script: `Temp/Invoke-B099vHierarchicalSmoke.ps1`. Credenciais e URLs base: `Temp/wsEducacaoSpTeste-local-test-environments.md` (fora do Git).

Bateria (16 checks por environment):

| Passo | Esperado | Resultado |
|---|---|---|
| POST Create hierárquico | 201 | OK |
| GET após Create | 200; 2 itens, 1 portfolio, 2 FolioDoc | OK |
| PUT sem Replace | 200; linhas preservadas | OK |
| PUT com Replace (`TesteItemReplace`, `TesteItemFolioReplace`, `TesteItemFolioDocReplace`, `TestePortfolioReplace`) | 200; 1 item, 1 portfolio (id 3), 1 FolioDoc | OK |
| GET List filtrado | 200; `TesteItemCount=1`, `TestePortfolioCount=1` | OK |

**.NET Framework / SQL Server** e **.NET / PostgreSQL**: `passed=True` (2026-08-28 07:17 local).

Nota operacional: Create exige PK de cabeçalho explícita (`TesteId`, `TesteDate`, `TesteCodigo`) no payload; omitir gera colisão (`DuplicatePrimaryKey`) porque as rules preenchem valores repetidos quando a PK não vem no corpo.

## Item 4 — critério 9 (contrato OpenAPI publicado)

YAML:

- `C:\KBs\wsEducacaoSpTeste\NETPostgreSQL155\web\apiTeste.yaml`
- `C:\KBs\wsEducacaoSpTeste\NETFrameworkSQLServer004\web\apiTeste.yaml`

Conferência manual (2026-08-28):

- coleções aninhadas em Create/Update/Response (`TesteItem` → `Folio` → `FolioDoc`, `TestePortfolio`);
- marcadores `*Replace` no Update;
- `sdtTeste_API_ListResponse_Item` expõe apenas `TesteItemCount` e `TestePortfolioCount` — sem arrays aninhados no item de lista;
- `Messages[]` com `$ref` para `sdt_API_ErrorMessage`; sem `maxLength` em `Message`.

Geração de cliente (`openapi-generator-cli 5.3.1`, Exit Code 0):

- PostgreSQL: `typescript-fetch`, `csharp`
- Framework: `typescript-fetch`, `csharp`

Saída local: `Temp/b099v-openapi-client/{pg-ts,pg-cs,fw-ts,fw-cs}/`.

## Fora deste recorte

- Metadata `GOAB_API_METADATA_B060_V1` / integridade B067 sem árvore hierárquica → Fase 6 (`B099b`).
- Remover / Sync em API hierárquica → Fase 6–7.
- Limiares de escala (critério 11): apply em 25,6 s (alerta só acima de 60 s); não medido nesta frente.

## Próxima ação

Fase 6 (`B099b`): metadata hierárquica V2, sync e integridade com a árvore de subníveis.
