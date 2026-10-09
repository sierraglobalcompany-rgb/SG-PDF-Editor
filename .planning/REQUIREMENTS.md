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

- [x] **READER-01 — AUTO PASS** Continuous vertical virtualized reading + lazy thumbnails, no eager full-document full-resolution render.
- [x] **READER-02 — AUTO PASS** Local PDFium Unicode search + one-page selection/copy; image-only PDFs require later OCR.
- [x] **READER-03 — AUTO PASS** Read-only cycle-safe bookmarks + explicit internal links and confirmed HTTP/HTTPS URI links only.
- [x] **READER-04 — AUTO PASS** Protected PDF open/retry/cancel with no password persistence.
- [x] **READER-05 — AUTO PASS** Reader shortcuts + atomic max-10 recents, no startup/menu target probing; no multi-document tabs.
- [x] **READER-06 — AUTO PASS** Preserve F3 architecture: continuous LEER; existing single-active-page FIRMAR path.
- [x] **READER-07 — AUTO PASS** Full-page bitmap retention bounded to visible + one neighbor each side; lazy thumbnails; stale publication rejected.

F4 closure `1d620f1b2b9717aab35671e18a9dc78f28a8afdd`; draft PR #23 open/unmerged. Real Windows reader QA: **NOT RUN**.

## F5 — Organize — AUTOMATED CLOSURE PASS

Automated closure is valid when the exact Task-10 checkpoint head has both push and PR Windows CI green. Pre-checkpoint evidence: functional CI `37990505245` attempt 2, docs push CI `37991289080` and PR CI `37991479820` all PASS with Release build 0 warnings/errors and 550/550 tests. Final exact-head run IDs are recorded in PR #24 conversation after they complete.

- [x] **ORG-01 — AUTO PASS** Move/reorder/rotate/delete/duplicate pages through immutable `OrganizePlan`; multi-selection preserves visual order; delete-all is blocked; output is materialized only through Save As and reopened/validated.
- [x] **ORG-02 — AUTO PASS** Insert selected ranges, merge/append, extract selection and split by every-N/explicit ranges reuse the same page-reference plan/materializer; split collisions are checked before first write and batch stops on first failure.
- [x] **ORG-03 — AUTO PASS** Structural preflight covers cryptographic signatures, password-opened sources, forms, bookmarks, named destinations, internal links, tagged structure, page labels, attachments and representative metadata. Signatures/password-opened sources are hard-blocked. Evidence-backed changed/lost non-crypto structures require explicit warning confirmation.
- [x] **ORG-04 — AUTO PASS** Required APIs are verified against pinned PDFium; no second PDF engine or new runtime dependency was introduced.

Preservation evidence: `docs/history/2026-10-09-F5-preservation-matrix.md`. Representative identity-import fixtures prove the current writer changes/loses forms, bookmarks, internal links, named destinations, tagged structure, page labels, attachments and original metadata values. F5 never presents those structures as preserved.

Task-10 hardening adds dirty-plan protection when opening another PDF or leaving ORGANIZAR; successful Save As clears dirty state. Existing stale-source, temp cleanup, cancellation, bounded-thumbnail and offline-runtime tests remain part of the full 550-test suite.

Real Windows organize drag/drop/large-document UX, Save As, insert/merge/extract/split, warning/block dialogs and offline smoke: **NOT RUN**.

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
- [x] **ORIGINAL** Edit/sign/organize flows protect source and use Save As behavior.
- [x] **CI** Completed automated slices through F5 have exact-head Windows CI evidence; final F5 exact-head evidence is linked from PR #24 conversation.
- [x] **KISS** No preventive enterprise architecture, generic PDF object graph, second PDF engine or dependency expansion through F5 closure audit.
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
