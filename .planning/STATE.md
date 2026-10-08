---
gsd_state_version: '1.0'
status: executing
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

**Core value:** Resolver PDF + ZPL diario de forma rápida, privada, estable y offline.  
**Current focus:** Phase 4 — **F3 Firma Visual**. F3.1 y F3.2 tienen automated PASS. F3.3 está funcionalmente GREEN y espera únicamente closure exact-head CI. Después sigue F3.4 biblioteca local, con design/approval primero.

## Current Position

F3.3 — **Draw signature / InkCanvas: functional GREEN; exact-head closure CI pending at documentation time**.  
Branch: `feat/f3-3-drawn-signature`.  
PR: #21 draft, base `feat/f3-2-photo-preparation`, no merge.  
Functional head: `d3e7d5e0fe460255ec37d41d8b5017272088406d` → CI `37802064739` PASS; Release build **0 warnings / 0 errors**; **246 PASS / 0 FAIL / 0 SKIPPED**.

F3.2 final: `2aa1f58a6e01397e84d8f8cfcf7eb6e0e168cf62` → push `37796057421` + PR `37796065775` PASS; **214 tests**.  
F3.1 final: `d559169280f9d9ee2c19f1c245f6657d1b598c6d` → push `37719587695` + PR `37719593067` PASS; **173 tests**.

Parallel acceptance gates still open:
- F0 physical Windows UI/print/offline smoke: **NOT RUN**.
- F1 private Mercado Libre corpus: **NOT RUN**.
- F2 private real-label corpus: **NOT RUN**.
- F2 physical thermal printer/ruler/scanner: **NOT RUN**.
- F3.1 hands-on real transparent-signature UX: **NOT RUN**.
- F3.2 real phone/scanner photo-quality QA: **NOT RUN**.
- F3.3 mouse/touch/stylus hardware QA: **NOT RUN**.

## Runtime / Architecture Decisions

- Windows x64 + C# + .NET 10 + WPF remain frozen.
- KISS solution remains `SGPdf.App + SGPdf.App.Tests`.
- PDFium remains primary PDF reader/render/editor; native calls serialized by `PdfiumRuntime.NativeGate`.
- PDFsharp 6.2.4 stays limited to label PDF composition/export.
- All visual signature sources converge to the existing F3.1 `SignatureAsset` + `AddSignatureAsset(...)` placement path.
- F3.3 uses WPF `InkCanvas` / `StrokeCollection`; no custom pointer/stylus engine.
- Drawn signature colors: Black `#000000`, Blue `#194196`.
- Drawn widths: Thin 2.0 DIP, Medium 3.5 DIP, Thick 5.0 DIP; Medium default.
- Drawn-signature raster scale is fixed at `300 / 96 = 3.125` and reuses the shared **20,000,000 pixel** guard.
- Drawn output is transparent/cropped from strokes only; the visible InkCanvas background/chrome is never embedded.
- Undo/Redo/Clear in F3.3 are dialog-local only; Clear is not undoable.
- **Ruling F3.3:** drawing helper is `SignatureStrokeStyle`, because F3.2 already owns enum `SignatureInkStyle` (`Original/Black/Blue`). Do not merge these concepts.
- F3.3 does not modify `PdfVisualSignatureWriter`, PDFium P/Invoke/coordinates, F3.2 processor, ZPL, PDFsharp scope, packages, networking or persistence.
- No merge to `main` without explicit user approval.

## F3.3 Delivered

- native WPF InkCanvas capture path for mouse/touch/stylus where Windows/WPF supports it;
- synthetic stroke renderer with alpha-preserving transparent output;
- fixed black/blue and three fixed widths;
- tiny/empty ink rejection;
- crop + bounded transparent padding;
- 300-DPI-equivalent rasterization;
- 20M output-pixel safety guard;
- `SignatureInkHistory` for dialog-local Undo/Redo/Clear;
- `SignatureDrawDialog` with Apply/Cancel safety;
- `Dibujar firma...` in FIRMAR;
- cancel/failure preserves existing placement/selection/dirty state;
- successful draw uses the same `AddSignatureAsset(...)` path as PNG/photo.

## Evidence

### F3.3
- Approved spec: `docs/superpowers/specs/2026-10-08-f3-3-drawn-signature-design.md`.
- Approved plan: `docs/superpowers/plans/2026-10-08-f3-3-drawn-signature.md`.
- Task 1 RED `599ca2300bc3747d5019a3a5cb266204d46def3f` → CI `37798957684` expected missing-contract failure.
- Task 1 final GREEN `e9a0f08744f25cd552e830900c9ff50ee2afa0bc` → CI `37800183733` PASS; **225 tests**.
- Task 2 RED `967e92a45fd364af66d64cdfc1fd693fc4991cb3` → CI `37800721227`: exactly 3 missing-contract compile errors, 0 warnings.
- Task 2 GREEN `b264708bd5cfa37ca532b9edcd645e596c98fda9` → PR CI `37801174098` PASS; **240 tests**, Release 0/0.
- Task 3 RED `7b4061018b5e5e5ff8f9ed4be511661d9952fab5` → CI `37801656831`: 6 new tests FAIL / 240 existing PASS, build 0/0.
- Functional GREEN `d3e7d5e0fe460255ec37d41d8b5017272088406d` → CI `37802064739` PASS; **246 tests**, Release 0/0.
- Scope audit against F3.2: only F3.3 spec/plan, feature-local ink code/dialog, `MainWindow.Sign.cs` source integration and tests. No package/lock/PDFium/F3.2/ZPL/network/persistence expansion.
- Closure exact-head push/PR CI IDs are recorded in PR #21 after the single closure-doc commit, without mutating the branch again.

## Manual QA Pending

F3.3 hardware QA remains separate:
1. mouse drawing;
2. touch screen when available;
3. Surface/Wacom/other stylus when available;
4. width/color feel;
5. Undo/Redo/Clear usability;
6. Apply → move/resize/save through F3.1;
7. external viewer inspection;
8. offline operation.

## Deferred / Next

| Category | Item | Status | Revisit |
|---|---|---|---|
| PDF | F3.1 Core visual signature | **Automated PASS** | manual UX later |
| PDF | F3.2 Photo/scan cleanup | **Automated PASS / real-photo QA NOT RUN** | manual QA later |
| PDF | F3.3 Draw signature / InkCanvas | **Functional GREEN; exact-head closure CI required** | current closure |
| PDF | F3.4 Local signature library | Approved scope, not designed yet | next after F3.3 closure |
| ZPL | Private real-label corpus | NOT RUN | local QA |
| ZPL | Physical thermal + scanner | NOT RUN | hardware QA |

## Session Continuity

Last resumed: 2026-10-08.  
Stopped at: F3.3 closure after functional head `d3e7d5e0…`, 246/246 tests.  
Resume files: `docs/history/2026-10-08-F3.3.md`, `.planning/phases/04-f3-visual-signature/F3.3-PLAN.md`, F3.3 spec/plan, PR #21.  
Next product work after exact-head closure verification: **F3.4 Local Signature Library — design/approval before code**.
