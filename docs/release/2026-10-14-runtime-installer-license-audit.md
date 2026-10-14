# SG PDF Editor — Runtime / Installer / License Audit

**Date:** 2026-10-14  
**Scope:** release engineering only; no product feature work.  
**Candidate:** `integration/release-candidate-20261014` @ `01d4adc3972ed939c5a2723a75d1833e61825d63`.

## 1. Authority and baseline

- F0–F12 automated functional baseline is CLOSED / feature-frozen.
- Authoritative functional implementation: `feat/f12-7-pdf-to-png-mvp-green3` @ `8e1845d6d8fe72374d7a80994d8500b6d36077cc`.
- Conservative integration candidate is a direct descendant of that functional HEAD plus release/planning documentation only.
- `main` remains read-only until explicit approval.
- Real/manual/private/physical Packs A–D remain NOT RUN where documented.

## 2. Installer / publish infrastructure audit

Repository-tree audit found no existing:

- WiX project or `.wxs`;
- Inno Setup `.iss`;
- MSIX packaging project;
- checked-in publish profile;
- generic installer framework;
- release/publish workflow separate from the normal Windows build workflow.

**Ruling:** do not add an installer framework before the published folder itself is reproducible and passes smoke. The KISS order is:

1. deterministic `win-x64` publish;
2. verify complete runtime payload;
3. clean/offline Windows smoke;
4. only then choose the smallest installer technology that adds real value.

This prevents debugging packaging and product runtime at the same time.

## 3. Current runtime dependency map

The existing project/build contracts already stage or copy the approved runtime pieces:

- PDFium via pinned NuGet package `bblanchon.PDFium.Win32 156.0.8076`;
- PDFsharp `6.2.4` via pinned NuGet package;
- DejaVu Sans pinned repo asset;
- Labelize `1.7.0` via `tools/setup-labelize.ps1`;
- qpdf `12.4.2` via `tools/setup-qpdf.ps1`;
- Tesseract `5.5.3.20260724` plus pinned `spa`/`eng` tessdata via `tools/setup-tesseract.ps1`.

Runtime setup scripts are development/CI staging mechanisms; the final product must not download dependencies while running.

## 4. Labelize `ZplGSCustom.ttf` provenance re-audit

### Verified facts

1. SG PDF's current `tools/setup-labelize.ps1` downloads the official `GOODBOY008/labelize` v1.7.0 Windows x64 release ZIP with pinned SHA-256 `cdd4030b0d1a8bad69b93f49866c8dcc5314af8975bb16a76991fe32f92dd21d`.
2. The script extracts and stages `labelize.exe`; SG PDF does **not** separately download or install `ZplGSCustom.ttf`.
3. Labelize v1.7.0 source embeds `src/assets/fonts/ZplGSCustom.ttf` using `include_bytes!` as `FONT_ZPL_GS` and uses it for the ZPL GS/symbol font face.
4. Labelize v1.7.0 `THIRD_PARTY_LICENSES.md` states that the 9-glyph FontForge-generated font was received from `ingridhq/zebrash` (MIT), carried over when the renderer was extracted, has no copyright string in its name table, and is treated by Labelize as distributable through that MIT lineage.
5. `ingridhq/zebrash` is published under MIT and its repository contains `internal/assets/fonts/ZplGSCustom.ttf`.
6. Labelize upstream explicitly states that the font's exact origin before zebrash is not documented.

A verbatim copy of Labelize v1.7.0's third-party notice is retained in:

`third_party/licenses/Labelize-THIRD-PARTY-NOTICES.md`

### Classification

**PROVENANCE RE-AUDIT: COMPLETE.**  
**ENGINEERING DISTRIBUTION EVIDENCE: SUPPORTIVE.**  
**ORIGINAL-AUTHOR PROVENANCE: NOT FULLY DOCUMENTED UPSTREAM.**

This is not a legal opinion and must not be described as absolute license certainty.

For a public production installer, use one of these two explicit resolutions:

- **A — accept documented residual risk:** keep the exact pinned official Labelize v1.7.0 binary and ship its MIT + third-party notices; or
- **B — eliminate the residual risk:** replace/redraw the tiny GS glyph asset in a controlled upstream/fork path, then rerun affected ZPL rendering/barcode regression and physical/private QA.

Do **not** silently replace fonts inside the shipped Labelize binary and do not introduce a second ZPL renderer.

Until A or B is explicitly resolved, the font item is **REVIEWED / RESIDUAL RISK OPEN**, not an unknown unaudited blocker and not an unconditional legal PASS.

## 5. License packaging requirements

Any public package must carry the notices required by the exact components actually distributed. At minimum review/package as applicable:

- Labelize MIT license;
- Labelize v1.7.0 third-party notices;
- PDFium/bblanchon notices;
- PDFsharp MIT;
- qpdf Apache-2.0 + NOTICE;
- Tesseract/tessdata Apache-2.0 notices;
- DejaVu/Bitstream Vera notice.

Do not ship historical/non-runtime license baggage as if it were a runtime dependency when it is not distributed.

## 6. Next KISS release-engineering gate

### Publish gate — NOT RUN

On a prepared Windows x64 development machine:

1. stage Labelize/qpdf/Tesseract with the existing pinned scripts;
2. locked restore;
3. Release build + full tests;
4. create one deterministic self-contained `win-x64` publish output (no new product dependency);
5. inventory the output and prove required runtime files are present;
6. launch from the published folder on Windows;
7. perform offline smoke with network disabled;
8. verify no runtime download, PATH/registry dependency or globally installed tool is required;
9. preserve source repo/working tree hygiene.

Do not add installer technology until this gate is GREEN.

### Installer gate — BLOCKED BY PUBLISH + REAL ENVIRONMENT

After publish gate GREEN:

- select one minimal Windows installer path;
- package the already-proven publish payload, not a second build pipeline;
- include required notices;
- test install / launch / uninstall on representative Windows;
- verify no P0/P1;
- do not auto-merge to `main`.

## 7. Status

- functional automated baseline: **PASS / FROZEN**;
- integration candidate selected: **PASS**;
- runtime dependency inventory: **PASS**;
- existing installer infrastructure: **NONE — expected, not a product defect**;
- Labelize font provenance re-audit: **COMPLETE / residual upstream-origin risk documented**;
- deterministic publish: **NOT RUN — LOCAL WINDOWS REQUIRED**;
- packaged-runtime smoke: **NOT RUN — LOCAL WINDOWS REQUIRED**;
- installer: **NOT STARTED BY DESIGN**;
- public release license decision for Labelize font: **OPEN — choose documented risk acceptance or replacement**;
- real Packs A–D: remain open as previously documented;
- `main`: unchanged/read-only.

## 8. Anti-noise decision

No new PDF engine, ZPL renderer, OCR engine, backend, database, packaging framework, runtime service or feature is justified by this audit.

The next technical work is **publish validation on the prepared Windows PC**, not more architecture.
