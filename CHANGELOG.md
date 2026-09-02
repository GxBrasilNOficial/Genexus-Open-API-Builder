# CHANGELOG.md

# Changelog

Todas as mudanças relevantes deste projeto serão registradas neste arquivo.

O formato segue princípios de changelog legível e versionamento progressivo.

---

## [Unreleased]

### Planned

- `B108` (pendência, plano aprovado 2026-08-31, código adiado): preferências só na criação; reencontro espelha KB; desmarcar confirma e rebaixa/remove no Apply (Delete some com BC). Plano: `Docs/Implementation/2026-08-31-B108-PLANO-PREFERENCIAS-E-RETRACAO.md`. Checkpoint e documento 06.

---

# [0.1.0-alpha.7] - 2026-09-01

Release focada em feedback visual de progresso, cancelamento cooperativo e indexação de objetos da KB (`B082`). Publicado em 2026-09-01 (tag `v0.1.0-alpha.7` + GitHub Release pre-release, dois assets DLL).

## Added

- `B082` (Fases A+B, 2026-08-31): quadro de progresso na abertura do Wizard, Apply, Sync e Remover (Preview antes do Sim); Abortar cooperativo; índice da KB; tempos `[B082] Fase` na Output; aviso de escala no Resumo. Smoke `Tributacao` e, no mesmo dia, `Empresa` (`Gx_FabricaBrasil`): abertura `6401` ms (`InterfaceMs=4770`); Apply `SuccessWithWarnings`, Criados=51, Atualizados=3, Bloqueados=0, `DuraçãoMs=249062` (~4,2 min; BC `78891` ms, List `95867` ms). Registro: `Docs/Implementation/2026-08-31-B082-PLANO-UX-PROGRESSO.md`. ~~Fora da próxima ação (`B108`).~~ Superado em 2026-09-02: o residual foi medido e virou a próxima ação única.
- Casca B082: etapas internas (`Preferências`, `Indexando objetos`, `Removendo`, `Pré-verificação`) passam a seguir o idioma da IDE; writers e Output `[B082]` inalterados.
- Casca B082 no Preview do Sync (2026-09-01): quadro **antes** do diálogo/relatório, índice da KB, `Sync PreviewMs` na Output. Smoke `Empresa`: `PreviewMs=5089`, diff vazio, KB intacta. Remover Preview na mesma Transaction: `PreviewMs=2525`, cancelado com Não.

## Changed

- Índice da KB: `RefreshFolders` após criar `GxOpenAPI`, para o segundo `CreateOrReencounter` no mesmo Apply (REST via BC) não tentar `CreateSharedFolder` de novo. Smoke `ShowcaseUnanimo` / `Company` (2026-09-01): primeiro Apply, Criados=16 (inclui Folder `GxOpenAPI`), Bloqueados=0.
- Registro B082: medição por item na casca (`ElapsedMs`); Output fica com totais de fase, sem dump de ms por objeto.
- Registro B082 (2026-09-01): polimento de outra sessão anotado — casca × B081 no Apply/Sync; `progress` na revalidação do `Remove()` efetivo — junto com o `GetAll` residual já previsto. ~~Não é a próxima ação (`B108`).~~ Superado em 2026-09-02.
- Registro B082 (2026-09-01): inventário «Fora da fila operacional» ampliado (itens 3–5) — `DoEvents`/reentrância, writers Procedure/List no residual de índice, higiene doc da sequência operacional.
- Instrumentação B082 (2026-09-02): `ApiPlanScanTelemetry` e `ApiPlanScanProbe` contam e cronometram as varreduras de catálogo do Apply, do Sync e do Remover, publicando no Output em `[B082]`. Só observam: sem escopo ativo o delegate executa igual ao código não instrumentado, e a apresentação do relatório final suspende a medição para não se atribuir à operação que terminou.
- Medição B082 (2026-09-02) na KB `Fabrica Brasil Test`, três transações: `Attribute.GetAll` custa ~1300 ms e é a varredura mais cara, embora a extensão nunca crie, altere ou apague atributos; o mapa de atributos já existe em `ApiPlanKbObjectNameIndex` e não tem consumidor; o índice é criado quatro vezes por Apply; o custo de varredura por Apply é praticamente constante em torno de 60 s, porque depende das partes da chave primária e dos filtros, não do tamanho da transação. Apply de `Setor` (12 objetos) leva 84 s; de `DocumentoFiscal` (171 campos), 187 s — com as mesmas 97 varreduras.
- Promoção de frente (2026-09-02): o residual `B082` foi desestacionado e é a próxima ação única, pela Etapa 1A de `Docs/Implementation/2026-09-02-B082-PLANO-HARDENING-E-DESEMPENHO.md`; `B108` recua para a ação seguinte, com plano intacto.
- Imagens promocionais do corte `0.1.0-alpha.7` versionadas em `Docs/Images/` (`release-0.1.0-alpha.7-promo.jpg` e `release-0.1.0-alpha.7-promo-es.jpg`). Entraram junto do commit `7008a90`, cuja mensagem trata de outro assunto; ficam registradas aqui para não constarem apenas de forma implícita no histórico.
- Teste do instrumento (2026-09-02): `Tests/ScanProbe/Test-ApiPlanScanProbe.ps1`, registrado como `tests.scanProbe` no orquestrador e no seu teste vinculante. Cobre callback único sob `Dispose` repetido, escopos aninhados, `Suspend`/restauração e tolerância a exceção no callback; validado por mutação. `ApiPlanScanProbe` e `ApiPlanScanTelemetry` passam a ser públicas, seguindo a convenção já usada pelas demais classes testadas por `Add-Type`.

## Validated

- Smoke U15 na Transaction `Empresa` com 13 subníveis (`Gx_FabricaBrasil`): Apply com sinal de vida contínuo (~4,2 min), Preview do Sync (~5 s) e Preview do Remover (~2,5 s).
- Smoke U15 no primeiro Apply com criação de pasta compartilhada `GxOpenAPI` na `Company` (`ShowcaseUnanimo`): 16 objetos criados sem colisão.
- Cancelamentos validados antes da escrita sem efeitos colaterais na KB.
- Linhas U14+ (canônica) e U13 (satélite) compiladas em Release para este corte.

## Assets

- `GenexusOpenApiBuilder.Extension.dll` — GeneXus 18 U14, U15 e posteriores U14+. SHA-256 `E7E462E4D47BE9140D7E1A70484266B9868081CB4B9D6BC6B415966F99CE2D8D`.
- `GenexusOpenApiBuilder.Extension-gx18u13.dll` — GeneXus 18 U13. SHA-256 `72B476F7C089005EDFFD8418634FA26D6B67FD03ED0BC41B267603BF0D794088`.

---

# [0.1.0-alpha.6] - 2026-08-31

Release focada no serviço REST `Delete` opt-in (`B100`). Publicado em 2026-08-31 (tag `v0.1.0-alpha.6` + GitHub Release pre-release, dois assets DLL).

## Added

- `B100`: serviço REST `Delete` opt-in (desligado por padrão). Procedure `proc*_API_Delete`, `DELETE` no path da chave do Get, `200` / `404` / `422` via BC, `SecurityLevel` próprio, confirmação ao marcar. HTTP: 401/404/200 nos dois environments da `apiNotaFiscal`; 422 de integridade no Framework (PostgreSQL dispensado em 2026-08-30). Evidência: `Docs/Implementation/2026-08-30-B100-DELETE-OPT-IN.md`.

## Changed

- Documentação pública (README ×3, `DEMO`) e contratos 15/27: `Delete` deixa de ser “inexistente no MVP” e passa a opt-in.
- README ×3 (“O que gera”) e `DEMO` passam a citar `Delete` opt-in; o teste de reencontro de serviços do Wizard trava a forma atual de `ResolveExistingServiceSelection`.
- README ×3 (contrato de erro HTTP): o 422 de `Create`/`Update` (rules do BC) passa a citar também o `Delete` opt-in na recusa por integridade referencial, alinhado ao documento 27.
- Wizard, preferências, Output B079 e relatório: a etapa BC passa a se chamar REST via Business Component e cita Delete quando marcado; o Apply lista `proc*_API_Delete`.
- B054 recusa regravar skeleton sobre API Object que já tem contrato REST (ex.: marcar Delete só na etapa de API Object, sem BC).
- Delete marcado exige Completar REST via Business Component no mesmo Apply (UI + `ThrowIfDeleteWithoutBusinessComponent` antes do primeiro `Save()`). Sem a etapa BC não há skeleton `proc*_API_Delete` nem rota B054. Validado no U15 em 2026-08-31 (`apiNotaFiscal`: diálogo ao desmarcar BC; remarcar Delete religa BC; Apply `Updated=15`, `Blocked=0`; Service Source do Delete com `ErrorResponse` / `RestStatusCode`).
- FAQ `22`: o `Delete` deixa de ser descrito como pós-MVP absoluto na resposta principal.
- Reencontro do `Delete`: o leitor de Service Source e de metadata passa a ver o serviço e o `securityLevel` por item; o matcher B079 reconhece a assinatura atual (`PK` + `ErrorResponse` + `RestStatusCode`); o Apply usa o combo do Delete em vez de um `SecurityLevel` vazio herdado do contrato existente.
- Sync (`B085`): reconstrói o `SecurityLevel` próprio do Delete a partir da metadata (`services[].securityLevel`) e monta o ApiPlan com o contrato da KB ativa, para não regravar o Delete com o nível global da API. Validado no U15 em 2026-08-31 (`apiNotaFiscal`: API `Authorization`, Delete `Authentication` após Apply do Sync com delta `NotaFiscalObs` 40→41). Evidência: `Docs/Implementation/B085-SINCRONIZAR-COM-TRANSACTION.md`.
- Reencontro: List/Get/Create/Update passam a herdar o Security Level do rádio do Wizard; o nível persistido por serviço só permanece no Delete (combo). Path/`operationId` da API existente continuam preservados. Validado no U15 em 2026-08-31 (`apiNotaFiscal`: abertura Authorization + Delete Authentication; rádio mudado para Authentication; Apply `Updated=15`, `Blocked=0`; Service Source dos cinco serviços com `[SecurityLevel(Authentication)]`).
- Mensagens de preflight dos writers: deixam de pedir `Execute B040-B046` / `B050-B053` / `B054` e passam a indicar o Wizard.
- Documentos Foundation `05`, `10`, `11` e `12` §11: deixam de afirmar Delete como pós-MVP absoluto e passam ao contrato vigente (quatro serviços obrigatórios + Delete opt-in desligado por padrão).
- Foundation `04`, `06` (nota operacional e `B075`) e `08` (regra MVP de `EndpointsCount`): a frase vigente deixa de ser “Delete é pós-MVP” / “não existe Delete no MVP”, para o mesmo contrato opt-in do `B100`. O plano 24 anota o `B075` da Sprint 6 como critério histórico.
- Foundation `07` (UX do wizard): Passo 2 passa a citar Delete opt-in, confirmação, combo de Security Level e dependência da etapa Completar REST via Business Component, sem alterar a estrutura de 3 passos.
- HTTP do Delete recapturado em 2026-08-31 na `apiNotaFiscal` (401/404/200 nos dois environments; 422 de integridade no Framework). Residual do nível próprio no IIS fechado no mesmo dia: Build All com Delete `SecurityHigh` e demais `SecurityLow`; `goab_api_teste` 201/200/404; `goab_role_denied` GET 200 e DELETE 403 `code` 139 nos dois. Evidência: `Docs/Implementation/2026-08-30-B100-DELETE-OPT-IN.md` §3.

## Fixed

- Output da etapa Procedures com Delete: o prefixo da etapa passa a `B050-B053/B100` (o item já saía `Backlog='B100'`). Smoke U15 2026-08-31 na `apiNotaFiscal`: prefixo no bloco e nos cinco itens; Delete com `Backlog='B100'`.
- Prévia do Wizard: o fingerprint e o refresh passam a incluir o Security Level do Delete e o checkbox de mensagens HTTP 422. Smoke U15: rádio Authentication, combo Delete Authorization; Apply `Updated=15`; Service Source dos quatro em `[SecurityLevel(Authentication)]` e Delete em `[SecurityLevel(Authorization)]`. Ir direto ao Resumo força refresh (`forceRefresh: true`); o cache só afeta as abas de geração.
- Preferências do Wizard: recusam gravar Delete sem Get, Create e Update (sem marcar serviços em silêncio). Smoke U15: só Delete dispara a mensagem nova; cancelar não grava. O codec (`Parse`/`Serialize`) recusa o mesmo estado; o `Load` cai em defaults conservadores. Load de File inválido não foi fumado na IDE (File é blob, sem edição de JSON na tela); cobre o teste offline.

## Removed

- Handlers incrementais órfãos B040/B050/B054 em `Package.cs` (`ExecuteCreate*` / `TryConfirmAndCreate*` e os MessageBox de «4 Procedures»). Não estavam no menu nem no manifesto; Wizard e Sync continuam nos `TryCreate*`.

## Validated

- HTTP do Delete nos environments `.NET`/PostgreSQL e `.NET Framework`/SQL Server (`apiNotaFiscal`), inclusive nível próprio Authorization vs Authentication no IIS.
- Smoke U15 de reencontro, Sync, dependência BC, Output, prévia e preferências.
- Linhas U14+ (canônica) e U13 (satélite) compiladas em Release para este corte.

## Assets

- `GenexusOpenApiBuilder.Extension.dll` — GeneXus 18 U14, U15 e posteriores U14+. SHA-256 `BE15C49E8F5909C2000910D0B6FD54A16A8AE567AE463C46842C9B933C5035BD`.
- `GenexusOpenApiBuilder.Extension-gx18u13.dll` — GeneXus 18 U13. SHA-256 `58600B4F7E6E4123BFBD22B913E11B81A0C2365A7240193DB7D450A0F7838187`.

---

# [0.1.0-alpha.5] - 2026-08-30

Release focada no suporte a Transactions com subníveis (Sprint 9, Fases 0–7).

## Added

- Critério 11 (Sprint 9): escala na Transaction `Empresa` (13 subníveis) na cópia `Gx_FabricaBrasil` — apply `SuccessWithWarnings` (44 SDTs próprios; skip do Create vazio de `ExclusivoEmVenda`), `Build All` Success nos dois environments, critério 8, Remover `Deleted=50` sem órfão. Alertas de tempo registrados. Evidência: `Docs/Implementation/2026-08-29-CRITERIO11-ESCALA-EMPRESA.md`.
- Fase 7 (Sprint 9): ciclo de vida sob hierarquia — releitura de `levels` no reencontro do Wizard (`PersistedHierarchicalRoot` + `ApplyPersistedPrune`), tolerância de preferências legadas sem `schemaVersion`, inventário dinâmico de SDTs próprios na remoção (`ApiPlanGeneratedApiRemovalInventory`), Sync sem falso positivo de conflito SDT em metadata hierárquica; gates `tests.wizardLifecycle` e testes de remoção/preferências atualizados. Smoke U15 na `Teste`/`apiTeste` (apply, critério 8, Sync zero-diff, Remover preview cancelado). Evidência: `Docs/Implementation/2026-08-28-FASE7-CICLO-VIDA-HIERARQUIA.md`.
- `B099b` (Sprint 9 / Fase 6): metadata hierárquica `GOAB_API_METADATA_B060_V2` com `levels` e `objects.sdts.own`; leitura tolerante V1+V2; `PlannedContractHash` B067 cobre a árvore; Sync hierárquico por GUID (`ApplyPersistedPrune`); Remover usa inventário `own`; aviso V1 de Remover/Sync removido; gate `tests.metadataHierarchical`. Smoke U15: `Teste`/`apiTeste` (Wizard, Sync, Remover); `NotaFiscal`/`apiFiscalPublica` reencontro plano V1→V2 (`levels=null`, hash flat estável). Residual Sync flat vs hierárquico endereçado na Fase 7. Evidência: `Docs/Implementation/2026-08-28-B099b-METADATA-HIERARQUICA-V2.md`.
- `B099v` (Sprint 9 / Fase 5-A): validação em runtime do que as Fases 2 a 5 emitiram — correção de `ResolveAggregateAttributeName` (PK própria em `count()`), ouro e gate `tests.listHierarchical` atualizados; reapply do Wizard na `Teste` de quatro níveis; smoke HTTP multinível (POST/GET/PUT com e sem Replace, List com contadores) nos dois environments; critério 9 (YAML hierárquico conferido + clientes `typescript-fetch`/`csharp` com `openapi-generator-cli 5.3.1`). Metadata permanece V1 até `B099b`. Evidência: `Docs/Implementation/2026-08-28-B099v-VALIDACAO-RUNTIME-MULTINIVEL.md`.
- `B099a` (Sprint 9 / Fase 5): Wizard hierárquico com seletor compartilhado (caminho `Shift / Worker`), dependência pai/filho, contador de List desligável nos filhos diretos, aviso de profundidade > 4 e `ApiPlan.Levels` podado para preview e apply; required de linha só na UI; aviso de ciclo de vida V1 (não Remover/Sync até `B099b`); gate `tests.wizardHierarchical`. Caminho plano e linha de base Fase 0 intactos. Metadata V2 fora deste recorte. Smoke U15 2026-08-26 na `Teste`: 3 e 4 níveis com `Build All` nos dois environments, sem `spc0018`. Evidência: `Docs/Implementation/2026-08-26-B099a-WIZARD-HIERARQUICO.md`.
- `B098` (Sprint 9 / Fase 4): `ListResponse_Item` quando há subníveis; `ListResponse.Items` tipa esse SDT; contadores `<Subnível>Count` via `count()` nos filhos diretos com `IncludeListCount` (default ligado); contrato compartilhado `ApiPlanListHierarchicalContract`; ouro em `Tests/ListHierarchical/Baselines/` e gate `tests.listHierarchical`; trava OpenAPI passa a exigir `_API_ListResponse_Item`. Caminho plano e linha de base Fase 0 intactos. Wizard flat e metadata V2 fora deste recorte. Evidência: `Docs/Implementation/2026-08-26-B098-LIST-CONTADORES.md`.
- `B097` (Sprint 9 / Fase 3): Source Business Component hierárquico para `Get`/`Create`/`Update` quando `ApiPlan.Levels` tem filhos; mapa alinhado ao naming B096; marcador `<Subnível>Replace` com match-by-PK (ou `Clear`+Add se PK autonumerada); ouro em `Tests/BusinessComponentHierarchical/Baselines/` e gate `tests.businessComponentHierarchical`. Wizard flat, `List`/`ListResponse_Item` e metadata V2 fora deste recorte. Evidência: `Docs/Implementation/2026-08-26-B097-BC-HIERARQUICO.md`.
- `B096` (Sprint 9 / Fase 2): plano de SDTs hierárquicos por contrato quando `ApiPlan.Levels` tem filhos; naming `sdt<Tx>_API_<Papel>_<caminho>` com desambiguação por `LevelOrder` e encurtamento de objeto a 128 quando o nome completo estoura 128 ou colide (folha ≤32 escolhe entre reusar a folha e o hash; membros sem teto nesta fase); marcador `<Subnível>Replace` no Update; pós-ordem em `OwnSdts`; até B098, `ListResponse.Items` permanecia coleção de `Response` no ouro B096 — o ouro foi atualizado com `ListResponse_Item` nesta frente; gate `tests.sdtHierarchicalPlan`. A trava `Test-OpenApiClientContractValidity.ps1` lê `ApiPlanSdtHierarchicalNaming.cs` para `_API_CreateRequest_`, `_API_UpdateRequest_`, `_API_Response_` e, desde B098, `_API_ListResponse_Item`. Wizard flat fora deste recorte. Evidência: `Docs/Implementation/2026-08-26-B096-SDTS-HIERARQUICOS.md`.
- `B095` (Sprint 9 / Fase 1): leitura hierárquica com núcleo recursivo `Build`/`ReadLevel` sobre fonte neutra, adaptador SDK fino (`Read(Transaction)`), modelo `ApiPlanLevel` / `ApiPlanLevelField`, `ApiPlan.Levels` (consumido pelo plano de SDT a partir de B096; Wizard flat ainda não), helper `TransactionAttributeKeyTraits` compartilhado com o Wizard, fixtures + ouro em `Tests/TransactionStructure/Baselines/` e gate `tests.transactionStructure`. Evidência: `Docs/Implementation/2026-08-25-B095-LEITURA-HIERARQUICA.md`.
- Fase 0 (Sprint 9): linha de base offline de transações planas em `Tests/GenerationBaseline/`, ligada ao checker pré-push como `tests.generationBaseline` (Source de Create/Update/Get/List, Service Source do API Object e plano de SDT, gerador pós-B102). Captura IDE de início registrada em `Tests/GenerationBaseline/IdeXpz/CAPTURE-INICIO.md` (Transaction plana `Teste` + SDTs compartilhados já presentes na KB). Conferência XPZ de fim fechada em 2026-08-28 (`Tests/GenerationBaseline/IdeXpz/CAPTURE-FIM.md`). Evidência: `Docs/Implementation/2026-08-25-FASE0-LINHA-DE-BASE-NAO-REGRESSAO.md`.

### Changed

- Documentação pública (README ×3, `INSTALL`, `DEMO`) substitui a limitação de “somente o primeiro nível” pela descrição do suporte hierárquico e aponta as notas `0.1.0-alpha.5`.
- Sprint 9: inserida a **Fase 5-A (`B099v`)** entre as Fases 5 e 6, sem renumerar as seguintes — validação em runtime do que as Fases 2 a 5 emitiram: correção da agregação `count()` com PK composta herdada, smoke HTTP multinível nos dois environments e o critério 9 (contrato OpenAPI publicado). Motivo da ordem: a Fase 6 grava metadata V2 sobre o contrato hierárquico, e defeito de Source BC ou de `List` descoberto depois custa migração de integridade. Decidido em 2026-08-27, na revisão semântica da rotina pré-push, que expôs dois gatilhos vencidos: o critério 9 devido ao fim da Fase 4 (nunca executado na parte manual) e a dívida do `count()` ancorada em "antes do smoke IDE multinível", marco já ocorrido em 2026-08-26. Os critérios 6 (`Gx_FabricaBrasil`) e 10 (smoke `Gx18u13`) permanecem gates da sprint, fora desta fase. A fase começa por reinstalar a DLL e reaplicar o Wizard: a `apiTeste` de quatro níveis hoje na KB é de 2026-08-26 e precede `8f80f39`, que mudou poda por papel, mapa BC e desambiguação de `VariableToken` — medir sobre ela seria medir o gerador antigo. Ressalva de datação registrada também na evidência do B099a. O backlog `06` desdobra a linha `B099` em `B099a` (concluído), `B099v` e `B099b`.

### Fixed

- Plano de SDT hierárquico: filho cujo Create fica só com PK herdada (0 membros) deixa de emitir SDT aninhado vazio — o GeneXus recusa SDT sem itens. Mapa BC alinhado. Preflight recusa SDT vazio **exceto** `ListFilters` sem filtros (contrato vigente; ouro `HeaderOnly`). Fixture `ExclusiveCreateEmpty`. Evidência: `Docs/Implementation/2026-08-29-CRITERIO11-ESCALA-EMPRESA.md`.
- Preview B086 (`ExtensionConfirmDialog`): lista de dezenas de SDTs passa a caber na área útil (teto `WorkingArea - 32`, rolagem, uma linha por objeto sem wrap); Sim/Não e a pergunta ficam fora da rolagem. Default continua Não. Teste `tests.generatedApiRemovalPlan`. Evidência: `Docs/Implementation/B086-REMOVER-API-GERADA.md`.
- Evidência B099a: nota de época em `Fields` (união estrutural na entrega 2026-08-26 → catálogo completo desde 2026-08-28 / FASE7).
- `Tools/Copy-ExtensionForGeneXus18.ps1`: `NextCommand` / `FollowingCommand` escolhem `Register-ExtensionForGx18u13.bat` (e o Install satélite no dry-run) quando a `BuildDll` está sob `gx18u13` ou o diretório é `GeneXus18up13`; linha canônica permanece no Register U14+.
- Documentação da Fase 0: CHANGELOG, plano `24`, especificação de subníveis e evidência `2026-08-25-FASE0-…` alinhados à conferência XPZ de fim já fechada em `CAPTURE-FIM.md` (2026-08-28).
- Sync hierárquico: poda de subnível grava catálogo completo em `Fields` e deixa omissão só em `Selected*` — campo desmarcado no Wizard deixa de aparecer como falso `Added` no Sync. Metadata antiga (união em `fields`) só melhora após regravação de `levels`. Teste `tests.wizardHierarchical` atualizado. Smoke U15: `TesteItemObs2` omitido → Sync `Adicionados=0`. Evidência: `Docs/Implementation/2026-08-28-FASE7-CICLO-VIDA-HIERARQUIA.md`.
- Wizard: seletor de nível (Requests/Response/Obrigatórios) passa a calcular `DropDownHeight` pelos itens ou ~55% da altura do diálogo — lista longa (ex. `Empresa` com 13 subníveis) deixa de exigir rolagem em dropdown minúsculo.
- `Register-ExtensionForGx18u13.bat`: wrapper dedicado ao registro da linha satélite (default `GeneXus18up13`); `Install-ExtensionForGx18u13.bat` e `AGENTS.md` apontam para ele.
- Critério 10 (Sprint 9): smoke `Gx18u13` multinível na `Teste`/`apiTeste` — Wizard reencontro `Updated=27`, `Build All` nos dois environments sem `spc0018`; DLL satélite conferida (`GxLine=Gx18u13`, `N=143920`). HTTP fora do gate (U15). Evidência: `Docs/Implementation/2026-08-29-CRITERIO10-SMOKE-GX18U13.md`.
- Dívidas B097 (2026-08-28): Update hierárquico deixa de atribuir PK autonumerada ao BC (`ShouldAssignFieldToBc`), alinhando ao Create; mapa BC recusa subnível sem nome estrutural na Transaction; fixture `InheritedPrimaryKey` usa nível `Line`; fallback `<unnamed>` permanece só no leitor B095 (`UnnamedSublevel` reader-only). Ouros e gates `tests.businessComponentHierarchical`, `tests.transactionStructure`, `tests.sdtHierarchicalPlan` e `tests.listHierarchical` atualizados.
- `ResolveAggregateAttributeName` (`ApiPlanListHierarchicalContract.cs`): preferir a primeira PK com `!IsForeignKey` em vez de `PrimaryKey[0]`, que em PK composta herdada devolvia o atributo do cabeçalho e gerava `count(HeaderId)` incorreto; ouro `InheritedPrimaryKey.txt` atualizado para `count(LineId)` / `LineCount=LineId`; asserção no teste `tests.listHierarchical` rejeita `count(HeaderId)` na fixture. Runtime confirmado em `procTeste_API_List` após reapply B099v. Residual consciente: PK de subnível em que todas as partes são FK ainda pode cair no fallback `PrimaryKey[0]` (cabeçalho) — registrado em `Docs/Implementation/2026-08-26-B098-LIST-CONTADORES.md`; não bloqueia `B099b`.
- Documentação alinhada na pré-push reforçada (2026-08-28): inventário B095 atualizado (`InheritedPrimaryKey` → nível `Line`; `UnnamedSublevel` cobre `<unnamed>`); evidência B099v corrige `Level1Count` → `LineCount` no ouro citado.
- Higiene B099a: removido o helper morto `SelectLevelFields` (filtro só técnico, sem chamadores). O único caminho vigente continua `SelectLevelFieldsForRole` (seleção do Wizard por papel + elegibilidade). Contrato, ouro e manifesto inalterados.
- Wizard hierárquico: a poda passa a gravar seleção Create/Update/Response por subnível (`Selected*FieldNames` em `ApiPlanLevel`); plano de SDT e mapa BC respeitam o papel — desmarcar só em Create deixa de publicar o campo no CreateRequest. Falha na leitura hierárquica deixa de cair no flat em silêncio (Output + MessageBox). `VariableToken` reserva nomes e desambigua colisão; dedupe de variáveis BC falha se o mesmo nome tiver tipos distintos. README×3, DEMO, `08` e `24` alinhados (subníveis não são mais “ignorados”; profundidade validada = 4). Em 2026-08-27: §8/§8-A da especificação de subníveis e remissão na emenda de profundidade do registro de decisões alinhados ao aviso acima de 4; testes offline cobrem combinações de papel na poda e colisão/`AllocateVariableToken`. Fixture `VariableTokenCollision` fecha o caminho end-to-end (duas rotas longas → `L1_SameLeaf` / `L1_SameLeaf_V2` no mapa e no Source BC).
- Relatório final B081: `ResolveFinalReportOwner` ignora `Form.ActiveForm` oculto ou disposed (Wizard já fechado ainda no `using`) e cai no handle da IDE. O primeiro apply de três níveis na `Teste` gravou `[B081]` na Output sem mostrar o diálogo; a segunda chamada mostrou. Sem `OpenForms`. Manifesto inalterado.
- Aviso de profundidade do Wizard: `ValidatedDepth=4` após o smoke U15 de quatro níveis na `Teste`; o texto trilíngue cita 4 níveis. Profundidade 5 continua avisando, sem bloquear. Manifesto inalterado.
- Tipo BC de nível aninhado: a variável `&Bc_<caminho>` passou a usar o tipo GeneXus com ancestrais (`Transaction.Pai.Neto`, não `Transaction.Neto`). O apply de três níveis na `Teste` (`apiTeste`) era bloqueado em B055 com `'Teste.TesteItemFolio'`. Filho direto permanece `Transaction.Nivel`. Teste `ThreeDeep` cobre o caminho. Manifesto inalterado.
- Documentação B098: residual `count(HeaderId)` na fixture `InheritedPrimaryKey` / PK composta herdada registrado no checkpoint e na evidência — a correção de `ResolveAggregateAttributeName` passa a ser o primeiro item da Fase 5-A (`B099v`); o gatilho anterior ("antes do smoke IDE multinível") venceu em 2026-08-26 sem ser cumprido, e o ouro atual congela o emissor. O mesmo gatilho vencido foi corrigido nos dois pontos que ainda o repetiam: a evidência `2026-08-26-B098-LIST-CONTADORES.md` (remissão datada, texto original preservado) e o comentário XML de `ResolveAggregateAttributeName` em `ApiPlanListHierarchicalContract.cs`, que agora aponta a Fase 5-A e o smoke HTTP.
- Documentação B096 (naming): gatilho do encurtamento é o nome completo do SDT estourar 128 ou colidir; folha ≤32 só escolhe entre reusar a folha e o hash de 8 hex; o nome da Transaction pode ser truncado; 128 não se aplica a membro (ouro `LongQualifier`: coleção 106, `Replace` 113).
- Checker e `AGENTS.md`: `currentFront`/`manualRequired` documentados como exclusivos de spike `B000`–`B006`; lista vazia com próxima ação `B007+` (ex. `B099v`) é o contrato, não falso verde, e não substitui a revisão semântica.
- Documentação B096: `10` declara o plano de SDT consumindo `Levels` quando há filhos; `ListResponse_Item` e contadores permanecem B098+; o `11` registra o limite 128 de objeto; o `08` e o `13` deixam de afirmar que a geração não consome a árvore; o `15`, o `26` e o `28` separam o contrato-alvo (B098 / Fase 7) do plano já emitido; a especificação de subníveis alinha a ordem de execução, o critério 2 (`Tests/SdtHierarchicalPlan/`) e o critério 9 (trava OpenAPI também lê `ApiPlanSdtHierarchicalNaming.cs`; `_API_ListResponse_Item` ainda não).
- Documentação B095 pós-entrega: §21 do `08` alinhada a `ApiPlanLevel`/`ApiPlan.Levels` (esboço `TransactionInfo`/`AttributeInfo` deixou de ser contrato vigente); `TransactionInfo`/`AttributeInfo` marcados como entidades conceituais do MD-F08; `ApiPlanLevel.Fields` declarados como candidatos da estrutura (não seleção do Wizard) no `08`, no `10`, na especificação de subníveis e na evidência B095; naquele alinhamento do `10`, árvore = B095 e nomes de SDT/`ListResponse_Item`/contadores = B096+ — o `B096` recoloca `ListResponse_Item` e contadores em B098+.
- `00-MASTER_INDEX_DO_PROJETO.md` e `25-MASTER_SUMARIO_EXECUTIVO_FINAL.md`: removem o status “implementação ainda não iniciada” — a extensão está em Alpha pública (`0.1.0-alpha.4`); apontam o checkpoint operacional.
- `TransactionAttributeKeyTraits.IsAutonumberCore`: short-circuit de PK composta **antes** do fail-open por metadata ausente — alinha o núcleo ao overload SDK. Caso `pkCount>1` + `hasAttributeMetadata=false` passa de `true` (histórico flat / Core anterior) para `false`. Teste cobre `(3, false, null)`.
- `TransactionAttributeKeyTraits.IsAutonumber`: short-circuit de PK composta **antes** de `GetPropertyValueString` — se a leitura lançar em chave composta, continua `false` (não cai no fail-open `true`). O teste `Test-PrototypeWizardAutonumberCompositeKey.ps1` trava essa ordem. `IsNullable` no helper permanece null-safe (`null` → `false`, sem NRE).
- `B107`: `Test-OpenApiClientContractValidity.ps1` deixa de ler YAML de pastas `C:\KBs\...` (falso verde e amarre à máquina). A trava do pré-push fica offline sobre `ApiPlan.cs`, incluindo `sdt_API_ErrorMessage`. Conferência de YAML publicado permanece evidência pontual na IDE.
- Documentação e teste da Fase 1 (`B095`): o teste deixa de montar `ApiPlanLevel` à mão (falso verde) e passa a exercitar `Build` + `IsAutonumberCore` + ouro JSON; a tabela de componentes e a evidência declaram o que o offline cobre e o que fica para smoke IDE.

## Validated

- Smoke HTTP multinível nos environments `.NET`/PostgreSQL e `.NET Framework`/SQL Server (`apiTeste`).
- Ciclo de vida hierárquico no U15 (Wizard, Sync, Remover); reencontro plano V1→V2.
- Smoke U13 multinível (`Build All` nos dois environments; HTTP fora do gate).
- Escala `Empresa` (13 subníveis) com `Build All` Success nos dois environments.
- Linhas U14+ (canônica) e U13 (satélite) compiladas em Release para este corte.

## Assets

- `GenexusOpenApiBuilder.Extension.dll` — GeneXus 18 U14, U15 e posteriores U14+. SHA-256 `89CACE1F006AD9411D1BC8E6ACD24C80CBCA2151C9DAC238F8BE4A88F18DB13A`.
- `GenexusOpenApiBuilder.Extension-gx18u13.dll` — GeneXus 18 U13. SHA-256 `0C98FF68F48830A38B53855D5DB9B3C65B2C080F22A143016A47A23E3DFDA17F`.

---

# [0.1.0-alpha.4] - 2026-08-24

Release focada no repasse das mensagens de erro do Business Component no HTTP 422 (`B102`).

## Added

- `B102`: Create/Update devolvem as mensagens de erro do Business Component em `ErrorResponse.Message` (`LongVarChar` 2097152, truncada visivelmente em 2045 + `...` = 2048) e em `Messages[]` tipado por `sdt_API_ErrorMessage`. Ligado por padrão, com default por KB e escolha por API fora do hash B067; o bloco Alpha de texto fixo continua reconhecido no reencontro. Gate HTTP fechado em 2026-08-24 nos dois environments da KB `wsEducacaoSpTeste` (`apiTeste`): texto da rule, acento UTF-8, truncamento visível, `Messages[]` com `business_rule`, opção desligada com texto genérico, filtro que exclui `Msg()` (tipo 0) e copia só `Type == 1` (`MessageTypes.Error`). YAML publicado de `apiTeste` declara `Messages` como `type: array` com `$ref` para `sdt_API_ErrorMessage` e **não** emite `maxLength`. Reencontro de API Alpha: cobertura parcial.

## Changed

- Revisão dirigida do plano da Sprint 9 (2026-08-23): a especificação de subníveis ganhou a Fase 0 (linha de base de não regressão para transações planas) e a Fase 7 (ciclo de vida sob hierarquia); os SDTs de subnível passam a ser próprios por contrato, nomeados `sdt<NomeBase>_API_<Papel>_<Subnível>`; a substituição de linhas no `Update` passa a exigir o marcador `<Subnível>Replace`; os contadores de `List` ficam desativáveis por subnível e restritos a subníveis diretos, alojados em `sdt<NomeBase>_API_ListResponse_Item`, que só existe quando há subnível selecionado; a metadata vai a `schemaVersion` V2 com leitura tolerante a V1. Novos itens de backlog: `B100` (serviço `Delete` opt-in), `B101` (experimento de membro nullable), `B102` (repasse da `Message` do Business Component, primeiro item da sprint), `B103` (reconhecimento de source por versão de contrato) e `B104` (organização de `Src`). Notas de revisão alinhadas nos documentos 05, 08, 13, 26, 28, no registro de decisões (`Emenda técnica — 2026-08-23`) e em `B011`. Sem mudança de código da extensão. Detalhe: `Docs/Implementation/2026-08-20-SUPORTE-TRANSACTIONS-SUBNIVEIS.md`.
- Revisão do plano de trabalho da Sprint 9 (2026-08-23), posterior à revisão da especificação e registrada na `Emenda técnica — 2026-08-23 (complemento)`: a Fase 0 ganha mecanismo em duas camadas — comparação automática de Source, Service Source e plano de SDT no checker, mais export XPZ dos SDTs na IDE —, e o critério de não regressão deixa de prometer "byte a byte" genérico; `B102` é especificado com `Message` em `LongVarChar` truncada pela geração, experimento de `Messages[]` como coleção tipada por SDT separado, repasse restrito a mensagens de erro, ligado por padrão, com preferência por KB e escolha por API; a sprint ganha os gates 4 e 5, cobrindo `B102` e `B100` com HTTP real nos dois environments, e a data de corte da triagem passa a ser a entrada da Fase 7; o contrato OpenAPI multinível entra nos critérios, com a trava mecânica estendida aos schemas derivados e geração de cliente como evidência pontual; a linha `Gx18u13` volta ao plano, com dois assets DLL em todos os cortes e smoke na IDE U13 no corte de subníveis; a propagação do marcador `<Subnível>Replace` entre níveis é fechada; a escala ganha limiares de reprovação e de alerta e o resumo do Wizard passa a exibir a contagem de objetos; a publicação passa a três cortes (`0.1.0-alpha.4`, `0.1.0-alpha.5` e `0.1.0-alpha.6`), desacoplando o `Delete` dos subníveis. Novo item de backlog: `B105` (escolha do chamador sobre o detalhe do corpo de erro, restringindo o default da API e nunca ampliando). Notas de reconciliação nos documentos 12, 15 e 27. Sem mudança de código da extensão.
- Documentação pública passa a declarar que a geração cobre apenas o primeiro nível da Transaction e que subníveis são ignorados sem aviso, nos três `README` e em `Docs/Public/DEMO.md`. A limitação existe desde a primeira Alpha e não estava registrada: quem gerou uma API sobre transação multinível recebeu um contrato que cobre apenas o cabeçalho.

## Validated

- Gate HTTP de `B102` nos environments `.NET`/PostgreSQL e `.NET Framework`/SQL Server (`apiTeste`).
- Linhas U14+ (canônica) e U13 (satélite) compiladas em Release para este corte.

## Assets

- `GenexusOpenApiBuilder.Extension.dll` — GeneXus 18 U14, U15 e posteriores U14+.
- `GenexusOpenApiBuilder.Extension-gx18u13.dll` — GeneXus 18 U13.

---

# [0.1.0-alpha.3] - 2026-08-18

Release focada na localização trilíngue da extensão (português, espanhol e inglês), robustez na reabertura e restauração do Wizard, e padronização do contrato de erro.

## Added

- Localização trilíngue completa da extensão (pt-BR, es, en): manifesto, comandos de menu, diálogos (Wizard, Sincronização, Preferências, Relatório Final) e mensagens na Output pelo idioma da KB.
- Restauração de serviços, campos e filtros de APIs existentes ao reabrir o Wizard.
- Diagnóstico B087 na Output ao bloquear alteração com fingerprint.

## Changed

- Contrato HTTP de erro de `List` alinhado a `Create` e `Update` (`ErrorResponse`, `RestStatusCode=400` e eliminação de `msg()`).
- Documentação pública completa com READMEs em português, espanhol e inglês.
- Ampliação da cobertura de testes unitários no pré-push mecânico.

## Fixed

- Reencontro seguro de API Objects com nome customizado via metadata persistente.
- Suporte à criação e reexecução em planos parciais somente `List`/`Get` (sem Create/Update).
- Parse de `Service Source` sem colisões ou duplicidade ao reabrir APIs já geradas.
- Leitura de SDTs próprios sem metadata através de iteração defensiva.
- Eliminação do falso positivo de integridade (B060) em serialização ISO-8601 com zeros fracionários.
- Botão "Voltar" ocultado na primeira aba do Wizard e centralização de diálogos no monitor da IDE.

## Validated

- Linhas U14+ (canônica) e U13 (satélite) compiladas em Release e validadas por inventário offline de assembly e metadados.
- Testes manuais e unitários de localização (pt-BR, es, en) e restauração no U15.

## Assets

- `GenexusOpenApiBuilder.Extension.dll` — GeneXus 18 U14, U15 e posteriores U14+.
- `GenexusOpenApiBuilder.Extension-gx18u13.dll` — GeneXus 18 U13.

---

## Histórico detalhado da 0.1.0-alpha.3

### Added

- Localização trilíngue da extensão (2026-08-15): detecção e resolução pelo idioma da KB (`ExtensionLanguage`, com fallback `English` sem KB aberta), catálogo centralizado de strings (`ExtensionLocalization`), localização de mensagens na janela Output (`ExtensionOutputLocalization`), comandos de menu traduzidos em português, espanhol e inglês no manifesto (`GenexusOpenApiBuilder.package`) e em `Package.cs`, e tradução de todas as janelas do Wizard, Sincronização, Preferências e Relatório Final. Testes unitários em `Tests/Localization/Test-ExtensionLanguage.ps1` e `Tests/Localization/Test-ExtensionOutputLocalization.ps1`.
- Leitura e restauração de filtros e seleções de APIs existentes no Wizard (2026-08-15): leitor `PrototypeWizardExistingApiContractReader` restaura serviços selecionados, campos de request/response, filtros e required a partir do API Object, da metadata persistente ou de SDTs próprios, evitando redefinir filtros deliberados ao reabrir o Wizard. Teste unitário em `Tests/WizardContract/Test-PrototypeWizardExistingApiFilters.ps1`.
- Diagnóstico B087 na Output (2026-08-15): quando o Wizard bloqueia o API Object no baseline de alteração intencional, a janela Output imprime `ClausulaQueFalhou`, o detalhe do fingerprint (`FingerprintDetalhe`, hashes gravado/recalculado, algorithm/scope, tamanho do snapshot) e `IntegrityPresente` lido do JSON mesmo quando o fingerprint falha, sem alterar a regra de posse.

### Changed

- Contrato HTTP de erro de `List` (2026-08-15): alinhado com `Create` e `Update`, utilizando `ErrorResponse`, `RestStatusCode=400` em erros de validação e remoção do uso de `msg()`. Sources legados com `msg()` permanecem reconhecidos como migráveis.
- Cobertura mecânica do pré-push (2026-08-17): `scripts/Invoke-PrePushMechanicalChecks.ps1` e `Tests/PrePushChecker/Test-OpenApiBuilderPrePushChecks.ps1` passaram a cobrir o parser de Service Source do wizard (`Test-PrototypeWizardServiceSourceParsing.ps1`), além dos testes de localização e restauração de filtros já registrados em 2026-08-15.
- Chrome da UI em português (2026-08-15): rótulos de segurança e paginação das preferências e do Wizard passam a português com acentos; `CreateRequest`, `UpdateRequest`, `Response` e `ListFilters` exibem gloss entre parênteses em pt-BR e espanhol (`ExtensionUiTerms.RoleLabel`) e permanecem só em inglês na UI inglesa. Verbos REST e tokens `Authentication`/`Authorization`/`None` continuam em inglês.
- Localização residual em espanhol (2026-08-15): motivos de campo bloqueado, operador `Contem`, status de geração, resumo do Wizard e frases da Output que ainda saíam em português passam pelo catálogo; a coluna de rótulos das preferências foi alargada para não cortar “Tamanho máximo da página”.
- Checkpoint e plano por sprints (2026-08-16): `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`, `Docs/Foundation/24-PLANO_IMPLEMENTACAO_REAL_POR_SPRINTS.md` e `Docs/Foundation/06-BACKLOG_v0.1.md` deixam de tratar localização residual, fingerprint B060, aborto na primeira aba e `Build All` pós-reencontro como pré-condição aberta; a próxima ação única passa a ser a Sprint 9.

### Fixed

- Reencontro de API Object com nome customizado (2026-08-17): `PrototypeWizardExistingApiContractReader` deixa de procurar apenas `api<Transaction>`. Com metadata própria, resolve o nome por `api.name`/`ownership.apiName` e lê o Service Source do objeto correto. Sem metadata, aceita conservadoramente um único API Object com Description própria e chamada a Procedure gerada para a Transaction; múltiplos candidatos bloqueiam o reencontro. Testes em `Tests/WizardContract/Test-PrototypeWizardExistingApiFilters.ps1` e `Test-PrototypeWizardServiceSourceParsing.ps1`. Validado no U15 com `apiFiscalPublica` / metadata `apiNotaFiscal_Metadata` e reabertura do Wizard.
- Primeira geração bloqueada no B070 por posse (2026-08-17): o Wizard aplica List, BC e metadata com `allowIntentionalContractRefresh: true`, modo em que a posse do API Object era confirmada só pela metadata — que o B060 grava depois do List. O API Object recém-criado pelo B054 era recusado como "não reencontrado com segurança" e a geração terminava interrompida, sem metadata. `ApiPlanApiObjectOwnership.ResolveIntentionalWriteOwnership` passa a decidir o modo: sem File de metadata vale o fallback histórico por Description e contrato gerenciado; com File, a posse continua exclusivamente da metadata; metadata ambígua bloqueia sem fallback. Teste `Tests/ApiObjectOwnership/Test-ApiPlanApiObjectOwnership.ps1`.
- Abertura do Wizard com Service Source gerado (2026-08-17): o parser deixava de abrir a API parcial porque a regex de `List|Get|Create|Update(` também casava a chamada `proc…_API_List(` / `proc…_API_Get(`, e o `ToDictionary` falhava com chave duplicada. A regex agora exige declaração (não precedida de identificador ou ponto), o contrato deduplica defensivamente e o Output registra duplicidade real sem derrubar o diálogo. Teste `Tests/WizardContract/Test-PrototypeWizardServiceSourceParsing.ps1`.
- Abertura do Wizard com SDTs próprios e sem metadata (2026-08-17): `ReadOwnedSdtFields` deixou de fazer cast de `StructureItemCollection` para `IEnumerable<SDTItem>`, que quebrava a abertura do Wizard após geração parcial. A leitura passa a iterar com `foreach (SDTItem item in Items)`, o mesmo padrão do Sync.
- Etapa Get/Create/Update REST sem Create/Update (2026-08-17): com plano só `List`/`Get`, o checkbox de Business Component fica bloqueado com motivo e não interrompe mais a geração; `ApplyList` e metadata podem concluir. Teste `Tests/WizardNavigation/Test-PrototypeWizardBusinessComponentNavigationPolicy.ps1`.
- Restauração de serviços no Wizard (2026-08-17): com contrato persistido (`ServicesAvailable=true`), serviços deliberadamente omitidos (ex.: só `List`/`Get`) deixam de reaparecer marcados ao reabrir; o fallback `true` permanece apenas quando não há seleção persistida. Teste `Tests/WizardContract/Test-PrototypeWizardExistingApiFilters.ps1`.
- Relatório final e confirmação em espanhol (2026-08-15): o título “Ningúna sincronizacao necessaria” vinha de `Nenhum` substituir o prefixo de `Nenhuma`; avisos longos quebrados a 96 colunas perdiam o restante da frase no diálogo; o parágrafo curto de Required no Resumen e o leftover `a atualizacao do API Object` no B054 ficavam em português; o MessageBox de eliminação usava Sim/Não do Windows. A tradução agora ocorre antes da quebra de linha, o catálogo cobre as frases faltantes e a confirmação B086 usa botões Sí/No da extensão, com altura ajustada ao texto (como o MessageBox anterior).
- Fingerprint da metadata B060 (2026-08-16): a relitura do File passava `generatedAtUtc` por `JObject.Parse`, que converte ISO-8601 em `DateTime` e, ao compactar de novo, corta zeros fracionários (`ToString("O")` vs formato ISO do Newtonsoft). O parse da metadata passou a `DateParseHandling.None`; a conferência SHA-256 permanece.
- Output de aborto do Wizard (2026-08-16): Voltar/Cancelar/fechar no wizard único e nos passos B031/B032, mais a saída de Business Component, passam pelo catálogo em espanhol e inglês.
- Primeira aba do Wizard (2026-08-16): o botão Voltar/Back/Atrás fica oculto; não fecha mais o diálogo. A saída sem concluir nessa tela continua sendo Cancelar, Esc ou fechar a janela.
- Leftovers de localização (2026-08-16): a confirmação B035 de habilitar Business Component, o fallback `<não definido>` no resumo de path e os MessageBoxes do relatório final (objeto principal ausente ou falha ao abrir) passam pelo catálogo em espanhol e inglês. Teste `Tests/Localization/Test-ExtensionLanguage.ps1`.
- Monitor do relatório final B081 (2026-08-16): o diálogo deixa de usar a posição do cursor; abre como modal da janela owner da IDE (`Form.ActiveForm`, com fallback em `Process.MainWindowHandle`) e centraliza na área útil desse monitor. Teste `Tests/ApplicationFinalReport/Test-ApiPlanApplicationFinalReport.ps1`. Validação manual U15 em multi-monitor ainda pendente.

---

# [0.1.0-alpha.2] - 2026-08-13

Release focada na liberação pública da linha GeneXus 18 Upgrade 13, mantendo o asset canônico U14+.

## Added

- DLL pública `GenexusOpenApiBuilder.Extension-gx18u13.dll` para GeneXus 18 Upgrade 13, com `GxLine=Gx18u13` e `PackageCompatibility=143920` validados no asset de Release.
- Notas de Release e guia de instalação com a distinção explícita entre a DLL canônica U14+ e a DLL satélite U13.

## Fixed

- Regras `NoAccept` deixam de gerar atribuições inválidas nos requests `Create` e `Update`, preservando a leitura em `Response`, `ListResponse` e filtros.

## Validated

- Linha U13: carga, menus, Wizard, recriação da API e `Build All` sem `spc0018`.
- Linha U14+: build Release e contrato `NoAccept` revalidados no U15, além da bateria HTTP autenticada da `apiNotaFiscal`.

## Assets

- `GenexusOpenApiBuilder.Extension.dll` — GeneXus 18 U14, U15 e posteriores U14+.
- `GenexusOpenApiBuilder.Extension-gx18u13.dll` — GeneXus 18 U13.

---

## Histórico detalhado da 0.1.0-alpha.2

### Added

- Suporte paralelo Gx18u13 avançado (2026-08-12): instalação local corroborou `N=143920`, o satélite compilou em Release com inventário offline aprovado e a carga foi confirmada no U13 por `genexus /install`, Extensions Manager marcado, menus principal e de contexto e Wizard executado na Transaction `Employee`. O Wizard terminou com `SuccessWithWarnings`, `Created=15`, `Updated=1`, `Blocked=0` e um aviso de fallback de descrições para inglês. O `Build All` da Transaction passou; o build focado das Procedures revelou e motivou a correção de `NoAccept`. Sem alterar a instalação protegida do GeneXus. Evidência: `Docs/Implementation/2026-08-12-FASE2-SATELITE-GX18U13.md`.
- Evidência de segundo usuário externo (2026-08-12): Miguel confirmou a Alpha `0.1.0-alpha.1` no GeneXus 18 U15 pelo caminho de mantenedor (repositório + build local + `Install-ExtensionForGeneXus18.bat` + `genexus /install`); feedback em issue #3. Não reabre o gate Sprint 8. Registro inicial que igualava à variante Packages/Release foi corrigido. Linha `Upgrade 15` na tabela de status do `README.md` reflete esse uso externo. Sem mudança de código da extensão. Evidência: `Docs/Implementation/2026-08-12-EVIDENCIA-USUARIO-EXTERNO-U15-ALPHA.md`.
- Evidência do gate Sprint 8 (2026-08-12): usuário externo Igor C. Menin com GeneXus 18 U14 (`18.0.187820`), DLL do Release `0.1.0-alpha.1`, instalação por cópia em `Packages` + `genexus /install`, menus e geração confirmados; feedback em issue #1; captura em `Docs/Images/alpha-u14-igor-menin.png`. Residual U14 de carregamento/uso prático fechado. Sem mudança de código da extensão. Evidência: `Docs/Implementation/2026-08-12-EVIDENCIA-USUARIO-EXTERNO-U14-ALPHA.md`.
- Workflow GitHub Actions (2026-08-12): `.github/workflows/publish-github-packages.yml` + `packaging/github-packages/GenexusOpenApiBuilder.Extension.nuspec` publicam `GenexusOpenApiBuilder.Extension` no GitHub Packages (NuGet) a partir da DLL do Release (`release: published` ou `workflow_dispatch`). Não altera instalação na IDE nem o guia da DLL. Sem mudança de código da extensão.
- Trava pré-push dos YAML / Issue Forms (2026-08-11): teste `Tests/IssueForms/Test-GitHubIssueFormsYaml.ps1` no check `tests.issueForms`, com lint estrutural offline e parse real via `python3`+`pyyaml` (ausência de ambiente → `environmentBlocked`, sem pular em silêncio). `.gitattributes` passa a forçar LF em `*.yml`/`*.yaml`. Sem mudança de código da extensão.
- Revalidação HTTP da `apiNotaFiscal` (2026-08-13): após remoção B086 e recriação pelo Wizard, os dois environments concluíram `Build All`; a bateria autenticada passou com OAuth `200`, ausência de token `401`, List/Get/Create/Update, `Location`, filtro, `404` de recurso inexistente e `400` de request inválido. Evidência: `Docs/Implementation/B071-B073-B079-GET-CREATE-UPDATE-HTTP.md`.
- READMEs em espanhol e inglês (2026-08-13): `README.es.md` e `README.en.md` com paridade de seções e de afirmações técnicas em relação ao `README.md`, e cabeçalho de troca de idioma nos três arquivos. Cada README aponta para as notas de Release do próprio idioma; o restante da documentação pública continua somente em PT-BR. Sem mudança de código da extensão.

### Changed

- Alinhamento documental pós-pré-push U13 (2026-08-13): nota de revisão em `B010-SDK-E-BUILD-MINIMO.md` e emenda no registro de decisões funcionais do MVP esclarecem que o satélite `Gx18u13` é cadeia paralela sem alterar o baseline canônico U14+; higiene de espaço espúrio no documento 06. Sem mudança de código da extensão.
- Wrappers de instalação (2026-08-12): `Install-ExtensionForGeneXus18.bat` e `Register-ExtensionForGeneXus18.bat` aceitam o diretório da instalação GeneXus como primeiro argumento, mantendo o padrão anterior quando omitido. Isso permite separar instalações U15 e U13 sem gravar caminhos locais no repositório.
- Cobertura mecânica adicional do pré-push (2026-08-13): `scripts/Invoke-PrePushMechanicalChecks.ps1` passou a executar os testes de leitura e elegibilidade de `NoAccept`, inventário offline da assembly e tratamento de caminhos dos BATs; a presença e o resultado desses quatro comandos também são verificados por `Tests/PrePushChecker/Test-OpenApiBuilderPrePushChecks.ps1`. O plano v12 registra explicitamente seu estado como snapshot pré-execução.
- Alinhamento documental pós-gate Sprint 8 (2026-08-12): `B000-CARREGAMENTO-IDE.md` deixa de listar validação U14 comunitária como pendência e registra o fechamento por usuário externo (issue #1); `CHANGELOG` Unreleased unifica as seções `## Added` duplicadas. Sem mudança de código da extensão.
- Ajuste dos Issue Forms (2026-08-11): formulário **Dúvida / outro** com um campo livre, para não perder contato curto na Alpha; blank issues continuam desabilitadas. Sem mudança de código da extensão.
- Preparação para feedback da Alpha (2026-08-11): Issue Forms em `.github/ISSUE_TEMPLATE/` (bug e sugestão; blank issues desabilitadas), labels `feedback-externo`, `instalacao` e `alpha-0.1.0`, e convenção no `CONTRIBUTING.md` para registrar no GitHub relatos que chegam por grupo ou mensagem direta. A próxima ação continua sendo o gate de usuário externo; sem mudança de código da extensão nem da documentação da Alpha publicada.
- Pós-publicação (2026-08-11): notas de release e checkpoint deixam de descrever a publicação como pendente. A Alpha `0.1.0-alpha.1` está publicada — tag `v0.1.0-alpha.1` no remoto apontando para `e0b2b7e` e GitHub Release pre-release com a DLL anexada (SHA-256 conferido após download). A tag publicada não deve ser movida; correção posterior exige `0.1.0-alpha.2`. Próxima ação = gate da Sprint 8 (usuário externo).
- Higiene pública pós-Alpha (2026-08-11): `LICENSE` sem cabeçalho `# LICENSE` (para o GitHub reconhecer MIT); `Docs/Public/INSTALL.md` e checkpoint registram que a sequência do usuário final foi reexecutada pelo mantenedor no mesmo dia (B094 §6); tópicos do repositório no GitHub (`genexus`, `openapi`, `rest-api`, `genexus-extension`). Sem mudança de código da extensão.

### Fixed

- Eco dos instaladores (2026-08-13): `Install-ExtensionForGeneXus18.bat` e `Install-ExtensionForGx18u13.bat` passam a orientar `Register-ExtensionForGeneXus18.bat` com `"%GENEXUS_DIRECTORY%"`, o mesmo diretório da cópia, em vez de “normalmente”. Evita `genexus /install` na IDE canônica depois de instalar a DLL satélite U13. Teste: `Tests/Installation/Test-InstallExtensionBatPathHandling.ps1`.
- Readme do pacote NuGet (2026-08-12): `packaging/github-packages/README.md` embutido no `.nuspec`/workflow de GitHub Packages, deixando claro que o feed não instala na IDE. O aviso “missing a readme” some na próxima publicação de versão. Sem mudança de código da extensão.
- Instalador satélite U13 (2026-08-12): `Install-ExtensionForGx18u13.bat` copia e valida explicitamente `artifacts/gx18u13/bin/Release/net471/GenexusOpenApiBuilder.Extension.dll`, evitando instalar por engano a DLL canônica na IDE U13. Teste: `Tests/Installation/Test-InstallExtensionBatPathHandling.ps1`.
- BAT de instalação com caminho `Program Files (x86)` (2026-08-12): a validação de `GeneXus.exe` deixou de abrir um bloco `if` dependente de parênteses depois da expansão de `%GENEXUS_DIRECTORY%`; isso evitava o erro `\GeneXus\GeneXus18 foi inesperado neste momento.` em caminhos com `(x86)`. Teste: `Tests/Installation/Test-InstallExtensionBatPathHandling.ps1`.
- Validação U13 do `NoAccept` (2026-08-13): após B086 remover 12 objetos próprios, o Wizard recriou `apiEmployee` (`Created=12`, `Updated=2`, `Blocked=0`) e o `Build All` especificou/compilou Create, Update, Get, List e `apiEmployee` sem `spc0018`; permaneceram somente fallback de descrições e aviso `pmm0003`.
- Compatibilidade U15 do `NoAccept` (2026-08-13): após nova remoção B086 e recriação da API, o Wizard confirmou `Create=5`, `Update=5`, campos bloqueados `CreateRequest=4`/`UpdateRequest=4`, `Created=12`, `Updated=2`, `Blocked=0`; o `Build All` U15 terminou sem `spc0018`.
- NoAccept em requests (2026-08-12): regra `NoAccept` passou a desabilitar o atributo no `CreateRequest` e no `UpdateRequest`, sem removê-lo de `Response`, `ListResponse` ou dos filtros. O motivo foi confirmado por A/B: sem `NoAccept`, as Procedures Create/Update passaram; com `NoAccept`, ambas falharam com `spc0018` por assignment em propriedade somente leitura. Evidência e implementação: `Docs/Implementation/2026-08-12-NOACCEPT-READONLY-BUSINESS-COMPONENT.md`.
- Workflow GitHub Packages (2026-08-12): `publish-github-packages.yml` passa a `windows-latest` porque `nuget.exe` no `ubuntu-latest` (24.04) depende de Mono, ausente na imagem — risco de `mono: not found` antes do primeiro run. Sem mudança de código da extensão.
- Issue Form **Dúvida / outro** (2026-08-11): o `placeholder` continha `Ex.:` sem aspas, e o segundo dois-pontos quebrava o YAML (`mapping values are not allowed here`). O GitHub não exibe formulário inválido e não emite aviso: o formulário simplesmente não aparecia no seletor, enquanto bug e sugestão apareciam. Valor entre aspas; os quatro arquivos de `.github/ISSUE_TEMPLATE/` revalidados por parse YAML.

---

# [0.1.0-alpha.1] - 2026-08-11

Primeira Alpha pública do Genexus Open API Builder (Sprint 8).

## Added

- Pacote público da Alpha: README orientado a visitante, [guia de instalação](Docs/Public/INSTALL.md), [demo rápida](Docs/Public/DEMO.md) e [notas de release](Docs/Releases/0.1.0-alpha.1.md).
- Galeria visual em `Docs/Images/` (Wizard completo, Preferências, Sync, Remover, metadata File, Folder e relatório de sucesso).
- Versão do pacote da extensão alinhada a `0.1.0-alpha.1`.

## Summary

- Marco **wizard funcional do MVP** concluído (2026-08-09), com comprovação integrada dos dez gates no GeneXus 18 U15.
- Ciclo de vida na IDE: Wizard, preferências por KB, sincronização com a Transaction, remoção conservadora, relatório final e UX mínima de colisão.
- Serviços `List`, `Get`, `Create` e `Update` com metadata persistente, posse via metadata e regeneração conservadora.
- Frentes pré-Alpha documentadas: limitações do YAML nativo (`B088`) e evidência HTTP 403 com role GAM restrita (`B089`).

## Changed

- Correção documental (2026-08-11): INSTALL do usuário final passa a seguir a ordem comprovada no B094 (Add > Local com a IDE aberta → fechar → `genexus /install` → reabrir). README e INSTALL deixam de ensinar atualização por Add > Local sobre DLL já em `Packages` — no B094 isso falhou com `Error installing extension`; atualização só com a DLL permanece não comprovada, e o caminho estável de atualização continua sendo o dos `.bat` de mantenedor.

- Documentação pública alinhada à evidência `B094` (2026-08-11): [INSTALL](Docs/Public/INSTALL.md) passa a distinguir instalação do usuário final (DLL via Add > Local + `genexus /install`, sem clonar) do fluxo de mantenedor (`.bat`); notas da Alpha deixam de negar o pacote instalável fora do repositório e listam o que ainda não entra (elevação, canal Web, desinstalação). Checkpoint, backlog 06, plano 24 e comprovação dos dez gates alinhados à ordem publicação → gate de usuário externo. Sem mudança de código da extensão. (A redação inicial de atualização por Add > Local no README foi corrigida em seguida; ver entrada acima.)

- `B094` (2026-08-10; correção 2026-08-11): evidência de instalação por usuário externo sem clonar o repositório — Add > Local + `genexus /install` ativou marcada + menus no GeneXus 18 U15, ainda com elevação (UAC). Correção do argumento falso “só Scanning / sem `added`” (captura incompleta); em cmd já elevado o log registrou `Package 'GenexusOpenApiBuilder.Extension.dll' added`. Premissa “`/install` elevado pode não varrer” refutada. Sem mudança de código da extensão. Evidência: `Docs/Implementation/B094-INSTALACAO-APENAS-COM-A-DLL-SEM-CLONAR.md`. Documentação pública não alterada nesta frente.

- Nota de revisão em `B000` (2026-08-11): `Docs/Implementation/B000-CARREGAMENTO-IDE.md` afirmava, em dois pontos, que `genexus /install` elevado não varria os pacotes no U15. A premissa foi refutada pelo `B094`; a observação original vinha de captura incompleta da saída. O texto de época é preservado e a nota registra a refutação. O contrato operacional do repositório não muda: `Register-ExtensionForGeneXus18.bat` continua recusando execução elevada. Sem mudança de código da extensão.

- `B089` concluído (2026-08-10): evidência HTTP **403 Forbidden** com papel GAM não-administrador sob `SecurityLevel = Authorization` em `apiNotaFiscal`. Setup via GAM Backoffice (role `Role_GOAB_Test_Denied`, Get Permitir, Create não atribuído, usuário `goab_role_denied`); GET **200** e POST Create **403** (`code` 139) nos environments .NET Framework/SQL Server e .NET/PostgreSQL; controle POST com usuário autorizado **201**. Documentado em `Docs/Implementation/B093-SECURITY-LEVEL-APIPLAN-OBJETO.md` §4.A.3.D. Sem mudança de código da extensão.

- `B088` concluído (2026-08-10): investigação read-only do gerador OpenAPI nativo (`Swagger.Yaml.stg` / `TypeDefinitions.Yaml.stg` / `Artech.Packages.RestServiceDL.Generator`) comprovou inviabilidade de substituir ou interceptar os templates sem alterar a instalação GeneXus. Limitação intransponível documentada; ressalvas e orientação de consumo (`openapi-generator-cli`, leitores/agentes do YAML cruzando Source/Events) incorporadas aos documentos 12 e 27. Sem mudança de código da extensão. Evidência: `Docs/Implementation/2026-08-10-B088-LIMITACOES-YAML-NATIVO.md`.

- Correção documental pós-Sprint 7 (2026-08-09): no gate 9 (metadata e reconhecimento seguro), a remissão de backlog passa a incluir `B087` junto de `B085`–`B086`, alinhando a matriz canônica do documento 06 §7.1 e a coluna de remissões do pacote `Docs/Implementation/2026-08-09-COMPROVACAO-DEZ-GATES-SPRINT7.md` à evidência de posse via metadata já citada.

- Sprint 7 fechada (2026-08-09): comprovação integrada dos dez gates técnicos transversais e marco **wizard funcional do MVP concluído**, por consolidação do acervo U15 das Sprints 1–7 (sem bateria U15 nova e sem mudança de código). Upgrade 14 permanece residual não validado nesta máquina e não bloqueia o marco. Evidência: `Docs/Implementation/2026-08-09-COMPROVACAO-DEZ-GATES-SPRINT7.md`.

- B083 residual (2026-08-08): UX mínima de conflitos de colisão — para cada objeto conflitante, lista `Nome | Tipo | Modulo | Folder` no Wizard, preflight, Output e relatório quando a escrita é barrada por colisão. Núcleo de detecção/bloqueio sem overwrite/`_v2` já existia. File usa `Folder='(n/a)'`. Validado no U15 em `Teste` (colisão com SDT externo; após remoção, geração completa e Build All nos dois environments). Teste `Tests/CollisionUx/Test-ApiPlanCollisionConflict.ps1`. Manifesto inalterado (só DLL). Evidência: `Docs/Implementation/B083-UX-CONFLITOS-COLISAO.md`.

- B081 (2026-08-08/09): relatório final pós-aplicação após Wizard, Sync (incluindo sem diff) e Remover — diálogo com criados/atualizados/removidos/bloqueados/avisos, efeitos colaterais do plano (Folder criado, Transaction atualizada pelo Business Component e SDTs escritos pelos writers), Output `[B081]`, Abrir objeto principal, wrap, altura ajustada em 10% e largura em 20%, limites relativos à área útil e rolagem vertical recalculada após o layout. Wizard principal inicia em `1200x912`, com mínimo `900x640`. Validado no U15 em `Teste`: criação com `Created=12`, incluindo `TesteOpenApi`; reuso com `Created=0`, `Updated=13`, `Warnings=2`; restauração final com `Created=11`, `Updated=2`; remoção com Folder reutilizado preservado. Teste `Tests/ApplicationFinalReport/Test-ApiPlanApplicationFinalReport.ps1`. Manifesto inalterado (só DLL). Evidência: `Docs/Implementation/B081-RELATORIO-FINAL-POS-APLICACAO.md`.

- Folder reutilizado (2026-08-08/09; alinhamento B066): Folder preexistente `<Transaction>OpenApi` no contêiner correto passa a ser reutilizado com aviso explícito, sem `Save()`/realinhamento e sem alterar Description; Folder em contêiner incorreto, sentinela divergente ou ocorrência ambígua continua bloqueando. Metadata mantém `wasCreated=true` para criação e `false` para reencontro; remoção preserva Folder reutilizado. Validado no U15 em `Teste`: caminho feliz com Description humana; contêiner incorreto, duplicidade entre módulos e sentinela de outra Transaction bloqueados antes de qualquer escrita; restauração final com `Created=11`, `Updated=2`, `Blocked=0`, `Warnings=2`; relatório rolável e metadata com `transactionFolder.wasCreated=false`. Teste `Tests/TransactionFolder/Test-ApiPlanTransactionFolderReusePolicy.ps1`. Manifesto inalterado (só DLL). Evidência: `Docs/Implementation/2026-08-08-FOLDER-REUTILIZADO-COM-AVISO.md`.

- B085 (2026-08-08): comando `Sincronizar com a Transaction` (menu principal e contexto); diff Transaction×metadata por `attributeGuid`; UI 2x2 de escolhas no delta; confirmação; gravação via writers/preflight sem reabrir o wizard. Metadata passa a persistir `transactionStructure` e `attributeGuid` nos campos. Preflight de Sync permite atualizar o contrato B067 e Source/Rules B055/B070 de propósito. Ordem do menu: Preferências → Wizard → Sincronizar → Remover API gerada. Validado no U15 em `Teste`: sem diff; add; rename por GUID; cancelar; conflito Replace; conflito Keep (`ManualKeep` preservado). Teste `Tests/TransactionSync/Test-ApiPlanTransactionSyncComparer.ps1`. Manifesto alterado (`genexus /install` necessário). Evidência: `Docs/Implementation/B085-SINCRONIZAR-COM-TRANSACTION.md`.

- B086 (2026-08-07/09): comando `Remover API gerada` no menu principal e no contexto da Transaction; plano a partir da metadata; confirmação; exclusão conservadora. Ordem: API Object → Procedures → SDTs (ListResponse antes de Response) → metadata → Folder se `wasCreated`. Validado no U15 em `Teste`: cancelamento; Folder reutilizado preservado; Folder criado removido (`Deleted=12` incluindo `TesteOpenApi`); reteste do Folder reutilizado com `Deleted=11`, `Blocked=0`, `Warnings=0` e Folder preservado; SDTs `GxOpenAPI` e BC preservados. Confirmação com Procedures/SDTs um por linha. Manifesto alterado. Teste `Tests/GeneratedApiRemoval/Test-ApiPlanGeneratedApiRemovalPlan.ps1`. Evidência: `Docs/Implementation/B086-REMOVER-API-GERADA.md`.

- B087 (2026-08-07): posse do API Object passa a ser resolvida pela metadata (`ownership` + integridade B067), com fallback na `Description` apenas quando o File ainda não existe. Reencontro deixa de sobrescrever a `Description`. Validado no U15 em `apiTeste` com Description humana preservada após Wizard. Description gerada padrão sem ponto terminal; forma legada com ponto aceita no fallback/integridade. Teste `Tests/ApiObjectOwnership/Test-ApiPlanApiObjectOwnership.ps1` integrado ao pré-push. Evidência: `Docs/Implementation/B087-POSSE-API-OBJECT-METADATA.md`.

- Revisão documental da Sprint 7 (2026-08-07): o objetivo vigente deixa de ser “revalidar conflitos/reexecução” (já entregues nas Sprints 5–6) e passa a ser o ciclo de vida operacional na IDE — posse do API Object via metadata (`B087`), `Remover API gerada` (`B086`), `Sincronizar com a Transaction` (`B085`), relatório final (`B081`), UX mínima de conflitos e comprovação dos dez gates. `B088` (YAML nativo) e `B089` (403 GAM) ficam pré-Alpha separados. Documentos: `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`, `Docs/Foundation/24-PLANO_IMPLEMENTACAO_REAL_POR_SPRINTS.md`, `Docs/Foundation/06-BACKLOG_v0.1.md`.

## Fixed

- Sync B085 (2026-08-10): posse do API Object no preflight deixa de exigir `IsManagedApiObject` contra o ApiPlan reconstruído; ownership na metadata (`schemaVersion` + `apiName` + `apiGuid`) basta. Corrige bloqueio falso ao adicionar campos. A seleção de campos no Sync preserva a ordem da metadata e anexa adds no fim (dedupe por GUID). Validado no U15 em `NotaFiscal`/`NotaFiscalObs3` (`Updated=13`, `Blocked=0`).

- Descriptions de produto sem IDs de backlog (2026-08-09): objetos gerados passam a usar `{Nome} - by Genexus Open API Builder` (Procedures, SDTs, API Object, metadata, Folders criados, preferências). Sentinelas legadas com `B0xx` / prosa `REST API for …` continuam reconhecidas no reencontro, Sync e Remover, desde que a descrição inteira seja compatível com o serviço, tipo, API e Transaction esperados. Divergências deixam de herdar posse por prefixo. Validado no U15 em `Teste`: Descriptions na IDE; Remover `Deleted=12` com posse canônica; Sync/Remover sem metadata bloqueiam; Wizard restaura `Created=12`; Sync sem diff; cancelamento do Remover. Manifesto inalterado (só DLL). Evidência: `Docs/Implementation/2026-08-09-DESCRIPTIONS-PRODUTO-SEM-BACKLOG.md`.

- B086 preflight de remoção (2026-08-09): `ValidateRemovalTargets` confere ambiguidade e posse de API Object, Procedures e SDTs próprios em `Preview` e no início de `Remove`, antes de qualquer `Delete()`. Evita remoção parcial quando um alvo divergente só seria detectado após o API Object já ter sido apagado. Manifesto inalterado (só DLL). Evidência: `Docs/Implementation/B086-REMOVER-API-GERADA.md`.

- Ownership legado e pré-voo B086 (2026-08-09): Procedures e SDTs só reconhecem a combinação legada exata de backlog + serviço/tipo; metadata também exige `Transaction` e `Api` compatíveis. O B086 ganhou regressão automatizada para confirmar o preflight de API Object, Procedures e SDTs antes do primeiro `Delete()`. Testes `Tests/OwnershipDescriptions/Test-ApiPlanOwnedObjectDescription.ps1` e `Tests/GeneratedApiRemoval/Test-ApiPlanGeneratedApiRemovalPreflight.ps1`. Manifesto inalterado (só DLL).

- Cobertura mecânica dos testes de ownership e B086 (2026-08-09): os dois testes acima passaram a ser executados pelo `scripts/Invoke-PrePushMechanicalChecks.ps1`, com fixtures, status e comandos verificados pelo teste do próprio checker. Isso evita que a cobertura fique restrita à execução manual.

- B085 Keep (2026-08-08): Sync passa `preserveSdts` também para writers BC (B055) e List (B070); antes o Keep só omitia `ConfigureSdt` na 1ª etapa e BC/List regravavam o SDT. Validado no U15 em `Teste` com membro `ManualKeep` preservado e `PreservedSdts=1`.

- Correção do `Location` para chave de texto (2026-08-06):
  - Path-encoding: `StrReplace(URLEncode(Trim(...)), !"+" , !"%20")` para emitir espaço como `%20` em segmento de path (a forma `URLEncode` sozinha emitia `+` e o GET no `Location` falhava).
  - Recusa de `/` em parte de PK texto no Create: após `Save` e antes do `Commit`, `Rollback` + `400 invalid_request` — `/` não é endereçável de forma confiável no path REST do MVP.
  - Preflight migra a forma anterior `URLEncode(Trim(...))` via `&LocationUrl`; teste de contrato atualizado com verificação por mutação.
  - Validação HTTP nos dois geradores: espaço `COD%2001` com GET `200`; acento e simples sem regressão; barra `400` sem gravação. Captura `Temp/location-matrix-2026-08-06-pos-correcao.json`; doc `Docs/Implementation/B071-B073-B079-GET-CREATE-UPDATE-HTTP.md`.

- Passo 4 (opção 2, 2026-08-06): chave primária não autonumerada no CreateRequest inicia **opcional** (não required); a aba Obrigatorios do Create ficou editável para exigir o campo no payload quando desejado. Evita 400 quando rules/BC preenchem chave omitida ou com default do tipo. Smoke HTTP em `apiTeste` (dois environments): POST só com `TesteDesc` → `201`, `Location` navegável e GET `200` após rules `on BeforeInsert` na Transaction `Teste` (rules `if insert` não preencheram `TesteId`/`TesteCodigo` via BC).

- Autonumeração no wizard (Passo 3, 2026-08-06): chave composta (`PrimaryKeyParts > 1`) deixa de bloquear campos no CreateRequest; PK simples continua lendo `Autonumber`/`idAUTONUMBER` com fallback conservador. Evidência em `Teste` vs `NotaFiscal`; sonda TEMP `AutonumberProbe` removida após a captura.

- Performance do wizard (2026-08-06): `ReadGenerationState` passou a um `GetAll` por tipo com lookup por nome; preview evita reentrância, só recalcula em abas de geração e usa cache por fingerprint (Resumo força refresh). Testes em `Tests/WizardContract/`.

- Recaptura 2026-08-06 do cabeçalho `Location` com chave composta e `URLEncode` na Transaction `Teste`:
  - Gerador Create passou a montar `&LocationUrl` e a usar `URLEncode(Trim(...))` após `Validation of Procedure` rejeitar a forma anterior com `.Trim()` encadeado na gravação em 1 clique.
  - Matriz HTTP real documentada em `Docs/Implementation/B071-B073-B079-GET-CREATE-UPDATE-HTTP.md`: acento navegável; espaço emitido como `+` e não navegável no `Location`; `%2F` com `404` nos dois ambientes (com corpos distintos). A matriz anterior que afirmava `200` para espaço/`%20` e sucesso de `%2F` no Kestrel foi marcada como superada.
  - Checkpoint `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md` alinhado à evidência capturada.

- Correções sobre os commits `ee09fa3` e `b4a70f6`:
  - Restaurado o reencontro de SDTs `ApiPlanSdtWriter.CreateOrReencounter` no fluxo `ApplyList` (`ApiPlanListProcedureWriter.cs`) para suporte a reaplicação isolada.
  - Invertido o fallback de autonumeração em `PrototypeWizardContract.cs` para bloqueio defensivo no `CreateRequest` em caso de erro ou incerteza.
  - Revertida a visibilidade dos 13 métodos auxiliares de writers de `internal` de volta para `private`.
  - Documentadas as alterações estruturais do commit `b4a70f6` e o defeito histórico do `ErrorItem` em `Docs/Implementation/2026-08-05-REGISTRO-MUDANCAS-ESTRUTURAIS-B4A70F6-E-DEFEITO-ERRORITEM.md`.
  - Corrigida a referência do documento 27 no backlog `06-BACKLOG_v0.1.md`.

## Added

- Fechamento dos gaps de evidência da Sprint 6 (Gaps 1, 2 e 3):
  - **GAP 1**: Provado em runtime HTTP o nível `SecurityLevel = Authorization` em `apiNotaFiscal` nos dois geradores (.NET Framework/SQL Server e .NET Core/PostgreSQL), registrando respostas 401 sem token / token inválido e 200 OK com token OAuth GAM válido do usuário de teste.
  - **GAP 2**: Corrigido o `CreateLocationUrlExpression` no gerador `ApiPlanBusinessComponentWriter` para serializar atributos de data no formato ISO estrito `YYYY-MM-DD` e aplicar `EncodeUrl` em campos alfanuméricos. Provada em runtime HTTP a emissão e navegabilidade direta do cabeçalho `Location` com chave primária composta (`TesteId` + `TesteDate`) na Transaction `Teste` (POST `201 Created` e GET `200 OK` direto na URL exata do `Location` retornado sem reescrita de URL, nos geradores .NET Framework e .NET Core).
  - **GAP 3**: Registrados os itens de backlog pré-Alpha `B088` (limitações do gerador nativo `Swagger.Yaml.stg`) e `B089` (validação automatizada de permissões granulares GAM por roles) em `Docs/Foundation/06-BACKLOG_v0.1.md` e a nota de auditabilidade e sobrescrita de arquivos YAML em `Docs/Implementation/B093-SECURITY-LEVEL-APIPLAN-OBJETO.md`.
- Tratamento do resíduo condicional do cabeçalho `Location` no serviço `Create` (B072/B079) para fecho integral da Sprint 6: o gerador de Procedure Business Component (`ApiPlanBusinessComponentWriter`) passou a emitir `&HttpResponse.AddHeader(!"Location", ...)` nativo e a declarar a variável `HttpResponse` (tipo `HttpResponse, GeneXus.Http`) ao retornar `201 Created`, montando a URL relativa do endpoint `Get` a partir do `RestPath` e da(s) chave(s) primária(s) simples ou composta(s).
- Fechamento da Sprint 6 com validação das três frentes de encerramento:
  - **Frente 1 (B047)**: Criado o teste automatizado off-line [`Tests/OpenApiContract/Test-OpenApiClientContractValidity.ps1`](file:///c:/Dev/Knowledge/Genexus-Open-API-Builder/Tests/OpenApiContract/Test-OpenApiClientContractValidity.ps1) para atestar a validade de identificadores `_API_` e `operationIds` para geradores de cliente OpenAPI, integrado à rotina pré-push `scripts/Invoke-PrePushMechanicalChecks.ps1`.
  - **Frente 2**: Auditoria minuciosa do YAML nos geradores `.NET Framework / SQL Server` e `.NET / PostgreSQL` cobrindo os seis eixos exigidos (rotas, métodos, `operationId`s `apiNome.Serviço`, ausência de DELETE, schemas `_API_` congelados em B062, bloco `security` por serviço B093 e schemas de request/response sem `Errors`), com evidência registrada em [`Docs/Implementation/2026-08-04-VALIDACAO-YAML-SPRINT6-EIXOS-SEGURANCA.md`](file:///c:/Dev/Knowledge/Genexus-Open-API-Builder/Docs/Implementation/2026-08-04-VALIDACAO-YAML-SPRINT6-EIXOS-SEGURANCA.md).
  - **Frente 3**: Declaração formal do Gate da Sprint 6 e Gate Técnico Transversal 4 como **Aprovados com Ressalva**, com alinhamento documental em `09`, `15`, `24` e emenda técnica datada de 2026-08-04 no registro primário de decisões `Docs/Decisions/2026-07-14-REGISTRO_DECISOES_FUNCIONAIS_MVP.md`.
- Estrutura inicial do repositório
- Pasta Docs organizada
- Foundation Docs 00 até 28
- checkpoint operacional `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`
- template de evidência reproduzível de `B010` em `Docs/Implementation`
- consolidação documental da entrevista funcional do MVP
- README inicial
- LICENSE MIT
- Planejamento da fase prática
- Sprint 0 concluído: build mínima reproduzível (`B010`–`B012`), solution e projeto de extensão em `Src`
- B000 concluído no U15: extensão mínima registrada, marcada e carregada na IDE com a DLL Release estável e os metadados públicos corrigidos
- B001 concluído no U15: detecção da KB ativa por API oficial, em modo somente leitura
- B002 concluído no U15: listagem de 10 Transactions reais por API oficial, em modo somente leitura
- B003 concluído no U15: criação controlada de Folder de teste com autorização explícita
- correção de segurança pós-B003 validada: a DLL atual não executa sondas automaticamente ao abrir uma KB
- B004 concluído no U15: ciclo de vida de API Object oficial comprovado com criação, alteração, releitura após reinstalação e exclusão confirmada
- B005 concluído no U15: ciclo de vida de Procedure, SDT, Folder e File comprovado com criação, alteração, releitura e exclusão confirmada
- B006 concluído no U15: metadata JSON em File preservou GUID, nome, descrição, bytes UTF-8 e SHA-256 após fechar e reabrir a KB
- B020 concluído no U15: detecção manual da KB ativa no fluxo do protótipo navegável, em modo somente leitura
- B021 concluído no U15: listagem manual de 10 Transactions da KB ativa no fluxo do protótipo navegável, em modo somente leitura
- B022 concluído no U15: seleção nativa manual de Transaction e leitura de seu módulo no fluxo do protótipo, em modo somente leitura
- B023 concluído no U15: detecção manual dos objetos planejados para a Transaction selecionada, incluindo `api<NomeBase>_Metadata`, em modo somente leitura
- B024 concluído no U15: verificação manual de `Business Component` da Transaction selecionada, em modo somente leitura
- B025 concluído no U15: leitura manual da chave primária simples e composta completa da Transaction selecionada, em modo somente leitura
- Menu principal `Genexus Open API Builder` validado no U15 com acesso aos comandos B020-B030, preservando o submenu de contexto da Transaction
- B030 validado no U15: `Abrir Wizard (B030)` seleciona `Transaction` pelo menu principal via seletor nativo e pelo contexto da Transaction, mantendo a escolha apenas em memória
- B031 validado no U15: `Configurar Contrato (B031)` navega sequencialmente por serviços, requests, response, filtros e resumo B032, acumulando escolhas apenas em memória sem criar `ApiPlan` nem alterar a KB
- B032 validado no U15: `Revisar Paths e Segurança (B032)` funciona pelo menu de contexto da `Transaction`, chama B031 automaticamente quando necessário e revisa paths, segurança, paginação e ordenação apenas em memória
- B033 validado no U15: `Abrir Wizard (B030)` passa a abrir o wizard único, absorvendo B031, B032 e B033 como páginas sequenciais e validando campos obrigatórios apenas em memória
- B034 validado no U15: cancelamento seguro do wizard único descarta `Transaction`, contrato, paths/segurança e obrigatoriedade em memória sem criar `ApiPlan` nem alterar a KB
- B035 validado no U15: wizard único verifica `Business Component`, bloqueia avanço sem BC e habilita a propriedade somente com confirmação explícita, mantendo decisões apenas em memória e sem criar `ApiPlan`
- B036 validado no U15: wizard único exibe campos tecnicamente inadequados desabilitados com motivo em `Requests` e `Filtros List`, mantém bloqueados não selecionáveis e registra contagens apenas em memória sem criar `ApiPlan`
- B037 validado no U15: wizard único consolida `Obrigatório no payload` para `CreateRequest` e `UpdateRequest`, explicita `Required` como presença do membro JSON e mantém decisões apenas em memória — a semântica de presença foi revista em 2026-08-03 e substituída por validação de preenchimento; ver a entrada correspondente em `Changed` e a nota de revisão de `B076` no documento 06
- B038 validado no U15: wizard único monta `ApiPlan` inicial em memória para `Contrato`, com `MetadataFile='apiContrato_Metadata'`, 4 endpoints, 4 Procedures planejadas e 2 SDTs compartilhados, marcado como `IsEngineReady=false` e com pendências `UNRESOLVED_B038_*`, sem persistir metadata nem gerar objetos na KB
- Representação provisória de B090/B091 e B092 validada no U15 dentro do `ApiPlan`: wizard único classifica sensíveis e auditoria por política inicial hardcoded em memória, registra origem/razão no `ApiPlan` e grava `SecurityLevel` com `GamCondition='UNRESOLVED_B092_GAM_CONDITION'`, mantendo B090/B091 canônicos abertos até configuração por KB/metadata e sem persistir metadata nem gerar objetos na KB
- Follow-up da Sprint 3 validado manualmente: `ApiPlan` em memória resolve `GeneratorTarget='.NET'`, `ConflictMode='BlockOnCollision'` e `ReexecutionMode='Safe'`, mantendo naquele momento condição GAM e prontidão da engine explicitamente pendentes, sem persistir metadata nem gerar objetos na KB
- Contrato preparatório de configuração por KB para B090/B091 validado no U15 dentro do `ApiPlan`, ainda sem metadata persistente e sem gerar objetos na KB
- B056 validado no U15 dentro do `ApiPlan`: descrições de serviço resolvidas em memória com `ServiceDescriptionsPending=0/4`, idioma `English` e fallback técnico registrado, ainda sem aplicar `[Description]` em objeto `API` real naquele recorte preparatório
- B092 validado no U15 dentro do `ApiPlan`: `Authentication`, `Authorization` e `None` agora resolvem `GamCondition` explicitamente no plano, com confirmação obrigatória para `Authorization` e `None`, ainda sem aplicar segurança em objeto `API` real e sem gerar objetos na KB
- B039 validado no U15: preview de engine SDT recebe o `ApiPlan`, resolve cinco SDTs próprios e dois compartilhados com status `ResolvedSdtContractPreviewNoKbWrite`, registra `WritesKnowledgeBase=False` e não escreve na KB
- B040-B046 validados no U15: comando `Criar SDTs (B040-B046)` criou 7 SDTs a partir do `ApiPlan` após confirmação modal explícita na IDE, com 5 próprios e 2 compartilhados, sem Procedures, API Object ou metadata persistente definitiva
- B050-B053 validados no U15: comando `Criar Procedures (B050-B053)` bloqueou execução sem `ApiPlan`, depois criou 4 Procedures skeleton a partir do `ApiPlan` e reencontrou 7 SDTs B040-B046, sem API Object, REST completo ou metadata persistente definitiva
- `Abrir Wizard (B030)` validado no U15 oferecendo B040-B046 e B050-B053 em abas próprias de confirmação no wizard, com Output marcada com `Trigger='Wizard'`, reencontro de 7 SDTs e 4 Procedures existentes e preservação dos comandos separados para reexecução/diagnóstico
- Correção pós-revisão B040-B046/B050-B053 validada no U15: abas `SDTs` e `Procedures` do wizard confirmam a escrita antes do resumo, Output registra `GenerateSdts=True`, `GenerateProcedures=True`, `Trigger='Wizard'`, reencontro de 7 SDTs e 4 Procedures, sem modais pós-wizard e sem criar API Object, REST completo ou metadata persistente definitiva
- B054 validado no U15: `apiCarga` exibe `List`, `Get`, `Create` e `Update` na aba `Service Source`, cada serviço delegando sem parâmetros à Procedure skeleton B050-B053 correspondente; reexecução bloqueia fonte divergente
- wizard passou a reconhecer em leitura o estado atual de SDTs, Procedures e API Object, apresentando criação, complementação, reencontro ou bloqueio antes de qualquer escrita e usando `Concluir Teste` quando não há etapa confirmada
- Folder `<Transaction>OpenApi` criado ou reencontrado como irmão físico da Transaction, com realinhamento conservador em reexecuções
- B055 validado no U15: Create e Update passaram a ser aplicados via Business Component nas Procedures já geradas, com variáveis reais, Source/Rules persistidos nas partes públicas corretas, preflight conservador, validação em chave simples (`Carga`) e composta (`Teste`) e API Object sincronizado com assinatura/variáveis compatíveis
- B056 aplicado e validado no U15 em API Object real: `apiGuiaPed` recebeu `[Description]` nos serviços `List`, `Get`, `Create` e `Update`, preservou as assinaturas parametrizadas de B055 em `Create`/`Update`, e `Build All` passou gerando documentação REST
- B060 concluído no U15: wizard grava ou reencontra File JSON de metadata, persiste `External File Name` via `BlobPart.FileName`, bloqueia JSON inválido, identidade incompatível e colisão externa antes da escrita, preserva descrições especiais B056 com aspas, barra invertida e caracteres incomuns, exporta JSON válido e mantém o escopo sem completar REST, códigos HTTP finais ou segurança definitiva
- B061/B062 validados no U15: objetos específicos que suportam Folder permanecem no Folder `<Transaction>OpenApi` dentro do módulo da `Transaction`, cobrindo `ContratoOpenApi` no `Root Module` e `SimulationResultOpenApi` no módulo não-root `Entities`; SDTs compartilhados permanecem em `GxOpenAPI`, File de metadata permanece no módulo da Transaction e nomes persistidos seguem as convenções congeladas
- B063-B066 validados no U15: preflight agregado bloqueia colisões externas/incompatíveis antes do primeiro `Save()`, metadata compatível permite reencontro conservador, paths/filtros/paginação/ordenação/segurança são persistidos e `transactionFolder.wasCreated` distingue Folder criado de Folder reutilizado
- B067 validado no U15: metadata grava integridade de descrições geradas, contrato planejado e Service Source; alteração manual posterior em `[Description]` bloqueia o wizard antes do primeiro `Save()` e a restauração da descrição original permite reencontro conservador
- B070/B077 validados no U15: `List` real sincroniza Procedure e API Object com filtros elegíveis, paginação, ordenação determinística, `totalCount`, `totalPages` e `AppliedFilters`; membros nullable de `ListFilters` são gerados com `Json Null Serialization = JSON null`, preservando `AppliedFilters.ContratoNumero=null` quando o filtro não é informado
- B068 implementado e validado funcionalmente no U15: novo comando `Configurar Preferências do Wizard` persiste defaults por KB no File `GxOpenApiBuilder_Settings`, e `Abrir Wizard (B030)` aplica esses defaults somente quando a etapa está habilitada pelo estado da KB; a configuração foi ampliada para serviços REST, `Security Level`, `Default Page Size` e `Maximum Page Size`; o preflight agregado respeita etapas selecionadas e B070 aceita reconfiguração segura de paginação em Source próprio conhecido
- Menu `Genexus Open API Builder` simplificado: no menu principal ficam `Configurar Preferências do Wizard` e `Wizard`; no contexto da Transaction fica somente `Wizard`; comandos incrementais B020-B025, B040-B054 e o placeholder `Futura Primeira Opção` foram removidos do runtime e do manifesto após o wizard absorver o fluxo.
- Preferência de `ApplyBusinessComponent` passa a ser aplicada somente quando a Transaction está apta via Business Component ou foi habilitada explicitamente no wizard, evitando bloqueio indevido de B055 e preservando List/Metadata quando BC está desabilitado.
- Navegação do wizard deixa de depender da passagem sequencial pela aba `Business Component`: a habilitação explícita pendente agora é avaliada também ao abrir o `Resumo` diretamente e antes da conclusão.
- Intenção de aplicar Create/Update via Business Component é preservada enquanto a habilitação explícita de BC ainda está pendente, permitindo que B055 rode no mesmo wizard após a Transaction ser habilitada.
- Ao marcar `Habilitar Business Component agora`, a aba passa a liberar `Aplicar Create/Update via Business Component após habilitar` como decisão pendente, sem antecipar gravação na KB.
- `Aplicar Create/Update via Business Component` passa a exigir SDTs, Procedures e API Object disponíveis ou confirmados no wizard, evitando B055 isolado quando a KB não tem dependências seguras.
- Aba `Business Component` foi reposicionada depois de `SDTs`, `Procedures` e `API Object`, alinhando a navegação com as dependências de B055.
- Após habilitar Business Component ao entrar direto no `Resumo`, o wizard recalcula as dependências antes de montar a seleção final, preservando `ApplyBusinessComponent=True` quando B055 estava marcado.
- Cobertura automatizada da navegação B055 passou a simular refresh local sem dependências seguido de refresh completo, garantindo restauração da intenção pendente antes da seleção final.
- B071-B073/B079 implementa a etapa REST de Get/Create/Update para regravar `proc<Transacao>_API_Get`, `proc<Transacao>_API_Create` e `proc<Transacao>_API_Update`, sincronizar o API Object com variáveis de `GetResponse`, `ErrorResponse` e `RestStatusCode`, expor `ErrorResponse` como saída pública dos três serviços, e usar Events do API Object para aplicar `&RestCode` em Get 200/404, Create 201/422 e Update 200/404/422; B055 próprio legado e variantes intermediárias B079 são reconhecidos como estados migráveis; a geração/reencontro foi validada na IDE em `NotaFiscal`; runtime HTTP foi validado em .NET Framework/SQL Server e .NET/PostgreSQL com List, Get, Create, Update, 400 e 404; `Location` de Create permanece pendente de confirmação nativa simples.
- B093 implementa a aplicação explícita do `Security Level` no API Object gerado: os writers de Business Component e List passam a emitir `[SecurityLevel(Authentication)]`, `[SecurityLevel(Authorization)]` ou `[SecurityLevel(None)]` em todos os serviços do API Object; o parser de contrato `ApiPlanServiceSourceContract.cs` e a integridade B067 em `ApiPlanMetadataFileWriter.cs` foram alinhados com suporte a migração conservadora de objetos anteriores, travados por testes unitários e validados pelo checker pré-push.

## Fixed

- contrato OpenAPI gerado passa a declarar `requestBody: required: true` em `Create` e `Update`, pela propriedade pública `Required` das variáveis de request do API Object, reaplicada também pelo writer de `List`, que recria essas variáveis em etapa posterior;
- `info.description` do contrato gerado deixa de expor a sentinela interna com IDs de backlog e passa a `REST API for <Transaction>, generated by Genexus Open API Builder.`; a sentinela de posse foi trocada sem lista de compatibilidade, então API gerada com a descrição anterior precisa ser apagada e regerada;
- `sdt_API_ErrorResponse` perde o nível `Errors`, que a geração nunca preenchia desde a retirada do `ErrorItem` em B071-B073/B079; o schema derivado `sdt_API_ErrorResponse.Errors_Error` deixa de aparecer no contrato público, e os documentos 12 e 27 receberam notas de revisão correspondentes;
- variável item das mensagens do Business Component em B071-B073/B079 passa a usar `Messages.Message, GeneXus.Common`, alinhando o preflight ao tipo GeneXus real;
- preenchimento detalhado de `ErrorResponse.Errors[]` em B071-B073/B079 foi retirado da geração atual porque a IDE rejeitou a validação da Procedure com `ErrorItem` de subestrutura SDT; o corpo de erro público permanece via `ErrorResponse.Code` e `ErrorResponse.Message`, e as mensagens do Business Component continuam registradas no Output técnico;
- preflight de B071-B073/B079 aceita `ErrorItem` de tentativas intermediárias próprias como migração conservadora quando o conjunto de variáveis bate e todas as demais variáveis continuam com tipo exato, permitindo regravar a Procedure para o contrato top-level atual;
- tela de preferências do wizard passa a exibir `listagem` e `metadata da API` nos defaults de geração e remove rótulos internos do usuário final;
- wizard remove IDs internos de backlog dos textos visíveis de confirmação, resumo e ajuda das abas, mantendo-os apenas nos logs técnicos;
- integridade B067 passa a aceitar `[Description]` seguida de anotações intermediárias, como `[RestMethod(POST)]`, antes da assinatura do serviço;
- integridade B067 passa a aceitar hashes de contrato planejado e de `ServiceGroupSource` esperado de variantes próprias anteriores com `RestPath` legado `{Chave}` ou anotações REST ainda incompletas, mantendo bloqueio para metadata externa ou contrato semântico divergente;
- parser de contrato B079 passa a exigir `Create` anotado como `[RestMethod(POST)]`, evitando aceitar API Object com verbo padrão incorreto;
- preflight de reexecução B071-B073/B079 passa a aceitar Sources próprios já gerados somente por equivalência canônica restrita, tolerando whitespace mas bloqueando código extra antes de sobrescrever;
- contrato B079 passa a expor `ErrorResponse` como `out` público em `Get`, `Create` e `Update`, mantendo a variante anterior com erro apenas interno como estado migrável.

- checkpoint preserva `B011` e `B012` antes de promover `B000`
- linha de corte do MVP passa a cobrir exaustivamente os itens necessários aos dez gates
- Sprints 3–7 distinguem ApiPlan, SDTs, Procedures/API/metadata, REST/segurança e operação conservadora
- referências de backlog, versões documentais e conflitos no wizard foram alinhadas
- layout inicial de `Src`, destino das evidências e ambiente-base de `B010` foram explicitados
- `Docs/Temp` foi protegido contra inclusão acidental no repositório público
- comandos experimentais B004 removidos do runtime após a validação do ciclo de vida do API Object
- comandos experimentais B005 removidos do runtime após a validação; o placeholder não operacional `Futura Primeira Opção` foi mantido apenas até o menu ganhar comandos permanentes do wizard
- comandos experimentais B006 removidos do runtime após a validação de persistência; a sonda permanece apenas como evidência histórica não invocada
- B031 limpa contrato em memória ao trocar a Transaction no B030 ou ao detectar ausência de seleção válida, evitando reutilização de decisões antigas por passos posteriores
- B031 desabilita partes da chave primária no `UpdateRequest`, preservando a regra de chave completa no `RestPath`
- B031 desabilita fórmulas em requests por API pública e bloqueia chave primária no `CreateRequest` até validação pública de autonumeração, sem reflexão em internos do SDK
- B032 sincroniza `Services base path` com `ApiName` até edição manual e o consumo por B033 foi validado posteriormente dentro do wizard único
- B090/B091 alinhados como representação provisória hardcoded em memória; os itens canônicos permanecem abertos até configuração explícita por KB em metadata persistente
- B040-B046 e B050-B053 agora executam preflight completo antes de qualquer `Save()`: SDTs validam colisões, descrições sentinela e tipos; Procedures validam todos os SDTs próprios/compatíveis e todas as Procedures planejadas antes de gravar a primeira
- Texto do wizard alinhado ao contrato provisório: preflight B040-B046/B050-B053 não promete validação de escopo físico enquanto ownership depender de descrição sentinela e metadata persistente ainda estiver pendente
- SDT writer ampliado para tipos públicos usados na validação composta (BITMAP, BINARY, BINARYFILE, VIDEO, AUDIO, GEOGRAPHY, GEOPOINT, GEOPOLYGON e GEOLINE)
- Correção pós-revisão B055 sincroniza API Object e Procedures: o Service Source parametrizado de Create/Update agora é acompanhado das variáveis reais do API Object, gravadas pela coleção pública de variáveis, com preflight do contrato antes das gravações
- Correção pós-revisão B055 preserva domínios nos SDTs próprios de request/response usando membros baseados nos atributos da Transaction; rerun em `GuiaPed` reconfigurou SDTs próprios e `Build All` passou com `apiGuiaPed`, `procGuiaPed_API_Create` e `procGuiaPed_API_Update` no U15
- Correção pré-push B055 força reconfiguração dos SDTs requeridos mesmo quando somente Business Component é aplicado, adia o realinhamento do Folder até depois do preflight principal e bloqueia Procedures B055 e API Object B055 reencontrados com variáveis extras, ausentes ou com tipo, atributo base, domínio ou objeto nomeado incompatível; Procedure já B055 sem variáveis não padrão também deixa de ser reparada silenciosamente
- Correção B056 faz B054 atualizar API Object B055 legado para a variante B055 com descrições, sem remover parâmetros de `Create`/`Update`, e mantém B054 legado atualizado para B054 com descrições
- Correção B060 alinha o preflight visual do wizard ao writer real para validar também `ownership.transactionGuid` e `ownership.apiGuid` antes de apresentar o File de metadata como reencontro válido
- Correção B060/B061 aceita reencontro de API Object B054 normalizado pela IDE com chamada qualificada de Procedure e grava o File de metadata no módulo da `Transaction`, validado em `SimulationResult` no módulo não-root `Entities`
- Rotina pré-push passa a executar o teste unitário do parser Service Source B054/B055 como gate automático
- Rotina pré-push passa a executar o teste unitário da integridade B067 como gate automático
- Correção B067 deixa o hash textual do Service Source como evidência e usa contrato semântico para bloqueio, preservando bloqueio de `[Description]` divergente
- Correção pré-push B070/B077 valida SDTs, Folder, Procedure, API Object e tipos planejados antes do primeiro `Save()` do `ApplyList`; depois reconfigura SDTs próprios mesmo sem a aba `SDTs` marcada e aceita reencontro semântico do API Object B070 para evitar falso bloqueio por normalização inofensiva do Service Source
- Cobertura automatizada pós-revisão B068 adicionada para serialização/parsing das preferências do wizard, escopo do preflight agregado por etapa e reencontro B070 quando somente literais de paginação mudam em Source próprio conhecido.
- Correção B068 remove gate indevido ao passar pela aba `Business Component`: navegar aba-a-aba não exige habilitar BC quando a etapa de aplicação via BC está desmarcada/bloqueada.
- Rotina pré-push passa a executar os testes unitários de preferências do wizard, escopo de preflight de escrita e política de reencontro B070.
- Parser de Service Source passa a validar o contrato B079 com Get parametrizado por `{&Chave}`, Create POST, Update PUT, `RestPath` explícito, `ErrorResponse` público, `RestStatusCode` interno e preservação do List B070 no mesmo API Object.
- Create/Update B079 passam a executar `Commit` após `Save()` bem-sucedido via Business Component e a validar membros obrigatórios comparando cada membro recebido com o valor default do mesmo membro em instância vazia do próprio SDT de request, sem comando C# embutido, retornando 400 antes do `Save()` quando o membro obrigatório chega ausente ou com o valor default do tipo (vazio, false ou 0).
- Rotina pré-push passa a executar o teste de coerência semântica de `Required`, que falha quando os textos do wizard, as mensagens de Output ou a documentação da frente voltam a descrever obrigatoriedade como presença de membro JSON, ou reduzem a limitação a uma formulação que omita `false` e `0`.
- Documentos de fundação reconciliados com a semântica efetivamente entregue em `B076`, cujo enunciado original tratava como problema único a distinção entre parâmetro ausente e valor vazio, `false` ou `0`. Nos filtros de `List` a distinção foi entregue conforme o enunciado, por `Json Null Serialization = JSON null`, e validada em runtime por `B070`/`B077`; no corpo de `Create` e `Update` ela é inviável sem comando `csharp`, e a geração passou a recusar com 400 o campo obrigatório não preenchido. O documento 06 recebeu nota de revisão separando os dois casos e preservando o enunciado original, os quatro caminhos descartados e a limitação assumida; o documento 24 e o gate transversal 6 dos documentos 09, 15 e 24 foram alinhados à mesma redação. A restrição de não expor campos públicos `Specified` no contrato permanece inalterada.
- Registro de decisões funcionais do MVP emendado pelo protocolo do próprio documento, que exige registrar nele as decisões revistas por validação técnica posterior: a `Emenda técnica — 2026-08-03` documenta o experimento previsto em `CreateRequest` e `UpdateRequest`, separa o caso resolvido dos filtros de `List` do caso revisto no corpo das requisições, e os trechos afetados receberam remissão apontando para ela, incluindo o gate técnico transversal 6.
- Duas divergências entre o contrato OpenAPI e o comportamento real foram caracterizadas como limitação do gerador GeneXus, com evidência registrada em `Docs/Implementation/2026-08-03-CONTRATO-OPENAPI-GAPS.md`: os códigos HTTP declarados por operação de API Object continuam restritos a `200` e `404`, porque o bloco de respostas é literal no template `Packages/RestDLTemplates/Swagger.Yaml.stg` da instalação; e o bloco `required:` dos schemas de request não é emitido mesmo com a propriedade `Required` gravada e persistida no item de SDT, o que foi comprovado por sonda temporária de releitura do modelo, removida antes do fechamento. Os documentos 12 e 27 registram as duas limitações e a marcação que produz efeito.
- `README.md` passa a documentar o requisito de ambiente para publicar a API gerada em IIS com o gerador .NET Framework: `PUT` não é entregue à aplicação enquanto o handler `ExtensionlessUrlHandler-Integrated-4.0` mantiver os verbos default do IIS, e a correção durável é no nó do servidor pelo IIS Manager, não no `web.config` do aplicativo gerado.

## Planned

- Confirmar suporte nativo simples para emitir `Location` no Create sem acoplamento frágil ao runtime gerado.

---

# [0.1.0] - 2026-04

## Added

- Criação oficial do projeto
- Definição de visão open source
- Coleção documental completa
- Estrutura base de diretórios
- Preparação documental para a futura fase de implementação

---

# Tipos de Mudança

- Added: nova funcionalidade
- Changed: alteração relevante
- Fixed: correção
- Removed: removido
- Deprecated: obsoleto
- Security: segurança

---

# Observação

Versões iniciais podem evoluir rapidamente durante a fase MVP.
