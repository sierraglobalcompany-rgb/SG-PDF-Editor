# SG PDF Editor — Contexto maestro

## AUTORIDAD VIGENTE — 2026-10-17

Este documento conserva **arquitectura y decisiones durables**. El estado operativo diario no se deduce de snapshots históricos: usar primero GitHub exact HEAD/CI → `.planning/STATE.md` → `.planning/ROADMAP.md`.

Estado resumido:
- F0–F12: CLOSED / FROZEN para la baseline automatizada ya documentada;
- F13: ACTIVE;
- Task9 / T9.1: ACTIVE;
- último anchor GREEN antes del R1 document-shell: `0abae7ceb5f08611180c883346e21589d1509664`, CI run `39000181654` SUCCESS;
- R1 multidocumento actual: RED / INCOMPLETE en `bbae51f49ee3c54e3667c5ea477c2c3f179d3c46`;
- DG-0 SG Design Governance se valida aislado antes de retomar R1;
- `main` continúa read-only en `31c0594758a83ec555d73ecdd7c597cdf8791fd7`;
- no hay merge aprobado.

La autoridad de diseño está en `docs/design/SGPDF_DESIGN_LAW.md` y la skill obligatoria para UI/UX en `.agents/skills/sgpdf-design/SKILL.md`.

---

## 1. Producto

SG PDF Editor es una aplicación Windows local-first/offline para dos trabajos diarios:

1. **PDF:** leer, navegar, imprimir, firmar, organizar, editar de forma conservadora, comentar y ejecutar utilidades profesionales aprobadas.
2. **Etiquetas térmicas:** abrir ZPL/TXT/PRN, previsualizar, respetar cantidades, maquetar, exportar PDF e imprimir sin depender de Internet.

No pretende ser un clon total de Acrobat. Se construye por vertical slices útiles, verificables y mantenibles.

## 2. Reglas no negociables

### KISS
`SIMPLE → EFICIENTE → OPTIMIZADO → EFICAZ → MANTENIBLE → A→B`.

### Anti-ruido
`DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.

### Ley SG de Diseño
`INTENT → HIERARCHY → DISCLOSURE → CONSISTENCY → FEEDBACK → ACCESSIBILITY → VERIFY`.

Toda UI debe partir de la tarea humana y aplicar progressive disclosure. No se acepta el patrón de “Excel infinito” donde todas las propiedades se muestran simultáneamente porque existen en el modelo.

### Guardrail anti-parches
Flujo separado para defectos:
`REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.

No renombrar este guardrail como una tercera ley numerada.

### Producto / datos
- Windows x64 inicialmente.
- C# + .NET 10 + WPF.
- Funciones principales offline.
- Sin API keys, SaaS, cuentas, servidores, créditos ni activación obligatoria.
- Sin backend/base de datos para el baseline actual.
- No licencia comercial obligatoria runtime.
- No AGPL/GPL fuerte sin aprobación explícita.
- Preservar originales y usar `Guardar como`/outputs separados donde la baseline lo define.
- Datos reales de Mercado Libre/clientes no se versionan ni entran a CI/Graphify.
- No merge automático a `main`.

## 3. Arquitectura runtime aprobada

```text
SG PDF Editor
│
├─ WPF + .NET 10
│
├─ PDFium
│  ├─ lectura/render
│  ├─ texto/objetos
│  ├─ firma visual
│  ├─ edición práctica
│  └─ organización de páginas
│
├─ Labels / ZPL
│  ├─ parser managed
│  ├─ cantidades ^PQ como metadata
│  ├─ layout/orquestación
│  └─ Labelize 1.7.0 local sidecar
│
├─ PDFsharp 6.2.4
│  └─ usos estrechos ya aprobados; no motor PDF general
│
├─ qpdf 12.4.2
│  └─ sidecar estrecho para protección AES aprobada
│
├─ Tesseract
│  └─ OCR local/offline
│
└─ Windows Printing
```

### Decisiones durables

- **PDFium** es el motor PDF primario.
- Toda llamada nativa PDFium se serializa mediante `PdfiumRuntime.NativeGate`; no se trata PDFium como thread-safe.
- **Labelize 1.7.0** es el único renderer ZPL runtime aprobado, ejecutado como proceso local pinneado.
- BinaryKits es histórico; no fallback runtime.
- No Labelary ni HTTP/local server para render ZPL.
- PDFsharp no se convierte en un segundo motor PDF general.
- qpdf permanece sidecar estrecho; no motor general.
- ZXing.Net permanece tests/QA-only.
- DejaVu Sans sigue permitido donde la baseline pinneada lo usa.
- No segundo viewer, plugin framework, generalized ModeManager, conversion framework genérico o BatchEngine genérico sin decisión explícita.

## 4. Solución KISS

Mientras sea suficiente:

```text
src/SGPdf.App/
tests/SGPdf.App.Tests/
```

No recrear capas `Core`, `Infrastructure`, `Domain`, `Application`, CQRS/event bus/DI complejo por prevención.

La separación por archivos/feature se usa solo para mantener owners comprensibles. Un partial existente puede seguir siendo el owner correcto cuando evita arquitectura adicional.

## 5. PDF / seguridad de escritura

- Preservar source/original cuando el contrato define Save As.
- Validar outputs de forma independiente donde la capacidad lo exige.
- No prometer preservación universal de firmas, formularios, tagging, metadata o compatibilidad externa sin evidencia representativa.
- Las funciones que modifican contenido deben respetar los guards/transactional patterns ya cerrados en sus slices.
- Firma visual y firma criptográfica son capacidades distintas.

## 6. ZPL / etiquetas

Entradas aprobadas:
- `.zpl`;
- `.txt` con ZPL;
- `.prn` con ZPL.

`^PQ` representa cantidad de salida; no multiplica innecesariamente trabajo de render por diseño.

Labelize 1.7.0 permanece el renderer oficial local. El flujo general sigue siendo:

```text
ZPL local
→ parser/documento normalizado
→ LabelizeProcessRenderer
→ output temporal local
→ preview/layout/export/print
→ cleanup
```

El runtime no descarga Labelize ni usa red.

### UX ZPL durable

El panel de propiedades no debe presentar permanentemente todas las opciones de cantidad, tamaño, resolución, medio, hoja, filas, columnas, rotación, márgenes y gaps en una lista plana.

Aplicar progressive disclosure:
- cantidad personalizada solo cuando se selecciona;
- tamaño personalizado solo cuando se selecciona;
- opciones de hoja solo cuando la salida utiliza hoja;
- filas/columnas solo para layout custom;
- márgenes/gaps/rotación avanzada solo cuando aportan al flujo actual;
- mantener capacidad sin saturar la visión.

Detalle: `.agents/skills/sgpdf-design/references/properties-panels.md`.

## 7. Autoridad visual

Roles separados:

- **App/software icon:** identidad de EXE/titlebar/taskbar/Alt-Tab.
- **PDF document icon:** identidad de archivo/documento; asociación Windows futura solo mediante instalador aprobado.
- **15 Home tool icons:** arte oficial por herramienta.

No reinterpretar estos activos durante cambios de UX no relacionados.

`PDF a PNG` continúa siendo el contrato funcional actual aunque el arte aprobado comunique genéricamente PDF→imagen. No anunciar JPG hasta que esa capacidad exista.

## 8. Ley SG de Diseño aplicada a WPF

Para cualquier menú, barra, rail, Home, properties panel, diálogo, formulario, copy, icono visible, layout o estado:

1. definir INTENT;
2. establecer HIERARCHY;
3. decidir DISCLOSURE;
4. REUSE/CONSISTENCY con recursos/owners existentes;
5. diseñar FEEDBACK;
6. verificar ACCESSIBILITY;
7. VERIFY con evidencia real.

Gate mínimo de cierre visual:
- Light + Dark;
- 125% Windows scaling;
- teclado/tab/foco;
- estados aplicables;
- sin clipping/scroll horizontal accidental;
- copy y unidades coherentes;
- tests aplicables + full suite + exact-head CI;
- QA manual/hardware permanece NOT RUN si no fue ejecutado.

No introducir framework UI nuevo para lograr polish que WPF/resources/templates existentes pueden resolver.

## 9. Funcionalidad profesional cerrada hasta F12

La baseline histórica contiene, entre otros:
- safe redaction conservadora;
- firma PFX/P12 dentro del alcance documentado;
- AcroForm clásico `/Tx` dentro del alcance documentado;
- Compare local mínimo;
- Batch `TextWatermark` únicamente;
- QR PNG únicamente;
- PDF→PNG, todas las páginas, 144 DPI fijo en su MVP cerrado.

Ampliaciones de estas capacidades requieren un nuevo gate explícito; no extender por conveniencia desde tareas visuales.

## 10. QA y semántica de evidencia

- AUTO/CI PASS demuestra únicamente lo ejecutado por automatización.
- Windows-real, hardware, private corpus, impresora/scanner, stylus/touch, PFX real, lector externo, offline físico y compatibilidad externa requieren evidencia separada.
- Usar `NOT RUN` o `BLOCKED — ENVIRONMENT` cuando corresponda; nunca inferir PASS.

## 11. Estado F13 / continuidad

### Cerrado antes de Task9
- F13 Tasks 1–8: cerradas por sus checkpoints.

### Task9
- T9.0: DONE.
- T9.1: ACTIVE.
- T9.2: NOT STARTED.

Anchor GREEN pre-R1:
`0abae7ceb5f08611180c883346e21589d1509664` — CI run `39000181654` SUCCESS.

R1 congelado/incompleto durante DG-0:
`feat/f13-task9-t9-1-document-shell-realignment-20261017` @ `bbae51f49ee3c54e3667c5ea477c2c3f179d3c46`.

DG-0 existe para que el resto de T9.1 se diseñe bajo una autoridad UX estable en lugar de seguir agregando opciones/paneles sin jerarquía.

## 12. Reanudación correcta

1. terminar DG-0 y dejar checkpoint GREEN;
2. reanudar R1 desde su checkpoint sin perder los contratos RED existentes;
3. aplicar la design skill a cada superficie UI afectada;
4. completar T9.1 con sus gates automáticos + visuales;
5. T9.2 solo después del cierre real de T9.1;
6. F14–F18 permanecen frozen;
7. `main` solo con aprobación explícita.

## 13. Historia y detalle

Los detalles de slices cerrados, preservation matrices, release packs, decisiones y experimentos siguen bajo `.planning/REQUIREMENTS.md`, `docs/history/**`, `docs/release/**` y planes/specs de `docs/superpowers/**`.

Esos documentos sirven como evidencia histórica/específica; no deben devolver el ruteo operativo a F12 cuando STATE y GitHub muestran F13 activo.
