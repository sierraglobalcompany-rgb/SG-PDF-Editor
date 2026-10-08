---
gsd_state_version: '1.0'
status: executing
progress:
  total_phases: 13
  completed_phases: 0
  total_plans: 12
  completed_plans: 0
  percent: 0
---

# Project State

## Project Reference
See `.planning/PROJECT.md`.

**Core value:** Resolver PDF + ZPL diario de forma rápida, privada, estable y offline.  
**Current focus:** Phase 4 — **F3 Firma Visual**. F3.1 y F3.2 tienen automated PASS. Siguiente slice: **F3.3 Dibujar firma con WPF InkCanvas**, empezando por design/approval gate.

## Current Position

F3.2 — **Photo/scan signature preparation: automated PASS**.  
Branch: `feat/f3-2-photo-preparation`.  
PR: #20 draft, base `feat/f3-visual-signature`, no merge.  
Functional head: `710340aed222d4c0bd97fc80099267af35ef6f95` → PR CI `37724154596` PASS; Release build **0 warnings / 0 errors**; **214 PASS / 0 FAIL / 0 SKIPPED**.

F3.1 final: `d559169280f9d9ee2c19f1c245f6657d1b598c6d` → push CI `37719587695` + PR CI `37719593067` PASS; **173 tests**.

Parallel acceptance gates still open:
- F0 physical Windows UI/print/offline smoke: **NOT RUN**.
- F1 private Mercado Libre corpus: **NOT RUN**.
- F2 private real-label corpus: **NOT RUN**.
- F2 physical thermal printer/ruler/scanner: **NOT RUN**.
- F3.1 hands-on real transparent-signature UX: **NOT RUN**.
- F3.2 real phone/scanner photo-quality QA: **NOT RUN**.

## Runtime / Architecture Decisions

- Windows x64 + C# + .NET 10 + WPF remain frozen.
- KISS solution remains `SGPdf.App + SGPdf.App.Tests`.
- PDFium remains primary PDF reader/render/editor; all native calls serialized by `PdfiumRuntime.NativeGate`.
- PDFsharp 6.2.4 stays limited to label PDF composition/export.
- F3.2 is preprocessing only: it does not modify `PdfVisualSignatureWriter`, coordinate mapping, PDFium P/Invoke, ZPL, or cryptographic-signature behavior.
- F3.2 accepts local `.png/.jpg/.jpeg`, normalizes to immutable BGRA and reuses the existing F3.1 `SignatureAsset`.
- Decoded-image limit is shared at exactly **20,000,000 pixels**.
- Paper/background estimation is performed once from the immutable full-resolution source. The same `SignaturePaperColor` is reused by reduced preview and final Apply.
- Preview is capped at **1200 px longest side** and is never used as the final asset. Apply always reprocesses the full immutable source.
- Automatic defaults: background removal 65, brightness 0, contrast 20, original ink, auto-crop ON.
- Black/blue recolor preserves alpha; blue is fixed at `#194196`.
- No AI/ML, cloud removal service, OCR, OpenCV/ImageSharp, runtime HTTP, telemetry, upload, or temp signature-image file.
- Cancel/failure in F3.2 never modifies existing placements, selection, dirty state or active PDF.
- No package/lockfile changes entered F3.2.
- No merge to `main` without explicit user approval.

## F3.2 Delivered

- safe PNG/JPEG local loader with shared 20M decoded-pixel guard;
- immutable `SignaturePhotoSource` BGRA contract;
- deterministic light-paper estimation from perimeter samples;
- soft alpha background removal preserving antialiased pen edges and original alpha;
- bounded background-removal / brightness / contrast controls;
- Original / Negro / Azul ink modes;
- usable-signature rejection for blank/noise-only inputs;
- automatic crop with bounded padding;
- checkerboard WPF preparation dialog;
- stale-preview version guard + WPF dispatcher-safe updates;
- reduced preview <=1200 px while Apply uses full source;
- `Crear desde foto...` integrated into the existing FIRMAR flow;
- cancel/failure state-preservation seams and automated regressions.

## Evidence

### F3.2
- Approved spec: `docs/superpowers/specs/2026-10-08-f3-2-photo-preparation-design.md`.
- Approved plan: `docs/superpowers/plans/2026-10-08-f3-2-photo-preparation.md`.
- Task 1 RED `b4e9652e6e39d6cd19f45a4987550ef6f83a9c30` → PR CI `37722059194`: expected 12 missing-symbol errors, 0 warnings.
- Task 1 GREEN correction `ac6c97c481dab1cab0c60706ed6e6cf323115fcf` → PR CI `37722384884` PASS.
- Task 3 dispatcher hardening `409815eb092b8b1c3fdcd927f4684f6612486a0e` → PR CI `37723699902` PASS.
- Task 4 RED `46e846d2d0e095af22f15f7bd710eefacc020a8f` → PR CI `37723874534`: Release build 0/0; exactly 4 new FIRMAR-photo tests failed while 210 passed.
- Functional GREEN `710340aed222d4c0bd97fc80099267af35ef6f95` → PR CI `37724154596` PASS; Release 0/0; **214 tests PASS**.
- Functional diff against F3.1: 17 files limited to F3.2 spec/plan, Sign preprocessing/UI integration and tests; no `.csproj`, lockfile, PDFium writer, ZPL or network surface change.
- Final documentation-head CI is recorded in PR #20 after the closure commit and does not change product behavior.

## Manual QA Pending

F3.2 automated evidence does not claim arbitrary real-world photo quality. Manual QA later should include:
1. black pen on white paper;
2. blue pen on white paper;
3. phone JPEG + scanner PNG;
4. warm/cool and moderately uneven lighting;
5. automatic preset and slider extremes;
6. Original/Negro/Azul output;
7. auto-crop + cancel/retry;
8. Apply → move/resize/save via F3.1;
9. network disconnected for the full flow.

## Deferred / Next

| Category | Item | Status | Revisit |
|---|---|---|---|
| PDF | F3.1 Core visual signature | **Automated PASS** | manual UX later |
| PDF | F3.2 Photo/scan cleanup | **Automated PASS / real-photo QA NOT RUN** | manual QA later |
| PDF | F3.3 Draw signature / InkCanvas | **Next — design/approve first** | next slice |
| PDF | F3.4 Local signature library | Approved scope, deferred | after F3.3 |
| ZPL | Private real-label corpus | NOT RUN | local QA |
| ZPL | Physical thermal + scanner | NOT RUN | hardware QA |

## Session Continuity

Last resumed: 2026-10-08.  
Stopped at: F3.2 automated closure after functional head `710340ae…`, 214/214 tests.  
Resume files: `docs/history/2026-10-08-F3.2.md`, `.planning/phases/04-f3-visual-signature/F3.2-PLAN.md`, F3.2 spec/plan, PR #20.  
Next product work: **F3.3 Draw signature / InkCanvas — design/approval before code**.