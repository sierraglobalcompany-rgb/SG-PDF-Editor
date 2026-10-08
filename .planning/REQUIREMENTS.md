# Requirements — SG PDF Editor

**Architecture source:** `docs/MASTER_CONTEXT.md` + `docs/MASTER_PLAN.md`.  
**Execution/status source:** GitHub exact heads/CI + `.planning/STATE.md`.  
**Purpose:** concise traceability; slice specs remain authoritative for detailed acceptance.

## Status Semantics

- **AUTO PASS** = automated implementation and CI evidence exists.
- **NOT RUN** = required real/private/physical QA has not been executed; never infer PASS from CI.
- Unchecked boxes remain open where final acceptance includes pending physical/private evidence or work has not yet been implemented.

## F0 — PDF Base

Automated implementation for PDF-BASE-01..07: **AUTO PASS**. Physical Windows/print/offline smoke: **NOT RUN**.

- [ ] **PDF-BASE-01** Open a local PDF from UI.
- [ ] **PDF-BASE-02** Render real pages with PDFium without blocking normal UI interaction.
- [ ] **PDF-BASE-03** Previous/next/go-to-page navigation.
- [ ] **PDF-BASE-04** Zoom + Fit Page + Fit Width.
- [ ] **PDF-BASE-05** Cancel/ignore obsolete render requests and prioritize current view.
- [ ] **PDF-BASE-06** Print through Windows, including Microsoft Print to PDF.
- [ ] **PDF-BASE-07** Operate with network disabled.

## F1 — Gate ZPL-A

Synthetic Gate: **PASS**; Labelize 1.7.0 selected. Private real Mercado Libre corpus: **NOT RUN**.

- [ ] **ZPL-GATE-01** Compare BinaryKits.Zpl vs Labelize with synthetic + private real corpus.
- [x] **ZPL-GATE-02** Synthetic validation for `^CI28`, `^FH`, `^FB`, `^FR`, `^GFA`, `^BC`, `^BQ`, `^PQ`, `^DF`, `^XF`.
- [x] **ZPL-GATE-03** Synthetic benchmark without multiplying work by `^PQ`.
- [x] **ZPL-GATE-04** Select one engine using fidelity/codes/performance/packaging/license evidence → Labelize 1.7.0.

`ZPL-GATE-01` stays open until the private corpus is executed.

## F2 — ZPL Labels

F2.1–F2.6 automated pipeline: **AUTO PASS**. Private corpus + physical printer/ruler/scanner: **NOT RUN**.

- [x] **LABEL-01** Open `.zpl`, `.txt`, `.prn` locally.
- [x] **LABEL-02** Separate designs and preserve `^PQ` quantity as metadata.
- [x] **LABEL-03** Local Labelize preview per design.
- [x] **LABEL-04** File / one-each / custom quantity.
- [x] **LABEL-05** Layout 1/2/3/4/6/8/10/12/custom + thermal/A4/Letter/custom media.
- [x] **LABEL-06** Export PDF without intentional barcode deformation.
- [ ] **LABEL-07** Windows-driver thermal printing at exact physical size — automated preflight PASS; physical print **NOT RUN**.
- [ ] **LABEL-08** Barcode/QR validation — automated decode PASS; physical scanner **NOT RUN**.

## F3 — Visual Signature

### F3.1 Core — AUTO PASS

- [x] **SIGN-01** Import a transparent PNG into the visual-signature flow.
- [x] **SIGN-02** Move, proportional resize, duplicate and delete placements.
- [x] **SIGN-03** Correct UI/device ↔ PDF coordinate mapping.
- [x] **SIGN-04** Save as copy and reopen/render preserving placement/transparency semantics.

Manual real-signature UX/save/open remains **NOT RUN**.

### F3.2 Photo/scan — AUTO PASS

- [x] **SIGN-05** Create a transparent `SignatureAsset` locally from PNG/JPG/JPEG photo/scan with bounded brightness/contrast, white-paper cleanup, Original/Black/Blue, auto-crop and full-resolution Apply.

Real phone/scanner photo-quality QA remains **NOT RUN**.

### F3.3 Draw — AUTO PASS

- [x] **SIGN-06** Draw a signature locally with WPF InkCanvas/StrokeCollection, fixed black/blue + three widths + dialog-local Undo/Redo/Clear, producing the existing transparent `SignatureAsset` and reusing `AddSignatureAsset(...)`.

Real mouse/touch/stylus hardware QA remains **NOT RUN**.

### F3.4 Local Library — CURRENT

- [ ] **SIGN-07** Persist/reuse transparent `SignatureAsset` entries under local app data using a small versioned JSON manifest + GUID PNG files, with Use/Save selected/Rename/Delete/Close, no cloud/database/network and reuse through exactly one existing `AddSignatureAsset(...)` call.

Written design spec exists; implementation has **NOT STARTED**.

## F4 — Full Reader

- [ ] **READER-01** Continuous scroll and thumbnails.
- [ ] **READER-02** Search and copy text.
- [ ] **READER-03** Bookmarks and links.
- [ ] **READER-04** Password PDFs.
- [ ] **READER-05** Shortcuts/recent files; tabs only if KISS/stability allow.

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
- [ ] **TEXT-03** Redistributable TTF fallback for new code points/subset limitations.
- [ ] **TEXT-04** Basic properties + save/reopen validation.

## F8–F12

- [ ] **COMMENTS** Highlight, underline/strike, notes, ink/shapes.
- [ ] **UTILS** Only justified offline utilities.
- [ ] **OCR** Local Tesseract searchable-text workflow.
- [ ] **TEXT-V2** Reading order/lines/paragraphs/limited reflow.
- [ ] **PRO** Cryptographic signing, forms, true redaction, compare, batch and audited conversions.

## Cross-cutting

- [ ] **OFFLINE** Normal product functions do not require Internet; automated guards exist, full physical/offline smoke pending.
- [x] **LICENSE** Current runtime dependencies are permissive/audited for current development scope; Labelize font provenance must be re-audited before public installer.
- [x] **PRIVACY** Private fixtures are ignored and CI hygiene rejects tracked `tests/PrivateFixtures/**`.
- [x] **ORIGINAL** Early edit/sign flows protect source and use Save As behavior.
- [x] **CI** Every completed automated slice has final-head Windows CI evidence.
- [x] **KISS** No preventive enterprise architecture/dependency expansion detected through F3.4 spec gate.
- [x] **NO-AUTOMERGE** Main remains unchanged; merges require explicit user approval.

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
