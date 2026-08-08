# B085 — Sincronizar com a Transaction

## Objetivo

Comando explícito que compara a Transaction atual com a metadata, apresenta diferenças com escolhas no delta, exige confirmação e só então regrava objetos próprios com preflight completo — sem reabrir o wizard.

## Comportamento

1. Resolve a Transaction pelo menu de contexto ou seletor nativo.
2. Exige File `api<Transaction>_Metadata` próprio e bloco `transactionStructure` com `attributeGuid` por atributo.
3. Diff por GUID: adicionados, removidos, renomeados, tipo/gravabilidade; avisos de remoção/tipo/BC.
4. UI própria: marcar inclusão de campos novos em Response/Create/Update/ListFilters; conflitos de SDT editado → Replace / Keep / Cancel.
5. Cancelar = zero escrita. Aplicar reconstrói `ApiPlan` a partir da metadata + escolhas, roda preflight e writers (SDTs → Procedures → API/BC/List → metadata).
6. Keep em SDT preserva a estrutura desse SDT (`ConfigureSdt` omitido); Replace sobrescreve.
7. Conflito de SDT compara **nomes dos membros** com o snapshot da metadata; alteração só de Description do membro não dispara conflito.

## Metadata

- `transactionStructure[]` com snapshot completo da Transaction na geração.
- `fields.*.attributeGuid`, `fields.required[].attributeGuid`, `order[].attributeGuid`.
- KB de teste pode regenerar metadata pelo Wizard antes do sync (sem metadata antiga sem GUID).

## Código / menu

- `ApiPlanTransactionSyncComparer` / `ApiPlanTransactionSyncOrchestrator` / `ApiPlanTransactionSyncDialog`
- Comando `Sincronizar com a Transaction` em `Package.cs` e `GenexusOpenApiBuilder.package` (menu principal e contexto; `Remover API gerada` fica por último)
- Preflight de Sync permite atualizar o contrato B067 e o Source/Rules gerados de Procedures/API (B055/B070) de propósito; o Wizard continua bloqueando divergência não intencional
- UI de campos adicionados: grade 2x2 (Response/CreateRequest na primeira linha; UpdateRequest/ListFilters na segunda), com GUID abreviado na lista
- Teste: `Tests/TransactionSync/Test-ApiPlanTransactionSyncComparer.ps1`

## Validação manual U15 (2026-08-08)

Transaction `Teste`, KB de teste.

1. Wizard regenerou API + metadata com `attributeGuid` / `transactionStructure`.
2. Sync sem mudanças → Output “Nenhuma diferenca…”; sem janela; KB intacta.
3. Campo adicionado (`TesteObs` / depois `TesteObs2`) → diff `+`; UI 2x2; Aplicar concluiu SDTs → Procedures → BC/API → List → metadata (`ResponseFields` atualizado).
4. Rename por GUID (`TesteObs` → `TesteObs1`) → `~ antigo -> novo`; Aplicar concluiu sem falso add/remove.
5. Cancelar na UI com diff pendente → “Sincronizacao cancelada”; zero escrita.
6. Conflito Replace: membro manual em `sdtTeste_API_Response` → conflito listado; Replace + Aplicar → `PreservedSdts=0`, SDT recarregado na IDE.
7. Correções durante o U15: preflight Sync para B067/Source B055; layout 2x2; falso positivo ListFilters (membros From/To).

Ramo Keep (preservar membro manual) permanece opcional, não bloqueante.

Status: **concluído**.
