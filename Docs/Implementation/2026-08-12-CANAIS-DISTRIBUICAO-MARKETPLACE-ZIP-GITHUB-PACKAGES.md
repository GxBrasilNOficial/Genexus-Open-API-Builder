# Canais de distribuição — Marketplace, ZIP, GitHub Packages e `.nupkg` (2026-08-12)

## Objetivo

Registrar o que a sessão de 2026-08-12 comprovou sobre **canais de instalação da extensão** no GeneXus 18 U15, além do caminho já canônico do [B094](B094-INSTALACAO-APENAS-COM-A-DLL-SEM-CLONAR.md) (DLL + Add > Local + `genexus /install`).

## Escopo

- Inclui: GitHub Packages / `.nupkg`; filtros reais de Add > Local e Install from file; estado do Marketplace pós-migração; limpeza pós-remoção da DLL; artefato ZIP de teste; Testes A e B na IDE.
- Exclui: mudança de código da extensão; alteração de `C:\Program Files (x86)\GeneXus` pelo agente; promoção do ZIP como caminho oficial em README/Release (ainda não).

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
5. **Install from file** (Start Page) aceita Browse só **`.zip`**, com categorias Extension / User Control / Pattern — mas **falhou** com o ZIP só com DLL (Teste A).
6. **Add > Local** aceita **`*.dll` ou `*.zip`**. Com o mesmo ZIP + `genexus /install`, a instalação **funcionou** (Teste B).

## Artefato ZIP de teste

Caminho local (gitignored via `Temp/*` e `*.zip`):

`Temp\install-from-file-test\GenexusOpenApiBuilder.Extension.0.1.0-alpha.1.zip`

- Conteúdo: somente `GenexusOpenApiBuilder.Extension.dll` na raiz.
- Fonte: `Src\Extension\bin\Release\net471\...`
- SHA-256 da DLL (idêntico a B094 / Alpha `0.1.0-alpha.1`): `3A5FD008B9B4D971D03DC10E50BF6C7D97813824FC5D6417498F4FDEC63D63EF`

## Testes A e B (2026-08-12)

Ambiente pré-teste: limpeza confirmada (seção seguinte) — sem OpenApiBuilder no catálogo / Packages.

### Teste A — Install from file (Start Page) — **falhou**

1. Start Page → Install from file → Browse → ZIP acima → Category **Extension** → Install.
2. Resultado: diálogo **Error installing extension**; sem detalhe adicional.
3. Extensions Manager: lista sem Genexus Open API Builder.
4. `Packages\`: sem `GenexusOpenApiBuilder.Extension.dll`.

Conclusão A: este formato de ZIP (só a DLL na raiz) **não** é aceito pelo Install from file nesta máquina/versão.

### Teste B — Add > Local + ZIP — **ok** (com `/install`)

1. Tools → Extensions Manager → Add → Local → mesmo ZIP → OK.
2. Extensão **listada**, checkbox **desmarcado**.
3. DLL presente em `C:\Program Files (x86)\GeneXus\GeneXus18\Packages\GenexusOpenApiBuilder.Extension.dll`.
4. Fechar IDE → `genexus /install` (cmd Administrador):
   - `Package 'C:\Program Files (x86)\GeneXus\GeneXus18\Packages\GenexusOpenApiBuilder.Extension.dll' added`
   - demais mensagens habituais (`Package Attribute not found`, NuGet modules).
5. Reabrir IDE: extensão **marcada**; menus principal (4) e contexto Transaction (3) ok.

Conclusão B: ZIP via **Add > Local** + fechar IDE + **`genexus /install`** é equivalente ao caminho DLL do B094 nesta evidência.

## Limpeza da instalação (pré-teste ZIP)

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
- ZIP pelo **mesmo** fluxo Add > Local + `/install` **também funciona** nesta evidência; Install from file **não**.
- Ainda **não** promover ZIP como anexo oficial de Release/README até decisão explícita (DLL sozinha basta e já está documentada).
- O item aberto do B094 sobre “Publicação/consumo do `.nupkg` por canal Web” fica **fechado com evidência negativa** nesta data.
- Marketplace / Add > Web **não** entram como guia operacional enquanto o feed/Marketplace permanecer nesse estado.
- **GitHub Packages (NuGet)** pode receber o `.nupkg` automaticamente via Actions (`.github/workflows/publish-github-packages.yml`): empacota a DLL anexada ao Release e faz push para `nuget.pkg.github.com/GxBrasilNOficial`. Continua **sem** valor para Extensions Manager / instalação na IDE.

## O que ainda falta

- Decisão de produto: anexar ZIP ao GitHub Release além da DLL (opcional; não bloqueia gate Sprint 8).
- Primeira publicação no Packages da org (rodar o workflow na tag `v0.1.0-alpha.1` ou no próximo Release).
- U14 e máquina “nunca usada com a extensão” continuam fora deste relatório (já no B094).

## Relacionados

- [B094 — Instalação apenas com a DLL](B094-INSTALACAO-APENAS-COM-A-DLL-SEM-CLONAR.md)
- [INSTALL — Alpha](../Public/INSTALL.md)
- [STATUS — checkpoint](../STATUS_ATUAL_E_PROXIMO_PASSO.md)
- [Release 0.1.0-alpha.1](../Releases/0.1.0-alpha.1.md)
