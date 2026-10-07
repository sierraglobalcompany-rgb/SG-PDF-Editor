# F2.4 Layout + PDF Export Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add exact-physical label sheet layout, WPF sheet preview, and transactional PDF export on top of F2.3.

**Architecture:** Keep layout math as pure managed C# in millimeters, separate from WPF and PDF creation. Quantities remain lazy to avoid expanding very large `^PQ`/custom counts. WPF preview and PDF export consume the same planner output; PDFsharp is introduced only when export begins.

**Tech Stack:** C# / .NET 10 / WPF / existing Labelize preview / existing PDFium / PDFsharp 6.2.4 in Task 3 only.

**Spec:** `docs/superpowers/specs/2026-10-07-f2-4-layout-pdf-export-design.md`

## Global Constraints

- Offline runtime only; no HTTP/network service.
- Never scale a label merely to make a layout fit.
- Quantities use `long` and are not eagerly expanded into copies.
- Thermal output is exact-size one label per page.
- A4 = 210 × 297 mm; Letter = 215.9 × 279.4 mm.
- Label rotation is explicit 0° or 90° only in this slice.
- No printer/driver code in F2.4.
- No ZXing/decode dependency in F2.4.
- PDFsharp must be pinned to stable 6.2.4 if/when Task 3 starts.
- No merge without explicit user approval.

## Review Focus

1. Huge `^PQ` / custom quantities must not allocate one object per requested copy.
2. A layout that misses by fractions of a millimeter must fail rather than shrink.
3. 90° rotation must swap occupied dimensions without altering physical label size.
4. Export failure must leave an existing destination PDF intact.
5. Custom media/margins/gaps must reject NaN, infinity, zero/negative dimensions and impossible usable areas.

---

### Task 1: Pure output sequence + physical layout geometry

**Files:**
- Create: `src/SGPdf.App/Features/Labels/LabelOutputSequence.cs`
- Create: `src/SGPdf.App/Features/Labels/LabelLayoutSettings.cs`
- Create: `src/SGPdf.App/Features/Labels/LabelLayoutPlan.cs`
- Create: `src/SGPdf.App/Features/Labels/LabelLayoutPlanner.cs`
- Test: `tests/SGPdf.App.Tests/LabelOutputSequenceTests.cs`
- Test: `tests/SGPdf.App.Tests/LabelLayoutPlannerTests.cs`

**Interfaces:**
- Consumes: `ZplDocument`, `ZplDesign`, `ZplQuantitySelection`, current label width/height from `ZplRenderOptions`.
- Produces: `LabelOutputSequence.TotalCount`, `GetDesignIndexAt(long)`, `LabelLayoutSettings`, `LabelLayoutPlanner.CreatePlan(...)`, `LabelLayoutPlan.GetPage(long)` and page placements in millimeters.

- [ ] **Step 1: Write RED tests for lazy quantity indexing**

Cover file/one-each/custom order, checked total, first/last output lookup, out-of-range index and a 99,999,999 quantity without materializing copies.

- [ ] **Step 2: Run focused tests**

Run the new output-sequence tests. Expected: compile/test failure because `LabelOutputSequence` does not exist.

- [ ] **Step 3: Implement minimal `LabelOutputSequence`**

Use cumulative checked quantities over printable designs; resolve indexes without expanding copies.

- [ ] **Step 4: Run focused + full suite**

Expected: output-sequence tests PASS and existing suite remains green.

- [ ] **Step 5: Write RED planner tests**

Cover A4/Letter/custom sizes, preset 1/2/4/6 grids, custom rows×columns, margins/gaps, deterministic grid choice, 90° rotation, exact-fit boundary, fractional non-fit, page count and huge quantity page-count math.

- [ ] **Step 6: Run planner tests**

Expected: fail because layout types/planner do not exist.

- [ ] **Step 7: Implement minimal pure layout model/planner**

All math in millimeters; no WPF/PDF types. Fail invalid/impossible geometry with a descriptive validation result/exception chosen consistently by tests. Thermal is exact one-label page.

- [ ] **Step 8: Run focused + full suite + CI**

Expected: all PASS; no new package dependencies.

- [ ] **Step 9: Commit Task 1**

Commit geometry and tests only.

---

### Task 2: WPF sheet configuration + preview

**Files:**
- Modify: `src/SGPdf.App/MainWindow.xaml`
- Modify: `src/SGPdf.App/MainWindow.Labels.cs`
- Create if needed: `src/SGPdf.App/Features/Labels/LabelSheetPreviewState.cs`
- Test: `tests/SGPdf.App.Tests/MainWindowZplLayoutUiTests.cs`

**Interfaces:**
- Consumes: Task 1 planner/plan, existing rendered PNG labels, F2.3 quantity/render settings.
- Produces: sheet settings UI, `Etiqueta | Hoja` preview mode, sheet navigation and WPF preview based on the same millimeter placements.

- [ ] **Step 1: Write RED WPF tests**

Assert controls for Thermal/A4/Letter/Custom, layout presets/custom grid, margins/gaps, rotation, preview mode and sheet navigation. Quantity/layout changes must not invoke Labelize. Invalid fit must surface a reason and mark export unavailable.

- [ ] **Step 2: Run focused tests**

Expected: fail on missing controls/state.

- [ ] **Step 3: Implement minimal WPF controls and sheet preview**

Reuse right properties panel. Keep individual label preview intact. Render sheet preview from managed PNG bytes with viewport-only scaling.

- [ ] **Step 4: Run focused + full suite + CI**

Expected: PASS, no new dependency.

- [ ] **Step 5: Commit Task 2**

---

### Task 3: Transactional PDF export with PDFsharp 6.2.4

**Files:**
- Modify: `src/SGPdf.App/SGPdf.App.csproj`
- Modify: `src/SGPdf.App/packages.lock.json`
- Modify dependency/license manifest/docs used by the repo.
- Create: `src/SGPdf.App/Features/Labels/LabelPdfExporter.cs`
- Modify: `src/SGPdf.App/MainWindow.Labels.cs`
- Modify: `src/SGPdf.App/MainWindow.xaml`
- Test: `tests/SGPdf.App.Tests/LabelPdfExporterTests.cs`
- Test/update UI contract tests as needed.

**Interfaces:**
- Consumes: `LabelLayoutPlan`, rendered PNG bytes.
- Produces: local PDF whose page dimensions and image rectangles match the plan exactly; `Guardar como PDF` UI.

- [ ] **Step 1: Write RED export tests before adding PDFsharp**

Tests cover exact A4/Letter/custom MediaBox dimensions, label placement rectangles, rotation, multipage counts and existing-destination preservation on synthetic failure.

- [ ] **Step 2: Run RED**

Expected: fail because exporter/package does not exist.

- [ ] **Step 3: Add pinned PDFsharp 6.2.4 and lock/license metadata**

No preview/7.x package. No MigraDoc unless a test demonstrates necessity (expected: not needed).

- [ ] **Step 4: Implement minimal `LabelPdfExporter`**

Convert mm→PDF points, draw PNGs from memory, write to same-directory temporary file, close/validate, then atomically publish. Never shrink to fit.

- [ ] **Step 5: Add `Guardar como PDF` UI**

Enable only for a valid layout plan. User chooses destination through standard Windows save dialog.

- [ ] **Step 6: Run focused + full suite + CI**

Expected: PASS, build 0 warnings/errors.

- [ ] **Step 7: Commit Task 3**

---

### Task 4: Reopen validation + closure audit

**Files:**
- Test: `tests/SGPdf.App.Tests/LabelPdfExportIntegrationTests.cs`
- Modify: `.planning/STATE.md`
- Modify: `.planning/ROADMAP.md`
- Create/update: `.planning/phases/02-f1-zpl-gate/F2.4-PLAN.md`
- Create: `docs/history/2026-10-07-F2.4.md`
- Modify: `AGENTS.md` only if execution order needs continuity update.

**Interfaces:**
- Consumes: Task 3 exported PDFs and existing PDFium read/render path.
- Produces: automated evidence that exported pages reopen with expected physical page dimensions and render successfully.

- [ ] **Step 1: Write integration RED where missing**

Generate representative exported PDFs and reopen through existing PDF path/PDFium metadata/render. Verify page count and physical page dimensions within PDF unit tolerance.

- [ ] **Step 2: Run integration + full suite**

Expected: PASS after only minimal fixes required by real reopen behavior. Do not add barcode decoder here.

- [ ] **Step 3: Whole-branch audit**

Confirm no print-driver code, no runtime network, no BinaryKits fallback, no private fixtures, no silent scaling and only PDFsharp 6.2.4 as the new runtime package.

- [ ] **Step 4: Update state/history/PR**

Record exact head and fresh CI. Keep PR draft/no merge. Manual/private QA remains open.

- [ ] **Step 5: Exact-head push + PR CI**

Expected: Windows hygiene/staging/locked restore/build/tests PASS; 0 warnings / 0 errors.
