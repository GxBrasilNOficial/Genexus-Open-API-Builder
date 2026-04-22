# 14-CONFLITOS_REEXECUCAO_E_VERSIONAMENTO.md

## Regras Oficiais de Conflitos, Nova Execução e Versionamento no MVP

**Projeto:** Genexus Open API Builder  
**Versão:** v1  
**Base Primária:** 10-ENGINE_GERACAO_OBJETOS.md v1.1  
**Dependência direta:** 11-CONVENCOES_NOMES_E_OUTPUTS.md v1.1  
**Relacionamento adicional:** 12-REGRAS_CRIACAO_API_OBJECTS.md v1.1 / 13-REUSO_E_GERACAO_SDTS.md v1.1  
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
- padronizar versionamento
- permitir evolução segura

Este documento **não define SDK**, **não trata UX detalhada**, **não redefine naming base**.

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
| Versionamento | criação de nova variante numerada |
| Update | tentativa controlada de atualizar objeto existente |
| Cancel | abortar geração |

[CFG-F14]

---

# 6. Tipos de Conflito

| Tipo | Exemplo |
|------|---------|
| Nome ocupado | ClienteApi já existe |
| SDT incompatível | ClienteRequest existe com estrutura divergente |
| Módulo divergente | objeto no módulo errado |
| Tipo divergente | nome igual para tipo diferente |
| Bloqueio técnico | objeto indisponível para update |

[CFG-F14]

---

# 7. Modos de Execução

| Modo | Comportamento |
|------|---------------|
| Safe | nunca sobrescreve automaticamente |
| Update | tenta atualizar compatíveis |
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

Se nome ocupado:

criar nova versão livre.

## Exemplos

- ClienteApi → ClienteApi_v2
- ClienteApi_v2 → ClienteApi_v3
- ClienteRequest → ClienteRequest_v2

## Regra

Buscar automaticamente o menor número livre.

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

Considerar objeto atualizável quando ao menos um:

- criado previamente pelo gerador
- nome segue padrão oficial
- metadata interna identifica origem
- módulo alvo coincide e estrutura compatível

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
- registrar se substituiu ou versionou

[CFG-F14]

---

# 14. Matriz de Resultado

| Situação | Safe | Update | Cancel |
|---------|------|--------|--------|
| ClienteApi existe | ClienteApi_v2 | tenta update | aborta |
| SDT incompatível | novo versionado | novo ou safe | aborta |
| rota faltando | novo versionado | adiciona | aborta |
| dúvida estrutural | novo versionado | safe | aborta |

[CFG-F14]

---

# 15. Versionamento Oficial

## Formato

<NomeOriginal>_v2  
<NomeOriginal>_v3  
<NomeOriginal>_v4

## Regra

Nunca usar datas aleatórias ou GUID no MVP.

## Motivo

Legibilidade humana e previsibilidade IA.

[NOM-F11][CFG-F14]

---

# 16. Política de Resíduo

Se geração falhar após criar parte dos objetos:

- registrar incompleta
- listar criados
- não apagar automaticamente no MVP
- sugerir rerun Safe ou limpeza manual

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
| ClienteApi existe + Safe | ClienteApi_v2 |
| ClienteApi existe + Cancel | aborta |
| rota ausente + Update | tenta completar |
| dúvida estrutural + Update | cai para Safe |
| falha parcial | loga incompleta |

[CFG-F14]

---

# 19. Uso Correto por Agentes de IA

## Pode assumir

- Safe é padrão preferido
- overwrite automático é risco
- rerun precisa previsibilidade
- versionamento incremental é oficial

## Deve tratar com cautela

- update depende ownership real
- SDK pode limitar updates finos
- rollback total não é garantido no MVP

---

# 20. Próxima Etapa Recomendada

Criar:

15-TESTES_VALIDACAO_E_QUALIDADE.md

Para consolidar testes automáticos e critérios de pronto.

---

# 21. Conclusão Objetiva

Quando houver conflito, o MVP deve errar para o lado seguro.

Criar nova versão previsível é melhor que quebrar ativo existente.