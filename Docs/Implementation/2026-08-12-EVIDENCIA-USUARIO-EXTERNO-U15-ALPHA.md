# Evidência — usuário externo na Alpha `0.1.0-alpha.1` (GeneXus 18 U15)

**Data:** 2026-08-12

**Contexto:** Sprint 9 / reforço de adoção (gate Sprint 8 já fechado na evidência U14)

**Issue:** [#3](https://github.com/GxBrasilNOficial/Genexus-Open-API-Builder/issues/3)

## Resumo

Segundo usuário externo confirmou que a Alpha `0.1.0-alpha.1` funcionou bem no GeneXus 18 Upgrade 15, com a mesma variante de instalação observada no relato U14 (DLL em `Packages` + `genexus /install`).

## Autor do relato

- **Nome:** Miguel (sobrenome não informado)
- **Canal:** mensagem direta / telefone; contato telefônico retido pelo mantenedor e **não** publicado no repositório nem na issue
- **Autorização do nome:** confirmada pelo mantenedor na sessão de registro (2026-08-12)

## Ambiente

| Item | Valor |
|------|--------|
| GeneXus | 18 Upgrade 15 |
| Artefato | DLL do GitHub Release `0.1.0-alpha.1` |
| Detalhe de KB/Transaction | não fornecido neste relato |

## Instalação observada

1. Salvou `GenexusOpenApiBuilder.Extension.dll` na pasta `Packages` prevista.
2. Executou `genexus /install`.

**Nota:** mesma variante do relato Igor / issue #1. O INSTALL público continua documentando Add > Local + `genexus /install` como caminho oficial.

## Evidência de runtime

Relato verbal/escrito ao mantenedor: “funcionou bem”. Sem captura arquivada neste registro.

## O que esta evidência reforça

- Segundo usuário externo da Alpha (após Igor no U14).
- Uso prático no U15 fora da máquina do mantenedor.
- Variante `Packages` + `/install` repetida por outro usuário.

## O que esta evidência não fecha

- Add > Local por usuário externo.
- Instalação sem elevação alguma.
- Demo/Sync/Remover/HTTP documentados passo a passo neste relato.
- Sobrenome completo do autor.

## Relação com o gate Sprint 8 e o B094

O gate Sprint 8 permanece fechado pela evidência U14 ([2026-08-12 — U14](2026-08-12-EVIDENCIA-USUARIO-EXTERNO-U14-ALPHA.md); issue #1). Este registro alimenta a Sprint 9 e complementa o B094 (U15 do mantenedor) com confirmação externa no mesmo upgrade.
