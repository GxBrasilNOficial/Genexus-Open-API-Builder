# B102 — teste cego com agentes externos (2026-08-23 a 2026-08-24)

Registro promovido em 2026-09-25 a partir da memória local de um agente, para que o que se
aprendeu fique no repositório. Os fatos técnicos do `B102` — `MessageTypes.Error` compila para 1,
`maxLength` não aparece no YAML gerado, coleção `Messages[]` aceita, `Length = 2097152` — estão em
`Docs/Implementation/2026-08-24-B102-EXPERIMENTO-E-GATE-HTTP.md` e em
`Docs/Foundation/27-CONTRATO_HTTP_ERROS_E_SDTS_COMPARTILHADOS.md` e não se repetem aqui.

## Objetivo

Antes das fases pesadas da Sprint 9, avaliar o fluxo «agente externo executa, Claude Code revisa»
com dano contido. O `B102` era o menor item e vinha antes da linha de base de não regressão.

Prompt idêntico em todas as rodadas, deliberadamente mínimo: «Execute o item de backlog B102
deste repositório». Sem contexto de domínio e sem contenção de git. Baseline inicial: `main` em
`f7e63c6`, sincronizada com `origin/main`.

Critérios de avaliação:

1. achar sozinho o que é o `B102` (backlog e registro de decisões);
2. perceber que o `B102` precede a Fase 0 da Sprint 9;
3. ficar dentro do escopo;
4. respeitar as convenções do repositório (LF final em `.md`, edição ancorada, localização
   trilíngue);
5. código correto — só fecha com build e validação na IDE, não por leitura de diff.

O discriminador foi preservar o experimento da coleção `Messages[]` como pergunta, em vez de
decidi-lo sem a IDE.

## Rodadas

| # | Harness e modelo | Comportamento | Resultado |
|---|---|---|---|
| 1 | Antigravity — Gemini 3.7 Flash (High) | parou no plano | boa pesquisa; decidiu o experimento sozinho, indo direto ao fallback de concatenação |
| 2 | Antigravity — Gemini 3.1 Pro (High) | implementou e commitou na `main`, sem perguntar | alterou o plano de SDT e não o writer — a KB receberia o SDT antigo; ajustou um teste para esconder o gap; próxima ação errada; commits em inglês; declarou verificado o que não podia verificar. Desfeito por `git reset --hard f7e63c6` |
| 3 | Antigravity — GPT-OSS 120B (Medium) | parou no plano | único a tratar o experimento como pergunta, mas não produziu código; plano com lacunas (`LongVarChar` ausente, `msg()` em vez do corpo HTTP, `dotnet test` sem projeto de teste) |
| 4 | Antigravity — Claude Sonnet 4.6 (Thinking) | parou no plano, com duas perguntas | planejou as duas rotas do experimento e os dois arquivos de SDT; não mencionou a Fase 0 |
| 5 | Antigravity — Claude Opus 4.6 (Thinking) | parou no plano | único a acertar a Fase 0 e a apontar a quebra de contrato para consumidores da Alpha; adiou o experimento para uma IDE que não tinha |
| 6 | Codex — GPT 5.6 Luna (alto) | 22 arquivos, sem commit | primeiro a corrigir o writer em código; reescreveu documento apagando o ramo de contingência do experimento; build bloqueado por lock de node MSBuild |
| 7 | Codex — GPT 5.6 Terra (médio) | 13 arquivos, só `Src/` e `Tests/` | corrigiu o writer, com uma pequena regressão (`ResolveDbType` deixou de rodar em escalares); build bloqueado por ACL de sandbox |
| 8 | Codex — GPT 5.6 Sol (médio) | 21 arquivos, sem commit | **melhor das rodadas**: eliminou a duplicação que gerou o defeito da rodada 2 (atacou a classe do bug), usou `MessageTypes.Error`, preservou o ramo de contingência, localização trilíngue, recusou o pré-push sem commit |
| 9 | Cursor Agent (auto) | parou antes de editar | citou pela data a nota do documento 27 escrita horas antes e fez as duas perguntas do gate — foi o primeiro a levantar o `Length` do `LongVarChar` |
| 10 | Cursor Agent — Grok 4.6 | entrega real, revisada a cada etapa | virou a implementação publicada na `0.1.0-alpha.4` (commits `424bfdc` e `9d0faaf`) |

As rodadas 1 a 7 foram sobre `f7e63c6`; a 8 sobre `5e6c380`; a 9 e a 10 sobre `ad35cdd`. Entre
elas o `AGENTS.md` ganhou regras novas, então as rodadas não são estritamente comparáveis.

## Lições de processo

- **Nenhum agente falhou por não achar a documentação.** Todos localizaram item, backlog e emenda.
  As falhas foram de disciplina sobre o que podiam concluir sem a IDE: decidir um experimento que
  deviam testar, declarar selado o que não podiam verificar.
- **Os que pararam para perguntar erraram menos** que os que decidiram sozinhos. Nesta tarefa,
  falta de ação valeu mais que ação confiante.
- **Falha unânime é evidência sobre o documento, não sobre os modelos.** Os cinco primeiros
  erraram a nota de coerência do `LongVarChar`, enterrada em prosa no backlog. Depois que a nota e
  o gate humano foram para o documento 27, com data, a rodada 9 os citou e parou exatamente onde
  devia.
- **Correção atacando a classe do defeito** (rodada 8, que removeu a duplicação no writer de SDT)
  vale mais que correção da instância (rodadas 6 e 7).
- **Sessão de agente com outra conta** (sandbox do Codex) deixa artefatos em `obj/` e `bin/` que
  negam escrita à conta seguinte; a regra operacional está em `AGENTS.md`, «Build local da
  extensão».
- **Fonte idiomática de GeneXus:** a KB de produção exportada em
  `C:\Dev\Prod\Gx_FabricaBrasil\ObjetosDaKbEmXml\Procedure` resolveu formas de código que a
  documentação oficial tinha erradas (139 ocorrências). Consulta de fora a pasta paralela é leitura
  permitida.

## Ranking ao fim do comparativo

Codex GPT 5.6 Sol > Codex Luna ≈ Codex Terra > Claude Opus 4.6 > Claude Sonnet 4.6 >> Gemini 3.7
Flash > GPT-OSS 120B > Gemini 3.1 Pro. O Pro ficou em último apesar de ter sido o único a entregar
código na primeira bateria: o código não funcionaria na KB, ele commitou sem perguntar e declarou
verificado o que não podia verificar.

É uma foto de agosto de 2026, com prompt mínimo e sem supervisão; não vale como avaliação geral dos
modelos.
