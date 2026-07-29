# B070/B077 - List com Filtros, Paginacao e AppliedFilters

## Objetivo

Completar o endpoint `List` gerado pelo wizard sobre os objetos B040-B046, B050-B053, B054 e B055, cobrindo filtros elegiveis, paginacao, ordenacao deterministica, `totalCount`, `totalPages` e `AppliedFilters`.

## Implementacao

B070 passou a regravar a Procedure `proc<NomeBase>_API_List` e sincronizar o API Object correspondente.

A Procedure gerada:

- recebe `pApiPage` e `pApiPageSize` como parametros de entrada e copia para variaveis internas `ApiPage` e `ApiPageSize`;
- aplica valores padrao e limite maximo antes da consulta;
- valida intervalos e periodos antes do `For each`;
- usa `where ... when not &Filtro.IsEmpty()` para filtros simples, intervalos e periodos;
- calcula `FirstRecord`, `LastRecord`, `TotalCount` e `TotalPages`;
- preenche `Items` somente no recorte da pagina solicitada;
- ordena de forma deterministica, acrescentando a chave primaria completa como desempate quando necessario;
- inicializa `AppliedFilters` e preenche somente os filtros efetivamente informados.

O SDT writer passou a gravar membros nullable de `ListFilters` com a propriedade GeneXus `idJsonInclude=idJsonJsonNull`, correspondente a `Json Null Serialization = JSON null`. Essa configuracao e obrigatoria para filtros aplicados: sem ela, membro numerico nao informado serializa como `0`, o que falsamente indicaria filtro aplicado.

`ApplyList` executa primeiro um preflight sem escrita dos SDTs, da Procedure, do API Object, do Folder e dos tipos de variaveis planejados. Somente depois desse trio ser validado, reexecuta o reencontro conservador dos SDTs antes de alterar Procedure e API Object, mesmo quando a aba `SDTs` nao foi marcada no wizard. Assim, um `ListFilters` antigo e proprio da extensao e regravado para conter a estrutura B077 exigida antes do trio runtime ser sincronizado.

O preflight de B070 aceita migração conservadora das variantes intermediarias geradas durante a validacao manual da frente, incluindo a versao com `new()` invalido em C# e a versao condicional de `AppliedFilters`.

## Validacao Manual

Validado manualmente em 2026-07-29 no GeneXus 18 U15, usando a Transaction `Contrato` na KB `wsEducacaoSpTeste`.

Evidencia:

- wizard com `SDTs` e `List` marcados concluiu com B040-B046 reencontrando 7 SDTs e B070 aplicando `procContrato_API_List` e `apiContrato`;
- `sdtContrato_API_ListFilters.ContratoNumero` ficou com `Json Null Serialization = JSON null`;
- `Build All` especificou `apiContrato` e `procContrato_API_List`, gerou SDTs e documentação REST, compilou com sucesso e manteve apenas o warning ambiental conhecido de `FBiTextSharp.dll`;
- endpoint autenticado `GET /rest/apiContrato/List?Apipage=1&Apipagesize=10` retornou HTTP 200, `Items=2`, `TotalCount=2`, `TotalPages=1` e `AppliedFilters.ContratoNumero=null`;
- endpoint autenticado com `Contratonumero=222023` retornou HTTP 200, `Items=1`, `TotalCount=1`, `TotalPages=1` e `AppliedFilters.ContratoNumero=222023`;
- endpoint autenticado com `Apipage=2&Apipagesize=1` retornou HTTP 200, `Items=1`, `TotalCount=2`, `TotalPages=2` e `AppliedFilters.ContratoNumero=null`;
- endpoint autenticado com filtro sem resultado retornou HTTP 200, colecao sem `Items`, `TotalCount=0`, `TotalPages=0` e `AppliedFilters.ContratoNumero=999999999`.

## Limites Mantidos

B070/B077 nao fecha B076, codigos HTTP finais, `Get`, `Create`, `Update`, `Location`, seguranca explicita final nem validacao do YAML gerado. Esses itens continuam nas frentes seguintes da Sprint 6.
