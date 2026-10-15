# F14 Productize Existing Capabilities Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** make every already-implemented SG PDF capability discoverable and usable from the new F13 shell without rewriting any PDF/ZPL business engine.

**Architecture:** extend the F13 static catalog and shell router only. Each shell action delegates to the current feature owner (`MainWindow.*` partial / existing service path); owner guards remain authoritative. F14 adds no generic module framework and no new PDF writer/renderer.

**Tech Stack:** C# / .NET 10 / WPF / current native Fluent resources; existing PDFium, PDFsharp narrow-use, Labelize, qpdf and Tesseract only.

**Spec:** `docs/product/2026-10-14-local-suite-expansion.md`  
**Inventory:** `docs/product/2026-10-14-existing-tool-entrypoint-inventory.md`

## Global Constraints

- **BLOCKED UNTIL:** UI-4 has no open P0/P1 and F13 Product Shell is GREEN.
- KISS: simple, efficient, optimized, effective; A→B.
- Anti-noise: `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
- Anti-patch: `REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.
- Do not move business logic out of its existing owner merely to satisfy the shell.
- Do not duplicate guard logic inside the catalog.
- Do not expose future/non-working cards as enabled functionality.
- No new production package.
- No cloud, API, account, AI, backend or DB.
- Source PDF remains protected by the existing Save As/copy semantics.
- No `main` merge without explicit user approval.

## Review Focus

1. A Home card must never bypass dirty Edit/Sign/Comment/Organize guards.
2. Tools that require an open PDF must never run against a ZPL/no-document state.
3. Batch/QR/ZPL actions that can start without an open PDF must remain usable from Home without creating a fake PDF session.
4. Censura must route through the existing safe text-redaction flow; no rectangle-redaction claim is introduced.
5. PFX/P12 signing must remain a separate cryptographic action from visual signing and must preserve password-memory handling.

---

### Task 1: Complete the implemented-tool catalog

**Files:**
- Modify: `src/SGPdf.App/Shell/ToolCatalog.cs`
- Modify if required: `src/SGPdf.App/Shell/ToolDescriptor.cs`
- Test: `tests/SGPdf.App.Tests/ToolCatalogTests.cs`

**Interfaces:**
- Consumes the F13 `ToolDescriptor`/`ToolCatalog` types.
- Produces catalog entries for implemented capabilities only.
- `ActionId` values must map one-to-one to the inventory document.

- [ ] **Step 1: write failing catalog completeness tests**

Assert the implemented catalog contains exactly the intended top-level actions:
- `open-pdf`, `open-zpl`, `reader`, `edit-pdf`, `comments`, `sign-visual`, `sign-digital`, `organize`, `forms-text`, `ocr-pdf`, `redact-text`, `compare-pdf`, `watermark-text`, `page-numbering`, `protect-pdf`, `pdf-to-png`, `batch-watermark`, `qr-png`.

Assert no entry exists yet for future-only `optimize`, `unlock`, `image-to-pdf`, `crop`, `pdf-to-jpg`, `forms-designer`, `compare-v2`.

- [ ] **Step 2: run focused RED**

Run:
`dotnet test tests/SGPdf.App.Tests/SGPdf.App.Tests.csproj -c Release --filter FullyQualifiedName~ToolCatalogTests`

- [ ] **Step 3: add only missing implemented entries**

Use concise Spanish title/description and the F13 icon resource keys. No runtime reflection.

- [ ] **Step 4: focused tests PASS**

- [ ] **Step 5: full suite PASS**

- [ ] **Step 6: checkpoint**

Suggested commit: `feat(shell): expose implemented tool catalog`

---

### Task 2: Route document-mode tools to existing owners

**Files:**
- Modify: `src/SGPdf.App/MainWindow.Shell.cs`
- Modify only if a reusable parameterless owner method is needed:
  - `src/SGPdf.App/MainWindow.Sign.cs`
  - `src/SGPdf.App/MainWindow.Signing.cs`
  - `src/SGPdf.App/MainWindow.Edit.cs`
  - `src/SGPdf.App/MainWindow.Comments.cs`
  - `src/SGPdf.App/MainWindow.Organize.cs`
  - `src/SGPdf.App/MainWindow.Forms.cs`
- Test: `tests/SGPdf.App.Tests/MainWindowToolRoutingTests.cs`

**Interfaces:**
- `HandleToolAction(string actionId)` remains the only shell dispatcher.
- Existing feature owner remains responsible for state/guards/dialogs.

- [ ] **Step 1: write RED routing tests**

Prove:
- `reader` reaches current Read owner;
- `edit-pdf` reaches `TryEnterEditModeAsync()` path;
- `comments` reaches `TryEnterCommentMode()` path;
- `sign-visual` reaches Sign owner;
- `organize` reaches `TryEnterOrganizeMode()` path;
- `forms-text` reaches existing AcroForm flow;
- `sign-digital` reaches cryptographic owner and its guard.

Tests must verify the shell does not instantiate replacement workspaces/services.

- [ ] **Step 2: implement minimal routing adapters**

If an event handler is currently the only entrypoint, add a tiny method in the same partial and have both old UI and shell call it. Do not copy handler bodies into `MainWindow.Shell.cs`.

- [ ] **Step 3: add dirty-state regression tests**

At least one each for Edit/Sign/Comment guard behavior when switching via shell.

- [ ] **Step 4: focused PASS**

- [ ] **Step 5: full suite + exact-head CI GREEN**

- [ ] **Step 6: checkpoint**

Suggested commit: `feat(shell): route document tools to existing owners`

---

### Task 3: Route utility/professional tools to existing owners

**Files:**
- Modify: `src/SGPdf.App/MainWindow.Shell.cs`
- Modify only for tiny reusable entry wrappers when necessary:
  - `src/SGPdf.App/MainWindow.Utilities.cs`
  - `src/SGPdf.App/MainWindow.Ocr.cs`
  - `src/SGPdf.App/MainWindow.Compare.cs`
  - `src/SGPdf.App/MainWindow.Conversions.cs`
  - `src/SGPdf.App/MainWindow.Batch.cs`
  - `src/SGPdf.App/MainWindow.QrCode.cs`
  - `src/SGPdf.App/MainWindow.Labels.cs`
- Test: `tests/SGPdf.App.Tests/MainWindowUtilityToolRoutingTests.cs`

**Interfaces:**
- Shell action routes to current owner only.
- Existing `CanRunUtility`, `CanStartBatch`, `CanStartCryptographicSigning`, busy/session guards stay authoritative.

- [ ] **Step 1: RED utility routing tests**

Cover:
- `watermark-text` → current watermark dialog/owner;
- `page-numbering` → current Bates/numbering owner;
- `protect-pdf` → current protection owner;
- `ocr-pdf` → current OCR owner;
- `compare-pdf` → current compare owner;
- `pdf-to-png` → existing exporter flow;
- `batch-watermark` → existing batch owner;
- `qr-png` → existing QR owner;
- `open-zpl` → `OpenZpl_Click` path.

- [ ] **Step 2: implement only minimal wrappers/routing**

No generic `ITool` abstraction or service locator.

- [ ] **Step 3: availability regression tests**

Verify:
- PDF-only actions unavailable/no-op with no PDF or ZPL active;
- batch/QR/ZPL remain usable from clean Home state according to current guards;
- busy state prevents conflicting operations.

- [ ] **Step 4: focused PASS**

- [ ] **Step 5: full suite + exact-head CI GREEN**

- [ ] **Step 6: checkpoint**

Suggested commit: `feat(shell): route utility tools to existing owners`

---

### Task 4: Productize Censura without inventing a new redaction engine

**Files:**
- Modify: `src/SGPdf.App/MainWindow.Shell.cs`
- Modify: `src/SGPdf.App/MainWindow.Edit.Text.cs` only if a small guidance entry method is required.
- Modify: Home/rail copy/template files from F13 as needed.
- Test: `tests/SGPdf.App.Tests/RedactionShellRoutingTests.cs`

**Interfaces:**
- `redact-text` routes into Edit mode.
- Actual redaction stays `TryStageSelectedTextRedaction()` on a selected eligible text object.
- Publication stays `TrySaveEditWorkspace()`.

- [ ] **Step 1: RED redaction-route contract**

Assert shell `redact-text`:
- enters Edit using existing guard path;
- surfaces concise guidance to select compatible text;
- does not create rectangle-redaction state;
- does not create a second Save As/writer.

- [ ] **Step 2: implement minimal guidance route**

Suggested user copy:
`Selecciona el texto que deseas censurar. SG PDF verificará si puede eliminarlo de forma segura antes de Guardar como...`

- [ ] **Step 3: focused + existing F12.1 preservation tests PASS**

- [ ] **Step 4: full suite + exact-head CI GREEN**

- [ ] **Step 5: checkpoint**

Suggested commit: `feat(shell): expose safe redaction workflow`

---

### Task 5: Home grouping, disabled states and no-dead-card gate

**Files:**
- Modify: `src/SGPdf.App/Shell/HomeDashboard.xaml`
- Modify: `src/SGPdf.App/Shell/HomeDashboard.xaml.cs`
- Modify: `src/SGPdf.App/MainWindow.Shell.cs`
- Test: `tests/SGPdf.App.Tests/HomeDashboardContractTests.cs`
- Test: `tests/SGPdf.App.Tests/ToolAvailabilityTests.cs`

**Interfaces:**
- Catalog defines display grouping.
- Host supplies current availability from owner/state; catalog does not reimplement business guards.

- [ ] **Step 1: RED category tests**

Expected categories:
- Organizar
- Editar
- Convertir
- Seguridad
- Firmar
- Formularios
- OCR
- Profesional
- Etiquetas

- [ ] **Step 2: RED no-dead-card test**

Every visible enabled card must resolve to a known implemented `ActionId`. Future-only actions must not be presented as working cards.

- [ ] **Step 3: implement grouping/availability presentation**

Disabled tool must look disabled and expose a short tooltip/reason where useful. Do not fork owner logic.

- [ ] **Step 4: keyboard/accessibility contract**

Tab order predictable; card title available to automation; Enter/Space triggers the same route as mouse click.

- [ ] **Step 5: focused/full suite + exact-head CI**

- [ ] **Step 6: Windows smoke**

Check Home + loaded PDF in light/dark and one scaled profile. Codex only if required for real Windows execution.

- [ ] **Step 7: checkpoint**

Suggested commit: `feat(shell): productize existing capability groups`

---

### Task 6: F14 closure and anti-noise cleanup

**Files:**
- Update: `.planning/STATE.md`
- Update: `.planning/ROADMAP.md`
- Update inventory only if final names changed.

- [ ] Confirm every F0–F12 implemented top-level capability intended for product discovery is reachable through Home/rail/menu fallback.
- [ ] Confirm no duplicate business writer/renderer/workspace was added.
- [ ] Confirm no future-only tool is falsely exposed as implemented.
- [ ] Confirm legacy menus may remain as fallback; delete only proven redundant shell elements.
- [ ] Confirm full exact-head CI totals and 0 failures.
- [ ] Confirm real Windows smoke has no new P0/P1.
- [ ] Set next bounded block to F15 Edit V3.

Suggested commit: `docs(state): close F14 productized capabilities`

## Self-review

### Spec coverage
- Existing capabilities become discoverable: Tasks 1–5.
- Reuse current owners: Tasks 2–4.
- No dead/future cards: Tasks 1 and 5.
- Censura keeps safe existing semantics: Task 4.
- Offline/local model preserved: global constraints.

### Review-focus coverage
- Dirty mode switching: Task 2.
- PDF-vs-ZPL/no-document state: Tasks 3 and 5.
- Home-only batch/QR/ZPL: Task 3.
- Safe-redaction semantics: Task 4.
- Visual vs cryptographic signing separation: Tasks 1–2.

### Proportion / YAGNI
F14 does not implement Edit V3, new converters, Optimize/Unlock/Crop, Compare V2, new form field types, scanner or packaging. It only productizes capability that already exists.

## Execution method

Use **ChatGPT/native implementation** after F13 GREEN. Codex is reserved for real Windows verification or a Windows-only reproduced defect.
