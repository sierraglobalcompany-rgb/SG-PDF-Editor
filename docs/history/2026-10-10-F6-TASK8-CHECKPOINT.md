# F6 — Task 8 checkpoint — independent preservation matrix + Save As warning policy

**Date:** 2026-10-10  
**Branch:** `feat/f6-images`  
**Task 8 base:** `a4632c2cd21f604c58ea7c9a340d0759eb11fbbe`  
**Scope:** Task 8 only — independent F6 preservation evidence, preflight classification and transactional `Guardar como...` warning policy.

## 1. TDD evidence

### 1.1 Preservation characterization RED

Commit: `c12d93b885cb5ed1d708a51d6f323a0b0738b6f2`  
Workflow run: `38055253981`

- Release build: PASS;
- build warnings: 0;
- build errors: 0;
- tests: **650 passed / 1 failed / 0 skipped / 651 total**;
- the sole intended failure was the missing `ImageEditPreflightInspector` contract;
- all eight real-writer preservation checks passed.

The real F6 writer preserved, after a 1-point image move + save + reopen, the representative fixture's:

- forms;
- bookmarks;
- named destinations;
- internal links;
- tagged structure;
- page labels;
- attachments;
- metadata values.

### 1.2 Policy RED

Commit: `88aac1b97f8b129d705192c058709a7bc317b0cd`  
Workflow run: `38055408445`

- Release build: PASS;
- build warnings: 0;
- build errors: 0;
- tests: **650 passed / 13 failed / 0 skipped / 663 total**;
- the 13 failures were the intended missing Task-8 preflight types/policy contracts.

### 1.3 Preflight GREEN

Commit: `0575144d19d2c7dc5c5b1ece6ad6c187c433cb0f`  
Workflow run: `38055554085`

- repository hygiene: PASS;
- locked restore: PASS;
- Release build: PASS;
- build warnings: 0;
- build errors: 0;
- tests: **663 passed / 0 failed / 0 skipped**.

Implemented `ImageEditPreflight.cs` with:

- `ImageEditFindingSeverity`;
- `ImageEditPreservationStatus`;
- `ImageEditFindingKind`;
- `ImageEditFinding`;
- `ImageEditPreflightResult`;
- `ImageEditPreflightInspector`.

### 1.4 Save As UI RED

Commit: `c8496e00667d82a35de1a5ed62f2ef6d6096847c`  
Workflow run: `38055674928`

- Release build: PASS;
- build warnings: 0;
- build errors: 0;
- tests: **663 passed / 8 failed / 0 skipped / 671 total**;
- the eight failures were Task-8 Save As contracts: command/seams absent, warning/block/cancel semantics absent, baseline timing absent and materialization guard absent.

### 1.5 Functional GREEN

Commit: `0a31e88cce60f269c945b0a8de7f39caaa314332`  
Workflow run: `38055929156`

Exact-head Windows evidence:

- checkout: `0a31e88cce60f269c945b0a8de7f39caaa314332`;
- repository hygiene: PASS;
- locked restore: PASS;
- Release build: PASS;
- build warnings: **0**;
- build errors: **0**;
- tests: **671 passed / 0 failed / 0 skipped**.

## 2. Independent preservation classification

F6 does not inherit F5 classifications mechanically. F5's writer reconstructs/imports pages; F6 reopens the original PDF and mutates real image page objects in-place with PDFium.

Observed F6 representative automated evidence:

| Structure | Classification | Save As severity |
|---|---|---|
| Forms | `ProvenPreserved` | `Info` |
| Bookmarks | `ProvenPreserved` | `Info` |
| Named destinations | `ProvenPreserved` | `Info` |
| Internal links | `ProvenPreserved` | `Info` |
| Tagged structure | `ProvenPreserved` | `Info` |
| Page labels | `ProvenPreserved` | `Info` |
| Attachments | `ProvenPreserved` | `Info` |
| Metadata | `ProvenPreserved` | `Info` |
| Cryptographic signature | `Unknown` | `Block` |
| Password-opened source | `Unknown` | `Block` |

Future noncrypto `ProvenChangedOrLost` or `Unknown` findings are `Warning` and require explicit confirmation.

Full evidence is recorded in `docs/history/2026-10-09-F6-preservation-matrix.md`.

## 3. Save As policy and transactional behavior

EDITAR now exposes a `Guardar como...` command that is available independently of image selection while image-edit mode is active.

The flow is:

1. destination selection;
2. independent F6 preflight;
3. `Block` stops with writer invocation count zero;
4. `Warning` requires exactly one explicit confirmation;
5. declining a warning stops with writer invocation count zero;
6. writer materializes only after policy passes;
7. `MarkSavedBaseline()` runs only after successful writer completion;
8. cancel or failure leaves dirty/history unchanged;
9. `_imageEditMaterializing` prevents concurrent/double Save As execution and is released in `finally`.

The writer's existing password, source fingerprint and signature guards remain in place. `warningsConfirmed` is now consumed rather than ignored.

Source overwrite policy is unchanged: F6 remains **Save As only**.

## 4. Exact scope audit

Diff from Task-8 base `a4632c2cd21f604c58ea7c9a340d0759eb11fbbe` to functional GREEN `0a31e88cce60f269c945b0a8de7f39caaa314332` contains exactly six files:

1. `src/SGPdf.App/Features/Edit/Images/ImageEditPreflight.cs` — new F6 preflight model/inspector;
2. `src/SGPdf.App/MainWindow.EditImages.cs` — Save As command, dialog seams, warning/block transaction and materialization guard;
3. `src/SGPdf.App/Pdf/PdfImageEditWriter.cs` — enforce noncrypto preflight warning confirmation;
4. `tests/SGPdf.App.Tests/ImageEditPreflightPolicyTests.cs` — policy RED/GREEN contracts;
5. `tests/SGPdf.App.Tests/ImageEditPreservationTests.cs` — independent real-writer preservation characterization;
6. `tests/SGPdf.App.Tests/MainWindowEditImageSaveAsTests.cs` — transactional Save As RED/GREEN contracts.

No Task-8 change touches:

- package dependencies or lockfiles;
- PDFium native bindings;
- FIRMAR implementation;
- ORGANIZAR implementation;
- LEER implementation;
- ZPL implementation;
- cloud/network/runtime services;
- source overwrite policy.

## 5. Repository state and boundaries

- `main` remains `31c0594758a83ec555d73ecdd7c597cdf8791fd7`;
- no F6 pull request exists;
- no merge was created;
- PDFium remains the only F6 PDF editing engine;
- no persistent native handles were added;
- manual Windows UX QA remains **NOT RUN**;
- private Mercado Libre/customer corpus QA remains **NOT RUN**;
- automated PASS is not presented as manual QA PASS.

## 6. Next gate

Publish this checkpoint and preservation matrix, then run fresh exact-head Windows CI on the checkpoint commit.

If that exact-head run is green, **Task 8 is CLOSED/GREEN** and work must stop. **Task 9 is NOT STARTED**; it may begin only after the next explicit user `continua`.
