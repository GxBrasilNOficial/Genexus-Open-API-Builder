# B108 — Plano: preferências só na criação + checkboxes alinhados à KB (com retração)

Data: 2026-08-31.
Estado: **planejado e aprovado em conversa; implementação adiada** (retomar em nova sessão).
Correlato de backlog: `Docs/Foundation/06-BACKLOG_v0.1.md` (`B108`).
Checkpoint: `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`. Este plano é a **ação seguinte**: desde 2026-09-02 a próxima ação única é a Etapa 1A do hardening `B082` (`Docs/Implementation/2026-09-02-B082-PLANO-HARDENING-E-DESEMPENHO.md`). Este plano permanece aprovado e gravado, sem alteração de escopo.

Não misturar com `B082` (sinal de vida no Wizard/Remover).

---

## 1. Motivo

Diagnóstico 2026-08-31 na Transaction `NotaFiscal` / `apiNotaFiscal`: após Apply com “Completar REST via Business Component” **desmarcado**, a reabertura do Wizard **religou** o checkbox porque a preferência da KB (`GxOpenApiBuilder_Settings`, B068) estava marcada.

Hoje:

- Serviços, Security, paginação e mensagens 422 **já** respeitam API/metadata no reencontro (`HasExistingApi` + `LoadSnapshot` / `ResolveExistingServiceSelection`).
- Checkboxes de geração (SDTs, Procedures, API Object, List, BC, metadata) ainda recebem seed do File da KB (`ApplyPreference`, `_applyBusinessComponentWhenReady` → `ResolveApplyBusinessComponentAfterGenerationRefresh`).
- Desmarcar BC no Apply significa apenas “não rode o writer nesta passagem”; o Source REST/BC **permanece**. O B054 ainda **bloqueia** rebaixar Service Source REST (`ThrowIfB054WouldDowngradeRestContract`).

Consequência: ler a API para decidir o checkbox de BC **não fecha** o ciclo enquanto desmarcar não retirar o REST/BC.

---

## 2. Decisão de produto (aprovada 2026-08-31)

### 2.1 Preferências da KB

- Defaults do File `GxOpenApiBuilder_Settings` aplicam-se **somente na criação** (`!ExistingApiContract.HasExistingApi`).
- No **reencontro/update**, o Wizard **não** seeda etapas de geração a partir desse File.

### 2.2 Espelho da KB nos checkboxes

No reencontro, cada checkbox de etapa espelha o que **existe / está aplicado** na KB (leitura já disponível via `ApiPlanGenerationStateReader` e contrato existente). Contrato de configuração (serviços, paths, segurança, paginação, required, filtros, hierarquia) continua vindo da API/metadata.

### 2.3 Apply alinha a KB à escolha

| Checkbox | Ligado no Apply | Desligado no Apply (se na abertura refletia existência/aplicação) |
|---|---|---|
| SDTs | criar / reencontrar | **apagar** SDTs próprios da API |
| Procedures | criar / reencontrar | **apagar** Procedures da API |
| API Object | criar / reencontrar | **apagar** o API Object |
| Metadata | gravar / reencontrar | **apagar** o File de metadata |
| Completar listagem (List) | aplicar / manter List real (B070) | **rebaixar** List → skeleton |
| Completar REST via BC | aplicar / manter REST/BC | **rebaixar** Get/Create/Update → skeleton; **Delete some** (serviço + Procedure + rota) |
| Delete | (acoplado a BC; já na API ou marcado na sessão) | some com BC; não existe sem BC |

Preservar sempre: SDTs compartilhados `GxOpenAPI`, propriedade Business Component da Transaction, Folder reutilizado (`wasCreated=false`).

### 2.4 Confirmação ao desmarcar

Ao **desmarcar** um checkbox que na abertura refletia “existe / aplicado”:

1. MessageBox descrevendo rebaixamento e/ou remoção que o Apply fará.
2. Sim / Não; **default Não**.
3. Não → checkbox volta a marcado.
4. Sim → permanece desmarcado; Apply executa a retração.

Desmarcar etapa que ainda não existe na KB (criação ou nunca aplicada) → sem diálogo de remoção.

Textos do marcador de BC (e afins) podem ser ajustados para refletir “usar / não usar” em vez de só “completar ao concluir”, desde que a semântica acima fique clara.

### 2.5 Cascata ao desmarcar

Espelho das dependências de geração (aviso único ou agregado, sempre default Não):

- SDTs ↓ → Procedures, API Object, List, BC, Metadata, Delete
- Procedures ↓ → API Object, List, BC, Metadata, Delete
- API Object ↓ → List, BC, Metadata, Delete
- BC ↓ → Delete
- List ou Metadata sozinhos → só a própria etapa

Marcar Delete com BC desligado → religa BC (comportamento já existente).

---

## 3. Ordem de implementação sugerida

1. **Seed**
   - `ApplyWizardPreferences`: preferências de geração **somente** sob `!HasExistingApi`.
   - No reencontro: popular checkboxes a partir do estado da KB (não do File).
   - `_applyBusinessComponentWhenReady` não nasce da preferência da KB no reencontro; Delete existente ainda pode exigir BC via `RequestApplyBusinessComponentForDelete`.

2. **UI de confirmação**
   - Handlers ao desmarcar + catálogo pt/es/en.
   - Cascata com um diálogo claro do conjunto afetado.

3. **Apply — retração**
   - Caminho explícito de rebaixamento BC/List (writers ou rotina dedicada).
   - Remoção seletiva reutilizando preflight/posse do `ApiPlanGeneratedApiRemover` (API → Procedures → SDTs próprios → metadata; Folder só se `wasCreated` e vazio de objetos próprios, como no Remover).
   - Quando a intenção for retração explícita, a trava `ThrowIfB054WouldDowngradeRestContract` **não** pode bloquear o rebaixamento autorizado (substituir por caminho consciente, não remover a proteção contra rebaixamento acidental via B054 “normal”).

4. **Preflight**
   - Antes do primeiro `Save()` / `Delete()` de retração: validar posse, ambiguidades e ordem; trio API/Procedure/SDT quando o contrato mudar.

5. **Relatório**
   - B081 lista removidos e rebaixados.

6. **Testes**
   - Mecânicos: seed só na criação; política BC sem pending da KB no reencontro; asserts de cascata/textos se couber off-line.
   - Build Release + checker de comandos (menu inalterado, salvo texto).
   - Smoke U15 (`NotaFiscal` / `apiNotaFiscal`): preferência BC ligada na KB não religa sozinha após Apply com BC desmarcado **e** retração aplicada; desmarcar BC pede confirmação e remove Delete/REST; desmarcar metadata apaga File; criação numa Transaction sem API ainda usa defaults da KB.

7. **Documentação no fechamento**
   - Backlog B108 → concluído; checkpoint; CHANGELOG; evidência de smoke neste arquivo ou satélite; varredura de frases que digam que desmarcar BC só “pula a etapa”.

---

## 4. Pontos de código atuais (partida)

- `Src/Extension/PrototypeWizardDialog.cs` — `ApplyWizardPreferences`, `ApplyPreference`, `_applyBusinessComponentWhenReady`, `ApplyBusinessComponentControlState`, acoplamento Delete/BC.
- `Src/Extension/Diagnostics/PrototypeWizardBusinessComponentNavigationPolicy.cs` — `ResolveApplyBusinessComponentAfterGenerationRefresh`.
- `Src/Extension/Diagnostics/ApiPlanGenerationStateReader.cs` — estado para seed no reencontro.
- `Src/Extension/Diagnostics/ApiPlanServiceSourceContract.cs` — `ThrowIfB054WouldDowngradeRestContract` (proteger acidental; liberar retração explícita).
- `Src/Extension/Diagnostics/ApiPlanGeneratedApiRemover.cs` — remoção seletiva / preflight de posse.
- `Src/Extension/Package.cs` — orquestração do Apply após `ShowDialog`.
- Preferências: `PrototypeWizardPreferences*.cs` / diálogo B068 (schema inalterado; só quando se aplica).

---

## 5. Fora de escopo deste B108

- `B082` (progresso / sinal de vida).
- Comandos Sync (`B085`) e Remover (`B086`) como produtos separados (a retração no Wizard pode reutilizar lógica interna do Remover).
- Mudança de schema do File `GxOpenApiBuilder_Settings`.
- Publicação de release (corte sob autorização humana à parte).

---

## 6. Critério de pronto

- Preferências da KB não alteram checkboxes de geração no reencontro.
- Reencontro espelha KB; Apply com desmarcação confirmada rebaixa ou remove de fato.
- Delete não permanece sem BC.
- Confirmação ao desmarcar com default Não.
- Criação continua usando defaults da KB.
- Evidência U15 + docs/backlog/checkpoint/CHANGELOG alinhados; pré-push da frente quando for a vez de publicar o commit.
