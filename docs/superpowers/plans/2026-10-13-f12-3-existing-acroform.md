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

### Task 2: Minimal AcroForm inspector/model — CLOSED / GREEN

**Files:**
- `src/SGPdf.App/Features/Forms/PdfAcroFormInspector.cs`
- `src/SGPdf.App/Features/Forms/PdfAcroFormTextFieldInfo.cs`
- `tests/SGPdf.App.Tests/PdfAcroFormInspectorTests.cs`

**Scope proven:**
- root classic text fields;
- hierarchical text fields via `/Kids`;
- inherited `/FT`;
- partial-name composition as `Parent.Child`;
- terminal `/Tx` values only;
- unsupported `/Btn` fields skipped;
- intermediate hierarchy nodes are not surfaced.

**KISS result:** inspection uses existing PDFsharp dictionaries only. No FormFill, no extra P/Invoke, no font resolver, no package, no second PDF engine and no UI were added.

- [x] **Flat-field GREEN:** SHA `19726df0be1d54c2d4f6240075295f4297bd8d5b`; build 0 warnings / 0 errors; 1152/1152 PASS.
- [x] **Hierarchy RED:** SHA `367cd78b7e813b68392af62610afe072a26a8900`; build 0 warnings / 0 errors; 1152 baseline PASS and one intended new failure.
- [x] **Hierarchy GREEN:** SHA `6799b23512e874b1ae373f370c769e0f589fdd67`; workflow `38360052031`; job `115171840150`; 0 warnings / 0 errors; 1153/1153 PASS, 0 skipped.
- [x] **Decision:** Task 2 CLOSED. Only classic text fields are proven. Checkbox/radio/combo/list remain NOT RUN.

History: `docs/history/2026-10-13-F12-3-task2-acroform-inspector.md`.

---

### Task 3: Transactional AcroForm writer/service — CLOSED / GREEN

**Files:**
- `src/SGPdf.App/Features/Forms/PdfAcroFormSaveService.cs`
- `tests/SGPdf.App.Tests/PdfAcroFormSaveServiceTests.cs`
- `tests/SGPdf.App.Tests/PdfAcroFormSaveServicePreflightTests.cs`
- `tests/SGPdf.App.Tests/PdfAcroFormSaveServiceHierarchyTests.cs`

**Transaction proven:**
`preflight → supported-field gate → fingerprint → same-dir temp → write /V + /AP/N → reopen/value validation → fingerprint recheck → atomic publish → cleanup`.

**Scope proven:**
- flat classic text fields;
- hierarchical text fields using composed names such as `customer.name`;
- inherited `/FT /Tx`;
- `/V` persistence;
- regenerated persisted `/AP/N`;
- source untouched on success;
- existing destination preserved on prepublish failure, source drift and cancellation;
- signed source blocked;
- real password-protected source blocked without publication;
- temp cleanup on success/failure.

**KISS result:** one focused service reuses the existing inspector, preflight, fingerprint and transactional publication pattern. The final hierarchy fix removes the redundant typed lookup-by-name pass and applies PDFsharp's own `PdfTextField.Text` appearance regeneration directly to the terminal `/Tx` dictionary already resolved by the raw hierarchy traversal. No synthetic `/FT`, manual `/AP` writer, new package, second PDF engine or UI were added.

- [x] **Transaction RED:** `d328c93cfa3b96ee02924737772dba12cdf65dbc`.
- [x] **Transaction GREEN:** `98d9594570cce73aa63e7f37913aa309f8d0ab03`.
- [x] **Appearance RED:** `f4acbb737d27876ee0400bdee2e16201537f5521`.
- [x] **Appearance GREEN:** `c1ff671e67cbcff920f270814e8a79db1d0ccd8a`.
- [x] **Existing destination preservation:** `af7f10b8797436e50cdecb5c411d49e8169ff400`.
- [x] **Source fingerprint drift:** `efbe2cee1a12f5db8d5708c5a1b9e187362c510d`.
- [x] **Cancellation before publish:** `50b9be8a277227f5a89c59f9425df4ef35bdd852`.
- [x] **Signed source fail-closed:** `5e24731be958bacd6b29627aa2733b34c66165d4`.
- [x] **Real password-protected source fail-closed:** `ecd42c734cffaab5779ebbe9ea62af77992f7570`.
- [x] **Hierarchy RED:** `1988f9fb2e3db1178462e1d965877d12cfcda71e`; workflow `38425728873`; job `115394499911`; build 0 warnings / 0 errors; 1160 baseline PASS and one intended failure because PDFsharp materialized the inherited child as `PdfGenericField`.
- [x] **Hierarchy GREEN:** `37158b25e6d957c421eada7d23604af49c23efc9`; workflow `38426417849`; job `115396878669`; build 0 warnings / 0 errors; 1161/1161 PASS, 0 skipped.
- [x] **Decision:** Task 3 CLOSED. Only classic text fields are supported. Checkbox/radio/combo/list remain NOT RUN.

History: `docs/history/2026-10-13-F12-3-task3-transactional-acroform-writer.md`.

---

### Task 4: Independent validator + preservation — READY, NOT STARTED

Validate supported field values after reopen, rendered appearance, page count and representative preserved structures. Fail closed on ambiguity.

---

### Task 5: Minimal WPF flow — blocked until Task 4

Expose only existing supported fields in a focused editor. No second editor shell/sidebar architecture. `Guardar como...` only.

---

### Task 6: Exact-head closure — blocked until Task 5

Update planning/history only after full CI. Mark only proven field types AUTO PASS; all others remain NOT RUN/out of scope. STOP without starting another professional slice or merging.
