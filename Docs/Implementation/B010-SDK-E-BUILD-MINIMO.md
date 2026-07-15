# B010 — SDK e Build Mínimo

## Estado

Template preparado antes da execução de `B010`. Este arquivo ainda não constitui evidência de conclusão.

## Objetivo da evidência

Registrar de forma reproduzível como o Extensibility SDK do GeneXus 18 Upgrade 15 foi localizado, referenciado e usado para compilar a solution e o projeto mínimos da extensão.

## Ambiente-base

- sistema operacional:
- versão e upgrade do GeneXus:
- origem e forma de obtenção do SDK:
- versão efetivamente encontrada:
- pré-requisitos instalados:

O resultado deve ser reproduzível em outra máquina Windows que tenha o GeneXus 18 Upgrade 15 e os pré-requisitos registrados. Caminhos absolutos específicos da máquina podem aparecer como evidência local, mas não podem ser necessários nem versionados como configuração do build.

## Localização reproduzível das dependências

- método de descoberta:
- assemblies ou pacotes necessários:
- mecanismo usado pelo projeto para resolver as referências:
- variáveis, propriedades ou convenções de caminho:

## Artefatos mínimos

- solution esperada: `Src/GenexusOpenApiBuilder.sln`
- projeto esperado: `Src/Extension/GenexusOpenApiBuilder.Extension.csproj`
- target framework confirmado durante `B010`:
- tipo de projeto e empacotamento confirmados durante `B010`:

## Build reproduzível

- comando executado:
- diretório de execução:
- resultado:
- data da validação:

## Evidência

- trecho relevante do log:
- arquivos produzidos:
- observações e limitações:

## Critério de encerramento

Preencher todas as seções aplicáveis, obter build mínimo bem-sucedido e atualizar `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md` para promover `B011`. O carregamento da extensão na IDE pertence a `B000`, não a `B010`.
