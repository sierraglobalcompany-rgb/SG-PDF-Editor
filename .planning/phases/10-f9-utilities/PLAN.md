# Phase F9 — Utilidades offline

**Status:** AUTOMATED CLOSURE PASS  
**Branch:** `feat/f9-offline-utilities`  
**Base:** F8 closure `7e092166b04d72e2b8d4d1ba2d95d48e1acdebb3`

## Objective

Deliver focused, offline PDF utilities without creating a generic mega-workspace or introducing a second PDF engine without measured need.

## Approved slices and final disposition

1. **F9.1 watermark + page/Bates numbering** — user-facing text watermark and numbering/Bates commands delivered; transactional Save As and reopen validation proven. Image watermark remains intentionally unexposed because F9 has no transactional product image-overlay writer.
2. **Authorized protection** — PDFium creation gate NO-GO; exactly one local fallback selected: qpdf 12.4.2. User-facing AES-256 open-password protection delivered. Permission toggles remain intentionally absent.
3. **Optimize/repair gates** — structural/lossless optimize characterization GO with representative preservation evidence. Generic Repair NO-GO; no placebo repair UI. Task 8 exposes no WPF Optimize command.
4. **Preservation/hardening/automated closure** — complete at the automated level; manual Windows/external-reader/private-corpus QA remains separately NOT RUN.

## Execution rules retained

- KISS/YAGNI.
- PDFium first, exact pinned `bblanchon.PDFium.Win32 156.0.8076`.
- qpdf 12.4.2 is the single narrowly scoped F9 sidecar justified by the measured PDFium encryption-creation gap.
- DejaVu Sans 2.37 remains the only pinned fallback font.
- Unsupported glyphs fail closed.
- Save As only; source stays untouched.
- No runtime network/cloud/API keys/accounts.
- No pdfcpu/second protection fallback.
- No merge to `main` without explicit approval.
- Manual Windows/external-reader/private-corpus QA remains NOT RUN unless actually executed.

## Automated evidence

- Task 5 WPF utility checkpoint: `35f904d2d947f61d5bfc7b142d25df5d0588c20f`; exact-head CI recorded 920/920 PASS.
- Task 6 protection capability checkpoint: `b616d664fcf9f0990d80b6de34b8f880f8c53b71`; exact-head CI recorded 922/922 PASS.
- Task 7 authorized protection checkpoint: `1e864a295133a8319c08927dd0026989fd38f207`; functional hardening reached 937/937 PASS before checkpoint closure.
- Task 8 checkpoint: `1eb4c2e580a158a544356a76358481a0259002f0`; exact-head CI run `38117535742`, job `114405329029`, Release build 0 warnings / 0 errors, 942/942 PASS.
- Task 9 closure-candidate head: `9f48c0a3c6c9358ea7c9c37a1a9f5973921f5b1d`; workflow `38118308492`, job `114407617996`, Release build 0 warnings / 0 errors, 942/942 PASS.

## F8→F9 scope audit

Base: `7e092166b04d72e2b8d4d1ba2d95d48e1acdebb3`  
Functional head: `3cf85840d26098048c57be2383ad11297875a990`

- 78 commits ahead / 0 behind;
- exact F8 closure merge base;
- no `packages.lock.json` change;
- no cloud/account/API-key/runtime-service layer;
- qpdf 12.4.2 is the only new runtime sidecar and is pinned/licensed/staged with verified digest;
- product runtime remains offline.

## Closure integration rule

The status/documentation commit that marks this phase `AUTOMATED CLOSURE PASS` must itself receive a fresh exact-head Windows CI pass before the stacked draft PR is considered closed. After that:

- create/maintain stacked draft PR `feat/f8-comments-v1` → `feat/f9-offline-utilities`;
- verify PR draft/open/unmerged;
- verify `main` unchanged;
- stop F9.

F10 OCR requires a later explicit continuation.

Canonical executable plan: `docs/superpowers/plans/2026-10-11-f9-offline-utilities.md`.
