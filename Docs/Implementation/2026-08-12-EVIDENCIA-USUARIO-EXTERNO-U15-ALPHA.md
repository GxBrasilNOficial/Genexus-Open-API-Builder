# Evidência — usuário externo na Alpha `0.1.0-alpha.1` (GeneXus 18 U15)

**Data:** 2026-08-12

**Contexto:** Sprint 9 / reforço de adoção (gate Sprint 8 já fechado na evidência U14)

**Issue:** [#3](https://github.com/GxBrasilNOficial/Genexus-Open-API-Builder/issues/3)

## Resumo

Segundo usuário externo confirmou que a Alpha `0.1.0-alpha.1` funcionou bem no GeneXus 18 Upgrade 15. Instalou pelo **caminho de mantenedor**: baixou o repositório, usou a DLL do **build local** do repo e executou `Install-ExtensionForGeneXus18.bat` (cópia para `Packages`) antes de `genexus /install`.

**Correção (2026-08-12):** o registro inicial igualava este relato à variante Packages do Igor (issue #1 / DLL do Release). O caminho real é o do mantenedor com build local; não é evidência da variante Packages/Release.

## Autor do relato

- **Nome:** Miguel (sobrenome não informado)
- **Canal:** mensagem direta / telefone; contato telefônico retido pelo mantenedor e **não** publicado no repositório nem na issue
- **Autorização do nome:** confirmada pelo mantenedor na sessão de registro (2026-08-12)

## Ambiente

| Item | Valor |
|------|--------|
| GeneXus | 18 Upgrade 15 |
| Artefato | DLL do **build local** do repositório (não a DLL anexada ao GitHub Release) |
| Detalhe de KB/Transaction | não fornecido neste relato |

## Instalação observada

1. Baixou o repositório para a máquina.
2. Executou `Install-ExtensionForGeneXus18.bat` (copia a DLL do build local para `Packages`; exige elevação típica).
3. Executou `genexus /install`.

**Nota:** este é o fluxo documentado para mantenedor em [INSTALL.md](../Public/INSTALL.md), não o caminho do usuário final (DLL do Release + Add > Local). Não confundir com a variante Packages + `/install` do relato U14 (Igor / issue #1), que usou a DLL do Release sem o `.bat`.

## Evidência de runtime

Relato verbal/escrito ao mantenedor: “funcionou bem”. Sem captura arquivada neste registro.

## O que esta evidência reforça

- Segundo usuário externo da Alpha (após Igor no U14).
- Uso prático no U15 fora da máquina do mantenedor.
- Fluxo de mantenedor (`Install-ExtensionForGeneXus18.bat` + `/install`) reproduzido por outro usuário com build local.

## O que esta evidência não fecha

- Instalação só com a DLL do GitHub Release (Add > Local ou cópia manual em `Packages`) por este usuário.
- Add > Local por usuário externo.
- Instalação sem elevação alguma.
- Demo/Sync/Remover/HTTP documentados passo a passo neste relato.
- Sobrenome completo do autor.

## Relação com o gate Sprint 8 e o B094

O gate Sprint 8 permanece fechado pela evidência U14 ([2026-08-12 — U14](2026-08-12-EVIDENCIA-USUARIO-EXTERNO-U14-ALPHA.md); issue #1). Este registro alimenta a Sprint 9 e confirma uso externo no U15 pelo caminho de mantenedor; **não** substitui o B094 nem a evidência de instalação só com a DLL do Release.
