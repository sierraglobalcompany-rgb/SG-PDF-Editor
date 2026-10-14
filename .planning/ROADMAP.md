# Roadmap — SG PDF Editor

> Operational authority: exact GitHub head/CI → `.planning/STATE.md` → this roadmap → `.planning/REQUIREMENTS.md` → approved slice specs/plans. `docs/MASTER_PLAN.md` is an architectural/historical plan and must not override newer operational state.

## Current baseline — 2026-10-14

**F0–F12 functional automated baseline is CLOSED and feature-frozen.**

Authoritative functional branch:
`feat/f12-7-pdf-to-png-mvp-green3`

Exact functional HEAD:
`8e1845d6d8fe72374d7a80994d8500b6d36077cc`

Exact-head functional CI:
- run `38555829715`;
- job `115824598759`;
- Release build: **0 warnings / 0 errors**;
- tests: **1202/1202 PASS**, 0 failed, 0 skipped.

`main` remains read-only at `31c0594758a83ec555d73ecdd7c597cdf8791fd7` until explicit user approval.

## Phase table

| Phase | Automated status | Real/manual/private/physical status |
|---|---|---|
| F0 PDF Base | CLOSED / AUTO PASS | open where documented |
| F1 ZPL Gate | synthetic PASS | private corpus open |
| F2 ZPL Workspace | CLOSED / AUTO PASS | physical/private open |
| F3 Visual Signature | CLOSED / AUTO PASS | Windows/photo/stylus open |
| F4 Reader | CLOSED / AUTO PASS | Windows real open |
| F5 Organize | CLOSED / AUTO PASS | Windows real open |
| F6 Images | CLOSED / AUTO PASS | Windows real open |
| F7 Text V1 | CLOSED / AUTO PASS | Windows/external open |
| F8 Comments | CLOSED / AUTO PASS | stylus/touch/external open |
| F9 Utilities | CLOSED / AUTO PASS | Windows/private/external open |
| F10 OCR | CLOSED / AUTO PASS | private scans/external/offline open |
| F11 Text V2 | CLOSED / AUTO PASS | representative layouts open |
| F12 Professional | **CLOSED / AUTO PASS** | representative QA open |

## F12 Professional — frozen baseline

| Slice | Baseline status |
|---|---|
| F12.1 Safe Redaction | CLOSED / AUTO PASS |
| F12.2 PFX/P12 Cryptographic Signature | CLOSED / AUTO PASS |
| F12.3 Existing AcroForm `/Tx` | CLOSED / AUTO PASS |
| F12.4 Compare | CLOSED / AUTO PASS — core + minimal UI |
| F12.5 Batch | CLOSED / AUTO PASS — TextWatermark only |
| F12.6 Code Generator | CLOSED / AUTO PASS — QR PNG only |
| F12.7 Audited Conversions | **CLOSED / AUTO PASS — PDF→PNG only** |

Any additional batch operation, symbology, conversion, form type or signing technology requires a new explicit value/capability decision and does not silently extend F12.

## F12.7 closed MVP — PDF→PNG

Flow:
`open PDF → Herramientas → Exportar PDF a PNG... → choose folder → PdfDocumentSession/PDFium → one PNG/page`.

Contract:
- current/open PDF only;
- all pages only;
- 144 DPI fixed baseline;
- deterministic `<basename>-pagina-001.png` naming;
- originals untouched;
- target collisions rejected before writing;
- `FileMode.CreateNew` blocks race-condition overwrite;
- partial outputs from the current operation are cleaned best-effort on later failure;
- core cancellation supported;
- minimal UI with folder selection and controlled status/errors;
- existing PDFium + WPF encoder only;
- no runtime/package addition.

Not baseline:
- JPEG/WebP/TIFF;
- page ranges;
- configurable DPI/quality studio;
- image→PDF;
- Office↔PDF / PDF→Office;
- LibreOffice/Office automation;
- cloud conversion;
- second PDF/document engine;
- generalized conversion framework/registry/plugins/queue.

## Release QA packs

All four packs now have a canonical automated-evidence preflight plus an executable real/manual checklist. **This does not mean the real QA was executed.**

| Pack | Canonical record | Automated preflight | Real/manual gate |
|---|---|---|---|
| A — Reader / Organize / Editing | `docs/release/2026-10-14-pack-a-reader-organize-editing.md` | PASS | NOT RUN — ENVIRONMENT |
| B — ZPL / QR / physical print-scan | `docs/release/2026-10-14-pack-b-zpl-qr-physical.md` | PASS | NOT RUN — ENVIRONMENT |
| C — visual + cryptographic signatures | `docs/release/2026-10-14-pack-c-signatures.md` | PASS | NOT RUN — ENVIRONMENT |
| D — OCR / Forms / Compare / Batch / PDF→PNG | `docs/release/2026-10-14-pack-d-ocr-forms-compare-batch-convert.md` | PASS | NOT RUN — ENVIRONMENT |

Exit for each real pack remains: representative evidence actually executed, no open P0/P1, limitations documented, and no synthetic/CI PASS promoted into a physical/manual PASS.

## Remaining sequence

### 1. Real QA execution — OPEN / environment-gated

Execute Packs A–D on suitable Windows/hardware/private-fixture environments. Until then their final gates remain NOT RUN.

### 2. Documentation routing consolidation — CURRENT BLOCK

Only fix documents that can materially misroute execution:
- `.planning/STATE.md` remains the daily operational router;
- this roadmap reflects Pack A–D preflight completion;
- `AGENTS.md` must reflect the closed F0–F12 baseline and current rules;
- `docs/MASTER_PLAN.md` remains useful for architectural/history detail but must be explicitly marked non-authoritative for current execution order;
- avoid Markdown-per-commit noise and do not rewrite historical evidence merely because it is old.

### 3. Integration audit — NEXT

Before touching `main`:
- map branch and PR ancestry;
- identify unique/product commits;
- classify historical/noise branches;
- verify that Pack A–D planning branches form only documentation lineage over the closed functional baseline;
- choose one conservative integration candidate;
- verify candidate exact-head CI;
- no destructive cleanup during audit.

### 4. Integration candidate

Use a dedicated integration branch only. Required gates:
- clean ancestry from authoritative functional head;
- no replay of historical PRs if already contained;
- locked restore;
- Release build/test;
- representative QA evidence;
- installer/runtime/license review.

### 5. Release Candidate

RC requires:
- clean-clone reproducibility;
- locked restore;
- Release build/tests;
- packaged runtime/installer smoke;
- offline smoke;
- no P0/P1 blockers;
- no feature creep.

### 6. `main`

`main` moves only with explicit user approval after integration + RC evidence. No automatic merge.

## Engineering laws

### KISS
Simple, efficient, optimized, effective. Prefer A→B over generalized architecture.

### Anti-noise
Before ADD: DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD.

### Anti-patch operational guardrail
`REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.

## Branch hygiene notes

Authoritative closed functional F12 head:
`feat/f12-7-pdf-to-png-mvp-green3` @ `8e1845d6d8fe72374d7a80994d8500b6d36077cc`.

Release-QA preflight lineage:
- `plan/release-pack-a-qa`;
- `plan/release-pack-b-qa`;
- `plan/release-pack-c-qa`;
- `plan/release-pack-d-qa`.

Do not treat these as authoritative functional bases:
- divergent historical `feat/f12-7-pdf-to-png`;
- old RED/checkpoint `feat/f12-7-pdf-to-png-mvp`;
- accidental empty aliases `...-green` and `...-green2`.

They are release-hygiene items, not functional blockers.

## Next bounded task

Run the **integration ancestry / PR audit** from the latest audited release-routing checkpoint, then select one conservative integration candidate. Do not merge to `main` and do not clean branches destructively during the audit.

No merge to `main` without explicit user approval.
