# Captura IDE — fim da Sprint 9 (Fase 0)

Data/hora local do registro neste repositório: 2026-08-28 ~23:25.
KB de teste: `wsEducacaoSpTeste` (pasta paralela `C:\Dev\Prod\Gx_wsEducacaoSpTeste`).
Âncora de início: `CAPTURE-INICIO.md` (2026-08-25).

## Rematerialização

- XPZ de entrada (renomeado após sync): `XpzExportadosPelaIDE\processado_wsEducacaoSpTeste_full_20260828a.xpz` (origem `wsEducacaoSpTeste_full_20260828a.xpz`).
- XMLs em `ObjetosDaKbEmXml\SDT` com `LastWriteTime` 2026-08-28 ~23:21.

## Conferência vs âncora de início (8 arquivos)

| Arquivo | Bytes início | Bytes fim | SHA-256 fim | vs início |
|---|---:|---:|---|---|
| sdtTeste_API_CreateRequest.xml | 4662 | 5837 | `7ABA557360947592DDE5B9E9BBB997C49F1D219C037BDBA3D9F044756F8A12C3` | **DIFF esperado** (hierarquia: coleções `TesteItem`/`TestePortfolio`; `TesteItemObs2` ausente no Create após smoke Sync) |
| sdtTeste_API_UpdateRequest.xml | 3525 | 5973 | `DFEEEAB6B10FB0E4AA3302314292D41CF23EE8C53CC76A4DC919A1A80A49B643` | **DIFF esperado** (hierarquia) |
| sdtTeste_API_Response.xml | 4616 | 5771 | `6E452F429DA13E40A9EBF3B4EDCC11774838C4E90F172D02B32799B7226600DE` | **DIFF esperado** (hierarquia) |
| sdtTeste_API_ListFilters.xml | 5047 | 5047 | `592ECC6027FFD553B0E87764611E46DC357020693C3AEBA12F7A74006B190EC5` | **DIFF** (mesmo tamanho; rematerialização / serialização) |
| sdtTeste_API_ListResponse.xml | 3082 | 3100 | `62BB5B5A8107081305C67B4A8DF288D0DF579286C1AD5CD98CE4C24ABBA69C1B` | **DIFF esperado** (`ListResponse_Item` / tipagem hierárquica) |
| sdt_API_ErrorMessage.xml | 2781 | 2781 | `144FAC1EE8EAE7F50306A3F5DF19D61345CC307A9FC8266561E85BA1B98E6FF5` | **DIFF hash, mesmo tamanho**; estrutura estável (`items=2`) — ruído típico de rematerialização XPZ, não mudança de contrato |
| sdt_API_ErrorResponse.xml | 3371 | 3371 | `11C65F5C1E995F837EABC97107670701A3C89F5D34F6804FA418EDA8C5098968` | **DIFF hash, mesmo tamanho**; `items=3` estável |
| sdt_API_Pagination.xml | 4012 | 4012 | `72211932AA069C4EA1B827DA290C9FFA60F23244F07018F7A55CA474F1471EB7` | **DIFF hash, mesmo tamanho**; `items=4` estável |

## SDTs hierárquicos presentes no fim (ausentes na âncora flat)

Incluem, entre outros: `sdtTeste_API_*_TesteItem`, `*_TesteItemFolio`, `*_TesteItemFolioDoc`, `*_TestePortfolio`, `sdtTeste_API_ListResponse_Item`. Confirmam que a Transaction `Teste` deixou de ser a âncora plana do início.

## Interpretação

1. A pasta paralela **não** estava mais estagnada: sync de 2026-08-28 refletiu a KB nativa.
2. Divergência dos SDTs próprios flat de `Teste` é **esperada** (Sprint 9 hierárquica + omissão de `TesteItemObs2` no Create/Update).
3. Compartilhados: **sem indício de regressão estrutural** (tamanho e contagem de itens iguais ao início); o SHA-256 mudou por serialização na rematerialização — a camada IDE não usa igualdade byte a byte como a offline.
4. A proteção contra regressão silenciosa do **emissor** continua na camada offline (`tests.generationBaseline`).

## Status

- Rematerialização IDE: **feita**.
- Conferência de fim: **fechada** com a interpretação acima.
