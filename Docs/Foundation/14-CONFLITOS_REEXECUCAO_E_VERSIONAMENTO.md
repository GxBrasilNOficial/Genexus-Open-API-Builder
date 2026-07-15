# 14-CONFLITOS_REEXECUCAO_E_VERSIONAMENTO.md

## Regras Oficiais de Conflitos, Reexecução e Ciclo de Vida no MVP

**Projeto:** Genexus Open API Builder
**Versão:** v1.0
**Base Primária:** 10-ENGINE_GERACAO_OBJETOS.md v1.0
**Dependência direta:** 11-CONVENCOES_NOMES_E_OUTPUTS.md v1.0
**Relacionamento adicional:** 12-REGRAS_CRIACAO_API_OBJECTS.md v1.0 / 13-REUSO_E_GERACAO_SDTS.md v1.0
**Objetivo:** definir como o produto reage quando já existem objetos prévios, nomes ocupados ou nova geração sobre a mesma Transaction.
**Idioma:** Português BR
**Público principal:** Agentes de IA + mantenedores humanos
**Data:** Abril/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- evitar sobrescrita indevida
- garantir reexecução previsível
- reduzir dano em KB existente
- padronizar regeneração conservadora
- permitir evolução segura

Este documento **não define SDK**, **não trata UX detalhada**, **não redefine naming base**.

O contrato de metadata, regeneração, sincronização e remoção está em `28-METADATA_REGENERACAO_SINCRONIZACAO_E_REMOCAO.md`.

---

# 2. Taxonomia

| Código | Tipo | Significado |
|------|------|-------------|
| ENG-F10 | Engine geração | Processo técnico |
| NOM-F11 | Naming oficial | Convenções |
| API-F12 | Objetos REST | Estruturas geradas |
| SDT-F13 | SDTs | Reuso e criação |
| CFG-F14 | Conflitos/versionamento | Definição deste documento |
| HP-F14 | Hipótese | Pode evoluir |

---

# 3. Fontes e Rastreabilidade

| Código | Fonte |
|------|-------|
| F10 | ENGINE_GERACAO_OBJETOS |
| F11 | CONVENCOES_NOMES_E_OUTPUTS |
| F12 | REGRAS_CRIACAO_API_OBJECTS |
| F13 | REUSO_E_GERACAO_SDTS |

---

# 4. Estratégia Oficial

No MVP:

1. preservar ativos existentes
2. preferir criação segura
3. evitar overwrite automático
4. tornar rerun previsível
5. permitir update controlado

[CFG-F14]

---

# 5. Conceitos Oficiais

| Termo | Definição |
|------|-----------|
| Conflito | nome já ocupado ou estrutura incompatível |
| Reexecução | nova geração para mesma Transaction |
| Metadata | File JSON que identifica objetos próprios |
| Update | atualização controlada de objeto próprio |
| Cancel | abortar geração |

[CFG-F14]

---

# 6. Tipos de Conflito

| Tipo | Exemplo |
|------|---------|
| Nome ocupado | apiCliente já existe sem metadata compatível |
| SDT incompatível | sdtCliente_API_Response existe sem metadata compatível |
| Módulo divergente | objeto no módulo errado |
| Tipo divergente | nome igual para tipo diferente |
| Bloqueio técnico | objeto indisponível para update |

[CFG-F14]

---

# 7. Modos de Execução

| Modo | Comportamento |
|------|---------------|
| Safe | atualiza apenas objetos próprios reconhecidos |
| Update | tenta atualizar próprios compatíveis |
| Cancel | aborta ao primeiro conflito |

## Default MVP

Safe.

[ENG-F10][CFG-F14]

---

# 8. Regra Geral de Decisão

Conflito detectado
→ identificar tipo
→ verificar modo atual
→ aplicar ação segura
→ registrar resultado

[CFG-F14]

---

# 9. Modo Safe

## Comportamento

Se nome ocupado sem metadata compatível:

bloquear geração.

## Exemplos

- apiCliente existente externo → bloqueia
- sdtCliente_API_Response externo → bloqueia
- objeto próprio com metadata compatível → atualiza conservadoramente

## Regra

Não criar `_v2` automaticamente.

[NOM-F11][CFG-F14]

---

# 10. Modo Update

## Aplicar somente quando:

- tipo do objeto coincide
- ownership compatível
- estrutura base reconhecível
- risco aceitável

## Exemplos possíveis

- adicionar rota ausente
- ajustar referência SDT
- completar metadata gerada

## Se dúvida

Migrar para Safe.

[CFG-F14]

---

# 11. Ownership Compatível

Considerar objeto atualizável somente quando a metadata persistente identifica o objeto como pertencente à mesma API gerada e à mesma Transaction.

Nome, módulo coincidente ou estrutura semelhante não bastam para assumir propriedade.

## Se não atender

Tratar como ativo externo.

[CFG-F14]

---

# 12. Modo Cancel

Ao detectar conflito:

- interromper geração
- não criar novos objetos
- informar motivo

## Uso indicado

Ambientes sensíveis.

[CFG-F14]

---

# 13. Reexecução da Mesma Transaction

Nova geração para mesma Transaction deve:

- usar mesmo naming base
- respeitar modo escolhido
- manter previsibilidade
- registrar se atualizou, bloqueou ou removeu
- comparar explicitamente metadata, fingerprint e objetos próprios antes de sincronizar
- não alterar objeto algum antes de confirmação quando houver impacto material

[CFG-F14]

---

# 14. Matriz de Resultado

| Situação | Safe | Update | Cancel |
|---------|------|--------|--------|
| apiCliente externo existe | bloqueia | bloqueia | aborta |
| SDT incompatível | bloqueia | bloqueia | aborta |
| serviço faltando em objeto próprio | atualiza conservadoramente | atualiza | aborta |
| dúvida estrutural | bloqueia | bloqueia | aborta |

[CFG-F14]

---

# 15. Versionamento Oficial

## Formato

Não há versionamento automático por sufixo no MVP.

## Regra

Não usar `_v2`, datas aleatórias ou GUID como solução automática de conflito.

Também não adotar, sobrescrever ou apagar objeto externo automaticamente.

## Motivo

Metadata persistente e bloqueio explícito são mais seguros que multiplicar objetos parecidos.

[NOM-F11][CFG-F14]

---

# 16. Política de Resíduo

Se geração falhar após criar parte dos objetos:

- pausar e informar o usuário sobre o estado atual
- listar objetos criados com sucesso
- listar etapa que falhou e o motivo
- oferecer ao usuário a decisão: manter os objetos salvos ou removê-los
- registrar a decisão tomada

[ENG-F10][CFG-F14]

---

# 17. Logs Obrigatórios

Registrar:

- modo usado
- conflitos encontrados
- decisão tomada
- objetos novos
- objetos atualizados
- falhas

[CFG-F14]

---

# 18. Critérios de Aceite

| Critério | Resultado Esperado |
|------|--------------------|
| apiCliente externo existe + Safe | bloqueia |
| apiCliente externo existe + Cancel | aborta |
| serviço ausente em objeto próprio + Update | tenta completar |
| dúvida estrutural + Update | bloqueia |
| falha parcial | loga incompleta |
| Folder reutilizado | preserva na remoção |
| SDTs compartilhados `GxOpenAPI` | preserva ao remover API específica |

[CFG-F14]

---

# 19. Uso Correto por Agentes de IA

## Pode assumir

- Safe é padrão preferido
- overwrite automático é risco
- rerun precisa previsibilidade
- metadata persistente governa atualização

## Deve tratar com cautela

- update depende ownership real
- SDK pode limitar updates finos
- rollback total não é garantido no MVP

---

# 20. Conclusão Objetiva

Quando houver conflito, o MVP deve errar para o lado seguro.

Criar nova versão previsível é melhor que quebrar ativo existente.
