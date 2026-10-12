# Phase F10 — Local OCR

**Status:** AUTOMATED CLOSURE DECLARED; exact-head integration CI required  
**Branch:** `feat/f10-local-ocr`  
**Base:** F9 closure `222454f5782f84a929c7fd610c284d15df8f869d`

## Goal

Add focused local/offline OCR that makes scanned PDF pages searchable without replacing their visual page or introducing a second PDF engine.

## Delivered

- PDFium invisible searchable-text capability gate.
- Pinned local Tesseract 5.5.3 runtime + Spanish/English data.
- Eligibility/scopes/languages and isolated recognition process.
- 300-DPI raster, strict TSV parsing and PDF geometry mapping.
- Single OCR planner, PDFium writer and semantic validator.
- Transactional Save As workflow with preflight, cancellation and cleanup.
- Focused WPF command/dialog/progress/cancel path.
- Fresh F10 preservation corpus including exact decoded image payload source→output.

## Invariants

- KISS/YAGNI.
- PDFium owns the PDF; Tesseract recognition only.
- One OCR engine; no cloud/account/API-key/backend/database/runtime download.
- qpdf remains a narrow existing sidecar/test helper, not OCR writer.
- Source untouched; atomic Save As.
- `main` frozen; no merge without explicit approval.
- AUTO PASS never implies manual/private/external-reader PASS.

## Evidence before closure-doc integration gate

- Task 7 GREEN: `358609915e730dbe50935877c1893e2cbaa9235c`, CI `38197616598` / `114643714366`.
- Preservation GREEN checkpoint: `1598a948ecbfe9a19c76ec1f56acd9ad2ba196bf`, CI `38204535141` / `114664976057`.
- Image payload micro-gate: `aba0228507055a3a9486060f6cbb076aafdc4643`, CI `38206127428` / `114669898651`, full pipeline PASS.
- Preservation matrix: `docs/history/2026-10-11-F10-preservation-matrix.md`.

`LOCAL DIRECT RUN = NOT SEPARATELY EVIDENCED`.

## Manual status

Real Windows OCR UX, low-quality/private scans, Adobe/Foxit/Edge, physical offline and representative hardware: **NOT RUN**.

## Integration gate

The commit carrying this closure declaration must pass exact-head Windows CI, then a draft/open/unmerged PR must exist from `feat/f9-offline-utilities` to `feat/f10-local-ocr`, and `main` must remain unchanged. Once those conditions are verified: **STOP F10 and await explicit continuation**.

Canonical executable plan: `docs/superpowers/plans/2026-10-11-f10-local-ocr.md`.
