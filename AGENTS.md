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

- F13 / Task9 / T9.1 ACTIVE; F0–F12 and Tasks 1–8 CLOSED; T9.2 and F14–F18 NOT STARTED/FROZEN.
- R1/R1B/R2/R3 technical GREEN/CLOSED; **R4-B GREEN/CLOSED**.
- **R4-C QA_PENDING:** commit `5ff21171abb4b467e8796ba18f54e0d337ab2418`; CI `39069863859` SUCCESS, full suite 1286 PASS / 0 FAIL / 1 SKIP; Windows visual/functional W01–W15 incomplete.
- **V4 Light/Dark shell mockups APROBADOS por usuario**: `docs/design/SGPDF_V4_APPROVED_SHELL_2026-10-17.md`. Cinco PNG aprobados incorporados en `docs/design/references/sgpdf-v4/` con SHA-256 verificados; allí figuran enlaces y hashes.
- **R4-D V4 realignment:** tabs y rail implementados. Gate Windows 17-10: test de selección Home realineado en `e4220b6`; focal 13 PASS/1 SKIP, full 1288 PASS/0 FAIL/1 SKIP, build 0/0, smoke 30s PASS. QA Windows parcial detenida por Escape del usuario; no cierre visual. CI final por verificar tras publicar. R4-C/R4-D siguen QA_PENDING; bloque 3/R5/T9.2 no iniciados. Ver checkpoint del rail.
- **R5 NOT STARTED. T9.2 NOT STARTED. NO MERGE a main** (`main` read-only).


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
R1/R1B/R2/R3                  GREEN / CLOSED técnico
R4-B                            GREEN / CLOSED
R4-C                            QA_PENDING, CI PASS; Windows QA pendiente
V4 mockups                      APROBADOS; binarios de referencia incorporados; comparación Windows pendiente
R4-D                            TABS CODE PUSHED, CI_PENDING; rail → top bar → drawer
R5/T9.2                        NOT STARTED
main                            READ-ONLY, NO MERGE
```

Al cambiar UI, leer `docs/design/SGPDF_V4_APPROVED_SHELL_2026-10-17.md` y reutilizar los iconos de `Assets/SGPdf.App.ico`, `Assets/SGPdf.Document.png`, `Assets/HomeTools/*.png` y `Themes/ShellIcons.xaml` según autoridad vigente; no usar recortes del mockup como assets productivos.
