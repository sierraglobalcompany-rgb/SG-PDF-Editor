---
gsd_state_version: '1.1'
status: ui4-fail-p1-signature-root-cause-trace-pending-f13-ab-approved
progress:
  closed_baseline_phases: 13
  expanded_roadmap_phases: 19
  current_phase: UI-4 Gate 0 stabilization before F13
---

# Project State — SG PDF Editor

**Updated:** 2026-10-15

## 1. Mandatory laws

1. **KISS:** simple, efficient, optimized, effective; A→B.
2. **Anti-noise:** `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
3. **Anti-patch:** `REPRODUCE → TRACE/CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.

`main` is read-only at `31c0594758a83ec555d73ecdd7c597cdf8791fd7`. No merge without explicit user approval.

## 2. Functional baseline F0–F12 — CLOSED

Authoritative functional product anchor: `8e1845d6d8fe72374d7a80994d8500b6d36077cc`.
Historical CI: run `38555829715`, job `115824598759`, tests `1202/1202 PASS`.

Do not reopen F0–F12 without a reproducible regression/new explicit requirement.

## 3. Release/UI branch — Gate 0 BLOCKED

Branch: `feat/release-ui-modernization`  
UI-4 tested HEAD: `abd52d05279e3e656723ad4ff5a21d36de91508e`  
Automated suite: `1214/1214 PASS` before manual UI-4.

### UI-4 result received and audited

Windows environment:
- Windows 11 Home Single Language x64 build `10.0.26300`;
- 1920×1200 @100%;
- .NET SDK 10.0.402.

Results:
- V01 PASS
- V02 PASS after local Dark Mode strip fix
- V03 PASS
- V04 NOT RUN
- V05 NOT RUN
- V06 PASS
- V07 PASS
- V08 FAIL-P2 / classification pending
- V09 NOT RUN visually; source functional path already covered by protected-PDF tests
- V10 FAIL-P1
- V11 FAIL-P1
- V12 FAIL-P1
- V13 NOT RUN
- V14 FAIL-P1

**FINAL_UI4_STATUS: FAIL-P1**.

The received UI4 report confirms symptoms but does **not** prove the V10–V12 root cause.

Latest authoritative checkpoint:
`docs/history/2026-10-15-ui4-result-audited-signature-trace-next.md`

Source/test audit:
`docs/history/2026-10-15-gate0-source-test-audit-before-f13.md`

### Local Windows diff to preserve

Local branch reported: `feat/fix-ui4-dark-mode-strip`.
Uncommitted intentional diff: `src/SGPdf.App/MainWindow.Sign.cs`.
Purpose: replace light-only mode-strip visuals with `SgToolbarButtonStyle` / `SgToolbarBorderStyle`.
Build PASS, tests `1214/1214`, `git diff --check` clean.
No push/main/PKCS.

### Next Windows task — narrow causal signature trace only

Use downloadable prompt generated in chat:
`SG_PDF_CODEX_UI4_SIGNATURE_ROOT_CAUSE_TRACE_2026-10-15.md`

The trace must distinguish:
1. shared panel/hit-test failure;
2. handler not reached;
3. delegate/default seam not reached;
4. `ShowDialog()` reached but modal hidden/off-screen/behind owner;
5. exception captured.

Mandatory comparison control: `Cargar PNG transparente...`, because it shares `SignaturePropertiesPanel`.

No signature product fix is accepted until `ROOT_CAUSE` is proven.

### V08 contract

Existing tests establish:
- Export PDF enabled only with valid `LabelLayoutPlan` and not busy;
- Print additionally requires `Thermal`.

Next Windows trace records `MediaKind`, plan/null, busy, both enabled states and validation text. Do not force-enable controls.

### Gate 0 exit

Gate 0 becomes GREEN only when:
- V10–V12 root cause resolved/reclassified and physically reverified;
- Edit/Save manual path classified;
- V08 classified;
- V09 visually verified;
- V04 1366×768 run;
- V05 scaled profile run;
- V13/V14 completed;
- no P0/P1;
- focused cause tests + full Release suite PASS;
- exact-head CI GREEN.

## 4. PKCS hygiene — GREEN isolated

Branch: `feat/fix-pkcs-hygiene`  
HEAD: `781cd6060121367f4d51efbe2724d820d1dfb842`  
CI run `38645380308`, job `116130807039`, tests `1214/1214 PASS`.

PKCS 8.0.2 is pinned only in the test graph. App/runtime unchanged. Not integrated into release/UI branch yet.

## 5. F13 — Product Shell / Dashboard — A+B APPROVED

Planning branch: `plan/post-ui4-f13-f18-expansion-20261014`.
Planning branch contains docs/planning only; no F13 product implementation yet.

A — visual spec:
`docs/superpowers/specs/2026-10-15-f13-approved-mockup-shell-design.md`

B — technical implementation plan:
`docs/superpowers/plans/2026-10-15-f13-approved-mockup-shell.md`

Mockup identity:
`docs/design/2026-10-15-f13-approved-mockup-reference.md`

A+B are cumulative, not alternatives.

Frozen design:
- Home/dashboard inspired by approved mockup;
- 96 px Home/document rail;
- 24 px Home padding;
- left rail + center document + 320 px contextual right pane;
- icon + text navigation;
- local/offline; no login/ads/cloud/AI;
- Light/Dark baseline;
- `PDF a PNG` honest current label; JPG remains F16;
- official app icon remains source-gated;
- no second PDF engine/writer/UI framework.

Preflight corrections already applied:
- F13 owns shell/core modes only; F14 owns full capability productization;
- no invalid `EventHandler<string>`;
- F13 preserves current Edit Save semantics; dirty-gating belongs to F15;
- `FindModePanel()` dependencies must be removed safely before legacy strip deletion;
- preserve existing control names on replacement controls where safe to avoid test churn.

**Do not start F13 product code before Gate 0 GREEN.**

## 6. F14 — Productize existing capabilities — PREFLIGHT IN PROGRESS

Plan:
`docs/superpowers/plans/2026-10-14-f14-productize-existing-capabilities.md`

Inventory:
`docs/product/2026-10-14-existing-tool-entrypoint-inventory.md`

New preflight finding to incorporate before execution:
- some legacy mode-safety guards are attached to old mouse/menu UI rather than fully contained in the functional owner entrypoint;
- example: COMENTAR dirty protection uses `PreviewMouseLeftButtonDown` on old mode buttons;
- `CanRunUtility()`, `CanStartBatch()` and `CanStartCryptographicSigning()` do not themselves cover every active mode such as `_commentModeActive`;
- therefore the new shell must use one **minimal explicit safe-transition helper** that invokes existing owner guards before routing actions; it must not duplicate writer/business logic.

No F14 code is being written while Gate 0 is open.

## 7. F15 — Edit V3 — DESIGN APPROVED / SPEC READY

Approved approach: unified UX over existing engine.

Spec:
`docs/superpowers/specs/2026-10-15-f15-edit-v3-design.md`

Preserve:
- `ImageEditWorkspace`;
- `TextEditWorkspace`;
- `RedactionWorkspace`;
- `PdfEditWriter`.

Frozen UX:
- direct text/image selection;
- contextual right panel;
- `Guardar como...` always visible in Edit;
- F15 target semantics: no dirty → disabled, dirty → enabled;
- save creates copy and original remains active;
- image Undo/Redo reused;
- no fake universal history for text/redaction.

F15 implementation plan waits for F13 shell stability.

## 8. F16–F18

- **F16:** PDF→JPG, Image→PDF, Crop, authorized Unlock, safe Optimize, image watermark after capability gate.
- **F17:** Compare V2, more AcroForm types/simple designer, signature UX completion.
- **F18:** optional LibreOffice→PDF, scanner, local HTML→PDF after separate gates.

## 9. Official application icon — REQUIRED / SOURCE-GATED

Do not synthesize from the mockup placeholder or text description.
Required later on window, exe, Alt+Tab/taskbar, shortcut and installer.

## 10. Immediate resume order

1. Run/audit `SIGNATURE_TRACE_RESULT` from the narrow Windows prompt.
2. Accept a fix only with demonstrated root cause.
3. Focused cause test → full suite → exact-head CI.
4. Rerun V10–V14 and remaining V04/V05/V08/V09.
5. Close UI-4 only with no P0/P1.
6. Consolidate release truth + isolated PKCS cleanly.
7. Execute approved F13 A+B natively.
8. Use Codex only for actual Windows/manual gates.
9. Execute F14 with explicit safe mode transitions, not direct guard-bypassing routes.
10. F15 after F13 stability and implementation-plan approval.
11. Publish/installer only after intended product baseline is stable.
12. `main` only with explicit user approval.
