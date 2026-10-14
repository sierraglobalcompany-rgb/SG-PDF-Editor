# Roadmap — SG PDF Editor

> GitHub exact-head CI manda sobre snapshots históricos. Estado operativo: `.planning/STATE.md`. Requisitos: `.planning/REQUIREMENTS.md`.

## Estado funcional

F0–F12 baseline: **CLOSED / AUTO PASS**.

Los gates manuales/privados/físicos permanecen abiertos donde `.planning/REQUIREMENTS.md` y el checklist de release lo indiquen.

## Release path — estado actual

### R1. Bounded real QA packs — DEFINED / NOT RUN

Checklist canónico: `docs/release/2026-10-14-real-qa-packs.md`.

Packs:
- A — Reader / Organize / Editing;
- B — ZPL / QR / physical print-scan;
- C — visual + cryptographic signatures;
- D — OCR / Forms / Compare / Batch / PDF→PNG.

Todos siguen **NOT RUN** hasta que exista evidencia real.

### R2. Documentation consolidation — BASELINE PASS

`AGENTS.md` ya no duplica el estado histórico F3. La autoridad operativa vive en GitHub exact heads + `STATE` + `ROADMAP` + `REQUIREMENTS`.

### R3. Release integration / stabilization

#### R3.1 Ancestry + PR forensic audit — CLOSED

Artifact: `docs/release/2026-10-14-integration-audit.md`.

Hallazgos cerrados:
- `feat/f12-7-pdf-to-png` @ `6e9240f...` es descendiente lineal de `main`: **1217 ahead / 0 behind**;
- F11 closure → final F12: **229 ahead / 0 behind**;
- muestras representativas de ramas F12, audit, design y temp/review inspeccionadas están contenidas en la línea final;
- no se detectó una segunda línea de producto en las ramas representativas auditadas;
- la cadena de PRs históricos llega hasta F11; F12 no tiene PRs equivalentes;
- por tanto, no se debe reconstruir release mergeando PRs históricos uno por uno.

No se ejecutó merge, rebase, force-push, branch deletion ni PR cleanup.

#### R3.2 Dedicated integration candidate — NEXT

Siguiente bloque:
1. crear una sola rama candidata desde el último head auditado de release-prep;
2. no modificar `main`;
3. ejecutar full exact-head Windows CI;
4. comprobar diff/ancestry antes de cualquier QA/release labeling;
5. mantener los PR/branches históricos intactos hasta decisión posterior.

#### R3.3 Real QA on candidate — OPEN

Ejecutar los packs A–D con PASS/FAIL/NOT RUN honestos. Priorizar P0/P1 e integridad de datos.

### R4. Installer / license / offline stabilization — OPEN

Antes de RC:
- provenance/licencia de assets/runtime, incluido Labelize/fonts;
- clean-clone + locked restore;
- packaged runtime/installer smoke;
- Windows offline smoke;
- no P0/P1 abiertos.

### R5. Release candidate / final — OPEN

RC no agrega features. Solo estabiliza, integra, verifica y documenta limitaciones reales.

## KISS post-F12

No reabrir features baseline oportunísticamente. Nuevas conversiones, simbologías, operaciones batch, formularios o firma avanzada requieren:

`VALUE GATE → CAPABILITY GATE → MVP estrecho → TEST → CLOSURE`.

No merge a `main` sin aprobación explícita del usuario.