# Suporte a Transactions com Subníveis (Multinível)

## Status

Frente em andamento na Sprint 9. Especificação funcional, levantamento na KB de produção e desenho da arquitetura de implementação concluídos. Próximo passo: execução da Fase 1 (Leitura hierárquica no SDK e modelo de domínio).

---

## Diagnóstico e Evidência na KB de Produção

Para fundamentar as decisões de arquitetura e a profundidade de níveis suportada, foi realizada uma varredura em modo consulta nos arquivos XML de Transaction da KB de produção (C:\Dev\Prod\Gx_FabricaBrasil\ObjetosDaKbEmXml\Transaction):

- **Total de Transactions analisadas:** 196
- **Transactions com 1 nível (planas):** 176 (89,8%)
- **Transactions com mais de 1 nível (multinível):** 20 (10,2%)
- **Profundidade máxima de aninhamento:** **3 níveis** (DadosDoDia -> Turno -> Funcionario)
- **Padrões de subnível identificados:**
  - **11 transações** possuem 1 subnível direto (ex.: CondicaoPagamento -> Parcelas, VendaLiberado -> Item, Municipio -> Mapeamentos, PautaMinima -> Produto).
  - **8 transações** possuem **múltiplos subníveis paralelos** no 2º nível (ex.: Empresa com 13 subníveis paralelos, Tributacao com 7, Usuario com 6, CompraGadoItens com 5, Rota com 3, Comissao com 2, EmbarqueSaida com 2, OperacaoFiscal com 2).
  - **1 transação** possui aninhamento em profundidade de 3 níveis (DadosDoDia).

Esses dados comprovam que o suporte a múltiplos subníveis paralelos e recursão em árvore é indispensável para cobrir a KB real.

---

## Decisões Funcionais e Arquiteturais

### 1. Leitura Hierárquica da Estrutura
- A leitura via GeneXus SDK deve navegar recursivamente pela árvore de TransactionLevels a partir de 	ransaction.Structure.Root.
- Cada nível é mapeado com seu nome, chave primária composta (chave do cabeçalho + chave da linha), atributos próprios, fórmulas de linha, atributos inferidos da tabela estendida e regras NoAccept.

### 2. Geração de SDTs com Coleções
- Os SDTs de contrato devem suportar nós aninhados do tipo coleção (IsCollection = true):
  - sdt<Tx>_API_Response: campos do cabeçalho + nó de coleção para cada subnível selecionado.
  - sdt<Tx>_API_CreateRequest: campos do cabeçalho + nó de coleção para criação das linhas.
  - sdt<Tx>_API_UpdateRequest: campos do cabeçalho + nó de coleção para atualização das linhas.
  - sdt<Tx>_API_ListResponse: campos de resumo do cabeçalho + contadores calculados <SubLevel>Count (ex.: ParcelasCount, ItensCount).

### 3. Procedimento Get (proc<Tx>_API_Get)
- Carrega a Transaction completa via Business Component: &BC.Load(&PK).
- Atribui os campos do cabeçalho para &GetResponse.
- Itera sobre as linhas de cada subnível do BC carregado:
  `genexus
  For &BCItem in &BC.<SubLevel>
      &ResponseItem = new()
      &ResponseItem.<Campo1> = &BCItem.<Campo1>
      ...
      &GetResponse.<SubLevel>.Add(&ResponseItem)
  EndFor
  `

### 4. Procedimento Create (proc<Tx>_API_Create)
- Atribui campos do cabeçalho de &CreateRequest para &BC.
- Itera sobre as coleções de cada subnível do request, preenchendo os itens do BC:
  `genexus
  For &RequestItem in &CreateRequest.<SubLevel>
      &BCItem = new()
      &BCItem.<Campo1> = &RequestItem.<Campo1>
      ...
      &BC.<SubLevel>.Add(&BCItem)
  EndFor
  &BC.Save()
  `

### 5. Procedimento Update (proc<Tx>_API_Update) — Substituição Completa
- O endpoint Update adota a semântica REST PUT de **substituição completa e idempotente**:
  - O payload da coleção representa o estado final desejado para as linhas.
  - Linhas existentes no banco que forem omitidas no payload do subnível são removidas pelo Business Component.
  - Se o consumidor da API desejar preservar registros, ele deve enviá-los ou marcá-los com flags da aplicação (ex.: "cancelado").

### 6. Procedimento List (proc<Tx>_API_List) — Resumo com Contadores
- Para preservar a performance da paginação, a listagem geral **não** aninha os arrays completos de subníveis.
- O For each do List percorre o nível 1 (cabeçalho) e projeta contadores numéricos para cada subnível ativo (&Item.<SubLevel>Count), informando a quantidade de registros filhos sem inflar o payload HTTP.

### 7. Interface do Wizard (UX)
- A aba de seleção de campos agrupa os atributos por nível (seletor de nível / abas por nível / seções colapsáveis).
- O usuário pode selecionar granularmente quais atributos de cada subnível entram em Create, Update e Response.
- Subníveis sem nenhum atributo marcado não são gerados nos SDTs nem no código de BC.

---

## Fases de Implementação Incremental

| Fase | Escopo | Componentes Afetados |
|---|---|---|
| **Fase 1** | Leitura hierárquica e modelo de domínio | PrototypeWizardContract.cs, PrototypePrimaryKeyReader.cs, ApiPlan.cs |
| **Fase 2** | Geração de SDTs hierárquicos com coleções | ApiPlanSdtGenerationPlan.cs, ApiPlanSdtWriter.cs |
| **Fase 3** | Geração de código Business Component nas Procedures | ApiPlanBusinessComponentWriter.cs (Get, Create, Update) |
| **Fase 4** | Procedimento de List com contadores de subníveis | ApiPlanListProcedureWriter.cs |
| **Fase 5** | Interface do Wizard com agrupamento por nível | PrototypeWizardForm e abas de seleção |
| **Fase 6** | Metadados hierárquicos, sincronização e integridade | ApiPlanMetadataFileWriter.cs, ApiPlanTransactionSyncComparer.cs, ApiPlanTransactionSyncOrchestrator.cs |

---

## Validação e Critérios de Sucesso

1. **Testes automatizados offline:** Testes unitários cobrindo parsing de estruturas multinível, montagem do ApiPlanLevel, integridade de metadados e comparação de sincronização com subníveis.
2. **Checker mecânico do repositório:** pwsh -NoProfile -File scripts/Invoke-PrePushMechanicalChecks.ps1 -AsJson executado e aprovado.
3. **Validação na IDE GeneXus 18:** Wizard executado em transação multinível (ex.: CondicaoPagamento ou Pedido), geração de SDTs/Procedures/API e Build All concluído com sucesso sem erros de especificação spc0018.
4. **Validação HTTP:** Chamadas reais POST, GET, PUT e GET (List) validando persistência, substituição de linhas e contadores.
