# B082 Etapas 2+3 — Sessão B, aceite IDE (retroativo)

Data da sessão: 2026-09-16.
KB: `wsEducacaoSpTeste` / Transaction `Teste` (GeneXus 18 U15; versão exata não registrada).
Frente/item: residual `B082`, Etapas 2 e 3, Sessão B (itens 142–149 do checkpoint).
Resultado: **aceite com achados** — oito cenários exercidos; um falhou e foi consertado e retestado na mesma sessão (caso 6); um achado ficou aberto sem correção e virou o `B125`.

**Redigido em 2026-09-21 sob a regra `B124`, a partir dos itens 142–149 de
`Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md` e do parágrafo «Progresso Sessão B» do plano `B082`; onde
o item não registrou dado, este doc marca «não registrado» em vez de inferir.**

## Proveniência e alcance

- **DLL não registrada.** O item 143 registra instalação da DLL em `GeneXus18\Packages` com
  `LastWriteTime` 12:46:03, mas não nomeia commit. A cronologia fecha a porta: o commit
  `687a494` (Fatia B + pendência 4) é das **22:43:13** do mesmo dia — posterior à instalação —,
  então não pode ser a origem da build instalada; o que a antecedeu foi o código de `78abfe1`
  (11:52) e o que ainda não estava commitado. Os itens 142 (a) e (b) antecedem esse install e
  também não registram a DLL. Proveniência declarada como **não registrada**, não inferida.
- **Alcance.** Este doc cobre **142–149** (Sessão B + Etapa 3). A Etapa **1B** (itens 150–151)
  tem doc próprio na entrada `B082 Etapa 1B (2026-09-17)` de `Validated` em
  `[0.1.0-alpha.8]` (`2026-09-17-B082-ETAPA-1B-ACEITE.md`) e
  fica fora deste recorte.
- **O que a DLL medida não inclui.** Nenhum dos oito cenários reexecutou o `B125` (Preview do
  segundo Remover listando alvos ausentes); o achado foi apenas observado e numerado. O reteste
  do sentido «Remover-em-curso → menu Recuperar» também **não** foi refeito pós-patch.

## Cenários

### 1 — Recuperar sob a guarda; diálogos unificados; diário terminal (item 142)

Preparação: diário já terminal na KB. Resultado observado, na ordem:

- diário terminal → diálogo «nada a recuperar»;
- Remover abortado (`UserAborted`, `JaRemovidos=2`, `Partial/RemovalPartial`, `FileId=88`);
- segundo Remover → `GateBlocked` + relatório, depois a oferta «Abrir a recuperação agora?»
  (ordem do código: `ShowFinalReport` então `OfferRecovery`); resposta **Não** à oferta;
- menu Recuperar → `ContinueRemovePass` → `Removed` (`Removidos=24`, mesma `OperationId`);
- durante a retomada, Output `Operação recusada: já há 'Recuperar operação interrompida' em andamento`.

Dois consertos nasceram do aceite do caso 1, aplicados no código da Sessão A: o comando
`Recuperar operação interrompida` furava a guarda de operação única (incluído no `TryEnter` do
menu; `OfferRecoveryAfterJournalBlock` → `RunRecovery` **sem** segundo `TryEnter`); e os avisos
simples da Recuperar deixaram `MessageBox` e passaram a `ExtensionRecoveryDialog`.

**Achado aberto (sem correção nesta sessão):** o Preview do segundo Remover ainda listava
`apiTeste`/`List` já ausentes. Remissão — 2026-09-17: esse sintoma virou o backlog **`B125`**
(reproduzir ou fechar; ainda sem revalidação).

### 2 — Guarda inversa (item 143, caso 1)

Remover completo (`PlannedDeletes=26`, `Removed`, `OperationId=c2a07391-…`, `FileId=88`); durante
a fila, Output `Operação recusada: já há 'Remover API gerada' em andamento`; após o fim, Recuperar
aceito com diálogo «nada a recuperar», etapa `Complete`, envelope `Removed/Removed`. PASS.

### 3 — Abort sem apagar o objeto do clique (item 143, caso 2)

Remover regenerado e abortado (`JaRemovidos=10` até `sdtTeste_API_Response`,
`Partial/RemovalPartial`, `OperationId=1630aa20-…`); nome na casca **não anotado** («não
registrado»). Conferência por MCP `genexus_inspect` na mesma KB: `apiTeste` / Procedures /
`ListResponse` / `Response` → `IndexedObjectUnavailable` (removidos); `sdtTeste_API_Response_TestePortfolio`
e `sdtTeste_API_UpdateRequest` presentes com GUID — o próximo da fila e os posteriores intactos.
Nota da sessão: `list_objects` ainda lista fantasmas no índice após `Delete`; o `inspect` é a
evidência. PASS.

### 4 — Folder fora do contêiner (D10) (item 144)

Folder `TesteOpenApi` movido para fora do contêiner esperado (`Root Module/CALCULOS/TesteOpenApi`,
Description canônica da extensão). Remover completo `OperationId=5f7418da-…`, `Estado=Removed`,
`Deleted=25` (lista sem `Folder:TesteOpenApi`), fila `Removidos=25, Preservados=1`, relatório
B081 `Removidos=25` / `Avisos=0` / «API removida com sucesso.». Folder vazio **permanece** sob
`CALCULOS` (KB Explorer + `genexus_inspect` path `Root Module/CALCULOS/TesteOpenApi`, Guid
`ff3571b6-…`). PASS.

### 5 — Preview divergente → zero exclusões (item 145)

Diálogo de confirmação do Remover é **modal** (não permite apagar no Explorer com ele aberto);
divergência provocada via MCP `genexus_delete_object` em `apiTeste` (Guid `08c1eef6-…`) com o
diálogo ainda aberto; ao Sim, `ResolveIntent`/`AssertPreviewStillMatches` bloqueou com
`Remoção bloqueada: a Knowledge Base divergiu do Preview confirmado (ApiObject 'apiTeste').
Nenhuma exclusão foi feita.`; Output `JaRemovidos=0`, relatório B081 `Removidos=0` /
`Bloqueados=1` / «Remocao interrompida.». Dump B109 da exceção esperado neste caminho. PASS.

### 6 — Casca × relatório (item 146)

Critério Etapa 3 (casca fechada antes do relatório em sucesso/abort/bloqueio) **fechado** neste
recorte: (a) **sucesso** — Wizard Apply → Relatório final; ao arrastar o relatório, só a IDE
embaixo (casca já fechada); (b) **abort** — Remover → Abortar → «Remoção abortada pelo usuário.» /
`Bloqueados` com Abortado [B082]; arrastar → nada embaixo; (c) **bloqueio** — segundo Remover com
diário parcial → «remoção interrompida»; arrastar → nada embaixo. PASS.

### 7 — Folder tipado não-vazio: falha, conserto, reteste (itens 147–148)

**Primeira execução — falhou o critério tipado.** Folder recriado no Root Module pelo Wizard;
WebPanel colocado dentro de `TesteOpenApi`; Remover `OperationId=27673109-…`, `Estado=Partial`,
`BlockReason=RetryBudgetExhausted`, `Passadas=26/26`, `Removidos=25`, `Preservados=0`,
`Pendentes=1`, `Bloqueado='Folder:TesteOpenApi'`. Relatório B081 `Removidos=25` / `Bloqueados=1`
/ `Avisos=1` com aviso de interrupção por orçamento — **sem** o aviso tipado `Folder
'TesteOpenApi' nao foi apagado porque nao ficou vazio.` e **sem** string mágica
`Folder:…:PreservedNonEmpty` em Removidos. Folder e WebPanel permaneceram na KB (efeito físico
ok; classificação errada). Causa no código: `IsFolderEmpty` só varria API/Procedure/SDT/File/
Folder — **não contava WebPanel**; a fila tentava `Delete` 26 vezes (`StillPresent`) e esgotava o
orçamento. **Conserto offline na mesma data:** `IsFolderEmpty` passou a `Folder.HasObjects` +
`SubFolders` (gate `tests.b082Etapa2Safety`). Recuperação do envelope: `Complete` → `Removed`
(`OperationId` mesmo), Folder+WebPanel intactos.

**Reteste pós-install (mesma data):** Wizard com Folder já habitado → metadata
`FolderWasCreated=False` / Preview «reutilizado; nunca apagar»; Remover `OperationId=542a5df7-…`,
`Estado=Removed`, `PlannedDeletes=25`, `Avisos=0`, `Success` — Folder fora da fila (caminho
reuso), **não** exercita `PreservedNonEmpty`. FAIL → conserto → reteste do caso.

**Caso 6 fechado (reteste correto pós-conserto `HasObjects`):** Folder apagado e recriado pelo
Wizard (`FolderWasCreated=True`); WebPanel movido **depois** do Apply; Remover
`OperationId=873872c5-…`, Preview «criado pela extensão; apagar só se ficar vazio»,
`PlannedDeletes=26`, `Estado=Removed`, `Passadas=1/26`, `Removidos=25`, `Preservados=1`,
`Bloqueados=0`; B081 `SuccessWithWarnings`, `Avisos=1`: `Folder 'TesteOpenApi' nao foi apagado
porque nao ficou vazio.` Scan `Folder/folder-vazio` 1×. Sem `RetryBudgetExhausted` e sem string
mágica em Removidos. PASS.

### 8 — DEMO §21 conferido (item 149)

`Docs/Public/DEMO.md` §21 já descreve casca **modeless**, Abortar cooperativo com KB parcial e
`Recuperar operação interrompida` — alinhado ao exercido nos itens 142–148 (abort Remove,
casca×relatório, Recuperar). Sem novo clique IDE. PASS (conferência, não execução).

## Aberto

- **`B125`** — Preview do segundo Remover listando `apiTeste`/`List` já ausentes (observado no
  cenário 1, item 142). Sem correção na sessão; sem revalidação. Reprodução/conserto pertencem ao
  próprio item (documento 06); este doc o referencia como achado da sessão, **não** o revalida.
- Sentido «Remover-em-curso → menu Recuperar» **não** refeito pós-patch (item 142).
- Etapa **1B** do `B082` ficou adiada nesta sessão (fechada depois, 2026-09-17, em doc próprio).

## Rastreabilidade

- Plano: `Docs/Implementation/2026-09-02-B082-PLANO-HARDENING-E-DESEMPENHO.md` («Retomada em duas
  sessões», «Progresso Sessão B»).
- Itens do checkpoint: 142–149 de `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`.
- Entrada que registrou a sessão no `CHANGELOG`: `B082 retomada Etapas 2+3 (2026-09-16)` em
  `Validated` da `0.1.0-alpha.8` (recorte **142–151**; este doc cobre **142–149**). **Citada,
  nunca editada** — seção de versão
  publicada é imutável.
- Regra que determinou este doc: `B124` —
  `Docs/Implementation/2026-09-21-B124-PLANO-REGRA-DOCUMENTO-EVIDENCIA-IDE.md` (§6).
