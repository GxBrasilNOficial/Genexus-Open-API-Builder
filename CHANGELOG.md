# CHANGELOG.md

# Changelog

Todas as mudanças relevantes deste projeto serão registradas neste arquivo.

O formato segue princípios de changelog legível e versionamento progressivo.

---

# [Unreleased]

## Added

- Estrutura inicial do repositório
- Pasta Docs organizada
- Foundation Docs 00 até 28
- checkpoint operacional `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`
- template de evidência reproduzível de `B010` em `Docs/Implementation`
- consolidação documental da entrevista funcional do MVP
- README inicial
- LICENSE MIT
- Planejamento da fase prática
- Sprint 0 concluído: build mínima reproduzível (`B010`–`B012`), solution e projeto de extensão em `Src`
- B000 concluído no U15: extensão mínima registrada, marcada e carregada na IDE com a DLL Release estável e os metadados públicos corrigidos
- B001 concluído no U15: detecção da KB ativa por API oficial, em modo somente leitura
- B002 concluído no U15: listagem de 10 Transactions reais por API oficial, em modo somente leitura
- B003 concluído no U15: criação controlada de Folder de teste com autorização explícita
- correção de segurança pós-B003 validada: a DLL atual não executa sondas automaticamente ao abrir uma KB
- B004 concluído no U15: ciclo de vida de API Object oficial comprovado com criação, alteração, releitura após reinstalação e exclusão confirmada

## Fixed

- checkpoint preserva `B011` e `B012` antes de promover `B000`
- linha de corte do MVP passa a cobrir exaustivamente os itens necessários aos dez gates
- Sprints 3–7 distinguem ApiPlan, SDTs, Procedures/API/metadata, REST/segurança e operação conservadora
- referências de backlog, versões documentais e conflitos no wizard foram alinhadas
- layout inicial de `Src`, destino das evidências e ambiente-base de `B010` foram explicitados
- `Docs/Temp` foi protegido contra inclusão acidental no repositório público
- comandos experimentais B004 removidos do runtime após a validação do ciclo de vida do API Object

## Planned

- Sprint 1 — tarefas restantes do Spike GeneXus Extensibility SDK (`B005`–`B006`)
- Protótipo inicial do wizard
- Primeira geração experimental

---

# [0.1.0] - 2026-04

## Added

- Criação oficial do projeto
- Definição de visão open source
- Coleção documental completa
- Estrutura base de diretórios
- Preparação documental para a futura fase de implementação

---

# Tipos de Mudança

- Added: nova funcionalidade
- Changed: alteração relevante
- Fixed: correção
- Removed: removido
- Deprecated: obsoleto
- Security: segurança

---

# Observação

Versões iniciais podem evoluir rapidamente durante a fase MVP.
