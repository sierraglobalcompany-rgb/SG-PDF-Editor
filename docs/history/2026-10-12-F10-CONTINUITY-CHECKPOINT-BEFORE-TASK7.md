# F10 — Continuity checkpoint before Task 7

Date: 2026-10-12 UTC
Branch: `feat/f10-local-ocr`
Purpose: preserve exact continuity before starting Task 7 UI work.

## Repository state at checkpoint start

Parent HEAD: `7b204d323398d9d524c34f983863fedbe01e8a05`
Parent message: `docs(f10): close task6 OCR workflow green`

`main` remains untouched. No merge or PR action is authorized by this checkpoint.

## F10 status

### Task 6 — CLOSED / GREEN

Formal Task 6 checkpoint already exists:

- `docs/history/2026-10-11-F10-TASK6-GREEN-CHECKPOINT.md`
- functional GREEN commit: `c5e35830a7b95e39c01aab36ac787c2a773227f8`
- formal closure commit: `7b204d323398d9d524c34f983863fedbe01e8a05`
- exact-head CI run for closure: `38191844007`
- exact-head job: `114626056172`
- Release build: 0 warnings / 0 errors
- tests: `1046 PASS / 0 FAIL / 0 SKIP`

Task 6 behavior closed includes:

- OCR preflight over existing edit preflight.
- Password-opened and cryptographically signed PDF blocking.
- Spanish default, English, Spanish+English.
- Current/range/automatic scopes and hybrid-document handling.
- Sequential page-by-page OCR with no partial publication.
- Workspace/raster/TSV cleanup across failures and cancellation.
- Real PDFium rasterizer + pinned local Tesseract + strict TSV parser + text-layer planner + transactional OCR writer in production constructor.
- Progress sequence ending in `Writing -> Validating` only after writer success.

## Task 7 — NOT STARTED IN PRODUCTION

No Task 7 production code has been added at this checkpoint.
No Task 7 RED tests have been committed yet.
Task 8 and F11 have not started.

### Authoritative Task 7 direction recovered from active plan

Task 7 must remain KISS and reuse existing WPF patterns:

- One OCR command under `Herramientas`.
- Minimal dialog/view-model.
- User-facing options only: language, scope, Save As, progress, Cancel.
- No technical tuning controls.
- Run the production `OcrWorkflow` created in Task 6.
- On success, reopen/load the generated searchable PDF using the existing MainWindow/document-loading path rather than inventing a second reader path.
- Cancellation and failure must be surfaced clearly and must not publish partial success.

### Existing WPF pattern already inspected

`src/SGPdf.App/MainWindow.Utilities.cs` currently owns the dynamically injected `Herramientas` menu through `InitializeUtilitiesUi()`.

Existing entries:

- `WatermarkTextMenuItem`
- `PageNumberingMenuItem`
- `ProtectionMenuItem`

Existing pattern:

- find main `Menu` under the root `DockPanel`;
- create `UtilitiesMenuItem` with header `_Herramientas`;
- bind command availability to `PrintPdfMenuItem.IsEnabled`;
- attach `Click` handlers;
- `RegisterName(...)` for menu items;
- use `SaveFileDialog` via `SelectUtilityDestination(...)`;
- gate execution through `CanRunUtility()`.

`CanRunUtility()` currently requires:

- active PDF session/navigation;
- not busy;
- no utility already executing;
- no signature mode;
- no edit mode;
- no organize mode;
- no active ZPL document.

Task 7 should integrate with this existing `Herramientas` host rather than creating a second top-level Tools menu.

### Existing dialog patterns available

Current project already contains WPF dialog pairs such as:

- `PdfPasswordDialog.xaml` / `.xaml.cs`
- `SignatureDrawDialog.xaml` / `.xaml.cs`
- `SignatureLibraryDialog.xaml` / `.xaml.cs`
- `SignaturePhotoDialog.xaml` / `.xaml.cs`

Task 7 should follow the same lightweight WPF style unless the active F10 plan requires otherwise.

## Exact next work

1. Confirm this checkpoint SHA is current branch HEAD.
2. Write Task 7 RED tests before production UI changes.
3. Keep RED small and structural, covering at minimum:
   - OCR entry exists under the single existing `Herramientas` menu;
   - OCR dialog/view-model exposes Spanish default plus supported language choices;
   - scope supports automatic/current/range without technical OCR controls;
   - progress/cancel contract exists;
   - successful workflow result can trigger the existing document reopen path.
4. Run exact-head CI and prove the RED failures are only the intended missing Task 7 production contract.
5. Only then implement minimum GREEN UI wiring.
6. Do not start Task 8 or F11 until Task 7 has its own verified GREEN checkpoint.

## Guardrails

- KISS/YAGNI.
- Local/offline/privacy only.
- PDFium remains PDF owner; Tesseract remains recognition only.
- No new cloud/backend/database.
- No second PDF/image stack.
- Save As only; source unchanged.
- Strict RED -> expected fail -> minimum GREEN -> targeted/full tests -> exact-head Windows CI -> checkpoint.
- Manual Windows/Adobe/Foxit/Edge/private-corpus QA remains NOT RUN unless separately evidenced.
- No merge to `main` without explicit user approval.
