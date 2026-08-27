[Português-BR](README.md) · [Español](README.es.md) · [English](README.en.md)

# Genexus Open API Builder

Herramienta open source para acelerar la generación de APIs REST a partir de **Transactions GeneXus**.

Alpha pública: **[`0.1.0-alpha.4`](https://github.com/GxBrasilNOficial/Genexus-Open-API-Builder/releases/tag/v0.1.0-alpha.4)** — elija la DLL correspondiente a su versión de GeneXus en el Release.

Menos repetición. Más entrega. Más valor para la comunidad GeneXus.

---

## Qué resuelve

Reduce el tiempo necesario para montar la estructura inicial de una API REST en el ecosistema GeneXus: en vez de crear manualmente API Objects, Procedures, SDTs y metadata, el wizard genera una base predecible, editable y rastreable.

## Para quién

- Software houses GeneXus
- Equipos corporativos internos
- Consultores independientes
- Comunidad técnica
- Estudiantes

## Qué genera

A partir de una Transaction:

- API Object principal (`List`, `Get`, `Create`, `Update`)
- Procedures de apoyo
- SDTs propios (Create, Update, Response, filtros, lista)
- SDTs compartidos de error y paginación
- Naming consistente
- Metadata persistente para una regeneración conservadora
- Ciclo de vida en la IDE: Wizard, Sincronizar con la Transaction, Remover API generada

### Contrato de error HTTP (desde `0.1.0-alpha.4`)

Si el Business Component rechaza una regla, `Create` y `Update` responden **HTTP 422** con `ErrorResponse.Code = validation_error`, el texto de las rules en `Message` y la colección `Messages[]` (solo mensajes de error). El Source de cada API solo cambia al reabrir el Wizard sobre ella; el SDT compartido `sdt_API_ErrorResponse` es único en la KB, así que regenerar cualquier API actualiza el schema de error publicado por todas. Quien comparaba la cadena fija `"Business rules rejected the request."` debe pasar a decidir por el `Code`. Detalle y opción de desactivar: [notas 0.1.0-alpha.4](Docs/Releases/0.1.0-alpha.4.es.md).

## Estado actual

| Elemento | Estado |
|------|--------|
| Wizard funcional del MVP | Completado (GeneXus 18 U15) |
| Ciclo de vida (propiedad, sincronización, eliminación, informe) | Completado |
| Alpha pública `0.1.0-alpha.4` | Paquete de esta release, con assets U14+ y U13 |
| Upgrade 13 | DLL satélite `GenexusOpenApiBuilder.Extension-gx18u13.dll` validada en U13 |
| Upgrade 14 | Confirmado por un usuario externo (Alpha `0.1.0-alpha.1`; carga + generación) |
| Upgrade 15 | Base del desarrollo; uso confirmado por un usuario externo mediante el camino del mantenedor (build local + `Install-ExtensionForGeneXus18.bat`) |

### Qué DLL descargar

El Release `0.1.0-alpha.4` contiene dos DLLs. Instale solamente la correspondiente a su instalación:

| Archivo en el GitHub Release | Sirve para | Observación |
|---|---|---|
| `GenexusOpenApiBuilder.Extension.dll` | GeneXus 18 Upgrade 14, Upgrade 15 y posteriores U14+ | Línea canónica; no usar en U13 |
| `GenexusOpenApiBuilder.Extension-gx18u13.dll` | GeneXus 18 Upgrade 13 | Línea satélite U13; no usar en U14+ |

El sufijo `-gx18u13` identifica solamente el asset de descarga. No cambie el nombre de los archivos para cambiar de línea ni instale las dos DLLs en la misma IDE.

### Limitaciones conocidas

- Transactions con subniveles: el Wizard genera encabezado + líneas seleccionadas; metadata, Sync y Eliminar siguen en V1 — no use Sync/Eliminar en una API jerárquica hasta la frente de metadata V2
- Contadores de List solo en hijos directos; profundidad mayor que 4 avisa sin bloquear
- Sin servicio `DELETE` en el MVP
- El YAML OpenAPI nativo de GeneXus tiene restricciones (documentadas); la extensión no reemplaza los templates de la instalación
- La clasificación de campos sensibles/auditoría todavía utiliza la política predeterminada
- La obligatoriedad en Create/Update valida el **llenado** (no la presencia JSON pura), con la limitación conocida de valores iguales al valor predeterminado del tipo

## Comenzar en minutos

1. [Instalar la extensión](Docs/Public/INSTALL.md)
2. [Seguir la demo rápida](Docs/Public/DEMO.md)
3. Leer las [notas de la Alpha](Docs/Releases/0.1.0-alpha.4.es.md)

## Capturas

Vista rápida. Galería completa del Wizard (todas las pestañas): [Docs/Public/DEMO.md](Docs/Public/DEMO.md).

![Menú Genexus Open API Builder](Docs/Images/alpha-menu.png)

![Preferencias del Wizard](Docs/Images/alpha-preferences.png)

![Menú de contexto](Docs/Images/alpha-context-menu.png)

![Wizard — Resumen](Docs/Images/alpha-wizard-resumo.png)

![Carpeta generada](Docs/Images/alpha-folder.png)

![Sincronizar con la Transaction](Docs/Images/alpha-sync.png)

![Remover API generada](Docs/Images/alpha-remover.png)

![Informe final](Docs/Images/alpha-relatorio-final.png)

## Requisito de entorno: PUT, DELETE y PATCH en IIS

Se aplica a quienes publican la API generada en **IIS**, con el generador **.NET Framework**.

El servicio `Update` se genera como `PUT`. De forma predeterminada, IIS no entrega ese verbo a la aplicación: el handler `ExtensionlessUrlHandler-Integrated-4.0` viene con `verb="GET,HEAD,POST,DEBUG"`. El cliente recibe **404 HTML de IIS**; `List`, `Get` y `Create` pueden funcionar normalmente.

Corrección duradera: en el **IIS Manager como administrador**, nodo del **servidor** → Mapeos de controladores → `ExtensionlessUrlHandler-Integrated-4.0` → Restricciones de solicitud → Verbos → agregue `PUT` (y `DELETE`/`PATCH` si es necesario) → reinicie IIS.

No agregue el handler solamente en el `web.config` de la aplicación generada: el Build All regenera esa sección. Tenga cuidado con WebDAV habilitado en el servidor.

El generador **.NET** no presenta este comportamiento. Diagnóstico completo: [B071-B073/B079](Docs/Implementation/B071-B073-B079-GET-CREATE-UPDATE-HTTP.md).

## Actualización de la extensión

Cuando haya una nueva DLL:

**Usuario final** (instaló solamente la DLL del Release):

La actualización solamente con **Add > Local** **no está comprobada**. En B094, con la DLL ya presente en `Packages`, Add > Local falló con `Error installing extension`; la reinstalación limpia exigió eliminar esa DLL (Program Files; normalmente con elevación) y repetir el flujo de instalación. Detalles y observaciones: [Docs/Public/INSTALL.md](Docs/Public/INSTALL.md#atualização-usuário-final).

**Desarrollador / mantenedor** (repositorio clonado) — camino comprobado:

1. Cierre la IDE GeneXus
2. Ejecute [`Install-ExtensionForGeneXus18.bat`](Install-ExtensionForGeneXus18.bat) como administrador; si la IDE está en otro directorio, páselo como primer argumento
3. Si el manifiesto/registro cambió desde el último `genexus /install`, ejecute [`Register-ExtensionForGeneXus18.bat`](Register-ExtensionForGeneXus18.bat) y ejecute `genexus /install`
4. Abra nuevamente la IDE

Detalles: [Docs/Public/INSTALL.md](Docs/Public/INSTALL.md).

## Roadmap resumido

| Etapa | Enfoque |
|-------|------|
| Alpha (ahora) | Primera versión abierta utilizable |
| Sprint 9 | Correcciones reales con feedback externo |
| Sprint 10 / Beta | Flujo principal estable y releases previsibles |

## Documentación

| Documento | Contenido |
|-----------|----------|
| [INSTALL](Docs/Public/INSTALL.md) | Instalación |
| [DEMO](Docs/Public/DEMO.md) | Guion corto |
| [CHANGELOG](CHANGELOG.md) | Historial de cambios |
| [0.1.0-alpha.4](Docs/Releases/0.1.0-alpha.4.es.md) | Notas ES; [PT-BR](Docs/Releases/0.1.0-alpha.4.md); [EN](Docs/Releases/0.1.0-alpha.4.en.md) — elección de la DLL |
| [Decisiones del MVP](Docs/Decisions/2026-07-14-REGISTRO_DECISOES_FUNCIONAIS_MVP.md) | Fuente primaria funcional |
| [Foundation](Docs/Foundation/00-MASTER_INDEX_DO_PROJETO.md) | Contratos y planificación |
| [Checkpoint operativo](Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md) | Estado interno del proyecto |

## Cómo contribuir

Los bugs, mejoras, documentación, pruebas y feedback de uso real son bienvenidos.

Lea [CONTRIBUTING.md](CONTRIBUTING.md). Licencia: [MIT](LICENSE).

## Estructura del repositorio

- `Docs` — documentación pública, foundation y evidencias
- `Src` — extensión y dominio
- `Tests` — pruebas locales
- `Tools` — instalación y checkers
- `Samples` — espacio para ejemplos futuros
