# S-B111 · F3 — P8: validação na IDE

**Data:** 2026-09-14. **Sprint:** `S-B111`. **Fase:** F3, etapa P8.
**Plano governante:** [`2026-09-04-B111-F3-PLANO-DURABILIDADE-E-REMOCAO.md`](2026-09-04-B111-F3-PLANO-DURABILIDADE-E-REMOCAO.md), seção 9.
**Implementação validada aqui:** [`2026-09-14-S-B111-F3-P4-P7-IMPLEMENTACAO-OFFLINE.md`](2026-09-14-S-B111-F3-P4-P7-IMPLEMENTACAO-OFFLINE.md).

**Documento em andamento.** Ele é escrito enquanto a bateria acontece, um cenário por vez, com
os números colados da janela Output. Cenário sem registro aqui é cenário que ainda não foi
exercido — não presuma o contrário.

## 1. Ambiente

| Item | Valor |
|---|---|
| DLL | build Release do commit `a32798b` (P4 a P7 + encerramento de registro), instalada com `genexus /install` porque o manifesto ganhou três comandos |
| IDE | GeneXus 18 U15 |
| KB | `wsEducacaoSpTeste` |
| Transaction | `Teste` — chave de três partes (`TesteId`, `TesteDate`, `TesteCodigo`) e quatro subníveis (`TestePortfolio`, `TesteItem`, `TesteItemFolio`, `TesteItemFolioDoc`) |
| Volume | 25 objetos próprios: 1 API Object, 5 Procedures (List/Get/Create/Update/Delete), 18 SDTs, 1 File de metadata |
| Preservados | 3 SDTs compartilhados (`sdt_API_ErrorMessage`, `sdt_API_ErrorResponse`, `sdt_API_Pagination`), o Folder `TesteOpenApi` (reutilizado, `FolderWasCreated=False`) e a própria Transaction |

O diário da KB é o File `GxOpenApiBuilder_OperationJournal`, `FileId=88`, preexistente desde a
validação da P2 — todas as operações abaixo reutilizaram esse mesmo File (`Created=False`),
como o contrato exige: há exatamente um por KB.

## 2. Cenário 1 — remoção completa pela fila nova

**Setup:** API gerada e íntegra na Transaction `Teste`. Nada alterado à mão.
**Ação:** menu de contexto da Transaction → `Remover API gerada` → `Sim`.

| Medida | Resultado |
|---|---|
| Estado terminal | `Removed` / `Removed` |
| Fila | `Outcome=Removed`, `Passadas=1/25`, `Removidos=25`, `Pendentes=0` |
| Intenção registrada | `Alvos=30, NaFila=25, Preservados=5` |
| Snapshot do checkpoint | `Recibos=25, Inventário=30` |
| Diário | `Checkpoints=4`, `TotalMs=216`, `Durability=Confirmed` |
| Relatório B081 | `Confirmados=25, Pendências=0, Removidos=25`, 5,1 s |
| Custo total | `TotalMs=5114`, dos quais `FilaRemocao=4714ms` |
| Identidade | `OperationId='22090d87-…'`, `ApplicationId='1a7c456d-…'` |

**O que isso prova.** A intenção completa — 30 alvos, 25 na fila — foi gravada **antes** do
primeiro `Delete()`, e o inventário do checkpoint (30) não é derivação dos recibos (25): é o
registro do que se pretendia, que sobrevive a uma interrupção. Os `Checkpoints=4` batem com a
política `3 + P` para `P=1`. Os três SDTs compartilhados, o Folder reutilizado e o Business
Component da Transaction ficaram intactos.

## 3. Regeração intermediária — Apply completo

**Setup do cenário 2.** API regerada pelo Wizard com a mesma seleção (cinco serviços, quatro
subníveis, todas as etapas de geração). Nada alterado nas abas.

| Medida | Resultado |
|---|---|
| Relatório | `Criados=25, Atualizados=3, Avisos=2`, 11,0 s, `Resultado='SuccessWithWarnings'` |
| API Object | `ApiSaveAttempted=True`, `ApiSaveCount=1`, `FinalApiWriter='List'` |
| Diário | `OperationId='ae98e2cb-…'`, `ApplicationId='27c47222-…'`, `Checkpoints=4`, `TotalMs=141` |
| Metadata | `GOAB_API_METADATA_B060_V3`, `Bytes=117988`, `Sha256='46C7BE98…'` |
| Integridade | `PlannedContractHash='16DF0B0A…'` |

Os dois avisos são conhecidos e não bloqueiam: fallback em inglês nas descrições de serviço e o
Folder preexistente que será reutilizado e nunca removido.

A ordem da F1 aparece intacta na Output: SDTs, Procedures, preparação do API Object sem gravar,
Business Component, List — e **um** `API.Save()` ao fim, pelo writer `List`.

## 4. Cenário 2 — alvo previsto ausente antes do `Delete()`

**A mudança de comportamento da P4, exercida em campo.**

**Setup:** com a API íntegra, o **API Object `apiTeste` foi apagado à mão** pela KB Explorer.
Escolhido por ser o único objeto da API que ninguém referencia — qualquer SDT ou Procedure seria
recusado pela IDE por estar referenciado. A metadata `apiTeste_Metadata` foi deixada no lugar:
é ela que declara o API Object como alvo previsto, e é o descompasso entre o previsto e o real
que o cenário exercita.

**Ação:** menu de contexto da Transaction → `Remover API gerada` → `Sim`.

| Medida | Resultado |
|---|---|
| Estado terminal | `Partial` / `RemovalPartial` |
| Motivo persistido | `TargetAbsentBeforeDelete` |
| Fila | `Passadas=1/25`, `Removidos=0`, `Pendentes=25`, `Bloqueado='ApiObject:apiTeste'` |
| Snapshot do checkpoint | `Recibos=1, Inventário=30` |
| Diário | `Checkpoints=3`, `TotalMs=121`, `Durability=Confirmed` |
| Relatório B081 | `Resultado='Interrupted'`, `Removidos=0`, `Bloqueados=1`, `Avisos=1`, 351 ms |
| Identidade | `OperationId='0eee2cc9-…'`, `ApplicationId='27c47222-…'` |

**O que isso prova.**

1. **`Removidos=0` com 25 pendentes.** A fila parou no primeiro alvo e não encostou nas cinco
   Procedures, nos dezoito SDTs nem na metadata. Antes desta DLL a ausência seria engolida como
   sucesso implícito e a remoção seguiria em frente — que é a origem do relatório «Removidos:
   nenhum» com objetos apagados, de 2026-09-06;
2. **`Checkpoints=3`.** Nenhuma passada chegou ao fim, então `P=0` e a matriz `3 + P` dá três:
   `Prepared`, `Active` e o terminal. O checkpoint de passada só existe quando a passada se
   completa;
3. **`Recibos=1` contra `Inventário=30`.** Um único recibo — o `NotAttempted` do API Object —
   e a intenção inteira preservada. É essa assimetria que permite dizer depois o que era
   previsto e o que aconteceu;
4. **`ApplicationId='27c47222-…'` é o mesmo do Apply da seção 3.** Confirma em campo a linha da
   matriz de identidade da seção 4.1.1: um `Remove` sobre metadata V3 cria `operationId` novo e
   **reutiliza** o `applicationId` do ownership, sem regravar a metadata. No cenário 1, o
   `applicationId` era outro (`1a7c456d-…`), o da geração anterior.

### 4.1 Anotação de apresentação, sem ação nesta etapa

O diagnóstico de persistência do relatório mostra, para o alvo ausente:

```
[ApiObject/API] apiTeste: Outcome=OutcomeUnknown; Confirmação=NotAttempted; API Object ausente antes do Delete.
```

e o relatório conta isso como `Pendências=1`. O estado, porém, é **conhecido**: o objeto está
comprovadamente ausente. `OutcomeUnknown` vem da resolução de desfecho do seam da F2 para um
`RecordNotAttempted`, é anterior a esta frente e **não influencia decisão nenhuma** — a fila
classificou `AbsentBeforeDelete` por evidência própria e o envelope gravou
`TargetAbsentBeforeDelete`. Fica registrado como candidato a ajuste de vocabulário depois da
P8, não como defeito de comportamento.

## 5. Cenário 3 — recuperação sobre o envelope interrompido

**Passou**, em três passagens: a primeira exerceu a recusa e expôs o problema da janela, a
segunda e a terceira ajustaram as medidas, e a última executou o encerramento.

**Setup:** o envelope `Partial/RemovalPartial` deixado pelo cenário 4. Nada alterado à mão.
**Ação:** menu de contexto da Transaction → `Recuperar operação interrompida`.

A reidratação leu o envelope e apurou a etapa certa:

```
Operação Remove sobre 'Teste': estado Partial/RemovalPartial, envelope Active, durabilidade Confirmed.
OperationId=0eee2cc9-…, ApplicationId=27c47222-…, atualizado em 2026-09-15 01:15:45Z.
Motivo registrado no envelope: TargetAbsentBeforeDelete.
Próxima etapa apurada: Discard.
```

As **trinta** linhas de inventário saíram com o cruzamento correto: `ApiObject apiTeste —
previsto: Delete; na KB: Absent`, os 24 alvos restantes `Delete`/`Present`, e os cinco
preservados (3 SDTs compartilhados, Folder e Transaction) como `Preserve`/`Present`. O
`OperationId` é o mesmo do cenário 4: a recuperação agiu sobre o envelope existente, sem criar
outro.

**Resposta `Não`:** `Recuperação recusada pelo usuário. Nenhuma alteração foi feita.` — o
caminho seguro fecha sem tocar em nada, como esperado.

**Resposta `Sim`:**

```
Recuperação concluída: Etapa='Discard', OperationId='0eee2cc9-…', Estado='Completed'.
O registro da operação interrompida foi encerrado. A Knowledge Base está liberada para a
próxima operação; nenhum objeto foi apagado, e o inventário do que ficou pela metade continua
gravado no diário.
```

O `OperationId` é o mesmo desde o cenário 4 — três operações de recuperação sobre o **mesmo**
envelope, sem que nenhuma criasse outro. O envelope foi de `Partial/RemovalPartial` a
`Completed/Discarded`, e os 24 objetos, a metadata e o Folder continuaram na KB.

Com isso, a saída que antes exigia apagar o File do diário à mão passou a existir dentro da
ferramenta — que era o ponto da P6.

### 5.1 Correção de apresentação saída deste cenário

A janela era um `MessageBox` nativo, que não aceita largura customizada: o texto chegava numa
coluna estreita, o inventário de 24 nomes derretia dentro do parágrafo e os asteriscos de
Markdown apareciam literais. Trocada pelo `ExtensionRecoveryDialog`, com o desenho do diálogo
do Remover — largura de leitura, inventário em bloco monoespaçado e rolável, pergunta no rodapé
e o botão seguro com o foco. Os resumos deixaram de enumerar nomes.

Numa segunda passagem, com o diálogo já em uso, as medidas foram aumentadas em 30% na largura
e na altura — 1404 × 624, com o bloco de inventário até 546 px, tudo limitado pela área útil do
monitor. Nomes de SDT hierárquico desta Transaction passam de sessenta caracteres, e é a
largura que decide se o inventário se lê ou se quebra no meio do nome.

As medidas finais, depois de duas passagens sobre a lista real de trinta alvos: **1685 × 749**,
com o bloco de inventário até 655 px e piso de 1123 px de largura ao encolher.

## 6. Cenários restantes

| # | Cenário | Estado |
|---|---|---|
| 1 | Remoção completa (fila nova) | **passou** — seção 2 |
| 2 | Alvo previsto ausente antes do `Delete()` | **passou** — seção 4 |
| 3 | Recuperação sobre o envelope `Partial`: encerrar o registro | **passou** — seção 5 |
| 4 | Devolver a KB ao normal — ver o achado da seção 7 | não iniciado |
| 5 | Abortar um Apply no meio; oferta proativa; recuperação | não iniciado |
| 6 | Wizard cancelado antes de aplicar: envelope `Prepared` e abandono | não iniciado |
| 7 | Interromper uma remoção no meio e retomar a fila | não iniciado |
| 8 | Remoção de API legado, com metadata válida e com metadata insuficiente | não iniciado |
| 9 | Acréscimo de tempo do diário na KB grande, contra o orçamento de 4.4 | não iniciado |

O cenário 3 dependia do envelope `Partial` deixado pelo cenário 2: **não apagar o File
`GxOpenApiBuilder_OperationJournal` à mão** entre um e outro, sob pena de destruir a condição.

## 7. Achado — metadata completa com API Object apagado à mão não tem saída pela ferramenta

Descoberto ao planejar o cenário 4, conferindo o código antes de propor o caminho.

O estado em que a KB ficou depois do cenário 2 — metadata **completa** e válida, API Object
ausente, 24 objetos próprios presentes — não é recuperável por nenhum dos três comandos:

| Comando | O que faz nesse estado | Por quê |
|---|---|---|
| `Wizard` | trava em `OwnershipSchemaApiNameOrGuidMismatch` | a metadata registra um `apiGuid` que não existe mais |
| `Remover API gerada` | `Partial` com `TargetAbsentBeforeDelete` | o primeiro alvo previsto está ausente — é o cenário 2 |
| Recuperação de metadata órfã (`B115`) | **não é oferecida** | exige exatamente **um** API Object presente (`TryPrepare`), e aqui há zero |

O `B115` cobre o caso vizinho — metadata **recuperada** apontando para um API Object que foi
removido e regerado com o mesmo nome —, e recusa deliberadamente a metadata completa: o
fingerprint B067 cobre o conteúdo inteiro, e corrigir só o `apiGuid` trocaria um bloqueio por
outro. A recusa está certa; o que falta é **orientação**: nenhuma das três mensagens diz o que
fazer.

A saída prática, exercida no cenário 4, é apagar **a metadata** à mão — um objeto, em vez dos
vinte e quatro — e reaplicar pelo Wizard, que reencontra SDTs e Procedures e regera API Object
e metadata. O plano volta aos defaults das preferências: paginação, ordenação e obrigatórios
específicos não sobrevivem, porque só existiam na metadata descartada.

**Corrigido na mesma data**, por decisão do usuário: texto não vira backlog. As duas mensagens
que a pessoa efetivamente encontra passaram a dizer o que fazer, nos três idiomas.

| Onde | O que passou a dizer |
|---|---|
| `ApiPlanMetadataFileWriter`, descompasso de `ownership.apiGuid` | que o File registra um API Object que não existe mais, que a saída é apagar **esse File** e reaplicar pelo Wizard, e o que se perde ao fazer isso |
| Relatório do `Remover`, quando o alvo ausente é o próprio API Object | as duas saídas — regerar sobre o que restou apagando a metadata, ou descartar apagando os objetos listados — deixando a escolha com quem decide |

A mensagem do `apiGuid` deixou de nomear o campo incompatível: nomear um campo de JSON não é
diagnóstico para quem está na IDE. A recusa continua a mesma; o que mudou é que ela agora
termina com um caminho. Asserções trilíngues no gate `tests.extensionOutputLocalization`.

O que **não** mudou, de propósito: a recusa do `B115` sobre metadata completa. Ela está certa
pelo motivo que o próprio código explica — o fingerprint B067 cobre o conteúdo inteiro, e
corrigir só o `apiGuid` trocaria um bloqueio por outro.
