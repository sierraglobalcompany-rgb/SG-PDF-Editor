# SG PDF — F5 ORGANIZAR — Task 3 Checkpoint Final

**Fecha:** 2026-10-09  
**Rama:** `feat/f5-organize`  
**Estado:** Task 3 — F5.1 Structural Preflight + Frozen Policies — **COMPLETO / GREEN**  
**No iniciar Task 4 sin nuevo `continua` del usuario.**

## Baseline

- `main` permanece intacto en `31c0594758a83ec555d73ecdd7c597cdf8791fd7`.
- Checkpoint Task 2: `6f210942726d7f8de279ca01e6236f8fd7917cf6`.
- RED válido Task 3: `1c0dee642eaed777517127032def79826d390b99`.
- Checkpoint RED documental: `ad401a70e7a3e67d8ca46dfd591e90ae328c7f74`.
- GREEN funcional Task 3: `e466ab7124376af4824695f85a98815a2f4b70f0`.

## TDD evidence

### RED

CI `37957833169` sobre `1c0dee642eaed777517127032def79826d390b99`:

- Restore PASS
- Build Release PASS
- 0 warnings / 0 errors
- 450 tests total
- 443 PASS
- 7 FAIL esperados
- los 7 fallos funcionales eran `TypeLoadException` por ausencia de `OrganizePreflightInspector`
- el capability probe de exports PDFium PASS

### GREEN

CI `37958464450` sobre `e466ab7124376af4824695f85a98815a2f4b70f0`:

- repository hygiene PASS
- Labelize pinning PASS
- locked restore PASS
- Release build PASS
- **0 warnings / 0 errors**
- **450/450 tests PASS**
- 0 skipped / 0 failed

## Implementado

### `Features/Organize/OrganizePreflight.cs`

Contratos:

- `OrganizeFindingSeverity`: Info / Warning / Block
- `OrganizePreservationStatus`: ProvenPreserved / ProvenChangedOrLost / Unknown
- `OrganizeFindingKind`: signature, password, form, bookmark, named destination, internal link, tagged structure, page label, attachment, metadata
- `OrganizeFinding`
- `OrganizePreflightResult`
- `OrganizePreflightInspector`

Política inicial F5 implementada:

- PDF abierto con contraseña → `Block`
- firma criptográfica → `Block`
- form detectado → `Warning + Unknown`
- bookmark detectado → `Warning + Unknown`
- enlace interno detectado → `Warning + Unknown`
- metadata detectada → `Warning + Unknown`
- plain unsigned/unprotected → procede sin warning
- cancelación precancelada → aborta sin resultado parcial

Se conserva una costura interna estrecha para inyectar el contador de firmas solo en el test de política; el constructor normal usa `PdfDocumentSession.GetCryptographicSignatureCount()` real.

### `PdfDocumentSession.Organize.cs`

Se añadieron solo:

- `GetOrganizeFormType(...)`
- `HasOrganizeMetadata(...)`

Cada lectura nativa usa `PdfiumRuntime.NativeGate` y no expone handles.

El inspector reutiliza `GetBookmarks()` y `GetPageLinks()` fuera de un gate externo; no se reintroduce el nested-gate/deadlock arreglado en Task 0.

### `PdfiumNative.cs`

Bindings nuevos mínimos y usados:

- `FPDF_GetFormType`
- `FPDF_GetMetaText`

El capability test además confirmó que el DLL pinneado exporta:

- `FPDF_GetSignatureCount`
- `FPDF_GetFormType`
- `FPDF_CountNamedDests`
- `FPDFCatalog_IsTagged`
- `FPDFDoc_GetAttachmentCount`
- `FPDF_GetPageLabel`
- `FPDF_GetMetaText`

No se añadieron bindings de detectores todavía no usados por Task 3. Named destinations/tagged/page labels/attachments quedan para la caracterización/evidencia de Task 9, tal como permite el plan.

## Fixtures/pruebas nuevas

- `tests/SGPdf.App.Tests/OrganizePdfFixtureFactory.cs`
- `tests/SGPdf.App.Tests/OrganizePreflightTests.cs`

Fixtures sintéticas públicas/no privadas:

- plain
- navegación con bookmark + enlace interno
- metadata
- AcroForm
- PDF protegido

No se comprometieron fixtures de clientes/Mercado Libre.

## Self-review

Diff Task 2 checkpoint → GREEN Task 3:

- `docs/superpowers/checkpoints/2026-10-09-f5-task3-red.md`
- `src/SGPdf.App/Features/Organize/OrganizePreflight.cs`
- `src/SGPdf.App/Pdf/PdfDocumentSession.Organize.cs`
- `src/SGPdf.App/Pdf/PdfiumNative.cs`
- `tests/SGPdf.App.Tests/OrganizePdfFixtureFactory.cs`
- `tests/SGPdf.App.Tests/OrganizePreflightTests.cs`

Sin cambios a:

- UI/WPF
- Firma
- ZPL/Labelize
- Reader
- writer estructural
- dependencias/lockfiles
- proyectos/arquitectura
- `main`

El commit accidental `a05f79c98a98136828c91f490abbc1c527b6858d` con `noop` fue eliminado previamente de la historia activa mediante force-with-lease y no aparece en el diff efectivo.

## Siguiente paso exacto

**Task 4 — F5.2 Transactional Writer + Output Validator**.

Al recibir `continua`:

1. escribir RED de `OrganizeOutputValidator`;
2. escribir RED del `PdfOrganizeWriter` transaccional;
3. verificar fallos por feature ausente;
4. implementar materializador mínimo con PDFium;
5. renderizar todas las páginas a 36 DPI en validación;
6. preservar destino ante cualquier fallo;
7. limpiar temp en `finally`;
8. suite completa + CI exact-head;
9. checkpoint final;
10. detenerse antes de Task 5.

**No mergear `main` sin aprobación explícita del usuario.**
