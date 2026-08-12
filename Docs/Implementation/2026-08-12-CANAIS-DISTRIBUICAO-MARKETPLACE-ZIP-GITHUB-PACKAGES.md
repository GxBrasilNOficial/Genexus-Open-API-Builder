# Canais de distribuição — Marketplace, ZIP, GitHub Packages e `.nupkg` (2026-08-12)

## Objetivo

Registrar, antes de qualquer promoção pública, o que a sessão de 2026-08-12 comprovou sobre **canais de instalação da extensão** no GeneXus 18 U15, além do caminho já canônico do [B094](B094-INSTALACAO-APENAS-COM-A-DLL-SEM-CLONAR.md) (DLL + Add > Local + `genexus /install`).

## Escopo

- Inclui: GitHub Packages / `.nupkg`; filtros reais de Add > Local e Install from file; estado do Marketplace pós-migração; limpeza pós-remoção da DLL; preparação do artefato ZIP de teste.
- Exclui: mudança de código da extensão; instalação bem-sucedida via ZIP (Testes A/B ainda pendentes); alteração de `C:\Program Files (x86)\GeneXus` pelo agente.

## Conclusões fechadas nesta data

1. **GitHub Packages não instala a extensão na IDE.** A aba Packages do GitHub é feed NuGet/npm/Docker. Publicar `.nupkg` lá **não** faz o Extensions Manager consumir nem ativar o pacote.
2. **O `.nupkg` da build é artefato técnico**, não instalador da IDE. Contém a mesma DLL em `lib/net471/`. Add > Local **não aceita** `.nupkg` (OK desabilitado) — já no B094; confirmado de novo.
3. **Não existe canal do Extensions Manager que consuma `.nupkg`.**
   - Canal **Web** = Marketplace via RSS (`ExtensionsRss` = `http://marketplace.genexus.com/afeed2.aspx?2` na config do GeneXus 18).
   - Add > **Local** filtra `*.dll` | `*.zip`.
   - NuGet na IDE = módulos de KB (`.gxmodules`), outro domínio.
4. **Marketplace “pós-migração” está inutilizável nesta máquina para Extensions:**
   - site antigo `marketplace.genexus.com` → página “We have moved!” → `market.gxapps.cloud`;
   - feed `afeed2.aspx?2` no domínio antigo = HTML de migração; no host novo = resposta vazia (0 bytes);
   - Start Page → Marketplace → Extensions / Patterns / External Tools / External Objects → **403 CloudFront**;
   - User Controls → “There are no products to display”;
   - Extensions Manager → Add → **Web** → erro na leitura do feed.
5. **Install from file** (Start Page) aceita Browse só **`.zip`**, com categorias Extension / User Control / Pattern.
6. **Add > Local** aceita **`*.dll` ou `*.zip`**.

## Artefato ZIP de teste (ainda não comprovado na IDE)

Caminho local (gitignored via `Temp/*` e `*.zip`):

`Temp\install-from-file-test\GenexusOpenApiBuilder.Extension.0.1.0-alpha.1.zip`

- Conteúdo: somente `GenexusOpenApiBuilder.Extension.dll` na raiz.
- Fonte: `Src\Extension\bin\Release\net471\...`
- SHA-256 da DLL (idêntico a B094 / Alpha `0.1.0-alpha.1`):  
  `3A5FD008B9B4D971D03DC10E50BF6C7D97813824FC5D6417498F4FDEC63D63EF`

### Roteiro pendente

| Teste | Sequência | Critério |
| --- | --- | --- |
| A | Start Page → Install from file → ZIP → Category **Extension** → Install | Lista / marcada / menus; se desmarcada, `genexus /install` |
| B | Extensions Manager → Add → Local → mesmo ZIP | Mesmo critério; limpar entre A e B se A instalar |

Enquanto A/B não passarem, **não** promover ZIP como caminho oficial em README/Release/CHANGELOG.

## Limpeza da instalação (2026-08-12)

1. Remoção manual de `C:\Program Files (x86)\GeneXus\GeneXus18\Packages\GenexusOpenApiBuilder.Extension.dll`.
2. Reabertura da IDE **sem** `/install` prévio → extensão sumiu do Extensions Manager; log `GXLogging.log` às **07:20:49** com `FileNotFoundException` ao tentar carregar a DLL ausente.
3. Fechar IDE → `genexus /install` (cmd Administrador, pasta GeneXus18):
   - varredura de Packages **sem** `GenexusOpenApiBuilder` (esperado);
   - `Package Attribute not found` em DLLs auxiliares = ruído habitual;
   - fase longa Publishing/Installing modules via NuGet em `.gxmodules` = normal.
4. Reabertura ~**07:41**:
   - catálogo `C:\ProgramData\GeneXus\GeneXus\18\packages.143920.xml` regenerado às **07:35:53**, **sem** OpenApiBuilder / GxBrasilNOficial;
   - `PostInitializing` sem a extensão;
   - nenhum `Could not load package` novo da nossa DLL;
   - DLL em Packages continua ausente.

Ambiente GeneXus ficou limpo para o teste do ZIP.

## Relação com B094 e INSTALL

- Caminho canônico do usuário final permanece: **DLL** do GitHub Release → Add > Local → fechar IDE → `genexus /install` — ver [INSTALL.md](../Public/INSTALL.md) e B094.
- O item aberto do B094 sobre “Publicação/consumo do `.nupkg` por canal Web” fica **fechado com evidência negativa** nesta data (remissão neste documento).
- Marketplace / Add > Web / GitHub Packages **não** entram como guia operacional enquanto o feed/Marketplace permanecer nesse estado.

## O que ainda falta

- Executar e registrar resultado dos Testes A e B (ZIP).
- Decidir se, com ZIP comprovado, anexar ZIP ao Release além da DLL (só após evidência).
- U14 e máquina “nunca usada com a extensão” continuam fora deste relatório (já no B094).

## Relacionados

- [B094 — Instalação apenas com a DLL](B094-INSTALACAO-APENAS-COM-A-DLL-SEM-CLONAR.md)
- [INSTALL — Alpha](../Public/INSTALL.md)
- [STATUS — checkpoint](../STATUS_ATUAL_E_PROXIMO_PASSO.md)
- [Release 0.1.0-alpha.1](../Releases/0.1.0-alpha.1.md)
