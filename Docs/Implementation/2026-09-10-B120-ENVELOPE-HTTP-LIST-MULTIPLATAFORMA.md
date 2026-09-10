# B120 — Envelope HTTP do `List` entre environments

**Estado:** aberto; urgente; deliberadamente estacionado até o encerramento da
sprint `S-B111`.

**Correlato de backlog:** [`B120`](../Foundation/06-BACKLOG_v0.1.md).

**Evidência principal:** [`B071-B073-B079-GET-CREATE-UPDATE-HTTP.md`](B071-B073-B079-GET-CREATE-UPDATE-HTTP.md), seção
«Primeiro smoke HTTP de `List` após os Build All».

## 1. Decisão de encaminhamento

O problema deve virar uma frente urgente de backlog, mas não deve interromper a
conclusão da sprint `S-B111`. O smoke já demonstrou que a aplicação inicia, a
autenticação funciona e o endpoint responde nos dois environments; o que falhou
foi a equivalência observável do contrato de resposta do `List`.

`B120` não muda a próxima ação única do checkpoint: concluir a evidência dos
cenários de Sync que ainda faltam para o aceite da F1. Depois desse encerramento,
este item deve ser puxado antes de considerar o contrato HTTP do `List` validado
de forma multiplataforma.

## 2. Problema observado

Em 2026-09-10, depois de `Build All` bem-sucedido nos dois environments, foi
executado o mesmo `GET /notafiscal` com e sem token, sem alteração de dados:

| Verificação | .NET Framework / SQL Server | .NET / PostgreSQL |
| --- | --- | --- |
| Sem token | `401` em JSON | `401` em JSON |
| Com token | `200`; 22 itens; página 1; tamanho 50; total 22; 1 página | `200`; 10 itens; página 1; tamanho 50; total 10; 1 página |
| Forma efetiva do corpo | `ListResponse` e `ErrorResponse` no nível raiz; paginação e filtros dentro de `ListResponse` | `Items`, `Pagination` e `AppliedFilters` no nível raiz; sem `ErrorResponse` no nível raiz |

Os totais diferentes são compatíveis com os dados de cada banco e não são o
defeito deste item. A divergência relevante é a forma do corpo para o mesmo
contrato gerado.

Os dois arquivos `apiNotaFiscal.yaml` declararam `ListOutput` com as
propriedades `ListResponse` e `ErrorResponse`. Portanto, o resultado atual é
incompatível com o contrato publicado, ainda que ambos os requests retornem
HTTP `200`.

## 3. Diagnóstico delimitado

O writer da extensão continua emitindo o contrato público atual do serviço:

- `List` declara `out: &ListResponse` e `out: &ErrorResponse`;
- a chamada da Procedure repassa `&ListResponse`, `&ErrorResponse` e
  `&RestStatusCode`;
- esse contrato é validado por `ApiPlanServiceSourceContract`.

O runtime gerado observado não trata os dois environments da mesma forma. No
artefato .NET Framework, o wrapper WCF expõe uma resposta envelopada. No
artefato .NET/PostgreSQL, o wrapper possui um caminho de retorno direto quando
há somente um valor de saída não nulo, achatando o conteúdo de `ListResponse`.

O diagnóstico atual, portanto, é uma divergência no wrapper/serialização gerado
ou na interação entre o gerador nativo e o contrato de múltiplas saídas. Não há
evidência de que seja problema de consulta, autenticação, IIS, DLL antiga,
navegador ou do conteúdo específico do PostgreSQL. Também não há base segura
para remover `ErrorResponse` público ou reordenar saídas apenas para fazer o
PostgreSQL coincidir por tentativa.

Esta classificação ainda precisa ser confirmada por uma reprodução mínima. O
fato de o sintoma aparecer no artefato gerado não autoriza editar arquivos
gerados em `C:\KBs` nem alterar a instalação do GeneXus.

## 4. Contrato a preservar até decisão explícita

Enquanto `B120` estiver aberto, o contrato de trabalho é o que a extensão e os
YAML atuais declaram:

1. o sucesso do `List` tem um `ListOutput` estável, contendo `ListResponse` e
   `ErrorResponse` nos mesmos níveis nos dois environments;
2. `ListResponse` contém `Items`, `Pagination` e `AppliedFilters`;
3. `ErrorResponse` permanece público, mesmo quando vazio em um sucesso;
4. respostas de erro do `List` devem ser comparáveis por status e forma do corpo
   entre os dois environments;
5. a correção não pode quebrar o contrato já validado de `Get`, `Create`,
   `Update` ou `Delete`.

O item deverá ratificar formalmente essa forma antes de implementar a solução.
Se o GeneXus exigir outra forma canônica suportada, a decisão deve atualizar
simultaneamente o writer, os YAMLs, a documentação e os testes; não basta
adaptar um dos environments.

## 5. Plano de investigação após a sprint

1. Confirmar as versões exatas do GeneXus 18, geradores e configurações dos dois
   environments, e verificar se há correção oficial ou requisito conhecido para
   respostas REST com múltiplas saídas.
2. Criar uma reprodução mínima e descartável com um API Object simples que tenha
   duas saídas, uma de dados e uma de erro, sem depender da Transaction
   `NotaFiscal` ou da quantidade de registros.
3. Comparar o código gerado, o YAML e os corpos HTTP de sucesso e erro nos dois
   environments.
4. Testar somente alternativas de contrato suportadas pelo GeneXus: não
   modificar a assinatura pública do `List` no produto antes de saber se a
   mudança é compatível com o runtime e com o YAML.
5. Decidir entre correção na emissão da extensão, requisito mínimo de runtime ou
   limitação documentada da plataforma.
6. Aplicar a decisão em fonte, se ela for da extensão, regenerar nos dois
   environments, executar `Build All` nos dois e repetir a bateria HTTP.
7. Atualizar o contrato OpenAPI, a documentação pública e os testes somente
   depois de os corpos reais coincidirem nos dois ambientes.

## 6. Matriz de aceitação

| Caso | Resultado exigido |
| --- | --- |
| `List` sem token | Mesmo status `401` e corpo de erro equivalente nos dois environments |
| `List` autenticado sem filtros | `200` e exatamente o mesmo envelope raiz declarado no YAML |
| `List` autenticado com filtro | Mesmo envelope, filtro aplicado e paginação equivalente |
| Página e tamanho válidos | Mesma forma de `Pagination`, com valores coerentes |
| Página ou tamanho inválidos | Mesmo status `400` e mesmo contrato de erro |
| Contrato publicado | YAML, wrapper gerado e corpo HTTP real concordantes nos dois environments |
| Regressão | `Get`, `Create`, `Update` e `Delete` continuam passando seus testes existentes |

O aceite não pode ser baseado somente em `200`, em contagem de itens ou em um
único environment.

## 7. Limites e não objetivos

- Não editar manualmente `apinotafiscal.cs`, `apinotafiscal_services.cs` ou
  qualquer outro artefato gerado em `C:\KBs` como correção do produto.
- Não alterar, copiar ou reparar arquivos em `C:\Program Files (x86)\GeneXus`.
- Não remover `ErrorResponse` público sem uma decisão específica e uma revisão
  dos consumidores, pois isso pode regredir o contrato de erros do `List`.
- Não confundir este item com a lacuna de cenários de Sync da F1 da `S-B111`.
- Não considerar o Build Release da extensão suficiente para validar a forma
  serializada do runtime.
- Não fechar `B120` com base apenas em uma correção observada no PostgreSQL; os
  dois environments precisam ser comparados na mesma rodada.

## 8. Dependências e saída esperada

Dependências: GeneXus 18 e seus dois geradores configurados, KB de teste,
`apiNotaFiscal`, `ListResponse`, `ErrorResponse`, acesso aos dois endpoints,
autenticação disponível e `Build All` reproduzível.

Saída esperada: uma decisão técnica rastreável que identifique o dono da
divergência, defina o envelope canônico, registre a correção ou a limitação
suportada e anexe evidência HTTP comparável de sucesso e erro nos dois
environments.

## 9. Relação com o estado atual

- O smoke HTTP que abriu este item está registrado em
  [`B071-B073-B079-GET-CREATE-UPDATE-HTTP.md`](B071-B073-B079-GET-CREATE-UPDATE-HTTP.md).
- O estado da F1 e a única ação vigente estão em
  [`2026-09-10-S-B111-F1-RECONCILIACAO-EVIDENCIA.md`](2026-09-10-S-B111-F1-RECONCILIACAO-EVIDENCIA.md).
- A abertura de `B120` não promove a F1, não fecha sua lacuna de Sync e não
  altera a decisão de manter `B108` estacionado.
