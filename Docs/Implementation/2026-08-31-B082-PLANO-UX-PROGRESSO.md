# B082 — Plano: sinal de vida no Wizard (abertura e Apply), Sync e Remover

Data: 2026-08-31.
Estado: **registro das Fases A+B já entregues**, com smoke `Empresa` e `ShowcaseUnanimo`/`Company` (2026-09-01). Este documento permanece como histórico da entrega do corte `0.1.0-alpha.7`.

**Superado em parte por `Docs/Implementation/2026-09-02-B082-PLANO-HARDENING-E-DESEMPENHO.md`.** Aquele plano reabriu o `B082` deliberadamente entre 2026-09-02 e a aceite da Etapa 1A em 2026-09-03; desde então a próxima ação de código é `B108` (`Docs/Implementation/2026-08-31-B108-PLANO-PREFERENCIAS-E-RETRACAO.md`). O **item 4** da seção «Fora da fila operacional» abaixo (índice/`GetAll` incompleto, classificado ali como P2 de performance) está **revogado**: a medição de 2026-09-02 mostrou que é a maior fatia isolada de custo da extensão. Os itens 1, 2 e 3 foram absorvidos como Etapas 2 e 3 daquele plano.
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
4. **Medição:** ms **por item** na casca (`ElapsedMs`); totais de **fase** na Output (`Fase SDTs=…`, `PreviewMs=…`) para calibrar UX e plano de escala. Sem dump de ms por objeto na Output.
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
| Ms por item na casca (`ElapsedMs`); nomes `[B082] SDT Created` na Output, sem ms por objeto | Feito (alinhado ao critério 5) |
| `RefreshFolders` após criar `GxOpenAPI` no mesmo índice (BC não chama `CreateSharedFolder` de novo) | Feito 2026-09-01 |
| Casca “carregando” no Preview do Remover + índice (sem `GetAll` por alvo) | Feito nesta sessão |
| Margem inferior nos botões Cancelar / Abortar / Fechar | Feito nesta sessão |

**Validação `Tributacao` / FabricaBrasil18Test (2026-08-31):** Apply completo após índice: criar 27 SDTs ~17 s; reencontro ~21 s; BC+List ~110 s (regrava SDTs). Remover: Preview ~10 s sem casca (corrigido nesta DLL); Delete 33 itens ~33,5 s. Abort no preflight: KB intacta.

**Smoke `Empresa` / `Gx_FabricaBrasil` (2026-08-31 Apply; 2026-09-01 Sync e Remover Preview):** abertura `6401` ms; Apply ~4,2 min (`DuraçãoMs=249062`, Criados=51, Bloqueados=0). Sync: casca **Preparando sincronização** antes do relatório; `PreviewMs=5089`; diff `Inalterados: 221`; KB intacta; B081 `Nenhuma sincronizacao necessaria`. Remover: casca **Preparando remoção**; `PreviewMs=2525`; plano 4 Procedures + 44 SDTs; usuário **Não**; `Remocao cancelada`; KB intacta.

**Smoke `ShowcaseUnanimo` / `Company` (2026-09-01):** KB sem Folder `GxOpenAPI` e sem `sdt_API_*`. Primeiro Apply: SDTs Created=8 / Reencountered=0; B081 lista Folders `GxOpenAPI` e `CompanyOpenApi` em Criados; REST via BC `3940` ms e List `3307` ms no mesmo índice; `SuccessWithWarnings`, Criados=16, Bloqueados=0, `DuraçãoMs=11334`. Um Folder `GxOpenAPI` na KB. Exercita `RefreshFolders` no segundo `CreateOrReencounter`.

### Fora da fila operacional

Abertura ainda passa de 5 s. Na `Empresa`, a maior fatia foi `InterfaceMs=4770` (montagem do diálogo); `ContratoMs=1404` lê o contrato existente (`PrototypeWizardContractReader` / `GetAll` próprios da abertura). O índice único começa no Apply (`IndiceKb`), no Preview do Remover e no Preview do Sync. A abertura do Wizard continua sem esse índice (`ContratoMs` / `InterfaceMs`).

`GetAll` residual depois do índice (não é a próxima ação): ver item 4 abaixo. Preview do Remover, Preview do Sync e preflight/Apply de SDT/Procedure já reutilizam o índice.

Polimento B082 para **outra sessão** (junto com o residual acima; **não** misturar com `B108`):

1. **Casca × relatório B081 no Apply/Sync (P2, UX).** O `using` da casca ainda está aberto quando `ShowFinalReport` roda (`Package.cs`). O Remover já fecha a casca e só então abre o B081. Camadas de UI; sem evidência de gravação incorreta. Origem: painel pré-push 2026-09-01. Fechar quando: B081 abre com casca fechada; smoke Apply/Sync documentado.
2. **`Remove()` efetivo: `ValidateRemovalTargets(..., progress: null, kbIndex: null)` (P2, abort cooperativo).** O Preview encaminha `progress` e `kbIndex`; o Remove recebe `busy.Session` e não passa. O loop de `Delete` aborta; a revalidação pré-Delete não. Corrigir o `progress` é o abort cooperativo nessa fase. O `kbIndex` no Remove efetivo entra no item 4. Origem: painel pré-push 2026-09-01. Fechar quando: revalidação pré-Delete respeita Abort; smoke Remover documentado.
3. **Reentrância / `DoEvents` durante escrita (P1, risco aceito no corte).** `ExtensionBusyProgressScope.cs:39` — janela modeless + `Application.DoEvents()` sem desabilitar o owner nem guarda de operação única. Limitação já avisada ao usuário (hint Abortar; IDE pode congelar num `Save()`). Origem: painel Codex 2026-09-01. Fechar quando: desenho alternativo (modal bloqueante, guard de operação única ou owner desabilitado) + smoke Apply longo.
4. **Índice compartilhado incompleto (P2, performance).** Além do parágrafo genérico de `GetAll`: `ApiPlanApiObjectWriter`, `ApiPlanMetadataFileWriter`, writers BC/List, `Remove()` efetivo (`kbIndex: null` + `GetAll` por `Delete` e checagem pós-`Delete`). Detalhe por classe: `ApiPlanProcedureWriter.cs` (~170) ainda usa `Procedure.GetAll`; `ApiPlanListProcedureWriter.cs` (~53) cria índice próprio em vez do compartilhado; `ApiPlanBusinessComponentWriter` mantém chamadas `GetAll` apesar de receber `kbIndex`. Origem: painel Codex/Claude 2026-09-01. Fechar quando: writers acima recebem/reutilizam `kbIndex` e smoke de escala documentado.
5. **Higiene doc checkpoint (P3, opcional).** Itens 91+ na «Sequência operacional vigente» do checkpoint para trabalho 2026-09-01 (Preview Sync com casca; `RefreshFolders` após `GxOpenAPI`). Origem: painel Claude 2026-09-01. Fechar quando: sequência numerada reflete marcos pós-item 90 sem contradizer a próxima ação (`B108`).

Isso deixou de ser fila parada em 2026-09-02 e virou pauta de código até a aceite da Etapa 1A em 2026-09-03 (`Docs/Implementation/2026-09-03-B082-ETAPA-1A-ACEITE.md`); o residual 1B/2/3 permanece no plano `Docs/Implementation/2026-09-02-B082-PLANO-HARDENING-E-DESEMPENHO.md`, sem ser a próxima ação única.

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

Não é para “usar todos os SDTs”. É para **resolver por nome** (~30 nomes do plano): colisão, duplicata, SDT externo, tipos referenciados. O SDK expõe `GetAll` + filtro; o wizard já faz uma varredura em `ApiPlanGenerationStateReader`. O Apply **reutiliza** esse índice (`ReadForIntentionalChangeWithIndex`) em vez de repetir `GetAll` por SDT do plano. Depois de criar a pasta compartilhada `GxOpenAPI`, `RefreshFolders` espelha o `RefreshSdts`: o segundo `CreateOrReencounter` no mesmo índice (REST via BC) não tenta `CreateSharedFolder` de novo.

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

`Dock.Fill` ignora `Form.Padding`. O quadro de progresso usa um `Panel` com padding; Wizard e relatório B081 têm margem inferior na faixa de botões. O Preview do Remover e o Preview do Sync abrem a mesma casca de progresso **antes** da confirmação (índice da KB + validação), com `PreviewMs` no Output. No Apply/Sync o B081 ainda abre com a casca viva (pendência na seção «Fora da fila operacional»).

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
5. Relatório final B081/B063 mantém duração total; Output registra totais de fase (`Fase …=N ms`); ms por item ficam na casca.
6. Preview do Remover e Preview do Sync mostram casca de progresso antes da confirmação; Cancelar/Abortar/Fechar não colam no bordo da janela.

---

## 7. Referências

- Recado: `Docs/Implementation/2026-08-29-UX-PROGRESSO-WIZARD-APPLY.md`
- Escala (critério 11 encerrado): `Docs/Implementation/2026-08-29-CRITERIO11-ESCALA-EMPRESA.md`
- Checkpoint: `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md` (próxima ação de código = `B108`; Etapa 1A do plano de 2026-09-02 **aceita** em 2026-09-03 — `Docs/Implementation/2026-09-03-B082-ETAPA-1A-ACEITE.md`; este arquivo é registro, não pauta)
