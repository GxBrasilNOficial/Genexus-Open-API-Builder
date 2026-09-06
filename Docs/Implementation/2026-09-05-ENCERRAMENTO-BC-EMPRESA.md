# Encerramento da investigação do Business Component da `Empresa`

**Data:** 2026-09-05
**KB:** `FabricaBrasil18Test` (`fabricabrasil18test`)
**Transaction:** `Empresa`
**GeneXus:** 18 U15
**Escopo:** investigação da falha da etapa Business Component; não é correção do gerador C# do GeneXus.

## Pergunta inicial

A etapa de Business Component falhou em todas as cinco execuções que a alcançaram. O diário de sondagem separou dois sintomas:

- quatro ocorrências de `Collection was modified; enumeration operation may not execute`;
- uma `Artech.Common.Diagnostics.ValidationException`, em `KBObjectManager.PrepareSave`, durante o `Save()` de `procEmpresa_API_Create`.

O objetivo desta rodada era identificar a causa provável do segundo ramo e executar o menor experimento capaz de distingui-la de um estado inconsistente da KB.

## Experimento executado

### 1. Remoção limpa

O comando `Remover API gerada` foi executado na `Empresa`.

Resultado B081:

- `Resultado='Success'`;
- `Removidos=50`;
- `Bloqueados=0`;
- `Avisos=0`.

Foram removidos o API Object `apiEmpresa`, quatro Procedures, 44 SDTs próprios e `apiEmpresa_Metadata`. Permaneceram a Transaction, o Business Component, os três SDTs compartilhados e a pasta reutilizada `EmpresaOpenApi`.

### 2. Baseline sem API

Antes de reaplicar, foi executado `Build All` nos dois environments:

| Environment | Resultado |
|---|---|
| `CSharpModel` | `Success: Build All`; compilação do DeveloperMenu OK; 2.196 arquivos JavaScript válidos |
| `NETFrameworkPostgreSQL` | `Success: Build All`; compilação do DeveloperMenu OK; 2.196 arquivos JavaScript válidos |

Ambos registraram `No objects to Specify`. Esse baseline mostrou que a KB e os environments compilavam sem os objetos da API.

### 3. Reaplicação limpa

O Wizard foi reaplicado com Business Component habilitado.

Resultado B081:

- `Resultado='SuccessWithWarnings'`;
- `Criados=50`;
- `Atualizados=0`;
- `Bloqueados=0`;
- `Avisos=2`;
- `Fase BusinessComponent=29759 ms`.

Os dois avisos foram o fallback das descrições para inglês e a reutilização da pasta preexistente. Não houve `Collection was modified`, `ValidationException` ou bloqueio.

O diagnóstico B112 registrou que `procEmpresa_API_Create` foi preparado e salvo com:

- `&HttpResponse`: tipo SDK `GX_USRDEFTYP`, `ATTCUSTOMTYPE=HttpResponse`;
- `&LocationUrl`: tipo `VARCHAR`, `ATTCUSTOMTYPE=VarChar`, comprimento `1024`;
- `RestStatusCode`: `Numeric(3.0)`.

### 4. Build após a reaplicação

No environment de referência `CSharpModel`, o `Build All` passou novamente, incluindo a geração dos API Objects, Procedures e SDTs recém-criados e a compilação do DeveloperMenu.

No `NETFrameworkPostgreSQL`, a geração também passou, mas a compilação falhou com conversões incompatíveis entre `bool`, `decimal` e `short` em SDTs gerados. Esse defeito é posterior à etapa de Business Component e específico do pipeline desse environment. Um `Rebuild All` posterior poderá separar resíduo de cache de defeito persistente do gerador; isso não é necessário para concluir esta investigação.

## Conclusão

### Ramo B — `ValidationException`

A causa provável é um estado parcial ou inconsistente dos objetos gerados na KB — API Object, Procedures, SDTs e metadata fora de uma combinação íntegra — e não o comprimento de `&LocationUrl` nem a presença do External Object `HttpResponse`.

O experimento mínimo confirmou a hipótese operacional: remover os 50 objetos, compilar a KB limpa nos environments, reaplicar a API e compilar novamente. A reaplicação limpa salvou `procEmpresa_API_Create` e o Build All do `CSharpModel` passou.

A causa exata da primeira `ValidationException` não pode ser reconstruída retroativamente, porque aquela execução terminou com fechamento forçado da IDE e não deixou um diagnóstico persistido equivalente ao B112. Portanto, esta conclusão é causalmente forte, mas permanece classificada como **causa provável**, não como reprodução determinística da exceção histórica.

### Ramo A — `Collection was modified`

O sintoma não se repetiu na remoção/reaplicação limpa. A hipótese específica de reentrância causada por `Application.DoEvents()` continua não confirmada; não há base nesta rodada para atribuí-la definitivamente ao `Pump`.

### Fora do escopo do encerramento

Os erros `bool`/`decimal`/`short` do `NETFrameworkPostgreSQL` pertencem à geração/compilação C# do GeneXus naquele environment. O `CSharpModel` compilou a geração limpa, e nenhum novo defeito da extensão foi identificado nessa frente.

## Estado das sondas e do código

- `B109ExceptionProbe`, `B111*Probe`, `ApiPlanSaveBoundaryProbe` e
  `ApiPlanMetadataVisibilityProbe` continuam instrumentação temporária conforme o checklist do
  checkpoint;
- a revisão por pares da sprint `S-B111` continua sendo a próxima frente formal do projeto.

**Funcionalidade de produção acrescentada na mesma rodada.** Além da instrumentação, entrou no
Wizard a **recuperação explícita de metadata órfã** (`ApiPlanOrphanMetadataRecovery`), com
preferência persistida, diálogo de confirmação e string localizada. Ela é **desligada por
padrão**, cria somente o File de metadata e não altera API Object, Procedures ou SDTs.

Isso **não** foi derivado da investigação: foi decisão do responsável humano, tomada ao ver que
`B115` — a ausência de qualquer caminho de limpeza para API gerada sem metadata — deixa o
usuário sem saída pela ferramenta. É entrega parcial daquele item: devolve a metadata ausente e,
com isso, reabilita `Remover` e `Sincronizar`, que hoje bloqueiam por metadata não encontrada.

A redação anterior desta seção afirmava que “nenhuma correção de produção foi derivada desta
investigação”. A frase era verdadeira no sentido estrito — a funcionalidade não saiu da
investigação —, mas omitia que uma funcionalidade havia entrado, o que induzia a leitura errada
do commit. Corrigido em 2026-09-05.

**Reparos aplicados após revisão do commit**, na mesma data:

- as duas sondas novas usavam os prefixos `B112`/`B113`, que no backlog designam outros itens —
  truncamento de `Description` e stall esporádico de gravação. Renomeadas para
  `ApiPlanSaveBoundaryProbe` e `ApiPlanMetadataVisibilityProbe`;
- `ApiPlanOrphanMetadataRecovery` chamava `ApiPlanKbObjectNameIndex.Create` fora da allowlist da
  regra de origem única, quebrando `tests.kbIndexReuse` e bloqueando o gate mecânico. O método
  foi renomeado para `TryPrepareOrphanMetadataRecovery` — `TryPrepare` seria genérico demais
  para uma allowlist por símbolo — e incluído na lista, como fluxo legítimo equivalente ao do
  Remover e ao do Sincronizar.

## Evidências relacionadas

- [`2026-09-04-B111-SONDAS-IDENTIDADE-E-DIARIO.md`](2026-09-04-B111-SONDAS-IDENTIDADE-E-DIARIO.md), §§10.7–10.8 — sintomas originais e stack da `ValidationException`;
- [`2026-09-04-EVIDENCIA-IDE-DRIFT-API-OBJECT.md`](2026-09-04-EVIDENCIA-IDE-DRIFT-API-OBJECT.md), §8 — remoção de 50 objetos e preservação do BC;
- [`B086-REMOVER-API-GERADA.md`](B086-REMOVER-API-GERADA.md) — contrato do comando de remoção.
