# `B111` — estado da revisão por pares (2026-09-03/04)

> **Registro histórico.** Este arquivo preserva o estado das rodadas anteriores à
> consolidação da `S-B111`. As seções abaixo não são a fonte atual de verdade: a rodada
> CLI de 2026-09-08 já ocorreu, o Cursor ainda não foi acionado e não há implementação
> autorizada. Para o estado corrente, use `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`, os
> planos F1/F2/F3 e os artefatos da rodada em `Temp/revisao-por-pares/`.

## Propósito

Permitir retomar a revisão do plano do `B111` em sessão nova, sem repetir rodadas já feitas nem
reabrir pontos já refutados. Registra quem revisou o quê, com que veredito, e o que falta para
fechar.

Plano aprovado, base das rodadas: [`2026-09-04-PLANO-API-OBJECT-GRAVACAO-UNICA.md`](2026-09-04-PLANO-API-OBJECT-GRAVACAO-UNICA.md).
Manuscrito expandido produzido nas rodadas, nunca aprovado e por muito tempo fora do controle de versao: [`2026-09-04-B111-MANUSCRITO-EXPANDIDO-V24.md`](2026-09-04-B111-MANUSCRITO-EXPANDIDO-V24.md) (a 'v24' citada abaixo).
Linha de base de campo: [`2026-09-04-EVIDENCIA-IDE-DRIFT-API-OBJECT.md`](2026-09-04-EVIDENCIA-IDE-DRIFT-API-OBJECT.md).

> **O objeto da revisão mudou em 2026-09-05.** Os dois documentos acima foram superados pela
> sprint `S-B111`, com três planos de fase, e deixaram de ser alvo da próxima rodada.
> Ver a seção 8.

## 1. Estado em uma linha

Nove versões de manuscrito, sete rodadas de revisão, painel com duas famílias de criador
efetivamente respondendo. **Na data deste registro, a versão então vigente do plano ainda
não havia sido submetida a revisão.** O recibo de fechamento daquela sequência não foi
emitido.

## 2. Composição do painel

Curadoria de revisores regravada em schema 3 durante a sessão, em nível de máquina (fora do
repositório, em `%LOCALAPPDATA%\xpz-llm-delegate\`). A ordem calibrada foi: opencode
`deepseek-v4-pro`; opencode `qwen3.8-max`; Codex `openai/gpt-5.6-luna`; Claude Code
`anthropic/claude-opus-5`.

Dois revisores da curadoria **não participaram**:

| Revisor | Motivo |
|---|---|
| opencode-go / `deepseek-v4-pro` | exige opt-in de hospedagem na China no workspace opencode; não habilitado |
| opencode-go / `qwen3.8-max` | `Insufficient balance` — saldo da conta opencode-go esgotado, afeta o provider inteiro |

Participaram, além dos previstos: opencode zen / `big-pickle` (a pedido, fora da curadoria) e
dois subagentes do orquestrador, que diferem dos externos por terem **acesso ao repositório** e
poderem verificar as afirmações do manuscrito em vez de aceitá-las.

## 3. Cronologia — quem viu qual versão

| Versão | Revisor | Veredito | Achado principal |
|---|---|---|---|
| v1 | Codex `openai/gpt-5.6-luna` | REVISAR | H2 falsa em escopo global; residual BC→List reproduz o sintoma; propôs promover o residual à solução principal |
| v2 | opencode `big-pickle` | CONCORDA COM RESSALVAS | H4 é hipótese, não fato; critério de sucesso do cenário A mal formulado |
| v3 | Claude Code `anthropic/claude-opus-5` | CONCORDA COM REVISÕES | **Erro lógico:** o cenário A não pode provar H4; o estado divergente é alcançável por **cancelamento**, não só por falha; baseline tem cinco cláusulas; propôs `deferApiSave` |
| v4 | opencode `big-pickle` | REVISA | Ambiguidade no item 5 quebraria "List sem Business Component"; o preflight que sustentava H5 está inativo nos fluxos vivos |
| v5 | Codex `openai/gpt-5.6-luna` | REVISAR | Sete gaps de rigor de validação; exigiu ordem observável e cenário "BC sem List" |
| v6 | subagente (com repositório) | REVISA | **Fato falso:** as marcas de progresso não chegam à Output; segunda dependência do writer de List (posse sem metadata); propôs o cenário G |
| v7 | mesmo subagente, escopo estreito | — | Item 7 apontava o molde errado; a escrita na Output força exibição do painel; cenário G inexecutável como redigido; condição do cenário D errada |
| v8 | Codex `openai/gpt-5.6-luna` | REVISAR | **Contradição:** a recuperação estava justificada pela posse, quando o gate de entrada é o de integridade |
| v9 | subagente novo (com repositório) | REVISA | O ramo "primeira geração" da seção 4 estava errado (bloqueia por `MetadataMissing`); a etapa de criação também grava o API Object; metade dos fatos valia só no Wizard |
| **v10** | **ninguém** | — | Versão atual. Incorpora a medição de campo e a cascata de degradação |

## 4. Diversidade e validade formal

Famílias de criador que responderam: **OpenAI** (Codex) e **Anthropic** (Claude Code e os dois
subagentes). O piso de duas famílias distintas foi satisfeito.

Duas ressalvas de honestidade metodológica:

- `opencode/big-pickle` resolve para uma família não reconhecida pelo cálculo de diversidade e
  **não conta no piso**, embora tenha contribuído com achados reais;
- a segunda família do painel é a **do próprio autor** do plano. Diversidade mais fraca do que
  seria com o DeepSeek ou o Qwen, que ficaram de fora.

**Nenhum recibo de fechamento foi emitido.** A rodada nunca chegou ao ponto de fechamento porque
o plano seguiu evoluindo a cada parecer, e depois a prova migrou para a medição na IDE.

## 5. O que já foi refutado — não reabrir

O apêndice do plano (seção 10) lista os pontos descartados com o motivo. Em resumo, para não
serem reintroduzidos: Procedures órfãs; remoção em estado parcial como gate; seam testável;
guarda por execução real em vez de seleção; irredutibilidade da janela residual; recuperação
justificada pela posse; ordem de gravação "inobservável"; Build All e HTTP como critério de saída.

Dois desses eram erros de raciocínio do autor, não divergências de opinião — vale ler o apêndice
antes de sustentar qualquer um deles de novo.

## 6. O que falta

1. ~~**Submeter a v10**~~ — **superado em 2026-09-05**: essa versão foi fatiada em três planos de
   fase, e o objeto da próxima rodada passou a ser esses três documentos. Ver a seção 8.
2. **Emitir o recibo de fechamento** da revisão, com o estado final de cada revisor da curadoria —
   inclusive os dois que não participaram, com o motivo.
3. ~~**Cenário J** (Sincronizar a partir do estado divergente) segue sem linha de base~~ —
   **medido em 2026-09-05**: o Sincronizar **bloqueia** antes de qualquer gravação, com
   `Interrupted` e zero objetos tocados, porque suas seleções derivam de uma metadata que o
   estado divergente não tem. Não degrada. Dos três caminhos que a mensagem de aborto
   recomenda, apenas o Wizard piora a situação. Ver
   [`…-SONDAS-IDENTIDADE-E-DIARIO.md`](2026-09-04-B111-SONDAS-IDENTIDADE-E-DIARIO.md) §10.5.

## 7. Recomendações para a próxima rodada

- **O plano é autônomo.** Submeta o documento commitado, não um manuscrito colado: ele já traz
  fatos separados por origem (leitura de código × medição), hipóteses com estado, e o apêndice de
  refutações.
- **Peça verificação, não leitura.** Os achados mais valiosos vieram dos revisores com acesso ao
  repositório, que testaram as afirmações em vez de aceitá-las. Aos externos, vale dizer
  explicitamente que os "fatos" são alegações a checar.
- **Recuperar `deepseek-v4-pro` e `qwen3.8-max`** exige, respectivamente, o opt-in de hospedagem
  e saldo na conta opencode-go. Isso melhoraria a diversidade do painel, hoje apoiada em duas
  famílias, uma delas a do autor.
- **Atenção ao padrão observado:** a taxa de achados caiu ao longo das rodadas de papel, mas não
  zerou — e a primeira hora de teste real produziu mais correções do que as três últimas rodadas
  de revisão somadas. Se a v10 passar sem achados relevantes, isso é sinal de maturidade do
  papel, não de que a implementação está livre de surpresas.

## 8. Atualização de 2026-09-05 — novo objeto de revisão

Nem o plano aprovado nem o manuscrito expandido v24 **devem ser submetidos**. O escopo foi fatiado, e quatro afirmações do manuscrito foram
desmentidas por medição.

### 8.1 O que mudou

Uma sessão de sondagem na IDE, com instrumentação própria e seis execuções sobre duas KBs
(`wseducacaospteste` e `fabricabrasil18test`), produziu
[`2026-09-04-B111-SONDAS-IDENTIDADE-E-DIARIO.md`](2026-09-04-B111-SONDAS-IDENTIDADE-E-DIARIO.md).
A partir dela, o `B111` virou a sprint `S-B111` com três fases, cada uma com plano próprio:

| Fase | Plano | Escopo |
|---|---|---|
| F1 | [`...-B111-F1-PLANO-ORDEM-E-WRITER-FINAL.md`](2026-09-04-B111-F1-PLANO-ORDEM-E-WRITER-FINAL.md) | ordem de gravação e writer final único |
| F2 | [`...-B111-F2-PLANO-SEAM-E-RECIBOS.md`](2026-09-04-B111-F2-PLANO-SEAM-E-RECIBOS.md) | seam de persistência e recibos |
| F3 | [`...-B111-F3-PLANO-DURABILIDADE-E-REMOCAO.md`](2026-09-04-B111-F3-PLANO-DURABILIDADE-E-REMOCAO.md) | durabilidade da intenção e remoção segura |

**Esses três documentos continuam sendo o objeto da próxima rodada, quando ela for retomada.**
O manuscrito permanece como origem das exigências e como registro do que já foi refutado
(seção 5 deste documento e o apêndice do próprio manuscrito), mas a seção 7 da F3 lista as
quatro correções que a medição impôs a ele. A consolidação de decisões de 2026-09-07 deve
ser lida antes de qualquer nova consulta.

### 8.2 O que a próxima rodada não precisa reabrir

Além do que a seção 5 já lista, estes pontos passaram de opinião a medição e não são mais
matéria de revisão de papel:

- identidade do API Object antes do Save — o `Guid` existe desde o `API.Create`, sobrevive
  ao `Save()` e reencontra o objeto, em seis execuções;
- limite da `Description` — 256 caracteres, truncando em silêncio;
- custo de gravação por tipo — `File` custa ~1,1 s na KB grande contra dezenas de ms nos
  demais tipos;
- custo de varredura e de remonte de índice;
- contagem real de pontos de `Save()` no pipeline.

Um revisor pode contestar a **interpretação** desses números. A sonda e o comando temporários
foram retirados em 2026-09-05, portanto não há reexecução disponível no estado atual.
Qualquer nova medição deve partir dos registros versionados ou de uma sonda específica explicitamente autorizada.

### 8.3 O que pedir ao painel

O risco desta rodada é diferente do da anterior. O manuscrito expandido sofria de excesso: o problema era
inchaço e contradição interna. Os três planos sofrem do risco oposto — **fatiar pode ter
deixado buraco**. Vale pedir explicitamente:

1. a F1 sozinha entrega benefício real, ou deixa o sistema num meio-termo pior que hoje?
2. a fronteira F1/F2 está no lugar certo, dado que a F1 verifica a contagem de Saves por
   instrumentação e só a F2 a prova por execução?
3. a F3 pode ser implementada sem revisitar decisões da F1 e da F2?
4. algum requisito do **plano aprovado** ou do manuscrito se perdeu no fatiamento sem ter sido conscientemente descartado? (a F1 declara, em 4.8 e 4.9, o que preservou do plano aprovado)

### 8.4 Um documento fora do controle de versão custou uma sessão inteira de confusão

O manuscrito expandido viveu de 2026-09-04 a 2026-09-05 apenas em `Temp/`, que o
`.gitignore` exclui. A sessão de sondagem trabalhou sobre ele acreditando estar sobre o plano
aprovado, e os três planos de fase nasceram citando o caminho do plano aprovado quando
descreviam o manuscrito. Uma auditoria externa apontou a divergência; a reconciliação está
registrada nos cabeçalhos dos dois documentos.

A regra que sai daí, e que a recomendação “submeta o documento commitado” da seção 7 já
antecipava sem prever este modo de falha: **um artefato que orienta trabalho não pode viver
em `Temp/`.** Se merece ser revisado, merece ser versionado.

### 8.5 A recomendação da seção 7 se confirmou

A última recomendação daquela seção — de que a primeira hora de teste real produziu mais
correções do que as três últimas rodadas de revisão somadas — voltou a se confirmar, e com
margem maior: uma sessão de sondagem eliminou uma seção inteira do plano, inverteu duas
conclusões do próprio autor e revelou três defeitos independentes, hoje numerados como
`B112`, `B113` e `B114`.

Isso não desqualifica a revisão por pares; qualifica **o que pedir a ela**. Peça avaliação
de desenho, coerência e escopo — e trate qualquer afirmação sobre comportamento do SDK como
hipótese a medir, não como ponto a debater.

## 9. Estado após a consolidação de 2026-09-07

As decisões funcionais e de escopo da S-B111 foram discutidas e registradas no documento
canônico `Docs/Implementation/2026-09-07-S-B111-DECISOES-APROVADAS.md`. Os planos F1, F2 e F3 agora
refletem, entre outros pontos, o diário único sem histórico, o protocolo `Prepared`/`Active`,
o `OperationId`, o `ApplicationId` da operação corrente, `OutcomeUnknown`, a classificação
`StillPresentAfterDelete`, o B115 dentro do seam e o comando unificado de recuperação.

Isso **não** emite o recibo de fechamento da revisão por pares. A revisão permanece pendente:
quando retomada, deverá consultar os três planos consolidados e reagir a cada parecer antes
de decidir se algum gap exige nova conversa humana. Nenhuma implementação, commit, push,
instalação ou consulta adicional foi autorizada por esta atualização documental.
