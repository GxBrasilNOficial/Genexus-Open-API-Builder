# B012 — Convenções de Nomes Aplicáveis

## Estado

Concluído em 2026-07-15.

## Fontes confrontadas

- documento 05, seção 5.7: define os nomes obrigatórios da solution e do projeto mínimo;
- documento 11: define nomes de objetos GeneXus e saídas que só existem após selecionar uma Transaction;
- registro funcional de 2026-07-14: confirma os padrões de Procedures e SDTs derivados da Transaction.

## Aplicação no código mínimo

| Escopo | Convenção aplicada | Evidência |
|---|---|---|
| Produto | `Genexus Open API Builder` como nome público. | `README.md` e documentação Foundation. |
| Solution | `GenexusOpenApiBuilder.sln`. | Caminho exigido pelo documento 05, seção 5.7. |
| Projeto de extensão | `GenexusOpenApiBuilder.Extension`. | Nome do `.csproj`, `AssemblyName` e `RootNamespace`. |
| Namespace inicial | `GenexusOpenApiBuilder.Extension`. | Mantém correspondência direta com o assembly e evita um namespace genérico. |
| Referências do SDK | Pacotes NuGet e MSBuild SDKs oficiais do GeneXus. | Não há referências diretas a DLLs da instalação. |

Os nomes já presentes permanecem sem alteração, pois atendem ao contrato de layout e não conflitam com as convenções do produto.

## Convenções reservadas à geração

Ainda não há Transaction, `ApiPlan` ou objetos GeneXus a gerar. Portanto, os seguintes padrões foram confirmados, mas não materializados prematuramente:

- API: `api<NomeBase>`;
- SDTs: `sdt<NomeBase>_API_<Finalidade>`;
- Procedures: `proc<NomeBase>_API_<Operação>`;
- serviços: `List`, `Get`, `Create` e `Update`;
- `RestPath`: minúsculo, hifenizado quando necessário e sem pluralização automática;
- colisões: bloquear; nunca gerar sufixos automáticos `_v2` ou equivalentes.

## Decisão

B012 não cria classes, projetos, objetos GeneXus ou regras de transformação de nomes. Esses artefatos dependem de uma Transaction e das APIs públicas verificadas no pacote de spike. B000–B005 foram concluídos posteriormente no U15; a próxima missão é `B006`, validar persistência de metadata em File após fechar e reabrir a KB conforme o checkpoint `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`.
