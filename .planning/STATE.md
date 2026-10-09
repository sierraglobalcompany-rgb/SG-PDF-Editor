---
gsd_state_version: '1.0'
status: implementing
progress:
  total_phases: 13
  completed_phases: 0
  total_plans: 13
  completed_plans: 0
  percent: 0
---

# Project State

## Project Reference
See `.planning/PROJECT.md`.

> Numeric GSD progress is not product-completion truth while automated and physical/private gates are tracked separately. GitHub exact heads/CI + this status are authoritative for executed work.

**Core value:** Resolver PDF + ZPL diario de forma rápida, privada, estable y offline.  
**Current focus:** Phase 5 — **F4 Lector Completo**, Tasks 1–5 closed automatically; Task 6 is the next user-approved execution gate.

## Previous Gate — F3.4 closed automatically

- Branch: `feat/f3-4-local-signature-library`.
- Closure head: `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b`.
- Closure push CI `37837613618`: PASS.
- PR CI `37837833517`: PASS.
- Release build: 0 warnings / 0 errors.
- Tests: 301 PASS / 0 FAIL / 0 SKIPPED.
- PR #22: draft/open/unmerged, stacked on F3.3.
- Manual real Windows QA: **NOT RUN**.

## Current Gate — F4 Task 6 pending user `continúa`

- Branch: `feat/f4-full-reader`.
- Base: exact F3.4 closure head `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b`.
- Formal design spec: `docs/superpowers/specs/2026-10-08-f4-full-reader-design.md` — **APPROVED by user on 2026-10-08**.
- TDD implementation plan: `docs/superpowers/plans/2026-10-08-f4-full-reader.md` — **APPROVED by user on 2026-10-08**.
- Plan companion: `.planning/phases/05-f4-full-reader/PLAN.md`.
- F4 PR: **NOT OPENED**; plan opens draft PR only at Task 9 closure.

### Task 1 — AUTO PASS

- Capability test commit: `adb4b0254fdc140ce21f503bd11297ae3cd6b5d3`.
- Pinned PDFium F4 export gate: PASS; no second PDF engine required.
- RED head: `ad676a933065eafd2e4ece816d026c8e651e26bb`.
- RED evidence: build 0 warnings / 0 errors; 10 expected failures for missing `PdfPageSize`, `GetPageSizes` and `ReaderLayoutPlanner`; 302 existing tests PASS.
- Functional GREEN head: `ce167be7ae0b7769c4fc9253c400bc5a9b59216a`.
- Exact-head CI `37843337925`: PASS.
- Release build: 0 warnings / 0 errors.
- Tests: 312 PASS / 0 FAIL / 0 SKIPPED.

### Task 2 — AUTO PASS

- RED head: `91b8d9b97a1d331207d4ecd478b4bce8baddad9d`.
- RED CI `37845233691`: expected failure after clean build; 16 new Task 2 tests failed while the prior 312 tests passed.
- Functional/verified head: `bfbc774066f91acafc960c8e7f77ba0df5f344f6`.
- Exact-head CI `37852149659`: PASS.
- Release build: 0 warnings / 0 errors.
- Tests: 328 PASS / 0 FAIL / 0 SKIPPED.
- Delivered: continuous virtualized `LEER` surface, sequential bounded visible+neighbor rendering, stale-generation protection, isolated per-page render errors, current-page navigation/zoom semantics, current-page bridge into the existing `FIRMAR` edit surface, and preserved ZPL behavior.

### Task 3 — AUTO PASS

- Final RED head: `c4315a10c05f0b21badd6307e47767f157a8cc87`.
- RED CI `37854879113`: expected build failure only because `ReaderThumbnailItem` did not yet exist; 0 warnings.
- Functional/verified head: `c968aa8e94f8b22820e3aa32ba1097d0e58516a2`.
- Exact-head CI `37855511813`: PASS.
- Release build: 0 warnings / 0 errors.
- Tests: 336 PASS / 0 FAIL / 0 SKIPPED.
- Delivered: lazy virtualized thumbnails at ~132 px width, left `Páginas` / `Marcadores` navigation, realized-range + one-neighbor retention, separate sequential thumbnail scheduler, stale-publication suppression, isolated thumbnail errors and synchronized navigation.

### Task 4 — AUTO PASS

- RED head: `a99287dc81623b742032ab234c6d1e37aa4d88b4`.
- RED CI `37856357399`: expected build failure with 9 missing-contract errors for typed PDF open errors/dialog; 0 warnings.
- Functional/verified head: `c27dfa738d946147bec5cb48c4e7ab2afde776bc`.
- Exact-head CI `37857787178`: PASS.
- Release build: 0 warnings / 0 errors.
- Tests: 346 PASS / 0 FAIL / 0 SKIPPED.
- Delivered: `PdfDocumentOpenError`, `PdfDocumentOpenException`, authoritative PDFium error-4 password classification, masked WPF password dialog, retry/cancel flow, wrong-password handling and candidate-first workspace preservation.
- Privacy: password exists only as transient attempt/dialog data; it is not persisted.

### Task 5 — AUTO PASS

- RED head: `b1e6f68922d52c92a76a93de23301cf3096872a9`.
- RED CI `37866817670`: expected build failure with 6 missing-contract errors for `PdfTextMatch` / `ReaderSearchCursor`; 0 warnings.
- Functional/verified head: `984545a8e8b6b098a55dad55b5059aa4c6020b3b`.
- Exact-head functional CI `37867206589`: PASS.
- Release build: 0 warnings / 0 errors.
- Tests: 368 PASS / 0 FAIL / 0 SKIPPED.
- Delivered: `PdfTextRect` / `PdfTextMatch`, PDFium text load/search/range/hit-test/rect APIs behind `NativeGate`, Unicode extraction, case-insensitive non-whole-word search defaults, pure page-by-page next/previous/wrap navigator, compact find bar, Ctrl+F / Enter / Shift+Enter / Escape behavior, active-match-only highlight and stale-generation rejection.
- Search strategy: on demand only; no OCR and no permanent whole-document index/result cache.
- KISS ruling: find UI is feature-local/dynamic in `MainWindow.ReaderSearch.cs`, matching the existing F4 reader/thumbnails pattern and avoiding a large `MainWindow.xaml` rewrite.
- Scope audit against Task 4 closure `c27dfa738d946147bec5cb48c4e7ab2afde776bc`: exactly 8 Task 5 code/test files changed; no package/lock, F3, ZPL, print, persistence/database/network or Task 6 selection/copy changes.
- Test correction during GREEN: a synthetic WinAnsi test originally mixed default search flags with PDFium `Ñ/ñ` case folding. Unicode extraction remained green; the flag-specific assertion was isolated to ASCII so it tests only case-insensitive + non-whole-word semantics.

### Frozen F4 direction

- PDFium only; required F4 exports verified in the pinned binary.
- `LEER` continuous virtualized; `FIRMAR` existing single-active-page edit surface.
- Full-resolution retention = visible pages + one neighbor before/after.
- Lazy thumbnails; no eager whole-document render.
- Search/copy uses PDFium text APIs; no OCR in F4.
- Search is page-by-page on demand; no permanent index.
- One-page text drag selection.
- Read-only cycle/depth/node-bounded bookmarks.
- Explicit internal PDF links + confirmed HTTP/HTTPS URI links only.
- Passwords never persisted.
- Recents = max 10 local paths + UTC timestamp; no startup path probing.
- PageUp/PageDown = one viewport-height scroll.
- No multi-document tabs in F4.

## F4 Plan Tasks

1. **DONE** — Native capability gate + pure continuous layout.
2. **DONE** — Continuous WPF surface + bounded rendering + FIRMAR boundary.
3. **DONE** — Lazy thumbnails + left navigation.
4. **DONE** — Password-protected PDFs.
5. **DONE** — PDFium text core + find navigation.
6. **NEXT** — One-page text selection + copy.
7. Bookmarks + safe explicit links.
8. Shortcuts + atomic recents + hardening.
9. Closure audit/docs + exact-head CI + draft stacked PR.

Project cadence: one task per user `continúa` unless the user explicitly changes it.

## Executed Chain

- A0 Foundation: PASS.
- A1 Development Intelligence: PASS.
- F0.1–F0.6 PDF Base: automated PASS.
- F1 Gate ZPL-A: synthetic PASS; private real corpus NOT RUN.
- F2.1–F2.6 ZPL Workspace: automated PASS.
- F3.1–F3.4 Visual Signature: automated PASS; corresponding manual QA remains NOT RUN.
- F4 Tasks 1–5: automated PASS.

## Parallel Acceptance Gates Still Open

- F0 physical Windows UI/print/offline smoke: **NOT RUN**.
- F1 private Mercado Libre corpus: **NOT RUN**.
- F2 private real-label corpus: **NOT RUN**.
- F2 thermal printer/ruler/scanner QA: **NOT RUN**.
- F3.1 real transparent-signature UX/save/open: **NOT RUN**.
- F3.2 real phone/scanner photo-quality QA: **NOT RUN**.
- F3.3 real mouse/touch/stylus QA: **NOT RUN**.
- F3.4 real save→restart→reuse/rename/delete/offline QA: **NOT RUN**.
- F4 continuous-reader real Windows scroll/zoom/performance UX QA: **NOT RUN**.
- F4 thumbnails real Windows scroll/click/large-document memory UX QA: **NOT RUN**.
- F4 real protected-PDF prompt/retry/cancel/printing QA: **NOT RUN**.
- F4 real text-search UX/Unicode/large-document performance QA: **NOT RUN**.

They remain separate from automated PASS and do not become accepted by inference.

## Runtime / Architecture Decisions

- Windows x64 + C# + .NET 10 + WPF.
- KISS solution remains `SGPdf.App + SGPdf.App.Tests`.
- PDFium primary; all native calls serialized by `PdfiumRuntime.NativeGate`.
- Labelize 1.7.0 only approved runtime ZPL renderer.
- PDFsharp remains labels-only in product runtime; test fixtures may use the existing test dependency graph.
- ZXing remains test/QA-only.
- F3 sources converge to `SignatureAsset` → `AddSignatureAsset(...)`.
- No merge to `main` without explicit user approval.

## Next Gate

```text
next user `continúa`
→ Task 6 RED for one-page text selection range + reader drag/overlay/copy behavior
→ confirm expected RED
→ minimal GREEN one-page selection + exact Unicode clipboard copy
→ full regression + exact-head CI
→ report evidence and stop before Task 7
```

## Continuity

- Cross-project audit: `docs/history/2026-10-08-CONTINUITY-AUDIT.md`.
- F3.4 closure: `docs/history/2026-10-08-F3.4.md`.
- F4 design: `docs/superpowers/specs/2026-10-08-f4-full-reader-design.md`.
- F4 plan: `docs/superpowers/plans/2026-10-08-f4-full-reader.md`.
