# 26-CONTRATO_FILTROS_PAGINACAO_ORDENACAO

## Contrato Funcional para List no MVP

**Projeto:** Genexus Open API Builder
**Versão:** v1.0
**Base Primária:** [Registro de decisões funcionais do MVP — 2026-07-14](../Decisions/2026-07-14-REGISTRO_DECISOES_FUNCIONAIS_MVP.md)
**Objetivo:** definir o contrato transversal de filtros, paginação e ordenação para o serviço `List` gerado pelo MVP.
**Idioma:** Português BR
**Público principal:** mantenedores humanos, colaboradores técnicos e apoio por IA
**Data:** Julho/2026

---

# 1. Papel do Documento

Este documento é fonte normativa para o comportamento do serviço `List`.

Ele deve ser referenciado por:

- 07-UX_WIZARD_INICIAL.md
- 08-MODELO_DADOS_E_METADATA.md
- 10-ENGINE_GERACAO_OBJETOS.md
- 12-REGRAS_CRIACAO_API_OBJECTS.md
- 15-TESTES_VALIDACAO_E_QUALIDADE.md

---

# 2. Escopo do MVP

O MVP deve gerar `List` com:

- filtros por atributos elegíveis
- paginação
- ordenação determinística
- envelope de resposta com itens, paginação e filtros aplicados

Não faz parte do MVP:

- linguagem dinâmica de consulta
- filtros por subnível
- ordenação arbitrária informada por string livre
- parâmetros públicos `sortBy` ou `sortDirection`
- busca textual global

---

# 3. Elegibilidade de Atributos

O wizard deve mostrar os atributos do primeiro nível da Transaction como candidatos a filtro.

Regras:

- pertence ao primeiro nível da Transaction
- tem tipo suportado pelo contrato de filtros
- atributos de subníveis não são oferecidos no MVP
- atributos tecnicamente inadequados aparecem desabilitados, com motivo
- atributos `LongVarChar`, `Image`, `Audio`, `Video` e tipos ainda não validados aparecem desabilitados
- tipos disponíveis somente no GeneXus Next, como `Embedding`, permanecem desabilitados até validação específica
- `DateTime` com `DateFormat = None` usa somente igualdade no MVP

Padrões de seleção:

- todas as partes da chave primária vêm marcadas por padrão
- o `Description Attribute`, quando existir, vem marcado por padrão
- os demais atributos vêm desmarcados
- campos de auditoria operacional podem ser filtros, mas vêm desmarcados por padrão
- campos sensíveis elegíveis vêm desmarcados e com alerta explícito

Campos sensíveis, tokens e credenciais nunca devem ser devolvidos em `appliedFilters`.

---

# 4. Tipos de Filtro

## Texto

Operadores disponíveis:

- `Igual`
- `Contém`
- `Começa com`

Regras:

- cada atributo textual usa um único operador
- chaves primárias textuais usam `Igual` por padrão
- demais textos usam `Contém` por padrão
- o parâmetro público preserva o nome do atributo
- `Termina com` não integra o MVP
- a extensão não promete busca indiferente a maiúsculas e minúsculas; o comportamento segue DBMS e collation

## Numérico

Regras:

- chaves primárias numéricas usam somente `Igual`
- chaves estrangeiras numéricas usam somente `Igual`
- domínios enumerados usam somente `Igual`, mesmo quando o tipo físico for numérico
- demais numéricos usam `Igual` por padrão
- demais numéricos podem receber `Usar intervalo`, desmarcado por padrão, quando aplicável

Quando `Usar intervalo` estiver marcado:

- gerar parâmetros opcionais e independentes `NomeDoAtributoMin` e `NomeDoAtributoMax`
- os limites são inclusivos
- `Min` maior que `Max` retorna `400 Bad Request`
- igualdade e intervalo não são usados simultaneamente para o mesmo atributo

## Data e DateTime

Regras:

- `Date` e `DateTime` podem receber a opção `Usar período`
- `Usar período` vem marcado por padrão quando o campo for selecionado como filtro
- o usuário pode desmarcar para gerar igualdade direta
- limites `From` e `To` são opcionais e independentes
- período com início posterior ao fim retorna `400 Bad Request`
- parâmetros preservam o nome do atributo com sufixos `From` e `To`
- período de `DateTime` usa parâmetros públicos do tipo `Date`
- em `DateTime`, o período considera somente a parte da data
- para `Date`, início e fim são inclusivos
- para `DateTime`, o início é o começo do dia e o limite final é exclusivo, correspondente ao começo do dia seguinte
- limites efetivamente aplicados aparecem em `appliedFilters` como datas `YYYY-MM-DD`

Se `Usar período` for desmarcado, haverá apenas o parâmetro com o nome e tipo originais do atributo para igualdade direta.

## Boolean, Guid e enumerados

Regras:

- usam somente `Igual`
- não recebem intervalo nem operadores textuais
- preservam o tipo e os valores definidos pelo domínio enumerado

---

# 5. Paginação

Parâmetros públicos mínimos:

- `page`
- `pageSize`

Regras:

- `page` tem padrão fixo `1`
- `page` não é campo configurável no wizard do MVP
- `Default Page Size` é editável no wizard e inicia em `50`
- `Maximum Page Size` é editável no wizard e inicia em `200`
- a validação exige `1 <= Default Page Size <= Maximum Page Size`
- `page` abaixo de `1` retorna `400 Bad Request`
- `pageSize` abaixo de `1` retorna `400 Bad Request`
- `pageSize` acima do máximo configurado retorna `400 Bad Request`
- a API não reduz `pageSize` silenciosamente
- paginação não pode ser desativada no MVP
- valores configurados são preservados na metadata

O envelope usa `sdt_API_Pagination`, definido no documento 27, com `Page`, `PageSize`, `TotalCount` e `TotalPages`.

---

# 6. Ordenação

Ordenação é estática, definida no wizard e preservada na metadata.

Regras:

- o usuário pode selecionar zero, um ou vários atributos ordenáveis
- cada atributo selecionado tem direção ascendente ou descendente
- o padrão é a chave primária completa, na ordem da Transaction, ascendente
- a ordem no wizard define prioridade de ordenação
- se o usuário escolher outra ordenação, partes ausentes da chave primária são acrescentadas ao final como desempate ascendente
- se nenhum atributo for selecionado, usa-se a chave primária completa ascendente
- não há `sortBy` nem `sortDirection` públicos no MVP

`totalCount` deve ser confiável e representar o total depois da aplicação dos filtros.

---

# 7. ListResponse

`ListResponse` deve conter:

- `items`
- `pagination`
- `appliedFilters`

`items` contém elementos do SDT de resposta principal.

**Nota de revisão — 2026-08-23 — Suporte a Subníveis:** a regra acima permanece exata para transação de nível único. Havendo subnível selecionado, `items` passa a conter elementos de `sdt<NomeBase>_API_ListResponse_Item`: os mesmos campos de cabeçalho do `Response`, **sem** os membros de coleção, mais os contadores `<Subnível>Count` dos subníveis diretos. A listagem continua não aninhando as coleções, por decisão de performance; publicar o `Response` aqui traria arrays permanentemente vazios, que o consumidor leria como ausência de linhas. **Atualização de 2026-08-26 (B096):** esse tipo de `items` é `B098`; até lá, inclusive no plano hierárquico já emitido, `items` permanece coleção de `sdt<NomeBase>_API_Response`. Detalhes na `Emenda técnica — 2026-08-23`.

**Confirmação de restrição — 2026-08-23:** a decisão de não oferecer atributos de subnível como filtro (seções 3 e 9) **permanece deliberada** depois da frente de subníveis, e não é pendência a resolver. Filtros continuam vindo somente do primeiro nível.

`pagination` deve usar contrato compartilhado documentado em `27-CONTRATO_HTTP_ERROS_E_SDTS_COMPARTILHADOS.md`.

`appliedFilters`:

- usa o SDT específico `sdtNomeDaTransacao_API_ListFilters`
- confirma somente filtros reconhecidos e aplicados
- mantém membros nulos quando o filtro não foi aplicado
- preserva `false`, `0` e string vazia como valores informados
- não é parâmetro de entrada
- não devolve campos sensíveis, tokens ou credenciais

Os filtros de entrada permanecem parâmetros planos da query string.

Lista válida sem resultados retorna `200 OK`, coleção vazia e totais zero, nunca `404`.

---

# 8. Ausência vs Valor Vazio

A geração de filtros opcionais deve distinguir:

- parâmetro ausente
- parâmetro presente com string vazia
- parâmetro presente com `false`
- parâmetro presente com `0`

Não usar campos auxiliares `Specified` no contrato público do MVP.

Essa distinção é gate técnico obrigatório. Ela não pode depender apenas de `IsEmpty()`. O spike deve validar como o objeto `API` informa a presença do parâmetro e, se necessário, avaliar recursos HTTP nativos do GeneXus sem alterar o tipo público nem recorrer a DLL.

---

# 9. Critérios de Aceite

- `List` compila e executa em cenário simples
- filtros oferecidos vêm somente do primeiro nível
- chaves primárias vêm marcadas por padrão
- filtros aceitos aparecem em `appliedFilters`
- filtros omitidos não são tratados como valores enviados
- `false`, `0` e string vazia não são confundidos com ausência
- paginação respeita padrão e limite máximo sem redução silenciosa
- `totalCount` e `totalPages` estão corretos
- ordenação é estável entre chamadas equivalentes
- não há filtros por subnível
