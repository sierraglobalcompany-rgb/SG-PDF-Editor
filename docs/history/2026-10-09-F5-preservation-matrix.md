# F5 — Matriz de evidencia de preservación

Fecha: 2026-10-09

## Alcance

Esta matriz documenta únicamente lo demostrado por fixtures sintéticas representativas y por el writer estructural actual de F5 (`PdfOrganizeWriter`). No es una afirmación general sobre PDFium ni una garantía para toda variante posible del formato PDF.

La caracterización principal usa una **copia de identidad**: importa todas las páginas, en el mismo orden y sin cambios deliberados de rotación. Si una estructura ya cambia o desaparece en ese caso mínimo, las operaciones F5 que comparten el mismo pipeline de documento nuevo + importación de páginas no pueden prometer preservarla.

## Matriz

| Estructura | Detector / API | Fixture / evidencia | Resultado al materializar | Clasificación F5 | Política UI | Límites de la evidencia |
|---|---|---|---|---|---|---|
| Firma criptográfica | `FPDF_GetSignatureCount` | Política probada con contador de firmas inyectado; export verificado | No se intenta writer | `Unknown` + `Block` | Bloqueo duro, sin override | F5 deliberadamente no altera PDFs firmados; no se hace inferencia sobre qué sobreviviría si se forzara la escritura. |
| PDF abierto con contraseña | `OpenedWithPassword` | `CreateProtected()` | No se intenta writer | `Unknown` + `Block` | Bloqueo duro, sin override | La contraseña no se persiste ni reutiliza para salida estructural. |
| Formularios | `FPDF_GetFormType` | `CreateAcroForm()` + `RealWriter_IdentityImport_DropsDetectedNonMetadataStructure` | Detectado en origen, ausente al reabrir salida | `ProvenChangedOrLost` | `Warning`, confirmación explícita | Fixture representativa AcroForm/Widget. No implica caracterización exhaustiva de todas las variantes XFA/AcroForm. |
| Marcadores | `GetBookmarks()` | `CreateNavigation()` + writer real | Detectados en origen, ausentes en salida | `ProvenChangedOrLost` | `Warning`, confirmación explícita | Fixture representativa con outline y destino de página. |
| Enlaces internos | `GetPageLinks()` con `InternalGoto` | `CreateNavigation()` + writer real | Detectados en origen, ausentes en salida | `ProvenChangedOrLost` | `Warning`, confirmación explícita | Fixture representativa de enlace página-a-página. No se generaliza a todos los tipos de anotación/enlace externo. |
| Destinos nombrados | `FPDF_CountNamedDests` | `CreateNamedDestination()` + writer real | Detectados en origen, ausentes en salida | `ProvenChangedOrLost` | `Warning`, confirmación explícita | Fixture representativa con name tree de destinos. |
| Estructura etiquetada | `FPDFCatalog_IsTagged` | `CreateTagged()` + writer real | Detectada en origen, ausente en salida | `ProvenChangedOrLost` | `Warning`, confirmación explícita | Fixture representativa con `MarkInfo` + `StructTreeRoot`. |
| Etiquetas de página | `FPDF_GetPageLabel` | `CreatePageLabels()` + writer real | Detectadas en origen, ausentes en salida | `ProvenChangedOrLost` | `Warning`, confirmación explícita | Fixture representativa de number tree con prefijo. |
| Adjuntos | `FPDFDoc_GetAttachmentCount` | `CreateAttachment()` + writer real | Detectados en origen, ausentes en salida | `ProvenChangedOrLost` | `Warning`, confirmación explícita | Fixture representativa con `EmbeddedFiles`, `Filespec` y stream embebido. |
| Metadatos | `FPDF_GetMetaText` + `GetOrganizeMetadataText` | `CreateMetadata()` + `Metadata_IdentityImport_DoesNotPreserveSourceTitleOrAuthor` | La salida puede contener metadata generada, pero `Title` y `Author` originales no se preservan | `ProvenChangedOrLost` | `Warning`, confirmación explícita | La evidencia demuestra pérdida/cambio de valores originales representativos; no afirma que todos los tags siempre queden vacíos. |

## Política resultante

- `CryptographicSignature` y `PasswordProtectedSource`: `Block`; no existe override.
- Estructuras no criptográficas detectadas y demostradas como cambiadas/perdidas: `Warning + ProvenChangedOrLost` y requieren confirmación explícita antes de escribir.
- Un hallazgo `Unknown` no se presenta nunca como preservado y, si es no criptográfico, debe seguir siendo `Warning`.
- `ProvenPreserved` solo puede mostrarse como `Info` cuando exista evidencia representativa suficiente. **Task 9 no clasifica ninguna de las estructuras estudiadas como `ProvenPreserved`.**
- La cancelación de una advertencia no llama al writer; confirmar la advertencia autoriza el intento de escritura. Esto ya está cubierto por los tests WPF de Guardar como.

## Operaciones cubiertas por inferencia del pipeline

Las operaciones de reorganización, inserción, merge, extracción y split terminan materializando mediante el mismo writer de documento nuevo e importación de páginas. La prueba de identidad es el caso menos destructivo del pipeline: al observar pérdida/cambio allí, F5 no hace una afirmación de preservación más fuerte para operaciones estructurales posteriores.

Esta inferencia es sobre **el pipeline actual de SG PDF Editor**, no sobre PDFium de forma general.

## Lo que NO se afirma

- No se promete preservación completa de objetos de documento, accesibilidad, scripts, anotaciones, capas, portfolios ni estructuras no modeladas por los detectores actuales.
- No se afirma que una estructura no detectada esté ausente o preservada.
- No se usa un segundo motor ni un reescritor genérico de object graph para intentar conservar estas estructuras.
- No se usan fixtures privadas ni PDFs de clientes para estas conclusiones.
- QA manual/visual de Windows no forma parte de esta matriz.
