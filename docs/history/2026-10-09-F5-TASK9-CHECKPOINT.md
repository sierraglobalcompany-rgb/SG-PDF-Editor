# F5 Task 9 — Checkpoint: Preservation Evidence Matrix + Warning Policy

Fecha: 2026-10-09
Rama: `feat/f5-organize`
Base de Task 9: `e7b606e86307a996c7eb87c7be88041df1f19c2a` (checkpoint Task 8)

## Objetivo cerrado

Task 9 endurece la política de preservación de F5 con evidencia ejecutable. No introduce un segundo motor, no modifica el writer y no intenta un reescritor genérico de objetos PDF.

## TDD / evidencia

### 1. RED — detectores extendidos

- Commit test-only inicial: `4090b91096580c09ffef328e90080c124d664331`.
- Corrección exclusiva de firma pública de tests: `0e538bdf89866419313534ea3ac8a7159d3b0c00`.
- CI RED válido: `37977866022`.
- Build: 0 warnings / 0 errores.
- Tests: 538 total; 534 PASS / 4 FAIL.
- Los cuatro fallos fueron exactamente los contratos ausentes de detección: named destinations, tagged structure, page labels y attachments.

### 2. GREEN — detección conservadora

- `f0b36523e0a42422ca3a3d690cb91218f01e1a97`: bindings PDFium para `FPDF_CountNamedDests`, `FPDFCatalog_IsTagged`, `FPDFDoc_GetAttachmentCount`, `FPDF_GetPageLabel`.
- `e2c1845d3eef87e73213b5746e0ceb363febc441`: probes de sesión bajo `PdfiumRuntime.NativeGate`.
- `af7091ab9dd5460dccd7e282ba6f6107d975cfd8`: preflight inicial de los cuatro tipos como `Warning + Unknown`.
- CI `37978208309`: PASS.

### 3. Caracterización real del writer

- Commit de caracterización: `ab49c73c248467107209163d37f9515d41dceeec`.
- CI intencionalmente rojo: `37978406373`.
- Observación de copia de identidad real:
  - Metadata: origen `[Metadata]`; salida `[Metadata]`.
  - Navigation: origen `[Bookmark,InternalLink]`; salida `[Metadata]`.
  - Form: origen `[Form]`; salida `[Metadata]`.
  - NamedDestination: origen `[NamedDestination]`; salida `[Metadata]`.
  - TaggedStructure: origen `[TaggedStructure]`; salida `[Metadata]`.
  - PageLabel: origen `[PageLabel]`; salida `[Metadata]`.
  - Attachment: origen `[Attachment]`; salida `[Metadata]`.
- El hecho de que la salida contuviera metadata no se tomó como prueba de preservación de metadata original.

### 4. RED/GREEN — valores reales de metadata

- `3045a6d3fcef8daf5fe51316c3921dceeea19160`: tests de pérdida estructural real y RED para lector de valores metadata.
- CI `37978641756`: build 0/0; 546 total; 545 PASS / 1 FAIL, únicamente por ausencia de `GetOrganizeMetadataText`.
- `01c6de593498de47b882b603510df6633b99465c`: lector de metadata bajo `NativeGate` usando `FPDF_GetMetaText`.
- CI `37978861031`: build 0 warnings / 0 errores; **546/546 PASS**.
- Evidencia: `Title = SG PDF metadata fixture` y `Author = SG PDF tests` en origen no se preservan con los mismos valores en la salida.

### 5. RED/GREEN — política final de preservación

- `b320f8ad02e89343cbf273b3a048254de41b2bbb`: RED de clasificación.
- CI `37979235200`: build 0/0; **539 PASS / 7 FAIL**, todos por `Unknown` donde la evidencia exigía `ProvenChangedOrLost`.
- Firma y password continuaron pasando como `Block + Unknown`.
- `619ee15ca3beed3f6f2fb72844288792f1f67114`: política de producción actualizada.
- CI `37979433987`: build **0 warnings / 0 errores**; **546/546 PASS**.

## Clasificación final Task 9

### Bloqueo duro

- `CryptographicSignature` → `Block + Unknown`, sin override.
- `PasswordProtectedSource` → `Block + Unknown`, sin override.

No se intenta materialización estructural para esos casos.

### Warning con evidencia de cambio/pérdida

Las siguientes estructuras detectadas quedan en `Warning + ProvenChangedOrLost`:

- Form
- Bookmark
- InternalLink
- NamedDestination
- TaggedStructure
- PageLabel
- Attachment
- Metadata

Todas requieren confirmación explícita antes de materializar.

No se clasificó ninguna de estas estructuras como `ProvenPreserved`.

## Fixtures representativas

`OrganizePdfFixtureFactory` contiene fixtures sintéticas/no privadas para:

- plain
- protected
- metadata
- navigation (bookmark + internal link)
- AcroForm
- named destination
- tagged structure
- page labels
- attachment

## UI / autorización

La política existente de WPF se conserva:

- Block → writer no se llama.
- Warning + Cancel → writer no se llama.
- Warning + Confirm → se pasa autorización explícita al writer.

Los tests previos de `MainWindowOrganizeTask6Tests` continúan cubriendo cancelación/confirmación. Task 9 no rediseña UI.

## Matriz durable

Documento autoritativo:

`docs/history/2026-10-09-F5-preservation-matrix.md`

La matriz distingue explícitamente detector, fixture, evidencia, clasificación, política UI y límites. No contiene una promesa global de preservación.

## Auditoría de alcance

Diff Task8 checkpoint → matriz Task9 antes de este checkpoint:

- `src/SGPdf.App/Features/Organize/OrganizePreflight.cs`
- `src/SGPdf.App/Pdf/PdfDocumentSession.Organize.cs`
- `src/SGPdf.App/Pdf/PdfiumNative.cs`
- `tests/SGPdf.App.Tests/OrganizePdfFixtureFactory.cs`
- `tests/SGPdf.App.Tests/OrganizePreflightTests.cs`
- `tests/SGPdf.App.Tests/OrganizePreservationTests.cs`
- `docs/history/2026-10-09-F5-preservation-matrix.md`

No se modificaron:

- `PdfOrganizeWriter`
- `OrganizeOutputValidator`
- WPF/UI
- dependencias/proyectos
- FIRMAR
- LEER
- ZPL/Labelize

## Límites / QA

- La evidencia representa fixtures sintéticas estables, no todas las posibles construcciones del estándar PDF.
- La pérdida observada en copia de identidad impide hacer una promesa más fuerte para operaciones F5 que reutilizan el mismo pipeline de documento nuevo + importación de páginas.
- No se ejecutó QA manual/visual de Windows dentro de Task 9.
- No se usaron fixtures privadas.
- Task 10 no se inició en este checkpoint.

## Cierre pendiente de este checkpoint

Después de este commit debe ejecutarse CI exacto del nuevo head y revalidarse que `main` permanezca intacto antes de declarar Task 9 cerrada.
