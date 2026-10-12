# F10 — Local OCR Design

**Date:** 2026-10-11  
**Status:** written-spec review gate  
**Branch:** `feat/f10-local-ocr`  
**Base:** F9 automated closure `222454f5782f84a929c7fd610c284d15df8f869d`

## Goal

Turn scanned/image-only PDFs into searchable, selectable and copyable PDFs while preserving the original page appearance and SG PDF Editor's offline, local-first and conservative Save As model.

F10 is not a document-reconstruction or Word-like editing phase. Its product promise is deliberately narrow:

> keep the page visually intact, recognize its text locally, add a searchable text layer, save a new PDF, then reopen and prove that the expected text can actually be found/copied.

## Product principles

1. **Offline by construction.** No OCR cloud, API keys, accounts, telemetry dependency or runtime download.
2. **KISS/YAGNI.** One OCR engine, one local sidecar path, one searchable-text output path.
3. **PDFium first for PDF ownership.** PDFium remains responsible for opening, rendering, page geometry, existing-text detection, writing/validation when the exact pinned runtime proves the required capability.
4. **Tesseract only for recognition.** Tesseract recognizes rasterized pages; it does not become SG PDF Editor's general PDF engine.
5. **Preserve the original visual page.** F10 must not replace a valid page with a newly rasterized PDF page merely to make OCR easy.
6. **Transactional Save As.** The source is never overwritten in place; output is staged, reopened, validated and atomically published only after all gates pass.
7. **Fail closed on uncertain security/preservation semantics.** Signatures, encryption and unsupported write paths are blocked rather than silently altered.
8. **No merge to `main` without explicit user approval.**

## Canonical product scope

Roadmap authority already defines F10 as:

`Tesseract local: scan → render → OCR → capa buscable. Español primero.`

F10 V1 therefore covers:

- scanned/image-only PDFs;
- hybrid PDFs containing both searchable and scanned pages;
- page scope: automatic, current page or explicit range;
- Spanish as the default recognition language;
- English and Spanish+English as explicit user options;
- local OCR only;
- searchable/selectable/copyable text layer;
- Save As to a new PDF;
- reopen/search/copy validation;
- cancellation and temporary-file cleanup;
- representative structural/visual preservation checks.

## Explicitly out of scope for F10 V1

- OCR cloud or remote conversion;
- arbitrary language-pack marketplace/download UI;
- automatic language detection across many languages;
- document translation;
- Word-like text reflow or paragraph reconstruction;
- F11 reading-order/reflow work;
- batch OCR across many source files;
- destructive deskew/denoise/background-removal pipeline;
- replacing original page images with recompressed versions;
- PDF/A conversion unless separately designed and tested;
- handwriting recognition claims;
- cryptographic signing or signature preservation after content mutation;
- password bypass/removal;
- hidden fallback to another OCR engine.

## Chosen architecture

```text
WPF / managed OCR request
  ↓
preflight + page eligibility
  ↓
PDFium render selected page(s) to local raster
  ↓
Tesseract local sidecar
  ↓
TSV word/line text + confidence + pixel coordinates
  ↓
managed coordinate normalization
  ↓
PDF searchable-text layer writer
  ↓
reopen + search/text/render/preservation validator
  ↓
atomic Save As publication
```

### Why TSV

Tesseract's TSV output gives recognized text plus block/paragraph/line/word hierarchy, confidence and pixel-space rectangles. SG PDF Editor needs those coordinates to place searchable text over the original scan without delegating ownership of the final PDF to Tesseract.

Plain `.txt` is insufficient because it loses geometry. Tesseract-generated PDF is not the primary F10 output path because it would give Tesseract control over page reconstruction and can conflict with SG PDF Editor's preservation guarantees.

## OCR engine and language data

Tesseract is the only OCR engine planned for F10.

Research snapshot at design time:

- current upstream Tesseract release observed: **5.5.3**;
- upstream license: Apache-2.0;
- Tesseract 5 supports the LSTM OCR engine and `tsv` output;
- multiple languages are selected with `-l lang1+lang2`;
- `tessdata_fast` is the recommended speed-oriented traineddata family for normal desktop use and is compatible with Tesseract 5.x;
- required initial language assets are `spa.traineddata` and `eng.traineddata`; `osd.traineddata` may be staged only if an evidence-backed orientation slice actually needs it.

**Important:** the spec does not yet freeze a Windows binary distribution. F10 Task 1/2 must select an exact Windows x64 redistributable package or reproducible build, pin version + archive digest, register license/notices, and prove offline execution before product integration. No unverified `tesseract.exe` may enter the product.

Recommended V1 model family: `tessdata_fast`, because F10 targets interactive desktop OCR and should optimize for useful accuracy with bounded latency/memory. `tessdata_best` is not bundled preventively; it may be evaluated later only if measured Spanish/English accuracy proves the fast models insufficient.

## Language UX

The OCR dialog exposes only user-meaningful controls:

- **Idioma:** Español (default), Inglés, Español + Inglés;
- **Páginas:** Automático (default), Página actual, Rango;
- **Guardar como:** destination chosen by the existing Save As pattern;
- progress/cancel while OCR runs.

The UI does not expose Tesseract OEM, PSM, DPI, confidence thresholds, tessdata paths, thread knobs or implementation details in V1.

Internal language mapping:

- Español → `spa`
- Inglés → `eng`
- Español + Inglés → `spa+eng`

## Page eligibility

### Automatic mode

Before raster OCR, SG PDF Editor inspects each page using the existing PDFium text layer.

A page is OCR-eligible when it does not contain meaningful searchable text according to a conservative managed policy. The initial policy must be deterministic and test-driven rather than a vague character-count heuristic hidden in UI.

Minimum behavior:

- image-only page → OCR;
- page with useful existing text → skip;
- hybrid document → process only eligible pages;
- fully searchable document → explicit no-op result, no pointless rewritten output;
- current-page/range modes still run the same eligibility/preflight policy unless a later explicit design introduces a safe override.

The goal is to avoid duplicate invisible text layers and degraded search/copy behavior.

## Rasterization

PDFium renders OCR-eligible pages locally.

Recommended initial target: **300 DPI equivalent** for normal document text because it is a conventional OCR-quality baseline and avoids UI complexity. It remains an internal default, not a product promise. The implementation plan must include a deterministic fixture proving that this render path gives stable Spanish/English recognition on CI hardware.

Rendering rules:

- respect page crop/rotation geometry;
- do not mutate the source page;
- use request-scoped temporary images/bitmaps;
- release large raster buffers page-by-page;
- do not eagerly rasterize an entire large document into memory;
- cancellation stops future pages and tears down the active OCR process tree.

## Tesseract sidecar process model

Follow the already-proven local sidecar pattern from Labelize/qpdf:

- exact executable path owned by SG PDF Editor;
- no system PATH dependency;
- `UseShellExecute = false`;
- argument list, never shell-concatenated command strings;
- explicit `--tessdata-dir` to the bundled local language-data directory;
- request-scoped temp directory;
- stdout/stderr captured;
- bounded timeout per page/request where appropriate;
- cancellation terminates the entire process tree;
- no runtime download;
- cleanup after success, error or cancellation.

A failed OCR page fails the requested output transaction unless the product later defines and tests a partial-success UX. F10 V1 does not silently publish a partially OCR'd document while reporting success.

## Searchable text-layer writer — hard capability gate

This is the primary technical risk of F10 and must be resolved before end-to-end OCR integration.

The exact pinned PDFium runtime must prove that SG PDF Editor can add text that is:

- invisible during normal rendering;
- still present in the PDF text model;
- selectable;
- copyable;
- searchable after Save As + reopen;
- Unicode-correct for representative Spanish characters;
- geometrically aligned closely enough with the raster source;
- preserved on rotated pages.

### Required synthetic proof

Create a deterministic scanned-page fixture containing representative text such as:

`Factura número 12345 — NIÑO Bogotá, información útil. Total: $98.765,43`

The capability gate must prove at least:

1. source page initially has no searchable text;
2. managed OCR-layer data includes words/lines and source pixel rectangles;
3. Unicode `á é í ó ú ü ñ Ñ` survives write/save/reopen;
4. rendered page remains visually equivalent within a defined tolerance;
5. reopened PDF exposes the expected text through the existing PDFium text extraction path;
6. existing F4 search can find representative words;
7. copied/extracted text is non-empty and ordered usefully for the deterministic fixture;
8. page rotation is handled in a separate fixture;
9. original file bytes/path remain untouched;
10. failed validation publishes nothing.

### No fake invisibility

Do not implement OCR text as white text, tiny text, alpha-zero text or any other visual trick unless the exact PDF semantics are proven to behave correctly in search/selection/render across the tested readers.

Preferred implementation is true invisible text rendering semantics (for example PDF text render mode 3) using the proven PDFium text-object/CID Type2 infrastructure where the exact runtime supports the required round trip.

If PDFium cannot satisfy the gate, stop the implementation slice and characterize exactly one alternative PDF-writing mechanism. Do not add another general PDF engine proactively.

## Coordinate mapping

Tesseract TSV rectangles are expressed in raster pixel coordinates with top-left origin. PDF page geometry uses PDF coordinates and may include rotation/crop transforms.

F10 therefore needs one managed, deterministic mapping layer:

`OCR pixel rectangle → unrotated device/page space → PDF user space`

Requirements:

- use actual raster width/height and PDF page dimensions;
- account for page rotation 0/90/180/270;
- avoid accumulated floating-point drift across many words;
- keep text baselines/bounds within the recognized word/line region;
- test portrait + landscape + rotated fixtures.

F10 does not need arbitrary perspective correction or deskew in V1.

## Text granularity and reading order

Recommended V1 writing unit: **line-oriented text objects assembled from TSV words**, while preserving per-word geometry data in the managed OCR result for validation/debugging.

Why:

- one object per page is too coarse for selection geometry;
- one object per character is unnecessarily heavy;
- one object per OCR word can explode object count on large documents;
- line-oriented output is a pragmatic KISS balance for normal scanned documents.

If executable characterization shows that line objects degrade search/selection alignment materially, the plan may use word-oriented objects, but that change requires measured evidence rather than speculation.

F11 remains responsible for sophisticated reading-order, columns and reflow semantics.

## Font strategy

Reuse the existing pinned **DejaVu Sans 2.37** CID Type2/Unicode infrastructure where its proven glyph coverage is sufficient for Spanish and English.

F10 must not introduce a second font stack unless the OCR fixture proves an unsupported required code point. Unsupported glyphs fail closed at the writer boundary rather than silently substituting missing characters.

## Preflight and security

F10 modifies PDF content, so security semantics are conservative.

### Cryptographically signed PDFs

**Blocked in F10 V1.** Adding OCR content invalidates the existing signature. The UI must explain that a new OCR copy cannot preserve the current cryptographic signature as valid.

### Password-protected/encrypted PDFs

Initial F10 policy: **blocked for write** unless a later executable preservation gate proves that the authorized source password can be supplied and the output can preserve the intended encryption/security state without stripping it.

No bypass, password removal or silent decryption publication.

### Source race

Reuse the source-fingerprint/concurrent-change protection pattern. If the source changes between preflight and publication, fail closed.

## Transactional Save As

The OCR writer follows the same conservative pattern proven in later SG PDF phases:

1. validate source/destination are distinct;
2. fingerprint source;
3. build request-scoped temp artifacts;
4. OCR selected eligible pages;
5. write text layer into a temporary output;
6. reopen temporary output;
7. validate page count, expected searchable text and render/preservation invariants;
8. re-check source fingerprint;
9. atomically publish destination;
10. remove temporary files/directories on all paths.

Existing destination files are replaced only after successful validation.

## Output validation

Validation must prove semantics, not merely `tesseract.exe` exit code 0.

Required automated checks include:

- output reopens in PDFium;
- page count unchanged;
- expected OCR page(s) now contain searchable text;
- skipped searchable page(s) do not get duplicate OCR layers;
- representative Spanish accents/ñ survive extraction;
- representative English survives extraction;
- bilingual mode produces useful expected tokens from both languages;
- F4 search finds known fixture terms;
- source file unchanged;
- rendered pixel comparison remains within a deliberately chosen tolerance for the deterministic fixture;
- temp files are cleaned;
- cancellation publishes nothing;
- destination replacement is atomic;
- failure on page N does not publish a partial-success document.

## Preservation matrix

F10 closure must run a fresh representative preservation matrix on a hybrid fixture. Where applicable it should cover:

- AcroForm;
- bookmarks;
- named destinations;
- internal links;
- tagged structure;
- page labels;
- attachments + exact payload;
- representative metadata;
- page rotation;
- supported/unsupported annotations already preserved by F8/F9;
- original scan/image content;
- pre-existing searchable text on pages skipped by OCR.

The matrix is evidence for the tested corpus only, not a universal PDF guarantee.

## Performance and resource model

F10 must remain usable on ordinary multi-page scans without creating a new scheduler architecture.

Initial model:

- process pages sequentially by default;
- keep at most the active OCR raster/result plus bounded UI preview/progress state;
- release page raster resources immediately after recognition;
- do not parallelize Tesseract preventively;
- cancellation checked between major stages and kills the active child process tree;
- add concurrency only if measured end-to-end latency justifies its complexity.

This deliberately favors predictable memory and simpler failure semantics over maximum throughput.

## UI behavior

Recommended command: **`OCR / Hacer PDF buscable…`** under `Herramientas`.

Dialog states:

1. configure language + page scope;
2. choose destination Save As;
3. progress showing current page / total eligible pages;
4. Cancel;
5. completion summary showing processed pages and pages skipped because they already contained searchable text.

For a fully searchable document, return a clear no-op message rather than generating another copy that changes nothing.

## Dependency / legal gate

Before runtime adoption, F10 must record in `third_party/manifest.json`:

- exact Tesseract version;
- exact Windows x64 distribution/build provenance;
- release/archive digest;
- Apache-2.0 license file;
- required third-party notices/licenses from the selected binary distribution;
- exact `spa`, `eng` and any actually-used `osd` traineddata provenance + digests;
- statement that runtime performs no network download.

Do not mark Tesseract `runtime-current-f10` until that gate is green.

## Test strategy

Follow RED → minimum GREEN → exact-head Windows CI.

Required deterministic automated corpus:

1. Spanish scan with accents/ñ/numbers/currency;
2. English scan;
3. bilingual scan;
4. two-page hybrid PDF: one searchable page + one scanned page;
5. rotated scanned page;
6. fully searchable PDF yielding no-op;
7. explicit page/range selection;
8. signed-source preflight block;
9. protected-source preflight block;
10. cancellation/timeout/temp cleanup;
11. OCR process non-zero exit;
12. malformed TSV/result rejection;
13. source-race rejection;
14. preservation fixture.

Recognition assertions should target known deterministic tokens/phrases and bounded geometry/semantic invariants, not demand byte-for-byte identical OCR output across unrelated Tesseract builds.

## Manual QA kept separate

Automated closure does not imply manual interoperability closure.

Until actually executed, keep these **NOT RUN**:

- real Windows OCR dialog UX;
- large real scanned documents;
- low-quality phone photos/scans;
- Spanish business documents outside synthetic fixtures;
- Adobe Acrobat Reader search/select/copy;
- Foxit search/select/copy;
- Edge PDF search/select/copy;
- private user OCR corpus;
- performance observation on representative user hardware.

## F10 execution gates

The implementation plan should preserve this order:

1. **Task 1 — searchable invisible-text capability gate** on the exact pinned PDFium/runtime path; no Tesseract product integration yet.
2. **Task 2 — Tesseract Windows distribution + license/provenance + offline sidecar gate.**
3. **Task 3 — managed OCR models, language/page policy and deterministic process adapter.**
4. **Task 4 — rasterization + TSV parsing + coordinate mapping.**
5. **Task 5 — transactional searchable-layer writer + validator.**
6. **Task 6 — hybrid-page/no-op/security/cancellation hardening.**
7. **Task 7 — focused WPF command/dialog.**
8. **Task 8 — preservation matrix, full Windows suite, scope audit and automated closure docs.**

The exact implementation plan is a later artifact and must not be treated as approved merely because this design spec exists.

## Definition of done for F10 automated closure

F10 is automated-PASS only when all of the following are true:

- local OCR works without network;
- Spanish default works on deterministic fixture;
- English and Spanish+English paths have deterministic evidence;
- scanned PDF becomes searchable/selectable/copyable after Save As + reopen;
- hybrid PDF OCRs only eligible pages;
- fully searchable PDF is a safe no-op;
- visual scan content remains preserved within the tested tolerance;
- page count and representative document-level structures are preserved in the fresh matrix;
- signed/protected unsupported cases fail closed;
- cancellation/error/source-race never publish partial output;
- runtime dependency provenance/licenses/digests are recorded;
- full exact-head Windows build/tests are green;
- manual/private/external-reader QA remains explicitly separate if not run;
- no merge to `main` occurs without explicit user approval.
