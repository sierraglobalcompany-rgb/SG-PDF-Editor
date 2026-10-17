# SG PDF Editor — Contexto maestro

## AUTORIDAD VIGENTE — 2026-10-17

Este documento conserva arquitectura y decisiones durables. El estado operativo diario se resuelve en este orden:

1. GitHub exact HEAD + exact-head CI;
2. `.planning/STATE.md`;
3. `.planning/ROADMAP.md`;
4. `.planning/REQUIREMENTS.md`;
5. spec/plan aprobado del slice;
6. para UI/UX: `docs/design/SGPDF_DESIGN_LAW.md` + `.agents/skills/sgpdf-design/SKILL.md`;
7. este MASTER_CONTEXT;
8. históricos como evidencia/contexto.

Estado resumido y routing al 2026-10-17:
- F0–F12 CLOSED/FROZEN; F13 Task9 T9.1 ACTIVE, T9.2 NOT STARTED; R1/R1B/R2/R3 technical GREEN/CLOSED.
- R4-B GREEN/CLOSED. R4-C está en `QA_PENDING` (commit `5ff21171abb4b467e8796ba18f54e0d337ab2418`, CI `39069863859` SUCCESS, Windows W01–W15 sin completar).
- Diseño visual V4 APROBADO por usuario; `docs/design/SGPDF_V4_APPROVED_SHELL_2026-10-17.md` es la referencia de UI. Los PNG de mockup todavía requieren publicación binaria en GitHub.
- R4-D (shell V4 alignment): primer microbloque tabs publicado (`14065f7`); microbloque rail también publicado (`da060c5`), selección única y 60 DIP; CI exact-head y QA Windows pendientes. R5 NOT STARTED, no merge a main.
- Checkpoint: `docs/history/2026-10-17-f13-t9-1-r4c-pending-v4-approved-checkpoint.md`.


---

## 1. Producto

SG PDF Editor es una aplicación Windows local-first/offline para:

1. PDF: leer, navegar, imprimir, firmar, organizar, editar conservadoramente, comentar y ejecutar utilidades profesionales aprobadas.
2. Etiquetas térmicas: abrir ZPL/TXT/PRN, previsualizar, respetar cantidades, maquetar, exportar PDF e imprimir sin Internet.

No pretende clonar Acrobat completo. Se construye por vertical slices útiles y verificables.

## 2. Reglas no negociables

### KISS
`SIMPLE → EFICIENTE → OPTIMIZADO → EFICAZ → MANTENIBLE → A→B`

### Anti-ruido
`DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`

### Ley SG de Diseño
`INTENT → HIERARCHY → DISCLOSURE → CONSISTENCY → FEEDBACK → ACCESSIBILITY → VERIFY`

Toda UI parte de la tarea humana y aplica progressive disclosure. No aceptar paneles tipo “Excel infinito”.

### Guardrail anti-parches
`REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`

No llamarlo ley numerada.

## 3. Arquitectura runtime aprobada

```text
SG PDF Editor
│
├─ WPF + .NET 10
├─ PDFium — motor PDF principal
├─ Labelize 1.7.0 — renderer ZPL runtime
├─ PDFsharp 6.2.4 — usos estrechos aprobados
├─ qpdf 12.4.2 — AES sidecar estrecho
├─ Tesseract — OCR local/offline
└─ Windows Printing
```

Decisiones durables:
- PDFium sigue siendo el motor PDF primario.
- Native PDFium bajo `PdfiumRuntime.NativeGate`.
- Labelize 1.7.0 es único renderer ZPL runtime.
- BinaryKits es histórico; no fallback runtime.
- No Labelary/HTTP server runtime.
- PDFsharp no se convierte en segundo motor general.
- qpdf no se convierte en motor general.
- ZXing.Net permanece test/QA-only.
- No segundo viewer, plugin framework, generalized ModeManager, conversion framework o BatchEngine sin decisión explícita.

## 4. Solución KISS

Mientras sea suficiente:

```text
src/SGPdf.App/
tests/SGPdf.App.Tests/
```

No recrear capas Core/Infrastructure/Domain/Application, CQRS/event bus/DI complejo por prevención.

## 5. Multi-documento

Owner único vigente:

```text
OpenDocuments
ActiveDocument
PdfDocumentWorkspace por PDF
└─ PdfDocumentPendingState
```

Cada workspace posee sesión, navegación, zoom, scroll y pending model state.

R1 estableció el lifecycle multi-documento real.

R1B preserva estado pendiente por PDF para Edit, Sign, Comments, Organize y AcroForm sin almacenar controles WPF. Secuencia:

`CAPTURE CURRENT MODEL → DETACH PRESENTATION → BIND TARGET → RESTORE TARGET MODEL/UI`

Reglas:
- cambiar de tab no exige guardar/discard;
- cerrar tab dirty sí usa guard explícito;
- Ctrl+O/Recientes/abrir otro PDF no destruyen trabajo pendiente;
- no crear segundo registro de documentos o sesiones.

## 6. R2 — Top shell + real tabs

R2 está GREEN/CLOSED.

Anchor:
`cf816557adab80f0e6e2e4ecb2f6628dc92eba90`

CI:
`39043770373` SUCCESS.

Durable truth:
- tabs visibles reales;
- botón `+` abre/agrega PDF;
- selección de tab usa owner multi-documento existente;
- cierre usa dirty guard existente;
- top shell fue reparentado sin mantener supuestos de que `Menu` sea hijo directo del `DockPanel`;
- no se introdujo una segunda arquitectura de navegación/documentos.

## 7. R3 — Document tool rail

R3 está GREEN/CLOSED técnicamente.

Product commit:
`394bd320f8df03c0b00b5a517c00869a6c3a74e2`

Exact-head GREEN:
`4050ef37422fc2cdb98b79f4bd4c215534fc553e`

CI:
- run `39046256870`;
- job `117435463253`;
- Build 0 warnings / 0 errors;
- startup smoke 30 s PASS;
- tests `1279 passed / 0 failed / 2 skipped / 1281 total`.

Arte oficial reutilizado en document rail:
- Editar → `edit-pdf.png`;
- Firmar → `sign.png`;
- Organizar → `organize-pages.png`;
- OCR → `ocr.png`;
- Censurar → `redact.png`;
- Comparar → `compare-pdf.png`;
- Convertir → `pdf-to-png.png`;
- Proteger → `protect.png`.

Leer, Comentar y Formularios mantienen glyph fallback porque aún no existe arte aprobado específico.

El rail conserva 96 px, orden, accesibilidad, selected/disabled semantics y labels sin wrap.

Un test viejo que exigía `Path` en todos los botones fue realineado a la nueva autoridad. No se añadieron Paths invisibles.

Un timeout aislado de Tesseract no reprodujo en el exact-head GREEN; no se cambió código/timeout OCR.

## 8. ZPL / etiquetas

Entradas aprobadas: `.zpl`, `.txt` con ZPL, `.prn` con ZPL.

`^PQ` representa cantidad de salida; no multiplicar render innecesariamente.

Labelize 1.7.0 permanece renderer oficial local.

### UX ZPL durable

El panel de propiedades no debe mostrar simultáneamente cantidad, tamaño, resolución, medio, hoja, filas, columnas, rotación, márgenes y gaps en una lista plana.

Aplicar progressive disclosure:
- cantidad personalizada solo cuando se selecciona;
- tamaño personalizado solo cuando se selecciona;
- opciones de hoja solo para salida en hoja;
- filas/columnas solo para layout custom;
- avanzado solo cuando aporta al flujo.

## 9. Autoridad visual

Roles separados:
- App/software icon → EXE/titlebar/taskbar/Alt-Tab.
- PDF document icon → identidad documental/asociación futura por installer.
- 15 Home tool icons → arte oficial por herramienta.

No reinterpretarlos durante cambios no relacionados.

`PDF a PNG` continúa siendo el contrato funcional actual; no anunciar JPG sin capacidad real.

## 10. Document shell / UX

Estado de hallazgos:
- DS-001 DOCUMENT_SHELL — mejorado por R2/R3; final visual QA sigue R7.
- DS-002 MULTI_DOCUMENT_WORKSPACE — R1/R1B/R2 GREEN.
- DS-003 SHELL_RAIL_PRESENTATION — R3 technical GREEN; final visual QA R7.
- DS-004 DOCUMENT_TOOL_RAIL — R3 technical GREEN.
- DS-005 READER_NAV_DRAWER — R4 NEXT.
- DS-006 BOOKMARKS — R4 AUDIT_REQUIRED.
- DS-007 IMAGE_SELECTION_RENDERING — R6 REPRODUCE_REQUIRED.
- DS-008 PDF_CONTEXT_MENU — R5 OPEN.
- DS-009 TOP_SHELL — R2 technical GREEN; final visual QA R7.
- DS-010 EDIT_OBJECT_INTERACTION — OPEN.

Sequence:

```text
R1  multi-document core              ✅
R1B pending state + close guard      ✅
R2  top shell + tabs                 ✅
R3  document rail + approved icons   ✅
R4  Pages/Bookmarks drawer           NEXT
R5  context menus
R6  image-selection root cause
R7  final visual/functional QA
```

## 11. R4 — recuperación de estado y V4 aprobado

- R4-B GREEN/CLOSED.
- R4-C implementación bookmark con lazy load, cancelación y feedback: CI automatizada PASS, QA Windows pendiente.
- Los mockups V4 Light/Dark, drawer cerrado/abierto e iconografía oficial fueron aprobados; leer `docs/design/SGPDF_V4_APPROVED_SHELL_2026-10-17.md`.
- R4-D por microbloques: tabs/header-only e icono de documento asociado publicados en `14065f7` sin CI/QA certificadas; falta certificar ese cambio; luego normalizar rail (único owner), command bar y drawer; evidenciar pantallas reales Windows.
- Preservar modelo por PDF, permisos, PDFium, ZPL, R4-C funcional, sin nuevos frameworks.
- No declarar GREEN visual antes de screenshots Light/Dark/125%/teclado y CI exact-head.


## 12. QA semantics

AUTO/CI PASS demonstrates only automated evidence.

Windows-real, Light/Dark visual fidelity, 125% scaling, hardware/private corpus, printer/scanner, stylus/touch, PFX real, external-reader and physical offline checks require separate evidence.

Use `NOT RUN` / `BLOCKED — ENVIRONMENT` when appropriate.

## 13. Micro-task/checkpoint discipline

To avoid chat/tool blocking, work in micro-tasks with checkpoints:

```text
slice-A focused change → checkpoint
slice-B CI/diagnostic → checkpoint
slice-C docs/closure → checkpoint
```

Do not accumulate multiple slices before checkpointing.

## 14. Recovery

Live HEAD y CI → `.planning/STATE.md` → `docs/design/SGPDF_V4_APPROVED_SHELL_2026-10-17.md` → `.planning/ROADMAP.md` → checkpoint `docs/history/2026-10-17-f13-t9-1-r4c-pending-v4-approved-checkpoint.md` → `AGENTS.md`.
R4-C QA_PENDING; R4-D PLANNED; R5/T9.2 NOT STARTED; main sin merge autorizado.
