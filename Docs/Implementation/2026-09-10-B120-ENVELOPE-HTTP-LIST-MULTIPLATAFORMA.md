# B120 — Envelope HTTP do `List` entre environments

**Estado:** aberto; urgente; diagnóstico e contorno de plataforma **confirmados**
(2026-09-24). Tentativas de preservar `ListResponse` com `Items` no .NET —
terceiro `out` (§11) e “gerar como o Get” com coleção no envelope (§12) —
**refutadas**. Contorno validado: outs flat (§10). A extensão **ainda não**
mudou o contrato emitido.

**Correlato de backlog:** [`B120`](../Foundation/06-BACKLOG_v0.1.md).

**Evidência principal (abertura):** [`B071-B073-B079-GET-CREATE-UPDATE-HTTP.md`](B071-B073-B079-GET-CREATE-UPDATE-HTTP.md),
seção «Primeiro smoke HTTP de `List` após os Build All».

**Evidência de contorno (2026-09-24):** probe descartável `apiProbeB120` (flat) na KB
`wsEducacaoSpTeste` (pasta `ProbeB120`) — ver §10.

**Evidência negativa (2026-09-24):** `apiProbeB120Wrap` (terceiro `out`) — §11;
probes Opt1 A–D (forma do SDT de dados) — §12.

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
front fora do GeneXus). Mitigação que mantém o sucesso achatado **também não**. Acrescentar
um terceiro `out` (§11) **também não**. Fazer o envelope “como o Get” **só funciona
sem coleção no SDT de dados** (§12 A/B); com `Items` (coleção de SDT ou de
`VARCHAR`) o unwrap volta (§12 C/D). O caminho validado para o `List` de produto é
**deixar de usar `ListResponse`** e expor outs flat (`Items` + `Pagination` +
`AppliedFilters` + `ErrorResponse`). Isso ainda não foi aplicado na extensão.

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

No artefato `NETPostgreSQL155\web\apinotafiscal_services.cs` (e no probe wrap
`apiprobeb120wrap_services.cs`), o padrão gerado é:

1. montar um `*_ResponseData` com as propriedades de saída;
2. contar quantas saídas “não nulas” entram em `nonNullCount`;
3. se `nonNullCount == 1`, retornar **só** esse valor (`GetResponse(nonNullData)`),
   achatando o envelope;
4. caso contrário, retornar o objeto `data` completo.

No `List` de `apiNotaFiscal` e nos probes com **coleção** no SDT de dados
(§11, §12 C/D):

- só esse SDT passa por `if (!…IsNull)` e incrementa `nonNullCount`;
- `ErrorResponse` (e outs satélite como `KeepAlive`) vão a `data` **sem**
  entrar no contador;
- no sucesso, `nonNullCount == 1` → unwrap → corpo = conteúdo do SDT
  (`Items` / `Pagination` / …);
- no `400` do produto, `&ListResponse = new()` cedo mantém o SDT “não null” →
  unwrap devolve o envelope interno e o `ErrorResponse` some do HTTP.

**Gatilho medido (§12):** o ramo `IsNull`/`nonNullCount++` aparece quando o SDT
de dados declara um membro **coleção** (`Items` de SDT **ou** de `VARCHAR`).
Sem coleção no SDT (Opt1 A) ou com SDT folha estilo Get (Opt1 B), o gerador
atribui direto a `data` e o contador fica 0 — mesmo padrão de
`Get`/`Create`/`Update` no `apinotafiscal_services.cs`.

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
  descarta na serialização);
- “faltou um terceiro out para empurrar `nonNullCount`” — refutado no §11;
- “dá para manter `ListResponse` com `Items` se o gerador emitir como o Get” —
  refutado no §12 (A/B sem coleção envelopam; C/D com coleção unwrapam).

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

### 4.2 Direção após os probes (2026-09-24)

Decisão humana explícita na sessão:

1. só documentar a limitação → **rejeitado** (APIs críticas para BFF/front);
2. mitigar mantendo sucesso achatado → **rejeitado**;
3. preservar `ListResponse` acrescentando um terceiro `out` → **refutado**
   (§11);
4. preservar `ListResponse` com `Items` fazendo o gerador emitir como o `Get`
   → **parcialmente esclarecido e fechado para o produto** (§12): o padrão Get
   só sai **sem** coleção no SDT; com `Items` o unwrap volta;
5. **contornar sem `ListResponse`**, expondo outs flat → **validado** (§10).

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
  `ErrorResponse` sem o unwrap do List);
- comunicar breaking change a consumidores Framework que já leem
  `body.ListResponse…` (o flat unifica com o que o PostgreSQL já devolve hoje).

Manter `ListResponse` **com** `Items` no contrato HTTP multiplataforma **não**
tem caminho medido na extensão: o gerador .NET unwrapa justamente esse desenho.

## 5. Plano de investigação — status

| # | Item | Status 2026-09-24 |
| --- | --- | --- |
| 1 | Versões GX / busca de correção oficial (U16, SAC) | Feito — sem fix anunciado |
| 2 | Reprodução mínima descartável (flat, wrap, Opt1) | Feito — §10–§12 |
| 3 | Comparar código gerado, YAML e HTTP sucesso/erro | Feito — §3, §10–§12 |
| 4 | Testar alternativa flat outs | Feito — passou (§10) |
| 4b | Testar preservar `ListResponse` + terceiro `out` | Feito — **falhou** (§11) |
| 4c | Testar Opt1 (forma do SDT / coleção) | Feito — gatilho = coleção (§12) |
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
- Probes `apiProbeB120` / `apiProbeB120Wrap` / `apiProbeB120Opt1*` / pasta
  `ProbeB120` na KB de teste são **descartáveis**; não são entrega do produto.
- Não reabrir terceiro `out` nem “ListResponse com Items gerando como Get” sem
  evidência nova de mudança no gerador GeneXus.

## 8. Dependências e saída esperada

Dependências: GeneXus 18 e seus dois geradores configurados, KB de teste,
`apiNotaFiscal` (sintoma), probes `apiProbeB120` / `apiProbeB120Wrap` /
`apiProbeB120Opt1*`, autenticação quando o serviço exigir, `Build All` /
Build With These Only reproduzível.

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

## 10. Probe `apiProbeB120` — outs flat (2026-09-24)

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

Query params gerados: `Apipage` / `Apipagesize` (não `page` / `pageSize`).

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

## 11. Probe `apiProbeB120Wrap` — terceiro `out` (2026-09-24)

Objetivo: manter `ListResponse` + `ErrorResponse` e acrescentar um `out` dummy
(`KeepAlive`) sempre preenchido, na hipótese de `nonNullCount` sair de 1 e o
gerador deixar de fazer unwrap. O flat (§10) **não** foi alterado.

### 11.1 Objetos

| Objeto | Papel |
| --- | --- |
| `apiProbeB120Wrap` | API; `GET /probe-b120-wrap`; `SecurityLevel(None)` |
| `procProbeB120_WrapList` | Preenche `ListResponse` + `ErrorResponse` + `KeepAlive` |
| `sdtProbeB120Wrap_ListResponse` | `Items` / `Pagination` / `AppliedFilters` (reusa SDTs do flat) |
| `sdtProbeB120Wrap_KeepAlive` | Dummy (`Marker`); preenchido no `200` e no `400` |

Assinatura:

```text
Wrap(in: &ApiPage, in: &ApiPageSize,
     out: &ListResponse, out: &ErrorResponse, out: &KeepAlive)
  => procProbeB120_WrapList(..., &RestStatusCode);
```

### 11.2 Wrapper gerado (.NET / PostgreSQL)

Em `NETPostgreSQL155\web\apiprobeb120wrap_services.cs`:

- só `ListResponse` tem `if (!AV8ListResponse.IsNull) { nonNullCount++; … }`;
- `ErrorResponse` e `KeepAlive` são atribuídos a `data` **sem** incrementar o
  contador;
- no sucesso, `nonNullCount == 1` → `GetResponse(nonNullData)` → unwrap.

O YAML OpenAPI declara `WrapOutput` com as três propriedades; o HTTP do .NET no
`200` **não** respeita esse schema.

### 11.3 Smoke HTTP

Query: `Apipage` / `Apipagesize`.

| Env | Caso | Status | Corpo observado |
| --- | --- | --- | --- |
| Framework | `Apipage=1&Apipagesize=10` | `200` | envelope `ListResponse`+`ErrorResponse`+`KeepAlive` |
| Framework | `Apipagesize=101` | `400` | `ErrorResponse`+`KeepAlive` |
| .NET (PG) | `Apipage=1&Apipagesize=10` | `200` | **unwrap** `Items`+`Pagination`+`AppliedFilters` (sem `KeepAlive`) |
| .NET (PG) | `Apipagesize=101` | `400` | `ErrorResponse`+`KeepAlive` (`ListResponse` omitido; `nonNullCount` ficou 0) |

### 11.4 Conclusão

**Acrescentar um terceiro `out` não evita o unwrap no .NET.** O gerador trata o
dummy como trata `ErrorResponse`: coloca em `data`, não conta. No Framework o
envelope já era estável; o terceiro out não muda o problema multiplataforma.

Hipótese “gerar como o Get” foi medida no §12: válida só sem coleção no SDT;
com `Items` (o desenho do produto) o unwrap permanece.

### 11.5 Detalhe de compilação no Framework

`&ListResponse.Pagination = new()` no Source GeneXus gerou `= new();`
(target-typed `new` do C# moderno) e falhou no MSBuild do Framework 4 com
CS1031. Contorno do probe: `new()` só em variável tipada local
(`&Pagination = new()` … `&ListResponse.Pagination = &Pagination`). Irrelevante
para o produto se o writer já seguir esse padrão.

## 12. Probes Opt1 A–D — forma do SDT de dados (2026-09-24)

Objetivo: descobrir o que faz o gerador .NET emitir o ramo `IsNull`/`nonNullCount`
(padrão List) versus atribuição direta (padrão Get), sem terceiro `out`. Flat e
wrap **não** foram alterados.

### 12.1 Matriz dos quatro probes

| Id | API / rota | SDT de dados | Pergunta |
| --- | --- | --- | --- |
| A | `apiProbeB120Opt1A` `/probe-b120-opt1-a` | `ListResponse` com `Pagination`+`Note` (**sem** coleção) | Envelope sem `Items` gera como Get? |
| B | `apiProbeB120Opt1B` `/probe-b120-opt1-b` | `LeafResponse` folha (`Label`) | Controle estilo Get |
| C | `apiProbeB120Opt1C` `/probe-b120-opt1-c` | `ListResponse` com `Items` (coleção de SDT) + Pagination + Filters | Baseline produto |
| D | `apiProbeB120Opt1D` `/probe-b120-opt1-d` | `ListResponse` com `Items` (coleção de `VARCHAR`) + Pagination | Coleção de primitivo também unwrapa? |

Assinatura comum (exceto B, que usa `LeafResponse`):

```text
Run(in: &ApiPage, in: &ApiPageSize, out: &ListResponse|&LeafResponse, out: &ErrorResponse)
  => procProbeB120_Opt1*(..., &RestStatusCode);
```

### 12.2 Wrapper gerado (.NET / PostgreSQL)

| Probe | `if (!….IsNull) nonNullCount++` no `*_services.cs`? |
| --- | --- |
| A | **Não** — `data.ListResponse = …;` direto (contador fica 0) |
| B | **Não** — `data.LeafResponse = …;` direto |
| C | **Sim** — só `ListResponse` |
| D | **Sim** — só `ListResponse` |

### 12.3 Smoke HTTP

Query: `Apipage` / `Apipagesize`. Base Framework e PostgreSQL locais.

| Probe | Env | 200 keys | 400 keys |
| --- | --- | --- | --- |
| A | Framework | `ListResponse`,`ErrorResponse` | `ListResponse`,`ErrorResponse` |
| A | .NET (PG) | `ListResponse`,`ErrorResponse` | `ListResponse`,`ErrorResponse` |
| B | Framework | `LeafResponse`,`ErrorResponse` | `LeafResponse`,`ErrorResponse` |
| B | .NET (PG) | `LeafResponse`,`ErrorResponse` | `LeafResponse`,`ErrorResponse` |
| C | Framework | `ListResponse`,`ErrorResponse` | `Code`,`Message` (corpo achatado de erro) |
| C | .NET (PG) | **unwrap** `Items`,`Pagination`,`AppliedFilters` | `ErrorResponse` |
| D | Framework | `ListResponse`,`ErrorResponse` | `Code`,`Message` |
| D | .NET (PG) | **unwrap** `Items`,`Pagination` | `ErrorResponse` |

### 12.4 Conclusão Opt1

1. O padrão Get (sem unwrap) **é reproduzível** no método “List” quando o SDT de
   dados **não tem membro coleção** (A, B) — envelope estável nos dois envs.
2. Qualquer `Items` coleção no envelope — de SDT (C) ou de `VARCHAR` (D) —
   restaura o ramo `IsNull` e o unwrap no .NET.
3. Para o `List` de produto, que **precisa** de coleção de itens, a opção 1
   **não** entrega `ListResponse` intacto no HTTP do .NET. O contorno continua
   sendo outs flat (§10).

## 13. Notas úteis para a implementação futura

1. **Não** tentar “consertar” o PostgreSQL editando `*_services.cs` gerado.
2. Expor `Items` como coleção + SDTs satélite + `ErrorResponse` (flat) foi
   suficiente no §10 para o gerador emitir sempre o envelope completo.
3. No erro, a Procedure do produto hoje instancia `ListResponse` cedo; com outs
   flat, garantir que `ErrorResponse` seja preenchido e que o status
   (`&RestStatusCode` / evento `*.After`) continue propagando `400`.
4. Diff esperado no YAML: sumir `ListOutput.ListResponse`; aparecer
   `Items` / `Pagination` / `AppliedFilters` / `ErrorResponse` no nível do
   schema de resposta do List (detalhe a fechar no desenho).
5. Consumidores BFF/front que já parseiam o achatado do PostgreSQL **e** o
   envelopado do Framework hoje estão em contrato divergente; a mudança flat
   unifica, mas é breaking para quem já acoplou a `ListResponse` no Framework.
6. Sessão SAC: login em `myaccount`/`developers` **não** autentica
   automaticamente `sac.genexus.com`; o banner “SIGNING IN…” some só após login
   no próprio SAC. Irrelevante para o runtime; útil para próximas varreduras de
   RN.
7. Não investir em “mais outs” (§11) nem em `ListResponse` com `Items` “estilo
   Get” (§12) sem mudança comprovada no gerador GeneXus.
8. Gatilho operacional para documentação interna: **membro coleção no SDT out
   de dados** ⇒ unwrap no .NET Core quando `nonNullCount == 1`.

## 14. Próximo passo técnico (quando a frente for aberta na extensão)

1. Desenhar o contrato flat do `List` (nomes JSON, SDTs, YAML, testes).
2. Implementar no writer e no plano de SDT; remover out `ListResponse` do
   serviço público.
3. Regenerar APIs de teste nos dois environments; Build All; smoke `200`/`400`
   em `apiNotaFiscal` (ou sucessor) **e** regressão Get/Create/Update/Delete.
4. Atualizar `CHANGELOG`, docs públicos afetados e este documento com o
   fechamento.
5. Remover ou arquivar os probes `ProbeB120` na KB quando não forem mais
   necessários.
