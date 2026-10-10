# F8 — Task 4 checkpoint — identidad estable + hit testing determinista

Fecha: 2026-10-10
Rama: `feat/f8-comments-v1`
Base Task 3: `d541fe0270214077a99e9e332f867284187133c7`

## Estado

Task 4 implementada y verificada automáticamente. Manual Windows/stylus/touch/external-viewer QA: **NOT RUN**.

## Contrato implementado

- `CommentIdentity` no usa `AnnotationIndex` como autoridad durable.
- Identidad estructural: `PageIndex + NativeSubtype + StructuralFingerprint + DuplicateOrdinal + DuplicateCount`.
- El fingerprint incluye geometría disponible, RGBA, quadpoints, contenido de nota, Ink strokes y border width; excluye el índice nativo.
- `Resolve(...)` re-resuelve contra un snapshot fresco y falla cerrado si cambia la estructura o la cardinalidad de duplicados exactos.
- Duplicados estructuralmente idénticos se distinguen por ordinal determinista; si aparece/desaparece un duplicado exacto, no se elige silenciosamente otro objeto.
- `CommentHitTester` elige de forma determinista la anotación topmost por `AnnotationIndex` entre los hits del snapshot actual.
- HIGHLIGHT/UNDERLINE/STRIKEOUT usan quad geometry, no solo el rectángulo grueso.
- INK usa distancia a segmentos + tolerancia + medio border width cuando existe.
- CIRCLE usa geometría elíptica; TEXT/SQUARE y subtipos con rect válido usan rect hit.
- `HitTestDevice(...)` convierte device → PDF mediante `PdfPageDeviceTransform`, por lo que la rotación queda absorbida por la transformación ya autoritativa del render.
- Geometría no usable se ignora; tolerancias inválidas fallan explícitamente.

## TDD evidence

### RED

Commit RED final: `fa8eecea84e34ed6ced89ed1c8b7a87249915ad4`
Windows CI: workflow `38094840759`, job `114338523633`.
Resultado esperado:
- Release build: 0 warnings / 0 errors.
- Tests existentes: 791 PASS.
- Nuevos Task 4: 10 FAIL por ausencia de `CommentIdentity` / `CommentHitTester`.

### GREEN intermedio

Se añadieron únicamente:
- `src/SGPdf.App/Features/Comments/CommentIdentity.cs`
- `src/SGPdf.App/Features/Comments/CommentHitTester.cs`

Primer CI GREEN detectó un defecto de compilación trivial: faltaba `using System.IO` en `CommentIdentity.cs`; no se relajaron tests ni contrato.

### GREEN funcional

Head funcional antes de este checkpoint: `236bff85990195a0effe837b41370f46a7ac8bc1`
Windows CI: workflow `38095045823`, job `114339121122`.
Resultado:
- Release build: **0 warnings / 0 errors**.
- Tests: **801 PASS / 0 FAIL / 0 skipped**.

## Scope audit

Diff Task 3 checkpoint → GREEN Task 4: 5 commits ahead / 0 behind.
Archivos cambiados únicamente:
- `src/SGPdf.App/Features/Comments/CommentIdentity.cs`
- `src/SGPdf.App/Features/Comments/CommentHitTester.cs`
- `tests/SGPdf.App.Tests/CommentIdentityTests.cs`
- `tests/SGPdf.App.Tests/CommentHitTesterTests.cs`

No se modificó:
- `CommentWorkspace` / `CommentState`;
- discovery PDFium de Task 3;
- writer/validator;
- UI/XAML;
- paquetes/NuGet;
- engine PDF;
- networking/cloud;
- `main`.

## Gate

Task 5 no se inicia dentro de este checkpoint. Siguiente tarea del plan: **Text-markup geometry + writer/validator slice**.
