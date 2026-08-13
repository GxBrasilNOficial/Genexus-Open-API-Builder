# NoAccept e elegibilidade de requests via Business Component

## Status

Implementação concluída no código da extensão; build local concluído nas soluções canônica e satélite U13; validação manual concluída no GeneXus 18 U13 e compatibilidade confirmada no GeneXus 18 U15.

## Descoberta confirmada por experimento A/B

O caso foi reproduzido na Transaction `Employee`, com `EmployeeAddedDate` declarado como `Date`, nullable, com:

```text
default(EmployeeAddedDate, &Today);
noaccept(EmployeeAddedDate);
```

O resultado foi:

1. Com `noaccept(EmployeeAddedDate);` presente, o build focado das Procedures `procEmployee_API_Create` e `procEmployee_API_Update` falhou na especificação com `spc0018`:
   `Property "Employeeaddeddate" is read-only. It can not be assigned.`
   As linhas apontadas foram 59 no Create e 76 no Update.
2. Removendo temporariamente somente `noaccept`, mantendo `default`, o build focado das duas Procedures passou por especificação, geração e compilação.
3. Restaurando `noaccept`, o mesmo erro voltou nas duas Procedures.

O `Build All` da Transaction `Employee` também passou depois da restauração, mas ele especificou a Transaction e o Business Component e não reespecificou necessariamente as Procedures da API. Por isso, o build focado das Procedures é a evidência determinante para este diagnóstico.

## Validação manual U13 após remoção e recriação

Para eliminar a colisão do `API Object` anterior, o comando B086 `Remover API gerada` removeu com sucesso 12 objetos próprios de `Employee`: `apiEmployee`, quatro Procedures, cinco SDTs próprios, `apiEmployee_Metadata` e o Folder `EmployeeOpenApi`. Os dois SDTs compartilhados e o Business Component da Transaction foram preservados.

Depois, o wizard foi executado novamente com a DLL satélite U13 instalada pelo `Install-ExtensionForGx18u13.bat`:

- contrato em memória: `CreateRequest=5`, `UpdateRequest=5`, `Response=9`, `ListFilters=2`;
- campos bloqueados visíveis: `CreateRequest=4`, `UpdateRequest=4`, `ListFilters=0`;
- geração: `Created=12`, `Updated=2`, `Blocked=0`, `Warnings=1`;
- os cinco SDTs próprios, quatro Procedures, `apiEmployee`, metadata e Folder foram criados; os dois SDTs compartilhados foram reencontrados;
- o único aviso continuou sendo o fallback de descrições em inglês.

Em seguida, o `Build All` especificou `procEmployee_API_Get`, `procEmployee_API_List`, `procEmployee_API_Create`, `procEmployee_API_Update` e `apiEmployee`, gerou os cinco SDTs de contrato e os dois compartilhados, gerou a documentação REST, compilou o DeveloperMenu e concluiu com sucesso. Permaneceu somente o aviso `pmm0003` sobre o módulo built-in `GeneXus`.

Essa execução é a confirmação final do diagnóstico: com `NoAccept(EmployeeAddedDate)` preservado, Create e Update foram especificados, gerados e compilados sem `spc0018`. O atributo foi excluído dos requests, sem ser removido dos contratos de resposta.

## Decisão funcional

`NoAccept` não significa que o atributo deixou de existir ou que não possa aparecer como dado de saída. Significa que a edição web não aceita entrada do usuário e, no contexto de Business Component, o valor recebido não pode ser atribuído como uma propriedade gravável. A semântica oficial está descrita em [NoAccept rule — GeneXus Wiki](https://wiki.genexus.com/commwiki/wiki?6856%2CNoAccept+rule%3D).

| Área do contrato | Comportamento para `NoAccept` |
|---|---|
| Aba `Requests` do wizard | atributo continua visível, mas desabilitado e com justificativa |
| `CreateRequest` | atributo não entra no SDT |
| `UpdateRequest` | atributo não entra no SDT |
| Procedures Create/Update | nenhum assignment para o atributo é gerado no BC |
| `Response` | atributo continua elegível como saída |
| `ListResponse` | atributo continua elegível como saída |
| `ListFilters` | não é bloqueado por esta regra; filtros apenas leem o atributo |
| API Object e metadata | refletem os requests efetivamente gerados |

Assim, “desabilitado em Requests” descreve a escolha de entrada da API, não a remoção do atributo da Transaction, da tabela ou dos contratos de resposta.

## Implementação

- `Src/Extension/Diagnostics/PrototypeWizardNoAcceptRuleReader.cs` lê a fonte persistida de `Transaction.Rules`, ignora comentários e literais e reconhece `NoAccept(Attribute)` sem depender de propriedades internas instáveis do SDK.
- `PrototypeWizardAttributeDecision.IsNoAccept` preserva a causa da classificação; as duas telas exibem o marcador `NoAccept` e a justificativa explica que o atributo é somente leitura via BC.
- `ApiPlan` filtra `CreateRequest` por `IsWritableByCreate` e `UpdateRequest` por `IsWritableByUpdate`; a filtragem também remove decisões de obrigatoriedade que apontariam para campos retirados.
- A sincronização reaplica a mesma proteção sobre campos vindos de metadata anterior, evitando que um request antigo reintroduza um campo `NoAccept`.
- O writer de Business Component continua consumindo somente os campos dos requests já filtrados; portanto, não precisa de uma exceção paralela nem de assignment condicional.

O reconhecimento foi escrito contra a fonte de Rules, e não contra uma propriedade específica de uma instalação. A decisão de produto é tratada como válida para as versões GeneXus 18 suportadas; a confirmação manual foi concluída em U13 e a compatibilidade foi confirmada novamente em U15.

## Limite deliberado

O leitor reconhece a forma direta `NoAccept(NomeDoAtributo)`, inclusive quando a regra tem condição. A implementação é conservadora: não tenta interpretar uma gramática completa de Rules nem inferir `NoAccept` a partir de texto em comentários ou strings. Uma forma sintática diferente deverá ganhar teste e regra explícita antes de ser aceita.

## Validação local

- `Tests/WizardContract/Test-PrototypeWizardNoAcceptRuleReader.ps1`: passou; comentários, strings, condição, diferença de caixa e deduplicação foram cobertos.
- `Tests/WizardContract/Test-NoAcceptRequestEligibilityContract.ps1`: protege a ligação entre leitor, decisão do wizard, `ApiPlan`, sincronização e writer.
- `dotnet build Src/GenexusOpenApiBuilder.sln -c Release --no-restore`: passou sem erros ou avisos.
- `dotnet build Src/GenexusOpenApiBuilder.Gx18u13.sln -c Release --no-restore`: passou sem erros; permanece apenas o aviso conhecido `MSB3277` de unificação de `mscorlib` nas referências pinadas da U13.
- Nenhum arquivo da instalação protegida em `C:\Program Files (x86)\GeneXus` foi alterado.

## Escopo ainda não coberto

A validação de compatibilidade U15 foi concluída depois da remoção e recriação da API. O `Build With This Only` fica opcional para capturar uma evidência ainda mais isolada. A validação HTTP da API gerada permanece separada desta frente; como a mudança é somente de código e não altera manifesto ou registro, não houve necessidade de `genexus /install` adicional por causa da correção da DLL.
