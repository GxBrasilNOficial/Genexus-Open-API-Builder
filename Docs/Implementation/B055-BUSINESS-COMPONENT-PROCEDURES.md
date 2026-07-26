# B055 - Uso via Business Component nas Procedures Create e Update

## Estado

B055 foi validado manualmente no GeneXus 18 U15 em 2026-07-26 como aplicação real de `Business Component` nas Procedures de Create e Update já criadas por B050-B053.

A etapa foi integrada ao encerramento de `Abrir Wizard (B030)` pela opção `Aplicar Create/Update via Business Component ao concluir`, localizada na aba `Business Component`.

## Escopo implementado

- criação de `ApiPlanBusinessComponentWriter` para aplicar o recorte B055 a partir do `ApiPlan` em memória;
- preflight de Transaction, SDTs, Procedures e API Object próprios antes de gravar;
- bloqueio quando a Transaction não está habilitada como `Business Component`;
- geração de Source e Rules das Procedures `proc<NomeBase>_API_Create` e `proc<NomeBase>_API_Update` usando a Transaction como BC;
- criação de variáveis reais pelo modelo de variáveis da Procedure, incluindo variáveis de chave baseadas em `Attribute:<NomeAtributo>`;
- persistência de Source por `ProcedurePart.Source` e Rules por `Rules.Source`, evitando o caminho textual que não alimentava o editor visível da IDE;
- validação pós-save de Source, Rules e variáveis reencontradas;
- suporte no SDT writer aos tipos públicos encontrados na validação composta: `BITMAP`, `BINARY`, `BINARYFILE`, `VIDEO`, `AUDIO`, `GEOGRAPHY`, `GEOPOINT`, `GEOPOLYGON` e `GEOLINE`.

## Comportamento validado

### Chave simples

Na KB `wsEducacaoSpTeste`, Transaction `Carga`:

- o wizard reencontrou SDTs, Procedures e API Object existentes;
- B055 aplicou Create/Update via Business Component;
- `procCarga_API_Create` persistiu `parm(in:&CreateRequest, out:&CreateResponse);`, Source e variáveis reais;
- `procCarga_API_Update` persistiu `parm(in:&CargaId, in:&UpdateRequest, out:&UpdateResponse);`, `&Carga.Load(&CargaId)`, Source e variáveis reais;
- `Build With This Only` passou para `procCarga_API_Create` e `procCarga_API_Update`.

### Chave composta

Na KB `GxTest3`, Transaction `Teste`:

- após remover campos geográficos que bloqueavam a reorg do PostgreSQL da KB de teste, o wizard criou novamente os 7 SDTs, 4 Procedures e o API Object `apiTeste`;
- B055 aplicou Create/Update via Business Component com `PrimaryKeyParts=2`;
- `procTeste_API_Update` persistiu `parm(in:&TesteDate, in:&TesteId, in:&UpdateRequest, out:&UpdateResponse);`;
- o Source usa `&Teste.Load(&TesteDate, &TesteId)` antes e depois do `Save()`;
- as variáveis `TesteDate` e `TesteId` foram criadas baseadas nos atributos correspondentes;
- `Build With This Only` passou para `procTeste_API_Create` e `procTeste_API_Update`.

## Limites explícitos

B055 não completa:

- paths ou métodos HTTP finais;
- códigos HTTP;
- `Location` de Create;
- semântica REST completa de List/Get/Create/Update;
- segurança definitiva;
- metadata persistente definitiva;
- descrições reais em serviços do API Object.

Esses itens permanecem nas próximas frentes da Sprint 5 e da Sprint 6.

## Próximo passo

B056 é a próxima frente canônica: aplicar descrições de serviços no API Object real, reaproveitando as descrições já resolvidas no `ApiPlan`, sem antecipar REST completo, segurança definitiva ou metadata persistente.
