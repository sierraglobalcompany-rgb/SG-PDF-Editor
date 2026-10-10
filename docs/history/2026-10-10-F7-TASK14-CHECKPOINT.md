# SG PDF Editor — F7 Task 14 Automated Closure — Checkpoint

Fecha: 2026-10-10

## Estado al crear este checkpoint

**F7 Task 14 — CLOSURE FINAL CHECKS IN PROGRESS**

Este documento congela la auditoría y reconciliación documental antes del gate exact-head final y del draft PR. No afirma QA manual Windows.

## Rama y base

- Repo: `sierraglobalcompany-rgb/SG-PDF-Editor`
- Rama: `feat/f7-text-v1`
- Base stacked: `feat/f6-images`
- Base exacta F6: `c6d762efca01d50bfe3932d1f05617190a464fc6`
- Inicio Task 14 / Task-13 checkpoint: `b31a75cf99207e2e6ac9072b50c5a1ac5fa32d05`
- `main` al iniciar Task 14: `31c0594758a83ec555d73ecdd7c597cdf8791fd7`

## Evidencia heredada válida antes del commit documental

Task-13 exact-head Windows CI:

- workflow `38089770428`
- job `114323641551`
- locked restore: PASS
- Release build: PASS
- warnings: 0
- errors: 0
- tests: **766 PASS / 0 FAIL / 0 SKIPPED**

## Auditoría F6 → F7

Comparación:

`c6d762efca01d50bfe3932d1f05617190a464fc6` → `b31a75cf99207e2e6ac9072b50c5a1ac5fa32d05`

Resultado:

- **100 commits ahead**;
- **0 behind**;
- merge base = cierre F6 exacto.

### Dependencias / runtime

- No se añadió ningún NuGet nuevo.
- No cambió ningún package lockfile.
- `src/SGPdf.App/SGPdf.App.csproj` mantiene exactamente:
  - `bblanchon.PDFium.Win32 156.0.8076`;
  - `PDFsharp 6.2.4`.
- El único delta del `.csproj` es copiar `DejaVuSans.ttf` a `fonts/DejaVuSans.ttf` en build/publish.
- No existe descarga runtime del font.
- No se añadió network/cloud/service/account/API-key layer.

### Motor PDF

- PDFium continúa como único motor del editor.
- `PdfEditWriter` usa los bindings PDFium del proyecto, incluido `FPDFText_LoadCidType2Font` para fallback.
- PDFsharp conserva su propósito histórico de composición de etiquetas/fixtures; no se usa como motor alterno de EDITAR.

### Asset nuevo permitido

Único asset runtime nuevo de F7:

`third_party/fonts/dejavu/DejaVuSans.ttf`

Manifest:

- version: 2.37;
- archive SHA-256: `5c6e497a2f36552cb5ffb112c413a6af39c0f3c47653662b90b4fa6499822fd7`;
- TTF SHA-256: `7da195a74c55bef988d0d48f9508bd5d849425c1770dba5d7bfc6ce9ed848954`;
- size: 757076 bytes;
- license/notices: `third_party/licenses/DejaVu-Fonts.txt`;
- runtime download: none.

## Requirement gate

- `TEXT-01` — **AUTO PASS**.
- `TEXT-02` — **AUTO PASS**.
- `TEXT-03` — **AUTO PASS**.
- `TEXT-04` — **AUTO PASS**.

Evidence is distributed across the task checkpoints plus the Task-13 full exact-head suite. Detailed mapping is reconciled in `.planning/REQUIREMENTS.md` and `.planning/phases/08-f7-text/PLAN.md`.

## Preservación

`docs/history/2026-10-10-F7-preservation-matrix.md` is based on the real combined F7 writer, not copied from F6.

Representative proven-preserved structures:

- AcroForm + field value;
- bookmarks;
- named destination + internal link;
- tagged / StructTreeRoot;
- page labels;
- embedded attachment + exact payload;
- metadata;
- page rotation.

Signature/password-opened sources remain Block.

## Docs reconciled by this checkpoint commit

- `.planning/STATE.md`
- `.planning/ROADMAP.md`
- `.planning/REQUIREMENTS.md`
- `.planning/phases/08-f7-text/PLAN.md`
- `docs/ROADMAP.md`
- `docs/history/2026-10-10-F7.md`
- `docs/history/2026-10-10-F7-TASK14-CHECKPOINT.md`

`third_party/manifest.json` was audited and already contains the required F7 font provenance/license/hash/runtime policy, so no Task-14 manifest edit is necessary.

## Manual QA

**NOT RUN / NOT CLAIMED.**

Automated PASS does not substitute for real Windows interaction/offline/large-document testing.

## Remaining exact Task-14 gate after this commit

1. exact-head Windows CI: hygiene + Labelize staging + locked restore + Release build + full suite;
2. create draft PR `F7 — Texto V1`, base `feat/f6-images`, head `feat/f7-text-v1`;
3. require PR CI GREEN on the expected head/merge ref;
4. verify PR remains draft/open/unmerged;
5. verify `main` remains `31c0594758a83ec555d73ecdd7c597cdf8791fd7`;
6. STOP before F8; no merge.
