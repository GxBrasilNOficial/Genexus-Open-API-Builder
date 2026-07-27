# B060 - Metadata persistente em File

Status em 2026-07-27: frente concluida, com implementacao tecnica local, build Release e validacoes manuais aprovadas em GeneXus 18 U15 na KB `wsEducacaoSpTeste`, Transactions `GuiaPed` e `Contrato`.

## Objetivo

Gravar ou reencontrar o objeto `File` de metadata persistente da API gerada, usando o `ApiPlan` em memoria como fonte do snapshot inicial. Esta frente nao completa REST, codigos HTTP finais ou seguranca definitiva.

## Escopo implementado

- Novo writer `ApiPlanMetadataFileWriter` cria ou reencontra `WikiFileKBObject` pelo nome `api<NomeBase>_Metadata`.
- A descricao sentinela e `Genexus Open API Builder B060 Metadata File - Transaction=<Transaction> - Api=<ApiName>`.
- O preflight bloqueia duplicidade, colisao externa, descricao divergente, JSON invalido, schema incompativel, Transaction/API divergentes e API Object nao gerenciado por B054/B055.
- A gravacao usa JSON UTF-8 com LF final e inclui `schemaVersion`, identidade da Transaction/API/File, servicos, SDTs, Procedures, campos, filtros, required, paginacao, ordenacao, seguranca planejada, descricoes B056, classificacao B090/B091, Business Component, readiness de engine e fingerprint SHA-256.
- As propriedades de extracao conhecidas do objeto File sao mantidas em `False`: `JavaExtract`, `NetExtract`, `NetCoreExtract`, `IOSExtract`, `AndroidExtract`, `ExtractZip` e `Extract` legado quando disponivel. O nome externo exportavel e persistido no `BlobPart` pela propriedade `FileName`, que alimenta o `External File Name` read-only da IDE.
- O wizard unico ganhou aba `Metadata B060`, checkbox `GenerateMetadata` e preview de criar/reencontrar/bloquear o File antes da conclusao. O preview de reencontro tambem valida o JSON persistido e classifica como bloqueado quando o conteudo esta vazio, invalido, com schema incompativel ou identidade divergente.
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

Validacoes negativas complementares aprovadas manualmente no mesmo ambiente:

- upload de substituto com JSON sintaticamente invalido no File `apiGuiaPed_Metadata`: o wizard exibiu `Estado atual da KB: Bloqueado`, desabilitou a confirmacao e registrou que nenhuma escrita seria permitida;
- upload de substituto com `ownership.apiName` divergente no mesmo File: o wizard exibiu o mesmo bloqueio conservador antes da escrita;
- restauracao por upload do JSON valido exportado em `D:\Temp\apiGuiaPed_Metadata.json`: o wizard voltou para `Estado atual da KB: Reencontrar e validar`, com confirmacao disponivel e ainda desmarcada por seguranca;
- reexecucao final, feita logo depois da restauracao, com `GenerateMetadata=True` reencontrou e validou o File existente, registrando na Output `Status='Reencountered'`, `Guid='1b943d80-5961-4b25-a89a-2454dae9f45d'`, `Bytes=99432` e `Sha256='C518A9EEFF4F9785E5B63D27F92D1E451D2E70B8E0FCE5BE8F98472D6095DF54'`;
- `Build All` executado em seguida passou com sucesso; nao havia objetos para especificar, e o warning `FBiTextSharp.dll` repetiu o comportamento ambiental ja classificado como nao relacionado ao B060.

Validacoes finais aprovadas manualmente em 2026-07-27 na Transaction `Contrato`:

- colisao externa: um File manual `apiContrato_Metadata`, carregado com JSON externo de teste e sem a descricao sentinela B060, fez a aba `Metadata B060` exibir `Estado atual da KB: Bloqueado` e `Bloqueado: 1 colisao(oes) externa(s), incompativel(is) ou ambigua(s) detectada(s). Nenhuma escrita sera permitida.`; a confirmacao ficou desabilitada e `Confirmado para escrita: False`;
- descricoes especiais B056/B060: a descricao da Transaction foi ajustada para `Contrato "Especial" \ Ação Ç`; o wizard gerou SDTs, Procedures, API Object, aplicou B055/B056 e gravou `apiContrato_Metadata` com `Status='Created'`, `Guid='b901d9e0-b213-4369-be79-18db0129cb82'`, `Bytes=12616` e `Sha256='3E7D0E5EF54B171D348030E6D46A08ED5DC8EA9F7E394B933E41D42975752D39'`;
- o JSON exportado localmente a partir do File `apiContrato_Metadata` parseou com `schemaVersion='GOAB_API_METADATA_B060_V1'`, Transaction `Contrato`, API `apiContrato`, 4 servicos, 4 Procedures, 2 SDTs compartilhados, 1 campo de Create, 1 de Update, 2 de Response, 1 filtro List e 2 entradas de obrigatoriedade;
- as descricoes especiais foram preservadas em `services[*].description` e `descriptions.services[*].description`, incluindo aspas, barra invertida, acentuacao e `Ç` como dados JSON;
- `Build All` passou especificando e gerando `apiContrato`, `Contrato`, `procContrato_API_Create`, `procContrato_API_List`, `procContrato_API_Update`, `procContrato_API_Get` e objetos relacionados; o warning `FBiTextSharp.dll` repetiu o comportamento ambiental ja classificado como nao relacionado ao B060.

Hash SHA-256 do arquivo exportado `D:\Temp\apiGuiaPed_Metadata.json`: `D2F16C9CCB66694911AE4EB31F8399627AC2562F40C067CC7828B869C809081E`.
Fingerprint interno `metadataWithoutFingerprint`: `21D835C9A8DD8A3AC183723E7390DC4C7BF2BB6F50D6B186F63BE86BF5CB9B4E`.
Hash SHA-256 do JSON exportado localmente a partir do File `apiContrato_Metadata`: `3E7D0E5EF54B171D348030E6D46A08ED5DC8EA9F7E394B933E41D42975752D39`.

B060 esta concluida no escopo canonico atual. Permanecem fora desta frente: REST completo, codigos HTTP finais, seguranca definitiva e metadata persistente alem do snapshot inicial.
