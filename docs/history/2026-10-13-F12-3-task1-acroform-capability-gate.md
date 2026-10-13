# F12.3 Task 1 — AcroForm capability gate

Fecha: 2026-10-13

## Resultado

**GO — AUTO PASS** para continuar únicamente con el soporte de campos de texto AcroForm clásicos existentes.

## Evidencia final

SHA exacto del gate: `8dd08032efd5d917c7f344ee834c56c9c8cd9bc5`

GitHub Actions:
- run: `38358408777`
- job Windows: `115166590262`
- repository hygiene: PASS
- locked restore: PASS
- Release build: PASS
- warnings: `0`
- errors: `0`
- tests: `1151/1151 PASS`
- skipped: `0`

## Qué quedó probado

1. El PDF fuente queda byte-for-byte intacto.
2. PDFsharp 6.2.4 abre una copia modificable del PDF clásico AcroForm.
3. El campo de texto existente se localiza por nombre.
4. El valor `/V` cambia y persiste al reabrir.
5. El appearance stream normal `/AP/N` también cambia y contiene el valor actualizado; no es un éxito de `/V` solamente.
6. PDFium sigue detectando el documento como formulario.
7. El número de páginas permanece estable y el PDF de salida reabre/renderiza normalmente.
8. No se añadió dependencia ni archivo de producción durante el gate.

## Hallazgo de render

`PdfDocumentSession.RenderPage()` usa `FPDF_RenderPageBitmap(..., flags: 0)`. Ese render de página no dibuja widgets AcroForm interactivos; PDFium requiere el módulo FormFill y `FPDF_FFLDraw` para dibujarlos. Por eso la comparación inicial de píxeles del rectángulo del widget era un falso negativo. El gate final valida directamente el `/AP/N` persistido y mantiene el render normal como prueba de integridad del PDF.

## Límite del GO

Este GO **no** habilita todavía checkbox, radio, combo/list, XFA, JavaScript, cálculos, campos de firma, flatten ni diseñador de formularios. Task 2 debe inspeccionar únicamente lo mínimo necesario para campos de texto existentes y continuar con Ley del Ruido.
