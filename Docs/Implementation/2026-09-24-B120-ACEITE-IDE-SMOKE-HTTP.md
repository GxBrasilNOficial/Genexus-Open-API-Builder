# B120 — Aceite IDE e smoke HTTP do envelope flat do List

Data: 2026-09-24.

Item de backlog: `B120`.

## Escopo

Validação de campo da opção A (outs flat do `List`) + A2 (remoção do SDT
`*ListResponse` órfão no reapply), após implementação na extensão e install da
DLL Release na IDE U15.

Não altera código neste documento. Não cobre publicação de release.

## Preparação

- KB: `wsEducacaoSpTeste` / Transaction `Teste` / Folder `TesteOpenApi` / API
  `apiTeste`.
- Environments: .NET Framework/SQL Server e .NET Core/PostgreSQL (URLs e OAuth em
  `Temp/wsEducacaoSpTeste-local-test-environments.md`, não versionado).
- DLL canônica U14+ instalada manualmente (`Install-ExtensionForGeneXus18.bat`);
  manifesto `.package` inalterado nesta frente.
- Regeneração: Wizard após limpeza de artefatos órfãos / recuperação de diário
  (sessão de campo); `Build All` nos dois environments.

## Smoke HTTP List — matriz §6 (2026-09-24)

Captura local: `Temp/b120-apiteste-smoke-2026-09-24.json` (não versionado).

| Caso | Framework | PostgreSQL |
| --- | --- | --- |
| Sem token | `401` | `401` |
| Auth `page=1&pageSize=10` | `200` — chaves flat; **sem** `ListResponse` | `200` — flat; **sem** `ListResponse` |
| Auth + filtro `Testeid` | `200` — quatro chaves flat | `200` — quatro chaves flat |
| `pageSize` acima do máximo | `400` + `invalid_request` | `400` + `invalid_request` |

YAML (`apiTeste.yaml` nos dois `web/`): `ListOutput` com `Items`, `Pagination`,
`AppliedFilters`, `ErrorResponse` — sem schema `ListResponse` no serviço.

**Nuance multiplataforma (serializador):** no .NET Core, `AppliedFilters` vazio e
`Items` vazio no `400` podem ser omitidos do JSON; no Framework as chaves
aparecem. Não é unwrap de `ListResponse`. Consumidores devem tratar
`AppliedFilters` como opcional no corpo (OpenAPI não a marca `required`).

## Regressão Get / Create / Update / Delete (2026-09-24)

Captura local: `Temp/b120-apiteste-regressao-crud-2026-09-24.json` (não
versionado).

| Caso | Framework | PostgreSQL |
| --- | --- | --- |
| Get inexistente | `404` `not_found` — `GetResponse`+`ErrorResponse` | igual |
| Create | `201` + `Location` — `CreateResponse`+`ErrorResponse` | igual |
| Get / Update / Get | `200` — envelopes de serviço, não flat do List | igual |
| Delete / Get pós-Delete / Delete inexistente | `200` / `404` / `404` | igual |

Delete exigiu permissão GAM `apiteste_Services_Delete` = Permitir no usuário
`goab_api_teste` (no PostgreSQL a permissão faltava após regenerar a API; no
Framework já estava Permitir). Ajuste feito no backoffice GAM de cada
environment.

## Wrapper gerado do List (Teste 2)

- PostgreSQL `apiteste_services.cs` / `gxep_list`: monta `Items` /
  `Pagination` / `AppliedFilters` / `ErrorResponse`; **sem** `nonNullCount` no
  List; **sem** `ListResponse`.
- Framework `apiteste.cs` / `gxep_list` (WCF Wrapped): retorno = coleção
  `Items` + outs; **sem** unwrap por `nonNullCount`; **sem** `ListResponse`.

## Sync (Teste 3)

Output B085/B081 na Transaction `Teste`:

- Diff: Adicionados=0; Removidos=0; Modificados=0; Inalterados=17
- `SuccessWithWarnings` — «Nenhuma sincronizacao necessaria»
- Pasta `TesteOpenApi`: sem `sdtTeste_API_ListResponse` (permanece
  `ListResponse_Item` + Filters + demais SDTs de Create/Update/Response)

## Pasta KB após aceite

Presentes: `apiTeste`, cinco Procedures (`List`/`Get`/`Create`/`Update`/`Delete`),
SDTs de request/response hierárquicos, `ListFilters`, `ListResponse_Item`.
Ausente: `sdtTeste_API_ListResponse`.

## Conclusão

Aceite IDE/HTTP do `B120` **fechado** na `Teste`/`wsEducacaoSpTeste` nos dois
environments. Critérios §14.5 do plano atendidos, com a nuance de omissão de
nulos/vazios no .NET Core registrada acima.

## Aberto / residual (fora do aceite HTTP)

- Probes descartáveis `ProbeB120` na KB: arquivar/remover quando conveniente
  (não são entrega).
- Remover com alvo já ausente ainda na metadata (`TargetAbsentBeforeDelete`) —
  observado na recuperação da sessão; não bloqueia o flat do List.
- Documentação pública (`README` / `Docs/Public/*`) sobre o envelope flat do
  List: parcial no `CHANGELOG`; alinhar no próximo corte se o contrato for
  anunciado ao consumidor.
