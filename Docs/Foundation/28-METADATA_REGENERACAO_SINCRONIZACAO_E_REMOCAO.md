# 28-METADATA_REGENERACAO_SINCRONIZACAO_E_REMOCAO

## Metadata Persistente, Regeneração e Ciclo de Vida

**Projeto:** Genexus Open API Builder
**Versão:** v1.0
**Base Primária:** Checkpoint funcional de 2026-07-14
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

Campos mínimos:

- versão do schema
- Transaction origem
- módulo origem
- API Object gerado
- Procedures geradas
- SDTs gerados
- Folder usado
- RestPath
- serviços habilitados
- chave primária completa
- fingerprint estrutural da Transaction
- data da geração
- versão do gerador quando disponível

---

# 4. Reencontro de Objetos

Na reexecução, o gerador deve:

- localizar metadata
- validar schema
- conferir se objetos esperados existem
- comparar fingerprint
- classificar divergências

Metadata ausente, corrompida ou incompatível deve bloquear atualização automática.

---

# 5. Regeneração Conservadora

A regeneração só pode atualizar objetos reconhecidos como próprios.

Não usar `_v2` automático para resolver conflito.

Não sobrescrever objeto externo com mesmo nome.

Quando houver edição manual detectável, o fluxo deve preservar, bloquear ou pedir decisão explícita, conforme o tipo de divergência.

---

# 6. Sincronização com a Transaction

A ação de sincronizar deve comparar:

- estrutura atual da Transaction
- metadata gravada
- SDTs próprios
- Procedures próprias
- API Object próprio

A sincronização deve reportar impacto antes de alterar qualquer objeto.

---

# 7. Remoção de API Gerada

Remover API gerada é operação de tooling, não endpoint REST.

Ela deve:

- usar metadata para listar objetos próprios
- mostrar impacto antes de confirmar
- remover apenas objetos próprios
- não remover nem desabilitar a Transaction
- não reverter automaticamente a propriedade Business Component

---

# 8. Colisões

Colisão ocorre quando:

- nome esperado já existe sem metadata compatível
- metadata aponta para objeto ausente
- objeto próprio foi alterado de forma incompatível
- Folder ou objeto compartilhado diverge do esperado

Colisões incompatíveis bloqueiam a geração até decisão explícita.

---

# 9. Critérios de Aceite

- metadata sobrevive ao fechamento e reabertura da KB
- reexecução reencontra objetos próprios sem depender apenas de nome
- colisão externa bloqueia geração
- `_v2` não é criado automaticamente
- remoção lista e remove apenas objetos próprios
- Business Component não é revertido pela remoção
