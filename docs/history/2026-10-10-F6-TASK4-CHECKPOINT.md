# F6 — Task 4 checkpoint — transforms, history and dirty guard

**Date:** 2026-10-10  
**Branch:** `feat/f6-images`  
**Task:** F6 Task 4 — F6.3 Move/resize/rotate/delete + undo/redo + dirty guard  
**Status:** CLOSED / GREEN  
**Task-3 base checkpoint:** `ba67362e9e134d24d0199ef6ffb121870451ab11`  
**Functional GREEN head:** `a188c27c54adc4763f79693d61b1e662f2dcbad1`

## 1. Scope completed

Task 4 now provides logical image editing in EDITAR without mutating the source PDF:

- move by drag with live overlay preview;
- corner resize with the opposite corner anchored;
- proportional resize by default;
- free-aspect resize while Shift is pressed;
- rotation around the image visual center;
- logical delete only;
- Ctrl+Z undo and Ctrl+Y redo;
- one completed gesture produces one history item;
- preview movement/resizing produces zero history entries until release;
- a new edit after undo clears redo;
- dirty guards for leaving EDITAR, opening another PDF and closing the window;
- clean EDITAR exits without prompting;
- discard destroys only the in-memory edit workspace.

No PDF writer, replacement-image materialization, opacity, z-order mutation or Save As pipeline was added in this task.

## 2. RED witness

RED commit:

`d1408223cea22ed96e32b8ff5488e63964ed9b04`

Message:

`test(images): add F6 Task 4 transform and hardening RED`

Windows CI run:

`38016448044`

Exact head verified:

`d1408223cea22ed96e32b8ff5488e63964ed9b04`

Evidence:

- repository hygiene: PASS;
- locked restore: PASS;
- Release build: PASS;
- build warnings: 0;
- build errors: 0;
- passed: 580;
- failed: 14;
- skipped: 0;
- total: 594.

All 14 failures were the new Task-4 contracts being intentionally absent: `ImageTransformMath`, `ImageResizeCorner`, logical transform commands, shortcut handling and dirty-guard contracts. No pre-existing test failed.

## 3. GREEN implementation

Functional GREEN commit:

`a188c27c54adc4763f79693d61b1e662f2dcbad1`

Message:

`feat(images): implement F6 Task 4 logical transforms and guards`

Production scope:

1. `src/SGPdf.App/Features/Edit/Images/ImageTransformMath.cs`
   - finite/non-singular affine validation;
   - translate;
   - rotate around center;
   - four-corner resize;
   - proportional/free-aspect behavior;
   - minimum-size and anchor-crossing rejection.
2. `src/SGPdf.App/MainWindow.EditImages.Commands.cs`
   - gesture preview state;
   - one-commit-on-release move/resize;
   - rotate/delete;
   - undo/redo;
   - EDITAR-only keyboard commands that do not steal keys from editable controls;
   - current logical geometry drives selection and overlay.
3. `src/SGPdf.App/MainWindow.EditImages.Hardening.cs`
   - discard confirmation;
   - guarded mode leave;
   - guarded window close.
4. `src/SGPdf.App/MainWindow.EditImages.cs`
   - interactive move body;
   - four resize handles;
   - rotate/delete controls;
   - overlay follows current/preview logical state.
5. `src/SGPdf.App/MainWindow.Reader.cs`
   - exactly three added lines: opening another PDF first honors the dirty EDITAR guard.

No native page-object mutation was introduced.

## 4. GREEN verification

Windows CI run:

`38016818948`

Exact head:

`a188c27c54adc4763f79693d61b1e662f2dcbad1`

Evidence:

- repository hygiene: PASS;
- locked restore: PASS;
- Release build: PASS;
- build warnings: 0;
- build errors: 0;
- tests: **594 / 594 PASS**;
- failed: 0;
- skipped: 0.

The full suite includes the existing LEER/FIRMAR/ORGANIZAR coverage plus the new Task-4 tests.

## 5. Scope audit

Compared with Task-3 checkpoint `ba67362e...`, Task 4 contains only:

- 3 new Task-4 test files;
- 3 new Task-4 production files;
- the Task-4 evolution of `MainWindow.EditImages.cs`;
- the narrow +3-line PDF-open guard in `MainWindow.Reader.cs`.

`main` remains unchanged at:

`31c0594758a83ec555d73ecdd7c597cdf8791fd7`

No PR was created and no merge was performed.

## 6. Safety / persistence invariant

All Task-4 edits remain managed logical state in `ImageEditWorkspace`.

- Source PDF remains untouched.
- Delete does not remove a native page object.
- Move/resize/rotate do not call native PDF mutation APIs.
- Discarding EDITAR throws away only the in-memory workspace.
- Password-opened and cryptographically signed PDFs remain blocked before mutable EDITAR workspace creation by Task 3.

## 7. Manual QA

Manual Windows UI QA: **NOT RUN** in this checkpoint. Automated Windows CI is green.

## 8. Resume point

Task 4 is closed. Do not reopen or reimplement it on resume.

The next approved `continua` starts **F6 Task 5** from the existing plan/spec, beginning with its RED tests before any new production code.

Task 5+ has not been started by this checkpoint.
