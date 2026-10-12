# F10 Task 4 — Raster 300 DPI, TSV estricto y geometría OCR

Fecha: 2026-10-11  
Rama: `feat/f10-local-ocr`  
Estado: **TASK 4 AUTOMATED GREEN / CLOSED**

## Alcance cerrado

Task 4 implementa únicamente el pipeline previo al writer OCR:

1. rasterización PDFium a 300 DPI con límites de seguridad antes de asignar el bitmap;
2. parsing TSV estricto de Tesseract con UTF-8 válido, filas nivel 5 y geometría validada;
3. mapeo de rectángulos OCR desde píxeles del render a coordenadas PDF mediante el `DeviceTransform` derivado de PDFium.

No se implementa todavía el writer PDF buscable, el validador semántico, el workflow OCR completo ni UI.

## 1. Raster OCR — RED → GREEN

### RED

Commit: `4f18c4c6282078bc5582e4e787b2f81afc27d7aa`  
Objetivo: fijar 300 DPI y límites de 20.000 px por eje / 100.000.000 píxeles totales antes de cualquier asignación grande.

El workflow Windows falló en tests como se esperaba, con build válido, porque `OcrRasterizer` aún no existía.

### GREEN

Commit: `455cd0526143df9a1add514193042f9387f4ccd8`  
Workflow: `38160869094`  
Job: `114533315700`

Resultado:
- repository hygiene PASS;
- Labelize staging PASS;
- qpdf staging PASS;
- Tesseract staging PASS;
- locked restore PASS;
- build PASS;
- tests PASS.

Contrato implementado:
- DPI fijo: 300;
- máximo por eje: 20.000 px;
- máximo total: 100.000.000 píxeles;
- rechazo antes de `RenderPage` cuando el tamaño excede los límites;
- sin reducción silenciosa de DPI;
- render página por página reutilizando `PdfDocumentSession.RenderPage`.

## 2. Parser TSV — RED → GREEN

### RED

Commit: `1ab89ad032bace7fe7e4c4e1db0170c1775e4df2`

El RED cubrió Unicode español, header TSV exacto, filas estructurales, geometría inválida, confianza no finita, texto nivel 5 vacío y UTF-8 inválido.

### Implementación y correcciones de harness

- modelos OCR: `c5e660e6e2b18f5fb3e6e4360b2e4246c0f24b34`;
- parser inicial: `128b7ee0a2fab33a1a27921e64f27ee8f69556e7`;
- import `System.IO`: `5360e393c75ea5e8d914a297e19e51c41d55632f`;
- correcciones de harness/reflexión: `ee8c6cd959e8687b4ce8d23215c85f7a23031528` y `dbfaaf7a38cbd17e906feb479f92ae540621d608`.

### GREEN exacto

Commit: `dbfaaf7a38cbd17e906feb479f92ae540621d608`  
Workflow: `38161771606`  
Attempt 2 job: `114536445866`

Resultado:
- build: **0 warnings / 0 errors**;
- tests: **982 PASS / 0 FAIL / 0 SKIP**.

El attempt 1 del mismo SHA tuvo un fallo WPF ajeno a OCR (`MainWindowOrganizeThumbnailTests.OneTileRenderFailure...`). El rerun del mismo SHA terminó verde sin modificar código OCR, por lo que se clasifica como flake externo al Task 4.

Contrato implementado:
- lectura UTF-8 estricta con bytes inválidos rechazados;
- header estándar de 12 columnas obligatorio;
- niveles estructurales 1–4 ignorados;
- sólo filas nivel 5 producen `OcrWord`;
- parsing numérico con cultura invariante;
- geometría válida obligatoria;
- confianza finita, incluyendo `0` como valor válido;
- texto OCR no vacío;
- sin umbral de confianza en F10 V1.

## 3. Coordenadas OCR → PDF — RED → GREEN

### RED limpio

Commit: `6a7dbd4069b07881fadc5e2cbec17ad0faa12e5a`  
Workflow: `38162170152`  
Job: `114537190398`

Resultado RED:
- build: **0 warnings / 0 errors**;
- pruebas existentes: **982 PASS**;
- nuevas pruebas de coordenadas: **10 FAIL** porque `OcrCoordinateMapper` todavía no existía.

Cobertura RED:
- retrato / 0° equivalente;
- paisaje;
- transformaciones equivalentes a 90°, 180° y 270°;
- precisión `1e-6` unidades PDF;
- rechazo de rectángulos fuera del dispositivo;
- rechazo de páginas renderizadas sin `DeviceTransform`.

### Implementación

- `PdfOcrRect`: `0dc8bd6a1bdc5ca45c7d5458197cd57ed0d7b4b7`;
- mapper affine: `a4016b9726ea0e916845a6a980b82e9bf806522f`.

El primer CI GREEN candidato en `a4016b...` compiló 0/0, pero las 10 pruebas no alcanzaron el mapper porque el harness intentaba crear un constructor `internal` con `Activator.CreateInstance` público. No se cambió producción para ocultar ese fallo de test.

Corrección exclusiva del harness: `98da53ab504066475108b64046456277728209de`.

### GREEN final exacto

Commit: `98da53ab504066475108b64046456277728209de`  
Workflow: `38162517540`  
Job: `114538224406`

Resultado:
- repository hygiene PASS;
- Labelize 1.7.0 staging PASS;
- qpdf 12.4.2 staging PASS;
- Tesseract 5.5.3 staging PASS;
- locked restore PASS;
- build: **0 warnings / 0 errors**;
- tests: **992 PASS / 0 FAIL / 0 SKIP**;
- duración de tests: 29 s.

El único ruido fue la advertencia deprecatoria Node.js/punycode de GitHub Actions, no una advertencia C# del producto.

El mapper:
- usa únicamente `PdfRenderedPage.DeviceTransform`;
- aplica la transformación affine a las cuatro esquinas del rectángulo OCR;
- deriva `Left/Bottom/Right/Top` por mínimos y máximos;
- no contiene heurísticas manuales de orientación;
- no llama Tesseract ni APIs nativas PDFium;
- rechaza rectángulos fuera del bitmap o resultados no finitos/degenerados.

## Salidas Task 4

Quedan automatizadas y verificadas:
- raster OCR seguro a 300 DPI;
- parser TSV estricto y determinista;
- geometría OCR en píxeles;
- transformación OCR → coordenadas PDF compatible con rotación mediante el transform ya derivado por PDFium.

## Fuera de alcance / todavía NO RUN o no implementado

- writer de capa invisible buscable: **NO IMPLEMENTADO**;
- publicación transaccional Save As OCR: **NO IMPLEMENTADA**;
- validador semántico OCR: **NO IMPLEMENTADO**;
- workflow OCR completo: **NO IMPLEMENTADO**;
- UI OCR: **NO IMPLEMENTADA**;
- PDF final generado por Tesseract: **NO SE USA**;
- QA manual Windows con UX real: **NOT RUN**;
- Adobe Reader / Foxit / Edge: **NOT RUN**;
- corpus privado de escaneos reales: **NOT RUN**.

## Gate de salida

**TASK 4 AUTOMATED GREEN / CLOSED.**

El siguiente bloque es Task 5, comenzando por el planner OCR RED antes de tocar el writer transaccional.
