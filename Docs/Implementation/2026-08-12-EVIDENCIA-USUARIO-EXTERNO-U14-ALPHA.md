# Evidência — usuário externo na Alpha `0.1.0-alpha.1` (GeneXus 18 U14)

**Data:** 2026-08-12  
**Gate:** Sprint 8 (usuário externo)  
**Issue:** [#1](https://github.com/GxBrasilNOficial/Genexus-Open-API-Builder/issues/1)

## Resumo

Usuário externo instalou a DLL da Alpha publicada, carregou a extensão no GeneXus 18 Upgrade 14 e gerou API a partir de uma Transaction. Fecha o gate da Sprint 8 e o residual de carregamento/uso prático em U14 (antes só comprovado no U15 do mantenedor).

## Autor do relato

- **Nome:** Igor C. Menin
- **Canal:** mensagem direta (captura + respostas), registrado pelo mantenedor na issue #1 conforme `CONTRIBUTING.md`
- **Autorização do nome:** confirmada pelo mantenedor na sessão de registro (2026-08-12)

## Ambiente

| Item | Valor |
|------|--------|
| GeneXus | 18 Upgrade 14 (`18.0.187820 U14`) |
| KB | `KbTesteGx18U14` |
| Transaction | `Teste` |
| Environment visível | `.NETSQLServer` / `Release` |
| Artefato | DLL do GitHub Release `0.1.0-alpha.1` |

## Instalação observada

1. Copiou `GenexusOpenApiBuilder.Extension.dll` para a pasta `Packages` da instalação GeneXus.
2. Executou `genexus /install`.

**Nota:** o INSTALL público documenta Add > Local + `genexus /install`. A cópia manual em `Packages` é uma **variante** que também deixa a DLL no local varrido pelo `/install`. Não é promovida como caminho oficial (continua exigindo escrita em Program Files / permissão adequada).

## Evidência de runtime

Captura: [Docs/Images/alpha-u14-igor-menin.png](../Images/alpha-u14-igor-menin.png)

Observado na IDE:

- diálogo About com GeneXus 18 U14;
- menu **Genexus Open API Builder** presente;
- Folder `TesteOpenApi` com `apiTeste`, Procedures `procTeste_API_{Create,Get,List,Update}` e SDTs `sdtTeste_API_*` de request/response/filtros.

## O que esta evidência fecha

- Gate Sprint 8: uso por usuário externo + feedback registrado (issue #1).
- Residual U14: carregamento da extensão e geração prática de objetos (compatibilidade das APIs do SDK usadas pelo wizard) em Upgrade 14.

## O que esta evidência não fecha

- Add > Local executado por esse usuário (usou cópia em `Packages`).
- Instalação sem elevação alguma.
- Cobertura completa de Sync/Remover/HTTP/DEMO passo a passo (a captura prova geração + menus; não substitui bateria U15 do mantenedor).
- Promoção da cópia manual como guia público.

## Relação com B094

O B094 comprovou no U15 do mantenedor o caminho Add > Local + `/install`. Esta frente comprova uso externo real da Alpha em U14 com variante de colocação da DLL. Os dois registros se complementam; o guia canônico permanece o do INSTALL.
