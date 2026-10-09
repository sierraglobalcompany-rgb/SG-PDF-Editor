---
gsd_state_version: '1.0'
status: f5-automated-closure
progress:
  total_phases: 13
  completed_phases: 6
  total_plans: 13
  completed_plans: 6
  percent: 46
---

# Project State

## Project Reference
See `.planning/PROJECT.md`.

> Numeric GSD progress is only an orientation. Automated, private and physical gates are tracked independently. GitHub exact heads/CI + this status are authoritative for executed work.

**Core value:** Resolver PDF + ZPL diario de forma rápida, privada, estable y offline.  
**Current focus:** **F5 Organizar automated closure PASS**, subject to the exact checkpoint head having both push and PR Windows CI green. Final exact-head run IDs are recorded in PR #24 conversation so the git head does not change merely to cite its own CI.

## Stable gates

- F3.4: branch `feat/f3-4-local-signature-library`, closure `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b`, draft PR #22; real Windows QA **NOT RUN**.
- F4: branch `feat/f4-full-reader`, closure `1d620f1b2b9717aab35671e18a9dc78f28a8afdd`, draft PR #23 open/unmerged; automated closure PASS; real Windows reader QA **NOT RUN**.
- F5: branch `feat/f5-organize`, stacked draft PR **#24**, base exact F4 closure; automated closure PASS when exact checkpoint push + PR CI are green; real Windows organize QA **NOT RUN**.

## F5 Organizar — automated closure

Evidence before the final self-contained checkpoint commit:

- Functional Task-10 head `d92cd0cc60fb7de9a3d9d595bdfd0468ee38ff8a`.
- CI `37990505245` attempt 2: PASS; Release build 0 warnings / 0 errors; 550/550 tests PASS.
- Reconciled-docs head `e9f96566c1d31a442da6373e1a648cc61a9e5bfc`.
- Push CI `37991289080`: PASS; build 0/0; 550/550 tests.
- PR #24 created draft/open/unmerged, exact base `feat/f4-full-reader` @ `1d620f1b...`, head `feat/f5-organize`.
- PR CI `37991479820`: PASS; build 0/0; 550/550 tests.
- Full F4→F5 compare at docs head: 83 commits ahead / 0 behind.
- No `.csproj` or lockfile changes in F5; no second PDF engine; no runtime network layer.

### F5 delivered behavior

- Plan-first ORGANIZAR workspace with reorder/move, relative 90° rotation, delete and duplicate.
- Candidate-first insert selected ranges from another PDF and merge/append by reuse of the same plan model.
- Extract selection and split by every-N or explicit ranges with deterministic names and collision preflight.
- Transactional PDFium writer: temp output → exact page imports → rotation → save → reopen → validate every page at 36 DPI → atomic publish → cleanup.
- Source fingerprints reject changed/missing sources before publication.
- Cryptographic signatures and password-opened sources hard-block structural output.
- Representative real-writer fixtures prove changed/lost forms, bookmarks, internal links, named destinations, tagged structure, page labels, attachments and original metadata; explicit warning confirmation is required.
- Lazy/bounded thumbnails; no eager full-document bitmap retention.
- Dirty ORGANIZAR guard protects open-PDF and LEER/FIRMAR/ZPL switches; successful Save As clears dirty state.
- Offline source scan and full regression suite remain green.

### F5 task status

0. NativeGate prerequisite — AUTO PASS.
1. PDFium capability + protected-session marker — AUTO PASS.
2. `OrganizePlan` + pure operations — AUTO PASS.
3. Structural preflight — AUTO PASS.
4. Transactional writer + output validator — AUTO PASS.
5. Organize surface + bounded thumbnails — AUTO PASS.
6. Selection/drag/reorder/save — AUTO PASS.
7. Insert + merge — AUTO PASS.
8. Extract + split — AUTO PASS.
9. Preservation hardening/matrix — AUTO PASS.
10. Hardening/audit/docs/draft PR closure — AUTO PASS once exact checkpoint push + PR CI are green.

`ORG-01..04`: **AUTO PASS**. Automated PASS does not imply manual/physical PASS.

## Manual/private/physical gates still open

- F0 real Windows UI/print/offline: **NOT RUN**.
- F1 private Mercado Libre corpus: **NOT RUN**.
- F2 private corpus + thermal/ruler/scanner: **NOT RUN**.
- F3 real Windows/photo/hardware/library QA: **NOT RUN**.
- F4 real Windows reader performance/UX/protected/search/selection/bookmarks/links/shortcuts/recents/offline: **NOT RUN**.
- F5 real Windows organize UX, drag/drop, large-document behavior, Save As, insert/merge/extract/split, warning/block dialogs and offline smoke: **NOT RUN**.

## Runtime / architecture frozen through F5

- Windows x64 / C# / .NET 10 / WPF.
- PDFium only PDF engine; all native calls behind `PdfiumRuntime.NativeGate`.
- Labelize 1.7.0 only runtime ZPL renderer.
- PDFsharp labels/synthetic fixtures only; ZXing test/QA-only.
- F3 sources converge to `SignatureAsset` → `AddSignatureAsset(...)`.
- F5 is plan-first and source-safe; structural output is Save As only.
- No merge to `main` without explicit user approval.

## Next Gate

```text
verify final Task-10 checkpoint exact head:
  push Windows CI PASS
  PR #24 Windows CI PASS
  PR draft/open/unmerged
  main unchanged
→ STOP F5

next user continuation
→ F6 Images design/spec only
→ no F6 implementation before design/plan approval
```

## Continuity

- F5 design: `docs/superpowers/specs/2026-10-09-f5-organize-design.md`.
- F5 TDD plan: `docs/superpowers/plans/2026-10-09-f5-organize.md`.
- F5 preservation matrix: `docs/history/2026-10-09-F5-preservation-matrix.md`.
- F5 closure history: `docs/history/2026-10-09-F5.md`.
- Task-10 checkpoint: `docs/history/2026-10-09-F5-TASK10-CHECKPOINT.md`.
