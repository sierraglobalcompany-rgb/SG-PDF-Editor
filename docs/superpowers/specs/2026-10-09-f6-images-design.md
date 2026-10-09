# F6 — Edición de imágenes — Design Specification

**Date:** 2026-10-09  
**Status:** CONVERSATIONAL DESIGN APPROVED — written spec awaiting user review  
**Branch:** `design/f6-images`  
**Base:** exact F5 automated-closure head `327d7064c14131e603e3bce6947b593a10f46363`  
**Product:** SG PDF Editor — Windows x64, C#/.NET 10, WPF, local-first/offline

## 1. Purpose

F6 adds practical editing of real image page objects inside a PDF without turning SG PDF Editor into a raster-overlay editor or a generic low-level PDF object-tree rewriter.

Success means the user can enter `EDITAR`, select a real PDF image object, extract it as a visually faithful local image, replace it while preserving its geometry when viable, move it, resize it, rotate it, delete it, undo/redo changes, and save the result through a validated `Guardar como...` pipeline.

Opacity and z-order belong to the F6 product requirement, but they are capability-gated against the exact pinned `pdfium.dll`. They are enabled only if the runtime exports and representative save/reopen fixtures prove the required behavior. If either gate fails, F6 closes with that capability explicitly unsupported rather than introducing a second engine or a destructive fallback.

F6 edits real PDF image objects. It does not hide originals under white rectangles, flatten the page as a screenshot, or silently replace unsupported operations with raster overlays.

## 2. Frozen project constraints

1. Windows x64, C#/.NET 10 and WPF.
2. PDFium remains the primary and normally only PDF engine.
3. The pinned runtime remains `bblanchon.PDFium.Win32 156.0.8076` unless a separate, evidence-backed upgrade is explicitly approved.
4. Every PDFium native call is serialized through `PdfiumRuntime.NativeGate`, a `SemaphoreSlim`, using `Wait()/Release()` rather than `lock`.
5. No cloud, SaaS, account, API key, telemetry or runtime Internet dependency.
6. No mandatory commercial runtime dependency.
7. KISS/YAGNI: no generic PDF object graph, plugin framework, event bus, CQRS, new DI architecture or second editor engine.
8. `Guardar como...` is the only initial write path for F6. The source PDF is never overwritten by the image editor.
9. Publication is transactional: temporary output first, reopen/validate, then atomic publication.
10. Existing LEER, FIRMAR, ORGANIZAR and ZPL behavior must not regress silently.
11. Private/customer/Mercado Libre fixtures are never committed.
12. No merge to `main` without explicit user approval.
13. Real/manual Windows QA is separate from automated PASS and must never be implied by CI.

## 3. Requirements mapped to F6

### IMG-01 — detect/select images and contextual actions

F6 detects only real PDF page objects of type `FPDF_PAGEOBJ_IMAGE`, exposes a single active selection, and provides contextual image actions in `EDITAR`.

### IMG-02 — extract/save and replace

F6 extracts a visually faithful PNG and replaces an image from local PNG/JPEG input while preserving the selected object's position, size and rotation whenever the tested PDFium route permits it.

### IMG-03 — transform and delete

F6 supports move, resize, rotate and delete. Opacity and z-order are included only after exact-runtime capability gates pass.

### IMG-04 — undo/redo

All user-visible image mutations participate in a local command-based undo/redo history before materialization.

## 4. Scope boundaries

### In scope

- one active image selection at a time;
- hit-test on the active PDF page;
- move;
- proportional resize by default;
- free resize with a modifier;
- rotate;
- delete;
- extract to PNG;
- replace from PNG/JPEG;
- undo/redo;
- dirty-state protection;
- transactional Save As;
- source fingerprint validation;
- exact-runtime capability probes;
- preservation characterization against representative fixtures;
- opacity if proven;
- z-order if proven.

### Explicitly out of initial F6

- multi-selection of images;
- crop;
- copy/paste between PDFs;
- advanced properties panel;
- generic page-object editing;
- text editing;
- vector/path editing;
- OCR;
- comments/annotations;
- cryptographic signing;
- repair/optimization;
- flattening pages as a fallback;
- raw/original embedded-image stream export presented as a normal user feature;
- preserving or rewriting unsupported document structures through a custom low-level PDF object-tree engine.

These exclusions keep IMG-01..04 focused and leave room for later slices without contaminating the core image editor.

## 5. Product UX

The product modes remain:

```text
LEER | FIRMAR | EDITAR | ORGANIZAR | COMENTAR
```

F6 implements the image-editing subset of `EDITAR` only. F7 later adds text editing to the same product mode without changing F6's image-object model.

### 5.1 Entering EDITAR

When a PDF is open and eligible for image editing, entering `EDITAR` shows the normal PDF page with an interaction overlay. Image objects become selectable; non-image objects remain read-only.

If the document is not eligible for editing, F6 reports a controlled reason instead of partially entering an unsafe state.

### 5.2 Selection and contextual actions

Only one image is active at a time.

The selected image shows a WPF overlay with:

- visible selection outline;
- four corner resize handles;
- move interaction by dragging the body;
- rotation control;
- small contextual action bar or context menu.

Initial contextual actions:

```text
Reemplazar | Extraer | Rotar | Eliminar
```

Conditional actions:

```text
Opacidad | Orden
```

`Opacidad` and `Orden` appear only if their exact-runtime gates pass.

Keyboard behavior:

- `Delete` → delete selected image;
- `Ctrl+Z` → undo;
- `Ctrl+Y` → redo;
- `Escape` → clear image selection;
- shortcuts operate only while `EDITAR` owns the interaction context.

### 5.3 Resize behavior

Four corner handles are sufficient for initial F6.

- corner drag preserves aspect ratio by default;
- holding `Shift` during a corner drag permits free aspect-ratio resize;
- no edge handles are required initially;
- zero/negative/degenerated transforms are rejected before they enter the edit state.

### 5.4 Dirty-state behavior

A mutation marks the image-edit workspace dirty.

Opening another PDF or leaving `EDITAR` while dirty invokes a discard guard. Cancel keeps the active PDF and edit plan unchanged. Confirmed discard drops only the in-memory F6 workspace; it never changes the source file.

A successful `Guardar como...` clears F6 dirty state. A cancelled or failed save does not.

## 6. Architecture — logical edit plan, native materialization later

Mouse/UI actions do not immediately rewrite the PDF. The editor keeps logical state in memory and materializes native changes only when saving.

Conceptual model:

```text
ImageEditPlan
  SourcePath
  SourceFingerprint
  Objects[]
  UndoStack[]
  RedoStack[]

ImageObjectRef
  PageIndex
  PageObjectIndex
  OriginalBounds
  OriginalMatrix
  OriginalImageMetadata

ImageEditState
  ObjectRef
  CurrentMatrix
  ReplacementAsset?
  Opacity?
  ZOrderOperation?
  Deleted
```

The concrete implementation may split these records differently if tests make a smaller model clearer, but the responsibilities above are frozen.

### 6.1 No persistent native handles

`IntPtr` PDFium page/page-object handles are never stored as durable UI identity.

The in-memory workspace stores logical references only. Native handles exist only within a bounded native operation while the document/page is loaded and `NativeGate` is held.

### 6.2 Source fingerprint

F6 reuses the cheap F5 concept: file length + last-write UTC is sufficient for stale-source protection. It is an identity guard, not a cryptographic integrity claim.

If the source changes after editing begins, materialization blocks before publishing anything.

### 6.3 Object identity

For an unchanged source, `PageIndex + PageObjectIndex` is the primary logical locator.

Before applying mutations to a page during save, F6 reopens the unchanged source and resolves all edited page objects for that page before any remove/reinsert operation changes object ordering. It then verifies representative immutable characteristics such as image type, original matrix/bounds and image metadata closely enough to reject an obviously mismatched object.

F6 does not persist object IDs into the PDF and does not claim object identity survives arbitrary external rewrites.

## 7. Image discovery and hit-testing

### 7.1 Page-object discovery

The capability gate must verify the exact pinned runtime route for:

- count page objects;
- retrieve page object by index;
- classify object type;
- obtain bounds and/or matrix;
- obtain image metadata required for validation.

Only `FPDF_PAGEOBJ_IMAGE` enters the image editor.

### 7.2 Geometry

Hit-testing must use real PDF-space image geometry rather than only a large axis-aligned screen rectangle when a tighter representation is available.

Preferred order:

1. exact rotated/quad bounds if exported and proven by the pinned runtime;
2. otherwise derive the image quadrilateral from its affine matrix and image unit rectangle;
3. use axis-aligned bounds only as a controlled fallback when representative rotated-image tests prove acceptable behavior.

The click point is transformed from device coordinates to PDF coordinates through the existing page/device mapping primitives.

### 7.3 Overlapping images

Initial behavior should select the visually top candidate under the cursor only if a fixture proves that the pinned PDFium object enumeration order maps predictably to paint order.

If that relationship is not reliable, F6 uses deterministic cycling among hit candidates instead of falsely claiming topmost-object selection.

No hidden assumption about z-order is allowed to leak into hit-testing.

## 8. Transform model

The image object's current affine matrix is the authority for move, resize and rotate.

### Move

Translate the current matrix in PDF units. Preview uses the logical matrix and WPF overlay; the source PDF remains unchanged.

### Resize

Resize is relative to the selected image's current geometry.

- proportional by default;
- free aspect ratio only with `Shift`;
- minimum non-zero dimensions enforced;
- no implicit pixel resampling merely because display geometry changes.

### Rotate

Rotation is applied around the visual center of the current image geometry unless a later tested PDFium limitation requires a narrower rule.

The UI may use a rotation handle and/or buttons. The logical state stores the resulting matrix rather than relying on a transient WPF transform.

### Delete

Delete marks the selected logical image as deleted. Native removal occurs only during save.

The workspace must not mutate unrelated page objects.

## 9. Undo/redo and post-save baseline

Undo/redo is command-based and local to the F6 workspace. F6 does not snapshot whole PDF files for history.

Conceptual command types:

```text
MoveImage
ResizeImage
RotateImage
ReplaceImage
DeleteImage
SetOpacity
ChangeZOrder
```

Only commands for capabilities actually enabled exist in the active product surface.

Each committed command records enough before/after logical state to reverse and reapply itself deterministically.

Rules:

- one user gesture becomes one history entry;
- continuous drag updates preview state but commits one history item when the gesture ends;
- `Ctrl+Z` moves one committed command to redo;
- `Ctrl+Y` reapplies one redo command;
- a new mutation after undo clears the redo stack;
- selection changes alone are not undoable edits;
- failed/cancelled commands never enter history.

A successful Save As establishes a new **logical saved baseline** without replacing the active source session:

- the current `ImageEditState` values remain in the workspace;
- the active source path/session remains the original source PDF;
- undo and redo stacks are cleared;
- `dirty=false` because the current logical state matches the just-published output;
- later edits start a new history from that baseline;
- any later Save As rematerializes the complete current logical state from the unchanged original source, so previously saved edits are not lost merely because the prior destination was not reopened.

A cancelled or failed Save As changes neither history nor dirty state.

## 10. Extract/save image

Initial user-facing extraction is **visually faithful PNG**, not a promise to reproduce the exact original embedded byte stream.

Reason: PDF image objects may use filters, masks, indexed/ICC color spaces or other PDF-specific encoding. Raw/decoded stream bytes are not necessarily a normal standalone JPEG/PNG.

Preferred extraction route:

1. use a PDFium rendered-image-object bitmap API if the exact runtime exports it and representative masks/transparency fixtures pass;
2. otherwise test the simpler image bitmap API against the same corpus;
3. encode the resulting local bitmap to PNG with existing .NET/WPF facilities;
4. if neither route can reproduce the fixture faithfully, extraction for that case is reported unsupported rather than returning misleading bytes.

No new image library is introduced merely to export PNG unless a concrete gap is proven.

## 11. Replace image

Initial accepted replacements:

- PNG;
- JPEG/JPG.

The replacement file is validated candidate-first before the edit plan changes.

### 11.1 Replacement asset lifetime

The editor captures the replacement content into an immutable local in-memory asset when the user selects it. The later save must not depend on the original replacement file still existing or remaining unchanged.

Conceptual model:

```text
ImageReplacementAsset
  Format
  Bytes
  PixelWidth
  PixelHeight
  HasAlpha
```

Only replacement assets actually used by the workspace are retained.

### 11.2 Geometry preservation

Replacing image content does not silently alter the selected object's current matrix.

The replacement initially occupies the same transformed PDF-space geometry as the image it replaces. The user may then move/resize/rotate normally.

This is geometry preservation, not an assertion that original compression, color profile or internal stream representation remains identical.

### 11.3 JPEG route

If the pinned runtime exposes and passes the JPEG inline-loading route, JPEG replacement may use it to avoid unnecessary decode/re-encode work.

If not, JPEG can use the proven bitmap replacement route. The exact implementation is decided by F6.1 capability evidence, not by API preference alone.

### 11.4 PNG and transparency

PNG replacement uses a local bitmap route.

Transparency must be tested with representative alpha fixtures through save/reopen/render. If transparent PNG replacement is not visually faithful, that case is blocked with a controlled message until a safe route is demonstrated.

F6 does not silently flatten transparency onto white.

## 12. Opacity capability gate

Opacity is not considered implemented because a public PDFium graphics-state API exists in documentation. It must pass against the exact pinned runtime and real materialization path.

Gate requirements:

1. verify the required export(s) on `pdfium.dll`;
2. apply 100%, 50% and 0% (or the closest supported equivalents) to representative image objects;
3. call page content regeneration;
4. save to a temporary PDF;
5. reopen and render;
6. demonstrate the expected visual alpha behavior without corrupting unrelated objects;
7. verify undo/redo semantics.

If the gate passes, the UI exposes a 0–100% opacity control.

If it fails, opacity remains documented as unsupported for the pinned runtime. No overlay or page rasterization fallback is allowed.

## 13. Z-order capability gate

Z-order is likewise evidence-gated.

Desired user operations if proven:

- `Traer al frente`;
- `Enviar al fondo`;
- `Adelantar`;
- `Retroceder`.

Candidate native strategy may involve remove/reinsert and, if exported by the pinned runtime, insertion at a specific object index. Modern PDFium documentation is not sufficient evidence that this exact packaged DLL supports the required route.

Gate requirements:

1. verify exact exports;
2. characterize PDFium page-object enumeration and rendering order with overlapping synthetic objects;
3. reorder one image relative to another image and relative to at least one non-image object;
4. regenerate page content;
5. save/reopen/render;
6. prove expected visible order;
7. prove the object is neither duplicated nor lost;
8. prove other page objects remain present;
9. prove undo/redo reverses/reapplies ordering safely.

If arbitrary safe ordering cannot be proven, F6 does not expose z-order controls and does not add another PDF engine merely for this requirement.

## 14. Save/materialization architecture

F6 must not reuse the F5 page-import writer as its normal image-edit writer because importing pages was proven to change/lose several document-level structures.

F6 instead edits a fresh reopening of the original source document and publishes a different output path.

Preferred pipeline:

```text
ImageEditPlan
  ↓
validate source fingerprint + preflight
  ↓
open source PDF fresh without retained password
  ↓
resolve edited image objects on touched pages
  ↓
apply logical mutations to native objects
  ↓
FPDFPage_GenerateContent on each touched page
  ↓
save whole document to temp destination
  ↓
close all native handles
  ↓
reopen temp with PdfDocumentSession
  ↓
validate document + edited pages
  ↓
atomic File.Replace/File.Move publication
  ↓
cleanup temp
```

### 14.1 Source protection

- destination must differ from source;
- source is never overwritten;
- changed/missing source blocks before publication;
- existing destination remains untouched until temp validation passes;
- cancellation/failure removes temp best-effort and preserves valid prior state.

### 14.2 Native handle lifecycle

All object resolution and mutation happens while the relevant document/page handles are alive. `NativeGate` is held only for bounded native work and is always released in `finally`.

No WPF drag gesture owns a PDFium handle.

### 14.3 Page content regeneration

Every touched page must pass the required PDFium content regeneration call before save. Failure blocks publication.

Untouched pages are not regenerated merely to simplify the implementation.

## 15. Validation after save

A native save return value is not enough.

The temporary output must be reopened before publication.

### Document validation

- output opens successfully;
- page count equals source page count;
- expected page sizes/rotations remain valid;
- every page renders sequentially at 36 DPI, one bitmap at a time.

This retains the F5-quality validation invariant while bounding bitmap memory.

### Edited-page validation

For each edited image fixture/output where native inspection is possible:

- expected image object still exists unless deleted;
- matrix/bounds match expected edit state within explicit tolerance;
- replacement dimensions/metadata are plausible;
- deleted objects are absent;
- page renders successfully.

Representative visual fixtures compare rendered regions/pixels with tolerance where exact byte identity is not meaningful.

## 16. Preflight and preservation policy

F6 uses a preservation matrix independent from F5 because the materialization strategy is different.

F5's page-import writer proved loss/change of multiple document-level structures. F6 edits the existing document in place in memory before saving a copy, so F5's loss results must not be mechanically inherited as F6 results.

Representative F6 fixtures must characterize at least:

1. cryptographic signatures;
2. forms;
3. bookmarks;
4. named destinations;
5. internal links;
6. tagged structure;
7. page labels;
8. attachments;
9. representative metadata values;
10. password/protected-session limitations.

Each applicable structure receives exactly one status:

- `PROVEN PRESERVED`;
- `PROVEN CHANGED/LOST`;
- `UNKNOWN`.

`UNKNOWN` is never displayed as preserved.

### 16.1 Cryptographic signatures

A source with cryptographic signatures is **Block** for F6 Save As editing. Image mutation can invalidate signatures even if signature objects remain visible.

No warning-and-continue override, signature stripping or re-signing belongs to F6.

### 16.2 Password-opened PDFs

A PDF whose active session required a password is **Block** for F6 editing/output initially.

F6 does not retain, serialize or reuse passwords and does not add an encrypted-writer credential architecture.

### 16.3 Non-cryptographic structures

Policy is evidence-driven:

- proven preserved → Info/no destructive warning for that structure;
- proven changed/lost → Warning requiring explicit confirmation before save;
- unknown → Warning requiring explicit confirmation before save.

No generic object-tree repair is added merely to turn a warning into preservation.

## 17. Failure and cancellation safety

F6 aborts without modifying the source when any of the following occurs:

- cryptographic signature Block;
- password-opened source Block;
- source missing or fingerprint mismatch;
- expected image object cannot be resolved safely;
- object is no longer an image;
- replacement input invalid or unsupported;
- replacement transparency route fails its required guarantee;
- required PDFium call fails;
- content regeneration fails;
- native save fails;
- cancellation is requested at a supported boundary;
- reopen/validation fails;
- output publication fails.

While materializing:

- edit mutations are disabled;
- a second save cannot run concurrently;
- the in-memory valid edit plan remains available on failure;
- temp files are cleaned best-effort;
- existing destination remains untouched until successful validation.

## 18. Capability gate — F6.1 authority

Before feature UI promises are frozen in implementation, CI must probe the exact packaged `pdfium.dll` for the native surface actually used.

Core capabilities to verify:

1. page-object count/get;
2. image-object type detection;
3. object bounds and matrix access;
4. image metadata/bitmap extraction route;
5. image bitmap replacement;
6. JPEG inline/native route if selected;
7. object matrix mutation sufficient for move/resize/rotate;
8. page-object removal;
9. page content regeneration;
10. full-document save/reopen after object mutation.

Optional/conditional capabilities:

11. opacity route;
12. exact-index object insertion/reordering route;
13. tighter rotated bounds API if used by hit-testing;
14. rendered image-object bitmap API if used by extraction.

Rules:

- public upstream API documentation is evidence of a candidate route, not proof that the pinned DLL exports it;
- a missing optional export removes that optional product capability, not the whole F6 phase;
- a missing core capability triggers design review before another dependency is considered;
- no runtime package is added preemptively.

## 19. Testing strategy

F6 uses synthetic public fixtures plus controlled generated images. No customer PDFs are committed.

### 19.1 Geometry fixtures

Cover:

- one plain image;
- multiple images;
- rotated image;
- scaled image;
- overlapping images;
- image plus text/vector neighbor;
- non-square image;
- image near page edge.

Tests verify discovery, hit-testing, matrix math and selection behavior.

### 19.2 Replacement fixtures

Cover:

- JPEG opaque;
- PNG opaque;
- PNG with alpha;
- dimensions different from original;
- invalid/non-image file;
- source replacement file modified/deleted after selection to prove the in-memory asset is independent.

### 19.3 History tests

Cover:

- move → undo → redo;
- resize → undo;
- rotate → undo;
- replace → undo;
- delete → undo;
- multiple commands in order;
- undo followed by new mutation clears redo;
- drag gesture yields one history record;
- failed action does not enter history;
- successful Save As preserves current logical states, clears undo/redo and marks clean;
- a later edit/save still materializes all previously saved logical image states from the original source.

### 19.4 Writer tests

Cover:

- successful Save As;
- destination equals source blocked;
- stale/missing source blocked;
- native mutation failure preserves destination;
- save failure preserves destination;
- validation failure preserves destination;
- cancellation preserves destination;
- temp cleanup;
- successful output reopens/renders all pages;
- source bytes/path remain untouched.

### 19.5 Regression tests

At F6 closure, rerun the whole solution and specifically verify no regression in:

- LEER navigation/render/search/selection;
- FIRMAR overlays/writer/library flows;
- ORGANIZAR writer/preflight/dirty state;
- ZPL workspace/export/print preflight;
- offline-runtime guard.

## 20. F6 implementation slices

The design deliberately splits F6 into small reviewable gates.

### F6.1 — Capability gate + model

- probe exact pinned PDFium exports;
- bind only proven core APIs;
- characterize page-object/image enumeration;
- define logical object reference/edit state;
- synthetic fixtures;
- determine opacity/z-order availability status without building their final UI.

### F6.2 — Selection + hit-test

- `EDITAR` image surface;
- image discovery on active page;
- screen↔PDF hit-test;
- single selection;
- WPF overlay/handles;
- no native PDF mutation yet.

### F6.3 — Move / resize / rotate / delete + undo/redo

- logical transform commands;
- WPF gesture preview;
- command history;
- dirty state;
- delete logical state;
- no final native writer beyond any isolated test harness required by earlier gates.

### F6.4 — Extract + replace

- visually faithful PNG extraction;
- PNG/JPEG replacement assets;
- geometry preservation;
- alpha/transparency evidence;
- candidate-first replacement flow.

### F6.5 — Transactional writer

- resolve source objects fresh;
- apply planned edits;
- regenerate touched pages;
- Save As to same-directory temp;
- reopen/render validation;
- atomic publication;
- failure/cancellation/stale-source safety.

### F6.6 — Opacity + z-order, conditional

- implement opacity only if its F6.1 gate passed;
- implement z-order only if its F6.1 gate passed;
- if one or both fail, document unsupported status and do not introduce another engine solely to satisfy them.

### F6.7 — Preservation + hardening + closure

- preservation matrix from representative real-writer fixtures;
- signature/password Blocks;
- warning confirmation policy for Unknown/ChangedOrLost structures;
- temp/cancellation/offline hardening;
- full regression;
- docs/history/STATE/roadmap/requirements;
- draft stacked PR;
- real/manual Windows QA remains separately labelled NOT RUN unless actually executed.

## 21. Stop conditions requiring design review

Stop implementation and return to design if any of these becomes true:

1. the pinned PDFium runtime lacks a core capability needed for IMG-01/02/03 move/resize/rotate/delete;
2. reliable image object identity cannot be maintained from selection through Save As without storing unsafe native handles;
3. replacing PNG/JPEG requires destructive page flattening for common cases;
4. transactional Save As cannot preserve the source and prior destination on failure;
5. cryptographic/password constraints would require storing/reusing credentials;
6. core F6 requires a second PDF engine;
7. WPF image selection/overlay requires a large custom rendering framework instead of a small overlay layer;
8. preservation hardening would require a generic PDF object-tree rewriter;
9. F6 causes a non-isolatable regression in LEER/FIRMAR/ORGANIZAR/ZPL.

Opacity or z-order gate failure alone is **not** a stop condition for the rest of F6.

## 22. Acceptance mapping

### IMG-01 AUTO PASS requires

- exact-runtime page-object discovery proven;
- real image objects selectable by hit-test;
- non-image objects not accidentally editable;
- rotated/overlapping fixture behavior deterministic;
- contextual actions correctly scoped to `EDITAR`.

### IMG-02 AUTO PASS requires

- PNG extraction from representative image fixtures;
- PNG/JPEG candidate-first replacement;
- replacement geometry preserved initially;
- source replacement file lifetime no longer matters after capture;
- transparent PNG behavior either proven or explicitly blocked.

### IMG-03 AUTO PASS requires

- move/resize/rotate/delete logical + saved-output verification;
- source remains untouched;
- output reopens/renders;
- opacity and z-order are either separately proven and enabled or explicitly recorded as unsupported by capability evidence.

Because IMG-03 names opacity/z-order as requirements, F6 closure documentation must state their exact status; it must not silently mark them complete if a gate failed.

### IMG-04 AUTO PASS requires

- command history covers every enabled mutating image action;
- undo/redo ordering deterministic;
- new mutation after undo clears redo;
- failed/cancelled actions do not corrupt history;
- dirty-state guards integrate with open/mode switching.

## 23. Manual QA gate

Automated closure does not imply manual Windows UX closure.

Real/manual QA should eventually include:

- selection accuracy at several zoom levels;
- rotated/overlapping images;
- mouse drag/resize/rotate feel;
- keyboard shortcuts;
- Save As dialogs;
- extraction file result viewed externally;
- replacement with real camera/screenshot/logo assets;
- transparent PNG if enabled;
- opacity/z-order if enabled;
- large/heavy PDF responsiveness;
- network-disabled smoke;
- regression through LEER/FIRMAR/ORGANIZAR/ZPL.

Until executed, this remains **NOT RUN**.

## 24. Current gate after this document

This document is the formal written form of the conversational F6 design approved in four sections.

No implementation plan and no F6 production code may be created until the user reviews and approves this written specification.

After written-spec approval, the only next architectural workflow step is to create the F6 implementation plan. The plan must preserve the slice boundaries and capability-gate rules above and must not begin implementation in the same approval step.
