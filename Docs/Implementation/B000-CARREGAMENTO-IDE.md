# B000 — Carregamento da Extensão na IDE

## Estado

Pendente de instalação manual e teste em IDE. O bloqueio anterior baseado na ausência do instalador Platform SDK foi removido em 2026-07-16, pois o método oficial para GeneXus 18 Upgrade 14 ou posterior usa pacotes NuGet e MSBuild SDKs.

## Objetivo

Comprovar que o pacote mínimo da extensão carrega na IDE GeneXus 18 U14 ou posterior, usando o U15 local como primeiro ambiente de validação, sem criar ou alterar objetos em uma Knowledge Base.

## Evidência disponível

- o projeto foi migrado para `GeneXus.Package.UI.Sdk` e compilado com as referências oficiais do feed GeneXus Azure Artifacts;
- a build produziu `Src/Packages/Release/GenexusOpenApiBuilder.Extension.0.1.0-preview.1.nupkg`;
- esse pacote contém `lib/net471/GenexusOpenApiBuilder.Extension.dll`;
- o formato do pacote foi confirmado pela build, mas o fluxo de registro e descoberta na IDE ainda não foi demonstrado;
- a instalação local disponível é GeneXus 18 Upgrade 15, build `18.0.15.188745`, e serve somente como ambiente de teste.

Fonte oficial: [GeneXus Platform SDK Download](https://docs.genexus.com/en/wiki?27521,GeneXus+Platform+SDK+Download).

## Regra de segurança

`AGENTS.md` proíbe criar, alterar, mover, renomear ou excluir itens em `C:\Program Files (x86)\GeneXus` e subpastas. Portanto, este repositório não automatiza cópia, registro, execução de `/install` nem outra alteração na instalação do GeneXus.

## Próxima tarefa técnica

O `.nupkg` de B010 prova apenas o empacotamento NuGet mínimo. Ele não possui ainda manifesto `.package` nem ponto de entrada de extensão; portanto, não há base para supor que seja descoberto pela IDE.

O primeiro passo de B000 é consultar o contrato oficial e o exemplo de extensões, determinar quais arquivos, metadados e classe de entrada tornam um pacote descobrível, e implementar somente esse mínimo. Caso o contrato exija manifesto `.package`, ele será criado nesta etapa; se não exigir, a evidência deve registrar o mecanismo alternativo. Depois de recompilar e inspecionar o novo artefato, o usuário poderá executar manualmente o procedimento de instalação na instalação normal de teste. O agente permanece limitado a observação em modo leitura, salvo alteração explícita da regra local de proteção.

## Critério de conclusão e evidência esperada

- contrato oficial de descoberta/carregamento identificado e citado;
- manifesto, ponto de entrada ou equivalentes mínimos criados somente conforme esse contrato;
- pacote recompilado e conteúdo inspecionado;
- pacote instalado manualmente pelo usuário em U14 e, quando disponível, no U15 local;
- extensão mínima carregada sem erro na IDE;
- log e instruções reproduzíveis registrados;
- nenhuma Knowledge Base aberta, criada ou alterada durante o teste.

## Sem efeitos colaterais até aqui

- nenhuma pasta ou arquivo da instalação do GeneXus foi alterado;
- nenhuma Knowledge Base foi aberta ou modificada;
- nenhum registro de extensão foi criado pelo projeto ou pelo agente.