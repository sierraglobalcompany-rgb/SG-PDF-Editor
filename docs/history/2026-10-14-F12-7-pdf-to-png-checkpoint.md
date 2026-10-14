# SG PDF — Checkpoint F12.7 PDF→PNG

Fecha: 2026-10-14

## Estado operativo

F12.7 Audited Conversions permanece **IN PROGRESS**.

Rama activa:
`feat/f12-7-pdf-to-png-mvp`

Base autorizada del slice:
`5b7d1f817c8704becad44f13d4fd74aebdd4d038`

No se ha hecho merge a `main`.

## Bloque 1 — Core PDF→PNG

**CERRADO / AUTO PASS.**

RED:
- commit: `a61ea392484a7c66dc7e15eb17a836a242c3c6d0`
- CI run: `38554190200`
- job: `115819132469`
- resultado esperado: Build FAIL porque `SGPdf.App.Features.Conversions` todavía no existía (`CS0234`).

GREEN:
- commit: `ede790484b9d6dc638854e560e2717ef18a61708`
- CI run: `38554558763`
- job: `115820363712`
- Release build: PASS, 0 warnings, 0 errors.
- tests: 1201/1201 PASS, 0 failed, 0 skipped.

Implementación cerrada:
- `src/SGPdf.App/Features/Conversions/PdfToPngExporter.cs`
- reutiliza `PdfDocumentSession` / PDFium existente;
- PNG codificado con `PngBitmapEncoder` de WPF;
- 144 DPI fijos para este MVP;
- todas las páginas;
- nombres deterministas `<basename>-pagina-001.png`;
- preflight de colisiones;
- `FileMode.CreateNew`, sin sobrescritura silenciosa;
- limpieza de salidas parciales si una página posterior falla;
- cancelación propagada;
- fuente PDF intacta;
- 0 dependencias nuevas;
- 0 segundo motor PDF;
- sin framework genérico de conversiones.

## Bloque 2 — UI/export mínimo

**ABIERTO EN RED. NO IMPLEMENTADO.**

RED actual:
- commit: `8e953a7a9d2eca1d2f04dd9a8f261c3bfe4d97e0`
- test: `tests/SGPdf.App.Tests/MainWindowPdfToPngTests.cs`
- contrato pendiente de implementar:
  - `MainWindow.ExportPdfToPng_Click`
  - `MainWindow.TryExportPdfToPngAsync`
  - runner devuelve `Task<bool>`.

CI del RED al momento de crear este checkpoint:
- run: `38555023277`
- job: `115821912654`
- Build ya PASS;
- Test todavía estaba ejecutándose;
- no se escribió GREEN de UI antes de este checkpoint.

## Siguiente acción exacta

Cuando se continúe:
1. confirmar el resultado final del RED `38555023277`;
2. si falla por ausencia de los handlers esperados, implementar solo la UI mínima;
3. añadir en `Herramientas` una acción `Exportar PDF a PNG...` vinculada al PDF abierto;
4. seleccionar carpeta con `OpenFolderDialog` existente;
5. llamar exclusivamente a `PdfToPngExporter`, sin duplicar render/codificación;
6. usar `_utilityExecuting` + `SetBusy(true/false)` y mensajes de estado existentes;
7. cancelación de diálogo = no-op controlado;
8. error controlado, sin archivos parciales gracias al owner core;
9. exact-head Build + suite completa;
10. si queda GREEN, actualizar `.planning/STATE.md` y `.planning/ROADMAP.md` y cerrar F12.7 PDF→PNG MVP.

## Scope congelado

NO agregar en este bloque:
- JPEG/WebP/TIFF;
- selector de formato;
- selector de páginas;
- DPI/quality configurables;
- imagen→PDF;
- Office/LibreOffice;
- cloud/backend;
- segundo renderer/PDF engine;
- registry/plugin/converter framework;
- workflow engine/queue.

## Leyes

- KISS: camino directo `PDF abierto → carpeta destino → PdfToPngExporter → PNGs`.
- Ley del ruido: DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD antes de crear owners nuevos.
- No merge a `main` sin aprobación explícita del usuario.
