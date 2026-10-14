# Release UI Modernization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Modernize SG PDF Editor's WPF visual shell without changing F0–F12 behavior or adding a runtime UI framework.

**Architecture:** Use native WPF Fluent theme in .NET 10 plus one app-level resource dictionary for SG PDF tokens and reusable control variants. Apply it incrementally, preserving existing `x:Name`, event handlers, PDF engines, sidecars, persistence, and functional workflow.

**Tech Stack:** C# / WPF / .NET 10 / XAML / xUnit.

**Spec:** `docs/release/2026-10-14-ui-modernization.md`

## Global Constraints

- F0–F12 behavior remains frozen.
- `main` remains read-only.
- No new NuGet UI package or second desktop framework.
- UI UX Pro Max is reference guidance only, not runtime code.
- Native WPF `ThemeMode="System"` is the Fluent base.
- Existing names and handlers remain stable unless a failing behavior test proves a change is required.
- Windows screenshot/manual QA is required before final visual PASS.

## Review Focus

- Dark/light system theme: controls and custom surfaces must not become unreadable.
- Keyboard focus: primary interactive controls must retain visible focus cues.
- Disabled state: toolbar actions must remain visibly and semantically disabled.
- 1366×768/scaling: toolbars and side panels must not clip critical actions.
- Existing event wiring: visual edits must not rename or detach functional controls.

---

### Task 1: UI foundation contract

**Files:**
- Create: `tests/SGPdf.App.Tests/UiThemeContractTests.cs`
- Create: `src/SGPdf.App/Themes/Modern.xaml`
- Modify: `src/SGPdf.App/App.xaml`

**Interfaces:**
- Consumes: WPF application resources.
- Produces: `ThemeMode="System"`, `Themes/Modern.xaml`, shared brushes and named style variants.

- [ ] **Step 1: Write the failing contract tests**

Test names:
- `AppXaml_UsesSystemFluentTheme`
- `AppXaml_MergesModernThemeDictionary`
- `ModernTheme_DefinesRequiredVisualResources`

Assertions:
- `App.xaml` root has `ThemeMode="System"`.
- `App.xaml` merges `Themes/Modern.xaml`.
- Modern theme includes keys `SgSurfaceBrush`, `SgPanelBrush`, `SgCanvasBrush`, `SgBorderBrush`, `SgAccentBrush`, `SgPrimaryButtonStyle`, `SgToolbarButtonStyle`, `SgIconButtonStyle`, and `SgSectionHeaderStyle`.

- [ ] **Step 2: Run the focused test and verify RED**

Run: `dotnet test SGPdf.slnx --configuration Release --filter UiThemeContractTests`
Expected: FAIL because ThemeMode/dictionary/resources do not yet exist.

- [ ] **Step 3: Implement the minimal theme foundation**

Use `ThemeMode="System"` on `Application`; merge one local `Themes/Modern.xaml`; define neutral tokens/styles using native controls and system resources where practical. Do not replace native Fluent dictionaries manually.

- [ ] **Step 4: Verify focused GREEN and full suite**

Run focused test, then locked restore/build/full tests.
Expected: all PASS.

- [ ] **Step 5: Commit**

Commit message: `feat(ui): add Fluent visual foundation`

### Task 2: Main shell visual hierarchy

**Files:**
- Modify: `src/SGPdf.App/MainWindow.xaml`
- Test: `tests/SGPdf.App.Tests/UiThemeContractTests.cs`

**Interfaces:**
- Consumes: Task 1 theme resources.
- Produces: modern shell using existing control names and handlers.

- [ ] **Step 1: Add RED structural assertions**

Assert the main shell consumes shared surface/canvas/panel styles and the primary empty-state action uses a shared button style while preserving required existing control names.

- [ ] **Step 2: Verify RED**

Run focused UI contract tests; expected failure before XAML restyle.

- [ ] **Step 3: Restyle only shell/chrome**

Modernize menu/command strip, navigation/zoom grouping, left page region, center document canvas, right properties surface, empty state, and status bar. Preserve behavior and event handlers.

- [ ] **Step 4: Verify focused GREEN + full suite**

Expected: PASS.

- [ ] **Step 5: Commit**

Commit message: `feat(ui): modernize main editor shell`

### Task 3: Dialog consistency

**Files:**
- Modify: `src/SGPdf.App/PdfPasswordDialog.xaml`
- Modify: `src/SGPdf.App/SignatureDrawDialog.xaml`
- Modify: `src/SGPdf.App/SignatureLibraryDialog.xaml`
- Modify: `src/SGPdf.App/SignatureNameDialog.xaml`
- Modify: `src/SGPdf.App/SignaturePhotoDialog.xaml`
- Test: `tests/SGPdf.App.Tests/UiThemeContractTests.cs`

**Interfaces:**
- Consumes: shared theme resources.
- Produces: coherent dialog surfaces and button hierarchy with unchanged behavior.

- [ ] **Step 1: Add RED assertions for shared style usage**
- [ ] **Step 2: Verify RED**
- [ ] **Step 3: Apply shared styles without changing handlers**
- [ ] **Step 4: Verify focused GREEN + full suite**
- [ ] **Step 5: Commit `feat(ui): align editor dialogs with Fluent shell`**

### Task 4: Windows visual QA handoff

**Files:**
- Update: `docs/release/2026-10-14-ui-modernization.md`
- Update: `.planning/STATE.md`

**Interfaces:**
- Consumes: CI-green UI branch and Windows environment prepared by Codex.
- Produces: screenshot/manual QA gate; no automatic visual PASS.

- [ ] **Step 1: Verify exact-head CI**
- [ ] **Step 2: Run app on Windows at 1366×768+ and system scaling**
- [ ] **Step 3: Capture main/loaded PDF/labels/signature/password screenshots**
- [ ] **Step 4: Record PASS/FAIL findings; fix P0/P1 visual usability defects only**
- [ ] **Step 5: Update STATE; portable publish remains after visual gate**
