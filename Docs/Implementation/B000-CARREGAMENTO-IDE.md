# B000 — Carregamento da Extensão na IDE

## Estado

Concluído no GeneXus 18 Upgrade 15: a extensão mínima foi recompilada sem SourceLink, registrada e confirmada como marcada no Extensions Manager, sem abrir uma Knowledge Base. Após a atualização dos comentários de manutenção em `SdkBuildMarker.cs` e `Package.cs`, a DLL foi reinstalada e a correspondência exata foi revalidada pelo SHA-256 `2B5E4E6BD00E5CCB1372711E9CC51999D79183DE54BCB86B1D024CD30C5D66C8`. O bloqueio anterior baseado na ausência do instalador Platform SDK foi removido em 2026-07-16, pois o método oficial para GeneXus 18 Upgrade 14 ou posterior usa pacotes NuGet e MSBuild SDKs.

## Objetivo

Comprovar que o pacote mínimo da extensão carrega na IDE GeneXus 18 U14 ou posterior, usando o U15 local como primeiro ambiente de validação, sem criar ou alterar objetos em uma Knowledge Base.

## Evidência disponível

- o projeto foi migrado para `GeneXus.Package.UI.Sdk` e compilado com as referências oficiais do feed GeneXus Azure Artifacts;
- a build produziu `Src/Packages/Release/GenexusOpenApiBuilder.Extension.0.1.0-preview.1.nupkg`;
- esse pacote contém `lib/net471/GenexusOpenApiBuilder.Extension.dll`;
- o pacote incorpora `GenexusOpenApiBuilder.package` e declara o ponto de entrada `GenexusOpenApiBuilder.Extension.Package`;
- o usuário instalou manualmente a DLL compilada pelo `Tools > Extensions Manager > Add > Local`, selecionando `Src\Extension\bin\Release\net471\GenexusOpenApiBuilder.Extension.dll`;
- o scanner de pacotes do U15 local encontrou `GenexusOpenApiBuilder.Extension.dll` durante a inicialização;
- a DLL instalada antes da correção coincidiu com a build anterior pelo SHA-256 `199824A19519A5401111A34D37EE50E9EE531A57A1E7C59018DC5F728B76B4D9`;
- após reiniciar a IDE, a extensão apareceu no Extensions Manager, mas estava desmarcada. Logo, a instalação e a cópia foram demonstradas; a ativação/carregamento ainda não.
- a comparação com um pacote UI ativo do U15 identificou a diferença: a classe de entrada agora herda de `AbstractPackageUI`, em vez de `AbstractPackage`;
- a execução manual de `genexus /install` registrou a DLL como adicionada, mas recusou a carga com `Compatibility: cannot load package ... version '0', expecting version '143920'`;
- com `GenerateAssemblyInfo` habilitado, o SDK gerou `PackageCompatibility(Version = 143920)` para a DLL; com `EnableSourceLink=false`, dois rebuilds Release produziram o mesmo SHA-256. Após as alterações documentais em `SdkBuildMarker.cs` e `Package.cs`, a DLL correspondente foi reinstalada e comparada à build atual pelo SHA-256 `2B5E4E6BD00E5CCB1372711E9CC51999D79183DE54BCB86B1D024CD30C5D66C8`.
- a DLL instalada corresponde exatamente à build Release estável por SHA-256, e a confirmação visual posterior mostrou a extensão marcada no Extensions Manager com fabricante `GxBrasilNOficial` e versão `0.1.0-preview.1`.
- a DLL atual contém `AssemblyDescription`, `FileDescription`, `ProductName` e `Comments` com `Genexus Open API Builder - Preview`, mas o Extensions Manager do U15 mantém a coluna Description vazia; essa exibição não bloqueia o B000, pois a extensão está marcada, carregada e identificada por Nome, Fabricante e Versão;
- a instalação local disponível é GeneXus 18 Upgrade 15, build `18.0.15.188745`, e serve somente como ambiente de teste.

Fonte oficial: [GeneXus Platform SDK Download](https://docs.genexus.com/en/wiki?27521,GeneXus+Platform+SDK+Download).

## Regra de segurança

`AGENTS.md` proíbe o agente de criar, alterar, mover, renomear ou excluir itens em `C:\Program Files (x86)\GeneXus` e subpastas. A cópia controlada só ocorre quando o usuário executa `Install-ExtensionForGeneXus18.bat` como Administrador; o agente não executa o arquivo nem altera a instalação do GeneXus.

## Diagnóstico reproduzível

O script abaixo apenas lê a DLL compilada e a DLL instalada. Ele confere hash, tipo de entrada e manifesto incorporado; não cria ou altera arquivos na instalação do GeneXus nem abre uma Knowledge Base:

```powershell
pwsh -NoProfile -File Tools/Test-InstalledExtension.ps1
```

O campo `ActivationVerified` permanece propositalmente como `false`: a marcação do Extensions Manager é estado interno da IDE, que o script não infere.

As tentativas de capturar o texto da janela de inicialização com `Start-Process -RedirectStandardOutput/-RedirectStandardError` produziram arquivos vazios em `C:\Temp`; essa janela não usa a saída padrão do processo `GeneXus.exe`.

Para a cópia controlada, execute `Install-ExtensionForGeneXus18.bat` na raiz do repositório usando **Executar como administrador**. O arquivo delega ao script interno `Tools/Copy-ExtensionForGeneXus18.ps1`, que exige a IDE fechada, cria backup em `C:\Temp`, copia a DLL compilada para `Packages\GenexusOpenApiBuilder.Extension.dll` e valida o hash. O script PowerShell não executa registro.

O registro é uma segunda etapa, executada sem elevação por `Register-ExtensionForGeneXus18.bat`. Esse arquivo abre um prompt normal na pasta de instalação do GeneXus, no qual o usuário digita `genexus /install`. No U15 local, esse é o contexto que efetivamente atualiza o log de pacotes e registra a extensão; a chamada elevada de `genexus /install` termina sem varrer os pacotes.

O caminho legado que tentava executar `genexus /install` dentro do PowerShell elevado foi removido. Ele podia não capturar a saída e, no U15 local, não realizava a varredura efetiva dos pacotes. O registro permanece exclusivamente no segundo `.bat`, sem Administrador.

## Próxima tarefa técnica

B000–B005 estão concluídos. A próxima frente canônica é B006 — validar persistência de metadata em File após fechar e reabrir a KB de teste, com autorização explícita antes de cada fase de escrita. O checkpoint canônico é `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`. A coluna Description vazia é uma limitação conhecida do Extensions Manager no U15 e não bloqueia este marco.

## Critério de conclusão e evidência esperada

- contrato oficial de descoberta/carregamento identificado e citado;
- manifesto, ponto de entrada ou equivalentes mínimos criados somente conforme esse contrato;
- pacote recompilado e conteúdo inspecionado;
- pacote instalado manualmente pelo usuário no U15 local como primeira validação;
- validação posterior no U14 por colegas da comunidade, sem data definida e sem bloquear o MVP;
- extensão mínima marcada e carregada sem erro na IDE;
- log e instruções reproduzíveis registrados;
- nenhuma Knowledge Base aberta, criada ou alterada durante o teste.

## Sem efeitos colaterais até aqui

- nenhuma pasta ou arquivo da instalação do GeneXus foi alterado pelo agente; as cópias para a instalação foram executadas pelo usuário por meio do instalador controlado;
- nenhuma Knowledge Base foi aberta ou modificada;
- nenhum registro de extensão foi criado pelo projeto ou pelo agente.
