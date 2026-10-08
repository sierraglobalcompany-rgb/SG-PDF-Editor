---
gsd_state_version: '1.0'
status: designing
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
**Current focus:** Phase 5 — **F4 Lector Completo**, written design spec self-reviewed and awaiting user approval. Product code for F4 has **NOT STARTED**.

## Previous Gate — F3.4 closed automatically

- Branch: `feat/f3-4-local-signature-library`.
- Base F3.3: `8b5bfd35b59fbc75826f8d7616aaaca3e2f31233`.
- Functional head: `c46ddbc9e7bea9ea1dab2eb7678a837d7364f9c0`.
- Closure head: `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b`.
- Closure push CI `37837613618`: PASS.
- PR CI `37837833517`: PASS.
- Release build: 0 warnings / 0 errors.
- Tests: 301 PASS / 0 FAIL / 0 SKIPPED.
- PR #22: draft/open/unmerged, stacked on F3.3.
- Manual real Windows QA: **NOT RUN**.

## Current Gate — F4 design review

- Branch: `feat/f4-full-reader`.
- Base: exact F3.4 closure head `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b`.
- Formal design spec: `docs/superpowers/specs/2026-10-08-f4-full-reader-design.md`.
- Spec state: **WRITTEN + SELF-REVIEWED; awaiting user approval**.
- F4 implementation plan: **NOT WRITTEN**.
- F4 product code: **NOT STARTED**.
- F4 PR: **NOT OPENED**.

### Frozen design direction

- PDFium only; no second PDF engine.
- `LEER` becomes continuous virtualized reading.
- `FIRMAR` remains the existing single-active-page editing surface.
- Continuous full-resolution bitmap retention is visible pages + one neighbor each side.
- Lazy thumbnails; no eager full-document render.
- Search/copy uses PDFium text APIs; no OCR in F4.
- Bookmarks read-only and cycle-bounded.
- Explicit PDF internal links + confirmed HTTP/HTTPS URI only.
- Passwords never persisted.
- Recent files = max 10 local paths + timestamp; no startup path probing.
- No multi-document tabs in F4.

## Executed Chain

- A0 Foundation: PASS.
- A1 Development Intelligence: PASS.
- F0.1–F0.6 PDF Base: automated PASS.
- F1 Gate ZPL-A: synthetic Gate PASS; Labelize 1.7.0 selected; private real corpus NOT RUN.
- F2.1–F2.6 ZPL Workspace: automated PASS.
- F3.1 Core visual signature: automated PASS.
- F3.2 Photo/scan preparation: automated PASS; real-photo QA NOT RUN.
- F3.3 Draw signature: automated PASS; hardware QA NOT RUN.
- F3.4 Local signature library: automated closure PASS; manual Windows QA NOT RUN.

## Parallel Acceptance Gates Still Open

- F0 physical Windows UI/print/offline smoke: **NOT RUN**.
- F1 private Mercado Libre corpus: **NOT RUN**.
- F2 private real-label corpus: **NOT RUN**.
- F2 thermal printer/ruler/scanner QA: **NOT RUN**.
- F3.1 real transparent-signature UX/save/open: **NOT RUN**.
- F3.2 real phone/scanner photo-quality QA: **NOT RUN**.
- F3.3 real mouse/touch/stylus QA: **NOT RUN**.
- F3.4 real save→restart→reuse/rename/delete/offline QA: **NOT RUN**.

They remain separate from automated PASS and do not become accepted by inference.

## Runtime / Architecture Decisions

- Windows x64 + C# + .NET 10 + WPF.
- KISS solution remains `SGPdf.App + SGPdf.App.Tests`.
- PDFium primary; all native calls serialized by `PdfiumRuntime.NativeGate`.
- Labelize 1.7.0 is the only approved runtime ZPL renderer.
- PDFsharp remains labels-only.
- ZXing.Net remains test/QA-only.
- F3 sources converge to `SignatureAsset` → `AddSignatureAsset(...)`.
- No merge to `main` without explicit user approval.

## Next Gate

```text
user reviews F4 written spec
→ if changes requested: revise + self-review again
→ if approved: invoke writing-plans and write detailed TDD implementation plan
→ user reviews/approves plan
→ only then begin F4.1 RED → GREEN
```

## Continuity

- Cross-project audit: `docs/history/2026-10-08-CONTINUITY-AUDIT.md`.
- F3.4 closure: `docs/history/2026-10-08-F3.4.md`.
- F4 design: `docs/superpowers/specs/2026-10-08-f4-full-reader-design.md`.
