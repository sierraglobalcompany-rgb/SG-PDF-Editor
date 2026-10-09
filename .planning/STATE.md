---
gsd_state_version: '1.0'
status: closing-f5
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
**Current focus:** **F5 Organizar automated closure**. Functional hardening is green; closure docs + stacked draft PR are the remaining Task 10 steps.

## Stable previous gates

- F3.4: branch `feat/f3-4-local-signature-library`, closure `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b`, draft PR #22, 301 tests at closure; real Windows QA **NOT RUN**.
- F4: branch `feat/f4-full-reader`, closure `1d620f1b2b9717aab35671e18a9dc78f28a8afdd`, draft PR #23 open/unmerged; automated closure PASS; real Windows reader QA **NOT RUN**.

## F5 Organizar — automated closure candidate

- Branch: `feat/f5-organize`.
- Exact base: F4 closure `1d620f1b2b9717aab35671e18a9dc78f28a8afdd`.
- Functional Task-10 head before closure docs: `d92cd0cc60fb7de9a3d9d595bdfd0468ee38ff8a`.
- Windows CI `37990505245`, attempt 2: **PASS**; locked restore, Release build **0 warnings / 0 errors**, **550 PASS / 0 FAIL / 0 SKIPPED**.
- Attempt 1 had one intermittent existing secondary-thumbnail assertion; rerunning the exact same SHA passed 550/550 without code changes, so it is recorded as test flakiness rather than a product regression.
- Full F4→F5 compare at the functional head: **82 commits ahead / 0 behind**.
- No `.csproj` or lockfile changes in F5; no second PDF engine; no network runtime; no `Features/Sign` or `Features/Labels` implementation changes.

### F5 delivered behavior

- Plan-first ORGANIZAR workspace with reorder/move, relative 90° rotation, delete and duplicate.
- Insert selected ranges from another PDF and merge/append by reuse of the same plan model.
- Extract selection and split by every-N or explicit ranges with deterministic names and collision preflight.
- Transactional PDFium writer: temp output → exact page imports → rotation → save → reopen → validate every page at 36 DPI → atomic publish → cleanup.
- Source fingerprints reject changed/missing source before publication.
- Cryptographic signatures and password-opened sources are hard blocks.
- Preservation findings are evidence based. Representative fixtures prove the current writer changes/loses forms, bookmarks, internal links, named destinations, tagged structure, page labels, attachments and original metadata; these require explicit warning confirmation.
- Lazy/bounded thumbnails; no full-document eager bitmap retention.
- Dirty ORGANIZAR guard protects open-PDF, LEER/FIRMAR switch and ZPL switch; successful Save As clears dirty state.
- Offline source scan and existing regression suite remain green.

### F5 task status

0. NativeGate prerequisite — **AUTO PASS**.
1. PDFium capability + protected-session marker — **AUTO PASS**.
2. `OrganizePlan` + pure operations — **AUTO PASS**.
3. Structural preflight — **AUTO PASS**.
4. Transactional writer + output validator — **AUTO PASS**.
5. Organize surface + bounded thumbnails — **AUTO PASS**.
6. Selection/drag/reorder/save — **AUTO PASS**.
7. Insert + merge — **AUTO PASS**.
8. Extract + split — **AUTO PASS**.
9. Preservation hardening/matrix — **AUTO PASS**.
10. Hardening/audit/docs/draft PR — **IN CLOSURE**; dirty guard GREEN, docs/PR exact-head checks pending.

`ORG-01..04` are reconciled as **AUTO PASS** in `.planning/REQUIREMENTS.md`. Automated PASS does not imply physical/manual PASS.

## Manual/private/physical gates still open

- F0 real Windows UI/print/offline: **NOT RUN**.
- F1 private Mercado Libre corpus: **NOT RUN**.
- F2 private corpus + thermal/ruler/scanner: **NOT RUN**.
- F3.1–F3.4 real Windows/photo/hardware/library QA: **NOT RUN**.
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
finish F5 closure docs
→ push exact-head Windows CI PASS
→ create stacked draft PR base feat/f4-full-reader / head feat/f5-organize
→ PR CI PASS on exact head
→ checkpoint Task 10 + final exact-head push/PR CI PASS
→ verify PR draft/open/unmerged + main unchanged
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
