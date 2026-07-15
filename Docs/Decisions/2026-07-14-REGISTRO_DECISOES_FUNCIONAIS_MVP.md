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
- Próxima etapa: executar `B010`, conforme o [checkpoint operacional](../STATUS_ATUAL_E_PROXIMO_PASSO.md).
- Implementação: ainda não iniciada.

## Objetivo e limites do produto

### Decisões aceitas

- O produto deve ser uma extensão executada dentro da IDE GeneXus.
- Seu objetivo central é gerar um objeto `API` oficial e nativo do GeneXus.
- Geração fora da IDE pertence a outros projetos e não satisfaz este objetivo.
- GeneXus 18 é a versão mínima; o ambiente inicial é GeneXus 18 Upgrade 15.
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
- O MVP trabalhará somente com o primeiro nível da Transaction.
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
- Em KB com GAM, as opções do MVP serão `Authentication`, selecionada por padrão, e `None`. Escolher `None` exigirá confirmação explícita antes da geração.
- Em KB sem GAM, `None` será o único valor aplicável e o wizard exibirá aviso explícito de que a API será gerada sem autenticação.
- O valor será gravado explicitamente em cada serviço; a API não poderá ficar silenciosamente pública por causa do padrão implícito `None` do GeneXus.
- O MVP não permitirá níveis diferentes para `List`, `Get`, `Create` e `Update`.
- `Authorization` e `SecurityPermission` ficarão para uma evolução posterior, com permissões coerentes e possivelmente distintas para leitura, criação e alteração.

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
- O objeto `sdtNomeDaTransacao_API_Response` cobrirá todos os atributos do primeiro nível declarados na estrutura da Transaction, incluindo atributos armazenados, inferidos da tabela estendida, fórmulas, partes da chave e campos somente de leitura.
- A extensão não incluirá indiscriminadamente todos os atributos alcançáveis pela tabela estendida que não estejam declarados na estrutura da Transaction.
- Mudanças na Transaction somente chegarão aos contratos por uma ação explícita `Sincronizar com a Transaction`.
- A sincronização comparará a estrutura atual com a metadata da última geração e apresentará atributos adicionados, removidos ou renomeados, mudanças de tipo e mudanças de gravabilidade.
- Nenhuma alteração será aplicada antes da confirmação do usuário.
- Atributo novo no primeiro nível virá proposto e marcado para inclusão no `Response`; nos Requests, dependerá de sua elegibilidade para inclusão ou alteração via BC.
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
- Não incluirá automaticamente atributos da tabela estendida que não apareçam na estrutura, subníveis nem campos sintéticos.
- Preservará a ordem da estrutura da Transaction.
- Cada membro será baseado no atributo original, preservando domínio, tipo, tamanho, decimais, nulabilidade e demais características aplicáveis.
- Os membros usarão exatamente os nomes dos atributos tanto na KB quanto no JSON, como `ProdutoId` e `ProdutoNome`.
- `Get`, `Create`, `Update` e cada item de `List` usarão o mesmo contrato.
- A diferença de caixa será intencional: o envelope genérico usará nomes externos em lower camel case, enquanto os dados da Transaction preservarão os nomes GeneXus.

## `sdtNomeDaTransacao_API_CreateRequest` — estrutura e presença

- Conterá somente os atributos selecionados no wizard e atribuíveis ao BC antes de `Save()`.
- Preservará a ordem da estrutura da Transaction; cada membro será baseado no atributo original e usará exatamente o nome do atributo no SDT, no JSON e no OpenAPI.
- Não conterá envelope, metadados, subníveis nem campos exclusivos de resposta.
- A propriedade `Required` representará que o membro deve estar presente no JSON; presença obrigatória não significará valor obrigatoriamente não vazio.
- Membro obrigatório ausente produzirá `400 Bad Request`.
- Membro opcional ausente não será atribuído ao BC, preservando regras `Default` e preenchimentos automáticos.
- Membro presente com valor vazio, `false` ou `0` será atribuído exatamente como recebido e validado pelas regras da Transaction aplicáveis via BC.
- A API não acrescentará campos auxiliares públicos, como `ProdutoAtivoSpecified`, para indicar a presença de outros membros.
- Antes da implementação, um experimento técnico de validação deverá confirmar como distinguir, usando recursos nativos do GeneXus, um membro ausente de um membro presente com valor vazio, `false` ou `0`.

## `sdtNomeDaTransacao_API_UpdateRequest` — estrutura e presença

- Representará a substituição completa, via `PUT`, dos campos atualizáveis selecionados.
- Não conterá partes da chave primária, pois elas identificarão o registro no `RestPath`.
- Conterá somente atributos selecionados e atribuíveis ao BC carregado antes de `Save()`, preservando ordem, tipos e nomes dos atributos.
- Todos os membros selecionados terão `Required = True`; a ausência de qualquer um produzirá `400 Bad Request` antes de qualquer atribuição ao BC.
- Valores vazios, `false` e `0` serão tratados como valores realmente enviados e submetidos às regras da Transaction aplicáveis via BC.
- O fluxo carregará o BC pela chave simples ou composta, retornará `404` quando não existir, validará a presença integral do Request, atribuirá os valores, salvará via BC, recarregará e devolverá o `Response`.
- Não haverá campos auxiliares públicos com sufixo `Specified`.
- O mesmo experimento técnico de validação do `CreateRequest` deverá comprovar a distinção entre membro ausente e membro presente no GeneXus 18.
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
- A forma exata emitida pelo GeneXus 18 Upgrade 15 deverá ser confirmada por spike e testes na IDE.

## KBs para testes

- KB menor, fora de produção, com backup disponível.
- Cópia de teste da KB principal, atualizada a partir de XPZs da principal.
- A validação começará na KB menor e avançará para a cópia de teste da principal.

## Gates técnicos transversais do MVP

Os seguintes experimentos são gates transversais do MVP. Sua comprovação será progressiva ao longo das Sprints 1–7, de acordo com as dependências de cada contrato; o conjunto completo deve estar aprovado antes do marco **wizard funcional do MVP concluído** e antes da Alpha.

1. A extensão carrega e funciona no GeneXus 18 Upgrade 15.
2. O SDK público permite criar, salvar, reabrir, alterar e excluir objetos nativos `API`, `Procedure`, `SDT`, `Folder` e `File`.
3. O objeto `API` delega às Procedures e persiste corretamente `RestMethod`, `RestPath`, `Description` e `SecurityLevel`.
4. O YAML gerado pelo GeneXus reflete corretamente rotas, métodos, parâmetros, SDTs e nomes com `_API_`.
5. `Create` e `Update` via BC funcionam com chave simples e composta, preservando regras da Transaction e mensagens do BC.
6. A implementação distingue membro JSON ausente de membro presente com vazio, `false` ou zero, sem campos públicos `Specified`.
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

## Próxima etapa executável

> Executar `B010`: localizar e confirmar o SDK de Extensibility do GeneXus 18 Upgrade 15 e criar em `Src` a solution e o projeto mínimos da extensão, com build reproduzível e evidência registrada.
