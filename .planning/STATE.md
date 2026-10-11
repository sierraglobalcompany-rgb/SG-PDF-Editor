---
gsd_state_version: '1.0'
status: f9-automated-closure
progress:
  total_phases: 13
  completed_phases: 10
  total_plans: 13
  completed_plans: 10
  percent: 77
---

# Project State

## Project Reference
See `.planning/PROJECT.md`.

> Numeric GSD progress is orientation only. Automated, private and physical gates are tracked independently. GitHub exact heads/CI + this status are authoritative for executed work.

**Core value:** Resolver PDF + ZPL diario de forma rápida, privada, estable y offline.  
**Current focus:** **F9 Utilidades offline — automated closure PASS.** User-facing text watermark, page/Bates numbering and authorized open-password protection are delivered. Optimize is capability-GO with representative preservation evidence but has no WPF command in F9; generic Repair is NO-GO and intentionally has no UI. Manual Windows/external-reader/private-corpus QA remains **NOT RUN**.

## Stable gates

- F3.4: `feat/f3-4-local-signature-library`, closure `1bef751962e0b4aaf35fbda9b8a1a9a2ee2ba36b`, draft PR #22; real Windows QA **NOT RUN**.
- F4: `feat/f4-full-reader`, closure `1d620f1b2b9717aab35671e18a9dc78f28a8afdd`, draft PR #23; automated closure PASS; real Windows reader QA **NOT RUN**.
- F5: `feat/f5-organize`, closure `327d7064c14131e603e3bce6947b593a10f46363`, stacked draft PR #24; automated closure PASS; real Windows organize QA **NOT RUN**.
- F6: `feat/f6-images`, closure `c6d762efca01d50bfe3932d1f05617190a464fc6`, stacked draft PR #25 open/unmerged; automated closure PASS; real Windows image-edit QA **NOT RUN**.
- F7: `feat/f7-text-v1`, closure `f4cd27a8e752071ca2ca4879b1eab84d52d188eb`, stacked draft PR #26 open/draft/unmerged; automated closure PASS; real Windows text-edit QA **NOT RUN**.
- F8: `feat/f8-comments-v1`, closure `7e092166b04d72e2b8d4d1ba2d95d48e1acdebb3`, stacked draft PR #27 open/draft/unmerged; automated closure PASS; real Windows/stylus/touch/external-reader QA **NOT RUN**.
- F9: `feat/f9-offline-utilities`; automated closure PASS. Task 8 checkpoint `1eb4c2e580a158a544356a76358481a0259002f0`; Task 9 closure-candidate exact-head `9f48c0a3c6c9358ea7c9c37a1a9f5973921f5b1d` passed Windows CI with 942/942 tests. Branch remains unmerged.

## F9 automated closure evidence

Closure-candidate exact-head verification:

- commit `9f48c0a3c6c9358ea7c9c37a1a9f5973921f5b1d`;
- workflow `38118308492`;
- job `114407617996`;
- repository hygiene: PASS;
- Labelize 1.7.0 staging: PASS;
- qpdf 12.4.2 staging: PASS;
- locked restore: PASS;
- Release build: **0 warnings / 0 errors**;
- tests: **942 passed / 0 failed / 0 skipped**.

The final status/documentation commit that marks F9 AUTO PASS must itself remain exact-head green before the stacked draft PR is considered closed; this is an integration verification step, not new product scope.

## F9 delivered/characterized facts

### User-facing utilities delivered

- Text watermark under `Herramientas` with page scope, anchors, size, opacity, rotation, layer and RGB; Save As only.
- Page numbering / Bates under `Herramientas` with start number, prefix/suffix, zero padding, selected range, six anchors, size, opacity, layer and RGB; Save As only.
- Authorized open-password protection under `Herramientas` using AES-256 qpdf output after exact pinned PDFium encryption-creation capability proved NO-GO.
- Protection requires explicit authorization; password-opened sources require the current source password again; cryptographically signed sources are blocked.

### Capability decisions intentionally not over-claimed

- Image-watermark native primitives were characterized GO, but no product image-watermark command is exposed because no F9 transactional image-overlay writer was implemented.
- Print/copy/edit permission flags are not exposed; F9 does not claim DRM semantics.
- Structural/lossless qpdf optimize characterization is **GO** and preserves the representative F9 matrix; size reduction is not promised. Task 8 exposes no WPF Optimize command.
- Generic Repair is **NO-GO** for F9: one deterministic damaged fixture recovered with warnings while severe truncation remained unrecoverable. No Repair UI is exposed.

### F9 preservation evidence

Fresh qpdf optimize evidence preserves representative:

- AcroForm;
- bookmarks;
- named destinations;
- internal links;
- tagged structure;
- page labels;
- attachment name + exact payload;
- title/author metadata;
- page rotation;
- supported existing annotation;
- unsupported representative annotation as preserved/read-only.

Canonical matrix: `docs/history/2026-10-11-F9-preservation-matrix.md`.

## F8→F9 scope audit

Base: `7e092166b04d72e2b8d4d1ba2d95d48e1acdebb3`  
Functional head: `3cf85840d26098048c57be2383ad11297875a990`

- **78 commits ahead / 0 behind**;
- merge base is exactly the F8 closure;
- no `packages.lock.json` change;
- qpdf 12.4.2 is the single new F9 runtime sidecar, adopted only after executable PDFium capability evidence;
- qpdf is pinned by official Windows archive SHA-256 and Apache-2.0/NOTICE files;
- no pdfcpu or second fallback is maintained;
- no runtime cloud/account/API-key/network service was added;
- development/CI stages qpdf from its verified archive; product runtime does not download it;
- `main` remains frozen and merge still requires explicit approval.

## Manual/private/physical gates still open

- F0 real Windows UI/print/offline: **NOT RUN**.
- F1 private Mercado Libre corpus: **NOT RUN**.
- F2 private corpus + thermal/ruler/scanner: **NOT RUN**.
- F3 real Windows/photo/hardware/library QA: **NOT RUN**.
- F4 real Windows reader QA: **NOT RUN**.
- F5 real Windows organize QA: **NOT RUN**.
- F6 real Windows image-edit UX: **NOT RUN**.
- F7 real Windows text-edit UX/offline/cross-mode smoke: **NOT RUN**.
- F8 real Windows comment UX, mouse/stylus/touch and Adobe/Foxit/Edge interop: **NOT RUN**.
- F9 real Windows utility-dialog UX/Save As smoke and Adobe/Foxit/Edge reopen/interoperability: **NOT RUN**.
- F9 private malformed-PDF repair corpus: **NOT RUN**.

## Runtime / architecture through F9

- Windows x64 / C# / .NET 10 / WPF.
- PDFium remains the primary reader/editor engine; native calls use `PdfiumRuntime.NativeGate`.
- Pinned PDFium package remains `bblanchon.PDFium.Win32 156.0.8076`.
- qpdf 12.4.2 is a narrowly scoped offline sidecar for F9 protection and optimize/repair characterization, not a second general editor architecture.
- PDFsharp remains labels/synthetic-fixture support, not a general editor engine.
- Labelize 1.7.0 remains the ZPL runtime renderer.
- DejaVu Sans 2.37 remains the pinned offline text fallback asset; runtime never downloads it.
- Save As protects originals; utility writers use conservative validation/publication semantics.
- No merge to `main` without explicit user approval.

## Next Gate

```text
verify this final F9 status commit with fresh exact-head Windows CI
→ create/maintain stacked draft PR: feat/f8-comments-v1 → feat/f9-offline-utilities
→ verify PR draft/open/unmerged + main unchanged
→ STOP F9

manual Windows/external-reader/private-corpus QA remains a separate NOT RUN gate
F10 OCR requires a later explicit continuation
```

## Continuity

- F9 design: `docs/superpowers/specs/2026-10-11-f9-offline-utilities-design.md`.
- F9 TDD plan: `docs/superpowers/plans/2026-10-11-f9-offline-utilities.md`.
- F9 phase plan: `.planning/phases/10-f9-utilities/PLAN.md`.
- F9 preservation matrix: `docs/history/2026-10-11-F9-preservation-matrix.md`.
- F9 closure history: `docs/history/2026-10-11-F9.md`.
- Task 8 checkpoint: `docs/history/2026-10-11-F9-TASK8-OPTIMIZE-REPAIR-PRESERVATION.md`.
