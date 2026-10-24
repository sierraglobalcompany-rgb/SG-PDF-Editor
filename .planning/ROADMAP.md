# Roadmap — SG PDF Editor

> Authority: exact GitHub HEAD/CI → `.planning/STATE.md` → this roadmap → `.planning/REQUIREMENTS.md` → approved slice specs/plans.

## ACTUAL — 2026-10-24

- F14/F15 functional slices A–D and T9.1-R5 are published on `feat/f15-a-compress-20261023`; R5 exact-head CI `40047923449` is **SUCCESS**. `main` remains read-only at `31c0594758a83ec555d73ecdd7c597cdf8791fd7`; no merge/release is authorized.
- R6 causal pixel test is locally verified against synthetic PDFium/WPF composition; the isolated full suite is **1379 PASS / 0 FAIL / 0 SKIP**. The CI for the checkpoint that versions this test must be read at its exact new SHA; the R5 base CI does not include it.
- R7 remains `PARTIAL / NOT_CLOSED`: WIP state is unknown, no app was launched; Windows GUI, Light/Dark, 125% scale, focus/Tab and complex/private corpus checks remain open. F14/F15 functional publication does not make those gates GREEN.
- Macro route: **A** consolidates R6 evidence and performs portable publish preflight; **B** is ChatGPT's independent audit of this report, TRX, hashes, capability table and payload manifest; **C** must be scoped after B. Its minimum evidence gate is a Windows session/VM demonstrably isolated from the WIP, followed by synthetic PDF/ZPL GUI and offline smoke. Installer and public license acceptance remain out of scope / human decision.
- The associated `SGPDF_PLAN_MAESTRO_MACROENTREGAS_RECUPERACION_2026-10-24.md` was not present in Downloads, the repository, or the searched report/Desktop locations; this ACTUAL block records only the received Macro A and verified repository state.

## Routing snapshot — 2026-10-17 (histórico)

- El freeze F14–F18 de este snapshot es histórico y precede a la excepción funcional F14-A/B autorizada el 2026-10-23; consulta el estado vigente en [STATE.md](STATE.md).

- F0–F12: CLOSED / FROZEN for automated baseline.
- F13: ACTIVE.
- F14–F18: FROZEN.
- `main`: read-only at `31c0594758a83ec555d73ecdd7c597cdf8791fd7` until explicit approval.
- T9.0: DONE.
- T9.1: ACTIVE.
- T9.2: NOT STARTED.

Canonical branch:
`feat/f13-task9-t9-1-document-shell-realignment-20261017`

## Governance

### DG-0 SG Design Governance — GREEN / CLOSED

- `docs/design/SGPDF_DESIGN_LAW.md`
- `.agents/skills/sgpdf-design/SKILL.md`
- implementation `3011f700e9014d72951314291bab4c3a67b89c8c`
- checkpoint `9ea973d79b4cd49946022cbbd174678a1280821f`

Mandatory rules:

- KISS: `SIMPLE → EFICIENTE → OPTIMIZADO → EFICAZ → MANTENIBLE → A→B`.
- Anti-noise: `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
- Ley SG de Diseño: `INTENT → HIERARCHY → DISCLOSURE → CONSISTENCY → FEEDBACK → ACCESSIBILITY → VERIFY`.
- Anti-patch guardrail: `REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.

## T9.1 — document-shell realignment

### R1 — reader multi-document core — GREEN / CLOSED

- checkpoint `454dcadb6eca13ab98b5d219146dd10ec25af78d`
- CI `39004872760`
- owner: `OpenDocuments`, `ActiveDocument`, one `PdfDocumentWorkspace` per PDF.

### R1B — per-document pending state + close guard — GREEN / CLOSED

- anchor `26d026b2fbe1f35754bcf7cef660d2fdaa7e5d34`
- CI `39040326178`
- preserves model state for Edit, Sign, Comments, Organize and AcroForm per document;
- dirty close guarded; switching/opening PDFs does not force discard.

### R2 — top shell + real document tabs — GREEN / CLOSED

- technical GREEN anchor `cf816557adab80f0e6e2e4ecb2f6628dc92eba90`
- CI `39043770373` SUCCESS.

Delivered:
- real tabs with file identity;
- `+` add/open;
- activation through existing document owner;
- close through existing dirty guard;
- no duplicate session/document registry;
- legacy menu-position assumptions hardened after top-shell reparenting.

### R3 — document rail + official artwork — GREEN / CLOSED technically

- product commit `394bd320f8df03c0b00b5a517c00869a6c3a74e2`
- test alignment / exact-head GREEN `4050ef37422fc2cdb98b79f4bd4c215534fc553e`
- CI run `39046256870`, job `117435463253`
- Build 0 warnings / 0 errors
- startup smoke PASS 30 s
- tests `1279 passed / 0 failed / 2 skipped / 1281 total`

Delivered:
- 96 px document rail retained;
- approved artwork for Edit, Sign, Organize, OCR, Censurar, Comparar, Convertir and Proteger;
- fallback glyph only for Leer, Comentar and Formularios;
- no label wrapping; accessible selected/disabled semantics preserved.

Transient Tesseract timeout from the previous run did not reproduce. No OCR timeout/code change was made.

Checkpoint:
`docs/history/2026-10-17-f13-t9-1-r3-green-r4-next-checkpoint.md`

## Remaining T9.1 slices

### R4 — Reader navigation y alineamiento visual V4

- **R4-B:** GREEN/CLOSED; navegación Páginas/Marcadores desplegable existente.
- **R4-C:** código `5ff21171abb4b467e8796ba18f54e0d337ab2418`; CI run `39069863859` SUCCESS; 1286 PASS/0 FAIL/1 SKIP, build 0/0, smoke 30s PASS; Windows W01–W15 **pendientes**, por tanto **QA_PENDING**.
- **Diseño V4:** usuario aprobó 4 vistas (Light/Dark, drawer abierto/cerrado) e iconos de app, documento asociado y herramientas. Ver [autoridad visual V4](../docs/design/SGPDF_V4_APPROVED_SHELL_2026-10-17.md).
- **R4-D V4:** tabs y rail implementados. Gate Windows 17-10: test de selección Home realineado en `e4220b6`; focal 13 PASS/1 SKIP, full 1288 PASS/0 FAIL/1 SKIP, build 0/0, smoke 30s PASS. QA Windows parcial detenida por Escape del usuario; no cierre visual. CI exact-head [39076821081](https://github.com/sierraglobalcompany-rgb/SG-PDF-Editor/actions/runs/39076821081) SUCCESS en `0427cfff84e500884759d5287fd53054738c2fb9` (1.288 PASS/0 FAIL/1 SKIP, build 0/0, smoke 30s). R4-C/R4-D siguen QA_PENDING; bloque 3/R5/T9.2 no iniciados. Ver checkpoint del rail.
- **Cierre de R4:** requiere pruebas funcionales+visuales completas, Light/Dark, escala 125%, exact-head CI posterior al último cambio.


### R5 — context menus

- right-click menu on PDF workspace/page;
- image-specific menu when selection owner applies;
- reuse existing command owners; no duplicate operations.

### R6 — image-selection rendering

- reproduce reported blue tint;
- full-image tint remains a defect because the intended overlay fill is transparent;
- fix root cause only.

### R7 — final visual / functional QA

Must include:
- Home Light/Dark;
- PDF open Light/Dark;
- 2+ document tabs;
- tab switching with pending state preservation;
- drawer closed/open;
- context menus;
- image selection;
- real 125% Windows scaling / DPI 120;
- P0=0 / P1=0 before T9.1 exit.

## Next bounded task — snapshot histórico, reemplazado por ACTUAL 2026-10-24

**T9.1-R4-D — tabs+rail publicados y CI SUCCESS; siguiente único gate: [QA visual Windows V4](../docs/qa/2026-10-17-r4c-r4d-v4-windows-matrix.md)**. R4-C/R4-D siguen QA_PENDING. Bloque 3/R5/T9.2 bloqueados.

Do not start R5, T9.2, F14–F18 or merge to `main` automatically. V4 is design-approved, not Windows QA-approved.
