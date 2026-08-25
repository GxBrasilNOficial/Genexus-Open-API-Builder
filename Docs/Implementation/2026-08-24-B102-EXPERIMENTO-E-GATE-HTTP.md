# B102 — Experimento na IDE e gate HTTP

Fecha os dois gates humanos declarados na nota `Gate humano — fechado em 2026-08-24` do documento
`Docs/Foundation/27-CONTRATO_HTTP_ERROS_E_SDTS_COMPARTILHADOS.md` e registra a evidência do gate HTTP
nos dois environments (`apiTeste`, 2026-08-24).

Executado em 2026-08-24, numa KB de teste, por sonda temporária instalada na extensão. Nenhuma
API gerada foi tocada; os SDTs da sonda usaram o prefixo próprio `sdt_GOAB_B102_` e foram
removidos ao final (`Deleted=6`).

---

## 1. Por que um experimento era necessário

Duas perguntas do `B102` não se respondiam por leitura de código nem por teste offline:

1. **Forma do corpo de erro.** A `Emenda técnica — 2026-08-03` retirou `Errors[]` depois que a IDE
   recusou uma tentativa com **subestrutura aninhada** dentro do próprio SDT
   (`sdt_API_ErrorResponse.Error`). Coleção tipada por um SDT **separado** é mecanismo distinto — o
   mesmo de `ListResponse.Items` — e nunca havia sido testado no corpo de erro.
2. **Comprimento declarado do `LongVarChar`.** O `Length` do membro é repassado direto ao SDK, e o
   repositório não tinha precedente: todos os usos de `LongVarChar` eram **variáveis**, que não
   carregam comprimento.

---

## 2. Fidelidade do experimento

A sonda **replica a sequência de chamadas do escritor real**, `ApiPlanSdtWriter.AddMember`, e não
uma rotina própria. É o que torna o resultado transferível para a geração de produção:

| `ApiPlanSdtWriter.AddMember` | Sonda |
|---|---|
| `root.AddItem(name, eDBType.GX_SDT)` | idem |
| `DataType.ParseInto(designModel, tipo, item)` | idem, com o mesmo tratamento de falha |
| `item.IsCollection = true` | idem |
| `item.CollectionItemName = <SDT>` | idem |

Para os membros escalares, a sonda usa `root.AddItem(nome, eDBType.LONGVARCHAR, length, 0)`, que é
exatamente o que `AddBuiltInMember` executa depois de resolver o tipo por `ResolveDbType`.

Toda leitura de resultado é feita **após `Save()` e releitura do SDT por GUID** — `SDTItem.Length`
e `SDTItem.Type` como o SDK devolve, nunca o valor enviado na criação.

---

## 3. Gate 1 — forma do corpo: **coleção aceita**

A IDE aceitou o membro coleção tipado por SDT separado no corpo de erro.

| Membro | Tipo observado | `isCollection` | `collectionItemName` |
|---|---|---|---|
| `Code` | `VARCHAR` (64) | false | — |
| `Message` | `LONGVARCHAR` | false | — |
| `Messages` | `GX_SDT` | **true** | `sdt_GOAB_B102_ErrorMessage` |

**Consequência para o `B102`:** o corpo de erro ganha o membro coleção `Messages`, tipado pelo SDT
compartilhado novo `sdt_API_ErrorMessage`. O ramo de contingência — concatenação por `" | "` como
forma única — **não** se aplica. `Message` permanece top-level e preenchida, concatenada, para não
quebrar consumidores da Alpha.

Isso também esclarece retroativamente a recusa de 2026-08-03: o problema era a subestrutura
aninhada, não o conceito de coleção.

O membro `Messages` veio com `lengthObserved: 4`. O valor não tem significado para referência de
SDT e não deve ser interpretado.

---

## 4. Gate 2 — `Length` do `LongVarChar`: **o SDK não determina**

Quatro valores foram criados e relidos. **Nenhum foi normalizado:**

| `lengthRequested` | `lengthObserved` | `typeObserved` | Aceito |
|---|---|---|---|
| 0 | 0 | `LONGVARCHAR` | sim |
| 2048 | 2048 | `LONGVARCHAR` | sim |
| 1048576 | 1048576 | `LONGVARCHAR` | sim |
| 2097152 | 2097152 | `LONGVARCHAR` | sim |

O experimento respondeu que **não há fato a descobrir**: o SDK é permissivo e preserva o que
recebe. A escolha do comprimento é, portanto, **decisão de design**, não observação — e quem
procurar um "valor correto" imposto pela plataforma não vai encontrar.

### Decisão

**`Length = 2097152`**, decidido pelo mantenedor em 2026-08-24, por alinhamento ao tamanho
convencional de `LongVarChar` no GeneXus. Vale para `sdt_API_ErrorResponse.Message` e para o membro
de texto de `sdt_API_ErrorMessage`.

Registre-se a distinção: a medição acima prova que o SDK **aceita e preserva** 2097152; que esse
seja o valor padrão do GeneXus para `LongVarChar` é conhecimento de plataforma do mantenedor, e não
algo que esta sonda tenha observado — ela nunca criou membro pela interface da IDE para ler o
padrão.

### Não confundir com o truncamento

O truncamento em cerca de 2K permanece no **código GeneXus gerado** (`SubStr` com reticência
final), independente do `Length` declarado. São limites distintos: um é declaração de tipo ao SDK,
o outro é limite operacional em runtime.

---

## 5. Premissas abertas pelo experimento — fechadas no gate HTTP

A sonda criou SDT e não gerou contrato OpenAPI nem chamou HTTP. Essas perguntas ficaram para o gate HTTP de `B102` e foram respondidas em 2026-08-24 nos YAMLs e nas chamadas de `apiTeste` (`NETPostgreSQL155` e `NETFrameworkSQLServer004`):

- **`maxLength` não existe no YAML.** Zero ocorrências em cada `apiTeste.yaml`. `Message` e o texto de `sdt_API_ErrorMessage` saem como `type: string`, sem comprimento. A decisão `Length = 2097152` é inconsequente para o contrato publicado pelo gerador nativo.
- **`Messages` está no schema publicado**, como `type: array` com `$ref: "#/components/schemas/sdt_API_ErrorMessage"`.
- **Runtime HTTP.** Ligado: 422 com texto da rule, acento UTF-8, truncamento visível em 2045 + `...` = 2048, `Messages[]` com um item `business_rule`. Desligado: texto genérico e fonte sem `GetMessages()`. Warning: o `Teste_BC` emite `Error()` (tipo 1) e `Msg()` (tipo 0) no mesmo `B102ERR`; o Create copia só `gxTpr_Type == 1` (`MessageTypes.Error`); o aviso não aparece no HTTP. Não afirmar `Warning = 2`.
- **Reencontro Alpha.** Cobertura parcial: Wizard na Transaction `NotaFiscal` (`apiFiscalPublica`) chegou a `teste de reencontro` e foi cancelado sem escrita; o catálogo mecânico de variantes já cobre o bloco Alpha; a regravação na `Teste` reportou `Updated=14`, `Blocked=0`. Cancelar prova abertura sem bloquear, não que regravar preserve.

---

## 6. Instrumento

A sonda está preservada, **não compilada**, em `Tools/Probes/B102ErrorResponseProbe.cs`. O
`.csproj` de `Src/Extension` não alcança `Tools/`, então a classe não entra na DLL publicada e não
pode ser invocada em runtime. O cabeçalho do arquivo explica como reativá-la deliberadamente.

---

## 7. Resultado bruto

`Temp/b102-probe-result.json` no momento da execução — `Temp/` é ignorado pelo git, e esta é a
cópia permanente:

```json
{
  "probe": "B102",
  "timestamp": "2026-08-24 13:44:19 -03:00",
  "kbModelGuid": "3936c02b-4b7a-49d4-affb-e862af6f96c0",
  "collectionExperiment": {
    "errorMessageSdt": "sdt_GOAB_B102_ErrorMessage",
    "errorResponseSdt": "sdt_GOAB_B102_ErrorResponse",
    "accepted": true,
    "stage": "completed",
    "error": null,
    "errorMessageMembers": [
      {
        "name": "Code",
        "type": "VARCHAR",
        "lengthObserved": 64,
        "decimalsObserved": 0,
        "isCollection": false,
        "collectionItemName": null
      },
      {
        "name": "Message",
        "type": "LONGVARCHAR",
        "lengthObserved": 0,
        "decimalsObserved": 0,
        "isCollection": false,
        "collectionItemName": null
      }
    ],
    "errorResponseMembers": [
      {
        "name": "Code",
        "type": "VARCHAR",
        "lengthObserved": 64,
        "decimalsObserved": 0,
        "isCollection": false,
        "collectionItemName": null
      },
      {
        "name": "Message",
        "type": "LONGVARCHAR",
        "lengthObserved": 0,
        "decimalsObserved": 0,
        "isCollection": false,
        "collectionItemName": null
      },
      {
        "name": "Messages",
        "type": "GX_SDT",
        "lengthObserved": 4,
        "decimalsObserved": 0,
        "isCollection": true,
        "collectionItemName": "sdt_GOAB_B102_ErrorMessage"
      }
    ]
  },
  "longVarCharLengthExperiments": [
    {
      "sdtName": "sdt_GOAB_B102_Len_0",
      "memberName": "Message",
      "lengthRequested": 0,
      "accepted": true,
      "lengthObserved": 0,
      "typeObserved": "LONGVARCHAR",
      "error": null
    },
    {
      "sdtName": "sdt_GOAB_B102_Len_2048",
      "memberName": "Message",
      "lengthRequested": 2048,
      "accepted": true,
      "lengthObserved": 2048,
      "typeObserved": "LONGVARCHAR",
      "error": null
    },
    {
      "sdtName": "sdt_GOAB_B102_Len_1048576",
      "memberName": "Message",
      "lengthRequested": 1048576,
      "accepted": true,
      "lengthObserved": 1048576,
      "typeObserved": "LONGVARCHAR",
      "error": null
    },
    {
      "sdtName": "sdt_GOAB_B102_Len_2097152",
      "memberName": "Message",
      "lengthRequested": 2097152,
      "accepted": true,
      "lengthObserved": 2097152,
      "typeObserved": "LONGVARCHAR",
      "error": null
    }
  ],
  "notes": [
    "lengthObserved is SDTItem.Length after Save + reload by GUID; not the value passed to AddItem.",
    "collectionAccepted reflects whether ErrorResponse with Messages collection typed by separate SDT saved successfully."
  ]
}
```
