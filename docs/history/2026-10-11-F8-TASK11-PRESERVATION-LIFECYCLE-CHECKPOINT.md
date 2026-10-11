# F8 Task 11 — Preservation matrix + lifecycle checkpoint

Fecha: 2026-10-11  
Rama: `feat/f8-comments-v1`

## Base exacta

Task 10 checkpoint:

`a3d56f287848020580cdba3d403eb84b3943f691`

## Alcance ejecutado

Task 11 del plan F8:

- medición fresca de preservación usando `PdfCommentWriter` real;
- fixture F8 propia, sin reutilizar conclusiones de F7;
- estructuras medidas: AcroForm/value, bookmarks, named destinations, internal links, tagged structure, page labels, attachment exact payload, metadata y page rotation;
- preservación de anotación `TEXT` soportada ya existente;
- preservación de anotación `STAMP` representativa no soportada/read-only en V1;
- lifecycle real `create → save → reopen → edit → save → reopen → delete → save → reopen`;
- matriz durable en `docs/history/2026-10-10-F8-preservation-matrix.md`.

## RED observado

Commit:

`20ee97f1a748d48966c95173b670c6ad2d304d35`

CI Windows:

- workflow `38103391271`;
- job `114363775914`;
- build 0 warnings / 0 errors;
- tests 847 PASS / 1 FAIL / 0 skipped.

El único fallo provenía de una aserción textual demasiado estricta para `STAMP`: esperaba `/Subtype /Stamp`, mientras PDFium reserializó válidamente `/Subtype/Stamp`. La anotación no soportada sí estaba preservada. No se modificó producción.

## GREEN funcional

Commit:

`63785d450a293de98f0ae898ebd875e8003438a3`

CI Windows:

- workflow `38103509065`;
- job `114364109894`;
- build 0 warnings / 0 errors;
- tests **848 PASS / 0 FAIL / 0 skipped**.

La única corrección entre RED y GREEN fue hacer la aserción tolerante a serialización PDF equivalente mediante `/Subtype\s*/Stamp`.

## Scope audit

Comparación:

`a3d56f287848020580cdba3d403eb84b3943f691..8fa48e8f3625c3cd593e7b497a2fba4e736049b3`

Resultado:

- status: ahead;
- ahead_by: 3;
- behind_by: 0;
- merge base exacta: checkpoint Task 10;
- archivos cambiados: 2;
  1. `tests/SGPdf.App.Tests/PdfCommentPreservationF8Tests.cs`
  2. `docs/history/2026-10-10-F8-preservation-matrix.md`
- producción modificada: **ninguna**;
- sin cambios en PDFium, writer, preflight, UI, csproj, paquetes o lockfiles.

## Estado de evidencia

Fresh F8 evidence: **PROVEN PRESERVED** para todas las estructuras representativas exigidas por Task 11 y para el lifecycle completo.

`STAMP` queda únicamente como evidencia de preservación/read-only; no se promueve a edición V1.

## QA manual

- Windows manual: NOT RUN
- stylus/touch: NOT RUN
- Adobe/Foxit/Edge external-reader visual validation: NOT RUN

## Siguiente unidad

Task 12 — hardening/regression/automated closure.

No abrir Task 12 antes de cerrar el CI exact-head de este checkpoint.
No merge.
