# F13 Approved Mockup Shell Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** llevar SG PDF Editor desde el shell actual al mockup aprobado de Dashboard + Documento abierto, reutilizando todos los owners funcionales existentes y sin añadir arquitectura paralela.

**Architecture:** extender el WPF actual con un catálogo estático pequeño, un `HomeDashboard`, un host de estados Home/PDF/ZPL y un rail izquierdo de herramientas. La migración es aditiva hasta probar equivalencia con los owners actuales; solo después se elimina la navegación global redundante. El renderer PDF, writers, workspaces y feature owners no se reemplazan.

**Tech Stack:** C# / .NET 10 / WPF / recursos nativos actuales en `Themes/Modern.xaml` / `ThemeMode="System"`; sin framework UI nuevo ni paquete de iconos.

**Spec:** `docs/superpowers/specs/2026-10-15-f13-approved-mockup-shell-design.md`

**Visual reference:** el mockup aprobado Dashboard + Documento abierto; su identidad verificable queda registrada en `docs/design/2026-10-15-f13-approved-mockup-reference.md`.

## Global Constraints

- **IMPLEMENTATION BLOCKED UNTIL:** UI-4 deje de estar `FAIL-P1` y el rerun Windows no tenga P0/P1.
- **KISS:** `SIMPLE → EFICIENTE → OPTIMIZADO → EFICAZ → A→B`.
- **RUIDO:** `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
- **ANTI-PARCHE:** `REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.
- `main` continúa read-only; no merge sin aprobación explícita del usuario.
- Reutilizar `RecentPdfStore`, `PdfScrollViewer`, `PdfImage`, file-open flows, current feature handlers/workspaces and current transactional writers.
- No segundo motor PDF, segundo writer, backend, cloud obligatorio, IA, login, advertising, plugin architecture, event bus, generic navigation framework, DI rewrite or UI framework migration.
- No new production package for F13.
- No removal of classic menu or centered legacy mode navigation until replacement equivalence is proven.
- F13 does not implement Edit V3; it creates the shell in which F15 will live.
- F13 does not claim capabilities not yet implemented.
- `PDF a PNG` is real in F13; JPG remains F16.
- `Imagen a PDF` may appear only as disabled `Próximamente` until F16.
- Image watermark must not be claimed in F13; the current watermark capability remains described according to what the owner really supports.
- The red `SG` mockup tile is not the official application icon.
- Codex is used only for Windows GUI/manual evidence or defects that materially require the user's PC.

## Review Focus

1. **Failed/cancelled file open:** the active PDF/ZPL workspace must survive; Task 4 owns this regression test.
2. **Dirty mode transitions:** rail navigation must invoke the same existing Edit/Sign/Organize/Comment guards; Task 5 owns equivalence tests.
3. **Small/scaled screens:** 1366×768 and a real scaled Windows profile must expose primary Home/document actions without horizontal shell clipping; Tasks 3 and 9 own this evidence.
4. **Theme states:** selected/hover/disabled must remain legible in Light/Dark without light-only hard-coded colors; Tasks 1 and 9 own this.
5. **Capability honesty:** future/non-implemented cards must never route as if functional; Task 2 owns catalog tests and Task 3 owns disabled-card behavior.

---

## File / Responsibility Map

### New product-shell files
- `src/SGPdf.App/Shell/ToolDescriptor.cs` — immutable description of a visible tool; no behavior.
- `src/SGPdf.App/Shell/ToolCatalog.cs` — deterministic static lists for Home and document rail.
- `src/SGPdf.App/Shell/HomeDashboard.xaml` — Home visual surface.
- `src/SGPdf.App/Shell/HomeDashboard.xaml.cs` — Home UI events only; no PDF business logic.
- `src/SGPdf.App/MainWindow.Shell.cs` — shell state, tool routing, Home/PDF/ZPL visibility.

### Existing files to extend
- `src/SGPdf.App/Themes/Modern.xaml` — SG visual primitives, icons and shell styles.
- `src/SGPdf.App/MainWindow.xaml` — shell slots, rail host, contextual toolbar/right frame.
- `src/SGPdf.App/MainWindow.xaml.cs` — only minimal initialization if `MainWindow.Shell.cs` cannot own it cleanly.
- Feature partials such as `MainWindow.Sign.cs`, `MainWindow.Edit.cs`, Organize/Comments partials — only to expose/reuse existing entry paths or remove proven redundant UI; business logic stays where it is.
- `src/SGPdf.App/SGPdf.App.csproj` — official icon wiring only after approved source exists.

### New tests
- `tests/SGPdf.App.Tests/ShellThemeContractTests.cs`
- `tests/SGPdf.App.Tests/ToolCatalogTests.cs`
- `tests/SGPdf.App.Tests/HomeDashboardContractTests.cs`
- `tests/SGPdf.App.Tests/MainWindowShellStateTests.cs`
- `tests/SGPdf.App.Tests/MainWindowToolRailTests.cs`
- `tests/SGPdf.App.Tests/MainWindowContextShellTests.cs`

Do not move unrelated tests merely to reorganize names.

---

## Gate 0 — Resolve the current UI-4 blocker before F13 code

**Files:** none in F13.

**Consumes:** current Windows diagnostic covering V10–V12, Edit/Save, V08 and V09.

**Produces:** accepted UI head with no P0/P1 and remaining V04/V05/V08/V09 evidence classified.

- [ ] Audit the returned Windows P1 diagnostic before accepting any fix.
- [ ] Resolve only demonstrated root causes under the anti-patch rule.
- [ ] Run focused cause tests + full Release suite.
- [ ] Obtain exact-head CI GREEN.
- [ ] Rerun real Windows UI-4 evidence including V04, V05 and all formerly blocked P1 paths.
- [ ] Update UI-4 release evidence/state only after no P0/P1 remain.

**Stop condition:** if UI-4 remains FAIL-P1, do not start Task 1 product code.

---

### Task 1: SG shell visual primitives and icon geometry

**Files:**
- Modify: `src/SGPdf.App/Themes/Modern.xaml`
- Create: `tests/SGPdf.App.Tests/ShellThemeContractTests.cs`

**Interfaces:**
- Produces resource keys consumed by Tasks 3–6.
- Consumes only current SG theme resources; no feature owner.

**Required resource roles:** Home/card container; Home/document rail item; selected rail state; primary/secondary shell button; tool icon tile/host; contextual toolbar container/button; context panel section/card; shell title/subtitle/muted text; local vector `Geometry` resources for the first visible tool set.

- [ ] **Step 1: write `ShellThemeContractTests` RED tests.** Assert all new shell resource keys exist; rail/card/context resources use SG theme resources; selected/hover/disabled do not introduce light-only literals; no Adobe/iLovePDF assets; no third-party icon package.
- [ ] **Step 2: run focused RED.** `dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj -c Release --filter FullyQualifiedName~ShellThemeContractTests` → expected FAIL.
- [ ] **Step 3: add minimal reusable resources to `Modern.xaml`.** Use spacing 4/8/12/16/24/32; card/panel radius 8–10; button radius 6–8; `DynamicResource` where runtime theme lookup is needed; Segoe UI; only needed vector geometries.
- [ ] **Step 4: focused tests PASS.**
- [ ] **Step 5: full Release suite PASS.**
- [ ] **Step 6: commit checkpoint:** `feat(shell): add approved mockup visual primitives`.

---

### Task 2: Static, capability-honest tool catalog

**Files:**
- Create: `src/SGPdf.App/Shell/ToolDescriptor.cs`
- Create: `src/SGPdf.App/Shell/ToolCatalog.cs`
- Create: `tests/SGPdf.App.Tests/ToolCatalogTests.cs`

**Interfaces:**

```csharp
internal sealed record ToolDescriptor(
    string Id,
    string Title,
    string Category,
    string Description,
    string IconKey,
    string ActionId,
    bool RequiresPdf,
    bool IsImplemented);
```

```csharp
internal static IReadOnlyList<ToolDescriptor> ToolCatalog.Home { get; }
internal static IReadOnlyList<ToolDescriptor> ToolCatalog.Document { get; }
```

No delegates, reflection, dynamic discovery or `MainWindow` references.

**Document order:** `read, edit, comment, sign, organize, forms, ocr, redact, compare, convert, protect`.

**Home baseline real cards:** Merge PDF, Split PDF, Organize pages, Edit PDF, OCR, Redact, Protect, Sign, Compare, PDF to PNG, QR, ZPL labels, current text watermark, page numbering. `Image to PDF` only as `IsImplemented=false` + `Próximamente`.

- [ ] **Step 1: write RED tests:** `Home_HasUniqueIdsAndActionIds`, `Document_HasUniqueIdsAndExpectedOrder`, `ImplementedTools_HaveTitleDescriptionAndIconKey`, `Catalog_HasNoCloudAiLoginOrAdvertisingAction`, `PdfToImage_DoesNotClaimJpgBeforeF16`, `ImageToPdf_IsNotExecutableBeforeF16`, `FutureTool_HasNoExecutableActionPath`.
- [ ] **Step 2: focused RED.** `dotnet test ... --filter FullyQualifiedName~ToolCatalogTests` → expected FAIL.
- [ ] **Step 3: implement immutable static catalog.**
- [ ] **Step 4: focused PASS.**
- [ ] **Step 5: full Release suite PASS.**
- [ ] **Step 6: checkpoint:** `feat(shell): add capability-honest tool catalog`.

---

### Task 3: Home Dashboard matching the approved mockup

**Files:**
- Create: `src/SGPdf.App/Shell/HomeDashboard.xaml`
- Create: `src/SGPdf.App/Shell/HomeDashboard.xaml.cs`
- Create: `tests/SGPdf.App.Tests/HomeDashboardContractTests.cs`
- Modify `Modern.xaml` only if Task 1 missed a truly reusable primitive.

**Interfaces:**

```csharp
internal IReadOnlyList<ToolDescriptor> Tools { get; set; }
internal event EventHandler? OpenPdfRequested;
internal event EventHandler? OpenZplRequested;
internal event EventHandler<string>? RecentPdfRequested;
internal event EventHandler<string>? ToolRequested;
```

If existing target conventions make `EventHandler<string>` undesirable, use one tiny local event-args type; no messaging framework. Home never opens files itself.

**Visual contract:** 96 px Home rail; 24 px content padding; welcome/local-offline hero; primary `Abrir PDF`; secondary `Abrir ZPL`; responsive cards; Recents host; Settings at rail bottom; no login/cloud/AI/ads.

- [ ] **Step 1: write RED Home structure tests.** Assert Home navigation affordances; primary/secondary open actions; one catalog-driven card host; future card disabled + `Próximamente`; recent host; no horizontal card scrolling; no login/ad/cloud/AI strings.
- [ ] **Step 2: focused RED.**
- [ ] **Step 3: implement `HomeDashboard` with responsive `ItemsControl` + wrapping panel.**
- [ ] **Step 4: consume Task 1 resources; no local duplicate style blocks.**
- [ ] **Step 5: focused PASS.**
- [ ] **Step 6: full Release suite PASS.**
- [ ] **Step 7: checkpoint:** `feat(shell): add approved local dashboard`.

Windows visual evidence is deferred to Task 9.

---

### Task 4: MainWindow shell state — Home / PDF / ZPL

**Files:**
- Create: `src/SGPdf.App/MainWindow.Shell.cs`
- Modify: `src/SGPdf.App/MainWindow.xaml`
- Modify: `src/SGPdf.App/MainWindow.xaml.cs` only for minimal shell init if needed.
- Modify: `src/SGPdf.App/MainWindow.ReaderShortcuts.cs` only if a tiny existing recent-open seam must be reused.
- Create: `tests/SGPdf.App.Tests/MainWindowShellStateTests.cs`

**Interfaces:**

A minimal owner-local state, e.g.:
```csharp
private enum ShellWorkspaceKind { Home, Pdf, Zpl }
private void RefreshShellWorkspace();
private void ShowHomeWorkspace();
private void ShowPdfWorkspace();
private void ShowZplWorkspace();
```

Exact names may follow existing MainWindow conventions, but one owner only; no navigation service.

Host routes to existing open flows. Recents preserve existing `TryOpenPdfPathAsync` semantics.

- [ ] **Step 1: write RED shell-state tests.** Startup/no document → Home; active PDF → PDF; active ZPL → ZPL; failed/cancelled PDF open preserves prior workspace; missing recent preserves current document and uses existing cleanup; PDF-only actions unavailable where appropriate.
- [ ] **Step 2: focused RED.**
- [ ] **Step 3: insert `HomeDashboard` without deleting existing PDF/ZPL UI.**
- [ ] **Step 4: implement minimal state methods in `MainWindow.Shell.cs`.**
- [ ] **Step 5: wire Home events to existing owners.**
- [ ] **Step 6: focused PASS.**
- [ ] **Step 7: full Release suite PASS.**
- [ ] **Step 8: exact-head CI GREEN before continuing.**
- [ ] **Step 9: checkpoint:** `feat(shell): host home pdf and zpl workspaces`.

---

### Task 5: Additive document rail with exact owner routing

**Files:**
- Modify: `src/SGPdf.App/MainWindow.xaml`
- Modify: `src/SGPdf.App/MainWindow.Shell.cs`
- Modify feature partials only if a tiny safe reusable entry method is required.
- Create: `tests/SGPdf.App.Tests/MainWindowToolRailTests.cs`

**Interface:** one local router `private void HandleToolAction(string actionId);` or async only if required by the real owner. Explicit switch only; no registration framework.

**Owner map:** read→`ReadMode_Click`; edit→`EditMode_Click`/`TryEnterEditModeAsync`; comment→`TryEnterCommentMode()`; sign→`SignMode_Click`; organize→`TryEnterOrganizeMode()`; forms→`EditAcroForm_Click`; ocr→`Ocr_Click`; redact→existing Edit/Redaction entry; compare→`ComparePdf_Click`; convert→`ExportPdfToPng_Click` baseline; protect→`Protection_Click`.

- [ ] **Step 1: RED rail structure tests.** 96 px target; catalog order; one selected tool; accessible visible text; selected not color-only; old centered strip remains during this task.
- [ ] **Step 2: RED routing/equivalence tests.** Edit/Sign/Read/Organize/Comment hit same owners/guards; future action non-executable; PDF-required disabled with no PDF.
- [ ] **Step 3: add rail additively.** Keep 240 px context area separate where needed; center flexible; 320 px right pane retained.
- [ ] **Step 4: implement explicit router.**
- [ ] **Step 5: focused PASS.**
- [ ] **Step 6: full Release suite PASS.**
- [ ] **Step 7: exact-head CI GREEN.**
- [ ] **Step 8: checkpoint:** `feat(shell): route approved document tool rail`.

---

### Task 6: Context toolbar and right contextual frame

**Files:**
- Modify: `src/SGPdf.App/MainWindow.xaml`
- Modify: `src/SGPdf.App/MainWindow.Shell.cs`
- Modify: `src/SGPdf.App/Themes/Modern.xaml` only for missing reusable frame styles.
- Create: `tests/SGPdf.App.Tests/MainWindowContextShellTests.cs`

**Interfaces:** F13 owns the frame, not F15 business behavior. Local helpers may be `RefreshContextToolbar()` / `RefreshContextPanelFrame()`; no universal command model.

**Baseline:** contextual toolbar distinct from global navigation; owner decides available actions; Edit surfaces Seleccionar/Undo/Redo/Guardar como only to current support; Undo/Redo never pretend text/redaction history exists; Save As routes to real owner; right pane ~320 px and scrollable; no selection → concise placeholder.

- [ ] **Step 1: RED context-shell tests.** Toolbar only in document workspace; Edit save visible in Edit; no-dirty state cannot false-save; Undo/Redo disabled when unsupported; right panel scrollable; mode switch clears prior contextual controls.
- [ ] **Step 2: focused RED.**
- [ ] **Step 3: add common toolbar/panel frame.**
- [ ] **Step 4: wire only real capability state.**
- [ ] **Step 5: focused PASS.**
- [ ] **Step 6: full Release suite PASS.**
- [ ] **Step 7: checkpoint:** `feat(shell): add contextual document command frame`.

---

### Task 7: Prove equivalence, then delete redundant centered navigation

**Files:**
- Modify: `src/SGPdf.App/MainWindow.Sign.cs`
- Modify: `src/SGPdf.App/MainWindow.Edit.cs`
- Modify Organize/Comment partials only if they inject controls into the same legacy strip.
- Extend: `MainWindowToolRailTests.cs`, `MainWindowContextShellTests.cs`.

**Interfaces:** business owners remain. Deletion only targets redundant global navigation/wiring; contextual commands such as Edit Save As are relocated/preserved.

- [ ] **Step 1: characterize every legacy strip child/wiring** as global navigation, contextual command, or status/guard owner.
- [ ] **Step 2: add equivalence tests for each global mode being replaced.**
- [ ] **Step 3: run tests with old + new navigation present — PASS.**
- [ ] **Step 4: request one bounded Codex Windows smoke** for Read/Edit/Sign/Organize/Comment + one dirty guard + Light/Dark selected state. No redesign or broad implementation.
- [ ] **Step 5: audit Windows result.**
- [ ] **Step 6: delete only proven redundant centered global mode controls.**
- [ ] **Step 7: focused PASS.**
- [ ] **Step 8: full Release suite PASS.**
- [ ] **Step 9: exact-head CI GREEN.**
- [ ] **Step 10: checkpoint:** `refactor(shell): remove proven redundant mode navigation`.

Classic Menu may remain through F14.

---

### Task 8: Official application icon — source-gated

**BLOCKER:** actual user-approved icon/logo source image must be available.

**Files, only after approval:** binary app asset; `src/SGPdf.App/SGPdf.App.csproj`; `MainWindow.xaml` only if explicit window icon assignment is necessary; one metadata/build contract if useful.

- [ ] **Step 1: verify approved source availability/provenance.** If absent: `BLOCKED — approved icon source unavailable`; stop. Mockup red `SG` is not a source.
- [ ] **Step 2: create multi-resolution `.ico` from approved source only.**
- [ ] **Step 3: wire application/project icon.**
- [ ] **Step 4: Release build + full suite.**
- [ ] **Step 5: bounded Windows verify `.exe`, window/title where rendered, Alt+Tab, taskbar.**
- [ ] **Step 6: checkpoint:** `feat(shell): add official sg pdf application icon`.

Installer later reuses the same identity.

---

### Task 9: F13 final visual QA against approved mockup and closure

**Files:** update `.planning/STATE.md`, `.planning/ROADMAP.md`; create concise closure evidence only for facts not recoverable from commits/CI.

**Automated prerequisite:** full Release suite PASS; exact-head CI PASS; no automated P0/P1.

**Real Windows evidence — Codex only:** Dashboard large; 1366×768 @100%; one scaled profile; PDF open; Edit active; Light; Dark; Tab/Shift+Tab + focus; disabled states; right panel scrolling; no primary horizontal clipping; hierarchy comparison to mockup.

Acceptance is hierarchy/behavioral visual equivalence, not brittle pixel-perfect matching.

- [ ] **Step 1: run full Release suite and record total.**
- [ ] **Step 2: exact-head CI GREEN and record run/job.**
- [ ] **Step 3: run bounded Windows F13 matrix.**
- [ ] **Step 4: classify P0/P1/P2.**
- [ ] **Step 5: P0/P1 → anti-patch flow before rerun.**
- [ ] **Step 6: confirm centered navigation gone and owners single.**
- [ ] **Step 7: confirm icon GREEN or explicitly source-blocked; final release cannot silently omit it.**
- [ ] **Step 8: update `STATE.md` + `ROADMAP.md`.**
- [ ] **Step 9: record F15 Edit V3 as next UX implementation inside completed shell.**

Suggested closure commit: `docs(state): close F13 approved product shell`.

---

## Self-review

### Spec coverage
- Home/dashboard: Tasks 1–4.
- 96 px rails, cards, spacing/theme/icon language: Tasks 1, 3, 5.
- Capability honesty: Tasks 2–3.
- Home/PDF/ZPL state: Task 4.
- document rail + owner routing: Task 5.
- context toolbar/right panel: Task 6.
- duplicate legacy navigation removal: Task 7.
- official app icon: Task 8.
- Light/Dark, 1366×768, scaling, keyboard and mockup comparison: Task 9.
- F15 isolation: Global Constraints + Tasks 6/9.

### Step scan
Every implementation task follows RED → minimal change → focused PASS → full suite → checkpoint. Real Windows work is limited to places automation cannot establish GUI equivalence.

### Type consistency
`ToolDescriptor` and `ToolCatalog.Home/Document` are defined once; `HomeDashboard` emits intent only; MainWindow remains owner; `HandleToolAction(string actionId)` is the only new routing entry. No universal command/history/navigation abstraction.

### Review Focus coverage
All five risks map to concrete tests/evidence.

### Proportion / YAGNI
Excluded: Edit V3 internals, Compare V2, new converters, forms designer, publish/installer, multi-document tabs, generic navigation infrastructure.

## Execution Method

Use **ChatGPT/native implementation** task-by-task after Gate 0 becomes GREEN.

Use **Codex only** for Windows interaction/equivalence in Task 7, final Windows visual QA in Task 9, icon Windows verification in Task 8, or a defect demonstrated to reproduce only on the user's PC. No broad Codex implementation handoff is planned for F13.
