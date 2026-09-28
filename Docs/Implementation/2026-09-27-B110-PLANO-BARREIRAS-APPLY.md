# B110 — Plano detalhado de ação — v6

**Estado em 2026-09-27:** etapas 0 a 8 concluídas. A matriz IDE da etapa 7 foi aceita
com exceção de campo explícita para o diálogo do cenário 14, validado offline e não
observado na IDE por falta de diário interrompido. A etapa 8 registrou a evidência dedicada,
as remissões documentais e a rotina pré-push local. O fechamento e seus limites estão em
`2026-09-27-B110-VALIDACAO-IDE.md`; o checkpoint vigente está em
`../STATUS_ATUAL_E_PROXIMO_PASSO.md`. Push, tag e release não fazem parte do fechamento.

**Data:** 2026-09-27.
**Origem da v6:** parecer solo (Opus 5.5) sobre a v5, conferido contra o código e o git, e três
decisões do mantenedor em 2026-09-27, marcadas como `[v6-n]`. A v5 veio de parecer solo sobre a
v4 e de sete decisões do mantenedor (`[v5-n]`). As marcas `[v5-n]`, `[v4-n]` e `[v3-n]` foram
mantidas onde o conteúdo continua. Ajuste no mesmo arquivo, em 2026-09-27, por apontamento de
outro revisor conferido no código: descrição do `ValidateForF1` no fato 2.2.8 e separação entre
prova de interface e prova de backend na §6.2 (`[v6-4]`, `[v6-5]`).

> **Rótulo.** Esta v6 não passou por revisão por pares. Os pareceres que a originaram são
> solos, e vários são da mesma família de modelo — segunda opinião, não painel.

## 0. O que mudou

### 0.0 Da v5 para a v6

| Item | v5 | v6 |
|---|---|---|
| Data da schema V2 (fato 2.2.7) | `5a94037` (2026-09-14) | `80fcfb9` (2026-08-28); janela de V1 com e sem `errorDetail` de 2026-08-24 a 2026-08-28 `[v6-1]` |
| Renomeação como fluxo | «não verificado»; bloqueio da §3.6 com revogação pendente | não suportado: o preflight recusa por `GuidNameMismatch` as seleções que passam pelos ramos estritos do `ValidateForF1`; a §3.6 fecha antes do diário as demais (SDT/Procedure e API Object com List ou BC) e antecipa a recusa para a interface `[v6-2]` `[v6-4]` |
| Ordem de execução | ausente | §10 com etapas 0 a 8, commit local por etapa e uma rodada de IDE `[v6-3]` |
| Provas de bloqueio T1 na IDE | casos 2, 3, 8 e 12 exigiam relatório de Apply bloqueado | IDE prova interface e ausência de escrita; relatório de backend por leitura estática e teste offline da composição da mensagem `[v6-5]` |

### 0.1 Da v4 para a v5

| Item | v4 | v5 |
|---|---|---|
| Renomeação da API no Wizard | não tratada; T1 contornável | Apply bloqueia e a interface trava o nome quando há API existente com nome diferente `[v5-1]` |
| Escopo do preflight | só `RequireApiObject` para qualquer escritor | SDTs, Procedures e API para qualquer escritor; mensagem da cascata alcançável `[v5-2]` |
| Padrões pós-B115 | «padrões das preferências» | padrões fixos do leitor (preferências não se aplicam com API existente) `[v5-3]` |
| Gatilho T2 | `recovery.imported` ou seção garantida pela `schemaVersion` | somente `recovery.imported` `[v5-4]` |
| Premissa da §4.5 | «pode reescrever como plana» | reinclui todos os subníveis com campos padrão; bloqueio mantido; caso adjacente registrado `[v5-5]` |
| Testes | sem registro no pré-push; âncoras não citadas | registro no orquestrador e âncoras de `Test-PrototypeWizardExistingApiFilters.ps1` `[v5-6]` |
| Ajustes | — | construtor do Sync; «Gerar metadata» no caso 7; item de metadata obsoleta; acknowledgement sem hash declarado `[v5-7]` |

### 0.2 Da v3 para a v4 (mantido para histórico)

| Item | v3 | v4 |
|---|---|---|
| Medição pós-B115 | fato 2.2.4 como previsão estática; §4.5 bloqueia «até haver medição» | F3 §13.6 citado: medido em 2026-09-06 na `Teste` de quatro níveis; reconfirmar com a DLL vigente; critério de revogação objetivo `[v4-1]` |
| Incidentes | um só, «2026-09-04/05, `MetadataMissing`» | dois: 04/09 `BaselineServiceSourceHashMismatch` (metadata íntegra) e 05/09 `MetadataMissing` `[v4-2]` |
| Perda de filtros em 04/09 | explicada pelo fallback sem metadata | precedência do leitor (Source antes da metadata), corrigida em `0902455` `[v4-2]` |
| «Confirmação barata» por leitura de `apiEmpresa` | proposta | retirada: objetos regravados em §10.7 `[v4-2]` |
| Estado de 04/09 na matriz | ausente | linha própria, saída pelo Remover `[v4-2]` |
| Proveniência | teste «conforme `recovery.notRecovered`»; sem `errorDetail` | cálculo por presença de chave; `errorDetail` como `Default`; caso negativo `[v4-3]` |
| Conjunto confirmado | recalculado pelas edições na interface e pela KB no Apply | sempre o da KB; edição é marca informativa `[v4-4]` |
| Mensagens de recuperação | fora da auditoria | `ApiPlanRecoveryRehydrator` e `RecoveryConfirmDiscard` auditados, texto condicionado ao estado `[v4-5]` |
| Visibilidade de T2 | «desde a `alpha.9`» | desde a `v0.1.0-alpha.8`; entrada `Planned` do CHANGELOG revisada `[v4-6]` |
| Ordem da interface | só o handler `CheckedChanged` | condição de saúde antes do cálculo de disponibilidade; transições na IDE `[v4-7]` |
| Redação | §9 sem escopo T2; §3.1 absoluta; retorno da §3.3 sem colisões | corrigidos `[v4-8]` |

### 0.3 Da v2 para a v3 (mantido para histórico)

| Item | v2 | v3 |
|---|---|---|
| Alcance do defeito | só API Object **bloqueado** | dois gatilhos: API bloqueado (T1) **e** contrato existente reconstruído por fallback com API apto (T2) |
| Cascata do estado de API | não declarada | declarada e aceita (§3.2) |
| Ramo que registra bloqueio e segue | não mencionado | corrigido para retornar (§3.3) |
| Orientação «apague API + File» | destino ambíguo | preservada como saída medida (§5) |
| Prova de «Apply bloqueia vindo de outro chamador» | item de IDE | prova estática + teste de escopo; IDE prova UI e ausência de escrita (§6) |
| Pendências de leitura estática | adiadas | fechadas (§2.3) |
| Local do documento | `Temp/` | promoção a `Docs/Implementation/` antes de implementar (§8) |

## 1. Objetivo

B110 deve impedir que o Apply grave a partir de uma intenção derivada de estado incompleto
sem que isso seja **bloqueado** ou **explicitamente aceito** pelo usuário.

Há dois incidentes medidos, com causas distintas `[v4-2]`:

- **2026-09-04** (`EVIDENCIA-IDE-DRIFT-API-OBJECT.md` §6–§8): cancelamento durante a etapa de
  Business Component deixou o API Object com `List()` sem parâmetros e a metadata íntegra no
  baseline anterior. O API bloqueou por `BaselineServiceSourceHashMismatch`; o plano perdeu
  filtros; SDT e quatro Procedures foram gravados com `SuccessWithWarnings` e `Bloqueados=0`.
- **2026-09-05** (`B111-SONDAS-IDENTIDADE-E-DIARIO.md` §10.1–§10.2): a etapa List falhou e a
  metadata não foi gravada. O API bloqueou por `MetadataMissing`; mesmo resultado de gravação e
  de relatório.

E um terceiro caminho, já exercido em campo sem aviso `[v4-1]`: em 2026-09-06, depois da
recuperação B115, o Wizard abriu destravado e um Apply completo gravou metadata completa a
partir de contrato reconstruído (F3 §13.6).

O defeito tem dois gatilhos, tratados de forma distinta:

- **T1 — API Object existente bloqueado.** Barreira fail-closed: nenhuma etapa de escrita do
  Apply é autorizada. Interface desabilita as opções; o Apply bloqueia antes do diário. Inclui
  a renomeação da API existente no Wizard (§3.6) `[v5-1]`.
- **T2 — API Object apto, mas metadata produzida pela recuperação B115** (`recovery.imported`)
  `[v5-4]`. O Wizard mostra quais seções do contrato vieram do fallback ou dos padrões e exige
  uma confirmação específica antes de qualquer escrita; o Apply revalida essa confirmação antes
  do diário; o relatório registra as seções reconstruídas. Sem confirmação, nenhuma escrita.

Preservar: primeira geração com API ausente (`Create`), sujeita à cascata da §3.2; seleção sem
etapas de escrita; Sync (caminho próprio, já fail-closed e já recusando metadata importada); e
a ordem F1/B111. A decisão F3 §13.5.1 («`Apply` completo pode substituir sua marca») continua
válida: T2 não a revoga, apenas exige que esse Apply seja consciente.

## 2. Base factual

### 2.1 Fatos medidos

- Na abertura deste plano, B110 estava aberto no backlog; o fechamento em 2026-09-27 está
  registrado no estado acima e no documento de evidência dedicado.
- **04/09** `[v4-2]`: API bloqueado por `BaselineServiceSourceHashMismatch` (EVIDENCIA §7.1),
  metadata íntegra (`FingerprintOk=True`). `ListFilters` de 1 para 0 no plano; SDT e quatro
  Procedures gravados; `SuccessWithWarnings`, `Bloqueados=0` (EVIDENCIA §7.2). No mesmo dia o
  Remover apagou os 50 objetos (EVIDENCIA §8.1): o estado persistido desse incidente **não foi
  inspecionado e não é mais inspecionável**.
- **05/09**: API bloqueado por `MetadataMissing`; `ListFilters` de 1 para 0; SDT e quatro
  Procedures gravados; `SuccessWithWarnings`, `Bloqueados=0` (SONDAS §10.2). Inspeção na IDE:
  `EmpresaId` continua no SDT persistido. Em seguida, o API foi apagado à mão e recriado
  (`353b6269-…`), e as Procedures foram regravadas com o plano íntegro (SONDAS §10.7).
- **06/09** `[v4-1]`: F3 §13.6, KB `wsEducacaoSpTeste`, Transaction `Teste` (quatro níveis).
  B115 aceito (metadata de 2290 bytes, 18 SDTs próprios); Sync bloqueou; Wizard reaberto
  **destravado** («teste de reencontro»); Apply completo `SuccessWithWarnings`, `Criados=0`,
  `Atualizados=7`, `Bloqueados=0`; metadata reescrita com 117926 bytes no mesmo File. O
  registro não diz que hierarquia o Wizard montou nem o que houve com os objetos de subnível.
  O `PlannedContractHash` igual ao de antes do experimento (F3 §13.7, «Fechamento do ciclo»)
  não distingue contrato restaurado de contrato de padrões, porque o registro não diz se a
  `Teste` tinha valores fora dos padrões. A DLL era anterior à `v0.1.0-alpha.8`: pela regra da
  data do gerador, a medição vale para a data em que foi feita, não como estado atual.
- A oferta B115 depende da preferência `offerOrphanMetadataRecovery`, cujo padrão é `false`.
- Contradições documentais a corrigir por nota datada: `EVIDENCIA-IDE-DRIFT-API-OBJECT.md` §7.2
  item 3 e §8 (SDT regravado «sem o membro»); `B111-SONDAS-IDENTIDADE-E-DIARIO.md` §10.4 (ainda
  descreve o SDT sem o membro e as Procedures regravadas); título e descrição do B110 no
  backlog («corrupção»). Ver §7.2 para o conteúdo de cada nota.

### 2.2 Fatos estáticos (leitura de código em 2026-09-27)

1. **O Source do API gerado antes da etapa List não tem parâmetros de filtro.**
   `ApiPlanBusinessComponentWriter.ServiceSource` emite `List()` sem parâmetros nas variantes
   B054/B055; só `ApiPlanListProcedureWriter.CreateB070ServiceGroupSource` (etapa List)
   acrescenta os filtros. `PrototypeWizardExistingApiContractReader.ReadApiFilters` só
   reconhece filtro por parâmetro `in:&<Atributo>` (ou pares `From/To`, `Min/Max`) no `List`, e
   o leitor marca os filtros como disponíveis sempre que o `List` está declarado.
2. **Dois mecanismos de perda de filtros** `[v4-2]`:
   - **04/09 — precedência do leitor, já corrigida.** Até `0902455` (2026-09-09), o leitor usava
     os filtros do Source sempre que o `List` estava declarado, mesmo com metadata presente. Com
     `List()` sem parâmetros, o plano saía com zero filtros apesar de `fields.listFilters` na
     metadata. `0902455` inverteu a precedência (metadata autoritativa, Source como fallback) e
     está publicado desde a `v0.1.0-alpha.8`. Com metadata presente, essa variante não degrada
     mais o plano; o bloqueio do API e a continuação das gravações seguem sendo B110.
   - **05/09 — fallback sem metadata (inferência).** A etapa List falhou (B109) e a metadata não
     foi gravada. O API ficou com Source B054/B055, sem filtros; na reabertura, sem metadata, o
     leitor leu o Source e obteve zero filtros. Isso concilia o incidente com F3 §13.3
     («medido em 2026-09-06: o reader reconstrói … filtros»): a reconstrução funciona **quando
     a etapa List chegou a gravar o Source**. Confirmação: teste offline de `ReadApiFilters`
     (§6.1). Não há confirmação por leitura da KB: o API e as Procedures do incidente foram
     regravados em SONDAS §10.7.
3. **Seções que a metadata B115 não traz e o que as substitui** `[v4-3]` `[v5-3]`.
   `CreateRecoveryJson` grava só `schemaVersion`, `generator`, `generatedAtUtc`, `ownership`,
   `objects` e `recovery`. `recovery.notRecovered` lista seis seções (`fields`, `pagination`,
   `order`, `services`, `levels`, `transactionStructure`), mas também ficam ausentes `api`,
   `security`, `descriptions` e `errorDetail`. Paginação, ordenação, obrigatórios,
   `servicesBasePath`, hierarquia e `errorDetail.includeBusinessComponentMessages` não têm
   fallback. **As preferências do usuário não se aplicam nesse caso:**
   `PrototypeWizardDialog.ApplyWizardPreferences` só as aplica quando
   `ExistingApiContract.HasExistingApi` é falso, e depois da B115 a API existe. Valem os
   **padrões fixos do leitor e do diálogo**:
   - paginação `50`/`200` (valores literais no carregamento do snapshot);
   - ordenação estática vazia, que o writer de List completa com a chave primária em ordem
     ascendente;
   - `servicesBasePath` igual ao nome da API;
   - `includeBusinessComponentMessages = true`;
   - obrigatórios pelo padrão por atributo do Wizard (`PrototypeWizardDialog.DefaultCreateRequired`:
     sensível nunca, chave primária conforme a regra própria), não os persistidos;
   - hierarquia conforme §4.5.
4. **Depois da B115, o API Object aparece apto — medido em 2026-09-06, reconfirmar com a DLL
   vigente** `[v4-1]`. A metadata B115 não tem `fingerprint` nem `integrity`.
   `ApiPlanMetadataIntegrity.DiagnoseMetadataFingerprint` devolve `AbsentAccepted` sem
   fingerprint; `ApiPlanApiObjectWriter.DiagnoseIntentionalChangeOwnership` devolve
   `isOwned=true` quando `integrity` falta e a posse confere; a etapa de metadata passa por
   `HasCompatibleGeneratedBaseline`, que também aceita a ausência de `integrity`. Logo
   `apiState.IsBlocked == false`, e uma guarda baseada só em T1 não alcança esse caminho. F3
   §13.6 registrou exatamente esse resultado na IDE.
5. **O caminho pós-B115 é o recomendado pelo próprio produto.** F3 §13.4–§13.5.1: «um
   `Wizard` + Apply completo reescreve a metadata inteira e a marca desaparece». A B115 é
   elegível justamente depois de aplicação parcial. Portanto o Apply pós-B115 grava metadata
   **completa** com filtros possivelmente zerados (fatos 1 e 2) e as seções do fato 3 nos
   padrões fixos, tornando persistente uma perda que nos incidentes ficou só em memória.
6. **Visibilidade publicada** `[v4-6]`. A marca `recovery.imported` entrou em `40b982b`
   (2026-09-06). `git merge-base --is-ancestor 40b982b v0.1.0-alpha.7` falha e o mesmo comando
   contra `v0.1.0-alpha.8` passa: o caminho de T2 está publicado desde a
   **`v0.1.0-alpha.8` (2026-09-17)**, ainda que dependente de preferência desligada por padrão.
7. **A metadata V1 não tem conjunto fixo de seções** `[v5-4]`. `errorDetail` entrou no writer
   em `424bfdc` (2026-08-24), e a schema V2 surgiu em `80fcfb9` (2026-08-28, «B099b: metadata
   hierárquica V2»; conferência: `git log --reverse -S "GOAB_API_METADATA_B060_V2" -- Src`)
   `[v6-1]`. Existe, portanto, metadata V1 legítima com e sem `errorDetail`: sem, até 2026-08-24;
   com, entre 2026-08-24 e 2026-08-28. Fora da B115, nenhum writer do produto grava
   metadata sem seções; metadata incompleta só vem de V1 legada ou de edição manual.
8. **Nome da API editável e nome da metadata fixo** `[v5-1]`. `ApiPlan.ApiName` vem de
   `PrototypeWizardReviewSelection.ApiName`, campo editável no Wizard; o estado da API
   (`ApiPlanGenerationStateReader.InspectApiObject`) procura por esse nome.
   `ApiPlanNames.Create` deriva o nome do File de metadata da Transaction, não do nome da API; e
   `PrototypeWizardExistingApiContractReader.ResolveApiObject` acha a API existente pelo GUID da
   metadata, pelo nome registrado nela ou pelo nome convencional, e lê o Source dela.
   **Inferência:** em estado `MetadataMissing` ou `BaselineServiceSourceHashMismatch`, renomear
   a API no Wizard faz o estado virar `Create`, e um Apply só de SDT/Procedure grava a partir do
   contrato lido da API antiga, sem T1.

   **O preflight já recusa a renomeação em parte das seleções** `[v6-2]` `[v6-4]` (leitura
   estática). `ApiPlanBuilder.Build` grava `PlannedApiGuid` com o GUID da API existente, mesmo
   com o nome alterado. `ValidateForF1` tem dois ramos estritos:
   - API Object sem List e sem BC → `PreflightB054ApiObjectStrict`;
   - sem API Object, com metadata, List ou BC → `PreflightExistingApiObjectStrict`.

   Os dois chegam a `RequirePlannedApiObject`, onde `ApiPlanMainObjectResolver.Resolve`
   classifica o par (GUID antigo, nome novo) como `GuidNameMismatch` e lança exceção no
   preflight, antes do diário. **Não passam por nenhum dos dois ramos:** a seleção só de SDT e
   Procedure, e a seleção de API Object **junto de** List ou BC. Nesta última, se houver recusa,
   ela acontece depois, dentro dos writers — não verificado. Renomear API gerada não é fluxo
   suportado: onde o preflight alcança, ele recusa; onde não alcança, nada garante o resultado.

### 2.3 Pendências da v2 fechadas por leitura estática `[v3-7]`

- `ApiPlanWritePreflightScope.FromSelection` tem um único chamador, o Apply do Wizard em
  `Package` (bloco que monta `preflightScope`). O Sync usa `ApiPlanWritePreflight.ValidateForSync`.
- Falha de preflight registra `report.AddBlocked("Preflight", "B063/B064/B067", …)` e mostra o
  relatório final sem abrir diário.
- Nenhuma escrita ocorre antes de `ApiPlanOperationJournalSession.Start`: entre a leitura de estado
  e o início do diário só há relatório, log e validação.
- `ApiPlanWritePreflight.ValidateForIntentionalChange` relê o estado com
  `forSyncContractRefresh: true`, a mesma variante da leitura inicial do Apply e da interface;
  incluir API Object no escopo produz bloqueio efetivo para `MetadataMissing` e para
  `BaselineServiceSourceHashMismatch` (cláusula de posse vira conflito em `CreateState`).

**Atualização de 2026-09-27 (`a7ac102`).** O segundo item desta lista, sobre o código genérico
`B063/B064/B067`, registra a leitura estática da v2. No Apply atual, as recusas T2 e de
renomeação registram `B110` no relatório e na Output; as demais falhas do preflight agregado
mantêm `B063/B064/B067`. Ver o complemento em
[`2026-09-27-B110-VALIDACAO-IDE.md`](2026-09-27-B110-VALIDACAO-IDE.md).

### 2.4 Perguntas de campo

- **Não demonstrável pelos artefatos disponíveis** `[v4-2]`: o Source persistido das
  Procedures e o estado do SDT gravados nos incidentes. Os objetos de 04/09 foram removidos
  (EVIDENCIA §8.1); os de 05/09 foram regravados com o plano íntegro (SONDAS §10.7). Fica
  registrado assim, sem pendência aberta.
- Fato 2.2.4: reconfirmar com a DLL vigente (§6.2, caso 7).
- Fato 2.2.8, inferência da renomeação: não é confirmável na IDE depois da correção, porque a
  §3.6 deixa o nome somente leitura; o caso 15 da §6.2 prova a trava, não a inferência. Fica como
  leitura estática, e a regra da §3.6 é provada offline (§6.1) `[v6-5]`.
- Hierarquia sob T2: ver §4.5.
- Renomear API gerada: não suportado — ver fato 2.2.8 para as seleções que o preflight já recusa
  e para as que não passam pelos ramos estritos, cujo resultado não foi verificado `[v6-2]`
  `[v6-4]`.

## 3. T1 — Barreira fail-closed para API bloqueado

### 3.1 Escopo do preflight `[v5-2]`

Em `ApiPlanWritePreflightScope.FromSelection`, **qualquer** escritor selecionado — SDTs,
Procedures, API Object, metadata, List ou Business Component — liga `RequireSdts`,
`RequireProcedures` e `RequireApiObject`. `RequireMetadataFile` continua dependendo só de
«Gerar metadata». Semântica documentada junto ao método: «SDTs, Procedures e API precisam estar
saudáveis para autorizar qualquer escrita do Apply», não «esses objetos serão escritos».

O efeito de bloqueio é o mesmo que a cascata da §3.2 já produziria; o que muda é a mensagem: com
`RequireProcedures` ligado, `CollectCollisionConflicts` coleta as colisões de Procedure e
`SelectBlockedStageNames` nomeia a etapa Procedures, de modo que um Apply só de SDT informa a
Procedure causadora em vez de «o estado dos SDTs ou Procedures precisa ser resolvido antes».
Não duplicar guarda equivalente em `Package`.

API ausente continua `Create` (não bloqueado) **desde que SDTs e Procedures não estejam
bloqueados** `[v4-8]`: uma Procedure externa com o nome planejado bloqueia o Apply mesmo sem API
existente, inclusive uma primeira geração só de SDTs.

### 3.2 Cascata declarada `[v3-3]`

O estado de API herda bloqueio de SDTs e Procedures (`ApiPlanGenerationStateReader`: API
bloqueado quando SDTs ou Procedures estão bloqueados). Com a §3.1, um Apply só de SDT passa a ser
bloqueado também por conflito em Procedure, com API existente ou ausente. Decisão: **aceitar**
(fail-closed; um SDT gravado sem Procedures e API utilizáveis não completa nada). Declarar no
comentário do método, no documento de evidência e no CHANGELOG. A mensagem cita a causa de
origem (Procedures), o que a §3.1 torna alcançável `[v5-2]`.

### 3.3 Ramo que registra bloqueio e segue `[v3-4]`

No Apply, logo depois de montar `preflightScope`, o bloco `blockedGenerationStages` registra
«Nenhum Save foi solicitado» e **não retorna**; quem bloqueia de fato é o preflight agregado
adiante. Corrigir para retornar pelo mesmo caminho de bloqueio (`AddBlocked` + relatório final,
sem diário), mantendo `ValidateForIntentionalChange` como segunda verificação. Assim o log deixa
de afirmar algo que o código não garante.

Requisitos do retorno antecipado `[v4-8]`:

- levar ao relatório as colisões do escopo e o detalhe de cada etapa bloqueada, como
  `BuildBlockedMessage` já faz no preflight — hoje `AppendCollisionConflictsToReport` só roda
  adiante, depois desse ponto;
- ficar **depois** do teste de seleção sem etapa de escrita, para que uma seleção vazia continue
  retornando sem bloqueio artificial (§6.2, caso 6).

Os avisos «Etapa … bloqueada na KB» acrescentados ao relatório para **todas** as etapas
bloqueadas (fora do escopo) são a origem do `SuccessWithWarnings` com `Bloqueados=0`. Com T1,
toda etapa bloqueada relevante passa a gerar `AddBlocked` antes; manter os avisos apenas para
etapas fora do escopo que não impedem a escrita, se ainda existirem.

### 3.4 Interface `[v4-7]`

Em `PrototypeWizardDialog.ApplyGenerationPreviewState`, desabilitar e desmarcar
`_generateSdtsCheck` e `_generateProceduresCheck` quando `apiState.IsBlocked`, exibindo a causa.
Não tratar `Create` como bloqueado.

Ordem de avaliação: hoje o método calcula `sdtsAvailable`, `proceduresAvailable` e
`baseApiObjectAvailable` por `IsDependencyAvailable` — que devolve verdadeiro para caixa
**marcada** sem olhar `IsBlocked` — e os passa a `ApplyBusinessComponentControlState` antes de
`ApplyGenerationControlState` desmarcar as caixas bloqueadas. Corrigir:

1. aplicar a condição de saúde (T1, renomeação da §3.6 e T2 sem confirmação) **antes** do
   cálculo de disponibilidade: primeiro desabilitar e desmarcar, depois calcular dependências,
   BC e List;
2. fazer `IsDependencyAvailable` considerar `IsBlocked`, ou garantir o mesmo efeito pela ordem
   do item 1;
3. centralizar em `ApplyGenerationPreviewState` a regra que hoje também vive no
   `CheckedChanged` de `_generateSdtsCheck` (em `WireGenerationConfirmation`), que escreve
   `_generateProceduresCheck.Enabled` `[v3-8]`.

Corrigir o comentário B115 no ponto em que `OfferOrphanMetadataRecoveryIfEnabled` é chamado em
`Package`, que afirma que Cancelar é a única ação disponível.

### 3.5 Testes de escopo (offline) `[v5-2]`

Em `Tests/WritePreflight/Test-ApiPlanWritePreflightScope.ps1`:

- só SDTs → `RequireSdts`, `RequireProcedures` e `RequireApiObject` verdadeiros;
  `RequireMetadataFile` falso;
- só Procedures → os três verdadeiros; `RequireMetadataFile` falso;
- SDTs + Procedures → os três verdadeiros;
- só SDTs com todas as etapas bloqueadas → `SelectBlockedStageNames` devolve SDTs, Procedures e
  API Object;
- casos existentes (List, metadata, BC, nenhuma etapa) inalterados.

O caso «`Create` apto permite primeira geração» **não** pertence a esse teste, que só recebe
booleanos `[v3-7]`: ele é do leitor de estado e fica provado na IDE (§6.2, casos 5 e 12) e por
leitura de `InspectApiObject` (API ausente → `Missing`, não conflito).

### 3.6 Renomeação da API existente `[v5-1]`

Quando `PrototypeWizardExistingApiContractReader` resolve uma API existente
(`HasExistingApi` com `ApiGuid` presente) e o nome planejado (`ApiPlan.ApiName`) difere do nome
dessa API, sem distinção de maiúsculas:

- **Apply:** bloqueia com `AddBlocked` antes do diário, com mensagem própria que cita o nome
  existente e o planejado. A verificação usa a mesma leitura de contrato de
  `ApiPlanBuilder.Build`, não o estado por nome.
- **Interface:** o campo de nome da API fica somente leitura enquanto houver API existente
  resolvida, com a explicação ao lado.
- **Alcance** `[v6-2]` `[v6-4]`: o bloqueio não cria restrição de produto nova. Ele estende a
  todas as seleções de escrita — inclusive só SDT/Procedure e API Object com List ou BC, que os
  ramos estritos do `ValidateForF1` não cobrem — a recusa por `GuidNameMismatch` que o preflight
  já aplica nas demais (fato 2.2.8), sempre antes do diário, e a antecipa para a interface, com
  mensagem legível. Não há o que revogar sem mudar os writers, o que fica fora do B110.

Primeira geração (sem API existente resolvida) continua com o nome livre.

## 4. T2 — Metadata da recuperação B115 (decisão B)

### 4.1 Proveniência do contrato `[v4-3]`

`PrototypeWizardExistingApiContractReader` passa a produzir, além do contrato, a
**proveniência** de cada seção:

| Seção | Fontes possíveis |
|---|---|
| serviços, descrições, SecurityLevel, `api.restPath` | metadata · Source do API |
| campos Create/Update/Response | metadata · SDTs próprios |
| `ListFilters` | metadata · Source do API (só se a etapa List gravou) |
| obrigatórios, paginação, ordenação, `servicesBasePath`, hierarquia | metadata · **padrão fixo** (fato 2.2.3) `[v5-3]` |
| `errorDetail.includeBusinessComponentMessages` | metadata · **padrão fixo** (`true`) |

Cada seção recebe `Metadata`, `Fallback` ou `Default`. A proveniência se calcula pela
**presença de cada chave** no `JObject` da metadata e pelo texto do Source — nunca pela lista
`recovery.notRecovered`, que fica apenas informativa e não cobre `api`, `security`,
`descriptions` nem `errorDetail`. O cálculo deve ser extraído para função pura (sem SDK),
testável offline como os demais testes que já carregam Newtonsoft.Json, respeitando as âncoras
da §6.1 `[v5-6]`.

A proveniência serve ao **painel** e ao **relatório**; **não** é gatilho de T2 (§4.2) `[v5-4]`.

Com API ausente (primeira geração), os campos podem vir de SDTs próprios remanescentes
(`Fallback`); isso não dispara T2.

### 4.2 Quando T2 dispara `[v5-4]`

- **Dispara somente** com metadata cuja chave `recovery.imported` seja `true`.
- **Não dispara:** metadata sem a marca, em qualquer versão de schema — inclusive V1 legada sem
  `errorDetail` ou outras seções (fato 2.2.7) —, API plana, metadata ausente com API existente
  (`MetadataMissing`, coberto por T1) e API ausente (primeira geração).
- Chave `levels` **ausente** e chave `levels` com valor **nulo** continuam casos distintos para
  a §4.5 `[v4-8]`: `ApiPlanMetadataLevelsCodec.CreateLevelsToken` devolve nulo quando não há
  níveis filhos, e o writer grava a chave com valor nulo; a metadata B115 não grava a chave.

Metadata incompleta sem a marca (V1 legada, edição manual) fica fora de T2 por decisão, não por
ser segura: o mantenedor escolheu não gerar confirmação espúria em metadata legada válida.

### 4.3 Interface `[v4-4]`

Quando T2 dispara, o Wizard mostra um painel próprio, antes das caixas de geração, com:

- a lista de seções `Fallback` e `Default` calculada pela KB, com o valor que será usado (por
  exemplo, «Filtros de List: 0, reconstruídos do Source do API»; «Paginação: 50/200, padrão
  fixo do Wizard — suas preferências não se aplicam a API existente») `[v5-3]`;
- em cada linha que o usuário alterar no Wizard, a marca informativa «alterado nesta execução»;
  a edição **não** retira a seção da lista;
- **quando «Gerar metadata» estiver marcado**, o aviso de que, ao concluir, esses valores serão
  gravados na metadata, a marca `recovery.imported` desaparecerá e eles passarão a ser o
  contrato de referência do Sincronizar; **quando não estiver**, o aviso de que a marca continua
  e o painel voltará a aparecer na próxima abertura `[v5-7]`;
- uma caixa de confirmação específica, desmarcada por padrão.

Enquanto a caixa não estiver marcada, todas as caixas de escrita ficam desabilitadas, pela
mesma ordem de avaliação da §3.4.

### 4.4 Apply (autoridade) `[v4-4]`

A seleção passa a carregar `ReconstructedContractAcknowledgement` com o conjunto de seções
confirmadas — o conjunto calculado pela KB, independente das edições. Antes do diário, o Apply
recalcula a proveniência a partir da KB (a mesma leitura de `ApiPlanBuilder.Build`) e bloqueia
com `AddBlocked` se T2 disparar e: não houver confirmação; ou o conjunto confirmado divergir do
recalculado. A divergência só acontece se a KB mudou entre a abertura do diálogo e o Apply, ou
se a interface foi contornada. Seleção sem escrita continua sem bloqueio.

**Escolha declarada** `[v5-7]`: a confirmação fica amarrada ao **conjunto de seções**, sem hash
dos valores reconstruídos. O diálogo é modal e o recálculo só diverge se a KB mudar no meio; o
hash acrescentaria custo sem proteção proporcional. Não reintroduzir o hash na implementação sem
nova decisão.

**Construtores da seleção** `[v5-7]`: `PrototypeWizardFlowSelection` é construído em
`PrototypeWizardDialog` e também em `ApiPlanTransactionSyncOrchestrator`. No caminho do Sync, o
campo novo recebe valor neutro (sem confirmação); o Sync já recusa metadata importada antes de
qualquer escrita, então T2 não se aplica a ele.

Relatório: com confirmação, `SuccessWithWarnings` é honesto, desde que liste cada seção
reconstruída, indique entre elas as alteradas pelo usuário e registre «contrato reconstruído
confirmado pelo usuário». O que B110 proíbe é o sucesso **silencioso**, não o sucesso com
aceite explícito.

### 4.5 Hierarquia — decidido: bloquear `[v4-1]` `[v5-5]`

Se a Transaction tem sublevels e a metadata sob T2 não tem a chave `levels`, o Wizard não
achata a API: `PrototypeWizardDialog.TryLoadHierarchicalSelection` chama
`ApiPlanHierarchicalWizardSelection.CreateDefault` e só aplica a poda quando há raiz persistida.
Sem ela, **todos os subníveis voltam incluídos, com campos padrão, sem a poda anterior**. O que
o Apply faz, a partir dessa seleção, com os objetos de subnível já gravados não foi medido.

**Decisão do mantenedor em 2026-09-27, confirmada na revisão da v3 e mantida na v5:** nesse
caso, T2 **bloqueia** em vez de pedir confirmação. O bloqueio segue o mesmo caminho de T1 no
Apply (`AddBlocked` antes do diário) e, na interface, o painel T2 mostra a causa sem oferecer a
caixa de confirmação. A mensagem indica a saída: **Remover API gerada**, que funciona sobre
metadata B115 (F3 §13.7: `Success`, `Removidos=24`), seguido de nova geração pelo Wizard.

Evidência anterior: F3 §13.6 exerceu exatamente esse cenário em 2026-09-06 (`Teste`, quatro
níveis) e terminou sem erro, mas não registrou a hierarquia montada nem o estado dos subníveis,
e a DLL é anterior à `v0.1.0-alpha.8`. Ela prova que o caminho roda, não que a hierarquia
sobrevive.

**Critério de revogação.** Repetir o §13.6 na `Teste` com a DLL vigente, sobre uma fixture com
valores diferentes dos padrões fixos do fato 2.2.3 (filtros conhecidos, obrigatórios, ordenação
diferente da PK, paginação diferente de 50/200, `servicesBasePath` diferente do nome da API,
`includeBusinessComponentMessages=false`) e com ao menos um subnível podado, registrando: a
hierarquia que o Wizard monta sem `levels`; o `PlannedContractHash` antes e depois; e o estado
de cada SDT e Procedure de subnível depois do Apply. Revogar o bloqueio exige essa medição e
nova decisão registrada.

**Caso adjacente, fora do escopo** `[v5-5]`: com metadata completa (sem a marca B115), se o
usuário podou todos os subníveis, a raiz do plano fica sem filhos, `CreateLevelsToken` devolve
nulo e a metadata grava `levels: null`. Na reabertura, sem raiz persistida, os subníveis
voltariam todos incluídos, em silêncio, e T2 não dispara. É inferência de leitura; registrado
como item próprio na §8.

## 5. Matriz de diagnóstico e mensagens `[v3-5]`

| Estado | Direção |
|---|---|
| `MetadataMissing`, B115 elegível, preferência ligada | Oferta B115 informa o que grava e o que **não** recupera; diz que o Wizard seguinte vai pedir a confirmação T2 e listar o que voltará aos padrões fixos — ou, com Transaction hierárquica, que vai bloquear e indicar o Remover (§4.5). Remover segue como escolha separada. |
| `MetadataMissing`, B115 elegível, preferência desligada (padrão) | Citar a preferência «Oferecer recuperação de metadata órfã no Wizard» pelo nome; manter a saída medida «apague o API Object e o File, os dois» de `DescribeApiObjectCause`. Não mudar a preferência. |
| `MetadataMissing`, B115 inelegível | Manter a saída medida de `DescribeApiObjectCause` (medição de 2026-09-14) e acrescentar o motivo de inelegibilidade quando houver. |
| Metadata íntegra, API fora do baseline (`BaselineServiceSourceHashMismatch`) — estado de 04/09 `[v4-2]` | Hoje `DescribeApiObjectCause` não dá orientação. Indicar **Remover API gerada**, que repara a partir desse estado com a metadata presente (EVIDENCIA §8.1), seguido de nova geração. |
| Metadata completa com API GUID divergente | Manter a saída medida de `DescribeApiObjectCause`; não prometer B115. |
| Metadata `recovery.imported`, API trocado | Fluxo próprio da B115; validar mensagem separadamente. |
| Metadata `recovery.imported`, API próprio, Transaction plana | T2: painel de contrato reconstruído. |
| Metadata `recovery.imported`, API próprio, Transaction hierárquica sem `levels` `[v4-1]` | T2 bloqueado: causa e saída pelo Remover (§4.5). |
| API existente resolvida com nome planejado diferente `[v5-1]` | Bloqueio da §3.6: citar o nome existente e o planejado; orientar a manter o nome existente. A mensagem substitui o diagnóstico técnico `GuidNameMismatch` que o preflight mostra hoje nas seleções cobertas pelos ramos estritos do `ValidateForF1` `[v6-2]` `[v6-4]`. |
| API removido fora da ferramenta / `DescribeRemovalGuidance` | Preservar orientação específica. |
| Registro encerrado por `Recuperar operação interrompida` `[v4-5]` | Não prometer «reaplicar pelo Wizard» nem «remover» de forma genérica: remeter à direção da linha correspondente desta matriz; se o estado não for conhecido na emissão, dizer que o Wizard indicará a saída ao abrir. |

Auditar os emissores antes de editar: `ApiPlanGenerationStateReader.DescribeApiObjectCause`,
`TryDescribeMetadataApiGuidMismatch`, `ApiPlanMetadataFileWriter.RequireApiGuid`,
`ApiPlanGeneratedApiRemovalPlan`, `Package.DescribeRemovalGuidance`,
`ApiPlanRecoveryRehydrator` (texto de encerramento em `ApiPlanRecovery.cs`),
`ExtensionLocalization.RecoveryConfirmDiscard` e a apresentação desses dois no `Package`
`[v4-5]`, catálogos de `ExtensionOutputLocalization` e textos de `ExtensionLocalization`
(incluindo os novos do painel T2 e da §3.6) em PT/ES/EN. Atualizar as âncoras por valor em
`Tests/Localization/Test-ExtensionOutputLocalization.ps1`, inclusive o bloco que fixa as
mensagens «apague», e os testes de recuperação afetados.

O ramo «nada a recuperar» (B109, achado colateral 1 de
`2026-09-25-B109-RAMO-C-DIAGNOSTICO.md`) segue fora do escopo.

## 6. Provas

### 6.1 Offline

- Escopo (§3.5) `[v5-2]`.
- Proveniência (§4.1) `[v4-3]`:
  - metadata completa V4 → tudo `Metadata`;
  - metadata B115 → `Fallback`/`Default` por presença de chave, **incluindo** `servicesBasePath`
    e `errorDetail` como `Default`, embora fora de `notRecovered`;
  - caso negativo: metadata com `notRecovered` vazio e seções ausentes não sai como tudo
    `Metadata`;
  - `levels` ausente × `levels` nulo → tratados como casos distintos;
  - Source B054/B055 (`List()` sem parâmetros) → `ListFilters` vazio; Source B070 → filtros
    reconstruídos. Este par confirma ou derruba a inferência 2.2.2 (05/09).
- Gatilho T2 (§4.2) `[v5-4]`: `recovery.imported=true` → dispara; metadata V1 sem `errorDetail`
  e sem a marca → não dispara; metadata V4 completa → não dispara; marca ausente com seções
  ausentes → não dispara.
- Regra do acknowledgement `[v4-4]`: conjunto confirmado igual ao recalculado → permite;
  divergente ou ausente → bloqueia; seção editada pelo usuário continua no conjunto e não
  bloqueia.
- Regra da renomeação (§3.6) `[v5-1]`: API existente resolvida + nome planejado igual (inclusive
  com caixa diferente) → permite; nome diferente → bloqueia; sem API existente → permite. Extrair
  a comparação para função pura.
- Composição da mensagem de bloqueio de T1 `[v6-5]`: extrair para função pura a montagem do
  texto a partir das etapas bloqueadas do escopo e das colisões coletadas (hoje em
  `BuildBlockedMessage` e no retorno antecipado da §3.3) e testar: Apply só de SDT com Procedure
  bloqueada → a mensagem nomeia a etapa Procedures e lista a colisão da Procedure; etapa de API
  bloqueada por cascata → a causa de origem aparece. Com a leitura estática do Apply (`AddBlocked`
  antes do diário, §2.3), é a prova do relatório de backend que a IDE não alcança (§6.2).
- Localização PT/ES/EN (§5), incluindo as mensagens de recuperação `[v4-5]` e as novas `[v5-1]`.
- **Harness** `[v5-6]`:
  - registrar cada teste offline novo (proveniência, gatilho T2, acknowledgement, renomeação) em
    `scripts/Invoke-PrePushMechanicalChecks.ps1`, junto de `tests.writePreflightScope`, e, por
    alterar o checker, executar também
    `pwsh -NoProfile -File Tests/PrePushChecker/Test-OpenApiBuilderPrePushChecks.ps1`;
  - `Tests/WizardContract/Test-PrototypeWizardExistingApiFilters.ps1` confere por texto, dentro
    do reader, `ResolveApiObject(designModel, transaction, metadata)` e `fields.listFilters`. A
    extração para função pura deve preservar essas âncoras ou atualizá-las no mesmo passo, com a
    justificativa registrada no documento de evidência.
- Leitura registrada da ordem do Apply (§2.3). Não há seam offline para executar o Apply inteiro;
  nenhum teste offline prova «zero writes».
- Build local com `dotnet build-server shutdown` antes e depois; `git diff --check`.

### 6.2 IDE, pelo mantenedor `[v3-6]`

KB descartável. Só troca de DLL: sem mudança de manifesto, identidade ou registro, portanto sem
`genexus /install`, salvo se o diff final contrariar isso. O agente compila; o mantenedor instala
e executa.

A guarda de backend de T1 contra «outro chamador» não é exercitável na IDE (diálogo modal, caixa
desabilitada); ela fica provada pela leitura estática e pelo teste de escopo. A IDE prova
interface e ausência de escrita.

**Separação de provas** `[v6-5]`: sempre que a própria correção desabilita as caixas de escrita,
o fluxo normal não consegue pedir o Apply bloqueado, e o relatório de backend (`Bloqueados >= 1`,
colisões, diário não iniciado) não é produzível na IDE. Nesses casos, a IDE confere a interface
(caixas desabilitadas e desmarcadas, causa visível no texto da etapa) e a ausência de escrita; o
relatório fica provado por leitura estática do Apply e pelo teste offline da composição da
mensagem (§6.1). Não exigir na IDE um caminho que a correção impede.

1. API bloqueado por `MetadataMissing`: caixas de SDT e Procedure desabilitadas, com causa.
2. Conflito em Procedure (API existente): caixas de SDT e Procedure desabilitadas; o texto da
   etapa cita a Procedure e sua colisão (§3.1, §3.2); nada gravado. Relatório de backend: §6.1
   `[v5-2]` `[v6-5]`.
3. Relatório de bloqueio de T1 (`Bloqueados >= 1`, sem diário, sem Save, com a lista de
   colisões, §3.3): **não é caso de IDE**; provado por leitura estática e teste offline (§6.1)
   `[v6-5]`.
4. API válido e metadata completa: geração normal, sem painel T2, ordem F1/B111 preservada.
5. API ausente (`Create`): primeira geração completa, com nome da API livre.
6. Nenhuma etapa de escrita: sem diário, sem gravação, sem bloqueio artificial.
7. B115 elegível, preferência ligada, Transaction plana, fixture com valores diferentes dos
   padrões fixos do fato 2.2.3 (inclusive `includeBusinessComponentMessages=false`)
   `[v4-1]` `[v4-3]` `[v5-3]`: aceitar recuperação → reabrir Wizard → registrar `apiState`
   (esperado: apto, fato 2.2.4) → painel T2 lista as seções, com `servicesBasePath` e
   `errorDetail`, mostrando os padrões fixos → caixas de escrita desabilitadas até a
   confirmação. Sem confirmar → cancelar, nada gravado. Alterar uma seção → marca «alterado
   nesta execução», seção continua na lista. Confirmar **com «Gerar metadata» marcado**
   `[v5-7]` → Apply grava, relatório lista seções reconstruídas e as alteradas, metadata perde
   `recovery.imported`.
   - **7b** `[v5-7]`: mesmo estado, confirmar **sem** «Gerar metadata» → aviso do painel diz que
     a marca continua; após o Apply, reabrir o Wizard → painel T2 reaparece.
8. B115 com Transaction hierárquica e metadata sem `levels`: painel T2 mostra a causa e a saída
   pelo Remover, sem caixa de confirmação; caixas de escrita desabilitadas; nada gravado (§4.5).
   O bloqueio do Apply antes do diário é provado offline, pela regra do acknowledgement (§6.1)
   `[v6-5]`.
9. Metadata legada (se houver KB com V1–V3), inclusive V1 sem `errorDetail`: sem painel T2
   `[v5-4]`.
10. B115 inelegível, GUID divergente e `BaselineServiceSourceHashMismatch`: mensagens próprias da
    §5 `[v4-2]`.
11. Em todo bloqueio: SDTs, Procedures, API Object, metadata, Folder e habilitação de BC
    inalterados; diário não iniciado.
12. API ausente + Procedure externa homônima: caixas de SDT e Procedure desabilitadas, com a
    causa na Procedure; nada gravado. Relatório de backend: §6.1 `[v4-8]` `[v6-5]`.
13. Transições da interface `[v4-7]`: preferências com todas as caixas marcadas e API
    bloqueado; apto → bloqueado; bloqueado → apto; confirmação T2 marcada e desmarcada. Em todos,
    BC e List desabilitados e desmarcados enquanto a condição não for atendida.
14. Mensagem de `Recuperar operação interrompida` sobre estado sem metadata: não promete
    reaplicar pelo Wizard (§5) `[v4-5]`.
15. Renomeação `[v5-1]`: com API existente (apta e, separadamente, em `MetadataMissing`), o
    campo de nome aparece somente leitura com a explicação; nenhum caminho da interface permite
    alterar o nome; o relatório de bloqueio da §3.6 fica provado pelo teste offline, já que a
    interface impede chegar a ele.

Medição separada, fora do aceite: o critério de revogação da §4.5, na `Teste` hierárquica.

## 7. Documentação e fechamento

1. Documento de evidência em `Docs/Implementation/` (documento 15 §18.2), com o comando de
   conferência de visibilidade da §2.2.6 e a justificativa de qualquer âncora de teste
   atualizada (§6.1) `[v5-6]`.
2. Notas datadas `[v4-2]`:
   - `EVIDENCIA-IDE-DRIFT-API-OBJECT.md` §7.2 item 3 e §8: a regravação do SDT «sem o membro» é
     inferência não medida; os objetos foram removidos em §8.1 e o estado não é demonstrável; o
     mecanismo da perda de filtros foi a precedência do leitor, corrigida em `0902455`. A nota
     **não** remete à inspeção de SONDAS §10.2, que é de outra sessão.
   - `B111-SONDAS` §10.4: remeter à inspeção de §10.2 (SDT com o membro) e registrar que o API
     e as Procedures foram regravados em §10.7.
   - `B111-SONDAS` §10.2: registrar a inferência 2.2.2 (05/09) com seu estado de prova e
     reclassificar a pendência das Procedures como não demonstrável.
3. Remissão datada em `B111-F3-PLANO-DURABILIDADE-E-REMOCAO.md` §13.4/§13.5: o Apply completo
   pós-B115 passa a exigir a confirmação T2, ou bloqueia com Transaction hierárquica; a decisão
   §13.5.1 continua válida. Em §13.6, nota de que o Apply registrado ali é o caso T2 exercido sem
   aviso. Em §13.3, nota de que os padrões usados no lugar das seções não recuperadas são os
   fixos do Wizard, não as preferências `[v5-3]`.
4. Backlog: B110 descrito como intenção derivada de estado incompleto (T1 e T2), com os dois
   incidentes separados, sem «corrupção» persistida não medida. Prioridade Alta.
5. Checkpoint com estado obtido e próxima ação única.
6. `[Unreleased]` `[v4-6]`: não há entrada B110; criar **uma** entrada em `Fixed`, dizendo que o
   caminho pós-B115 é visível desde a `v0.1.0-alpha.8` (fato 2.2.6), que a variante de 04/09
   teve a perda de filtros corrigida em `0902455` (também na `alpha.8`) e que o caminho T1
   depende de estado alcançável em versões anteriores — conferir contra a tag antes de
   escrever. Registrar em `Changed` as mudanças de comportamento visíveis que não são correção:
   bloqueio de Apply só de SDT por conflito em Procedure (§3.2) e nome da API somente leitura com
   API existente (§3.6) `[v5-1]` `[v5-2]`. No mesmo passo, revisar a frase da entrada `Planned`
   «Escrita parcial do BC» que afirma que o Wizard «degrada o plano». `Validated` só depois da
   IDE.

   **Fechamento documental de 2026-09-27.** A entrada B110 ficou única em `Fixed`, com o bloqueio
   e as duas mudanças visíveis descritos no mesmo texto. A separação adicional em `Changed`
   prevista acima não foi feita para respeitar a regra “uma entrega, uma entrada” do `AGENTS.md`;
   esta nota preserva a decisão sem reescrever a proposta histórica.

7. Varredura de promoção: B110, B115, `recovery.imported`, `notRecovered`, «Apply completo»,
   «Cancelar é a única», `degrada o plano`, `limpeza manual`, `alpha.9`, «reaplicar pelo
   Wizard», «padrão das preferências», «renomear», termos de próximo passo e contagens `[v4-6]`
   `[v5-3]`.

## 8. Processo, itens fora do escopo e autorização `[v3-9]`

- O original desta proposta estava em `Temp/`, ignorado pelo Git. Este plano foi promovido
  a `Docs/Implementation/` com autorização de execução do mantenedor em 2026-09-27.
- Ao promover, citar código por símbolo (tipo, método, bloco), não por arquivo e linha: uma
  referência móvel nova a C# em `Docs/` reprova o check `docs.csharpLineReferences` (B128).
- **Itens próprios, fora do B110**, a registrar no backlog na promoção:
  - **Subníveis podados que voltam em silêncio** `[v5-5]`: metadata completa com `levels: null`
    depois de o usuário podar todos os subníveis; na reabertura, `CreateDefault` reinclui todos
    (§4.5, caso adjacente). Inferência; pede medição.
  - **Metadata obsoleta com API ausente** `[v5-7]`: com File de metadata de uma API que não
    existe mais e API ausente, a etapa de metadata fica bloqueada enquanto a API está em
    `Create`; o Apply cria a API sem metadata e deixa o par com GUID divergente. Avaliar.
- Fora do escopo: editar KB, instalar DLL, alterar `C:\Program Files (x86)\GeneXus`, mudar a
  preferência B115 padrão, criar release, tag ou push. Os commits locais por etapa da §10 integram a execução
  autorizada em 2026-09-27.

## 9. Critérios de aceite

- T1: API existente bloqueado + qualquer seleção de escrita → interface impede SDT/Procedure
  (e, por consequência, BC e List), provado na IDE; Apply bloqueia antes do diário, com
  `Bloqueados >= 1` e a lista de colisões no relatório, provado por leitura estática e teste
  offline da composição da mensagem (§6.1) `[v6-5]`.
- Escopo: qualquer escritor exige SDTs, Procedures e API; um Apply só de SDT bloqueado por
  Procedure cita a Procedure e sua colisão `[v5-2]`.
- Renomeação: com API existente resolvida, o nome é somente leitura na interface e o Apply
  bloqueia nome planejado diferente `[v5-1]`.
- T2: dispara somente por `recovery.imported`; sem a confirmação específica, nenhuma escrita; o
  conjunto confirmado é o calculado pela KB, sem hash de valores; com confirmação, o relatório
  lista cada seção reconstruída e as alteradas pelo usuário; o Apply recusa confirmação
  divergente ou ausente `[v5-4]` `[v5-7]`.
- Proveniência calculada por presença de chave; `servicesBasePath` e `errorDetail` aparecem como
  `Default` sob B115; o painel mostra os padrões fixos, não as preferências `[v5-3]`.
- Aviso do painel condicional a «Gerar metadata»; sem ele, a marca continua e o painel reaparece
  `[v5-7]`.
- Sem T2 em metadata sem a marca, inclusive V1 legada; primeira geração (sem bloqueio de
  SDT/Procedure, nome livre) e seleção vazia preservadas.
- **Sob T2**, Transaction hierárquica com a chave `levels` ausente → bloqueio, sem opção de
  confirmar, com saída pelo Remover (§4.5) `[v4-8]`.
- Mensagens da §5 corretas por estado, inclusive `BaselineServiceSourceHashMismatch`, a de
  renomeação e as de recuperação, em PT/ES/EN.
- Testes offline (escopo, proveniência, gatilho T2, acknowledgement, renomeação, localização)
  passando e registrados no orquestrador pré-push; `Tests/PrePushChecker` passando; âncoras de
  `Test-PrototypeWizardExistingApiFilters.ps1` preservadas ou atualizadas com justificativa;
  leitura da ordem do Apply registrada; matriz IDE executada `[v5-6]`.
- Contradições documentais corrigidas por nota datada, com os dois incidentes separados;
  nenhuma afirmação de dano persistido sem inspeção.
- Itens fora do escopo da §8 registrados no backlog.
- Evidência e checkpoint conforme B124; `[Unreleased]` coerente com a tag.

## 10. Ordem de execução `[v6-3]`

Sequência autorizada e executada em 2026-09-27, mantida aqui para rastreio. As etapas de
código terminaram com as validações e os commits locais indicados; a etapa 7 foi aceita com
a exceção de campo declarada no estado deste plano. Push, tag e release continuam exigindo
autorização própria.

| Etapa | Conteúdo | Validação ao fim |
|---|---|---|
| 0 | Promover o plano a `Docs/Implementation/`, com commit | `git diff --check`, LF final, check B128 |
| 1 | T1 no backend: escopo (§3.1), retorno antecipado com colisões (§3.3), composição da mensagem como função pura `[v6-5]` | `Test-ApiPlanWritePreflightScope.ps1`, teste offline da mensagem registrado no orquestrador, build |
| 2 | Renomeação (§3.6): função pura e bloqueio no Apply | teste offline novo, registrado no orquestrador |
| 3 | Proveniência e gatilho T2 (§4.1, §4.2): função pura; âncoras de `Test-PrototypeWizardExistingApiFilters.ps1` | testes offline novos; `Tests/PrePushChecker/Test-OpenApiBuilderPrePushChecks.ps1` |
| 4 | T2 no Apply: acknowledgement, construtor do Sync, relatório, bloqueio hierárquico (§4.4, §4.5) | teste do acknowledgement, build |
| 5 | Interface: ordem de avaliação, painel T2, nome somente leitura (§3.4, §4.3, §3.6) | build; `Tools/Test-ExtensionCommandRegistration.ps1` inalterado |
| 6 | Mensagens e localização (§5), PT/ES/EN | testes de localização |
| 7 | DLL para o mantenedor: uma rodada com os casos da §6.2 | execução na IDE pelo mantenedor |
| 8 | Documentação (§7): evidência B124, notas datadas, backlog, checkpoint, `[Unreleased]`, varredura | rotina pré-push local |

Razões da ordem: o backend é a autoridade e as etapas 1 a 4 se provam offline; a interface
entra quando os contratos estão fixos; a IDE fica numa rodada só, porque a instalação é do
mantenedor; a documentação vem depois da evidência de campo, como pede o B124. A sessão de
execução pode propor mudança de ordem, mas só a aplica com decisão do mantenedor.
