---
gsd_state_version: '1.1'
status: f12-functional-baseline-closed-release-ui-qa-open
progress:
  total_phases: 13
  completed_phases: 13
  functional_percent_estimate: 100
  release_readiness_percent_estimate: 87
  overall_percent_estimate: 94
---

# Project State

## Authority

Operational order:
1. live GitHub exact HEAD + exact-head CI;
2. this file;
3. `.planning/ROADMAP.md`;
4. `.planning/REQUIREMENTS.md`;
5. approved slice/release record;
6. `docs/MASTER_CONTEXT.md`;
7. historical/master snapshots only as context.

Two laws remain mandatory:
- **KISS:** simple, efficient, optimized, effective; A→B.
- **Anti-noise:** `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.

Operational anti-patch guardrail:
`REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.

## Functional baseline — CLOSED / FEATURE-FROZEN

Authoritative functional branch:
`feat/f12-7-pdf-to-png-mvp-green3`

Functional HEAD:
`8e1845d6d8fe72374d7a80994d8500b6d36077cc`

Functional exact-head CI:
- run `38555829715`;
- job `115824598759`;
- Release build 0 warnings / 0 errors;
- tests `1202/1202 PASS`.

`main` remains read-only at:
`31c0594758a83ec555d73ecdd7c597cdf8791fd7`

No merge/rebase/force-push to `main` without explicit user approval.

## Conservative integration candidate — SELECTED

Branch:
`integration/release-candidate-20261014`

HEAD:
`01d4adc3972ed939c5a2723a75d1833e61825d63`

This is a direct descendant of the functional baseline with documentation/planning lineage only. Historical stacked PRs are evidence, not a merge queue. Old `integration/release-candidate` is stale/divergent and must not be reused.

Verification anchor:
- run `38593351241`;
- job `115953668459`;
- hygiene/staging/restore/build/tests PASS.

## Release QA Packs

Automated preflight PASS; real/manual/private/physical gates remain open:
- Pack A — Reader / Organize / Editing;
- Pack B — ZPL / QR / print-scan;
- Pack C — visual + PFX/P12 signatures;
- Pack D — OCR / Forms / Compare / Batch / PDF→PNG.

Canonical records remain under `docs/release/2026-10-14-pack-*.md`.
Synthetic/CI PASS never upgrades real/manual/physical/private QA to PASS.

## Runtime / installer / license audit

Canonical record:
`docs/release/2026-10-14-runtime-installer-license-audit.md`

KISS release order:
`visual QA → dependency hygiene → deterministic publish → runtime inventory → clean/offline smoke → minimal installer → installer smoke → RC`.

Labelize `ZplGSCustom.ttf` provenance re-audit has supportive redistribution evidence with residual original-author provenance risk; do not claim legal certainty.

## Windows development environment — VERIFIED BY CODEX

Reported 2026-10-14:
- Windows 11 x64;
- Git + PowerShell 7;
- .NET SDK 10.0.402 x64;
- Labelize 1.7.0, qpdf 12.4.2, Tesseract 5.5.3.20260724 staged;
- locked restore PASS;
- Release build PASS;
- application startup smoke PASS.

That run used an older audit checkout, so it is environment evidence only, not the current release-head verification anchor.

Open dependency hygiene item:
- `System.Security.Cryptography.Pkcs 8.0.1` reports NU1902 + NU1903 in current restore/build;
- resolve separately by root cause before public release;
- do not patch `main` directly.

## UI/UX release modernization

Canonical record:
`docs/release/2026-10-14-ui-modernization.md`

Implementation plan:
`docs/superpowers/plans/2026-10-14-release-ui-modernization.md`

Branch:
`feat/release-ui-modernization`

Base:
`25ac6e163662207bbc40157d1bed3811d1974e62`

### UI-1 Foundation — GREEN

Implementation anchor:
`198b278605d33bff61be0ee5a9097d359239b324`

Implemented:
- `App.xaml` → `ThemeMode="System"`;
- one shared `Themes/Modern.xaml` resource dictionary;
- no third-party UI framework/package.

Exact-head CI:
- run `38599058194` — SUCCESS.

### UI-2 Main shell — GREEN

Final correction HEAD:
`fb5413d84fb5e2e733a791a44c6688b6d01a7acd`

Resolved root cause:
- eager app-level `StaticResource` lookup failed when tests constructed `MainWindow` without `App.xaml` resources;
- shell now defers SG resource lookup with `DynamicResource` where required;
- shared SG resources, shell structure, names and handlers preserved;
- no duplicate dictionary, test bootstrap, framework or functional rewrite added.

Exact-head CI:
- run `38630414847` — SUCCESS.

### UI-3 Dialog consistency — GREEN

RED contract HEAD:
`494115e0e84fe9d923c3503da21a0be9ab83540f`

RED evidence:
- build PASS;
- 1209 existing tests PASS;
- exactly 5 expected new failures, one per dialog before shared styles were applied.

GREEN product HEAD:
`60bb0f73090cd4d3e24d5eab5c69ee707432f823`

Changed only approved dialog XAML:
- PDF password;
- signature draw;
- signature library;
- signature name;
- signature photo.

No C# behavior/package/handler/control-name changes.

Exact-head CI:
- run `38632179198` — SUCCESS;
- full suite `1214/1214 PASS`, 0 failed, 0 skipped.

### UI-4 Windows visual QA — READY / NOT RUN

Canonical matrix:
`docs/release/2026-10-14-ui-visual-qa.md`

Required real evidence includes:
- light + dark system theme;
- 1366×768 at 100% scaling;
- one higher-resolution/scaled profile;
- keyboard focus;
- disabled states;
- empty shell, loaded PDF, label workflow, password dialog and signature dialogs;
- functional smoke without detached handlers/crash.

All UI-4 manual cases are currently **NOT RUN**. Do not infer PASS from CI.

## Official icon — PENDING BINARY MATERIALIZATION

User-approved visual identity is documented:
- stylized red PDF/document mark;
- white negative-space page/fold forms;
- high-contrast silhouette.

The binary `.ico` asset is not committed. Materialize only from the approved source image when that source is actually available; do not silently redesign or substitute it.

## Current checkpoint — 2026-10-14

Current release/UI branch:
`feat/release-ui-modernization`

Latest product-code HEAD:
`60bb0f73090cd4d3e24d5eab5c69ee707432f823`

Truth at checkpoint:
- F0–F12 functional baseline: CLOSED / frozen;
- UI-1: GREEN;
- UI-2: GREEN;
- UI-3: GREEN;
- UI-4: READY but real Windows evidence NOT RUN;
- official icon binary: pending source-image materialization;
- `System.Security.Cryptography.Pkcs` dependency hygiene: open separate task;
- deterministic portable publish: not started after visual gate;
- installer: not started;
- Packs A–D real/manual/private/physical evidence remains open;
- `main`: unchanged/read-only.

## Resume order

1. Execute `docs/release/2026-10-14-ui-visual-qa.md` on the real Windows environment and capture evidence locally.
2. Fix only reproduced P0/P1 visual usability defects; each fix returns through full suite + exact-head CI.
3. Update the visual QA record and this STATE with actual PASS/FAIL evidence.
4. Materialize/integrate the approved app/PDF icon when its source image is available.
5. Resolve `System.Security.Cryptography.Pkcs 8.0.1` NU1902/NU1903 by root cause.
6. Build deterministic self-contained `win-x64` publish.
7. Runtime inventory + clean/offline smoke.
8. Add one minimal installer technology only after publish smoke is GREEN.
9. Installer smoke → RC → remaining representative Packs A–D.
10. `main` only with explicit approval.

## Planning estimates

Planning estimates, not telemetry:
- automated functional implementation: **~100%**;
- release readiness: **~87%** while real visual/dependency/publish/installer gates remain open;
- weighted overall: **~94%**.
