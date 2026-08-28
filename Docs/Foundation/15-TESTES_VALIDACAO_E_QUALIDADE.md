# 15-TESTES_VALIDACAO_E_QUALIDADE.md

## Regras Oficiais de Testes, Validação e Critérios de Qualidade do MVP

**Projeto:** Genexus Open API Builder
**Versão:** v1.0
**Base Primária:** 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md v1.1
**Dependência direta:** 10-ENGINE_GERACAO_OBJETOS.md v1.0
**Relacionamento adicional:** 12-REGRAS_CRIACAO_API_OBJECTS.md v1.0 / 13-REUSO_E_GERACAO_SDTS.md v1.0 / 14-CONFLITOS_REEXECUCAO_E_VERSIONAMENTO.md v1
**Objetivo:** definir como validar se o produto gera objetos corretos, previsíveis e seguros antes de ser considerado pronto para uso interno.
**Idioma:** Português BR
**Público principal:** Agentes de IA + mantenedores humanos
**Data:** Abril/2026
**Última revisão:** Julho/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- padronizar critérios de pronto
- reduzir regressões
- validar geração automática
- medir estabilidade mínima
- apoiar evolução segura

Este documento **não define roadmap**, **não substitui QA humano**, **não trata marketing**.

Os testes de `List` devem cobrir o contrato de `26-CONTRATO_FILTROS_PAGINACAO_ORDENACAO.md`. Os testes de HTTP, erros e SDTs compartilhados devem cobrir `27-CONTRATO_HTTP_ERROS_E_SDTS_COMPARTILHADOS.md`. Os testes de metadata, regeneração, sincronização e remoção devem cobrir `28-METADATA_REGENERACAO_SINCRONIZACAO_E_REMOCAO.md`.

---

# 2. Taxonomia

| Código | Tipo | Significado |
|------|------|-------------|
| ENG-F10 | Engine geração | Processo técnico |
| API-F12 | Objetos REST | Saída funcional |
| SDT-F13 | SDTs | Estruturas auxiliares |
| CFG-F14 | Conflitos/versionamento | Segurança operacional |
| QA-F15 | Qualidade/testes | Definição deste documento |
| HP-F15 | Hipótese | Pode evoluir |

---

# 3. Fontes e Rastreabilidade

| Código | Fonte |
|------|-------|
| F10 | ENGINE_GERACAO_OBJETOS |
| F12 | REGRAS_CRIACAO_API_OBJECTS |
| F13 | REUSO_E_GERACAO_SDTS |
| F14 | CONFLITOS_REEXECUCAO_E_VERSIONAMENTO |

---

# 4. Estratégia Oficial

No MVP:

1. testar fluxo principal primeiro
2. automatizar o que for repetível
3. validar cenários de erro
4. priorizar previsibilidade
5. corrigir regressão antes de expandir features

[QA-F15]

---

# 5. Pirâmide de Testes do MVP

| Nível | Foco |
|------|------|
| Unitário | regras puras |
| Integração | SDK + geração |
| Funcional | objeto gerado utilizável |
| Regressão | reruns e versões |
| Manual guiado | UX final |

[QA-F15]

---

# 6. Casos Unitários Obrigatórios

## Validar funções puras

- naming base
- bloqueio de `_v2` automático
- RestPath singular sem pluralização automática
- distinção entre `Services base path` e RestPath
- classificação explícita de sensíveis/auditoria
- reencontro de SDT próprio por metadata
- decisão Safe / Update / Cancel
- seleção de filtros, operadores, períodos e intervalos conforme documento 26
- cálculo de `totalCount` e `totalPages`
- distinção entre ausência, string vazia, `false` e `0`
- nomes fixos de serviço e `operationId` no padrão `apiNome.Serviço`
- seleção de modelo de `[Description]` por idioma da KB e fallback para inglês
- parser semântico de `Service Source` B054/B055, cobrindo vínculo serviço-Procedure, argumentos, módulo esperado e rejeição de divergências antes do primeiro `Save()`

## Resultado esperado

Determinístico.

[QA-F15]

---

# 7. Casos de Integração Obrigatórios

## Ambiente real GeneXus

- extensão carrega
- wizard abre
- metadata lida
- objeto criado
- save executa
- logs retornam

## Resultado esperado

Fluxo completo sem travar IDE.

[QA-F15]

---

# 8. Casos Funcionais REST

## Transaction simples Cliente

Validar geração de:

- apiCliente
- procCliente_API_List/Get/Create/Update
- sdtCliente_API_CreateRequest
- sdtCliente_API_UpdateRequest
- sdtCliente_API_Response
- sdtCliente_API_ListFilters
- sdtCliente_API_ListResponse
- sdt_API_ErrorMessage
- sdt_API_ErrorResponse
- sdt_API_Pagination

## Validar rotas

- List
- Get
- Create
- Update
- Create com status 201
- Update com PUT e status 200
- List sem resultados com 200, coleção vazia e totais zero
- operationIds no padrão `apiCliente.List`, `apiCliente.Get`, `apiCliente.Create` e `apiCliente.Update`
- descrições `[Description]` curtas nos serviços selecionados

Não deve existir endpoint `Delete` no MVP.

**Nota de revisão — 2026-08-23:**

**Quanto ao `Delete` (`B100`):** o critério acima vale **integralmente** enquanto o `B100` não for implementado. A lista de serviços gerados é fechada em `List`, `Get`, `Create` e `Update` (`PrototypeWizardContract.ServiceNames`); não existe checkbox de `Delete` no Wizard, nem Procedure, nem rota. Um teste na forma absoluta é hoje o comportamento **correto**, não um falso positivo.

**Condicionamento futuro, quando o `B100` for entregue:** o critério passa a ser: não deve existir endpoint `Delete` com o serviço desmarcado — que será o padrão, desligado; marcado, valem `procCliente_API_Delete`, a rota `Delete`, o `operationId` `apiCliente.Delete` e o contrato `200` com a chave removida, `404` em registro inexistente e `422` com `Code = "validation_error"` na recusa do Business Component. Este parágrafo descreve estado futuro e **não** afrouxa o critério corrente. **Correção de 2026-08-24:** a redação anterior condicionava o critério desde já, descrevendo como vigente uma UI que não existe — um `Delete` parcialmente implementado não seria reprovado por ela.

**Quanto a subníveis (B095–B099):** o critério abaixo é da frente completa, não do runtime atual. Hoje (`B099v` concluído em 2026-08-28): o Wizard poda `Levels` e o apply hierárquico é permitido; o plano offline emite SDTs derivados, Source BC com `<Subnível>Replace` e `List` com `ListResponse_Item` + contadores; smoke HTTP multinível nos dois environments comprovou rotas hierárquicas (POST/GET/PUT com e sem Replace, List com contadores). Metadata/sync/remoção continuam V1 até `B099b`. Required de linha é só UI. Gates `tests.wizardHierarchical`, `tests.sdtHierarchicalPlan`, `tests.businessComponentHierarchical`, `tests.listHierarchical`.

Quando a frente B095–B099 estiver entregue, a validação de geração deve acrescentar os SDTs derivados por contrato (`sdtCliente_API_CreateRequest_<Subnível>`, `sdtCliente_API_UpdateRequest_<Subnível>`, `sdtCliente_API_Response_<Subnível>`) e, quando houver subnível selecionado, o `sdtCliente_API_ListResponse_Item`. A validação de rotas deve acrescentar: coleções preenchidas no `Get`; substituição de linhas no `Update` somente com `<Subnível>Replace = True`; preservação das linhas quando o marcador está ausente; contadores `<Subnível>Count` na listagem; e ausência dos membros de coleção nos elementos de `items`. Em transação de nível único, todos os critérios originais permanecem exatamente como estão, inclusive `items` como coleção de `sdtCliente_API_Response`.

**Onde esse critério é exercitado — 2026-08-28.** A parte de **geração** do parágrafo acima (SDTs derivados por contrato e `ListResponse_Item`) está coberta offline pelos gates `tests.sdtHierarchicalPlan`, `tests.businessComponentHierarchical` e `tests.listHierarchical`. A parte de **rotas** — coleções preenchidas no `Get`, substituição de linhas só com `<Subnível>Replace = True`, preservação quando o marcador está ausente, contadores na listagem e ausência dos membros de coleção em `items` — foi exercitada na Fase 5-A (`B099v`): smoke HTTP na `apiTeste` de quatro níveis nos dois environments (2026-08-28). Evidência: `Docs/Implementation/2026-08-28-B099v-VALIDACAO-RUNTIME-MULTINIVEL.md`. Critério 9 (YAML publicado + geração de cliente) fechado no mesmo recorte.

**Não regressão:** a saída gerada para transação de nível único deve permanecer idêntica à linha de base capturada na Fase 0, com o escopo dividido em duas camadas, porque "byte a byte" só é alcançável em parte da saída:

- **comparação automática, byte a byte, no checker pré-push:** Source das Procedures `Create`, `Update`, `Get` e `List`, Service Source do API Object e plano de SDT serializado, produzidos offline a partir de um `ApiPlan` sintético;
- **conferência manual, no início e no fim da sprint:** a forma física dos SDTs já presentes na KB, por export/rematerialização XPZ — `SDTStructure` depende do modelo da KB e não sai sem ela; a captura de início **não** exige regenerar a API nem instalar a DLL do dia (âncora de deriva na KB; paridade com o emissor atual é da camada offline);
- **recaptura da linha de base** somente em commit isolado, contendo apenas os arquivos de referência e a justificativa da mudança de saída.

## Resultado esperado

Estrutura pronta para teste inicial.

[API-F12][QA-F15]

---

# 9. Casos de SDT

## Reencontro

SDT próprio por metadata deve ser reencontrado.

## Novo

SDT externo incompatível deve bloquear se colidir.

## Sensível

Campos senha/token devem iniciar desmarcados com alerta.

## Contratos compartilhados

`sdt_API_ErrorResponse` deveria conter `Code`, `Message` e `Errors[]` com `Code`, `Message` e `Field` (critério original do MVP).

Critério revisto em 2026-08-03: `Errors[]` foi retirado do SDT gerado, porque a geração nunca chegou a preenchê-lo e ele aparecia no contrato público como array sempre vazio. O critério passou a ser `sdt_API_ErrorResponse` conter `Code` e `Message`. Ver a nota de revisão da seção 3 do documento 27 e `Docs/Implementation/2026-08-03-CONTRATO-OPENAPI-GAPS.md`.

Complemento de 2026-08-24: o experimento da coleção foi aceito na IDE. O critério vigente passa a exigir `Code`, `Message` (`LongVarChar` 2097152, truncada pela geração em cerca de 2K) e `Messages[]` preenchido com as mensagens de **erro** do Business Component. O texto genérico permanece apenas quando o repasse está desligado ou não há erro do BC. Gate HTTP fechado na mesma data nos dois environments (`apiTeste`): acento, truncamento visível, `Msg()` (tipo 0) excluído, YAML sem `maxLength` e com `Messages` publicado.

`sdt_API_Pagination` deve conter `Page`, `PageSize`, `TotalCount` e `TotalPages`.

[SDT-F13][QA-F15]

---

# 10. Casos de Conflito

| Cenário | Resultado Esperado |
|--------|--------------------|
| apiCliente externo existe + Safe | bloqueia |
| apiCliente externo existe + Cancel | aborta |
| apiCliente próprio + Update | tenta atualizar |
| dúvida estrutural | fallback seguro |

[CFG-F14][QA-F15]

---

# 10.1 Gates Técnicos Transversais do MVP

Validar progressivamente nas Sprints 1–7 e aprovar o conjunto antes do marco **wizard funcional do MVP concluído** e antes da Alpha:

1. extensão carrega no GeneXus 18 U14 ou posterior, com U15 como ambiente inicial
2. SDK cria, salva, reabre, altera e exclui objetos nativos `API`, `Procedure`, `SDT`, `Folder` e `File`
3. objeto `API` delega às Procedures e persiste `RestMethod`, `RestPath`, `Description` e `SecurityLevel`
4. YAML gerado pelo GeneXus reflete rotas, métodos, parâmetros, SDTs e nomes `_API_` (aprovado com ressalva das respostas HTTP declaradas 200/404 no YAML nativo)
5. `Create` e `Update` via BC funcionam com chave simples e composta, preservando regras e mensagens
6. filtro de `List` ausente é distinguido de vazio, `false` e zero, e campo obrigatório não preenchido é recusado com 400, sem membros públicos `Specified`
7. implementação controla códigos HTTP, corpo e `Location`, respeitando seu caráter opcional
8. `List` funciona com filtros opcionais, períodos, paginação, totalização e ordenação determinística
9. metadata em `File` sobrevive a fechar/reabrir a KB e reconhece objetos próprios
10. colisão, regeneração e remoção não sobrescrevem nem apagam objetos alheios

Se qualquer gate falhar sem alternativa nativa segura, o desenho deve ser revisto antes de declarar concluído o wizard funcional do MVP.

Não são bloqueadores: associação visual sob a Transaction, uso de objeto `Documentation` como fonte de metadata, uniformidade de erros interceptados antes da Procedure, migração assistida após renomear ou mover Transaction, GeneXus Next, base compartilhada `api/v1` e otimizações de build.

---

# 11. Casos de Reexecução

Gerar duas vezes mesma Transaction.

## Validar

- previsibilidade naming
- ausência de overwrite indevido
- logs corretos
- rerun consistente

[QA-F15]

---

# 12. Casos Negativos

## Entradas inválidas

- sem Transaction
- módulo inexistente
- KB sem permissão
- metadata incompleta
- nome inválido
- `page` ou `pageSize` inválidos
- filtro ou período inválido
- membro obrigatório ausente em Create ou Update

## Resultado esperado

Erro claro + sem lixo técnico novo.

[QA-F15]

---

# 13. Critérios de Qualidade do Código Gerado

| Critério | Esperado |
|---------|----------|
| Nome legível | Sim |
| Estrutura editável | Sim |
| Dependência oculta crítica | Não |
| Duplicação extrema | Não |
| Campos sensíveis expostos | Não |
| Campos públicos `Specified` | Não |
| Stack trace público em erro | Não |

[QA-F15]

---

# 14. Critérios de Performance (Meta)

| Cenário | Meta |
|--------|------|
| Transaction pequena | rápida |
| média | aceitável |
| grande | degradar controlado |

## Regra

Sem travar IDE.

[HP-F15]

---

# 15. Critérios de Estabilidade

Após múltiplas execuções:

- sem crash da extensão
- sem corrupção aparente KB
- sem crescimento anormal de erro
- logs utilizáveis

[QA-F15]

---

# 16. Critérios de Pronto Interno

Para uso interno inicial:

- fluxo principal passa
- conflito Safe confiável
- geração básica REST pronta
- SDTs corretos
- erros compreensíveis
- sem bug crítico aberto

[QA-F15]

---

# 17. Critérios de Não Pronto

Bloquear liberação se houver:

- overwrite indevido
- perda de objetos
- crash recorrente IDE
- geração inconsistente
- vazamento de campos sensíveis

[QA-F15]

---

# 18. Evidências Esperadas

Registrar:

- prints
- logs
- KB teste usada
- versão GeneXus
- casos executados
- falhas encontradas

Para `B010`, centralizar ambiente, descoberta das dependências, comando e resultado do build em `Docs/Implementation/B010-SDK-E-BUILD-MINIMO.md`. Esse registro comprova somente o build mínimo; o carregamento na IDE continua pertencendo a `B000`.

[QA-F15]

---

# 18.1 KBs de Teste

A validação deve começar por uma KB menor, fora de produção, com backup disponível.

Depois, deve avançar para uma cópia de teste atualizada da KB principal.

Nenhuma operação de validação deve ser feita diretamente na KB principal de produção.

---

# 19. Uso Correto por Agentes de IA

## Pode assumir

- teste principal vem antes de edge case
- regressão precisa repetir cenários
- logs são parte do produto
- qualidade inclui segurança

## Deve tratar com cautela

- performance depende ambiente
- REST real depende artefato final
- UX final precisa teste humano

---

# 20. Conclusão Objetiva

No MVP, qualidade significa confiança operacional.

Se gerar certo repetidas vezes sem quebrar ambiente, está no caminho correto.
