---
gsd_state_version: '1.0'
status: f12-in-progress-redaction-ui-stage-green-paused
progress:
  total_phases: 13
  completed_phases: 12
  total_plans: 13
  completed_plans: 12
  percent: 92
---

# Project State

## Project Reference
See `.planning/PROJECT.md`.

**Core value:** Resolver PDF + ZPL diario de forma rápida, privada, estable y offline.

## Current state

F12 is IN PROGRESS on branch `feat/f12-professional` and is intentionally PAUSED by user request after two GREEN redaction microblocks.

Functional HEAD before checkpoint documentation: `142d7b063faabc897ee56c015ba4de5f35071013`.

Checkpoint document: `docs/history/2026-10-12-F12-checkpoint-redaction-ui-stage.md`.

Completed in this checkpoint:

- text-selection redaction eligibility shown in the existing properties panel using existing `RedactionPolicy`;
- safe/unsafe reasons exposed conservatively;
- `Redactar` stages only an in-memory redaction draft using existing `RedactionWorkspace`;
- source path + fingerprint must match the active EDITAR source workspace;
- staged redaction participates in the existing unsaved-changes guard;
- source PDF bytes remain unchanged while staging;
- no writer/materializer/Save As publication integration has been added yet.

## Fresh automated evidence

- Eligibility GREEN `4e91710a5397c9c34f0d42058b137c10bc9682b2`: Windows CI run `38285822979`, job `114933825149`; Release build 0 warnings / 0 errors; 1110/1110 tests PASS.
- Draft staging GREEN `142d7b063faabc897ee56c015ba4de5f35071013`: Windows CI run `38286648237`, job `114936510676`; Release build 0 warnings / 0 errors; 1111/1111 tests PASS.

`LOCAL DIRECT RUN = NOT SEPARATELY EVIDENCED`.

## Guardrails

KISS / YAGNI remain mandatory. Reuse existing F12 contracts and the current EDITAR/Save As pipeline. Do not add a second sidebar, service layer, workspace family, or parallel save architecture without demonstrated need. Redaction must fail closed and staging must never mutate the original PDF. No merge to `main` without explicit user approval.

## Next — NOT STARTED

```text
inspect existing Save As + PdfRedactionMaterializer contract
→ write test-only RED proving staged redaction publishes only through Guardar como...
→ minimum GREEN integration into the existing save pipeline
→ source/fingerprint drift must fail closed
→ focused + full Windows CI
```

Do not skip directly to GREEN. Do not open additional F12 scope before this publication microblock closes.

## Manual/private/physical gates

All previously documented manual/private/physical gates remain open unless separately evidenced. Automated PASS does not replace representative private-PDF, real Windows UX, Adobe/Foxit/Edge interoperability, or physical/offline QA.

## Continuity

- F12 plan: `docs/superpowers/plans/2026-10-12-f12-redaction.md`.
- F12 checkpoint: `docs/history/2026-10-12-F12-checkpoint-redaction-ui-stage.md`.
