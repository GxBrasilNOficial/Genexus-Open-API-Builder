# B120 — Envelope HTTP do `List` entre environments

**Estado:** aberto; urgente; diagnóstico e contorno de plataforma **confirmados**
(2026-09-24); a extensão **ainda não** mudou o contrato emitido. Falta desenhar e
aplicar a mudança no writer/YAML/testes.

**Correlato de backlog:** [`B120`](../Foundation/06-BACKLOG_v0.1.md).

**Evidência principal (abertura):** [`B071-B073-B079-GET-CREATE-UPDATE-HTTP.md`](B071-B073-B079-GET-CREATE-UPDATE-HTTP.md),
seção «Primeiro smoke HTTP de `List` após os Build All».

**Evidência de contorno (2026-09-24):** probe descartável `apiProbeB120` na KB
`wsEducacaoSpTeste` (pasta `ProbeB120`) — ver §10.

## 1. Decisão de encaminhamento

O problema deve virar uma frente urgente de backlog, mas não deve interromper a
conclusão da sprint `S-B111`. O smoke já demonstrou que a aplicação inicia, a
autenticação funciona e o endpoint responde nos dois environments; o que falhou
foi a equivalência observável do contrato de resposta do `List`.

`B120` não muda a próxima ação única do checkpoint, que na data deste registro era preparar a
F3 (durabilidade e remoção). **Remissão — 2026-09-14:** a F3 entregou as etapas P0, P1 e P2, a
última validada na IDE, e a próxima ação única passou a ser a P3; o que continua valendo aqui
é que `B120` não a altera.
As F1 e F2 foram encerradas, com a exceção explícita do `B121` fora da sprint; os cenários de
Sync já não são a pendência que condiciona este item. `B120` deve ser retomado conforme a ordem
do checkpoint e antes de considerar o contrato HTTP do `List` validado de forma multiplataforma.

**Atualização 2026-09-24:** apenas documentar a limitação **não** resolve (APIs para BFF /
front fora do GeneXus). Mitigação que mantém o sucesso achatado **também não**. O caminho
validado por probe é **deixar de usar `ListResponse`** e expor outs flat
(`Items` + `Pagination` + `AppliedFilters` + `ErrorResponse`). Isso ainda não foi
aplicado na extensão.

## 2. Problema observado

Em 2026-09-10, depois de `Build All` bem-sucedido nos dois environments, foi
executado o mesmo `GET /notafiscal` com e sem token, sem alteração de dados:

| Verificação | .NET Framework / SQL Server | .NET / PostgreSQL |
| --- | --- | --- |
| Sem token | `401` em JSON | `401` em JSON |
| Com token | `200`; 22 itens; página 1; tamanho 50; total 22; 1 página | `200`; 10 itens; página 1; tamanho 50; total 10; 1 página |
| Forma efetiva do corpo | `ListResponse` e `ErrorResponse` no nível raiz; paginação e filtros dentro de `ListResponse` | `Items`, `Pagination` e `AppliedFilters` no nível raiz; sem `ErrorResponse` no nível raiz |

Os totais diferentes são compatíveis com os dados de cada banco e não são o
defeito deste item. A divergência relevante é a forma do corpo para o mesmo
contrato gerado.

Os dois arquivos `apiNotaFiscal.yaml` declararam `ListOutput` com as
propriedades `ListResponse` e `ErrorResponse`. Portanto, o resultado atual é
incompatível com o contrato publicado, ainda que ambos os requests retornem
HTTP `200`.

**Complemento 2026-09-23 (mesmo contrato, erro de paginação):** com
`pageSize=99999` (acima do máximo), Framework devolve envelope com
`ErrorResponse.Code=invalid_request`; PostgreSQL (.NET) devolve só
`AppliedFilters` no nível raiz — **sem** `ErrorResponse` no corpo. O YAML nos
dois environments continua declarando `ListOutput` com as duas propriedades.

## 3. Diagnóstico confirmado (2026-09-23 / 2026-09-24)

### 3.1 O que a extensão emite

O writer continua emitindo:

- `List` declara `out: &ListResponse` e `out: &ErrorResponse`;
- a Procedure recebe `&ListResponse`, `&ErrorResponse` e `&RestStatusCode`;
- contrato validado por `ApiPlanServiceSourceContract`.

Dono do sintoma: **wrapper REST gerado pelo GeneXus** (artefato
`apinotafiscal_services.cs` no environment .NET), não a Procedure nem a consulta.

### 3.2 Mecanismo no gerador .NET (Core)

No artefato `NETPostgreSQL155\web\apiprobeb120_services.cs` / `apinotafiscal_services.cs`,
o padrão gerado é:

1. montar um `*_ResponseData` com as propriedades de saída;
2. contar quantas saídas “não nulas” entram em `nonNullCount`;
3. se `nonNullCount == 1`, retornar **só** esse valor (`GetResponse(nonNullData)`),
   achatando o envelope;
4. caso contrário, retornar o objeto `data` completo.

No `List` de `apiNotaFiscal`:

- só `ListResponse` passa por `if (!AV8ListResponse.IsNull)` e incrementa
  `nonNullCount`;
- `ErrorResponse` é atribuído a `data`, mas **nunca** entra no contador;
- no sucesso, `ListResponse` não é null → `nonNullCount == 1` → unwrap → corpo =
  conteúdo de `ListResponse` (`Items` / `Pagination` / `AppliedFilters`);
- no `400`, a Procedure já fez `&ListResponse = new()` no início → o SDT
  continua “não null” → o unwrap devolve o `ListResponse` (muitas vezes só com
  filtros) e o `ErrorResponse` some do HTTP.

No `Get` / `Create` / `Update` do **mesmo** `apinotafiscal_services.cs`, o
gerador **não** incrementa `nonNullCount` para o SDT de dados (atribui direto a
`data` e deixa o contador em 0) → sempre `GetResponse(data)` → envelope estável
com `*Response` + `ErrorResponse`. Por isso Get/Create/Update parecem “ok” nos
dois environments enquanto o `List` quebra no .NET.

Símbolos úteis no gerador (`Artech.Generator.DotNetCore.dll`):
`WRAP_SINGLE_API_OUTPUT`, `unwrap` / `unwrap_rest_parm`, `nonNullCount`,
`nonNullData`. O Framework também conhece esses conceitos, mas o caminho WCF /
controller observado para `apiNotaFiscal` **não** reproduz o unwrap do List.

Versão medida na sessão: GeneXus 18 **U15** (`18.0.15.188745`).

### 3.3 O que NÃO é a causa

- consulta SQL / dados do PostgreSQL;
- autenticação / IIS / DLL antiga da extensão;
- diferença de totais entre bancos;
- “faltou ErrorResponse no Source da Procedure” (ele é preenchido; o wrapper
  descarta na serialização).

### 3.4 GeneXus 18 Upgrade 16

Consultados (2026-09-23/24): wiki oficial do U16, release notes SAC autenticadas
(faixa V18 U15→U16 e buscas `REST` / `API object` / `serializ` / `JSON` /
`OpenAPI` / `unwrap` / `ErrorResponse` / `nonNull`).

**Não há indício de correção** do unwrap de múltiplos `out` REST entre
Framework e .NET Core. Itens próximos no U16 (Tags OpenAPI, Required, enums no
YAML, .NET 10, libs, ordem determinística PostgreSQL, SAC 61708 Json Name, SAC
61553 coleção de booleanos) **não** descrevem este sintoma.

Conclusão operacional: **não contar com o U16 como fix do B120**; o contorno é
da extensão (contrato emitido).

## 4. Contrato — evolução da decisão

### 4.1 Contrato histórico (até o probe)

Enquanto só havia o sintoma em `apiNotaFiscal`, o documento exigia preservar
`ListOutput` com `ListResponse` + `ErrorResponse` nos dois environments.

### 4.2 Direção após o probe (2026-09-24)

Decisão humana explícita na sessão:

1. só documentar a limitação → **rejeitado** (APIs críticas para BFF/front);
2. mitigar mantendo sucesso achatado → **rejeitado**;
3. **contornar sem `ListResponse`**, expondo outs flat → **validado por probe**
   antes de redesenhar o produto na extensão.

Envelope candidato a canônico do `List` (ainda não emitido pela extensão):

```text
raiz:
  Items            (coleção do item de resposta)
  Pagination       (SDT de paginação)
  AppliedFilters   (SDT de filtros)
  ErrorResponse    (sdt_API_ErrorResponse; presente também no sucesso, vazio)
```

Implicações quando a frente for implementada na extensão (ainda pendente):

- atualizar writer (`ApiPlanListProcedureWriter` / Service Source / Variables);
- atualizar SDT plan (deixar de publicar `ListResponse` como out do serviço, ou
  reaproveitar só internamente se fizer sentido);
- atualizar YAML OpenAPI, testes de contrato, docs públicos;
- regenerar nos dois environments e repetir a matriz §6;
- não quebrar `Get` / `Create` / `Update` / `Delete` (já usam `*Response` +
  `ErrorResponse` sem o unwrap do List).

## 5. Plano de investigação — status

| # | Item | Status 2026-09-24 |
| --- | --- | --- |
| 1 | Versões GX / busca de correção oficial (U16, SAC) | Feito — sem fix anunciado |
| 2 | Reprodução mínima descartável (duas formas de outs) | Feito — `apiProbeB120` |
| 3 | Comparar código gerado, YAML e HTTP sucesso/erro | Feito — ver §3 e §10 |
| 4 | Testar alternativa de contrato suportada (flat outs) | Feito — passou nos dois envs |
| 5 | Decidir dono + envelope canônico | Direção tomada (flat); falta desenho na extensão |
| 6 | Aplicar na extensão + Build All + HTTP | Pendente |
| 7 | Atualizar OpenAPI público / docs / testes do produto | Pendente (após §6) |

## 6. Matriz de aceitação

| Caso | Resultado exigido |
| --- | --- |
| `List` sem token | Mesmo status `401` e corpo de erro equivalente nos dois environments |
| `List` autenticado sem filtros | `200` e exatamente o mesmo envelope raiz declarado no YAML |
| `List` autenticado com filtro | Mesmo envelope, filtro aplicado e paginação equivalente |
| Página e tamanho válidos | Mesma forma de `Pagination`, com valores coerentes |
| Página ou tamanho inválidos | Mesmo status `400` e mesmo contrato de erro (`ErrorResponse` no corpo) |
| Contrato publicado | YAML, wrapper gerado e corpo HTTP real concordantes nos dois environments |
| Regressão | `Get`, `Create`, `Update` e `Delete` continuam passando seus testes existentes |

O aceite não pode ser baseado somente em `200`, em contagem de itens ou em um
único environment. Após a mudança para outs flat, o YAML deve declarar o
envelope flat (não mais `ListResponse` + `ErrorResponse` como único par raiz).

## 7. Limites e não objetivos

- Não editar manualmente `apinotafiscal.cs`, `apinotafiscal_services.cs` ou
  qualquer outro artefato gerado em `C:\KBs` como correção do produto.
- Não alterar, copiar ou reparar arquivos em `C:\Program Files (x86)\GeneXus`.
- Não remover `ErrorResponse` público sem uma decisão específica e uma revisão
  dos consumidores, pois isso pode regredir o contrato de erros do `List`.
- Não confundir este item com a lacuna de cenários de Sync da F1 da `S-B111`.
- Não considerar o Build Release da extensão suficiente para validar a forma
  serializada do runtime.
- Não fechar `B120` com base apenas em uma correção observada no PostgreSQL; os
  dois environments precisam ser comparados na mesma rodada.
- Probe `apiProbeB120` / pasta `ProbeB120` na KB de teste é **descartável**; não
  é entrega do produto e não substitui a mudança na extensão.

## 8. Dependências e saída esperada

Dependências: GeneXus 18 e seus dois geradores configurados, KB de teste,
`apiNotaFiscal` (sintoma), probe `apiProbeB120` (contorno), autenticação quando
o serviço exigir, `Build All` / Build With These Only reproduzível.

Saída esperada (ainda aberta): implementação na extensão do envelope flat,
evidência HTTP `200`/`400` nos dois environments sobre o `List` de produto
(não só o probe), YAML e docs alinhados.

## 9. Relação com o estado atual

- O smoke HTTP que abriu este item está registrado em
  [`B071-B073-B079-GET-CREATE-UPDATE-HTTP.md`](B071-B073-B079-GET-CREATE-UPDATE-HTTP.md).
- A próxima ação única vigente está no checkpoint
  [`STATUS_ATUAL_E_PROXIMO_PASSO.md`](../STATUS_ATUAL_E_PROXIMO_PASSO.md); a reconciliação da F1
  está em [`2026-09-10-S-B111-F1-RECONCILIACAO-EVIDENCIA.md`](2026-09-10-S-B111-F1-RECONCILIACAO-EVIDENCIA.md).
- A abertura de `B120` não promove a F1, não fecha sua lacuna de Sync e não
  altera a decisão de manter `B108` estacionado.

## 10. Probe `apiProbeB120` (2026-09-24)

### 10.1 Objetos (KB `wsEducacaoSpTeste`, pasta `ProbeB120`)

| Objeto | Papel |
| --- | --- |
| `apiProbeB120` | API; `GET /probe-b120-flat`; `SecurityLevel(None)` |
| `procProbeB120_FlatList` | Preenche outs flat; `pageSize` fora de 1–100 → `400` + `ErrorResponse` |
| `sdtProbeB120_Item` | Item (`Label`) |
| `sdtProbeB120_Pagination` | `Page` / `PageSize` / `TotalCount` |
| `sdtProbeB120_Filters` | `Note` |
| `sdt_API_ErrorResponse` | Reutilizado |

Assinatura do serviço (sem `ListResponse`):

```text
Flat(in: &ApiPage, in: &ApiPageSize,
     out: &Items, out: &Pagination, out: &AppliedFilters, out: &ErrorResponse)
  => procProbeB120_FlatList(..., &RestStatusCode);
```

### 10.2 Wrapper gerado (.NET / PostgreSQL)

Em `NETPostgreSQL155\web\apiprobeb120_services.cs` **não há** `nonNullCount` /
`IsNull` no caminho do `Flat`. O código atribui as quatro propriedades a
`apiprobeb120_flat_ResponseData` e faz sempre `return GetResponse(data)`.

### 10.3 Smoke HTTP

Base .NET: `http://localhost/wsEducacaoSpTesteNETPostgreSQL`  
Base Framework: `http://localhost/wsEducacaoSpTesteNETFrameworkSQLServer`

| Env | Caso | Status | Observação |
| --- | --- | --- | --- |
| .NET (PG) | `Apipage=1&Apipagesize=10` | `200` | `Items`+`Pagination`+`AppliedFilters`+`ErrorResponse` |
| .NET (PG) | `Apipagesize=999` | `400` | `ErrorResponse.Code=invalid_request` presente; `Items` omitido se vazio |
| Framework | `Apipagesize=10` | `200` | mesmo envelope de quatro chaves |
| Framework | `Apipagesize=999` | `400` | `ErrorResponse` presente; `Items: []` explícito |

Conclusão do probe: **outs flat sem `ListResponse` funcionam nos dois
environments** e preservam `ErrorResponse` no erro.

### 10.4 Bug operacional GeneXus (SDT novo)

SDTs criados na sessão às vezes **não** geram `type_Sdt*.cs` no primeiro
`Build All` (CS0246: tipo `SdtsdtProbeB120_*` / `*_RESTInterface` não
encontrado). Contorno conhecido: **Build With These Only** nos objetos do
probe, ou **Rebuild All**. Não é o B120 em si; atrasa só a compilação do
experimento.

Warnings `spc0022` (`&ApiPage` / `&ApiPageSize` input-only atribuídos):
cosméticos no probe; o padrão do produto usa parâmetros `pApiPage` e cópia
interna.

## 11. Notas úteis para a implementação futura

1. **Não** tentar “consertar” o PostgreSQL editando `*_services.cs` gerado.
2. Alinhar o `List` ao padrão que o gerador .NET já trata bem nos outros
   verbos: múltiplas propriedades no `ResponseData` **sem** o ramo
   `if (!*.IsNull) nonNullCount++` associado a um único SDT envelope.
3. Expor `Items` como coleção + SDTs satélite + `ErrorResponse` foi suficiente
   no probe para o gerador emitir sempre o envelope completo.
4. No erro, a Procedure do produto hoje instancia `ListResponse` cedo; com outs
   flat, garantir que `ErrorResponse` seja preenchido e que o status
   (`&RestStatusCode` / evento `*.After`) continue propagando `400`.
5. Diff esperado no YAML: sumir `ListOutput.ListResponse`; aparecer
   `Items` / `Pagination` / `AppliedFilters` / `ErrorResponse` no nível do
   schema de resposta do List (detalhe a fechar no desenho).
6. Consumidores BFF/front que já parseiam o achatado do PostgreSQL **e** o
   envelopado do Framework hoje estão em contrato divergente; a mudança flat
   unifica, mas é breaking para quem já acoplou a `ListResponse` no Framework.
7. Sessão SAC: login em `myaccount`/`developers` **não** autentica
   automaticamente `sac.genexus.com`; o banner “SIGNING IN…” some só após login
   no próprio SAC. Irrelevante para o runtime; útil para próximas varreduras de
   RN.

## 12. Próximo passo técnico (quando a frente for aberta na extensão)

1. Desenhar o contrato flat do `List` (nomes JSON, SDTs, YAML, testes).
2. Implementar no writer e no plano de SDT; remover out `ListResponse` do
   serviço público.
3. Regenerar APIs de teste nos dois environments; Build All; smoke `200`/`400`
   em `apiNotaFiscal` (ou sucessor) **e** regressão Get/Create/Update/Delete.
4. Atualizar `CHANGELOG`, docs públicos afetados e este documento com o
   fechamento.
5. Remover ou arquivar o probe `ProbeB120` na KB quando não for mais necessário.
