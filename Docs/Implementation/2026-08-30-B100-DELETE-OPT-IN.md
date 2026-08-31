# B100 — Serviço Delete opt-in

Fecha o serviço REST `Delete` (opt-in, desligado por padrão) na KB `wsEducacaoSpTeste`, Transaction
`NotaFiscal` / `apiNotaFiscal`, GeneXus 18 U15. Código na working tree da extensão; publicação
`0.1.0-alpha.6` permanece corte separado.

---

## 1. Contrato gerado

Quando o checkbox `Delete` está marcado no Wizard:

- Procedure `proc<Nome>_API_Delete`
- rota `DELETE` no mesmo path da chave do Get
- Events `Delete.After` com `&RestCode = &RestStatusCode`
- `SecurityLevel` próprio (aba Segurança); aviso se `None`
- HTTP: `200` (chave removida), `404` `not_found` (inexistente), `422` `validation_error` (recusa do BC, inclusive integridade)

Quando está desmarcado (padrão e fallback de File antigo sem o membro): o API Object não declara
`Delete`, não há `proc*_API_Delete` nova, e a aba Segurança não sugere nível para um serviço que
não será gravado.

API já gerada **não** herda o default da KB ao reabrir: o Delete só entra se o operador marcar.

Confirmação no apply: apagar o cabeçalho apaga as linhas filhas na mesma transação atômica.
Reabrir uma API que já tem Delete não pede a confirmação de novo.

---

## 2. Camadas anti-acidente (IDE)

1. Preferência KB `services.delete` default e fallback `false`.
2. Checkbox desmarcado em API nova; API existente não herda o default da casa.
3. Confirmação consciente ao marcar; aviso se Delete estiver com `None`.
4. Recusa do BC (`Load` → `Delete()` → Commit ou Rollback); sem exclusão forçada.

---

## 3. Gate HTTP

Ambiente: IIS local, GAM `Authorization`, usuário `goab_api_teste`. Após o Build All, a permissão
`apinotafiscal_Services_Delete` precisou de **Permitir** no próprio usuário (403 GAM `code` 139
enquanto só o papel Administrator tinha a permissão).

| Caso | Framework / SQL Server | .NET / PostgreSQL |
|---|---|---|
| Sem token | 401 | 401 |
| Id inexistente | 404 `not_found` | 404 `not_found` |
| Create 201 + Delete 200 + GET 404 | sim (id 23) | sim (id 10) |
| Recusa BC (integridade) | 422 `validation_error` / `CannotDeleteReferencedRecord` na nota 1; GET depois 200 | dispensado em 2026-08-30 |

O 422 no Framework veio da rule da Transaction `NotaFiscal`: o BC recusa delete se existe `GuiaPed`
com `GuiaPedNfId` igual à nota. `GuiaPedNfId` tem `NoAccept` via BC; o Wizard bloqueia o campo no
Create/Update de `apiGuiaPed`. A amarração nota↔guia no produto é `wpAtualizacaoDeNotaFiscalDeGuias`
/ `procAtualizacaoDeNotaFiscalDeGuias` (UPDATE direto), não o BC.

**Decisão operacional 2026-08-30:** o 422 no PostgreSQL não é essencial. 200/404 já passaram nos
dois environments; o 422 de integridade ficou comprovado no Framework. O mesmo gerador emite o
Source nos dois.

Build All nos dois environments sem `spc0018` nas Procedures da API (aviso ambiental
`FBiTextSharp.dll` no Framework).

---

## 4. Sync (`B085`) e o nível próprio do Delete

O `SecurityLevel` do Delete não segue o nível global da API. O Sync regrava o Service Source no Apply intencional; desde 2026-08-31 ele lê `services[].securityLevel` do item Delete na metadata e monta o ApiPlan com o contrato da KB. Sem isso o writer BC copiava `security.level` para o Delete.

Evidência U15: `Docs/Implementation/B085-SINCRONIZAR-COM-TRANSACTION.md` (seção 2026-08-31). HTTP do Delete permanece o da seção 3; este recorte não refez chamada REST.

---

## 5. Wizard no reencontro — rádio global (2026-08-31)

O Apply do Wizard sobre API existente deve gravar o que está na UI: rádio para List/Get/Create/Update, combo para Delete. Até este conserto o `CreateServices` copiava `securityLevel` persistido por serviço também nos quatro obrigatórios, então mudar o rádio não regrava o Service Source.

Código: `ResolveReencounteredServiceSecurityLevel` em `ApiPlan.cs` — Delete usa o combo; os demais herdam `plan.Security` (o rádio). Path e `operationId` da API existente continuam preservados.

Smoke U15 na `NotaFiscal` / `apiNotaFiscal`, reencontro, REST via Business Component marcado:

1. Abertura: global `Authorization`, Delete `Authentication`.
2. Operador mudou o rádio para `Authentication` (combo inalterado).
3. Apply `SuccessWithWarnings`, `Atualizados=15`, `Bloqueados=0`, `ApplyBusinessComponent=True`.
4. Service Source: List, Get, Create, Update e Delete com `[SecurityLevel(Authentication)]`.

---

## 6. Fora deste recorte

- Corte GitHub `0.1.0-alpha.6` (tag, notas trilíngues, dois assets DLL): autorização humana.
- `B082` (progresso na UI): outra sessão.
- `B105` (teto de detalhe de erro pelo chamador): Sprint 9 se houver folga, senão Sprint 10.
- Regenerar `apiGuiaPed` só para o 422 do PostgreSQL: cancelado.

---

## 7. Testes offline

`Tests/WizardPreferences/Test-PrototypeWizardPreferences.ps1` (default `false`, fallback legado).
`Tests/WizardLifecycle/Test-ApiPlanWizardHierarchicalLifecycle.ps1` (recusar confirmação desmarca
Delete). `Tests/WizardContract/Test-PrototypeWizardExistingApiFilters.ps1` (reencontro: rádio nos quatro, combo no Delete; Sync lê
`services[].securityLevel` do Delete). Contrato OpenAPI: `Delete` entra em
`PrototypeWizardContract.ServiceNames` e na trava `Tests/OpenApiContract/`.
