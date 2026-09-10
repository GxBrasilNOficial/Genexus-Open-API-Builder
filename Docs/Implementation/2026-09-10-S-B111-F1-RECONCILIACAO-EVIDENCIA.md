# S-B111 F1 — Reconciliação da evidência manual na IDE

## Conclusão

Esta reconciliação confronta o plano da F1 com o histórico completo da sessão de
2026-09-09/10 e com os relatórios e Outputs enviados durante os testes. O trabalho
manual não foi perdido: os quatro fluxos positivos do Wizard foram exercitados, e
há evidência de um Sync bem-sucedido com `BC + List`. O problema foi de promoção
documental: esses testes do Wizard foram tratados como se também fechassem a
matriz específica do Sync.

O registro correto é, portanto:

- a implementação da F1 está concluída e recebeu validação manual parcial;
- os quatro perfis positivos do Wizard estão registrados como exercitados;
- o Sync `BC + List` está registrado com relatório e Output;
- não há evidência separada, na sessão, dos outros três perfis de Sync nem do
  bloqueio de Sync com BC selecionado em Transaction sem BC habilitado;
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
| Sync | sem BC/List | Foi o primeiro cenário planejado, mas o relatório/Output mostrou BC e List; a execução foi reclassificada como `BC + List` | **Não comprovado como caso separado** |
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

## O que foi anotado e o que não foi

O documento de aceite da F1 e o Changelog registraram corretamente os quatro
perfis do Wizard e o Sync `BC + List`, mas não mantiveram uma matriz explícita que
separasse as duas famílias. A frase posterior “não há outro teste obrigatório”
foi ampla demais: ela confundiu a matriz positiva do Wizard com a matriz de Sync
do plano.

Build All nos dois environments e o teste HTTP de Update são evidências
complementares. Eles não substituem os casos manuais de Sync definidos na seção
7 do plano da F1.

## Fonte e limite da reconstrução

A reconstrução foi feita a partir do histórico local da sessão, que preserva as
mensagens, os textos de relatório/Output e as referências às capturas. Esse
histórico está fora do repositório e não deve ser tratado como artefato portátil;
este documento registra apenas a conclusão auditável e não depende da permanência
dos arquivos temporários de captura.

Plano confrontado: `2026-09-04-B111-F1-PLANO-ORDEM-E-WRITER-FINAL.md`, seções 6.2
e 7.
