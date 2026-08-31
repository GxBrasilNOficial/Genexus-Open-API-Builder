# 11-CONVENCOES_NOMES_E_OUTPUTS.md

## Convenções Oficiais de Nomes e Saídas do MVP

**Projeto:** Genexus Open API Builder
**Versão:** v1.0
**Base Primária:** 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md v1.1
**Dependência direta:** 10-ENGINE_GERACAO_OBJETOS.md v1.0
**Relacionamento adicional:** 05-ARQUITETURA_FUNCIONAL_MVP.md v1.1 / 08-MODELO_DADOS_E_METADATA.md v1.0
**Objetivo:** definir padrões obrigatórios de nomenclatura e outputs gerados pelo produto, garantindo previsibilidade, idempotência e manutenção simples.
**Idioma:** Português BR
**Público principal:** Agentes de IA + mantenedores humanos
**Data:** Abril/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- padronizar nomes gerados
- evitar colisões desnecessárias
- facilitar manutenção futura
- permitir reexecução previsível
- reduzir decisões manuais

Este documento **não trata UX**, **não trata SDK**, **não redefine contrato da engine**.

---

# 2. Taxonomia

| Código | Tipo | Significado |
|------|------|-------------|
| DP-F04 | Decisão oficial | Requisito aprovado |
| ENG-F10 | Engine geração | Processo técnico |
| NOM-F11 | Naming/output | Definição deste documento |
| HP-F11 | Hipótese | Pode evoluir no futuro |

---

# 3. Fontes e Rastreabilidade

| Código | Fonte |
|------|-------|
| F04 | REQUISITOS_MVP |
| F05 | ARQUITETURA_FUNCIONAL_MVP |
| F08 | MODELO_DADOS_E_METADATA |
| F10 | ENGINE_GERACAO_OBJETOS |

---

# 4. Estratégia Oficial

No MVP:

1. nomes simples
2. nomes previsíveis
3. nomes derivados da Transaction
4. mínimo de abreviações
5. baixa surpresa ao usuário

[NOM-F11]

---

# 5. Nome Base

## Regra

O nome base padrão será o nome da Transaction selecionada.

## Exemplo

| Transaction | Nome Base |
|------------|-----------|
| Cliente | Cliente |
| Produto | Produto |
| PedidoVenda | PedidoVenda |

## Observação

Não pluralizar no nome base.

[NOM-F11]

---

# 6. Artefato REST Principal

## Padrão

api<NomeBase>

## Exemplos

| Transaction | Resultado |
|------------|-----------|
| Cliente | apiCliente |
| Produto | apiProduto |
| PedidoVenda | apiPedidoVenda |

## Regra

Primeira escolha oficial no MVP.

O nome do objeto `API` é visível e editável no wizard.

O `Services base path` começa com o mesmo valor do objeto `API`, também é visível e editável, e acompanha o nome do objeto enquanto não tiver sido alterado manualmente. Depois de alterado manualmente, deve ser preservado e gravado explicitamente no objeto `API`.

[NOM-F11]

---

# 6.1 File de Metadata

## Padrão

api<NomeBase>_Metadata

## Exemplos

| Transaction | Resultado |
|------------|-----------|
| Cliente | apiCliente_Metadata |
| Produto | apiProduto_Metadata |
| PedidoVenda | apiPedidoVenda_Metadata |

## Regra

O objeto `File` de metadata guarda JSON técnico persistente da API gerada para a Transaction. O nome é derivado do objeto `API` principal e não recebe sufixo de formato, porque o conteúdo JSON é contrato interno do MVP.

Por ser artefato interno da extensão, o objeto `File` de metadata não deve ser extraído para nenhum gerador. Ao criar ou atualizar esse objeto, a extensão deve manter em `False` todas as propriedades de extração por gerador disponíveis no GeneXus, incluindo:

- `Extract for Java Generator`
- `Extract for .Net Generator`
- `Extract for .Net Core Generator`
- `Extract for iOS Generator`
- `Extract for Android Generator`
- `Extract for .NET Framework Generator`, quando disponível em versões/geradores legados
- `Extract`, quando disponível como propriedade legada/deprecated
- `Extract Zip`

Se uma versão futura do GeneXus expuser nova propriedade de extração do `File` para outro gerador, ela deve ser tratada por padrão como não exportável e mantida em `False` até revisão explícita.

[NOM-F11]

---

# 7. SDTs Oficiais

| Finalidade | Padrão |
|-----------|--------|
| Create | sdt<NomeBase>_API_CreateRequest |
| Update | sdt<NomeBase>_API_UpdateRequest |
| Response | sdt<NomeBase>_API_Response |
| Filtros | sdt<NomeBase>_API_ListFilters |
| Lista | sdt<NomeBase>_API_ListResponse |

## Exemplos

| Transaction | Create | Update | Response | List |
|------------|--------|--------|----------|------|
| Cliente | sdtCliente_API_CreateRequest | sdtCliente_API_UpdateRequest | sdtCliente_API_Response | sdtCliente_API_ListResponse |
| Produto | sdtProduto_API_CreateRequest | sdtProduto_API_UpdateRequest | sdtProduto_API_Response | sdtProduto_API_ListResponse |

**Nota de revisão — 2026-08-23 — Suporte a Subníveis:** a tabela acima permanece exata para transação de nível único. Havendo subníveis selecionados, cada subnível gera um SDT próprio **por contrato**, nomeado `sdt<NomeBase>_API_<Papel>_<Subnível>`:

| Finalidade | Padrão | Exemplo |
|-----------|--------|---------|
| Linhas em Create | `sdt<NomeBase>_API_CreateRequest_<Subnível>` | `sdtCliente_API_CreateRequest_Parcelas` |
| Linhas em Update | `sdt<NomeBase>_API_UpdateRequest_<Subnível>` | `sdtCliente_API_UpdateRequest_Parcelas` |
| Linhas em Response | `sdt<NomeBase>_API_Response_<Subnível>` | `sdtCliente_API_Response_Parcelas` |
| Item da lista | `sdt<NomeBase>_API_ListResponse_Item` | `sdtCliente_API_ListResponse_Item` |

**Atualização de 2026-08-29:** SDT aninhado com 0 membros naquele papel não é emitido (Create só com PK herdada); a coleção correspondente não entra no pai. O teto `6 + 3N` permanece o máximo, não a contagem fixa.

O papel permanece imediatamente após `_API_` e o subnível é qualificador, de modo que cada contrato forme um bloco contíguo na ordenação alfabética da Folder, com os derivados logo abaixo do SDT pai. Em profundidade maior que 2, o caminho se acumula no qualificador (`sdtDadosDoDia_API_Response_Turno_Funcionario`). O limite de nome de **objeto** GeneXus 18 é **128** caracteres. A Fase 2 (`B096`) encurta o nome do SDT quando o nome completo estoura 128 ou colide: se a folha do caminho (último segmento) tiver até 32 caracteres, tenta `sdt<Tx>_API_<Papel>_<folha>`; senão, ou se essa forma não couber ou colidir, usa hash SHA-256 de 8 hex. O encurtamento pode truncar o nome da Transaction para caber em 128. Não se aplica a nomes de membro: a coleção e o `<Subnível>Replace` usam o identificador sanitizado do nível, sem teto nesta fase (o ouro `LongQualifier` congela membro de 106 caracteres e `Replace` de 113). Smoke IDE deve confirmar objeto e membro. O `ListResponse_Item` existe **somente** quando há subnível selecionado (B098); sem subníveis, e nesta Fase 2, `Items` continua sendo coleção de `sdt<NomeBase>_API_Response`. Detalhes na `Emenda técnica — 2026-08-23` do registro de decisões do MVP e em `Docs/Implementation/2026-08-26-B096-SDTS-HIERARQUICOS.md`.

O marcador `_API_` faz parte do nome público levado pelo GeneXus para `components/schemas` no OpenAPI. Essa exposição foi aceita conscientemente para favorecer organização e identificação dentro da KB.

O YAML gerado pelo GeneXus e ao menos um gerador de cliente OpenAPI devem validar a compatibilidade prática desses nomes.

[NOM-F11]

---

# 8. Nome de Versão Segura (Reexecução)

Quando objeto existir sem metadata compatível:

- bloquear colisão
- informar objeto conflitante
- exigir decisão explícita

## Regra obrigatória

Não criar `_v2`, `_v3` ou variações automáticas no MVP.

## Exemplo

Se existir `apiCliente` sem metadata compatível, a geração deve bloquear.

[ENG-F10][NOM-F11]

---

# 9. Módulo Destino

## Prioridade

1. módulo da Transaction
2. módulo raiz apenas se o SDK exigir e houver decisão explícita

## Regra

No MVP, o wizard não permite escolher livremente módulo destino.

Objetos específicos da Transaction ficam no mesmo módulo da Transaction. A organização visual preferencial é associação sob a própria Transaction se o SDK público permitir; o fallback é Folder nativo `NomeDaTransacaoOpenApi`.

Os SDTs compartilhados ficam no `Root Module`, dentro do Folder `GxOpenAPI`.

[NOM-F11]

---

# 10. Paths REST Oficiais

## Estratégia MVP

Usar o campo `Caminho comum dos serviços (RestPath)`, hifenizado quando necessário, sem pluralização automática e sem prefixo `/api` implícito.

## Regras

| Caso | Resultado |
|------|-----------|
| Cliente | cliente |
| Produto | produto |
| PedidoVenda | pedido-venda |
| Item | item |
| Nome incerto | wizard confirma RestPath |

## Heurística básica

- converter para minúsculas
- separar palavras por hífen quando necessário
- não pluralizar

`List` e `Create` usam o caminho comum diretamente. `Get` e `Update` acrescentam todas as partes da chave no `RestPath`, preservando ordem e tipos.

O `Services base path` não é o mesmo conceito que o `RestPath`: o primeiro participa da base exposta pelo objeto `API`; o segundo é o caminho comum dos serviços.

[NOM-F11]

---

# 11. Serviços REST Padrão

## Serviços MVP

- `List`
- `Get`
- `Create`
- `Update`
- `Delete` — opt-in, desligado por padrão (`B100`, 2026-08-30)

## Chave composta

Chave simples e composta devem ser suportadas sem degradação parcial.

## Regra

`Delete` não entra no contrato REST enquanto estiver desmarcado. Marcado, usa o mesmo path de chave do `Get` e a Procedure `procNomeDaTransacao_API_Delete`. Chave composta não bloqueia `List`, `Get`, `Create`, `Update` nem o `Delete` opt-in.

## Terminologia

A interface e a documentação usam **serviço**, conforme a terminologia do objeto `API` GeneXus.

“Recurso” pode aparecer apenas em explicações conceituais de REST.

## OperationId

Os `operationId` seguem o nome do objeto `API` e o nome fixo do serviço:

- `apiProduto.List`
- `apiProduto.Get`
- `apiProduto.Create`
- `apiProduto.Update`

`Create` foi escolhido em vez de `Insert`. `Get` foi escolhido em vez de `GetById`, pois a chave pode ser composta.

APIs manuais de negócio continuam livres para usar outras convenções.

[DP-F04][NOM-F11]

---

# 12. Campos Sensíveis

## Regra

Naming não decide exposição de campos. Seguir classificação explícita do documento 08: sensíveis desmarcados com alerta e auditoria tratada separadamente.

[NOM-F11]

---

# 13. Nome de Operações Internas

| Finalidade | Nome |
|----------|------|
| Listar | List |
| Buscar por chave | Get |
| Criar | Create |
| Atualizar | Update |

## Regra

Os nomes dos serviços do MVP são fixos. O artefato REST final não pode alterar implicitamente `List`, `Get`, `Create` e `Update`.

[NOM-F11]

---

# 13.1 Descrições dos Serviços

A extensão deve gerar uma anotação `[Description]` curta e padronizada para cada serviço selecionado.

Regras:

- o wizard não terá campo de descrição
- usar preferencialmente a descrição legível da Transaction
- quando a descrição estiver vazia, usar o nome do objeto
- a descrição continua editável no objeto `API` nativo
- alteração manual posterior não é sobrescrita silenciosamente
- o idioma é escolhido automaticamente pelo idioma principal da KB
- há modelos de descrição em português, espanhol e inglês
- idioma sem modelo usa inglês
- fallback para inglês deve aparecer no resumo da geração
- a descrição legível da Transaction é preservada no idioma original, sem tradução automática
- a metadata deve preservar as descrições geradas e permitir detectar alteração manual

[NOM-F11]

---

# 13.2 Procedures Geradas

Para uma Transaction `NomeDaTransacao`, os nomes das Procedures são:

- `procNomeDaTransacao_API_List`
- `procNomeDaTransacao_API_Get`
- `procNomeDaTransacao_API_Create`
- `procNomeDaTransacao_API_Update`
- `procNomeDaTransacao_API_Delete` — `B100` concluído; gerado somente quando o serviço `Delete` está marcado no Wizard (desligado por padrão)

Regras:

- o prefixo `proc` identifica o tipo de objeto
- `_API_` separa essas Procedures das preexistentes
- nomes são derivados automaticamente e não editáveis no wizard do MVP
- ficam no mesmo módulo e Folder dos demais objetos específicos da Transaction
- a Procedure nomeia a operação, não o Request
- o objeto `API` delega cada serviço à Procedure correspondente

[NOM-F11]

---

# 14. Output Formal Relacionado

O contrato oficial de saída da engine está no documento 10.

Este documento complementa naming para:

- MainObjectName
- CreatedObjects
- UpdatedObjects
- PathsGerados
- WarningsNaming

## Regra

Não substituir o contrato principal do doc 10.

[ENG-F10][NOM-F11]

---

# 15. Relação com ResolvedGenerationPlan

O `ResolvedGenerationPlan` do documento 10 utiliza estas regras para definir:

- nomes finais
- paths finais
- metadata de objetos próprios
- bloqueio de colisão incompatível

[NOM-F11]

---

# 16. Regras Anti-Ruído

## Não gerar automaticamente

- CliApi
- TblClienteApi
- ClienteSrvX
- ApiClienteMain

## Preferir

- apiCliente
- sdtCliente_API_CreateRequest
- sdtCliente_API_Response

[NOM-F11]

---

# 17. Idempotência de Naming

Mesma entrada + modo Update:

- tenta mesmo nome original

Mesma entrada + modo Safe:

- atualiza apenas objetos próprios reconhecidos por metadata
- bloqueia colisão incompatível

## Regra

Resultado previsível.

[ENG-F10][NOM-F11]

---

# 18. Critérios de Aceite

| Critério | Resultado Esperado |
|------|--------------------|
| Cliente gera REST | apiCliente |
| Cliente gera SDTs | sdtCliente_API_CreateRequest / Response / ListResponse |
| Safe com conflito externo | bloqueia colisão |
| Produto RestPath | produto |
| ClienteSenha | desmarcado com alerta |
| CliApi automático | não gerado |

[NOM-F11]

---

# 19. Uso Correto por Agentes de IA

## Pode assumir

- naming simples vence naming sofisticado
- previsibilidade é prioridade
- metadata governa reexecução segura
- doc 10 governa contrato da engine

## Deve tratar com cautela

- pluralizações complexas futuras
- extensões pós-MVP de naming
- convenções podem evoluir

---

# 20. Conclusão Objetiva

Se o naming for estável, todo o produto fica mais confiável.

Nomes previsíveis reduzem conflito, facilitam manutenção e melhoram reexecução.
