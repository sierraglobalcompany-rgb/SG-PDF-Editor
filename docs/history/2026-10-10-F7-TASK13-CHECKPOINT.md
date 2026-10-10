# SG PDF Editor — F7 Task 13 Hardening — Checkpoint

Fecha: 2026-10-10

## Estado

**F7 Task 13 — CLOSED / AUTOMATED PASS**

Este checkpoint cierra únicamente Task 13 — Hardening, rendimiento y regresiones. No inicia Task 14.

## Rama y SHAs

- Repo: `sierraglobalcompany-rgb/SG-PDF-Editor`
- Rama: `feat/f7-text-v1`
- Inicio exacto de Task 13 / Task 12 CLOSED: `20798f13de4d38d56931154216308c240ee66a00`
- RED: `8df8a5652e208d303058a80da67e451e130edd2e`
- GREEN funcional: `50c8d71d3e7d22c3bd6173cb1296e098b051261f`
- `main` verificado sin cambios: `31c0594758a83ec555d73ecdd7c597cdf8791fd7`

## Alcance literal verificado

### 1. Discovery limitado a página activa

Se añadió una regresión con PDF sintético de dos páginas que entra a EDITAR e instrumenta `_getImageObjects` y `_getTextObjects`.

Resultado: ambos discovery se invocan exactamente una vez y únicamente con `pageIndex = 0`, la página activa. No se barre el documento completo.

### 2. Estado persistente sin handles nativos

La regresión inspecciona fields/properties de los artefactos persistentes de texto:

- `PdfTextObjectInfo`
- `TextObjectKey`
- `PdfTextObjectQuad`
- `PdfTextFillColor`
- `TextEditState`
- `TextEditCandidate`
- `TextEditWorkspace`
- `CidType2FontMapPlan`
- `FallbackFontAsset`

Resultado: no hay `IntPtr` ni `UIntPtr` persistentes en esos artefactos.

### 3. Fallback font > 2 MiB rechazado antes de abrir la fuente

Se encontró un hueco real: `ReadValidatedBytes` ya aplicaba el límite de 2 MiB antes de `File.ReadAllBytes`, pero la ruta de `GlyphTypeface` usada por `SupportsRune` / `GetGlyphId` abría directamente el archivo.

GREEN mínimo:

- `Typeface` ahora llama `CreateValidatedTypeface(ResolveFontPath())`;
- `CreateValidatedTypeface` llama primero al gate común `ValidateFontFile`;
- `ReadValidatedBytes` reutiliza el mismo gate;
- solo después del gate se permite `new GlyphTypeface(...)` o `File.ReadAllBytes(...)`.

No se cambió el font, el hash esperado, dependencias, motor PDF ni estrategia de fallback.

### 4. Cancel después de validation/temp

La regresión ejecuta el writer combinado real con texto dirty y un destino existente sentinela; el validator cancela después de escribir/validar el temporal.

Resultado:

- `OperationCanceledException`;
- destino original intacto;
- workspace de texto sigue dirty;
- cero `.*.sgpdf.tmp` residuales.

### 5. Stale source

La regresión crea workspaces reales de imagen + texto, ensucia ambos, altera el PDF fuente después de capturar los fingerprints y llama al writer combinado real.

Resultado:

- `IOException` antes de publicar;
- ambos workspaces conservan `IsDirty = true`;
- no aparece destino;
- cero temporales residuales.

### 6. Publication failure

La regresión crea workspaces dirty de imagen + texto y fuerza fallo de publicación usando una colisión con directorio en la ruta destino.

Resultado:

- falla la publicación;
- ambos workspaces continúan dirty;
- no se reemplaza la colisión existente;
- cero temporales residuales.

## TDD — RED

Commit:

`8df8a5652e208d303058a80da67e451e130edd2e`

CI Windows:

- workflow: `38089489218`
- job: `114322804371`
- Release build: **PASS**
- warnings: **0**
- errors: **0**
- tests: **766 total**
- PASS: **765**
- FAIL: **1**
- SKIPPED: **0**

Único fallo:

`TextEditHardeningTests.FallbackFontAsset_OversizedLockedFile_IsRejectedBeforeTypefaceOpen`

Causa exacta: el helper/ruta `CreateValidatedTypeface` todavía no existía; las otras cinco regresiones nuevas y las 760 pruebas anteriores quedaron verdes. El RED fue conductual y específico, sin ruido de compilación/harness.

## GREEN

Commit:

`50c8d71d3e7d22c3bd6173cb1296e098b051261f`

CI Windows exact-head funcional:

- workflow: `38089642759`
- job: `114323270538`
- hygiene: **PASS**
- Labelize staging: **PASS**
- locked restore: **PASS**
- Release build: **PASS**
- warnings: **0**
- errors: **0**
- tests: **766 PASS / 0 FAIL / 0 SKIPPED**

La suite completa incluye las regresiones de LEER, FIRMAR, ORGANIZAR, ZPL, EDITAR/F6 imágenes y comportamiento offline existentes; ninguna regresionó.

## Auditoría de scope

Comparación:

`20798f13de4d38d56931154216308c240ee66a00` → `50c8d71d3e7d22c3bd6173cb1296e098b051261f`

Resultado:

- 2 commits ahead;
- 0 behind;
- archivos cambiados únicamente:
  1. `src/SGPdf.App/Features/Edit/Text/FallbackFontAsset.cs`
  2. `tests/SGPdf.App.Tests/TextEditHardeningTests.cs`

Sin cambios en:

- `.csproj`;
- lockfiles;
- NuGet/dependencias;
- motor PDF;
- UI de Task 12;
- writer/validator;
- `main`;
- Task 14/F8.

## QA manual

**NOT RUN / NOT CLAIMED.**

Task 13 se cierra por evidencia automatizada Windows exact-head. La pasada manual Windows pertenece al cierre F7 de Task 14.

## Integración / siguiente gate

- No PR creado.
- No merge realizado.
- `main` continúa en `31c0594758a83ec555d73ecdd7c597cdf8791fd7`.
- Task 14 **NO iniciada**.
- Próximo gate autorizado únicamente con nueva instrucción del usuario: **F7 Task 14 — documentación final, QA manual y cierre/PR**.
