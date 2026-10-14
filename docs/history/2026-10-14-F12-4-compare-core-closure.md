# F12.4 — Compare: capability gate y cierre del núcleo mínimo

Fecha: 2026-10-14  
Rama: `feat/f12-4-task2-compare-core`  
Base del núcleo después del capability gate: `665bdcbbc8371eb9680c0c3e5065e497607be346`  
HEAD productivo auditado antes de este cierre documental: `d95f4507e23553be9f674e6ee62a0e487ba1f71d`

## Estado

- **Task 1 — capability gate:** `GO / CLOSED`.
- **Task 2 — núcleo mínimo de comparación:** `CLOSED / GREEN`.
- **F12.4 completo:** `OPEN`.
- **Siguiente etapa:** UI/flujo mínimo de Compare `READY / NOT STARTED`.

No se ha realizado merge a `main`.

## Declaración segura del núcleo

> SG PDF puede comparar dos PDFs localmente con PDFium existente y distinguir, dentro del alcance probado, documentos idénticos, cambios de texto, cambios exclusivamente visuales y diferencias estructurales por cantidad de páginas.

Esta declaración corresponde únicamente al núcleo automatizado probado. No implica diff semántico universal, detección OCR automática, emparejamiento inteligente de páginas ni compatibilidad universal con cualquier PDF.

## Task 1 — capability gate

El gate se ejecutó sin código productivo y sin dependencias nuevas, reutilizando capacidades ya existentes de `PdfDocumentSession`:

- `PageCount`;
- `GetTextObjects()`;
- `RenderPage()` con PDFium.

Casos demostrados en el capability gate:

1. PDFs idénticos → sin diferencia textual ni visual;
2. texto modificado → señal textual detectable;
3. gráfico modificado → señal visual detectable sin falso cambio textual;
4. distinta cantidad de páginas → conteos estructurales detectables.

Resultado: **GO sin segundo motor PDF ni paquete nuevo**.

## Task 2 — núcleo mínimo

Producción añadida:

- `src/SGPdf.App/Features/Compare/PdfCompareService.cs`

Pruebas añadidas:

- `tests/SGPdf.App.Tests/PdfCompareServiceTests.cs`

Contrato actual de `PdfCompareResult`:

- `LeftPageCount`;
- `RightPageCount`;
- `TextChanged`;
- `VisualChanged`.

Comportamientos cerrados:

1. **Idéntico**  
   Conteos iguales, `TextChanged=false`, `VisualChanged=false`.

2. **Texto-only**  
   `TextChanged=true`; el mismo cambio no se doble contabiliza como `VisualChanged`.

3. **Visual-only**  
   Con texto equivalente y render distinto en páginas comunes: `TextChanged=false`, `VisualChanged=true`.

4. **Estructura por page count**  
   Las diferencias de cantidad de páginas quedan representadas por `LeftPageCount/RightPageCount`. El contenido se compara únicamente en las páginas comunes para no convertir una página añadida o eliminada en falso cambio textual o visual-only.

## Semántica deliberada de `VisualChanged`

En este núcleo, `VisualChanged` significa **cambio visual independiente del texto (visual-only)**.

Si ya existe un cambio textual, el núcleo no intenta además separar un segundo cambio gráfico simultáneo. Esa ampliación no es requisito del contrato actual y no debe añadirse sin una necesidad concreta de la siguiente capa.

Esto evita introducir heurísticas, clasificación adicional o arquitectura prematura.

## Fuera de alcance / NOT RUN

- UI de Compare;
- navegación entre diferencias;
- overlay/heatmap visual;
- OCR automático para PDFs escaneados;
- diff semántico tipo Word;
- reordenamiento o emparejamiento inteligente de páginas;
- comparación profunda de firmas, metadata o objetos PDF;
- exportación de reporte PDF;
- persistencia de comparaciones;
- backend, nube, IA o telemetría;
- OpenCV, ImageMagick o segundo motor PDF;
- garantía universal para cualquier PDF.

## KISS / Ley del Ruido

Auditoría del núcleo desde `665bdcbbc8371eb9680c0c3e5065e497607be346` hasta `d95f4507e23553be9f674e6ee62a0e487ba1f71d`:

- 8 commits incrementales de RED/GREEN/auditoría;
- diff funcional concentrado en **2 archivos**;
- `PdfCompareService.cs`: 67 líneas productivas;
- `PdfCompareServiceTests.cs`: 223 líneas de pruebas;
- sin cambios de `.csproj` ni lockfile;
- sin dependencia nueva;
- sin segundo motor PDF;
- sin XAML/UI;
- sin base de datos, backend, red o telemetría;
- sin framework genérico de diff;
- sin modelo por página, enum o coordinador adicional antes de demostrar necesidad.

Resultado del núcleo: **KISS PASS / Ley del Ruido PASS**.

## Evidencia fresca previa al cierre documental

Rerun exact-head sobre `d95f4507e23553be9f674e6ee62a0e487ba1f71d`:

- workflow run: `38491853828`;
- job fresco: `115620320706`;
- repository hygiene: PASS;
- restore locked: PASS;
- build: 0 warnings / 0 errors;
- tests: 1178/1178 PASS;
- failed: 0;
- skipped: 0.

La única advertencia observada pertenece a GitHub Actions por la transición/deprecación de Node y no al código SG PDF.

## Siguiente etapa READY — no iniciada

La siguiente etapa puede diseñar e implementar una **UI mínima de Compare** reutilizando la shell WPF existente y consumiendo este núcleo.

Guardrails para esa etapa:

- no abrir UI antes de definir un contrato mínimo;
- reutilizar la shell y patrones WPF existentes;
- no introducir segunda sidebar, coordinador genérico de modos ni arquitectura paralela;
- no añadir dependencias para visualización mientras PDFium y WPF existentes sean suficientes;
- mantener la comparación local/offline;
- una sola responsabilidad por micro-gate;
- RED → GREEN mínimo → CI exact-head → auditoría → STOP.

## Regla de cierre exact-head documental

El commit que contiene este documento debe pasar nuevamente el workflow Windows completo. Si ese CI exact-head falla, este checkpoint documental queda bloqueado y no se inicia la UI. Si pasa, Task 1 y Task 2 quedan documentados como cerrados y la UI permanece únicamente `READY / NOT STARTED`.
