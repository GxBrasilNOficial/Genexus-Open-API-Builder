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
- `services[]` inclui `securityLevel` por serviço; o Sync reconstrói o do Delete a partir desse campo, não de `security.level`.
- KB de teste pode regenerar metadata pelo Wizard antes do sync (sem metadata antiga sem GUID).

## Código / menu

- `ApiPlanTransactionSyncComparer` / `ApiPlanTransactionSyncFieldSelection` / `ApiPlanTransactionSyncOrchestrator` / `ApiPlanTransactionSyncDialog`
- Comando `Sincronizar com a Transaction` em `Package.cs` e `GenexusOpenApiBuilder.package` (menu principal e contexto; `Remover API gerada` fica por último)
- Preflight de Sync permite atualizar o contrato B067 e o Source/Rules gerados de Procedures/API (B055/B070) de propósito; o Wizard continua bloqueando divergência não intencional
- UI de campos adicionados: grade 2x2 (Response/CreateRequest na primeira linha; UpdateRequest/ListFilters na segunda), com GUID abreviado na lista
- Testes: `Tests/TransactionSync/Test-ApiPlanTransactionSyncComparer.ps1`, `Tests/TransactionSync/Test-ApiPlanTransactionSyncFieldSelection.ps1`; contrato de posse Sync em `Tests/ApiObjectOwnership/Test-ApiPlanApiObjectOwnership.ps1`; trava do `SecurityLevel` do Delete em `Tests/WizardContract/Test-PrototypeWizardExistingApiFilters.ps1`

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

## Correção — SecurityLevel do Delete (2026-08-31)

O Apply intencional do Sync regrava o Service Source via writer BC. Até este conserto o orquestrador lia só `security.level` (nível global) e `ApiPlanBuilder.Build` no Sync não recebia o `DesignModel`, então o Delete mais restrito (ou mais frouxo) que o restante da API virava o nível global.

Código: `ReadPersistedDeleteSecurityLevel` em `ApiPlanTransactionSyncOrchestrator` lê `services[].securityLevel` do item `Delete` e passa no `PrototypeWizardReviewSelection`; `Package.cs` monta o plano com `ApiPlanBuilder.Build(knowledgeBase.DesignModel, transaction, selection)`. Manifesto inalterado (só DLL).

### Validação manual U15 (2026-08-31)

KB `wsEducacaoSpTeste`, Transaction `NotaFiscal` / `apiNotaFiscal`, DLL Release vigente.

1. Wizard: API `Authorization`, Delete `Authentication`, Completar REST via Business Component `True` → Apply `SuccessWithWarnings`, `Blocked=0`.
2. Service Source **antes** do Sync: `Delete` com `[SecurityLevel(Authentication)]`; List/Get/Create/Update com `Authorization`.
3. Sync sem delta → nenhuma escrita (esperado; não prova o conserto).
4. Delta mínimo: `NotaFiscalObs` Length `40.0` → `41.0`. Preview: `Modificados=1`, `Inalterados=8`, sem Added e sem conflito de SDT.
5. Aplicar: `Trigger='SyncB085'`; Procedures reencontradas incluindo `procNotaFiscal_API_Delete`; BC com `DeleteProcedureGuid`; metadata `Reencountered` Guid `bc37000d-8132-40fd-b99b-2b55a319abe1`; B067 `PlannedContractHash='C4C8E598…'`; relatório `Atualizados=15`, `Bloqueados=0`, `Avisos=2`.
6. Service Source **depois** do Sync: `Delete` permanece `[SecurityLevel(Authentication)]`; os outros, `Authorization`.

Ensaio: o Length 41 ficou na Transaction; o operador pode reverter para 40 e sincronizar de novo se quiser limpar o delta.

Status: **concluído** (preservação do SecurityLevel do Delete no Sync validada no U15).

## Escrita parcial do BC — drift API Object ↔ metadata (2026-09-03)

### Sintoma

Se o Sync (ou o Apply com BC) **aborta depois** de `ApiPlanBusinessComponentWriter.Apply` ter gravado o API Object mas **antes** de concluir Procedures e/ou metadata, o Service Source do `api<Nome>` fica à frente do hash B067 em `api<Nome>_Metadata`. O preflight seguinte bloqueia Wizard e Sync com `BaselineServiceSourceHashMismatch` até a KB ser realinhada.

Evidência: Sync Keep em `wsEducacaoSpTeste` / `NotaFiscal` (2026-09-03) — BC falhou em `procNotaFiscal_API_Get`; tentativa de Replace imediata bloqueou no preflight; **Remover API gerada** + Wizard restaurou baseline.

### Causa no código

Em `ApiPlanBusinessComponentWriter.Apply`, `saveSteps` grava o **API Object primeiro** e as Procedures depois (`SaveApi` → `SaveProcedure` Get/Create/Update/Delete). A metadata B060/B067 só é escrita no **final** do Sync/Apply. Qualquer falha no meio deixa API atualizado e metadata antiga.

### Recuperação operacional (hoje)

1. **Remover API gerada** na Transaction + **Wizard** completo (recomendado em KB de teste).
2. Não editar manualmente o hash em `api<Nome>_Metadata` salvo decisão consciente de auditoria.

### Correções possíveis (código — pendente)

| Prioridade | Ação | Efeito |
|---|---|---|
| **P1 (recomendada)** | Reordenar `saveSteps` para gravar o API Object **por último**, após todas as Procedures passarem em `Save()`. | Se Get/Create/Update/Delete falhar, API e metadata permanecem alinhados; preflight não trava. |
| P2 | Na falha do Sync/Apply, detectar drift API↔metadata e orientar no B081 («Remover + Wizard») com mensagem explícita. | Não evita escrita parcial; melhora diagnóstico. |
| P3 | Rollback do Service Source do API Object em `catch` quando Procedures falham depois de `SaveApi`. | Mais frágil (estado GeneXus, Events, variáveis). |
| Fora de escopo imediato | Transação atômica multi-objeto na IDE. | SDK não oferece commit/rollback transacional real. |

**Próximo passo sugerido:** P1 na Etapa 1B residual do `B082` ou frente dedicada pequena, com teste que simule falha em `SaveProcedure` e confirme API inalterado.

### Validação Keep/Replace (2026-09-03)

KB `wsEducacaoSpTeste`, `NotaFiscal`: edição manual `NotaFiscalObs3` → `NotaFiscalObs3Manual` → conflito de SDT. **Keep** preservou estrutura (`Unchanged` + aviso); BC interrompido (tensão esperada). Após recuperação, **Replace** (`PreservedSdts=0`, Response `Reencountered`) concluiu Sync com `Blocked=0`.
