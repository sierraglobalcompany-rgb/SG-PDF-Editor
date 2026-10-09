# F5 Task 2 — WIP Checkpoint

Date: 2026-10-09
Branch: `feat/f5-organize`
Status: **WIP — GREEN IMPLEMENTATION COMMITTED BUT NOT YET VERIFIED**

## Exact recovery point

- Previous completed checkpoint: Task 1 head `fd98cfa3096385e5f7c8864b82568cddcc677bef`.
- Task 2 RED commit: `8f06817cea8780ab7df4a09974c836e6967b8fef`.
- RED CI run: `37955886508`.
- RED evidence: build succeeded with 0 warnings / 0 errors; total 442 tests; 426 existing tests passed; 16 new Task 2 tests failed because `SGPdf.App.Features.Organize.*` types did not yet exist. This is the expected RED reason.
- WIP GREEN implementation commit: `330ad90322f08a249cf28388fd9949a91375f726`.
- The WIP GREEN commit has **not yet been verified**. Do not call Task 2 complete until exact-head CI passes.

## Task 2 scope

Task 2 is pure plan/state logic only. No PDFium calls, WPF UI, new dependencies, writer, preflight or Task 3 work belongs here.

### Tests already committed

- `tests/SGPdf.App.Tests/OrganizePlanTests.cs`
- `tests/SGPdf.App.Tests/OrganizePlanOperationsTests.cs`

Coverage includes:

- source fingerprint capture and changed/missing-file detection;
- normalized absolute source path;
- factory page ordering and unique logical IDs;
- rejection of unknown source, out-of-range source page and duplicate item ID;
- rotation delta normalization modulo 4;
- `MoveSelection_NonContiguousForwardBackward_PreservesRelativeOrderWithoutLoss`;
- drop inside selected span => no-op;
- rotation composition modulo 4;
- delete subset + block delete-all;
- duplicate with fresh logical IDs after selected block;
- insert source pages at beginning/middle/end preserving requested order;
- invalid IDs and insertion/source-page indices rejected.

### WIP production files committed

- `src/SGPdf.App/Features/Organize/OrganizeSource.cs`
- `src/SGPdf.App/Features/Organize/OrganizePage.cs`
- `src/SGPdf.App/Features/Organize/OrganizePlan.cs`
- `src/SGPdf.App/Features/Organize/OrganizePlanOperations.cs`

## Important rulings

1. `OrganizePlan` remains internal and tests access the internal contract via reflection because the project currently has no `InternalsVisibleTo` convention. Do not widen the API to public solely for tests.
2. `MoveSelection` uses insertion-slot semantics against the current/original page order. Selected pages are always moved in their current visual order, never in caller collection order.
3. If insertion falls within the selected span, the move is a no-op.
4. `OrganizeSourceFingerprint` is intentionally cheap: file length + last-write UTC ticks. It is change detection, not cryptographic identity.
5. Task 2 operations must remain pure; filesystem access exists only in `OrganizeSource.Capture` / `MatchesCurrentFile`.

## Exact next step

1. Inspect CI for exact head `330ad90322f08a249cf28388fd9949a91375f726` (or the documentation checkpoint head that follows this file).
2. If Task 2 tests fail, use the failure output to make the smallest tested fix; do not change tests merely to fit implementation.
3. Run full suite and require build 0 warnings / 0 errors plus all tests PASS.
4. Only then create the final Task 2 commit/checkpoint and mark Task 2 complete.
5. Do **not** start Task 3 until Task 2 exact-head CI is green.

## Safety / repository state

- No merge to `main` has been performed.
- `main` was last verified untouched at `31c0594758a83ec555d73ecdd7c597cdf8791fd7` before Task 2 began.
- No new runtime dependency has been added.
- No Task 3 implementation has started.
