# F13 Product Shell / Dashboard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** replace SG PDF's fragmented discoverability with one local Home/dashboard plus one left-side document tool rail while reusing all existing feature owners and preserving current PDF/ZPL behavior.

**Architecture:** add a tiny static tool catalog and one Home surface, then introduce a left navigation rail that routes into existing MainWindow feature handlers/workspaces. Keep the classic menu and legacy mode strip temporarily as fallback/equivalence evidence; delete the redundant strip only after automated + real Windows equivalence is proven.

**Tech Stack:** C# / .NET 10 / WPF / current native Fluent resources via `ThemeMode="System"`; no new UI framework or package.

**Spec:** `docs/superpowers/specs/2026-10-14-f13-product-shell-design.md`

## Global Constraints

- **BLOCKED UNTIL:** current UI-4 FAIL-P1 is resolved and rerun has no P0/P1.
- KISS: simple, efficient, optimized, effective; A→B.
- Anti-noise: `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
- Anti-patch: `REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.
- Reuse current `RecentPdfStore`, file-open methods, feature handlers and workspaces.
- No generalized plugin registry, event bus, mode framework, navigation framework or dependency-injection rewrite.
- No new production package.
- No removal of classic menu or legacy mode strip until replacement equivalence is proven.
- No `main` merge without explicit user approval.

## Review Focus

1. Opening a recent file that no longer exists must keep current cleanup behavior and never corrupt the active document.
2. Home must not show while a PDF/ZPL workspace is active, and document panels must not remain interactive over Home.
3. Switching tools with dirty Edit/Sign/Organize state must invoke existing guards, not bypass them through a new button.
4. 1366×768 and scaled Windows profiles must keep primary actions accessible without horizontal shell clipping.
5. Light/dark selected/hover/disabled tool states must use theme resources, never hard-coded light colors.

---

### Task 1: Static tool catalog

**Files:**
- Create: `src/SGPdf.App/Shell/ToolDescriptor.cs`
- Create: `src/SGPdf.App/Shell/ToolCatalog.cs`
- Test: `tests/SGPdf.App.Tests/ToolCatalogTests.cs`

**Interfaces:**
- Produces: `ToolDescriptor` immutable value describing `Id`, `Title`, `Category`, `Description`, `IconKey`, `ActionId`, `RequiresPdf` and `IsImplemented`.
- Produces: `ToolCatalog.All` deterministic read-only list.
- Consumes: no MainWindow instance and no feature service.

- [ ] **Step 1: write failing catalog contract tests**

Test names/assertions:
- `All_HasUniqueIdsAndActionIds` — no duplicate non-empty ids/action ids.
- `All_HasDeterministicPrimaryOrder` — first primary sequence is Read, Edit, Comment, Sign, Organize, Forms, OCR, Redact, Compare, Convert, Protect, More.
- `All_ImplementedCoreToolsHaveIconAndDescription` — implemented entries have non-empty `IconKey` and concise description.
- `All_DoesNotExposeCloudOrAiBaselineTools` — no account/cloud/AI request action ids.

- [ ] **Step 2: run focused RED**

Run:
`dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj -c Release --filter FullyQualifiedName~ToolCatalogTests`

Expected: FAIL because shell catalog types do not exist.

- [ ] **Step 3: implement minimal immutable catalog**

No dynamic discovery/reflection/plugins. Use a static readonly list only.

- [ ] **Step 4: rerun focused tests — PASS**

- [ ] **Step 5: run full suite**

Expected: existing suite + new catalog tests, 0 failed.

- [ ] **Step 6: commit checkpoint**

Suggested commit:
`feat(shell): add static tool catalog`

---

### Task 2: Home dashboard surface

**Files:**
- Create: `src/SGPdf.App/Shell/HomeDashboard.xaml`
- Create: `src/SGPdf.App/Shell/HomeDashboard.xaml.cs`
- Modify: `src/SGPdf.App/MainWindow.xaml`
- Modify: `src/SGPdf.App/MainWindow.xaml.cs` only for shell visibility/routing if needed.
- Modify: `src/SGPdf.App/MainWindow.ReaderShortcuts.cs` only if a small reusable recent-open method is required; do not duplicate store logic.
- Modify: `src/SGPdf.App/Themes/Modern.xaml` only for reusable Home/tool-card resources.
- Test: `tests/SGPdf.App.Tests/HomeDashboardContractTests.cs`

**Interfaces:**
- `HomeDashboard.Tools` consumes `ToolCatalog.All`.
- Events/actions exposed to host: `OpenPdfRequested`, `OpenZplRequested`, `RecentPdfRequested(path)`, `ToolRequested(actionId)`.
- Host remains owner of file dialogs and feature methods.

- [ ] **Step 1: write RED Home structure tests**

Assertions:
- control contains a primary `Abrir PDF` action;
- contains `Abrir ZPL` secondary action;
- tool cards are generated from one catalog source, not duplicated XAML literals;
- Home has a recent-items host;
- Home has no login/ad/cloud/AI controls;
- primary buttons/styles use existing SG resources via dynamic lookup where test construction requires it.

- [ ] **Step 2: run focused RED**

Expected: missing control/resources.

- [ ] **Step 3: implement Home layout**

Design constraints:
- native WPF;
- cards wrap vertically/horizontally using a wrapping panel or ItemsControl template;
- no horizontal scrolling required for primary Home content at supported minimum window size;
- actual recent entries come from existing `RecentPdfStore` through MainWindow host;
- recent click routes through existing `TryOpenPdfPathAsync` semantics.

- [ ] **Step 4: integrate shell visibility**

Required state contract:
- no active PDF/ZPL → Home visible;
- PDF active → Home collapsed, PDF workspace visible;
- ZPL active → Home collapsed, ZPL workspace visible;
- failed/cancelled open does not destroy current active workspace.

- [ ] **Step 5: focused tests — PASS**

- [ ] **Step 6: full suite — PASS**

- [ ] **Step 7: exact-head CI — GREEN**

- [ ] **Step 8: Windows visual smoke**

Check:
- empty Home light/dark;
- recent list with 0 and multiple items;
- 1366×768 @100%;
- one scaled profile;
- keyboard focus/Enter/Space;
- open PDF and return to Home only after document truly closes/resets.

- [ ] **Step 9: checkpoint**

Suggested commit:
`feat(shell): add local home dashboard`

---

### Task 3: Reusable SG tool-button visual language

**Files:**
- Modify: `src/SGPdf.App/Themes/Modern.xaml`
- Test: `tests/SGPdf.App.Tests/ModernThemeContractTests.cs` or extend the existing UI modernization contract test file if one already owns theme contracts.

**Interfaces:**
- Produces lightweight reusable styles/resources for:
  - Home tool card;
  - left rail tool button;
  - selected tool state;
  - icon host.

- [ ] **Step 1: RED theme contract**

Assert required resource keys exist and do not hard-code light-only foreground/background colors for selected/hover/disabled states.

- [ ] **Step 2: implement minimal resource additions**

Icon strategy:
- vector `Geometry` resources inside existing `Modern.xaml` where practical;
- no icon package;
- do not copy third-party proprietary assets.

- [ ] **Step 3: focused + full suite**

- [ ] **Step 4: checkpoint**

Suggested commit:
`feat(shell): add reusable tool navigation styles`

---

### Task 4: Left tool rail — additive/equivalence stage

**Files:**
- Modify: `src/SGPdf.App/MainWindow.xaml`
- Create: `src/SGPdf.App/MainWindow.Shell.cs` if routing/state would otherwise bloat an unrelated feature partial.
- Modify only the specific feature partials required to expose/reuse an existing safe entry method; prefer calling current methods rather than moving them.
- Test: `tests/SGPdf.App.Tests/MainWindowToolRailTests.cs`

**Interfaces:**
- Tool rail obtains entries from `ToolCatalog.All`.
- `HandleToolAction(string actionId)` resolves only known action ids to existing feature-owner methods.
- No generic command bus/event aggregator.

- [ ] **Step 1: RED rail structure tests**

Assertions:
- left primary rail exists in PDF workspace;
- visible items follow catalog order;
- selected state has exactly one global primary tool where appropriate;
- legacy centered mode strip still exists during this task;
- tool rail does not create duplicate Edit/Sign workspaces.

- [ ] **Step 2: RED guard-equivalence tests**

Use existing test seams/delegates where available to prove:
- Rail `Edit` invokes the same Edit entry path;
- Rail `Sign` invokes the same Sign entry path;
- Rail `Read` respects pending-signature/edit guards;
- unavailable PDF-only tools are disabled when no PDF is open.

- [ ] **Step 3: implement additive rail**

Layout target:
- compact fixed rail ≈ 96 px;
- existing 240 px pages/context column remains adjacent where relevant;
- center canvas remains flexible;
- existing 320 px properties panel remains on right.

- [ ] **Step 4: focused tests — PASS**

- [ ] **Step 5: full suite + exact-head CI — GREEN**

- [ ] **Step 6: real Windows equivalence smoke**

At minimum: Read/Edit/Sign/Organize/Comment with clean state + one dirty-state guard path.

- [ ] **Step 7: checkpoint**

Suggested commit:
`feat(shell): add left document tool rail`

---

### Task 5: Remove redundant centered mode strip after equivalence proof

**Files:**
- Modify: `src/SGPdf.App/MainWindow.Sign.cs`
- Modify: `src/SGPdf.App/MainWindow.Edit.cs`
- Modify organize/comment partial only if their buttons are part of the same dynamic strip lifecycle.
- Modify: tests that explicitly assert legacy mode-strip existence; replace them with rail owner/equivalence contracts.

**Interfaces:**
- Existing feature methods/workspaces remain unchanged owners.
- Deletion target is only redundant global navigation UI/wiring, not functional state/guards.

- [ ] **Step 1: characterize all children/wiring currently injected into the mode panel**

List Read/Sign/Edit/Organize/other current mode buttons and Save As controls that share the panel. Classify each as:
- global navigation → move/delete after rail equivalent;
- contextual command (e.g. Edit Save As) → relocate to top/context command area, not delete.

- [ ] **Step 2: write tests for the replacement locations/owners**

- [ ] **Step 3: remove only proven redundant mode navigation**

Do not delete save commands or lifecycle guard code.

- [ ] **Step 4: full suite + exact-head CI**

- [ ] **Step 5: Windows regression pass**

Required: light/dark, keyboard, disabled states, Edit/Save, Sign, Organize, loaded PDF.

- [ ] **Step 6: checkpoint**

Suggested commit:
`refactor(shell): remove redundant mode strip`

---

### Task 6: Official application icon

**BLOCKER:** actual user-approved source image must be available.

**Files:**
- Create binary asset under the project's existing assets/resources convention after source approval.
- Modify: `src/SGPdf.App/SGPdf.App.csproj` for `ApplicationIcon` or equivalent approved MSBuild wiring.
- Modify: `src/SGPdf.App/MainWindow.xaml` only if explicit window icon assignment is required.
- Test: add metadata/build contract where practical; final acceptance is Windows visual/runtime evidence.

- [ ] **Step 1: verify source asset provenance**

If source is absent: STOP this task and record `BLOCKED — approved source unavailable`.

- [ ] **Step 2: generate multi-resolution `.ico` from approved source only**

No redesign, AI recreation or text-description substitute.

- [ ] **Step 3: wire project/window icon**

- [ ] **Step 4: Release build + full suite**

- [ ] **Step 5: Windows verify**

Check:
- executable icon;
- title/window icon where Windows renders it;
- Alt+Tab;
- taskbar.

Shortcut/installer verification belongs to packaging later but must reuse the same binary identity.

- [ ] **Step 6: checkpoint**

Suggested commit:
`feat(shell): add official application icon`

---

### Task 7: F13 closure

**Files:**
- Update: `.planning/STATE.md`
- Update: `.planning/ROADMAP.md`
- Create closure evidence only if it records durable final facts rather than duplicating commits.

- [ ] Confirm no open F13 P0/P1.
- [ ] Confirm full exact-head suite totals and CI run/job.
- [ ] Confirm real Windows light/dark + keyboard + 1366×768 + scaled profile.
- [ ] Confirm current mode handlers/guards are still single-owner.
- [ ] Confirm classic menu deletion was not bundled unless independently proven safe; menu fallback may remain for F14.
- [ ] Confirm icon status is `GREEN` or explicitly `BLOCKED — source unavailable`; never substitute.
- [ ] Record next bounded task as F14.1 productizing existing Organize/page routing.

## Self-review

### Spec coverage
- Home/dashboard: Tasks 1–2.
- iconography/visual language: Task 3.
- left Acrobat-inspired primary tool rail: Tasks 4–5.
- preserve pages/context + right properties: Tasks 4–5 acceptance.
- app icon: Task 6.
- responsive/accessibility/manual evidence: Tasks 2, 4, 5, 7.
- no business-engine rewrite: global constraints + equivalence gates.

### Proportion / YAGNI
F13 does not implement Edit V3, new converters, Compare V2, forms expansion or packaging. It only establishes the product shell needed to expose those later.

## Execution method

Use **ChatGPT/native implementation** once Gate 0 is GREEN. Codex is only needed for real Windows visual/manual verification or a defect that reproduces only on the user's PC.
