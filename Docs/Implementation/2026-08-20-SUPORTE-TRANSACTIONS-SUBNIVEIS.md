# Suporte a Transactions com Subníveis (Multinível) — B095–B099

## Status

Frente em andamento na Sprint 9 (absorvendo os itens de backlog B095 a B099). Especificação funcional, levantamento na KB de produção e desenho da arquitetura de implementação concluídos. Próximo passo: execução da Fase 1 (B095 — Leitura hierárquica no SDK, modelo de domínio e testes unitários offline).

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
- Os SDTs de contrato suportam nós aninhados do tipo coleção (`IsCollection = true`):
  - `sdt<Tx>_API_Response`: campos do cabeçalho + nó de coleção para cada subnível selecionado.
  - `sdt<Tx>_API_CreateRequest`: campos do cabeçalho + nó de coleção para criação das linhas.
  - `sdt<Tx>_API_UpdateRequest`: campos do cabeçalho + nó de coleção para atualização das linhas.
  - `sdt<Tx>_API_ListResponse`: campos de resumo do cabeçalho + contadores calculados `<SubLevel>Count` (ex.: `ParcelasCount`, `ItensCount`).

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
- **`Update` (`proc<Tx>_API_Update`) — Substituição Completa:**
  - O endpoint `Update` adota a semântica REST `PUT` de **substituição completa e idempotente**:
    - O payload da coleção representa o estado final desejado para as linhas.
    - Linhas existentes no banco que forem omitidas no payload do subnível são removidas pelo Business Component.
    - Se o consumidor da API desejar preservar registros, ele deve enviá-los ou marcá-los com flags da aplicação (ex.: "cancelado").

### 5. Atomicidade e Controle Transacional do Business Component
- No GeneXus, o `Business Component` executa `Save()` sob uma única transação atômica nativa de banco de dados (cabeçalho + todas as linhas filhas):
  - Se `&BC.Success()` for verdadeiro: a Procedure executa `Commit`, gravando o conjunto completo.
  - Se `&BC.Success()` for falso: a Procedure **não** executa `Commit` e dispara `Rollback`, garantindo ausência de escrita parcial ou registros órfãos no banco de dados.

### 6. Contrato de Erros para Subníveis (Alinhamento com o Runtime e Emenda 2026-08-03)
- O SDT compartilhado `sdt_API_ErrorResponse` permanece top-level (`Code` e `Message`), sem array de erros aninhados.
- Em caso de falha de validação pelo Business Component em qualquer nível (cabeçalho ou linhas filhas) durante o `Save()`:
  - O código HTTP retornado é `422 Unprocessable Content`;
  - `&ErrorResponse.Code` retorna `!"validation_error"`;
  - `&ErrorResponse.Message` retorna `!"Business rules rejected the request."`;
  - As mensagens detalhadas do BC continuam sendo emitidas na janela Output/log da IDE via `&Messages = &BC.GetMessages()` e `msg(...)`.
  - Não é emitido array paralelo por linha, mantendo conformidade estrita com o contrato OpenAPI entregue no MVP.

### 7. Procedimento `List` (B098) — Resumo com Contadores
- A listagem geral paginada **não** aninha os arrays completos de subníveis para preservar a performance.
- O `For each` do `List` percorre o nível 1 (cabeçalho) e projeta contadores numéricos para cada subnível ativo (`&Item.<SubLevel>Count`), informando a quantidade de registros filhos sem inflar o payload HTTP.

### 8. Interface do Wizard (UX) e Sincronização Hierárquica (B099)
- A aba de seleção de campos agrupa os atributos por nível (seletor de nível / abas por nível / seções colapsáveis).
- O usuário pode selecionar granularmente quais atributos de cada subnível entram em Create, Update e Response.
- Subníveis sem nenhum atributo marcado não são gerados nos SDTs nem no código de BC.
- O comparador de sincronização (`Sincronizar com a Transaction`) compara adições, renomeações e remoções dentro da hierarquia de níveis com base nos `attributeGuid`s.

---

## Fases de Implementação Incremental

| Fase / Backlog | Escopo | Componentes Afetados |
|---|---|---|
| **Fase 1 (B095)** | Leitura hierárquica recursiva, modelo `ApiPlanLevel` e testes offline | `PrototypeWizardContract.cs`, `PrototypePrimaryKeyReader.cs`, `ApiPlan.cs`, `Tests/TransactionStructure/` |
| **Fase 2 (B096)** | Geração de SDTs hierárquicos com coleções | `ApiPlanSdtGenerationPlan.cs`, `ApiPlanSdtWriter.cs` |
| **Fase 3 (B097)** | Geração de código Business Component nas Procedures (Get, Create, Update) | `ApiPlanBusinessComponentWriter.cs` |
| **Fase 4 (B098)** | Procedimento de List com contadores de subníveis | `ApiPlanListProcedureWriter.cs` |
| **Fase 5 (B099a)** | Interface do Wizard com agrupamento por nível | `PrototypeWizardForm` e abas de seleção |
| **Fase 6 (B099b)** | Metadados hierárquicos, sincronização e integridade | `ApiPlanMetadataFileWriter.cs`, `ApiPlanTransactionSyncComparer.cs`, `ApiPlanTransactionSyncOrchestrator.cs` |

---

## Validação e Critérios de Sucesso

1. **Testes automatizados offline:** Teste unitário em `Tests/TransactionStructure/` com fixtures XML de transações multinível reais (ex.: `CondicaoPagamento`, `DadosDoDia`), validando navegação recursiva, chaves compostas por nível e herança de regras `NoAccept`.
2. **Compatibilidade canônica e satélite:** Compilação com 0 erros e 0 avisos em `Src/GenexusOpenApiBuilder.sln` e `Src/GenexusOpenApiBuilder.Gx18u13.sln`.
3. **Checker mecânico do repositório:** `pwsh -NoProfile -File scripts/Invoke-PrePushMechanicalChecks.ps1 -AsJson` executado e aprovado.
4. **Validação na IDE GeneXus 18:** Wizard executado em transação multinível na KB de teste, geração de SDTs/Procedures/API e `Build All` concluído com sucesso sem erros de especificação `spc0018`.
5. **Validação HTTP:** Chamadas reais `POST`, `GET`, `PUT` e `GET (List)` validando persistência, substituição de linhas, contadores e integridade do contrato de erro.
