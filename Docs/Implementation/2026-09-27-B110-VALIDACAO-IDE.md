# B110 — Validação na IDE — 2026-09-27

Estado: **B110 fechada em 2026-09-27, com exceção de campo explícita no cenário 14**. Onze cenários têm PASS individual, o cenário 11 tem prova transversal delimitada, o cenário 3 é offline e a variante V1 do cenário 9 não tinha fixture disponível.

Sessão: 2026-09-27. Redação: 2026-09-27.
Frente: B110 — barreiras de Apply.
KB: wsEducacaoSpTeste.
Transactions exercitadas: Teste, Contrato, Carga, Laudo, Escola e Distribuidora, todas no Root Module.
Ambiente: GeneXus 18, instalação padrão. Update exato não registrado.
Resultado de campo: **PASS** nos cenários 1, 2, 4, 5, 6, 7, 8, 10, 12, 13 e 15 da seção 6.2. O cenário 11 tem prova de preservação antes/depois nos bloqueios T2 de Teste e T1 de Distribuidora, com alcance delimitado nas respectivas seções. O cenário 14 foi validado offline e aceito pelo mantenedor como exceção de observação na IDE. O cenário 10 validou Source divergente, GUID divergente e B115 inelegível, com restauração das preparações via MCP. O cenário 7 concluiu recuperação B115 em Transaction plana, cancelamento sem confirmação e ramos 7a/7b, incluindo reabertura após cada Apply. O cenário 2 confirmou a interface de bloqueio, o cancelamento e a restauração manual da Description original.

## Proveniência e alcance

- Repositório em main, commit 7c87c40 no momento do registro; árvore de trabalho limpa antes desta edição.
- DLL instalada comparada com a build local por Tools/Test-InstalledExtension.ps1: InstalledMatchesBuild=True; SHA-256 94CFA9A2BBF5D311CB9FA28B2A8B6DC957E78168EC3CAFF34208E8799A7595C5.
- O checker relata ActivationVerified=False porque não consulta a marcação do Extensions Manager. A abertura do Wizard e os Apply abaixo foram observados na IDE. A versão/update exatos da IDE não foram registrados.
- Os outputs foram colados pelo mantenedor nesta conversa; as telas foram anexadas nesta mesma sessão. As imagens não foram copiadas para o repositório.
- A identidade GUID da KB e uma inspeção independente da árvore final da KB não foram registradas. O estado final descrito abaixo vem dos relatórios da extensão e do diário apresentados.
- O escopo cobre apenas os cenários indicados neste documento. Não houve chamada HTTP dos endpoints, validação de runtime da API publicada ou instalação adicional. No cenário 7, o File `apiCarga_Metadata` foi removido de forma direcionada via GeneXus18MCP e recriado pelo B115; nenhum API Object, Procedure ou SDT foi alterado por essa recuperação.

## Cenário 4 — API existente apta e metadata completa

Esperado pelo plano: geração normal, sem painel de contrato reconstruído e com a ordem de gravação preservada.

| Verificação | Obtido |
|---|---|
| Estado na aba API Object | Reencontrar e validar; gerenciados=1, ausentes=0, planejados=1; confirmação para escrita marcada. |
| Nome na aba Paths | apiTeste somente para leitura, acompanhado da explicação de que a API gerada não pode ser renomeada. |
| Painel T2 | Não apareceu a aba Contrato reconstruído. |
| Resumo | Planejava API Object, SDTs, Procedures, List e metadata; Delete estava incluído entre os serviços selecionados. |
| Preflight e diário | Preflight agregado aprovado antes do primeiro Save. Diário concluiu em Completed/Completed, Durability=Confirmed. |
| Resultado | SuccessWithWarnings; 0 criados, 27 atualizados, 0 removidos, 0 bloqueados; 12 recibos de persistência; duração 8,736 s. |
| SDTs e Procedures | 20 SDTs reencontrados (17 próprios + 3 compartilhados) e 5 Procedures reencontradas; nenhuma criada. |
| API e metadata | Writer final List; um salvamento confirmado do API Object. Metadata apiTeste_Metadata reencontrada e atualizada. |
| Avisos | Descrições dos serviços usaram fallback em inglês porque o idioma da KB não estava validado por API pública. A pasta TesteOpenApi existente foi reutilizada e a descrição existente seria preservada. |

Rastreio observado no Output:

- OperationId: cebd2988-50a7-47f4-ac17-c58236cb64d9.
- ApplicationId: 90b46789-b0ef-4349-821c-b16c2eff8ef0.
- FileId do diário: 88.
- API Object apiTeste: GUID 019e6692-f831-4e29-958f-56c49826f101.
- Metadata apiTeste_Metadata: GUID 6175f3cc-0f17-43a2-bdea-dbaaf67015ed.
- Pasta TesteOpenApi: GUID 0d08b38c-2362-4100-9443-1ec46f121676.
- Checkpoints: 4; recibos: 12; inventário: 7.

Resultado: **PASS** para reencontro de API apta, nome somente leitura, ausência de T2 e conclusão do Apply. O estado de runtime HTTP não foi exercitado.

## Cenário 5 — API ausente e primeira geração com nome livre

Esperado pelo plano: primeira geração completa em estado Create, permitindo definir livremente o nome da API.

| Verificação | Obtido |
|---|---|
| Estado inicial na aba API Object | Criar; gerenciados=0, ausentes=1, planejados=1; confirmação para criar marcada. |
| Metadata e recuperação | apiContrato_Metadata estava ausente e o API Object apiContrato foi encontrado zero vezes. O Output informou que a recuperação não se aplicava. |
| Nome e paths | Nome alterado de apiContrato para apiContratoB110; Services base path acompanhou a alteração; RestPath permaneceu /contrato. A tela mostrou os paths resultantes. |
| Resumo | Criar/validar SDTs, Procedures, API Object, listagem e metadata estavam selecionados; completar REST via Business Component também estava selecionado. Delete estava entre os serviços. |
| Preflight e diário | Preflight agregado aprovado antes do primeiro Save. Diário concluiu em Completed/Completed, Durability=Confirmed. |
| Resultado | SuccessWithWarnings; 12 criados, 3 atualizados, 0 removidos, 0 bloqueados; 17 recibos de persistência; duração 5,784 s. |
| SDTs e Procedures | 4 SDTs próprios criados, 3 compartilhados reencontrados e 5 Procedures criadas. |
| API e metadata | API Object apiContratoB110 criada, salva uma vez pelo writer final List; metadata apiContrato_Metadata criada com integridade B067. |
| Aviso | Descrições dos serviços usaram fallback em inglês porque o idioma da KB não estava validado por API pública. |

Rastreio observado no Output:

- OperationId: b895951e-5954-4b43-99cb-f6889bc1a51b.
- ApplicationId: a940b357-cc83-4039-92e1-94597178e958.
- FileId do diário: 88.
- API Object apiContratoB110: GUID 485c8ed2-f184-40d5-9737-ddc53424edb8.
- Metadata apiContrato_Metadata criada: GUID 2df71550-b2b3-4b8b-ba7a-26042810fb82; SHA-256 7A893098142E3E9C9E394C4EEA9D4A97364826F10FA405AAEE41F491519B8BBDBF.
- PlannedContractHash da metadata: B27DEE57E7FFF92E9C3DC1560B28936C16E5CB5ABB1505AB935A0C41AEF97C87.
- Checkpoints: 4; recibos: 17; inventário: 12.

Resultado: **PASS** para primeira geração, nome livre, persistência do API Object e criação da metadata. O estado de runtime HTTP não foi exercitado.

## Cenário 6 — nenhuma etapa de escrita

Esperado pelo plano: ao concluir o teste com todas as etapas de escrita desmarcadas, o Apply deve informar que nenhuma escrita foi solicitada e parar sem abrir diário nem salvar objetos.

| Verificação | Obtido |
|---|---|
| Resumo | SDTs, Procedures, API Object, completar listagem, metadata e REST via Business Component estavam desmarcados; `EnabledDuringWizard=False`. O botão dizia Concluir teste. |
| Metadata | `apiContrato_Metadata` presente; GetAll encontrou 29 Files e o direct-Get reencontrou o FileId 115, GUID `2df71550-b2b3-4b8b-ba7a-26042810fb82`, com 42.481 bytes. |
| Recuperação | Não oferecida: o API Object `apiContrato` foi encontrado zero vezes, quando era esperado exatamente um. |
| Ramo sem escrita | Output: `[B040-B046/B060] Nenhuma etapa de escrita foi confirmada no wizard para Transaction='Contrato'. Nenhuma escrita foi solicitada.` |
| Diário e relatório | O Output colado terminou nessa mensagem; não mostrou abertura de diário, recibos, etapa de Save nem relatório final. |

Resultado: **PASS** para o ramo sem escrita solicitada. Não houve leitura independente da KB depois da execução; a evidência é a mensagem de saída e o fluxo exercitado.

## Cenário 1 — API existente bloqueada por MetadataMissing

Esperado pelo plano: com API Object existente e metadata ausente, a interface bloqueia as etapas de SDTs e Procedures, mostra a causa e não permite escrita.

| Verificação | Obtido |
|---|---|
| Metadata | Duas passagens de GetAll encontraram 28 Files; `apiContrato_Metadata` ausente (zero correspondências). `GxOpenApiBuilder_Settings` presente no FileId 30, GUID `6c5c9449-f119-40c3-9367-6b7a89d2a278`. |
| API existente | Diagnóstico B087 encontrou exatamente um API Object: `apiContratoB110`, GUID `485c8ed2-f184-40d5-9737-ddc53424edb8`, no Folder `ContratoOpenApi`. `MetadataPresente=False`; fingerprint, schema e integridade também ausentes. |
| SDTs e Procedures | Wizard mostrou estado bloqueado por um conflito `MetadataMissing`; as confirmações de escrita estavam desabilitadas. Procedures identificou a dependência dos SDTs e também mostrou o mesmo bloqueio. |
| Delete e Resumo | Para avançar, Delete foi desmarcado após o aviso de que exigia REST via Business Component. O Resumo então mostrou List, Get, Create e Update; todas as etapas de escrita falsas, `EnabledDuringWizard=False` e estado bloqueado por um conflito. |
| Paths | Nome `apiContratoB110`, Services base path `apiContratoB110` e RestPath `/contrato`; a tela exibiu a explicação de que o nome da API existente é somente leitura. Não foi tentada edição do campo nesta execução. |
| Recuperação | Não oferecida porque a busca esperava o API Object `apiContrato` e encontrou zero. O objeto existente era `apiContratoB110`, renomeado no cenário 5; não se inferiu outra causa. |
| Encerramento | Wizard cancelado; Output informou que nenhum ApiPlan foi criado e que nenhuma alteração foi feita na KB. |

Resultado: **PASS** para bloqueio de SDTs/Procedures por metadata ausente e ausência de escrita. A indicação somente leitura de Paths também foi observada nesse estado. A elegibilidade de recuperação B115 não foi exercitada.

## Cenário 2 — colisão em Procedure com API existente

Esperado pelo plano: com API existente e colisão em Procedure, as confirmações de SDTs e Procedures ficam desabilitadas, a etapa Procedures identifica a colisão e o fluxo da IDE não grava nada.

| Verificação | Obtido |
|---|---|
| Estado original da Procedure | Antes do teste, a imagem de Properties registrou `procTeste_API_List`, no Folder `TesteOpenApi`, com Description `procTeste_API_List - by Genexus Open API Builder`. |
| Preparação da colisão | O mantenedor confirmou ter alterado a Description somente para o valor temporário combinado, mantendo o nome e o Source; a captura da aba Procedures mostrou o conflito externo `procTeste_API_List`, Tipo Procedure, Module `Root Module`, Folder `TesteOpenApi`. |
| Interface de bloqueio | Aba Procedures: estado bloqueado por um conflito; confirmação de Procedures desabilitada. A dependência SDTs também aparece bloqueada e sua confirmação desabilitada. |
| Resumo | API `apiTeste`, serviços List, Get, Create e Update; Delete desmarcado. Todas as etapas de escrita estavam falsas, `EnabledDuringWizard=False` e o estado indicava um conflito. |
| Leitura da KB na abertura | Metadata `apiTeste_Metadata` presente; direct-Get reencontrou FileId 114, GUID `6175f3cc-0f17-43a2-bdea-dbaaf67015ed`, 117.984 bytes. B087 informou que o diagnóstico de posse do API Object ficou bloqueado antes de gerar lista de conflitos, pois SDTs ou Procedures precisavam ser resolvidos. |
| Encerramento | Após Cancelar, B034 informou que o Wizard descartou Transaction e decisões em memória, não criou ApiPlan e não fez alteração na KB. |
| Restauração manual | Após o cancelamento, o mantenedor confirmou a Description exata `procTeste_API_List - by Genexus Open API Builder`. |

Resultado: **PASS** para a interface esperada de bloqueio, cancelamento sem escrita pelo Wizard e restauração manual da Description. A causa exibida na aba Procedures foi a colisão com `procTeste_API_List`; o relatório de backend de Apply bloqueado permanece coberto por leitura estática e teste offline, conforme §6.2 do plano.

## Cenário 7 — recuperação B115 em Transaction plana (concluído)

Esperado pelo plano: em uma Transaction sem subníveis, com API Object próprio e metadata ausente, o B115 oferece a recuperação; ao reabrir o Wizard, a reconstrução do contrato não deve bloquear por falta de níveis. Depois, a matriz T2 exige exercitar a confirmação e os dois ramos de Apply: com geração de metadata marcada (7a) e desmarcada (7b).

| Verificação | Obtido |
|---|---|
| Transaction e estrutura | GeneXus18MCP encontrou `Carga` no Root Module. A estrutura tem `CargaId`, `CargaCarregamentoData`, `CargaObservacao` e `CargaObservacao2`, todos no nível principal, sem subníveis. |
| API existente | MCP encontrou o API Object `apiCarga`, Description `apiCarga - by Genexus Open API Builder`, em `Root Module/ExclusivoDessaKB/CargaOpenApi`. Get, Create e Update chamam `procCarga_API_Get`, `procCarga_API_Create` e `procCarga_API_Update`. |
| Inventário | Antes da recuperação, havia 3 Procedures, 3 SDTs próprios e 3 SDTs compartilhados. A API existente não tem List nem Delete; esta rodada testa elegibilidade/recuperação, não paridade dos cinco serviços. |
| Metadata antes do teste | MCP encontrou `apiCarga_Metadata` como File, Description `apiCarga_Metadata - by Genexus Open API Builder`, GUID `6bac1059-e4c7-4e13-bb71-1106b3c16490`. |
| Remoção direcionada | A prévia do MCP apontou somente `File:apiCarga_Metadata`, sem referências e sem ações implícitas. A exclusão confirmou um objeto removido, `persisted=True` e releitura confirmada; `apiCarga` continuou presente. |
| Oferta B115 | Output da IDE: metadata ausente; `API Object` próprio confirmado por Description e Service Source; inventário de 3 Procedures, 3 SDTs próprios e 3 compartilhados. |
| Recuperação | O mantenedor confirmou a oferta. B115 recriou `apiCarga_Metadata`, GUID `f10e5793-414f-46c2-9017-d5ec48bc4cd5`, 1.479 bytes. Output afirma que nenhum API Object, Procedure ou SDT foi alterado. |
| Diálogos da recuperação | A oferta alertou que somente a metadata seria criada; a confirmação informou que era necessário reabrir o Wizard e que nenhuma outra etapa fora executada nessa invocação. |
| Contrato reconstruído após reabrir | A tela mostrou `RestPath=/carga`, `Services base path=apiCarga`, serviços Create/Get/Update, 3 campos em Create/Update, filtro `CargaId`, resposta e atualização reconstruídas, e `Hierarquia: sem subníveis`. Não apareceu bloqueio por falta de níveis. |
| Resumo após reabrir | Serviços Get, Create e Update; sem List nem Delete. Todas as etapas de escrita estavam falsas e `EnabledDuringWizard=False`. O botão final dizia `Concluir teste`. |
| Painel T2 e ramo sem confirmação | A aba `Contrato reconstruído` exibiu a caixa `Confirmo o contrato reconstruído listado, incluindo minhas alterações nesta execução`, desmarcada. No Resumo, as etapas de escrita continuaram falsas e `EnabledDuringWizard=False`; o Output do `Concluir teste` registrou que nenhuma escrita foi solicitada. Em nova invocação, o mantenedor cancelou diretamente no painel sem confirmar; B034 registrou nenhum ApiPlan criado e nenhuma alteração na KB. |
| Preparação 7a | Mensagens de erro BC alteradas para `False`; o painel manteve a seção e exibiu `alterado nesta execução`. O mantenedor marcou a confirmação T2. Resumo final: Get/Create/Update; SDTs, Procedures, API Object, metadata e REST via BC `True`; completar listagem `False`; mensagens BC `False`. |
| Apply 7a | Preflight agregado aprovado antes do primeiro Save; 6 SDTs e 3 Procedures reencontrados. Writer final Business Component; API Object salvo uma vez. Resultado `SuccessWithWarnings`: 0 criados, 11 atualizados, 0 removidos, 0 bloqueados; 8 recibos, 0 falhas de persistência; duração 3.322 ms. |
| Diário 7a | OperationId `9f31a509-b9aa-4c93-b55f-d4d4daf6de17`; ApplicationId `4e11e4c2-5ea3-4bd0-81b1-99455d7edf4d`; FileId 88; 4 checkpoints; `Completed/Completed`, `Durability=Confirmed`. API Object GUID `48cae5b3-f935-4278-996d-096c343daa96`. |
| Metadata 7a | File `apiCarga_Metadata` reencontrado no mesmo GUID `f10e5793-414f-46c2-9017-d5ec48bc4cd5`; schema V4, 40.700 bytes; SHA-256 `E80C9100250836B59F0493BEF4CFFFB0AF5322CD6F71F44CD472998815698482`. Integridade B067 gravada; PlannedContractHash `5A8E6028F71E2C7341E17B460A1D1DF95F28537E95F40C2B9A35B1EDDDCC89A6`. |
| Relatório T2 7a | Registrou contrato reconstruído confirmado pelo usuário e 15 seções reconstruídas, incluindo `api.servicesBasePath` e `errorDetail.includeBusinessComponentMessages`; esta última identificada como alterada nesta execução. A mensagem sobre remover `recovery.imported` está no futuro; não substitui a inspeção da metadata gravada ou a verificação da próxima abertura. |
| Reabertura após 7a | Captura e confirmação do mantenedor: aba `Contrato reconstruído` ausente e mensagens de erro BC continuam desmarcadas. Direct-Get reencontrou FileId 117, mesmo GUID, 40.700 bytes. B115 informou que o File existe e não foi produzido pela recuperação; recuperação não se aplica. O comportamento confirma o encerramento do estado importado, sem inspeção literal do JSON. Cancelamento B034: nenhum ApiPlan criado, nenhuma alteração na KB nesta invocação. |
| Output final e persistência | Na reabertura, o direct-Get reencontrou `apiCarga_Metadata`, FileId 117, GUID `f10e5793-414f-46c2-9017-d5ec48bc4cd5`, 1.479 bytes. B115 não foi oferecida porque o File já registra o API Object atual. O Output final informa: `Nenhuma etapa de escrita foi confirmada no wizard para Transaction='Carga'. Nenhuma escrita foi solicitada.` |

Resultado: **PASS para o ramo 7a**, com confirmação, edição de seção, relatório, persistência concluída e reabertura sem painel T2, preservando mensagens BC `False`. A saída do estado importado foi verificada pelo comportamento do Wizard e diagnóstico B115, sem leitura literal da chave JSON.

### Ramo 7b — Apply sem gravar metadata

- O mantenedor apagou somente `apiCarga_Metadata` e aceitou nova recuperação B115. Metadata recriada com GUID `2100a264-6751-49dc-83f3-7adbc85b6f82`, 1.479 bytes; 3 Procedures, 3 SDTs próprios e 3 compartilhados. B115 declarou nenhuma alteração nesses artefatos.
- Na reabertura, FileId 118, mesmo GUID e 1.479 bytes. Resumo aprovado antes de aplicar: Get/Create/Update; SDTs, Procedures, API Object e REST via BC `True`; listagem e metadata `False`; mensagens BC `True`, padrão fixo da reconstrução. Confirmação T2 marcada pelo mantenedor.
- Apply: preflight agregado aprovado antes do primeiro Save; 6 SDTs e 3 Procedures reencontrados; writer final Business Component; API Object GUID `48cae5b3-f935-4278-996d-096c343daa96`, salvo uma vez.
- Resultado `SuccessWithWarnings`: 0 criados, 10 atualizados, 0 removidos, 0 bloqueados; 7 recibos, 0 falhas de persistência; duração 3.113 ms. O Output não registra execução de B060/B067 para gravar metadata.
- Diário: OperationId `15da3305-8373-496a-9856-23a32341fb26`; ApplicationId `1e642026-d099-4e42-a33d-eb70e81a23a2`; FileId 88; 4 checkpoints; `Completed/Completed`, `Durability=Confirmed`.
- Relatório: contrato reconstruído confirmado, 15 seções listadas e aviso `Sem Gerar metadata, recovery.imported permanece e o painel reaparecerá na próxima abertura do Wizard`.
- Reabertura pós-Apply: captura mostrou painel `Contrato reconstruído` reaparecendo, confirmação desmarcada e aviso de permanência da marca. Direct-Get reencontrou FileId 118, GUID `2100a264-6751-49dc-83f3-7adbc85b6f82`, 1.479 bytes. B115 não ofereceu nova recuperação porque o File já registra o API Object atual. B034 confirmou cancelamento sem ApiPlan e sem alteração na KB nesta invocação.

Resultado: **PASS para 7b e para o cenário 7 completo**. Sem gravar metadata, o estado importado continuou reconhecido e o painel reapareceu; com gravação em 7a, o painel deixou de aparecer. A permanência/saída da marca foi observada pelo comportamento e diagnóstico da extensão, sem leitura literal do JSON ou comparação independente dos bytes completos.

## Cenário 8 — recuperação B115 em Transaction hierárquica

Estado: **concluído**. A `Teste` não satisfaz a precondição plana do cenário 7: o diálogo B115 a identifica como hierárquica e avisa que a metadata recuperada não terá contrato de níveis.

| Verificação | Obtido |
|---|---|
| Preferências antes do ajuste | A primeira captura mostrou recuperação órfã marcada, recuperação proativa após bloqueio do diário marcada, paginação 50/200 e mensagens de erro BC marcadas. |
| Preferências preparadas | A segunda captura mostrou recuperação órfã ainda marcada, paginação 25/100 e mensagens de erro BC desmarcadas; os demais defaults visíveis foram mantidos. |
| Persistência das preferências | Output: `GxOpenApiBuilder_Settings`, `Status='Updated'`, GUID `6c5c9449-f119-40c3-9367-6b7a89d2a278`, 741 bytes. |
| Metadata e API | O mantenedor informou que apagou `apiTeste_Metadata`. A captura da árvore ainda mostra o API Object `apiTeste` e os objetos gerados em `TesteOpenApi`. |
| Oferta B115 | A captura mostrou `apiTeste_Metadata`, inventário de 5 Procedures, 17 SDTs próprios e 3 compartilhados; avisou que `Teste` é hierárquica e que, após recuperar, o Wizard bloqueará por falta de contrato de níveis. |
| Resultado da recuperação | Output B115: recuperou `apiTeste_Metadata`, GUID `17ae6165-e072-43e8-a339-017b6756eae0`, 2.346 bytes; 5 Procedures, 17 SDTs próprios e 3 compartilhados. A mensagem registra intenção importada e confirma que nenhum API Object, Procedure ou SDT foi alterado por B115. |
| Estado após recuperar | A segunda captura confirmou que a metadata foi recuperada e orientou reabrir o Wizard; nenhuma outra etapa foi executada nessa invocação. |
| Painel `Contrato reconstruído` | Captura da aba: indica `RestPath=/teste` reconstruído, `Services base path=apiTeste` como padrão fixo, mensagens BC `True` como padrão fixo, paginação `50/200`, filtros `4` e hierarquia reconstruída com todos os níveis padrão. A mensagem inferior bloqueia porque a Transaction tem subníveis e a metadata recuperada não contém `levels`; orienta Remover API gerada e nova geração. |
| Confirmações de escrita | SDTs, Procedures e API Object: caixas `Confirmar ... ao concluir` desabilitadas e `Confirmado para escrita: False`. Procedures: 5 gerenciadas/planejadas; API Object: 1 gerenciado/planejado; SDTs: 21 planejados. |
| Resumo | Ao abrir Resumo, Delete selecionado sem REST via Business Component abriu aviso de que nenhuma alteração foi feita. O aviso é distinto do bloqueio T2. |
| Cancelamento e leitura final | Depois do cancelamento, GetAll encontrou 28 Files e o direct-Get confirmou `apiTeste_Metadata`, FileId 116, GUID `17ae6165-e072-43e8-a339-017b6756eae0`, 2.346 bytes. B115 não foi oferecida novamente porque a metadata já registra o API Object atual. B034 confirmou que o Wizard descartou decisões, não criou ApiPlan e não fez alteração na KB nesta execução. |

Resultado: **PASS** para elegibilidade/recuperação B115 e para o bloqueio hierárquico T2 sem caixa de confirmação, com controles de escrita desabilitados. O cancelamento confirmou que a invocação do Wizard não iniciou Apply; a metadata B115 permaneceu na KB.

## Cenário 9 — metadata legada (V2 e V3 validadas; V1 não localizada)

- Inventário somente leitura via GeneXus18MCP: seis Files com nome contendo `_Metadata`. `apiLaudo_Metadata` e `apiProduto_Metadata` usam `GOAB_API_METADATA_B060_V2`; `apiEscola_Metadata` usa V3. As três não possuem chave `recovery` e possuem `errorDetail`; os GUIDs de API registrados correspondem às APIs existentes. Nenhuma V1 encontrada nesse inventário.
- Fixture exercitada na IDE: Transaction `Laudo`, API `apiLaudo`, GUID `ac52ab28-6817-405a-8ba7-f8a3cbe10489`; metadata GUID `05b8301d-c05c-44a3-b2eb-a81cf661ec9a`, schema V2, 33.098 bytes, FileId 80.
- Captura mostra abas começando em Serviços, sem `Contrato reconstruído`; mantenedor confirmou ausência do painel. Serviços List/Get/Create/Update selecionados e Delete desmarcado; nenhuma seleção foi alterada para este teste.
- Output B115: File existente e não produzido pela recuperação; recuperação não se aplica. B034: Wizard cancelado, decisões descartadas, nenhum ApiPlan criado e nenhuma alteração na KB.

- Fixture V3 exercitada na IDE: Transaction `Escola`, API `apiEscola`; direct-Get da metadata reencontrou FileId 85, GUID `bfdbd782-218d-485d-8877-5c01ddf9851a`, 174.670 bytes. O mantenedor confirmou ausência de `Contrato reconstruído`; não anexou captura nesta resposta. B115: File existente e não produzido pela recuperação; recuperação não se aplica. B034: nenhum ApiPlan criado e nenhuma alteração na KB após Cancelar.

Resultado: **PASS nas fixtures V2 Laudo e V3 Escola**. V1 sem `errorDetail` não foi exercitada porque nenhuma fixture V1 foi localizada na KB consultada; registrar como variante condicional não disponível, sem generalizar a evidência V2/V3 para V1.

## Cenário 10 — mensagens por causa (10a, 10b e 10c validados)

### 10a — BaselineServiceSourceHashMismatch

- Preparação autorizada via GeneXus18MCP: somente o RestPath de List em `apiLaudo` foi alterado de `/laudo` para `/laudo_b110_probe`. Backup da parte Methods exportado em `Temp/`. Prévia com uma ocorrência; releitura independente confirmou Methods conforme solicitado, Events/Variables e GUID preservados. A verificação exata inicial do MCP acusou diferença CRLF/LF, mas a releitura confirmou o conteúdo e o diagnóstico na IDE confirmou o hash alterado.
- Tela API Object e Output B087: `BaselineServiceSourceHashMismatch — O Source do API diverge do baseline da metadata. Use Remover API gerada e depois gere novamente pelo Wizard. Nenhuma escrita foi solicitada.`
- GUIDs da API e metadata coincidem: `ac52ab28-6817-405a-8ba7-f8a3cbe10489`. Metadata V2 presente, própria, parse/fingerprint/ownership válidos; baseline de versão, GUID, Description e descrições válido. Somente `BaselineServiceSourceHashOk=False`: hash atual `7F84E2979B5082627EDCE705C0E44E99C48C1519AA63F4A992E7DF203124A83F`, gravado `AFC7592811184FE51A388F7D4EE57C636540121EF11A9AAD7863B6D48B519BB7`.
- O mantenedor confirmou SDTs e Procedures desabilitados/desmarcados; textos das duas abas mostraram o conflito propagado, confirmação para escrita `False`, apesar do reencontro dos próprios artefatos (SDTs: 8 planejados no rótulo; Procedures: 4).
- B115 não ofereceu recuperação, porque a metadata existe e não foi produzida pela recuperação. B034 confirmou Cancelar sem ApiPlan e sem alterações pelo Wizard. Nenhum Remover ou Apply foi executado.
- Restauração via MCP após Cancelar: path List voltou a `/laudo`, Save/releitura confirmados, Methods iguais ao original e hash voltou exatamente a `AFC7592811184FE51A388F7D4EE57C636540121EF11A9AAD7863B6D48B519BB7`. Metadata antes/depois: 33.098 bytes, SHA-256 `4ED1F7F19C7068AEBCF9BC76036E7BFF8DF193A140A5E1C779C6095AA65C37CD`, inalterada.

- Reabertura após restauração: mantenedor confirmou API Object em `Reencontrar e validar`, sem conflito de Source, e confirmações de SDTs/Procedures habilitadas. Não anexou captura nesta resposta. Direct-Get confirmou metadata FileId 80, GUID `05b8301d-c05c-44a3-b2eb-a81cf661ec9a`, 33.098 bytes; B034 confirmou novo cancelamento sem ApiPlan e sem alteração na KB.

Resultado: **PASS para 10a**, incluindo mensagem específica, propagação do bloqueio, restauração via MCP e retorno da interface ao estado apto. As variantes de GUID divergente e B115 inelegível estão registradas em 10b e 10c abaixo.

### 10b — GUID divergente

- Preparação autorizada via GeneXus18MCP na KB `wsEducacaoSpTeste`: exportação/importação SDK de um único File, `apiLaudo_Metadata`, sem dependências. Backup XPZ original e JSON original preservados em `Temp/`; `import_part` não foi aplicado porque sua prévia acessa as propriedades XML do File, não o payload JSON.
- O JSON passou a registrar `11111111-2222-4333-8444-555555555555` nas três referências de identidade da API (ownership, inventário e baseline). Fingerprint recalculado pelo mesmo contrato Newtonsoft, `DateParseHandling.None` e snapshot compacto sem `fingerprint`: `5E454D6731D27A5406E4338429822A930777B61F8A433737A59D9038C57532F7`. Delta JSON limitado aos três GUIDs e ao fingerprint; envelope XPZ preservado, exceto `lastUpdate` atualizado para importação.
- Prévia SDK reconheceu exatamente um File, GUID `05b8301d-c05c-44a3-b2eb-a81cf661ec9a`. Importação real retornou sucesso; releitura independente do blob confirmou igualdade exata com o candidato: 33.098 bytes, SHA-256 `A151A49AC31CA3F8FB1EDF948834FB0CD509ED9DE590894B9B7C6586C8833198`.
- API Object permanece com GUID `ac52ab28-6817-405a-8ba7-f8a3cbe10489`, Description canônica e Methods originais, hash `AFC7592811184FE51A388F7D4EE57C636540121EF11A9AAD7863B6D48B519BB7`. O GUID e a Description do próprio File foram preservados. Esta preparação altera deliberadamente a metadata; não é evidência de escrita pelo Wizard.
- Capturas de API Object, SDTs e Procedures confirmaram `OwnershipSchemaApiNameOrGuidMismatch`, API atual `ac52ab28-6817-405a-8ba7-f8a3cbe10489` e metadata registrando `11111111-2222-4333-8444-555555555555`. A orientação cita os dois objetos a apagar para regenerar, reencontro de SDTs/Procedures e perda das escolhas que existiam na metadata. Não promete recuperação B115 para este conflito.
- Confirmações nas três abas desabilitadas/desmarcadas, com escrita `False`. A captura adicional de Resumo mostrou todas as seis etapas de escrita `False` e botão `Concluir teste`. Output B087 confirmou metadata própria e parseável, com ownership inválido; B115 não oferecida. B034 confirmou cancelamento sem ApiPlan e sem alteração pelo Wizard.
- `FingerprintPresente=False` e `IntegrityPresente=False` no diagnóstico não comprovam ausência dessas chaves no JSON: o ramo de ownership inválido retorna antes das respectivas avaliações. O payload preparado continha ambas; sua releitura exata foi registrada acima.
- Após Cancelar, restauração via MCP por importação de um único File, mantendo sua identidade e atualizando apenas o timestamp do envelope. Releitura confirmou os 33.098 bytes originais, igualdade byte a byte com o backup e SHA-256 `4ED1F7F19C7068AEBCF9BC76036E7BFF8DF193A140A5E1C779C6095AA65C37CD`.

- Reabertura após restauração: três capturas confirmaram API Object, SDTs e Procedures em `Reencontrar e validar`, sem conflito, com caixas habilitadas/marcadas e `Confirmado para escrita: True`. Dependências de Procedures/SDTs confirmadas nesta execução. Direct-Get manteve FileId 80, GUID `05b8301d-c05c-44a3-b2eb-a81cf661ec9a`, 33.098 bytes. B034 confirmou cancelamento, decisões descartadas e nenhum ApiPlan ou alteração pelo Wizard; marcar as caixas não foi seguido de Apply.

Resultado: **PASS para 10b**, com mensagem específica, bloqueio propagado, Resumo sem escrita, cancelamento, restauração exata do payload e reabertura apta documentada por capturas.

### 10c — B115 inelegível

- Preparação autorizada via MCP, sem exclusão: File `apiLaudo_Metadata` renomeado para `apiLaudo_Metadata_B110Backup`; prévia do rename encontrou zero referências e execução não alterou outros objetos. Releitura confirmou FileId 80, GUID `05b8301d-c05c-44a3-b2eb-a81cf661ec9a`, nome/path temporário e blob original de 33.098 bytes, SHA-256 `4ED1F7F19C7068AEBCF9BC76036E7BFF8DF193A140A5E1C779C6095AA65C37CD`. Consulta exata de File pelo nome original retornou zero correspondências.
- Description de `apiLaudo` alterada de `apiLaudo - by Genexus Open API Builder` para `apiLaudo - B110 teste externo`; Save e releitura confirmados. GUID da API preservado e Methods mantiveram hash `AFC7592811184FE51A388F7D4EE57C636540121EF11A9AAD7863B6D48B519BB7`.
- Resultado na IDE: mantenedor confirmou ausência da oferta B115. As três capturas mostraram API Object bloqueado por `MetadataMissing`, conflito propagado para SDTs e Procedures, caixas desabilitadas/desmarcadas e confirmação para escrita `False`.
- Output: duas passagens de 28 Files, zero correspondências para `apiLaudo_Metadata`; B115 não oferecida porque `API Object 'apiLaudo' não tem a Description própria da extensão; recuperação recusada.` A mensagem de MetadataMissing informa que a oferta depende da elegibilidade B115, sem garanti-la. B034 confirmou cancelamento sem ApiPlan e sem alteração pelo Wizard.
- Restauração autorizada via MCP: File voltou a `apiLaudo_Metadata` e API voltou à Description `apiLaudo - by Genexus Open API Builder`; zero referências afetadas pelo rename. Releitura confirmou GUIDs originais e blob de 33.098 bytes com SHA-256 original `4ED1F7F19C7068AEBCF9BC76036E7BFF8DF193A140A5E1C779C6095AA65C37CD`. Não houve exclusão ou regeneração. Reabertura após esta restauração ainda não exercitada.

Resultado: **PASS para 10c**. As três variantes dedicadas do cenário 10 foram exercitadas e as alterações de preparação restauradas pelo MCP.

## Cenário 11 — preservação nos bloqueios T2 hierárquico e T1 com Procedure externa

- Fixture sem nova alteração de KB: Transaction hierárquica `Teste`, metadata recuperada com `recovery.imported=true` e sem `levels`; bloqueio T2 já observado no cenário 8. O mantenedor abriu e cancelou o Wizard, sem Apply, edição ou recuperação.
- Baseline capturado via exportação SDK sem dependências: 30 objetos distintos — 17 SDTs próprios, 3 compartilhados, 5 Procedures, API Object, metadata, Folder `TesteOpenApi`, Transaction e diário global. Hash por XML completo de cada Object, incluindo identidade, propriedades, partes e `lastUpdate`, salvo em `Temp/B110-11-snapshot-before.json`; a comparação posterior usou os mesmos 30 GUIDs. A consulta indexada também retornou o antigo `sdtTeste_API_ListResponse`, mas o export vivo não o encontrou; ele não foi contado como objeto existente nem incluído no baseline.
- BC confirmado diretamente no XML da Transaction: `idISBUSINESSCOMPONENT=True`. Metadata: 2.346 bytes, SHA-256 `90BC78A02CF78EC9F7605A6118C8BC7C092AA80DC578051F99735A11BF642CB5`. Diário: 5.464 bytes, SHA-256 `70B6DE5A9ACC758848C9EEF4F569BF7117E5A3DA1566A9EEB77D45AC15288295`, OperationId `15da3305-8373-496a-9856-23a32341fb26`, Transaction `Carga`; presença de diário anterior não significa início de nova operação.
- Captura e Output da rodada: painel `Contrato reconstruído` bloqueado por subníveis sem `levels`, sem caixa de confirmação; B115 não oferecida porque a metadata já registra a API atual. B034 confirmou cancelamento sem ApiPlan e sem alteração pelo Wizard.
- Exportação posterior reencontrou os mesmos 30 objetos. Comparação por GUID, nome, tipo e SHA-256 do XML completo de cada Object: zero diferenças, inclusive partes, propriedades, identidade e `lastUpdate`. BC permaneceu `idISBUSINESSCOMPONENT=True`. Metadata e diário também foram comparados byte a byte, com igualdade e hashes anteriores preservados; nenhum novo diário substituiu a operação anterior de Carga.
- A primeira execução do comparador apresentou diferenças de representação de `lastUpdate` por coerção de datas no `ConvertFrom-Json`, apesar de todos os hashes XML já iguais. O comparador foi corrigido para comparar o hash do XML completo, que já inclui o timestamp original, sem excluir conteúdo do objeto; a conferência independente dos dois snapshots confirmou zero diferenças.

Resultado: **PASS de preservação nos bloqueios T2 hierárquico e T1 com Procedure externa**: o segundo recebeu comparação independente antes/depois no cenário 12. Nos demais bloqueios exercitados, as saídas B034 confirmaram cancelamento sem ApiPlan e sem escrita solicitada pelo Wizard, mas não houve comparação integral independente de todos os objetos. A exigência transversal do cenário 11 é aceita com esse alcance declarado, sem extrapolar os dois snapshots para as execuções anteriores.

## Cenário 12 — API ausente e Procedure externa homônima

- Fixture: Transaction plana `Distribuidora`, GUID `cc00ab74-6e67-477d-a815-62690464c047`, com chave `DistribuidoraId` e campo `DistribuidoraNome`. Leitura direta SDK confirmou ausência de `apiDistribuidora`, `apiDistribuidora_Metadata` e Folder `DistribuidoraOpenApi`.
- Preparação autorizada via MCP: criada apenas Procedure temporária `procDistribuidora_API_List`, GUID `bb4a45d7-2893-4011-95ae-c459c711290b`, Source com comentário de teste e sem código executável, Description padrão `proc Distribuidora_API_List`, sem sentinela de posse da extensão. Não houve Specify/build. Ela foi removida via MCP depois do cancelamento e comparação, por ter sido criada somente para este cenário.
- Baseline pós-preparação: seis objetos exportados sem dependências — Procedure temporária, Transaction, três SDTs compartilhados e diário global. Hash do XML completo por GUID em `Temp/B110-12-snapshot-before.json`. BC da Transaction já era `idISBUSINESSCOMPONENT=True`; sua habilitação não foi alterada. Comparação após Cancelar e conferência de não criação de API/metadata/Folder/SDTs/Procedures próprias pendentes, integrando a exigência transversal do cenário 11.
- Capturas: Procedures bloqueadas com uma colisão identificada por `procDistribuidora_API_List`, tipo Procedure, Root Module, sem Folder. SDTs exibiram estado local `Completar` (3 gerenciados, 5 ausentes, 8 planejados), mas a barreira global desabilitou/desmarcou a confirmação; conflito da Procedure propagado e escrita `False` nas duas abas.
- Output confirmou API dependente bloqueada por SDTs/Procedures, metadata ausente, B115 não oferecida porque `apiDistribuidora` foi encontrado zero vezes. B034 confirmou cancelamento sem ApiPlan e sem alteração pelo Wizard.
- Comparação pós-cancelamento dos seis objetos: GUIDs, nomes e hashes do XML completo iguais, incluindo Source da Procedure, Transaction/BC, SDTs compartilhados e diário. Consulta de Distribuidora não encontrou novos SDTs/Procedures próprios; leituras diretas confirmaram API, metadata e Folder ainda ausentes. Isso estende a prova transversal do cenário 11 ao bloqueio T1 com API ausente.
- Limpeza da preparação via MCP: prévia de exclusão encontrou zero referências e resolveu exatamente o GUID temporário `bb4a45d7-2893-4011-95ae-c459c711290b`; exclusão real usou esse estado via token de concorrência e retornou persistência/releitura confirmadas, sem ações implícitas de ciclo de vida. Somente a Procedure criada para este teste foi removida.

Resultado: **PASS para cenário 12**, com bloqueio correto, preservação antes/depois e remoção da fixture temporária.

## Cenário 13 — transições da interface

- Preferências conferidas por leitura do File `GxOpenApiBuilder_Settings` via MCP: as seis opções de geração/conclusão, incluindo `applyList` e `applyBusinessComponent`, estavam `true`. O File não foi alterado.
- `Teste`, com contrato recuperado T2 hierárquico sem possibilidade de confirmação: capturas das abas Business Component e List mostraram as duas caixas desabilitadas e desmarcadas. O Output B034 confirmou Cancelar sem ApiPlan e sem alteração na KB.
- `Carga`, com metadata recuperada T2 plana: capturas antes da confirmação do contrato mostraram Business Component e List desabilitados e desmarcados. O mantenedor marcou a confirmação, verificou que ambas ficaram habilitadas e marcou a conclusão da listagem; ao desmarcar a confirmação, ambas voltaram a ficar desmarcadas. Repetiu a marcação da confirmação e verificou a reabilitação, depois cancelou. As transições após a confirmação foram relatadas pelo mantenedor, sem capturas adicionais. O Output B034 confirmou Cancelar sem ApiPlan e sem alteração na KB.
- O percurso de `Laudo` nos cenários 9/10a já havia demonstrado estado apto, bloqueio real do API Object após divergência do Source e retorno a apto após restauração, em aberturas separadas. Na condição bloqueada, SDTs e Procedures ficaram desabilitados; após restauração, habilitados.

Resultado: **PASS para cenário 13**, combinando o bloqueio real de API Object em `Laudo`, as caixas BC/List explicitamente bloqueadas sob T2 em `Teste` e `Carga`, e as transições de confirmação T2 dentro da mesma abertura de `Carga`. A transição por alteração externa do Source não foi feita dentro da mesma invocação modal.

## Cenário 14 — mensagem de recuperação de operação interrompida (sem fixture na IDE)

- Leitura do File `GxOpenApiBuilder_OperationJournal` via MCP: operação `Apply` de `Carga` em `operationState=Completed`, `logicalStage=Completed`, `journalDurability=Confirmed`. A KB não apresentou diário interrompido para abrir o diálogo exigido pelo cenário.
- Leitura do fluxo: `RunRecovery` usa o resumo de `ApiPlanRecoveryRehydrator` e, para `Discard` após gravação parcial, pergunta com `RecoveryConfirmDiscard`. Ambos dizem para abrir o Wizard para **avaliar o estado atual conforme metadata e API Object**, sem prometer reaplicação nem recuperação do contrato original.
- Gate local `Tests/OperationJournal/Test-ApiPlanRecovery.ps1` passou. Ele exercita o `Apply` parcial com recibo, exige `NextStep=Discard` e verifica que o resumo não contém «reaplicar pelo Wizard».

Resultado: **comportamento textual validado offline; apresentação na IDE não exercitada**. Em 2026-09-27, o mantenedor aceitou explicitamente encerrar a B110 com essa exceção de campo. O aceite não transforma o teste offline em observação na IDE. Fabricar um diário interrompido na KB ativa apenas para mostrar o diálogo alteraria o mecanismo de recuperação e não foi feito.

## Cenário 15 — nome da API existente somente leitura

- Com `apiTeste` apta, a aba Paths mostrou o nome da API somente leitura e a explicação de que a API gerada não pode ser renomeada (cenário 4).
- Com `apiContratoB110` existente e `MetadataMissing`, a aba Paths mostrou o nome resolvido e a mesma explicação; não houve tentativa de edição do campo (cenário 1).
- Leitura do código da interface: quando o contrato existente resolve um `ApiGuid`, o Wizard atribui `ResolvedApiName` ao único campo `_apiNameText` e define `ReadOnly=true` em `PrototypeWizardDialog.cs`. O teste offline `Test-ApiPlanExistingNamePolicy.ps1` cobre a recusa de nome planejado diferente; a recusa de backend não foi provocada pela interface, que impede a edição.

Resultado: **PASS para cenário 15** pela observação dos dois estados na IDE, leitura do controle e teste offline da guarda de backend. Não houve tentativa manual de digitar no campo somente leitura.

## Observações e limites

- A tela de Teste mostrou 21 planejados sob “SDTs” e avisou 26 objetos entre SDTs e Procedures. O Output registrou 20 SDTs e 5 Procedures. Na leitura estática feita ao redigir este registro, o estado exibido para SDTs soma também a pasta da Transaction; isso explica a diferença numérica, mas torna o rótulo “SDTs” impreciso. Nenhum vigésimo primeiro SDT foi observado no Output.
- Os avisos de idioma apareceram nos dois cenários. Não foram tratados como falha de geração; a validação pública do idioma da KB não fez parte desta sessão.
- O aviso sobre a pasta TesteOpenApi descreve a reutilização e uma consequência possível de remoção quando a pasta ficar vazia. Nenhuma remoção foi feita ou testada.
- A proposta de unificar Paths e API Object foi registrada verbalmente pelo mantenedor para discussão depois da bateria. Nenhuma alteração de interface foi feita neste registro.
- Estudo de UX sobre as abas SDTs e Procedures registrado como `B134` em `Docs/Foundation/06-BACKLOG_v0.1.md`. Observação de campo: desmarcar SDTs também desmarca Procedures; a decisão entre agrupar as etapas ou manter abas separadas com a dependência explícita ainda não foi tomada. Necessidade de espaço na tela não foi medida.
- No cenário 1, a recuperação B115 não foi oferecida porque procurou `apiContrato`, enquanto o API Object existente se chama `apiContratoB110`. O Output registra a divergência; não foi tratada como falha do cenário T1.

## Limites aceitos desta bateria

- Cenário 9: V2 Laudo e V3 Escola validadas; V1 sem `errorDetail` não localizada entre os seis Files de metadata inventariados na KB, variante condicional não exercitada.
- Cenário 10: 10a, 10b e 10c concluídos, com restauração via MCP. Reabertura apta confirmada após 10a/10b; após 10c não foi repetida, pois a matriz pede a mensagem específica e a restauração foi conferida diretamente no MCP. No cenário 1, a explicação de Paths foi observada, mas o campo não foi submetido a uma tentativa de edição; a condição `ReadOnly` do controle foi conferida no código para o cenário 15.
- Cenário 14: texto validado offline, mas sem exibição do diálogo na IDE porque o diário atual está concluído; o mantenedor aceitou essa exceção em 2026-09-27.
- A contagem de Folder junto aos SDTs é uma questão de rótulo da interface, registrada para avaliação separada em `B134`; não altera o bloqueio de escrita medido nesta bateria.
- A aceitação da bateria é qualificada pelos limites acima. As remissões documentais e os gates locais do plano foram conferidos no fechamento da frente; a rotina pré-push tem relatório próprio e não transforma o cenário 14 em prova de IDE.
