# Critério 11 — Escala na Transaction `Empresa` (13 subníveis)

Data: 2026-08-29.
Gate: Sprint 9, critério 11 (escala, limiares declarados).
Escopo: cópia local `Gx_FabricaBrasil` / KB `FabricaBrasil18Test`, Transaction `Empresa` (13 subníveis paralelos no 2º nível). HTTP, Sync e apply em `DadosDoDia`/`CondicaoPagamento` ficam fora deste gate (já cobertos noutros critérios).

## Limiares (documento 20)

- **Reprovam:** órfão após `Remover API gerada`; colisão de nome sem resolução / nome acima de 128 sem encurtamento; `Build All` com erro na transação de 13; Wizard > 30 s para abrir ou para o preview.
- **Alertam, registram e seguem:** abertura ou preview > 5 s; apply completo > 60 s.

Fórmula de teto: `6 + 3N` SDTs próprios (N = filhos selecionados). Com N = 13 o máximo é **45**, quando todo filho tem campos nos três papéis.

## Pré-condições

- GeneXus 18 U15, DLL canônica reinstalada após o skip de SDT Create vazio (`Install-ExtensionForGeneXus18.bat`; manifesto inalterado).
- Preferências do Wizard gravadas na KB (`GxOpenApiBuilder_Settings`).
- Pasta `EmpresaOpenApi` já existia (apply interrompido no dia): o apply seguinte reencontrou a pasta (`wasCreated=false`).

## Wizard — abertura e critério 8

| Check | Resultado |
|---|---|
| Tempo até abrir | ~7 s — **alerta** (> 5 s, < 30 s) |
| Preview | abaixo de 30 s (não reprova) |
| 13 filhos no seletor Requests | presentes; **Incluir este subnível** marcado em todos |
| `ExclusivoEmVenda` no Create | PK herdada bloqueada (“Chave herdada do nível pai”); Update com a PK marcada — esperado |
| Reabertura | `Estado: teste de reencontro` |
| Cancelar | `[B034]` zero escrita |

## Apply

Primeira tentativa: IDE irresponsiva no thread da UI; GeneXus recusou SDT Create aninhado sem membros (`sdtEmpresa_API_CreateRequest_ExclusivoEmVenda`). Correção no gerador: pular SDT aninhado com 0 membros (plano, mapa BC, preflight). Fixture offline `ExclusiveCreateEmpty`.

Segunda tentativa (DLL com o skip):

| Check | Resultado |
|---|---|
| Outcome | `SuccessWithWarnings` |
| Duração | **107 min 23 s** — **alerta** (≫ 60 s) |
| Criados / Atualizados / Bloqueados | 50 / 3 / 0 |
| OwnSdts / Shared | 44 / 3 |
| `sdtEmpresa_API_CreateRequest_ExclusivoEmVenda` | **ausente** (skip) |
| Avisos | fallback de descrições em inglês; pasta `EmpresaOpenApi` reutilizada |

44 = 45 − 1: o filho só-PK-herdada não emite Create próprio. Nenhum nome estourou 128; nenhuma colisão.

## Build All

Primeira passagem: PostgreSQL **Success**; SQL Server (`CSharpModel`) Failed em `type_Sdt*.cs` (`CS0031`/`CS0029` no `initialize()`). Classificado na hora como ruído do gerador, não do Source da extensão.

Reexecução no mesmo dia (confirmação humana): **Success nos dois environments**, sem erros.

| Environment | Resultado final |
|---|---|
| `.NET Framework` + PostgreSQL (`NETFrameworkPostgreSQL`) | **Success**; `apiEmpresa` e `procEmpresa_API_*` especificados/gerados/compilados; sem `spc0018` |
| `.NET Framework` + SQL Server (`CSharpModel`) | **Success**; sem erros |

## Remover API gerada

Preview: 4 Procedures, 44 SDTs próprios, 3 compartilhados preservados, pasta reutilizada. Diálogo de confirmação passou a rolar a lista sem wrap (mesma forma da Output); Sim/Não visíveis.

| Check | Resultado |
|---|---|
| `[B086]` | `Deleted=50` (1 API + 4 Procedures + 44 SDTs + 1 File) |
| B081 | `Success`, Bloqueados=0, Avisos=0, `DuraçãoMs=31836` |
| Após Sim | diálogo some; IDE irresponsiva ~20–32 s no thread da UI (mesmo padrão do apply) |

Work With Objects após a remoção: **nenhum** `apiEmpresa`, `procEmpresa_API_*`, `sdtEmpresa_API_*` nem `apiEmpresa_Metadata`.

Permaneceram (previsto): `sdt_API_ErrorMessage`, `sdt_API_ErrorResponse`, `sdt_API_Pagination`, pasta `GxOpenAPI`, pasta `EmpresaOpenApi` vazia (`wasCreated=false` → nunca apagar), `GxOpenApiBuilder_Settings`, Business Component da `Empresa` ligado.

## Fora de escopo (explícito)

- HTTP na `Empresa` (critério 7 já fechou na `Teste`).
- Sync na `Empresa` (Fase 7 já fechou na `Teste`).
- Sinal de vida no apply/Remover (`B082`) — nova sessão, fora do corte `0.1.0-alpha.5`.

## Status

**Aprovado**, com alertas de tempo (abertura ~7 s; apply 107 min; Remover ~32 s sem casca). `Build All` Success nos dois environments. Gate da sprint fechado em 2026-08-29.
