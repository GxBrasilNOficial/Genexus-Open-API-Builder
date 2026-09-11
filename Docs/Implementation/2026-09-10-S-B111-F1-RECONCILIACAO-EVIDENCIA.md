# S-B111 F1 — Reconciliação da evidência manual na IDE

## Conclusão

Esta reconciliação confronta o plano da F1 com o histórico completo da sessão de
2026-09-09/11 e com os relatórios e Outputs enviados durante os testes. O trabalho
manual não foi perdido: os quatro fluxos positivos do Wizard foram exercitados, há
evidência de Sync bem-sucedido com `BC + List`, sem BC/List e somente BC e, após a
correção da regressão, também há evidência do guard de divergência
manual e da restauração idempotente. O problema foi de promoção documental: os
testes do Wizard foram tratados como se também fechassem a matriz específica do
Sync, e a primeira falha do Sync sem BC/List não foi separada do caminho corrigido.

O registro correto é, portanto:

- a implementação da F1 está concluída e recebeu validação manual para
  encerramento da frente, com a exceção explícita do `B121`;
- os quatro perfis positivos do Wizard estão registrados como exercitados;
- os Syncs `BC + List`, sem BC/List e somente BC estão registrados com relatório e Output;
- o guard de reencontro estrito para divergência manual e a restauração sem
  diferença também estão registrados com relatório e Output;
- há uma tentativa de Sync somente List, mas ela executou consumidores de BC
  antes de List e, por isso, não comprova o perfil isolado; o caso foi registrado
  no `B121`;
- o bloqueio de Sync com BC selecionado em Transaction sem BC habilitado foi
  executado em 2026-09-11 e passou no preflight com zero gravações;
- a F1 pode ser promovida à avaliação da F2 com a exceção explícita do `B121`,
  que permanece fora da sprint para tornar a seleção de consumidores explícita.

Não se conclui que os cenários sem registro nunca tenham sido executados. Conclui-se
somente que a evidência encontrada não sustenta o aceite amplo exigido pelo plano.

## Matriz reconciliada

| Família | Cenário | Evidência encontrada na sessão | Situação para o aceite da F1 |
|---|---|---|---|
| Wizard | API-only | Transaction `Laudo`; `FinalApiWriter='B054'`, `ApiSaveCount=1`, `Bloqueados=0`; criação do API Object novo confirmada | **Passou** |
| Wizard | BC-only | Transaction `NotaFiscal`; aplicação com List desmarcado, writer final Business Component, um Save e zero bloqueios; reteste da correção na Transaction `Carga`, com `Completar listagem=False`, `FinalApiWriter='Business Component'`, `ApiSaveCount=1`, `Criados=5`, `Atualizados=6` e `Bloqueados=0` | **Passou** |
| Wizard | List-only | Transaction `NotaFiscal`; `FinalApiWriter='List'`, `ApiSaveCount=1` e `Bloqueados=0` confirmados na Output | **Passou** |
| Wizard | BC + List | Transaction `NotaFiscal`; BC salvou Procedures, List salvou a Procedure e o API Object por último, com um Save e zero bloqueios | **Passou** |
| Wizard | API-only sobre API já REST-completa | `apiNotaFiscal` bloqueado antes do primeiro Save; `ApiSaveCount=0` e `Bloqueados=1` | **Guarda passou** |
| Sync | sem BC/List | Após a correção, `LaudoObs` foi marcado somente em `Response`; o resumo manteve BC e List desmarcados, o Sync aplicou `Updated=14`, `Blocked=0` e um `API.Save()`. Em seguida, a divergência manual `LaudoObs`→`LaudoObs1` bloqueou com `ApiSaveCount=0`, e a restauração produziu reencontro sem diferença | **Passou; caminho positivo, guard e restauração** |
| Sync | somente BC | Transaction `Carga`; `CargaObservacao2` selecionado em Response/Create/Update, `ListFilters` desmarcado; Procedures BC salvas antes do API Object; `FinalApiWriter='Business Component'`, `ApiSaveCount=1`, `Atualizados=11`, `Bloqueados=0` | **Passou** |
| Sync | somente List | Tentativa na Transaction `Contrato`: `ContratoObservacao` foi marcado somente em `Response`, mas o Output registrou BC (`Get/Create/Update`) antes de List; relatório com `FinalWriter='List'`, `ApiSaveCount=1` e `Bloqueados=0` | **Não comprovado; evidência do B121** |
| Sync | BC + List | Delta `NotaFiscalObs2` 40→41; `FinalApiWriter='List'`, `ApiSaveCount=1`, `Atualizados=14`, `Bloqueados=0`; Output confirmou consumidores antes do API Object | **Passou** |
| Sync | BC selecionado sem BC habilitado na Transaction | Transaction `Contrato` com `Business Component=False`; o diálogo não oferecia opção explícita de BC, mas o preflight detectou a etapa derivada e bloqueou com `B055` antes de qualquer gravação; `ApiSaveAttempted=False`, `ApiSaveCount=0`, `Criados/Atualizados/Removidos=0` e `Bloqueados=1` | **Passou; zero gravações** |

## Linha do tempo que evita a confusão

1. O primeiro teste de Sync foi planejado como “sem BC/List”, com um delta em
   `NotaFiscalObs`, mas o relatório mostrou as Procedures de BC e List e writer
   final `List`. Ele foi corretamente reclassificado na própria sessão como
   `BC + List`.
2. O Wizard `BC-only` foi executado e passou na `NotaFiscal`; depois, na `Carga`,
   foi retestado após a correção que omite os contratos de List sem o serviço
   `List`, com `Criados=5`, `Atualizados=6` e `Bloqueados=0`.
3. O Wizard `List-only` foi executado e passou com `FinalApiWriter='List'` e um
   `API.Save()`.
4. O Wizard `BC + List` foi executado e passou com BC antes de List e o API Object
   salvo uma única vez no final.
5. O Wizard `API-only` sobre a `NotaFiscal` foi primeiro bloqueado corretamente
   porque a API já tinha contrato REST completo; depois foi executado
   positivamente na Transaction `Laudo`, com API Object novo e writer `B054`.
6. O Sync `BC + List` foi executado novamente com o delta `NotaFiscalObs2` 40→41;
   o relatório foi fechado antes da captura da Output, que confirmou a ordem e o
   único Save.
7. A primeira tentativa separada do Sync sem BC/List usou o novo atributo
   `LaudoObs`, marcado somente em `Response`. Com a DLL anterior, o preflight
   bloqueou o reencontro estrito antes de qualquer gravação, porque o SDT
   existente tinha dois membros e o contrato planejado passou a exigir três.
8. Com a DLL corrigida, o mesmo cenário foi reaplicado e passou: o SDT recebeu
   `LaudoObs`, o relatório registrou `Updated=14`, `Blocked=0` e um único
   salvamento do API Object.
9. Para retestar a causa original da alteração, `LaudoObs` foi renomeado somente
   no SDT para `LaudoObs1`. O Sync detectou conflito e bloqueou no preflight,
   registrando membro extra, `ApiSaveAttempted=False`, `ApiSaveCount=0` e
   `Bloqueados=1`; nenhum objeto foi alterado.
10. `LaudoObs1` foi restaurado para `LaudoObs`. O Sync seguinte encontrou os
    três membros esperados, com diff zero, `ApiSaveCount=0` e `Bloqueados=0`.
11. Depois do Apply BC-only do Wizard, a `Carga` recebeu `CargaObservacao2`
    (`VarChar(40)`). O Sync somente BC aplicou o delta com o atributo em
    Response/Create/Update e `ListFilters` desmarcado; o Output confirmou os
    três consumidores antes de `apiCarga`, e o relatório confirmou um único
    `API.Save()` final, `Atualizados=11` e `Bloqueados=0`.
12. Na tentativa seguinte de Sync somente List, a Transaction `Contrato` tinha
    sido criada pelo Wizard com `ApplyBusinessComponent=False` e `ApplyList=True`.
    Mesmo sem opção de BC no diálogo e com `ContratoObservacao` marcado somente
    em `Response`, o Output registrou `procContrato_API_Get`,
    `procContrato_API_Create` e `procContrato_API_Update` no estágio Business
    Component antes de `procContrato_API_List` e `apiContrato`. O relatório
    terminou com `FinalWriter='List'`, `ApiSaveCount=1`, `Atualizados=14` e
    `Bloqueados=0`; o caso foi classificado como evidência do B121, não como
    aceite de Sync somente List.
13. No reteste seguinte, ainda na `Contrato` e com `Business Component=False`,
    o diálogo mostrou a inclusão de `ContratoObservacao2 (VARCHAR)` somente em
    `Response`, além da divergência manual do `sdtContrato_API_Response`.
    Após Apply, o preflight bloqueou em `ApiPlanWritePreflight.ValidateForF1`
    com `B055`, porque a etapa Business Component estava desabilitada na
    Transaction. O relatório registrou `Resultado='Interrupted'`,
    `ApiSaveAttempted=False`, `ApiSaveCount=0`, `Criados=0`, `Atualizados=0`,
    `Removidos=0`, `Bloqueados=1` e `Avisos=2`; a divergência do SDT não foi
    substituída e nenhuma alteração foi feita na KB.
14. Depois da restauração de uma versão salva da KB, a árvore `LaudoOpenApi`
    foi conferida com `apiLaudo`, quatro Procedures e cinco SDTs próprios, mas
    sem o File `apiLaudo_Metadata`. O Wizard ofereceu a recuperação de metadata;
    a confirmação criou o File de inventário (`Guid` próprio,
    `Bytes=1489`), não alterou API Object, Procedure ou SDT e exigiu reabrir o
    Wizard. A primeira aplicação posterior foi bloqueada antes de qualquer
    gravação por `B063/B064/B067`, porque o contrato planejado tinha zero
    `ListFilters` e o SDT existente continha o membro extra `LaudoNumero`.
    Após restaurar novamente a versão salva e alinhar o contrato ao estado
    existente — serviços `List`, `Get`, `Create` e `Update`, filtro
    `LaudoNumero`, SDTs/Procedures/API habilitados, `Completar listagem=False`,
    metadata habilitada e REST via BC desabilitado — a aplicação completa
    atualizou as quatro Procedures, `apiLaudo` e `apiLaudo_Metadata` (`Bytes=33098`),
    com `FinalApiWriter='B054'`, `ApiSaveCount=1` e `Bloqueados=0`. Os dois
    avisos foram o fallback de idioma e a reutilização do Folder. Esta passagem
    recuperou a baseline; não deve ser contada como o teste API-only com SDTs e
    Procedures desmarcados.

## O que foi anotado e o que não foi

O documento de aceite da F1 e o Changelog registraram corretamente os quatro
perfis do Wizard e os Syncs `BC + List`, sem BC/List e somente BC, mas não
mantiveram inicialmente uma matriz explícita que separasse as duas famílias.
A frase posterior “não há outro teste obrigatório” foi ampla demais: ela confundiu
a matriz positiva do Wizard com a matriz de Sync do plano. A evidência consolidada
fecha também o caminho positivo somente BC e o bloqueio B055 de BC sem habilitação.
Permanece sem comprovação isolada somente o Sync somente List, registrado no
`B121`; a F1 foi aceita para prosseguir com essa exceção explícita.

Build All nos dois environments e o teste HTTP de Update são evidências
complementares. Eles não substituem os casos manuais de Sync definidos na seção
7 do plano da F1. A evidência atual de `LaudoObs` cobre o caminho positivo sem
BC/List, o bloqueio de divergência manual e o reencontro idempotente;
`CargaObservacao2` cobre o caminho somente BC. Somente List continua sem
comprovação isolada; a tentativa de `Contrato` foi capturada, mas classificada
no `B121` porque o Sync executou BC antes de List. O bloqueio de BC sem
habilitação foi capturado separadamente e passou com zero gravações.

## Fonte e limite da reconstrução

A reconstrução foi feita a partir do histórico local da sessão, que preserva as
mensagens, os textos de relatório/Output e as referências às capturas. Esse
histórico está fora do repositório e não deve ser tratado como artefato portátil;
este documento registra apenas a conclusão auditável e não depende da permanência
dos arquivos temporários de captura.

Plano confrontado: `2026-09-04-B111-F1-PLANO-ORDEM-E-WRITER-FINAL.md`, seções 6.2
e 7.
