# F5 — Organizar PDF — Design Specification

**Date:** 2026-10-09  
**Status:** APPROVED + IMPLEMENTED — automated closure; manual Windows QA NOT RUN  
**Branch:** `feat/f5-organize`  
**Base:** F4 closure head `1d620f1b2b9717aab35671e18a9dc78f28a8afdd`  
**Product:** SG PDF Editor — Windows x64, C#/.NET 10, WPF, local-first/offline

## 1. Purpose

F5 adds practical page organization without turning SG PDF Editor into a generic PDF object-tree editor.

Success means the user can enter `ORGANIZAR`, work from page thumbnails, select one or many pages, reorder, rotate, delete or duplicate them, insert pages from another PDF, merge PDFs, extract selected pages and split a PDF. The source file is not modified in place: the result is produced with `Guardar como...`, reopened and validated before publication.

F5 is preservation-aware. Structural operations may affect signatures, forms, bookmarks, named destinations, links, tagged structure, page labels, attachments or other document-level data. SG PDF Editor must never claim preservation that has not been demonstrated against the exact pinned PDFium runtime.

F5 does not add image/text editing, comments, OCR, cryptographic signing, generic repair/optimization or a second PDF engine.

## 2. Frozen project constraints

1. Windows x64, C#/.NET 10 and WPF.
2. PDFium remains the primary and only PDF engine unless a demonstrated gap later justifies another permissive utility.
3. All native PDFium activity stays behind `PdfiumRuntime.NativeGate`.
4. No cloud, account, API key, telemetry or runtime Internet dependency.
5. No commercial runtime dependency.
6. KISS/YAGNI: no generic document graph, MVVM framework, DI container, event bus or plugin system.
7. `Guardar como...` is mandatory for the first F5 writer; source overwrite is out of scope.
8. Existing F2 ZPL, F3 FIRMAR and F4 LEER behavior must not be silently changed.
9. No merge to `main` without explicit user approval.
10. Earlier manual/private/physical QA remains independent of F5 automated work.

## 3. UX model

The product modes remain:

```text
LEER | FIRMAR | EDITAR | ORGANIZAR | COMENTAR
```

F5 implements `ORGANIZAR` only.

### 3.1 Organize surface

`ORGANIZAR` uses a dedicated center surface with a virtualized thumbnail grid and a small action bar:

```text
Rotar izq. | Rotar der. | Eliminar | Duplicar | Insertar
Extraer | Dividir | Guardar como...

[ 1 ] [ 2 ] [ 3 ] [ 4 ]
[ 5 ] [ 6 ] [ 7 ] [ 8 ]
...
```

Each tile shows the thumbnail, current plan number, selection state and an optional source indicator for pages inserted from another PDF.

Selection follows familiar Windows behavior:

- click = one page;
- Ctrl+click = toggle;
- Shift+click = range;
- Ctrl+A = all;
- Escape = clear selection;
- Delete = request deletion.

Drag-and-drop is the primary reorder interaction. Multi-selected pages move as one ordered block and preserve relative order. Dropping inside the selected block is a no-op.

### 3.2 Mode boundaries

Entering `ORGANIZAR` creates an in-memory workspace from the currently open PDF. LEER is hidden while organizing; FIRMAR overlays and ZPL controls are inactive.

If FIRMAR has unresolved dirty state, the existing guard resolves it before ORGANIZAR opens. F5 does not create a second signature dirty-state authority.

Leaving a dirty ORGANIZAR workspace is guarded before opening another PDF or switching to LEER/FIRMAR/ZPL. Cancel preserves the current plan/session; confirmed discard affects only the in-memory plan and never changes the source PDF. A successful Save As clears the organize dirty state.

## 4. Core architecture — plan first, materialize once

Mouse/UI actions do not rewrite the native PDF immediately. They mutate a logical `OrganizePlan`; native PDF work occurs only when producing an output.

Conceptual model:

```text
OrganizePlan
  Sources[]
  Pages[]

OrganizeSource
  SourceId
  Path
  OriginalPageCount

OrganizePage
  ItemId
  SourceId
  SourcePageIndex
  RotationDeltaQuarterTurns
```

`ItemId` is unique per logical tile. Duplicates receive new IDs even when they point to the same source page.

One model powers all operations:

- reorder → reorder `Pages[]`;
- rotate → modify quarter-turn delta modulo 4;
- delete → remove plan items;
- duplicate → clone plan items with new IDs;
- insert → add a source plus page references;
- merge → insert all pages from another source, normally at the end;
- extract → materialize a derived plan containing selected items;
- split → materialize several derived plans.

This is the central KISS decision for F5: there is no separate merge, extract or split engine.

## 5. PDFium capability gate

Before any F5 writer is accepted, CI must verify the exact pinned `pdfium.dll` exports the APIs actually required.

Candidate APIs:

- `FPDF_CreateNewDocument`;
- `FPDF_ImportPagesByIndex` and/or `FPDF_ImportPages`;
- `FPDFPage_GetRotation`;
- `FPDFPage_SetRotation`;
- `FPDF_SaveAsCopy`;
- existing open/page/render APIs used for validation.

`FPDFPage_New` and `FPDFPage_Delete` may also be probed, but the preferred design does not mutate the source document if a new output can be composed from page imports.

If a required API is absent:

1. try another already-exported PDFium route;
2. evaluate a PDFium package upgrade only with regression/license evidence;
3. only after a demonstrated PDFium gap may qpdf/pdfcpu or another permissive utility be proposed;
4. no second engine is introduced preemptively.

Implementation result: the exact pinned DLL exposes the critical writer route used by F5, so no second PDF engine or package expansion was needed.

## 6. Native materialization

Preferred and implemented pipeline:

```text
validated OrganizePlan
  ↓
preflight
  ↓
create temporary destination document
  ↓
import exact source page indices in final plan order
  ↓
apply planned quarter-turn rotations
  ↓
save temporary PDF
  ↓
close native handles
  ↓
reopen with PdfDocumentSession
  ↓
validate
  ↓
atomic publication to destination
```

Batch contiguous import runs only when it stays simple. Correct plan order matters more than reducing native call count.

Rotation is relative to the original page rotation:

```text
outputRotation = (sourceRotation + delta) mod 4
```

Deleting every page is blocked; F5 does not create zero-page PDFs.

Destination must differ from the active source path. Existing destination replacement is transactional only after successful temp validation.

## 7. Preflight and preservation

A PDF that reopens and renders is not automatically semantically preserved.

Preflight uses a small result model:

```text
OrganizeFinding
  Kind
  Severity: Info | Warning | Block
  Message
  PreservationStatus: ProvenPreserved | ProvenChangedOrLost | Unknown
```

F5 investigates, to the practical extent supported by stable APIs/tests:

1. cryptographic signatures;
2. forms / form type;
3. bookmarks / outline presence;
4. named destinations;
5. internal links/destinations;
6. tagged/structure-tree presence;
7. page labels;
8. embedded files/attachments;
9. protected/encrypted-document write limitations;
10. representative metadata preservation.

Each structure is documented as exactly one of:

- `PROVEN PRESERVED`;
- `PROVEN CHANGED/LOST`;
- `UNKNOWN`.

`UNKNOWN` is never presented as preserved.

### 7.1 Cryptographic signatures — frozen policy

**F5 blocks structural output when `FPDF_GetSignatureCount(document) > 0`.**

Reason: page structure changes can invalidate cryptographic signatures even if signature objects remain visible. F5 offers no warning-and-continue override. Re-signing, signature validation and stripping signatures are outside F5.

### 7.2 Protected/password PDFs — frozen policy

**F5 does not write or import from a source that required a password to open.**

F4 may read such PDFs with transient credentials, but F5 does not persist passwords or introduce a general encrypted-writer credential model. ORGANIZAR shows a controlled limitation rather than failing later during save. Protected secondary sources are likewise rejected candidate-first.

### 7.3 Forms, bookmarks, destinations, links and other structures

`FPDF_ImportPages*` is treated as page import, not as proof that document-level structures remain valid.

Representative fixtures passed through the real F5 writer demonstrate that the current identity page-import route changes/loses forms, bookmarks, internal links, named destinations, tagged structure, page labels, attachments and original metadata values. These structures are classified `ProvenChangedOrLost`, surfaced as warnings and require explicit confirmation before materialization. See `docs/history/2026-10-09-F5-preservation-matrix.md`.

F5 does not implement a generic low-level PDF object-tree rewriter merely to preserve these structures. A future gap important enough for a different architecture remains a separate decision.

## 8. ORG-01 — intra-document operations

### Reorder

- one or many selected pages;
- relative order preserved;
- page count unchanged;
- source remains untouched before save.

### Rotate

- left/right in 90° increments;
- repeated operations compose modulo 4;
- thumbnail orientation reflects plan state;
- reopened output reports/renders expected rotation.

### Delete

- removes selected plan items;
- cannot reduce plan to zero pages.

### Duplicate

- duplicates are inserted immediately after the selected block by default;
- new logical IDs;
- materialized output contains independent page instances.

## 9. ORG-02 — insert, merge, extract and split

### Insert from PDF

Candidate-first local flow:

1. choose PDF;
2. open/validate source;
3. reject protected/password-required sources;
4. choose `Todas` or a validated page/range expression;
5. insert at the resolved plan position.

Failure leaves the current plan unchanged.

### Merge

`Combinar PDF...` reuses the insertion mechanism with all pages and append semantics by default. No separate merge engine.

### Extract

`Extraer` materializes the selected pages in current plan order into one new PDF. The organize workspace itself is unchanged; extraction does not implicitly delete pages.

### Split

Initial modes:

- every N pages;
- explicit non-overlapping ranges.

Example:

```text
1-3, 4-7, 8-10
```

Every group must be valid and non-empty. Output names derive deterministically from the chosen base name plus range/ordinal suffix.

Split checks all target collisions before the first write and stops on the first failed output. Previously published outputs remain; the failed temp is not published. A distributed multi-file transaction is out of scope.

## 10. Thumbnail and rendering strategy

Reuse F4 render primitives and visual conventions where practical, but organize state is independent from `ReaderPageItem`.

Organize thumbnails are lazy, low-resolution and virtualized. Reordering alone reuses the bitmap. Retention stays bounded to realized tiles plus a small neighbor window.

F5 never rasterizes the whole PDF to perform structural organization.

## 11. State, cancellation and failure safety

New source PDFs are candidate-first: validate first, then publish the plan change.

Materialization accepts cancellation around source open/import/save/validation boundaries. Native PDFium calls remain synchronous and globally serialized; F5 does not add progressive editing solely for cancellation.

While saving/merging/splitting:

- plan mutation is disabled;
- a second materialization cannot run concurrently;
- failure returns to the valid in-memory plan;
- temp files are removed best-effort in `finally`;
- source files are never changed.

Task-10 closure hardening also guards dirty ORGANIZAR state before opening another PDF or switching surfaces. Successful Save As is the point that clears dirty state.

## 12. Output validation — frozen strategy

Every produced PDF is reopened with `PdfDocumentSession` before publication.

Validation always checks:

- exact expected page count;
- valid page size for every page;
- expected rotation for every page;
- source/order identity using deterministic test fixtures.

For runtime structural smoke, every output page is rendered sequentially at **36 DPI** before publication. Only one validation bitmap needs to be alive at a time. This is intentionally stronger and simpler than a sampling policy; physical performance is measured later and the rule may only be relaxed with evidence.

Preservation validation is separate: bookmarks/forms/destinations/etc. are inspected explicitly when the relevant fixture is under test.

## 13. Testing strategy

TDD is mandatory and was used through F5.

### Pure plan tests

- move one/many;
- contiguous and non-contiguous selection;
- duplicate;
- zero-page delete guard;
- rotation modulo 4;
- insert beginning/middle/end;
- extract derivation;
- split planning/range validation;
- unique IDs and deterministic order.

### Native capability tests

Against the pinned `pdfium.dll`:

- required exports;
- create destination;
- import exact page indices;
- duplicate the same source page;
- set/get rotation;
- save/reopen.

### Synthetic end-to-end fixtures

Fixtures cover:

- unique page identity markers;
- portrait/landscape mixes;
- pre-rotated pages;
- multiple source PDFs;
- bookmarks/internal links;
- forms;
- named destinations;
- tagged structure;
- page labels;
- attachments;
- representative metadata;
- cryptographic-signature policy;
- larger page counts for virtualization/memory smoke.

No private customer/Mercado Libre PDF is committed.

### Regression boundaries

CI continues protecting:

- F4 LEER;
- F3 FIRMAR;
- F2 ZPL;
- locked restore/offline-runtime policy;
- no dependency/lock change unless explicitly justified by an approved gap decision.

Functional Task-10 closure evidence before docs: CI `37990505245` attempt 2 PASS, Release build 0 warnings / 0 errors, 550/550 tests PASS.

## 14. Module boundaries

Implemented shape remains concentrated around:

```text
src/SGPdf.App/Features/Organize/
  OrganizePlan.cs
  OrganizePage.cs
  OrganizeSource.cs
  OrganizeSelection.cs
  OrganizePreflight.cs
  OrganizeSplitPlanner.cs
  OrganizeOutputNaming.cs
  OrganizeBatchPublisher.cs
  OrganizePageItem.cs

src/SGPdf.App/Pdf/
  PdfDocumentSession.Organize.cs
  PdfOrganizeWriter.cs
  OrganizeOutputValidator.cs

src/SGPdf.App/
  MainWindow.Organize*.cs
```

Boundaries remain:

- plan/state logic independent of WPF;
- native writer independent of UI tile controls;
- raw native document handles are not broadly exposed;
- no new project/layer was required.

## 15. Implementation slices

### F5.1 — Capability + plan + preflight

Implemented and automated PASS.

### F5.2 — Intra-document organize

Implemented and automated PASS: organize grid, selection/drag, rotate/delete/duplicate, transactional writer and reopen/render validation.

### F5.3 — Insert + merge

Implemented and automated PASS: candidate secondary sources, ranges, append/merge reuse and multi-source validation.

### F5.4 — Extract + split

Implemented and automated PASS: extraction, every-N split, explicit ranges, deterministic names and stop-on-first-failure publication rule.

### F5.5 — Preservation hardening + closure

Preservation matrix and dirty-state hardening are implemented and automated GREEN. Final closure consists of reconciled docs, stacked draft PR and exact-head push/PR CI checks. Manual Windows QA remains NOT RUN.

## 16. Explicitly out of scope

- crop or arbitrary page resizing;
- page-content editing;
- image/text object editing;
- comments/annotations;
- OCR;
- redaction;
- cryptographic signing/re-signing;
- form editing;
- bookmark/destination editing;
- generic PDF repair;
- optimize/compress;
- cloud upload/merge;
- folder batch processing;
- command-line product surface;
- multi-document tab redesign;
- qpdf/pdfcpu without demonstrated PDFium failure.

## 17. Acceptance mapping

**ORG-01 AUTO PASS:** move/reorder/rotate/delete/duplicate are deterministic, source-safe and validated after transactional output.

**ORG-02 AUTO PASS:** insert/merge/extract/split reuse the plan/materializer architecture and publish only validated outputs.

**ORG-03 AUTO PASS:** preservation findings are evidence-based and signed/protected inputs obey the frozen block policies; proven changed/lost non-crypto structures require explicit warning confirmation.

**ORG-04 AUTO PASS:** required PDFium APIs are verified against the exact pinned runtime and no second engine entered F5.

Manual real-Windows acceptance remains NOT RUN and is not implied by these automated statuses.

## 18. Frozen design decisions

1. F5 is plan-first; no immediate source mutation.
2. One page-reference model powers reorder/delete/duplicate/insert/merge/extract/split.
3. F5 structural output is always `Guardar como...`; no source overwrite.
4. Writer = new destination + exact page imports + rotation + save/reopen/validate.
5. PDFium capability is verified against the pinned binary before writer use.
6. Successful page import is not proof of document-level preservation.
7. Cryptographically signed PDFs are blocked from F5 structural output.
8. Password-required PDFs are blocked from F5 structural output/import.
9. Runtime output validation renders every page sequentially at 36 DPI before publication.
10. Organize thumbnails remain lazy/bounded and do not become a second reader.
11. LEER, FIRMAR and ZPL remain separate surfaces.
12. No second PDF engine or generic document graph entered F5.
13. Dirty organize state is explicitly guarded before destructive surface/document switches.

## 19. Closure gate

The original user-review/design gate was completed before implementation. The approved TDD plan is `docs/superpowers/plans/2026-10-09-f5-organize.md`.

F5 implementation is now at automated closure. Completion requires the final stacked draft PR to remain open/unmerged with exact-head push and PR Windows CI green, plus verification that `main` is unchanged. Manual Windows QA remains a separate NOT RUN gate.

After F5 automated closure, the next permitted product step is **F6 Images design/spec only**. No F6 implementation begins until its design and TDD plan are separately approved.
