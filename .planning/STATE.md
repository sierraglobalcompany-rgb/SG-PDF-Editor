---
gsd_state_version: '1.0'
status: executing
progress:
  total_phases: 13
  completed_phases: 0
  total_plans: 11
  completed_plans: 0
  percent: 0
---

# Project State

## Project Reference
See `.planning/PROJECT.md`.

**Core value:** Resolver PDF + ZPL diario de forma rápida, privada, estable y offline.  
**Current focus:** Phase 4 — **F3 Firma Visual**. F3.1 Core está funcionalmente GREEN; cierre documental/exact-head CI en curso. Siguiente slice después del cierre: **F3.2 Foto/escaneo → firma transparente**.

## Current Position

F3.1 — **Visual Signature Core: functional automated PASS / closure CI pending on final docs head**.  
Branch: `feat/f3-visual-signature`.  
PR: #19 draft, base `feat/f2-6-validation-hardening`, no merge.  
Functional head: `edc255871f929bc5124220ce5c3ad7380a211b80` → PR CI `37719148872` PASS; Release build **0 warnings / 0 errors**; **173 PASS / 0 FAIL / 0 SKIPPED**.

Parallel acceptance gates still open:
- F0 physical Windows UI/print/offline smoke: **NOT RUN**.
- F1 private Mercado Libre corpus: **NOT RUN**.
- F2 private real-label corpus: **NOT RUN**.
- F2 physical thermal printer/ruler/scanner: **NOT RUN**.

## Runtime / Architecture Decisions

- Windows x64 + C# + .NET 10 + WPF remain frozen.
- KISS solution remains `SGPdf.App + SGPdf.App.Tests`.
- PDFium remains primary PDF reader/render/editor; all native calls serialized by `PdfiumRuntime.NativeGate`.
- PDFsharp 6.2.4 stays limited to label PDF composition/export; F3 signing writes with PDFium only.
- `Guardar como` remains mandatory for F3.1; the source PDF is never overwritten.
- Visual-signature state is in-memory and PDF-space points are authoritative; screen/device geometry is derived from the PDFium render transform.
- F3.1 pending edits are current-page only. Navigation/open/leave/close paths use a Save / Discard / Cancel guard.
- PNG import in F3.1 requires real transparency. White-background cleanup belongs to F3.2.
- Existing cryptographic signatures are not modified or created. Save preflight warns when signatures exist and blocks when signature count cannot be evaluated.
- F3.1 added no package/lock changes and no runtime network.
- **Ruling:** FIRMAR controls/overlay are created in `MainWindow.Sign.cs` during initialization rather than rewriting the large existing XAML shell. This preserves the same named controls/UX contract while minimizing regression risk during the interrupted-session recovery; no new UI framework was introduced.
- No merge to `main` without explicit user approval.

## F3.1 Delivered

- transparent PNG → immutable `SignatureAsset` with decoded-pixel/alpha safety checks;
- PDFium render device→page affine transform captured and reused for stable placement across zoom/refit;
- current-page `SignatureEditState` with centered add, select, proportional resize, move/clamp, duplicate, delete, dirty tracking;
- FIRMAR/LEER mode controls + page-aligned overlay;
- PDFium image-object insertion with alpha preservation and transactional temp → validate → destination save;
- reopen/render validation after save, outside `NativeGate`;
- source==destination block and existing-destination preservation on failure;
- cryptographic signature count preflight/warning;
- dirty guard including cancellable window close;
- headless STA tests remain noninteractive while visible product windows still show messages.

## Evidence

### F2 closure
- F2.5 final `c003d6512a5df5be2f53aab2262d09c6dcbf92cf` → push `37682952554` + PR `37682957831` PASS; 136 tests.
- F2.6 final `6c7d60a5bad22db20860685e955aa5ef03fbfa19` → push `37690690752` + PR `37690696356` PASS; 144 tests.

### F3.1
- Approved spec: `docs/superpowers/specs/2026-10-07-f3-visual-signature-design.md`.
- Approved plan: `docs/superpowers/plans/2026-10-07-f3-1-visual-signature-core.md`.
- Task 4 RED head `f33223e25699644112637e3907748ce70a5647a0` → PR CI `37701394410`: expected missing `PendingSignatureDecision` / `SignatureGuardReason`, build 0 warnings.
- Task 4 GREEN functional head `edc255871f929bc5124220ce5c3ad7380a211b80` → PR CI `37719148872` PASS; build 0/0; **173 tests**.
- Whole functional diff against F2.6: 18 files limited to F3 spec/plan, Sign feature/native PDFium additions and tests; no `.csproj`, lockfile or third-party runtime mutation.

## Manual QA Pending

F3.1 automated evidence does not replace hands-on Windows QA. Still useful later:
1. real transparent signature PNG;
2. drag + resize feel at 100%, Fit Page and Fit Width;
3. Save As + visual reopen in an external PDF viewer;
4. PDF containing an existing cryptographic signature to verify warning UX.

## Deferred / Next

| Category | Item | Status | Revisit |
|---|---|---|---|
| PDF | F3.1 Core visual signature | **Functional automated PASS; closure exact-head CI pending** | now |
| PDF | F3.2 Photo/scan cleanup | **Next — design/plan gate** | next slice |
| PDF | F3.3 Draw signature / InkCanvas | Approved scope, not implemented | after F3.2 |
| PDF | F3.4 Local signature library | Approved scope, deferred | after F3.3 |
| ZPL | Private real-label corpus | NOT RUN | local QA |
| ZPL | Physical thermal + scanner | NOT RUN | hardware QA |

## Session Continuity

Last resumed: 2026-10-08.  
Stopped at: F3.1 functional GREEN (`edc25587…`, 173/173) and Task 5 closure documentation/exact-head CI.  
Resume files: `docs/history/2026-10-08-F3.1.md`, `.planning/phases/04-f3-visual-signature/F3.1-PLAN.md`, F3 spec/plan, PR #19.  
After F3.1 exact-head closure, proceed to **F3.2 design/approval**, not directly to implementation.
