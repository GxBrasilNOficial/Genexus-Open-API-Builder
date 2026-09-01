# B082 — Plano: sinal de vida no Wizard (abertura e Apply), Sync e Remover

Data: 2026-08-31.
Estado: **registro das Fases A+B já entregues**, com smoke `Empresa` em 2026-08-31. Não reabrir este desenho. A próxima ação de código continua `B108`.
Correlato de backlog: `B082` em `Docs/Foundation/06-BACKLOG_v0.1.md`.
Recado original: `Docs/Implementation/2026-08-29-UX-PROGRESSO-WIZARD-APPLY.md`.

Não misturar com `B108` (preferências e retração).

---

## 1. Problema de produto

Enquanto Apply, Sync ou Remover rodam no **thread da UI** do GeneXus, a IDE fica irresponsiva. O usuário não vê progresso e já houve relato de **fechar o GeneXus na marra** (`Empresa`, ~107 min de apply; Remover ~32 s).

Mostrar só o tempo no relatório final **não resolve** — falta feedback **durante** o bloqueio.

Restrição técnica: `KBModel.Save()` / `Delete()` no SDK exigem afinidade STA/UI; background não é primeira hipótese.

---

## 2. Objetivo do B082

1. **Casca sempre visível** durante abertura lenta do Wizard, Apply, Sync e Remover.
2. **Progresso por objeto** (ou etapa) com contagem quando conhecida.
3. **Abortar cooperativo** entre objetos (Save/Delete em curso termina; KB pode ficar inconsistente — melhor que matar a IDE).
4. **Medição** (ms por item) na Output para calibrar UX e plano de escala.
5. **Aviso honesto** no Resumo quando `planejados` for alto.

Fora de escopo imediato: multi-threading de escrita; cancelar um `Save()` SDK no meio.

---

## 3. Fases

### Fase A — Sonda B082-PROBE (esta sessão)

| Item | Estado |
|---|---|
| `ExtensionBusyProgressDialog` + `ExtensionBusyProgressScope` | Feito |
| Instrumentar Remover (por Delete) | Feito |
| Instrumentar Apply Wizard + Sync (SDTs, Procedures, BC, List, metadata) | Feito |
| Instrumentar abertura do Wizard (timings na Output) | Feito |
| Abort cooperativo + `Pump()` / `DoEvents` | Feito (refinando) |
| Localização pt/es/en | Feito |
| Índice único da KB (`ApiPlanKbObjectNameIndex`) — **reutilizar** o do `ReadForIntentionalChange` no Apply | Feito nesta sessão |
| Preflight SDT com progresso 1/N (sem `GetAll` repetido de SDT) | Feito nesta sessão |
| Índice de **Attributes** no mesmo índice (preflight ainda fazia `Attribute.GetAll` por membro — gargalo em `Tributacao` 26/30) | Feito nesta sessão |
| Tempos por fase no Output após Concluir e aplicar | Feito (`Fase …=N ms`) |
| Casca “carregando” no Preview do Remover + índice (sem `GetAll` por alvo) | Feito nesta sessão |
| Margem inferior nos botões Cancelar / Abortar / Fechar | Feito nesta sessão |

**Validação `Tributacao` / FabricaBrasil18Test (2026-08-31):** Apply completo após índice: criar 27 SDTs ~17 s; reencontro ~21 s; BC+List ~110 s (regrava SDTs). Remover: Preview ~10 s sem casca (corrigido nesta DLL); Delete 33 itens ~33,5 s. Abort no preflight: KB intacta.

**Smoke `Empresa` / `Gx_FabricaBrasil` (2026-08-31, Output da IDE):** abertura `total ate ShowDialog=6401` ms (`PrefsMs=24`, `ContratoMs=1404`, `InterfaceMs=4770`). Apply Wizard List/Get/Create/Update + List + BC, sem Delete: `IndiceKb=2394`, `PreflightAgregado=2160`, `SDTs=35436` (Created=44, Reencountered=3), `Procedures=3224` (Created=4), `ApiObject=10483`, `BusinessComponent=78891`, `List=95867`, `Metadata=22733`, `TotalAposConcluir=251459`. B081: `SuccessWithWarnings`, Criados=51, Atualizados=3, Bloqueados=0, `DuraçãoMs=249062` (~4,2 min; o apply mudo de 2026-08-29 na mesma Transaction tinha sido ~107 min). Aviso único: fallback de descrições em inglês.

### Fora da fila operacional

Abertura ainda passa de 5 s. Na `Empresa`, a maior fatia foi `InterfaceMs=4770` (montagem do diálogo); `ContratoMs=1404` lê o contrato existente (`PrototypeWizardContractReader` / `GetAll` próprios da abertura). **Não** há `ApiPlanKbObjectNameIndex` nesse caminho — o índice único começa no Apply (`IndiceKb`) e no Preview do Remover.

`GetAll` residual depois do índice (não é a próxima ação): `ApiPlanApiObjectWriter`, `ApiPlanMetadataFileWriter`, writers BC/List, e o `Remove()` efetivo (`kbIndex: null` + `GetAll` por `Delete` e checagem pós-`Delete`). Preview do Remover e preflight/Apply de SDT/Procedure já reutilizam o índice.

Isso **não** é a próxima ação de código (`B108`).

### Fase B — Consolidar sonda em B082 produtivo

1. Remover sufixo `[B082-PROBE]` dos títulos — **feito** (Output de tempos permanece `[B082]`).
2. Encerrar sonda: sem comandos de menu extras; manter só o diálogo de progresso — **feito** (nunca houve comando extra).
3. Aviso no Resumo do Wizard quando SDTs+Procedures planejados ≥ 25 — **feito**.
4. Cursor de espera no owner da IDE durante Apply/Remover/Sync/abertura — **feito**.
5. Limitação documentada no aviso de escala e no hint do Abortar: durante um único `Save()` a IDE pode congelar; Abort para no próximo objeto.

Não reabrir fases de performance/corte neste arquivo como fila.

---

## 4. Decisões técnicas registradas

### 4.1 Por que indexar a KB?

Não é para “usar todos os SDTs”. É para **resolver por nome** (~30 nomes do plano): colisão, duplicata, SDT externo, tipos referenciados. O SDK expõe `GetAll` + filtro; o wizard já faz uma varredura em `ApiPlanGenerationStateReader`. O Apply **reutiliza** esse índice (`ReadForIntentionalChangeWithIndex`) em vez de repetir `GetAll` por SDT do plano.

### 4.2 Abort

- `RequestAbort()` + `ThrowIfAbortRequested()` entre objetos.
- `Application.DoEvents()` via `Pump()` nos pontos entre Saves.
- Durante um `Save()` único: clique em Abortar enfileira; para após o Save.

### 4.3 Contrato de escrita

Inalterado: preflight agregado antes do primeiro `Save()`, mesma ordem de objetos, sem escrita parcial intencional no abort.

### 4.4 Quadro de progresso e outros aplicativos

Não usar `TopMost`. Com um monitor, o usuário precisa poder ir ao navegador/Cursor sem o quadro cobrir tudo. O diálogo aparece na barra de tarefas; só sobe sobre o GeneXus quando o GeneXus está em primeiro plano (sem ativar a janela se outro processo estiver na frente).

### 4.5 Remover após abort sem metadata

O Remover exige `api{Transaction}_Metadata`. Abort no meio dos SDTs deixa objetos próprios sem File — Remover bloqueia. Reparar hoje: completar o Apply ou apagar SDTs próprios na IDE. Gap futuro: remoção por posse quando o File não existe.

### 4.6 Rodapé dos diálogos

`Dock.Fill` ignora `Form.Padding`. O quadro de progresso usa um `Panel` com padding; Wizard e relatório B081 têm margem inferior na faixa de botões. O Preview do Remover abre a mesma casca de progresso **antes** do Sim (índice da KB + validação), com `PreviewMs` no Output.

---

## 5. Arquivos principais

| Área | Arquivo |
|---|---|
| Diálogo / scope | `Src/Extension/ExtensionBusyProgressDialog.cs`, `ExtensionBusyProgressScope.cs` |
| Sessão abort/progresso | `Src/Extension/Diagnostics/ApiPlanBusyProgress.cs` |
| Índice KB | `Src/Extension/Diagnostics/ApiPlanKbObjectNameIndex.cs` |
| Orquestração | `Src/Extension/Package.cs` |
| Writers | `ApiPlanSdtWriter`, `ApiPlanProcedureWriter`, `ApiPlanBusinessComponentWriter`, `ApiPlanGeneratedApiRemover` |
| Strings | `Src/Extension/ExtensionLocalization.cs` |

---

## 6. Critérios de aceite (B082 fechado)

1. Apply em KB grande mostra etapa + contador (ex.: `SDTs 3/30`) sem ficar minutos em `0/N`.
2. Remover mostra progresso por objeto deletado.
3. Abortar interrompe antes do próximo objeto (com aviso de inconsistência).
4. Abertura do Wizard > 5 s mostra “carregando” (Fase B).
5. Relatório final B081/B063 mantém duração total; Output registra ms por item da sonda até consolidar.
6. Preview do Remover mostra casca de progresso antes do Sim; Cancelar/Abortar/Fechar não colam no bordo da janela.

---

## 7. Referências

- Recado: `Docs/Implementation/2026-08-29-UX-PROGRESSO-WIZARD-APPLY.md`
- Escala (critério 11 encerrado): `Docs/Implementation/2026-08-29-CRITERIO11-ESCALA-EMPRESA.md`
- Checkpoint: `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md` (`B108` = próxima ação de código; este arquivo é registro, não pauta)
