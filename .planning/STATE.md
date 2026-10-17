---
gsd_state_version: '1.1'
status: f13-task9-t9-1-dg0-green-r1-resume-ready
progress:
  total_phases: 19
  completed_phases: 13
  current_phase: F13
  current_task: Task9
  current_subtask: T9.1
---

# Project State

## Authority

Operational order:
1. live GitHub exact HEAD + exact-head CI;
2. this file;
3. `.planning/ROADMAP.md`;
4. `.planning/REQUIREMENTS.md`;
5. approved slice/spec/plan;
6. for UI/UX: `docs/design/SGPDF_DESIGN_LAW.md` + `.agents/skills/sgpdf-design/SKILL.md`;
7. `docs/MASTER_CONTEXT.md`;
8. historical/master snapshots as context only.

## Mandatory rules

- **KISS:** `SIMPLE → EFICIENTE → OPTIMIZADO → EFICAZ → MANTENIBLE → A→B`.
- **Anti-noise:** `DELETE → SIMPLIFY → REUSE → MERGE → EXTEND → ADD`.
- **Ley SG de Diseño:** `INTENT → HIERARCHY → DISCLOSURE → CONSISTENCY → FEEDBACK → ACCESSIBILITY → VERIFY`.
- **Guardrail anti-parches** (separate, not a numbered law): `REPRODUCE → CLASSIFY → ROOT CAUSE → OWNER → MINIMAL CHANGE → CAUSE TEST → FULL SUITE → EXACT-HEAD CI → REMOVE REDUNDANT WORKAROUND`.

## Frozen history

- F0–F12: CLOSED / FROZEN for the automated baseline already recorded in requirements/history.
- Real/manual/private/physical QA remains separate wherever historical records say NOT RUN.
- F13 Tasks 1–8: CLOSED by their existing checkpoints.
- F14–F18: FROZEN; do not start opportunistically.

## F13 Task9 — ACTIVE

- T9.0: DONE.
- T9.1: ACTIVE.
- T9.2: NOT STARTED.
- No merge to `main` approved.

### Last verified GREEN product anchor before current document-shell R1

Commit:
`0abae7ceb5f08611180c883346e21589d1509664`

Exact-head CI:
- run `39000181654`;
- conclusion `SUCCESS`.

### DG-0 — SG Design Governance — GREEN / CLOSED

Branch:
`feat/f13-dg0-sg-design-governance-20261017`

Base:
`0abae7ceb5f08611180c883346e21589d1509664`

RED contract:
`57b1eeb22302b33619e58246a3f3dcb1b8970363`

RED exact-head CI:
- run `39002512451`;
- job `117308235572`;
- Build PASS;
- startup smoke PASS;
- Test FAIL as intended before governance materialization.

GREEN implementation anchor:
`3011f700e9014d72951314291bab4c3a67b89c8c`

GREEN exact-head CI:
- run `39002956513`;
- job `117309529279`;
- Build PASS;
- startup smoke PASS;
- Test PASS;
- overall SUCCESS.

Closure record:
`docs/history/2026-10-17-f13-dg0-design-governance-checkpoint.md`

DG-0 adds no runtime/product feature, no new UI framework, and no production dependency. Its purpose is durable design governance before further T9.1 visual work.

### R1 document-shell / multi-document — RED / INCOMPLETE — NEXT

Branch:
`feat/f13-task9-t9-1-document-shell-realignment-20261017`

Frozen checkpoint:
`bbae51f49ee3c54e3667c5ea477c2c3f179d3c46`

Exact-head CI at that checkpoint:
- run `39001470186`;
- conclusion `FAILURE`.

Do not promote this branch to a green baseline. Resume its existing RED contracts next while carrying the validated Design Law/skill; do not restart from stale F12 routing.

## Main

`main` live verified at:
`31c0594758a83ec555d73ecdd7c597cdf8791fd7`

Read-only until explicit user approval. No automatic merge/rebase/force-push.

## Runtime / architecture constraints

- Windows x64 + WPF + .NET 10.
- `SGPdf.App + SGPdf.App.Tests` while sufficient.
- PDFium primary PDF engine; `PdfiumRuntime.NativeGate` for native access.
- Labelize 1.7.0 only ZPL runtime renderer.
- PDFsharp narrow approved roles only; not general second engine.
- qpdf narrow AES sidecar.
- Tesseract local OCR.
- ZXing.Net test/QA only.
- No backend, DB, cloud, plugin framework, second viewer/engine, generalized ModeManager/conversion/Batch framework without explicit decision.
- No new UI framework/package simply for visual polish.

## Visual / design authority

The approved app icon, PDF document icon and 15 Home tool icons keep distinct roles. Do not reinterpret them during unrelated UX work.

For all visible/interactable changes, load the SG design skill. Core rule:

`INTENT → HIERARCHY → DISCLOSURE → CONSISTENCY → FEEDBACK → ACCESSIBILITY → VERIFY`

The ZPL properties panel is the reference problem for progressive disclosure: supported capabilities remain, but unrelated settings must not all compete in one long visible property sheet.

## Resume order

1. Resume R1 from `bbae51f49ee3c54e3667c5ea477c2c3f179d3c46`, preserving its RED contracts and incorporating DG-0 governance deliberately.
2. Continue remaining T9.1 work under the Design Law and required exact-head/visual gates.
3. Do not start T9.2 until T9.1 is actually closed.
4. Keep F14–F18 frozen.
5. `main` only with explicit approval.

## QA semantics

Automated PASS never upgrades Windows-real, hardware, private-corpus, scanner, printer, stylus/touch, external-reader or other manual evidence to PASS. Use `NOT RUN`, `BLOCKED — ENVIRONMENT`, or the actual observed result.
