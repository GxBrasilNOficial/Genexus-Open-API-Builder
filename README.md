[Português-BR](README.md) · [Español](README.es.md) · [English](README.en.md)

# Genexus Open API Builder

Ferramenta open source para acelerar a geração de APIs REST a partir de **Transactions GeneXus**.

Alpha pública: **[`0.1.0-alpha.5`](https://github.com/GxBrasilNOficial/Genexus-Open-API-Builder/releases/tag/v0.1.0-alpha.5)** — escolha a DLL correspondente à sua versão do GeneXus no Release.

Menos repetição. Mais entrega. Mais valor para a comunidade GeneXus.

---

## O que resolve

Reduz o tempo para montar a estrutura inicial de uma API REST no ecossistema GeneXus: em vez de criar manualmente API Object, Procedures, SDTs e metadata, o wizard gera uma base previsível, editável e rastreável.

## Para quem

- Software houses GeneXus
- Times corporativos internos
- Consultores independentes
- Comunidade técnica
- Estudantes

## O que gera

A partir de uma Transaction:

- API Object principal (`List`, `Get`, `Create`, `Update`; `Delete` opt-in, desligado por padrão)
- Procedures de apoio
- SDTs próprios (Create, Update, Response, filtros, lista)
- SDTs compartilhados de erro e paginação
- Naming consistente
- Metadata persistente para regeneração conservadora
- Ciclo de vida na IDE: Wizard, Sincronizar com a Transaction, Remover API gerada

### Contrato de erro HTTP (desde `0.1.0-alpha.4`)

Em recusa de regra do Business Component, `Create` e `Update` respondem **HTTP 422** com `ErrorResponse.Code = validation_error`, o texto das rules em `Message` e a coleção `Messages[]` (só mensagens de erro). O Source de cada API só muda ao reabrir o Wizard sobre ela; o SDT compartilhado `sdt_API_ErrorResponse` é único na KB, então regerar qualquer API atualiza o schema de erro publicado por todas. Quem comparava a string fixa `"Business rules rejected the request."` precisa passar a decidir pelo `Code`. Detalhe e opção de desligar: [notas 0.1.0-alpha.4](Docs/Releases/0.1.0-alpha.4.md). Subníveis e o marcador `<Subnível>Replace`: [notas 0.1.0-alpha.5](Docs/Releases/0.1.0-alpha.5.md).

## Status atual

| Item | Estado |
|------|--------|
| Wizard funcional do MVP | Concluído (GeneXus 18 U15) |
| Ciclo de vida (posse, sync, remoção, relatório) | Concluído |
| Alpha pública `0.1.0-alpha.5` | Pacote desta release (subníveis), com assets U14+ e U13 |
| Upgrade 13 | DLL satélite `GenexusOpenApiBuilder.Extension-gx18u13.dll` validada no U13 |
| Upgrade 14 | Confirmado por usuário externo (Alpha `0.1.0-alpha.1`; carregamento + geração) |
| Upgrade 15 | Base do desenvolvimento; uso confirmado por usuário externo pelo caminho de mantenedor (build local + `Install-ExtensionForGeneXus18.bat`) |

### Qual DLL baixar

O Release `0.1.0-alpha.5` contém duas DLLs. Instale somente a correspondente à sua instalação:

| Arquivo no GitHub Release | Serve para | Observação |
|---|---|---|
| `GenexusOpenApiBuilder.Extension.dll` | GeneXus 18 Upgrade 14, Upgrade 15 e posteriores U14+ | Linha canônica; não usar no U13 |
| `GenexusOpenApiBuilder.Extension-gx18u13.dll` | GeneXus 18 Upgrade 13 | Linha satélite U13; não usar em U14+ |

O sufixo `-gx18u13` identifica apenas o asset de download. Não renomeie os arquivos para trocar de linha nem instale as duas DLLs na mesma IDE.

### Limitações honestas

- Transações com subníveis: o Wizard gera cabeçalho + linhas selecionadas; metadata V2, Sync e Remover cobrem a hierarquia; sem endpoints próprios de subnível; contadores de List só nos filhos diretos; profundidade acima de 4 gera aviso sem bloquear; alterar netos exige substituir o nível pai (`<Subnível>Replace`)
- Serviço `DELETE` é opt-in e fica desligado por padrão; API já gerada só ganha o endpoint ao reabrir o Wizard com o checkbox marcado. Apagar o cabeçalho apaga as linhas filhas na mesma transação. `404` em id inexistente não é idempotente (`200`)
- YAML OpenAPI nativo do GeneXus tem restrições (documentadas); a extensão não substitui os templates da instalação
- Classificação de campos sensíveis/auditoria ainda usa política default
- Obrigatoriedade em Create/Update valida **preenchimento** (não presença JSON pura), com a limitação conhecida de valores iguais ao default do tipo

## Começar em minutos

1. [Instalar a extensão](Docs/Public/INSTALL.md)
2. [Seguir a demo rápida](Docs/Public/DEMO.md)
3. Ler as [notas da Alpha](Docs/Releases/0.1.0-alpha.5.md)

## Capturas

Visão rápida. Galeria completa do Wizard (todas as abas): [Docs/Public/DEMO.md](Docs/Public/DEMO.md).

![Menu Genexus Open API Builder](Docs/Images/alpha-menu.png)

![Preferências do Wizard](Docs/Images/alpha-preferences.png)

![Menu de contexto](Docs/Images/alpha-context-menu.png)

![Wizard — Resumo](Docs/Images/alpha-wizard-resumo.png)

![Folder gerado](Docs/Images/alpha-folder.png)

![Sincronizar com a Transaction](Docs/Images/alpha-sync.png)

![Remover API gerada](Docs/Images/alpha-remover.png)

![Relatório final](Docs/Images/alpha-relatorio-final.png)

## Requisito de ambiente: PUT, DELETE e PATCH em IIS

Aplica-se a quem publica a API gerada em **IIS**, com o gerador **.NET Framework**.

O serviço `Update` é gerado como `PUT`. Por padrão, o IIS não entrega esse verbo à aplicação: o handler `ExtensionlessUrlHandler-Integrated-4.0` vem com `verb="GET,HEAD,POST,DEBUG"`. O cliente recebe **404 HTML do IIS**; `List`, `Get` e `Create` podem funcionar normalmente.

Correção durável: no **IIS Manager como administrador**, nó do **servidor** → Mapeamentos de Manipulador → `ExtensionlessUrlHandler-Integrated-4.0` → Restrições da Solicitação → Verbos → acrescente `PUT` (e `DELETE`/`PATCH` se necessário) → reinicie o IIS.

Não acrescente o handler só no `web.config` do app gerado: o Build All regenera essa seção. Cuidado com WebDAV habilitado no servidor.

O gerador **.NET** não apresenta esse comportamento. Diagnóstico completo: [B071-B073/B079](Docs/Implementation/B071-B073-B079-GET-CREATE-UPDATE-HTTP.md).

## Atualização da extensão

Quando houver nova DLL:

**Usuário final** (instalou só com a DLL do Release):

Atualização só com **Add > Local** **não está comprovada**. No B094, com a DLL já presente em `Packages`, o Add > Local falhou com `Error installing extension`; a reinstalação limpa exigiu apagar essa DLL (Program Files; tipicamente com elevação) e repetir o fluxo de instalação. Detalhes e o que foi observado: [Docs/Public/INSTALL.md](Docs/Public/INSTALL.md#atualização-usuário-final).

**Desenvolvedor / mantenedor** (repositório clonado) — caminho comprovado:

1. Feche a IDE GeneXus
2. Execute [`Install-ExtensionForGeneXus18.bat`](Install-ExtensionForGeneXus18.bat) como administrador; se a IDE estiver em outro diretório, passe-o como primeiro argumento
3. Se o manifesto/registro mudou desde o último `genexus /install`, execute [`Register-ExtensionForGeneXus18.bat`](Register-ExtensionForGeneXus18.bat) e rode `genexus /install`
4. Reabra a IDE

Detalhes: [Docs/Public/INSTALL.md](Docs/Public/INSTALL.md).

## Roadmap resumido

| Etapa | Foco |
|-------|------|
| Alpha (agora) | Primeira versão aberta utilizável |
| Sprint 9 | Correções reais com feedback externo |
| Sprint 10 / Beta | Fluxo principal estável e releases previsíveis |

## Documentação

| Documento | Conteúdo |
|-----------|----------|
| [INSTALL](Docs/Public/INSTALL.md) | Instalação |
| [DEMO](Docs/Public/DEMO.md) | Roteiro curto |
| [CHANGELOG](CHANGELOG.md) | Histórico de mudanças |
| [0.1.0-alpha.5](Docs/Releases/0.1.0-alpha.5.md) | Notas PT-BR; [ES](Docs/Releases/0.1.0-alpha.5.es.md); [EN](Docs/Releases/0.1.0-alpha.5.en.md) — escolha da DLL |
| [Decisões do MVP](Docs/Decisions/2026-07-14-REGISTRO_DECISOES_FUNCIONAIS_MVP.md) | Fonte primária funcional |
| [Foundation](Docs/Foundation/00-MASTER_INDEX_DO_PROJETO.md) | Contratos e planejamento |
| [Checkpoint operacional](Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md) | Estado interno do projeto |

## Como contribuir

Bugs, melhorias, documentação, testes e feedback de uso real são bem-vindos.

Leia [CONTRIBUTING.md](CONTRIBUTING.md). Licença: [MIT](LICENSE).

## Estrutura do repositório

- `Docs` — documentação pública, foundation e evidências
- `Src` — extensão e domínio
- `Tests` — testes locais
- `Tools` — instalação e checkers
- `Samples` — espaço para exemplos futuros
