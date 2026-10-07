# F2.1 ZPL Parse + Open Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Open local `.zpl`, `.txt`, and `.prn` files, parse printable ZPL designs and `^PQ` quantities entirely in managed C#, and switch the WPF workspace to a safe ZPL-loaded placeholder without invoking Labelize yet.

**Architecture:** Add a small `Features/Labels/` slice with immutable document/design records, a pure `ZplDocumentParser`, and a strict local `ZplFileLoader`. The parser preserves one normalized document-level render stream so `^DF/^XF` context survives into F2.2, while user-visible designs carry only metadata and file quantities. `MainWindow` gets a thin open-ZPL command; state changes occur only after parsing succeeds so an invalid label file never destroys the current valid PDF/ZPL session.

**Tech Stack:** C# / .NET 10 / WPF / xUnit. No Labelize runtime dependency in F2.1.

**Spec:** `docs/superpowers/specs/2026-10-07-f2-labelize-architecture-design.md`

## Global Constraints

- Windows x64; C# + .NET 10 LTS + WPF.
- KISS/YAGNI: no new Core/Application/Infrastructure layers, DI framework, plugin abstraction, or dual-engine abstraction.
- Fully local/offline; no HTTP, sockets, services, API keys, cloud calls, or runtime downloads.
- Real Mercado Libre/customer ZPL never enters Git, CI artifacts, or Graphify.
- F2.1 must not add Labelize, BinaryKits, PDFsharp, ZXing, or any other runtime/package dependency.
- `^PQ` is quantity metadata, not a render multiplier.
- `^DF/^XF` source order/context must remain available in the normalized document stream for F2.2.
- A failed ZPL open must preserve the current valid document state.
- No merge to `main` without explicit user approval.

## Review Focus

1. **Malformed block boundaries** — unmatched `^XA` / `^XZ` must fail as a controlled parse error rather than silently dropping content. Covered in Task 1 parser tests.
2. **Quantity boundary behavior** — absent `^PQ` defaults to 1; `^PQ0` normalizes to 1; values above Zebra's documented maximum `99,999,999` fail instead of creating absurd metadata. Covered in Task 2.
3. **Multiple `^PQ` commands in one printable block** — the last command is authoritative and all `^PQ` commands are removed from `NormalizedRenderSource`. Covered in Task 2.
4. **Stored-format quantity ambiguity** — `^PQ` inside a `^DF` definition is rejected in F2.1 rather than silently assigning the wrong quantity to later `^XF` calls. Covered in Task 2 and revisited only if the private corpus proves it is needed.
5. **Invalid local encoding / unsupported extension** — invalid UTF-8 and files outside `.zpl/.txt/.prn` fail before UI state changes. Covered in Task 3.

---

## File Structure

Create:
- `src/SGPdf.App/Features/Labels/ZplDesign.cs` — immutable metadata for one user-visible printable design.
- `src/SGPdf.App/Features/Labels/ZplDocument.cs` — immutable parsed document plus normalized document-level render source.
- `src/SGPdf.App/Features/Labels/ZplDocumentParser.cs` — pure block discovery, printable-design classification, quantity extraction/removal, malformed-input checks.
- `src/SGPdf.App/Features/Labels/ZplFileLoader.cs` — supported-extension and strict UTF-8 local file loading.
- `tests/SGPdf.App.Tests/ZplDocumentParserTests.cs` — synthetic parser/quantity/stored-format coverage.
- `tests/SGPdf.App.Tests/ZplFileLoaderTests.cs` — local temp-file loader coverage.
- `.planning/phases/02-f1-zpl-gate/F2.1-PLAN.md` — compact GSD execution/acceptance mirror for this slice.
- `docs/history/2026-10-07-F2.1.md` — portable completion history, created only after final verification.

Modify:
- `src/SGPdf.App/MainWindow.xaml` — add `Archivo > Abrir etiquetas ZPL...`.
- `src/SGPdf.App/MainWindow.xaml.cs` — candidate-load then commit workspace switch; PDF/ZPL state exclusivity; busy/error/status handling.
- `.planning/STATE.md` — record approved Labelize architecture, F1 synthetic result, F2.1 position and physical/private gates.
- `.planning/ROADMAP.md` — update stale current-position text only; do not mark F1 formally closed before private real samples.
- `docs/MASTER_CONTEXT.md` and `docs/MASTER_PLAN.md` — replace BinaryKits-preferred runtime architecture with the approved Labelize sidecar design and F2 slice sequence.

Do **not** modify in F2.1:
- `src/SGPdf.App/SGPdf.App.csproj` package references;
- `third_party/manifest.json` (Labelize is not shipped/called until F2.2);
- PDFium code, PDF print code, or existing PDF tests except where a workspace-state regression directly requires it.

---

### Task 1: ZPL document model and block parsing

**Files:**
- Create: `src/SGPdf.App/Features/Labels/ZplDesign.cs`
- Create: `src/SGPdf.App/Features/Labels/ZplDocument.cs`
- Create: `src/SGPdf.App/Features/Labels/ZplDocumentParser.cs`
- Create: `tests/SGPdf.App.Tests/ZplDocumentParserTests.cs`

**Interfaces:**
- Produces: `ZplDocumentParser.Parse(string sourcePath, string source) -> ZplDocument`
- Produces: `ZplDocument.SourcePath`, `OriginalSource`, `NormalizedRenderSource`, `IReadOnlyList<ZplDesign> Designs`, `TotalQuantityFromFile`
- Produces: `ZplDesign.Index`, `SourceBlockIndex`, `OriginalBlock`, `QuantityFromFile`
- Later tasks consume these exact names; do not rename them mid-plan.

- [ ] **Step 1: Write failing parser tests for basic blocks and stored-format classification**

Add tests with synthetic strings only:

```csharp
[Fact]
public void Parse_TwoPrintableBlocks_ReturnsTwoDesignsInSourceOrder()
{
    var source = "^XA^FO10,10^FDUno^FS^XZ\n^XA^FO10,10^FDDos^FS^XZ";

    var document = ZplDocumentParser.Parse("labels.zpl", source);

    Assert.Equal(2, document.Designs.Count);
    Assert.Equal(0, document.Designs[0].Index);
    Assert.Equal(0, document.Designs[0].SourceBlockIndex);
    Assert.Equal(1, document.Designs[1].Index);
    Assert.Equal(1, document.Designs[1].SourceBlockIndex);
}

[Fact]
public void Parse_DfDefinitionThenXfInvocation_ExposesOnlyPrintableInvocation()
{
    var source = "^XA^DFR:FORM.ZPL^FO10,10^FN1^FS^XZ\n^XA^XFR:FORM.ZPL^FS^FN1^FDOK^FS^XZ";

    var document = ZplDocumentParser.Parse("template.zpl", source);

    var design = Assert.Single(document.Designs);
    Assert.Equal(1, design.SourceBlockIndex);
    Assert.Contains("^DFR:FORM.ZPL", document.NormalizedRenderSource, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("^XFR:FORM.ZPL", document.NormalizedRenderSource, StringComparison.OrdinalIgnoreCase);
}
```

Also add:
- `Parse_NoXaXzBlocks_ThrowsInvalidDataException`
- `Parse_UnterminatedXa_ThrowsInvalidDataException`
- `Parse_StrayXzBeforeXa_ThrowsInvalidDataException`

- [ ] **Step 2: Run only the parser tests and verify RED**

Run:

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj -c Release --filter FullyQualifiedName~ZplDocumentParserTests
```

Expected: build/test FAIL because the `Features.Labels` types do not exist.

- [ ] **Step 3: Implement the immutable models**

Use namespace `SGPdf.App.Features.Labels`.

Required signatures:

```csharp
public sealed record ZplDesign(
    int Index,
    int SourceBlockIndex,
    string OriginalBlock,
    int QuantityFromFile);
```

```csharp
public sealed class ZplDocument
{
    public string SourcePath { get; }
    public string OriginalSource { get; }
    public string NormalizedRenderSource { get; }
    public IReadOnlyList<ZplDesign> Designs { get; }
    public int TotalQuantityFromFile { get; }
}
```

`TotalQuantityFromFile` is the checked sum of design quantities.

- [ ] **Step 4: Implement minimal block discovery/classification in `ZplDocumentParser`**

Required API:

```csharp
public static class ZplDocumentParser
{
    public static ZplDocument Parse(string sourcePath, string source);
}
```

Rules for this task:
- command matching is case-insensitive;
- identify complete `^XA ... ^XZ` blocks in source order;
- reject unmatched/stray boundaries with `InvalidDataException`;
- a block containing `^DF` is a template-definition/support block and is not user-visible in `Designs`;
- an `^XF` invocation block remains printable unless it is itself a `^DF` definition;
- preserve the full source-order document stream in `NormalizedRenderSource` for now; quantity normalization is Task 2;
- reject null arguments with standard argument exceptions.

- [ ] **Step 5: Run parser tests and full existing suite**

Run:

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj -c Release --filter FullyQualifiedName~ZplDocumentParserTests
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj -c Release
```

Expected: new parser tests PASS; all pre-existing PDF tests remain PASS.

- [ ] **Step 6: Commit Task 1**

```bash
git add src/SGPdf.App/Features/Labels tests/SGPdf.App.Tests/ZplDocumentParserTests.cs
git commit -m "feat(labels): parse ZPL document blocks"
```

---

### Task 2: `^PQ` quantity extraction and render normalization

**Files:**
- Modify: `src/SGPdf.App/Features/Labels/ZplDocumentParser.cs`
- Modify: `tests/SGPdf.App.Tests/ZplDocumentParserTests.cs`

**Interfaces:**
- Consumes: `ZplDocumentParser.Parse(...)`, `ZplDesign.QuantityFromFile`, `ZplDocument.NormalizedRenderSource` from Task 1.
- Produces: normalized quantity semantics that F2.2 can send to Labelize without duplicate preview renders.

- [ ] **Step 1: Add failing quantity tests**

Add exact cases:

```csharp
[Fact]
public void Parse_Pq28_StoresQuantityAndRemovesPrintMultiplicationFromRenderSource()
{
    var source = "^XA^FO10,10^FDLabel^FS^PQ28^XZ";

    var document = ZplDocumentParser.Parse("qty.zpl", source);

    var design = Assert.Single(document.Designs);
    Assert.Equal(28, design.QuantityFromFile);
    Assert.Equal(28, document.TotalQuantityFromFile);
    Assert.DoesNotContain("^PQ", document.NormalizedRenderSource, StringComparison.OrdinalIgnoreCase);
}
```

Also add:
- absent `^PQ` => quantity `1`;
- `^PQ0` => quantity `1`;
- `^PQ 3,0,0,N` => quantity `3` and command removed;
- two `^PQ` commands in one printable block => last one wins; both are removed;
- two printable blocks with `^PQ2` and `^PQ3` => total quantity `5`;
- `^PQ100000000` => `InvalidDataException`;
- `^PQ` with malformed non-numeric `q` => `InvalidDataException` rather than silently defaulting;
- `^PQ` inside a `^DF` definition => `InvalidDataException` with a message indicating stored-format quantity semantics are not silently guessed in F2.1;
- normalized source still contains `^DF`, `^XF`, `^CI28`, field data, and original source order.

- [ ] **Step 2: Run quantity tests and verify RED**

Run the parser test filter again.

Expected: quantity/default/normalization tests FAIL because Task 1 does not yet process `^PQ`.

- [ ] **Step 3: Implement quantity parsing/removal**

Inside `ZplDocumentParser` keep helpers private unless a test needs public behavior.

Pinned behavior:
- Zebra `q` range is `1..99,999,999`, default `1`;
- `0` is normalized to `1` for compatibility with the expected renderer/Labelary behavior;
- positive values above `99,999,999` are rejected;
- when multiple `^PQ` commands exist in one printable block, use the last command's `q`;
- remove every `^PQ...` command from `NormalizedRenderSource` without removing neighboring ZPL commands;
- do not expand the source or duplicate blocks.

- [ ] **Step 4: Run parser tests + full suite**

Expected: all parser tests PASS and no PDF regressions.

- [ ] **Step 5: Commit Task 2**

```bash
git add src/SGPdf.App/Features/Labels/ZplDocumentParser.cs tests/SGPdf.App.Tests/ZplDocumentParserTests.cs
git commit -m "feat(labels): preserve ZPL print quantities as metadata"
```

---

### Task 3: Strict local ZPL file loader

**Files:**
- Create: `src/SGPdf.App/Features/Labels/ZplFileLoader.cs`
- Create: `tests/SGPdf.App.Tests/ZplFileLoaderTests.cs`

**Interfaces:**
- Consumes: `ZplDocumentParser.Parse(string sourcePath, string source)`.
- Produces: `ZplFileLoader.Load(string filePath) -> ZplDocument`.

- [ ] **Step 1: Write failing file-loader tests**

Use test-created temporary files only. Cover:
- `.zpl`, `.txt`, `.prn` are accepted case-insensitively;
- UTF-8 Spanish text such as `Medellín Ñandú` survives unchanged in `OriginalSource`;
- UTF-8 BOM is accepted;
- unsupported `.pdf` / `.bin` extension throws `NotSupportedException`;
- nonexistent path throws `FileNotFoundException`;
- byte sequence invalid under strict UTF-8 throws a controlled decoding exception and does not produce a `ZplDocument`.

- [ ] **Step 2: Run loader tests and verify RED**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj -c Release --filter FullyQualifiedName~ZplFileLoaderTests
```

Expected: FAIL because `ZplFileLoader` does not exist.

- [ ] **Step 3: Implement `ZplFileLoader`**

Required API:

```csharp
public static class ZplFileLoader
{
    public static ZplDocument Load(string filePath);
}
```

Rules:
- resolve full path once;
- accept only `.zpl`, `.txt`, `.prn` using ordinal-ignore-case comparison;
- read locally with strict UTF-8 while allowing BOM detection;
- no encoding guessing, network fallback, temp copy, or external process in F2.1;
- pass the full path and decoded source to `ZplDocumentParser.Parse`.

- [ ] **Step 4: Run loader tests + full suite**

Expected: all loader/parser/PDF tests PASS.

- [ ] **Step 5: Commit Task 3**

```bash
git add src/SGPdf.App/Features/Labels/ZplFileLoader.cs tests/SGPdf.App.Tests/ZplFileLoaderTests.cs
git commit -m "feat(labels): load local ZPL files"
```

---

### Task 4: WPF open-ZPL flow and safe workspace switch

**Files:**
- Modify: `src/SGPdf.App/MainWindow.xaml`
- Modify: `src/SGPdf.App/MainWindow.xaml.cs`

**Interfaces:**
- Consumes: `ZplFileLoader.Load(...)` and `ZplDocument`.
- Produces: UI command `OpenZpl_Click`, private field `_zplDocument`, and a loaded-label placeholder state for F2.2.

- [ ] **Step 1: Add the UI command in XAML**

Under `_Archivo`, keep the existing PDF entry and add:

```xml
<MenuItem x:Name="OpenZplMenuItem"
          Header="Abrir _etiquetas ZPL..."
          Click="OpenZpl_Click" />
```

Do not rename the existing PDF command.

- [ ] **Step 2: Implement candidate-first `OpenZpl_Click`**

Required behavior:
- dialog title: `Abrir etiquetas ZPL`;
- filter: `ZPL / TXT / PRN (*.zpl;*.txt;*.prn)|*.zpl;*.txt;*.prn|Todos los archivos (*.*)|*.*`;
- `CheckFileExists = true`, `Multiselect = false`;
- parse on `Task.Run(() => ZplFileLoader.Load(dialog.FileName))`;
- do not dispose/mutate the active PDF or previous ZPL until the candidate parser returns successfully;
- on failure, show a controlled Spanish error and preserve the previous valid workspace;
- on success, cancel pending PDF resize work, dispose the old PDF session, clear `_session/_navigation`, assign `_zplDocument`, clear the PDF image, collapse PDF-only controls through the existing `UpdateViewerControlsUi()` path, and show the existing center empty-state text as the temporary F2.1 label placeholder;
- placeholder text: `Archivo ZPL cargado\n{N} diseño(s) detectado(s)`;
- status text includes filename, design count, and `TotalQuantityFromFile`;
- title becomes `SG PDF Editor — <filename>`.

- [ ] **Step 3: Make PDF-open success clear the ZPL workspace only after PDF candidate success**

In the existing `OpenPdf_Click`, set `_zplDocument = null` only at the same commit point where the successfully rendered candidate PDF becomes `_session`. An invalid PDF must preserve a loaded ZPL document exactly as an invalid ZPL preserves a valid PDF.

- [ ] **Step 4: Extend busy/close state without adding architecture**

- `SetBusy` disables both `OpenPdfMenuItem` and `OpenZplMenuItem` while busy.
- `OnClosed` clears `_zplDocument` in addition to existing PDF cleanup.
- no new view model, navigation framework, tabs, or mode service.

- [ ] **Step 5: Build and run the full suite**

```powershell
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx -c Release --no-restore
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj -c Release --no-build
```

Expected: restore/build/tests PASS; no package lock changes.

- [ ] **Step 6: Manual Windows smoke for this thin UI slice**

When an interactive Windows desktop is available, verify:
1. `Archivo > Abrir etiquetas ZPL...` exposes `.zpl/.txt/.prn`;
2. valid synthetic file shows correct design/quantity summary;
3. `^PQ28` still shows one design, quantity total 28;
4. invalid ZPL error leaves the prior PDF/ZPL unchanged;
5. valid ZPL after PDF hides PDF navigation/image and updates title/status;
6. valid PDF after ZPL replaces the ZPL placeholder only after PDF render succeeds.

If no interactive Windows desktop is available, record this smoke as **NOT RUN** rather than PASS.

- [ ] **Step 7: Commit Task 4**

```bash
git add src/SGPdf.App/MainWindow.xaml src/SGPdf.App/MainWindow.xaml.cs
git commit -m "feat(labels): open ZPL files from WPF"
```

---

### Task 5: F2.1 source-of-truth updates and exact-head verification

**Files:**
- Create: `.planning/phases/02-f1-zpl-gate/F2.1-PLAN.md`
- Create after verification: `docs/history/2026-10-07-F2.1.md`
- Modify: `.planning/STATE.md`
- Modify: `.planning/ROADMAP.md`
- Modify: `docs/MASTER_CONTEXT.md`
- Modify: `docs/MASTER_PLAN.md`

**Interfaces:**
- Consumes: verified F2.1 behavior from Tasks 1–4 and approved spec.
- Produces: portable project continuity and authoritative Labelize architecture references.

- [ ] **Step 1: Update architecture/state documents without overclaiming gates**

Record:
- Labelize 1.7.0 is the approved F2 engine selection;
- integration model is bundled local CLI sidecar starting in F2.2;
- BinaryKits is historical gate evidence, not a fallback runtime dependency;
- F1 synthetic gate is PASS for engine selection but F1 formal closure still waits for private real Mercado Libre samples;
- F0 physical Windows smoke remains pending;
- F2.1 adds only managed parsing/open/quantity and no Labelize runtime dependency.

Update stale `ROADMAP` current-position text, but do not check off F1 requirements that explicitly require the private real corpus.

- [ ] **Step 2: Create compact F2.1 GSD plan/history entries**

`F2.1-PLAN.md` must capture scope, acceptance criteria, TDD evidence, and manual smoke status.

`docs/history/2026-10-07-F2.1.md` is written after exact-head CI so it can contain final branch/head/run/PR evidence.

- [ ] **Step 3: Run exact final verification**

Run locally/CI on the documentation head:

```powershell
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx -c Release --no-restore
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj -c Release --no-build
```

Also require existing repository hygiene/offline guards to remain green.

Expected: zero new runtime packages; Release build and all tests PASS.

- [ ] **Step 4: Open/update draft PR without merge**

Execution branch name: `feat/f2-1-zpl-parse-open`.

Create it from the approved design/plan head. Preferred stacked PR base: `design/f2-labelize-architecture`. Keep PR draft, include RED/GREEN/final exact-head CI evidence, and do not merge.

- [ ] **Step 5: Commit final docs and re-run CI if docs changed after the prior run**

The completion claim must reference CI against the exact final commit containing both code and final docs.

---

## Self-Review Result

- **Spec coverage:** F2.1 covers the spec's parse/open/quantity slice and deliberately excludes Labelize execution, preview rendering, dimensions, layout, PDF export and print; those remain F2.2+.
- **Step scan:** every implementation step names an exact interface, behavior or verification command; no `TBD`/placeholder implementation decisions remain.
- **Type consistency:** later tasks consume the exact `ZplDocument`, `ZplDesign`, `ZplDocumentParser.Parse` and `ZplFileLoader.Load` names defined in earlier tasks.
- **Review Focus:** malformed boundaries, quantity limits/multiples, stored-format quantity ambiguity and invalid encoding/extensions all have explicit owning tests.
- **Proportion:** plan describes interfaces/tests and leaves method bodies to the implementer; it does not embed a full parser implementation.
