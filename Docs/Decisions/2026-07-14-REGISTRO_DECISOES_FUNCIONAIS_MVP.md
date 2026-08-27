# Registro de decisões funcionais do MVP — entrevista de revisão de 2026-07-14

## Finalidade deste documento

Este documento preserva as decisões aceitas na entrevista funcional realizada antes da implementação. Ele não é uma transcrição literal da conversa: é o registro consolidado que serve como fonte primária das decisões funcionais do MVP.

Os documentos em `Docs/Foundation` devem materializar essas decisões nos contratos organizados por assunto e não podem contradizê-las.

## Autoridade documental e precedência

A consolidação documental de julho de 2026 foi formalmente concluída em 2026-07-15. Este registro permanece a fonte primária das decisões funcionais do MVP; os documentos `Foundation` materializam seus contratos por assunto.

Uma validação técnica posterior pode exigir revisão de uma decisão. Nesse caso, a mudança deve ser registrada explicitamente neste documento ou em registro sucessor, com atualização dos documentos `Foundation` afetados.

## Estado da revisão

- Data: 2026-07-14.
- Situação: entrevista funcional e consolidação documental do MVP concluídas.
- Consolidação documental: auditada em 2026-07-14 e formalmente encerrada em 2026-07-15.
- Próxima etapa vigente: consultar o [checkpoint operacional](../STATUS_ATUAL_E_PROXIMO_PASSO.md).
- Implementação funcional: ainda não iniciada; a preparação técnica mínima de build foi concluída em B010.

## Emenda técnica — 2026-07-16

### Fato oficial que motivou a emenda

A documentação oficial informa que o instalador do Platform SDK foi descontinuado a partir de GeneXus 18 Upgrade 14. Para U14+, as assemblies de referência são pacotes NuGet do feed GeneXus Azure Artifacts e os tipos de projeto são MSBuild SDKs. Esse fato substitui a premissa anterior de que o projeto deveria localizar DLLs do SDK sob a instalação local.

### Decisão de produto e manutenção

O mantenedor aprovou U14+ como baseline de compatibilidade do MVP, mantendo U15 como primeiro ambiente disponível de validação. A razão é operacional: U14 é o primeiro upgrade coberto pelo mecanismo oficial moderno, permitindo uma única cadeia versionada de feed, SDKs e lockfile.

U13 e anteriores não foram considerados tecnicamente impossíveis. Eles podem exigir o Platform SDK legado, mas incluí-los significaria manter, documentar e testar uma segunda cadeia de build e empacotamento. Esse custo não foi aceito para o MVP. O baseline U14+ é, portanto, decisão de escopo reversível pelo mantenedor, não afirmação de incompatibilidade de U13.

**Emenda técnica de 2026-08-13 — satélite `Gx18u13`:** o parágrafo acima permanece válido para o baseline canônico do MVP (U14+). A Opção B / Fase 2 entregou uma segunda cadeia satélite paralela, sem promover U13 a baseline nem anular a decisão de 2026-07-16; ver `Emenda técnica — 2026-08-13` abaixo.

### Limite da evidência

A build comprovou o mecanismo moderno no repositório, não o carregamento da extensão em U14 ou U15. Assim, “U14+” significa alvo de produto e de teste pendente; suporte prático só poderá ser anunciado após o `B000`. A diferença U15 relativa a chamadas de `API Object` a partir de eventos Angular está fora do escopo do MVP e não determina o baseline.

A emenda preserva o objetivo funcional — extensão dentro da IDE que gera objeto `API` nativo — e exige que o `B000` torne o pacote minimamente descobrível e carregável antes da instalação manual.

## Emenda técnica — 2026-08-13

### Fato que motivou a emenda

A Frente de suporte paralelo `Gx18u13` (D43, Opção B) entregou solution, projeto, referências pinadas, instalador satélite e inventário offline; a DLL foi compilada em Release e validada manualmente no GeneXus 18 U13. Isso materializa a segunda cadeia de build e empacotamento que a emenda de 2026-07-16 descrevia como custo não aceito *no baseline do MVP*.

### Decisão de produto e manutenção

O baseline canônico do MVP permanece U14+ via feed NuGet e MSBuild SDKs. A linha `Gx18u13` é suporte paralelo de manutenção, não revisão do baseline nem anúncio de U13 como alvo oficial do produto. O custo da segunda cadeia foi aceito nesse recorte paralelo; a decisão de escopo de 2026-07-16 continua reversível e não foi revertida.

### O que a emenda não altera

Permanecem válidos o mecanismo moderno como caminho canônico, U15 como ambiente principal do mantenedor e a distinção entre existência técnica de uma extensão em U13 e inclusão de U13 no baseline do MVP. Evidências: `Docs/Decisions/2026-08-12-PLANO_SUPORTE_PARALELO_GX18U13_OPCAO_B.md`, `Docs/Implementation/2026-08-12-FASE2-SATELITE-GX18U13.md` e a nota de revisão de 2026-08-13 em `Docs/Implementation/B010-SDK-E-BUILD-MINIMO.md`.

## Emenda técnica — 2026-08-20 — Suporte a Transactions com subníveis

### Fato que motivou a emenda

A análise da KB de produção (`Gx_FabricaBrasil`) revelou que 10,2% das Transactions possuem múltiplos níveis (com até 13 subníveis paralelos e até 3 níveis de profundidade). A restrição inicial do MVP que limitava a geração estritamente ao 1º nível impedia o uso da extensão em casos centrais do negócio (como pedidos, notas fiscais, tributações e regras operacionais compostas).

### Decisão de produto e arquitetura

Fica autorizada a expansão do gerador para suportar Transactions com subníveis (B095–B099):
1. **Elegibilidade intra-subnível:** Atributos de subníveis selecionados passam a ser elegíveis e entram como coleções aninhadas nos SDTs de `CreateRequest`, `UpdateRequest` e `Response`. Regras `NoAccept`, fórmulas de linha e atributos inferidos permanecem desabilitados em Requests (somente leitura) e elegíveis em `Response`. Chaves primárias de subnível identificam a linha no `UpdateRequest` e são opcionais/omitidas no `CreateRequest` quando autonumeradas/sequenciais.
2. **Listagem:** O endpoint `List` preserva alta performance paginando sobre o cabeçalho e incluindo contadores numéricos `<Subnível>Count` (ex.: `ParcelasCount`, `ItensCount`), sem aninhar coleções completas na listagem geral.
3. **Substituição completa no `Update` e atomicidade:** O `PUT` em transação multinível adota substituição idempotente completa da coleção de linhas no Business Component. O `Save()` do BC governa a gravação em transação única atômica (executando `Commit` em caso de sucesso e `Rollback` em caso de falha).
4. **Contrato de erros:** Falhas de validação em subníveis retornam o `sdt_API_ErrorResponse` top-level unificado com `Code = "validation_error"` e a `Message` emitida pelo BC, sem array paralelo por linha.
5. **Detalhamento:** Especificação, levantamento e fases de implementação registrados em `Docs/Implementation/2026-08-20-SUPORTE-TRANSACTIONS-SUBNIVEIS.md`.

**Nota de revisão — 2026-08-23.** Os itens 2 e 3 foram revistos pela `Emenda técnica — 2026-08-23`: os contadores de `List` passam a ser desativáveis por subnível e restritos a subníveis diretos, e a substituição completa no `Update` passa a exigir o marcador explícito `<Subnível>Replace`. O item 4 permanece como decisão vigente, e a `Emenda técnica — 2026-08-23` registra que a implementação ainda não o cumpre — gap tratado em `B102`.

**Remissão — 2026-08-24.** O item 4 desta emenda (contrato de erros / `Message` do BC) passou a ser cumprido pelo fechamento de `B102`; ver `Emenda técnica — 2026-08-24`.

## Emenda técnica — 2026-08-23 — Revisão dirigida da Sprint 9

### Fato que motivou a emenda

A revisão do plano da Sprint 9, conduzida em 2026-08-23 sobre a especificação de subníveis, o backlog e o código do gerador, encontrou decisões ausentes em pontos que atravessam subsistemas já entregues (reencontro, integridade, remoção governada, releitura de contrato existente), além de uma decisão registrada que a implementação não cumpre. As decisões abaixo consolidam o resultado.

### Decisões de contrato e arquitetura

1. **Forma e nomenclatura dos SDTs de subnível.** Cada subnível selecionado gera um SDT próprio **por contrato**, referenciado como membro coleção, nomeado `sdt<NomeBase>_API_<Papel>_<Subnível>` — o papel permanece imediatamente após `_API_` e o subnível é qualificador, de modo que cada contrato forme um bloco contíguo na Folder (`sdtCliente_API_Response`, `sdtCliente_API_Response_Parcelas`). Um SDT único compartilhado publicaria campos somente leitura no `CreateRequest`, defeito da mesma natureza do `Errors[]` retirado pela `Emenda técnica — 2026-08-03`. Subestrutura aninhada dentro do próprio SDT fica descartada nesta frente.
2. **Substituição no `Update` sob marcador.** A substituição completa passa a exigir `<Subnível>Replace = True`; ausente ou `False` preserva as linhas. A decisão decorre da limitação comprovada em 2026-08-03 — ausente é indistinguível de vazio no corpo —, que sem marcador faria um `PUT` incompleto apagar linhas em silêncio. O default seguro é obtido justamente porque booleano ausente equivale a `False`.
3. **Ordem das linhas.** Declarada contratualmente como a da chave primária do subnível, ascendente. No `Update`, a ordem do payload é irrelevante, salvo chave autonumerada ou sequencial, em que a ordem determina a numeração atribuída.
4. **Contadores de `List`.** Fórmula agregada nativa, ligados por padrão, desativáveis por subnível e restritos a subníveis diretos. Contador de neto seria soma achatada atravessando os pais, com nome que não denunciaria a agregação.
4-A. **Tipo dos elementos de `Items` na listagem.** Em transação com subnível selecionado, `Items` passa a ser coleção de `sdt<NomeBase>_API_ListResponse_Item` — mesmos campos de cabeçalho, sem os membros de coleção, com os contadores. Em transação de nível único, `Items` continua sendo coleção de `sdt<NomeBase>_API_Response`, sem alteração alguma. O motivo é que o `List` não preenche as coleções: reusar o `Response` publicaria arrays permanentemente vazios que o consumidor leria como ausência de linhas. Fica revista, apenas para o caso com subníveis, a regra de que `Get`, `Create`, `Update` e cada item de `List` usam o mesmo contrato.
5. **Profundidade.** Recursão genérica sem limite artificial no código; profundidade 3 é o alcance da evidência, sinalizado por aviso no Wizard acima desse valor, sem bloquear a geração.

**Remissão — 2026-08-26 / 2026-08-27.** O alcance da evidência de **validação** passou a **4** após o smoke U15 na Transaction `Teste` (`ValidatedDepth = 4`; aviso do Wizard acima de 4). A profundidade 3 permanece o máximo observado no levantamento da KB de produção. Ver §8-A de `Docs/Implementation/2026-08-20-SUPORTE-TRANSACTIONS-SUBNIVEIS.md` e `Docs/Implementation/2026-08-26-B099a-WIZARD-HIERARQUICO.md`.

6. **Metadata.** `schemaVersion` passa a `V2`; a leitura aceita `V1` e `V2`, e a conversão ocorre apenas quando a geração é aplicada. Sem tolerância, toda API da Alpha ficaria irreencontrável e irremovível.
7. **Não regressão.** Alterações de gerador nesta sprint não podem mudar a saída de transações de nível único, verificada contra linha de base capturada antes da frente.
8. **Não escopo.** Subníveis não recebem endpoints próprios.
9. **Serviço `Delete` (`B100`).** Fica autorizado como frente própria, opt-in e desligado por padrão: `200` com a chave removida, `404` em inexistente, `422` com `validation_error` em recusa do BC, inclusive por integridade referencial. Distinguir conflito referencial de recusa por regra exigiria classificar erro pelo texto da mensagem, contrariando a regra de decidir por `Code` e nunca por texto.
10. **`Message` do `422` (`B102`).** A decisão de 2026-07-14 — `Message` como texto legível produzido pela aplicação, sem tradução pela extensão — permanece vigente e **não** é revista. Registra-se que a implementação atual não a cumpre: o gerador emite texto fixo e descarta as mensagens do BC, de modo que uma rule `error` da KB não chega ao consumidor. `B102` implementa o repasse, com opção de desligar para API exposta publicamente.

**Remissão — 2026-08-24.** A afirmação de que a implementação não cumpria a decisão 10 ficou datada nesta emenda; o gap fechou em `B102` — ver `Emenda técnica — 2026-08-24`.

### O que a emenda não altera

Permanecem válidos o corpo de erro top-level com `Code` e `Message` sem array por linha, a atomicidade do `Save()` do Business Component governando `Commit` e `Rollback`, a elegibilidade intra-subnível da emenda de 2026-08-20 e a regra de que clientes decidem por `Code`, nunca pelo texto de `Message`.

**Nota de revisão — complemento de 2026-08-23.** A decisão 10 desta emenda foi ampliada, e a afirmação de que o corpo de erro permanece sem array passou a ser condicional: ver a `Emenda técnica — 2026-08-23 (complemento)`, logo abaixo.

## Emenda técnica — 2026-08-23 (complemento) — Revisão do plano de trabalho da Sprint 9

### Fato que motivou a emenda

Na mesma data, uma segunda revisão examinou o **plano de trabalho** da Sprint 9 — e não o desenho da frente de subníveis, já tratado pela emenda anterior. Ela encontrou uma fase fundacional sem mecanismo executável, um item de sprint sem especificação, gates que não cobriam dois dos entregáveis, e o contrato OpenAPI publicado fora do plano. As decisões abaixo consolidam o resultado.

### Decisões

1. **Forma do corpo de erro, agora condicionada a experimento.** A `Emenda técnica — 2026-08-03` retirou `Errors[]` depois que a IDE recusou uma tentativa com **subestrutura aninhada** dentro do próprio SDT (`sdt_API_ErrorResponse.Error`). Coleção tipada por um SDT **separado** é mecanismo distinto — o mesmo de `ListResponse.Items` — e nunca foi testado no corpo de erro. `B102` executa o experimento: aceito, o corpo ganha o membro coleção `Messages` tipado por `sdt_API_ErrorMessage`, preenchido a partir de `GetMessages()`; recusado, as mensagens vão concatenadas por `" | "`. Em ambos os casos `Message` permanece top-level e preenchido, e nenhuma das formas correlaciona mensagem com índice de linha.

**Remissão — 2026-08-24.** O experimento da decisão 1 fechou com coleção aceita; ver `Emenda técnica — 2026-08-24`.

2. **Tipo e limite da `Message`.** Passa a `LongVarChar`, com truncamento explícito pela geração em cerca de 2K. Tipo sem limite não é conteúdo sem limite: um Business Component com muitas rules produziria corpo de erro arbitrariamente grande. Somente mensagens de **erro** são repassadas.
3. **Default do repasse.** Ligado, com aviso quando `SecurityLevel = None`, e desligável por KB e por API. A decisão de 2026-07-14 sobre a `Message` continua sendo a norma; desligado por padrão perpetuaria o descumprimento para quem não conhece a opção.
4. **Escolha do chamador (`B105`).** O consumidor pode **restringir** o detalhe do erro abaixo do default da API, nunca ampliá-lo. Sem teto, a opção de desligar seria contornável pelo cliente.
5. **Propagação do marcador de substituição entre níveis.** Com `<Pai>Replace = True`, cada linha de pai enviada tem seus filhos regidos pelo `<Filho>Replace` daquele item; pai omitido é removido com os filhos; pai novo insere os filhos enviados. Com `<Pai>Replace` ausente ou `False`, a coleção do pai é ignorada por inteiro, inclusive os marcadores internos. Não existe caminho para alterar netos sem assumir a substituição do nível pai, e a limitação é declarada em vez de contornada por merge parcial que o Business Component não sustenta.
6. **Caminho no erro de campo obrigatório de linha** usa índice **base 0**, o do JSON enviado pelo cliente, e não o da coleção GeneXus.
7. **Linha de base de não regressão.** Duas camadas: comparação automática, no checker, de Source das Procedures, Service Source do API Object e plano de SDT; e conferência manual, por export XPZ, da forma física dos SDTs. Recaptura somente em commit isolado, contendo apenas os arquivos de referência e a justificativa.
8. **Gates da sprint.** O repasse da `Message` e o serviço `Delete` passam a ter gate próprio, com HTTP real nos dois environments — o pipeline REST já divergiu por gerador antes.
9. **Contrato OpenAPI publicado.** A trava mecânica passa a reconhecer os schemas derivados, o YAML multinível é conferido ao fim da Fase 4 e a geração de cliente é repetida como evidência pontual. Limitação do gerador nativo não bloqueia a sprint: vira documentação.
10. **Publicação em três cortes**, com os dois assets DLL em cada um, desacoplando o `Delete` do corte de subníveis.

### O que a emenda não altera

Permanecem válidas a regra de que o cliente decide por `Code` e nunca pelo texto de `Message`, a ausência de array de erros **por linha**, a atomicidade do `Save()`, a elegibilidade intra-subnível, a semântica de `Required` como preenchimento fixada em 2026-08-03 e a recusa em traduzir mensagens do Business Component.

## Emenda técnica — 2026-08-24 — Fechamento de `B102` (contrato de erro HTTP 422)

### Fato que motivou a emenda

Em 2026-08-24 o experimento da coleção tipada por SDT separado foi aceito na IDE e o gate HTTP de `B102` fechou nos dois environments da KB `wsEducacaoSpTeste` (`apiTeste`). A geração passou a cumprir a decisão de 2026-07-14 sobre `Message` legível produzida pela aplicação, e o corpo de erro ganhou `Messages[]` sem reintroduzir `Errors[]` nem `Field`. Evidência: documento 27 e `Docs/Implementation/2026-08-24-B102-EXPERIMENTO-E-GATE-HTTP.md`.

### Decisões

1. **Forma do corpo.** `sdt_API_ErrorResponse` contém `Code`, `Message` top-level e o membro coleção `Messages` tipado por `sdt_API_ErrorMessage`, preenchido a partir de `GetMessages()`. O ramo de concatenação como forma única **não** se aplica. `Message` permanece preenchida, concatenada por `" | "`, para não quebrar consumidores da Alpha. Nenhuma das formas correlaciona mensagem com índice de linha de subnível.
2. **Tipo e limite.** `Message` (top-level e no item de `Messages[]`) é `LongVarChar` com `Length = 2097152` na declaração ao SDK; a geração trunca visivelmente em cerca de 2K com reticência final (`SubStr` + `...`). O YAML nativo **não** emite `maxLength`.
3. **Filtro.** Só mensagens de **erro** do Business Component entram no corpo (`MessageTypes.Error` / `Type == 1`). `Msg()` fica de fora. `Field` permanece fora do contrato entregue.
4. **`Messages[].Code`.** Preserva o identificador da mensagem do BC quando existir; sem identificador, usa `business_rule`. O `Code` principal do envelope continua `validation_error`.
5. **Default do repasse.** Ligado por padrão, com aviso quando `SecurityLevel = None`, desligável por KB e por API. Desligado, o corpo volta ao texto genérico `"Business rules rejected the request."` sem chamar `GetMessages()`.
6. **Decisão por `Code`.** Clientes continuam decidindo por `Code`, nunca pelo texto de `Message`.

### O que a emenda não altera

Permanecem válidas a ausência de array de erros **por linha**, a atomicidade do `Save()`, a elegibilidade intra-subnível, a semântica de `Required` como preenchimento, a recusa em traduzir mensagens do BC e a retirada definitiva de `Errors[]` como subestrutura aninhada.

### Remissões

Ficam remidos, quanto ao estado de implementação, os trechos das emendas de 2026-08-20 e 2026-08-23 que registravam o gap de `B102` ou o experimento ainda aberto, e a afirmação da `Emenda técnica — 2026-08-03 — contrato OpenAPI publicado` de que o SDT compartilhava apenas `Code` e `Message` — válida naquela data; desde esta emenda o SDT inclui também `Messages[]`.

## Emenda técnica — 2026-08-03

### Experimento previsto que motivou a emenda

Este registro exigia, em dois pontos, um experimento técnico antes da implementação: confirmar como distinguir, por recursos nativos do GeneXus, um membro ausente de um membro presente com valor vazio, `false` ou `0`. O experimento foi executado no fechamento de `B071`-`B073`/`B079` e o resultado obriga a separar dois casos que o registro tratava como um só.

### Filtros de `List` — decisão preservada

Nos filtros opcionais de `List`, a distinção foi obtida com recursos nativos e permanece exatamente como decidido. O SDT writer grava os membros nullable de `ListFilters` com a propriedade GeneXus `idJsonInclude=idJsonJsonNull`, correspondente a `Json Null Serialization = JSON null`. Sem ela, membro numérico não informado serializa como `0` e indicaria falsamente filtro aplicado. `B070`/`B077` comprovou o comportamento em runtime: sem filtro, `AppliedFilters` traz o membro nulo; com filtro, traz o valor informado.

### Membros do corpo de `Create` e `Update` — decisão revista

No corpo das requisições, a distinção não é obtenível sem comando `csharp`. Quatro caminhos foram testados e descartados na IDE: comando `csharp` com `IsDirty`, que emite `spc0087` e foi recusado por decisão do projeto; `HttpRequest.ToString()` dentro da Procedure, onde o corpo bruto não chega; `&Sdt.IsDirty()` nativo, que não existe na linguagem, confirmado por IntelliSense e documentação; e `HttpRequest.ToString()` no evento `Before` do API Object, que devolveu `len=0` nos dois geradores porque o corpo já foi consumido pelo pipeline REST.

A decisão revista: `Required` passa a significar preenchimento, não presença do membro JSON. A geração compara cada campo obrigatório com o valor default do mesmo membro em instância vazia do próprio SDT de request, o que dispensa ramificar por tipo de dado. `Create` e `Update` respondem `400 Bad Request` quando o obrigatório chega ausente **ou** com o valor default do tipo — vazio, `false` ou `0`.

Ficam revistos por esta emenda os trechos correspondentes de `sdtNomeDaTransacao_API_CreateRequest`, de `sdtNomeDaTransacao_API_UpdateRequest` e o gate técnico transversal 6.

### Limitação assumida

Campo obrigatório cujo valor legítimo seja igual ao default do tipo é recusado com `400`. A limitação foi aceita explicitamente em lugar de introduzir comando `csharp` na geração.

### O que a emenda não altera

Permanecem válidas a ausência de campos auxiliares públicos com sufixo `Specified` no contrato, a regra de que membro opcional ausente não é atribuído ao BC, e o tratamento de valores vazios, `false` e `0` como valores realmente enviados quando o membro é preenchido. Os documentos `Foundation` 06, 09, 15 e 24 foram atualizados na mesma data.

## Emenda técnica — 2026-08-03 — contrato OpenAPI publicado

### Fato que motivou a emenda

A conferência do YAML gerado pelo GeneXus, feita depois do fechamento de `B071`-`B073`/`B079`, mostrou divergências entre o contrato publicado e o comportamento real da API. Parte é corrigível por objetos e propriedades; parte é imposta pelo gerador do GeneXus, que produz o YAML a partir de templates da instalação e não expõe ponto de extensão para esses trechos.

### `Errors[]` — decisão revista

Este registro previa corpo de erro com `Errors[]` derivado das mensagens do BC, com `Code`, `Message` e `Field` por item. O preenchimento foi descartado em `B071`-`B073`/`B079`, depois que a IDE manteve a rejeição da validação da Procedure com `ErrorItem` de subestrutura SDT, e o erro público passou a ser top-level.

A decisão revista retira `Errors[]` também da estrutura do SDT compartilhado `sdt_API_ErrorResponse`, que passa a conter apenas `Code` e `Message`. O motivo é que manter a subestrutura publicava no contrato um array que a geração nunca preenche, pior do que não oferecê-lo: o consumidor poderia construir tratamento de erro sobre um campo sempre ausente.

**Remissão — 2026-08-24.** A afirmação "apenas `Code` e `Message`" descreve o estado após esta emenda de 2026-08-03; desde o fechamento de `B102` o SDT inclui também `Messages[]` — ver `Emenda técnica — 2026-08-24`.

Ficam revistos por esta emenda os trechos deste registro que descrevem `Errors[].Message`, `Errors[].Code` e `Errors[].Field`. Permanecem válidas as regras de `Code` principal, de idioma de `Message`, de não tradução das mensagens do BC e de decisão do cliente por `Code` e nunca por texto.

### Limitações do gerador assumidas

Duas decisões deste registro não são expressáveis no contrato publicado, por limitação do gerador do GeneXus, comprovada e não contornável por API pública do Extensibility SDK:

- os códigos HTTP por operação — `201`, `400`, `409`, `422`, `500` — não são declarados no YAML de um objeto `API`, que sai sempre com `200` e `404`. O runtime continua devolvendo os códigos decididos aqui; apenas a documentação publicada não os anuncia;
- a obrigatoriedade por campo não é declarada nos schemas de request, mesmo com a propriedade nativa correspondente gravada e persistida no item de SDT.

A obrigatoriedade do corpo, essa sim, passou a ser declarada: `requestBody` sai com `required: true` em `Create` e `Update`.

### O que a emenda não altera

Permanecem válidas todas as decisões de status HTTP em runtime, a semântica de `Required` como preenchimento fixada na emenda anterior desta mesma data, e a regra de que a extensão não escreve o arquivo YAML diretamente. Evidência completa em `Docs/Implementation/2026-08-03-CONTRATO-OPENAPI-GAPS.md`; documentos `Foundation` 12, 15 e 27 atualizados na mesma data.

## Objetivo e limites do produto
### Decisões aceitas

- O produto deve ser uma extensão executada dentro da IDE GeneXus.
- Seu objetivo central é gerar um objeto `API` oficial e nativo do GeneXus.
- Geração fora da IDE pertence a outros projetos e não satisfaz este objetivo.
- GeneXus 18 Upgrade 14 é a versão mínima de compatibilidade; GeneXus 18 Upgrade 15 é o ambiente inicial de validação.
- Compatibilidade futura com GeneXus Next é desejável, mas não bloqueia o MVP.
- O projeto será totalmente open source e sem limite de uso.
- O mantenedor será o primeiro usuário, em suas próprias KBs, mas o produto deve servir à comunidade.
- K2BTools Service Builder e WorkWithPlus Service Layer são referências de mercado, não dependências nem fontes de implementação.
- K2BTools deve ser tratado como produto pago.

## Remoção limpa e dependências

- Os objetos gerados serão objetos nativos do GeneXus.
- Remover a extensão não poderá impedir build, geração ou execução da KB.
- A desinstalação não apagará automaticamente os objetos gerados.
- A extensão não deixará dependência obrigatória de runtime.
- DLL própria como `External Object` somente será admitida se SDK e comandos nativos não resolverem; o fonte ficará no repositório.
- O MVP não terá uma `Procedure` utilitária compartilhada obrigatória em runtime.
- A remoção da extensão não reverterá automaticamente a propriedade `Business Component` de Transactions.

## Entrada do wizard

- O núcleo receberá uma coleção de Transactions desde o início.
- O MVP limitará cada execução a uma Transaction.
- Haverá entrada pelo menu principal, com seleção nativa filtrada para Transaction e seleção única.
- Haverá também entrada pelo menu de contexto de uma Transaction.
- As duas entradas usarão o mesmo wizard e motor de geração.
- Seleção múltipla ficará para uma fase posterior.
- O SDK público já demonstrou possuir diálogo de seleção por tipo e suporte a seleção múltipla.

## Business Component

- O MVP usará `Business Component` para preservar as regras da Transaction aplicáveis via BC.
- CRUD direto por `Procedure`, sem BC, poderá existir no futuro, mas não integra o MVP.
- Sem BC, o MVP não gerará a API.
- Se a propriedade estiver desabilitada, o wizard poderá oferecer habilitá-la.
- A autorização aparecerá desmarcada por padrão e bloqueará a geração enquanto não for marcada.
- Cancelar o wizard não modificará a Transaction.

## Serviços do MVP

- `List`: incluído e marcado por padrão.
- `Get`: incluído e marcado por padrão.
- `Create`: incluído e marcado por padrão.
- `Update`: incluído e marcado por padrão.
- `Delete`: fora do primeiro MVP; quando implementado, será opt-in e desmarcado por padrão.
- O MVP trabalhará somente com o primeiro nível da Transaction. **Superado pela `Emenda técnica — 2026-08-20`:** transações com subníveis passam a ser suportadas (B095–B099). **Remissão sobre `Delete`:** a decisão acima de mantê-lo opt-in e desmarcado por padrão permanece vigente e é executada por `B100`, conforme a `Emenda técnica — 2026-08-23`.
- Chaves primárias simples e compostas serão suportadas.
- Ordem e tipos das partes da chave serão preservados no `RestPath` e na chamada ao BC.
- `Update` usará `PUT` no mesmo caminho de `Get`, com todas as partes da chave no `RestPath`.
- `Update` representará substituição completa dos campos atualizáveis selecionados, e não atualização parcial.
- A implementação carregará o BC, retornará `404` quando o registro não existir, preservará chave, autonumeração e campos não editáveis, aplicará os valores recebidos e salvará via BC.
- O wizard mostrará os campos elegíveis e permitirá seleção fácil; os padrões de marcação ainda serão discutidos campo a campo.

## Filtros do serviço List

- O wizard mostrará todos os atributos do primeiro nível da Transaction.
- Todas as partes da chave primária virão marcadas como filtros por padrão.
- O `Description Attribute`, quando existir, também virá marcado por padrão.
- Os demais atributos virão desmarcados.
- Atributos tecnicamente inadequados para filtro serão exibidos desabilitados, com o motivo, em vez de serem ocultados.
- Atributos de subníveis não serão oferecidos como filtros no MVP.
- Atributos `Date` e `DateTime` poderão ser marcados com a opção adicional `Usar período`.
- Para atributo `DateTime`, o período considerará somente a parte da data.
- `Usar período` virá marcado por padrão para todo atributo `Date` ou `DateTime` escolhido como filtro.
- O usuário poderá desmarcá-lo para gerar filtro por igualdade direta.
- Os limites inicial e final serão independentes e opcionais.
- Período com início posterior ao fim gerará erro de validação.
- Para `Date`, o início será inclusivo (`>=`) e o fim também será inclusivo (`<=`).
- Para `DateTime`, o início será o começo do dia informado e o limite final será exclusivo, correspondente ao começo do dia seguinte à data final.
- Os limites efetivamente aplicados serão devolvidos em `appliedFilters` como datas no formato `YYYY-MM-DD`.
- Os parâmetros preservarão integralmente o nome do atributo e receberão os sufixos `From` e `To`.
- Em período de atributo `DateTime`, os dois parâmetros terão tipo `Date`, apesar do tipo original do atributo.
- Se `Usar período` for desmarcado, haverá somente o parâmetro com o nome e o tipo originais do atributo, para igualdade direta.
- Para atributos textuais, o wizard oferecerá os operadores `Igual`, `Contém` e `Começa com`.
- Chaves primárias textuais usarão `Igual` por padrão; os demais atributos textuais usarão `Contém` por padrão.
- Cada atributo textual terá somente um operador selecionado e gerará um único parâmetro, cujo nome permanecerá igual ao nome do atributo.
- `Termina com` não integrará o MVP.
- A extensão não prometerá busca indiferente a maiúsculas e minúsculas; esse comportamento seguirá o DBMS e a collation da aplicação.
- Chaves primárias numéricas, chaves estrangeiras numéricas e atributos baseados em domínio enumerado usarão somente `Igual`.
- Os demais atributos numéricos usarão `Igual` por padrão e poderão receber a opção adicional `Usar intervalo`, desmarcada por padrão.
- Quando `Usar intervalo` estiver marcado, serão gerados os parâmetros opcionais e independentes `NomeDoAtributoMin` e `NomeDoAtributoMax`, com limites inclusivos (`>=` e `<=`).
- Intervalo numérico com `Min` maior que `Max` retornará `400 Bad Request`; os limites reconhecidos serão devolvidos em `appliedFilters`.
- Cada atributo numérico usará igualdade ou intervalo, nunca os dois simultaneamente.
- Um domínio enumerado não receberá intervalo, mesmo que seu tipo físico seja numérico.
- Atributos `Boolean`, domínios enumerados e `Guid` usarão somente o operador `Igual`; não receberão intervalo nem operadores textuais.
- O contrato gerado preservará o tipo e, para domínios enumerados, os valores definidos pelo domínio.
- A presença de filtros opcionais deverá distinguir parâmetro ausente de valores vazios válidos, especialmente `false` e `0`; essa distinção não poderá depender somente de `IsEmpty()`.
- O spike verificará como o objeto `API` informa a presença do parâmetro e, se necessário, avaliará recursos HTTP nativos do GeneXus sem alterar o tipo público nem recorrer a DLL.
- Parâmetro ausente não aplicará filtro; `false` e `0` informados aplicarão filtros reais e deverão aparecer em `appliedFilters`.
- A tipagem correta desses parâmetros deverá permanecer visível no YAML gerado pelo GeneXus.
- Se um tipo não puder satisfazer esses requisitos de forma nativa e confiável, ele ficará desabilitado como filtro no MVP, com o motivo apresentado no wizard.
- Atributos `LongVarChar`, `Image`, `Audio`, `Video` e qualquer tipo ainda não validado pela extensão aparecerão desabilitados como filtros, sempre com o motivo.
- Tipos disponíveis somente no GeneXus Next, como `Embedding`, permanecerão desabilitados até receberem suporte específico.
- O MVP não gerará filtros alternativos como “possui mídia”, “está vazio” ou pesquisa textual em conteúdo longo.
- Um atributo `DateTime` configurado como somente horário (`DateFormat = None`) não receberá `Usar período`; usará somente `Igual` no MVP.

## Segurança

- O wizard terá um único campo `Security Level`, aplicado inicialmente a todos os serviços gerados.
- Em KB com GAM, o wizard exibirá os valores oficiais de `SecurityLevel`: `Authentication`, `Authorization` e `None`; `Authentication` permanecerá selecionada por padrão.
- Escolher `Authorization` exigirá permissões GAM coerentes antes da geração definitiva.
- Escolher `None` exigirá confirmação explícita antes da geração.
- Em KB sem GAM, `None` será o único valor aplicável e o wizard exibirá aviso explícito de que a API será gerada sem autenticação.
- O valor será gravado explicitamente em cada serviço; a API não poderá ficar silenciosamente pública por causa do padrão implícito `None` do GeneXus.
- O MVP não permitirá níveis diferentes para `List`, `Get`, `Create` e `Update`.
- `SecurityPermission` granular por serviço ficará para evolução posterior, com permissões coerentes e possivelmente distintas para leitura, criação e alteração.

## Paginação e ordenação

- `page` terá padrão fixo `1` e não será um campo configurável no wizard do MVP.
- O wizard exibirá `Default Page Size`, editável e preenchido inicialmente com `50`.
- O wizard exibirá `Maximum Page Size`, editável e preenchido inicialmente com `200`.
- A validação exigirá `1 <= Default Page Size <= Maximum Page Size`.
- Valores de `page` ou `pageSize` menores que `1`, bem como `pageSize` acima do máximo configurado, produzirão `400 Bad Request`; a API não reduzirá valores silenciosamente.
- O MVP não oferecerá opção para desativar a paginação.
- Os dois valores configuráveis serão preservados nos metadados da extensão e reutilizados em regenerações posteriores.
- O wizard permitirá selecionar zero, um ou vários atributos ordenáveis e definir, para cada um, direção ascendente ou descendente.
- A seleção padrão será a chave primária completa, na ordem em que aparece na Transaction e em direção ascendente.
- A ordem dos atributos escolhidos no wizard definirá a prioridade das cláusulas de ordenação.
- Se o usuário escolher outra ordenação, as partes da chave primária que ainda não estiverem presentes serão acrescentadas ao final, em direção ascendente, como critérios de desempate.
- Se nenhum atributo for selecionado, será usada a chave primária completa em direção ascendente.
- A ordenação será estática, definida na geração. O MVP não exporá parâmetros públicos como `sortBy` ou `sortDirection`.
- A configuração será preservada nos metadados da extensão para regenerações posteriores.
- `List` sempre retornará envelope com `items`, `pagination` e `appliedFilters`.
- `pagination` usará o SDT compartilhado `sdt_API_Pagination`, com `page`, `pageSize`, `totalCount` e `totalPages`.
- `totalCount` representará o total depois da aplicação dos filtros.
- `appliedFilters` conterá os valores efetivamente reconhecidos e aplicados, depois de validação, normalização e valores padrão.
- Sem filtros aplicados, `appliedFilters` continuará presente e seus membros serão `null`, conforme a decisão posterior sobre `sdtNomeDaTransacao_API_ListFilters`.
- Filtros inválidos gerarão erro de validação em vez de serem ignorados silenciosamente.
- Filtros sensíveis, tokens e credenciais nunca serão devolvidos.

## Nome do objeto API e Services base path

Para uma Transaction `NomeDaTransacao`:

```text
Objeto API:          apiNomeDaTransacao
Services base path:  apiNomeDaTransacao
```

- Ambos serão visíveis e editáveis no wizard.
- O `Services base path` acompanhará o nome do objeto enquanto não tiver sido editado manualmente.
- Depois de edição manual, o valor será preservado.
- A propriedade será sempre gravada explicitamente no objeto `API`.
- Uma base compartilhada como `api/v1` ficará para investigação futura.

## Terminologia e nomes dos serviços

- A interface e a documentação usarão **serviço**, conforme a terminologia do objeto `API` GeneXus.
- “Recurso” poderá aparecer apenas em explicações conceituais de REST.
- Os nomes CRUD padrão serão `List`, `Get`, `Create`, `Update` e `Delete`.
- Para `Produto`, os `operationId` tenderão a `apiProduto.List`, `apiProduto.Get`, `apiProduto.Create`, `apiProduto.Update` e `apiProduto.Delete`.
- `Create` foi preferido a `Insert`, e `Get` a `GetById`, porque a chave pode ser composta.
- APIs manuais de negócio continuam livres para usar português no infinitivo impessoal, como em `apiPDV_Integracao`.

## Descrições dos serviços

- A extensão gerará automaticamente uma anotação `[Description]` curta e padronizada para cada serviço selecionado: `List`, `Get`, `Create` e `Update`; o mesmo princípio valerá para `Delete` quando ele existir.
- O MVP não acrescentará campos de descrição ao wizard.
- As descrições usarão preferencialmente a descrição legível da Transaction, recorrendo ao nome do objeto quando ela estiver vazia.
- O texto permanecerá editável no objeto `API` nativo depois da geração.
- Alterações manuais posteriores serão tratadas pelo mecanismo geral de comparação e confirmação da regeneração, nunca sobrescritas silenciosamente.
- O idioma será escolhido automaticamente a partir do idioma principal da KB; não haverá outro campo no wizard.
- O MVP fornecerá modelos de descrição em português, espanhol e inglês.
- Quando o idioma da KB não tiver modelo próprio, a extensão usará inglês e informará o fallback no resumo da geração.
- A descrição legível da Transaction será preservada no idioma em que estiver escrita, sem tradução automática.


## Caminho comum dos serviços — RestPath

- O wizard terá o campo editável `Caminho comum dos serviços (RestPath)`.
- A sugestão automática converterá mecanicamente o nome da Transaction para minúsculas separadas por hífen.
- O MVP não tentará pluralização linguística.
- O usuário poderá substituir a sugestão pelo plural ou por outro caminho.
- `List` e `Create` usarão o caminho comum diretamente; `Get` acrescentará todas as partes da chave.

Exemplos:

```text
Produto                   -> /produto
DocumentoFiscal           -> /documento-fiscal
BandeiraDeCartao          -> /bandeira-de-cartao
PessoaEnderecos           -> /pessoa-enderecos
DocumentoFiscalItemIbsCbs -> /documento-fiscal-item-ibs-cbs
GTA                       -> /gta
UF                        -> /uf
```

Exemplo de inclusão para `BandeiraDeCartao`:

```text
Objeto API:          apiBandeiraDeCartao
Services base path:  apiBandeiraDeCartao
Serviço:             Create
Método HTTP:         POST
RestPath:            /bandeira-de-cartao
operationId:         apiBandeiraDeCartao.Create
URL relativa:        /apiBandeiraDeCartao/bandeira-de-cartao
```

A rejeição da pluralização automática foi sustentada por 184 nomes reais de Transactions da KB FabricaBrasil, incluindo nomes simples, compostos, já pluralizados, siglas, convenções especiais e plurais irregulares.

## Módulo e organização dos objetos

- Todos os objetos gerados ficarão no mesmo módulo da Transaction.
- O módulo não será editável no MVP.
- Um `Module` exclusivo para APIs não será criado por padrão: módulos-fonte dentro da mesma KB não reduzem automaticamente o tempo de build.
- A separação em módulo exclusivo também acrescentaria referências qualificadas, regras de visibilidade e possíveis colisões entre Transactions homônimas de módulos diferentes.
- O ganho de build documentado para módulos depende de empacotamento e instalação como `Module Reference`, cenário incompatível com contratos gerados para Transactions locais e em evolução.
- Um spike medirá o impacto real dos objetos adicionais no build; eventual organização alternativa será reconsiderada apenas com evidência.
- Um spike avaliará associação visual sob a Transaction, semelhante ao nó do WorkWithWeb.
- A associação só será usada se o SDK público e estável permitir, sem dependência persistente de Pattern.
- O fallback será uma `Folder` nativa chamada `NomeDaTransacaoOpenApi`.

## Metadados e regeneração

- Cada geração terá definição técnica persistente em JSON, armazenada como objeto `File` da KB.
- Os objetos gerados terão documentação humana de origem.
- Partes textuais poderão usar marcadores delimitados para separar regiões geradas e mantidas pelo usuário.
- A regeneração atualizará somente o que pertence à extensão e não acumulará objetos `_v2`.
- Objeto `Documentation` poderá ser reconsiderado após experimento técnico de round-trip pelo SDK; não será a fonte técnica principal por enquanto.
- O MVP assumirá que o nome e o módulo da Transaction permanecem inalterados entre geração e regeneração.
- Renomeação e movimentação assistidas da Transaction ficarão fora do MVP; a extensão não renomeará nem moverá automaticamente o conjunto gerado.
- Se a origem esperada não puder ser reencontrada com segurança, a regeneração será bloqueada antes de qualquer alteração e informará que esse cenário ainda não é suportado.

## Colisões com objetos preexistentes

- Os nomes exemplificados com `Corte`, `Produto` ou outra Transaction são apenas concretizações da regra genérica baseada na Transaction escolhida.
- Antes de criar ou alterar qualquer objeto, a extensão verificará todos os nomes planejados para a execução.
- Nome livre poderá ser criado normalmente.
- Objeto reconhecido pelos metadados como pertencente à mesma API e à mesma Transaction seguirá o fluxo de regeneração, com comparação e confirmação.
- Objeto existente sem metadados válidos da extensão, ou associado por eles a outra API ou Transaction, será tratado como colisão.
- Se houver qualquer colisão entre objetos, a execução não criará nem alterará nenhum objeto planejado.
- O wizard mostrará, para cada conflito, nome, tipo, módulo e Folder.
- O MVP não sobrescreverá, adotará, apagará nem acrescentará sufixos automaticamente a objetos preexistentes.
- O usuário poderá resolver o conflito na KB e executar novamente. Quando somente o nome do objeto `API` conflitar, também poderá alterar esse nome no campo já editável do wizard.
- Folder preexistente com o nome `NomeDaTransacaoOpenApi` no módulo correto poderá ser reutilizado, pois é apenas um contêiner organizacional.
- O resumo do wizard informará explicitamente que o Folder existente será reutilizado.
- Nenhum conteúdo preexistente será movido, alterado nem assumido como pertencente à extensão.
- Os objetos planejados dentro dele continuarão sujeitos à verificação normal de colisões.
- Os metadados distinguirão Folder reutilizado de Folder criado pela extensão.
- Ao remover a API, a extensão retirará somente os objetos que ela própria gerou e nunca apagará um Folder preexistente reutilizado.
- A remoção ocorrerá somente pelo comando explícito `Remover API gerada`; desinstalar a extensão da IDE não apagará objetos da KB.
- Antes de remover, a extensão mostrará todos os objetos identificados pelos metadados e exigirá confirmação.
- Se o Folder tiver sido criado pela extensão e ficar vazio depois da remoção, ele será apagado na mesma operação.
- Se o Folder contiver qualquer objeto que não pertença à geração removida, ele será preservado.
- Os SDTs compartilhados do Folder `GxOpenAPI` não serão apagados ao remover uma API específica.


## Reuso de SDTs

- No MVP, a extensão criará contratos próprios e não reutilizará SDTs arbitrários preexistentes na KB.
- Em uma regeneração, a extensão reencontrará e atualizará os SDTs que ela própria tiver gerado anteriormente, identificados pela metadata persistente da geração.
- SDT preexistente sem evidência de ter sido criado pela extensão será tratado como externo, ainda que o nome ou a estrutura sejam semelhantes.
- Reuso assistido de SDTs externos poderá ser estudado depois do MVP, sempre com escolha explícita e critérios próprios para cada responsabilidade de contrato.
- O possível custo dos SDTs adicionais no build será medido nas duas KBs de teste; não será presumido apenas pela quantidade de objetos.

## Sincronização com a Transaction

- Os SDTs gerados serão retratos controlados do contrato da API, e não espelhos alterados automaticamente junto com a Transaction.
- O objeto `sdtNomeDaTransacao_API_Response` cobrirá todos os atributos do primeiro nível declarados na estrutura da Transaction, incluindo atributos armazenados, inferidos da tabela estendida, fórmulas, partes da chave e campos somente de leitura. **Superado em parte pelas emendas de 2026-08-20 e 2026-08-23:** o `Response` passa a incluir também um membro coleção por subnível selecionado, tipado por `sdt<NomeBase>_API_Response_<Subnível>`. A cobertura do primeiro nível permanece exatamente como descrita.
- A extensão não incluirá indiscriminadamente todos os atributos alcançáveis pela tabela estendida que não estejam declarados na estrutura da Transaction.
- Mudanças na Transaction somente chegarão aos contratos por uma ação explícita `Sincronizar com a Transaction`.
- A sincronização comparará a estrutura atual com a metadata da última geração e apresentará atributos adicionados, removidos ou renomeados, mudanças de tipo e mudanças de gravabilidade.
- Nenhuma alteração será aplicada antes da confirmação do usuário.
- Atributo novo no primeiro nível virá proposto e marcado para inclusão no `Response`; nos Requests, dependerá de sua elegibilidade para inclusão ou alteração via BC. **Estendido pelas emendas de 2026-08-20 e 2026-08-23:** a mesma regra vale por nível, e a sincronização compara adições, remoções e renomeações dentro da hierarquia, inclusive o caso de um subnível inteiro deixar de existir na Transaction.
- Alterações potencialmente incompatíveis, como remoção, renomeação ou mudança de tipo, receberão aviso específico.
- Um novo campo obrigatório ou uma nova regra aplicável via BC será sinalizado como risco de quebra do `Create`, mesmo antes de qualquer mudança no contrato publicado.
- Se um SDT gerado tiver sido alterado manualmente desde a última geração, o MVP não tentará mesclagem automática nem o sobrescreverá silenciosamente; mostrará o conflito e permitirá manter, substituir conscientemente ou cancelar.
- Detecção automática em segundo plano e indicador persistente de API desatualizada poderão ser considerados depois do MVP.

## CreateRequest — elegibilidade inicial

- O `sdtNomeDaTransacao_API_CreateRequest` aceitará somente atributos do primeiro nível que possam receber valor antes do `Save()` do BC.
- Virão marcados por padrão: partes não autonumeradas da chave primária, atributos secundários armazenados, chaves estrangeiras armazenadas e atributos graváveis com regra `Default`.
- Atributos nullable ou opcionais continuarão elegíveis e marcados; inclusão no contrato não significa obrigatoriedade no payload.
- Serão exibidos desabilitados e com justificativa: chave autonumerada, fórmula, atributo inferido da tabela estendida, redundante mantido automaticamente, atributo de subnível e qualquer atributo inequivocamente não atribuível via BC.
- Campos potencialmente sensíveis continuarão tecnicamente elegíveis, mas virão desmarcados e com alerta.
- Tipos `Image`, `Video`, `Audio`, `Blob` e `BlobFile` ficarão desabilitados no MVP por exigirem fluxo específico de upload.
- A extensão avisará quando um campo parecer necessário para regras aplicáveis via BC, mas a validação definitiva permanecerá responsabilidade do próprio BC.
- Campos reconhecidos como auditoria serão exibidos, mas permanecerão desabilitados no `CreateRequest` e no `UpdateRequest`.

**Emenda técnica de 2026-08-12 — `NoAccept`:** atributos cobertos por uma regra `NoAccept` continuam visíveis na aba `Requests`, mas ficam desabilitados no `CreateRequest` e no `UpdateRequest`, porque a geração de assignments para o BC causa `spc0018` por propriedade somente leitura. Eles permanecem candidatos a `Response`, `ListResponse` e `ListFilters`. A evidência A/B e a implementação estão em `Docs/Implementation/2026-08-12-NOACCEPT-READONLY-BUSINESS-COMPONENT.md`.

**Emenda técnica de 2026-08-20 — Subníveis:** atributos de subníveis selecionados passam a ser elegíveis e entram como coleções aninhadas no `CreateRequest`, conforme detalhado na `Emenda técnica — 2026-08-20` e em `Docs/Implementation/2026-08-20-SUPORTE-TRANSACTIONS-SUBNIVEIS.md`.

## CreateRequest — presença dos membros no JSON

- A obrigatoriedade de presença será definida separadamente da seleção do membro para o `sdtNomeDaTransacao_API_CreateRequest`.
- Partes não autonumeradas da chave primária deverão estar presentes.
- Campos necessários para criar o registro, sem regra `Default` nem preenchimento automático conhecido, deverão estar presentes.
- Campos com regra `Default`, nullable ou opcionais, e campos preenchidos pelas regras da Transaction aplicáveis via BC poderão ser omitidos.
- Campos de origem de migração selecionados serão opcionais por padrão, salvo decisão explícita do usuário no wizard.
- O wizard mostrará uma definição separada de `Obrigatório no payload`, preenchida automaticamente e editável somente quando a alteração for segura.
- A presença obrigatória não impedirá o envio do valor vazio representável pelo tipo; a validade desse valor continuará sujeita às regras da Transaction aplicáveis via BC.

## UpdateRequest — elegibilidade inicial

- O `sdtNomeDaTransacao_API_UpdateRequest` aceitará somente atributos do primeiro nível que possam receber valor no BC carregado antes do `Save()`.
- Virão marcados por padrão os atributos ordinários armazenados e atribuíveis via BC, inclusive chaves estrangeiras armazenadas, campos nullable ou opcionais e campos de origem de migração.
- Todas as partes da chave primária serão exibidas desabilitadas, pois a chave já identificará o registro no `RestPath` e o serviço `Update` não permitirá alterá-la.
- Campos potencialmente sensíveis continuarão tecnicamente elegíveis, mas virão desmarcados e com alerta.
- Campos de auditoria operacional, fórmulas, atributos inferidos da tabela estendida, redundantes mantidos automaticamente, atributos de subnível e atributos inequivocamente não atribuíveis via BC serão exibidos desabilitados e com justificativa.
- Tipos `Image`, `Video`, `Audio`, `Blob` e `BlobFile` ficarão desabilitados no MVP.
- O padrão será atualizar todos os campos ordinários graváveis selecionados, preservando a identidade do registro, a auditoria e os valores controlados pelo sistema.

**Emenda técnica de 2026-08-12 — `NoAccept`:** a regra transversal acima também se aplica ao `UpdateRequest`; o atributo permanece fora do corpo e dos assignments de Update, embora continue disponível nos contratos de saída.

**Emenda técnica de 2026-08-20 — Subníveis:** atributos de subníveis selecionados passam a ser elegíveis e entram como coleções aninhadas no `UpdateRequest`, com política de substituição completa no BC, conforme detalhado na `Emenda técnica — 2026-08-20` e em `Docs/Implementation/2026-08-20-SUPORTE-TRANSACTIONS-SUBNIVEIS.md`. **Revisto em 2026-08-23:** a substituição completa passa a exigir o marcador `<Subnível>Replace = True` no próprio `UpdateRequest`; ausente ou `False`, as linhas do subnível não são tocadas.

## UpdateRequest — presença dos membros no JSON

- Todos os membros selecionados para o `sdtNomeDaTransacao_API_UpdateRequest` deverão estar presentes no JSON.
- A presença obrigatória não torna obrigatório um valor não vazio: o cliente poderá enviar o valor vazio representável pelo tipo.
- A validade do valor vazio continuará sujeita às regras da Transaction aplicáveis via BC.
- A ausência de qualquer membro selecionado causará `400 Bad Request` antes que a Procedure atribua valores ao BC ou tente salvá-lo.
- O contrato OpenAPI resultante deverá expressar essa obrigatoriedade de presença; o YAML gerado pelo GeneXus será usado para validar o resultado.
- Um experimento técnico de validação verificará como o objeto `API` e a desserialização do SDT distinguem membro ausente de membro presente com valor vazio ou nulo. Se a distinção não estiver disponível por comandos nativos, a solução técnica será avaliada explicitamente antes da implementação.

## Retornos de sucesso de Create e Update

- `Create` retornará `201 Created`.
- `Update` retornará `200 OK`, e não `204 No Content`.
- Ambos retornarão o registro completo em `sdtNomeDaTransacao_API_Response`, sem criar outro SDT apenas para envolver o resultado.
- Depois de salvar com sucesso, a Procedure recarregará o BC pela chave final e montará o `Response`. Isso incluirá chave autonumerada, valores aplicados por regras `Default`, auditoria, fórmulas e atributos inferidos selecionados para o contrato.
- O cabeçalho HTTP `Location`, indicando o caminho de consulta do registro recém-criado, é desejável no `Create`, mas não obrigatório para o MVP.
- `Location` somente será gerado se houver suporte nativo simples no GeneXus; não justificará DLL, `External Object` ou solução complexa no MVP.

## Retornos de Get e List

- `Get` retornará `200 OK` com `sdtNomeDaTransacao_API_Response` quando encontrar o registro.
- Chave inexistente em `Get` retornará `404 Not Found` com o contrato uniforme de erro.
- Uma consulta válida de `List` retornará sempre `200 OK` com `sdtNomeDaTransacao_API_ListResponse`.
- Quando nenhum registro corresponder aos filtros, `List` não retornará `404`; retornará coleção vazia, total zero, metadados de paginação e confirmação dos filtros recebidos.
- Parâmetro ou filtro inválido retornará `400 Bad Request` com o contrato uniforme de erro.

## Contrato de erros e status HTTP

- `400 Bad Request`: JSON inválido, parâmetro malformado, membro obrigatório ausente, paginação, filtro ou período inválido.
- `401 Unauthorized`: autenticação obrigatória ausente ou inválida.
- `403 Forbidden`: usuário autenticado, mas sem autorização para executar o serviço.
- `404 Not Found`: chave inexistente em `Get` ou `Update`.
- `409 Conflict`: chave duplicada, restrição de unicidade ou outro conflito identificável com segurança.
- `422 Unprocessable Content`: requisição estruturalmente válida que foi rejeitada pelas regras de negócio executadas via BC.
- `500 Internal Server Error`: falha inesperada; a resposta pública não exporá exceção, stack trace nem detalhes internos.
- Quando uma falha do BC produzir mensagens, o erro principal usará `Code = validation_error`, uma mensagem de resumo e itens em `Errors` derivados das mensagens do BC.
- O `Code` principal será um identificador estável em inglês e `snake_case`: `invalid_request`, `unauthorized`, `forbidden`, `not_found`, `conflict`, `validation_error` ou `internal_error`.
- `Message` e `Errors[].Message` serão textos legíveis no idioma usado pela aplicação e pela KB; a extensão não tentará traduzir as mensagens produzidas pelo BC.
- `Errors[].Code` preservará o identificador da mensagem do BC quando ele existir; na ausência dele, usará o código genérico `business_rule`.
- Clientes e frontends deverão tomar decisões por `Code`, nunca pela comparação do texto de `Message`.
- `Errors[].Field` conterá exatamente o nome público da entrada recebida pela API, preservando maiúsculas e minúsculas. Poderá identificar um membro do Request ou um parâmetro de rota, filtro ou paginação.
- `Errors[].Field` não exporá nomes de variáveis internas das Procedures.
- A extensão não tentará descobrir o campo analisando o texto da mensagem do BC. O preenchimento ocorrerá somente quando a validação gerada já conhecer a entrada ou quando metadados nativos fornecerem uma relação inequívoca.
- Regras gerais, regras envolvendo vários campos e mensagens sem associação confiável deixarão `Field` vazio.
- Remissão: os itens acima que descrevem `Errors[]` foram revistos pela `Emenda técnica — 2026-08-03 — contrato OpenAPI publicado`. O corpo de erro entregue é top-level, com `Code` e `Message`, e `Errors[]` não existe no SDT gerado.
- Remissão — 2026-08-24: desde o fechamento de `B102` o SDT inclui também `Messages[]` tipado por `sdt_API_ErrorMessage`; ver `Emenda técnica — 2026-08-24`. `Field` permanece fora do contrato entregue.
- Não será acrescentado um membro separado como `Location` ao contrato de erro no MVP.
- Erros controlados pelas Procedures e pelo objeto `API` usarão `sdt_API_ErrorResponse`.
- Um spike deverá verificar se erros interceptados pelo GAM ou pelo runtime antes da Procedure podem preservar o mesmo corpo. A uniformidade nesses casos não será prometida antes dessa validação.
- Se um conflito não puder ser distinguido com segurança de uma rejeição de regra de negócio, a extensão não presumirá `409`; usará `422`.

## Campos de auditoria e de origem de migração

- A extensão terá configuração geral por KB para reconhecer campos de auditoria operacional por nomes exatos ou sufixos suficientemente específicos.
- A configuração inicial poderá contemplar convenções como `InclusaoDataHora`, `InclusaoUsuarioId`, `InclusaoUsuarioNome`, `UltimaAtualizacaoDataHora`, `UltimaAtualizacaoUsuarioId` e `UltimaAtualizacaoUsuarioNome`.
- Fragmentos genéricos como `Atualizacao`, `ResumoAtualizacao`, `Usuario` ou `DataHora` não serão usados isoladamente, pois produziriam falsos positivos.
- Campos classificados como auditoria operacional ficarão desabilitados no `CreateRequest` e no `UpdateRequest`; as Procedures geradas não atribuirão a eles valores recebidos da requisição, deixando seu preenchimento para as regras da Transaction aplicáveis via BC.
- Esses campos integrarão normalmente o `Response`.
- Poderão ser oferecidos como filtros de `List`, mas virão desmarcados por padrão. Quando forem `Date` ou `DateTime`, poderão usar a opção de período já definida para filtros.
- O MVP não oferecerá liberação casual por API para aceitar campos reais de auditoria nos Requests. Uma convenção diferente deverá ser tratada conscientemente na configuração geral da KB.
- Campos destinados a preservar origem ou informações de migração não serão confundidos com auditoria operacional. O exemplo `PessoaOrigemResumoAtualizacao` continuará candidato normal ao `CreateRequest` e ao `UpdateRequest` quando for atribuível via BC.
- O fato de um campo estar desabilitado via edição web não prova que seja não atribuível via BC; a extensão avaliará a elegibilidade no contexto do BC.

## Folder e SDTs compartilhados

- A extensão criará o Folder `GxOpenAPI` dentro do `Root Module` quando o primeiro objeto compartilhado for necessário.
- Como Folder, `GxOpenAPI` fornecerá organização visual sem criar namespace, encapsulamento ou regra própria de visibilidade.
- Os objetos compartilhados permanecerão objetos nativos pertencentes ao `Root Module` e poderão ser referenciados pelas APIs geradas em outros módulos.
- O MVP terá dois SDTs compartilhados por KB: `sdt_API_ErrorResponse` e `sdt_API_Pagination`.
- `sdt_API_ErrorResponse` terá `Code`, `Message` e a coleção interna `Errors`; cada item de `Errors` terá `Code`, `Message` e `Field`.
- `Errors` será subestrutura do próprio `sdt_API_ErrorResponse`; não será criado `sdt_API_ErrorDetail` separado no MVP.
- Remissão — 2026-08-24: desde o fechamento de `B102` o conjunto compartilhado passa a três SDTs — `sdt_API_ErrorMessage`, `sdt_API_ErrorResponse` e `sdt_API_Pagination`. `sdt_API_ErrorResponse` contém `Code`, `Message` (`LongVarChar` 2097152) e `Messages[]` tipado por `sdt_API_ErrorMessage`; `Errors` como subestrutura interna e `Field` ficam fora do contrato entregue. Ver `Emenda técnica — 2026-08-24`.
- `sdt_API_Pagination` terá `Page`, `PageSize`, `TotalCount` e `TotalPages` e será usado pelo membro `Pagination` dos `ListResponse` específicos.
- Os SDTs compartilhados serão criados uma única vez, reutilizados pelas gerações seguintes e nunca sobrescritos silenciosamente quando houver estrutura incompatível.
- O Folder e seus objetos não serão apagados automaticamente ao remover uma API nem ao desinstalar a extensão.
- `sdt_API_ListOptions` não integrará o MVP: `page` e `pageSize` continuarão parâmetros simples do serviço; um objeto apenas interno acrescentaria mapeamento e dependência sem centralizar a lógica de validação.
- `sdt_API_SuccessResponse` não será criado: os códigos HTTP já indicam sucesso e `Create`, `Update` e `Get` retornarão diretamente os contratos tipados específicos da Transaction.
- Não serão criados no MVP SDTs genéricos para filtros aplicados, períodos de data, ordenação, auditoria ou links de paginação. Eles perderiam tipagem, contrariariam parâmetros planos já aceitos ou ainda não possuem requisito concreto.
- Novos objetos só entrarão em `GxOpenAPI` quando tiverem estrutura realmente idêntica entre APIs, significado independente da Transaction e benefício concreto de reutilização.

## Procedures geradas — nomenclatura

Para uma Transaction `NomeDaTransacao`, o padrão aceito é:

```text
procNomeDaTransacao_API_List
procNomeDaTransacao_API_Get
procNomeDaTransacao_API_Create
procNomeDaTransacao_API_Update
```

- O prefixo `proc` identifica o tipo do objeto e acompanha a convenção preferida pelo usuário.
- O marcador `_API_` separará visualmente essas implementações das Procedures preexistentes relacionadas à mesma Transaction.
- Cada nome será derivado automaticamente e não será editável no wizard do MVP.
- As Procedures ficarão no mesmo módulo e no mesmo Folder dos demais objetos gerados para a Transaction.
- A Procedure nomeará a operação executada, e não apenas seu parâmetro de entrada. Por isso, `procNomeDaTransacao_API_Create` receberá `sdtNomeDaTransacao_API_CreateRequest` sem adotar o sufixo `Request`.
- O objeto `API` delegará cada serviço à Procedure correspondente.


## SDTs gerados — nomenclatura

Para uma Transaction `NomeDaTransacao`, o padrão aceito é:

```text
sdtNomeDaTransacao_API_CreateRequest
sdtNomeDaTransacao_API_UpdateRequest
sdtNomeDaTransacao_API_Response
sdtNomeDaTransacao_API_ListFilters
sdtNomeDaTransacao_API_ListResponse
```

- O marcador `_API_` separará visualmente os contratos gerados dos muitos SDTs preexistentes relacionados à mesma Transaction.
- Os objetos continuarão agrupados alfabeticamente pelo prefixo `sdtNomeDaTransacao`.
- Os nomes são válidos para objetos `SDT` GeneXus e para chaves de componentes OpenAPI.
- O GeneXus leva o nome do objeto `SDT` para `components/schemas`; portanto, o marcador fará parte do contrato OpenAPI público.
- Essa exposição foi aceita como compromisso consciente em favor da organização e da identificação dentro da KB.
- A compatibilidade prática desses nomes será validada posteriormente com o YAML gerado pelo GeneXus e ao menos um gerador de cliente OpenAPI.

## `sdtNomeDaTransacao_API_ListFilters` — responsabilidade e estrutura

- Terá uma única responsabilidade: representar, na resposta, os filtros que a API reconheceu.
- Não será parâmetro de entrada do serviço `List`; os filtros permanecerão parâmetros planos da query string.
- Será o tipo do membro `AppliedFilters` de `sdtNomeDaTransacao_API_ListResponse`.
- Terá somente membros correspondentes aos filtros escolhidos no wizard.
- Filtros por igualdade, `Contém` e `Começa com` usarão membro com o mesmo nome e tipo público do parâmetro.
- Períodos usarão membros `NomeDoAtributoFrom` e `NomeDoAtributoTo`; intervalos numéricos usarão `NomeDoAtributoMin` e `NomeDoAtributoMax`.
- Não conterá paginação nem repetirá o operador, que é fixo na geração da API e deverá ser descrito no contrato OpenAPI.
- Seus membros permitirão `null`: `null` significará filtro não aplicado, enquanto qualquer valor não nulo, inclusive `false` ou `0`, confirmará o valor reconhecido.
- Essa representação evitará membros auxiliares como `NomeDoAtributoApplied`.
- Um spike validará `AllowNull` e a serialização JSON desse SDT no GeneXus 18. Se o comportamento nativo não preservar a distinção, o contrato deverá ser reavaliado antes da implementação.

## `sdtNomeDaTransacao_API_ListResponse` — estrutura

- Terá somente três membros: `Items`, `Pagination` e `AppliedFilters`.
- `Items` será coleção de `sdtNomeDaTransacao_API_Response`.
- `Pagination` terá o tipo compartilhado `sdt_API_Pagination`.
- `AppliedFilters` terá o tipo `sdtNomeDaTransacao_API_ListFilters`.
- Os três membros estarão presentes em toda resposta `200 OK`.
- Quando não houver registros, `Items` será uma coleção vazia; `TotalCount` e `TotalPages` serão zero.
- `Pagination` refletirá a página e o tamanho efetivamente aplicados; `AppliedFilters` seguirá a regra dos membros `null` já definida.
- Não serão acrescentados `Success`, `Message`, `Status`, links nem outro envelope.
- Dentro da KB, os membros usarão PascalCase. Seus nomes externos serão configurados em lower camel case: `items`, `pagination` e `appliedFilters`; o mesmo padrão será aplicado a `page`, `pageSize`, `totalCount` e `totalPages`.
- Um spike confirmará que os nomes externos e a estrutura aparecem dessa forma no YAML gerado pelo GeneXus.

## `sdtNomeDaTransacao_API_Response` — estrutura

- Incluirá todos os atributos do primeiro nível explicitamente declarados na estrutura da Transaction: chave primária completa, atributos armazenados, atributos inferidos ou da tabela estendida declarados e fórmulas ou outros atributos calculados declarados.
- Não incluirá automaticamente atributos da tabela estendida que não apareçam na estrutura, subníveis nem campos sintéticos. **Superado quanto a subníveis pelas emendas de 2026-08-20 e 2026-08-23:** subníveis selecionados no Wizard entram como membro coleção. Permanece válido para atributos não declarados na estrutura e para campos sintéticos.
- Preservará a ordem da estrutura da Transaction.
- Cada membro será baseado no atributo original, preservando domínio, tipo, tamanho, decimais, nulabilidade e demais características aplicáveis.
- Os membros usarão exatamente os nomes dos atributos tanto na KB quanto no JSON, como `ProdutoId` e `ProdutoNome`.
- `Get`, `Create`, `Update` e cada item de `List` usarão o mesmo contrato. **Condicionado pela `Emenda técnica — 2026-08-23`:** permanece assim em transação de nível único; havendo subnível selecionado, cada item de `List` passa a usar `sdt<NomeBase>_API_ListResponse_Item`, para não publicar coleções que a listagem nunca preenche.
- A diferença de caixa será intencional: o envelope genérico usará nomes externos em lower camel case, enquanto os dados da Transaction preservarão os nomes GeneXus.

## `sdtNomeDaTransacao_API_CreateRequest` — estrutura e presença

- Conterá somente os atributos selecionados no wizard e atribuíveis ao BC antes de `Save()`.
- Preservará a ordem da estrutura da Transaction; cada membro será baseado no atributo original e usará exatamente o nome do atributo no SDT, no JSON e no OpenAPI.
- Não conterá envelope, metadados, subníveis nem campos exclusivos de resposta.
- A propriedade `Required` representará que o membro deve estar presente no JSON; presença obrigatória não significará valor obrigatoriamente não vazio. **Revisto pela emenda técnica de 2026-08-03:** `Required` passou a significar preenchimento, e o membro obrigatório com valor default do tipo também é recusado.
- Membro obrigatório ausente produzirá `400 Bad Request`.
- Membro opcional ausente não será atribuído ao BC, preservando regras `Default` e preenchimentos automáticos.
- Membro presente com valor vazio, `false` ou `0` será atribuído exatamente como recebido e validado pelas regras da Transaction aplicáveis via BC. **Revisto pela emenda técnica de 2026-08-03:** isso vale para membros opcionais; membro obrigatório com valor default do tipo é recusado com `400` antes da atribuição.
- A API não acrescentará campos auxiliares públicos, como `ProdutoAtivoSpecified`, para indicar a presença de outros membros.
- Antes da implementação, um experimento técnico de validação deverá confirmar como distinguir, usando recursos nativos do GeneXus, um membro ausente de um membro presente com valor vazio, `false` ou `0`. **Experimento concluído em 2026-08-03:** a distinção não é obtenível no corpo da requisição sem comando `csharp`; ver a emenda técnica da mesma data.

## `sdtNomeDaTransacao_API_UpdateRequest` — estrutura e presença

- Representará a substituição completa, via `PUT`, dos campos atualizáveis selecionados.
- Não conterá partes da chave primária, pois elas identificarão o registro no `RestPath`.
- Conterá somente atributos selecionados e atribuíveis ao BC carregado antes de `Save()`, preservando ordem, tipos e nomes dos atributos.
- Todos os membros selecionados terão `Required = True`; a ausência de qualquer um produzirá `400 Bad Request` antes de qualquer atribuição ao BC. **Revisto pela emenda técnica de 2026-08-03:** o `400` passa a ocorrer também quando o membro chega com o valor default do tipo.
- Valores vazios, `false` e `0` serão tratados como valores realmente enviados e submetidos às regras da Transaction aplicáveis via BC.
- O fluxo carregará o BC pela chave simples ou composta, retornará `404` quando não existir, validará a presença integral do Request, atribuirá os valores, salvará via BC, recarregará e devolverá o `Response`.
- Não haverá campos auxiliares públicos com sufixo `Specified`.
- O mesmo experimento técnico de validação do `CreateRequest` deverá comprovar a distinção entre membro ausente e membro presente no GeneXus 18. **Experimento concluído em 2026-08-03:** ver a emenda técnica da mesma data.
- Atualização parcial e `PATCH` não integrarão o MVP.

## Evidências locais consultadas

- `C:\GxModels\FabricaBrasil18\NETPostgreSQL\Web\apiPDV_Integracao.yaml`: API manual em produção, sem ligação direta com uma Transaction.
- `C:\KBs\wsEducacaoSpTeste\NETPostgreSQL155\Web\ProdutoApi.yaml`: API de teste gerada por agente via XPZ, sem entrevista sobre convenções.
- `C:\Dev\Prod\Gx_FabricaBrasil\ObjetosDaKbEmXml\Transaction`: consulta externa e somente para leitura dos nomes de 184 Transactions.
- `C:\Dev\Prod\Gx_FabricaBrasil\ObjetosDaKbEmXml\Transaction\Pessoa.xml`: consulta externa e somente para leitura que confirmou `PessoaOrigemResumoAtualizacao` como atributo de primeiro nível distinto dos campos operacionais de auditoria.
- `C:\Dev\Prod\Gx_FabricaBrasil\ObjetosDaKbEmXml\SDT`: consulta externa e somente para leitura de 632 SDTs; foram encontrados 85 SDTs cujo nome começa com `sdt` seguido do nome de alguma Transaction.

Os YAMLs confirmaram a composição técnica entre o `Services base path` em `servers.url` e os caminhos dos serviços em `paths`. Seus nomes não foram adotados automaticamente como preferência do mantenedor.

### Papel do OpenAPI YAML

- O OpenAPI YAML é gerado pelo GeneXus a partir do objeto `API` e dos objetos referenciados.
- A extensão não criará nem alterará diretamente o arquivo YAML.
- O desenho e a implementação devem atuar sobre objetos GeneXus, propriedades, serviços, anotações, variáveis, SDTs e Procedures.
- Exemplos em YAML representam resultados esperados, não artefatos-fonte controlados pela extensão.
- O YAML gerado será usado para validar o contrato público resultante e para testes de regressão.
- A forma exata emitida para GeneXus 18 U14 ou posterior deverá ser confirmada por spike e testes na IDE, com U15 como ambiente inicial.

## KBs para testes

- KB menor, fora de produção, com backup disponível.
- Cópia de teste da KB principal, atualizada a partir de XPZs da principal.
- A validação começará na KB menor e avançará para a cópia de teste da principal.

## Gates técnicos transversais do MVP

Os seguintes experimentos são gates transversais do MVP. Sua comprovação será progressiva ao longo das Sprints 1–7, de acordo com as dependências de cada contrato; o conjunto completo deve estar aprovado antes do marco **wizard funcional do MVP concluído** e antes da Alpha.

1. A extensão carrega e funciona no GeneXus 18 U14 ou posterior, com validação inicial no U15.
2. O SDK público permite criar, salvar, reabrir, alterar e excluir objetos nativos `API`, `Procedure`, `SDT`, `Folder` e `File`.
3. O objeto `API` delega às Procedures e persiste corretamente `RestMethod`, `RestPath`, `Description` e `SecurityLevel`.
4. O YAML gerado pelo GeneXus reflete corretamente rotas, métodos, parâmetros, SDTs e nomes com `_API_`. Ver a emenda técnica de 2026-08-04.
5. `Create` e `Update` via BC funcionam com chave simples e composta, preservando regras da Transaction e mensagens do BC.
6. Nos filtros de `List`, a implementação distingue membro JSON ausente de membro presente com vazio, `false` ou zero; no corpo de `Create` e `Update`, o campo obrigatório não preenchido é recusado com `400`. Sem campos públicos `Specified`. Ver a emenda técnica de 2026-08-03.
7. A implementação controla códigos HTTP, corpo da resposta e cabeçalho `Location`.
8. `List` funciona com filtros opcionais, períodos, paginação, totalização e ordenação determinística.
9. Metadados em objeto `File` sobrevivem ao fechamento e à reabertura da KB e permitem reconhecer objetos próprios com segurança.
10. Colisão, regeneração e remoção funcionam sem sobrescrever nem apagar objetos alheios.

Se qualquer gate falhar sem alternativa nativa segura, o desenho será revisto antes de declarar concluído o wizard funcional do MVP.

Não bloquearão o MVP:

- associação visual dos objetos sob a Transaction;
- uso de objeto `Documentation` como fonte de metadados;
- uniformização do corpo de erros produzidos diretamente pelo GAM ou pelo runtime antes da Procedure;
- migração assistida depois de renomear ou mover a Transaction;
- suporte a GeneXus Next, base compartilhada como `api/v1` e otimizações de build.

## Encerramento da consolidação e liberação da implementação

- A auditoria e o alinhamento dos documentos `Foundation` foram concluídos.
- A implementação está liberada para começar pela Sprint 0 — Preparação.
- O estado operacional e a próxima ação executável são mantidos no [checkpoint do projeto](../STATUS_ATUAL_E_PROXIMO_PASSO.md).

## Emenda técnica — 2026-08-04

- **Gate 4 e Gate da Sprint 6**: Aprovados com ressalva em 2026-08-04 após validação nos geradores `.NET Framework / SQL Server` e `.NET / PostgreSQL`.
- Os serviços `List`, `Get`, `Create` e `Update` estão operacionais, seguros e refletidos no YAML gerado pelo GeneXus com rotas, métodos, `operationId`s no padrão `apiNome.Serviço`, schemas `_API_` sem o nível `Errors` em `sdt_API_ErrorResponse` e bloco `security` com `oAuthGXGAM` ativado por serviço (B093).
- A conformidade de identificadores `_API_` e `operationIds` para geradores de cliente OpenAPI foi validada empiricamente via `openapi-generator-cli 5.3.1` (geradores `typescript-fetch` e `csharp`) com 0 erros, gerando os métodos `apiNotaFiscalList`, `apiNotaFiscalGet`, `apiNotaFiscalCreate` e `apiNotaFiscalUpdate` e preservando as classes de modelo `_API_`, além de respaldada pelo teste automatizado `Tests/OpenApiContract/Test-OpenApiClientContractValidity.ps1`.
- A ressalva documental limita-se às respostas HTTP declaradas no YAML nativo, mantidas restritas a 200/404 pelo gerador nativo do GeneXus (`Swagger.Yaml.stg`), enquanto em runtime o serviço `Create` responde HTTP 201 e falhas de autenticação/validação respondem 401/404/400.
