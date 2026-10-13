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

### Task 4: Independent validator + representative preservation — CLOSED / GREEN

**Files:**
- `src/SGPdf.App/Features/Forms/PdfAcroFormOutputValidator.cs`
- `src/SGPdf.App/Features/Forms/PdfAcroFormSaveService.cs`
- `tests/SGPdf.App.Tests/PdfAcroFormFormFillCapabilityTests.cs`
- `tests/SGPdf.App.Tests/PdfAcroFormOutputValidatorTests.cs`
- `tests/SGPdf.App.Tests/PdfAcroFormPreservationTests.cs`
- `tests/SGPdf.App.Tests/PdfAcroFormSaveServiceValidatorIntegrationTests.cs`

**Validation proven:**
- persisted target `/V` must equal the expected value after reopen;
- target `/Rect` must stay stable;
- target `/AP/N` must remain usable and must change when the value changes;
- representative source/output visual evidence is compared with PDFium FormFill + `FPDF_FFLDraw`;
- validator failure occurs before publication and preserves the previous destination while cleaning the temp;
- the source fingerprint is still rechecked after validation and before atomic publication.

**Representative preservation gate:** page count, unrelated page text, a native image XObject, bookmark + destination, URI link, second untouched `/Tx` value + `/AP/N`, selected Info metadata and exact source bytes are preserved by the representative fixture. This is not a universal PDF-preservation claim.

**KISS / independence boundary:** PDFsharp performs narrow structural checks of values, geometry and persisted appearance; PDFium FormFill provides a separate rendered visual signal. The save path now uses `PdfAcroFormOutputValidator` as the single pre-publication validator; the older redundant `ValidateTemp` path was removed. No second PDF engine, new package, generic forms framework, backend, network, telemetry or UI were introduced.

- [x] **Task 4.1 — FormFill capability GO:** `e2d245b6945c854b495db3e02083903c192d1779`; workflow `38432225516`; job `115417065446`; 1163/1163 PASS.
- [x] **Task 4.2 — validator RED:** `68c61f26e5a8c27f3574546acfde07e33f135be1`; workflow `38433037081`; job `115419867542`; build clean, 1163 baseline PASS + 4 intended RED failures.
- [x] **Task 4.3 — validator GREEN:** functional `67d9b83c11bd6d241b65572cc08b126e9ad795ea`, exact-head `7e35fe8599801621a99bb7d2b62a90f05937b7f6`; workflow `38435244741`; job `115427392807`; 1167/1167 PASS.
- [x] **Task 4.4 — representative preservation:** `0474586f8a026d224db60acfc7cb79d26bebb719` → `c9da1c2d20cbb71af2c2b987efb12198ab63f2c6`; workflow `38438239079`; job `115437752560`; 1168/1168 PASS.
- [x] **Task 4.5 integration RED:** `c5f0e29d2540af71dac78c007c2f3bfb73d8f7c7`; workflow `38439465785`; job `115441952240`; 1168 baseline PASS + 1 intended failure proving the validator was not yet wired into publication.
- [x] **Task 4.5 integration GREEN:** `fbe16db8f5e1dcf2bdd08e608e474763028be431`; workflow `38439821214`; job `115443183657`; build 0 warnings / 0 errors; 1169/1169 PASS, 0 skipped.
- [x] **Task 4 exact-head closure:** `0a5568bca12cd25c4217c69b4a02c59528bcd1e8`; workflow `38440303987`; job `115444840022`; build 0 warnings / 0 errors; 1169/1169 PASS, 0 skipped.

**Decision:** Task 4 CLOSED / GREEN. The preservation statement remains a **representative preservation gate**, not a universal PDF-preservation claim.

History: `docs/history/2026-10-13-F12-3-task4-independent-validator-preservation.md`.

---

### Task 5: Minimal WPF flow — CLOSED / GREEN

**Files:**
- `src/SGPdf.App/MainWindow.Forms.cs`
- narrow lifecycle integrations in `MainWindow.Reader.cs`, `MainWindow.Labels.cs`, `MainWindow.Sign.cs`, `MainWindow.Edit.cs`, `MainWindow.Organize.cs`
- `tests/SGPdf.App.Tests/PdfAcroFormWpfFlowContractTests.cs`

**Scope proven:**
- only existing supported classic `/Tx` fields are exposed;
- fields are loaded through `PdfAcroFormInspector.InspectTextFields`;
- the existing right-side properties host is reused; no second window/sidebar/editor shell;
- save uses only `Guardar como...` through the existing transactional `PdfAcroFormSaveService`;
- only fields whose values changed are sent to the writer;
- no-op save is skipped before opening the save dialog;
- form UI state is reset on PDF/workspace changes;
- Formularios is mutually exclusive with FIRMAR, EDITAR and ORGANIZAR;
- existing edit/organize/signature guards are reused instead of introducing a mode coordinator.

**KISS / Ley del Ruido:** PASS. The implementation stays inside the existing `MainWindow` partial pattern and adds no MVVM/controller/view-model framework, form designer, second PDF engine, package, backend, network, telemetry or generic mode architecture.

- [x] **Initial RED:** `37d60a1d5b25a27440e2ac4501415f8c1763dd59`.
- [x] **RED aligned to existing runtime-UI pattern:** `4a62d9035e632af4ddee94cbab7f9d296ff9d58f`.
- [x] **Minimal UI GREEN:** `65dc4d31cf79b5ec3019ed650d942145bea61e5f`; workflow `38444540427`; 1170/1170 PASS.
- [x] **Hardening RED — changed fields + workspace reset:** `4debb2937c68fc162f656ffdc9c0399bff879df8`; workflow `38445453673`; build clean, 1170 baseline PASS + 2 intended failures.
- [x] **Hardening GREEN:** `2476ee017c62ca68f85e06ae2fce4967f2e54c60`; workflow `38447153597`; 1172/1172 PASS.
- [x] **Mode-exclusivity RED:** `d72c9a8c3edaf05e6a62f0cbfc8850ec2a5a2bd0`; workflow `38448279524`; build clean, 1172 baseline PASS + 1 intended failure.
- [x] **Mode-exclusivity GREEN / functional Task 5 HEAD:** `85aaefd3f4dc8b1822cc145d3dc44a26ae6c2d8a`; workflow `38449772950`; job `115477374426`; build 0 warnings / 0 errors; 1173/1173 PASS, 0 skipped.

**Decision:** Task 5 CLOSED / GREEN for the supported classic `/Tx` scope. Checkbox/radio/combo/list, XFA, JavaScript/calculations/actions, signature fields as form fields, flattening, form creation/designer and advanced styling remain NOT RUN/out of scope.

History: `docs/history/2026-10-13-F12-3-task5-minimal-wpf-flow.md`.

---

### Task 6: Exact-head closure — CLOSED / GREEN

**Pre-closure fresh verification:** HEAD `38a4aab4b2593c16b6d95233c84147f2e3210ab9`; workflow `38451787497`, rerun job `115504632038`; Windows build 0 warnings / 0 errors; 1173/1173 PASS, 0 skipped; repository hygiene PASS.

**AUTO PASS:** existing classic text fields `/Tx` only, including flat fields, hierarchy via `/Kids`, inherited `/FT /Tx`, composed names, transactional save, target value `/V`, target appearance `/AP/N`, representative PDFium FormFill visual validation and the minimal WPF `Guardar como...` flow.

**NOT RUN / out of scope:** checkbox/radio (`/Btn`), combo/list (`/Ch`), XFA, JavaScript/calculations/actions, form signature fields, flattening, form creation/designer, advanced styling and universal reader/PDF preservation claims.

**Final boundary:** preservation remains a **representative preservation gate**. No universal preservation or compatibility claim is made.

**KISS / Ley del Ruido:** PASS. F12.3 introduced no new package, second PDF engine, backend, network, telemetry, mode coordinator, parallel MVVM/controller architecture or second editor shell. No `.csproj` or lockfile changed from the F12.2 base.

**Decision:** F12.3 is CLOSED / GREEN only when the exact-head Windows workflow triggered by this documentation commit passes. On PASS, STOP without starting F12.4 and without merge.

History: `docs/history/2026-10-13-F12-3-exact-head-closure.md`.
