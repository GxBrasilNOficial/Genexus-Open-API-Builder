# Plano — Suporte paralelo GeneXus 18 U13 (Opção B)

**Status:** plano v12 — documento **autocontido**; fila serial encerrada; polish seletivo pós-GLM; v11 pré-execução; v12 absorve ressalvas Opus 5 (segunda opinião)
**Data:** 2026-08-12
**Versão do documento:** 12
**Produto:** Genexus Open API Builder
**Decisão:** Opção **B** — segundo artefato oficial no Release para **GeneXus 18 Upgrade 13**, sem contaminar U14+
**Nomenclatura:** `Gx18u13` em solutions, projetos, assets e scripts (**não** usar `Legacy`)

Este documento **não** autoriza merge do PR [#2](https://github.com/GxBrasilNOficial/Genexus-Open-API-Builder/pull/2) nem alteração imediata da `main`. Define como chegar a B com isolamento.

**Baseline atual:** GeneXus 18 **U14+** via `GeneXus.Package.UI.Sdk` (B010/B000); U14 confirmado na Alpha; U15 = ambiente do mantenedor. O contribuidor Igor C. Menin validou U14 na Alpha e possui instalação U13 em path não default (observado no PR #2: `C:\GeneXus\Gx18\U13`). O mantenedor **não** tem U13 nesta máquina; Exp-Compat / Exp-Carga / população de `Src/Lib/Gx18u13` dependem desse canal.

**Checkpoint operacional:** `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md` ainda aponta Sprint 9 (feedback Alpha) como próxima ação única. Este plano **não** substitui esse checkpoint até a execução ser iniciada (D43).

---

## 1. Objetivo

Dois artefatos oficiais, quando ambos validados:

| Artefato no Release | Faixa GeneXus | Como se obtém |
|---------------------|---------------|---------------|
| `GenexusOpenApiBuilder.Extension.dll` | U14+ (canônico) | CI/build oficial: `GeneXus.Package.UI.Sdk` + NuGet `18.13.2` |
| `GenexusOpenApiBuilder.Extension-gx18u13.dll` | GX18 Upgrade 13 | Build **local** do satélite (refs em `Src/Lib/Gx18u13`, não versionadas no git público) |

Texto público: **“linha Gx18u13 — testada em GeneXus 18 Upgrade 13”**. GX17/U12 = não anunciados.

Na pasta `Packages` da IDE o arquivo instalado chama-se sempre `GenexusOpenApiBuilder.Extension.dll`. O sufixo `-gx18u13` é só do download/Release.

### 1.1 Política de Release (tag = rótulo GitHub, ex. `v0.1.0-alpha.2`)

| ID | Regra |
|----|--------|
| R1 | Release U14+ **nunca espera** validação U13 nem o contribuidor. |
| R2 | Quando os **dois** assets forem publicados, vão na **mesma tag**. |
| R3 | Sem evidência U13: Release só com DLL U14+ + texto “sem artefato Gx18u13 nesta versão”; `INSTALL.md` aponta a última tag Gx18u13 conhecida-boa, se existir. |
| R4 | **Proibido** anexar `-gx18u13` depois a uma tag já publicada. Atrasado → próxima tag. |
| R5 | Não existe Release **somente** Gx18u13. Hotfix do satélite → nova tag com **os dois** assets (U14+ rebuildado do mesmo código da tag). Essa tag nova dispara o workflow NuGet e publica uma versão canônica nova, mesmo se o binário U14+ for rebuild idêntico — **aceito** (D50). Não condicionar o workflow. |
| R6 | Asset Gx18u13 da tag N é buildado do **código da tag N** (checkout dessa tag / commit da tag). |

### 1.2 Política de proveniência e CI (1ª entrega)

Não haverá CI público que compile o satélite Gx18u13 (evita expor/precisar DLLs Artech no runner público e evita self-hosted na 1ª entrega).

- CI GitHub continua responsável **só** pela linha U14+ (e feed NuGet canônico).
- Build Gx18u13: **manual pelo mantenedor** (máquina com `Src/Lib/Gx18u13` local, gitignored).
- Antes de anexar ao Release: obrigatório `Test-ReleasedExtension.ps1` (checksum + carimbo `GxLine=Gx18u13` + manifesto + **nome de asset** `GenexusOpenApiBuilder.Extension-gx18u13.dll` + Version/InformationalVersion = D30).
- Contribuidor (Igor) valida na IDE U13. O asset publicado no Release é **sempre** o recompilado pelo mantenedor (D8/D27); o contribuidor **não** publica binário próprio no Release.

O workflow `.github/workflows/publish-github-packages.yml` baixa o padrão **exato** `GenexusOpenApiBuilder.Extension.dll`. Com dois assets na tag, esse pattern deve permanecer literal (não `*.dll`), para não empacotar o asset `-gx18u13` no NuGet da org (D42).

#### 1.2.1 Evidência reprodutível do build manual U13 (D31)

Para cada asset Gx18u13 anexado à tag N, o mantenedor registra (arquivo de evidência junto ao Release ou anexo interno auditável):

1. `git checkout` da tag N (ou do commit apontado pela tag) com **working tree limpa**.
2. SHA completo do commit (`git rev-parse HEAD`) — deve bater com a tag.
3. Versão Assembly/InformationalVersion do output (= D30).
4. SHA-256 de **cada** DLL listada em `Src/Lib/Gx18u13/` usada pelo satélite (lista explícita §5.1 — não glob opaco).
5. Versão do SDK/MSBuild usada no build (ex. `dotnet --info` / MSBuild version), sem versionar as DLLs privadas.
6. Só então: build → rename/copy para o nome de asset → `Test-ReleasedExtension` → anexa à tag.

`Test-ReleasedExtension` valida o **binário final**; D31 prova **de qual fonte/refs** ele veio. Os dois são obrigatórios na 1ª entrega.

Evolução futura (fora da 1ª entrega): runner self-hosted opcional.

---

## 2. Não-objetivos

- Não trocar `Sdk` do `.csproj` canônico para `Microsoft.NET.Sdk`.
- Não auto-detectar pasta U13 nos defaults dos scripts canônicos.
- Não anunciar suporte a GX17 / U12 neste plano.
- Não usar o nome `Legacy`.
- Não mergear o PR #2 como está.
- Não travar cadência U14+ por falta do asset Gx18u13.
- Não versionar DLLs proprietárias GeneXus/Artech no git público.
- Não usar `PackageReference` / NuGet no satélite na 1ª entrega.
- Não prometer detecção automática “esta pasta é U13 vs U14+” na 1ª entrega (Q7 = melhoria).
- Não enumerar cada `.cs` compartilhado em `Compile.Shared.props` (D36).
- Não abrir pastas `Line.*` antes do Exp-APIs exigir divergência (D39).
- Não usar HintPath absoluto de máquina no satélite (D34).
- Não colocar build do satélite no checker pré-push público (D45).
- Não forçar `EnableDefaultCompileItems=false` no canônico enquanto não existir `Line.*` (D46).
- Não condicionar `publish-github-packages.yml` para evitar inflação de versão em hotfix U13 (D50).

---

## 3. Invariantes

1. **Default = U14+.** `dotnet build Src\GenexusOpenApiBuilder.sln` produz só o canônico.
2. **Gx18u13 é opt-in.** Só `GenexusOpenApiBuilder.Gx18u13.sln` / scripts dedicados. Projeto satélite **fora** da solution canônica.
3. **Isolamento físico.** Output do satélite em `artifacts/gx18u13/` via `Directory.Build.props` **condicional** por `$(MSBuildProjectName)` (D47), importado **antes** do SDK — não só `BaseOutputPath` no corpo do csproj. Satélite **não** escreve `Src/Extension/packages.lock.json`. `Src/Lib/Gx18u13/` também entra no `.gitignore`.
4. **Satélite sem NuGet.** Zero `PackageReference`; só `<Reference>` com **HintPath relativo pinado** `..\Lib\Gx18u13\<NomeExato>.dll` (lista fechada — **proibido** `*.dll` / glob e **proibido** path absoluto de máquina). Sem resolução por GAC/outro path: arquivo ausente = falha de build. `RestorePackagesWithLockFile=false` no mesmo bloco condicional do props da raiz (D35/D47).
5. **Compile compartilhado + exclusões explícitas.**
   - `Src/Extension/Compile.Shared.props` declara **globs de pasta** compartilhados (D36): `..\Domain\**\*.cs` com `LinkBase="Domain"` (D53), `Diagnostics\**\*.cs`, e `*.cs` na raiz de `Extension` **exceto** `Line.*`. Código novo compartilhado vai nessas três árvores.
   - O **satélite** importa esse props desde a Fase 2, com `EnableDefaultCompileItems=false`.
   - O **canônico** permanece com o csproj atual (glob Domain + itens default do SDK) **até** existir `Line.*` (D46). Quando `Line.*` nascer: canônico também `EnableDefaultCompileItems=false`, importa o shared e inclui só `Line.Gx18u14plus`.
   - Fontes só U14+ ou só Gx18u13 em `Line.Gx18u14plus/` e `Line.Gx18u13/` — criadas **somente** se o Exp-APIs exigir (D39).
6. **Scripts canônicos:** defaults U14+ preservados; sem auto-detect U13.
7. **Bats Gx18u13:** `-GeneXusDirectory` obrigatório (erro imediato se ausente).
8. **ExpectedLine = bat**, não faixa da pasta. Carimbo `GxLine` deve igualar ExpectedLine **antes** do Copy.
9. **Carimbo bilateral.** `AssemblyMetadata("GxLine","Gx18u14plus")` e `("GxLine","Gx18u13")`. “Sem carimbo = erro” só a partir da versão que introduzir carimbos. No canônico, o carimbo entra via `AssemblyAttribute` no `.csproj` (D39), não via pasta Line prematura.
10. **Proibido `#if` em `Package.cs`.** Stubs = runtime ou tipos/`partial` em `Line.*`.
11. **PackageCompatibility:** canônico = SDK NuGet. Satélite = `AssemblyAttribute` manual com número do **Exp-Compat**.
12. **Gate U14+:** build canônico + checker + U15 quando canônico mudar; Exp-Build: satélite → canônico → `git status` limpo.
13. **Pré-push mecânico = só canônico.** `scripts/Invoke-PrePushMechanicalChecks.ps1` restaura e constrói **apenas** `Src\GenexusOpenApiBuilder.sln`. O satélite **não** entra nesse gate. Presença de `Src/Lib/Gx18u13` é campo informativo no JSON (`satelliteRefs: absent|present`), **fora** de `warnings[]` (D45). Build do satélite = checklist de Release/D31 na máquina do mantenedor.

---

## 4. Catálogo de decisões (autocontido)

| ID | Decisão |
|----|---------|
| D1 | Opção B: segundo artefato oficial no Release |
| D2 | PR #2 não mergeável sem redesign |
| D3 | Baseline primário = U14+ |
| D4 | Duas solutions: canônica + `GenexusOpenApiBuilder.Gx18u13.sln` |
| D5 | Projeto satélite; não trocar Sdk do canônico |
| D6 | Nomenclatura `Gx18u13` / `-gx18u13` |
| D7 | Manifesto canônico único + GUID atual; não omitir `AddCommand` |
| D8 | Build/refs no mantenedor; validação funcional U13 pelo Igor |
| D9 | Go/no-go = §7.1 |
| D10 | Anúncio só “testado em GX18 U13” |
| D11 | R1–R6 |
| D12 | Carimbar as duas linhas |
| D13 | Modos validação: contribuidor e artefato Release |
| D14 | Satélite embute `.package` com nome lógico igual ao canônico |
| D15 | Checker rejeita qualquer `#if` em `Package.cs` |
| D16 | Coexistência U13+U14: tolerada, não oficial |
| D17 | Isolamento físico em `artifacts/gx18u13` |
| D18 | `-GeneXusDirectory` obrigatório nos bats Gx18u13 |
| D19 | R4/R5 (sem anexar atrasado; sem Release só-Gx18u13) |
| D20 | **Substituído por D45.** Não emitir warning de pré-push por ausência de `Lib/Gx18u13` |
| D21 | `Test-ReleasedExtension` + PEReader; pin nome lógico `.package` |
| D22 | Versão base em `Version.Shared.props` (Fase 2: migrar Version/AssemblyVersion/FileVersion/InformationalVersion do csproj canônico) |
| D23 | Exp-Compat determina PackageCompatibility do U13 |
| D24 | Exp-APIs no go/no-go antes de Exp-Carga; `Line.*` se API divergir |
| D25 | Contratos §6 |
| D26 | Zero PackageReference no satélite |
| D27 | Proveniência 1ª entrega: build manual mantenedor + Test-Released + evidência D31; sem CI público satélite |
| D28 | Exp-Compat usa DLL-sonda mínima (não o produto) |
| D29 | Satélite: `Microsoft.NET.Sdk`, `net471`, contrato §5.1 |
| D30 | Tag `vX` ↔ Version/InformationalVersion do commit da tag = X; `Test-ReleasedExtension` confere isso (D49) |
| D31 | Evidência reprodutível U13: checkout limpo da tag, SHA commit, hashes das refs `Src/Lib/Gx18u13`, versão MSBuild/SDK |
| D32 | Asset Release: output do satélite renomeado/copiado para `GenexusOpenApiBuilder.Extension-gx18u13.dll`; `Test-ReleasedExtension` exige esse nome |
| D33 | Checklist de validação do contribuidor na IDE U13 (§6.5.1): carga, menus, Wizard smoke, Build All amostra quando aplicável, smoke do asset Release |
| D34 | HintPath do satélite é **relativo** e pinado (`..\Lib\Gx18u13\<NomeExato>.dll`); path absoluto de máquina é proibido |
| D35 | Satélite **não** consome `GeneXusPackageReferenceVersion` para adicionar `PackageReference`. Lockfile e paths do satélite: bloco condicional no `Directory.Build.props` da raiz (D47), não só override no corpo do csproj |
| D36 | `Compile.Shared.props` usa globs de pasta compartilhada, não enumeração arquivo a arquivo |
| D37 | Exp-Compat: sonda compilada com o **SDK canônico atual**; não exige `Src/Lib/Gx18u13` nem o satélite |
| D38 | Lista pinada de refs inclui, além de `Artech.*` realmente usadas, `Newtonsoft.Json`, `System.Drawing` e `System.Windows.Forms` quando o shared as referencia |
| D39 | Pastas `Line.*` só após Exp-APIs exigir divergência; carimbo canônico via `AssemblyAttribute` no `.csproj` |
| D40 | Exp-ParidadeEmpacote mede a DLL U14+ já instalável localmente (nome lógico do `.package`); não depende do Igor |
| D41 | Exp-Carga registra se o U13 obedece ao contrato Register sem Admin / `genexus /install` do U15; §5.0 não muda a priori |
| D42 | Workflow `publish-github-packages.yml` permanece com pattern literal `GenexusOpenApiBuilder.Extension.dll` |
| D43 | Ao **iniciar** a execução deste plano, atualizar `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md` (Fase 1 como frente ativa, ou Sprint 9 + Gx18u13 em paralelo explícito) |
| D44 | `Src/Lib/Gx18u13` é populado a partir da instalação U13 do contribuidor (cópia local gitignored), não a partir de Program Files do mantenedor |
| D45 | Pré-push mecânico só canônico. JSON: `satelliteRefs` informativo, **não** `warnings[]`. Build satélite = Release/D31. O meta-teste `Tests/PrePushChecker/Test-OpenApiBuilderPrePushChecks.ps1` exige `warnings.Count -eq 0` e proíbe o checker de invocar `Tools/`, Program Files ou DLL |
| D46 | `EnableDefaultCompileItems=false` no **canônico** só quando existir `Line.*`. O risco de embed do `.package` é `EnableDefaultItems` / `EnableDefaultEmbeddedResourceItems`, não o switch de Compile |
| D47 | Isolamento `obj`/`bin` do satélite: `Directory.Build.props` condicional `MSBuildProjectName == GenexusOpenApiBuilder.Extension.Gx18u13` (antes do SDK). Dois csproj na mesma pasta `Src/Extension/` |
| D48 | **Nomes** das DLLs pinadas: deriváveis localmente (usings + `packages.lock.json` / `project.assets.json`). **Arquivos** U13: só via Igor (D44). A lista de nomes não espera o contribuidor |
| D49 | `Test-ReleasedExtension` confere Version/InformationalVersion contra a tag (D30). `Version.Shared.props` é mudança canônica nomeada da Fase 2 |
| D50 | R5: inflação de versão no GitHub Packages em hotfix U13 é aceita; tag nova = versão NuGet nova |
| D51 | Contrato único de path da IDE: `-GeneXusDirectory`; `InstalledDll` = `Packages\GenexusOpenApiBuilder.Extension.dll` derivado. Não manter `-InstalledDll` como default paralelo |
| D52 | `Copy-ExtensionForGeneXus18.ps1`: elevação só se o destino não for gravável pelo usuário atual (típico Program Files). Path gravável tipo `C:\GeneXus\Gx18\U13` **não** exige Admin por política |
| D53 | `Compile.Shared.props` preserva `LinkBase="Domain"` no glob `..\Domain\**\*.cs` |
| D54 | Exp-Carga separa evidência: menu principal (autocontido) vs menu de contexto (depende de `KBObjectGrp` / GUID `98121D96-A7D8-468b-9310-B1F468F812AE` no `.package`) |

### 4.1 Gates abertos

| ID | Como fechar |
|----|-------------|
| Q1 | Exp-Carga |
| Q2 | Exp-Empacote se Q1 falhar |
| Q3 | Exp-Compat + Igor |
| Q4 | Exp-APIs |
| Q5 | Exp-Distribuição |
| Q7 | Melhoria: FileVersion do GeneXus.exe |

Q6 não é gate vigente (número retirado em iterações anteriores; não reutilizar).

---

## 5. Arquitetura MSBuild

```text
Src/
  GenexusOpenApiBuilder.sln
  GenexusOpenApiBuilder.Gx18u13.sln
  Extension/
    GenexusOpenApiBuilder.Extension.csproj
    GenexusOpenApiBuilder.Extension.Gx18u13.csproj
    GenexusOpenApiBuilder.package
    Compile.Shared.props
    Version.Shared.props
    Lib.Gx18u13.References.props   ← lista explícita de HintPath (sem glob)
    Line.Gx18u14plus/              ← só se Exp-APIs exigir
    Line.Gx18u13/                  ← só se Exp-APIs exigir
  Lib/Gx18u13/                     ← DLLs privadas; gitignored

Directory.Build.props              ← bloco condicional do satélite (D47)
artifacts/gx18u13/                 ← obj/bin; gitignored
Tools/
Install-ExtensionForGeneXus18.bat
Install-ExtensionForGx18u13.bat
Register-ExtensionForGx18u13.bat
```

`.gitignore` (obrigatório na Fase 2): `Src/Lib/Gx18u13/` e `artifacts/gx18u13/`. `bin/` e `obj/` já são globais; a regra de `artifacts/` é necessária porque esses diretórios **não** se chamam `bin`/`obj`. Validar as duas entradas novas com `git check-ignore -v` (o bloco `!Src/` no `.gitignore` atual não deve reincluir `Lib/Gx18u13`).

O `Directory.Build.props` da raiz hoje define `RestorePackagesWithLockFile=true` e `GeneXusPackageReferenceVersion=18.13.2` para **todos** os projetos. Os dois `.csproj` compartilham `Src/Extension/` (e o `packages.lock.json` canônico). O bloco condicional D47 anula lockfile e redireciona output **só** do satélite, sem o satélite usar `GeneXusPackageReferenceVersion` para `PackageReference`.

### 5.0 Bats Gx18u13 — Install vs Register

Espelha o contrato já vigente em `AGENTS.md` para U14+; os bats Gx18u13 **não** inventam outro fluxo.

- `Install-ExtensionForGx18u13.bat` — caminho **primário**: fecha a IDE → copia/valida DLL com `-GeneXusDirectory` e `-ExpectedLine Gx18u13` → reabre e testa.
- `Register-ExtensionForGx18u13.bat` — **condicional**: só quando, desde o último `genexus /install` bem-sucedido, mudou o `.package`, a identidade do pacote ou o registro da extensão; aí: Register → no prompt, `genexus /install` → `exit`.
- Atualização só de DLL (mesmo manifesto): **não** pedir `/install`.
- O agente não executa esses bats nem altera `C:\Program Files (x86)\GeneXus`; só orienta.
- `-GeneXusDirectory` é obrigatório: o U13 do contribuidor **não** está no default `C:\Program Files (x86)\GeneXus\GeneXus18` (evidência PR #2).
- O PR #2 registra com Administrador; este plano **não** adota isso. O Exp-Carga mede o comportamento real do U13 (D41) sem mudar este contrato até haver evidência.
- Cópia (D52): Admin só se `Packages\` não for gravável; não exigir Admin só porque o bat é “de instalação”.

### 5.1 Esqueleto normativo do satélite

- `Sdk="Microsoft.NET.Sdk"`, `TargetFramework=net471`
- `EnableDefaultCompileItems=false` (satélite, desde a Fase 2)
- Paths e `RestorePackagesWithLockFile=false`: `Directory.Build.props` condicional (D47); o csproj pode repetir o lockfile como cinto, mas **não** é o lugar do `BaseIntermediateOutputPath`
- **Não** definir/usar `GeneXusPackageReferenceVersion` para adicionar `PackageReference`
- `LangVersion` / `Nullable` alinhados ao canônico
- Import `Version.Shared.props` + `Compile.Shared.props` + `Lib.Gx18u13.References.props`
- Compile adicional: `Line.Gx18u13/**/*.cs` (pode ser vazio)
- `AssemblyMetadata GxLine=Gx18u13`
- `PackageCompatibility` = número do Exp-Compat
- `EmbeddedResource` do `.package` com LogicalName pinado (medido no Exp-ParidadeEmpacote)
- Referências: **somente** as entradas de `Lib.Gx18u13.References.props`, cada uma com `HintPath` = `..\Lib\Gx18u13\<NomeExato>.dll`, `Private=false`. **Proibido** `Include="*.dll"`. Nomes da lista: D48 (local). Arquivos: D44. Go/no-go exige lista pinada **e** arquivos presentes na máquina que builda o asset.
- Pós-build Release: copiar/renomear o output `GenexusOpenApiBuilder.Extension.dll` (nome de assembly instalável) para o **asset** `GenexusOpenApiBuilder.Extension-gx18u13.dll` antes de `Test-ReleasedExtension` / anexar à tag.

Canônico: Sdk GeneXus; **não** muda `EnableDefaultCompileItems` até D46 disparar; carimbo `Gx18u14plus` via `AssemblyAttribute` no Exp-Carimbo. Se no futuro o canônico desligar itens default, o risco de embed é `EnableDefaultEmbeddedResourceItems` — aí declara o mesmo `EmbeddedResource` pinado sem trocar o Sdk.

### 5.1.1 Como popular `Src/Lib/Gx18u13` (D44 + D48)

1. **No mantenedor, sem U13:** redigir `Lib.Gx18u13.References.props` com os **nomes** derivados dos `using Artech.*` / `Newtonsoft.Json` e do fechamento em `Src/Extension/packages.lock.json` (o csproj canônico só declara `System.Drawing` e `System.Windows.Forms`; o restante Artech vem do SDK).
2. **Na máquina U13:** copiar **somente** esses arquivos da pasta de instalação (ex. `C:\GeneXus\Gx18\U13`) para `Src/Lib/Gx18u13/` local (gitignored).
3. Conferir nomes byte a byte com o props.
4. D31 registra SHA-256 de cada arquivo usado no build da tag.
5. Não copiar de `C:\Program Files (x86)\GeneXus` do U15.

### 5.2 Carimbo

- Tipo: `AssemblyMetadataAttribute`
- Chave: `GxLine`
- Valores: `Gx18u14plus` | `Gx18u13` (um só)
- Canônico: item MSBuild `AssemblyAttribute` no `.csproj` (ou props compartilhado de carimbo), não um `.cs` em `Line.*` só para isso.
- **Exp-Carimbo (§7)** valida **apenas o canônico** (`Gx18u14plus`) e implica reinstalação U15 da DLL canônica (“sem carimbo = erro” só a partir dessa versão).
- Carimbo `Gx18u13` é validado no gate de build/teste do **satélite** (Fase 2/3): PEReader pós-build + `Test-ReleasedExtension -ExpectedLine Gx18u13` + pré-Copy nos bats Gx18u13. “Paridade + carimbo OK” no go/no-go (§7.1) = canônico carimbado **e** satélite/protótipo com carimbo bilateral comprovado nesse gate.

### 5.3 Nome lógico `.package`

Medido no Exp-ParidadeEmpacote a partir da DLL canônica atual (`Tools/Test-InstalledExtension.ps1` já expõe `ManifestResource`); pinado no checklist de implementação (não inventar sem medir).

---

## 6. Contratos dos scripts

### 6.1 `Copy-ExtensionForGeneXus18.ps1`

| Parâmetro | Regra |
|-----------|--------|
| `-GeneXusDirectory` | Canônico: default atual. Bat Gx18u13 sempre passa path. |
| `-BuildDll` | Origem; Gx18u13 → artifacts ou asset baixado. Default canônico permanece `Src\Extension\bin\Release\net471\...`. |
| `-ExpectedLine` | Gx18u13 obrigatório `Gx18u13`; canônico default `Gx18u14plus`. |
| Ordem | PEReader `GxLine` **antes** do Copy. |
| Destino | `Packages\GenexusOpenApiBuilder.Extension.dll` |
| Elevação | D52: só se o destino não for gravável; o script atual exige Admin incondicionalmente — mudar na Fase 3 |

### 6.2 `Test-InstalledExtension.ps1`

| Parâmetro / regra | Detalhe |
|-------------------|---------|
| `-GeneXusDirectory` | Contrato único (D51). Obrigatório nos fluxos Gx18u13. Default canônico = path U18 atual. |
| `InstalledDll` | Sempre derivado: `<GeneXusDirectory>\Packages\GenexusOpenApiBuilder.Extension.dll`. O parâmetro `-InstalledDll` de hoje **sai** (não coexistir dois defaults). |
| `-BuildDll` | DLL de origem do build/asset; hash comparado com a instalada em `Packages\`. |
| `-ExpectedLine` | Obrigatório nos fluxos novos: `Gx18u14plus` ou `Gx18u13`. |
| Inspeção | PEReader para metadados/carimbo/manifesto (**não** `LoadFile` para isso). O script atual ainda usa `LoadFile` para o recurso `.package`; a troca é da **Fase 3**, não da Fase 1. |
| Destino conferido | `Packages\GenexusOpenApiBuilder.Extension.dll` (nome instalado, sem sufixo `-gx18u13`). |

### 6.3 `Test-ReleasedExtension.ps1` (novo)

| Parâmetro / regra | Detalhe |
|-------------------|---------|
| `-DllPath` (ou equivalente) | Caminho do asset sob teste. |
| `-ExpectedLine` | `Gx18u14plus` (asset canônico) ou `Gx18u13` (asset `-gx18u13`). |
| `-ExpectedFileName` | Nome de arquivo obrigatório: `GenexusOpenApiBuilder.Extension.dll` **ou** `GenexusOpenApiBuilder.Extension-gx18u13.dll` (D32). |
| `-ExpectedVersion` | InformationalVersion/Version iguais ao rótulo da tag sem o prefixo `v` (D30/D49). |
| Checks | SHA-256 + PEReader + carimbo + nome lógico `.package` + tipo Package/AbstractPackageUI + versão. |
| Runtime | PS 7.4+. |

### 6.4 Checksums

| Item | Regra |
|------|--------|
| Formato | `sha256sum`: `<hash>  <filename>` |
| Publicação | Arquivo de checksums na Release (assets oficiais). |
| Evidência D31 | Pode incluir hashes das refs privadas `Src/Lib/Gx18u13` + versão MSBuild/SDK **sem** publicar as DLLs. |

### 6.5 Fluxos

| Fluxo | Passos |
|-------|--------|
| Contribuidor (Igor) — §6.5.1 | Build local → `Install-ExtensionForGx18u13.bat` com `-GeneXusDirectory` → `Test-InstalledExtension` → checklist IDE §6.5.1. |
| Release U13 (mantenedor) | Checkout tag N limpo → registrar D31 → build satélite (`GenexusOpenApiBuilder.Gx18u13.sln`, **não** o checker pré-push) → renomear/copiar para `…-gx18u13.dll` → `Test-ReleasedExtension` → anexa assets (+ checksums). |
| Release U14+ | CI/build oficial → `Test-ReleasedExtension` no asset canônico (R1 independente). |

#### 6.5.1 Checklist de validação do contribuidor (Igor) na IDE U13 (D33 + D54)

Além de Install + `Test-InstalledExtension`, registrar evidência de:

1. Extensão carrega (sem erro de PackageCompatibility / “expecting version”).
2. **Menu principal** visível (antes de Help), com os comandos operacionais vigentes.
3. **Menu de contexto** da Transaction: submenu `Genexus Open API Builder` (não itens soltos). Se falhar só o contexto, anotar GUID/grupo — não misturar com falha de carga.
4. Abrir **Wizard** (smoke: abre sem crash; não exige fechar toda a Fase 4).
5. Quando o artefato já for satélite/protótipo **de produto** (não a sonda Exp-Compat): **Build All** de uma Transaction de amostra e anotar OK/falha.
6. Após o mantenedor publicar o asset da tag: smoke de **reinstalação do asset Release** (não só do build local do contribuidor).

Itens 1–3 são mínimos para “carga OK”; 4–5 alinham ao go/no-go §7.1; 6 fecha o laço D13 (modo artefato Release).

---

## 7. Fase 1 — Experimentos (ordem)

Sondas da Fase 1 ≠ satélite definitivo da Fase 2 (pastas `spikes/` ou `Temp/`, descartáveis). **Não** começar pelo satélite nem pelos bats Gx18u13.

| # | ID | Conteúdo | Saída | Dependência |
|---|-----|----------|--------|-------------|
| 1 | Exp-ParidadeEmpacote | Medir `ManifestResource` da DLL canônica local | Nome lógico pinado | Só máquina do mantenedor (D40) |
| 1b | Exp-RefsNomes | Lista de **nomes** de DLL a partir de usings + lockfile | Rascunho de `Lib.Gx18u13.References.props` | Local (D48); paralelo a 1 |
| 2 | Exp-Compat | DLL-sonda mínima **com SDK canônico**; Igor lê `expecting version` na IDE U13 | Número N | Igor + sonda; **sem** `Lib/Gx18u13` (D37) |
| 3 | Exp-APIs | Copiar **arquivos** U13 (D44); compilar contra eles; decidir Line.* | Shared vs Line.*; lista pinada fechada | 1b + arquivos U13 |
| 4 | Exp-Carimbo | `GxLine=Gx18u14plus` no canônico via `AssemblyAttribute` | OK/fallback; reinstall U15 | Depois de 1; muda a DLL canônica |
| 5 | Exp-Carga | Menu principal **e** contexto (D54); Register/`/install` (D41) | Carga OK | N + lista pinada + Igor |
| 6 | Exp-Empacote | Se carga falhar | | Q2 |
| 7 | Exp-Build | Canônico limpo + git status após satélite; carimbo `Gx18u13` no output/asset | | Fase 2 esqueleto |
| 8 | Exp-Distribuição | Q5 | | |

Itens 1, 1b e 2 podem correr em paralelo (1/1b locais; 2 com o Igor).

**O que autoriza a Fase 2 (esqueleto MSBuild):** Exp-Compat → N **e** lista de nomes (1b). Não espera Wizard+Build All.

**O que fecha o go/no-go §7.1 / §12:** a tabela §7.1 completa. O item 5 do go/no-go exige Wizard+Build All no **satélite ou protótipo de produto**, não na sonda Exp-Compat. Evidência só de sonda deixa o item 5 aberto.

### 7.0 Receita normativa do Exp-Compat (D28 + D37)

1. Spike descartável: `Package : AbstractPackageUI`, manifesto mínimo, `GenerateAssemblyInfo` do SDK canônico (Compatibility 143920).
2. **Não** é o produto; **não** usa `Src/Lib/Gx18u13`.
3. Entregar a DLL da sonda ao Igor.
4. No U13: Add > Local ou cópia em `Packages` + `genexus /install` (registrar o caminho usado).
5. Capturar o texto `Compatibility: cannot load package ... version '143920', expecting version 'N'` (ou equivalente).
6. N vira `PackageCompatibility` do satélite (D23). Se a sonda **carregar** no U13 com 143920, registrar isso: o N pode ser 143920 e o Exp-Compat fecha com evidência positiva, não só com recusa.
7. Corroborar N com o nome de `packages.*.xml` na máquina U13 (§11 pergunta 1).

### 7.1 Go/no-go

1. Exp-Compat → N
2. Exp-APIs fechado (**lista pinada** em `Lib.Gx18u13.References.props` + arquivos na máquina de build)
3. Paridade + carimbo OK (= Exp-Carimbo canônico **e** carimbo satélite no gate §5.2)
4. Exp-Carga: menu principal e contexto (D54)
5. Wizard + Build All no artefato de produto (satélite ou protótipo de produto; **não** a sonda)
6. U14+ intacto

Registrar por experimento: entrada, comando, esperado, observado, artefato.

---

## 8. Fases 2–6

**Fase 2:** satélite §5 (D47/D46); Compatibility=N; `Version.Shared.props` (D22/D49); scripts §6 (parâmetros; PEReader completo e D52 ficam na Fase 3 se o canônico ainda usar `LoadFile` / Admin incondicional); checker `#if`; D45 (`satelliteRefs` se o checker passar a emitir o campo — atualizar o meta-teste **sem** colocar isso em `warnings[]`); `.gitignore` Lib/artifacts com `git check-ignore -v`.
**Fase 3:** bats (§5.0 Install vs Register); D51/D52; pré-Copy; Test-Released (D32 + D49); inspeção PEReader sem `LoadFile`; `/install` só se manifesto/registro mudou.
**Fase 4:** paridade funcional U13.
**Fase 5:** Release R1–R6 + D27/D31/D50; docs (`INSTALL.md`, checkpoint); NuGet só U14+ (D42).
**Fase 6:** PR #2 alinhado; crédito Igor.

---

## 9. PR #2

Reaproveitar ideia bats/HintPath. Rejeitar: Sdk raiz, GUID novo no comum, auto-detect, `#if` no Package.cs, enfraquecer testes, Register elevado como contrato, segundo arquivo `.package` (`Package.package`), `TargetGX` no csproj canônico.

Path observado no PR (não vira default de script): `C:\GeneXus\Gx18\U13`.

---

## 10. Riscos

| Risco | Mitigação |
|-------|-----------|
| Compatibility errado | Exp-Compat (sonda D37) |
| API só U14+ | Exp-APIs + Line.* tardio (D39) |
| Contaminação obj/lock | D47 (props antes do SDK) + D35 |
| Path errado | ExpectedLine + pré-Copy; `-GeneXusDirectory` obrigatório (D51) |
| Binário de terceiro | D27 + D31 |
| Asset atrasado | R4 |
| Line.Gx18u13 no canônico | D46: canônico só desliga default compile quando Line.* existir |
| Ref resolvida fora de Lib/ | HintPath relativo pinado (D34); arquivo ausente = falha |
| Asset com nome errado | D32 + Test-ReleasedExtension |
| NuGet org com DLL U13 | D42 pattern literal |
| Fase 1 sem U13 no mantenedor | D37 + D44 + D48; canal Igor explícito |
| Embed `.package` no canônico | não mexer em `EnableDefaultEmbeddedResourceItems`; D46 |
| Checkpoint Sprint 9 vs esta frente | D43 antes de código de produto |
| Contrato Register U13 ≠ U15 | D41; não copiar o Admin do PR #2 sem evidência |
| Warning de Lib quebra o meta-teste pré-push | D45 |
| Inflação NuGet em hotfix U13 | D50 (aceita) |
| Admin inútil em path gravável | D52 |

---

## 11. Perguntas ao Igor

1. Qual arquivo `packages.*.xml` existe na máquina U13 (tipicamente sob `C:\ProgramData\GeneXus\...`)? O número no nome (ex. `packages.143920.xml` no U15) deve coincidir com o N do Exp-Compat.
2. Texto `expecting version` com a sonda do Exp-Compat (item 2 da Fase 1).
3. `Artech.*` + FileVersion (Q7, não bloqueia 1ª entrega).
4. Smoke após artefato do mantenedor (item 6 do §6.5.1).
5. Confirmar execução do checklist §6.5.1 nos builds de contribuição.
6. Confirmar path da instalação U13 e disponibilidade para copiar as DLLs da lista pinada (D44).
7. No Exp-Carga: `genexus /install` com e sem elevação — o que varre `Packages` no U13 (D41).
8. Menu de contexto: o submenu aparece unificado, ou os comandos caem na raiz do menu do objeto (D54)?

---

## 12. Critério “chegamos em B”

R1–R6; D27/D31/D32/D33; D34–D54 vigentes no código/docs; INSTALL; solution canônica limpa; go/no-go §7.1; §6 implementado; evidência U13; U15 pós-carimbo; docs; checker `#if`; `.package` pinado; checksums na tag dual; workflow NuGet sem o asset `-gx18u13`; pré-push sem warning por `Lib/` ausente.

---

## 13. Referências

PR #2; B000; B010; wikis PackageCompatibility; `Directory.Build.props` e `Src/Extension/packages.lock.json` atuais; `Tools/Test-InstalledExtension.ps1` / `Copy-ExtensionForGeneXus18.ps1`; `Tests/PrePushChecker/Test-OpenApiBuilderPrePushChecks.ps1`; `.github/workflows/publish-github-packages.yml`; revisões Opus + Codex CLI + Nemotron + GLM-5.2 → v6–v10 (Minimax pulado); v11 = ajustes pré-execução; v12 = segunda opinião Claude Code `claude-opus-5` (não painel completo).

---

## 14. Histórico

| Data | Evento |
|------|--------|
| 2026-08-12 | v1–v5 — iterações (v5 ainda referia D1–D22 externos) |
| 2026-08-12 | v6 — autocontido; D1–D30; sonda; MSBuild normativo; D27 proveniência; Line.*; scripts completos |
| 2026-08-12 | v7 — mantenedor confirma G2/G3/G4; contrato MSBuild §5 vigente; consulta Codex CLI (`gpt-5.6-terra`) |
| 2026-08-12 | v8 — absorve ressalvas Codex: D31 evidência reprodutível; D32 nome de asset; refs pinadas; carimbo bilateral no go/no-go; `.gitignore` Lib/artifacts |
| 2026-08-12 | v9 — absorve ressalvas Nemotron: tabelas de parâmetros §6.2–6.5; D33 checklist contribuidor; Minimax pulado nesta sessão |
| 2026-08-12 | **v10** — pós-GLM seletivo: (1) redação §1.2 publicação só mantenedor; (2) §5.0 Install vs Register Gx18u13 (= `AGENTS.md`). Descartados do GLM: G5b–G5e e O5b–O5d (já cobertos, pedantismo ou escopo precoce) |
| 2026-08-12 | **v11** — pré-execução contra o repositório real: D34–D44 |
| 2026-08-12 | **v12** — absorve Opus 5: D20→D45 (pré-push só canônico; `satelliteRefs` fora de `warnings[]`); D46 Line.* tardio no canônico; D47 isolamento via `Directory.Build.props` condicional; D48 nomes locais vs arquivos U13; D49 versão no Test-Released + Version.Shared.props na Fase 2; D50 inflação NuGet aceita; D51 `-GeneXusDirectory` único; D52 Admin só se destino não gravável; D53 `LinkBase=Domain`; D54 menu principal vs contexto. Fase 2 esqueleto após N + lista de nomes; go/no-go item 5 exige artefato de produto |

---

## 15. Primeira fatia de execução (após aprovação humana desta v12)

Ainda **não** é código de produto:

1. Congelar esta v12 (commit só se o mantenedor pedir).
2. D43: atualizar o checkpoint se a frente for iniciada.
3. Exp-ParidadeEmpacote local + Exp-RefsNomes (1b).
4. Spike Exp-Compat + envio ao Igor.

Satélite, bats Gx18u13, alteração de `Copy-ExtensionForGeneXus18.ps1` e carimbo no canônico ficam **depois** dos itens 3–4 e da aprovação da fatia correspondente.
