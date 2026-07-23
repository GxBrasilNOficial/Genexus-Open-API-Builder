# B037 - Obrigatorio no Payload no Wizard

Implementacao local preparada em 2026-07-23: o wizard unico aberto por `Abrir Wizard (B030)` consolidou a leitura de `Obrigatorio no payload` para `CreateRequest` e `UpdateRequest`, distinguindo presenca do membro JSON de valor nao vazio.

## Objetivo

Revisar e consolidar no wizard unico a decisao de obrigatoriedade tecnica no payload para `CreateRequest` e `UpdateRequest`, mantendo todas as escolhas apenas em memoria e sem criar `ApiPlan` ou objetos de API.

## Escopo implementado

- a aba `Obrigatorios` passou a separar visualmente `CreateRequest` e `UpdateRequest`;
- cada decisao mostra campo, valor de `Required` e motivo legivel;
- `CreateRequest` mantem campos nullable como opcionais no payload;
- `CreateRequest` mantem campos sensiveis selecionados como opcionais no prototipo;
- `CreateRequest` marca como `Required=True` campos selecionados sem nulabilidade conhecida;
- `UpdateRequest` marca todo membro selecionado como `Required=True`, seguindo a regra de PUT completo;
- a UI e o resumo indicam que `Required` significa presenca do membro JSON, nao valor nao vazio;
- vazio, `false` e `0` continuam tratados como valores enviados e sujeitos ao BC;
- nenhuma escolha foi persistida e nenhum objeto foi criado, alterado ou excluido pela geracao.

## Implementacao

`Src/Extension/PrototypeWizardDialog.cs` substitui o texto unico da aba `Obrigatorios` por duas areas read-only, uma para `CreateRequest` e outra para `UpdateRequest`. As decisoes continuam sendo calculadas a partir de `PrototypeWizardRequiredFieldDecision` e armazenadas somente em `PrototypeWizardFlowSelection` quando o usuario conclui o wizard.

`Src/Extension/Package.cs` adiciona a evidencia B037 na janela Output ao concluir o wizard, com as contagens de `CreateRequired` e `UpdateRequired` e a declaracao explicita de que `Required` representa presenca do membro JSON.

## Validacao local

- `dotnet build Src\GenexusOpenApiBuilder.sln --configuration Release`: OK, 0 erros; avisos `NU1900` ocorreram por indisponibilidade de consulta de vulnerabilidades nos feeds NuGet;
- `pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1`: OK, com 8 comandos registrados e sincronizados;
- `git diff --check`: OK.

## Validacao manual pendente

A validacao funcional no GeneXus 18 U15 ainda deve confirmar, por `Abrir Wizard (B030)`, que:

- a aba `Obrigatorios` exibe `CreateRequest` e `UpdateRequest` em areas separadas;
- campos nullable selecionados no `CreateRequest` aparecem com `Required=False`;
- campos selecionados sem nulabilidade conhecida aparecem com `Required=True` no `CreateRequest`;
- todos os campos selecionados no `UpdateRequest` aparecem com `Required=True`;
- o resumo `B037` deixa claro que `Required` significa presenca do membro JSON, nao valor nao vazio;
- a Output inclui a linha `[B037] Obrigatorio no payload consolidado`;
- nenhuma escolha e persistida, nenhum `ApiPlan` e criado e nenhum objeto de API e gerado.

## Resultado parcial

Criterio local atendido em 2026-07-23: B037 esta implementado e compilando, pronto para instalacao manual da DLL e validacao funcional na IDE. O checkpoint operacional nao foi promovido porque a validacao manual no GeneXus ainda esta pendente.
