# S-B111 F1 — Reconciliação da evidência manual na IDE

## Conclusão

Esta reconciliação confronta o plano da F1 com o histórico completo da sessão de
2026-09-09/10 e com os relatórios e Outputs enviados durante os testes. O trabalho
manual não foi perdido: os quatro fluxos positivos do Wizard foram exercitados, há
evidência de Sync bem-sucedido com `BC + List` e, após a correção da regressão,
também há evidência do caminho positivo sem BC/List, do guard de divergência
manual e da restauração idempotente. O problema foi de promoção documental: os
testes do Wizard foram tratados como se também fechassem a matriz específica do
Sync, e a primeira falha do Sync sem BC/List não foi separada do caminho corrigido.

O registro correto é, portanto:

- a implementação da F1 está concluída e recebeu validação manual parcial;
- os quatro perfis positivos do Wizard estão registrados como exercitados;
- os Syncs `BC + List` e sem BC/List estão registrados com relatório e Output;
- o guard de reencontro estrito para divergência manual e a restauração sem
  diferença também estão registrados com relatório e Output;
- não há evidência separada, na sessão, dos perfis Sync somente BC, somente List
  nem do bloqueio de Sync com BC selecionado em Transaction sem BC habilitado;
- a F1 não deve ser promovida à avaliação da F2 até essa lacuna ser recuperada ou
  executada e registrada.

Não se conclui que os cenários sem registro nunca tenham sido executados. Conclui-se
somente que a evidência encontrada não sustenta o aceite amplo exigido pelo plano.

## Matriz reconciliada

| Família | Cenário | Evidência encontrada na sessão | Situação para o aceite da F1 |
|---|---|---|---|
| Wizard | API-only | Transaction `Laudo`; `FinalApiWriter='B054'`, `ApiSaveCount=1`, `Bloqueados=0`; criação do API Object novo confirmada | **Passou** |
| Wizard | BC-only | Transaction `NotaFiscal`; aplicação com List desmarcado, writer final Business Component, um Save e zero bloqueios confirmados no relatório/Output | **Passou** |
| Wizard | List-only | Transaction `NotaFiscal`; `FinalApiWriter='List'`, `ApiSaveCount=1` e `Bloqueados=0` confirmados na Output | **Passou** |
| Wizard | BC + List | Transaction `NotaFiscal`; BC salvou Procedures, List salvou a Procedure e o API Object por último, com um Save e zero bloqueios | **Passou** |
| Wizard | API-only sobre API já REST-completa | `apiNotaFiscal` bloqueado antes do primeiro Save; `ApiSaveCount=0` e `Bloqueados=1` | **Guarda passou** |
| Sync | sem BC/List | Após a correção, `LaudoObs` foi marcado somente em `Response`; o resumo manteve BC e List desmarcados, o Sync aplicou `Updated=14`, `Blocked=0` e um `API.Save()`. Em seguida, a divergência manual `LaudoObs`→`LaudoObs1` bloqueou com `ApiSaveCount=0`, e a restauração produziu reencontro sem diferença | **Passou; caminho positivo, guard e restauração** |
| Sync | somente BC | Não foi encontrado relatório/Output de uma execução de Sync isolada nesse perfil | **Não comprovado** |
| Sync | somente List | Não foi encontrado relatório/Output de uma execução de Sync isolada nesse perfil | **Não comprovado** |
| Sync | BC + List | Delta `NotaFiscalObs2` 40→41; `FinalApiWriter='List'`, `ApiSaveCount=1`, `Atualizados=14`, `Bloqueados=0`; Output confirmou consumidores antes do API Object | **Passou** |
| Sync | BC selecionado sem BC habilitado na Transaction | Não foi encontrada execução distinta desse guard no Sync. O aviso de dependência visto no Wizard não substitui este caso | **Não comprovado** |

## Linha do tempo que evita a confusão

1. O primeiro teste de Sync foi planejado como “sem BC/List”, com um delta em
   `NotaFiscalObs`, mas o relatório mostrou as Procedures de BC e List e writer
   final `List`. Ele foi corretamente reclassificado na própria sessão como
   `BC + List`.
2. O Wizard `BC-only` foi executado e passou após a correção do preflight de
   metadata que havia causado uma execução parcial anterior.
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

## O que foi anotado e o que não foi

O documento de aceite da F1 e o Changelog registraram corretamente os quatro
perfis do Wizard e o Sync `BC + List`, mas não mantiveram uma matriz explícita que
separasse as duas famílias. A frase posterior “não há outro teste obrigatório”
foi ampla demais: ela confundiu a matriz positiva do Wizard com a matriz de Sync
do plano. A nova evidência fecha o caminho positivo sem BC/List e o guard que
motivou a correção, mas não substitui os três perfis de Sync ainda ausentes.

Build All nos dois environments e o teste HTTP de Update são evidências
complementares. Eles não substituem os casos manuais de Sync definidos na seção
7 do plano da F1. A evidência atual de `LaudoObs` cobre o caminho positivo sem
BC/List, o bloqueio de divergência manual e o reencontro idempotente; somente BC,
somente List e BC sem habilitação na Transaction continuam sem captura separada.

## Fonte e limite da reconstrução

A reconstrução foi feita a partir do histórico local da sessão, que preserva as
mensagens, os textos de relatório/Output e as referências às capturas. Esse
histórico está fora do repositório e não deve ser tratado como artefato portátil;
este documento registra apenas a conclusão auditável e não depende da permanência
dos arquivos temporários de captura.

Plano confrontado: `2026-09-04-B111-F1-PLANO-ORDEM-E-WRITER-FINAL.md`, seções 6.2
e 7.
