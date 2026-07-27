# B060 - Metadata persistente em File

Status em 2026-07-27: implementacao tecnica local concluida, build Release aprovado e validacao manual inicial aprovada em GeneXus 18 U15 na KB `wsEducacaoSpTeste`, Transaction `GuiaPed`.

## Objetivo

Gravar ou reencontrar o objeto `File` de metadata persistente da API gerada, usando o `ApiPlan` em memoria como fonte do snapshot inicial. Esta frente nao completa REST, codigos HTTP finais ou seguranca definitiva.

## Escopo implementado

- Novo writer `ApiPlanMetadataFileWriter` cria ou reencontra `WikiFileKBObject` pelo nome `api<NomeBase>_Metadata`.
- A descricao sentinela e `Genexus Open API Builder B060 Metadata File - Transaction=<Transaction> - Api=<ApiName>`.
- O preflight bloqueia duplicidade, colisao externa, descricao divergente, JSON invalido, schema incompativel, Transaction/API divergentes e API Object nao gerenciado por B054/B055.
- A gravacao usa JSON UTF-8 com LF final e inclui `schemaVersion`, identidade da Transaction/API/File, servicos, SDTs, Procedures, campos, filtros, required, paginacao, ordenacao, seguranca planejada, descricoes B056, classificacao B090/B091, Business Component, readiness de engine e fingerprint SHA-256.
- As propriedades de extracao conhecidas do objeto File sao mantidas em `False`: `JavaExtract`, `NetExtract`, `NetCoreExtract`, `IOSExtract`, `AndroidExtract`, `ExtractZip` e `Extract` legado quando disponivel. O nome externo exportavel e persistido no `BlobPart` pela propriedade `FileName`, que alimenta o `External File Name` read-only da IDE.
- O wizard unico ganhou aba `Metadata B060`, checkbox `GenerateMetadata` e preview de criar/reencontrar/bloquear o File antes da conclusao.
- No fluxo de escrita, B060 roda depois de SDTs, Procedures, API Object e B055 quando marcados, para persistir o snapshot do estado sincronizado.

## Descricoes especiais herdadas de B056

B060 nao reinterpreta as descricoes. O JSON e serializado por `Newtonsoft.Json`, preservando aspas, barras invertidas e caracteres Unicode como dados JSON. Quebras de linha e caracteres de controle continuam governados pelo contrato B056 antes da aplicacao no `Service Source`; B060 apenas persiste o valor final do `ApiPlan.ServiceDescriptions`.

## Validacao tecnica local

Executado em 2026-07-27:

```powershell
dotnet build Src\Extension\GenexusOpenApiBuilder.Extension.csproj -c Release
pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1
git diff --check
```

Resultado: build Release com 0 erros, checker de comandos `Status: OK` com 11 comandos, e diff sem erro de whitespace. O build emitiu warnings NU1900 porque os indices NuGet de vulnerabilidade nao ficaram acessiveis na rede local, sem impedir a compilacao.

## Validacao manual inicial

Validado manualmente em 2026-07-27 no GeneXus 18 U15, KB `wsEducacaoSpTeste`, Transaction `GuiaPed`:

- a aba `Metadata B060` apareceu no wizard unico; por seguranca, a confirmacao iniciou desmarcada e foi marcada manualmente antes de concluir;
- a primeira gravacao criou o File `apiGuiaPed_Metadata`, mas revelou bug de abertura quando `External File Name` ficava vazio;
- a correcao passou a persistir o nome externo no `BlobPart.FileName`; a reexecucao do wizard abriu o File sem erro na IDE e permitiu exportar `D:\Temp\apiGuiaPed_Metadata.json`;
- o JSON exportado parseou com `schemaVersion='GOAB_API_METADATA_B060_V1'`, Transaction `GuiaPed`, API `apiGuiaPed`, 4 servicos, 4 Procedures, 2 SDTs compartilhados, 21 campos de Create, 21 de Update, 53 de Response, 2 filtros List e 42 entradas de obrigatoriedade;
- o JSON exportado preservou descricoes B056 para List/Get/Create/Update e declarou `scope.doesNotCompleteRest=true`.

Hash SHA-256 do arquivo exportado `D:\Temp\apiGuiaPed_Metadata.json`: `D2F16C9CCB66694911AE4EB31F8399627AC2562F40C067CC7828B869C809081E`.
Fingerprint interno `metadataWithoutFingerprint`: `21D835C9A8DD8A3AC183723E7390DC4C7BF2BB6F50D6B186F63BE86BF5CB9B4E`.

Validacoes complementares ainda recomendadas antes de encerrar B060 como frente concluida: bloqueio de colisao externa, bloqueio de JSON invalido ou identidade divergente, e caso negativo real de descricoes com aspas, barra invertida e caracteres incomuns.
