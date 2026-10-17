# SG PDF Editor — Contexto maestro

## AUTORIDAD VIGENTE — 2026-10-17

Este documento conserva **arquitectura y decisiones durables**. El estado operativo diario se resuelve en este orden:

1. GitHub exact HEAD + exact-head CI;
2. `.planning/STATE.md`;
3. `.planning/ROADMAP.md`;
4. `.planning/REQUIREMENTS.md`;
5. spec/plan aprobado del slice;
6. para UI/UX: `docs/design/SGPDF_DESIGN_LAW.md` + `.agents/skills/sgpdf-design/SKILL.md`;
7. este MASTER_CONTEXT para decisiones durables;
8. históricos como evidencia/contexto.

Estado resumido actual:
- F0–F12: CLOSED / FROZEN para la baseline automatizada;
- F13: ACTIVE;
- Task9 / T9.1: ACTIVE;
- DG-0 Design Governance: GREEN / CLOSED;
- R1 reader multi-document core: GREEN / CLOSED en `454dcadb6eca13ab98b5d219146dd10ec25af78d`;
- R1B per-document pending state + close guard: GREEN / CLOSED técnicamente en `26d026b2fbe1f35754bcf7cef660d2fdaa7e5d34`, exact-head CI run `39040326178` SUCCESS;
- siguiente bloque elegible: R2 top shell + real document tabs, NOT STARTED en este checkpoint;
- T9.2 NOT STARTED;
- F14–F18 FROZEN;
- `main` read-only; no merge aprobado.

Checkpoint R1B:
`docs/history/2026-10-17-f13-t9-1-r1b-pending-state-green-checkpoint.md`.

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

Decisiones durables:
- PDFium es el motor PDF primario.
- Toda llamada nativa PDFium se serializa mediante `PdfiumRuntime.NativeGate`.
- Labelize 1.7.0 es el único renderer ZPL runtime aprobado.
- BinaryKits es histórico; no fallback runtime.
- No Labelary ni HTTP/local server para render ZPL.
- PDFsharp no se convierte en un segundo motor PDF general.
- qpdf permanece sidecar estrecho; no motor general.
- ZXing.Net permanece tests/QA-only.
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

## 6. Multi-documento

La dirección aprobada para T9.1 es multi-documento real, no tabs decorativos.

Owner mínimo y vigente:

```text
OpenDocuments
ActiveDocument
PdfDocumentWorkspace por PDF
└─ PdfDocumentPendingState
```

Cada `PdfDocumentWorkspace` posee:
- `PdfDocumentSession`;
- page sizes;
- navegación;
- zoom;
- offset vertical;
- estado pendiente por documento mediante `PdfDocumentPendingState`.

R1 reader core quedó GREEN/CLOSED en `454dcadb...`.

R1B quedó GREEN/CLOSED técnicamente en `26d026b2...` y resolvió el requisito previo a tabs visibles:
- cambiar de documento preserva el trabajo pendiente del PDF, sin exigir guardar solo para cambiar de tab y sin descartarlo silenciosamente;
- cierre de un documento dirty tiene guard explícito antes de disposal;
- el pending state conserva **model state**, nunca controles WPF;
- Edit reutiliza image/text/redaction workspaces y selección;
- Sign reutiliza `SignatureEditState`;
- Comments reutiliza `CommentWorkspace` y metadata necesaria;
- Organize reutiliza `OrganizePlan`, dirty y source page sizes;
- AcroForm conserva valores string original/current y reconstruye TextBox al activar el documento.

El cambio de documento sigue la secuencia KISS:
`CAPTURE CURRENT MODEL → DETACH PRESENTATION → BIND TARGET → RESTORE TARGET MODEL/UI`.

La auditoría de cierre detectó `LEGACY_SINGLE_DOCUMENT_GUARD_DRIFT` en rutas de Open PDF. Se eliminaron las pre-guardas de discard para Ctrl+O/Open PDF/Recientes y el owner obsoleto `MainWindow.Comments.Integration.cs`. Se conservaron los guards que sí corresponden a Open ZPL, cambios de modo y cierre de ventana.

No crear un document framework, DI container, event bus o ModeManager. R2 debe reutilizar exactamente estos owners.

## 7. ZPL / etiquetas

Entradas aprobadas:
- `.zpl`;
- `.txt` con ZPL;
- `.prn` con ZPL.

`^PQ` representa cantidad de salida; no multiplica innecesariamente trabajo de render por diseño.

Labelize 1.7.0 permanece el renderer oficial local.

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

## 8. Autoridad visual

Roles separados:
- **App/software icon:** EXE/titlebar/taskbar/Alt-Tab.
- **PDF document icon:** identidad de archivo/documento; asociación Windows futura solo mediante instalador aprobado.
- **15 Home tool icons:** arte oficial por herramienta.

No reinterpretar estos activos durante cambios UX no relacionados.

`PDF a PNG` continúa siendo el contrato funcional actual aunque el arte aprobado comunique genéricamente PDF→imagen. No anunciar JPG hasta que esa capacidad exista.

## 9. Document shell / UX aprobado

Hallazgos abiertos:
- DS-001 DOCUMENT_SHELL — P1 OPEN.
- DS-002 MULTI_DOCUMENT_WORKSPACE — P1 ACTIVE; R1 + R1B GREEN, R2 next.
- DS-003 SHELL_RAIL_PRESENTATION — P1 OPEN.
- DS-004 DOCUMENT_TOOL_RAIL — P1 OPEN.
- DS-005 READER_NAV_DRAWER — P1 OPEN.
- DS-006 BOOKMARKS — P2 AUDIT_REQUIRED.
- DS-007 IMAGE_SELECTION_RENDERING — P1 REPRODUCE_REQUIRED.
- DS-008 PDF_CONTEXT_MENU — P1 OPEN.
- DS-009 TOP_SHELL — P1 OPEN.
- DS-010 EDIT_OBJECT_INTERACTION — P1 OPEN.

Secuencia:

```text
R1 reader multi-document core       ✅
R1B pending state + close guard     ✅
R2 top shell + tabs                 NEXT
→ R3 document rail + approved icons
→ R4 Pages/Bookmarks collapsible drawer
→ R5 context menus
→ R6 image-selection root cause
→ R7 final visual/functional QA
```

R2 debe añadir presentación/interaction de tabs sobre el owner existente, no un segundo registro de documentos o sesiones.

Páginas/Marcadores debe abrir **contraído por defecto** y permitir expand/collapse.

El context menu PDF debe reutilizar capacidades reales; no copiar Adobe indiscriminadamente.

La selección de imagen no debe teñir la imagen completa de azul. El código declara fill transparente; si el tintado visual ocurre, se trata como bug y se reproduce antes de corregir.

## 10. Ley SG de Diseño aplicada a WPF

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
- 125% Windows scaling real cuando se exige;
- teclado/tab/foco;
- estados aplicables;
- sin clipping/scroll horizontal accidental;
- copy y unidades coherentes;
- tests aplicables + full suite + exact-head CI;
- QA manual/hardware permanece NOT RUN si no fue ejecutado.

No introducir framework UI nuevo para lograr polish que WPF/resources/templates existentes pueden resolver.

## 11. Funcionalidad profesional cerrada hasta F12

La baseline histórica contiene, entre otros:
- safe redaction conservadora;
- firma PFX/P12 dentro del alcance documentado;
- AcroForm clásico `/Tx` dentro del alcance documentado;
- Compare local mínimo;
- Batch `TextWatermark` únicamente;
- QR PNG únicamente;
- PDF→PNG, todas las páginas, 144 DPI fijo en su MVP cerrado.

Ampliaciones requieren un nuevo gate explícito; no extender por conveniencia desde tareas visuales.

## 12. QA y semántica de evidencia

- AUTO/CI PASS demuestra únicamente lo ejecutado por automatización.
- Windows-real, hardware, private corpus, impresora/scanner, stylus/touch, PFX real, lector externo, offline físico y compatibilidad externa requieren evidencia separada.
- Usar `NOT RUN` o `BLOCKED — ENVIRONMENT` cuando corresponda; nunca inferir PASS.

## 13. Estado F13 / continuidad

### Cerrado antes de Task9
- F13 Tasks 1–8: cerradas por sus checkpoints.

### Task9
- T9.0: DONE.
- T9.1: ACTIVE.
- T9.2: NOT STARTED.

R1 reader multi-document GREEN/CLOSED:
`454dcadb6eca13ab98b5d219146dd10ec25af78d` — CI `39004872760` SUCCESS.

R1B pending-state GREEN/CLOSED técnicamente:
`26d026b2fbe1f35754bcf7cef660d2fdaa7e5d34` — CI `39040326178` SUCCESS — `1274/0/4/1278`.

Siguiente elegible:
`T9.1-R2 — TOP_SHELL_AND_REAL_DOCUMENT_TABS`.

R2 está NOT STARTED en este checkpoint. No iniciar T9.2 hasta cierre real de T9.1.

## 14. Historia y recuperación

Detalle operativo actual:
- `.planning/STATE.md`;
- `.planning/ROADMAP.md`;
- `docs/history/2026-10-17-f13-t9-1-r1b-pending-state-green-checkpoint.md`.

Los detalles de slices cerrados, preservation matrices, release packs, decisiones y experimentos siguen bajo `.planning/REQUIREMENTS.md`, `docs/history/**`, `docs/release/**` y planes/specs de `docs/superpowers/**`.

Esos documentos sirven como evidencia histórica/específica; no deben devolver el ruteo operativo a F12 cuando STATE y GitHub muestran F13 activo.
