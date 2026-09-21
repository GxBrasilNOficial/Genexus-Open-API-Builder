# 28-METADATA_REGENERACAO_SINCRONIZACAO_E_REMOCAO

## Metadata Persistente, Regeneração e Ciclo de Vida

**Projeto:** Genexus Open API Builder
**Versão:** v1.0
**Base Primária:** [Registro de decisões funcionais do MVP — 2026-07-14](../Decisions/2026-07-14-REGISTRO_DECISOES_FUNCIONAIS_MVP.md)
**Objetivo:** definir como o MVP identifica objetos gerados, regenera com segurança, sincroniza com a Transaction e remove APIs geradas.
**Idioma:** Português BR
**Público principal:** mantenedores humanos, colaboradores técnicos e apoio por IA
**Data:** Julho/2026

---

# 1. Papel do Documento

Este documento é fonte normativa para:

- metadata persistente em objeto `File`
- reencontro de objetos próprios
- regeneração conservadora
- sincronização com a Transaction
- colisões
- remoção de API gerada

Ele deve ser referenciado por 08, 10, 13, 14, 15 e 24.

---

# 2. Princípio Central

O MVP não deve inferir propriedade de objetos apenas por nome.

Objetos pertencem a uma API gerada quando a metadata persistente os identifica como tal.

---

# 3. Persistência

A metadata deve ser gravada em objeto `File` da KB, em JSON.

O nome do objeto `File` de metadata deve seguir `api<NomeBase>_Metadata`, usando o mesmo nome base da `Transaction` que origina a API.

O objeto `File` de metadata é interno da extensão e não deve ser exportado para artefatos de nenhum gerador. A criação e a atualização desse objeto devem garantir `False` em todas as propriedades de extração por gerador disponíveis no GeneXus, incluindo `Extract for Java Generator`, `Extract for .Net Generator`, `Extract for .Net Core Generator`, `Extract for iOS Generator`, `Extract for Android Generator`, `Extract for .NET Framework Generator` quando disponível, `Extract` legado/deprecated quando disponível e `Extract Zip`.

Novas propriedades futuras de extração de `File` por gerador devem ser classificadas conservadoramente como não exportáveis e mantidas em `False` até revisão explícita.

Campos mínimos, conforme aplicável:

- versão do schema
- Transaction origem
- módulo da Transaction
- objeto `API` gerado
- `Services base path`
- RestPath
- serviços habilitados
- chave primária completa
- Folder específico usado e indicação de criado ou reutilizado
- Procedures próprias
- SDTs próprios
- SDTs compartilhados usados
- campos selecionados de `CreateRequest`
- campos selecionados de `UpdateRequest`
- obrigatoriedade no payload
- filtros selecionados
- operadores de filtros
- períodos e intervalos configurados
- paginação padrão e máxima
- ordenação e direções
- `Security Level`
- descrições geradas
- idioma usado nas descrições
- fallback de descrição para inglês quando aplicável
- dados para detectar alteração manual posterior nas descrições dos serviços
- hash de integridade das descrições geradas
- hash do contrato planejado essencial
- dados de integridade do API Object próprio, do Service Source esperado e do contrato semântico validado
- fingerprint estrutural da Transaction
- data da geração
- versão do gerador quando disponível
- dados necessários para reconhecer propriedade
- dados necessários para detectar alterações manuais

---

# 4. Reencontro de Objetos

Na reexecução, o gerador deve:

- localizar metadata
- validar schema
- conferir se objetos esperados existem
- comparar fingerprint do JSON persistido (relido sem converter ISO-8601 em `DateTime`)
- classificar divergências

Metadata ausente, corrompida ou incompatível deve bloquear atualização automática.

Propriedade de objetos nunca deve ser reconhecida apenas pelo nome.

Quando a metadata possuir bloco de integridade versionado, a reexecução deve validar esse bloco antes de classificar o API Object como próprio. Divergência nas descrições geradas, no contrato planejado essencial ou no contrato semântico do Service Source bloqueia a execução antes de qualquer `Save()`. Hash textual do Service Source pode ser persistido como evidência, mas não deve substituir a validação semântica.

---

# 5. Regeneração Conservadora

A regeneração só pode atualizar objetos reconhecidos como próprios.

Não usar `_v2` automático para resolver conflito.

Não sobrescrever objeto externo com mesmo nome.

Quando houver edição manual detectável, o fluxo deve preservar, bloquear ou pedir decisão explícita, conforme o tipo de divergência.

Antes de qualquer gravação, a extensão deve verificar todos os nomes planejados para a execução. Se houver uma colisão, nenhum objeto planejado deve ser criado ou alterado.

---

# 6. Sincronização com a Transaction

A ação de sincronizar deve comparar:

- estrutura atual da Transaction
- metadata gravada
- SDTs próprios
- Procedures próprias
- API Object próprio

A sincronização deve reportar impacto antes de alterar qualquer objeto.

O relatório deve cobrir:

- campos adicionados
- campos removidos
- campos renomeados
- mudanças de tipo
- mudanças de gravabilidade
- riscos de quebra por novo campo obrigatório ou nova regra aplicável via BC
- conflitos em SDTs editados manualmente

Nenhuma alteração deve ser aplicada antes da confirmação do usuário.

---

# 7. Remoção de API Gerada

Remover API gerada é operação de tooling, não endpoint REST.

A remoção ocorre somente pelo comando explícito `Remover API gerada`.

Desinstalar a extensão da IDE não remove objetos da KB.

Ela deve:

- usar metadata para listar objetos próprios
- mostrar impacto antes de confirmar
- remover apenas objetos próprios
- não remover nem desabilitar a Transaction
- não reverter automaticamente a propriedade Business Component
- apagar Folder só com posse histórica (`ownedByThisApi`), GUID coincidente quando existir, Description canônica ou legada, contêiner esperado e Folder vazio — reutilizado sem posse permanece
- preservar Folder próprio quando contiver objetos alheios, Description divergente ou contêiner inesperado
- GUID persistido divergente do Folder na KB preserva (homônimo de terceiro)
- preservar os SDTs compartilhados em `GxOpenAPI`

**Build após remoção (evidência B099b, 2026-08-28).** Remover apaga objetos do **Design**; caches de especificação e artefatos gerados por **environment** (`GXSPC*`, `GeneXus.Programs.Common.sdts.targets`, `type_Sdt*.cs`) podem continuar referenciando SDTs removidos. Work With Objects limpo no Design **não** garante Build All incremental limpo. Após Remover, tratar **Rebuild All por environment** como passo operacional recomendado — em KBs grandes o custo de horas evita falha tardia na compilação por SDT órfão. Evidência: `Docs/Implementation/2026-08-28-B099b-METADATA-HIERARQUICA-V2.md` (seção build pós-Remover).

---

# 8. Colisões

Colisão ocorre quando:

- nome esperado já existe sem metadata compatível
- metadata aponta para objeto ausente
- objeto próprio foi alterado de forma incompatível
- Folder ou objeto compartilhado diverge do esperado

Colisões incompatíveis bloqueiam a geração até decisão explícita.

O MVP não deve sobrescrever, adotar, apagar nem criar sufixos automaticamente para resolver colisão.

Folder preexistente específico da Transaction pode ser reutilizado com aviso. A metadata distingue a operação corrente (`wasCreated`) da posse histórica (`ownedByThisApi`); reuso sem posse não autoriza exclusão.

---

# 9. Critérios de Aceite

- metadata sobrevive ao fechamento e reabertura da KB
- reexecução reencontra objetos próprios sem depender apenas de nome
- colisão externa bloqueia geração
- `_v2` não é criado automaticamente
- remoção lista e remove apenas objetos próprios
- Business Component não é revertido pela remoção
- Folder sem posse histórica (`ownedByThisApi=false`) não é apagado; com posse, só com GUID coincidente, Description própria, contêiner esperado e vazio
- SDTs compartilhados em `GxOpenAPI` permanecem ao remover uma API específica
- sincronização apresenta comparação antes de alterar qualquer objeto

---

# 10. Nota de revisão — 2026-08-23 — Suporte a Subníveis

As decisões da `Emenda técnica — 2026-08-23` alcançam este contrato em quatro pontos. As seções acima permanecem válidas para transação de nível único.

**Persistência por nível.** "Campos selecionados de `CreateRequest`" e "campos selecionados de `UpdateRequest`" passam a ser registrados **por nível**, junto da hierarquia (nome do nível, profundidade, nível pai, ordem e chave primária própria). É o que a Fase 6 grava e o que a Fase 7 relê ao reabrir o Wizard.

**Versão do schema.** A metadata passa de `schemaVersion` V1 para V2. A leitura aceita as duas versões — V1 é interpretada como transação de nível único —, a gravação emite sempre V2, e a conversão ocorre somente quando a geração é aplicada, nunca na simples abertura do Wizard. Sem essa tolerância, toda API gerada na Alpha ficaria simultaneamente irreencontrável e irremovível, já que reencontro e remoção validam o carimbo.

**Inventário próprio deixa de ser fixo (Fase 7).** "SDTs próprios" passa a incluir os derivados de subnível (`sdt<NomeBase>_API_<Papel>_<Subnível>`) e, quando houver subnível selecionado, o `sdt<NomeBase>_API_ListResponse_Item`. A remoção lê esses nomes da metadata (`objects.sdts.own` quando presente; senão inventário dinâmico a partir de `levels`) em vez de assumir lista fechada, sob pena de deixar órfãos na KB. A ordem de exclusão parte da dependência entre tipos. **Desde a `S-B111 F3` ela é o ponto de partida de uma fila em passadas, não a garantia de que tudo sai de primeira** — ver a nota 11. **Desde `B099b`/`Fase 7`:** `ApiPlanGeneratedApiRemovalInventory` resolve `own` gravado ou reconstrói via stub `ApiPlan` + plano de SDT; fallback flat nos cinco nomes fixos só quando não há hierarquia ou o stub não monta (ex.: SDTs raiz ausentes). Se `levels` anuncia hierarquia mas está ilegível, a remoção falha — não cai no flat.

**Sincronização hierárquica.** A comparação com a Transaction passa a percorrer a árvore de níveis, confrontando adições, remoções e renomeações dentro de cada nível por `attributeGuid`, e tratando explicitamente o caso de um subnível inteiro deixar de existir na estrutura.

---

# 11. Nota de revisão — 2026-09-15 — a remoção passou a ter intenção durável

A `S-B111 F3` mudou o contrato de remoção descrito acima em três pontos, e este documento não o
acompanhava. As seções 1 a 10 continuam válidas no que dizem sobre **o que** é próprio e **o que**
é preservado; o que envelheceu foi **como** a exclusão acontece.

**A ordem deixou de ser garantia.** O texto da nota 10 dizia que "a ordem de exclusão continua
respeitando a dependência entre tipos", e isso bastava quando a exclusão era uma passagem única.
A ordenação canônica por tipo permanece — `ApiPlanRemovalIntent.Order` ordena por
`RemovalRank(ObjectType)` —, mas ela é agora **o ponto de partida de uma fila**, não a promessa de
que tudo sai de primeira. Um alvo que a IDE recusa por dependência volta para a fila e é tentado
na passada seguinte, com orçamento `max(1, itens da fila)`. A erosão começou antes desta frente:
em 2026-09-06 o laço de SDTs já tinha deixado de depender da ordem perfeita, e a F3 estendeu isso
a todos os tipos removíveis — API Object, Procedures, metadata e Folder na mesma fila.

**A intenção é registrada antes da primeira exclusão.** Existe um File único por KB,
`GxOpenApiBuilder_OperationJournal`, onde o inventário completo dos alvos — com a identidade de
cada um — é gravado **antes** do primeiro `Delete()`. É o que permite saber o que faltava depois
que o File de metadata sai da KB: ele é o penúltimo da fila, e sem o diário não há outra fonte.

**Remover duas vezes a mesma API deixou de ser aceito em silêncio.** Um alvo previsto que já não
está na KB antes da exclusão encerra a operação em `Partial` com `TargetAbsentBeforeDelete`, porque
quem apagou aquele objeto não foi esta operação. A saída é o comando **`Recuperar operação
interrompida`**, que relê cada alvo por identidade e fecha o registro.

Contrato completo em `Docs/Implementation/2026-09-04-B111-F3-PLANO-DURABILIDADE-E-REMOCAO.md`;
o que foi exercido na IDE, em `Docs/Implementation/2026-09-14-S-B111-F3-P8-VALIDACAO-IDE.md`.

---

# 12. Nota de revisão — 2026-09-20 — posse histórica do Folder (`B123`)

As seções 7 e 9 ainda falavam em «nunca apagar Folder reutilizado» e em distinguir só
criado vs reutilizado. Isso envelheceu quando a metadata passou a `GOAB_API_METADATA_B060_V4`:
`wasCreated` descreve a operação corrente; `ownedByThisApi` autoriza a fila de remoção;
`guid` identifica o Folder e, se divergir do presente na KB, a exclusão é recusada.
Um Folder reencontrado com posse histórica pode ser apagado se ficar vazio — e só se
Description e contêiner ainda forem os esperados em `DeleteOwnFolder`. O texto das
seções 7/9 foi alinhado nesta mesma data; o contrato detalhado permanece no plano
`Docs/Implementation/2026-09-20-B123-PLANO-POSSE-HISTORICA-FOLDER.md` e no documento 08
(remissão `B123`).
