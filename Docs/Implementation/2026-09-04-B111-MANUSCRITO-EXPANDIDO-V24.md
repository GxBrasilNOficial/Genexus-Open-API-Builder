# Manuscrito expandido v24 — API Object gravado uma única vez (`B111`)

> **Status em 2026-09-05: SUPERADO. Não implementar por este documento.**
>
> Este manuscrito foi produzido nas rodadas de revisão por pares de 2026-09-03/04 como
> **expansão** do plano então aprovado
> ([`2026-09-04-PLANO-API-OBJECT-GRAVACAO-UNICA.md`](2026-09-04-PLANO-API-OBJECT-GRAVACAO-UNICA.md)),
> ampliando o escopo de "reordenar as gravações" para diário durável, seam de persistência,
> máquina de estados de três dimensões e remoção de API legado. **Nunca foi promovido a plano
> aprovado.**
>
> Viveu até 2026-09-05 apenas em `Temp/`, fora do controle de versão, e foi promovido ao
> repositório nessa data para que as referências dos planos de fase tenham um alvo rastreável.
>
> **Quatro afirmações deste manuscrito foram desmentidas por medição** — identidade do API,
> localização do diário, releitura por GUID e frequência de gravação. Ver a seção 7 de
> [`2026-09-04-B111-F3-PLANO-DURABILIDADE-E-REMOCAO.md`](2026-09-04-B111-F3-PLANO-DURABILIDADE-E-REMOCAO.md)
> e as medições em
> [`2026-09-04-B111-SONDAS-IDENTIDADE-E-DIARIO.md`](2026-09-04-B111-SONDAS-IDENTIDADE-E-DIARIO.md).
>
> **Plano vigente:** a sprint `S-B111`, em três documentos de fase — F1, F2 e F3. Este
> manuscrito permanece como origem das exigências de seam, recibos, falhas C/D/E e diário, que
> as fases F2 e F3 herdaram e corrigiram.

**Status original (mantido como registro):** manuscrito autocontido para revisão e decisão
humana. **Não** autoriza implementação, alteração de código, instalação, commit ou push.
**Versão:** v24 (2026-09-04).
**Item:** B111 em Docs/Foundation/06-BACKLOG_v0.1.md.

## 0. Decisão de desenho

O objetivo desta frente é preservar a vantagem principal de B111:

> Quando BC ou List for writer do contrato final, B054 prepara o API Object sem
> API.Save(). O API Object é persistido uma única vez pelo último writer do contrato
> final: BC quando só BC participa; List quando List participa.

Quando não houver BC nem List, B054 continua sendo o writer final do fluxo API-only e
persiste o API Object uma vez.

A tabela abaixo considera GenerateApiObject=true, isto é, que B111 é responsável por
persistir o API. Quando GenerateApiObject=false, o API deve existir e ser validado antes
do pipeline; nenhum writer chama API.Save(). O caso independente está definido na
seção 4.4.

| Seleção do fluxo | Preparação B054 | Writer final do API Object | Chamadas físicas a API.Save() |
|---|---|---|---|
| API-only | prepara o contrato B054 | B054 | uma |
| BC-only | prepara sem salvar API | BC | uma |
| List-only | prepara sem salvar API | List | uma |
| BC+List | prepara sem salvar API | List; BC não salva API | uma |

Folder, Transaction, SDTs, Procedures, metadata e diário B111 podem ter persistências
próprias. A contagem acima é exclusivamente de chamadas físicas a API.Save() por uma
aplicação de Sync ou Wizard.

O predicado que decide se B111 assume a orquestração é:

    b111ManagedApply =
        GenerateApiObject ||
        GenerateMetadata ||
        ApplyBusinessComponent ||
        ApplyList

Quando esse predicado é falso, B111 não passa a controlar uma etapa isolada de SDT ou
Procedure apenas porque o usuário a selecionou. Essas etapas mantêm seus contratos
próprios. Quando GenerateMetadata é verdadeiro, o fluxo entra no predicado porque a
metadata depende de um API reencontrável e precisa participar da mesma intenção e da
mesma recuperação.

### 0.1 O que é comum e o que é deliberadamente diferente

O contrato de gravação única, o gate de pré-escrita, a identidade do API, os recibos,
a ordem dos writers e a recuperação de estado ambíguo valem para Sync e Wizard.

O Sync atual não oferece ao usuário a matriz de flags do Wizard. Seu BuildSelection
constrói um perfil fixo:

    GenerateSdts = true
    GenerateProcedures = true
    GenerateApiObject = true
    GenerateMetadata = true
    ApplyList = existe serviço List selecionado
    ApplyBusinessComponent = existe serviço Get/Create/Update/Delete selecionado

Esse perfil fixo é parte explícita do contrato v24. A matriz de
GenerateApiObject=false e as combinações independentes de flags pertencem ao Wizard
nesta frente. Não se deve insinuar que o Sync oferece toggles que ele não oferece.
Expandir a UI do Sync para a mesma matriz é outra frente e não é necessário para
implementar B111 com segurança.

O Sync também não deve salvar automaticamente a Transaction para habilitar BC. Se o
Sync pedir aplicação de BC e a Transaction não estiver habilitada para BC, o fluxo deve
bloquear antes do diário e antes de qualquer primeiro Save(), informando que a
habilitação deve ser feita pelo fluxo apropriado. No Wizard, o callback de habilitação
de BC deve apenas registrar uma mutação pendente; o transaction.Save() só poderá
ocorrer depois do gate e depois da confirmação do diário, como uma etapa explicitamente
recibada. Assim, a diferença de entrada é visível, segura e testável.

### 0.2 Não há atomicidade implícita

Esta frente não promete uma transação SDK multiobjeto nem rollback automático de
Transaction, Folder, SDTs, Procedures, API, metadata ou diário.

No modo A, o plano acrescenta:

1. um gate absoluto antes de qualquer Save() do pipeline;
2. um diário B111 persistente, com a intenção completa, confirmado antes da primeira
   gravação de objeto do plano;
3. uma máquina de estados que separa estado físico do API, estágio lógico do diário e
   durabilidade do diário;
4. recibos de persistência com leitura de confirmação;
5. recuperação que bloqueia uma nova escrita até inventariar e comparar o estado real
   contra a intenção durável.

No modo B, diário e recuperação automática são substituídos por checkpoint manual
bloqueante. Gate, ordem física, identidade do API, recibos, relatório e testes de
falha continuam obrigatórios.

O alvo recomendado é o modo A. A implementação só pode escolher o modo A ou B depois
de decisão humana explícita. Não é aceitável implementar uma mistura silenciosa na qual
o diário existe em algumas execuções, mas critérios de recuperação automática são
alegados quando ele não foi confirmado.

O diário não transforma a KB em transação. Ele torna a intenção e o ponto de falha
reconstruíveis. Se o diário não puder ser criado ou atualizado com segurança, a
aplicação deve abortar antes de gravar qualquer objeto do plano.

## 1. Papel do revisor

Este manuscrito é para revisão independente, não para execução.

O revisor deve:

1. ler o documento inteiro;
2. confirmar fatos no código e nas evidências da seção 14;
3. emitir CONCORDA, REVISA ou REJEITA;
4. procurar regressões em primeira geração, reaplicação, Sync, Wizard, BC-only,
   List-only, BC+List, API-only, flags independentes, API inexistente,
   cancelamento, falha antes e depois de cada Save(), remoção e relatório;
5. verificar que cada regra é implementável sem adivinhar nomes ou identidade;
6. não editar arquivos, implementar, instalar, commitar, fazer push ou despachar outro
   revisor.

O payload desta revisão é público: código e documentação do repositório, sem conteúdo
de uma KB paralela.

## 2. Problema observado e benefício preservado

Na implementação observada, a gravação do API Object ocorre antes das Procedures e, em
alguns fluxos, antes do List. Uma interrupção entre API e metadata pode deixar:

1. API Object incompatível com as Procedures;
2. Transaction bloqueada por BaselineServiceSourceHashMismatch;
3. reabertura do Wizard capaz de derivar uma intenção nova sobre estado divergente;
4. relatório atribuindo um API antigo a uma aplicação que ainda não o persistiu.

A evidência de IDE mediu aproximadamente 49 segundos em uma Transaction com 44 SDTs.
Também observou API B070 com List() incompatível com Procedure parametrizada e uma
degradação em cascata de SDT, Procedures e API quando o Wizard foi reaberto sobre
estado divergente.

O ganho de B111 é deslocar a persistência do API para depois dos consumidores finais.
Esse ganho seria perdido se B054 apenas fosse chamado mais tarde no caminho BC+List,
mas continuasse salvando antecipadamente em BC-only ou List-only.

B109 permanece inconclusivo e não é reaberto nesta frente.

## 3. Fatos atuais e limites da evidência

Os itens abaixo são fatos de trabalho que devem ser confirmados durante a
implementação, sem transformar evidência antiga em prova do gerador novo.

### 3.1 Código relevante

1. ApiPlanApiObjectWriter.CreateOrReencounter prepara Folder, SDTs e Procedures e
   hoje chama API.Save() tanto para API existente quanto para API criada.
2. ApiPlanBusinessComponentWriter começa seus save steps pelo API e depois grava
   Get/Create/Update/Delete.
3. ApiPlanListProcedureWriter começa pelo API e depois grava List.
4. TryApplyBusinessComponent e TryApplyList convertem exceção não-aborto em
   resultado falso; aborto é relançado para o Apply de Sync/Wizard.
5. SaveProcedure e SaveApi podem reler e validar depois do Save() físico.
6. O List deduz parâmetros de BC a partir do contrato persistido do API.
7. TryCreateApiObject registra B054 no relatório e atribui MainObject/ApiName.
8. TryResolveMainObjectFromKb em Package.cs pode reencontrar API somente pelo nome,
   o que não pode ser a prova de identidade em um fluxo de gravação única.
9. Metadata, quando selecionada, é gravada depois do API e depende de um API
   reencontrável.
10. Os testes existentes são predominantemente marks PowerShell; eles não provam por
    si só ordem física, falha depois de Save() ou reaplicação sobre parcial.
11. ApiPlanTransactionSyncOrchestrator.BuildSelection monta o perfil fixo descrito
    em 0.1; o diálogo atual não oferece os mesmos toggles de geração do Wizard.
12. ApiPlanGeneratedApiRemover.Remove hoje localiza metadata, valida alvos e inicia
    a exclusão pelo API; essa ordem não pode permitir exclusão cega de um API legado
    sem diário B111 ou intenção reconstruída.

### 3.2 Evidência histórica

1. A primeira geração observada seguiu SDTs, Procedures-esqueleto, B054/API, BC, List
   e metadata.
2. Essa evidência foi produzida pela DLL do commit 4b9ca09.
3. Não há evidência de transação SDK multiobjeto ou rollback automático.
4. A reabertura do Wizard pode derivar um plano de um API antigo; “executar de novo”
   não é, sozinho, recuperação demonstrada.
5. O escritor atual de metadata não é um diário pré-escrita: ele exige API existente,
   reencontro e validação de posse antes de salvar metadata B060.

Evidência de runtime só vale para a DLL que a produziu e para a data em que foi
capturada. Se um commit posterior alterar ApiPlan*, writers de Source, mapa de
contrato ou plano de SDT, artefatos já presentes na KB não podem ser usados como
medição do gerador vigente sem reinstalar a DLL e reaplicar o Wizard.

## 4. Contrato-alvo da implementação

### 4.1 Integração nas duas árvores de Package.cs

Em ambas as árvores de Package.cs, o Apply deve:

1. construir ou receber uma seleção já validada;
2. calcular b111ManagedApply;
3. se o predicado for falso, sair pelo contrato existente sem assumir a orquestração
   B111;
4. se for verdadeiro, construir uma intenção completa antes do primeiro Save() do
   plano;
5. executar o gate absoluto;
6. no modo A, criar e confirmar o diário B111; no modo B, criar e confirmar o
   checkpoint manual;
7. executar writers na ordem definida;
8. persistir o API Object somente no writer final;
9. registrar recibos e atualizar o diário ou checkpoint a cada etapa confirmada,
   conforme o modo;
10. produzir relatório a partir de identidades e recibos, nunca de nome isolado.

O código não pode manter um caminho antigo que chame B054 com API.Save() antes de
BC ou List quando ApplyBusinessComponent ou ApplyList for verdadeiro.

### 4.2 Perfil fixo do Sync e matriz do Wizard

O contrato de entrada é:

| Entrada | Perfil nesta frente | O que deve ser testado |
|---|---|---|
| Sync | flags de geração sempre verdadeiras; BC/List dependem dos serviços | ordem final, uma persistência de API, gate, diário ou checkpoint, falhas e relatório |
| Wizard | flags de geração independentes; BC/List dependem dos serviços | toda a matriz da seção 4.4, além da ordem e recuperação |

No Sync, GenerateApiObject=false não é uma seleção possível nesta frente. Não criar
uma falsa paridade estática copiando testes do Wizard para uma UI que não os oferece.

No Sync, quando BC foi selecionado mas a Transaction não está habilitada para BC:

1. detectar isso no preflight;
2. produzir mensagem operacional;
3. retornar bloqueio antes de criar diário, alterar Transaction, salvar Folder, SDT,
   Procedure, API ou metadata;
4. não chamar automaticamente transaction.Save().

No Wizard:

1. a seleção de habilitação de BC não pode salvar ao abrir a tela;
2. o callback registra PendingBusinessComponentEnablement;
3. o gate verifica a mutação pendente;
4. depois do diário confirmado, transaction.Save() é uma etapa planejada e recibada;
5. falha ou resultado ambíguo dessa etapa interrompe o pipeline e entra na recuperação,
   sem prosseguir para API.

### 4.3 Gate absoluto antes de qualquer Save

Quando b111ManagedApply for verdadeiro, o gate precisa ocorrer antes de:

- transaction.Save() usado para habilitar BC;
- criação ou atualização do diário B111;
- Folder.Save();
- qualquer SDT.Save();
- qualquer Procedure.Save();
- qualquer API.Save();
- qualquer File.Save() de metadata.

A ordem da lista é intencional: o diário é um objeto do plano e deve ser confirmado
antes dos demais objetos, mas o gate deve antecedê-lo.

O gate deve validar:

1. Transaction resolvida por identidade estável, não apenas por nome;
2. ApplicationId e Transaction GUID coerentes;
3. nome planejado e nome persistido separados;
4. contrato final completo e hash determinístico;
5. serviços selecionados e flags coerentes com a entrada;
6. todos os tipos, domínios, SDTs, Procedures, Folder, API e metadata necessários;
7. colisões de nomes e identidades externas;
8. posse de objetos existentes;
9. alvos de atualização e remoção;
10. capacidade de aplicar a mutação BC, quando houver;
11. disponibilidade do writer final;
12. disponibilidade e integridade do mecanismo de diário no modo A;
13. disponibilidade do checkpoint manual no modo B;
14. ausência de diário B111 anterior em estado parcial, desconhecido ou ambíguo;
15. ausência de API existente com identidade conflitante;
16. que nenhum writer fará descoberta por nome para substituir a identidade planejada.

O gate é uma barreira de escrita, não uma consulta informativa. Se falhar, não deve
haver objeto parcial produzido pelo pipeline.

### 4.3.1 Ordem física obrigatória

Depois do gate e da confirmação inicial do diário no modo A, ou do checkpoint manual
no modo B, a ordem física deve ser:

1. salvar a Transaction somente quando o Wizard tiver uma mutação BC pendente;
2. salvar Folder, se necessária;
3. salvar SDTs selecionados;
4. salvar Procedures-esqueleto e Procedures de consumidores selecionadas;
5. executar BC, quando selecionado;
6. executar List, quando selecionado;
7. fazer exatamente o único API.Save() no writer final: B054 em API-only, BC em
   BC-only, List em List-only e List em BC+List;
8. salvar metadata, quando selecionada;
9. confirmar cada Save; no modo A, atualizar e reler o diário; no modo B, registrar o
   checkpoint manual equivalente; só então avançar.

Os writers podem agrupar etapas internas, mas não podem inverter a dependência:
nenhum consumidor final pode salvar API antes do seu Source/Rules/Variables estarem
finalizados, e metadata nunca precede o API fisicamente confirmado. Quando uma etapa
não foi selecionada, ela é omitida; omitir SDT ou Procedure não autoriza um Save oculto.
No Sync, a etapa 1 nunca é criada implicitamente: se BC exigir habilitação, o preflight
bloqueia conforme 4.2.

### 4.4 Matriz completa do Wizard e caso GenerateApiObject=false

As flags de SDT e Procedure são independentes. B111 não pode forçar
GenerateSdts=true ou GenerateProcedures=true para compensar uma flag falsa.

| GenerateApiObject | BC/List/metadata | API existente e próprio | API ausente | Resultado |
|---|---|---|---|---|
| true | nenhum | qualquer | permitido | B054 é writer final e salva uma vez |
| true | BC ou List | qualquer | permitido | writer final cria e salva uma vez |
| true | metadata | qualquer | permitido se o contrato criar API | writer final cria API; metadata vem depois |
| false | nenhum | obrigatório | bloqueio antes do primeiro Save | não criar API; não usar B054 como atalho |
| false | BC ou List | obrigatório | bloqueio antes do primeiro Save | writer de serviço usa API próprio existente e não salva API |
| false | metadata | obrigatório | bloqueio antes do primeiro Save | metadata não cria API implicitamente |
| false | BC+List+metadata | obrigatório | bloqueio antes do primeiro Save | todos os consumidores usam o mesmo API próprio |

“API próprio” significa posse validada por identidade e contrato, não apenas nome igual
ou descrição parecida. Se houver mais de um candidato, ou se a identidade não puder
ser confirmada, o resultado é bloqueio/OutcomeUnknown conforme o momento, nunca escolha
arbitrária.

Se GenerateApiObject=true e GenerateSdts=false ou GenerateProcedures=false, o writer
deve respeitar essas flags. Ele pode exigir que dependências já existam e estejam
coerentes, mas não pode salvá-las ocultamente.

No Sync, somente as linhas equivalentes a GenerateApiObject=true,
GenerateSdts=true, GenerateProcedures=true e GenerateMetadata=true são aplicáveis.
Os testes de false acima devem declarar explicitamente que são Wizard-only.

### 4.5 B054 como preparação, não como writer antecipado

Quando BC ou List participar:

1. B054 constrói o contrato em memória;
2. calcula nome, identidade planejada, Source, Rules, variáveis e referências;
3. prepara Folder, SDTs e Procedures conforme as flags;
4. não chama API.Save();
5. não cria uma segunda API temporária na KB;
6. entrega contexto transient ao writer final;
7. deixa separado o PlannedApiName do nome e GUID realmente persistidos.

O contexto transient é um contrato interno explícito, por exemplo:

    ApiPlanTransientApiContext

Ele deve carregar ao menos:

- ApplicationId;
- Transaction GUID;
- PlannedApiName;
- PlannedApiGuid, quando o SDK permitir identidade estável antes do Save;
- NewApiIdentityKey, quando não permitir;
- Description planejada;
- Source, Rules e Variables finais;
- hash do contrato;
- objetos preparados e suas flags;
- writer final esperado;
- vínculo com o diário B111.

ApiPlanBusinessComponentWriter e ApiPlanListProcedureWriter não podem rejeitar o
contexto transient chamando FindApi, API.GetAll ou equivalente como condição de
validação. Para um API existente, a busca deve usar identidade validada. Para um API
novo, o objeto transitório é a fonte autorizada até o único Save final.

### 4.6 Diário B111 durável no modo A — alternativa histórica

> **Registro histórico, superado em 2026-09-07:** esta seção documenta a alternativa de
> diário com nome derivado por hash. Ela foi preservada como origem do manuscrito, mas não
> é contrato operacional. O contrato aprovado usa um único File por KB, com o nome fixo
> `GxOpenApiBuilder_OperationJournal`, sem módulo, e recuperação explícita na F3.

O diário B111 é um objeto File próprio da KB, distinto da metadata de negócio. Seu
nome determinístico é:

    B111_J_ + primeiros 24 caracteres hexadecimais de
    SHA-256(TransactionGuid em minúsculas invariant + separador + PlannedApiName)

PlannedApiName é conhecido antes do primeiro Save e deve ser normalizado de forma
determinística. Essa chave identifica o File do diário, mas não prova posse nem
identidade do API. ApplicationId, Transaction GUID, contrato e identidade continuam
obrigatórios no conteúdo e na validação. O nome não pode depender do nome que só será
descoberto depois do API persistido.

O diário deve conter:

- schema version;
- Transaction GUID;
- ApplicationId;
- nome da Transaction;
- API planejado e, quando conhecido, API persistido;
- PlannedApiGuid ou NewApiIdentityKey;
- PersistedMainObjectName e GUID persistido, quando confirmados;
- hash completo do contrato;
- flags de SDT, Procedure, API e metadata;
- serviços BC/List selecionados;
- Folder planejada;
- SDTs, Procedures e metadata planejados;
- estado físico do API;
- estágio lógico da aplicação;
- estado de durabilidade do próprio diário;
- lista de receipts;
- sequência de writes;
- versão da DLL/gerador, quando disponível;
- modo A;
- LegacyImported para diário reconstruído durante remoção;
- (proposta histórica, não vigente) histórico de ApplicationId e reaplicações;
- timestamps e mensagens de bloqueio.

Antes da primeira gravação de Transaction, Folder, SDT, Procedure, API ou metadata:

1. criar o diário;
2. salvar o diário;
3. reler pelo identificador do File;
4. confirmar Transaction GUID, ApplicationId, hash e intenção;
5. somente então iniciar a primeira gravação de objeto do pipeline.

Se a criação, confirmação ou atualização segura do diário falhar, abortar antes de
gravar qualquer objeto do pipeline. Um diário que existe mas não pôde ser confirmado
fica em JournalDurabilityUnknown; não se deve prosseguir nem criar um segundo diário.

Não reutilizar um File de outra Transaction, outro ApplicationId ou outro contrato.
Ao abrir um fluxo, enumerar todos os diários B111 e procurar o Transaction GUID. Se
houver mais de um candidato, ou se um candidato próprio estiver em Partial,
OutcomeUnknown, RemovalInProgress, RemovalPartial ou JournalDurabilityUnknown,
bloquear para recuperação explícita.

O diário só pode ser considerado reutilizável quando for próprio, íntegro, tiver
intenção compatível e estiver em Completed ou Removed, conforme a operação.
ApplicationId ativo e histórico devem ser comparados; não sobrescrever o histórico
silenciosamente.

### 4.7 Identidade de API novo e identidade persistida

Antes do único API.Save() de um API novo, registrar a identidade no diário no modo A
ou no checkpoint manual no modo B:

1. PlannedApiGuid, se o SDK fornecer GUID estável antes do Save; ou
2. NewApiIdentityKey, um identificador novo e único, criado antes do Save.

Além disso, a Description do API novo deve manter a descrição humana canônica e incluir
um marcador exato, completo e parseável:

    GOAB-B111-IDENTITY;TransactionGuid=<guid>;ApplicationId=<id>;ContractHash=<sha256>

O helper de identidade deve:

- montar a descrição sem perder a descrição humana;
- verificar limite de tamanho antes de salvar;
- rejeitar truncamento ou marcador incompleto;
- reconhecer somente o prefixo e os campos exatos;
- validar GUID, ApplicationId e hash;
- não alterar o significado de ApiPlanOwnedObjectDescription.IsCanonical;
- preservar o fallback histórico de posse apenas onde o contrato antigo ainda o exigir,
  sem usá-lo como prova suficiente para reaplicação B111.

Depois do Save, o writer deve reler o objeto pela identidade planejada ou pela
NewApiIdentityKey/marcador e confirmar:

- GUID;
- nome;
- Description;
- Transaction GUID;
- ApplicationId;
- hash do contrato;
- Source, Rules e Variables;
- posse.

O resultado persistido deve ser guardado separadamente como:

    PersistedMainObjectName
    PersistedMainObjectGuid

É proibido tratar PlannedApiName como se fosse o nome persistido ou usar uma busca
por nome como substituto de identidade.

### 4.8 Três dimensões de estado

O estado não pode ser comprimido em um único enum. O plano deve registrar e reportar,
separadamente:

1. estado físico do API Object;
2. estágio lógico da aplicação no diário;
3. durabilidade conhecida do diário.

A tabela abaixo define os estados duráveis do modo A. No modo B, o estágio lógico é
registrado no checkpoint e a terceira dimensão é ManualOnly/confirmado pelo humano,
sem ser apresentada como durabilidade de diário.

| Estado físico do API | Estágio lógico do diário | Durabilidade do diário | Interpretação |
|---|---|---|---|
| Absent | Planned | Confirmed | intenção confirmada; API ainda não salvo |
| PlannedOnly | Planned | Confirmed | identidade planejada; nenhum Save confirmado |
| OutcomeUnknown | ApiSaveOutcomeUnknown | Confirmed | chamada de API teve resultado não determinável |
| PhysicallySaved | ApiPhysicallySaved | Confirmed | API relido e confirmado; pipeline pode ainda estar parcial |
| PhysicallySaved | PipelinePartial | Confirmed | API confirmado; algum consumidor ou metadata falhou |
| PhysicallySaved | ApiPhysicallySaved ou PipelinePartial | Unknown | API confirmado, mas atualização do diário não confirmada |
| PhysicallyAbsent | PreApiPartial | Confirmed | falha antes do API; outros objetos podem estar parciais |
| Any | OutcomeUnknown | Unknown | não é seguro inferir nem continuar |
| Any | ManualCheckpoint | ManualOnly | modo B; só humano pode autorizar a próxima ação |
| PhysicallySaved | Completed | Confirmed | pipeline concluído e intenção final confirmada |
| PhysicallySaved ou Absent | RemovalInProgress | Confirmed | remoção autorizada e preparada, ainda não concluída |
| Absent ou parcial | Removed ou RemovalPartial | Confirmed | remoção concluída ou interrompida com diário preservado |

Um API.Save() que retorna exceção, timeout, cancelamento ou resposta incompleta deve
ser OutcomeUnknown até consulta de identidade e leitura de confirmação. Não pode ser
automaticamente classificado como “não salvou”.

No modo A, a regra para atualizar o diário é:

- primeiro confirmar o Save do objeto com receipt;
- depois atualizar e reler o diário;
- se a atualização não for confirmada, preservar o último estado durável e marcar
  JournalDurabilityUnknown;
- nunca transformar estado desconhecido em Completed;
- nunca iniciar segunda persistência de API para “corrigir” um estado desconhecido.

No modo B, os mesmos receipts e a mesma ordem física devem ser registrados no
checkpoint manual. A extensão deve bloquear quando não houver confirmação humana do
checkpoint; ela não pode simular uma durabilidade que o modo B não possui.

### 4.9 Receipt de persistência e seam executável

Todo Save do plano deve passar por um seam de persistência injetável, inclusive:

- Transaction;
- diário B111;
- Folder;
- SDT;
- Procedure;
- API;
- metadata.

Cada PersistenceReceipt deve conter:

- etapa e tipo de objeto;
- identidade planejada;
- nome planejado;
- GUID, quando disponível;
- hash de conteúdo relevante;
- ordem monotônica;
- início e fim;
- resultado Confirmed, Failed ou OutcomeUnknown;
- exceção ou timeout;
- leitura de confirmação;
- relação com o diário no modo A ou com o checkpoint no modo B.

O seam não é um mock apenas textual. Deve ser compilado e executado em teste,
interceptar cada caminho físico de Save, permitir falha antes do Save, falha depois do
Save e resposta ambígua, e expor a sequência observada.

Se existir qualquer chamada direta a Save() fora do seam no pipeline B111, o teste
deve falhar. A busca textual serve para localizar candidatos, mas não substitui a
execução do seam.

### 4.10 Writer de Business Component

Quando BC for writer final:

1. receber contexto transient ou API existente por identidade validada;
2. validar que a assinatura e parâmetros da Procedure são os do contrato final;
3. aplicar Get/Create/Update/Delete sem persistir API antecipadamente;
4. persistir a Procedure conforme a flag e registrar receipt;
5. persistir o API uma única vez, no ponto final definido;
6. reler o API por identidade;
7. atualizar o diário no modo A ou o checkpoint no modo B após cada confirmação;
8. se List também participar, não declarar o pipeline concluído; List seguirá como
   consumidor final e será o único writer do API.

Se BC estiver selecionado e o API for inexistente com GenerateApiObject=false, o
gate bloqueia. Se BC estiver selecionado e o API for novo com
GenerateApiObject=true, o contexto transient é obrigatório.

### 4.11 Writer de List

Quando List participar:

1. receber contexto transient ou API existente por identidade validada;
2. usar o contrato final do API para derivar parâmetros BC;
3. persistir Procedure/Source de List conforme a seleção;
4. executar o único API.Save() final, se o API for gerenciado;
5. reler o API por identidade;
6. registrar receipts e atualizar o diário ou checkpoint, conforme o modo;
7. marcar Completed somente depois de metadata, se selecionada, também confirmada.

No fluxo BC+List, BC deve deixar o API em memória e o List deve ser o writer final.
Qualquer ordem que salve o API em BC e depois tente atualizar no List viola B111.

### 4.12 Metadata e relatório

Metadata não pode ser usada como diário pré-escrita. No modo A, o diário B111 é
criado por um writer próprio, antes da primeira gravação do pipeline.

Metadata selecionada:

1. é escrita depois de o API final estar fisicamente confirmado;
2. usa PersistedMainObjectGuid/identidade confirmada;
3. registra receipt;
4. atualiza o diário no modo A ou registra o checkpoint no modo B;
5. só leva o diário a Completed no modo A depois de leitura de confirmação; no modo B,
   registra a conclusão no checkpoint manual.

O relatório deve carregar:

- Transaction GUID e ApplicationId;
- PlannedApiName;
- PersistedMainObjectName e GUID;
- writer final;
- API físico e estágio do diário;
- durabilidade do diário;
- número de chamadas a API.Save();
- sequência de receipts;
- objetos criados, atualizados, bloqueados, warnings e errors.

TryResolveMainObjectFromKb não pode reencontrar o resultado final apenas por
collector.ApiName em um fluxo B111 gerenciado. A resolução final deve usar receipt,
GUID e identidade confirmada. Busca por nome só pode ser diagnóstico secundário e deve
reportar ambiguidade.

### 4.13 Trilha de execução

Registrar uma trilha estruturada com:

- início, cancelamento e fim;
- modo A ou B;
- entrada Sync ou Wizard;
- seleção completa;
- gate e razões de bloqueio;
- criação/confirmação do diário ou checkpoint;
- cada Save planejado;
- resultado de cada receipt;
- identidade planejada e persistida;
- writer final;
- transições dos três estados;
- recuperação executada, bloqueada ou exigindo humano.

Não registrar conteúdo sensível desnecessário. A trilha precisa ser suficiente para
reproduzir a ordem e distinguir “não tentou”, “falhou antes”, “salvou mas não foi
confirmado” e “confirmou fisicamente, mas o diário não foi atualizado”.

## 5. Consistência, recuperação e bloqueio

### 5.1 Fonte de verdade por modo

No modo A:

- o diário B111 é a intenção durável;
- receipts e leitura da KB são a evidência de estado físico;
- o último estado confirmado não pode ser promovido sem nova leitura;
- uma intenção nova nunca substitui silenciosamente uma intenção parcial.

No modo B:

- o checkpoint manual é a intenção operacional;
- o humano deve registrar seleção, ordem, nomes, GUIDs, objetos parciais e próximo
  passo autorizado;
- a extensão deve bloquear reaplicação automática;
- “continuar” não pode significar simplesmente chamar o Wizard de novo.

### 5.2 Recuperação de OutcomeUnknown do API

Ao encontrar API OutcomeUnknown:

1. bloquear qualquer novo API.Save();
2. reler por PlannedApiGuid, se houver;
3. procurar NewApiIdentityKey e o marcador B111;
4. validar Transaction GUID, ApplicationId, hash e contrato;
5. enumerar candidatos por identidade;
6. tratar zero, um confirmado e múltiplos candidatos como resultados distintos;
7. se um API próprio for confirmado, registrar PhysicallySaved;
8. se nenhum for confirmado, só concluir ausência após consulta suficiente e registro;
9. se houver múltiplos ou conflitantes, marcar OutcomeUnknown e bloquear;
10. atualizar o diário apenas com o que foi confirmado.

Nunca criar um novo API por nome porque a primeira chamada “pareceu falhar”.

### 5.3 Recuperação de JournalDurabilityUnknown

Quando o objeto pipeline foi confirmado, mas o update do diário não foi confirmado:

1. não continuar a sequência;
2. reler o diário pelo File identity;
3. enumerar todos os diários B111 com o Transaction GUID;
4. comparar ApplicationId, contrato, receipts e estados;
5. se houver exatamente um diário próprio e uma versão recuperável, corrigir o diário
   com transição explícita;
6. se houver zero, mais de um, conflito ou leitura ambígua, bloquear;
7. nunca criar um segundo diário para ocultar a incerteza.

### 5.4 Falha antes do API final

A falha antes do API final pode deixar Transaction, Folder, SDT ou Procedure parciais.
A recuperação deve:

1. ler o diário e seus receipts;
2. inventariar cada objeto alvo por GUID, identidade e hash;
3. separar ausente, confirmado, divergente e desconhecido;
4. comparar com a intenção durável;
5. decidir entre continuar somente uma etapa ainda não executada, reconciliar
   manualmente ou bloquear;
6. nunca repetir a persistência do API se já houver estado físico confirmado ou
   desconhecido;
7. nunca usar uma reabertura comum do Wizard como mecanismo de recuperação.

Se o estado de um objeto não puder ser determinado, a reaplicação automática fica
bloqueada. O plano não promete rollback.

### 5.5 Falhas C, D e E

As três classes precisam aparecer no código, nos testes e no relatório:

- C: falha antes de qualquer Save do objeto atual;
- D: Save retornou erro, timeout, cancelamento ou resposta não determinável;
- E: Save foi confirmado fisicamente, mas a atualização/leitura do diário falhou.

Para C, o receipt é Failed e a etapa pode continuar apenas se a política de
recuperação disser que nenhum efeito foi produzido.

Para D, o receipt é OutcomeUnknown; a identidade deve ser consultada antes de
qualquer repetição.

Para E, o objeto físico pode estar confirmado, mas o diário é
JournalDurabilityUnknown; bloquear a próxima etapa até reconciliação.

### 5.6 Remover API gerada e APIs legadas sem diário

A remoção deve ser tratada como operação B111 própria, sem apagar primeiro e tentar
explicar depois.

#### 5.6.1 API com diário B111

Antes de qualquer Delete():

1. localizar todos os diários pelo Transaction GUID;
2. exigir exatamente um diário próprio e íntegro;
3. validar ApplicationId, identidade do API, contrato e conjunto de alvos;
4. bloquear estados parciais, desconhecidos ou de durabilidade desconhecida;
5. registrar intenção de remoção e salvar/relê-la como RemovalInProgress;
6. somente então excluir API, Procedures, SDTs, metadata e Folder na ordem definida;
7. emitir receipt para cada exclusão;
8. após cada confirmação, atualizar e reler o diário;
9. marcar Removed apenas quando todos os alvos previstos estiverem confirmadamente
   ausentes;
10. marcar RemovalPartial em interrupção e preservar o diário.

O diário de remoção não deve ser apagado junto com a metadata. Ele é o registro da
operação e da recuperação.

#### 5.6.2 API legado criado antes de B111, sem diário B111

É proibido excluir um API legado cegamente, mesmo que o nome pareça seguir o padrão.
O remover deve escolher uma das duas saídas seguras:

1. construir e confirmar um diário de remoção importado a partir de metadata válida;
2. bloquear antes do primeiro Delete() e exigir recuperação manual.

O caminho implementável recomendado é:

1. criar o índice da KB;
2. localizar o API candidato e validar posse histórica;
3. localizar metadata correspondente;
4. validar Transaction GUID, ApplicationId, contrato, nomes e todas as referências;
5. reconstruir o conjunto completo de API, Procedures, SDTs, metadata e Folder;
6. rejeitar ausência, corrupção, conflito ou ambiguidade;
7. criar um diário B111 com LegacyImported=true, hash do contrato importado e os
   alvos validados;
8. salvar e reler o diário em RemovalInProgress;
9. só então começar os Deletes;
10. preservar o diário e registrar Removed ou RemovalPartial.

Se metadata válida não permitir reconstruir todos os alvos, não apagar parcialmente
por inferência. Retornar bloqueio com instrução de recuperação manual.

O fallback antigo ApiPlanOwnedObjectDescription.IsCanonical continua útil para
reconhecer posse histórica, mas não substitui Transaction GUID, metadata válida e
diário de remoção. Nome, Description canônica ou prefixo isolado nunca autorizam
exclusão.

## 6. Estados, mensagens e comportamento de cancelamento

Estados mínimos de aplicação:

    NotStarted
    GateBlocked
    JournalCreating
    JournalConfirmed
    TransactionPending
    FolderPending
    SdtsPending
    ProceduresPending
    ApiPending
    ApiSaveOutcomeUnknown
    ApiPhysicallySaved
    MetadataPending
    Partial
    JournalDurabilityUnknown
    Completed
    RemovalInProgress
    RemovalPartial
    Removed

Cancelamento antes do diário confirmado não grava objetos do pipeline.
Cancelamento depois do diário confirmado interrompe a sequência, atualiza o diário se
for seguro e deixa o próximo estado explícito. Cancelamento durante qualquer Save é
OutcomeUnknown até consulta.

Mensagens devem distinguir:

- gate bloqueado;
- Sync sem BC habilitado;
- Wizard com habilitação BC pendente;
- diário não confirmado;
- API inexistente quando GenerateApiObject=false;
- API com identidade ambígua;
- API salvo fisicamente;
- resultado desconhecido;
- diário com durabilidade desconhecida;
- recuperação bloqueada;
- API legado sem metadata suficiente;
- remoção parcial.

Mensagens novas devem seguir o mecanismo de internacionalização vigente e não expor
hashes ou identificadores além do necessário para diagnóstico.

## 7. Testes automatizados

### 7.1 Contratos estáticos

Criar ou ampliar testes para confirmar:

1. existe um predicado b111ManagedApply equivalente nas duas árvores de Package.cs;
2. todos os quatro fluxos compartilham a regra de um único writer de API;
3. nenhum writer BC/List chama API Save antes do ponto final;
4. B054 oferece contexto transient quando BC/List participa;
5. Sync declara o perfil fixo e não anuncia flags que a UI não oferece;
6. Wizard possui a matriz independente da seção 4.4;
7. o gate precede diário e todo outro Save;
8. o diário contém os campos obrigatórios;
9. a fórmula de nome do diário é determinística;
10. PlannedApiName e PersistedMainObjectName são campos distintos;
11. PlannedApiGuid/NewApiIdentityKey são gravados antes do API Save;
12. o marcador de Description é completo e validável;
13. os três estados são persistidos/reportados separadamente;
14. cada Save passa pelo seam;
15. o remover não inicia Delete sem diário próprio ou diário legado importado
    confirmado;
16. não existe fallback final por nome isolado;
17. a remoção preserva o diário.

Esses testes são sentinelas de contrato. Não são suficientes sem os testes executáveis
de seam e de integração abaixo.

### 7.2 Seam real de persistência

O teste deve compilar e executar uma implementação real do orchestrator com seam
injetado. O seam precisa:

- contar API.Save();
- registrar todos os Saves em ordem;
- injetar falha antes;
- injetar falha depois;
- injetar timeout/resultado ambíguo;
- permitir leitura de confirmação;
- simular falha de atualização do diário;
- simular objetos ausentes, divergentes e duplicados.

O contador deve provar:

- API-only: um API Save;
- BC-only: um API Save no writer BC;
- List-only: um API Save no writer List;
- BC+List: um API Save no writer List;
- Sync com BC/List: um API Save no writer final;
- qualquer combinação sem GenerateApiObject, em Wizard, não salva API.

### 7.3 Matriz executável

Cobrir no mínimo:

| Entrada | Seleção | Resultado mínimo |
|---|---|---|
| Wizard | API-only | B054 é único writer de API |
| Wizard | BC-only | BC é único writer de API |
| Wizard | List-only | List é único writer de API |
| Wizard | BC+List | List é único writer de API |
| Sync | sem BC/List | B054 é único writer de API |
| Sync | com BC | BC é único writer de API |
| Sync | com List | List é único writer de API |
| Sync | com BC+List | List é único writer de API |
| Wizard | GenerateApiObject=false, API próprio | atualizar consumidores sem API Save |
| Wizard | GenerateApiObject=false, API ausente | bloquear antes do primeiro Save |
| Wizard | flags SDT/Procedure false | não salvar etapa falsa |
| Sync | BC sem habilitação | bloquear sem diário e sem Save |
| Wizard | habilitação BC pendente | só salvar Transaction depois de gate/diário |
| qualquer | diário não confirmável | abortar antes do primeiro objeto |
| qualquer | API Save ambíguo | OutcomeUnknown e nenhum segundo API Save |
| qualquer | update do diário ambíguo | JournalDurabilityUnknown e bloqueio |
| remover | diário B111 próprio | RemovalInProgress, Deletes com receipts |
| remover | legado com metadata válida | diário LegacyImported antes do primeiro Delete |
| remover | legado sem metadata válida | bloqueio sem Delete |

### 7.4 Falha em cada fronteira

Executar a matriz com falha:

1. antes do diário;
2. durante confirmação do diário;
3. antes/depois de Transaction Save;
4. antes/depois de Folder Save;
5. antes/depois de SDT Save;
6. antes/depois de Procedure Save;
7. antes/depois do único API Save;
8. antes/depois de metadata Save;
9. durante cada atualização do diário;
10. durante cada Delete do remover.

Para cada ponto, validar estado físico, estágio lógico e durabilidade do diário como
colunas independentes. Validar também que uma segunda execução não duplica API nem
substitui intenção parcial.

### 7.5 Reaplicação

Para cada partial:

1. parar a execução;
2. ler diário, receipts e KB;
3. comparar identidades, hashes e alvos;
4. reabrir o fluxo somente em modo de recuperação;
5. permitir apenas etapas ainda não confirmadas;
6. impedir novo API Save quando API está confirmado ou desconhecido;
7. concluir somente após releitura e diário Completed.

Um teste que apenas chama novamente Apply pelo caminho normal não comprova
recuperação.

## 8. Validação na IDE

A validação manual deve usar uma Transaction de teste com objetos suficientes para
exercitar SDT, Procedure, API, BC e List.

### 8.1 Sync

Validar:

1. Sync sem BC/List;
2. Sync com BC;
3. Sync com List;
4. Sync com BC+List;
5. Transaction sem BC habilitado quando BC é selecionado;
6. que o caso acima bloqueia antes de qualquer gravação;
7. que não há toggle de GenerateApiObject=false apresentado como se existisse;
8. que o API aparece uma única vez e o relatório usa GUID/receipt.

### 8.2 Wizard

Validar:

1. API-only;
2. BC-only;
3. List-only;
4. BC+List;
5. cada linha de GenerateApiObject=false;
6. flags de SDT e Procedure independentes;
7. abertura do diálogo sem transaction.Save();
8. habilitação BC pendente aplicada somente após gate e diário;
9. falha/cancelamento em cada fronteira;
10. reentrada bloqueada ou recuperada pelo diário;
11. relatório final com as três dimensões e o contador de API Save.

### 8.3 Remoção

Validar API B111 com diário e API legado sem diário B111.

No caso legado, validar também:

- metadata válida produz diário importado antes do primeiro Delete();
- metadata inválida, ausente ou ambígua bloqueia sem Delete();
- interrupção preserva diário e marca remoção parcial;
- não há exclusão baseada somente em nome ou Description.

### 8.4 Alcance da evidência

Após qualquer alteração em emissor, writer, mapa de contrato, identidade ou seam,
reinstalar manualmente a DLL indicada pela política do repositório, reaplicar o fluxo
e repetir a validação. Não citar uma evidência de DLL anterior como evidência do
gerador novo.

## 9. Critérios de aceite

### 9.1 Critérios comuns a Sync e Wizard

1. Os quatro fluxos de API têm exatamente uma chamada física a API.Save().
2. O API Save acontece no writer final definido.
3. B054 não salva API quando BC ou List participa.
4. O gate ocorre antes do diário e de qualquer outro Save.
5. Cada objeto salvo tem receipt confirmável.
6. API novo possui identidade planejada, marcador completo e leitura de confirmação.
7. API existente é resolvido por identidade e contrato, não por nome isolado.
8. O relatório separa planejado de persistido e usa as três dimensões de estado.
9. Falhas C, D e E são distinguíveis e testadas.
10. Reaplicação não cria API duplicado nem sobrescreve intenção parcial.
11. Remoção não exclui alvo sem intenção de remoção confirmada.

### 9.2 Critérios específicos do Sync

1. O perfil fixo atual está documentado e testado.
2. BC sem habilitação bloqueia antes de diário, Transaction Save e demais Saves.
3. Sync não promete a matriz de flags que só existe no Wizard.
4. Sync com BC/List usa a mesma ordem final e o mesmo seam.

### 9.3 Critérios específicos do Wizard

1. GenerateApiObject=false não cria API.
2. API ausente com GenerateApiObject=false bloqueia antes do primeiro Save.
3. SDT e Procedure false não produzem Save oculto.
4. habilitação BC não salva ao abrir o diálogo.
5. mutação BC pendente ocorre somente depois do gate e do diário confirmado.

### 9.4 Critérios do modo A

1. Diário próprio é confirmado antes do primeiro objeto do pipeline.
2. Cada transição relevante é salva e relida.
3. estado físico, estágio lógico e durabilidade não são colapsados.
4. OutcomeUnknown e JournalDurabilityUnknown bloqueiam continuação automática.
5. recuperação compara intenção durável com inventário físico.
6. remoção legada cria diário importado ou bloqueia antes de Delete.

### 9.5 Critérios do modo B

1. Nenhum critério de diário A é alegado.
2. Checkpoint manual é criado antes da primeira gravação e antes de reaplicar.
3. Cada falha deixa o fluxo bloqueado para inspeção humana.
4. Não existe recuperação automática silenciosa.
5. Identidade, ordem, receipts e um API Save continuam obrigatórios.

Se algum critério comum ou específico falhar, a frente não está pronta para
implementação/aceite.

## 10. Decisão humana obrigatória sobre recuperação

### Modo A — diário B111 e recuperação condicionada

Escolher este modo para obter recuperação automática condicionada. Ele exige diário
próprio, confirmação por leitura, três dimensões de estado, receipts, seam executável,
inventário físico e bloqueio em qualquer ambiguidade.

### Modo B — recuperação manual bloqueante

Escolher este modo somente se a equipe aceitar que o diário B111 não será usado como
fonte durável. O checkpoint manual deve registrar intenção e objetos parciais antes de
qualquer reaplicação. A extensão não pode alegar que detecta ou reconcilia sozinha todo
estado parcial.

A decisão deve ser registrada no checkpoint da frente antes da implementação. Em caso
de ausência de decisão, o status é bloqueado para implementação.

## 11. Fora de escopo

Não fazem parte deste plano:

- transação atômica universal do SDK;
- rollback automático de todos os objetos;
- expansão da UI do Sync para a matriz de flags do Wizard;
- mudança do contrato de B109;
- alteração da instalação em C:\Program Files (x86)\GeneXus;
- instalação automática de DLL;
- publicação, commit ou push;
- remoção de objetos que não possam ser ligados a intenção e identidade;
- uso de nome ou Description como única prova de posse;
- redesign geral do Wizard fora da habilitação BC necessária;
- substituição da metadata existente por diário B111;
- limpeza automática de parciais sem decisão e evidência.

## 12. Documentação, instalação e fechamento

Durante a implementação, atualizar a documentação normativa que descreve a ordem de
gravação, o perfil Sync, a matriz Wizard, o diário, a identidade, a remoção e o
relatório.

Antes de gerar DLL para teste:

1. executar o checker de sincronização de comandos;
2. compilar a extensão;
3. confirmar o hash da DLL;
4. orientar a instalação manual conforme as regras do repositório;
5. declarar se manifesto/registro mudou;
6. pedir genexus /install somente se manifesto/registro mudou;
7. repetir a validação funcional na IDE.

O agente não altera a instalação do GeneXus.

Antes de concluir a frente, procurar no repositório por:

- B111, B054, B060 e B070;
- GenerateApiObject, GenerateSdts, GenerateProcedures, GenerateMetadata;
- ApiPlanApiObjectWriter, ApiPlanBusinessComponentWriter,
  ApiPlanListProcedureWriter, ApiPlanGeneratedApiRemover;
- PlannedApiName, PersistedMainObjectName, OutcomeUnknown,
  JournalDurabilityUnknown, LegacyImported;
- frases de “próxima ação”, “próxima missão”, “passivo”, “temporário”, “sonda”,
  “API salvo antes” e “API final”.

Corrigir documentos contraditórios, preservando referências históricas que estejam
claramente marcadas como históricas.

## 13. Resultado esperado da implementação

Ao final:

1. B054 prepara o contrato sem antecipar API Save quando BC/List participa;
2. BC-only, List-only e BC+List têm um único writer final e uma única persistência de
   API;
3. API-only continua funcionando e salva uma vez;
4. Sync preserva seu perfil fixo e adota a mesma ordem segura;
5. Wizard suporta a matriz independente sem esconder saves;
6. uma seleção inválida é bloqueada antes do primeiro objeto;
7. o diário ou checkpoint torna a intenção explícita;
8. o estado físico do API, o estágio lógico e a durabilidade do diário são
   distinguíveis;
9. falha após Save não causa segunda persistência cega;
10. o relatório aponta objeto planejado e objeto persistido por identidade;
11. remoção legada só começa com diário importado confirmado ou fica bloqueada;
12. testes estáticos, seam, matriz, falha, reaplicação e IDE cobrem os critérios;
13. nenhum comando ou instalação fora do escopo foi executado como parte do plano.

O plano só pode ser considerado pronto para implementação quando a decisão de modo
estiver registrada e todos os critérios aplicáveis forem aceitos pelo revisor e pelo
responsável humano.

## 14. Fontes normativas e evidências

### Repositório ativo

- README.md
- AGENTS.md
- Src/Extension/Package.cs
- Src/Extension/GenexusOpenApiBuilder.package
- Src/Extension/Diagnostics/ApiPlanApiObjectWriter.cs
- Src/Extension/Diagnostics/ApiPlanBusinessComponentWriter.cs
- Src/Extension/Diagnostics/ApiPlanListProcedureWriter.cs
- Src/Extension/Diagnostics/ApiPlanGeneratedApiRemover.cs
- Src/Extension/Diagnostics/ApiPlanOwnedObjectDescription.cs
- Src/Extension/Diagnostics/ApiPlanTransactionSyncOrchestrator.cs
- Src/Extension/Diagnostics/PrototypeWizardDialog.cs
- Src/Extension/Diagnostics/PrototypeWizardBusinessComponentSelection.cs
- Tests/
- Tools/Test-ExtensionCommandRegistration.ps1
- Docs/Foundation/06-BACKLOG_v0.1.md
- Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md

### Evidências de processo

- evidência de IDE da primeira geração, aproximadamente 49 segundos e 44 SDTs;
- observação do caso B070 com List incompatível com Procedure parametrizada;
- observação de degradação em cascata após reabertura do Wizard;
- análise da chamada atual de BuildSelection do Sync;
- análise do remover atual e de seu início de exclusão pelo API;
- análise de ApiPlanOwnedObjectDescription e de TryResolveMainObjectFromKb;
- pareceres anteriores que apontaram gate tardio, identidade ambígua, falta de seam,
  estados colapsados e remoção legada sem diário.

Essas fontes são evidência para revisão. Nenhuma delas substitui compilação, testes
executáveis ou validação manual da DLL produzida pela implementação.

## 15. Perguntas ao revisor independente

1. O perfil fixo do Sync está explicitamente separado da matriz independente do
   Wizard, sem regressão funcional ou promessa de UI inexistente?
2. Há algum Save do pipeline ainda fora do gate ou fora do seam?
3. A ordem realmente garante que B054 não persiste API em BC-only, List-only ou
   BC+List?
4. A matriz GenerateApiObject=false bloqueia todos os casos de API ausente sem
   criar API implicitamente?
5. Transaction Save para BC está diferido no Wizard e bloqueado no Sync quando
   necessário?
6. A identidade de API novo sobrevive a timeout, exceção e reentrada sem duplicação?
7. O estado físico do API, o estágio lógico e a durabilidade do diário permanecem
   independentes em código, relatório e testes?
8. A recuperação bloqueia todo resultado desconhecido sem transformar um nome parecido
   em prova?
9. O remover reconstrói um diário seguro para legado ou bloqueia antes do primeiro
   Delete?
10. O relatório final usa receipt/GUID e distingue planejado de persistido?
11. A evidência manual será repetida depois da DLL que contém esta alteração?
12. Existe alguma regressão não coberta, especialmente em cancelamento, metadata,
   reentrada, múltiplos diários, dois ApplicationId ou objetos históricos?

**FIM DO PLANO V24**
