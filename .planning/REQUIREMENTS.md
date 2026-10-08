# Requirements — SG PDF Editor

**Architecture source:** `docs/MASTER_CONTEXT.md` + `docs/MASTER_PLAN.md`.  
**Execution/status source:** GitHub exact heads/CI + `.planning/STATE.md`.  
**Purpose:** concise traceability; approved slice specs are authoritative for detailed acceptance.

## Status Semantics

- **AUTO PASS** = automated implementation + CI evidence exists.
- **NOT RUN** = required real/private/physical QA not executed.
- Unchecked boxes remain open where implementation/acceptance is pending.

## F0 — PDF Base

Automated implementation PDF-BASE-01..07: **AUTO PASS**. Physical Windows/print/offline smoke: **NOT RUN**.

- [ ] **PDF-BASE-01** Open local PDF from UI.
- [ ] **PDF-BASE-02** Render real pages with PDFium without normal UI blocking.
- [ ] **PDF-BASE-03** Previous/next/go-to-page navigation.
- [ ] **PDF-BASE-04** Zoom + Fit Page + Fit Width.
- [ ] **PDF-BASE-05** Cancel/ignore obsolete render requests and prioritize current view.
- [ ] **PDF-BASE-06** Print through Windows, including Microsoft Print to PDF.
- [ ] **PDF-BASE-07** Operate with network disabled.

## F1 — Gate ZPL-A

Synthetic Gate PASS; Labelize 1.7.0 selected. Private real Mercado Libre corpus **NOT RUN**.

- [ ] **ZPL-GATE-01** Compare BinaryKits.Zpl vs Labelize with synthetic + private real corpus.
- [x] **ZPL-GATE-02** Synthetic validation for required ZPL commands/content.
- [x] **ZPL-GATE-03** Synthetic benchmark without multiplying work by `^PQ`.
- [x] **ZPL-GATE-04** Select engine using fidelity/performance/packaging/license evidence -> Labelize 1.7.0.

## F2 — ZPL Labels

F2.1–F2.6 automated pipeline: **AUTO PASS**. Private corpus + physical printer/ruler/scanner **NOT RUN**.

- [x] **LABEL-01** Open `.zpl`, `.txt`, `.prn` locally.
- [x] **LABEL-02** Separate designs and preserve `^PQ` as metadata.
- [x] **LABEL-03** Local Labelize preview per design.
- [x] **LABEL-04** File / one-each / custom quantity.
- [x] **LABEL-05** Layouts + thermal/A4/Letter/custom media.
- [x] **LABEL-06** Export PDF without intentional barcode deformation.
- [ ] **LABEL-07** Windows-driver thermal printing exact physical size — automated preflight PASS; physical print NOT RUN.
- [ ] **LABEL-08** Barcode/QR validation — automated decode PASS; physical scanner NOT RUN.

## F3 — Visual Signature

### F3.1 Core — AUTO PASS

- [x] **SIGN-01** Import transparent PNG.
- [x] **SIGN-02** Move/proportional resize/duplicate/delete placements.
- [x] **SIGN-03** Correct device↔PDF coordinate mapping.
- [x] **SIGN-04** Save copy + reopen/render preserving placement/transparency.

Manual real-signature UX/save/open: **NOT RUN**.

### F3.2 Photo/scan — AUTO PASS

- [x] **SIGN-05** Create transparent `SignatureAsset` locally from PNG/JPG/JPEG with bounded cleanup/adjustments and full-resolution Apply.

Real phone/scanner QA: **NOT RUN**.

### F3.3 Draw — AUTO PASS

- [x] **SIGN-06** Draw locally with WPF InkCanvas/StrokeCollection and reuse existing `SignatureAsset` + `AddSignatureAsset(...)` path.

Mouse/touch/stylus hardware QA: **NOT RUN**.

### F3.4 Local Library — AUTO PASS

- [x] **SIGN-07** Persist/reuse transparent `SignatureAsset` entries under LocalAppData using versioned JSON + GUID PNG, Use/Save selected/Rename/Delete/Close, no cloud/database/network and exactly one existing `AddSignatureAsset(...)` path when used.

Closure head `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b`; closure CI `37837613618` PASS; PR CI `37837833517` PASS; 301 tests. Real Windows QA: **NOT RUN**.

## F4 — Full Reader — IMPLEMENTATION-PLAN GATE

Formal design: `docs/superpowers/specs/2026-10-08-f4-full-reader-design.md` — **APPROVED by user 2026-10-08**.  
TDD implementation plan: `docs/superpowers/plans/2026-10-08-f4-full-reader.md` — **WRITTEN + SELF-AUDITED, awaiting user approval**.  
Product code: **NOT STARTED**.

- [ ] **READER-01** Continuous vertical virtualized reading + lazy thumbnails, no eager full-document full-resolution render.
- [ ] **READER-02** Local PDFium search + one-page text selection/copy; image-only PDFs correctly require later OCR.
- [ ] **READER-03** Read-only cycle-safe bookmarks + explicit PDF internal links and confirmed HTTP/HTTPS URI links only.
- [ ] **READER-04** Password-protected PDF open/retry/cancel flow with no password persistence.
- [ ] **READER-05** Reader shortcuts + max-10 local recent-file paths, no startup path probing; no multi-document tabs in F4.
- [ ] **READER-06** Preserve existing F3 architecture: LEER continuous; FIRMAR uses existing single-active-page `PdfImage`/`SignatureEditState` path.
- [ ] **READER-07** Visible full-page bitmap retention bounded to visible pages + one neighbor before/after; lazy thumbnails; stale render publication rejected.

No READER item becomes PASS from spec or plan approval alone. Each requires its owning RED/GREEN implementation evidence plus exact-head CI; manual Windows QA remains separately reported.

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
- [x] **LICENSE** Current runtime dependencies permissive/audited for current development scope; Labelize font provenance re-audit before public installer.
- [x] **PRIVACY** Private fixtures ignored and CI hygiene rejects tracked `tests/PrivateFixtures/**`.
- [x] **ORIGINAL** Early edit/sign flows protect source and use Save As behavior.
- [x] **CI** Every completed automated slice has final-head Windows CI evidence.
- [x] **KISS** No preventive enterprise architecture/dependency expansion detected through F4 planning gate.
- [x] **NO-AUTOMERGE** Main unchanged; merges require explicit user approval.

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
