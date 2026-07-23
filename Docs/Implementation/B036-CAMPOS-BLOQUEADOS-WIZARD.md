# B036 - Campos bloqueados visiveis no Wizard

Concluido no GeneXus 18 Upgrade 15 em 2026-07-23: o wizard unico aberto por `Abrir Wizard (B030)` incorporou a exibicao de campos tecnicamente inadequados desabilitados, com motivo legivel, mantendo-os desmarcados e impossiveis de selecionar.

## Objetivo

Exibir no wizard unico os campos tecnicamente inadequados para payload e filtro, sem oculta-los, preservando o motivo operacional e mantendo todas as decisoes apenas em memoria.

## Escopo implementado

- `Requests` exibe campos bloqueados em `CreateRequest` e `UpdateRequest`, desmarcados e sem alternancia pelo usuario;
- o motivo do bloqueio fica no proprio item bloqueado, com quebra de linha no painel;
- `Filtros List` exibe campos candidatos e mantem bloqueados desabilitados quando aplicavel;
- a area separada de motivos foi removida para evitar duplicacao e rolagem horizontal;
- a UI passou a usar controles com quebra real de linha para itens longos;
- o resumo `B036` apresenta decisoes e garantias em duas areas lado a lado;
- a Output registra contagens de campos bloqueados por `CreateRequest`, `UpdateRequest` e `ListFilters`;
- nenhuma escolha foi persistida e nenhum `ApiPlan` ou objeto de API foi criado.

## Implementacao

`Src/Extension/PrototypeWizardDialog.cs` substitui as listas de selecao por paineis com `CheckBox` nativo por item. Cada item carrega um `ChoiceItem` com valor, rotulo, estado habilitado e motivo de bloqueio. Itens desabilitados usam `AutoCheck=false`, iniciam desmarcados e permanecem em cinza legivel.

A troca foi necessaria porque `CheckedListBox` nativo nao quebrou linha de forma confiavel no WinForms, mesmo com tentativa de desenho customizado. A solucao atual evita rolagem horizontal para motivos longos e preserva a selecao por checkbox esperada pelo usuario.

`Src/Extension/Package.cs` calcula as contagens B036 a partir do snapshot tecnico da Transaction e escreve a mensagem de evidencia na janela Output depois da conclusao normal do wizard.

Durante os ajustes de UX da frente, a aba `Seguranca` passou a exibir os tres valores oficiais de `SecurityLevel` (`Authentication`, `Authorization` e `None`) como opcoes visiveis, conforme a documentacao oficial GeneXus. Os contratos funcionais foram alinhados para registrar que `SecurityPermission` granular fica para evolucao posterior.

## Validacao local

- `dotnet build Src\GenexusOpenApiBuilder.sln --configuration Release`: OK, 0 erros; avisos `NU1900` ocorreram apenas por indisponibilidade de consulta de vulnerabilidades nos feeds NuGet;
- `pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1`: OK, com 8 comandos registrados e sincronizados;
- `git diff --check`: OK.

## Validacao manual no U15

Validacao manual concluida no U15 usando o wizard unico por `Abrir Wizard (B030)` no contexto de `Contrato`, `Escola` e `GuiaPed`.

A UI confirmou:

- campos bloqueados aparecem desabilitados e com motivo em `Requests`;
- itens bloqueados permanecem desmarcados e nao podem ser selecionados;
- filtros candidatos continuam selecionaveis quando tecnicamente validos;
- `Seguranca` mostra `Authentication`, `Authorization` e `None` sem combo;
- `Paginacao` usa campos numericos compactos;
- `Resumo B036` separa decisoes e garantias em duas areas;
- abas, botoes e textos visiveis receberam acentuacao pt-BR quando aplicavel.

A Output observada confirmou `Contrato`:

```text
[Genexus Open API Builder][B030] Wizard único concluido em memoria: Transaction='Contrato', Module='Root Module', SelectionSource='Contexto'.
[Genexus Open API Builder][B031] Contrato em memoria: Services='List,Get,Create,Update', Create=1, Update=1, Response=2, ListFilters=1.
[Genexus Open API Builder][B032] Paths e segurança em memoria: ApiName='apiContrato', ServicesBasePath='apiContrato', RestPath='/contrato', SecurityLevel='Authentication'.
[Genexus Open API Builder][B033] Obrigatoriedade em memoria: CreateRequired=0, UpdateRequired=1. Required significa presença do membro JSON, nao valor nao-vazio.
[Genexus Open API Builder][B036] Campos bloqueados visiveis no wizard: CreateRequest=1, UpdateRequest=1, ListFilters=0. Itens bloqueados ficaram desmarcados, com motivo, e nao podem ser selecionados.
[Genexus Open API Builder][B035] Business Component em memoria: IsBusinessComponent=True, EnabledDuringWizard=False, Status='Apta via Business Component'.
[Genexus Open API Builder][B034] Wizard concluido sem acionar cancelamento. Decisoes permanecem somente em memoria; nenhum ApiPlan foi criado e nenhuma geracao de objetos de API foi executada.
```

A Output observada confirmou `GuiaPed` com volume maior de campos bloqueados:

```text
[Genexus Open API Builder][B030] Wizard único concluido em memoria: Transaction='GuiaPed', Module='Root Module', SelectionSource='Contexto'.
[Genexus Open API Builder][B031] Contrato em memoria: Services='List,Get,Create,Update', Create=21, Update=21, Response=53, ListFilters=2.
[Genexus Open API Builder][B032] Paths e segurança em memoria: ApiName='apiGuiaPed', ServicesBasePath='apiGuiaPed', RestPath='/guiaped', SecurityLevel='Authentication'.
[Genexus Open API Builder][B033] Obrigatoriedade em memoria: CreateRequired=0, UpdateRequired=21. Required significa presença do membro JSON, nao valor nao-vazio.
[Genexus Open API Builder][B036] Campos bloqueados visiveis no wizard: CreateRequest=32, UpdateRequest=32, ListFilters=1. Itens bloqueados ficaram desmarcados, com motivo, e nao podem ser selecionados.
[Genexus Open API Builder][B035] Business Component em memoria: IsBusinessComponent=True, EnabledDuringWizard=False, Status='Apta via Business Component'.
[Genexus Open API Builder][B034] Wizard concluido sem acionar cancelamento. Decisoes permanecem somente em memoria; nenhum ApiPlan foi criado e nenhuma geracao de objetos de API foi executada.
```

## Resultado

Criterio atendido em 2026-07-23: B036 tornou campos tecnicamente inadequados visiveis, desabilitados, motivados e nao selecionaveis no wizard unico, com contagem na Output e resumo em memoria, sem `ApiPlan`, sem persistencia das escolhas e sem geracao de objetos de API. B037 foi concluido posteriormente, consolidando a obrigatoriedade tecnica no payload para `CreateRequest` e `UpdateRequest`.
