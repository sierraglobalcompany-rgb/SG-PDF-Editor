# F10 — Local OCR Preservation Matrix

**Fecha local:** 2026-10-11  
**Branch:** `feat/f10-local-ocr`  
**Scope:** corpus híbrido sintético determinista de 2 páginas. Esta matriz demuestra únicamente lo medido; no afirma preservación universal para todo PDF posible.

## Resultado

| Invariante | Evidencia F10 | Resultado | Observación |
|---|---|---|---|
| Source original | bytes antes/después | PASS | fuente no mutada |
| Page count | 2 → 2 | PASS | |
| Página OCR elegible | `OCR-F10` searchable | PASS | mutación esperada |
| Texto de página omitida | `SEARCHABLE-SKIP` exacto | PASS | sin OCR duplicado |
| Render del scan | 72 DPI, dimensiones/stride/pixels | PASS | pixel-identical en fixture |
| Image XObject | `/Subtype /Image` | PASS | presente post-save |
| Image decoded payload | qpdf `--show-pages --with-images` + `--show-object=... --filtered-stream-data`, source→output | PASS | comparación binaria decodificada exacta |
| AcroForm | `/AcroForm` | PASS | |
| Widget / field | `/FT /Tx` | PASS | |
| Bookmark | `F10 Bookmark` | PASS | |
| Named destination | `F10Dest` | PASS | |
| Internal link | `/Subtype /Link` | PASS | |
| Tagged structure | `StructTreeRoot` + `MarkInfo` | PASS | |
| Page labels | `/PageLabels` | PASS | |
| Attachment | `fixture.bin` + payload decodificado | PASS | payload exacto mediante qpdf |
| Metadata title | `F10 Preservation Corpus` | PASS | |
| Metadata author | `SG PDF` | PASS | |
| Rotation | 90° | PASS | |
| Supported annotation | `Text` / `SUPPORTED-ANNOTATION-F10` | PASS | |
| Unsupported representative annotation | `Polygon` / `UNSUPPORTED-ANNOTATION-F10` | PASS | preservada |

## Evidencia durable

- Preservation GREEN funcional: `3993a33b557db91a6f1837e99ea50a129095973b`.
- Preservation checkpoint: `1598a948ecbfe9a19c76ec1f56acd9ad2ba196bf`.
- Exact-head CI del checkpoint: run `38204535141`, job `114664976057`, Release build 0 warnings / 0 errors, suite completa GREEN.
- Micro-gate de payload de imagen: `aba0228507055a3a9486060f6cbb076aafdc4643`.
- Exact-head CI del micro-gate: run `38206127428`, job `114669898651`; repository hygiene, Labelize, qpdf, Tesseract, locked restore, Release build y test completo PASS.

El micro-gate solo fortaleció `PdfOcrPreservationTests`; no modificó `PdfOcrWriter` ni otro código productivo. El hallazgo fue un hueco de evidencia, no un defecto demostrado del producto.

## Límite de evidencia

`LOCAL DIRECT RUN = NOT SEPARATELY EVIDENCED` en este cierre. La evidencia ejecutada y observada aquí es Windows GitHub CI exact-head. No se promueve CI a QA manual.

Siguen **NOT RUN**: interacción OCR real en Windows, escaneos reales de baja calidad, Adobe Acrobat Reader, Foxit, Edge, corpus privado, hardware representativo y prueba física con red deshabilitada.
