---
gsd_state_version: '1.1'
status: f12-functional-baseline-closed-release-ui-polish-open
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
- tests 1202/1202 PASS.

`main` remains read-only at:
`31c0594758a83ec555d73ecdd7c597cdf8791fd7`

No merge/rebase/force-push to `main` without explicit user approval.

## Conservative integration candidate — SELECTED

Branch:
`integration/release-candidate-20261014`

HEAD:
`01d4adc3972ed939c5a2723a75d1833e61825d63`

This is a direct descendant of the functional baseline with documentation/planning lineage only. Old stacked PRs are historical evidence, not a merge queue. Old `integration/release-candidate` is stale/divergent and must not be reused.

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

Audit checkpoint:
`25ac6e163662207bbc40157d1bed3811d1974e62`

Runtime/license audit CI:
- run `38595250869` / job `115960181338` — SUCCESS.

KISS release order remains:
`visual polish → deterministic publish → runtime inventory → clean/offline smoke → minimal installer → installer smoke → RC`.

Labelize `ZplGSCustom.ttf` provenance re-audit is complete with supportive redistribution evidence and documented residual original-author provenance risk. Do not claim legal certainty.

## Windows development environment — VERIFIED BY CODEX

Reported on 2026-10-14:
- Windows 11 x64;
- Git + PowerShell 7;
- .NET SDK 10.0.402 x64 installed per-user;
- Labelize 1.7.0, qpdf 12.4.2, Tesseract 5.5.3.20260724 staged successfully;
- locked restore PASS;
- Release build PASS;
- local tests reported 1178/1178 PASS on the older audit checkout used by Codex;
- application startup smoke PASS.

Codex used an older audit branch, so those counts are **environment evidence only**, not the current release-head verification anchor.

Open dependency hygiene item:
- `System.Security.Cryptography.Pkcs 8.0.1` transitively reports NU1902 + NU1903;
- resolve separately by root cause before public release;
- do not patch `main` directly.

## UI/UX release modernization — OPEN

Canonical record:
`docs/release/2026-10-14-ui-modernization.md`

Implementation plan:
`docs/superpowers/plans/2026-10-14-release-ui-modernization.md`

Branch:
`feat/release-ui-modernization`

Base:
`25ac6e163662207bbc40157d1bed3811d1974e62`

Reference guidance:
- Microsoft WPF Fluent System theme;
- `nextlevelbuilder/ui-ux-pro-max-skill` MIT, **design/development guidance only**, not runtime dependency.

### UI-1 Foundation — GREEN

Implementation HEAD:
`198b278605d33bff61be0ee5a9097d359239b324`

Implemented:
- `App.xaml` → `ThemeMode="System"`;
- one shared `Themes/Modern.xaml` resource dictionary;
- no third-party UI framework/package.

Exact-head CI:
- run `38599058194`;
- job `115973245330`;
- hygiene/staging/restore/build/tests SUCCESS.

### UI-2 Main shell — BLOCKED AFTER GREEN ATTEMPT

RED contract commit:
`383413bff2cdf6dca130ccb6bd1379f2ad89cc06`

RED exact-head evidence:
- run `38599600200`;
- job `115975105472`;
- Release build PASS;
- tests: 1207 PASS, exactly 1 FAIL as expected because `MainWindow` had not yet adopted the modern shell surfaces;
- 7 `xUnit2031` analyzer warnings came from the test contract itself, not production code.

Analyzer-noise cleanup commit:
`e70a5aada42f4181328e3f55dcc92ebf2e957d2d`

GREEN implementation attempt:
`4a83e7ac48f7fbbcc716274236192285d91c8062`

Attempted scope only:
- apply shared modern surfaces/styles to the existing `MainWindow` shell;
- preserve root `DockPanel`, three-column reader layout, behavior-owned control names and event wiring;
- no dialogs, icon, installer, publish or new package work.

Exact-head CI for GREEN attempt:
- run `38600360434`;
- job `115977718873`;
- result: **FAIL during XAML compilation/build**;
- tests did not become authoritative for this HEAD because compilation failed;
- root cause has **not** been diagnosed yet.

**UI-2 is NOT GREEN. Do not patch forward and do not start UI-3/dialogs until the exact XAML compile cause is identified and fixed minimally.**

## Official icon — USER APPROVED REFERENCE

On 2026-10-14 the user supplied the official visual reference for:
1. SG PDF Editor application/executable icon;
2. Windows PDF file-association icon;
3. future installer/start-menu/shortcut icon.

Visual identity:
- stylized red PDF/document silhouette;
- white negative-space page/fold forms;
- simple high-contrast mark.

The user-provided image in chat is the visual master reference. The binary `.ico` asset is **not yet committed**. When work resumes, derive the required Windows multi-resolution icon from that reference without redesigning its identity.

## Current checkpoint / STOP POINT — 2026-10-14

Checkpoint branch:
`feat/release-ui-modernization`

Product HEAD at stop request:
`4a83e7ac48f7fbbcc716274236192285d91c8062`

Checkpoint sequence:
- UI-1: GREEN and CI verified;
- UI-2 RED: valid and confirmed by one expected failing contract test;
- UI-2 test analyzer noise: cleaned;
- UI-2 GREEN attempt: written, but exact-head CI fails during XAML compilation; **BLOCKED / NOT GREEN**;
- root cause of the XAML compile failure: pending diagnosis;
- icon identity: approved and documented; binary asset integration pending;
- dialogs: not started;
- screenshot QA: not started;
- portable publish: intentionally paused until visual gate;
- installer: intentionally not started;
- `main`: unchanged/read-only.

No further product changes are authorized by this checkpoint itself.

## Resume order

1. Start from `feat/release-ui-modernization` and read the exact build/XAML error for run `38600360434`, job `115977718873`.
2. Reproduce/classify the XAML compile failure and identify root cause before editing.
3. Apply the smallest UI-2 correction only; no opportunistic redesign.
4. Run focused contract + full suite + exact-head CI; UI-2 may be called GREEN only after build/tests succeed.
5. Modernize dialogs using the same shared resources.
6. Integrate the approved app/PDF icon asset.
7. Run real Windows screenshot QA at 1366×768+ and system scaling.
8. Resolve the transitive `System.Security.Cryptography.Pkcs` vulnerability cleanly.
9. Resume deterministic self-contained `win-x64` publish.
10. Only after publish/offline smoke GREEN, add one minimal installer technology.
11. RC + remaining representative Packs A–D; `main` only with explicit approval.

## Planning estimates

Planning estimates, not telemetry:
- automated functional implementation: **~100%**;
- release readiness: **~87%** because visual modernization is now a real release gate;
- weighted overall: **~94%**.
