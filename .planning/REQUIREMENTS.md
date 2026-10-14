# Requirements — SG PDF Editor

**Architecture source:** `docs/MASTER_CONTEXT.md` + `docs/MASTER_PLAN.md`.  
**Execution/status source:** GitHub exact heads/CI + `.planning/STATE.md`.  
**Purpose:** concise traceability; approved slice specs remain authoritative for detailed acceptance.

## Status semantics

- **AUTO PASS** = automated implementation + CI evidence exists.
- **NOT RUN** = required real/private/physical QA not executed.
- Automated PASS never implies physical/manual/external-reader PASS.

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

F3.4 automated closure PASS; corresponding real Windows/photo/hardware/library QA: **NOT RUN**.

## F4 — Full Reader

- [x] **READER-01..07 — AUTO PASS** Continuous virtualized reading, PDFium Unicode search/selection, bookmarks/links, protected-open flow, shortcuts/recents, bounded rendering and preserved signing-mode architecture.

Real Windows reader QA: **NOT RUN**.

## F5 — Organize

- [x] **ORG-01..04 — AUTO PASS** Reorder/rotate/delete/duplicate/insert/merge/extract/split, conservative structural preflight, transactional materialization and pinned-PDFium capability verification.

Preservation evidence: `docs/history/2026-10-09-F5-preservation-matrix.md`. Real Windows organize QA: **NOT RUN**.

## F6 — Images

- [x] **IMG-01..04 — AUTO PASS** Select/detect real image objects, extract/replace, transform/opacity/z-order and undo/redo/save baseline semantics.

Preservation evidence: `docs/history/2026-10-09-F6-preservation-matrix.md`. Real Windows image-edit QA: **NOT RUN**.

## F7 — Text V1

- [x] **TEXT-01..04 — AUTO PASS** Conservative real text-object selection/editing, pinned DejaVu Sans 2.37 CID Type2 fallback with explicit Unicode maps, properties and combined save/reopen validation.

Preservation evidence: `docs/history/2026-10-10-F7-preservation-matrix.md`. Real Windows text-edit UX/offline/manual QA: **NOT RUN**.

## F8 — Comments V1

- [x] **COMMENTS — AUTO PASS** Real PDF annotations in `COMENTAR`, stable managed identity, transactional Save As, independent preservation matrix and create→edit→delete lifecycle.

Preservation evidence: `docs/history/2026-10-10-F8-preservation-matrix.md`. Real Windows/stylus/touch/Adobe/Foxit/Edge QA: **NOT RUN**.

## F9 — Utilities

- [x] **UTILS — AUTO PASS** Text watermark, Bates/page numbering, authorized AES-256 open-password protection with narrowly scoped qpdf 12.4.2; optimize characterized GO without WPF command; generic Repair NO-GO; image-watermark capability not exposed without transactional writer.

Preservation evidence: `docs/history/2026-10-11-F9-preservation-matrix.md`. Real Windows utility UX/external-reader/private malformed-PDF corpus: **NOT RUN**.

## F10 — Local OCR

- [x] **OCR — AUTO PASS** Local/offline Tesseract searchable-text workflow: Spanish/English/bilingual; automatic/current/range; 300-DPI raster + strict TSV geometry; PDFium invisible text writer/validator; transactional Save As; security preflight; progress/cancel; focused WPF command; fresh writer-specific preservation corpus including decoded image payload source→output.

Preservation evidence: `docs/history/2026-10-11-F10-preservation-matrix.md`. Real Windows OCR UX, low-quality/private scans, physical offline, representative hardware and Adobe/Foxit/Edge QA: **NOT RUN**.

## F11 — Text V2

- [x] **TEXT-V2 — AUTO PASS** Deterministic reading order from PDFium geometry; runs→lines→paragraphs; conservative one/two-column classification; explicit `Simple/Ambiguous/Unsupported`; safe one-column homogeneous paragraph reflow over existing text objects only; reuse of Texto V1 transactional Save As/reopen/validate path; minimal WPF UI with Spanish rejection reason; F10 OCR corpus integration; no PdfPig/new PDF dependency required.

Preservation evidence: `docs/history/2026-10-12-F11-preservation-matrix.md`. Real Windows reflow UX, representative private documents, complex real-world layouts, physical offline and Adobe/Foxit/Edge QA: **NOT RUN**.

## F12 — Professional

### F12.1 Safe Redaction

- [x] **PRO-REDACTION — AUTO PASS** True safe redaction for complete simple visible horizontal text objects: conservative policy; underlying object removal; opaque black path; semantic/tagged risk blocked fail-closed; source/fingerprint race protection; reopened-output validation; multi-target; combined text-edit + redaction; minimal existing-panel UI; `Guardar como...` as the only publication path; no new PDF engine/runtime dependency.

Evidence: `docs/history/2026-10-12-F12-1-safe-redaction-closure.md`.

### F12.2 Cryptographic Signature PFX/P12 MVP

- [x] **PRO-SIGN-PFX — AUTO PASS** RSA private-key certificate loading; detached SHA-256 CMS; PDFsharp 6.2.4 signature writer; PDFium signature snapshot; strict one-signature/`ByteRange`/`adbe.pkcs7.detached` validation; ASN.1 exact CMS extraction + `SignedCms.CheckSignature(true)`; transactional temp→independent validate→source fingerprint recheck→publish; existing destination preserved on failure; minimal `Firmar digitalmente` WPF flow; password memory-only; representative synthetic preservation gate; no second PDF engine or new runtime dependency.

Evidence: `docs/history/2026-10-12-F12-2-cryptographic-signature-closure.md`.

Advanced signing such as Windows Certificate Store, smartcard/HSM, TSA/timestamping, revocation policy and legal/regulatory qualification is **not baseline scope** and requires separate approval.

### F12.3 Existing AcroForm text fields

- [x] **PRO-FORMS-TX — AUTO PASS** Inspect, edit and transactionally save existing classic AcroForm `/Tx` fields.
- [x] Flat fields, hierarchical `/Kids`, inherited `/FT /Tx` and names such as `Parent.Child`.
- [x] Preserve source bytes and validate destination value/appearance through a **representative preservation gate**.
- [x] Fail closed for signed/password-protected source paths within the demonstrated save flow.
- [x] Minimal WPF flow reusing existing properties panel and `Guardar como...`.
- [ ] `/Btn`, `/Ch`, XFA, JavaScript/calculations/actions, signature-form fields, flattening, form creation/designer and advanced styling — **NOT BASELINE / separate approval**.
- [ ] Universal Adobe/Foxit/Edge compatibility or universal structure preservation — **NOT CLAIMED**.

Evidence: `docs/history/2026-10-13-F12-3-exact-head-closure.md`.

### F12.4 Compare

#### Core

- [x] **PRO-COMPARE-CORE — AUTO PASS** Local/offline comparison using existing PDFium signals only.
- [x] Detect identical documents.
- [x] Detect text-only change.
- [x] Detect visual-only change when text is equivalent.
- [x] Represent structural page-count differences independently.
- [x] Compare content only across common pages so added/removed pages do not become false text/visual signals.
- [x] `VisualChanged` means **visual change independent of text**; simultaneous text+graphic decomposition is not claimed.

Evidence: `docs/history/2026-10-14-F12-4-compare-core-closure.md`.

#### Minimal UI

- [x] **PRO-COMPARE-UI-01 — AUTO PASS** With PDF A open, expose `Archivo → Comparar con otro PDF...`.
- [x] **PRO-COMPARE-UI-02 — AUTO PASS** Select local PDF B; cancel is no-op.
- [x] **PRO-COMPARE-UI-03 — AUTO PASS** Call existing `PdfCompareService.Compare(A,B)`.
- [x] **PRO-COMPARE-UI-04 — AUTO PASS** Show A/B names, page counts, text signal, visual-only signal, structural status and overall conclusion in the existing right panel.
- [x] **PRO-COMPARE-UI-05 — AUTO PASS** Close/reset comparison without replacing or reloading A unnecessarily.
- [x] **PRO-COMPARE-UI-06 — AUTO PASS** Respect existing Edit/Organize/Sign/Form guards/lifecycle.
- [x] **PRO-COMPARE-UI-07 — AUTO PASS** Invalid/unreadable/protected B fails in a controlled way and does not destroy A.

Design: `docs/superpowers/specs/2026-10-14-f12-4-compare-ui-design.md`.  
Plan: `docs/superpowers/plans/2026-10-14-f12-4-compare-ui.md`.  
Closure: `docs/history/2026-10-14-F12-4-compare-ui-closure.md`.

Not baseline: side-by-side viewers, A/B toggling, per-page results, overlay/heatmap, OCR, semantic diff, page matching/reordering, deep signature/metadata comparison or report export.

Real/private/manual Compare QA remains **NOT RUN** and belongs to the later OCR/Forms/Compare QA pack. Automated closure is not a universal-fidelity claim.

### F12.5 Batch

- [x] **PRO-BATCH-01 — AUTO PASS** Local/offline TextWatermark batch with explicit local PDF inputs and output folder.
- [x] **PRO-BATCH-02 — AUTO PASS** Preserve originals and refuse silent output overwrite.
- [x] **PRO-BATCH-03 — AUTO PASS** Report per-item success/failure and continue remaining items after one item fails.
- [x] **PRO-BATCH-04 — AUTO PASS** Reuse existing watermark request/planner/writer rather than duplicate a PDF pipeline.
- [x] Exact baseline batch operation is deliberately **TextWatermark only**; additional operations require a separate value/capability decision.

### F12.6 Code Generator

- [x] **PRO-CODEGEN-01 — AUTO PASS** Local/offline QR PNG generator without paid/cloud dependency.
- [x] **PRO-CODEGEN-02 — AUTO PASS** Baseline symbology explicitly scoped to QR after capability gate; Code128/other formats remain separate approval.
- [x] **PRO-CODEGEN-03 — AUTO PASS** Reuse existing `ZplDocumentParser` + `LabelizeProcessRenderer`; no parallel rendering stack or new runtime dependency.
- [x] **PRO-CODEGEN-04 — AUTO PASS** Real generated PNG decodes back to the exact payload, including UTF-8 and ZPL-looking input.
- [x] **PRO-CODEGEN-05 — AUTO PASS** Existing `Herramientas` menu owner exposes focused value-entry → explicit PNG Save As with overwrite prompt.

Physical phone/scanner QR verification remains **NOT RUN** and belongs to later representative QA. Not baseline: Code128, generic barcode studio, style/logo templates, batch QR, PDF insertion or additional symbologies.

### F12.7 Audited Conversions

- [x] **PRO-CONVERT-01 — AUTO PASS** Baseline conversion separately capability-gated and explicitly scoped to **PDF→PNG only**.
- [x] **PRO-CONVERT-02 — AUTO PASS** Local/offline conversion reuses existing PDFium render owner, writes to an explicit folder and never modifies the source PDF.
- [x] **PRO-CONVERT-03 — AUTO PASS** Representative 2-page PDF evidence validates exact output count, deterministic names, PNG decode and expected 150-DPI dimensions; overwrite preflight is independently tested.
- [x] **PRO-CONVERT-04 — AUTO PASS** Limitations are explicit: raster output only, fixed 150 DPI baseline, no universal color/print/fidelity claim.
- [x] **PRO-CONVERT-05 — AUTO PASS** No second PDF engine, cloud service, LibreOffice/Office automation, generic conversion framework or new runtime dependency.

Scope: `docs/superpowers/specs/2026-10-14-f12-7-pdf-to-png-scope.md`.  
Plan: `docs/superpowers/plans/2026-10-14-f12-7-pdf-to-png.md`.

Real Windows/private PDF→PNG visual review remains **NOT RUN**. Not baseline: DOCX/PPTX/XLSX conversion, PDF semantic reconstruction to Office formats, JPEG/WebP/TIFF selection, DPI presets, batch conversion or universal-fidelity guarantees.

## Cross-cutting

- [ ] **OFFLINE** Normal functions require no Internet; full physical/offline smoke remains pending where noted.
- [x] **LICENSE** Runtime dependencies/assets audited through current automated implementation; F12.5/F12.6/F12.7 add no runtime dependency. Labelize font provenance re-audit remains required before public installer.
- [x] **PRIVACY** Private fixtures ignored and CI hygiene rejects tracked `tests/PrivateFixtures/**`.
- [x] **ORIGINAL** Existing writer flows protect source and use Save As semantics where modification occurs; Compare is read-only; QR is independent of PDF state; PDF→PNG reads the source and writes separate image files only.
- [x] **CI** Functional automated baseline through F12.7 PDF→PNG: implementation HEAD `521f0974b4b8a6b5ae7138a51cf11e61d572a998`; repository hygiene, locked restore/runtime staging, Release Build and full Test steps PASS; build `0` warnings / `0` errors; `1201/1201` PASS, `0` failed, `0` skipped; exact-head CI run `38514284225`, job `115689481722`.
- [x] **KISS** F12.7 adds one focused `PdfToPngConverter` and one existing-Utilities menu action; no `ConversionEngine`, registry/plugins, second PDF engine, cloud/backend, Office/LibreOffice dependency or new runtime package.
- [x] **NO-AUTOMERGE** `main` remains at the previously audited baseline and merges require explicit user approval.
- [ ] **INTEGRATION** Active implementation lineage remains far ahead of `main`; controlled consolidation is required before release and is not authorized by this requirements update.
- [ ] **REAL-QA** Representative manual/private/physical/external-reader QA remains open where phase sections say NOT RUN.

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
| 13 | F12 Professional | PRO-REDACTION, PRO-SIGN-PFX, PRO-FORMS-TX, PRO-COMPARE-*, PRO-BATCH-*, PRO-CODEGEN-*, PRO-CONVERT-* |
