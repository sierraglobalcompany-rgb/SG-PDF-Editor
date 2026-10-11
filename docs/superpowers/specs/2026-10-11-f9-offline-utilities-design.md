# F9 — Offline Utilities Design

**Date:** 2026-10-11  
**Status:** approved for implementation by explicit `continua`  
**Base:** `feat/f8-comments-v1` closure `7e092166b04d72e2b8d4d1ba2d95d48e1acdebb3`

## Goal

Add a small set of genuinely useful PDF utilities that remain local/offline and preserve SG PDF Editor's conservative Save As model: watermark, page/Bates numbering, authorized protection, and evidence-based optimize/repair.

## Product principles

1. KISS/YAGNI: utilities are independent actions, not a new mega-workspace.
2. Offline: no network, accounts, API keys, cloud services or remote conversion.
3. PDFium-first: the pinned `bblanchon.PDFium.Win32 156.0.8076` remains the primary PDF engine.
4. A second PDF tool/engine may enter only after an executable capability gate proves PDFium cannot safely implement a required utility.
5. Save As is transactional: source untouched, sibling temp, reopen/validate, atomic publish, cleanup on error/cancellation.
6. Cryptographic signatures and password/encryption state are handled conservatively; no utility silently invalidates or strips security semantics.
7. No merge to `main` without explicit approval.

## UX model

F9 does not add another persistent editor mode. Utilities are exposed as focused commands under a `Herramientas` surface. Each command opens a small task-specific dialog, previews only what is necessary, then writes a new PDF through Save As.

This mirrors the useful separation found in mature PDF products while avoiding a generic "toolbox" architecture.

## Slice A — Watermark and numbering

### Text watermark V1

Required capabilities:

- Unicode text;
- page scope: all pages, current page, explicit range;
- position presets plus explicit offset only if needed by the UI;
- font size/scale;
- color;
- opacity;
- rotation;
- foreground/background placement only if exact PDFium behavior is proven by the capability gate;
- Save As and reopen/render validation.

The implementation should reuse the pinned DejaVu Sans 2.37 fallback asset and the proven CID Type2 font path rather than inventing a second font stack.

### Image watermark

Not assumed. It enters F9.1 only if the exact pinned PDFium build proves that a new image page object can be inserted, positioned, assigned alpha, saved, reopened and rendered without damaging unrelated page content. If the gate is incomplete, image watermark is deferred rather than approximated.

### Page numbering / Bates

One engine covers both ordinary numbering and Bates-like numbering:

- page/range scope;
- starting number;
- prefix;
- suffix;
- zero-padding width, e.g. `000001`;
- six header/footer anchor positions;
- font size and color;
- Unicode prefix/suffix;
- deterministic numbering from the selected range order;
- Save As and reopen/text/render validation.

No separate Bates subsystem is needed in V1.

## Slice B — Authorized protection

Protection starts with an exact capability gate. Desired product behavior:

- set an open password;
- optionally set/limit printing/copy/edit permissions only if those semantics can be round-tripped and validated;
- never remove/bypass a password the user did not provide;
- never market PDF permission flags as unbreakable DRM;
- preserve the original and always publish a new protected file.

If PDFium cannot create the required encryption safely, evaluate a small local tool such as qpdf. Adding it requires explicit evidence, pinned version/digest, license review, offline packaging and tests. Do not add pdfcpu/qpdf prophylactically.

## Slice C — Optimize / repair

### Optimize

Start with lossless/structural operations only. A smaller output file is not itself proof of correctness. Validation must include reopen, page count, representative render/text checks and preservation checks.

Destructive image recompression/downsampling is outside initial scope unless a later explicit slice defines user-visible quality controls and measurable validation.

### Repair

A repair button exists only if a corpus of intentionally malformed but recoverable PDFs proves that the chosen engine/tool can improve reopenability without silently changing valid documents. If no reliable repair capability is demonstrated, the UI must not expose a placebo action.

## Shared architecture

Use focused files under `Features/Utilities/` and `Pdf/` rather than growing `MainWindow.xaml.cs` or reusing unrelated writers.

Expected boundaries:

- managed settings/range/number formatting models under `Features/Utilities/`;
- one writer per materially different persistence mechanism rather than a mega-writer;
- PDFium interop additions isolated in partial native files when needed;
- output validators reopen the temp file and validate semantic expectations before publication;
- UI wiring remains a thin adapter over managed models/writers.

No persistent native handles may be stored in managed utility state.

## F9.1 exact capability gate

Before product code for watermark/numbering, characterize the exact pinned PDFium runtime.

### Text-overlay proof

Create a synthetic source PDF, then on an existing page:

1. load pinned DejaVu Sans through the already-proven CID Type2 path;
2. create a new text page object;
3. assign Unicode text including `NIÑO Bogotá 東京`;
4. set RGBA fill including non-255 alpha;
5. set matrix/position and a non-zero rotation;
6. insert once at the back and once at the front using deterministic page-object ordering;
7. regenerate page content;
8. Save As;
9. reopen and prove text/object ordering/geometry/color where PDFium exposes it;
10. render smoke at low DPI and prove the output is not blank/corrupt.

### Image-overlay proof

Create a tiny in-memory bitmap and prove:

1. new image object;
2. bitmap assignment;
3. matrix/position;
4. alpha path if PDFium supports it predictably;
5. front/back insertion order;
6. GenerateContent → Save As → reopen → image discovery/render.

Failure of the image proof does not block text watermark or numbering.

## Validation and preservation

Every writer slice must test at least:

- source path differs from destination;
- source fingerprint is still current;
- cancellation never publishes a partial destination;
- temp cleanup;
- existing destination is replaced atomically only after successful validation;
- page count preserved unless a utility explicitly changes it (F9 V1 utilities do not);
- unrelated page content remains readable/renderable;
- cryptographic signature policy is explicit;
- password-opened inputs fail closed unless the specific utility has an authorized-password design.

F9 closure gets a preservation matrix and exact-head Windows CI evidence. Manual Windows/Adobe/Foxit/Edge QA remains separately labelled NOT RUN until actually performed.

## Explicitly out of scope for F9 V1

- OCR (F10);
- cryptographic signing (F12);
- redaction (F12);
- arbitrary PDF-to-Office/image conversions;
- cloud compression;
- DRM claims;
- destructive image recompression without explicit later design;
- automatic repair of every malformed PDF;
- merging F9 branches to `main` without approval.
