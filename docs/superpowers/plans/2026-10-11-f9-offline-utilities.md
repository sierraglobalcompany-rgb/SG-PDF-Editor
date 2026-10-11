# F9 Offline Utilities Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver focused offline utilities for text watermarking, page/Bates numbering, authorized PDF protection, and evidence-based optimize/repair without weakening SG PDF Editor's conservative preservation model.

**Architecture:** Utilities are independent commands, not another persistent editor workspace. PDFium remains primary; every persistence slice uses transactional Save As plus reopen validation, and any qpdf/pdfcpu adoption requires a failed PDFium capability gate first.

**Tech Stack:** C# / .NET 10 / WPF / pinned `bblanchon.PDFium.Win32 156.0.8076` / existing DejaVu Sans 2.37 offline asset.

**Spec:** `docs/superpowers/specs/2026-10-11-f9-offline-utilities-design.md`

## Global Constraints

- Offline only; no runtime network/cloud/API keys/accounts.
- Keep `main` frozen; no merge without explicit approval.
- Branch: `feat/f9-offline-utilities`, stacked from F8 closure `7e092166b04d72e2b8d4d1ba2d95d48e1acdebb3`.
- PDFium first; no qpdf/pdfcpu until a concrete PDFium capability gap is demonstrated.
- TDD per task: observable RED → minimum GREEN → full Windows CI exact-head → scope audit → checkpoint.
- Source PDF is never modified in place.
- Manual Windows/external-reader QA is NOT RUN unless actually executed.

## Review Focus

1. Rotated/cropped pages: overlay geometry must remain visually correct rather than assuming portrait MediaBox coordinates. Task 3 owns tests.
2. Signed/password-encrypted PDFs: utilities must fail closed unless a later protection slice explicitly owns authorized-password semantics. Tasks 3, 5 and 7 own tests.
3. Cancellation or validator failure after temp creation: existing destination must survive unchanged and sibling temp must be removed. Tasks 3 and 5 own tests.
4. Unicode numbering/watermark strings and very large page numbers: formatting must be deterministic and overflow-safe. Task 2 owns tests.
5. Valid PDFs with unusual document-level structures: output must reopen and preserve representative metadata/forms/bookmarks/links/tags/attachments/page labels. Task 8 owns the preservation matrix.

---

### Task 1: Exact PDFium overlay capability gate

**Files:**
- Create: `tests/SGPdf.App.Tests/PdfiumUtilityOverlayCapabilityTests.cs`
- Create: `tests/SGPdf.App.Tests/UtilityOverlayNativeHarness.cs`
- Modify only if a required exported function is absent from managed interop: `src/SGPdf.App/Pdf/PdfiumNative.Utilities.cs`
- History: `docs/history/2026-10-11-F9-TASK1-OVERLAY-CAPABILITY-GATE.md`

**Interfaces:**
- Consumes existing `PdfiumRuntime`, `PdfiumNative`, pinned DejaVu fallback asset and PDFium save writer pattern.
- Produces measured facts only: text overlay support, z-order behavior, Unicode/color/alpha/matrix/rotation round-trip, image overlay support and any exact interop additions required by the pinned binary.

- [ ] **Step 1: Write RED capability tests** for a text overlay inserted behind and in front of existing content. Assert Unicode `NIÑO Bogotá 東京`, non-opaque RGBA, position/matrix, non-zero rotation, deterministic page-object order, Save As, reopen text/object discovery and low-DPI render smoke.
- [ ] **Step 2: Add RED image-overlay characterization** using a tiny in-memory bitmap. Assert object insertion, matrix, alpha when measurable, z-order, Save As, reopen image discovery and render smoke.
- [ ] **Step 3: Run exact Windows CI and accept RED only if failures are caused by missing gate harness/interop rather than broken test syntax or build errors.**
- [ ] **Step 4: Implement the minimum characterization harness/interops.** Reuse the existing CID Type2/DejaVu path; do not add a product writer or UI.
- [ ] **Step 5: Run the full suite.** Expected: all tests green, build 0 warnings / 0 errors excluding external runner deprecation noise.
- [ ] **Step 6: Record measured capability decisions.** Image watermark is explicitly deferred if any required persistence/alpha/z-order behavior is not proven.
- [ ] **Step 7: Commit checkpoint and run exact-head CI.**

### Task 2: Managed settings and numbering formatter

**Files:**
- Create: `src/SGPdf.App/Features/Utilities/UtilityPageRange.cs`
- Create: `src/SGPdf.App/Features/Utilities/TextOverlaySettings.cs`
- Create: `src/SGPdf.App/Features/Utilities/PageNumberingSettings.cs`
- Create: `src/SGPdf.App/Features/Utilities/PageNumberFormatter.cs`
- Tests: `tests/SGPdf.App.Tests/PageNumberFormatterTests.cs`, `UtilityPageRangeTests.cs`

**Interfaces:**
- Produces pure managed settings with no native handles.
- `PageNumberFormatter.Format(long number, PageNumberingSettings settings) -> string`.
- `UtilityPageRange.Resolve(int pageCount) -> IReadOnlyList<int>` returns zero-based ordered page indices.

- [ ] **Step 1: RED tests** for all/current/explicit range, invalid range, deterministic ordering, start number, prefix/suffix, zero padding, Unicode and overflow boundaries.
- [ ] **Step 2: Implement minimum immutable models and formatter/parser behavior.**
- [ ] **Step 3: Full suite + exact-head CI + checkpoint.**

### Task 3: Transactional text overlay writer

**Files:**
- Create: `src/SGPdf.App/Pdf/PdfTextOverlayWriter.cs`
- Create: `src/SGPdf.App/Pdf/PdfTextOverlayOutputValidator.cs`
- Create: `src/SGPdf.App/Features/Utilities/PdfUtilityPreflight.cs`
- Tests: `tests/SGPdf.App.Tests/PdfTextOverlayWriterTests.cs`, `PdfUtilityPreflightTests.cs`

**Interfaces:**
- `PdfTextOverlayWriter.SaveAsCopy(string sourcePath, PdfEditSourceFingerprint fingerprint, IReadOnlyList<TextOverlayPlacement> placements, string destinationPath, CancellationToken cancellationToken)`.
- Writer uses only gate-proven insertion/z-order behavior and pinned fallback font.

- [ ] **Step 1: RED tests** for source/destination separation, stale fingerprint, signatures/password input fail-closed, foreground/background, rotated page geometry, cancellation/temp cleanup and validator-failure destination preservation.
- [ ] **Step 2: Minimum writer** with fresh source reopen, native gate, sibling temp, page-bounded mutation, GenerateContent, save, close, reopen validation, atomic publish.
- [ ] **Step 3: Full suite + exact-head CI + checkpoint.**

### Task 4: Watermark and numbering product services

**Files:**
- Create: `src/SGPdf.App/Features/Utilities/WatermarkPlanner.cs`
- Create: `src/SGPdf.App/Features/Utilities/PageNumberingPlanner.cs`
- Tests: `WatermarkPlannerTests.cs`, `PageNumberingPlannerTests.cs`

**Interfaces:**
- Produces `TextOverlayPlacement` values consumed by Task 3.
- Numbering uses Task 2 formatter; no separate Bates writer.

- [ ] **Step 1: RED planner tests** for anchor positions, offsets, rotation, opacity, selected ranges and six header/footer numbering anchors.
- [ ] **Step 2: Implement pure geometry/planning logic.**
- [ ] **Step 3: Integration tests create watermark and multi-page Bates-style numbering, save, reopen and verify text/render.**
- [ ] **Step 4: Full suite + exact-head CI + checkpoint.**

### Task 5: F9.1 focused WPF commands

**Files:**
- Create focused dialogs/partials under `src/SGPdf.App/Features/Utilities/` and `MainWindow.Utilities.cs`.
- Modify: `src/SGPdf.App/MainWindow.xaml` only for thin `Herramientas` command wiring.
- Tests: `tests/SGPdf.App.Tests/MainWindowUtilitiesTests.cs` plus dialog/view-model tests where practical.

**Interfaces:**
- UI maps user inputs to Task 2/4 managed models and delegates persistence to Task 3.

- [ ] **Step 1: RED structural/behavior tests** for Watermark and Number Pages commands, validation messages, Save As cancellation and dirty/error states.
- [ ] **Step 2: Minimum dialogs** for text watermark and numbering. Expose image watermark only if Task 1 gate passed every required capability.
- [ ] **Step 3: Full suite + exact-head CI + checkpoint.**

### Task 6: Protection capability gate and engine decision

**Files:**
- Create capability tests/harness under `tests/SGPdf.App.Tests/`.
- Create interop/tool adapter only after the gate determines the path.
- Update third-party manifest/licenses only if a new local tool is actually adopted.

**Interfaces:**
- Produces a documented engine decision for open-password and permission flags.

- [ ] **Step 1: RED characterization** for creating a newly protected copy and reopening with wrong/right password.
- [ ] **Step 2: Determine whether exact pinned PDFium can satisfy required encryption semantics.**
- [ ] **Step 3: If not, evaluate one minimal local candidate (prefer qpdf) with pinned version/digest/license and offline process adapter tests; do not add multiple fallback engines.**
- [ ] **Step 4: Exact-head CI + decision checkpoint.**

### Task 7: Authorized protection product slice

**Files:**
- Create focused protection settings/writer/validator/UI files based on Task 6 decision.
- Tests cover authorization, open password, permissions only if proven, cancellation and atomic Save As.

- [ ] **Step 1: RED behavior tests** including no password bypass/removal, wrong authorization, signatures policy and destination preservation.
- [ ] **Step 2: Minimum product implementation.**
- [ ] **Step 3: Full suite + exact-head CI + checkpoint.**

### Task 8: Optimize/repair capability gates and preservation matrix

**Files:**
- Create gate tests and `docs/history/2026-10-11-F9-preservation-matrix.md`.
- Add product code only for capabilities that pass.

- [ ] **Step 1: Build deterministic valid/malformed fixtures.**
- [ ] **Step 2: Prove lossless optimize semantics and measure size/reopen/render/text/preservation rather than assuming smaller is better.**
- [ ] **Step 3: Prove repair against intentionally recoverable malformed PDFs. If reliable recovery is not demonstrated, document NO-GO and expose no repair UI.**
- [ ] **Step 4: Preservation corpus** covers forms, bookmarks, links/named destinations, tagged PDF, attachments, metadata, page labels, rotations and existing supported/unsupported annotations.
- [ ] **Step 5: Full suite + exact-head CI + checkpoint.**

### Task 9: F9 hardening and automated closure

**Files:**
- Update: `.planning/STATE.md`, `.planning/REQUIREMENTS.md` only for facts actually delivered.
- Create: `.planning/phases/10-f9-utilities/PLAN.md` if not already created during kickoff.
- Create: `docs/history/2026-10-11-F9.md`.
- Update docs/third-party notices only for actual dependency changes.

- [ ] **Step 1: Run scope audit from F8 closure to F9 functional head.** No network/cloud/account/API-key/runtime-service additions.
- [ ] **Step 2: Full Windows CI exact-head.** Record build warnings/errors and exact pass/fail/skip counts.
- [ ] **Step 3: Document automated vs manual gates accurately.** Manual Windows and external-reader QA remains NOT RUN unless performed.
- [ ] **Step 4: Create stacked draft PR only after automated closure, keep unmerged, verify `main` unchanged.**

## Self-review

- Spec coverage: watermark, numbering/Bates, protection, optimize and repair all have an owning task; dependency adoption is gated.
- Type consistency: Task 2/4 managed planners feed Task 3 writer; UI remains adapter-only.
- Review Focus items are assigned to concrete tests.
- No task assumes image watermark, encryption, optimize or repair capability before executable evidence.
- Plan intentionally avoids a generic UtilityWorkspace or mega-writer.
