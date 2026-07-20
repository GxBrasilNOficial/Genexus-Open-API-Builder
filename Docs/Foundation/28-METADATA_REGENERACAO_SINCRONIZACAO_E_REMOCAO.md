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
- comparar fingerprint
- classificar divergências

Metadata ausente, corrompida ou incompatível deve bloquear atualização automática.

Propriedade de objetos nunca deve ser reconhecida apenas pelo nome.

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
- nunca apagar Folder reutilizado
- apagar Folder criado pela extensão apenas se ficar vazio
- preservar Folder criado pela extensão quando contiver objetos alheios
- preservar os SDTs compartilhados em `GxOpenAPI`

---

# 8. Colisões

Colisão ocorre quando:

- nome esperado já existe sem metadata compatível
- metadata aponta para objeto ausente
- objeto próprio foi alterado de forma incompatível
- Folder ou objeto compartilhado diverge do esperado

Colisões incompatíveis bloqueiam a geração até decisão explícita.

O MVP não deve sobrescrever, adotar, apagar nem criar sufixos automaticamente para resolver colisão.

Folder preexistente específico da Transaction pode ser reutilizado com aviso. A metadata deve distinguir Folder reutilizado de Folder criado pela extensão.

---

# 9. Critérios de Aceite

- metadata sobrevive ao fechamento e reabertura da KB
- reexecução reencontra objetos próprios sem depender apenas de nome
- colisão externa bloqueia geração
- `_v2` não é criado automaticamente
- remoção lista e remove apenas objetos próprios
- Business Component não é revertido pela remoção
- Folder reutilizado nunca é apagado pela remoção
- SDTs compartilhados em `GxOpenAPI` permanecem ao remover uma API específica
- sincronização apresenta comparação antes de alterar qualquer objeto
