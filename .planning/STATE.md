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
**Current focus:** Phase 3 — F2 Etiquetas; F2.1 and F2.2 automated closed. Next slice: F2.3 Quantity UX + dimensions. F1 private-corpus acceptance remains open.

## Current Position

F2.2 — **Labelize Adapter + Preview: automated PASS**.  
Branch: `feat/f2-2-labelize-preview`.  
PR: #14 draft, base `feat/f2-1-zpl-parse-open`, no merge.  
Implementation head before closure docs: `5b8829b65e082ad185eb9f97ed145962b70411d1` → Windows CI `37654759373` PASS, Release build 0 warnings / 0 errors, 73 tests PASS.  
Task 5 closure docs are the final branch mutation; exact-head CI after that mutation is recorded in PR #14.  
Last activity: 2026-10-07 — pinned Labelize sidecar, safe local process execution, managed PNG mapping/cleanup and navigable WPF preview completed.

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

- Gate ZPL-A selected **Labelize 1.7.0** over BinaryKits for F2.
- Labelize is a pinned bundled local `labelize.exe` sidecar; no local HTTP server and no custom Rust ABI.
- BinaryKits is historical Gate evidence only; no dual-engine runtime abstraction or automatic fallback.
- Labelize release archive SHA-256: `cdd4030b0d1a8bad69b93f49866c8dcc5314af8975bb16a76991fe32f92dd21d`.
- Development/CI may stage the pinned binary explicitly; product runtime must never download it.
- `^PQ` is quantity metadata, not a render multiplier. It is removed from normalized render source; absent/zero defaults to 1; last command wins per printable block; max supported quantity 99,999,999.
- `TotalQuantityFromFile` is `long`.
- `^DF` stored-format definition blocks remain support context, not user-visible designs; normalized document-level source preserves `^DF/^XF` order.
- `^PQ` inside a `^DF` definition is rejected rather than guessed.
- F2.2 renders the normalized document once, maps Labelize outputs deterministically to printable designs and fails on output-count mismatch rather than guessing.
- Labelize render temp data is request-scoped; PNG bytes are loaded into managed memory before cleanup.
- WPF preview uses `BitmapImage.CacheOption=OnLoad`; no temp file remains attached to the UI bitmap.
- Label preview navigation is Previous / `Etiqueta n de N` / Next; each design appears once regardless of `^PQ`.
- Opening ZPL remains candidate-first: parse + render must succeed before replacing a valid PDF/ZPL workspace.
- Window close cancels an active Labelize child process through the existing process-tree cancellation path.
- F2.2 defaults remain 102×152 mm at 8 dpmm; final size/dpmm UX belongs to F2.3.
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
- Verified head `2df2f374ae6124729389640425dc8334d0647f9f`.
- Windows workflow `37577735748`: PASS.
- Both engines rendered the synthetic corpus and produced decodable Code128/QR after alpha-aware comparison.
- Measured throughput: Labelize ~59–61 designs/s vs BinaryKits ~26–28 designs/s.
- Packaging: Labelize executable 5,860,352 bytes vs BinaryKits probe framework-dependent publish ~16.9 MB / 14 files.
- `^FT + ^BQ`: BinaryKits rendered the QR 60 px lower vertically than Labelize while both remained decodable.
- **Formal F1 acceptance remains open for private real Mercado Libre corpus.**

### F2.1 — ZPL Parse + Open

- Automated implementation/history PASS on branch `feat/f2-1-zpl-parse-open` / PR #13 draft.
- History-complete head `21fc4e515677b4870fe6319b246d7329bda39ce1` → `37584180581` PASS.
- Final implementation head recorded in the F2.1 history/PR; no merge.

### F2.2 — Labelize Adapter + Preview

- Tasks 1–3 consolidated GREEN head `8d6da1e6cd2099a812adc54c8e6c90d409e01be0` → `37641938607` PASS; build 0 warnings / 0 errors; 72 tests PASS.
- Task 4 RED head `691163513af05c6182cc0a65c7356eb9df498ddc` → `37642332662` expected failure.
- Task 4 GREEN implementation head `5b8829b65e082ad185eb9f97ed145962b70411d1` → `37654759373` PASS; build 0 warnings / 0 errors; 73 tests PASS.
- Diff audit: no runtime HTTP/server, no BinaryKits fallback, no PDF composition/PDFsharp, no private fixtures, no committed Labelize runtime/archive.
- Exact-head closure CI after Task 5 docs is recorded in PR #14.

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
5. interactive PDF ↔ ZPL switching and render cancellation on Windows;
6. network-disabled/temp-residue privacy smoke;
7. barcode/QR scanner QA and exact-size thermal printing in later F2 acceptance.

## Blockers / Concerns

- No automated technical blocker in F2.2.
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
| ZPL | Quantity UX + dimensions | **Next** | F2.3 |
| ZPL | Layout/PDF composition | Planned, dependency only when needed | F2.4 |
| ZPL | Exact thermal print | Planned | F2.5 |
| ZPL | Decode/private/physical hardening | Planned | F2.6 |
| Tooling | Future automatic GSD↔Graphify integration | Deferred | later tooling upgrade |

## Session Continuity

Last session: 2026-10-07  
Stopped at: **F2.2 automated complete**, PR #14 draft, no merge; final exact-head closure run is recorded in PR #14.  
Next technical slice: **F2.3 Quantity UX + physical dimensions/dpmm**, using the stable F2.2 preview and without starting F2.4 composition.  
Resume files: `docs/history/2026-10-07-F2.2.md`, `.planning/phases/02-f1-zpl-gate/F2.2-PLAN.md`, `docs/superpowers/specs/2026-10-07-f2-labelize-architecture-design.md`.
