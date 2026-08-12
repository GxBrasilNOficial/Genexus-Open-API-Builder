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

## Contrato operacional do menu contextual

Um comando só está completamente registrado quando o mesmo ID aparece nas três camadas: `AddCommand(new CommandKey(...))` em `Src/Extension/Package.cs`, `CommandDefinition` no manifesto e `Command refid` no grupo de comandos em `Groups` que o submenu referencia. Alterar apenas uma ou duas camadas pode compilar sem erro, mas não produz a opção esperada na IDE.

Antes de cada build destinada à atualização manual da DLL, validar o contrato com:

```powershell
pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1
```

O placeholder não operacional **Futura Primeira Opção** foi necessário enquanto o submenu ainda não tinha comandos permanentes. Após a consolidação do wizard e das preferências por KB, o menu principal deve manter somente comandos operacionais vigentes.

O caminho legado que tentava executar `genexus /install` dentro do PowerShell elevado foi removido. Ele podia não capturar a saída e, no U15 local, não realizava a varredura efetiva dos pacotes. O registro permanece exclusivamente no segundo `.bat`, sem Administrador.

## Nota de revisão — B094 (2026-08-11)

Duas afirmações deste documento sobre `genexus /install` elevado foram **refutadas** pelo `B094`, no mesmo GeneXus 18 U15:

- em `Diagnóstico reproduzível`: "a chamada elevada de `genexus /install` termina sem varrer os pacotes";
- em `Contrato operacional do menu contextual`: "no U15 local, não realizava a varredura efetiva dos pacotes".

Em cmd já elevado, `genexus /install` varreu `Packages` e registrou `Package 'GenexusOpenApiBuilder.Extension.dll' added`. A observação original veio de **captura incompleta da saída**, não de ausência de varredura: redirecionar `> arquivo 2>&1` a partir de cmd normal produz log vazio, porque o trabalho real ocorre em processo elevado filho.

O texto original dos dois parágrafos é preservado como registro da observação de época. O contrato operacional vigente do repositório não muda: `Register-ExtensionForGeneXus18.bat` continua recusando execução elevada.

Evidência: [B094 — Instalação apenas com a DLL (sem clonar)](B094-INSTALACAO-APENAS-COM-A-DLL-SEM-CLONAR.md).

## Nota de revisão — U14 por usuário externo (2026-08-12)

O critério que pedia “validação posterior no U14 por colegas da comunidade, sem data definida e sem bloquear o MVP” ficou **fechado com evidência externa**: usuário Igor C. Menin, DLL do Release `0.1.0-alpha.1`, GeneXus 18 U14 (`18.0.187820`), cópia em `Packages` + `genexus /install`, menus e geração na KB `KbTesteGx18U14`. Fecha o residual de carregamento/uso prático em U14 citado pelo gate 1 da comprovação Sprint 7; a bateria completa de validação permanece no U15 do mantenedor. Evidência: [2026-08-12 — usuário externo U14](2026-08-12-EVIDENCIA-USUARIO-EXTERNO-U14-ALPHA.md); issue [#1](https://github.com/GxBrasilNOficial/Genexus-Open-API-Builder/issues/1).

## Próxima tarefa técnica

B000–B006 estão concluídos no U15. B020 também foi concluído posteriormente no U15; a próxima frente canônica vigente deve ser consultada no checkpoint `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`. A coluna Description vazia é uma limitação conhecida do Extensions Manager no U15 e não bloqueia o B000.

## Critério de conclusão e evidência esperada

- contrato oficial de descoberta/carregamento identificado e citado;
- manifesto, ponto de entrada ou equivalentes mínimos criados somente conforme esse contrato;
- pacote recompilado e conteúdo inspecionado;
- pacote instalado manualmente pelo usuário no U15 local como primeira validação;
- validação no U14 por usuário externo fechada em 2026-08-12 (carregamento + geração; Alpha `0.1.0-alpha.1`; issue #1); a bateria completa permanece no U15 do mantenedor;
- extensão mínima marcada e carregada sem erro na IDE;
- log e instruções reproduzíveis registrados;
- nenhuma Knowledge Base aberta, criada ou alterada durante o teste.

## Sem efeitos colaterais até aqui

- nenhuma pasta ou arquivo da instalação do GeneXus foi alterado pelo agente; as cópias para a instalação foram executadas pelo usuário por meio do instalador controlado;
- nenhuma Knowledge Base foi aberta ou modificada;
- nenhum registro de extensão foi criado pelo projeto ou pelo agente.
