# README.md

# Genexus Open API Builder

Ferramenta open source para acelerar a geração de APIs REST baseadas em **Transactions GeneXus**.

Transforma tarefas repetitivas em automação útil, previsível e rastreável.

---

# Objetivo

Reduzir o tempo necessário para criar estruturas iniciais de APIs REST no ecossistema GeneXus.

Em vez de criar tudo manualmente, o projeto gera uma base pronta para evolução.

---

# O Que Gera

A partir de uma Transaction, o projeto busca gerar:

- API principal
- Procedures de apoio
- SDTs próprios de Create, Update, Response, filtros e lista
- SDTs compartilhados de erro e paginação
- serviços `List`, `Get`, `Create` e `Update`
- naming consistente
- metadata persistente para regeneração conservadora

---

# Público-Alvo

- Software houses GeneXus
- Times corporativos internos
- Consultores independentes
- Comunidade técnica
- Estudantes

---

# Estado Atual

A consolidação documental posterior à entrevista funcional do MVP foi concluída.

A base de build mínima e o carregamento da extensão foram validados no GeneXus 18 U15 pelo mecanismo oficial disponível a partir do U14. Permanece pendente somente a validação prática no U14.

Para retomar o trabalho em uma nova sessão, consulte o checkpoint operacional:

[Estado atual e próximo passo](Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md)

---

# Atualização Manual da Extensão no GeneXus 18

Quando uma nova DLL estiver pronta para teste:

1. feche completamente a IDE GeneXus;
2. execute [`Install-ExtensionForGeneXus18.bat`](Install-ExtensionForGeneXus18.bat) na raiz do repositório usando **Executar como administrador**;
3. se houve alteração desde o último `genexus /install` bem-sucedido em `Src/Extension/GenexusOpenApiBuilder.package`, na identidade do pacote ou no registro de comandos, execute [`Register-ExtensionForGeneXus18.bat`](Register-ExtensionForGeneXus18.bat) normalmente, sem Administrador; no prompt aberto, digite `genexus /install`, confira a varredura e depois digite `exit`;
4. abra novamente a IDE e siga o roteiro de teste da frente ativa.

O primeiro arquivo delega a cópia, o backup e a validação de hash ao script interno `Tools/Copy-ExtensionForGeneXus18.ps1`. Esse script não registra a extensão. O segundo `.bat` e `genexus /install` são condicionais: atualizam o registro e o manifesto carregados pela IDE, mas não são necessários quando somente a DLL foi alterada desde o último registro bem-sucedido.

Para conferir posteriormente, somente por leitura, se a DLL instalada coincide com a build:

```powershell
pwsh -NoProfile -File Tools/Test-InstalledExtension.ps1
```

O agente não executa os instaladores nem altera diretamente a pasta de instalação do GeneXus. O histórico técnico detalhado está em [B000 — Carregamento na IDE](Docs/Implementation/B000-CARREGAMENTO-IDE.md).

---

# Validação Pré-Push

Antes de enviar commits, atualize a referência remota e execute o checker mecânico pelo nome canônico:

```powershell
git fetch origin
pwsh -NoProfile -File scripts/Invoke-PrePushMechanicalChecks.ps1 -AsJson
```

O JSON valida branch, divergência remota, whitespace, parse dos scripts, restore, build e limpeza da working tree. Resultado mecânico não substitui a revisão semântica: quando `manualRequired` estiver preenchido, o push permanece bloqueado até revisar os itens e registrar gaps confirmados, flags descartados e áreas não cobertas.

Quando o checker ou seu teste for alterado, execute também:

```powershell
pwsh -NoProfile -File Tests/PrePushChecker/Test-OpenApiBuilderPrePushChecks.ps1
```

---

# Fonte Primária das Decisões do MVP

O registro consolidado da entrevista funcional de julho de 2026 é a fonte primária das decisões do MVP:

[Registro de decisões funcionais do MVP — 2026-07-14](Docs/Decisions/2026-07-14-REGISTRO_DECISOES_FUNCIONAIS_MVP.md)

Esse registro preserva as decisões funcionais consolidadas. Os documentos em `Docs/Foundation` materializam os contratos organizados por assunto; mudanças posteriores devem atualizar explicitamente as fontes afetadas.

---

# Estrutura do Repositório

- Docs
- Src
- Tests
- Samples
- Tools
- Temp

---

# Documentação Base

A fundação estratégica do projeto está em:

Docs/Foundation/

Evidências reproduzíveis da implementação prática ficam em:

Docs/Implementation/

---

# Filosofia

- Open Source real
- Valor prático
- Simplicidade inicial
- Código rastreável
- Evolução pública
- Sem hype vazio

---

# Como Contribuir

Contribuições são bem-vindas:

- bugs
- melhorias
- documentação
- testes
- ideias úteis
- Pull Requests

Leia também:

CONTRIBUTING.md

---

# Roadmap Resumido

## Fase 1

MVP funcional.

## Fase 2

Produto confiável.

## Fase 3

Expansão técnica.

---

# Mensagem Oficial

Menos repetição.
Mais entrega.
Mais valor para a comunidade GeneXus.

---

# Status

Fundação documental concluída. A próxima ação vigente está no [checkpoint operacional](Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md).
