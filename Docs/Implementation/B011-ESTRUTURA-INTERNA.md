# B011 — Estrutura Interna Inicial

## Estado

Concluído em 2026-07-15.

## Decisão

A estrutura inicial de `Src` já atende ao contrato da seção 5.7 do documento de arquitetura:

| Caminho | Decisão em B011 |
|---|---|
| `Src/Extension/` | Mantido como único projeto nesta etapa, pois contém a extensão mínima e as referências ao Extensibility SDK. |
| `Src/Core/` | Mantido vazio, com `.gitkeep`; receberá a orquestração somente quando houver caso de uso validado. |
| `Src/Domain/` | Mantido vazio, com `.gitkeep`; receberá modelos e invariantes quando o `ApiPlan` for iniciado. |
| `Src/Infrastructure/` | Mantido vazio, com `.gitkeep`; receberá adaptadores do SDK e persistência quando o spike comprovar as APIs necessárias. |
| `Src/UI/` | Mantido vazio, com `.gitkeep`; receberá o wizard após a validação de carregamento e UI no spike. |

## Limites preservados

- nenhum projeto adicional foi criado;
- nenhuma camada recebeu código sem responsabilidade comprovada;
- `B010` permanece isolado no projeto de extensão;
- o carregamento da IDE, os comandos e a UI continuam pertencendo ao pacote de spike `B000`–`B006`.

## Evidência

A solution `Src/GenexusOpenApiBuilder.sln` contém somente `GenexusOpenApiBuilder.Extension`. As demais pastas já estavam materializadas com `.gitkeep` e foram mantidas como contrato de organização, sem introduzir dependências vazias.

## Próximo passo

Promover `B012` para confirmar e aplicar as convenções de nomes congeladas na documentação.