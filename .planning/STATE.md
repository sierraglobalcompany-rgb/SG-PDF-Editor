---
gsd_state_version: '1.0'
status: executing
progress:
  total_phases: 13
  completed_phases: 0
  total_plans: 7
  completed_plans: 0
  percent: 0
---

# Project State

## Project Reference

See: `.planning/PROJECT.md`.

**Core value:** Resolver PDF + ZPL diario de forma rápida, privada, estable y offline.  
**Current focus:** Phase 3 — F2 Etiquetas; F2.1, F2.2 and F2.3 automated closed. Next slice: **F2.4 Layout + PDF composition**, pending its own design/approval. F1 private-corpus acceptance remains open.

## Current Position

F2.3 — **Quantity UX + Physical Dimensions/dpmm: automated PASS**.  
Branch: `feat/f2-3-quantity-dimensions`.  
PR: #15 draft, base `feat/f2-2-labelize-preview`, no merge.  
Functional GREEN head before closure docs: `7fba8883f5af57ec1ac8a69bd5ba8bdba379822c` → push Windows CI `37659069251` PASS; Release build 0 warnings / 0 errors; 83 PASS / 0 FAIL / 0 SKIPPED.  
Closure docs are the final branch mutation; exact-head CI after that mutation is recorded in PR #15.  
Last activity: 2026-10-07 — quantity modes, physical size presets/custom dimensions and 6/8/12/24 dpmm rerender controls completed without entering layout/PDF/print scope.

Parallel acceptance gates still open:

- F0 PDF Base: F0.1–F0.6 automated PASS; physical Windows UI/print/offline smoke **NOT RUN**.
- F1 Gate ZPL-A: synthetic comparison complete and engine selection approved as **Labelize 1.7.0**; private real Mercado Libre ZPL corpus still required for formal F1 close.
- F2 private/manual acceptance: real labels, interactive desktop behavior, exact-size printing/scanning and privacy residue checks remain **NOT RUN**.

## Development Tooling

- GSD Core pin: `1.15.0`, project-scoped; installer-owned `.codex/` ignored.
- Graphify pin: `0.9.77`, project-scoped; `graphify-out/` ignored/regenerable.
- Graph corpus: `src/` + `tests/` via `.graphifyignore`.
- Graphify auto-update: OFF.
- Setup reproducible: `tools/setup-dev.ps1`.
- GSD/Graphify are dev-only and never runtime/build dependencies of SG PDF Editor.

## Accumulated Decisions

### Runtime / PDF

- Windows x64 + C# + .NET 10 LTS + WPF remain frozen.
- KISS solution: App + Tests only; no preventive Core/Infrastructure/Domain/CQRS/MediatR layers.
- PDFium is the primary PDF engine; native calls globally serialized with `SemaphoreSlim(1,1)`.
- F0.1 renders PDFium BGRA buffers and WPF creates `BitmapSource` outside native work.
- F0.2 uses immutable `PageNavigationState`; navigation commits only after successful render.
- F0.3 uses `PdfZoomState` (`Manual`, `FitPage`, `FitWidth`); `100% = 96 DPI`; PDFium rerenders rather than stretching a bitmap.
- F0.4 uses `PdfRenderScheduler` latest-request-wins; resize-fit debounce 150 ms; no general worker/queue architecture.
- F0.5 uses WPF `PrintDialog` + `DocumentPaginator` + Windows spooler; print raster starts at 200 DPI pending physical measurement.
- F0.6 completed direct go-to-page and offline guards.

### ZPL / Labels

- Gate ZPL-A selected **Labelize 1.7.0** over BinaryKits for F2.
- Labelize is a pinned bundled local `labelize.exe` sidecar; no local HTTP server and no custom Rust ABI.
- BinaryKits is historical Gate evidence only; no dual-engine runtime abstraction or automatic fallback.
- Labelize release archive SHA-256: `cdd4030b0d1a8bad69b93f49866c8dcc5314af8975bb16a76991fe32f92dd21d`.
- Development/CI may stage the pinned binary explicitly; product runtime must never download it.
- `^PQ` is quantity metadata, not a render multiplier. It is removed from normalized render source; absent/zero defaults to 1; last command wins per printable block; max supported quantity 99,999,999.
- `TotalQuantityFromFile` is `long`.
- `^DF` stored-format definition blocks remain support context, not user-visible designs; normalized document-level source preserves `^DF/^XF` order.
- `^PQ` inside a `^DF` definition is rejected rather than guessed.
- F2.2 renders normalized ZPL once, maps Labelize outputs deterministically and fails on output-count mismatch.
- Labelize render temp data is request-scoped; PNG bytes are in managed memory before cleanup; WPF uses `BitmapImage.CacheOption=OnLoad`.
- Label preview navigation is Previous / `Etiqueta n de N` / Next; each design appears once regardless of `^PQ`.
- Opening ZPL is candidate-first; render must succeed before replacing a valid workspace. Window close cancels active Labelize work.
- F2.3 quantity modes: `Del archivo`, `Una de cada`, `Personalizada por diseño`; quantity changes **never rerender** Labelize.
- F2.3 physical presets: 102×152, 100×150, 100×100 mm plus custom width/height; resolutions 6/8/12/24 dpmm.
- Physical size/dpmm changes rerender the normalized ZPL with Labelize; the old PNG is never stretched.
- Applied physical settings commit only after successful rerender; failure/cancellation keeps the last valid preview/settings and selected design.
- F2.3 added no package/dependency; the small `_renderZplAsync` delegate is a local test seam, not a renderer abstraction layer.
- F2.4 is next. Do not add PDFsharp preemptively; first design the composition path and prove the smallest required dependency.
- Real Mercado Libre/customer ZPL stays private/local and is never committed, attached to CI artifacts or indexed by Graphify.
- No merge to `main` without explicit user approval.

## Evidence

### F0

- F0.1 final CI `37538180207`; head `a4edc2e6bc101654a4d99a06c2df5aeb2c45739e`; PR #7 draft.
- F0.2 final CI `37541684119`; head `119a7af1482e5e2e92b3d97fc709b596fca58f88`; PR #8 draft.
- F0.3 final CI `37569477256`; head `cb3841163b8f79ef5fb8ad58f0fd920ecbf84e30`; PR #9 draft.
- F0.4 final CI `37571999577`; head `1b948dd2452acdf830cf054adb085889cdb51a9d`; PR #10 draft.
- F0.5 final CI `37573058825`; head `1b2355d7611db9101d74d7e42babcf9d11c9ca94`; PR #11 draft.
- F0.6 final CI `37575306423`; head `6ddf9891705bda905b49d9a1a0bd9fd765ec916c`; PR #12 draft.

### F1 — Gate ZPL-A synthetic probe

- Throwaway branch: `spike/f1-zpl-gate-a`.
- Verified head `2df2f374ae6124729389640425dc8334d0647f9f`; workflow `37577735748` PASS.
- Both engines rendered/decoded the synthetic corpus; Labelize selected by fidelity/performance/packaging/licensing evidence.
- Formal F1 acceptance still waits for the private real Mercado Libre corpus.

### F2.1 — ZPL Parse + Open

- Automated implementation/history PASS on branch `feat/f2-1-zpl-parse-open` / PR #13 draft.
- History-complete head `21fc4e515677b4870fe6319b246d7329bda39ce1` → `37584180581` PASS.

### F2.2 — Labelize Adapter + Preview

- Tasks 1–3 consolidated GREEN head `8d6da1e6cd2099a812adc54c8e6c90d409e01be0` → `37641938607` PASS; 72 tests.
- Task 4 RED `691163513af05c6182cc0a65c7356eb9df498ddc` → `37642332662` expected failure.
- Task 4 GREEN `5b8829b65e082ad185eb9f97ed145962b70411d1` → `37654759373` PASS; 73 tests.
- Final F2.2 head `9af600ce19812adb67a11718a47734c346b51bfc` → push CI `37656292018` PASS and PR CI `37656303399` PASS; build 0/0; 73 tests.

### F2.3 — Quantity UX + Physical Dimensions/dpmm

- Task 1 RED `dc1630b0bd7ba7abdb3d3279f60b3a747d2df189` → `37657634828` expected compile failure; GREEN `8b079d3c7e510081da368ce7383dd4edf9c9db8a` → `37657815355` PASS.
- Task 2 RED `96986b42a2ca3b29e92668b0d662d36725698996` → `37658018096`: 79 PASS / 1 expected FAIL; GREEN `d1e212929448b9f5de83a37538ed82c3de45428a` → `37658341567` PASS.
- Task 3 RED `f022ef6f7ded98e07bcaf6cfd914a1b1dece3606` → `37658611498`: 80 PASS / 3 expected FAIL.
- Task 3 GREEN implementation head `7fba8883f5af57ec1ac8a69bd5ba8bdba379822c` → push CI `37659069251` PASS; build 0 warnings / 0 errors; 83 PASS / 0 FAIL / 0 SKIPPED.
- Functional diff audit before closure docs: 6 files only; no package, no parser/runtime engine change, no PDF/layout/print/private fixtures.
- Exact-head closure CI after documentation is recorded in PR #15.

## Manual / Private QA Pending

### F0 physical Windows smoke — NOT RUN

1. real one-page/multipage/heavy PDFs;
2. navigation/go-to-page/zoom/fit/rapid resize;
3. print cancel/all/current/range;
4. Microsoft Print to PDF + reopen;
5. portrait/landscape + physical printer if available;
6. network disabled during local PDF flow;
7. invalid PDF preserving prior valid session.

### F1/F2 private and physical acceptance — NOT RUN

1. real private Mercado Libre `.zpl/.txt/.prn` corpus;
2. design and `^PQ` counts against actual files;
3. stored-format/template cases if present;
4. Labelize preview fidelity against known-good output;
5. F2.3 quantity modes and size/dpmm controls against real labels;
6. interactive PDF ↔ ZPL switching and render cancellation on Windows;
7. network-disabled/temp-residue privacy smoke;
8. barcode/QR scanner QA and exact-size thermal printing in later F2 acceptance.

## Blockers / Concerns

- No automated technical blocker in F2.3.
- F0 physical smoke remains open.
- F1 formal close waits for private real labels, but F2 can continue in small slices.
- `ZplGSCustom.ttf` Labelize provenance is acceptable for development/Gate selection but must be re-audited before a public production installer.
- `FPDF_RenderPageBitmap` remains synchronous; progressive/native mid-call cancellation is deferred until measured need.
- PDF print raster is 200 DPI pending real-world measurement.

## Deferred / Next

| Category | Item | Status | Revisit |
|---|---|---|---|
| PDF | Progressive rendering / native mid-call abort | Deferred until measured need | after physical heavy-PDF test |
| Print | DPI/raster strategy optimization | Deferred until measured need | after physical print test |
| ZPL | Labelize sidecar + navigable PNG preview | **Automated PASS** | F2.2 manual/private QA later |
| ZPL | Quantity UX + physical dimensions/dpmm | **Automated PASS** | F2.3 manual/private QA later |
| ZPL | Layout/PDF composition | **Next — design first** | F2.4 |
| ZPL | Exact thermal print | Planned | F2.5 |
| ZPL | Decode/private/physical hardening | Planned | F2.6 |
| Tooling | Future automatic GSD↔Graphify integration | Deferred | later tooling upgrade |

## Session Continuity

Last session: 2026-10-07  
Stopped at: **F2.3 automated complete**, PR #15 draft, no merge; final exact-head closure run is recorded in PR #15.  
Next technical slice: **F2.4 Layout + PDF composition**. Design/approve it before implementation and do not add PDFsharp until the design demonstrates need.  
Resume files: `docs/history/2026-10-07-F2.3.md`, `.planning/phases/02-f1-zpl-gate/F2.3-PLAN.md`, `docs/superpowers/specs/2026-10-07-f2-labelize-architecture-design.md`.
