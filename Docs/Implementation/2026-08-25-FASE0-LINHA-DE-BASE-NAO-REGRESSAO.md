# Fase 0 — Linha de base de não regressão (transações planas)

Data: 2026-08-25.
Frente: Sprint 9 — somente Fase 0.
Escopo: duas camadas (offline + IDE). Não inclui B095 nem Fases 1–7.

## Camada offline (checker mecânico)

### O que foi entregue

- Facade `ApiPlanGenerationBaseline` em `Src/Extension/Diagnostics/ApiPlanGenerationBaseline.cs`.
- Fixtures sintéticas de nível único:
  - `FlatSimpleKey` — chave simples
  - `FlatCompositeKey` — chave composta
  - `FlatNoAccept` — atributo somente no Response (simula exclusão de request por `NoAccept`)
- Arquivos de referência em `Tests/GenerationBaseline/Baselines/<fixture>/`:
  - `Create.source.txt`, `Update.source.txt`, `Get.source.txt`, `List.source.txt`
  - `ApiObject.serviceSource.txt`
  - `SdtPlan.json`
- Teste `Tests/GenerationBaseline/Test-ApiPlanGenerationBaseline.ps1` ligado a
  `scripts/Invoke-PrePushMechanicalChecks.ps1` como `tests.generationBaseline`.
- A linha de base reflete o gerador **atual** (pós-B102): Create/Update com
  `GetMessages()`, `Messages[]` e truncamento de `Message`.

### Como rodar

```powershell
pwsh -NoProfile -File Tests/GenerationBaseline/Test-ApiPlanGenerationBaseline.ps1
```

Requisitos: DLL Release em `Src/Extension/bin/Release/net471/` e instalação GeneXus
legível em `C:\Program Files (x86)\GeneXus\GeneXus18` (somente leitura). Ausência → `exit 2`.

### Recaptura

Só em commit isolado, cujo diff contenha exclusivamente os arquivos de referência e a
justificativa escrita. Não recapturar no mesmo commit que altera o emissor.

```powershell
pwsh -NoProfile -File Tests/GenerationBaseline/Test-ApiPlanGenerationBaseline.ps1 -UpdateBaselines
```

## Camada IDE (manual, pontual)

### Objetivo

Export XPZ dos SDTs gerados de **uma** Transaction plana, para cobrir ordem de itens e
propriedades físicas do SDT que a camada offline não enxerga.

### Instruções de captura (início da sprint)

1. Abrir a KB de teste (`wsEducacaoSpTeste` ou equivalente). Não usar KB de cliente.
2. Escolher uma Transaction plana já gerada pela extensão (sem subníveis), com SDTs próprios
   presentes (CreateRequest, UpdateRequest, Response, ListFilters, ListResponse) e, se útil,
   os compartilhados `sdt_API_ErrorMessage`, `sdt_API_ErrorResponse`, `sdt_API_Pagination`.
3. Na IDE GeneXus, exportar os SDTs selecionados para XPZ (Export).
4. Copiar o arquivo para `Tests/GenerationBaseline/IdeXpz/` (pasta local; conteúdo XPZ não
   versionado por padrão — ver `.gitignore` da pasta).
5. Anotar nesta evidência: nome da Transaction, data/hora, ambiente (U15), e caminho local do XPZ.

### Estado da captura de início

**Registrada em 2026-08-25** a partir da pasta paralela da KB de teste
`C:\Dev\Prod\Gx_wsEducacaoSpTeste\ObjetosDaKbEmXml\SDT` (Transaction plana `Teste` +
SDTs compartilhados). Manifesto versionado com SHA-256:
`Tests/GenerationBaseline/IdeXpz/CAPTURE-INICIO.md`. Os XMLs ficam locais (gitignored).

**Correção de leitura:** esses SDTs **não** foram gerados nem regravados na IDE em
2026-08-25; a DLL da sessão Fase 0 **não** foi instalada nesse dia. Os timestamps de
hoje na pasta paralela são de **rematerialização** do XPZ já existente. A captura IDE
fixa a forma física dos objetos **já presentes** na KB (gerações anteriores). A
camada offline, por outro lado, reflete o emissor do código atual (pós-B102).

A conferência de **fim** da sprint permanece para o fechamento da Sprint 9 — não ocorre agora.

### Conferência de fim de sprint (não agora)

Repetir o export da mesma Transaction (ou da mesma seleção de SDTs) e comparar ordem de
itens e propriedades relevantes com o XPZ de início. Divergência inesperada bloqueia o
fechamento da sprint até justificativa ou correção.

## Fora de escopo desta entrega

- B095 e Fases 1–7
- B100 / B105
- Alteração deliberada de emissores além da exposição necessária à linha de base
- Versionamento de XML/XPZ de KB de cliente
