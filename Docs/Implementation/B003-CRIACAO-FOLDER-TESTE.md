# B003 — Criação Controlada de Folder de Teste

## Estado

Concluído no GeneXus 18 Upgrade 15: a extensão criou um Folder de teste no Root Module da KB autorizada, sem modificar objetos preexistentes.

## Escrita autorizada

- KB de teste: `wsEducacaoSpTeste`;
- objeto: Folder `GxOpenApi_B003_Probe` no Root Module;
- efeito esperado: criar o Folder apenas se ele ainda não existir;
- proteção: nenhum objeto existente é modificado ou substituído.

## Contrato oficial usado

```csharp
var folder = new Folder(knowledgeBase.DesignModel, folderName);
folder.Save();
```

Antes da criação, a sonda percorre `Folder.GetAll(knowledgeBase.DesignModel)` e compara os nomes em memória. Caso o Folder exista, ela apenas informa o fato na janela Output.

## Roteiro de validação manual

1. Instalar manualmente a DLL Release atualizada.
2. Iniciar o GeneXus 18 U15 com a extensão marcada.
3. Abrir a KB de teste `wsEducacaoSpTeste`.
4. Confirmar na janela Output a linha iniciada por `[Genexus Open API Builder][B003] Folder de teste criado:`.
5. Confirmar no Root Module a presença do Folder `GxOpenApi_B003_Probe`.
6. Confirmar que nenhum objeto preexistente foi alterado.

## Evidência de compilação

- `dotnet build Src\GenexusOpenApiBuilder.sln --configuration Release --no-restore` concluído em 2026-07-18, com 0 avisos e 0 erros.

## Evidência do teste manual

- GeneXus 18 Upgrade 15, com a extensão marcada no Extensions Manager;
- KB de teste: `wsEducacaoSpTeste`;
- saída capturada: `[Genexus Open API Builder][B003] Folder de teste criado: GxOpenApi_B003_Probe.`;
- painel Properties confirmou: nome `GxOpenApi_B003_Probe`, descrição `Gx Open Api B003 Probe`, Module/Folder `Root Module` e visibilidade pública;
- nenhum objeto preexistente foi alterado pela extensão.

## Critério de conclusão

Critério atendido em 2026-07-18: o Folder de teste foi criado e visualizado no Root Module, com a evidência registrada neste documento e no checkpoint operacional.
