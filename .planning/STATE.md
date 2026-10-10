---
gsd_state_version: '1.0'
status: f7-automated-closure
progress:
  total_phases: 13
  completed_phases: 8
  total_plans: 13
  completed_plans: 8
  percent: 62
---

# Project State

## Project Reference
See `.planning/PROJECT.md`.

> Numeric GSD progress is orientation only. Automated, private and physical gates are tracked independently. GitHub exact heads/CI + this status are authoritative for executed work.

**Core value:** Resolver PDF + ZPL diario de forma rápida, privada, estable y offline.  
**Current focus:** **F7 Texto V1 automated closure finalization** — implementation through Task 13 is GREEN; Task 14 is reconciling docs, final exact-head CI and the stacked draft PR. Manual Windows QA remains **NOT RUN**.

## Stable gates

- F3.4: `feat/f3-4-local-signature-library`, closure `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b`, draft PR #22; real Windows QA **NOT RUN**.
- F4: `feat/f4-full-reader`, closure `1d620f1b2b9717aab35671e18a9dc78f28a8afdd`, draft PR #23; automated closure PASS; real Windows reader QA **NOT RUN**.
- F5: `feat/f5-organize`, closure `327d7064c14131e603e3bce6947b593a10f46363`, stacked draft PR #24; automated closure PASS; real Windows organize QA **NOT RUN**.
- F6: `feat/f6-images`, closure `c6d762efca01d50bfe3932d1f05617190a464fc6`, stacked draft PR #25 open/unmerged; automated closure PASS; real Windows image-edit QA **NOT RUN**.
- F7: `feat/f7-text-v1`, Task-13 checkpoint `b31a75cf99207e2e6ac9072b50c5a1ac5fa32d05`; automated implementation through hardening PASS; Task 14 docs/PR finalization in progress.

## F7 Text V1 — automated closure evidence

Fresh exact-head verification on the Task-13 checkpoint `b31a75cf99207e2e6ac9072b50c5a1ac5fa32d05`:

- Windows workflow `38089770428`: PASS;
- locked restore: PASS;
- Release build: **0 warnings / 0 errors**;
- tests: **766 passed / 0 failed / 0 skipped**.

Full F6→F7 pre-docs audit:

- base `c6d762efca01d50bfe3932d1f05617190a464fc6`;
- pre-docs head `b31a75cf99207e2e6ac9072b50c5a1ac5fa32d05`;
- **100 commits ahead / 0 behind**;
- no new NuGet package and no package-lockfile change;
- the only `.csproj` change copies the pinned fallback TTF into build/publish output;
- PDFium remains the only PDF editor engine;
- no runtime network/cloud/account/API-key/service layer;
- only DejaVu Sans 2.37 was added as a runtime asset, with provenance/license/hash recorded in `third_party/manifest.json`.

### F7 delivered behavior

- Discover top-level real PDF text page objects on the active page only, preserving Unicode, matrix, bounds/quad, font, size, fill and render mode in managed snapshots.
- Deterministic mixed hit-testing across text + image objects by topmost `PageObjectIndex`.
- Conservative `TextEditPolicy` + candidate-first `TextEditWorkspace`; non-Fill render modes remain selectable/read-only.
- `OriginalFont` route for safe edits and pinned DejaVu Sans 2.37 fallback for new code points or size changes.
- Fallback materialization uses `FPDFText_LoadCidType2Font` with explicit `ToUnicode` + `CIDToGIDMap`; `FPDFText_LoadFont` remains rejected for product fallback.
- Combined image + text writer resolves objects before mutation, generates page content once per touched page and publishes transactionally through temp → reopen/validate → atomic publication.
- Combined validator checks exact Unicode, TEXT type, matrix, size, fill, font route and rendered edited pages before publication.
- F7 re-measured preservation with the real combined writer: forms, bookmarks, named destinations/internal links, tagged structure, page labels, attachments, metadata and page rotation are `ProvenPreserved / Info` on the representative corpus.
- Text UI lives inside the single EDITAR shell, shares selection, dirty guards and Save As with images, and does not add another mode/guard chain.
- Hardening covers active-page-only discovery, zero durable native handles, >2 MiB fallback-font rejection before read/typeface open, cancellation/temp cleanup and dirty-baseline retention after stale/publication failure.

### F7 requirement status

- `TEXT-01` detect/select text objects — **AUTO PASS**.
- `TEXT-02` conservative in-place text editing — **AUTO PASS**.
- `TEXT-03` redistributable TTF fallback — **AUTO PASS**.
- `TEXT-04` basic properties + save/reopen validation — **AUTO PASS**.

Automated PASS does not imply real/manual PASS.

## Manual/private/physical gates still open

- F0 real Windows UI/print/offline: **NOT RUN**.
- F1 private Mercado Libre corpus: **NOT RUN**.
- F2 private corpus + thermal/ruler/scanner: **NOT RUN**.
- F3 real Windows/photo/hardware/library QA: **NOT RUN**.
- F4 real Windows reader QA: **NOT RUN**.
- F5 real Windows organize QA: **NOT RUN**.
- F6 real Windows image-edit UX: **NOT RUN**.
- F7 real Windows text-edit UX: selection at zoom/rotation/overlap, read-only modes, typing/Apply, color/size controls, OriginalFont/fallback rendering, Save As dialogs, mixed image+text edits, heavy PDF, network-disabled and cross-mode smoke — **NOT RUN**.

## Runtime / architecture frozen through F7

- Windows x64 / C# / .NET 10 / WPF.
- PDFium remains the only PDF editor engine; native calls use `PdfiumRuntime.NativeGate`.
- Pinned PDFium package remains `bblanchon.PDFium.Win32 156.0.8076`.
- PDFsharp remains labels/synthetic-fixture support, not a general editor engine.
- Labelize 1.7.0 remains the ZPL runtime renderer.
- DejaVu Sans 2.37 is the pinned offline text fallback asset; runtime never downloads it.
- Save As protects originals; cryptographically signed/password-opened sources are blocked for EDITAR output.
- No merge to `main` without explicit user approval.

## Next Gate

```text
Task 14 docs/checkpoint commit
→ exact-head push Windows CI PASS
→ create stacked draft PR: feat/f6-images → feat/f7-text-v1
→ PR CI PASS on expected head/merge ref
→ verify PR draft/open/unmerged + main unchanged
→ STOP F7

next user continuation
→ F8 Comentarios design/spec only
→ no F8 implementation in the F7 closure gate
```

## Continuity

- F7 design: `docs/superpowers/specs/2026-10-10-f7-text-v1-design.md`.
- F7 TDD plan: `docs/superpowers/plans/2026-10-10-f7-text-v1.md`.
- F7 phase plan: `.planning/phases/08-f7-text/PLAN.md`.
- F7 preservation matrix: `docs/history/2026-10-10-F7-preservation-matrix.md`.
- F7 closure history: `docs/history/2026-10-10-F7.md`.
- Task-13 checkpoint: `docs/history/2026-10-10-F7-TASK13-CHECKPOINT.md`.
- Task-14 checkpoint: `docs/history/2026-10-10-F7-TASK14-CHECKPOINT.md`.
