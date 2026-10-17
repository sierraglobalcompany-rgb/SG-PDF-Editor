# Instrucciones para agentes de desarrollo

## Autoridad operativa

Antes de tocar código o documentación:

1. GitHub live: exact HEAD + exact-head CI;
2. `.planning/STATE.md`;
3. `.planning/ROADMAP.md`;
4. `.planning/REQUIREMENTS.md`;
5. spec/plan aprobado del slice;
6. para UI/UX: `docs/design/SGPDF_DESIGN_LAW.md` + `.agents/skills/sgpdf-design/SKILL.md`;
7. `docs/MASTER_CONTEXT.md` para arquitectura durable;
8. `docs/history/**` como evidencia/checkpoints.

GitHub + STATE mandan sobre snapshots viejos.

## Estado actual — 2026-10-17

- F0–F12: CLOSED / FROZEN en la baseline automatizada.
- F13: ACTIVE; Tasks 1–8 cerradas.
- Task9 T9.0: DONE.
- Task9 T9.1: ACTIVE.
- DG-0 SG Design Governance: GREEN / CLOSED.
- R1 multi-document core: GREEN / CLOSED.
- R1B pending state + dirty-close guard: GREEN / CLOSED.
- R2 top shell + real document tabs: GREEN / CLOSED en `cf816557adab80f0e6e2e4ecb2f6628dc92eba90`; CI `39043770373` SUCCESS.
- R3 document rail + official artwork: GREEN / CLOSED técnicamente; product `394bd320f8df03c0b00b5a517c00869a6c3a74e2`, exact-head GREEN `4050ef37422fc2cdb98b79f4bd4c215534fc553e`; CI `39046256870`, job `117435463253`, Build 0/0, smoke 30 s PASS, tests `1279/0/2/1281`.
- Los 2 skips restantes reservan R4 drawer y R5 context menu; no borrarlos.
- Siguiente bloque: **T9.1-R4 — Pages/Bookmarks collapsible drawer**, NOT STARTED.
- T9.2 NOT STARTED. F14–F18 FROZEN.
- `main` read-only en `31c0594758a83ec555d73ecdd7c597cdf8791fd7`.
- No merge aprobado.

Checkpoint actual:
`docs/history/2026-10-17-f13-t9-1-r3-green-r4-next-checkpoint.md`

## Leyes obligatorias

### KISS
`SIMPLE → EFICIENTE → OPTIMIZADO → EFICAZ → MANTENIBLE → A→B`

### Anti-ruido
`DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`

### Ley SG de Diseño
`INTENT → HIERARCHY → DISCLOSURE → CONSISTENCY → FEEDBACK → ACCESSIBILITY → VERIFY`

No diseñar paneles como un “Excel infinito”. Usar progressive disclosure y una jerarquía humana clara.

### Guardrail anti-parches
`REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`

Es un guardrail separado; no llamarlo ley numerada.

## Gate UI/UX obligatorio

Antes de cambiar XAML, copy, navegación o interacción:

1. definir intención humana;
2. jerarquía primaria/secundaria;
3. qué queda visible por defecto;
4. qué se revela condicionalmente;
5. lenguaje/unidades/errores;
6. teclado/foco/estados/accesibilidad;
7. verificar Light/Dark, 125% scaling cuando aplique, clipping, tests y CI.

Compilar no equivale a QA visual.

## Arquitectura congelada / límites

- Windows x64 + WPF + .NET 10.
- `SGPdf.App + SGPdf.App.Tests` mientras sea suficiente.
- PDFium motor PDF principal; native calls bajo `PdfiumRuntime.NativeGate`.
- Labelize 1.7.0 único renderer ZPL runtime.
- PDFsharp 6.2.4 solo usos estrechos aprobados.
- qpdf 12.4.2 sidecar AES estrecho.
- Tesseract OCR local/offline.
- ZXing.Net test/QA-only.
- No backend, DB, cloud, plugin system, segundo viewer/engine, ModeManager genérico, BatchEngine genérico o framework de conversiones sin decisión explícita.
- No nuevo framework UI para polish que WPF/resources/templates existentes puedan resolver.

## Multi-documento vigente

Owner único:

```text
OpenDocuments
ActiveDocument
PdfDocumentWorkspace por PDF
└─ PdfDocumentPendingState
```

Reglas:
- guardar model state, no controles WPF;
- cambio de tab reutiliza `ActivateDocumentAsync()`;
- cerrar tab dirty reutiliza guard existente;
- no crear segundo registro de sesiones/documentos;
- Ctrl+O/Recientes/abrir otro PDF no fuerzan discard del documento actual.

## Autoridad visual

- App icon, PDF document icon y 15 Home tool icons tienen roles separados.
- En document rail, usar arte oficial aprobado para `edit/sign/organize/ocr/redact/compare/convert/protect`.
- `read/comment/forms` mantienen glyph semántico hasta arte aprobado específico.
- `PDF a PNG` sigue siendo el contrato funcional aunque el arte sea genérico PDF→imagen.
- No Registry hack para asociaciones.

## Git

- No merge a `main` sin aprobación explícita.
- No force-push/rebase por comodidad.
- No borrar contratos `Skip` futuros para maquillar CI.
- Exact-head CI antes de declarar GREEN.
- Si un test viejo contradice un contrato nuevo aprobado, clasificar y realinear el test; no meter workarounds invisibles en producto.

## Micro-task/checkpoint rule

Por instrucción del usuario, dividir el trabajo en micro-tasks y crear checkpoint frecuente para evitar bloqueos.

Formato recomendado:

```text
R?-A cambio focal → checkpoint
R?-B CI/diagnóstico → checkpoint
R?-C docs/cierre → checkpoint
STOP antes del siguiente slice si el bloque se alarga
```

No acumular varios slices sin checkpoint.

## Orden actual

```text
R1  multi-document core                 ✅ GREEN / CLOSED
R1B pending state + close guard         ✅ GREEN / CLOSED
R2  top shell + real document tabs      ✅ GREEN / CLOSED
R3  document rail + official artwork    ✅ GREEN / CLOSED técnico
R4  Pages/Bookmarks drawer              ▶ NEXT / NOT STARTED
R5  context menus                       ⏳
R6  image-selection root cause          ⏳
R7  final visual/functional QA          ⏳
T9.2                                     🔒 NOT STARTED
F14–F18                                  🔒 FROZEN
main                                     🔒 no merge approval
```

Siguiente acción elegible: R4 con Ley SG de Diseño + TDD, reutilizando thumbnails/navigation existentes y auditando bookmarks reales antes de declarar funcionalidad.
