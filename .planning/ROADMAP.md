# Roadmap — SG PDF Editor

> Operational authority: exact GitHub head/CI → `.planning/STATE.md` → this roadmap → `.planning/REQUIREMENTS.md` → approved slice specs/plans. Historical handoffs are secondary.

## Current baseline — 2026-10-14

**F0–F12 functional automated baseline is CLOSED.**

Authoritative functional branch:
`feat/f12-7-pdf-to-png-mvp-green3`

Exact functional HEAD:
`8e1845d6d8fe72374d7a80994d8500b6d36077cc`

Exact-head CI:
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

Baseline F12 is now feature-frozen. Any additional batch operation, symbology, conversion, form type or signing technology requires a new explicit value/capability decision and does not silently extend F12.

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

## Remaining sequence

### 1. Real QA packs

Execute bounded packs. Automated PASS does not become physical/manual PASS automatically.

**Pack A — Reader / Organize / Editing**
- open representative PDFs in Windows;
- navigation/zoom/continuous reader;
- print path including Microsoft Print to PDF;
- organize operations + saved-output reopen;
- text/image/comment editing representative smoke;
- external-reader check where relevant;
- offline smoke.

**Pack B — ZPL / QR / physical print-scan**
- representative/private ZPL corpus;
- label dimensions;
- thermal print;
- barcode/QR real scanner;
- generated QR real phone/scanner.

**Pack C — visual + cryptographic signatures**
- imported/drawn/photo signature UX;
- representative PFX/P12 corpus;
- external reader validation;
- password handling and failure/cancel paths.

**Pack D — OCR / Forms / Compare / Batch / PDF→PNG**
- representative/private scans;
- `/Tx` field editing and reopen;
- compare representative PDFs;
- batch watermark real files;
- QR UX;
- PDF→PNG visual/export smoke and collision behavior.

### 2. Documentation consolidation

Only fix docs that can misroute execution:
- `AGENTS.md` current operational rules;
- stale master-plan wording marked historical when appropriate;
- one operational source of truth;
- avoid Markdown-per-commit noise.

### 3. Integration audit

Before touching `main`:
- map branch and PR ancestry;
- identify unique/product commits;
- classify historical/noise branches;
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

### Anti-patch
`REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI`.

## Branch hygiene notes

Authoritative closed F12 head:
`feat/f12-7-pdf-to-png-mvp-green3` @ `8e1845d6d8fe72374d7a80994d8500b6d36077cc`.

Do not treat these as authoritative:
- divergent historical `feat/f12-7-pdf-to-png`;
- old RED/checkpoint `feat/f12-7-pdf-to-png-mvp`;
- accidental empty aliases `...-green` and `...-green2`.

They are release-hygiene items, not functional blockers.

## Next bounded task

Start **Pack A automated preflight + executable manual checklist**, unless a newer exact-head release checkpoint already supersedes this roadmap.

No merge to `main` without explicit user approval.
