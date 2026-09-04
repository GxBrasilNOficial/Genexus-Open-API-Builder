# Plano — API Object gravado uma única vez, ao fim do pipeline

## Situação

Plano aprovado para implementação, ainda **não implementado**. Nasceu do incidente de 2026-09-03
na `NotaFiscal` (Sync com resolução Keep interrompido, seguido de bloqueio por baseline), passou
por sete rodadas de revisão em papel e por uma bateria de medição na IDE em 2026-09-03/04.

Evidência de linha de base: [`2026-09-04-EVIDENCIA-IDE-DRIFT-API-OBJECT.md`](2026-09-04-EVIDENCIA-IDE-DRIFT-API-OBJECT.md).

Antecedente do diagnóstico: seção «Escrita parcial do BC — drift API Object ↔ metadata» em
[`B085-SINCRONIZAR-COM-TRANSACTION.md`](B085-SINCRONIZAR-COM-TRANSACTION.md).

## 1. A dor

Se a geração ou a sincronização for interrompida na janela entre a gravação do API Object e a da
metadata, três coisas acontecem:

1. o API Object fica **inconsistente com as Procedures** — declara um serviço sem parâmetro
   algum delegando a uma Procedure que tem seis;
2. a Transaction fica **bloqueada** por `BaselineServiceSourceHashMismatch`;
3. a tentativa natural de reparo — reabrir o Wizard e aplicar — **degrada a KB ainda mais**,
   porque o bloqueio atinge só o API Object e as demais etapas seguem gravando a partir de um
   plano derivado do estado corrompido.

A janela medida é de **cerca de 49 segundos** numa Transaction de porte real, e a interrupção não
precisa ser uma falha: o botão Cancelar basta, e há também um bug intermitente que interrompe
sozinho.

## 2. Como o mecanismo funciona hoje

A extensão grava, para cada Transaction: Procedures (`proc<Nome>_API_*`), um API Object
(`api<Nome>`) e um File de metadata (`api<Nome>_Metadata`).

O File guarda um **baseline** de cinco cláusulas: versão do formato, GUID do API Object,
sentinela de Description, hash do Service Source e hash das descrições geradas. As cláusulas
sensíveis a conteúdo derivam todas do estado do API Object. Antes de qualquer escrita nova, o
preflight compara o estado atual contra esse baseline; divergência bloqueia. **O objetivo do
baseline é detectar edição manual do API Object feita fora da ferramenta.**

Existem três variantes de Service Source reconhecidas como próprias: **B054** (gravada na criação
do API Object), **B055** (gravada pela etapa de Business Component) e **B070** (gravada pela
etapa de List, com paginação e filtros). As três são aceitas como gerenciadas.

O pipeline executa, em Wizard e em Sincronizar:

1. SDTs
2. Procedures
3. criação/reencontro do API Object
4. **etapa de Business Component** — grava o API Object em B055 e as Procedures
5. **etapa de List** — regrava o mesmo API Object em B070 e grava a Procedure de List
6. metadata — registra o hash do Service Source atual

## 3. Fatos apurados

### 3.1 Por leitura de código

**3.1.1 Ordem de gravação nos dois writers.** `ApiPlanBusinessComponentWriter.Apply` monta os
passos começando pelo API Object, seguido de Get, Create, Update e opcionalmente Delete.
`ApiPlanListProcedureWriter.Apply` faz o mesmo com dois passos. Em ambos o API Object é gravado
**primeiro**.

**3.1.2 A interrupção do pipeline é estrutural.** Cada etapa é seguida de um teste do seu
resultado que, em caso de falha, exibe o relatório e retorna, registrando que as seguintes não
foram aplicadas. Não existe caminho em que a etapa de Business Component seja selecionada, falhe,
e a de List rode em seguida.

**3.1.3 Não há dependência de dados entre os passos de gravação.** `SaveApi` altera e relê apenas
o API Object; `SaveProcedure`, apenas a Procedure. A mutação in-memory do API Object acontece
dentro de `SaveApi` — com a gravação adiada, uma falha anterior não deixa objeto sujo pendente.

**3.1.4 O writer de List depende do API já gravado em dois pontos.** Primeiro, a decisão
`includeBusinessComponentParameters`, calculada lendo o API persistido no início do método.
Segundo, o reencontro confirma a posse, e essa confirmação bifurca conforme o File de metadata
exista: com metadata, resolve por schema, nome e identificadores; **sem** metadata — caso da
primeira geração —, cai num fallback sensível à variante do Source. O plano sobrevive porque esse
reconhecimento aceita B054.

**3.1.5 Os writers não criam objetos.** `FindProcedure` e `FindApi` exigem posse comprovada e
lançam se não reencontrarem. Quando executam, Procedures e API Object já existem.

**3.1.6 Os preflights de conteúdo estão inativos nos fluxos vivos.** Todos estão condicionados a
a escrita não ser declarada como alteração deliberada, e **todos os call sites reais declaram**.
O portão que de fato executa no writer de Business Component é a verificação dos Events, que
aceita Events vazios, na forma esperada, ou em formas anteriores conhecidas.

**3.1.7 A variável de status HTTP é incondicional no writer de List.** Ela está na lista base, não
depende da flag de parâmetros de Business Component — portanto esse writer sempre grava os Events
do API Object.

**3.1.8 As duas etapas são governadas por booleanos independentes** no Wizard: aplicar Business
Component depende de aptidão, confirmação e serviços marcados; aplicar List depende só de List
estar marcado. "List sem Business Component" é estado legítimo. **No Sincronizar isso não vale**:
lá as seleções são derivadas da metadata, com metadata e API Object sempre ligados.

**3.1.9 A metadata é sempre a última gravação** — ela depende do estado já persistido do API
Object.

**3.1.10 Posse e integridade são gates distintos.** Posse responde "este API Object é meu";
integridade responde "o estado atual é o que gravei". O gate de **entrada** de Wizard e Sincronizar
é o de **integridade**, avaliado contra o último baseline e independente do plano novo. Resolver a
posse não desbloqueia nada.

**3.1.11 Existe proteção contra rebaixar contrato REST.** Ao reencontrar um API Object que não
está na variante de Business Component, a etapa de criação verifica e **lança** se o Source
existente já for um contrato REST completo, em vez de rebaixá-lo em silêncio.

**3.1.12 Aplicar as etapas com a gravação de metadata desmarcada produz drift deliberado**, por
configuração e sem falha alguma. Vale **apenas no Wizard**.

**3.1.13 Não existe projeto de testes .NET.** A solution tem um único projeto; a suíte é
inteiramente de contract marks em PowerShell sobre o texto dos `.cs`.

**3.1.14 Existem dois callbacks de escrita de SDT.** O da etapa dedicada de SDTs emite na Output
e alimenta o relatório; o usado nas etapas de Business Component e de List **só** alimenta o
relatório.

**3.1.15 A função de escrita na Output força a exibição do painel** ao final. Hoje isso só ocorre
nas fronteiras de etapa.

### 3.2 Por medição na IDE

Detalhe completo no documento de evidência. Aqui, só o que sustenta decisões deste plano.

**3.2.1 A janela é de ~49 segundos.** Tempos por etapa numa Transaction com 44 SDTs próprios e
subníveis: API Object 2,9 s; Business Component 38,5 s; List 8,9 s; metadata 2,1 s.

**3.2.2 A ordem de gravação é observável ao vivo, mas não fica registrada.** A janela de andamento
exibe contador e nome do objeto (`1/4 api<Nome>`, `2/4 proc<Nome>_API_Get`, …). Foi assim que o
estado divergente foi produzido de propósito. Esses rótulos são efêmeros; nada disso chega à
Output.

**3.2.3 O estado intermediário é inconsistente, não incompleto.** Com o API Object na variante de
Business Component, o serviço List é emitido como `List() => proc<Nome>_API_List();` — sem
argumento — enquanto a Procedure tem seis parâmetros.

**3.2.4 CASCATA DE DEGRADAÇÃO.** Reabrir o Wizard sobre o estado divergente produz: bloqueio da
etapa de API Object; desligamento das etapas dependentes; e execução normal das demais. O Wizard
monta o contrato **lendo o API Object** para descobrir os filtros — que estava degenerado —, então
o plano vem sem filtros, o writer de SDT trata o membro real como sobra e o remove, e as
Procedures são regravadas com o contrato empobrecido. Resultado observado:
`SuccessWithWarnings`, cinco objetos atualizados, **zero bloqueados**.

**3.2.5 O hash do File de metadata não é estável** entre gravações do mesmo plano — mesmo tamanho,
mesmo hash de contrato planejado, hash de arquivo diferente.

**3.2.6 A remoção funciona a partir do estado degradado**: 50 objetos removidos, zero bloqueados,
zero avisos, preservando SDTs compartilhados, Folder reutilizado e Business Component.

**3.2.7 Há um bug intermitente na etapa de Business Component** —
`Collection was modified; enumeration operation may not execute` — que interrompe o Apply sem
cancelamento e não se reproduziu na execução seguinte. **Fora do escopo desta frente**, com item
próprio; importa aqui por mostrar que a interrupção não depende do usuário.

## 4. Mudança proposta

1. Em `ApiPlanBusinessComponentWriter.Apply`, reordenar os passos: Procedures primeiro, API Object
   por último.
2. Em `ApiPlanListProcedureWriter.Apply`, reordenar: Procedure primeiro, API Object por último.
3. Acrescentar ao writer de Business Component um parâmetro `deferApiSave`. Quando verdadeiro,
   essa etapa **não grava** o API Object; a gravação fica a cargo da etapa de List.
4. Nos dois call sites do orquestrador, ligar `deferApiSave` quando **as duas** etapas forem
   aplicadas — Business Component **e** List.
5. Passar ao writer de List o valor de `includeBusinessComponentParameters` explicitamente como
   verdadeiro **somente nesse mesmo caso**. Em qualquer outro — inclusive "List sem Business
   Component" — o writer continua deduzindo o valor do API Object persistido, como hoje.

   A condição de guarda é **"a etapa de Business Component rodou"**, não "a etapa de List será
   aplicada". Amarrar o valor forçado ao booleano do List produziria, no caminho "List sem
   Business Component", um API Object declarando serviços que delegam a Procedures inexistentes e
   referenciando SDTs que só a etapa de Business Component cria.

6. Ajustar o relatório final para atribuir a atualização do API Object à etapa que de fato o
   gravou. Move-se **apenas a autoria**; a designação do API Object como objeto principal
   permanece, porque usa o identificador do objeto reencontrado.

7. Emitir na Output **uma linha por objeto gravado**, nos dois writers, para que a ordem efetiva
   fique registrada (fato 3.2.2). Especificação:

   - **7a.** O molde é o callback de escrita de SDT da **etapa dedicada de SDTs**, que emite na
     Output e alimenta o relatório — **não** o homônimo das etapas de Business Component e de
     List, que só alimenta o relatório (fato 3.1.14).
   - **7b.** A emissão é por callback **dentro do laço**, não por lista de resultados devolvida ao
     orquestrador: quando o writer lança no meio, não haveria lista, e é justamente nas
     interrupções que a evidência importa.
   - **7c.** A linha sai **depois** de a gravação retornar. Não emitir linha "gravando" antes: o
     objeto que falhou já é nomeado pela mensagem de erro, e última linha emitida mais linha de
     erro dão o ponto de parada sem ambiguidade.
   - **7d.** A linha carrega o **rótulo da etapa**, para que item 6 e item 7 contem a mesma
     história sobre autoria.
   - **7e.** Alimenta **somente** a Output, nunca o relatório final — as Procedures já entram no
     relatório por outro caminho e seriam duplicadas.
   - **7f.** Usa uma variante de escrita que **não força a exibição** do painel (fato 3.1.15);
     sete chamadas de dentro do laço, com o diálogo modal aberto, disputariam primeiro plano.
   - **7g.** Limitação aceita: os SDTs gravados dentro dessas duas etapas continuarão sem aparecer
     na Output (fato 3.1.14). A trilha terá um intervalo sem SDTs; quem ler a captura precisa
     saber que é esperado.

**Resultado.** Num pipeline com Business Component e List, o API Object passa a ser gravado **uma
única vez pelos writers**, já na variante final, imediatamente antes da metadata. Numa geração
nova continua havendo a persistência da etapa de criação — a promessa é "uma gravação pelos
writers", não "uma persistência em toda a geração".

## 5. Testes automatizados

Contract marks, nos arquivos de teste já registrados no orquestrador de checagens pré-push —
**sem criar gate novo**:

- a ordem dos passos, com o API Object por último, nos **dois** writers;
- a existência do caminho `deferApiSave` e o fato de a etapa de Business Component não gravar o
  API Object quando ele está ligado;
- a **quantidade** de gravações do API Object em cada writer, não só a presença dos trechos;
- a guarda do item 5 dependendo de a etapa de Business Component ter rodado;
- para o item 7: emissão depois da gravação, rótulo de etapa presente, ausência de alimentação do
  relatório, e uso da variante sem exibição forçada.

**Hospedagem.** As asserções sobre a guarda vivem no orquestrador, e o teste do writer de List
compila classes via `Add-Type` sem ler o texto daquele writer. O hospedeiro adequado para as
asserções de ordem e de guarda é o teste do relatório final de aplicação, que já lê os três
arquivos e já está registrado no checker.

**Eficácia** verificada por mutação em cada asserção.

**Critérios de saída:** compilação em Release sem avisos novos e execução do orquestrador de
checagens pré-push, além dos contract marks. Build All e chamada HTTP **não** são gate desta
frente — ela altera ordem de persistência, não o contrato REST gerado; valem como conferência de
não-regressão.

## 6. Validação na IDE

Antes de qualquer captura, confirmar que a DLL instalada corresponde ao commit revisado.

Em cada ponto de medição, registrar a **variante** do Service Source, não apenas o hash. A leitura
direta da KB é o instrumento mais confiável: o serviço List com paginação e filtro indica a
variante final; sem parâmetro algum, a da etapa de Business Component.

**Não** comparar o hash do File de metadata entre execuções para concluir que nada mudou (fato
3.2.5); o que se verifica é se a metadata foi ou não regravada.

| Cenário | O que faz | O que prova |
|---|---|---|
| A | Reproduzir o incidente: conflito com Keep, falha na Procedure, Replace imediato | Ausência de bloqueio por baseline e hashes finais alinhados — **não** "o Replace resolveu o conflito" |
| B1 | Cancelar **antes** da gravação do API Object | API Object intocado, baseline íntegro |
| B2 | Cancelar ou falhar **depois** da gravação do API Object e antes da metadata | A janela residual da seção 7, documentada |
| C | Sync completo bem-sucedido | Hipótese H1 (o SDK aceita Procedure nova com API no contrato antigo), com **ordem observável** lida da trilha do item 7 |
| D | "List sem Business Component" | Não-piora: conferir **quais serviços** o Source declara, não só a ausência de parâmetros |
| F | "Business Component sem List" | API Object gravado depois das Procedures, terminando na variante de Business Component, metadata alinhada |
| G | API preexistente na variante de List **sem** parâmetros de BC, reaplicado com as duas etapas | Que a guarda do item 5 é necessária: sem ela o resultado seria um Source sem os parâmetros logo após o usuário pedir Business Component |
| H | API Object editado à mão | Não-regressão: a detecção de edição manual continua bloqueando |
| I | Repetir a interrupção que hoje gera o estado divergente e reaplicar | **Não propagação**: plano mantém os filtros, nenhum SDT divergente por "membros extras", Procedures não regravadas com contrato reduzido |
| J | Sincronizar a partir do estado divergente | Pendente **também como linha de base** — ver seção 8 |

**Inventário do estado parcial:** em toda interrupção, registrar também quais Procedures e SDTs
ficaram persistidos. Esta frente protege o alinhamento entre API Object e baseline; ela **não**
promete consistência global da geração.

**Receita do cenário G** (a redação ingênua é inexecutável — com um plano só de List, os Events
persistidos não casariam com nenhuma forma aceita e a etapa de Business Component lançaria antes
de medir):

1. Wizard sobre Transaction com Business Component **desabilitado**, marcando Get, Create, Update
   e List. Gerar. Resultado: API na variante de List sem parâmetros de BC, com Events na forma dos
   quatro serviços.
2. Habilitar Business Component na Transaction.
3. Sincronizar, ou reabrir o Wizard, marcando **as duas** etapas.

O estado final do cenário D é o inicial do G — encadeie os dois.

**Pré-condição do cenário H:** a metadata da KB de teste precisa ter o bloco de integridade
preenchido; sem ele a verificação devolve compatível sem olhar nada e o cenário passaria vazio.

**Cenário de metadata sem mecanismo determinístico:** não há como fazer a gravação da metadata
falhar sob comando. A janela API Object → metadata fica como limitação documentada; se o cenário
B2 ocorrer espontaneamente, serve de evidência oportunista.

## 7. Residual não resolvido por esta frente

Com a mudança, o API Object deixa de ter estado intermediário durante o pipeline. Sobra a janela
entre a gravação do API Object e a da metadata: se esta falhar logo depois, o baseline fica
desatualizado.

A janela não se fecha invertendo a ordem, porque a metadata precisa do hash do objeto já
persistido. **Não** é correto chamá-la de irredutível: continuam concebíveis o cálculo do baseline
a partir do conteúdo planejado, um marcador de geração pendente, reparo idempotente da metadata ou
um protocolo de recuperação. Todas mudam o contrato do baseline e enfraquecem a detecção de edição
manual, que é o propósito dele — por isso ficam fora daqui.

Ponto de partida concreto para quem retomar: `ApiPlanMetadataFileWriter` já constrói, a partir do
plano, o conjunto de Service Sources compatíveis esperados. **Ressalva:** esse conjunto alimenta a
integridade de posse, e **não** o gate de entrada, que compara o hash literal sem tolerância —
portanto o backlog é mais caro do que a existência dessa maquinaria sugere.

**Trade-off aceito.** Na janela de falha parcial, o artefato intermediário fica na variante de
criação em vez da variante de Business Component. As Procedures já estarão no contrato novo,
enquanto o API Object declara serviços sem argumento — inconsistência de aridade, provavelmente
não especificável. Ocorre sempre que o API Object estiver nessa variante no início da aplicação, e
a recuperação é reexecutar (a posse resolve nos dois ramos: sem metadata, pelo fallback que aceita
a variante de criação; com metadata, por schema, nome e identificadores).

A frente considera o trade-off favorável, e a medição reforçou: o caminho atual **também** deixa
estado inconsistente (fato 3.2.3) e ainda o **propaga** na tentativa de reparo (fato 3.2.4). A
troca é entre uma inconsistência que se propaga e uma que não se propaga.

## 8. Pendências

- **Cenário J**, como linha de base: a mensagem exibida ao abortar recomenda "Remover / Wizard /
  Sync" para reparar. Está medido que Remover repara (3.2.6) e que o Wizard degrada (3.2.4). O
  comportamento do Sincronizar não foi medido e não é inferível — ele percorre outro caminho, com
  seleções derivadas da metadata. Se também degradar, a mensagem recomenda dois caminhos que
  pioram a situação, o que é defeito de orientação independente desta frente.
- **Item próprio** para o bug intermitente (3.2.7).
- **Item próprio** para o comportamento de bloqueio parcial que permite às demais etapas gravarem
  a partir de um plano derivado do estado bloqueado (3.2.4) — questionável por si, mesmo sem esta
  frente.

## 9. Hipóteses e seu estado

| # | Hipótese | Estado |
|---|---|---|
| H1 | O SDK aceita gravar Procedure nova enquanto o API Object está no contrato antigo | **Não demonstrada.** Única que depende da IDE; exercitada pelo cenário C, no caminho feliz |
| H2 | Não tocar o API Object mantém o baseline **inteiro** alinhado | Confirmada por leitura: as cláusulas sensíveis a conteúdo derivam do API Object. Escopo: fala do alinhamento com o baseline, **não** de consistência global da geração |
| H3 | O estado pós-falha é aceito na reaplicação pelos preflights de conteúdo | **Retirada.** Esses preflights não executam em fluxo vivo (3.1.6) |
| H4 | Adiar a gravação não quebra a etapa de List | Sustentada por leitura: o portão que executa aceita Events vazios; a variável de status é incondicional; o valor da flag passa a ser explícito; o Folder acompanha a gravação. As conferências de leitura única, igualdade de Folder e ausência de outros dados persistidos na montagem **foram todas respondidas por código** |
| H5 | Contract mark é a única verificação capaz de travar ordem e guarda | Confirmada, com a ressalva de que compilação e checker pré-push continuam sendo gates. O que não é automatizável aqui é o comportamento do SDK |

## 10. Apêndice — o que foi refutado ao longo da revisão

Registrado para não ser reintroduzido.

- **"A reordenação pode deixar Procedures órfãs antes de existir o API Object."** Não é estado
  alcançável: os writers não criam objetos (3.1.5).
- **"A remoção da API em estado parcial é um gate desta frente."** O ponto é real mas
  pré-existente e ortogonal — a metadata sempre foi a última gravação. E a medição mostrou que a
  remoção funciona a partir do estado degradado (3.2.6).
- **"Extrair a montagem dos passos para um seam testável."** Não há projeto de testes .NET onde
  exercitá-lo (3.1.13); criar essa infraestrutura é outra frente.
- **"A guarda deve representar execução real da etapa, não a seleção do usuário."** O cenário que
  a motivaria — Business Component selecionado, falhando, e List rodando em seguida — não é
  alcançável: a interrupção do pipeline é estrutural (3.1.2).
- **"A janela residual é irredutível."** Não demonstrado; ver seção 7.
- **"A recuperação funciona porque a posse resolve."** Errado: confunde dois gates. Funciona
  porque o API Object não foi tocado e as cláusulas continuam batendo (3.1.10).
- **"A ordem de gravação é inobservável."** Errado: é observável ao vivo; o que falta é registro
  (3.2.2).
- **Build All e chamada HTTP como critério de saída.** A frente não altera o contrato REST
  gerado; ficam como conferência de não-regressão.
