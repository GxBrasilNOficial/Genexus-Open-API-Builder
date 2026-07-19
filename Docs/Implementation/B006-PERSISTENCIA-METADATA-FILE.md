# B006 — Persistência de metadata em File após reabrir a KB

## Estado

Validação funcional concluída no GeneXus 18 Upgrade 15 contra a KB de teste `wsEducacaoSpTeste`. Um objeto `File` JSON temporário foi criado, relido antes do fechamento, relido após fechar e reabrir a KB e excluído ao final com ausência confirmada.

O fechamento passivo foi concluído: o runtime foi recompilado sem os comandos B006, reinstalado manualmente e conferido por leitura contra a build.

## Objetivo

Comprovar, por APIs públicas, que metadata JSON armazenada em um objeto `File` permanece íntegra e reencontrável após fechar e reabrir a KB.

## APIs públicas reutilizadas

- `WikiFileKBObject(KBModel)`;
- `WikiFileKBObject.GetAll(KBModel)`;
- `BlobPart.Data`;
- `BinaryStream.FromBytes(byte[])`;
- `Save()` e `Delete()`.

## Sonda histórica

Arquivo: `Src/Extension/Diagnostics/MetadataFilePersistenceProbe.cs`.

A sonda usa um único objeto temporário controlado:

- Name: `fileGxOpenApiB006MetadataProbe.json`;
- Description: `Gx Open API Builder B006 Metadata File Probe`;
- conteúdo: JSON UTF-8 determinístico, incluindo caracteres Unicode;
- tamanho esperado: `316` bytes;
- SHA-256 esperado: `69E48FA5AFD2E660C6C9FFCB85A3015CF4D5FF644A5FB2CF4668BBA9A1409F59`.

## Classificação das ações

Somente leitura:

- `B006PreflightMetadataFile`;
- `B006ReadMetadataFile`;
- `B006ReadAfterReopenMetadataFile`.

Escrita:

- `B006CreateMetadataFile` — cria exclusivamente o File temporário B006;
- `B006DeleteMetadataFile` — exclui exclusivamente o File temporário validado pela sentinela B006.

Nenhuma ação era automática. O usuário executou manualmente cada comando de escrita após o agente delimitar a operação e solicitar autorização.

## Evidência capturada

1. O preflight confirmou o nome disponível sem alterar a KB.
2. A criação salvou e releu imediatamente o File:
   - Guid: `4c0b88f9-ee42-437c-b650-f4f2818e8317`;
   - Bytes: `316`;
   - SHA-256: `69E48FA5AFD2E660C6C9FFCB85A3015CF4D5FF644A5FB2CF4668BBA9A1409F59`.
3. A releitura anterior ao fechamento confirmou o mesmo GUID, nome, descrição, tamanho, bytes e hash.
4. A KB `wsEducacaoSpTeste` foi fechada completamente e reaberta.
5. A releitura posterior à reabertura confirmou novamente os mesmos valores.
6. A exclusão removeu o File de GUID `4c0b88f9-ee42-437c-b650-f4f2818e8317` e confirmou sua ausência.

## Resultado funcional

A metadata persistiu integralmente após fechar e reabrir a KB. Foram preservados:

- identidade do objeto pelo GUID;
- nome e capitalização;
- descrição;
- sequência exata dos bytes UTF-8;
- texto JSON;
- tamanho;
- SHA-256.

Nenhum objeto B006 permaneceu na KB ao final.

## Fechamento do runtime

Os cinco comandos experimentais B006 foram removidos de `Package.cs` e do manifesto. O submenu preserva somente o placeholder não operacional `Futura Primeira Opção`.

A sonda permanece no código apenas como evidência técnica sem invocação pelo runtime.

## Build passiva

Validações locais:

- registro sincronizado com exatamente um comando: `Futura Primeira Opção`;
- busca sem referências B006 em `Package.cs` e no manifesto;
- build Release com 0 avisos e 0 erros;
- DLL instalada coincide com a build (`InstalledMatchesBuild=True`);
- SHA-256 da DLL passiva instalada: `B7212897E8143758966D7DD03C93D2DACC2F12D60A9B358D6C970D8234D0E4EE`;
- submenu confirmado apenas com `Futura Primeira Opção`, sem comandos ou execução automática B006.
