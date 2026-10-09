# F5 — Organizar PDF — Design Specification

**Date:** 2026-10-09  
**Status:** WRITTEN — awaiting user review  
**Branch:** `feat/f5-organize`  
**Base:** F4 closure head `1d620f1b2b9717aab35671e18a9dc78f28a8afdd`  
**Product:** SG PDF Editor — Windows x64, C#/.NET 10, WPF, local-first/offline

## 1. Purpose

F5 adds practical page organization without turning SG PDF Editor into a general document-rewrite engine.

Success means a user can enter `ORGANIZAR`, see the document as page thumbnails, select one or many pages, reorder them visually, rotate, delete or duplicate them, then insert pages from another PDF, extract selected pages, merge PDFs and split a PDF into useful outputs. The original file is never modified in place by default: the result is materialized through `Guardar como...`, reopened and validated before publication.

F5 is explicitly preservation-aware. Page operations may affect signatures, forms, bookmarks, named destinations, internal links, tagged structure, page labels, attachments or other document-level structures. SG PDF Editor must detect what it can, test actual behavior against the pinned PDFium build and never claim preservation that has not been demonstrated.

F5 does not add image editing, text editing, annotations, OCR, cryptographic signing, generic repair/optimization or a second PDF engine.

## 2. Existing baseline and non-negotiable constraints

The F4 baseline already provides:

- `PdfDocumentSession` as the native PDFium document owner;
- `PdfiumRuntime.NativeGate` as the global PDFium serialization boundary;
- continuous `LEER` with bounded full-resolution rendering;
- lazy PDF thumbnails;
- bookmark/link inspection;
- `FPDF_GetSignatureCount` already bound for signature preflight;
- existing `Guardar como...` transactional behavior in the visual-signature writer: temporary file, close/reopen validation and atomic publication;
- existing FIRMAR and ZPL surfaces that must remain isolated from page organization.

Project constraints remain:

1. Windows x64, C#/.NET 10 and WPF.
2. PDFium remains the only PDF engine unless a concrete, tested gap proves another utility necessary.
3. All native PDFium activity remains behind `PdfiumRuntime.NativeGate`.
4. No cloud, SaaS, account, API key, telemetry or runtime Internet dependency.
5. No commercial runtime dependency.
6. KISS/YAGNI: no generic document graph, MVVM framework, DI container, event bus, repository layer or plugin system.
7. `Guardar como...` remains the default for structural editing.
8. No merge to `main` without explicit user approval.
9. Existing F2/F3/F4 behavior must not be silently changed.
10. Automated PASS never upgrades unresolved manual/private/physical gates from earlier phases.

## 3. Product UX model

The top-level mode concept remains:

```text
LEER | FIRMAR | EDITAR | ORGANIZAR | COMENTAR
```

F5 implements `ORGANIZAR` only.

### 3.1 Organize surface

`ORGANIZAR` uses a dedicated center surface rather than mutating the continuous reader UI.

Recommended commercial/KISS pattern:

```text
┌───────────────────────────────────────────────────────────┐
│ Rotar izq. | Rotar der. | Eliminar | Duplicar | Insertar │
│ Extraer | Dividir | Guardar como...                      │
├───────────────────────────────────────────────────────────┤
│ [ 1 ] [ 2 ] [ 3 ] [ 4 ]                                 │
│ [ 5 ] [ 6 ] [ 7 ] [ 8 ]     thumbnail grid              │
│ ...                                                       │
└───────────────────────────────────────────────────────────┘
```

Each tile shows:

- page thumbnail;
- visible page number in the current plan;
- selection state;
- rotation indicator only when useful;
- optional source indicator when pages came from another PDF.

No dense metadata panel is required in F5.

### 3.2 Selection

Minimum interaction:

- click = select one page;
- Ctrl+click = toggle individual pages;
- Shift+click = contiguous range from selection anchor;
- Ctrl+A = select all pages in the organize workspace;
- Escape = clear selection;
- Delete = request deletion of selected pages, subject to confirmation when appropriate.

Selection follows current plan order, not original source page numbers.

### 3.3 Reorder

Drag-and-drop is the primary reorder interaction.

Rules:

- dragging one selected tile moves the complete selected set while preserving its relative order;
- a visible insertion marker shows the destination;
- dropping inside the selected block is a no-op;
- reorder only mutates the in-memory plan; it does not rewrite the PDF immediately;
- keyboard-only reorder shortcuts are optional in F5 and are not an acceptance requirement.

### 3.4 Mode boundaries

Entering `ORGANIZAR` from an open PDF creates an organize workspace from the current document.

While `ORGANIZAR` is active:

- continuous LEER is hidden;
- FIRMAR overlays are not active;
- ZPL controls remain hidden;
- reader search/selection/bookmark interactions do not mutate the organize workspace.

Returning to `LEER` without saving discards only the organize plan, never the source PDF.

If FIRMAR has unresolved visual-signature changes, the existing dirty guard must resolve them before entering structural organization. F5 must not invent a second dirty-state authority.

## 4. Core architecture — plan first, materialize once

F5 does not perform a native PDF operation for every mouse action.

All UI operations modify an immutable or copy-on-write logical `OrganizePlan`. Native PDF rewrite occurs only when an output is requested.

### 4.1 OrganizePlan

Conceptual shape:

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

`ItemId` is unique per logical tile. This is important because duplicating the same source page creates two independent plan items while both may reference the same source page index.

The plan order is the desired output order.

### 4.2 Operation mapping

The same plan model covers the main features:

- **move/reorder** → reorder `Pages[]`;
- **rotate** → update `RotationDeltaQuarterTurns` modulo 4;
- **delete** → remove page items;
- **duplicate** → copy selected page items with new `ItemId`s;
- **insert from PDF** → add another `OrganizeSource`, then insert selected/all source-page references;
- **merge** → same mechanism as insert, normally append all pages from one or more PDFs;
- **extract** → materialize a plan containing only the selected items;
- **split** → materialize several derived plans from deterministic ranges.

This avoids separate reorder, merge, extract and split engines.

### 4.3 No in-place source mutation during editing

The source document remains unchanged while the workspace is open.

Advantages:

- Cancel is trivial and safe.
- UI operations are fast and testable without native PDF calls.
- Reorder/delete/duplicate share one state model.
- Structural preflight can run before native rewrite.
- Save failure leaves the source and current valid document untouched.

## 5. PDFium capability gate

Before F5 writer implementation is considered viable, the exact pinned `pdfium.dll` must be tested for the native exports actually required by the chosen implementation.

Candidate APIs include:

- `FPDF_CreateNewDocument`;
- `FPDF_ImportPagesByIndex` and/or `FPDF_ImportPages`;
- `FPDFPage_GetRotation`;
- `FPDFPage_SetRotation`;
- `FPDF_SaveAsCopy`;
- existing open/page/render APIs used for validation.

`FPDFPage_Delete` / `FPDFPage_New` may be tested because PDFium exposes them, but the preferred F5 architecture does not require destructive source mutation if a new output document can be composed from the plan.

The capability test must run against the same pinned runtime used by CI/product. Documentation saying an API exists is not enough.

If a required function is absent from the pinned binary:

1. first determine whether the same feature can be expressed with another already-exported PDFium API;
2. then evaluate a PDFium package upgrade with its own regression/license audit if justified;
3. only after a demonstrated PDFium gap may qpdf/pdfcpu or another permissive utility be proposed;
4. no second engine is added preemptively.

## 6. Native materialization strategy

### 6.1 Preferred writer model

Create a new destination PDF and import source pages into it in final plan order.

Conceptual flow:

```text
validated OrganizePlan
  ↓
preflight result
  ↓
create temporary output document
  ↓
for each source/run needed by the plan:
    import exact source page indices at destination position
  ↓
apply planned quarter-turn rotations to output pages
  ↓
save temporary PDF
  ↓
close native handles
  ↓
reopen with PdfDocumentSession
  ↓
validate
  ↓
atomic publish to chosen destination
```

Batch contiguous import runs when it materially simplifies/native-call count, but do not create a complex optimizer. Correct plan order is more important than minimizing native calls.

### 6.2 Rotation semantics

`RotationDeltaQuarterTurns` is relative to the source page's current rotation.

At materialization:

```text
outputRotation = (sourceRotation + delta) mod 4
```

The UI may show only clockwise/counter-clockwise actions. Arbitrary-angle rotation is outside F5.

### 6.3 Zero-page result

A PDF cannot be saved from an empty organize plan in F5.

Deleting all pages is blocked with a controlled message. If the user wants no pages, they can cancel rather than produce a malformed/meaningless output.

### 6.4 Source overwrite

The initial F5 contract follows existing structural-edit safety:

- destination must be different from the active source path;
- existing destination may be replaced transactionally only after successful temp validation;
- the original active PDF is not overwritten by the first F5 implementation.

In-place replace may be revisited later only after broader preservation confidence.

## 7. Preflight and preservation policy

F5 does not equate “PDF saved and renders” with “all semantic structures were preserved.”

### 7.1 Preflight result

Use a small explicit result model, for example:

```text
OrganizePreflightResult
  Findings[]
  CanProceed

OrganizeFinding
  Kind
  Severity
  Message
```

Severity remains intentionally small:

```text
Info | Warning | Block
```

No generic rules engine is needed.

### 7.2 Structures to inspect

At minimum, F5 preflight investigates:

1. cryptographic signatures;
2. forms / AcroForm presence or form type;
3. bookmarks / outline presence;
4. named destinations;
5. internal links/destinations relevant to page indices;
6. tagged/structure-tree presence when a stable detection route is available;
7. page labels when detectable with reasonable complexity;
8. embedded-file/attachment presence when detectable with reasonable complexity;
9. encryption/protected-document write limitations.

Metadata by itself is lower risk than navigation/form structures but must still be included in preservation tests when the writer is evaluated.

### 7.3 Preservation classifications

For each structure, project documentation must distinguish:

- **PROVEN PRESERVED** — synthetic/representative test demonstrates structure survives the exact operation/writer path and reopen inspection;
- **PROVEN CHANGED/LOST** — test demonstrates degradation;
- **UNKNOWN** — not yet demonstrated.

The UI must never turn `UNKNOWN` into a preservation promise.

### 7.4 Cryptographic signatures

Structural page changes can invalidate cryptographic signatures even if the signature objects remain present.

F5 therefore treats cryptographic signatures as a high-risk finding. The initial implementation must not silently save a reorganized signed PDF as if the signature remained valid.

The implementation plan must choose and test one conservative policy before shipping the write path:

- block organization of cryptographically signed PDFs; or
- require an explicit warning/confirmation that structural modification can invalidate signatures.

Automatic stripping, re-signing or cryptographic validation is outside F5.

### 7.5 Forms, bookmarks, destinations and links

`FPDF_ImportPages*` is a page-import API, not a blanket guarantee that every document-level structure will be reconstructed consistently after page reorder/duplication/deletion.

Therefore:

- do not claim preservation based only on successful import;
- build representative fixtures;
- inspect reopened output using PDFium APIs where available;
- if a structure is lost or points to wrong pages, classify it honestly and gate/warn accordingly;
- do not implement a generic low-level PDF object-tree rewriter in F5 merely to rescue an edge case.

If preserving one structure requires a disproportionate document-graph rewrite, that is evidence for a later dedicated utility/gap decision, not permission to expand F5 indefinitely.

## 8. Intra-document operations — ORG-01

### 8.1 Reorder/move

Plan-only operation. Supports single and multi-selection.

Acceptance:

- exact relative order of moved selection is preserved;
- no lost/duplicated item IDs;
- page count unchanged;
- source PDF untouched until save.

### 8.2 Rotate

Rotate selected pages left/right by 90° increments.

Acceptance:

- multiple rotations compose modulo 4;
- mixed source rotations are handled correctly;
- thumbnail orientation reflects plan state without rewriting source;
- final reopened PDF reports/visually renders expected orientation.

### 8.3 Delete

Remove selected plan items after confirmation when the action is consequential.

Acceptance:

- page count updates immediately in plan/UI;
- deletion can never produce zero output pages;
- source remains untouched before save.

### 8.4 Duplicate

Duplicate selected pages immediately after the selection by default, preserving selected relative order.

Acceptance:

- duplicates have new logical IDs;
- source page references may be shared safely;
- output contains separate page instances after materialization.

## 9. Inter-document operations — ORG-02

### 9.1 Insert pages from PDF

`Insertar desde PDF...` opens a local PDF candidate-first.

If the candidate cannot be opened, the active organize workspace remains unchanged.

Default KISS insertion UX:

1. choose PDF;
2. show page-count/source summary;
3. choose `Todas` or a validated page/range expression;
4. insert before/after the current selection or append if no insertion point is active.

A second full document-management window is not required.

### 9.2 Merge

`Combinar PDF...` reuses the same insertion mechanism with all pages selected and append semantics by default.

F5 does not need a separate merge engine or a separate output format.

### 9.3 Protected secondary sources

F4 supports transient password entry for reading protected PDFs, but existing edit writers do not yet establish a general encrypted-write credential model.

F5 must not persist passwords simply to make inter-document composition easier.

The first implementation may conservatively reject protected secondary-source composition if it cannot keep the operation transient and safely reopen/save through PDFium. This limitation must be explicit rather than silently failing later.

## 10. Extract and split

### 10.1 Extract selected pages

`Extraer` creates a new derived plan from the selected pages in their current organize order.

The original workspace is unchanged.

The user chooses an output path and receives one PDF containing the selection.

No automatic deletion from the source plan occurs unless a future explicit “extract and remove” option is separately designed.

### 10.2 Split

F5 provides a small practical split surface, not a batch-processing framework.

Initial supported split modes:

- every N pages;
- explicit page ranges.

Example:

```text
1-3, 4-7, 8-10
```

Each resulting group must be non-empty, valid and non-overlapping for the first implementation. Complex overlapping batch recipes are outside F5.

Output filenames are deterministic from the chosen base name plus an ordinal/range suffix. Existing output files are never silently overwritten in bulk.

### 10.3 Many-output failure behavior

Split publication is per output file.

If one output fails:

- already validated/published previous outputs remain valid;
- the failed output is not published from an invalid temp;
- remaining behavior must be deterministic and documented by the implementation plan (stop on first failure is the preferred KISS default).

A cross-file distributed transaction is not required.

## 11. Thumbnail strategy

Reuse F4 PDF render primitives and visual conventions where practical, but do not couple organize state to `ReaderPageItem` state.

`OrganizePageItem` may contain:

- logical ID;
- source/page reference;
- planned rotation;
- display number;
- selection state;
- lazy thumbnail bitmap/state.

Thumbnail rendering stays low resolution and lazy/virtualized. Do not render full-resolution page bitmaps merely to populate the organize grid.

When a page is only reordered, its existing thumbnail can be reused. A planned rotation may rotate the thumbnail visually in WPF or request a low-resolution rerender; choose the simpler implementation that produces correct orientation and does not mutate the source PDF.

## 12. State, cancellation and error handling

### 12.1 Candidate-first state changes

Operations that introduce a source PDF must be candidate-first:

```text
open/validate candidate source
  ↓ success
publish source + plan change
```

Failure leaves current plan/source list unchanged.

### 12.2 Native materialization cancellation

Longer save/merge/split work accepts `CancellationToken` around source open/import/save/validation boundaries.

PDFium calls remain synchronous unless existing project behavior already wraps them. F5 does not add progressive native editing solely for cancellation.

Cancellation before publication cleans temporary output and leaves source/destination state valid.

### 12.3 UI busy state

During materialization:

- prevent concurrent plan mutations;
- keep cancel behavior explicit where supported;
- do not allow a second save/materialize operation concurrently;
- return to the existing valid plan after failure.

### 12.4 Temporary files

Use request-scoped temporary files in the destination directory where atomic replacement semantics matter. Clean them in `finally` best-effort, matching existing project safety patterns.

Do not create persistent working copies of every source page.

## 13. Output validation

Every F5 output must be reopened with the project reader before publication is considered successful.

Minimum structural checks:

- output opens with PDFium;
- expected page count;
- every output page reports valid page size;
- expected page rotations;
- selected representative pages render successfully;
- full render validation of every page for smaller outputs, or a bounded deterministic validation strategy for very large outputs defined by implementation tests.

Operation-specific checks:

- reorder → identifiable page sequence matches plan;
- duplicate → expected duplicated page content/order exists;
- delete → removed pages absent and count correct;
- insert/merge → source-boundary pages appear in correct positions;
- extract/split → each output matches its derived plan.

Fixtures should use deterministic page markers/text so order can be verified semantically rather than only by page count.

Preservation checks are separate from basic structural validation and must inspect the relevant structure explicitly.

## 14. Testing strategy

Implementation uses TDD and exact-head Windows CI as established by prior phases.

### 14.1 Pure plan tests

Test without PDFium where possible:

- move one/many;
- contiguous/non-contiguous selections;
- duplicate;
- delete guard against zero pages;
- rotate modulo 4;
- insert at beginning/middle/end;
- extract derivation;
- split range parsing/planning;
- invariants: stable source references, unique item IDs, deterministic order.

### 14.2 Native capability tests

Against pinned `pdfium.dll`:

- required export availability;
- create destination document;
- import exact page indices;
- duplicate same source page through repeated import/reference plan;
- apply/get rotation;
- save/reopen.

### 14.3 End-to-end synthetic PDFs

Generate deterministic PDFs covering:

- mixed portrait/landscape sizes;
- unique text/page markers;
- existing rotations;
- multiple source documents;
- internal links/destinations;
- bookmarks;
- forms where practical;
- signed fixture metadata/signature detection where a safe synthetic fixture is feasible;
- large-enough page count for virtualization/performance smoke.

Private customer PDFs never enter the repository.

### 14.4 Regression boundaries

CI must continue protecting:

- F4 LEER open/render/navigation;
- F3 FIRMAR entry/save architecture;
- F2 ZPL workflow;
- locked restore/offline-runtime policy;
- no new package/lock change unless explicitly justified by a later approved gap decision.

## 15. Performance and scale boundaries

F5 targets normal business PDFs and must remain bounded rather than pre-rendering/copying everything eagerly.

Rules:

- plan mutations are in-memory metadata operations;
- thumbnails are lazy and low-resolution;
- no page rasterization is required for native import itself;
- no whole-document image conversion;
- split/merge processing is sequential by default rather than spawning many native writers;
- all PDFium calls still respect the one global native gate.

Do not add a background worker pool for structural PDF writes without measured evidence.

## 16. File/module boundaries

Expected feature-local shape, subject to implementation-plan refinement:

```text
src/SGPdf.App/Features/Organize/
  OrganizePlan.cs
  OrganizePage.cs
  OrganizeSource.cs
  OrganizeSelection.cs
  OrganizePreflight.cs
  OrganizeSplitPlanner.cs
  ...small focused UI/state helpers

src/SGPdf.App/Pdf/
  PdfDocumentSession.Organize.cs      // narrow inspection/native operations if appropriate
  PdfOrganizeWriter.cs               // transactional materialization

src/SGPdf.App/
  MainWindow.Organize.cs              // narrow WPF integration
```

Names are not mandatory API commitments. The architectural boundaries are:

- plan/state logic independent from WPF;
- native writer independent from tile/selection controls;
- raw PDF document handles are not exposed broadly to UI feature classes;
- no new project/layer unless implementation evidence requires it.

## 17. Proposed implementation slices

The later implementation plan should preserve small gates:

### F5.1 — Capability + plan + preflight foundation

- pinned PDFium export test;
- pure `OrganizePlan` operations;
- preservation/preflight detection contracts;
- representative import-preservation probes;
- no broad UI/write claim until capability evidence is green.

### F5.2 — Intra-document organize

- organize grid;
- selection + drag reorder;
- rotate/delete/duplicate;
- transactional single-output writer;
- reopen/order/rotation validation.

### F5.3 — Insert + merge

- secondary local PDF candidate flow;
- insert ranges;
- append/merge reuse;
- multi-source validation.

### F5.4 — Extract + split

- selected-page extraction;
- every-N-pages + explicit-range split;
- deterministic filenames and per-output transactional save.

### F5.5 — Preservation hardening + closure

- fixtures and preflight reconciliation;
- warnings/blocks based on evidence;
- large-document/manual Windows QA checklist;
- offline/privacy/temp-residue audit;
- docs/history/draft PR closure.

No later slice should be started merely because the preceding one exists; each slice requires its own red/green evidence and scope audit.

## 18. Explicitly out of scope

F5 does **not** include:

- arbitrary page crop/redimensioning;
- page-content editing;
- image/text object editing;
- annotations/comments;
- OCR;
- redaction;
- cryptographic signing/re-signing;
- form editing;
- bookmark editor;
- named-destination editor;
- generic PDF object-tree repair;
- optimize/compress;
- cloud merge/upload;
- batch folder processing;
- command-line product surface;
- tabs/multi-document workspace redesign;
- qpdf/pdfcpu or another structural utility without demonstrated PDFium failure.

## 19. Acceptance mapping

### ORG-01 — Move/reorder/rotate/delete/duplicate pages

PASS only when:

- plan operations are deterministic and TDD-covered;
- UI can perform them without mutating source;
- transactional output reopens;
- page order/count/rotation match the plan.

### ORG-02 — Insert/extract/merge/split

PASS only when:

- multiple sources remain candidate-first and local/offline;
- insert/merge use the same plan/materializer architecture;
- extract/split generate correct independently validated outputs;
- failures do not corrupt source or publish invalid temp outputs.

### ORG-03 — Preservation preflight

PASS only when:

- required structures are inspected to the practical extent defined here;
- preservation status is evidence-based;
- risky/unknown structures produce the approved warning/block behavior;
- the product does not claim unsupported preservation.

### ORG-04 — PDFium first

PASS only when:

- required APIs are verified against the pinned build;
- no second structural engine is introduced without a demonstrated, documented PDFium gap and separate approval.

## 20. Design decisions frozen by this specification

1. F5 is **plan-first**, not immediate native mutation.
2. One page-reference model powers reorder/delete/duplicate/insert/merge/extract/split.
3. The default output is **Guardar como...** and source overwrite is not part of initial F5.
4. Preferred writer = create destination + import exact page indices + apply planned rotations + save/reopen/validate.
5. PDFium capability is tested against the exact pinned binary before implementation relies on it.
6. `FPDF_ImportPages*` success alone is not evidence of preservation of document-level structures.
7. Cryptographic signatures are never silently represented as still valid after structural changes.
8. Organize thumbnails are lazy/bounded and do not become a second full-resolution reader.
9. Existing LEER/FIRMAR/ZPL surfaces remain separate.
10. No second PDF engine, generic document graph or enterprise framework enters F5 without demonstrated necessity.

## 21. User-review gate

This document defines the F5 architecture and scope only.

After user approval of this written specification, the next permitted step is to create the detailed TDD implementation plan with `writing-plans`. Product-code implementation must not begin before that plan is written, reviewed and approved according to the project workflow.
