# B020 — Detecção da Knowledge Base Ativa no Protótipo

## Estado

Concluído no GeneXus 18 Upgrade 15: a extensão detectou manualmente a KB ativa no fluxo do protótipo, exibindo nome, GUID e localização na janela Output sem persistência e sem operações de escrita.

## Objetivo

Consolidar a detecção da Knowledge Base ativa como primeira capacidade manual e somente leitura do protótipo navegável do wizard.

Esta frente reutiliza a evidência pública comprovada no B001, mas não reativa o gatilho automático `OnAfterOpenKB`.

## Contrato aplicado

- o comando é acionado manualmente pelo menu `Genexus Open API Builder > Detectar KB Ativa (B020)`;
- a leitura usa API pública da IDE para obter a KB atualmente disponível;
- `ActiveKnowledgeBaseProbe.TryRead` continua isolando a leitura de `Name`, `Guid` e `Location`;
- o resultado é apresentado somente na janela Output;
- a escrita no Output usa `IOutputService2.DefaultOutputId`, o mesmo padrão validado no B001;
- nenhuma escolha é persistida;
- nenhum objeto GeneXus é criado, alterado ou excluído.

## Implementação

`Src/Extension/Package.cs` registra um comando manual de protótipo para B020 e mantém o placeholder `Futura Primeira Opção`. O comando permanece no runtime durante a Sprint 2 enquanto serve como entrada navegável somente leitura; deverá ser removido ou substituído quando o fluxo consolidado do wizard absorver essa etapa.

O manifesto `Src/Extension/GenexusOpenApiBuilder.package` registra o mesmo ID nas duas camadas XML exigidas: `CommandDefinition` e `Command refid` dentro do grupo usado pelo submenu.

Após o primeiro teste manual, o comando aparecia no menu, mas não emitia linha visível na Output. A implementação foi ajustada para reutilizar o Output padrão da IDE, em vez de tentar escrever em um Output customizado ainda não selecionado/visível.

## Roteiro de validação executado

1. Compilar a solução em Release.
2. Fechar completamente a IDE GeneXus.
3. Executar `Install-ExtensionForGeneXus18.bat` como Administrador.
4. Executar `Register-ExtensionForGeneXus18.bat` normalmente, sem Administrador.
5. No prompt aberto pelo segundo arquivo, digitar `genexus /install`, conferir a varredura e depois digitar `exit`.
6. Abrir novamente a IDE GeneXus com uma KB de teste.
7. Acionar `Genexus Open API Builder > Detectar KB Ativa (B020)`.
8. Confirmar na Output a linha `[Genexus Open API Builder][B020]` com `Name`, `Guid` e `Location`.
9. Confirmar que nenhuma criação, alteração ou exclusão de objeto ocorreu.
10. Executar `pwsh -NoProfile -File Tools/Test-InstalledExtension.ps1` quando a DLL instalada precisar ser comparada com a build.

## Evidência do teste manual

- GeneXus 18 Upgrade 15, com a extensão reinstalada e marcada no Extensions Manager;
- KB de teste aberta: wsEducacaoSpTeste;
- saída capturada: [Genexus Open API Builder][B020] Knowledge Base ativa detectada: Name='wsEducacaoSpTeste', Guid='39e12e41-51a7-466f-a448-dbc3a05f17c7', Location='C:\KBs\wsEducacaoSpTeste'.
- nenhuma criação, alteração ou exclusão de objeto GeneXus foi relatada durante o acionamento manual.

## Critério de conclusão

Critério atendido em 2026-07-19: a KB ativa foi detectada por comando manual no fluxo do protótipo, com evidência na Output e sem persistência ou escrita na KB.
