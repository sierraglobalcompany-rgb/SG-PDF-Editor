# F5 Task 10 — Final Automated Closure Checkpoint

**Date:** 2026-10-09  
**Branch:** `feat/f5-organize`  
**Stacked base:** `feat/f4-full-reader` @ `1d620f1b2b9717aab35671e18a9dc78f28a8afdd`  
**PR:** #24 — `F5 — Organizar PDF` — draft/open/unmerged  
**Manual Windows QA:** NOT RUN

## Purpose

Durable checkpoint for Task 10 only: dirty-state hardening, full F4→F5 audit, planning/history reconciliation and stacked draft-PR closure. This checkpoint does not merge anything and does not start F6.

## RED — dirty organize guard

Added `MainWindowOrganizeHardeningTests` for four missing behaviors:

1. dirty plan + open another PDF + cancel preserves current session/plan;
2. dirty plan + confirmed discard allows replacement PDF to open and clears organize state;
3. dirty plan + cancel mode switch preserves ORGANIZAR state;
4. successful Save As clears dirty state and clean leave does not prompt.

RED commit: `dbb5dfdda097f468119155b6bb528e8d407201c8`.

First run had the four intended missing-contract failures plus one unrelated intermittent thumbnail failure. The exact same head was rerun without code changes; valid RED was:

- Release build: 0 warnings / 0 errors;
- 550 total;
- 546 passed;
- 4 failed, exactly the four new dirty-guard contracts.

## GREEN — targeted production change only

Commits:

- `39a772caf54956dd726a38cb3cc44a27fe12d866` — add unsaved-plan guard;
- `386df4f3ab86455b184cd9306c417fc3456ac92a` — track dirty plan lifecycle;
- `d92cd0cc60fb7de9a3d9d595bdfd0468ee38ff8a` — guard PDF open against dirty plan.

Behavior:

- plan mutation marks `_organizePlanDirty`;
- successful Save As clears dirty only after writer returns successfully;
- opening another PDF uses the same organize leave guard before candidate work starts;
- LEER/FIRMAR preview switch is guarded;
- ZPL switch is guarded;
- cancel preserves session/plan;
- leave is blocked while organize materialization is active.

No writer, PDF engine, dependency or thumbnail implementation change was required for this gap.

## Functional GREEN evidence

Exact functional head: `d92cd0cc60fb7de9a3d9d595bdfd0468ee38ff8a`.

Windows CI `37990505245`, attempt 2:

- hygiene PASS;
- pinned Labelize PASS;
- locked restore PASS;
- Release build PASS — 0 warnings / 0 errors;
- tests PASS — 550/550, 0 failed, 0 skipped.

Attempt 1 on the same SHA had one existing secondary-thumbnail `Assert.NotNull` failure. Exact-SHA rerun passed all 550 with no code change, so the event is recorded as test flakiness rather than product regression.

## Hardening evidence already covered by the full suite

Task 10 audited existing coverage before adding code:

- stale/missing/changed source preserves destination and cleans temp;
- native save/validation failure preserves destination and cleans temp;
- signed/password preflight blocks before output publication;
- warning confirmation is mandatory;
- batch collisions are checked before first write;
- batch cancellation stops before the next writer;
- split stops on first failure and retains previously published outputs;
- large organize thumbnail retention stays bounded to realized + neighbor items;
- stale thumbnail publication is rejected;
- per-tile thumbnail failure is isolated;
- offline test scans application source for network primitives/URLs and runtime packages.

No redundant subsystem was added for already-covered behavior.

## Full F4 → F5 audit

Base: `1d620f1b2b9717aab35671e18a9dc78f28a8afdd`.

At reconciled-docs head `e9f96566c1d31a442da6373e1a648cc61a9e5bfc`:

- 83 commits ahead / 0 behind;
- no `.csproj` or `packages.lock.json` changes;
- no second PDF engine;
- no runtime network/service layer;
- no `Features/Sign/` implementation change;
- no `Features/Labels/` implementation change;
- `MainWindow.Reader.cs` differs from F4 by the three-line organize dirty guard at open-PDF entry;
- Task 0 Navigation diff is the required `SemaphoreSlim` NativeGate correction;
- PDF session/native additions are narrow organize/protected-session probes;
- no generic PDF object graph, DI/MVVM framework, database, cloud, autosave/recovery or plugin subsystem.

No critical/high closure defect was identified.

## Preservation closure

`docs/history/2026-10-09-F5-preservation-matrix.md` is authoritative.

Hard blocks:

- cryptographic signatures;
- sources opened with a password.

Representative real-writer evidence = `ProvenChangedOrLost` + Warning + explicit confirmation:

- forms;
- bookmarks;
- internal links;
- named destinations;
- tagged structure;
- page labels;
- attachments;
- original metadata values.

No structure lacking evidence is claimed preserved.

## Documentation closure

Commit `e9f96566c1d31a442da6373e1a648cc61a9e5bfc` — `docs(organize): prepare F5 automated closure` — reconciled:

- `.planning/STATE.md`;
- `.planning/ROADMAP.md`;
- `.planning/REQUIREMENTS.md`;
- `.planning/phases/06-f5-organize/PLAN.md`;
- `docs/history/2026-10-09-F5.md`;
- F5 design spec status/closure gate.

Push CI `37991289080`: PASS, build 0/0, tests 550/550.

## Draft PR closure

PR #24 created as required:

- title: `F5 — Organizar PDF`;
- base: `feat/f4-full-reader`;
- base SHA: `1d620f1b2b9717aab35671e18a9dc78f28a8afdd`;
- head: `feat/f5-organize`;
- draft: true;
- state: open;
- merged: false.

PR CI on docs head: `37991479820` — PASS, Release build 0 warnings / 0 errors, tests 550/550.

## Final exact-head rule

The commit containing this checkpoint is the final Task-10/F5 automated-closure candidate. It must pass both:

1. branch push Windows CI;
2. PR #24 `pull_request` Windows CI.

Those run IDs are recorded as a top-level PR #24 conversation comment **after** they pass, rather than creating another git commit merely to cite the CI of this commit.

Only with both final checks green may F5 be reported **AUTOMATED CLOSURE PASS**.

## Manual QA remains open

Still **NOT RUN**:

- real Windows large-document organize performance/UX;
- real Ctrl/Shift selection + drag/drop;
- real Save As chooser/replacement/error flow;
- representative real insert/merge/extract/split PDFs;
- signed/password/preservation warning dialogs;
- network-disabled physical smoke;
- end-to-end real LEER/FIRMAR/ZPL regression.

Earlier F0–F4 manual/private/physical gates remain independent and open where previously documented.

## Stop boundary

After final exact-head push + PR CI are green and PR/main state are reverified:

- STOP F5;
- do not merge PR #24;
- do not merge `main`;
- next user continuation may begin **F6 Images design/spec only**.
