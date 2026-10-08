# F3.4 — Local Signature Library Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let the user save, reuse, rename and delete visual signature assets locally between PDFs while preserving the existing F3.1 placement/save behavior and remaining fully offline.

**Architecture:** F3.4 adds one feature-local persistence boundary around the existing immutable `SignatureAsset`. A small `SignatureLibraryStore` owns the LocalAppData root, manifest v1 validation, lossless PNG persistence and failure-safe publication. A focused WPF `SignatureLibraryDialog` presents healthy/broken items and returns only `SignatureAsset?` to `MainWindow`. `MainWindow.Sign.cs` supplies the currently selected placement's existing asset for **Guardar firma seleccionada...** and, on successful **Usar**, calls the proven `AddSignatureAsset(...)` exactly once. No second placement model, PDF writer, dirty-state model, database or network surface is introduced.

**Tech Stack:** C# / .NET 10 / WPF; `System.Text.Json`; `System.IO`; WPF `BitmapSource` / `PngBitmapEncoder`; existing `SignatureAsset`, `SignaturePngLoader`, `SignatureEditState`, `AddSignatureAsset(...)`; xUnit. No new runtime package.

**Spec:** `docs/superpowers/specs/2026-10-08-f3-4-local-signature-library-design.md`

**Spec gate:** approved by the user on 2026-10-08 by asking to continue after the continuity/spec review. Product code remains untouched until this implementation plan is separately approved.

## Global Constraints

- Work only on `feat/f3-4-local-signature-library`, stacked on F3.3 final head `8b5bfd35b59fbc75826f8d7616aaaca3e2f31233`.
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
- **Use** returns an existing `SignatureAsset` to MainWindow and then calls existing `AddSignatureAsset(...)` exactly once. Never duplicate centering, coordinates, selection or dirty-state logic.
- Save/rename/delete library operations are not PDF edits and must not alter PDF dirty/selection/page/render state.
- No new NuGet dependency, SQLite/database, encryption/vault, cloud/network/HTTP, external process, repository framework, app-wide DI, thumbnail cache or migration framework.
- Do not modify `PdfVisualSignatureWriter`, PDFium P/Invoke/coordinates, F3.2 processing, F3.3 ink behavior, ZPL/Labelize, PDFsharp scope, `.csproj` or lockfiles. If implementation appears to require any of those, stop and return to design.

## Review Focus

1. **Corrupt-manifest preservation:** invalid JSON/version/IDs/path metadata disables the library without rewriting user data or breaking the rest of FIRMAR.
2. **Two-file Add atomicity:** a PNG or manifest publication failure never leaves `library.json` referencing an unpublished PNG; rollback targets only files created by that operation.
3. **Delete semantics:** after successful manifest removal, later PNG-delete failure is a cleanup warning/orphan, not a reason to resurrect the item.
4. **Broken-item isolation:** a missing/corrupt PNG makes only that item unavailable while healthy entries remain usable and the broken metadata remains manageable.
5. **MainWindow state safety:** dialog cancel/failure preserves placements/selection/dirty state; successful Use adds exactly one normal centered selected dirty placement through F3.1.

---

### Task 1: Manifest model, LocalAppData root and safe read boundary

**Files:**
- Create: `src/SGPdf.App/Features/Sign/SignatureLibraryItem.cs`
- Create: `src/SGPdf.App/Features/Sign/SignatureLibraryStore.cs`
- Test: `tests/SGPdf.App.Tests/SignatureLibraryStoreTests.cs`

**Task-1 contracts:**

```csharp
internal sealed record SignatureLibraryItem(
    Guid Id,
    string DisplayName,
    string FileName);

internal sealed class SignatureLibraryUnavailableException : InvalidDataException
{
    internal SignatureLibraryUnavailableException(string message, Exception? inner = null);
}

internal sealed class SignatureLibraryStore
{
    internal SignatureLibraryStore(string? rootDirectory = null);

    internal string RootDirectory { get; }
    internal IReadOnlyList<SignatureLibraryItem> Load();
    internal SignatureAsset LoadAsset(Guid id);
}
```

Important dependency ordering: **Task 1 does not reference `SignatureLibraryFileOps`**. The deterministic failure seam is introduced only in Task 2 when mutation atomicity needs it.

Production root:

```csharp
Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "SG PDF Editor",
    "Signatures")
```

Manifest read rules:

- missing root / missing `library.json` → empty list, no creation;
- `System.Text.Json`, UTF-8, require version 1 and non-null items;
- every ID valid/non-empty/unique;
- every filename simple basename, `.png`, non-rooted, no separator/traversal;
- preserve manifest order;
- structural failure throws controlled `SignatureLibraryUnavailableException` and leaves disk byte-for-byte untouched;
- orphan PNGs ignored;
- `LoadAsset(id)` resolves only a validated manifest entry and delegates decode/safety to `SignaturePngLoader.Load(...)`.

- [ ] **Step 1: Write RED tests**

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

Use unique roots under `Path.GetTempPath()`. Capture manifest bytes before failure checks and assert exact preservation.

- [ ] **Step 2: Confirm RED**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignatureLibraryStoreTests"
```

Expected: failure only because Task-1 library contracts do not exist.

- [ ] **Step 3: Implement minimum read-only store.**

No directory creation from normal browse/load. No generic repository/filesystem framework.

- [ ] **Step 4: GREEN + regression**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignatureLibraryStoreTests"
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Require focused PASS, locked restore PASS, Release build 0 warnings/0 errors and all existing tests PASS.

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

**Task-2 contracts:**

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
```

Task 2 changes/adds the store mutation constructor seam without breaking Task 1 callers:

```csharp
internal SignatureLibraryStore(
    string? rootDirectory = null,
    SignatureLibraryFileOps? fileOps = null);

internal SignatureLibraryItem Add(string displayName, SignatureAsset asset);
internal SignatureLibraryItem Rename(Guid id, string displayName);
internal SignatureLibraryDeleteResult Delete(Guid id);
```

`SignatureLibraryFileOps` is feature-local and only exists so tests can deterministically fail publication/delete boundaries. Production defaults are direct `System.IO` operations.

PNG path:

```text
SignatureAsset BGRA
→ BitmapSource.Create(... PixelFormats.Bgra32 ...)
→ PngBitmapEncoder
→ unique temp file in RootDirectory
→ same-volume publication
```

Rules:

- no resize/reprocess/JPEG;
- new GUID + `<id:N>.png`;
- validate current library and name before creating root/writing;
- Add publishes PNG before manifest;
- existing manifest replacement preserves old manifest if replacement fails;
- Add manifest failure best-effort deletes only the just-published PNG; rollback-delete failure leaves an ignored orphan;
- Rename changes manifest only;
- Delete publishes manifest removal before best-effort PNG delete;
- never sweep unrelated orphan files.

Name rules:

```text
trim → reject empty → reject duplicate OrdinalIgnoreCase → preserve resulting Unicode/casing
```

- [ ] **Step 1: RED Add/round-trip tests**

```text
Add_TrimsNameAndGeneratesGuidBasedPngName
Add_RejectsWhitespaceAndCaseInsensitiveDuplicateWithoutMutation
Add_CreatesRootLazilyOnlyAfterValidation
Add_PublishesPngBeforeManifestReferencesIt
Add_RoundTrip_PreservesDimensionsAndAlphaSemantics
Add_RoundTrip_LoadAssetPassesExistingPngValidation
Add_DisplayNameCharactersNeverAffectPhysicalFilename
```

- [ ] **Step 2: RED failure-order tests**

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

- [ ] **Step 3: Confirm RED**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignatureLibraryStoreTests"
```

- [ ] **Step 4: Implement mutation/atomicity behavior minimally.**

Temporary PNG/manifest files stay in the same library directory. Never truncate `library.json` in place and never auto-repair corrupt metadata.

- [ ] **Step 5: GREEN + regression**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignatureLibraryStoreTests"
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

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
- Test: `tests/SGPdf.App.Tests/SignatureLibraryDialogTests.cs`

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

Production `Open(...)` creates the default store. No service locator/DI container.

A small dialog-local row model may contain:

```csharp
internal sealed record SignatureLibraryDialogItem(
    Guid Id,
    string DisplayName,
    BitmapSource? Thumbnail,
    bool IsAvailable,
    string? UnavailableText);
```

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

**Narrow test seams:**

```csharp
private Func<string, string?, string?> _promptName = ...;
private Func<string, bool> _confirmDelete = ...;
```

Equivalent owner-aware signatures are acceptable if WPF needs them. Keep private/dialog-local.

Behavior:

- valid empty library → `No hay firmas guardadas.`;
- corrupt manifest → unavailable state; Use/mutation disabled; Close works; no repair;
- load metadata once, then load each PNG independently;
- missing/corrupt PNG remains visible as broken; Use disabled; Rename/Delete allowed; healthy rows unaffected;
- bounded in-memory WPF thumbnail from actual asset, no persisted cache;
- Save selected enabled only if constructor received a placement asset;
- Save selected prompts name → store.Add(exact asset) → refresh/select new row → dialog stays open;
- Rename prompts current name → store.Rename → refresh/select same GUID;
- Delete requires explicit confirmation and surfaces cleanup warning without resurrecting item;
- Use reloads selected healthy asset at action time → `SelectedAsset` → successful close;
- Use-time load failure adds/returns nothing and keeps dialog manageable;
- Close/window-X/cancel → null.

- [ ] **Step 1: RED STA dialog tests**

```text
Dialog_EmptyLibrary_ShowsEmptyStateAndCorrectButtonStates
Dialog_SaveSelectedDisabledWhenNoSelectedPlacementAsset
Dialog_StructurallyInvalidManifest_DisablesMutationAndUseButCanClose
Dialog_HealthyEntry_ShowsNameAndThumbnail
Dialog_MissingPng_MarksOnlyThatItemUnavailableAndLeavesHealthyEntryUsable
Dialog_CorruptPng_MarksOnlyThatItemUnavailable
Dialog_UseDisabledWithoutSelectionOrForBrokenItem
Dialog_SaveSelected_UsesSuppliedExactAssetRefreshesAndStaysOpen
Dialog_SaveSelected_EmptyOrDuplicateName_KeepsState
Dialog_Rename_KeepsSelectedGuidAndDoesNotRewriteImage
Dialog_DeleteCancel_IsStateSafe
Dialog_DeleteConfirmed_RemovesMetadataAndRefreshes
Dialog_DeleteCleanupWarning_LeavesItemLogicallyRemovedAndShowsWarning
Dialog_UseHealthyItem_ReturnsLoadedSignatureAsset
Dialog_UseTimeFailure_ReturnsNothingAndKeepsDialogOpen
Dialog_CloseOrWindowX_ReturnsNoAsset
```

Follow the existing F3 `RunInSta` + named-control test pattern. Do not require interactive MessageBox clicks in CI.

- [ ] **Step 2: Confirm RED**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignatureLibraryDialogTests"
```

- [ ] **Step 3: Implement focused dialogs.**

UI stays KISS: thumbnail/name/status + Use/Save selected/Rename/Delete/Close. No search, tags, favorites, folders, sorting, reordering or settings page.

- [ ] **Step 4: GREEN + regression**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~SignatureLibraryDialogTests"
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

- [ ] **Step 5: Commit**

```bash
git add src/SGPdf.App/SignatureLibraryDialog.xaml src/SGPdf.App/SignatureLibraryDialog.xaml.cs src/SGPdf.App/SignatureNameDialog.xaml src/SGPdf.App/SignatureNameDialog.xaml.cs tests/SGPdf.App.Tests/SignatureLibraryDialogTests.cs
git commit -m "feat(sign): add local signature library dialog"
```

---

### Task 4: Integrate library into existing FIRMAR path

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

`GetSelectedSignatureAsset()` reuses only:

```text
_signatureEditState.SelectedId
→ matching SignaturePlacement in Placements
→ placement.Asset
```

No second selected-asset field/model.

**Named action:**

```text
Name = SignatureLibraryButton
Content = Biblioteca de firmas...
```

Add in `BuildSignaturePropertiesPanel()` after PNG/photo/draw source actions and before placement-management buttons. Preserve code-built FIRMAR UI; do not rewrite `MainWindow.xaml`.

Behavior:

- dialog receives exact selected placement asset or null;
- null return = close/cancel/no Use, no PDF-state mutation;
- exception = controlled status/message, no state mutation;
- non-null return = existing `AddSignatureAsset(asset)` exactly once;
- F3.1 remains authority for centering, coordinates, selection, dirty state and overlay;
- Save/Rename/Delete happen in dialog/store and never directly change `_signatureEditState`;
- placed asset remains valid in memory after library rename/delete.

- [ ] **Step 1: RED MainWindow tests**

```text
LibraryAction_ExistsExactlyOnceInFirmarPanel
LibraryOpenClose_PreservesPriorPlacementsSelectionAndDirtyState
LibraryOpen_ReceivesExactSelectedPlacementAsset_NotPlacementRasterization
LibraryOpen_WithNoSelectedPlacement_ReceivesNull
LibraryFailure_PreservesPriorPdfSignatureState
LibraryUse_AddsExactlyOneNormalCenteredSelectedDirtyPlacement
LibraryUse_UsesSameAddSignatureAssetGeometryAsDirectSource
LibraryRenameDeleteDoNotMutateAlreadyPlacedAsset
ExistingPngPhotoDrawActionsRemainAvailableAndUnchanged
```

Use existing reflection/delegate/STA style from `MainWindowSignatureDrawTests`. For selected asset use `Assert.Same` to guard object identity.

- [ ] **Step 2: Confirm RED**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~MainWindowSignatureLibraryTests"
```

- [ ] **Step 3: Implement minimal integration.**

Do not alter `AddSignatureAsset(...)`, coordinate mapper, PDF writer, photo/draw preparation or Save As behavior.

- [ ] **Step 4: GREEN + F3 regression + full regression**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj --configuration Release --filter "FullyQualifiedName~MainWindowSignatureLibraryTests|FullyQualifiedName~MainWindowSignatureTests|FullyQualifiedName~MainWindowSignaturePhotoTests|FullyQualifiedName~MainWindowSignatureDrawTests"
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

- [ ] **Step 5: Commit**

```bash
git add src/SGPdf.App/MainWindow.Sign.cs tests/SGPdf.App.Tests/MainWindowSignatureLibraryTests.cs
git commit -m "feat(sign): integrate reusable signature library"
```

---

### Task 5: Closure, architecture audit, exact-head CI and draft PR

**Files:**
- Modify: `.planning/STATE.md`
- Modify: `.planning/ROADMAP.md`
- Modify: `.planning/phases/04-f3-visual-signature/F3.4-PLAN.md`
- Create: `docs/history/2026-10-08-F3.4.md`
- No product-code changes in closure-only commit.

- [ ] **Step 1: Exact local verification**

```powershell
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx --configuration Release --no-restore
dotnet test SGPdf.slnx --configuration Release --no-build
```

Require locked restore PASS, build 0 warnings/0 errors, all tests PASS.

- [ ] **Step 2: Scope audit vs F3.3**

```bash
git diff --stat 8b5bfd35b59fbc75826f8d7616aaaca3e2f31233...HEAD
git diff --name-only 8b5bfd35b59fbc75826f8d7616aaaca3e2f31233...HEAD
```

Explicitly verify no unauthorized changes to:

```text
src/SGPdf.App/SGPdf.App.csproj
src/SGPdf.App/packages.lock.json
tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj
tests/SGPdf.App.Tests/packages.lock.json
src/SGPdf.App/Features/Sign/PdfVisualSignatureWriter.cs
SignatureCoordinateMapper / PDFium interop
F3.2 photo processor
F3.3 InkCanvas renderer/history
ZPL / Labelize
PDFsharp scope
network/runtime service surface
```

- [ ] **Step 3: Recheck the five Review Focus items against concrete tests and disk-side effects.**

- [ ] **Step 4: Update closure docs** with RED/GREEN evidence, functional head, test count, 0/0 build, scope audit and manual Windows QA still NOT RUN unless actually executed.

- [ ] **Step 5: Closure-doc commit only**

```bash
git add .planning/STATE.md .planning/ROADMAP.md .planning/phases/04-f3-visual-signature/F3.4-PLAN.md docs/history/2026-10-08-F3.4.md
git commit -m "docs(sign): close F3.4 local signature library"
```

Do not mutate branch afterward merely to write CI IDs into repo files.

- [ ] **Step 6: Push and require exact-head GitHub CI PASS** for hygiene, pinned Labelize staging, locked restore, Release build and tests.

- [ ] **Step 7: Open/update draft stacked PR**

```text
base = feat/f3-3-drawn-signature
head = feat/f3-4-local-signature-library
draft = true
```

Record push/PR CI IDs in PR body after they exist without creating another code/docs commit solely for IDs.

- [ ] **Step 8: Keep manual Windows QA separate**: save→restart→reuse, rename/delete, inspect LocalAppData, confirm placed-copy survival, network disabled.

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

Coverage against the approved spec:

- manifest/model + corruption preservation → Task 1;
- Add/PNG round-trip + name rules → Task 2;
- atomic publication/failure ordering/orphans → Task 2;
- broken-item isolation → Task 3;
- empty/manage/use dialog UX → Task 3;
- selected-placement asset identity + existing placement path → Task 4;
- full regression/dependency/architecture boundaries → Task 5;
- manual Windows QA remains separate and is never inferred from CI → Task 5.

Dependency-order audit:

- Task 1 compiles independently with `SignatureLibraryStore(string? rootDirectory = null)` and contains no reference to `SignatureLibraryFileOps`.
- Task 2 creates `SignatureLibraryFileOps` and only then extends the store constructor with the optional failure seam.
- Task 3 depends only on the fully GREEN Task-2 store.
- Task 4 depends only on the GREEN dialog contract and existing F3.1 placement path.

No spec requirement needs a new package, PDF writer change, database, network, encryption, global repository abstraction or second selection/dirty-state model. If implementation contradicts that statement, stop and return to design.
