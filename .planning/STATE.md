---
gsd_state_version: '1.1'
status: f13-t9-1-v4-unified-code-ci-green-windows-qa-pending
progress:
  total_phases: 19
  completed_phases: 13
  current_phase: F13
  current_task: Task9
  current_subtask: T9.1
---

## Estado operativo — Macro B/C · 2026-10-24

- El publish `win-x64` bloqueado ahora es reproducible: `packages.lock.json` de App/Tests incluye el target RID con versiones y hashes existentes preservados; el cambio está en `b71f279`.
- Build Release local: 0 advertencias / 0 errores; suite: 1379 PASS / 0 FAIL / 0 SKIP. Publish autocontenido inventariado con los 13 avisos .NET/WPF y terceros aplicables. Ver reporte y manifiesto Macro B/C externo.
- Aplicación portable no lanzada: no se demostró aislamiento respecto al WIP. GUI/125%/foco y smoke offline siguen sin verificar; no se declaran R7 ni release GREEN.
- Riesgo residual `ZplGSCustom.ttf`: decisión humana pública pendiente. `main` sigue read-only; sin installer, merge ni release.

## Estado operativo — Macro A / F14–F15–T9.1 · 2026-10-24

- F15-A/B: publicado en `81dfc45fd39b723407562957b072a610b823378a`; CI exact-head `build` `39993998408` **SUCCESS**.
- F15-C (recorte y desbloqueo autorizado): publicado en `d0938547e3f58c962ec80c2b829bf1abd18314d6`; CI exact-head `build` `40008500764` **SUCCESS**. QA WPF `GUI_NOT_VERIFIED`; no equivale a cierre visual.
- F15-D (marca de agua de imagen): publicado en `ddd47ef3236e10fa85d41e1af8ccb988cc8784cc`; CI exact-head `40021821032` **SUCCESS**. QA automatizada local: focal 24/24, integrada 40/40, suite 1374 PASS / 0 FAIL / 1 SKIP y smoke 30 s PASS. Tab/foco `NOT_VERIFIED`.
- T9.1-R5 (menús contextuales): commit `22f1a38c225478685d759879aea90645eba166c1`; CI exact-head `build` `40047923449` **SUCCESS** (1378 PASS / 0 FAIL / 0 SKIP). GUI `NOT_VERIFIED` por WIP; R4 `PARTIAL`; V4/F18 pendientes.
- R6: test causal WPF/PDFium `MainWindowImageSelectionPixelCompositionTests.cs`, SHA-256 `1EF9CABBA4283DABDC05B240368E6B40B9C8F312E32CAEF38D1C93E6739A9481`; focal PASS en el reporte R6 y suite aislada local **1379 PASS / 0 FAIL / 0 SKIP**. El CI base 40047923449 no incluye este test; exact-head CI del checkpoint que lo versiona queda por confirmar tras publicación. No declarar `R6_FINAL_GREEN` ni cerrar R7 por CI solamente.
- Publish portable `win-x64` autocontenido compilado e inventariado fuera del repo: 582 archivos / 281,487,751 bytes. App no lanzada; GUI bloqueada por WIP no aislada y smoke offline `NOT_RUN`. `PUBLIC_LICENSE_GATE=DECISION_HUMANA_PENDING` por la procedencia residual documentada de `ZplGSCustom.ttf`.
- Dark/125 %, foco y corpus documental complejo: pendientes. F14/F15 no se declaran global GREEN. `main` sigue read-only, sin merge ni release.
## Estado operativo — F14-A/B · 2026-10-23

- Excepción funcional F14-A/B: autorizada y ejecutada después del freeze histórico F14–F18. Prioridad vigente: funcionalidad F14–F17 primero; pulido visual V4 en F18/Astra.
- F14-B: PASS_LOCAL_FUNCIONAL según QA local: focal 14/14; suite completa 1317 PASS / 0 FAIL / 1 SKIP; smoke 30 s; Unir 3 páginas y Dividir 2+2; las fuentes quedaron intactas. Home: 14/15 accesos conectados; Imagen→PDF permanece deshabilitada hasta F15.
- R4-C/R4-D y Dark/125 %: QA_PENDING. La verificación exact-head del CI de esta publicación se registra en el reporte PUB1 V3; al redactar este bloque seguía pendiente. PDFs complejos no acreditados.
- F13/T9.1 continúa activo. F15 es la siguiente etapa de planificación y no se inicia aquí. main sigue read-only: sin merge ni release. F14 no se declara global GREEN; V4 no queda certificada.

# Project State

## Historia — autoridad V4 shell unificado (2026-10-19; snapshot histórico)

**Usuario autorizó reconstrucción integral de la composición visual**, incluida **apariencia** de la fila superior; no desbloquea nuevas funciones. Se reemplaza el prompt/lote B previo por [plano y QA integrados](../docs/plans/SGPDF_V4_SHELL_UNIFICADO_RECONSTRUCCION_QA_2026-10-19.md).

- Baseline Lote A `644ffbb` **Windows GREEN** Dark/hot-theme/W09; no reabrirlo.
- Refactor UI integrado: `3968e0d` (cabecera/tab/toolbar en XAML), `bf97a97` (rail/drawer/canvas), `a45c90c` (contratos actualizados). Exact-head [CI 39204283530](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39204283530) **SUCCESS**, 1290 PASS / 0 FAIL / 1 SKIP, build 0 warnings/0 errors y smoke 30 s PASS.
- **R4-C/R4-D QA_PENDING VISUAL WINDOWS**: la reconstrucción NO se ha comparado aún con las cuatro vistas reales de Windows. Próxima acción única: **Codex QA_ONLY integrado**, cuatro capturas Light/Dark/navegación abierto/cerrado en 125%, min-width 960 DIP, multi-PDF, W09, Home/ZPL, dirty guard y Light↔Dark caliente. No marcar V4 GREEN por CI.
- **Bloque 3 solo visual** autorizado dentro de T9.1; **bloque 3 funcional, R5/T9.2, F14–F18, IA y merge NO autorizados**.
- Solo actualizar esta sección o el checkpoint canónico si cambia la verdad viva; historia previa queda abajo como evidencia, no como estado activo. Nuevo commit documental necesita su propio CI exact-head.

## Método vigente / foco real — 2026-10-18

- **LOTE_A_CODE_AND_WINDOWS_GREEN** para QA-V4-002/003; W09 conserva PASS y regresión corta real PASS. [Resultado canónico FIX_AND_VALIDATE](../docs/history/2026-10-18-v4-lote-a-dark-hot-theme-fixed.md): causas, diff, QA Windows 125%, hashes y gates. CI exact-head del commit final se entrega al publicar. **R4-C/R4-D siguen QA_PENDING** por fidelidad/cobertura V4 restante; este resultado prevalece sobre P1 de tema abiertos en textos históricos inferiores. Lote B/C pendiente; bloque 3/R5/T9.2/F14–F18/merge sin autorización.
- **Ruta optimizada:** [auditoría 10 pasadas + lotes A/B/C](../docs/plans/SGPDF_R4_V4_METODOLOGIA_LOTES_2026-10-18.md); [prompt único para Codex/Antigravity](../docs/qa/SGPDF_PROMPT_CODEX_ANTIGRAVITY_R4_V4_LOTES_2026-10-18.md). Lote A P1/tema/W09 → B estructura V4 → C QA final. Un CI y un checkpoint por lote, salvo riesgo P0 o bloqueo real.
- **No aprobado:** command bar/bloque 3, R5/T9.2/F14–F18, merge a main y cualquier herramienta IA. Esta sección prevalece sobre frases históricas posteriores aún no depuradas.

## Authority

Operational order:
1. live GitHub exact HEAD + exact-head CI;
2. this file;
3. `.planning/ROADMAP.md`;
4. `.planning/REQUIREMENTS.md`;
5. approved slice/spec/plan;
6. for UI/UX: `docs/design/SGPDF_DESIGN_LAW.md` + `.agents/skills/sgpdf-design/SKILL.md`;
7. `docs/MASTER_CONTEXT.md` for durable architecture;
8. `docs/history/**` as checkpoints/evidence.

## Mandatory rules

- **KISS:** `SIMPLE → EFICIENTE → OPTIMIZADO → EFICAZ → MANTENIBLE → A→B`.
- **Anti-noise:** `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
- **Ley SG de Diseño:** `INTENT → HIERARCHY → DISCLOSURE → CONSISTENCY → FEEDBACK → ACCESSIBILITY → VERIFY`.
- **Guardrail anti-parches:** `REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.

## Frozen history

- F0–F12: CLOSED / FROZEN for automated baseline.
- F13 Tasks 1–8: CLOSED by checkpoints.
- F14–F18: FROZEN.
- `main`: read-only at `31c0594758a83ec555d73ecdd7c597cdf8791fd7` until explicit approval.

## F13 Task9 — ACTIVE

- T9.0: DONE.
- T9.1: ACTIVE.
- T9.2: NOT STARTED.
- No merge to `main` approved.

Canonical branch:
`feat/f13-task9-t9-1-document-shell-realignment-20261017`

## DG-0 — SG Design Governance — GREEN / CLOSED

- implementation: `3011f700e9014d72951314291bab4c3a67b89c8c`;
- checkpoint: `9ea973d79b4cd49946022cbbd174678a1280821f`.

## T9.1 slices

### R1 reader multi-document core — GREEN / CLOSED

- checkpoint: `454dcadb6eca13ab98b5d219146dd10ec25af78d`;
- CI run `39004872760`, job `117315172824`;
- tests `1266 passed / 0 failed / 4 skipped / 1270 total`.

Established `OpenDocuments`, `ActiveDocument` and one `PdfDocumentWorkspace` per PDF with independent session/page/zoom/scroll ownership.

### R1B per-document pending state + close guard — GREEN / CLOSED

- technical anchor: `26d026b2fbe1f35754bcf7cef660d2fdaa7e5d34`;
- CI run `39040326178`, job `117418290462`;
- tests `1274 passed / 0 failed / 4 skipped / 1278 total`.

Each `PdfDocumentWorkspace` owns model-only pending state for Edit, Sign, Comments, Organize and AcroForm. Switching PDFs preserves pending work. Dirty close uses an explicit guard. Opening/switching PDFs does not force discard.

### R2 top shell + real document tabs — GREEN / CLOSED

Technical GREEN anchor:
`cf816557adab80f0e6e2e4ecb2f6628dc92eba90`

Exact-head CI:
- run `39043770373`;
- overall SUCCESS.

R2 truth:
- real document tabs exist;
- `+` opens/adds PDFs;
- tab activation reuses `ActiveDocument` / `ActivateDocumentAsync()`;
- tab close reuses the R1B dirty-close owner;
- no second document/session registry was introduced;
- top-shell menu reparenting was hardened by removing assumptions that Menu is a direct DockPanel child.

### R3 document tool rail + official artwork — GREEN / CLOSED technically

Product implementation:
`394bd320f8df03c0b00b5a517c00869a6c3a74e2`

Test alignment / exact-head GREEN:
`4050ef37422fc2cdb98b79f4bd4c215534fc553e`

Exact-head CI:
- run `39046256870`;
- job `117435463253`;
- Build PASS — 0 warnings / 0 errors;
- startup smoke PASS — 30 s;
- tests `1279 passed / 0 failed / 2 skipped / 1281 total`;
- overall SUCCESS.

R3 truth:
- document rail remains 96 px and ordered;
- accessibility/selected/disabled semantics remain;
- official approved artwork is used for `edit`, `sign`, `organize`, `ocr`, `redact`, `compare`, `convert`, `protect`;
- `read`, `comment`, `forms` keep semantic glyph fallback because no approved matching art exists yet;
- labels use 10.5 pt, no wrapping and ellipsis trimming;
- an old test requiring Path for all 11 buttons was realigned to the approved Image/Path split;
- a one-off Tesseract process timeout from the previous run did not reproduce; no OCR timeout/code change was made.

Checkpoint:
`docs/history/2026-10-17-f13-t9-1-r3-green-r4-next-checkpoint.md`.

## R4 — estado actual verificado (2026-10-17)

**Último retest Windows — 2026-10-18:** app `fc662c8c9123c3f2e0ddbf685c3cf1e01d76a404`, build 0/0; primer smoke terminó ExitCode=0 antes de 30s sin causa confirmada, repetición PASS 30s. **V01/icon PASS posfix**, cuatro capturas WPF reales Light/Dark abierto/cerrado al 125%. **R4-C/R4-D QA_PENDING / P1 OPEN:** contraste de tabs/miniaturas/Organizar en Dark, actualización de tema en caliente y restauración visual W09 entre PDFs homónimos (causa por confirmar). Guard de cierre No/Sí, cierre inactivo, bookmarks y ZPL ejecutados; mínimo/Tab-order/error inducido siguen sin cobertura íntegra. Sin cambios productivos/tests/assets. [Matriz actualizada](../docs/qa/2026-10-17-r4c-r4d-v4-windows-matrix.md) y [checkpoint posfix](../docs/history/2026-10-18-r4c-r4d-v4-postfix-windows-results.md). CI del commit documental posterior por verificar aparte. No bloque 3/R5/T9.2/merge. Los estados de icono pendiente siguientes son históricos previos a este retest.

**Actualización QA-V4-001 (2026-10-18): código corregido, CI GREEN, aún NO validado visualmente en Windows.** Prueba WPF real del icono, `ad69280`, reproducida RED en [39082039007](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39082039007): el `Image` generado tenía `Source == null`; después se modificó **una sola línea** en `BuildDocumentTabHeaderTemplate()` para usar URI de paquete absoluta `pack://application:,,,/SGPdf.App;component/Assets/SGPdf.Document.png` (commit `3f5d0c9`). [CI 39082254808](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39082254808) SUCCESS: 1.289 PASS, 0 FAIL, 1 SKIP; build 0 errores/advertencias, smoke 30 s PASS. **No actualizar V01/T06 a PASS:** el screenshot real anterior corresponde a `26db4b1`; se requiere nueva V01 en Windows sobre el nuevo HEAD y luego completar V02–V04/W01–W15/T01–T08. [Checkpoint causal](../docs/history/2026-10-18-qa-v4-001-tab-icon-red-green-windows-retest-pending.md). No merge, bloque 3 ni R5/T9.2.

**Último gate Windows:** app `26db4b18a9cfe144be88775ac2f170e5939a53a0`, build 0/0 y smoke 30s PASS; Light, escala real 125% (monitor 1920x1200). **STOP_P1 / QA_PENDING:** icono PDF de la pestaña ausente en captura real V01 (T06 FAIL). Owner `BuildDocumentTabHeaderTemplate()`; causa exacta de resolución del bitmap aún no confirmada. Sin cambio de producto ni tests. QA restante detenida según prompt; no cierre ni bloque 3/R5/T9.2. Matriz y diagnóstico: [checkpoint QA](../docs/history/2026-10-17-r4c-r4d-windows-qa-p1-tab-icon.md). CI del commit documental posterior se verifica por separado.

- **R4-B:** GREEN / CLOSED; no reabrir.
- **R4-C (marcadores):** código en `5ff21171abb4b467e8796ba18f54e0d337ab2418`, exact-head CI [39069863859](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39069863859) SUCCESS: 1286 PASS, 0 FAIL, 1 SKIP; build 0/0; smoke 30s. **Windows QA W01–W15 pendiente → QA_PENDING**; no GREEN de cierre.
- **V4 visual:** mockups aprobados el 17-10, autoridad [V4](../docs/design/SGPDF_V4_APPROVED_SHELL_2026-10-17.md); validar hashes antes de incorporar imágenes. Cinco PNG V4 incorporados con SHA-256 verificados y enlaces en la spec; comparación visual Windows pendiente.
- **R4-D:** tabs y rail implementados. Gate Windows 17-10: test de selección Home realineado en `e4220b6`; focal 13 PASS/1 SKIP, full 1288 PASS/0 FAIL/1 SKIP, build 0/0, smoke 30s PASS. QA Windows parcial detenida por Escape del usuario; no cierre visual. CI exact-head [39076821081](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39076821081) SUCCESS en `0427cfff84e500884759d5287fd53054738c2fb9` (1.288 PASS, 0 FAIL, 1 SKIP, build 0/0, smoke 30s). R4-C/R4-D siguen QA_PENDING; bloque 3/R5/T9.2 no iniciados. Ver checkpoint del rail.
- **R5 y T9.2:** NOT STARTED. **NO MERGE**.

Checkpoint: `docs/history/2026-10-17-f13-t9-1-r4c-pending-v4-approved-checkpoint.md`.

## Remaining findings / sequence

- DS-001 DOCUMENT_SHELL — P1 ACTIVE, improved by R2/R3.
- DS-002 MULTI_DOCUMENT_WORKSPACE — R1/R1B/R2 GREEN.
- DS-003 SHELL_RAIL_PRESENTATION — R3 technical GREEN; final visual QA remains R7.
- DS-004 DOCUMENT_TOOL_RAIL — R3 technical GREEN.
- DS-005 READER_NAV_DRAWER — P1 NEXT / R4.
- DS-006 BOOKMARKS — P2 AUDIT_REQUIRED in R4.
- DS-007 IMAGE_SELECTION_RENDERING — P1 REPRODUCE_REQUIRED in R6.
- DS-008 PDF_CONTEXT_MENU — P1 OPEN / R5.
- DS-009 TOP_SHELL — R2 technical GREEN; final visual QA remains R7.
- DS-010 EDIT_OBJECT_INTERACTION — P1 OPEN.

Execution order:
`R4-C QA Windows + R4-D V4 → R4 closure auditada → R5 (requiere aprobación) → R6 → R7`.

## QA semantics

Automated PASS proves only automated evidence. Windows-real, Light/Dark visual fidelity, 125% scaling, hardware/private corpus, printing/scanning/stylus and external-reader checks remain separate until actually run.

## V4 postfix QA handoff — 2026-10-18

- Auditoría de **31 herramientas sin IA** y brecha visual V4: [inventario auditado](../docs/qa/2026-10-18-auditoria-31-herramientas-sin-ia-y-brecha-visual-v4.md), baseline `a8ef2cf`, CI [39083267232](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39083267232) SUCCESS (1289/0/1; build 0/0; smoke 30s).
- **Próximo gate único:** [Codex QA V01 posfix y comparación V4](../docs/qa/2026-10-18-codex-v4-v01-postfix-visual-gate.md). Solo evidencia real Windows, no implementar UI, sin subir documentos privados.
- R4-C/R4-D siguen `QA_PENDING` por foto V01 anterior al fix y resto visual W01–W15/T01–T08; bloque 3, F14, R5, T9.2 y merge siguen detenidos. No integrar IA ni traducción IA; `PDF→Markdown` inventariado como no implementado y sin IA.
- **CI de este commit documental** pendiente de verificar por separado; no inferir GREEN visual.

## W09 homologación de drawer — código GREEN, QA Windows pendiente (2026-10-18)

- Test causal 2 PDF homónimos `245ca41` + harness `9eb73a6`; [RED 39152982080](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39152982080) confirmó `Expected Collapsed / Actual Visible`.
- Fix de owner `cc5d58d` en `MainWindow.Documents.cs` llama explícitamente `SyncShellWorkspaceFromActiveDocument()` al activar; [GREEN 39153249583](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39153249583): 1.290 PASS, 0 FAIL, 1 SKIP, build 0/0, smoke 30 s. [Checkpoint](../docs/history/2026-10-18-r4d-w09-homonymous-pdfs-red-green-checkpoint.md).
- **W09 aún no PASS Windows**; P1 contraste Dark y cambio de tema vivo continúan OPEN; R4-C/R4-D `QA_PENDING`. No bloque 3/R5/T9.2/F14/merge.

## Recovery if chat ends

1. Confirmar rama y HEAD/CI live; nunca asumir un SHA histórico.
2. Leer este STATE y [V4 aprobada](../docs/design/SGPDF_V4_APPROVED_SHELL_2026-10-17.md) + checkpoint `docs/history/2026-10-17-f13-t9-1-r4c-pending-v4-approved-checkpoint.md`.
3. Estado: R4-C/R4-D WINDOWS QA_PENDING (código tabs+rail y CI SUCCESS en `0427cff`), bloque 3/R5/T9.2 NOT STARTED, R4-B GREEN/CLOSED. No merge.
4. Cinco PNG V4 en `docs/design/references/sgpdf-v4/`, hashes verificados; no confundir mockups con evidencia Windows.
5. Ejecutar [matriz de QA W01–W15 y V4](../docs/qa/2026-10-17-r4c-r4d-v4-windows-matrix.md) en Windows real antes de cierre. Este cambio documental requiere CI nuevo del HEAD final.

### R4-D / QA automatizado adicional — 2026-10-17

- Test de contrato del `TabControl` header-only reforzado con XAML semántico en `f3aecace66f1b4a039487f3a864d8417141b2822` (no `ContentPresenter` de workspace debajo de las tabs).
- Test STA real de pestaña seleccionada en apertura PDF contra `ActiveDocument` en `e8001a0ea354a22f5b9868d1c792148e92d11e49`.
- Exact-head CI de `e8001a0`: [39080291059](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39080291059), SUCCESS: 1.288 PASS, 0 FAIL, 1 SKIP; 0 warnings/0 errors; startup 30 s PASS.
- **No prueba visual**: aún sin cuatro capturas WPF reales, Windows 125% ni cobertura completa W01–W15/T01–T08. R4-C/R4-D permanecen `QA_PENDING`. No abrir bloque 3/R5/T9.2 ni merge.
