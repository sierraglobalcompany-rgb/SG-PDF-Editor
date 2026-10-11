# Phase F9 — Utilidades offline

**Status:** AUTOMATED CLOSURE CANDIDATE — Task 9 exact-head CI pending  
**Branch:** `feat/f9-offline-utilities`  
**Base:** F8 closure `7e092166b04d72e2b8d4d1ba2d95d48e1acdebb3`

## Objective

Deliver focused, offline PDF utilities without creating a generic mega-workspace or introducing a second PDF engine without measured need.

## Approved slices and disposition

1. **F9.1 watermark + page/Bates numbering** — user-facing text watermark and numbering/Bates commands delivered; transactional Save As and reopen validation proven. Image watermark remains intentionally unexposed because F9 has no transactional product image-overlay writer.
2. **Authorized protection** — PDFium creation gate NO-GO; exactly one local fallback selected: qpdf 12.4.2. User-facing AES-256 open-password protection delivered. Permission toggles remain intentionally absent.
3. **Optimize/repair gates** — structural/lossless optimize characterization GO with representative preservation evidence. Generic Repair NO-GO; no placebo repair UI. Task 8 exposes no WPF Optimize command.
4. **Preservation/hardening/automated closure** — Tasks 1–8 GREEN; Task 9 is reconciling state/requirements/history and running final exact-head CI before opening the stacked draft PR.

## Execution rules

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

## F8→F9 scope audit before Task 9 docs

Base: `7e092166b04d72e2b8d4d1ba2d95d48e1acdebb3`  
Functional head: `3cf85840d26098048c57be2383ad11297875a990`

- 78 commits ahead / 0 behind;
- exact F8 closure merge base;
- no `packages.lock.json` change;
- no cloud/account/API-key/runtime-service layer;
- qpdf 12.4.2 is the only new runtime sidecar and is pinned/licensed/staged with verified digest;
- product runtime remains offline.

## Current task

**Task 9 — F9 hardening and automated closure.**

Remaining sequence:

1. commit closure-candidate planning/history reconciliation;
2. run fresh Windows CI on that exact head;
3. only if GREEN, mark F9/UTILS AUTO PASS and record final exact-head evidence;
4. run fresh CI on the final status commit;
5. create stacked draft PR `feat/f8-comments-v1` → `feat/f9-offline-utilities`;
6. verify PR remains draft/open/unmerged and `main` unchanged;
7. STOP F9.

Canonical executable plan: `docs/superpowers/plans/2026-10-11-f9-offline-utilities.md`.
