# B055 - Uso via Business Component nas Procedures Create e Update

## Estado

B055 foi validado manualmente no GeneXus 18 U15 em 2026-07-26 como aplicação real de `Business Component` nas Procedures de Create e Update já criadas por B050-B053, com sincronização posterior do API Object como consumidor final do contrato.

A etapa foi integrada ao encerramento de `Abrir Wizard (B030)` pela opção `Aplicar Create/Update via Business Component ao concluir`, localizada na aba `Business Component`.

## Escopo implementado

- criação de `ApiPlanBusinessComponentWriter` para aplicar o recorte B055 a partir do `ApiPlan` em memória;
- preflight de Transaction, SDTs, Procedures e API Object próprios antes de gravar;
- B055 reexecuta B040-B046 para reencontrar e reconfigurar SDTs próprios/compartilhados antes de gravar Procedures e API Object, inclusive quando a aba SDTs não foi marcada nesta execução;
- bloqueio quando a Transaction não está habilitada como `Business Component`;
- geração de Source e Rules das Procedures `proc<NomeBase>_API_Create` e `proc<NomeBase>_API_Update` usando a Transaction como BC;
- criação de variáveis reais pelo modelo de variáveis da Procedure, incluindo variáveis de chave baseadas em `Attribute:<NomeAtributo>`;
- geração/reconfiguração dos SDTs próprios de request/response com membros baseados nos atributos da Transaction, preservando domínios usados pelo Business Component;
- persistência de Source por `ProcedurePart.Source` e Rules por `Rules.Source`, evitando o caminho textual que não alimentava o editor visível da IDE;
- sincronização do API Object com `ServiceGroupSource.Source` parametrizado para Create/Update e variáveis reais de API contendo chaves, requests e responses usados nas chamadas;
- realinhamento do API Object próprio para o Folder irmão `<Transaction>OpenApi` adiado para depois do preflight de Procedures, API Object e variáveis, reduzindo risco de alteração parcial antes de falhas detectáveis;
- validação pré-save do contrato API/Procedure, incluindo resolução de tipos das variáveis das Procedures e do API Object, reencontro dos SDTs usados pelas variáveis do API Object, bloqueio de Service Source B054 manualmente divergente e bloqueio de variáveis extras, ausentes ou com tipo/atributo base incompatível no API Object B055 reencontrado;
- validação pós-save de Source, Rules e variáveis reencontradas nas Procedures e de Service Source/variáveis reencontradas no API Object;
- suporte no SDT writer aos tipos públicos encontrados na validação composta: `BITMAP`, `BINARY`, `BINARYFILE`, `VIDEO`, `AUDIO`, `GEOGRAPHY`, `GEOPOINT`, `GEOPOLYGON` e `GEOLINE`.

## Comportamento validado

### Chave simples

Na KB `wsEducacaoSpTeste`, Transaction `Carga`:

- o wizard reencontrou SDTs, Procedures e API Object existentes;
- B055 aplicou Create/Update via Business Component;
- `procCarga_API_Create` persistiu `parm(in:&CreateRequest, out:&CreateResponse);`, Source e variáveis reais;
- `procCarga_API_Update` persistiu `parm(in:&CargaId, in:&UpdateRequest, out:&UpdateResponse);`, `&Carga.Load(&CargaId)`, Source e variáveis reais;
- `Build With This Only` passou para `procCarga_API_Create` e `procCarga_API_Update`;
- esta validação inicial não compilou o API Object depois da mudança de assinatura, gap corrigido no runtime e pendente de revalidação manual.

### Chave composta

Na KB `GxTest3`, Transaction `Teste`:

- após remover campos geográficos que bloqueavam a reorg do PostgreSQL da KB de teste, o wizard criou novamente os 7 SDTs, 4 Procedures e o API Object `apiTeste`;
- B055 aplicou Create/Update via Business Component com `PrimaryKeyParts=2`;
- `procTeste_API_Update` persistiu `parm(in:&TesteDate, in:&TesteId, in:&UpdateRequest, out:&UpdateResponse);`;
- o Source usa `&Teste.Load(&TesteDate, &TesteId)` antes e depois do `Save()`;
- as variáveis `TesteDate` e `TesteId` foram criadas baseadas nos atributos correspondentes;
- `Build With This Only` passou para `procTeste_API_Create` e `procTeste_API_Update`;
- esta validação inicial não compilou o API Object depois da mudança de assinatura, gap corrigido no runtime e pendente de revalidação manual.

## Revalidação pós-revisão do API Object

Na KB `GxTest3`, Transaction `Teste`, após reinstalar a DLL corrigida:

- `apiTeste` passou a declarar `Create(in: &CreateRequest, out: &CreateResponse)` delegando para `procTeste_API_Create(&CreateRequest, &CreateResponse)`;
- `apiTeste` passou a declarar `Update(in: &TesteDate, in: &TesteId, in: &UpdateRequest, out: &UpdateResponse)` delegando para `procTeste_API_Update(&TesteDate, &TesteId, &UpdateRequest, &UpdateResponse)`;
- a aba `Variables` do API Object exibiu `CreateRequest`, `CreateResponse`, `TesteDate`, `TesteId`, `UpdateRequest` e `UpdateResponse` com tipos compatíveis;
- `Build With This Only` de `apiTeste` passou por especificação, geração, documentação REST, Protocol Buffer, compilação e atualização de configuração web;
- o warning de `LSI.Extensions` sobre variáveis não usadas foi descartado como evidência bloqueante porque o build nativo do API Object reconheceu as variáveis usadas na assinatura e nas chamadas.

## Revalidação pós-correção de reexecução conservadora e domínios em SDT

Na Transaction `GuiaPed`, após reinstalar a DLL corrigida:

- o wizard concluiu com `GenerateSdts=True`, `GenerateProcedures=True`, `GenerateApiObject=True` e `ApplyBusinessComponent=True`;
- B040-B046 reencontrou e reconfigurou os 7 SDTs em `GuiaPedOpenApi`/`GxOpenAPI`;
- B050-B053 reencontrou as 4 Procedures em `GuiaPedOpenApi`;
- B054 detectou o API Object `apiGuiaPed` existente e delegou a sincronização para B055;
- B055 aplicou Create/Update via Business Component e sincronizou o API Object `apiGuiaPed`;
- `Build All` passou especificando `apiGuiaPed`, `procGuiaPed_API_Create` e `procGuiaPed_API_Update`, gerando os SDTs de request/response e a documentação REST sem erro;
- a validação cobre o caso de atributos baseados em domínio nos SDTs de request/response usados por Business Component.

## Revalidação dos caminhos isolados do wizard

Na Transaction `GuiaPed`, após reinstalar a DLL corrigida, a reexecução isolada das etapas do wizard foi validada manualmente:

- somente `Business Component` marcado: o wizard executou `GenerateSdts=False`, `GenerateProcedures=False`, `GenerateApiObject=False` e `ApplyBusinessComponent=True`; B055 reencontrou/reconfigurou dependências, sincronizou Procedures e API Object, e `Build With This Only` de `apiGuiaPed` passou;
- somente `Procedures` marcado: o wizard executou `GenerateSdts=False`, `GenerateProcedures=True`, `GenerateApiObject=False` e `ApplyBusinessComponent=False`; B050-B053 reencontrou as 4 Procedures existentes sem sobrescrever o Source/Rules/variáveis B055 de Create/Update;
- somente `API Object` marcado: o wizard executou `GenerateSdts=False`, `GenerateProcedures=False`, `GenerateApiObject=True` e `ApplyBusinessComponent=False`; B054 reencontrou SDTs e Procedures como dependências, reencontrou `apiGuiaPed`, preservou o Service Source parametrizado e as variáveis `CreateRequest`, `CreateResponse`, `GuiaPedIdboleto`, `UpdateRequest` e `UpdateResponse`, e `Build With This Only` de `apiGuiaPed` passou.

Essa matriz cobre a reexecução conservadora das três confirmações independentes relacionadas ao trio SDT/Procedure/API no estado pós-B055, sem completar REST, códigos HTTP, segurança definitiva ou metadata persistente.

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
