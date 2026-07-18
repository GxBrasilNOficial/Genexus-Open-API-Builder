# B001 — Detecção da Knowledge Base Ativa

## Estado

Concluído no GeneXus 18 Upgrade 15: a extensão identificou uma KB de teste existente, usando o evento público de abertura e sem operações de escrita.

## Objetivo

Comprovar que a extensão identifica a Knowledge Base aberta na IDE GeneXus usando somente APIs públicas, sem criar, abrir, salvar, fechar ou alterar objetos GeneXus.

## Contrato oficial usado

A extensão consulta os serviços de interface do SDK:

```csharp
public override void OnAfterOpenKB(object sender, KBEventArgs e)
{
    var knowledgeBase = e.KB;
}
```

- `OnAfterOpenKB` é o evento público chamado ao concluir a abertura de uma KB;
- `e.KB` entrega a Knowledge Base recém-aberta no momento correto do ciclo;
- a sonda lê somente `Name`, `Guid` e `Location` da KB recebida.

As referências vêm dos pacotes oficiais de SDK restaurados pelo projeto, versão `18.13.2`:

- `Artech.Architecture.UI.Framework.Sdk`;
- `Artech.Architecture.Common.Sdk`.

## Implementação

`Src/Extension/Diagnostics/ActiveKnowledgeBaseProbe.cs` isola a leitura. O método `TryRead` recebe a KB fornecida pelo evento e retorna `null` apenas se ela não estiver disponível; caso contrário, mantém os dados observados apenas em memória.

Quando uma KB termina de abrir, `Package.OnAfterOpenKB` usa o `IOutputService` oficial para acrescentar uma linha e exibir a janela Output da IDE. Não há chamadas de criação, abertura, persistência, alteração ou fechamento de KB e objetos.

## Roteiro de validação manual

1. Compilar a solução em Release.
2. Instalar manualmente a DLL Release conforme `Docs/Implementation/B000-CARREGAMENTO-IDE.md`.
3. Iniciar o GeneXus 18 U15, com a extensão marcada no Extensions Manager.
4. Abrir uma KB de teste já existente. Não criar nem converter KB para este teste.
5. Quando a KB terminar de abrir, verificar a janela Output exibida pela IDE e registrar a linha com `Name`, `Guid` e `Location`.
6. Confirmar que nenhuma janela de criação, salvamento ou alteração de objeto foi acionada.

## Evidência de compilação

- `dotnet build Src\GenexusOpenApiBuilder.sln --configuration Release --no-restore` concluído em 2026-07-18, com 0 avisos e 0 erros.

## Evidência do teste manual

- GeneXus 18 Upgrade 15, com a extensão marcada no Extensions Manager;
- KB de teste aberta: `wsEducacaoSpTeste`;
- saída capturada: [Genexus Open API Builder][B001] Knowledge Base ativa detectada: Name='wsEducacaoSpTeste', Guid='39e12e41-51a7-466f-a448-dbc3a05f17c7', Location='C:\KBs\wsEducacaoSpTeste'.
- nenhuma operação de criação, abertura, salvamento, fechamento ou alteração de objeto foi acionada pela extensão.

## Critério de conclusão

Critério atendido em 2026-07-18: a KB ativa foi observada em uma sessão existente da IDE e a evidência está registrada neste documento e no checkpoint operacional.
