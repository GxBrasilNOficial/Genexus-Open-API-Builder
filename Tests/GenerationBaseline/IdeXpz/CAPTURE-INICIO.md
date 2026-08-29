# Captura IDE — início da Sprint 9 (Fase 0)

Data/hora local do registro neste repositório: 2026-08-25 (sessão Fase 0).
KB de teste: `wsEducacaoSpTeste` (pasta paralela `C:\Dev\Prod\Gx_wsEducacaoSpTeste`).
Transaction plana: `Teste` (chave composta; um único `Level` raiz na estrutura).
Ambiente de referência: GeneXus 18 U15 (mantenedor).

## O que esta captura é — e o que não é

Esta captura registra a **forma física dos SDTs já existentes** na KB de teste no
início da frente de subníveis, a partir dos XMLs da pasta paralela.

**Não** significa que os objetos foram gerados ou regravados na IDE em 2026-08-25.
Neste dia o mantenedor **não** alterou a KB na IDE e **não** instalou a DLL
produzida nesta sessão da Fase 0. Os SDTs refletem gerações **anteriores** ainda
presentes na KB.

Os `LastWriteTime` dos XMLs em `ObjetosDaKbEmXml/SDT` e o carimbo
`last_xpz_materialization_run_at=2026-08-25T14:31:56.0000000Z` referem-se à
**rematerialização** do acervo XPZ na pasta paralela, não a uma regeneração na IDE.

A linha de base **offline** (`Tests/GenerationBaseline/Baselines/`) é independente:
ela emite Source/Service Source/plano de SDT a partir do código do gerador atual
(pós-B102). A camada IDE não prova paridade com essa DLL até haver reinstalação e
regravação explícitas na IDE.

**Decisão (2026-08-25):** para o início da Sprint 9, a captura acima **cumpre** o
objetivo da camada IDE (âncora de deriva na KB). Não se exige regenerar a API nem
instalar a DLL da sessão Fase 0 só para “atualizar” estes hashes.

## Origem dos arquivos

Copiados de:

`C:\Dev\Prod\Gx_wsEducacaoSpTeste\ObjetosDaKbEmXml\SDT`

XPZ completo contemporâneo na pasta paralela (não copiado para o repositório; também
é reexport/rematerialização do acervo, não prova de geração neste dia):

`C:\Dev\Prod\Gx_wsEducacaoSpTeste\XpzExportadosPelaIDE\wsEducacaoSpTeste_full_20260825a.xpz`

Os arquivos `.xml` desta pasta permanecem locais (`.gitignore`); este manifesto é a
evidência versionável da captura de início.

## Arquivos e SHA-256

| Arquivo | Bytes | SHA-256 |
|---|---:|---|
| sdtTeste_API_CreateRequest.xml | 4662 | F0950B27A01B3EB0D7881F25D35D15354068350FA02DD0DD92D9C1EC35B6704B |
| sdtTeste_API_UpdateRequest.xml | 3525 | CFF9D2CBE8B7F2C76C1416D03DEE822BB765AABC3E2CFF5B72B33DF72ACD01D5 |
| sdtTeste_API_Response.xml | 4616 | 94A3E9E6BB7C8AB9427E33D42ECE379172FF7D6F65BDC02634C498AA1409E480 |
| sdtTeste_API_ListFilters.xml | 5047 | 8BA2A32B885F46405C6939166D792515F305E0BE046E5AC9BE55F602DF63B173 |
| sdtTeste_API_ListResponse.xml | 3082 | 590FFA300A4DFF8D909591AAB34A9FFEB46DB9FE8A7B2C849346808CC33668C7 |
| sdt_API_ErrorMessage.xml | 2781 | D23B066B892301AD6AFD4E4BA7E80215EE3448B8A08545D3F1E525668F00D81D |
| sdt_API_ErrorResponse.xml | 3371 | 1861E67CEF94EDA4D07571705D0D94CB10DCA98D9BD82752E8C9D1602BF1E4B0 |
| sdt_API_Pagination.xml | 4012 | 8DFDA1FE9128EC96F3C2F2DC0C053C00483C8137F6299B640047A62C78C0ED6F |

## Conferência de fim de sprint

**Fechada em 2026-08-28** — ver `CAPTURE-FIM.md` (rematerialização via
`processado_wsEducacaoSpTeste_full_20260828a.xpz`).
