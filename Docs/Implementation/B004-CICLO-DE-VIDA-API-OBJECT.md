# B004 — Ciclo de vida de API Object

## Objetivo

Comprovar, em uma Knowledge Base de teste, que a extensão consegue criar, alterar, reler e excluir um API Object por meio das APIs públicas do GeneXus Extensibility SDK, sem tocar em APIs existentes.

## Ambiente e limites

- KB de teste: `wsEducacaoSpTeste`;
- IDE validada: GeneXus 18 U15 local;
- objeto temporário: `apiGxOpenApiB004Probe`;
- a instalação do GeneXus não foi alterada pelo agente; a cópia e o registro da DLL foram executados manualmente pelo usuário pelos scripts controlados do repositório;
- cada fase que escreve na KB foi executada somente após autorização explícita do usuário.

## Contrato público comprovado

A inspeção dos metadados públicos do SDK 18.13.2 identificou `Artech.Genexus.Common.Objects.API` e os membros usados no teste:

- `API.Create(KBModel)` para criar o objeto;
- `API.Get(KBModel, Guid)` para reler pelo identificador;
- `API.GetAll(KBModel)` para localizar e confirmar ausência;
- `KBObject.Save()` para persistir nome e descrição;
- `KBObject.Delete()` para excluir o objeto.

Os comandos foram registrados no contexto de um objeto da KB e obtêm o modelo por `KBObjectSelectionHelper.TryGetOnlyOneKBObjectFrom(data.Context)`. O submenu de diagnóstico foi usado somente para este spike.

## Salvaguardas implementadas

A sonda não executa automaticamente quando a KB é aberta. As operações exigem comando explícito no submenu **Genexus Open API Builder**.

Antes de alterar, reler ou excluir, ela exige exatamente um objeto com o nome `apiGxOpenApiB004Probe` e uma das descrições sentinela do próprio teste:

- `Gx Open API Builder B004 Probe - criado`;
- `Gx Open API Builder B004 Probe - alterado`.

Se houver zero, mais de um ou descrição diferente, a operação é bloqueada sem escrita. Essa regra também permite retomar o teste após reinstalar a extensão, quando a memória de processo não está mais disponível.

## Execução reproduzida

1. `B004PreflightApiObject` confirmou que o nome estava disponível e não fez alteração.
2. Com autorização de criação, `B004CreateApiObject` criou o objeto e o releu.
3. Após reinstalação manual da DLL, com autorização de alteração, `B004UpdateApiObject` alterou a descrição e releu o mesmo objeto.
4. `B004ReadApiObject` confirmou a persistência sem escrita.
5. Com autorização de exclusão, `B004DeleteApiObject` removeu o objeto e confirmou sua ausência por GUID.

## Evidências observadas

| Fase | Resultado |
| --- | --- |
| Criação e releitura | `Name='apiGxOpenApiB004Probe'`, `Guid='06baea65-0638-4195-8f84-694ff6411820'`, `Description='Gx Open API Builder B004 Probe - criado'` |
| Alteração e releitura | Mesmo GUID; `Description='Gx Open API Builder B004 Probe - alterado'` |
| Releitura independente | Mesmo nome, GUID e descrição alterada após reinstalação da extensão |
| Exclusão | `API Object de teste excluído e ausência confirmada: Guid='06baea65-0638-4195-8f84-694ff6411820'` |

## Conclusão

B004 está concluído. O ciclo de vida de um API Object oficial foi comprovado por APIs públicas, com confirmação de persistência e de exclusão. Nenhuma API preexistente foi alterada. A próxima frente é B005, conforme `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`.
