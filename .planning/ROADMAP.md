# Roadmap — SG PDF Editor

> Arquitectura: `docs/MASTER_CONTEXT.md` + `docs/MASTER_PLAN.md`. Estado operativo: `.planning/STATE.md`. GitHub exact heads/CI mandan sobre snapshots históricos.

## Estado por fase

| Fase | Estado automatizado | QA manual/privado/físico |
|---|---|---|
| F0 PDF Base | CLOSED / AUTO PASS | NOT RUN completo |
| F1 Gate ZPL-A | synthetic PASS | private corpus NOT RUN |
| F2 ZPL Workspace | CLOSED / AUTO PASS | private + thermal/ruler/scanner NOT RUN |
| F3 Firma Visual | CLOSED / AUTO PASS | Windows/photo/stylus/library NOT RUN |
| F4 Lector Completo | CLOSED / AUTO PASS | Windows real NOT RUN |
| F5 Organizar | CLOSED / AUTO PASS | Windows real NOT RUN |
| F6 Imágenes | CLOSED / AUTO PASS | Windows real NOT RUN |
| F7 Texto V1 | CLOSED / AUTO PASS | Windows real NOT RUN |
| F8 Comentarios | CLOSED / AUTO PASS | stylus/touch/external readers NOT RUN |
| F9 Utilidades | CLOSED / AUTO PASS | Windows/external/private repair corpus NOT RUN |
| F10 OCR Local | CLOSED / AUTO PASS | Windows OCR/external readers/private corpus NOT RUN |
| F11 Texto V2 | CLOSED / AUTO PASS | Windows reflow/private layouts/external readers NOT RUN |
| **F12 Profesional** | **CLOSED / AUTO PASS BASELINE** | **real/private/manual gates remain open** |

## F12 Professional — closed automated baseline

| Slice | State | Limits / future gate |
|---|---|---|
| F12.1 Safe Redaction | CLOSED / AUTO PASS | advanced semantics only by separate approval |
| F12.2 Cryptographic Signature PFX/P12 MVP | CLOSED / AUTO PASS | store/smartcard/TSA etc. separate approval |
| F12.3 Existing AcroForm text fields | CLOSED / AUTO PASS | `/Btn`, `/Ch`, XFA/designer separate approval |
| F12.4 Compare | CLOSED / AUTO PASS | representative real-PDF QA later |
| F12.5 Batch | CLOSED / AUTO PASS — TextWatermark MVP | other operations separate value/capability gate |
| F12.6 Code Generator | CLOSED / AUTO PASS — QR PNG MVP | Code128/other symbologies separate gate |
| F12.7 Audited Conversions | **CLOSED / AUTO PASS — PDF→PNG MVP** | other conversions separate gate |

## F12.7 PDF → PNG — closed MVP

Capability gate result: **GO using existing PDFium render owner + WPF PNG encoder**.

Flow:
`open PDF → Herramientas → Convertir PDF a PNG... → choose folder → sequential 150 DPI render → page-0001.png ...`.

Closed behavior:
- local/offline;
- current already-open PDF only;
- every page exported sequentially at 150 DPI;
- deterministic names;
- explicit output folder;
- preflight rejects any existing target before writing;
- `FileMode.CreateNew` protects against overwrite races;
- original PDF never modified;
- representative two-page contract validates exact output count and rendered dimensions;
- existing Utilities owner/lifecycle reused;
- zero new runtime dependencies.

Explicitly not baseline:
- generic conversion engine/registry/plugins;
- DOCX/PPTX/XLSX ↔ PDF;
- LibreOffice or Office automation;
- cloud conversion;
- second PDF engine;
- JPEG/WebP/TIFF selector, DPI presets or batch conversion;
- universal fidelity claim.

Spec: `docs/superpowers/specs/2026-10-14-f12-7-pdf-to-png-scope.md`.  
Plan: `docs/superpowers/plans/2026-10-14-f12-7-pdf-to-png.md`.

Functional exact-head evidence:
- `521f0974b4b8a6b5ae7138a51cf11e61d572a998` on `feat/f12-7-pdf-to-png`;
- Release build 0 warnings / 0 errors;
- 1201/1201 tests PASS, 0 failed, 0 skipped;
- CI run `38514284225`, job `115689481722`.

## Remaining sequence — release path, not feature expansion

### 1. Bounded real QA packs

Execute accumulated manual/private/physical work in bounded packs:
- Reader / Organize / Editing;
- ZPL + QR + physical print/scan;
- visual + cryptographic signatures;
- OCR / Forms / Compare / Batch / PDF→PNG.

Prioritize P0/P1 functional or data-integrity risks first. Automated PASS is not a substitute for these gates.

### 2. Documentation consolidation

One bounded cleanup:
- reconcile stale `AGENTS.md` / historical master-plan wording where useful;
- preserve one operational source of truth;
- mark superseded docs instead of multiplying handoffs;
- remove proven documentation noise without losing recovery evidence.

### 3. Release integration / stabilization

Before release:
- map active branch/PR ancestry and unique commits;
- choose one conservative integration strategy;
- use a dedicated integration branch;
- reconcile stacked branches/PRs without destructive cleanup;
- keep `main` read-only until explicit user approval;
- run complete exact-head CI and prioritized real QA;
- close installer/license provenance items.

### 4. Release candidate / final

Release candidate requires clean-clone reproducibility, locked restore, Release build/tests, packaged runtime/installer smoke, offline smoke and no P0/P1 blockers. Final adds no feature creep.

## KISS guardrail after F12 closure

Do not reopen baseline features opportunistically. New conversions, symbologies, batch operations or advanced PDF capabilities require separate value/capability decisions and must prove reuse before new architecture.

No merge to `main` without explicit user approval.
