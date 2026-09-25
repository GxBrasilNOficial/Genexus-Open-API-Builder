# B082 Etapa 1B — aceite de desempenho (Remove)

Data: 2026-09-17.
KB grande: `FabricaBrasil18Test` (`Fabrica Brasil Test`), GeneXus 18 U15.
DLL: Release desta sessão (build local pós-`ForgetRemoved*` / índice Nível B no Remover).
Plano: `Docs/Implementation/2026-09-02-B082-PLANO-HARDENING-E-DESEMPENHO.md` (contrato Nível B).

**Decisão humana (2026-09-17):** aceitar a Etapa 1B e **fechar o residual `B082`**, com
ressalva explícita de relógio em `Setor` e `Empresa`. As marcas estruturais da 1B passaram nas
três remoções; `DocumentoFiscal` cumpriu a meta de tempo; `Setor` e `Empresa` ficaram acima do
teto absoluto por ~10% e ~0,3%, dentro ou no limiar da oscilação de totais do Remover admitida no
plano (~8,5%; comparação de totais exige 10% para afirmar regressão).

## Escopo entregue

- Contrato Nível B escrito antes do código.
- Índice coerente sob exclusão: `ForgetRemoved*` só após `confirmacao-pos-delete` por leitura
  corrente (`GetAll` por GUID).
- Localização e revalidação pré-`Delete` pelo índice mantido.
- Confirmações pós-`Delete` individuais e por leitura corrente (decisão 3) — **não** agregadas,
  **não** via índice.
- Retomada `ContinueInterruptedRemoval` com índice próprio.
- Gate `tests.b082Etapa1BIndex`.
- Fora: `RefreshProcedures` do Apply; journal/fila/recovery da F3 (já entregues).

## Marcas estruturais (Remove)

Devem **desaparecer**: `localizacao-delete`, `revalidacao-pre-delete`.

Devem **permanecer**: `confirmacao-pos-delete` (uma por objeto apagado); `folder-vazio` quando o
Folder entra na fila.

Nas três corridas desta sessão: **cumprido**. Nenhum Scan de localização/revalidação; só
confirmação (+ `folder-vazio`).

## Remove — KB grande (metas 1B)

| Operação | Meta 1B | Medido | Scans | Deleted | Relógio |
|---|---|---|---|---|---|
| `Setor` | ≤ 7 s | 7711 ms | 13 (12 confirmação + `folder-vazio`) | 12 | acima (~10%) — **aceito com ressalva** |
| `Empresa` | ≤ 20 s | 20063 ms | 52 (51 confirmação + `folder-vazio`) | 51 | acima (+63 ms, ~0,3%) — **aceito com ressalva** |
| `DocumentoFiscal` | ≤ 9 s | 7998 ms | 13 (12 confirmação + `folder-vazio`) | 12 | **ok** |

Todas: `Estado=Removed`, 1 passada, `Bloqueados=0`. Preview separado (`PreviewMs` ~2,4–3,0 s) não
entra no `TotalMs` do Remove.

Custo restante de varredura = confirmação pós-Delete (decisão 3). Em `Empresa`, ~8,7 s de
confirmação em ~20 s totais.

## Comparação com a linha pré-1B (orientação, não prova)

Medição 2026-09-02 / aceite 1A: Remove `Empresa` ~35 s com ~205 / 155 scans de `GetAll` por
alvo. Nesta sessão: ~20 s com 52 scans só de confirmação. Os números de 2026-09-02 **não**
provam esta DLL; servem só para contextualizar o ganho estrutural.

## Fechamento do residual B082

Com 1A (2026-09-03), Etapas 2 e 3 (2026-09-16) e 1B (2026-09-17, este aceite), o residual de
hardening/desempenho do `B082` no plano de 2026-09-02 fica **fechado**. Próxima ação única do
checkpoint permanece `B122` (não displace). Push e corte de release continuam a exigir
autorização humana explícita.

## Remissão — 2026-09-25: a meta de tempo não é gate

Em 2026-09-25, três Removers da `Empresa` na mesma KB, com DLLs da frente `B109`, levaram 24,6 s,
26,5 s e 29,7 s, contra o teto de ~20 s deste aceite. O mantenedor declarou que desvio dessa ordem
não é relevante e que a meta de tempo é fonte de ruído. **A meta de tempo da 1B não é critério
ativo:** desvio de ~10 s no Remover não abre item nem pede remedição. As marcas estruturais da 1B
continuam valendo. Registro de campo:
`Docs/Implementation/2026-09-25-B109-RAMO-C-DIAGNOSTICO.md`, seções 6 e 7.
