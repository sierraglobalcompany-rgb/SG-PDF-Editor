# F13 Task9 / T9.1 — R1B per-document pending state GREEN checkpoint

Date: 2026-10-17

## Canonical branch

`feat/f13-task9-t9-1-document-shell-realignment-20261017`

No merge to `main` is approved. `main` remains historical/read-only. T9.2 remains NOT STARTED. F14–F18 remain frozen.

## R1B technical GREEN anchor

Commit:
`26d026b2fbe1f35754bcf7cef660d2fdaa7e5d34`

Exact-head GitHub Actions:
- run `39040326178`;
- job `117418290462`;
- checkout exact SHA `26d026b2fbe1f35754bcf7cef660d2fdaa7e5d34`;
- repository hygiene PASS;
- restore locked dependencies PASS;
- Build PASS — 0 warnings / 0 errors;
- startup smoke PASS — process survived 30 seconds;
- tests PASS — 1274 passed / 0 failed / 4 skipped / 1278 total;
- overall SUCCESS.

The 4 skips are intentional future T9.1 contracts reserved for R2–R5: visible document tabs/+ button, official document-rail artwork, collapsible Pages/Bookmarks drawer and PDF workspace context menu.

## What R1B now owns

R1B keeps document switching KISS: there is no DI container, event bus, document framework or generalized ModeManager.

Each `PdfDocumentWorkspace` owns one `PdfDocumentPendingState`. That model stores the existing feature owners/snapshots needed to preserve pending per-document work:
- Edit: image/text/redaction workspaces plus selections/active objects;
- Sign: `SignatureEditState`;
- Comments: `CommentWorkspace`, active tool/selection and loaded/source metadata;
- Organize: `OrganizePlan`, dirty flag and source page sizes;
- AcroForm: original/current string values, never WPF controls.

`MainWindow.DocumentPendingState.cs` captures the active document state before switching, detaches global WPF presentation state without disposing the model, then restores the target document model and reconstructs the UI.

Closing a document uses an explicit dirty-close guard before disposal. Switching documents does not itself require save/discard.

## Route-drift defect found during closure audit

After the first R1B GREEN implementation, a second audit found old single-document guards still attached to user routes for opening another PDF. They could prompt for discard before the multi-document owner had a chance to capture pending state.

Classification:
`LEGACY_SINGLE_DOCUMENT_GUARD_DRIFT`

Root owners:
- Ctrl+O shortcut pre-guards;
- Edit Open-PDF class handler;
- Comment Open-PDF class handler;
- obsolete comment shortcut/recent-file integration guards.

Correction followed anti-noise/anti-patch:
- removed pre-discard logic from Ctrl+O;
- Open PDF no longer triggers Edit/Comment discard guards;
- ZPL, mode-switch and window-close guards remain intact;
- deleted obsolete `MainWindow.Comments.Integration.cs` instead of adding another workaround;
- realigned stale shortcut tests to multi-document semantics.

Focused RED evidence before correction:
- RED commit `db94045d9710fb67d827b79b260090c4eb3a5aa5`;
- run `39039423862`, job `117415694668`;
- Build 0/0 and startup smoke PASS;
- exactly 3 expected route-contract failures, 1271 passed, 4 skipped.

An old signature Ctrl+O test then exposed test-contract drift at `7b6cecacdbcefe7f87bbd99e42892bbff09ab50c`; product behavior was not reverted. The stale test was realigned in `26d026b2...`, after which the full exact-head suite passed.

## Audit delta from pre-R1B authority

Compare base `a6b0a1066bb1187845b5a151321442db9c5160f6` → R1B GREEN `26d026b2fbe1f35754bcf7cef660d2fdaa7e5d34` is ahead-only and contains the bounded pending-state implementation/tests plus the route-drift cleanup. No merge/rebase/force-push is part of this checkpoint.

## Current routing after this checkpoint

- R1 reader multi-document core: GREEN/CLOSED.
- R1B per-document pending state + dirty close guard: GREEN/CLOSED technically.
- R2 top shell + visible document tabs: NEXT / NOT STARTED.
- R3 document rail: NOT STARTED.
- R4 Pages/Bookmarks drawer: NOT STARTED.
- R5 context menus: NOT STARTED.
- R6 image-selection root cause: NOT STARTED.
- R7 final visual/functional QA: NOT STARTED.
- T9.2: NOT STARTED.

R2 may now begin only on explicit continuation of T9.1 work. Do not start T9.2 automatically and do not merge to `main` without explicit user approval.

## Recovery instruction

If the chat/context is lost, start with:
1. `.planning/STATE.md`;
2. this checkpoint;
3. live branch HEAD + exact-head CI;
4. `.planning/ROADMAP.md`;
5. `AGENTS.md` and `docs/MASTER_CONTEXT.md` only as needed.

If live GitHub is newer than this checkpoint, live exact HEAD/CI and `.planning/STATE.md` take precedence.
