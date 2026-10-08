# F3.4 — Local Signature Library Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let the user save, reuse, rename and delete visual signature assets locally between PDFs while preserving the existing F3.1 placement/save behavior and remaining fully offline.

**Architecture:** F3.4 adds one feature-local persistence boundary around the existing immutable `SignatureAsset`. A small `SignatureLibraryStore` owns the LocalAppData root, manifest v1 validation, lossless PNG persistence and failure-safe publication. A focused WPF `SignatureLibraryDialog` presents healthy/broken items and returns only `SignatureAsset?` to `MainWindow`. `MainWindow.Sign.cs` supplies the currently selected placement's existing asset for **Guardar firma seleccionada...** and, on successful **Usar**, calls the proven `AddSignatureAsset(...)` exactly once. No second placement model, PDF writer, dirty-state model, database or network surface is introduced.

**Tech Stack:** C# / .NET 10 / WPF; `System.Text.Json`; `System.IO`; WPF `BitmapSource` / `PngBitmapEncoder`; existing `SignatureAsset`, `SignaturePngLoader`, `SignatureEditState`, `AddSignatureAsset(...)`; xUnit. No new runtime package.

**Spec:** `docs/superpowers/specs/2026-10-08-f3-4-local-signature-library-design.md`

**Spec gate:** approved by the user on 2026-10-08 by asking to continue after the continuity/spec review. Product code remains untouched until this implementation plan is separately approved.

## Global Constraints

- Work only on `feat/f3-4-local-signature-library`, stacked on F3.3 final head `8b5bfd35b59fbc75826f8d7616aaaca3e2f31233`.
- Current pre-plan head is `69af1cc46bf54156854554f3974f519e3f127da5`; CI `37825730483` is PASS.
- PR remains draft/unmerged when opened; no merge to `main` without explicit user approval.
- Storage root is `%LOCALAPPDATA%\SG PDF Editor\Signatures\`, resolved through `Environment.SpecialFolder.LocalApplicationData`.
- Opening an empty library must not create the directory/manifest; create lazily only for a successful mutation.
- Manifest authority is `library.json`, UTF-8 JSON, `version = 1`, ordered `items` containing stable GUID + display name + simple GUID-based `.png` filename.
- Display names are trimmed; empty/whitespace rejected; duplicate names rejected with `StringComparer.OrdinalIgnoreCase`; Unicode/casing otherwise preserved.
- Physical filenames never derive from the display name and must be simple `.png` basenames with no directory/traversal components.
- Persist only the existing `SignatureAsset` pixels/geometry as lossless PNG. Do not persist placement bounds, page, PDF path, dirty state or overlay rasterization.
- Reload library PNGs through existing `SignaturePngLoader.Load(...)`; the existing 20,000,000-pixel and transparency rules remain authoritative.
- All temp files for mutations live in the same library directory. Never truncate `library.json` in place.
- Add ordering: validate → encode PNG temp → publish final PNG → publish replacement manifest. If manifest publication fails, old manifest remains authoritative and only the current operation's PNG may be best-effort rolled back.
- Rename is manifest-only; GUID and PNG filename/bytes remain unchanged.
- Delete ordering: publish manifest without item → best-effort delete PNG. If PNG deletion fails, logical delete stays committed and the orphan is ignored.
- Invalid manifest is whole-library unavailable and must never be auto-repaired/overwritten. Missing/corrupt individual PNG is item-level unavailable only.
- Orphan PNGs are ignored; never auto-import, rename or delete them in ordinary load.
- **Use** must return an existing `SignatureAsset` to MainWindow and then call existing `AddSignatureAsset(...)` exactly once. Never duplicate centering, coordinates, selection or dirty-state logic.
- Save/rename/delete library operations are not PDF edits and must not alter PDF dirty/selection/page/render state.
- No new NuGet dependency, SQLite/database, encryption/vault, cloud/network/HTTP, external process, repository framework, app-wide DI, thumbnail cache or migration framework.
- Do not modify `PdfVisualSignatureWriter`, PDFium P/Invoke/coordinates, F3.2 processing, F3.3 ink behavior, ZPL/Labelize, PDFsharp scope, `.csproj` or lockfiles. If implementation appears to require any of those, stop and return to design.

## Review Focus

1. **Corrupt-manifest preservation:** invalid JSON/version/IDs/path metadata must disable the library without rewriting user data or breaking the rest of FIRMAR.
2. **Two-file Add atomicity:** a PNG publication or manifest publication failure must never leave `library.json` referencing an unpublished PNG; rollback may target only files created by that operation.
3. **Delete semantics:** once the manifest removal succeeds, a later PNG-delete failure is a cleanup warning/orphan, not a reason to resurrect the item.
4. **Broken-item isolation:** a missing/corrupt PNG must make only that item unavailable while healthy entries remain usable and the broken metadata remains deletable/renamable.
5. **MainWindow state safety:** dialog cancel/failure must preserve placements/selection/dirty state, while successful Use must add exactly one normal centered selected dirty placement through the existing F3.1 path.

---

### Task 1: Manifest model, LocalAppData root and safe load boundary

**Files:**
- Create: `src/SGPdf.App/Features/Sign/SignatureLibraryItem.cs`
- Create: `src/SGPdf.App/Features/Sign/SignatureLibraryStore.cs`
- Create: `tests/SGPdf.App.Tests/SignatureLibraryStoreTests.cs`

**Minimal contracts:**

```csharp
internal sealed record SignatureLibraryItem(
    Guid Id,
    string DisplayName,
    string FileName);

internal sealed class SignatureLibraryStore
{
    internal SignatureLibraryStore(
        string? rootDirectory = null,
        SignatureLibraryFileOps? fileOps = null);

    internal string RootDirectory { get; }
    internal IReadOnlyList<SignatureLibraryItem> Load();
    internal SignatureAsset LoadAsset(Guid id);
}
```

The constructor uses an explicit `rootDirectory` only for tests; production default is:

```csharp
Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "SG PDF Editor",
    "Signatures")
```

Keep manifest DTOs private/internal to the store. Do not expose `JsonDocument`, mutable DTO lists, or physical full paths as product models.

Manifest validation on `Load()`:

- missing root or missing `library.json` → empty list and no directory creation;
- parse UTF-8 JSON with `System.Text.Json`;
- require `version == 1` and non-null `items`;
- parse every `id` as non-empty GUID and reject duplicate IDs;
- require non-null display name metadata; structural loading may preserve exactly stored display name, while Add/Rename own user-input trimming/duplicate-name rules;
- require simple `.png` `fileName`: `Path.GetFileName(fileName) == fileName`, no separators, rooted path, `.`/`..`, or alternate extension;
- preserve manifest item order;
- invalid structure throws one controlled feature-local exception (for example `SignatureLibraryUnavailableException`) without changing disk;
- orphan PNGs do not participate in `Load()`.

`LoadAsset(id)` must find the manifest entry from a valid manifest, combine only the validated basename with the library root and delegate decoding to `SignaturePngLoader.Load(...)`.

- [ ] **Step 1: Write RED model/load tests**

```text
Load_MissingRoot_ReturnsEmptyWithoutCreatingDirectory
Load_MissingManifest_ReturnsEmptyWithoutCreatingManifest
Load_ValidV1_PreservesManifestOrderAndMetadata
Load_InvalidJson_ThrowsControlledUnavailableAndDoesNotRewriteFile
Load_UnsupportedVersion_ThrowsControlledUnavailableAndDoesNotRewriteFile
Load_DuplicateGuid_ThrowsControlledUnavailable
Load_UnsafeFileName_RejectsTraversalRootedPathAndNonPng
Load_OrphanPng_IsIgnoredAndNotDeleted
LoadAsset_UsesValidatedManifestEntryAndExistingPngLoader
```

Use a unique test root below `Path.GetTempPath()`. Capture original manifest bytes before failure assertions and verify exact byte preservation.

- [ ] **Step 2: Confirm RED**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignatureLibraryStoreTests"
```

Expected: compile/test failure only because F3.4 library contracts do not exist.

- [ ] **Step 3: Implement the minimum model + read-only store boundary.**

No directory creation from `Load()`/empty browse. No generic repository/filesystem framework.

- [ ] **Step 4: Focused GREEN + full regression**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignatureLibraryStoreTests"
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Require focused PASS, locked restore PASS, Release build 0 warnings/0 errors, all existing tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/SGPdf.App/Features/Sign/SignatureLibraryItem.cs src/SGPdf.App/Features/Sign/SignatureLibraryStore.cs tests/SGPdf.App.Tests/SignatureLibraryStoreTests.cs
git commit -m "feat(sign): add local signature library manifest"
```

---

### Task 2: Lossless PNG persistence + Add/Rename/Delete atomicity

**Files:**
- Create: `src/SGPdf.App/Features/Sign/SignatureLibraryFileOps.cs`
- Modify: `src/SGPdf.App/Features/Sign/SignatureLibraryStore.cs`
- Modify/Test: `tests/SGPdf.App.Tests/SignatureLibraryStoreTests.cs`

**Additional contracts:**

```csharp
internal sealed class SignatureLibraryFileOps
{
    internal Action<string, string> PublishNewFile { get; init; }
    internal Action<string, string> ReplaceFile { get; init; }
    internal Action<string> DeleteFile { get; init; }
}

internal sealed record SignatureLibraryDeleteResult(
    bool FileCleanupSucceeded,
    string? Warning = null);

internal sealed class SignatureLibraryStore
{
    internal SignatureLibraryItem Add(string displayName, SignatureAsset asset);
    internal SignatureLibraryItem Rename(Guid id, string displayName);
    internal SignatureLibraryDeleteResult Delete(Guid id);
}
```

`SignatureLibraryFileOps` is a **feature-local deterministic failure seam only**, not a general filesystem service. Defaults stay direct `System.IO` operations. Tests may replace publication/delete delegates to force exact failure boundaries.

PNG encoding:

```text
SignatureAsset.BgraPixels
→ BitmapSource.Create(... PixelFormats.Bgra32 ...)
→ BitmapFrame.Create(...)
→ PngBitmapEncoder
→ request-scoped temp file in RootDirectory
```

Rules:

- preserve original width/height and alpha; no resize/reprocess;
- generate `id = Guid.NewGuid()` and `fileName = $"{id:N}.png"`;
- create the root lazily only when a mutation has passed all validation and is ready to write;
- temp PNG and temp manifest use unique names in the same root;
- flush/close temp before publication;
- manifest publication uses move when absent and same-volume replacement when present;
- best-effort cleanup is limited to temp/final files provably created by the current operation.

Name normalization used by Add/Rename:

```text
trim → reject empty → reject duplicate OrdinalIgnoreCase → preserve resulting Unicode/casing
```

Rename excludes the currently renamed item's own name from duplicate detection.

- [ ] **Step 1: Extend RED tests for Add and PNG round-trip**

```text
Add_TrimsNameAndGeneratesGuidBasedPngName
Add_RejectsWhitespaceAndCaseInsensitiveDuplicateWithoutMutation
Add_CreatesRootLazilyOnlyAfterValidation
Add_PublishesPngBeforeManifestReferencesIt
Add_RoundTrip_PreservesDimensionsAndAlphaSemantics
Add_RoundTrip_LoadAssetPassesExistingPngValidation
Add_DisplayNameCharactersNeverAffectPhysicalFilename
```

For alpha round-trip, assert dimensions and representative alpha/BGRA semantics rather than implementation-specific PNG bytes.

- [ ] **Step 2: Add RED deterministic failure tests**

```text
Add_PngPublicationFailure_LeavesOldManifestUnchanged
Add_ManifestPublicationFailure_LeavesOldManifestAuthoritativeAndRemovesCurrentPngBestEffort
Add_ManifestFailureAndRollbackDeleteFailure_LeavesOnlyIgnoredOrphan
Add_Failure_CleansOnlyFilesCreatedByCurrentOperation
Rename_ChangesOnlyManifestNameAndKeepsGuidFilenameAndPngBytes
Rename_ManifestPublicationFailure_LeavesOldNameAuthoritative
Delete_ManifestPublicationFailure_KeepsEntryAndPng
Delete_SuccessfulManifestThenPngDeleteFailure_RemainsLogicallyDeletedAndReturnsWarning
Delete_Success_RemovesMetadataThenBackingPng
```

The forced failure seam must distinguish publication destinations by exact path/basename rather than introducing mock infrastructure.

- [ ] **Step 3: Confirm RED**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignatureLibraryStoreTests"
```

- [ ] **Step 4: Implement minimum mutation/atomicity behavior.**

Never sweep unrelated orphan PNGs. Never rewrite a structurally corrupt manifest as empty. Delete does not roll back a successfully published metadata removal just because physical cleanup fails.

- [ ] **Step 5: Focused GREEN + full regression**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignatureLibraryStoreTests"
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Require 0 warnings/0 errors and all tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/SGPdf.App/Features/Sign/SignatureLibraryFileOps.cs src/SGPdf.App/Features/Sign/SignatureLibraryStore.cs tests/SGPdf.App.Tests/SignatureLibraryStoreTests.cs
git commit -m "feat(sign): persist signature library safely"
```

---

### Task 3: `Biblioteca de firmas` dialog + name prompt + broken-item isolation

**Files:**
- Create: `src/SGPdf.App/SignatureLibraryDialog.xaml`
- Create: `src/SGPdf.App/SignatureLibraryDialog.xaml.cs`
- Create: `src/SGPdf.App/SignatureNameDialog.xaml`
- Create: `src/SGPdf.App/SignatureNameDialog.xaml.cs`
- Create/Test: `tests/SGPdf.App.Tests/SignatureLibraryDialogTests.cs`

**Dialog contract:**

```csharp
public partial class SignatureLibraryDialog : Window
{
    internal SignatureLibraryDialog(
        SignatureLibraryStore store,
        SignatureAsset? selectedPlacementAsset);

    internal SignatureAsset? SelectedAsset { get; }

    internal static SignatureAsset? Open(
        Window owner,
        SignatureAsset? selectedPlacementAsset);
}
```

Production `Open(...)` creates the default `SignatureLibraryStore`. Do not add a service locator or DI container.

A small dialog-local view item may carry:

```csharp
internal sealed record SignatureLibraryDialogItem(
    Guid Id,
    string DisplayName,
    BitmapSource? Thumbnail,
    bool IsAvailable,
    string? UnavailableText);
```

Thumbnail creation uses the already loaded `SignatureAsset` in memory and `BitmapSource.Create(...)`; no thumbnail sidecar/cache.

**Named controls:**

```text
SignatureLibraryItemsList
SignatureLibraryEmptyText
SignatureLibraryUseButton
SignatureLibrarySaveSelectedButton
SignatureLibraryRenameButton
SignatureLibraryDeleteButton
SignatureLibraryCloseButton
SignatureLibraryStatusText
```

**Narrow dialog test seams:**

```csharp
private Func<string, string?, string?> _promptName = ...;
private Func<string, bool> _confirmDelete = ...;
```

Equivalent owner-aware signatures are acceptable if needed by WPF. Keep them private and local to this dialog.

Behavior:

- valid empty library → `No hay firmas guardadas.`;
- structurally invalid manifest → library-unavailable state, no mutation/Use actions, Close still works;
- load metadata first, then attempt each `LoadAsset(id)` independently;
- missing/corrupt PNG → keep display name with unavailable placeholder/status; Use disabled for it, Rename/Delete available, healthy entries unaffected;
- **Guardar firma seleccionada...** enabled only when constructor received a selected placement asset;
- Save prompts for name, calls `store.Add(name, selectedPlacementAsset)`, refreshes/selects new entry and keeps dialog open;
- Rename prompts current name, calls store Rename, refreshes/selects same GUID, no PNG rewrite;
- Delete confirmation clearly contains/display-identifies the selected name, then calls store Delete; cleanup warning is surfaced without restoring the item;
- **Usar** loads the selected healthy asset at action time, stores it in `SelectedAsset`, then closes successfully;
- a Use-time load failure keeps `SelectedAsset == null`, keeps dialog manageable and does not close as success;
- Close/window-X/cancel returns null.

`SignatureNameDialog` is a tiny text prompt only; no metadata editor.

- [ ] **Step 1: Write RED STA dialog tests**

```text
Dialog_EmptyLibrary_ShowsEmptyStateAndCorrectButtonStates
Dialog_SaveSelectedDisabledWhenNoSelectedPlacementAsset
Dialog_StructurallyInvalidManifest_DisablesMutationAndUseButCanClose
Dialog_HealthyEntry_ShowsNameAndThumbnail
Dialog_MissingPng_MarksOnlyThatItemUnavailableAndLeavesHealthyEntryUsable
Dialog_CorruptPng_MarksOnlyThatItemUnavailable
Dialog_UseDisabledWithoutSelectionOrForBrokenItem
Dialog_SaveSelected_UsesSuppliedExactAssetRefreshesAndStaysOpen
Dialog_SaveSelected_EmptyOrDuplicateName_ShowsControlledValidationAndKeepsState
Dialog_Rename_KeepsSelectedGuidAndDoesNotAffectThumbnailAsset
Dialog_DeleteCancel_IsStateSafe
Dialog_DeleteConfirmed_RemovesMetadataAndRefreshes
Dialog_DeleteCleanupWarning_LeavesItemLogicallyRemovedAndShowsWarning
Dialog_UseHealthyItem_ReturnsLoadedSignatureAsset
Dialog_UseTimeFailure_ReturnsNothingAndKeepsDialogOpen
Dialog_CloseOrWindowX_ReturnsNoAsset
```

Follow existing F3 STA test style (`RunInSta`, named controls, direct/internal methods where needed). Do not depend on interactive MessageBox clicks in CI; inject only the two narrow prompt/confirmation seams.

- [ ] **Step 2: Confirm RED**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignatureLibraryDialogTests"
```

Expected failures only because dialog contracts do not yet exist.

- [ ] **Step 3: Implement the focused WPF dialogs.**

Keep UI simple: one bounded list/grid, thumbnail + name + unavailable indication, five actions. No search/tags/favorites/folders/sorting/reorder/settings.

- [ ] **Step 4: Focused GREEN + full regression**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignatureLibraryDialogTests"
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Require 0 warnings/0 errors and all tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/SGPdf.App/SignatureLibraryDialog.xaml src/SGPdf.App/SignatureLibraryDialog.xaml.cs src/SGPdf.App/SignatureNameDialog.xaml src/SGPdf.App/SignatureNameDialog.xaml.cs tests/SGPdf.App.Tests/SignatureLibraryDialogTests.cs
git commit -m "feat(sign): add local signature library dialog"
```

---

### Task 4: Integrate library into the existing FIRMAR path

**Files:**
- Modify: `src/SGPdf.App/MainWindow.Sign.cs`
- Create/Test: `tests/SGPdf.App.Tests/MainWindowSignatureLibraryTests.cs`
- Regression-only: existing `MainWindowSignatureTests.cs`, `MainWindowSignaturePhotoTests.cs`, `MainWindowSignatureDrawTests.cs`

**MainWindow seam:**

```csharp
private Func<Window, SignatureAsset?, SignatureAsset?> _openSignatureLibrary =
    static (owner, selectedAsset) => SignatureLibraryDialog.Open(owner, selectedAsset);

private SignatureAsset? GetSelectedSignatureAsset();
private bool TryOpenSignatureLibrary();
```

`GetSelectedSignatureAsset()` uses the one existing selection model only:

```text
_signatureEditState.SelectedId
→ find matching SignaturePlacement in .Placements
→ return placement.Asset
```

Do not add a second selected-asset field to `MainWindow` or `SignatureEditState` unless a focused test proves a narrow helper is materially simpler.

**Named action:**

```text
Name = SignatureLibraryButton
Content = Biblioteca de firmas...
```

Add it in `BuildSignaturePropertiesPanel()` after the three creation-source actions (PNG/photo/draw) and before placement-management actions. Preserve code-built FIRMAR UI; do not rewrite `MainWindow.xaml`.

Behavior:

- opening the dialog supplies the exact selected placement asset or null;
- null return = Close/cancel/no Use, no PDF-state mutation;
- thrown dialog/store failure = controlled status/message, preserve existing placements/selection/dirty/page/active PDF;
- non-null return from Use = call existing `AddSignatureAsset(asset)` exactly once;
- F3.1 remains authoritative for centering, page coordinates, selection, dirty state and overlay refresh;
- library Save/Rename/Delete happen inside the dialog and never directly mutate `_signatureEditState`;
- an asset already placed remains valid in memory even if its library item is renamed/deleted.

- [ ] **Step 1: Write RED MainWindow tests**

```text
LibraryAction_ExistsExactlyOnceInFirmarPanel
LibraryOpenClose_PreservesPriorPlacementsSelectionAndDirtyState
LibraryOpen_ReceivesExactSelectedPlacementAsset_NotPlacementRasterization
LibraryOpen_WithNoSelectedPlacement_ReceivesNull
LibraryFailure_PreservesPriorPdfSignatureState
LibraryUse_AddsExactlyOneNormalCenteredSelectedDirtyPlacement
LibraryUse_UsesSameAddSignatureAssetGeometryAsDirectSource
LibrarySaveRenameDeleteViaDialogDoNotMutateAlreadyPlacedAsset
ExistingPngPhotoDrawActionsRemainAvailableAndUnchanged
```

Use the existing reflection/delegate/STA pattern already used by `MainWindowSignatureDrawTests`; do not introduce a test DI framework.

For the “exact selected asset” test, capture object identity (`Assert.Same`) passed to `_openSignatureLibrary`; this is the guard against accidentally rasterizing the placement overlay or saving placement geometry.

- [ ] **Step 2: Confirm RED**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~MainWindowSignatureLibraryTests"
```

- [ ] **Step 3: Implement minimal MainWindow integration.**

No changes to `AddSignatureAsset(...)`, `SignatureCoordinateMapper`, `PdfVisualSignatureWriter`, photo/draw preparation, or PDF Save As flow.

- [ ] **Step 4: Focused GREEN + F3 regression + full regression**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~MainWindowSignatureLibraryTests|FullyQualifiedName~MainWindowSignatureTests|FullyQualifiedName~MainWindowSignaturePhotoTests|FullyQualifiedName~MainWindowSignatureDrawTests"
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Require build 0/0 and all tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/SGPdf.App/MainWindow.Sign.cs tests/SGPdf.App.Tests/MainWindowSignatureLibraryTests.cs
git commit -m "feat(sign): integrate reusable signature library"
```

---

### Task 5: Closure, architecture audit, CI evidence and draft PR

**Files:**
- Modify: `.planning/STATE.md`
- Modify: `.planning/ROADMAP.md`
- Modify: `.planning/phases/04-f3-visual-signature/F3.4-PLAN.md`
- Create: `docs/history/2026-10-08-F3.4.md`
- Do **not** modify product code during the closure-only commit.

- [ ] **Step 1: Run exact local verification on the implementation head**

```powershell
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Require:

```text
restore: PASS locked
build: 0 warnings / 0 errors
all tests: PASS / 0 FAIL / 0 SKIPPED unless an explicitly pre-existing skip is documented
```

- [ ] **Step 2: Scope audit against F3.3 base**

```bash
git diff --stat 8b5bfd35b59fbc75826f8d7616aaaca3e2f31233...HEAD
git diff --name-only 8b5bfd35b59fbc75826f8d7616aaaca3e2f31233...HEAD
```

Explicitly verify **no** unauthorized changes to:

```text
src/SGPdf.App/SGPdf.App.csproj
src/SGPdf.App/packages.lock.json
tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj
tests/SGPdf.App.Tests/packages.lock.json
src/SGPdf.App/Features/Sign/PdfVisualSignatureWriter.cs
PDFium interop / coordinate mapping
F3.2 processor
F3.3 InkCanvas renderer/history behavior
ZPL / Labelize
PDFsharp scope
network/runtime service surface
```

Expected new product files are limited to feature-local library store/model/file-op seam, two small WPF dialogs, focused MainWindow sign integration and focused tests.

- [ ] **Step 3: Failure-policy self-audit**

Re-read the five Review Focus points and confirm test names/evidence exist for each. Specifically inspect disk-side effects in tests rather than relying only on thrown exception types.

- [ ] **Step 4: Update closure docs**

Record:

- final functional head before closure docs;
- focused RED commits/runs and GREEN commits/runs;
- final test count;
- build warning/error count;
- exact scope audit;
- manual Windows QA as **NOT RUN** unless actually performed;
- no new dependency/network/PDF/ZPL scope.

- [ ] **Step 5: Commit closure docs only**

```bash
git add .planning/STATE.md .planning/ROADMAP.md .planning/phases/04-f3-visual-signature/F3.4-PLAN.md docs/history/2026-10-08-F3.4.md
git commit -m "docs(sign): close F3.4 local signature library"
```

After this closure commit, do not mutate the branch merely to write CI IDs into repository files.

- [ ] **Step 6: Push and require exact-head CI PASS**

The existing GitHub workflow must pass repository hygiene, pinned Labelize staging, locked restore, Release build and all tests on the exact closure SHA.

- [ ] **Step 7: Open/update draft PR stacked on F3.3**

PR base: `feat/f3-3-drawn-signature`.  
PR head: `feat/f3-4-local-signature-library`.  
Keep draft/unmerged.

Record final push/PR CI IDs in the PR body after they exist, without changing the code/docs head just to record those IDs.

- [ ] **Step 8: Manual QA remains a separate gate**

Do not call hardware/UX PASS from CI. Later Windows QA should cover save→restart→reuse, rename/delete, LocalAppData inspection, placed-copy survival after deletion, and network-disabled operation.

---

## Planned File Map

```text
Create
  src/SGPdf.App/Features/Sign/SignatureLibraryItem.cs
  src/SGPdf.App/Features/Sign/SignatureLibraryFileOps.cs
  src/SGPdf.App/Features/Sign/SignatureLibraryStore.cs
  src/SGPdf.App/SignatureLibraryDialog.xaml
  src/SGPdf.App/SignatureLibraryDialog.xaml.cs
  src/SGPdf.App/SignatureNameDialog.xaml
  src/SGPdf.App/SignatureNameDialog.xaml.cs
  tests/SGPdf.App.Tests/SignatureLibraryStoreTests.cs
  tests/SGPdf.App.Tests/SignatureLibraryDialogTests.cs
  tests/SGPdf.App.Tests/MainWindowSignatureLibraryTests.cs
  docs/history/2026-10-08-F3.4.md          # closure only

Modify during implementation
  src/SGPdf.App/MainWindow.Sign.cs

Modify during closure
  .planning/STATE.md
  .planning/ROADMAP.md
  .planning/phases/04-f3-visual-signature/F3.4-PLAN.md

Must remain unchanged
  src/SGPdf.App/Features/Sign/PdfVisualSignatureWriter.cs
  src/SGPdf.App/Features/Sign/SignatureCoordinateMapper.cs
  src/SGPdf.App/Features/Sign/SignatureImageProcessor.cs
  src/SGPdf.App/Features/Sign/SignatureInkRenderer.cs
  src/SGPdf.App/Features/Sign/SignatureInkHistory.cs
  src/SGPdf.App/SGPdf.App.csproj
  src/SGPdf.App/packages.lock.json
  tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj
  tests/SGPdf.App.Tests/packages.lock.json
```

## Plan Self-Audit

This plan covers every automated-acceptance section of the approved spec:

- manifest/model + corruption preservation → Task 1;
- Add/PNG round-trip + display-name rules → Task 2;
- atomicity/failure ordering/orphans → Task 2;
- missing/corrupt item isolation → Task 3;
- empty/dialog/manage/use UX → Task 3;
- selected-placement asset identity + existing placement pipeline → Task 4;
- full regression/dependency/architecture boundaries → Task 5;
- manual Windows QA kept separate and explicitly NOT inferred from CI → Task 5.

No spec requirement requires a new package, PDF writer change, database, network, encryption, global repository abstraction or second selection/dirty-state model. If implementation contradicts that statement, stop and return to design before proceeding.
