# Registro de Mudanças Estruturais do Commit b4a70f6, Modelo de Dados e Defeito Histórico ErrorItem

**Data de Registro:** 2026-08-05
**Autores:** Equipe de Desenvolvimento / Antigravity Agent
**Escopo:** Commits `ee09fa3`, `b4a70f6` e correções vigentes em `main`.

---

## 1. Reorganização do Modelo de Dados da Transaction Teste

### Histórico e Raciocínio
A Transaction de testes funcionais `Teste` teve sua estrutura expandida para utilizar uma **chave primária composta de três partes**:
- `TesteId` (Numeric 6.0)
- `TesteDate` (Date)
- `TesteCodigo` (VarChar 20)

### Impacto de Banco de Dados e Reorganização
Essa alteração alterou a definição física da tabela no banco de dados da Knowledge Base. Em ambos os ambientes de execução (.NET Framework em IIS e .NET Core / .NET Kestrel):
1. A reorganização de tabela (Database Reorganization) foi executada e confirmada.
2. Os dados de teste e estruturas de índices foram atualizados.
3. A chave de três partes exige que o segmento de texto `TesteCodigo` seja incluído nas preferências e seleções do wizard em `CreateFields` para que o serviço `Create` monte a URL completa do cabeçalho `Location`.

---

## 2. Mudanças Estruturais de Gravabilidade em b4a70f6

### Contexto da Falha do SDK GeneXus
Durante a refatoração para gravação em 1 clique (single-pass execution), a tentativa inicial de salvar a Procedure atribuindo `ProcedurePart.Source` e `Rules.Source` antes de atualizar `procedure.Variables` resultou no erro interno do SDK:
> `"Validation of Procedure failed"`

### Causa Raiz
O parser de compilação/validação interna do SDK GeneXus exige que todas as variáveis referenciadas nas regras `parm(...)` (ex.: `&CreateRequest`, `&CreateResponse`, `&ErrorResponse`, `&RestStatusCode`) já existam na coleção `procedure.Variables` no momento em que `Rules.Source` ou `ProcedurePart.Source` é atribuído e validado.

### Correção Aplicada em b4a70f6
1. **Inversão da Ordem de Atualização da Procedure (`SaveProcedure`):**
   - Primeiro chama `ReplaceVariables(model, procedure, variables)` para popular/atualizar a coleção de variáveis do objeto;
   - Em seguida, atribui `procedure.Rules.Source = rules;`;
   - Atribui `procedure.ProcedurePart.Source = source;`;
   - Invoca `procedure.Save()`.

2. **Inversão da Ordem entre API Object e Procedure (`SaveApi` antes de `SaveProcedure`):**
   - `SaveApi` grava primeiro as variáveis do API Object e expõe as rotas REST (`ServiceGroupSource`);
   - Em seguida, as Procedures filhas de BC/List são salvas com garantia de resolução de escopo e referências de parâmetros.

3. **Garantia de Preflight Conservador:**
   - As verificações de preflight (`IsManagedCreateSource`, `IsManagedUpdateSource`, `IsManagedGetSource`, `IsManagedListSource`) continuam sendo avaliadas antes do início das gravações. Se qualquer Procedure ou API Object for externo ou tiver sido alterado manualmente pelo usuário, a gravação é blocked preventivamente antes de realizar qualquer alteração parcial na KB.

---

## 3. Análise do Defeito Histórico de ErrorItem (Commit Published 48955bd)

### Histórico da Introdução
No commit `48955bd` ("Completa runtime REST de Get Create e Update"), o gerador de Procedure de Business Component incluía o seguinte bloco no código fonte gerado:
```genexus
For &Message in &Messages
    &ErrorItem = new()
    &ErrorItem.Code = !"business_rule"
    &ErrorItem.Message = &Message.Description
    &ErrorItem.Field = !""
    &ErrorResponse.Errors.Add(&ErrorItem)
EndFor
```

### Causa da Quebra e Período de Ocorrência
Em 2026-08-03, o SDT compartilhado `sdt_API_ErrorResponse` teve sua estrutura simplificada, e a coleção filha `Errors` (do tipo `sdt_API_ErrorResponse.Error`) foi removida da definição do SDT.

Como consequência:
- Entre 2026-08-03 e a remoção efetuada no commit `b4a70f6`, qualquer geração do zero da Procedure `Create` ou `Update` gerava código referenciando `&ErrorResponse.Errors` e a variável `&ErrorItem` (tipo `sdt_API_ErrorResponse.Error`), objetos que não existiam no SDT atualizado.
- A compilação da Procedure dentro do GeneXus falhava ao tentar resolver a propriedade `.Errors` e o tipo `sdt_API_ErrorResponse.Error`.

### Por Que Não Foi Detectado Imediatamente
As validações manuais realizadas no período intermediário executavam sobre objetos **reencontrados** cuja estrutura de código foi preservada pelo preflight ou cujas variáveis não haviam sido regravadas do zero. Apenas no teste de gravação limpa em 1 clique do commit `b4a70f6` a regressão foi evidenciada e o bloco obsoleto foi removido.

### Mecanismos de Prevenção Contra Regressão
1. O bloco e a variável `&ErrorItem` foram totalmente removidos dos geradores `ApiPlanBusinessComponentWriter.cs`.
2. O teste automatizado `Tests/BusinessComponentWriter/Test-ApiPlanBusinessComponentWriterVariableContract.ps1` valida explicitamente a lista de variáveis e o contrato das Procedures REST, garantindo que `ErrorItem` não esteja presente e que `sdt_API_ErrorResponse` corresponda à assinatura achatada de erros.
