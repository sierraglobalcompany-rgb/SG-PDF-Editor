# F6 Task 10 — Closure Checkpoint

Fecha: 2026-10-10  
Rama: `feat/f6-images`  
Task: **F6 closure — docs, audit and stacked draft PR**  
Estado al crear este checkpoint: **FUNCTIONAL GREEN; docs-head CI + PR gates pendientes**

## Base de Task 10

- F5 exact base: `327d7064c14131e603e3bce6947b593a10f46363`
- F6 functional head: `7f70a68b2148504888040483a4d9a9e9dd6b4a03`
- Task-9 checkpoint head: `e1dfc5e41b17fa8444a02ebab7cbfabce741955f`
- `main`: `31c0594758a83ec555d73ecdd7c597cdf8791fd7`
- F6 PR antes de 10.6: no existe
- merge: no realizado

## 10.1 — Fresh exact functional-head verification

Se rerunó el job Windows sobre el **functional head exacto** `7f70a68b2148504888040483a4d9a9e9dd6b4a03`.

Workflow:

- run `38057258528`
- attempt 2
- checkout exact SHA: PASS
- repository hygiene: PASS
- Labelize pinned staging: PASS
- locked restore: PASS
- Release build: PASS — **0 warnings / 0 errors**
- tests: **680 PASS / 0 FAIL / 0 skipped**

Este rerun es la evidencia fresca de Task 10; no se reutilizó de forma ciega el resultado anterior de Task 9.

## 10.2 — Audit F5 → F6

Comparación antes de docs:

- base `327d7064c14131e603e3bce6947b593a10f46363`
- head `e1dfc5e41b17fa8444a02ebab7cbfabce741955f`
- **64 commits ahead / 0 behind**
- merge base exacto = F5 closure

Hallazgos:

- no cambios `.csproj`;
- no cambios de package lockfiles;
- no segundo motor PDF;
- no runtime network/cloud/service/account/API-key layer;
- no generic PDF object graph;
- no DI/MVVM framework nuevo;
- no database/autosave subsystem;
- no refactor ajeno de F5/F4;
- integración Reader limitada a EDITAR/dirty guards;
- código F6 concentrado en `Features/Edit/Images`, `MainWindow.EditImages*` y primitivas PDFium/writer específicas;
- PDFium permanece bajo el gate global del proyecto.

No se encontró defecto crítico/alto de producto en el cierre automatizado.

## 10.3 — Requirement reconciliation

- `IMG-01`: **AUTO PASS** — discovery/select/context actions.
- `IMG-02`: **AUTO PASS** — extraction + PNG/JPEG replacement + geometry/lifetime evidence.
- `IMG-03`: **AUTO PASS** — move/resize/rotate/delete + opacity + z-order. Ambos optional gates pasaron en el runtime pinneado.
- `IMG-04`: **AUTO PASS** — undo/redo/history/dirty integration.

No se usa PARTIAL para IMG-03 porque Task 7 probó opacidad y z-order con save/reopen/render y sin fallback destructivo.

## 10.4 — Manual QA

Continúa **NOT RUN**. La suite automatizada no se presenta como sustituto de:

- zoom selection;
- rotated/overlap UX;
- drag/resize/rotate feel;
- keyboard/mode switching;
- Save As dialogs;
- external PNG inspection;
- real PNG/JPEG replacement assets;
- alpha/opacity/z-order visual UI;
- heavy PDF responsiveness;
- physically network-disabled smoke;
- real LEER/FIRMAR/ORGANIZAR/ZPL regression.

## Preservation

Matriz: `docs/history/2026-10-09-F6-preservation-matrix.md`.

Representative F6-writer evidence:

- forms → `ProvenPreserved / Info`
- bookmarks → `ProvenPreserved / Info`
- named destinations → `ProvenPreserved / Info`
- internal links → `ProvenPreserved / Info`
- tagged structure → `ProvenPreserved / Info`
- page labels → `ProvenPreserved / Info`
- attachments → `ProvenPreserved / Info`
- representative metadata values → `ProvenPreserved / Info`
- cryptographic signature → `Unknown / Block`
- password-opened source → `Unknown / Block`

## Optional capability verdict

- opacity: **PROVEN AVAILABLE / product path GREEN**
- z-order exact-index insertion: **PROVEN AVAILABLE / product path GREEN**
- no second engine or raster-overlay fallback added

## Task 9 hardening retained

- dirty guards protect Ctrl+O and menu Open PDF/ZPL;
- writer cancellation/publication cleanup characterized GREEN;
- bitmap copy validates format/stride/sizes and applies a **256 MiB** combined managed-copy budget before allocation;
- active-page-only discovery is locked by test;
- persistent image workspace models contain no `IntPtr`/`UIntPtr` native handles;
- replacement assets use captured managed bytes.

## Docs reconciled in this Task 10 commit

- `.planning/STATE.md`
- `.planning/ROADMAP.md`
- `.planning/REQUIREMENTS.md`
- `.planning/phases/07-f6-images/PLAN.md`
- `docs/history/2026-10-09-F6.md`
- this checkpoint

`docs/superpowers/specs/2026-10-09-f6-images-design.md` remains the frozen design record; current execution status is intentionally sourced from STATE/ROADMAP/REQUIREMENTS/history/checkpoints instead of rewriting the design narrative after implementation.

## Gates después de este commit

1. require exact closure-docs head push CI PASS;
2. create stacked **draft PR** base `feat/f5-organize`, head `feat/f6-images`, title `F6 — Imágenes`;
3. require exact-head PR CI PASS;
4. verify PR draft/open/unmerged;
5. verify `main` remains `31c0594758a83ec555d73ecdd7c597cdf8791fd7`;
6. record CI/PR run IDs in PR conversation if needed rather than changing git head only to cite its own CI;
7. STOP F6 — do not start F7.

## NEXT_EXACT_TASK after closure

After the above gates pass and the user later says continue:

> **F7 — Text V1 design/spec only.**

No F7 implementation without a dedicated approved design and TDD plan.
