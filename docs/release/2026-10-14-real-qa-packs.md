# Release real-QA packs — 2026-10-14

Purpose: execute the remaining manual/private/physical evidence in bounded packs after the automated F0–F12 baseline closed. This file is a gate/checklist, not evidence that the checks have run.

## Rules

- Automated PASS never upgrades a manual/private/physical item to PASS.
- Use representative non-customer fixtures when possible; private customer material stays ignored/local and is never committed.
- Record exact app build/HEAD, Windows version, device/model where relevant, source fixture identity/hash when safe, result and defect severity.
- P0 = data loss/security/source corruption/crash blocking primary flow. P1 = primary feature wrong/unusable without safe workaround. Release cannot proceed with open P0/P1.
- A failure is triaged root-cause-first; do not patch the checklist or add architecture to hide it.
- `main` remains read-only until explicit approval.

## Pack A — Reader / Organize / Editing

Representative corpus: text-heavy PDF, image-heavy PDF, mixed portrait/landscape PDF, bookmarks/links PDF, protected-open PDF where authorized.

- [ ] Open, navigate, zoom, fit, search and select text in Windows UI.
- [ ] Print to Microsoft Print to PDF.
- [ ] Organize: reorder, rotate, delete, duplicate, insert/merge, extract/split and reopen output.
- [ ] Edit representative image and text cases, Save As, reopen in SG PDF Editor.
- [ ] Open saved outputs in at least one external reader (Adobe/Foxit/Edge when available).
- [ ] Confirm original input files are unchanged for Save-As flows.
- [ ] Run with network disabled for normal local workflows.

Exit: no P0/P1; any preservation limitation is documented, reproducible and within approved scope.

## Pack B — ZPL / QR / physical print-scan

Representative corpus: synthetic labels plus ignored private Mercado Libre fixtures when available.

- [ ] Open/preview representative `.zpl/.txt/.prn`.
- [ ] Verify one-each/custom quantity/layout behavior.
- [ ] Thermal print at intended physical size; measure with ruler.
- [ ] Scan representative barcode/QR from printed output.
- [ ] Generate QR PNG from SG PDF Editor and scan with at least one real phone/scanner.
- [ ] Verify no customer fixture is tracked by Git.

Exit: no P0/P1; physical size and scanability acceptable on representative hardware. Labelize font/license provenance remains a separate installer gate.

## Pack C — visual + cryptographic signatures

Representative inputs: normal PDF, signed-PDF warning case, representative PNG/photo signature, stylus/mouse drawing, representative local PFX/P12 test certificate.

- [ ] Import transparent PNG, place/move/resize, Save As, reopen.
- [ ] Photo/scan cleanup quality check on real capture.
- [ ] Draw with mouse and, when available, stylus/touch; verify usable ink result.
- [ ] Local signature library add/reuse/remove behavior.
- [ ] PFX/P12 signing with representative certificate; verify output in SG PDF Editor and at least one external reader.
- [ ] Confirm password remains memory-only and cancellation leaves source/destination safe.

Exit: no P0/P1; no claim of TSA/smartcard/HSM/legal qualification beyond approved baseline.

## Pack D — OCR / Forms / Compare / Batch / PDF→PNG

Representative corpus: low/medium-quality scans, Spanish/English/bilingual, classic `/Tx` form, compare pairs (identical/text/visual/page-count), multi-file batch, portrait+landscape PDF→PNG.

- [ ] OCR searchable output on real scan; reopen/search in SG PDF Editor and external reader.
- [ ] Existing `/Tx` edit/save/reopen on representative form.
- [ ] Compare reports expected signals on real document pairs.
- [ ] Batch text watermark processes multiple files, reports per-item result and protects originals.
- [ ] PDF→PNG visual review: every page exported, orientation/dimensions visually sensible, no silent overwrite.
- [ ] Cancellation/error paths leave originals intact.

Exit: no P0/P1; known scope limits remain explicit (no universal OCR/form/compare/conversion fidelity claim).

## Evidence record template

For each executed pack, append a short record to a dated local/release note only after the checks actually run:

- Pack:
- Date:
- Exact branch/HEAD:
- Windows build:
- Hardware/external reader:
- Fixtures used:
- PASS items:
- FAIL items + severity:
- Defect links/commits:
- Final gate: PASS / FAIL / PARTIAL / NOT RUN

## Current status

- Pack A: **NOT RUN**
- Pack B: **NOT RUN**
- Pack C: **NOT RUN**
- Pack D: **NOT RUN**

The automated baseline remains valid, but release readiness must not treat these gates as completed until real evidence exists.
