# Suporte a Transactions com Subníveis (Multinível) — B095–B099

## Status

Frente em andamento na Sprint 9 (absorvendo os itens de backlog B095 a B099). Especificação funcional, levantamento na KB de produção e desenho da arquitetura de implementação concluídos.

Em 2026-08-23 a especificação passou por revisão dirigida e recebeu as decisões consolidadas na `Emenda técnica — 2026-08-23` do registro de decisões do MVP. A revisão acrescentou a Fase 0 (linha de base de não regressão), a Fase 7 (ciclo de vida sob hierarquia) e três itens correlatos fora da frente de subníveis (`B100`, `B101` e `B102`).

Ainda em 2026-08-23, uma segunda revisão — desta vez sobre o **plano de trabalho** da sprint, e não sobre o desenho — fechou quinze pontos de exequibilidade e de gate, consolidados na `Emenda técnica — 2026-08-23 (complemento)` do mesmo registro. Dela resultaram o mecanismo concreto da Fase 0, a ampliação de `B102`, dois gates novos na sprint, a validação do contrato OpenAPI multinível, o retorno da linha `Gx18u13` ao plano, o fechamento da decisão que estava pendente na Fase 3, limiares de escala, o item `B105` e a divisão da publicação em três cortes.

**Atualização de 2026-08-25 (tarde).** Camada offline da Fase 0 capturada e ligada ao checker
(`Tests/GenerationBaseline/`, `tests.generationBaseline`). Captura IDE de início registrada
em `Tests/GenerationBaseline/IdeXpz/CAPTURE-INICIO.md` (SDTs da Transaction plana `Teste` +
compartilhados, a partir de `C:\Dev\Prod\Gx_wsEducacaoSpTeste\ObjetosDaKbEmXml\SDT`).
Esses SDTs são o acervo **já existente** na KB — **não** regenerados na IDE neste dia e
**sem** instalação da DLL da sessão Fase 0; timestamps de 2026-08-25 na pasta paralela são
de rematerialização do XPZ. Conferência XPZ de fim de sprint permanece para o fechamento.
Detalhe: `Docs/Implementation/2026-08-25-FASE0-LINHA-DE-BASE-NAO-REGRESSAO.md`.

**Atualização de 2026-08-25 (noite).** ~~Conferência humana da Fase 0 encerrada; camadas de início permanecem; próxima ação = Fase 1 (`B095`).~~ **Superada** pela atualização B095 abaixo.

**Atualização de 2026-08-25 (noite, B095).** Fase 1 (`B095`) concluída: leitor hierárquico à parte (`TransactionStructureReader` com núcleo `Build` + adaptador SDK), modelo `ApiPlanLevel` / `ApiPlanLevelField`, critério compartilhado `TransactionAttributeKeyTraits`, testes offline com ouro em `Tests/TransactionStructure/Baselines/` ligados ao pré-push. Caminho flat do Wizard sem o leitor hierárquico. ~~Próxima ação = Fase 2 (`B096`).~~ **Superada** pela atualização B096 abaixo. Evidência: `Docs/Implementation/2026-08-25-B095-LEITURA-HIERARQUICA.md`.

**Atualização de 2026-08-31 (publicação `0.1.0-alpha.6`).** Tag `v0.1.0-alpha.6` e GitHub Release pre-release com os dois assets DLL. Próxima ação operacional = `B108` (plano aprovado e gravado; código adiado). Plano: [2026-08-31-B108-PLANO-PREFERENCIAS-E-RETRACAO.md](2026-08-31-B108-PLANO-PREFERENCIAS-E-RETRACAO.md). Evidência: [Release](https://github.com/GxBrasilNOficial/Genexus-Open-API-Builder/releases/tag/v0.1.0-alpha.6).

**Atualização de 2026-08-31 (pacote `0.1.0-alpha.6`).** CHANGELOG, notas PT/ES/EN, versão `0.1.0-alpha.6`, README/`INSTALL`/`DEMO` e builds Release canônico + satélite preparados. ~~Tag e GitHub Release aguardam autorização humana.~~ **Superada** pela publicação acima. Evidência: `Docs/Releases/0.1.0-alpha.6.md`.

**Atualização de 2026-08-30 (`B100`).** Serviço `Delete` opt-in concluído. ~~Próxima ação operacional = preparar o corte `0.1.0-alpha.6`.~~ **Superada** pela atualização do pacote acima. Evidência: `Docs/Implementation/2026-08-30-B100-DELETE-OPT-IN.md`.

**Atualização de 2026-08-28 (B099v).** Fase 5-A (`B099v`) concluída: correção de `ResolveAggregateAttributeName` (PK própria em `count()`), ouro e gate `tests.listHierarchical` atualizados; reapply do Wizard na `Teste` de quatro níveis; smoke HTTP multinível nos dois environments; critério 9 (YAML hierárquico + geração de cliente). Evidência: `Docs/Implementation/2026-08-28-B099v-VALIDACAO-RUNTIME-MULTINIVEL.md`.

**Atualização de 2026-08-27 (Fase 5-A / `B099v`).** Inserida uma fase entre a 5 e a 6, sem renumerar as seguintes: validação em runtime do que as Fases 2 a 5 emitiram — correção da agregação `count()` com PK composta herdada, smoke HTTP multinível nos dois environments e o critério 9, cujo prazo (fim da Fase 4) venceu sem execução. A razão da ordem é que a Fase 6 grava metadata V2 sobre o contrato hierárquico: defeito descoberto depois custa migração de integridade. ~~Próxima ação = Fase 5-A (`B099v`).~~ **Superada** pela atualização B099v acima.

**Atualização de 2026-08-27.** §8 e §8-A alinhados a `ValidatedDepth = 4` (aviso acima de 4; survey de produção permanece 3). Remissão correspondente na emenda de 2026-08-23 do registro de decisões. Testes offline ampliam combinações Create/Update/Response na poda e colisão/`AllocateVariableToken`.

**Atualização de 2026-08-26 (B099a).** Fase 5 (`B099a`) concluída: Wizard hierárquico (seletor compartilhado, dependência pai/filho, contador desligável, aviso de profundidade), `ApiPlan.Levels` podado para preview e apply, ouro offline e gate `tests.wizardHierarchical`. Metadata V2, sync e remoção ficam na Fase 6. ~~Próxima ação = Fase 6 (`B099b`).~~ **Superada** pela atualização Fase 5-A acima. Evidência: `Docs/Implementation/2026-08-26-B099a-WIZARD-HIERARQUICO.md`.

**Atualização de 2026-08-26 (B098).** Fase 4 (`B098`) concluída: `ListResponse_Item` condicionado, contadores `count()` de subníveis diretos, ouro offline e gate `tests.listHierarchical`. Wizard flat e metadata V2 fora deste recorte. ~~Próxima ação = Fase 5 (`B099a`).~~ **Superada** pela atualização B099a acima. Evidência: `Docs/Implementation/2026-08-26-B098-LIST-CONTADORES.md`.

**Atualização de 2026-08-26 (B097).** Fase 3 (`B097`) concluída: Source BC hierárquico Get/Create/Update com `<Subnível>Replace`, mapa alinhado ao B096, ouro offline e gate `tests.businessComponentHierarchical`. Wizard flat e `ListResponse_Item` fora deste recorte. ~~Próxima ação = Fase 4 (`B098`).~~ **Superada** pela atualização B098 acima. Evidência: `Docs/Implementation/2026-08-26-B097-BC-HIERARQUICO.md`.

**Atualização de 2026-08-26 (B096).** Fase 2 (`B096`) concluída: plano de SDT hierárquico por contrato (Create/Update/Response + `<Subnível>Replace`), naming/desambiguação/encurtamento de objeto a 128, ouro offline e gate `tests.sdtHierarchicalPlan`. Wizard flat e `ListResponse_Item` fora deste recorte. ~~Próxima ação = Fase 3 (`B097`).~~ **Superada** pela atualização B097 acima. Evidência: `Docs/Implementation/2026-08-26-B096-SDTS-HIERARQUICOS.md`.

---

## Diagnóstico e Evidência na KB de Produção

Para fundamentar as decisões de arquitetura e a profundidade de níveis suportada, foi realizada uma varredura em modo consulta nos arquivos XML de Transaction da KB de produção (`C:\Dev\Prod\Gx_FabricaBrasil\ObjetosDaKbEmXml\Transaction`):

- **Total de Transactions analisadas:** 196
- **Transactions com 1 nível (planas):** 176 (89,8%)
- **Transactions com mais de 1 nível (multinível):** 20 (10,2%)
- **Profundidade máxima de aninhamento:** **3 níveis** (`DadosDoDia` -> `Turno` -> `Funcionario`)
- **Padrões de subnível identificados:**
  - **11 transações** possuem 1 subnível direto (ex.: `CondicaoPagamento` -> `Parcelas`, `VendaLiberado` -> `Item`, `Municipio` -> `Mapeamentos`, `PautaMinima` -> `Produto`).
  - **8 transações** possuem **múltiplos subníveis paralelos** no 2º nível (ex.: `Empresa` com 13 subníveis paralelos, `Tributacao` com 7, `Usuario` com 6, `CompraGadoItens` com 5, `Rota` com 3, `Comissao` com 2, `EmbarqueSaida` com 2, `OperacaoFiscal` com 2).
  - **1 transação** possui aninhamento em profundidade de 3 níveis (`DadosDoDia`).

Esses dados comprovam que o suporte a múltiplos subníveis paralelos e recursão em árvore (até profundidade 3) é indispensável para cobrir a KB real.

---

## Decisões Funcionais e Arquiteturais

### 1. Leitura Hierárquica da Estrutura e Modelo de Domínio (B095)
- A leitura via GeneXus SDK navega recursivamente pela árvore de `TransactionLevel`s a partir de `transaction.Structure.Root`.
- Cada nível é representado no modelo `ApiPlanLevel` com `LevelName`, `Depth` (1 para cabeçalho, 2 para 1º subnível, 3 para aninhamento), `ParentLevelName`, `LevelOrder`, `PrimaryKey` composta do nível e lista de campos candidatos da estrutura (`Fields`; seleção por contrato = B099+).

### 2. Regras Explícitas de Elegibilidade Intra-Subnível
- **Fórmulas de subnível** (ex.: `ItemTotal = ItemQtd * ItemPreco`): desabilitadas em `CreateRequest` e `UpdateRequest` (somente leitura); elegíveis e habilitadas em `Response`.
- **Regras `NoAccept` em subnível**: desabilitadas em `CreateRequest` e `UpdateRequest` (evitando erro `spc0018`); elegíveis em `Response`.
- **Atributos inferidos da tabela estendida no subnível** (ex.: `ProdutoNome` vindo de `ProdutoId`): desabilitados em Requests; elegíveis em `Response`.
- **Chave primária do subnível**:
  - No `CreateRequest`: se for autonumerada ou gerenciada pelo BC, fica opcional/desabilitada; se for informada (ex.: número da parcela `ParcelaId`), é enviada no objeto da coleção.
  - No `UpdateRequest`: identifica a linha na coleção para que o BC execute o update correspondente.
- **Campos de auditoria e sensibilidade no subnível**: herdam a mesma classificação e proteção do nível 1.

### 3. Geração de SDTs com Coleções (B096)

**Forma do nó de coleção — um SDT próprio por subnível, por contrato.** O SDT writer atual só emite membros na raiz do SDT e a única estrutura que suporta é um membro tipado como **outro SDT** com `IsCollection = true`; subestrutura aninhada dentro do mesmo SDT não é suportada hoje (`Src/Extension/Diagnostics/ApiPlanSdtWriter.cs`). O mecanismo já está em uso, com `ListResponse` tipando `Items` com `Response`.

Cada subnível selecionado gera portanto um SDT próprio **por contrato**, e não um SDT único compartilhado entre os três. O motivo é de contrato, não de implementação: um SDT único precisaria conter a união dos campos, e campos somente leitura (fórmula, `NoAccept`, atributo inferido) apareceriam no `CreateRequest`, contrariando a elegibilidade da seção 2 e publicando no contrato OpenAPI membros que a geração nunca lê — o mesmo defeito que motivou a retirada de `Errors[]` na `Emenda técnica — 2026-08-03`.

**Nomenclatura — o papel permanece imediatamente após `_API_`, e o subnível é qualificador.** O padrão é `sdt<NomeBase>_API_<Papel>_<Subnível>`, e não o inverso: assim cada contrato forma um bloco contíguo na ordenação alfabética da Folder, com seus derivados logo abaixo do SDT pai, e o nome se anuncia como derivado do contrato a que pertence.

- `sdt<Tx>_API_Response`: campos do cabeçalho + membro coleção por subnível selecionado, tipado por `sdt<Tx>_API_Response_<Subnível>`.
- `sdt<Tx>_API_CreateRequest`: campos do cabeçalho + membro coleção tipado por `sdt<Tx>_API_CreateRequest_<Subnível>`, **salvo** quando o SDT aninhado daquele papel ficaria com 0 membros (Create só com PK herdada): o GeneXus recusa SDT sem itens, então aquele SDT e a coleção no pai **não** são emitidos.
- `sdt<Tx>_API_UpdateRequest`: campos do cabeçalho + membro coleção tipado por `sdt<Tx>_API_UpdateRequest_<Subnível>`, mais o marcador `<Subnível>Replace` descrito na seção 10.
- `sdt<Tx>_API_ListResponse`: envelope de lista, com os mesmos três membros de sempre (`Items`, `Pagination`, `AppliedFilters`).
- `sdt<Tx>_API_ListResponse_Item`: **somente quando houver subnível selecionado** — tipo dos elementos de `Items`, conforme a seção 7-A.

Em profundidade maior que 2, o SDT do subnível contém, por sua vez, o membro coleção do nível seguinte, com o caminho acumulado no qualificador (`sdt<Tx>_API_Response_<Subnível>_<SubSubnível>`). Limite de nome de **objeto** GeneXus 18 = **128**. A Fase 2 (`B096`) encurta o nome do SDT quando o nome completo estoura 128 ou colide: folha até 32 caracteres tenta reusar a folha; senão (ou se essa forma não couber/colidir) usa hash SHA-256 de 8 hex. O encurtamento pode truncar o nome da Transaction. Nomes de membro (coleção e `Replace`) herdam o identificador sanitizado do nível, sem teto nesta fase. Escrita real na KB e smoke do limite (objeto e membro) ficam para depois desta fase.

Exemplo completo, para `DadosDoDia -> Turno -> Funcionario` com os três níveis selecionados:

```
sdtDadosDoDia_API_CreateRequest
sdtDadosDoDia_API_CreateRequest_Turno
sdtDadosDoDia_API_CreateRequest_Turno_Funcionario
sdtDadosDoDia_API_ListFilters
sdtDadosDoDia_API_ListResponse
sdtDadosDoDia_API_ListResponse_Item
sdtDadosDoDia_API_Response
sdtDadosDoDia_API_Response_Turno
sdtDadosDoDia_API_Response_Turno_Funcionario
sdtDadosDoDia_API_UpdateRequest
sdtDadosDoDia_API_UpdateRequest_Turno
sdtDadosDoDia_API_UpdateRequest_Turno_Funcionario
```

**Consequência para o inventário de remoção:** a lista de SDTs próprios deixa de ser fixa. `ApiPlanGeneratedApiRemovalPlan` passa a ler os nomes gravados na metadata em vez da lista hardcoded de cinco nomes, sob pena de deixar órfãos na KB ao remover uma API multinível. A ordem de exclusão continua respeitando a dependência de tipos (um SDT referenciado não pode ser apagado antes de quem o referencia).

**Ordem de criação e de remoção, declaradas em par.** A criação percorre a árvore em **pós-ordem**: o SDT mais profundo primeiro, porque o SDT pai o referencia como tipo do membro coleção e não resolve enquanto o filho não existir. A remoção segue a ordem inversa, que é o que o plano de remoção já pratica ao apagar `ListResponse` antes de `Response`.

### 4. Geração de Código Business Component nas Procedures (B097)
- **`Get` (`proc<Tx>_API_Get`):**
  - Carrega a Transaction completa via Business Component: `&BC.Load(&PK)`.
  - Atribui campos do cabeçalho para `&GetResponse`.
  - Itera sobre as linhas de cada subnível do BC carregado:
    ```genexus
    For &BCItem in &BC.<Subnível>
        &ResponseItem = new()
        &ResponseItem.<Campo1> = &BCItem.<Campo1>
        ...
        &GetResponse.<Subnível>.Add(&ResponseItem)
    EndFor
    ```
- **`Create` (`proc<Tx>_API_Create`):**
  - Atribui campos do cabeçalho de `&CreateRequest` para `&BC`.
  - Itera sobre as coleções de cada subnível do request, adicionando itens ao BC:
    ```genexus
    For &RequestItem in &CreateRequest.<Subnível>
        &BCItem = new()
        &BCItem.<Campo1> = &RequestItem.<Campo1>
        ...
        &BC.<Subnível>.Add(&BCItem)
    EndFor
    &BC.Save()
    ```
- **`Update` (`proc<Tx>_API_Update`) — Substituição Completa sob marcador explícito:**
  - O endpoint `Update` adota a semântica REST `PUT` de **substituição completa e idempotente**, porém condicionada ao marcador `<Subnível>Replace` descrito na seção 10:
    - Com `<Subnível>Replace = True`, o payload da coleção representa o estado final desejado para as linhas, e linhas existentes no banco que forem omitidas são removidas pelo Business Component.
    - Com o marcador ausente ou `False`, as linhas daquele subnível não são tocadas e o `Update` atua somente sobre o cabeçalho.
    - Se o consumidor da API desejar preservar registros dentro de uma substituição, ele deve enviá-los ou marcá-los com flags da aplicação (ex.: "cancelado").
  - A ordem das linhas no payload não identifica a linha; a identificação é pela chave primária do subnível. A exceção está declarada na seção 9.

### 5. Atomicidade e Controle Transacional do Business Component
- No GeneXus, o `Business Component` executa `Save()` sob uma única transação atômica nativa de banco de dados (cabeçalho + todas as linhas filhas):
  - Se `&BC.Success()` for verdadeiro: a Procedure executa `Commit`, gravando o conjunto completo.
  - Se `&BC.Success()` for falso: a Procedure **não** executa `Commit` e dispara `Rollback`, garantindo ausência de escrita parcial ou registros órfãos no banco de dados.

### 6. Contrato de Erros para Subníveis (Alinhamento com o Runtime e Emenda 2026-08-03)
- Em caso de falha de validação pelo Business Component em qualquer nível (cabeçalho ou linhas filhas) durante o `Save()`:
  - O código HTTP retornado é `422 Unprocessable Content`;
  - `&ErrorResponse.Code` retorna `!"validation_error"`;
  - `&ErrorResponse.Message` carrega o texto emitido pelo Business Component, conforme `B102`. A geração atual emite esse texto em `Message` (`LongVarChar`, truncada visivelmente em cerca de 2K) e em `Messages[]` tipado por `sdt_API_ErrorMessage`; o texto fixo da Alpha permanece apenas no reconhecimento de reencontro e quando o repasse está desligado;
  - `Message` é `LongVarChar`, com truncamento explícito da geração em cerca de 2K e reticência final, em vez do corte silencioso que o `VarChar(256)` produzia.
- **Forma do corpo de erro, fechada pelo experimento de `B102` em 2026-08-24.** A retirada de `Errors[]` pela `Emenda técnica — 2026-08-03` foi motivada por uma tentativa com **subestrutura aninhada** dentro do próprio SDT (`sdt_API_ErrorResponse.Error`), que a IDE recusou. Coleção tipada por um **SDT separado** é mecanismo distinto, o mesmo que já funciona em `ListResponse.Items`. A IDE aceitou esse mecanismo: `sdt_API_ErrorResponse` ganha o membro coleção `Messages`, tipado por `sdt_API_ErrorMessage`, preenchido a partir de `GetMessages()`. `Message` permanece top-level e preenchido, concatenado por `" | "`, para não quebrar consumidores da Alpha. Somente mensagens de **erro** são repassadas — `Msg()` do Business Component não entra no corpo do `422` (tipo 0 no `Teste_BC`; o Create filtra `Type == 1`). Gate HTTP fechado nos dois environments em 2026-08-24.
- Não é emitido array paralelo **por linha**: nenhuma das duas formas correlaciona mensagem com índice de linha, e o corpo de erro continua sem estrutura espelhando a hierarquia.

### 7. Procedimento `List` (B098) — Resumo com Contadores
- A listagem geral paginada **não** aninha os arrays completos de subníveis para preservar a performance.
- O `For each` do `List` percorre o nível 1 (cabeçalho) e projeta contadores numéricos para cada subnível ativo (`&Item.<Subnível>Count`), informando a quantidade de registros filhos sem inflar o payload HTTP.
- **Mecanismo:** fórmula agregada nativa (`count()`) avaliada dentro do `For each` do cabeçalho, resolvida pelo GeneXus como agregação no próprio SQL. Fica descartado o `For each` aninhado com incremento manual de variável, que traria as linhas filhas ao servidor de aplicação apenas para contá-las.
- **Controle pelo usuário:** o contador é gerado por padrão para cada subnível selecionado e pode ser **desativado individualmente** no Wizard. O controle existe porque o custo cresce com o número de subníveis paralelos: uma transação como `Empresa`, com 13 subníveis, produziria 13 agregações por linha da página.
- **Somente subníveis diretos:** contadores são gerados apenas para subníveis de profundidade 2.
- **Onde os contadores vivem:** no `sdt<Tx>_API_ListResponse_Item`, descrito na seção 7-A, e não no `Response`. Contador de neto seria uma soma achatada atravessando os pais (o total de funcionários do dia, perdendo a distribuição por turno), informação que o formato da listagem não comporta e cujo nome não denunciaria a agregação. Quem precisa do detalhe usa o `Get`, que devolve a árvore completa.

### 7-A. Tipo dos Elementos de `Items` na Listagem

- Em transação **sem** subnível selecionado, nada muda: `Items` continua sendo coleção de `sdt<Tx>_API_Response`, exatamente como no contrato vigente. Essa condição não é detalhe: se o tipo mudasse para todas as transações, toda API plana da Alpha teria o contrato alterado e a linha de base da Fase 0 seria invalidada.
- Em transação **com** subnível selecionado, `Items` passa a ser coleção de `sdt<Tx>_API_ListResponse_Item`, que contém os mesmos campos de cabeçalho do `Response`, **sem** os membros de coleção, mais os contadores `<Subnível>Count`.
- **Motivo.** O `List` não preenche as coleções, por decisão de performance da seção 7. Reusar o `Response` publicaria em cada elemento da listagem arrays permanentemente vazios, que o consumidor leria como "não há linhas" quando o correto é "esta resposta não traz linhas". É o mesmo defeito que motivou a retirada de `Errors[]` do `sdt_API_ErrorResponse` na `Emenda técnica — 2026-08-03`: publicar no contrato uma estrutura que a geração nunca preenche. O contador substitui o array vazio por um número verdadeiro.
- O envelope `sdt<Tx>_API_ListResponse` permanece com `Items`, `Pagination` e `AppliedFilters`; muda apenas o tipo dos elementos de `Items`.
- A regra "`Get`, `Create`, `Update` e cada item de `List` usam o mesmo `Response`", registrada nos documentos 13 e 26 e no registro de decisões, foi escrita quando o gerador só conhecia um nível. Ela permanece válida para transação plana e fica condicionada nesta frente.

### 8. Interface do Wizard (UX) e Sincronização Hierárquica (B099)
- A aba de seleção de campos agrupa os atributos por nível (seletor de nível / abas por nível / seções colapsáveis).
- O usuário pode selecionar granularmente quais atributos de cada subnível entram em Create, Update e Response.
- Subníveis sem nenhum atributo marcado não são gerados nos SDTs nem no código de BC.
- Marcar um subnível aninhado exige que o subnível pai esteja marcado; a UI trata isso como dependência, não como escolha livre.
- Cada subnível selecionado exibe o controle de contador de `List` (ligado por padrão), conforme a seção 7.
- Transações com profundidade maior que 4 exibem aviso de **profundidade não validada**, sem bloquear a geração, conforme a seção 8-A.
- A aba `Obrigatórios` passa a agrupar por nível, com a mesma estrutura da aba de seleção de campos, para que o `Required` de campo de linha descrito na seção 10 tenha onde ser marcado.
- O resumo do Wizard passa a exibir **quantos objetos serão criados** antes de aplicar. Com um subnível a diferença é pequena; com treze subníveis paralelos a geração passa de doze para mais de cinquenta objetos, e o número precisa aparecer antes da decisão, não no relatório final.
- O comparador de sincronização (`Sincronizar com a Transaction`) compara adições, renomeações e remoções dentro da hierarquia de níveis com base nos `attributeGuid`s.

### 8-A. Profundidade Suportada

- O leitor de estrutura, o modelo de domínio e os geradores tratam níveis por **recursão genérica, sem limite artificial** no código. Limite embutido custaria mais para implementar do que a recursão honesta e transformaria um caso raro em bloqueio total do wizard, inclusive para o cabeçalho.
- A profundidade **3** permanece o máximo observado no levantamento da KB de produção (`DadosDoDia -> Turno -> Funcionario`); isso descreve o acervo, não a trava do Wizard.
- A profundidade **4** é o alcance da **evidência de validação** na KB de teste (`Teste` com `TesteItem` → `TesteItemFolio` → `TesteItemFolioDoc`, mais o irmão `TestePortfolio`), coberta pelos testes offline (`FourDeep` / `ValidatedDepth`) e pelo smoke U15 de 2026-08-26. `ValidatedDepth = 4` no código.
- Acima de 4 níveis, o Wizard avisa que a profundidade não foi validada e deixa a decisão com o usuário, que pode desmarcar os níveis mais profundos.

### 9. Ordem das Linhas na Coleção

- As linhas de cada coleção são devolvidas na ordem da **chave primária do subnível, ascendente** — que é a ordem em que o Business Component materializa as linhas após o `Load()`. A decisão é declarar contratualmente o comportamento que já existe, e não reordenar em memória.
- A declaração entra na descrição do serviço e na documentação pública, porque o consumidor idempotente precisa saber que dois `GET` do mesmo registro trazem as linhas na mesma ordem, em qualquer dos dois geradores.
- No `Update`, **a ordem em que o cliente envia as linhas é irrelevante**: a identificação é pela chave do subnível. A exceção é a chave **autonumerada ou sequencial**, em que não há como identificar a linha pelo payload e a ordem do array passa a determinar a numeração atribuída. Sem essa ressalva escrita, reordenar um array de parcelas parece inócuo e troca os números de parcela de lugar.
- Ordem configurável por subnível fica fora desta frente; nada nesta decisão impede acrescentá-la depois de forma aditiva.

### 10. `Required` no Subnível e Marcador de Substituição

- **Campo obrigatório dentro da linha:** vale a mesma regra da `Emenda técnica — 2026-08-03` — `Required` significa preenchimento, não presença do membro JSON. A resposta continua `400` com `Code = "invalid_request"`, e a `Message` identifica a linha pelo caminho, com **índice base 0** (`Parcelas[0].ParcelaValor` é a primeira linha). A base é a do JSON que o cliente enviou, e não a da coleção GeneXus, que itera a partir de 1: o custo é um decremento na montagem da mensagem, e o ganho é o consumidor conseguir localizar a linha no próprio payload sem conversão mental. Isso não cria estrutura nova no corpo de erro.
- **Coleção ausente ou vazia no `Create`:** significa zero linhas e é sucesso (`201`). O modelo GeneXus não obriga um subnível a ter linhas, e a geração não inventa essa obrigação.
- **Marcador `<Subnível>Replace` no `Update`:** o `UpdateRequest` recebe um membro booleano por subnível selecionado.

| Valor recebido | Efeito sobre as linhas filhas |
|---|---|
| ausente ou `False` | não são tocadas; o `PUT` atualiza somente o cabeçalho |
| `True` | substituição completa pelo array enviado |
| `True` com array vazio | todas as linhas são removidas |

- **Por que o marcador existe.** A `Emenda técnica — 2026-08-03` comprovou que o corpo da requisição não permite distinguir membro ausente de membro vazio. Sem marcador, um `PUT` que **esquece** o array `Itens` seria indistinguível de um `PUT` que pede a remoção de todos os itens: o cliente atualizaria uma observação do cabeçalho e perderia as linhas, em silêncio e sem reversão. O marcador se apoia justamente na limitação — o default de um booleano é `False`, e ausente é indistinguível de `False`, de modo que o comportamento por omissão é o seguro.
- **Marcador em profundidade maior que 2.** O marcador do nível 2 fica no corpo principal do `UpdateRequest`; o do nível 3 fica **dentro de cada item** do nível 2, porque a decisão de substituir é por linha pai. Em `DadosDoDia -> Turno -> Funcionario`, `TurnoReplace` está no topo e `FuncionarioReplace` dentro de cada `Turno` enviado.
- **Regra de propagação entre níveis, fechada em 2026-08-23.** Quando `TurnoReplace = True` remove uma linha de turno, os funcionários daquela linha vão junto, por integridade do próprio Business Component; a combinação "substituir turnos e preservar funcionários de turnos removidos" não existe. O comportamento do código gerado é:

| Estado no `UpdateRequest` | Efeito |
|---|---|
| `TurnoReplace = True`, turno enviado que já existia, `FuncionarioReplace = True` | os funcionários daquele turno são substituídos pelo array enviado |
| `TurnoReplace = True`, turno enviado que já existia, `FuncionarioReplace` ausente ou `False` | os funcionários existentes daquele turno são preservados; o array de funcionários enviado é ignorado, sem erro |
| `TurnoReplace = True`, turno novo | os funcionários enviados são inseridos; o marcador é irrelevante, porque não há o que preservar |
| `TurnoReplace = True`, turno omitido | o turno é removido com seus funcionários |
| `TurnoReplace` ausente ou `False` | o array de turnos é ignorado por inteiro, inclusive qualquer `FuncionarioReplace` dentro dele; o `PUT` atualiza somente o cabeçalho |

  A consequência precisa constar da documentação pública: **não há caminho para alterar netos sem assumir a substituição do nível pai**. Quem quer editar funcionários envia `TurnoReplace = True` com o estado completo dos turnos. É limitação da máscara binária por nível, preferível a inventar uma semântica de merge parcial que o Business Component não sustenta. Ignorar sem erro o array de funcionários quando o marcador está ausente mantém a coerência com o nível 2, onde coleção sem marcador também não é tocada.

  O que permanece para a Fase 3 não é a decisão, e sim a **comprovação**: confirmar na IDE que o Business Component remove os netos junto com a linha pai e que a ordem das operações não dispara erro de integridade. Isso é caso de teste do gate, não pendência de especificação.
- **Precedente.** A construção equivale a uma máscara de atualização por coleção, no espírito do `updateMask` adotado por APIs que expõem atualização parcial. A alternativa de uma máscara única em string foi considerada e descartada nesta frente: booleanos tipados aparecem no schema OpenAPI e são validáveis, enquanto uma string livre esconde erro de digitação em silêncio.

### 11. Colisão de Nomes

- A geração passa a criar nomes que não existiam: o nó de coleção (`Parcelas`), o contador (`ParcelasCount`), o marcador (`ParcelasReplace`) e os SDTs por caminho. Qualquer um pode colidir com atributo de cabeçalho legitimamente presente na transação, e nomes longos podem colapsar entre si após encurtamento.
- **Regra de desambiguação determinística**, aplicada na montagem do plano de geração, com sufixo numérico estável derivado da ordem dos níveis — estável para que reexecuções produzam o mesmo nome e o fingerprint do contrato não oscile.
- **Verificação no preflight**, antes de qualquer escrita: colisão irresolúvel aborta a geração com mensagem clara e **nenhum objeto criado**, seguindo o padrão já adotado pelo SDT writer quando um tipo requerido não resolve.

### 12. Não Escopo Declarado

- **Subníveis não recebem endpoints próprios.** Não são gerados `GET /<tx>/{id}/<sublevel>`, `POST /<tx>/{id}/<sublevel>` nem `DELETE /<tx>/{id}/<sublevel>/{n}`. Os subníveis existem apenas como coleções aninhadas dentro dos serviços do cabeçalho.
- **Não há serviço `Delete` nesta frente**, em nenhum nível. A liberação do `Delete` de **cabeçalho** ficou na frente própria `B100` (concluída em 2026-08-30, opt-in). Continua **sem** `DELETE /<tx>/{id}/<sublevel>/{n}`.
- **Consequência prática, que precisa constar também na documentação pública:** remover uma **linha filha** continua sendo enviar o `Update` com `<Subnível>Replace = True` e omitir a linha. O `Delete` do `B100` apaga o registro do cabeçalho (e as filhas na mesma transação do BC), não uma linha isolada.
- **Filtros de `List` por campo de subnível ficam fora desta frente.** Filtrar o cabeçalho por conteúdo de linha exige condição de existência dentro do `For each` e muda a semântica da paginação; não é extensão trivial do que a frente entrega.
- **O `Get` devolve a árvore completa, sem paginação das linhas filhas.** Uma transação com muitas linhas produz resposta grande, e isso é limitação conhecida, não defeito. Nenhum teto é imposto agora: sem caso real medido, limitar seria otimização especulativa.

---

## Fases de Implementação Incremental

| Fase / Backlog | Escopo | Componentes Afetados |
|---|---|---|
| **Fase 0** | Linha de base de não regressão em duas camadas, conforme detalhado abaixo da tabela | `Tests/GenerationBaseline/` (nova cobertura), `scripts/Invoke-PrePushMechanicalChecks.ps1` |
| **Fase 1 (B095)** | Leitura hierárquica recursiva, modelo `ApiPlanLevel` e testes offline do núcleo (**concluída em 2026-08-25**) | `TransactionStructureReader.cs`, `TransactionAttributeKeyTraits.cs`, `ApiPlan.cs`, `Tests/TransactionStructure/` — nesta fase o Wizard flat ainda não consumia o leitor; B099a passou a consumi-lo. `PrototypePrimaryKeyReader.cs` intocado |
| **Fase 2 (B096)** | Geração de SDTs hierárquicos por contrato, regra de nomes e desambiguação (**concluída em 2026-08-26**, plano offline; writer físico inalterado) | `ApiPlanSdtGenerationPlan.cs`, `ApiPlanSdtHierarchicalNaming.cs`, `Tests/SdtHierarchicalPlan/` |
| **Fase 3 (B097)** | Geração de código Business Component nas Procedures (Get, Create, Update) e marcador `<Subnível>Replace` (**concluída em 2026-08-26**, Source offline) | `ApiPlanBusinessComponentWriter.cs`, `ApiPlanBusinessComponentHierarchicalSource.cs`, `ApiPlanHierarchicalContractMap.cs`, `Tests/BusinessComponentHierarchical/` |
| **Fase 4 (B098)** | Procedimento de List com contadores de subníveis diretos e `ListResponse_Item` condicionado (**concluída em 2026-08-26**, offline) | `ApiPlanListProcedureWriter.cs`, `ApiPlanSdtGenerationPlan.cs`, `ApiPlanListHierarchicalContract.cs`, `Tests/ListHierarchical/` |
| **Fase 5 (B099a)** | Interface do Wizard com agrupamento por nível, dependência entre níveis, controle de contador e aviso de profundidade (**concluída em 2026-08-26**; apply hierárquico permitido; metadata V2 fora) | `ApiPlanHierarchicalWizardSelection.cs`, `PrototypeWizardDialog.cs`, `ApiPlanBuilder.ResolveHierarchicalLevels`, `Tests/WizardHierarchical/` |
| **Fase 5-A (B099v)** | Validação em runtime do que as Fases 2 a 5 emitiram: correção da agregação `count()` com PK composta herdada, smoke HTTP multinível nos dois environments e critério 9 (contrato OpenAPI publicado). Antecede a Fase 6 porque a metadata V2 passa a gravar o contrato hierárquico: defeito de Source BC ou de List descoberto depois custa migração de integridade, e antes custa uma regravação | `ApiPlanListHierarchicalContract.cs` (`ResolveAggregateAttributeName`), `Tests/ListHierarchical/`, evidência de IDE/HTTP |
| **Fase 6 (B099b)** | Metadados hierárquicos (`schemaVersion` V2), sincronização e integridade (**concluída em 2026-08-28**) | `ApiPlanMetadataFileWriter.cs`, `ApiPlanTransactionSyncComparer.cs`, `ApiPlanTransactionSyncOrchestrator.cs` |
| **Fase 7** | Ciclo de vida sob hierarquia: releitura de contrato existente, preferências do Wizard e inventário dinâmico de remoção (**concluída em 2026-08-28**) | `PrototypeWizardExistingApiContractReader.cs`, `PrototypeWizardPreferencesCodec.cs`, `ApiPlanGeneratedApiRemovalInventory.cs`, `ApiPlanGeneratedApiRemovalPlan.cs` |

**Mecanismo da Fase 0 — duas camadas.** A escolha decorre de uma medição: `ApiPlan` tem construtor puro, os emissores de Source são estáticos e recebem apenas o plano, e `ApiPlanSdtGenerationPlanBuilder` não referencia o SDK — mas a forma física do SDT vive em `SDTStructure` e `KBModel`, e só sai da IDE. "Byte a byte" é alcançável para uma parte da saída, e não para a outra.

- **Camada offline, no checker mecânico.** Um teste monta `ApiPlan` sintético para duas ou três transações de nível único — chave simples, chave composta, uma com `NoAccept` — e grava arquivos de referência do Source de `Create`, `Update`, `Get` e `List`, do Service Source do API Object e do plano de SDT serializado em JSON. Divergência reprova o pré-push. É a camada que protege o defeito que motivou o critério: o falso positivo de adulteração no reencontro nasce do Source e do contrato, não da forma física do SDT.
- **Camada de IDE, manual e pontual.** Export/rematerialização XPZ da **forma física** dos SDTs de uma Transaction plana **já presentes na KB** no início da sprint, reconferida no fim. Cobre ordem de itens e propriedades do `SDTStructure`, que a camada offline não enxerga. **Não** exige regenerar a API nem instalar a DLL do dia da captura de início: o objetivo é âncora de deriva na KB ao longo da sprint, não paridade com o emissor atual (essa paridade de Source/Service Source/plano lógico fica na camada offline). Detalhe do início de 2026-08-25: `Docs/Implementation/2026-08-25-FASE0-LINHA-DE-BASE-NAO-REGRESSAO.md` e `Tests/GenerationBaseline/IdeXpz/CAPTURE-INICIO.md`.

**Reautorização da linha de base.** Quando a saída mudar legitimamente, a recaptura acontece em **commit próprio e isolado**, cujo diff contenha exclusivamente os arquivos de referência e a justificativa escrita da mudança. Recapturar no mesmo commit que altera o emissor transformaria a proteção em carimbo, porque ninguém revisa com atenção um diff que muda código e referência ao mesmo tempo.

**Pré-requisito das Fases 5 a 7 — ambientes de validação.** A validação na IDE depende de estrutura multinível preparada antes, em três ambientes com papéis distintos:

- **KB de teste `wsEducacaoSpTeste`** — transações multinível criadas para o teste, cobrindo os três casos que a frente precisa exercitar (um subnível direto, múltiplos subníveis paralelos, três níveis de profundidade), com `Create Database` nos environments `NETPostgreSQL155` e `NETFrameworkSQLServer004`. É o ambiente do gate e das chamadas HTTP reais.
- **Cópia local da KB de produção `Gx_FabricaBrasil`** — validação contra estrutura real, onde estão os casos que nenhuma transação sintética reproduz com fidelidade (`Empresa` com 13 subníveis paralelos, `DadosDoDia` com três níveis, `CondicaoPagamento -> Parcelas`). Uso somente para medir comportamento; nenhum XML de cliente é versionado neste repositório.
- **KB do GeneXus 18 U13** — a validação satélite começou na Transaction plana `Employee`; o smoke do critério 10 exige Transaction multinível. **Fechado em 2026-08-29** na `Teste`/`apiTeste` (quatro níveis) com Wizard + `Build All` sem `spc0018` nos dois environments — evidência `2026-08-29-CRITERIO10-SMOKE-GX18U13.md`. A leitura hierárquica continua sendo a superfície de SDK onde U13 e U15 já divergiram uma vez (`NoAccept`/`spc0018`).

---

## Versionamento da Metadata e Compatibilidade com a Alpha

A Fase 6 (`B099b`, concluída em 2026-08-28) acrescentou a estrutura de níveis à metadata própria. Antes dela, a Alpha gravava somente `GOAB_API_METADATA_B060_V1`; a partir de `B099b`, a **gravação** emite `GOAB_API_METADATA_B060_V2` e o reencontro/remoção validam o carimbo com tolerância V1+V2.

- A **leitura** aceita `V1` e `V2`. Metadata `V1` é interpretada como transação de nível único.
- A **gravação** emite sempre `V2`.
- **Não há passo de migração autônomo.** O arquivo `V1` só é convertido quando a geração for efetivamente aplicada àquela API, momento em que o arquivo já seria regravado e o SHA-256 recalculado de qualquer forma. Converter durante a simples abertura do Wizard alteraria a KB numa operação que o usuário entende como leitura, mudando o próprio mecanismo de integridade sem que ele tenha pedido.
- A mesma política de tolerância vale para `GOAB_WIZARD_PREFERENCES_V1`, tratado na Fase 7.
- Sem essa tolerância, toda API gerada na Alpha ficaria simultaneamente irreencontrável e **irremovível**, já que os dois caminhos validam o carimbo.

---

## Validação e Critérios de Sucesso

1. **Não regressão de transações planas**, com o escopo nomeado em vez de um "byte a byte" genérico:
   - **sob comparação automática, byte a byte:** Source das Procedures `Create`, `Update`, `Get` e `List`, Service Source do API Object e plano de SDT serializado, contra os arquivos de referência da camada offline da Fase 0. Divergência reprova o pré-push;
   - **sob conferência manual, no início e no fim da sprint:** a forma física dos SDTs já presentes na KB, pelo export/rematerialização XPZ da camada de IDE (sem exigir regeneração Wizard+DLL no dia da captura de início);
   - **fora da linha de base, deliberadamente:** tudo o que dependa de estado da KB do momento da captura.
   O critério existe porque o reencontro compara hashes do source contra o valor gravado na metadata: qualquer alteração incidental no emissor durante as Fases 2 a 4 acusaria APIs legítimas da Alpha de adulteração, repetindo o falso positivo diagnosticado em 2026-08-15.
2. **Testes automatizados offline:** teste unitário em `Tests/TransactionStructure/` (B095, árvore) e em `Tests/SdtHierarchicalPlan/` (B096, plano de SDT) com fixtures **sintéticas**, de nomes neutros, reproduzindo as formas que interessam — um subnível, múltiplos subníveis paralelos, três níveis, chave autonumerada, chave informada, fórmula de linha, `NoAccept` em subnível, colisão de membro, qualificador longo e cabeçalho sem filhos. O teste depende da forma da árvore e do plano emitido, não da semântica do cliente; XML de KB de cliente não é versionado neste repositório público.
3. **Compatibilidade canônica e satélite:** Compilação com 0 erros e 0 avisos em `Src/GenexusOpenApiBuilder.sln` e `Src/GenexusOpenApiBuilder.Gx18u13.sln`.
4. **Checker mecânico do repositório:** `pwsh -NoProfile -File scripts/Invoke-PrePushMechanicalChecks.ps1 -AsJson` executado e aprovado.
5. **Validação na IDE GeneXus 18:** Wizard executado em transação multinível na KB de teste, geração de SDTs/Procedures/API e `Build All` concluído com sucesso sem erros de especificação `spc0018`.
6. **Validação contra estrutura real:** Wizard executado na cópia local da `Gx_FabricaBrasil` sobre os casos de 13 subníveis paralelos e de três níveis, com resultado registrado por medição (contagens, avisos, bloqueios).
7. **Validação HTTP:** Chamadas reais `POST`, `GET`, `PUT` e `GET (List)` nos dois environments, validando persistência, substituição de linhas sob `<Subnível>Replace`, preservação das linhas quando o marcador está ausente, contadores e integridade do contrato de erro.
8. **Ida e volta do Wizard:** após gerar uma API multinível, reabrir o Wizard e confirmar que a seleção de níveis e atributos volta íntegra — proteção contra o modo de falha silencioso em que a segunda execução regrava a API sem os subníveis.
9. **Contrato OpenAPI publicado, ao fim da Fase 4** e não na véspera do corte, para que ainda haja sprint para reagir:

   **Prazo vencido — realocado em 2026-08-27; concluído em B099v (2026-08-28).** A Fase 4 fechou em 2026-08-26 e a Fase 5 fechou no mesmo dia; das três partes abaixo, só a trava mecânica offline foi executada na época. A conferência manual do YAML e a geração de cliente foram feitas na Fase 5-A (`B099v`) sobre a `apiTeste` de quatro níveis regerada. O critério permanece escrito aqui como está, para preservar o que foi decidido em 2026-08-23.

   - a trava mecânica offline `Tests/OpenApiContract/Test-OpenApiClientContractValidity.ps1` lê `ApiPlan.cs` (padrões de cabeçalho e serviços, desde `B107`) e, desde `B096`, também `ApiPlanSdtHierarchicalNaming.cs` para `_API_CreateRequest_`, `_API_UpdateRequest_`, `_API_Response_` e, desde `B098`, `_API_ListResponse_Item`. Sem esses padrões ela seguiria passando enquanto o contrato muda por baixo;
   - conferência **manual** do YAML nos dois environments (evidência pontual na IDE, não gate do pré-push), para uma transação com um subnível e outra com três níveis: coleções aninhadas presentes, referências de schema resolvidas, `ListResponse_Item` com os contadores e sem os arrays, corpo de erro conforme `B102`;
   - geração de cliente como **evidência pontual**, repetindo o método da Sprint 6 (`openapi-generator-cli 5.3.1`, `typescript-fetch` e `csharp`, Exit Code 0). Não entra como trava recorrente: depende de ferramenta externa e de rede, e o pré-push precisa continuar rodando offline;
   - se o gerador nativo não expressar bem os arrays aninhados, isso é limitação dele — `B088` já provou que não há ponto de extensão sem tocar a instalação. Decisão tomada de antemão: **não bloqueia a sprint**, e vira limitação documentada nos documentos 12 e 27 e nas notas do corte de subníveis.
10. **Smoke da linha `Gx18u13`**, no corte de subníveis: Wizard sobre transação multinível na KB U13, geração e `Build All` sem `spc0018`. Chamada HTTP permanece exclusividade do U15, nos dois environments. No corte de `B102` a garantia proporcional é menor — build Release da solution satélite e inventário offline de assembly —, porque ali a mudança é no emissor de Source, idêntico nas duas linhas, e não na leitura de estrutura pelo SDK. **Status 2026-08-29: aprovado** — `Docs/Implementation/2026-08-29-CRITERIO10-SMOKE-GX18U13.md`.
11. **Escala, com limiares declarados.** Uma API sobre transação de 13 subníveis paralelos gera no máximo `6 + 3N` SDTs próprios (N = filhos selecionados), isto é **45** quando todo filho tem campos nos três papéis, e mais de cinquenta objetos, contra doze no caminho plano. Filho cujo Create fica só com PK herdada (0 membros) **não** emite o SDT aninhado de Create — o GeneXus recusa SDT sem itens; na `Empresa` o critério 11 mediu **44** (`45 − 1`).
    - **Reprovam:** qualquer objeto órfão após `Remover API gerada`; colisão de nome que não resolva deterministicamente ou nome que estoure o limite do GeneXus sem o encurtamento previsto; `Build All` com erro na transação de 13 subníveis; Wizard acima de **30 s** para abrir ou para calcular o preview no pior caso da KB real, patamar em que o usuário lê travamento e não lentidão.
    - **Alertam, registram e seguem:** abertura ou preview acima de **5 s**, sinal de que a indexação por `GetAll` de 2026-08-06 não escalou para a forma hierárquica; aplicação completa acima de **60 s**.
    - Nenhum teto é imposto ao número de subníveis selecionáveis: seria a mesma trava artificial que a seção 8-A rejeitou para profundidade. A contagem de objetos no resumo do Wizard cumpre o papel de informar sem bloquear.
    - **Status 2026-08-29: aprovado** — `Docs/Implementation/2026-08-29-CRITERIO11-ESCALA-EMPRESA.md`. Alertas de tempo registrados; `Build All` Success nos dois environments; Remover sem órfão.

---

## Itens Correlatos Fora Desta Frente

Estes itens nasceram da revisão de 2026-08-23. Não pertencem a B095–B099 e têm gates próprios, mas condicionam a ordem de execução da Sprint 9.

| Item | Escopo | Posição |
|---|---|---|
| `B102` | Repasse do texto emitido pelo Business Component na `Message` do `422`, com `Message` em `LongVarChar`, `Messages[]` como coleção tipada por `sdt_API_ErrorMessage`, filtro por mensagens de erro, preferência por KB e escolha por API | **Primeiro item da Sprint 9**; concluído em 2026-08-24 (gate HTTP nos dois environments), antes da Fase 0 |
| `B100` | Serviço `Delete`, opt-in, com quatro camadas anti acidente | Após a Fase 7; **concluído em 2026-08-30**; corte `0.1.0-alpha.6` **publicado** em 2026-08-31 |
| `B105` | Escolha do chamador sobre o detalhe do corpo de erro, podendo apenas **restringir** o que o default da API permite, nunca ampliar | Fora de `B102`; nesta sprint se houver folga, senão Sprint 10 |
| `B101` | Experimento de membro nullable para distinguir membro ausente de membro vazio | Candidato à Sprint 10, fora da Sprint 9 |

**Ordem de execução resultante:** `B102` (concluído) → Fase 0 (concluída: camadas de início + conferência de fim em 2026-08-28, `CAPTURE-FIM.md`) → Fase 1/`B095` (concluída em 2026-08-25) → Fase 2/`B096` (concluída em 2026-08-26) → Fase 3/`B097` (concluída em 2026-08-26) → Fase 4/`B098` (concluída em 2026-08-26) → Fase 5/`B099a` (concluída em 2026-08-26) → Fase 5-A/`B099v` (concluída em 2026-08-28) → Fase 6/`B099b` (concluída em 2026-08-28) → Fase 7 (concluída em 2026-08-28) → `B100` (concluído em 2026-08-30).

`B105` nasceu separado de `B102` de propósito. Ele acrescenta parâmetro aos serviços `Create` e `Update`, muda a assinatura no API Object, muda o YAML e pede caso de teste HTTP próprio — e `B102` já é o primeiro item da sprint mexendo em quatro subsistemas. O que a Fase 0 precisa ter estabilizado é o **default por API**, que `B102` entrega.

`B102` precede a Fase 0 porque altera o bloco de erro emitido para **todas** as transações, planas inclusive. Executado depois, obrigaria a recapturar a linha de base no meio da sprint, justamente quando ela mais serve.
