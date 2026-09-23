# B127 — Re-smoke IDE: GUID divergente no Preview e homônimo sem herdar posse

Data: 2026-09-22.

Item de backlog: `B127`.

## Escopo

Re-smoke na IDE dos hardenings pós-§6 do `B123`:

1. `86ef414` — com metadata `ownedByThisApi=true` e GUID do Folder **divergente** do vivo,
   o Preview/confirmação **desanuncia** o Folder (`reutilizado; nunca apagar`) e a contagem
   não o inclui.
2. `c358fb2` — Apply/reencontro com metadata ainda `ownedByThisApi=true` + GUID antigo e
   Folder vivo com GUID novo **não herda** posse: a regravação sai com
   `ownedByThisApi=false` e o GUID vivo.

Não muda código nem schema. Não cobre `B126` (Description/contêiner) nem `B125`
(Preview pós-aborto).

## Preparação

KB: `wsEducacaoSpTeste` / Transaction `Teste` / Folder `TesteOpenApi`.
DLL Release já instalada (mesma do `B126`; manifesto inalterado). Gates offline dos
hardenings já PASS.

Baseline: Wizard Apply criou a API com posse
(`TransactionFolderGuid='7dd3ba2b-5842-421f-9d3e-eaaa18d5f3cf'`, metadata V4,
`ownedByThisApi=true`).

Edição externa do File via `D:\Temp\apiTeste_Metadata.json` (a IDE não edita o blob JSON
direto): GUID do Folder trocado para `eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee`, mantendo
`ownedByThisApi=true`. Após a troca, o fingerprint SHA-256 da metadata precisou ser
realinhado ao hash recalculado (`FingerprintHashMismatch` bloqueava o API Object até a
correção).

## Smoke IDE (2026-09-22) — PASS

### 1. Preview com GUID divergente (`86ef414`)

Remover → Preview (cancelado com Não):

- `Folder: TesteOpenApi (reutilizado; nunca apagar)`
- **Não** a frase do `B126` («será preservado — Description ou contêiner divergente»)
- Output: `Remocao cancelada pelo usuario` / nenhuma alteração na KB

### 2. Apply sem herdar posse (`c358fb2`)

Wizard reencontro com Delete + BC + API Object + Metadata + List; Apply
`SuccessWithWarnings`, `Atualizados=28`, `Criados=0`.

Metadata regravada (`Status='Reencountered'`, Bytes=118075,
Sha256=`101D690931AF6BB1F684CC8402479528EABAF63C56E5135D4FD80110D991A047`).

Conferido em `D:\Temp\apiTeste_Metadata.json` e no blob da KB:

| Campo | Valor |
|---|---|
| `ownedByThisApi` | `false` |
| `guid` | `7dd3ba2b-5842-421f-9d3e-eaaa18d5f3cf` (vivo) |
| `wasCreated` | `false` |

Aviso B081 alinhado: Folder preexistente reutilizado; «nunca será removido pela remoção
desta API».

**Conclusão:** os dois hardenings têm evidência IDE. `B127` fechado.
