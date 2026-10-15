# F14 Productize Existing Capabilities Implementation Plan

> **Execution:** ChatGPT/native after F13 GREEN. Codex only for real Windows verification or a Windows-only reproduced defect.

**Estado:** PREFLIGHT AUDITADO / IMPLEMENTACIÓN BLOQUEADA HASTA F13 GREEN  
**Goal:** hacer descubribles y utilizables desde el shell F13 todas las capacidades ya implementadas, sin reescribir motores PDF/ZPL.  
**Architecture:** extender únicamente el catálogo estático y el router del shell. Cada acción delega a su owner actual. Antes de ejecutar una acción que cambia/sale de modo, el shell usa una transición segura explícita que llama los guards existentes.  
**Tech:** C# / .NET 10 / WPF / PDFium actual / PDFsharp narrow-use / Labelize / qpdf / Tesseract. Sin paquetes nuevos.

**Spec:** `docs/product/2026-10-14-local-suite-expansion.md`  
**Inventory:** `docs/product/2026-10-14-existing-tool-entrypoint-inventory.md`

## 0. Preflight correction — guard ownership

El código real demuestra que no todos los guards están contenidos dentro del método funcional:

- COMENTAR protege cambios dirty mediante `PreviewMouseLeftButtonDown` sobre los botones legacy y `TryLeaveCommentModeWithGuard()`;
- `CanRunUtility()` cubre PDF/busy/Edit/Sign/Organize/ZPL, pero no `_commentModeActive`;
- `CanStartBatch()` tampoco cubre `_commentModeActive`;
- `CanStartCryptographicSigning()` tampoco cubre `_commentModeActive`;
- `ComparePdf_Click()` protege Edit/Organize/Sign, pero no COMENTAR;
- `EditAcroForm_Click()` protege Edit/Organize/Sign, pero no COMENTAR;
- el nuevo rail/router no puede depender de eventos de mouse de controles que F13 terminará eliminando.

### Decisión KISS

F14 usará **un helper local y explícito en `MainWindow.Shell.cs`**, por ejemplo:

```csharp
private bool TryPrepareForShellAction(string targetActionId)
```

El nombre exacto puede variar, pero su responsabilidad es solo:

1. identificar si la acción objetivo abandona el modo activo;
2. llamar los guards YA EXISTENTES:
   - `TryLeaveEditModeWithGuard()`;
   - `TryLeaveOrganizeModeWithGuard()`;
   - `TryLeaveCommentModeWithGuard()`;
   - `TryResolvePendingSignatureEdits(...)` cuando corresponda;
3. detener el routing si el usuario cancela;
4. no duplicar writers/workspaces/dialog business logic.

No crear:
- navigation framework;
- state machine genérica;
- command bus;
- service locator;
- event aggregator.

Si Forms demuestra cambios no guardados sin guard existente, se caracteriza primero y se agrega un guard mínimo dentro del owner Forms; no se inventa en el catálogo.

---

## 1. Global constraints

- **BLOCKED UNTIL:** UI-4 sin P0/P1 y F13 Product Shell GREEN.
- KISS: A→B.
- Anti-noise: `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
- Anti-patch: `REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI`.
- No mover business logic fuera de su owner.
- No duplicar availability/guard logic dentro del catálogo.
- No exponer future-only tools como funcionales.
- No paquete productivo nuevo.
- No cloud/API/login/AI/backend/DB.
- No merge a `main` sin aprobación explícita.

## 2. Review focus

1. Home/rail nunca debe saltarse dirty guards de Edit/Sign/Comment/Organize.
2. PDF-only tools no ejecutan contra Home/ZPL/no-document.
3. Batch/QR/ZPL siguen disponibles desde Home limpio sin sesión PDF falsa.
4. Censura sigue siendo redacción segura de texto dentro de Edit.
5. Firma visual y PFX/P12 permanecen flujos separados.
6. Forms no debe perder cambios silenciosamente al navegar desde el shell.

---

## Task 1 — Complete implemented-tool catalog

**Files:**
- `src/SGPdf.App/Shell/ToolCatalog.cs`
- `src/SGPdf.App/Shell/ToolDescriptor.cs` solo si es necesario
- `tests/SGPdf.App.Tests/ToolCatalogTests.cs`

### Implemented top-level actions

- `open-pdf`
- `open-zpl`
- `reader`
- `edit-pdf`
- `comments`
- `sign-visual`
- `sign-digital`
- `organize`
- `forms-text`
- `ocr-pdf`
- `redact-text`
- `compare-pdf`
- `watermark-text`
- `page-numbering`
- `protect-pdf`
- `pdf-to-png`
- `batch-watermark`
- `qr-png`

Future-only / non-executable:
- optimize
- unlock
- image-to-pdf
- crop
- pdf-to-jpg
- forms-designer
- compare-v2

### Steps
- [ ] RED completeness/uniqueness/capability-honesty tests.
- [ ] Add only missing implemented entries.
- [ ] Future actions remain absent or explicitly non-executable according to F13 catalog contract.
- [ ] Focused PASS.
- [ ] Full suite PASS.
- [ ] Commit: `feat(shell): expose implemented tool catalog`.

---

## Task 2 — Safe shell transition contract

**Files:**
- `src/SGPdf.App/MainWindow.Shell.cs`
- owner partial only if a reusable guard seam is genuinely missing
- `tests/SGPdf.App.Tests/MainWindowShellGuardTests.cs`

### RED cases

Prove from shell routing:
- dirty COMENTAR + user cancels → Edit/Sign/Organize/utility action does NOT run;
- dirty Edit + cancel → target action does NOT run;
- dirty visual signature + cancel → target action does NOT run;
- dirty Organize + cancel → target action does NOT run;
- clean mode transition resets/leaves previous owner once;
- target equal to current mode does not prompt unnecessarily;
- no writer/service is created by this helper.

### Implementation

Add one explicit helper local to `MainWindow` that calls existing guards. Keep target handling explicit, not reflective.

### Steps
- [ ] RED shell guard tests.
- [ ] Minimal helper implementation.
- [ ] Focused PASS.
- [ ] Full suite PASS.
- [ ] Exact-head CI GREEN.
- [ ] Commit: `fix(shell): preserve owner guards across tool routing`.

This task must complete before Tasks 3–5.

---

## Task 3 — Route document/professional tools to existing owners

**Files:**
- `MainWindow.Shell.cs`
- tiny wrappers in owners only if event handler is the only reusable entry
- `MainWindowToolRoutingTests.cs`

### Routes

- `forms-text` → current AcroForm owner;
- `sign-digital` → cryptographic owner;
- remaining F13 core modes continue using their established F13 routes.

Before owner execution:
`TryPrepareForShellAction(actionId)` must succeed.

### Forms characterization

Before routing Forms broadly:
- create/open `/Tx` fixture;
- modify field value without saving;
- attempt shell navigation away;
- determine whether current owner has an adequate dirty/discard contract.

If not:
- classify as owner gap;
- add the smallest Forms-owned guard;
- causal test;
- no generic dirty framework.

### Steps
- [ ] RED routing + guard tests.
- [ ] Minimal wrappers/routing.
- [ ] Forms dirty-state characterization.
- [ ] Focused PASS.
- [ ] Full suite + CI GREEN.
- [ ] Commit: `feat(shell): route document tools to existing owners`.

---

## Task 4 — Route utility tools to existing owners

**Files:**
- `MainWindow.Shell.cs`
- tiny owner wrappers only if required:
  - `MainWindow.Utilities.cs`
  - `MainWindow.Ocr.cs`
  - `MainWindow.Compare.cs`
  - `MainWindow.Conversions.cs`
  - `MainWindow.Batch.cs`
  - `MainWindow.QrCode.cs`
  - `MainWindow.Labels.cs`
- `MainWindowUtilityToolRoutingTests.cs`

### Routes

- `watermark-text`
- `page-numbering`
- `protect-pdf`
- `ocr-pdf`
- `compare-pdf`
- `pdf-to-png`
- `batch-watermark`
- `qr-png`
- `open-zpl`

### Rules

- call safe-transition helper first when current mode can own dirty work;
- after transition, existing `CanRunUtility`, `CanStartBatch` etc. remain authoritative for business availability;
- do not copy those predicates into catalog;
- PDF-only actions unavailable in Home/ZPL/no-document;
- Batch/QR/ZPL can run from clean Home state under current owner guards.

### Compare note

`ComparePdf_Click()` currently guards Edit/Organize/Sign but not Comment. The shell helper must close that gap before invoking it; do not duplicate compare logic.

### Steps
- [ ] RED utility routing tests.
- [ ] RED dirty Comment → utility/compare blocked when discard cancelled.
- [ ] Minimal routing/wrappers.
- [ ] Availability tests.
- [ ] Focused PASS.
- [ ] Full suite + CI GREEN.
- [ ] Commit: `feat(shell): route utility tools to existing owners`.

---

## Task 5 — Productize Censura safely

**Files:**
- `MainWindow.Shell.cs`
- `MainWindow.Edit.Text.cs` only for concise guidance seam if required
- Home/rail copy
- `RedactionShellRoutingTests.cs`

### Contract

`redact-text`:
1. safe transition into Edit;
2. user selects compatible text;
3. existing `TryStageSelectedTextRedaction()`;
4. existing Edit Save As writer path.

No rectangle redaction claim. No second redaction workspace/writer.

Suggested guidance:
`Selecciona el texto que deseas censurar. SG PDF verificará si puede eliminarlo de forma segura antes de Guardar como...`

### Steps
- [ ] RED route/guard contract.
- [ ] Minimal guidance route.
- [ ] Existing redaction preservation tests PASS.
- [ ] Full suite + CI GREEN.
- [ ] Commit: `feat(shell): expose safe redaction workflow`.

---

## Task 6 — Home grouping / availability / no-dead-card gate

**Files:**
- `HomeDashboard.xaml`
- `HomeDashboard.xaml.cs`
- `MainWindow.Shell.cs`
- `HomeDashboardContractTests.cs`
- `ToolAvailabilityTests.cs`

Categories:
- Organizar
- Editar
- Convertir
- Seguridad
- Firmar
- Formularios
- OCR
- Profesional
- Etiquetas

### Contract

- enabled card always resolves to known implemented action;
- disabled/future card never executes;
- host computes presentation availability from real state/owner helpers, not duplicated business rules;
- Enter/Space routes exactly like mouse;
- accessible names present.

### Steps
- [ ] RED grouping/no-dead-card/accessibility tests.
- [ ] Implement availability presentation.
- [ ] Focused/full suite + CI.
- [ ] Windows smoke: Home + PDF, Light/Dark + one scaled profile.
- [ ] Commit: `feat(shell): productize existing capability groups`.

---

## Task 7 — F14 closure / anti-noise cleanup

- [ ] every intended F0–F12 top-level capability reachable through Home/rail/menu fallback;
- [ ] no duplicate writer/renderer/workspace;
- [ ] no future-only false claim;
- [ ] dirty guards preserved from shell transitions;
- [ ] Forms navigation characterized and safe;
- [ ] redundant shell entries removed only after proof;
- [ ] full exact-head CI 0 failures;
- [ ] Windows smoke no P0/P1;
- [ ] update `STATE.md` / `ROADMAP.md`;
- [ ] next block = F15 Edit V3.

Suggested commit: `docs(state): close F14 productized capabilities`.

## Execution method

ChatGPT/native implementation after F13 GREEN. Codex only for bounded real Windows evidence or a Windows-only defect.
