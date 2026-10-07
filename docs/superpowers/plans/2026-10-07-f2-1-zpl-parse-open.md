# F2.1 ZPL Parse + Open Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Open local `.zpl`, `.txt`, and `.prn` files, parse printable ZPL designs and `^PQ` quantities entirely in managed C#, and switch the WPF workspace to a safe ZPL-loaded placeholder without invoking Labelize yet.

**Architecture:** Add one focused `Features/Labels/` slice with immutable document/design models, a pure `ZplDocumentParser`, and a strict local `ZplFileLoader`. The parser produces one normalized document-level render stream so `^DF/^XF` context survives into F2.2. `MainWindow` gets a thin candidate-first open command: only a successfully parsed ZPL replaces the active PDF/ZPL state.

**Tech Stack:** C# / .NET 10 / WPF / xUnit. No Labelize runtime dependency in F2.1.

**Spec:** `docs/superpowers/specs/2026-10-07-f2-labelize-architecture-design.md`

## Global Constraints

- Windows x64; C# + .NET 10 LTS + WPF.
- KISS/YAGNI; no new architectural layers, DI framework, plugin abstraction, or dual-engine abstraction.
- Fully local/offline; no HTTP, sockets, API calls, runtime downloads, or customer-data uploads.
- Real Mercado Libre/customer ZPL never enters Git, CI artifacts, or Graphify.
- F2.1 adds no Labelize, BinaryKits, PDFsharp, ZXing, or other package/runtime dependency.
- `^PQ` is quantity metadata, never preview-render multiplication.
- `^DF/^XF` source order/context must survive in `NormalizedRenderSource`.
- Failed PDF/ZPL opens preserve the current valid workspace.
- No merge to `main` without explicit user approval.

## Review Focus

1. Malformed `^XA/^XZ` boundaries fail with `InvalidDataException`; no silent partial parse.
2. Quantity semantics: absent `^PQ` => 1; `^PQ0` => 1; maximum accepted `q` = `99,999,999`; above max fails.
3. Multiple `^PQ` commands in one printable block: last command wins; all are removed from normalized render input.
4. `^PQ` inside a `^DF` definition is rejected in F2.1 rather than silently assigning incorrect effective quantity to later `^XF` calls.
5. Invalid UTF-8 and unsupported extensions fail before UI state changes.

---

## File Map

Create:
- `src/SGPdf.App/Features/Labels/ZplDesign.cs`
- `src/SGPdf.App/Features/Labels/ZplDocument.cs`
- `src/SGPdf.App/Features/Labels/ZplDocumentParser.cs`
- `src/SGPdf.App/Features/Labels/ZplFileLoader.cs`
- `tests/SGPdf.App.Tests/ZplDocumentParserTests.cs`
- `tests/SGPdf.App.Tests/ZplFileLoaderTests.cs`
- `.planning/phases/02-f1-zpl-gate/F2.1-PLAN.md`
- `docs/history/2026-10-07-F2.1.md` only after exact-head verification.

Modify:
- `src/SGPdf.App/MainWindow.xaml`
- `src/SGPdf.App/MainWindow.xaml.cs`
- `.planning/STATE.md`
- `.planning/ROADMAP.md`
- `docs/MASTER_CONTEXT.md`
- `docs/MASTER_PLAN.md`

Do not modify package references or `third_party/manifest.json` in F2.1. Labelize packaging starts in F2.2.

---

### Task 1: Document model + basic block parser

**Files:** create `ZplDesign.cs`, `ZplDocument.cs`, `ZplDocumentParser.cs`, `ZplDocumentParserTests.cs`.

**Interfaces:**

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
    public long TotalQuantityFromFile { get; }
}
```

```csharp
public static class ZplDocumentParser
{
    public static ZplDocument Parse(string sourcePath, string source);
}
```

- [ ] **Step 1: Write RED tests**

Add these tests:
- `Parse_TwoPrintableBlocks_ReturnsTwoDesignsInSourceOrder`
- `Parse_DfDefinitionThenXfInvocation_ExposesOnlyPrintableInvocation`
- `Parse_NoXaXzBlocks_ThrowsInvalidDataException`
- `Parse_UnterminatedXa_ThrowsInvalidDataException`
- `Parse_StrayXzBeforeXa_ThrowsInvalidDataException`

Core assertions for the first two:

```csharp
var document = ZplDocumentParser.Parse("labels.zpl",
    "^XA^FO10,10^FDUno^FS^XZ\n^XA^FO10,10^FDDos^FS^XZ");
Assert.Equal(2, document.Designs.Count);
Assert.Equal(0, document.Designs[0].Index);
Assert.Equal(1, document.Designs[1].SourceBlockIndex);
```

```csharp
var document = ZplDocumentParser.Parse("template.zpl",
    "^XA^DFR:FORM.ZPL^FO10,10^FN1^FS^XZ\n^XA^XFR:FORM.ZPL^FS^FN1^FDOK^FS^XZ");
Assert.Single(document.Designs);
Assert.Equal(1, document.Designs[0].SourceBlockIndex);
Assert.Contains("^DFR:FORM.ZPL", document.NormalizedRenderSource, StringComparison.OrdinalIgnoreCase);
Assert.Contains("^XFR:FORM.ZPL", document.NormalizedRenderSource, StringComparison.OrdinalIgnoreCase);
```

- [ ] **Step 2: Verify RED**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj -c Release --filter FullyQualifiedName~ZplDocumentParserTests
```

Expected: FAIL because label types/parser do not exist.

- [ ] **Step 3: Implement minimal parser**

Rules:
- namespace `SGPdf.App.Features.Labels`;
- command matching case-insensitive;
- discover complete `^XA ... ^XZ` blocks in source order;
- reject unmatched/stray boundaries;
- a block containing a real `^DF` command is support/template-only, not user-visible;
- an `^XF` invocation block is printable unless it is itself a `^DF` definition;
- `NormalizedRenderSource` initially preserves complete source order;
- null inputs use standard argument exceptions;
- `TotalQuantityFromFile` is a checked `long` sum; each design quantity remains `int`.

- [ ] **Step 4: Verify GREEN + regression**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj -c Release --filter FullyQualifiedName~ZplDocumentParserTests
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj -c Release
```

Expected: parser tests PASS and existing PDF tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/SGPdf.App/Features/Labels tests/SGPdf.App.Tests/ZplDocumentParserTests.cs
git commit -m "feat(labels): parse ZPL document blocks"
```

---

### Task 2: `^PQ` extraction + normalized render stream

**Files:** modify `ZplDocumentParser.cs` and `ZplDocumentParserTests.cs`.

**Consumes:** Task 1 interfaces.  
**Produces:** quantity metadata and render input with print multiplication removed.

- [ ] **Step 1: Add RED quantity tests**

Required cases:
- `^PQ28` => one design, quantity 28, total `28L`, no `^PQ` in normalized source;
- absent `^PQ` => quantity 1;
- `^PQ0` => quantity 1;
- `^PQ 3,0,0,N` => quantity 3;
- two `^PQ` commands in one printable block => last `q` wins and both commands disappear;
- two printable blocks `^PQ2` + `^PQ3` => total `5L`;
- `^PQ100000000` => `InvalidDataException`;
- malformed nonnumeric `q` => `InvalidDataException`;
- `^PQ` inside a `^DF` definition => `InvalidDataException` with a controlled message;
- normalized source still contains `^DF`, `^XF`, `^CI28`, field data and original block order.

Example:

```csharp
var document = ZplDocumentParser.Parse("qty.zpl", "^XA^FO10,10^FDLabel^FS^PQ28^XZ");
var design = Assert.Single(document.Designs);
Assert.Equal(28, design.QuantityFromFile);
Assert.Equal(28L, document.TotalQuantityFromFile);
Assert.DoesNotContain("^PQ", document.NormalizedRenderSource, StringComparison.OrdinalIgnoreCase);
```

- [ ] **Step 2: Verify RED** using the parser-test filter.

- [ ] **Step 3: Implement quantity parsing/removal**

Pinned rules:
- Zebra `q` valid range `1..99,999,999`, default 1;
- normalize `q=0` to 1 for expected Labelary/renderer compatibility;
- reject values above max and malformed `q`;
- last `^PQ` within a printable block controls metadata;
- remove all `^PQ...` commands from normalized render source without removing adjacent commands;
- never duplicate blocks or renders.

- [ ] **Step 4: Verify GREEN + full suite**.

- [ ] **Step 5: Commit**

```bash
git add src/SGPdf.App/Features/Labels/ZplDocumentParser.cs tests/SGPdf.App.Tests/ZplDocumentParserTests.cs
git commit -m "feat(labels): preserve ZPL print quantities as metadata"
```

---

### Task 3: Strict local file loader

**Files:** create `ZplFileLoader.cs` and `ZplFileLoaderTests.cs`.

**Interface:**

```csharp
public static class ZplFileLoader
{
    public static ZplDocument Load(string filePath);
}
```

- [ ] **Step 1: Write RED tests** using test-created temporary files only.

Cover:
- `.zpl`, `.txt`, `.prn`, case-insensitive;
- UTF-8 `Medellín Ñandú` preserved exactly in `OriginalSource`;
- UTF-8 BOM accepted;
- unsupported `.pdf` / `.bin` => `NotSupportedException`;
- missing file => `FileNotFoundException`;
- invalid strict UTF-8 bytes => controlled decoding exception, no document returned.

- [ ] **Step 2: Verify RED**

```powershell
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj -c Release --filter FullyQualifiedName~ZplFileLoaderTests
```

- [ ] **Step 3: Implement loader**

Rules:
- `Path.GetFullPath` once;
- only `.zpl/.txt/.prn`, ordinal-ignore-case;
- local strict UTF-8 read with BOM detection;
- no encoding guessing/network/temp copies/external process;
- delegate parsing to `ZplDocumentParser.Parse(fullPath, source)`.

- [ ] **Step 4: Verify loader tests + full suite**.

- [ ] **Step 5: Commit**

```bash
git add src/SGPdf.App/Features/Labels/ZplFileLoader.cs tests/SGPdf.App.Tests/ZplFileLoaderTests.cs
git commit -m "feat(labels): load local ZPL files"
```

---

### Task 4: WPF open-ZPL flow + safe state replacement

**Files:** modify `MainWindow.xaml` and `MainWindow.xaml.cs`.

**Consumes:** `ZplFileLoader.Load(...)`, `ZplDocument`.

- [ ] **Step 1: Add menu command**

Under `_Archivo`:

```xml
<MenuItem x:Name="OpenZplMenuItem"
          Header="Abrir _etiquetas ZPL..."
          Click="OpenZpl_Click" />
```

- [ ] **Step 2: Implement `OpenZpl_Click`**

Exact behavior:
- title `Abrir etiquetas ZPL`;
- filter `ZPL / TXT / PRN (*.zpl;*.txt;*.prn)|*.zpl;*.txt;*.prn|Todos los archivos (*.*)|*.*`;
- `CheckFileExists=true`, `Multiselect=false`;
- parse via `Task.Run(() => ZplFileLoader.Load(dialog.FileName))`;
- mutate no active document state until parse success;
- failure shows controlled Spanish error and preserves current PDF/ZPL;
- success cancels pending resize render, disposes prior PDF session, clears `_session/_navigation`, assigns private `_zplDocument`, clears/hides `PdfImage`, collapses PDF-only controls through `UpdateViewerControlsUi()`, and shows existing center empty state;
- placeholder text `Archivo ZPL cargado\n{N} diseño(s) detectado(s)`;
- status includes filename, design count and total file quantity;
- title `SG PDF Editor — <filename>`.

- [ ] **Step 3: Make successful PDF open clear `_zplDocument` only at PDF candidate commit point**

Invalid PDF must preserve a loaded ZPL exactly as invalid ZPL preserves a valid PDF.

- [ ] **Step 4: Extend existing lifecycle only**

- `SetBusy` disables both open commands;
- `OnClosed` clears `_zplDocument`;
- no view model/mode service/tabs/framework.

- [ ] **Step 5: Release verification**

```powershell
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx -c Release --no-restore
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj -c Release --no-build
```

Expected: all PASS, no package-lock changes.

- [ ] **Step 6: Manual Windows smoke when desktop is available**

Verify:
1. menu accepts `.zpl/.txt/.prn`;
2. valid synthetic multi-label file reports design count;
3. `^PQ28` reports one design and total quantity 28;
4. invalid ZPL leaves prior workspace unchanged;
5. valid ZPL after PDF hides PDF viewer controls/image;
6. valid PDF after ZPL replaces placeholder only after PDF candidate render succeeds.

If interactive Windows is unavailable, record **NOT RUN**, never PASS.

- [ ] **Step 7: Commit**

```bash
git add src/SGPdf.App/MainWindow.xaml src/SGPdf.App/MainWindow.xaml.cs
git commit -m "feat(labels): open ZPL files from WPF"
```

---

### Task 5: Sources of truth + exact-head CI

**Files:** create `.planning/phases/02-f1-zpl-gate/F2.1-PLAN.md`; create `docs/history/2026-10-07-F2.1.md` only after final verification; update `.planning/STATE.md`, `.planning/ROADMAP.md`, `docs/MASTER_CONTEXT.md`, `docs/MASTER_PLAN.md`.

- [ ] **Step 1: Update architecture/state docs**

Record:
- Labelize 1.7.0 is the approved F2 engine selection;
- production integration model is bundled local CLI sidecar beginning F2.2;
- BinaryKits is historical gate evidence, not runtime fallback;
- F1 synthetic gate is PASS for engine selection, but formal F1 closure still waits for private real Mercado Libre samples;
- F0 physical Windows smoke remains pending;
- F2.1 contains only managed parser/loader/UI open flow and no Labelize dependency.

Do not mark requirements requiring private real corpus or physical QA as accepted.

- [ ] **Step 2: Write compact F2.1 GSD phase plan** with scope, acceptance, RED/GREEN evidence and manual-smoke status.

- [ ] **Step 3: Run final exact-head verification**

```powershell
dotnet restore SGPdf.slnx --locked-mode
dotnet build SGPdf.slnx -c Release --no-restore
dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj -c Release --no-build
```

Require repository hygiene/offline guards green and zero new runtime packages.

- [ ] **Step 4: Create/update history after that run** with exact branch/head/run evidence, then rerun CI if the history commit changes head.

- [ ] **Step 5: Open draft stacked PR**

Implementation branch: `feat/f2-1-zpl-parse-open`, created from the approved design/plan head. Preferred PR base: `design/f2-labelize-architecture`. Include RED/GREEN/final exact-head CI evidence. Keep draft; no merge.

---

## Self-Review Result

- Spec coverage is limited intentionally to F2.1; Labelize execution/preview starts F2.2.
- Interfaces are consistent across tasks: `ZplDocument`, `ZplDesign`, `ZplDocumentParser.Parse`, `ZplFileLoader.Load`.
- `QuantityFromFile` is `int`; aggregate `TotalQuantityFromFile` is `long` to avoid artificial sum overflow.
- All five review-focus risks have explicit tests or controlled manual UI acceptance.
- No package or architecture expansion is hidden in the plan.
