# F6 — Imágenes — DESIGN WIP CHECKPOINT

**Date:** 2026-10-09  
**Status:** DESIGN ONLY — WIP / not yet approved  
**Branch:** `design/f6-images`  
**Base commit:** `327d7064c14131e603e3bce6947b593a10f46363` (exact F5 automated-closure checkpoint)  
**No product code has been changed for F6.**

## 1. Why this checkpoint exists

The conversation may be interrupted or exhaust context. This checkpoint records the exact F6 design state so another chat/agent can resume without reconstructing F5 or re-opening architectural decisions already discussed.

## 2. Prior phase state — frozen

F5 `ORGANIZAR` is closed at automated level only.

- F5 head: `327d7064c14131e603e3bce6947b593a10f46363`
- Draft PR: #24 `F5 — Organizar PDF`
- PR base: `feat/f4-full-reader`
- PR head: `feat/f5-organize`
- PR is draft/open/unmerged.
- Push CI on final F5 checkpoint: `37991847671` PASS.
- PR CI on final F5 checkpoint: `37991854689` PASS.
- Build: 0 warnings / 0 errors.
- Tests: 550/550 PASS.
- `main` remains untouched at `31c0594758a83ec555d73ecdd7c597cdf8791fd7`.
- Real/manual Windows F5 QA remains NOT RUN.
- No merge is authorized.

F6 design work must not modify PR #24 or its branch.

## 3. F6 source requirements already present in project

From `docs/MASTER_PLAN.md` and `.planning/REQUIREMENTS.md`:

### IMG-01
Detect/select images and expose contextual actions.

### IMG-02
Extract/save images and replace them while preserving geometry where viable.

### IMG-03
Move / resize / rotate / opacity / z-order / delete.

### IMG-04
Undo / redo.

Historical broader UX ideas in `MASTER_CONTEXT.md` also mention contextual image actions such as copy, crop and properties, but these are **not frozen for the initial F6 slice**.

## 4. Process classification

F6 is classified as **architectural**, not bounded, because it introduces a new PDF object-editing subsystem and affects save/materialization semantics.

Required workflow:

1. read-only project exploration;
2. clarify intent where needed;
3. propose 2–3 approaches;
4. present design in sections and obtain user approval;
5. only after design approval, write the formal spec;
6. user reviews/approves written spec;
7. only then create implementation plan;
8. only after plan approval may implementation/TDD begin.

**Current gate:** still inside step 4. No spec file has been written yet and implementation is forbidden.

## 5. Architecture options considered

Three approaches were presented:

### Option A — PDFium object editor conservador — RECOMMENDED

Select real `FPDF_PAGEOBJ_IMAGE` objects, edit image/object state through PDFium and materialize a validated Save As copy. Unsupported operations remain explicitly unsupported rather than being silently simulated.

Advantages:

- edits real PDF objects;
- preserves vector/text surroundings;
- aligns with PDFium-first rule;
- avoids hidden destructive fallback behavior;
- compatible with transaction/save/reopen validation patterns already proven in F3/F5.

### Option B — Overlay/raster replacement — REJECTED

Cover old image and place another raster object above it.

Rejected because the original object remains beneath the overlay, semantics become misleading and PDFs become progressively harder to maintain.

### Option C — Hybrid object edit + silent raster fallback — REJECTED for initial F6

Try native object editing first and silently fallback to raster/overlay when unsupported.

Rejected because it increases complexity and makes undo/preservation/quality behavior inconsistent.

## 6. Proposed architecture section 1 — NOT YET USER-APPROVED

The assistant presented the following base design and asked for explicit approval. The user requested this checkpoint before answering that approval question.

### Proposed base

`EDITAR` mode gets a real-image-object editing workspace:

```text
PDF page
  ↓
PDFium page-object discovery
  ↓
hit-test / select only image objects
  ↓
logical image-edit state in memory
  ↓
move / resize / rotate / replace / delete / supported properties
  ↓
Guardar como...
  ↓
transactional materialization
  ↓
reopen + render / structural validation
```

Important proposal details:

- mouse/UI operations should not immediately rewrite the source PDF;
- source PDF remains untouched;
- Save As remains the initial write behavior;
- selection operates on real PDF image page objects, not screen captures;
- no cloud, SaaS, account, API key or runtime Internet;
- PDFium remains the primary/only PDF engine unless a demonstrated gap justifies a separate decision later;
- do not use an overlay/raster trick as an invisible fallback;
- if an operation cannot be proven safe against the pinned runtime, expose it as unsupported rather than pretending it worked;
- initial F6 should focus on IMG-01..04;
- crop/copy/advanced properties are candidates for a later slice rather than initial scope unless the user explicitly expands F6.

### Pending user gate

The exact question that was pending when this checkpoint was requested:

> Approve/revise the architecture base above before proceeding to design section 2: selection model, hit-test, transforms and undo/redo.

Do **not** treat this checkpoint request as approval of section 1.

## 7. PDFium feasibility observations already made

Existing repository `PdfiumNative.cs` currently contains reader/organize bindings but not the image object editing API surface yet.

Design exploration concluded that PDFium public page-object/image APIs appear capable of supporting the core direction: object enumeration/type, object bounds/matrix transforms, image bitmap access/replacement, page remove/insert and content regeneration/materialization.

However two areas were explicitly flagged as capability gates before any product promise:

### Z-order

Do not assume arbitrary object-index reordering is directly supported. The design must verify the exact pinned PDFium exports and define safe semantics before promising `bring forward/send backward` behavior.

### Opacity

Do not promise full commercial-editor opacity behavior until the relevant graphics-state APIs are verified against the exact pinned `pdfium.dll` and representative fixtures.

These are design questions, not implementation blockers yet.

## 8. Frozen constraints inherited into F6

- Windows x64.
- C# / .NET 10 / WPF.
- Local-first/offline.
- KISS/YAGNI.
- PDFium first and normally only PDF engine.
- All PDFium native activity serialized through `PdfiumRuntime.NativeGate` (`SemaphoreSlim`, `Wait()/Release()`, never `lock`).
- No runtime SaaS/account/API key/network dependency.
- No commercial mandatory runtime dependency.
- Save As by default for editing flows until preservation is mature.
- Never modify original source on failed/cancelled output.
- Reopen/validate before publication.
- No private customer/Mercado Libre fixtures committed.
- No merge to `main` without explicit user approval.
- LEER / FIRMAR / ORGANIZAR / ZPL behavior must not regress silently.

## 9. What has NOT been done in F6

- no F6 spec file written;
- no F6 implementation plan written;
- no F6 production code;
- no F6 tests;
- no PDFium image-object bindings added;
- no dependency changes;
- no UI added;
- no new PR;
- no merge;
- no changes to `feat/f5-organize` or PR #24.

## 10. Exact next step

Resume at the **design approval gate**, not implementation.

1. Present/recall proposed architecture section 1 if needed.
2. Obtain explicit user approval or requested changes.
3. If approved, continue with **design section 2: selection model + hit-test + move/resize/rotate + undo/redo**.
4. Then continue remaining design sections (replace/extract, save/materialization/preservation, error handling/testing/capability gates).
5. Only after conversational design approval, write `docs/superpowers/specs/2026-10-09-f6-images-design.md` and commit it on `design/f6-images`.
6. Stop for user review of the written spec.

Do not write implementation code or implementation plan before those gates are passed.
