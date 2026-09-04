# `B111` — estado da revisão por pares (2026-09-03/04)

## Propósito

Permitir retomar a revisão do plano do `B111` em sessão nova, sem repetir rodadas já feitas nem
reabrir pontos já refutados. Registra quem revisou o quê, com que veredito, e o que falta para
fechar.

Plano revisado: [`2026-09-04-PLANO-API-OBJECT-GRAVACAO-UNICA.md`](2026-09-04-PLANO-API-OBJECT-GRAVACAO-UNICA.md).
Linha de base de campo: [`2026-09-04-EVIDENCIA-IDE-DRIFT-API-OBJECT.md`](2026-09-04-EVIDENCIA-IDE-DRIFT-API-OBJECT.md).

## 1. Estado em uma linha

Nove versões de manuscrito, sete rodadas de revisão, painel com duas famílias de criador
efetivamente respondendo. **A versão atual do plano — a que está commitada — nunca foi
submetida a revisão.** Não há recibo de fechamento emitido.

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

1. **Submeter a v10** — a versão atual nunca foi revisada, e é a que mais mudou de substância: ela
   reenuncia a dor a partir da cascata de degradação medida em campo.
2. **Emitir o recibo de fechamento** da revisão, com o estado final de cada revisor da curadoria —
   inclusive os dois que não participaram, com o motivo.
3. **Cenário J** (Sincronizar a partir do estado divergente) segue sem linha de base; não é
   bloqueio da revisão, mas altera a seção de recuperação se o resultado surpreender.

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
