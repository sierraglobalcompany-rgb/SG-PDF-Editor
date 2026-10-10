# F7 — Task 3 Shared EDITAR Infrastructure — CHECKPOINT

**Fecha:** 2026-10-10  
**Fase:** F7 — Texto V1  
**Task:** 3 — promover infraestructura F6 realmente compartida por EDITAR  
**Estado:** **GREEN / PASS automatizado**  
**Rama:** `feat/f7-text-v1`  
**Base Task 3:** `1154895bcb5665f35979fd66f935cc2011ca59eb`  
**Head funcional del refactor:** `38bcb4e461c23dfd7e7bd93f2ad7ba0f50c0db7f`  
**Head GREEN verificado tras corregir el gate:** `3b78230fc772f4392001b70c1881bcb2b2c7596e`

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

## Incidencia de verificación

El primer CI posterior al refactor (`cec420bf721de69da702a30872a93726e3819dcb`, run `38072323944`) compiló 0/0 pero dejó 691 PASS / 1 FAIL porque el barrido mecánico también había reescrito las cinco cadenas *legacy* del propio test estructural. El test terminó intentando `Assert.Null(...)` sobre los nombres nuevos.

No fue una regresión de producto. Se corrigieron únicamente esas cadenas del gate en `3b78230fc772f4392001b70c1881bcb2b2c7596e`.

## GREEN exacto

CI: `38072433547`

- repository hygiene: PASS;
- Labelize staging: PASS;
- locked restore: PASS;
- Release build: **0 warnings / 0 errors**;
- tests: **692 PASS / 0 FAIL / 0 skipped**;
- workflow: **SUCCESS**.

## Gobernanza

- F6 PR #25 continúa sin merge automático.
- `main` no debe modificarse ni mergearse sin aprobación explícita.
- Manual Windows QA permanece `NOT RUN`.
- Este checkpoint cierra únicamente Task 3.

## Próximo paso permitido

**Task 4 — shell único de EDITAR para imagen + texto**, siguiendo RED → GREEN.

No iniciar Task 5+ antes de cerrar Task 4.
