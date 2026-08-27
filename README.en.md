[Português-BR](README.md) · [Español](README.es.md) · [English](README.en.md)

# Genexus Open API Builder

Open source tool to accelerate REST API generation from **GeneXus Transactions**.

Public alpha: **[`0.1.0-alpha.4`](https://github.com/GxBrasilNOficial/Genexus-Open-API-Builder/releases/tag/v0.1.0-alpha.4)** — choose the DLL matching your GeneXus version in the Release.

Less repetition. More delivery. More value for the GeneXus community.

---

## What it solves

It reduces the time needed to assemble the initial structure of a REST API in the GeneXus ecosystem: instead of manually creating API Objects, Procedures, SDTs, and metadata, the wizard generates a predictable, editable, and traceable foundation.

## Who it is for

- GeneXus software houses
- Internal corporate teams
- Independent consultants
- Technical community
- Students

## What it generates

From a Transaction:

- Main API Object (`List`, `Get`, `Create`, `Update`)
- Supporting Procedures
- Custom SDTs (Create, Update, Response, filters, list)
- Shared error and pagination SDTs
- Consistent naming
- Persistent metadata for conservative regeneration
- IDE lifecycle: Wizard, Sync with the Transaction, Remove generated API

### HTTP error contract (since `0.1.0-alpha.4`)

When the Business Component rejects a rule, `Create` and `Update` respond with **HTTP 422**, `ErrorResponse.Code = validation_error`, the rule text in `Message`, and the `Messages[]` collection (error messages only). Each API's Source changes only when you reopen the Wizard on it; the shared `sdt_API_ErrorResponse` SDT is unique per KB, so regenerating any API updates the error schema published by all of them. Callers that compared the fixed string `"Business rules rejected the request."` should switch to `Code`. Details and the opt-out: [0.1.0-alpha.4 notes](Docs/Releases/0.1.0-alpha.4.en.md).

## Current status

| Item | Status |
|------|--------|
| Functional MVP wizard | Completed (GeneXus 18 U15) |
| Lifecycle (ownership, sync, removal, report) | Completed |
| Public alpha `0.1.0-alpha.4` | Release package with U14+ and U13 assets |
| Upgrade 13 | Satellite DLL `GenexusOpenApiBuilder.Extension-gx18u13.dll` validated on U13 |
| Upgrade 14 | Confirmed by an external user (Alpha `0.1.0-alpha.1`; loading + generation) |
| Upgrade 15 | Development baseline; use confirmed by an external user through the maintainer path (local build + `Install-ExtensionForGeneXus18.bat`) |

### Which DLL to download

Release `0.1.0-alpha.4` contains two DLLs. Install only the one corresponding to your installation:

| File in the GitHub Release | Intended for | Note |
|---|---|---|
| `GenexusOpenApiBuilder.Extension.dll` | GeneXus 18 Upgrade 14, Upgrade 15, and later U14+ versions | Canonical line; do not use on U13 |
| `GenexusOpenApiBuilder.Extension-gx18u13.dll` | GeneXus 18 Upgrade 13 | U13 satellite line; do not use on U14+ |

The `-gx18u13` suffix only identifies the download asset. Do not rename the files to switch lines or install both DLLs in the same IDE.

### Known limitations

- Transactions with sublevels: the Wizard generates header + selected lines; metadata, Sync, and Remove are still V1 — do not Sync/Remove a hierarchical API until the V2 metadata front
- List counters only on direct children; depth above 4 warns without blocking
- No `DELETE` service in the MVP
- Native GeneXus OpenAPI YAML has documented restrictions; the extension does not replace the installation templates
- Sensitive-field/audit classification still uses the default policy
- Create/Update required-field validation checks **filled-in values** (not pure JSON presence), with the known limitation for values equal to the type default

## Get started in minutes

1. [Install the extension](Docs/Public/INSTALL.md)
2. [Follow the quick demo](Docs/Public/DEMO.md)
3. Read the [Alpha notes](Docs/Releases/0.1.0-alpha.4.en.md)

## Screenshots

Quick overview. Complete Wizard gallery (all tabs): [Docs/Public/DEMO.md](Docs/Public/DEMO.md).

![Genexus Open API Builder menu](Docs/Images/alpha-menu.png)

![Wizard preferences](Docs/Images/alpha-preferences.png)

![Context menu](Docs/Images/alpha-context-menu.png)

![Wizard — Summary](Docs/Images/alpha-wizard-resumo.png)

![Generated folder](Docs/Images/alpha-folder.png)

![Sync with the Transaction](Docs/Images/alpha-sync.png)

![Remove generated API](Docs/Images/alpha-remover.png)

![Final report](Docs/Images/alpha-relatorio-final.png)

## Environment requirement: PUT, DELETE, and PATCH on IIS

This applies to users publishing the generated API on **IIS** with the **.NET Framework** generator.

The `Update` service is generated as `PUT`. By default, IIS does not deliver that verb to the application: the `ExtensionlessUrlHandler-Integrated-4.0` handler comes with `verb="GET,HEAD,POST,DEBUG"`. The client receives an **IIS HTML 404**; `List`, `Get`, and `Create` may work normally.

Durable fix: in **IIS Manager as administrator**, server **node** → Handler Mappings → `ExtensionlessUrlHandler-Integrated-4.0` → Request Restrictions → Verbs → add `PUT` (and `DELETE`/`PATCH` if needed) → restart IIS.

Do not add the handler only to the generated app's `web.config`: Build All regenerates that section. Be careful with WebDAV enabled on the server.

The **.NET** generator does not exhibit this behavior. Complete diagnosis: [B071-B073/B079](Docs/Implementation/B071-B073-B079-GET-CREATE-UPDATE-HTTP.md).

## Updating the extension

When a new DLL is available:

**End user** (installed only with the Release DLL):

Updating only with **Add > Local** is **not proven**. In B094, with the DLL already present in `Packages`, Add > Local failed with `Error installing extension`; a clean reinstall required deleting that DLL (Program Files; typically with elevation) and repeating the installation flow. Details and observations: [Docs/Public/INSTALL.md](Docs/Public/INSTALL.md#atualização-usuário-final).

**Developer / maintainer** (cloned repository) — proven path:

1. Close the GeneXus IDE
2. Run [`Install-ExtensionForGeneXus18.bat`](Install-ExtensionForGeneXus18.bat) as administrator; if the IDE is in another directory, pass it as the first argument
3. If the manifest/registration changed since the last `genexus /install`, run [`Register-ExtensionForGeneXus18.bat`](Register-ExtensionForGeneXus18.bat) and run `genexus /install`
4. Reopen the IDE

Details: [Docs/Public/INSTALL.md](Docs/Public/INSTALL.md).

## Short roadmap

| Stage | Focus |
|-------|------|
| Alpha (now) | First usable open version |
| Sprint 9 | Real fixes based on external feedback |
| Sprint 10 / Beta | Stable main flow and predictable releases |

## Documentation

| Document | Content |
|-----------|----------|
| [INSTALL](Docs/Public/INSTALL.md) | Installation |
| [DEMO](Docs/Public/DEMO.md) | Short walkthrough |
| [CHANGELOG](CHANGELOG.md) | Change history |
| [0.1.0-alpha.4](Docs/Releases/0.1.0-alpha.4.en.md) | EN notes; [PT-BR](Docs/Releases/0.1.0-alpha.4.md); [ES](Docs/Releases/0.1.0-alpha.4.es.md) — DLL selection |
| [MVP decisions](Docs/Decisions/2026-07-14-REGISTRO_DECISOES_FUNCIONAIS_MVP.md) | Functional primary source |
| [Foundation](Docs/Foundation/00-MASTER_INDEX_DO_PROJETO.md) | Contracts and planning |
| [Operational checkpoint](Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md) | Internal project status |

## How to contribute

Bugs, improvements, documentation, tests, and real-world usage feedback are welcome.

Read [CONTRIBUTING.md](CONTRIBUTING.md). License: [MIT](LICENSE).

## Repository structure

- `Docs` — public documentation, foundation, and evidence
- `Src` — extension and domain
- `Tests` — local tests
- `Tools` — installation and checkers
- `Samples` — space for future examples
