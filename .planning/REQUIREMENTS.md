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

- [x] **PRO-REDACTION — AUTO PASS** F12.1 true safe redaction for complete simple visible horizontal text objects: conservative policy; underlying object removal; opaque black path; semantic/tagged risk blocked fail-closed; source/fingerprint race protection; reopened-output validation; multi-target; combined text-edit + redaction; minimal existing-panel UI; `Guardar como...` as the only publication path; no new PDF engine/runtime dependency.
- [x] **PRO-SIGN-PFX — AUTO PASS** F12.2 local cryptographic-signature PFX/P12 MVP: RSA private-key certificate loading; detached SHA-256 CMS; PDFsharp 6.2.4 signature writer; PDFium signature snapshot; strict one-signature/`ByteRange`/`adbe.pkcs7.detached` validation; ASN.1 exact CMS extraction + `SignedCms.CheckSignature(true)`; transactional temp→independent validate→source fingerprint recheck→publish; existing destination preserved on failure; minimal `Firmar digitalmente` WPF flow; password memory-only; representative synthetic preservation gate for source bytes/page count/text/image/bookmark/link/pre-existing AcroForm field/metadata/render; no second PDF engine or new runtime dependency.
- [ ] **PRO** Remaining professional scope: advanced forms, compare, batch, audited conversions and any advanced signing scope beyond the PFX MVP.

F12.1 closure evidence: `docs/history/2026-10-12-F12-1-safe-redaction-closure.md`.  
F12.2 closure evidence: `docs/history/2026-10-12-F12-2-cryptographic-signature-closure.md`.

F12.2 automated closure does **not** imply real/private/manual signing validation. Still **NOT RUN**: real Windows end-to-end UX, representative private PDFs and real PFX/P12 corpus, Adobe/Foxit/Edge interoperability, Windows Certificate Store, smartcard/HSM, TSA/timestamping, revocation/OCSP/CRL and legal/regulatory qualification.

## Cross-cutting

- [ ] **OFFLINE** Normal functions require no Internet; full physical/offline smoke remains pending where noted.
- [x] **LICENSE** Runtime dependencies/assets audited through F12.2; F12.1/F12.2 add no runtime/PDF dependency. The direct PKCS PackageReference trial was rejected because .NET 10 already provides `SignedCms`. qpdf 12.4.2, Tesseract 5.5.3 + `spa`/`eng`, PDFium, PDFsharp and existing pinned assets remain as previously audited. Labelize font provenance re-audit remains required before public installer.
- [x] **PRIVACY** Private fixtures ignored and CI hygiene rejects tracked `tests/PrivateFixtures/**`; F12.2 PFX tests use generated/self-signed material rather than committed private certificates.
- [x] **ORIGINAL** Edit/visual-sign/organize/comment/utilities/OCR/Text V2/F12.1 redaction/F12.2 cryptographic-signature writers protect source and use Save As semantics; F12.2 publishes only after independent validation + final source fingerprint match.
- [x] **CI** Functional automated slices through F12.2 have exact-head Windows evidence. F12.2 Task 6/preservation functional SHA `8d7c155972bd62740fbec48bc0a7a52b1b37b92a`: `1150/1150` PASS, `0` warnings, `0` errors, `0` skipped.
- [x] **KISS** No preventive enterprise architecture or generic PDF object graph. PDFium remains primary PDF inspector/owner; PDFsharp is reused narrowly for signing; qpdf remains narrow; Tesseract is recognition-only; F12.2 adds no second engine, backend, generic signing framework, TSA/HSM layer or duplicate preflight/fingerprint system.
- [x] **NO-AUTOMERGE** `main` remains untouched at `31c0594758a83ec555d73ecdd7c597cdf8791fd7`; merges require explicit user approval.

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
| 13 | F12 Professional | PRO-REDACTION, PRO-SIGN-PFX, PRO |
