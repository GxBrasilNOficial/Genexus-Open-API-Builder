# B119 — Limpeza de variáveis locais sem uso

**Estado:** planejado; manutenção futura.

**Correlato de backlog:** [`B119`](../Foundation/06-BACKLOG_v0.1.md).

**Escopo:** limpeza pontual de código local, sem mudança de comportamento esperado.

## Objetivo

Auditar e remover variáveis locais declaradas sem uso efetivo, começando por
`b111ManagedApply` no fluxo de Sync em `Src/Extension/Package.cs`.

O item fica fora da frente atual da sprint `S-B111` e deve ser puxado
separadamente, como manutenção de clareza do código.

## Evidência inicial

- O fluxo de Sync declara `b111ManagedApply` junto da preparação do `ApiPlan`.
- No estado atual, essa declaração do Sync não possui consumidor posterior no
  fluxo.
- A declaração homônima do fluxo de Wizard é usada no teste que decide se há
  escrita a executar e não faz parte deste item.
- O teste `Tests/ApplicationFinalReport/Test-ApiPlanApplicationFinalReport.ps1`
  verifica textualmente que o mesmo predicado aparece nos dois fluxos; uma
  limpeza futura deverá atualizar esse contrato de teste somente se a decisão
  técnica for remover a duplicação.
- O build Release e os checkers atuais passam; portanto, a evidência inicial
  caracteriza manutenção de clareza, não uma falha funcional observada.

## Limites

- não alterar a lógica do Wizard ou do Sync além de remover variáveis
  comprovadamente sem uso;
- não remover métodos, parâmetros, campos ou variáveis apenas por busca textual;
- não confundir símbolos públicos ou usados por reflexão, geração, testes ou
  contratos com variáveis locais sem uso;
- não transformar este item em refatoração ampla.

## Plano futuro

1. Revalidar o uso real da variável e possíveis referências indiretas.
2. Remover somente a declaração que permanecer sem consumidor.
3. Executar testes, restore/build Release e os checkers aplicáveis.
4. Revisar o diff e registrar a evidência do comportamento preservado.

## Critério de aceite

- `b111ManagedApply` do Sync é removida somente se a revalidação confirmar que
  não há uso efetivo;
- a lógica e o contrato do Wizard permanecem inalterados;
- testes, checkers e build passam;
- não sobra referência órfã ao símbolo no trecho alterado;
- o item só é fechado com commit específico e evidência da validação.
