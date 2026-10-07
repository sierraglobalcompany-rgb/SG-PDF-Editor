---
gsd_state_version: '1.0'
status: executing
progress:
  total_phases: 13
  completed_phases: 0
  total_plans: 6
  completed_plans: 0
  percent: 0
---

# Project State

## Project Reference

See: `.planning/PROJECT.md`.

**Core value:** Resolver PDF + ZPL diario de forma rápida, privada, estable y offline.  
**Current focus:** Phase 3 — F2 Etiquetas; F2.1 automated closed, next slice F2.2 Labelize adapter + preview. F1 private-corpus acceptance remains open.

## Current Position

F2.1 — **ZPL Parse + Open: automated PASS**.  
Branch: `feat/f2-1-zpl-parse-open`.  
PR: #13 draft, base `design/f2-labelize-architecture`, no merge.  
History-complete head `21fc4e515677b4870fe6319b246d7329bda39ce1` passed Windows CI `37584180581`; this state/plan stamp receives one final exact-head CI recorded in PR #13.  
Last activity: 2026-10-07 — parser/quantity/loader/WPF open flow completed without adding Labelize or other runtime dependencies.

Parallel acceptance gates still open:

- F0 PDF Base: F0.1–F0.6 automated PASS; physical Windows UI/print/offline smoke **NOT RUN**.
- F1 Gate ZPL-A: synthetic comparison complete and engine selection approved as **Labelize 1.7.0**; private real Mercado Libre ZPL corpus still required for formal F1 close.
- F2.1 private/manual smoke: real labels and interactive PDF ↔ ZPL switching **NOT RUN**.

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
- F0.2 uses immutable `PageNavigationState`; navigation state commits only after successful render.
- F0.3 uses `PdfZoomState` (`Manual`, `FitPage`, `FitWidth`); `100% = 96 DPI`; PDFium rerenders rather than stretching a bitmap.
- Zoom/fit UX follows shared Acrobat/Foxit/PDF-XChange patterns, simplified by KISS.
- F0.4 uses `PdfRenderScheduler` latest-request-wins; resize-fit debounce 150 ms; no general queue/workers/multilevel priority.
- PDFium cancellation is cooperative around synchronous native render. Progressive rendering remains deferred until real latency evidence requires it.
- F0.5 uses WPF `PrintDialog` + `DocumentPaginator` + Windows spooler; Microsoft Print to PDF uses the same standard path.
- F0.5 print raster starts at 200 DPI; quality/memory/latency must be measured physically before changing strategy.
- F0.6 completed `PDF-BASE-03` with `GoToPageNumber(int)` and compact `Página [n] de N`.
- F0.6 offline guard rejects direct network assemblies/clients/remote XAML navigation and pins runtime NuGet surface to `bblanchon.PDFium.Win32` only.

### ZPL / Labels

- Gate ZPL-A synthetic probe selected **Labelize 1.7.0** over BinaryKits for F2.
- Labelize integration model: pinned bundled local `labelize.exe` sidecar invoked as a child process beginning in F2.2; no local HTTP server and no custom Rust ABI.
- BinaryKits is historical Gate evidence only; do not carry a dual-engine runtime abstraction.
- Labelize release archive pin verified in the spike: SHA-256 `cdd4030b0d1a8bad69b93f49866c8dcc5314af8975bb16a76991fe32f92dd21d`.
- F2.1 deliberately contains **no Labelize call/package**: parser, quantity semantics, strict local loader and WPF open flow only.
- `^PQ` is quantity metadata, not a render multiplier. It is removed from normalized render source; absent/zero defaults to 1; last command wins per printable block; max supported quantity 99,999,999.
- `TotalQuantityFromFile` is `long`.
- `^DF` stored-format definition blocks remain support context, not user-visible designs; normalized document-level source preserves `^DF/^XF` order.
- `^PQ` inside a `^DF` definition is rejected in F2.1 rather than guessed.
- Real Mercado Libre/customer ZPL stays private/local and is never committed, attached to CI artifacts or indexed by Graphify.
- WPF label integration is kept in `MainWindow.Labels.cs` as a partial of the existing window; no ViewModel/service/framework was introduced.
- Workspace replacement is candidate-first: an invalid ZPL cannot destroy the current valid PDF/ZPL state.
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
- Verified head `2df2f374ae6124729389640425dc8334d0647f9f`.
- Windows workflow `37577735748`: PASS.
- Both engines rendered the synthetic corpus and produced decodable Code128/QR after alpha-aware comparison.
- Measured throughput: Labelize ~59–61 designs/s vs BinaryKits ~26–28 designs/s.
- Packaging: Labelize executable 5,860,352 bytes vs BinaryKits probe framework-dependent publish ~16.9 MB / 14 files.
- `^FT + ^BQ`: BinaryKits rendered the QR 60 px lower vertically than Labelize while both remained decodable.
- User approved Labelize 1.7.0 as the F2 engine selection.
- **Formal F1 acceptance remains open for private real Mercado Libre corpus.**

### F2.1 — ZPL Parse + Open

- Task 1 RED `37582129129`; GREEN `37582251039`.
- Task 2 RED `37582400091`: 39 PASS / 9 expected FAIL; GREEN `37582542467`: 48 tests PASS.
- Task 3 RED `37582646779`; first implementation `37582750270` exposed missing `System.IO`; corrected GREEN `37582975432` PASS.
- Task 4 RED `37583103587`: 57 PASS / 2 expected FAIL; GREEN `37583395960` PASS.
- Docs-complete head `0c63575c7d9844626d3c35314cb03209f00ba2cb` → `37583965791` PASS.
- History-complete head `21fc4e515677b4870fe6319b246d7329bda39ce1` → `37584180581` PASS.
- PR #13 draft; no merge.

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
4. F2.2 Labelize preview fidelity;
5. barcode/QR scanner QA and exact-size thermal printing;
6. offline/temp/privacy residue audit.

## Blockers / Concerns

- No automated technical blocker in F2.1.
- F0 physical smoke remains open.
- F1 formal close waits for private real labels, but the engine architecture is approved and F2 can proceed in slices.
- `ZplGSCustom.ttf` Labelize provenance is acceptable for development/Gate selection but must be re-audited before a public production installer.
- `FPDF_RenderPageBitmap` remains synchronous; progressive/native mid-call cancellation is deferred until measured need.
- PDF print raster is 200 DPI pending real-world measurement.

## Deferred / Next

| Category | Item | Status | Revisit |
|---|---|---|---|
| PDF | Progressive rendering / native mid-call abort | Deferred until measured need | after physical heavy-PDF test |
| Print | DPI/raster strategy optimization | Deferred until measured need | after physical print test |
| ZPL | Labelize sidecar process renderer + PNG preview | **Next** | F2.2 |
| ZPL | Quantity UX + dimensions | Planned | F2.3 |
| ZPL | Layout/PDF composition | Planned, dependency only when needed | F2.4 |
| ZPL | Exact thermal print | Planned | F2.5 |
| ZPL | Decode/private/physical hardening | Planned | F2.6 |
| Tooling | Future automatic GSD↔Graphify integration | Deferred | later tooling upgrade |

## Session Continuity

Last session: 2026-10-07  
Stopped at: **F2.1 automated complete**, PR #13 draft, no merge. Final exact-head CI for this state/plan stamp is recorded externally in PR #13.  
Next technical slice: F2.2 Labelize 1.7.0 local process adapter + PNG preview.  
Resume files: `docs/history/2026-10-07-F2.1.md`, `.planning/phases/02-f1-zpl-gate/F2.1-PLAN.md`, `docs/superpowers/specs/2026-10-07-f2-labelize-architecture-design.md`.
