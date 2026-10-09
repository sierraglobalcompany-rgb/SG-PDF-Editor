# Requirements — SG PDF Editor

**Architecture source:** `docs/MASTER_CONTEXT.md` + `docs/MASTER_PLAN.md`.  
**Execution/status source:** GitHub exact heads/CI + `.planning/STATE.md`.  
**Purpose:** concise traceability; approved slice specs are authoritative for detailed acceptance.

## Status Semantics

- **AUTO PASS** = automated implementation + CI evidence exists.
- **NOT RUN** = required real/private/physical QA not executed.
- Automated PASS never implies physical/manual PASS.

## F0 — PDF Base

Automated F0.1–F0.6: **AUTO PASS**. Physical Windows/print/offline smoke: **NOT RUN**.

- [ ] **PDF-BASE-01** Open local PDF from UI — automated PASS; physical gate open.
- [ ] **PDF-BASE-02** Render real pages with PDFium without normal UI blocking — automated PASS; physical gate open.
- [ ] **PDF-BASE-03** Previous/next/go-to-page navigation — automated PASS; physical gate open.
- [ ] **PDF-BASE-04** Zoom + Fit Page + Fit Width — automated PASS; physical gate open.
- [ ] **PDF-BASE-05** Cancel/ignore obsolete render requests and prioritize current view — automated PASS; physical gate open.
- [ ] **PDF-BASE-06** Print through Windows, including Microsoft Print to PDF — automated PASS; physical gate open.
- [ ] **PDF-BASE-07** Operate with network disabled — automated PASS; physical gate open.

## F1 — Gate ZPL-A

Synthetic Gate PASS; Labelize 1.7.0 selected. Private real Mercado Libre corpus **NOT RUN**.

- [ ] **ZPL-GATE-01** Synthetic + private real corpus comparison — private corpus still open.
- [x] **ZPL-GATE-02** Synthetic required-command/content validation.
- [x] **ZPL-GATE-03** Synthetic benchmark without multiplying `^PQ` work.
- [x] **ZPL-GATE-04** Engine selection → Labelize 1.7.0.

## F2 — ZPL Labels

F2.1–F2.6 automated pipeline: **AUTO PASS**. Private corpus + physical printer/ruler/scanner: **NOT RUN**.

- [x] **LABEL-01** Open `.zpl`, `.txt`, `.prn` locally.
- [x] **LABEL-02** Separate designs and preserve `^PQ` metadata.
- [x] **LABEL-03** Local Labelize preview.
- [x] **LABEL-04** File / one-each / custom quantity.
- [x] **LABEL-05** Layouts + thermal/A4/Letter/custom media.
- [x] **LABEL-06** PDF export without intentional barcode deformation.
- [ ] **LABEL-07** Thermal print exact physical size — automated preflight PASS; physical print NOT RUN.
- [ ] **LABEL-08** Barcode/QR — automated decode PASS; physical scanner NOT RUN.

## F3 — Visual Signature

- [x] **SIGN-01** Transparent PNG import — AUTO PASS.
- [x] **SIGN-02** Move/proportional resize/duplicate/delete — AUTO PASS.
- [x] **SIGN-03** Device↔PDF coordinate mapping — AUTO PASS.
- [x] **SIGN-04** Save copy + reopen/render — AUTO PASS.
- [x] **SIGN-05** Local photo/scan preparation → `SignatureAsset` — AUTO PASS.
- [x] **SIGN-06** WPF InkCanvas drawing via existing asset/placement path — AUTO PASS.
- [x] **SIGN-07** Local signature library JSON + GUID PNG, no cloud/database/network — AUTO PASS.

F3.4 closure `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b`; PR #22 draft/open/unmerged. Corresponding real Windows/photo/hardware/library QA: **NOT RUN**.

## F4 — Full Reader — AUTOMATED CLOSURE PASS

Design and TDD plan approved. Tasks 1–9 automated closure evidence exists. Draft PR #23 is open/unmerged on the exact F3.4 base. Manual Windows QA remains **NOT RUN**.

- [x] **READER-01 — AUTO PASS** Continuous vertical virtualized reading + lazy thumbnails, no eager full-document full-resolution render. Evidence: Tasks 1–3; CIs `37843337925`, `37852149659`, `37855511813`.
- [x] **READER-02 — AUTO PASS** Local PDFium Unicode search + one-page selection/copy; image-only PDFs require later OCR. Evidence: Tasks 5–6; CIs `37867206589`, `37870778333`.
- [x] **READER-03 — AUTO PASS** Read-only cycle-safe bookmarks + explicit internal links and confirmed HTTP/HTTPS URI links only. Evidence: Task 7; CI `37872707082`.
- [x] **READER-04 — AUTO PASS** Protected PDF open/retry/cancel with no password persistence. Evidence: Task 4; CI `37857787178`.
- [x] **READER-05 — AUTO PASS** Reader shortcuts + atomic max-10 recents, no startup/menu target probing; no multi-document tabs. Evidence: Task 8; CI `37875513602`.
- [x] **READER-06 — AUTO PASS** Preserve F3 architecture: continuous LEER; existing single-active-page FIRMAR `PdfImage`/`SignatureEditState`. Full F4 diff contains no `Features/Sign` file changes.
- [x] **READER-07 — AUTO PASS** Full-page bitmap retention bounded to visible + one neighbor each side; lazy thumbnails; stale publication rejected. Evidence: Tasks 1–3.

Closure evidence before the final docs-only state commit:

- audited checkpoint `c36a8630e685cb143b312a0f260301b87a62bbd5` → CI `37875773158` PASS, 417 tests, build 0 warnings / 0 errors;
- full F3.4→F4 audit: 118 ahead / 0 behind; no project/lock dependency changes, second engine, WebView/network runtime, tabs, OCR, database, password persistence or F5+ scope;
- Task-9 docs checkpoint `48a79b412f5b4b2903c954f79ab37e420f87e6ae` → push CI `37890536672` PASS and PR CI `37890669463` PASS;
- PR #23 `F4 — Full Reader`: draft/open/unmerged, base `feat/f3-4-local-signature-library`, head `feat/f4-full-reader`;
- `main` verified unchanged at `31c0594758a83ec555d73ecdd7c597cdf8791fd7`.

The current exact branch head carrying the final state docs must also keep push + PR checks green; GitHub exact-head checks are authoritative.

F4 real Windows performance/UX/protected-PDF/search/selection/bookmark/link/shortcuts/recents/offline QA: **NOT RUN**.

## F5 — Organize

- [ ] **ORG-01** Move/reorder/rotate/delete/duplicate pages.
- [ ] **ORG-02** Insert/extract/merge/split.
- [ ] **ORG-03** Preflight signatures/forms/bookmarks/destinations and relevant structures.
- [ ] **ORG-04** PDFium first; another utility only for demonstrated gaps.

## F6 — Images

- [ ] **IMG-01** Detect/select images and contextual actions.
- [ ] **IMG-02** Extract/save and replace while preserving geometry where viable.
- [ ] **IMG-03** Move/resize/rotate/opacity/z-order/delete.
- [ ] **IMG-04** Undo/redo.

## F7 — Text V1

- [ ] **TEXT-01** Detect/select text objects.
- [ ] **TEXT-02** Conservative in-place text editing where safe.
- [ ] **TEXT-03** Redistributable TTF fallback for new code points/subset limits.
- [ ] **TEXT-04** Basic properties + save/reopen validation.

## F8–F12

- [ ] **COMMENTS** Highlight, underline/strike, notes, ink/shapes.
- [ ] **UTILS** Only justified offline utilities.
- [ ] **OCR** Local Tesseract searchable-text workflow.
- [ ] **TEXT-V2** Reading order/lines/paragraphs/limited reflow.
- [ ] **PRO** Cryptographic signing, forms, true redaction, compare, batch and audited conversions.

## Cross-cutting

- [ ] **OFFLINE** Normal functions require no Internet; full physical/offline smoke remains pending where noted.
- [x] **LICENSE** Current runtime dependencies audited for current development scope; Labelize font provenance re-audit before public installer.
- [x] **PRIVACY** Private fixtures ignored and CI hygiene rejects tracked `tests/PrivateFixtures/**`.
- [x] **ORIGINAL** Early edit/sign flows protect source and use Save As behavior.
- [x] **CI** Completed automated slices have exact-head Windows CI evidence; final docs-only F4 head must remain green on push + PR checks.
- [x] **KISS** No preventive enterprise architecture/dependency expansion through F4 closure audit.
- [x] **NO-AUTOMERGE** Main remains untouched; merges require explicit user approval.

## Traceability

| GSD Phase | Product Phase | Requirements |
|---:|---|---|
| 1 | F0 PDF Base | PDF-BASE-* |
| 2 | F1 Gate ZPL-A | ZPL-GATE-* |
| 3 | F2 ZPL Workspace | LABEL-* |
| 4 | F3 Visual Signature | SIGN-* |
| 5 | F4 Full Reader | READER-* |
| 6 | F5 Organize | ORG-* |
| 7 | F6 Images | IMG-* |
| 8 | F7 Text V1 | TEXT-* |
| 9 | F8 Comments | COMMENTS |
| 10 | F9 Utilities | UTILS |
| 11 | F10 OCR | OCR |
| 12 | F11 Text V2 | TEXT-V2 |
| 13 | F12 Professional | PRO |
