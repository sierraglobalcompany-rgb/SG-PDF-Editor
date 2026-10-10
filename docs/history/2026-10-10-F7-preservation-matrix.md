# F7 — Task 11 Preservation Matrix — CHECKPOINT

**Fecha:** 2026-10-10  
**Fase:** F7 — Texto V1  
**Task:** 11 — matriz de preservación F7 + preflight  
**Estado:** **CLOSED / AUTOMATED PASS**  
**Rama:** `feat/f7-text-v1`  
**Base Task 11:** `d1d2030ff0ae16e2a05f9991b0fdfc1c9adf84b9`  
**Head funcional GREEN:** `65a2f23a604e4013ca4b11b3248374de43a202ce`

## Objetivo

Task 11 vuelve a medir preservación con el **writer combinado real de F7**. No se reutilizan como verdad los resultados históricos de F6.

La fixture representativa ejecuta en una sola materialización:

- una edición real de imagen: movimiento de la imagen existente;
- una edición real de texto por `OriginalFont`: `BASE -> SEAB`;
- guardado mediante el overload combinado `ImageEditWorkspace + TextEditWorkspace`;
- reapertura del PDF resultante antes de aceptar cualquier conclusión de preservación.

## Matriz medida F7

| Estructura | Evidencia automatizada después de save -> reopen | Resultado | Preflight |
| --- | --- | --- | --- |
| AcroForm + valor | formulario sigue presente y el valor `Filled Value` permanece | `ProvenPreserved` | `Info` |
| Bookmarks / outlines | bookmark `Root` permanece y apunta a la página esperada | `ProvenPreserved` | `Info` |
| Named destination + internal link | `ChapterOne` permanece; el link interno sigue resolviendo a la página 2 | `ProvenPreserved` | `Info` |
| Tagged PDF / `StructTreeRoot` | documento sigue marcado/tagged y conserva `StructTreeRoot` | `ProvenPreserved` | `Info` |
| Page labels | las etiquetas de página permanecen, incluida `A-` | `ProvenPreserved` | `Info` |
| Embedded attachment | `note.txt` permanece y `FPDFAttachment_GetFile` devuelve exactamente `hello-f7` | `ProvenPreserved` | `Info` |
| Metadata | `Title`, `Author` y metadata custom `CustomMarker` permanecen | `ProvenPreserved` | `Info` |
| Page rotation | la segunda página conserva rotación PDFium `1` = 90° | `ProvenPreserved` | `Info` |

No se observó pérdida inesperada de ninguna de las estructuras requeridas en la fixture combinada representativa.

## Política de preflight F7

Se mantiene la política conservadora existente:

- PDF abierto con contraseña: `Block` + `Unknown`;
- firma criptográfica o imposibilidad de comprobarla con seguridad: `Block` + `Unknown`;
- estructura con preservación demostrada por el writer combinado F7: `Info` + `ProvenPreserved`;
- estructura no criptográfica con estado `Unknown` o `ProvenChangedOrLost`: `Warning` y `RequiresWarningConfirmation = true`.

Task 11 añade `PageRotation` al preflight y actualiza la evidencia textual para referirse al writer combinado de EDITAR/F7, no al writer histórico F6.

## RED

### Primer RED

Commit: `924cf4c1d94445e6b6c0d78fc8f1b3ac0945dbad`  
Workflow: `38087297055`  
Job: `114316354690`

Resultado:

- build: **0 warnings / 0 errors**;
- tests: **741 PASS / 10 FAIL / 751 total**.

El RED detectó correctamente el preflight histórico, pero también expuso un error del harness: el test buscaba el valor del formulario con espaciado PDF textual rígido.

### Corrección de harness — formulario

Commit: `71ca47141f1a0ed341a155e27873c86f7aa70488`  
Workflow: `38087398406`  
Job: `114316656001`

Se sustituyó la comparación cruda dependiente de espaciado por una aserción tolerante a la serialización. El valor del formulario quedó demostrado como preservado. El run dejó visible un segundo error de harness: buscar el payload del attachment como texto plano dentro del PDF serializado.

### RED válido final

Commit: `7165989111c119000f84faab37dde9465681bc1c`  
Workflow: `38087531222`  
Job: `114317053256`

El attachment pasó a medirse mediante la API de PDFium (`FPDFDoc_GetAttachment`, `FPDFAttachment_GetName`, `FPDFAttachment_GetFile`) y no por representación cruda del stream.

Resultado:

- build: **0 warnings / 0 errors**;
- tests: **742 PASS / 9 FAIL / 751 total**;
- la prueba estructural combinada completa **PASS**;
- los nueve fallos restantes quedaron limitados exclusivamente al producto preflight:
  - ocho findings aún declaraban evidencia de `writer F6`;
  - `PageRotation` todavía no existía como finding.

Este es el RED válido de producto para Task 11.

## GREEN funcional

Commit: `65a2f23a604e4013ca4b11b3248374de43a202ce`  
Workflow: `38087673453`  
Job: `114317472667`

Cambios mínimos:

1. se añadió `PdfEditFindingKind.PageRotation`;
2. el inspector detecta cualquier página con rotación no cero;
3. las estructuras demostradas se describen como preservadas por el writer combinado de EDITAR/F7;
4. el mensaje de firma criptográfica dejó de referirse únicamente a edición de imágenes y ahora aplica al EDITAR compartido.

Resultado exacto:

- repository hygiene: PASS;
- Labelize 1.7.0 staging: PASS;
- locked restore: PASS;
- Release build: **0 warnings / 0 errors**;
- tests: **751 PASS / 0 FAIL / 0 skipped**;
- workflow: **SUCCESS**.

## Auditoría de alcance

Diff Task 11 desde Task 10 antes de este checkpoint:

### Producto

1. `src/SGPdf.App/Features/Edit/PdfEditPreflight.cs`

### Tests

2. `tests/SGPdf.App.Tests/PdfEditPreservationF7Tests.cs`

No hubo cambios de:

- `.csproj`;
- lockfiles;
- dependencias/NuGet;
- segundo motor PDF;
- writer combinado;
- output validator Task 10;
- UI de texto;
- OCR/reflow/free-text;
- F8;
- `main`.

## QA manual

**NOT RUN.** Task 11 es un gate estructural automatizado save -> reopen; no se presenta como prueba visual/manual de Windows.

## Estado de integración

- `main` permanece congelado en `31c0594758a83ec555d73ecdd7c597cdf8791fd7`;
- no se hizo merge;
- no se abrió PR de F7;
- no se inició F8;
- no se inició Task 12.

Task 11 queda **CLOSED / AUTOMATED PASS** una vez que el CI exact-head de este checkpoint confirme nuevamente la suite completa.
