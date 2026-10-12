# F10 Task 8 — Checkpoint de preservación GREEN

**Fecha:** 2026-10-12  
**Branch:** `feat/f10-local-ocr`  
**Estado:** CHECKPOINT — PRESERVATION GREEN / CIERRE F10 AÚN PENDIENTE  
**Main:** NO TOCADO

## Estado exacto al parar

Task 7 permanece CLOSED/GREEN.

Task 8 avanzó hasta una matriz de preservación automatizada real sobre `PdfOcrWriter`, sin cambios productivos porque la evidencia mostró que el writer ya preserva correctamente el corpus representativo.

Archivo de prueba añadido:

- `tests/SGPdf.App.Tests/PdfOcrPreservationTests.cs`

El corpus híbrido sintético de 2 páginas verifica:

- página escaneada con OCR nuevo;
- página con texto nativo omitida y texto preexistente preservado;
- fuente original byte-a-byte intacta;
- render del escaneo preservado;
- AcroForm;
- bookmarks/outlines;
- destinos nombrados;
- links internos;
- tagged structure / `StructTreeRoot` / `MarkInfo`;
- page labels;
- attachment por nombre y payload decodificado exacto;
- title/author metadata;
- page rotation;
- anotación soportada representativa;
- anotación no soportada representativa preservada;
- XObject de imagen preservado.

## Ciclo TDD / evidencia

### RED 1

Commit:

`f807a2f8dd4ffe25ca96aba78f07d6c680a549ff`

Resultado esperado encontrado: la prueba falló porque comparaba sintaxis PDF con espacios literales y PDFium compactó tokens, por ejemplo `/FT/Tx` en vez de `/FT /Tx`.

Esto fue un defecto del harness, no una pérdida productiva.

### RED 2 / corrección de harness

Commit:

`241e997ed909bdea02b475dcd424d934cd217484`

Se normalizó whitespace de la sintaxis PDF. La suite posterior mostró que la búsqueda de bytes crudos del attachment tampoco era una medición válida porque PDFium puede recomprimir streams.

También reapareció el flaky histórico de thumbnails ya conocido; no se modificó código ajeno a F10.

### GREEN final de preservación

Commit:

`3993a33b557db91a6f1837e99ea50a129095973b`

Cambio: el attachment pasó a verificarse semánticamente mediante extracción binaria decodificada con el qpdf 12.4.2 ya pinneado y stageado por CI.

CI exact-head:

- workflow run: `38204356414`
- job: `114664430810`
- Release build: **0 warnings / 0 errors**
- tests: **1056 passed / 0 failed / 0 skipped**
- repository hygiene: PASS
- Labelize 1.7.0 staging: PASS
- qpdf 12.4.2 staging: PASS
- Tesseract 5.5.3 staging: PASS
- locked restore: PASS

Conclusión técnica: **no se requiere cambio productivo en `PdfOcrWriter` para esta matriz representativa**.

## Scope audit ya confirmado

Base F9→F10 usada para auditoría:

`222454f5782f84a929c7fd610c284d15df8f869d`

Head funcional GREEN actual antes de este checkpoint:

`3993a33b557db91a6f1837e99ea50a129095973b`

Comparación observada:

- 116 commits ahead / 0 behind;
- sin cambio de `package-lock` / segundo stack web;
- OCR local/offline;
- Tesseract pinneado;
- PDFium continúa como owner PDF principal;
- qpdf sigue siendo el sidecar ya aprobado de F9 y sólo se reutiliza en test para validar payload del attachment;
- sin cloud/backend/database/accounts/API keys;
- sin merge a `main`.

## Drift documental detectado y todavía NO corregido

`.planning/STATE.md` todavía describe F9 como foco actual y 77%.

`.planning/REQUIREMENTS.md` todavía deja OCR sin marcar.

No se corrigieron todavía porque este checkpoint se creó antes del bloque de cierre documental final de F10.

## Próximo paso exacto al reanudar

1. Reconciliar `STATE`, `REQUIREMENTS` y documentación de fase F10 con la evidencia real de Tasks 1–8.
2. Crear la matriz/documento final de preservación F10 y cierre Task 8.
3. Ejecutar auditoría final de alcance F9→F10.
4. Ejecutar CI Windows exact-head sobre el commit documental final.
5. Crear/mantener PR apilado draft F9→F10, verificar open/draft/unmerged.
6. Verificar `main` sin cambios.
7. STOP F10.

## Gates manuales todavía abiertos

- Windows UX OCR real: **NOT RUN**.
- Adobe/Foxit/Edge interoperability OCR: **NOT RUN**.
- corpus privado de documentos escaneados: **NOT RUN**.
- prueba manual con documentos reales y calidad OCR español/inglés: **NOT RUN**.

Este checkpoint no declara F10 cerrado; declara únicamente **Task 8 preservation GREEN y listo para cierre documental final**.
