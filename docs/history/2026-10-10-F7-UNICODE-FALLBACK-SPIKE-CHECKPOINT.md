# F7 — Unicode fallback spike — CHECKPOINT

**Fecha:** 2026-10-10  
**Fase:** F7 — Texto V1  
**Estado:** **SPIKE PASS / bloqueo técnico resuelto; spec todavía pendiente de revisión**  
**Rama:** `feat/f7-text-v1`  
**Base del spike:** `d798e90e6fbfc7dd860513a69587c84aea1114e2`  
**Head funcional del spike:** `c34f4f578831d1321b8c5958672c63341e7a06ce`

---

## 1. Pregunta del spike

¿El `pdfium.dll` pinneado por `bblanchon.PDFium.Win32 156.0.8076` ofrece una ruta nativa, distinta de `FPDFText_LoadFont`, que permita incrustar una TTF con mapeo Unicode explícito y reabrir exactamente texto español como `NIÑO áé`?

El spike fue autorizado como investigación test-only. No incluye código de producto, dependencias nuevas ni inicio de Task 2.

---

## 2. Resultado

**PASS.**

El DLL pinneado exporta y ejecuta correctamente:

- `FPDFText_LoadCidType2Font`
- `FPDFPageObj_CreateTextObj`
- `FPDFText_SetText`
- `FPDFFont_Close`

La ruta probada suministra al runtime:

1. bytes TTF locales;
2. `ToUnicode CMap` explícito;
3. `CIDToGIDMap` explícito;
4. glyph IDs reales de la TTF para los caracteres del texto.

Para el fixture `NIÑO áé`, el spike genera un mapa mínimo de CIDs, crea un nuevo objeto de texto, copia tamaño/color/matriz, lo inserta en el mismo `PageObjectIndex`, genera contenido, guarda, reabre y vuelve a extraer el texto.

Resultado después de reopen:

`NIÑO áé`

**Unicode exacto: PASS.**

Render reabierto válido: **PASS.**

---

## 3. Diferencia frente a las rutas rechazadas

### `FPDFText_LoadFont(..., cid: 1)`

Rechazada previamente porque el mapeo autogenerado convertía U+0020 SPACE en U+00A0 NBSP.

### `FPDFText_LoadFont(..., cid: 0)`

Rechazada previamente porque corrompía caracteres españoles (`ñ/á/é` → `ÿ`).

### `FPDFText_LoadCidType2Font(...)`

Aceptada por el spike porque `ToUnicode` y `CIDToGIDMap` son provistos explícitamente por SG PDF en vez de confiar en el mapeo autogenerado de PDFium.

No se normaliza texto después de extraerlo y no se oculta ninguna diferencia Unicode.

---

## 4. Implementación test-only del spike

Archivo añadido:

- `tests/SGPdf.App.Tests/TextEditExplicitUnicodeSpikeTests.cs`

El test:

- usa una TTF instalada en Windows únicamente como fixture efímero;
- usa SkiaSharp ya presente en tests para resolver glyph IDs;
- construye un `ToUnicode CMap` mínimo;
- construye un `CIDToGIDMap` big-endian;
- prueba el DLL real pinneado;
- no añade código a `src/`;
- no añade paquetes;
- no convierte la fuente Windows en dependencia de producto.

---

## 5. CI exacto

Run: `38069275620`  
SHA: `c34f4f578831d1321b8c5958672c63341e7a06ce`

Resultado:

- hygiene: PASS;
- Labelize staging: PASS;
- locked restore: PASS;
- Release build: PASS;
- warnings: **0**;
- errors: **0**;
- tests: **687 passed / 0 failed / 0 skipped**.

---

## 6. Recomendación de diseño

La estrategia recomendada para F7 queda:

1. **Fuente original** para edición conservadora cuando los code points nuevos ya son demostrablemente seguros.
2. **Fallback TTF controlado** usando `FPDFText_LoadCidType2Font` con `ToUnicode` + `CIDToGIDMap` generados explícitamente.
3. No usar `FPDFText_LoadFont` como fallback Unicode de producto.
4. Mantener PDFium como único motor PDF.
5. Mantener Unicode exacto como requisito; no introducir normalizaciones silenciosas.

Esto conserva la arquitectura KISS original y evita un segundo motor PDF.

---

## 7. Lo que aún NO está aprobado

Este PASS técnico **no autoriza automáticamente Task 2**.

Antes de implementar producto se debe:

1. revisar la spec F7 para sustituir la ruta fallback genérica por la ruta CID Type2 explícita;
2. decidir cómo generar `CIDToGIDMap` en producto sin depender de SkiaSharp test-only ni añadir arquitectura excesiva;
3. mantener el Gate legal/provenance de la TTF redistribuible;
4. actualizar el plan TDD afectado;
5. obtener aprobación de la spec revisada antes de reanudar implementación.

No se ha añadido DejaVu Sans ni otra fuente al runtime.

---

## 8. Gobernanza

- No se tocó código de producto.
- No se inició Task 2.
- No se añadió un segundo motor PDF.
- No se hizo merge.
- `main` permanece fuera de esta investigación.

**Siguiente paso permitido:** revisión escrita de la spec F7 incorporando esta ruta de fallback explícito.
