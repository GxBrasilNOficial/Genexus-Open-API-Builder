# B005 — Ciclo de vida de Procedure, SDT, Folder e File

## Estado

B005 concluído no U15 contra a KB de teste `wsEducacaoSpTeste`. O ciclo de vida de `Procedure`, `SDT`, `Folder` e `File` foi comprovado por APIs públicas com criação, alteração, releitura e exclusão confirmada, sempre com autorização explícita antes das fases de escrita.

## APIs públicas confirmadas

- `Procedure(KBModel)`, `Procedure.GetAll(KBModel)`, `Save()` e `Delete()`;
- `SDT(KBModel)`, `SDT.GetAll(KBModel)`, `Save()` e `Delete()`;
- `Folder(KBModel, string)`, `Folder.GetAll(KBModel)`, `Save()` e `Delete()`;
- objeto File: `WikiFileKBObject(KBModel)`, `WikiFileKBObject.GetAll(KBModel)`, `BlobPart.Data`, `Save()` e `Delete()`;
- conteúdo do File: `BinaryStream.FromBytes(byte[])`.

As quatro famílias são localizadas por enumeração e comparação de nome. A sonda usa nomes e sentinelas exclusivos, releitura após cada fase e confirmação de ausência após exclusão.

## Sonda histórica

Arquivo de sonda histórica: `Src/Extension/Diagnostics/B005LifecycleProbe.cs`.

Durante a validação, foram usados comandos manuais temporários:

- `B005PreflightProcedureSdtFolderFile` — somente leitura;
- `B005CreateProcedureSdtFolderFile` — escrita de criação;
- `B005UpdateProcedureSdtFolderFile` — escrita de alteração;
- `B005ReadProcedureSdtFolderFile` — somente leitura;
- `B005DeleteProcedureSdtFolderFile` — escrita de exclusão; também aceita limpar subconjunto parcial B005, desde que cada objeto encontrado tenha descrição sentinela B005.

Após a validação, esses comandos foram removidos do runtime. Na época, a IDE não exibiu o popup quando o submenu ficou vazio; por isso, o manifesto preservou temporariamente o popup **Genexus Open API Builder** com um comando placeholder não operacional chamado **Futura Primeira Opção**. Esse placeholder foi removido posteriormente quando o menu passou a ter comandos permanentes do wizard.

### Checklist para comandos temporários

Para qualquer sonda posterior:

1. incluir ou remover o `AddCommand(new CommandKey(...))` em `Src/Extension/Package.cs`;
2. incluir ou remover o `CommandDefinition` correspondente no manifesto;
3. incluir ou remover o `Command refid` correspondente no grupo de comandos em `Groups` que o submenu referencia;
4. manter no menu somente comandos operacionais vigentes;
5. executar `pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1` antes do build e novamente no fechamento passivo.

Os IDs devem coincidir exatamente nas três camadas. Build bem-sucedido, isoladamente, não comprova que o comando aparecerá na IDE.

## Correção durante a validação

A primeira execução de criação falhou com `Validation of Structured Data Type 'sdtGxOpenApiB005Probe' failed`, porque o SDT temporário estava vazio. A sonda foi corrigida para criar o membro mínimo `ProbeValue` do tipo `VarChar(128)`.

A tentativa inicial deixou objetos parciais (`Procedure=1`, `Folder=1`, `SDT=0`, `File=0`). Com autorização explícita do usuário, `B005DeleteProcedureSdtFolderFile` removeu somente esses objetos B005 com sentinela e confirmou a ausência antes da nova tentativa.

## Build de validação

Com a sonda B005 ativa, o build foi validado com sucesso:

```powershell
dotnet build Src\Extension\GenexusOpenApiBuilder.Extension.csproj --configuration Release --no-restore
```

Resultado: compilação com sucesso, 0 avisos e 0 erros.

## Fechamento passivo

Após a validação funcional, os comandos experimentais B005 foram removidos do runtime e a extensão foi recompilada sem comandos de escrita B005 ativos. A DLL passiva final foi instalada manualmente pelo usuário com `Install-ExtensionForGeneXus18.bat`.

A verificação local confirmou:

- build final Release com 0 avisos e 0 erros;
- DLL instalada coincide com a build (`InstalledMatchesBuild=True`);
- SHA-256 final da DLL: `A94A3420EE0BB694E2B6480159F3CBCBE40443E55B86959E2D28A2759E757246`;
- menu de contexto exibido como `Genexus Open API Builder > Futura Primeira Opção`;
- placeholder `Futura Primeira Opção` é não operacional e não lê nem escreve na KB.

## Evidência capturada

1. `B005PreflightProcedureSdtFolderFile` confirmou nomes disponíveis, sem alteração.
2. A primeira tentativa de criação falhou na validação do SDT vazio.
3. Após correção, novo preflight identificou sobra parcial: `Procedure=1`, `SDT=0`, `Folder=1`, `File=0`.
4. Com autorização de exclusão, a sonda removeu os objetos parciais e confirmou ausência.
5. Novo preflight confirmou todos os nomes disponíveis.
6. Com autorização de criação, a sonda criou e releu os quatro objetos: `Procedure='7a92c5f5-651d-46fb-91ee-44768733a50e'`, `SDT='c0198c47-8e63-487e-9d9d-9c2f672a31b6'`, `Folder='0c7e8ad3-2d4f-40b4-81c5-fd3c50ca40a1'`, `File='0d77352c-0fcb-4f9c-b6a0-e71aab9f015a'`.
7. Com autorização de alteração, a sonda alterou e releu os mesmos quatro GUIDs.
8. A leitura sem escrita confirmou os quatro objetos no estado alterado.
9. Com autorização de exclusão, a sonda excluiu os quatro objetos e confirmou ausência pelos mesmos GUIDs.

## Resultado

B005 está concluído. Nenhum objeto B005 permaneceu na KB ao final do teste. A sonda permanece no código apenas como evidência técnica não invocada pelo runtime.
