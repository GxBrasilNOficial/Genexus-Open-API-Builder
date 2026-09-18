# B122 — Plano: edição textual ancorada versionada

Data: 2026-09-18.
Item de backlog: `B122` (documento 06, nota operacional 2026-09-15).
Checkpoint: `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md` (próxima ação única vigente).
Estado: **plano** — sem implementação neste arquivo.

**Emenda 2026-09-18 (mesma data):** revisão por parecer externo — gap de aplicação
sequencial vs. validação só no original (§5 / D16); checklist explícito da fixture do
checker (§7.4); regra de exit do harness do teste frente ao exit `2` do script (§6 / §8).

**Emenda 2026-09-18 (2ª):** parecer seguinte — §2.3 alinhada a D6 (normaliza /
`eolNormalized`, sem `POLICY_MISMATCH`); exemplo e teste do hazard de concatenação
corrigidos para falhar de fato no D16.3 (`ANCHOR_NOT_UNIQUE`), não no passo 1.
## 1. Problema

Numa sessão anterior, um agente criou 39 scripts Python descartáveis para editar texto neste
repositório. Todos faziam a mesma coisa à mão: encoding, quebra de linha, `contains` + primeira
ocorrência. Nenhum sobreviveu à sessão, nenhum foi revisado, nenhum é reexecutável.

A ferramenta nativa de edição resolve uma substituição por vez. O que falta é lote com validação
prévia, política de EOL/encoding por construção, unicidade real da âncora, prévia e rastro.

`B122` **não muda o produto** (extensão / IDE). Muda o risco de toda alteração de texto feita por
agente neste repositório.

## 2. Evidência coletada (2026-09-18)

Diagnósticos somente leitura + fixtures em `%TEMP%` (fora do repo).

### 2.1 Política declarada vs. working tree

`.gitattributes` vigente:

```text
/.gitattributes text eol=lf
*.md text eol=lf
*.yml text eol=lf
*.yaml text eol=lf
```

Não há `eol=` para `.cs`, `.ps1`, `.json`, `.bat`, etc. A formulação da nota do backlog que
associava «`.cs` em CRLF via attributes» **não corresponde** ao arquivo atual.

Censo na working tree (sem `bin`/`obj`/`artifacts`/`Temp`):

| Extensão | n | LF | CRLF | MIXED | BOM |
|---|---:|---:|---:|---:|---:|
| `.md` | 180 | 171 | 9 | 0 | 0 |
| `.cs` | 97 | 31 | 62 | 4 | 0 |
| `.ps1` | 66 | 34 | 29 | 3 | 0 |
| `.yml` | 5 | 5 | 0 | 0 | 0 |

`git check-attr` confirma `eol: lf` só nos paths cobertos; `.cs`/`.ps1` ficam `unspecified`.
`core.autocrlf=true` (configuração do sistema Git) está ativo nesta máquina.

Os nove `.md` em CRLF (política violada na working tree) incluem, entre outros:

- `Docs/Implementation/2026-08-28-B099b-METADATA-HIERARQUICA-V2.md`
- `Docs/Implementation/2026-09-17-B082-ETAPA-1B-ACEITE.md`
- `Tests/GenerationBaseline/IdeXpz/CAPTURE-FIM.md`

### 2.2 Âncoras frágeis em docs longos

Contagem exata (case-sensitive, substring) em três arquivos típicos de edição de agente:

| Âncora | STATUS (~206 KiB) | 06-BACKLOG | CHANGELOG |
|---|---:|---:|---:|
| `Wizard` | 93 | 24 | 109 |
| `metadata` | 86 | 52 | 93 |
| `S-B111` | 88 | 38 | 22 |
| `Próxima ação única` | 67 | — | — |
| `B122` | 18 | 4 | — |

No `CHANGELOG.md`, headings exatos também colidem (`## Added` ×9, `## Fixed` ×5, …).
Conclusão: `assert âncora in texto` seguido de substituir a primeira ocorrência é **inseguro**
nestes arquivos. `count == 1` é requisito de aceite, não detalhe.

### 2.3 Matriz comportamental (temp)

| Caso | Validador estrito | Anti-padrão (primeira ocorrência) |
|---|---|---|
| WhatIf com âncora única | valida; zero escrita | escreveria |
| Âncora duplicada | `ANCHOR_NOT_UNIQUE` | edita a 1ª; deixa residual |
| Âncora ausente | `ANCHOR_MISSING` | recusa |
| Lote com 2ª op inválida | bloqueia **antes** de qualquer write | aplica a 1ª → estado parcial |
| BOM UTF-8 | detectável | depende do `Encoding` escolhido |
| EOL MIXED | detectável | `WriteAllText` pode homogenizar sem aviso |
| CRLF × política LF | no apply: normaliza para a política e `eolNormalized=true` (D6); não há exit `POLICY_MISMATCH` | preserva ou converte sem regra |
| Apply único + LF | aplica e mantém LF | — |

### 2.4 Fronteira com `Apply-ApprovedPatch`

O motor `GeneXus-XPZ-Skills/scripts/Apply-ApprovedPatch.ps1` (skill
`xpz-codex-apply-patch-alternative`) resolve **outro** problema: backup auditável de *unified
diff* Git quando a rota nativa `apply_patch` falha. Exige `main`, `AllowedPath`, stage/dry-run e
não deve ser copiado para este repositório (`AGENTS.md` local).

| Aspecto | Apply-ApprovedPatch | B122 |
|---|---|---|
| Entrada | diff unificado (Base64) | `{ path, from, to }` ancorado |
| Unicidade de âncora | N/A (hunk) | `count == 1` |
| Onde vive | skills (não copiar) | `scripts/` deste repo |
| Stage Git | sim | não |
| Substitui o outro? | não | não |

São complementares. B122 substitui os scripts descartáveis de âncora; não encapsula o motor das
skills.

## 3. Objetivo e fora de escopo

### Objetivo (v1)

Entregar `scripts/Apply-TextPatch.ps1` + teste offline + documentação mínima de uso, de modo que
um agente (ou humano) aplique N substituições ancoradas num arquivo com:

1. validação de **todas** as âncoras antes de qualquer escrita;
2. unicidade estrita (`count == 1`);
3. EOL e encoding por política, não por parâmetro do chamador;
4. `-WhatIf` com prévia;
5. recibo JSON no stdout.

### Fora de escopo (v1)

- Mudança na extensão C#, manifesto, menus ou DLL.
- Copiar ou reimplementar `Apply-ApprovedPatch`.
- Promover a ferramenta a skill global (só depois de uso real, conforme a nota do 06).
- Criação/deleção de arquivos; rename; patch binário.
- Regex / wildcards na âncora (só literal ordinal).
- Lote atômico multi-arquivo (v1 = **um arquivo**, N ops).
- «Consertar» em massa os 9 `.md` CRLF ou os MIXED `.cs`/`.ps1` sem edição pedida.
- Alterar `.gitattributes` nesta frente (pode ser follow-up se a evidência pedir).

## 4. Decisões de desenho (fechadas por este plano)

| # | Decisão | Escolha v1 | Motivo |
|---|---|---|---|
| D1 | Nome | `scripts/Apply-TextPatch.ps1` | alinhado à nota do 06 |
| D2 | Escopo atômico | 1 path × N ops | cobre o sintoma do lote parcial; multi-arquivo fica v2 |
| D3 | Âncora | literal, ordinal, `count == 1` | evidência §2.2 |
| D4 | Ordem de validação | (a) path/IO (b) BOM (c) EOL (d) âncoras no original (e) fail-fast cruzado `to`⊃`from` (f) apply em memória com revalidação por passo (g) só então escreve | falha cedo; zero escrita parcial; ver D16 |
| D5 | Encoding | UTF-8 **sem BOM** na escrita; leitura recusa BOM | AGENTS.md + nota do 06 |
| D6 | EOL com `eol=` no attributes | gravar com essa política; se a working tree divergir, normalizar **só nesse apply** e marcar `eolNormalized=true` no recibo | attributes é a fonte canônica onde existe |
| D7 | EOL sem `eol=` (ex.: `.cs`) | se o arquivo for LF-only ou CRLF-only, **preservar**; se MIXED, **recusar** | attributes incompleto; preservar evita reformatar a árvore |
| D8 | MIXED | sempre recusar (`EOL_MIXED`) | evidência §2.1 / §2.3 |
| D9 | Entrada | manifesto JSON (arquivo via `-ManifestPath`) com `path` + `ops[{from,to}]` | evita aspas embutidas no argv; auditável |
| D10 | Prévia | `-WhatIf`: valida tudo (incluindo simulação em memória da D16), emite diff por op no recibo, **não grava** | |
| D11 | Rastro | JSON compacto no stdout (`kind=apply-text-patch-result`) | espelha o padrão do ApprovedPatch sem copiar o motor |
| D12 | `#requires` | PowerShell 7.4+ | igual ao pré-push local |
| D13 | Teste | `Tests/TextPatch/Test-ApplyTextPatch.ps1` cobrindo A–H + interferência sequencial | fixtures em diretório temp do teste |
| D14 | Gate pré-push | registrar `tests.textPatch` no checker **e** na fixture de `Test-OpenApiBuilderPrePushChecks.ps1` | o custo mecânico maior está na fixture do checker; ver §7.4 |
| D15 | Fronteira | documentar no comentário de cabeçalho do script: não substitui ApprovedPatch | §2.4 |
| D16 | Apply sequencial | validar no original **e** revalidar `count == 1` no buffer **antes de cada** substituição; recusa estática `to[i]` contém `from[j]` (`j≠i`) é fail-fast complementar, **não** substituto | parecer 2026-09-18; hazard de concatenação |

### Resolução de EOL (algoritmo)

1. Resolver path relativo à raiz do repositório (recusar path absoluto fora da raiz, `..`, etc.).
2. `git check-attr eol -- <path>`:
   - se `eol=lf` ou `eol=crlf` → política = esse valor;
   - se `unspecified` → política = `preserve`.
3. Medir EOL atual dos bytes (LF / CRLF / MIXED / NONE).
4. Se MIXED → falhar `EOL_MIXED`.
5. Se política ∈ {lf, crlf} e atual ≠ política e atual ≠ NONE → apply grava na política e
   `eolNormalized=true`; WhatIf reporta a normalização sem gravar.
6. Se política = `preserve` → regravar com o mesmo EOL detectado.
7. Arquivo sem newline (NONE): na v1, recusar se houver ops (evita inventar EOL); exceção só se
   o conteúdo pós-ops continuar sem newline **e** o original também era NONE — fora do caminho
   típico; simplificar: **recusar NONE** com ops (exit `EOL_NONE`).

### Resolução de attributes sem `git`?

O script **pode** parsear `.gitattributes` na raiz como fallback, mas a fonte preferida é
`git check-attr` (respeita precedência real). Se `git` falhar, abortar `GIT_ATTR_FAILED` — não
adivinhar.

## 5. Contrato de entrada

Manifesto JSON (UTF-8 sem BOM), exemplo:

```json
{
  "schemaVersion": 1,
  "path": "Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md",
  "ops": [
    {
      "from": "trecho único e longo o bastante…",
      "to": "trecho substituto…"
    }
  ]
}
```

Regras:

- `schemaVersion` deve ser `1`.
- `path` relativo à raiz, com `/` ou `\`; normalizado internamente; sem `..`.
- `ops` não vazio; cada `from` não vazio; `from == to` é inválido.
- Duas ops no mesmo manifesto não podem partilhar a mesma `from` (literal idêntico).
- Cascata intencional (op2 depende do texto já alterado pela op1) **fora da v1** — o autor
  deve usar dois invokes sequenciais com WhatIf entre eles.

### Algoritmo de âncoras e apply em memória (D16)

Validar só contra o texto **original** e depois aplicar em ordem **não basta**. Se o `to` da
op *i* introduzir (ou a concatenação com o entorno criar) uma nova ocorrência do `from` da
op *j*, a validação no original não vê o problema e a substituição seguinte pode acertar a
ocorrência errada — ou deixar de ser única.

Hazard de concatenação (o check estático `to`⊃`from` **não** pega). O exemplo precisa
fazer a âncora da op2 **existir uma vez no original**; senão o passo 1 já falha com
`ANCHOR_MISSING` e o D16.3 nem roda. Forma correta — a op1 cria a **segunda** ocorrência:

- texto: `PREFIX_OLD_SUFFIX and PREFIX_SUFFIX`
- op1: `PREFIX_OLD` → `PREFIX`
- op2: `PREFIX_SUFFIX` → `…`
- no original: `PREFIX_SUFFIX` tem `count == 1` (só o trecho após `and `); `PREFIX_OLD_SUFFIX`
  **não** contém a substring `PREFIX_SUFFIX`
- após op1 o buffer vira `PREFIX_SUFFIX and PREFIX_SUFFIX`: `count == 2` → no passo D16.3,
  antes da op2, falha com `ANCHOR_NOT_UNIQUE` sem gravar
- `to` da op1 (`PREFIX`) **não** contém `from` da op2, então o fail-fast estático (passo 2)
  também não mascara este caso

(O exemplo antigo só com `PREFIX_OLD_SUFFIX` era didaticamente errado: `PREFIX_SUFFIX` tinha
count 0 no original e morria no passo 1.)

Pipeline obrigatório (WhatIf e apply reais usam o mesmo caminho até o passo de gravar):

1. Validar todas as `from` no texto **original**: cada uma com `count == 1`; `from`s distintas.
2. Fail-fast estático: se `to[i]` contém `from[j]` como substring ordinal para algum `j ≠ i`,
   recusar com `CROSS_OP_TO_CONTAINS_FROM` (exit `12`). Complementar — não substitui o passo 3.
3. `buffer ← original`. Para cada op *i* na ordem do manifesto:
   - medir `count` de `from[i]` em `buffer`;
   - se `count ≠ 1`, falhar com `ANCHOR_MISSING` ou `ANCHOR_NOT_UNIQUE` (conforme o caso),
     **sem gravar**;
   - substituir **somente** essa única ocorrência em `buffer`.
4. Só depois do passo 3 completo: se não for `-WhatIf`, gravar `buffer` com a política de EOL
   / encoding; se for `-WhatIf`, emitir a prévia e sair sem tocar no disco.

Assim o lote permanece atômico na working tree: qualquer falha no passo 3 aborta antes da
escrita, mesmo que as âncoras fossem válidas no original.

Invocação canônica (cwd = raiz do repo):

```powershell
pwsh -NoProfile -File scripts/Apply-TextPatch.ps1 -ManifestPath path\to\manifest.json
pwsh -NoProfile -File scripts/Apply-TextPatch.ps1 -ManifestPath path\to\manifest.json -WhatIf
```

## 6. Contrato de saída

Stdout: uma linha JSON (ou JSON compacto de um objeto), campos mínimos:

```json
{
  "kind": "apply-text-patch-result",
  "schemaVersion": 1,
  "status": "ok",
  "code": "APPLIED",
  "whatIf": false,
  "path": "Docs/....md",
  "opsApplied": 1,
  "eolPolicy": "lf",
  "eolBefore": "lf",
  "eolAfter": "lf",
  "eolNormalized": false,
  "sha256Before": "...",
  "sha256After": "...",
  "ops": [
    { "index": 0, "fromLength": 12, "toLength": 10, "occurrenceCount": 1 }
  ]
}
```

Em erro: `status=error`, `code` = um dos códigos abaixo, `message` legível, sem gravar.

### Códigos / exit codes

| Exit | Code | Quando |
|---:|---|---|
| 0 | `APPLIED` / `WHATIF_OK` | sucesso |
| 2 | `INVALID_INPUT` | manifesto, path, ops vazias, `from==to`, `from`s duplicadas no manifesto |
| 3 | `ANCHOR_MISSING` | alguma `from` com count 0 (no original **ou** no buffer do passo D16) |
| 4 | `ANCHOR_NOT_UNIQUE` | alguma `from` com count ≠ 1 (idem) |
| 5 | `EOL_MIXED` | arquivo com LF e CRLF misturados |
| 6 | `EOL_NONE` | arquivo sem newline e ops presentes |
| 7 | `BOM_PRESENT` | BOM na leitura |
| 8 | `BATCH_VALIDATION_FAILED` | agregador (mais de um erro de âncora no original); detalhe em `errors[]` |
| 9 | `WRITE_FAILED` | IO ao gravar |
| 10 | `GIT_ATTR_FAILED` | `git check-attr` indisponível/falhou |
| 11 | `PATH_OUTSIDE_REPO` | path escapa a raiz |
| 12 | `CROSS_OP_TO_CONTAINS_FROM` | fail-fast: `to[i]` contém `from[j]` (`j≠i`) |
| 1 | `INTERNAL_ERROR` | exceção não classificada |

Quando várias âncoras falham **no original**, preferir exit `8` com `errors[]` listando
missing/not-unique por índice — em vez de parar na primeira — para o agente corrigir o
manifesto de uma vez. Falha no passo sequencial (D16.3) reporta o índice da op que quebrou.

### Exit `2` do script ≠ exit `2` do checker

No `Invoke-PrePushMechanicalChecks.ps1`, exit `2` do **teste unitário** é mapeado para
`environmentBlocked`. O `Apply-TextPatch.ps1` usar `2 = INVALID_INPUT` **não colide**, porque o
gate `tests.textPatch` invoca `Tests/TextPatch/Test-ApplyTextPatch.ps1`, não o script de
produção.

Regra obrigatória do harness: `Test-ApplyTextPatch.ps1` trata os exit codes do script só como
asserção interna (incluindo casos negativos que esperam `2`, `3`, `4`, …) e **ele próprio** só
pode sair com:

- `0` — suíte passou;
- `1` — suíte falhou (asserção ou setup).

Propagar o exit `2` do script para o processo do teste geraria falso `environmentBlocked` no
pré-push. Não é necessário renumerar os codes do script.

## 7. Implementação (ordem de trabalho)

1. **Escrever o plano** (este arquivo) — feito; emenda D16/gate/exit na mesma data.
2. Implementar `scripts/Apply-TextPatch.ps1` conforme §§4–6 (pipeline D16 incluso).
3. Implementar `Tests/TextPatch/Test-ApplyTextPatch.ps1` cobrindo:
   - WhatIf sem mudar bytes;
   - apply único;
   - duplicata no original;
   - ausente;
   - lote com falha no meio (arquivo intacto);
   - BOM;
   - MIXED;
   - `.md` simulado com attributes LF + working tree CRLF → normaliza no apply e
     `eolNormalized=true`;
   - path fora do repo;
   - `from` duplicada entre duas ops no mesmo manifesto;
   - **interferência sequencial por `to` que contém `from` de outra op** (exit `12`);
   - **hazard de concatenação** — texto
     `PREFIX_OLD_SUFFIX and PREFIX_SUFFIX`, op1 `PREFIX_OLD`→`PREFIX`, op2 com
     `from=PREFIX_SUFFIX`: passa no passo 1 (`count==1` cada), não dispara exit `12`, e
     **deve falhar no D16.3** com `ANCHOR_NOT_UNIQUE` sem gravar;
   - caso negativo com exit `2` do script em que o **teste** ainda assim termina em exit `0`.
4. Integrar o gate `tests.textPatch` — **maior trecho mecânico da frente de integração**,
   além de uma linha no array do checker. Checklist obrigatório (mesmo padrão dos gates
   existentes):
   1. entrada `[ordered]@{ Name = 'tests.textPatch'; … }` em
      `scripts/Invoke-PrePushMechanicalChecks.ps1`;
   2. diretório `Tests\TextPatch\` no repo real com `Test-ApplyTextPatch.ps1`;
   3. em `Tests/PrePushChecker/Test-OpenApiBuilderPrePushChecks.ps1`: criar o diretório na
      fixture temp, gravar stub `#requires` + `PASS: fixture Text Patch`, e assertar no JSON
      que o check `tests.textPatch` veio `passed` e que `commands` contém o
      `pwsh -NoProfile -File Tests/TextPatch/Test-ApplyTextPatch.ps1`;
   4. rodar `pwsh -NoProfile -File Tests/PrePushChecker/Test-OpenApiBuilderPrePushChecks.ps1`
      após o encaixe.
5. Acrescentar parágrafo curto de uso em comentário `.SYNOPSIS` / `.NOTES` do script (sem novo
   README público).
6. Atualizar checkpoint + entrada `[Unreleased]` no `CHANGELOG` **só quando a implementação
   estiver testada** (não neste passo de plano).
7. Corrigir a imprecisão da nota operacional do B122 no documento 06 (attributes vs. realidade)
   na mesma frente de implementação, com remissão a este plano.

## 8. Critérios de aceite

- [x] `Apply-TextPatch.ps1` existe em `scripts/` e roda em pwsh 7.4+.
- [x] Matriz A–H do §2.3 coberta pelo teste offline com token de sucesso explícito.
- [x] Pipeline D16: revalidação no buffer antes de cada op; hazard de concatenação coberto.
- [x] Fail-fast `CROSS_OP_TO_CONTAINS_FROM` coberto por teste.
- [x] Nenhuma escrita ocorre se qualquer âncora falhar (original ou buffer).
- [x] WhatIf não altera SHA-256 do alvo.
- [x] Escrita UTF-8 sem BOM; leitura com BOM falha.
- [x] MIXED falha; `.md` com política LF grava LF.
- [x] Gate `tests.textPatch` no checker **e** na fixture de `Test-OpenApiBuilderPrePushChecks.ps1`
      (checklist §7.4 completo).
- [x] `Test-ApplyTextPatch.ps1` só sai com exit `0` ou `1` (nunca propaga exit `2` do script).
- [x] Cabeçalho do script declara fronteira com `Apply-ApprovedPatch`.
- [x] Documento 06: nota B122 alinhada à evidência de attributes (remissão datada).
- [x] Checkpoint: frente `B122` fechada; próxima ação única vigente = `B123` (2026-09-18).

## 9. Riscos e mitigação

| Risco | Mitigação |
|---|---|
| Normalizar EOL em `.md` CRLF altera diff além do trecho editado | recibo com `eolNormalized`; WhatIf mostra; agente deve avisar o humano se o diff EOL for grande |
| Âncora «única» ainda semanticamente frágil (trecho curto) | documentação: âncora deve incluir contexto local; ferramenta não estima qualidade semântica |
| Op *i* cria/altera ocorrência da âncora da op *j* (incl. concatenação) | D16: revalidação no buffer + teste de concatenação; fail-fast `to`⊃`from` complementar |
| Exit `2` do script vazar no gate como `environmentBlocked` | harness do teste só retorna `0`/`1`; nota §6 |
| Fixture do checker esquecida ao adicionar o gate | checklist §7.4; critério de aceite explícito |
| Agente continua inventando script Python | checkpoint + AGENTS/local: preferir este script quando a edição for ancorada em lote |
| Confundir com ApprovedPatch | D15 + §2.4 |
| `core.autocrlf` mascarar bytes após commit | ferramenta opera na working tree; aceite mede bytes em disco, não o blob Git |

## 10. Próximo passo operacional

~~Após aprovação humana deste plano: implementar os itens 2–5 da §7.~~ **Feito em 2026-09-18**
(implementação, testes, gate, commits `ce1cf44`/`bf40b3b`). Próxima ação do repositório: `B123`
(ver checkpoint).

FIM DO PLANO
