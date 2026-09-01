# Missão estacionada — sinal de vida no Wizard (abertura e apply) e no Remover

Data: 2026-08-29.
Estado: **recado histórico**. Fases A+B do `B082` foram implementadas em 2026-08-31 (`Docs/Implementation/2026-08-31-B082-PLANO-UX-PROGRESSO.md`). A medição do critério 11 **encerrou**; não misturar com `B108` nem com o corte `0.1.0-alpha.5` / fechamento do `B100`.
Correlato de backlog: `B082` (mostrar tempo de execução) — o tempo sozinho não resolve o que foi visto; o usuário precisa de feedback **enquanto** a IDE está bloqueada.

## O que aconteceu

Na cópia `Gx_FabricaBrasil`, Transaction `Empresa` (13 subníveis; Wizard na casa de 49 objetos planejados; apply `OwnSdts=44` após o skip do Create vazio; cabeçalho ~162 attrs / `ListResponse_Item` com 175 membros):

1. **Abertura** do Wizard ~7 s (alerta do critério 11: > 5 s; abaixo da reprova de 30 s). Sem indicador de “carregando, aguarde”.
2. **Apply** (`Concluir e aplicar`) no thread da UI: a IDE fica irresponsiva; a janela chega a colapsar numa fatia no canto, tela preta. Comparado ao WorkWithWeb, que gera objetos demorados sem parecer travamento.
3. Relato já existente de usuário que **fechou o GeneXus na marra** quando não viu perspectiva de término. Esse é o risco de produto, não só cosmética.
4. **Remover API gerada** na mesma `Empresa` (50 `Delete()`): ao confirmar Sim o diálogo some e a IDE fica irresponsiva ~20–32 s (`DuraçãoMs=31836`) até o relatório B081. Mesmo padrão: `ShowDialog` fecha e o trabalho segue no thread da UI (`Package.cs` / `ApiPlanGeneratedApiRemover.Remove`).

## Por que fica pior que o WorkWithWeb

O Wizard é `ShowDialog`. Em `Concluir e aplicar` o diálogo fecha (`DialogResult.OK`) e **depois** `Package.cs` grava SDTs, Procedures, API e metadata no mesmo thread da UI, ainda dentro do comando de menu. A casca some, o comando não devolveu, e o GeneXus não pinta.

O `KBModel` da extensão vive nesse thread. Mandar `Save()` para background não é a primeira hipótese — afinidade STA/UI do SDK.

## Abordagem (implementada nas Fases A+B)

Sem mudar o contrato de escrita (preflight antes do primeiro `Save()`, mesma ordem de objetos). Detalhe e evidência: `Docs/Implementation/2026-08-31-B082-PLANO-UX-PROGRESSO.md`.

1. Diálogo de progresso visível durante apply, Sync e Remover (incluindo Preview).
2. Cursor de espera no owner da IDE.
3. Sinal na abertura do Wizard (casca + tempos `PrefsMs` / `ContratoMs` / `InterfaceMs` na Output). O índice único da KB **não** entra nessa abertura: na `Empresa` (2026-08-31) `IndiceKb=2394` ms foi no Apply, depois de Concluir.
4. Aviso no Resumo quando SDTs+Procedures planejados ≥ 25.

## Fora deste recado

Critério 11 (escala) **fechou** em 2026-08-29: `Docs/Implementation/2026-08-29-CRITERIO11-ESCALA-EMPRESA.md`. Este arquivo não pede reabrir a medição.
