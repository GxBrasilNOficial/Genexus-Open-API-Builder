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
- `Delete` fica reservado para evolução futura

## Chave composta

Chave simples e composta devem ser suportadas sem degradação parcial.

## Regra

`Delete` não compõe o endpoint REST do MVP. Chave composta não bloqueia `List`, `Get`, `Create` ou `Update`.

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
