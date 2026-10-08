# F3.4 — Local Signature Library — Design Specification

**Date:** 2026-10-08  
**Status:** written spec awaiting user review  
**Base:** F3.3 final head `8b5bfd35b59fbc75826f8d7616aaaca3e2f31233`  
**Branch:** `feat/f3-4-local-signature-library`

## 1. Intent

F3.4 lets the user reuse visual signatures between PDFs without cloud storage, accounts, a database, or a second signature engine.

Every supported signature source already converges on the F3.1 `SignatureAsset` contract:

```text
Transparent PNG ─┐
Photo / scan ─────┼─→ SignatureAsset ─→ AddSignatureAsset(...) ─→ placement / save
Drawn signature ──┘
```

F3.4 persists and reloads that same asset locally. It does not persist placement geometry, page information, PDF state, or PDF edits.

Success means the user can open **FIRMAR → Biblioteca de firmas...**, save the currently selected signature asset with a friendly name, see previously saved signatures, reuse one in another PDF, rename it, or remove it. Reusing a saved signature must return to the existing `AddSignatureAsset(...)` path so the normal centered / selected / dirty behavior remains authoritative.

F3.4 is a small local convenience library, not a secure signature vault and not cryptographic signing.

## 2. Approved UX scope

The FIRMAR panel gains one new entry:

```text
Biblioteca de firmas...
```

The library dialog shows a simple list/grid containing:

- a thumbnail;
- a display name;
- an unavailable/broken state when metadata exists but its PNG cannot be used.

Actions:

- **Usar**;
- **Guardar firma seleccionada...**;
- **Renombrar**;
- **Eliminar**;
- **Cerrar**.

Commercial interaction pattern intentionally stays small:

```text
save → see → use → manage
```

No separate manager, cloud account, tab system, favorites, folders, tags, templates, or settings page is introduced.

## 3. Non-goals

F3.4 excludes:

- cryptographic signatures;
- default signature;
- initials;
- generated name/date fields;
- stamps;
- categories/tags;
- folders;
- favorites;
- search;
- sorting controls;
- drag-to-reorder;
- import/export of the whole library;
- sync;
- cloud;
- accounts;
- server;
- database / SQLite;
- encryption / password-protected vault;
- content hashing or deduplication;
- multi-user profiles inside the app;
- cross-machine roaming;
- app-wide undo/redo;
- multi-process synchronization beyond normal filesystem safety;
- any new PDF writer or placement model.

A user may intentionally save identical signature pixels under different valid display names. F3.4 does not deduplicate by content.

## 4. Fit with the current F3 architecture

Current F3.1–F3.3 already provide the required seams:

- `SignatureAsset` owns immutable BGRA pixels and image geometry;
- `SignaturePlacement` contains the asset currently placed on a PDF page;
- `SignatureEditState` owns placements, `SelectedId`, and dirty state;
- `AddSignatureAsset(...)` creates the centered placement, selects it, and marks the edit state dirty;
- `SignaturePngLoader.Load(...)` already validates PNG structure, dimensions, the shared pixel limit, BGRA conversion, and transparency.

F3.4 must reuse those contracts rather than add a parallel placement/writer pipeline.

The library is conceptually:

```text
SignatureAsset
    ↓ save
PNG + small JSON manifest under LocalAppData
    ↓ load
SignatureAsset
    ↓
AddSignatureAsset(...)
```

F3.4 must not modify `PdfVisualSignatureWriter` unless implementation uncovers evidence that the approved architecture is impossible without it; that condition requires returning to design first.

## 5. Storage root

Use the current Windows user's local application data:

```text
%LOCALAPPDATA%\SG PDF Editor\Signatures\
    library.json
    <guid>.png
    <guid>.png
    ...
```

Resolve `%LOCALAPPDATA%` through the .NET special-folder API rather than by manually parsing an environment string.

The directory is created lazily only when a successful mutating operation requires it. Merely opening an empty library must not create files unnecessarily.

No files are written beside the user's PDF, into the repository, into cloud storage, or into a shared machine-wide directory.

## 6. Manifest format

F3.4 uses one small UTF-8 JSON manifest serialized with `System.Text.Json`.

Version 1 shape:

```json
{
  "version": 1,
  "items": [
    {
      "id": "1aa9717f-5d96-4f8b-a1fe-3a8b7f7e4b17",
      "displayName": "Firma principal",
      "fileName": "1aa9717f5d964f8ba1fe3a8b7f7e4b17.png"
    }
  ]
}
```

Rules:

- `version` is required and must equal `1` for F3.4;
- `items` is required and may be empty;
- `id` is a valid, stable, unique GUID;
- `displayName` is user-facing only;
- `fileName` is the physical PNG filename only;
- physical filenames never derive from the visible display name;
- a new item uses a newly generated GUID and a filename based on that GUID, for example `<id:N>.png`;
- `fileName` must be a simple `.png` filename with no directory separators or traversal components;
- duplicate IDs or unsafe filenames make the manifest structurally invalid;
- manifest order is preserved as the stable display order; F3.4 does not add sorting/reordering behavior.

The explicit `version` field is enough for now. Do not design a migration framework before a second format actually exists.

## 7. Display-name rules

Before add or rename:

1. trim leading/trailing whitespace;
2. reject an empty/whitespace-only result;
3. preserve the user's Unicode characters and casing;
4. reject a duplicate display name using `StringComparer.OrdinalIgnoreCase` after trimming.

Do not silently suffix names such as `(2)` and do not silently replace another entry.

Because the visible name is not used as a filename, filesystem-invalid characters in the display name do not need special rewriting.

Validation errors keep the naming UI open and leave the library unchanged.

## 8. PNG persistence

The library persists only the transparent signature asset itself.

To encode an asset:

```text
SignatureAsset BGRA
→ WPF BitmapSource
→ PngBitmapEncoder
→ PNG bytes/file
```

Requirements:

- no JPEG;
- preserve alpha/transparency;
- preserve `PixelWidth` / `PixelHeight`;
- no resizing or reprocessing during save;
- no placement bounds, page coordinates, page number, rotation state, dirty state, or PDF path;
- no thumbnail sidecar files;
- no temporary files outside the library directory.

A library PNG is reopened through the existing transparent-PNG validation path, preferably `SignaturePngLoader.Load(...)`, so the existing 20,000,000 decoded-pixel guard and transparency rules remain authoritative.

Saving then reopening an asset must preserve geometry and alpha semantics; antialiased pixel values should round-trip through lossless PNG without intentional transformation.

## 9. Atomic file-write policy

There is no true two-file filesystem transaction, so F3.4 uses a small failure-safe protocol that keeps `library.json` authoritative.

All temporary files are created in the same `Signatures` directory as their final destination so final rename/replace stays on the same volume.

### 9.1 Manifest write helper

For every manifest mutation:

1. serialize the complete new manifest to a uniquely named temporary file;
2. close/flush that file before publication;
3. if `library.json` already exists, replace it using a same-volume replacement strategy that preserves the old manifest if publication fails;
4. if no manifest exists yet, move the completed temp file into place;
5. best-effort delete only temporary files created by the current operation after failure.

Never open `library.json` and truncate/rewrite it in place.

### 9.2 Add ordering

Add uses:

```text
validate current library + name
→ encode PNG to temp
→ publish new GUID PNG
→ publish new manifest referencing it
```

If PNG publication fails, manifest is untouched.

If PNG publication succeeds but manifest publication fails:

- the operation reports failure;
- the previous manifest remains authoritative;
- best-effort delete the PNG created by this failed operation;
- if that cleanup itself fails, the file is merely an orphan and must not be auto-adopted.

The implementation may only roll back files it can prove were created by the current operation. It must never sweep arbitrary existing PNGs as an automatic "repair".

### 9.3 Rename ordering

Rename is manifest-only:

```text
validate name
→ publish replacement manifest
```

The PNG filename and GUID do not change.

A failed manifest publication leaves the old name authoritative.

### 9.4 Delete ordering

Delete prioritizes not leaving a manifest entry that points to a deliberately deleted file:

```text
remove entry in new manifest
→ publish manifest
→ delete referenced PNG best-effort
```

If manifest publication fails, do not delete the PNG.

Once the manifest removal succeeds, the item is logically deleted from the library. If physical PNG deletion then fails, report a controlled cleanup warning and leave the unreferenced PNG as an orphan. Do not roll the manifest backward solely to keep a useless file referenced.

## 10. Loading, corruption, and recovery behavior

### 10.1 Missing directory / missing manifest

Treat as a normal empty library.

No error and no automatic file creation are required.

### 10.2 Structurally invalid `library.json`

Examples:

- invalid JSON;
- unsupported `version`;
- missing required shape;
- duplicate IDs;
- unsafe/path-traversing `fileName`;
- other metadata state that cannot be interpreted safely.

Behavior:

- show a controlled library-unavailable error;
- do not overwrite, rename, delete, or "repair" the corrupt manifest;
- disable library mutation/use actions that would require trusting that manifest;
- allow the dialog to close normally;
- keep the rest of FIRMAR fully usable: transparent PNG, photo preparation, drawing, placements, and PDF saving still work.

F3.4 does not include automatic recovery UI. Preserving the corrupt data is safer than silently replacing it with an empty manifest.

### 10.3 Valid manifest, missing/corrupt PNG

This is an item-level failure, not whole-library corruption.

The metadata entry remains visible with its display name and an unavailable placeholder/status. For that item:

- **Usar** is disabled;
- **Eliminar** remains available so the user can remove the stale entry;
- **Renombrar** may remain available because it only changes valid metadata;
- healthy entries remain fully usable.

Do not remove the metadata entry automatically.

### 10.4 Orphan PNG

A PNG not referenced by the valid manifest is ignored.

Do not:

- auto-import it;
- infer a name;
- delete it during ordinary library load;
- rewrite the manifest to include it.

Orphan cleanup/repair tooling is outside F3.4.

## 11. Library model / store boundary

Keep the persistence unit feature-local and small. A conceptual shape is:

```csharp
internal sealed record SignatureLibraryItem(
    Guid Id,
    string DisplayName,
    string FileName);

internal sealed class SignatureLibraryStore
{
    IReadOnlyList<SignatureLibraryItem> Load();
    SignatureLibraryItem Add(string displayName, SignatureAsset asset);
    void Rename(Guid id, string displayName);
    void Delete(Guid id);
    SignatureAsset LoadAsset(Guid id);
}
```

Exact method signatures may be adjusted by the implementation plan/tests, but the responsibilities are frozen:

- manifest validation;
- name validation;
- local path resolution;
- lossless PNG encode/load;
- transactional manifest publication;
- add/rename/delete semantics;
- no WPF dialog concerns;
- no PDF concerns.

No repository/service/DI framework is needed around this store.

## 12. Thumbnail policy

The dialog uses the real library PNG/asset as the source for an in-memory WPF thumbnail.

Requirements:

- fixed bounded visual box;
- preserve aspect ratio;
- no upscaling requirement;
- transparent background handled visually by normal WPF composition;
- no persisted thumbnail cache;
- no second image format;
- a failed image load yields the unavailable placeholder rather than crashing the dialog.

Libraries are expected to be small in this slice, so no virtualization/cache subsystem is introduced solely for F3.4.

## 13. Empty-library behavior

When the manifest is absent or valid with zero items, show a clear empty state such as:

```text
No hay firmas guardadas.
```

Button state:

- **Usar** disabled;
- **Renombrar** disabled;
- **Eliminar** disabled;
- **Guardar firma seleccionada...** enabled only if MainWindow supplied a currently selected placement asset;
- **Cerrar** enabled.

The dialog remains useful because the user can save the currently selected signature into the empty library.

## 14. Saving the currently selected signature

F3.4 saves the asset, not the placement.

The current implementation already exposes:

- `SignatureEditState.SelectedId`;
- `SignatureEditState.Placements`;
- `SignaturePlacement.Asset`.

MainWindow may obtain the selected placement by matching `SelectedId` to the current placements, or a narrow helper may be added to `SignatureEditState` if that makes the behavior independently testable. Do not add a second selection model.

When **Guardar firma seleccionada...** is invoked:

1. require a selected placement;
2. take that placement's existing `SignatureAsset` directly;
3. ask for a display name;
4. validate the name;
5. persist the asset through `SignatureLibraryStore.Add(...)`;
6. refresh/select the new library entry;
7. keep the library dialog open.

Saving to the library must **not**:

- rasterize the on-screen overlay;
- use placement size as image size;
- save PDF coordinates;
- create another PDF edit;
- change PDF dirty state;
- change the selected PDF placement;
- touch the source PDF.

If no placement is selected, the save action is disabled/no-op with controlled explanatory state.

A small name prompt/dialog is allowed. It must not become a general metadata editor.

## 15. Using a saved signature

When the user selects a healthy item and presses **Usar**:

1. load its PNG through the library/store validation path;
2. produce the normal existing `SignatureAsset`;
3. return/hand that asset to MainWindow;
4. close the library dialog after successful selection;
5. call **exactly one** existing `AddSignatureAsset(asset)`;
6. let F3.1 create the placement centered on the current page;
7. let F3.1 select it;
8. let F3.1 mark the edit state dirty;
9. refresh the normal overlay.

F3.4 must not duplicate centering, coordinate mapping, selection, or dirty-state logic.

If loading fails between selection and Use:

- add no placement;
- preserve existing placements;
- preserve existing selection;
- preserve dirty state;
- keep the active PDF/current page unchanged;
- show a controlled error and keep the library manageable.

## 16. Rename behavior

`Renombrar` applies only to the selected manifest entry.

- prompt/edit the display name;
- use the same trim/empty/duplicate validation as Add;
- do not rename the physical PNG;
- do not change GUID;
- do not reload/re-encode the image;
- do not affect any placement already present in a PDF.

Cancel is state-safe.

## 17. Delete behavior

`Eliminar` requires explicit confirmation containing the display name or otherwise making the selected item clear.

Deleting from the library:

- removes only the library entry / backing library PNG according to the safe ordering above;
- does not delete or mutate signature placements already added to the open PDF;
- does not change current PDF dirty state;
- does not touch the original source PDF;
- does not search other PDFs.

Placed signatures already hold their `SignatureAsset` in memory, so they remain valid after the source library entry is removed.

## 18. Dialog integration

Keep F3 UI consistent with the existing pattern of feature-local WPF dialogs.

Conceptual flow:

```text
FIRMAR
  └─ Biblioteca de firmas...
       └─ SignatureLibraryDialog
            ├─ list / thumbnails / names
            ├─ Use
            ├─ Save selected
            ├─ Rename
            ├─ Delete
            └─ Close
```

A narrow MainWindow seam is preferred so tests can substitute the dialog/store behavior without filesystem/UI coupling. Exact delegate names belong in the implementation plan.

Do not rewrite the main XAML solely to add this feature. Preserve the F3.1 ruling that FIRMAR integration can remain primarily in `MainWindow.Sign.cs` unless real implementation evidence favors a smaller local refactor.

## 19. State-safety rules

### Opening / closing / canceling library

Must not modify:

- PDF placements;
- selected placement;
- PDF dirty state;
- active PDF;
- page;
- zoom;
- render state.

### Add to library / rename / delete library item

These are library mutations, not PDF mutations. They do not change PDF dirty state.

### Use

Only successful **Usar** creates a PDF placement, through `AddSignatureAsset(...)`, and therefore intentionally marks the PDF signature edit state dirty.

### Failure

Library I/O/JSON/PNG failures must never discard a valid existing PDF edit state or replace the user's PDF.

## 20. Privacy and local-only behavior

F3.4 is fully local.

No:

- network;
- HTTP;
- telemetry;
- cloud API;
- upload;
- account;
- external process;
- temp signature file outside LocalAppData;
- database;
- secret/key material.

Saved signatures are personal local files under the current Windows profile. F3.4 does **not** claim encryption-at-rest. Encryption is explicitly deferred rather than implied.

## 21. Dependency policy

Use only existing platform/runtime capabilities:

- `System.Text.Json`;
- `System.IO`;
- WPF `BitmapSource` / `PngBitmapEncoder`;
- current `SignatureAsset` / `SignaturePngLoader` / F3 placement contracts.

Expected dependency delta:

```text
NuGet packages: 0
.csproj package additions: 0
network dependencies: 0
```

Do not add SQLite, ImageSharp, OpenCV, cloud SDKs, encryption libraries, or a repository framework for F3.4.

## 22. Automated acceptance

### 22.1 Manifest / model

Tests must prove:

- missing library/manifest loads as empty;
- valid v1 manifest loads entries in stable manifest order;
- empty/whitespace name rejected;
- name is trimmed;
- duplicate display name rejected case-insensitively;
- duplicate GUID rejected as structural manifest error;
- unsafe/traversing `fileName` rejected as structural manifest error;
- unsupported version is controlled and non-destructive;
- invalid JSON is controlled and non-destructive.

### 22.2 Add / PNG round-trip

Tests must prove:

- Add generates a stable GUID entry;
- physical filename is GUID-based, not display-name-based;
- PNG is created under the approved LocalAppData library root abstraction/test root;
- manifest references the PNG only after PNG publication succeeds;
- saved/reloaded asset preserves width/height;
- saved/reloaded asset preserves transparency/alpha semantics;
- library PNG reopens through normal image safety validation;
- no new dependency is required.

### 22.3 Atomicity / failures

Tests must prove at observable boundaries:

- failed PNG publication leaves manifest unchanged;
- failed manifest publication during Add leaves old manifest authoritative;
- cleanup only targets files created by the current failed operation;
- failed Rename manifest publication leaves old name authoritative;
- failed Delete manifest publication keeps PNG/reference intact;
- successful Delete manifest publication followed by PNG-delete failure leaves item logically deleted and reports controlled cleanup failure/warning;
- corrupt manifest is never silently replaced with an empty one;
- orphan PNG is ignored, not auto-imported or auto-deleted.

Tests may use an injected/test filesystem boundary only as small as needed to make deterministic failure cases possible; do not introduce a general infrastructure abstraction.

### 22.4 Item-level broken PNG

Tests must prove:

- missing PNG does not corrupt healthy entries;
- corrupt PNG does not corrupt healthy entries;
- broken item cannot be Used;
- broken item remains deletable;
- healthy items remain usable.

### 22.5 Dialog

Tests must prove:

- empty-state text/state;
- thumbnail + name for healthy entry;
- unavailable state for broken entry;
- Use disabled with no selection/broken item;
- Save selected disabled when no selected placement asset is supplied;
- Add/rename empty name rejected;
- duplicate name rejected;
- Delete confirmation/cancel is state-safe;
- Close/cancel returns no asset for Use.

### 22.6 MainWindow integration

Tests must prove:

- exactly one `Biblioteca de firmas...` action exists in FIRMAR;
- opening/closing library adds nothing and preserves current state;
- Save selected persists the selected placement's **asset**, not placement geometry;
- Save selected does not change PDF dirty/selection state;
- successful Use invokes existing placement flow exactly once;
- successful Use yields one normal centered, selected, dirty placement;
- failed/canceled Use adds nothing and preserves placements/selection/dirty state;
- deleting/renaming a library entry does not mutate an already placed signature;
- PNG/photo/draw creation flows remain unchanged.

### 22.7 Regression / architecture

Full locked restore/build/test remains GREEN with:

- 0 warnings;
- 0 errors;
- no runtime package additions;
- no PDFium writer/coordinate changes;
- no PDFsharp scope expansion;
- no ZPL/Labelize changes;
- no runtime network surface.

## 23. Manual Windows QA

Automated PASS does not replace a small real-Windows UX check.

Later manual QA should cover:

1. create signature from photo or drawing;
2. place/select it;
3. save selected asset to library;
4. close/reopen SG PDF Editor;
5. open another PDF;
6. use saved signature;
7. move/resize/save PDF through F3.1;
8. rename library item;
9. delete library item while a copy is already placed and confirm placed copy remains;
10. inspect `%LOCALAPPDATA%\SG PDF Editor\Signatures\`;
11. run with network disabled.

This QA is useful but can remain separately reported from automated F3.4 PASS if the slice follows the same project policy as F3.1–F3.3.

## 24. Non-regression boundaries

F3.4 leaves unchanged:

- F3.1 placement / coordinate / PDF writing semantics;
- F3.2 photo preparation;
- F3.3 InkCanvas drawing behavior;
- original-PDF preservation;
- cryptographic-signature warning behavior;
- PDFium runtime/threading rules;
- ZPL/Labelize;
- PDFsharp labels-only scope;
- offline-first / local-first policy;
- no-auto-merge rule.

## 25. Stop conditions

Return to design before implementation continues if F3.4 appears to require:

- a new runtime NuGet dependency;
- SQLite/database infrastructure;
- cloud/network service;
- encryption/vault infrastructure;
- changes to `PdfVisualSignatureWriter` solely for library reuse;
- a second signature placement model;
- a second dirty-state model;
- automatic orphan/corrupt-library repair that deletes existing user files;
- app-wide repository/DI abstraction;
- large MainWindow refactor unrelated to F3.4;
- changing F3.1/F3.2/F3.3 asset semantics to make persistence work.

## 26. Completion boundary

F3.4 automated closure requires:

- this written spec approved by the user;
- a separately approved TDD implementation plan;
- RED → GREEN evidence for store/atomicity/dialog/integration;
- locked restore/build/test PASS;
- 0 warnings / 0 errors;
- scope audit proving zero unauthorized dependency/PDF/ZPL/network expansion;
- closure docs / STATE / ROADMAP update;
- exact-head CI evidence;
- PR kept draft/unmerged unless the user explicitly authorizes merge.

At the end of this spec gate, no product code has been implemented yet. The next allowed step after user approval is to invoke the implementation-planning gate for F3.4.
