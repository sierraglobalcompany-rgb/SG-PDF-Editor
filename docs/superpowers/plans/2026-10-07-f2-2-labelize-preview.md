# F2.2 Labelize Adapter + Preview Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Render the F2.1 normalized ZPL document through pinned Labelize 1.7.0 as a local child process, map PNG outputs to printable designs, and show navigable previews in WPF without persistent customer-data residue.

**Architecture:** Keep parsing in managed C# and add one process-isolated `LabelizeProcessRenderer` under `Features/Labels`. A setup step downloads/verifies the pinned Windows x64 archive for development/CI; the normal product runtime only resolves a bundled local executable from `AppContext.BaseDirectory`. Each render uses a request-scoped temp directory, fully loads PNG bytes into managed memory, validates one output per printable design, then removes the temp directory.

**Tech Stack:** C# / .NET 10 / WPF, Labelize 1.7.0 Windows x64 CLI, xUnit, test-only ZXing.Net + SkiaSharp for barcode/QR regressions.

**Spec:** `docs/superpowers/specs/2026-10-07-f2-labelize-architecture-design.md`

## Global Constraints

- Windows x64, C#/.NET 10/WPF, KISS/YAGNI.
- Main product functions remain offline; runtime never downloads Labelize or uses HTTP.
- Runtime engine is pinned **Labelize 1.7.0 Windows x64 MSVC**.
- Release archive SHA-256: `cdd4030b0d1a8bad69b93f49866c8dcc5314af8975bb16a76991fe32f92dd21d`.
- CLI invocation uses `ProcessStartInfo.ArgumentList`; never shell-concatenate customer input.
- One Labelize invocation per normalized document stream, not one process per `^PQ` copy.
- `^PQ` remains metadata only; F2.2 does not implement quantity-selection UX.
- `^DF/^XF` support context remains in `NormalizedRenderText`; output count mismatch is a controlled failure.
- No PDF composition, thermal printing, local HTTP server, Rust ABI, BinaryKits fallback, accounts/cloud/network runtime.
- Real Mercado Libre/customer files remain private and are never committed or uploaded as CI artifacts.
- No merge without explicit user approval.

## Review Focus

- Multi-label CLI naming (`label_1.png`, `label_2.png`, …) maps deterministically and never guesses on count mismatch.
- Cancellation/timeout kills the child process tree and does not leave request temp directories.
- Missing/invalid Labelize executable produces a controlled error and preserves the previous valid workspace.
- PNG bytes are fully loaded before temp cleanup; WPF never holds a file-backed bitmap to a deleted temp path.
- `^DF/^XF`, Code128, QR and `^FT + ^BQ` regressions use the real pinned Labelize binary in Windows CI.

---

### Task 1: Pin and stage the Labelize sidecar

**Files:**
- Create: `tools/setup-labelize.ps1`
- Create: `third_party/licenses/Labelize-MIT.txt`
- Modify: `third_party/manifest.json`
- Modify: `.gitignore`
- Modify: `src/SGPdf.App/SGPdf.App.csproj`
- Modify: `.github/workflows/build.yml`
- Create: `src/SGPdf.App/Features/Labels/LabelizeRuntime.cs`
- Test: `tests/SGPdf.App.Tests/LabelizeRuntimeTests.cs`

**Interfaces:**
- Produces: `LabelizeRuntime.ResolveBundledExecutablePath(string baseDirectory) -> string`.
- Produces: local staged binary at ignored `third_party/runtime/labelize/labelize.exe`, copied to output as `labelize/labelize.exe` when present.

- [ ] **Step 1: Write RED tests** for the deterministic bundled path and controlled missing-file validation.
- [ ] **Step 2: Run CI and verify RED** because `LabelizeRuntime` does not exist.
- [ ] **Step 3: Implement runtime resolver + pinned setup script + manifest/license/build staging.** The setup script downloads only v1.7.0 Windows x64, verifies the exact SHA-256, extracts `labelize.exe`, and is idempotent.
- [ ] **Step 4: Run full Windows CI and verify GREEN.** Build must have 0 warnings/errors and tests green with Labelize staged before build.
- [ ] **Step 5: Commit** as the task implementation.

### Task 2: Safe child-process execution

**Files:**
- Create: `src/SGPdf.App/Features/Labels/ChildProcessRunner.cs`
- Modify: `src/SGPdf.App/SGPdf.App.csproj` only if `InternalsVisibleTo` is needed through an assembly attribute file instead.
- Create: `src/SGPdf.App/Properties/AssemblyInfo.cs`
- Test: `tests/SGPdf.App.Tests/ChildProcessRunnerTests.cs`

**Interfaces:**
- Produces internal `ChildProcessRunner.RunAsync(ProcessStartInfo startInfo, TimeSpan timeout, CancellationToken cancellationToken) -> Task<ChildProcessResult>`.
- `ChildProcessResult` exposes `ExitCode`, `StandardOutput`, `StandardError`.

- [ ] **Step 1: Write RED tests** for stdout/stderr capture, timeout, and cooperative cancellation using Windows system processes.
- [ ] **Step 2: Run CI and verify RED** because the runner is absent.
- [ ] **Step 3: Implement minimal runner** with redirected output/error, no shell execution, bounded timeout, linked cancellation and `Kill(entireProcessTree: true)` on timeout/cancel.
- [ ] **Step 4: Run full Windows CI and verify GREEN.** No leaked child process should remain from the test command.
- [ ] **Step 5: Commit.**

### Task 3: Real Labelize renderer, mapping and cleanup

**Files:**
- Create: `src/SGPdf.App/Features/Labels/ZplRenderOptions.cs`
- Create: `src/SGPdf.App/Features/Labels/ZplRenderedLabel.cs`
- Create: `src/SGPdf.App/Features/Labels/LabelizeProcessRenderer.cs`
- Create: `tests/SGPdf.App.Tests/LabelizeProcessRendererTests.cs`
- Modify: `tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj`
- Modify: `tests/SGPdf.App.Tests/packages.lock.json`

**Interfaces:**
- Produces `ZplRenderOptions(double WidthMm, double HeightMm, int Dpmm)` with F2.2 defaults 102 mm × 152 mm at 8 dpmm and validation for Labelize-supported dpmm values 6/8/12/24.
- Produces `ZplRenderedLabel(int DesignIndex, double WidthMm, double HeightMm, int Dpmm, byte[] PngBytes)`.
- Produces public `LabelizeProcessRenderer.RenderAsync(ZplDocument document, ZplRenderOptions options, CancellationToken cancellationToken = default) -> Task<IReadOnlyList<ZplRenderedLabel>>`.
- Public/default renderer resolves `AppContext.BaseDirectory/labelize/labelize.exe`; an internal constructor accepts executable/timeout/temp-root for tests.

- [ ] **Step 1: Write RED integration tests** using the pinned staged Labelize binary for: one PNG, two printable labels, `^DF/^XF` support block + printable label mapping, Code128 decode, QR decode, and `^FT + ^BQ` decode/position smoke. Add failure tests for missing executable and output-count mismatch where feasible without inventing mappings.
- [ ] **Step 2: Run CI and verify RED** because renderer/models do not exist.
- [ ] **Step 3: Implement renderer**: request temp dir; UTF-8 no-BOM `input.zpl`; `convert`, `--format zpl`, `--type png`, explicit `--width`, `--height`, `--dpmm`; output stem `label.png`; enumerate either single `label.png` or numbered `label_1.png...` in numeric order; validate non-empty PNGs and exact count; load bytes; cleanup in `finally`.
- [ ] **Step 4: Verify cancellation/temp cleanup** with a pre-cancelled token and a dedicated temp root; no request directory may remain.
- [ ] **Step 5: Run full Windows CI and verify GREEN**, including real Labelize barcode/QR regressions.
- [ ] **Step 6: Commit.**

### Task 4: WPF preview and design navigation

**Files:**
- Modify: `src/SGPdf.App/MainWindow.Labels.cs`
- Modify: `src/SGPdf.App/MainWindow.xaml`
- Modify: `tests/SGPdf.App.Tests/MainWindowZplTests.cs`

**Interfaces:**
- Consumes `LabelizeProcessRenderer.RenderAsync(...)` and `ZplRenderedLabel.PngBytes`.
- Produces candidate-first open flow: parse + render succeeds before PDF/ZPL workspace replacement.
- Produces a compact ZPL preview navigation bar: Previous, `Etiqueta n de N`, Next; each design appears once regardless of `^PQ`.

- [ ] **Step 1: Write RED STA WPF tests** proving commit shows the first PNG, label navigation changes the selected PNG/index with bounds, and busy state disables label navigation/open commands.
- [ ] **Step 2: Run CI and verify RED** because rendered-label state/navigation does not exist.
- [ ] **Step 3: Integrate preview** using `BitmapImage.CacheOption=OnLoad`; retain managed PNG bytes only; opening a new ZPL renders in background before commit; closing the window cancels an active label render.
- [ ] **Step 4: Run full Windows CI and verify GREEN.**
- [ ] **Step 5: Commit.**

### Task 5: Closure, audit and exact-head verification

**Files:**
- Create: `.planning/phases/02-f1-zpl-gate/F2.2-PLAN.md`
- Modify: `.planning/STATE.md`
- Modify: `.planning/ROADMAP.md` only for current-position stamp if needed
- Create: `docs/history/2026-10-07-F2.2.md`
- Update PR body.

**Interfaces:** none beyond documenting the verified branch state.

- [ ] **Step 1: Audit the whole diff** against this plan/spec: no HTTP/server, no BinaryKits, no PDF composition, no private fixtures, no committed executable/archive.
- [ ] **Step 2: Run fresh exact-head Windows CI** and require repository hygiene, locked restore, Release build, complete tests, 0 failures.
- [ ] **Step 3: Record RED/GREEN run IDs, final head, remaining manual/private QA and next slice F2.3.**
- [ ] **Step 4: Keep PR draft and do not merge.**
