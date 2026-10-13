# F12.3 Existing AcroForm Implementation Plan

> Implement task-by-task. Task 1 is a hard capability gate and may legitimately close NO-GO.

**Goal:** Rellenar campos AcroForm clásicos ya existentes mediante `Guardar como...`, sin modificar el original, sin segundo motor PDF y con validación automatizada de valor + apariencia + preservación.

**Architecture:** PDFsharp 6.2.4 is the candidate writer proven by Task 1 for a classic text field. PDFium remains the reader/render engine and independent checker. Later transactional publication must reuse `PdfEditPreflightInspector`, `PdfEditSourceFingerprint`, same-directory temp, validation and atomic publish.

**Spec:** `docs/superpowers/specs/2026-10-13-f12-3-existing-acroform-design.md`

## Global constraints

- KISS/YAGNI and Ley del Ruido.
- Existing AcroForm only; no form designer.
- No XFA, JavaScript/calculation engine, signature fields, flatten or advanced styling in MVP.
- No new PDF engine/package during Task 1.
- Signed/password-opened sources stay fail-closed.
- No merge without explicit approval.

---

### Task 1: PDFsharp AcroForm text-field capability gate — GO

**Files:**
- `tests/SGPdf.App.Tests/PdfAcroFormCapabilityTests.cs`
- Production files unchanged.

**Gate result:** GO for an existing classic text field. PDFsharp 6.2.4 persists `/V`, regenerates the field `/AP/N`, leaves source bytes exact, preserves page count/form detection, and the output reopens/renders through PDFium. The ordinary `PdfDocumentSession.RenderPage()` is not a widget renderer because it does not initialize FormFill/`FPDF_FFLDraw`; therefore persisted `/AP/N` is inspected directly for this gate while normal rendering remains an open/render smoke.

- [x] **Step 1 — RED:** focused gate created.
- [x] **Step 2 — exact RED evidence:** SHA `2d430962185750d2c8e64ae06f9e5177869aaf2b`, build 0 warnings / 0 errors, 1150 baseline PASS, sole intended gate failure.
- [x] **Step 3 — minimal GREEN:** test-only PDFsharp typed text-field setter; no production service/UI.
- [x] **Step 4 — appearance proof:** source/output `/AP/N` unfiltered streams differ and output appearance contains `UPDATED BY SG PDF`; `/V` independently equals the new value.
- [x] **Step 5 — preservation proof:** source bytes exact, page count stable, form still detected, output opens/renders, no package/runtime dependency added.
- [x] **Step 6 — exact-head CI:** SHA `8dd08032efd5d917c7f344ee834c56c9c8cd9bc5`; build 0 warnings / 0 errors; 1151/1151 PASS, 0 skipped.
- [x] **Step 7 — decision:** GO. Task 2 may start.

**Characterization:** PDFsharp Core requires an explicit font-resolution strategy when materializing `PdfTextField`; the gate isolates this with a test-only resolver. Production font handling must be explicit before Task 3 and must not add an unnecessary PDF engine.

---

### Task 2: Minimal AcroForm inspector/model — OPEN

Potential files:
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