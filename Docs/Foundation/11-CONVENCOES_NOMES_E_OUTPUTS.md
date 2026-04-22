# 11-CONVENCOES_NOMES_E_OUTPUTS.md

## Convenções Oficiais de Nomes e Saídas do MVP

**Projeto:** Genexus Open API Builder  
**Versão:** v1.1  
**Base Primária:** 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md v2.2  
**Dependência direta:** 10-ENGINE_GERACAO_OBJETOS.md v1.1  
**Relacionamento adicional:** 05-ARQUITETURA_FUNCIONAL_MVP.md v3.1 / 08-MODELO_DADOS_E_METADATA.md v1.4  
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

<NomeBase>Api

## Exemplos

| Transaction | Resultado |
|------------|-----------|
| Cliente | ClienteApi |
| Produto | ProdutoApi |
| PedidoVenda | PedidoVendaApi |

## Regra

Primeira escolha oficial no MVP.

[NOM-F11]

---

# 7. SDTs Oficiais

| Finalidade | Padrão |
|-----------|--------|
| Request | <NomeBase>Request |
| Response | <NomeBase>Response |
| Lista | <NomeBase>ListResponse |

## Exemplos

| Transaction | Request | Response | List |
|------------|---------|----------|------|
| Cliente | ClienteRequest | ClienteResponse | ClienteListResponse |
| Produto | ProdutoRequest | ProdutoResponse | ProdutoListResponse |

[NOM-F11]

---

# 8. Nome de Versão Segura (Reexecução)

Quando objeto existir e modo Safe estiver ativo:

<NomeOriginal>_v2  
<NomeOriginal>_v3  
<NomeOriginal>_v4

## Regra obrigatória

Buscar automaticamente o menor sufixo livre disponível.

## Exemplo

Se existir:

- ClienteApi
- ClienteApi_v2
- ClienteApi_v3

Novo nome:

- ClienteApi_v4

[ENG-F10][NOM-F11]

---

# 9. Módulo Destino

## Prioridade

1. escolhido no wizard  
2. módulo da Transaction  
3. módulo raiz da KB

## Regra

Naming não altera módulo automaticamente.

[NOM-F11]

---

# 10. Paths REST Oficiais

## Estratégia MVP

Usar plural simples quando seguro.  
Quando houver dúvida, permitir override manual.

## Regras

| Caso | Resultado |
|------|-----------|
| Cliente | /api/clientes |
| Produto | /api/produtos |
| Pedido | /api/pedidos |
| Item | /api/items |
| Nome incerto | wizard confirma path |

## Heurística básica

- termina com vogal: +s
- termina com m: troca por ns
- termina com s: manter
- caso duvidoso: confirmação manual

[NOM-F11]

---

# 11. Endpoints CRUD Padrão

## Para PK simples

| Método | Path |
|------|------|
| GET | /api/clientes |
| GET | /api/clientes/{id} |
| POST | /api/clientes |
| PUT | /api/clientes/{id} |
| DELETE | /api/clientes/{id} |

## Para chave composta

Fora do escopo automático inicial do MVP.

## Regra

Se detectar PK composta:

- avisar usuário
- bloquear CRUD automático completo
- permitir evolução futura

[DP-F04][NOM-F11]

---

# 12. Campos Sensíveis

## Não expor automaticamente

- senha
- password
- hash
- token
- secret
- audituser
- auditdate

## Exemplos

| Campo | Resultado |
|------|-----------|
| ClienteSenha | omitido |
| UserToken | omitido |
| Nome | mantido |
| Email | mantido |

## Regra

Seguir heurística do doc 08.

[NOM-F11]

---

# 13. Nome de Operações Internas

| Finalidade | Nome |
|----------|------|
| Listar | GetAll |
| Buscar por id | GetById |
| Criar | Create |
| Atualizar | Update |
| Excluir | Delete |

## Observação

Pode variar conforme artefato REST final.

[HP-F11]

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
- nomes versionados
- fallback de colisão

[NOM-F11]

---

# 16. Regras Anti-Ruído

## Não gerar automaticamente

- CliApi
- TblClienteApi
- ClienteSrvX
- ApiClienteMain

## Preferir

- ClienteApi
- ClienteRequest
- ClienteResponse

[NOM-F11]

---

# 17. Idempotência de Naming

Mesma entrada + modo Update:

- tenta mesmo nome original

Mesma entrada + modo Safe:

- usa próximo _vN livre

## Regra

Resultado previsível.

[ENG-F10][NOM-F11]

---

# 18. Critérios de Aceite

| Critério | Resultado Esperado |
|------|--------------------|
| Cliente gera REST | ClienteApi |
| Cliente gera SDTs | ClienteRequest / Response / ListResponse |
| Safe com conflito | ClienteApi_v2 ou próximo livre |
| Produto path base | /api/produtos |
| ClienteSenha | omitido |
| CliApi automático | não gerado |

[NOM-F11]

---

# 19. Uso Correto por Agentes de IA

## Pode assumir

- naming simples vence naming sofisticado
- previsibilidade é prioridade
- sufixos oficiais devem ser respeitados
- doc 10 governa contrato da engine

## Deve tratar com cautela

- pluralizações complexas futuras
- chave composta pós-MVP
- convenções podem evoluir

---

# 20. Próxima Etapa Recomendada

Criar:

12-REGRAS_CRIACAO_API_OBJECTS.md

Para detalhar conteúdo interno dos objetos REST gerados.

---

# 21. Conclusão Objetiva

Se o naming for estável, todo o produto fica mais confiável.

Nomes previsíveis reduzem conflito, facilitam manutenção e melhoram reexecução.