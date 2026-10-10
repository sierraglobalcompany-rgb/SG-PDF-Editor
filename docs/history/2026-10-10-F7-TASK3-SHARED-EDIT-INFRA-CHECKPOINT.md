# F7 — Task 3 Shared EDITAR Infrastructure — CHECKPOINT

**Fecha:** 2026-10-10  
**Fase:** F7 — Texto V1  
**Task:** 3 — promover infraestructura F6 realmente compartida por EDITAR  
**Estado:** **CI_PENDING**  
**Rama:** `feat/f7-text-v1`  
**Base Task 3:** `1154895bcb5665f35979fd66f935cc2011ca59eb`  
**Head funcional:** `38bcb4e461c23dfd7e7bd93f2ad7ba0f50c0db7f`

## RED

- SHA: `4f9111781a2bd21dda5e87941476fc56269f5efe`
- CI: `38072099798`
- Build: 0 warnings / 0 errors.
- Tests: 691 PASS / 1 FAIL / 692 total.
- Único fallo esperado: `PdfEditSharedInfrastructureNamingTests`, porque los nuevos tipos `PdfEdit*` aún no existían.

## GREEN aplicado

Promoción behavior-preserving:

- `ImageEditSourceFingerprint` → `PdfEditSourceFingerprint`;
- `ImageEditPreflight*` → `PdfEditPreflight*` / `PdfEditFinding*`;
- `PdfImageEditWriter` → `PdfEditWriter`;
- `ImageEditOutputValidator` → `PdfEditOutputValidator`.

Los tipos compartidos de fingerprint/preflight viven ahora en `SGPdf.App.Features.Edit`.

Se actualizaron referencias F6 y tests existentes. Los workspaces, comandos y tipos propios de imagen permanecen específicos de imagen.

## Scope / KISS

- Sin `IPdfWriter`.
- Sin base writer, factory hierarchy, strategy framework ni registry genérico.
- Sin cambio de UX.
- `MainWindow.EditImages.cs` y `MainWindow.EditImages.Hardening.cs` **no se renombran en Task 3**; eso pertenece a Task 4.
- El workflow temporal utilizado para efectuar el rename atómico se autoeliminó y no aparece en el diff final contra la base Task 3.

## Evidencia pendiente

Windows CI exacto sobre este checkpoint: **PENDING**.

No avanzar a Task 4 hasta reemplazar este estado por PASS con evidencia exacta.
