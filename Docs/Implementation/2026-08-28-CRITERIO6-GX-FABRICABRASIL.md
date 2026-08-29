# Critério 6 — Validação em `Gx_FabricaBrasil`

Data: 2026-08-28.
Gate: Sprint 9, critério 6 (estrutura real).
Escopo: Wizard na cópia local da KB de produção; medição de contagens, avisos e bloqueios.

## Medição offline (consulta XML — 2026-08-28)

Fonte: `C:\Dev\Prod\Gx_FabricaBrasil\ObjetosDaKbEmXml\Transaction` (somente leitura).

| Transaction | Filhos diretos | Max depth | Perfil |
|---|---:|---:|---|
| `Empresa` | 13 | 2 | Larga e rasa; raiz ~162 attrs |
| `DadosDoDia` | 2 (`Turno`, `CentroCusto`) | 3 | `Turno` → `Funcionario` |
| `CondicaoPagamento` | 1 (`Parcelas`) | 2 | Linear mínima |

`Empresa` — filhos: `CriacaoVolumes`, `VolumeDeProdutoComParteCarcacaAnimal`, `ExclusivoEmVenda`, `BusifrigComissionadoIgnorar`, `BusifrigPessoaGrupo`, `BusifrigAnimalParaAbateGrupo`, `BusifrigProdutoGrupo`, `AbateCalculo`, `GeracaoAutomaticaDFe`, `FaixaIdDePessoa`, `TipoDeRomaneioAntigoParaBusifrigTipolancto`, `EtiquetagemDoAbate`, `BloqueioDeVolumeParaSaida`.

## Protocolo Wizard (manual — U15)

Pré-requisito: DLL atual instalada (`Install-ExtensionForGeneXus18.bat` como admin; IDE fechada). Manifesto inalterado → sem `genexus /install` salvo se o pacote mudou.

1. Abrir a KB `Gx_FabricaBrasil` (cópia local de produção).
2. Para cada Transaction da tabela: abrir Wizard pelo menu de contexto.
3. Registrar na Output / relatório B081:
   - tempo até abrir e até o preview (limiares: alerta > 5 s; reprova > 30 s);
   - contagem de subníveis exibidos vs medição offline;
   - aviso de profundidade (só se depth > 4 — nenhum destes casos);
   - cancelar sem gravar **ou**, se houver autorização explícita para apply em cópia, aplicar só `CondicaoPagamento` (menor) e anotar `Created`/`Updated`/`Blocked`/`Warnings`.
4. Não versionar XML/XPZ de cliente neste repositório.

## Smoke Wizard IDE (2026-08-28)

KB: cópia local `Gx_FabricaBrasil`. DLL vigente (pós-correção Sync). Preferências ausentes → defaults em memória (esperado). Os três wizards **cancelados** sem escrita na KB (`B034`).

| Transaction | Tempo até abrir | Limiar | Resultado |
|---|---:|---|---|
| `CondicaoPagamento` | ≤ 3 s | alerta > 5 s; reprova > 30 s | OK |
| `DadosDoDia` | ≤ 3 s | idem | OK |
| `Empresa` | ~6 s | idem | **Alerta** (acima de 5 s; abaixo de 30 s) |

Conferência visual do seletor de nível (aba Requests), 2026-08-28:

| Transaction | Observado | vs XML |
|---|---|---|
| `CondicaoPagamento` | `Cabeçalho` + `Parcelas` | OK |
| `DadosDoDia` | `Cabeçalho`, `Turno`, `Turno / Funcionario`, `CentroCusto` | OK (depth 3) |
| `Empresa` | lista com os 13 subníveis (ex.: `CriacaoVolumes`, `GeracaoAutomaticaDFe`, `EtiquetagemDoAbate`…) — dropdown curto exigia rolagem | OK estrutura; UX do dropdown melhorada em seguida |

## Status

- Offline: feito.
- Tempos de abertura IDE: feitos.
- Contagem visual de subníveis vs XML: **aprovada**.
- Apply: não executado (cancelado de propósito).
- UX: altura do dropdown do seletor de nível passa a usar ~55% da altura do diálogo — **confirmado em `Empresa` (14 itens visíveis sem rolagem minúscula)**.
