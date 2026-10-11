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

- [x] **ORG-01 — AUTO PASS** Move/reorder/rotate/delete/duplicate pages through immutable `OrganizePlan`; multi-selection preserves visual order; delete-all is blocked; output is materialized only through Save As and reopened/validated.
- [x] **ORG-02 — AUTO PASS** Insert selected ranges, merge/append, extract selection and split by every-N/explicit ranges reuse the same page-reference plan/materializer; split collisions are checked before first write and batch stops on first failure.
- [x] **ORG-03 — AUTO PASS** Structural preflight covers cryptographic signatures, password-opened sources, forms, bookmarks, named destinations, internal links, tagged structure, page labels, attachments and representative metadata. Signatures/password-opened sources are hard-blocked. Evidence-backed changed/lost non-crypto structures require explicit warning confirmation.
- [x] **ORG-04 — AUTO PASS** Required APIs are verified against pinned PDFium; no second PDF engine or new runtime dependency was introduced.

Preservation evidence: `docs/history/2026-10-09-F5-preservation-matrix.md`. Real Windows organize QA: **NOT RUN**.

## F6 — Images — AUTOMATED CLOSURE PASS

Closure `c6d762efca01d50bfe3932d1f05617190a464fc6`; draft PR #25 open/unmerged.

- [x] **IMG-01 — AUTO PASS** Detect/select real PDF image page objects on the active page with deterministic rotated/overlap hit-testing.
- [x] **IMG-02 — AUTO PASS** Visually faithful PNG extraction + PNG/JPEG replacement with geometry preservation and managed replacement bytes.
- [x] **IMG-03 — AUTO PASS** Move/resize/rotate/delete + opacity + z-order with exact-runtime and save/reopen evidence.
- [x] **IMG-04 — AUTO PASS** Undo/redo, dirty-state integration and successful-save baseline semantics.

Independent F6 preservation evidence: `docs/history/2026-10-09-F6-preservation-matrix.md`. Real Windows image-edit QA: **NOT RUN**.

## F7 — Text V1 — AUTOMATED FUNCTIONAL CLOSURE PASS

Task-13 exact-head evidence: `b31a75cf99207e2e6ac9072b50c5a1ac5fa32d05`, workflow `38089770428`, Release build **0 warnings / 0 errors**, **766/766 tests PASS**.

- [x] **TEXT-01 — AUTO PASS** Detect/select top-level real PDF text objects on the active page. Unicode, matrix, bounds/quad, font, size, fill and render mode are captured in managed snapshots; mixed image/text topmost selection uses `PageObjectIndex`.
- [x] **TEXT-02 — AUTO PASS** Conservative in-place text editing where safe. `TextEditPolicy` keeps unsupported render modes read-only, rejects invalid content/size/coverage and selects `OriginalFont` only under the proven conservative rules.
- [x] **TEXT-03 — AUTO PASS** Redistributable fallback is pinned DejaVu Sans 2.37 with provenance/license/archive+TTF hashes. Fallback uses PDFium CID Type2 with explicit `ToUnicode` and `CIDToGIDMap`; runtime does not download fonts and no new NuGet was added.
- [x] **TEXT-04 — AUTO PASS** Basic Texto V1 properties + combined save/reopen validation. The combined validator proves exact Unicode, TEXT type, matrix, size, fill, expected font route and valid render before atomic publication.

Independent F7 preservation evidence: `docs/history/2026-10-10-F7-preservation-matrix.md`. Real Windows text-edit UX/offline/manual QA: **NOT RUN**.

## F8 — Comments V1 — AUTOMATED CLOSURE PASS

Closure `7e092166b04d72e2b8d4d1ba2d95d48e1acdebb3`; stacked draft PR #27 open/draft/unmerged.

- [x] **COMMENTS — AUTO PASS** Real PDF annotations in independent `COMENTAR` mode: Select, Highlight, Underline, Strikeout, Note, Ink, Rectangle and Ellipse. Supported native subtypes are `TEXT`, `SQUARE`, `CIRCLE`, `HIGHLIGHT`, `UNDERLINE`, `STRIKEOUT` and `INK`.
- [x] **COMMENTS — AUTO PASS** Stable managed structural identity + deterministic hit-testing; raw annotation index is never durable authority and duplicate/source mismatch fails closed.
- [x] **COMMENTS — AUTO PASS** Transactional Save As uses fresh-source materialization, reopen/validation, source-race detection, atomic publication and sibling-temp cleanup; password-opened and cryptographically signed sources remain blocked.
- [x] **COMMENTS — AUTO PASS** Fresh F8 preservation matrix proves representative forms, bookmarks, named destinations, internal links, tagged structure, page labels, attachments/payload, metadata, page rotation, supported comments and unsupported `STAMP` preservation/read-only behavior.
- [x] **COMMENTS — AUTO PASS** Lifecycle proven across independent reopen cycles: create → save → reopen → edit → save → reopen → delete → save → reopen.

Preservation evidence: `docs/history/2026-10-10-F8-preservation-matrix.md`. Real Windows/stylus/touch/Adobe/Foxit/Edge compatibility QA: **NOT RUN**.

## F9 — Utilities — AUTOMATED CLOSURE PASS

Task 9 closure-candidate exact-head evidence: commit `9f48c0a3c6c9358ea7c9c37a1a9f5973921f5b1d`, workflow `38118308492`, job `114407617996`, Release build **0 warnings / 0 errors**, **942/942 tests PASS**.

- [x] **UTILS — AUTO PASS** Text watermark and page/Bates numbering are user-facing WPF commands with transactional Save As/reopen validation and deterministic Unicode/range/formatting behavior.
- [x] **UTILS — AUTO PASS** Authorized AES-256 open-password protection is user-facing; PDFium creation capability was NO-GO, so the single pinned fallback qpdf 12.4.2 is used locally/offline. Password bypass/removal is absent; signed sources fail closed; permission flags are intentionally not exposed.
- [x] **UTILS — AUTO PASS** Structural/lossless qpdf optimize is characterized GO with a fresh representative preservation matrix and no promise that output must be smaller. Task 8 exposes no WPF Optimize command.
- [x] **UTILS — AUTO PASS** Generic Repair is NO-GO from the measured synthetic corpus, so no placebo Repair command is exposed.
- [x] **UTILS — AUTO PASS** Image-watermark native primitives are characterized, but no product command is exposed because no transactional image-overlay writer exists in F9.

Preservation evidence: `docs/history/2026-10-11-F9-preservation-matrix.md`. Real Windows utility UX/offline/manual Adobe/Foxit/Edge compatibility QA and private malformed-PDF repair corpus: **NOT RUN**.

## F10–F12

- [ ] **OCR** Local Tesseract searchable-text workflow.
- [ ] **TEXT-V2** Reading order/lines/paragraphs/limited reflow.
- [ ] **PRO** Cryptographic signing, forms, true redaction, compare, batch and audited conversions.

## Cross-cutting

- [ ] **OFFLINE** Normal functions require no Internet; full physical/offline smoke remains pending where noted.
- [x] **LICENSE** Runtime dependencies/assets audited through F9; qpdf 12.4.2 is pinned by archive digest with Apache-2.0 + NOTICE. Labelize font provenance re-audit remains required before public installer.
- [x] **PRIVACY** Private fixtures ignored and CI hygiene rejects tracked `tests/PrivateFixtures/**`.
- [x] **ORIGINAL** Edit/sign/organize/comment and F9 utility writers protect the source and use Save As behavior.
- [x] **CI** Automated slices through F9 have Windows CI evidence; F9 closure-candidate exact-head ran 942/942 tests green, and the final status commit is required to remain exact-head green before the stacked draft PR is considered closed.
- [x] **KISS** No preventive enterprise architecture or generic PDF object graph. qpdf is the single narrowly justified F9 sidecar after PDFium capability NO-GO; no pdfcpu fallback or runtime service layer was added.
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
