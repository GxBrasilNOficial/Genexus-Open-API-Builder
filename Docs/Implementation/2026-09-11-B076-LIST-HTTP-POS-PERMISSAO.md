# B076 — HTTP do List da Laudo após permissão GAM

## Contexto

Em 2026-09-11, depois do `Build All` nos dois environments locais, a
primeira tentativa de validar o `List` da Transaction `Laudo` alcançou a API,
mas recebeu `403`, `code=139`, porque o principal de teste não tinha a
permissão específica `apilaudo_Services_List`. A extensão e os artefatos
gerados confirmavam `SecurityHigh` para o serviço.

O usuário abriu o GAM Backoffice do primeiro environment
(`NETFrameworkSQLServer`), acessou o usuário `goab_api_teste`, selecionou a
aplicação `wsEducacaoSpTeste` e adicionou `apilaudo_Services_List`. A lista
final do usuário mostrou a permissão como `Permitir`, com `Herdado=0`, portanto
como permissão direta do usuário.

## Teste HTTP no primeiro environment

Foram usadas as credenciais locais da pasta `Temp/` e o código secreto do
cliente já utilizado pela rotina HTTP anterior, sem expor ou versionar esses
valores. Nenhuma operação de escrita foi executada.

| Verificação | Resultado |
| --- | --- |
| `GET /apiLaudo/laudo` sem token | `401` |
| `POST /oauth/gam/v2.0/access_token` | `200`, token obtido |
| `GET /apiLaudo/laudo?Apipage=1&Apipagesize=50` com token | `200`, corpo `{}` |
| `GET` com filtro sem resultado | `200`, corpo `{}` |
| `GET` da página 2 com tamanho 1 | `200`, corpo `{}` |

O filtro válido não pôde ser medido: a resposta inicial não trouxe nenhum
`LaudoNumero` de onde extrair um valor existente. Também não foi possível
medir `Items`, `Pagination` ou `AppliedFilters`, porque o corpo recebido foi
um objeto JSON vazio. O segundo environment não foi alterado no GAM nesta
rodada e não deve ser considerado aprovado por este teste.

## Evidência do artefato gerado

No primeiro environment, os arquivos gerados foram conferidos em:

- `C:\KBs\wsEducacaoSpTeste\NETFrameworkSQLServer004\web\apiLaudo.yaml`;
- `C:\KBs\wsEducacaoSpTeste\NETFrameworkSQLServer004\web\OpenApi3\apiLaudo.json`;
- `C:\KBs\wsEducacaoSpTeste\NETFrameworkSQLServer004\web\apilaudo.cs`;
- `C:\KBs\wsEducacaoSpTeste\NETFrameworkSQLServer004\web\proclaudo_api_list.cs`.

O YAML e o JSON têm as rotas e os `operationId`, mas não emitem parâmetros,
schemas de resposta nem propriedades para `ListResponse`, `Items`,
`Pagination` e `AppliedFilters`. O `apilaudo.cs` gerado declara o método REST
`gxep_list()` como `void`: ele executa `worker.gxep_list()` e encerra sem
expor as saídas da Procedure ao contrato REST. Já
`proclaudo_api_list.cs` contém a lógica correta para montar
`ListResponse`, preencher itens e calcular paginação. A diferença confirma
que a falha está na ligação do API Object com o contrato público, não no
GAM nem na consulta ao banco.

O `Service Source` persistido no API Object corresponde ao caminho B054
(`List() => procLaudo_API_List()`), que não declara `ApiPage`, `ApiPageSize`,
filtros nem `out: &ListResponse`. No código do builder, o caminho B070 do
`ApiPlanListProcedureWriter` já possui a forma parametrizada e as saídas;
portanto, o próximo ajuste deve garantir que o API Object seja salvo com esse
contrato de List, em vez de deixar somente a Procedure atualizada ou manter
o writer B054.

## Classificação

- GAM direto no primeiro environment: **passou**.
- Autenticação sem/com token: **passou** (`401` / OAuth `200`).
- Alcance do endpoint autenticado: **passou** (`200`).
- Corpo e contrato funcional do `List`: **bloqueado** pelo corpo `{}`.
- B076: **não aprovado** nesta rodada.

Antes de repetir o HTTP, o Wizard deve produzir um relatório que mostre o
API Object salvo com o contrato de List parametrizado (e não apenas a
Procedure `procLaudo_API_List`). Depois disso, é necessário novo `Build All`
nos dois environments e nova medição de itens, filtro válido, filtro sem
resultado e página 2.
