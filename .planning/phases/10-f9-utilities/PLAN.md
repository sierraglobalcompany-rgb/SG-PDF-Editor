# Phase F9 — Utilidades offline

**Status:** IN PROGRESS  
**Branch:** `feat/f9-offline-utilities`  
**Base:** F8 closure `7e092166b04d72e2b8d4d1ba2d95d48e1acdebb3`

## Objective

Deliver focused, offline PDF utilities without creating a generic mega-workspace or introducing a second PDF engine without measured need.

## Approved slices

1. F9.1 watermark + page/Bates numbering.
2. Protection capability gate, then authorized protection using PDFium or one justified local tool.
3. Optimize/repair capability gates; expose only behavior proven reliable.
4. Preservation/hardening/automated closure.

## Execution rules

- KISS/YAGNI.
- PDFium first, exact pinned `bblanchon.PDFium.Win32 156.0.8076`.
- DejaVu Sans 2.37 remains the only pinned fallback font until an explicit later font decision.
- Unsupported glyphs fail closed.
- TDD: RED → minimum GREEN → exact-head Windows CI → scope audit → checkpoint.
- Save As only; source stays untouched.
- No runtime network/cloud/API keys/accounts.
- No qpdf/pdfcpu prophylactically.
- No merge to `main` without explicit approval.
- Manual Windows/external-reader QA must remain NOT RUN unless actually executed.

## Task map

See `docs/superpowers/plans/2026-10-11-f9-offline-utilities.md` for the executable TDD plan.

Current task: **Task 1 — exact PDFium overlay capability gate**. Product writer/UI work does not start until this gate closes.
