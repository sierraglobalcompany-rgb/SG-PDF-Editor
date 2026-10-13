# F12.3 Existing AcroForm Implementation Plan

> Implement task-by-task. Task 1 is a hard capability gate and may legitimately close NO-GO.

**Goal:** Rellenar campos AcroForm clásicos ya existentes mediante `Guardar como...`, sin modificar el original, sin segundo motor PDF y con validación automatizada de valor + apariencia + preservación.

**Architecture:** PDFsharp 6.2.4 is only a candidate writer until Task 1 proves it. PDFium remains the reader/render engine and independent checker. Later transactional publication must reuse `PdfEditPreflightInspector`, `PdfEditSourceFingerprint`, same-directory temp, validation and atomic publish.

**Spec:** `docs/superpowers/specs/2026-10-13-f12-3-existing-acroform-design.md`

## Global constraints

- KISS/YAGNI and Ley del Ruido.
- Existing AcroForm only; no form designer.
- No XFA, JavaScript/calculation engine, signature fields, flatten or advanced styling in MVP.
- No new PDF engine/package during Task 1.
- Signed/password-opened sources stay fail-closed.
- No merge without explicit approval.

---

### Task 1: PDFsharp AcroForm text-field capability gate

**Files:**
- Create: `tests/SGPdf.App.Tests/PdfAcroFormCapabilityTests.cs`
- Production files: unchanged.

**Gate:** A synthetic classic AcroForm text field must be writable on an output copy, persist after reopen, remain a form under PDFium, render successfully, visibly update its appearance, and leave source bytes exact.

- [ ] **Step 1 — RED:** add one focused test with a deliberately unimplemented test-only write probe. Require source intact, output value changed, form detected, page count stable and render appearance changed.
- [ ] **Step 2 — exact RED evidence:** run Windows CI on exact RED SHA. Baseline tests must remain green; failure must belong only to the new gate.
- [ ] **Step 3 — minimal GREEN:** implement only the test-side PDFsharp probe necessary to set the existing text-field value and save output. No production API/service/UI.
- [ ] **Step 4 — appearance proof:** reopen semantic value independently and render the field region. A `/V`-only success without visible new appearance is a gate failure.
- [ ] **Step 5 — preservation proof:** source bytes unchanged, page count stable, form still detected, output opens/renders, no new runtime dependency.
- [ ] **Step 6 — exact-head CI:** hygiene, locked restore, Release build, full Windows suite; require 0 warnings / 0 errors / all tests PASS.
- [ ] **Step 7 — decision:** record GO or NO-GO. On NO-GO, STOP F12.3; do not start Task 2.

---

### Task 2: Minimal AcroForm inspector/model — blocked until Task 1 GO

Potential files only after GO:
- `src/SGPdf.App/Pdf/PdfiumNative.Forms.cs`
- `src/SGPdf.App/Pdf/PdfDocumentSession.Forms.cs`
- one small managed field model
- focused tests

Inspect only field kinds proven supportable. No native handles escape `NativeGate`.

---

### Task 3: Transactional AcroForm writer/service — blocked until Task 2

Reuse existing preflight/fingerprint transaction:
`preflight → fingerprint → same-dir temp → write → validate → fingerprint recheck → atomic publish → cleanup`.

Original never overwritten. Existing destination survives failures.

---

### Task 4: Independent validator + preservation — blocked until Task 3

Validate supported field values after reopen, rendered appearance, page count and representative preserved structures. Fail closed on ambiguity.

---

### Task 5: Minimal WPF flow — blocked until Task 4

Expose only existing supported fields in a focused editor. No second editor shell/sidebar architecture. `Guardar como...` only.

---

### Task 6: Exact-head closure — blocked until Task 5

Update planning/history only after full CI. Mark only proven field types AUTO PASS; all others remain NOT RUN/out of scope. STOP without starting another professional slice or merging.