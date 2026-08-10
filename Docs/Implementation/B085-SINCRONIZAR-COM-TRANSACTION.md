# B085 — Sincronizar com a Transaction

## Objetivo

Comando explícito que compara a Transaction atual com a metadata, apresenta diferenças com escolhas no delta, exige confirmação e só então regrava objetos próprios com preflight completo — sem reabrir o wizard.

## Comportamento

1. Resolve a Transaction pelo menu de contexto ou seletor nativo.
2. Exige File `api<Transaction>_Metadata` próprio e bloco `transactionStructure` com `attributeGuid` por atributo.
3. Diff por GUID: adicionados, removidos, renomeados, tipo/gravabilidade; avisos de remoção/tipo/BC.
4. UI própria: marcar inclusão de campos novos em Response/Create/Update/ListFilters; conflitos de SDT editado → Replace / Keep / Cancel.
5. Cancelar = zero escrita. Aplicar reconstrói `ApiPlan` a partir da metadata + escolhas, roda preflight e writers (SDTs → Procedures → API/BC/List → metadata).
6. Keep em SDT preserva a estrutura desse SDT (`ConfigureSdt` omitido) em **todas** as etapas do Sync que reencontram SDTs (B040-B046, B055/BC e B070/List); Replace sobrescreve.
7. Conflito de SDT compara **nomes dos membros** com o snapshot da metadata; alteração só de Description do membro não dispara conflito.
8. Posse no preflight Sync: File de metadata próprio com `ownership.apiName` / `ownership.apiGuid` (e `schemaVersion`) — **sem** exigir que o Service Source atual case com o ApiPlan novo (`IsManagedApiObject`). O Wizard continua exigindo integridade B067 + Source gerenciado via `Resolve`.
9. Ordem dos campos reconstruídos: preserva a ordem da metadata; campos adicionados entram no fim; deduplicação por GUID (não por nome).

## Metadata

- `transactionStructure[]` com snapshot completo da Transaction na geração.
- `fields.*.attributeGuid`, `fields.required[].attributeGuid`, `order[].attributeGuid`.
- KB de teste pode regenerar metadata pelo Wizard antes do sync (sem metadata antiga sem GUID).

## Código / menu

- `ApiPlanTransactionSyncComparer` / `ApiPlanTransactionSyncFieldSelection` / `ApiPlanTransactionSyncOrchestrator` / `ApiPlanTransactionSyncDialog`
- Comando `Sincronizar com a Transaction` em `Package.cs` e `GenexusOpenApiBuilder.package` (menu principal e contexto; `Remover API gerada` fica por último)
- Preflight de Sync permite atualizar o contrato B067 e o Source/Rules gerados de Procedures/API (B055/B070) de propósito; o Wizard continua bloqueando divergência não intencional
- UI de campos adicionados: grade 2x2 (Response/CreateRequest na primeira linha; UpdateRequest/ListFilters na segunda), com GUID abreviado na lista
- Testes: `Tests/TransactionSync/Test-ApiPlanTransactionSyncComparer.ps1`, `Tests/TransactionSync/Test-ApiPlanTransactionSyncFieldSelection.ps1`; contrato de posse Sync em `Tests/ApiObjectOwnership/Test-ApiPlanApiObjectOwnership.ps1`

## Validação manual U15 (2026-08-08)

Transaction `Teste`, KB de teste.

1. Wizard regenerou API + metadata com `attributeGuid` / `transactionStructure`.
2. Sync sem mudanças → Output “Nenhuma diferenca…”; sem janela; KB intacta.
3. Campo adicionado (`TesteObs` / depois `TesteObs2`) → diff `+`; UI 2x2; Aplicar concluiu SDTs → Procedures → BC/API → List → metadata (`ResponseFields` atualizado).
4. Rename por GUID (`TesteObs` → `TesteObs1`) → `~ antigo -> novo`; Aplicar concluiu sem falso add/remove.
5. Cancelar na UI com diff pendente → “Sincronizacao cancelada”; zero escrita.
6. Conflito Replace: membro manual em `sdtTeste_API_Response` → conflito listado; Replace + Aplicar → `PreservedSdts=0`, SDT recarregado na IDE.
7. Conflito Keep: membro manual `ManualKeep` em `sdtTeste_API_Response` → Keep + Aplicar → `PreservedSdts=1` e membro preservado no SDT (após correção que propaga `preserveSdts` também para BC/List; antes o Keep só evitava ConfigureSdt na 1ª etapa e BC/List regravavam o Response).
8. Correções durante o U15: preflight Sync para B067/Source B055; layout 2x2; falso positivo ListFilters (membros From/To); Keep através de BC/List.

## Correção pós-Alpha — posse Sync (2026-08-10)

Código: `IsOwnedApiObjectForSync` e preflight de metadata no modo Sync passam a usar só ownership da metadata; seleção de campos extraída para `ApiPlanTransactionSyncFieldSelection` com ordem estável.

### Validação manual U15 (2026-08-10)

Transaction `NotaFiscal`, KB de teste, DLL Release instalada (manifesto inalterado).

1. Sync sem delta → `Inalterados: 5`, `Blocked=0`, headline “Nenhuma sincronizacao necessaria.”
2. Campo `NotaFiscalObs3` (VARCHAR) adicionado → diff `+ NotaFiscalObs3`; UI 2x2 com inclusão em Response/CreateRequest/UpdateRequest (ListFilters desmarcado).
3. **Aplicar sincronizacao** → preflight aprovado; `Updated=13`, `Blocked=0`, `PreservedSdts=0`; Output com B040–B046 reencontrados, B071–B079 (`ResponseFields=6`, `CreateFields=5`, `UpdateFields=5`), B070, B060 (`Reencountered`, Guid `d0e010a7-7d26-4f2c-83a6-5195d211aa75`) e B067; avisos só de idioma e Folder reutilizado.

Status: **concluído** (fix de posse Sync validado no U15).
