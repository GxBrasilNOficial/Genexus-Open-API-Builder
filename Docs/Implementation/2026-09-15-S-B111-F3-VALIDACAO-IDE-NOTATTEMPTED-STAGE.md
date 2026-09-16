# S-B111 · F3 — validação na IDE: stage de NotAttempted (TargetAbsentBeforeDelete)

**Data:** 2026-09-15.
**Motivo:** em `e20e0aa`, `NotAttempted` passou a enviar o role canônico no parâmetro que
`RecordNotAttempted` trata como `receipts[].stage`. Alvo ausente saía no relatório como
`[Get/Procedure]` ou `[MainApi/API]`, enquanto o `Persist` irmão mantinha
`Procedures`/`ApiObject`/`OwnSdts`/`Metadata`. A correção separou `stage` e `role`.

**Ambiente:** GeneXus 18 U15 · KB `wsEducacaoSpTeste` · Transaction `Teste` · DLL Release
corrente instalada com `Install-ExtensionForGeneXus18.bat` (sem `genexus /install`).

## Setup

1. API íntegra (`apiTeste`, Procedures, SDTs, `apiTeste_Metadata`).
2. API Object `apiTeste` apagado à mão na KB Explorer; metadata e demais objetos
   preservados.

## Ação

Menu de contexto da `Teste` → `Remover API gerada` → `Sim`.

## Resultado

| Medida | Valor |
|---|---|
| Estado | `Partial` / `RemovalPartial` |
| Motivo | `TargetAbsentBeforeDelete` |
| Fila | `Passadas=1/25`, `Removidos=0`, `Pendentes=25`, `Bloqueado='ApiObject:apiTeste'` |
| Diário | `FileId=88`, `Checkpoints=3`, `TotalMs=156`, `Recibos=1`, `Durability=Confirmed` |
| Relatório | `Interrupted`, `Removidos=0`, `Bloqueados=1`, `DuraçãoMs=464` |

Diagnóstico de persistência (ponto da correção):

```text
[ApiObject/API] apiTeste: Outcome=OutcomeUnknown; Confirmação=NotAttempted; API Object ausente antes do Delete.
```

Stage = `ApiObject`, não `MainApi`. Com o bug, o prefixo seria `[MainApi/API]`.

## Alcance

Provado o caminho `NotAttempted` do API Object com a DLL que separa stage e role. Os call
sites de Procedure/SDT/Metadata seguem o mesmo contrato e o gate de resiliência trava as
quatro assinaturas offline.
