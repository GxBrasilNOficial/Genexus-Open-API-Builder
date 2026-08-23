# Suporte a Transactions com Subníveis (Multinível) — B095–B099

## Status

Frente em andamento na Sprint 9 (absorvendo os itens de backlog B095 a B099). Especificação funcional, levantamento na KB de produção e desenho da arquitetura de implementação concluídos.

Em 2026-08-23 a especificação passou por revisão dirigida e recebeu as decisões consolidadas na `Emenda técnica — 2026-08-23` do registro de decisões do MVP. A revisão acrescentou a Fase 0 (linha de base de não regressão), a Fase 7 (ciclo de vida sob hierarquia) e três itens correlatos fora da frente de subníveis (`B100`, `B101` e `B102`).

Próximo passo: executar `B102` (repasse da mensagem do Business Component), que precede a captura da linha de base da Fase 0 justamente para que os arquivos de referência já nasçam com o contrato de erro definitivo.

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
- Cada nível é representado no modelo `ApiPlanLevel` com `LevelName`, `Depth` (1 para cabeçalho, 2 para 1º subnível, 3 para aninhamento), `ParentLevelName`, `LevelOrder`, `PrimaryKey` composta do nível e listas de campos selecionados.

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

- `sdt<Tx>_API_Response`: campos do cabeçalho + membro coleção por subnível selecionado, tipado por `sdt<Tx>_API_<SubLevel>_Response`.
- `sdt<Tx>_API_CreateRequest`: campos do cabeçalho + membro coleção tipado por `sdt<Tx>_API_<SubLevel>_CreateRequest`.
- `sdt<Tx>_API_UpdateRequest`: campos do cabeçalho + membro coleção tipado por `sdt<Tx>_API_<SubLevel>_UpdateRequest`, mais o marcador `<SubLevel>Replace` descrito na seção 10.
- `sdt<Tx>_API_ListResponse`: campos de resumo do cabeçalho + contadores `<SubLevel>Count` descritos na seção 7.

Em profundidade maior que 2, o SDT do subnível contém, por sua vez, o membro coleção do nível seguinte, com nome composto pelo caminho (`sdt<Tx>_API_<SubLevel>_<SubSubLevel>_Response`). A regra de encurtamento de nome e o limite aceito pelo GeneXus devem ser medidos na Fase 2 antes da primeira escrita real.

**Consequência para o inventário de remoção:** a lista de SDTs próprios deixa de ser fixa. `ApiPlanGeneratedApiRemovalPlan` passa a ler os nomes gravados na metadata em vez da lista hardcoded de cinco nomes, sob pena de deixar órfãos na KB ao remover uma API multinível. A ordem de exclusão continua respeitando a dependência de tipos (um SDT referenciado não pode ser apagado antes de quem o referencia).

### 4. Geração de Código Business Component nas Procedures (B097)
- **`Get` (`proc<Tx>_API_Get`):**
  - Carrega a Transaction completa via Business Component: `&BC.Load(&PK)`.
  - Atribui campos do cabeçalho para `&GetResponse`.
  - Itera sobre as linhas de cada subnível do BC carregado:
    ```genexus
    For &BCItem in &BC.<SubLevel>
        &ResponseItem = new()
        &ResponseItem.<Campo1> = &BCItem.<Campo1>
        ...
        &GetResponse.<SubLevel>.Add(&ResponseItem)
    EndFor
    ```
- **`Create` (`proc<Tx>_API_Create`):**
  - Atribui campos do cabeçalho de `&CreateRequest` para `&BC`.
  - Itera sobre as coleções de cada subnível do request, adicionando itens ao BC:
    ```genexus
    For &RequestItem in &CreateRequest.<SubLevel>
        &BCItem = new()
        &BCItem.<Campo1> = &RequestItem.<Campo1>
        ...
        &BC.<SubLevel>.Add(&BCItem)
    EndFor
    &BC.Save()
    ```
- **`Update` (`proc<Tx>_API_Update`) — Substituição Completa sob marcador explícito:**
  - O endpoint `Update` adota a semântica REST `PUT` de **substituição completa e idempotente**, porém condicionada ao marcador `<SubLevel>Replace` descrito na seção 10:
    - Com `<SubLevel>Replace = True`, o payload da coleção representa o estado final desejado para as linhas, e linhas existentes no banco que forem omitidas são removidas pelo Business Component.
    - Com o marcador ausente ou `False`, as linhas daquele subnível não são tocadas e o `Update` atua somente sobre o cabeçalho.
    - Se o consumidor da API desejar preservar registros dentro de uma substituição, ele deve enviá-los ou marcá-los com flags da aplicação (ex.: "cancelado").
  - A ordem das linhas no payload não identifica a linha; a identificação é pela chave primária do subnível. A exceção está declarada na seção 9.

### 5. Atomicidade e Controle Transacional do Business Component
- No GeneXus, o `Business Component` executa `Save()` sob uma única transação atômica nativa de banco de dados (cabeçalho + todas as linhas filhas):
  - Se `&BC.Success()` for verdadeiro: a Procedure executa `Commit`, gravando o conjunto completo.
  - Se `&BC.Success()` for falso: a Procedure **não** executa `Commit` e dispara `Rollback`, garantindo ausência de escrita parcial ou registros órfãos no banco de dados.

### 6. Contrato de Erros para Subníveis (Alinhamento com o Runtime e Emenda 2026-08-03)
- O SDT compartilhado `sdt_API_ErrorResponse` permanece top-level (`Code` e `Message`), sem array de erros aninhados.
- Em caso de falha de validação pelo Business Component em qualquer nível (cabeçalho ou linhas filhas) durante o `Save()`:
  - O código HTTP retornado é `422 Unprocessable Content`;
  - `&ErrorResponse.Code` retorna `!"validation_error"`;
  - `&ErrorResponse.Message` carrega o texto emitido pelo Business Component, conforme `B102`. Até a conclusão de `B102`, a geração emite o texto fixo `!"Business rules rejected the request."`, que é o comportamento vigente na Alpha;
  - Não é emitido array paralelo por linha, mantendo conformidade estrita com o contrato OpenAPI entregue no MVP.

### 7. Procedimento `List` (B098) — Resumo com Contadores
- A listagem geral paginada **não** aninha os arrays completos de subníveis para preservar a performance.
- O `For each` do `List` percorre o nível 1 (cabeçalho) e projeta contadores numéricos para cada subnível ativo (`&Item.<SubLevel>Count`), informando a quantidade de registros filhos sem inflar o payload HTTP.
- **Mecanismo:** fórmula agregada nativa (`count()`) avaliada dentro do `For each` do cabeçalho, resolvida pelo GeneXus como agregação no próprio SQL. Fica descartado o `For each` aninhado com incremento manual de variável, que traria as linhas filhas ao servidor de aplicação apenas para contá-las.
- **Controle pelo usuário:** o contador é gerado por padrão para cada subnível selecionado e pode ser **desativado individualmente** no Wizard. O controle existe porque o custo cresce com o número de subníveis paralelos: uma transação como `Empresa`, com 13 subníveis, produziria 13 agregações por linha da página.
- **Somente subníveis diretos:** contadores são gerados apenas para subníveis de profundidade 2. Contador de neto seria uma soma achatada atravessando os pais (o total de funcionários do dia, perdendo a distribuição por turno), informação que o formato da listagem não comporta e cujo nome não denunciaria a agregação. Quem precisa do detalhe usa o `Get`, que devolve a árvore completa.

### 8. Interface do Wizard (UX) e Sincronização Hierárquica (B099)
- A aba de seleção de campos agrupa os atributos por nível (seletor de nível / abas por nível / seções colapsáveis).
- O usuário pode selecionar granularmente quais atributos de cada subnível entram em Create, Update e Response.
- Subníveis sem nenhum atributo marcado não são gerados nos SDTs nem no código de BC.
- Marcar um subnível aninhado exige que o subnível pai esteja marcado; a UI trata isso como dependência, não como escolha livre.
- Cada subnível selecionado exibe o controle de contador de `List` (ligado por padrão), conforme a seção 7.
- Transações com profundidade maior que 3 exibem aviso de **profundidade não validada**, sem bloquear a geração, conforme a seção 8-A.
- O comparador de sincronização (`Sincronizar com a Transaction`) compara adições, renomeações e remoções dentro da hierarquia de níveis com base nos `attributeGuid`s.

### 8-A. Profundidade Suportada

- O leitor de estrutura, o modelo de domínio e os geradores tratam níveis por **recursão genérica, sem limite artificial** no código. Limite embutido custaria mais para implementar do que a recursão honesta e transformaria um caso raro em bloqueio total do wizard, inclusive para o cabeçalho.
- A profundidade **3** é o alcance da **evidência**, não uma trava: é o que a KB de produção apresenta (`DadosDoDia -> Turno -> Funcionario`), o que os testes offline cobrem e o que a validação na IDE comprova.
- Acima de 3 níveis, o Wizard avisa que a profundidade não foi validada e deixa a decisão com o usuário, que pode desmarcar os níveis mais profundos.

### 9. Ordem das Linhas na Coleção

- As linhas de cada coleção são devolvidas na ordem da **chave primária do subnível, ascendente** — que é a ordem em que o Business Component materializa as linhas após o `Load()`. A decisão é declarar contratualmente o comportamento que já existe, e não reordenar em memória.
- A declaração entra na descrição do serviço e na documentação pública, porque o consumidor idempotente precisa saber que dois `GET` do mesmo registro trazem as linhas na mesma ordem, em qualquer dos dois geradores.
- No `Update`, **a ordem em que o cliente envia as linhas é irrelevante**: a identificação é pela chave do subnível. A exceção é a chave **autonumerada ou sequencial**, em que não há como identificar a linha pelo payload e a ordem do array passa a determinar a numeração atribuída. Sem essa ressalva escrita, reordenar um array de parcelas parece inócuo e troca os números de parcela de lugar.
- Ordem configurável por subnível fica fora desta frente; nada nesta decisão impede acrescentá-la depois de forma aditiva.

### 10. `Required` no Subnível e Marcador de Substituição

- **Campo obrigatório dentro da linha:** vale a mesma regra da `Emenda técnica — 2026-08-03` — `Required` significa preenchimento, não presença do membro JSON. A resposta continua `400` com `Code = "invalid_request"`, e a `Message` identifica a linha pelo caminho (`Parcelas[2].ParcelaValor`). Isso não cria estrutura nova no corpo de erro, que permanece top-level com `Code` e `Message`.
- **Coleção ausente ou vazia no `Create`:** significa zero linhas e é sucesso (`201`). O modelo GeneXus não obriga um subnível a ter linhas, e a geração não inventa essa obrigação.
- **Marcador `<SubLevel>Replace` no `Update`:** o `UpdateRequest` recebe um membro booleano por subnível selecionado.

| Valor recebido | Efeito sobre as linhas filhas |
|---|---|
| ausente ou `False` | não são tocadas; o `PUT` atualiza somente o cabeçalho |
| `True` | substituição completa pelo array enviado |
| `True` com array vazio | todas as linhas são removidas |

- **Por que o marcador existe.** A `Emenda técnica — 2026-08-03` comprovou que o corpo da requisição não permite distinguir membro ausente de membro vazio. Sem marcador, um `PUT` que **esquece** o array `Itens` seria indistinguível de um `PUT` que pede a remoção de todos os itens: o cliente atualizaria uma observação do cabeçalho e perderia as linhas, em silêncio e sem reversão. O marcador se apoia justamente na limitação — o default de um booleano é `False`, e ausente é indistinguível de `False`, de modo que o comportamento por omissão é o seguro.
- **Precedente.** A construção equivale a uma máscara de atualização por coleção, no espírito do `updateMask` adotado por APIs que expõem atualização parcial. A alternativa de uma máscara única em string foi considerada e descartada nesta frente: booleanos tipados aparecem no schema OpenAPI e são validáveis, enquanto uma string livre esconde erro de digitação em silêncio.

### 11. Colisão de Nomes

- A geração passa a criar nomes que não existiam: o nó de coleção (`Parcelas`), o contador (`ParcelasCount`), o marcador (`ParcelasReplace`) e os SDTs por caminho. Qualquer um pode colidir com atributo de cabeçalho legitimamente presente na transação, e nomes longos podem colapsar entre si após encurtamento.
- **Regra de desambiguação determinística**, aplicada na montagem do plano de geração, com sufixo numérico estável derivado da ordem dos níveis — estável para que reexecuções produzam o mesmo nome e o fingerprint do contrato não oscile.
- **Verificação no preflight**, antes de qualquer escrita: colisão irresolúvel aborta a geração com mensagem clara e **nenhum objeto criado**, seguindo o padrão já adotado pelo SDT writer quando um tipo requerido não resolve.

### 12. Não Escopo Declarado

- **Subníveis não recebem endpoints próprios.** Não são gerados `GET /<tx>/{id}/<sublevel>`, `POST /<tx>/{id}/<sublevel>` nem `DELETE /<tx>/{id}/<sublevel>/{n}`. Os subníveis existem apenas como coleções aninhadas dentro dos serviços do cabeçalho.
- **Não há serviço `Delete` nesta frente**, em nenhum nível. A liberação do `Delete` é tratada como frente própria em `B100`.
- **Consequência prática, que precisa constar também na documentação pública:** enquanto `B100` não estiver concluído, a única forma de remover uma linha filha é enviar o `Update` com `<SubLevel>Replace = True` e omitir a linha.

---

## Fases de Implementação Incremental

| Fase / Backlog | Escopo | Componentes Afetados |
|---|---|---|
| **Fase 0** | Linha de base de não regressão: captura dos arquivos de referência (golden files) da saída atual para transações de nível único, ligada ao checker mecânico | `Tests/` (nova cobertura), `scripts/Invoke-PrePushMechanicalChecks.ps1` |
| **Fase 1 (B095)** | Leitura hierárquica recursiva, modelo `ApiPlanLevel` e testes offline | `PrototypeWizardContract.cs`, `PrototypePrimaryKeyReader.cs`, `ApiPlan.cs`, `Tests/TransactionStructure/` |
| **Fase 2 (B096)** | Geração de SDTs hierárquicos por contrato, regra de nomes e desambiguação | `ApiPlanSdtGenerationPlan.cs`, `ApiPlanSdtWriter.cs` |
| **Fase 3 (B097)** | Geração de código Business Component nas Procedures (Get, Create, Update) e marcador `<SubLevel>Replace` | `ApiPlanBusinessComponentWriter.cs` |
| **Fase 4 (B098)** | Procedimento de List com contadores de subníveis diretos | `ApiPlanListProcedureWriter.cs` |
| **Fase 5 (B099a)** | Interface do Wizard com agrupamento por nível, dependência entre níveis, controle de contador e aviso de profundidade | `PrototypeWizardDialog.cs` e abas de seleção |
| **Fase 6 (B099b)** | Metadados hierárquicos (`schemaVersion` V2), sincronização e integridade | `ApiPlanMetadataFileWriter.cs`, `ApiPlanTransactionSyncComparer.cs`, `ApiPlanTransactionSyncOrchestrator.cs` |
| **Fase 7** | Ciclo de vida sob hierarquia: releitura de contrato existente, preferências do Wizard e inventário dinâmico de remoção | `PrototypeWizardExistingApiContractReader.cs`, `PrototypeWizardPreferencesCodec.cs`, `ApiPlanGeneratedApiRemovalPlan.cs`, `ApiPlanGeneratedApiRemover.cs` |

**Pré-requisito das Fases 5 a 7 — ambientes de validação.** A validação na IDE depende de estrutura multinível preparada antes, em dois ambientes com papéis distintos:

- **KB de teste `wsEducacaoSpTeste`** — transações multinível criadas para o teste, cobrindo os três casos que a frente precisa exercitar (um subnível direto, múltiplos subníveis paralelos, três níveis de profundidade), com `Create Database` nos environments `NETPostgreSQL155` e `NETFrameworkSQLServer004`. É o ambiente do gate e das chamadas HTTP reais.
- **Cópia local da KB de produção `Gx_FabricaBrasil`** — validação contra estrutura real, onde estão os casos que nenhuma transação sintética reproduz com fidelidade (`Empresa` com 13 subníveis paralelos, `DadosDoDia` com três níveis, `CondicaoPagamento -> Parcelas`). Uso somente para medir comportamento; nenhum XML de cliente é versionado neste repositório.

---

## Versionamento da Metadata e Compatibilidade com a Alpha

A Fase 6 acrescenta a estrutura de níveis à metadata própria, hoje carimbada `GOAB_API_METADATA_B060_V1` e validada por igualdade exata tanto no reencontro quanto no plano de remoção.

- A **leitura** passa a aceitar `V1` e `V2`. Metadata `V1` é interpretada como transação de nível único.
- A **gravação** emite sempre `V2`.
- **Não há passo de migração autônomo.** O arquivo `V1` só é convertido quando a geração for efetivamente aplicada àquela API, momento em que o arquivo já seria regravado e o SHA-256 recalculado de qualquer forma. Converter durante a simples abertura do Wizard alteraria a KB numa operação que o usuário entende como leitura, mudando o próprio mecanismo de integridade sem que ele tenha pedido.
- A mesma política de tolerância vale para `GOAB_WIZARD_PREFERENCES_V1`, tratado na Fase 7.
- Sem essa tolerância, toda API gerada na Alpha ficaria simultaneamente irreencontrável e **irremovível**, já que os dois caminhos validam o carimbo.

---

## Validação e Critérios de Sucesso

1. **Não regressão de transações planas:** para transação de nível único, a saída gerada (SDTs, source das Procedures e source do API Object) permanece **byte a byte idêntica** à linha de base capturada na Fase 0. O critério existe porque o reencontro compara hashes do source contra o valor gravado na metadata: qualquer alteração incidental no emissor durante as Fases 2 a 4 acusaria APIs legítimas da Alpha de adulteração, repetindo o falso positivo diagnosticado em 2026-08-15.
2. **Testes automatizados offline:** teste unitário em `Tests/TransactionStructure/` com fixtures **sintéticas**, de nomes neutros, reproduzindo as formas que interessam — um subnível, múltiplos subníveis paralelos, três níveis, chave autonumerada, chave informada, fórmula de linha e `NoAccept` em subnível. O teste depende da forma da árvore, não da semântica do cliente; XML de KB de cliente não é versionado neste repositório público.
3. **Compatibilidade canônica e satélite:** Compilação com 0 erros e 0 avisos em `Src/GenexusOpenApiBuilder.sln` e `Src/GenexusOpenApiBuilder.Gx18u13.sln`.
4. **Checker mecânico do repositório:** `pwsh -NoProfile -File scripts/Invoke-PrePushMechanicalChecks.ps1 -AsJson` executado e aprovado.
5. **Validação na IDE GeneXus 18:** Wizard executado em transação multinível na KB de teste, geração de SDTs/Procedures/API e `Build All` concluído com sucesso sem erros de especificação `spc0018`.
6. **Validação contra estrutura real:** Wizard executado na cópia local da `Gx_FabricaBrasil` sobre os casos de 13 subníveis paralelos e de três níveis, com resultado registrado por medição (contagens, avisos, bloqueios).
7. **Validação HTTP:** Chamadas reais `POST`, `GET`, `PUT` e `GET (List)` nos dois environments, validando persistência, substituição de linhas sob `<SubLevel>Replace`, preservação das linhas quando o marcador está ausente, contadores e integridade do contrato de erro.
8. **Ida e volta do Wizard:** após gerar uma API multinível, reabrir o Wizard e confirmar que a seleção de níveis e atributos volta íntegra — proteção contra o modo de falha silencioso em que a segunda execução regrava a API sem os subníveis.

---

## Itens Correlatos Fora Desta Frente

Estes itens nasceram da revisão de 2026-08-23. Não pertencem a B095–B099 e têm gates próprios, mas condicionam a ordem de execução da Sprint 9.

| Item | Escopo | Posição |
|---|---|---|
| `B102` | Repasse do texto emitido pelo Business Component na `Message` do `422`, com opção de desligar para API exposta publicamente | **Primeiro item da Sprint 9**, antes da Fase 0 |
| `B100` | Serviço `Delete`, opt-in, com quatro camadas anti acidente | Após a Fase 7 |
| `B101` | Experimento de membro nullable para distinguir membro ausente de membro vazio | Candidato à Sprint 10, fora da Sprint 9 |

**Ordem de execução resultante:** `B102` → Fase 0 → Fases 1 a 6 → Fase 7 → `B100`.

`B102` precede a Fase 0 porque altera o bloco de erro emitido para **todas** as transações, planas inclusive. Executado depois, obrigaria a recapturar a linha de base no meio da sprint, justamente quando ela mais serve.
